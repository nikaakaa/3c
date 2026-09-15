using System;
using System.Collections.Generic;
using ThirdPersonPerformance.Instrumentation;
using ThirdPersonSimulation;

namespace ThirdPersonSimulation.Fixed
{
    internal sealed class FixedAbilityDomainRuntime
    {
        readonly FixedGameplayAbilityExecutionInstallation m_Installation;
        readonly FixedActionRuntime m_Actions;
        readonly FixedActionStateStore m_ActionStore;
        readonly IFixedAbilityOperationControlRuntime m_Control;

        public FixedAbilityDomainRuntime(
            FixedGameplayAbilityExecutionInstallation installation,
            FixedActionRuntime actions,
            FixedActionStateStore actionStore,
            IFixedAbilityOperationControlRuntime control)
        {
            m_Installation = installation ?? throw new ArgumentNullException(nameof(installation));
            m_Actions = actions ?? throw new ArgumentNullException(nameof(actions));
            m_ActionStore = actionStore ?? throw new ArgumentNullException(nameof(actionStore));
            m_Control = control ?? throw new ArgumentNullException(nameof(control));
        }

        [PerformanceProbe("simulation.operation.ability-tick")]
        public void Tick()
        {
            GameplayAbilityExecutionBinding skill = m_Installation.Data.Binding;
            var stoppingInstances = new HashSet<ulong>();
            IReadOnlyList<FixedActionInstanceState> actions = m_ActionStore.CurrentActions(skill.SkillId);
            for (int i = 0; i < actions.Count; i++)
                ProcessExisting(actions[i], skill, stoppingInstances);

            m_Actions.TryCommitPendingControl(skill.SkillId);
            actions = m_ActionStore.CurrentActions(skill.SkillId);
            for (int i = 0; i < actions.Count; i++)
                TickActive(actions[i], skill, stoppingInstances);
        }

        void ProcessExisting(
            FixedActionInstanceState action,
            GameplayAbilityExecutionBinding skill,
            HashSet<ulong> stoppingInstances)
        {
            if (!action.IsValid)
                return;
            if (m_Actions.StopIfEquipmentContextStale(action))
            {
                using (m_ActionStore.EnterSkillExecution(action))
                {
                    if (m_Control.IsActive(skill.EntryOperation))
                        m_Control.ForceStop(
                            skill.EntryOperation,
                            OperationStopContext.ActionContextEnded(skill.EntryOperation));
                }
                m_Actions.ClearTerminalResources(action.InstanceId);
                m_ActionStore.RemoveSkillExecution(action.InstanceId);
                return;
            }

            bool removeFrame = false;
            using (m_ActionStore.EnterSkillExecution(action))
            {
                if (m_Control.IsStopping(skill.EntryOperation))
                {
                    if (action.State == SimulationActionState.Aborted || action.State == SimulationActionState.Rejected)
                    {
                        m_Control.ForceStop(
                            skill.EntryOperation,
                            OperationStopContext.ActionContextEnded(skill.EntryOperation));
                        removeFrame = true;
                    }
                    else
                    {
                        OperationStopStatus stop = m_Control.ContinueStop(skill.EntryOperation);
                        if (stop == OperationStopStatus.Failed)
                            throw new InvalidOperationException($"Skill '{skill.SkillId}' EntryOperation stop failed.");
                        if (stop == OperationStopStatus.Running)
                            stoppingInstances.Add(action.InstanceId);
                        else if (!m_Control.IsActive(skill.EntryOperation))
                            removeFrame = true;
                    }
                }
                else if (!action.IsActive)
                {
                    if (!m_Control.IsActive(skill.EntryOperation))
                    {
                        removeFrame = true;
                    }
                    else if (action.State == SimulationActionState.Aborted || action.State == SimulationActionState.Rejected)
                    {
                        m_Control.ForceStop(
                            skill.EntryOperation,
                            OperationStopContext.ActionContextEnded(skill.EntryOperation));
                        removeFrame = true;
                    }
                    else
                    {
                        OperationStopStatus stop = m_Control.RequestStop(
                            skill.EntryOperation,
                            OperationStopContext.ActionContextEnded(skill.EntryOperation));
                        if (stop == OperationStopStatus.Failed)
                            throw new InvalidOperationException($"Skill '{skill.SkillId}' EntryOperation stop failed.");
                        if (stop == OperationStopStatus.Running)
                            stoppingInstances.Add(action.InstanceId);
                        else
                            removeFrame = true;
                    }
                }
            }
            if (!removeFrame)
                return;
            m_Actions.ClearTerminalResources(action.InstanceId);
            m_ActionStore.RemoveSkillExecution(action.InstanceId);
        }

