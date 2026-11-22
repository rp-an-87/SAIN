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
    public const int Finder = 4;
    public const int SetEnemyPos = 5;
    public const int Uncertainty = 6;
    public const int Snap = 7;
    public const int Members = 8;
    public const int Hooks = 9;
    public const int Queues = 10;
    public const int Duel = 11;
    public const int Pressure = 12;
    public const int Suppress = 13;
    public const int Interrupts = 14;
    public const int CloseDiag = 15;
    public const int Lookup = 16;
    public const int Tail = 17;
    public const int Relocate = 18;
    public const int HurtLoss = 19;
    public const int SquadFollow = 20;
    public const int Bursts = 21;
    public const int Push = 22;
    public const int RelNav = 23;
    public const int RelLos = 24;
    public const int RelCover = 25;
    public const int RelPath = 26;
    private const int COUNT = 27;
    private static readonly string[] _names = ["update", "apply+cover", "alert fan-out", "report delivery", "cover finder", "  of delivery: vanilla SetEnemyPos", "  of delivery: uncertainty", "  of delivery: navmesh snap", "  of delivery: member loop", "  of update: hooks", "  of update: queues", "  of update: duel+squad follow+retreat", "  of update: pressure retreat", "  of update: area suppression", "  of update: interrupts", "  of update: close diagnostics", "  of delivery: closest member", "  of update: safety net+trace", "  of duel: relocation point search (per attempt)", "  of duel: hurt+squad loss retreats", "  of duel: squad follow", "  of duel: burst tracking", "  of duel: push start", "  of search: navmesh sampling", "  of search: line of sight", "  of search: cover scan", "  of search: path checks"];
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
    public static int ReportDeferred;

    /// <summary>Ticks spent in this part so far in the current frame.</summary>
    public static long FrameTicks(int id)
    {
        return _frame[id] == Time.frameCount ? _frameSum[id] : 0;
    }

    public static long TicksFromMs(float ms)
    {
        return (long)(ms * Stopwatch.Frequency / 1000.0);
    }

    /// <summary>Timers only run while the ShotPerfLog setting is on (read every 10 s, so a change takes up to 10 s). Off by default in normal play: no overhead.</summary>
    private static bool _enabled;

    public static long Start()
    {
        return _enabled ? Stopwatch.GetTimestamp() : 0;
    }

    /// <summary>For the timer the report frame budget depends on: runs whether or not the diagnostics are on.</summary>
    public static long StartAlways()
    {
        return Stopwatch.GetTimestamp();
    }

    public static void Stop(int id, long start)
    {
        if (start == 0)
        {
            return;
        }
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
            _enabled = SAINPlugin.LoadedPreset.GlobalSettings.Fork.ShotPerfLog;
            _nextLog = now + 10f;
            _since = now;
            return;
        }
        float span = Mathf.Max(now - _since, 0.1f);
        _nextLog = now + 10f;
        _since = now;
        bool wasEnabled = _enabled;
        _enabled = SAINPlugin.LoadedPreset.GlobalSettings.Fork.ShotPerfLog;
        if (wasEnabled)
        {
            double toMs = 1000.0 / Stopwatch.Frequency;
            var sb = new System.Text.StringBuilder(220);
            sb.Append("[ShotPerf] ").Append(span.ToString("0")).Append(" s: ");
            for (int i = 0; i < COUNT; i++)
            {
                if (_calls[i] == 0 && i >= 5)
                {
                    continue;
                }
                sb.Append(_names[i]).Append(' ').Append((_total[i] * toMs / span).ToString("0.00")).Append(" ms/s (")
                    .Append(_calls[i]).Append(" calls, worst call ").Append((_max[i] * toMs).ToString("0.00"))
                    .Append(", worst frame ").Append((_frameMax[i] * toMs).ToString("0.00")).Append(") | ");
            }
            sb.Append("vanilla report calls ").Append(ReportCalls).Append(", taken ").Append(ReportAccepted).Append(", reports pushed to a later frame ").Append(ReportDeferred);
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
        ReportDeferred = 0;
    }
}
