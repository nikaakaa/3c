using ThirdPersonAnimation;
using ThirdPersonCamera;
using ThirdPersonCharacterConfig;
using ThirdPersonCharacterStateMachine;
using ThirdPersonInput;
using ThirdPersonSimulation;
using UnityEngine;
using UnityEngine.Serialization;

namespace ThirdPersonMovement
{
    [DisallowMultipleComponent]
    public sealed class PlayerLocomotionController : MonoBehaviour, ILocomotionFrameRuntimePort, ILocomotionOutputRuntimePort
    {
        [SerializeField] MonoBehaviour inputSourceBehaviour;
        [FormerlySerializedAs("motionDriver")]
        [SerializeField] MonoBehaviour motionExecutorBehaviour;
        [SerializeField] MonoBehaviour facingProviderBehaviour;
        [SerializeField] ThirdPersonCameraController cameraController;
        [SerializeField] BasicLocomotionAnimancerPresenter locomotionPresenter;
        [SerializeField] CharacterConfigSO characterConfig;
        [System.Obsolete("Legacy serialized field; runtime reads CharacterConfigSO only.")]
        [SerializeField] RunLocomotionAnimationConfigSO runAnimationConfig;
        [System.Obsolete("Legacy serialized field; runtime reads CharacterConfigSO only.")]
        [SerializeField] BasicMovementConfigSO config;
        [System.Obsolete("Legacy serialized field; runtime reads CharacterConfigSO only.")]
        [SerializeField] CharacterStateMachineDefinitionSO stateMachineDefinition;
        [SerializeField] bool autoUpdate = true;
        [SerializeField] bool debugCameraLog = true;
        [SerializeField, Min(0f)] float debugCameraLogInterval = 0.1f;

        const float DirectionSqrEpsilon = 0.000001f;

        readonly LocomotionRuntimeStateStore runtimeStateStore = new LocomotionRuntimeStateStore();
        readonly CharacterRuntimeBlackboard runtimeBlackboard = new CharacterRuntimeBlackboard();
        IBasicLocomotionInputSource inputSource;
        IBasicLocomotionMotionExecutor motionExecutor;
        IFacingDirectionProvider facingProvider;
        IAnimationPhasePlaybackProgressSource playbackProgressSource;
        ILocomotionAnimationPlaybackProgressController playbackProgressController;
        bool previousCameraAutoTick;
        bool hasPreviousCameraAutoTick;
        float nextCameraDebugLogTime;
        bool suppressBasicMotionExecution;
        bool suppressLocomotionAnimationPresentation;
        int localDecisionStep;
        bool loggedRetiredDirectTick;
        LocomotionFrameRuntimeAdapter frameRuntimeAdapter;
        LocomotionFrameRuntimeHost frameRuntimeHost;
        LocomotionOutputRuntimeAdapter outputRuntimeAdapter;
        LocomotionOutputRuntimeHost outputRuntimeHost;
        readonly RollbackCameraBasisProvider rollbackCameraBasisProvider = new RollbackCameraBasisProvider();

