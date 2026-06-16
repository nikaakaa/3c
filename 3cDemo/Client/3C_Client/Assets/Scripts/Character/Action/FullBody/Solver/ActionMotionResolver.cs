using ThirdPersonMovement;
using ThirdPersonCharacterStateMachine;
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

            ActionMovementCommand command = new ActionMovementCommand(
                spec.LockedWorldDirection,
                frameDistance,
                input.DeltaTime,
                rotateToDirection);
            bool completed = duration <= 0f || spec.StateTime >= duration;
            bool setRunLatch = completed && spec.SetRunLatchOnComplete;
            string diagnosticSummary =
                $"actionMotion={spec.ActionState.Value} variant={spec.Variant} distance={command.PlanarDistance:F3} completed={completed} runLatch={setRunLatch} sourceStep={spec.SourceStep}";

            return new ActionMotionResolveResult(
                spec,
                command,
                command.HasMovement,
                completed,
                setRunLatch,
                spec.SourceStep,
                diagnosticSummary);
        }
    }
}
