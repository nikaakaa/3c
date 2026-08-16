using System;
using ThirdPersonCharacter.Pipeline.Animation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    internal readonly struct CharacterPredictiveFootPlacementQueryResult
    {
        internal CharacterPredictiveFootPlacementQueryResult(
            FootPlacementSurface futureLandingSupport,
            FootPlacementGroundEnvelope groundEnvelope,
            CharacterPredictiveBodySupportPath bodySupportPath,
            float soleSupportRadius,
            CharacterFootPlacementQueryRequest futureLandingRequest,
            float virtualGroundSplitEventPhase,
            Vector3 virtualGroundOpposingLanding,
            Vector3 virtualGroundSplitRoutePoint,
            float virtualGroundSplitPlanarError,
            float virtualGroundSplitFraction,
            FootPlacementSurface virtualGroundSplitSupport,
            ulong virtualGroundSplitLandingEventIdentity,
            int routeSampleCount,
            int queryCount,
            int rawHitCount,
            int acceptedHitCount,
            int edgePlaneCandidateCount,
            int acceptedEdgePlaneCount,
            int rejectedCount,
            CharacterPredictiveFootQueryRejectCounts rejectCounts,
            float[] routeEventPhases,
            float[] routeFractions,
            Vector3[] footRoutes,
            int footRateSampleCount,
            float[] footRateEventPhases,
            float[] footRateProgress,
            CharacterPredictiveFootQueryRequestSnapshot[] queryRequests,
            int queryRequestCount,
            CharacterPredictiveFootQueryGeometrySnapshot[] acceptedSupports,
            int acceptedSupportCount,
            CharacterPredictiveFootQueryGeometrySnapshot[] rejectedGeometry,
            int rejectedGeometryCount)
        {
            FutureLandingSupport = futureLandingSupport;
            GroundEnvelope = groundEnvelope;
            BodySupportPath = bodySupportPath;
            SoleSupportRadius = soleSupportRadius;
            FutureLandingRequest = futureLandingRequest;
            VirtualGroundSplitEventPhase = virtualGroundSplitEventPhase;
            VirtualGroundOpposingLanding = virtualGroundOpposingLanding;
            VirtualGroundSplitRoutePoint = virtualGroundSplitRoutePoint;
            VirtualGroundSplitPlanarError = virtualGroundSplitPlanarError;
            VirtualGroundSplitFraction = virtualGroundSplitFraction;
            VirtualGroundSplitSupport = virtualGroundSplitSupport;
            VirtualGroundSplitLandingEventIdentity = virtualGroundSplitLandingEventIdentity;
            RouteSampleCount = routeSampleCount;
            QueryCount = queryCount;
            RawHitCount = rawHitCount;
            AcceptedHitCount = acceptedHitCount;
            EdgePlaneCandidateCount = edgePlaneCandidateCount;
            AcceptedEdgePlaneCount = acceptedEdgePlaneCount;
            RejectedCount = rejectedCount;
            RejectCounts = rejectCounts;
            RouteEventPhases = routeEventPhases;
            RouteFractions = routeFractions;
            GroundProbeRoute = footRoutes;
            FootRateSampleCount = footRateSampleCount;
            FootRateEventPhases = footRateEventPhases;
            FootRateProgress = footRateProgress;
            QueryRequests = queryRequests;
            QueryRequestCount = queryRequestCount;
            AcceptedSupports = acceptedSupports;
            AcceptedSupportCount = acceptedSupportCount;
            RejectedGeometry = rejectedGeometry;
            RejectedGeometryCount = rejectedGeometryCount;
        }

        internal FootPlacementSurface FutureLandingSupport { get; }
        internal FootPlacementGroundEnvelope GroundEnvelope { get; }
        internal CharacterPredictiveBodySupportPath BodySupportPath { get; }
        internal float SoleSupportRadius { get; }
        internal CharacterFootPlacementQueryRequest FutureLandingRequest { get; }
        internal float VirtualGroundSplitEventPhase { get; }
        internal Vector3 VirtualGroundOpposingLanding { get; }
        internal Vector3 VirtualGroundSplitRoutePoint { get; }
        internal float VirtualGroundSplitPlanarError { get; }
        internal float VirtualGroundSplitFraction { get; }
        internal FootPlacementSurface VirtualGroundSplitSupport { get; }
        internal ulong VirtualGroundSplitLandingEventIdentity { get; }
        internal int RouteSampleCount { get; }
        internal int QueryCount { get; }
        internal int RawHitCount { get; }
        internal int AcceptedHitCount { get; }
        internal int EdgePlaneCandidateCount { get; }
        internal int AcceptedEdgePlaneCount { get; }
        internal int RejectedCount { get; }
        internal CharacterPredictiveFootQueryRejectCounts RejectCounts { get; }
        internal float[] RouteEventPhases { get; }
        internal float[] RouteFractions { get; }
        internal Vector3[] GroundProbeRoute { get; }
        internal int FootRateSampleCount { get; }
        internal float[] FootRateEventPhases { get; }
        internal float[] FootRateProgress { get; }
        internal CharacterPredictiveFootQueryRequestSnapshot[] QueryRequests { get; }
        internal int QueryRequestCount { get; }
        internal CharacterPredictiveFootQueryGeometrySnapshot[] AcceptedSupports { get; }
        internal int AcceptedSupportCount { get; }
        internal CharacterPredictiveFootQueryGeometrySnapshot[] RejectedGeometry { get; }
        internal int RejectedGeometryCount { get; }
        internal bool HasFutureLandingSupport =>
            FutureLandingSupport.IsValid && GroundEnvelope.IsValid && BodySupportPath.IsValid;
    }

    internal sealed partial class CharacterPredictiveFootPlacementQuery
    {
        internal const int MaximumRouteSampleCount = 64;
        internal const int MaximumQueryRequestCount = (MaximumRouteSampleCount - 1) * 3 + 1;
        internal const int MaximumPathPointCapacity =
            1 + (MaximumRouteSampleCount - 1) * (32 + 1);
        internal const int MaximumAcceptedGeometryCount = MaximumQueryRequestCount * 32;
        internal const int MaximumRejectedGeometryCount = MaximumPathPointCapacity * 4;

        readonly struct FootPathSample
        {
            internal FootPathSample(
                float fraction,
                FootPlacementSurface surface,
                Vector3 point,
                Vector3 root,
                Vector3 hip,
                float soleHeight,
                bool isSupport)
            {
                Fraction = Mathf.Clamp01(fraction);
                Surface = surface;
                Point = point;
                Root = root;
                Hip = hip;
                SoleHeight = soleHeight;
                IsSupport = isSupport;
            }

            internal float Fraction { get; }
            internal FootPlacementSurface Surface { get; }
            internal Vector3 Point { get; }
            internal Vector3 Root { get; }
            internal Vector3 Hip { get; }
            internal float SoleHeight { get; }
            internal bool IsSupport { get; }
        }

        readonly struct EdgePlaneCandidate
        {
            internal EdgePlaneCandidate(
                float fraction,
                FootPlacementSurface surface,
                Vector3 point,
                Vector3 root,
                Vector3 hip,
                float soleHeight,
                float animationClearanceHeight,
                Vector3 soleToAnkle,
                float authoredReach,
                int queryIndex)
            {
                Fraction = Mathf.Clamp01(fraction);
                Surface = surface;
                Point = point;
                Root = root;
                Hip = hip;
                SoleHeight = soleHeight;
                AnimationClearanceHeight = Mathf.Max(0f, animationClearanceHeight);
                SoleToAnkle = soleToAnkle;
                AuthoredReach = authoredReach;
                QueryIndex = queryIndex;
            }

            internal float Fraction { get; }
            internal FootPlacementSurface Surface { get; }
            internal Vector3 Point { get; }
            internal Vector3 Root { get; }
            internal Vector3 Hip { get; }
            internal float SoleHeight { get; }
            internal float AnimationClearanceHeight { get; }
            internal Vector3 SoleToAnkle { get; }
            internal float AuthoredReach { get; }
            internal int QueryIndex { get; }
        }

        readonly CharacterFootPlacementWorldQueryBackend m_World;
        readonly CharacterPredictiveFootPlacementRuntimeSettings m_Settings;
        readonly FootPlacementGroundEnvelopeSegment[] m_Segments =
            new FootPlacementGroundEnvelopeSegment[MaximumPathPointCapacity - 1];
        readonly FootPathSample[] m_PathSamples = new FootPathSample[MaximumPathPointCapacity];
        readonly Vector3[] m_GroundProbeRoute = new Vector3[MaximumRouteSampleCount];
        readonly Vector3[] m_RootRoutes = new Vector3[MaximumRouteSampleCount];
        readonly Vector3[] m_HipRoutes = new Vector3[MaximumRouteSampleCount];
        readonly float[] m_RouteEventPhases = new float[MaximumRouteSampleCount];
        readonly float[] m_RouteFractions = new float[MaximumRouteSampleCount];
        readonly float[] m_FootRateEventPhases = new float[MaximumRouteSampleCount];
        readonly float[] m_FootRateProgress = new float[MaximumRouteSampleCount];
        readonly int[] m_UpperHullIndices = new int[MaximumPathPointCapacity];
        readonly bool[] m_ForwardReachable = new bool[MaximumPathPointCapacity];
        readonly bool[] m_BackwardReachable = new bool[MaximumPathPointCapacity];
        readonly EdgePlaneCandidate[] m_EdgePlanes =
            new EdgePlaneCandidate[(MaximumRouteSampleCount - 1) * 32];
        readonly CharacterPredictiveFootQueryRequestSnapshot[] m_QueryRequests =
            new CharacterPredictiveFootQueryRequestSnapshot[MaximumQueryRequestCount];
        readonly CharacterPredictiveFootQueryGeometrySnapshot[] m_AcceptedSupports =
            new CharacterPredictiveFootQueryGeometrySnapshot[MaximumAcceptedGeometryCount];
        readonly CharacterPredictiveFootQueryGeometrySnapshot[] m_RejectedGeometry =
            new CharacterPredictiveFootQueryGeometrySnapshot[MaximumRejectedGeometryCount];
        int m_QueryRequestCount;
        int m_AcceptedSupportCount;
        int m_RejectedGeometryCount;

        internal CharacterPredictiveFootPlacementQuery(
            CharacterFootPlacementWorldQueryBackend world,
            CharacterPredictiveFootPlacementRuntimeSettings settings)
        {
            m_World = world ?? throw new ArgumentNullException(nameof(world));
            m_Settings = settings;
        }

        FootPlacementSurface QuerySupport(
            int footIndex,
            Vector3 route,
            Vector3 root,
            Vector3 hip,
            int layerMask,
            Vector3 up,
            Vector3 previousSupport,
            CharacterFootPlacementQueryPurpose purpose,
            float animationClearanceHeight,
            Vector3 soleToAnkle,
            float authoredReach,
            float maximumReach,
            bool validateReach,
            out Vector3 selectedRoot,
            out Vector3 selectedHip,
            out FootPlacementGroundEnvelopeRejectReason sampleReject,
            out CharacterFootPlacementQueryRequest request,
            ref int queryCount,
            ref int rawHitCount,
            ref int acceptedHitCount,
            ref int rejectedCount,
            ref CharacterPredictiveFootQueryRejectCounts rejectCounts)
        {
            ResolveVerticalSweep(
                route,
                route,
                hip,
                hip,
                previousSupport,
                up,
                m_Settings.PathSphereRadius,
                out Vector3 castStart,
                out _,
                out _,
                out float castDistance);
            request = new CharacterFootPlacementQueryRequest(
                CharacterFootPlacementQueryShape.Sphere,
                purpose,
                footIndex,
                castStart,
                Vector3.zero,
                -up,
                castDistance,
                m_Settings.PathSphereRadius,
                layerMask,
                Mathf.Cos(m_Settings.MaximumSlopeDegrees * Mathf.Deg2Rad));
            int queryIndex = RecordRequest(in request);
            queryCount++;
            int hitCount = m_World.QueryAll(in request);
            rawHitCount += hitCount;
            FootPlacementSurface selected = default;
            selectedRoot = default;
            selectedHip = default;
            sampleReject = FootPlacementGroundEnvelopeRejectReason.NoCandidate;
            if (hitCount == 0)
            {
                rejectedCount++;
                rejectCounts.Add(sampleReject);
            }
            for (int hitIndex = 0; hitIndex < hitCount; hitIndex++)
            {
                CharacterFootPlacementQueryHit hit = m_World.GetHit(hitIndex);
                if (!TryResolveSupportPoint(
                        hit,
                        route,
                        up,
                        out Vector3 supportPoint,
                        out FootPlacementGroundEnvelopeRejectReason hitReject))
                {
                    rejectedCount++;
                    rejectCounts.Add(hitReject);
                    RecordRejected(queryIndex, in hit, hitReject);
                    sampleReject = hitReject;
                    continue;
                }
                var candidate = new FootPlacementSurface(
                    hit.PhysicsHit.collider,
                    supportPoint,
                    hit.Normal.normalized);
                float supportHeightOffset =
                    Vector3.Dot(supportPoint, up) - Vector3.Dot(route, up);
                Vector3 supportRoot = root + up * supportHeightOffset;
                Vector3 supportHip = hip + up * supportHeightOffset;
                Vector3 reachableSole = supportPoint + up * animationClearanceHeight;
                if (validateReach && !IsReachableAnkle(
                        supportHip,
                        reachableSole,
                        soleToAnkle,
                        authoredReach,
                        maximumReach))
                {
                    rejectedCount++;
                    rejectCounts.Add(FootPlacementGroundEnvelopeRejectReason.ReachExceeded);
                    RecordRejected(
                        queryIndex,
                        in hit,
                        FootPlacementGroundEnvelopeRejectReason.ReachExceeded);
                    sampleReject = FootPlacementGroundEnvelopeRejectReason.ReachExceeded;
                    continue;
                }
                if (!selected.IsValid)
                {
                    selected = candidate;
                    selectedRoot = supportRoot;
                    selectedHip = supportHip;
                }
                acceptedHitCount++;
                RecordAccepted(queryIndex, candidate.Point, candidate.Normal, candidate.Identity);
            }
            return selected;
        }

        int ResolveRouteSampleCount(float routeLength)
        {
            float spacing = Mathf.Max(0.02f, m_Settings.PathSphereRadius * 0.75f);
            return Mathf.Clamp(
                Mathf.CeilToInt(routeLength / spacing) + 1,
                2,
                MaximumRouteSampleCount);
        }

        void AddSegmentHits(
            int footIndex,
            Vector3 start,
            Vector3 end,
            Vector3 previousSupport,
            float startFraction,
            float endFraction,
            Vector3 rootStart,
            Vector3 rootEnd,
            Vector3 hipStart,
            Vector3 hipEnd,
            in CharacterPredictiveFootRootTrajectory rootTrajectory,
            in AnimationPredictedFootStepSample step,
            int layerMask,
            Vector3 up,
            float soleSupportRadius,
            float maximumReach,
            ref int pathSampleCount,
            ref int queryCount,
            ref int rawHitCount,
            ref int acceptedHitCount,
            ref int edgePlaneCandidateCount,
            ref int rejectedCount,
            ref CharacterPredictiveFootQueryRejectCounts rejectCounts)
        {
            Vector3 path = end - start;
            if (path.sqrMagnitude <= 0.00000001f)
                return;
            ResolveVerticalSweep(
                start,
                end,
                hipStart,
                hipEnd,
                previousSupport,
                up,
                m_Settings.SwingCapsuleRadius,
                out Vector3 castStart,
                out Vector3 castEnd,
                out float castSurfaceHeight,
                out float castDistance);
            var request = new CharacterFootPlacementQueryRequest(
                CharacterFootPlacementQueryShape.Capsule,
                CharacterFootPlacementQueryPurpose.SwingClearance,
                footIndex,
                castStart,
                castEnd,
                -up,
                castDistance,
                m_Settings.SwingCapsuleRadius,
                layerMask,
                -1f);
            int queryIndex = RecordRequest(in request);
            queryCount++;
            int hitCount = m_World.QueryAll(in request);
            rawHitCount += hitCount;
            float minimumGroundNormalDot = Mathf.Cos(m_Settings.MaximumSlopeDegrees * Mathf.Deg2Rad);
            for (int i = 0; i < hitCount; i++)
            {
                CharacterFootPlacementQueryHit hit = m_World.GetHit(i);
                Vector3 normal = hit.Normal.normalized;
                float routeT = ResolveRouteFraction(start, end, hit.Point, normal, up);
                float fraction = Mathf.Lerp(
                    startFraction,
                    endFraction,
                    routeT);
                Vector3 route = Vector3.Lerp(start, end, routeT);
                float routeHeight = Vector3.Dot(route, up);
                float soleHeight = castSurfaceHeight - hit.Distance;
                Vector3 root = Vector3.Lerp(rootStart, rootEnd, routeT);
                Vector3 hip = Vector3.Lerp(hipStart, hipEnd, routeT);
                Vector3 point = route + up * (soleHeight - routeHeight);
                float supportHeightOffset = soleHeight - routeHeight;
                Vector3 supportRoot = root + up * supportHeightOffset;
                Vector3 supportHip = hip + up * supportHeightOffset;
                var surface = new FootPlacementSurface(hit.PhysicsHit.collider, point, normal);
                float groundDot = Vector3.Dot(normal, up);
                if (groundDot >= minimumGroundNormalDot)
                {
                    AddPathSample(
                        new FootPathSample(
                            fraction,
                            surface,
                            point,
                            supportRoot,
                            supportHip,
                            soleHeight,
                            false),
                        ref pathSampleCount);
                    acceptedHitCount++;
                    RecordAccepted(queryIndex, point, normal, surface.Identity);
                    continue;
                }
                if (Mathf.Abs(groundDot) < minimumGroundNormalDot &&
                    edgePlaneCandidateCount < m_EdgePlanes.Length)
                {
                    Vector3 edgeNormal = Vector3.ProjectOnPlane(normal, up);
                    float edgeSupportRadius = Mathf.Max(
                        m_Settings.SwingCapsuleRadius,
                        soleSupportRadius);
                    Vector3 expandedEdgePoint = edgeNormal.sqrMagnitude > 0.000001f
                        ? hit.Point + edgeNormal.normalized * edgeSupportRadius
                        : hit.Point;
                    float edgeRouteT = ResolveRouteFraction(
                        start,
                        end,
                        expandedEdgePoint,
                        normal,
                        up);
                    float edgeFraction = Mathf.Lerp(
                        startFraction,
                        endFraction,
                        edgeRouteT);
                    Vector3 edgeRoute = Vector3.Lerp(start, end, edgeRouteT);
                    float edgeRouteHeight = Vector3.Dot(edgeRoute, up);
                    Vector3 edgeRoot = Vector3.Lerp(rootStart, rootEnd, edgeRouteT);
                    Vector3 edgeHip = Vector3.Lerp(hipStart, hipEnd, edgeRouteT);
                    Vector3 edgePoint = edgeRoute + up * (soleHeight - edgeRouteHeight);
                    m_EdgePlanes[edgePlaneCandidateCount++] = new EdgePlaneCandidate(
                        edgeFraction,
                        new FootPlacementSurface(hit.PhysicsHit.collider, edgePoint, normal),
                        edgePoint,
                        edgeRoot,
                        edgeHip,
                        soleHeight,
                        0f,
                        Vector3.zero,
                        0f,
                        queryIndex);
                    continue;
                }
                rejectedCount++;
                rejectCounts.Add(FootPlacementGroundEnvelopeRejectReason.SlopeExceeded);
                RecordRejected(
                    queryIndex,
                    in hit,
                    FootPlacementGroundEnvelopeRejectReason.SlopeExceeded);
            }
        }

        void ResolveVerticalSweep(
            Vector3 start,
            Vector3 end,
            Vector3 discoveryStart,
            Vector3 discoveryEnd,
            Vector3 previousSupport,
            Vector3 up,
            float radius,
            out Vector3 castStart,
            out Vector3 castEnd,
            out float castSurfaceHeight,
            out float castDistance)
        {
            float startHeight = Vector3.Dot(start, up);
            float endHeight = Vector3.Dot(end, up);
            float discoveryStartHeight = Vector3.Dot(discoveryStart, up);
            float discoveryEndHeight = Vector3.Dot(discoveryEnd, up);
            float previousSupportHeight = Vector3.Dot(previousSupport, up);
            castSurfaceHeight = Mathf.Max(
                Mathf.Max(startHeight, endHeight) + m_Settings.CastAbove,
                Mathf.Max(
                    Mathf.Max(discoveryStartHeight, discoveryEndHeight),
                    previousSupportHeight + m_Settings.MaximumStepUp));
            float minimumSurfaceHeight = Mathf.Min(
                Mathf.Min(startHeight, endHeight) - m_Settings.CastBelow,
                previousSupportHeight - m_Settings.MaximumStepDown);
            float centerHeight = castSurfaceHeight + radius;
            castStart = start + up * (centerHeight - startHeight);
            castEnd = end + up * (centerHeight - endHeight);
            castDistance = Mathf.Max(0.0001f, castSurfaceHeight - minimumSurfaceHeight);
        }

        void AddPathSample(FootPathSample sample, ref int count)
        {
            if (count >= m_PathSamples.Length)
                return;
            m_PathSamples[count++] = sample;
        }

        int RecordRequest(in CharacterFootPlacementQueryRequest request)
        {
            if (m_QueryRequestCount >= m_QueryRequests.Length)
                throw new InvalidOperationException("Predictive Foot query request snapshot capacity was exceeded.");
            int index = m_QueryRequestCount++;
            m_QueryRequests[index] = new CharacterPredictiveFootQueryRequestSnapshot(in request);
            return index;
        }

        void RecordAccepted(int queryIndex, Vector3 point, Vector3 normal, int surfaceIdentity)
        {
            if (m_AcceptedSupportCount >= m_AcceptedSupports.Length)
                throw new InvalidOperationException("Predictive Foot accepted support snapshot capacity was exceeded.");
            m_AcceptedSupports[m_AcceptedSupportCount++] =
                new CharacterPredictiveFootQueryGeometrySnapshot(
                    queryIndex,
                    point,
                    normal,
                    surfaceIdentity,
                    FootPlacementGroundEnvelopeRejectReason.None);
        }

        void RecordRejected(
            int queryIndex,
            in CharacterFootPlacementQueryHit hit,
            FootPlacementGroundEnvelopeRejectReason reason)
        {
            int surfaceIdentity = hit.PhysicsHit.collider ? hit.PhysicsHit.collider.GetInstanceID() : 0;
            RecordRejected(queryIndex, hit.Point, hit.Normal, surfaceIdentity, reason);
        }

        void RecordRejected(
            int queryIndex,
            Vector3 point,
            Vector3 normal,
            int surfaceIdentity,
            FootPlacementGroundEnvelopeRejectReason reason)
        {
            if (m_RejectedGeometryCount >= m_RejectedGeometry.Length)
                throw new InvalidOperationException("Predictive Foot rejected geometry snapshot capacity was exceeded.");
            m_RejectedGeometry[m_RejectedGeometryCount++] =
                new CharacterPredictiveFootQueryGeometrySnapshot(
                    queryIndex,
                    point,
                    normal,
                    surfaceIdentity,
                    reason);
        }

        void ResolveEdgePlanes(
            int edgePlaneCount,
            Vector3 up,
            float maximumReach,
            ref int pathSampleCount,
            ref int acceptedHitCount,
            ref int acceptedEdgePlaneCount,
            ref int rejectedCount,
            ref CharacterPredictiveFootQueryRejectCounts rejectCounts)
        {
            for (int i = 1; i < edgePlaneCount; i++)
            {
                EdgePlaneCandidate value = m_EdgePlanes[i];
                int insertion = i;
                while (insertion > 0 && Compare(value, m_EdgePlanes[insertion - 1]) < 0)
                {
                    m_EdgePlanes[insertion] = m_EdgePlanes[insertion - 1];
                    insertion--;
                }
                m_EdgePlanes[insertion] = value;
            }

            for (int edgeIndex = 0; edgeIndex < edgePlaneCount; edgeIndex++)
            {
                EdgePlaneCandidate edge = m_EdgePlanes[edgeIndex];
                int beforeIndex = -1;
                int afterIndex = -1;
                for (int sampleIndex = 0; sampleIndex < pathSampleCount; sampleIndex++)
                {
                    FootPathSample sample = m_PathSamples[sampleIndex];
                    if (!sample.IsSupport)
                        continue;
                    if (sample.Fraction < edge.Fraction - 0.00001f &&
                        (beforeIndex < 0 || sample.Fraction > m_PathSamples[beforeIndex].Fraction))
                    {
                        beforeIndex = sampleIndex;
                    }
                    if (sample.Fraction > edge.Fraction + 0.00001f &&
                        (afterIndex < 0 || sample.Fraction < m_PathSamples[afterIndex].Fraction))
                    {
                        afterIndex = sampleIndex;
                    }
                }

                if (beforeIndex < 0 || afterIndex < 0)
                {
                    rejectedCount++;
                    rejectCounts.Add(FootPlacementGroundEnvelopeRejectReason.EdgeGap);
                    RecordRejected(
                        edge.QueryIndex,
                        edge.Point,
                        edge.Surface.Normal,
                        edge.Surface.Identity,
                        FootPlacementGroundEnvelopeRejectReason.EdgeGap);
                    continue;
                }

                FootPathSample before = m_PathSamples[beforeIndex];
                FootPathSample after = m_PathSamples[afterIndex];
                float beforeGap = Vector3.ProjectOnPlane(edge.Point - before.Point, up).magnitude;
                float afterGap = Vector3.ProjectOnPlane(after.Point - edge.Point, up).magnitude;
                if (beforeGap > m_Settings.MaximumEdgeGap || afterGap > m_Settings.MaximumEdgeGap)
                {
                    rejectedCount++;
                    rejectCounts.Add(FootPlacementGroundEnvelopeRejectReason.EdgeGap);
                    RecordRejected(
                        edge.QueryIndex,
                        edge.Point,
                        edge.Surface.Normal,
                        edge.Surface.Identity,
                        FootPlacementGroundEnvelopeRejectReason.EdgeGap);
                    continue;
                }
                if (Mathf.Abs(after.SoleHeight - before.SoleHeight) >
                    m_Settings.MaximumHeightDiscontinuity)
                {
                    rejectedCount++;
                    rejectCounts.Add(FootPlacementGroundEnvelopeRejectReason.HeightDiscontinuity);
                    RecordRejected(
                        edge.QueryIndex,
                        edge.Point,
                        edge.Surface.Normal,
                        edge.Surface.Identity,
                        FootPlacementGroundEnvelopeRejectReason.HeightDiscontinuity);
                    continue;
                }

                float soleHeight = Mathf.Max(
                    edge.SoleHeight,
                    Mathf.Max(before.SoleHeight, after.SoleHeight));
                Vector3 point = edge.Point + up * (soleHeight - edge.SoleHeight);
                FootPathSample soleSupport = after.SoleHeight >= before.SoleHeight
                    ? after
                    : before;
                AddPathSample(
                    new FootPathSample(
                        edge.Fraction,
                        new FootPlacementSurface(
                            soleSupport.Surface.Collider,
                            point,
                            soleSupport.Surface.Normal),
                        point,
                        edge.Root,
                        edge.Hip,
                        soleHeight,
                        false),
                    ref pathSampleCount);
                acceptedHitCount++;
                acceptedEdgePlaneCount++;
                RecordAccepted(
                    edge.QueryIndex,
                    point,
                    edge.Surface.Normal,
                    edge.Surface.Identity);
            }
        }

        static float ResolveRouteFraction(
            Vector3 start,
            Vector3 end,
            Vector3 hitPoint,
            Vector3 hitNormal,
            Vector3 up)
        {
            Vector3 path = end - start;
            float denominator = Vector3.Dot(path, hitNormal);
            if (Mathf.Abs(denominator) > 0.000001f)
                return Mathf.Clamp01(Vector3.Dot(hitPoint - start, hitNormal) / denominator);
            Vector3 planarPath = Vector3.ProjectOnPlane(path, up);
            float lengthSquared = planarPath.sqrMagnitude;
            if (lengthSquared <= 0.00000001f)
                return 0.5f;
            return Mathf.Clamp01(
                Vector3.Dot(Vector3.ProjectOnPlane(hitPoint - start, up), planarPath) /
                lengthSquared);
        }

        void SortAndCollapsePathSamples(ref int count)
        {
            const float fractionTolerance = 0.00001f;
            for (int i = 1; i < count; i++)
            {
                FootPathSample value = m_PathSamples[i];
                int insertion = i;
                while (insertion > 0 && Compare(value, m_PathSamples[insertion - 1]) < 0)
                {
                    m_PathSamples[insertion] = m_PathSamples[insertion - 1];
                    insertion--;
                }
                m_PathSamples[insertion] = value;
            }
            int write = 0;
            for (int read = 0; read < count; read++)
            {
                FootPathSample candidate = m_PathSamples[read];
                if (write > 0 && Mathf.Abs(candidate.Fraction - m_PathSamples[write - 1].Fraction) <= fractionTolerance)
                {
                    float fraction = candidate.Fraction;
                    if (m_PathSamples[write - 1].Fraction <= fractionTolerance)
                        fraction = 0f;
                    else if (candidate.Fraction >= 1f - fractionTolerance)
                        fraction = 1f;
                    m_PathSamples[write - 1] = new FootPathSample(
                        fraction,
                        candidate.Surface,
                        candidate.Point,
                        candidate.Root,
                        candidate.Hip,
                        candidate.SoleHeight,
                        candidate.IsSupport);
                    continue;
                }
                m_PathSamples[write++] = candidate;
            }
            count = write;
        }

        static int Compare(FootPathSample left, FootPathSample right)
        {
            int fraction = left.Fraction.CompareTo(right.Fraction);
            if (fraction != 0)
                return fraction;
            int height = left.SoleHeight.CompareTo(right.SoleHeight);
            if (height != 0)
                return height;
            return left.Surface.Identity.CompareTo(right.Surface.Identity);
        }

        static int Compare(EdgePlaneCandidate left, EdgePlaneCandidate right)
        {
            int fraction = left.Fraction.CompareTo(right.Fraction);
            if (fraction != 0)
                return fraction;
            int height = left.SoleHeight.CompareTo(right.SoleHeight);
            if (height != 0)
                return height;
            return left.Surface.Identity.CompareTo(right.Surface.Identity);
        }

        int BuildUpperEnvelope(int pathSampleCount) =>
            AppendUpperEnvelope(0, pathSampleCount - 1, 0);

        int AppendUpperEnvelope(int firstIndex, int lastIndex, int segmentCount)
        {
            int hullCount = 0;
            for (int i = firstIndex; i <= lastIndex; i++)
            {
                while (hullCount >= 2 &&
                       Cross(
                           m_PathSamples[m_UpperHullIndices[hullCount - 2]],
                           m_PathSamples[m_UpperHullIndices[hullCount - 1]],
                           m_PathSamples[i]) >= 0f)
                {
                    hullCount--;
                }
                m_UpperHullIndices[hullCount++] = i;
            }
            for (int i = 1; i < hullCount; i++)
            {
                FootPathSample start = m_PathSamples[m_UpperHullIndices[i - 1]];
                FootPathSample end = m_PathSamples[m_UpperHullIndices[i]];
                m_Segments[segmentCount++] = new FootPlacementGroundEnvelopeSegment(
                    start.Fraction,
                    end.Fraction,
                    end.Surface,
                    start.Point,
                    end.Point,
                    start.SoleHeight,
                    end.SoleHeight,
                    true);
            }
            return segmentCount;
        }

        bool AcceptSupportTransition(
            Vector3 previous,
            Vector3 current,
            Vector3 up,
            out FootPlacementGroundEnvelopeRejectReason rejectReason)
        {
            float height = Vector3.Dot(current - previous, up);
            if (height > m_Settings.MaximumStepUp || height < -m_Settings.MaximumStepDown)
            {
                rejectReason = FootPlacementGroundEnvelopeRejectReason.StepExceeded;
                return false;
            }
            float planarGap = Vector3.ProjectOnPlane(current - previous, up).magnitude;
            if (planarGap > m_Settings.MaximumEdgeGap)
            {
                rejectReason = FootPlacementGroundEnvelopeRejectReason.EdgeGap;
                return false;
            }
            rejectReason = FootPlacementGroundEnvelopeRejectReason.None;
            return true;
        }

        bool RetainReachableSupportChain(
            ref int pathSampleCount,
            Vector3 up,
            ref int rejectedCount,
            ref CharacterPredictiveFootQueryRejectCounts rejectCounts,
            out FootPlacementGroundEnvelopeRejectReason rejectReason)
        {
            int firstSupportIndex = -1;
            int lastSupportIndex = -1;
            for (int i = 0; i < pathSampleCount; i++)
            {
                if (!m_PathSamples[i].IsSupport)
                    continue;
                if (firstSupportIndex < 0)
                    firstSupportIndex = i;
                lastSupportIndex = i;
            }
            if (firstSupportIndex < 0 || lastSupportIndex <= firstSupportIndex)
            {
                rejectReason = FootPlacementGroundEnvelopeRejectReason.NoCandidate;
                return false;
            }
            Array.Clear(m_ForwardReachable, 0, pathSampleCount);
            Array.Clear(m_BackwardReachable, 0, pathSampleCount);
            m_ForwardReachable[firstSupportIndex] = true;
            FootPlacementGroundEnvelopeRejectReason finalReject =
                FootPlacementGroundEnvelopeRejectReason.NoCandidate;
            for (int current = firstSupportIndex + 1; current <= lastSupportIndex; current++)
            {
                FootPlacementGroundEnvelopeRejectReason currentReject =
                    FootPlacementGroundEnvelopeRejectReason.NoCandidate;
                for (int previous = current - 1; previous >= firstSupportIndex; previous--)
                {
                    if (!m_ForwardReachable[previous])
                        continue;
                    if (AcceptSupportTransition(
                            m_PathSamples[previous].Point,
                            m_PathSamples[current].Point,
                            up,
                            out FootPlacementGroundEnvelopeRejectReason transitionReject))
                    {
                        m_ForwardReachable[current] = true;
                        break;
                    }
                    currentReject = PreferReachabilityReject(currentReject, transitionReject);
                }
                if (current == lastSupportIndex)
                    finalReject = currentReject;
            }
            if (!m_ForwardReachable[lastSupportIndex])
            {
                rejectReason = finalReject == FootPlacementGroundEnvelopeRejectReason.NoCandidate
                    ? FootPlacementGroundEnvelopeRejectReason.EdgeGap
                    : finalReject;
                return false;
            }
            m_BackwardReachable[lastSupportIndex] = true;
            for (int current = lastSupportIndex - 1; current >= firstSupportIndex; current--)
            {
                for (int next = current + 1; next <= lastSupportIndex; next++)
                {
                    if (!m_BackwardReachable[next] ||
                        !AcceptSupportTransition(
                            m_PathSamples[current].Point,
                            m_PathSamples[next].Point,
                            up,
                            out _))
                    {
                        continue;
                    }
                    m_BackwardReachable[current] = true;
                    break;
                }
            }
            int write = 0;
            for (int i = firstSupportIndex; i <= lastSupportIndex; i++)
            {
                if (m_ForwardReachable[i] && m_BackwardReachable[i])
                {
                    m_PathSamples[write++] = m_PathSamples[i];
                    continue;
                }
                rejectedCount++;
                rejectCounts.Add(ResolveUnreachableSampleReject(
                    i,
                    firstSupportIndex,
                    lastSupportIndex,
                    up));
            }
            pathSampleCount = write;
            rejectReason = FootPlacementGroundEnvelopeRejectReason.None;
            return pathSampleCount >= 2;
        }

        FootPlacementGroundEnvelopeRejectReason ResolveUnreachableSampleReject(
            int sampleIndex,
            int firstSupportIndex,
            int lastSupportIndex,
            Vector3 up)
        {
            FootPlacementGroundEnvelopeRejectReason result =
                FootPlacementGroundEnvelopeRejectReason.NoCandidate;
            for (int previous = sampleIndex - 1; previous >= firstSupportIndex; previous--)
            {
                if (!m_ForwardReachable[previous])
                    continue;
                if (!AcceptSupportTransition(
                        m_PathSamples[previous].Point,
                        m_PathSamples[sampleIndex].Point,
                        up,
                        out FootPlacementGroundEnvelopeRejectReason transitionReject))
                {
                    result = PreferReachabilityReject(result, transitionReject);
                }
            }
            for (int next = sampleIndex + 1; next <= lastSupportIndex; next++)
            {
                if (!m_BackwardReachable[next])
                    continue;
                if (!AcceptSupportTransition(
                        m_PathSamples[sampleIndex].Point,
                        m_PathSamples[next].Point,
                        up,
                        out FootPlacementGroundEnvelopeRejectReason transitionReject))
                {
                    result = PreferReachabilityReject(result, transitionReject);
                }
            }
            return result == FootPlacementGroundEnvelopeRejectReason.NoCandidate
                ? FootPlacementGroundEnvelopeRejectReason.EdgeGap
                : result;
        }

        static FootPlacementGroundEnvelopeRejectReason PreferReachabilityReject(
            FootPlacementGroundEnvelopeRejectReason current,
            FootPlacementGroundEnvelopeRejectReason candidate)
        {
            if (candidate == FootPlacementGroundEnvelopeRejectReason.StepExceeded ||
                current == FootPlacementGroundEnvelopeRejectReason.NoCandidate)
            {
                return candidate;
            }
            return current;
        }

        static float Cross(FootPathSample first, FootPathSample second, FootPathSample third) =>
            (second.Fraction - first.Fraction) * (third.SoleHeight - first.SoleHeight) -
            (second.SoleHeight - first.SoleHeight) * (third.Fraction - first.Fraction);

        bool TryResolveSupportPoint(
            CharacterFootPlacementQueryHit hit,
            Vector3 route,
            Vector3 up,
            out Vector3 supportPoint,
            out FootPlacementGroundEnvelopeRejectReason rejectReason)
        {
            supportPoint = default;
            if (!hit.HasHit || !hit.PhysicsHit.collider)
            {
                rejectReason = FootPlacementGroundEnvelopeRejectReason.NoCandidate;
                return false;
            }
            if (Vector3.Angle(up, hit.Normal) > m_Settings.MaximumSlopeDegrees)
            {
                rejectReason = FootPlacementGroundEnvelopeRejectReason.SlopeExceeded;
                return false;
            }
            Vector3 normal = hit.Normal.normalized;
            float denominator = Vector3.Dot(up, normal);
            if (!float.IsFinite(denominator) || denominator <= 0.0001f)
            {
                rejectReason = FootPlacementGroundEnvelopeRejectReason.InvalidCandidate;
                return false;
            }
            float translation = Vector3.Dot(hit.Point - route, normal) / denominator;
            supportPoint = route + up * translation;
            if (!IsFinite(supportPoint))
            {
                rejectReason = FootPlacementGroundEnvelopeRejectReason.InvalidCandidate;
                return false;
            }
            Vector3 tangentialOffset = Vector3.ProjectOnPlane(supportPoint - hit.Point, normal);
            float maximumOffsetSquared =
                m_Settings.PathSphereRadius * m_Settings.PathSphereRadius;
            if (!IsFinite(tangentialOffset) ||
                tangentialOffset.sqrMagnitude > maximumOffsetSquared)
            {
                rejectReason = FootPlacementGroundEnvelopeRejectReason.UnsupportedCenter;
                return false;
            }
            rejectReason = FootPlacementGroundEnvelopeRejectReason.None;
            return true;
        }

        static bool IsFinite(Vector3 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);

        static Vector3 EvaluateFutureSoleToAnkle(
            in AnimationPredictedFootStepSample step,
            in CharacterPredictiveFootRootTrajectory rootTrajectory,
            float eventPhase)
        {
            rootTrajectory.EvaluateEventPhase(eventPhase, out _, out Quaternion rootRotation);
            return rootRotation * (
                step.EvaluateRootLocalAnkleRoute(eventPhase) -
                step.EvaluateRootLocalFootRoute(eventPhase));
        }

        static float EvaluateAnimationClearanceHeight(
            in AnimationPredictedFootStepSample step,
            float eventPhase) =>
            Mathf.Max(0f, step.EvaluateAnimationClearanceHeight(eventPhase));

        static float EvaluateAuthoredReach(
            in AnimationPredictedFootStepSample step,
            float eventPhase) =>
            Vector3.Distance(
                step.EvaluateRootLocalHipRoute(eventPhase),
                step.EvaluateRootLocalAnkleRoute(eventPhase));

        static bool IsReachableAnkle(
            Vector3 hip,
            Vector3 sole,
            Vector3 soleToAnkle,
            float authoredReach,
            float maximumReach)
        {
            Vector3 ankle = sole + soleToAnkle;
            return IsFinite(hip) && IsFinite(ankle) && IsFinite(soleToAnkle) &&
                   float.IsFinite(authoredReach) && authoredReach >= 0f &&
                   Vector3.Distance(hip, ankle) <= Mathf.Max(maximumReach, authoredReach) + 0.0001f;
        }
    }
}
