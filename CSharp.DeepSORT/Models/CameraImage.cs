using System.Drawing;
using System.Windows.Media.Imaging;
using System.IO;
using System.ComponentModel;

namespace CSharp.DeepSORT.Models;
public class CameraImage : INotifyPropertyChanged
{
    public CameraImage
    (
        Bitmap? image,
        string cameraName
    )
    {
        BitmapImage = image;
        CameraName = cameraName;
        if (image == null )
        {
            return;
        }
        _image = ToBitmapSource(image);
        _width = GetWidth(image);
        _height = GetHeight(image);
    }
    public Bitmap? BitmapImage { get; set; }
    public string CameraName { get; set; }
    private BitmapSource? _image;
    private int _width;
    private int _height;
    private object _lock = new object();

    public BitmapSource? Image
    {
        get => _image;
        set
        {
            _image = value;
            OnPropertyChanged(nameof(Image));
        }
    }

    public int Width
    {
        get => _width;
        set
        {
            _width = value;
            OnPropertyChanged(nameof(Width));
        }
    }

    public int Height
    {
        get => _height;
        set
        {
            _height = value;
            OnPropertyChanged(nameof(Height));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private int GetHeight(Bitmap bitmapImage)
    {
        if (bitmapImage == null)
        {
            throw new ArgumentNullException(nameof(bitmapImage));
        }
        return bitmapImage.Height; 
    }

    private int GetWidth(Bitmap bitmapImage)
    {
        if (bitmapImage == null)
        {
            throw new ArgumentNullException(nameof(bitmapImage));
        }
        return bitmapImage.Width; 
    }

    public void UpdateImage(Bitmap image)
    {
        this.Image = ToBitmapSource(image);
        // this.Width = GetWidth(image);
        // this.Height = GetHeight(image);
    }

    
    private BitmapSource? ToBitmapSource(Bitmap? image)
    {
        if (image == null)
        {
            return null;
        }

        using MemoryStream memory = new MemoryStream();

        // lock (_lock) // Bitmapのコピー操作をロック
        // {
        //     using (Bitmap clonedImage = (Bitmap)image.Clone())
        //     {
        //         clonedImage.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
        //     }
        // }
        image.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
        
        memory.Position = 0;

        BitmapImage bitmapImage = new BitmapImage();
        bitmapImage.BeginInit();
        bitmapImage.StreamSource = memory;
        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
        bitmapImage.EndInit();
        // bitmapImage.Freeze(); // スレッド間での安全な操作のために Freeze() を呼び出す

        return bitmapImage;
    }

    public Bitmap BitmapSourceToBitmap(BitmapSource bitmapSource)
    {
        Bitmap bitmap;
        using (MemoryStream memoryStream = new MemoryStream())
        {
            // BitmapSource をエンコードし、MemoryStream に保存
            BitmapEncoder encoder = new BmpBitmapEncoder(); // BMP形式でエンコード
            encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
            encoder.Save(memoryStream);
            
            // MemoryStream を Bitmap に変換
            bitmap = new Bitmap(memoryStream);
        }
        return bitmap;
    }
}