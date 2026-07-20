using System;
using System.Collections.Generic;
using System.Linq;

namespace ThirdPersonSimulation
{
    internal sealed partial class Float32GameplayEffectTarget : IGameplayEffectControlPort<
        SimulationGameplayEffectApplication,
        PortableEffectSpecState,
        PortableActiveEffectState,
        PortablePredictionRecord,
        PortableTagQuery,
        Float32CharacterStateSavepoint>,
        IGameplayEffectApplicationAdmissionPort<
            SimulationGameplayEffectApplication,
            PortableEffectSpecState,
            Float32Scalar>
    {
        readonly Float32CharacterStateTransaction m_Transaction;
        readonly GameplayEffectStateAggregate m_CommittedState;
        readonly ActorId m_ActorId;
        readonly SimulationTick m_Tick;
        readonly int m_TickRate;
        readonly Func<ulong> m_AllocateHandle;
        readonly Func<ulong> m_CaptureAllocator;
        readonly Action<ulong> m_RestoreAllocator;
        readonly List<PortableEffectRuntimeChange> m_Changes;
        readonly Dictionary<ulong, PortableEffectCause> m_Causes;
        readonly Float32GameplayEffectExecutionScratch m_Scratch;
        readonly GameplayEffectControlRuntime<
            SimulationGameplayEffectApplication,
            PortableEffectSpecState,
            PortableActiveEffectState,
            PortablePredictionRecord,
            PortableTagQuery,
            Float32CharacterStateSavepoint> m_Control;
        readonly GameplayEffectApplicationAdmissionRuntime<
            SimulationGameplayEffectApplication,
            PortableEffectSpecState,
            Float32Scalar> m_Admission;
        SimulationGameplayEffectState m_State;
        PortablePredictionRecord m_CurrentPrediction;

        public Float32GameplayEffectTarget(
            Float32CharacterStateTransaction transaction,
            ActorId actorId,
            SimulationTick tick,
            Func<ulong> allocateHandle,
            Func<ulong> captureAllocator,
            Action<ulong> restoreAllocator,
            Float32GameplayEffectExecutionScratch scratch)
        {
            m_Transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
            m_CommittedState = transaction.GetGameplayEffectAggregate();
            m_ActorId = actorId;
            m_Tick = tick;
            m_TickRate = transaction.Program.Manifest.TickRate;
            m_AllocateHandle = allocateHandle ?? throw new ArgumentNullException(nameof(allocateHandle));
            m_CaptureAllocator = captureAllocator ?? throw new ArgumentNullException(nameof(captureAllocator));
            m_RestoreAllocator = restoreAllocator ?? throw new ArgumentNullException(nameof(restoreAllocator));
            if (scratch == null)
                throw new ArgumentNullException(nameof(scratch));
            m_Scratch = scratch;
            m_Changes = scratch.Changes;
            m_Causes = scratch.Causes;
            m_CommittedState.CollectActiveEffectIdentities(m_Scratch.ActiveIdentities);
            foreach (GameplayEffectActiveIdentity active in m_Scratch.ActiveIdentities)
                m_Causes[active.Handle] = new PortableEffectCause(active.Definition, active.InstanceId, active.Context);
            m_Control = new GameplayEffectControlRuntime<
                SimulationGameplayEffectApplication,
                PortableEffectSpecState,
                PortableActiveEffectState,
                PortablePredictionRecord,
                PortableTagQuery,
                Float32CharacterStateSavepoint>(this);
            m_Admission = new GameplayEffectApplicationAdmissionRuntime<
                SimulationGameplayEffectApplication,
                PortableEffectSpecState,
                Float32Scalar>(this, m_Scratch.SuppliedSourceAttributes);
        }

        public IReadOnlyList<string> OwnedTags => m_State != null ? m_State.CopyOwnedTags() : m_CommittedState.CopyOwnedTags();

        public bool HasTag(string tagId)
        {
            if (m_State != null)
                return m_State.HasTag(tagId);
            string query = SimulationGameplayEffectProgram.NormalizeTag(tagId);
            foreach (string owned in m_CommittedState.CopyOwnedTags())
            {
                if (m_Transaction.Layout.GameplayEffectProgram.IsTagOrParent(owned, query))
                    return true;
            }
            return false;
        }

        public bool Matches(PortableTagQuery query)
        {
            return m_State != null
                ? m_State.Matches(query)
                : m_Transaction.Layout.GameplayEffectProgram.Matches(query, m_CommittedState.CopyOwnedTags());
        }

        public bool TryGetAttribute(string attributeId, out Float32Scalar baseValue, out Float32Scalar currentValue, out ulong revision)
        {
            if (m_State != null && m_State.TryGetAttribute(attributeId, out PortableAttributeState value))
            {
                baseValue = value.BaseValue;
                currentValue = value.CurrentValue;
                revision = value.Revision;
                return true;
            }
            if (m_State == null && m_CommittedState.TryGetAttribute(attributeId, out baseValue, out currentValue, out revision))
                return true;
            baseValue = Float32Scalar.Zero;
            currentValue = Float32Scalar.Zero;
            revision = 0;
            return false;
        }

        public GameplayEffectApplyResult Apply(SimulationGameplayEffectApplication application)
        {
            EnsureWorkingState();
            return m_Control.Apply(application);
        }

        public int Remove(GameplayEffectRemoveRequest<PortableTagQuery> request)
        {
            EnsureWorkingState();
            return m_Control.Remove(request);
        }

        public void Advance()
        {
            m_Control.Advance();
        }

        public void ApplyGameplayResult(SimulationGameplayResultIngress ingress)
        {
            if (ingress.Application.Context.TargetActorId != m_ActorId)
                throw new InvalidOperationException($"Gameplay Result targets '{ingress.Application.Context.TargetActorId}', expected '{m_ActorId}'.");
            GameplayEffectApplyResult result = Apply(ingress.Application);
            if (!result.AcceptedMutation)
                throw new InvalidOperationException($"Gameplay Result Effect '{ingress.Application.EffectId}' was rejected: {result.Kind}/{result.Reason}.");
        }

        public void ApplyLifecycle(SimulationGameplayEffectLifecycleIngress ingress)
        {
            EnsureWorkingState();
            m_Control.ApplyLifecycle(ToCommand(ingress));
        }

        public void ApplyAttribute(SimulationAttributeValueIngress ingress)
        {
            EnsureWorkingState();
            ulong causeHandle = 0;
            if (ingress.CauseEffectInstanceId != 0)
            {
                PortableActiveEffectState active = m_State.FindActiveByInstance(ingress.CauseEffectInstanceId);
                if (active != null)
                    causeHandle = active.Handle;
            }
            if (causeHandle == 0 && !string.IsNullOrEmpty(ingress.CauseEffectId) && ingress.CauseContext.IsValid)
            {
                causeHandle = m_AllocateHandle();
                PortableEffectDefinition definition = m_State.Program.RequireEffect(ingress.CauseEffectId);
                m_Causes[causeHandle] = new PortableEffectCause(definition, ingress.CauseEffectInstanceId, ingress.CauseContext);
            }
            if (m_State.ApplyAuthoritativeAttribute(ingress.AttributeId, ingress.BaseValue, ingress.CurrentValue, ingress.ValueRevision, causeHandle, out IReadOnlyList<PortableAttributeChange> changes))
                AddAttributeChanges(changes);
        }

        public void ClearConfirmedAction(ulong actionInstanceId)
        {
            EnsureWorkingState();
            m_Control.ClearConfirmedAction(actionInstanceId);
        }

        public void SetActionTags(ulong actionInstanceId, IEnumerable<string> tags)
        {
            EnsureWorkingState();
            m_State.SetTagSource(GameplayTagSourceIdentity.ActionInstance(actionInstanceId), tags);
        }

        public void RemoveActionTags(ulong actionInstanceId)
        {
            EnsureWorkingState();
            m_State.RemoveTagSource(GameplayTagSourceIdentity.ActionInstance(actionInstanceId));
        }

        public IReadOnlyList<PortableEffectRuntimeChange> PendingChanges => m_Changes;
        public void ClearChanges() => m_Changes.Clear();

        bool EvaluateTagRequirement(PortableEffectSpecState spec, PortableTagRequirementsComponent requirement)
        {
            bool source = requirement.Source.IsEmpty || m_State.Program.Matches(requirement.Source, spec.SourceTags);
            bool target = requirement.Target.IsEmpty || m_State.Matches(requirement.Target);
            return source && target;
        }

        bool EvaluateAttributeRequirement(PortableEffectSpecState spec, PortableAttributeRequirementsComponent requirement)
        {
            Float32Scalar value;
            if (requirement.Source == PortableAttributeSource.SourceSnapshot)
            {
                if (!spec.SourceAttributes.TryGetValue(requirement.AttributeId, out value))
                    return false;
            }
            else if (!m_State.TryGetAttribute(requirement.AttributeId, out PortableAttributeState attribute))
            {
                return false;
            }
            else
            {
                value = attribute.CurrentValue;
            }
            if (!TryResolveMagnitude(spec, requirement.Threshold, out Float32Scalar threshold, out _))
                return false;
            return requirement.Comparison switch
            {
                PortableAttributeComparison.Less => value < threshold,
                PortableAttributeComparison.LessOrEqual => value <= threshold,
                PortableAttributeComparison.Equal => value == threshold,
                PortableAttributeComparison.GreaterOrEqual => value >= threshold,
                PortableAttributeComparison.Greater => value > threshold,
                PortableAttributeComparison.NotEqual => value != threshold,
                _ => false
            };
        }

        void ActivateCurrentModifier(PortableActiveEffectState active, PortableModifierComponent modifier)
        {
            if (!TryResolveMagnitude(active.Spec, modifier.Magnitude, out Float32Scalar value, out string liveAttribute))
                throw new InvalidOperationException($"Unable to resolve Gameplay Attribute modifier magnitude for '{modifier.AttributeId}'.");
            Float32Scalar coefficient = modifier.Magnitude.Coefficient;
            Float32Scalar postAdd = modifier.Magnitude.PostAdd;
            if (modifier.ScaleWithStack)
            {
                Float32Scalar stack = Float32Scalar.FromInt64(active.StackCount);
                if (string.IsNullOrEmpty(liveAttribute))
                    value *= stack;
                else
                {
                    coefficient *= stack;
                    postAdd *= stack;
                }
            }
            var state = new PortableAttributeModifierState
            {
                Handle = m_AllocateHandle(),
                SourceEffectHandle = active.Handle,
                Operation = modifier.Operation,
                Magnitude = value,
                Priority = modifier.Priority,
                ClampBound = modifier.ClampBound,
                LiveAttributeId = liveAttribute,
                LiveCoefficient = coefficient,
                LivePostAdd = postAdd,
                InsertionSequence = m_AllocateHandle()
            };
            AddAttributeChanges(m_State.AddModifier(modifier.AttributeId, state));
        }

        void ActivateGrantedTags(PortableActiveEffectState active)
        {
            var grantedTags = new SortedSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < active.Spec.Definition.Components.Length; i++)
            {
                if (active.Spec.Definition.Components[i] is not PortableGrantedTagsComponent granted)
                    continue;
                for (int tagIndex = 0; tagIndex < granted.Tags.Length; tagIndex++)
                    grantedTags.Add(granted.Tags[tagIndex]);
            }
            m_State.SetTagSource($"effect:{active.Handle}", grantedTags);
        }

