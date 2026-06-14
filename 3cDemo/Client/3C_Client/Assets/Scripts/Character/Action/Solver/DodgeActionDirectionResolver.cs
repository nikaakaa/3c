using ThirdPersonMovement;
using UnityEngine;

namespace ThirdPersonAction
{
    public static class DodgeActionDirectionResolver
    {
        public static bool TryResolve(
            in MovementInputIntent intent,
            Vector3 currentWorldMoveDirection,
            Vector3 facingForward,
            out DodgeActionVariant variant,
            out Vector3 worldDirection)
        {
            if (intent.HasMoveIntent)
            {
                variant = DodgeActionVariant.Directional;
                worldDirection = NormalizePlanarOrZero(currentWorldMoveDirection);
                return worldDirection.sqrMagnitude > 0.000001f;
            }

            variant = DodgeActionVariant.Backstep;
            worldDirection = NormalizePlanarOrZero(-facingForward);
            return worldDirection.sqrMagnitude > 0.000001f;
        }

        static Vector3 NormalizePlanarOrZero(Vector3 value)
        {
            value.y = 0f;
            float sqrMagnitude = value.sqrMagnitude;
            return sqrMagnitude > 0.000001f ? value / Mathf.Sqrt(sqrMagnitude) : Vector3.zero;
        }
    }
}
