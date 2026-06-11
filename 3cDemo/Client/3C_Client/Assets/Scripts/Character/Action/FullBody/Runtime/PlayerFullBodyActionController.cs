using ThirdPersonAnimation;
using ThirdPersonCharacterStateMachine;
using ThirdPersonDiagnostics;
using ThirdPersonInput;
using ThirdPersonMovement;
using UnityEngine;

namespace ThirdPersonAction
{
    [DefaultExecutionOrder(35)]
    [DisallowMultipleComponent]
    public sealed class PlayerFullBodyActionController : MonoBehaviour
    {
        [SerializeField] InputRequestBufferComponent inputBufferComponent;
        [SerializeField] PlayerLocomotionController locomotionController;
        [SerializeField] CharacterStateMachineDefinitionSO stateMachineDefinition;
        [SerializeField] MonoBehaviour facingProviderBehaviour;
        [SerializeField] MonoBehaviour actionMovementExecutorBehaviour;
        [SerializeField] MonoBehaviour animationPresenterBehaviour;
        [SerializeField] bool autoUpdate = true;
        [SerializeField] bool restoreLocomotionAutoUpdateOnDisable = true;
        [SerializeField] string debugFullBodyStatePath;
        [SerializeField] string debugPendingTransitionPath;

        CharacterStateMachineRunner stateMachine;
        IActionMovementExecutor actionMovementExecutor;
        IFacingDirectionProvider facingProvider;
        IActionAnimationPresenter animationPresenter;
        CharacterStateMachineSnapshot currentStateSnapshot = CharacterStateMachineSnapshot.Inactive;
        string lastLoggedFullBodyPath = string.Empty;
        string lastLoggedPendingTransitionPath = string.Empty;
        string lastLoggedLocomotionPath = string.Empty;
        BasicMovementPhase lastLoggedLocomotionPhase = BasicMovementPhase.Idle;
        bool loggedInitialLocomotionState = true;
        bool hadPreviousLocomotionAutoUpdate;
        bool previousLocomotionAutoUpdate;

        public bool AutoUpdate { get => autoUpdate; set => autoUpdate = value; }
        public CharacterStateMachineDefinitionSO StateMachineDefinition { get => stateMachineDefinition; set { stateMachineDefinition = value; RebuildStateMachine(false); } }
        public PlayerLocomotionController LocomotionController { get => locomotionController; set => locomotionController = value; }
        public InputRequestBufferComponent InputBufferComponent { get => inputBufferComponent; set => inputBufferComponent = value; }
        public MonoBehaviour ActionMovementExecutorBehaviour { get => actionMovementExecutorBehaviour; set { actionMovementExecutorBehaviour = value; actionMovementExecutor = value as IActionMovementExecutor; } }
        public MonoBehaviour FacingProviderBehaviour { get => facingProviderBehaviour; set { facingProviderBehaviour = value; facingProvider = value as IFacingDirectionProvider; } }
        public MonoBehaviour AnimationPresenterBehaviour { get => animationPresenterBehaviour; set { animationPresenterBehaviour = value; ResolveAnimationPresenter(); } }
        public CharacterStateMachineRunner StateMachine => stateMachine;
        public FullBodyOwner CurrentOwner => currentStateSnapshot.Owner;
        public CharacterStateMachineSnapshot CurrentStateSnapshot => currentStateSnapshot;
        public string ActiveFullBodyStatePath => currentStateSnapshot.ActivePath;
        public string PendingFullBodyTransitionPath => currentStateSnapshot.PendingTransitionPath;
        public CharacterStateMachineFrame LastStateFrame { get; private set; }
        public BasicLocomotionFrame LastLocomotionFrame { get; private set; }

        void Reset()
        {
            ResolveReferences();
        }

        void OnEnable()
        {
            ResolveReferences();
            RebuildStateMachine(false);
            CaptureLocomotionAutoUpdate();
        }

        void OnDisable()
        {
            RestoreLocomotionAutoUpdate();
            if (stateMachine != null)
                stateMachine.Reset();
            SetInactiveStateSnapshot();
        }

