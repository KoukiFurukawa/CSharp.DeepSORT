using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCvSharp;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace CsharpByteTrack.ObjectDetection;
public class YoloxObjectDetector
{
    private const string DEFAULT_MODEL_PATH = "./model/object_detection_model.onnx";
    private readonly InferenceSession _onnxSession;
    private readonly string _inputName;
    private readonly int[] _inputShape;
    private readonly string _outputName;

    // コンストラクタ
    public YoloxObjectDetector(string modelPath = DEFAULT_MODEL_PATH)
    {
        _onnxSession = InitializeSession(modelPath);
        (_inputName, _inputShape) = GetModelInputInfo();
        _outputName = GetModelOutputInfo();
    }

    // セッションの初期化
    private static InferenceSession InitializeSession(string modelPath)
    {
        return new InferenceSession(modelPath);
    }

    // モデル入力情報の取得
    private (string, int[]) GetModelInputInfo()
    {
        var inputMeta = _onnxSession.InputMetadata;
        foreach (var name in inputMeta.Keys)
        {
            var inputShape = inputMeta[name].Dimensions;
            return (name, inputShape);
        }
        throw new Exception("Model does not have any inputs.");
    }

    // モデル出力情報の取得
    private string GetModelOutputInfo()
    {
        var outputMeta = _onnxSession.OutputMetadata;
        foreach (var name in outputMeta.Keys)
        {
            return name;
        }
        throw new Exception("Model does not have any outputs.");
    }

    // 推論
    public (List<Rect>, List<double>, List<int>) Inference(Mat image)
    {
        Mat tempImage = image.Clone();
        int imageHeight = tempImage.Rows;
        int imageWidth = tempImage.Cols;

        var (processedImage, ratio) = Preprocess(tempImage);

        // Tensorに変換する前にバイト配列の長さを確認し、Matの生データを取得
        var processedBytes = new byte[processedImage.Total() * processedImage.ElemSize()];
        Marshal.Copy(processedImage.Data, processedBytes, 0, processedBytes.Length);

        var inputTensor = new DenseTensor<byte>(processedBytes, _inputShape);
        var input = NamedOnnxValue.CreateFromTensor(_inputName, inputTensor);

        using var results = _onnxSession.Run(new[] { input });

        var test = results.First(v => v.Name == _outputName);

        // _outputName に一致する結果から double 型のリストに変換する
        var test2 = results.Where(v => v.Name == _outputName);  // _outputName と一致するすべての要素を取得

        var test3 = test2.SelectMany(v => ((DenseTensor<float>)v.Value).ToArray());
        var test4 = test3.Select(value => Convert.ToDouble(value));
        var test5 = test4.ToList();

        var output = test5 == null ? [] : test5.ToArray();

        //var output = results.First(v => v.Name == _outputName).AsEnumerable<double>().ToArray();

        return Postprocess(output, ratio, imageWidth, imageHeight);
    }

    // 前処理
    private (Mat, float) Preprocess(Mat image)
    {
        float ratio = Math.Min((float)_inputShape[0] / image.Rows, (float)_inputShape[1] / image.Cols);
        var resizedImage = image.Resize(new Size((int)(image.Cols * ratio), (int)(image.Rows * ratio)));

        Mat paddedImage = new Mat(new Size(_inputShape[1], _inputShape[0]), MatType.CV_8UC3, Scalar.All(114));
        resizedImage.CopyTo(paddedImage[new Rect(0, 0, resizedImage.Cols, resizedImage.Rows)]);

        return (paddedImage, ratio);
    }

    // 後処理
    private static (List<Rect>, List<double>, List<int>) Postprocess(double[] outputs, float ratio, int maxWidth, int maxHeight)
    {
        var bboxes = new List<Rect>();
        var scores = new List<double>();
        var classIds = new List<int>();

        for (int i = 0; i < outputs.Length; i += 6)
        {
            double score = outputs[i + 4];
            if (score > 0.5)
            {
                int xMin = Math.Max(0, (int)(outputs[i] / ratio));
                int yMin = Math.Max(0, (int)(outputs[i + 1] / ratio));
                int xMax = Math.Min(maxWidth, (int)(outputs[i + 2] / ratio));
                int yMax = Math.Min(maxHeight, (int)(outputs[i + 3] / ratio));

                bboxes.Add(new Rect(xMin, yMin, xMax - xMin, yMax - yMin));
                scores.Add(score);
                classIds.Add((int)outputs[i + 5]);
            }
        }

        return (bboxes, scores, classIds);
    }
}