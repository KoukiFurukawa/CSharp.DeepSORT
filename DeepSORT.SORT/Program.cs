using CsharpByteTrack.ByteTracker;
using CsharpByteTrack.ObjectDetection;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System.Collections.Generic;
using System.Drawing;

/*
1. 物体検出
    1-1. ONNX ファイルを利用して、物体検出を行う
    1-2. 検出結果は、3 つのリストで返される
    1-3. 座標情報、スコア、物体のクラス
2. 物体追跡
    2-1. MultiClassByteTrackerを利用して物体検出を行う
    2-2. 物体検出毎にトラッカーを起動
    2-3. トラッカーは Class 毎に生成される（今回だと Ps と Fl ）
    2-4. クラス毎の検出結果をトラッカーに与えてトラッキングを行う
    2-5. その結果を基に画面描写
*/

namespace CsharpByteTrack;
class Program
{
    static void _Main(string[] args)
    {
        // 設定
        string target = "webcam";  // "video" or "webcam"
        bool ByteTrackProcess = true;  // ByteTrackの描画の有無
        string videoFile = "20240909_082654_5DE5_corrected";
        string inputPath = $"./videos/input/{videoFile}.mp4";
        string outputPath = $"./videos/output/processed_{videoFile}.mp4";
        string modelPath = "./model/object_detection_model.onnx";
        float displayScale = 1f;  // ウィンドウ表示倍率

        // YOLOX パラメータ
        double scoreTh = 0.3;

        // ByteTrack パラメータ
        double trackThresh = 0.5;
        int trackBuffer = 30;
        double matchThresh = 0.7;
        int minBoxArea = 100;
        bool mot20 = true;

        // 動画キャプチャの設定
        VideoCapture cap;
        if (target == "video")
        {
            cap = new VideoCapture(inputPath);
            if (!cap.IsOpened())
            {
                Console.WriteLine("指定された動画が開けませんでした。");
                return;
            }
        }
        else
        {
            cap = new VideoCapture(0);
        }

        // カメラまたは動画のプロパティ取得
        int capWidth = (int)cap.Get(VideoCaptureProperties.FrameWidth);
        int capHeight = (int)cap.Get(VideoCaptureProperties.FrameHeight);
        double capFps = cap.Get(VideoCaptureProperties.Fps);

        // モデルロード
        YoloxObjectDetector detector = new YoloxObjectDetector(modelPath);

        // ByteTrack インスタンス生成
        MultiClassByteTracker tracker = new MultiClassByteTracker(
            (int)capFps, trackThresh, trackBuffer, matchThresh, minBoxArea, mot20
        );

        // 出力用のVideoWriter（動画の場合のみ）
        VideoWriter? writer = null;
        if (target == "video")
        {
            writer = new VideoWriter(outputPath, FourCC.XVID, capFps, new OpenCvSharp.Size(capWidth, capHeight));
        }

        // トラッキングIDを保持する辞書
        Dictionary<string, int> trackIdDict = new Dictionary<string, int>();

        // メインループ
        while (true)
        {
            // フレーム読込み
            Mat frame = new Mat();
            bool ret = cap.Read(frame);
            if (!ret) break;

            // フレームのコピー
            Mat debugImage = frame.Clone();

            // 物体検出
            var (bboxes, scores, classIds) = detector.Inference(frame);

            // 物体追跡
            var (tIds, tBboxes, tScores, tClassIds) = tracker.Invoke
            (
                frame.ToBitmap(), bboxes, scores, classIds
            );

            // トラッキングIDと連番の紐付け
            foreach (string trackerId in tIds)
            {
                if (!trackIdDict.ContainsKey(trackerId))
                {
                    int newId = trackIdDict.Count;
                    trackIdDict[trackerId] = newId;
                }
            }

            // デバッグ描画
            if (ByteTrackProcess == true)
            {
                debugImage = PredictionDrawer.DrawPrediction(
                    debugImage, scoreTh, tIds, tBboxes, tScores, tClassIds, trackIdDict
                );
            }
            else
            {
                debugImage = PredictionDrawer2.DrawPrediction(
                    debugImage, scoreTh, bboxes, scores, classIds
                );
            }

            // 表示倍率適用
            Mat resizedImage = new Mat();
            Cv2.Resize(debugImage, resizedImage, new OpenCvSharp.Size(), displayScale, displayScale);

            // ウィンドウに表示
            Cv2.ImShow("YOLOX ByteTrack (Multi Class)", resizedImage);

            // 動画ファイルに書き込み
            if (target == "video" && writer != null)
            {
                writer.Write(debugImage);
            }

            // ESCキーで終了
            int key = Cv2.WaitKey(1);
            if (key == 27) break;
        }

        // リソース解放
        cap.Release();
        writer?.Release();
        Cv2.DestroyAllWindows();
    }
}