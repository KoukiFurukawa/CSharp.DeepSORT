using DeepSORT.Domain.Models.Detector;
using DeepSORT.Domain.Models.Predictor;
using OpenCvSharp;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Csharp.DeepSORT
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        private readonly ModelPath _mockModelPath;
        private readonly string IMAGE_PATH = AppContext.BaseDirectory + "./images/zidane.jpg";
        private readonly string IMAGE_PATH2 = AppContext.BaseDirectory + "./images/dog.jpeg";
        public MainWindow()
        {
            InitializeComponent();
            string modelPath = AppContext.BaseDirectory + "./models/yolo11n.onnx";
            string modelPath2 = AppContext.BaseDirectory + "./models/resnet18-v1-7.onnx";
            this._mockModelPath = new ModelPath(modelPath);
            Detector detector = new(this._mockModelPath);
            Predictor predictor = new(modelPath2, IMAGE_PATH);
            Mat image = Cv2.ImRead(IMAGE_PATH);
            var (bboxes, scores, classIds) = detector.Inference(image);
            Debug.WriteLine(bboxes.Count);

            var (label, features) = predictor.PredictAndExtractFeatures();
            Debug.WriteLine(label);

        }
    }
}