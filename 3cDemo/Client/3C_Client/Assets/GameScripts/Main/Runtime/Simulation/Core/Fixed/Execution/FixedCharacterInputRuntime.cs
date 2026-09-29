using System;
using System.Collections.Generic;
using ThirdPersonSimulation;

namespace ThirdPersonSimulation.Fixed
{
    internal sealed class FixedCharacterInputRuntime
    {
        static readonly StringComparer s_RequestIdComparer = StringComparer.Ordinal;

        IFixedInputRequestStatePort m_InputRequests;
        readonly string[] m_RequestIds;

        public FixedCharacterInputRuntime(
            IReadOnlyList<string> requestIds)
        {
            if (requestIds == null)
                throw new ArgumentNullException(nameof(requestIds));
            m_RequestIds = new string[requestIds.Count];
            for (int i = 0; i < requestIds.Count; i++)
                m_RequestIds[i] = requestIds[i];
            Array.Sort(m_RequestIds, StringComparer.Ordinal);
            for (int i = 0; i < m_RequestIds.Length; i++)
            {
                if (string.IsNullOrEmpty(m_RequestIds[i]) ||
                    i > 0 && string.Equals(m_RequestIds[i - 1], m_RequestIds[i], StringComparison.Ordinal))
                    throw new ArgumentException("Fixed Character Input request identities are invalid or duplicated.", nameof(requestIds));
            }
        }

        internal void Begin(IFixedInputRequestStatePort inputRequests)
        {
            m_InputRequests = inputRequests ?? throw new ArgumentNullException(nameof(inputRequests));
        }

        public void ApplyRequests(IReadOnlyList<SimulationInputRequest> requests)
        {
            requests ??= Array.Empty<SimulationInputRequest>();
            for (int requestIndex = 0; requestIndex < m_RequestIds.Length; requestIndex++)
            {
                string requestId = m_RequestIds[requestIndex];
                SimulationInputRequestState state = m_InputRequests.GetInputRequest(requestId);
                if (state.IsValid && state.ExpireTick < m_InputRequests.Tick.Value)
                    state = default;
                m_InputRequests.SetInputRequest(requestId, state);
            }
            for (int inputIndex = 0; inputIndex < requests.Count; inputIndex++)
            {
                SimulationInputRequest request = requests[inputIndex];
                int requestIndex = Array.BinarySearch(m_RequestIds, request.RequestId, s_RequestIdComparer);
                if (requestIndex < 0)
                    continue;
                string requestId = m_RequestIds[requestIndex];
                SimulationInputRequestState state = m_InputRequests.GetInputRequest(requestId);
                if (!state.IsValid || request.Priority > state.Priority ||
                    request.Priority == state.Priority && request.Sequence > state.Sequence)
                {
                    state = new SimulationInputRequestState(
                        request.RequestId,
                        request.Sequence,
                        request.SourceTick,
                        request.ExpireSimulationTick,
                        request.Priority);
                }
                m_InputRequests.SetInputRequest(requestId, state);
            }
        }
    }
}
