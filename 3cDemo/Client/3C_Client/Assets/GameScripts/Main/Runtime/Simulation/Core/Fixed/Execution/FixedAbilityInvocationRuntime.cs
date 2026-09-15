using System;
using System.Collections.Generic;
using ThirdPersonSimulation;

namespace ThirdPersonSimulation.Fixed
{
    internal sealed class FixedAbilityInvocationResult
    {
        public FixedAbilityInvocationResult(
            ResolvedGameplayMotion motion,
            IReadOnlyList<GameplayFact> gameplayFacts,
            IReadOnlyList<PresentationCommand> presentationCommands,
            IReadOnlyList<SimulationTraceRecord> traceRecords)
        {
            Motion = motion;
            GameplayFacts = gameplayFacts ?? throw new ArgumentNullException(nameof(gameplayFacts));
            PresentationCommands = presentationCommands ?? throw new ArgumentNullException(nameof(presentationCommands));
            TraceRecords = traceRecords ?? throw new ArgumentNullException(nameof(traceRecords));
        }

        public ResolvedGameplayMotion Motion { get; }
        public IReadOnlyList<GameplayFact> GameplayFacts { get; }
        public IReadOnlyList<PresentationCommand> PresentationCommands { get; }
        public IReadOnlyList<SimulationTraceRecord> TraceRecords { get; }
    }

    internal interface IFixedAbilityExecutionServiceFactory
    {
        FixedGameplayEffectOperationRuntime CreateGameplayEffects(
            FixedGameplayAbilityExecutionInstallation installation,
            FixedGameplayAbilityExecutionAccess access,
            FixedAbilityExecutionFrame frame,
            FixedActionStateStore actions,
            FixedHandleAllocator handles,
            FixedFactSink facts,
            FixedPresentationSink presentation,
            FixedTraceSink trace,
            FixedAbilityExecutionWorkspace workspace);

        FixedEquipmentRuntime CreateEquipment(
            FixedGameplayAbilityExecutionInstallation installation,
            FixedGameplayAbilityExecutionAccess access,
            FixedAbilityExecutionFrame frame,
            FixedActionStateStore actions,
            FixedHandleAllocator handles,
            FixedGameplayEffectOperationRuntime gameplayEffects);

        FixedLocomotionRuntime CreateLocomotion(
            FixedGameplayAbilityExecutionInstallation installation,
            FixedGameplayAbilityExecutionAccess access,
            FixedValueRuntime values,
            FixedMotionAccumulator motion,
            FixedAbilityExecutionFrame frame);
    }

    internal sealed class FixedAbilityInvocationRuntime : IDisposable
    {
        readonly IFixedAbilityDomainStatePort m_DomainState;
        readonly Action<IFixedAbilityExecutionStateTransaction> m_AcceptAbility;
        readonly IFixedAbilityExecutionStateTransaction m_AbilityState;
        readonly FixedAbilityExecutionWorkspace m_Workspace;
        readonly FixedAbilityExecutionFrame m_Frame;
        readonly FixedInputRuntime m_Input;
        readonly FixedActionStateStore m_ActionStore;
        readonly FixedActionRuntime m_Actions;
        readonly FixedGameplayEffectOperationRuntime m_GameplayEffects;
        readonly FixedEquipmentRuntime m_Equipment;
        readonly FixedValueRuntime m_Values;
        readonly FixedBlackboardRuntime m_Blackboard;
        readonly FixedMotionAccumulator m_Motion;
        readonly FixedLocomotionRuntime m_Locomotion;
        readonly FixedAbilityControlRuntime m_Control;
        readonly FixedAbilityDomainRuntime m_Domain;
        bool m_Begun;
        bool m_Completed;
        bool m_Accepted;
        bool m_Disposed;

