using System;
using System.Collections.Generic;
using ThirdPersonSimulation;

namespace ThirdPersonSimulation.ServerAuthoritative
{
    internal sealed class ServerAuthoritativePredictionConfirmationState
    {
        readonly int m_RequestCapacity;
        SortedDictionary<ulong, SimulationInputRequest> m_PendingRequests =
            new SortedDictionary<ulong, SimulationInputRequest>();

        public ServerAuthoritativePredictionConfirmationState(int requestCapacity)
        {
            if (requestCapacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(requestCapacity));
            m_RequestCapacity = requestCapacity;
        }

        public ulong ConfirmedInputSequence { get; private set; }
        public ServerAuthoritativeEventHorizon ConfirmedEventHorizon { get; private set; }
        public ulong LastAuthorityAckTick { get; private set; }
        public ulong LastBaselineTick { get; private set; }
        public ulong LastAuthorityClockEstimate { get; private set; }
        public int PendingRequestCount => m_PendingRequests.Count;

        public void ObserveAuthorityClock(ulong authorityTickEstimate)
        {
            LastAuthorityClockEstimate = authorityTickEstimate;
        }

        public IReadOnlyList<SimulationInputRequest> ScheduleRequests(
            IReadOnlyList<SimulationInputRequest> incoming,
            bool consume)
        {
            if (incoming == null)
                throw new ArgumentNullException(nameof(incoming));
            for (int i = 0; i < incoming.Count; i++)
                RetainRequest(incoming[i]);
            if (!consume)
                return Array.Empty<SimulationInputRequest>();
            var result = new SimulationInputRequest[m_PendingRequests.Count];
            int index = 0;
            foreach (SimulationInputRequest request in m_PendingRequests.Values)
                result[index++] = request;
            m_PendingRequests.Clear();
            return result;
        }

        public ServerAuthoritativePredictionCorrectionCheckpoint PrepareAck(AuthoritativeInputAck ack)
        {
            if (ack == null)
                throw new ArgumentNullException(nameof(ack));
            if (ack.AuthorityTick.Value < LastAuthorityAckTick)
            {
                throw new InvalidOperationException(
                    $"Authority input ack cursor regressed: actor={ack.ActorId};incomingTick={ack.AuthorityTick.Value};lastTick={LastAuthorityAckTick};incomingSequence={ack.ConfirmedInputSequence};confirmedSequence={ConfirmedInputSequence}.");
            }
            return new ServerAuthoritativePredictionCorrectionCheckpoint(
                Math.Max(ConfirmedInputSequence, ack.ConfirmedInputSequence),
                MergeConfirmationHorizon(ConfirmedEventHorizon, ack.ConfirmedEventHorizon),
                ack.AuthorityTick.Value,
                LastBaselineTick,
                LastAuthorityClockEstimate,
                m_PendingRequests.Values);
        }

        public ServerAuthoritativePredictionCorrectionCheckpoint PrepareBaseline(AuthoritativeActorBaseline baseline)
        {
            if (baseline == null)
                throw new ArgumentNullException(nameof(baseline));
            return new ServerAuthoritativePredictionCorrectionCheckpoint(
                Math.Max(ConfirmedInputSequence, baseline.ConfirmedInputSequence),
                MergeConfirmationHorizon(ConfirmedEventHorizon, baseline.ConfirmedEventHorizon),
                LastAuthorityAckTick,
                Math.Max(LastBaselineTick, baseline.AuthorityTick.Value),
                LastAuthorityClockEstimate,
                m_PendingRequests.Values);
        }

        public ServerAuthoritativePredictionCorrectionCheckpoint Capture() =>
            new ServerAuthoritativePredictionCorrectionCheckpoint(
                ConfirmedInputSequence,
                ConfirmedEventHorizon,
                LastAuthorityAckTick,
                LastBaselineTick,
                LastAuthorityClockEstimate,
                m_PendingRequests.Values);

        public void Restore(ServerAuthoritativePredictionCorrectionCheckpoint checkpoint)
        {
            if (checkpoint == null)
                throw new ArgumentNullException(nameof(checkpoint));
            var requests = new SortedDictionary<ulong, SimulationInputRequest>();
            for (int i = 0; i < checkpoint.PendingRequests.Count; i++)
            {
                SimulationInputRequest request = checkpoint.PendingRequests[i];
                requests.Add(request.Sequence, request);
            }
            ConfirmedInputSequence = checkpoint.ConfirmedInputSequence;
            ConfirmedEventHorizon = checkpoint.ConfirmedEventHorizon;
            LastAuthorityAckTick = checkpoint.LastAuthorityAckTick;
            LastBaselineTick = checkpoint.LastBaselineTick;
            LastAuthorityClockEstimate = checkpoint.LastAuthorityClockEstimate;
            m_PendingRequests = requests;
        }

        static ServerAuthoritativeEventHorizon MergeConfirmationHorizon(
            ServerAuthoritativeEventHorizon current,
            ServerAuthoritativeEventHorizon incoming)
        {
            if (incoming.Sequence > current.Sequence)
                return incoming;
            if (incoming.Sequence < current.Sequence)
                return current;
            if (incoming.Sequence != 0 && !incoming.EventId.Equals(current.EventId))
                throw new InvalidOperationException("Authority confirmation EventId changed at the same sequence.");
            return current;
        }

        void RetainRequest(SimulationInputRequest request)
        {
            if (m_PendingRequests.TryGetValue(request.Sequence, out SimulationInputRequest existing))
            {
                if (!string.Equals(existing.RequestId, request.RequestId, StringComparison.Ordinal) ||
                    existing.SourceTick != request.SourceTick ||
                    existing.ExpireSimulationTick != request.ExpireSimulationTick ||
                    existing.Priority != request.Priority)
                {
                    throw new InvalidOperationException($"Prediction request sequence '{request.Sequence}' changed while pending.");
                }
                return;
            }
            if (m_PendingRequests.Count >= m_RequestCapacity)
                throw new InvalidOperationException("Prediction pending request capacity is exhausted.");
            m_PendingRequests.Add(request.Sequence, request);
        }
    }

    internal sealed class ServerAuthoritativePredictionCorrectionCheckpoint
    {
        public ServerAuthoritativePredictionCorrectionCheckpoint(
            ulong confirmedInputSequence,
            ServerAuthoritativeEventHorizon confirmedEventHorizon,
            ulong lastAuthorityAckTick,
            ulong lastBaselineTick,
            ulong lastAuthorityClockEstimate,
            IEnumerable<SimulationInputRequest> pendingRequests)
        {
            ConfirmedInputSequence = confirmedInputSequence;
            ConfirmedEventHorizon = confirmedEventHorizon;
            LastAuthorityAckTick = lastAuthorityAckTick;
            LastBaselineTick = lastBaselineTick;
            LastAuthorityClockEstimate = lastAuthorityClockEstimate;
            var requests = new List<SimulationInputRequest>();
            if (pendingRequests != null)
                requests.AddRange(pendingRequests);
            PendingRequests = requests.AsReadOnly();
        }

        public ulong ConfirmedInputSequence { get; }
        public ServerAuthoritativeEventHorizon ConfirmedEventHorizon { get; }
        public ulong LastAuthorityAckTick { get; }
        public ulong LastBaselineTick { get; }
        public ulong LastAuthorityClockEstimate { get; }
        public IReadOnlyList<SimulationInputRequest> PendingRequests { get; }
    }
}
