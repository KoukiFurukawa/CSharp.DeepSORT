using System.Collections.Specialized;

namespace CsharpByteTrack.ByteTracker;

public class BaseTrack
{
    protected static int Count = 0;
    public int TrackId = 0;
    public bool IsActivated = false;
    public TrackState State = TrackState.New;
    public OrderedDictionary History = new();
    public List<int> Features = [];
    public int? CurrentFeature = null;
    public double Score = 0;
    public int StartFrame = 0;
    public int FrameId = 0;
    public int TimeSinceUpdate = 0;
    private static readonly object lockObject = new object();

    public (long, long) location = (long.MaxValue, long.MaxValue);

    public int EndFrame() => this.FrameId;
    static public int NextId()
    {
        lock (lockObject)
        {
            Count++;
            return Count;
        }
    }

    public Exception Activate() => throw new NotImplementedException(nameof(BaseTrack));
    public virtual void Predict() => throw new NotImplementedException(nameof(BaseTrack));
    public Exception Update() => throw new NotImplementedException(nameof(BaseTrack));
    public void MartLost() => this.State = TrackState.Lost;
    public void MarkRemoved() => this.State = TrackState.Removed;
}


public enum TrackState
{
    New = 0,
    Tracked = 1,
    Lost = 2,
    Removed = 3
}