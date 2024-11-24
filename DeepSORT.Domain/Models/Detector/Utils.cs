using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeepSORT.Domain.Models.Detector;
public static class Utils
{
    public static List<int> Nms(List<Rect> boxes, List<float> scores, double iouThreshold)
    {
        var sortedIndices = scores
            .Select((score, index) => new { score, index })
            .OrderByDescending(x => x.score)
            .Select(x => x.index)
            .ToList();

        var keepBoxes = new List<int>();

        while (sortedIndices.Count > 0)
        {
            var boxId = sortedIndices[0];
            keepBoxes.Add(boxId);

            var ious = ComputeIou(boxes[boxId], sortedIndices.Skip(1).Select(i => boxes[i]).ToList());

            sortedIndices = sortedIndices.Skip(1)
                .Where((t, i) => ious[i] < iouThreshold)
                .ToList();
        }

        return keepBoxes;
    }

    public static List<int> MulticlassNms(List<Rect> boxes, List<float> scores, List<int> classIds, double iouThreshold)
    {
        var uniqueClassIds = classIds.Distinct().ToList();
        var keepBoxes = new List<int>();

        foreach (var classId in uniqueClassIds)
        {
            var classIndices = classIds
                .Select((id, index) => new { id, index })
                .Where(x => x.id == classId)
                .Select(x => x.index)
                .ToList();

            var classBoxes = classIndices.Select(index => boxes[index]).ToList();
            var classScores = classIndices.Select(index => scores[index]).ToList();

            var classKeepBoxes = Nms(classBoxes, classScores, iouThreshold);
            keepBoxes.AddRange(classKeepBoxes.Select(index => classIndices[index]));
        }

        return keepBoxes;
    }


    private static List<float> ComputeIou(Rect box, List<Rect> boxes)
    {
        var ious = new List<float>();

        foreach (var otherBox in boxes)
        {
/*            // Compute intersection
            var xMin = Math.Max(box.Left, otherBox.Left);
            var yMin = Math.Max(box.Top, otherBox.Top);
            var xMax = Math.Min(box.Right, otherBox.Right);
            var yMax = Math.Min(box.Bottom, otherBox.Bottom);

            var intersectionArea = Math.Max(0, xMax - xMin) * Math.Max(0, yMax - yMin);

            // Compute union
            var boxArea = box.Width * box.Height;
            var otherBoxArea = otherBox.Width * otherBox.Height;
            var unionArea = boxArea + otherBoxArea - intersectionArea;

            // Compute IoU
            var iou = intersectionArea / unionArea;*/


            // 重なり部分の幅と高さを計算
            int dx = Math.Min(box.Right, otherBox.Right) - Math.Max(box.Left, otherBox.Left);
            int dy = Math.Min(box.Bottom, otherBox.Bottom) - Math.Max(box.Top, otherBox.Top);

            // 重なり部分の面積
            float intersection = (dx < 0 || dy < 0) ? 0 : dx * dy;

            // 合計面積（IoUの分母）を計算
            float union = (box.Width * box.Height) + (otherBox.Width * otherBox.Height) - intersection;

            // IoUを計算
            var iou = intersection / union;
            ious.Add(iou);
        }

        return ious;
    }
}
