using System;
using System.Collections.Generic;
using ThirdPersonCharacterStateMachine;
using ThirdPersonDiagnostics;
using ThirdPersonMovement;
using UnityEngine;

namespace ThirdPersonAction
{
    public sealed class FullBodyDiagnosticAdapter
    {
        readonly ICharacterDiagnosticSink sink;

        public FullBodyDiagnosticAdapter(ICharacterDiagnosticSink sink)
        {
            this.sink = sink ?? throw new ArgumentNullException(nameof(sink));
        }

        public void Submit(in CharacterFrameDiagnosticTrace trace)
        {
            if (!trace.HasEvent)
                return;

            RuntimeDiagnosticLogEvent diagnosticEvent = trace.ToEvent();
            sink.Submit(in diagnosticEvent);
        }

        public void LogPipelineSnapshot(string activePath, int step, string diagnosticSummary)
        {
            Submit(FullBodyDiagnosticEventFormatter.PipelineSnapshot(activePath, step, diagnosticSummary));
        }

        public void LogFullBodyPathChanged(
            in CharacterStateMachineSnapshot previousSnapshot,
            in CharacterStateMachineSnapshot snapshot,
            int step)
        {
            Submit(FullBodyDiagnosticEventFormatter.FullBodyPathChanged(in previousSnapshot, in snapshot, step));
        }

        public void LogFullBodyPendingTransitionChanged(
            in CharacterStateMachineSnapshot previousSnapshot,
            in CharacterStateMachineSnapshot snapshot,
            int step)
        {
            Submit(FullBodyDiagnosticEventFormatter.FullBodyPendingTransitionChanged(in previousSnapshot, in snapshot, step));
        }

        public void LogLocomotionPhaseChanged(
            string locomotionPath,
            string lastLoggedLocomotionPath,
            BasicMovementPhase lastLoggedLocomotionPhase,
            BasicMovementGait gait,
            in CharacterStateMachineSnapshot snapshot,
            int step)
        {
            Submit(FullBodyDiagnosticEventFormatter.LocomotionPhaseChanged(
                locomotionPath,
                lastLoggedLocomotionPath,
                lastLoggedLocomotionPhase,
                gait,
                in snapshot,
                step));
        }

        public void LogActionAccepted(
            in CharacterStateMachineSnapshot previousSnapshot,
            in CharacterStateMachineSnapshot snapshot,
            in CharacterStateMachineFrame frame,
            int step)
        {
            Submit(FullBodyDiagnosticEventFormatter.ActionAccepted(in previousSnapshot, in snapshot, in frame, step));
        }

        public void LogFullBodyTickSnapshot(
            in CharacterStateMachineSnapshot snapshot,
            int step,
            string context)
        {
            Submit(FullBodyDiagnosticEventFormatter.FullBodyTickSnapshot(in snapshot, step, context));
        }

        public void LogAnimationTickSnapshot(
            string activePath,
            int step,
            string context)
        {
            Submit(FullBodyDiagnosticEventFormatter.AnimationTickSnapshot(activePath, step, context));
        }

        public void LogStateMachineDefinitionInvalid(string message)
        {
            Submit(FullBodyDiagnosticEventFormatter.StateMachineDefinitionInvalid(message));
        }

        public void LogTimelineFactsTrace(StateTimelineFactsTrace trace)
        {
            if (trace.Source == StateTimelineFactsSource.None)
                return;

            Submit(FullBodyDiagnosticEventFormatter.TimelineFactsTrace(trace));
        }

        public void LogTransitionConditionTraces(IReadOnlyList<CharacterStateTransitionConditionTrace> traces)
        {
            if (traces == null)
                return;

            for (int i = 0; i < traces.Count; i++)
            {
                CharacterStateTransitionConditionTrace trace = traces[i];
                if (!trace.EmitDiagnostic)
                    continue;

                Submit(FullBodyDiagnosticEventFormatter.TransitionConditionTrace(trace));
            }
        }

    }