        void DeactivatePersistent(PortableActiveEffectState active)
        {
            AddAttributeChanges(m_State.RemoveModifiersByEffect(active.Handle));
            m_State.RemoveTagSource($"effect:{active.Handle}");
        }

        void ExecuteNumericComponent(
            PortableEffectSpecState spec,
            ulong handle,
            int stackCount,
            PortableEffectComponent component)
        {
            switch (component)
            {
                case PortableModifierComponent modifier when modifier.Application == PortableModifierApplication.BaseValue:
                {
                    if (!TryResolveMagnitude(spec, modifier.Magnitude, out Float32Scalar magnitude, out _))
                        throw new InvalidOperationException($"Unable to resolve Gameplay Modifier magnitude for '{modifier.AttributeId}'.");
                    if (modifier.ScaleWithStack)
                        magnitude *= Float32Scalar.FromInt64(stackCount);
                    CapturePredictionBefore(modifier.AttributeId);
                    AddAttributeChanges(m_State.MutateBase(modifier.AttributeId, modifier.Operation, magnitude, modifier.ClampBound, handle));
                    break;
                }
                case PortableExecutionComponent execution:
                {
                    for (int mutationIndex = 0; mutationIndex < execution.Mutations.Length; mutationIndex++)
                    {
                        PortableExecutionMutation mutation = execution.Mutations[mutationIndex];
                        if (!TryResolveMagnitude(spec, mutation.Magnitude, out Float32Scalar magnitude, out _))
                            throw new InvalidOperationException($"Unable to resolve Gameplay Execution magnitude for '{mutation.AttributeId}'.");
                        magnitude *= Float32Scalar.FromInt64(stackCount);
                        CapturePredictionBefore(mutation.AttributeId);
                        AddAttributeChanges(m_State.MutateBase(mutation.AttributeId, mutation.Operation, magnitude, mutation.ClampBound, handle));
                    }
                    break;
                }
            }
        }

