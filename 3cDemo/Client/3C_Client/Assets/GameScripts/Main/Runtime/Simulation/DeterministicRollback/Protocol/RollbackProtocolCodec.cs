using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ThirdPersonSimulation.Fixed;

namespace ThirdPersonSimulation.DeterministicRollback
{
    public static class RollbackProtocolCodec
    {
        const uint Magic = 0x50524244;
        const uint PayloadMagic = 0x4C505244;
        const int Version = 8;

        public static void Write(CanonicalWriter writer, RollbackProtocolEnvelope envelope)
        {
            if (writer == null)
                throw new ArgumentNullException(nameof(writer));
            if (envelope.Payload == null)
                throw new ArgumentNullException(nameof(envelope));
            WriteEnvelope(
                writer,
                envelope.SessionId,
                envelope.SenderPeerId,
                envelope.Sequence,
                envelope.Payload);
        }

        public static void Write(
            CanonicalWriter writer,
            string sessionId,
            string senderPeerId,
            ulong sequence,
            IRollbackProtocolPayload payload)
        {
            if (writer == null)
                throw new ArgumentNullException(nameof(writer));
            SimulationIdentity.Require(sessionId, nameof(sessionId));
            SimulationIdentity.Require(senderPeerId, nameof(senderPeerId));
            if (sequence == 0)
                throw new ArgumentOutOfRangeException(nameof(sequence));
            if (payload == null)
                throw new ArgumentNullException(nameof(payload));
            WriteEnvelope(writer, sessionId, senderPeerId, sequence, payload);
        }

        static void WriteEnvelope(
            CanonicalWriter writer,
            string sessionId,
            string senderPeerId,
            ulong sequence,
            IRollbackProtocolPayload payload)
        {
            writer.WriteUInt32(Magic);
            writer.WriteInt32(Version);
            writer.WriteString(sessionId);
            writer.WriteString(senderPeerId);
            writer.WriteUInt64(sequence);
            writer.WriteByte((byte)payload.Kind);
            WritePayload(writer, payload);
        }

        public static RollbackProtocolEnvelope Read(
            CanonicalWriter canonicalScratch,
            ArraySegment<byte> bytes,
            in RollbackProtocolExpectedIdentity expectedIdentity)
        {
            if (canonicalScratch == null)
                throw new ArgumentNullException(nameof(canonicalScratch));
            var reader = new CanonicalReader(bytes);
            if (reader.ReadUInt32() != Magic || reader.ReadInt32() != Version)
                throw new InvalidDataException("Rollback protocol envelope header is invalid.");
            string sessionId = ReadIdentity(reader, expectedIdentity.SessionId, expectedIdentity.SessionUtf8.Span);
            string senderPeerId = ReadIdentity(reader, expectedIdentity.SenderPeerId, expectedIdentity.SenderUtf8.Span);
            ulong sequence = reader.ReadUInt64();
            RollbackProtocolMessageKind kind = ReadKind(reader.ReadByte());
            IRollbackProtocolPayload payload = ReadPayload(reader, kind, canonicalScratch);
            reader.RequireComplete();
            var envelope = new RollbackProtocolEnvelope(sessionId, senderPeerId, sequence, payload);
            canonicalScratch.Reset();
            WriteEnvelope(
                canonicalScratch,
                envelope.SessionId,
                envelope.SenderPeerId,
                envelope.Sequence,
                envelope.Payload);
            if (!canonicalScratch.ContentEquals(bytes.AsSpan()))
                throw new InvalidDataException("Rollback protocol envelope is not canonical.");
            return envelope;
        }

        static string ReadIdentity(CanonicalReader reader, string expected, ReadOnlySpan<byte> expectedUtf8)
        {
            ArraySegment<byte> value = reader.ReadUtf8Segment();
            return value.AsSpan().SequenceEqual(expectedUtf8)
                ? expected
                : Encoding.UTF8.GetString(value.Array, value.Offset, value.Count);
        }

