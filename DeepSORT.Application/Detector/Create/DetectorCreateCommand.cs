using SixLabors.ImageSharp.Formats.Pbm;

namespace DeepSORT.Application.DetectorUseCase.Create
{
    public class DetectorCreateCommand
    {
        public string ModelPath { get; }

        public DetectorCreateCommand(string modelPath)
        {
            ModelPath = modelPath;
        }
    }
}