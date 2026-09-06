using System;
using System.Collections.Generic;

namespace ThirdPersonSimulation
{
    internal readonly struct Float32ActionActivationRequestState
    {
		public Float32ActionActivationRequestState(
			string actionId,
			CharacterSkillId skillId,
			OperationHandle skillEntryOperation,
			string contextId,
            string sourceInputRequestId,
            ulong inputSequence,
            ulong startTick,
            string targetKey,
            SimulationActionTargetSnapshot targetSnapshot,
            SimulationExecutionSource source,
            EquipmentActionContext equipmentContext = default,
            ulong replacementActionInstanceId = 0)
        {
			ActionId = SimulationIdentity.Require(actionId, nameof(actionId));
			SkillId = skillId;
			SkillEntryOperation = skillEntryOperation;
            ContextId = SimulationIdentity.Require(contextId, nameof(contextId));
            SourceInputRequestId = sourceInputRequestId ?? string.Empty;
            if (inputSequence == 0 || startTick == 0 || !source.IsValid)
                throw new ArgumentException("Action activation request identity is incomplete.");
            InputSequence = inputSequence;
            StartTick = startTick;
            TargetKey = targetKey ?? string.Empty;
            TargetSnapshot = targetSnapshot;
            Source = source;
            EquipmentContext = equipmentContext;
            ReplacementActionInstanceId = replacementActionInstanceId;
        }

		public string ActionId { get; }
		public CharacterSkillId SkillId { get; }
		public OperationHandle SkillEntryOperation { get; }
        public string ContextId { get; }
        public string SourceInputRequestId { get; }
        public ulong InputSequence { get; }
        public ulong StartTick { get; }
        public string TargetKey { get; }
        public SimulationActionTargetSnapshot TargetSnapshot { get; }
        public SimulationExecutionSource Source { get; }
        public EquipmentActionContext EquipmentContext { get; }
        public ulong ReplacementActionInstanceId { get; }
        public bool IsValid =>
            !string.IsNullOrEmpty(ActionId) &&
			!string.IsNullOrEmpty(ContextId) &&
			InputSequence != 0 &&
			StartTick != 0 &&
			Source.IsValid &&
			(!Source.IsCharacterControl || SkillId.IsValid && SkillEntryOperation.IsValid);
    }

