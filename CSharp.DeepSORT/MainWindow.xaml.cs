using CSharp.DeepSORT;
using CSharp.DeepSORT.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace Csharp.DeepSORT
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        public static IServiceProvider? ServiceProvider { get; private set; }
        private MainWindowViewModel context;
        /*        private readonly ModelPath _mockModelPath;
                private readonly string IMAGE_PATH = AppContext.BaseDirectory + "./images/zidane.jpg";
                private readonly string IMAGE_PATH2 = AppContext.BaseDirectory + "./images/dog.jpeg";*/
        public MainWindow()
        {
            ServiceProvider = PrepareServiceCollection.Initialize();
            this.context = ServiceProvider.GetRequiredService<MainWindowViewModel>();
            this.DataContext = this.context;
            InitializeComponent();
            /*            string modelPath = AppContext.BaseDirectory + "./models/yolo11n.onnx";
                        *//*string modelPath2 = AppContext.BaseDirectory + "./models/resnet18-v1-7.onnx";*//*
                        string modelPath2 = AppContext.BaseDirectory + "./models/resnet18_conv5.onnx";
                        this._mockModelPath = new ModelPath(modelPath);

                        ModelPath modelPathObj = new ModelPath(modelPath2);
                        ImagePath imagePathObj = new ImagePath(IMAGE_PATH2);
                        Detector detector = new(this._mockModelPath);
                        Predictor predictor = new(modelPathObj, imagePathObj);
                        Mat image = Cv2.ImRead(IMAGE_PATH);
                        var (bboxes, scores, classIds) = detector.Inference(image);
                        Debug.WriteLine(bboxes.Count);

                        try
                        {
                            var (label, features) = predictor.PredictAndExtractFeatures();
                            Debug.WriteLine(label);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex.Message);
                        }*/
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {

            if (MessageBoxResult.Yes != MessageBox.Show("画面を閉じます。よろしいですか？", "確認", MessageBoxButton.YesNo, MessageBoxImage.Information))
            {
                e.Cancel = true;
                return;
            }
            else
            {
                this.context.Close();
            }
        }
    }
}