    public static class FullBodyDiagnosticEventFormatter
    {
        public static CharacterFrameDiagnosticTrace PipelineSnapshot(string activePath, int step, string diagnosticSummary)
        {
            return new CharacterFrameDiagnosticTrace(
                RuntimeDiagnosticLogCategory.FullBody,
                RuntimeDiagnosticLogLevel.Trace,
                "fullbody-frame-pipeline",
                activePath ?? string.Empty,
                string.Empty,
                step,
                Time.frameCount,
                diagnosticSummary ?? string.Empty);
        }

        public static CharacterFrameDiagnosticTrace FullBodyPathChanged(
            in CharacterStateMachineSnapshot previousSnapshot,
            in CharacterStateMachineSnapshot snapshot,
            int step)
        {
            FullBodyStateView stateView = FullBodyStateView.FromSnapshot(in snapshot);
            return new CharacterFrameDiagnosticTrace(
                RuntimeDiagnosticLogCategory.FullBody,
                RuntimeDiagnosticLogLevel.Info,
                "fullbody-path-changed",
                snapshot.ActivePath,
                previousSnapshot.ActivePath,
                step,
                Time.frameCount,
                $"owner={stateView.Owner.Kind} action={stateView.ActionState.Value} stateTime={snapshot.StateTime:F3} variant={snapshot.Variant}");
        }

        public static CharacterFrameDiagnosticTrace FullBodyPendingTransitionChanged(
            in CharacterStateMachineSnapshot previousSnapshot,
            in CharacterStateMachineSnapshot snapshot,
            int step)
        {
            FullBodyStateView stateView = FullBodyStateView.FromSnapshot(in snapshot);
            return new CharacterFrameDiagnosticTrace(
                RuntimeDiagnosticLogCategory.FullBody,
                RuntimeDiagnosticLogLevel.Trace,
                "fullbody-pending-transition-changed",
                snapshot.PendingTransitionPath,
                previousSnapshot.PendingTransitionPath,
                step,
                Time.frameCount,
                $"owner={stateView.Owner.Kind} action={stateView.ActionState.Value}");
        }

        public static CharacterFrameDiagnosticTrace LocomotionPhaseChanged(
            string locomotionPath,
            string lastLoggedLocomotionPath,
            BasicMovementPhase lastLoggedLocomotionPhase,
            BasicMovementGait gait,
            in CharacterStateMachineSnapshot snapshot,
            int step)
        {
            FullBodyStateView stateView = FullBodyStateView.FromSnapshot(in snapshot);
            return new CharacterFrameDiagnosticTrace(
                RuntimeDiagnosticLogCategory.Locomotion,
                RuntimeDiagnosticLogLevel.Info,
                "locomotion-phase-changed",
                locomotionPath,
                lastLoggedLocomotionPath,
                step,
                Time.frameCount,
                $"fromPhase={lastLoggedLocomotionPhase} toPhase={stateView.LocomotionPhase} gait={gait} phaseTime={snapshot.StateTime:F3}");
        }

        public static CharacterFrameDiagnosticTrace ActionAccepted(
            in CharacterStateMachineSnapshot previousSnapshot,
            in CharacterStateMachineSnapshot snapshot,
            in CharacterStateMachineFrame frame,
            int step)
        {
            FullBodyStateView stateView = FullBodyStateView.FromSnapshot(in snapshot);
            return new CharacterFrameDiagnosticTrace(
                RuntimeDiagnosticLogCategory.Action,
                RuntimeDiagnosticLogLevel.Info,
                "action-accepted",
                snapshot.ActivePath,
                previousSnapshot.ActivePath,
                step,
                Time.frameCount,
                $"owner={stateView.Owner.Kind} action={stateView.ActionState.Value} variant={snapshot.Variant} animation={(frame.HasAnimationRequest ? frame.AnimationRequest.Key.Value : string.Empty)}");
        }

