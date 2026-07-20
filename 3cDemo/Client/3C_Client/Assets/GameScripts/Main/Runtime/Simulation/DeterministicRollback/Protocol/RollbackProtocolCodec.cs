using System;
using System.Collections.Generic;
using System.IO;

namespace ThirdPersonSimulation.DeterministicRollback
{
    public static class RollbackProtocolCodec
    {
        const uint Magic = 0x50524244;
        const uint PayloadMagic = 0x4C505244;
        const int Version = 5;

        public static byte[] Write(RollbackProtocolEnvelope envelope)
        {
            if (envelope == null)
                throw new ArgumentNullException(nameof(envelope));
            using var writer = new CanonicalWriter();
            writer.WriteUInt32(Magic);
            writer.WriteInt32(Version);
            writer.WriteString(envelope.SessionId);
            writer.WriteString(envelope.SenderPeerId);
            writer.WriteUInt64(envelope.Sequence);
            writer.WriteByte((byte)envelope.Payload.Kind);
            WritePayload(writer, envelope.Payload);
            return writer.ToArray();
        }

        public static RollbackProtocolEnvelope Read(byte[] bytes)
        {
            var reader = new CanonicalReader(bytes ?? throw new ArgumentNullException(nameof(bytes)));
            if (reader.ReadUInt32() != Magic || reader.ReadInt32() != Version)
                throw new InvalidDataException("Rollback protocol envelope header is invalid.");
            string sessionId = reader.ReadString();
            string senderPeerId = reader.ReadString();
            ulong sequence = reader.ReadUInt64();
            RollbackProtocolMessageKind kind = ReadKind(reader.ReadByte());
            IRollbackProtocolPayload payload = ReadPayload(reader, kind);
            reader.RequireComplete();
            var envelope = new RollbackProtocolEnvelope(sessionId, senderPeerId, sequence, payload);
            if (!BytesEqual(bytes, Write(envelope)))
                throw new InvalidDataException("Rollback protocol envelope is not canonical.");
            return envelope;
        }

        public static byte[] WriteCanonicalPayload(IRollbackProtocolPayload payload)
        {
            if (payload == null)
                throw new ArgumentNullException(nameof(payload));
            using var writer = new CanonicalWriter();
            writer.WriteUInt32(PayloadMagic);
            writer.WriteInt32(Version);
            writer.WriteByte((byte)payload.Kind);
            WritePayload(writer, payload);
            return writer.ToArray();
        }

        public static IRollbackProtocolPayload ReadCanonicalPayload(byte[] bytes)
        {
            var reader = new CanonicalReader(bytes ?? throw new ArgumentNullException(nameof(bytes)));
            if (reader.ReadUInt32() != PayloadMagic || reader.ReadInt32() != Version)
                throw new InvalidDataException("Rollback canonical payload header is invalid.");
            IRollbackProtocolPayload payload = ReadPayload(reader, ReadKind(reader.ReadByte()));
            reader.RequireComplete();
            if (!BytesEqual(bytes, WriteCanonicalPayload(payload)))
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
                    writer.WriteBytes(RollbackInputCodec.WriteBundle(bundle));
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
                    writer.WriteString(request.ExpectedWorldHash.Value);
                    break;
                case RollbackSnapshotResponse response:
                    writer.WriteString(response.AuthorityPeerId);
                    writer.WriteString(response.RequesterPeerId);
                    writer.WriteUInt64(response.Tick.Value);
                    writer.WriteString(response.SnapshotHash.Value);
                    writer.WriteBytes(response.CopySnapshotBytes());
                    break;
                case RollbackLeave leave:
                    writer.WriteString(leave.PeerId);
                    writer.WriteString(leave.Reason);
                    break;
                default:
                    throw new InvalidDataException($"Rollback payload type '{payload.GetType().FullName}' is unsupported.");
            }
        }