        bool TryResolveMagnitude(PortableEffectSpecState spec, PortableMagnitude magnitude, out Float32Scalar value, out string liveAttribute)
        {
            value = Float32Scalar.Zero;
            liveAttribute = string.Empty;
            switch (magnitude.Source)
            {
                case PortableMagnitudeSource.Constant:
                    value = magnitude.Constant;
                    break;
                case PortableMagnitudeSource.SetByCaller:
                    if (!spec.SetByCaller.TryGetValue(magnitude.SetByCallerParameterId, out value))
                        return false;
                    break;
                case PortableMagnitudeSource.SourceAttributeSnapshot:
                    if (!spec.SourceAttributes.TryGetValue(magnitude.AttributeId, out value))
                        return false;
                    break;
                case PortableMagnitudeSource.TargetAttributeSnapshot:
                    if (!spec.TargetAttributes.TryGetValue(magnitude.AttributeId, out value))
                        return false;
                    break;
                case PortableMagnitudeSource.TargetAttributeLive:
                    if (!m_State.TryGetAttribute(magnitude.AttributeId, out PortableAttributeState attribute))
                        return false;
                    value = attribute.CurrentValue;
                    liveAttribute = magnitude.AttributeId;
                    break;
                default:
                    return false;
            }
            value = value * magnitude.Coefficient + magnitude.PostAdd;
            return true;
        }

