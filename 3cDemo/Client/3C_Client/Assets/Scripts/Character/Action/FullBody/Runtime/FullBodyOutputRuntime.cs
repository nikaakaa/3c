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

    internal interface IFullBodyAnimationOutputDependencies
    {
        IActionAnimationPresenter ActionAnimationPresenter { get; }
        ILocomotionOutputRuntimePort LocomotionOutputRuntime { get; }
    }

    internal interface IFullBodyRuntimeFactsDependencies
    {
        ILocomotionOutputRuntimePort LocomotionOutputRuntime { get; }
        IActionAnimationPresenter ActionAnimationPresenter { get; }
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
        readonly FullBodyAnimationOutputPresenter animationOutputPresenter;
        readonly FullBodyRuntimeFactsWriter runtimeFactsWriter;
        readonly FullBodySnapshotWriter snapshotWriter;
        readonly FullBodyDiagnosticSubmitter diagnosticSubmitter;

        public FullBodyOutputRuntime(
            FullBodyOutputCacheWriter cacheWriter,
            FullBodyInputRequestConsumer inputRequestConsumer,
            FullBodyMotionOutputApplier motionOutputApplier,
            FullBodyAnimationOutputPresenter animationOutputPresenter,
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

        public bool ConsumeStateFrameInputRequest(in CharacterStateMachineFrame stateFrame, int step)
        {
            return inputRequestConsumer.Consume(in stateFrame, step);
        }

        public void ExecuteStateFrameMotion(
            in CharacterStateMachineFrame stateFrame,
            in BasicLocomotionFrame locomotionFrame,
            in ActionMotionResolveResult actionMotionResult,
            out bool actionMovementExecuted,
            out bool basicMovementExecuted)
        {
            motionOutputApplier.Apply(
                in stateFrame,
                in locomotionFrame,
                in actionMotionResult,
                out actionMovementExecuted,
                out basicMovementExecuted);
        }

        public void PresentStateFrameAnimation(
            in CharacterStateMachineFrame stateFrame,
            in BasicLocomotionFrame locomotionFrame,
            bool exitedToLocomotion,
            out bool actionAnimationPresented,
            out bool locomotionAnimationPresented)
        {
            animationOutputPresenter.Present(
                in stateFrame,
                in locomotionFrame,
                exitedToLocomotion,
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

        public bool Consume(in CharacterStateMachineFrame stateFrame, int step)
        {
            if (!stateFrame.ConsumeInputRequest)
                return false;

            InputRequestBuffer inputRequestBuffer = dependencies.InputRequestBuffer;
            return inputRequestBuffer != null && inputRequestBuffer.TryConsume(stateFrame.ConsumedRequestKind, step, out _);
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
            in CharacterStateMachineFrame stateFrame,
            in BasicLocomotionFrame locomotionFrame,
            in ActionMotionResolveResult actionMotionResult,
            out bool actionMovementExecuted,
            out bool basicMovementExecuted)
        {
            actionMovementExecuted = false;
            basicMovementExecuted = false;

            IActionMovementExecutor actionMovementExecutor = dependencies.ActionMovementExecutor;
            if (actionMotionResult.HasActionMovement && actionMovementExecutor != null)
            {
                ActionMovementCommand command = actionMotionResult.MovementCommand;
                actionMovementExecutor.ExecuteActionMovement(in command);
                actionMovementExecuted = true;
            }

            ILocomotionOutputRuntimePort locomotionOutputRuntime = dependencies.LocomotionOutputRuntime;
            if (stateFrame.ExecuteBasicMovement && locomotionOutputRuntime != null)
            {
                locomotionOutputRuntime.ExecuteLocomotionMotion(in locomotionFrame);
                basicMovementExecuted = true;
            }
        }
    }

    internal sealed class FullBodyAnimationOutputPresenter
    {
        readonly IFullBodyAnimationOutputDependencies dependencies;

        public FullBodyAnimationOutputPresenter(IFullBodyAnimationOutputDependencies dependencies)
        {
            this.dependencies = dependencies ?? throw new ArgumentNullException(nameof(dependencies));
        }

        public void Present(
            in CharacterStateMachineFrame stateFrame,
            in BasicLocomotionFrame locomotionFrame,
            bool exitedToLocomotion,
            out bool actionAnimationPresented,
            out bool locomotionAnimationPresented)
        {
            actionAnimationPresented = false;
            locomotionAnimationPresented = false;

            IActionAnimationPresenter actionAnimationPresenter = dependencies.ActionAnimationPresenter;
            if (stateFrame.HasAnimationRequest &&
                stateFrame.AnimationRequest.IsActionAnimation &&
                actionAnimationPresenter != null)
            {
                actionAnimationPresenter.Present(stateFrame.AnimationRequest);
                actionAnimationPresented = true;
            }

            if (exitedToLocomotion && actionAnimationPresenter != null)
                actionAnimationPresenter.Clear();

            ILocomotionOutputRuntimePort locomotionOutputRuntime = dependencies.LocomotionOutputRuntime;
            if (stateFrame.PresentLocomotionAnimation && locomotionOutputRuntime != null)
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

            locomotionOutputRuntime.WriteActionFacts(CharacterRuntimeActionFacts.FromStateFrame(
                in stateFrame,
                in actionMotionResult,
                exitedToLocomotion,
                step));
        }

        public void WriteAnimationFacts(int step)
        {
            ILocomotionOutputRuntimePort locomotionOutputRuntime = dependencies.LocomotionOutputRuntime;
            if (locomotionOutputRuntime == null)
                return;

            IActionAnimationPresenter actionAnimationPresenter = dependencies.ActionAnimationPresenter;
            ActionAnimationPlaybackProgress actionProgress = actionAnimationPresenter != null
                ? actionAnimationPresenter.CurrentPlaybackProgress
                : ActionAnimationPlaybackProgress.Invalid;
            string actionAnimationName = actionAnimationPresenter != null ? actionAnimationPresenter.CurrentAnimationName : string.Empty;

            locomotionOutputRuntime.WriteAnimationFacts(new CharacterRuntimeAnimationFacts(
                dependencies.LocomotionAnimationPlaybackProgress,
                dependencies.LocomotionAnimationName,
                actionProgress,
                actionAnimationName,
                step));
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
                $"actionFrameActive={state.LastStateFrame.Owner.IsAction} actionFrameCompleted={state.LastActionMotionResult.ActionCompleted} actionMove={state.LastActionMotionResult.MovementCommand.PlanarDistance:F3} actionRotate={state.LastActionMotionResult.MovementCommand.RotateToDirection}";
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
