using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeepSORT.Domain.Models.WebCamera;

public class WebCameraFps : ValueObject<int, WebCameraFps>
{
    public int Value {  get; private set; }
    public WebCameraFps(int value)
    {
        Validate(value);
        Value = value;
    }

    public override int GetValue()
    {
        return this.Value;
    }

    private void Validate(int value)
    {
        if (value < 0) { throw new ArgumentException("FPS には 1以上30以下 を指定してください"); }
        if (value > 30) { throw new ArgumentException("FPS には 1以上30以下 を指定してください"); }
    }
}
