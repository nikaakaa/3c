using System;
using System.Collections.Generic;
using System.IO;
using ThirdPersonSimulation;

namespace ThirdPersonSimulation.ServerAuthoritative
{
    internal static class ServerAuthoritativePredictionStateCodec
    {
        const uint CorrectionMagic = 0x43524153;
        const int CorrectionVersion = 4;
        const uint HistoryMagic = 0x48524153;
        const int HistoryVersion = 3;
        const uint JournalMagic = 0x4a524153;

        public static byte[] WriteCorrection(ServerAuthoritativePredictionCorrectionCheckpoint checkpoint)
        {
            if (checkpoint == null)
                throw new ArgumentNullException(nameof(checkpoint));
            using var writer = new CanonicalWriter();
            writer.WriteUInt32(CorrectionMagic);
            writer.WriteInt32(CorrectionVersion);
            writer.WriteUInt64(checkpoint.ConfirmedInputSequence);
            writer.WriteUInt64(checkpoint.ConfirmedEventHorizon.Sequence);
            writer.WriteString(checkpoint.ConfirmedEventHorizon.IsEmpty
                ? string.Empty
                : checkpoint.ConfirmedEventHorizon.EventId.ToString());
            writer.WriteUInt64(checkpoint.LastAuthorityAckTick);
            writer.WriteUInt64(checkpoint.LastBaselineTick);
            writer.WriteUInt64(checkpoint.LastAuthorityClockEstimate);
            writer.WriteInt32(checkpoint.PendingRequests.Count);
            for (int i = 0; i < checkpoint.PendingRequests.Count; i++)
            {
                SimulationInputRequest request = checkpoint.PendingRequests[i];
                writer.WriteString(request.RequestId);
                writer.WriteUInt64(request.Sequence);
                writer.WriteUInt64(request.SourceTick);
                writer.WriteUInt64(request.ExpireSimulationTick);
                writer.WriteInt32(request.Priority);
            }
            return writer.ToArray();
        }

        public static ServerAuthoritativePredictionCorrectionCheckpoint ReadCorrection(
            byte[] bytes,
            int requestCapacity)
        {
            var reader = new CanonicalReader(bytes);
            RequireHeader(reader, CorrectionMagic, CorrectionVersion);
            ulong confirmedInputSequence = reader.ReadUInt64();
            ulong horizonSequence = reader.ReadUInt64();
            string eventId = reader.ReadString();
            ServerAuthoritativeEventHorizon horizon = horizonSequence == 0
                ? ServerAuthoritativeEventHorizon.Empty
                : new ServerAuthoritativeEventHorizon(horizonSequence, new EventId(new StableHash(eventId)));
            ulong lastAuthorityAckTick = reader.ReadUInt64();
            ulong lastBaselineTick = reader.ReadUInt64();
            ulong lastAuthorityClockEstimate = reader.ReadUInt64();
            int pendingCount = RequireCount(reader.ReadInt32(), requestCapacity);
            var pending = new SortedDictionary<ulong, SimulationInputRequest>();
            for (int i = 0; i < pendingCount; i++)
            {
                var request = new SimulationInputRequest(
                    reader.ReadString(),
                    reader.ReadUInt64(),
                    reader.ReadUInt64(),
                    reader.ReadUInt64(),
                    reader.ReadInt32());
                if (pending.ContainsKey(request.Sequence))
                    throw new InvalidDataException("Prediction pending request payload contains a duplicate sequence.");
                pending.Add(request.Sequence, request);
            }
            reader.RequireComplete();
            return new ServerAuthoritativePredictionCorrectionCheckpoint(
                confirmedInputSequence,
                horizon,
                lastAuthorityAckTick,
                lastBaselineTick,
                lastAuthorityClockEstimate,
                pending.Values);
        }

