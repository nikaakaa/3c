using System;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    public enum CharacterFootPlacementQueryShape : byte
    {
        Sphere = 1,
        Capsule = 2
    }

    public enum CharacterFootPlacementQueryPurpose : byte
    {
        CurrentGrounding = 1,
        FutureLanding = 2,
        GroundEnvelope = 3,
        SwingClearance = 4
    }

    public readonly struct CharacterFootPlacementQueryRequest
    {
        public CharacterFootPlacementQueryRequest(
            CharacterFootPlacementQueryShape shape,
            CharacterFootPlacementQueryPurpose purpose,
            int footIndex,
            Vector3 origin,
            Vector3 capsuleEnd,
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
            CapsuleEnd = capsuleEnd;
            Direction = direction;
            MaximumDistance = maximumDistance;
            Radius = radius;
            LayerMask = layerMask;
            MinimumGroundNormalDot = minimumGroundNormalDot;
        }

        public CharacterFootPlacementQueryShape Shape { get; }
        public CharacterFootPlacementQueryPurpose Purpose { get; }
        public int FootIndex { get; }
        public Vector3 Origin { get; }
        public Vector3 CapsuleEnd { get; }
        public Vector3 Direction { get; }
        public float MaximumDistance { get; }
        public float Radius { get; }
        public int LayerMask { get; }
        public float MinimumGroundNormalDot { get; }
    }

    public readonly struct CharacterFootPlacementQueryHit
    {
        internal CharacterFootPlacementQueryHit(RaycastHit hit)
        {
            PhysicsHit = hit;
            SurfaceIdentity = hit.collider ? hit.collider.GetInstanceID() : 0;
            HasHit = hit.collider;
            Location = HasHit ? hit.point : Vector3.zero;
        }

        public bool HasHit { get; }
        public RaycastHit PhysicsHit { get; }
        public int SurfaceIdentity { get; }
        public Vector3 Location { get; }
        public Vector3 Point => HasHit ? PhysicsHit.point : Vector3.zero;
        public Vector3 Normal => HasHit ? PhysicsHit.normal : Vector3.up;
        public float Distance => HasHit ? PhysicsHit.distance : 0f;
    }

    internal sealed class CharacterFootPlacementWorldQueryBackend
    {
        readonly PhysicsScene m_PhysicsScene;
        readonly CharacterFootPlacementPoseRig m_Rig;
        readonly RaycastHit[] m_Hits;
        readonly CharacterFootPlacementQueryRequest[] m_LastCurrentRequests =
            new CharacterFootPlacementQueryRequest[2];
        readonly bool[] m_HasLastCurrentRequest = new bool[2];

        internal CharacterFootPlacementWorldQueryBackend(
            PhysicsScene physicsScene,
            CharacterFootPlacementPoseRig rig,
            int hitCapacity)
        {
            if (!physicsScene.IsValid())
                throw new ArgumentException("Foot Placement requires a valid PhysicsScene.", nameof(physicsScene));
            if (hitCapacity < 4 || hitCapacity > 32)
                throw new ArgumentOutOfRangeException(nameof(hitCapacity));
            m_PhysicsScene = physicsScene;
            m_Rig = rig ?? throw new ArgumentNullException(nameof(rig));
            m_Hits = new RaycastHit[hitCapacity];
        }

        internal PhysicsScene PhysicsScene => m_PhysicsScene;
        internal RaycastHit[] HitWorkspace => m_Hits;

        internal void BeginFrame()
        {
            m_HasLastCurrentRequest[0] = false;
            m_HasLastCurrentRequest[1] = false;
        }

        internal bool TryGetLastCurrentRequest(
            int footIndex,
            out CharacterFootPlacementQueryRequest request)
        {
            if ((uint)footIndex >= 2u || !m_HasLastCurrentRequest[footIndex])
            {
                request = default;
                return false;
            }
            request = m_LastCurrentRequests[footIndex];
            return true;
        }

        internal bool Query(
            in CharacterFootPlacementQueryRequest request,
            out CharacterFootPlacementQueryHit hit)
        {
            if (!IsRequestValid(in request))
            {
                hit = default;
                return false;
            }
            if (request.Purpose == CharacterFootPlacementQueryPurpose.CurrentGrounding &&
                request.FootIndex >= 0)
            {
                m_LastCurrentRequests[request.FootIndex] = request;
                m_HasLastCurrentRequest[request.FootIndex] = true;
            }

            int count = request.Shape == CharacterFootPlacementQueryShape.Sphere
                ? m_PhysicsScene.SphereCast(
                    request.Origin,
                    request.Radius,
                    request.Direction.normalized,
                    m_Hits,
                    request.MaximumDistance,
                    request.LayerMask,
                    QueryTriggerInteraction.Ignore)
                : m_PhysicsScene.CapsuleCast(
                    request.Origin,
                    request.CapsuleEnd,
                    request.Radius,
                    request.Direction.normalized,
                    m_Hits,
                    request.MaximumDistance,
                    request.LayerMask,
                    QueryTriggerInteraction.Ignore);
            if (!TrySelectHit(count, in request, out RaycastHit selected))
            {
                hit = default;
                return false;
            }
            hit = new CharacterFootPlacementQueryHit(selected);
            return true;
        }

        bool TrySelectHit(
            int count,
            in CharacterFootPlacementQueryRequest request,
            out RaycastHit selected)
        {
            selected = default;
            bool found = false;
            float selectedDistance = float.PositiveInfinity;
            int selectedIdentity = int.MaxValue;
            Vector3 supportUp = -request.Direction.normalized;
            int hitCount = Mathf.Min(count, m_Hits.Length);
            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit candidate = m_Hits[i];
                if (!candidate.collider ||
                    m_Rig.IsSelfCollider(candidate.collider) ||
                    !IsFinite(candidate.point) ||
                    !IsFinite(candidate.normal) ||
                    candidate.normal.sqrMagnitude <= 0.000001f ||
                    Vector3.Dot(candidate.normal.normalized, supportUp) < request.MinimumGroundNormalDot ||
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
                selected = candidate;
                selectedDistance = candidate.distance;
                selectedIdentity = identity;
                found = true;
            }
            return found;
        }

        bool IsRequestValid(in CharacterFootPlacementQueryRequest request) =>
            request.FootIndex >= 0 && request.FootIndex < 2 &&
            request.LayerMask != 0 &&
            IsFinite(request.Origin) &&
            IsFinite(request.CapsuleEnd) &&
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
