using System;
using System.Collections.Generic;
using ThirdPersonAnimation;
using ThirdPersonCharacterConfig;
using ThirdPersonCharacterStateMachine;
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
        [SerializeField] CharacterConfigSO characterConfig;
        [Obsolete("Legacy serialized field; runtime reads CharacterConfigSO only.")]
        [SerializeField] CharacterStateMachineDefinitionSO stateMachineDefinition;
        [SerializeField] ActionInterruptPolicySetSO interruptPolicySet;
        [SerializeField] DodgeActionConfigSO dodgeActionConfig;
        [SerializeField] MonoBehaviour facingProviderBehaviour;
        [SerializeField] MonoBehaviour actionMovementExecutorBehaviour;
        [SerializeField] MonoBehaviour animationPresenterBehaviour;
        [SerializeField] bool autoUpdate = true;
        [SerializeField] bool restoreLocomotionAutoUpdateOnDisable = true;
        [SerializeField] string debugFullBodyStatePath;
        [SerializeField] string debugPendingTransitionPath;

        CharacterStateMachineRunner stateMachine;
        IActionMovementExecutor actionMovementExecutor;
        IActionAnimationPresenter animationPresenter;
        IActionAnimationPlaybackProgressController actionPlaybackProgressController;
        IReadOnlyList<ActionInterruptPolicy> runtimeInterruptPolicies = Array.Empty<ActionInterruptPolicy>();
        ActionInterruptPolicySetSO cachedInterruptPolicySet;
        bool interruptPoliciesCompiled;
        CharacterStateMachineSnapshot currentStateSnapshot = CharacterStateMachineSnapshot.Inactive;
        string lastLoggedFullBodyPath = string.Empty;
        string lastLoggedPendingTransitionPath = string.Empty;
        string lastLoggedLocomotionPath = string.Empty;
        BasicMovementPhase lastLoggedLocomotionPhase = BasicMovementPhase.Idle;
        bool loggedInitialLocomotionState = true;
        bool hadPreviousLocomotionAutoUpdate;
        bool previousLocomotionAutoUpdate;
        readonly FullBodyFramePipeline framePipeline = new FullBodyFramePipeline();

        public bool AutoUpdate { get => autoUpdate; set => autoUpdate = value; }
        public CharacterConfigSO CharacterConfig { get => characterConfig; set { characterConfig = value; RebuildStateMachine(false); } }
        public CharacterStateMachineDefinitionSO StateMachineDefinition { get => ResolveStateMachineDefinition(); set { RebuildStateMachine(false); } }
        public PlayerLocomotionController LocomotionController { get => locomotionController; set => locomotionController = value; }
        public InputRequestBufferComponent InputBufferComponent { get => inputBufferComponent; set => inputBufferComponent = value; }
        public ActionInterruptPolicySetSO InterruptPolicySet { get => interruptPolicySet; set { interruptPolicySet = value; ClearInterruptPolicyCache(); } }
        public DodgeActionConfigSO DodgeActionConfigAsset { get => dodgeActionConfig; set => dodgeActionConfig = value; }
        public MonoBehaviour ActionMovementExecutorBehaviour { get => actionMovementExecutorBehaviour; set { actionMovementExecutorBehaviour = value; actionMovementExecutor = value as IActionMovementExecutor; } }
        public MonoBehaviour FacingProviderBehaviour { get => facingProviderBehaviour; set => facingProviderBehaviour = value; }
        public MonoBehaviour AnimationPresenterBehaviour { get => animationPresenterBehaviour; set { animationPresenterBehaviour = value; ResolveAnimationPresenter(); } }
        public CharacterStateMachineRunner StateMachine => stateMachine;
        public FullBodyOwner CurrentOwner => currentStateSnapshot.Owner;
        public CharacterStateMachineSnapshot CurrentStateSnapshot => currentStateSnapshot;
        public string ActiveFullBodyStatePath => currentStateSnapshot.ActivePath;
        public string PendingFullBodyTransitionPath => currentStateSnapshot.PendingTransitionPath;
        public CharacterStateMachineFrame LastStateFrame { get; private set; }
        public BasicLocomotionFrame LastLocomotionFrame { get; private set; }
        public FullBodyFrameResult LastFramePipelineResult { get; private set; }

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
            int step = inputBufferComponent != null ? inputBufferComponent.CurrentStep : Time.frameCount;
            if (!TryReadFrameInputFromSource(deltaTime, step, out FullBodyFrameInput input))
                return false;

            return Tick(in input);
        }

        public bool Tick(in BasicLocomotionInputSnapshot input)
        {
            int step = inputBufferComponent != null ? inputBufferComponent.CurrentStep : Time.frameCount;
            FullBodyFrameInput frameInput = FullBodyFrameInput.FromLocomotionInput(step, in input);
            return Tick(in frameInput);
        }

        public bool Tick(in FullBodyFrameInput input)
        {
            ResolveReferences();
            bool success = framePipeline.Tick(this, in input, out FullBodyFrameResult result);
            LastFramePipelineResult = result;
            return success;
        }

        public ActionInterruptPolicyValidationResult ValidateActionInterruptPolicies()
        {
            ActionInterruptPolicyValidationResult result = new ActionInterruptPolicyValidationResult();
            bool hasDodgeConfig = TryResolveDodgeActionConfig(out DodgeActionConfig config);
            if (!hasDodgeConfig)
                result.AddError("Dodge action config is missing.");

            if (interruptPolicySet == null)
            {
                result.AddError("FullBody Action interrupt policy set is missing.");
                return result;
            }

            IReadOnlyList<ActionInterruptPolicy> policies = ResolveInterruptPolicies();
            CharacterStateMachineDefinitionSO definitionAsset = ResolveStateMachineDefinition();
            ActionInterruptPolicyValidationResult validation = definitionAsset != null
                ? ActionInterruptPolicyValidator.Validate(policies, definitionAsset.TimelinePolicies)
                : ActionInterruptPolicyValidator.Validate(policies);
            for (int i = 0; i < validation.Errors.Count; i++)
                result.AddError(validation.Errors[i]);
            for (int i = 0; i < validation.Warnings.Count; i++)
                result.AddWarning(validation.Warnings[i]);

            if (hasDodgeConfig && !FullBodyActionInterruptGate.HasDodgePolicy(policies, in config))
                result.AddError("FullBody Action interrupt policy set is missing Action.None -> Action.Dodge or Action.Dodge -> Action.Dodge policy. Both are required for dodge initiation and chain dodge.");

            return result;
        }

        public bool TryResolveDodgeActionConfig(out DodgeActionConfig config)
        {
            if (dodgeActionConfig != null)
            {
                config = dodgeActionConfig.ToConfig();
                return true;
            }

            config = default;
            return false;
        }

        [Obsolete("Use TryResolveDodgeActionConfig.")]
        public DodgeActionConfig ResolveDodgeActionConfig()
        {
            if (TryResolveDodgeActionConfig(out DodgeActionConfig config))
                return config;

            throw new InvalidOperationException("Dodge action config is missing. Assign a DodgeActionConfigSO asset.");
        }

        public int ResolveCurrentActionResistance()
        {
            if (!TryResolveDodgeActionConfig(out DodgeActionConfig config))
                return 0;

            return ResolveCurrentActionResistance(in currentStateSnapshot, in config);
        }

        public static int ResolveCurrentActionResistance(in CharacterStateMachineSnapshot snapshot, in DodgeActionConfig config)
        {
            if (!snapshot.Owner.IsAction)
                return 0;

            return snapshot.ActionState == ActionStateIds.Dodge ? config.Resistance : 0;
        }

        public FullBodyActionRestoreState CaptureRestoreState()
        {
            ResolveReferences();
            if (!EnsureStateMachine())
                return FullBodyActionRestoreState.Inactive;

            FullBodyActionGameplayRestoreState gameplay = new FullBodyActionGameplayRestoreState(
                stateMachine.CaptureRestoreState());
            FullBodyActionDiagnosticRestoreState diagnostic = new FullBodyActionDiagnosticRestoreState(
                debugFullBodyStatePath,
                debugPendingTransitionPath,
                lastLoggedFullBodyPath,
                lastLoggedPendingTransitionPath,
                lastLoggedLocomotionPath,
                lastLoggedLocomotionPhase,
                loggedInitialLocomotionState);
            return new FullBodyActionRestoreState(gameplay, diagnostic);
        }

        public bool Restore(in FullBodyActionRestoreState restoreState)
        {
            ResolveReferences();
            if (!EnsureStateMachine())
                return false;

            if (!stateMachine.Restore(restoreState.StateMachine))
                return false;

            currentStateSnapshot = stateMachine.Snapshot;
            FullBodyActionDiagnosticRestoreState diagnostic = restoreState.Diagnostic;
            debugFullBodyStatePath = string.IsNullOrEmpty(diagnostic.DebugFullBodyStatePath)
                ? currentStateSnapshot.ActivePath
                : diagnostic.DebugFullBodyStatePath;
            debugPendingTransitionPath = string.IsNullOrEmpty(diagnostic.DebugPendingTransitionPath)
                ? currentStateSnapshot.PendingTransitionPath
                : diagnostic.DebugPendingTransitionPath;
            lastLoggedFullBodyPath = diagnostic.LastLoggedFullBodyPath;
            lastLoggedPendingTransitionPath = diagnostic.LastLoggedPendingTransitionPath;
            lastLoggedLocomotionPath = diagnostic.LastLoggedLocomotionPath;
            lastLoggedLocomotionPhase = diagnostic.LastLoggedLocomotionPhase;
            loggedInitialLocomotionState = diagnostic.LoggedInitialLocomotionState;
            return true;
        }

        public void RestoreActionAnimationPlayback(in ActionAnimationPlaybackProgress progress, string animationName)
        {
            ResolveAnimationPresenter();
            if (actionPlaybackProgressController != null)
                actionPlaybackProgressController.RestorePlaybackProgress(in progress, animationName);
            else if (!progress.HasValidPlayback && animationPresenter != null)
                animationPresenter.Clear();
        }

        internal bool TryReadFrameInputFromSource(float deltaTime, int step, out FullBodyFrameInput input)
        {
            ResolveReferences();
            if (!EnsureStateMachine() || locomotionController == null)
            {
                input = default;
                return false;
            }

            locomotionController.ReleaseRollbackCameraBasisOverride();
            if (!locomotionController.TryReadInput(deltaTime, out BasicLocomotionInputSnapshot locomotionInput))
            {
                input = default;
                return false;
            }

            input = FullBodyFrameInput.FromLocomotionInput(step, in locomotionInput);
            return true;
        }

        internal bool PrepareFramePipelineAdapters()
        {
            ResolveReferences();
            return EnsureStateMachine() && locomotionController != null;
        }

        internal IReadOnlyList<ActionInterruptPolicy> ResolveInterruptPoliciesForPipeline()
        {
            return ResolveInterruptPolicies();
        }

        internal void SetLastFrameOutputsForPipeline(
            in BasicLocomotionFrame locomotionFrame,
            in CharacterStateMachineFrame stateFrame)
        {
            LastLocomotionFrame = locomotionFrame;
            LastStateFrame = stateFrame;
        }

        internal bool ConsumeStateFrameInputRequestForPipeline(in CharacterStateMachineFrame stateFrame, int step)
        {
            if (!stateFrame.ConsumeInputRequest || inputBufferComponent == null)
                return false;

            return inputBufferComponent.Buffer.TryConsume(stateFrame.ConsumedRequestKind, step, out _);
        }

        internal void ExecuteStateFrameMotionForPipeline(
            in CharacterStateMachineFrame stateFrame,
            in BasicLocomotionFrame locomotionFrame,
            out bool actionMovementExecuted,
            out bool basicMovementExecuted)
        {
            actionMovementExecuted = false;
            basicMovementExecuted = false;

            if (stateFrame.HasActionMovement && actionMovementExecutor != null)
            {
                actionMovementExecutor.ExecuteActionMovement(stateFrame.ActionMovementCommand);
                actionMovementExecuted = true;
            }

            if (stateFrame.ExecuteBasicMovement && locomotionController != null)
            {
                locomotionController.ExecuteLocomotionMotion(in locomotionFrame);
                basicMovementExecuted = true;
            }
        }

        internal void PresentStateFrameAnimationForPipeline(
            in CharacterStateMachineFrame stateFrame,
            in BasicLocomotionFrame locomotionFrame,
            bool exitedToLocomotion,
            out bool actionAnimationPresented,
            out bool locomotionAnimationPresented)
        {
            actionAnimationPresented = false;
            locomotionAnimationPresented = false;

            if (stateFrame.Owner.IsAction && stateFrame.HasAnimationRequest && animationPresenter != null)
            {
                animationPresenter.Present(stateFrame.AnimationRequest);
                actionAnimationPresented = true;
            }

            if (exitedToLocomotion && animationPresenter != null)
                animationPresenter.Clear();

            if (stateFrame.PresentLocomotionAnimation && locomotionController != null)
            {
                locomotionController.PresentLocomotionAnimation(in locomotionFrame);
                locomotionAnimationPresented = true;
            }
        }

        internal void WriteStateFrameActionFactsForPipeline(
            in CharacterStateMachineFrame stateFrame,
            bool exitedToLocomotion,
            int step)
        {
            if (locomotionController == null)
                return;

            locomotionController.WriteActionFacts(CharacterRuntimeActionFacts.FromStateFrame(
                in stateFrame,
                exitedToLocomotion,
                step));
        }

        internal void UpdateStateSnapshotForPipeline(in CharacterStateMachineFrame stateFrame, int step)
        {
            UpdateStateSnapshot(in stateFrame, step);
        }

        internal void WriteAnimationRuntimeFactsForPipeline(int step)
        {
            WriteAnimationRuntimeFacts(step);
        }

        internal void CompleteLocomotionTickForPipeline()
        {
            if (locomotionController != null)
                locomotionController.CompleteLocomotionTick();
        }

        internal void LogDiagnosticTickSnapshotsForPipeline(int step)
        {
            LogDiagnosticTickSnapshots(step);
        }

        void WriteAnimationRuntimeFacts(int step)
        {
            if (locomotionController == null)
                return;

            AnimationPhasePlaybackProgress locomotionProgress = locomotionController.CurrentAnimationPlaybackProgress;
            string locomotionAnimationName = locomotionController.CurrentAnimationName;
            ActionAnimationPlaybackProgress actionProgress = animationPresenter != null
                ? animationPresenter.CurrentPlaybackProgress
                : ActionAnimationPlaybackProgress.Invalid;
            string actionAnimationName = animationPresenter != null ? animationPresenter.CurrentAnimationName : string.Empty;

            locomotionController.WriteAnimationFacts(new CharacterRuntimeAnimationFacts(
                locomotionProgress,
                locomotionAnimationName,
                actionProgress,
                actionAnimationName,
                step));
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
                FullBodyDiagnostics.LogFullBodyPathChanged(in previousSnapshot, in snapshot, step);
                lastLoggedFullBodyPath = snapshot.ActivePath;
            }

            if (snapshot.PendingTransitionPath == lastLoggedPendingTransitionPath)
                return;

            FullBodyDiagnostics.LogFullBodyPendingTransitionChanged(in previousSnapshot, in snapshot, step);
            lastLoggedPendingTransitionPath = snapshot.PendingTransitionPath;
        }

        void LogLocomotionStateChange(in CharacterStateMachineSnapshot snapshot, int step)
        {
            string locomotionPath = snapshot.IsLocomotion ? snapshot.ActivePath : lastLoggedLocomotionPath;
            if (loggedInitialLocomotionState &&
                snapshot.LocomotionPhase == lastLoggedLocomotionPhase &&
                locomotionPath == lastLoggedLocomotionPath)
                return;

            FullBodyDiagnostics.LogLocomotionPhaseChanged(
                locomotionPath,
                lastLoggedLocomotionPath,
                lastLoggedLocomotionPhase,
                LastLocomotionFrame.Command.Gait,
                in snapshot,
                step);
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

            FullBodyDiagnostics.LogActionAccepted(in previousSnapshot, in snapshot, in frame, step);
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
            FullBodyDiagnostics.LogFullBodyTickSnapshot(in currentStateSnapshot, step, BuildFullBodyTickContext());
        }

        void LogAnimationTickSnapshot(int step)
        {
            FullBodyDiagnostics.LogAnimationTickSnapshot(currentStateSnapshot.ActivePath, step, BuildAnimationTickContext());
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
                $"actionKey={(animationPresenter != null ? animationPresenter.CurrentKey.Value : string.Empty)} actionAnimation={(animationPresenter != null ? animationPresenter.CurrentAnimationName : string.Empty)} actionNormalized={(animationPresenter != null ? animationPresenter.CurrentNormalizedTime : 0f):F3} actionValid={(animationPresenter != null && animationPresenter.HasValidPlayback)} actionEnded={(animationPresenter != null && animationPresenter.CurrentPlaybackProgress.IsEnded)}";
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
                CharacterStateMachineDefinitionSO definitionAsset = ResolveStateMachineDefinition();
                if (definitionAsset == null)
                    throw new InvalidOperationException("Character state machine config is missing. Assign CharacterConfigSO.StateMachine.");

                CharacterStateMachineDefinition definition = definitionAsset.ToDefinition();
                stateMachine = new CharacterStateMachineRunner(definition);
            }
            catch (System.Exception exception)
            {
                SetInactiveStateSnapshot();
                if (logErrors)
                    FullBodyDiagnostics.LogStateMachineDefinitionInvalid(exception.Message);
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
            inputBufferComponent = FullBodyReferenceResolver.ResolveInputBuffer(this, inputBufferComponent);
            locomotionController = FullBodyReferenceResolver.ResolveLocomotionController(this, locomotionController);

            if (actionMovementExecutorBehaviour == null && FullBodyReferenceResolver.TryResolveLocomotionActionExecutor(locomotionController, out IActionMovementExecutor locomotionExecutor, out MonoBehaviour locomotionExecutorBehaviour))
            {
                actionMovementExecutor = locomotionExecutor;
                actionMovementExecutorBehaviour = locomotionExecutorBehaviour;
            }
            else if (actionMovementExecutorBehaviour == null && FullBodyReferenceResolver.TryResolveComponentInterface(this, out IActionMovementExecutor resolvedExecutor, out MonoBehaviour executorBehaviour))
            {
                actionMovementExecutor = resolvedExecutor;
                actionMovementExecutorBehaviour = executorBehaviour;
            }
            else
            {
                actionMovementExecutor = actionMovementExecutorBehaviour as IActionMovementExecutor;
            }

            facingProviderBehaviour = FullBodyReferenceResolver.ResolveFacingProviderBehaviour(this, facingProviderBehaviour);

            if (animationPresenterBehaviour == null && FullBodyReferenceResolver.TryResolveComponentInterface(this, out IActionAnimationPresenter resolvedPresenter, out MonoBehaviour presenterBehaviour))
                animationPresenterBehaviour = presenterBehaviour;

            ResolveAnimationPresenter();
        }

        void ResolveAnimationPresenter()
        {
            animationPresenter = animationPresenterBehaviour as IActionAnimationPresenter;
            actionPlaybackProgressController = animationPresenterBehaviour as IActionAnimationPlaybackProgressController;
        }

        CharacterConfigSO ResolveCharacterConfig()
        {
            if (characterConfig != null)
                return characterConfig;

            return locomotionController != null ? locomotionController.CharacterConfig : null;
        }

        CharacterStateMachineDefinitionSO ResolveStateMachineDefinition()
        {
            CharacterConfigSO config = ResolveCharacterConfig();
            return config != null ? config.StateMachine : null;
        }

        IReadOnlyList<ActionInterruptPolicy> ResolveInterruptPolicies()
        {
            if (interruptPoliciesCompiled && cachedInterruptPolicySet == interruptPolicySet)
                return runtimeInterruptPolicies ?? Array.Empty<ActionInterruptPolicy>();

            cachedInterruptPolicySet = interruptPolicySet;
            runtimeInterruptPolicies = interruptPolicySet != null
                ? interruptPolicySet.CompilePolicies()
                : Array.Empty<ActionInterruptPolicy>();
            interruptPoliciesCompiled = true;
            return runtimeInterruptPolicies;
        }

        void ClearInterruptPolicyCache()
        {
            cachedInterruptPolicySet = null;
            runtimeInterruptPolicies = Array.Empty<ActionInterruptPolicy>();
            interruptPoliciesCompiled = false;
        }

    }
}
