using UnityEngine;

namespace ThirdPersonSimulation
{
    public readonly struct RollbackCameraBasisState
    {
        public RollbackCameraBasisState(Vector3 planarForward, Vector3 planarRight, float yaw)
        {
            PlanarForward = NormalizePlanarOrZero(planarForward);
            PlanarRight = NormalizePlanarOrZero(planarRight);
            Yaw = NormalizeYaw(yaw);
        }

        public Vector3 PlanarForward { get; }
        public Vector3 PlanarRight { get; }
        public float Yaw { get; }

        public static RollbackCameraBasisState Default => new RollbackCameraBasisState(Vector3.forward, Vector3.right, 0f);

        static Vector3 NormalizePlanarOrZero(Vector3 value)
        {
            value.y = 0f;
            float sqrMagnitude = value.sqrMagnitude;
            return sqrMagnitude > 0.000001f ? value / Mathf.Sqrt(sqrMagnitude) : Vector3.zero;
        }

        static float NormalizeYaw(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
                return 0f;

            value %= 360f;
            return value < 0f ? value + 360f : value;
        }
    }
}
