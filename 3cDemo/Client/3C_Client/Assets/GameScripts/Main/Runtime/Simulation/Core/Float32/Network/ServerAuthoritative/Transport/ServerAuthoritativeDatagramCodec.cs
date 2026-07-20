using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;

namespace ThirdPersonSimulation.ServerAuthoritative.Transport
{
    public enum ServerAuthoritativeDatagramKind : byte
    {
        DataPlaneHello = 1,
        DataPlaneHelloAck = 2,
        Command = 3,
        Snapshot = 4
    }

    public readonly struct ServerAuthoritativeDatagramIdentity : IEquatable<ServerAuthoritativeDatagramIdentity>
    {
        public ServerAuthoritativeDatagramIdentity(
            ServerAuthoritativeRoomId roomId,
            ServerAuthoritativeSessionId sessionId,
            ServerAuthoritativePlayerId playerId,
            ActorId actorId)
        {
            if (!roomId.IsValid || !sessionId.IsValid || !playerId.IsValid || !actorId.IsValid)
                throw new ArgumentException("Gameplay datagram identity is incomplete.");
            RoomId = roomId;
            SessionId = sessionId;
            PlayerId = playerId;
            ActorId = actorId;
        }

        public ServerAuthoritativeRoomId RoomId { get; }
        public ServerAuthoritativeSessionId SessionId { get; }
        public ServerAuthoritativePlayerId PlayerId { get; }
        public ActorId ActorId { get; }

        public bool Equals(ServerAuthoritativeDatagramIdentity other) =>
            RoomId.Equals(other.RoomId) && SessionId.Equals(other.SessionId) &&
            PlayerId.Equals(other.PlayerId) && ActorId.Equals(other.ActorId);
        public override bool Equals(object obj) => obj is ServerAuthoritativeDatagramIdentity other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(RoomId, SessionId, PlayerId, ActorId);
        public override string ToString() => $"{RoomId}/{SessionId}/{PlayerId}/{ActorId}";
    }

    public readonly struct ServerAuthoritativeDatagramHeader
    {
        public ServerAuthoritativeDatagramHeader(
            ServerAuthoritativeDatagramIdentity identity,
            ServerAuthoritativeDatagramKind kind,
            ulong packetSequence,
            int payloadLength)
        {
            if (!Enum.IsDefined(typeof(ServerAuthoritativeDatagramKind), kind) || packetSequence == 0 || payloadLength < 0)
                throw new ArgumentException("Gameplay datagram header is invalid.");
            Identity = identity;
            Kind = kind;
            PacketSequence = packetSequence;
            PayloadLength = payloadLength;
        }

        public int ProtocolVersion => ServerAuthoritativeGameplayDatagramCodec.ProtocolVersion;
        public ServerAuthoritativeDatagramIdentity Identity { get; }
        public ServerAuthoritativeDatagramKind Kind { get; }
        public ulong PacketSequence { get; }
        public int PayloadLength { get; }
    }

    public sealed class ServerAuthoritativeDatagramPacket
    {
        readonly byte[] m_Payload;

        public ServerAuthoritativeDatagramPacket(ServerAuthoritativeDatagramHeader header, byte[] payload)
        {
            m_Payload = payload == null ? throw new ArgumentNullException(nameof(payload)) : (byte[])payload.Clone();
            if (header.PayloadLength != m_Payload.Length)
                throw new ArgumentException("Gameplay datagram payload length does not match its header.", nameof(payload));
            Header = header;
        }

        public ServerAuthoritativeDatagramHeader Header { get; }
        public byte[] CopyPayload() => (byte[])m_Payload.Clone();
    }

    public static class ServerAuthoritativeGameplayDatagramCodec
    {
        const uint Magic = 0x44504153;
        public const int ProtocolVersion = 1;

