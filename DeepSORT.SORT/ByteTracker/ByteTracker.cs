
using Accord.Math;
using Accord.Statistics;
using System.Linq;

/*
1. 物体クラスごとに生成されているクラスである
2. ここでは「検出された物体」を記録する
3. 
*/

namespace CsharpByteTrack.ByteTracker;
public class ByteTracker
{
    private List<STrack> trackedStracks = new();
    private List<STrack> lostStracks = new();
    private List<STrack> removedStracks = new();

    private int frameId = 0;
    private readonly double detThresh;
    private readonly int maxTimeLost;
    private readonly double matchThresh;
    private readonly KalmanFilter kalmanFilter = new();
    private readonly bool mot20;

    public ByteTracker(ByteTrackerArgs args, double frameRate = 10)
    {
        this.detThresh = args.TrackThresh + 0.1;
        this.matchThresh = args.MatchThresh;
        this.maxTimeLost = (int)(frameRate / 30.0 * args.TrackBuffer);
        this.mot20 = args.MOT20;
    }

    public List<STrack> Update(double[,] outputResults, int[] imgInfo, int[] imgSize)
    {
        this.frameId++;
        List<STrack> activatedStracks = new();
        List<STrack> refindStracks = new();
        List<STrack> lostStracksLocal = new();
        List<STrack> removedStracksLocal = new();

        double[] scores = GetScores(outputResults);
        double[,] bboxes = GetBboxes(outputResults);

        ScaleBboxes(ref bboxes, imgInfo, imgSize);

        bool[] remainInds = scores.Select((s, i) => s > detThresh).ToArray();
        bool[] lowInds = scores.Select((s, i) => s > 0.1 && s < detThresh).ToArray();

        double[,] dets = FilterBboxes(bboxes, remainInds); // 基準値(0.6)を超えて検出された boundaryBox
        double[] scoresKeep = FilterScores(scores, remainInds);
        double[,] detsSecond = FilterBboxes(bboxes, lowInds);
        double[] scoresSecond = FilterScores(scores, lowInds);

        List<STrack> detections = CreateDetections(dets, scoresKeep);
        List<STrack> unconfirmed = this.trackedStracks.Where(t => !t.IsActivated).ToList();
        List<STrack> confirmedTracks = this.trackedStracks.Where(t => t.IsActivated).ToList(); // 追跡中の物体?

        List<STrack> strackPool = JointStracks(confirmedTracks, this.lostStracks);

        STrack.MultiPredict(ref strackPool);
        var dists = Matching.IoUDistance(strackPool, detections);
        if (!mot20) dists = Matching.FuseScore(dists, detections);

        var (matches, uTrack, uDetection) = Matching.LinearAssignment(dists, this.detThresh);



        // this.ProcessMatches(matches, ref strackPool, detections, ref activatedStracks, ref refindStracks);
        foreach (var (trackIdx, detIdx) in matches)
        {
            var track = strackPool[trackIdx];
            var detection = detections[detIdx];
            if (track.State == TrackState.Tracked)
            {
                try
                {
                    track.Update(detection, frameId);
                    activatedStracks.Add(track);
                    strackPool[trackIdx] = track;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    return [];
                }
            }
            else
            {
                track.ReActivate(detection, frameId, newId: false);
                refindStracks.Add(track);
            }
        }


        // this.ProcessLowScoreDetections(detsSecond, scoresSecond, ref strackPool, uTrack, ref activatedStracks, ref refindStracks);
        var detectionsSecond = CreateDetections(detsSecond, scoresSecond);
        var rTrackedStracks = new List<STrack>();
        foreach (var i in uTrack)
        {
            var track = strackPool[i];
            if (track.State == TrackState.Tracked)
            {
                rTrackedStracks.Add(track);
            }
        }

        var lowScoreDists = Matching.IoUDistance(rTrackedStracks, detectionsSecond);
        var (lowScoreMatches, lowScoreUTrack, _) = Matching.LinearAssignment(lowScoreDists, this.detThresh);

        foreach (var (tracked, det) in lowScoreMatches)
        {
            var track = rTrackedStracks[tracked];
            var detection = detectionsSecond[det];
            if (track.State == TrackState.Tracked)
            {
                try
                {
                    track.Update(detection, frameId);
                    activatedStracks.Add(track);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    return [];
                }
            }
            else
            {
                track.ReActivate(detection, frameId, newId: false);
                refindStracks.Add(track);
            }
        }

        foreach (var it in lowScoreUTrack)
        {
            var track = rTrackedStracks[it];
            if (track.State != TrackState.Lost)
            {
                track.MartLost();
                lostStracksLocal.Add(track);
            }
        }


        // this.UpdateUnconfirmedTracks(ref unconfirmed, detections, uDetection, ref activatedStracks, ref removedStracksLocal);
        List<STrack> unconfirmedDetections = [];
        foreach (var i in uDetection)
        {
            unconfirmedDetections.Add(detections[i]);
        }
        var unconfirmedDists = Matching.IoUDistance(unconfirmed, unconfirmedDetections);
        // if (!mot20) { unconfirmedDists = Matching.FuseScore(unconfirmed, unconfirmedDists); } // FuseScoreの改修が必要

        var (unconfirmedMatches, uUnconfirmed, unconfirmedDetection) = Matching.LinearAssignment(unconfirmedDists, matchThresh);

        foreach (var (trackedId, detectionId) in unconfirmedMatches)
        {
            try
            {
                unconfirmed[trackedId].Update(unconfirmedDetections[detectionId], frameId);
                activatedStracks.Add(unconfirmed[trackedId]);
            }
            catch
            {
                return [];
            }
        }

        foreach (var idx in uUnconfirmed)
        {
            unconfirmed[idx].MarkRemoved();
            removedStracksLocal.Add(unconfirmed[idx]);
        }


        // this.InitializeNewTracks(detections, uDetection, ref activatedStracks);
        foreach (var idx in unconfirmedDetection)
        {
            var track = detections[idx];
            if (track.Score < this.detThresh) continue;
            track.Activate(this.kalmanFilter, this.frameId);
            activatedStracks.Add(track);
        }


        // this.UpdateTrackStates(ref activatedStracks, ref refindStracks, ref lostStracksLocal, ref removedStracksLocal);
        foreach (var track in this.lostStracks)
        {
            if (this.frameId - track.EndFrame() > this.maxTimeLost)
            {
                track.MarkRemoved();
                removedStracksLocal.Add(track);
            }
        }

        this.trackedStracks = this.trackedStracks.Where(t => t.State == TrackState.Tracked).ToList();

        this.trackedStracks = JointStracks(trackedStracks, activatedStracks);
        this.trackedStracks = JointStracks(trackedStracks, refindStracks);

        // this.lostStracks = lostStracks.Except(trackedStracks).ToList();
        this.lostStracks = SubSTracks(this.lostStracks, this.trackedStracks);
        this.lostStracks.AddRange(lostStracksLocal);
        this.lostStracks = SubSTracks(this.lostStracks, this.removedStracks);

        this.removedStracks.AddRange(removedStracksLocal);
        // this.removedStracks.AddRange(this.lostStracks.Where(t => frameId - t.EndFrame() > maxTimeLost));

        this.trackedStracks = this.trackedStracks.Where(t => t.State == TrackState.Tracked).ToList();
        (this.trackedStracks, this.lostStracks) = RemoveDuplicateSTracks(this.trackedStracks, this.lostStracks);

        var outputStracks = this.trackedStracks.Where(t => t.IsActivated).ToList();

        return outputStracks;
    }

