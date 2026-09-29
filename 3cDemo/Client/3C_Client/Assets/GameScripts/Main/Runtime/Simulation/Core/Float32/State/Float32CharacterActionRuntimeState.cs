using System;
using System.Collections.Generic;
using SimulationActionActivationRequestState = ThirdPersonSimulation.SimulationActionActivationRequestState<ThirdPersonSimulation.SimulationActionTargetSnapshot>;

namespace ThirdPersonSimulation
{
    internal sealed class Float32CharacterActionRuntimeState : IFloat32ActionRuntimeStatePort
    {
        readonly List<SimulationActionActivationRequestState> m_ActionActivationRequests;
        readonly List<Float32ActionInstanceState> m_ActionInstances;
        readonly int m_Capacity;
        readonly Dictionary<ulong, int> m_ActionInstanceIndices;
        bool m_ActivationRequestsDirty;
        bool m_InstancesDirty;
        ulong m_ActionEventSequence;
        IReadOnlyList<SimulationActionActivationRequestState> m_BaseActionActivationRequests;
        IReadOnlyList<Float32ActionInstanceState> m_BaseActionInstances;
        bool m_Disposed;

        public Float32CharacterActionRuntimeState(int capacity)
        {
            m_Capacity = capacity;
            m_ActionActivationRequests = new List<SimulationActionActivationRequestState>(capacity);
            m_ActionInstances = new List<Float32ActionInstanceState>(capacity);
            m_ActionInstanceIndices = new Dictionary<ulong, int>(capacity);
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

        public bool TryGetActionInstance(ulong instanceId, out Float32ActionInstanceState action)
        {
            RequireActive();
            if (m_ActionInstanceIndices.TryGetValue(instanceId, out int index))
            {
                action = m_ActionInstances[index];
                return true;
            }
            action = default;
            return false;
        }

        public bool TryFindActionInstanceIndex(ulong instanceId, out int index)
        {
            RequireActive();
            return m_ActionInstanceIndices.TryGetValue(instanceId, out index);
        }

        public void ReplaceActionInstanceAt(int index, Float32ActionInstanceState action)
        {
            RequireActive();
            if (index < 0 || index >= m_ActionInstances.Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            Float32ActionInstanceState previous = m_ActionInstances[index];
            m_ActionInstances[index] = action;
            UpdateActionInstanceIndex(index, previous, action);
            m_InstancesDirty = !Same(m_ActionInstances, m_BaseActionInstances);
        }

        public void AddActionInstance(Float32ActionInstanceState action)
        {
            RequireActive();
            if (m_ActionInstances.Count >= m_Capacity)
                throw new InvalidOperationException("Character action state capacity is exhausted.");
            m_ActionInstances.Add(action);
            UpdateActionInstanceIndex(m_ActionInstances.Count - 1, default, action);
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
                if (requests.Count > m_Capacity)
                    throw new InvalidOperationException("Restored character action state exceeds capacity.");
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
                if (actions.Count > m_Capacity)
                    throw new InvalidOperationException("Restored character action state exceeds capacity.");
                m_ActionInstances.AddRange(actions);
            }
            RebuildActionInstanceIndex();
            m_InstancesDirty = !Same(m_ActionInstances, m_BaseActionInstances);
        }

        void UpdateActionInstanceIndex(
            int index,
            Float32ActionInstanceState previous,
            Float32ActionInstanceState action)
        {
            if (previous.IsValid && previous.InstanceId != action.InstanceId)
                m_ActionInstanceIndices.Remove(previous.InstanceId);
            m_ActionInstanceIndices[action.InstanceId] = index;
        }

        void RebuildActionInstanceIndex()
        {
            m_ActionInstanceIndices.Clear();
            for (int index = 0; index < m_ActionInstances.Count; index++)
            {
                Float32ActionInstanceState action = m_ActionInstances[index];
                if (action.IsValid && !m_ActionInstanceIndices.TryAdd(action.InstanceId, index))
                    throw new InvalidOperationException($"Action instance '{action.InstanceId}' is duplicated.");
            }
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
