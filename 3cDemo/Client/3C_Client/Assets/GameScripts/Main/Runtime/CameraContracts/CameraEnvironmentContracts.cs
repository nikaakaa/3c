using UnityEngine;

namespace ThirdPersonCamera
{
    public enum CameraCollisionStatus : byte
    {
        NotEvaluated = 0,
        Clear = 1,
        Corrected = 2,
        StartOverlapped = 3,
        NoLegalSpace = 4
    }

    public enum CameraCollisionTriggerMode : byte
    {
        UseGlobal = 0,
        Ignore = 1,
        Collide = 2
    }

    public readonly struct CameraEnvironmentQueryRequest
    {
        public CameraEnvironmentQueryRequest(
            Vector3 desiredLocation,
            Vector3 pivotLocation,
            Vector3 protectionCenterOffset,
            float protectionRadius,
            float minimumDistance,
            float distanceLimit,
            float cameraRadius,
            int layerMask,
            CameraCollisionTriggerMode triggerMode)
        {
            DesiredLocation = desiredLocation;
            PivotLocation = pivotLocation;
            ProtectionCenterOffset = protectionCenterOffset;
            ProtectionRadius = protectionRadius;
            MinimumDistance = minimumDistance;
            DistanceLimit = distanceLimit;
            CameraRadius = cameraRadius;
            LayerMask = layerMask;
            TriggerMode = triggerMode;
        }

        public Vector3 DesiredLocation { get; }
        public Vector3 PivotLocation { get; }
        public Vector3 ProtectionCenterOffset { get; }
        public float ProtectionRadius { get; }
        public float MinimumDistance { get; }
        public float DistanceLimit { get; }
        public float CameraRadius { get; }
        public int LayerMask { get; }
        public CameraCollisionTriggerMode TriggerMode { get; }
    }

    public readonly struct CameraEnvironmentQueryResult
    {
        public CameraEnvironmentQueryResult(
            CameraCollisionStatus status,
            Vector3 safeLocation,
            float safeDistance,
            Vector3 hitNormal,
            int colliderInstanceId,
            Vector3 occlusionPoint,
            Vector3 occlusionNormal)
        {
            Status = status;
            SafeLocation = safeLocation;
            SafeDistance = safeDistance;
            HitNormal = hitNormal;
            ColliderInstanceId = colliderInstanceId;
            OcclusionPoint = occlusionPoint;
            OcclusionNormal = occlusionNormal;
        }

        public CameraCollisionStatus Status { get; }
        public Vector3 SafeLocation { get; }
        public float SafeDistance { get; }
        public Vector3 HitNormal { get; }
        public int ColliderInstanceId { get; }
        public Vector3 OcclusionPoint { get; }
        public Vector3 OcclusionNormal { get; }
    }

    public readonly struct CameraCollisionResult
    {
        public CameraCollisionResult(
            CameraCollisionStatus status,
            Vector3 desiredLocation,
            Vector3 constrainedLocation,
            Vector3 hitNormal,
            float correctionDistance,
            int colliderInstanceId)
        {
            Status = status;
            DesiredLocation = desiredLocation;
            ConstrainedLocation = constrainedLocation;
            HitNormal = hitNormal;
            CorrectionDistance = correctionDistance;
            ColliderInstanceId = colliderInstanceId;
        }

        public CameraCollisionStatus Status { get; }
        public Vector3 DesiredLocation { get; }
        public Vector3 ConstrainedLocation { get; }
        public Vector3 HitNormal { get; }
        public float CorrectionDistance { get; }
        public int ColliderInstanceId { get; }
        public bool IsApplied => Status != CameraCollisionStatus.NotEvaluated;
        public bool IsSafe => Status != CameraCollisionStatus.NoLegalSpace;

        public static CameraCollisionResult NotEvaluated(Vector3 desiredLocation) =>
            new CameraCollisionResult(
                CameraCollisionStatus.NotEvaluated,
                desiredLocation,
                desiredLocation,
                Vector3.zero,
                0f,
                0);
    }

    public interface ICameraEnvironmentQuery
    {
        CameraEnvironmentQueryResult Resolve(in CameraEnvironmentQueryRequest request);
    }
}