        void Update()
        {
            if (autoUpdate)
                Tick(Time.deltaTime);
        }

        public bool Tick(float deltaTime)
        {
            ResolveReferences();
            if (!EnsureStateMachine())
                return false;

            if (locomotionController == null)
                return false;

            if (!locomotionController.TryReadInput(deltaTime, out BasicLocomotionInputSnapshot input))
                return false;

            return Tick(in input);
        }

        public bool Tick(in BasicLocomotionInputSnapshot input)
        {
            ResolveReferences();
            if (!EnsureStateMachine())
                return false;

            if (locomotionController == null)
                return false;

            int step = inputBufferComponent != null ? inputBufferComponent.CurrentStep : Time.frameCount;
            BasicMovementSettings settings = BasicMovementSettings.FromConfig(locomotionController.Config);
            CharacterInputRequestFact inputRequest = FullBodyActionInputRequestBuilder.BuildDodgeRequestFact(
                inputBufferComponent != null ? inputBufferComponent.Buffer : null,
                step,
                in input,
                in settings,
                locomotionController.RunLatchActive,
                locomotionController.CameraController,
                facingProvider,
                DodgeActionConfig.Default);
            if (!locomotionController.TryEvaluateWithStateMachine(
                    in input,
                    stateMachine,
                    in inputRequest,
                    step,
                    out BasicLocomotionFrame locomotionFrame,
                    out CharacterStateMachineFrame stateFrame))
            {
                return false;
            }

            LastLocomotionFrame = locomotionFrame;
            LastStateFrame = stateFrame;
            ApplyStateFrameOutputs(in stateFrame, in locomotionFrame, step);
            UpdateStateSnapshot(in stateFrame, step);

            locomotionController.CompleteLocomotionTick();
            LogDiagnosticTickSnapshots(step);
            return true;
        }

        void ApplyStateFrameOutputs(in CharacterStateMachineFrame stateFrame, in BasicLocomotionFrame locomotionFrame, int step)
        {
            if (stateFrame.ConsumeInputRequest && inputBufferComponent != null)
                inputBufferComponent.Buffer.TryConsume(stateFrame.ConsumedRequestKind, step, out _);

            if (stateFrame.HasAnimationRequest && animationPresenter != null)
                animationPresenter.Present(stateFrame.AnimationRequest);

            if (currentStateSnapshot.Owner.IsAction && !stateFrame.Owner.IsAction && animationPresenter != null)
                animationPresenter.Clear();

            if (stateFrame.HasActionMovement && actionMovementExecutor != null)
                actionMovementExecutor.ExecuteActionMovement(stateFrame.ActionMovementCommand);

            if (stateFrame.ExecuteBasicMovement)
                locomotionController.ExecuteLocomotionMotion(in locomotionFrame);

            if (stateFrame.PresentLocomotionAnimation)
                locomotionController.PresentLocomotionAnimation(in locomotionFrame);
        }

        void UpdateStateSnapshot(in CharacterStateMachineFrame stateFrame, int step)
        {
            CharacterStateMachineSnapshot previousSnapshot = currentStateSnapshot;
            currentStateSnapshot = stateFrame.Snapshot;
            debugFullBodyStatePath = currentStateSnapshot.ActivePath;
            debugPendingTransitionPath = currentStateSnapshot.PendingTransitionPath;

            LogFullBodySnapshotChange(in previousSnapshot, in currentStateSnapshot, step);
            LogLocomotionStateChange(in currentStateSnapshot, step);
            LogActionDecision(in previousSnapshot, in currentStateSnapshot, in stateFrame, step);
        }

        void SetInactiveStateSnapshot()
        {
            currentStateSnapshot = CharacterStateMachineSnapshot.Inactive;
            debugFullBodyStatePath = currentStateSnapshot.ActivePath;
            debugPendingTransitionPath = currentStateSnapshot.PendingTransitionPath;
            lastLoggedFullBodyPath = currentStateSnapshot.ActivePath;
            lastLoggedPendingTransitionPath = currentStateSnapshot.PendingTransitionPath;
            lastLoggedLocomotionPath = string.Empty;
            lastLoggedLocomotionPhase = BasicMovementPhase.Idle;
            loggedInitialLocomotionState = true;
        }

