using ThirdPersonAnimation;
using ThirdPersonCharacterStateMachine;
using ThirdPersonDiagnostics;
using ThirdPersonMovement;
using UnityEngine;

namespace ThirdPersonAction
{
    public static class FullBodyDiagnostics
    {
        public static void LogPipelineSnapshot(string activePath, int step, string diagnosticSummary)
        {
            RuntimeDiagnosticLog.Submit(new RuntimeDiagnosticLogEvent(
                RuntimeDiagnosticLogCategory.FullBody,
                RuntimeDiagnosticLogLevel.Trace,
                "fullbody-frame-pipeline",
                activePath ?? string.Empty,
                string.Empty,
                step,
                Time.frameCount,
                diagnosticSummary ?? string.Empty));
        }

        public static void LogFullBodyPathChanged(
            in CharacterStateMachineSnapshot previousSnapshot,
            in CharacterStateMachineSnapshot snapshot,
            int step)
        {
            RuntimeDiagnosticLog.Submit(new RuntimeDiagnosticLogEvent(
                RuntimeDiagnosticLogCategory.FullBody,
                RuntimeDiagnosticLogLevel.Info,
                "fullbody-path-changed",
                snapshot.ActivePath,
                previousSnapshot.ActivePath,
                step,
                Time.frameCount,
                $"owner={snapshot.Owner.Kind} action={snapshot.ActionState.Value} stateTime={snapshot.StateTime:F3} variant={snapshot.Variant}"));
        }

        public static void LogFullBodyPendingTransitionChanged(
            in CharacterStateMachineSnapshot previousSnapshot,
            in CharacterStateMachineSnapshot snapshot,
            int step)
        {
            RuntimeDiagnosticLog.Submit(new RuntimeDiagnosticLogEvent(
                RuntimeDiagnosticLogCategory.FullBody,
                RuntimeDiagnosticLogLevel.Trace,
                "fullbody-pending-transition-changed",
                snapshot.PendingTransitionPath,
                previousSnapshot.PendingTransitionPath,
                step,
                Time.frameCount,
                $"owner={snapshot.Owner.Kind} action={snapshot.ActionState.Value}"));
        }

        public static void LogLocomotionPhaseChanged(
            string locomotionPath,
            string lastLoggedLocomotionPath,
            BasicMovementPhase lastLoggedLocomotionPhase,
            BasicMovementGait gait,
            in CharacterStateMachineSnapshot snapshot,
            int step)
        {
            RuntimeDiagnosticLog.Submit(new RuntimeDiagnosticLogEvent(
                RuntimeDiagnosticLogCategory.Locomotion,
                RuntimeDiagnosticLogLevel.Info,
                "locomotion-phase-changed",
                locomotionPath,
                lastLoggedLocomotionPath,
                step,
                Time.frameCount,
                $"fromPhase={lastLoggedLocomotionPhase} toPhase={snapshot.LocomotionPhase} gait={gait} phaseTime={snapshot.StateTime:F3}"));
        }

        public static void LogActionAccepted(
            in CharacterStateMachineSnapshot previousSnapshot,
            in CharacterStateMachineSnapshot snapshot,
            in CharacterStateMachineFrame frame,
            int step)
        {
            RuntimeDiagnosticLog.Submit(new RuntimeDiagnosticLogEvent(
                RuntimeDiagnosticLogCategory.Action,
                RuntimeDiagnosticLogLevel.Info,
                "action-accepted",
                snapshot.ActivePath,
                previousSnapshot.ActivePath,
                step,
                Time.frameCount,
                $"owner={snapshot.Owner.Kind} action={snapshot.ActionState.Value} variant={snapshot.Variant} animation={(frame.HasAnimationRequest ? frame.AnimationRequest.Key.Value : string.Empty)}"));
        }

        public static void LogFullBodyTickSnapshot(
            in CharacterStateMachineSnapshot snapshot,
            int step,
            string context)
        {
            RuntimeDiagnosticLog.Submit(new RuntimeDiagnosticLogEvent(
                RuntimeDiagnosticLogCategory.FullBody,
                RuntimeDiagnosticLogLevel.Trace,
                "fullbody-tick-snapshot",
                snapshot.ActivePath,
                string.Empty,
                step,
                Time.frameCount,
                context));
        }

        public static void LogAnimationTickSnapshot(
            string activePath,
            int step,
            string context)
        {
            RuntimeDiagnosticLog.Submit(new RuntimeDiagnosticLogEvent(
                RuntimeDiagnosticLogCategory.Animation,
                RuntimeDiagnosticLogLevel.Trace,
                "animation-tick-snapshot",
                activePath,
                string.Empty,
                step,
                Time.frameCount,
                context));
        }

        public static void LogStateMachineDefinitionInvalid(string message)
        {
            RuntimeDiagnosticLog.Submit(new RuntimeDiagnosticLogEvent(
                RuntimeDiagnosticLogCategory.FullBody,
                RuntimeDiagnosticLogLevel.Error,
                "state-machine-definition-invalid",
                string.Empty,
                string.Empty,
                0,
                Time.frameCount,
                "Character state machine definition is invalid:\n" + message));
        }
    }
}
