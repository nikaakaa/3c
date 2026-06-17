using ThirdPersonAnimation;
using ThirdPersonCamera;
using ThirdPersonCharacterConfig;
using ThirdPersonCharacterStateMachine;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonMovement
{
    public readonly struct LocomotionRuntimeModuleRestoreState
    {
        public LocomotionRuntimeModuleRestoreState(
            LocomotionFrameRuntimeState frameState,
            CharacterRuntimeBlackboardRestoreState blackboard,
            LocomotionRuntimeRollbackState rollbackState)
        {
            FrameState = frameState;
            Blackboard = blackboard;
            RollbackState = rollbackState;
        }

        public LocomotionFrameRuntimeState FrameState { get; }
        public CharacterRuntimeBlackboardRestoreState Blackboard { get; }
        public LocomotionRuntimeRollbackState RollbackState { get; }
    }

    public sealed class LocomotionRuntimeModule
    {
        readonly LocomotionRuntimeStateStore stateStore = new LocomotionRuntimeStateStore();
        readonly CharacterRuntimeBlackboard runtimeBlackboard = new CharacterRuntimeBlackboard();
        readonly RollbackCameraBasisProvider rollbackCameraBasisProvider = new RollbackCameraBasisProvider();
        ILocomotionRuntimeUnityAdapter unityAdapter;
        LocomotionFrameRuntimeAdapter frameRuntimeAdapter;
        LocomotionFrameRuntimeHost frameRuntimeHost;
        LocomotionOutputRuntimeAdapter outputRuntimeAdapter;
        LocomotionOutputRuntimeHost outputRuntimeHost;

        public LocomotionRuntimeModule()
        {
        }

        public LocomotionRuntimeModule(ILocomotionRuntimeUnityAdapter unityAdapter)
        {
            Bind(unityAdapter);
        }

        public BasicMovementPhase CurrentPhase => stateStore.CurrentPhase;
        public float CurrentPhaseTime => stateStore.CurrentPhaseTime;
        public string ActiveStatePath => stateStore.ActiveStatePath;
        public BasicMovementGait CurrentGait => stateStore.CurrentGait;
        public BasicMovementGait LastMovingGait => stateStore.LastMovingGait;
        public Vector3 CurrentWorldDirection => stateStore.CurrentWorldDirection;
        public MovementInputIntent CurrentIntent => stateStore.CurrentIntent;
        public BasicLocomotionFrame CurrentFrame => stateStore.CurrentFrame;
        public bool RunLatchActive => stateStore.RunLatchActive;
        public CharacterRuntimeBlackboardSnapshot RuntimeBlackboardSnapshot => runtimeBlackboard.Snapshot;
        public RollbackCameraBasisProvider RollbackCameraBasisProvider => rollbackCameraBasisProvider;
        public ILocomotionFrameRuntimePort FrameRuntimePort => frameRuntimeAdapter ?? CreateFrameRuntimeAdapter();
        public ILocomotionOutputRuntimePort OutputRuntimePort => outputRuntimeAdapter ?? CreateOutputRuntimeAdapter();

        internal LocomotionRuntimeStateStore StateStore => stateStore;
        internal CharacterRuntimeBlackboard RuntimeBlackboard => runtimeBlackboard;

        public void Bind(ILocomotionRuntimeUnityAdapter adapter)
        {
            if (ReferenceEquals(unityAdapter, adapter))
                return;

            unityAdapter = adapter;
            frameRuntimeAdapter = null;
            frameRuntimeHost = null;
            outputRuntimeAdapter = null;
            outputRuntimeHost = null;
        }

        public void ResetAfterLifecycleDisable()
        {
            stateStore.ResetAfterLifecycleDisable();
        }

        public void SetRunLatchActive(bool active)
        {
            stateStore.SetRunLatchActive(active);
        }

        public void WriteActionFacts(in CharacterRuntimeActionFacts facts)
        {
            runtimeBlackboard.WriteActionFacts(in facts);
        }

        public void WriteAnimationFacts(in CharacterRuntimeAnimationFacts facts)
        {
            runtimeBlackboard.WriteAnimationFacts(in facts);
        }

        public void WriteLocomotionPreemptionFact(in LocomotionPreemptionFact fact)
        {
            runtimeBlackboard.WriteLocomotionPreemptionFact(in fact);
        }

        public LocomotionRuntimeRollbackState CaptureRollbackState()
        {
            return stateStore.CaptureRollbackState();
        }

        public LocomotionRuntimeModuleRestoreState CaptureRestoreState()
        {
            return new LocomotionRuntimeModuleRestoreState(
                stateStore.CaptureFrameState(),
                runtimeBlackboard.CaptureRestoreState(),
                stateStore.CaptureRollbackState());
        }

        public void Restore(in LocomotionRuntimeModuleRestoreState state)
        {
            stateStore.ApplyFrameState(state.FrameState);
            runtimeBlackboard.Restore(state.Blackboard);
            LocomotionRuntimeRollbackState rollbackState = state.RollbackState;
            stateStore.RestoreRollbackState(in rollbackState);
        }

        public void RestoreBlackboard(in CharacterRuntimeBlackboardRestoreState state)
        {
            runtimeBlackboard.Restore(state);
        }

        public CharacterRuntimeBlackboardRestoreState CaptureBlackboard()
        {
            return runtimeBlackboard.CaptureRestoreState();
        }

        public void RestoreSnapshotHeader(
            bool runLatchActive,
            BasicMovementGait lastMovingGait,
            Vector3 currentWorldDirection,
            string activeStatePath)
        {
            stateStore.RestoreSnapshotHeader(
                runLatchActive,
                lastMovingGait,
                currentWorldDirection,
                activeStatePath);
        }

        public void RestoreRollbackState(in LocomotionRuntimeRollbackState state)
        {
            stateStore.RestoreRollbackState(in state);
        }

        public void RestoreFrame(in BasicLocomotionFrame frame, float phaseTime)
        {
            stateStore.RestoreFrame(in frame, phaseTime);
        }

        public bool HasPreviousMotionPlaybackProgress => stateStore.HasPreviousMotionPlaybackProgress;

        public void ResetMotionPlaybackWindow(BasicMovementPhase phase)
        {
            stateStore.ResetMotionPlaybackWindow(phase);
        }

        public void SeedMotionPlaybackWindow(in AnimationPhasePlaybackProgress progress, BasicMovementPhase phase)
        {
            stateStore.SeedMotionPlaybackWindow(in progress, phase);
        }

        LocomotionFrameRuntimeAdapter CreateFrameRuntimeAdapter()
        {
            frameRuntimeHost ??= new LocomotionFrameRuntimeHost(this);
            LocomotionPrepareFactsProvider prepareFactsProvider = new LocomotionPrepareFactsProvider(
                frameRuntimeHost,
                stateStore);
            LocomotionSpatialFactsProvider spatialFactsProvider = new LocomotionSpatialFactsProvider(frameRuntimeHost);
            LocomotionMotionFactsProvider motionFactsProvider = new LocomotionMotionFactsProvider(
                frameRuntimeHost,
                stateStore);
            LocomotionFrameRuntime runtime = new LocomotionFrameRuntime(
                new LocomotionFrameBuilder(),
                stateStore,
                prepareFactsProvider,
                spatialFactsProvider,
                motionFactsProvider,
                frameRuntimeHost);
            frameRuntimeAdapter = new LocomotionFrameRuntimeAdapter(
                runtime,
                stateStore,
                frameRuntimeHost);
            return frameRuntimeAdapter;
        }

        LocomotionOutputRuntimeAdapter CreateOutputRuntimeAdapter()
        {
            outputRuntimeHost ??= new LocomotionOutputRuntimeHost(this);
            LocomotionOutputRuntime runtime = new LocomotionOutputRuntime(
                new LocomotionMotionOutputApplier(outputRuntimeHost),
                new LocomotionAnimationOutputPresenter(outputRuntimeHost),
                new LocomotionRuntimeBlackboardWriter(outputRuntimeHost),
                new LocomotionOutputCompletion(outputRuntimeHost));
            outputRuntimeAdapter = new LocomotionOutputRuntimeAdapter(runtime);
            return outputRuntimeAdapter;
        }

        sealed class LocomotionOutputRuntimeHost :
            ILocomotionMotionOutputDependencies,
            ILocomotionAnimationOutputDependencies,
            ILocomotionRuntimeBlackboardDependencies,
            ILocomotionOutputCompletionDependencies
        {
            readonly LocomotionRuntimeModule module;

            public LocomotionOutputRuntimeHost(LocomotionRuntimeModule module)
            {
                this.module = module;
            }

            ILocomotionRuntimeUnityAdapter Adapter => module.unityAdapter;
            public IBasicLocomotionMotionExecutor MotionExecutor => Adapter?.MotionExecutor;
            public bool SuppressBasicMotionExecution => Adapter != null && Adapter.SuppressBasicMotionExecution;
            public bool SuppressLocomotionAnimationPresentation => Adapter != null && Adapter.SuppressLocomotionAnimationPresentation;
            public RunLocomotionAnimationConfigSO AnimationConfig => Adapter?.AnimationConfig;
            public CharacterRuntimeBlackboardSnapshot RuntimeBlackboardSnapshot => module.runtimeBlackboard.Snapshot;
            public BasicMovementGait CurrentGait => module.CurrentGait;
            public LocomotionRuntimeStateStore StateStore => module.stateStore;
            public bool HasCameraController => Adapter != null && Adapter.HasCameraController;
            public bool IsRollbackCameraBasisOverrideActive => module.rollbackCameraBasisProvider.UsingOverride;
            public string CurrentAnimationName => Adapter?.CurrentAnimationName ?? string.Empty;

            public void ResolveMotionExecutor()
            {
                Adapter?.ResolveMotionExecutor();
            }

            public void PresentAnimation(in MovementAnimationContext context)
            {
                Adapter?.PresentAnimation(in context);
            }

            public void WriteActionFactsToBlackboard(in CharacterRuntimeActionFacts facts)
            {
                module.runtimeBlackboard.WriteActionFacts(in facts);
            }

            public void WriteAnimationFactsToBlackboard(in CharacterRuntimeAnimationFacts facts)
            {
                module.runtimeBlackboard.WriteAnimationFacts(in facts);
            }

            public void WriteLocomotionPreemptionFactToBlackboard(in LocomotionPreemptionFact fact)
            {
                module.runtimeBlackboard.WriteLocomotionPreemptionFact(in fact);
            }

            public void ResolveCamera()
            {
                Adapter?.ResolveCamera();
            }

            public void SyncRollbackCameraBasis()
            {
                Adapter?.SyncRollbackCameraBasisFromCamera(module.rollbackCameraBasisProvider);
            }
        }

        sealed class LocomotionFrameRuntimeHost :
            ILocomotionPrepareFactsProviderHost,
            ILocomotionSpatialFactsProviderHost,
            ILocomotionFrameRuntimeOutputHost
        {
            readonly LocomotionRuntimeModule module;

            public LocomotionFrameRuntimeHost(LocomotionRuntimeModule module)
            {
                this.module = module;
            }

            ILocomotionRuntimeUnityAdapter Adapter => module.unityAdapter;
            public BasicMovementConfigSO MovementConfig => Adapter?.MovementConfig;
            public RunLocomotionAnimationConfigSO AnimationConfig => Adapter?.AnimationConfig;
            public CharacterRuntimeBlackboardSnapshot RuntimeBlackboardSnapshot => module.runtimeBlackboard.Snapshot;
            public string ActiveStatePath => module.stateStore.ActiveStatePath;
            public RollbackCameraBasisProvider RollbackCameraBasisProvider => module.rollbackCameraBasisProvider;
            public bool HasCameraController => Adapter != null && Adapter.HasCameraController;
            public Vector2 CameraLookSensitivity => Adapter != null ? Adapter.CameraLookSensitivity : new Vector2(0.12f, 0.12f);
            public float HostYaw => Adapter != null ? Adapter.HostYaw : 0f;
            public AnimationPhasePlaybackProgress CurrentAnimationPlaybackProgress =>
                Adapter != null ? Adapter.CurrentAnimationPlaybackProgress : AnimationPhasePlaybackProgress.Invalid(module.CurrentPhase);

            public void AdvanceAnimationPlaybackProgress(float deltaTime)
            {
                Adapter?.AdvanceAnimationPlaybackProgress(deltaTime);
            }

            public AnimationPhasePlaybackProgress ResolvePlaybackProgress(BasicMovementPhase phase)
            {
                return Adapter != null ? Adapter.ResolvePlaybackProgress(phase) : AnimationPhasePlaybackProgress.Invalid(phase);
            }

            public void SubmitFormalConfigMissing(string eventId, string message)
            {
                Adapter?.SubmitFormalConfigMissing(eventId, message);
            }

            public void ApplyCameraLook(Vector2 lookInput)
            {
                Adapter?.ApplyCameraLook(lookInput);
            }

            public void SyncRollbackCameraBasisFromCamera()
            {
                Adapter?.SyncRollbackCameraBasisFromCamera(module.rollbackCameraBasisProvider);
            }

            public void SyncRollbackCameraBasisWithoutCamera()
            {
                Adapter?.SyncRollbackCameraBasisWithoutCamera(module.rollbackCameraBasisProvider);
            }

            public Vector3 ResolveFacingForward()
            {
                return Adapter != null ? Adapter.ResolveFacingForward() : Vector3.forward;
            }

            public void LogCameraInput(Vector2 moveInput, Vector2 lookInput)
            {
                Adapter?.LogCameraInput(moveInput, lookInput);
            }

            public void WriteLocomotionFacts(in CharacterRuntimeLocomotionFacts facts)
            {
                module.runtimeBlackboard.WriteLocomotionFacts(in facts);
            }

            public void WriteLocomotionPreemptionFact(in LocomotionPreemptionFact fact)
            {
                module.runtimeBlackboard.WriteLocomotionPreemptionFact(in fact);
            }
        }
    }

    public interface ILocomotionRuntimeUnityAdapter
    {
        IBasicLocomotionMotionExecutor MotionExecutor { get; }
        BasicMovementConfigSO MovementConfig { get; }
        RunLocomotionAnimationConfigSO AnimationConfig { get; }
        bool SuppressBasicMotionExecution { get; }
        bool SuppressLocomotionAnimationPresentation { get; }
        bool HasCameraController { get; }
        Vector2 CameraLookSensitivity { get; }
        float HostYaw { get; }
        AnimationPhasePlaybackProgress CurrentAnimationPlaybackProgress { get; }
        string CurrentAnimationName { get; }

        void ResolveMotionExecutor();
        void ResolveCamera();
        void PresentAnimation(in MovementAnimationContext context);
        void AdvanceAnimationPlaybackProgress(float deltaTime);
        AnimationPhasePlaybackProgress ResolvePlaybackProgress(BasicMovementPhase phase);
        void SubmitFormalConfigMissing(string eventId, string message);
        void ApplyCameraLook(Vector2 lookInput);
        void SyncRollbackCameraBasisFromCamera(RollbackCameraBasisProvider basisProvider);
        void SyncRollbackCameraBasisWithoutCamera(RollbackCameraBasisProvider basisProvider);
        Vector3 ResolveFacingForward();
        void LogCameraInput(Vector2 moveInput, Vector2 lookInput);
    }
}
