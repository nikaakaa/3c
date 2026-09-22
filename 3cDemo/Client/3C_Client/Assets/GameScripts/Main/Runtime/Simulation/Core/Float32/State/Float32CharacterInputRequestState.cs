using System;
using System.Collections.Generic;

namespace ThirdPersonSimulation
{
    internal sealed class Float32CharacterInputRequestState : IFloat32InputRequestStatePort
    {
        SimulationTick m_Tick;
        readonly Dictionary<string, SimulationInputRequestState> m_Requests;
        KeyValuePair<string, SimulationInputRequestState>[] m_BaseRequests;
        bool m_Dirty;
        bool m_Disposed;

        public Float32CharacterInputRequestState()
        {
            m_Requests = new Dictionary<string, SimulationInputRequestState>(StringComparer.Ordinal);
        }

        public Float32CharacterInputRequestState Restart(
            SimulationTick tick,
            KeyValuePair<string, SimulationInputRequestState>[] requests)
        {
            if (!tick.IsValid)
                throw new ArgumentException("Float32 Character input request state tick is invalid.", nameof(tick));
            m_Tick = tick;
            m_BaseRequests = requests;
            m_Requests.Clear();
            m_Dirty = false;
            if (requests != null)
            {
                for (int i = 0; i < requests.Length; i++)
                {
                    if (requests[i].Key == null ||
                        !m_Requests.TryAdd(requests[i].Key, requests[i].Value))
                        throw new ArgumentException("Float32 Character input request identities are null or duplicated.", nameof(requests));
                }
            }
            m_Disposed = false;
            return this;
        }

        public SimulationTick Tick => m_Tick;

        public SimulationInputRequestState GetInputRequest(string requestId)
        {
            RequireActive();
            return m_Requests.TryGetValue(requestId ?? string.Empty, out SimulationInputRequestState state)
                ? state
                : default;
        }

        public void SetInputRequest(string requestId, SimulationInputRequestState state)
        {
            RequireActive();
            string identity = requestId ?? string.Empty;
            if (m_Requests.TryGetValue(identity, out SimulationInputRequestState existing) &&
                existing.Equals(state))
                return;
            m_Requests[identity] = state;
            m_Dirty = !MatchesBaseRequests();
        }

        internal KeyValuePair<string, SimulationInputRequestState>[] Capture()
        {
            RequireActive();
            if (m_Requests.Count == 0)
                return Array.Empty<KeyValuePair<string, SimulationInputRequestState>>();
            var requests = new KeyValuePair<string, SimulationInputRequestState>[m_Requests.Count];
            int index = 0;
            foreach (KeyValuePair<string, SimulationInputRequestState> request in m_Requests)
                requests[index++] = request;
            return requests;
        }

        internal bool IsUnchanged => !m_Dirty;

        bool MatchesBaseRequests()
        {
            int expectedCount = m_BaseRequests?.Length ?? 0;
            if (m_Requests.Count != expectedCount)
                return false;
            for (int i = 0; i < expectedCount; i++)
            {
                KeyValuePair<string, SimulationInputRequestState> request = m_BaseRequests[i];
                if (!m_Requests.TryGetValue(request.Key, out SimulationInputRequestState value) ||
                    !value.Equals(request.Value))
                    return false;
            }
            return true;
        }

        internal void Dispose()
        {
            m_Disposed = true;
        }

        void RequireActive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(Float32CharacterInputRequestState));
        }
    }

    internal sealed class InputRequestKeyComparer : IComparer<KeyValuePair<string, SimulationInputRequestState>>
    {
        public static readonly InputRequestKeyComparer Instance = new();

        public int Compare(
            KeyValuePair<string, SimulationInputRequestState> left,
            KeyValuePair<string, SimulationInputRequestState> right) =>
            string.CompareOrdinal(left.Key, right.Key);
    }
}
