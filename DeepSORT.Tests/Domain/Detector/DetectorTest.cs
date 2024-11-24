using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using OpenCvSharp;
using Microsoft.ML.OnnxRuntime;
using DeepSORT.Domain.Models.Detector;
using System.Diagnostics;
using Xunit.Abstractions;
using Xunit.Sdk;
using Microsoft.ML.OnnxRuntime.Tensors;
using Microsoft.VisualStudio.TestPlatform.Utilities;

namespace DeepSORT.Test.Domain.DetectorTest;

public class DetectorTests
{
    private readonly ModelPath _mockModelPath;
    private readonly string IMAGE_PATH = AppContext.BaseDirectory + "./images/zidane.jpg";
    private readonly ITestOutputHelper _testOutputHelper;

    public DetectorTests(ITestOutputHelper testOutputHelper)
    {
        string modelPath = AppContext.BaseDirectory + "./models/yolo11n.onnx";
        this._mockModelPath = new ModelPath(modelPath);
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public void Detector_Initialization_Success()
    {
        Detector detector = new(this._mockModelPath);
        Assert.NotNull(detector);
        detector.Close();
    }

    [Fact]
    public void Inference_ShouldReturnExpectedResults()
    {
        Detector detector = new(this._mockModelPath);
        Mat image = Cv2.ImRead(IMAGE_PATH);

        /*Tensor<float> output = detector.Inference(image);
        _testOutputHelper.WriteLine($"{output.Dimensions[0]}, {output.Dimensions[1]}, {output.Dimensions[2]}");*/

        var (bboxes, scores, classIds) = detector.Inference(image);
        // _testOutputHelper.WriteLine($"{predictions}");
        Assert.NotEmpty(bboxes);
        Assert.NotEmpty(scores);
        Assert.NotEmpty(classIds);

        Assert.Equal(3, bboxes.Count());
        Assert.Equal(3, scores.Count());
        Assert.Equal(3, classIds.Count());
    }
}