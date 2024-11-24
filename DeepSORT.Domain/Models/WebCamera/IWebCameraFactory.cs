namespace DeepSORT.Domain.Models.WebCamera;
public interface IWebCameraFactory
{
    WebCamera Create
    (
        WebCameraFps fps
    );
}