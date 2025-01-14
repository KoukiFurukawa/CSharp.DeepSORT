using OpenCvSharp;

public class InspectionHistory
{
    public List<int> ClassIds { get; set; }
    public List<double> Scores { get; set; }
    public List<Rect> Bboxes { get; set; }
    public Mat Frame;
    public DateTime Timestamp;

    public InspectionHistory(List<Rect> bboxes, List<float> scores, List<int> classIds, Mat image)
    {
        this.ClassIds = classIds;
        this.Bboxes = bboxes;
        this.Frame = image;
        this.Timestamp = DateTime.Now;
        List<double> doubles = new List<double>();
        for (int i = 0; i < scores.Count; i++)
        {
            doubles.Add(scores[i]);
        }
        this.Scores = doubles;
    }
}