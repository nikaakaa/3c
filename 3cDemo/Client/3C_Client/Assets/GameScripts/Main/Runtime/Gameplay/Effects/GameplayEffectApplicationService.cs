using System;
using System.Collections.Generic;

namespace ThirdPersonGameplay.Effects
{
    internal sealed class GameplayEffectApplicationService
    {
        readonly GameplayEffectRuntimeState m_State;
        readonly GameplayEffectSpecFactory m_SpecFactory;
        readonly GameplayEffectComponentExecutor m_Components;
        readonly GameplayEffectChangeRecorder m_Changes;
        readonly GameplayEffectPredictionJournalService m_Prediction;
        readonly GameplayEffectMutationTransaction m_Transaction;

        public GameplayEffectApplicationService(
            GameplayEffectRuntimeState state,
            GameplayEffectSpecFactory specFactory,
            GameplayEffectComponentExecutor components,
            GameplayEffectChangeRecorder changes,
            GameplayEffectPredictionJournalService prediction,
            GameplayEffectMutationTransaction transaction)
        {
            m_State = state ?? throw new ArgumentNullException(nameof(state));
            m_SpecFactory = specFactory ?? throw new ArgumentNullException(nameof(specFactory));
            m_Components = components ?? throw new ArgumentNullException(nameof(components));
            m_Changes = changes ?? throw new ArgumentNullException(nameof(changes));
            m_Prediction = prediction ?? throw new ArgumentNullException(nameof(prediction));
            m_Transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
        }

        public GameplayEffectCanApplyResult CanApply(GameplayEffectApplyRequest request)
        {
            if (!m_SpecFactory.TryBuild(request, out GameplayEffectSpec spec, out GameplayEffectApplyResultCode code, out string reason))
                return new GameplayEffectCanApplyResult(false, code, reason);
            if (!m_Components.CanApply(spec, out reason))
                return new GameplayEffectCanApplyResult(false, GameplayEffectApplyResultCode.RequirementFailed, reason);
            if (spec.Definition.StackingPolicy != GameplayEffectStackingPolicy.Independent &&
                m_State.ActiveEffects.TryGetStack(spec.StackKey, out ActiveGameplayEffect active) &&
                active.StackCount >= spec.Definition.MaxStacks &&
                spec.Definition.OverflowPolicy == GameplayEffectOverflowPolicy.Reject)
                return new GameplayEffectCanApplyResult(false, GameplayEffectApplyResultCode.OverflowRejected, "MaxStacksReached");
            return new GameplayEffectCanApplyResult(true, GameplayEffectApplyResultCode.Applied, string.Empty);
        }

        public bool BeginMutationTransaction() => m_Transaction.Begin();

        public bool CommitPendingMutations(
            out GameplayEffectApplyResult failure,
            out GameplayEffectPendingApplication failedApplication)
        {
            return TryFlushPendingCore(out failure, out failedApplication);
        }

        public void EndMutationTransaction(bool commit) => m_Transaction.End(commit);

        public GameplayEffectApplyResult Apply(GameplayEffectApplyRequest request)
        {
            GameplayEffectCanApplyResult canApply = CanApply(request);
            if (!canApply.Allowed)
                return ApplyCore(request);
            bool root = m_Transaction.Begin();
            bool commit = false;
            try
            {
                GameplayEffectApplyResult result = ApplyCore(request);
                if (!IsAcceptedMutation(result))
                    return result;
                if (root && !TryFlushPendingCore(out GameplayEffectApplyResult failure, out _))
                {
                    return new GameplayEffectApplyResult(
                        failure.Code,
                        default,
                        default,
                        $"AdditionalEffect:{failure.Reason}");
                }
                m_Changes.DrainStateChanges();
                commit = true;
                return result;
            }
            finally
            {
                m_Transaction.End(commit);
            }
        }