    private (List<STrack>, List<STrack>) RemoveDuplicateSTracks
    (
        List<STrack> sTracksA, List<STrack> sTracksB
    )
    {
        var pDist = Matching.IoUDistance(sTracksA, sTracksB);
        List<int> dupa = [];
        List<int> dupb = [];

        // 距離行列から 0.15 未満の要素のインデックスを取得
        for (int i = 0; i < pDist.GetLength(0); i++)
        {
            for (int j = 0; j < pDist.GetLength(1); j++)
            {
                if (pDist[i, j] < 0.15)
                {
                    int timep = sTracksA[i].FrameId - sTracksA[i].StartFrame;
                    int timeq = sTracksB[j].FrameId - sTracksB[j].StartFrame;

                    if (timep > timeq)
                        dupb.Add(j);
                    else
                        dupa.Add(i);
                }
            }
        }
        List<STrack> resa = sTracksA
            .Where((t, index) => !dupa.Contains(index))
            .ToList();
        List<STrack> resb = sTracksB
            .Where((t, index) => !dupb.Contains(index))
            .ToList();

        return (resa, resb);
    }

    private static double[] GetScores(double[,] outputResults) =>
        Enumerable.Range(0, outputResults.GetLength(0)).Select(i => outputResults[i, 4]).ToArray();

