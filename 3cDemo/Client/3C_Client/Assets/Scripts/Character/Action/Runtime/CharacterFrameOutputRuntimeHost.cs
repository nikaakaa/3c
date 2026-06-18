using System;
using ThirdPersonAnimation;
using ThirdPersonCharacterStateMachine;
using ThirdPersonInput;
using ThirdPersonMovement;

namespace ThirdPersonAction
{
    internal sealed class CharacterFrameOutputRuntimeHost :
        ICharacterFrameOutputCache,
        ICharacterFrameInputRequestConsumerDependencies,
        ICharacterFrameMotionOutputDependencies,
        ICharacterAnimationOutputDependencies,
        ICharacterFrameRuntimeFactsDependencies,
        ICharacterFrameSnapshotOutputState,
        ICharacterFrameDiagnosticDependencies
    {
        readonly CommittedActionRuntimeModule module;
        readonly ICommittedActionRuntimeUnityAdapter unityAdapter;

        public CharacterFrameOutputRuntimeHost(
            CommittedActionRuntimeModule module,
            ICommittedActionRuntimeUnityAdapter unityAdapter)
        {
            this.module = module ?? throw new ArgumentNullException(nameof(module));
            this.unityAdapter = unityAdapter;
        }

        public BasicLocomotionFrame LastLocomotionFrame
        {
            get => module.LastLocomotionFrame;
            set => module.LastLocomotionFrame = value;
        }

        public CharacterStateMachineFrame LastStateFrame
        {
            get => module.LastStateFrame;
            set => module.LastStateFrame = value;
        }

        public ActionMotionResolveResult LastActionMotionResult
        {
            get => module.LastActionMotionResult;
            set => module.LastActionMotionResult = value;
        }

        public CharacterStateMachineSnapshot CurrentStateSnapshot
        {
            get => module.StateMachineRuntime.CurrentStateSnapshot;
            set => module.StateMachineRuntime.CurrentStateSnapshot = value;
        }

        public string DebugStatePath
        {
            get => module.StateMachineRuntime.DebugStatePath;
            set => module.StateMachineRuntime.DebugStatePath = value;
        }

        public string DebugPendingTransitionPath
        {
            get => module.StateMachineRuntime.DebugPendingTransitionPath;
            set => module.StateMachineRuntime.DebugPendingTransitionPath = value;
        }

        public string LastLoggedStatePath
        {
            get => module.StateMachineRuntime.LastLoggedStatePath;
            set => module.StateMachineRuntime.LastLoggedStatePath = value;
        }

        public string LastLoggedPendingTransitionPath
        {
            get => module.StateMachineRuntime.LastLoggedPendingTransitionPath;
            set => module.StateMachineRuntime.LastLoggedPendingTransitionPath = value;
        }

        public string LastLoggedLocomotionPath
        {
            get => module.StateMachineRuntime.LastLoggedLocomotionPath;
            set => module.StateMachineRuntime.LastLoggedLocomotionPath = value;
        }

        public BasicMovementPhase LastLoggedLocomotionPhase
        {
            get => module.StateMachineRuntime.LastLoggedLocomotionPhase;
            set => module.StateMachineRuntime.LastLoggedLocomotionPhase = value;
        }

        public bool LoggedInitialLocomotionState
        {
            get => module.StateMachineRuntime.LoggedInitialLocomotionState;
            set => module.StateMachineRuntime.LoggedInitialLocomotionState = value;
        }

        public InputRequestBuffer InputRequestBuffer =>
            unityAdapter != null && unityAdapter.InputBufferComponent != null ? unityAdapter.InputBufferComponent.Buffer : null;

        public IActionMovementExecutor ActionMovementExecutor => unityAdapter?.ActionMovementExecutor;
        public ILocomotionOutputRuntimePort LocomotionOutputRuntime => unityAdapter?.LocomotionOutputRuntime;
        public ICharacterAnimationOutputPresenter AnimationPresenter => unityAdapter?.AnimationPresenter;

        public AnimationPhasePlaybackProgress LocomotionAnimationPlaybackProgress
        {
            get
            {
                if (unityAdapter != null && unityAdapter.LocomotionOutputRuntime != null)
                    return unityAdapter.LocomotionAnimationPlaybackProgress;

                CharacterStateMachineSnapshot snapshot = module.StateMachineRuntime.CurrentStateSnapshot;
                return AnimationPhasePlaybackProgress.Invalid(CharacterStateDomainView.FromSnapshot(in snapshot).LocomotionPhase);
            }
        }

        public string LocomotionAnimationName =>
            unityAdapter?.LocomotionAnimationName ?? string.Empty;

        public ActionAnimationKey ActionAnimationKey =>
            unityAdapter != null && unityAdapter.AnimationPresenter != null ? unityAdapter.AnimationPresenter.CurrentSnapshot.ActionProgress.Key : default;

        public float ActionAnimationNormalizedTime =>
            unityAdapter != null && unityAdapter.AnimationPresenter != null ? unityAdapter.AnimationPresenter.CurrentSnapshot.ActionProgress.NormalizedTime : 0f;

        public bool ActionAnimationHasValidPlayback =>
            unityAdapter != null && unityAdapter.AnimationPresenter != null && unityAdapter.AnimationPresenter.CurrentSnapshot.ActionProgress.HasValidPlayback;

        public bool ActionAnimationPlaybackEnded =>
            unityAdapter != null && unityAdapter.AnimationPresenter != null && unityAdapter.AnimationPresenter.CurrentSnapshot.ActionProgress.IsEnded;

        public string ActionAnimationName =>
            unityAdapter != null && unityAdapter.AnimationPresenter != null ? unityAdapter.AnimationPresenter.CurrentSnapshot.ActionAnimationName : string.Empty;

        public void LogLocomotionDiagnosticTickSnapshot(int step)
        {
            unityAdapter?.LogLocomotionDiagnosticTickSnapshot(step);
        }
    }
}
