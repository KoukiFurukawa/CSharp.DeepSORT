using Accord.Math;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using MathNet.Numerics.Providers.LinearAlgebra;
using Microsoft.VisualBasic;
using OpenCvSharp;
using System.Data;
using System.Diagnostics;
using System.Drawing;


namespace CsharpByteTrack.ByteTracker;

public class KalmanFilter
{
    private readonly Dictionary<int, double> chi2inv95 = new()
    {
        { 1, 3.8415 },
        { 2, 5.9915 },
        { 3, 7.8147 },
        { 4, 9.4877 },
        { 5, 11.070 },
        { 6, 12.592 },
        { 7, 14.067 },
        { 8, 15.507 },
        { 9, 16.919 }
    };
    private Matrix<double> _motionMat;
    private Matrix<double> _updateMat;
    private double _stdWeightPosition = 1.0 / 20;
    private double _stdWeightVelocity = 1.0 / 160;

    public KalmanFilter()
    {
        int ndim = 4;
        double dt = 1.0;

        this._motionMat = DenseMatrix.Create(2 * ndim, 2 * ndim, 0);
        this._updateMat = DenseMatrix.Create(ndim, 2 * ndim, 0);

        for (int i = 0; i < 2 * ndim; i++)
        {
            this._motionMat[i, i] = dt;
        }

        for (int j = 0; j < ndim; j++)
        {
            this._motionMat[j, ndim + j] = dt;
        }

        for (int k = 0; k < ndim; k++)
        {
            this._updateMat[k, k] = dt;
        }
    }

    public (Vector<double> mean, Matrix<double> covariance) Initiate(Vector<double> measurement)
    {
        var meanPos = measurement;
        var meanVel = Vector<double>.Build.Dense(measurement.Count, 0);
        var mean = Vector<double>.Build.DenseOfEnumerable(meanPos.Concat(meanVel));

        var std = new double[]
        {
            2 * _stdWeightPosition * measurement[3],
            2 * _stdWeightPosition * measurement[3],
            1e-2,
            2 * _stdWeightPosition * measurement[3],
            10 * _stdWeightVelocity * measurement[3],
            10 * _stdWeightVelocity * measurement[3],
            1e-5,
            10 * _stdWeightVelocity * measurement[3]
        };

        var covariance = DenseMatrix.OfDiagonalArray(std.Select(x => x * x).ToArray());

        Validate(mean, covariance);

        return (mean, covariance);
    }

    private Matrix<double> Dot(Matrix<double> a, Matrix<double> b)
    {
        var result = Matrix<double>.Build.Dense(a.RowCount, b.ColumnCount);

        for (int i = 0; i < a.RowCount; i++)
        {
            for (int j = 0; j < b.ColumnCount; j++)
            {
                double sum = 0;
                for (int k = 0; k < b.RowCount; k++)
                {
                    sum += a[i, k] * b[k, j];
                }
                result[i, j] = sum;
            }
        }

        return result;
    }

    public (Vector<double> mean, Matrix<double> covariance) Predict
    (
        Vector<double> mean, Matrix<double> covariance
    )
    {
        var stdPos = new double[]
        {
            this._stdWeightPosition * mean[3],
            this._stdWeightPosition * mean[3],
            1e-2,
            this._stdWeightPosition * mean[3]
        };
        var stdVel = new double[]
        {
            this._stdWeightVelocity * mean[3],
            this._stdWeightVelocity * mean[3],
            1e-5,
            this._stdWeightVelocity * mean[3]
        };

        var motionCov = DenseMatrix.OfDiagonalArray(stdPos.Concat(stdVel).Select(x => x * x).ToArray());

        mean = this._motionMat * mean;
        covariance = this._motionMat * covariance * this._motionMat.Transpose() + motionCov;

        Validate(mean, covariance);

        return (mean, covariance);
    }

    public (Vector<double> mean, Matrix<double> covariance) Project(Vector<double> mean, Matrix<double> covariance)
    {
        double h = mean[3];
        var std = new double[]
        {
            this._stdWeightPosition * h,
            this._stdWeightPosition * h,
            1e-1,
            this._stdWeightPosition * h
        };
        var innovationCov = DenseMatrix.OfDiagonalArray(std.Select(x => x * x).ToArray());

        // var newMean = _updateMat * mean;
        var calMean = Vector<double>.Build.Dense(_updateMat.RowCount, 0);
        for (var i = 0; i < _updateMat.RowCount; i++)
        {
            double sum = 0;
            for (var j = 0; j < _updateMat.ColumnCount; j++)
            {
                sum += _updateMat[i, j] * mean[j];
            }
            calMean[i] = sum;
        }

        // var newCovariance = _updateMat * covariance * _updateMat.Transpose() + innovationCov;
        var calCovariance = Dot(_updateMat, covariance);
        calCovariance = Dot(calCovariance, _updateMat.Transpose());
        calCovariance += innovationCov;

        // Validate(newMean, newCovariance);
        Validate(calMean, calCovariance);

        return (calMean, calCovariance);
    }

