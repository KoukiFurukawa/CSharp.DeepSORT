using DeepSORT.Domain.Models.WebCamera;
using Xunit.Abstractions;

namespace DeepSORT.Test.Domain.WebCameraTest
{
    public class WebCameraTest
    {
        private WebCameraFps _fps { get; init; }
        private readonly ITestOutputHelper _testOutputHelper;

        public WebCameraTest(ITestOutputHelper testOutputHelper)
        {
            this._testOutputHelper = testOutputHelper;
            this._fps = new WebCameraFps(10);
        }

        [Fact]
        public void Could_Open_WebCamera()
        {
            var webCamera = new WebCamera(this._fps);
            Assert.NotNull(webCamera);

            // 未OpenのカメラをOpenすると成功する
            var (result, err) = webCamera.Open();
            Assert.Null(err);
            Assert.True(result);

            // Open済みのカメラに対して Open しようとするとエラーが返る
            (result, err) = webCamera.Open();
            Assert.False(result);
            Assert.NotNull(err);
        }

        [Fact]
        public void Could_Grab_Image()
        {
            var webCamera = new WebCamera(this._fps);
            Assert.NotNull(webCamera);

            // 未Openのカメラから画像を取得しようとするとエラーが返る
            var (image, err) = webCamera.GrabImage();
            Assert.NotNull(err);

            webCamera.Open();

            // Open済みのカメラから画像を取得すると成功する
            (image, err) = webCamera.GrabImage();
            Assert.Null(err);
            Assert.NotNull(image);
        }

        [Fact]
        public void Could_Close_WebCamera()
        {
            var webCamera = new WebCamera(this._fps);
            Assert.NotNull(webCamera);

            // 未OpenのカメラをCloseするとエラーが返る
            var err = webCamera.Close();
            Assert.NotNull(err);

            webCamera.Open();

            // Open済みのカメラをCloseすると成功する
            err = webCamera.Close();
            Assert.Null(err);
        }

    }
}