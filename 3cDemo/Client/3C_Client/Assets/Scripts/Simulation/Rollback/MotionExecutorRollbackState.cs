using UnityEngine;

namespace ThirdPersonSimulation
{
    public readonly struct MotionExecutorRollbackState
    {
        public MotionExecutorRollbackState(float currentSpeed, Vector3 lastWorldDirection, float verticalVelocity)
            : this(currentSpeed, lastWorldDirection, verticalVelocity, Vector3.zero, 0f, false)
        {
        }

        public MotionExecutorRollbackState(
            float currentSpeed,
            Vector3 lastWorldDirection,
            float verticalVelocity,
            Vector3 rootPosition,
            float rootYaw,
            bool hasRootPose)
        {
            CurrentSpeed = Mathf.Max(0f, SanitizeFinite(currentSpeed));
            LastWorldDirection = NormalizePlanarOrZero(lastWorldDirection);
            VerticalVelocity = SanitizeFinite(verticalVelocity);
            RootPosition = Sanitize(rootPosition);
            RootYaw = NormalizeYaw(rootYaw);
            HasRootPose = hasRootPose;
        }

        public float CurrentSpeed { get; }
        public Vector3 LastWorldDirection { get; }
        public float VerticalVelocity { get; }
        public Vector3 RootPosition { get; }
        public float RootYaw { get; }
        public bool HasRootPose { get; }

        public static MotionExecutorRollbackState Empty => new MotionExecutorRollbackState(0f, Vector3.zero, 0f);

        static float SanitizeFinite(float value)
        {
            return float.IsNaN(value) || float.IsInfinity(value) ? 0f : value;
        }

        static Vector3 Sanitize(Vector3 value)
        {
            return new Vector3(SanitizeFinite(value.x), SanitizeFinite(value.y), SanitizeFinite(value.z));
        }

        static float NormalizeYaw(float value)
        {
            value = SanitizeFinite(value) % 360f;
            return value < 0f ? value + 360f : value;
        }

        static Vector3 NormalizePlanarOrZero(Vector3 value)
        {
            value.y = 0f;
            float sqrMagnitude = value.sqrMagnitude;
            return sqrMagnitude > 0.000001f ? value / Mathf.Sqrt(sqrMagnitude) : Vector3.zero;
        }
    }
}
