using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;

namespace ThirdPersonSimulation
{
    public sealed class SimulationActorState
    {
        public SimulationActorState(ActorId actorId, CharacterSimulationState state)
        {
            if (!actorId.IsValid)
                throw new ArgumentException("ActorId is invalid.", nameof(actorId));
            ActorId = actorId;
            State = state ?? throw new ArgumentNullException(nameof(state));
        }
        public ActorId ActorId { get; }
        public CharacterSimulationState State { get; }
    }

    public sealed class SimulationActorSnapshot
    {
        readonly byte[] m_StateBytes;

        public SimulationActorSnapshot(
            ActorId actorId,
            ProgramId programId,
            ProgramHash programHash,
            LayoutHash layoutHash,
            CharacterStateHash stateHash,
            byte[] stateBytes)
        {
            if (!actorId.IsValid || !programId.IsValid || !programHash.IsValid || !layoutHash.IsValid || !stateHash.IsValid)
                throw new ArgumentException("Actor snapshot identity is incomplete.");
            ActorId = actorId;
            ProgramId = programId;
            ProgramHash = programHash;
            LayoutHash = layoutHash;
            StateHash = stateHash;
            m_StateBytes = stateBytes == null ? throw new ArgumentNullException(nameof(stateBytes)) : (byte[])stateBytes.Clone();
        }

        public ActorId ActorId { get; }
        public ProgramId ProgramId { get; }
        public ProgramHash ProgramHash { get; }
        public LayoutHash LayoutHash { get; }
        public CharacterStateHash StateHash { get; }
        public ReadOnlyMemory<byte> StateBytes => m_StateBytes;
        internal byte[] StateBytesBuffer => m_StateBytes;

        public CharacterSimulationState Decode(CharacterSimulationProgram program)
        {
            if (program == null || program.Manifest.ProgramId != ProgramId || !program.ProgramHash.Equals(ProgramHash) || !program.LayoutHash.Equals(LayoutHash))
                throw new InvalidDataException($"Actor '{ActorId}' snapshot Program binding is stale or mismatched.");
            CharacterSimulationState state = CharacterSimulationStateCodec.Read(m_StateBytes, program);
            CharacterStateHash hash = CharacterSimulationStateCodec.ComputeHash(state, m_StateBytes);
            if (!hash.Equals(StateHash))
                throw new InvalidDataException($"Actor '{ActorId}' Character state hash is invalid.");
            return state;
        }
    }

    public sealed class SimulationWorldSnapshot
    {
        readonly ReadOnlyCollection<SimulationActorSnapshot> m_Actors;
        readonly byte[] m_WorldStateBytes;

        public SimulationWorldSnapshot(
            SimulationNumericProfile numericProfile,
            ProgramCatalogHash programCatalogHash,
            SolverImplementationId solverId,
            string solverVersion,
            WorldRevision worldRevision,
            SimulationTick tick,
            IEnumerable<SimulationActorSnapshot> actors,
            byte[] worldStateBytes,
            bool deterministicValidity)
        {
            if (!numericProfile.IsValid || !programCatalogHash.IsValid || string.IsNullOrEmpty(solverId.Value) || string.IsNullOrEmpty(worldRevision.Value) || !tick.IsValid)
                throw new ArgumentException("Simulation World Snapshot header is incomplete.");
            NumericProfile = numericProfile;
            ProgramCatalogHash = programCatalogHash;
            SolverId = solverId;
            SolverVersion = SimulationIdentity.Require(solverVersion, nameof(solverVersion));
            WorldRevision = worldRevision;
            Tick = tick;
            var copied = actors == null ? new List<SimulationActorSnapshot>() : new List<SimulationActorSnapshot>(actors);
            for (int i = 0; i < copied.Count; i++)
            {
                if (copied[i] == null)
                    throw new ArgumentException("Simulation World Snapshot actor roster contains a null entry.", nameof(actors));
            }
            copied.Sort((left, right) => left.ActorId.CompareTo(right.ActorId));
            if (copied.Count == 0)
                throw new ArgumentException("Simulation World Snapshot actor roster cannot be empty.", nameof(actors));
            for (int i = 0; i < copied.Count; i++)
            {
                if (copied[i] == null || i > 0 && copied[i - 1].ActorId == copied[i].ActorId)
                    throw new ArgumentException("Simulation World Snapshot actor roster contains null or duplicate entries.", nameof(actors));
            }
            m_Actors = copied.AsReadOnly();
            m_WorldStateBytes = worldStateBytes == null ? throw new ArgumentNullException(nameof(worldStateBytes)) : (byte[])worldStateBytes.Clone();
            DeterministicValidity = deterministicValidity;
            WorldHash = SimulationWorldSnapshotCodec.ComputeHash(this);
        }

