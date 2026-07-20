using UnityEngine;

namespace ThirdPersonCamera
{
    public static class CameraBasisResolver
    {
        public static void ResolvePlanarBasis(Quaternion rotation, out Vector3 planarForward, out Vector3 planarRight)
        {
            planarForward = NormalizePlanar(rotation * Vector3.forward);
            planarRight = NormalizePlanar(rotation * Vector3.right);
        }

        public static Vector3 NormalizePlanar(Vector3 value)
        {
            value.y = 0f;
            float sqrMagnitude = value.sqrMagnitude;
            return sqrMagnitude > 0.000001f ? value / Mathf.Sqrt(sqrMagnitude) : Vector3.zero;
        }
    }
}
