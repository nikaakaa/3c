using System;
using System.Collections.Generic;
using Fantasy;
using ThirdPersonSimulation;
using ThirdPersonSimulation.ServerAuthoritative;

namespace ThirdPersonGameplay.Networking.ServerAuthoritative
{
    internal readonly struct ServerAuthoritativePredictionLivenessResult
    {
        public ServerAuthoritativePredictionLivenessResult(bool success, string code, string message)
        {
            Success = success;
            Code = code ?? string.Empty;
            Message = message ?? string.Empty;
        }

        public bool Success { get; }
        public string Code { get; }
        public string Message { get; }
    }

    internal readonly struct ServerAuthoritativePredictionEvidenceReport
    {
        public ServerAuthoritativePredictionEvidenceReport(
            bool available,
            string detail,
            ulong authorityEstimate,
            ulong confirmedInputSequence,
            ulong snapshotAgeTicks,
            bool success)
        {
            Available = available;
            Detail = detail ?? string.Empty;
            AuthorityEstimate = authorityEstimate;
            ConfirmedInputSequence = confirmedInputSequence;
            SnapshotAgeTicks = snapshotAgeTicks;
            Success = success;
        }

        public bool Available { get; }
        public string Detail { get; }
        public ulong AuthorityEstimate { get; }
        public ulong ConfirmedInputSequence { get; }
        public ulong SnapshotAgeTicks { get; }
        public bool Success { get; }
    }

    internal readonly struct ServerAuthoritativePredictionObservationResult
    {
        public ServerAuthoritativePredictionObservationResult(
            AuthoritativeObservationBatch batch,
            ServerAuthoritativePredictionEvidenceReport report)
        {
            Batch = batch ?? throw new ArgumentNullException(nameof(batch));
            Report = report;
        }

        public AuthoritativeObservationBatch Batch { get; }
        public ServerAuthoritativePredictionEvidenceReport Report { get; }
    }

    internal sealed class ServerAuthoritativePredictionEvidenceModule
    {
        readonly ServerAuthoritativeProcessIdentity m_Process;
        readonly ServerAuthoritativeModelPolicy m_Policy;
        readonly int m_ReliableCapacity;
        readonly Queue<AuthoritativeActorBaseline> m_Baselines = new Queue<AuthoritativeActorBaseline>();
        readonly Queue<RemotePresentationBatch> m_Remote = new Queue<RemotePresentationBatch>();
        readonly Queue<RemotePresentationBatch> m_Reliable = new Queue<RemotePresentationBatch>();
        AuthoritativeInputAck m_LatestAck;
        ulong m_ReceiveSequence;
        ulong m_CommittedEventHorizon;
        ulong m_AuthorityClockAnchorTick;
        long m_AuthorityClockAnchorMicros;
        ulong m_RemoteBodyCount;
        ulong m_LastRemoteBodyTick;
        ulong m_LastReliableEventSequence;
        ulong m_DataPlaneReadySourceTick;
        ulong m_LastSnapshotSourceTick;
        ulong m_LastStreamEvidenceSourceTick;
        ulong m_PreviousCommandPacketCount;
        ulong m_PreviousCommandPayloadBytes;
        ulong m_PreviousSnapshotPacketCount;
        ulong m_PreviousSnapshotPayloadBytes;
        bool m_HasAuthorityClockAnchor;
        bool m_AuthorityClockRunning;

        public ServerAuthoritativePredictionEvidenceModule(
            ServerAuthoritativeProcessIdentity process,
            ServerAuthoritativeModelPolicy policy,
            int reliableCapacity)
        {
            m_Process = process;
            m_Policy = policy ?? throw new ArgumentNullException(nameof(policy));
            m_ReliableCapacity = reliableCapacity > 0
                ? reliableCapacity
                : throw new ArgumentOutOfRangeException(nameof(reliableCapacity));
        }

