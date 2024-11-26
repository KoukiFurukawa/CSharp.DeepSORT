using DeepSORT.Domain.Models.WebCamera;

namespace DeepSORT.Infrastructure.Factory;
public class WebCameraFactory : IWebCameraFactory
{
    public WebCamera Create(WebCameraFps fps)
    {
        WebCamera webCamera = new(fps);
        return webCamera;
    }
}