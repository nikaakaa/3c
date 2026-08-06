using System;
using RootMotion.FinalIK;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    internal readonly struct CharacterPredictiveFootPlacementQueryResult
    {
        internal CharacterPredictiveFootPlacementQueryResult(
            FootPlacementSurface futureLandingSupport,
            FootPlacementGroundEnvelope groundEnvelope,
            float swingClearance,
            int queryCount,
            int rejectedCount)
        {
            FutureLandingSupport = futureLandingSupport;
            GroundEnvelope = groundEnvelope;
            SwingClearance = swingClearance;
            QueryCount = queryCount;
            RejectedCount = rejectedCount;
        }

        internal FootPlacementSurface FutureLandingSupport { get; }
        internal FootPlacementGroundEnvelope GroundEnvelope { get; }
        internal float SwingClearance { get; }
        internal int QueryCount { get; }
        internal int RejectedCount { get; }
        internal bool HasFutureLandingSupport => FutureLandingSupport.IsValid;
    }

    internal sealed class CharacterPredictiveFootPlacementQuery
    {
        readonly CharacterFootPlacementWorldQueryBackend m_World;
        readonly CharacterPredictiveFootPlacementRuntimeSettings m_Settings;
        readonly FootPlacementGroundEnvelopeSegment[] m_Segments;

        internal CharacterPredictiveFootPlacementQuery(
            CharacterFootPlacementWorldQueryBackend world,
            CharacterPredictiveFootPlacementRuntimeSettings settings)
        {
            m_World = world ?? throw new ArgumentNullException(nameof(world));
            m_Settings = settings;
            m_Segments = new FootPlacementGroundEnvelopeSegment[settings.PathSampleCount + 1];
        }

        internal CharacterPredictiveFootPlacementQueryResult Query(
            int footIndex,
            Vector3 currentSole,
            Vector3 predictedSole,
            Vector3 hip,
            float legLength,
            int layerMask)
        {
            int queryCount = 0;
            int rejectedCount = 0;
            int segmentCount = 0;
            FootPlacementSurface future = default;
            FootPlacementGroundEnvelopeRejectReason rejectReason = FootPlacementGroundEnvelopeRejectReason.None;
            FootPlacementSurface previous = default;
            int sampleCount = m_Settings.PathSampleCount + 1;
            for (int sampleIndex = 1; sampleIndex <= sampleCount; sampleIndex++)
            {
                float fraction = sampleIndex / (float)sampleCount;
                Vector3 sample = Vector3.Lerp(currentSole, predictedSole, fraction);
                var groundingRequest = new GroundingQueryRequest(
                    GroundingQueryShape.Sphere,
                    sampleIndex == sampleCount
                        ? GroundingQueryPurpose.FutureLanding
                        : GroundingQueryPurpose.GroundEnvelope,
                    m_World.PhysicsScene,
                    layerMask,
                    footIndex,
                    sample + Vector3.up * m_Settings.CastAbove,
                    Vector3.zero,
                    Vector3.down,
                    m_Settings.PathSphereRadius,
                    m_Settings.CastAbove + m_Settings.CastBelow);
                queryCount++;
                FootPlacementGroundEnvelopeRejectReason candidateReject =
                    FootPlacementGroundEnvelopeRejectReason.None;
                GroundingQueryHit hit;
                bool hasHit;
                if (sampleIndex == sampleCount)
                {
                    var request = new CharacterFutureLandingQueryRequest(in groundingRequest);
                    hasHit = m_World.Query(in request, out hit);
                }
                else
                {
                    var request = new CharacterPathSampleQueryRequest(
                        CharacterPathSampleQueryKind.GroundEnvelope,
                        in groundingRequest);
                    hasHit = m_World.Query(in request, out hit);
                }
                if (!hasHit ||
                    !Accept(hit, currentSole.y, hip, legLength, out candidateReject))
                {
                    rejectedCount++;
                    if (rejectReason == FootPlacementGroundEnvelopeRejectReason.None)
                        rejectReason = candidateReject == FootPlacementGroundEnvelopeRejectReason.None
                            ? FootPlacementGroundEnvelopeRejectReason.NoCandidate
                            : candidateReject;
                    break;
                }
                var surface = new FootPlacementSurface(hit.PhysicsHit.collider, hit.Point, hit.Normal.normalized);
                if (previous.IsValid &&
                    (Mathf.Abs(surface.Point.y - previous.Point.y) > m_Settings.MaximumHeightDiscontinuity ||
                     Vector3.Distance(surface.Point, previous.Point) > m_Settings.MaximumEdgeGap))
                {
                    rejectedCount++;
                    rejectReason = FootPlacementGroundEnvelopeRejectReason.SurfaceDiscontinuity;
                    break;
                }
                float start = (sampleIndex - 1f) / sampleCount;
                m_Segments[segmentCount++] = new FootPlacementGroundEnvelopeSegment(
                    start,
                    fraction,
                    surface,
                    previous.IsValid ? previous.Point : currentSole,
                    surface.Point,
                    Mathf.Max(currentSole.y, surface.Point.y),
                    true);
                previous = surface;
                if (sampleIndex == sampleCount)
                    future = surface;
            }

            float swingClearance = QuerySwingClearance(
                footIndex,
                currentSole,
                predictedSole,
                layerMask,
                ref queryCount);
            if (!future.IsValid && rejectReason == FootPlacementGroundEnvelopeRejectReason.None)
                rejectReason = FootPlacementGroundEnvelopeRejectReason.NoCandidate;
            return new CharacterPredictiveFootPlacementQueryResult(
                future,
                new FootPlacementGroundEnvelope(
                    m_Segments,
                    segmentCount,
                    future.IsValid ? FootPlacementGroundEnvelopeRejectReason.None : rejectReason),
                swingClearance,
                queryCount,
                rejectedCount);
        }

        float QuerySwingClearance(
            int footIndex,
            Vector3 start,
            Vector3 end,
            int layerMask,
            ref int queryCount)
        {
            Vector3 path = end - start;
            float distance = path.magnitude;
            if (distance <= 0.0001f)
                return 0f;
            Vector3 origin = start + Vector3.up * m_Settings.CastAbove;
            var groundingRequest = new GroundingQueryRequest(
                GroundingQueryShape.Capsule,
                GroundingQueryPurpose.SwingClearance,
                m_World.PhysicsScene,
                layerMask,
                footIndex,
                origin,
                origin + Vector3.up * (m_Settings.SwingCapsuleRadius * 2f),
                path / distance,
                m_Settings.SwingCapsuleRadius,
                distance);
            var request = new CharacterPathSampleQueryRequest(
                CharacterPathSampleQueryKind.SwingClearance,
                in groundingRequest);
            queryCount++;
            if (!m_World.Query(in request, out GroundingQueryHit hit))
                return 0f;
            return Mathf.Clamp(
                hit.Point.y + m_Settings.SwingCapsuleRadius - Mathf.Lerp(start.y, end.y, hit.Distance / distance),
                0f,
                m_Settings.MaximumSwingClearance);
        }

        bool Accept(
            GroundingQueryHit hit,
            float currentSoleHeight,
            Vector3 hip,
            float legLength,
            out FootPlacementGroundEnvelopeRejectReason rejectReason)
        {
            if (!hit.HasHit || !hit.PhysicsHit.collider)
            {
                rejectReason = FootPlacementGroundEnvelopeRejectReason.NoCandidate;
                return false;
            }
            if (Vector3.Angle(Vector3.up, hit.Normal) > m_Settings.MaximumSlopeDegrees)
            {
                rejectReason = FootPlacementGroundEnvelopeRejectReason.SlopeExceeded;
                return false;
            }
            float heightDelta = hit.Point.y - currentSoleHeight;
            if (heightDelta > m_Settings.MaximumStepUp || heightDelta < -m_Settings.MaximumStepDown)
            {
                rejectReason = FootPlacementGroundEnvelopeRejectReason.StepExceeded;
                return false;
            }
            if (Vector3.Distance(hip, hit.Point) > legLength * 1.05f)
            {
                rejectReason = FootPlacementGroundEnvelopeRejectReason.ReachExceeded;
                return false;
            }
            rejectReason = FootPlacementGroundEnvelopeRejectReason.None;
            return true;
        }
    }
}