        void LogFullBodySnapshotChange(
            in CharacterStateMachineSnapshot previousSnapshot,
            in CharacterStateMachineSnapshot snapshot,
            int step)
        {
            if (snapshot.ActivePath != lastLoggedFullBodyPath)
            {
                RuntimeDiagnosticLog.Submit(new RuntimeDiagnosticLogEvent(
                    RuntimeDiagnosticLogCategory.FullBody,
                    RuntimeDiagnosticLogLevel.Info,
                    "fullbody-path-changed",
                    snapshot.ActivePath,
                    previousSnapshot.ActivePath,
                    step,
                    Time.frameCount,
                    $"owner={snapshot.Owner.Kind} action={snapshot.ActionState.Value} stateTime={snapshot.StateTime:F3} variant={snapshot.Variant}"));

                lastLoggedFullBodyPath = snapshot.ActivePath;
            }

            if (snapshot.PendingTransitionPath == lastLoggedPendingTransitionPath)
                return;

            RuntimeDiagnosticLog.Submit(new RuntimeDiagnosticLogEvent(
                RuntimeDiagnosticLogCategory.FullBody,
                RuntimeDiagnosticLogLevel.Trace,
                "fullbody-pending-transition-changed",
                snapshot.PendingTransitionPath,
                previousSnapshot.PendingTransitionPath,
                step,
                Time.frameCount,
                $"owner={snapshot.Owner.Kind} action={snapshot.ActionState.Value}"));

            lastLoggedPendingTransitionPath = snapshot.PendingTransitionPath;
        }

        void LogLocomotionStateChange(in CharacterStateMachineSnapshot snapshot, int step)
        {
            string locomotionPath = snapshot.IsLocomotion ? snapshot.ActivePath : lastLoggedLocomotionPath;
            if (loggedInitialLocomotionState &&
                snapshot.LocomotionPhase == lastLoggedLocomotionPhase &&
                locomotionPath == lastLoggedLocomotionPath)
                return;

            RuntimeDiagnosticLog.Submit(new RuntimeDiagnosticLogEvent(
                RuntimeDiagnosticLogCategory.Locomotion,
                RuntimeDiagnosticLogLevel.Info,
                "locomotion-phase-changed",
                locomotionPath,
                lastLoggedLocomotionPath,
                step,
                Time.frameCount,
                $"fromPhase={lastLoggedLocomotionPhase} toPhase={snapshot.LocomotionPhase} gait={LastLocomotionFrame.Command.Gait} phaseTime={snapshot.StateTime:F3}"));

            lastLoggedLocomotionPhase = snapshot.LocomotionPhase;
            lastLoggedLocomotionPath = locomotionPath;
            loggedInitialLocomotionState = true;
        }

        void LogActionDecision(
            in CharacterStateMachineSnapshot previousSnapshot,
            in CharacterStateMachineSnapshot snapshot,
            in CharacterStateMachineFrame frame,
            int step)
        {
            if (!frame.ConsumeInputRequest)
                return;

            RuntimeDiagnosticLog.Submit(new RuntimeDiagnosticLogEvent(
                RuntimeDiagnosticLogCategory.Action,
                RuntimeDiagnosticLogLevel.Info,
                "action-accepted",
                snapshot.ActivePath,
                previousSnapshot.ActivePath,
                step,
                Time.frameCount,
                $"owner={snapshot.Owner.Kind} action={snapshot.ActionState.Value} variant={snapshot.Variant} animation={(frame.HasAnimationRequest ? frame.AnimationRequest.Key.Value : string.Empty)}"));
        }

        void LogDiagnosticTickSnapshots(int step)
        {
            LogFullBodyTickSnapshot(step);
            if (locomotionController != null)
                locomotionController.LogDiagnosticTickSnapshot(step);
            LogAnimationTickSnapshot(step);
        }

