using System;
using System.Collections.Generic;
using ThirdPersonSimulation;

namespace ThirdPersonSimulation.Fixed
{
    internal interface IFixedAbilityActionControlPort
    {
        bool ActivateFromControl(CharacterControlAbilityRequest request);
        void StopFromControl(CharacterControlAbilityStopRequest request);
    }

    internal interface IFixedAbilityExecutionServiceFactory
    {
        FixedAbilityExecutionAssembly Create(
            FixedGameplayAbilityExecutionData executionData,
            FixedGameplayAbilityExecutionServices executionServices,
            IFixedAbilityActionBindingProvider actionBindings,
            IFixedAbilityDomainRuntimeFactory domainRuntimeFactory,
            EquipmentProgramLayout equipmentLayout,
            FixedAbilityExecutionFrame frame,
            IFixedAbilityExecutionSavepointPort savepointPort,
            IFixedInputRequestStatePort inputRequests,
            FixedAbilityExecutionWorkspace workspace);
    }

    public interface IFixedAbilityActionBindingProvider
    {
        GameplayAbilityExecutionBinding RequireActionBinding(CharacterSkillId abilityId);
        internal ActionAdmissionProfile RequireAdmissionProfile(CharacterSkillId abilityId, string actionId);
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
            FixedAbilityOperationControlRuntime control,
            FixedAbilityDomainRuntime domain)
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

        public FixedInputRuntime Input { get; }
        public IFixedAbilityActionControlPort Actions => ActionRuntime;
        internal FixedActionRuntime ActionRuntime { get; }
        public FixedGameplayEffectOperationRuntime GameplayEffects { get; }
        public FixedEquipmentRuntime Equipment { get; }
        public FixedBlackboardRuntime Blackboard { get; }
        public FixedMotionAccumulator Motion { get; }
        public FixedAbilityOperationControlRuntime Control { get; }
        public FixedAbilityDomainRuntime Domain { get; }
    }

    internal sealed class FixedAbilityInvocationRuntime : IDisposable
    {
        readonly IFixedSkillExecutionState m_SkillState;
        readonly FixedAbilityExecutionWorkspace m_Workspace;
        readonly FixedAbilityExecutionFrame m_Frame;
        readonly FixedInputRuntime m_Input;
        readonly FixedActionRuntime m_Actions;
        readonly FixedGameplayEffectOperationRuntime m_GameplayEffects;
        readonly IEquipmentActionContextReader m_Equipment;
        readonly FixedBlackboardRuntime m_Blackboard;
        readonly FixedMotionAccumulator m_Motion;
        readonly FixedAbilityOperationControlRuntime m_Control;
        readonly FixedAbilityDomainRuntime m_Domain;
        bool m_Begun;
        bool m_Completed;
        bool m_Accepted;
        bool m_Disposed;

        public FixedAbilityInvocationRuntime(
            FixedAbilityExecutionContext execution,
            IFixedAbilityActionBindingProvider actionBindings,
            IFixedAbilityDomainRuntimeFactory domainRuntimeFactory,
            EquipmentProgramLayout equipmentLayout,
            IFixedSkillExecutionState skillState,
            IFixedAbilityExecutionSavepointPort savepointPort,
            IFixedInputRequestStatePort inputRequests,
            IFixedActionRuntimeStatePort actionState,
            IFixedHandleAllocatorStatePort handleAllocatorState,
            IFixedEventSequenceStatePort eventSequenceState,
            IFixedGameplayEffectStatePort gameplayEffectState,
            IFixedEquipmentStatePort equipmentState,
            ActorId actorId,
            SimulationTick tick,
            FixedAbilityExecutionInput input,
            FixedAbilityBodyFacts bodyFacts,
            FixedAbilityExecutionWorkspace workspace,
            IFixedAbilityExecutionServiceFactory serviceFactory)
        {
            execution = execution ?? throw new ArgumentNullException(nameof(execution));
            AbilityId = execution.Data.AbilityId;
            actionBindings = actionBindings ?? throw new ArgumentNullException(nameof(actionBindings));
            domainRuntimeFactory = domainRuntimeFactory ?? throw new ArgumentNullException(nameof(domainRuntimeFactory));
            skillState = skillState ?? throw new ArgumentNullException(nameof(skillState));
            savepointPort = savepointPort ?? throw new ArgumentNullException(nameof(savepointPort));
            inputRequests = inputRequests ?? throw new ArgumentNullException(nameof(inputRequests));
            if (!actorId.IsValid || !tick.IsValid)
                throw new ArgumentException("Fixed Ability invocation identity is incomplete.");
            if (input == null)
                throw new ArgumentNullException(nameof(input));
            m_SkillState = skillState;
            m_Workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
            m_Frame = new FixedAbilityExecutionFrame(
                execution.Data,
                execution.Layout,
                execution.Services,
                actorId,
                tick,
                input,
                bodyFacts,
                skillState,
                actionState,
                handleAllocatorState,
                eventSequenceState,
                gameplayEffectState,
                equipmentState,
                m_Workspace);

            FixedAbilityExecutionAssembly assembly = serviceFactory.Create(
                execution.Data,
                execution.Services,
                actionBindings,
                domainRuntimeFactory,
                equipmentLayout,
                m_Frame,
                savepointPort,
                inputRequests,
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
        public IReadOnlyList<AbilityTimelineLogicMotionWarp> TimelineMotionWarps => m_Workspace.TimelineMotionWarps;

        public void CopyMotionContributionsTo(FixedMotionContributionScratch contributions)
        {
            m_Workspace.CopyMotionContributionsTo(contributions);
            m_Workspace.ClearMotionContributions();
        }

        public void ClearTimelineMotionWarps()
        {
            RequireEvaluation();
            m_Workspace.TimelineMotionWarps.Clear();
        }

        public void AddTimelineMotionWarp(AbilityTimelineLogicMotionWarp warp)
        {
            RequireEvaluation();
            if (warp.AbilityId != AbilityId)
                throw new InvalidOperationException("Timeline MotionWarp belongs to a different Ability invocation.");
            m_Workspace.TimelineMotionWarps.Add(warp);
        }

        public void ApplyTimelineMotionWarps(ref ResolvedMotionChannel action)
        {
            RequireEvaluation();
            m_Motion.ApplyTimelineMotionWarps(ref action);
        }


        public IFixedAbilityActionControlPort Actions => m_Actions;
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

        public void Complete(
            List<GameplayFact> gameplayFacts,
            List<PresentationCommand> presentationCommands,
            List<SimulationTraceRecord> traceRecords)
        {
            RequireEvaluation();
            if (m_Completed)
                throw new InvalidOperationException("Fixed Ability invocation has already completed.");
            if (gameplayFacts == null)
                throw new ArgumentNullException(nameof(gameplayFacts));
            if (presentationCommands == null)
                throw new ArgumentNullException(nameof(presentationCommands));
            if (traceRecords == null)
                throw new ArgumentNullException(nameof(traceRecords));
            m_Control.EndEvaluation();
            gameplayFacts.AddRange(m_Workspace.Facts);
            presentationCommands.AddRange(m_Workspace.Presentation);
            traceRecords.AddRange(m_Workspace.Trace);
            m_Frame.End();
            m_Completed = true;
        }

        public void Accept(Action<IFixedSkillExecutionState> acceptAbility)
        {
            RequireOpen();
            if (!m_Completed || m_Accepted)
                throw new InvalidOperationException("Fixed Ability invocation cannot accept its current candidate.");
            acceptAbility = acceptAbility ?? throw new ArgumentNullException(nameof(acceptAbility));
            acceptAbility(m_SkillState);
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
