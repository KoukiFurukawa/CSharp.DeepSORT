using OpenCvSharp;

namespace CsharpByteTrack.ObjectDetection;
public class PredictionDrawer
{
    public static Mat DrawPrediction
    (
        Mat image,
        double scoreTh,
        List<string> trackerIds,
        List<Rect> bboxes,
        List<double> scores,
        List<int> classIds,
        Dictionary<string, int> trackIdDict
    )
    {
        // イメージのコピーを作成
        Mat debugImage = image.Clone();

        for (int i = 0; i < trackerIds.Count; i++)
        {
            string trackerId = trackerIds[i];
            Rect bbox = bboxes[i];
            double score = scores[i];
            int classId = classIds[i];

            // スコアの確認
            if (scoreTh > score) continue;

            // クラスIDに応じたラベルと色の設定
            string label;
            Scalar color;
            if (classId == 0)
            {
                label = "Forklift";
                color = new Scalar(255, 0, 0); // 青
            }
            else if (classId == 1)
            {
                label = "Person";
                color = new Scalar(0, 0, 255); // 赤
            }
            else
            {
                label = "Unknown";
                color = new Scalar(0, 255, 0); // 緑（その他）
            }

            // バウンディングボックスの描画
            Cv2.Rectangle(debugImage, bbox, color, thickness: 2);

            // トラックIDとスコアの表示
            string scoreTxt = Math.Round(score, 2).ToString();
            string trackTxt = $"Track ID: {trackerId} ({scoreTxt})";
            Cv2.PutText(
                debugImage,
                trackTxt,
                new Point(bbox.Left, bbox.Top - 30),
                HersheyFonts.HersheySimplex,
                0.7,
                color,
                thickness: 2
            );

            // クラスラベルの表示
            string classTxt = $"Class: {label}";
            Cv2.PutText(
                debugImage,
                classTxt,
                new Point(bbox.Left, bbox.Top - 10),
                HersheyFonts.HersheySimplex,
                0.7,
                color,
                thickness: 2
            );
        }
        return debugImage;
    }
}
