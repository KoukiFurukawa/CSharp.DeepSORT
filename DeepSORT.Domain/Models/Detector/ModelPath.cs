using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeepSORT.Domain.Models.Detector;

public class ModelPath : ValueObject<string, ModelPath>
{
    public string Value { get; private set; }
    public ModelPath(string value)
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
        if (splitValue[splitValue.Length - 1] != "onnx")
        {
            throw new ArgumentException("パスには .onnx のファイルを指定してください");
        }
    }

}