using ThirdPersonSimulation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;

namespace ThirdPersonSimulation
{
    public static class CharacterSimulationStateCodec
    {
        const uint Magic = 0x54534343;
		const int Version = 20;
		public const string CodecIdentity = "character-state/float32/v21";
		const string HashIdentity = "character-state-hash/float32/v19";

        public static byte[] Write(CharacterSimulationState state)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            using var writer = new CanonicalWriter();
            WriteCanonical(writer, state);
            return writer.ToArray();
        }

        static void WriteCanonical(CanonicalWriter writer, CharacterSimulationState state)
        {
            writer.WriteUInt32(Magic);
            writer.WriteInt32(Version);
            writer.WriteString(CodecIdentity);
            SimulationNumericProfileCodec.Write(writer, state.NumericProfile);
            writer.WriteInt32(state.NumericProfile.AbiVersion.Value);
            writer.WriteString(state.ProgramId.Value);
            writer.WriteString(state.ProgramHash.ToString());
            writer.WriteString(state.LayoutHash.ToString());
            writer.WriteUInt64(state.LastCompletedTick);
            writer.WriteUInt64(state.EventSequence);
            writer.WriteUInt64(state.ActionEventSequence);
            writer.WriteInt32(state.SlotCount);
            ProgramExecutionLayout layout = state.ExecutionLayout;
            for (int i = 0; i < state.SlotCount; i++)
            {
                ProgramStateSlot slot = layout.Program.StateSlots[i];
                WriteValue(writer, state.Get(i, slot.ValueKind), layout);
            }
            writer.WriteBytes(CharacterControlRuntimeStateCodec.Write(state.ControlState));
			bool hasGameplayEffectState = state.TryGetGameplayEffectState(out GameplayEffectStateAggregate gameplayEffectState);
			writer.WriteBoolean(hasGameplayEffectState);
			if (hasGameplayEffectState)
				GameplayEffectStateAggregateCodec.Write(writer, gameplayEffectState, layout.GameplayEffectProgram);
			bool hasEquipmentState = state.TryGetEquipmentState(out EquipmentStateAggregate equipmentState);
			writer.WriteBoolean(hasEquipmentState);
			if (hasEquipmentState)
				EquipmentStateAggregateCodec.Write(writer, equipmentState);
        }

        public static CharacterSimulationState Read(byte[] bytes, CharacterSimulationProgram program)
        {
            if (program == null)
                throw new ArgumentNullException(nameof(program));
            var reader = new CanonicalReader(bytes);
            if (reader.ReadUInt32() != Magic || reader.ReadInt32() != Version ||
                !string.Equals(reader.ReadString(), CodecIdentity, StringComparison.Ordinal))
                throw new InvalidDataException("Character state header is invalid.");
            SimulationNumericProfile numericProfile = SimulationNumericProfileCodec.Read(reader);
            var targetAbi = new TargetAbiVersion(reader.ReadInt32());
            var programId = new ProgramId(reader.ReadString());
            var programHash = new ProgramHash(new StableHash(reader.ReadString()));
            var layoutHash = new LayoutHash(new StableHash(reader.ReadString()));
            ulong lastCompletedTick = reader.ReadUInt64();
            ulong eventSequence = reader.ReadUInt64();
            ulong actionEventSequence = reader.ReadUInt64();
            ProgramExecutionLayout layout = ProgramExecutionLayout.GetOrCreate(program);
            if (numericProfile != program.Manifest.NumericProfile ||
                !targetAbi.Equals(program.Manifest.NumericProfile.AbiVersion) ||
                programId != program.Manifest.ProgramId ||
                !programHash.Equals(program.ProgramHash) ||
                !layoutHash.Equals(program.LayoutHash))
            {
                throw new InvalidDataException("Character state Program binding is stale or mismatched.");
            }
            int count = reader.ReadInt32();
            if (count != program.StateSlots.Count)
                throw new InvalidDataException($"Character state has '{count}' slots, expected '{program.StateSlots.Count}'.");
            var values = new CharacterStateValue[count];
            for (int i = 0; i < count; i++)
            {
                values[i] = ReadValue(reader, layout);
                if (values[i].Kind != program.StateSlots[i].ValueKind)
                    throw new InvalidDataException($"Character state slot '{i}' kind does not match Program layout.");
            }
            CharacterControlRuntimeState controlState = CharacterControlRuntimeStateCodec.Read(reader.ReadBytes());
			if (controlState.LastCompletedTick != lastCompletedTick)
				throw new InvalidDataException("Character control runtime state Tick does not match Character state.");
			bool hasGameplayEffectState = reader.ReadBoolean();
			GameplayEffectStateAggregate gameplayEffectState = hasGameplayEffectState
				? GameplayEffectStateAggregateCodec.Read(reader, layout.GameplayEffectProgram)
				: null;
			if (hasGameplayEffectState != program.Manifest.Capabilities.HasGameplayCapability("GameplayEffect"))
				throw new InvalidDataException("Character Gameplay Effect state presence does not match its runtime capability.");
			bool hasEquipmentState = reader.ReadBoolean();
			EquipmentStateAggregate equipmentState = hasEquipmentState
				? EquipmentStateAggregateCodec.Read(reader, layout.Equipment)
				: null;
			if (hasEquipmentState != layout.Equipment.CapabilityEnabled)
				throw new InvalidDataException("Character Equipment state presence does not match its runtime binding.");
            reader.RequireComplete();
            var result = CharacterSimulationState.Create(program, layout, lastCompletedTick, values, eventSequence, actionEventSequence, controlState, gameplayEffectState, equipmentState);
            RequireCanonical(bytes, Write(result), "Character state");
            return result;
        }

