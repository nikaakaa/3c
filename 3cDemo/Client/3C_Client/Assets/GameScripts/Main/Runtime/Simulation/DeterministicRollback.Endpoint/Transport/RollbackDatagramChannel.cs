using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;

namespace ThirdPersonSimulation.DeterministicRollback
{
    public sealed class RollbackDatagramChannel
    {
        sealed class PendingReliableMessage
        {
            public PendingReliableMessage(RollbackDatagramPacket[] packets, long nextSendTimestamp)
            {
                Packets = packets;
                NextSendTimestamp = nextSendTimestamp;
            }

            public RollbackDatagramPacket[] Packets { get; }
            public long NextSendTimestamp { get; set; }
        }

        sealed class FragmentAssembly
        {
            readonly byte[][] m_Fragments;
            int m_ReceivedCount;
            int m_ReceivedBytes;

            public FragmentAssembly(RollbackDatagramPacket packet)
            {
                Reliable = packet.Reliable;
                TotalPayloadBytes = packet.TotalPayloadBytes;
                m_Fragments = new byte[packet.FragmentCount][];
            }

            public bool Reliable { get; }
            public int TotalPayloadBytes { get; }
            public bool IsComplete => m_ReceivedCount == m_Fragments.Length;

            public void Add(RollbackDatagramPacket packet)
            {
                if (packet.Reliable != Reliable || packet.FragmentCount != m_Fragments.Length ||
                    packet.TotalPayloadBytes != TotalPayloadBytes)
                {
                    throw new InvalidDataException("Rollback message fragment metadata changed during reassembly.");
                }
                if (m_Fragments[packet.FragmentIndex] != null)
                    return;
                byte[] payload = packet.CopyPayload();
                m_Fragments[packet.FragmentIndex] = payload;
                m_ReceivedCount++;
                m_ReceivedBytes = checked(m_ReceivedBytes + payload.Length);
                if (m_ReceivedBytes > TotalPayloadBytes)
                    throw new InvalidDataException("Rollback message fragments exceed the declared payload size.");
            }

            public byte[] Complete()
            {
                if (!IsComplete || m_ReceivedBytes != TotalPayloadBytes)
                    throw new InvalidOperationException("Rollback message reassembly is incomplete.");
                var result = new byte[TotalPayloadBytes];
                int offset = 0;
                for (int i = 0; i < m_Fragments.Length; i++)
                {
                    Buffer.BlockCopy(m_Fragments[i], 0, result, offset, m_Fragments[i].Length);
                    offset += m_Fragments[i].Length;
                }
                return result;
            }
        }

        readonly RollbackDatagramEndpoint m_Endpoint;
        readonly RollbackEndpointDefinition m_Definition;
        readonly string m_LocalPeerId;
        readonly string m_RemotePeerId;
        readonly IPEndPoint m_RemoteEndPoint;
        readonly Dictionary<ulong, PendingReliableMessage> m_PendingReliable = new Dictionary<ulong, PendingReliableMessage>();
        readonly Dictionary<ulong, FragmentAssembly> m_Reassembly = new Dictionary<ulong, FragmentAssembly>();
        readonly Queue<RollbackProtocolEnvelope> m_Received = new Queue<RollbackProtocolEnvelope>();
        readonly HashSet<ulong> m_CompletedSequences = new HashSet<ulong>();
        readonly Queue<ulong> m_CompletedOrder = new Queue<ulong>();
        readonly long m_ResendInterval;
        ulong m_NextDatagramSequence = 1;
        ulong m_NextMessageSequence = 1;

        public RollbackDatagramChannel(
            RollbackDatagramEndpoint endpoint,
            RollbackEndpointDefinition definition,
            string localPeerId,
            string remotePeerId,
            IPEndPoint remoteEndPoint)
        {
            m_Endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
            m_Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            m_LocalPeerId = RollbackEndpointIdentity.Require(localPeerId, nameof(localPeerId));
            m_RemotePeerId = RollbackEndpointIdentity.Require(remotePeerId, nameof(remotePeerId));
            m_RemoteEndPoint = remoteEndPoint == null
                ? throw new ArgumentNullException(nameof(remoteEndPoint))
                : new IPEndPoint(remoteEndPoint.Address, remoteEndPoint.Port);
            m_ResendInterval = Math.Max(1L, Stopwatch.Frequency * definition.ReliableResendMilliseconds / 1000L);
        }