        public BasicMovementPhase CurrentPhase => runtimeStateStore.CurrentPhase;
        public float CurrentPhaseTime => runtimeStateStore.CurrentPhaseTime;
        public string ActiveStatePath => runtimeStateStore.ActiveStatePath;
        public BasicMovementGait CurrentGait => runtimeStateStore.CurrentGait;
        public Vector3 CurrentWorldDirection => runtimeStateStore.CurrentWorldDirection;
        public MovementInputIntent CurrentIntent => runtimeStateStore.CurrentIntent;
        public BasicLocomotionFrame CurrentFrame => runtimeStateStore.CurrentFrame;
        public AnimationPhasePlaybackProgress CurrentAnimationPlaybackProgress => ResolveCurrentAnimationPlaybackProgress();
        public string CurrentAnimationName => locomotionPresenter != null ? locomotionPresenter.CurrentAnimationName : string.Empty;
        public bool RunLatchActive => FrameRuntimePort.RunLatchActive;
        public RollbackCameraBasisProvider RollbackCameraBasisProvider => rollbackCameraBasisProvider;
        public bool IsRollbackCameraBasisOverrideActive => rollbackCameraBasisProvider.UsingOverride;
        public MonoBehaviour InputSourceBehaviour { get => inputSourceBehaviour; set => inputSourceBehaviour = value; }
        public MonoBehaviour MotionExecutorBehaviour { get => motionExecutorBehaviour; set => motionExecutorBehaviour = value; }
        public MonoBehaviour FacingProviderBehaviour { get => facingProviderBehaviour; set { facingProviderBehaviour = value; facingProvider = value as IFacingDirectionProvider; } }
        public ThirdPersonCameraController CameraController { get => cameraController; set => cameraController = value; }
        public BasicLocomotionAnimancerPresenter LocomotionPresenter
        {
            get => locomotionPresenter;
            set
            {
                locomotionPresenter = value;
                playbackProgressController = value as ILocomotionAnimationPlaybackProgressController;
            }
        }
        public CharacterConfigSO CharacterConfig { get => characterConfig; set => characterConfig = value; }
        public RunLocomotionAnimationConfigSO RunAnimationConfig { get => ResolveRunAnimationConfig(); set { } }
        public CharacterRuntimeBlackboardSnapshot RuntimeBlackboardSnapshot => FrameRuntimePort.RuntimeBlackboardSnapshot;
        public BasicMovementConfigSO Config { get => ResolveMovementConfig(); set { } }
        public CharacterStateMachineDefinitionSO StateMachineDefinition { get => ResolveStateMachineDefinition(); set => SetStateMachineDefinition(value); }
        public bool AutoUpdate { get => autoUpdate; set => autoUpdate = value; }
        public bool SuppressBasicMotionExecution { get => suppressBasicMotionExecution; set => suppressBasicMotionExecution = value; }
        public bool SuppressLocomotionAnimationPresentation { get => suppressLocomotionAnimationPresentation; set => suppressLocomotionAnimationPresentation = value; }
        internal ILocomotionFrameRuntimePort FrameRuntimePort => frameRuntimeAdapter ?? CreateFrameRuntimeAdapter();
        internal ILocomotionOutputRuntimePort OutputRuntimePort => outputRuntimeAdapter ?? CreateOutputRuntimeAdapter();
        public void ReleaseRollbackCameraBasisOverride() => rollbackCameraBasisProvider.ReleaseOverride();

        void Reset()
        {
            ResolveInputSource();
            ResolveMotionExecutor();
            ResolveFacingProvider();
            ResolveLocomotionPresenter();
        }

        void OnEnable()
        {
            ResolveInputSource();
            ResolveMotionExecutor();
            ResolveFacingProvider();
            ResolveLocomotionPresenter();

            if (HasEnabledLegacyPlayer())
            {
                LocomotionDiagnostics.SubmitLegacyPlayerEnabled();
                enabled = false;
                return;
            }

            if (inputSource == null)
            {
                LocomotionDiagnostics.SubmitInputSourceMissing();
                enabled = false;
                return;
            }

            if (motionExecutor == null)
            {
                LocomotionDiagnostics.SubmitMotionExecutorMissing();
                enabled = false;
                return;
            }

            inputSource.SetInputEnabled(true);

            if (cameraController != null)
            {
                previousCameraAutoTick = cameraController.AutoTick;
                hasPreviousCameraAutoTick = true;
                cameraController.AutoTick = false;
            }
        }

        void OnDisable()
        {
            if (cameraController != null && hasPreviousCameraAutoTick)
            {
                cameraController.AutoTick = previousCameraAutoTick;
                hasPreviousCameraAutoTick = false;
            }

            if (inputSource != null)
                inputSource.SetInputEnabled(false);

            runtimeStateStore.ResetAfterLifecycleDisable();
            ResetMotionPlaybackWindow();
        }

