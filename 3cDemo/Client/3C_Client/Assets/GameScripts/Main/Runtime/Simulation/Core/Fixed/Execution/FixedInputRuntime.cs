using ThirdPersonSimulation;
using System;
using System.Collections.Generic;

namespace ThirdPersonSimulation.Fixed
{
    internal readonly struct FixedInputRuntime
    {
        readonly FixedAbilityExecutionFrame m_Frame;

        public FixedInputRuntime(
            FixedAbilityExecutionFrame frame)
        {
            m_Frame = frame ?? throw new ArgumentNullException(nameof(frame));
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
            state = m_Frame.InputRequests.GetInputRequest(requestId);
            return state.IsValid && !state.Consumed && state.ExpireTick >= m_Frame.Tick.Value;
        }

        public void ClearRequest(string requestId)
        {
            SimulationInputRequestState state = m_Frame.InputRequests.GetInputRequest(requestId);
            if (state.IsValid && !state.Consumed)
                m_Frame.InputRequests.SetInputRequest(requestId, state.Consume());
        }

        public SimulationInputValue ReadValue(string inputId, SimulationInputValueKind kind) =>
            m_Frame.Input.ReadValue(inputId, kind);

    }
}

