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
        InvalidCandidate = 8
    }

    internal readonly struct FootPlacementGroundEnvelopeSegment
    {
        public FootPlacementGroundEnvelopeSegment(
            float startFraction,
            float endFraction,
            FootPlacementSurface surface,
            Vector3 edgeStart,
            Vector3 edgeEnd,
            float minimumSoleHeight,
            bool virtualPlane)
        {
            StartFraction = Mathf.Clamp01(startFraction);
            EndFraction = Mathf.Clamp01(endFraction);
            Surface = surface;
            EdgeStart = edgeStart;
            EdgeEnd = edgeEnd;
            MinimumSoleHeight = minimumSoleHeight;
            IsVirtualPlane = virtualPlane;
        }

        public float StartFraction { get; }
        public float EndFraction { get; }
        public FootPlacementSurface Surface { get; }
        public Vector3 EdgeStart { get; }
        public Vector3 EdgeEnd { get; }
        public float MinimumSoleHeight { get; }
        public bool IsVirtualPlane { get; }
        public bool Contains(float fraction) => fraction >= StartFraction && fraction <= EndFraction;
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

        public FootPlacementGroundEnvelopeSegment GetSegment(int index)
        {
            if (index < 0 || index >= Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            return m_Segments[index];
        }
    }
}
