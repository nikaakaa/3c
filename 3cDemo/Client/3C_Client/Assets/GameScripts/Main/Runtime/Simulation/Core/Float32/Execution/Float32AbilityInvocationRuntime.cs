using System;
using System.Collections.Generic;

namespace ThirdPersonSimulation
{
    internal interface IFloat32AbilityActionControlPort
    {
        bool ActivateFromControl(CharacterControlAbilityRequest request);
        void StopFromControl(CharacterControlAbilityStopRequest request);
    }

    internal sealed class Float32AbilityInvocationResult
    {
        public Float32AbilityInvocationResult(
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

    internal interface IFloat32AbilityExecutionServiceFactory
    {
        Float32AbilityExecutionAssembly Create(
            Float32GameplayAbilityExecutionData executionData,
            Float32GameplayAbilityExecutionServices executionServices,
            IFloat32AbilityActionBindingProvider actionBindings,
            IFloat32AbilityDomainRuntimeFactory domainRuntimeFactory,
            EquipmentProgramLayout equipmentLayout,
            Float32AbilityExecutionFrame frame,
            Float32AbilityExecutionWorkspace workspace);
    }

    public interface IFloat32AbilityActionBindingProvider
    {
        GameplayAbilityExecutionBinding RequireActionBinding(CharacterSkillId abilityId);
    }

    internal sealed class Float32AbilityExecutionAssembly
    {
        public Float32AbilityExecutionAssembly(
            Float32InputRuntime input,
            Float32ActionRuntime actions,
            Float32GameplayEffectOperationRuntime gameplayEffects,
            Float32EquipmentRuntime equipment,
            Float32BlackboardRuntime blackboard,
            Float32MotionAccumulator motion,
            Float32AbilityOperationControlRuntime control,
            Float32AbilityDomainRuntime domain)
        {
            Input = input ?? throw new ArgumentNullException(nameof(input));
            ActionRuntime = actions ?? throw new ArgumentNullException(nameof(actions));
            GameplayEffects = gameplayEffects;
            Equipment = equipment;
            Blackboard = blackboard ?? throw new ArgumentNullException(nameof(blackboard));
            Motion = motion ?? throw new ArgumentNullException(nameof(motion));
            Control = control ?? throw new ArgumentNullException(nameof(control));
            Domain = domain ?? throw new ArgumentNullException(nameof(domain));
        }

        public Float32InputRuntime Input { get; }
        public IFloat32AbilityActionControlPort Actions => ActionRuntime;
        internal Float32ActionRuntime ActionRuntime { get; }
        public Float32GameplayEffectOperationRuntime GameplayEffects { get; }
        public Float32EquipmentRuntime Equipment { get; }
        public Float32BlackboardRuntime Blackboard { get; }
        public Float32MotionAccumulator Motion { get; }
        public Float32AbilityOperationControlRuntime Control { get; }
        public Float32AbilityDomainRuntime Domain { get; }
    }

    internal sealed class Float32AbilityInvocationRuntime : IDisposable
    {
        readonly Action<IFloat32SkillExecutionState> m_AcceptAbility;
        readonly IFloat32SkillExecutionState m_SkillState;
        readonly Float32AbilityExecutionWorkspace m_Workspace;
        readonly Float32AbilityExecutionFrame m_Frame;
        readonly Float32InputRuntime m_Input;
        readonly Float32ActionRuntime m_Actions;
        readonly Float32GameplayEffectOperationRuntime m_GameplayEffects;
        readonly IEquipmentActionContextReader m_Equipment;
        readonly Float32BlackboardRuntime m_Blackboard;
        readonly Float32MotionAccumulator m_Motion;
        readonly Float32AbilityOperationControlRuntime m_Control;
        readonly Float32AbilityDomainRuntime m_Domain;
        bool m_Begun;
        bool m_Completed;
        bool m_Accepted;
        bool m_Disposed;

        public Float32AbilityInvocationRuntime(
            Float32AbilityExecutionContext execution,
            IFloat32AbilityActionBindingProvider actionBindings,
            IFloat32AbilityDomainRuntimeFactory domainRuntimeFactory,
            EquipmentProgramLayout equipmentLayout,
            IFloat32SkillExecutionState skillState,
            IFloat32AbilityExecutionSavepointPort savepointPort,
            ActorId actorId,
            SimulationTick tick,
            Float32AbilityExecutionInput input,
            Float32AbilityBodyFacts bodyFacts,
            Float32AbilityExecutionWorkspace workspace,
            IFloat32InputRequestStatePort inputRequests,
            IFloat32ActionRuntimeStatePort actionState,
            IFloat32HandleAllocatorStatePort handleAllocatorState,
            IFloat32EventSequenceStatePort eventSequenceState,
            IFloat32GameplayEffectStatePort gameplayEffectState,
            IFloat32EquipmentStatePort equipmentState,
            IFloat32AbilityExecutionServiceFactory serviceFactory,
            Action<IFloat32SkillExecutionState> acceptAbility)
        {
            execution = execution ?? throw new ArgumentNullException(nameof(execution));
            AbilityId = execution.Data.AbilityId;
            actionBindings = actionBindings ?? throw new ArgumentNullException(nameof(actionBindings));
            domainRuntimeFactory = domainRuntimeFactory ?? throw new ArgumentNullException(nameof(domainRuntimeFactory));
            m_AcceptAbility = acceptAbility ?? throw new ArgumentNullException(nameof(acceptAbility));
            if (!actorId.IsValid || !tick.IsValid)
                throw new ArgumentException("Float32 Ability invocation identity is incomplete.");
            if (input == null)
                throw new ArgumentNullException(nameof(input));
            m_SkillState = skillState ?? throw new ArgumentNullException(nameof(skillState));
            m_Workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
            m_Workspace.Reset();
            m_Frame = new Float32AbilityExecutionFrame(
                execution.Data,
                execution.Layout,
                execution.Services,
                actorId,
                tick,
                input,
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

            Float32AbilityExecutionAssembly assembly = serviceFactory.Create(
                execution.Data,
                execution.Services,
                actionBindings,
                domainRuntimeFactory,
                equipmentLayout,
                m_Frame,
                m_Workspace);
            m_Input = assembly.Input;
            m_Actions = assembly.ActionRuntime;
            m_GameplayEffects = assembly.GameplayEffects;
            m_Equipment = assembly.Equipment;
            m_Blackboard = assembly.Blackboard;
            m_Motion = assembly.Motion;
            m_Control = assembly.Control;
            m_Domain = assembly.Domain;
        }

        public CharacterSkillId AbilityId { get; }
        public IFloat32AbilityActionControlPort Actions => m_Actions;
        public bool HasGameplayEffects => m_GameplayEffects != null;
        public IEquipmentActionContextReader Equipment => m_Equipment;

        public bool HasActionWindowProjection(string windowType)
        {
            IReadOnlyList<SimulationActionWindowProjectionCandidate> projections = m_Workspace.ActionWindowProjections;
            for (int i = 0; i < projections.Count; i++)
                if (string.Equals(projections[i].WindowType, windowType, StringComparison.Ordinal))
                    return true;
            return false;
        }

        public void BeginEvaluation(
            bool diagnosticsEnabled,
            bool captureValues,
            bool captureControlFlow)
        {
            RequireOpen();
            if (m_Begun)
                throw new InvalidOperationException("Float32 Ability invocation evaluation is already active.");
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
                "Float32 Ability invocation has no declared Gameplay Effect service.")).ApplyIngress(ingress);
        }

        public void AdvanceGameplayEffects()
        {
            RequireEvaluation();
            (m_GameplayEffects ?? throw new InvalidOperationException(
                "Float32 Ability invocation has no declared Gameplay Effect service.")).Advance();
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

        public Float32AbilityInvocationResult Complete()
        {
            RequireEvaluation();
            if (m_Completed)
                throw new InvalidOperationException("Float32 Ability invocation has already completed.");
            ResolvedGameplayMotion motion = m_Motion.Resolve();
            m_Control.EndEvaluation();
            var result = new Float32AbilityInvocationResult(
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
                throw new InvalidOperationException("Float32 Ability invocation cannot accept its current candidate.");
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
                throw new InvalidOperationException("Float32 Ability invocation is not in its evaluation phase.");
        }

        void RequireOpen()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(Float32AbilityInvocationRuntime));
        }
    }
}
