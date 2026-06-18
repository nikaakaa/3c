using System;
using ThirdPersonAnimation;
using ThirdPersonCharacterStateMachine;
using ThirdPersonInput;
using ThirdPersonMovement;

namespace ThirdPersonAction
{
    internal interface ICharacterFrameOutputCache
    {
        BasicLocomotionFrame LastLocomotionFrame { get; set; }
        CharacterStateMachineFrame LastStateFrame { get; set; }
        ActionMotionResolveResult LastActionMotionResult { get; set; }
    }

    internal interface ICharacterFrameInputRequestConsumerDependencies
    {
        InputRequestBuffer InputRequestBuffer { get; }
    }

    internal interface ICharacterFrameMotionOutputDependencies
    {
        IActionMovementExecutor ActionMovementExecutor { get; }
        ILocomotionOutputRuntimePort LocomotionOutputRuntime { get; }
    }

    internal interface ICharacterAnimationOutputDependencies
    {
        ICharacterAnimationOutputPresenter AnimationPresenter { get; }
        ILocomotionOutputRuntimePort LocomotionOutputRuntime { get; }
    }

    internal interface ICharacterFrameRuntimeFactsDependencies
    {
        ILocomotionOutputRuntimePort LocomotionOutputRuntime { get; }
        ICharacterAnimationOutputPresenter AnimationPresenter { get; }
        AnimationPhasePlaybackProgress LocomotionAnimationPlaybackProgress { get; }
        string LocomotionAnimationName { get; }
    }

    internal interface ICharacterFrameSnapshotOutputState : ICharacterFrameOutputCache
    {
        CharacterStateMachineSnapshot CurrentStateSnapshot { get; set; }
        string DebugStatePath { get; set; }
        string DebugPendingTransitionPath { get; set; }
        string LastLoggedStatePath { get; set; }
        string LastLoggedPendingTransitionPath { get; set; }
        string LastLoggedLocomotionPath { get; set; }
        BasicMovementPhase LastLoggedLocomotionPhase { get; set; }
        bool LoggedInitialLocomotionState { get; set; }
    }

    internal interface ICharacterFrameDiagnosticDependencies : ICharacterFrameRuntimeFactsDependencies
    {
        ActionAnimationKey ActionAnimationKey { get; }
        float ActionAnimationNormalizedTime { get; }
        bool ActionAnimationHasValidPlayback { get; }
        bool ActionAnimationPlaybackEnded { get; }
        string ActionAnimationName { get; }
        void LogLocomotionDiagnosticTickSnapshot(int step);
    }

    internal sealed class CharacterFrameOutputRuntime
    {
        readonly CharacterFrameOutputCacheWriter cacheWriter;
        readonly CharacterFrameInputRequestConsumer inputRequestConsumer;
        readonly CharacterFrameMotionOutputApplier motionOutputApplier;
        readonly CharacterAnimationOutputPresenter animationOutputPresenter;
        readonly CharacterFrameRuntimeFactsWriter runtimeFactsWriter;
        readonly CharacterFrameSnapshotWriter snapshotWriter;
        readonly CharacterFrameDiagnosticSubmitter diagnosticSubmitter;

        public CharacterFrameOutputRuntime(
            CharacterFrameOutputCacheWriter cacheWriter,
            CharacterFrameInputRequestConsumer inputRequestConsumer,
            CharacterFrameMotionOutputApplier motionOutputApplier,
            CharacterAnimationOutputPresenter animationOutputPresenter,
            CharacterFrameRuntimeFactsWriter runtimeFactsWriter,
            CharacterFrameSnapshotWriter snapshotWriter,
            CharacterFrameDiagnosticSubmitter diagnosticSubmitter)
        {
            this.cacheWriter = cacheWriter ?? throw new ArgumentNullException(nameof(cacheWriter));
            this.inputRequestConsumer = inputRequestConsumer ?? throw new ArgumentNullException(nameof(inputRequestConsumer));
            this.motionOutputApplier = motionOutputApplier ?? throw new ArgumentNullException(nameof(motionOutputApplier));
            this.animationOutputPresenter = animationOutputPresenter ?? throw new ArgumentNullException(nameof(animationOutputPresenter));
            this.runtimeFactsWriter = runtimeFactsWriter ?? throw new ArgumentNullException(nameof(runtimeFactsWriter));
            this.snapshotWriter = snapshotWriter ?? throw new ArgumentNullException(nameof(snapshotWriter));
            this.diagnosticSubmitter = diagnosticSubmitter ?? throw new ArgumentNullException(nameof(diagnosticSubmitter));
        }

