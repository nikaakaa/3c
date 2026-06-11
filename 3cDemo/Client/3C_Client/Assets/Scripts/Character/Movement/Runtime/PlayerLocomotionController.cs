using ThirdPersonAnimation;
using ThirdPersonCamera;
using ThirdPersonCharacterStateMachine;
using ThirdPersonDiagnostics;
using ThirdPersonInput;
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
        [SerializeField] ThirdPersonCameraController cameraController;
        [SerializeField] BasicLocomotionAnimancerPresenter locomotionPresenter;
        [SerializeField] RunLocomotionAnimationConfigSO runAnimationConfig;
        [SerializeField] BasicMovementConfigSO config;
        [SerializeField] CharacterStateMachineDefinitionSO stateMachineDefinition;
        [SerializeField] bool autoUpdate = true;
        [SerializeField] bool debugCameraLog = true;
        [SerializeField, Min(0f)] float debugCameraLogInterval = 0.1f;

        readonly BasicLocomotionPipeline pipeline = new BasicLocomotionPipeline();
        CharacterStateMachineRunner stateMachine;
        IBasicLocomotionInputSource inputSource;
        IBasicLocomotionMotionExecutor motionExecutor;
        IAnimationPhasePlaybackProgressSource playbackProgressSource;
        AnimationPhasePlaybackProgress previousMotionPlaybackProgress;
        MovementInputIntent currentIntent;
        BasicMovementGait lastMovingGait = BasicMovementGait.Walk;
        Vector3 currentWorldDirection;
        BasicLocomotionFrame currentFrame;
        bool hasPreviousMotionPlaybackProgress;
        bool hasActiveMoveStopGait;
        BasicMovementGait activeMoveStopGait = BasicMovementGait.Walk;
        bool previousCameraAutoTick;
        bool hasPreviousCameraAutoTick;
        float nextCameraDebugLogTime;
        bool defaultGraphWarningLogged;
        bool suppressBasicMotionExecution;
        bool suppressLocomotionAnimationPresentation;
        bool runLatchActive;

        public BasicMovementPhase CurrentPhase => stateMachine != null ? stateMachine.Snapshot.LocomotionPhase : BasicMovementPhase.Idle;
        public float CurrentPhaseTime => stateMachine != null ? stateMachine.StateTime : 0f;
        public string ActiveStatePath => stateMachine != null ? stateMachine.Snapshot.ActivePath : string.Empty;
        public BasicMovementGait CurrentGait => currentIntent.HasMoveIntent ? currentIntent.Gait : lastMovingGait;
        public Vector3 CurrentWorldDirection => currentWorldDirection;
        public MovementInputIntent CurrentIntent => currentIntent;
        public BasicLocomotionFrame CurrentFrame => currentFrame;
        public AnimationPhasePlaybackProgress CurrentAnimationPlaybackProgress => ResolvePlaybackProgress(CurrentPhase);
        public string CurrentAnimationName => locomotionPresenter != null ? locomotionPresenter.CurrentAnimationName : string.Empty;
        public bool RunLatchActive => runLatchActive;
        public MonoBehaviour InputSourceBehaviour { get => inputSourceBehaviour; set => inputSourceBehaviour = value; }
        public MonoBehaviour MotionExecutorBehaviour { get => motionExecutorBehaviour; set => motionExecutorBehaviour = value; }
        public ThirdPersonCameraController CameraController { get => cameraController; set => cameraController = value; }
        public BasicLocomotionAnimancerPresenter LocomotionPresenter { get => locomotionPresenter; set => locomotionPresenter = value; }
        public RunLocomotionAnimationConfigSO RunAnimationConfig { get => runAnimationConfig; set => runAnimationConfig = value; }
        public BasicMovementConfigSO Config { get => config; set => config = value; }
        public CharacterStateMachineDefinitionSO StateMachineDefinition { get => stateMachineDefinition; set => SetStateMachineDefinition(value); }
        public bool AutoUpdate { get => autoUpdate; set => autoUpdate = value; }
        public bool SuppressBasicMotionExecution { get => suppressBasicMotionExecution; set => suppressBasicMotionExecution = value; }
        public bool SuppressLocomotionAnimationPresentation { get => suppressLocomotionAnimationPresentation; set => suppressLocomotionAnimationPresentation = value; }
        public bool UsesDefaultStateMachine => stateMachineDefinition == null;

        void Reset()
        {
            ResolveInputSource();
            ResolveMotionExecutor();
            ResolveLocomotionPresenter();
        }

        void OnEnable()
        {
            if (!TryEnsureStateMachine())
            {
                enabled = false;
                return;
            }

            ResolveInputSource();
            ResolveMotionExecutor();
            ResolveLocomotionPresenter();

            if (HasEnabledLegacyPlayer())
            {
                Debug.LogError("[PlayerLocomotionController] Legacy Player path is enabled. Player locomotion is disabled to avoid double movement input.");
                enabled = false;
                return;
            }

            if (inputSource == null)
            {
                Debug.LogError("[PlayerLocomotionController] Locomotion input source is missing. Player locomotion cannot read movement input.");
                enabled = false;
                return;
            }

            if (motionExecutor == null)
            {
                Debug.LogError("[PlayerLocomotionController] Locomotion motion executor is missing. Player locomotion cannot enter the main movement path.");
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
            if (!TryReadInput(deltaTime, out BasicLocomotionInputSnapshot input))
                return false;

            Tick(in input, diagnosticStep);
            return true;
        }

        public void Tick(in BasicLocomotionInputSnapshot input)
        {
            Tick(in input, 0);
        }

        public void Tick(in BasicLocomotionInputSnapshot input, int diagnosticStep)
        {
            if (!TryEvaluateLocomotion(in input, out BasicLocomotionFrame frame))
                return;

            ExecuteLocomotionMotion(in frame);
            PresentLocomotionAnimation(in frame);
            CompleteLocomotionTick();
            LogDiagnosticTickSnapshot(diagnosticStep);
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
            if (!TryEnsureStateMachine())
            {
                frame = default;
                return false;
            }

            CharacterInputRequestFact request = CharacterInputRequestFact.None(InputRequestKind.Dodge);
            return TryEvaluateWithStateMachine(in input, stateMachine, in request, 0, out frame, out _);
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

            BasicMovementSettings baseSettings = BasicMovementSettings.FromConfig(config);
            bool wantsRun = input.RunHeld || runLatchActive;
            MovementInputIntent pendingIntent = MovementInputIntent.FromRaw(input.Move, baseSettings.InputDeadZone, wantsRun);
            CharacterStateMachineSnapshot snapshot = runner.Snapshot;
            BasicMovementPhase currentPhase = snapshot.LocomotionPhase;
            BasicMovementGait frameGait = ResolveFrameGait(currentPhase, in pendingIntent);
            BasicMovementSettings settings = ResolveMovementSettings(frameGait, in baseSettings);
            BasicMovementPhaseFacts phaseFacts = ResolvePhaseFacts(currentPhase, runner.StateTime, frameGait, input.DeltaTime, in settings);
            BasicLocomotionInputSnapshot resolvedInput = new BasicLocomotionInputSnapshot(
                input.DeltaTime,
                input.Move,
                input.Look,
                wantsRun);

            if (cameraController != null)
                cameraController.ApplyLook(input.Look);

            LogCameraInput(input.Move, input.Look);

            Vector3 worldDirection = CameraRelativeMovementResolver.Resolve(pendingIntent, cameraController);
            CharacterStateMachineContext context = new CharacterStateMachineContext(
                input.DeltaTime,
                currentStep,
                pendingIntent,
                worldDirection,
                phaseFacts,
                inputRequest);
            bool runLatchBeforeStateTick = runLatchActive;
            stateFrame = runner.Tick(in context);
            ApplyStateMachineOutputs(in stateFrame);

            BasicMovementMotionFacts motionFacts = ResolveMotionFacts(stateFrame.LocomotionPhase, frameGait);
            currentFrame = pipeline.Tick(in resolvedInput, in settings, cameraController, stateFrame.LocomotionPhase, phaseFacts, motionFacts, frameGait);
            currentIntent = currentFrame.Intent;
            UpdatePhaseGaitMemory(stateFrame.LocomotionPhase, frameGait);
            LogStateMachineOutputProbe(
                currentStep,
                currentPhase,
                frameGait,
                in pendingIntent,
                in phaseFacts,
                runLatchBeforeStateTick,
                in stateFrame);
            if (currentIntent.HasMoveIntent)
                lastMovingGait = currentIntent.Gait;

            currentWorldDirection = currentFrame.WorldDirection;
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
            if (cameraController != null)
                cameraController.Resolve();

            ResetRunLatchAfterIdle();
        }

        public void SetStateMachineDefinition(CharacterStateMachineDefinitionSO definition)
        {
            stateMachineDefinition = definition;
            stateMachine = null;
            defaultGraphWarningLogged = false;
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

        void UpdatePhaseGaitMemory(BasicMovementPhase phase, BasicMovementGait frameGait)
        {
            if (phase == BasicMovementPhase.MoveStop)
            {
                activeMoveStopGait = frameGait;
                hasActiveMoveStopGait = true;
                return;
            }

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
                planarSpeed);
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

        BasicMovementMotionFacts ResolveMotionFacts(BasicMovementPhase phase, BasicMovementGait gait)
        {
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
            AnimationMotionProfileSample sample = AnimationMotionProfileSampler.Sample(profile, in playbackWindow);
            if (!sample.HasMotionContribution)
                return BasicMovementMotionFacts.None(phase);

            return new BasicMovementMotionFacts(
                true,
                sample.LocalPlanarDelta,
                sample.YawDelta,
                sample.SourcePhase,
                sample.SourceAliasKey);
        }

        AnimationPhasePlaybackProgress ResolvePlaybackProgress(BasicMovementPhase phase)
        {
            IAnimationPhasePlaybackProgressSource source = playbackProgressSource ?? locomotionPresenter;
            return source != null ? source.CurrentPlaybackProgress : AnimationPhasePlaybackProgress.Invalid(phase);
        }

        AnimationMotionPlaybackWindow BuildMotionPlaybackWindow(BasicMovementPhase phase, BasicMovementGait gait, in AnimationPhasePlaybackProgress progress)
        {
            if (!progress.HasValidPlayback || progress.Phase != phase)
            {
                ResetMotionPlaybackWindow();
                return AnimationMotionPlaybackWindow.Invalid(phase, gait);
            }

            bool samePlayback =
                hasPreviousMotionPlaybackProgress &&
                previousMotionPlaybackProgress.HasValidPlayback &&
                previousMotionPlaybackProgress.Phase == progress.Phase &&
                previousMotionPlaybackProgress.AliasKey == progress.AliasKey &&
                progress.NormalizedTime >= previousMotionPlaybackProgress.NormalizedTime;

            float previousTime = samePlayback ? previousMotionPlaybackProgress.NormalizedTime : progress.NormalizedTime;
            previousMotionPlaybackProgress = progress;
            hasPreviousMotionPlaybackProgress = true;
            return new AnimationMotionPlaybackWindow(phase, gait, progress.AliasKey, previousTime, progress.NormalizedTime, true);
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
            if (runAnimationConfig != null)
                return runAnimationConfig;

            return locomotionPresenter != null ? locomotionPresenter.RunAnimationConfig : null;
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

        bool HasEnabledLegacyPlayer()
        {
            Component legacyPlayer = GetComponent("Player");
            return legacyPlayer is Behaviour behaviour && behaviour.enabled;
        }

        bool TryEnsureStateMachine()
        {
            if (stateMachine != null)
                return true;

            try
            {
                CharacterStateMachineDefinition definition = stateMachineDefinition != null
                    ? stateMachineDefinition.ToDefinition()
                    : CharacterStateMachineDefinition.CreateDefault();
                stateMachine = new CharacterStateMachineRunner(definition);
            }
            catch (System.Exception exception)
            {
                Debug.LogError($"[PlayerLocomotionController] Character state machine is invalid. {exception.Message}", this);
                return false;
            }

            if (stateMachineDefinition == null && !defaultGraphWarningLogged)
            {
                defaultGraphWarningLogged = true;
                Debug.LogWarning("[PlayerLocomotionController] Character state machine config is missing. Using generated default unified state machine.", this);
            }

            return true;
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

        void ResolveLocomotionPresenter()
        {
            if (locomotionPresenter != null)
                return;

            if (TryGetComponent(out BasicLocomotionAnimancerPresenter presenter))
            {
                locomotionPresenter = presenter;
                return;
            }

            locomotionPresenter = GetComponentInChildren<BasicLocomotionAnimancerPresenter>(true);
        }

        void LogCameraInput(Vector2 moveInput, Vector2 lookInput)
        {
            if (!ShouldLogCamera(lookInput.sqrMagnitude > 0.000001f))
                return;

            Debug.Log(
                $"[DEBUG-CAM-CHAIN] movement.camera frame={Time.frameCount} object={name} " +
                $"move={moveInput.ToString("F3")} look={lookInput.ToString("F3")} camera={CameraName()} " +
                $"cameraAutoTick={(cameraController != null ? cameraController.AutoTick.ToString() : "null")} " +
                $"followPosition={transform.position.ToString("F3")}");
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
