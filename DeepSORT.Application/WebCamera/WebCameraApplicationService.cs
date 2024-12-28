using DeepSORT.Application.WebCameraUseCase.Create;
using DeepSORT.Domain.Models.WebCamera;

namespace DeepSORT.Application.WebCameraUseCase;
public class WebCameraUseCase
{
    private IWebCameraFactory WebCameraFactory { get; }
    public WebCameraUseCase(IWebCameraFactory webCameraFactory)
    {
        this.WebCameraFactory = webCameraFactory;
    }

    public WebCamera CreateDetector(WebCameraCreateCommand command)
    {
        WebCameraFps fps = new WebCameraFps(command.Fps);
        WebCamera webCamera =  this.WebCameraFactory.Create(fps);
        return webCamera;
    }
}
