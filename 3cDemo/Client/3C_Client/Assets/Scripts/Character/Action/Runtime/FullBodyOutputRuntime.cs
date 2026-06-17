using System;
using ThirdPersonAnimation;
using ThirdPersonCharacterStateMachine;
using ThirdPersonInput;
using ThirdPersonMovement;

namespace ThirdPersonAction
{
    internal interface IFullBodyOutputFrameCache
    {
        BasicLocomotionFrame LastLocomotionFrame { get; set; }
        CharacterStateMachineFrame LastStateFrame { get; set; }
        ActionMotionResolveResult LastActionMotionResult { get; set; }
    }

    internal interface IFullBodyInputRequestConsumerDependencies
    {
        InputRequestBuffer InputRequestBuffer { get; }
    }

    internal interface IFullBodyMotionOutputDependencies
    {
        IActionMovementExecutor ActionMovementExecutor { get; }
        ILocomotionOutputRuntimePort LocomotionOutputRuntime { get; }
    }

    internal interface ICharacterAnimationOutputDependencies
    {
        ICharacterAnimationOutputPresenter AnimationPresenter { get; }
        ILocomotionOutputRuntimePort LocomotionOutputRuntime { get; }
    }

    internal interface IFullBodyRuntimeFactsDependencies
    {
        ILocomotionOutputRuntimePort LocomotionOutputRuntime { get; }
        ICharacterAnimationOutputPresenter AnimationPresenter { get; }
        AnimationPhasePlaybackProgress LocomotionAnimationPlaybackProgress { get; }
        string LocomotionAnimationName { get; }
    }

    internal interface IFullBodySnapshotOutputState : IFullBodyOutputFrameCache
    {
        CharacterStateMachineSnapshot CurrentStateSnapshot { get; set; }
        string DebugFullBodyStatePath { get; set; }
        string DebugPendingTransitionPath { get; set; }
        string LastLoggedFullBodyPath { get; set; }
        string LastLoggedPendingTransitionPath { get; set; }
        string LastLoggedLocomotionPath { get; set; }
        BasicMovementPhase LastLoggedLocomotionPhase { get; set; }
        bool LoggedInitialLocomotionState { get; set; }
    }

    internal interface IFullBodyDiagnosticDependencies : IFullBodyRuntimeFactsDependencies
    {
        ActionAnimationKey ActionAnimationKey { get; }
        float ActionAnimationNormalizedTime { get; }
        bool ActionAnimationHasValidPlayback { get; }
        bool ActionAnimationPlaybackEnded { get; }
        string ActionAnimationName { get; }
        void LogLocomotionDiagnosticTickSnapshot(int step);
    }

    internal sealed class FullBodyOutputRuntime
    {
        readonly FullBodyOutputCacheWriter cacheWriter;
        readonly FullBodyInputRequestConsumer inputRequestConsumer;
        readonly FullBodyMotionOutputApplier motionOutputApplier;
        readonly CharacterAnimationOutputPresenter animationOutputPresenter;
        readonly FullBodyRuntimeFactsWriter runtimeFactsWriter;
        readonly FullBodySnapshotWriter snapshotWriter;
        readonly FullBodyDiagnosticSubmitter diagnosticSubmitter;

        public FullBodyOutputRuntime(
            FullBodyOutputCacheWriter cacheWriter,
            FullBodyInputRequestConsumer inputRequestConsumer,
            FullBodyMotionOutputApplier motionOutputApplier,
            CharacterAnimationOutputPresenter animationOutputPresenter,
            FullBodyRuntimeFactsWriter runtimeFactsWriter,
            FullBodySnapshotWriter snapshotWriter,
            FullBodyDiagnosticSubmitter diagnosticSubmitter)
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

    internal sealed class FullBodyOutputCacheWriter
    {
        readonly IFullBodyOutputFrameCache cache;

        public FullBodyOutputCacheWriter(IFullBodyOutputFrameCache cache)
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

    internal sealed class FullBodyInputRequestConsumer
    {
        readonly IFullBodyInputRequestConsumerDependencies dependencies;

        public FullBodyInputRequestConsumer(IFullBodyInputRequestConsumerDependencies dependencies)
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

    internal sealed class FullBodyMotionOutputApplier
    {
        readonly IFullBodyMotionOutputDependencies dependencies;

        public FullBodyMotionOutputApplier(IFullBodyMotionOutputDependencies dependencies)
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

    internal sealed class FullBodyRuntimeFactsWriter
    {
        readonly IFullBodyRuntimeFactsDependencies dependencies;

        public FullBodyRuntimeFactsWriter(IFullBodyRuntimeFactsDependencies dependencies)
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

    internal sealed class FullBodySnapshotWriter
    {
        readonly IFullBodySnapshotOutputState state;
        readonly FullBodyDiagnosticSubmitter diagnostics;