        public static void WriteCanonicalPayload(CanonicalWriter writer, IRollbackProtocolPayload payload)
        {
            writer.WriteUInt32(PayloadMagic);
            writer.WriteInt32(Version);
            writer.WriteByte((byte)payload.Kind);
            WritePayload(writer, payload);
        }

        public static void WriteCanonicalStateHashPayload(
            CanonicalWriter writer,
            string peerId,
            SimulationTick tick,
            StableHash worldHash,
            StableHash rosterHash,
            StableHash kccHash,
            SimulationWorldSnapshot world)
        {
            writer.WriteUInt32(PayloadMagic);
            writer.WriteInt32(Version);
            writer.WriteByte((byte)RollbackProtocolMessageKind.StateHash);
            WriteStateHashCore(writer, peerId, tick, worldHash, rosterHash, kccHash, new SnapshotStateHashActors(world));
        }

        public static IRollbackProtocolPayload ReadCanonicalPayload(
            CanonicalWriter canonicalScratch,
            ReadOnlyMemory<byte> bytes)
        {
            if (canonicalScratch == null)
                throw new ArgumentNullException(nameof(canonicalScratch));
            var reader = new CanonicalReader(bytes);
            if (reader.ReadUInt32() != PayloadMagic || reader.ReadInt32() != Version)
                throw new InvalidDataException("Rollback canonical payload header is invalid.");
            IRollbackProtocolPayload payload = ReadPayload(reader, ReadKind(reader.ReadByte()), canonicalScratch);
            reader.RequireComplete();
            canonicalScratch.Reset();
            WriteCanonicalPayload(canonicalScratch, payload);
            if (!canonicalScratch.ContentEquals(bytes.Span))
                throw new InvalidDataException("Rollback protocol payload is not canonical.");
            return payload;
        }

        static void WritePayload(CanonicalWriter writer, IRollbackProtocolPayload payload)
        {
            switch (payload)
            {
                case RollbackHandshake handshake:
                    WriteHandshake(writer, handshake);
                    break;
                case RollbackRoster roster:
                    WriteRoster(writer, roster);
                    break;
                case RollbackActorInputBatch input:
                    WriteInputBatch(writer, input);
                    break;
                case RollbackRelayedExplicitInputBatch relayed:
                    WriteRelayedInputBatch(writer, relayed);
                    break;
                case RollbackCanonicalInputBundle bundle:
                    RollbackInputCodec.WriteLengthPrefixedBundle(writer, bundle);
                    break;
                case RollbackCanonicalConfirmation confirmation:
                    WriteCanonicalConfirmation(writer, confirmation);
                    break;
                case RollbackStateHashReport report:
                    WriteStateHash(writer, report);
                    break;
                case RollbackSnapshotRequest request:
                    writer.WriteString(request.RequesterPeerId);
                    writer.WriteString(request.AuthorityPeerId);
                    writer.WriteUInt64(request.Tick.Value);
                    writer.WriteHash(request.ExpectedWorldHash);
                    break;
                case RollbackSnapshotResponse response:
                    writer.WriteString(response.AuthorityPeerId);
                    writer.WriteString(response.RequesterPeerId);
                    writer.WriteUInt64(response.Tick.Value);
                    writer.WriteHash(response.SnapshotHash);
                    writer.WriteBytes(response.SnapshotBytes);
                    break;
                case RollbackLeave leave:
                    writer.WriteString(leave.PeerId);
                    writer.WriteString(leave.Reason);
                    break;
                default:
                    throw new InvalidDataException($"Rollback payload type '{payload.GetType().FullName}' is unsupported.");
            }
        }

