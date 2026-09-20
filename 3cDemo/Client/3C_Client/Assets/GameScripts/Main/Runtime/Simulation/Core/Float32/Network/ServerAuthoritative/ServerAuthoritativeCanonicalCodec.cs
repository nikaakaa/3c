using System;
using System.Collections.Generic;
using System.IO;
using ThirdPersonSimulation;

namespace ThirdPersonSimulation.ServerAuthoritative
{
    public static class ServerAuthoritativeCanonicalCodec
    {
        const uint InputMagic = 0x49415343;
        const uint BaselineMagic = 0x42415343;
        public const int InputSchemaVersion = 2;
        public const int BaselineSchemaVersion = 4;
        const int MaximumCollectionCount = 4096;

        public static byte[] WriteInput(SimulationInput input)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));
            using var writer = new CanonicalWriter();
            WriteInput(writer, input);
            return writer.ToArray();
        }

        public static void WriteLengthPrefixedInput(CanonicalWriter writer, SimulationInput input)
        {
            if (writer == null)
                throw new ArgumentNullException(nameof(writer));
            if (input == null)
                throw new ArgumentNullException(nameof(input));
            long prefixPosition = writer.BeginLengthPrefixedBlock();
            WriteInput(writer, input);
            writer.EndLengthPrefixedBlock(prefixPosition);
        }

        static void WriteInput(CanonicalWriter writer, SimulationInput input)
        {
            writer.WriteUInt32(InputMagic);
            writer.WriteInt32(InputSchemaVersion);
            SimulationNumericProfileCodec.Write(writer, input.NumericProfile);
            writer.WriteByte((byte)input.TickSource.Kind);
            writer.WriteString(input.TickSource.ClockId);
            writer.WriteUInt64(input.TickSource.SourceTick);
            writer.WriteString(input.InputSourceIdentity);
            writer.WriteUInt64(input.Sequence);
            writer.WriteInt32(input.Values.Count);
            for (int i = 0; i < input.Values.Count; i++)
                WriteInputValue(writer, input.Values[i]);
            writer.WriteInt32(input.Requests.Count);
            for (int i = 0; i < input.Requests.Count; i++)
                WriteInputRequest(writer, input.Requests[i]);
        }

        public static SimulationInput ReadInput(ArraySegment<byte> bytes)
        {
            var reader = Reader(bytes, InputMagic, InputSchemaVersion, "ServerAuthoritative canonical input");
            SimulationNumericProfile profile = SimulationNumericProfileCodec.Read(reader);
            var sourceKind = ReadSourceKind(reader.ReadByte());
            string clockId = reader.ReadString();
            ulong sourceTick = reader.ReadUInt64();
            string inputSource = reader.ReadString();
            ulong sequence = reader.ReadUInt64();
            int valueCount = ReadCount(reader, "input value");
            var values = new SimulationInputValue[valueCount];
            for (int i = 0; i < valueCount; i++)
                values[i] = ReadInputValue(reader);
            int requestCount = ReadCount(reader, "input request");
            var requests = new SimulationInputRequest[requestCount];
            for (int i = 0; i < requestCount; i++)
                requests[i] = ReadInputRequest(reader);
            reader.RequireComplete();
            var result = new SimulationInput(
                profile,
                new SimulationTickSourceIdentity(sourceKind, clockId, sourceTick),
                inputSource,
                sequence,
                values,
                requests);
            using var writer = new CanonicalWriter();
            WriteInput(writer, result);
            RequireCanonical(bytes.AsSpan(), writer, "ServerAuthoritative canonical input");
            return result;
        }

        public static byte[] WriteBaseline(AuthoritativeActorBaseline baseline)
        {
            if (baseline == null)
                throw new ArgumentNullException(nameof(baseline));
            using var writer = new CanonicalWriter();
            WriteBaseline(writer, baseline);
            return writer.ToArray();
        }

        internal static void WriteLengthPrefixedBaseline(CanonicalWriter writer, AuthoritativeActorBaseline baseline)
        {
            if (baseline == null)
                throw new ArgumentNullException(nameof(baseline));
            long prefixPosition = writer.BeginLengthPrefixedBlock();
            WriteBaseline(writer, baseline);
            writer.EndLengthPrefixedBlock(prefixPosition);
        }

        static void WriteBaseline(CanonicalWriter writer, AuthoritativeActorBaseline baseline)
        {
            writer.WriteUInt32(BaselineMagic);
            writer.WriteInt32(BaselineSchemaVersion);
            writer.WriteString(baseline.ActorId.Value);
            writer.WriteUInt64(baseline.AuthorityTick.Value);
            SimulationNumericProfileCodec.Write(writer, baseline.NumericProfile);
            writer.WriteInt32(baseline.TargetAbiVersion.Value);
            writer.WriteString(baseline.StateCodecIdentity);
            writer.WriteString(baseline.GameplayContentHash.ToString());
            writer.WriteString(baseline.OperationSetVersion.Value);
            writer.WriteBytes(baseline.CopyCharacterStateBytes());
            writer.WriteString(baseline.StateHash.ToString());
            writer.WriteString(baseline.WorldRevision.Value);
            writer.WriteString(baseline.SolverId.Value);
            writer.WriteString(baseline.SolverVersion);
            writer.WriteUInt64((ulong)baseline.SolverCapabilities);
            WriteBody(writer, baseline.Body);
            writer.WriteString(baseline.BodyHash.ToString());
            writer.WriteUInt64(baseline.ConfirmedInputSequence);
            writer.WriteUInt64(baseline.ConfirmedEventHorizon.Sequence);
            writer.WriteString(baseline.ConfirmedEventHorizon.EventId.IsValid
                ? baseline.ConfirmedEventHorizon.EventId.ToString()
                : string.Empty);
        }

        public static AuthoritativeActorBaseline ReadBaseline(byte[] bytes)
        {
            var reader = Reader(new ArraySegment<byte>(bytes ?? throw new ArgumentNullException(nameof(bytes))), BaselineMagic, BaselineSchemaVersion, "ServerAuthoritative baseline");
            var actorId = new ActorId(reader.ReadString());
            var tick = new SimulationTick(reader.ReadUInt64());
            SimulationNumericProfile numericProfile = SimulationNumericProfileCodec.Read(reader);
            var targetAbiVersion = new TargetAbiVersion(reader.ReadInt32());
            string stateCodecIdentity = reader.ReadString();
            var gameplayContentHash = new GameplayContentHash(new StableHash(reader.ReadString()));
            var operationSet = new OperationSetVersion(reader.ReadString());
            byte[] stateBytes = reader.ReadBytes();
            var stateHash = new CharacterStateHash(new StableHash(reader.ReadString()));
            var worldRevision = new WorldRevision(reader.ReadString());
            var solverId = new SolverImplementationId(reader.ReadString());
            string solverVersion = reader.ReadString();
            var solverCapabilities = (WorldCapability)reader.ReadUInt64();
            WorldBodyState body = ReadBody(reader);
            var encodedBodyHash = new StableHash(reader.ReadString());
            ulong confirmedInputSequence = reader.ReadUInt64();
            ulong eventSequence = reader.ReadUInt64();
            string eventHash = reader.ReadString();
            reader.RequireComplete();
            if (body.ActorId != actorId)
                throw new InvalidDataException("Authoritative baseline body ActorId does not match the baseline.");
            StableHash actualBodyHash = ComputeBodyHash(body);
            if (!actualBodyHash.Equals(encodedBodyHash))
                throw new InvalidDataException("Authoritative baseline body hash does not match its canonical body.");
            EventId eventId = string.IsNullOrEmpty(eventHash) ? default : new EventId(new StableHash(eventHash));
            var result = new AuthoritativeActorBaseline(
                actorId,
                tick,
                numericProfile,
                targetAbiVersion,
                stateCodecIdentity,
                gameplayContentHash,
                operationSet,
                stateBytes,
                stateHash,
                worldRevision,
                solverId,
                solverVersion,
                solverCapabilities,
                body,
                confirmedInputSequence,
                new ServerAuthoritativeEventHorizon(eventSequence, eventId));
            using var writer = new CanonicalWriter();
            WriteBaseline(writer, result);
            RequireCanonical(bytes, writer, "ServerAuthoritative baseline");
            return result;
        }

        public static StableHash ComputeBodyHash(WorldBodyState body)
        {
            using var writer = new CanonicalWriter();
            writer.WriteString("server-authoritative-body/2");
            WriteBody(writer, body);
            return writer.ComputeHash();
        }

        static void WriteInputValue(CanonicalWriter writer, SimulationInputValue value)
        {
            writer.WriteString(value.InputId);
            writer.WriteByte((byte)value.Kind);
            switch (value.Kind)
            {
                case SimulationInputValueKind.Boolean:
                    writer.WriteBoolean(value.Boolean);
                    break;
                case SimulationInputValueKind.Scalar:
                    writer.WriteScalar(value.Scalar);
                    break;
                case SimulationInputValueKind.Vector2:
                    writer.WriteVector2(value.Vector2);
                    break;
                case SimulationInputValueKind.Vector3:
                    writer.WriteVector3(value.Vector3);
                    break;
                case SimulationInputValueKind.Yaw:
                    writer.WriteYaw(value.Yaw);
                    break;
                case SimulationInputValueKind.ActionTargetSnapshot:
                    writer.WriteString(value.ActionTargetSnapshot.TargetId);
                    writer.WriteVector3(value.ActionTargetSnapshot.Position);
                    writer.WriteYaw(value.ActionTargetSnapshot.Yaw);
                    break;
                default:
                    throw new InvalidDataException($"Unsupported input value kind '{value.Kind}'.");
            }
        }

        static SimulationInputValue ReadInputValue(CanonicalReader reader)
        {
            string inputId = reader.ReadString();
            var kind = (SimulationInputValueKind)reader.ReadByte();
            return kind switch
            {
                SimulationInputValueKind.Boolean => SimulationInputValue.FromBoolean(inputId, reader.ReadBoolean()),
                SimulationInputValueKind.Scalar => SimulationInputValue.FromScalar(inputId, reader.ReadScalar()),
                SimulationInputValueKind.Vector2 => SimulationInputValue.FromVector2(inputId, reader.ReadVector2()),
                SimulationInputValueKind.Vector3 => SimulationInputValue.FromVector3(inputId, reader.ReadVector3()),
                SimulationInputValueKind.Yaw => SimulationInputValue.FromYaw(inputId, reader.ReadYaw()),
                SimulationInputValueKind.ActionTargetSnapshot => SimulationInputValue.FromActionTargetSnapshot(
                    inputId,
                    new SimulationActionTargetSnapshot(reader.ReadString(), reader.ReadVector3(), reader.ReadYaw())),
                _ => throw new InvalidDataException($"Canonical input value kind '{(byte)kind}' is invalid.")
            };
        }

        static void WriteInputRequest(CanonicalWriter writer, SimulationInputRequest request)
        {
            writer.WriteString(request.RequestId);
            writer.WriteUInt64(request.Sequence);
            writer.WriteUInt64(request.SourceTick);
            writer.WriteUInt64(request.ExpireSimulationTick);
            writer.WriteInt32(request.Priority);
        }

        static SimulationInputRequest ReadInputRequest(CanonicalReader reader)
        {
            return new SimulationInputRequest(
                reader.ReadString(),
                reader.ReadUInt64(),
                reader.ReadUInt64(),
                reader.ReadUInt64(),
                reader.ReadInt32());
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

        static WorldBodyState ReadBody(CanonicalReader reader)
        {
            return new WorldBodyState(
                new ActorId(reader.ReadString()),
                reader.ReadVector3(),
                reader.ReadYaw(),
                reader.ReadVector3(),
                reader.ReadScalar(),
                reader.ReadBoolean(),
                (WorldCollisionSummary)reader.ReadUInt32());
        }

        static CanonicalReader Reader(ArraySegment<byte> bytes, uint magic, int expectedVersion, string label)
        {
            if (bytes.Array == null)
                throw new ArgumentNullException(nameof(bytes));
            var reader = new CanonicalReader(bytes);
            if (reader.ReadUInt32() != magic)
                throw new InvalidDataException($"{label} magic is invalid.");
            int version = reader.ReadInt32();
            if (version != expectedVersion)
                throw new InvalidDataException($"{label} schema version '{version}' is unsupported.");
            return reader;
        }

        static int ReadCount(CanonicalReader reader, string label)
        {
            int count = reader.ReadInt32();
            if (count < 0 || count > MaximumCollectionCount)
                throw new InvalidDataException($"Canonical {label} count '{count}' is invalid.");
            return count;
        }

        static SimulationTickSourceKind ReadSourceKind(byte value)
        {
            var typed = (SimulationTickSourceKind)value;
            if (typed != SimulationTickSourceKind.LocalLogic &&
                typed != SimulationTickSourceKind.Authoritative &&
                typed != SimulationTickSourceKind.Replay)
                throw new InvalidDataException($"Canonical input source kind '{value}' is invalid.");
            return typed;
        }

        static void RequireCanonical(ReadOnlySpan<byte> source, CanonicalWriter canonical, string label)
        {
            if (!canonical.ContentEquals(source))
                throw new InvalidDataException($"{label} is not canonical.");
        }
    }
}
