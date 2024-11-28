using DeepSORT.Infrastructure.Factory;
using DeepSORT.Domain.Models.WebCamera;
using Xunit.Abstractions;

namespace DeepSORT.Infrastructure.Test;
public class WebCameraFactoryTest
{

    private readonly ITestOutputHelper _testOutputHelper;

    public WebCameraFactoryTest(ITestOutputHelper testOutputHelper)
    {
        this._testOutputHelper = testOutputHelper;
    }

    [Fact]
    public void Could_Create_Same_WebCameraObject()
    {
        var fps = new WebCameraFps(10);
        var _WebCamera = new WebCamera(fps);

        var _WebCameraFactory = new WebCameraFactory();

        Assert.NotNull(_WebCameraFactory.Create(fps));

        Assert.Equal(_WebCamera.GetType(), _WebCameraFactory.Create(fps).GetType());
        Assert.NotEqual(_WebCamera, _WebCameraFactory.Create(fps));
    }
}