using System;
using ThirdPersonAnimation;
using ThirdPersonCharacterConfig;
using ThirdPersonCharacterStateMachine;
using ThirdPersonInput;
using ThirdPersonMovement;

namespace ThirdPersonAction
{
    public sealed class FullBodyActionRuntimeModule
    {
        readonly CharacterStateMachineRuntime stateMachineRuntime = new CharacterStateMachineRuntime();
        readonly ActionLifecycleRuntime actionLifecycleRuntime = new ActionLifecycleRuntime();
        IFullBodyActionRuntimeUnityAdapter unityAdapter;
        FullBodyOutputRuntime outputRuntime;
        FullBodyOutputRuntimeHost outputRuntimeHost;

        public CharacterStateMachineRuntime StateMachineRuntime => stateMachineRuntime;
        public CharacterStateMachineRunner StateMachine => stateMachineRuntime.StateMachine;
        public CharacterStateMachineSnapshot CurrentStateSnapshot => stateMachineRuntime.CurrentStateSnapshot;
        public string ActiveFullBodyStatePath => stateMachineRuntime.ActiveFullBodyStatePath;
        public string PendingFullBodyTransitionPath => stateMachineRuntime.PendingFullBodyTransitionPath;
        public CharacterStateMachineFrame LastStateFrame { get; internal set; }
        public BasicLocomotionFrame LastLocomotionFrame { get; internal set; }
        public ActionMotionResolveResult LastActionMotionResult { get; internal set; }
        internal FullBodyOutputRuntime OutputRuntime => outputRuntime ?? (outputRuntime = CreateOutputRuntime());

        public void Bind(IFullBodyActionRuntimeUnityAdapter adapter)
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

        public FullBodyActionRestoreState CaptureRestoreState()
        {
            FullBodyActionRestoreState stateMachineRestore = stateMachineRuntime.CaptureRestoreState();
            FullBodyActionGameplayRestoreState gameplay = new FullBodyActionGameplayRestoreState(
                stateMachineRestore.Gameplay.StateMachine,
                actionLifecycleRuntime.CaptureRestoreState());
            return new FullBodyActionRestoreState(gameplay, stateMachineRestore.Diagnostic);
        }

        public bool Restore(in FullBodyActionRestoreState restoreState)
        {
            if (!stateMachineRuntime.Restore(in restoreState))
                return false;

            actionLifecycleRuntime.Restore(restoreState.Gameplay.ActionLifecycle);
            return true;
        }

        FullBodyOutputRuntime CreateOutputRuntime()
        {
            outputRuntimeHost ??= new FullBodyOutputRuntimeHost(this, unityAdapter);
            FullBodyDiagnosticSubmitter diagnostics = new FullBodyDiagnosticSubmitter(outputRuntimeHost, outputRuntimeHost);
            return new FullBodyOutputRuntime(
                new FullBodyOutputCacheWriter(outputRuntimeHost),
                new FullBodyInputRequestConsumer(outputRuntimeHost),
                new FullBodyMotionOutputApplier(outputRuntimeHost),
                new CharacterAnimationOutputPresenter(outputRuntimeHost),
                new FullBodyRuntimeFactsWriter(outputRuntimeHost),
                new FullBodySnapshotWriter(outputRuntimeHost, diagnostics),
                diagnostics);
        }
    }

    public interface IFullBodyActionRuntimeUnityAdapter
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
