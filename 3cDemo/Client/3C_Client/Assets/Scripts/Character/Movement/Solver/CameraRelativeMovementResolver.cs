using ThirdPersonCamera;
using UnityEngine;

namespace ThirdPersonMovement
{
    public static class CameraRelativeMovementResolver
    {
        public static Vector3 Resolve(in MovementInputIntent intent, ICameraMovementBasisProvider basisProvider)
        {
            if (!intent.HasMoveIntent || basisProvider == null)
                return Vector3.zero;

            return Resolve(intent.NormalizedInput, basisProvider.CameraPlanarForward, basisProvider.CameraPlanarRight);
        }

        public static Vector3 Resolve(Vector2 input, Vector3 planarForward, Vector3 planarRight)
        {
            if (input.sqrMagnitude <= 0.000001f)
                return Vector3.zero;

            planarForward.y = 0f;
            planarRight.y = 0f;
            planarForward = NormalizeOrZero(planarForward);
            planarRight = NormalizeOrZero(planarRight);

            Vector3 worldDirection = planarForward * input.y + planarRight * input.x;
            worldDirection.y = 0f;
            return NormalizeOrZero(worldDirection);
        }

        static Vector3 NormalizeOrZero(Vector3 value)
        {
            float sqrMagnitude = value.sqrMagnitude;
            return sqrMagnitude > 0.000001f ? value / Mathf.Sqrt(sqrMagnitude) : Vector3.zero;
        }
    }
}
