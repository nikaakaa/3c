using System;
using System.Collections.Generic;
using System.Net;
using Fantasy;
using ThirdPersonSimulation;
using ThirdPersonSimulation.ServerAuthoritative;
using ThirdPersonSimulation.ServerAuthoritative.Transport;

namespace ThirdPersonGameplay.Networking.ServerAuthoritative
{
    internal enum ServerAuthoritativePredictionDatagramEventKind : byte
    {
        DataPlaneReady = 1,
        Snapshot = 2
    }

    internal readonly struct ServerAuthoritativePredictionDatagramEvent
    {
        public ServerAuthoritativePredictionDatagramEvent(
            ServerAuthoritativePredictionDatagramEventKind kind,
            ulong authorityTick,
            SnapshotDatagram snapshot)
        {
            Kind = kind;
            AuthorityTick = authorityTick;
            Snapshot = snapshot;
        }

        public ServerAuthoritativePredictionDatagramEventKind Kind { get; }
        public ulong AuthorityTick { get; }
        public SnapshotDatagram Snapshot { get; }
    }

    internal readonly struct ServerAuthoritativePredictionDatagramMetrics
    {
        public ServerAuthoritativePredictionDatagramMetrics(
            ulong commandPackets,
            ulong commandPayloadBytes,
            ulong snapshotPackets,
            ulong snapshotPayloadBytes,
            ulong sequenceGaps,
            ulong duplicatePackets,
            ulong outOfOrderPackets)
        {
            CommandPackets = commandPackets;
            CommandPayloadBytes = commandPayloadBytes;
            SnapshotPackets = snapshotPackets;
            SnapshotPayloadBytes = snapshotPayloadBytes;
            SequenceGaps = sequenceGaps;
            DuplicatePackets = duplicatePackets;
            OutOfOrderPackets = outOfOrderPackets;
        }

        public ulong CommandPackets { get; }
        public ulong CommandPayloadBytes { get; }
        public ulong SnapshotPackets { get; }
        public ulong SnapshotPayloadBytes { get; }
        public ulong SequenceGaps { get; }
        public ulong DuplicatePackets { get; }
        public ulong OutOfOrderPackets { get; }
    }

    internal sealed class ServerAuthoritativeDatagramChannelModule
    {
        readonly ServerAuthoritativeDatagramEndpoint m_Endpoint;
        readonly Queue<ServerAuthoritativePredictionDatagramEvent> m_PredictionEvents =
            new Queue<ServerAuthoritativePredictionDatagramEvent>();
        readonly List<CanonicalInputSample> m_CommandHistory = new List<CanonicalInputSample>(4);
        ServerAuthoritativeDataPlaneTicketMessage m_PredictionTicket;
        ServerAuthoritativeDatagramIdentity m_PredictionIdentity;
        ulong m_SendPacketSequence;
        ulong m_LastReceivePacketSequence;
        ulong m_LatestSnapshotSequence;
        ulong m_CommandPacketCount;
        ulong m_CommandPayloadBytes;
        ulong m_SnapshotPacketCount;
        ulong m_SnapshotPayloadBytes;
        ulong m_SequenceGaps;
        ulong m_DuplicatePackets;
        ulong m_OutOfOrderPackets;
        int m_HelloRetryTicks;
        bool m_PredictionReady;
        bool m_Disposed;

        public ServerAuthoritativeDatagramChannelModule(
            ServerAuthoritativeDataPlaneLaunch launch,
            int queueCapacity,
            int maxDatagramBytes)
        {
            m_Endpoint = new ServerAuthoritativeDatagramEndpoint(
                launch.BindEndPoint,
                queueCapacity,
                maxDatagramBytes);
        }

        public IPEndPoint LocalEndPoint => m_Endpoint.LocalEndPoint;
        public int ReceiveQueueDepth => m_Endpoint.ReceiveQueueDepth;
        public int SendQueueDepth => m_Endpoint.SendQueueDepth;
        public bool IsFailed => m_Endpoint.IsFailed;
        public bool IsPredictionReady => m_PredictionReady;
        public string PredictionTicketId => m_PredictionTicket?.TicketId ?? string.Empty;