        void Update()
        {
            if (!autoUpdate)
                return;

            TickFromInputSource(Time.deltaTime);
        }

        public bool TickFromInputSource(float deltaTime)
        {
            return TickFromInputSource(deltaTime, 0);
        }

        public bool TickFromInputSource(float deltaTime, int diagnosticStep)
        {
            LogRetiredDirectTick(diagnosticStep);
            return false;
        }

        public void Tick(in BasicLocomotionInputSnapshot input)
        {
            Tick(in input, 0);
        }

        public void Tick(in BasicLocomotionInputSnapshot input, int diagnosticStep)
        {
            LogRetiredDirectTick(diagnosticStep);
        }

        public bool TryReadInput(float deltaTime, out BasicLocomotionInputSnapshot input)
        {
            if (inputSource == null)
                ResolveInputSource();

            if (inputSource == null)
            {
                input = default;
                return false;
            }

            input = inputSource.ReadInput(deltaTime);
            return true;
        }

        public bool TryEvaluateLocomotion(in BasicLocomotionInputSnapshot input, out BasicLocomotionFrame frame)
        {
            LogRetiredDirectTick(0);
            frame = default;
            return false;
        }

        public bool TryEvaluateWithStateMachine(
            in BasicLocomotionInputSnapshot input,
            CharacterStateMachineRunner runner,
            in CharacterInputRequestFact inputRequest,
            int currentStep,
            out BasicLocomotionFrame frame,
            out CharacterStateMachineFrame stateFrame)
        {
            if (runner == null)
            {
                frame = default;
                stateFrame = default;
                return false;
            }

            if (cameraController == null)
                ResolveCameraController();

            int decisionStep = ResolveDecisionStep(currentStep);
            if (!TryPrepareDecisionFrame(in input, runner, decisionStep, out LocomotionDecisionFrame decisionFrame))
            {
                frame = default;
                stateFrame = default;
                return false;
            }

            return TryEvaluatePreparedWithStateMachine(
                in decisionFrame,
                runner,
                in inputRequest,
                decisionStep,
                out frame,
                out stateFrame);
        }

        public bool TryEvaluatePreparedWithStateMachine(
            in LocomotionDecisionFrame decisionFrame,
            CharacterStateMachineRunner runner,
            in CharacterInputRequestFact inputRequest,
            int currentStep,
            out BasicLocomotionFrame frame,
            out CharacterStateMachineFrame stateFrame)
        {
            if (!TryEvaluatePreparedGameplayDecision(
                    in decisionFrame,
                    runner,
                    in inputRequest,
                    currentStep,
                    out LocomotionStateDecisionFrame stateDecision))
            {
                frame = default;
                stateFrame = default;
                return false;
            }

            return TryBuildMotionFromStateDecision(
                in stateDecision,
                currentStep,
                out frame,
                out stateFrame);
        }

        public bool TryEvaluatePreparedGameplayDecision(
            in LocomotionDecisionFrame decisionFrame,
            CharacterStateMachineRunner runner,
            in CharacterInputRequestFact inputRequest,
            StateTimelineWindowFacts currentTimelineFacts,
            int currentStep,
            out LocomotionStateDecisionFrame stateDecision)
        {
            return FrameRuntimePort.TryEvaluatePreparedGameplayDecision(
                in decisionFrame,
                runner,
                in inputRequest,
                currentTimelineFacts,
                currentStep,
                out stateDecision);
        }

        public bool TryEvaluatePreparedGameplayDecision(
            in LocomotionDecisionFrame decisionFrame,
            CharacterStateMachineRunner runner,
            in CharacterInputRequestFact inputRequest,
            int currentStep,
            out LocomotionStateDecisionFrame stateDecision)
        {
            return (frameRuntimeAdapter ?? CreateFrameRuntimeAdapter()).TryEvaluatePreparedGameplayDecision(
                in decisionFrame,
                runner,
                in inputRequest,
                currentStep,
                out stateDecision);
        }

