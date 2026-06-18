using System;
using ThirdPersonAnimation;
using ThirdPersonCamera;
using ThirdPersonCharacterConfig;
using ThirdPersonCharacterStateMachine;
using ThirdPersonInput;
using ThirdPersonMovement;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonAction
{
    [DefaultExecutionOrder(34)]
    [DisallowMultipleComponent]
    public sealed class CharacterFrameRuntimeController :
        MonoBehaviour,
        ILocomotionRuntimeUnityAdapter,
        ICommittedActionRuntimeUnityAdapter
    {
        [SerializeField] CharacterConfigSO characterConfig;
        [SerializeField] InputRequestBufferComponent inputBufferComponent;
        [SerializeField] UnityInputSystemRequestBufferAdapter requestBufferAdapter;
        [SerializeField] MonoBehaviour inputSourceBehaviour;
        [SerializeField] MonoBehaviour motionExecutorBehaviour;
        [SerializeField] MonoBehaviour facingProviderBehaviour;
        [SerializeField] ThirdPersonCameraController cameraController;
        [SerializeField] MonoBehaviour locomotionPresenter;
        [SerializeField] MonoBehaviour actionMovementExecutorBehaviour;
        [SerializeField] MonoBehaviour animationPresenterBehaviour;
        [SerializeField] bool autoUpdate = true;
        [SerializeField] bool debugCameraLog = true;
        [SerializeField, Min(0f)] float debugCameraLogInterval = 0.1f;

        const float DirectionSqrEpsilon = 0.000001f;

        CharacterRuntimeCore runtimeCore;
        IBasicLocomotionInputSource inputSource;
        IBasicLocomotionMotionExecutor motionExecutor;
        IActionMovementExecutor actionMovementExecutor;
        IFacingDirectionProvider facingProvider;
        ILocomotionAnimationPresenter locomotionAnimationPresenter;
        ICharacterAnimationOutputPresenter animationPresenter;
        ILocomotionAnimationPlaybackProgressController playbackProgressController;
        IActionAnimationPlaybackProgressController actionPlaybackProgressController;
        bool previousCameraAutoTick;
        bool hasPreviousCameraAutoTick;
        bool suppressBasicMotionExecution;
        bool suppressLocomotionAnimationPresentation;
        float nextCameraDebugLogTime;

        public CharacterConfigSO CharacterConfig { get => characterConfig; set { characterConfig = value; ApplyFormalConfig(); UpdateRuntimeDependencies(); } }
        public InputRequestBufferComponent InputBufferComponent { get => inputBufferComponent; set { inputBufferComponent = value; UpdateRuntimeDependencies(); } }
        public UnityInputSystemRequestBufferAdapter RequestBufferAdapter { get => requestBufferAdapter; set { requestBufferAdapter = value; ApplyFormalConfig(); UpdateRuntimeDependencies(); } }
        public MonoBehaviour InputSourceBehaviour { get => inputSourceBehaviour; set { inputSourceBehaviour = value; inputSource = value as IBasicLocomotionInputSource; ApplyFormalConfig(); } }
        public MonoBehaviour MotionExecutorBehaviour { get => motionExecutorBehaviour; set { motionExecutorBehaviour = value; motionExecutor = value as IBasicLocomotionMotionExecutor; } }
        public MonoBehaviour FacingProviderBehaviour { get => facingProviderBehaviour; set { facingProviderBehaviour = value; facingProvider = value as IFacingDirectionProvider; } }
        public ThirdPersonCameraController CameraController { get => cameraController; set => cameraController = value; }
        public MonoBehaviour LocomotionPresenterBehaviour { get => locomotionPresenter; set => SetLocomotionPresenter(value); }
        public MonoBehaviour ActionMovementExecutorBehaviour { get => actionMovementExecutorBehaviour; set { actionMovementExecutorBehaviour = value; actionMovementExecutor = value as IActionMovementExecutor; } }
        public MonoBehaviour AnimationPresenterBehaviour { get => animationPresenterBehaviour; set { animationPresenterBehaviour = value; ResolveAnimationPresenter(); } }
        public bool AutoUpdate { get => autoUpdate; set => autoUpdate = value; }
        public bool SuppressBasicMotionExecution { get => suppressBasicMotionExecution; set => suppressBasicMotionExecution = value; }
        public bool SuppressLocomotionAnimationPresentation { get => suppressLocomotionAnimationPresentation; set => suppressLocomotionAnimationPresentation = value; }
        public CharacterRuntimeCore RuntimeCore => runtimeCore ?? (runtimeCore = CreateRuntimeCore());
        public LocomotionRuntimeModule LocomotionModule => RuntimeCore.LocomotionModule;
        public CommittedActionRuntimeModule CommittedActionModule => RuntimeCore.CommittedActionModule;
        public CharacterFrameResult LastFramePipelineResult => RuntimeCore.LastFramePipelineResult;
        public ICharacterFrameRuntimePort RuntimePort => RuntimeCore.RuntimePort;
        public AnimationPhasePlaybackProgress CurrentAnimationPlaybackProgress => ResolveCurrentAnimationPlaybackProgress();
        public string CurrentAnimationName => locomotionAnimationPresenter != null ? locomotionAnimationPresenter.CurrentAnimationName : string.Empty;
        public RollbackCameraBasisProvider RollbackCameraBasisProvider => LocomotionModule.RollbackCameraBasisProvider;
        public bool IsRollbackCameraBasisOverrideActive => LocomotionModule.RollbackCameraBasisProvider.UsingOverride;

        void Reset()
        {
            ResolveReferences();
            ApplyFormalConfig();
        }

        void OnEnable()
        {
            ResolveReferences();
            ApplyFormalConfig();
            UpdateRuntimeDependencies();
            EnableUnityAdapters();
        }

        void OnDisable()
        {
            RestoreUnityAdapters();
            LocomotionModule.ResetAfterLifecycleDisable();
            ResetMotionPlaybackWindow();
            CommittedActionModule.Reset();
        }

        void Update()
        {
            if (autoUpdate)
                Tick(Time.deltaTime);
        }

        public bool Tick(float deltaTime)
        {
            int step = inputBufferComponent != null ? inputBufferComponent.CurrentStep : Time.frameCount;
            if (!TryReadFrameInputFromSource(deltaTime, step, out CharacterFrameInput input))
                return false;

            return Tick(in input);
        }

        public bool Tick(in BasicLocomotionInputSnapshot input)
        {
            int step = inputBufferComponent != null ? inputBufferComponent.CurrentStep : Time.frameCount;
            CharacterFrameInput frameInput = CharacterFrameInput.FromLocomotionInput(step, in input);
            return Tick(in frameInput);
        }

        public bool Tick(in CharacterFrameInput input)
        {
            if (!PrepareFrameRuntimeAdapters())
                return false;

            return RuntimeCore.Tick(in input);
        }

        public CharacterFrameContext BeginFrame(in CharacterFrameInput input)
        {
            return RuntimeCore.BeginFrame(in input);
        }

        public bool RunPhase(
            SimulationTickPhase phase,
            ref CharacterFrameContext context,
            out CharacterFrameResult result)
        {
            if (!PrepareFrameRuntimeAdapters())
            {
                context.MarkFailed("runtime-not-ready");
                result = new CharacterFrameResult(in context);
                return false;
            }

            return RuntimeCore.RunPhase(phase, ref context, out result);
        }

        public bool TryReadFrameInputFromSource(float deltaTime, int step, out CharacterFrameInput input)
        {
            if (!PrepareFrameRuntimeAdapters())
            {
                input = default;
                return false;
            }

            LocomotionModule.RollbackCameraBasisProvider.ReleaseOverride();
            if (!TryReadInput(deltaTime, out BasicLocomotionInputSnapshot locomotionInput))
            {
                input = default;
                return false;
            }

            input = CharacterFrameInput.FromLocomotionInput(step, in locomotionInput);
            return true;
        }

        public bool TryReadInput(float deltaTime, out BasicLocomotionInputSnapshot input)
        {
            if (inputSource == null)
                ResolveInputSource();

            ApplyFormalConfig();

            if (inputSource == null)
            {
                input = default;
                return false;
            }

            input = inputSource.ReadInput(deltaTime);
            return true;
        }

        public bool PrepareFrameRuntimeAdapters()
        {
            ResolveReferences();
            ApplyFormalConfig();
            UpdateRuntimeDependencies();
            return RuntimeCore.PrepareFrameRuntimeAdapters();
        }

        public CharacterSimulationSnapshot CaptureSimulationSnapshot(SimulationTick tick)
        {
            ResolveReferences();
            UpdateRuntimeDependencies();
            CommittedActionModule.EnsureStateMachine(characterConfig != null ? characterConfig.StateMachine : null, true);

            if (motionExecutor == null)
                ResolveMotionExecutor();

            AnimationPhasePlaybackProgress progress = CurrentAnimationPlaybackProgress;
            RollbackCameraBasisState cameraBasisState = CaptureRollbackCameraBasisState();
            LocomotionRuntimeRollbackState locomotionRuntimeState = LocomotionModule.CaptureRollbackState();
            CommittedActionRestoreState committedActionState = CommittedActionModule.CaptureRestoreState();
            InputRequestBufferComponentRestoreState inputBufferState = inputBufferComponent != null
                ? inputBufferComponent.CaptureRestoreState()
                : InputRequestBufferComponentRestoreState.Empty;
            MotionExecutorRollbackState motionExecutorState = motionExecutor is IMotionExecutorRollbackStateProvider stateProvider
                ? stateProvider.CaptureRollbackState()
                : new MotionExecutorRollbackState(
                    motionExecutor != null ? motionExecutor.CurrentSpeed : 0f,
                    motionExecutor != null ? motionExecutor.LastWorldDirection : Vector3.zero,
                    0f);

            return new CharacterSimulationSnapshot(
                tick,
                transform.position,
                transform.eulerAngles.y,
                committedActionState.StateMachine,
                LocomotionModule.RunLatchActive,
                LocomotionModule.LastMovingGait,
                LocomotionModule.CurrentWorldDirection,
                LocomotionModule.CurrentPhase,
                LocomotionModule.CurrentGait,
                progress.AliasKey,
                progress.NormalizedTime,
                LocomotionModule.CaptureBlackboard(),
                committedActionState,
                inputBufferState,
                cameraBasisState.Yaw,
                cameraBasisState,
                locomotionRuntimeState,
                motionExecutorState);
        }

        public bool RestoreSimulationSnapshot(in CharacterSimulationSnapshot snapshot)
        {
            ResolveReferences();
            UpdateRuntimeDependencies();

            if (!CommittedActionModule.EnsureStateMachine(characterConfig != null ? characterConfig.StateMachine : null, true))
                return false;

            CommittedActionRestoreState committedActionRestoreState = snapshot.CommittedActionRestoreState;
            if (committedActionRestoreState.Snapshot.ActiveState.IsValid ||
                committedActionRestoreState.Gameplay.ActionLifecycle.HasActiveAction)
            {
                CommittedActionModule.Restore(in committedActionRestoreState);
            }

            if (inputBufferComponent != null)
                inputBufferComponent.Restore(snapshot.InputBufferRestoreState);

            LocomotionModule.RestoreBlackboard(snapshot.RuntimeBlackboardRestoreState);
            transform.SetPositionAndRotation(snapshot.Position, Quaternion.Euler(0f, snapshot.Yaw, 0f));
            LocomotionModule.RollbackCameraBasisProvider.Override(snapshot.CameraBasisState);
            LocomotionModule.RestoreSnapshotHeader(
                snapshot.RunLatchActive,
                snapshot.LastMovingGait,
                snapshot.CurrentWorldDirection,
                snapshot.CommittedActionRestoreState.Snapshot.ActivePath);
            if (motionExecutor == null)
                ResolveMotionExecutor();
            if (motionExecutor is IMotionExecutorRollbackStateProvider stateProvider)
                stateProvider.RestoreRollbackState(snapshot.MotionExecutorState);

            LocomotionRuntimeRollbackState locomotionState = snapshot.LocomotionRuntimeState;
            LocomotionModule.RestoreRollbackState(in locomotionState);
            BasicMovementConfigSO movementConfig = ResolveMovementConfig();
            if (movementConfig == null)
            {
                LogFormalConfigMissing("movement-config-missing", "CharacterConfigSO.Movement is missing. Locomotion snapshot cannot be restored.");
                return false;
            }

            BasicLocomotionFrame restoredFrame = new BasicLocomotionFrame(
                new BasicLocomotionInputSnapshot(0f, Vector2.zero, Vector2.zero, snapshot.RunLatchActive),
                BasicMovementSettings.FromConfig(movementConfig),
                LocomotionModule.CurrentIntent,
                LocomotionModule.CurrentWorldDirection,
                snapshot.LocomotionPhase,
                new MovementCommand(LocomotionModule.CurrentWorldDirection, 0f, 0f, 0f, snapshot.LocomotionPhase, snapshot.LocomotionGait, BasicMovementMotionFacts.None(snapshot.LocomotionPhase)));
            LocomotionModule.RestoreFrame(in restoredFrame, ResolveSnapshotPhaseTime(in snapshot));
            AnimationPhasePlaybackProgress restoredProgress = ResolveSnapshotAnimationPlaybackProgress(in snapshot);
            RestoreAnimationPlaybackProgress(in restoredProgress, snapshot.LocomotionGait);
            RestoreActionAnimationPlayback(
                snapshot.RuntimeBlackboard.Animation.ActionProgress,
                snapshot.RuntimeBlackboard.Animation.ActionAnimationName);
            return true;
        }

        public RollbackCameraBasisState CaptureRollbackCameraBasisState()
        {
            if (cameraController == null)
                ResolveCameraController();

            if (cameraController != null && !LocomotionModule.RollbackCameraBasisProvider.UsingOverride)
                cameraController.Resolve();

            LocomotionModule.RollbackCameraBasisProvider.SyncFrom(cameraController, ResolveCameraPlanarYaw());
            return new RollbackCameraBasisState(
                LocomotionModule.RollbackCameraBasisProvider.CameraPlanarForward,
                LocomotionModule.RollbackCameraBasisProvider.CameraPlanarRight,
                LocomotionModule.RollbackCameraBasisProvider.Yaw);
        }

        public void ReleaseRollbackCameraBasisOverride()
        {
            LocomotionModule.RollbackCameraBasisProvider.ReleaseOverride();
        }

        public void LogDiagnosticTickSnapshot(int step)
        {
            LocomotionDiagnostics.LogTickSnapshot(LocomotionModule.ActiveStatePath, step, BuildLocomotionDiagnosticContext());
        }

        CharacterRuntimeCore CreateRuntimeCore()
        {
            return new CharacterRuntimeCore(CreateDependencies());
        }

        CharacterRuntimeCoreDependencies CreateDependencies()
        {
            return new CharacterRuntimeCoreDependencies(
                characterConfig,
                inputBufferComponent,
                this,
                this,
                requestBufferAdapter);
        }

        void UpdateRuntimeDependencies()
        {
            if (runtimeCore != null)
                runtimeCore.UpdateDependencies(CreateDependencies());
        }

        void ResolveReferences()
        {
            if (inputBufferComponent == null)
            {
                inputBufferComponent = GetComponent<InputRequestBufferComponent>();
                if (inputBufferComponent == null)
                    inputBufferComponent = GetComponentInParent<InputRequestBufferComponent>();
                if (inputBufferComponent == null)
                    inputBufferComponent = GetComponentInChildren<InputRequestBufferComponent>(true);
            }

            if (requestBufferAdapter == null)
            {
                requestBufferAdapter = GetComponent<UnityInputSystemRequestBufferAdapter>();
                if (requestBufferAdapter == null)
                    requestBufferAdapter = GetComponentInParent<UnityInputSystemRequestBufferAdapter>();
                if (requestBufferAdapter == null)
                    requestBufferAdapter = GetComponentInChildren<UnityInputSystemRequestBufferAdapter>(true);
            }

            ResolveInputSource();
            ResolveMotionExecutor();
            ResolveFacingProvider();
            ResolveLocomotionPresenter();
            ResolveActionMovementExecutor();
            ResolveAnimationPresenter();
            ResolveCameraController();
        }

        void ApplyFormalConfig()
        {
            if (characterConfig == null)
                return;

            if (requestBufferAdapter != null)
                requestBufferAdapter.ApplyFormalInputConfig(characterConfig);

            if (inputSource is IFormalLocomotionInputConfigReceiver receiver)
                receiver.ApplyFormalInputConfig(characterConfig);
        }

        void EnableUnityAdapters()
        {
            if (inputSource != null)
                inputSource.SetInputEnabled(true);

            if (cameraController != null)
            {
                previousCameraAutoTick = cameraController.AutoTick;
                hasPreviousCameraAutoTick = true;
                cameraController.AutoTick = false;
            }
        }

        void RestoreUnityAdapters()
        {
            if (cameraController != null && hasPreviousCameraAutoTick)
            {
                cameraController.AutoTick = previousCameraAutoTick;
                hasPreviousCameraAutoTick = false;
            }

            if (inputSource != null)
                inputSource.SetInputEnabled(false);
        }

        void ResolveInputSource()
        {
            if (inputSourceBehaviour != null)
            {
                inputSource = inputSourceBehaviour as IBasicLocomotionInputSource;
                if (inputSource != null)
                    return;
            }
            else if (inputSource != null)
            {
                return;
            }

            if (LocomotionRuntimeReferenceResolver.TryResolveComponentInterface(this, out inputSource, out MonoBehaviour sourceBehaviour))
                inputSourceBehaviour = sourceBehaviour;
        }

        void ResolveMotionExecutor()
        {
            if (motionExecutorBehaviour != null)
            {
                motionExecutor = motionExecutorBehaviour as IBasicLocomotionMotionExecutor;
                if (motionExecutor != null)
                    return;
            }
            else if (motionExecutor != null)
            {
                return;
            }

            if (LocomotionRuntimeReferenceResolver.TryResolveComponentInterface(this, out motionExecutor, out MonoBehaviour executorBehaviour))
                motionExecutorBehaviour = executorBehaviour;
        }

        void ResolveActionMovementExecutor()
        {
            if (actionMovementExecutorBehaviour != null)
            {
                actionMovementExecutor = actionMovementExecutorBehaviour as IActionMovementExecutor;
                if (actionMovementExecutor != null)
                    return;
            }
            else if (actionMovementExecutor != null)
            {
                return;
            }

            if (motionExecutor is IActionMovementExecutor motionActionExecutor)
            {
                actionMovementExecutor = motionActionExecutor;
                actionMovementExecutorBehaviour = motionExecutorBehaviour;
                return;
            }

            if (CharacterRuntimeReferenceResolver.TryResolveComponentInterface(this, out IActionMovementExecutor resolvedExecutor, out MonoBehaviour executorBehaviour))
            {
                actionMovementExecutor = resolvedExecutor;
                actionMovementExecutorBehaviour = executorBehaviour;
            }
        }

        void ResolveFacingProvider()
        {
            facingProvider = LocomotionRuntimeReferenceResolver.ResolveFacingProvider(this, facingProviderBehaviour, out facingProviderBehaviour);
        }

        void ResolveLocomotionPresenter()
        {
            if (locomotionPresenter != null)
            {
                locomotionAnimationPresenter = locomotionPresenter as ILocomotionAnimationPresenter;
                if (locomotionAnimationPresenter != null)
                {
                    if (playbackProgressController == null)
                        playbackProgressController = locomotionAnimationPresenter;
                    return;
                }
            }

            locomotionAnimationPresenter = LocomotionRuntimeReferenceResolver.ResolveLocomotionPresenter(
                this,
                out MonoBehaviour presenterBehaviour,
                out playbackProgressController);
            locomotionPresenter = presenterBehaviour;
        }

        void ResolveAnimationPresenter()
        {
            if (animationPresenterBehaviour == null && locomotionPresenter is ICharacterAnimationOutputPresenter)
                animationPresenterBehaviour = locomotionPresenter;

            if (animationPresenterBehaviour == null &&
                CharacterRuntimeReferenceResolver.TryResolveComponentInterface(this, out ICharacterAnimationOutputPresenter _, out MonoBehaviour presenterBehaviour))
            {
                animationPresenterBehaviour = presenterBehaviour;
            }

            animationPresenter = animationPresenterBehaviour as ICharacterAnimationOutputPresenter;
            actionPlaybackProgressController = animationPresenterBehaviour as IActionAnimationPlaybackProgressController;
        }

        void ResolveCameraController()
        {
            if (cameraController == null)
                cameraController = LocomotionRuntimeReferenceResolver.ResolveCameraController(this);
        }

        void SetLocomotionPresenter(MonoBehaviour presenter)
        {
            locomotionPresenter = presenter;
            locomotionAnimationPresenter = presenter as ILocomotionAnimationPresenter;
            playbackProgressController = presenter as ILocomotionAnimationPlaybackProgressController;
            if (animationPresenterBehaviour == null && presenter is ICharacterAnimationOutputPresenter)
                AnimationPresenterBehaviour = presenter;
        }

        RunLocomotionAnimationConfigSO ResolveRunAnimationConfig()
        {
            return characterConfig != null ? characterConfig.LocomotionAnimation : null;
        }

        BasicMovementConfigSO ResolveMovementConfig()
        {
            return characterConfig != null ? characterConfig.Movement : null;
        }

        AnimationPhasePlaybackProgress ResolvePlaybackProgress(BasicMovementPhase phase)
        {
            IAnimationPhasePlaybackProgressSource source = locomotionAnimationPresenter;
            return source != null ? source.CurrentPlaybackProgress : AnimationPhasePlaybackProgress.Invalid(phase);
        }

        AnimationPhasePlaybackProgress ResolveCurrentAnimationPlaybackProgress()
        {
            IAnimationPhasePlaybackProgressSource source = locomotionAnimationPresenter;
            if (source == null)
                return AnimationPhasePlaybackProgress.Invalid(LocomotionModule.CurrentPhase);

            AnimationPhasePlaybackProgress progress = source.CurrentPlaybackProgress;
            return progress.HasValidPlayback ? progress : AnimationPhasePlaybackProgress.Invalid(LocomotionModule.CurrentPhase);
        }

        void RestoreAnimationPlaybackProgress(in AnimationPhasePlaybackProgress progress, BasicMovementGait gait)
        {
            ILocomotionAnimationPlaybackProgressController controller = ResolvePlaybackProgressController();
            if (controller != null)
                controller.RestorePlaybackProgress(in progress, gait);
        }

        void RestoreActionAnimationPlayback(in ActionAnimationPlaybackProgress progress, string animationName)
        {
            ResolveAnimationPresenter();
            if (actionPlaybackProgressController != null)
                actionPlaybackProgressController.RestorePlaybackProgress(in progress, animationName);
            else if (!progress.HasValidPlayback && animationPresenter != null)
                animationPresenter.ClearActionPlayback();
        }

        void AdvanceAnimationPlaybackProgress(float deltaTime)
        {
            ILocomotionAnimationPlaybackProgressController controller = ResolvePlaybackProgressController();
            if (controller != null)
                controller.AdvancePlayback(deltaTime);
        }

        ILocomotionAnimationPlaybackProgressController ResolvePlaybackProgressController()
        {
            if (playbackProgressController != null)
                return playbackProgressController;

            if (locomotionAnimationPresenter == null)
                ResolveLocomotionPresenter();

            playbackProgressController = locomotionAnimationPresenter;
            return playbackProgressController;
        }

        void ResetMotionPlaybackWindow()
        {
            LocomotionModule.ResetMotionPlaybackWindow(LocomotionModule.CurrentPhase);
        }

        float ResolveCameraPlanarYaw()
        {
            if (cameraController == null)
                ResolveCameraController();

            if (cameraController == null)
                return 0f;

            Vector3 forward = cameraController.CameraPlanarForward;
            forward.y = 0f;
            if (forward.sqrMagnitude <= DirectionSqrEpsilon)
                return cameraController.Yaw;

            forward.Normalize();
            return Mathf.Repeat(Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg, 360f);
        }

        Vector3 ResolveFacingForward()
        {
            if (facingProvider == null)
                ResolveFacingProvider();

            Vector3 forward = facingProvider != null ? facingProvider.FacingForward : transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude <= DirectionSqrEpsilon)
                return Vector3.forward;

            return forward.normalized;
        }

        void LogCameraInput(Vector2 moveInput, Vector2 lookInput)
        {
            if (!ShouldLogCamera(lookInput.sqrMagnitude > 0.000001f))
                return;

            LocomotionDiagnostics.LogCameraInput(
                name,
                moveInput,
                lookInput,
                CameraName(),
                cameraController != null ? cameraController.AutoTick.ToString() : "null",
                transform.position);
        }

        bool ShouldLogCamera(bool force)
        {
            if (!debugCameraLog)
                return false;

            if (debugCameraLogInterval <= 0f)
                return true;

            float now = Time.unscaledTime;
            if (!force && now < nextCameraDebugLogTime)
                return false;

            nextCameraDebugLogTime = now + debugCameraLogInterval;
            return true;
        }

        string CameraName()
        {
            return cameraController != null ? cameraController.name : "null";
        }

        string BuildLocomotionDiagnosticContext()
        {
            AnimationPhasePlaybackProgress progress = ResolvePlaybackProgress(LocomotionModule.CurrentPhase);
            string presenterName = locomotionPresenter != null ? locomotionPresenter.name : "null";
            string animationName = locomotionAnimationPresenter != null ? locomotionAnimationPresenter.CurrentAnimationName : string.Empty;
            BasicLocomotionFrame currentFrame = LocomotionModule.CurrentFrame;
            MovementInputIntent currentIntent = LocomotionModule.CurrentIntent;

            return
                $"phase={LocomotionModule.CurrentPhase} gait={currentFrame.Command.Gait} phaseTime={LocomotionModule.CurrentPhaseTime:F3} " +
                $"hasMove={currentIntent.HasMoveIntent} strength={currentIntent.Strength:F3} " +
                $"rawMove={currentIntent.RawInput.ToString("F3")} normalizedMove={currentIntent.NormalizedInput.ToString("F3")} " +
                $"worldDirection={LocomotionModule.CurrentWorldDirection.ToString("F3")} planarSpeed={currentFrame.Command.PlanarSpeed:F3} rotationSpeed={currentFrame.Command.RotationSpeed:F3} " +
                $"runLatch={LocomotionModule.RunLatchActive} motionSuppressed={suppressBasicMotionExecution} animationSuppressed={suppressLocomotionAnimationPresentation} " +
                $"hasAnimationMotion={currentFrame.Command.HasAnimationMotion} animMotionSourcePhase={currentFrame.Command.AnimationMotionSourcePhase} animMotionAlias={currentFrame.Command.AnimationMotionSourceAliasKey} " +
                $"animationPresenter={presenterName} animationPhase={progress.Phase} animationAlias={progress.AliasKey} animationName={animationName} " +
                $"animationNormalized={progress.NormalizedTime:F3} animationValid={progress.HasValidPlayback} animationEnded={progress.IsEnded}";
        }

        void LogFormalConfigMissing(string eventId, string message)
        {
            LocomotionDiagnostics.SubmitFormalConfigMissing(LocomotionModule.ActiveStatePath, eventId, message);
        }

        static float ResolveSnapshotPhaseTime(in CharacterSimulationSnapshot snapshot)
        {
            CharacterStateMachineSnapshot committedAction = snapshot.CommittedActionRestoreState.Snapshot;
            return committedAction.ActiveState.IsValid ? committedAction.StateTime : snapshot.StateMachine.StateTime;
        }

        static AnimationPhasePlaybackProgress ResolveSnapshotAnimationPlaybackProgress(in CharacterSimulationSnapshot snapshot)
        {
            AnimationPhasePlaybackProgress blackboardProgress = snapshot.RuntimeBlackboard.Animation.LocomotionProgress;
            if (blackboardProgress.HasValidPlayback && !string.IsNullOrWhiteSpace(blackboardProgress.AliasKey))
                return blackboardProgress;

            return new AnimationPhasePlaybackProgress(
                snapshot.LocomotionPhase,
                snapshot.AnimationKey,
                snapshot.AnimationNormalizedTime,
                !string.IsNullOrWhiteSpace(snapshot.AnimationKey),
                blackboardProgress.IsEnded);
        }

        IBasicLocomotionMotionExecutor ILocomotionRuntimeUnityAdapter.MotionExecutor => motionExecutor;
        BasicMovementConfigSO ILocomotionRuntimeUnityAdapter.MovementConfig => ResolveMovementConfig();
        RunLocomotionAnimationConfigSO ILocomotionRuntimeUnityAdapter.AnimationConfig => ResolveRunAnimationConfig();
        bool ILocomotionRuntimeUnityAdapter.SuppressBasicMotionExecution => suppressBasicMotionExecution;
        bool ILocomotionRuntimeUnityAdapter.SuppressLocomotionAnimationPresentation => suppressLocomotionAnimationPresentation;
        bool ILocomotionRuntimeUnityAdapter.HasCameraController => cameraController != null;
        Vector2 ILocomotionRuntimeUnityAdapter.CameraLookSensitivity => cameraController != null ? cameraController.Sensitivity : new Vector2(0.12f, 0.12f);
        float ILocomotionRuntimeUnityAdapter.HostYaw => transform.eulerAngles.y;
        AnimationPhasePlaybackProgress ILocomotionRuntimeUnityAdapter.CurrentAnimationPlaybackProgress => ResolveCurrentAnimationPlaybackProgress();
        string ILocomotionRuntimeUnityAdapter.CurrentAnimationName => CurrentAnimationName;
        void ILocomotionRuntimeUnityAdapter.ResolveMotionExecutor() => ResolveMotionExecutor();
        void ILocomotionRuntimeUnityAdapter.ResolveCamera() { if (cameraController != null) cameraController.Resolve(); }
        void ILocomotionRuntimeUnityAdapter.AdvanceAnimationPlaybackProgress(float deltaTime) => AdvanceAnimationPlaybackProgress(deltaTime);
        AnimationPhasePlaybackProgress ILocomotionRuntimeUnityAdapter.ResolvePlaybackProgress(BasicMovementPhase phase) => ResolvePlaybackProgress(phase);
        void ILocomotionRuntimeUnityAdapter.SubmitFormalConfigMissing(string eventId, string message) => LogFormalConfigMissing(eventId, message);
        void ILocomotionRuntimeUnityAdapter.ApplyCameraLook(Vector2 lookInput) { if (cameraController != null) cameraController.ApplyLook(lookInput); }
        void ILocomotionRuntimeUnityAdapter.SyncRollbackCameraBasisFromCamera(RollbackCameraBasisProvider basisProvider) => basisProvider.SyncFrom(cameraController, ResolveCameraPlanarYaw());
        void ILocomotionRuntimeUnityAdapter.SyncRollbackCameraBasisWithoutCamera(RollbackCameraBasisProvider basisProvider) => basisProvider.SyncFrom(null, ResolveCameraPlanarYaw());
        Vector3 ILocomotionRuntimeUnityAdapter.ResolveFacingForward() => ResolveFacingForward();
        void ILocomotionRuntimeUnityAdapter.LogCameraInput(Vector2 moveInput, Vector2 lookInput) => LogCameraInput(moveInput, lookInput);
        void ILocomotionRuntimeUnityAdapter.PresentAnimation(in MovementAnimationContext context)
        {
            if (locomotionAnimationPresenter == null)
                ResolveLocomotionPresenter();

            locomotionAnimationPresenter?.Present(in context);
        }

        InputRequestBufferComponent ICommittedActionRuntimeUnityAdapter.InputBufferComponent => inputBufferComponent;
        ILocomotionOutputRuntimePort ICommittedActionRuntimeUnityAdapter.LocomotionOutputRuntime => RuntimeCore.LocomotionOutputRuntime;
        AnimationPhasePlaybackProgress ICommittedActionRuntimeUnityAdapter.LocomotionAnimationPlaybackProgress => ResolveCurrentAnimationPlaybackProgress();
        string ICommittedActionRuntimeUnityAdapter.LocomotionAnimationName => CurrentAnimationName;
        IActionMovementExecutor ICommittedActionRuntimeUnityAdapter.ActionMovementExecutor => actionMovementExecutor;
        ICharacterAnimationOutputPresenter ICommittedActionRuntimeUnityAdapter.AnimationPresenter => animationPresenter;
        void ICommittedActionRuntimeUnityAdapter.LogLocomotionDiagnosticTickSnapshot(int step) => LogDiagnosticTickSnapshot(step);
    }
}
