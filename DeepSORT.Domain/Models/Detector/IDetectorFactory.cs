namespace DeepSORT.Domain.Models.Detector;
public interface IDetectorFactory
{
    Detector Create
    (
        ModelPath modelPath
    );
}