        public static CharacterStateHash ComputeHash(CharacterSimulationState state)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            if (state.TryGetStateHash(out CharacterStateHash stateHash))
                return stateHash;
            using var writer = new CanonicalWriter();
            writer.WriteString(HashIdentity);
            WriteCanonical(writer, state);
            return state.CacheStateHash(new CharacterStateHash(writer.ComputeHash()));
        }

        internal static void WriteValue(
            CanonicalWriter writer,
            CharacterStateValue value,
            ProgramExecutionLayout layout)
        {
            writer.WriteByte((byte)value.Kind);
            switch (value.Kind)
            {
                case ProgramStateValueKind.Boolean: writer.WriteBoolean(value.Boolean); break;
                case ProgramStateValueKind.Int32: writer.WriteInt32(value.Int32); break;
                case ProgramStateValueKind.UInt64: writer.WriteUInt64(value.UInt64); break;
                case ProgramStateValueKind.Scalar: writer.WriteScalar(value.Scalar); break;
                case ProgramStateValueKind.Vector2: writer.WriteVector2(value.Vector2); break;
                case ProgramStateValueKind.Vector3: writer.WriteVector3(value.Vector3); break;
                case ProgramStateValueKind.Yaw: writer.WriteYaw(value.Yaw); break;
                case ProgramStateValueKind.Identity: writer.WriteString(value.Identity); break;
                case ProgramStateValueKind.BlackboardOwnerToken: WriteBlackboardOwnerToken(writer, value.BlackboardOwnerToken); break;
                case ProgramStateValueKind.BlackboardWriteStamp: WriteBlackboardWriteStamp(writer, value.BlackboardWriteStamp); break;
                case ProgramStateValueKind.InputRequest: WriteInputRequest(writer, value.InputRequest); break;
                case ProgramStateValueKind.ActionActivationRequest: WriteActionRequest(writer, value.ActionActivationRequest); break;
                case ProgramStateValueKind.ActionInstance: WriteActionInstance(writer, value.ActionInstance); break;
                case ProgramStateValueKind.ActionInstanceReference: WriteActionReference(writer, value.ActionInstanceReference); break;
                case ProgramStateValueKind.ActionTargetSnapshot: WriteTargetSnapshot(writer, value.ActionTargetSnapshot); break;
                case ProgramStateValueKind.AbilityExecutionState: WriteSkillExecutionState(writer, value.SkillExecutionState, layout); break;
                default: throw new InvalidDataException($"Unsupported Character state value kind '{value.Kind}'.");
            }
        }

        internal static CharacterStateValue ReadValue(
            CanonicalReader reader,
            ProgramExecutionLayout layout)
        {
            ProgramStateValueKind kind = ReadEnum<ProgramStateValueKind>(reader.ReadByte());
            switch (kind)
            {
                case ProgramStateValueKind.Boolean: return CharacterStateValue.FromBoolean(reader.ReadBoolean());
                case ProgramStateValueKind.Int32: return CharacterStateValue.FromInt32(reader.ReadInt32());
                case ProgramStateValueKind.UInt64: return CharacterStateValue.FromUInt64(reader.ReadUInt64());
                case ProgramStateValueKind.Scalar: return CharacterStateValue.FromScalar(reader.ReadScalar());
                case ProgramStateValueKind.Vector2: return CharacterStateValue.FromVector2(reader.ReadVector2());
                case ProgramStateValueKind.Vector3: return CharacterStateValue.FromVector3(reader.ReadVector3());
                case ProgramStateValueKind.Yaw: return CharacterStateValue.FromYaw(reader.ReadYaw());
                case ProgramStateValueKind.Identity: return CharacterStateValue.FromIdentity(reader.ReadString());
                case ProgramStateValueKind.BlackboardOwnerToken: return CharacterStateValue.FromBlackboardOwnerToken(ReadBlackboardOwnerToken(reader));
                case ProgramStateValueKind.BlackboardWriteStamp: return CharacterStateValue.FromBlackboardWriteStamp(ReadBlackboardWriteStamp(reader));
                case ProgramStateValueKind.InputRequest: return CharacterStateValue.FromInputRequest(ReadInputRequest(reader));
                case ProgramStateValueKind.ActionActivationRequest: return CharacterStateValue.FromActionActivationRequest(ReadActionRequest(reader, layout));
                case ProgramStateValueKind.ActionInstance: return CharacterStateValue.FromActionInstance(ReadActionInstance(reader, layout));
                case ProgramStateValueKind.ActionInstanceReference: return CharacterStateValue.FromActionInstanceReference(ReadActionReference(reader));
                case ProgramStateValueKind.ActionTargetSnapshot: return CharacterStateValue.FromActionTargetSnapshot(ReadTargetSnapshot(reader));
                case ProgramStateValueKind.AbilityExecutionState: return CharacterStateValue.FromSkillExecutionState(ReadSkillExecutionState(reader, layout));
                default: throw new InvalidDataException($"Unsupported Character state value kind '{kind}'.");
            }
        }

        static void WriteBlackboardOwnerToken(CanonicalWriter writer, BlackboardOwnerToken value)
        {
            writer.WriteByte((byte)value.ScopeKind);
            writer.WriteInt32(value.CompiledOwnerIndex);
            writer.WriteUInt64(value.Generation);
        }

        static BlackboardOwnerToken ReadBlackboardOwnerToken(CanonicalReader reader)
        {
            byte scope = reader.ReadByte();
            int owner = reader.ReadInt32();
            ulong generation = reader.ReadUInt64();
            if (scope == 0 && owner == 0 && generation == 0)
                return default;
            return new BlackboardOwnerToken(ReadEnum<ProgramScopeKind>(scope), owner, generation);
        }

        static void WriteBlackboardWriteStamp(CanonicalWriter writer, BlackboardWriteStamp value)
        {
            writer.WriteInt32(value.SourceOperation.Value);
            writer.WriteUInt64(value.LogicTick);
            writer.WriteUInt64(value.ActionInstanceId);
            writer.WriteInt32(value.TimelineOperation.Value);
            writer.WriteInt32(value.ClipOperation.Value);
            writer.WriteInt32(value.Cycle);
        }

        static BlackboardWriteStamp ReadBlackboardWriteStamp(CanonicalReader reader)
        {
            int source = reader.ReadInt32();
            ulong tick = reader.ReadUInt64();
            ulong action = reader.ReadUInt64();
            int timeline = reader.ReadInt32();
            int clip = reader.ReadInt32();
            int cycle = reader.ReadInt32();
            if (source < 0 && tick == 0 && action == 0 && timeline < 0 && clip < 0 && cycle == 0)
                return default;
            return new BlackboardWriteStamp(
                new OperationHandle(source),
                tick,
                action,
                timeline < 0 ? OperationHandle.Invalid : new OperationHandle(timeline),
                clip < 0 ? OperationHandle.Invalid : new OperationHandle(clip),
                cycle);
        }

        static void WriteInputRequest(CanonicalWriter writer, Float32InputRequestState value)
        {
            writer.WriteBoolean(value.IsValid);
            if (!value.IsValid)
                return;
            writer.WriteString(value.RequestId);
            writer.WriteUInt64(value.Sequence);
            writer.WriteUInt64(value.SourceTick);
            writer.WriteUInt64(value.ExpireTick);
            writer.WriteInt32(value.Priority);
            writer.WriteBoolean(value.Consumed);
        }

        static Float32InputRequestState ReadInputRequest(CanonicalReader reader)
        {
            if (!reader.ReadBoolean())
                return default;
            var value = new Float32InputRequestState(
                reader.ReadString(),
                reader.ReadUInt64(),
                reader.ReadUInt64(),
                reader.ReadUInt64(),
                reader.ReadInt32(),
                reader.ReadBoolean());
            if (!value.IsValid)
                throw new InvalidDataException("Character state Input request identity is invalid.");
            return value;
        }

        static void WriteActionRequest(CanonicalWriter writer, Float32ActionActivationRequestState value)
        {
            writer.WriteBoolean(value.IsValid);
            if (!value.IsValid)
                return;
            writer.WriteString(value.ActionId);
            writer.WriteString(value.SkillId.Value ?? string.Empty);
            writer.WriteInt32(value.SkillEntryOperation.Value);
            writer.WriteString(value.ContextId);
            writer.WriteString(value.SourceInputRequestId);
            writer.WriteUInt64(value.InputSequence);
            writer.WriteUInt64(value.StartTick);
            writer.WriteString(value.TargetKey);
            WriteTargetSnapshot(writer, value.TargetSnapshot);
            SimulationExecutionSourceCodec.Write(writer, value.Source);
            EquipmentActionContextCodec.Write(writer, value.EquipmentContext);
            writer.WriteUInt64(value.ReplacementActionInstanceId);
        }

        static Float32ActionActivationRequestState ReadActionRequest(
            CanonicalReader reader,
            ProgramExecutionLayout layout)
        {
            if (!reader.ReadBoolean())
                return default;
            string actionId = reader.ReadString();
            CharacterSkillId skillId = ReadOptionalSkillId(reader.ReadString());
            OperationHandle skillEntryOperation = new OperationHandle(reader.ReadInt32());
            string contextId = reader.ReadString();
            string sourceInputRequestId = reader.ReadString();
            ulong inputSequence = reader.ReadUInt64();
            ulong startTick = reader.ReadUInt64();
            string targetKey = reader.ReadString();
            SimulationActionTargetSnapshot target = ReadTargetSnapshot(reader);
            SimulationExecutionSource source = ReadExecutionSource(reader, layout);
            EquipmentActionContext equipmentContext = EquipmentActionContextCodec.Read(reader, layout.Equipment);
            ulong replacementActionInstanceId = reader.ReadUInt64();
            return new Float32ActionActivationRequestState(
                actionId,
                skillId,
                skillEntryOperation,
                contextId,
                sourceInputRequestId,
                inputSequence,
                startTick,
                targetKey,
                target,
                source,
                equipmentContext,
                replacementActionInstanceId);
        }

        static void WriteActionInstance(CanonicalWriter writer, Float32ActionInstanceState value)
        {
            writer.WriteBoolean(value.IsValid);
            if (!value.IsValid)
                return;
            writer.WriteString(value.ActionId);
            writer.WriteString(value.SkillId.Value ?? string.Empty);
            writer.WriteInt32(value.SkillEntryOperation.Value);
            writer.WriteUInt64(value.SkillExecutionGeneration);
            writer.WriteString(value.ContextId);
            writer.WriteUInt64(value.InstanceId);
            writer.WriteUInt64(value.PredictionKey);
            writer.WriteString(value.SourceInputRequestId);
            writer.WriteUInt64(value.InputSequence);
            writer.WriteUInt64(value.StartTick);
            writer.WriteString(value.TargetKey);
            WriteTargetSnapshot(writer, value.TargetSnapshot);
            SimulationExecutionSourceCodec.Write(writer, value.Source);
            writer.WriteByte((byte)value.Phase);
            writer.WriteByte((byte)value.State);
            writer.WriteByte((byte)value.LastTransition);
            writer.WriteUInt64(value.LastTransitionTick);
            writer.WriteUInt64(value.LastTransitionSourceTick);
            writer.WriteString(value.Reason);
            EquipmentActionContextCodec.Write(writer, value.EquipmentContext);
            writer.WriteUInt64(value.SegmentGeneration);
        }

        static Float32ActionInstanceState ReadActionInstance(
            CanonicalReader reader,
            ProgramExecutionLayout layout)
        {
            if (!reader.ReadBoolean())
                return default;
            var value = new Float32ActionInstanceState(
                reader.ReadString(),
                ReadOptionalSkillId(reader.ReadString()),
                new OperationHandle(reader.ReadInt32()),
                reader.ReadUInt64(),
                reader.ReadString(),
                reader.ReadUInt64(),
                reader.ReadUInt64(),
                reader.ReadString(),
                reader.ReadUInt64(),
                reader.ReadUInt64(),
                reader.ReadString(),
                ReadTargetSnapshot(reader),
                ReadExecutionSource(reader, layout),
                ReadEnum<SimulationActionPhase>(reader.ReadByte()),
                ReadEnum<SimulationActionState>(reader.ReadByte()),
                ReadEnum<SimulationActionLifecycleTransitionType>(reader.ReadByte()),
                reader.ReadUInt64(),
                reader.ReadUInt64(),
                reader.ReadString(),
                EquipmentActionContextCodec.Read(reader, layout.Equipment),
                reader.ReadUInt64());
            if (!value.IsValid)
                throw new InvalidDataException("Character state Action instance identity is invalid.");
            return value;
        }

        static CharacterSkillId ReadOptionalSkillId(string value) =>
            string.IsNullOrEmpty(value) ? default : new CharacterSkillId(value);

        static void WriteActionReference(CanonicalWriter writer, Float32ActionInstanceReference value)
        {
            if (!value.IsValid && value.HasAnyIdentity)
                throw new InvalidDataException("Character state Action instance reference has an incomplete execution identity.");
            writer.WriteBoolean(value.IsValid);
            if (!value.IsValid)
                return;
            writer.WriteString(value.ActionId);
            writer.WriteString(value.ContextId);
            writer.WriteUInt64(value.InstanceId);
            writer.WriteUInt64(value.PredictionKey);
            writer.WriteBoolean(value.HasSkillExecution);
            if (value.HasSkillExecution)
            {
                writer.WriteString(value.SkillId.Value);
                writer.WriteInt32(value.SkillEntryOperation.Value);
                writer.WriteUInt64(value.SkillExecutionGeneration);
            }
        }

        static Float32ActionInstanceReference ReadActionReference(CanonicalReader reader)
        {
            if (!reader.ReadBoolean())
                return default;
            string actionId = reader.ReadString();
            string contextId = reader.ReadString();
            ulong instanceId = reader.ReadUInt64();
            ulong predictionKey = reader.ReadUInt64();
            bool hasSkillExecution = reader.ReadBoolean();
            CharacterSkillId skillId = default;
            OperationHandle skillEntryOperation = OperationHandle.Invalid;
            ulong skillExecutionGeneration = 0;
            if (hasSkillExecution)
            {
                skillId = new CharacterSkillId(reader.ReadString());
                skillEntryOperation = new OperationHandle(reader.ReadInt32());
                skillExecutionGeneration = reader.ReadUInt64();
            }
            var value = new Float32ActionInstanceReference(
                actionId,
                contextId,
                instanceId,
                predictionKey,
                skillId,
                skillEntryOperation,
                skillExecutionGeneration);
            if (!value.IsValid)
                throw new InvalidDataException("Character state Action instance reference identity is invalid.");
            return value;
        }

        static void WriteSkillExecutionState(
            CanonicalWriter writer,
			GameplayAbilityExecutionAggregate<CharacterStateValue> value,
            ProgramExecutionLayout layout)
        {
            if (value == null)
                throw new InvalidDataException("Character state Skill execution aggregate is missing.");
            writer.WriteInt32(value.Frames.Count);
            for (int frameIndex = 0; frameIndex < value.Frames.Count; frameIndex++)
            {
				GameplayAbilityExecutionFrame<CharacterStateValue> frame = value.Frames[frameIndex];
                if (frame == null || frame.Generation == 0)
                    throw new InvalidDataException("Character state Skill execution frame is incomplete.");
                writer.WriteString(frame.SkillId.Value);
                writer.WriteInt32(frame.EntryOperation.Value);
                writer.WriteUInt64(frame.ActionInstanceId);
                writer.WriteUInt64(frame.PredictionKey);
                writer.WriteUInt64(frame.Generation);
                writer.WriteInt32(frame.Values.Count);
                foreach (KeyValuePair<int, CharacterStateValue> state in frame.Values)
                {
                    if (state.Key < 0 || state.Key >= layout.Program.StateSlots.Count ||
                        state.Value.Kind != layout.Program.StateSlots[state.Key].ValueKind)
                        throw new InvalidDataException("Character state Skill execution frame contains an invalid state value.");
                    writer.WriteInt32(state.Key);
                    WriteValue(writer, state.Value, layout);
                }
            }
        }

		static GameplayAbilityExecutionAggregate<CharacterStateValue> ReadSkillExecutionState(
            CanonicalReader reader,
            ProgramExecutionLayout layout)
        {
            int frameCount = reader.ReadInt32();
            if (frameCount < 0)
                throw new InvalidDataException("Character state Skill execution frame count is invalid.");
			var frames = new List<GameplayAbilityExecutionFrame<CharacterStateValue>>(frameCount);
            for (int frameIndex = 0; frameIndex < frameCount; frameIndex++)
            {
                CharacterSkillId skillId = new CharacterSkillId(reader.ReadString());
                OperationHandle entryOperation = new OperationHandle(reader.ReadInt32());
                ulong actionInstanceId = reader.ReadUInt64();
                ulong predictionKey = reader.ReadUInt64();
                ulong generation = reader.ReadUInt64();
                if (generation == 0)
                    throw new InvalidDataException("Character state Skill execution frame generation is missing.");
                int valueCount = reader.ReadInt32();
                if (valueCount < 0)
                    throw new InvalidDataException("Character state Skill execution frame value count is invalid.");
                var values = new List<KeyValuePair<int, CharacterStateValue>>(valueCount);
                var seen = new HashSet<int>();
                for (int valueIndex = 0; valueIndex < valueCount; valueIndex++)
                {
                    int slotIndex = reader.ReadInt32();
                    if (slotIndex < 0 || slotIndex >= layout.Program.StateSlots.Count || !seen.Add(slotIndex))
                        throw new InvalidDataException("Character state Skill execution frame state address is invalid or duplicated.");
                    CharacterStateValue value = ReadValue(reader, layout);
                    if (value.Kind != layout.Program.StateSlots[slotIndex].ValueKind)
                        throw new InvalidDataException("Character state Skill execution frame state value kind does not match its address.");
                    values.Add(new KeyValuePair<int, CharacterStateValue>(slotIndex, value));
                }
				frames.Add(new GameplayAbilityExecutionFrame<CharacterStateValue>(
                    skillId,
                    entryOperation,
                    actionInstanceId,
                    predictionKey,
                    generation,
                    values));
            }
			return new GameplayAbilityExecutionAggregate<CharacterStateValue>(frames);
        }

        static void WriteTargetSnapshot(CanonicalWriter writer, SimulationActionTargetSnapshot value)
        {
            writer.WriteString(value.TargetId);
            writer.WriteVector3(value.Position);
            writer.WriteYaw(value.Yaw);
        }

        static SimulationActionTargetSnapshot ReadTargetSnapshot(CanonicalReader reader)
        {
            return new SimulationActionTargetSnapshot(
                reader.ReadString(),
                reader.ReadVector3(),
                reader.ReadYaw());
        }

        static SimulationExecutionSource ReadExecutionSource(CanonicalReader reader, ProgramExecutionLayout layout)
        {
            SimulationExecutionSource source = SimulationExecutionSourceCodec.Read(reader);
            if (source.IsSkillOperation && source.Operation.Value >= layout.Program.Operations.Count)
                throw new InvalidDataException($"Character state operation handle '{source.Operation.Value}' is invalid.");
            return source;
        }

        static T ReadEnum<T>(int value) where T : struct
        {
            object candidate = Enum.ToObject(typeof(T), value);
            if (!Enum.IsDefined(typeof(T), candidate))
                throw new InvalidDataException($"Enum value '{value}' is invalid for {typeof(T).Name}.");
            return (T)candidate;
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

                                                                                                                                                           
