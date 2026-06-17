using System.Collections.Generic;
using ThirdPersonCharacterStateMachine;
using ThirdPersonDiagnostics;
using ThirdPersonMovement;

namespace ThirdPersonAction
{
    public static class FullBodyDiagnostics
    {
        static readonly FullBodyDiagnosticAdapter defaultAdapter =
            new FullBodyDiagnosticAdapter(RuntimeDiagnosticLogCharacterSink.Instance);

        public static FullBodyDiagnosticAdapter DefaultAdapter => defaultAdapter;

        public static void LogPipelineSnapshot(string activePath, int step, string diagnosticSummary)
        {
            defaultAdapter.LogPipelineSnapshot(activePath, step, diagnosticSummary);
        }

        public static void LogFullBodyPathChanged(
            in CharacterStateMachineSnapshot previousSnapshot,
            in CharacterStateMachineSnapshot snapshot,
            int step)
        {
            defaultAdapter.LogFullBodyPathChanged(in previousSnapshot, in snapshot, step);
        }

        public static void LogFullBodyPendingTransitionChanged(
            in CharacterStateMachineSnapshot previousSnapshot,
            in CharacterStateMachineSnapshot snapshot,
            int step)
        {
            defaultAdapter.LogFullBodyPendingTransitionChanged(in previousSnapshot, in snapshot, step);
        }

        public static void LogLocomotionPhaseChanged(
            string locomotionPath,
            string lastLoggedLocomotionPath,
            BasicMovementPhase lastLoggedLocomotionPhase,
            BasicMovementGait gait,
            in CharacterStateMachineSnapshot snapshot,
            int step)
        {
            defaultAdapter.LogLocomotionPhaseChanged(
                locomotionPath,
                lastLoggedLocomotionPath,
                lastLoggedLocomotionPhase,
                gait,
                in snapshot,
                step);
        }

        public static void LogActionAccepted(
            in CharacterStateMachineSnapshot previousSnapshot,
            in CharacterStateMachineSnapshot snapshot,
            in CharacterStateMachineFrame frame,
            int step)
        {
            defaultAdapter.LogActionAccepted(in previousSnapshot, in snapshot, in frame, step);
        }

        public static void LogFullBodyTickSnapshot(
            in CharacterStateMachineSnapshot snapshot,
            int step,
            string context)
        {
            defaultAdapter.LogFullBodyTickSnapshot(in snapshot, step, context);
        }

        public static void LogAnimationTickSnapshot(
            string activePath,
            int step,
            string context)
        {
            defaultAdapter.LogAnimationTickSnapshot(activePath, step, context);
        }

        public static void LogStateMachineDefinitionInvalid(string message)
        {
            defaultAdapter.LogStateMachineDefinitionInvalid(message);
        }

        public static void LogTimelineFactsTrace(StateTimelineFactsTrace trace)
        {
            defaultAdapter.LogTimelineFactsTrace(trace);
        }

        public static void LogTransitionConditionTraces(IReadOnlyList<CharacterStateTransitionConditionTrace> traces)
        {
            defaultAdapter.LogTransitionConditionTraces(traces);
        }

    }
}
