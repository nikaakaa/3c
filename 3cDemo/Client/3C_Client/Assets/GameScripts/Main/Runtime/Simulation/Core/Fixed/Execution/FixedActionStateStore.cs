using ThirdPersonSimulation;
using System;
using System.Collections.Generic;
using SimulationActionActivationRequestState = ThirdPersonSimulation.SimulationActionActivationRequestState<ThirdPersonSimulation.Fixed.SimulationActionTargetSnapshot>;

namespace ThirdPersonSimulation.Fixed
{
    internal readonly struct FixedActionInstanceState
    {
        public FixedActionInstanceState(
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
            ulong segmentGeneration = 0)
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
        }

        public string ActionId { get; }
        public CharacterSkillId SkillId { get; }
        public OperationHandle SkillEntryOperation { get; }
        public ulong SkillExecutionGeneration { get; }
        public ulong SegmentGeneration { get; }
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

        public FixedActionInstanceState WithLifecycle(
            SimulationActionPhase phase,
            SimulationActionState state,
            SimulationActionLifecycleTransitionType transition,
            ulong transitionTick,
            ulong sourceTick,
            string reason)
        {
            return new FixedActionInstanceState(
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
                SegmentGeneration);
        }

        public FixedActionInstanceState WithSegmentGeneration(ulong segmentGeneration)
        {
            return new FixedActionInstanceState(
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
                segmentGeneration);
        }

