using DeepSORT.Domain.Models.Detector;

namespace DeepSORT.Application.DetectorUseCase;
public class DetectorUseCase
{
    private IDetectorFactory DetectorFactory { get; }
    public DetectorUseCase(IDetectorFactory detectorFactory)
    {
        this.DetectorFactory = detectorFactory;
    }

    public Detector CreateDetector(ModelPath modelPath)
    {
        return this.DetectorFactory.Create(modelPath);
    }
}
