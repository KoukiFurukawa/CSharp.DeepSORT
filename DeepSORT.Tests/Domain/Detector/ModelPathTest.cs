using DeepSORT.Domain.Models.Detector;
using System.Text;

namespace DeepSORT.Test.Domain.DetectorTest
{
    public class ModelPathTest
    {
        [Fact]
        public void Constructor_Should_ThrowException_When_ModelPathIsInvalid()
        {
            string path_1 = new StringBuilder().Insert(0, "hogefuga", 100).ToString(); // 形式が異なる
            string path_2 = ""; // 空文字
            string path_3 = null; // null
            string path_4 = AppContext.BaseDirectory + "./models/yolov8n.onnx"; // 存在しないPATH
            string path_5 = AppContext.BaseDirectory + "./models/yolo11n.pt"; // onnx ファイルじゃない
            string path_6 = AppContext.BaseDirectory + "./models/yolo11n.onnx"; // onnx ファイルじゃない

            // 失敗
            Assert.Throws<ArgumentException>(() => new ModelPath(path_1));
            Assert.Throws<ArgumentException>(() => new ModelPath(path_2));
            Assert.Throws<ArgumentNullException>(() => new ModelPath(path_3));
            Assert.Throws<ArgumentException>(() => new ModelPath(path_4));
            Assert.Throws<ArgumentException>(() => new ModelPath(path_5));

            // 成功
            ModelPath ModelPathId_1 = new(path_6);
            Assert.NotNull(ModelPathId_1);
        }

    }
}