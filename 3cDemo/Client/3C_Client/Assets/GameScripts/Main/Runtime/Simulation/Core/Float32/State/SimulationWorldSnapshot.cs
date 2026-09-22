using System;
using System.Collections.Generic;
using System.IO;

namespace ThirdPersonSimulation
{
    public readonly struct SimulationActorState
    {
        public SimulationActorState(ActorId actorId, Float32CharacterRuntimeState state)
        {
            if (!actorId.IsValid)
                throw new ArgumentException("ActorId is invalid.", nameof(actorId));
            ActorId = actorId;
            State = state ?? throw new ArgumentNullException(nameof(state));
        }

        public ActorId ActorId { get; }
        public Float32CharacterRuntimeState State { get; }
    }

    public sealed class SimulationActorSnapshot
    {
        readonly byte[] m_StateBytes;

        public SimulationActorSnapshot(
            ActorId actorId,
            GameplayContentHash gameplayContentHash,
            StableHash stateSchemaHash,
            CharacterStateHash stateHash,
            string stateCodecIdentity,
            byte[] ownedStateBytes)
        {
            if (!actorId.IsValid || !gameplayContentHash.IsValid || !stateSchemaHash.IsValid || !stateHash.IsValid ||
                !string.Equals(stateCodecIdentity, Float32CharacterRuntimeStateCodec.CodecIdentity, StringComparison.Ordinal))
            {
                throw new ArgumentException("Actor snapshot identity is incomplete.");
            }
            ActorId = actorId;
            GameplayContentHash = gameplayContentHash;
            StateSchemaHash = stateSchemaHash;
            StateHash = stateHash;
            StateCodecIdentity = stateCodecIdentity;
            m_StateBytes = ownedStateBytes == null
                ? throw new ArgumentNullException(nameof(ownedStateBytes))
                : ownedStateBytes;
        }

        public ActorId ActorId { get; }
        public GameplayContentHash GameplayContentHash { get; }
        public StableHash StateSchemaHash { get; }
        public CharacterStateHash StateHash { get; }
        public string StateCodecIdentity { get; }
        public ReadOnlyMemory<byte> StateBytes => m_StateBytes;
        internal byte[] StateBytesBuffer => m_StateBytes;

        public Float32CharacterRuntimeState Decode(
            SimulationActorBinding actor)
        {
            if (!string.Equals(StateCodecIdentity, Float32CharacterRuntimeStateCodec.CodecIdentity, StringComparison.Ordinal) ||
                actor == null)
            {
                throw new InvalidDataException($"Actor '{ActorId}' snapshot Ability binding is stale or mismatched.");
            }
            if (actor.ActorId != ActorId ||
                !GameplayContentHash.Equals(new GameplayContentHash(actor.GameplayContentHash)) ||
                !StateSchemaHash.Equals(actor.StateSchemaHash))
                throw new InvalidDataException($"Actor '{ActorId}' snapshot Character Runtime binding is stale or mismatched.");
            Float32CharacterRuntimeState state = Float32CharacterRuntimeStateCodec.Read(
                m_StateBytes,
                actor);
            if (!Float32CharacterRuntimeStateCodec.ComputeHash(state).Equals(StateHash))
                throw new InvalidDataException($"Actor '{ActorId}' Character runtime state hash is invalid.");
            return state;
        }
    }

    public sealed class SimulationWorldSnapshot
    {
        readonly IReadOnlyList<SimulationActorSnapshot> m_Actors;
        readonly byte[] m_WorldStateBytes;

        public SimulationWorldSnapshot(
            SimulationNumericProfile numericProfile,
            GameplayContentHash gameplayContentHash,
            StableHash stateSchemaHash,
            SolverImplementationId solverId,
            string solverVersion,
            WorldRevision worldRevision,
            SimulationTick tick,
            IReadOnlyList<SimulationActorSnapshot> actors,
            byte[] worldStateBytes,
            bool deterministicValidity)
            : this(
                numericProfile,
                gameplayContentHash,
                stateSchemaHash,
                solverId,
                solverVersion,
                worldRevision,
                tick,
                CopyActors(actors),
                CopyWorldStateBytes(worldStateBytes),
                deterministicValidity)
        {
        }