        public static byte[] WriteHistory(ServerAuthoritativePredictionHistoryCheckpoint checkpoint)
        {
            if (checkpoint == null)
                throw new ArgumentNullException(nameof(checkpoint));
            using var writer = new CanonicalWriter();
            writer.WriteUInt32(HistoryMagic);
            writer.WriteInt32(HistoryVersion);
            writer.WriteInt32(checkpoint.Records.Count);
            for (int i = 0; i < checkpoint.Records.Count; i++)
            {
                ServerAuthoritativePredictionHistoryRecord record = checkpoint.Records[i].Value;
                writer.WriteUInt64(record.Tick.Value);
                writer.WriteString(record.Input.ActorId.Value);
                writer.WriteUInt64(record.Input.SourceTick);
                writer.WriteUInt64(record.Input.InputSequence);
                writer.WriteBytes(ServerAuthoritativeCanonicalCodec.WriteInput(record.Input.Input));
                writer.WriteString(record.CompositionIdentity.ToString());
                writer.WriteBytes(SimulationWorldSnapshotCodec.Write(record.World));
                writer.WriteBytes(WritePipelineProjection(record.PipelineProjection));
                WriteObservedFrame(writer, record.ObservedWorldConstraints);
                writer.WriteUInt64(record.JournalCursor);
            }
            writer.WriteInt32(checkpoint.RemoteBodies.Actors.Count);
            for (int actorIndex = 0; actorIndex < checkpoint.RemoteBodies.Actors.Count; actorIndex++)
            {
                ServerAuthoritativeRemoteBodyActorCheckpoint actor = checkpoint.RemoteBodies.Actors[actorIndex];
                writer.WriteString(actor.ActorId.Value);
                writer.WriteInt32(actor.Samples.Count);
                for (int sampleIndex = 0; sampleIndex < actor.Samples.Count; sampleIndex++)
                    WriteBodySample(writer, actor.Samples[sampleIndex]);
            }
            return writer.ToArray();
        }

        public static ServerAuthoritativePredictionHistoryCheckpoint ReadHistory(
            byte[] bytes,
            int historyCapacity)
        {
            var reader = new CanonicalReader(bytes);
            RequireHeader(reader, HistoryMagic, HistoryVersion);
            int count = RequireCount(reader.ReadInt32(), historyCapacity);
            var records = new SortedDictionary<ulong, ServerAuthoritativePredictionHistoryRecord>();
            for (int i = 0; i < count; i++)
            {
                var tick = new SimulationTick(reader.ReadUInt64());
                var actorId = new ActorId(reader.ReadString());
                ulong sourceTick = reader.ReadUInt64();
                ulong inputSequence = reader.ReadUInt64();
                CharacterSimulationInput input = ServerAuthoritativeCanonicalCodec.ReadInput(reader.ReadBytes());
                var ownerInput = new OwnerCanonicalInputBatch(actorId, sourceTick, inputSequence, input);
                var composition = new SimulationSessionCompositionIdentity(new StableHash(reader.ReadString()));
                SimulationWorldSnapshot world = SimulationWorldSnapshotCodec.Read(reader.ReadBytes());
                SimulationPipelineStateSnapshot pipeline = ReadPipelineProjection(reader.ReadBytes());
                ObservedWorldConstraintFrame observed = ReadObservedFrame(reader);
                ulong journalCursor = reader.ReadUInt64();
                var record = new ServerAuthoritativePredictionHistoryRecord(
                    ownerInput,
                    composition,
                    world,
                    pipeline,
                    observed,
                    journalCursor);
                if (record.Tick != tick || records.ContainsKey(tick.Value))
                    throw new InvalidDataException("Prediction history payload Tick order is invalid.");
                records.Add(tick.Value, record);
            }
            int actorCount = RequireCount(reader.ReadInt32(), 64);
            var remoteActors = new ServerAuthoritativeRemoteBodyActorCheckpoint[actorCount];
            for (int actorIndex = 0; actorIndex < actorCount; actorIndex++)
            {
                var actorId = new ActorId(reader.ReadString());
                int sampleCount = RequireCount(reader.ReadInt32(), checked(historyCapacity * 4));
                var samples = new CharacterBodySample[sampleCount];
                for (int sampleIndex = 0; sampleIndex < sampleCount; sampleIndex++)
                    samples[sampleIndex] = ReadBodySample(reader);
                remoteActors[actorIndex] = new ServerAuthoritativeRemoteBodyActorCheckpoint(actorId, samples);
            }
            reader.RequireComplete();
            return new ServerAuthoritativePredictionHistoryCheckpoint(
                records,
                new ServerAuthoritativeRemoteBodyTimelineCheckpoint(remoteActors));
        }