        public FullBodySnapshotWriter(
            IFullBodySnapshotOutputState state,
            FullBodyDiagnosticSubmitter diagnostics)
        {
            this.state = state ?? throw new ArgumentNullException(nameof(state));
            this.diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
        }

        public void Update(in CharacterStateMachineFrame stateFrame, int step)
        {
            CharacterStateMachineSnapshot previousSnapshot = state.CurrentStateSnapshot;
            state.CurrentStateSnapshot = stateFrame.Snapshot;
            state.DebugFullBodyStatePath = state.CurrentStateSnapshot.ActivePath;
            state.DebugPendingTransitionPath = state.CurrentStateSnapshot.PendingTransitionPath;
            diagnostics.SubmitSnapshotChange(in previousSnapshot, state.CurrentStateSnapshot, in stateFrame, step);
        }
    }

    internal sealed class FullBodyDiagnosticSubmitter
    {
        readonly IFullBodySnapshotOutputState state;
        readonly IFullBodyDiagnosticDependencies dependencies;

        public FullBodyDiagnosticSubmitter(
            IFullBodySnapshotOutputState state,
            IFullBodyDiagnosticDependencies dependencies)
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
            LogFullBodySnapshotChange(in previousSnapshot, in snapshot, step);
            LogLocomotionStateChange(in snapshot, step);
            LogActionDecision(in previousSnapshot, in snapshot, in frame, step);
            FullBodyDiagnostics.LogTransitionConditionTraces(frame.ConditionTraces);
        }

        public void SubmitTickSnapshots(int step)
        {
            CharacterStateMachineSnapshot snapshot = state.CurrentStateSnapshot;
            FullBodyDiagnostics.LogFullBodyTickSnapshot(in snapshot, step, BuildFullBodyTickContext());
            dependencies.LogLocomotionDiagnosticTickSnapshot(step);
            FullBodyDiagnostics.LogAnimationTickSnapshot(snapshot.ActivePath, step, BuildAnimationTickContext());
        }

        void LogFullBodySnapshotChange(
            in CharacterStateMachineSnapshot previousSnapshot,
            in CharacterStateMachineSnapshot snapshot,
            int step)
        {
            if (snapshot.ActivePath != state.LastLoggedFullBodyPath)
            {
                FullBodyDiagnostics.LogFullBodyPathChanged(in previousSnapshot, in snapshot, step);
                state.LastLoggedFullBodyPath = snapshot.ActivePath;
            }

            if (snapshot.PendingTransitionPath == state.LastLoggedPendingTransitionPath)
                return;

            FullBodyDiagnostics.LogFullBodyPendingTransitionChanged(in previousSnapshot, in snapshot, step);
            state.LastLoggedPendingTransitionPath = snapshot.PendingTransitionPath;
        }

        void LogLocomotionStateChange(in CharacterStateMachineSnapshot snapshot, int step)
        {
            FullBodyStateView stateView = FullBodyStateView.FromSnapshot(in snapshot);
            string locomotionPath = stateView.IsLocomotion ? snapshot.ActivePath : state.LastLoggedLocomotionPath;
            if (state.LoggedInitialLocomotionState &&
                stateView.LocomotionPhase == state.LastLoggedLocomotionPhase &&
                locomotionPath == state.LastLoggedLocomotionPath)
                return;

            FullBodyDiagnostics.LogLocomotionPhaseChanged(
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
                FullBodyDiagnostics.LogActionAccepted(in previousSnapshot, in snapshot, in frame, step);
        }

        string BuildFullBodyTickContext()
        {
            CharacterStateMachineSnapshot snapshot = state.CurrentStateSnapshot;
            FullBodyStateView stateView = FullBodyStateView.FromSnapshot(in snapshot);
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
            FullBodyStateView stateView = FullBodyStateView.FromSnapshot(in snapshot);
            AnimationPhasePlaybackProgress locomotionProgress = dependencies.LocomotionAnimationPlaybackProgress;

            return
                $"owner={stateView.Owner.Kind} fullBodyPath={snapshot.ActivePath} " +
                $"locomotionPhase={stateView.LocomotionPhase} locomotionGait={state.LastLocomotionFrame.Command.Gait} " +
                $"locomotionAlias={locomotionProgress.AliasKey} locomotionAnimation={dependencies.LocomotionAnimationName} locomotionNormalized={locomotionProgress.NormalizedTime:F3} locomotionValid={locomotionProgress.HasValidPlayback} locomotionEnded={locomotionProgress.IsEnded} " +
                $"actionKey={dependencies.ActionAnimationKey.Value} actionAnimation={dependencies.ActionAnimationName} actionNormalized={dependencies.ActionAnimationNormalizedTime:F3} actionValid={dependencies.ActionAnimationHasValidPlayback} actionEnded={dependencies.ActionAnimationPlaybackEnded}";
        }
    }
}
