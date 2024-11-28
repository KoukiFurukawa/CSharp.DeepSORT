using DeepSORT.Infrastructure.Factory;
using DeepSORT.Domain.Models.Detector;
using Xunit.Abstractions;

namespace DeepSORT.Infrastructure.Test;
public class DetectorFactoryTest
{

    private readonly ITestOutputHelper _testOutputHelper;

    public DetectorFactoryTest(ITestOutputHelper testOutputHelper)
    {
        this._testOutputHelper = testOutputHelper;
    }

    [Fact]
    public void Could_Create_Same_DetectorObject()
    {
        string modelPath = AppContext.BaseDirectory + "./models/yolo11n.onnx";
        var _ModelPath = new ModelPath(modelPath);
        var _Detector = new Detector(_ModelPath);

        var _DetectorFactory = new DetectorFactory();

        Assert.NotNull(_DetectorFactory.Create(_ModelPath));

        Assert.Equal(_Detector.GetType(), _DetectorFactory.Create(_ModelPath).GetType());
        Assert.NotEqual(_Detector, _DetectorFactory.Create(_ModelPath));
    }
}