        internal SimulationWorldSnapshot(
            SimulationNumericProfile numericProfile,
            GameplayContentHash gameplayContentHash,
            StableHash stateSchemaHash,
            SolverImplementationId solverId,
            string solverVersion,
            WorldRevision worldRevision,
            SimulationTick tick,
            SimulationActorSnapshot[] actors,
            byte[] worldStateBytes,
            bool deterministicValidity)
        {
            if (!numericProfile.IsValid || !gameplayContentHash.IsValid || !stateSchemaHash.IsValid || string.IsNullOrEmpty(solverId.Value) || string.IsNullOrEmpty(worldRevision.Value) || !tick.IsValid)
                throw new ArgumentException("Simulation World Snapshot header is incomplete.");
            NumericProfile = numericProfile;
            GameplayContentHash = gameplayContentHash;
            StateSchemaHash = stateSchemaHash;
            SolverId = solverId;
            SolverVersion = SimulationIdentity.Require(solverVersion, nameof(solverVersion));
            WorldRevision = worldRevision;
            Tick = tick;
            SimulationActorSnapshot[] copied = actors ?? Array.Empty<SimulationActorSnapshot>();
            for (int i = 0; i < copied.Length; i++)
                if (copied[i] == null)
                    throw new ArgumentException("Simulation World Snapshot actor roster contains a null entry.", nameof(actors));
            Array.Sort(copied, (left, right) => left.ActorId.CompareTo(right.ActorId));
            if (copied.Length == 0)
                throw new ArgumentException("Simulation World Snapshot actor roster cannot be empty.", nameof(actors));
            for (int i = 1; i < copied.Length; i++)
                if (copied[i - 1].ActorId == copied[i].ActorId)
                    throw new ArgumentException("Simulation World Snapshot actor roster contains duplicate entries.", nameof(actors));
            m_Actors = copied;
            m_WorldStateBytes = worldStateBytes ?? throw new ArgumentNullException(nameof(worldStateBytes));
            DeterministicValidity = deterministicValidity;
            WorldHash = SimulationWorldSnapshotCodec.ComputeHash(this);
        }

        public SimulationNumericProfile NumericProfile { get; }
        public GameplayContentHash GameplayContentHash { get; }
        public StableHash StateSchemaHash { get; }
        public SolverImplementationId SolverId { get; }
        public string SolverVersion { get; }
        public WorldRevision WorldRevision { get; }
        public SimulationTick Tick { get; }
        public IReadOnlyList<SimulationActorSnapshot> Actors => m_Actors;
        public ReadOnlyMemory<byte> WorldStateBytes => m_WorldStateBytes;
        internal byte[] WorldStateBytesBuffer => m_WorldStateBytes;
        public bool DeterministicValidity { get; }
        public SimulationWorldHash WorldHash { get; }

        static SimulationActorSnapshot[] CopyActors(IReadOnlyList<SimulationActorSnapshot> actors)
        {
            if (actors == null || actors.Count == 0)
                return Array.Empty<SimulationActorSnapshot>();
            var copied = new SimulationActorSnapshot[actors.Count];
            for (int i = 0; i < copied.Length; i++)
                copied[i] = actors[i];
            return copied;
        }

        static byte[] CopyWorldStateBytes(byte[] worldStateBytes) =>
            worldStateBytes == null
                ? throw new ArgumentNullException(nameof(worldStateBytes))
                : (byte[])worldStateBytes.Clone();

        public WorldSimulationState DecodeWorldState() =>
            WorldSimulationStateCodec.Read(m_WorldStateBytes, NumericProfile, SolverId, SolverVersion, WorldRevision);
    }