        public bool TryBuildMotionFromStateDecision(
            in LocomotionStateDecisionFrame stateDecision,
            int currentStep,
            out BasicLocomotionFrame frame,
            out CharacterStateMachineFrame stateFrame)
        {
            return FrameRuntimePort.TryBuildMotionFromStateDecision(
                in stateDecision,
                currentStep,
                out frame,
                out stateFrame);
        }

        public void ExecuteLocomotionMotion(in BasicLocomotionFrame frame)
        {
            OutputRuntimePort.ExecuteLocomotionMotion(in frame);
        }

        public void PresentLocomotionAnimation(in BasicLocomotionFrame frame)
        {
            OutputRuntimePort.PresentLocomotionAnimation(in frame);
        }

        public void CompleteLocomotionTick()
        {
            OutputRuntimePort.CompleteLocomotionTick();
        }

        public RollbackCameraBasisState CaptureRollbackCameraBasisState()
        {
            if (cameraController == null)
                ResolveCameraController();

            if (cameraController != null && !rollbackCameraBasisProvider.UsingOverride)
                cameraController.Resolve();

            rollbackCameraBasisProvider.SyncFrom(cameraController, ResolveCameraPlanarYaw());
            return new RollbackCameraBasisState(
                rollbackCameraBasisProvider.CameraPlanarForward,
                rollbackCameraBasisProvider.CameraPlanarRight,
                rollbackCameraBasisProvider.Yaw);
        }

        public void SetStateMachineDefinition(CharacterStateMachineDefinitionSO definition)
        {
            runtimeStateStore.ResetForStateMachineDefinition();
            ResetMotionPlaybackWindow();
        }

        public void SetRunLatchActive(bool active)
        {
            runtimeStateStore.SetRunLatchActive(active);
        }

        public bool TryPrepareDecisionFrame(
            in BasicLocomotionInputSnapshot input,
            CharacterStateMachineRunner runner,
            int currentStep,
            out LocomotionDecisionFrame decisionFrame)
        {
            return FrameRuntimePort.TryPrepareDecisionFrame(
                in input,
                runner,
                currentStep,
                out decisionFrame);
        }

        int ResolveDecisionStep(int currentStep)
        {
            if (currentStep > 0)
            {
                localDecisionStep = currentStep;
                return currentStep;
            }

            localDecisionStep++;
            return localDecisionStep;
        }

        public void WriteActionFacts(in CharacterRuntimeActionFacts facts)
        {
            OutputRuntimePort.WriteActionFacts(in facts);
        }

        public void WriteAnimationFacts(in CharacterRuntimeAnimationFacts facts)
        {
            OutputRuntimePort.WriteAnimationFacts(in facts);
        }

        public CharacterSimulationSnapshot CaptureSimulationSnapshot(SimulationTick tick)
        {
            if (motionExecutor == null)
                ResolveMotionExecutor();
            AnimationPhasePlaybackProgress progress = CurrentAnimationPlaybackProgress;
            CharacterStateMachineRestoreState stateRestore = new CharacterStateMachineRestoreState(
                CharacterStateMachineSnapshot.Inactive,
                Vector3.zero,
                false,
                false,
                false,
                false);
            RollbackCameraBasisState cameraBasisState = CaptureRollbackCameraBasisState();
            LocomotionRuntimeRollbackState locomotionRuntimeState = runtimeStateStore.CaptureRollbackState();
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
                stateRestore,
                runtimeStateStore.RunLatchActive,
                runtimeStateStore.LastMovingGait,
                runtimeStateStore.CurrentWorldDirection,
                CurrentPhase,
                CurrentGait,
                progress.AliasKey,
                progress.NormalizedTime,
                runtimeBlackboard.CaptureRestoreState(),
                ThirdPersonAction.FullBodyActionRestoreState.Inactive,
                ThirdPersonInput.InputRequestBufferComponentRestoreState.Empty,
                cameraBasisState.Yaw,
                cameraBasisState,
                locomotionRuntimeState,
                motionExecutorState);
        }

