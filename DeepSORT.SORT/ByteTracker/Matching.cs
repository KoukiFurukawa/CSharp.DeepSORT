using MathNet.Numerics.LinearAlgebra;
using Accord.Math;
using Accord.Math.Optimization;
using OpenCvSharp;

namespace CsharpByteTrack.ByteTracker;
public static class Matching
{
    public static (List<(int, int)> matches, List<int> unmatchedO, List<int> unmatchedQ) MergeMatches
    (
        (int, int)[] m1,
        (int, int)[] m2,
        (int O, int P, int Q) shape
    )
    {
        var (O, P, Q) = shape;

        var M1 = Matrix<double>.Build.Sparse(O, P, 0.0);
        foreach (var (i, j) in m1)
            M1[i, j] = 1;

        var M2 = Matrix<double>.Build.Sparse(P, Q, 0.0);
        foreach (var (j, k) in m2)
            M2[j, k] = 1;

        var mask = M1 * M2;
        var matches = new List<(int, int)>();

        foreach (var (i, j, _) in mask.EnumerateIndexed(Zeros.AllowSkip))
            matches.Add((i, j));

        var unmatchedO = Enumerable.Range(0, O).Except(matches.Select(m => m.Item1)).ToList();
        var unmatchedQ = Enumerable.Range(0, Q).Except(matches.Select(m => m.Item2)).ToList();

        return (matches, unmatchedO, unmatchedQ);
    }

    public static (List<(int, int)> matches, List<int> unmatchedA, List<int> unmatchedB) IndicesToMatches
    (
        double[,] costMatrix, (int, int)[] indices, double thresh
    )
    {
        var matches = new List<(int, int)>();
        var unmatchedA = new List<int>();
        var unmatchedB = new List<int>();

        foreach (var (i, j) in indices)
        {
            if (costMatrix[i, j] <= thresh)
                matches.Add((i, j));
        }

        unmatchedA = Enumerable.Range(0, costMatrix.GetLength(0))
            .Except(matches.Select(m => m.Item1)).ToList();
        unmatchedB = Enumerable.Range(0, costMatrix.GetLength(1))
            .Except(matches.Select(m => m.Item2)).ToList();

        return (matches, unmatchedA, unmatchedB);
    }

    public static (List<(int, int)> matches, List<int> unmatchedA, List<int> unmatchedB) LinearAssignment
    (
        double[,] costMatrix, double thresh
    )
    {
        if (costMatrix.Length == 0)
        {
            return (new List<(int, int)>(), Enumerable.Range(0, costMatrix.GetLength(0)).ToList(),
                Enumerable.Range(0, costMatrix.GetLength(1)).ToList());
        }

        // 使用するlapjv関数の実装が必要。Accordなどを検討
        var (matches, unmatchedA, unmatchedB) = SolveLapJV(costMatrix, thresh);

        return (matches, unmatchedA, unmatchedB);
    }