        public SimulationNumericProfile NumericProfile { get; }
        public ProgramCatalogHash ProgramCatalogHash { get; }
        public SolverImplementationId SolverId { get; }
        public string SolverVersion { get; }
        public WorldRevision WorldRevision { get; }
        public SimulationTick Tick { get; }
        public IReadOnlyList<SimulationActorSnapshot> Actors => m_Actors;
        public ReadOnlyMemory<byte> WorldStateBytes => m_WorldStateBytes;
        internal byte[] WorldStateBytesBuffer => m_WorldStateBytes;
        public bool DeterministicValidity { get; }
        public SimulationWorldHash WorldHash { get; }

        public WorldSimulationState DecodeWorldState()
        {
            return WorldSimulationStateCodec.Read(m_WorldStateBytes, NumericProfile, SolverId, SolverVersion, WorldRevision);
        }
    }

    public static class SimulationWorldSnapshotFactory
    {
        public static SimulationWorldSnapshot Capture(
            SimulationProgramCatalog catalog,
            SimulationTick tick,
            IEnumerable<SimulationActorState> actorStates,
            WorldSimulationState worldState,
            WorldCapability solverCapabilities)
        {
            if (catalog == null)
                throw new ArgumentNullException(nameof(catalog));
            if (worldState == null)
                throw new ArgumentNullException(nameof(worldState));
            if (catalog.NumericProfile != worldState.NumericProfile)
                throw new InvalidOperationException("Program Catalog and World state Numeric Profiles do not match.");
            var actors = actorStates == null ? new List<SimulationActorState>() : new List<SimulationActorState>(actorStates);
            for (int i = 0; i < actors.Count; i++)
            {
                if (actors[i] == null)
                    throw new InvalidOperationException("Character state roster contains a null entry.");
            }
            actors.Sort((left, right) => left.ActorId.CompareTo(right.ActorId));
            if (actors.Count != worldState.Bodies.Count)
                throw new InvalidOperationException("Character state roster and World body roster do not match.");
            var snapshots = new SimulationActorSnapshot[actors.Count];
            bool programsDeterministic = true;
            for (int i = 0; i < actors.Count; i++)
            {
                SimulationActorState actor = actors[i];
                if (i > 0 && actors[i - 1].ActorId == actor.ActorId || worldState.Bodies[i].ActorId != actor.ActorId)
                    throw new InvalidOperationException("Character state roster and World body roster are not the same stable ActorId order.");
                CharacterSimulationProgram program = catalog.GetRequired(actor.State.ProgramId);
                if (actor.State.NumericProfile != catalog.NumericProfile || !program.ProgramHash.Equals(actor.State.ProgramHash) || !program.LayoutHash.Equals(actor.State.LayoutHash))
                    throw new InvalidOperationException($"Actor '{actor.ActorId}' Character state binding does not match Catalog.");
                byte[] stateBytes = CharacterSimulationStateCodec.Write(actor.State);
                snapshots[i] = new SimulationActorSnapshot(
                    actor.ActorId,
                    program.Manifest.ProgramId,
                    program.ProgramHash,
                    program.LayoutHash,
                    CharacterSimulationStateCodec.ComputeHash(actor.State, stateBytes),
                    stateBytes);
                programsDeterministic &= program.Manifest.NumericProfile.DeterministicReplay && program.Manifest.Capabilities.HasGameplayCapability("DeterministicReplay");
            }
            bool deterministicValidity = catalog.NumericProfile.DeterministicReplay && programsDeterministic && (solverCapabilities & WorldCapability.DeterministicReplay) != 0;
            return new SimulationWorldSnapshot(
                catalog.NumericProfile,
                catalog.CatalogHash,
                worldState.SolverId,
                worldState.SolverVersion,
                worldState.WorldRevision,
                tick,
                snapshots,
                WorldSimulationStateCodec.Write(worldState),
                deterministicValidity);
        }
    }