        public GameplayEffectRemoveResult Remove(GameplayEffectRemoveRequest request)
        {
            m_Transaction.Begin();
            bool commit = false;
            List<ActiveGameplayEffect> values = m_State.ActiveEffects.Snapshot();
            var removed = new List<GameplayEffectHandle>();
            try
            {
                for (int i = 0; i < values.Count; i++)
                {
                    ActiveGameplayEffect value = values[i];
                    if (!MatchesRemoveRequest(value, request))
                        continue;
                    GameplayEffectHandle handle = value.Handle;
                    RemoveActive(value, GameplayEffectLifecycleOperation.Removed, true);
                    removed.Add(handle);
                    if (request.Selector == GameplayEffectRemoveSelector.Handle)
                        break;
                }
                if (!TryFlushPendingCore(out _, out _))
                    return new GameplayEffectRemoveResult(Array.Empty<GameplayEffectHandle>());
                m_Changes.DrainStateChanges();
                commit = true;
                return new GameplayEffectRemoveResult(removed);
            }
            finally
            {
                m_Transaction.End(commit);
            }
        }

        public void ExecutePeriod(ActiveGameplayEffect active, ulong authoritativeRevision = 0)
        {
            GameplayEffectComponentContext context = m_Components.CreateContext(
                active,
                GameplayEffectExecutionTrigger.Period,
                GameplayEffectLifecycleOperation.PeriodExecuted);
            m_Components.InvokeExecute(active.Spec, context);
            active.LifecycleRevision = authoritativeRevision > 0
                ? authoritativeRevision
                : active.LifecycleRevision + 1;
            m_Changes.AddLifecycle(active, GameplayEffectLifecycleOperation.PeriodExecuted);
        }

        public void RemoveActive(
            ActiveGameplayEffect active,
            GameplayEffectLifecycleOperation operation,
            bool emitBusinessOutputs)
        {
            if (active == null || active.PendingRemoval)
                return;
            m_Components.DeactivatePersistent(active);
            if (emitBusinessOutputs)
            {
                m_Components.InvokeRemoved(active, operation);
                active.LifecycleRevision++;
                m_Changes.AddLifecycle(active, operation);
            }
            m_State.ActiveEffects.Remove(active);
        }

        public void RestoreActive(ActiveGameplayEffect active, GameplayActiveEffectSnapshot snapshot)
        {
            if (active.InstanceId != snapshot.InstanceId &&
                !m_State.ActiveEffects.UpdateInstanceId(active, snapshot.InstanceId))
                throw new InvalidOperationException($"Unable to restore Gameplay Effect instance id '{snapshot.InstanceId}'.");
            active.StartTick = snapshot.StartTick;
            active.EndTick = snapshot.EndTick;
            active.NextPeriodTick = snapshot.NextPeriodTick;
            active.StackCount = snapshot.StackCount;
            active.Inhibited = snapshot.Inhibited;
            active.LifecycleRevision = snapshot.LifecycleRevision;
            if (!active.Inhibited)
                m_Components.ActivatePersistent(active);
        }

        GameplayEffectApplyResult ApplyCore(GameplayEffectApplyRequest request)
        {
            if (!m_SpecFactory.TryBuild(request, out GameplayEffectSpec spec, out GameplayEffectApplyResultCode code, out string reason))
                return RejectApply(request, code, reason);
            if (!m_Components.CanApply(spec, out reason))
                return RejectApply(request, GameplayEffectApplyResultCode.RequirementFailed, reason);
            if (spec.Definition.DurationPolicy == GameplayEffectDurationPolicy.Instant)
                return ApplyInstant(spec);
            if (spec.Definition.StackingPolicy != GameplayEffectStackingPolicy.Independent &&
                m_State.ActiveEffects.TryGetStack(spec.StackKey, out ActiveGameplayEffect active))
                return ApplyStack(spec, active);
            return ApplyNewActive(spec);
        }

        GameplayEffectApplyResult ApplyInstant(GameplayEffectSpec spec)
        {
            GameplayEffectHandle handle = m_State.NextHandle();
            GameplayEffectInstanceId instanceId = m_State.ResolveInstanceId(spec.AuthoritativeInstanceId);
            ulong revision = spec.AuthoritativeLifecycleRevision > 0 ? spec.AuthoritativeLifecycleRevision : 1;
            m_Changes.RegisterCause(handle, spec, instanceId);
            m_Prediction.Begin(spec, handle, instanceId, false, false, default);
            try
            {
                GameplayEffectComponentContext context = m_Components.CreateContext(
                    spec,
                    null,
                    handle,
                    instanceId,
                    1,
                    GameplayEffectExecutionTrigger.Instant,
                    GameplayEffectLifecycleOperation.Applied);
                m_Components.InvokeApplied(spec, context);
                m_Components.InvokeExecute(spec, context);
                m_Changes.AddLifecycle(
                    spec,
                    instanceId,
                    GameplayEffectLifecycleOperation.Applied,
                    m_State.CurrentTick,
                    m_State.CurrentTick,
                    1,
                    revision,
                    true);
                m_Prediction.Complete();
                return new GameplayEffectApplyResult(GameplayEffectApplyResultCode.Applied, handle, instanceId, string.Empty);
            }
            catch
            {
                m_Prediction.CancelCurrent();
                throw;
            }
        }