        void LogFullBodyTickSnapshot(int step)
        {
            RuntimeDiagnosticLog.Submit(new RuntimeDiagnosticLogEvent(
                RuntimeDiagnosticLogCategory.FullBody,
                RuntimeDiagnosticLogLevel.Trace,
                "fullbody-tick-snapshot",
                currentStateSnapshot.ActivePath,
                string.Empty,
                step,
                Time.frameCount,
                BuildFullBodyTickContext()));
        }

        void LogAnimationTickSnapshot(int step)
        {
            RuntimeDiagnosticLog.Submit(new RuntimeDiagnosticLogEvent(
                RuntimeDiagnosticLogCategory.Animation,
                RuntimeDiagnosticLogLevel.Trace,
                "animation-tick-snapshot",
                currentStateSnapshot.ActivePath,
                string.Empty,
                step,
                Time.frameCount,
                BuildAnimationTickContext()));
        }

        string BuildFullBodyTickContext()
        {
            return
                $"owner={currentStateSnapshot.Owner.Kind} ownerAction={currentStateSnapshot.ActionState.Value} " +
                $"stateTime={currentStateSnapshot.StateTime:F3} pending={currentStateSnapshot.PendingTransitionPath} variant={currentStateSnapshot.Variant} " +
                $"locomotionPhase={currentStateSnapshot.LocomotionPhase} locomotionPath={currentStateSnapshot.ActivePath} locomotionGait={LastLocomotionFrame.Command.Gait} " +
                $"hasMove={LastLocomotionFrame.Intent.HasMoveIntent} moveStrength={LastLocomotionFrame.Intent.Strength:F3} worldDirection={LastLocomotionFrame.WorldDirection.ToString("F3")} " +
                $"actionFrameActive={LastStateFrame.Owner.IsAction} actionFrameCompleted={LastStateFrame.ActionCompleted} actionMove={LastStateFrame.ActionMovementCommand.PlanarDistance:F3} actionRotate={LastStateFrame.ActionMovementCommand.RotateToDirection}";
        }

        string BuildAnimationTickContext()
        {
            AnimationPhasePlaybackProgress locomotionProgress = locomotionController != null
                ? locomotionController.CurrentAnimationPlaybackProgress
                : AnimationPhasePlaybackProgress.Invalid(currentStateSnapshot.LocomotionPhase);
            string locomotionAnimationName = locomotionController != null ? locomotionController.CurrentAnimationName : string.Empty;

            return
                $"owner={currentStateSnapshot.Owner.Kind} fullBodyPath={currentStateSnapshot.ActivePath} " +
                $"locomotionPhase={currentStateSnapshot.LocomotionPhase} locomotionGait={LastLocomotionFrame.Command.Gait} " +
                $"locomotionAlias={locomotionProgress.AliasKey} locomotionAnimation={locomotionAnimationName} locomotionNormalized={locomotionProgress.NormalizedTime:F3} locomotionValid={locomotionProgress.HasValidPlayback} locomotionEnded={locomotionProgress.IsEnded} " +
                $"actionKey={(animationPresenter != null ? animationPresenter.CurrentKey.Value : string.Empty)} actionAnimation={(animationPresenter != null ? animationPresenter.CurrentAnimationName : string.Empty)} actionNormalized={(animationPresenter != null ? animationPresenter.CurrentNormalizedTime : 0f):F3} actionValid={(animationPresenter != null && animationPresenter.HasValidPlayback)}";
        }

        bool EnsureStateMachine()
        {
            if (stateMachine != null)
                return true;

            return RebuildStateMachine(true);
        }