    public static class SimulationWorldSnapshotFactory
    {
        public static SimulationWorldSnapshot Capture(
            Float32CharacterRuntime characterRuntime,
            SimulationTick tick,
            IReadOnlyList<SimulationActorState> actorStates,
            WorldSimulationState worldState,
            WorldCapability solverCapabilities)
        {
            if (characterRuntime == null)
                throw new ArgumentNullException(nameof(characterRuntime));
            if (worldState == null)
                throw new ArgumentNullException(nameof(worldState));
            if (characterRuntime.NumericProfile != worldState.NumericProfile)
                throw new InvalidOperationException("Character Runtime and World state Numeric Profiles do not match.");
            var actors = actorStates == null || actorStates.Count == 0
                ? Array.Empty<SimulationActorState>()
                : new SimulationActorState[actorStates.Count];
            for (int i = 0; i < actors.Length; i++)
            {
                actors[i] = actorStates[i];
                if (actors[i].State == null)
                    throw new InvalidOperationException("Character runtime state roster contains a null entry.");
            }
            Array.Sort(actors, (left, right) => left.ActorId.CompareTo(right.ActorId));
            if (actors.Length != worldState.Bodies.Count || actors.Length != characterRuntime.Roster.Count)
                throw new InvalidOperationException("Character runtime state, Character Runtime and World body rosters do not match.");
            var snapshots = new SimulationActorSnapshot[actors.Length];
            bool abilitiesDeterministic = true;
            for (int i = 0; i < actors.Length; i++)
            {
                SimulationActorState actor = actors[i];
                SimulationActorBinding binding = characterRuntime.Roster[i];
                if (i > 0 && actors[i - 1].ActorId == actor.ActorId ||
                    worldState.Bodies[i].ActorId != actor.ActorId || binding.ActorId != actor.ActorId)
                {
                    throw new InvalidOperationException("Character runtime state, Character Runtime and World body rosters are not the same stable ActorId order.");
                }
                GameplayContentHash actorContentHash = new GameplayContentHash(binding.GameplayContentHash);
                if (!actor.State.GameplayContentHash.Equals(actorContentHash))
                    throw new InvalidOperationException($"Actor '{actor.ActorId}' Character runtime state identity does not match Character Runtime binding.");
                if (!actor.State.StateSchemaHash.Equals(binding.StateSchemaHash))
                    throw new InvalidOperationException($"Actor '{actor.ActorId}' Character runtime state schema does not match Character Runtime binding.");
                if (actor.State.NumericProfile != characterRuntime.NumericProfile)
                    throw new InvalidOperationException($"Actor '{actor.ActorId}' Character runtime state Numeric Profile does not match Character Runtime.");
                byte[] stateBytes = Float32CharacterRuntimeStateCodec.Write(actor.State);
                snapshots[i] = new SimulationActorSnapshot(
                    actor.ActorId,
                    actor.State.GameplayContentHash,
                    actor.State.StateSchemaHash,
                    Float32CharacterRuntimeStateCodec.ComputeHash(actor.State),
                    Float32CharacterRuntimeStateCodec.CodecIdentity,
                    stateBytes);
                for (int abilityIndex = 0; abilityIndex < binding.AbilityInstallations.Installations.Count; abilityIndex++)
                {
                    Float32GameplayAbilityExecutionData ability = binding.AbilityInstallations.Installations[abilityIndex].Data;
                    abilitiesDeterministic &= ability.NumericProfile.DeterministicReplay && ability.Capabilities.HasGameplayCapability("DeterministicReplay");
                }
            }
            bool deterministicValidity = characterRuntime.NumericProfile.DeterministicReplay && abilitiesDeterministic &&
                (solverCapabilities & WorldCapability.DeterministicReplay) != 0;
            return new SimulationWorldSnapshot(
                characterRuntime.NumericProfile,
                characterRuntime.GameplayContentHash,
                characterRuntime.StateSchemaHash,
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
        readonly IReadOnlyList<SimulationActorState> m_Actors;

        public SimulationWorldStateSet(ulong lastCompletedTick, IReadOnlyList<SimulationActorState> actors, WorldSimulationState worldState)
        {
            LastCompletedTick = lastCompletedTick;
            WorldState = worldState ?? throw new ArgumentNullException(nameof(worldState));
            var copied = actors == null || actors.Count == 0
                ? Array.Empty<SimulationActorState>()
                : new SimulationActorState[actors.Count];
            for (int i = 0; i < copied.Length; i++)
            {
                copied[i] = actors[i];
                if (copied[i].State == null)
                    throw new ArgumentException("Simulation state Actor roster contains a null entry.", nameof(actors));
            }
            Array.Sort(copied, (left, right) => left.ActorId.CompareTo(right.ActorId));
            if (copied.Length == 0 || copied.Length != worldState.Bodies.Count)
                throw new ArgumentException("Simulation state Actor and World body rosters must be non-empty and equal.", nameof(actors));
            for (int i = 0; i < copied.Length; i++)
            {
                if (copied[i].State.NumericProfile != worldState.NumericProfile ||
                    copied[i].ActorId != worldState.Bodies[i].ActorId ||
                    i > 0 && copied[i - 1].ActorId == copied[i].ActorId)
                {
                    throw new ArgumentException("Simulation state Actor and World body rosters must share one stable ActorId order.", nameof(actors));
                }
            }
            m_Actors = copied;
        }

        public ulong LastCompletedTick { get; }
        public IReadOnlyList<SimulationActorState> Actors => m_Actors;
        public WorldSimulationState WorldState { get; }
    }

    public sealed class SimulationWorldStateStore
    {
        readonly Float32CharacterRuntime m_Runtime;
        SimulationWorldStateSet m_Current;

        public SimulationWorldStateStore(Float32CharacterRuntime runtime, SimulationWorldStateSet initialState)
        {
            m_Runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            m_Current = initialState ?? throw new ArgumentNullException(nameof(initialState));
            ValidateCurrentBindings(initialState);
        }

        public SimulationWorldStateSet Current => m_Current;

        public void Restore(SimulationWorldSnapshot snapshot) => m_Current = ValidateAndDecode(snapshot);

        public SimulationWorldStateSet PrepareRestore(SimulationWorldSnapshot snapshot) => ValidateAndDecode(snapshot);

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
                if (stateSet.Actors[i].State.LastCompletedTick != stateSet.LastCompletedTick)
                    throw new InvalidOperationException($"Actor '{stateSet.Actors[i].ActorId}' state Tick does not match the published Tick.");
            m_Current = stateSet;
        }

        SimulationWorldStateSet ValidateAndDecode(SimulationWorldSnapshot snapshot)
        {
            if (snapshot == null)
                throw new ArgumentNullException(nameof(snapshot));
            if (snapshot.NumericProfile != m_Runtime.NumericProfile || !snapshot.GameplayContentHash.Equals(m_Runtime.GameplayContentHash) ||
                !snapshot.StateSchemaHash.Equals(m_Runtime.StateSchemaHash))
                throw new InvalidDataException("Snapshot Numeric Profile or GameplayContentHash does not match the active Character Runtime.");
            if (!snapshot.SolverId.Equals(m_Current.WorldState.SolverId) ||
                !string.Equals(snapshot.SolverVersion, m_Current.WorldState.SolverVersion, StringComparison.Ordinal) ||
                !snapshot.WorldRevision.Equals(m_Current.WorldState.WorldRevision))
            {
                throw new InvalidDataException("Snapshot Solver or WorldRevision binding does not match the active world.");
            }
            if (snapshot.Actors.Count != m_Current.Actors.Count)
                throw new InvalidDataException("Snapshot Actor roster count does not match the active roster.");
            var restoredActors = new SimulationActorState[snapshot.Actors.Count];
            for (int i = 0; i < snapshot.Actors.Count; i++)
            {
                SimulationActorSnapshot actorSnapshot = snapshot.Actors[i];
                SimulationActorState currentActor = m_Current.Actors[i];
                SimulationActorBinding binding = m_Runtime.Roster[i];
                if (actorSnapshot.ActorId != currentActor.ActorId ||
                    !actorSnapshot.GameplayContentHash.Equals(currentActor.State.GameplayContentHash) ||
                    !actorSnapshot.StateSchemaHash.Equals(currentActor.State.StateSchemaHash) ||
                    binding.ActorId != actorSnapshot.ActorId)
                    throw new InvalidDataException("Snapshot Actor roster or Ability binding does not match the active roster.");
                Float32CharacterRuntimeState state = actorSnapshot.Decode(
                    binding);
                if (state.LastCompletedTick != snapshot.Tick.Value)
                    throw new InvalidDataException($"Actor '{actorSnapshot.ActorId}' state Tick does not match Snapshot Tick.");
                restoredActors[i] = new SimulationActorState(actorSnapshot.ActorId, state);
            }
            WorldSimulationState worldState = snapshot.DecodeWorldState();
            if (worldState.Bodies.Count != restoredActors.Length)
                throw new InvalidDataException("Snapshot World body roster does not match Actor roster.");
            for (int i = 0; i < restoredActors.Length; i++)
                if (worldState.Bodies[i].ActorId != restoredActors[i].ActorId)
                    throw new InvalidDataException("Snapshot World body order does not match Actor roster.");
            return new SimulationWorldStateSet(snapshot.Tick.Value, restoredActors, worldState);
        }

        void ValidateCurrentBindings(SimulationWorldStateSet stateSet)
        {
            if (stateSet.WorldState.NumericProfile != m_Runtime.NumericProfile || stateSet.Actors.Count != m_Runtime.Roster.Count)
                throw new InvalidDataException("Simulation state does not match the active Character Runtime.");
            for (int i = 0; i < stateSet.Actors.Count; i++)
            {
                SimulationActorState actor = stateSet.Actors[i];
                SimulationActorBinding binding = m_Runtime.Roster[i];
                if (actor.ActorId != binding.ActorId)
                    throw new InvalidDataException($"Simulation state Actor '{actor.ActorId}' does not match the active Character Runtime roster.");
                GameplayContentHash expected = new GameplayContentHash(binding.GameplayContentHash);
                if (!actor.State.GameplayContentHash.Equals(expected))
                    throw new InvalidDataException($"Simulation state Actor '{actor.ActorId}' content identity does not match the active Character Runtime.");
                if (!actor.State.StateSchemaHash.Equals(binding.StateSchemaHash))
                    throw new InvalidDataException($"Simulation state Actor '{actor.ActorId}' schema identity does not match the active Character Runtime.");
            }
        }

        static void RequireSameRosterAndWorldBinding(SimulationWorldStateSet current, SimulationWorldStateSet candidate)
        {
            if (!candidate.WorldState.SolverId.Equals(current.WorldState.SolverId) ||
                candidate.WorldState.NumericProfile != current.WorldState.NumericProfile ||
                !string.Equals(candidate.WorldState.SolverVersion, current.WorldState.SolverVersion, StringComparison.Ordinal) ||
                !candidate.WorldState.WorldRevision.Equals(current.WorldState.WorldRevision) ||
                candidate.Actors.Count != current.Actors.Count)
            {
                throw new InvalidOperationException("Simulation state replacement changes the locked Solver, WorldRevision, or Actor roster.");
            }
            for (int i = 0; i < current.Actors.Count; i++)
            {
                if (candidate.Actors[i].ActorId != current.Actors[i].ActorId ||
                    !candidate.Actors[i].State.GameplayContentHash.Equals(current.Actors[i].State.GameplayContentHash) ||
                    !candidate.Actors[i].State.StateSchemaHash.Equals(current.Actors[i].State.StateSchemaHash) ||
                    candidate.WorldState.Bodies[i].ActorId != current.WorldState.Bodies[i].ActorId)
                {
                    throw new InvalidOperationException("Simulation state replacement changes the locked Actor or Ability binding.");
                }
            }
        }
    }

