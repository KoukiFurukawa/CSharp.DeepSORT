using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenCvSharp;
using OpenCvSharp.Dnn;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace DeepSORT.Domain.Models.Detector;

public class Detector
{
    private string modelPath { get; }
    private readonly InferenceSession _onnxSession;
    private readonly string _inputName;
    private readonly int[] _inputShape;
    private readonly string _outputName;
    private readonly double confThreshold = 0.2;
    private readonly double iouThreshold = 0.3;

    public Detector(ModelPath modelPath)
    {
        this.modelPath = modelPath.GetValue();
        this._onnxSession = InitializeSession();
        (this._inputName, this._inputShape) = GetModelInputInfo();
        this._outputName = GetModelOutputInfo();
    }

    public (List<Rect>, List<float>, List<int>) Inference(Mat image)
    {
        Mat tempImage = image.Clone();
        int imgHeight = tempImage.Height;
        int imgWidth = tempImage.Width;

        // var processedImage = this.Preprocess(tempImage);

        var inputTensor = this.PrepareInput(tempImage);
        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor(_inputName, inputTensor)
        };

        using var results = _onnxSession.Run(inputs);

        var output = results[0].AsTensor<float>();
        var shape = output.Dimensions;

        return this.ProcessOutput(output, imgHeight, imgWidth);
    }

    public void Close()
    {
        this._onnxSession.Dispose();
    }

    private Tensor<float> PrepareInput(Mat image)
    {
        Mat inputImg = new Mat();
        Cv2.CvtColor(image, inputImg, ColorConversionCodes.BGR2RGB);

        Cv2.Resize(inputImg, inputImg, new Size(640, 640));

        inputImg.ConvertTo(inputImg, MatType.CV_32FC3, 1.0 / 255.0);

        // var channels = Cv2.Split(inputImg); // Split the channels (HWC -> CHW)
        var inputTensor = new DenseTensor<float>([1, 3, this._inputShape[2], this._inputShape[3]]);

        /*for (int c = 0; c < 3; c++)
        {
            for (int h = 0; h < this._inputShape[2]; h++)
            {
                for (int w = 0; w < this._inputShape[3]; w++)
                {
                    inputTensor[0, c, h, w] = (float)channels[c].At<float>(h, w);
                }
            }
        }*/

        // var inputTensor = CvDnn.BlobFromImage(inputImg, 1.0, new Size(640, 640), new Scalar(), true, false);

        for (int y = 0; y < 640; y++)
        {
            for (int x = 0; x < 640; x++)
            {
                var pixel = inputImg.At<Vec3f>(y, x);
                inputTensor[0, 0, y, x] = pixel.Item0;
                inputTensor[0, 1, y, x] = pixel.Item1;
                inputTensor[0, 2, y, x] = pixel.Item2;
            }
        }

        return inputTensor;
    }


    private InferenceSession InitializeSession()
    {
        return new InferenceSession(this.modelPath);
    }


    private (string, int[]) GetModelInputInfo()
    {
        var inputMeta = this._onnxSession.InputMetadata;
        foreach (var name in inputMeta.Keys)
        {
            var inputShape = inputMeta[name].Dimensions;
            return (name, inputShape);
        }
        throw new Exception("Model does not have any inputs.");
    }


    private string GetModelOutputInfo()
    {
        var outputMeta = _onnxSession.OutputMetadata;
        foreach (var name in outputMeta.Keys)
        {
            return name;
        }
        throw new Exception("Model does not have any outputs.");
    }

    private (List<Rect>, List<float>, List<int>) ProcessOutput(Tensor<float> output, int imgHeight, int imgWidth)
    {
        float[][] predictions = SqueezeAndTransposeTensor(output); // 転置も一緒にしてる
        int numPredictions = predictions[0].Length;
        int numAttributes = predictions.Length;
        var scores = new float[numAttributes];
        for (int i = 0; i < numAttributes; i++)
        {
            // scores[i] = predictions[i].Skip(4).Max();
            float maxScore = float.MinValue;
            for (int j = 4; j < numPredictions; j++)
            {
                if (predictions[i][j] > maxScore && predictions[i][j] < 1)
                {
                    maxScore = predictions[i][j];
                }
            }
            scores[i] = maxScore;
        }

        var filteredPredictions = new List<float[]>();
        var filteredScores = new List<float>();
        var filteredIndices = new List<int>();

        for (int i = 0; i < numAttributes; i++)
        {
            if (scores[i] > this.confThreshold)
            {
                var prediction = new float[numPredictions];
                for (int j = 0; j < numPredictions; j++)
                {
                    prediction[j] = predictions[i][j];
                }
                filteredPredictions.Add(prediction);
                filteredScores.Add(scores[i]);
                filteredIndices.Add(i);
            }
        }

        if (filteredIndices.Count == 0)
        {
            return (new List<Rect>(), new List<float>(), new List<int>());
        }

        var classIds = new List<int>();
        foreach (var prediction in filteredPredictions)
        {
            var classId = Array.IndexOf(prediction.Skip(4).ToArray(), prediction.Skip(4).Max());
            classIds.Add(classId);
        }

        // Extract bounding boxes
        var boxes = ExtractBoxes(filteredPredictions);
        boxes = RescaleBoxes(boxes, imgHeight, imgWidth);

        // Apply Non-Maximum Suppression (NMS)
        var indices = Utils.MulticlassNms(boxes, filteredScores, classIds, this.iouThreshold);

        return (
            indices.Select(i => boxes[i]).ToList(),
            indices.Select(i => scores[i]).ToList(),
            indices.Select(i => classIds[i]).ToList()
        );
    }

    private List<Rect> ExtractBoxes(List<float[]> predictions)
    {
        var boxes = new List<Rect>();

        foreach (var prediction in predictions)
        {
            var x = prediction[0];
            var y = prediction[1];
            var width = prediction[2];
            var height = prediction[3];
            var rect = new Rect((int)x, (int)y, (int)width, (int)height);
            boxes.Add(rect);
        }
        return boxes;
    }

    public List<Rect> RescaleBoxes(List<Rect> boxes, int imgHeight, int imgWidth)
    {
        var inputShape = new float[] { 640, 640, 640, 640 };

        var rescaledBoxes = new List<Rect>();
        foreach (var box in boxes)
        {
            var x = box.X / inputShape[0] * imgWidth;
            var y = box.Y / inputShape[1] * imgHeight;
            var width = box.Width / inputShape[2] * imgWidth;
            var height = box.Height / inputShape[3] * imgHeight;

            rescaledBoxes.Add(new Rect((int)x, (int)y, (int)width, (int)height));
        }

        return rescaledBoxes;
    }

    private float[][] SqueezeTensor(Tensor<float> tensor)
    {
        float[][] tensorT = new float[tensor.Dimensions[2]][];

        for (int w = 0; w < tensor.Dimensions[2]; w++)
        {
            tensorT[w] = new float[tensor.Dimensions[1]];
            for (int h = 0; h < tensor.Dimensions[1]; h++)
            {
                tensorT[w][h] = tensor[0, h, w];
            }
        }

        return tensorT;
    }

    private float[][] SqueezeAndTransposeTensor(Tensor<float> tensor)
    {
        // Assuming tensor shape is (1, 84, 8400)
        int channels = tensor.Dimensions[1]; // 84
        int width = tensor.Dimensions[2];    // 8400

        // Create a new 2D array for the transposed tensor
        float[][] tensorT = new float[width][];

        for (int w = 0; w < width; w++)
        {
            tensorT[w] = new float[channels];
            for (int c = 0; c < channels; c++)
            {
                tensorT[w][c] = tensor[0, c, w];
            }
        }

        return tensorT;
    }
}
