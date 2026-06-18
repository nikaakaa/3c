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
            Vector3 facingForward,
            in DodgeActionTuning tuning,
            out DodgeActionRequest request)
        {
            if (inputBuffer == null || !inputBuffer.TryPeek(InputRequestKind.Dodge, currentStep, out BufferedInputRequest inputRequest))
            {
                request = default;
                return false;
            }

            CharacterActionRequest actionRequest = CharacterActionRequest.FromBufferedInput(
                CharacterFrameRequestProviderId.Dodge,
                ActionRequestType.Dodge,
                in inputRequest,
                inputRequest.OriginStep);
            return TryResolveRequest(
                in actionRequest,
                in movementIntent,
                currentWorldMoveDirection,
                facingForward,
                in tuning,
                out request);
        }

        public static bool TryResolveRequest(
            in CharacterActionRequest actionRequest,
            in MovementInputIntent movementIntent,
            Vector3 currentWorldMoveDirection,
            Vector3 facingForward,
            in DodgeActionTuning tuning,
            out DodgeActionRequest request)
        {
            return TryResolveRequest(
                in actionRequest,
                in movementIntent,
                currentWorldMoveDirection,
                facingForward,
                tuning.Priority,
                out request);
        }

        public static bool TryResolveRequest(
            in CharacterActionRequest actionRequest,
            in MovementInputIntent movementIntent,
            Vector3 currentWorldMoveDirection,
            Vector3 facingForward,
            int priority,
            out DodgeActionRequest request)
        {
            if (!actionRequest.HasRequest ||
                actionRequest.RequestType != ActionRequestType.Dodge ||
                actionRequest.SourceInputKind != InputRequestKind.Dodge)
            {
                request = default;
                return false;
            }

            if (!DodgeActionDirectionResolver.TryResolve(movementIntent, currentWorldMoveDirection, facingForward, out DodgeActionVariant variant, out Vector3 worldDirection))
            {
                request = default;
                return false;
            }

            request = new DodgeActionRequest(
                variant,
                worldDirection,
                actionRequest.OriginStep,
                actionRequest.ExpireStep,
                priority,
                actionRequest.SourceOrder,
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
