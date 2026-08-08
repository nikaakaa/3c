using ThirdPersonSimulation;
using System;
using System.Collections.Generic;

namespace ThirdPersonSimulation.Fixed
{
    internal readonly struct FixedInputRequestState
    {
        public FixedInputRequestState(
            string requestId,
            ulong sequence,
            ulong sourceTick,
            ulong expireTick,
            int priority,
            bool consumed = false)
        {
            RequestId = requestId ?? string.Empty;
            Sequence = sequence;
            SourceTick = sourceTick;
            ExpireTick = expireTick;
            Priority = priority;
            Consumed = consumed;
        }

        public string RequestId { get; }
        public ulong Sequence { get; }
        public ulong SourceTick { get; }
        public ulong ExpireTick { get; }
        public int Priority { get; }
        public bool Consumed { get; }
        public bool IsValid => !string.IsNullOrEmpty(RequestId) && Sequence != 0;

        public FixedInputRequestState Consume()
        {
            return IsValid
                ? new FixedInputRequestState(RequestId, Sequence, SourceTick, ExpireTick, Priority, true)
                : this;
        }
    }

    internal sealed class FixedInputRuntime : FixedOperationModule, IFixedInputPort
    {
        readonly FixedStatePort m_State;
        readonly FixedEvaluationFrame m_Frame;

        public FixedInputRuntime(
            FixedProgramAccess access,
            FixedStatePort state,
            FixedEvaluationFrame frame)
            : base(access)
        {
            m_State = state ?? throw new ArgumentNullException(nameof(state));
            m_Frame = frame ?? throw new ArgumentNullException(nameof(frame));
        }

        public void ApplyRequests()
        {
            foreach (KeyValuePair<string, TypedStateAddress> pair in m_Layout.InputRequestIndex)
            {
                string requestId = pair.Key;
                TypedStateAddress address = pair.Value;
                FixedInputRequestState state = m_State.Get(address.SlotIndex).InputRequest;
                if (state.IsValid && state.ExpireTick < m_Frame.Tick.Value)
                    state = default;
                for (int requestIndex = 0; requestIndex < m_Frame.Input.Requests.Count; requestIndex++)
                {
                    SimulationInputRequest request = m_Frame.Input.Requests[requestIndex];
                    if (!string.Equals(request.RequestId, requestId, StringComparison.Ordinal))
                        continue;
                    if (!state.IsValid ||
                        request.Priority > state.Priority ||
                        request.Priority == state.Priority && request.Sequence > state.Sequence)
                    {
                        state = new FixedInputRequestState(
                            request.RequestId,
                            request.Sequence,
                            request.SourceTick,
                            request.ExpireSimulationTick,
                            request.Priority);
                    }
                }
                m_State.Set(address.SlotIndex, CharacterStateValue.FromInputRequest(state));
            }
        }

        public void ApplyBlackboardInputBindings(IFixedBlackboardPort blackboard)
        {
            if (blackboard == null)
                throw new ArgumentNullException(nameof(blackboard));
            for (int i = 0; i < m_Layout.BlackboardInputBindings.Count; i++)
            {
                BlackboardInputStateBinding binding = m_Layout.BlackboardInputBindings[i];
                blackboard.ProjectBlackboardInput(binding, ReadValue(binding.InputId, (SimulationInputValueKind)binding.InputKind));
            }
        }

        public bool HasRequest(string requestId, out FixedInputRequestState state)
        {
            if (!m_Layout.TryGetInputRequest(requestId, out TypedStateAddress address))
            {
                state = default;
                return false;
            }
            state = m_State.Get(address.SlotIndex).InputRequest;
            return state.IsValid && !state.Consumed && state.ExpireTick >= m_Frame.Tick.Value;
        }

        public void ClearRequest(string requestId)
        {
            if (!m_Layout.TryGetInputRequest(requestId, out TypedStateAddress address))
                return;
            FixedInputRequestState state = m_State.Get(address.SlotIndex).InputRequest;
            if (state.IsValid && !state.Consumed)
                m_State.Set(address.SlotIndex, CharacterStateValue.FromInputRequest(state.Consume()));
        }

        public SimulationInputValue ReadValue(string inputId, SimulationInputValueKind kind)
        {
            for (int i = 0; i < m_Frame.Input.Values.Count; i++)
            {
                if (!string.Equals(m_Frame.Input.Values[i].InputId, inputId, StringComparison.Ordinal))
                    continue;
                if (m_Frame.Input.Values[i].Kind != kind)
                    throw new InvalidOperationException($"Input '{inputId}' is '{m_Frame.Input.Values[i].Kind}', expected '{kind}'.");
                return m_Frame.Input.Values[i];
            }
            throw new InvalidOperationException($"Tick input does not contain required value '{inputId}'.");
        }

    }
}