        public void BindRemote(ServerAuthoritativeDatagramIdentity identity, IPEndPoint remoteEndPoint) =>
            m_Endpoint.BindRemote(identity, remoteEndPoint);
        public void RevokeRemote(ServerAuthoritativeDatagramIdentity identity) => m_Endpoint.RevokeRemote(identity);
        public void EnqueueSend(ServerAuthoritativeDatagramPacket packet) => m_Endpoint.EnqueueSend(packet);
        public void PumpSend() => m_Endpoint.PumpSend();
        public bool TryReceive(out ServerAuthoritativeReceivedDatagram datagram) => m_Endpoint.TryReceive(out datagram);
        public void ThrowIfUnavailable() => m_Endpoint.ThrowIfUnavailable();
        public ServerAuthoritativeDatagramMetrics CaptureMetrics() => m_Endpoint.CaptureMetrics();

        public void AcceptPredictionTicket(
            ServerAuthoritativeDataPlaneTicketMessage ticket,
            ServerAuthoritativeDatagramIdentity identity,
            IPEndPoint authorityEndPoint,
            long clockMicros)
        {
            if (ticket == null)
                throw new ArgumentNullException(nameof(ticket));
            if (m_PredictionTicket != null)
                throw new InvalidOperationException("Prediction received more than one data-plane ticket.");
            if (authorityEndPoint == null)
                throw new ArgumentNullException(nameof(authorityEndPoint));
            m_PredictionTicket = ticket;
            m_PredictionIdentity = identity;
            m_Endpoint.BindRemote(identity, authorityEndPoint);
            SendHello(clockMicros);
        }

        public void PumpPrediction(long clockMicros)
        {
            m_Endpoint.ThrowIfUnavailable();
            while (m_Endpoint.TryReceive(out ServerAuthoritativeReceivedDatagram received))
            {
                ServerAuthoritativeDatagramPacket packet = received.Packet;
                if (!packet.Header.Identity.Equals(m_PredictionIdentity))
                    continue;
                if (packet.Header.PacketSequence <= m_LastReceivePacketSequence)
                {
                    if (packet.Header.PacketSequence == m_LastReceivePacketSequence)
                        m_DuplicatePackets++;
                    else
                        m_OutOfOrderPackets++;
                    continue;
                }
                if (m_LastReceivePacketSequence != 0 && packet.Header.PacketSequence > m_LastReceivePacketSequence + 1)
                    m_SequenceGaps = checked(m_SequenceGaps + packet.Header.PacketSequence - m_LastReceivePacketSequence - 1);
                m_LastReceivePacketSequence = packet.Header.PacketSequence;
                switch (packet.Header.Kind)
                {
                    case ServerAuthoritativeDatagramKind.DataPlaneHelloAck:
                    {
                        DataPlaneHelloAck ack = ServerAuthoritativeDatagramPayloadCodec.ReadHelloAck(packet.CopyPayload());
                        if (m_PredictionTicket == null)
                            throw new InvalidOperationException("Prediction received data-plane acknowledgement before its ticket.");
                        if (m_PredictionReady)
                            continue;
                        m_PredictionReady = true;
                        m_PredictionEvents.Enqueue(new ServerAuthoritativePredictionDatagramEvent(
                            ServerAuthoritativePredictionDatagramEventKind.DataPlaneReady,
                            ack.AuthorityTick,
                            null));
                        break;
                    }
                    case ServerAuthoritativeDatagramKind.Snapshot:
                    {
                        byte[] payload = packet.CopyPayload();
                        m_SnapshotPacketCount++;
                        m_SnapshotPayloadBytes = checked(m_SnapshotPayloadBytes + (ulong)payload.Length);
                        SnapshotDatagram snapshot = ServerAuthoritativeDatagramPayloadCodec.ReadSnapshot(payload);
                        m_PredictionEvents.Enqueue(new ServerAuthoritativePredictionDatagramEvent(
                            ServerAuthoritativePredictionDatagramEventKind.Snapshot,
                            snapshot.AuthorityTick,
                            snapshot));
                        break;
                    }
                    default:
                        throw new InvalidOperationException($"Prediction received unexpected gameplay datagram '{packet.Header.Kind}'.");
                }
            }
            m_Endpoint.PumpSend();
            if (!m_PredictionReady && m_PredictionTicket != null && ++m_HelloRetryTicks >= 15)
            {
                m_HelloRetryTicks = 0;
                SendHello(clockMicros);
            }
        }

