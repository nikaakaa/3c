using System;
using System.Collections.Generic;
using SimulationActionActivationRequestState = ThirdPersonSimulation.SimulationActionActivationRequestState<ThirdPersonSimulation.SimulationActionTargetSnapshot>;

namespace ThirdPersonSimulation
{
	internal readonly struct Float32ActionInstanceState : IEquatable<Float32ActionInstanceState>
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
            EquipmentActionContext equipmentContext = default,
            ulong segmentGeneration = 0,
            string activationEntryId = "")
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
            SegmentGeneration = segmentGeneration;
            ActivationEntryId = activationEntryId ?? string.Empty;
        }

		public string ActionId { get; }
		public CharacterSkillId SkillId { get; }
		public OperationHandle SkillEntryOperation { get; }
		public ulong SkillExecutionGeneration { get; }
		public ulong SegmentGeneration { get; }
        public string ActivationEntryId { get; }
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
		public bool Equals(Float32ActionInstanceState other) =>
			string.Equals(ActionId, other.ActionId, StringComparison.Ordinal) &&
			SkillId.Equals(other.SkillId) &&
			SkillEntryOperation.Equals(other.SkillEntryOperation) &&
			SkillExecutionGeneration == other.SkillExecutionGeneration &&
			SegmentGeneration == other.SegmentGeneration &&
            string.Equals(ActivationEntryId, other.ActivationEntryId, StringComparison.Ordinal) &&
			string.Equals(ContextId, other.ContextId, StringComparison.Ordinal) &&
			InstanceId == other.InstanceId &&
			PredictionKey == other.PredictionKey &&
			string.Equals(SourceInputRequestId, other.SourceInputRequestId, StringComparison.Ordinal) &&
			InputSequence == other.InputSequence &&
			StartTick == other.StartTick &&
			string.Equals(TargetKey, other.TargetKey, StringComparison.Ordinal) &&
			TargetSnapshot.Equals(other.TargetSnapshot) &&
			Source.Equals(other.Source) &&
			Phase == other.Phase &&
			State == other.State &&
			LastTransition == other.LastTransition &&
			LastTransitionTick == other.LastTransitionTick &&
			LastTransitionSourceTick == other.LastTransitionSourceTick &&
			string.Equals(Reason, other.Reason, StringComparison.Ordinal) &&
			EquipmentContext.Equals(other.EquipmentContext);
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
				EquipmentContext,
            SegmentGeneration, ActivationEntryId);
		}

		public Float32ActionInstanceState WithSegmentGeneration(ulong segmentGeneration)
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
				Phase,
				State,
				LastTransition,
				LastTransitionTick,
				LastTransitionSourceTick,
				Reason,
				EquipmentContext,
				segmentGeneration, ActivationEntryId);
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
				EquipmentContext,
            SegmentGeneration, ActivationEntryId);
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
        public bool Equals(Float32ActionInstanceReference other) =>
            string.Equals(ActionId, other.ActionId, StringComparison.Ordinal) &&
            string.Equals(ContextId, other.ContextId, StringComparison.Ordinal) &&
            InstanceId == other.InstanceId &&
            PredictionKey == other.PredictionKey &&
            SkillId.Equals(other.SkillId) &&
            SkillEntryOperation.Equals(other.SkillEntryOperation) &&
            SkillExecutionGeneration == other.SkillExecutionGeneration;
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

	internal sealed class Float32ActionStateStore : Float32OperationModule, IFloat32ActionContextReader, IFloat32SkillExecutionStateAccess, IGameplayAbilityExecutionStorage<AbilityStateValue>
	{
		static readonly Comparison<Float32ActionInstanceState> s_InstanceComparison =
			(left, right) => left.InstanceId.CompareTo(right.InstanceId);

		readonly Float32AbilityExecutionFrame m_Frame;
		readonly Stack<Float32ActionInstanceReference> m_SkillExecutionStack = new Stack<Float32ActionInstanceReference>();
		readonly Stack<SkillExecutionScope> m_SkillExecutionScopePool = new();
        readonly List<Float32ActionInstanceState> m_EvaluatedActions = new(8);
        readonly Dictionary<ulong, int> m_EvaluatedActionIndexes = new();
		readonly GameplayAbilityExecutionManager<AbilityStateValue> m_SkillExecution;
		readonly TraceExecutionScope m_TraceExecutionScope = new();

        public Float32ActionStateStore(
            Float32GameplayAbilityExecutionAccess access,
            Float32AbilityExecutionFrame frame)
            : base(access)
		{
			m_Frame = frame ?? throw new ArgumentNullException(nameof(frame));
			m_SkillExecution = new GameplayAbilityExecutionManager<AbilityStateValue>(this);
			m_Frame.BindSkillExecutionStateAccess(this);
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
            m_EvaluatedActions.Clear();
            m_EvaluatedActionIndexes.Clear();
		}

		public IDisposable EnterSkillExecution(Float32ActionInstanceState action)
		{
			if (!action.IsValid || !action.SkillId.IsValid || !action.SkillEntryOperation.IsValid)
				throw new ArgumentException("Skill execution action identity is incomplete.", nameof(action));
            RetainEvaluatedAction(action);
			IDisposable execution = m_SkillExecution.Enter(new AbilityExecutionContext(
				action.SkillId,
				action.SkillEntryOperation,
				action.InstanceId,
				action.PredictionKey,
				action.SkillExecutionGeneration));
			return m_TraceExecutionScope.Begin(
				execution,
				m_Frame.PushActionTraceContext(action.InstanceId, action.SkillId, action.SkillEntryOperation));
		}

		public bool RemoveSkillExecution(ulong actionInstanceId)
		{
			return m_SkillExecution.Remove(actionInstanceId);
		}

		public bool IsContextActive(string contextId)
		{
			foreach (Float32ActionInstanceState action in m_Frame.ActionState.GetActionInstances())
			{
				if (action.IsActive && string.Equals(action.ContextId, contextId ?? string.Empty, StringComparison.Ordinal))
					return true;
			}
			return false;
		}

        public bool IsActivationEntry(string entryId) =>
            TryGetCurrentSkillExecution(out Float32ActionInstanceState action) &&
            string.Equals(action.ActivationEntryId, entryId, StringComparison.Ordinal);

        public void RequireActivationEntry(string entryId)
        {
            if (string.IsNullOrEmpty(entryId))
                return;
            for (int i = 0; i < m_Ability.Operations.Count; i++)
            {
                var operation = m_Ability.Operations[i];
                if (operation.Code == SimulationOperationCode.ActivationEntry &&
                    string.Equals(operation.Text0, entryId, StringComparison.Ordinal))
                    return;
            }
            throw new InvalidOperationException($"Ability has no activation entry '{entryId}'.");
        }
        public bool IsCurrentExecutionContextActive()
        {
            return TryGetCurrentSkillExecution(out Float32ActionInstanceState action) && action.IsActive;
        }

		public bool IsAbilityActive(CharacterSkillId abilityId)
		{
			foreach (Float32ActionInstanceState action in m_Frame.ActionState.GetActionInstances())
			{
				if (action.IsActive && action.SkillId == abilityId)
					return true;
			}
			return false;
		}

		public bool IsSkillExecutionActive(ulong actionInstanceId) =>
			m_SkillExecution.IsActive(actionInstanceId);

		public bool HasSkillExecutionFrame(ulong actionInstanceId) =>
			m_SkillExecution.HasFrame(actionInstanceId);

		public bool IsAbilityCompleted(CharacterSkillId abilityId) => CompletedAbilityInstanceId(abilityId) != 0;

		public ulong CompletedAbilityInstanceId(CharacterSkillId abilityId)
		{
			ulong result = 0;
			ulong resultTick = 0;
			foreach (Float32ActionInstanceState action in m_Frame.ActionState.GetActionInstances())
			{
				if (action.SkillId != abilityId || action.State != SimulationActionState.Ended)
					continue;
				if (result == 0 ||
					action.LastTransitionTick > resultTick ||
					action.LastTransitionTick == resultTick && action.InstanceId > result)
				{
					result = action.InstanceId;
					resultTick = action.LastTransitionTick;
				}
			}
			return result;
		}

		public int FindActive(CharacterSkillId skillId, out Float32ActionInstanceState state)
		{
			int found = -1;
			state = default;
			IReadOnlyList<Float32ActionInstanceState> actions = m_Frame.ActionState.GetActionInstances();
			for (int i = 0; i < actions.Count; i++)
				MatchActive(i, skillId, ref found, ref state);
			return found;
		}

		public bool TryGetActiveAbilityInstanceId(CharacterSkillId abilityId, out ulong instanceId)
		{
			ulong found = 0;
			foreach (Float32ActionInstanceState state in m_Frame.ActionState.GetActionInstances())
			{
				if (!state.IsActive || state.SkillId != abilityId)
					continue;
				if (found != 0)
				{
					instanceId = 0;
					return false;
				}
				found = state.InstanceId;
			}
			instanceId = found;
			return found != 0;
		}

		public int FindCurrent(CharacterSkillId skillId, out Float32ActionInstanceState state)
		{
			int found = -1;
			state = default;
			IReadOnlyList<Float32ActionInstanceState> actions = m_Frame.ActionState.GetActionInstances();
			for (int i = 0; i < actions.Count; i++)
			{
				Float32ActionInstanceState candidate = actions[i];
				if (!candidate.IsValid || candidate.SkillId != skillId)
					continue;
				if (found >= 0)
					throw new InvalidOperationException($"Skill '{skillId}' resolves multiple Action instances.");
				found = i;
				state = candidate;
			}
			return found;
		}

		public void CopyCurrentActions(CharacterSkillId skillId, List<Float32ActionInstanceState> results)
		{
			if (results == null)
				throw new ArgumentNullException(nameof(results));
			foreach (Float32ActionInstanceState candidate in m_Frame.ActionState.GetActionInstances())
			{
				if (candidate.IsValid && candidate.SkillId == skillId)
					results.Add(candidate);
			}
			results.Sort(s_InstanceComparison);
		}

		public IReadOnlyList<Float32ActionInstanceState> ActionInstances => m_Frame.ActionState.GetActionInstances();

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
            RetainEvaluatedAction(next);
			return next;
		}

		public IDisposable PushSkillExecution(Float32ActionInstanceState action)
		{
			if (!action.IsActive || !action.SkillId.IsValid || !action.SkillEntryOperation.IsValid)
				throw new ArgumentException("Skill execution owner is incomplete.", nameof(action));
			Float32ActionInstanceReference reference = Float32ActionInstanceReference.FromInstance(action);
			m_SkillExecutionStack.Push(reference);
			SkillExecutionScope scope = m_SkillExecutionScopePool.Count > 0
				? m_SkillExecutionScopePool.Pop()
				: new SkillExecutionScope();
			return scope.Begin(
				this,
				reference,
				m_Frame.PushActionTraceContext(action.InstanceId, action.SkillId, action.SkillEntryOperation));
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
            IReadOnlyList<Float32ActionInstanceState> actions = m_Frame.ActionState.GetActionInstances();
            if (!string.IsNullOrEmpty(contextId))
            {
                for (int i = 0; i < actions.Count; i++)
                    MatchActive(i, contextId, ref found, ref state);
            }
            else
            {
                for (int i = 0; i < actions.Count; i++)
                    MatchActive(i, string.Empty, ref found, ref state);
            }
            return found;
        }

        public Float32ActionInstanceState FindOnlyActive()
        {
            Float32ActionInstanceState result = default;
            foreach (Float32ActionInstanceState current in m_Frame.ActionState.GetActionInstances())
            {
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
			if (!TryGetInstance(reference.InstanceId, out Float32ActionInstanceState current))
				return default;
			return current.IsActive &&
				   string.Equals(current.ActionId, reference.ActionId, StringComparison.Ordinal) &&
                   string.Equals(current.ContextId, reference.ContextId, StringComparison.Ordinal) &&
                   current.PredictionKey == reference.PredictionKey &&
                   reference.MatchesSkillExecution(current)
				? current
				: default;
		}

		bool IFloat32SkillExecutionStateAccess.TryGet(int slotIndex, out AbilityStateValue value)
		{
			return m_SkillExecution.TryGet(slotIndex, out value);
		}

		bool IFloat32SkillExecutionStateAccess.TrySet(int slotIndex, AbilityStateValue value)
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
			if (!TryGetInstance(reference.InstanceId, out Float32ActionInstanceState current))
				return default;
            return current.IsActive &&
                   string.Equals(current.ActionId, reference.ActionId, StringComparison.Ordinal) &&
                   string.Equals(current.ContextId, reference.ContextId, StringComparison.Ordinal) &&
                   current.PredictionKey == reference.PredictionKey &&
                   reference.MatchesTransientSkillExecution(current)
                ? current
                : default;
        }

		public bool ContainsInstance(ulong instanceId)
		{
			return m_Frame.ActionState.TryGetActionInstance(instanceId, out _);
		}

        public bool TryGetEvaluatedInstance(ulong instanceId, out Float32ActionInstanceState state)
        {
            if (m_EvaluatedActionIndexes.TryGetValue(instanceId, out int index))
            {
                state = m_EvaluatedActions[index];
                return true;
            }
            state = default;
            return false;
        }

        // Timeline motion resolves after lifecycle changes and may outlive the action's state slot.
        void RetainEvaluatedAction(Float32ActionInstanceState action)
        {
            if (m_EvaluatedActionIndexes.TryGetValue(action.InstanceId, out int index))
            {
                m_EvaluatedActions[index] = action;
                return;
            }
            m_EvaluatedActions.Add(action);
            m_EvaluatedActionIndexes.Add(action.InstanceId, m_EvaluatedActions.Count - 1);
        }

		public bool TryGetInstance(ulong instanceId, out Float32ActionInstanceState state)
		{
			return m_Frame.ActionState.TryGetActionInstance(instanceId, out state);
		}

        public void WriteState(Float32ActionInstanceState action)
        {
            if (TryFindInstanceIndex(action.InstanceId, out int existingIndex))
            {
                ReplaceAction(existingIndex, action);
                return;
            }
			int index = FindPendingRequestInstanceIndex(
				action.ActionId,
				action.SkillId,
				action.SkillEntryOperation,
				action.ContextId,
				action.InputSequence,
				action.StartTick,
				ulong.MaxValue);
			if (index < 0)
				index = FindEmptyInstanceIndex(action.ActionId);
			if (index < 0)
				throw new InvalidOperationException($"Action '{action.ActionId}' has no free instance state capacity.");
            IReadOnlyList<Float32ActionInstanceState> actions = m_Frame.ActionState.GetActionInstances();
			if (index < actions.Count)
			{
				Float32ActionInstanceState previous = actions[index];
				if (previous.IsValid && previous.IsTerminal)
					m_SkillExecution.Remove(previous.InstanceId);
				m_Frame.ActionState.ReplaceActionInstanceAt(index, action);
			}
			else
				m_Frame.ActionState.AddActionInstance(action);
        }

		void ReplaceAction(int index, Float32ActionInstanceState action)
		{
			m_Frame.ActionState.ReplaceActionInstanceAt(index, action);
		}

		public bool HasPendingRequest(string actionId)
		{
			foreach (SimulationActionActivationRequestState request in m_Frame.ActionState.GetActionActivationRequests())
				if (request.IsValid && string.Equals(request.ActionId, actionId, StringComparison.Ordinal))
					return true;
			return false;
		}

		public void StageRequest(SimulationActionActivationRequestState state)
		{
			IReadOnlyList<SimulationActionActivationRequestState> current = m_Frame.ActionState.GetActionActivationRequests();
			int count = 0;
			for (int i = 0; i < current.Count; i++)
				if (string.Equals(current[i].ActionId, state.ActionId, StringComparison.Ordinal))
					count++;
			if (count >= m_Layout.ActionCapacity(state.ActionId))
				throw new InvalidOperationException($"Action '{state.ActionId}' has no free activation request capacity.");
            m_Frame.ActionState.AddActivationRequest(state);
        }

		public int FindPendingSkill(
			CharacterSkillId skillId,
			out SimulationActionActivationRequestState request)
		{
			int found = -1;
			request = default;
			IReadOnlyList<SimulationActionActivationRequestState> requests = m_Frame.ActionState.GetActionActivationRequests();
			for (int i = 0; i < requests.Count; i++)
			{
				SimulationActionActivationRequestState candidate = requests[i];
				if (!candidate.IsValid || !candidate.Source.IsCharacterControl || candidate.SkillId != skillId)
					continue;
				if (found >= 0)
					throw new InvalidOperationException($"Skill '{skillId}' has multiple pending Action activation requests.");
				found = i;
				request = candidate;
			}
			return found;
		}

		public int FindPendingRequest(
			string actionId,
			CharacterSkillId skillId,
			OperationHandle entryOperation,
			string contextId,
			ulong inputSequence,
			ulong startTick,
			ulong replacementActionInstanceId)
		{
			IReadOnlyList<SimulationActionActivationRequestState> requests = m_Frame.ActionState.GetActionActivationRequests();
			for (int i = 0; i < requests.Count; i++)
			{
				SimulationActionActivationRequestState candidate = requests[i];
				if (candidate.IsValid && candidate.SkillId == skillId &&
					string.Equals(candidate.ActionId, actionId, StringComparison.Ordinal) &&
					candidate.SkillEntryOperation.Equals(entryOperation) &&
					string.Equals(candidate.ContextId, contextId, StringComparison.Ordinal) &&
					candidate.InputSequence == inputSequence &&
					candidate.StartTick == startTick &&
					(replacementActionInstanceId == ulong.MaxValue ||
					 candidate.ReplacementActionInstanceId == replacementActionInstanceId))
					return i;
			}
			return -1;
		}

		int FindPendingRequestInstanceIndex(
			string actionId,
			CharacterSkillId skillId,
			OperationHandle entryOperation,
			string contextId,
			ulong inputSequence,
			ulong startTick,
			ulong replacementActionInstanceId)
		{
			IReadOnlyList<SimulationActionActivationRequestState> requests = m_Frame.ActionState.GetActionActivationRequests();
			for (int i = 0; i < requests.Count; i++)
			{
				SimulationActionActivationRequestState candidate = requests[i];
				if (candidate.IsValid && candidate.SkillId == skillId &&
					string.Equals(candidate.ActionId, actionId, StringComparison.Ordinal) &&
					candidate.SkillEntryOperation.Equals(entryOperation) &&
					string.Equals(candidate.ContextId, contextId, StringComparison.Ordinal) &&
					candidate.InputSequence == inputSequence &&
					candidate.StartTick == startTick &&
					(replacementActionInstanceId == ulong.MaxValue ||
					 candidate.ReplacementActionInstanceId == replacementActionInstanceId))
				{
					if (candidate.ReplacementActionInstanceId != 0 &&
						TryFindInstanceIndex(candidate.ReplacementActionInstanceId, out int replacementIndex))
						return replacementIndex;
					return FindEmptyInstanceIndex(actionId);
				}
			}
			return -1;
		}

		public void ClearRequestAt(int index)
		{
            m_Frame.ActionState.RemoveActivationRequestAt(index);
		}

		bool TryFindInstanceIndex(ulong instanceId, out int index)
		{
			return m_Frame.ActionState.TryFindActionInstanceIndex(instanceId, out index);
		}

		int FindEmptyInstanceIndex(string actionId)
		{
			IReadOnlyList<Float32ActionInstanceState> actions = m_Frame.ActionState.GetActionInstances();
			int count = 0;
			int terminalIndex = -1;
			for (int i = 0; i < actions.Count; i++)
			{
				Float32ActionInstanceState action = actions[i];
				if (action.ActionId == actionId)
					count++;
				if (action.ActionId == actionId && action.IsTerminal)
					return i;
				if (terminalIndex < 0 && action.IsTerminal)
					terminalIndex = i;
			}
			return count < m_Layout.ActionCapacity(actionId)
				? terminalIndex >= 0 ? terminalIndex : actions.Count
				: -1;
		}

		public ulong NextSequence()
		{
			return m_Frame.ActionState.NextActionEventSequence();
		}

		bool IGameplayAbilityExecutionStorage<AbilityStateValue>.IsAbilityStateSlot(int slotIndex) =>
			m_Layout.IsSkillExecutionStateSlot(slotIndex);

		bool IGameplayAbilityExecutionStorage<AbilityStateValue>.IsValueValid(
			int slotIndex,
			AbilityStateValue value) =>
			value.Kind == m_Ability.StateSlots[slotIndex].ValueKind;

		AbilityStateValue IGameplayAbilityExecutionStorage<AbilityStateValue>.DefaultValue(int slotIndex)
		{
			ProgramStateSlot slot = m_Ability.StateSlots[slotIndex];
			return slot.DefaultConstantIndex >= 0
				? AbilityStateValue.FromConstant(
					m_Ability.Constants[slot.DefaultConstantIndex],
					slot.ValueKind)
				: AbilityStateValue.Default(slot.ValueKind);
		}

		GameplayAbilityExecutionAggregate<AbilityStateValue> IGameplayAbilityExecutionStorage<AbilityStateValue>.ReadAggregate() =>
			m_Frame.SkillState.GetAbilityExecutionState();

		void IGameplayAbilityExecutionStorage<AbilityStateValue>.WriteAggregate(
			GameplayAbilityExecutionAggregate<AbilityStateValue> aggregate) =>
			m_Frame.SkillState.SetAbilityExecutionState(aggregate);

		void MatchActive(
			int index,
			string contextId,
            ref int found,
            ref Float32ActionInstanceState state)
        {
            Float32ActionInstanceState candidate = m_Frame.ActionState.GetActionInstances()[index];
            if (!candidate.IsActive ||
                !string.IsNullOrEmpty(contextId) && !string.Equals(candidate.ContextId, contextId, StringComparison.Ordinal))
                return;
            if (found >= 0)
                throw new InvalidOperationException($"Action Context '{contextId}' resolves multiple active Action instances.");
			found = index;
			state = candidate;
		}

		void MatchActive(
			int index,
			CharacterSkillId skillId,
			ref int found,
			ref Float32ActionInstanceState state)
		{
			Float32ActionInstanceState candidate = m_Frame.ActionState.GetActionInstances()[index];
			if (!candidate.IsActive || candidate.SkillId != skillId)
				return;
			if (found >= 0)
				throw new InvalidOperationException($"Skill '{skillId}' resolves multiple active Action instances.");
			found = index;
			state = candidate;
		}

		void PopSkillExecution(Float32ActionInstanceReference expected)
		{
			if (m_SkillExecutionStack.Count == 0 || !m_SkillExecutionStack.Pop().Equals(expected))
				throw new InvalidOperationException("Skill execution scope is unbalanced.");
		}

		sealed class SkillExecutionScope : IDisposable
		{
			Float32ActionStateStore m_Owner;
			Float32ActionInstanceReference m_Expected;
			Float32AbilityExecutionFrame.ActionTraceContextScope m_TraceScope;
			bool m_Disposed;

			public SkillExecutionScope Begin(
				Float32ActionStateStore owner,
				Float32ActionInstanceReference expected,
				Float32AbilityExecutionFrame.ActionTraceContextScope traceScope)
			{
				m_Owner = owner;
				m_Expected = expected;
				m_TraceScope = traceScope;
				m_Disposed = false;
				return this;
			}

			public void Dispose()
			{
				if (m_Disposed)
					return;
				m_Disposed = true;
				m_Owner.PopSkillExecution(m_Expected);
				m_TraceScope.Dispose();
				m_Owner.m_SkillExecutionScopePool.Push(this);
				m_Owner = null;
				m_Expected = default;
				m_TraceScope = default;
			}
		}

		sealed class TraceExecutionScope : IDisposable
		{
			IDisposable m_Execution;
			Float32AbilityExecutionFrame.ActionTraceContextScope m_Trace;
			bool m_Disposed;

			public TraceExecutionScope Begin(IDisposable execution, Float32AbilityExecutionFrame.ActionTraceContextScope trace)
			{
				m_Execution = execution;
				m_Trace = trace;
				m_Disposed = false;
				return this;
			}

			public void Dispose()
			{
				if (m_Disposed)
					return;
				m_Disposed = true;
				m_Execution.Dispose();
				m_Trace.Dispose();
				m_Execution = null;
				m_Trace = default;
			}
		}

	}
}