        public bool RestoreSimulationSnapshot(in CharacterSimulationSnapshot snapshot)
        {
            runtimeBlackboard.Restore(snapshot.RuntimeBlackboardRestoreState);
            transform.SetPositionAndRotation(snapshot.Position, Quaternion.Euler(0f, snapshot.Yaw, 0f));
            rollbackCameraBasisProvider.Override(snapshot.CameraBasisState);

            runtimeStateStore.RestoreSnapshotHeader(
                snapshot.RunLatchActive,
                snapshot.LastMovingGait,
                snapshot.CurrentWorldDirection,
                snapshot.FullBodyRestoreState.Snapshot.ActivePath);
            if (motionExecutor == null)
                ResolveMotionExecutor();
            if (motionExecutor is IMotionExecutorRollbackStateProvider stateProvider)
                stateProvider.RestoreRollbackState(snapshot.MotionExecutorState);
            LocomotionRuntimeRollbackState locomotionState = snapshot.LocomotionRuntimeState;
            runtimeStateStore.RestoreRollbackState(in locomotionState);
            BasicMovementConfigSO movementConfig = ResolveMovementConfig();
            if (movementConfig == null)
            {
                LogFormalConfigMissing("movement-config-missing", "CharacterConfigSO.Movement is missing. Locomotion snapshot cannot be restored.");
                return false;
            }

            BasicLocomotionFrame restoredFrame = new BasicLocomotionFrame(
                new BasicLocomotionInputSnapshot(0f, Vector2.zero, Vector2.zero, snapshot.RunLatchActive),
                BasicMovementSettings.FromConfig(movementConfig),
                runtimeStateStore.CurrentIntent,
                runtimeStateStore.CurrentWorldDirection,
                snapshot.LocomotionPhase,
                new MovementCommand(runtimeStateStore.CurrentWorldDirection, 0f, 0f, 0f, snapshot.LocomotionPhase, snapshot.LocomotionGait, BasicMovementMotionFacts.None(snapshot.LocomotionPhase)));
            runtimeStateStore.RestoreFrame(in restoredFrame, ResolveSnapshotPhaseTime(in snapshot));
            AnimationPhasePlaybackProgress restoredProgress = ResolveSnapshotAnimationPlaybackProgress(in snapshot);
            RestoreAnimationPlaybackProgress(in restoredProgress, snapshot.LocomotionGait);
            if (!runtimeStateStore.HasPreviousMotionPlaybackProgress)
            {
                SeedMotionPlaybackWindow(in restoredProgress);
            }
            return true;
        }

