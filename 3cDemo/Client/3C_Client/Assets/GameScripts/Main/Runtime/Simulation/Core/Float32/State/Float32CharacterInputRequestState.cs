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
            IReadOnlyDictionary<string, SimulationInputRequestState> requests)
        {
            if (!tick.IsValid)
                throw new ArgumentException("Float32 Character input request state tick is invalid.", nameof(tick));
            m_Tick = tick;
            m_Requests = requests == null
                ? new Dictionary<string, SimulationInputRequestState>(StringComparer.Ordinal)
                : new Dictionary<string, SimulationInputRequestState>(requests, StringComparer.Ordinal);
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

        internal Dictionary<string, SimulationInputRequestState> Capture()
        {
            RequireActive();
            return new Dictionary<string, SimulationInputRequestState>(m_Requests, StringComparer.Ordinal);
        }

        internal void Restore(IReadOnlyDictionary<string, SimulationInputRequestState> requests)
        {
            RequireActive();
            m_Requests.Clear();
            if (requests == null)
                return;
            foreach (KeyValuePair<string, SimulationInputRequestState> request in requests)
                m_Requests.Add(request.Key, request.Value);
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
}
