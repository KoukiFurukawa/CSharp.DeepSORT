using DeepSORT.Domain.Models.Detector;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;


namespace DeepSORT.Domain.Models.Predictor;
public class Predictor
{
    private string ModelPath { get; }
    private string ImagePath { get; }
    public Predictor(string modelPath, string imagePath)
    {
        this.ModelPath = modelPath;
        this.ImagePath = imagePath;
    }

    public (string label, float[] features) PredictAndExtractFeatures()
    {
        using var session = new InferenceSession(ModelPath);
        var image = LoadAndPreprocessImage(ImagePath);

        var inputTensor = new DenseTensor<float>(image, new[] { 1, 3, 224, 224 });
        var input = NamedOnnxValue.CreateFromTensor("data", inputTensor);

        using var results = session.Run(new[] { input });
        var output = results.First().AsTensor<float>().ToArray();

        float sum = output.Sum(x => (float)Math.Exp(x));
        IEnumerable<float> softmax = output.Select(x => (float)Math.Exp(x) / sum);

        var label = GetLabel(output);
        var features = ExtractFeatures(output);

        return (label, features);
    }

    private static float[] LoadAndPreprocessImage(string imagePath)
    {
        using var image = SixLabors.ImageSharp.Image.Load<Rgb24>(imagePath);
        image.Mutate(x => x.Resize(new ResizeOptions
        {
            Size = new SixLabors.ImageSharp.Size(224, 224),
            Mode = ResizeMode.Crop
        }));

        var imageData = new float[3 * 224 * 224];
        for (int y = 0; y < 224; y++)
        {
            for (int x = 0; x < 224; x++)
            {
                var pixel = image[x, y];
                imageData[(0 * 224 + y) * 224 + x] = (pixel.R / 255.0f - 0.485f) / 0.229f;
                imageData[(1 * 224 + y) * 224 + x] = (pixel.G / 255.0f - 0.456f) / 0.224f;
                imageData[(2 * 224 + y) * 224 + x] = (pixel.B / 255.0f - 0.406f) / 0.225f;
            }
        }
        return imageData;
    }
    private static string GetLabel(float[] output)
    {
        // Assuming the output is a classification result, get the label with the highest score
        var maxIndex = output.ToList().IndexOf(output.Max());
        return $"Class {maxIndex}";
    }

    private static float[] ExtractFeatures(float[] output)
    {
        // Assuming the output contains the features directly
        return output;
    }

}