        void TickActive(
            FixedActionInstanceState action,
            GameplayAbilityExecutionBinding skill,
            HashSet<ulong> stoppingInstances)
        {
            if (!action.IsActive || stoppingInstances.Contains(action.InstanceId))
                return;
            using (m_ActionStore.EnterSkillExecution(action))
            {
                if (!action.SkillEntryOperation.Equals(skill.EntryOperation))
                    throw new InvalidOperationException($"Skill '{skill.SkillId}' Action instance is bound to a different EntryOperation.");
                OperationRunnableStatus status = m_Control.ReadStatus(skill.EntryOperation);
                if (status == OperationRunnableStatus.Success || status == OperationRunnableStatus.Failure)
                {
                    if (action.SkillExecutionGeneration == 0)
                    {
                        OperationStopStatus reset = m_Control.RequestStop(
                            skill.EntryOperation,
                            OperationStopContext.ActionContextEnded(skill.EntryOperation));
                        if (reset != OperationStopStatus.Completed)
                            throw new InvalidOperationException($"Skill '{skill.SkillId}' previous EntryOperation state could not reset.");
                        status = m_Control.ReadStatus(skill.EntryOperation);
                    }
                    else
                    {
                        Resolve(action, skill, status == OperationRunnableStatus.Success);
                        return;
                    }
                }
                if (action.SkillExecutionGeneration != 0 && status == OperationRunnableStatus.Dormant)
                    throw new InvalidOperationException($"Skill '{skill.SkillId}' Action instance lost its EntryOperation state.");
                if (action.SkillExecutionGeneration != 0 &&
                    m_Control.ReadGeneration(skill.EntryOperation) != action.SkillExecutionGeneration)
                    throw new InvalidOperationException($"Skill '{skill.SkillId}' Action instance generation does not match its EntryOperation.");
                using (m_ActionStore.PushSkillExecution(action))
                {
                    OperationExecutionResult result = m_Control.Tick(skill.EntryOperation);
                    FixedActionInstanceState current = m_ActionStore.RequireActiveTransient(
                        FixedActionInstanceReference.FromInstance(action));
                    if (!current.IsActive)
                        return;
                    current = m_ActionStore.BindSkillExecution(
                        current,
                        skill.EntryOperation,
                        m_Control.ReadGeneration(skill.EntryOperation));
                    if (result == OperationExecutionResult.Success || result == OperationExecutionResult.Failure)
                        Resolve(current, skill, result == OperationExecutionResult.Success);
                }
            }
        }

        void Resolve(FixedActionInstanceState action, GameplayAbilityExecutionBinding skill, bool success)
        {
            m_Actions.ResolveFromControl(
                action,
                SimulationExecutionSource.FromSkillOperation(
                    skill.EntryOperation,
                    m_Installation.Services.SourcePath(skill.EntryOperation)),
                GameplayAbilityEndTriggerNames.ExecutionCompleted,
                success ? AbilityLifecycleTransition.Complete : AbilityLifecycleTransition.Abort,
                success ? "SkillCompleted" : "SkillExecutionFailed");
        }
    }
}