    public (Matrix<double> mean, Matrix<double>[] covariance) MultiPredict
    (
        Matrix<double> mean, Matrix<double>[] covariance
    )
    {
        int n = mean.RowCount;
        var stdPos = new double[n, 4];
        var stdVel = new double[n, 4];

        for (int i = 0; i < n; i++)
        {
            double h = mean[i, 3]; // height
            stdPos[i, 0] = _stdWeightPosition * h;
            stdPos[i, 1] = _stdWeightPosition * h;
            stdPos[i, 2] = 1e-2;
            stdPos[i, 3] = _stdWeightPosition * h;

            stdVel[i, 0] = _stdWeightVelocity * h;
            stdVel[i, 1] = _stdWeightVelocity * h;
            stdVel[i, 2] = 1e-5;
            stdVel[i, 3] = _stdWeightVelocity * h;
        }

        var motionCov = new Matrix<double>[n];

        for (int i = 0; i < n; i++)
        {
            var diagValues = stdPos.GetRow(i).Concat(stdVel.GetRow(i))
                                .Select(x => x * x).ToArray();
            motionCov[i] = DenseMatrix.OfDiagonalArray(8, 8, diagValues);  // 対角行列を生成
        }

        var T_motionMat = this._motionMat.Transpose();
        var calMean = Dot(mean, this._motionMat.Transpose());

        var left = new Matrix<double>[n];
        for (int i = 0; i < n; i++)
        {
            var cov = covariance[i];
            var product = Dot(_motionMat, cov);
            left[i] = product;
        }

        var T_left = TransposeMatrices(left);

        var newLeft = new Matrix<double>[n];
        for (int i = 0; i < n; i++)
        {
            var t = T_left[i];
            newLeft[i] = Dot(t, T_motionMat);
        }

        var predictedCovariance = new Matrix<double>[n];
        for (int i = 0; i < n; i++)
        {
            if (newLeft[i].RowCount != motionCov[i].RowCount || newLeft[i].ColumnCount != motionCov[i].ColumnCount)
                throw new ArgumentException($"行列のサイズが一致しません: 行列 {i}");

            predictedCovariance[i] = newLeft[i] + motionCov[i];
            Validate(calMean.Row(i), predictedCovariance[i]);
        }

        return (calMean, predictedCovariance);
    }

    private Matrix<double>[] TransposeMatrices(Matrix<double>[] matrices)
    {
        int n = matrices.Length;
        var transposed = new Matrix<double>[n];

        for (var i = 0; i < n; i++)
        {
            transposed[i] = matrices[i];
        }

        return transposed;
    }

    public (Vector<double> mean, Matrix<double> covariance) Update
    (
        Vector<double> mean, Matrix<double> covariance, Vector<double> measurement
    )
    {
        var (projectedMean, projectedCov) = this.Project(mean, covariance);
        var cholFactor = projectedCov.Cholesky();

        var T_updateMat = _updateMat.Transpose();
        var newProduct = Dot(covariance, T_updateMat);

        var kalmanGain = cholFactor.Solve(newProduct.Transpose()).Transpose();

        var innovation = measurement - projectedMean;
        var newMean = mean + (kalmanGain * innovation);

        var newCovariance = covariance - kalmanGain * projectedCov * kalmanGain.Transpose();

        Validate(newMean, newCovariance);

        return (newMean, newCovariance);
    }

    public double[] GatingDistance
    (
        Vector<double> mean, Matrix<double> covariance, Matrix<double> measurements,
        bool onlyPosition = false, Metric metric = Metric.Maha
    )
    {
        var (projectedMean, projectedCov) = Project(mean, covariance);

        if (onlyPosition)
        {
            projectedMean = projectedMean.SubVector(0, 2);
            projectedCov = projectedCov.SubMatrix(0, 2, 0, 2);
            measurements = measurements.SubMatrix(0, measurements.RowCount, 0, 2);
        }

        var d = measurements - projectedMean.ToRowMatrix();

        if (metric == Metric.Gaussian)
        {
            return d.PointwiseMultiply(d).RowSums().ToArray();
        }
        else if (metric == Metric.Maha)
        {
            var choleskyFactor = projectedCov.Cholesky().Factor;
            var z = choleskyFactor.Solve(d.Transpose());
            return z.PointwiseMultiply(z).ColumnSums().ToArray();
        }
        else
        {
            throw new ArgumentException("Invalid distance metric");
        }
    }

    private static void Validate(Vector<double> mean, Matrix<double> covariance)
    {
        for (int i= 0; i < mean.Count; i++)
        {
            if 
            (
                double.IsNaN(mean[i]) || double.IsInfinity(mean[i]) ||
                double.IsNegativeInfinity(mean[i]) || double.IsPositiveInfinity(mean[i])
            )
            {
                throw new Exception("Meanに 無効な値 を確認");
            }
        }
        
        for (int i = 0; i < covariance.RowCount; i++)
        {
            for (int j = 0; j < covariance.ColumnCount; j++)
            {
                if 
                (
                    double.IsNegative(covariance[i, j]) || double.IsNaN(covariance[i, j]) || 
                    double.IsPositiveInfinity(covariance[i, j]) || double.IsNegativeInfinity(covariance[i, j])
                )
                {
                    throw new Exception("Covarianceに 無効な値 を確認");
                }
            }
        }
    } 

}

public enum Metric
{
    Maha,
    Gaussian

}

public static class ArrayExtensions
{
    public static double[] GetRow(this double[,] matrix, int rowIndex)
    {
        int columns = matrix.GetLength(1);
        double[] row = new double[columns];
        for (int i = 0; i < columns; i++)
        {
            row[i] = matrix[rowIndex, i];
        }
        return row;
    }
}
