using ThirdPersonAnimation;
using ThirdPersonCamera;
using ThirdPersonCharacterConfig;
using ThirdPersonCharacterStateMachine;
using ThirdPersonDiagnostics;
using ThirdPersonInput;
using ThirdPersonSimulation;
using UnityEngine;
using UnityEngine.Serialization;

namespace ThirdPersonMovement
{
    [DisallowMultipleComponent]
    public sealed class PlayerLocomotionController : MonoBehaviour
    {
        const string TurnBackRootMotionLogKeyword = "TURNBACK_RM_CHAIN";
        const string TurnBackDirectionDebugChannel = "Locomotion.turnback-direction-debug";
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
        const float TurnBackIntentMinAngle = 120f;
        const int TurnBackIntentWindowSteps = 2;

        readonly BasicLocomotionPipeline pipeline = new BasicLocomotionPipeline();
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
                 RuntimeDiagnosticLog.Submit(new RuntimeDiagnosticLogEvent(
                     RuntimeDiagnosticLogCategory.Locomotion,
                     RuntimeDiagnosticLogLevel.Error,
                     "legacy-player-enabled",
                     "",
                     "",
                     0,
                     Time.frameCount,
                     "Legacy Player path is enabled. Player locomotion is disabled to avoid double movement input."));
                enabled = false;
                return;
            }

            if (inputSource == null)
            {
                 RuntimeDiagnosticLog.Submit(new RuntimeDiagnosticLogEvent(
                     RuntimeDiagnosticLogCategory.Locomotion,
                     RuntimeDiagnosticLogLevel.Error,
                     "input-source-missing",
                     "",
                     "",
                     0,
                     Time.frameCount,
                     "Locomotion input source is missing. Player locomotion cannot read movement input."));
                enabled = false;
                return;
            }