        IEnumerable<string> CollectSnapshotAttributes(PortableEffectDefinition definition, PortableMagnitudeSource source)
        {
            var result = new SortedSet<string>(StringComparer.Ordinal);
            Collect(definition.DurationMagnitude);
            if (definition.HasPeriod)
                Collect(definition.PeriodMagnitude);
            for (int i = 0; i < definition.Components.Length; i++)
            {
                switch (definition.Components[i])
                {
                    case PortableModifierComponent modifier:
                        Collect(modifier.Magnitude);
                        break;
                    case PortableAttributeRequirementsComponent requirement:
                        if (requirement.Source == PortableAttributeSource.SourceSnapshot && source == PortableMagnitudeSource.SourceAttributeSnapshot)
                            result.Add(requirement.AttributeId);
                        Collect(requirement.Threshold);
                        break;
                    case PortableExecutionComponent execution:
                        for (int mutationIndex = 0; mutationIndex < execution.Mutations.Length; mutationIndex++)
                            Collect(execution.Mutations[mutationIndex].Magnitude);
                        break;
                }
            }
            return result;

            void Collect(PortableMagnitude magnitude)
            {
                if (magnitude.Source == source)
                    result.Add(magnitude.AttributeId);
            }
        }

        SimulationGameplayEffectApplication BuildAdditionalApplication(
            PortableEffectSpecState parent,
            PortableAdditionalEffectsComponent component,
            int effectIndex)
        {
            PortableAdditionalEffect effect = component.Effects[effectIndex];
            PortableEffectDefinition definition = m_State.Program.RequireEffect(effect.EffectId);
            List<SimulationSetByCallerValue> values = m_Scratch.AdditionalSetByCallerValues.Acquire();
            List<SimulationAttributeCapture> attributes = m_Scratch.AdditionalSourceAttributes.Acquire();
            try
            {
                for (int bindingIndex = 0; bindingIndex < effect.Bindings.Length; bindingIndex++)
                {
                    PortableAdditionalParameterBinding binding = effect.Bindings[bindingIndex];
                    Float32Scalar value;
                    if (binding.Source == PortableAdditionalParameterSource.ParentSetByCaller)
                    {
                        if (!parent.SetByCaller.TryGetValue(binding.ParentParameterId, out value))
                            throw new InvalidOperationException($"Additional Effect parent parameter '{binding.ParentParameterId}' is unavailable.");
                    }
                    else
                    {
                        value = binding.Constant;
                    }
                    values.Add(new SimulationSetByCallerValue(binding.ChildParameterId, value));
                }
                foreach (KeyValuePair<string, Float32Scalar> pair in parent.SourceAttributes)
                    attributes.Add(new SimulationAttributeCapture(pair.Key, pair.Value));
                return new SimulationGameplayEffectApplication(
                    definition.Id,
                    definition.Revision,
                    parent.Context,
                    values,
                    attributes,
                    parent.SourceTags);
            }
            finally
            {
                m_Scratch.AdditionalSourceAttributes.Release(attributes);
                m_Scratch.AdditionalSetByCallerValues.Release(values);
            }
        }

