using ThirdPersonCamera;
using ThirdPersonMovement;
using UnityEngine;

namespace ThirdPersonAction
{
    public static class DodgeActionDirectionResolver
    {
        public static bool TryResolve(
            in BasicLocomotionInputSnapshot input,
            in BasicMovementSettings settings,
            ICameraMovementBasisProvider cameraBasisProvider,
            IFacingDirectionProvider facingProvider,
            out DodgeActionVariant variant,
            out Vector3 worldDirection)
        {
            MovementInputIntent intent = MovementInputIntent.FromRaw(input.Move, settings.InputDeadZone, input.RunHeld);
            return TryResolve(intent, CameraRelativeMovementResolver.Resolve(intent, cameraBasisProvider), facingProvider, out variant, out worldDirection);
        }

        public static bool TryResolve(
            in MovementInputIntent intent,
            Vector3 currentWorldMoveDirection,
            IFacingDirectionProvider facingProvider,
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
            worldDirection = facingProvider != null ? NormalizePlanarOrZero(-facingProvider.FacingForward) : Vector3.zero;
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
