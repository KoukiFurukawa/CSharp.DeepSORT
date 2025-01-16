using OpenCvSharp;

public class InspectionHistory
{
    public List<int> ClassIds { get; set; }
    public List<double> Scores { get; set; }
    public List<Rect> Bboxes { get; set; }
    public Mat Frame;
    public DateTime Timestamp;
    public AlertLevel IsDanger;

    public enum AlertLevel
    {
        NEUTRAL,
        CAUTION,
        DANGER,
    }

    private readonly int CAUTION = 200;
    private readonly int DANGER = 10;

    public InspectionHistory(List<Rect> bboxes, List<float> scores, List<int> classIds, Mat image)
    {
        this.ClassIds = classIds;
        this.Bboxes = bboxes;
        this.Frame = image;
        this.Timestamp = DateTime.Now;
        List<double> doubles = [];
        for (int i = 0; i < scores.Count; i++)
        {
            doubles.Add(scores[i]);
        }
        this.Scores = doubles;
    }

    public void CalculateMinimumBboxesRange()
    {
        List<int> humanIndexes = [];
        for (int i = 0; i < Bboxes.Count; i++)
        {
            if (ClassIds[i] == 0) { humanIndexes.Add(i); }
        }

        double minRange = double.MaxValue;

        for (int j = 0; j < humanIndexes.Count; j++)
        {
            Rect humanBboxes = Bboxes[humanIndexes[j]];

            for (int k = 0; k < Bboxes.Count; k++) 
            {
                if (humanIndexes[j] == k) { continue; }

                Rect carBboxes = Bboxes[k];
                if (humanBboxes.IntersectsWith(carBboxes))
                {
                    this.IsDanger = AlertLevel.DANGER;
                    return;
                }

                int xDist = Math.Max(humanBboxes.Left - carBboxes.Right, carBboxes.Left - humanBboxes.Right);
                int yDist = Math.Max(humanBboxes.Top - carBboxes.Bottom, carBboxes.Top - humanBboxes.Bottom);

                xDist = Math.Max(0, xDist);
                yDist = Math.Max(0, yDist);

                double tempRange = Math.Sqrt(xDist * xDist + yDist * yDist);
                minRange = tempRange < minRange ? tempRange : minRange;
            }
        }
        
        if (minRange < this.DANGER)
        {
            this.IsDanger = AlertLevel.DANGER;
        }
        else if (minRange < this.CAUTION)
        {
            this.IsDanger = AlertLevel.CAUTION;
        }
        else
        {
            this.IsDanger= AlertLevel.NEUTRAL;
        }

        return;
    }
}