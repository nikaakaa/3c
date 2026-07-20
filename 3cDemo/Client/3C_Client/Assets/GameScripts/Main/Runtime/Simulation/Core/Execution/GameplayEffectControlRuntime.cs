using System;
using System.Collections.Generic;

namespace ThirdPersonSimulation
{
    internal sealed class GameplayEffectControlRuntime<
        TApplication,
        TSpec,
        TActive,
        TPrediction,
        TTagQuery,
        TSavepoint>
        where TActive : class, IGameplayEffectActiveControl<TSpec>
        where TPrediction : class, IGameplayEffectPredictionControl<TSpec>
    {
        readonly IGameplayEffectControlPort<TApplication, TSpec, TActive, TPrediction, TTagQuery, TSavepoint> m_Port;
        readonly Queue<PendingAdditionalApplication> m_PendingAdditional = new Queue<PendingAdditionalApplication>();
        TPrediction m_CurrentPrediction;

        public GameplayEffectControlRuntime(
            IGameplayEffectControlPort<TApplication, TSpec, TActive, TPrediction, TTagQuery, TSavepoint> port)
        {
            m_Port = port ?? throw new ArgumentNullException(nameof(port));
        }

        public GameplayEffectApplyResult Apply(TApplication application)
        {
            TSavepoint savepoint = m_Port.CreateSavepoint();
            ulong allocator = m_Port.CaptureAllocator();
            int changeCount = m_Port.ChangeCount;
            try
            {
                GameplayEffectApplyResult result = ApplyCore(application);
                if (!result.AcceptedMutation)
                {
                    m_Port.Release(savepoint);
                    return result;
                }
                if (!FlushAdditional(out GameplayEffectApplyResult failure, out string ownerEffectId, out ulong ownerInstanceId, out TApplication failedApplication))
                {
                    m_Port.Restore(savepoint);
                    m_Port.RestoreAllocator(allocator);
                    m_Port.TrimChanges(changeCount);
                    m_Port.RebuildCauses();
                    GameplayEffectApplicationIdentity failed = m_Port.DescribeApplication(failedApplication);
                    m_Port.EmitFailure(ownerEffectId, ownerInstanceId, failed.EffectId, failure);
                    return new GameplayEffectApplyResult(failure.Kind, 0, 0, $"AdditionalEffect:{failure.Reason}");
                }
                m_Port.Release(savepoint);
                return result;
            }
            catch
            {
                if (m_Port.SavepointIsActive(savepoint))
                    m_Port.Restore(savepoint);
                m_Port.RestoreAllocator(allocator);
                m_Port.TrimChanges(changeCount);
                m_PendingAdditional.Clear();
                CancelPrediction();
                m_Port.RebuildCauses();
                throw;
            }
        }

        public int Remove(GameplayEffectRemoveRequest<TTagQuery> request)
        {
            TSavepoint savepoint = m_Port.CreateSavepoint();
            ulong allocator = m_Port.CaptureAllocator();
            int changeCount = m_Port.ChangeCount;
            try
            {
                IReadOnlyList<TActive> activeEffects = m_Port.AcquireActiveEffects();
                int removed = 0;
                try
                {
                    for (int i = 0; i < activeEffects.Count; i++)
                    {
                        TActive active = activeEffects[i];
                        if (!MatchesRemoval(active, request))
                            continue;
                        RemoveActive(active, GameplayEffectLifecycleKind.Removed, true);
                        removed++;
                        if (request.Selector == GameplayEffectRemoveSelector.Handle)
                            break;
                    }
                }
                finally
                {
                    m_Port.ReleaseActiveEffects(activeEffects);
                }
                if (!FlushAdditional(out GameplayEffectApplyResult failure, out string ownerEffectId, out ulong ownerInstanceId, out TApplication failedApplication))
                {
                    m_Port.Restore(savepoint);
                    m_Port.RestoreAllocator(allocator);
                    m_Port.TrimChanges(changeCount);
                    m_Port.RebuildCauses();
                    GameplayEffectApplicationIdentity failed = m_Port.DescribeApplication(failedApplication);
                    m_Port.EmitFailure(ownerEffectId, ownerInstanceId, failed.EffectId, failure);
                    return 0;
                }
                m_Port.Release(savepoint);
                return removed;
            }
            catch
            {
                if (m_Port.SavepointIsActive(savepoint))
                    m_Port.Restore(savepoint);
                m_Port.RestoreAllocator(allocator);
                m_Port.TrimChanges(changeCount);
                m_PendingAdditional.Clear();
                m_Port.RebuildCauses();
                throw;
            }
        }

        public void Advance()
        {
            if (!m_Port.HasActiveEffects)
                return;
            TSavepoint savepoint = m_Port.CreateSavepoint();
            ulong allocator = m_Port.CaptureAllocator();
            int changeCount = m_Port.ChangeCount;
            try
            {
                IReadOnlyList<TActive> activeEffects = m_Port.AcquireActiveEffects();
                try
                {
                    for (int i = 0; i < activeEffects.Count; i++)
                    {
                        TActive active = activeEffects[i];
                        if (m_Port.FindActiveByHandle(active.Handle) == null)
                            continue;
                        if (RemovalRequirementMet(active))
                        {
                            RemoveActive(active, GameplayEffectLifecycleKind.Removed, true);
                            continue;
                        }
                        bool ongoing = OngoingRequirementsMet(active);
                        if (!ongoing && !active.Inhibited)
                        {
                            m_Port.DeactivatePersistent(active);
                            active.Inhibited = true;
                            active.LifecycleRevision = checked(active.LifecycleRevision + 1);
                            m_Port.MarkActiveEffectsDirty();
                            m_Port.EmitLifecycle(active, GameplayEffectLifecycleKind.Inhibited);
                        }
                        else if (ongoing && active.Inhibited)
                        {
                            active.Inhibited = false;
                            ActivatePersistent(active);
                            active.LifecycleRevision = checked(active.LifecycleRevision + 1);
                            m_Port.MarkActiveEffectsDirty();
                            m_Port.EmitLifecycle(active, GameplayEffectLifecycleKind.Resumed);
                        }
                        if (!active.Inhibited)
                        {
                            GameplayEffectPreparedSpec<TSpec> spec = m_Port.DescribeSpec(active.Spec);
                            ulong nextPeriod = m_Port.GetNextPeriod(active.InstanceId);
                            while (spec.PeriodTicks > 0 && nextPeriod <= m_Port.Tick &&
                                   (active.EndTick == 0 || nextPeriod < active.EndTick))
                            {
                                ExecutePeriod(active, 0);
                                nextPeriod = CheckedAdd(nextPeriod, spec.PeriodTicks);
                                m_Port.SetNextPeriod(active.InstanceId, nextPeriod);
                            }
                            InvokeWhileActive(active);
                        }
                        if (active.EndTick != 0 && m_Port.Tick >= active.EndTick)
                            RemoveActive(active, GameplayEffectLifecycleKind.Expired, true);
                    }
                }
                finally
                {
                    m_Port.ReleaseActiveEffects(activeEffects);
                }
                if (!FlushAdditional(out GameplayEffectApplyResult failure, out string ownerEffectId, out ulong ownerInstanceId, out TApplication failedApplication))
                {
                    m_Port.Restore(savepoint);
                    m_Port.RestoreAllocator(allocator);
                    m_Port.TrimChanges(changeCount);
                    m_Port.RebuildCauses();
                    GameplayEffectApplicationIdentity failed = m_Port.DescribeApplication(failedApplication);
                    m_Port.EmitFailure(ownerEffectId, ownerInstanceId, failed.EffectId, failure);
                    return;
                }
                m_Port.Release(savepoint);
            }
            catch
            {
                if (m_Port.SavepointIsActive(savepoint))
                    m_Port.Restore(savepoint);
                m_Port.RestoreAllocator(allocator);
                m_Port.TrimChanges(changeCount);
                m_PendingAdditional.Clear();
                m_Port.RebuildCauses();
                throw;
            }
        }

        public void ApplyLifecycle(GameplayEffectLifecycleCommand<TApplication> command)
        {
            if (command.Context.IsValid && command.Context.TargetActor != m_Port.ActorId)
                throw new InvalidOperationException($"Gameplay Effect lifecycle targets '{command.Context.TargetActor}', expected '{m_Port.ActorId}'.");
            if (command.InstanceId != 0 &&
                m_Port.TryGetLastLifecycleRevision(command.InstanceId, out ulong last) &&
                command.LifecycleRevision <= last)
            {
                return;
            }

            switch (command.Kind)
            {
                case GameplayEffectLifecycleKind.Confirmed:
                    ConfirmPrediction(command);
                    break;
                case GameplayEffectLifecycleKind.Rejected:
                    RejectPrediction(command.Context.PredictionKey);
                    break;
                case GameplayEffectLifecycleKind.Corrected:
                    RejectPrediction(command.Context.PredictionKey);
                    ApplyAuthoritative(command, true);
                    break;
                case GameplayEffectLifecycleKind.Applied:
                    ApplyAuthoritative(command, false);
                    break;
                case GameplayEffectLifecycleKind.Removed:
                case GameplayEffectLifecycleKind.Expired:
                    RemoveActive(RequireIngressActive(command), command.Kind, true);
                    break;
                case GameplayEffectLifecycleKind.StackChanged:
                {
                    TActive active = RequireIngressActive(command);
                    m_Port.DeactivatePersistent(active);
                    active.StackCount = Math.Max(1, command.StackCount);
                    active.StartTick = command.StartTick;
                    active.EndTick = command.EndTick;
                    active.LifecycleRevision = command.LifecycleRevision;
                    m_Port.MarkActiveEffectsDirty();
                    ActivatePersistent(active);
                    m_Port.EmitLifecycle(active, GameplayEffectLifecycleKind.StackChanged);
                    break;
                }
                case GameplayEffectLifecycleKind.Inhibited:
                case GameplayEffectLifecycleKind.Resumed:
                {
                    TActive active = RequireIngressActive(command);
                    if (command.Kind == GameplayEffectLifecycleKind.Inhibited && !active.Inhibited)
                    {
                        m_Port.DeactivatePersistent(active);
                        active.Inhibited = true;
                    }
                    else if (command.Kind == GameplayEffectLifecycleKind.Resumed && active.Inhibited)
                    {
                        active.Inhibited = false;
                        ActivatePersistent(active);
                    }
                    active.LifecycleRevision = command.LifecycleRevision;
                    m_Port.MarkActiveEffectsDirty();
                    m_Port.EmitLifecycle(active, command.Kind);
                    break;
                }
                case GameplayEffectLifecycleKind.PeriodExecuted:
                    ExecutePeriod(RequireIngressActive(command), command.LifecycleRevision);
                    break;
                default:
                    throw new InvalidOperationException($"Gameplay Effect lifecycle operation '{command.Kind}' is not accepted as ingress.");
            }
            if (command.InstanceId != 0 && command.LifecycleRevision != 0)
            {
                m_Port.SetLastLifecycleRevision(command.InstanceId, command.LifecycleRevision);
                m_Port.MarkJournalDirty();
            }
        }

        public void ClearConfirmedAction(ulong actionInstanceId)
        {
            if (actionInstanceId == 0)
                return;
            IReadOnlyList<ulong> keys = m_Port.AcquirePredictionKeys();
            bool removed = false;
            try
            {
                for (int keyIndex = 0; keyIndex < keys.Count; keyIndex++)
                {
                    if (!m_Port.TryGetPredictions(keys[keyIndex], out IReadOnlyList<TPrediction> records) || records.Count == 0)
                        continue;
                    bool removable = true;
                    for (int recordIndex = 0; recordIndex < records.Count; recordIndex++)
                    {
                        TPrediction record = records[recordIndex];
                        if (!record.Confirmed || record.SourceActionInstanceId != actionInstanceId)
                        {
                            removable = false;
                            break;
                        }
                    }
                    if (removable)
                    {
                        m_Port.RemovePredictions(keys[keyIndex]);
                        removed = true;
                    }
                }
            }
            finally
            {
                m_Port.ReleasePredictionKeys(keys);
            }
            if (removed)
                m_Port.MarkJournalDirty();
        }

        GameplayEffectApplyResult ApplyCore(TApplication application)
        {
            if (!m_Port.TryPrepare(application, out GameplayEffectPreparedSpec<TSpec> spec, out GameplayEffectApplyResult failure))
                return Reject(application, failure);
            if (!ApplicationRequirementsMet(spec, out string reason))
                return Reject(application, new GameplayEffectApplyResult(GameplayEffectApplyResultKind.RequirementFailed, 0, 0, reason));
            GameplayEffectApplicationIdentity identity = m_Port.DescribeApplication(application);
            if (spec.Descriptor.Duration == GameplayEffectDurationKind.Instant)
                return ApplyInstant(spec, identity.AuthoritativeInstanceId, identity.AuthoritativeLifecycleRevision);
            TActive stack = FindStack(spec);
            return stack == null
                ? ApplyNewActive(spec, identity.AuthoritativeInstanceId, identity.AuthoritativeLifecycleRevision)
                : ApplyStack(spec, stack);
        }

        GameplayEffectApplyResult ApplyInstant(
            GameplayEffectPreparedSpec<TSpec> spec,
            ulong authoritativeInstanceId,
            ulong authoritativeRevision)
        {
            ulong handle = m_Port.AllocateHandle();
            ulong instanceId = ResolveInstanceId(authoritativeInstanceId);
            ulong revision = authoritativeRevision > 0 ? authoritativeRevision : 1;
            m_Port.RegisterCause(handle, spec, instanceId);
            BeginPrediction(spec, handle, instanceId, false, null);
            try
            {
                InvokeApplied(spec, instanceId);
                InvokeExecute(spec, null, handle, instanceId, 1, false);
                m_Port.EmitLifecycle(spec, instanceId, GameplayEffectLifecycleKind.Applied, m_Port.Tick, m_Port.Tick, 1, revision, true);
                CompletePrediction();
                return new GameplayEffectApplyResult(GameplayEffectApplyResultKind.Applied, handle, instanceId, string.Empty);
            }
            catch
            {
                CancelPrediction();
                throw;
            }
        }

        GameplayEffectApplyResult ApplyNewActive(
            GameplayEffectPreparedSpec<TSpec> spec,
            ulong authoritativeInstanceId,
            ulong authoritativeRevision)
        {
            ulong handle = m_Port.AllocateHandle();
            ulong instanceId = ResolveInstanceId(authoritativeInstanceId);
            ulong revision = authoritativeRevision > 0 ? authoritativeRevision : 1;
            ulong endTick = spec.Descriptor.Duration == GameplayEffectDurationKind.Duration
                ? CheckedAdd(m_Port.Tick, spec.DurationTicks)
                : 0;
            TActive active = m_Port.CreateActive(
                spec,
                handle,
                instanceId,
                m_Port.Tick,
                endTick,
                m_Port.AllocateHandle(),
                revision);
            m_Port.AddActive(active);
            if (spec.PeriodTicks > 0)
                m_Port.SetNextPeriod(instanceId, CheckedAdd(m_Port.Tick, spec.PeriodTicks));
            m_Port.RegisterCause(handle, spec, instanceId);
            BeginPrediction(spec, handle, instanceId, true, null);
            try
            {
                ActivatePersistent(active);
                InvokeApplied(spec, instanceId);
                m_Port.EmitLifecycle(active, GameplayEffectLifecycleKind.Applied);
                if (spec.Descriptor.ExecuteOnApplication)
                    ExecutePeriod(active, 0);
                CompletePrediction();
                return new GameplayEffectApplyResult(GameplayEffectApplyResultKind.Applied, handle, instanceId, string.Empty);
            }
            catch
            {
                m_Port.DeactivatePersistent(active);
                m_Port.RemoveActive(active);
                CancelPrediction();
                throw;
            }
        }

        GameplayEffectApplyResult ApplyStack(GameplayEffectPreparedSpec<TSpec> incoming, TActive active)
        {
            GameplayEffectPreparedSpec<TSpec> activeSpec = m_Port.DescribeSpec(active.Spec);
            m_Port.RegisterCause(active.Handle, activeSpec, active.InstanceId);
            if (active.StackCount >= incoming.Descriptor.MaximumStacks)
                return ApplyOverflow(incoming, active);
            GameplayEffectActiveControlSnapshot before = CaptureActive(active);
            BeginPrediction(incoming, active.Handle, active.InstanceId, false, active);
            try
            {
                m_Port.DeactivatePersistent(active);
                active.StackCount++;
                active.LifecycleRevision = checked(active.LifecycleRevision + 1);
                UpdateStackTime(active, incoming);
                m_Port.MarkActiveEffectsDirty();
                ActivatePersistent(active);
                InvokeApplied(incoming, active.InstanceId);
                m_Port.EmitLifecycle(active, GameplayEffectLifecycleKind.StackChanged);
                if (incoming.Descriptor.ExecuteOnApplication)
                    ExecutePeriod(active, 0);
                CompletePrediction();
                return new GameplayEffectApplyResult(GameplayEffectApplyResultKind.Applied, active.Handle, active.InstanceId, string.Empty);
            }
            catch
            {
                m_Port.DeactivatePersistent(active);
                RestoreActive(active, before);
                CancelPrediction();
                throw;
            }
        }

        GameplayEffectApplyResult ApplyOverflow(GameplayEffectPreparedSpec<TSpec> spec, TActive active)
        {
            active.LifecycleRevision = checked(active.LifecycleRevision + 1);
            m_Port.MarkActiveEffectsDirty();
            m_Port.EmitLifecycle(active, GameplayEffectLifecycleKind.Overflow);
            switch (spec.Descriptor.Overflow)
            {
                case GameplayEffectOverflowKind.ReplaceOldest:
                    RemoveActive(active, GameplayEffectLifecycleKind.Removed, true);
                    return ApplyNewActive(spec, 0, 0);
                case GameplayEffectOverflowKind.ApplyOverflowEffects:
                    InvokeOverflow(spec, active);
                    return new GameplayEffectApplyResult(GameplayEffectApplyResultKind.OverflowRejected, active.Handle, active.InstanceId, "OverflowEffectsApplied");
                default:
                    return new GameplayEffectApplyResult(GameplayEffectApplyResultKind.OverflowRejected, active.Handle, active.InstanceId, "MaxStacksReached");
            }
        }

        void ExecutePeriod(TActive active, ulong authoritativeRevision)
        {
            GameplayEffectPreparedSpec<TSpec> spec = m_Port.DescribeSpec(active.Spec);
            InvokeExecute(spec, active, active.Handle, active.InstanceId, active.StackCount, true);
            active.LifecycleRevision = authoritativeRevision > 0
                ? authoritativeRevision
                : checked(active.LifecycleRevision + 1);
            m_Port.MarkActiveEffectsDirty();
            m_Port.EmitLifecycle(active, GameplayEffectLifecycleKind.PeriodExecuted);
        }

        void RemoveActive(TActive active, GameplayEffectLifecycleKind lifecycle, bool emitOutputs)
        {
            if (active == null || m_Port.FindActiveByHandle(active.Handle) == null)
                return;
            m_Port.DeactivatePersistent(active);
            if (emitOutputs)
            {
                InvokeRemoved(active, lifecycle);
                active.LifecycleRevision = checked(active.LifecycleRevision + 1);
                m_Port.MarkActiveEffectsDirty();
                m_Port.EmitLifecycle(active, lifecycle);
            }
            m_Port.RemoveActive(active);
        }

        void RestoreActive(TActive active, GameplayEffectActiveControlSnapshot snapshot)
        {
            active.InstanceId = snapshot.InstanceId;
            active.StartTick = snapshot.StartTick;
            active.EndTick = snapshot.EndTick;
            active.StackCount = snapshot.StackCount;
            active.Inhibited = snapshot.Inhibited;
            active.LifecycleRevision = snapshot.LifecycleRevision;
            m_Port.MarkActiveEffectsDirty();
            m_Port.SetNextPeriod(active.InstanceId, snapshot.NextPeriodTick);
            if (!active.Inhibited)
                ActivatePersistent(active);
        }

        void UpdateStackTime(TActive active, GameplayEffectPreparedSpec<TSpec> incoming)
        {
            GameplayEffectPreparedSpec<TSpec> existing = m_Port.DescribeSpec(active.Spec);
            if (existing.Descriptor.Duration == GameplayEffectDurationKind.Duration)
            {
                switch (existing.Descriptor.DurationUpdate)
                {
                    case GameplayEffectDurationUpdateKind.Refresh:
                        active.StartTick = m_Port.Tick;
                        active.EndTick = CheckedAdd(m_Port.Tick, incoming.DurationTicks);
                        m_Port.MarkActiveEffectsDirty();
                        break;
                    case GameplayEffectDurationUpdateKind.Extend:
                        active.EndTick = CheckedAdd(active.EndTick, incoming.DurationTicks);
                        m_Port.MarkActiveEffectsDirty();
                        break;
                }
            }
            if (existing.Descriptor.PeriodUpdate == GameplayEffectPeriodUpdateKind.Reset && incoming.PeriodTicks > 0)
                m_Port.SetNextPeriod(active.InstanceId, CheckedAdd(m_Port.Tick, incoming.PeriodTicks));
        }

        bool ApplicationRequirementsMet(GameplayEffectPreparedSpec<TSpec> spec, out string reason)
        {
            int count = m_Port.ComponentCount(spec.TargetSpec);
            for (int i = 0; i < count; i++)
            {
                GameplayEffectComponentDescriptor component = m_Port.DescribeComponent(spec.TargetSpec, i);
                if (component.RequirementPhase != GameplayEffectRequirementPhase.Application)
                    continue;
                if (component.Kind == GameplayEffectComponentKind.TagRequirement &&
                    !m_Port.EvaluateTagRequirement(spec.TargetSpec, i))
                {
                    reason = "TagRequirementFailed";
                    return false;
                }
                if (component.Kind == GameplayEffectComponentKind.AttributeRequirement &&
                    !m_Port.EvaluateAttributeRequirement(spec.TargetSpec, i))
                {
                    reason = "AttributeRequirementFailed";
                    return false;
                }
            }
            reason = string.Empty;
            return true;
        }

        TActive FindStack(GameplayEffectPreparedSpec<TSpec> incoming)
        {
            if (incoming.Descriptor.Stacking == GameplayEffectStackingKind.None)
                return null;
            IReadOnlyList<TActive> activeEffects = m_Port.AcquireActiveEffects();
            try
            {
                for (int i = 0; i < activeEffects.Count; i++)
                {
                    TActive active = activeEffects[i];
                    GameplayEffectPreparedSpec<TSpec> existing = m_Port.DescribeSpec(active.Spec);
                    if (!string.Equals(existing.Descriptor.EffectId, incoming.Descriptor.EffectId, StringComparison.Ordinal))
                        continue;
                    if (incoming.Descriptor.Stacking == GameplayEffectStackingKind.BySource &&
                        existing.Context.SourceActor != incoming.Context.SourceActor)
                    {
                        continue;
                    }
                    return active;
                }
                return null;
            }
            finally
            {
                m_Port.ReleaseActiveEffects(activeEffects);
            }
        }

        bool MatchesRemoval(TActive active, GameplayEffectRemoveRequest<TTagQuery> request)
        {
            return request.Selector switch
            {
                GameplayEffectRemoveSelector.Handle => request.Handle != 0 && active.Handle == request.Handle,
                GameplayEffectRemoveSelector.EffectId => !string.IsNullOrEmpty(request.EffectId) && m_Port.MatchesEffectId(active.Spec, request.EffectId),
                GameplayEffectRemoveSelector.SourceActor => request.SourceActor.IsValid && m_Port.DescribeSpec(active.Spec).Context.SourceActor == request.SourceActor,
                GameplayEffectRemoveSelector.EffectTagQuery => m_Port.MatchesEffectTagQuery(active.Spec, request.TagQuery),
                _ => false
            };
        }

        bool OngoingRequirementsMet(TActive active)
        {
            int count = m_Port.ComponentCount(active.Spec);
            for (int i = 0; i < count; i++)
            {
                GameplayEffectComponentDescriptor component = m_Port.DescribeComponent(active.Spec, i);
                if (component.RequirementPhase != GameplayEffectRequirementPhase.Ongoing)
                    continue;
                if (component.Kind == GameplayEffectComponentKind.TagRequirement &&
                    !m_Port.EvaluateTagRequirement(active.Spec, i))
                {
                    return false;
                }
                if (component.Kind == GameplayEffectComponentKind.AttributeRequirement &&
                    !m_Port.EvaluateAttributeRequirement(active.Spec, i))
                {
                    return false;
                }
            }
            return true;
        }

        bool RemovalRequirementMet(TActive active)
        {
            int count = m_Port.ComponentCount(active.Spec);
            for (int i = 0; i < count; i++)
            {
                GameplayEffectComponentDescriptor component = m_Port.DescribeComponent(active.Spec, i);
                if (component.RequirementPhase != GameplayEffectRequirementPhase.Removal)
                    continue;
                if (component.Kind == GameplayEffectComponentKind.TagRequirement &&
                    m_Port.EvaluateTagRequirement(active.Spec, i))
                {
                    return true;
                }
                if (component.Kind == GameplayEffectComponentKind.AttributeRequirement &&
                    m_Port.EvaluateAttributeRequirement(active.Spec, i))
                {
                    return true;
                }
            }
            return false;
        }

        void ActivatePersistent(TActive active)
        {
            int count = m_Port.ComponentCount(active.Spec);
            for (int i = 0; i < count; i++)
            {
                GameplayEffectComponentDescriptor component = m_Port.DescribeComponent(active.Spec, i);
                if (component.Kind == GameplayEffectComponentKind.Modifier &&
                    component.ModifierPhase == GameplayEffectModifierPhase.CurrentValue)
                {
                    m_Port.ActivateCurrentModifier(active, i);
                }
            }
            m_Port.ActivateGrantedTags(active);
        }

        void InvokeApplied(GameplayEffectPreparedSpec<TSpec> spec, ulong instanceId)
        {
            int count = m_Port.ComponentCount(spec.TargetSpec);
            for (int i = 0; i < count; i++)
            {
                GameplayEffectComponentDescriptor component = m_Port.DescribeComponent(spec.TargetSpec, i);
                if (component.Kind == GameplayEffectComponentKind.AdditionalEffects)
                    EnqueueAdditional(spec, instanceId, i, GameplayEffectAdditionalTrigger.Applied);
                else if (component.Kind == GameplayEffectComponentKind.Cue && component.CueTrigger == GameplayEffectCueTrigger.OnActive)
                    m_Port.EmitCue(spec, instanceId, i, true);
            }
        }

        void InvokeExecute(
            GameplayEffectPreparedSpec<TSpec> spec,
            TActive active,
            ulong handle,
            ulong instanceId,
            int stackCount,
            bool period)
        {
            int count = m_Port.ComponentCount(spec.TargetSpec);
            for (int i = 0; i < count; i++)
            {
                GameplayEffectComponentDescriptor component = m_Port.DescribeComponent(spec.TargetSpec, i);
                if (component.Kind == GameplayEffectComponentKind.Execution ||
                    component.Kind == GameplayEffectComponentKind.Modifier && component.ModifierPhase == GameplayEffectModifierPhase.BaseValue)
                {
                    m_Port.ExecuteNumericComponent(spec, active, handle, stackCount, i);
                }
                else if (period && component.Kind == GameplayEffectComponentKind.AdditionalEffects)
                {
                    EnqueueAdditional(spec, instanceId, i, GameplayEffectAdditionalTrigger.Period);
                }
                else if (component.Kind == GameplayEffectComponentKind.Cue && component.CueTrigger == GameplayEffectCueTrigger.Executed)
                {
                    m_Port.EmitCue(spec, instanceId, i, true);
                }
            }
        }

        void InvokeWhileActive(TActive active)
        {
            GameplayEffectPreparedSpec<TSpec> spec = m_Port.DescribeSpec(active.Spec);
            int count = m_Port.ComponentCount(active.Spec);
            for (int i = 0; i < count; i++)
            {
                GameplayEffectComponentDescriptor component = m_Port.DescribeComponent(active.Spec, i);
                if (component.Kind == GameplayEffectComponentKind.Cue && component.CueTrigger == GameplayEffectCueTrigger.WhileActive)
                    m_Port.EmitCue(spec, active.InstanceId, i, true);
            }
        }

        void InvokeRemoved(TActive active, GameplayEffectLifecycleKind lifecycle)
        {
            GameplayEffectPreparedSpec<TSpec> spec = m_Port.DescribeSpec(active.Spec);
            GameplayEffectCueTrigger trigger = lifecycle == GameplayEffectLifecycleKind.Expired
                ? GameplayEffectCueTrigger.Expired
                : GameplayEffectCueTrigger.Removed;
            int count = m_Port.ComponentCount(active.Spec);
            for (int i = 0; i < count; i++)
            {
                GameplayEffectComponentDescriptor component = m_Port.DescribeComponent(active.Spec, i);
                if (component.Kind == GameplayEffectComponentKind.AdditionalEffects)
                    EnqueueAdditional(spec, active.InstanceId, i, GameplayEffectAdditionalTrigger.Removed);
                else if (component.Kind == GameplayEffectComponentKind.Cue && component.CueTrigger == trigger)
                    m_Port.EmitCue(spec, active.InstanceId, i, true);
            }
        }

        void InvokeOverflow(GameplayEffectPreparedSpec<TSpec> spec, TActive active)
        {
            int count = m_Port.ComponentCount(spec.TargetSpec);
            for (int i = 0; i < count; i++)
            {
                GameplayEffectComponentDescriptor component = m_Port.DescribeComponent(spec.TargetSpec, i);
                if (component.Kind == GameplayEffectComponentKind.AdditionalEffects)
                    EnqueueAdditional(spec, active.InstanceId, i, GameplayEffectAdditionalTrigger.Overflow);
            }
        }

        void EnqueueAdditional(
            GameplayEffectPreparedSpec<TSpec> spec,
            ulong ownerInstanceId,
            int componentIndex,
            GameplayEffectAdditionalTrigger trigger)
        {
            int count = m_Port.AdditionalEffectCount(spec.TargetSpec, componentIndex);
            for (int i = 0; i < count; i++)
            {
                if (m_Port.DescribeAdditionalEffectTrigger(spec.TargetSpec, componentIndex, i) != trigger)
                    continue;
                TApplication application = m_Port.BuildAdditionalApplication(spec, ownerInstanceId, componentIndex, i);
                m_PendingAdditional.Enqueue(new PendingAdditionalApplication(
                    application,
                    spec.Descriptor.EffectId,
                    ownerInstanceId));
            }
        }

        void BeginPrediction(
            GameplayEffectPreparedSpec<TSpec> spec,
            ulong handle,
            ulong instanceId,
            bool createdActive,
            TActive activeBefore)
        {
            if (!spec.Context.Predicted)
                return;
            if (m_CurrentPrediction != null)
                throw new InvalidOperationException("Nested Gameplay Effect prediction records are not supported.");
            GameplayEffectActiveControlSnapshot snapshot = activeBefore == null
                ? default
                : CaptureActive(activeBefore);
            m_CurrentPrediction = m_Port.CreatePrediction(
                spec,
                handle,
                instanceId,
                createdActive,
                activeBefore != null,
                snapshot);
            m_Port.SetCurrentPrediction(m_CurrentPrediction);
        }

        void CompletePrediction()
        {
            TPrediction record = m_CurrentPrediction;
            m_CurrentPrediction = null;
            m_Port.SetCurrentPrediction(null);
            if (record == null)
                return;
            m_Port.CompletePrediction(record);
            m_Port.AddPrediction(record);
            m_Port.MarkJournalDirty();
        }

        void CancelPrediction()
        {
            TPrediction record = m_CurrentPrediction;
            m_CurrentPrediction = null;
            m_Port.SetCurrentPrediction(null);
            if (record != null)
                m_Port.CancelPrediction(record);
        }

        void ConfirmPrediction(GameplayEffectLifecycleCommand<TApplication> command)
        {
            ulong key = command.Context.PredictionKey;
            if (!m_Port.TryGetPredictions(key, out IReadOnlyList<TPrediction> records))
                throw new InvalidOperationException($"Gameplay Effect prediction '{key}' was not found.");
            bool found = false;
            for (int i = 0; i < records.Count; i++)
            {
                TPrediction record = records[i];
                if (!string.IsNullOrEmpty(command.EffectId) && !m_Port.MatchesEffectId(record.Spec, command.EffectId))
                    continue;
                found = true;
                ulong instanceId = record.InstanceId;
                TActive active = m_Port.FindActiveByHandle(record.Handle);
                if (active != null)
                {
                    if (command.InstanceId != 0 && command.InstanceId != active.InstanceId)
                    {
                        if (m_Port.FindActiveByInstance(command.InstanceId) != null)
                            throw new InvalidOperationException($"Authoritative Gameplay Effect instance '{command.InstanceId}' conflicts with an active instance.");
                        ulong nextPeriod = m_Port.GetNextPeriod(active.InstanceId);
                        m_Port.SetNextPeriod(active.InstanceId, 0);
                        active.InstanceId = command.InstanceId;
                        m_Port.SetNextPeriod(active.InstanceId, nextPeriod);
                    }
                    active.LifecycleRevision = Math.Max(active.LifecycleRevision, command.LifecycleRevision > 0 ? command.LifecycleRevision : 1);
                    m_Port.MarkActiveEffectsDirty();
                    instanceId = active.InstanceId;
                    m_Port.EmitLifecycle(active, GameplayEffectLifecycleKind.Confirmed);
                }
                else
                {
                    GameplayEffectPreparedSpec<TSpec> spec = m_Port.DescribeSpec(record.Spec);
                    m_Port.EmitLifecycle(
                        spec,
                        command.InstanceId != 0 ? command.InstanceId : instanceId,
                        GameplayEffectLifecycleKind.Confirmed,
                        m_Port.Tick,
                        m_Port.Tick,
                        1,
                        command.LifecycleRevision > 0 ? command.LifecycleRevision : 1,
                        true);
                }
                record.Confirmed = true;
            }
            if (!found)
                throw new InvalidOperationException($"Gameplay Effect prediction '{key}' did not match '{command.EffectId}'.");
            m_Port.MarkJournalDirty();
        }

        void RejectPrediction(ulong predictionKey)
        {
            if (predictionKey == 0 || !m_Port.TryGetPredictions(predictionKey, out IReadOnlyList<TPrediction> records))
                throw new InvalidOperationException($"Gameplay Effect prediction '{predictionKey}' was not found.");
            m_Port.RemovePredictions(predictionKey);
            m_Port.MarkJournalDirty();
            bool conflict = false;
            for (int i = records.Count - 1; i >= 0; i--)
            {
                TPrediction record = records[i];
                TActive active = m_Port.FindActiveByHandle(record.Handle);
                if (record.CreatedActive && active != null)
                {
                    m_Port.DeactivatePersistent(active);
                    m_Port.RemoveActive(active);
                }
                else if (record.HasActiveBefore && active != null)
                {
                    m_Port.DeactivatePersistent(active);
                    RestoreActive(active, record.ActiveBefore);
                }
                if (!m_Port.RestorePredictionAttributes(record))
                    conflict = true;
                for (int cueIndex = 0; cueIndex < record.CueIds.Count; cueIndex++)
                    m_Port.EmitPredictionCueRemoval(record, record.CueIds[cueIndex]);
                GameplayEffectPreparedSpec<TSpec> spec = m_Port.DescribeSpec(record.Spec);
                m_Port.EmitLifecycle(
                    spec,
                    record.InstanceId,
                    GameplayEffectLifecycleKind.Rejected,
                    m_Port.Tick,
                    m_Port.Tick,
                    0,
                    1,
                    spec.Descriptor.Duration == GameplayEffectDurationKind.Instant);
            }
            if (conflict)
            {
                m_Port.EmitFailure(
                    string.Empty,
                    0,
                    string.Empty,
                    new GameplayEffectApplyResult(GameplayEffectApplyResultKind.Rejected, 0, 0, "PredictionAttributeRevisionConflict"));
            }
        }

        void ApplyAuthoritative(GameplayEffectLifecycleCommand<TApplication> command, bool corrected)
        {
            GameplayEffectApplyResult result = Apply(command.AuthoritativeApplication);
            if (!result.AcceptedMutation)
                throw new InvalidOperationException($"Authoritative Gameplay Effect '{command.EffectId}' was rejected: {result.Kind}/{result.Reason}.");
            if (corrected)
            {
                TActive active = m_Port.FindActiveByInstance(result.InstanceId);
                if (active != null)
                    m_Port.EmitLifecycle(active, GameplayEffectLifecycleKind.Corrected);
            }
        }

        TActive RequireIngressActive(GameplayEffectLifecycleCommand<TApplication> command)
        {
            TActive active = m_Port.FindActiveByInstance(command.InstanceId);
            if (active == null || !m_Port.MatchesEffectId(active.Spec, command.EffectId))
                throw new InvalidOperationException($"Gameplay Effect lifecycle ingress does not match active instance '{command.EffectId}/{command.InstanceId}'.");
            return active;
        }

        GameplayEffectApplyResult Reject(TApplication application, GameplayEffectApplyResult failure)
        {
            m_Port.TryEmitRejectedApplication(application, failure);
            return failure;
        }

        bool FlushAdditional(
            out GameplayEffectApplyResult failure,
            out string ownerEffectId,
            out ulong ownerInstanceId,
            out TApplication failedApplication)
        {
            int processed = 0;
            while (m_PendingAdditional.Count > 0)
            {
                if (++processed > 4096)
                    throw new InvalidOperationException("Additional Gameplay Effect chain exceeded the portable execution limit.");
                PendingAdditionalApplication pending = m_PendingAdditional.Dequeue();
                TApplication application = pending.Application;
                ownerEffectId = pending.OwnerEffectId;
                ownerInstanceId = pending.OwnerInstanceId;
                GameplayEffectApplyResult result = ApplyCore(application);
                if (!result.AcceptedMutation)
                {
                    failure = result;
                    failedApplication = application;
                    m_PendingAdditional.Clear();
                    return false;
                }
            }
            failure = default;
            ownerEffectId = string.Empty;
            ownerInstanceId = 0;
            failedApplication = default;
            return true;
        }

        GameplayEffectActiveControlSnapshot CaptureActive(TActive active)
        {
            return new GameplayEffectActiveControlSnapshot(
                active.InstanceId,
                active.StartTick,
                active.EndTick,
                m_Port.GetNextPeriod(active.InstanceId),
                active.StackCount,
                active.Inhibited,
                active.LifecycleRevision);
        }

        ulong ResolveInstanceId(ulong authoritative)
        {
            if (authoritative != 0)
            {
                if (m_Port.FindActiveByInstance(authoritative) != null)
                    throw new InvalidOperationException($"Gameplay Effect instance '{authoritative}' already exists.");
                return authoritative;
            }
            ulong value;
            do value = m_Port.AllocateHandle();
            while (m_Port.FindActiveByInstance(value) != null);
            return value;
        }

        static ulong CheckedAdd(ulong left, ulong right)
        {
            if (ulong.MaxValue - left < right)
                throw new OverflowException("Gameplay Effect tick range overflowed.");
            return left + right;
        }

        readonly struct PendingAdditionalApplication
        {
            public PendingAdditionalApplication(
                TApplication application,
                string ownerEffectId,
                ulong ownerInstanceId)
            {
                Application = application;
                OwnerEffectId = ownerEffectId;
                OwnerInstanceId = ownerInstanceId;
            }

            public TApplication Application { get; }
            public string OwnerEffectId { get; }
            public ulong OwnerInstanceId { get; }
        }
    }
}