    internal readonly struct Float32ActionInstanceState
    {
		public Float32ActionInstanceState(
			string actionId,
			CharacterSkillId skillId,
			OperationHandle skillEntryOperation,
			ulong skillExecutionGeneration,
			string contextId,
            ulong instanceId,
            ulong predictionKey,
            string sourceInputRequestId,
            ulong inputSequence,
            ulong startTick,
            string targetKey,
            SimulationActionTargetSnapshot targetSnapshot,
            SimulationExecutionSource source,
            SimulationActionPhase phase,
            SimulationActionState state,
            SimulationActionLifecycleTransitionType lastTransition,
            ulong lastTransitionTick,
            ulong lastTransitionSourceTick,
            string reason,
            EquipmentActionContext equipmentContext = default)
        {
			ActionId = actionId ?? string.Empty;
			SkillId = skillId;
			SkillEntryOperation = skillEntryOperation;
			SkillExecutionGeneration = skillExecutionGeneration;
            ContextId = contextId ?? string.Empty;
            InstanceId = instanceId;
            PredictionKey = predictionKey;
            SourceInputRequestId = sourceInputRequestId ?? string.Empty;
            InputSequence = inputSequence;
            StartTick = startTick;
            TargetKey = targetKey ?? string.Empty;
            TargetSnapshot = targetSnapshot;
            Source = source;
            Phase = phase;
            State = state;
            LastTransition = lastTransition;
            LastTransitionTick = lastTransitionTick;
            LastTransitionSourceTick = lastTransitionSourceTick;
            Reason = reason ?? string.Empty;
            EquipmentContext = equipmentContext;
        }

		public string ActionId { get; }
		public CharacterSkillId SkillId { get; }
		public OperationHandle SkillEntryOperation { get; }
		public ulong SkillExecutionGeneration { get; }
        public string ContextId { get; }
        public ulong InstanceId { get; }
        public ulong PredictionKey { get; }
        public string SourceInputRequestId { get; }
        public ulong InputSequence { get; }
        public ulong StartTick { get; }
        public string TargetKey { get; }
        public SimulationActionTargetSnapshot TargetSnapshot { get; }
        public SimulationExecutionSource Source { get; }
        public SimulationActionPhase Phase { get; }
        public SimulationActionState State { get; }
        public SimulationActionLifecycleTransitionType LastTransition { get; }
        public ulong LastTransitionTick { get; }
        public ulong LastTransitionSourceTick { get; }
        public string Reason { get; }
        public EquipmentActionContext EquipmentContext { get; }
        public bool IsValid =>
            !string.IsNullOrEmpty(ActionId) &&
            !string.IsNullOrEmpty(ContextId) &&
            InstanceId != 0 &&
            PredictionKey != 0 &&
            InputSequence != 0 &&
			StartTick != 0 &&
			Source.IsValid &&
			(!Source.IsCharacterControl || SkillId.IsValid && SkillEntryOperation.IsValid);
        public bool IsTerminal =>
            State == SimulationActionState.Rejected ||
            State == SimulationActionState.Cancelled ||
            State == SimulationActionState.Interrupted ||
            State == SimulationActionState.Aborted ||
            State == SimulationActionState.Ended;
        public bool IsActive => IsValid && !IsTerminal;

        public Float32ActionInstanceState WithLifecycle(
            SimulationActionPhase phase,
            SimulationActionState state,
            SimulationActionLifecycleTransitionType transition,
            ulong transitionTick,
            ulong sourceTick,
            string reason)
        {
			return new Float32ActionInstanceState(
				ActionId,
				SkillId,
				SkillEntryOperation,
				SkillExecutionGeneration,
				ContextId,
                InstanceId,
                PredictionKey,
                SourceInputRequestId,
                InputSequence,
                StartTick,
                TargetKey,
                TargetSnapshot,
                Source,
                phase,
                state,
                transition,
                transitionTick,
                sourceTick,
                reason,
				EquipmentContext);
		}

		public Float32ActionInstanceState WithSkillExecution(OperationHandle entryOperation, ulong generation)
		{
			return new Float32ActionInstanceState(
				ActionId,
				SkillId,
				entryOperation,
				generation,
				ContextId,
				InstanceId,
				PredictionKey,
				SourceInputRequestId,
				InputSequence,
				StartTick,
				TargetKey,
				TargetSnapshot,
				Source,
				Phase,
				State,
				LastTransition,
				LastTransitionTick,
				LastTransitionSourceTick,
				Reason,
				EquipmentContext);
		}
    }

    internal readonly struct Float32ActionInstanceReference
    {
        public Float32ActionInstanceReference(
            string actionId,
            string contextId,
            ulong instanceId,
            ulong predictionKey,
            CharacterSkillId skillId = default,
            OperationHandle skillEntryOperation = default,
            ulong skillExecutionGeneration = 0)
        {
            if (skillId.IsValid != skillEntryOperation.IsValid || !skillId.IsValid && skillExecutionGeneration != 0)
                throw new ArgumentException("Action instance skill execution identity is incomplete.");
            ActionId = actionId ?? string.Empty;
            ContextId = contextId ?? string.Empty;
            InstanceId = instanceId;
            PredictionKey = predictionKey;
            SkillId = skillId;
            SkillEntryOperation = skillEntryOperation;
            SkillExecutionGeneration = skillExecutionGeneration;
        }

        public string ActionId { get; }
        public string ContextId { get; }
        public ulong InstanceId { get; }
        public ulong PredictionKey { get; }
        public CharacterSkillId SkillId { get; }
        public OperationHandle SkillEntryOperation { get; }
        public ulong SkillExecutionGeneration { get; }
        public bool HasSkillExecution => SkillId.IsValid && SkillEntryOperation.IsValid;
        public bool HasAnyIdentity =>
            !string.IsNullOrEmpty(ActionId) ||
            !string.IsNullOrEmpty(ContextId) ||
            InstanceId != 0 ||
            PredictionKey != 0 ||
            SkillId.IsValid ||
            SkillEntryOperation.IsValid ||
            SkillExecutionGeneration != 0;
        public bool IsTransientValid =>
            !string.IsNullOrEmpty(ActionId) &&
            !string.IsNullOrEmpty(ContextId) &&
            InstanceId != 0 &&
            PredictionKey != 0 &&
            (!HasSkillExecution && SkillExecutionGeneration == 0 || HasSkillExecution);
        public bool IsValid =>
            IsTransientValid &&
            (!HasSkillExecution || SkillExecutionGeneration != 0);

        public bool MatchesSkillExecution(Float32ActionInstanceState state) =>
            IsValid &&
            state.IsValid &&
            state.SkillId == SkillId &&
            state.SkillEntryOperation.Equals(SkillEntryOperation) &&
            (!HasSkillExecution || state.SkillExecutionGeneration == SkillExecutionGeneration);

        public bool MatchesTransientSkillExecution(Float32ActionInstanceState state) =>
            IsTransientValid &&
            state.IsValid &&
            state.SkillId == SkillId &&
            state.SkillEntryOperation.Equals(SkillEntryOperation) &&
            (SkillExecutionGeneration == 0 || state.SkillExecutionGeneration == SkillExecutionGeneration);

        public static Float32ActionInstanceReference FromInstance(Float32ActionInstanceState state)
        {
            return state.IsValid
                ? new Float32ActionInstanceReference(
                    state.ActionId,
                    state.ContextId,
                    state.InstanceId,
                    state.PredictionKey,
                    state.SkillId,
                    state.SkillEntryOperation,
                    state.SkillExecutionGeneration)
                : default;
        }
    }

