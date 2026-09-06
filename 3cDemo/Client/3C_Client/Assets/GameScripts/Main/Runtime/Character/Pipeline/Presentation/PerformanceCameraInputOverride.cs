using System;
using ThirdPersonPerformance;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    public static class PerformanceCameraInputOverride
    {
        static PerformanceCameraTraceDocument s_Trace;
        static int s_Frame;

        public static bool IsActive => s_Trace != null;

        public static void Activate(PerformanceCameraTraceDocument trace)
        {
            if (trace == null || !string.Equals(trace.schema, PerformanceCaptureSchemas.CameraTrace, StringComparison.Ordinal) ||
                string.IsNullOrWhiteSpace(trace.trace_id) || string.IsNullOrWhiteSpace(trace.input_id) ||
                trace.revision <= 0 || trace.tick_rate <= 0 || trace.frame_count <= 0 || trace.segments == null || trace.segments.Length == 0)
            {
                throw new ArgumentException("Performance camera trace is incomplete.", nameof(trace));
            }
            int covered = 0;
            for (int i = 0; i < trace.segments.Length; i++)
            {
                PerformanceCameraTraceSegmentDocument segment = trace.segments[i] ??
                    throw new ArgumentException("Performance camera trace contains a null segment.", nameof(trace));
                if (segment.first_frame != covered || segment.frame_count <= 0 ||
                    !float.IsFinite(segment.x) || !float.IsFinite(segment.y))
                {
                    throw new ArgumentException("Performance camera trace segments are not contiguous.", nameof(trace));
                }
                covered = checked(covered + segment.frame_count);
            }
            if (covered != trace.frame_count)
                throw new ArgumentException("Performance camera trace coverage does not match its frame count.", nameof(trace));
            s_Trace = trace;
            s_Frame = 0;
        }

        public static void SetFrame(int frame)
        {
            if (s_Trace == null || frame < 0 || frame >= s_Trace.frame_count)
                throw new ArgumentOutOfRangeException(nameof(frame));
            s_Frame = frame;
        }

        public static bool TryGet(string inputId, out Vector2 value)
        {
            value = default;
            if (s_Trace == null || !string.Equals(inputId, s_Trace.input_id, StringComparison.Ordinal))
                return false;
            for (int i = 0; i < s_Trace.segments.Length; i++)
            {
                PerformanceCameraTraceSegmentDocument segment = s_Trace.segments[i];
                if (s_Frame < segment.first_frame || s_Frame >= segment.first_frame + segment.frame_count)
                    continue;
                value = new Vector2(segment.x, segment.y);
                return true;
            }
            throw new InvalidOperationException("Performance camera trace frame has no segment.");
        }

        public static void Clear()
        {
            s_Trace = null;
            s_Frame = 0;
        }
    }
}
