using System.Drawing;
using OpenCvSharp;
using OpenCvSharp.Dnn;

namespace CsharpByteTrack.ByteTracker;

public class MultiClassByteTracker
{
    public int Fps { get; set; }
    public double TrackThresh { get; set; }
    public int TrackBuffer { get; set; }
    public double MatchThresh { get; set; }
    public int MinBoxArea { get; set; }
    public bool MOT20 { get; set; }
    public Dictionary<int, ByteTracker> TrackerDict = [];

    public MultiClassByteTracker
    (
        int fps, double trackThresh = 0.5, int trackBuffer = 30,
        double matchThresh = 0.8, int minBoxArea = 10, bool mot20 = false
    )
    {
        this.Fps = fps;
        this.TrackThresh = trackThresh;
        this.TrackBuffer = trackBuffer;
        this.MatchThresh = matchThresh;
        this.MinBoxArea = minBoxArea;
        this.MOT20 = mot20;
    }

    public (List<string>, List<Rect>, List<double>, List<int>) Invoke
    (
        Bitmap image,
        List<Rect> boundaryBoxes,
        List<double> scores,
        List<int> classIds
    )
    {
        List<string> trackIds = [];
        List<Rect> trackBoundaryBoxes = [];
        List<double> trackScores = [];
        List<int> trackClassIds = [];

        foreach (int classId in classIds)
        {
            if (!this.TrackerDict.ContainsKey(classId))
            {
                ByteTrackerArgs args = new()
                {
                    TrackBuffer = this.TrackBuffer,
                    TrackThresh = this.TrackThresh,
                    MatchThresh = this.MatchThresh,
                    MOT20 = this.MOT20
                };
                ByteTracker _ByteTracker = new ByteTracker
                (
                    args, this.Fps
                );
                this.TrackerDict.Add(classId, _ByteTracker);
            }
        }

        foreach (int classId in this.TrackerDict.Keys)
        {
            var targetIndices = classIds.Select((id, index) => id == classId ? index : -1)
                                        .Where(index => index != -1)
                                        .ToList();
            if (targetIndices.Count == 0) { continue; }

            var targetBoundaryBoxes = targetIndices.Select(i => boundaryBoxes[i]).ToList();
            var targetScores = targetIndices.Select(i => scores[i]).ToList();

            double[][] detections = targetBoundaryBoxes.Select((boundaryBox, i) => new double[]
            {
                boundaryBox.Left, boundaryBox.Top, boundaryBox.Right, boundaryBox.Bottom,
                targetScores[i], classId
            }).ToArray();

            var result = UpdateTracker(classId, image, detections);

            foreach (var (bbox, score, id) in result)
            {
                trackIds.Add($"{classId}_{id}");
                trackBoundaryBoxes.Add(bbox);
                trackScores.Add(score);
                trackClassIds.Add(classId);
            }
        }

        return (trackIds, trackBoundaryBoxes, trackScores, trackClassIds);
    }


    private List<(Rect boundaryBox, double score, int id)> UpdateTracker
    (
        int classId, Bitmap image, double[][] detections)
    {
        var imageInfo = new
        {
            image.Width,
            image.Height
        };

        double[,] shapedDetections = ConvertToMultidimensional(detections);

        var onlineTargets = this.TrackerDict[classId].Update
        (
            shapedDetections,
            new[] { imageInfo.Height, imageInfo.Width },
            new[] { imageInfo.Height, imageInfo.Width }
        );

        List<(Rect boundaryBox, double score, int id)> result = onlineTargets
            .Where(target => target.Tlwh[2] * target.Tlwh[3] > MinBoxArea)
            .Select(target => (target.CreateRectangle(), target.Score, target.TrackId))
            .ToList();

        if (result.Count > 1)
        {
            //throw new Exception("2個以上検出してるよ");
        }

        return result;
    }

    private static double[,] ConvertToMultidimensional(double[][] jaggedArray)
    {
        int rows = jaggedArray.Length;
        int cols = jaggedArray[0].Length; // 仮定: 全ての行の長さが同じ
        double[,] result = new double[rows, cols];

        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                result[i, j] = jaggedArray[i][j];
            }
        }

        return result;
    }
}