        static IRollbackProtocolPayload ReadPayload(
            CanonicalReader reader,
            RollbackProtocolMessageKind kind,
            CanonicalWriter canonicalScratch)
        {
            return kind switch
            {
                RollbackProtocolMessageKind.Handshake => ReadHandshake(reader),
                RollbackProtocolMessageKind.Roster => ReadRoster(reader),
                RollbackProtocolMessageKind.ActorInputBatch => ReadInputBatch(reader, canonicalScratch),
                RollbackProtocolMessageKind.RelayedExplicitInputBatch => ReadRelayedInputBatch(reader, canonicalScratch),
                RollbackProtocolMessageKind.CanonicalBundle => RollbackInputCodec.ReadBundle(reader.ReadBytesSegment(), canonicalScratch),
                RollbackProtocolMessageKind.CanonicalConfirmation => ReadCanonicalConfirmation(reader, canonicalScratch),
                RollbackProtocolMessageKind.StateHash => ReadStateHash(reader),
                RollbackProtocolMessageKind.SnapshotRequest => new RollbackSnapshotRequest(
                    reader.ReadString(),
                    reader.ReadString(),
                    new SimulationTick(reader.ReadUInt64()),
                    new StableHash(reader.ReadString())),
                RollbackProtocolMessageKind.SnapshotResponse => new RollbackSnapshotResponse(
                    reader.ReadString(),
                    reader.ReadString(),
                    new SimulationTick(reader.ReadUInt64()),
                    new StableHash(reader.ReadString()),
                    reader.ReadBytesSegment().AsSpan()),
                RollbackProtocolMessageKind.Leave => new RollbackLeave(reader.ReadString(), reader.ReadString()),
                _ => throw new InvalidDataException($"Rollback payload kind '{kind}' is unsupported.")
            };
        }

        static void WriteInputBatch(CanonicalWriter writer, RollbackActorInputBatch value)
        {
            writer.WriteInt32(value.Frames.Count);
            for (int i = 0; i < value.Frames.Count; i++)
                RollbackInputCodec.WriteLengthPrefixedInput(writer, value.Frames[i]);
        }

        static RollbackActorInputBatch ReadInputBatch(CanonicalReader reader, CanonicalWriter canonicalScratch)
        {
            int count = ReadCount(reader);
            var frames = new RollbackActorInputFrame[count];
            for (int i = 0; i < count; i++)
                frames[i] = RollbackInputCodec.ReadInput(reader.ReadBytesSegment(), canonicalScratch);
            return RollbackActorInputBatch.FromOwnedFrames(frames);
        }

        static void WriteRelayedInputBatch(CanonicalWriter writer, RollbackRelayedExplicitInputBatch value)
        {
            writer.WriteInt32(value.Frames.Count);
            for (int i = 0; i < value.Frames.Count; i++)
                RollbackInputCodec.WriteLengthPrefixedInput(writer, value.Frames[i]);
        }

        static RollbackRelayedExplicitInputBatch ReadRelayedInputBatch(CanonicalReader reader, CanonicalWriter canonicalScratch)
        {
            int count = ReadCount(reader);
            var frames = new RollbackActorInputFrame[count];
            for (int i = 0; i < count; i++)
                frames[i] = RollbackInputCodec.ReadInput(reader.ReadBytesSegment(), canonicalScratch);
            return RollbackRelayedExplicitInputBatch.FromOwnedFrames(frames);
        }

        static void WriteCanonicalConfirmation(CanonicalWriter writer, RollbackCanonicalConfirmation value)
        {
            writer.WriteUInt64(value.PreviousConfirmedTick);
            writer.WriteUInt64(value.ConfirmedTick.Value);
            writer.WriteInt32(value.FinalBundles.Count);
            for (int i = 0; i < value.FinalBundles.Count; i++)
                RollbackInputCodec.WriteLengthPrefixedBundle(writer, value.FinalBundles[i]);
        }

        static RollbackCanonicalConfirmation ReadCanonicalConfirmation(CanonicalReader reader, CanonicalWriter canonicalScratch)
        {
            ulong previousConfirmedTick = reader.ReadUInt64();
            var confirmedTick = new SimulationTick(reader.ReadUInt64());
            int count = ReadCount(reader);
            var bundles = new RollbackCanonicalInputBundle[count];
            for (int i = 0; i < count; i++)
                bundles[i] = RollbackInputCodec.ReadBundle(reader.ReadBytesSegment(), canonicalScratch);
            return RollbackCanonicalConfirmation.FromOwnedBundles(previousConfirmedTick, confirmedTick, bundles);
        }

