using UnityEngine;

namespace ThirdPersonCamera
{
    public readonly struct CameraArmCollisionState
    {
        public CameraArmCollisionState(
            bool hasHit,
            float currentDistance,
            float targetDistance,
            Vector3 anchorPosition,
            Vector3 desiredPosition,
            Vector3 resolvedPosition,
            Collider hitCollider)
        {
            HasHit = hasHit;
            CurrentDistance = currentDistance;
            TargetDistance = targetDistance;
            AnchorPosition = anchorPosition;
            DesiredPosition = desiredPosition;
            ResolvedPosition = resolvedPosition;
            HitCollider = hitCollider;
        }

        public bool HasHit { get; }
        public float CurrentDistance { get; }
        public float TargetDistance { get; }
        public Vector3 AnchorPosition { get; }
        public Vector3 DesiredPosition { get; }
        public Vector3 ResolvedPosition { get; }
        public Collider HitCollider { get; }
        public string HitName => HitCollider != null ? HitCollider.name : string.Empty;
    }
}
