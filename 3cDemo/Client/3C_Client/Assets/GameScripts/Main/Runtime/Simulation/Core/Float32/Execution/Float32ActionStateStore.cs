using System;
using System.Collections.Generic;

namespace ThirdPersonSimulation
{
    internal readonly struct Float32ActionActivationRequestState
    {
        public Float32ActionActivationRequestState(
            string actionId,
            string contextId,
            string sourceInputRequestId,
            ulong inputSequence,
            ulong startTick,
            string targetKey,
            SimulationActionTargetSnapshot targetSnapshot,
            OperationHandle sourceOperation,
            EquipmentActionContext equipmentContext = default)
        {
            ActionId = SimulationIdentity.Require(actionId, nameof(actionId));
            ContextId = SimulationIdentity.Require(contextId, nameof(contextId));
            SourceInputRequestId = sourceInputRequestId ?? string.Empty;
            if (inputSequence == 0 || startTick == 0 || !sourceOperation.IsValid)
                throw new ArgumentException("Action activation request identity is incomplete.");
            InputSequence = inputSequence;
            StartTick = startTick;
            TargetKey = targetKey ?? string.Empty;
            TargetSnapshot = targetSnapshot;
            SourceOperation = sourceOperation;
            EquipmentContext = equipmentContext;
        }

        public string ActionId { get; }
        public string ContextId { get; }
        public string SourceInputRequestId { get; }
        public ulong InputSequence { get; }
        public ulong StartTick { get; }
        public string TargetKey { get; }
        public SimulationActionTargetSnapshot TargetSnapshot { get; }
        public OperationHandle SourceOperation { get; }
        public EquipmentActionContext EquipmentContext { get; }
        public bool IsValid =>
            !string.IsNullOrEmpty(ActionId) &&
            !string.IsNullOrEmpty(ContextId) &&
            InputSequence != 0 &&
            StartTick != 0 &&
            SourceOperation.IsValid;
    }

    internal readonly struct Float32ActionInstanceState
    {
        public Float32ActionInstanceState(
            string actionId,
            string contextId,
            ulong instanceId,
            ulong predictionKey,
            string sourceInputRequestId,
            ulong inputSequence,
            ulong startTick,
            string targetKey,
            SimulationActionTargetSnapshot targetSnapshot,
            OperationHandle sourceOperation,
            SimulationActionPhase phase,
            SimulationActionState state,
            SimulationActionLifecycleTransitionType lastTransition,
            ulong lastTransitionTick,
            ulong lastTransitionSourceTick,
            string reason,
            EquipmentActionContext equipmentContext = default)
        {
            ActionId = actionId ?? string.Empty;
            ContextId = contextId ?? string.Empty;
            InstanceId = instanceId;
            PredictionKey = predictionKey;
            SourceInputRequestId = sourceInputRequestId ?? string.Empty;
            InputSequence = inputSequence;
            StartTick = startTick;
            TargetKey = targetKey ?? string.Empty;
            TargetSnapshot = targetSnapshot;
            SourceOperation = sourceOperation;
            Phase = phase;
            State = state;
            LastTransition = lastTransition;
            LastTransitionTick = lastTransitionTick;
            LastTransitionSourceTick = lastTransitionSourceTick;
            Reason = reason ?? string.Empty;
            EquipmentContext = equipmentContext;
        }

        public string ActionId { get; }
        public string ContextId { get; }
        public ulong InstanceId { get; }
        public ulong PredictionKey { get; }
        public string SourceInputRequestId { get; }
        public ulong InputSequence { get; }
        public ulong StartTick { get; }
        public string TargetKey { get; }
        public SimulationActionTargetSnapshot TargetSnapshot { get; }
        public OperationHandle SourceOperation { get; }
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
            SourceOperation.IsValid;
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
                ContextId,
                InstanceId,
                PredictionKey,
                SourceInputRequestId,
                InputSequence,
                StartTick,
                TargetKey,
                TargetSnapshot,
                SourceOperation,
                phase,
                state,
                transition,
                transitionTick,
                sourceTick,
                reason,
                EquipmentContext);
        }
    }

    internal readonly struct Float32ActionInstanceReference
    {
        public Float32ActionInstanceReference(string actionId, string contextId, ulong instanceId, ulong predictionKey)
        {
            ActionId = actionId ?? string.Empty;
            ContextId = contextId ?? string.Empty;
            InstanceId = instanceId;
            PredictionKey = predictionKey;
        }

        public string ActionId { get; }
        public string ContextId { get; }
        public ulong InstanceId { get; }
        public ulong PredictionKey { get; }
        public bool IsValid =>
            !string.IsNullOrEmpty(ActionId) &&
            !string.IsNullOrEmpty(ContextId) &&
            InstanceId != 0 &&
            PredictionKey != 0;

        public static Float32ActionInstanceReference FromInstance(Float32ActionInstanceState state)
        {
            return state.IsValid
                ? new Float32ActionInstanceReference(state.ActionId, state.ContextId, state.InstanceId, state.PredictionKey)
                : default;
        }
    }

    internal sealed class Float32ActionStateStore : Float32OperationModule, IFloat32ActionContextReader
    {
        readonly Float32StatePort m_State;

        public Float32ActionStateStore(Float32ProgramAccess access, Float32StatePort state)
            : base(access)
        {
            m_State = state ?? throw new ArgumentNullException(nameof(state));
        }

        public bool IsContextActive(string contextId) => FindActive(contextId, out _) >= 0;

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
                   current.PredictionKey == reference.PredictionKey
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
    }
}