	internal sealed class Float32ActionStateStore : Float32OperationModule, IFloat32ActionContextReader, IFloat32SkillExecutionStateAccess, IActionSkillExecutionStorage<CharacterStateValue>
	{
		readonly Float32StatePort m_State;
		readonly Stack<Float32ActionInstanceReference> m_SkillExecutionStack = new Stack<Float32ActionInstanceReference>();
		readonly ActionSkillExecutionManager<CharacterStateValue> m_SkillExecution;

        public Float32ActionStateStore(
            Float32ProgramAccess access,
            Float32StatePort state,
            Float32EvaluationFrame frame)
            : base(access)
		{
			m_State = state ?? throw new ArgumentNullException(nameof(state));
			m_SkillExecution = new ActionSkillExecutionManager<CharacterStateValue>(this);
			(frame ?? throw new ArgumentNullException(nameof(frame)))
				.BindSkillExecutionStateAccess(this);
		}

		public void BeginEvaluation()
		{
			if (m_SkillExecutionStack.Count != 0)
				throw new InvalidOperationException("Skill execution stack retained transient state across evaluations.");
			m_SkillExecution.BeginEvaluation();
		}

		public void EndEvaluation()
		{
			if (m_SkillExecutionStack.Count != 0)
				throw new InvalidOperationException("Skill execution stack has an unclosed runtime scope.");
			m_SkillExecution.EndEvaluation();
		}

		public IDisposable EnterSkillExecution(Float32ActionInstanceState action)
		{
			if (!action.IsValid || !action.SkillId.IsValid || !action.SkillEntryOperation.IsValid)
				throw new ArgumentException("Skill execution action identity is incomplete.", nameof(action));
			return m_SkillExecution.Enter(new ActionSkillExecutionIdentity(
				action.SkillId,
				action.SkillEntryOperation,
				action.InstanceId,
				action.PredictionKey,
				action.SkillExecutionGeneration));
		}

		public bool RemoveSkillExecution(ulong actionInstanceId)
		{
			return m_SkillExecution.Remove(actionInstanceId);
		}

		public bool IsContextActive(string contextId) => FindActive(contextId, out _) >= 0;

		public bool IsSkillActive(CharacterSkillId skillId) => FindActive(skillId, out _) >= 0;

		public bool IsSkillExecutionActive(ulong actionInstanceId) =>
			m_SkillExecution.IsActive(actionInstanceId);

		public bool IsSkillCompleted(CharacterSkillId skillId) => CompletedSkillInstanceId(skillId) != 0;

		public ulong CompletedSkillInstanceId(CharacterSkillId skillId)
		{
			ulong result = 0;
			foreach (TypedActionStateAddresses addresses in m_Layout.ActionStateIndex.Values)
			{
				Float32ActionInstanceState action = m_State.Get(addresses.Instance.SlotIndex).ActionInstance;
				if (action.SkillId != skillId || action.State != SimulationActionState.Ended)
					continue;
				if (result != 0)
					throw new InvalidOperationException($"Skill '{skillId}' resolves multiple completed Action instances.");
				result = action.InstanceId;
			}
			return result;
		}

