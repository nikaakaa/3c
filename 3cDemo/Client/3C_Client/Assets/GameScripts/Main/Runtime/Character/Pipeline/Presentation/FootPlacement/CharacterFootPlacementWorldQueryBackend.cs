using System;
using KK.GeneratedDiagnosticSampling;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    public enum CharacterFootPlacementQueryShape : byte
    {
        Sphere = 1
    }

    public enum CharacterFootPlacementQueryPurpose : byte
    {
        FutureLanding = 1,
        CurrentContactVerification = 2,
        CurrentSupport = 3
    }

    public readonly struct CharacterFootPlacementQueryRequest
    {
        public CharacterFootPlacementQueryRequest(
            CharacterFootPlacementQueryShape shape,
            CharacterFootPlacementQueryPurpose purpose,
            int footIndex,
            Vector3 origin,
            Vector3 direction,
            float maximumDistance,
            float radius,
            int layerMask,
            float minimumGroundNormalDot)
        {
            Shape = shape;
            Purpose = purpose;
            FootIndex = footIndex;
            Origin = origin;
            Direction = direction;
            MaximumDistance = maximumDistance;
            Radius = radius;
            LayerMask = layerMask;
            MinimumGroundNormalDot = minimumGroundNormalDot;
        }

        [DiagnosticField]
        [DiagnosticGroup("landing-observation")]
        public CharacterFootPlacementQueryShape Shape { get; }
        [DiagnosticField]
        [DiagnosticGroup("landing-observation")]
        public CharacterFootPlacementQueryPurpose Purpose { get; }
        [DiagnosticField]
        [DiagnosticGroup("landing-observation")]
        public int FootIndex { get; }
        [DiagnosticField]
        [DiagnosticGroup("landing-observation")]
        public Vector3 Origin { get; }
        [DiagnosticField]
        [DiagnosticGroup("landing-observation")]
        public Vector3 Direction { get; }
        [DiagnosticField]
        [DiagnosticGroup("landing-observation")]
        public float MaximumDistance { get; }
        [DiagnosticField]
        [DiagnosticGroup("landing-observation")]
        public float SupportMaximumDistance => MaximumDistance + Radius;
        [DiagnosticField]
        [DiagnosticGroup("landing-observation")]
        public float Radius { get; }
        [DiagnosticField]
        [DiagnosticGroup("landing-observation")]
        public int LayerMask { get; }
        [DiagnosticField]
        [DiagnosticGroup("landing-observation")]
        public float MinimumGroundNormalDot { get; }
    }

    public readonly struct CharacterFootSupportQueryDiagnostics
    {
        internal CharacterFootSupportQueryDiagnostics(
            int searchHitCount,
            int supportHitCount,
            int outsideSupportRayCount,
            int steepSurfaceCount)
        {
            SearchHitCount = searchHitCount;
            SupportHitCount = supportHitCount;
            OutsideSupportRayCount = outsideSupportRayCount;
            SteepSurfaceCount = steepSurfaceCount;
        }

        [DiagnosticField]
        [DiagnosticGroup("support-query")]
        public int SearchHitCount { get; }
        [DiagnosticField]
        [DiagnosticGroup("support-query")]
        public int SupportHitCount { get; }
        [DiagnosticField]
        [DiagnosticGroup("support-query")]
        public int OutsideSupportRayCount { get; }
        [DiagnosticField]
        [DiagnosticGroup("support-query")]
        public int SteepSurfaceCount { get; }
    }

    internal sealed class CharacterFootPlacementWorldQueryBackend :
        ICharacterFootPlacementWorldQuery
    {
        readonly PhysicsScene m_PhysicsScene;
        readonly CharacterFootPlacementPoseRig m_Rig;
        readonly RaycastHit[] m_LandingHits;
        readonly RaycastHit[] m_GroundPathHits;
        readonly RaycastHit[] m_CurrentSupportHits;
        CharacterFootGroundGeometrySource m_GroundGeometry;

        internal CharacterFootPlacementWorldQueryBackend(
            PhysicsScene physicsScene,
            CharacterFootPlacementPoseRig rig,
            int landingHitCapacity,
            int currentSupportHitCapacity,
            int groundPathSegmentHitCapacity)
        {
            if (!physicsScene.IsValid())
                throw new ArgumentException("Foot Placement requires a valid PhysicsScene.", nameof(physicsScene));
            if (landingHitCapacity < 4 || landingHitCapacity > 64)
                throw new ArgumentOutOfRangeException(nameof(landingHitCapacity));
            if (currentSupportHitCapacity < 4 || currentSupportHitCapacity > 32)
                throw new ArgumentOutOfRangeException(nameof(currentSupportHitCapacity));
            if (groundPathSegmentHitCapacity < 4 || groundPathSegmentHitCapacity > 32)
                throw new ArgumentOutOfRangeException(nameof(groundPathSegmentHitCapacity));
            m_PhysicsScene = physicsScene;
            m_Rig = rig ?? throw new ArgumentNullException(nameof(rig));
            m_LandingHits = new RaycastHit[landingHitCapacity];
            m_GroundPathHits = new RaycastHit[groundPathSegmentHitCapacity];
            m_CurrentSupportHits = new RaycastHit[currentSupportHitCapacity];
        }

        internal PhysicsScene PhysicsScene => m_PhysicsScene;
        public ulong WorldRevision => 1UL;

        public CharacterFootLandingQueryResult Query(
            in CharacterFootPlacementQueryRequest request)
        {
            bool requestValid = IsGroundRequestValid(in request);
            int count = QueryAll(in request, out bool capacityExceeded,
                out CharacterFootSupportQueryDiagnostics coverage);
            if (capacityExceeded)
            {
                return new CharacterFootLandingQueryResult(
                    CharacterFootLandingQueryRejectReason.CapacityExceeded,
                    default,
                    new CharacterFootLandingQuerySelectionDiagnostics(
                        CharacterFootLandingQueryCandidateSelectionState
                            .CapacityExceeded,
                        0,
                        default,
                        coverage));
            }
            if (count <= 0)
            {
                return new CharacterFootLandingQueryResult(
                    requestValid
                        ? CharacterFootLandingQueryRejectReason.NoHit
                        : CharacterFootLandingQueryRejectReason.InvalidRequest,
                    default,
                    new CharacterFootLandingQuerySelectionDiagnostics(
                        requestValid
                            ? CharacterFootLandingQueryCandidateSelectionState
                                .NoCandidate
                            : CharacterFootLandingQueryCandidateSelectionState
                                .InvalidRequest,
                        0,
                        default,
                        coverage));
            }
            RaycastHit hit = m_LandingHits[0];
            CharacterFootLandingQueryCandidateDiagnostics selected =
                CandidateDiagnostics(in hit);
            return new CharacterFootLandingQueryResult(
                CharacterFootLandingQueryRejectReason.None,
                new CharacterFootLandingSupport(
                    hit.collider.GetInstanceID(),
                    hit.point,
                    hit.normal,
                    hit.distance),
                new CharacterFootLandingQuerySelectionDiagnostics(
                    CharacterFootLandingQueryCandidateSelectionState.Selected,
                    count,
                    selected,
                    coverage));
        }

        internal int QueryAll(
            in CharacterFootPlacementQueryRequest request,
            out bool capacityExceeded,
            out CharacterFootSupportQueryDiagnostics coverage)
        {
            capacityExceeded = false;
            coverage = default;
            if (!IsGroundRequestValid(in request))
                return 0;
            int count = m_PhysicsScene.SphereCast(
                request.Origin,
                request.Radius,
                request.Direction.normalized,
                m_LandingHits,
                request.MaximumDistance,
                request.LayerMask,
                QueryTriggerInteraction.Ignore);
            if (count >= m_LandingHits.Length)
            {
                capacityExceeded = true;
                return 0;
            }
            return ResolveSupportCandidates(
                m_LandingHits,
                count,
                request.Origin,
                request.Direction.normalized,
                request.SupportMaximumDistance,
                request.MinimumGroundNormalDot,
                out coverage);
        }

        public CharacterFootGroundPathQueryResult Query(
            in CharacterFootGroundPathQueryRequest request,
            CharacterFootGroundContactPage output)
        {
            if (output == null)
                throw new ArgumentNullException(nameof(output));
            output.Clear();
            if (!request.IsValid || request.ContactCapacity != output.Capacity ||
                request.SegmentHitCapacity != m_GroundPathHits.Length)
            {
                return new CharacterFootGroundPathQueryResult(
                    CharacterFootGroundPathRejectReason.InvalidRequest,
                    0);
            }

            float axisLength = Vector3.Distance(request.AxisStart, request.AxisEnd);
            int segmentCount = Mathf.Max(
                1,
                Mathf.CeilToInt(axisLength / request.MaximumAxisSegmentLength));
            if (segmentCount > 256)
            {
                return new CharacterFootGroundPathQueryResult(
                    CharacterFootGroundPathRejectReason.InvalidRequest,
                    0);
            }

            Vector3 direction = request.Direction.normalized;
            for (int segmentIndex = 0; segmentIndex < segmentCount; segmentIndex++)
            {
                float startT = (float)segmentIndex / segmentCount;
                float endT = (float)(segmentIndex + 1) / segmentCount;
                Vector3 segmentStart = Vector3.Lerp(
                    request.AxisStart,
                    request.AxisEnd,
                    startT);
                Vector3 segmentEnd = Vector3.Lerp(
                    request.AxisStart,
                    request.AxisEnd,
                    endT);
                int count = m_PhysicsScene.CapsuleCast(
                    segmentStart,
                    segmentEnd,
                    request.Radius,
                    direction,
                    m_GroundPathHits,
                    request.MaximumDistance,
                    request.LayerMask,
                    QueryTriggerInteraction.Ignore);
                if (count >= m_GroundPathHits.Length)
                {
                    output.Clear();
                    return new CharacterFootGroundPathQueryResult(
                        CharacterFootGroundPathRejectReason.CapacityExceeded,
                        segmentCount);
                }

                int hitCount = Mathf.Min(count, m_GroundPathHits.Length);
                for (int hitIndex = 0; hitIndex < hitCount; hitIndex++)
                {
                    RaycastHit hit = m_GroundPathHits[hitIndex];
                    if (!hit.collider || m_Rig.IsSelfCollider(hit.collider) ||
                        IsInitialOverlap(in hit) || !IsFinite(hit.point) ||
                        !IsFinite(hit.normal) || hit.normal.sqrMagnitude <= 0.000001f ||
                        !float.IsFinite(hit.distance) || hit.distance < 0f)
                    {
                        continue;
                    }
                    int surfaceIdentity = hit.collider.GetInstanceID();
                    if (surfaceIdentity == 0 ||
                        output.Contains(segmentIndex, surfaceIdentity))
                    {
                        continue;
                    }
                    ulong candidateIdentity =
                        ((ulong)(uint)(segmentIndex + 1) << 32) |
                        unchecked((uint)surfaceIdentity);
                    var contact = new CharacterFootGroundContact(
                        segmentIndex,
                        surfaceIdentity,
                        candidateIdentity,
                        hit.point,
                        hit.normal,
                        hit.distance);
                    if (!output.TryAdd(in contact))
                    {
                        output.Clear();
                        return new CharacterFootGroundPathQueryResult(
                            CharacterFootGroundPathRejectReason.CapacityExceeded,
                            segmentCount);
                    }
                }
            }

            if (output.Count == 0)
            {
                return new CharacterFootGroundPathQueryResult(
                    CharacterFootGroundPathRejectReason.NoContact,
                    segmentCount);
            }
            output.SortCanonical();
            CharacterFootGroundSurfacePage surfaces = output.SurfaceCoverage;
            if (!surfaces.Begin(in request, WorldRevision))
            {
                return new CharacterFootGroundPathQueryResult(
                    CharacterFootGroundPathRejectReason.SurfaceGeometryUnavailable, segmentCount);
            }
            m_GroundGeometry ??= new CharacterFootGroundGeometrySource(m_PhysicsScene, m_Rig);
            for (int i = 0; i < output.Count; i++)
            {
                CharacterFootGroundSurfaceState state =
                    m_GroundGeometry.Validate(output.ContactAt(i).SurfaceIdentity);
                if (state != CharacterFootGroundSurfaceState.Ready)
                {
                    surfaces.Fail(state);
                    return new CharacterFootGroundPathQueryResult(
                        SurfaceFailure(state), segmentCount);
                }
            }
            CharacterFootGroundSurfaceState geometryState =
                m_GroundGeometry.Query(in request, surfaces);
            if (geometryState != CharacterFootGroundSurfaceState.Ready)
            {
                surfaces.Fail(geometryState);
                return new CharacterFootGroundPathQueryResult(
                    SurfaceFailure(geometryState), segmentCount);
            }
            surfaces.Complete();
            return new CharacterFootGroundPathQueryResult(
                CharacterFootGroundPathRejectReason.None,
                segmentCount);
        }

        static CharacterFootGroundPathRejectReason SurfaceFailure(
            CharacterFootGroundSurfaceState state) => state switch
        {
            CharacterFootGroundSurfaceState.UnsupportedGeometry =>
                CharacterFootGroundPathRejectReason.SurfaceGeometryUnsupported,
            CharacterFootGroundSurfaceState.GeometryChanged =>
                CharacterFootGroundPathRejectReason.SurfaceGeometryChanged,
            CharacterFootGroundSurfaceState.CapacityExceeded =>
                CharacterFootGroundPathRejectReason.SurfaceCoverageCapacityExceeded,
            _ => CharacterFootGroundPathRejectReason.SurfaceGeometryUnavailable
        };

        public CharacterFootCurrentSupportProbeResult Query(
            in CharacterFootCurrentSupportProbeRequest request)
        {
            ulong queryRevision = WorldRevision;
            if (!request.IsValid ||
                request.HitCapacity != m_CurrentSupportHits.Length)
            {
                return CharacterFootCurrentSupportProbeResult.Rejected(
                    request.Kind,
                    CharacterFootCurrentSupportProbeRejectReason.InvalidRequest,
                    queryRevision,
                    false,
                    default);
            }
            int count = m_PhysicsScene.SphereCast(
                request.Origin,
                request.Radius,
                request.Direction,
                m_CurrentSupportHits,
                request.MaximumDistance,
                request.LayerMask,
                QueryTriggerInteraction.Ignore);
            if (count >= m_CurrentSupportHits.Length)
            {
                return CharacterFootCurrentSupportProbeResult.Rejected(
                    request.Kind,
                    CharacterFootCurrentSupportProbeRejectReason.CapacityExceeded,
                    queryRevision,
                    true,
                    default);
            }
            int validCount = ResolveSupportCandidates(
                m_CurrentSupportHits,
                count,
                request.Origin,
                request.Direction,
                request.SupportMaximumDistance,
                request.MinimumGroundNormalDot,
                out CharacterFootSupportQueryDiagnostics coverage);
            if (validCount == 0)
            {
                return CharacterFootCurrentSupportProbeResult.Rejected(
                    request.Kind,
                    CharacterFootCurrentSupportProbeRejectReason.NoHit,
                    queryRevision,
                    true,
                    coverage);
            }
            RaycastHit selected = m_CurrentSupportHits[0];
            return new CharacterFootCurrentSupportProbeResult(
                request.Kind,
                CharacterFootCurrentSupportProbeState.Accepted,
                CharacterFootCurrentSupportProbeRejectReason.None,
                validCount,
                coverage,
                selected.collider.GetInstanceID(),
                selected.point,
                selected.normal.normalized,
                selected.distance,
                queryRevision,
                true);
        }

        int ResolveSupportCandidates(
            RaycastHit[] hits,
            int count,
            Vector3 origin,
            Vector3 direction,
            float maximumDistance,
            float minimumGroundNormalDot,
            out CharacterFootSupportQueryDiagnostics coverage)
        {
            var supportRay = new Ray(origin, direction);
            Vector3 up = -direction;
            int validCount = 0;
            int outsideSupportRayCount = 0;
            int steepSurfaceCount = 0;
            for (int i = 0; i < count; i++)
            {
                RaycastHit candidate = hits[i];
                Collider collider = candidate.collider;
                if (!collider || m_Rig.IsSelfCollider(collider) ||
                    IsInitialOverlap(in candidate))
                    continue;
                if (!collider.Raycast(supportRay, out RaycastHit support, maximumDistance))
                {
                    outsideSupportRayCount++;
                    continue;
                }
                if (!IsFinite(support.point) || !IsFinite(support.normal) ||
                    support.normal.sqrMagnitude <= 0.000001f ||
                    !float.IsFinite(support.distance) || support.distance < 0f)
                    continue;
                if (Vector3.Dot(support.normal.normalized, up) < minimumGroundNormalDot)
                {
                    steepSurfaceCount++;
                    continue;
                }
                hits[validCount++] = support;
            }
            for (int i = 1; i < validCount; i++)
            {
                RaycastHit value = hits[i];
                int insertion = i;
                while (insertion > 0 && CompareCurrentSupport(value, hits[insertion - 1]) < 0)
                {
                    hits[insertion] = hits[insertion - 1];
                    insertion--;
                }
                hits[insertion] = value;
            }
            coverage = new CharacterFootSupportQueryDiagnostics(
                count, validCount, outsideSupportRayCount, steepSurfaceCount);
            return validCount;
        }

        static int CompareCurrentSupport(RaycastHit left, RaycastHit right)
        {
            int landing = CompareLanding(left, right);
            if (landing != 0)
                return landing;
            int normalX = left.normal.x.CompareTo(right.normal.x);
            if (normalX != 0)
                return normalX;
            int normalY = left.normal.y.CompareTo(right.normal.y);
            if (normalY != 0)
                return normalY;
            int normalZ = left.normal.z.CompareTo(right.normal.z);
            return normalZ != 0
                ? normalZ
                : left.triangleIndex.CompareTo(right.triangleIndex);
        }

        static int CompareLanding(RaycastHit left, RaycastHit right)
        {
            int distance = left.distance.CompareTo(right.distance);
            if (distance != 0)
                return distance;
            int identity = left.collider.GetInstanceID().CompareTo(right.collider.GetInstanceID());
            if (identity != 0)
                return identity;
            int x = left.point.x.CompareTo(right.point.x);
            if (x != 0)
                return x;
            int y = left.point.y.CompareTo(right.point.y);
            return y != 0 ? y : left.point.z.CompareTo(right.point.z);
        }

        static CharacterFootLandingQueryCandidateDiagnostics
            CandidateDiagnostics(in RaycastHit hit) =>
            new CharacterFootLandingQueryCandidateDiagnostics(
                hit.collider.GetInstanceID(),
                hit.point,
                hit.distance);

        static bool IsInitialOverlap(in RaycastHit hit) =>
            hit.distance <= 0.000001f;

        static bool IsGroundRequestValid(in CharacterFootPlacementQueryRequest request) =>
            request.Shape == CharacterFootPlacementQueryShape.Sphere &&
            (request.Purpose == CharacterFootPlacementQueryPurpose.FutureLanding ||
             request.Purpose ==
             CharacterFootPlacementQueryPurpose.CurrentContactVerification) &&
            request.FootIndex >= 0 && request.FootIndex < 2 &&
            request.LayerMask != 0 &&
            IsFinite(request.Origin) &&
            IsFinite(request.Direction) &&
            request.Direction.sqrMagnitude > 0f &&
            float.IsFinite(request.MaximumDistance) && request.MaximumDistance > 0f &&
            float.IsFinite(request.Radius) && request.Radius > 0f &&
            float.IsFinite(request.MinimumGroundNormalDot) &&
            request.MinimumGroundNormalDot >= -1f && request.MinimumGroundNormalDot <= 1f;

        static bool IsFinite(Vector3 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
    }
}
