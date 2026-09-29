using System;
using System.Collections.Generic;

namespace ThirdPersonSimulation
{
    internal interface IFloat32AbilityActionControlPort
    {
        bool ActivateFromControl(CharacterControlAbilityRequest request);
        void StopFromControl(CharacterControlAbilityStopRequest request);
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
            Float32AbilityOperationControlRuntime control,
            Float32AbilityExecutionWorkspace workspace);
    }

    public interface IFloat32AbilityActionBindingProvider
    {
        GameplayAbilityExecutionBinding RequireActionBinding(CharacterSkillId abilityId);
        internal ActionAdmissionProfile RequireAdmissionProfile(CharacterSkillId abilityId, string actionId);
    }

    internal readonly struct Float32AbilityExecutionAssembly
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
            Input = input;
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

    internal sealed class Float32AbilityInvocationRuntime
    {
        IFloat32SkillExecutionState m_SkillState;
        readonly Float32AbilityExecutionWorkspace m_Workspace;
        Float32AbilityExecutionFrame m_Frame;
        Float32InputRuntime m_Input;
        Float32ActionRuntime m_Actions;
        Float32GameplayEffectOperationRuntime m_GameplayEffects;
        IEquipmentActionContextReader m_Equipment;
        Float32BlackboardRuntime m_Blackboard;
        Float32MotionAccumulator m_Motion;
        readonly Float32AbilityOperationControlRuntime m_Control;
        Float32AbilityDomainRuntime m_Domain;
        readonly Float32AbilityExecutionContext m_Execution;
        readonly IFloat32AbilityActionBindingProvider m_ActionBindings;
        readonly IFloat32AbilityDomainRuntimeFactory m_DomainRuntimeFactory;
        readonly EquipmentProgramLayout m_EquipmentLayout;
        readonly IFloat32AbilityExecutionServiceFactory m_ServiceFactory;
        bool m_Begun;
        bool m_Beginning;
        bool m_Completed;
        bool m_Accepted;
        bool m_AssemblyBuilt;

        public Float32AbilityInvocationRuntime(
            Float32AbilityExecutionContext execution,
            IFloat32AbilityActionBindingProvider actionBindings,
            IFloat32AbilityDomainRuntimeFactory domainRuntimeFactory,
            EquipmentProgramLayout equipmentLayout,
            Float32AbilityExecutionWorkspace workspace,
            Float32AbilityOperationControlRuntime control,
            IFloat32AbilityExecutionServiceFactory serviceFactory)
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

        public void Begin(in Float32AbilityInvocationContext context)
        {
            if (m_Begun)
            {
                if (!m_Completed)
                    throw new InvalidOperationException(
                        $"Float32 Ability invocation evaluation is already active for '{AbilityId}' " +
                        $"at '{m_Frame.Tick.Value}' while beginning '{context.Tick.Value}'.");
                m_Begun = false;
                m_Completed = false;
                m_Accepted = false;
            }

            m_SkillState = context.SkillState ?? throw new ArgumentNullException(nameof(context.SkillState));
            m_Beginning = true;
            if (m_Frame == null)
            {
                m_Frame = new Float32AbilityExecutionFrame(
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
                Float32AbilityExecutionAssembly assembly = m_ServiceFactory.Create(
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

            m_Beginning = false;
            m_Completed = false;
            m_Accepted = false;
        }

        public IReadOnlyList<AbilityTimelineLogicMotionWarp> TimelineMotionWarps => m_Workspace.TimelineMotionWarps;

        public void CopyMotionContributionsTo(Float32MotionContributionScratch contributions)
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
            m_Workspace.TimelineMotionWarps.Add(warp);
        }

        public void ApplyTimelineMotionWarps(ref ResolvedMotionChannel action)
        {
            RequireEvaluation();
            m_Motion.ApplyTimelineMotionWarps(ref action);
        }


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
            if (m_Begun)
                throw new InvalidOperationException(
                    $"Float32 Ability invocation evaluation is already active for '{AbilityId}' " +
                    $"at tick '{m_Frame.Tick.Value}'.");
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

        public void Complete(
            List<GameplayFact> gameplayFacts,
            List<PresentationCommand> presentationCommands,
            List<SimulationTraceRecord> traceRecords)
        {
            RequireEvaluation();
            if (m_Completed)
                throw new InvalidOperationException("Float32 Ability invocation has already completed.");
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

        public void Accept(Action<IFloat32SkillExecutionState> acceptAbility)
        {
            if (!m_Completed || m_Accepted)
                throw new InvalidOperationException("Float32 Ability invocation cannot accept its current candidate.");
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
                throw new InvalidOperationException("Float32 Ability invocation is not in its evaluation phase.");
        }
    }
}
