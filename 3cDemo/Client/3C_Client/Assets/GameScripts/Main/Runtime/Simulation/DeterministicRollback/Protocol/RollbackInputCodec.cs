using System;
using System.Collections.Generic;
using System.IO;
using ThirdPersonSimulation.Fixed;

namespace ThirdPersonSimulation.DeterministicRollback
{
    public static class RollbackInputCodec
    {
        const uint InputMagic = 0x49524244;
        const uint BundleMagic = 0x42524244;
        const int Version = 3;

        public static byte[] WriteInput(RollbackActorInputFrame frame)
        {
            if (frame == null)
                throw new ArgumentNullException(nameof(frame));
            using var writer = new CanonicalWriter();
            WriteInput(writer, frame);
            return writer.ToArray();
        }

        public static RollbackActorInputFrame ReadInput(byte[] bytes)
        {
            var reader = new CanonicalReader(bytes ?? throw new ArgumentNullException(nameof(bytes)));
            RollbackActorInputFrame frame = ReadInput(reader);
            reader.RequireComplete();
            return frame;
        }

        public static byte[] WriteBundle(RollbackCanonicalInputBundle bundle)
        {
            if (bundle == null)
                throw new ArgumentNullException(nameof(bundle));
            using var writer = new CanonicalWriter();
            writer.WriteUInt32(BundleMagic);
            writer.WriteInt32(Version);
            writer.WriteUInt64(bundle.Tick.Value);
            writer.WriteUInt64(bundle.BundleSequence);
            writer.WriteInt32(bundle.Actors.Count);
            for (int i = 0; i < bundle.Actors.Count; i++)
                WriteInput(writer, bundle.Actors[i]);
            return writer.ToArray();
        }

        public static RollbackCanonicalInputBundle ReadBundle(byte[] bytes)
        {
            var reader = new CanonicalReader(bytes ?? throw new ArgumentNullException(nameof(bytes)));
            if (reader.ReadUInt32() != BundleMagic || reader.ReadInt32() != Version)
                throw new InvalidDataException("Rollback canonical bundle header is invalid.");
            var tick = new SimulationTick(reader.ReadUInt64());
            ulong sequence = reader.ReadUInt64();
            int count = ReadCount(reader);
            var actors = new RollbackActorInputFrame[count];
            for (int i = 0; i < count; i++)
                actors[i] = ReadInput(reader);
            reader.RequireComplete();
            var bundle = new RollbackCanonicalInputBundle(tick, sequence, actors);
            if (!BytesEqual(bytes, WriteBundle(bundle)))
                throw new InvalidDataException("Rollback canonical bundle is not canonical.");
            return bundle;
        }

        public static StableHash ComputeInputHash(ActorId actorId, SimulationTick tick, CharacterSimulationInput input)
        {
            using var writer = new CanonicalWriter();
            WriteInputPayload(writer, actorId, tick, input);
            return writer.ComputeHash();
        }

        public static StableHash ComputeBundleHash(RollbackCanonicalInputBundle bundle)
        {
            return SimulationCanonicalPayloadHash.Compute(WriteBundle(bundle));
        }

        public static StableHash ComputeGameplayInputHash(
            ActorId actorId,
            SimulationTick tick,
            CharacterSimulationInput input)
        {
            if (!actorId.IsValid || !tick.IsValid || input == null)
                throw new ArgumentException("Rollback Gameplay input identity is incomplete.");
            using var writer = new CanonicalWriter();
            writer.WriteString(actorId.Value);
            writer.WriteUInt64(tick.Value);
            writer.WriteInt32(input.Values.Count);
            for (int i = 0; i < input.Values.Count; i++)
            {
                SimulationInputValue value = input.Values[i];
                writer.WriteString(value.InputId);
                writer.WriteByte((byte)value.Kind);
                switch (value.Kind)
                {
                    case SimulationInputValueKind.Boolean: writer.WriteBoolean(value.Boolean); break;
                    case SimulationInputValueKind.Scalar: writer.WriteScalar(value.Scalar); break;
                    case SimulationInputValueKind.Vector2: writer.WriteVector2(value.Vector2); break;
                    case SimulationInputValueKind.Vector3: writer.WriteVector3(value.Vector3); break;
                    case SimulationInputValueKind.Yaw: writer.WriteYaw(value.Yaw); break;
                    case SimulationInputValueKind.ActionTargetSnapshot: WriteTargetSnapshot(writer, value.ActionTargetSnapshot); break;
                    default: throw new InvalidDataException($"Rollback input value kind '{value.Kind}' is unsupported.");
                }
            }
            writer.WriteInt32(input.Requests.Count);
            for (int i = 0; i < input.Requests.Count; i++)
            {
                SimulationInputRequest request = input.Requests[i];
                writer.WriteString(request.RequestId);
                writer.WriteUInt64(request.Sequence);
                writer.WriteUInt64(request.SourceTick);
                writer.WriteUInt64(request.ExpireSimulationTick);
                writer.WriteInt32(request.Priority);
            }
            return writer.ComputeHash();
        }

