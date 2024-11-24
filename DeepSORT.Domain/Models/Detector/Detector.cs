using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeepSORT.Domain.Models.Detector;

public class Detector
{
    private string modelPath { get; }

    public Detector(ModelPath modelPath)
    {
        this.modelPath = modelPath.GetValue();
    }


}