    public static double[,] IoUs(Vector<double>[] atlbrs, Vector<double>[] btlbrs)
    {
        int rows = atlbrs.Length;
        int cols = btlbrs.Length;
        double[,] ious = new double[rows, cols];

        if (rows == 0 || cols == 0)
            return ious;

        Rect[] convertedAtlbrs = atlbrs.Select(atlbr =>
        {
            if (atlbr.Count != 4)
                throw new ArgumentException("Each vector must contain exactly 4 elements: [x1, y1, x2, y2].");

            // (x1, y1) = 左上の座標, (x2, y2) = 右下の座標
            int x = (int)atlbr[0];
            int y = (int)atlbr[1];
            int width = (int)(atlbr[2] - atlbr[0]);  // x2 - x1
            int height = (int)(atlbr[3] - atlbr[1]); // y2 - y1

            return new Rect(x, y, width, height);
        }).ToArray();  // ここでToArray()を追加

        Rect[] convertedBtlbrs = (Rect[])btlbrs.Select(btlbr =>
        {
            if (btlbr.Count != 4)
                throw new ArgumentException("Each vector must contain exactly 4 elements: [x1, y1, x2, y2].");

            // (x1, y1) = 左上の座標, (x2, y2) = 右下の座標
            int x = (int)btlbr[0];
            int y = (int)btlbr[1];
            int width = (int)(btlbr[2] - btlbr[0]);  // x2 - x1
            int height = (int)(btlbr[3] - btlbr[1]); // y2 - y1

            return new Rect(x, y, width, height);
        }).ToArray();

        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                ious[i, j] = CalcIoU(convertedAtlbrs[i], convertedBtlbrs[j]);
                //ious[i, j] = IoU(convertedAtlbrs[i], convertedBtlbrs[j]);
            }
        }

        return ious;
    }

    public static double IoU(Rect boxA, Rect boxB)
    {
        int xA = Math.Max(boxA.Left, boxB.Left);
        int yA = Math.Max(boxA.Top, boxB.Top);
        int xB = Math.Min(boxA.Right, boxB.Right);
        int yB = Math.Min(boxA.Bottom, boxB.Bottom);

        // 重なり領域の面積を計算
        int interWidth = Math.Max(0, xB - xA);
        int interHeight = Math.Max(0, yB - yA);
        double interArea = interWidth * interHeight;

        // 各ボックスの面積を計算
        double boxAArea = boxA.Width * boxA.Height;
        double boxBArea = boxB.Width * boxB.Height;

        // IoU = intersection / (areaA + areaB - intersection)
        double iou = interArea / (boxAArea + boxBArea - interArea);

        if (iou < 0)
        {
            Console.WriteLine("IoU_Error");
        }

        return iou;
    }

    public static float CalcIoU(Rect a, Rect b)
    {
        // 重なり部分の幅と高さを計算
        int dx = Math.Min(a.Right, b.Right) - Math.Max(a.Left, b.Left);
        int dy = Math.Min(a.Bottom, b.Bottom) - Math.Max(a.Top, b.Top);

        // 重なり部分の面積
        float intersection = (dx < 0 || dy < 0) ? 0 : dx * dy;

        // 合計面積（IoUの分母）を計算
        float union = (a.Width * a.Height) + (b.Width * b.Height) - intersection;

        // IoUを計算
        return intersection / union;
    }


    public static double[,] IoUDistance(List<STrack> aTracks, List<STrack> bTracks)
    {
        Vector<double>[] atlbrs = aTracks.Select(t => t.Tlbr).ToArray();
        Vector<double>[] btlbrs = bTracks.Select(t => t.Tlbr).ToArray();

        var ious = IoUs(atlbrs, btlbrs);
        int rows = ious.GetLength(0);
        int cols = ious.GetLength(1);

        // コスト行列の生成（1 - IoU）
        var costMatrix = new double[rows, cols];
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                costMatrix[i, j] = 1.0 - ious[i, j];
            }
        }
        return costMatrix;
    }

    public static (List<(int, int)> matches, List<int> unmatchedA, List<int> unmatchedB) SolveLapJV
    (
        double[,] costMatrix, double thresh
    )
    {
        double[][] jaggedArray = ToJaggedArray(costMatrix);

        // ハンガリアン法を利用した割り当て問題の解決
        var solver = new Munkres(jaggedArray);

        bool success = solver.Minimize();

        if (!success) throw new Exception("ハンガリアン法を用いた割当問題が解決できませんでした。");

        double[] x = solver.Solution;  // x[i] = assigned column for row i

        List<(int, int)> matches = new();
        for (int i = 0; i < x.Length; i++)
        {
            if (x[i] >= 0 || x[i] < thresh)  // If row i was assigned a valid column
                matches.Add((i, (int)x[i]));
        }

        // Collect unmatched rows and columns
        var unmatchedA = Enumerable.Range(0, x.Length).Where(i => x[i] < 0 || x[i] >= thresh).ToList();
        var unmatchedB = Enumerable.Range(0, costMatrix.GetLength(1)).Where(j => !x.Contains(j)).ToList();

        return (matches, unmatchedA, unmatchedB);
    }

    public static double[,] FuseScore(double[,] costMatrix, List<STrack> detections)
    {
        // コスト行列のサイズを取得
        int rows = costMatrix.GetLength(0);
        int cols = costMatrix.GetLength(1);

        // costMatrix が空なら、そのまま返す
        if (rows == 0 || cols == 0)
            return costMatrix;

        // iou_sim = 1 - cost_matrix
        double[,] iouSim = new double[rows, cols];
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                iouSim[i, j] = 1 - costMatrix[i, j];
            }
        }

        // det_scores の拡張と複製
        double[] detScores = detections.Select(d => d.Score).ToArray();
        double[,] expandedScores = new double[rows, cols];
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                expandedScores[i, j] = detScores[j];
            }
        }

        // fuse_sim の計算
        double[,] fuseSim = new double[rows, cols];
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                fuseSim[i, j] = iouSim[i, j] * expandedScores[i, j];
            }
        }

        // fuse_cost = 1 - fuse_sim
        double[,] fuseCost = new double[rows, cols];
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                fuseCost[i, j] = 1 - fuseSim[i, j];
            }
        }

        return fuseCost;
    }

    public static double[][] ToJaggedArray(double[,] matrix)
    {
        int rows = matrix.GetLength(0);
        int cols = matrix.GetLength(1);
        var jagged = new double[rows][];

        for (int i = 0; i < rows; i++)
        {
            int arraySize = cols;
            if (cols < rows)
            {
                arraySize = rows;
            }
            jagged[i] = new double[arraySize];
            for (int j = 0; j < cols; j++)
            {
                jagged[i][j] = matrix[i, j];
            }
            for (int j = cols; j < arraySize; j++)
            {
                jagged[i][j] = double.PositiveInfinity;
            }
            
        }
        return jagged;
    }
}