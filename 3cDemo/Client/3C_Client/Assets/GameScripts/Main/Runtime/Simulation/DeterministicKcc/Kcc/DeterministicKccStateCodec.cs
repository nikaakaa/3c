using System;
using System.Collections.Generic;
using System.IO;
using ThirdPersonSimulation.Fixed;

namespace ThirdPersonSimulation.DeterministicKcc
{
    public enum DeterministicKccLedgeState : byte
    {
        None = 0,
        StableSide = 1,
        EmptySide = 2
    }

    public readonly struct DeterministicKccBodyState
    {
        public DeterministicKccBodyState(
            ActorId actorId,
            bool foundAnyGround,
            bool isStableOnGround,
            int groundSurfaceId,
            int groundPrimitiveId,
            DeterministicCollisionFeatureId groundFeatureId,
            FixedVector3 groundNormal,
            FixedVector3 innerGroundNormal,
            FixedVector3 outerGroundNormal,
            bool snappingPrevented,
            DeterministicKccLedgeState ledgeState,
            bool lastMovementIterationFoundAnyGround)
        {
            bool identityValid = groundSurfaceId >= 0 && groundPrimitiveId >= 0 && groundFeatureId.IsValid;
            bool identityEmpty = groundSurfaceId == -1 && groundPrimitiveId == -1 && !groundFeatureId.IsValid;
            if (!actorId.IsValid || groundSurfaceId < -1 || groundPrimitiveId < -1 ||
                isStableOnGround && (!foundAnyGround || snappingPrevented) ||
                foundAnyGround && (!identityValid || groundNormal.SqrMagnitude == FixedScalar.Zero) ||
                !foundAnyGround && (!identityEmpty || groundNormal.SqrMagnitude != FixedScalar.Zero ||
                                    innerGroundNormal.SqrMagnitude != FixedScalar.Zero ||
                                    outerGroundNormal.SqrMagnitude != FixedScalar.Zero ||
                                    snappingPrevented || ledgeState != DeterministicKccLedgeState.None) ||
                !Enum.IsDefined(typeof(DeterministicKccLedgeState), ledgeState))
            {
                throw new ArgumentException("Deterministic KCC body state is invalid.");
            }
            ActorId = actorId;
            FoundAnyGround = foundAnyGround;
            IsStableOnGround = isStableOnGround;
            GroundSurfaceId = groundSurfaceId;
            GroundPrimitiveId = groundPrimitiveId;
            GroundFeatureId = groundFeatureId;
            GroundNormal = groundNormal;
            InnerGroundNormal = innerGroundNormal;
            OuterGroundNormal = outerGroundNormal;
            SnappingPrevented = snappingPrevented;
            LedgeState = ledgeState;
            LastMovementIterationFoundAnyGround = lastMovementIterationFoundAnyGround;
        }

        public ActorId ActorId { get; }
        public bool FoundAnyGround { get; }
        public bool IsStableOnGround { get; }
        public int GroundSurfaceId { get; }
        public int GroundPrimitiveId { get; }
        public DeterministicCollisionFeatureId GroundFeatureId { get; }
        public FixedVector3 GroundNormal { get; }
        public FixedVector3 InnerGroundNormal { get; }
        public FixedVector3 OuterGroundNormal { get; }
        public bool SnappingPrevented { get; }
        public DeterministicKccLedgeState LedgeState { get; }
        public bool LastMovementIterationFoundAnyGround { get; }
    }

    public static class DeterministicKccStateCodec
    {
        const uint Magic = 0x5343434B;
        const int Version = 3;

