using System;
using System.Collections.Generic;

namespace ThirdPersonGameplay.Effects
{
    internal sealed class GameplayEffectLifecycleScheduler
    {
        readonly GameplayEffectRuntimeState m_State;
        readonly GameplayEffectApplicationService m_Application;
        readonly GameplayEffectComponentExecutor m_Components;
        readonly GameplayEffectChangeRecorder m_Changes;

        public GameplayEffectLifecycleScheduler(
            GameplayEffectRuntimeState state,
            GameplayEffectApplicationService application,
            GameplayEffectComponentExecutor components,
            GameplayEffectChangeRecorder changes)
        {
            m_State = state ?? throw new ArgumentNullException(nameof(state));
            m_Application = application ?? throw new ArgumentNullException(nameof(application));
            m_Components = components ?? throw new ArgumentNullException(nameof(components));
            m_Changes = changes ?? throw new ArgumentNullException(nameof(changes));
        }

        public bool Advance(out GameplayEffectExecutionFailure failure)
        {
            bool root = m_Application.BeginMutationTransaction();
            bool commit = false;
            GameplayEffectApplyResult applyFailure = default;
            GameplayEffectPendingApplication failedApplication = default;
            try
            {
                List<ActiveGameplayEffect> values = m_State.ActiveEffects.BeginStableIteration();
                try
                {
                    for (int i = 0; i < values.Count; i++)
                    {
                        ActiveGameplayEffect active = values[i];
                        if (active.PendingRemoval)
                            continue;
                        if (m_Components.RemovalRequirementMet(active))
                        {
                            m_Application.RemoveActive(active, GameplayEffectLifecycleOperation.Removed, true);
                            continue;
                        }

                        bool ongoing = m_Components.OngoingRequirementsMet(active);
                        if (!ongoing && !active.Inhibited)
                        {
                            m_Components.DeactivatePersistent(active);
                            active.Inhibited = true;
                            active.LifecycleRevision++;
                            m_Changes.AddLifecycle(active, GameplayEffectLifecycleOperation.Inhibited);
                        }
                        else if (ongoing && active.Inhibited)
                        {
                            active.Inhibited = false;
                            m_Components.ActivatePersistent(active);
                            active.LifecycleRevision++;
                            m_Changes.AddLifecycle(active, GameplayEffectLifecycleOperation.Resumed);
                        }

                        if (!active.Inhibited)
                        {
                            while (active.Spec.PeriodTicks > 0 &&
                                   active.NextPeriodTick <= m_State.CurrentTick &&
                                   (active.EndTick == 0 || active.NextPeriodTick < active.EndTick))
                            {
                                m_Application.ExecutePeriod(active);
                                active.NextPeriodTick = GameplayEffectRuntimeState.CheckedAdd(
                                    active.NextPeriodTick,
                                    active.Spec.PeriodTicks);
                            }
                            m_Components.InvokeWhileActive(active);
                        }

                        if (active.EndTick != 0 && m_State.CurrentTick >= active.EndTick)
                            m_Application.RemoveActive(active, GameplayEffectLifecycleOperation.Expired, true);
                    }
                }
                finally
                {
                    m_State.ActiveEffects.EndStableIteration();
                }
                commit = !root || m_Application.CommitPendingMutations(out applyFailure, out failedApplication);
            }
            finally
            {
                m_Application.EndMutationTransaction(commit);
            }
            if (commit)
            {
                failure = default;
                return true;
            }
            failure = new GameplayEffectExecutionFailure(
                failedApplication.OwnerEffectId,
                failedApplication.OwnerInstanceId,
                failedApplication.Trigger,
                failedApplication.Request?.EffectId ?? default,
                applyFailure.Code,
                applyFailure.Reason);
            return false;
        }
    }
}
