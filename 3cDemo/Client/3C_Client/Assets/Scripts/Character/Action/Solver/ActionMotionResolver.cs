using ThirdPersonMovement;
using ThirdPersonCharacterStateMachine;
using ThirdPersonMotionWarping;
using UnityEngine;

namespace ThirdPersonAction
{
    public static class ActionMotionResolver
    {
        public static ActionMotionResolveResult Resolve(in ActionMotionResolveInput input)
        {
            ActionMotionSpec spec = input.Spec;
            if (!spec.HasSpec)
                return ActionMotionResolveResult.None(spec.SourceStep);

            float duration = spec.Duration;
            float distance = spec.Distance;
            bool rotateToDirection = spec.RotateToDirection;

            float frameDistance = 0f;
            if (duration > 0f)
            {
                float previousStateTime = Mathf.Max(0f, spec.StateTime - input.DeltaTime);
                float remainingDuration = Mathf.Max(0f, duration - previousStateTime);
                frameDistance = distance * Mathf.Min(input.DeltaTime, remainingDuration) / duration;
            }

            MotionWarpResult motionWarpResult = MotionWarpResult.None(
                spec.MotionWarpPayload.Policy.PolicyId,
                spec.MotionWarpPayload.TargetBindingId,
                spec.SourceStep);
            Vector3 commandDirection = spec.LockedWorldDirection;
            float commandDistance = spec.MotionWarpPayload.HasWarp ? 0f : frameDistance;
            float commandYaw = 0f;
            bool hasCommandYaw = false;
            if (spec.MotionWarpPayload.HasWarp)
            {
                motionWarpResult = MotionWarpSolver.Resolve(new MotionWarpInput(
                    spec.MotionWarpPayload.Policy,
                    input.WarpRootSnapshot,
                    input.WarpTargetSnapshot,
                    IsMotionWindowActive(in input),
                    spec.SourceStep));
                if (!motionWarpResult.IsValid)
                {
                    return BuildWarpInvalidResult(
                        in spec,
                        in motionWarpResult,
                        duration,
                        input.HasMoveIntentAtCompletion,
                        spec.SourceStep);
                }

                if (motionWarpResult.PlanarDelta.sqrMagnitude > 0.000001f)
                {
                    commandDirection = motionWarpResult.PlanarDelta.normalized;
                    commandDistance = motionWarpResult.PlanarDelta.magnitude;
                }

                commandYaw = motionWarpResult.YawDelta;
                hasCommandYaw = Mathf.Abs(commandYaw) > 0.0001f;
            }

            ActionMovementCommand command = new ActionMovementCommand(
                commandDirection,
                commandDistance,
                input.DeltaTime,
                rotateToDirection,
                commandYaw,
                hasCommandYaw);
            bool completed = duration <= 0f || spec.StateTime >= duration;
            bool setRunLatch = completed && spec.SetRunLatchOnComplete && input.HasMoveIntentAtCompletion;
            string diagnosticSummary =
                $"actionMotion={spec.ActionState.Value} variant={spec.Variant} distance={command.PlanarDistance:F3} warp={spec.MotionWarpPayload.HasWarp} warpValid={motionWarpResult.IsValid} warpYaw={command.YawDelta:F3} completed={completed} hasMove={input.HasMoveIntentAtCompletion} runLatch={setRunLatch} sourceStep={spec.SourceStep}";

            return new ActionMotionResolveResult(
                spec,
                command,
                command.HasMotion,
                completed,
                setRunLatch,
                spec.SourceStep,
                diagnosticSummary,
                motionWarpResult);
        }

        static bool IsMotionWindowActive(in ActionMotionResolveInput input)
        {
            return !input.TimelineFacts.StateId.IsValid || input.TimelineFacts.MotionWindowActive;
        }

        static ActionMotionResolveResult BuildWarpInvalidResult(
            in ActionMotionSpec spec,
            in MotionWarpResult motionWarpResult,
            float duration,
            bool hasMoveIntentAtCompletion,
            int sourceStep)
        {
            bool completed = duration <= 0f || spec.StateTime >= duration;
            bool setRunLatch = completed && spec.SetRunLatchOnComplete && hasMoveIntentAtCompletion;
            return new ActionMotionResolveResult(
                spec,
                default,
                false,
                completed,
                setRunLatch,
                sourceStep,
                $"actionMotion={spec.ActionState.Value} variant={spec.Variant} warpInvalid={motionWarpResult.FailureReason} completed={completed} sourceStep={sourceStep}",
                motionWarpResult);
        }
    }
}