        public static byte[] Write(
            StableHash collisionWorldHash,
            StableHash configurationHash,
            IReadOnlyList<DeterministicKccBodyState> states)
        {
            if (!collisionWorldHash.IsValid || !configurationHash.IsValid || states == null)
                throw new ArgumentException("Deterministic KCC state identity is incomplete.");
            using var writer = new CanonicalWriter();
            writer.WriteUInt32(Magic);
            writer.WriteInt32(Version);
            writer.WriteString(collisionWorldHash.Value);
            writer.WriteString(configurationHash.Value);
            writer.WriteInt32(states.Count);
            for (int i = 0; i < states.Count; i++)
            {
                DeterministicKccBodyState state = states[i];
                if (i > 0 && states[i - 1].ActorId.CompareTo(state.ActorId) >= 0)
                    throw new InvalidOperationException("Deterministic KCC state Actor order is not canonical.");
                writer.WriteString(state.ActorId.Value);
                writer.WriteBoolean(state.FoundAnyGround);
                writer.WriteBoolean(state.IsStableOnGround);
                writer.WriteInt32(state.GroundSurfaceId);
                writer.WriteInt32(state.GroundPrimitiveId);
                writer.WriteByte((byte)state.GroundFeatureId.Kind);
                writer.WriteInt32(state.GroundFeatureId.Index);
                writer.WriteVector3(state.GroundNormal);
                writer.WriteVector3(state.InnerGroundNormal);
                writer.WriteVector3(state.OuterGroundNormal);
                writer.WriteBoolean(state.SnappingPrevented);
                writer.WriteByte((byte)state.LedgeState);
                writer.WriteBoolean(state.LastMovementIterationFoundAnyGround);
            }
            return writer.ToArray();
        }

        public static DeterministicKccBodyState[] Read(
            byte[] bytes,
            StableHash expectedCollisionWorldHash,
            StableHash expectedConfigurationHash)
        {
            var reader = new CanonicalReader(bytes ?? throw new ArgumentNullException(nameof(bytes)));
            if (reader.ReadUInt32() != Magic || reader.ReadInt32() != Version)
                throw new InvalidDataException("Deterministic KCC state header is invalid.");
            var collisionWorldHash = new StableHash(reader.ReadString());
            var configurationHash = new StableHash(reader.ReadString());
            if (!collisionWorldHash.Equals(expectedCollisionWorldHash) || !configurationHash.Equals(expectedConfigurationHash))
                throw new InvalidDataException("Deterministic KCC state world or configuration identity is stale.");
            int count = reader.ReadInt32();
            if (count < 0 || count > 100000)
                throw new InvalidDataException($"Deterministic KCC body count '{count}' is invalid.");
            var states = new DeterministicKccBodyState[count];
            for (int i = 0; i < count; i++)
            {
                states[i] = new DeterministicKccBodyState(
                    new ActorId(reader.ReadString()),
                    reader.ReadBoolean(),
                    reader.ReadBoolean(),
                    reader.ReadInt32(),
                    reader.ReadInt32(),
                    ReadFeature(reader.ReadByte(), reader.ReadInt32()),
                    reader.ReadVector3(),
                    reader.ReadVector3(),
                    reader.ReadVector3(),
                    reader.ReadBoolean(),
                    ReadLedgeState(reader.ReadByte()),
                    reader.ReadBoolean());
                if (i > 0 && states[i - 1].ActorId.CompareTo(states[i].ActorId) >= 0)
                    throw new InvalidDataException("Deterministic KCC state Actor order is not canonical.");
            }
            reader.RequireComplete();
            return states;
        }

        static DeterministicCollisionFeatureId ReadFeature(byte kind, int index)
        {
            if (index == 0 && kind == 0)
                return DeterministicCollisionFeatureId.Invalid;
            if (!Enum.IsDefined(typeof(DeterministicCollisionFeatureKind), kind) || index < 0)
                throw new InvalidDataException($"Deterministic KCC ground feature '{kind}:{index}' is invalid.");
            return new DeterministicCollisionFeatureId((DeterministicCollisionFeatureKind)kind, index);
        }

        static DeterministicKccLedgeState ReadLedgeState(byte value)
        {
            if (!Enum.IsDefined(typeof(DeterministicKccLedgeState), value))
                throw new InvalidDataException($"Deterministic KCC ledge state '{value}' is invalid.");
            return (DeterministicKccLedgeState)value;
        }
    }
}