        public void SetLastFrameOutputs(
            in BasicLocomotionFrame locomotionFrame,
            in CharacterStateMachineFrame stateFrame,
            in ActionMotionResolveResult actionMotionResult)
        {
            cacheWriter.Write(in locomotionFrame, in stateFrame, in actionMotionResult);
        }

        public bool ConsumeFrameInputRequest(in CharacterFrameInputConsumeSubmission inputConsume)
        {
            return inputRequestConsumer.Consume(in inputConsume);
        }

        public void ExecuteFrameMotion(
            in CharacterFrameMovementSubmission movement,
            out bool actionMovementExecuted,
            out bool basicMovementExecuted)
        {
            motionOutputApplier.Apply(
                in movement,
                out actionMovementExecuted,
                out basicMovementExecuted);
        }

        public void PresentFrameAnimation(
            in CharacterFrameAnimationSubmission animation,
            in BasicLocomotionFrame locomotionFrame,
            out bool actionAnimationPresented,
            out bool locomotionAnimationPresented)
        {
            animationOutputPresenter.Present(
                in animation,
                in locomotionFrame,
                out actionAnimationPresented,
                out locomotionAnimationPresented);
        }

        public void WriteStateFrameActionFacts(
            in CharacterStateMachineFrame stateFrame,
            in ActionMotionResolveResult actionMotionResult,
            bool exitedToLocomotion,
            int step)
        {
            runtimeFactsWriter.WriteActionFacts(in stateFrame, in actionMotionResult, exitedToLocomotion, step);
        }

        public void WriteLocomotionPreemptionFact(in LocomotionPreemptionFact fact)
        {
            runtimeFactsWriter.WriteLocomotionPreemptionFact(in fact);
        }

        public void UpdateStateSnapshot(in CharacterStateMachineFrame stateFrame, int step)
        {
            snapshotWriter.Update(in stateFrame, step);
        }

        public void WriteAnimationRuntimeFacts(int step)
        {
            runtimeFactsWriter.WriteAnimationFacts(step);
        }

        public void CompleteLocomotionTick()
        {
            runtimeFactsWriter.CompleteLocomotionTick();
        }

        public void LogDiagnosticTickSnapshots(int step)
        {
            diagnosticSubmitter.SubmitTickSnapshots(step);
        }
    }

    internal sealed class CharacterFrameOutputCacheWriter
    {
        readonly ICharacterFrameOutputCache cache;

        public CharacterFrameOutputCacheWriter(ICharacterFrameOutputCache cache)
        {
            this.cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        public void Write(
            in BasicLocomotionFrame locomotionFrame,
            in CharacterStateMachineFrame stateFrame,
            in ActionMotionResolveResult actionMotionResult)
        {
            cache.LastLocomotionFrame = locomotionFrame;
            cache.LastStateFrame = stateFrame;
            cache.LastActionMotionResult = actionMotionResult;
        }
    }

    internal sealed class CharacterFrameInputRequestConsumer
    {
        readonly ICharacterFrameInputRequestConsumerDependencies dependencies;

        public CharacterFrameInputRequestConsumer(ICharacterFrameInputRequestConsumerDependencies dependencies)
        {
            this.dependencies = dependencies ?? throw new ArgumentNullException(nameof(dependencies));
        }

        public bool Consume(in CharacterFrameInputConsumeSubmission inputConsume)
        {
            if (!inputConsume.HasInputConsume)
                return false;

            InputRequestBuffer inputRequestBuffer = dependencies.InputRequestBuffer;
            return inputRequestBuffer != null &&
                   inputRequestBuffer.TryConsume(inputConsume.ConsumedRequestKind, inputConsume.Step, out _);
        }
    }

    internal sealed class CharacterFrameMotionOutputApplier
    {
        readonly ICharacterFrameMotionOutputDependencies dependencies;