        public static byte[] WriteJournal(ServerAuthoritativePredictionJournalCheckpoint checkpoint)
        {
            if (checkpoint == null)
                throw new ArgumentNullException(nameof(checkpoint));
            using var writer = new CanonicalWriter();
            writer.WriteUInt32(JournalMagic);
            writer.WriteInt32(ServerAuthoritativePredictionPassIds.JournalStateSchemaVersion);
            writer.WriteUInt64(checkpoint.Cursor);
            writer.WriteInt32(checkpoint.Entries.Count);
            for (int i = 0; i < checkpoint.Entries.Count; i++)
            {
                KeyValuePair<EventId, ServerAuthoritativeJournalEntry> pair = checkpoint.Entries[i];
                writer.WriteString(pair.Key.ToString());
                writer.WriteUInt64(pair.Value.Tick.Value);
                writer.WriteUInt64(pair.Value.Sequence);
                writer.WriteByte((byte)pair.Value.Disposition);
            }
            return writer.ToArray();
        }

        public static ServerAuthoritativePredictionJournalCheckpoint ReadJournal(
            byte[] bytes,
            int historyCapacity)
        {
            var reader = new CanonicalReader(bytes);
            RequireHeader(reader, JournalMagic, ServerAuthoritativePredictionPassIds.JournalStateSchemaVersion);
            ulong cursor = reader.ReadUInt64();
            int count = RequireCount(reader.ReadInt32(), checked(historyCapacity * 64));
            var entries = new SortedDictionary<EventId, ServerAuthoritativeJournalEntry>();
            for (int i = 0; i < count; i++)
            {
                var eventId = new EventId(new StableHash(reader.ReadString()));
                var tick = new SimulationTick(reader.ReadUInt64());
                ulong sequence = reader.ReadUInt64();
                var disposition = (ServerAuthoritativeEventDisposition)reader.ReadByte();
                if (!eventId.IsValid || !tick.IsValid || sequence == 0 ||
                    !Enum.IsDefined(typeof(ServerAuthoritativeEventDisposition), disposition) ||
                    entries.ContainsKey(eventId))
                {
                    throw new InvalidDataException("Prediction disposition journal payload is invalid.");
                }
                entries.Add(eventId, new ServerAuthoritativeJournalEntry(eventId, tick, sequence, disposition));
            }
            reader.RequireComplete();
            return new ServerAuthoritativePredictionJournalCheckpoint(entries, cursor, 0);
        }

        static void WriteObservedFrame(CanonicalWriter writer, ObservedWorldConstraintFrame frame)
        {
            writer.WriteUInt64(frame.Tick.Value);
            writer.WriteString(frame.FrameHash.Value);
            writer.WriteInt32(frame.Constraints.Count);
            for (int i = 0; i < frame.Constraints.Count; i++)
            {
                ObservedWorldConstraint value = frame.Constraints[i];
                writer.WriteString(value.ActorId.Value);
                writer.WriteUInt64(value.TargetTick.Value);
                WriteBody(writer, value.BeforeBody);
                WriteBody(writer, value.FinalBody);
                writer.WriteUInt64(value.SourcePreviousTick.Value);
                writer.WriteUInt64(value.SourceCurrentTick.Value);
                writer.WriteByte((byte)value.SamplingKind);
                writer.WriteString(value.ContactShapeConfigurationHash.Value);
            }
        }

        static ObservedWorldConstraintFrame ReadObservedFrame(CanonicalReader reader)
        {
            var tick = new SimulationTick(reader.ReadUInt64());
            var expectedHash = new StableHash(reader.ReadString());
            int count = RequireCount(reader.ReadInt32(), 64);
            var constraints = new ObservedWorldConstraint[count];
            for (int i = 0; i < count; i++)
            {
                constraints[i] = new ObservedWorldConstraint(
                    new ActorId(reader.ReadString()),
                    new SimulationTick(reader.ReadUInt64()),
                    ReadBody(reader),
                    ReadBody(reader),
                    new SimulationTick(reader.ReadUInt64()),
                    new SimulationTick(reader.ReadUInt64()),
                    (ObservedWorldConstraintSamplingKind)reader.ReadByte(),
                    new StableHash(reader.ReadString()));
            }
            var frame = new ObservedWorldConstraintFrame(tick, constraints);
            if (frame.FrameHash != expectedHash)
                throw new InvalidDataException("Prediction history observed frame hash does not match its canonical value.");
            return frame;
        }

        static void WriteBodySample(CanonicalWriter writer, CharacterBodySample sample)
        {
            writer.WriteString(sample.ActorId.Value);
            writer.WriteUInt64(sample.Tick.Value);
            WriteBody(writer, sample.BeforeBody);
            WriteBody(writer, sample.FinalBody);
            writer.WriteVector3(sample.AppliedDisplacement);
            writer.WriteScalar(sample.AppliedYawDegrees);
        }

