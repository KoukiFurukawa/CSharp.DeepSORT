using System.ComponentModel;

namespace CSharp.DeepSORT.Models;
public class RequestButton : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    /*    public BitmapSource? Image
        {
            get => _image;
            set
            {
                _image = value;
                OnPropertyChanged(nameof(Image));
            }
        }*/

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}