        public void AcceptDataPlaneReady(ulong authorityTick, ulong sourceTick, long clockMicros)
        {
            if (sourceTick == 0)
                throw new ArgumentOutOfRangeException(nameof(sourceTick));
            ObserveAuthorityClock(authorityTick, clockMicros, authorityTick > 0);
            m_DataPlaneReadySourceTick = sourceTick;
        }

        public void AcceptCheckpoint(
            ServerAuthoritativeCheckpointResult result,
            ulong sourceTick,
            long clockMicros)
        {
            if (result.Kind != ServerAuthoritativeCheckpointResultKind.Accepted)
                throw new InvalidOperationException("Prediction evidence accepts only reconstructed checkpoints.");
            m_LatestAck = result.Ack ?? throw new InvalidOperationException("Reconstructed checkpoint omitted owner acknowledgement.");
            m_Baselines.Enqueue(result.Baseline ?? throw new InvalidOperationException("Reconstructed checkpoint omitted owner baseline."));
            if (result.Remote != null)
                m_Remote.Enqueue(result.Remote);
            m_LastSnapshotSourceTick = sourceTick;
            ObserveAuthorityClock(result.Ack.AuthorityTick.Value, clockMicros, true);
        }

        public int AcceptReliableEvents(
            G2C_ServerAuthoritativeReliableGameplayEventBatch message,
            ActorId remoteActor)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));
            if (!remoteActor.IsValid || remoteActor == m_Process.ActorId)
                throw new ArgumentException("Remote ActorId is invalid.", nameof(remoteActor));
            if (message.Events == null || m_Reliable.Count + message.Events.Count > m_ReliableCapacity)
                throw new InvalidOperationException("Prediction reliable event queue overflowed.");
            int payloadBytes = 0;
            for (int i = 0; i < message.Events.Count; i++)
            {
                ServerAuthoritativeReliableGameplayEventMessage encoded = message.Events[i] ??
                    throw new InvalidOperationException("Reliable event is missing.");
                if (!string.Equals(encoded.ActorId, remoteActor.Value, StringComparison.Ordinal) ||
                    encoded.Payload == null || encoded.PayloadLength != encoded.Payload.Length)
                {
                    throw new InvalidOperationException("Reliable event identity or payload boundary is invalid.");
                }
                RemotePresentationBatch batch = ServerAuthoritativeEgressCodec.ReadRemotePresentation(encoded.Payload);
                if (batch.ActorId != remoteActor || batch.ReliableEvents.Count != 1 ||
                    batch.ReliableEvents[0].Header.Sequence != encoded.EventSequence ||
                    batch.ReliableEvents[0].Header.Tick.Value != encoded.AuthorityTick ||
                    !string.Equals(batch.ReliableEvents[0].Header.EventId.ToString(), encoded.EventId, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("Reliable event metadata does not match its canonical payload.");
                }
                if (encoded.EventSequence <= m_LastReliableEventSequence)
                    throw new InvalidOperationException("Reliable event sequence duplicated or regressed.");
                m_LastReliableEventSequence = encoded.EventSequence;
                payloadBytes = checked(payloadBytes + encoded.Payload.Length);
                m_Reliable.Enqueue(batch);
            }
            return payloadBytes;
        }

        public ServerAuthoritativePredictionLivenessResult EvaluateLiveness(ulong sourceTick, int timeoutTicks)
        {
            if (m_DataPlaneReadySourceTick == 0)
                return new ServerAuthoritativePredictionLivenessResult(true, string.Empty, string.Empty);
            ulong last = Math.Max(m_DataPlaneReadySourceTick, m_LastSnapshotSourceTick);
            if (sourceTick <= last + (ulong)timeoutTicks)
                return new ServerAuthoritativePredictionLivenessResult(true, string.Empty, string.Empty);
            return new ServerAuthoritativePredictionLivenessResult(
                false,
                "server_authoritative_snapshot_liveness_failed",
                $"Prediction received no snapshot for '{sourceTick - last}' source ticks.");
        }

        public ServerAuthoritativePredictionObservationResult Drain(
            SimulationTickSourceIdentity source,
            ActorId remoteActor,
            ServerAuthoritativePredictionDatagramMetrics datagram,
            ServerAuthoritativeCheckpointMetrics checkpoint)
        {
            var baselines = new List<AuthoritativeActorBaseline>(1);
            AuthoritativeActorBaseline latestBaseline = null;
            while (m_Baselines.Count > 0)
            {
                AuthoritativeActorBaseline candidate = m_Baselines.Dequeue();
                if (latestBaseline != null && candidate.AuthorityTick.CompareTo(latestBaseline.AuthorityTick) <= 0)
                    throw new InvalidOperationException("Prediction baseline queue is not strictly increasing by authority Tick.");
                latestBaseline = candidate;
            }
            if (latestBaseline != null)
                baselines.Add(latestBaseline);
            var bodies = new List<CharacterBodySample>();
            var samples = new List<PresentationCommand>();
            var events = new List<ServerAuthoritativeReliableEvent>();
            while (m_Remote.Count > 0)
            {
                RemotePresentationBatch batch = m_Remote.Dequeue();
                bodies.AddRange(batch.BodySamples);
                samples.AddRange(batch.SampleCommands);
            }
            while (m_Reliable.Count > 0)
                events.AddRange(m_Reliable.Dequeue().ReliableEvents);
            if (bodies.Count > 0)
            {
                m_RemoteBodyCount = checked(m_RemoteBodyCount + (ulong)bodies.Count);
                m_LastRemoteBodyTick = bodies[bodies.Count - 1].Tick.Value;
            }
            ulong authorityEstimate = EstimateAuthorityTick(ClockMicros());
            var batchResult = new AuthoritativeObservationBatch(
                ++m_ReceiveSequence,
                authorityEstimate,
                m_LatestAck,
                baselines,
                new[] { new RemotePresentationBatch(remoteActor, bodies, samples, events, false) });
            return new ServerAuthoritativePredictionObservationResult(
                batchResult,
                BuildReport(source.SourceTick, authorityEstimate, remoteActor, bodies.Count, datagram, checkpoint));
        }

        public void AcknowledgeRemoteEvents(ulong eventHorizon)
        {
            if (eventHorizon < m_CommittedEventHorizon)
                throw new InvalidOperationException("Committed remote reliable event horizon regressed.");
            m_CommittedEventHorizon = eventHorizon;
        }

        ServerAuthoritativePredictionEvidenceReport BuildReport(
            ulong sourceTick,
            ulong authorityEstimate,
            ActorId remoteActor,
            int receivedRemoteBodies,
            ServerAuthoritativePredictionDatagramMetrics datagram,
            ServerAuthoritativeCheckpointMetrics checkpoint)
        {
            if (m_LatestAck == null)
                return default;
            ulong interval = checked((ulong)m_Policy.SimulationTickRate * 5UL);
            if (m_LastStreamEvidenceSourceTick != 0 && sourceTick < m_LastStreamEvidenceSourceTick + interval)
                return default;
            float elapsedSeconds = m_LastStreamEvidenceSourceTick == 0
                ? Math.Max(1f, sourceTick / (float)m_Policy.SimulationTickRate)
                : (sourceTick - m_LastStreamEvidenceSourceTick) / (float)m_Policy.SimulationTickRate;
            float commandPacketsPerSecond = (datagram.CommandPackets - m_PreviousCommandPacketCount) / elapsedSeconds;
            float commandBytesPerSecond = (datagram.CommandPayloadBytes - m_PreviousCommandPayloadBytes) / elapsedSeconds;
            float snapshotPacketsPerSecond = (datagram.SnapshotPackets - m_PreviousSnapshotPacketCount) / elapsedSeconds;
            float snapshotBytesPerSecond = (datagram.SnapshotPayloadBytes - m_PreviousSnapshotPayloadBytes) / elapsedSeconds;
            m_LastStreamEvidenceSourceTick = sourceTick;
            m_PreviousCommandPacketCount = datagram.CommandPackets;
            m_PreviousCommandPayloadBytes = datagram.CommandPayloadBytes;
            m_PreviousSnapshotPacketCount = datagram.SnapshotPackets;
            m_PreviousSnapshotPayloadBytes = datagram.SnapshotPayloadBytes;
            ulong ackTick = m_LatestAck.AuthorityTick.Value;
            ulong snapshotAge = authorityEstimate - Math.Min(authorityEstimate, ackTick);
            ulong baselineHits = datagram.SnapshotPackets - Math.Min(
                datagram.SnapshotPackets,
                checkpoint.BaselineMisses + checkpoint.ReconstructionFailures);
            string detail =
                $"role={m_Process.Role};actor={m_Process.ActorId};sourceTick={sourceTick};authorityEstimate={authorityEstimate};ackTick={ackTick};confirmedInput={m_LatestAck.ConfirmedInputSequence};snapshot={checkpoint.LatestSnapshotSequence};remoteActor={remoteActor};receivedRemoteBodies={receivedRemoteBodies};remoteBodyTotal={m_RemoteBodyCount};remoteBodyLatestTick={m_LastRemoteBodyTick};commandPackets={datagram.CommandPackets};commandPayloadBytes={datagram.CommandPayloadBytes};commandPacketsPerSecond={commandPacketsPerSecond:0.##};commandBytesPerSecond={commandBytesPerSecond:0.##};snapshotPackets={datagram.SnapshotPackets};snapshotPayloadBytes={datagram.SnapshotPayloadBytes};snapshotPacketsPerSecond={snapshotPacketsPerSecond:0.##};snapshotBytesPerSecond={snapshotBytesPerSecond:0.##};sequenceGaps={datagram.SequenceGaps};duplicates={datagram.DuplicatePackets};outOfOrder={datagram.OutOfOrderPackets};baselineHits={baselineHits};baselineMisses={checkpoint.BaselineMisses};reconstructionFailures={checkpoint.ReconstructionFailures};snapshotAgeTicks={snapshotAge}";
            return new ServerAuthoritativePredictionEvidenceReport(
                true,
                detail,
                authorityEstimate,
                m_LatestAck.ConfirmedInputSequence,
                snapshotAge,
                checkpoint.ReconstructionFailures == 0);
        }

        void ObserveAuthorityClock(ulong authorityTick, long clockMicros, bool running)
        {
            m_AuthorityClockAnchorTick = authorityTick;
            m_AuthorityClockAnchorMicros = clockMicros;
            m_HasAuthorityClockAnchor = true;
            m_AuthorityClockRunning |= running;
        }

        ulong EstimateAuthorityTick(long nowMicros)
        {
            if (!m_HasAuthorityClockAnchor || !m_AuthorityClockRunning || nowMicros <= m_AuthorityClockAnchorMicros)
                return m_HasAuthorityClockAnchor ? m_AuthorityClockAnchorTick : 0;
            ulong elapsedMicros = checked((ulong)(nowMicros - m_AuthorityClockAnchorMicros));
            ulong wholeSeconds = elapsedMicros / 1000000UL;
            ulong remainingMicros = elapsedMicros % 1000000UL;
            ulong elapsedTicks = checked(
                wholeSeconds * (ulong)m_Policy.SimulationTickRate +
                remainingMicros * (ulong)m_Policy.SimulationTickRate / 1000000UL);
            ulong snapshotInterval = checked((ulong)(m_Policy.SimulationTickRate / m_Policy.SnapshotPacketRate));
            elapsedTicks = Math.Min(elapsedTicks, snapshotInterval);
            return checked(m_AuthorityClockAnchorTick + elapsedTicks);
        }

        static long ClockMicros() => checked(
            System.Diagnostics.Stopwatch.GetTimestamp() * 1000000L /
            System.Diagnostics.Stopwatch.Frequency);
    }
}