    public sealed class SimulationWorldStateSet
    {
        readonly ReadOnlyCollection<SimulationActorState> m_Actors;

        public SimulationWorldStateSet(ulong lastCompletedTick, IEnumerable<SimulationActorState> actors, WorldSimulationState worldState)
        {
            LastCompletedTick = lastCompletedTick;
            WorldState = worldState ?? throw new ArgumentNullException(nameof(worldState));
            var copied = actors == null ? new List<SimulationActorState>() : new List<SimulationActorState>(actors);
            for (int i = 0; i < copied.Count; i++)
            {
                if (copied[i] == null)
                    throw new ArgumentException("Simulation state Actor roster contains a null entry.", nameof(actors));
            }
            copied.Sort((left, right) => left.ActorId.CompareTo(right.ActorId));
            if (copied.Count == 0 || copied.Count != worldState.Bodies.Count)
                throw new ArgumentException("Simulation state Actor and World body rosters must be non-empty and equal.", nameof(actors));
            for (int i = 0; i < copied.Count; i++)
            {
                if (copied[i] == null || copied[i].State.NumericProfile != worldState.NumericProfile || copied[i].ActorId != worldState.Bodies[i].ActorId || i > 0 && copied[i - 1].ActorId == copied[i].ActorId)
                    throw new ArgumentException("Simulation state Actor and World body rosters must share one stable ActorId order.", nameof(actors));
            }
            m_Actors = copied.AsReadOnly();
        }

        public ulong LastCompletedTick { get; }
        public IReadOnlyList<SimulationActorState> Actors => m_Actors;
        public WorldSimulationState WorldState { get; }
    }

    public sealed class SimulationWorldStateStore
    {
        readonly SimulationProgramCatalog m_Catalog;
        SimulationWorldStateSet m_Current;

        public SimulationWorldStateStore(SimulationProgramCatalog catalog, SimulationWorldStateSet initialState)
        {
            m_Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            m_Current = initialState ?? throw new ArgumentNullException(nameof(initialState));
            ValidateCurrentBindings(initialState);
        }

        public SimulationWorldStateSet Current => m_Current;

        public void Restore(SimulationWorldSnapshot snapshot)
        {
            SimulationWorldStateSet restored = ValidateAndDecode(snapshot);
            m_Current = restored;
        }

        public SimulationWorldStateSet PrepareRestore(SimulationWorldSnapshot snapshot)
        {
            return ValidateAndDecode(snapshot);
        }

        public void ReplaceValidated(SimulationWorldStateSet stateSet)
        {
            if (stateSet == null)
                throw new ArgumentNullException(nameof(stateSet));
            ValidateCurrentBindings(stateSet);
            RequireSameRosterAndWorldBinding(m_Current, stateSet);
            m_Current = stateSet;
        }

        public void Publish(SimulationWorldStateSet stateSet)
        {
            if (stateSet == null)
                throw new ArgumentNullException(nameof(stateSet));
            if (stateSet.LastCompletedTick != checked(m_Current.LastCompletedTick + 1))
                throw new InvalidOperationException("Published Simulation state must immediately follow the current Tick.");
            ValidateCurrentBindings(stateSet);
            RequireSameRosterAndWorldBinding(m_Current, stateSet);
            for (int i = 0; i < stateSet.Actors.Count; i++)
            {
                if (stateSet.Actors[i].State.LastCompletedTick != stateSet.LastCompletedTick)
                    throw new InvalidOperationException($"Actor '{stateSet.Actors[i].ActorId}' state Tick does not match the published Tick.");
            }
            m_Current = stateSet;
        }