        public CharacterFrameMotionOutputApplier(ICharacterFrameMotionOutputDependencies dependencies)
        {
            this.dependencies = dependencies ?? throw new ArgumentNullException(nameof(dependencies));
        }

        public void Apply(
            in CharacterFrameMovementSubmission movement,
            out bool actionMovementExecuted,
            out bool basicMovementExecuted)
        {
            actionMovementExecuted = false;
            basicMovementExecuted = false;

            IActionMovementExecutor actionMovementExecutor = dependencies.ActionMovementExecutor;
            ActionMotionResolveResult actionMotionResult = movement.ActionMotionResult;
            if (movement.ExecuteActionMovement && actionMotionResult.HasActionMovement && actionMovementExecutor != null)
            {
                ActionMovementCommand command = actionMotionResult.MovementCommand;
                actionMovementExecutor.ExecuteActionMovement(in command);
                actionMovementExecuted = true;
            }

            ILocomotionOutputRuntimePort locomotionOutputRuntime = dependencies.LocomotionOutputRuntime;
            if (movement.ExecuteBasicMovement && locomotionOutputRuntime != null)
            {
                BasicLocomotionFrame locomotionFrame = movement.LocomotionFrame;
                locomotionOutputRuntime.ExecuteLocomotionMotion(in locomotionFrame);
                basicMovementExecuted = true;
            }
        }
    }

    internal sealed class CharacterAnimationOutputPresenter
    {
        readonly ICharacterAnimationOutputDependencies dependencies;

        public CharacterAnimationOutputPresenter(ICharacterAnimationOutputDependencies dependencies)
        {
            this.dependencies = dependencies ?? throw new ArgumentNullException(nameof(dependencies));
        }

        public void Present(
            in CharacterFrameAnimationSubmission animation,
            in BasicLocomotionFrame locomotionFrame,
            out bool actionAnimationPresented,
            out bool locomotionAnimationPresented)
        {
            actionAnimationPresented = false;
            locomotionAnimationPresented = false;

            ICharacterAnimationOutputPresenter animationPresenter = dependencies.AnimationPresenter;
            CharacterStateAnimationRequest animationRequest = animation.AnimationRequest;
            if (animation.HasAnimationRequest &&
                animationRequest.IsActionAnimation &&
                animationPresenter != null)
            {
                animationPresenter.PresentAction(animationRequest);
                actionAnimationPresented = true;
            }

            if (animation.ExitedToLocomotion && animationPresenter != null)
                animationPresenter.ClearActionPlayback();

            ILocomotionOutputRuntimePort locomotionOutputRuntime = dependencies.LocomotionOutputRuntime;
            if (animation.PresentLocomotionAnimation && locomotionOutputRuntime != null)
            {
                locomotionOutputRuntime.PresentLocomotionAnimation(in locomotionFrame);
                locomotionAnimationPresented = true;
            }
        }
    }

    internal sealed class CharacterFrameRuntimeFactsWriter
    {
        readonly ICharacterFrameRuntimeFactsDependencies dependencies;

        public CharacterFrameRuntimeFactsWriter(ICharacterFrameRuntimeFactsDependencies dependencies)
        {
            this.dependencies = dependencies ?? throw new ArgumentNullException(nameof(dependencies));
        }

        public void WriteActionFacts(
            in CharacterStateMachineFrame stateFrame,
            in ActionMotionResolveResult actionMotionResult,
            bool exitedToLocomotion,
            int step)
        {
            ILocomotionOutputRuntimePort locomotionOutputRuntime = dependencies.LocomotionOutputRuntime;
            if (locomotionOutputRuntime == null)
                return;

            if (actionMotionResult.SetRunLatch)
                locomotionOutputRuntime.SetRunLatchActive(true);

            locomotionOutputRuntime.WriteActionFacts(CharacterRuntimeActionFacts.FromActionMotionResult(
                in actionMotionResult,
                exitedToLocomotion,
                step));
        }

