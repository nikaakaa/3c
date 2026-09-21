using System;
using System.IO;

namespace ThirdPersonSimulation.DeterministicRollback
{
    public enum RollbackDatagramKind : byte
    {
        Payload = 1,
        Acknowledgement = 2
    }

    public sealed class RollbackDatagramPacket
    {
        byte[] m_Payload;
        int m_PayloadLength;

        public RollbackDatagramPacket(
            RollbackDatagramKind kind,
            string sessionId,
            string senderPeerId,
            ulong datagramSequence,
            ulong messageSequence,
            bool reliable,
            int fragmentIndex,
            int fragmentCount,
            int totalPayloadBytes,
            ReadOnlySpan<byte> payload)
        {
            Validate(
                kind,
                sessionId,
                senderPeerId,
                datagramSequence,
                messageSequence,
                reliable,
                fragmentIndex,
                fragmentCount,
                totalPayloadBytes,
                payload.Length);
            m_Payload = payload.ToArray();
            m_PayloadLength = payload.Length;
            Kind = kind;
            SessionId = sessionId;
            SenderPeerId = senderPeerId;
            DatagramSequence = datagramSequence;
            MessageSequence = messageSequence;
            Reliable = reliable;
            FragmentIndex = fragmentIndex;
            FragmentCount = fragmentCount;
            TotalPayloadBytes = totalPayloadBytes;
        }

        internal RollbackDatagramPacket()
        {
        }

        internal RollbackDatagramPacket Reset(
            RollbackDatagramKind kind,
            string sessionId,
            string senderPeerId,
            ulong datagramSequence,
            ulong messageSequence,
            bool reliable,
            int fragmentIndex,
            int fragmentCount,
            int totalPayloadBytes,
            byte[] payload,
            int payloadLength)
        {
            Validate(
                kind,
                sessionId,
                senderPeerId,
                datagramSequence,
                messageSequence,
                reliable,
                fragmentIndex,
                fragmentCount,
                totalPayloadBytes,
                payloadLength);
            Kind = kind;
            SessionId = sessionId;
            SenderPeerId = senderPeerId;
            DatagramSequence = datagramSequence;
            MessageSequence = messageSequence;
            Reliable = reliable;
            FragmentIndex = fragmentIndex;
            FragmentCount = fragmentCount;
            TotalPayloadBytes = totalPayloadBytes;
            m_Payload = payload;
            m_PayloadLength = payloadLength;
            return this;
        }

        internal void Release()
        {
            Kind = default;
            SessionId = null;
            SenderPeerId = null;
            DatagramSequence = 0;
            MessageSequence = 0;
            Reliable = false;
            FragmentIndex = 0;
            FragmentCount = 0;
            TotalPayloadBytes = 0;
            m_Payload = null;
            m_PayloadLength = 0;
        }

        public RollbackDatagramKind Kind { get; private set; }
        public string SessionId { get; private set; }
        public string SenderPeerId { get; private set; }
        public ulong DatagramSequence { get; private set; }
        public ulong MessageSequence { get; private set; }
        public bool Reliable { get; private set; }
        public int FragmentIndex { get; private set; }
        public int FragmentCount { get; private set; }
        public int TotalPayloadBytes { get; private set; }
        public ReadOnlySpan<byte> Payload => m_Payload.AsSpan(0, m_PayloadLength);

        static void Validate(
            RollbackDatagramKind kind,
            string sessionId,
            string senderPeerId,
            ulong datagramSequence,
            ulong messageSequence,
            bool reliable,
            int fragmentIndex,
            int fragmentCount,
            int totalPayloadBytes,
            int payloadLength)
        {
            if ((kind != RollbackDatagramKind.Payload && kind != RollbackDatagramKind.Acknowledgement) || datagramSequence == 0 || messageSequence == 0)
                throw new ArgumentException("Rollback datagram identity is invalid.");
            RollbackEndpointIdentity.Require(sessionId, nameof(sessionId));
            RollbackEndpointIdentity.Require(senderPeerId, nameof(senderPeerId));
            if (kind == RollbackDatagramKind.Acknowledgement)
            {
                if (!reliable || fragmentIndex != 0 || fragmentCount != 0 || totalPayloadBytes != 0 || payloadLength != 0)
                    throw new ArgumentException("Rollback acknowledgement datagram is invalid.");
            }
            else if (fragmentCount <= 0 || fragmentIndex < 0 || fragmentIndex >= fragmentCount ||
                     totalPayloadBytes <= 0 || payloadLength <= 0 || payloadLength > totalPayloadBytes ||
                     !reliable && fragmentCount != 1)
            {
                throw new ArgumentException("Rollback payload datagram is invalid.");
            }
        }
    }

