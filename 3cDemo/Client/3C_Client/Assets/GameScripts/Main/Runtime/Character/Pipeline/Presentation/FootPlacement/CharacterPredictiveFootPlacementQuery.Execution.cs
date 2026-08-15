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
            in AnimationPredictedFootStepSample step,
            in CharacterPredictiveFootRootTrajectory rootTrajectory,
            Vector3 groundProbeStart,
            FootPlacementSurface groundProbeSupport,
            float virtualGroundSplitEventPhase,
            ulong virtualGroundSplitLandingEventIdentity,
            int layerMask,
            Vector3 up,
            float soleSupportRadius,
            float maximumReach,
            out CharacterPredictiveFootRootTrajectory resolvedTrajectory)
        {
            return QueryCore(
                footIndex,
                in step,
                in rootTrajectory,
                groundProbeStart,
                groundProbeSupport,
                virtualGroundSplitEventPhase,
                virtualGroundSplitLandingEventIdentity,
                layerMask,
                up,
                soleSupportRadius,
                maximumReach,
                out resolvedTrajectory);
        }

        CharacterPredictiveFootPlacementQueryResult QueryCore(
            int footIndex,
            in AnimationPredictedFootStepSample step,
            in CharacterPredictiveFootRootTrajectory rootTrajectory,
            Vector3 groundProbeStart,
            FootPlacementSurface groundProbeSupport,
            float virtualGroundSplitEventPhase,
            ulong virtualGroundSplitLandingEventIdentity,
            int layerMask,
            Vector3 up,
            float soleSupportRadius,
            float maximumReach,
            out CharacterPredictiveFootRootTrajectory resolvedTrajectory)
        {
            if (!float.IsFinite(soleSupportRadius) || soleSupportRadius <= 0f)
                throw new ArgumentOutOfRangeException(nameof(soleSupportRadius));
            if (!float.IsFinite(maximumReach) || maximumReach <= 0f)
                throw new ArgumentOutOfRangeException(nameof(maximumReach));
            m_QueryRequestCount = 0;
            m_AcceptedSupportCount = 0;
            m_RejectedGeometryCount = 0;
            resolvedTrajectory = rootTrajectory;
            var counters = new QueryCounters();
            float virtualGroundSplitActionProgress = ResolveVirtualGroundSplitActionProgress(
                virtualGroundSplitEventPhase,
                in step,
                in rootTrajectory,
                out Vector3 virtualGroundOpposingLanding,
                out Vector3 virtualGroundSplitRoutePoint,
                out float virtualGroundSplitPlanarError);
            bool hasVirtualGroundSplit = virtualGroundSplitActionProgress > 0f;
            Vector3 routeStart = groundProbeStart;
            Vector3 routeEnd = rootTrajectory.EvaluateFootRoute(1f);
            ResolveGroundProbeRouteMetrics(
                routeStart,
                routeEnd,
                virtualGroundOpposingLanding,
                hasVirtualGroundSplit,
                up,
                out float routeLength,
                out float virtualGroundSplitFraction);
            int uniformRouteSampleCount = ResolveRouteSampleCount(routeLength);
            int routeSampleCount = BuildRouteEventPhases(
                uniformRouteSampleCount,
                rootTrajectory.PathStartPhase,
                virtualGroundSplitEventPhase,
                virtualGroundSplitFraction);
            bool hasGroundProbeRoute = BuildGroundProbeRoute(
                routeSampleCount,
                routeStart,
                routeEnd,
                virtualGroundSplitEventPhase,
                virtualGroundOpposingLanding,
                virtualGroundSplitFraction,
                in rootTrajectory);
            int footRateSampleCount = hasGroundProbeRoute
                ? BuildFootRate(
                    in rootTrajectory,
                    groundProbeStart,
                    routeSampleCount,
                    virtualGroundSplitEventPhase,
                    virtualGroundSplitFraction)
                : 0;
            hasGroundProbeRoute &= footRateSampleCount >= 2;
            FootPlacementSurface future = default;
            FootPlacementSurface currentSupport = groundProbeSupport;
            FootPlacementSurface virtualGroundSplitSupport = default;
            Vector3 currentSupportRoot = default;
            Vector3 currentSupportHip = default;
            Vector3 futureSupportRoot = default;
            Vector3 futureSupportHip = default;
            Vector3 virtualGroundSplitRoot = default;
            Vector3 virtualGroundSplitHip = default;
            CharacterFootPlacementQueryRequest futureLandingRequest = default;
            int pathSampleCount = 0;
            FootPlacementGroundEnvelopeRejectReason rejectReason =
                FootPlacementGroundEnvelopeRejectReason.None;
            if (hasGroundProbeRoute)
            {
                currentSupportRoot = m_RootRoutes[0];
                currentSupportHip = m_HipRoutes[0];
                bool currentSupportAccepted = currentSupport.IsValid;
                if (!currentSupportAccepted)
                    rejectReason = FootPlacementGroundEnvelopeRejectReason.NoCandidate;
                else if (Vector3.Angle(up, currentSupport.Normal) > m_Settings.MaximumSlopeDegrees)
                {
                    rejectReason = FootPlacementGroundEnvelopeRejectReason.SlopeExceeded;
                    currentSupportAccepted = false;
                }
                if (currentSupportAccepted)
                {
                    future = CollectGroundPath(
                        footIndex,
                        currentSupport,
                        currentSupportRoot,
                        currentSupportHip,
                        routeSampleCount,
                        in step,
                        in rootTrajectory,
                        layerMask,
                        up,
                        soleSupportRadius,
                        maximumReach,
                        virtualGroundSplitFraction,
                        virtualGroundSplitEventPhase,
                        virtualGroundOpposingLanding,
                        ref counters,
                        out pathSampleCount,
                        out futureLandingRequest,
                        out virtualGroundSplitSupport,
                        out futureSupportRoot,
                        out futureSupportHip,
                        out virtualGroundSplitRoot,
                        out virtualGroundSplitHip,
                        out rejectReason);
                }
            }
            else
            {
                rejectReason = FootPlacementGroundEnvelopeRejectReason.InvalidCandidate;
            }

            int discoveredSegmentCount = pathSampleCount > 1
                ? BuildUpperEnvelope(pathSampleCount)
                : 0;
            int segmentCount = future.IsValid ? discoveredSegmentCount : 0;
            bool hasCompleteBodySupportPath =
                currentSupport.IsValid &&
                future.IsValid &&
                (!hasVirtualGroundSplit || virtualGroundSplitSupport.IsValid);
            CharacterPredictiveBodySupportPath bodySupportPath = hasCompleteBodySupportPath
                ? new CharacterPredictiveBodySupportPath(
                    rootTrajectory.PathStartPhase,
                    up,
                    currentSupportRoot,
                    currentSupportHip,
                    hasVirtualGroundSplit,
                    virtualGroundSplitEventPhase,
                    virtualGroundSplitRoot,
                    virtualGroundSplitHip,
                    futureSupportRoot,
                    futureSupportHip)
                : default;
            if (!future.IsValid && rejectReason == FootPlacementGroundEnvelopeRejectReason.None)
                rejectReason = FootPlacementGroundEnvelopeRejectReason.NoCandidate;
            return new CharacterPredictiveFootPlacementQueryResult(
                future,
                new FootPlacementGroundEnvelope(
                    m_Segments,
                    segmentCount,
                    future.IsValid ? FootPlacementGroundEnvelopeRejectReason.None : rejectReason),
                bodySupportPath,
                soleSupportRadius,
                futureLandingRequest,
                virtualGroundSplitEventPhase,
                virtualGroundOpposingLanding,
                virtualGroundSplitRoutePoint,
                virtualGroundSplitPlanarError,
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
                m_RouteEventPhases,
                m_RouteFractions,
                m_GroundProbeRoute,
                footRateSampleCount,
                m_FootRateEventPhases,
                m_FootRateProgress,
                m_QueryRequests,
                m_QueryRequestCount,
                m_AcceptedSupports,
                m_AcceptedSupportCount,
                m_RejectedGeometry,
                m_RejectedGeometryCount);
        }

        FootPlacementSurface CollectGroundPath(
            int footIndex,
            FootPlacementSurface currentSupport,
            Vector3 currentSupportRoot,
            Vector3 currentSupportHip,
            int routeSampleCount,
            in AnimationPredictedFootStepSample step,
            in CharacterPredictiveFootRootTrajectory rootTrajectory,
            int layerMask,
            Vector3 up,
            float soleSupportRadius,
            float maximumReach,
            float virtualGroundSplitFraction,
            float virtualGroundSplitEventPhase,
            Vector3 virtualGroundOpposingLanding,
            ref QueryCounters counters,
            out int pathSampleCount,
            out CharacterFootPlacementQueryRequest futureLandingRequest,
            out FootPlacementSurface virtualGroundSplitSupport,
            out Vector3 futureSupportRoot,
            out Vector3 futureSupportHip,
            out Vector3 virtualGroundSplitRoot,
            out Vector3 virtualGroundSplitHip,
            out FootPlacementGroundEnvelopeRejectReason rejectReason)
        {
            pathSampleCount = 1;
            m_PathSamples[0] = new FootPathSample(
                0f,
                currentSupport,
                currentSupport.Point,
                currentSupportRoot,
                currentSupportHip,
                Vector3.Dot(currentSupport.Point, up),
                true);
            Vector3 previousRoute = m_GroundProbeRoute[0];
            Vector3 previousSupport = currentSupport.Point;
            float previousFraction = 0f;
            FootPlacementSurface future = default;
            futureLandingRequest = default;
            virtualGroundSplitSupport = default;
            futureSupportRoot = default;
            futureSupportHip = default;
            virtualGroundSplitRoot = default;
            virtualGroundSplitHip = default;
            rejectReason = FootPlacementGroundEnvelopeRejectReason.None;
            for (int sampleIndex = 1; sampleIndex < routeSampleCount; sampleIndex++)
            {
                float fraction = m_RouteFractions[sampleIndex];
                bool landingSample = sampleIndex == routeSampleCount - 1;
                Vector3 rawRoute = m_GroundProbeRoute[sampleIndex];
                Vector3 route = rawRoute;
                float eventPhase = m_RouteEventPhases[sampleIndex];
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
                    landingSample
                        ? CharacterFootPlacementQueryPurpose.FutureLanding
                        : CharacterFootPlacementQueryPurpose.GroundEnvelope,
                    animationClearance,
                    soleToAnkle,
                    EvaluateRouteAuthoredReach(
                        route,
                        m_HipRoutes[sampleIndex],
                        up,
                        animationClearance,
                        soleToAnkle),
                    maximumReach,
                    landingSample,
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
                    bool virtualGroundSample = virtualGroundSplitFraction > 0f &&
                                               Mathf.Abs(eventPhase - virtualGroundSplitEventPhase) <= 0.00001f;
                    if (virtualGroundSample)
                    {
                        virtualGroundSplitSupport = selected;
                        virtualGroundSplitRoot = selectedRoot;
                        virtualGroundSplitHip = selectedHip;
                    }
                    AddSegmentHits(
                        footIndex,
                        previousRoute,
                        route,
                        previousSupport,
                        selected.Point,
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
                        soleSupportRadius,
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
                    {
                        future = selected;
                        futureSupportRoot = selectedRoot;
                        futureSupportHip = selectedHip;
                    }
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

        static float EvaluateRouteAuthoredReach(
            Vector3 route,
            Vector3 hip,
            Vector3 up,
            float animationClearance,
            Vector3 soleToAnkle)
        {
            Vector3 baselineAnkle = route +
                                    up * animationClearance +
                                    soleToAnkle;
            return Vector3.Distance(hip, baselineAnkle);
        }

        bool BuildGroundProbeRoute(
            int routeSampleCount,
            Vector3 routeStart,
            Vector3 routeEnd,
            float splitEventPhase,
            Vector3 splitPoint,
            float splitFraction,
            in CharacterPredictiveFootRootTrajectory rootTrajectory)
        {
            if (routeSampleCount < 2 || routeSampleCount > MaximumRouteSampleCount)
                return false;
            float length = 0f;
            for (int sampleIndex = 0; sampleIndex < routeSampleCount; sampleIndex++)
            {
                float eventPhase = m_RouteEventPhases[sampleIndex];
                Vector3 foot = EvaluateGroundProbePoint(
                    eventPhase,
                    routeStart,
                    routeEnd,
                    splitEventPhase,
                    splitPoint,
                    splitFraction,
                    in rootTrajectory);
                rootTrajectory.EvaluateEventPhase(eventPhase, out Vector3 root, out _);
                Vector3 hip = rootTrajectory.EvaluateHipRoute(eventPhase);
                if (!IsFinite(foot) || !IsFinite(root) || !IsFinite(hip))
                    return false;
                m_GroundProbeRoute[sampleIndex] = foot;
                m_RootRoutes[sampleIndex] = root;
                m_HipRoutes[sampleIndex] = hip;
                if (sampleIndex > 0)
                {
                    length += Vector3.ProjectOnPlane(
                        foot - m_GroundProbeRoute[sampleIndex - 1],
                        rootTrajectory.Up).magnitude;
                }
                m_RouteFractions[sampleIndex] = length;
            }
            if (length <= 0.000001f)
                return false;
            for (int sampleIndex = 1; sampleIndex < routeSampleCount; sampleIndex++)
                m_RouteFractions[sampleIndex] /= length;
            m_RouteFractions[0] = 0f;
            m_RouteFractions[routeSampleCount - 1] = 1f;
            return true;
        }

        static void ResolveGroundProbeRouteMetrics(
            Vector3 routeStart,
            Vector3 routeEnd,
            Vector3 splitPoint,
            bool hasSplit,
            Vector3 up,
            out float routeLength,
            out float splitFraction)
        {
            float firstLength = hasSplit
                ? Vector3.ProjectOnPlane(splitPoint - routeStart, up).magnitude
                : 0f;
            float secondLength = hasSplit
                ? Vector3.ProjectOnPlane(routeEnd - splitPoint, up).magnitude
                : Vector3.ProjectOnPlane(routeEnd - routeStart, up).magnitude;
            routeLength = firstLength + secondLength;
            splitFraction = hasSplit && routeLength > 0.000001f
                ? Mathf.Clamp01(firstLength / routeLength)
                : 0f;
        }

        int BuildFootRate(
            in CharacterPredictiveFootRootTrajectory rootTrajectory,
            Vector3 groundProbeStart,
            int routeSampleCount,
            float splitEventPhase,
            float splitFraction)
        {
            int count = Mathf.Min(
                AnimationPredictedFootStepCurveSet.RouteSampleCount,
                MaximumRouteSampleCount - 1);
            for (int i = 0; i < count; i++)
            {
                m_FootRateEventPhases[i] = Mathf.Lerp(
                    rootTrajectory.PathStartPhase,
                    1f,
                    i / (count - 1f));
            }
            if (splitFraction > 0f && splitFraction < 1f &&
                !ContainsPhase(m_FootRateEventPhases, count, splitEventPhase))
            {
                m_FootRateEventPhases[count++] = splitEventPhase;
                Array.Sort(m_FootRateEventPhases, 0, count);
            }
            float previous = 0f;
            for (int i = 0; i < count; i++)
            {
                float phase = m_FootRateEventPhases[i];
                Vector3 animatedFoot = rootTrajectory.EvaluateFootRoute(phase);
                float progress = ResolveGroundProbeProjection(
                    animatedFoot,
                    routeSampleCount,
                    previous,
                    rootTrajectory.Up);
                if (!float.IsFinite(progress))
                    return 0;
                if (i == 0)
                    progress = 0f;
                else if (i == count - 1)
                    progress = 1f;
                else
                    progress = Mathf.Max(previous, progress);
                m_FootRateProgress[i] = progress;
                previous = progress;
            }
            return count;
        }

        static Vector3 EvaluateGroundProbePoint(
            float eventPhase,
            Vector3 routeStart,
            Vector3 routeEnd,
            float splitEventPhase,
            Vector3 splitPoint,
            float splitFraction,
            in CharacterPredictiveFootRootTrajectory rootTrajectory)
        {
            float pathStartPhase = rootTrajectory.PathStartPhase;
            if (splitFraction <= 0f || splitFraction >= 1f)
            {
                float progress = Mathf.InverseLerp(pathStartPhase, 1f, eventPhase);
                return Vector3.Lerp(routeStart, routeEnd, progress);
            }
            if (eventPhase <= splitEventPhase)
            {
                float progress = Mathf.InverseLerp(pathStartPhase, splitEventPhase, eventPhase);
                return Vector3.Lerp(routeStart, splitPoint, progress);
            }
            float remainingProgress = Mathf.InverseLerp(splitEventPhase, 1f, eventPhase);
            return Vector3.Lerp(splitPoint, routeEnd, remainingProgress);
        }

        float ResolveGroundProbeProjection(
            Vector3 point,
            int routeSampleCount,
            float minimumProgress,
            Vector3 up)
        {
            if (routeSampleCount < 2 || routeSampleCount > MaximumRouteSampleCount)
                return float.NaN;
            Vector3 planarPoint = Vector3.ProjectOnPlane(point, up);
            float bestProgress = minimumProgress;
            float bestDistance = float.PositiveInfinity;
            for (int i = 1; i < routeSampleCount; i++)
            {
                float segmentStartProgress = m_RouteFractions[i - 1];
                float segmentEndProgress = m_RouteFractions[i];
                if (segmentEndProgress + 0.000001f < minimumProgress)
                    continue;
                Vector3 start = Vector3.ProjectOnPlane(m_GroundProbeRoute[i - 1], up);
                Vector3 end = Vector3.ProjectOnPlane(m_GroundProbeRoute[i], up);
                Vector3 segment = end - start;
                float lengthSquared = segment.sqrMagnitude;
                float t = lengthSquared > 0.000001f
                    ? Mathf.Clamp01(Vector3.Dot(planarPoint - start, segment) / lengthSquared)
                    : 0f;
                float progress = Mathf.Lerp(segmentStartProgress, segmentEndProgress, t);
                if (progress < minimumProgress)
                {
                    progress = minimumProgress;
                    float range = segmentEndProgress - segmentStartProgress;
                    t = range > 0.000001f
                        ? Mathf.Clamp01((progress - segmentStartProgress) / range)
                        : 1f;
                }
                Vector3 projected = Vector3.Lerp(start, end, t);
                float distance = (planarPoint - projected).sqrMagnitude;
                if (distance + 0.000001f >= bestDistance)
                    continue;
                bestDistance = distance;
                bestProgress = progress;
            }
            return bestProgress;
        }

        float ResolveVirtualGroundSplitActionProgress(
            float opposingEventPhase,
            in AnimationPredictedFootStepSample step,
            in CharacterPredictiveFootRootTrajectory rootTrajectory,
            out Vector3 opposingLanding,
            out Vector3 splitRoutePoint,
            out float planarError)
        {
            opposingLanding = default;
            splitRoutePoint = default;
            planarError = 0f;
            if (!step.HasOpposingLandingEvent ||
                opposingEventPhase <= rootTrajectory.PathStartPhase + 0.0001f ||
                opposingEventPhase >= 0.9999f)
            {
                return 0f;
            }
            rootTrajectory.EvaluateEventPhase(
                opposingEventPhase,
                out Vector3 opposingRoot,
                out Quaternion opposingRootRotation);
            opposingLanding = opposingRoot +
                              opposingRootRotation * step.OpposingRootLocalLanding;
            if (!IsFinite(opposingLanding))
                return 0f;
            float splitProgress = Mathf.Clamp01(
                (opposingEventPhase - rootTrajectory.PathStartPhase) /
                Mathf.Max(0.000001f, 1f - rootTrajectory.PathStartPhase));
            splitRoutePoint = opposingLanding;
            planarError = 0f;
            return splitProgress > 0.0001f && splitProgress < 0.9999f
                ? splitProgress
                : 0f;
        }

        int BuildRouteEventPhases(
            int uniformCount,
            float pathStartPhase,
            float splitEventPhase,
            float splitFraction)
        {
            if (uniformCount < 2 || uniformCount > MaximumRouteSampleCount)
                return 0;
            for (int i = 0; i < uniformCount; i++)
            {
                float routeFraction = i / (uniformCount - 1f);
                m_RouteEventPhases[i] = splitFraction > 0f && splitFraction < 1f
                    ? routeFraction <= splitFraction
                        ? Mathf.Lerp(
                            pathStartPhase,
                            splitEventPhase,
                            routeFraction / splitFraction)
                        : Mathf.Lerp(
                            splitEventPhase,
                            1f,
                            (routeFraction - splitFraction) / (1f - splitFraction))
                    : Mathf.Lerp(pathStartPhase, 1f, routeFraction);
            }
            if (splitEventPhase <= pathStartPhase + 0.0001f || splitEventPhase >= 0.9999f)
                return uniformCount;
            for (int i = 1; i < uniformCount - 1; i++)
            {
                if (Mathf.Abs(m_RouteEventPhases[i] - splitEventPhase) <= 0.00001f)
                    return uniformCount;
            }
            int count = uniformCount;
            if (count < MaximumRouteSampleCount)
            {
                m_RouteEventPhases[count++] = splitEventPhase;
            }
            else
            {
                int nearest = 1;
                float nearestDistance = Mathf.Abs(m_RouteEventPhases[nearest] - splitEventPhase);
                for (int i = 2; i < count - 1; i++)
                {
                    float distance = Mathf.Abs(m_RouteEventPhases[i] - splitEventPhase);
                    if (distance >= nearestDistance)
                        continue;
                    nearest = i;
                    nearestDistance = distance;
                }
                m_RouteEventPhases[nearest] = splitEventPhase;
            }
            Array.Sort(m_RouteEventPhases, 0, count);
            return count;
        }

        static bool ContainsPhase(float[] phases, int count, float eventPhase)
        {
            for (int i = 0; i < count; i++)
            {
                if (Mathf.Abs(phases[i] - eventPhase) <= 0.00001f)
                    return true;
            }
            return false;
        }

    }
}
