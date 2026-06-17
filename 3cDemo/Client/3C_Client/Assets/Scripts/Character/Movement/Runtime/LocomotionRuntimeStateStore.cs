using ThirdPersonAnimation;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonMovement
{
    internal sealed class LocomotionRuntimeStateStore
    {
        AnimationPhasePlaybackProgress previousMotionPlaybackProgress;
        bool hasPreviousMotionPlaybackProgress;

        public MovementInputIntent CurrentIntent { get; private set; }
        public BasicMovementGait LastMovingGait { get; private set; } = BasicMovementGait.Walk;
        public Vector3 CurrentWorldDirection { get; private set; }
        public BasicLocomotionFrame CurrentFrame { get; private set; }
        public float CurrentPhaseTime { get; private set; }
        public bool HasActiveMoveStopGait { get; private set; }
        public BasicMovementGait ActiveMoveStopGait { get; private set; } = BasicMovementGait.Walk;
        public bool RunLatchActive { get; private set; }
        public Vector3 PreviousWorldDirection { get; private set; }
        public LocomotionTurnBackIntent PendingTurnBackIntent { get; private set; }
        public string ActiveStatePath { get; private set; } = string.Empty;
        public bool HasPreviousMotionPlaybackProgress => hasPreviousMotionPlaybackProgress;
        public BasicMovementPhase CurrentPhase => CurrentFrame.Phase;
        public BasicMovementGait CurrentGait => CurrentFrame.Command.Gait;

        public LocomotionFrameRuntimeState CaptureFrameState()
        {
            return new LocomotionFrameRuntimeState(
                CurrentIntent,
                LastMovingGait,
                HasActiveMoveStopGait,
                ActiveMoveStopGait,
                RunLatchActive,
                PreviousWorldDirection,
                PendingTurnBackIntent);
        }

        public void ApplyFrameState(in LocomotionFrameRuntimeState state)
        {
            CurrentIntent = state.CurrentIntent;
            LastMovingGait = state.LastMovingGait;
            HasActiveMoveStopGait = state.HasActiveMoveStopGait;
            ActiveMoveStopGait = state.ActiveMoveStopGait;
            RunLatchActive = state.RunLatchActive;
            PreviousWorldDirection = state.PreviousWorldDirection;
            PendingTurnBackIntent = state.PendingTurnBackIntent;
        }

        public void ApplyFrameResult(in LocomotionFrameBuilderResult result)
        {
            LocomotionFrameRuntimeState runtimeState = result.RuntimeState;
            ApplyFrameState(in runtimeState);
            if (!result.HasFrame)
                return;

            CurrentFrame = result.Frame;
            CurrentPhaseTime = result.CurrentPhaseTime;
            ActiveStatePath = result.ActiveStatePath;
            CurrentWorldDirection = result.CurrentWorldDirection;
        }

        public void ResetAfterLifecycleDisable()
        {
            LastMovingGait = BasicMovementGait.Walk;
            RunLatchActive = false;
            HasActiveMoveStopGait = false;
            ResetMotionPlaybackWindow(CurrentPhase);
        }

        public void ResetForStateMachineDefinition()
        {
            LastMovingGait = BasicMovementGait.Walk;
            RunLatchActive = false;
            HasActiveMoveStopGait = false;
            ResetMotionPlaybackWindow(CurrentPhase);
        }

        public void SetRunLatchActive(bool active)
        {
            RunLatchActive = active;
            if (!active && !CurrentIntent.HasMoveIntent)
                LastMovingGait = BasicMovementGait.Walk;
        }

        public void ClearRunLatchAfterIdle()
        {
            RunLatchActive = false;
            LastMovingGait = BasicMovementGait.Walk;
        }

        public void ClearTurnBackPreemptionResidue()
        {
            PendingTurnBackIntent = LocomotionTurnBackIntent.None;
            ResetMotionPlaybackWindow(BasicMovementPhase.TurnBack);
        }

        public LocomotionRuntimeRollbackState CaptureRollbackState()
        {
            MovementInputIntent currentIntent = CurrentIntent;
            LocomotionTurnBackIntent pendingTurnBackIntent = PendingTurnBackIntent;
            return LocomotionSnapshotAdapter.CaptureRuntimeState(
                in currentIntent,
                PreviousWorldDirection,
                in previousMotionPlaybackProgress,
                hasPreviousMotionPlaybackProgress,
                HasActiveMoveStopGait,
                ActiveMoveStopGait,
                in pendingTurnBackIntent);
        }

        public void RestoreSnapshotHeader(
            bool runLatchActive,
            BasicMovementGait lastMovingGait,
            Vector3 currentWorldDirection,
            string activeStatePath)
        {
            RunLatchActive = runLatchActive;
            LastMovingGait = lastMovingGait;
            CurrentWorldDirection = currentWorldDirection;
            ActiveStatePath = activeStatePath ?? string.Empty;
        }

        public void RestoreRollbackState(in LocomotionRuntimeRollbackState state)
        {
            LocomotionSnapshotAdapter.ReadRuntimeState(
                in state,
                out MovementInputIntent currentIntent,
                out Vector3 previousWorldDirection,
                out AnimationPhasePlaybackProgress previousProgress,
                out bool hasPreviousProgress,
                out bool hasActiveMoveStopGait,
                out BasicMovementGait activeMoveStopGait,
                out LocomotionTurnBackIntent pendingTurnBackIntent);

            CurrentIntent = currentIntent;
            PreviousWorldDirection = previousWorldDirection;
            previousMotionPlaybackProgress = previousProgress;
            hasPreviousMotionPlaybackProgress = hasPreviousProgress;
            HasActiveMoveStopGait = hasActiveMoveStopGait;
            ActiveMoveStopGait = activeMoveStopGait;
            PendingTurnBackIntent = pendingTurnBackIntent;
        }

        public void RestoreFrame(in BasicLocomotionFrame frame, float phaseTime)
        {
            CurrentFrame = frame;
            CurrentPhaseTime = phaseTime;
        }

        public AnimationMotionPlaybackWindow BuildMotionPlaybackWindow(
            BasicMovementPhase phase,
            BasicMovementGait gait,
            string aliasKey,
            in AnimationPhasePlaybackProgress progress,
            bool requireProgressPhase = false,
            bool sampleFromZeroOnNewPlayback = false)
        {
            if (!progress.HasValidPlayback || (requireProgressPhase && progress.Phase != phase))
            {
                ResetMotionPlaybackWindow(phase);
                return AnimationMotionPlaybackWindow.Invalid(phase, gait);
            }

            bool samePlayback =
                hasPreviousMotionPlaybackProgress &&
                previousMotionPlaybackProgress.HasValidPlayback &&
                previousMotionPlaybackProgress.AliasKey == aliasKey &&
                progress.NormalizedTime >= previousMotionPlaybackProgress.NormalizedTime;

            float previousTime = samePlayback
                ? previousMotionPlaybackProgress.NormalizedTime
                : sampleFromZeroOnNewPlayback ? 0f : progress.NormalizedTime;
            previousMotionPlaybackProgress = progress;
            hasPreviousMotionPlaybackProgress = true;
            return new AnimationMotionPlaybackWindow(phase, gait, aliasKey, previousTime, progress.NormalizedTime, true);
        }

        public void ResetMotionPlaybackWindow(BasicMovementPhase phase)
        {
            previousMotionPlaybackProgress = AnimationPhasePlaybackProgress.Invalid(phase);
            hasPreviousMotionPlaybackProgress = false;
        }

        public void SeedMotionPlaybackWindow(in AnimationPhasePlaybackProgress progress, BasicMovementPhase currentPhase)
        {
            if (!progress.HasValidPlayback)
            {
                ResetMotionPlaybackWindow(currentPhase);
                return;
            }

            previousMotionPlaybackProgress = progress;
            hasPreviousMotionPlaybackProgress = true;
        }
    }
}