        public static StableHash ComputeGameplayBundleHash(RollbackCanonicalInputBundle bundle)
        {
            if (bundle == null)
                throw new ArgumentNullException(nameof(bundle));
            var values = new string[bundle.Actors.Count + 2];
            values[0] = "deterministic-rollback-gameplay-input-bundle/2";
            values[1] = bundle.Tick.Value.ToString();
            for (int i = 0; i < bundle.Actors.Count; i++)
                values[i + 2] = $"{bundle.Actors[i].ActorId.Value}:{bundle.Actors[i].GameplayHash.Value}";
            return StableHash.Compute(values);
        }

        static void WriteInput(CanonicalWriter writer, RollbackActorInputFrame frame)
        {
            writer.WriteUInt32(InputMagic);
            writer.WriteInt32(Version);
            writer.WriteByte((byte)frame.Provenance);
            WriteInputPayload(writer, frame.ActorId, frame.Tick, frame.Input);
        }

        static void WriteInputPayload(
            CanonicalWriter writer,
            ActorId actorId,
            SimulationTick tick,
            CharacterSimulationInput input)
        {
            writer.WriteString(actorId.Value);
            writer.WriteUInt64(tick.Value);
            writer.WriteByte((byte)input.TickSource.Kind);
            writer.WriteString(input.TickSource.ClockId);
            writer.WriteUInt64(input.TickSource.SourceTick);
            writer.WriteString(input.InputSourceIdentity);
            writer.WriteUInt64(input.Sequence);
            writer.WriteInt32(input.Values.Count);
            for (int i = 0; i < input.Values.Count; i++)
            {
                SimulationInputValue value = input.Values[i];
                writer.WriteString(value.InputId);
                writer.WriteByte((byte)value.Kind);
                switch (value.Kind)
                {
                    case SimulationInputValueKind.Boolean: writer.WriteBoolean(value.Boolean); break;
                    case SimulationInputValueKind.Scalar: writer.WriteScalar(value.Scalar); break;
                    case SimulationInputValueKind.Vector2: writer.WriteVector2(value.Vector2); break;
                    case SimulationInputValueKind.Vector3: writer.WriteVector3(value.Vector3); break;
                    case SimulationInputValueKind.Yaw: writer.WriteYaw(value.Yaw); break;
                    case SimulationInputValueKind.ActionTargetSnapshot: WriteTargetSnapshot(writer, value.ActionTargetSnapshot); break;
                    default: throw new InvalidDataException($"Rollback input value kind '{value.Kind}' is unsupported.");
                }
            }
            writer.WriteInt32(input.Requests.Count);
            for (int i = 0; i < input.Requests.Count; i++)
            {
                SimulationInputRequest request = input.Requests[i];
                writer.WriteString(request.RequestId);
                writer.WriteUInt64(request.Sequence);
                writer.WriteUInt64(request.SourceTick);
                writer.WriteUInt64(request.ExpireSimulationTick);
                writer.WriteInt32(request.Priority);
            }
        }