        public FixedAbilityInvocationRuntime(
            FixedGameplayAbilityExecutionInstallation installation,
            FixedGameplayAbilityExecutionInstallationSet installations,
            IFixedAbilityExecutionStateTransaction abilityState,
            IFixedAbilityDomainStatePort domainState,
            ActorId actorId,
            SimulationTick tick,
            FixedAbilityExecutionInput input,
            IReadOnlyList<SimulationIngress> ingress,
            FixedAbilityBodyFacts bodyFacts,
            FixedAbilityExecutionWorkspace workspace,
            IFixedInputRequestStatePort inputRequests,
            IFixedActionRuntimeStatePort actionState,
            IFixedAbilityExecutionServiceFactory serviceFactory,
            Action<IFixedAbilityExecutionStateTransaction> acceptAbility)
        {
            Installation = installation ?? throw new ArgumentNullException(nameof(installation));
            installations = installations ?? throw new ArgumentNullException(nameof(installations));
            m_DomainState = domainState ?? throw new ArgumentNullException(nameof(domainState));
            m_AcceptAbility = acceptAbility ?? throw new ArgumentNullException(nameof(acceptAbility));
            if (!actorId.IsValid || !tick.IsValid)
                throw new ArgumentException("Fixed Ability invocation identity is incomplete.");
            if (input == null)
                throw new ArgumentNullException(nameof(input));
            m_AbilityState = abilityState ?? throw new ArgumentNullException(nameof(abilityState));
            m_Workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
            m_Workspace.Reset();
            m_Frame = new FixedAbilityExecutionFrame(
                installation,
                actorId,
                tick,
                input,
                ingress ?? Array.Empty<SimulationIngress>(),
                bodyFacts,
                m_AbilityState,
                m_DomainState,
                inputRequests,
                actionState,
                m_Workspace);

            FixedGameplayAbilityExecutionAccess access = installation.Access;
            FixedStatePort controlState = m_Frame.CreateStatePort(
                "Control",
                installation.Services.ControlPolicy);
            m_ActionStore = new FixedActionStateStore(access, m_Frame);
            m_Input = new FixedInputRuntime(access, m_Frame);
            var handles = new FixedHandleAllocator(access, m_Frame);
            m_Blackboard = new FixedBlackboardRuntime(
                access,
                m_Frame.CreateStatePort("Blackboard", installation.Services.BlackboardPolicy),
                m_Frame,
                m_ActionStore,
                m_Frame.Facts,
                m_Frame.Trace,
                m_Workspace);
            m_GameplayEffects = serviceFactory.CreateGameplayEffects(
                installation,
                access,
                m_Frame,
                m_ActionStore,
                handles,
                m_Frame.Facts,
                m_Frame.Presentation,
                m_Frame.Trace,
                m_Workspace);

            m_Equipment = serviceFactory.CreateEquipment(
                installation,
                access,
                m_Frame,
                m_ActionStore,
                handles,
                m_GameplayEffects);
            bool equipmentEnabled = installation.Data.Capabilities.HasGameplayCapability("Equipment");
            if (equipmentEnabled && m_Equipment == null)
                throw new InvalidOperationException(
                    $"Ability '{installation.Data.AbilityId}' requires the declared Equipment service.");

            FixedAbilityControlRuntime control = null;
            m_Actions = new FixedActionRuntime(
                access,
                installations,
                m_Frame,
                m_Input,
                m_ActionStore,
                m_Blackboard,
                m_GameplayEffects,
                m_GameplayEffects,
                handles,
                m_Frame.Facts,
                m_Frame.Trace,
                m_Equipment,
                operation => control == null ||
                    !control.IsActive(operation) && !control.IsStopping(operation));
            m_Values = new FixedValueRuntime(
                access,
                m_Input,
                m_ActionStore,
                m_Actions,
                m_GameplayEffects,
                m_Equipment,
                m_Blackboard,
                m_Frame,
                m_Workspace);
            m_Motion = new FixedMotionAccumulator(
                access,
                m_Frame,
                m_Workspace.MotionContributions,
                m_Workspace.MotionWarpSamples,
                m_ActionStore);
            m_Locomotion = serviceFactory.CreateLocomotion(
                installation,
                access,
                m_Values,
                m_Motion,
                m_Frame);
            var target = new FixedAbilityExecutionTarget(
                access,
                controlState,
                m_Frame.CreateOperationStateReset(),
                m_Values,
                m_Blackboard,
                m_Actions,
                m_GameplayEffects,
                m_Equipment,
                m_Locomotion,
                m_Frame.Facts,
                m_Frame.Presentation,
                m_Frame.Trace);
            var services = new FixedAbilityExecutionServiceSet(
                m_Frame,
                target,
                m_Actions,
                m_ActionStore,
                m_Input,
                m_GameplayEffects,
                m_Equipment,
                m_Values,
                m_Blackboard,
                m_Motion);
            control = new FixedAbilityControlRuntime(installation, services);
            m_Control = control;
            m_Domain = new FixedAbilityDomainRuntime(
                installation,
                m_Actions,
                m_ActionStore,
                m_Control);
        }