		public int FindActive(CharacterSkillId skillId, out Float32ActionInstanceState state)
		{
			int found = -1;
			state = default;
			foreach (TypedActionStateAddresses addresses in m_Layout.ActionStateIndex.Values)
				MatchActive(addresses.Instance, skillId, ref found, ref state);
			return found;
		}

		public bool TryGetActiveSkillInstanceId(CharacterSkillId skillId, out ulong instanceId)
		{
			if (FindActive(skillId, out Float32ActionInstanceState state) < 0)
			{
				instanceId = 0;
				return false;
			}
			instanceId = state.InstanceId;
			return true;
		}

		public int FindCurrent(CharacterSkillId skillId, out Float32ActionInstanceState state)
		{
			int found = -1;
			state = default;
			foreach (TypedActionStateAddresses addresses in m_Layout.ActionStateIndex.Values)
			{
				Float32ActionInstanceState candidate = m_State.Get(addresses.Instance.SlotIndex).ActionInstance;
				if (!candidate.IsValid || candidate.SkillId != skillId)
					continue;
				if (found >= 0)
					throw new InvalidOperationException($"Skill '{skillId}' resolves multiple Action instances.");
				found = addresses.Instance.SlotIndex;
				state = candidate;
			}
			return found;
		}

		public Float32ActionInstanceState BindSkillExecution(
			Float32ActionInstanceState action,
			OperationHandle entryOperation,
			ulong generation)
		{
			if (!action.IsActive || !action.SkillId.IsValid || !entryOperation.IsValid || generation == 0)
				throw new ArgumentException("Skill execution binding is incomplete.", nameof(action));
			if (action.SkillEntryOperation.IsValid && !action.SkillEntryOperation.Equals(entryOperation))
				throw new InvalidOperationException($"Skill '{action.SkillId}' changed EntryOperation for Action instance '{action.InstanceId}'.");
			if (action.SkillExecutionGeneration != 0 && action.SkillExecutionGeneration != generation)
				throw new InvalidOperationException($"Skill '{action.SkillId}' changed execution generation for Action instance '{action.InstanceId}'.");
			m_SkillExecution.BindGeneration(action.InstanceId, generation);
			Float32ActionInstanceState next = action.WithSkillExecution(entryOperation, generation);
			WriteState(next);
			return next;
		}

		public IDisposable PushSkillExecution(Float32ActionInstanceState action)
		{
			if (!action.IsActive || !action.SkillId.IsValid || !action.SkillEntryOperation.IsValid)
				throw new ArgumentException("Skill execution owner is incomplete.", nameof(action));
			Float32ActionInstanceReference reference = Float32ActionInstanceReference.FromInstance(action);
			m_SkillExecutionStack.Push(reference);
			return new SkillExecutionScope(this, reference);
		}

		public bool TryGetCurrentSkillExecution(out Float32ActionInstanceState action)
		{
			if (m_SkillExecutionStack.Count == 0)
			{
				action = default;
				return false;
			}
            action = RequireActiveTransient(m_SkillExecutionStack.Peek());
            return action.IsActive;
        }

        public int FindActive(string contextId, out Float32ActionInstanceState state)
        {
            int found = -1;
            state = default;
            if (!string.IsNullOrEmpty(contextId))
            {
                IReadOnlyList<TypedStateAddress> addresses = m_Layout.ActionInstances(contextId);
                for (int i = 0; i < addresses.Count; i++)
                    MatchActive(addresses[i], contextId, ref found, ref state);
            }
            else
            {
                foreach (TypedActionStateAddresses addresses in m_Layout.ActionStateIndex.Values)
                    MatchActive(addresses.Instance, string.Empty, ref found, ref state);
            }
            return found;
        }

        public Float32ActionInstanceState FindOnlyActive()
        {
            Float32ActionInstanceState result = default;
            foreach (TypedActionStateAddresses addresses in m_Layout.ActionStateIndex.Values)
            {
                Float32ActionInstanceState current = m_State.Get(addresses.Instance.SlotIndex).ActionInstance;
                if (!current.IsActive)
                    continue;
                if (result.IsActive)
                    throw new InvalidOperationException("Action Context is ambiguous because multiple actions are active.");
                result = current;
            }
            return result;
        }

        public Float32ActionInstanceState RequireActive(Float32ActionInstanceState expected)
        {
            if (!expected.IsActive)
                return default;
            int slot = FindActive(expected.ContextId, out Float32ActionInstanceState current);
            return slot >= 0 && current.InstanceId == expected.InstanceId ? current : default;
        }

