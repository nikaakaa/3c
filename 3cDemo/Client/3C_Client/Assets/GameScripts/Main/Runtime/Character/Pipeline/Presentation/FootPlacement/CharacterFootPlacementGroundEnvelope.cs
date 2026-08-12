using System;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    public enum FootPlacementGroundEnvelopeRejectReason : byte
    {
        None = 0,
        NoCandidate = 1,
        HeightDiscontinuity = 2,
        EdgeGap = 3,
        SurfaceDiscontinuity = 4,
        ReachExceeded = 5,
        SlopeExceeded = 6,
        StepExceeded = 7,
        InvalidCandidate = 8,
        UnsupportedCenter = 9
    }

    internal readonly struct FootPlacementGroundEnvelopeSegment
    {
        public FootPlacementGroundEnvelopeSegment(
            float startFraction,
            float endFraction,
            FootPlacementSurface surface,
            Vector3 edgeStart,
            Vector3 edgeEnd,
            Vector3 rootStart,
            Vector3 rootEnd,
            Vector3 hipStart,
            Vector3 hipEnd,
            float startSoleHeight,
            float endSoleHeight,
            bool virtualPlane)
        {
            StartFraction = Mathf.Clamp01(startFraction);
            EndFraction = Mathf.Clamp01(endFraction);
            Surface = surface;
            EdgeStart = edgeStart;
            EdgeEnd = edgeEnd;
            RootStart = rootStart;
            RootEnd = rootEnd;
            HipStart = hipStart;
            HipEnd = hipEnd;
            StartSoleHeight = startSoleHeight;
            EndSoleHeight = endSoleHeight;
            IsVirtualPlane = virtualPlane;
        }

        public float StartFraction { get; }
        public float EndFraction { get; }
        public FootPlacementSurface Surface { get; }
        public Vector3 EdgeStart { get; }
        public Vector3 EdgeEnd { get; }
        public Vector3 RootStart { get; }
        public Vector3 RootEnd { get; }
        public Vector3 HipStart { get; }
        public Vector3 HipEnd { get; }
        public float StartSoleHeight { get; }
        public float EndSoleHeight { get; }
        public float MinimumSoleHeight => Mathf.Max(StartSoleHeight, EndSoleHeight);
        public bool IsVirtualPlane { get; }
        public bool Contains(float fraction) => fraction >= StartFraction && fraction <= EndFraction;

        public float Evaluate(float fraction)
        {
            float length = EndFraction - StartFraction;
            if (length <= 0.000001f)
                return EndSoleHeight;
            return Mathf.Lerp(
                StartSoleHeight,
                EndSoleHeight,
                Mathf.Clamp01((fraction - StartFraction) / length));
        }
    }

    internal readonly struct FootPlacementGroundEnvelope
    {
        readonly FootPlacementGroundEnvelopeSegment[] m_Segments;

        public FootPlacementGroundEnvelope(
            FootPlacementGroundEnvelopeSegment[] segments,
            int count,
            FootPlacementGroundEnvelopeRejectReason rejectReason)
        {
            m_Segments = segments ?? throw new ArgumentNullException(nameof(segments));
            Count = Mathf.Clamp(count, 0, segments.Length);
            RejectReason = rejectReason;
        }

        public int Count { get; }
        public FootPlacementGroundEnvelopeRejectReason RejectReason { get; }
        public bool IsValid => Count > 0;

        public float MaximumMinimumSoleHeight
        {
            get
            {
                if (!IsValid)
                    throw new InvalidOperationException("Ground Envelope has no segments.");
                float value = m_Segments[0].MinimumSoleHeight;
                for (int i = 1; i < Count; i++)
                    value = Mathf.Max(value, m_Segments[i].MinimumSoleHeight);
                return value;
            }
        }

        public FootPlacementGroundEnvelopeSegment GetSegment(int index)
        {
            if (index < 0 || index >= Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            return m_Segments[index];
        }

        public float EvaluateMinimumSoleHeight(float fraction)
        {
            if (!IsValid)
                throw new InvalidOperationException("Ground Envelope has no segments.");
            float value = Mathf.Clamp01(fraction);
            for (int i = 0; i < Count; i++)
            {
                if (m_Segments[i].Contains(value))
                    return m_Segments[i].Evaluate(value);
            }
            return value < m_Segments[0].StartFraction
                ? m_Segments[0].StartSoleHeight
                : m_Segments[Count - 1].EndSoleHeight;
        }

        public int CopyTo(FootPlacementGroundEnvelopeSegment[] destination)
        {
            if (destination == null || destination.Length < Count)
                throw new ArgumentException("Ground Envelope destination is too small.", nameof(destination));
            Array.Copy(m_Segments, destination, Count);
            return Count;
        }
    }
}
