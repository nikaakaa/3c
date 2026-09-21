using ThirdPersonSimulation;
using System;
using System.Collections.Generic;

namespace ThirdPersonSimulation.Fixed
{
    internal readonly struct FixedInputRuntime
    {
        readonly FixedAbilityExecutionFrame m_Frame;
        readonly IFixedInputRequestStatePort m_InputRequests;

        public FixedInputRuntime(
            FixedAbilityExecutionFrame frame,
            IFixedInputRequestStatePort inputRequests)
        {
            m_Frame = frame ?? throw new ArgumentNullException(nameof(frame));
            m_InputRequests = inputRequests ?? throw new ArgumentNullException(nameof(inputRequests));
        }

        public void ApplyBlackboardInputBindings(IFixedBlackboardPort blackboard)
        {
            if (blackboard == null)
                throw new ArgumentNullException(nameof(blackboard));
            for (int i = 0; i < m_Frame.Layout.BlackboardInputBindings.Count; i++)
            {
                BlackboardInputStateBinding binding = m_Frame.Layout.BlackboardInputBindings[i];
                blackboard.ProjectBlackboardInput(binding, ReadValue(binding.InputId, (SimulationInputValueKind)binding.InputKind));
            }
        }

        public bool HasRequest(string requestId, out SimulationInputRequestState state)
        {
            if (!m_Frame.Layout.HasInputRequest(requestId))
            {
                state = default;
                return false;
            }
            state = m_InputRequests.GetInputRequest(requestId);
            return state.IsValid && !state.Consumed && state.ExpireTick >= m_Frame.Tick.Value;
        }

        public void ClearRequest(string requestId)
        {
            if (!m_Frame.Layout.HasInputRequest(requestId))
                return;
            SimulationInputRequestState state = m_InputRequests.GetInputRequest(requestId);
            if (state.IsValid && !state.Consumed)
                m_InputRequests.SetInputRequest(requestId, state.Consume());
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

