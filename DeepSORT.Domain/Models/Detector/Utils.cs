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
            /*// Compute intersection
            var xMin = Math.Max(box[0], otherBox[9]);
            var yMin = Math.Max(box[1], otherBox[1]);
            var xMax = Math.Min(box[2], otherBox[2]);
            var yMax = Math.Min(box[3], otherBox[3]);

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

    public static List<Rect> Xywh2Tlwh(float[,] boxes)
    {
        int numBoxes = boxes.GetLength(0);
        List<Rect> convertedBoxes = [];

        for (int i = 0; i < numBoxes; i++)
        {
            float x = boxes[i, 0];
            float y = boxes[i, 1];
            float w = boxes[i, 2];
            float h = boxes[i, 3];

            float top = y - h / 2;    // top
            float left = x - w / 2;   // left
            float width = w;          // width
            float height = h;         // height

            Rect rect = new Rect()
            {
                Top = (int)top,
                Left = (int)left,
                Width = (int)width,
                Height = (int)height,
            };
            convertedBoxes.Add(rect);
        }

        return convertedBoxes;
    }

    public static float[,] Xyxy2Xywh(float[,] boxes)
    {
        int numBoxes = boxes.GetLength(0);
        float[,] convertedBoxes = new float[numBoxes, 4];

        for (int i = 0; i < numBoxes; i++)
        {
            float x1 = boxes[i, 0];
            float y1 = boxes[i, 1];
            float x2 = boxes[i, 2];
            float y2 = boxes[i, 3];

            float x = (x1 + x2) / 2; // center x
            float y = (y1 + y2) / 2; // center y
            float w = x2 - x1;       // width
            float h = y2 - y1;       // height

            convertedBoxes[i, 0] = x;
            convertedBoxes[i, 1] = y;
            convertedBoxes[i, 2] = w;
            convertedBoxes[i, 3] = h;
        }

        return convertedBoxes;
    }
}