    public static class RollbackDatagramCodec
    {
        const uint Magic = 0x55425244;
        const int Version = 1;

        public static int GetMaximumFragmentPayloadBytes(
            string sessionId,
            string senderPeerId,
            int maximumDatagramBytes)
        {
            RollbackEndpointIdentity.Require(sessionId, nameof(sessionId));
            RollbackEndpointIdentity.Require(senderPeerId, nameof(senderPeerId));
            if (maximumDatagramBytes < 256 || maximumDatagramBytes > 1200)
                throw new ArgumentOutOfRangeException(nameof(maximumDatagramBytes));
            using var writer = new CanonicalWriter();
            WriteHeader(
                writer,
                RollbackDatagramKind.Payload,
                sessionId,
                senderPeerId,
                1,
                1,
                true,
                0,
                1,
                1);
            writer.WriteBytes(Array.Empty<byte>());
            int capacity = maximumDatagramBytes - checked((int)writer.Length);
            if (capacity <= 0)
                throw new InvalidOperationException("Rollback datagram identity leaves no payload capacity.");
            return capacity;
        }

        public static byte[] Write(RollbackDatagramPacket packet, int maximumDatagramBytes)
        {
            if (packet == null)
                throw new ArgumentNullException(nameof(packet));
            using var writer = new CanonicalWriter();
            WriteHeader(
                writer,
                packet.Kind,
                packet.SessionId,
                packet.SenderPeerId,
                packet.DatagramSequence,
                packet.MessageSequence,
                packet.Reliable,
                packet.FragmentIndex,
                packet.FragmentCount,
                packet.TotalPayloadBytes);
            writer.WriteBytes(packet.Payload);
            byte[] result = writer.ToArray();
            if (result.Length > maximumDatagramBytes)
                throw new InvalidDataException($"Rollback datagram '{result.Length}' exceeds MTU budget '{maximumDatagramBytes}'.");
            return result;
        }

        public static RollbackDatagramPacket Read(ArraySegment<byte> bytes, int maximumDatagramBytes)
        {
            if (bytes.Count == 0 || bytes.Count > maximumDatagramBytes)
                throw new InvalidDataException("Rollback datagram size is invalid.");
            var reader = new CanonicalReader(bytes);
            if (reader.ReadUInt32() != Magic || reader.ReadInt32() != Version)
                throw new InvalidDataException("Rollback datagram header is invalid.");
            RollbackDatagramKind kind = ReadKind(reader.ReadByte());
            string sessionId = reader.ReadString();
            string senderPeerId = reader.ReadString();
            ulong datagramSequence = reader.ReadUInt64();
            ulong messageSequence = reader.ReadUInt64();
            bool reliable = reader.ReadBoolean();
            int fragmentIndex = reader.ReadInt32();
            int fragmentCount = reader.ReadInt32();
            int totalPayloadBytes = reader.ReadInt32();
            ArraySegment<byte> payload = reader.ReadBytesSegment();
            reader.RequireComplete();
            return new RollbackDatagramPacket(
                kind,
                sessionId,
                senderPeerId,
                datagramSequence,
                messageSequence,
                reliable,
                fragmentIndex,
                fragmentCount,
                totalPayloadBytes,
                payload.AsSpan());
        }

        static void WriteHeader(
            CanonicalWriter writer,
            RollbackDatagramKind kind,
            string sessionId,
            string senderPeerId,
            ulong datagramSequence,
            ulong messageSequence,
            bool reliable,
            int fragmentIndex,
            int fragmentCount,
            int totalPayloadBytes)
        {
            writer.WriteUInt32(Magic);
            writer.WriteInt32(Version);
            writer.WriteByte((byte)kind);
            writer.WriteString(sessionId);
            writer.WriteString(senderPeerId);
            writer.WriteUInt64(datagramSequence);
            writer.WriteUInt64(messageSequence);
            writer.WriteBoolean(reliable);
            writer.WriteInt32(fragmentIndex);
            writer.WriteInt32(fragmentCount);
            writer.WriteInt32(totalPayloadBytes);
        }

        static RollbackDatagramKind ReadKind(byte value)
        {
            if (value != (byte)RollbackDatagramKind.Payload && value != (byte)RollbackDatagramKind.Acknowledgement)
                throw new InvalidDataException($"Rollback datagram kind '{value}' is unsupported.");
            return (RollbackDatagramKind)value;
        }
    }
}
