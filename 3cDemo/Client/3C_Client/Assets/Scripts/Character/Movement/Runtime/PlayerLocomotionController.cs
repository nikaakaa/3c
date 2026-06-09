using ThirdPersonAnimation;
using ThirdPersonCamera;
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
        [SerializeField] BasicMovementConfigSO config;
        [SerializeField] LocomotionStateGraphConfigSO stateGraphConfig;
        [SerializeField] bool autoUpdate = true;
        [SerializeField] bool debugCameraLog = true;
        [SerializeField, Min(0f)] float debugCameraLogInterval = 0.1f;

        readonly BasicLocomotionPipeline pipeline = new BasicLocomotionPipeline();
        BasicLocomotionStateMachine stateMachine;
        IBasicLocomotionInputSource inputSource;
        IBasicLocomotionMotionExecutor motionExecutor;
        MovementInputIntent currentIntent;
        Vector3 currentWorldDirection;
        BasicLocomotionFrame currentFrame;
        LocomotionAnimationGait lastMovingGait = LocomotionAnimationGait.Run;
        bool previousCameraAutoTick;
        bool hasPreviousCameraAutoTick;
        float nextCameraDebugLogTime;
        bool defaultGraphWarningLogged;

        public BasicMovementPhase CurrentPhase => stateMachine != null ? stateMachine.Phase : BasicMovementPhase.Idle;
        public float CurrentPhaseTime => stateMachine != null ? stateMachine.PhaseTime : 0f;
        public string ActiveStatePath => stateMachine != null ? stateMachine.ActivePath : string.Empty;
        public Vector3 CurrentWorldDirection => currentWorldDirection;
        public MovementInputIntent CurrentIntent => currentIntent;
        public MonoBehaviour InputSourceBehaviour { get => inputSourceBehaviour; set => inputSourceBehaviour = value; }
        public MonoBehaviour MotionExecutorBehaviour { get => motionExecutorBehaviour; set => motionExecutorBehaviour = value; }
        public ThirdPersonCameraController CameraController { get => cameraController; set => cameraController = value; }
        public BasicLocomotionAnimancerPresenter LocomotionPresenter { get => locomotionPresenter; set => locomotionPresenter = value; }
        public BasicMovementConfigSO Config { get => config; set => config = value; }
        public LocomotionStateGraphConfigSO StateGraphConfig { get => stateGraphConfig; set => SetStateGraphConfig(value); }
        public bool AutoUpdate { get => autoUpdate; set => autoUpdate = value; }
        public bool UsesDefaultStateGraph => stateMachine != null && stateMachine.UsesDefaultGraph;

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
        }

        void Update()
        {
            if (!autoUpdate)
                return;

            TickFromInputSource(Time.deltaTime);
        }

        public bool TickFromInputSource(float deltaTime)
        {
            if (inputSource == null)
                ResolveInputSource();

            if (inputSource == null)
                return false;

            Tick(inputSource.ReadInput(deltaTime));
            return true;
        }

        public void Tick(in BasicLocomotionInputSnapshot input)
        {
            if (!TryEnsureStateMachine())
                return;

            BasicMovementSettings baseSettings = BasicMovementSettings.FromConfig(config);
            MovementInputIntent previewIntent = MovementInputIntent.FromRaw(input.Move, baseSettings.InputDeadZone);
            BasicMovementSettings settings = ResolveMovementSettings(in previewIntent, in baseSettings);

            if (cameraController != null)
                cameraController.ApplyLook(input.Look);

            LogCameraInput(input.Move, input.Look);

            currentFrame = pipeline.Tick(in input, in settings, cameraController, stateMachine);
            currentIntent = currentFrame.Intent;
            currentWorldDirection = currentFrame.WorldDirection;
            if (currentIntent.HasMoveIntent)
                lastMovingGait = ResolveGait(currentIntent.Strength);

            if (motionExecutor == null)
                ResolveMotionExecutor();

            if (motionExecutor != null)
            {
                MovementCommand command = currentFrame.Command;
                motionExecutor.ExecuteBasicMovement(in command);
            }

            if (locomotionPresenter != null)
            {
                float currentSpeed = motionExecutor != null ? motionExecutor.CurrentSpeed : currentFrame.Command.PlanarSpeed;
                MovementAnimationContext animationContext = BuildAnimationContext(in currentFrame, currentSpeed);
                locomotionPresenter.Present(in animationContext);
            }

            if (cameraController != null)
                cameraController.Resolve();
        }

        public void SetStateGraphConfig(LocomotionStateGraphConfigSO graphConfig)
        {
            stateGraphConfig = graphConfig;
            stateMachine = null;
            defaultGraphWarningLogged = false;
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

        static MovementAnimationContext BuildAnimationContext(in BasicLocomotionFrame frame, float planarSpeed)
        {
            return new MovementAnimationContext(
                frame.Phase,
                frame.Intent.HasMoveIntent,
                frame.Intent.Strength,
                frame.WorldDirection,
                planarSpeed);
        }

        BasicMovementSettings ResolveMovementSettings(in MovementInputIntent previewIntent, in BasicMovementSettings settings)
        {
            LocomotionAnimationSetSO set = locomotionPresenter != null ? locomotionPresenter.AnimationSet : null;
            if (set == null)
                return settings;

            LocomotionAnimationGait gait = previewIntent.HasMoveIntent ? ResolveGait(previewIntent.Strength) : lastMovingGait;
            float stopExitDuration = set.ResolveTiming(BasicMovementPhase.MoveStop, gait, gait, settings.MoveStopMinTime).ExitDuration;
            return settings.WithMoveStopExitDuration(stopExitDuration);
        }

        LocomotionAnimationGait ResolveGait(float inputStrength)
        {
            float threshold = locomotionPresenter != null ? locomotionPresenter.RunInputThreshold : 0.65f;
            return inputStrength >= threshold ? LocomotionAnimationGait.Run : LocomotionAnimationGait.Walk;
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
                stateMachine = new BasicLocomotionStateMachine(stateGraphConfig);
            }
            catch (System.Exception exception)
            {
                Debug.LogError($"[PlayerLocomotionController] Locomotion state graph is invalid. {exception.Message}", this);
                return false;
            }

            if (stateMachine.UsesDefaultGraph && !defaultGraphWarningLogged)
            {
                defaultGraphWarningLogged = true;
                Debug.LogWarning("[PlayerLocomotionController] State graph config is missing. Using generated default locomotion graph.", this);
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