        public string RemotePeerId => m_RemotePeerId;
        public IPEndPoint RemoteEndPoint => new IPEndPoint(m_RemoteEndPoint.Address, m_RemoteEndPoint.Port);
        public int PendingReliableCount => m_PendingReliable.Count;
        public int ReassemblyCount => m_Reassembly.Count;
        public int ReceivedCount => m_Received.Count;

        public bool FitsSingleDatagram(
            IRollbackProtocolPayload payload,
            out int encodedPayloadBytes,
            out int maximumPayloadBytes)
        {
            if (payload == null)
                throw new ArgumentNullException(nameof(payload));
            encodedPayloadBytes = Encode(payload, m_NextMessageSequence).Length;
            maximumPayloadBytes = GetMaximumFragmentPayloadBytes();
            return encodedPayloadBytes <= maximumPayloadBytes;
        }

        public ulong Send(IRollbackProtocolPayload payload, bool reliable)
        {
            if (payload == null)
                throw new ArgumentNullException(nameof(payload));
            if (m_PendingReliable.Count >= m_Definition.MaximumQueuedMessages && reliable)
                throw new InvalidOperationException("Rollback reliable message capacity is exhausted.");
            ulong messageSequence = NextMessageSequence();
            byte[] bytes = Encode(payload, messageSequence);
            int fragmentBytes = GetMaximumFragmentPayloadBytes();
            int fragmentCount = checked((bytes.Length + fragmentBytes - 1) / fragmentBytes);
            if (!reliable && fragmentCount != 1)
                throw new InvalidOperationException("Rollback unreliable payload exceeds one datagram.");
            if (fragmentCount > m_Definition.MaximumFragmentsPerMessage)
                throw new InvalidOperationException("Rollback payload exceeds the bounded fragment capacity.");
            var packets = new RollbackDatagramPacket[fragmentCount];
            for (int i = 0; i < fragmentCount; i++)
            {
                int offset = i * fragmentBytes;
                int length = Math.Min(fragmentBytes, bytes.Length - offset);
                var fragment = new byte[length];
                Buffer.BlockCopy(bytes, offset, fragment, 0, length);
                packets[i] = new RollbackDatagramPacket(
                    RollbackDatagramKind.Payload,
                    m_Definition.SessionId,
                    m_LocalPeerId,
                    NextDatagramSequence(),
                    messageSequence,
                    reliable,
                    i,
                    fragmentCount,
                    bytes.Length,
                    fragment);
            }
            Enqueue(packets);
            if (reliable)
            {
                m_PendingReliable.Add(
                    messageSequence,
                    new PendingReliableMessage(packets, checked(Stopwatch.GetTimestamp() + m_ResendInterval)));
            }
            return messageSequence;
        }

        byte[] Encode(IRollbackProtocolPayload payload, ulong messageSequence) =>
            RollbackProtocolCodec.Write(
                new RollbackProtocolEnvelope(m_Definition.SessionId, m_LocalPeerId, messageSequence, payload));

        int GetMaximumFragmentPayloadBytes() =>
            RollbackDatagramCodec.GetMaximumFragmentPayloadBytes(
                m_Definition.SessionId,
                m_LocalPeerId,
                m_Definition.MaximumDatagramBytes);

