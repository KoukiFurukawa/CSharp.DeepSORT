namespace DeepSORT.Application.WebCameraUseCase.Create
{
    public class WebCameraCreateCommand
    {
        public int Fps { get; }

        public WebCameraCreateCommand(int fps)
        {
            Fps = fps;
        }
    }
}