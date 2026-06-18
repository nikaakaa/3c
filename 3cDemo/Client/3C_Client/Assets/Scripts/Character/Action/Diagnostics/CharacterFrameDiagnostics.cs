using System.Collections.Generic;
using ThirdPersonCharacterStateMachine;
using ThirdPersonDiagnostics;
using ThirdPersonMovement;

namespace ThirdPersonAction
{
    public static class CharacterFrameDiagnostics
    {
        static readonly CharacterFrameDiagnosticAdapter defaultAdapter =
            new CharacterFrameDiagnosticAdapter(RuntimeDiagnosticLogCharacterSink.Instance);

        public static CharacterFrameDiagnosticAdapter DefaultAdapter => defaultAdapter;

        public static void LogPipelineSnapshot(string activePath, int step, string diagnosticSummary)
        {
            defaultAdapter.LogPipelineSnapshot(activePath, step, diagnosticSummary);
        }

        public static void LogStatePathChanged(
            in CharacterStateMachineSnapshot previousSnapshot,
            in CharacterStateMachineSnapshot snapshot,
            int step)
        {
            defaultAdapter.LogStatePathChanged(in previousSnapshot, in snapshot, step);
        }

        public static void LogPendingTransitionChanged(
            in CharacterStateMachineSnapshot previousSnapshot,
            in CharacterStateMachineSnapshot snapshot,
            int step)
        {
            defaultAdapter.LogPendingTransitionChanged(in previousSnapshot, in snapshot, step);
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

        public static void LogStateTickSnapshot(
            in CharacterStateMachineSnapshot snapshot,
            int step,
            string context)
        {
            defaultAdapter.LogStateTickSnapshot(in snapshot, step, context);
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