        static void WriteHandshake(CanonicalWriter writer, RollbackHandshake value)
        {
            writer.WriteString(value.PeerId);
            WriteComponentIdentity(writer, value.Model);
            writer.WriteHash(value.GameplayContentHash);
            writer.WriteHash(value.StateSchemaHash);
            writer.WriteInt32(value.TickRate);
            writer.WriteHash(value.CollisionWorldHash);
            writer.WriteHash(value.KccIdentityHash);
            writer.WriteString(value.Protocol.ProtocolId);
            writer.WriteString(value.Protocol.SemanticVersion);
            writer.WriteHash(value.Protocol.SchemaHash);
        }

        static RollbackHandshake ReadHandshake(CanonicalReader reader)
        {
            return new RollbackHandshake(
                reader.ReadString(),
                ReadComponentIdentity(reader),
                new GameplayContentHash(new StableHash(reader.ReadString())),
                new StableHash(reader.ReadString()),
                reader.ReadInt32(),
                new StableHash(reader.ReadString()),
                new StableHash(reader.ReadString()),
                new SimulationProtocolIdentity(reader.ReadString(), reader.ReadString(), new StableHash(reader.ReadString())));
        }

        static void WriteRoster(CanonicalWriter writer, RollbackRoster value)
        {
            writer.WriteUInt64(value.Revision);
            writer.WriteInt32(value.Entries.Count);
            for (int i = 0; i < value.Entries.Count; i++)
            {
                RollbackRosterEntry entry = value.Entries[i];
                writer.WriteString(entry.PeerId);
                writer.WriteString(entry.PlayerId);
                writer.WriteString(entry.ActorId.Value);
            }
        }

        static RollbackRoster ReadRoster(CanonicalReader reader)
        {
            ulong revision = reader.ReadUInt64();
            int count = ReadCount(reader);
            var entries = new RollbackRosterEntry[count];
            for (int i = 0; i < count; i++)
                entries[i] = new RollbackRosterEntry(reader.ReadString(), reader.ReadString(), new ActorId(reader.ReadString()));
            return RollbackRoster.FromOwnedEntries(revision, entries);
        }

        static void WriteStateHash(CanonicalWriter writer, RollbackStateHashReport value)
        {
            WriteStateHashCore(writer, value.PeerId, value.Tick, value.WorldHash, value.RosterHash, value.KccHash, new ReportStateHashActors(value));
        }

        static void WriteStateHashCore<TActors>(
            CanonicalWriter writer,
            string peerId,
            SimulationTick tick,
            StableHash worldHash,
            StableHash rosterHash,
            StableHash kccHash,
            TActors actors)
            where TActors : struct, IRollbackStateHashActors
        {
            writer.WriteString(peerId);
            writer.WriteUInt64(tick.Value);
            writer.WriteHash(worldHash);
            writer.WriteHash(rosterHash);
            writer.WriteHash(kccHash);
            writer.WriteInt32(actors.Count);
            for (int i = 0; i < actors.Count; i++)
            {
                writer.WriteString(actors.ActorId(i).Value);
                writer.WriteHash(actors.GameplayContentHash(i));
                writer.WriteHash(actors.CharacterStateHash(i));
            }
        }

        static RollbackStateHashReport ReadStateHash(CanonicalReader reader)
        {
            string peerId = reader.ReadString();
            var tick = new SimulationTick(reader.ReadUInt64());
            var worldHash = new StableHash(reader.ReadString());
            var rosterHash = new StableHash(reader.ReadString());
            var kccHash = new StableHash(reader.ReadString());
            int actorCount = ReadCount(reader);
            var actors = new RollbackActorHash[actorCount];
            for (int i = 0; i < actorCount; i++)
            {
                var actorId = new ActorId(reader.ReadString());
                var gameplayContentHash = new GameplayContentHash(new StableHash(reader.ReadString()));
                var characterStateHash = new CharacterStateHash(new StableHash(reader.ReadString()));
                actors[i] = new RollbackActorHash(actorId, gameplayContentHash, characterStateHash);
            }
            return RollbackStateHashReport.FromOwnedActors(peerId, tick, worldHash, rosterHash, kccHash, actors);
        }

