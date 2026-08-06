using System;
using RootMotion.FinalIK;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    internal readonly struct CharacterFutureLandingQueryRequest
    {
        internal CharacterFutureLandingQueryRequest(in GroundingQueryRequest groundingRequest)
        {
            if (groundingRequest.Shape != GroundingQueryShape.Sphere ||
                groundingRequest.Purpose != GroundingQueryPurpose.FutureLanding)
            {
                throw new ArgumentException(
                    "Future Landing requires a Sphere Grounding request.",
                    nameof(groundingRequest));
            }
            GroundingRequest = groundingRequest;
        }

        internal GroundingQueryRequest GroundingRequest { get; }
    }

    internal enum CharacterPathSampleQueryKind : byte
    {
        GroundEnvelope = 1,
        SwingClearance = 2
    }

    internal readonly struct CharacterPathSampleQueryRequest
    {
        internal CharacterPathSampleQueryRequest(
            CharacterPathSampleQueryKind kind,
            in GroundingQueryRequest groundingRequest)
        {
            if (kind != CharacterPathSampleQueryKind.GroundEnvelope &&
                kind != CharacterPathSampleQueryKind.SwingClearance)
            {
                throw new ArgumentOutOfRangeException(nameof(kind));
            }
            if (kind == CharacterPathSampleQueryKind.GroundEnvelope &&
                    (groundingRequest.Shape != GroundingQueryShape.Sphere ||
                     groundingRequest.Purpose != GroundingQueryPurpose.GroundEnvelope) ||
                kind == CharacterPathSampleQueryKind.SwingClearance &&
                    (groundingRequest.Shape != GroundingQueryShape.Capsule ||
                     groundingRequest.Purpose != GroundingQueryPurpose.SwingClearance))
            {
                throw new ArgumentException(
                    $"Path Sample '{kind}' has invalid Grounding shape '{groundingRequest.Shape}'.",
                    nameof(groundingRequest));
            }
            Kind = kind;
            GroundingRequest = groundingRequest;
        }

        internal CharacterPathSampleQueryKind Kind { get; }
        internal GroundingQueryRequest GroundingRequest { get; }
    }

    internal sealed class CharacterFootPlacementWorldQueryBackend : IGroundingWorldQueryBackend
    {
        readonly PhysicsScene m_PhysicsScene;
        readonly CharacterFootPlacementPoseRig m_Rig;
        readonly RaycastHit[] m_Hits;
        float m_MinimumGroundNormalDot;
        readonly GroundingQueryRequest[] m_LastRayRequests = new GroundingQueryRequest[2];
        readonly GroundingQueryRequest[] m_LastToeRequests = new GroundingQueryRequest[2];
        readonly GroundingQueryRequest[] m_LastFootCenterRequests = new GroundingQueryRequest[2];
        readonly bool[] m_HasLastRayRequest = new bool[2];
        readonly bool[] m_HasLastToeRequest = new bool[2];
        readonly bool[] m_HasLastFootCenterRequest = new bool[2];

        internal CharacterFootPlacementWorldQueryBackend(
            PhysicsScene physicsScene,
            CharacterFootPlacementPoseRig rig,
            int hitCapacity,
            float maximumSlopeDegrees)
        {
            if (!physicsScene.IsValid())
                throw new ArgumentException("Foot Placement requires a valid PhysicsScene.", nameof(physicsScene));
            if (hitCapacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(hitCapacity));
            if (!float.IsFinite(maximumSlopeDegrees) || maximumSlopeDegrees < 0f || maximumSlopeDegrees >= 90f)
                throw new ArgumentOutOfRangeException(nameof(maximumSlopeDegrees));
            m_PhysicsScene = physicsScene;
            m_Rig = rig ?? throw new ArgumentNullException(nameof(rig));
            m_Hits = new RaycastHit[hitCapacity];
            m_MinimumGroundNormalDot = Mathf.Cos(maximumSlopeDegrees * Mathf.Deg2Rad);
        }

        internal void ApplyMaximumSlope(float maximumSlopeDegrees)
        {
            if (!float.IsFinite(maximumSlopeDegrees) || maximumSlopeDegrees < 0f || maximumSlopeDegrees >= 90f)
                throw new ArgumentOutOfRangeException(nameof(maximumSlopeDegrees));
            m_MinimumGroundNormalDot = Mathf.Cos(maximumSlopeDegrees * Mathf.Deg2Rad);
        }

        internal PhysicsScene PhysicsScene => m_PhysicsScene;
        internal RaycastHit[] HitWorkspace => m_Hits;

        internal void BeginGroundingFrameDiagnostics()
        {
            m_HasLastRayRequest[0] = false;
            m_HasLastRayRequest[1] = false;
            m_HasLastToeRequest[0] = false;
            m_HasLastToeRequest[1] = false;
            m_HasLastFootCenterRequest[0] = false;
            m_HasLastFootCenterRequest[1] = false;
        }

        internal bool TryGetLastRayRequest(int footIndex, out GroundingQueryRequest request)
        {
            if ((uint)footIndex >= 2u || !m_HasLastRayRequest[footIndex])
            {
                request = default;
                return false;
            }
            request = m_LastRayRequests[footIndex];
            return true;
        }

        internal bool TryGetLastFootCenterRequest(int footIndex, out GroundingQueryRequest request)
        {
            if ((uint)footIndex >= 2u || !m_HasLastFootCenterRequest[footIndex])
            {
                request = default;
                return false;
            }
            request = m_LastFootCenterRequests[footIndex];
            return true;
        }

        internal bool TryGetLastToeRequest(int footIndex, out GroundingQueryRequest request)
        {
            if ((uint)footIndex >= 2u || !m_HasLastToeRequest[footIndex])
            {
                request = default;
                return false;
            }
            request = m_LastToeRequests[footIndex];
            return true;
        }

        public bool Query(in GroundingQueryRequest request, out GroundingQueryHit hit) =>
            QueryCore(in request, true, out hit);

        internal bool Query(
            in CharacterFutureLandingQueryRequest request,
            out GroundingQueryHit hit) =>
            QueryCore(request.GroundingRequest, false, out hit);

        internal bool Query(
            in CharacterPathSampleQueryRequest request,
            out GroundingQueryHit hit) =>
            QueryCore(request.GroundingRequest, false, out hit);

        bool QueryCore(
            in GroundingQueryRequest request,
            bool recordCurrentGroundingDiagnostics,
            out GroundingQueryHit hit)
        {
            if (!request.PhysicsScene.Equals(m_PhysicsScene) ||
                request.FootIndex < -1 || request.FootIndex >= 2 ||
                request.LayerMask == 0 ||
                !IsFinite(request.Origin) ||
                !IsFinite(request.Direction) ||
                request.Direction.sqrMagnitude <= 0f ||
                !float.IsFinite(request.MaxDistance) ||
                request.MaxDistance <= 0f)
            {
                hit = default;
                return false;
            }

            if (recordCurrentGroundingDiagnostics && request.FootIndex >= 0 &&
                request.Purpose == GroundingQueryPurpose.Heel)
            {
                m_LastRayRequests[request.FootIndex] = request;
                m_HasLastRayRequest[request.FootIndex] = true;
            }
            else if (recordCurrentGroundingDiagnostics && request.FootIndex >= 0 &&
                     request.Purpose == GroundingQueryPurpose.Toe)
            {
                m_LastToeRequests[request.FootIndex] = request;
                m_HasLastToeRequest[request.FootIndex] = true;
            }
            else if (recordCurrentGroundingDiagnostics && request.FootIndex >= 0)
            {
                m_LastFootCenterRequests[request.FootIndex] = request;
                m_HasLastFootCenterRequest[request.FootIndex] = true;
            }

            int count;
            switch (request.Shape)
            {
                case GroundingQueryShape.Ray:
                    count = m_PhysicsScene.Raycast(
                        request.Origin,
                        request.Direction.normalized,
                        m_Hits,
                        request.MaxDistance,
                        request.LayerMask,
                        QueryTriggerInteraction.Ignore);
                    break;
                case GroundingQueryShape.Sphere:
                    if (!float.IsFinite(request.Radius) || request.Radius <= 0f)
                    {
                        hit = default;
                        return false;
                    }
                    count = m_PhysicsScene.SphereCast(
                        request.Origin,
                        request.Radius,
                        request.Direction.normalized,
                        m_Hits,
                        request.MaxDistance,
                        request.LayerMask,
                        QueryTriggerInteraction.Ignore);
                    break;
                case GroundingQueryShape.Capsule:
                    if (!IsFinite(request.CapsuleEnd) ||
                        !float.IsFinite(request.Radius) ||
                        request.Radius <= 0f)
                    {
                        hit = default;
                        return false;
                    }
                    count = m_PhysicsScene.CapsuleCast(
                        request.Origin,
                        request.CapsuleEnd,
                        request.Radius,
                        request.Direction.normalized,
                        m_Hits,
                        request.MaxDistance,
                        request.LayerMask,
                        QueryTriggerInteraction.Ignore);
                    break;
                default:
                    hit = default;
                    return false;
            }

            if (!TrySelectHit(count, request.Direction, out RaycastHit selected, out int selectedIdentity))
            {
                hit = default;
                return false;
            }

            hit = new GroundingQueryHit(true, selected, selectedIdentity);
            return true;
        }

        bool TrySelectHit(
            int count,
            Vector3 direction,
            out RaycastHit selected,
            out int selectedIdentity)
        {
            bool found = false;
            selected = default;
            selectedIdentity = int.MaxValue;
            float selectedDistance = float.PositiveInfinity;
            int hitCount = Mathf.Min(count, m_Hits.Length);
            Vector3 supportUp = -direction.normalized;
            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit candidate = m_Hits[i];
                if (!candidate.collider ||
                    m_Rig.IsSelfCollider(candidate.collider) ||
                    !IsFinite(candidate.point) ||
                    !IsFinite(candidate.normal) ||
                    candidate.normal.sqrMagnitude <= 0.000001f ||
                    Vector3.Dot(candidate.normal.normalized, supportUp) < m_MinimumGroundNormalDot ||
                    !float.IsFinite(candidate.distance) ||
                    candidate.distance < 0f)
                {
                    continue;
                }
                int identity = candidate.collider.GetInstanceID();
                if (candidate.distance > selectedDistance ||
                    Mathf.Approximately(candidate.distance, selectedDistance) && identity >= selectedIdentity)
                {
                    continue;
                }
                found = true;
                selected = candidate;
                selectedDistance = candidate.distance;
                selectedIdentity = identity;
            }
            return found;
        }

        static bool IsFinite(Vector3 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
    }
}
