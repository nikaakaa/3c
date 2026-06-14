using ThirdPersonAnimation;
using ThirdPersonMovement;
using UnityEngine;

namespace ThirdPersonSimulation
{
    public readonly struct LocomotionRuntimeRollbackState
    {
        public LocomotionRuntimeRollbackState(
            MovementInputIntent currentIntent,
            Vector3 previousWorldDirection,
            AnimationPhasePlaybackProgress previousMotionPlaybackProgress,
            bool hasPreviousMotionPlaybackProgress,
            bool hasActiveMoveStopGait,
            BasicMovementGait activeMoveStopGait,
            LocomotionTurnBackIntent pendingTurnBackIntent = default)
        {
            CurrentIntent = currentIntent;
            PreviousWorldDirection = NormalizePlanarOrZero(previousWorldDirection);
            PreviousMotionPlaybackProgress = previousMotionPlaybackProgress;
            HasPreviousMotionPlaybackProgress = hasPreviousMotionPlaybackProgress;
            HasActiveMoveStopGait = hasActiveMoveStopGait;
            ActiveMoveStopGait = activeMoveStopGait;
            PendingTurnBackIntent = pendingTurnBackIntent;
        }

        public MovementInputIntent CurrentIntent { get; }
        public Vector3 PreviousWorldDirection { get; }
        public AnimationPhasePlaybackProgress PreviousMotionPlaybackProgress { get; }
        public bool HasPreviousMotionPlaybackProgress { get; }
        public bool HasActiveMoveStopGait { get; }
        public BasicMovementGait ActiveMoveStopGait { get; }
        public LocomotionTurnBackIntent PendingTurnBackIntent { get; }

        public static LocomotionRuntimeRollbackState Empty => new LocomotionRuntimeRollbackState(
            default,
            Vector3.zero,
            AnimationPhasePlaybackProgress.Invalid(BasicMovementPhase.Idle),
            false,
            false,
            BasicMovementGait.Walk);

        static Vector3 NormalizePlanarOrZero(Vector3 value)
        {
            value.y = 0f;
            float sqrMagnitude = value.sqrMagnitude;
            return sqrMagnitude > 0.000001f ? value / Mathf.Sqrt(sqrMagnitude) : Vector3.zero;
        }
    }
}
