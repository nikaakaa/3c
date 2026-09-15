using System;
using System.Collections.Generic;
using SimulationActionActivationRequestState = ThirdPersonSimulation.SimulationActionActivationRequestState<ThirdPersonSimulation.Fixed.SimulationActionTargetSnapshot>;

namespace ThirdPersonSimulation.Fixed
{
    internal sealed class FixedCharacterActionRuntimeState : IFixedActionRuntimeStatePort
    {
        readonly List<SimulationActionActivationRequestState> m_ActionActivationRequests;
        readonly List<FixedActionInstanceState> m_ActionInstances;
        ulong m_ActionEventSequence;
        bool m_Disposed;

        public FixedCharacterActionRuntimeState(
            IReadOnlyList<SimulationActionActivationRequestState> actionActivationRequests,
            IReadOnlyList<FixedActionInstanceState> actionInstances,
            ulong actionEventSequence)
        {
            m_ActionActivationRequests = actionActivationRequests == null
                ? new List<SimulationActionActivationRequestState>()
                : new List<SimulationActionActivationRequestState>(actionActivationRequests);
            m_ActionInstances = actionInstances == null
                ? new List<FixedActionInstanceState>()
                : new List<FixedActionInstanceState>(actionInstances);
            m_ActionEventSequence = actionEventSequence;
        }

        public ulong NextActionEventSequence()
        {
            RequireActive();
            m_ActionEventSequence = checked(m_ActionEventSequence + 1UL);
            if (m_ActionEventSequence == 0)
                throw new OverflowException("Action event sequence overflowed.");
            return m_ActionEventSequence;
        }

        public IReadOnlyList<SimulationActionActivationRequestState> GetActionActivationRequests()
        {
            RequireActive();
            return m_ActionActivationRequests;
        }

        public void SetActionActivationRequests(IReadOnlyList<SimulationActionActivationRequestState> requests)
        {
            RequireActive();
            m_ActionActivationRequests.Clear();
            if (requests != null)
                m_ActionActivationRequests.AddRange(requests);
        }

        public IReadOnlyList<FixedActionInstanceState> GetActionInstances()
        {
            RequireActive();
            return m_ActionInstances;
        }

        public void SetActionInstances(IReadOnlyList<FixedActionInstanceState> actions)
        {
            RequireActive();
            m_ActionInstances.Clear();
            if (actions != null)
                m_ActionInstances.AddRange(actions);
        }

        internal ulong ActionEventSequence
        {
            get
            {
                RequireActive();
                return m_ActionEventSequence;
            }
        }

        internal void Restore(
            IReadOnlyList<SimulationActionActivationRequestState> actionActivationRequests,
            IReadOnlyList<FixedActionInstanceState> actionInstances,
            ulong actionEventSequence)
        {
            RequireActive();
            SetActionActivationRequests(actionActivationRequests);
            SetActionInstances(actionInstances);
            m_ActionEventSequence = actionEventSequence;
        }

        internal void Dispose()
        {
            m_Disposed = true;
        }

        void RequireActive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(FixedCharacterActionRuntimeState));
        }
    }
}
