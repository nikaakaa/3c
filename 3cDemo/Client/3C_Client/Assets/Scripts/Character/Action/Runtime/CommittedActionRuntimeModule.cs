using System;
using ThirdPersonAnimation;
using ThirdPersonCharacterConfig;
using ThirdPersonCharacterStateMachine;
using ThirdPersonInput;
using ThirdPersonMovement;

namespace ThirdPersonAction
{
    public sealed class CommittedActionRuntimeModule
    {
        readonly CharacterStateMachineRuntime stateMachineRuntime = new CharacterStateMachineRuntime();
        readonly ActionLifecycleRuntime actionLifecycleRuntime = new ActionLifecycleRuntime();
        ICommittedActionRuntimeUnityAdapter unityAdapter;
        CharacterFrameOutputRuntime outputRuntime;
        CharacterFrameOutputRuntimeHost outputRuntimeHost;

        public CharacterStateMachineRuntime StateMachineRuntime => stateMachineRuntime;
        public CharacterStateMachineRunner StateMachine => stateMachineRuntime.StateMachine;
        public CharacterStateMachineSnapshot CurrentStateSnapshot => stateMachineRuntime.CurrentStateSnapshot;
        public string ActiveStatePath => stateMachineRuntime.ActiveStatePath;
        public string PendingStateTransitionPath => stateMachineRuntime.PendingStateTransitionPath;
        public CharacterStateMachineFrame LastStateFrame { get; internal set; }
        public BasicLocomotionFrame LastLocomotionFrame { get; internal set; }
        public ActionMotionResolveResult LastActionMotionResult { get; internal set; }
        internal CharacterFrameOutputRuntime OutputRuntime => outputRuntime ?? (outputRuntime = CreateOutputRuntime());

        public void Bind(ICommittedActionRuntimeUnityAdapter adapter)
        {
            if (ReferenceEquals(unityAdapter, adapter))
                return;

            unityAdapter = adapter;
            outputRuntime = null;
            outputRuntimeHost = null;
        }

        public void Reset()
        {
            stateMachineRuntime.Reset();
            actionLifecycleRuntime.Reset();
        }

        public bool Rebuild(CharacterStateMachineDefinitionSO definition, bool logErrors)
        {
            return stateMachineRuntime.Rebuild(definition, logErrors);
        }

        public bool EnsureStateMachine(CharacterStateMachineDefinitionSO definition, bool logErrors)
        {
            if (stateMachineRuntime.StateMachine != null)
                return true;

            return Rebuild(definition, logErrors);
        }

        public int ResolveCurrentActionResistance(in CharacterActionCatalog catalog)
        {
            ActionStateId activeAction = actionLifecycleRuntime.ActiveActionState;
            if (!activeAction.IsValid || !catalog.TryGetDefinition(activeAction, out CharacterActionDefinition definition))
                return 0;

            return definition.Resistance;
        }

        public ActionLifecycleFrame TickActionLifecycle(
            in CharacterResolvedAction acceptedAction,
            float deltaTime,
            int step)
        {
            return actionLifecycleRuntime.Tick(in acceptedAction, deltaTime, step);
        }

        public ActionLifecycleFrame TickActionLifecycle(
            in CharacterResolvedAction acceptedAction,
            in CharacterActionCatalog actionCatalog,
            float deltaTime,
            int step)
        {
            return actionLifecycleRuntime.Tick(in acceptedAction, in actionCatalog, deltaTime, step);
        }

        public void CompleteActionLifecycle(
            in ActionMotionResolveResult result,
            in ActionAnimationPlaybackProgress actionProgress,
            bool requireAnimationEnded)
        {
            actionLifecycleRuntime.Complete(in result, in actionProgress, requireAnimationEnded);
        }

        public void CompleteActionLifecycle(in ActionMotionResolveResult result, bool requireAnimationEnded)
        {
            ActionAnimationPlaybackProgress actionProgress = ActionAnimationPlaybackProgress.Invalid;
            if (requireAnimationEnded && unityAdapter != null && unityAdapter.AnimationPresenter != null)
                actionProgress = unityAdapter.AnimationPresenter.CurrentSnapshot.ActionProgress;

            CompleteActionLifecycle(in result, in actionProgress, requireAnimationEnded);
        }

        public CommittedActionRestoreState CaptureRestoreState()
        {
            CommittedActionRestoreState stateMachineRestore = stateMachineRuntime.CaptureRestoreState();
            CommittedActionGameplayRestoreState gameplay = new CommittedActionGameplayRestoreState(
                stateMachineRestore.Gameplay.StateMachine,
                actionLifecycleRuntime.CaptureRestoreState());
            return new CommittedActionRestoreState(gameplay, stateMachineRestore.Diagnostic);
        }

        public bool Restore(in CommittedActionRestoreState restoreState)
        {
            if (!stateMachineRuntime.Restore(in restoreState))
                return false;

            actionLifecycleRuntime.Restore(restoreState.Gameplay.ActionLifecycle);
            return true;
        }

        CharacterFrameOutputRuntime CreateOutputRuntime()
        {
            outputRuntimeHost ??= new CharacterFrameOutputRuntimeHost(this, unityAdapter);
            CharacterFrameDiagnosticSubmitter diagnostics = new CharacterFrameDiagnosticSubmitter(outputRuntimeHost, outputRuntimeHost);
            return new CharacterFrameOutputRuntime(
                new CharacterFrameOutputCacheWriter(outputRuntimeHost),
                new CharacterFrameInputRequestConsumer(outputRuntimeHost),
                new CharacterFrameMotionOutputApplier(outputRuntimeHost),
                new CharacterAnimationOutputPresenter(outputRuntimeHost),
                new CharacterFrameRuntimeFactsWriter(outputRuntimeHost),
                new CharacterFrameSnapshotWriter(outputRuntimeHost, diagnostics),
                diagnostics);
        }
    }

    public interface ICommittedActionRuntimeUnityAdapter
    {
        InputRequestBufferComponent InputBufferComponent { get; }
        ILocomotionOutputRuntimePort LocomotionOutputRuntime { get; }
        AnimationPhasePlaybackProgress LocomotionAnimationPlaybackProgress { get; }
        string LocomotionAnimationName { get; }
        IActionMovementExecutor ActionMovementExecutor { get; }
        ICharacterAnimationOutputPresenter AnimationPresenter { get; }
        void LogLocomotionDiagnosticTickSnapshot(int step);
    }
}
