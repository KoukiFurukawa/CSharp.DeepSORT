using Newtonsoft.Json;
using OpenCvSharp;

namespace DeepSORT.Domain.Models;
public abstract class ValueObject<T1, T2> : IEquatable<T2> where T2 : ValueObject<T1, T2>
{
    public abstract T1 GetValue();

    public bool Equals(T2? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return string.Equals(GetValue(), other.GetValue());
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj is T2 other && Equals(other);
    }

    public override int GetHashCode()
    {
        return GetValue()?.GetHashCode() ?? 0;
    }

    public override string ToString()
    {
        T1 value = GetValue();

        if (value == null)
        {
            return "Value is null";
        }

        if (value is string StringValue)
        {
            return StringValue;
        }
        else if (value is int IntValue)
        {
            return IntValue.ToString();
        }
        else if (value is double DoubleValue)
        {
            return DoubleValue.ToString();
        }
        else if (value is Mat MatValue)
        {
            double[][] result = new double[MatValue.Rows][];
            for (int i = 0; i < MatValue.Rows; i++)
            {
                result[i] = new double[MatValue.Cols];
                for (int j = 0; j < MatValue.Cols; j++)
                {
                    result[i][j] = MatValue.At<double>(i, j);
                }
            }

            return JsonConvert.SerializeObject(result, Formatting.Indented);
        }
        else
        {
            return $"Unsupported type.";
        }
    }

}