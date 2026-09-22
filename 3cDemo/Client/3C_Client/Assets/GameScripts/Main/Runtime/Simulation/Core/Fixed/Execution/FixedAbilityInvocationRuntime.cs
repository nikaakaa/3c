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
            FixedAbilityOperationControlRuntime control,
            FixedAbilityExecutionWorkspace workspace);
    }

    public interface IFixedAbilityActionBindingProvider
    {
        GameplayAbilityExecutionBinding RequireActionBinding(CharacterSkillId abilityId);
        internal ActionAdmissionProfile RequireAdmissionProfile(CharacterSkillId abilityId, string actionId);
    }

    internal readonly struct FixedAbilityExecutionAssembly
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
            Input = input;
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

    internal sealed class FixedAbilityInvocationRuntime
    {
        IFixedSkillExecutionState m_SkillState;
        readonly FixedAbilityExecutionWorkspace m_Workspace;
        FixedAbilityExecutionFrame m_Frame;
        FixedInputRuntime m_Input;
        FixedActionRuntime m_Actions;
        FixedGameplayEffectOperationRuntime m_GameplayEffects;
        IEquipmentActionContextReader m_Equipment;
        FixedBlackboardRuntime m_Blackboard;
        FixedMotionAccumulator m_Motion;
        readonly FixedAbilityOperationControlRuntime m_Control;
        FixedAbilityDomainRuntime m_Domain;
        readonly FixedAbilityExecutionContext m_Execution;
        readonly IFixedAbilityActionBindingProvider m_ActionBindings;
        readonly IFixedAbilityDomainRuntimeFactory m_DomainRuntimeFactory;
        readonly EquipmentProgramLayout m_EquipmentLayout;
        readonly IFixedAbilityExecutionServiceFactory m_ServiceFactory;
        bool m_Begun;
        bool m_Beginning;
        bool m_Completed;
        bool m_Accepted;
        bool m_AssemblyBuilt;

        public FixedAbilityInvocationRuntime(
            FixedAbilityExecutionContext execution,
            IFixedAbilityActionBindingProvider actionBindings,
            IFixedAbilityDomainRuntimeFactory domainRuntimeFactory,
            EquipmentProgramLayout equipmentLayout,
            FixedAbilityExecutionWorkspace workspace,
            FixedAbilityOperationControlRuntime control,
            IFixedAbilityExecutionServiceFactory serviceFactory)
        {
            execution = execution ?? throw new ArgumentNullException(nameof(execution));
            AbilityId = execution.Data.AbilityId;
            m_Execution = execution;
            actionBindings = actionBindings ?? throw new ArgumentNullException(nameof(actionBindings));
            domainRuntimeFactory = domainRuntimeFactory ?? throw new ArgumentNullException(nameof(domainRuntimeFactory));
            control = control ?? throw new ArgumentNullException(nameof(control));
            m_Workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
            m_ActionBindings = actionBindings;
            m_DomainRuntimeFactory = domainRuntimeFactory;
            m_EquipmentLayout = equipmentLayout;
            m_Control = control;
            m_ServiceFactory = serviceFactory ?? throw new ArgumentNullException(nameof(serviceFactory));
        }

        public CharacterSkillId AbilityId { get; }

        public void Begin(in FixedAbilityInvocationContext context)
        {
            if (m_Begun)
            {
                if (!m_Completed)
                    throw new InvalidOperationException("Fixed Ability invocation evaluation is already active.");
                m_Begun = false;
                m_Completed = false;
                m_Accepted = false;
            }

            m_SkillState = context.SkillState ?? throw new ArgumentNullException(nameof(context.SkillState));
            m_Beginning = true;
            if (m_Frame == null)
            {
                m_Frame = new FixedAbilityExecutionFrame(
                    m_Execution.Data,
                    m_Execution.Layout,
                    m_Execution.Services,
                    context.ActorId,
                    m_Execution.Trace,
                    m_Workspace);
            }

            m_Frame.Begin(context);
            if (!m_AssemblyBuilt)
            {
                FixedAbilityExecutionAssembly assembly = m_ServiceFactory.Create(
                    m_Execution.Data,
                    m_Execution.Services,
                    m_ActionBindings,
                    m_DomainRuntimeFactory,
                    m_EquipmentLayout,
                    m_Frame,
                    m_Control,
                    m_Workspace);
                m_Input = assembly.Input;
                m_Actions = assembly.ActionRuntime;
                m_GameplayEffects = assembly.GameplayEffects;
                m_Equipment = assembly.Equipment;
                m_Blackboard = assembly.Blackboard;
                m_Motion = assembly.Motion;
                m_Domain = assembly.Domain;
                m_AssemblyBuilt = true;
            }

            m_Begun = true;
            m_Beginning = false;
            m_Completed = false;
            m_Accepted = false;
        }

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
            if (!m_Completed || m_Accepted)
                throw new InvalidOperationException("Fixed Ability invocation cannot accept its current candidate.");
            acceptAbility = acceptAbility ?? throw new ArgumentNullException(nameof(acceptAbility));
            acceptAbility(m_SkillState);
            m_SkillState.Dispose();
            m_Accepted = true;
        }

        public void Abort()
        {
            if (m_Beginning)
            {
                m_Frame?.End();
                m_SkillState?.Dispose();
                m_Beginning = false;
                m_SkillState = null;
                return;
            }

            if (!m_Begun)
                return;
            if (!m_Completed)
            {
                m_Control.EndEvaluation();
                m_Frame.End();
            }
            if (!m_Accepted)
                m_SkillState.Dispose();
            m_Begun = false;
            m_Completed = false;
            m_Accepted = false;
            m_SkillState = null;
        }

        void RequireEvaluation()
        {
            if (!m_Begun || m_Completed)
                throw new InvalidOperationException("Fixed Ability invocation is not in its evaluation phase.");
        }
    }
}