        public void WriteAnimationFacts(int step)
        {
            ILocomotionOutputRuntimePort locomotionOutputRuntime = dependencies.LocomotionOutputRuntime;
            if (locomotionOutputRuntime == null)
                return;

            ICharacterAnimationOutputPresenter animationPresenter = dependencies.AnimationPresenter;
            CharacterAnimationPlaybackSnapshot snapshot = animationPresenter != null
                ? animationPresenter.CurrentSnapshot
                : CharacterAnimationPlaybackSnapshot.Empty(dependencies.LocomotionAnimationPlaybackProgress.Phase);
            ActionAnimationPlaybackProgress actionProgress = animationPresenter != null
                ? snapshot.ActionProgress
                : ActionAnimationPlaybackProgress.Invalid;
            string actionAnimationName = animationPresenter != null ? snapshot.ActionAnimationName : string.Empty;

            locomotionOutputRuntime.WriteAnimationFacts(new CharacterRuntimeAnimationFacts(
                dependencies.LocomotionAnimationPlaybackProgress,
                dependencies.LocomotionAnimationName,
                actionProgress,
                actionAnimationName,
                step));
        }

        public void WriteLocomotionPreemptionFact(in LocomotionPreemptionFact fact)
        {
            dependencies.LocomotionOutputRuntime?.WriteLocomotionPreemptionFact(in fact);
        }

        public void CompleteLocomotionTick()
        {
            dependencies.LocomotionOutputRuntime?.CompleteLocomotionTick();
        }
    }

    internal sealed class CharacterFrameSnapshotWriter
    {
        readonly ICharacterFrameSnapshotOutputState state;
        readonly CharacterFrameDiagnosticSubmitter diagnostics;

        public CharacterFrameSnapshotWriter(
            ICharacterFrameSnapshotOutputState state,
            CharacterFrameDiagnosticSubmitter diagnostics)
        {
            this.state = state ?? throw new ArgumentNullException(nameof(state));
            this.diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
        }

        public void Update(in CharacterStateMachineFrame stateFrame, int step)
        {
            CharacterStateMachineSnapshot previousSnapshot = state.CurrentStateSnapshot;
            state.CurrentStateSnapshot = stateFrame.Snapshot;
            state.DebugStatePath = state.CurrentStateSnapshot.ActivePath;
            state.DebugPendingTransitionPath = state.CurrentStateSnapshot.PendingTransitionPath;
            diagnostics.SubmitSnapshotChange(in previousSnapshot, state.CurrentStateSnapshot, in stateFrame, step);
        }
    }

    internal sealed class CharacterFrameDiagnosticSubmitter
    {
        readonly ICharacterFrameSnapshotOutputState state;
        readonly ICharacterFrameDiagnosticDependencies dependencies;

        public CharacterFrameDiagnosticSubmitter(
            ICharacterFrameSnapshotOutputState state,
            ICharacterFrameDiagnosticDependencies dependencies)
        {
            this.state = state ?? throw new ArgumentNullException(nameof(state));
            this.dependencies = dependencies ?? throw new ArgumentNullException(nameof(dependencies));
        }

        public void SubmitSnapshotChange(
            in CharacterStateMachineSnapshot previousSnapshot,
            in CharacterStateMachineSnapshot snapshot,
            in CharacterStateMachineFrame frame,
            int step)
        {
            LogStateSnapshotChange(in previousSnapshot, in snapshot, step);
            LogLocomotionStateChange(in snapshot, step);
            LogActionDecision(in previousSnapshot, in snapshot, in frame, step);
            CharacterFrameDiagnostics.LogTransitionConditionTraces(frame.ConditionTraces);
        }

        public void SubmitTickSnapshots(int step)
        {
            CharacterStateMachineSnapshot snapshot = state.CurrentStateSnapshot;
            CharacterFrameDiagnostics.LogStateTickSnapshot(in snapshot, step, BuildStateTickContext());
            dependencies.LogLocomotionDiagnosticTickSnapshot(step);
            CharacterFrameDiagnostics.LogAnimationTickSnapshot(snapshot.ActivePath, step, BuildAnimationTickContext());
        }

        void LogStateSnapshotChange(
            in CharacterStateMachineSnapshot previousSnapshot,
            in CharacterStateMachineSnapshot snapshot,
            int step)
        {
            if (snapshot.ActivePath != state.LastLoggedStatePath)
            {
                CharacterFrameDiagnostics.LogStatePathChanged(in previousSnapshot, in snapshot, step);
                state.LastLoggedStatePath = snapshot.ActivePath;
            }

            if (snapshot.PendingTransitionPath == state.LastLoggedPendingTransitionPath)
                return;

            CharacterFrameDiagnostics.LogPendingTransitionChanged(in previousSnapshot, in snapshot, step);
            state.LastLoggedPendingTransitionPath = snapshot.PendingTransitionPath;
        }