        static RollbackActorInputFrame ReadInput(CanonicalReader reader)
        {
            if (reader.ReadUInt32() != InputMagic || reader.ReadInt32() != Version)
                throw new InvalidDataException("Rollback Actor input header is invalid.");
            RollbackInputProvenance provenance = ReadProvenance(reader.ReadByte());
            var actorId = new ActorId(reader.ReadString());
            var tick = new SimulationTick(reader.ReadUInt64());
            SimulationTickSourceKind sourceKind = ReadTickSourceKind(reader.ReadByte());
            string clockId = reader.ReadString();
            ulong sourceTick = reader.ReadUInt64();
            string sourceIdentity = reader.ReadString();
            ulong sequence = reader.ReadUInt64();
            int valueCount = ReadCount(reader);
            var values = new SimulationInputValue[valueCount];
            for (int i = 0; i < valueCount; i++)
            {
                string inputId = reader.ReadString();
                SimulationInputValueKind kind = ReadValueKind(reader.ReadByte());
                values[i] = kind switch
                {
                    SimulationInputValueKind.Boolean => SimulationInputValue.FromBoolean(inputId, reader.ReadBoolean()),
                    SimulationInputValueKind.Scalar => SimulationInputValue.FromScalar(inputId, reader.ReadScalar()),
                    SimulationInputValueKind.Vector2 => SimulationInputValue.FromVector2(inputId, reader.ReadVector2()),
                    SimulationInputValueKind.Vector3 => SimulationInputValue.FromVector3(inputId, reader.ReadVector3()),
                    SimulationInputValueKind.Yaw => SimulationInputValue.FromYaw(inputId, reader.ReadYaw()),
                    SimulationInputValueKind.ActionTargetSnapshot => SimulationInputValue.FromActionTargetSnapshot(inputId, ReadTargetSnapshot(reader)),
                    _ => throw new InvalidDataException($"Rollback input value kind '{kind}' is unsupported.")
                };
            }
            int requestCount = ReadCount(reader);
            var requests = new SimulationInputRequest[requestCount];
            for (int i = 0; i < requestCount; i++)
            {
                requests[i] = new SimulationInputRequest(
                    reader.ReadString(),
                    reader.ReadUInt64(),
                    reader.ReadUInt64(),
                    reader.ReadUInt64(),
                    reader.ReadInt32());
            }
            var input = new CharacterSimulationInput(
                FixedSimulationNumericProfile.Value,
                new SimulationTickSourceIdentity(sourceKind, clockId, sourceTick),
                sourceIdentity,
                sequence,
                values,
                requests);
            return new RollbackActorInputFrame(actorId, tick, sequence, input, provenance);
        }

        static int ReadCount(CanonicalReader reader)
        {
            int value = reader.ReadInt32();
            if (value < 0 || value > 1000000)
                throw new InvalidDataException($"Rollback protocol count '{value}' is invalid.");
            return value;
        }

        static RollbackInputProvenance ReadProvenance(byte value)
        {
            if (!Enum.IsDefined(typeof(RollbackInputProvenance), value))
                throw new InvalidDataException($"Rollback input provenance '{value}' is invalid.");
            return (RollbackInputProvenance)value;
        }

        static SimulationInputValueKind ReadValueKind(byte value)
        {
            if (!Enum.IsDefined(typeof(SimulationInputValueKind), value))
                throw new InvalidDataException($"Rollback input value kind '{value}' is invalid.");
            return (SimulationInputValueKind)value;
        }

        static void WriteTargetSnapshot(CanonicalWriter writer, SimulationActionTargetSnapshot value)
        {
            writer.WriteString(value.TargetId);
            writer.WriteVector3(value.Position);
            writer.WriteYaw(value.Yaw);
        }

        static SimulationActionTargetSnapshot ReadTargetSnapshot(CanonicalReader reader)
        {
            return new SimulationActionTargetSnapshot(reader.ReadString(), reader.ReadVector3(), reader.ReadYaw());
        }

        static SimulationTickSourceKind ReadTickSourceKind(byte value)
        {
            if (!Enum.IsDefined(typeof(SimulationTickSourceKind), value))
                throw new InvalidDataException($"Rollback Tick source kind '{value}' is invalid.");
            return (SimulationTickSourceKind)value;
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
