using ThirdPersonSimulation;
using System;
using System.Collections.Generic;

namespace ThirdPersonSimulation.Fixed
{
    internal readonly struct FixedActionActivationRequestState
    {
        public FixedActionActivationRequestState(
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
            EquipmentActionContext equipmentContext = default)
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
        public bool IsValid =>
            !string.IsNullOrEmpty(ActionId) &&
            !string.IsNullOrEmpty(ContextId) &&
            InputSequence != 0 &&
            StartTick != 0 &&
            Source.IsValid &&
            (!Source.IsCharacterControl || SkillId.IsValid && SkillEntryOperation.IsValid);
    }

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
                EquipmentContext);
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
                EquipmentContext);
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

    internal sealed class FixedActionStateStore : FixedOperationModule, IFixedActionContextReader, IFixedSkillExecutionStateAccess
    {
        readonly FixedStatePort m_State;
        readonly Stack<FixedActionInstanceReference> m_SkillExecutionStack = new Stack<FixedActionInstanceReference>();
        FixedSkillExecutionStateAggregate m_SkillExecutionStates;
        FixedSkillExecutionStateFrame m_ActiveSkillExecution;

        public FixedActionStateStore(
            FixedProgramAccess access,
            FixedStatePort state,
            FixedEvaluationFrame frame)
            : base(access)
        {
            m_State = state ?? throw new ArgumentNullException(nameof(state));
            (frame ?? throw new ArgumentNullException(nameof(frame)))
                .BindSkillExecutionStateAccess(this);
        }

        public void BeginEvaluation()
        {
            if (m_ActiveSkillExecution != null || m_SkillExecutionStack.Count != 0)
                throw new InvalidOperationException("Skill execution state retained transient execution state across evaluations.");
            m_SkillExecutionStates = null;
        }

        public void EndEvaluation()
        {
            if (m_ActiveSkillExecution != null || m_SkillExecutionStack.Count != 0)
                throw new InvalidOperationException("Skill execution state has an unclosed runtime scope.");
            m_SkillExecutionStates = null;
        }

        public IDisposable EnterSkillExecution(FixedActionInstanceState action)
        {
            if (!action.IsValid || !action.SkillId.IsValid || !action.SkillEntryOperation.IsValid)
                throw new ArgumentException("Skill execution action identity is incomplete.", nameof(action));
            if (m_ActiveSkillExecution != null)
                throw new InvalidOperationException("Skill execution frames cannot be nested.");
            EnsureSkillExecutionStates();
            FixedSkillExecutionStateFrame frame = m_SkillExecutionStates.Find(action.InstanceId);
            if (frame == null)
            {
                frame = new FixedSkillExecutionStateFrame(
                    action.SkillId,
                    action.SkillEntryOperation,
                    action.InstanceId,
                    action.PredictionKey,
                    action.SkillExecutionGeneration);
                m_SkillExecutionStates.Add(frame);
                WriteSkillExecutionStates();
            }
            else
            {
                if (frame.SkillId != action.SkillId ||
                    !frame.EntryOperation.Equals(action.SkillEntryOperation) ||
                    frame.PredictionKey != action.PredictionKey ||
                    frame.Generation != 0 && action.SkillExecutionGeneration != 0 &&
                    frame.Generation != action.SkillExecutionGeneration)
                {
                    throw new InvalidOperationException($"Skill execution frame does not match Action instance '{action.InstanceId}'.");
                }
            }
            m_ActiveSkillExecution = frame;
            return new SkillExecutionFrameScope(this, frame);
        }

        public bool RemoveSkillExecution(ulong actionInstanceId)
        {
            if (m_ActiveSkillExecution != null)
                throw new InvalidOperationException("Skill execution frame cannot be removed while active.");
            EnsureSkillExecutionStates();
            if (!m_SkillExecutionStates.Remove(actionInstanceId))
                return false;
            WriteSkillExecutionStates();
            return true;
        }

        public bool IsContextActive(string contextId) => FindActive(contextId, out _) >= 0;

        public bool IsSkillActive(CharacterSkillId skillId) => FindActive(skillId, out _) >= 0;

        public bool IsSkillCompleted(CharacterSkillId skillId) => CompletedSkillInstanceId(skillId) != 0;

        public ulong CompletedSkillInstanceId(CharacterSkillId skillId)
        {
            ulong result = 0;
            foreach (TypedActionStateAddresses addresses in m_Layout.ActionStateIndex.Values)
            {
                FixedActionInstanceState action = m_State.Get(addresses.Instance.SlotIndex).ActionInstance;
                if (action.SkillId != skillId || action.State != SimulationActionState.Ended)
                    continue;
                if (result != 0)
                    throw new InvalidOperationException($"Skill '{skillId}' resolves multiple completed Action instances.");
                result = action.InstanceId;
            }
            return result;
        }

        public int FindActive(CharacterSkillId skillId, out FixedActionInstanceState state)
        {
            int found = -1;
            state = default;
            foreach (TypedActionStateAddresses addresses in m_Layout.ActionStateIndex.Values)
                MatchActive(addresses.Instance, skillId, ref found, ref state);
            return found;
        }

        public int FindCurrent(CharacterSkillId skillId, out FixedActionInstanceState state)
        {
            int found = -1;
            state = default;
            foreach (TypedActionStateAddresses addresses in m_Layout.ActionStateIndex.Values)
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
            if (m_ActiveSkillExecution != null && m_ActiveSkillExecution.ActionInstanceId == action.InstanceId)
                m_ActiveSkillExecution.BindGeneration(generation);
            FixedActionInstanceState next = action.WithSkillExecution(entryOperation, generation);
            WriteState(next);
            if (m_ActiveSkillExecution != null && m_ActiveSkillExecution.ActionInstanceId == action.InstanceId)
                WriteSkillExecutionStates();
            return next;
        }

        public IDisposable PushSkillExecution(FixedActionInstanceState action)
        {
            if (!action.IsActive || !action.SkillId.IsValid || !action.SkillEntryOperation.IsValid)
                throw new ArgumentException("Skill execution owner is incomplete.", nameof(action));
            FixedActionInstanceReference reference = FixedActionInstanceReference.FromInstance(action);
            m_SkillExecutionStack.Push(reference);
            return new SkillExecutionScope(this, reference);
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

        public FixedActionInstanceState FindOnlyActive()
        {
            FixedActionInstanceState result = default;
            foreach (TypedActionStateAddresses addresses in m_Layout.ActionStateIndex.Values)
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
            TypedActionStateAddresses addresses = m_Layout.RequireAction(reference.ActionId);
            FixedActionInstanceState current = m_State.Get(addresses.Instance.SlotIndex).ActionInstance;
            return current.IsActive &&
                   string.Equals(current.ContextId, reference.ContextId, StringComparison.Ordinal) &&
                   current.InstanceId == reference.InstanceId &&
                   current.PredictionKey == reference.PredictionKey &&
                   reference.MatchesSkillExecution(current)
                ? current
                   : default;
        }

        public FixedActionInstanceState RequireActiveTransient(FixedActionInstanceReference reference)
        {
            if (!reference.IsTransientValid)
                return default;
            TypedActionStateAddresses addresses = m_Layout.RequireAction(reference.ActionId);
            FixedActionInstanceState current = m_State.Get(addresses.Instance.SlotIndex).ActionInstance;
            return current.IsActive &&
                   string.Equals(current.ContextId, reference.ContextId, StringComparison.Ordinal) &&
                   current.InstanceId == reference.InstanceId &&
                   current.PredictionKey == reference.PredictionKey &&
                   reference.MatchesTransientSkillExecution(current)
                ? current
                : default;
        }

        bool IFixedSkillExecutionStateAccess.TryGet(int slotIndex, out CharacterStateValue value)
        {
            if (m_ActiveSkillExecution == null || !m_Layout.IsSkillExecutionStateSlot(slotIndex))
            {
                value = default;
                return false;
            }
            if (m_ActiveSkillExecution.TryGetValue(slotIndex, out value))
                return true;
            value = DefaultSkillExecutionState(slotIndex);
            return true;
        }

        bool IFixedSkillExecutionStateAccess.TrySet(int slotIndex, CharacterStateValue value)
        {
            if (m_ActiveSkillExecution == null || !m_Layout.IsSkillExecutionStateSlot(slotIndex))
                return false;
            if (value.Kind != m_Program.StateSlots[slotIndex].ValueKind)
                throw new InvalidOperationException($"Skill execution state slot '{slotIndex}' expects '{m_Program.StateSlots[slotIndex].ValueKind}', received '{value.Kind}'.");
            m_ActiveSkillExecution.SetValue(slotIndex, value);
            WriteSkillExecutionStates();
            return true;
        }

        bool IFixedSkillExecutionStateAccess.TryReset(int slotIndex)
        {
            if (m_ActiveSkillExecution == null || !m_Layout.IsSkillExecutionStateSlot(slotIndex))
                return false;
            m_ActiveSkillExecution.SetValue(slotIndex, DefaultSkillExecutionState(slotIndex));
            WriteSkillExecutionStates();
            return true;
        }

        public bool ContainsInstance(ulong instanceId)
        {
            foreach (TypedActionStateAddresses addresses in m_Layout.ActionStateIndex.Values)
            {
                FixedActionInstanceState current = m_State.Get(addresses.Instance.SlotIndex).ActionInstance;
                if (current.InstanceId == instanceId)
                    return true;
            }
            return false;
        }

        public void WriteState(FixedActionInstanceState action)
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

        public void WriteRequest(int slot, FixedActionActivationRequestState state) =>
            m_State.Set(slot, CharacterStateValue.FromActionActivationRequest(state));

        public FixedActionActivationRequestState ReadRequest(int slot) =>
            m_State.Get(slot).ActionActivationRequest;

        public int FindPendingSkill(
            CharacterSkillId skillId,
            out FixedActionActivationRequestState request)
        {
            int found = -1;
            request = default;
            foreach (TypedActionStateAddresses addresses in m_Layout.ActionStateIndex.Values)
            {
                FixedActionActivationRequestState candidate = m_State.Get(addresses.Request.SlotIndex).ActionActivationRequest;
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

        public FixedActionInstanceState ReadSlot(int slot) => m_State.Get(slot).ActionInstance;

        public ulong NextSequence()
        {
            int slot = m_Layout.RequireStateSlot(ProgramStateSemantic.ActionEventSequence);
            ulong value = checked(m_State.Get(slot).UInt64 + 1);
            if (value == 0)
                throw new OverflowException("Action sequence overflowed.");
            m_State.Set(slot, CharacterStateValue.FromUInt64(value));
            return value;
        }

        void EnsureSkillExecutionStates()
        {
            if (m_SkillExecutionStates != null)
                return;
            m_SkillExecutionStates = m_State
                .Get(m_Layout.SkillExecutionStateAddress.SlotIndex)
                .SkillExecutionState
                .Clone();
            WriteSkillExecutionStates();
        }

        void WriteSkillExecutionStates()
        {
            m_State.Set(
                m_Layout.SkillExecutionStateAddress.SlotIndex,
                CharacterStateValue.FromSkillExecutionState(m_SkillExecutionStates));
        }

        CharacterStateValue DefaultSkillExecutionState(int slotIndex)
        {
            ProgramStateSlot slot = m_Program.StateSlots[slotIndex];
            return slot.DefaultConstantIndex >= 0
                ? CharacterStateValue.FromConstant(
                    m_Program.Constants[slot.DefaultConstantIndex],
                    slot.ValueKind)
                : CharacterStateValue.Default(slot.ValueKind);
        }

        void ExitSkillExecution(FixedSkillExecutionStateFrame frame)
        {
            if (!ReferenceEquals(m_ActiveSkillExecution, frame))
                throw new InvalidOperationException("Skill execution frame scope is unbalanced.");
            if (frame.Generation == 0)
                m_SkillExecutionStates.Remove(frame.ActionInstanceId);
            WriteSkillExecutionStates();
            m_ActiveSkillExecution = null;
        }

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
            bool m_Disposed;

            public SkillExecutionScope(FixedActionStateStore owner, FixedActionInstanceReference expected)
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

        sealed class SkillExecutionFrameScope : IDisposable
        {
            readonly FixedActionStateStore m_Owner;
            readonly FixedSkillExecutionStateFrame m_Frame;
            bool m_Disposed;

            public SkillExecutionFrameScope(
                FixedActionStateStore owner,
                FixedSkillExecutionStateFrame frame)
            {
                m_Owner = owner;
                m_Frame = frame;
            }

            public void Dispose()
            {
                if (m_Disposed)
                    return;
                m_Disposed = true;
                m_Owner.ExitSkillExecution(m_Frame);
            }
        }
    }
}