        SimulationWorldStateSet ValidateAndDecode(SimulationWorldSnapshot snapshot)
        {
            if (snapshot == null)
                throw new ArgumentNullException(nameof(snapshot));
            if (snapshot.NumericProfile != m_Catalog.NumericProfile || !snapshot.ProgramCatalogHash.Equals(m_Catalog.CatalogHash))
                throw new InvalidDataException("Snapshot Numeric Profile or ProgramCatalogHash does not match the active Catalog.");
            if (!snapshot.SolverId.Equals(m_Current.WorldState.SolverId) ||
                !string.Equals(snapshot.SolverVersion, m_Current.WorldState.SolverVersion, StringComparison.Ordinal) ||
                !snapshot.WorldRevision.Equals(m_Current.WorldState.WorldRevision))
                throw new InvalidDataException("Snapshot Solver or WorldRevision binding does not match the active world.");
            if (snapshot.Actors.Count != m_Current.Actors.Count)
                throw new InvalidDataException("Snapshot Actor roster count does not match the active roster.");
            var restoredActors = new SimulationActorState[snapshot.Actors.Count];
            for (int i = 0; i < snapshot.Actors.Count; i++)
            {
                SimulationActorSnapshot actorSnapshot = snapshot.Actors[i];
                SimulationActorState currentActor = m_Current.Actors[i];
                if (actorSnapshot.ActorId != currentActor.ActorId || actorSnapshot.ProgramId != currentActor.State.ProgramId)
                    throw new InvalidDataException("Snapshot Actor roster or Program binding does not match the active roster.");
                CharacterSimulationProgram program = m_Catalog.GetRequired(actorSnapshot.ProgramId);
                CharacterSimulationState state = actorSnapshot.Decode(program);
                if (state.LastCompletedTick != snapshot.Tick.Value)
                    throw new InvalidDataException($"Actor '{actorSnapshot.ActorId}' state Tick does not match Snapshot Tick.");
                restoredActors[i] = new SimulationActorState(actorSnapshot.ActorId, state);
            }
            WorldSimulationState worldState = snapshot.DecodeWorldState();
            if (worldState.Bodies.Count != restoredActors.Length)
                throw new InvalidDataException("Snapshot World body roster does not match Actor roster.");
            for (int i = 0; i < restoredActors.Length; i++)
            {
                if (worldState.Bodies[i].ActorId != restoredActors[i].ActorId)
                    throw new InvalidDataException("Snapshot World body order does not match Actor roster.");
            }
            return new SimulationWorldStateSet(snapshot.Tick.Value, restoredActors, worldState);
        }

        void ValidateCurrentBindings(SimulationWorldStateSet stateSet)
        {
            for (int i = 0; i < stateSet.Actors.Count; i++)
            {
                CharacterSimulationState state = stateSet.Actors[i].State;
                CharacterSimulationProgram program = m_Catalog.GetRequired(state.ProgramId);
                if (state.NumericProfile != m_Catalog.NumericProfile || stateSet.WorldState.NumericProfile != m_Catalog.NumericProfile || !program.ProgramHash.Equals(state.ProgramHash) || !program.LayoutHash.Equals(state.LayoutHash))
                    throw new InvalidDataException($"Actor '{stateSet.Actors[i].ActorId}' state does not match active Catalog.");
            }
        }

        static void RequireSameRosterAndWorldBinding(SimulationWorldStateSet current, SimulationWorldStateSet candidate)
        {
            if (!candidate.WorldState.SolverId.Equals(current.WorldState.SolverId) ||
                candidate.WorldState.NumericProfile != current.WorldState.NumericProfile ||
                !string.Equals(candidate.WorldState.SolverVersion, current.WorldState.SolverVersion, StringComparison.Ordinal) ||
                !candidate.WorldState.WorldRevision.Equals(current.WorldState.WorldRevision) ||
                candidate.Actors.Count != current.Actors.Count)
                throw new InvalidOperationException("Simulation state replacement changes the locked Solver, WorldRevision, or Actor roster.");
            for (int i = 0; i < current.Actors.Count; i++)
            {
                if (candidate.Actors[i].ActorId != current.Actors[i].ActorId ||
                    candidate.Actors[i].State.ProgramId != current.Actors[i].State.ProgramId ||
                    candidate.WorldState.Bodies[i].ActorId != current.WorldState.Bodies[i].ActorId)
                    throw new InvalidOperationException("Simulation state replacement changes the locked Actor or Program binding.");
            }
        }
    }

