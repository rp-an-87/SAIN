using System.Diagnostics;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.Decision;

/// <summary>
/// Fork diagnostic: how much time the shot reaction code takes. Every 10 s one line with, per part, the average ms per second, the worst single call
/// and the worst frame (the sum of all calls in one frame), plus how many vanilla report calls came in and how many were taken.
/// </summary>
internal static class ShotPerf
{
    public const int Update = 0;
    public const int Apply = 1;
    public const int FanOut = 2;
    public const int Deliver = 3;
    private const int COUNT = 4;
    private static readonly string[] _names = ["update", "apply+cover", "alert fan-out", "report delivery"];
    private static readonly long[] _total = new long[COUNT];
    private static readonly long[] _max = new long[COUNT];
    private static readonly long[] _frameSum = new long[COUNT];
    private static readonly long[] _frameMax = new long[COUNT];
    private static readonly int[] _frame = new int[COUNT];
    private static readonly int[] _calls = new int[COUNT];
    private static float _nextLog;
    private static float _since;

    public static int ReportCalls;
    public static int ReportAccepted;

    public static long Start()
    {
        return Stopwatch.GetTimestamp();
    }

    public static void Stop(int id, long start)
    {
        long dt = Stopwatch.GetTimestamp() - start;
        _total[id] += dt;
        _calls[id]++;
        if (dt > _max[id])
        {
            _max[id] = dt;
        }
        int frame = Time.frameCount;
        if (_frame[id] != frame)
        {
            _frame[id] = frame;
            _frameSum[id] = 0;
        }
        _frameSum[id] += dt;
        if (_frameSum[id] > _frameMax[id])
        {
            _frameMax[id] = _frameSum[id];
        }
    }

    public static void Report(float now)
    {
        if (now < _nextLog)
        {
            return;
        }
        if (_nextLog <= 0f)
        {
            _nextLog = now + 10f;
            _since = now;
            return;
        }
        float span = Mathf.Max(now - _since, 0.1f);
        _nextLog = now + 10f;
        _since = now;
        if (SAINPlugin.LoadedPreset.GlobalSettings.Fork.ShotPerfLog)
        {
            double toMs = 1000.0 / Stopwatch.Frequency;
            var sb = new System.Text.StringBuilder(220);
            sb.Append("[ShotPerf] ").Append(span.ToString("0")).Append(" s: ");
            for (int i = 0; i < COUNT; i++)
            {
                sb.Append(_names[i]).Append(' ').Append((_total[i] * toMs / span).ToString("0.00")).Append(" ms/s (")
                    .Append(_calls[i]).Append(" calls, worst call ").Append((_max[i] * toMs).ToString("0.00"))
                    .Append(", worst frame ").Append((_frameMax[i] * toMs).ToString("0.00")).Append(") | ");
            }
            sb.Append("vanilla report calls ").Append(ReportCalls).Append(", taken ").Append(ReportAccepted);
            global::SAIN.Logger.LogInfoAlways(sb.ToString());
        }
        for (int i = 0; i < COUNT; i++)
        {
            _total[i] = 0;
            _max[i] = 0;
            _frameMax[i] = 0;
            _calls[i] = 0;
        }
        ReportCalls = 0;
        ReportAccepted = 0;
    }
}