        static float ResolveSnapshotPhaseTime(in CharacterSimulationSnapshot snapshot)
        {
            CharacterStateMachineSnapshot fullBody = snapshot.FullBodyRestoreState.Snapshot;
            return fullBody.ActiveState.IsValid ? fullBody.StateTime : snapshot.StateMachine.StateTime;
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

        public void SetInputSource(IBasicLocomotionInputSource source)
        {
            inputSource = source;
            inputSourceBehaviour = source as MonoBehaviour;
        }

        public void SetMotionExecutor(IBasicLocomotionMotionExecutor executor)
        {
            motionExecutor = executor;
            motionExecutorBehaviour = executor as MonoBehaviour;
        }

        public void SetAnimationPlaybackProgressSource(IAnimationPhasePlaybackProgressSource source)
        {
            playbackProgressSource = source;
            playbackProgressController = source as ILocomotionAnimationPlaybackProgressController;
            ResetMotionPlaybackWindow();
        }

        public void LogDiagnosticTickSnapshot(int step)
        {
            LocomotionDiagnostics.LogTickSnapshot(ActiveStatePath, step, BuildLocomotionDiagnosticContext());
        }

        AnimationPhasePlaybackProgress ResolvePlaybackProgress(BasicMovementPhase phase)
        {
            IAnimationPhasePlaybackProgressSource source = playbackProgressSource ?? locomotionPresenter;
            return source != null ? source.CurrentPlaybackProgress : AnimationPhasePlaybackProgress.Invalid(phase);
        }

        AnimationPhasePlaybackProgress ResolveCurrentAnimationPlaybackProgress()
        {
            IAnimationPhasePlaybackProgressSource source = playbackProgressSource ?? locomotionPresenter;
            if (source == null)
                return AnimationPhasePlaybackProgress.Invalid(CurrentPhase);

            AnimationPhasePlaybackProgress progress = source.CurrentPlaybackProgress;
            return progress.HasValidPlayback ? progress : AnimationPhasePlaybackProgress.Invalid(CurrentPhase);
        }

        void RestoreAnimationPlaybackProgress(in AnimationPhasePlaybackProgress progress, BasicMovementGait gait)
        {
            ILocomotionAnimationPlaybackProgressController controller = ResolvePlaybackProgressController();
            if (controller != null)
                controller.RestorePlaybackProgress(in progress, gait);
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

            if (playbackProgressSource is ILocomotionAnimationPlaybackProgressController sourceController)
            {
                playbackProgressController = sourceController;
                return playbackProgressController;
            }

            if (locomotionPresenter == null)
                ResolveLocomotionPresenter();

            playbackProgressController = locomotionPresenter as ILocomotionAnimationPlaybackProgressController;
            return playbackProgressController;
        }

        string BuildLocomotionDiagnosticContext()
        {
            AnimationPhasePlaybackProgress progress = ResolvePlaybackProgress(CurrentPhase);
            string presenterName = locomotionPresenter != null ? locomotionPresenter.name : "null";
            string animationName = locomotionPresenter != null ? locomotionPresenter.CurrentAnimationName : string.Empty;

            return
                $"phase={CurrentPhase} gait={CurrentFrame.Command.Gait} phaseTime={CurrentPhaseTime:F3} " +
                $"hasMove={CurrentIntent.HasMoveIntent} strength={CurrentIntent.Strength:F3} " +
                $"rawMove={CurrentIntent.RawInput.ToString("F3")} normalizedMove={CurrentIntent.NormalizedInput.ToString("F3")} " +
                $"worldDirection={CurrentWorldDirection.ToString("F3")} planarSpeed={CurrentFrame.Command.PlanarSpeed:F3} rotationSpeed={CurrentFrame.Command.RotationSpeed:F3} " +
                $"runLatch={RunLatchActive} motionSuppressed={suppressBasicMotionExecution} animationSuppressed={suppressLocomotionAnimationPresentation} " +
                $"hasAnimationMotion={CurrentFrame.Command.HasAnimationMotion} animMotionSourcePhase={CurrentFrame.Command.AnimationMotionSourcePhase} animMotionAlias={CurrentFrame.Command.AnimationMotionSourceAliasKey} " +
                $"animationPresenter={presenterName} animationPhase={progress.Phase} animationAlias={progress.AliasKey} animationName={animationName} " +
                $"animationNormalized={progress.NormalizedTime:F3} animationValid={progress.HasValidPlayback} animationEnded={progress.IsEnded}";
        }

        void ResetMotionPlaybackWindow()
        {
            runtimeStateStore.ResetMotionPlaybackWindow(CurrentPhase);
        }

        void SeedMotionPlaybackWindow(in AnimationPhasePlaybackProgress progress)
        {
            if (!progress.HasValidPlayback)
            {
                ResetMotionPlaybackWindow();
                return;
            }

            runtimeStateStore.SeedMotionPlaybackWindow(in progress, CurrentPhase);
        }

        LocomotionFrameRuntimeAdapter CreateFrameRuntimeAdapter()
        {
            frameRuntimeHost ??= new LocomotionFrameRuntimeHost(this);
            LocomotionPrepareFactsProvider prepareFactsProvider = new LocomotionPrepareFactsProvider(
                frameRuntimeHost,
                runtimeStateStore);
            LocomotionSpatialFactsProvider spatialFactsProvider = new LocomotionSpatialFactsProvider(frameRuntimeHost);
            LocomotionMotionFactsProvider motionFactsProvider = new LocomotionMotionFactsProvider(
                frameRuntimeHost,
                runtimeStateStore);
            LocomotionFrameRuntime runtime = new LocomotionFrameRuntime(
                new LocomotionFrameBuilder(),
                runtimeStateStore,
                prepareFactsProvider,
                spatialFactsProvider,
                motionFactsProvider,
                frameRuntimeHost);
            frameRuntimeAdapter = new LocomotionFrameRuntimeAdapter(
                runtime,
                runtimeStateStore,
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

        RunLocomotionAnimationConfigSO ResolveRunAnimationConfig()
        {
            if (characterConfig != null && characterConfig.LocomotionAnimation != null)
                return characterConfig.LocomotionAnimation;

            return null;
        }

        BasicMovementConfigSO ResolveMovementConfig()
        {
            if (characterConfig != null && characterConfig.Movement != null)
                return characterConfig.Movement;

            return null;
        }

        CharacterStateMachineDefinitionSO ResolveStateMachineDefinition()
        {
            if (characterConfig != null && characterConfig.StateMachine != null)
                return characterConfig.StateMachine;

            return null;
        }

        bool HasEnabledLegacyPlayer()
        {
            Component legacyPlayer = GetComponent("Player");
            return legacyPlayer is Behaviour behaviour && behaviour.enabled;
        }

        void LogRetiredDirectTick(int step)
        {
            if (loggedRetiredDirectTick)
                return;

            loggedRetiredDirectTick = true;
            LocomotionDiagnostics.SubmitRetiredDirectTick(ActiveStatePath, step);
        }

        void LogFormalConfigMissing(string eventId, string message)
        {
            LocomotionDiagnostics.SubmitFormalConfigMissing(ActiveStatePath, eventId, message);
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
            {
                inputSourceBehaviour = sourceBehaviour;
            }
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
            {
                motionExecutorBehaviour = executorBehaviour;
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
                if (playbackProgressController == null)
                    playbackProgressController = locomotionPresenter as ILocomotionAnimationPlaybackProgressController;
                return;
            }

            locomotionPresenter = LocomotionRuntimeReferenceResolver.ResolveLocomotionPresenter(this, out playbackProgressController);
        }

        void ResolveCameraController()
        {
            cameraController = LocomotionRuntimeReferenceResolver.ResolveCameraController(this);
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

        sealed class LocomotionOutputRuntimeHost :
            ILocomotionMotionOutputDependencies,
            ILocomotionAnimationOutputDependencies,
            ILocomotionRuntimeBlackboardDependencies,
            ILocomotionOutputCompletionDependencies
        {
            readonly PlayerLocomotionController owner;

            public LocomotionOutputRuntimeHost(PlayerLocomotionController owner)
            {
                this.owner = owner;
            }

            public IBasicLocomotionMotionExecutor MotionExecutor => owner.motionExecutor;
            public bool SuppressBasicMotionExecution => owner.suppressBasicMotionExecution;
            public bool SuppressLocomotionAnimationPresentation => owner.suppressLocomotionAnimationPresentation;
            public RunLocomotionAnimationConfigSO AnimationConfig => owner.ResolveRunAnimationConfig();
            public CharacterRuntimeBlackboardSnapshot RuntimeBlackboardSnapshot => owner.runtimeBlackboard.Snapshot;
            public BasicMovementGait CurrentGait => owner.CurrentGait;
            public LocomotionRuntimeStateStore StateStore => owner.runtimeStateStore;
            public bool HasCameraController => owner.cameraController != null;
            public bool IsRollbackCameraBasisOverrideActive => owner.rollbackCameraBasisProvider.UsingOverride;
            public string CurrentAnimationName => owner.CurrentAnimationName;

            public void ResolveMotionExecutor()
            {
                owner.ResolveMotionExecutor();
            }

            public void PresentAnimation(in MovementAnimationContext context)
            {
                if (owner.locomotionPresenter != null)
                    owner.locomotionPresenter.Present(in context);
            }

            public void WriteActionFactsToBlackboard(in CharacterRuntimeActionFacts facts)
            {
                owner.runtimeBlackboard.WriteActionFacts(in facts);
            }

            public void WriteAnimationFactsToBlackboard(in CharacterRuntimeAnimationFacts facts)
            {
                owner.runtimeBlackboard.WriteAnimationFacts(in facts);
            }

            public void ResolveCamera()
            {
                if (owner.cameraController != null)
                    owner.cameraController.Resolve();
            }

            public void SyncRollbackCameraBasis()
            {
                owner.rollbackCameraBasisProvider.SyncFrom(owner.cameraController, owner.ResolveCameraPlanarYaw());
            }
        }

        sealed class LocomotionFrameRuntimeHost :
            ILocomotionPrepareFactsProviderHost,
            ILocomotionSpatialFactsProviderHost,
            ILocomotionFrameRuntimeOutputHost
        {
            readonly PlayerLocomotionController owner;

            public LocomotionFrameRuntimeHost(PlayerLocomotionController owner)
            {
                this.owner = owner;
            }

            public BasicMovementConfigSO MovementConfig => owner.ResolveMovementConfig();
            public RunLocomotionAnimationConfigSO AnimationConfig => owner.ResolveRunAnimationConfig();
            public CharacterRuntimeBlackboardSnapshot RuntimeBlackboardSnapshot => owner.runtimeBlackboard.Snapshot;
            public string ActiveStatePath => owner.runtimeStateStore.ActiveStatePath;
            public RollbackCameraBasisProvider RollbackCameraBasisProvider => owner.rollbackCameraBasisProvider;
            public bool HasCameraController => owner.cameraController != null;
            public Vector2 CameraLookSensitivity => owner.cameraController != null ? owner.cameraController.Sensitivity : new Vector2(0.12f, 0.12f);
            public float HostYaw => owner.transform.eulerAngles.y;
            public AnimationPhasePlaybackProgress CurrentAnimationPlaybackProgress => owner.ResolveCurrentAnimationPlaybackProgress();

            public void AdvanceAnimationPlaybackProgress(float deltaTime)
            {
                owner.AdvanceAnimationPlaybackProgress(deltaTime);
            }

            public AnimationPhasePlaybackProgress ResolvePlaybackProgress(BasicMovementPhase phase)
            {
                return owner.ResolvePlaybackProgress(phase);
            }

            public void SubmitFormalConfigMissing(string eventId, string message)
            {
                owner.LogFormalConfigMissing(eventId, message);
            }

            public void ApplyCameraLook(Vector2 lookInput)
            {
                if (owner.cameraController != null)
                    owner.cameraController.ApplyLook(lookInput);
            }

            public void SyncRollbackCameraBasisFromCamera()
            {
                owner.rollbackCameraBasisProvider.SyncFrom(owner.cameraController, owner.ResolveCameraPlanarYaw());
            }

            public void SyncRollbackCameraBasisWithoutCamera()
            {
                owner.rollbackCameraBasisProvider.SyncFrom(null, owner.ResolveCameraPlanarYaw());
            }

            public Vector3 ResolveFacingForward()
            {
                return owner.ResolveFacingForward();
            }

            public void LogCameraInput(Vector2 moveInput, Vector2 lookInput)
            {
                owner.LogCameraInput(moveInput, lookInput);
            }

            public void WriteLocomotionFacts(in CharacterRuntimeLocomotionFacts facts)
            {
                owner.runtimeBlackboard.WriteLocomotionFacts(in facts);
            }
        }

    }
}
