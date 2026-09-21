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
            public PendingReliableMessage(int maximumPacketCount, int maximumPayloadBytes)
            {
                Packets = new RollbackDatagramPacket[maximumPacketCount];
                PayloadBuffers = new byte[maximumPacketCount][];
                for (int i = 0; i < maximumPacketCount; i++)
                    PayloadBuffers[i] = new byte[maximumPayloadBytes];
            }

            public RollbackDatagramPacket[] Packets { get; }
            public byte[][] PayloadBuffers { get; }
            public int PacketCount { get; private set; }
            public long NextSendTimestamp { get; set; }

            public void Reset(int packetCount, long nextSendTimestamp)
            {
                PacketCount = packetCount;
                NextSendTimestamp = nextSendTimestamp;
            }

            public void Release()
            {
                for (int i = 0; i < Packets.Length; i++)
                    Packets[i]?.Release();
                PacketCount = 0;
            }
        }

        sealed class FragmentAssembly
        {
            public FragmentAssembly(int maximumPacketCount)
            {
                Fragments = new RollbackDatagramPacket[maximumPacketCount];
            }

            public RollbackDatagramPacket[] Fragments { get; }
            public bool Reliable { get; private set; }
            public int TotalPayloadBytes { get; private set; }
            int FragmentCount { get; set; }
            int m_ReceivedCount;
            int m_ReceivedBytes;

            public bool IsComplete => m_ReceivedCount == FragmentCount;

            public void Reset(RollbackDatagramPacket packet)
            {
                if (packet.FragmentCount > Fragments.Length)
                    throw new InvalidDataException("Rollback message fragments exceed the bounded fragment capacity.");
                Reliable = packet.Reliable;
                TotalPayloadBytes = packet.TotalPayloadBytes;
                FragmentCount = packet.FragmentCount;
            }

            public void Add(RollbackDatagramPacket packet)
            {
                if (packet.Reliable != Reliable || packet.FragmentCount != FragmentCount ||
                    packet.TotalPayloadBytes != TotalPayloadBytes)
                {
                    throw new InvalidDataException("Rollback message fragment metadata changed during reassembly.");
                }
                if (Fragments[packet.FragmentIndex] != null)
                    return;
                Fragments[packet.FragmentIndex] = packet;
                m_ReceivedCount++;
                m_ReceivedBytes = checked(m_ReceivedBytes + packet.Payload.Length);
                if (m_ReceivedBytes > TotalPayloadBytes)
                    throw new InvalidDataException("Rollback message fragments exceed the declared payload size.");
            }

            public int CopyComplete(Span<byte> destination)
            {
                if (!IsComplete || m_ReceivedBytes != TotalPayloadBytes)
                    throw new InvalidOperationException("Rollback message reassembly is incomplete.");
                if (destination.Length < TotalPayloadBytes)
                    throw new InvalidOperationException("Rollback assembled payload buffer is too small.");
                int offset = 0;
                for (int i = 0; i < FragmentCount; i++)
                {
                    ReadOnlySpan<byte> payload = Fragments[i].Payload;
                    payload.CopyTo(destination.Slice(offset));
                    offset += payload.Length;
                }
                return TotalPayloadBytes;
            }

            public void Release()
            {
                Array.Clear(Fragments, 0, FragmentCount);
                Reliable = false;
                TotalPayloadBytes = 0;
                FragmentCount = 0;
                m_ReceivedCount = 0;
                m_ReceivedBytes = 0;
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
        readonly List<FragmentAssembly> m_ReassemblyPool;
        readonly Queue<RollbackProtocolEnvelope> m_Received;
        readonly HashSet<ulong> m_CompletedSequences;
        readonly Queue<ulong> m_CompletedOrder;
        readonly int m_CompletedHistoryCapacity;
        readonly long m_ResendInterval;
        readonly int m_MaximumFragmentPayloadBytes;
        readonly CanonicalWriter m_EncodeWriter;
        readonly CanonicalWriter m_DecodeScratch;
        readonly byte[] m_AssembledPayload;
        RollbackDatagramPacket m_Acknowledgement;
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
            m_AssembledPayload = new byte[encodeBufferCapacity];
            int messageCapacity = m_Definition.MaximumQueuedMessages;
            m_CompletedHistoryCapacity = checked(messageCapacity * 2);
            int completedStorageCapacity = checked(m_CompletedHistoryCapacity + 1);
            m_PendingReliable = new Dictionary<ulong, PendingReliableMessage>(messageCapacity);
            m_PendingReliablePool = new List<PendingReliableMessage>(messageCapacity);
            m_Reassembly = new Dictionary<ulong, FragmentAssembly>(messageCapacity);
            m_ReassemblyPool = new List<FragmentAssembly>(messageCapacity);
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
                    int offset = i * fragmentBytes;
                    int length = Math.Min(fragmentBytes, bytes.Length - offset);
                    if (pending.Packets[i] == null)
                        pending.Packets[i] = new RollbackDatagramPacket();
                    pending.Packets[i].Reset(
                        RollbackDatagramKind.Payload,
                        m_Definition.SessionId,
                        m_LocalPeerId,
                        NextDatagramSequence(),
                        messageSequence,
                        true,
                        i,
                        fragmentCount,
                        totalBytes,
                        pending.PayloadBuffers[i],
                        length);
                    bytes.Slice(offset, length).CopyTo(pending.PayloadBuffers[i]);
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
                assembly = RentReassembly();
                assembly.Reset(packet);
                m_Reassembly.Add(packet.MessageSequence, assembly);
            }
            assembly.Add(packet);
            if (!assembly.IsComplete)
                return;
            m_Reassembly.Remove(packet.MessageSequence);
            bool reliable = assembly.Reliable;
            int assembledBytes = assembly.CopyComplete(m_AssembledPayload);
            ReturnReassembly(assembly);
            RollbackProtocolEnvelope envelope = RollbackProtocolCodec.Read(
                m_DecodeScratch,
                new ArraySegment<byte>(m_AssembledPayload, 0, assembledBytes));
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
            if (reliable)
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
                return new PendingReliableMessage(m_Definition.MaximumFragmentsPerMessage, m_MaximumFragmentPayloadBytes);
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

        FragmentAssembly RentReassembly()
        {
            int last = m_ReassemblyPool.Count - 1;
            if (last < 0)
                return new FragmentAssembly(m_Definition.MaximumFragmentsPerMessage);
            FragmentAssembly assembly = m_ReassemblyPool[last];
            m_ReassemblyPool.RemoveAt(last);
            return assembly;
        }

        void ReturnReassembly(FragmentAssembly assembly)
        {
            assembly.Release();
            if (m_ReassemblyPool.Count < m_Definition.MaximumQueuedMessages)
                m_ReassemblyPool.Add(assembly);
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
                new RollbackDatagramPacket(
                    RollbackDatagramKind.Payload,
                    m_Definition.SessionId,
                    m_LocalPeerId,
                    NextDatagramSequence(),
                    messageSequence,
                    reliable,
                    fragmentIndex,
                    fragmentCount,
                    totalBytes,
                    bytes.Slice(offset, Math.Min(m_MaximumFragmentPayloadBytes, bytes.Length - offset))),
                m_RemoteEndPoint);
        }

        void EnqueuePending(PendingReliableMessage pending)
        {
            for (int i = 0; i < pending.PacketCount; i++)
                m_Endpoint.EnqueueSend(pending.Packets[i], m_RemoteEndPoint);
        }

        void SendAcknowledgement(ulong messageSequence)
        {
            m_Acknowledgement ??= new RollbackDatagramPacket();
            m_Endpoint.EnqueueSend(
                m_Acknowledgement.Reset(
                    RollbackDatagramKind.Acknowledgement,
                    m_Definition.SessionId,
                    m_LocalPeerId,
                    NextDatagramSequence(),
                    messageSequence,
                    true,
                    0,
                    0,
                    0,
                    Array.Empty<byte>(),
                    0),
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