        public bool TryTakePredictionEvent(out ServerAuthoritativePredictionDatagramEvent value)
        {
            if (m_PredictionEvents.Count == 0)
            {
                value = default;
                return false;
            }
            value = m_PredictionEvents.Dequeue();
            return true;
        }

        public void SendPredictionCommand(OwnerCanonicalInputBatch input, ServerAuthoritativeModelPolicy policy)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));
            if (policy == null)
                throw new ArgumentNullException(nameof(policy));
            var sample = new CanonicalInputSample(input.SourceTick, input.InputSequence, input.Input);
            AppendCommandHistory(sample);
            ulong interval = checked((ulong)(policy.SimulationTickRate / policy.CommandPacketRate));
            if (input.SourceTick != 1 && input.SourceTick % interval != 0)
                return;
            var command = new CommandDatagram(m_LatestSnapshotSequence, m_LatestSnapshotSequence, m_CommandHistory);
            byte[] payload = ServerAuthoritativeDatagramPayloadCodec.Write(command);
            SendPacket(ServerAuthoritativeDatagramKind.Command, payload);
            m_CommandPacketCount++;
            m_CommandPayloadBytes = checked(m_CommandPayloadBytes + (ulong)payload.Length);
            m_Endpoint.PumpSend();
        }

        public void AcceptLatestSnapshotSequence(ulong value)
        {
            if (value < m_LatestSnapshotSequence)
                throw new InvalidOperationException("Prediction latest snapshot sequence regressed.");
            m_LatestSnapshotSequence = value;
        }

        public ServerAuthoritativePredictionDatagramMetrics CapturePredictionMetrics() =>
            new ServerAuthoritativePredictionDatagramMetrics(
                m_CommandPacketCount,
                m_CommandPayloadBytes,
                m_SnapshotPacketCount,
                m_SnapshotPayloadBytes,
                m_SequenceGaps,
                m_DuplicatePackets,
                m_OutOfOrderPackets);

        void AppendCommandHistory(CanonicalInputSample sample)
        {
            if (m_CommandHistory.Count > 0 && sample.InputSequence <= m_CommandHistory[0].InputSequence)
                throw new InvalidOperationException("Prediction command input sequence duplicated or regressed.");
            while (m_CommandHistory.Count > 0 &&
                   m_CommandHistory[0].TargetAuthorityTick >= sample.TargetAuthorityTick)
            {
                m_CommandHistory.RemoveAt(0);
            }
            m_CommandHistory.Insert(0, sample);
            if (m_CommandHistory.Count > 4)
                m_CommandHistory.RemoveAt(4);
        }

        void SendHello(long clockMicros)
        {
            if (m_PredictionTicket == null)
                return;
            SendPacket(
                ServerAuthoritativeDatagramKind.DataPlaneHello,
                ServerAuthoritativeDatagramPayloadCodec.Write(new DataPlaneHello(
                    m_PredictionTicket.TicketId,
                    m_PredictionTicket.Nonce,
                    clockMicros)));
            m_Endpoint.PumpSend();
        }

        void SendPacket(ServerAuthoritativeDatagramKind kind, byte[] payload)
        {
            var header = new ServerAuthoritativeDatagramHeader(
                m_PredictionIdentity,
                kind,
                ++m_SendPacketSequence,
                payload.Length);
            m_Endpoint.EnqueueSend(new ServerAuthoritativeDatagramPacket(header, payload));
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            m_PredictionEvents.Clear();
            m_CommandHistory.Clear();
            m_PredictionTicket = null;
            m_Endpoint.Dispose();
        }
    }
}
