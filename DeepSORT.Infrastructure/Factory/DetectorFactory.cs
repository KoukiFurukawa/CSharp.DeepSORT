using DeepSORT.Domain.Models.Detector;

namespace DeepSORT.Infrastructure.Factory;
public class DetectorFactory : IDetectorFactory
{
    public Detector Create(ModelPath modelPath)
    {
        Detector detector = new Detector(modelPath);
        return detector;
    }
}