        public static CharacterFrameDiagnosticTrace FullBodyTickSnapshot(
            in CharacterStateMachineSnapshot snapshot,
            int step,
            string context)
        {
            return new CharacterFrameDiagnosticTrace(
                RuntimeDiagnosticLogCategory.FullBody,
                RuntimeDiagnosticLogLevel.Trace,
                "fullbody-tick-snapshot",
                snapshot.ActivePath,
                string.Empty,
                step,
                Time.frameCount,
                context);
        }

        public static CharacterFrameDiagnosticTrace AnimationTickSnapshot(
            string activePath,
            int step,
            string context)
        {
            return new CharacterFrameDiagnosticTrace(
                RuntimeDiagnosticLogCategory.Animation,
                RuntimeDiagnosticLogLevel.Trace,
                "animation-tick-snapshot",
                activePath,
                string.Empty,
                step,
                Time.frameCount,
                context);
        }

        public static CharacterFrameDiagnosticTrace StateMachineDefinitionInvalid(string message)
        {
            return new CharacterFrameDiagnosticTrace(
                RuntimeDiagnosticLogCategory.FullBody,
                RuntimeDiagnosticLogLevel.Error,
                "state-machine-definition-invalid",
                string.Empty,
                string.Empty,
                0,
                Time.frameCount,
                "Character state machine definition is invalid:\n" + message);
        }

        public static CharacterFrameDiagnosticTrace TimelineFactsTrace(StateTimelineFactsTrace trace)
        {
            StateTimelineWindowFacts facts = trace.Facts;
            string source = ResolveTimelineFactsSource(trace.Source);
            return new CharacterFrameDiagnosticTrace(
                RuntimeDiagnosticLogCategory.FullBody,
                RuntimeDiagnosticLogLevel.Trace,
                "state-timeline-window-facts",
                facts.StateId.Value,
                source,
                trace.SourceStep,
                Time.frameCount,
                $"{source} state={facts.StateId.Value} sourceStep={trace.SourceStep} normalized={facts.NormalizedTime:F3} normalizedValid={facts.HasValidNormalizedTime} elapsed={facts.ElapsedSeconds:F3} motion={facts.MotionWindowActive} inputLock={facts.InputLockWindowActive} interrupt={facts.InterruptWindowActive} exit={facts.ExitWindowActive} priority={facts.Priority} resistance={facts.Resistance} minPriority={facts.MinPriority} force={facts.Force} activeWindows={facts.ActiveWindowIds} requestWindows={facts.RequestWindowIds} activeFacts={facts.ActiveFactIds} requestFacts={facts.RequestFactIds} request={trace.RequestType}");
        }

        public static CharacterFrameDiagnosticTrace TransitionConditionTrace(CharacterStateTransitionConditionTrace trace)
        {
            return new CharacterFrameDiagnosticTrace(
                ResolveConditionCategory(trace.ConditionKind),
                RuntimeDiagnosticLogLevel.Trace,
                trace.Message,
                trace.SourceStatePath,
                trace.TargetStatePath,
                trace.SourceStep,
                Time.frameCount,
                $"condition={trace.ConditionKey} passed={trace.Passed} reason={trace.Reason} {trace.Context}");
        }

        static RuntimeDiagnosticLogCategory ResolveConditionCategory(CharacterStateTransitionConditionKind conditionKind)
        {
            return conditionKind == CharacterStateTransitionConditionKind.MoveTurnBackRequested ||
                   conditionKind == CharacterStateTransitionConditionKind.LocomotionPreemptionPending
                ? RuntimeDiagnosticLogCategory.Locomotion
                : RuntimeDiagnosticLogCategory.FullBody;
        }

        static string ResolveTimelineFactsSource(StateTimelineFactsSource source)
        {
            switch (source)
            {
                case StateTimelineFactsSource.Current:
                    return "source=current";
                case StateTimelineFactsSource.Projected:
                    return "source=projected";
                case StateTimelineFactsSource.Target:
                    return "source=target";
                default:
                    return "source=none";
            }
        }
    }
}
