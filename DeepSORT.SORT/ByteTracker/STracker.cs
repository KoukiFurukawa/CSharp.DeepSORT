using System.Drawing;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using OpenCvSharp;

namespace CsharpByteTrack.ByteTracker;
public class STrack : BaseTrack
{
    public int TrackletLen { get; private set; }
    private static readonly KalmanFilter SharedKalman = new();
    private KalmanFilter? _kalmanFilter;

    private Matrix<double>? _covariance;
    private Vector<double>? _mean;
    private Vector<double> _tlwh;

    public STrack(double[] tlwh, double score)
    {
        this._tlwh = Vector<double>.Build.Dense(tlwh);  // Deep copy of the array
        this._kalmanFilter = null;
        this._mean = null;
        this._covariance = null;
        this.Score = score;
        this.TrackletLen = 0;
    }

    public override void Predict()
    {
        Vector<double>? meanState = _mean?.Clone();  // Deep copy

        if (meanState != null && this._kalmanFilter != null && this._covariance != null)
        {
            if (this.State != TrackState.Tracked)
            {
                meanState[7] = 0;
            }
            var (newMean, newCovariance) = this._kalmanFilter.Predict(meanState, this._covariance);

            Validate(newMean, newCovariance);
            this._mean = newMean; 
            this._covariance = newCovariance;
        }
    }

    public static void MultiPredict(ref List<STrack> stracks)
    {
        if (stracks.Count == 0) return;

        for (var i = 0; i < stracks.Count; i++)
        {
            if (stracks[i]._covariance == null) return;
            if (stracks[i]._mean == null) return;
        }

        Matrix<double> multiMean = Matrix<double>.Build.DenseOfRowArrays(stracks.Select(st => st._mean?.ToArray()).Where(arr => arr != null).ToArray());
        var matrices = stracks.Select(st => st._covariance).Where(m => m != null).ToArray();
        // Matrix<double> multiCovariance = Matrix<double>.Build.DenseOfRowArrays
        // (
        //     matrices.SelectMany(m => m?.ToRowArrays() ?? throw new Exception("共分散 covariance が null です")).ToArray()
        // );

        var multiCovariance = new Matrix<double>[stracks.Count];

        for (int i = 0; i < stracks.Count; i++)
        {
            // 各トラックの共分散行列を multiCovariance に埋め込む
            var block = matrices[i];
            multiCovariance[i] = block;
        }

        for (int i = 0; i < stracks.Count; i++)
        {
            if (stracks[i].State != TrackState.Tracked)
            {
                multiMean[i, 7] = 0;
            }
        }

        var (predictedMeans, predictedCovariances) = SharedKalman.MultiPredict(multiMean, multiCovariance);

        for (int i = 0; i < stracks.Count; i++)
        {
            Validate(predictedMeans.Row(i), predictedCovariances[i]);

            stracks[i]._mean = predictedMeans.Row(i);
            // stracks[i]._covariance = predictedCovariances.Row(i).ToColumnMatrix();
            stracks[i]._covariance = predictedCovariances[i];
        }
    }

    public void Activate(KalmanFilter kalmanFilter, int frameId)
    {
        this._kalmanFilter = kalmanFilter;
        this.TrackId = NextId();
        var (newMean, newCovariance) = this._kalmanFilter.Initiate(TlwhToXyah(this._tlwh));

        Validate(newMean, newCovariance);
        this._mean = newMean; 
        this._covariance = newCovariance;

        this.TrackletLen = 0;
        this.State = TrackState.Tracked;
        this.IsActivated = frameId == 1;
        this.FrameId = frameId;
        this.StartFrame = frameId;
    }

    public void ReActivate(STrack newTrack, int frameId, bool newId = false)
    {
        if (this._kalmanFilter == null || this._mean == null || this._covariance == null) return;

        var (newMean, newCovariance) = this._kalmanFilter.Update(
            this._mean, this._covariance, TlwhToXyah(newTrack.Tlwh));

        Validate(newMean, newCovariance);
        this._mean = newMean; 
        this._covariance = newCovariance;

        this.TrackletLen = 0;
        this.State = TrackState.Tracked;
        this.IsActivated = true;
        this.FrameId = frameId;

        if (newId) this.TrackId = NextId();
        this.Score = newTrack.Score;
    }

    public void Update(STrack newTrack, int frameId)
    {
        if (this._kalmanFilter == null || _mean == null || this._covariance == null) return;

        this.FrameId = frameId;
        this.TrackletLen++;

        // Validate(_mean, _covariance);

        var newTlwh = newTrack.Tlwh;
        var (newMean, newCov) = this._kalmanFilter.Update(
            this._mean, this._covariance, TlwhToXyah(newTlwh));

        Validate(newMean, newCov);

        this._covariance = newCov;
        this._mean = newMean;
        this.State = TrackState.Tracked;
        this.IsActivated = true;
        this.Score = newTrack.Score;
    }

    public Vector<double> Tlwh
    {
        get
        {
            if (_mean == null) return _tlwh.Clone();
            var ret = Vector<double>.Build.Dense(_mean.Take(4).ToArray());
            ret[2] *= ret[3];
            ret[0] -= ret[2] / 2;
            ret[1] -= ret[3] / 2;
            return ret;
        }
    }

    public Rect CreateRectangle()
    {
        Rect rectangle = new Rect
        (
            (int)Tlwh[0], (int)Tlwh[1], (int)Tlwh[2], (int)Tlwh[3]
        );
        return rectangle;
    }

    public Vector<double> Tlbr
    {
        get
        {
            var ret = Tlwh;
            ret[2] += ret[0];
            ret[3] += ret[1];
            return ret;
        }
    }

    public static Vector<double> TlwhToXyah(Vector<double> tlwh)
    {
        var ret = tlwh.Clone();
        ret[0] += ret[2] / 2;
        ret[1] += ret[3] / 2;
        ret[2] /= ret[3];
        return ret;
    }

    // public double[] ToXyah() => TlwhToXyah(Tlwh);

    public static double[] TlbrToTlwh(double[] tlbr)
    {
        var ret = tlbr.ToArray();
        ret[2] -= ret[0];
        ret[3] -= ret[1];
        return ret;
    }

    public static Vector<double> TlwhToTlbr(Vector<double> tlwh)
    {
        var ret = tlwh.Clone();
        ret[2] += ret[0];
        ret[3] += ret[1];
        return ret;
    }

    public override string ToString()
    {
        return $"OT_{TrackId}_({StartFrame}-{EndFrame()})";
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