		public Float32ActionInstanceState RequireActive(Float32ActionInstanceReference reference)
        {
            if (!reference.IsValid)
                return default;
            TypedActionStateAddresses addresses = m_Layout.RequireAction(reference.ActionId);
            Float32ActionInstanceState current = m_State.Get(addresses.Instance.SlotIndex).ActionInstance;
			return current.IsActive &&
                   string.Equals(current.ContextId, reference.ContextId, StringComparison.Ordinal) &&
                   current.InstanceId == reference.InstanceId &&
                   current.PredictionKey == reference.PredictionKey &&
                   reference.MatchesSkillExecution(current)
                ? current
				: default;
		}

		bool IFloat32SkillExecutionStateAccess.TryGet(int slotIndex, out CharacterStateValue value)
		{
			return m_SkillExecution.TryGet(slotIndex, out value);
		}

		bool IFloat32SkillExecutionStateAccess.TrySet(int slotIndex, CharacterStateValue value)
		{
			return m_SkillExecution.TrySet(slotIndex, value);
		}

		bool IFloat32SkillExecutionStateAccess.TryReset(int slotIndex)
		{
			return m_SkillExecution.TryReset(slotIndex);
		}

        public Float32ActionInstanceState RequireActiveTransient(Float32ActionInstanceReference reference)
        {
            if (!reference.IsTransientValid)
                return default;
            TypedActionStateAddresses addresses = m_Layout.RequireAction(reference.ActionId);
            Float32ActionInstanceState current = m_State.Get(addresses.Instance.SlotIndex).ActionInstance;
            return current.IsActive &&
                   string.Equals(current.ContextId, reference.ContextId, StringComparison.Ordinal) &&
                   current.InstanceId == reference.InstanceId &&
                   current.PredictionKey == reference.PredictionKey &&
                   reference.MatchesTransientSkillExecution(current)
                ? current
                : default;
        }

        public bool ContainsInstance(ulong instanceId)
        {
            foreach (TypedActionStateAddresses addresses in m_Layout.ActionStateIndex.Values)
            {
                Float32ActionInstanceState current = m_State.Get(addresses.Instance.SlotIndex).ActionInstance;
                if (current.InstanceId == instanceId)
                    return true;
            }
            return false;
        }

        public bool TryGetInstance(ulong instanceId, out Float32ActionInstanceState state)
        {
            foreach (TypedActionStateAddresses addresses in m_Layout.ActionStateIndex.Values)
            {
                Float32ActionInstanceState current = m_State.Get(addresses.Instance.SlotIndex).ActionInstance;
                if (current.InstanceId != instanceId)
                    continue;
                state = current;
                return true;
            }
            state = default;
            return false;
        }

        public void WriteState(Float32ActionInstanceState action)
        {
            TypedActionStateAddresses addresses = m_Layout.RequireAction(action.ActionId);
            m_State.Set(addresses.Instance.SlotIndex, CharacterStateValue.FromActionInstance(action));
        }

        public int RequireSlot(string actionId, ProgramStateSemantic semantic)
        {
            TypedActionStateAddresses addresses = m_Layout.RequireAction(actionId);
            return semantic switch
            {
                ProgramStateSemantic.ActionRequestBuffer => addresses.Request.SlotIndex,
                ProgramStateSemantic.ActionInstance => addresses.Instance.SlotIndex,
                ProgramStateSemantic.ActionEventSequence => addresses.EventSequence.SlotIndex,
                _ => throw new InvalidOperationException($"Action '{actionId}' has no typed '{semantic}' state.")
            };
        }

        public void WriteRequest(int slot, Float32ActionActivationRequestState state) =>
            m_State.Set(slot, CharacterStateValue.FromActionActivationRequest(state));

		public Float32ActionActivationRequestState ReadRequest(int slot) =>
			m_State.Get(slot).ActionActivationRequest;

		public int FindPendingSkill(
			CharacterSkillId skillId,
			out Float32ActionActivationRequestState request)
		{
			int found = -1;
			request = default;
			foreach (TypedActionStateAddresses addresses in m_Layout.ActionStateIndex.Values)
			{
				Float32ActionActivationRequestState candidate = m_State.Get(addresses.Request.SlotIndex).ActionActivationRequest;
				if (!candidate.IsValid || !candidate.Source.IsCharacterControl || candidate.SkillId != skillId)
					continue;
				if (found >= 0)
					throw new InvalidOperationException($"Skill '{skillId}' has multiple pending Action activation requests.");
				found = addresses.Request.SlotIndex;
				request = candidate;
			}
			return found;
		}

