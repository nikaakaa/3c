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
    public sealed class PlayerLocomotionController : MonoBehaviour
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

        readonly LocomotionFramePipeline framePipeline = new LocomotionFramePipeline();
        readonly CharacterRuntimeBlackboard runtimeBlackboard = new CharacterRuntimeBlackboard();
        IBasicLocomotionInputSource inputSource;
        IBasicLocomotionMotionExecutor motionExecutor;
        IFacingDirectionProvider facingProvider;
        IAnimationPhasePlaybackProgressSource playbackProgressSource;
        ILocomotionAnimationPlaybackProgressController playbackProgressController;
        AnimationPhasePlaybackProgress previousMotionPlaybackProgress;
        MovementInputIntent currentIntent;
        BasicMovementGait lastMovingGait = BasicMovementGait.Walk;
        Vector3 currentWorldDirection;
        BasicLocomotionFrame currentFrame;
        float currentPhaseTime;
        bool hasPreviousMotionPlaybackProgress;
        bool hasActiveMoveStopGait;
        BasicMovementGait activeMoveStopGait = BasicMovementGait.Walk;
        bool previousCameraAutoTick;
        bool hasPreviousCameraAutoTick;
        float nextCameraDebugLogTime;
        bool suppressBasicMotionExecution;
        bool suppressLocomotionAnimationPresentation;
        bool runLatchActive;
        Vector3 previousWorldDirection;
        LocomotionTurnBackIntent pendingTurnBackIntent;
        int localDecisionStep;
        bool loggedRetiredDirectTick;
        string activeStatePath = string.Empty;
        readonly RollbackCameraBasisProvider rollbackCameraBasisProvider = new RollbackCameraBasisProvider();

        public BasicMovementPhase CurrentPhase => currentFrame.Phase;
        public float CurrentPhaseTime => currentPhaseTime;
        public string ActiveStatePath => activeStatePath;
        public BasicMovementGait CurrentGait => currentFrame.Command.Gait;
        public Vector3 CurrentWorldDirection => currentWorldDirection;
        public MovementInputIntent CurrentIntent => currentIntent;
        public BasicLocomotionFrame CurrentFrame => currentFrame;
        public AnimationPhasePlaybackProgress CurrentAnimationPlaybackProgress => ResolveCurrentAnimationPlaybackProgress();
        public string CurrentAnimationName => locomotionPresenter != null ? locomotionPresenter.CurrentAnimationName : string.Empty;
        public bool RunLatchActive => runLatchActive;
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
        public CharacterRuntimeBlackboardSnapshot RuntimeBlackboardSnapshot => runtimeBlackboard.Snapshot;
        public BasicMovementConfigSO Config { get => ResolveMovementConfig(); set { } }
        public CharacterStateMachineDefinitionSO StateMachineDefinition { get => ResolveStateMachineDefinition(); set => SetStateMachineDefinition(value); }
        public bool AutoUpdate { get => autoUpdate; set => autoUpdate = value; }
        public bool SuppressBasicMotionExecution { get => suppressBasicMotionExecution; set => suppressBasicMotionExecution = value; }
        public bool SuppressLocomotionAnimationPresentation { get => suppressLocomotionAnimationPresentation; set => suppressLocomotionAnimationPresentation = value; }
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

            lastMovingGait = BasicMovementGait.Walk;
            runLatchActive = false;
            hasActiveMoveStopGait = false;
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
            int currentStep,
            out LocomotionStateDecisionFrame stateDecision)
        {
            if (runner == null)
            {
                stateDecision = default;
                return false;
            }

            BasicLocomotionInputSnapshot frameInput = decisionFrame.Input;
            BasicMovementSettings settings = decisionFrame.Settings;
            CharacterRuntimeBlackboardSnapshot blackboardSnapshot = runtimeBlackboard.Snapshot;
            LocomotionFramePipelineInput pipelineInput = BuildFramePipelineInput(
                in frameInput,
                currentStep,
                runner.Snapshot.LocomotionPhase,
                runner.StateTime,
                in settings,
                in inputRequest,
                in blackboardSnapshot);
            if (!framePipeline.TryEvaluatePreparedGameplayDecision(
                    in decisionFrame,
                    runner,
                    in pipelineInput,
                    out stateDecision,
                    out LocomotionFramePipelineResult pipelineResult))
            {
                return false;
            }

            LocomotionFrameRuntimeState runtimeState = pipelineResult.RuntimeState;
            ApplyFrameRuntimeState(in runtimeState);
            return true;
        }

        public bool TryBuildMotionFromStateDecision(
            in LocomotionStateDecisionFrame stateDecision,
            int currentStep,
            out BasicLocomotionFrame frame,
            out CharacterStateMachineFrame stateFrame)
        {
            if (!stateDecision.HasStateFrame)
            {
                frame = default;
                stateFrame = default;
                return false;
            }

            CharacterStateMachineFrame stateFrameForMotion = stateDecision.StateFrame;
            BasicMovementMotionFacts motionFacts = ResolveMotionFacts(
                in stateFrameForMotion,
                stateDecision.FrameGait,
                currentStep);
            AnimationPhasePlaybackProgress progress = ResolveCurrentAnimationPlaybackProgress();
            LocomotionFrameRuntimeState runtimeState = CaptureFrameRuntimeState();
            if (!framePipeline.TryBuildMotionFromStateDecision(
                    in stateDecision,
                    currentStep,
                    in motionFacts,
                    in runtimeState,
                    in progress,
                    out frame,
                    out stateFrame,
                    out LocomotionFramePipelineResult pipelineResult))
            {
                return false;
            }

            ApplyFramePipelineResult(in pipelineResult);
            CharacterRuntimeLocomotionFacts locomotionFacts = pipelineResult.LocomotionFacts;
            runtimeBlackboard.WriteLocomotionFacts(in locomotionFacts);
            return true;
        }

        public void ExecuteLocomotionMotion(in BasicLocomotionFrame frame)
        {
            if (motionExecutor == null)
                ResolveMotionExecutor();

            if (motionExecutor != null && !suppressBasicMotionExecution)
            {
                MovementCommand command = frame.Command;
                motionExecutor.ExecuteBasicMovement(in command);
            }
        }

        public void PresentLocomotionAnimation(in BasicLocomotionFrame frame)
        {
            if (locomotionPresenter != null && !suppressLocomotionAnimationPresentation)
            {
                float currentSpeed = motionExecutor != null ? motionExecutor.CurrentSpeed : frame.Command.PlanarSpeed;
                MovementAnimationContext animationContext = BuildAnimationContext(in frame, currentSpeed);
                locomotionPresenter.Present(in animationContext);
            }
        }

        public void CompleteLocomotionTick()
        {
            if (cameraController != null && !rollbackCameraBasisProvider.UsingOverride)
                cameraController.Resolve();

            rollbackCameraBasisProvider.SyncFrom(cameraController, ResolveCameraPlanarYaw());

            ResetRunLatchAfterIdle();
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
            lastMovingGait = BasicMovementGait.Walk;
            runLatchActive = false;
            hasActiveMoveStopGait = false;
            ResetMotionPlaybackWindow();
        }

        public void SetRunLatchActive(bool active)
        {
            runLatchActive = active;
            if (!active && !currentIntent.HasMoveIntent)
                lastMovingGait = BasicMovementGait.Walk;
        }

        LocomotionFramePipelineInput BuildFramePipelineInput(
            in BasicLocomotionInputSnapshot input,
            int currentStep,
            BasicMovementPhase currentPhase,
            float currentPhaseTime,
            in BasicMovementSettings baseSettings,
            in CharacterInputRequestFact inputRequest,
            in CharacterRuntimeBlackboardSnapshot blackboardSnapshot)
        {
            return new LocomotionFramePipelineInput(
                input,
                currentStep,
                currentPhase,
                currentPhaseTime,
                baseSettings,
                inputRequest,
                blackboardSnapshot,
                CaptureFrameRuntimeState(),
                ActiveStatePath);
        }

        LocomotionFrameRuntimeState CaptureFrameRuntimeState()
        {
            return new LocomotionFrameRuntimeState(
                currentIntent,
                lastMovingGait,
                hasActiveMoveStopGait,
                activeMoveStopGait,
                runLatchActive,
                previousWorldDirection,
                pendingTurnBackIntent);
        }

        void ApplyFrameRuntimeState(in LocomotionFrameRuntimeState state)
        {
            currentIntent = state.CurrentIntent;
            lastMovingGait = state.LastMovingGait;
            hasActiveMoveStopGait = state.HasActiveMoveStopGait;
            activeMoveStopGait = state.ActiveMoveStopGait;
            runLatchActive = state.RunLatchActive;
            previousWorldDirection = state.PreviousWorldDirection;
            pendingTurnBackIntent = state.PendingTurnBackIntent;
        }

        void ApplyFramePipelineResult(in LocomotionFramePipelineResult result)
        {
            LocomotionFrameRuntimeState runtimeState = result.RuntimeState;
            ApplyFrameRuntimeState(in runtimeState);
            if (!result.HasFrame)
                return;

            currentFrame = result.Frame;
            currentPhaseTime = result.CurrentPhaseTime;
            activeStatePath = result.ActiveStatePath;
            currentWorldDirection = result.CurrentWorldDirection;
        }

        public bool TryPrepareDecisionFrame(
            in BasicLocomotionInputSnapshot input,
            CharacterStateMachineRunner runner,
            int currentStep,
            out LocomotionDecisionFrame decisionFrame)
        {
            if (runner == null)
            {
                decisionFrame = default;
                return false;
            }

            BasicMovementConfigSO movementConfig = ResolveMovementConfig();
            if (movementConfig == null)
            {
                LogFormalConfigMissing("movement-config-missing", "CharacterConfigSO.Movement is missing. Locomotion facts cannot be prepared.");
                decisionFrame = default;
                return false;
            }

            BasicMovementSettings baseSettings = BasicMovementSettings.FromConfig(movementConfig);
            AdvanceAnimationPlaybackProgress(input.DeltaTime);
            BasicMovementPhase currentPhase = runner.Snapshot.LocomotionPhase;
            CharacterInputRequestFact inputRequest = default;
            CharacterRuntimeBlackboardSnapshot blackboardSnapshot = runtimeBlackboard.Snapshot;
            LocomotionFramePipelineInput pipelineInput = BuildFramePipelineInput(
                in input,
                currentStep,
                currentPhase,
                runner.StateTime,
                in baseSettings,
                in inputRequest,
                in blackboardSnapshot);
            LocomotionFramePrepareFacts prepareFacts = framePipeline.ResolvePrepareFacts(in pipelineInput);
            BasicMovementSettings settings = ResolveMovementSettings(prepareFacts.FrameGait, in baseSettings);
            BasicMovementPhaseFacts phaseFacts = ResolvePhaseFacts(
                currentPhase,
                runner.StateTime,
                prepareFacts.FrameGait,
                input.DeltaTime,
                in settings);
            MovementInputIntent prepareIntent = prepareFacts.Intent;
            LocomotionSpatialFacts spatialFacts = ResolveSpatialFacts(in input, in prepareIntent);
            if (!framePipeline.TryPrepareDecisionFrame(
                    in pipelineInput,
                    in prepareFacts,
                    in settings,
                    in phaseFacts,
                    in spatialFacts,
                    out decisionFrame,
                    out LocomotionFramePipelineResult pipelineResult))
            {
                return false;
            }

            LocomotionFrameRuntimeState runtimeState = pipelineResult.RuntimeState;
            ApplyFrameRuntimeState(in runtimeState);
            return true;
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

        LocomotionSpatialFacts ResolveSpatialFacts(
            in BasicLocomotionInputSnapshot input,
            in MovementInputIntent intent)
        {
            if (rollbackCameraBasisProvider.UsingOverride)
            {
                rollbackCameraBasisProvider.ApplyLook(input.Look, cameraController != null ? cameraController.Sensitivity : new Vector2(0.12f, 0.12f));
            }
            else if (cameraController != null)
            {
                cameraController.ApplyLook(input.Look);
                rollbackCameraBasisProvider.SyncFrom(cameraController, ResolveCameraPlanarYaw());
            }
            else
            {
                rollbackCameraBasisProvider.SyncFrom(null, ResolveCameraPlanarYaw());
            }

            LogCameraInput(input.Move, input.Look);

            return LocomotionFactsBuilder.BuildSpatialFacts(
                in intent,
                CameraRelativeMovementResolver.Resolve(intent, rollbackCameraBasisProvider),
                ResolveFacingForward(),
                rollbackCameraBasisProvider.CameraPlanarForward,
                rollbackCameraBasisProvider.CameraPlanarRight);
        }

        public void WriteActionFacts(in CharacterRuntimeActionFacts facts)
        {
            runtimeBlackboard.WriteActionFacts(in facts);
        }

        public void WriteAnimationFacts(in CharacterRuntimeAnimationFacts facts)
        {
            CharacterRuntimeAnimationFacts resolvedFacts = ResolveLocomotionFootPhaseAnimationFacts(in facts);
            runtimeBlackboard.WriteAnimationFacts(in resolvedFacts);
        }

        void WriteLocomotionAnimationFacts(int sourceStep)
        {
            CharacterRuntimeAnimationFacts previous = runtimeBlackboard.Snapshot.Animation;
            WriteAnimationFacts(new CharacterRuntimeAnimationFacts(
                CurrentAnimationPlaybackProgress,
                CurrentAnimationName,
                previous.ActionProgress,
                previous.ActionAnimationName,
                sourceStep));
        }

        CharacterRuntimeAnimationFacts ResolveLocomotionFootPhaseAnimationFacts(in CharacterRuntimeAnimationFacts facts)
        {
            CharacterRuntimeAnimationFacts previous = runtimeBlackboard.Snapshot.Animation;
            AnimationPhasePlaybackProgress locomotionProgress = facts.LocomotionProgress;
            LocomotionFootPhaseSample currentSample = ResolveCurrentLocomotionFootPhaseSample(
                in locomotionProgress,
                facts.SourceStep);
            LocomotionFootPhaseSample exitSample = ResolveLastLocomotionExitFootPhase(
                in previous,
                in facts,
                facts.SourceStep);

            return new CharacterRuntimeAnimationFacts(
                facts.LocomotionProgress,
                facts.LocomotionAnimationName,
                facts.ActionProgress,
                facts.ActionAnimationName,
                currentSample,
                exitSample,
                facts.SourceStep);
        }

        LocomotionFootPhaseSample ResolveCurrentLocomotionFootPhaseSample(
            in AnimationPhasePlaybackProgress progress,
            int sourceStep)
        {
            BasicMovementGait gait = ResolvePlaybackGait(in progress, CurrentGait);
            if (!progress.HasValidPlayback || string.IsNullOrWhiteSpace(progress.AliasKey))
            {
                return LocomotionFootPhaseSample.Invalid(
                    progress.Phase,
                    gait,
                    progress.AliasKey,
                    progress.NormalizedTime,
                    sourceStep);
            }

            RunLocomotionAnimationConfigSO animationConfig = ResolveRunAnimationConfig();
            LocomotionFootPhaseProfileSO profile = animationConfig != null
                ? animationConfig.ResolveFootPhaseProfile(progress.Phase, gait, progress.AliasKey)
                : null;
            return LocomotionFootPhaseSampler.Sample(
                profile,
                progress.Phase,
                gait,
                progress.AliasKey,
                progress.NormalizedTime,
                sourceStep);
        }

        LocomotionFootPhaseSample ResolveLastLocomotionExitFootPhase(
            in CharacterRuntimeAnimationFacts previous,
            in CharacterRuntimeAnimationFacts current,
            int sourceStep)
        {
            if (!IsTurnBackToRunLoopAnimationTransition(in previous, in current))
                return previous.LastLocomotionExitFootPhase;

            LocomotionFootPhaseSample previousSample = previous.CurrentLocomotionFootPhase;
            if (previousSample.IsValid && previousSample.Phase == BasicMovementPhase.TurnBack)
                return previousSample.WithSourceStep(sourceStep);

            AnimationPhasePlaybackProgress progress = previous.LocomotionProgress;
            BasicMovementGait gait = ResolvePlaybackGait(in progress, BasicMovementGait.Run);
            return LocomotionFootPhaseSample.Invalid(
                BasicMovementPhase.TurnBack,
                gait,
                progress.AliasKey,
                progress.NormalizedTime,
                sourceStep);
        }

        bool IsTurnBackToRunLoopAnimationTransition(
            in CharacterRuntimeAnimationFacts previous,
            in CharacterRuntimeAnimationFacts current)
        {
            bool previousWasTurnBack =
                previous.LocomotionProgress.Phase == BasicMovementPhase.TurnBack ||
                previous.CurrentLocomotionFootPhase.Phase == BasicMovementPhase.TurnBack;
            if (!previousWasTurnBack)
                return false;

            AnimationPhasePlaybackProgress currentProgress = current.LocomotionProgress;
            if (!currentProgress.HasValidPlayback || currentProgress.Phase != BasicMovementPhase.MoveLoop)
                return false;

            RunLocomotionAnimationConfigSO animationConfig = ResolveRunAnimationConfig();
            string runLoopAlias = LocomotionAnimationAliasResolver.ResolveAliasKey(
                animationConfig,
                BasicMovementPhase.MoveLoop,
                BasicMovementGait.Run);
            return string.Equals(currentProgress.AliasKey, runLoopAlias, System.StringComparison.Ordinal);
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
            LocomotionRuntimeRollbackState locomotionRuntimeState = LocomotionSnapshotAdapter.CaptureRuntimeState(
                in currentIntent,
                previousWorldDirection,
                in previousMotionPlaybackProgress,
                hasPreviousMotionPlaybackProgress,
                hasActiveMoveStopGait,
                activeMoveStopGait,
                in pendingTurnBackIntent);
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
                runLatchActive,
                lastMovingGait,
                currentWorldDirection,
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

            runLatchActive = snapshot.RunLatchActive;
            lastMovingGait = snapshot.LastMovingGait;
            currentWorldDirection = snapshot.CurrentWorldDirection;
            if (motionExecutor == null)
                ResolveMotionExecutor();
            if (motionExecutor is IMotionExecutorRollbackStateProvider stateProvider)
                stateProvider.RestoreRollbackState(snapshot.MotionExecutorState);
            LocomotionRuntimeRollbackState locomotionState = snapshot.LocomotionRuntimeState;
            LocomotionSnapshotAdapter.ReadRuntimeState(
                in locomotionState,
                out currentIntent,
                out previousWorldDirection,
                out previousMotionPlaybackProgress,
                out hasPreviousMotionPlaybackProgress,
                out hasActiveMoveStopGait,
                out activeMoveStopGait,
                out pendingTurnBackIntent);
            activeStatePath = snapshot.FullBodyRestoreState.Snapshot.ActivePath;
            BasicMovementConfigSO movementConfig = ResolveMovementConfig();
            if (movementConfig == null)
            {
                LogFormalConfigMissing("movement-config-missing", "CharacterConfigSO.Movement is missing. Locomotion snapshot cannot be restored.");
                return false;
            }

            currentFrame = new BasicLocomotionFrame(
                new BasicLocomotionInputSnapshot(0f, Vector2.zero, Vector2.zero, snapshot.RunLatchActive),
                BasicMovementSettings.FromConfig(movementConfig),
                currentIntent,
                currentWorldDirection,
                snapshot.LocomotionPhase,
                new MovementCommand(currentWorldDirection, 0f, 0f, 0f, snapshot.LocomotionPhase, snapshot.LocomotionGait, BasicMovementMotionFacts.None(snapshot.LocomotionPhase)));
            currentPhaseTime = ResolveSnapshotPhaseTime(in snapshot);
            AnimationPhasePlaybackProgress restoredProgress = ResolveSnapshotAnimationPlaybackProgress(in snapshot);
            RestoreAnimationPlaybackProgress(in restoredProgress, snapshot.LocomotionGait);
            if (hasPreviousMotionPlaybackProgress)
            {
                hasPreviousMotionPlaybackProgress = true;
            }
            else
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

        MovementAnimationContext BuildAnimationContext(in BasicLocomotionFrame frame, float planarSpeed)
        {
            bool hasEntryFootPhaseMatchRequest = TryResolveRunLoopEntryFootPhaseMatch(
                in frame,
                out LocomotionFootPhaseMatchResult entryFootPhaseMatchResult);

            return new MovementAnimationContext(
                frame.Phase,
                frame.Command.Gait,
                frame.Intent.HasMoveIntent,
                frame.Intent.Strength,
                frame.WorldDirection,
                planarSpeed,
                frame.Command.TurnBackMotionPolicy,
                frame.Command.HasTurnBackMotionPolicy,
                entryFootPhaseMatchResult,
                hasEntryFootPhaseMatchRequest);
        }

        bool TryResolveRunLoopEntryFootPhaseMatch(
            in BasicLocomotionFrame frame,
            out LocomotionFootPhaseMatchResult result)
        {
            result = LocomotionFootPhaseMatchResult.NotRequested;
            if (frame.Phase != BasicMovementPhase.MoveLoop || frame.Command.Gait != BasicMovementGait.Run)
                return false;

            RunLocomotionAnimationConfigSO animationConfig = ResolveRunAnimationConfig();
            string runLoopAlias = LocomotionAnimationAliasResolver.ResolveAliasKey(
                animationConfig,
                BasicMovementPhase.MoveLoop,
                BasicMovementGait.Run);
            CharacterRuntimeAnimationFacts previousAnimation = runtimeBlackboard.Snapshot.Animation;
            bool previousWasTurnBack =
                previousAnimation.LocomotionProgress.Phase == BasicMovementPhase.TurnBack ||
                previousAnimation.CurrentLocomotionFootPhase.Phase == BasicMovementPhase.TurnBack;
            if (!previousWasTurnBack)
                return false;

            LocomotionFootPhaseSample exitSample = previousAnimation.CurrentLocomotionFootPhase;
            if (!exitSample.IsValid)
            {
                result = LocomotionFootPhaseMatchResult.Invalid("exit-foot-phase-invalid");
                return true;
            }

            LocomotionFootPhaseMatchRequest request = new LocomotionFootPhaseMatchRequest(
                exitSample,
                BasicMovementPhase.MoveLoop,
                BasicMovementGait.Run,
                runLoopAlias);
            LocomotionFootPhaseProfileSO targetProfile = animationConfig != null
                ? animationConfig.ResolveFootPhaseProfile(BasicMovementPhase.MoveLoop, BasicMovementGait.Run, runLoopAlias)
                : null;
            result = LocomotionFootPhaseMatcher.Match(in request, targetProfile);
            return true;
        }

        BasicMovementGait ResolvePlaybackGait(
            in AnimationPhasePlaybackProgress progress,
            BasicMovementGait fallback)
        {
            RunLocomotionAnimationConfigSO animationConfig = ResolveRunAnimationConfig();
            return LocomotionAnimationAliasResolver.ResolveGaitForAlias(
                animationConfig,
                progress.Phase,
                progress.AliasKey,
                fallback);
        }

        static bool TryNormalizePlanar(Vector3 value, out Vector3 normalized)
        {
            value.y = 0f;
            float sqrMagnitude = value.sqrMagnitude;
            if (sqrMagnitude <= DirectionSqrEpsilon)
            {
                normalized = Vector3.zero;
                return false;
            }

            normalized = value / Mathf.Sqrt(sqrMagnitude);
            return true;
        }

        static Vector3 NormalizePlanarOrZero(Vector3 value)
        {
            return TryNormalizePlanar(value, out Vector3 normalized) ? normalized : Vector3.zero;
        }

        BasicMovementSettings ResolveMovementSettings(BasicMovementGait gait, in BasicMovementSettings baseSettings)
        {
            RunLocomotionAnimationConfigSO animationConfig = ResolveRunAnimationConfig();
            if (animationConfig == null)
                return baseSettings;

            return animationConfig.ApplyPhaseTiming(gait, in baseSettings);
        }

        BasicMovementPhaseFacts ResolvePhaseFacts(BasicMovementGait gait, float deltaTime, in BasicMovementSettings settings)
        {
            return ResolvePhaseFacts(CurrentPhase, CurrentPhaseTime, gait, deltaTime, in settings);
        }

        BasicMovementPhaseFacts ResolvePhaseFacts(
            BasicMovementPhase phase,
            float currentPhaseTime,
            BasicMovementGait gait,
            float deltaTime,
            in BasicMovementSettings settings)
        {
            float nextPhaseTime = currentPhaseTime + Mathf.Max(0f, deltaTime);
            RunLocomotionAnimationConfigSO animationConfig = ResolveRunAnimationConfig();
            if (animationConfig == null)
                return BasicMovementPhaseFacts.FromTiming(phase, nextPhaseTime, in settings);

            LocomotionAnimationPhaseConfig phaseConfig = animationConfig.ResolvePhaseConfig(phase, gait);
            AnimationPhasePlaybackProgress progress = ResolvePlaybackProgress(phase);
            AnimationPhaseTimelineFacts facts = AnimationPhaseTimelineSampler.Sample(phase, in phaseConfig, nextPhaseTime, in progress);
            return new BasicMovementPhaseFacts(facts.CanExit);
        }

        BasicMovementMotionFacts ResolveMotionFacts(BasicMovementGait gait)
        {
            return ResolveMotionFacts(CurrentPhase, gait);
        }

        BasicMovementMotionFacts ResolveMotionFacts(
            in CharacterStateMachineFrame stateFrame,
            BasicMovementGait gait,
            int currentStep)
        {
            if (stateFrame.LocomotionPhase == BasicMovementPhase.TurnBack)
                return ResolveTurnBackRootMotionFacts(in stateFrame, gait, currentStep);

            return ResolveMotionFacts(stateFrame.LocomotionPhase, gait);
        }

        BasicMovementMotionFacts ResolveMotionFacts(BasicMovementPhase phase, BasicMovementGait gait)
        {
            if (phase == BasicMovementPhase.TurnBack)
                return ResolveTurnBackRootMotionFacts(phase, gait);

            RunLocomotionAnimationConfigSO animationConfig = ResolveRunAnimationConfig();
            if (animationConfig == null)
            {
                ResetMotionPlaybackWindow();
                return BasicMovementMotionFacts.None(phase);
            }

            string aliasKey = animationConfig.ResolveAliasKey(phase, gait);
            LocomotionMotionProfileSO profile = animationConfig.ResolveMotionProfile(phase, gait, aliasKey);
            if (profile == null)
            {
                ResetMotionPlaybackWindow();
                return BasicMovementMotionFacts.None(phase);
            }

            AnimationPhasePlaybackProgress progress = ResolvePlaybackProgress(phase);
            AnimationMotionPlaybackWindow playbackWindow = BuildMotionPlaybackWindow(phase, gait, in progress);
            AnimationMotionProfileSample sample = ResolveBakedMotionProfileSample(
                animationConfig,
                phase,
                gait,
                aliasKey,
                in playbackWindow);
            if (!sample.HasMotionContribution)
                return BasicMovementMotionFacts.None(phase);

            return new BasicMovementMotionFacts(
                true,
                sample.LocalPlanarDelta,
                sample.YawDelta,
                sample.SourcePhase,
                sample.SourceAliasKey);
        }

        BasicMovementMotionFacts ResolveTurnBackRootMotionFacts(BasicMovementPhase phase, BasicMovementGait gait)
        {
            return ResolveTurnBackRootMotionFacts(
                phase,
                gait,
                TurnBackMotionPolicy.Default,
                Vector3.zero,
                Vector3.zero,
                0);
        }

        BasicMovementMotionFacts ResolveTurnBackRootMotionFacts(
            in CharacterStateMachineFrame stateFrame,
            BasicMovementGait gait,
            int currentStep)
        {
            TurnBackMotionPolicy policy = stateFrame.HasTurnBackMotionPolicy
                ? stateFrame.TurnBackMotionPolicy
                : TurnBackMotionPolicy.Default;
            return ResolveTurnBackRootMotionFacts(
                stateFrame.LocomotionPhase,
                gait,
                policy,
                stateFrame.TurnBackWorldDirection,
                stateFrame.TurnBackEntryBasisForward,
                currentStep,
                stateFrame.TimelineFacts);
        }

        BasicMovementMotionFacts ResolveTurnBackRootMotionFacts(
            BasicMovementPhase phase,
            BasicMovementGait gait,
            TurnBackMotionPolicy policy,
            Vector3 lockedWorldDirection,
            Vector3 entryPlanarBasisForward,
            int currentStep,
            StateTimelineWindowFacts timelineFacts = default)
        {
            RunLocomotionAnimationConfigSO animationConfig = ResolveRunAnimationConfig();
            string aliasKey = policy.IsEnabled ? policy.AliasKey : animationConfig != null ? animationConfig.ResolveAliasKey(phase, gait) : TurnBackMotionPolicy.DefaultAliasKey;
            AnimationPhasePlaybackProgress progress = ResolvePlaybackProgress(phase);
            AnimationMotionPlaybackWindow playbackWindow = BuildMotionPlaybackWindow(
                phase,
                gait,
                aliasKey,
                in progress,
                true,
                true);
            AnimationMotionProfileSample bakedSample = TurnBackMotionResolver.RequiresBakedMotion(in policy)
                ? ResolveTurnBackBakedMotionSample(
                    animationConfig,
                    phase,
                    gait,
                    aliasKey,
                    in playbackWindow)
                : AnimationMotionProfileSample.None(phase);
            TurnBackMotionResolution resolution = TurnBackMotionResolver.Resolve(
                phase,
                aliasKey,
                in policy,
                in bakedSample,
                entryPlanarBasisForward,
                in timelineFacts);
            Vector3 appliedPlanarDelta = resolution.AppliedPlanarDelta;
            float appliedYawDelta = resolution.AppliedYawDelta;
            BasicMovementPlanarDeltaSpace deltaSpace = resolution.DeltaSpace;
            Vector3 resolvedEntryBasisForward = resolution.EntryPlanarBasisForward;
            if (resolution.EntryBasisMissing)
            {
                Vector3 rejectedPlanarDelta = resolution.RejectedPlanarDelta;
                LogTurnBackEntryBasisMissing(currentStep, phase, gait, aliasKey, in rejectedPlanarDelta);
            }

            LogTurnBackRootMotionConsumed(
                phase,
                gait,
                aliasKey,
                in bakedSample,
                in policy,
                in playbackWindow,
                in appliedPlanarDelta,
                appliedYawDelta,
                deltaSpace,
                resolvedEntryBasisForward,
                in timelineFacts);
            LogTurnBackStatePolicy(currentStep, phase, gait, aliasKey, in policy, lockedWorldDirection, resolvedEntryBasisForward, in appliedPlanarDelta, appliedYawDelta, in timelineFacts);
            return resolution.MotionFacts;
        }

        AnimationMotionProfileSample ResolveTurnBackBakedMotionSample(
            RunLocomotionAnimationConfigSO animationConfig,
            BasicMovementPhase phase,
            BasicMovementGait gait,
            string aliasKey,
            in AnimationMotionPlaybackWindow playbackWindow)
        {
            return ResolveBakedMotionProfileSample(
                animationConfig,
                phase,
                gait,
                aliasKey,
                in playbackWindow);
        }

        AnimationMotionProfileSample ResolveBakedMotionProfileSample(
            RunLocomotionAnimationConfigSO animationConfig,
            BasicMovementPhase phase,
            BasicMovementGait gait,
            string aliasKey,
            in AnimationMotionPlaybackWindow playbackWindow)
        {
            if (animationConfig == null || !playbackWindow.HasValidPlayback)
                return AnimationMotionProfileSample.None(phase);

            LocomotionMotionProfileSO profile = animationConfig.ResolveMotionProfile(phase, gait, aliasKey);
            return AnimationMotionProfileSampler.Sample(profile, in playbackWindow);
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

        AnimationMotionPlaybackWindow BuildMotionPlaybackWindow(BasicMovementPhase phase, BasicMovementGait gait, in AnimationPhasePlaybackProgress progress)
        {
            string aliasKey = progress.AliasKey;
            return BuildMotionPlaybackWindow(phase, gait, aliasKey, in progress, true);
        }

        AnimationMotionPlaybackWindow BuildMotionPlaybackWindow(
            BasicMovementPhase phase,
            BasicMovementGait gait,
            string aliasKey,
            in AnimationPhasePlaybackProgress progress,
            bool requireProgressPhase = false,
            bool sampleFromZeroOnNewPlayback = false)
        {
            if (!progress.HasValidPlayback || (requireProgressPhase && progress.Phase != phase))
            {
                ResetMotionPlaybackWindow();
                return AnimationMotionPlaybackWindow.Invalid(phase, gait);
            }

            bool samePlayback =
                hasPreviousMotionPlaybackProgress &&
                previousMotionPlaybackProgress.HasValidPlayback &&
                previousMotionPlaybackProgress.AliasKey == aliasKey &&
                progress.NormalizedTime >= previousMotionPlaybackProgress.NormalizedTime;

            float previousTime = samePlayback
                ? previousMotionPlaybackProgress.NormalizedTime
                : sampleFromZeroOnNewPlayback ? 0f : progress.NormalizedTime;
            previousMotionPlaybackProgress = progress;
            hasPreviousMotionPlaybackProgress = true;
            return new AnimationMotionPlaybackWindow(phase, gait, aliasKey, previousTime, progress.NormalizedTime, true);
        }

        string BuildLocomotionDiagnosticContext()
        {
            AnimationPhasePlaybackProgress progress = ResolvePlaybackProgress(CurrentPhase);
            string presenterName = locomotionPresenter != null ? locomotionPresenter.name : "null";
            string animationName = locomotionPresenter != null ? locomotionPresenter.CurrentAnimationName : string.Empty;

            return
                $"phase={CurrentPhase} gait={currentFrame.Command.Gait} phaseTime={CurrentPhaseTime:F3} " +
                $"hasMove={currentIntent.HasMoveIntent} strength={currentIntent.Strength:F3} " +
                $"rawMove={currentIntent.RawInput.ToString("F3")} normalizedMove={currentIntent.NormalizedInput.ToString("F3")} " +
                $"worldDirection={currentWorldDirection.ToString("F3")} planarSpeed={currentFrame.Command.PlanarSpeed:F3} rotationSpeed={currentFrame.Command.RotationSpeed:F3} " +
                $"runLatch={runLatchActive} motionSuppressed={suppressBasicMotionExecution} animationSuppressed={suppressLocomotionAnimationPresentation} " +
                $"hasAnimationMotion={currentFrame.Command.HasAnimationMotion} animMotionSourcePhase={currentFrame.Command.AnimationMotionSourcePhase} animMotionAlias={currentFrame.Command.AnimationMotionSourceAliasKey} " +
                $"animationPresenter={presenterName} animationPhase={progress.Phase} animationAlias={progress.AliasKey} animationName={animationName} " +
                $"animationNormalized={progress.NormalizedTime:F3} animationValid={progress.HasValidPlayback} animationEnded={progress.IsEnded}";
        }

        void ResetMotionPlaybackWindow()
        {
            previousMotionPlaybackProgress = AnimationPhasePlaybackProgress.Invalid(CurrentPhase);
            hasPreviousMotionPlaybackProgress = false;
        }

        void SeedMotionPlaybackWindow(in AnimationPhasePlaybackProgress progress)
        {
            if (!progress.HasValidPlayback)
            {
                ResetMotionPlaybackWindow();
                return;
            }

            previousMotionPlaybackProgress = progress;
            hasPreviousMotionPlaybackProgress = true;
        }

        void ResetRunLatchAfterIdle()
        {
            if (CurrentPhase != BasicMovementPhase.Idle || currentIntent.HasMoveIntent)
                return;

            if (runLatchActive || lastMovingGait != BasicMovementGait.Walk)
            {
                LocomotionDiagnostics.LogRunLatchResetAfterIdle(
                    ActiveStatePath,
                    CurrentPhase,
                    currentIntent.HasMoveIntent,
                    lastMovingGait,
                    runLatchActive,
                    CurrentAnimationName);
            }

            runLatchActive = false;
            lastMovingGait = BasicMovementGait.Walk;
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

        void LogTurnBackRootMotionConsumed(
            BasicMovementPhase phase,
            BasicMovementGait gait,
            string aliasKey,
            in AnimationMotionProfileSample bakedSample,
            in TurnBackMotionPolicy policy,
            in AnimationMotionPlaybackWindow playbackWindow,
            in Vector3 appliedPlanarDelta,
            float appliedYawDelta,
            BasicMovementPlanarDeltaSpace deltaSpace,
            Vector3 entryPlanarBasisForward,
            in StateTimelineWindowFacts timelineFacts)
        {
            LocomotionDiagnostics.LogTurnBackRootMotionConsumed(
                phase,
                gait,
                aliasKey,
                in bakedSample,
                in policy,
                in playbackWindow,
                in appliedPlanarDelta,
                appliedYawDelta,
                deltaSpace,
                entryPlanarBasisForward,
                in timelineFacts);
        }

        void LogTurnBackStatePolicy(
            int currentStep,
            BasicMovementPhase phase,
            BasicMovementGait gait,
            string aliasKey,
            in TurnBackMotionPolicy policy,
            Vector3 lockedWorldDirection,
            Vector3 entryPlanarBasisForward,
            in Vector3 planarDelta,
            float yawDelta,
            in StateTimelineWindowFacts timelineFacts)
        {
            AnimationPhasePlaybackProgress progress = ResolveCurrentAnimationPlaybackProgress();
            LocomotionDiagnostics.LogTurnBackStatePolicy(
                ActiveStatePath,
                currentStep,
                phase,
                gait,
                aliasKey,
                in policy,
                lockedWorldDirection,
                entryPlanarBasisForward,
                in planarDelta,
                yawDelta,
                in timelineFacts,
                in progress,
                transform.eulerAngles.y);
        }

        void LogTurnBackEntryBasisMissing(
            int currentStep,
            BasicMovementPhase phase,
            BasicMovementGait gait,
            string aliasKey,
            in Vector3 rejectedPlanarDelta)
        {
            LocomotionDiagnostics.LogTurnBackEntryBasisMissing(
                ActiveStatePath,
                currentStep,
                phase,
                gait,
                aliasKey,
                in rejectedPlanarDelta);
        }

        void LogTurnBackFrameSummary(
            int currentStep,
            BasicMovementPhase phaseBeforeTick,
            in LocomotionDecisionFacts facts,
            in CharacterStateMachineFrame stateFrame,
            in BasicMovementMotionFacts motionFacts,
            in BasicLocomotionFrame frame)
        {
            AnimationPhasePlaybackProgress progress = ResolveCurrentAnimationPlaybackProgress();
            LocomotionDiagnostics.LogTurnBackFrameSummary(
                currentStep,
                phaseBeforeTick,
                in facts,
                in stateFrame,
                in motionFacts,
                in frame,
                in progress);
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
            LocomotionDiagnostics.SubmitRetiredDirectTick(activeStatePath, step);
        }

        void LogFormalConfigMissing(string eventId, string message)
        {
            LocomotionDiagnostics.SubmitFormalConfigMissing(activeStatePath, eventId, message);
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

    }
}
