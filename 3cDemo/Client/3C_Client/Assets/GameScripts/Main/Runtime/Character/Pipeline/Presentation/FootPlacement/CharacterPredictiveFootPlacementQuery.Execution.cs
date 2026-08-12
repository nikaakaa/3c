using System;
using ThirdPersonCharacter.Pipeline.Animation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    internal sealed partial class CharacterPredictiveFootPlacementQuery
    {
        struct QueryCounters
        {
            internal int QueryCount;
            internal int RawHitCount;
            internal int AcceptedHitCount;
            internal int EdgePlaneCandidateCount;
            internal int AcceptedEdgePlaneCount;
            internal int RejectedCount;
            internal CharacterPredictiveFootQueryRejectCounts RejectCounts;
        }

        internal CharacterPredictiveFootPlacementQueryResult Query(
            int footIndex,
            Vector3 nativeSole,
            in AnimationPredictedFootStepSample step,
            in CharacterPredictiveFootRootTrajectory rootTrajectory,
            float virtualGroundSplitFraction,
            ulong virtualGroundSplitLandingEventIdentity,
            int layerMask,
            Vector3 up,
            float maximumReach,
            out CharacterPredictiveFootRootTrajectory resolvedTrajectory)
        {
            return Query(
                footIndex,
                nativeSole,
                in step,
                in rootTrajectory,
                virtualGroundSplitFraction,
                virtualGroundSplitLandingEventIdentity,
                layerMask,
                up,
                maximumReach,
                true,
                out resolvedTrajectory);
        }

        CharacterPredictiveFootPlacementQueryResult Query(
            int footIndex,
            Vector3 nativeSole,
            in AnimationPredictedFootStepSample step,
            in CharacterPredictiveFootRootTrajectory rootTrajectory,
            float virtualGroundSplitFraction,
            ulong virtualGroundSplitLandingEventIdentity,
            int layerMask,
            Vector3 up,
            float maximumReach,
            bool resolveTerrainProgress,
            out CharacterPredictiveFootRootTrajectory resolvedTrajectory)
        {
            if (!float.IsFinite(maximumReach) || maximumReach <= 0f)
                throw new ArgumentOutOfRangeException(nameof(maximumReach));
            m_QueryRequestCount = 0;
            m_AcceptedSupportCount = 0;
            m_RejectedGeometryCount = 0;
            resolvedTrajectory = rootTrajectory;
            var counters = new QueryCounters();
            int routeSampleCount = BuildRouteFractions(
                ResolveRouteSampleCount(
                in step,
                in rootTrajectory,
                up),
                virtualGroundSplitFraction);
            bool hasAnimationRoute = BuildAnimationRoute(
                routeSampleCount,
                in step,
                in rootTrajectory,
                up);
            FootPlacementSurface future = default;
            FootPlacementSurface virtualGroundSplitSupport = default;
            CharacterFootPlacementQueryRequest futureLandingRequest = default;
            int pathSampleCount = 0;
            FootPlacementGroundEnvelopeRejectReason rejectReason =
                FootPlacementGroundEnvelopeRejectReason.None;
            if (hasAnimationRoute)
            {
                float startPhase = rootTrajectory.PathStartPhase;
                float nativeSoleHeight = Vector3.Dot(nativeSole, up);
                float startClearance = EvaluateAnimationClearanceHeight(in step, startPhase);
                Vector3 startSoleToAnkle = rootTrajectory.EvaluateSoleToAnkle(startPhase);
                FootPlacementSurface currentSupport = QuerySupport(
                    footIndex,
                    m_FootRoutes[0],
                    m_RootRoutes[0],
                    m_HipRoutes[0],
                    layerMask,
                    up,
                    nativeSole,
                    nativeSoleHeight,
                    CharacterFootPlacementQueryPurpose.GroundEnvelope,
                    startClearance,
                    startSoleToAnkle,
                    EvaluateSupportedAuthoredReach(
                        m_FootRoutes[0],
                        m_HipRoutes[0],
                        nativeSoleHeight,
                        up,
                        startClearance,
                        startSoleToAnkle),
                    maximumReach,
                    false,
                    out _,
                    out _,
                    out rejectReason,
                    out _,
                    ref counters.QueryCount,
                    ref counters.RawHitCount,
                    ref counters.AcceptedHitCount,
                    ref counters.RejectedCount,
                    ref counters.RejectCounts);
                if (currentSupport.IsValid)
                {
                    float currentSupportHeight = Vector3.Dot(currentSupport.Point, up);
                    future = CollectGroundPath(
                        footIndex,
                        currentSupport.Point,
                        currentSupportHeight,
                        routeSampleCount,
                        in step,
                        in rootTrajectory,
                        layerMask,
                        up,
                        maximumReach,
                        virtualGroundSplitFraction,
                        ref counters,
                        out pathSampleCount,
                        out futureLandingRequest,
                        out virtualGroundSplitSupport,
                        out rejectReason);
                }
            }
            else
            {
                rejectReason = FootPlacementGroundEnvelopeRejectReason.InvalidCandidate;
            }

            int discoveredSegmentCount = pathSampleCount > 1
                ? BuildUpperEnvelope(pathSampleCount, virtualGroundSplitFraction)
                : 0;
            if (resolveTerrainProgress &&
                TryBuildTerrainProgress(
                    in rootTrajectory,
                    up,
                    discoveredSegmentCount,
                    out resolvedTrajectory))
            {
                CharacterPredictiveFootRootTrajectory terrainTrajectory = resolvedTrajectory;
                return Query(
                    footIndex,
                    nativeSole,
                    in step,
                    in terrainTrajectory,
                    virtualGroundSplitFraction,
                    virtualGroundSplitLandingEventIdentity,
                    layerMask,
                    up,
                    maximumReach,
                    false,
                    out resolvedTrajectory);
            }
            int segmentCount = future.IsValid ? discoveredSegmentCount : 0;
            if (!future.IsValid && rejectReason == FootPlacementGroundEnvelopeRejectReason.None)
                rejectReason = FootPlacementGroundEnvelopeRejectReason.NoCandidate;
            return new CharacterPredictiveFootPlacementQueryResult(
                future,
                new FootPlacementGroundEnvelope(
                    m_Segments,
                    segmentCount,
                    future.IsValid ? FootPlacementGroundEnvelopeRejectReason.None : rejectReason),
                futureLandingRequest,
                virtualGroundSplitFraction,
                virtualGroundSplitSupport,
                virtualGroundSplitLandingEventIdentity,
                routeSampleCount,
                counters.QueryCount,
                counters.RawHitCount,
                counters.AcceptedHitCount,
                counters.EdgePlaneCandidateCount,
                counters.AcceptedEdgePlaneCount,
                counters.RejectedCount,
                counters.RejectCounts,
                m_QueryRequests,
                m_QueryRequestCount,
                m_AcceptedSupports,
                m_AcceptedSupportCount,
                m_RejectedGeometry,
                m_RejectedGeometryCount);
        }

        FootPlacementSurface CollectGroundPath(
            int footIndex,
            Vector3 currentSupport,
            float currentSupportHeight,
            int routeSampleCount,
            in AnimationPredictedFootStepSample step,
            in CharacterPredictiveFootRootTrajectory rootTrajectory,
            int layerMask,
            Vector3 up,
            float maximumReach,
            float virtualGroundSplitFraction,
            ref QueryCounters counters,
            out int pathSampleCount,
            out CharacterFootPlacementQueryRequest futureLandingRequest,
            out FootPlacementSurface virtualGroundSplitSupport,
            out FootPlacementGroundEnvelopeRejectReason rejectReason)
        {
            pathSampleCount = 1;
            m_PathSamples[0] = new FootPathSample(
                0f,
                default,
                currentSupport,
                m_RootRoutes[0],
                m_HipRoutes[0],
                currentSupportHeight,
                true);
            Vector3 previousRoute = m_FootRoutes[0];
            Vector3 previousSupport = currentSupport;
            float previousFraction = 0f;
            FootPlacementSurface future = default;
            futureLandingRequest = default;
            virtualGroundSplitSupport = default;
            rejectReason = FootPlacementGroundEnvelopeRejectReason.None;
            for (int sampleIndex = 1; sampleIndex < routeSampleCount; sampleIndex++)
            {
                float fraction = m_RouteFractions[sampleIndex];
                bool landingSample = sampleIndex == routeSampleCount - 1;
                Vector3 rawRoute = m_FootRoutes[sampleIndex];
                Vector3 route = rawRoute;
                float eventPhase = Mathf.Lerp(rootTrajectory.PathStartPhase, 1f, fraction);
                Vector3 soleToAnkle = rootTrajectory.EvaluateSoleToAnkle(eventPhase);
                float animationClearance = EvaluateAnimationClearanceHeight(in step, eventPhase);
                FootPlacementSurface selected = QuerySupport(
                    footIndex,
                    route,
                    m_RootRoutes[sampleIndex],
                    m_HipRoutes[sampleIndex],
                    layerMask,
                    up,
                    previousSupport,
                    currentSupportHeight,
                    landingSample
                        ? CharacterFootPlacementQueryPurpose.FutureLanding
                        : CharacterFootPlacementQueryPurpose.GroundEnvelope,
                    animationClearance,
                    soleToAnkle,
                    EvaluateSupportedAuthoredReach(
                        route,
                        m_HipRoutes[sampleIndex],
                        currentSupportHeight,
                        up,
                        animationClearance,
                        soleToAnkle),
                    maximumReach,
                    true,
                    out Vector3 selectedRoot,
                    out Vector3 selectedHip,
                    out FootPlacementGroundEnvelopeRejectReason sampleReject,
                    out CharacterFootPlacementQueryRequest request,
                    ref counters.QueryCount,
                    ref counters.RawHitCount,
                    ref counters.AcceptedHitCount,
                    ref counters.RejectedCount,
                    ref counters.RejectCounts);
                if (landingSample)
                    futureLandingRequest = request;
                if (selected.IsValid)
                {
                    if (Mathf.Abs(fraction - virtualGroundSplitFraction) <= 0.00001f)
                        virtualGroundSplitSupport = selected;
                    AddSegmentHits(
                        footIndex,
                        previousRoute,
                        route,
                        previousSupport,
                        selected.Point,
                        currentSupportHeight,
                        previousFraction,
                        fraction,
                        m_RootRoutes[sampleIndex - 1],
                        m_RootRoutes[sampleIndex],
                        m_HipRoutes[sampleIndex - 1],
                        m_HipRoutes[sampleIndex],
                        in rootTrajectory,
                        in step,
                        layerMask,
                        up,
                        maximumReach,
                        ref pathSampleCount,
                        ref counters.QueryCount,
                        ref counters.RawHitCount,
                        ref counters.AcceptedHitCount,
                        ref counters.EdgePlaneCandidateCount,
                        ref counters.RejectedCount,
                        ref counters.RejectCounts);
                    AddPathSample(
                        new FootPathSample(
                            fraction,
                            selected,
                            selected.Point,
                            selectedRoot,
                            selectedHip,
                            Vector3.Dot(selected.Point, up),
                            true),
                        ref pathSampleCount);
                    previousSupport = selected.Point;
                    if (landingSample)
                        future = selected;
                }
                else
                {
                    rejectReason = sampleReject;
                    future = default;
                    break;
                }
                previousRoute = rawRoute;
                previousFraction = fraction;
            }
            ResolveEdgePlanes(
                counters.EdgePlaneCandidateCount,
                up,
                maximumReach,
                ref pathSampleCount,
                ref counters.AcceptedHitCount,
                ref counters.AcceptedEdgePlaneCount,
                ref counters.RejectedCount,
                ref counters.RejectCounts);
            RemoveCoincidentNonSupportSamples(ref pathSampleCount);
            SortAndCollapsePathSamples(ref pathSampleCount);
            return future;
        }

        void RemoveCoincidentNonSupportSamples(ref int count)
        {
            const float fractionTolerance = 0.00001f;
            int write = 0;
            for (int read = 0; read < count; read++)
            {
                FootPathSample candidate = m_PathSamples[read];
                if (!candidate.IsSupport)
                {
                    bool hasOfficialSupport = false;
                    for (int supportIndex = 0; supportIndex < count; supportIndex++)
                    {
                        FootPathSample support = m_PathSamples[supportIndex];
                        if (!support.IsSupport ||
                            Mathf.Abs(support.Fraction - candidate.Fraction) > fractionTolerance)
                        {
                            continue;
                        }
                        hasOfficialSupport = true;
                        break;
                    }
                    if (hasOfficialSupport)
                        continue;
                }
                m_PathSamples[write++] = candidate;
            }
            count = write;
        }

        static float EvaluateSupportedAuthoredReach(
            Vector3 route,
            Vector3 hip,
            float supportHeight,
            Vector3 up,
            float animationClearance,
            Vector3 soleToAnkle)
        {
            Vector3 baselineSupport = route +
                                      up * (supportHeight - Vector3.Dot(route, up));
            Vector3 baselineAnkle = baselineSupport +
                                    up * animationClearance +
                                    soleToAnkle;
            return Vector3.Distance(hip, baselineAnkle);
        }

        bool BuildAnimationRoute(
            int routeSampleCount,
            in AnimationPredictedFootStepSample step,
            in CharacterPredictiveFootRootTrajectory rootTrajectory,
            Vector3 up)
        {
            if (routeSampleCount < 2 || routeSampleCount > MaximumRouteSampleCount)
                return false;
            for (int sampleIndex = 0; sampleIndex < routeSampleCount; sampleIndex++)
            {
                float fraction = m_RouteFractions[sampleIndex];
                EvaluateWorldRoute(
                    in step,
                    in rootTrajectory,
                    fraction,
                    up,
                    out Vector3 foot,
                    out Vector3 root,
                    out Vector3 hip);
                float eventPhase = Mathf.Lerp(
                    rootTrajectory.PathStartPhase,
                    1f,
                    Mathf.Clamp01(fraction));
                hip = rootTrajectory.EvaluateHipRoute(eventPhase);
                if (!IsFinite(foot) || !IsFinite(root) || !IsFinite(hip))
                    return false;
                m_FootRoutes[sampleIndex] = foot;
                m_RootRoutes[sampleIndex] = root;
                m_HipRoutes[sampleIndex] = hip;
            }
            return true;
        }

        int BuildRouteFractions(int uniformCount, float splitFraction)
        {
            if (uniformCount < 2 || uniformCount > MaximumRouteSampleCount)
                return 0;
            for (int i = 0; i < uniformCount; i++)
                m_RouteFractions[i] = i / (uniformCount - 1f);
            if (splitFraction <= 0.0001f || splitFraction >= 0.9999f)
                return uniformCount;
            for (int i = 1; i < uniformCount - 1; i++)
            {
                if (Mathf.Abs(m_RouteFractions[i] - splitFraction) <= 0.00001f)
                    return uniformCount;
            }
            int count = uniformCount;
            if (count < MaximumRouteSampleCount)
            {
                m_RouteFractions[count++] = splitFraction;
            }
            else
            {
                int nearest = 1;
                float nearestDistance = Mathf.Abs(m_RouteFractions[nearest] - splitFraction);
                for (int i = 2; i < count - 1; i++)
                {
                    float distance = Mathf.Abs(m_RouteFractions[i] - splitFraction);
                    if (distance >= nearestDistance)
                        continue;
                    nearest = i;
                    nearestDistance = distance;
                }
                m_RouteFractions[nearest] = splitFraction;
            }
            Array.Sort(m_RouteFractions, 0, count);
            return count;
        }

    }
}
