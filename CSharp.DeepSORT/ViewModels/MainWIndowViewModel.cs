using Csharp.DeepSORT;
using CSharp.DeepSORT.Models;
using CsharpByteTrack.ByteTracker;
using CsharpByteTrack.ObjectDetection;
using DeepSORT.Application.DetectorUseCase;
using DeepSORT.Application.DetectorUseCase.Create;
using DeepSORT.Application.WebCameraUseCase;
using DeepSORT.Application.WebCameraUseCase.Create;
using DeepSORT.Domain.Models.Detector;
using DeepSORT.Domain.Models.Predictor;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;

namespace CSharp.DeepSORT.ViewModels;
public class MainWindowViewModel
{
    public CameraImage WebCamera { get; private set; }
    private CancellationTokenSource _cancellationTokenSource = new();
    private object Lock = new();
    private Detector Detector { get; }
    private MultiClassByteTracker Tracker { get; }

    Dictionary<string, int> trackIdDict = new Dictionary<string, int>();

    private readonly int FPS = 15;
    private readonly double scoreTh = 0.3;
    private readonly double trackThresh = 0.5;
    private readonly int trackBuffer = 30;
    private readonly double matchThresh = 0.7;
    private readonly int minBoxArea = 100;
    private readonly bool mot20 = true;
    // private Predictor _predictor { get; }

    public MainWindowViewModel(DetectorUseCase detectorUseCase, WebCameraUseCase webCameraUseCase)
    {
        this.WebCamera = new CameraImage(null, "web");

        WebCameraCreateCommand webCameraCreateCommand = new(this.FPS);
        DetectorCreateCommand detectorCreateCommand = new(AppContext.BaseDirectory + "./models/yolo11n.onnx");

        var Camera = webCameraUseCase.Create(webCameraCreateCommand);
        this.Detector = detectorUseCase.Create(detectorCreateCommand);
        this.Tracker = new MultiClassByteTracker(FPS, trackThresh, trackBuffer, matchThresh, minBoxArea, mot20);

        /*        ModelPath predictorModelPath = new ModelPath(AppContext.BaseDirectory + "./models/resnet18_conv5.onnx");
                this._predictor = new(predictorModelPath);*/

        Task.Run(StartCaptureImageAsync);
    }

    private async Task StartCaptureImageAsync()
    {
        BlockingCollection<Mat> frameBuffer = new(boundedCapacity: 10); // バッファに最大10フレーム
        BlockingCollection<InspectionHistory> inspectionBuffer = new(boundedCapacity: 10); // バッファに最大10フレーム
        BlockingCollection<Bitmap> showBuffer = new(boundedCapacity: 10); // バッファに最大10フレーム

        // 画像取得専用タスク -----------------------------------------------------------------------------------------------------
        _ = Task.Run(async () =>
        {
            VideoCapture videoCapture = new(0);
            if (!videoCapture.IsOpened())
            {
                Debug.WriteLine("カメラが開けませんでした");
                return;
            }

            await Task.Delay(1000);
            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                Mat image = new();
                try
                {
                    if (videoCapture.Read(image) && !image.Empty())
                    {
                        // Bitmap frame = BitmapConverter.ToBitmap(image);  // OpenCV -> Bitmap
                        Mat frame = image.Clone();
                        frameBuffer.Add(frame);  // バッファに追加
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
                finally
                {
                    image.Dispose();
                }
                await Task.Delay(100);  // 適宜待機時間を挿入
            }
            videoCapture.Release();
        });

        // 物体検出 --------------------------------------------------------------------------------------------------------
        _ = Task.Run(async () =>
        {
            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                if (frameBuffer.Count == 0)
                {
                    await Task.Delay(1);
                    continue;
                }
                try
                {
                    Mat frame = frameBuffer.Take();
                    var (boxes, scores, classIds) = this.Detector.Inference(frame);
                    InspectionHistory inspectionHistory = new(boxes, scores, classIds, frame);
                    inspectionBuffer.Add(inspectionHistory);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
                await Task.Delay(1);  // 適宜待機時間を挿入
            }
        });

        // 物体追跡 --------------------------------------------------------------------------------------------------------
        _ = Task.Run(async () =>
        {
            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                if (inspectionBuffer.Count == 0)
                {
                    await Task.Delay(1);
                    continue;
                }
                try
                {
                    InspectionHistory frame = inspectionBuffer.Take();
                    Bitmap image = BitmapConverter.ToBitmap(frame.Frame);
                    var (tIds, tBboxes, tScores, tClassIds) = this.Tracker.Invoke(image, frame.Bboxes, frame.Scores, frame.ClassIds);

                    // トラッキングIDと連番の紐付け
                    foreach (string trackerId in tIds)
                    {
                        if (!this.trackIdDict.ContainsKey(trackerId))
                        {
                            int newId = this.trackIdDict.Count;
                            this.trackIdDict[trackerId] = newId;
                        }
                    }

                    Mat debugImage = PredictionDrawer.DrawPrediction(
                        frame.Frame, scoreTh, tIds, tBboxes, tScores, tClassIds, trackIdDict
                    );
                    showBuffer.Add(BitmapConverter.ToBitmap(debugImage));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
                await Task.Delay(1);  // 適宜待機時間を挿入
            }
        });

        // UI 更新専用タスク (バッファの内容を利用)
        await Task.Run(async () =>
        {
            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                Bitmap? frame;
                if (showBuffer.Count == 0)
                {
                    await Task.Delay(1);
                    continue;
                }
                if (showBuffer.TryTake(out frame, 100))  // バッファからフレーム取得
                {
                    await App.Current.Dispatcher.InvokeAsync(() =>
                    {
                        this.WebCamera.UpdateImage(frame);  // WPFの画像更新
                        frame.Dispose();
                    });
                }
                await Task.Delay(1);
            }
        });
    }
}