            if (motionExecutor == null)
            {
                 RuntimeDiagnosticLog.Submit(new RuntimeDiagnosticLogEvent(
                     RuntimeDiagnosticLogCategory.Locomotion,
                     RuntimeDiagnosticLogLevel.Error,
                     "motion-executor-missing",
                     "",
                     "",
                     0,
                     Time.frameCount,
                     "Locomotion motion executor is missing. Player locomotion cannot enter the main movement path."));
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

            BasicLocomotionInputSnapshot input = decisionFrame.Input;
            MovementInputIntent pendingIntent = decisionFrame.Intent;
            BasicMovementPhase currentPhase = runner.Snapshot.LocomotionPhase;
            BasicMovementGait frameGait = decisionFrame.FrameGait;
            BasicMovementPhaseFacts phaseFacts = decisionFrame.PhaseFacts;
            LocomotionDecisionFacts decisionFacts = decisionFrame.Facts;
            CharacterRuntimeBlackboardSnapshot blackboardBeforeTick = runtimeBlackboard.Snapshot;
            CharacterStateMachineContext context = BuildStateMachineContext(
                in input,
                currentStep,
                in decisionFacts,
                in inputRequest,
                in blackboardBeforeTick);
            bool runLatchBeforeStateTick = runLatchActive;
            CharacterStateMachineFrame stateFrame = runner.Tick(in context);
            ConsumeTurnBackIntentIfEntered(in decisionFacts, in stateFrame, currentStep);
            ApplyStateMachineOutputs(in stateFrame);
            stateDecision = new LocomotionStateDecisionFrame(
                decisionFrame,
                stateFrame,
                currentPhase,
                frameGait,
                pendingIntent,
                phaseFacts,
                decisionFacts,
                blackboardBeforeTick,
                runLatchBeforeStateTick);
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

            LocomotionDecisionFrame decisionFrame = stateDecision.DecisionFrame;
            LocomotionDecisionFacts decisionFacts = stateDecision.DecisionFacts;
            stateFrame = stateDecision.StateFrame;
            BasicLocomotionInputSnapshot input = decisionFrame.Input;
            BasicMovementGait frameGait = stateDecision.FrameGait;
            CharacterRuntimeBlackboardSnapshot blackboardBeforeTick = stateDecision.BlackboardBeforeTick;
            BasicMovementMotionFacts motionFacts = ResolveMotionFacts(in stateFrame, frameGait, currentStep);
            BasicMovementSettings settings = decisionFrame.Settings;
            LocomotionDecisionFacts motionDecisionFacts = ResolveMotionDecisionFacts(in decisionFacts, in stateFrame);
            currentFrame = pipeline.Tick(in input, in settings, in motionDecisionFacts, stateFrame.LocomotionPhase, motionFacts, frameGait);
            currentPhaseTime = stateFrame.Snapshot.StateTime;
            activeStatePath = stateFrame.Snapshot.ActivePath;
            currentIntent = currentFrame.Intent;
            UpdatePhaseGaitMemory(stateFrame.LocomotionPhase, frameGait);
            LogStateMachineOutputProbe(
                currentStep,
                stateDecision.PhaseBeforeTick,
                stateDecision.FrameGait,
                stateDecision.PendingIntent,
                stateDecision.PhaseFacts,
                stateDecision.RunLatchBeforeStateTick,
                in stateFrame);
            LogTurnBackFrameSummary(
                currentStep,
                stateDecision.PhaseBeforeTick,
                in decisionFacts,
                in stateFrame,
                in motionFacts,
                in currentFrame);
            if (currentIntent.HasMoveIntent)
                lastMovingGait = currentIntent.Gait;

            currentWorldDirection = currentFrame.WorldDirection;
            WriteLocomotionFacts(in currentFrame, in stateFrame, in blackboardBeforeTick, currentStep);
            UpdatePreviousWorldDirection(in currentFrame);
            frame = currentFrame;
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

        BasicMovementGait ResolveFrameGait(BasicMovementPhase currentPhase, in MovementInputIntent pendingIntent)
        {
            if (pendingIntent.HasMoveIntent)
                return pendingIntent.Gait;

            if (currentPhase == BasicMovementPhase.MoveStop && hasActiveMoveStopGait)
                return activeMoveStopGait;

            return lastMovingGait;
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
            MovementInputIntent pendingIntent = ResolveMovementIntent(in input, in baseSettings);
            BasicMovementPhase currentPhase = runner.Snapshot.LocomotionPhase;
            BasicMovementGait frameGait = ResolveFrameGait(currentPhase, in pendingIntent);
            BasicMovementSettings settings = ResolveMovementSettings(frameGait, in baseSettings);
            BasicMovementPhaseFacts phaseFacts = ResolvePhaseFacts(currentPhase, runner.StateTime, frameGait, input.DeltaTime, in settings);
            bool wantsRun = pendingIntent.HasMoveIntent && pendingIntent.Gait == BasicMovementGait.Run || input.RunHeld || runLatchActive;
            BasicLocomotionInputSnapshot resolvedInput = new BasicLocomotionInputSnapshot(
                input.DeltaTime,
                input.Move,
                input.Look,
                wantsRun);
            LocomotionSpatialFacts spatialFacts = ResolveSpatialFacts(in input, in pendingIntent);
            LocomotionDecisionFacts decisionFacts = DeriveLocomotionDecisionFacts(
                in pendingIntent,
                frameGait,
                currentPhase,
                in phaseFacts,
                in spatialFacts,
                currentStep);
            decisionFrame = new LocomotionDecisionFrame(
                resolvedInput,
                settings,
                pendingIntent,
                decisionFacts,
                frameGait);
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

        MovementInputIntent ResolveMovementIntent(in BasicLocomotionInputSnapshot input, in BasicMovementSettings baseSettings)
        {
            bool wantsRun = input.RunHeld || runLatchActive;
            return MovementInputIntent.FromRaw(input.Move, baseSettings.InputDeadZone, wantsRun);
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

            return new LocomotionSpatialFacts(
                CameraRelativeMovementResolver.Resolve(intent, rollbackCameraBasisProvider),
                ResolveFacingForward(),
                rollbackCameraBasisProvider.CameraPlanarForward,
                rollbackCameraBasisProvider.CameraPlanarRight);
        }

        LocomotionDecisionFacts DeriveLocomotionDecisionFacts(
            in MovementInputIntent intent,
            BasicMovementGait frameGait,
            BasicMovementPhase currentPhase,
            in BasicMovementPhaseFacts phaseFacts,
            in LocomotionSpatialFacts spatialFacts,
            int currentStep)
        {
            LocomotionTurnBackIntent turnBackIntent = ResolveTurnBackIntent(
                in intent,
                frameGait,
                currentPhase,
                in spatialFacts,
                currentStep);
            LocomotionDecisionFacts facts = new LocomotionDecisionFacts(
                intent,
                frameGait,
                phaseFacts,
                spatialFacts,
                turnBackIntent);
            LogLocomotionDecisionFacts(currentStep, currentPhase, in facts);
            return facts;
        }

        LocomotionTurnBackIntent ResolveTurnBackIntent(
            in MovementInputIntent intent,
            BasicMovementGait frameGait,
            BasicMovementPhase currentPhase,
            in LocomotionSpatialFacts spatialFacts,
            int currentStep)
        {
            if (currentPhase == BasicMovementPhase.TurnBack)
            {
                ClearTurnBackIntent("already-turnback", currentStep);
                return LocomotionTurnBackIntent.None;
            }

            if (!intent.HasMoveIntent)
            {
                if (frameGait == BasicMovementGait.Run && pendingTurnBackIntent.IsValidAt(currentStep))
                {
                    LogTurnBackIntent("hold-empty-input-window", currentStep, pendingTurnBackIntent);
                    return pendingTurnBackIntent;
                }

                ClearTurnBackIntent("no-move-or-expired", currentStep);
                return LocomotionTurnBackIntent.None;
            }

            if (frameGait != BasicMovementGait.Run || intent.Gait != BasicMovementGait.Run)
            {
                ClearTurnBackIntent("not-run-gait", currentStep);
                return LocomotionTurnBackIntent.None;
            }

            if (!spatialFacts.HasWorldMoveDirection)
            {
                ClearTurnBackIntent("missing-spatial-facts", currentStep);
                return LocomotionTurnBackIntent.None;
            }

            if (pendingTurnBackIntent.IsValidAt(currentStep) &&
                Vector3.Angle(pendingTurnBackIntent.WorldMoveDirection, spatialFacts.WorldMoveDirection) <= 20f)
            {
                LogTurnBackIntent("hold-existing-reverse-input", currentStep, pendingTurnBackIntent);
                return pendingTurnBackIntent;
            }

            if (!TryResolveTurnBackReferenceFacing(currentPhase, in spatialFacts, out Vector3 referenceFacing))
            {
                ClearTurnBackIntent("missing-facing-reference", currentStep);
                return LocomotionTurnBackIntent.None;
            }

            float angle = Vector3.Angle(referenceFacing, spatialFacts.WorldMoveDirection);
            if (angle >= TurnBackIntentMinAngle)
            {
                pendingTurnBackIntent = LocomotionTurnBackIntent.Capture(
                    currentStep,
                    TurnBackIntentWindowSteps,
                    angle,
                    TurnBackIntentMinAngle,
                    spatialFacts.WorldMoveDirection,
                    referenceFacing);
                LogTurnBackIntent("captured", currentStep, pendingTurnBackIntent);
                return pendingTurnBackIntent;
            }

            ClearTurnBackIntent("angle-below-threshold", currentStep, angle);
            return LocomotionTurnBackIntent.None;
        }

        bool TryResolveTurnBackReferenceFacing(
            BasicMovementPhase currentPhase,
            in LocomotionSpatialFacts spatialFacts,
            out Vector3 referenceFacing)
        {
            if (currentPhase != BasicMovementPhase.MoveLoop)
                return TryNormalizePlanar(previousWorldDirection, out referenceFacing);

            if (TryNormalizePlanar(previousWorldDirection, out Vector3 previousDirection) &&
                spatialFacts.HasWorldMoveDirection &&
                Vector3.Angle(previousDirection, spatialFacts.WorldMoveDirection) >= TurnBackIntentMinAngle)
            {
                referenceFacing = previousDirection;
                return true;
            }

            if (spatialFacts.HasFacingForward)
            {
                referenceFacing = spatialFacts.FacingForward;
                return true;
            }

            referenceFacing = Vector3.zero;
            return false;
        }

        CharacterStateMachineContext BuildStateMachineContext(
            in BasicLocomotionInputSnapshot input,
            int currentStep,
            in LocomotionDecisionFacts decisionFacts,
            in CharacterInputRequestFact inputRequest,
            in CharacterRuntimeBlackboardSnapshot blackboardBeforeTick)
        {
            return new CharacterStateMachineContext(
                input.DeltaTime,
                currentStep,
                in decisionFacts,
                inputRequest,
                blackboardBeforeTick);
        }

        void ConsumeTurnBackIntentIfEntered(
            in LocomotionDecisionFacts decisionFacts,
            in CharacterStateMachineFrame stateFrame,
            int currentStep)
        {
            if (stateFrame.LocomotionPhase != BasicMovementPhase.TurnBack)
                return;

            if (!decisionFacts.TurnBackIntent.IsValid)
                return;

            LogTurnBackIntent("consumed-enter-turnback", currentStep, decisionFacts.TurnBackIntent);
            pendingTurnBackIntent = LocomotionTurnBackIntent.None;
        }


        public void WriteActionFacts(in CharacterRuntimeActionFacts facts)
        {
            runtimeBlackboard.WriteActionFacts(in facts);
        }

        public void WriteAnimationFacts(in CharacterRuntimeAnimationFacts facts)
        {
            runtimeBlackboard.WriteAnimationFacts(in facts);
        }

        void WriteLocomotionAnimationFacts(int sourceStep)
        {
            CharacterRuntimeAnimationFacts previous = runtimeBlackboard.Snapshot.Animation;
            runtimeBlackboard.WriteAnimationFacts(new CharacterRuntimeAnimationFacts(
                CurrentAnimationPlaybackProgress,
                CurrentAnimationName,
                previous.ActionProgress,
                previous.ActionAnimationName,
                sourceStep));
        }

        void WriteLocomotionFacts(
            in BasicLocomotionFrame frame,
            in CharacterStateMachineFrame stateFrame,
            in CharacterRuntimeBlackboardSnapshot previousBlackboard,
            int sourceStep)
        {
            runtimeBlackboard.WriteLocomotionFacts(new CharacterRuntimeLocomotionFacts(
                stateFrame.LocomotionPhase,
                frame.Command.Gait,
                lastMovingGait,
                hasActiveMoveStopGait,
                activeMoveStopGait,
                runLatchActive,
                frame.WorldDirection,
                frame.Intent.HasMoveIntent,
                frame.Intent.Strength,
                sourceStep));
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
            LocomotionRuntimeRollbackState locomotionRuntimeState = new LocomotionRuntimeRollbackState(
                currentIntent,
                previousWorldDirection,
                previousMotionPlaybackProgress,
                hasPreviousMotionPlaybackProgress,
                hasActiveMoveStopGait,
                activeMoveStopGait,
                pendingTurnBackIntent);
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
            previousWorldDirection = locomotionState.PreviousWorldDirection;
            pendingTurnBackIntent = locomotionState.PendingTurnBackIntent;
            hasActiveMoveStopGait = locomotionState.HasActiveMoveStopGait;
            activeMoveStopGait = locomotionState.ActiveMoveStopGait;
            currentIntent = locomotionState.CurrentIntent;
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
            if (locomotionState.HasPreviousMotionPlaybackProgress)
            {
                previousMotionPlaybackProgress = locomotionState.PreviousMotionPlaybackProgress;
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

        void UpdatePhaseGaitMemory(BasicMovementPhase phase, BasicMovementGait frameGait)
        {
            if (phase == BasicMovementPhase.MoveStop)
            {
                activeMoveStopGait = frameGait;
                hasActiveMoveStopGait = true;
                return;
            }

            if (phase != BasicMovementPhase.TurnBack)
                hasActiveMoveStopGait = false;
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
            RuntimeDiagnosticLog.Submit(new RuntimeDiagnosticLogEvent(
                RuntimeDiagnosticLogCategory.Locomotion,
                RuntimeDiagnosticLogLevel.Trace,
                "locomotion-tick-snapshot",
                ActiveStatePath,
                string.Empty,
                step,
                Time.frameCount,
                BuildLocomotionDiagnosticContext()));
        }

        static MovementAnimationContext BuildAnimationContext(in BasicLocomotionFrame frame, float planarSpeed)
        {
            return new MovementAnimationContext(
                frame.Phase,
                frame.Command.Gait,
                frame.Intent.HasMoveIntent,
                frame.Intent.Strength,
                frame.WorldDirection,
                planarSpeed,
                frame.Command.TurnBackMotionPolicy,
                frame.Command.HasTurnBackMotionPolicy);
        }

        void UpdatePreviousWorldDirection(in BasicLocomotionFrame frame)
        {
            if (!frame.Intent.HasMoveIntent)
                return;

            if (TryNormalizePlanar(frame.WorldDirection, out Vector3 direction))
                previousWorldDirection = direction;
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
            bool hasTimelineFacts = timelineFacts.StateId == CharacterStateIds.TurnBack;
            bool motionWindowActive = !hasTimelineFacts || timelineFacts.MotionWindowActive;
            bool inputLockActive = hasTimelineFacts ? timelineFacts.InputLockWindowActive : policy.SuppressInputRotation || policy.SuppressInputPlanarMovement;
            AnimationPhasePlaybackProgress progress = ResolvePlaybackProgress(phase);
            AnimationMotionPlaybackWindow playbackWindow = BuildMotionPlaybackWindow(
                phase,
                gait,
                aliasKey,
                in progress,
                true,
                true);
            AnimationMotionProfileSample bakedSample = RequiresTurnBackBakedMotion(in policy)
                ? ResolveTurnBackBakedMotionSample(
                    animationConfig,
                    phase,
                    gait,
                    aliasKey,
                    in playbackWindow)
                : AnimationMotionProfileSample.None(phase);
            BasicMovementPlanarDeltaSpace deltaSpace = policy.TranslationSource == TurnBackMotionTranslationSource.BakedMotionProfile
                ? BasicMovementPlanarDeltaSpace.EntryLocal
                : BasicMovementPlanarDeltaSpace.World;
            bool entryBasisValid = deltaSpace != BasicMovementPlanarDeltaSpace.EntryLocal ||
                                   TryNormalizePlanar(entryPlanarBasisForward, out entryPlanarBasisForward);
            Vector3 planarDelta = motionWindowActive
                ? ResolveTurnBackPlanarDelta(in policy, in bakedSample)
                : Vector3.zero;
            if (deltaSpace == BasicMovementPlanarDeltaSpace.EntryLocal &&
                planarDelta.sqrMagnitude > 0.000001f &&
                !entryBasisValid)
            {
                LogTurnBackEntryBasisMissing(currentStep, phase, gait, aliasKey, in planarDelta);
                planarDelta = Vector3.zero;
            }

            float appliedYawDelta = motionWindowActive
                ? ResolveTurnBackYawDelta(in policy, in bakedSample)
                : 0f;
            bool hasMotion = planarDelta.sqrMagnitude > 0.000001f || Mathf.Abs(appliedYawDelta) > 0.0001f;
            LogTurnBackRootMotionConsumed(
                phase,
                gait,
                aliasKey,
                in bakedSample,
                in policy,
                in playbackWindow,
                in planarDelta,
                appliedYawDelta,
                deltaSpace,
                entryPlanarBasisForward,
                in timelineFacts);
            LogTurnBackStatePolicy(currentStep, phase, gait, aliasKey, in policy, lockedWorldDirection, entryPlanarBasisForward, in planarDelta, appliedYawDelta, in timelineFacts);
            return new BasicMovementMotionFacts(
                hasMotion,
                planarDelta,
                appliedYawDelta,
                phase,
                aliasKey,
                inputLockActive && policy.SuppressInputRotation,
                inputLockActive && policy.SuppressInputPlanarMovement,
                deltaSpace,
                policy,
                entryPlanarBasisForward);
        }

        static bool RequiresTurnBackBakedMotion(in TurnBackMotionPolicy policy)
        {
            return policy.YawSource == TurnBackMotionYawSource.BakedMotionProfile ||
                   policy.TranslationSource == TurnBackMotionTranslationSource.BakedMotionProfile;
        }

        static Vector3 ResolveTurnBackPlanarDelta(
            in TurnBackMotionPolicy policy,
            in AnimationMotionProfileSample bakedSample)
        {
            switch (policy.TranslationSource)
            {
                case TurnBackMotionTranslationSource.BakedMotionProfile:
                    return bakedSample.HasMotionContribution ? bakedSample.LocalPlanarDelta : Vector3.zero;
                default:
                    return Vector3.zero;
            }
        }

        static float ResolveTurnBackYawDelta(
            in TurnBackMotionPolicy policy,
            in AnimationMotionProfileSample bakedSample)
        {
            switch (policy.YawSource)
            {
                case TurnBackMotionYawSource.BakedMotionProfile:
                    return bakedSample.HasMotionContribution ? bakedSample.YawDelta : 0f;
                default:
                    return 0f;
            }
        }

        LocomotionDecisionFacts ResolveMotionDecisionFacts(
            in LocomotionDecisionFacts decisionFacts,
            in CharacterStateMachineFrame stateFrame)
        {
            if (stateFrame.LocomotionPhase != BasicMovementPhase.TurnBack ||
                !stateFrame.HasTurnBackMotionPolicy ||
                !TryNormalizePlanar(stateFrame.TurnBackWorldDirection, out Vector3 lockedDirection))
            {
                return decisionFacts;
            }

            LocomotionSpatialFacts spatialFacts = new LocomotionSpatialFacts(
                lockedDirection,
                decisionFacts.SpatialFacts.FacingForward,
                decisionFacts.SpatialFacts.CameraPlanarForward,
                decisionFacts.SpatialFacts.CameraPlanarRight);
            return new LocomotionDecisionFacts(
                decisionFacts.MoveIntent,
                decisionFacts.GaitCandidate,
                decisionFacts.PhaseFacts,
                spatialFacts,
                decisionFacts.TurnBackIntent);
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
                RuntimeDiagnosticLog.Submit(new RuntimeDiagnosticLogEvent(
                    RuntimeDiagnosticLogCategory.Locomotion,
                    RuntimeDiagnosticLogLevel.Info,
                    "locomotion-run-latch-reset-after-idle",
                    ActiveStatePath,
                    string.Empty,
                    0,
                    Time.frameCount,
                    $"phase={CurrentPhase} intentHasMove={currentIntent.HasMoveIntent} lastMovingGait={lastMovingGait} runLatchBefore={runLatchActive} animation={CurrentAnimationName}"));
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


        void ApplyStateMachineOutputs(in CharacterStateMachineFrame stateFrame)
        {
            bool previousRunLatch = runLatchActive;

            if (stateFrame.ResetRunLatch)
                SetRunLatchActive(false);

            if (stateFrame.SetRunLatch)
                SetRunLatchActive(true);

            if (previousRunLatch != runLatchActive || stateFrame.SetRunLatch || stateFrame.ResetRunLatch)
            {
                RuntimeDiagnosticLog.Submit(new RuntimeDiagnosticLogEvent(
                    RuntimeDiagnosticLogCategory.Locomotion,
                    RuntimeDiagnosticLogLevel.Info,
                    "locomotion-run-latch-output-applied",
                    stateFrame.Snapshot.ActivePath,
                    string.Empty,
                    0,
                    Time.frameCount,
                    $"setOutput={stateFrame.SetRunLatch} resetOutput={stateFrame.ResetRunLatch} before={previousRunLatch} after={runLatchActive} statePhase={stateFrame.LocomotionPhase} stateGait={stateFrame.Snapshot.Variant} actionCompleted={stateFrame.ActionCompleted}"));
            }
        }

        void LogStateMachineOutputProbe(
            int currentStep,
            BasicMovementPhase phaseBeforeTick,
            BasicMovementGait frameGait,
            in MovementInputIntent pendingIntent,
            in BasicMovementPhaseFacts phaseFacts,
            bool runLatchBeforeTick,
            in CharacterStateMachineFrame stateFrame)
        {
            if (!runLatchBeforeTick &&
                !runLatchActive &&
                !stateFrame.SetRunLatch &&
                !stateFrame.ResetRunLatch &&
                stateFrame.LocomotionPhase != BasicMovementPhase.MoveStop &&
                frameGait != BasicMovementGait.Run)
            {
                return;
            }

            RuntimeDiagnosticLog.Submit(new RuntimeDiagnosticLogEvent(
                RuntimeDiagnosticLogCategory.Locomotion,
                RuntimeDiagnosticLogLevel.Trace,
                "locomotion-state-machine-output-probe",
                stateFrame.Snapshot.ActivePath,
                string.Empty,
                currentStep,
                Time.frameCount,
                $"phaseBefore={phaseBeforeTick} statePhase={stateFrame.LocomotionPhase} frameGait={frameGait} hasMove={pendingIntent.HasMoveIntent} pendingGait={pendingIntent.Gait} phaseCanExit={phaseFacts.PhaseCanExit} setRunLatch={stateFrame.SetRunLatch} resetRunLatch={stateFrame.ResetRunLatch} runLatchBeforeTick={runLatchBeforeTick} runLatchAfterOutput={runLatchActive} lastMovingGait={lastMovingGait} hasMoveStopGait={hasActiveMoveStopGait} moveStopGait={activeMoveStopGait} executeBasic={stateFrame.ExecuteBasicMovement} presentLocomotion={stateFrame.PresentLocomotionAnimation}"));
        }

        void LogLocomotionDecisionFacts(
            int currentStep,
            BasicMovementPhase phaseBeforeTick,
            in LocomotionDecisionFacts facts)
        {
            RuntimeDiagnosticLog.Submit(new RuntimeDiagnosticLogEvent(
                RuntimeDiagnosticLogCategory.Locomotion,
                RuntimeDiagnosticLogLevel.Trace,
                "locomotion-decision-pipeline",
                ActiveStatePath,
                string.Empty,
                currentStep,
                Time.frameCount,
                $"phaseBefore={phaseBeforeTick} hasMove={facts.HasMoveIntent} gaitCandidate={facts.GaitCandidate} " +
                $"rawMove={facts.MoveIntent.RawInput.ToString("F3")} normalizedMove={facts.MoveIntent.NormalizedInput.ToString("F3")} strength={facts.MoveIntent.Strength:F3} " +
                $"worldMove={facts.SpatialFacts.WorldMoveDirection.ToString("F3")} facing={facts.SpatialFacts.FacingForward.ToString("F3")} " +
                $"cameraForward={facts.SpatialFacts.CameraPlanarForward.ToString("F3")} cameraRight={facts.SpatialFacts.CameraPlanarRight.ToString("F3")} " +
                $"phaseCanExit={facts.PhaseFacts.PhaseCanExit} turnBackValid={facts.TurnBackIntent.IsValidAt(currentStep)} " +
                $"turnBackAngle={facts.TurnBackIntent.Angle:F3} turnBackThreshold={facts.TurnBackIntent.Threshold:F3} " +
                $"turnBackOrigin={facts.TurnBackIntent.OriginStep} turnBackExpire={facts.TurnBackIntent.ExpireStep}"));
        }

        void ClearTurnBackIntent(string reason, int currentStep, float angle = -1f)
        {
            if (pendingTurnBackIntent.IsValid)
                LogTurnBackIntent(reason, currentStep, pendingTurnBackIntent, angle);

            pendingTurnBackIntent = LocomotionTurnBackIntent.None;
        }

        void LogTurnBackIntent(
            string reason,
            int currentStep,
            in LocomotionTurnBackIntent intent,
            float observedAngle = -1f)
        {
            RuntimeDiagnosticLog.Submit(new RuntimeDiagnosticLogEvent(
                RuntimeDiagnosticLogCategory.Locomotion,
                RuntimeDiagnosticLogLevel.Trace,
                "locomotion-turnback-intent",
                ActiveStatePath,
                string.Empty,
                currentStep,
                Time.frameCount,
                $"reason={reason} valid={intent.IsValidAt(currentStep)} rawValid={intent.IsValid} origin={intent.OriginStep} expire={intent.ExpireStep} " +
                $"angle={intent.Angle:F3} observedAngle={observedAngle:F3} threshold={intent.Threshold:F3} " +
                $"worldMove={intent.WorldMoveDirection.ToString("F3")} facing={intent.FacingForward.ToString("F3")}"));
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
            Vector3 entryPlanarBasisRight = ResolvePlanarRightOrZero(entryPlanarBasisForward);
            RuntimeDiagnosticLog.Submit(new RuntimeDiagnosticLogEvent(
                RuntimeDiagnosticLogCategory.Locomotion,
                RuntimeDiagnosticLogLevel.Info,
                "turnback-root-motion-consumed",
                aliasKey,
                string.Empty,
                0,
                Time.frameCount,
                $"[{TurnBackRootMotionLogKeyword}] stage=controller phase={phase} gait={gait} alias={aliasKey} " +
                $"bakedMotion={bakedSample.HasMotionContribution} bakedAlias={bakedSample.SourceAliasKey} bakedLocalDelta={bakedSample.LocalPlanarDelta.ToString("F3")} bakedYawDelta={bakedSample.YawDelta:F3} " +
                $"playbackWindow={playbackWindow.HasValidPlayback}/{playbackWindow.PreviousNormalizedTime:F3}->{playbackWindow.CurrentNormalizedTime:F3} " +
                $"appliedTranslationSource={policy.TranslationSource} appliedPlanarDelta={appliedPlanarDelta.ToString("F3")} appliedYawSource={policy.YawSource} yawDelta={appliedYawDelta:F3} deltaSpace={deltaSpace} entryBasisForward={entryPlanarBasisForward.ToString("F3")} entryBasisRight={entryPlanarBasisRight.ToString("F3")} " +
                $"turnComplete={policy.TurnCompleteNormalizedTime:F3} suppressInputRotation={policy.SuppressInputRotation} suppressInputPlanarMovement={policy.SuppressInputPlanarMovement} bakedProfile={policy.BakedMotionProfileId} " +
                $"timelineMotion={timelineFacts.MotionWindowActive} timelineInputLock={timelineFacts.InputLockWindowActive} timelineInterrupt={timelineFacts.InterruptWindowActive} timelineExit={timelineFacts.ExitWindowActive} timelineWindows={timelineFacts.ActiveWindowIds}",
                TurnBackDirectionDebugChannel));
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
            float lockedYaw = TryNormalizePlanar(lockedWorldDirection, out Vector3 lockedDirection)
                ? Quaternion.LookRotation(lockedDirection, Vector3.up).eulerAngles.y
                : 0f;
            float entryYaw = TryNormalizePlanar(entryPlanarBasisForward, out Vector3 entryDirection)
                ? Quaternion.LookRotation(entryDirection, Vector3.up).eulerAngles.y
                : 0f;
            bool canExit = progress.HasValidPlayback && progress.NormalizedTime >= policy.TurnCompleteNormalizedTime;
            RuntimeDiagnosticLog.Submit(new RuntimeDiagnosticLogEvent(
                RuntimeDiagnosticLogCategory.Locomotion,
                RuntimeDiagnosticLogLevel.Info,
                "locomotion-turnback-state-policy",
                ActiveStatePath,
                aliasKey,
                currentStep,
                Time.frameCount,
                $"[{TurnBackRootMotionLogKeyword}] stage=policy phase={phase} gait={gait} alias={aliasKey} entryPhase={policy.EntryPhase} entryGait={policy.EntryGait} " +
                $"lockedDirection={lockedWorldDirection.ToString("F3")} lockedYaw={lockedYaw:F3} entryBasisForward={entryPlanarBasisForward.ToString("F3")} entryYaw={entryYaw:F3} currentYaw={transform.eulerAngles.y:F3} " +
                $"yawSource={policy.YawSource} translationSource={policy.TranslationSource} planarDelta={planarDelta.ToString("F3")} yawDelta={yawDelta:F3} " +
                $"suppressInputRotation={policy.SuppressInputRotation} suppressInputPlanarMovement={policy.SuppressInputPlanarMovement} " +
                $"startNormalized={policy.StartNormalizedTime:F3} lockInputNormalized={policy.LockInputNormalizedTime:F3} exitNormalized={policy.ExitNormalizedTime:F3} turnComplete={policy.TurnCompleteNormalizedTime:F3} " +
                $"progressAlias={progress.AliasKey} progressNormalized={progress.NormalizedTime:F3} progressValid={progress.HasValidPlayback} progressEnded={progress.IsEnded} canExit={canExit} bakedProfile={policy.BakedMotionProfileId} " +
                $"timelineNormalized={timelineFacts.NormalizedTime:F3} timelineNormalizedValid={timelineFacts.HasValidNormalizedTime} timelineElapsed={timelineFacts.ElapsedSeconds:F3} timelineMotion={timelineFacts.MotionWindowActive} timelineInputLock={timelineFacts.InputLockWindowActive} timelineInterrupt={timelineFacts.InterruptWindowActive} timelineExit={timelineFacts.ExitWindowActive} timelineWindows={timelineFacts.ActiveWindowIds}",
                TurnBackDirectionDebugChannel));
        }

        void LogTurnBackEntryBasisMissing(
            int currentStep,
            BasicMovementPhase phase,
            BasicMovementGait gait,
            string aliasKey,
            in Vector3 rejectedPlanarDelta)
        {
            RuntimeDiagnosticLog.Submit(new RuntimeDiagnosticLogEvent(
                RuntimeDiagnosticLogCategory.Locomotion,
                RuntimeDiagnosticLogLevel.Warning,
                "turnback-entry-basis-missing",
                ActiveStatePath,
                aliasKey,
                currentStep,
                Time.frameCount,
                $"[{TurnBackRootMotionLogKeyword}] stage=controller-entry-basis-missing phase={phase} gait={gait} alias={aliasKey} deltaSpace={BasicMovementPlanarDeltaSpace.EntryLocal} rejectedPlanarDelta={rejectedPlanarDelta.ToString("F3")}"));
        }

        static Vector3 ResolvePlanarRightOrZero(Vector3 forward)
        {
            return TryNormalizePlanar(forward, out Vector3 normalizedForward)
                ? Vector3.Cross(Vector3.up, normalizedForward).normalized
                : Vector3.zero;
        }

        void LogTurnBackFrameSummary(
            int currentStep,
            BasicMovementPhase phaseBeforeTick,
            in LocomotionDecisionFacts facts,
            in CharacterStateMachineFrame stateFrame,
            in BasicMovementMotionFacts motionFacts,
            in BasicLocomotionFrame frame)
        {
            bool relevant =
                phaseBeforeTick == BasicMovementPhase.TurnBack ||
                stateFrame.LocomotionPhase == BasicMovementPhase.TurnBack ||
                facts.TurnBackIntent.IsValidAt(currentStep) ||
                motionFacts.SourcePhase == BasicMovementPhase.TurnBack;

            if (!relevant)
                return;

            MovementCommand command = frame.Command;
            AnimationPhasePlaybackProgress progress = ResolveCurrentAnimationPlaybackProgress();
            RuntimeDiagnosticLog.Submit(new RuntimeDiagnosticLogEvent(
                RuntimeDiagnosticLogCategory.Locomotion,
                RuntimeDiagnosticLogLevel.Info,
                "turnback-frame-summary",
                stateFrame.Snapshot.ActivePath,
                progress.AliasKey,
                currentStep,
                Time.frameCount,
                $"phaseBefore={phaseBeforeTick} statePhase={stateFrame.LocomotionPhase} stateTime={stateFrame.Snapshot.StateTime:F3} " +
                $"hasMove={facts.HasMoveIntent} gaitCandidate={facts.GaitCandidate} commandGait={command.Gait} " +
                $"worldMove={facts.SpatialFacts.WorldMoveDirection.ToString("F3")} facing={facts.SpatialFacts.FacingForward.ToString("F3")} desiredFacing={command.DesiredFacing.ToString("F3")} " +
                $"turnBackValid={facts.TurnBackIntent.IsValidAt(currentStep)} turnBackRawValid={facts.TurnBackIntent.IsValid} turnBackAngle={facts.TurnBackIntent.Angle:F3} turnBackThreshold={facts.TurnBackIntent.Threshold:F3} " +
                $"turnBackOrigin={facts.TurnBackIntent.OriginStep} turnBackExpire={facts.TurnBackIntent.ExpireStep} executeBasic={stateFrame.ExecuteBasicMovement} presentLocomotion={stateFrame.PresentLocomotionAnimation} " +
                $"hasAnimationMotion={command.HasAnimationMotion} animationAlias={command.AnimationMotionSourceAliasKey} animationDeltaSpace={command.AnimationPlanarDeltaSpace} animationDelta={command.AnimationLocalPlanarDelta.ToString("F3")} animationBasisForward={command.AnimationPlanarBasisForward.ToString("F3")} animationYawDelta={command.AnimationYawDelta:F3} " +
                $"suppressInputRotation={command.SuppressInputRotation} suppressInputPlanarMovement={command.SuppressInputPlanarMovement} planarSpeed={command.PlanarSpeed:F3} rotationSpeed={command.RotationSpeed:F3} deltaTime={command.DeltaTime:F3} " +
                $"animationProgressAlias={progress.AliasKey} animationProgressPhase={progress.Phase} animationNormalized={progress.NormalizedTime:F3} animationValid={progress.HasValidPlayback} animationEnded={progress.IsEnded}",
                TurnBackDirectionDebugChannel));
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
            RuntimeDiagnosticLog.Submit(new RuntimeDiagnosticLogEvent(
                RuntimeDiagnosticLogCategory.Locomotion,
                RuntimeDiagnosticLogLevel.Error,
                "locomotion-direct-driver-retired",
                activeStatePath,
                string.Empty,
                step,
                Time.frameCount,
                "PlayerLocomotionController direct gameplay tick is retired. Drive locomotion through PlayerFullBodyActionController and FullBodyFramePipeline."));
        }

        void LogFormalConfigMissing(string eventId, string message)
        {
            RuntimeDiagnosticLog.Submit(new RuntimeDiagnosticLogEvent(
                RuntimeDiagnosticLogCategory.Locomotion,
                RuntimeDiagnosticLogLevel.Error,
                eventId,
                activeStatePath,
                string.Empty,
                0,
                Time.frameCount,
                message));
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

            if (TryResolveComponentInterface(out inputSource, out MonoBehaviour sourceBehaviour))
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

            if (TryResolveComponentInterface(out motionExecutor, out MonoBehaviour executorBehaviour))
            {
                motionExecutorBehaviour = executorBehaviour;
            }
        }

        void ResolveFacingProvider()
        {
            if (facingProviderBehaviour == null)
                facingProviderBehaviour = GetComponent<TransformFacingDirectionProvider>();
            if (facingProviderBehaviour == null && TryResolveComponentInterface(out IFacingDirectionProvider provider, out MonoBehaviour providerBehaviour))
                facingProviderBehaviour = providerBehaviour;
            facingProvider = facingProviderBehaviour as IFacingDirectionProvider;
        }

        void ResolveLocomotionPresenter()
        {
            if (locomotionPresenter != null)
            {
                if (playbackProgressController == null)
                    playbackProgressController = locomotionPresenter as ILocomotionAnimationPlaybackProgressController;
                return;
            }

            if (TryGetComponent(out BasicLocomotionAnimancerPresenter presenter))
            {
                locomotionPresenter = presenter;
                playbackProgressController = presenter;
                return;
            }

            locomotionPresenter = GetComponentInChildren<BasicLocomotionAnimancerPresenter>(true);
            playbackProgressController = locomotionPresenter as ILocomotionAnimationPlaybackProgressController;
        }

        void ResolveCameraController()
        {
            cameraController = GetComponent<ThirdPersonCameraController>();
            if (cameraController != null)
                return;

            cameraController = GetComponentInParent<ThirdPersonCameraController>(true);
            if (cameraController != null)
                return;

            cameraController = GetComponentInChildren<ThirdPersonCameraController>(true);
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

             RuntimeDiagnosticLog.Submit(new RuntimeDiagnosticLogEvent(
                 RuntimeDiagnosticLogCategory.Locomotion,
                 RuntimeDiagnosticLogLevel.Info,
                 "movement-camera-input",
                 "",
                 "",
                 0,
                 Time.frameCount,
                 $"[DEBUG-CAM-CHAIN] movement.camera frame={Time.frameCount} object={name} " +
                 $"move={moveInput.ToString("F3")} look={lookInput.ToString("F3")} camera={CameraName()} " +
                 $"cameraAutoTick={(cameraController != null ? cameraController.AutoTick.ToString() : "null")} " +
                 $"followPosition={transform.position.ToString("F3")}"));
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

        bool TryResolveComponentInterface<T>(out T service, out MonoBehaviour serviceBehaviour) where T : class
        {
            MonoBehaviour[] behaviours = GetComponents<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is T candidate)
                {
                    service = candidate;
                    serviceBehaviour = behaviours[i];
                    return true;
                }
            }

            service = null;
            serviceBehaviour = null;
            return false;
        }
    }
}
