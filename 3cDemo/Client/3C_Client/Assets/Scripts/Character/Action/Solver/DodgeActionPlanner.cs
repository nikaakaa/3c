using ThirdPersonInput;
using ThirdPersonMovement;
using UnityEngine;

namespace ThirdPersonAction
{
    public static class DodgeActionPlanner
    {
        public static bool TryBuildRequest(
            InputRequestBuffer inputBuffer,
            int currentStep,
            in MovementInputIntent movementIntent,
            Vector3 currentWorldMoveDirection,
            IFacingDirectionProvider facingProvider,
            in DodgeActionConfig config,
            out DodgeActionRequest request)
        {
            if (inputBuffer == null || !inputBuffer.TryPeek(InputRequestKind.Dodge, currentStep, out BufferedInputRequest inputRequest))
            {
                request = default;
                return false;
            }

            if (!DodgeActionDirectionResolver.TryResolve(movementIntent, currentWorldMoveDirection, facingProvider, out DodgeActionVariant variant, out Vector3 worldDirection))
            {
                request = default;
                return false;
            }

            request = new DodgeActionRequest(
                variant,
                worldDirection,
                inputRequest.OriginStep,
                inputRequest.ExpireStep,
                config.Priority,
                inputRequest.OriginStep,
                ActionStateIds.Dodge);
            return true;
        }

        public static ActionAnimationKey ResolveAnimationKey(DodgeActionVariant variant)
        {
            return variant == DodgeActionVariant.Backstep
                ? ActionAnimationKeys.DodgeBackstep
                : ActionAnimationKeys.DodgeDirectional;
        }
    }
}
