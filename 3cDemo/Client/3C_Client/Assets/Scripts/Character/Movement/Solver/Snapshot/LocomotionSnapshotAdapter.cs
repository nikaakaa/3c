using ThirdPersonAnimation;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonMovement
{
    public static class LocomotionSnapshotAdapter
    {
        public static LocomotionRuntimeRollbackState CaptureRuntimeState(
            in MovementInputIntent currentIntent,
            Vector3 previousWorldDirection,
            in AnimationPhasePlaybackProgress previousMotionPlaybackProgress,
            bool hasPreviousMotionPlaybackProgress,
            bool hasActiveMoveStopGait,
            BasicMovementGait activeMoveStopGait,
            in LocomotionTurnBackIntent pendingTurnBackIntent)
        {
            return new LocomotionRuntimeRollbackState(
                currentIntent,
                previousWorldDirection,
                previousMotionPlaybackProgress,
                hasPreviousMotionPlaybackProgress,
                hasActiveMoveStopGait,
                activeMoveStopGait,
                pendingTurnBackIntent);
        }

        public static void ReadRuntimeState(
            in LocomotionRuntimeRollbackState state,
            out MovementInputIntent currentIntent,
            out Vector3 previousWorldDirection,
            out AnimationPhasePlaybackProgress previousMotionPlaybackProgress,
            out bool hasPreviousMotionPlaybackProgress,
            out bool hasActiveMoveStopGait,
            out BasicMovementGait activeMoveStopGait,
            out LocomotionTurnBackIntent pendingTurnBackIntent)
        {
            currentIntent = state.CurrentIntent;
            previousWorldDirection = state.PreviousWorldDirection;
            previousMotionPlaybackProgress = state.PreviousMotionPlaybackProgress;
            hasPreviousMotionPlaybackProgress = state.HasPreviousMotionPlaybackProgress;
            hasActiveMoveStopGait = state.HasActiveMoveStopGait;
            activeMoveStopGait = state.ActiveMoveStopGait;
            pendingTurnBackIntent = state.PendingTurnBackIntent;
        }
    }
}
