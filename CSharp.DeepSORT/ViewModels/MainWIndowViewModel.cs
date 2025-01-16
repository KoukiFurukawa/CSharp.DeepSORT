using Csharp.DeepSORT;
using CSharp.DeepSORT.Models;
using CsharpByteTrack.ByteTracker;
using CsharpByteTrack.ObjectDetection;
using DeepSORT.Application.DetectorUseCase;
using DeepSORT.Application.DetectorUseCase.Create;
using DeepSORT.Application.WebCameraUseCase;
using DeepSORT.Application.WebCameraUseCase.Create;
using DeepSORT.Domain.Models.Detector;
using Newtonsoft.Json;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.Net.Http;
using System.Text;

namespace CSharp.DeepSORT.ViewModels;
public class MainWindowViewModel
{
    public CameraImage WebCamera { get; private set; }
    private CancellationTokenSource _cancellationTokenSource = new();
    private object Lock = new();
    private Detector Detector { get; }
    private MultiClassByteTracker Tracker { get; }

    Dictionary<string, int> trackIdDict = new Dictionary<string, int>();

    private HttpClient HttpClient = new HttpClient
    {
        BaseAddress = new Uri("http://10.77.96.91:8000/")
    };
    private readonly string POST_ENDPOINT = "";
    private InspectionHistory.AlertLevel prevLevel = InspectionHistory.AlertLevel.NEUTRAL;

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

        _ = Task.Run(StartCaptureImageAsync);
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
                    inspectionHistory.CalculateMinimumBboxesRange();

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

                    try
                    {
                        if (frame.IsDanger != this.prevLevel)
                        {
                            var person = new Alert(frame.IsDanger);
                            var json = JsonConvert.SerializeObject(person);
                            this.prevLevel = frame.IsDanger;

                            // POSTリクエストを作成
                            var request = new HttpRequestMessage(HttpMethod.Post, this.POST_ENDPOINT)
                            {
                                // Content-typeを明示的に指定
                                Content = new StringContent(json, Encoding.UTF8, @"application/json")
                            };

                            _ = Task.Run(() =>
                            {
                                this.HttpClient.SendAsync(request);
                            });
                        }
                    }
                    catch (Exception ex) { throw new Exception(ex.Message); }

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

        this.HttpClient.Dispose();
        Debug.WriteLine("HttpClient-Dispose");
    }
}

[JsonObject]
public class Alert
{
    [JsonProperty("level")]
    public int Level { get; private set; }

    public Alert(InspectionHistory.AlertLevel level)
    {
        if (level == InspectionHistory.AlertLevel.DANGER)
        {
            this.Level = 2;
        }
        else if (level == InspectionHistory.AlertLevel.CAUTION)
        {
            this.Level = 1;
        }
        else if (level == InspectionHistory.AlertLevel.NEUTRAL)
        {
            this.Level = 0;
        }
    }
}