        GameplayEffectApplyResult ApplyNewActive(GameplayEffectSpec spec)
        {
            GameplayEffectHandle handle = m_State.NextHandle();
            GameplayEffectInstanceId instanceId = m_State.ResolveInstanceId(spec.AuthoritativeInstanceId);
            ulong endTick = spec.Definition.DurationPolicy == GameplayEffectDurationPolicy.Duration
                ? GameplayEffectRuntimeState.CheckedAdd(m_State.CurrentTick, spec.DurationTicks)
                : 0;
            ulong revision = spec.AuthoritativeLifecycleRevision > 0 ? spec.AuthoritativeLifecycleRevision : 1;
            var active = new ActiveGameplayEffect(
                handle,
                instanceId,
                spec,
                m_State.CurrentTick,
                endTick,
                spec.FirstPeriodTick,
                m_State.NextInsertionSequence(),
                revision);
            m_State.ActiveEffects.Add(active);
            m_Changes.RegisterCause(active);
            m_Prediction.Begin(spec, handle, instanceId, true, false, default);
            try
            {
                m_Components.ActivatePersistent(active);
                GameplayEffectComponentContext context = m_Components.CreateContext(
                    active,
                    GameplayEffectExecutionTrigger.Period,
                    GameplayEffectLifecycleOperation.Applied);
                m_Components.InvokeApplied(spec, context);
                m_Changes.AddLifecycle(active, GameplayEffectLifecycleOperation.Applied);
                if (spec.Definition.ExecuteOnApplication)
                    ExecutePeriod(active);
                m_Prediction.Complete();
                return new GameplayEffectApplyResult(GameplayEffectApplyResultCode.Applied, handle, instanceId, string.Empty);
            }
            catch
            {
                m_Components.DeactivatePersistent(active);
                m_State.ActiveEffects.Remove(active);
                m_Prediction.CancelCurrent();
                throw;
            }
        }

        GameplayEffectApplyResult ApplyStack(GameplayEffectSpec spec, ActiveGameplayEffect active)
        {
            m_Changes.RegisterCause(active);
            if (active.StackCount >= spec.Definition.MaxStacks)
                return ApplyOverflow(spec, active);
            GameplayActiveEffectSnapshot before = active.Snapshot();
            m_Prediction.Begin(spec, active.Handle, active.InstanceId, false, true, before);
            try
            {
                m_Components.DeactivatePersistent(active);
                active.StackCount++;
                active.LifecycleRevision++;
                UpdateStackTime(active, spec);
                m_Components.ActivatePersistent(active);
                GameplayEffectComponentContext context = m_Components.CreateContext(
                    active,
                    GameplayEffectExecutionTrigger.Period,
                    GameplayEffectLifecycleOperation.StackChanged);
                m_Components.InvokeApplied(spec, context);
                m_Changes.AddLifecycle(active, GameplayEffectLifecycleOperation.StackChanged);
                if (spec.Definition.ExecuteOnApplication)
                    ExecutePeriod(active);
                m_Prediction.Complete();
                return new GameplayEffectApplyResult(GameplayEffectApplyResultCode.Applied, active.Handle, active.InstanceId, string.Empty);
            }
            catch
            {
                m_Components.DeactivatePersistent(active);
                RestoreActive(active, before);
                m_Prediction.CancelCurrent();
                throw;
            }
        }

