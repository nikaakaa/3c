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
            m_ActionActivationRequests.Clear();
            if (actionActivationRequests != null)
            {
                if (m_ActionActivationRequests.Capacity < actionActivationRequests.Count)
                    m_ActionActivationRequests.Capacity = actionActivationRequests.Count;
                m_ActionActivationRequests.AddRange(actionActivationRequests);
            }

            m_ActionInstances.Clear();
            if (actionInstances != null)
            {
                if (m_ActionInstances.Capacity < actionInstances.Count)
                    m_ActionInstances.Capacity = actionInstances.Count;
                m_ActionInstances.AddRange(actionInstances);
            }

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

        public void SetActionActivationRequests(IReadOnlyList<SimulationActionActivationRequestState> requests)
        {
            RequireActive();
            if (Same(m_ActionActivationRequests, requests))
                return;
            m_ActivationRequestsDirty = true;
            m_ActionActivationRequests.Clear();
            if (requests != null)
                m_ActionActivationRequests.AddRange(requests);
        }

        public IReadOnlyList<Float32ActionInstanceState> GetActionInstances()
        {
            RequireActive();
            return m_ActionInstances;
        }

        public void SetActionInstances(IReadOnlyList<Float32ActionInstanceState> actions)
        {
            RequireActive();
            if (Same(m_ActionInstances, actions))
                return;
            m_InstancesDirty = true;
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

        internal bool AreActionActivationRequestsUnchanged => !m_ActivationRequestsDirty;

        internal bool AreActionInstancesUnchanged => !m_InstancesDirty;

        internal void Restore(
            IReadOnlyList<SimulationActionActivationRequestState> actionActivationRequests,
            IReadOnlyList<Float32ActionInstanceState> actionInstances,
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
                throw new ObjectDisposedException(nameof(Float32CharacterActionRuntimeState));
        }

        static bool Same<T>(IReadOnlyList<T> left, IReadOnlyList<T> right)
        {
            if (left == null || right == null)
                return left == right;
            if (left.Count != right.Count)
                return false;
            for (int i = 0; i < left.Count; i++)
            {
                if (!EqualityComparer<T>.Default.Equals(left[i], right[i]))
                    return false;
            }
            return true;
        }
    }
}
