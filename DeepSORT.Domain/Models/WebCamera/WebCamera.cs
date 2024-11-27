using OpenCvSharp;

namespace DeepSORT.Domain.Models.WebCamera;
public class WebCamera
{
    public VideoCapture? Capture;

    // init params
    private int fps;
    private int bufferSize;
    private readonly int width = 640;
    private readonly int height = 480;

    public WebCamera(WebCameraFps fps, int bufferSize = 1)
    {
        this.fps = fps.GetValue();
        this.bufferSize = bufferSize;
    }

    public (bool, Exception?) Open()
    {
        bool result;

        if (this.Capture != null && this.Capture.IsOpened())
        {
            return (false, new Exception("Open済みのカメラを再度開こうとしています。"));
        }

        try
        {
            this.Capture = new VideoCapture(0);
            this.Capture.Set(VideoCaptureProperties.BufferSize, this.bufferSize);
            this.Capture.Set(VideoCaptureProperties.Fps, this.fps);
            this.Capture.Set(VideoCaptureProperties.FrameWidth, this.width);
            this.Capture.Set(VideoCaptureProperties.FrameWidth, this.height);
            result = Capture.IsOpened();
        }
        catch (Exception ex)
        {
            throw new Exception("カメラ接続に失敗", ex);
        }

        return (result, null);
    }

    public (Mat image, Exception? err) GrabImage()
    {
        Mat image = new();

        if (this.Capture == null)
        {
            return (image, new Exception("Webカメラを Open していないのに撮像しようとしています。"));
        }

        try
        {
            if (this.Capture.Read(image) && !image.Empty())
            {
                return (image, null);
            }
            else
            {
                return (image, new Exception("撮像に失敗しました"));
            }
        }
        catch (Exception ex)
        {
            return (image, ex);
        }
    }

    public Exception? Close()
    {
        if (Capture != null && Capture.IsOpened())
        {
            Capture.Release();
            Capture.Dispose();
            return null;
        }
        else
        {
            return new Exception("Webカメラを Close しようとしていますが、Open されていません。");
        }
    }

}