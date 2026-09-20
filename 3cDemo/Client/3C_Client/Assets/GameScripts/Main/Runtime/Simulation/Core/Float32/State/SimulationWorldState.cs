using System;
using System.Collections.Generic;
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
        readonly IReadOnlyList<WorldBodyState> m_Bodies;
        readonly byte[] m_SolverStatePayload;

        public WorldSimulationState(
            SimulationNumericProfile numericProfile,
            SolverImplementationId solverId,
            string solverVersion,
            WorldRevision worldRevision,
            WorldStatePersistenceMode persistenceMode,
            IReadOnlyList<WorldBodyState> bodies,
            byte[] solverStatePayload)
            : this(
                numericProfile,
                solverId,
                solverVersion,
                worldRevision,
                persistenceMode,
                CopyBodies(bodies),
                CopySolverStatePayload(solverStatePayload))
        {
        }

        WorldSimulationState(
            SimulationNumericProfile numericProfile,
            SolverImplementationId solverId,
            string solverVersion,
            WorldRevision worldRevision,
            WorldStatePersistenceMode persistenceMode,
            WorldBodyState[] bodies,
            byte[] solverStatePayload)
        {
            if (!numericProfile.IsValid || string.IsNullOrEmpty(solverId.Value) || string.IsNullOrEmpty(worldRevision.Value))
                throw new ArgumentException("World state identity is incomplete.");
            NumericProfile = numericProfile;
            SolverId = solverId;
            SolverVersion = SimulationIdentity.Require(solverVersion, nameof(solverVersion));
            WorldRevision = worldRevision;
            PersistenceMode = persistenceMode;
            WorldBodyState[] ownedBodies = bodies ?? Array.Empty<WorldBodyState>();
            Array.Sort(ownedBodies, (left, right) => left.ActorId.CompareTo(right.ActorId));
            for (int i = 1; i < ownedBodies.Length; i++)
            {
                if (ownedBodies[i - 1].ActorId == ownedBodies[i].ActorId)
                    throw new ArgumentException($"World state contains duplicate ActorId '{ownedBodies[i].ActorId}'.", nameof(bodies));
            }
            m_Bodies = ownedBodies;
            m_SolverStatePayload = solverStatePayload ?? Array.Empty<byte>();
        }

        public static WorldSimulationState FromOwnedState(
            SimulationNumericProfile numericProfile,
            SolverImplementationId solverId,
            string solverVersion,
            WorldRevision worldRevision,
            WorldStatePersistenceMode persistenceMode,
            WorldBodyState[] bodies,
            byte[] solverStatePayload) =>
            new WorldSimulationState(
                numericProfile,
                solverId,
                solverVersion,
                worldRevision,
                persistenceMode,
                bodies,
                solverStatePayload);

        public SimulationNumericProfile NumericProfile { get; }
        public SolverImplementationId SolverId { get; }
        public string SolverVersion { get; }
        public WorldRevision WorldRevision { get; }
        public WorldStatePersistenceMode PersistenceMode { get; }
        public IReadOnlyList<WorldBodyState> Bodies => m_Bodies;
        public ReadOnlyMemory<byte> SolverStatePayload => m_SolverStatePayload;

        public WorldSimulationState Clone() =>
            new WorldSimulationState(
                NumericProfile,
                SolverId,
                SolverVersion,
                WorldRevision,
                PersistenceMode,
                CopyBodies(m_Bodies),
                CopySolverStatePayload(m_SolverStatePayload));

        static WorldBodyState[] CopyBodies(IReadOnlyList<WorldBodyState> bodies)
        {
            if (bodies == null || bodies.Count == 0)
                return Array.Empty<WorldBodyState>();
            var copied = new WorldBodyState[bodies.Count];
            for (int i = 0; i < copied.Length; i++)
                copied[i] = bodies[i];
            return copied;
        }

        static byte[] CopySolverStatePayload(byte[] solverStatePayload) =>
            solverStatePayload == null || solverStatePayload.Length == 0
                ? Array.Empty<byte>()
                : (byte[])solverStatePayload.Clone();
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
            ArraySegment<byte> payload = reader.ReadBytesSegment();
            reader.RequireComplete();
            if (numericProfile != expectedNumericProfile || !solverId.Equals(expectedSolverId) || !string.Equals(solverVersion, expectedSolverVersion, StringComparison.Ordinal) || !worldRevision.Equals(expectedWorldRevision))
                throw new InvalidDataException("World state Numeric Profile, Solver, or revision binding is stale or mismatched.");
            var result = WorldSimulationState.FromOwnedState(
                numericProfile,
                solverId,
                solverVersion,
                worldRevision,
                persistenceMode,
                bodies,
                payload.Count == 0 ? Array.Empty<byte>() : payload.AsSpan().ToArray());
            using var writer = new CanonicalWriter();
            WriteCanonical(writer, result);
            if (!writer.ContentEquals(bytes))
                throw new InvalidDataException("World state is not canonical.");
            return result;
        }

        static WorldStatePersistenceMode ReadPersistenceMode(byte value)
        {
            if ((WorldStatePersistenceMode)value is not (WorldStatePersistenceMode.Reconstruct or WorldStatePersistenceMode.Snapshot))
                throw new InvalidDataException($"World persistence mode '{value}' is invalid.");
            return (WorldStatePersistenceMode)value;
        }

    }
}