        void CapturePredictionBefore(string attributeId)
        {
            if (m_CurrentPrediction == null)
                return;
            string id = SimulationGameplayEffectProgram.NormalizeAttribute(attributeId);
            if (m_CurrentPrediction.Attributes.ContainsKey(id))
                return;
            PortableAttributeState value = m_State.RequireAttribute(id);
            m_CurrentPrediction.Attributes.Add(id, new PortablePredictionAttributeSnapshot(id, value.BaseValue, value.CurrentValue, value.Revision, 0));
        }

        void AddLifecycle(PortableActiveEffectState active, SimulationGameplayEffectLifecycleOperation operation)
        {
            AddLifecycle(active.Spec.Definition, active.InstanceId, operation, active.Spec.Context, active.StartTick, active.EndTick, active.StackCount, active.LifecycleRevision, false);
        }

        void AddLifecycle(PortableEffectDefinition definition, ulong instanceId, SimulationGameplayEffectLifecycleOperation operation, SimulationGameplayEffectContext context, ulong startTick, ulong endTick, int stackCount, ulong revision, bool instant)
        {
            if (instanceId == 0 || revision == 0)
                return;
            AddChange(cursor => new PortableEffectLifecycleRuntimeChange(cursor, definition, instanceId, operation, context, startTick, endTick, stackCount, revision, instant));
        }

        void AddAttributeChanges(IReadOnlyList<PortableAttributeChange> changes)
        {
            for (int i = 0; i < changes.Count; i++)
            {
                PortableAttributeChange change = changes[i];
                PortableEffectCause cause = m_Causes.TryGetValue(change.CauseHandle, out PortableEffectCause value) ? value : default;
                AddChange(cursor => new PortableAttributeRuntimeChange(
                    cursor,
                    change,
                    cause.Definition?.Id,
                    cause.InstanceId,
                    cause.Context));
            }
        }

        void AddCue(string cueId, PortableCueTrigger trigger, PortableEffectDefinition definition, ulong instanceId, SimulationGameplayEffectContext context, bool trackPrediction = true)
        {
            AddChange(cursor => new PortableCueRuntimeChange(cursor, cueId, trigger, definition, instanceId, context));
            if (trackPrediction && m_CurrentPrediction != null && !m_CurrentPrediction.CueIds.Contains(cueId))
                m_CurrentPrediction.CueIds.Add(cueId);
        }

        void AddFailure(string ownerEffectId, ulong ownerInstanceId, string requestedEffectId, SimulationGameplayEffectApplyResultCode code, string reason)
        {
            AddChange(cursor => new PortableEffectFailureRuntimeChange(cursor, ownerEffectId, ownerInstanceId, requestedEffectId, code, reason));
        }

        void AddChange(Func<ulong, PortableEffectRuntimeChange> create)
        {
            m_State.ChangeCursor = checked(m_State.ChangeCursor + 1);
            if (m_State.ChangeCursor == 0)
                throw new OverflowException("Gameplay Effect ChangeSet cursor overflowed.");
            m_Changes.Add(create(m_State.ChangeCursor));
        }

        void TrimChanges(int count)
        {
            if (m_Changes.Count > count)
                m_Changes.RemoveRange(count, m_Changes.Count - count);
        }

        void RebuildCauses()
        {
            m_Causes.Clear();
            if (m_State != null)
            {
                foreach (PortableActiveEffectState active in m_State.ActiveEffects)
                    m_Causes.Add(active.Handle, new PortableEffectCause(active.Spec.Definition, active.InstanceId, active.Spec.Context));
                return;
            }
            m_CommittedState.CollectActiveEffectIdentities(m_Scratch.ActiveIdentities);
            foreach (GameplayEffectActiveIdentity active in m_Scratch.ActiveIdentities)
                m_Causes.Add(active.Handle, new PortableEffectCause(active.Definition, active.InstanceId, active.Context));
        }

    }
}
