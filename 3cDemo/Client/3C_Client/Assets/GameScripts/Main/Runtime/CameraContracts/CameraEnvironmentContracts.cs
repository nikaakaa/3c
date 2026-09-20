using System;
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
            Vector3 previousLocation,
            Vector3 desiredLocation,
            Vector3 pivotLocation,
            float radius,
            float nearClipPlane,
            int layerMask,
            CameraCollisionTriggerMode triggerMode,
            float deltaSeconds,
            bool reset)
        {
            if (!Finite(previousLocation) || !Finite(desiredLocation) || !Finite(pivotLocation) ||
                !float.IsFinite(radius) || radius < 0f || !float.IsFinite(nearClipPlane) || nearClipPlane < 0f ||
                !float.IsFinite(deltaSeconds) || deltaSeconds < 0f)
                throw new ArgumentOutOfRangeException(nameof(desiredLocation));
            if (triggerMode != CameraCollisionTriggerMode.UseGlobal &&
                triggerMode != CameraCollisionTriggerMode.Ignore &&
                triggerMode != CameraCollisionTriggerMode.Collide)
                throw new ArgumentOutOfRangeException(nameof(triggerMode));
            PreviousLocation = previousLocation;
            DesiredLocation = desiredLocation;
            PivotLocation = pivotLocation;
            Radius = radius;
            NearClipPlane = nearClipPlane;
            LayerMask = layerMask;
            TriggerMode = triggerMode;
            DeltaSeconds = deltaSeconds;
            Reset = reset;
        }

        public Vector3 PreviousLocation { get; }
        public Vector3 DesiredLocation { get; }
        public Vector3 PivotLocation { get; }
        public float Radius { get; }
        public float NearClipPlane { get; }
        public int LayerMask { get; }
        public CameraCollisionTriggerMode TriggerMode { get; }
        public float DeltaSeconds { get; }
        public bool Reset { get; }

        static bool Finite(Vector3 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
    }

    public readonly struct CameraEnvironmentQueryResult
    {
        public CameraEnvironmentQueryResult(
            CameraCollisionStatus status,
            Vector3 safeLocation,
            Vector3 hitNormal,
            float hitDistance,
            string colliderIdentity)
        {
            if ((status != CameraCollisionStatus.NotEvaluated &&
                 status != CameraCollisionStatus.Clear &&
                 status != CameraCollisionStatus.Corrected &&
                 status != CameraCollisionStatus.StartOverlapped &&
                 status != CameraCollisionStatus.NoLegalSpace) ||
                !Finite(safeLocation) || !Finite(hitNormal) || !float.IsFinite(hitDistance) || hitDistance < 0f)
                throw new ArgumentOutOfRangeException(nameof(status));
            Status = status;
            SafeLocation = safeLocation;
            HitNormal = hitNormal;
            HitDistance = hitDistance;
            ColliderIdentity = colliderIdentity ?? string.Empty;
        }

        public CameraCollisionStatus Status { get; }
        public Vector3 SafeLocation { get; }
        public Vector3 HitNormal { get; }
        public float HitDistance { get; }
        public string ColliderIdentity { get; }

        public static CameraEnvironmentQueryResult Clear(Vector3 desiredLocation) =>
            new CameraEnvironmentQueryResult(
                CameraCollisionStatus.Clear,
                desiredLocation,
                Vector3.zero,
                0f,
                string.Empty);

        static bool Finite(Vector3 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
    }

    public readonly struct CameraCollisionResult
    {
        public CameraCollisionResult(
            CameraCollisionStatus status,
            Vector3 desiredLocation,
            Vector3 constrainedLocation,
            Vector3 hitNormal,
            float correctionDistance,
            string colliderIdentity)
        {
            if ((status != CameraCollisionStatus.NotEvaluated &&
                 status != CameraCollisionStatus.Clear &&
                 status != CameraCollisionStatus.Corrected &&
                 status != CameraCollisionStatus.StartOverlapped &&
                 status != CameraCollisionStatus.NoLegalSpace) ||
                !Finite(desiredLocation) || !Finite(constrainedLocation) || !Finite(hitNormal) ||
                !float.IsFinite(correctionDistance) || correctionDistance < 0f)
                throw new ArgumentOutOfRangeException(nameof(status));
            Status = status;
            DesiredLocation = desiredLocation;
            ConstrainedLocation = constrainedLocation;
            HitNormal = hitNormal;
            CorrectionDistance = correctionDistance;
            ColliderIdentity = colliderIdentity ?? string.Empty;
        }

        public CameraCollisionStatus Status { get; }
        public Vector3 DesiredLocation { get; }
        public Vector3 ConstrainedLocation { get; }
        public Vector3 HitNormal { get; }
        public float CorrectionDistance { get; }
        public string ColliderIdentity { get; }
        public bool IsApplied => Status != CameraCollisionStatus.NotEvaluated;
        public bool IsSafe => Status != CameraCollisionStatus.NoLegalSpace;

        public static CameraCollisionResult NotEvaluated(Vector3 desiredLocation) =>
            new CameraCollisionResult(
                CameraCollisionStatus.NotEvaluated,
                desiredLocation,
                desiredLocation,
                Vector3.zero,
                0f,
                string.Empty);

        static bool Finite(Vector3 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
    }

    public interface ICameraEnvironmentQuery
    {
        CameraEnvironmentQueryResult Resolve(in CameraEnvironmentQueryRequest request);
    }
}
