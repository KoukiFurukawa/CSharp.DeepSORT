using DeepSORT.Domain.Models.Detector;
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
        public MainWindow()
        {
            InitializeComponent();
            string modelPath = AppContext.BaseDirectory + "./models/yolo11n.onnx";
            this._mockModelPath = new ModelPath(modelPath);
            Detector detector = new(this._mockModelPath);
            Mat image = Cv2.ImRead(IMAGE_PATH);
            var (bboxes, scores, classIds) = detector.Inference(image);
            Debug.WriteLine(bboxes.Count);
        }
    }
}