        public FixedGameplayAbilityExecutionInstallation Installation { get; }
        public FixedAbilityExecutionFrame Frame => m_Frame;
        public FixedAbilityExecutionWorkspace Workspace => m_Workspace;
        public FixedInputRuntime Input => m_Input;
        public FixedActionRuntime Actions => m_Actions;
        public FixedGameplayEffectOperationRuntime GameplayEffects => m_GameplayEffects;
        public FixedEquipmentRuntime Equipment => m_Equipment;
        public FixedBlackboardRuntime Blackboard => m_Blackboard;
        public FixedMotionAccumulator Motion => m_Motion;
        public FixedLocomotionRuntime Locomotion => m_Locomotion;
        public FixedAbilityControlRuntime Control => m_Control;

        public void BeginEvaluation(
            bool diagnosticsEnabled,
            bool captureValues,
            bool captureControlFlow)
        {
            RequireOpen();
            if (m_Begun)
                throw new InvalidOperationException("Fixed Ability invocation evaluation is already active.");
            m_Control.BeginEvaluation(diagnosticsEnabled, captureValues, captureControlFlow);
            m_Begun = true;
        }

        public void ApplyActionIngress(SimulationIngress ingress)
        {
            RequireEvaluation();
            m_Actions.ApplyIngress(ingress);
        }

        public void ApplyGameplayEffectIngress(SimulationIngress ingress)
        {
            RequireEvaluation();
            (m_GameplayEffects ?? throw new InvalidOperationException(
                "Fixed Ability invocation has no declared Gameplay Effect service.")).ApplyIngress(ingress);
        }

        public void AdvanceGameplayEffects()
        {
            RequireEvaluation();
            (m_GameplayEffects ?? throw new InvalidOperationException(
                "Fixed Ability invocation has no declared Gameplay Effect service.")).Advance();
        }

        public void ApplyInputBindings()
        {
            RequireEvaluation();
            m_Input.ApplyBlackboardInputBindings(m_Blackboard);
        }

        public void Tick()
        {
            RequireEvaluation();
            m_Domain.Tick();
        }

        public FixedAbilityInvocationResult Complete()
        {
            RequireEvaluation();
            if (m_Completed)
                throw new InvalidOperationException("Fixed Ability invocation has already completed.");
            ResolvedGameplayMotion motion = m_Motion.Resolve();
            m_Control.EndEvaluation();
            var result = new FixedAbilityInvocationResult(
                motion,
                new List<GameplayFact>(m_Workspace.Facts),
                new List<PresentationCommand>(m_Workspace.Presentation),
                new List<SimulationTraceRecord>(m_Workspace.Trace));
            m_Frame.End();
            m_Completed = true;
            return result;
        }

        public void Accept()
        {
            RequireOpen();
            if (!m_Completed || m_Accepted)
                throw new InvalidOperationException("Fixed Ability invocation cannot accept its current candidate.");
            m_AcceptAbility(m_AbilityState);
            m_AbilityState.Dispose();
            m_Accepted = true;
        }

        public void Abort()
        {
            if (m_Disposed)
                return;
            if (m_Begun && !m_Completed)
            {
                m_Control.EndEvaluation();
                m_Frame.End();
                m_Completed = true;
            }
            if (!m_Accepted)
                m_AbilityState.Abort();
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            if (!m_Accepted)
                Abort();
            m_AbilityState.Dispose();
            m_Disposed = true;
        }

        void RequireEvaluation()
        {
            RequireOpen();
            if (!m_Begun || m_Completed)
                throw new InvalidOperationException("Fixed Ability invocation is not in its evaluation phase.");
        }

        void RequireOpen()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(FixedAbilityInvocationRuntime));
        }
    }
}
