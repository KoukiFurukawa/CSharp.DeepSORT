using DeepSORT.Domain.Models.WebCamera;
using System.Text;

namespace DeepSORT.Test.Domain.WebCameraTest
{
    public class WebCameraFpsTest
    {
        [Fact]
        public void Constructor_Should_ThrowException_When_WebCameraFpsIsInvalid()
        {
            int fps_1 = 0; // 0以下
            int fps_2 = -128; // 0以下
            int fps_3 = 31; // 30以上
            int fps_4 = 100; // 30以上

            int fps_5 = 30;
            int fps_6 = 1;
            int fps_7 = 10;

            // 失敗
            Assert.Throws<ArgumentException>(() => new WebCameraFps(fps_1));
            Assert.Throws<ArgumentException>(() => new WebCameraFps(fps_2));
            Assert.Throws<ArgumentException>(() => new WebCameraFps(fps_3));
            Assert.Throws<ArgumentException>(() => new WebCameraFps(fps_4));

            // 成功
            WebCameraFps webCameraFps_1 = new(fps_5);
            WebCameraFps webCameraFps_2 = new(fps_6);
            WebCameraFps webCameraFps_3 = new(fps_7);
            Assert.NotNull(webCameraFps_1);
            Assert.NotNull(webCameraFps_2);
            Assert.NotNull(webCameraFps_3);
        }

    }
}