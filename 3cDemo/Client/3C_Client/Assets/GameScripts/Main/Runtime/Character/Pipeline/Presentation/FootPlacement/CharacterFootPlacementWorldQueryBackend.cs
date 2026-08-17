using System;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    public enum CharacterFootPlacementQueryShape : byte
    {
        Sphere = 1
    }

    public enum CharacterFootPlacementQueryPurpose : byte
    {
        FutureLanding = 1
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

        public CharacterFootPlacementQueryShape Shape { get; }
        public CharacterFootPlacementQueryPurpose Purpose { get; }
        public int FootIndex { get; }
        public Vector3 Origin { get; }
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

        internal bool Query(
            in CharacterFootPlacementQueryRequest request,
            out CharacterFootPlacementQueryHit hit)
        {
            int count = QueryAll(in request);
            if (count <= 0)
            {
                hit = default;
                return false;
            }
            hit = new CharacterFootPlacementQueryHit(m_Hits[0]);
            return true;
        }

        internal int QueryAll(in CharacterFootPlacementQueryRequest request)
        {
            if (!IsRequestValid(in request))
                return 0;
            int count = m_PhysicsScene.SphereCast(
                request.Origin,
                request.Radius,
                request.Direction.normalized,
                m_Hits,
                request.MaximumDistance,
                request.LayerMask,
                QueryTriggerInteraction.Ignore);
            Vector3 supportUp = -request.Direction.normalized;
            int hitCount = Mathf.Min(count, m_Hits.Length);
            int validCount = 0;
            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit candidate = m_Hits[i];
                if (!candidate.collider ||
                    m_Rig.IsSelfCollider(candidate.collider) ||
                    IsInitialOverlap(in candidate) ||
                    !IsFinite(candidate.point) ||
                    !IsFinite(candidate.normal) ||
                    candidate.normal.sqrMagnitude <= 0.000001f ||
                    Vector3.Dot(candidate.normal.normalized, supportUp) < request.MinimumGroundNormalDot ||
                    !float.IsFinite(candidate.distance) ||
                    candidate.distance < 0f)
                {
                    continue;
                }
                m_Hits[validCount++] = candidate;
            }
            for (int i = 1; i < validCount; i++)
            {
                RaycastHit value = m_Hits[i];
                int insertion = i;
                while (insertion > 0 && Compare(value, m_Hits[insertion - 1]) < 0)
                {
                    m_Hits[insertion] = m_Hits[insertion - 1];
                    insertion--;
                }
                m_Hits[insertion] = value;
            }
            return validCount;
        }

        static int Compare(RaycastHit left, RaycastHit right)
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

        static bool IsInitialOverlap(in RaycastHit hit) =>
            hit.distance <= 0.000001f;

        static bool IsRequestValid(in CharacterFootPlacementQueryRequest request) =>
            request.Shape == CharacterFootPlacementQueryShape.Sphere &&
            request.Purpose == CharacterFootPlacementQueryPurpose.FutureLanding &&
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
