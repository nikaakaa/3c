using System;
using System.Collections.Generic;
using ThirdPersonCharacterBehavior;
using ThirdPersonCharacterConfig;
using ThirdPersonCharacterStateMachine;
using ThirdPersonInput;
using ThirdPersonMovement;
using ThirdPersonSimulation;

namespace ThirdPersonAction
{
    public readonly struct CharacterRuntimeCoreDependencies
    {
        public CharacterRuntimeCoreDependencies(
            CharacterConfigSO characterConfig,
            InputRequestBufferComponent inputBufferComponent,
            ILocomotionRuntimeUnityAdapter locomotionAdapter,
            ICommittedActionRuntimeUnityAdapter committedActionAdapter,
            UnityInputSystemRequestBufferAdapter requestBufferAdapter)
        {
            CharacterConfig = characterConfig;
            InputBufferComponent = inputBufferComponent;
            LocomotionAdapter = locomotionAdapter;
            CommittedActionAdapter = committedActionAdapter;
            RequestBufferAdapter = requestBufferAdapter;
        }

        public CharacterConfigSO CharacterConfig { get; }
        public InputRequestBufferComponent InputBufferComponent { get; }
        public ILocomotionRuntimeUnityAdapter LocomotionAdapter { get; }
        public ICommittedActionRuntimeUnityAdapter CommittedActionAdapter { get; }
        public UnityInputSystemRequestBufferAdapter RequestBufferAdapter { get; }
    }

    public readonly struct CharacterRuntimeCoreRestoreState
    {
        public CharacterRuntimeCoreRestoreState(
            LocomotionRuntimeModuleRestoreState locomotion,
            CommittedActionRestoreState committedAction)
        {
            Locomotion = locomotion;
            CommittedAction = committedAction;
        }

        public LocomotionRuntimeModuleRestoreState Locomotion { get; }
        public CommittedActionRestoreState CommittedAction { get; }
    }

    public sealed class CharacterRuntimeCore
    {
        CharacterRuntimeCoreDependencies dependencies;
        CharacterFrameRuntimeHost frameRuntimeHost;
        CharacterFrameRuntimePortAdapter runtimePort;
        LocomotionRuntimeModule locomotionModule;
        CommittedActionRuntimeModule committedActionModule;
        IReadOnlyList<ActionInterruptPolicy> runtimeInterruptPolicies = Array.Empty<ActionInterruptPolicy>();
        ActionInterruptPolicySetSO cachedInterruptPolicySet;
        bool interruptPoliciesCompiled;

        public CharacterRuntimeCore()
        {
            BindRuntimeModules();
        }

        public CharacterRuntimeCore(CharacterRuntimeCoreDependencies dependencies)
        {
            UpdateDependencies(dependencies);
        }

        public CharacterFrameResult LastFramePipelineResult => FrameRuntimeHost.LastFrameResult;
        public ICharacterFrameRuntimePort RuntimePort => runtimePort ?? (runtimePort = new CharacterFrameRuntimePortAdapter(this));
        public LocomotionRuntimeModule LocomotionModule => locomotionModule ?? (locomotionModule = new LocomotionRuntimeModule());
        public CommittedActionRuntimeModule CommittedActionModule => committedActionModule ?? (committedActionModule = new CommittedActionRuntimeModule());
        public ILocomotionFrameRuntimePort LocomotionFrameRuntime => LocomotionModule.FrameRuntimePort;
        public ILocomotionOutputRuntimePort LocomotionOutputRuntime => LocomotionModule.OutputRuntimePort;
        public CharacterStateMachineRunner StateMachine => CommittedActionModule.StateMachine;
        public CharacterStateMachineSnapshot CurrentStateSnapshot => CommittedActionModule.CurrentStateSnapshot;
        public InputRequestBuffer InputRequestBuffer =>
            dependencies.InputBufferComponent != null ? dependencies.InputBufferComponent.Buffer : null;
        public string ActiveFrameStatePath => CommittedActionModule.ActiveStatePath;
        internal CharacterFrameOutputRuntime ActionOutputRuntime => CommittedActionModule.OutputRuntime;
        internal CharacterFrameRuntimeHost FrameRuntimeHost => frameRuntimeHost ?? (frameRuntimeHost = CreateFrameRuntimeHost());

        public void UpdateDependencies(CharacterRuntimeCoreDependencies dependencies)
        {
            if (!ReferenceEquals(this.dependencies.CharacterConfig, dependencies.CharacterConfig))
            {
                ClearInterruptPolicyCache();
                frameRuntimeHost = null;
            }

            this.dependencies = dependencies;
            BindRuntimeModules();
            ApplyFormalConfig();
        }

        public bool Tick(in CharacterFrameInput input)
        {
            if (!PrepareFrameRuntimeAdapters())
                return false;

            return FrameRuntimeHost.Tick(RuntimePort, in input, out _);
        }

        public CharacterFrameContext BeginFrame(in CharacterFrameInput input)
        {
            return FrameRuntimeHost.BeginFrame(in input);
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

            return FrameRuntimeHost.RunPhase(RuntimePort, phase, ref context, out result);
        }

        public bool PrepareFrameRuntimeAdapters()
        {
            BindRuntimeModules();
            ApplyFormalConfig();
            return dependencies.CharacterConfig != null &&
                   dependencies.InputBufferComponent != null &&
                   dependencies.LocomotionAdapter != null &&
                   dependencies.CommittedActionAdapter != null &&
                   TryResolveBehaviorRuntimeDefinition(out _) &&
                   CommittedActionModule.EnsureStateMachine(dependencies.CharacterConfig.StateMachine, true);
        }