        void LogLocomotionStateChange(in CharacterStateMachineSnapshot snapshot, int step)
        {
            CharacterStateDomainView stateView = CharacterStateDomainView.FromSnapshot(in snapshot);
            string locomotionPath = stateView.IsLocomotion ? snapshot.ActivePath : state.LastLoggedLocomotionPath;
            if (state.LoggedInitialLocomotionState &&
                stateView.LocomotionPhase == state.LastLoggedLocomotionPhase &&
                locomotionPath == state.LastLoggedLocomotionPath)
                return;

            CharacterFrameDiagnostics.LogLocomotionPhaseChanged(
                locomotionPath,
                state.LastLoggedLocomotionPath,
                state.LastLoggedLocomotionPhase,
                state.LastLocomotionFrame.Command.Gait,
                in snapshot,
                step);
            state.LastLoggedLocomotionPhase = stateView.LocomotionPhase;
            state.LastLoggedLocomotionPath = locomotionPath;
            state.LoggedInitialLocomotionState = true;
        }

        static void LogActionDecision(
            in CharacterStateMachineSnapshot previousSnapshot,
            in CharacterStateMachineSnapshot snapshot,
            in CharacterStateMachineFrame frame,
            int step)
        {
            if (frame.ConsumeInputRequest)
                CharacterFrameDiagnostics.LogActionAccepted(in previousSnapshot, in snapshot, in frame, step);
        }

        string BuildStateTickContext()
        {
            CharacterStateMachineSnapshot snapshot = state.CurrentStateSnapshot;
            CharacterStateDomainView stateView = CharacterStateDomainView.FromSnapshot(in snapshot);
            return
                $"owner={stateView.Owner.Kind} ownerAction={stateView.ActionState.Value} " +
                $"stateTime={snapshot.StateTime:F3} pending={snapshot.PendingTransitionPath} variant={snapshot.Variant} " +
                $"locomotionPhase={stateView.LocomotionPhase} locomotionPath={snapshot.ActivePath} locomotionGait={state.LastLocomotionFrame.Command.Gait} " +
                $"hasMove={state.LastLocomotionFrame.Intent.HasMoveIntent} moveStrength={state.LastLocomotionFrame.Intent.Strength:F3} worldDirection={state.LastLocomotionFrame.WorldDirection.ToString("F3")} " +
                $"actionFrameActive={state.LastStateFrame.ActionState.IsValid && state.LastStateFrame.ActionState != ActionStateIds.None} actionFrameCompleted={state.LastActionMotionResult.ActionCompleted} actionMove={state.LastActionMotionResult.MovementCommand.PlanarDistance:F3} actionRotate={state.LastActionMotionResult.MovementCommand.RotateToDirection}";
        }

        string BuildAnimationTickContext()
        {
            CharacterStateMachineSnapshot snapshot = state.CurrentStateSnapshot;
            CharacterStateDomainView stateView = CharacterStateDomainView.FromSnapshot(in snapshot);
            AnimationPhasePlaybackProgress locomotionProgress = dependencies.LocomotionAnimationPlaybackProgress;

            return
                $"owner={stateView.Owner.Kind} fullBodyPath={snapshot.ActivePath} " +
                $"locomotionPhase={stateView.LocomotionPhase} locomotionGait={state.LastLocomotionFrame.Command.Gait} " +
                $"locomotionAlias={locomotionProgress.AliasKey} locomotionAnimation={dependencies.LocomotionAnimationName} locomotionNormalized={locomotionProgress.NormalizedTime:F3} locomotionValid={locomotionProgress.HasValidPlayback} locomotionEnded={locomotionProgress.IsEnded} " +
                $"actionKey={dependencies.ActionAnimationKey.Value} actionAnimation={dependencies.ActionAnimationName} actionNormalized={dependencies.ActionAnimationNormalizedTime:F3} actionValid={dependencies.ActionAnimationHasValidPlayback} actionEnded={dependencies.ActionAnimationPlaybackEnded}";
        }
    }
}
