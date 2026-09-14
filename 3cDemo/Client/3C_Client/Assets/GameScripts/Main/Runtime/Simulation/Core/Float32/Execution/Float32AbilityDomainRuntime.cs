using System;
using System.Collections.Generic;
using ThirdPersonPerformance.Instrumentation;

namespace ThirdPersonSimulation
{
    internal sealed class Float32AbilityDomainRuntime
    {
        readonly Float32GameplayAbilityExecutionInstallationSet m_Installations;
        readonly Float32ActionRuntime m_Actions;
        readonly Float32ActionStateStore m_ActionStore;
        readonly OperationControlRuntime<Float32OperationTarget> m_Control;

        public Float32AbilityDomainRuntime(
            Float32GameplayAbilityExecutionInstallationSet installations,
            Float32ActionRuntime actions,
            Float32ActionStateStore actionStore,
            OperationControlRuntime<Float32OperationTarget> control)
        {
            m_Installations = installations ?? throw new ArgumentNullException(nameof(installations));
            m_Actions = actions ?? throw new ArgumentNullException(nameof(actions));
            m_ActionStore = actionStore ?? throw new ArgumentNullException(nameof(actionStore));
            m_Control = control ?? throw new ArgumentNullException(nameof(control));
        }

        [PerformanceProbe("simulation.operation.ability-tick")]
        public void Tick()
        {
            IReadOnlyList<Float32GameplayAbilityExecutionInstallation> installations = m_Installations.Installations;
            var stoppingInstances = new HashSet<ulong>();
            for (int i = 0; i < installations.Count; i++)
            {
                Float32GameplayAbilityExecutionInstallation installation = installations[i];
                GameplayAbilityExecutionBinding skill = installation.Data.Binding;
                IReadOnlyList<Float32ActionInstanceState> actions = m_ActionStore.CurrentActions(skill.SkillId);
                for (int actionIndex = 0; actionIndex < actions.Count; actionIndex++)
                {
                    Float32ActionInstanceState action = actions[actionIndex];
                    if (!action.IsValid)
                        continue;
                    bool removeFrame = false;
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
                        continue;
                    }
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
                            if (m_Control.IsActive(skill.EntryOperation))
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
                            else
                            {
                                removeFrame = true;
                            }
                        }
                    }
                    if (removeFrame)
                        m_Actions.ClearTerminalResources(action.InstanceId);
                    if (removeFrame)
                        m_ActionStore.RemoveSkillExecution(action.InstanceId);
                }
            }
            for (int i = 0; i < installations.Count; i++)
            {
                Float32GameplayAbilityExecutionInstallation installation = installations[i];
                GameplayAbilityExecutionBinding skill = installation.Data.Binding;
                m_Actions.TryCommitPendingControl(skill.SkillId);
                IReadOnlyList<Float32ActionInstanceState> actions = m_ActionStore.CurrentActions(skill.SkillId);
                for (int actionIndex = 0; actionIndex < actions.Count; actionIndex++)
                {
                    Float32ActionInstanceState action = actions[actionIndex];
                    if (!action.IsActive || stoppingInstances.Contains(action.InstanceId))
                        continue;
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
                                m_Actions.ResolveFromControl(
                                    action,
                                    SimulationExecutionSource.FromSkillOperation(
                                        skill.EntryOperation,
                                        installation.Services.SourcePath(skill.EntryOperation)),
                                    GameplayAbilityEndTriggerNames.ExecutionCompleted,
                                    status == OperationRunnableStatus.Success
                                        ? AbilityLifecycleTransition.Complete
                                        : AbilityLifecycleTransition.Abort,
                                    status == OperationRunnableStatus.Success ? "SkillCompleted" : "SkillExecutionFailed");
                                continue;
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
                            Float32ActionInstanceState current = m_ActionStore.RequireActiveTransient(
                                Float32ActionInstanceReference.FromInstance(action));
                            if (current.IsActive)
                            {
                                current = m_ActionStore.BindSkillExecution(
                                    current,
                                    skill.EntryOperation,
                                    m_Control.ReadGeneration(skill.EntryOperation));
                                if (result == OperationExecutionResult.Success || result == OperationExecutionResult.Failure)
                                    m_Actions.ResolveFromControl(
                                        current,
                                        SimulationExecutionSource.FromSkillOperation(
                                            skill.EntryOperation,
                                            installation.Services.SourcePath(skill.EntryOperation)),
                                        GameplayAbilityEndTriggerNames.ExecutionCompleted,
                                        result == OperationExecutionResult.Success
                                            ? AbilityLifecycleTransition.Complete
                                            : AbilityLifecycleTransition.Abort,
                                        result == OperationExecutionResult.Success ? "SkillCompleted" : "SkillExecutionFailed");
                            }
                        }
                    }
                }
            }
        }
    }
}