        bool RebuildStateMachine(bool logErrors)
        {
            stateMachine = null;

            try
            {
                CharacterStateMachineDefinition definition = stateMachineDefinition != null
                    ? stateMachineDefinition.ToDefinition()
                    : CharacterStateMachineDefinition.CreateDefault();
                stateMachine = new CharacterStateMachineRunner(definition);
            }
            catch (System.Exception exception)
            {
                SetInactiveStateSnapshot();
                if (logErrors)
                    Debug.LogError("Character state machine definition is invalid:\n" + exception.Message, this);
                return false;
            }

            currentStateSnapshot = stateMachine.Snapshot;
            debugFullBodyStatePath = currentStateSnapshot.ActivePath;
            debugPendingTransitionPath = currentStateSnapshot.PendingTransitionPath;
            lastLoggedFullBodyPath = currentStateSnapshot.ActivePath;
            lastLoggedPendingTransitionPath = currentStateSnapshot.PendingTransitionPath;
            lastLoggedLocomotionPhase = currentStateSnapshot.LocomotionPhase;
            lastLoggedLocomotionPath = currentStateSnapshot.ActivePath;
            loggedInitialLocomotionState = true;
            return true;
        }

        void CaptureLocomotionAutoUpdate()
        {
            if (locomotionController == null || hadPreviousLocomotionAutoUpdate)
                return;

            previousLocomotionAutoUpdate = locomotionController.AutoUpdate;
            hadPreviousLocomotionAutoUpdate = true;
            locomotionController.AutoUpdate = false;
        }

        void RestoreLocomotionAutoUpdate()
        {
            if (!restoreLocomotionAutoUpdateOnDisable || !hadPreviousLocomotionAutoUpdate || locomotionController == null)
                return;

            locomotionController.AutoUpdate = previousLocomotionAutoUpdate;
            hadPreviousLocomotionAutoUpdate = false;
        }

        void ResolveReferences()
        {
            if (inputBufferComponent == null)
                inputBufferComponent = GetComponent<InputRequestBufferComponent>();
            if (inputBufferComponent == null)
                inputBufferComponent = GetComponentInParent<InputRequestBufferComponent>();

            if (locomotionController == null)
                locomotionController = GetComponent<PlayerLocomotionController>();
            if (locomotionController == null)
                locomotionController = GetComponentInParent<PlayerLocomotionController>();

            if (actionMovementExecutorBehaviour == null && TryResolveLocomotionActionExecutor(out IActionMovementExecutor locomotionExecutor, out MonoBehaviour locomotionExecutorBehaviour))
            {
                actionMovementExecutor = locomotionExecutor;
                actionMovementExecutorBehaviour = locomotionExecutorBehaviour;
            }
            else if (actionMovementExecutorBehaviour == null && TryResolveComponentInterface(out IActionMovementExecutor resolvedExecutor, out MonoBehaviour executorBehaviour))
            {
                actionMovementExecutor = resolvedExecutor;
                actionMovementExecutorBehaviour = executorBehaviour;
            }
            else
            {
                actionMovementExecutor = actionMovementExecutorBehaviour as IActionMovementExecutor;
            }

            if (facingProviderBehaviour == null)
                facingProviderBehaviour = GetComponent<TransformFacingDirectionProvider>();
            facingProvider = facingProviderBehaviour as IFacingDirectionProvider;

            if (animationPresenterBehaviour == null && TryResolveComponentInterface(out IActionAnimationPresenter resolvedPresenter, out MonoBehaviour presenterBehaviour))
                animationPresenterBehaviour = presenterBehaviour;

            ResolveAnimationPresenter();
        }

        void ResolveAnimationPresenter()
        {
            animationPresenter = animationPresenterBehaviour as IActionAnimationPresenter;
        }

        bool TryResolveLocomotionActionExecutor(out IActionMovementExecutor executor, out MonoBehaviour executorBehaviour)
        {
            executor = null;
            executorBehaviour = null;

            if (locomotionController == null || locomotionController.MotionExecutorBehaviour == null)
                return false;

            executor = locomotionController.MotionExecutorBehaviour as IActionMovementExecutor;
            if (executor == null)
                return false;

            executorBehaviour = locomotionController.MotionExecutorBehaviour;
            return true;
        }

        bool TryResolveComponentInterface<T>(out T service, out MonoBehaviour serviceBehaviour) where T : class
        {
            MonoBehaviour[] behaviours = GetComponentsInChildren<MonoBehaviour>(true);
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
