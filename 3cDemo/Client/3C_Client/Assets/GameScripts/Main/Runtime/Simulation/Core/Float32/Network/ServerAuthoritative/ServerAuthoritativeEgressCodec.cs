using System;
using System.Collections.Generic;
using System.IO;
using ThirdPersonSimulation;

namespace ThirdPersonSimulation.ServerAuthoritative
{
    public static class ServerAuthoritativeEgressChannels
    {
        public const string ClientInput = "server-authoritative.client-input";
        public const string AuthorityReplication = "server-authoritative.authority-replication";
        public const string RemotePresentation = "server-authoritative.remote-presentation";
        public const string ClientInputSchema = "server-authoritative-client-input/1";
        public const string AuthorityReplicationSchema = "server-authoritative-authority-replication/5";
        public const string RemotePresentationSchema = "server-authoritative-remote-presentation/5";
        public const int SchemaVersion = 1;
        public const int AuthorityReplicationSchemaVersion = 5;
        public const int RemotePresentationSchemaVersion = 5;
    }

    public static class ServerAuthoritativeEgressCodec
    {
        const uint InputMagic = 0x49454153;
        const uint ReplicationMagic = 0x52454153;
        const uint RemoteMagic = 0x50454153;
        const int InputVersion = 1;
        const int ReplicationVersion = 5;
        const int RemoteVersion = 5;
        const int MaximumCount = 4096;

