using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeepSORT.Domain.Models.Predictor;

public class ImagePath : ValueObject<string, ImagePath>
{
    public string Value { get; private set; }
    public ImagePath(string value)
    {
        Validate(value);
        Value = value;
    }

    public override string GetValue()
    {
        return this.Value;
    }

    private void Validate(string value)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(value);

        if (!File.Exists(value))
        {
            throw new ArgumentException("指定したパスが無効です");
        }

        string[] splitValue = value.Split('.');
        string[] imageExtensions = ["jpeg", "jpg", "png", "bmp", "gif"];
        if (!imageExtensions.Contains(splitValue[splitValue.Length - 1]))
        {
            throw new ArgumentException("パスには 画像 のファイルを指定してください");
        }
    }

}