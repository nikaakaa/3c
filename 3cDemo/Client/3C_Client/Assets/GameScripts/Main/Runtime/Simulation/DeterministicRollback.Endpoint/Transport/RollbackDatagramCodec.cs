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
        readonly byte[] m_Payload;

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
            byte[] payload)
        {
            if (!Enum.IsDefined(typeof(RollbackDatagramKind), kind) || datagramSequence == 0 || messageSequence == 0)
                throw new ArgumentException("Rollback datagram identity is invalid.");
            SessionId = RollbackEndpointIdentity.Require(sessionId, nameof(sessionId));
            SenderPeerId = RollbackEndpointIdentity.Require(senderPeerId, nameof(senderPeerId));
            m_Payload = payload == null ? Array.Empty<byte>() : (byte[])payload.Clone();
            if (kind == RollbackDatagramKind.Acknowledgement)
            {
                if (!reliable || fragmentIndex != 0 || fragmentCount != 0 || totalPayloadBytes != 0 || m_Payload.Length != 0)
                    throw new ArgumentException("Rollback acknowledgement datagram is invalid.");
            }
            else if (fragmentCount <= 0 || fragmentIndex < 0 || fragmentIndex >= fragmentCount ||
                     totalPayloadBytes <= 0 || m_Payload.Length <= 0 || m_Payload.Length > totalPayloadBytes ||
                     !reliable && fragmentCount != 1)
            {
                throw new ArgumentException("Rollback payload datagram is invalid.");
            }
            Kind = kind;
            DatagramSequence = datagramSequence;
            MessageSequence = messageSequence;
            Reliable = reliable;
            FragmentIndex = fragmentIndex;
            FragmentCount = fragmentCount;
            TotalPayloadBytes = totalPayloadBytes;
        }

        public RollbackDatagramKind Kind { get; }
        public string SessionId { get; }
        public string SenderPeerId { get; }
        public ulong DatagramSequence { get; }
        public ulong MessageSequence { get; }
        public bool Reliable { get; }
        public int FragmentIndex { get; }
        public int FragmentCount { get; }
        public int TotalPayloadBytes { get; }
        public byte[] CopyPayload() => (byte[])m_Payload.Clone();
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
            int capacity = maximumDatagramBytes - writer.ToArray().Length;
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
            writer.WriteBytes(packet.CopyPayload());
            byte[] result = writer.ToArray();
            if (result.Length > maximumDatagramBytes)
                throw new InvalidDataException($"Rollback datagram '{result.Length}' exceeds MTU budget '{maximumDatagramBytes}'.");
            return result;
        }

        public static RollbackDatagramPacket Read(byte[] bytes, int maximumDatagramBytes)
        {
            if (bytes == null || bytes.Length == 0 || bytes.Length > maximumDatagramBytes)
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
            byte[] payload = reader.ReadBytes();
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
                payload);
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
            if (!Enum.IsDefined(typeof(RollbackDatagramKind), value))
                throw new InvalidDataException($"Rollback datagram kind '{value}' is unsupported.");
            return (RollbackDatagramKind)value;
        }
    }
}