        public static byte[] WriteOwnerInput(OwnerCanonicalInputBatch input)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));
            using var writer = new CanonicalWriter();
            writer.WriteUInt32(InputMagic);
            writer.WriteInt32(InputVersion);
            writer.WriteString(input.ActorId.Value);
            writer.WriteUInt64(input.SourceTick);
            writer.WriteUInt64(input.InputSequence);
            writer.WriteBytes(ServerAuthoritativeCanonicalCodec.WriteInput(input.Input));
            return writer.ToArray();
        }

        public static OwnerCanonicalInputBatch ReadOwnerInput(byte[] bytes)
        {
            CanonicalReader reader = Reader(bytes, InputMagic, InputVersion, "owner input");
            var actorId = new ActorId(reader.ReadString());
            ulong sourceTick = reader.ReadUInt64();
            ulong inputSequence = reader.ReadUInt64();
            CharacterSimulationInput input = ServerAuthoritativeCanonicalCodec.ReadInput(reader.ReadBytes());
            reader.RequireComplete();
            return new OwnerCanonicalInputBatch(actorId, sourceTick, inputSequence, input);
        }

        public static byte[] WriteAuthorityReplication(AuthorityReplicationBatch batch)
        {
            if (batch == null)
                throw new ArgumentNullException(nameof(batch));
            using var writer = new CanonicalWriter();
            writer.WriteUInt32(ReplicationMagic);
            writer.WriteInt32(ReplicationVersion);
            writer.WriteUInt64(batch.AuthorityTick.Value);
            writer.WriteInt32(batch.Acks.Count);
            for (int i = 0; i < batch.Acks.Count; i++)
                WriteAck(writer, batch.Acks[i]);
            writer.WriteInt32(batch.Baselines.Count);
            for (int i = 0; i < batch.Baselines.Count; i++)
                writer.WriteBytes(ServerAuthoritativeCanonicalCodec.WriteBaseline(batch.Baselines[i]));
            writer.WriteInt32(batch.RemotePresentation.Count);
            for (int i = 0; i < batch.RemotePresentation.Count; i++)
                writer.WriteBytes(WriteRemotePresentation(batch.RemotePresentation[i]));
            return writer.ToArray();
        }

        public static AuthorityReplicationBatch ReadAuthorityReplication(byte[] bytes)
        {
            CanonicalReader reader = Reader(bytes, ReplicationMagic, ReplicationVersion, "authority replication");
            var tick = new SimulationTick(reader.ReadUInt64());
            int ackCount = ReadCount(reader, "input ack");
            var acks = new AuthoritativeInputAck[ackCount];
            for (int i = 0; i < ackCount; i++)
                acks[i] = ReadAck(reader);
            int baselineCount = ReadCount(reader, "baseline");
            var baselines = new AuthoritativeActorBaseline[baselineCount];
            for (int i = 0; i < baselineCount; i++)
                baselines[i] = ServerAuthoritativeCanonicalCodec.ReadBaseline(reader.ReadBytes());
            int remoteCount = ReadCount(reader, "remote presentation");
            var remote = new RemotePresentationBatch[remoteCount];
            for (int i = 0; i < remoteCount; i++)
                remote[i] = ReadRemotePresentation(reader.ReadBytes());
            reader.RequireComplete();
            return new AuthorityReplicationBatch(tick, acks, baselines, remote);
        }

        static void WriteAck(CanonicalWriter writer, AuthoritativeInputAck ack)
        {
            writer.WriteString(ack.ActorId.Value);
            writer.WriteUInt64(ack.AuthorityTick.Value);
            writer.WriteUInt64(ack.ConfirmedInputSequence);
            writer.WriteUInt64(ack.ConfirmedEventHorizon.Sequence);
            writer.WriteString(ack.ConfirmedEventHorizon.IsEmpty
                ? string.Empty
                : ack.ConfirmedEventHorizon.EventId.ToString());
        }

        static AuthoritativeInputAck ReadAck(CanonicalReader reader)
        {
            var actorId = new ActorId(reader.ReadString());
            var tick = new SimulationTick(reader.ReadUInt64());
            ulong inputSequence = reader.ReadUInt64();
            ulong eventSequence = reader.ReadUInt64();
            string eventId = reader.ReadString();
            if ((eventSequence == 0) != string.IsNullOrEmpty(eventId))
                throw new InvalidDataException("Authority input ack Event horizon is invalid.");
            ServerAuthoritativeEventHorizon horizon = eventSequence == 0
                ? ServerAuthoritativeEventHorizon.Empty
                : new ServerAuthoritativeEventHorizon(eventSequence, new EventId(new StableHash(eventId)));
            return new AuthoritativeInputAck(actorId, tick, inputSequence, horizon);
        }

        public static byte[] WriteRemotePresentation(RemotePresentationBatch batch)
        {
            if (batch == null)
                throw new ArgumentNullException(nameof(batch));
            using var writer = new CanonicalWriter();
            writer.WriteUInt32(RemoteMagic);
            writer.WriteInt32(RemoteVersion);
            writer.WriteString(batch.ActorId.Value);
            writer.WriteBoolean(batch.ResetBodyStream);
            writer.WriteInt32(batch.BodySamples.Count);
            for (int i = 0; i < batch.BodySamples.Count; i++)
                WriteBodySample(writer, batch.BodySamples[i]);
            writer.WriteInt32(batch.SampleCommands.Count);
            for (int i = 0; i < batch.SampleCommands.Count; i++)
                WritePresentationCommand(writer, batch.SampleCommands[i]);
            writer.WriteInt32(batch.ReliableEvents.Count);
            for (int i = 0; i < batch.ReliableEvents.Count; i++)
                WriteReliableEvent(writer, batch.ReliableEvents[i]);
            return writer.ToArray();
        }

        public static RemotePresentationBatch ReadRemotePresentation(byte[] bytes)
        {
            CanonicalReader reader = Reader(bytes, RemoteMagic, RemoteVersion, "remote presentation");
            var actorId = new ActorId(reader.ReadString());
            bool resetBodyStream = reader.ReadBoolean();
            int bodyCount = ReadCount(reader, "body sample");
            var bodies = new CharacterBodySample[bodyCount];
            for (int i = 0; i < bodyCount; i++)
                bodies[i] = ReadBodySample(reader);
            int sampleCount = ReadCount(reader, "presentation sample");
            var samples = new PresentationCommand[sampleCount];
            for (int i = 0; i < sampleCount; i++)
                samples[i] = ReadPresentationCommand(reader);
            int eventCount = ReadCount(reader, "reliable event");
            var events = new ServerAuthoritativeReliableEvent[eventCount];
            for (int i = 0; i < eventCount; i++)
                events[i] = ReadReliableEvent(reader);
            reader.RequireComplete();
            return new RemotePresentationBatch(actorId, bodies, samples, events, resetBodyStream);
        }

        static void WriteReliableEvent(CanonicalWriter writer, ServerAuthoritativeReliableEvent value)
        {
            writer.WriteBoolean(value.IsGameplay);
            if (value.IsGameplay)
                WriteGameplayFact(writer, value.GameplayFact);
            else
                WritePresentationCommand(writer, value.PresentationCommand);
        }

        static ServerAuthoritativeReliableEvent ReadReliableEvent(CanonicalReader reader) =>
            reader.ReadBoolean()
                ? new ServerAuthoritativeReliableEvent(ReadGameplayFact(reader))
                : new ServerAuthoritativeReliableEvent(ReadPresentationCommand(reader));

        static void WriteGameplayFact(CanonicalWriter writer, GameplayFact fact)
        {
            WriteHeader(writer, fact.Header);
            writer.WriteByte((byte)fact.Kind);
            switch (fact.Kind)
            {
                case GameplayFactKind.Action:
                    WriteAction(writer, fact.Action);
                    break;
                case GameplayFactKind.ActionWindow:
                    WriteActionWindow(writer, fact.ActionWindow);
                    break;
                case GameplayFactKind.Effect:
                    WriteEffect(writer, fact.Effect);
                    break;
                case GameplayFactKind.Attribute:
                    WriteAttribute(writer, fact.Attribute);
                    break;
                case GameplayFactKind.Cue:
                    WriteCue(writer, fact.Cue);
                    break;
                case GameplayFactKind.Motion:
                case GameplayFactKind.State:
                    writer.WriteString(fact.SubjectId);
                    writer.WriteString(fact.StateId);
                    writer.WriteScalar(fact.Scalar);
                    break;
                default:
                    throw new InvalidDataException($"Unsupported GameplayFact kind '{fact.Kind}'.");
            }
        }

        static GameplayFact ReadGameplayFact(CanonicalReader reader)
        {
            SimulationEventHeader header = ReadHeader(reader);
            var kind = ReadEnum<GameplayFactKind>(reader.ReadByte(), "gameplay fact kind");
            return kind switch
            {
                GameplayFactKind.Action => new GameplayFact(header, ReadAction(reader)),
                GameplayFactKind.ActionWindow => new GameplayFact(header, ReadActionWindow(reader)),
                GameplayFactKind.Effect => new GameplayFact(header, ReadEffect(reader)),
                GameplayFactKind.Attribute => new GameplayFact(header, ReadAttribute(reader)),
                GameplayFactKind.Cue => new GameplayFact(header, ReadCue(reader)),
                GameplayFactKind.Motion or GameplayFactKind.State => new GameplayFact(
                    header,
                    kind,
                    reader.ReadString(),
                    reader.ReadString(),
                    reader.ReadScalar()),
                _ => throw new InvalidDataException($"Unsupported GameplayFact kind '{kind}'.")
            };
        }

        static void WritePresentationCommand(CanonicalWriter writer, PresentationCommand command)
        {
            WriteHeader(writer, command.Header);
            writer.WriteByte((byte)command.Kind);
            writer.WriteString(command.ProducerId);
            writer.WriteScalar(command.SampleTime);
            writer.WriteScalar(command.Weight);
            writer.WriteUInt64(command.ProducerGeneration);
            writer.WriteInt32(command.Cycle);
            writer.WriteUInt64(command.SourceActionInstanceId);
            writer.WriteScalar(command.VisualTimeScale);
        }

        static PresentationCommand ReadPresentationCommand(CanonicalReader reader) => new PresentationCommand(
            ReadHeader(reader),
            ReadEnum<PresentationCommandKind>(reader.ReadByte(), "presentation command kind"),
            reader.ReadString(),
            reader.ReadScalar(),
            reader.ReadScalar(),
            reader.ReadUInt64(),
            reader.ReadInt32(),
            reader.ReadUInt64(),
            reader.ReadScalar());

        static void WriteHeader(CanonicalWriter writer, SimulationEventHeader header)
        {
            SimulationNumericProfileCodec.Write(writer, header.NumericProfile);
            writer.WriteString(header.EventId.ToString());
            writer.WriteString(header.ActorId.Value);
            writer.WriteUInt64(header.Tick.Value);
            writer.WriteInt32(header.Activation.Operation.Value);
            writer.WriteUInt64(header.Activation.Generation);
            writer.WriteString(header.Activation.ExecutionPath);
            writer.WriteUInt64(header.Sequence);
            writer.WriteString(header.Channel);
        }

        static SimulationEventHeader ReadHeader(CanonicalReader reader) => new SimulationEventHeader(
            SimulationNumericProfileCodec.Read(reader),
            new EventId(new StableHash(reader.ReadString())),
            new ActorId(reader.ReadString()),
            new SimulationTick(reader.ReadUInt64()),
            new ActivationId(new OperationHandle(reader.ReadInt32()), reader.ReadUInt64(), reader.ReadString()),
            reader.ReadUInt64(),
            reader.ReadString());

        static void WriteBodySample(CanonicalWriter writer, CharacterBodySample sample)
        {
            writer.WriteString(sample.ActorId.Value);
            writer.WriteUInt64(sample.Tick.Value);
            WriteBody(writer, sample.BeforeBody);
            WriteBody(writer, sample.FinalBody);
            writer.WriteVector3(sample.AppliedDisplacement);
            writer.WriteScalar(sample.AppliedYawDegrees);
        }

        static CharacterBodySample ReadBodySample(CanonicalReader reader) => new CharacterBodySample(
            new ActorId(reader.ReadString()),
            new SimulationTick(reader.ReadUInt64()),
            ReadBody(reader),
            ReadBody(reader),
            reader.ReadVector3(),
            reader.ReadScalar());

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

        static void WriteAction(CanonicalWriter writer, ActionFact value)
        {
            writer.WriteUInt64(value.ActionInstanceId);
            writer.WriteUInt64(value.PredictionKey);
            writer.WriteUInt64(value.InputSequence);
            writer.WriteString(value.ActionId);
            writer.WriteByte((byte)value.TransitionType);
            writer.WriteByte((byte)value.Phase);
            writer.WriteByte((byte)value.State);
            writer.WriteString(value.Reason);
        }

        static ActionFact ReadAction(CanonicalReader reader) => new ActionFact(
            reader.ReadUInt64(),
            reader.ReadUInt64(),
            reader.ReadUInt64(),
            reader.ReadString(),
            ReadEnum<SimulationActionLifecycleTransitionType>(reader.ReadByte(), "action transition"),
            ReadEnum<SimulationActionPhase>(reader.ReadByte(), "action phase"),
            ReadEnum<SimulationActionState>(reader.ReadByte(), "action state"),
            reader.ReadString());

        static void WriteActionWindow(CanonicalWriter writer, ActionWindowFact value)
        {
            writer.WriteUInt64(value.ActionInstanceId);
            writer.WriteString(value.ActionId);
            writer.WriteString(value.WindowId);
            writer.WriteString(value.WindowType);
            writer.WriteUInt64(value.StartTick);
            writer.WriteUInt64(value.EndTick);
            writer.WriteUInt64(value.Digest);
        }

        static ActionWindowFact ReadActionWindow(CanonicalReader reader) => new ActionWindowFact(
            reader.ReadUInt64(),
            reader.ReadString(),
            reader.ReadString(),
            reader.ReadString(),
            reader.ReadUInt64(),
            reader.ReadUInt64(),
            reader.ReadUInt64());

        static void WriteEffect(CanonicalWriter writer, GameplayEffectFact value)
        {
            writer.WriteString(value.EffectId);
            writer.WriteUInt64(value.InstanceId);
            writer.WriteByte((byte)value.Operation);
            WriteEffectContext(writer, value.Context);
            writer.WriteUInt64(value.StartTick);
            writer.WriteUInt64(value.EndTick);
            writer.WriteInt32(value.StackCount);
            writer.WriteUInt64(value.LifecycleRevision);
            writer.WriteUInt32(value.DefinitionRevision);
            writer.WriteBoolean(value.Instant);
        }

        static GameplayEffectFact ReadEffect(CanonicalReader reader) => new GameplayEffectFact(
            reader.ReadString(),
            reader.ReadUInt64(),
            ReadEnum<SimulationGameplayEffectLifecycleOperation>(reader.ReadByte(), "effect operation"),
            ReadEffectContext(reader),
            reader.ReadUInt64(),
            reader.ReadUInt64(),
            reader.ReadInt32(),
            reader.ReadUInt64(),
            reader.ReadUInt32(),
            reader.ReadBoolean());

        static void WriteAttribute(CanonicalWriter writer, GameplayAttributeFact value)
        {
            writer.WriteString(value.AttributeId);
            writer.WriteScalar(value.BeforeBase);
            writer.WriteScalar(value.BaseValue);
            writer.WriteScalar(value.BeforeCurrent);
            writer.WriteScalar(value.CurrentValue);
            writer.WriteUInt64(value.ValueRevision);
            writer.WriteString(value.CauseEffectId);
            writer.WriteUInt64(value.CauseEffectInstanceId);
            WriteOptionalEffectContext(writer, value.CauseContext);
        }

        static GameplayAttributeFact ReadAttribute(CanonicalReader reader) => new GameplayAttributeFact(
            reader.ReadString(),
            reader.ReadScalar(),
            reader.ReadScalar(),
            reader.ReadScalar(),
            reader.ReadScalar(),
            reader.ReadUInt64(),
            reader.ReadString(),
            reader.ReadUInt64(),
            ReadOptionalEffectContext(reader));

        static void WriteCue(CanonicalWriter writer, GameplayCueFact value)
        {
            writer.WriteString(value.CueId);
            writer.WriteString(value.TriggerId);
            writer.WriteString(value.SourceId);
            writer.WriteUInt64(value.SourceInstanceId);
            WriteOptionalEffectContext(writer, value.EffectContext);
        }

        static GameplayCueFact ReadCue(CanonicalReader reader) => new GameplayCueFact(
            reader.ReadString(),
            reader.ReadString(),
            reader.ReadString(),
            reader.ReadUInt64(),
            ReadOptionalEffectContext(reader));

        static void WriteOptionalEffectContext(CanonicalWriter writer, SimulationGameplayEffectContext context)
        {
            writer.WriteBoolean(context.IsValid);
            if (context.IsValid)
                WriteEffectContext(writer, context);
        }

        static SimulationGameplayEffectContext ReadOptionalEffectContext(CanonicalReader reader) =>
            reader.ReadBoolean() ? ReadEffectContext(reader) : default;

        static void WriteEffectContext(CanonicalWriter writer, SimulationGameplayEffectContext context)
        {
            if (!context.IsValid)
                throw new InvalidDataException("Gameplay Effect context is invalid.");
            writer.WriteString(context.SourceActorId.Value);
            writer.WriteString(context.TargetActorId.Value);
            writer.WriteUInt64(context.SourceActionInstanceId);
            writer.WriteUInt64(context.PredictionKey);
            writer.WriteUInt64(context.GameplayResultId);
            writer.WriteUInt64(context.SourceTick);
            writer.WriteByte((byte)context.ApplicationMode);
        }

        static SimulationGameplayEffectContext ReadEffectContext(CanonicalReader reader) => new SimulationGameplayEffectContext(
            new ActorId(reader.ReadString()),
            new ActorId(reader.ReadString()),
            reader.ReadUInt64(),
            reader.ReadUInt64(),
            reader.ReadUInt64(),
            reader.ReadUInt64(),
            ReadEnum<SimulationGameplayEffectApplicationMode>(reader.ReadByte(), "effect application mode"));

        static CanonicalReader Reader(byte[] bytes, uint magic, int version, string label)
        {
            var reader = new CanonicalReader(bytes ?? throw new ArgumentNullException(nameof(bytes)));
            if (reader.ReadUInt32() != magic || reader.ReadInt32() != version)
                throw new InvalidDataException($"ServerAuthoritative {label} schema is invalid.");
            return reader;
        }

        static int ReadCount(CanonicalReader reader, string label)
        {
            int count = reader.ReadInt32();
            if (count < 0 || count > MaximumCount)
                throw new InvalidDataException($"ServerAuthoritative {label} count '{count}' is invalid.");
            return count;
        }

        static T ReadEnum<T>(byte value, string label) where T : struct, Enum
        {
            var result = (T)Enum.ToObject(typeof(T), value);
            if (!Enum.IsDefined(typeof(T), result))
                throw new InvalidDataException($"ServerAuthoritative {label} '{value}' is invalid.");
            return result;
        }
    }
}
