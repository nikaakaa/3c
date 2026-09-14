using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;

namespace ThirdPersonSimulation
{
    [Flags]
    public enum WorldCollisionSummary : uint
    {
        None = 0,
        Sides = 1,
        Above = 2,
        Below = 4
    }

    public enum WorldStatePersistenceMode : byte
    {
        Reconstruct = 1,
        Snapshot = 2
    }

    public readonly struct WorldBodyState
    {
        public WorldBodyState(
            ActorId actorId,
            Float32Vector3 position,
            Float32Yaw yaw,
            Float32Vector3 velocity,
            Float32Scalar verticalVelocity,
            bool grounded,
            WorldCollisionSummary collision)
        {
            if (!actorId.IsValid)
                throw new ArgumentException("World body ActorId is invalid.", nameof(actorId));
            ActorId = actorId;
            Position = position;
            Yaw = yaw;
            Velocity = velocity;
            VerticalVelocity = verticalVelocity;
            Grounded = grounded;
            Collision = collision;
        }
        public ActorId ActorId { get; }
        public Float32Vector3 Position { get; }
        public Float32Yaw Yaw { get; }
        public Float32Vector3 Velocity { get; }
        public Float32Scalar VerticalVelocity { get; }
        public bool Grounded { get; }
        public WorldCollisionSummary Collision { get; }
    }

    public sealed class WorldSimulationState
    {
        readonly ReadOnlyCollection<WorldBodyState> m_Bodies;
        readonly byte[] m_SolverStatePayload;

        public WorldSimulationState(
            SimulationNumericProfile numericProfile,
            SolverImplementationId solverId,
            string solverVersion,
            WorldRevision worldRevision,
            WorldStatePersistenceMode persistenceMode,
            IEnumerable<WorldBodyState> bodies,
            byte[] solverStatePayload)
        {
            if (!numericProfile.IsValid || string.IsNullOrEmpty(solverId.Value) || string.IsNullOrEmpty(worldRevision.Value))
                throw new ArgumentException("World state identity is incomplete.");
            NumericProfile = numericProfile;
            SolverId = solverId;
            SolverVersion = SimulationIdentity.Require(solverVersion, nameof(solverVersion));
            WorldRevision = worldRevision;
            PersistenceMode = persistenceMode;
            var copied = bodies == null ? new List<WorldBodyState>() : new List<WorldBodyState>(bodies);
            copied.Sort((left, right) => left.ActorId.CompareTo(right.ActorId));
            for (int i = 1; i < copied.Count; i++)
            {
                if (copied[i - 1].ActorId == copied[i].ActorId)
                    throw new ArgumentException($"World state contains duplicate ActorId '{copied[i].ActorId}'.", nameof(bodies));
            }
            m_Bodies = copied.AsReadOnly();
            m_SolverStatePayload = solverStatePayload == null ? Array.Empty<byte>() : (byte[])solverStatePayload.Clone();
        }

        public SimulationNumericProfile NumericProfile { get; }
        public SolverImplementationId SolverId { get; }
        public string SolverVersion { get; }
        public WorldRevision WorldRevision { get; }
        public WorldStatePersistenceMode PersistenceMode { get; }
        public IReadOnlyList<WorldBodyState> Bodies => m_Bodies;
        public ReadOnlyMemory<byte> SolverStatePayload => m_SolverStatePayload;
    }

    public static class WorldSimulationStateCodec
    {
        const uint Magic = 0x54535743;
        const int Version = 3;

        public static byte[] Write(WorldSimulationState state)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            using var writer = new CanonicalWriter();
            WriteCanonical(writer, state);
            return writer.ToArray();
        }

        public static StableHash ComputeHash(WorldSimulationState state)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            using var writer = new CanonicalWriter();
            WriteCanonical(writer, state);
            return writer.ComputeHash();
        }

        static void WriteCanonical(CanonicalWriter writer, WorldSimulationState state)
        {
            writer.WriteUInt32(Magic);
            writer.WriteInt32(Version);
            SimulationNumericProfileCodec.Write(writer, state.NumericProfile);
            writer.WriteString(state.SolverId.Value);
            writer.WriteString(state.SolverVersion);
            writer.WriteString(state.WorldRevision.Value);
            writer.WriteByte((byte)state.PersistenceMode);
            writer.WriteInt32(state.Bodies.Count);
            for (int i = 0; i < state.Bodies.Count; i++)
            {
                WorldBodyState body = state.Bodies[i];
                writer.WriteString(body.ActorId.Value);
                writer.WriteVector3(body.Position);
                writer.WriteYaw(body.Yaw);
                writer.WriteVector3(body.Velocity);
                writer.WriteScalar(body.VerticalVelocity);
                writer.WriteBoolean(body.Grounded);
                writer.WriteUInt32((uint)body.Collision);
            }
            writer.WriteBytes(state.SolverStatePayload.Span);
        }

        public static WorldSimulationState Read(
            byte[] bytes,
            SimulationNumericProfile expectedNumericProfile,
            SolverImplementationId expectedSolverId,
            string expectedSolverVersion,
            WorldRevision expectedWorldRevision)
        {
            var reader = new CanonicalReader(bytes);
            if (reader.ReadUInt32() != Magic || reader.ReadInt32() != Version)
                throw new InvalidDataException("World state header is invalid.");
            SimulationNumericProfile numericProfile = SimulationNumericProfileCodec.Read(reader);
            var solverId = new SolverImplementationId(reader.ReadString());
            string solverVersion = reader.ReadString();
            var worldRevision = new WorldRevision(reader.ReadString());
            WorldStatePersistenceMode persistenceMode = ReadPersistenceMode(reader.ReadByte());
            int count = reader.ReadInt32();
            if (count < 0 || count > 1000000)
                throw new InvalidDataException($"World body count '{count}' is invalid.");
            var bodies = new WorldBodyState[count];
            for (int i = 0; i < count; i++)
            {
                bodies[i] = new WorldBodyState(
                    new ActorId(reader.ReadString()),
                    reader.ReadVector3(),
                    reader.ReadYaw(),
                    reader.ReadVector3(),
                    reader.ReadScalar(),
                    reader.ReadBoolean(),
                    (WorldCollisionSummary)reader.ReadUInt32());
            }
            byte[] payload = reader.ReadBytes();
            reader.RequireComplete();
            if (numericProfile != expectedNumericProfile || !solverId.Equals(expectedSolverId) || !string.Equals(solverVersion, expectedSolverVersion, StringComparison.Ordinal) || !worldRevision.Equals(expectedWorldRevision))
                throw new InvalidDataException("World state Numeric Profile, Solver, or revision binding is stale or mismatched.");
            var result = new WorldSimulationState(numericProfile, solverId, solverVersion, worldRevision, persistenceMode, bodies, payload);
            RequireCanonical(bytes, Write(result), "World state");
            return result;
        }

        static WorldStatePersistenceMode ReadPersistenceMode(byte value)
        {
            if (!Enum.IsDefined(typeof(WorldStatePersistenceMode), value))
                throw new InvalidDataException($"World persistence mode '{value}' is invalid.");
            return (WorldStatePersistenceMode)value;
        }

        static void RequireCanonical(byte[] source, byte[] canonical, string label)
        {
            if (source.Length != canonical.Length)
                throw new InvalidDataException($"{label} is not canonical.");
            for (int i = 0; i < source.Length; i++)
            {
                if (source[i] != canonical[i])
                    throw new InvalidDataException($"{label} is not canonical.");
            }
        }
    }
}

