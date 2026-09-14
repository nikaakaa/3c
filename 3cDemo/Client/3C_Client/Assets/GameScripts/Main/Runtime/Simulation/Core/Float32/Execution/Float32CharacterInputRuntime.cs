using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ThirdPersonSimulation
{
    internal sealed class Float32CharacterInputRuntime
    {
        readonly IFloat32AbilityDomainStatePort m_DomainState;
        readonly ReadOnlyCollection<string> m_RequestIds;

        public Float32CharacterInputRuntime(
            IFloat32AbilityDomainStatePort domainState,
            IEnumerable<string> requestIds)
        {
            m_DomainState = domainState ?? throw new ArgumentNullException(nameof(domainState));
            var values = new List<string>(requestIds ?? Array.Empty<string>());
            values.Sort(StringComparer.Ordinal);
            for (int i = 0; i < values.Count; i++)
            {
                if (string.IsNullOrEmpty(values[i]) || i > 0 && string.Equals(values[i - 1], values[i], StringComparison.Ordinal))
                    throw new ArgumentException("Float32 Character Input request identities are invalid or duplicated.", nameof(requestIds));
            }
            m_RequestIds = values.AsReadOnly();
        }

        public IReadOnlyList<string> RequestIds => m_RequestIds;

        public void ApplyRequests(IReadOnlyList<SimulationInputRequest> requests)
        {
            requests ??= Array.Empty<SimulationInputRequest>();
            for (int requestIndex = 0; requestIndex < m_RequestIds.Count; requestIndex++)
            {
                string requestId = m_RequestIds[requestIndex];
                SimulationInputRequestState state = m_DomainState.GetInputRequest(requestId);
                if (state.IsValid && state.ExpireTick < m_DomainState.Tick.Value)
                    state = default;
                for (int inputIndex = 0; inputIndex < requests.Count; inputIndex++)
                {
                    SimulationInputRequest request = requests[inputIndex];
                    if (!string.Equals(request.RequestId, requestId, StringComparison.Ordinal))
                        continue;
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
                }
                m_DomainState.SetInputRequest(requestId, state);
            }
        }
    }
}