        public void Process(RollbackReceivedDatagram received)
        {
            if (received == null)
                throw new ArgumentNullException(nameof(received));
            if (!EndPointEquals(received.RemoteEndPoint, m_RemoteEndPoint))
                throw new InvalidOperationException($"Rollback peer '{m_RemotePeerId}' changed its UDP endpoint while active.");
            RollbackDatagramPacket packet = received.Packet;
            if (!string.Equals(packet.SessionId, m_Definition.SessionId, StringComparison.Ordinal) ||
                !string.Equals(packet.SenderPeerId, m_RemotePeerId, StringComparison.Ordinal))
            {
                throw new InvalidDataException("Rollback datagram session or peer identity does not match the bound channel.");
            }
            if (packet.Kind == RollbackDatagramKind.Acknowledgement)
            {
                m_PendingReliable.Remove(packet.MessageSequence);
                return;
            }
            if (m_CompletedSequences.Contains(packet.MessageSequence))
            {
                if (packet.Reliable)
                    SendAcknowledgement(packet.MessageSequence);
                return;
            }
            if (!m_Reassembly.TryGetValue(packet.MessageSequence, out FragmentAssembly assembly))
            {
                if (m_Reassembly.Count >= m_Definition.MaximumQueuedMessages)
                    throw new InvalidOperationException("Rollback reassembly capacity is exhausted.");
                assembly = new FragmentAssembly(packet);
                m_Reassembly.Add(packet.MessageSequence, assembly);
            }
            assembly.Add(packet);
            if (!assembly.IsComplete)
                return;
            m_Reassembly.Remove(packet.MessageSequence);
            RollbackProtocolEnvelope envelope = RollbackProtocolCodec.Read(assembly.Complete());
            if (!string.Equals(envelope.SessionId, m_Definition.SessionId, StringComparison.Ordinal) ||
                !string.Equals(envelope.SenderPeerId, m_RemotePeerId, StringComparison.Ordinal) ||
                envelope.Sequence != packet.MessageSequence)
            {
                throw new InvalidDataException("Rollback protocol envelope does not match its UDP datagram identity.");
            }
            if (m_Received.Count >= m_Definition.MaximumQueuedMessages)
                throw new InvalidOperationException("Rollback received message capacity is exhausted.");
            RememberCompleted(packet.MessageSequence);
            m_Received.Enqueue(envelope);
            if (assembly.Reliable)
                SendAcknowledgement(packet.MessageSequence);
        }

        public bool TryReceive(out RollbackProtocolEnvelope envelope)
        {
            if (m_Received.Count == 0)
            {
                envelope = null;
                return false;
            }
            envelope = m_Received.Dequeue();
            return true;
        }

        public void Pump()
        {
            long now = Stopwatch.GetTimestamp();
            foreach (PendingReliableMessage pending in m_PendingReliable.Values)
            {
                if (now < pending.NextSendTimestamp)
                    continue;
                Enqueue(pending.Packets);
                pending.NextSendTimestamp = checked(now + m_ResendInterval);
            }
        }

        void SendAcknowledgement(ulong messageSequence)
        {
            m_Endpoint.EnqueueSend(
                new RollbackDatagramPacket(
                    RollbackDatagramKind.Acknowledgement,
                    m_Definition.SessionId,
                    m_LocalPeerId,
                    NextDatagramSequence(),
                    messageSequence,
                    true,
                    0,
                    0,
                    0,
                    Array.Empty<byte>()),
                m_RemoteEndPoint);
        }

        void Enqueue(IReadOnlyList<RollbackDatagramPacket> packets)
        {
            for (int i = 0; i < packets.Count; i++)
                m_Endpoint.EnqueueSend(packets[i], m_RemoteEndPoint);
        }

        void RememberCompleted(ulong sequence)
        {
            m_CompletedSequences.Add(sequence);
            m_CompletedOrder.Enqueue(sequence);
            int capacity = checked(m_Definition.MaximumQueuedMessages * 2);
            while (m_CompletedOrder.Count > capacity)
                m_CompletedSequences.Remove(m_CompletedOrder.Dequeue());
        }

        ulong NextDatagramSequence()
        {
            ulong value = m_NextDatagramSequence;
            m_NextDatagramSequence = checked(value + 1);
            return value;
        }

        ulong NextMessageSequence()
        {
            ulong value = m_NextMessageSequence;
            m_NextMessageSequence = checked(value + 1);
            return value;
        }

        static bool EndPointEquals(IPEndPoint left, IPEndPoint right) =>
            left.Port == right.Port && left.Address.Equals(right.Address);
    }
}