		public void ClearRequest(int slot) => m_State.Set(slot, CharacterStateValue.FromActionActivationRequest(default));

        public Float32ActionInstanceState ReadSlot(int slot) => m_State.Get(slot).ActionInstance;

		public ulong NextSequence()
		{
			int slot = m_Layout.RequireStateSlot(ProgramStateSemantic.ActionEventSequence);
			ulong value = checked(m_State.Get(slot).UInt64 + 1);
            if (value == 0)
                throw new OverflowException("Action sequence overflowed.");
            m_State.Set(slot, CharacterStateValue.FromUInt64(value));
			return value;
		}

		bool IActionSkillExecutionStorage<CharacterStateValue>.IsSkillStateSlot(int slotIndex) =>
			m_Layout.IsSkillExecutionStateSlot(slotIndex);

		bool IActionSkillExecutionStorage<CharacterStateValue>.IsValueValid(
			int slotIndex,
			CharacterStateValue value) =>
			value.Kind == m_Program.StateSlots[slotIndex].ValueKind;

		CharacterStateValue IActionSkillExecutionStorage<CharacterStateValue>.DefaultValue(int slotIndex)
		{
			ProgramStateSlot slot = m_Program.StateSlots[slotIndex];
			return slot.DefaultConstantIndex >= 0
				? CharacterStateValue.FromConstant(
					m_Program.Constants[slot.DefaultConstantIndex],
					slot.ValueKind)
				: CharacterStateValue.Default(slot.ValueKind);
		}

		ActionSkillExecutionAggregate<CharacterStateValue> IActionSkillExecutionStorage<CharacterStateValue>.ReadAggregate() =>
			m_State.Get(m_Layout.SkillExecutionStateAddress.SlotIndex).SkillExecutionState;

		void IActionSkillExecutionStorage<CharacterStateValue>.WriteAggregate(
			ActionSkillExecutionAggregate<CharacterStateValue> aggregate) =>
			m_State.Set(
				m_Layout.SkillExecutionStateAddress.SlotIndex,
				CharacterStateValue.FromSkillExecutionState(aggregate));

		void MatchActive(
			TypedStateAddress address,
			string contextId,
            ref int found,
            ref Float32ActionInstanceState state)
        {
            Float32ActionInstanceState candidate = m_State.Get(address.SlotIndex).ActionInstance;
            if (!candidate.IsActive ||
                !string.IsNullOrEmpty(contextId) && !string.Equals(candidate.ContextId, contextId, StringComparison.Ordinal))
                return;
            if (found >= 0)
                throw new InvalidOperationException($"Action Context '{contextId}' resolves multiple active Action instances.");
			found = address.SlotIndex;
			state = candidate;
		}

		void MatchActive(
			TypedStateAddress address,
			CharacterSkillId skillId,
			ref int found,
			ref Float32ActionInstanceState state)
		{
			Float32ActionInstanceState candidate = m_State.Get(address.SlotIndex).ActionInstance;
			if (!candidate.IsActive || candidate.SkillId != skillId)
				return;
			if (found >= 0)
				throw new InvalidOperationException($"Skill '{skillId}' resolves multiple active Action instances.");
			found = address.SlotIndex;
			state = candidate;
		}

		void PopSkillExecution(Float32ActionInstanceReference expected)
		{
			if (m_SkillExecutionStack.Count == 0 || !m_SkillExecutionStack.Pop().Equals(expected))
				throw new InvalidOperationException("Skill execution scope is unbalanced.");
		}

		sealed class SkillExecutionScope : IDisposable
		{
			readonly Float32ActionStateStore m_Owner;
			readonly Float32ActionInstanceReference m_Expected;
			bool m_Disposed;

			public SkillExecutionScope(Float32ActionStateStore owner, Float32ActionInstanceReference expected)
			{
				m_Owner = owner;
				m_Expected = expected;
			}

			public void Dispose()
			{
				if (m_Disposed)
					return;
				m_Disposed = true;
				m_Owner.PopSkillExecution(m_Expected);
			}
		}

	}
}
