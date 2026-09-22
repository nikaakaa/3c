using System;
using System.Collections.Generic;
using SimulationActionActivationRequestState = ThirdPersonSimulation.SimulationActionActivationRequestState<ThirdPersonSimulation.SimulationActionTargetSnapshot>;

namespace ThirdPersonSimulation
{
    internal sealed class Float32CharacterActionRuntimeState : IFloat32ActionRuntimeStatePort
    {
        readonly List<SimulationActionActivationRequestState> m_ActionActivationRequests;
        readonly List<Float32ActionInstanceState> m_ActionInstances;
        bool m_ActivationRequestsDirty;
        bool m_InstancesDirty;
        ulong m_ActionEventSequence;
        IReadOnlyList<SimulationActionActivationRequestState> m_BaseActionActivationRequests;
        IReadOnlyList<Float32ActionInstanceState> m_BaseActionInstances;
        bool m_Disposed;

        public Float32CharacterActionRuntimeState()
        {
            m_ActionActivationRequests = new List<SimulationActionActivationRequestState>();
            m_ActionInstances = new List<Float32ActionInstanceState>();
        }

        public Float32CharacterActionRuntimeState Restart(
            IReadOnlyList<SimulationActionActivationRequestState> actionActivationRequests,
            IReadOnlyList<Float32ActionInstanceState> actionInstances,
            ulong actionEventSequence)
        {
            m_BaseActionActivationRequests = actionActivationRequests ?? Array.Empty<SimulationActionActivationRequestState>();
            m_BaseActionInstances = actionInstances ?? Array.Empty<Float32ActionInstanceState>();
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

        public IReadOnlyList<Float32ActionInstanceState> GetActionInstances()
        {
            RequireActive();
            return m_ActionInstances;
        }

        public void ReplaceActionInstanceAt(int index, Float32ActionInstanceState action)
        {
            RequireActive();
            if (index < 0 || index >= m_ActionInstances.Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            m_ActionInstances[index] = action;
            m_InstancesDirty = !Same(m_ActionInstances, m_BaseActionInstances);
        }

        public void AddActionInstance(Float32ActionInstanceState action, int capacity)
        {
            RequireActive();
            if (capacity < m_ActionInstances.Count + 1)
                throw new ArgumentOutOfRangeException(nameof(capacity));
            if (m_ActionInstances.Capacity < capacity)
                m_ActionInstances.Capacity = capacity;
            m_ActionInstances.Add(action);
            m_InstancesDirty = !Same(m_ActionInstances, m_BaseActionInstances);
        }

        public void AddActivationRequest(SimulationActionActivationRequestState request, int capacity)
        {
            RequireActive();
            if (capacity < m_ActionActivationRequests.Count + 1)
                throw new ArgumentOutOfRangeException(nameof(capacity));
            if (m_ActionActivationRequests.Capacity < capacity)
                m_ActionActivationRequests.Capacity = capacity;
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
            IReadOnlyList<Float32ActionInstanceState> actionInstances,
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
                if (m_ActionActivationRequests.Capacity < requests.Count)
                    m_ActionActivationRequests.Capacity = requests.Count;
                m_ActionActivationRequests.AddRange(requests);
            }
            m_ActivationRequestsDirty = !Same(m_ActionActivationRequests, m_BaseActionActivationRequests);
        }

        void ReplaceActionInstances(IReadOnlyList<Float32ActionInstanceState> actions)
        {
            if (Same(m_ActionInstances, actions))
                return;
            m_InstancesDirty = true;
            m_ActionInstances.Clear();
            if (actions != null)
            {
                if (m_ActionInstances.Capacity < actions.Count)
                    m_ActionInstances.Capacity = actions.Count;
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
                throw new ObjectDisposedException(nameof(Float32CharacterActionRuntimeState));
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
