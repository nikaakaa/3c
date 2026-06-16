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
        readonly FullBodySubmissionBuilder frameSubmissionBuilder = new FullBodySubmissionBuilder();
        CharacterFramePipelineHost framePipelineHost;
        FullBodyRuntimePortAdapter runtimePort;
        FullBodyOutputRuntime outputRuntime;
        FullBodyOutputRuntimeHost outputRuntimeHost;

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
        public FullBodyOwner CurrentOwner => FullBodyStateView.FromSnapshot(in currentStateSnapshot).Owner;
        public CharacterStateMachineSnapshot CurrentStateSnapshot => currentStateSnapshot;
        public string ActiveFullBodyStatePath => currentStateSnapshot.ActivePath;
        public string PendingFullBodyTransitionPath => currentStateSnapshot.PendingTransitionPath;
        public CharacterStateMachineFrame LastStateFrame { get; private set; }
        public BasicLocomotionFrame LastLocomotionFrame { get; private set; }
        public ActionMotionResolveResult LastActionMotionResult { get; private set; }
        public CharacterFrameResult LastFramePipelineResult => FramePipelineHost.LastFrameResult;
        public ICharacterFrameRuntimePort RuntimePort => runtimePort ?? (runtimePort = new FullBodyRuntimePortAdapter(this));
        internal FullBodyOutputRuntime OutputRuntime => outputRuntime ?? (outputRuntime = CreateOutputRuntime());
        internal CharacterFramePipelineHost FramePipelineHost => framePipelineHost ?? (framePipelineHost = CreateFramePipelineHost());

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
            ResolveReferences();
            return FramePipelineHost.Tick(RuntimePort, in input, out _);
        }

        CharacterFramePipelineHost CreateFramePipelineHost()
        {
            return new CharacterFramePipelineHost(frameSubmissionBuilder, frameSubmissionBuilder);
        }

        FullBodyOutputRuntime CreateOutputRuntime()
        {
            outputRuntimeHost ??= new FullBodyOutputRuntimeHost(this);
            FullBodyDiagnosticSubmitter diagnostics = new FullBodyDiagnosticSubmitter(outputRuntimeHost, outputRuntimeHost);
            return new FullBodyOutputRuntime(
                new FullBodyOutputCacheWriter(outputRuntimeHost),
                new FullBodyInputRequestConsumer(outputRuntimeHost),
                new FullBodyMotionOutputApplier(outputRuntimeHost),
                new FullBodyAnimationOutputPresenter(outputRuntimeHost),
                new FullBodyRuntimeFactsWriter(outputRuntimeHost),
                new FullBodySnapshotWriter(outputRuntimeHost, diagnostics),
                diagnostics);
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

            if (hasDodgeConfig && !FullBodyActionInterruptRequestFactory.HasDodgePolicy(policies, in config))
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
            FullBodyStateView stateView = FullBodyStateView.FromSnapshot(in snapshot);
            if (!stateView.Owner.IsAction)
                return 0;

            return stateView.ActionState == ActionStateIds.Dodge ? config.Resistance : 0;
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

        internal bool TryReadFrameInputFromSource(float deltaTime, int step, out CharacterFrameInput input)
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

            input = CharacterFrameInput.FromLocomotionInput(step, in locomotionInput);
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
            FullBodyStateView stateView = FullBodyStateView.FromSnapshot(in currentStateSnapshot);
            debugFullBodyStatePath = currentStateSnapshot.ActivePath;
            debugPendingTransitionPath = currentStateSnapshot.PendingTransitionPath;
            lastLoggedFullBodyPath = currentStateSnapshot.ActivePath;
            lastLoggedPendingTransitionPath = currentStateSnapshot.PendingTransitionPath;
            lastLoggedLocomotionPhase = stateView.LocomotionPhase;
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

        sealed class FullBodyOutputRuntimeHost :
            IFullBodyOutputFrameCache,
            IFullBodyInputRequestConsumerDependencies,
            IFullBodyMotionOutputDependencies,
            IFullBodyAnimationOutputDependencies,
            IFullBodyRuntimeFactsDependencies,
            IFullBodySnapshotOutputState,
            IFullBodyDiagnosticDependencies
        {
            readonly PlayerFullBodyActionController controller;

            public FullBodyOutputRuntimeHost(PlayerFullBodyActionController controller)
            {
                this.controller = controller ?? throw new ArgumentNullException(nameof(controller));
            }

            public BasicLocomotionFrame LastLocomotionFrame
            {
                get => controller.LastLocomotionFrame;
                set => controller.LastLocomotionFrame = value;
            }

            public CharacterStateMachineFrame LastStateFrame
            {
                get => controller.LastStateFrame;
                set => controller.LastStateFrame = value;
            }

            public ActionMotionResolveResult LastActionMotionResult
            {
                get => controller.LastActionMotionResult;
                set => controller.LastActionMotionResult = value;
            }

            public CharacterStateMachineSnapshot CurrentStateSnapshot
            {
                get => controller.currentStateSnapshot;
                set => controller.currentStateSnapshot = value;
            }

            public string DebugFullBodyStatePath
            {
                get => controller.debugFullBodyStatePath;
                set => controller.debugFullBodyStatePath = value ?? string.Empty;
            }

            public string DebugPendingTransitionPath
            {
                get => controller.debugPendingTransitionPath;
                set => controller.debugPendingTransitionPath = value ?? string.Empty;
            }

            public string LastLoggedFullBodyPath
            {
                get => controller.lastLoggedFullBodyPath;
                set => controller.lastLoggedFullBodyPath = value ?? string.Empty;
            }

            public string LastLoggedPendingTransitionPath
            {
                get => controller.lastLoggedPendingTransitionPath;
                set => controller.lastLoggedPendingTransitionPath = value ?? string.Empty;
            }

            public string LastLoggedLocomotionPath
            {
                get => controller.lastLoggedLocomotionPath;
                set => controller.lastLoggedLocomotionPath = value ?? string.Empty;
            }

            public BasicMovementPhase LastLoggedLocomotionPhase
            {
                get => controller.lastLoggedLocomotionPhase;
                set => controller.lastLoggedLocomotionPhase = value;
            }

            public bool LoggedInitialLocomotionState
            {
                get => controller.loggedInitialLocomotionState;
                set => controller.loggedInitialLocomotionState = value;
            }

            public InputRequestBuffer InputRequestBuffer =>
                controller.inputBufferComponent != null ? controller.inputBufferComponent.Buffer : null;

            public IActionMovementExecutor ActionMovementExecutor => controller.actionMovementExecutor;
            public ILocomotionOutputRuntimePort LocomotionOutputRuntime => controller.locomotionController;
            public IActionAnimationPresenter ActionAnimationPresenter => controller.animationPresenter;

            public AnimationPhasePlaybackProgress LocomotionAnimationPlaybackProgress
            {
                get
                {
                    if (controller.locomotionController != null)
                        return controller.locomotionController.CurrentAnimationPlaybackProgress;

                    CharacterStateMachineSnapshot snapshot = controller.currentStateSnapshot;
                    return AnimationPhasePlaybackProgress.Invalid(FullBodyStateView.FromSnapshot(in snapshot).LocomotionPhase);
                }
            }

            public string LocomotionAnimationName =>
                controller.locomotionController != null ? controller.locomotionController.CurrentAnimationName : string.Empty;

            public ActionAnimationKey ActionAnimationKey =>
                controller.animationPresenter != null ? controller.animationPresenter.CurrentKey : default;

            public float ActionAnimationNormalizedTime =>
                controller.animationPresenter != null ? controller.animationPresenter.CurrentNormalizedTime : 0f;

            public bool ActionAnimationHasValidPlayback =>
                controller.animationPresenter != null && controller.animationPresenter.HasValidPlayback;

            public bool ActionAnimationPlaybackEnded =>
                controller.animationPresenter != null && controller.animationPresenter.CurrentPlaybackProgress.IsEnded;

            public string ActionAnimationName =>
                controller.animationPresenter != null ? controller.animationPresenter.CurrentAnimationName : string.Empty;

            public void LogLocomotionDiagnosticTickSnapshot(int step)
            {
                if (controller.locomotionController != null)
                    controller.locomotionController.LogDiagnosticTickSnapshot(step);
            }
        }

    }
}
