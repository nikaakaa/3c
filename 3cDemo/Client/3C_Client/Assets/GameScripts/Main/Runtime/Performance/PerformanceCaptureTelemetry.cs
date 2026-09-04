using System;
using System.Diagnostics;

namespace ThirdPersonPerformance
{
    public readonly struct PerformanceLogicTickSample
    {
        public PerformanceLogicTickSample(ulong tick, ulong renderFrame, long elapsedNanoseconds, long[] phaseNanoseconds)
        {
            Tick = tick;
            RenderFrame = renderFrame;
            ElapsedNanoseconds = elapsedNanoseconds;
            PhaseNanoseconds = phaseNanoseconds ?? throw new ArgumentNullException(nameof(phaseNanoseconds));
        }

        public ulong Tick { get; }
        public ulong RenderFrame { get; }
        public long ElapsedNanoseconds { get; }
        public long[] PhaseNanoseconds { get; }
    }

    public static class PerformanceCaptureTelemetry
    {
        static ulong[] s_Ticks = Array.Empty<ulong>();
        static ulong[] s_RenderFrames = Array.Empty<ulong>();
        static long[] s_ElapsedNanoseconds = Array.Empty<long>();
        static long[,] s_PhaseNanoseconds = new long[0, 0];
        static int[] s_PhaseStack = Array.Empty<int>();
        static long[] s_PhaseStartedAt = Array.Empty<long>();
        static int s_Count;
        static int s_CurrentIndex = -1;
        static int s_PhaseDepth;
        static int s_PhaseCount;
        static bool s_Active;
        static bool s_Overflowed;

        public static bool IsActive => s_Active;
        public static bool Overflowed => s_Overflowed;

        public static void Begin(int logicTickCapacity, int phaseCount = 31)
        {
            if (logicTickCapacity <= 0 || phaseCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(logicTickCapacity));
            if (s_Active)
                throw new InvalidOperationException("Performance capture telemetry is already active.");
            s_Ticks = new ulong[logicTickCapacity];
            s_RenderFrames = new ulong[logicTickCapacity];
            s_ElapsedNanoseconds = new long[logicTickCapacity];
            s_PhaseNanoseconds = new long[logicTickCapacity, phaseCount];
            s_PhaseStack = new int[64];
            s_PhaseStartedAt = new long[64];
            s_Count = 0;
            s_CurrentIndex = -1;
            s_PhaseDepth = 0;
            s_PhaseCount = phaseCount;
            s_Overflowed = false;
            s_Active = true;
        }

        public static long BeginLogicTick(ulong tick, ulong renderFrame)
        {
            if (!s_Active)
                return 0L;
            if (tick == 0 || s_CurrentIndex >= 0)
                throw new InvalidOperationException("Performance LogicTick telemetry nesting is invalid.");
            if (s_Count >= s_Ticks.Length)
            {
                s_Overflowed = true;
                return 0L;
            }
            s_CurrentIndex = s_Count;
            s_Ticks[s_CurrentIndex] = tick;
            s_RenderFrames[s_CurrentIndex] = renderFrame;
            s_PhaseDepth = 0;
            return Stopwatch.GetTimestamp();
        }

        public static void EndLogicTick(ulong tick, long startedAt)
        {
            if (startedAt == 0L)
                return;
            long elapsed = Stopwatch.GetTimestamp() - startedAt;
            long nanoseconds = checked(elapsed * 1000000000L / Stopwatch.Frequency);
            if (!s_Active)
                return;
            if (s_CurrentIndex != s_Count || s_Ticks[s_CurrentIndex] != tick || s_PhaseDepth != 0)
                throw new InvalidOperationException("Performance LogicTick telemetry completion is invalid.");
            s_ElapsedNanoseconds[s_CurrentIndex] = nanoseconds;
            s_Count++;
            s_CurrentIndex = -1;
        }

        public static void BeginPhase(int phase)
        {
            if (!s_Active)
                return;
            if (s_CurrentIndex < 0 || phase <= 0 || phase >= s_PhaseCount || s_PhaseDepth >= s_PhaseStack.Length)
                throw new InvalidOperationException("Performance phase telemetry begin is invalid.");
            s_PhaseStack[s_PhaseDepth] = phase;
            s_PhaseStartedAt[s_PhaseDepth] = Stopwatch.GetTimestamp();
            s_PhaseDepth++;
        }

        public static void EndPhase(int phase)
        {
            long completedAt = Stopwatch.GetTimestamp();
            if (!s_Active)
                return;
            if (s_CurrentIndex < 0 || s_PhaseDepth <= 0 || s_PhaseStack[s_PhaseDepth - 1] != phase)
                throw new InvalidOperationException("Performance phase telemetry end is invalid.");
            s_PhaseDepth--;
            long elapsed = completedAt - s_PhaseStartedAt[s_PhaseDepth];
            long nanoseconds = checked(elapsed * 1000000000L / Stopwatch.Frequency);
            s_PhaseNanoseconds[s_CurrentIndex, phase] = checked(s_PhaseNanoseconds[s_CurrentIndex, phase] + nanoseconds);
        }

        public static PerformanceLogicTickSample[] Complete()
        {
            if (!s_Active)
                throw new InvalidOperationException("Performance capture telemetry is not active.");
            var samples = new PerformanceLogicTickSample[s_Count];
            for (int i = 0; i < samples.Length; i++)
            {
                var phases = new long[s_PhaseCount];
                for (int phase = 1; phase < phases.Length; phase++)
                    phases[phase] = s_PhaseNanoseconds[i, phase];
                samples[i] = new PerformanceLogicTickSample(s_Ticks[i], s_RenderFrames[i], s_ElapsedNanoseconds[i], phases);
            }
            Reset();
            return samples;
        }

        public static void Cancel()
        {
            Reset();
        }

        static void Reset()
        {
            s_Ticks = Array.Empty<ulong>();
            s_RenderFrames = Array.Empty<ulong>();
            s_ElapsedNanoseconds = Array.Empty<long>();
            s_PhaseNanoseconds = new long[0, 0];
            s_PhaseStack = Array.Empty<int>();
            s_PhaseStartedAt = Array.Empty<long>();
            s_Count = 0;
            s_CurrentIndex = -1;
            s_PhaseDepth = 0;
            s_PhaseCount = 0;
            s_Overflowed = false;
            s_Active = false;
        }
    }
}