        static IRollbackProtocolPayload ReadPayload(CanonicalReader reader, RollbackProtocolMessageKind kind)
        {
            return kind switch
            {
                RollbackProtocolMessageKind.Handshake => ReadHandshake(reader),
                RollbackProtocolMessageKind.Roster => ReadRoster(reader),
                RollbackProtocolMessageKind.ActorInputBatch => ReadInputBatch(reader),
                RollbackProtocolMessageKind.RelayedExplicitInputBatch => ReadRelayedInputBatch(reader),
                RollbackProtocolMessageKind.CanonicalBundle => RollbackInputCodec.ReadBundle(reader.ReadBytes()),
                RollbackProtocolMessageKind.CanonicalConfirmation => ReadCanonicalConfirmation(reader),
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
                    reader.ReadBytes()),
                RollbackProtocolMessageKind.Leave => new RollbackLeave(reader.ReadString(), reader.ReadString()),
                _ => throw new InvalidDataException($"Rollback payload kind '{kind}' is unsupported.")
            };
        }

        static void WriteInputBatch(CanonicalWriter writer, RollbackActorInputBatch value)
        {
            writer.WriteInt32(value.Frames.Count);
            for (int i = 0; i < value.Frames.Count; i++)
                writer.WriteBytes(RollbackInputCodec.WriteInput(value.Frames[i]));
        }

        static RollbackActorInputBatch ReadInputBatch(CanonicalReader reader)
        {
            int count = ReadCount(reader);
            var frames = new RollbackActorInputFrame[count];
            for (int i = 0; i < count; i++)
                frames[i] = RollbackInputCodec.ReadInput(reader.ReadBytes());
            return new RollbackActorInputBatch(frames);
        }

        static void WriteRelayedInputBatch(CanonicalWriter writer, RollbackRelayedExplicitInputBatch value)
        {
            writer.WriteInt32(value.Frames.Count);
            for (int i = 0; i < value.Frames.Count; i++)
                writer.WriteBytes(RollbackInputCodec.WriteInput(value.Frames[i]));
        }

        static RollbackRelayedExplicitInputBatch ReadRelayedInputBatch(CanonicalReader reader)
        {
            int count = ReadCount(reader);
            var frames = new RollbackActorInputFrame[count];
            for (int i = 0; i < count; i++)
                frames[i] = RollbackInputCodec.ReadInput(reader.ReadBytes());
            return new RollbackRelayedExplicitInputBatch(frames);
        }

        static void WriteCanonicalConfirmation(CanonicalWriter writer, RollbackCanonicalConfirmation value)
        {
            writer.WriteUInt64(value.PreviousConfirmedTick);
            writer.WriteUInt64(value.ConfirmedTick.Value);
            writer.WriteInt32(value.FinalBundles.Count);
            for (int i = 0; i < value.FinalBundles.Count; i++)
                writer.WriteBytes(RollbackInputCodec.WriteBundle(value.FinalBundles[i]));
        }

        static RollbackCanonicalConfirmation ReadCanonicalConfirmation(CanonicalReader reader)
        {
            ulong previousConfirmedTick = reader.ReadUInt64();
            var confirmedTick = new SimulationTick(reader.ReadUInt64());
            int count = ReadCount(reader);
            var bundles = new RollbackCanonicalInputBundle[count];
            for (int i = 0; i < count; i++)
                bundles[i] = RollbackInputCodec.ReadBundle(reader.ReadBytes());
            return new RollbackCanonicalConfirmation(previousConfirmedTick, confirmedTick, bundles);
        }

        static void WriteHandshake(CanonicalWriter writer, RollbackHandshake value)
        {
            writer.WriteString(value.PeerId);
            WriteComponentIdentity(writer, value.Model);
            writer.WriteString(value.SemanticHash.ToString());
            writer.WriteString(value.FixedProgramHash.ToString());
            writer.WriteString(value.FixedLayoutHash.ToString());
            writer.WriteInt32(value.TickRate);
            writer.WriteString(value.CollisionWorldHash.Value);
            writer.WriteString(value.KccIdentityHash.Value);
            writer.WriteString(value.Protocol.ProtocolId);
            writer.WriteString(value.Protocol.SemanticVersion);
            writer.WriteString(value.Protocol.SchemaHash.Value);
        }

        static RollbackHandshake ReadHandshake(CanonicalReader reader)
        {
            return new RollbackHandshake(
                reader.ReadString(),
                ReadComponentIdentity(reader),
                new SemanticHash(new StableHash(reader.ReadString())),
                new ProgramHash(new StableHash(reader.ReadString())),
                new LayoutHash(new StableHash(reader.ReadString())),
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
            return new RollbackRoster(revision, entries);
        }

        static void WriteStateHash(CanonicalWriter writer, RollbackStateHashReport value)
        {
            writer.WriteString(value.PeerId);
            writer.WriteUInt64(value.Tick.Value);
            writer.WriteString(value.WorldHash.Value);
            writer.WriteString(value.RosterHash.Value);
            writer.WriteString(value.KccHash.Value);
            writer.WriteInt32(value.Actors.Count);
            for (int i = 0; i < value.Actors.Count; i++)
            {
                RollbackActorHash actor = value.Actors[i];
                writer.WriteString(actor.ActorId.Value);
                writer.WriteString(actor.ActorHash.Value);
                writer.WriteInt32(actor.Modules.Count);
                for (int moduleIndex = 0; moduleIndex < actor.Modules.Count; moduleIndex++)
                {
                    writer.WriteString(actor.Modules[moduleIndex].Key);
                    writer.WriteString(actor.Modules[moduleIndex].Value.Value);
                }
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
                var actorHash = new StableHash(reader.ReadString());
                int moduleCount = ReadCount(reader);
                var modules = new KeyValuePair<string, StableHash>[moduleCount];
                for (int moduleIndex = 0; moduleIndex < moduleCount; moduleIndex++)
                    modules[moduleIndex] = new KeyValuePair<string, StableHash>(reader.ReadString(), new StableHash(reader.ReadString()));
                actors[i] = new RollbackActorHash(actorId, actorHash, modules);
            }
            return new RollbackStateHashReport(peerId, tick, worldHash, rosterHash, kccHash, actors);
        }

        static void WriteComponentIdentity(CanonicalWriter writer, SimulationComponentIdentity value)
        {
            writer.WriteByte((byte)value.Role);
            writer.WriteString(value.ComponentId);
            writer.WriteString(value.SemanticVersion);
            writer.WriteString(value.ConfigurationHash.Value);
        }

        static SimulationComponentIdentity ReadComponentIdentity(CanonicalReader reader)
        {
            byte role = reader.ReadByte();
            if (!Enum.IsDefined(typeof(SimulationComponentRole), role))
                throw new InvalidDataException($"Rollback component role '{role}' is invalid.");
            return new SimulationComponentIdentity(
                (SimulationComponentRole)role,
                reader.ReadString(),
                reader.ReadString(),
                new StableHash(reader.ReadString()));
        }

        static RollbackProtocolMessageKind ReadKind(byte value)
        {
            if (!Enum.IsDefined(typeof(RollbackProtocolMessageKind), value))
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

        static bool BytesEqual(byte[] left, byte[] right)
        {
            if (left.Length != right.Length)
                return false;
            for (int i = 0; i < left.Length; i++)
            {
                if (left[i] != right[i])
                    return false;
            }
            return true;
        }
    }
}
