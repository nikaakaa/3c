using System;
using System.Collections.Generic;
using SimulationActionActivationRequestState = ThirdPersonSimulation.SimulationActionActivationRequestState<ThirdPersonSimulation.Fixed.SimulationActionTargetSnapshot>;

namespace ThirdPersonSimulation.Fixed
{
    internal sealed class FixedCharacterActionRuntimeState : IFixedActionRuntimeStatePort
    {
        readonly List<SimulationActionActivationRequestState> m_ActionActivationRequests;
        readonly List<FixedActionInstanceState> m_ActionInstances;
        readonly int m_Capacity;
        bool m_ActivationRequestsDirty;
        bool m_InstancesDirty;
        ulong m_ActionEventSequence;
        IReadOnlyList<SimulationActionActivationRequestState> m_BaseActionActivationRequests;
        IReadOnlyList<FixedActionInstanceState> m_BaseActionInstances;
        bool m_Disposed;

        public FixedCharacterActionRuntimeState(int capacity)
        {
            m_Capacity = capacity;
            m_ActionActivationRequests = new List<SimulationActionActivationRequestState>(capacity);
            m_ActionInstances = new List<FixedActionInstanceState>(capacity);
        }

        public FixedCharacterActionRuntimeState Restart(
            IReadOnlyList<SimulationActionActivationRequestState> actionActivationRequests,
            IReadOnlyList<FixedActionInstanceState> actionInstances,
            ulong actionEventSequence)
        {
            m_BaseActionActivationRequests = actionActivationRequests ?? Array.Empty<SimulationActionActivationRequestState>();
            m_BaseActionInstances = actionInstances ?? Array.Empty<FixedActionInstanceState>();
            ReplaceActivationRequests(actionActivationRequests);
            ReplaceActionInstances(actionInstances);

            m_ActionEventSequence = actionEventSequence;
            m_ActivationRequestsDirty = false;
            m_InstancesDirty = false;
            m_Disposed = false;
            return this;
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

        public IReadOnlyList<FixedActionInstanceState> GetActionInstances()
        {
            RequireActive();
            return m_ActionInstances;
        }

        public void ReplaceActionInstanceAt(int index, FixedActionInstanceState action)
        {
            RequireActive();
            if (index < 0 || index >= m_ActionInstances.Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            m_ActionInstances[index] = action;
            m_InstancesDirty = !Same(m_ActionInstances, m_BaseActionInstances);
        }

        public void AddActionInstance(FixedActionInstanceState action)
        {
            RequireActive();
            if (m_ActionInstances.Count >= m_Capacity)
                throw new InvalidOperationException("Character action state capacity is exhausted.");
            m_ActionInstances.Add(action);
            m_InstancesDirty = !Same(m_ActionInstances, m_BaseActionInstances);
        }

        public void AddActivationRequest(SimulationActionActivationRequestState request)
        {
            RequireActive();
            if (m_ActionActivationRequests.Count >= m_Capacity)
                throw new InvalidOperationException("Character action state capacity is exhausted.");
            m_ActionActivationRequests.Add(request);
            m_ActivationRequestsDirty = !Same(m_ActionActivationRequests, m_BaseActionActivationRequests);
        }

        public void RemoveActivationRequestAt(int index)
        {
            RequireActive();
            if (index < 0 || index >= m_ActionActivationRequests.Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            m_ActionActivationRequests.RemoveAt(index);
            m_ActivationRequestsDirty = !Same(m_ActionActivationRequests, m_BaseActionActivationRequests);
        }

        internal ulong ActionEventSequence
        {
            get
            {
                RequireActive();
                return m_ActionEventSequence;
            }
        }

        internal bool AreActionActivationRequestsUnchanged => !m_ActivationRequestsDirty;

        internal bool AreActionInstancesUnchanged => !m_InstancesDirty;

        internal void Restore(
            IReadOnlyList<SimulationActionActivationRequestState> actionActivationRequests,
            IReadOnlyList<FixedActionInstanceState> actionInstances,
            ulong actionEventSequence)
        {
            RequireActive();
            ReplaceActivationRequests(actionActivationRequests);
            ReplaceActionInstances(actionInstances);
            m_ActionEventSequence = actionEventSequence;
        }

        void ReplaceActivationRequests(IReadOnlyList<SimulationActionActivationRequestState> requests)
        {
            if (Same(m_ActionActivationRequests, requests))
                return;
            m_ActivationRequestsDirty = true;
            m_ActionActivationRequests.Clear();
            if (requests != null)
            {
                if (requests.Count > m_Capacity)
                    throw new InvalidOperationException("Restored character action state exceeds capacity.");
                m_ActionActivationRequests.AddRange(requests);
            }
            m_ActivationRequestsDirty = !Same(m_ActionActivationRequests, m_BaseActionActivationRequests);
        }

        void ReplaceActionInstances(IReadOnlyList<FixedActionInstanceState> actions)
        {
            if (Same(m_ActionInstances, actions))
                return;
            m_InstancesDirty = true;
            m_ActionInstances.Clear();
            if (actions != null)
            {
                if (actions.Count > m_Capacity)
                    throw new InvalidOperationException("Restored character action state exceeds capacity.");
                m_ActionInstances.AddRange(actions);
            }
            m_InstancesDirty = !Same(m_ActionInstances, m_BaseActionInstances);
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

        static bool Same<T>(IReadOnlyList<T> left, IReadOnlyList<T> right)
            where T : struct, IEquatable<T>
        {
            if (left == null || right == null)
                return left == right;
            if (left.Count != right.Count)
                return false;
            for (int i = 0; i < left.Count; i++)
            {
                if (!left[i].Equals(right[i]))
                    return false;
            }
            return true;
        }
    }
}