        static CharacterBodySample ReadBodySample(CanonicalReader reader)
        {
            var actorId = new ActorId(reader.ReadString());
            return new CharacterBodySample(
                actorId,
                new SimulationTick(reader.ReadUInt64()),
                ReadBody(reader),
                ReadBody(reader),
                reader.ReadVector3(),
                reader.ReadScalar());
        }

        static void WriteBody(CanonicalWriter writer, WorldBodyState body)
        {
            writer.WriteString(body.ActorId.Value);
            writer.WriteVector3(body.Position);
            writer.WriteYaw(body.Yaw);
            writer.WriteVector3(body.Velocity);
            writer.WriteScalar(body.VerticalVelocity);
            writer.WriteBoolean(body.Grounded);
            writer.WriteUInt32((uint)body.Collision);
        }

        static WorldBodyState ReadBody(CanonicalReader reader) => new WorldBodyState(
            new ActorId(reader.ReadString()),
            reader.ReadVector3(),
            reader.ReadYaw(),
            reader.ReadVector3(),
            reader.ReadScalar(),
            reader.ReadBoolean(),
            (WorldCollisionSummary)reader.ReadUInt32());

        static byte[] WritePipelineProjection(SimulationPipelineStateSnapshot snapshot)
        {
            using var writer = new CanonicalWriter();
            writer.WriteString(snapshot.Pipeline.Id.Value);
            writer.WriteString(snapshot.Pipeline.Revision.Value);
            writer.WriteInt32(snapshot.Pipeline.SchemaVersion.Value);
            writer.WriteString(snapshot.Pipeline.Hash.ToString());
            writer.WriteByte((byte)snapshot.Backend.Role);
            writer.WriteString(snapshot.Backend.ComponentId);
            writer.WriteString(snapshot.Backend.SemanticVersion);
            writer.WriteString(snapshot.Backend.ConfigurationHash.ToString());
            writer.WriteUInt64(snapshot.LastCompletedTick);
            writer.WriteInt32(snapshot.Participants.Count);
            for (int i = 0; i < snapshot.Participants.Count; i++)
            {
                SimulationPipelinePassStateSnapshot participant = snapshot.Participants[i];
                writer.WriteString(participant.PassId.Value);
                writer.WriteString(participant.ImplementationVersion.Value);
                writer.WriteString(participant.StateOwner);
                writer.WriteString(participant.StateSchemaId);
                writer.WriteInt32(participant.StateSchemaVersion);
                writer.WriteString(participant.StateHash.ToString());
                writer.WriteBytes(participant.CopyPayload());
            }
            return writer.ToArray();
        }

        static SimulationPipelineStateSnapshot ReadPipelineProjection(byte[] bytes)
        {
            var reader = new CanonicalReader(bytes);
            var pipeline = new SimulationPipelineIdentity(
                new SimulationPipelineId(reader.ReadString()),
                new SimulationPipelineRevision(reader.ReadString()),
                new SimulationPipelineSchemaVersion(reader.ReadInt32()),
                new SimulationPipelineHash(new StableHash(reader.ReadString())));
            var backend = new SimulationComponentIdentity(
                (SimulationComponentRole)reader.ReadByte(),
                reader.ReadString(),
                reader.ReadString(),
                new StableHash(reader.ReadString()));
            ulong tick = reader.ReadUInt64();
            int count = RequireCount(reader.ReadInt32(), 64);
            var participants = new SimulationPipelinePassStateSnapshot[count];
            for (int i = 0; i < count; i++)
            {
                participants[i] = new SimulationPipelinePassStateSnapshot(
                    new SimulationPipelinePassId(reader.ReadString()),
                    new SimulationPipelinePassImplementationVersion(reader.ReadString()),
                    reader.ReadString(),
                    reader.ReadString(),
                    reader.ReadInt32(),
                    new StableHash(reader.ReadString()),
                    reader.ReadBytes());
            }
            reader.RequireComplete();
            return new SimulationPipelineStateSnapshot(pipeline, backend, tick, participants);
        }

        static void RequireHeader(CanonicalReader reader, uint magic, int version)
        {
            if (reader.ReadUInt32() != magic || reader.ReadInt32() != version)
                throw new InvalidDataException("ServerAuthoritative prediction state header is invalid.");
        }

        static int RequireCount(int count, int maximum)
        {
            if (count < 0 || count > maximum)
                throw new InvalidDataException("ServerAuthoritative prediction state count is invalid.");
            return count;
        }
    }
}
