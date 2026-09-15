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
        FixedAbilityExecutionAssembly Create(
            FixedGameplayAbilityExecutionInstallation installation,
            IFixedAbilityInstallationProvider installations,
            FixedAbilityExecutionFrame frame,
            FixedAbilityExecutionWorkspace workspace);
    }

    public interface IFixedAbilityInstallationProvider
    {
        FixedGameplayAbilityExecutionInstallation Require(CharacterSkillId abilityId);
    }

    internal sealed class FixedAbilityExecutionAssembly
    {
        public FixedAbilityExecutionAssembly(
            FixedInputRuntime input,
            FixedActionRuntime actions,
            FixedGameplayEffectOperationRuntime gameplayEffects,
            FixedEquipmentRuntime equipment,
            FixedBlackboardRuntime blackboard,
            FixedMotionAccumulator motion,
            FixedAbilityControlRuntime control,
            FixedAbilityDomainRuntime domain)
        {
            Input = input ?? throw new ArgumentNullException(nameof(input));
            Actions = actions ?? throw new ArgumentNullException(nameof(actions));
            GameplayEffects = gameplayEffects;
            Equipment = equipment;
            Blackboard = blackboard ?? throw new ArgumentNullException(nameof(blackboard));
            Motion = motion ?? throw new ArgumentNullException(nameof(motion));
            Control = control ?? throw new ArgumentNullException(nameof(control));
            Domain = domain ?? throw new ArgumentNullException(nameof(domain));
        }

        public FixedInputRuntime Input { get; }
        public FixedActionRuntime Actions { get; }
        public FixedGameplayEffectOperationRuntime GameplayEffects { get; }
        public FixedEquipmentRuntime Equipment { get; }
        public FixedBlackboardRuntime Blackboard { get; }
        public FixedMotionAccumulator Motion { get; }
        public FixedAbilityControlRuntime Control { get; }
        public FixedAbilityDomainRuntime Domain { get; }
    }

    internal sealed class FixedAbilityInvocationRuntime : IDisposable
    {
        readonly Action<IFixedSkillExecutionState> m_AcceptAbility;
        readonly IFixedSkillExecutionState m_SkillState;
        readonly FixedAbilityExecutionWorkspace m_Workspace;
        readonly FixedAbilityExecutionFrame m_Frame;
        readonly FixedInputRuntime m_Input;
        readonly FixedActionRuntime m_Actions;
        readonly FixedGameplayEffectOperationRuntime m_GameplayEffects;
        readonly FixedEquipmentRuntime m_Equipment;
        readonly FixedBlackboardRuntime m_Blackboard;
        readonly FixedMotionAccumulator m_Motion;
        readonly FixedAbilityControlRuntime m_Control;
        readonly FixedAbilityDomainRuntime m_Domain;
        bool m_Begun;
        bool m_Completed;
        bool m_Accepted;
        bool m_Disposed;

        public FixedAbilityInvocationRuntime(
            FixedGameplayAbilityExecutionInstallation installation,
            IFixedAbilityInstallationProvider installations,
            IFixedSkillExecutionState skillState,
            IFixedAbilityExecutionSavepointPort savepointPort,
            ActorId actorId,
            SimulationTick tick,
            FixedAbilityExecutionInput input,
            IReadOnlyList<SimulationIngress> ingress,
            FixedAbilityBodyFacts bodyFacts,
            FixedAbilityExecutionWorkspace workspace,
            IFixedInputRequestStatePort inputRequests,
            IFixedActionRuntimeStatePort actionState,
            IFixedHandleAllocatorStatePort handleAllocatorState,
            IFixedEventSequenceStatePort eventSequenceState,
            IFixedGameplayEffectStatePort gameplayEffectState,
            IFixedEquipmentStatePort equipmentState,
            IFixedAbilityExecutionServiceFactory serviceFactory,
            Action<IFixedSkillExecutionState> acceptAbility)
        {
            Installation = installation ?? throw new ArgumentNullException(nameof(installation));
            installations = installations ?? throw new ArgumentNullException(nameof(installations));
            m_AcceptAbility = acceptAbility ?? throw new ArgumentNullException(nameof(acceptAbility));
            if (!actorId.IsValid || !tick.IsValid)
                throw new ArgumentException("Fixed Ability invocation identity is incomplete.");
            if (input == null)
                throw new ArgumentNullException(nameof(input));
            m_SkillState = skillState ?? throw new ArgumentNullException(nameof(skillState));
            m_Workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
            m_Workspace.Reset();
            m_Frame = new FixedAbilityExecutionFrame(
                installation,
                actorId,
                tick,
                input,
                ingress ?? Array.Empty<SimulationIngress>(),
                bodyFacts,
                m_SkillState,
                savepointPort,
                inputRequests,
                actionState,
                handleAllocatorState,
                eventSequenceState,
                gameplayEffectState,
                equipmentState,
                m_Workspace);

            FixedAbilityExecutionAssembly assembly = serviceFactory.Create(
                installation,
                installations,
                m_Frame,
                m_Workspace);
            m_Input = assembly.Input;
            m_Actions = assembly.Actions;
            m_GameplayEffects = assembly.GameplayEffects;
            m_Equipment = assembly.Equipment;
            m_Blackboard = assembly.Blackboard;
            m_Motion = assembly.Motion;
            m_Control = assembly.Control;
            m_Domain = assembly.Domain;
        }

        public FixedGameplayAbilityExecutionInstallation Installation { get; }
        public FixedAbilityExecutionWorkspace Workspace => m_Workspace;
        public FixedActionRuntime Actions => m_Actions;
        public FixedGameplayEffectOperationRuntime GameplayEffects => m_GameplayEffects;
        public FixedEquipmentRuntime Equipment => m_Equipment;

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
            m_AcceptAbility(m_SkillState);
            m_SkillState.Dispose();
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
                m_SkillState.Dispose();
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            if (!m_Accepted)
                Abort();
            m_SkillState.Dispose();
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
