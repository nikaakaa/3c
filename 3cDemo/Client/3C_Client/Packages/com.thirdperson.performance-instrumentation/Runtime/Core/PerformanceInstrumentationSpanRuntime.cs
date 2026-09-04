using System;
using System.Threading;

namespace ThirdPersonPerformance.Instrumentation
{
    public struct PerformanceSpanRecord
    {
        public ulong Sequence;
        public ulong PointId;
        public ulong MetricId;
        public ulong RenderFrame;
        public ulong LogicTick;
        public ulong ActorId;
        public ulong ProgramIdentity;
        public ulong PipelineIdentity;
        public long StartedAt;
        public long DurationTicks;
        public int ThreadId;
        public PerformanceInstrumentationContextFlags ContextFlags;
        public PerformanceProbeEndState EndState;
    }

    public sealed class PerformanceSpanBuffer
    {
        PerformanceSpanRecord[] m_Records = Array.Empty<PerformanceSpanRecord>();
        int m_Count;
        int m_Overflowed;
        long m_LastPointId;

        public int Capacity => m_Records.Length;
        public int Count => Math.Min(Math.Max(Volatile.Read(ref m_Count), 0), m_Records.Length);
        public int AttemptedCount => Math.Max(Volatile.Read(ref m_Count), 0);
        public ulong LastPointId => unchecked((ulong)Volatile.Read(ref m_LastPointId));
        public ulong LastSequence => AttemptedCount == 0 ? 0UL : (ulong)(AttemptedCount - 1);
        public bool Overflowed => Volatile.Read(ref m_Overflowed) != 0;

        public void Begin(int capacity)
        {
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));
            m_Records = new PerformanceSpanRecord[capacity];
            Reset();
        }

        public void Reset()
        {
            Volatile.Write(ref m_Count, 0);
            Volatile.Write(ref m_Overflowed, 0);
            Volatile.Write(ref m_LastPointId, 0L);
        }

        public bool TryWrite(
            ulong pointId,
            ulong metricId,
            in PerformanceInstrumentationContext context,
            long startedAt,
            long durationTicks,
            int threadId,
            PerformanceProbeEndState endState)
        {
            int index = Interlocked.Increment(ref m_Count) - 1;
            Interlocked.Exchange(ref m_LastPointId, unchecked((long)pointId));
            if (index < 0 || index >= m_Records.Length)
            {
                Volatile.Write(ref m_Overflowed, 1);
                return false;
            }

            m_Records[index] = new PerformanceSpanRecord
            {
                Sequence = (ulong)index,
                PointId = pointId,
                MetricId = metricId,
                RenderFrame = context.RenderFrame,
                LogicTick = context.LogicTick,
                ActorId = context.ActorId,
                ProgramIdentity = context.ProgramIdentity,
                PipelineIdentity = context.PipelineIdentity,
                StartedAt = startedAt,
                DurationTicks = durationTicks,
                ThreadId = threadId,
                ContextFlags = context.Flags,
                EndState = endState
            };
            return true;
        }

        public PerformanceSpanRecord[] Complete()
        {
            int count = Count;
            var result = new PerformanceSpanRecord[count];
            Array.Copy(m_Records, result, count);
            return result;
        }

        public void Clear()
        {
            m_Records = Array.Empty<PerformanceSpanRecord>();
            Reset();
        }
    }

    public static class PerformanceInstrumentationSpanRuntime
    {
        static PerformanceInstrumentationMode s_Mode;
        static PerformanceSpanBuffer s_Buffer;
        static bool s_Capturing;
        static bool s_Faulted;

        public static PerformanceInstrumentationMode Mode => s_Mode;
        public static bool IsCapturing => s_Capturing;
        public static bool Faulted => s_Faulted || (s_Buffer != null && s_Buffer.Overflowed);
        public static PerformanceSpanBuffer SpanBuffer => s_Buffer;

        public static void Configure(PerformanceInstrumentationMode mode, PerformanceSpanBuffer buffer)
        {
            if (!Enum.IsDefined(typeof(PerformanceInstrumentationMode), mode))
                throw new ArgumentOutOfRangeException(nameof(mode));
            if (mode == PerformanceInstrumentationMode.Span && buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (mode == PerformanceInstrumentationMode.Span && buffer.Capacity <= 0)
                throw new ArgumentException("Performance Span buffer has no capacity.", nameof(buffer));
            if (s_Capturing)
                throw new InvalidOperationException("Performance instrumentation is capturing.");
            s_Mode = mode;
            s_Buffer = buffer;
            s_Faulted = false;
        }

        public static void BeginCapture(int contextDepth = 32)
        {
            if (s_Mode == PerformanceInstrumentationMode.Disabled)
                throw new InvalidOperationException("Performance instrumentation is disabled.");
            if (s_Capturing)
                throw new InvalidOperationException("Performance instrumentation capture is already active.");
            s_Buffer?.Reset();
            PerformanceInstrumentationContextRuntime.Prepare(contextDepth);
            s_Faulted = false;
            s_Capturing = true;
        }

        public static PerformanceSpanRecord[] EndCapture()
        {
            if (!s_Capturing)
                throw new InvalidOperationException("Performance instrumentation capture is not active.");
            s_Capturing = false;
            PerformanceInstrumentationContextRuntime.Clear();
            return s_Mode == PerformanceInstrumentationMode.Span
                ? s_Buffer.Complete()
                : Array.Empty<PerformanceSpanRecord>();
        }

        public static void CancelCapture()
        {
            s_Capturing = false;
            s_Faulted = false;
            PerformanceInstrumentationContextRuntime.Clear();
            s_Buffer?.Clear();
        }

        public static bool TryWrite(
            ulong pointId,
            ulong metricId,
            in PerformanceInstrumentationContext context,
            long startedAt,
            long durationTicks,
            int threadId,
            PerformanceProbeEndState endState)
        {
            if (!s_Capturing || s_Mode != PerformanceInstrumentationMode.Span)
                return true;
            if (s_Buffer.TryWrite(
                pointId,
                metricId,
                in context,
                startedAt,
                durationTicks,
                threadId,
                endState))
            {
                return true;
            }
            s_Faulted = true;
            return false;
        }
    }
}
