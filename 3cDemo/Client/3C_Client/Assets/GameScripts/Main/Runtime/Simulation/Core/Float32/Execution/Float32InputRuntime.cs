using System;
using System.Collections.Generic;

namespace ThirdPersonSimulation
{
    internal readonly struct Float32InputRequestState
    {
        public Float32InputRequestState(
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

        public Float32InputRequestState Consume()
        {
            return IsValid
                ? new Float32InputRequestState(RequestId, Sequence, SourceTick, ExpireTick, Priority, true)
                : this;
        }
    }

    internal sealed class Float32InputRuntime : Float32OperationModule, IFloat32InputPort
    {
        readonly Float32StatePort m_State;
        readonly Float32EvaluationFrame m_Frame;

        public Float32InputRuntime(
            Float32ProgramAccess access,
            Float32StatePort state,
            Float32EvaluationFrame frame)
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
                Float32InputRequestState state = m_State.Get(address.SlotIndex).InputRequest;
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
                        state = new Float32InputRequestState(
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

        public void ApplyBlackboardInputBindings(IFloat32BlackboardPort blackboard)
        {
            if (blackboard == null)
                throw new ArgumentNullException(nameof(blackboard));
            for (int i = 0; i < m_Layout.BlackboardInputBindings.Count; i++)
            {
                BlackboardInputStateBinding binding = m_Layout.BlackboardInputBindings[i];
                blackboard.ProjectBlackboardInput(binding, ReadValue(binding.InputId, (SimulationInputValueKind)binding.InputKind));
            }
        }

        public bool HasRequest(string requestId, out Float32InputRequestState state)
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
            Float32InputRequestState state = m_State.Get(address.SlotIndex).InputRequest;
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
