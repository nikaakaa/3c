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
            public PendingReliableMessage(int maximumPacketCount)
            {
                Packets = new RollbackDatagramPacket[maximumPacketCount];
            }

            public RollbackDatagramPacket[] Packets { get; }
            public int PacketCount { get; private set; }
            public long NextSendTimestamp { get; set; }

            public void Reset(int packetCount, long nextSendTimestamp)
            {
                PacketCount = packetCount;
                NextSendTimestamp = nextSendTimestamp;
            }

            public void Release()
            {
                Array.Clear(Packets, 0, PacketCount);
                PacketCount = 0;
            }
        }

        sealed class FragmentAssembly
        {
            readonly RollbackDatagramPacket[] m_Fragments;
            int m_ReceivedCount;
            int m_ReceivedBytes;

            public FragmentAssembly(RollbackDatagramPacket packet)
            {
                Reliable = packet.Reliable;
                TotalPayloadBytes = packet.TotalPayloadBytes;
                m_Fragments = new RollbackDatagramPacket[packet.FragmentCount];
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
                m_Fragments[packet.FragmentIndex] = packet;
                m_ReceivedCount++;
                m_ReceivedBytes = checked(m_ReceivedBytes + packet.Payload.Length);
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
                    ReadOnlySpan<byte> payload = m_Fragments[i].Payload;
                    payload.CopyTo(result.AsSpan(offset));
                    offset += payload.Length;
                }
                return result;
            }
        }

        readonly RollbackDatagramEndpoint m_Endpoint;
        readonly RollbackEndpointDefinition m_Definition;
        readonly string m_LocalPeerId;
        readonly string m_RemotePeerId;
        readonly IPEndPoint m_RemoteEndPoint;
        readonly Dictionary<ulong, PendingReliableMessage> m_PendingReliable;
        readonly List<PendingReliableMessage> m_PendingReliablePool;
        readonly Dictionary<ulong, FragmentAssembly> m_Reassembly;
        readonly Queue<RollbackProtocolEnvelope> m_Received;
        readonly HashSet<ulong> m_CompletedSequences;
        readonly Queue<ulong> m_CompletedOrder;
        readonly int m_CompletedHistoryCapacity;
        readonly long m_ResendInterval;
        readonly int m_MaximumFragmentPayloadBytes;
        readonly CanonicalWriter m_EncodeWriter;
        readonly CanonicalWriter m_DecodeScratch;
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
            m_MaximumFragmentPayloadBytes = RollbackDatagramCodec.GetMaximumFragmentPayloadBytes(
                m_Definition.SessionId,
                m_LocalPeerId,
                m_Definition.MaximumDatagramBytes);
            int encodeBufferCapacity = checked(
                m_MaximumFragmentPayloadBytes * m_Definition.MaximumFragmentsPerMessage);
            m_EncodeWriter = new CanonicalWriter(new byte[encodeBufferCapacity]);
            m_DecodeScratch = new CanonicalWriter(new byte[encodeBufferCapacity]);
            int messageCapacity = m_Definition.MaximumQueuedMessages;
            m_CompletedHistoryCapacity = checked(messageCapacity * 2);
            int completedStorageCapacity = checked(m_CompletedHistoryCapacity + 1);
            m_PendingReliable = new Dictionary<ulong, PendingReliableMessage>(messageCapacity);
            m_PendingReliablePool = new List<PendingReliableMessage>(messageCapacity);
            m_Reassembly = new Dictionary<ulong, FragmentAssembly>(messageCapacity);
            m_Received = new Queue<RollbackProtocolEnvelope>(messageCapacity);
            m_CompletedSequences = new HashSet<ulong>(completedStorageCapacity);
            m_CompletedOrder = new Queue<ulong>(completedStorageCapacity);
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
            m_EncodeWriter.Reset();
            RollbackProtocolCodec.Write(
                m_EncodeWriter,
                new RollbackProtocolEnvelope(m_Definition.SessionId, m_LocalPeerId, m_NextMessageSequence, payload));
            encodedPayloadBytes = checked((int)m_EncodeWriter.Length);
            maximumPayloadBytes = m_MaximumFragmentPayloadBytes;
            return encodedPayloadBytes <= maximumPayloadBytes;
        }

        public ulong Send(IRollbackProtocolPayload payload, bool reliable)
        {
            if (payload == null)
                throw new ArgumentNullException(nameof(payload));
            if (m_PendingReliable.Count >= m_Definition.MaximumQueuedMessages && reliable)
                throw new InvalidOperationException("Rollback reliable message capacity is exhausted.");
            ulong messageSequence = NextMessageSequence();
            m_EncodeWriter.Reset();
            RollbackProtocolCodec.Write(
                m_EncodeWriter,
                new RollbackProtocolEnvelope(m_Definition.SessionId, m_LocalPeerId, messageSequence, payload));
            int totalBytes = checked((int)m_EncodeWriter.Length);
            ReadOnlySpan<byte> bytes = m_EncodeWriter.WrittenSpan;
            int fragmentBytes = m_MaximumFragmentPayloadBytes;
            int fragmentCount = checked((totalBytes + fragmentBytes - 1) / fragmentBytes);
            if (!reliable && fragmentCount != 1)
                throw new InvalidOperationException("Rollback unreliable payload exceeds one datagram.");
            if (fragmentCount > m_Definition.MaximumFragmentsPerMessage)
                throw new InvalidOperationException("Rollback payload exceeds the bounded fragment capacity.");
            if (!reliable)
            {
                for (int i = 0; i < fragmentCount; i++)
                    EnqueuePacket(messageSequence, false, i, fragmentCount, totalBytes, bytes, offset: i * fragmentBytes);
                return messageSequence;
            }

            PendingReliableMessage pending = RentPending();
            bool stored = false;
            try
            {
                for (int i = 0; i < fragmentCount; i++)
                {
                    pending.Packets[i] = CreatePacket(
                        messageSequence,
                        true,
                        i,
                        fragmentCount,
                        totalBytes,
                        bytes,
                        i * fragmentBytes);
                }
                pending.Reset(fragmentCount, checked(Stopwatch.GetTimestamp() + m_ResendInterval));
                EnqueuePending(pending);
                m_PendingReliable.Add(messageSequence, pending);
                stored = true;
            }
            finally
            {
                if (!stored)
                    ReturnPending(pending);
            }
            return messageSequence;
        }

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
                if (m_PendingReliable.TryGetValue(packet.MessageSequence, out PendingReliableMessage pending))
                {
                    m_PendingReliable.Remove(packet.MessageSequence);
                    ReturnPending(pending);
                }
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
            RollbackProtocolEnvelope envelope = RollbackProtocolCodec.Read(m_DecodeScratch, assembly.Complete());
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
            foreach (KeyValuePair<ulong, PendingReliableMessage> pair in m_PendingReliable)
            {
                PendingReliableMessage pending = pair.Value;
                if (now < pending.NextSendTimestamp)
                    continue;
                EnqueuePending(pending);
                pending.NextSendTimestamp = checked(now + m_ResendInterval);
            }
        }

        PendingReliableMessage RentPending()
        {
            int last = m_PendingReliablePool.Count - 1;
            if (last < 0)
                return new PendingReliableMessage(m_Definition.MaximumFragmentsPerMessage);
            PendingReliableMessage pending = m_PendingReliablePool[last];
            m_PendingReliablePool.RemoveAt(last);
            return pending;
        }

        void ReturnPending(PendingReliableMessage pending)
        {
            pending.Release();
            if (m_PendingReliablePool.Count < m_Definition.MaximumQueuedMessages)
                m_PendingReliablePool.Add(pending);
        }

        RollbackDatagramPacket CreatePacket(
            ulong messageSequence,
            bool reliable,
            int fragmentIndex,
            int fragmentCount,
            int totalBytes,
            ReadOnlySpan<byte> bytes,
            int offset)
        {
            int length = Math.Min(m_MaximumFragmentPayloadBytes, bytes.Length - offset);
            return new RollbackDatagramPacket(
                RollbackDatagramKind.Payload,
                m_Definition.SessionId,
                m_LocalPeerId,
                NextDatagramSequence(),
                messageSequence,
                reliable,
                fragmentIndex,
                fragmentCount,
                totalBytes,
                bytes.Slice(offset, length));
        }

        void EnqueuePacket(
            ulong messageSequence,
            bool reliable,
            int fragmentIndex,
            int fragmentCount,
            int totalBytes,
            ReadOnlySpan<byte> bytes,
            int offset)
        {
            m_Endpoint.EnqueueSend(
                CreatePacket(messageSequence, reliable, fragmentIndex, fragmentCount, totalBytes, bytes, offset),
                m_RemoteEndPoint);
        }

        void EnqueuePending(PendingReliableMessage pending)
        {
            for (int i = 0; i < pending.PacketCount; i++)
                m_Endpoint.EnqueueSend(pending.Packets[i], m_RemoteEndPoint);
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

        void RememberCompleted(ulong sequence)
        {
            m_CompletedSequences.Add(sequence);
            m_CompletedOrder.Enqueue(sequence);
            while (m_CompletedOrder.Count > m_CompletedHistoryCapacity)
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
