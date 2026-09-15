using System;
using System.Collections.Generic;

namespace ThirdPersonSimulation
{
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
        Float32GameplayEffectOperationRuntime CreateGameplayEffects(
            Float32GameplayAbilityExecutionInstallation installation,
            Float32GameplayAbilityExecutionAccess access,
            Float32AbilityExecutionFrame frame,
            Float32ActionStateStore actions,
            Float32HandleAllocator handles,
            Float32FactSink facts,
            Float32PresentationSink presentation,
            Float32TraceSink trace,
            Float32AbilityExecutionWorkspace workspace);

        Float32EquipmentRuntime CreateEquipment(
            Float32GameplayAbilityExecutionInstallation installation,
            Float32GameplayAbilityExecutionAccess access,
            Float32AbilityExecutionFrame frame,
            Float32ActionStateStore actions,
            Float32HandleAllocator handles,
            Float32GameplayEffectOperationRuntime gameplayEffects);

        Float32LocomotionRuntime CreateLocomotion(
            Float32GameplayAbilityExecutionInstallation installation,
            Float32GameplayAbilityExecutionAccess access,
            Float32ValueRuntime values,
            Float32MotionAccumulator motion,
            Float32AbilityExecutionFrame frame);
    }

    internal sealed class Float32AbilityInvocationRuntime : IDisposable
    {
        readonly IFloat32AbilityDomainStatePort m_DomainState;
        readonly Action<IFloat32AbilityExecutionStateTransaction> m_AcceptAbility;
        readonly IFloat32AbilityExecutionStateTransaction m_AbilityState;
        readonly Float32AbilityExecutionWorkspace m_Workspace;
        readonly Float32AbilityExecutionFrame m_Frame;
        readonly Float32InputRuntime m_Input;
        readonly Float32ActionStateStore m_ActionStore;
        readonly Float32ActionRuntime m_Actions;
        readonly Float32GameplayEffectOperationRuntime m_GameplayEffects;
        readonly Float32EquipmentRuntime m_Equipment;
        readonly Float32ValueRuntime m_Values;
        readonly Float32BlackboardRuntime m_Blackboard;
        readonly Float32MotionAccumulator m_Motion;
        readonly Float32LocomotionRuntime m_Locomotion;
        readonly Float32AbilityControlRuntime m_Control;
        readonly Float32AbilityDomainRuntime m_Domain;
        bool m_Begun;
        bool m_Completed;
        bool m_Accepted;
        bool m_Disposed;

        public Float32AbilityInvocationRuntime(
            Float32GameplayAbilityExecutionInstallation installation,
            Float32GameplayAbilityExecutionInstallationSet installations,
            IFloat32AbilityExecutionStateTransaction abilityState,
            IFloat32AbilityDomainStatePort domainState,
            ActorId actorId,
            SimulationTick tick,
            Float32AbilityExecutionInput input,
            IReadOnlyList<SimulationIngress> ingress,
            Float32AbilityBodyFacts bodyFacts,
            Float32AbilityExecutionWorkspace workspace,
            IFloat32InputRequestStatePort inputRequests,
            IFloat32ActionRuntimeStatePort actionState,
            IFloat32HandleAllocatorStatePort handleAllocatorState,
            IFloat32EventSequenceStatePort eventSequenceState,
            IFloat32GameplayEffectStatePort gameplayEffectState,
            IFloat32AbilityExecutionServiceFactory serviceFactory,
            Action<IFloat32AbilityExecutionStateTransaction> acceptAbility)
        {
            Installation = installation ?? throw new ArgumentNullException(nameof(installation));
            installations = installations ?? throw new ArgumentNullException(nameof(installations));
            m_DomainState = domainState ?? throw new ArgumentNullException(nameof(domainState));
            m_AcceptAbility = acceptAbility ?? throw new ArgumentNullException(nameof(acceptAbility));
            if (!actorId.IsValid || !tick.IsValid)
                throw new ArgumentException("Float32 Ability invocation identity is incomplete.");
            if (input == null)
                throw new ArgumentNullException(nameof(input));
            m_AbilityState = abilityState ?? throw new ArgumentNullException(nameof(abilityState));
            m_Workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
            m_Workspace.Reset();
            m_Frame = new Float32AbilityExecutionFrame(
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
                handleAllocatorState,
                eventSequenceState,
                gameplayEffectState,
                m_Workspace);

            Float32GameplayAbilityExecutionAccess access = installation.Access;
            Float32StatePort controlState = m_Frame.CreateStatePort(
                "Control",
                installation.Services.ControlPolicy);
            m_ActionStore = new Float32ActionStateStore(access, m_Frame);
            m_Input = new Float32InputRuntime(access, m_Frame);
            var handles = new Float32HandleAllocator(access, m_Frame);
            m_Blackboard = new Float32BlackboardRuntime(
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

            Float32AbilityControlRuntime control = null;
            m_Actions = new Float32ActionRuntime(
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
            m_Values = new Float32ValueRuntime(
                access,
                m_Input,
                m_ActionStore,
                m_Actions,
                m_GameplayEffects,
                m_Equipment,
                m_Blackboard,
                m_Frame,
                m_Workspace);
            m_Motion = new Float32MotionAccumulator(
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
            var target = new Float32AbilityExecutionTarget(
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
            var services = new Float32AbilityExecutionServiceSet(
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
            control = new Float32AbilityControlRuntime(installation, services);
            m_Control = control;
            m_Domain = new Float32AbilityDomainRuntime(
                installation,
                m_Actions,
                m_ActionStore,
                m_Control);
        }

        public Float32GameplayAbilityExecutionInstallation Installation { get; }
        public Float32AbilityExecutionFrame Frame => m_Frame;
        public Float32AbilityExecutionWorkspace Workspace => m_Workspace;
        public Float32InputRuntime Input => m_Input;
        public Float32ActionRuntime Actions => m_Actions;
        public Float32GameplayEffectOperationRuntime GameplayEffects => m_GameplayEffects;
        public Float32EquipmentRuntime Equipment => m_Equipment;
        public Float32BlackboardRuntime Blackboard => m_Blackboard;
        public Float32MotionAccumulator Motion => m_Motion;
        public Float32LocomotionRuntime Locomotion => m_Locomotion;
        public Float32AbilityControlRuntime Control => m_Control;

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
                throw new InvalidOperationException("Float32 Ability invocation is not in its evaluation phase.");
        }

        void RequireOpen()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(Float32AbilityInvocationRuntime));
        }
    }
}
