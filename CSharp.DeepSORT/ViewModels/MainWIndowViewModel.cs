using CSharp.DeepSORT.Models;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using Csharp.DeepSORT;

namespace CSharp.DeepSORT.ViewModels;
public class MainWindowViewModel
{
    public CameraImage WebCamera { get; private set; }
    private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
    private object _lock = new object();

    public MainWindowViewModel()
    {
        this.WebCamera = new CameraImage(null, "web");
        Task.Run(StartCaptureImageAsync);
    }

    private async Task StartCaptureImageAsync()
    {
        BlockingCollection<Bitmap> frameBuffer = new BlockingCollection<Bitmap>(boundedCapacity: 10); // バッファに最大10フレーム

        // 画像取得専用タスク
        _ = Task.Run(async () =>
        {
            VideoCapture videoCapture = new(0);
            if (!videoCapture.IsOpened())
            {
                Debug.WriteLine("カメラが開けませんでした");
                return;
            }
            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                Mat image = new();
                try
                {
                    if (videoCapture.Read(image) && !image.Empty())
                    {
                        Bitmap frame = BitmapConverter.ToBitmap(image);  // OpenCV -> Bitmap
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

        // UI 更新専用タスク (バッファの内容を利用)
        await Task.Run(async () =>
        {
            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                Bitmap? frame;
                if (frameBuffer.Count == 0) { continue; }
                if (frameBuffer.TryTake(out frame, 100))  // バッファからフレーム取得
                {
                    await App.Current.Dispatcher.InvokeAsync(() =>
                    {
                        this.WebCamera.UpdateImage(frame);  // WPFの画像更新
                        frame.Dispose();
                    });
                }
            }
        });
    }
}