    public static class SimulationWorldSnapshotCodec
    {
        const uint Magic = 0x504e5343;
        const int Version = 3;

        public static byte[] Write(SimulationWorldSnapshot snapshot)
        {
            if (snapshot == null)
                throw new ArgumentNullException(nameof(snapshot));
            using var writer = new CanonicalWriter();
            writer.WriteUInt32(Magic);
            writer.WriteInt32(Version);
            writer.WriteString(snapshot.WorldHash.ToString());
            WriteHashPayload(writer, snapshot);
            return writer.ToArray();
        }

        public static SimulationWorldSnapshot Read(byte[] bytes)
        {
            var reader = new CanonicalReader(bytes);
            if (reader.ReadUInt32() != Magic || reader.ReadInt32() != Version)
                throw new InvalidDataException("Simulation World Snapshot header is invalid.");
            var expectedHash = new SimulationWorldHash(new StableHash(reader.ReadString()));
            SimulationNumericProfile numericProfile = SimulationNumericProfileCodec.Read(reader);
            var catalogHash = new ProgramCatalogHash(new StableHash(reader.ReadString()));
            var solverId = new SolverImplementationId(reader.ReadString());
            string solverVersion = reader.ReadString();
            var worldRevision = new WorldRevision(reader.ReadString());
            var tick = new SimulationTick(reader.ReadUInt64());
            bool deterministicValidity = reader.ReadBoolean();
            int count = reader.ReadInt32();
            if (count <= 0 || count > 1000000)
                throw new InvalidDataException($"Snapshot actor count '{count}' is invalid.");
            var actors = new SimulationActorSnapshot[count];
            for (int i = 0; i < count; i++)
            {
                actors[i] = new SimulationActorSnapshot(
                    new ActorId(reader.ReadString()),
                    new ProgramId(reader.ReadString()),
                    new ProgramHash(new StableHash(reader.ReadString())),
                    new LayoutHash(new StableHash(reader.ReadString())),
                    new CharacterStateHash(new StableHash(reader.ReadString())),
                    reader.ReadBytes());
            }
            byte[] worldStateBytes = reader.ReadBytes();
            reader.RequireComplete();
            var snapshot = new SimulationWorldSnapshot(numericProfile, catalogHash, solverId, solverVersion, worldRevision, tick, actors, worldStateBytes, deterministicValidity);
            if (!snapshot.WorldHash.Equals(expectedHash))
                throw new InvalidDataException($"Simulation World Snapshot hash mismatch. Expected '{expectedHash}', actual '{snapshot.WorldHash}'.");
            return snapshot;
        }

        public static SimulationWorldHash ComputeHash(SimulationWorldSnapshot snapshot)
        {
            using var writer = new CanonicalWriter();
            WriteHashPayload(writer, snapshot);
            return new SimulationWorldHash(writer.ComputeHash());
        }

        static void WriteHashPayload(CanonicalWriter writer, SimulationWorldSnapshot snapshot)
        {
            SimulationNumericProfileCodec.Write(writer, snapshot.NumericProfile);
            writer.WriteString(snapshot.ProgramCatalogHash.ToString());
            writer.WriteString(snapshot.SolverId.Value);
            writer.WriteString(snapshot.SolverVersion);
            writer.WriteString(snapshot.WorldRevision.Value);
            writer.WriteUInt64(snapshot.Tick.Value);
            writer.WriteBoolean(snapshot.DeterministicValidity);
            writer.WriteInt32(snapshot.Actors.Count);
            for (int i = 0; i < snapshot.Actors.Count; i++)
            {
                SimulationActorSnapshot actor = snapshot.Actors[i];
                writer.WriteString(actor.ActorId.Value);
                writer.WriteString(actor.ProgramId.Value);
                writer.WriteString(actor.ProgramHash.ToString());
                writer.WriteString(actor.LayoutHash.ToString());
                writer.WriteString(actor.StateHash.ToString());
                writer.WriteBytes(actor.StateBytesBuffer);
            }
            writer.WriteBytes(snapshot.WorldStateBytesBuffer);
        }
    }
}
