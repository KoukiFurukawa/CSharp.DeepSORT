using DeepSORT.Application.DetectorUseCase.Create;
using DeepSORT.Domain.Models.Detector;

namespace DeepSORT.Application.DetectorUseCase;
public class DetectorUseCase
{
    private IDetectorFactory DetectorFactory { get; }
    public DetectorUseCase(IDetectorFactory detectorFactory)
    {
        this.DetectorFactory = detectorFactory;
    }

    public Detector Create(DetectorCreateCommand command)
    {
        ModelPath modelPath = new ModelPath(command.ModelPath);
        Detector detector = this.DetectorFactory.Create(modelPath);
        return detector;
    }
}