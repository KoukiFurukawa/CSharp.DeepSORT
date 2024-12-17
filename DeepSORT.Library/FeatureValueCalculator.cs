// using System;
// using System.Collections.Generic;
// using System.Drawing;
// using System.Linq;
// using Microsoft.ML;
// using Microsoft.ML.OnnxRuntime;
// using Microsoft.ML.OnnxRuntime.Tensors;
// using OpenCvSharp;

// namespace Library
// {
//     public static class FeatureValueCalculator
//     {
//         public static double CalculateFeatureValue(Mat image, Rect[] boxes)
//         {
//             MLContext mlContext = new();

//             // AlexNet モデルを使用したパイプラインの構築
//             var pipeline = mlContext.Transforms
//                 .ResizeImages(outputColumnName: "input", imageWidth: 227, imageHeight: 227) // AlexNet の入力サイズ
//                 .Append(mlContext.Transforms.ExtractPixels(outputColumnName: "input"))
//                 .Append(mlContext.Model.LoadDnnImageFeaturizerModel(modelFactory: AlexNetExtension.AlexNet, inputColumnName: "input"));

//             var transform = new ImageTransform();
//             List<float[]> featureList = new();

//             foreach (var box in boxes)
//             {
//                 // クロップした画像の取得
//                 Mat croppedImg = new Mat(image, box);
//                 Bitmap imgBitmap = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(croppedImg);

//                 // 画像の前処理と特徴量の抽出
//                 var imgTensor = transform.Apply(imgBitmap);
//                 var features = ExtractFeatures(imgTensor);
//                 featureList.Add(features.ToArray());
//             }

//             // 特徴量の平均値を計算して返す
//             var averageFeatures = featureList.SelectMany(f => f).Average();
//             return averageFeatures;
//         }

//         static DenseTensor<float> ExtractFeatures(Tensor<float> imgTensor)
//         {
//             // ONNXモデルのロード
//             using var session = new InferenceSession("path_to_model.onnx");

//             // 入力テンソルの作成
//             var input = new List<NamedOnnxValue>
//             {
//                 NamedOnnxValue.CreateFromTensor("input", imgTensor)
//             };

//             // 推論の実行
//             using IDisposableReadOnlyCollection<DisposableNamedOnnxValue> results = session.Run(input);

//             // 特徴量の抽出
//             var output = results.First().AsTensor<float>();
//             return output;
//         }
//     }

//     class ImageTransform
//     {
//         public Tensor<float> Apply(Bitmap img)
//         {
//             // リサイズ、正規化、およびテンソルへの変換
//             Bitmap resizedImg = new Bitmap(img, new Size(227, 227));
//             float[] imgData = new float[3 * 227 * 227];

//             for (int y = 0; y < 227; y++)
//             {
//                 for (int x = 0; x < 227; x++)
//                 {
//                     Color pixel = resizedImg.GetPixel(x, y);
//                     imgData[(0 * 227 + y) * 227 + x] = (pixel.R / 255.0f - 0.485f) / 0.229f;
//                     imgData[(1 * 227 + y) * 227 + x] = (pixel.G / 255.0f - 0.456f) / 0.224f;
//                     imgData[(2 * 227 + y) * 227 + x] = (pixel.B / 255.0f - 0.406f) / 0.225f;
//                 }
//             }

//             var tensor = new DenseTensor<float>(imgData, new[] { 1, 3, 227, 227 });
//             return tensor;
//         }
//     }
// }