    public static class SimulationWorldSnapshotCodec
    {
        const uint Magic = 0x504e5343;
        const int Version = 6;

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
            var gameplayContentHash = new GameplayContentHash(new StableHash(reader.ReadString()));
            var stateSchemaHash = new StableHash(reader.ReadString());
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
                    new GameplayContentHash(new StableHash(reader.ReadString())),
                    new StableHash(reader.ReadString()),
                    new CharacterStateHash(new StableHash(reader.ReadString())),
                    reader.ReadString(),
                    reader.ReadBytes());
            }
            byte[] worldStateBytes = reader.ReadBytes();
            reader.RequireComplete();
            var snapshot = new SimulationWorldSnapshot(
                numericProfile,
                gameplayContentHash,
                stateSchemaHash,
                solverId,
                solverVersion,
                worldRevision,
                tick,
                actors,
                worldStateBytes,
                deterministicValidity);
            if (!snapshot.WorldHash.Equals(expectedHash))
                throw new InvalidDataException($"Simulation World Snapshot hash mismatch. Expected '{expectedHash}', actual '{snapshot.WorldHash}'.");
            RequireCanonical(bytes, Write(snapshot), "Simulation World Snapshot");
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
            writer.WriteString(snapshot.GameplayContentHash.ToString());
            writer.WriteString(snapshot.StateSchemaHash.ToString());
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
                writer.WriteString(actor.GameplayContentHash.ToString());
                writer.WriteString(actor.StateSchemaHash.ToString());
                writer.WriteString(actor.StateHash.ToString());
                writer.WriteString(actor.StateCodecIdentity);
                writer.WriteBytes(actor.StateBytesBuffer);
            }
            writer.WriteBytes(snapshot.WorldStateBytesBuffer);
        }

        static void RequireCanonical(byte[] source, byte[] canonical, string label)
        {
            if (source.Length != canonical.Length)
                throw new InvalidDataException($"{label} is not canonical.");
            for (int i = 0; i < source.Length; i++)
                if (source[i] != canonical[i])
                    throw new InvalidDataException($"{label} is not canonical.");
        }
    }
}