        static void WriteComponentIdentity(CanonicalWriter writer, SimulationComponentIdentity value)
        {
            writer.WriteByte((byte)value.Role);
            writer.WriteString(value.ComponentId);
            writer.WriteString(value.SemanticVersion);
            writer.WriteHash(value.ConfigurationHash);
        }

        static SimulationComponentIdentity ReadComponentIdentity(CanonicalReader reader)
        {
            byte role = reader.ReadByte();
            if (role != (byte)SimulationComponentRole.ExecutionBackend &&
                role != (byte)SimulationComponentRole.SessionSource &&
                role != (byte)SimulationComponentRole.WorldSolver &&
                role != (byte)SimulationComponentRole.SnapshotCodec &&
                role != (byte)SimulationComponentRole.Committer &&
                role != (byte)SimulationComponentRole.Model &&
                role != (byte)SimulationComponentRole.Endpoint &&
                role != (byte)SimulationComponentRole.Diagnostics)
                throw new InvalidDataException($"Rollback component role '{role}' is invalid.");
            return new SimulationComponentIdentity(
                (SimulationComponentRole)role,
                reader.ReadString(),
                reader.ReadString(),
                new StableHash(reader.ReadString()));
        }

        static RollbackProtocolMessageKind ReadKind(byte value)
        {
            if (value != (byte)RollbackProtocolMessageKind.Handshake &&
                value != (byte)RollbackProtocolMessageKind.Roster &&
                value != (byte)RollbackProtocolMessageKind.ActorInputBatch &&
                value != (byte)RollbackProtocolMessageKind.CanonicalBundle &&
                value != (byte)RollbackProtocolMessageKind.StateHash &&
                value != (byte)RollbackProtocolMessageKind.SnapshotRequest &&
                value != (byte)RollbackProtocolMessageKind.SnapshotResponse &&
                value != (byte)RollbackProtocolMessageKind.Leave &&
                value != (byte)RollbackProtocolMessageKind.CanonicalConfirmation &&
                value != (byte)RollbackProtocolMessageKind.RelayedExplicitInputBatch)
                throw new InvalidDataException($"Rollback protocol message kind '{value}' is invalid.");
            return (RollbackProtocolMessageKind)value;
        }

        static int ReadCount(CanonicalReader reader)
        {
            int value = reader.ReadInt32();
            if (value < 0 || value > 1000000)
                throw new InvalidDataException($"Rollback protocol count '{value}' is invalid.");
            return value;
        }

        interface IRollbackStateHashActors
        {
            int Count { get; }
            ActorId ActorId(int index);
            GameplayContentHash GameplayContentHash(int index);
            CharacterStateHash CharacterStateHash(int index);
        }

        readonly struct ReportStateHashActors : IRollbackStateHashActors
        {
            readonly RollbackStateHashReport m_Report;

            public ReportStateHashActors(RollbackStateHashReport report)
            {
                m_Report = report ?? throw new ArgumentNullException(nameof(report));
            }

            public int Count => m_Report.Actors.Count;
            public ActorId ActorId(int index) => m_Report.Actors[index].ActorId;
            public GameplayContentHash GameplayContentHash(int index) => m_Report.Actors[index].GameplayContentHash;
            public CharacterStateHash CharacterStateHash(int index) => m_Report.Actors[index].CharacterStateHash;
        }

        readonly struct SnapshotStateHashActors : IRollbackStateHashActors
        {
            readonly SimulationWorldSnapshot m_World;

            public SnapshotStateHashActors(SimulationWorldSnapshot world)
            {
                m_World = world ?? throw new ArgumentNullException(nameof(world));
            }

            public int Count => m_World.Actors.Count;
            public ActorId ActorId(int index) => m_World.Actors[index].ActorId;
            public GameplayContentHash GameplayContentHash(int index) => m_World.Actors[index].GameplayContentHash;
            public CharacterStateHash CharacterStateHash(int index) => m_World.Actors[index].StateHash;
        }
    }
}