        public static byte[] Write(ServerAuthoritativeDatagramPacket packet, int maximumBytes)
        {
            if (packet == null)
                throw new ArgumentNullException(nameof(packet));
            if (maximumBytes <= 0)
                throw new ArgumentOutOfRangeException(nameof(maximumBytes));
            using var writer = new CanonicalWriter();
            writer.WriteUInt32(Magic);
            writer.WriteInt32(ProtocolVersion);
            writer.WriteByte((byte)packet.Header.Kind);
            writer.WriteString(packet.Header.Identity.RoomId.Value);
            writer.WriteString(packet.Header.Identity.SessionId.Value);
            writer.WriteString(packet.Header.Identity.PlayerId.Value);
            writer.WriteString(packet.Header.Identity.ActorId.Value);
            writer.WriteUInt64(packet.Header.PacketSequence);
            writer.WriteBytes(packet.CopyPayload());
            byte[] bytes = writer.ToArray();
            if (bytes.Length > maximumBytes)
                throw new InvalidDataException($"Gameplay datagram size '{bytes.Length}' exceeds budget '{maximumBytes}'.");
            return bytes;
        }

        public static ServerAuthoritativeDatagramPacket Read(byte[] bytes, int maximumBytes)
        {
            if (bytes == null || bytes.Length == 0 || bytes.Length > maximumBytes)
                throw new InvalidDataException("Gameplay datagram length is invalid.");
            var reader = new CanonicalReader(bytes);
            if (reader.ReadUInt32() != Magic)
                throw new InvalidDataException("Gameplay datagram magic is invalid.");
            int version = reader.ReadInt32();
            if (version != ProtocolVersion)
                throw new InvalidDataException($"Gameplay datagram protocol version '{version}' is unsupported.");
            var kind = (ServerAuthoritativeDatagramKind)reader.ReadByte();
            if (!Enum.IsDefined(typeof(ServerAuthoritativeDatagramKind), kind))
                throw new InvalidDataException($"Gameplay datagram kind '{kind}' is unsupported.");
            var identity = new ServerAuthoritativeDatagramIdentity(
                new ServerAuthoritativeRoomId(reader.ReadString()),
                new ServerAuthoritativeSessionId(reader.ReadString()),
                new ServerAuthoritativePlayerId(reader.ReadString()),
                new ActorId(reader.ReadString()));
            ulong packetSequence = reader.ReadUInt64();
            byte[] payload = reader.ReadBytes();
            reader.RequireComplete();
            return new ServerAuthoritativeDatagramPacket(
                new ServerAuthoritativeDatagramHeader(identity, kind, packetSequence, payload.Length),
                payload);
        }
    }