        public CharacterRuntimeCoreRestoreState CaptureRestoreState()
        {
            return new CharacterRuntimeCoreRestoreState(
                LocomotionModule.CaptureRestoreState(),
                CommittedActionModule.CaptureRestoreState());
        }

        public bool Restore(in CharacterRuntimeCoreRestoreState state)
        {
            LocomotionRuntimeModuleRestoreState locomotion = state.Locomotion;
            LocomotionModule.Restore(in locomotion);
            CommittedActionRestoreState committedAction = state.CommittedAction;
            return CommittedActionModule.Restore(in committedAction) || !committedAction.Snapshot.ActiveState.IsValid;
        }

        public bool TryResolveActionCatalog(out CharacterActionCatalog catalog)
        {
            CharacterActionCatalogSO asset = dependencies.CharacterConfig != null ? dependencies.CharacterConfig.ActionCatalog : null;
            if (asset != null)
            {
                ActionTimelineCompileContext compileContext = ActionTimelineCompileContext.FromTickRate(SimulationTickRate.Default);
                catalog = asset.ToCatalog(in compileContext);
                return catalog.HasCatalog;
            }

            catalog = CharacterActionCatalog.Empty;
            return false;
        }

        public bool TryResolveBodyClaimPolicy(out BodyClaimPolicy policy)
        {
            BodyClaimPolicySO asset = dependencies.CharacterConfig != null ? dependencies.CharacterConfig.BodyClaimPolicy : null;
            if (asset != null)
            {
                policy = asset.ToPolicy();
                return policy.HasPolicy;
            }

            policy = BodyClaimPolicy.Empty;
            return false;
        }

        public int ResolveCurrentActionResistance()
        {
            if (!TryResolveActionCatalog(out CharacterActionCatalog catalog))
                return 0;

            return CommittedActionModule.ResolveCurrentActionResistance(in catalog);
        }

        public ActionLifecycleFrame TickActionLifecycle(
            in CharacterResolvedAction acceptedAction,
            in CharacterActionCatalog actionCatalog,
            float deltaTime,
            int step)
        {
            return CommittedActionModule.TickActionLifecycle(in acceptedAction, in actionCatalog, deltaTime, step);
        }

        public void CompleteActionLifecycle(in ActionMotionResolveResult result, bool requireAnimationEnded)
        {
            CommittedActionModule.CompleteActionLifecycle(in result, requireAnimationEnded);
        }

        public IReadOnlyList<ActionInterruptPolicy> ResolveInterruptPolicies()
        {
            ActionInterruptPolicySetSO policySet = dependencies.CharacterConfig != null
                ? dependencies.CharacterConfig.ActionInterruptPolicy
                : null;

            if (interruptPoliciesCompiled && cachedInterruptPolicySet == policySet)
                return runtimeInterruptPolicies ?? Array.Empty<ActionInterruptPolicy>();

            cachedInterruptPolicySet = policySet;
            runtimeInterruptPolicies = policySet != null
                ? policySet.CompilePolicies()
                : Array.Empty<ActionInterruptPolicy>();
            interruptPoliciesCompiled = true;
            return runtimeInterruptPolicies;
        }

        public bool WriteBufferedInputFacts(in CharacterFrameInput input)
        {
            InputRequestBufferComponent buffer = dependencies.InputBufferComponent;
            if (buffer == null)
                return false;

            buffer.SetStep(input.Step);
            buffer.AddButtonState(InputButtonKind.Dodge, ToInputButtonState(input.Dodge));
            buffer.AddButtonState(InputButtonKind.Attack, ToInputButtonState(input.Attack));
            buffer.AddButtonState(InputButtonKind.Jump, ToInputButtonState(input.Jump));
            buffer.AddButtonState(InputButtonKind.Interact, ToInputButtonState(input.Interact));
            return true;
        }

        void BindRuntimeModules()
        {
            LocomotionModule.Bind(dependencies.LocomotionAdapter);
            CommittedActionModule.Bind(dependencies.CommittedActionAdapter);
        }

        void ApplyFormalConfig()
        {
            CharacterConfigSO characterConfig = dependencies.CharacterConfig;
            if (characterConfig == null)
                return;

            if (dependencies.RequestBufferAdapter != null)
                dependencies.RequestBufferAdapter.ApplyFormalInputConfig(characterConfig);
        }

        void ClearInterruptPolicyCache()
        {
            cachedInterruptPolicySet = null;
            runtimeInterruptPolicies = Array.Empty<ActionInterruptPolicy>();
            interruptPoliciesCompiled = false;
        }

        bool TryResolveBehaviorRuntimeDefinition(out CharacterBehaviorRuntimeDefinition definition)
        {
            CharacterBehaviorRuntimeDefinitionSO asset = dependencies.CharacterConfig != null
                ? dependencies.CharacterConfig.BehaviorRuntimeDefinition
                : null;
            if (asset == null)
            {
                definition = CharacterBehaviorRuntimeDefinition.Invalid("behavior-entry-definition-missing");
                return false;
            }

            definition = asset.ToDefinition();
            return definition.IsValid;
        }

        CharacterFrameRuntimeHost CreateFrameRuntimeHost()
        {
            TryResolveBehaviorRuntimeDefinition(out CharacterBehaviorRuntimeDefinition definition);
            CharacterBehaviorSubmissionRunner runner = new CharacterBehaviorSubmissionRunner(definition);
            return new CharacterFrameRuntimeHost(runner, runner);
        }

        static InputButtonState ToInputButtonState(PredictionButtonFrame frame)
        {
            return new InputButtonState(frame.Pressed, frame.Held, frame.Released);
        }
    }
}