    private static double[,] GetBboxes(double[,] outputResults)
    {
        var bboxes = new double[outputResults.GetLength(0), 4];
        for (int i = 0; i < outputResults.GetLength(0); i++)
        {
            for (int j = 0; j < 4; j++)
            {
                bboxes[i, j] = outputResults[i, j];
            }
        }
        return bboxes;
    }

    private static void ScaleBboxes(ref double[,] bboxes, int[] imgInfo, int[] imgSize)
    {
        double scale = Math.Min((double)imgSize[0] / imgInfo[0], (double)imgSize[1] / imgInfo[1]);
        for (int i = 0; i < bboxes.GetLength(0); i++)
        {
            for (int j = 0; j < 4; j++)
            {
                bboxes[i, j] /= scale;
            }
        }
    }

    private static double[,] FilterBboxes(double[,] bboxes, bool[] filter)
    {
        double[][] filteredRows = Enumerable.Range(0, bboxes.GetLength(0))
            .Where(i => filter[i])
            .Select(i =>
                Enumerable.Range(0, bboxes.GetLength(1)).Select(j => bboxes[i, j]).ToArray()
            ).ToArray();

        int rowCount = filteredRows.Length;
        int colCount = bboxes.GetLength(1);
        var result = new double[rowCount, colCount];

        for (int i = 0; i < rowCount; i++)
        {
            for (int j = 0; j < colCount; j++)
            {
                result[i, j] = filteredRows[i][j];
            }
        }

        return result;
    }

    private static double[] FilterScores(double[] scores, bool[] filter)
    {
        return scores.Where((_, i) => filter[i]).ToArray();
    }

    private static List<STrack> CreateDetections(double[,] bboxes, double[] scores)
    {
        var detections = new List<STrack>();
        for (int i = 0; i < bboxes.GetLength(0); i++)
        {
            var tlwh = STrack.TlbrToTlwh(bboxes.GetRow(i));
            detections.Add(new STrack(tlwh, scores[i]));
        }
        return detections;
    }

    private static List<STrack> JointStracks(List<STrack> a, List<STrack> b)
    {
        Dictionary<int, int> exists = new();
        List<STrack> result = [];
        foreach (var strack in a)
        {
            exists[strack.TrackId] = 1;
            result.Add(strack);
        }
        foreach (var strack in b)
        {
            var tid = strack.TrackId;
            if (!exists.ContainsKey(tid))
            {
                exists[tid] = 1;
                result.Add(strack);
            }
        }
        return result;
    }


    private static List<STrack> SubSTracks(List<STrack> a, List<STrack> b)
    {
        var stracks = new Dictionary<int, STrack>();
        foreach (var t in a)
        {
            stracks[t.TrackId] = t;
        }
        foreach (var t in b)
        {
            int tid = t.TrackId;
            if (stracks.ContainsKey(tid))
            {
                stracks.Remove(tid);
            }
        }
        return stracks.Values.ToList();
    }
}

public record ByteTrackerArgs
{
    public double TrackThresh { get; set; }
    public int TrackBuffer { get; set; }
    public double MatchThresh { get; set; }
    public bool MOT20 { get; set; }
}