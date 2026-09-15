using System;
using System.Collections.Generic;
using System.IO;
using SimulationActionActivationRequestState = ThirdPersonSimulation.SimulationActionActivationRequestState<ThirdPersonSimulation.Fixed.SimulationActionTargetSnapshot>;

namespace ThirdPersonSimulation.Fixed
{
    internal static class FixedCharacterRuntimeStateCodec
    {
        const uint Magic = 0x54535243;
        const int Version = 3;
        const string HashIdentity = "fixed-character-runtime-state-hash/3";
        public const string CodecIdentity = "fixed-character-runtime-state/3";

        public static byte[] Write(FixedCharacterRuntimeState state)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            using var writer = new CanonicalWriter();
            WriteCanonical(writer, state);
            return writer.ToArray();
        }

        public static CharacterStateHash ComputeHash(FixedCharacterRuntimeState state)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            using var writer = new CanonicalWriter();
            writer.WriteString(HashIdentity);
            WriteCanonical(writer, state);
            return new CharacterStateHash(writer.ComputeHash());
        }

        public static FixedCharacterRuntimeState Read(
            byte[] bytes,
            FixedGameplayAbilityExecutionInstallationSet installations,
            GameplayContentHash expectedGameplayContentHash,
            CharacterGameplayEffectRuntimeBinding gameplayEffectBinding,
            CharacterEquipmentRuntimeBinding equipmentBinding)
        {
            if (bytes == null || installations == null)
                throw new ArgumentNullException(bytes == null ? nameof(bytes) : nameof(installations));
            var reader = new CanonicalReader(bytes);
            if (reader.ReadUInt32() != Magic || reader.ReadInt32() != Version ||
                !string.Equals(reader.ReadString(), CodecIdentity, StringComparison.Ordinal))
            {
                throw new InvalidDataException("Fixed Character runtime state header is invalid.");
            }
            SimulationNumericProfile numericProfile = SimulationNumericProfileCodec.Read(reader);
            GameplayContentHash gameplayContentHash = new GameplayContentHash(new StableHash(reader.ReadString()));
            if (!gameplayContentHash.Equals(expectedGameplayContentHash))
                throw new InvalidDataException("Fixed Character runtime state GameplayContentHash does not match the active Character Runtime.");
            ulong lastCompletedTick = reader.ReadUInt64();
            int abilityCount = ReadCount(reader, installations.Installations.Count, "Fixed Character Ability partition");
            if (abilityCount != installations.Installations.Count)
                throw new InvalidDataException("Fixed Character runtime state Ability partitions do not match the installed Ability set.");
            var abilities = new List<FixedAbilityRuntimeState>(abilityCount);
            for (int i = 0; i < abilityCount; i++)
            {
                GameplayAbilityExecutionIdentity identity = ReadIdentity(reader);
                FixedGameplayAbilityExecutionInstallation installation = installations.Require(identity.AbilityId);
                installation.Identity.Require(identity);
                Dictionary<int, AbilityStateValue> stateValues = ReadValues(reader, installation.Layout);
                GameplayAbilityExecutionAggregate<AbilityStateValue> abilityExecutionState = ReadAbilityExecutionState(reader, installation.Layout);
                Dictionary<int, FixedMotionWarpState> motionWarpStates = ReadMotionWarpStates(reader, installation.Layout);
                abilities.Add(new FixedAbilityRuntimeState(
                    identity,
                    lastCompletedTick,
                    stateValues,
                    abilityExecutionState,
                    motionWarpStates));
            }
            EquipmentProgramLayout equipmentLayout = !installations.RequiresEquipment || equipmentBinding == null
                ? null
                : EquipmentProgramLayoutCompiler.CompileRoleStateLayout(equipmentBinding);
            List<SimulationActionActivationRequestState> actionActivationRequests = ReadActionActivationRequests(reader, installations, equipmentLayout);
            List<FixedActionInstanceState> actionInstances = ReadActionInstances(reader, installations, equipmentLayout);
            Dictionary<string, SimulationInputRequestState> inputRequests = ReadInputRequests(reader, installations);
            ulong eventSequence = reader.ReadUInt64();
            ulong actionEventSequence = reader.ReadUInt64();
            ulong handleAllocator = reader.ReadUInt64();
            CharacterControlRuntimeState controlState = reader.ReadBoolean()
                ? CharacterControlRuntimeStateCodec.Read(reader.ReadBytes())
                : null;
            GameplayEffectStateAggregate gameplayEffectState = null;
            if (reader.ReadBoolean())
            {
                FixedGameplayEffectRuntimeCatalog effectCatalog = gameplayEffectBinding == null
                    ? null
                    : new FixedGameplayEffectRuntimeCatalog(gameplayEffectBinding);
                if (effectCatalog == null)
                    throw new InvalidDataException("Fixed Character runtime state contains Gameplay Effect state without a Character Effect service.");
                var effectReader = new CanonicalReader(reader.ReadBytes());
                gameplayEffectState = GameplayEffectStateAggregateCodec.Read(effectReader, effectCatalog);
                effectReader.RequireComplete();
            }
            EquipmentStateAggregate equipmentState = null;
            if (reader.ReadBoolean())
            {
                if (equipmentLayout == null)
                    throw new InvalidDataException("Fixed Character runtime state contains Equipment state without an installed layout.");
                var equipmentReader = new CanonicalReader(reader.ReadBytes());
                equipmentState = EquipmentStateAggregateCodec.Read(equipmentReader, equipmentLayout);
                equipmentReader.RequireComplete();
            }
            reader.RequireComplete();
            var state = new FixedCharacterRuntimeState(
                numericProfile,
                gameplayContentHash,
                lastCompletedTick,
                abilities,
                actionActivationRequests,
                actionInstances,
                inputRequests,
                eventSequence,
                actionEventSequence,
                handleAllocator,
                controlState,
                gameplayEffectState,
                equipmentState);
            RequireCanonical(bytes, Write(state), "Fixed Character runtime state");
            return state;
        }

        static void WriteCanonical(CanonicalWriter writer, FixedCharacterRuntimeState state)
        {
            writer.WriteUInt32(Magic);
            writer.WriteInt32(Version);
            writer.WriteString(CodecIdentity);
            SimulationNumericProfileCodec.Write(writer, state.NumericProfile);
            writer.WriteString(state.GameplayContentHash.ToString());
            writer.WriteUInt64(state.LastCompletedTick);
            writer.WriteInt32(state.Abilities.Count);
            for (int i = 0; i < state.Abilities.Count; i++)
            {
                FixedAbilityRuntimeState ability = state.Abilities[i];
                WriteIdentity(writer, ability.AbilityIdentity);
                WriteValues(writer, ability.StateValues);
                WriteAbilityExecutionState(writer, ability.AbilityExecutionState);
                WriteMotionWarpStates(writer, ability.MotionWarpStates);
            }
            WriteActionActivationRequests(writer, state.ActionActivationRequests);
            WriteActionInstances(writer, state.ActionInstances);
            WriteInputRequests(writer, state.InputRequests);
            writer.WriteUInt64(state.EventSequence);
            writer.WriteUInt64(state.ActionEventSequence);
            writer.WriteUInt64(state.HandleAllocator);
            writer.WriteBoolean(state.ControlState != null);
            if (state.ControlState != null)
                writer.WriteBytes(CharacterControlRuntimeStateCodec.Write(state.ControlState));
            writer.WriteBoolean(state.GameplayEffectState != null);
            if (state.GameplayEffectState != null)
            {
                using var effectWriter = new CanonicalWriter();
                GameplayEffectStateAggregateCodec.Write(
                    effectWriter,
                    state.GameplayEffectState);
                writer.WriteBytes(effectWriter.ToArray());
            }
            writer.WriteBoolean(state.EquipmentState != null);
            if (state.EquipmentState != null)
            {
                using var equipmentWriter = new CanonicalWriter();
                EquipmentStateAggregateCodec.Write(equipmentWriter, state.EquipmentState);
                writer.WriteBytes(equipmentWriter.ToArray());
            }
        }

        static void WriteIdentity(CanonicalWriter writer, GameplayAbilityExecutionIdentity identity)
        {
            writer.WriteString(identity.AbilityId.Value);
            writer.WriteString(identity.ContentHash.ToString());
            writer.WriteString(identity.StateSchemaHash.ToString());
            writer.WriteString(identity.OperationSetVersion.Value);
            SimulationNumericProfileCodec.Write(writer, identity.NumericProfile);
        }

        static GameplayAbilityExecutionIdentity ReadIdentity(CanonicalReader reader) =>
            new GameplayAbilityExecutionIdentity(
                new CharacterSkillId(reader.ReadString()),
                new StableHash(reader.ReadString()),
                new StableHash(reader.ReadString()),
                new OperationSetVersion(reader.ReadString()),
                SimulationNumericProfileCodec.Read(reader));

        static void WriteValues(CanonicalWriter writer, IReadOnlyDictionary<int, AbilityStateValue> values)
        {
            var keys = new List<int>(values.Keys);
            keys.Sort();
            writer.WriteInt32(keys.Count);
            for (int i = 0; i < keys.Count; i++)
            {
                writer.WriteInt32(keys[i]);
                WriteValue(writer, values[keys[i]]);
            }
        }

        static Dictionary<int, AbilityStateValue> ReadValues(CanonicalReader reader, GameplayAbilityExecutionLayout layout)
        {
            int count = ReadCount(reader, layout.StateSlots.Count, "Fixed Character state value");
            var values = new Dictionary<int, AbilityStateValue>(count);
            int previous = -1;
            for (int i = 0; i < count; i++)
            {
                int slotIndex = reader.ReadInt32();
                if (slotIndex < 0 || slotIndex >= layout.StateSlots.Count || slotIndex <= previous)
                    throw new InvalidDataException("Fixed Character state value indexes are invalid or not canonically ordered.");
                AbilityStateValue value = ReadValue(reader);
                if (value.Kind != layout.StateSlots[slotIndex].ValueKind)
                    throw new InvalidDataException($"Fixed Character state value slot '{slotIndex}' kind does not match the Ability layout.");
                values.Add(slotIndex, value);
                previous = slotIndex;
            }
            return values;
        }

        static void WriteAbilityExecutionState(
            CanonicalWriter writer,
            GameplayAbilityExecutionAggregate<AbilityStateValue> aggregate)
        {
            var frames = new List<GameplayAbilityExecutionFrame<AbilityStateValue>>(aggregate?.Frames ?? Array.Empty<GameplayAbilityExecutionFrame<AbilityStateValue>>());
            frames.Sort((left, right) => left.ActionInstanceId.CompareTo(right.ActionInstanceId));
            writer.WriteInt32(frames.Count);
            for (int i = 0; i < frames.Count; i++)
            {
                GameplayAbilityExecutionFrame<AbilityStateValue> frame = frames[i];
                writer.WriteString(frame.SkillId.Value);
                writer.WriteInt32(frame.EntryOperation.Value);
                writer.WriteUInt64(frame.ActionInstanceId);
                writer.WriteUInt64(frame.PredictionKey);
                writer.WriteUInt64(frame.Generation);
                WriteValues(writer, frame.Values);
            }
        }

        static GameplayAbilityExecutionAggregate<AbilityStateValue> ReadAbilityExecutionState(
            CanonicalReader reader,
            GameplayAbilityExecutionLayout layout)
        {
            int count = ReadCount(reader, 1000000, "Fixed Ability execution frame");
            var frames = new List<GameplayAbilityExecutionFrame<AbilityStateValue>>(count);
            ulong previous = 0;
            for (int i = 0; i < count; i++)
            {
                var skillId = new CharacterSkillId(reader.ReadString());
                OperationHandle entryOperation = ReadRequiredOperation(reader, layout);
                ulong actionInstanceId = reader.ReadUInt64();
                ulong predictionKey = reader.ReadUInt64();
                ulong generation = reader.ReadUInt64();
                if (actionInstanceId == 0 || i > 0 && actionInstanceId <= previous)
                    throw new InvalidDataException("Fixed Ability execution frame identities are invalid or not canonically ordered.");
                Dictionary<int, AbilityStateValue> values = ReadValues(reader, layout);
                frames.Add(new GameplayAbilityExecutionFrame<AbilityStateValue>(
                    skillId,
                    entryOperation,
                    actionInstanceId,
                    predictionKey,
                    generation,
                    values));
                previous = actionInstanceId;
            }
            return new GameplayAbilityExecutionAggregate<AbilityStateValue>(frames);
        }

        static void WriteInputRequests(
            CanonicalWriter writer,
            IReadOnlyDictionary<string, SimulationInputRequestState> requests)
        {
            var keys = new List<string>(requests.Keys);
            keys.Sort(StringComparer.Ordinal);
            writer.WriteInt32(keys.Count);
            for (int i = 0; i < keys.Count; i++)
            {
                writer.WriteString(keys[i]);
                SimulationInputRequestStateCodec.Write(writer, requests[keys[i]]);
            }
        }

        static Dictionary<string, SimulationInputRequestState> ReadInputRequests(
            CanonicalReader reader,
            FixedGameplayAbilityExecutionInstallationSet installations)
        {
            int count = ReadCount(reader, 1000000, "Fixed Character Input request");
            var known = new HashSet<string>(StringComparer.Ordinal);
            for (int installationIndex = 0; installationIndex < installations.Installations.Count; installationIndex++)
                for (int requestIndex = 0; requestIndex < installations.Installations[installationIndex].Layout.InputRequestIds.Count; requestIndex++)
                    known.Add(installations.Installations[installationIndex].Layout.InputRequestIds[requestIndex]);
            var requests = new Dictionary<string, SimulationInputRequestState>(StringComparer.Ordinal);
            string previous = null;
            for (int i = 0; i < count; i++)
            {
                string requestId = SimulationIdentity.Require(reader.ReadString(), "InputRequestId");
                if (previous != null && string.CompareOrdinal(previous, requestId) >= 0 || !known.Contains(requestId))
                    throw new InvalidDataException("Fixed Character Input request identities are invalid or not canonically ordered.");
                SimulationInputRequestState value = SimulationInputRequestStateCodec.Read(reader);
                if (value.IsValid && !string.Equals(value.RequestId, requestId, StringComparison.Ordinal))
                    throw new InvalidDataException("Fixed Character Input request state key does not match its value.");
                requests.Add(requestId, value);
                previous = requestId;
            }
            return requests;
        }

        static void WriteActionActivationRequests(
            CanonicalWriter writer,
            IReadOnlyList<SimulationActionActivationRequestState> requests)
        {
            writer.WriteInt32(requests.Count);
            for (int i = 0; i < requests.Count; i++)
            {
                SimulationActionActivationRequestState request = requests[i];
                writer.WriteBoolean(request.IsValid);
                if (!request.IsValid)
                    continue;
                writer.WriteString(request.ActionId);
                WriteOptionalSkill(writer, request.SkillId);
                WriteOptionalOperation(writer, request.SkillEntryOperation);
                writer.WriteString(request.ContextId);
                writer.WriteString(request.SourceInputRequestId);
                writer.WriteUInt64(request.InputSequence);
                writer.WriteUInt64(request.StartTick);
                writer.WriteString(request.TargetKey);
                WriteTarget(writer, request.TargetSnapshot);
                SimulationExecutionSourceCodec.Write(writer, request.Source);
                WriteEquipmentContext(writer, request.EquipmentContext);
                writer.WriteUInt64(request.ReplacementActionInstanceId);
            }
        }

        static List<SimulationActionActivationRequestState> ReadActionActivationRequests(
            CanonicalReader reader,
            FixedGameplayAbilityExecutionInstallationSet installations,
            EquipmentProgramLayout equipmentLayout)
        {
            int count = ReadCount(reader, 1000000, "Fixed Action activation request");
            var requests = new List<SimulationActionActivationRequestState>(count);
            for (int i = 0; i < count; i++)
            {
                if (!reader.ReadBoolean())
                    continue;
                var request = new SimulationActionActivationRequestState(
                    reader.ReadString(),
                    ReadOptionalSkill(reader),
                    ReadOptionalOperation(reader, null),
                    reader.ReadString(),
                    reader.ReadString(),
                    reader.ReadUInt64(),
                    reader.ReadUInt64(),
                    reader.ReadString(),
                    ReadTarget(reader),
                    SimulationExecutionSourceCodec.Read(reader),
                    ReadEquipmentContext(reader, equipmentLayout),
                    reader.ReadUInt64());
                if (!request.IsValid)
                    throw new InvalidDataException("Fixed Action activation request identity is invalid.");
                RequireSkillExecution(request.SkillId, request.SkillEntryOperation, installations);
                requests.Add(request);
            }
            return requests;
        }

        static void WriteActionInstances(CanonicalWriter writer, IReadOnlyList<FixedActionInstanceState> actions)
        {
            writer.WriteInt32(actions.Count);
            for (int i = 0; i < actions.Count; i++)
            {
                FixedActionInstanceState action = actions[i];
                writer.WriteBoolean(action.IsValid);
                if (!action.IsValid)
                    continue;
                writer.WriteString(action.ActionId);
                WriteOptionalSkill(writer, action.SkillId);
                WriteOptionalOperation(writer, action.SkillEntryOperation);
                writer.WriteUInt64(action.SkillExecutionGeneration);
                writer.WriteString(action.ContextId);
                writer.WriteUInt64(action.InstanceId);
                writer.WriteUInt64(action.PredictionKey);
                writer.WriteString(action.SourceInputRequestId);
                writer.WriteUInt64(action.InputSequence);
                writer.WriteUInt64(action.StartTick);
                writer.WriteString(action.TargetKey);
                WriteTarget(writer, action.TargetSnapshot);
                SimulationExecutionSourceCodec.Write(writer, action.Source);
                writer.WriteByte((byte)action.Phase);
                writer.WriteByte((byte)action.State);
                writer.WriteByte((byte)action.LastTransition);
                writer.WriteUInt64(action.LastTransitionTick);
                writer.WriteUInt64(action.LastTransitionSourceTick);
                writer.WriteString(action.Reason);
                WriteEquipmentContext(writer, action.EquipmentContext);
                writer.WriteUInt64(action.SegmentGeneration);
            }
        }

        static List<FixedActionInstanceState> ReadActionInstances(
            CanonicalReader reader,
            FixedGameplayAbilityExecutionInstallationSet installations,
            EquipmentProgramLayout equipmentLayout)
        {
            int count = ReadCount(reader, 1000000, "Fixed Action instance");
            var actions = new List<FixedActionInstanceState>(count);
            for (int i = 0; i < count; i++)
            {
                if (!reader.ReadBoolean())
                    continue;
                var action = new FixedActionInstanceState(
                    reader.ReadString(),
                    ReadOptionalSkill(reader),
                    ReadOptionalOperation(reader, null),
                    reader.ReadUInt64(),
                    reader.ReadString(),
                    reader.ReadUInt64(),
                    reader.ReadUInt64(),
                    reader.ReadString(),
                    reader.ReadUInt64(),
                    reader.ReadUInt64(),
                    reader.ReadString(),
                    ReadTarget(reader),
                    SimulationExecutionSourceCodec.Read(reader),
                    ReadEnum<SimulationActionPhase>(reader.ReadByte()),
                    ReadEnum<SimulationActionState>(reader.ReadByte()),
                    ReadEnum<SimulationActionLifecycleTransitionType>(reader.ReadByte()),
                    reader.ReadUInt64(),
                    reader.ReadUInt64(),
                    reader.ReadString(),
                    ReadEquipmentContext(reader, equipmentLayout),
                    reader.ReadUInt64());
                if (!action.IsValid)
                    throw new InvalidDataException("Fixed Action instance identity is invalid.");
                RequireSkillExecution(action.SkillId, action.SkillEntryOperation, installations);
                actions.Add(action);
            }
            return actions;
        }

        static void WriteMotionWarpStates(
            CanonicalWriter writer,
            IReadOnlyDictionary<int, FixedMotionWarpState> states)
        {
            var keys = new List<int>(states.Keys);
            keys.Sort();
            writer.WriteInt32(keys.Count);
            for (int i = 0; i < keys.Count; i++)
            {
                writer.WriteInt32(keys[i]);
                WriteMotionWarpState(writer, states[keys[i]]);
            }
        }

        static Dictionary<int, FixedMotionWarpState> ReadMotionWarpStates(
            CanonicalReader reader,
            GameplayAbilityExecutionLayout layout)
        {
            int count = ReadCount(reader, layout.MotionWarpOperationIds.Count, "Fixed MotionWarp state");
            var values = new Dictionary<int, FixedMotionWarpState>(count);
            int previous = -1;
            for (int i = 0; i < count; i++)
            {
                int operation = reader.ReadInt32();
                if (operation < 0 || operation <= previous || !layout.HasMotionWarp(new OperationHandle(operation)))
                    throw new InvalidDataException("Fixed MotionWarp operation identities are invalid or not canonically ordered.");
                FixedMotionWarpState value = ReadMotionWarpState(reader, layout);
                if (!value.Active)
                    throw new InvalidDataException("Fixed MotionWarp state is not active.");
                values.Add(operation, value);
                previous = operation;
            }
            return values;
        }

        static void WriteMotionWarpState(CanonicalWriter writer, FixedMotionWarpState value)
        {
            writer.WriteBoolean(value.Active);
            if (!value.Active)
                return;
            writer.WriteBoolean(value.Initialized);
            writer.WriteUInt64(value.PlaybackGeneration);
            WriteActionReference(writer, value.ActionInstance);
            writer.WriteVector3(value.StartBodyPosition);
            writer.WriteYaw(value.StartBodyYaw);
            writer.WriteVector3(value.SourceWindowStartPosition);
            writer.WriteScalar(value.SourceWindowStartYaw);
            writer.WriteVector3(value.ResolvedTargetPosition);
            writer.WriteYaw(value.ResolvedTargetYaw);
            writer.WriteByte((byte)value.LimitResult);
            writer.WriteVector3(value.PreviousWarpedPosition);
            writer.WriteYaw(value.PreviousWarpedYaw);
            writer.WriteScalar(value.LastPositionProgress);
            writer.WriteScalar(value.LastYawProgress);
            WriteOptionalOperation(writer, value.SourceOperation);
        }

        static FixedMotionWarpState ReadMotionWarpState(
            CanonicalReader reader,
            GameplayAbilityExecutionLayout layout)
        {
            if (!reader.ReadBoolean())
                return default;
            return new FixedMotionWarpState(
                true,
                reader.ReadBoolean(),
                reader.ReadUInt64(),
                ReadActionReference(reader, layout),
                reader.ReadVector3(),
                reader.ReadYaw(),
                reader.ReadVector3(),
                reader.ReadScalar(),
                reader.ReadVector3(),
                reader.ReadYaw(),
                ReadEnum<ProgramMotionWarpLimitResult>(reader.ReadByte()),
                reader.ReadVector3(),
                reader.ReadYaw(),
                reader.ReadScalar(),
                reader.ReadScalar(),
                ReadOptionalOperation(reader, layout));
        }

        static void WriteValue(CanonicalWriter writer, AbilityStateValue value)
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
                case ProgramStateValueKind.ActionTargetSnapshot: WriteTarget(writer, value.ActionTargetSnapshot); break;
                default: throw new InvalidDataException($"Unsupported Fixed Character state value kind '{value.Kind}'.");
            }
        }

        static AbilityStateValue ReadValue(CanonicalReader reader)
        {
            ProgramStateValueKind kind = ReadEnum<ProgramStateValueKind>(reader.ReadByte());
            return kind switch
            {
                ProgramStateValueKind.Boolean => AbilityStateValue.FromBoolean(reader.ReadBoolean()),
                ProgramStateValueKind.Int32 => AbilityStateValue.FromInt32(reader.ReadInt32()),
                ProgramStateValueKind.UInt64 => AbilityStateValue.FromUInt64(reader.ReadUInt64()),
                ProgramStateValueKind.Scalar => AbilityStateValue.FromScalar(reader.ReadScalar()),
                ProgramStateValueKind.Vector2 => AbilityStateValue.FromVector2(reader.ReadVector2()),
                ProgramStateValueKind.Vector3 => AbilityStateValue.FromVector3(reader.ReadVector3()),
                ProgramStateValueKind.Yaw => AbilityStateValue.FromYaw(reader.ReadYaw()),
                ProgramStateValueKind.Identity => AbilityStateValue.FromIdentity(reader.ReadString()),
                ProgramStateValueKind.BlackboardOwnerToken => AbilityStateValue.FromBlackboardOwnerToken(ReadBlackboardOwnerToken(reader)),
                ProgramStateValueKind.BlackboardWriteStamp => AbilityStateValue.FromBlackboardWriteStamp(ReadBlackboardWriteStamp(reader)),
                ProgramStateValueKind.ActionTargetSnapshot => AbilityStateValue.FromActionTargetSnapshot(ReadTarget(reader)),
                _ => throw new InvalidDataException($"Unsupported Fixed Character state value kind '{kind}'.")
            };
        }

        static void WriteBlackboardOwnerToken(CanonicalWriter writer, BlackboardOwnerToken value)
        {
            writer.WriteByte(value.IsValid ? (byte)value.ScopeKind : (byte)0);
            writer.WriteInt32(value.IsValid ? value.CompiledOwnerIndex : -1);
            writer.WriteUInt64(value.IsValid ? value.Generation : 0);
        }

        static BlackboardOwnerToken ReadBlackboardOwnerToken(CanonicalReader reader)
        {
            byte scope = reader.ReadByte();
            int owner = reader.ReadInt32();
            ulong generation = reader.ReadUInt64();
            if (scope == 0 && owner == -1 && generation == 0)
                return default;
            return new BlackboardOwnerToken(
                ReadEnum<ProgramScopeKind>(scope),
                owner,
                generation);
        }

        static void WriteBlackboardWriteStamp(CanonicalWriter writer, BlackboardWriteStamp value)
        {
            writer.WriteInt32(value.IsValid ? value.SourceOperation.Value : -1);
            writer.WriteUInt64(value.IsValid ? value.LogicTick : 0);
            writer.WriteUInt64(value.IsValid ? value.ActionInstanceId : 0);
            writer.WriteInt32(value.IsValid && value.TimelineOperation.IsValid ? value.TimelineOperation.Value : -1);
            writer.WriteInt32(value.IsValid && value.ClipOperation.IsValid ? value.ClipOperation.Value : -1);
            writer.WriteInt32(value.IsValid ? value.Cycle : 0);
        }

        static BlackboardWriteStamp ReadBlackboardWriteStamp(CanonicalReader reader)
        {
            int source = reader.ReadInt32();
            ulong tick = reader.ReadUInt64();
            ulong action = reader.ReadUInt64();
            int timeline = reader.ReadInt32();
            int clip = reader.ReadInt32();
            int cycle = reader.ReadInt32();
            if (source == -1 && tick == 0 && action == 0 && timeline == -1 && clip == -1 && cycle == 0)
                return default;
            return new BlackboardWriteStamp(
                new OperationHandle(source),
                tick,
                action,
                timeline < 0 ? OperationHandle.Invalid : new OperationHandle(timeline),
                clip < 0 ? OperationHandle.Invalid : new OperationHandle(clip),
                cycle);
        }

        static void WriteTarget(CanonicalWriter writer, SimulationActionTargetSnapshot target)
        {
            writer.WriteString(target.TargetId);
            writer.WriteVector3(target.Position);
            writer.WriteYaw(target.Yaw);
        }

        static SimulationActionTargetSnapshot ReadTarget(CanonicalReader reader) =>
            new SimulationActionTargetSnapshot(
                reader.ReadString(),
                reader.ReadVector3(),
                reader.ReadYaw());

        static void WriteActionReference(CanonicalWriter writer, FixedActionInstanceReference value)
        {
            writer.WriteBoolean(value.IsValid);
            if (!value.IsValid)
                return;
            writer.WriteString(value.ActionId);
            writer.WriteString(value.ContextId);
            writer.WriteUInt64(value.InstanceId);
            writer.WriteUInt64(value.PredictionKey);
            WriteOptionalSkill(writer, value.SkillId);
            WriteOptionalOperation(writer, value.SkillEntryOperation);
            writer.WriteUInt64(value.SkillExecutionGeneration);
        }

        static FixedActionInstanceReference ReadActionReference(
            CanonicalReader reader,
            GameplayAbilityExecutionLayout layout)
        {
            if (!reader.ReadBoolean())
                return default;
            return new FixedActionInstanceReference(
                reader.ReadString(),
                reader.ReadString(),
                reader.ReadUInt64(),
                reader.ReadUInt64(),
                ReadOptionalSkill(reader),
                ReadOptionalOperation(reader, layout),
                reader.ReadUInt64());
        }

        static void WriteOptionalSkill(CanonicalWriter writer, CharacterSkillId value)
        {
            writer.WriteBoolean(value.IsValid);
            if (value.IsValid)
                writer.WriteString(value.Value);
        }

        static CharacterSkillId ReadOptionalSkill(CanonicalReader reader) =>
            reader.ReadBoolean() ? new CharacterSkillId(reader.ReadString()) : default;

        static void WriteOptionalOperation(CanonicalWriter writer, OperationHandle value) =>
            writer.WriteInt32(value.IsValid ? value.Value : -1);

        static OperationHandle ReadOptionalOperation(CanonicalReader reader, GameplayAbilityExecutionLayout layout)
        {
            int value = reader.ReadInt32();
            if (value < -1 || layout != null && value >= layout.Operations.Count)
                throw new InvalidDataException("Fixed Character runtime state operation handle is invalid.");
            return value < 0 ? OperationHandle.Invalid : new OperationHandle(value);
        }

        static OperationHandle ReadRequiredOperation(CanonicalReader reader, GameplayAbilityExecutionLayout layout)
        {
            OperationHandle operation = ReadOptionalOperation(reader, layout);
            return operation.IsValid
                ? operation
                : throw new InvalidDataException("Fixed Character runtime state requires an operation handle.");
        }

        static void WriteEquipmentContext(CanonicalWriter writer, EquipmentActionContext context)
        {
            writer.WriteBoolean(context.IsValid);
            if (!context.IsValid)
                return;
            writer.WriteString(context.SlotId.Value);
            writer.WriteString(context.EquipmentId.Value);
            writer.WriteString(context.FeatureId.Value);
            writer.WriteUInt64(context.EquipmentRevision);
            writer.WriteString(context.RouteId.Value);
        }

        static EquipmentActionContext ReadEquipmentContext(CanonicalReader reader, EquipmentProgramLayout layout)
        {
            if (!reader.ReadBoolean())
                return default;
            var context = new EquipmentActionContext(
                new EquipmentSlotId(reader.ReadString()),
                new EquipmentId(reader.ReadString()),
                new EquipmentFeatureId(reader.ReadString()),
                reader.ReadUInt64(),
                new EquipmentActionRouteId(reader.ReadString()));
            if (layout == null)
                throw new InvalidDataException("Fixed Character runtime state contains an Equipment Action Context without an Equipment layout.");
            EquipmentProgramItem item = layout.RequireItem(context.EquipmentId);
            EquipmentProgramFeature feature = layout.RequireFeature(context.FeatureId);
            EquipmentProgramRoute route = layout.RequireRoute(context.RouteId);
            if (!layout.TryGetRouteImplementation(context.FeatureId, context.RouteId, out _))
                throw new InvalidDataException($"Equipment Action Context '{context}' has no Feature route implementation.");
            if (item.SlotId != context.SlotId || item.FeatureId != context.FeatureId ||
                route.OwnerSlotId != context.SlotId || feature.FeatureId != context.FeatureId)
            {
                throw new InvalidDataException($"Equipment Action Context '{context}' does not match the Equipment layout.");
            }
            return context;
        }

        static void RequireSkillExecution(
            CharacterSkillId skillId,
            OperationHandle operation,
            FixedGameplayAbilityExecutionInstallationSet installations)
        {
            if (!operation.IsValid)
                return;
            if (!skillId.IsValid)
                throw new InvalidDataException("Fixed Character runtime state operation has no Ability identity.");
            FixedGameplayAbilityExecutionInstallation installation = installations.Require(skillId);
            if (operation.Value >= installation.Layout.Operations.Count)
                throw new InvalidDataException("Fixed Character runtime state Ability operation is outside its installed Ability layout.");
        }

        static int ReadCount(CanonicalReader reader, int maximum, string label)
        {
            int count = reader.ReadInt32();
            if (count < 0 || count > maximum)
                throw new InvalidDataException($"{label} count '{count}' is invalid.");
            return count;
        }

        static T ReadEnum<T>(byte value) where T : struct
        {
            object candidate = Enum.ToObject(typeof(T), value);
            if (!Enum.IsDefined(typeof(T), candidate))
                throw new InvalidDataException($"Fixed Character runtime state enum '{typeof(T).Name}' value '{value}' is invalid.");
            return (T)candidate;
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
