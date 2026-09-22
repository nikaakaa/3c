using System;
using System.Collections.Generic;

namespace ThirdPersonSimulation
{
    internal sealed class Float32CharacterInputRequestState : IFloat32InputRequestStatePort
    {
        readonly SimulationTick m_Tick;
        readonly Dictionary<string, SimulationInputRequestState> m_Requests;
        bool m_Disposed;

        public Float32CharacterInputRequestState(
            SimulationTick tick,
            KeyValuePair<string, SimulationInputRequestState>[] requests)
        {
            if (!tick.IsValid)
                throw new ArgumentException("Float32 Character input request state tick is invalid.", nameof(tick));
            m_Tick = tick;
            m_Requests = new Dictionary<string, SimulationInputRequestState>(
                requests?.Length ?? 0,
                StringComparer.Ordinal);
            if (requests != null)
            {
                for (int i = 0; i < requests.Length; i++)
                {
                    if (requests[i].Key == null ||
                        !m_Requests.TryAdd(requests[i].Key, requests[i].Value))
                        throw new ArgumentException("Float32 Character input request identities are null or duplicated.", nameof(requests));
                }
            }
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
            m_Requests[requestId ?? string.Empty] = state;
        }

        internal KeyValuePair<string, SimulationInputRequestState>[] Capture()
        {
            RequireActive();
            var requests = new KeyValuePair<string, SimulationInputRequestState>[m_Requests.Count];
            int index = 0;
            foreach (KeyValuePair<string, SimulationInputRequestState> request in m_Requests)
                requests[index++] = request;
            return requests;
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
