using UnityEngine;

namespace ThirdPersonMovement
{
    public readonly struct LocomotionFrameRuntimeState
    {
        public LocomotionFrameRuntimeState(
            MovementInputIntent currentIntent,
            BasicMovementGait lastMovingGait,
            bool hasActiveMoveStopGait,
            BasicMovementGait activeMoveStopGait,
            bool runLatchActive,
            Vector3 previousWorldDirection,
            LocomotionTurnBackIntent pendingTurnBackIntent)
        {
            CurrentIntent = currentIntent;
            LastMovingGait = lastMovingGait;
            HasActiveMoveStopGait = hasActiveMoveStopGait;
            ActiveMoveStopGait = activeMoveStopGait;
            RunLatchActive = runLatchActive;
            PreviousWorldDirection = previousWorldDirection;
            PendingTurnBackIntent = pendingTurnBackIntent;
        }

        public MovementInputIntent CurrentIntent { get; }
        public BasicMovementGait LastMovingGait { get; }
        public bool HasActiveMoveStopGait { get; }
        public BasicMovementGait ActiveMoveStopGait { get; }
        public bool RunLatchActive { get; }
        public Vector3 PreviousWorldDirection { get; }
        public LocomotionTurnBackIntent PendingTurnBackIntent { get; }

        public LocomotionFrameRuntimeState WithCurrentIntent(in MovementInputIntent intent)
        {
            return new LocomotionFrameRuntimeState(
                intent,
                LastMovingGait,
                HasActiveMoveStopGait,
                ActiveMoveStopGait,
                RunLatchActive,
                PreviousWorldDirection,
                PendingTurnBackIntent);
        }

        public LocomotionFrameRuntimeState WithLastMovingGait(BasicMovementGait gait)
        {
            return new LocomotionFrameRuntimeState(
                CurrentIntent,
                gait,
                HasActiveMoveStopGait,
                ActiveMoveStopGait,
                RunLatchActive,
                PreviousWorldDirection,
                PendingTurnBackIntent);
        }

        public LocomotionFrameRuntimeState WithMoveStopGait(bool hasMoveStopGait, BasicMovementGait gait)
        {
            return new LocomotionFrameRuntimeState(
                CurrentIntent,
                LastMovingGait,
                hasMoveStopGait,
                gait,
                RunLatchActive,
                PreviousWorldDirection,
                PendingTurnBackIntent);
        }

        public LocomotionFrameRuntimeState WithRunLatch(bool active, bool resetLastMovingGait)
        {
            return new LocomotionFrameRuntimeState(
                CurrentIntent,
                resetLastMovingGait ? BasicMovementGait.Walk : LastMovingGait,
                HasActiveMoveStopGait,
                ActiveMoveStopGait,
                active,
                PreviousWorldDirection,
                PendingTurnBackIntent);
        }

        public LocomotionFrameRuntimeState WithPreviousWorldDirection(Vector3 direction)
        {
            return new LocomotionFrameRuntimeState(
                CurrentIntent,
                LastMovingGait,
                HasActiveMoveStopGait,
                ActiveMoveStopGait,
                RunLatchActive,
                direction,
                PendingTurnBackIntent);
        }

        public LocomotionFrameRuntimeState WithPendingTurnBackIntent(in LocomotionTurnBackIntent intent)
        {
            return new LocomotionFrameRuntimeState(
                CurrentIntent,
                LastMovingGait,
                HasActiveMoveStopGait,
                ActiveMoveStopGait,
                RunLatchActive,
                PreviousWorldDirection,
                intent);
        }
    }
}