    public readonly struct DataPlaneHello
    {
        public DataPlaneHello(string ticketId, string nonce, long clientClockMicros)
        {
            TicketId = RequireToken(ticketId, nameof(ticketId));
            Nonce = RequireToken(nonce, nameof(nonce));
            if (clientClockMicros <= 0)
                throw new ArgumentOutOfRangeException(nameof(clientClockMicros));
            ClientClockMicros = clientClockMicros;
        }

        public string TicketId { get; }
        public string Nonce { get; }
        public long ClientClockMicros { get; }

        internal static string RequireToken(string value, string parameter)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length > 128)
                throw new ArgumentException("Data-plane token is invalid.", parameter);
            return value;
        }
    }

    public readonly struct DataPlaneHelloAck
    {
        public DataPlaneHelloAck(ulong authorityTick, long echoedClientClockMicros, long authorityClockMicros)
        {
            if (echoedClientClockMicros <= 0 || authorityClockMicros <= 0)
                throw new ArgumentException("Data-plane hello acknowledgement is invalid.");
            AuthorityTick = authorityTick;
            EchoedClientClockMicros = echoedClientClockMicros;
            AuthorityClockMicros = authorityClockMicros;
        }

        public ulong AuthorityTick { get; }
        public long EchoedClientClockMicros { get; }
        public long AuthorityClockMicros { get; }
    }

    public sealed class CanonicalInputSample
    {
        public CanonicalInputSample(ulong targetAuthorityTick, ulong inputSequence, CharacterSimulationInput input)
        {
            if (targetAuthorityTick == 0 || inputSequence == 0)
                throw new ArgumentException("Canonical input sample identity is invalid.");
            Input = input ?? throw new ArgumentNullException(nameof(input));
            if (input.Sequence != inputSequence || input.TickSource.SourceTick == 0)
                throw new ArgumentException("Canonical input sample identity does not match its input.", nameof(input));
            TargetAuthorityTick = targetAuthorityTick;
            InputSequence = inputSequence;
        }

        public ulong TargetAuthorityTick { get; }
        public ulong InputSequence { get; }
        public CharacterSimulationInput Input { get; }
    }

    public sealed class CommandDatagram
    {
        readonly ReadOnlyCollection<CanonicalInputSample> m_Samples;

        public CommandDatagram(
            ulong latestSnapshotSequence,
            ulong latestBaseSnapshotSequence,
            IEnumerable<CanonicalInputSample> samples)
        {
            var values = samples == null ? throw new ArgumentNullException(nameof(samples)) : new List<CanonicalInputSample>(samples);
            if (values.Count == 0 || values.Count > 4)
                throw new ArgumentException("Command datagram requires one to four input samples.", nameof(samples));
            for (int i = 1; i < values.Count; i++)
            {
                if (values[i - 1].TargetAuthorityTick <= values[i].TargetAuthorityTick ||
                    values[i - 1].InputSequence <= values[i].InputSequence)
                {
                    throw new ArgumentException("Command datagram samples must be newest-first and strictly ordered.", nameof(samples));
                }
            }
            LatestSnapshotSequence = latestSnapshotSequence;
            LatestBaseSnapshotSequence = latestBaseSnapshotSequence;
            m_Samples = values.AsReadOnly();
        }

        public ulong LatestSnapshotSequence { get; }
        public ulong LatestBaseSnapshotSequence { get; }
        public ulong SourceTick => m_Samples[0].Input.TickSource.SourceTick;
        public IReadOnlyList<CanonicalInputSample> Samples => m_Samples;
    }

    public sealed class SnapshotDatagram
    {
        readonly byte[] m_DeltaPayload;

        public SnapshotDatagram(
            ulong snapshotSequence,
            ulong baseSnapshotSequence,
            ulong authorityTick,
            ulong acknowledgedInputSequence,
            ulong reliableEventHorizon,
            byte[] deltaPayload)
        {
            if (snapshotSequence == 0 || authorityTick == 0)
                throw new ArgumentException("Snapshot datagram identity is invalid.");
            SnapshotSequence = snapshotSequence;
            BaseSnapshotSequence = baseSnapshotSequence;
            AuthorityTick = authorityTick;
            AcknowledgedInputSequence = acknowledgedInputSequence;
            ReliableEventHorizon = reliableEventHorizon;
            m_DeltaPayload = deltaPayload == null ? throw new ArgumentNullException(nameof(deltaPayload)) : (byte[])deltaPayload.Clone();
        }

        public ulong SnapshotSequence { get; }
        public ulong BaseSnapshotSequence { get; }
        public ulong AuthorityTick { get; }
        public ulong AcknowledgedInputSequence { get; }
        public ulong ReliableEventHorizon { get; }
        public byte[] CopyDeltaPayload() => (byte[])m_DeltaPayload.Clone();
    }

    public static class ServerAuthoritativeDatagramPayloadCodec
    {
        const int SchemaVersion = 1;

        public static byte[] Write(DataPlaneHello value)
        {
            using var writer = Writer(ServerAuthoritativeDatagramKind.DataPlaneHello);
            writer.WriteString(value.TicketId);
            writer.WriteString(value.Nonce);
            writer.WriteInt64(value.ClientClockMicros);
            return writer.ToArray();
        }

        public static DataPlaneHello ReadHello(byte[] bytes)
        {
            CanonicalReader reader = Reader(bytes, ServerAuthoritativeDatagramKind.DataPlaneHello);
            var value = new DataPlaneHello(reader.ReadString(), reader.ReadString(), reader.ReadInt64());
            reader.RequireComplete();
            return value;
        }

        public static byte[] Write(DataPlaneHelloAck value)
        {
            using var writer = Writer(ServerAuthoritativeDatagramKind.DataPlaneHelloAck);
            writer.WriteUInt64(value.AuthorityTick);
            writer.WriteInt64(value.EchoedClientClockMicros);
            writer.WriteInt64(value.AuthorityClockMicros);
            return writer.ToArray();
        }

        public static DataPlaneHelloAck ReadHelloAck(byte[] bytes)
        {
            CanonicalReader reader = Reader(bytes, ServerAuthoritativeDatagramKind.DataPlaneHelloAck);
            var value = new DataPlaneHelloAck(reader.ReadUInt64(), reader.ReadInt64(), reader.ReadInt64());
            reader.RequireComplete();
            return value;
        }

        public static byte[] Write(CommandDatagram value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            using var writer = Writer(ServerAuthoritativeDatagramKind.Command);
            writer.WriteUInt64(value.LatestSnapshotSequence);
            writer.WriteUInt64(value.LatestBaseSnapshotSequence);
            writer.WriteInt32(value.Samples.Count);
            for (int i = 0; i < value.Samples.Count; i++)
            {
                CanonicalInputSample sample = value.Samples[i];
                writer.WriteUInt64(sample.TargetAuthorityTick);
                writer.WriteUInt64(sample.InputSequence);
                writer.WriteBytes(ServerAuthoritativeCanonicalCodec.WriteInput(sample.Input));
            }
            return writer.ToArray();
        }

        public static CommandDatagram ReadCommand(byte[] bytes)
        {
            CanonicalReader reader = Reader(bytes, ServerAuthoritativeDatagramKind.Command);
            ulong latestSnapshot = reader.ReadUInt64();
            ulong latestBase = reader.ReadUInt64();
            int count = reader.ReadInt32();
            if (count <= 0 || count > 4)
                throw new InvalidDataException($"Command sample count '{count}' is invalid.");
            var samples = new CanonicalInputSample[count];
            for (int i = 0; i < count; i++)
            {
                ulong targetTick = reader.ReadUInt64();
                ulong inputSequence = reader.ReadUInt64();
                CharacterSimulationInput input = ServerAuthoritativeCanonicalCodec.ReadInput(reader.ReadBytes());
                samples[i] = new CanonicalInputSample(targetTick, inputSequence, input);
            }
            reader.RequireComplete();
            return new CommandDatagram(latestSnapshot, latestBase, samples);
        }

        public static byte[] Write(SnapshotDatagram value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            using var writer = Writer(ServerAuthoritativeDatagramKind.Snapshot);
            writer.WriteUInt64(value.SnapshotSequence);
            writer.WriteUInt64(value.BaseSnapshotSequence);
            writer.WriteUInt64(value.AuthorityTick);
            writer.WriteUInt64(value.AcknowledgedInputSequence);
            writer.WriteUInt64(value.ReliableEventHorizon);
            writer.WriteBytes(value.CopyDeltaPayload());
            return writer.ToArray();
        }

        public static SnapshotDatagram ReadSnapshot(byte[] bytes)
        {
            CanonicalReader reader = Reader(bytes, ServerAuthoritativeDatagramKind.Snapshot);
            var value = new SnapshotDatagram(
                reader.ReadUInt64(),
                reader.ReadUInt64(),
                reader.ReadUInt64(),
                reader.ReadUInt64(),
                reader.ReadUInt64(),
                reader.ReadBytes());
            reader.RequireComplete();
            return value;
        }

        static CanonicalWriter Writer(ServerAuthoritativeDatagramKind kind)
        {
            var writer = new CanonicalWriter();
            writer.WriteInt32(SchemaVersion);
            writer.WriteByte((byte)kind);
            return writer;
        }

        static CanonicalReader Reader(byte[] bytes, ServerAuthoritativeDatagramKind expectedKind)
        {
            var reader = new CanonicalReader(bytes ?? throw new ArgumentNullException(nameof(bytes)));
            int version = reader.ReadInt32();
            if (version != SchemaVersion)
                throw new InvalidDataException($"Datagram payload schema version '{version}' is unsupported.");
            var kind = (ServerAuthoritativeDatagramKind)reader.ReadByte();
            if (kind != expectedKind)
                throw new InvalidDataException($"Datagram payload kind '{kind}' does not match '{expectedKind}'.");
            return reader;
        }
    }
}