        public FixedActionInstanceState WithSkillExecution(OperationHandle entryOperation, ulong generation)
        {
            return new FixedActionInstanceState(
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
                SegmentGeneration);
        }
    }

    internal readonly struct FixedActionInstanceReference
    {
        public FixedActionInstanceReference(
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

        public bool MatchesSkillExecution(FixedActionInstanceState state) =>
            IsValid &&
            state.IsValid &&
            state.SkillId == SkillId &&
            state.SkillEntryOperation.Equals(SkillEntryOperation) &&
            (!HasSkillExecution || state.SkillExecutionGeneration == SkillExecutionGeneration);

        public bool MatchesTransientSkillExecution(FixedActionInstanceState state) =>
            IsTransientValid &&
            state.IsValid &&
            state.SkillId == SkillId &&
            state.SkillEntryOperation.Equals(SkillEntryOperation) &&
            (SkillExecutionGeneration == 0 || state.SkillExecutionGeneration == SkillExecutionGeneration);

        public static FixedActionInstanceReference FromInstance(FixedActionInstanceState state)
        {
            return state.IsValid
                ? new FixedActionInstanceReference(
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

    internal sealed class FixedActionStateStore : FixedOperationModule, IFixedActionContextReader, IFixedSkillExecutionStateAccess, IGameplayAbilityExecutionStorage<CharacterStateValue>
    {
        readonly FixedEvaluationFrame m_Frame;
        readonly FixedStatePort m_State;
        readonly Stack<FixedActionInstanceReference> m_SkillExecutionStack = new Stack<FixedActionInstanceReference>();
        readonly GameplayAbilityExecutionManager<CharacterStateValue> m_SkillExecution;

        public FixedActionStateStore(
            FixedProgramAccess access,
            FixedStatePort state,
            FixedEvaluationFrame frame)
            : base(access)
        {
            m_Frame = frame ?? throw new ArgumentNullException(nameof(frame));
            m_State = state ?? throw new ArgumentNullException(nameof(state));
            m_SkillExecution = new GameplayAbilityExecutionManager<CharacterStateValue>(this);
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
        }

        public IDisposable EnterSkillExecution(FixedActionInstanceState action)
        {
            if (!action.IsValid || !action.SkillId.IsValid || !action.SkillEntryOperation.IsValid)
                throw new ArgumentException("Skill execution action identity is incomplete.", nameof(action));
            IDisposable execution = m_SkillExecution.Enter(new AbilityExecutionContext(
                action.SkillId,
                action.SkillEntryOperation,
                action.InstanceId,
                action.PredictionKey,
                action.SkillExecutionGeneration));
            return new TraceExecutionScope(
                execution,
				m_Frame.PushActionTraceContext(action.InstanceId, action.SkillId, action.SkillEntryOperation));
        }

        public bool RemoveSkillExecution(ulong actionInstanceId)
        {
            return m_SkillExecution.Remove(actionInstanceId);
        }

        public bool IsContextActive(string contextId)
        {
            foreach (TypedActionStateAddresses addresses in m_Layout.AllActionStateAddresses)
            {
                FixedActionInstanceState action = m_State.Get(addresses.Instance.SlotIndex).ActionInstance;
                if (action.IsActive && string.Equals(action.ContextId, contextId ?? string.Empty, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        public bool IsCurrentExecutionContextActive()
        {
            return TryGetCurrentSkillExecution(out FixedActionInstanceState action) && action.IsActive;
        }

        public bool IsAbilityActive(CharacterSkillId abilityId)
        {
            foreach (TypedActionStateAddresses addresses in m_Layout.AllActionStateAddresses)
            {
                if (m_State.Get(addresses.Instance.SlotIndex).ActionInstance.IsActive &&
                    m_State.Get(addresses.Instance.SlotIndex).ActionInstance.SkillId == abilityId)
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
            foreach (TypedActionStateAddresses addresses in m_Layout.AllActionStateAddresses)
            {
                FixedActionInstanceState action = m_State.Get(addresses.Instance.SlotIndex).ActionInstance;
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

        public int FindActive(CharacterSkillId skillId, out FixedActionInstanceState state)
        {
            int found = -1;
            state = default;
            foreach (TypedActionStateAddresses addresses in m_Layout.AllActionStateAddresses)
                MatchActive(addresses.Instance, skillId, ref found, ref state);
            return found;
        }

        public bool TryGetActiveAbilityInstanceId(CharacterSkillId abilityId, out ulong instanceId)
        {
            ulong found = 0;
            foreach (TypedActionStateAddresses addresses in m_Layout.AllActionStateAddresses)
            {
                FixedActionInstanceState state = m_State.Get(addresses.Instance.SlotIndex).ActionInstance;
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

        public int FindCurrent(CharacterSkillId skillId, out FixedActionInstanceState state)
        {
            int found = -1;
            state = default;
            foreach (TypedActionStateAddresses addresses in m_Layout.AllActionStateAddresses)
            {
                FixedActionInstanceState candidate = m_State.Get(addresses.Instance.SlotIndex).ActionInstance;
                if (!candidate.IsValid || candidate.SkillId != skillId)
                    continue;
                if (found >= 0)
                    throw new InvalidOperationException($"Skill '{skillId}' resolves multiple Action instances.");
                found = addresses.Instance.SlotIndex;
                state = candidate;
            }
            return found;
        }

        public IReadOnlyList<FixedActionInstanceState> CurrentActions(CharacterSkillId skillId)
        {
            var result = new List<FixedActionInstanceState>();
            foreach (TypedActionStateAddresses addresses in m_Layout.AllActionStateAddresses)
            {
                FixedActionInstanceState candidate = m_State.Get(addresses.Instance.SlotIndex).ActionInstance;
                if (candidate.IsValid && candidate.SkillId == skillId)
                    result.Add(candidate);
            }
            result.Sort((left, right) => left.InstanceId.CompareTo(right.InstanceId));
            return result.AsReadOnly();
        }

        public IEnumerable<FixedActionInstanceState> EnumerateActiveActions()
        {
            foreach (TypedActionStateAddresses addresses in m_Layout.AllActionStateAddresses)
            {
                FixedActionInstanceState action = m_State.Get(addresses.Instance.SlotIndex).ActionInstance;
                if (action.IsActive)
                    yield return action;
            }
        }

        public FixedActionInstanceState BindSkillExecution(
            FixedActionInstanceState action,
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
            FixedActionInstanceState next = action.WithSkillExecution(entryOperation, generation);
            WriteState(next);
            return next;
        }

        public IDisposable PushSkillExecution(FixedActionInstanceState action)
        {
            if (!action.IsActive || !action.SkillId.IsValid || !action.SkillEntryOperation.IsValid)
                throw new ArgumentException("Skill execution owner is incomplete.", nameof(action));
            FixedActionInstanceReference reference = FixedActionInstanceReference.FromInstance(action);
            m_SkillExecutionStack.Push(reference);
            return new SkillExecutionScope(
                this,
                reference,
			m_Frame.PushActionTraceContext(action.InstanceId, action.SkillId, action.SkillEntryOperation));
        }

        public bool TryGetCurrentSkillExecution(out FixedActionInstanceState action)
        {
            if (m_SkillExecutionStack.Count == 0)
            {
                action = default;
                return false;
            }
            action = RequireActiveTransient(m_SkillExecutionStack.Peek());
            return action.IsActive;
        }

        public int FindActive(string contextId, out FixedActionInstanceState state)
        {
            int found = -1;
            state = default;
            if (!string.IsNullOrEmpty(contextId))
            {
                foreach (TypedActionStateAddresses addresses in m_Layout.AllActionStateAddresses)
                    MatchActive(addresses.Instance, contextId, ref found, ref state);
            }
            else
            {
                foreach (TypedActionStateAddresses addresses in m_Layout.AllActionStateAddresses)
                    MatchActive(addresses.Instance, string.Empty, ref found, ref state);
            }
            return found;
        }

        public FixedActionInstanceState FindOnlyActive()
        {
            FixedActionInstanceState result = default;
            foreach (TypedActionStateAddresses addresses in m_Layout.AllActionStateAddresses)
            {
                FixedActionInstanceState current = m_State.Get(addresses.Instance.SlotIndex).ActionInstance;
                if (!current.IsActive)
                    continue;
                if (result.IsActive)
                    throw new InvalidOperationException("Action Context is ambiguous because multiple actions are active.");
                result = current;
            }
            return result;
        }

        public FixedActionInstanceState RequireActive(FixedActionInstanceState expected)
        {
            if (!expected.IsActive)
                return default;
            int slot = FindActive(expected.ContextId, out FixedActionInstanceState current);
            return slot >= 0 && current.InstanceId == expected.InstanceId ? current : default;
        }

        public FixedActionInstanceState RequireActive(FixedActionInstanceReference reference)
        {
            if (!reference.IsValid)
                return default;
            if (!TryGetInstance(reference.InstanceId, out FixedActionInstanceState current))
                return default;
            return current.IsActive &&
                   string.Equals(current.ActionId, reference.ActionId, StringComparison.Ordinal) &&
                   string.Equals(current.ContextId, reference.ContextId, StringComparison.Ordinal) &&
                   current.PredictionKey == reference.PredictionKey &&
                   reference.MatchesSkillExecution(current)
                ? current
                : default;
        }

        public FixedActionInstanceState RequireActiveTransient(FixedActionInstanceReference reference)
        {
            if (!reference.IsTransientValid)
                return default;
            if (!TryGetInstance(reference.InstanceId, out FixedActionInstanceState current))
                return default;
            return current.IsActive &&
                   string.Equals(current.ActionId, reference.ActionId, StringComparison.Ordinal) &&
                   string.Equals(current.ContextId, reference.ContextId, StringComparison.Ordinal) &&
                   current.PredictionKey == reference.PredictionKey &&
                   reference.MatchesTransientSkillExecution(current)
                ? current
                : default;
        }

        bool IFixedSkillExecutionStateAccess.TryGet(int slotIndex, out CharacterStateValue value)
        {
            return m_SkillExecution.TryGet(slotIndex, out value);
        }

        bool IFixedSkillExecutionStateAccess.TrySet(int slotIndex, CharacterStateValue value)
        {
            return m_SkillExecution.TrySet(slotIndex, value);
        }

        bool IFixedSkillExecutionStateAccess.TryReset(int slotIndex)
        {
            return m_SkillExecution.TryReset(slotIndex);
        }

        public bool ContainsInstance(ulong instanceId)
        {
            foreach (TypedActionStateAddresses addresses in m_Layout.AllActionStateAddresses)
            {
                FixedActionInstanceState current = m_State.Get(addresses.Instance.SlotIndex).ActionInstance;
                if (current.InstanceId == instanceId)
                    return true;
            }
            return false;
        }

        public bool TryGetInstance(ulong instanceId, out FixedActionInstanceState state)
        {
            foreach (TypedActionStateAddresses addresses in m_Layout.AllActionStateAddresses)
            {
                FixedActionInstanceState current = m_State.Get(addresses.Instance.SlotIndex).ActionInstance;
                if (current.InstanceId != instanceId)
                    continue;
                state = current;
                return true;
            }
            state = default;
            return false;
        }

        public void WriteState(FixedActionInstanceState action)
        {
            if (TryFindInstanceSlot(action.InstanceId, out int existingSlot))
            {
                m_State.Set(existingSlot, CharacterStateValue.FromActionInstance(action));
                return;
            }
            int slot = FindPendingRequestInstanceSlot(
                action.ActionId,
                action.SkillId,
                action.SkillEntryOperation,
                action.ContextId,
                action.InputSequence,
                action.StartTick,
                ulong.MaxValue);
            if (slot < 0)
                slot = FindEmptyInstanceSlot(action.ActionId);
            if (slot < 0)
                throw new InvalidOperationException($"Action '{action.ActionId}' has no free instance state slot.");
            FixedActionInstanceState previous = m_State.Get(slot).ActionInstance;
            if (previous.IsValid && previous.IsTerminal)
                m_SkillExecution.Remove(previous.InstanceId);
            m_State.Set(slot, CharacterStateValue.FromActionInstance(action));
        }

        public bool HasPendingRequest(string actionId)
        {
            foreach (SimulationActionActivationRequestState request in m_Frame.Transaction.GetActionActivationRequests())
                if (request.IsValid && string.Equals(request.ActionId, actionId, StringComparison.Ordinal))
                    return true;
            return false;
        }

        public void StageRequest(SimulationActionActivationRequestState state)
        {
            IReadOnlyList<SimulationActionActivationRequestState> current = m_Frame.Transaction.GetActionActivationRequests();
            int count = 0;
            for (int i = 0; i < current.Count; i++)
                if (string.Equals(current[i].ActionId, state.ActionId, StringComparison.Ordinal))
                    count++;
            if (count >= m_Layout.ActionStateSlots(state.ActionId).Count)
                throw new InvalidOperationException($"Action '{state.ActionId}' has no free activation request capacity.");
            var requests = new List<SimulationActionActivationRequestState>(current.Count + 1);
            for (int i = 0; i < current.Count; i++)
                requests.Add(current[i]);
            requests.Add(state);
            m_Frame.Transaction.SetActionActivationRequests(requests);
        }

        public int FindPendingSkill(
            CharacterSkillId skillId,
            out SimulationActionActivationRequestState request)
        {
            int found = -1;
            request = default;
            IReadOnlyList<SimulationActionActivationRequestState> requests = m_Frame.Transaction.GetActionActivationRequests();
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
            IReadOnlyList<SimulationActionActivationRequestState> requests = m_Frame.Transaction.GetActionActivationRequests();
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

        int FindPendingRequestInstanceSlot(
            string actionId,
            CharacterSkillId skillId,
            OperationHandle entryOperation,
            string contextId,
            ulong inputSequence,
            ulong startTick,
            ulong replacementActionInstanceId)
        {
            IReadOnlyList<SimulationActionActivationRequestState> requests = m_Frame.Transaction.GetActionActivationRequests();
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
                        TryFindInstanceSlot(candidate.ReplacementActionInstanceId, out int replacementSlot))
                        return replacementSlot;
                    return FindEmptyInstanceSlot(actionId);
                }
            }
            return -1;
        }

        public void ClearRequestAt(int index)
        {
            IReadOnlyList<SimulationActionActivationRequestState> current = m_Frame.Transaction.GetActionActivationRequests();
            if (index < 0 || index >= current.Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            var requests = new List<SimulationActionActivationRequestState>(current.Count - 1);
            for (int i = 0; i < current.Count; i++)
                if (i != index)
                    requests.Add(current[i]);
            m_Frame.Transaction.SetActionActivationRequests(requests);
        }

        public FixedActionInstanceState ReadSlot(int slot) => m_State.Get(slot).ActionInstance;

        bool TryFindInstanceSlot(ulong instanceId, out int slot)
        {
            foreach (TypedActionStateAddresses addresses in m_Layout.AllActionStateAddresses)
            {
                if (m_State.Get(addresses.Instance.SlotIndex).ActionInstance.InstanceId == instanceId)
                {
                    slot = addresses.Instance.SlotIndex;
                    return true;
                }
            }
            slot = -1;
            return false;
        }

        int FindEmptyInstanceSlot(string actionId)
        {
            foreach (TypedActionStateAddresses addresses in m_Layout.ActionStateSlots(actionId))
            {
                FixedActionInstanceState action = m_State.Get(addresses.Instance.SlotIndex).ActionInstance;
                if (!action.IsValid || action.IsTerminal)
                    return addresses.Instance.SlotIndex;
            }
            return -1;
        }

        public ulong NextSequence()
        {
            return m_Frame.Transaction.NextActionEventSequence();
        }

        bool IGameplayAbilityExecutionStorage<CharacterStateValue>.IsAbilityStateSlot(int slotIndex) =>
            m_Layout.IsSkillExecutionStateSlot(slotIndex);

        bool IGameplayAbilityExecutionStorage<CharacterStateValue>.IsValueValid(
            int slotIndex,
            CharacterStateValue value) =>
            value.Kind == m_Program.StateSlots[slotIndex].ValueKind;

        CharacterStateValue IGameplayAbilityExecutionStorage<CharacterStateValue>.DefaultValue(int slotIndex)
        {
            ProgramStateSlot slot = m_Program.StateSlots[slotIndex];
            return slot.DefaultConstantIndex >= 0
                ? CharacterStateValue.FromConstant(
                    m_Program.Constants[slot.DefaultConstantIndex],
                    slot.ValueKind)
                : CharacterStateValue.Default(slot.ValueKind);
        }

        GameplayAbilityExecutionAggregate<CharacterStateValue> IGameplayAbilityExecutionStorage<CharacterStateValue>.ReadAggregate() =>
            m_Frame.Transaction.GetAbilityExecutionState();

        void IGameplayAbilityExecutionStorage<CharacterStateValue>.WriteAggregate(
            GameplayAbilityExecutionAggregate<CharacterStateValue> aggregate) =>
            m_Frame.Transaction.SetAbilityExecutionState(aggregate);

        void MatchActive(
            TypedStateAddress address,
            string contextId,
            ref int found,
            ref FixedActionInstanceState state)
        {
            FixedActionInstanceState candidate = m_State.Get(address.SlotIndex).ActionInstance;
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
            ref FixedActionInstanceState state)
        {
            FixedActionInstanceState candidate = m_State.Get(address.SlotIndex).ActionInstance;
            if (!candidate.IsActive || candidate.SkillId != skillId)
                return;
            if (found >= 0)
                throw new InvalidOperationException($"Skill '{skillId}' resolves multiple active Action instances.");
            found = address.SlotIndex;
            state = candidate;
        }

        void PopSkillExecution(FixedActionInstanceReference expected)
        {
            if (m_SkillExecutionStack.Count == 0 || !m_SkillExecutionStack.Pop().Equals(expected))
                throw new InvalidOperationException("Skill execution scope is unbalanced.");
        }

        sealed class SkillExecutionScope : IDisposable
        {
            readonly FixedActionStateStore m_Owner;
            readonly FixedActionInstanceReference m_Expected;
            readonly IDisposable m_TraceScope;
            bool m_Disposed;

            public SkillExecutionScope(
                FixedActionStateStore owner,
                FixedActionInstanceReference expected,
                IDisposable traceScope)
            {
                m_Owner = owner;
                m_Expected = expected;
                m_TraceScope = traceScope;
            }

            public void Dispose()
            {
                if (m_Disposed)
                    return;
                m_Disposed = true;
                m_Owner.PopSkillExecution(m_Expected);
                m_TraceScope.Dispose();
            }
        }

        sealed class TraceExecutionScope : IDisposable
        {
            readonly IDisposable m_Execution;
            readonly IDisposable m_Trace;
            bool m_Disposed;

            public TraceExecutionScope(IDisposable execution, IDisposable trace)
            {
                m_Execution = execution;
                m_Trace = trace;
            }

            public void Dispose()
            {
                if (m_Disposed)
                    return;
                m_Disposed = true;
                m_Execution.Dispose();
                m_Trace.Dispose();
            }
        }

    }
}
