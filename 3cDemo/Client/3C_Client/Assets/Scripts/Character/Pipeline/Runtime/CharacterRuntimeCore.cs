using System;
using System.Collections.Generic;
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
            IFullBodyActionRuntimeUnityAdapter fullBodyActionAdapter,
            UnityInputSystemRequestBufferAdapter requestBufferAdapter)
        {
            CharacterConfig = characterConfig;
            InputBufferComponent = inputBufferComponent;
            LocomotionAdapter = locomotionAdapter;
            FullBodyActionAdapter = fullBodyActionAdapter;
            RequestBufferAdapter = requestBufferAdapter;
        }

        public CharacterConfigSO CharacterConfig { get; }
        public InputRequestBufferComponent InputBufferComponent { get; }
        public ILocomotionRuntimeUnityAdapter LocomotionAdapter { get; }
        public IFullBodyActionRuntimeUnityAdapter FullBodyActionAdapter { get; }
        public UnityInputSystemRequestBufferAdapter RequestBufferAdapter { get; }
    }

    public readonly struct CharacterRuntimeCoreRestoreState
    {
        public CharacterRuntimeCoreRestoreState(
            LocomotionRuntimeModuleRestoreState locomotion,
            FullBodyActionRestoreState fullBody)
        {
            Locomotion = locomotion;
            FullBody = fullBody;
        }

        public LocomotionRuntimeModuleRestoreState Locomotion { get; }
        public FullBodyActionRestoreState FullBody { get; }
    }

    public sealed class CharacterRuntimeCore
    {
        CharacterRuntimeCoreDependencies dependencies;
        CharacterFrameRuntimeHost frameRuntimeHost;
        CharacterFrameRuntimePortAdapter runtimePort;
        LocomotionRuntimeModule locomotionModule;
        FullBodyActionRuntimeModule fullBodyModule;
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
        public FullBodyActionRuntimeModule FullBodyModule => fullBodyModule ?? (fullBodyModule = new FullBodyActionRuntimeModule());
        public ILocomotionFrameRuntimePort LocomotionFrameRuntime => LocomotionModule.FrameRuntimePort;
        public ILocomotionOutputRuntimePort LocomotionOutputRuntime => LocomotionModule.OutputRuntimePort;
        public CharacterStateMachineRunner StateMachine => FullBodyModule.StateMachine;
        public CharacterStateMachineSnapshot CurrentStateSnapshot => FullBodyModule.CurrentStateSnapshot;
        public InputRequestBuffer InputRequestBuffer =>
            dependencies.InputBufferComponent != null ? dependencies.InputBufferComponent.Buffer : null;
        public string ActiveFrameStatePath => FullBodyModule.ActiveFullBodyStatePath;
        internal FullBodyOutputRuntime ActionOutputRuntime => FullBodyModule.OutputRuntime;
        internal CharacterFrameRuntimeHost FrameRuntimeHost => frameRuntimeHost ?? (frameRuntimeHost = CreateFrameRuntimeHost());

        public void UpdateDependencies(CharacterRuntimeCoreDependencies dependencies)
        {
            if (!ReferenceEquals(this.dependencies.CharacterConfig, dependencies.CharacterConfig))
                ClearInterruptPolicyCache();

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
                   dependencies.FullBodyActionAdapter != null &&
                   FullBodyModule.EnsureStateMachine(dependencies.CharacterConfig.StateMachine, true);
        }

        public CharacterRuntimeCoreRestoreState CaptureRestoreState()
        {
            return new CharacterRuntimeCoreRestoreState(
                LocomotionModule.CaptureRestoreState(),
                FullBodyModule.CaptureRestoreState());
        }

        public bool Restore(in CharacterRuntimeCoreRestoreState state)
        {
            LocomotionRuntimeModuleRestoreState locomotion = state.Locomotion;
            LocomotionModule.Restore(in locomotion);
            FullBodyActionRestoreState fullBody = state.FullBody;
            return FullBodyModule.Restore(in fullBody) || !fullBody.Snapshot.ActiveState.IsValid;
        }

        public bool TryResolveActionCatalog(out CharacterActionCatalog catalog)
        {
            CharacterActionCatalogSO asset = dependencies.CharacterConfig != null ? dependencies.CharacterConfig.ActionCatalog : null;
            if (asset != null)
            {
                catalog = asset.ToCatalog();
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

            return FullBodyModule.ResolveCurrentActionResistance(in catalog);
        }

        public ActionLifecycleFrame TickActionLifecycle(
            in CharacterResolvedAction acceptedAction,
            in CharacterActionCatalog actionCatalog,
            float deltaTime,
            int step)
        {
            return FullBodyModule.TickActionLifecycle(in acceptedAction, in actionCatalog, deltaTime, step);
        }

        public void CompleteActionLifecycle(in ActionMotionResolveResult result, bool requireAnimationEnded)
        {
            FullBodyModule.CompleteActionLifecycle(in result, requireAnimationEnded);
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
            FullBodyModule.Bind(dependencies.FullBodyActionAdapter);
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

        static CharacterFrameRuntimeHost CreateFrameRuntimeHost()
        {
            CharacterFrameSubmitterGraph submitterGraph = CharacterFrameSubmitterGraph.CreateDefault();
            return new CharacterFrameRuntimeHost(submitterGraph, submitterGraph);
        }

        static InputButtonState ToInputButtonState(PredictionButtonFrame frame)
        {
            return new InputButtonState(frame.Pressed, frame.Held, frame.Released);
        }
    }
}