        GameplayEffectApplyResult ApplyOverflow(GameplayEffectSpec spec, ActiveGameplayEffect active)
        {
            active.LifecycleRevision++;
            m_Changes.AddLifecycle(active, GameplayEffectLifecycleOperation.Overflow);
            switch (spec.Definition.OverflowPolicy)
            {
                case GameplayEffectOverflowPolicy.ReplaceOldest:
                {
                    RemoveActive(active, GameplayEffectLifecycleOperation.Removed, true);
                    return ApplyNewActive(spec);
                }
                case GameplayEffectOverflowPolicy.ApplyOverflowEffects:
                    m_Components.InvokeOverflow(spec, active);
                    return new GameplayEffectApplyResult(
                        GameplayEffectApplyResultCode.OverflowRejected,
                        active.Handle,
                        active.InstanceId,
                        "OverflowEffectsApplied");
                default:
                    return new GameplayEffectApplyResult(
                        GameplayEffectApplyResultCode.OverflowRejected,
                        active.Handle,
                        active.InstanceId,
                        "MaxStacksReached");
            }
        }

        GameplayEffectApplyResult RejectApply(
            GameplayEffectApplyRequest request,
            GameplayEffectApplyResultCode code,
            string reason)
        {
            GameplayEffectInstanceId instanceId = request?.AuthoritativeInstanceId ?? default;
            GameplayEffectSpec rejectedSpec = m_SpecFactory.CreateRejectedSpec(request);
            if (rejectedSpec != null)
            {
                m_Changes.AddLifecycle(
                    rejectedSpec,
                    instanceId,
                    GameplayEffectLifecycleOperation.Rejected,
                    m_State.CurrentTick,
                    m_State.CurrentTick,
                    0,
                    request?.AuthoritativeLifecycleRevision ?? 0,
                    rejectedSpec.Definition.DurationPolicy == GameplayEffectDurationPolicy.Instant);
            }
            return new GameplayEffectApplyResult(code, default, instanceId, reason);
        }

        bool TryFlushPendingCore(
            out GameplayEffectApplyResult failure,
            out GameplayEffectPendingApplication failedApplication)
        {
            while (m_Transaction.TryDequeueAdditionalEffect(out GameplayEffectPendingApplication application))
            {
                GameplayEffectApplyResult result = ApplyCore(application.Request);
                if (!IsAcceptedMutation(result))
                {
                    failure = result;
                    failedApplication = application;
                    return false;
                }
            }
            failure = default;
            failedApplication = default;
            return true;
        }

        static bool IsAcceptedMutation(GameplayEffectApplyResult result)
        {
            return result.Succeeded ||
                   result.Code == GameplayEffectApplyResultCode.OverflowRejected &&
                   string.Equals(result.Reason, "OverflowEffectsApplied", StringComparison.Ordinal);
        }

        void UpdateStackTime(ActiveGameplayEffect active, GameplayEffectSpec incoming)
        {
            if (active.Spec.Definition.DurationPolicy == GameplayEffectDurationPolicy.Duration)
            {
                switch (active.Spec.Definition.DurationUpdate)
                {
                    case GameplayEffectDurationUpdatePolicy.Refresh:
                        active.StartTick = m_State.CurrentTick;
                        active.EndTick = GameplayEffectRuntimeState.CheckedAdd(m_State.CurrentTick, incoming.DurationTicks);
                        break;
                    case GameplayEffectDurationUpdatePolicy.Extend:
                        active.EndTick = GameplayEffectRuntimeState.CheckedAdd(active.EndTick, incoming.DurationTicks);
                        break;
                }
            }
            if (active.Spec.Definition.PeriodUpdate == GameplayEffectPeriodUpdatePolicy.Reset && incoming.PeriodTicks > 0)
                active.NextPeriodTick = GameplayEffectRuntimeState.CheckedAdd(m_State.CurrentTick, incoming.PeriodTicks);
        }

        bool MatchesRemoveRequest(ActiveGameplayEffect active, GameplayEffectRemoveRequest request)
        {
            switch (request.Selector)
            {
                case GameplayEffectRemoveSelector.Handle:
                    return active.Handle == request.Handle;
                case GameplayEffectRemoveSelector.EffectId:
                    return active.Spec.EffectId == request.EffectId;
                case GameplayEffectRemoveSelector.SourceActorId:
                    return string.Equals(active.Spec.Context.SourceActorId, request.SourceActorId, StringComparison.Ordinal);
                case GameplayEffectRemoveSelector.EffectTagQuery:
                    return m_State.Tags.Matches(request.EffectTagQuery, active.Spec.Definition.EffectTags);
                default:
                    return false;
            }
        }
    }
}
