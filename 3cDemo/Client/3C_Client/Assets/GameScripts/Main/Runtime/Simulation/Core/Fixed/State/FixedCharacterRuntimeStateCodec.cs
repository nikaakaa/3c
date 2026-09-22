using System;
using ThirdPersonSimulation.Fixed;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SimulationActionActivationRequestState = ThirdPersonSimulation.SimulationActionActivationRequestState<ThirdPersonSimulation.Fixed.SimulationActionTargetSnapshot>;

namespace ThirdPersonSimulation.Fixed
{
    internal static class FixedCharacterRuntimeStateCodec
    {
        const uint Magic = 0x54535243;
        const int Version = 10;
        const string HashIdentity = "fixed-character-runtime-state-hash/8";
        public const string CodecIdentity = "fixed-character-runtime-state/8";

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
            SimulationActorBinding actor)
        {
            if (bytes == null || actor == null)
                throw new ArgumentNullException(bytes == null ? nameof(bytes) : nameof(actor));
            FixedGameplayAbilityExecutionInstallationSet installations = actor.AbilityInstallations;
            GameplayContentHash expectedGameplayContentHash = new GameplayContentHash(actor.GameplayContentHash);
            CharacterGameplayEffectRuntimeBinding gameplayEffectBinding = actor.GameplayEffectRuntimeBinding;
            CharacterEquipmentRuntimeBinding equipmentBinding = actor.EquipmentRuntimeBinding;
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
            StableHash stateSchemaHash = new StableHash(reader.ReadString());
            if (!stateSchemaHash.Equals(actor.StateSchemaHash))
                throw new InvalidDataException("Fixed Character runtime state StateSchemaHash does not match the active Character Runtime.");
            ulong lastCompletedTick = reader.ReadUInt64();
            int abilityCount = ReadCount(reader, installations.Installations.Count, "Fixed Character Ability partition");
            if (abilityCount != installations.Installations.Count)
                throw new InvalidDataException("Fixed Character runtime state Ability partitions do not match the installed Ability set.");
            var abilities = new FixedAbilityRuntimeState[abilityCount];
            for (int i = 0; i < abilityCount; i++)
            {
                GameplayAbilityExecutionIdentity identity = ReadIdentity(reader);
                FixedGameplayAbilityExecutionInstallation installation = installations.Require(identity.AbilityId);
                installation.Identity.Require(identity);
                Dictionary<int, AbilityStateValue> stateValues = ReadValues(reader, installation.Layout);
                GameplayAbilityExecutionAggregate<AbilityStateValue> abilityExecutionState = ReadAbilityExecutionState(reader, installation.Layout);
                Dictionary<int, FixedMotionWarpState> motionWarpStates = ReadMotionWarpStates(reader, installation.Layout);
                abilities[i] = FixedAbilityRuntimeState.Adopt(
                    identity,
                    stateValues,
                    abilityExecutionState,
                    motionWarpStates);
            }
            EquipmentProgramLayout equipmentLayout = !installations.RequiresEquipment || equipmentBinding == null
                ? null
                : EquipmentProgramLayoutCompiler.CompileRoleStateLayout(equipmentBinding);
            SimulationActionActivationRequestState[] actionActivationRequests =
                ReadActionActivationRequests(reader, installations, equipmentLayout).ToArray();
            FixedActionInstanceState[] actionInstances =
                ReadActionInstances(reader, installations, equipmentLayout).ToArray();
            KeyValuePair<string, SimulationInputRequestState>[] inputRequests = ReadInputRequests(reader, installations);
            ulong eventSequence = reader.ReadUInt64();
            ulong actionEventSequence = reader.ReadUInt64();
            ulong handleAllocator = reader.ReadUInt64();
            CharacterControlRuntimeState controlState = reader.ReadBoolean()
                ? CharacterControlRuntimeStateCodec.Read(reader.ReadBytesSegment())
                : default;
            GameplayEffectStateAggregate gameplayEffectState = null;
            if (reader.ReadBoolean())
            {
                FixedGameplayEffectRuntimeCatalog effectCatalog = gameplayEffectBinding == null
                    ? null
                    : new FixedGameplayEffectRuntimeCatalog(gameplayEffectBinding);
                if (effectCatalog == null)
                    throw new InvalidDataException("Fixed Character runtime state contains Gameplay Effect state without a Character Effect service.");
                var effectReader = new CanonicalReader(reader.ReadBytesSegment());
                gameplayEffectState = GameplayEffectStateAggregateCodec.Read(
                    effectReader,
                    effectCatalog,
                    actor.EffectExecutionScratch);
                effectReader.RequireComplete();
            }
            EquipmentStateAggregate equipmentState = null;
            if (reader.ReadBoolean())
            {
                if (equipmentLayout == null)
                    throw new InvalidDataException("Fixed Character runtime state contains Equipment state without an installed layout.");
                var equipmentReader = new CanonicalReader(reader.ReadBytesSegment());
                equipmentState = EquipmentStateAggregateCodec.Read(equipmentReader, equipmentLayout);
                equipmentReader.RequireComplete();
            }
            int timelineSnapshotCount = ReadCount(reader, 1024, "Fixed Character Timeline snapshot");
            var timelineSnapshots = new AbilityTimelineRuntimeSnapshot[timelineSnapshotCount];
            for (int i = 0; i < timelineSnapshotCount; i++)
                timelineSnapshots[i] = ReadTimelineSnapshot(reader);
            reader.RequireComplete();
            var state = FixedCharacterRuntimeState.AdoptPrepared(
                numericProfile,
                gameplayContentHash,
                stateSchemaHash,
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
                equipmentState,
                timelineSnapshots);
            using var writer = new CanonicalWriter();
            WriteCanonical(writer, state);
            if (!writer.ContentEquals(bytes))
                throw new InvalidDataException("Fixed Character runtime state is not canonical.");
            return state;
        }

        static void WriteCanonical(CanonicalWriter writer, FixedCharacterRuntimeState state)
        {
            writer.WriteUInt32(Magic);
            writer.WriteInt32(Version);
            writer.WriteString(CodecIdentity);
            SimulationNumericProfileCodec.Write(writer, state.NumericProfile);
            writer.WriteString(state.GameplayContentHash.ToString());
            writer.WriteString(state.StateSchemaHash.ToString());
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
            writer.WriteBoolean(state.ControlState.IsValid);
            if (state.ControlState.IsValid)
                CharacterControlRuntimeStateCodec.WriteLengthPrefixed(writer, state.ControlState);
            writer.WriteBoolean(state.GameplayEffectState != null);
            if (state.GameplayEffectState != null)
            {
                long effectPrefixPosition = writer.BeginLengthPrefixedBlock();
                GameplayEffectStateAggregateCodec.Write(
                    writer,
                    state.GameplayEffectState);
                writer.EndLengthPrefixedBlock(effectPrefixPosition);
            }
            writer.WriteBoolean(state.EquipmentState != null);
            if (state.EquipmentState != null)
            {
                long equipmentPrefixPosition = writer.BeginLengthPrefixedBlock();
                EquipmentStateAggregateCodec.Write(writer, state.EquipmentState);
                writer.EndLengthPrefixedBlock(equipmentPrefixPosition);
            }
            writer.WriteInt32(state.TimelineSnapshots.Count);
            for (int i = 0; i < state.TimelineSnapshots.Count; i++)
                WriteTimelineSnapshot(writer, state.TimelineSnapshots[i]);
        }

        static void WriteTimelineSnapshot(CanonicalWriter writer, AbilityTimelineRuntimeSnapshot snapshot)
        {
            writer.WriteInt32(snapshot.RuntimeHandle);
            writer.WriteUInt64(snapshot.Generation);
            writer.WriteString(snapshot.RequestId);
            writer.WriteString(snapshot.OwnerIdentity);
            writer.WriteString(snapshot.CallIdentity);
            writer.WriteUInt64(snapshot.ExecutionInstanceId);
            writer.WriteByte((byte)snapshot.PlaybackMode);
            writer.WriteString(snapshot.ContentRevision);
            writer.WriteByte((byte)snapshot.State);
            writer.WriteInt64(snapshot.CursorTime.Raw);
            writer.WriteInt32(snapshot.Cycle);
            writer.WriteInt32(snapshot.TimeCarry);
            writer.WriteInt64(snapshot.Control.Rate.Raw);
            writer.WriteBoolean(snapshot.Control.Paused);
            writer.WriteInt32(snapshot.TreeDecisionExits.Count);
            for (int i = 0; i < snapshot.TreeDecisionExits.Count; i++)
                writer.WriteString(snapshot.TreeDecisionExits[i]);
            writer.WriteInt32(snapshot.PendingTreeDecisionExits.Count);
            for (int i = 0; i < snapshot.PendingTreeDecisionExits.Count; i++)
                writer.WriteString(snapshot.PendingTreeDecisionExits[i]);
            writer.WriteString(snapshot.SectionId);
            writer.WriteInt32(snapshot.ActiveClipIds.Count);
            for (int i = 0; i < snapshot.ActiveClipIds.Count; i++)
                writer.WriteString(snapshot.ActiveClipIds[i]);
            writer.WriteInt32(snapshot.ActiveTreeClips.Count);
            for (int i = 0; i < snapshot.ActiveTreeClips.Count; i++)
            {
                AbilityTimelineTreeClipState clip = snapshot.ActiveTreeClips[i];
                writer.WriteString(clip.ClipAuthoringId);
                writer.WriteString(clip.TreeGraphId);
                writer.WriteString(clip.TreeGraphRevision);
                writer.WriteString(clip.NodeAuthoringId);
                writer.WriteUInt64(clip.PlaybackGeneration);
                writer.WriteUInt64(clip.BranchRevision);
                writer.WriteInt32(clip.Cycle);
            }
            writer.WriteBoolean(snapshot.HasStopContext);
            writer.WriteByte((byte)snapshot.StopCause);
            writer.WriteUInt64(snapshot.StopLocalLogicTick);
            writer.WriteBoolean(snapshot.InitialBoundaryPending);
            writer.WriteString(snapshot.TimelineId);
            writer.WriteBoolean(snapshot.Loop);
            writer.WriteString(snapshot.ActionContext.ActionId);
            writer.WriteString(snapshot.ActionContext.ContextId);
            writer.WriteUInt64(snapshot.ActionContext.InstanceId);
            writer.WriteUInt64(snapshot.ActionContext.PredictionKey);
            writer.WriteString(snapshot.ActionContext.SkillId.IsValid ? snapshot.ActionContext.SkillId.Value : string.Empty);
            writer.WriteInt32(snapshot.ActionContext.SkillEntryOperation.IsValid ? snapshot.ActionContext.SkillEntryOperation.Value : -1);
            writer.WriteUInt64(snapshot.ActionContext.SkillExecutionGeneration);
            writer.WriteInt32(snapshot.InvocationSource.OperationIndex);
            writer.WriteString(snapshot.InvocationSource.GraphAuthoringId);
            writer.WriteString(snapshot.InvocationSource.NodeAuthoringId);
            writer.WriteString(snapshot.InvocationSource.GraphInvocationPath);
            writer.WriteString(snapshot.InvocationSource.OperationExecutionPath);
            writer.WriteUInt64(snapshot.InvocationSource.InvocationGeneration);
            writer.WriteUInt64(snapshot.InputSequence);
            writer.WriteUInt64(snapshot.StartTick.Value);
        }

        static AbilityTimelineRuntimeSnapshot ReadTimelineSnapshot(CanonicalReader reader)
        {
            int runtimeHandle = reader.ReadInt32();
            ulong generation = reader.ReadUInt64();
            string requestId = reader.ReadString();
            string ownerIdentity = reader.ReadString();
            string callIdentity = reader.ReadString();
            ulong executionInstanceId = reader.ReadUInt64();
            AbilityTimelineSnapshotMode playbackMode = ReadTimelineSnapshotMode(reader.ReadByte());
            string contentRevision = reader.ReadString();
            AbilityTimelineSnapshotState state = ReadTimelineSnapshotState(reader.ReadByte());
            FixedScalar cursorTime = FixedScalar.FromRaw(reader.ReadInt64());
            int cycle = reader.ReadInt32();
            int timeCarry = reader.ReadInt32();
            var control = new AbilityTimelinePlaybackControl(ThirdPersonSimulation.Fixed.FixedScalar.FromRaw(reader.ReadInt64()), reader.ReadBoolean());
            int treeDecisionExitCount = ReadCount(reader, 1024, "timeline tree decision exits");
            var treeDecisionExits = new string[treeDecisionExitCount];
            for (int i = 0; i < treeDecisionExitCount; i++)
                treeDecisionExits[i] = reader.ReadString();
            int pendingTreeDecisionExitCount = ReadCount(reader, 1024, "Fixed Character Timeline pending tree decision exits");
            var pendingTreeDecisionExits = new string[pendingTreeDecisionExitCount];
            for (int i = 0; i < pendingTreeDecisionExitCount; i++)
                pendingTreeDecisionExits[i] = reader.ReadString();
            string sectionId = reader.ReadString();
            int clipCount = ReadCount(reader, 1024, "Fixed Character Timeline active clips");
            var clips = new string[clipCount];
            for (int i = 0; i < clipCount; i++)
                clips[i] = reader.ReadString();
            int treeClipCount = ReadCount(reader, reader.Remaining / sizeof(int), "Timeline active TreeClip invocations");
            var treeClips = treeClipCount == 0 ? Array.Empty<AbilityTimelineTreeClipState>() : new AbilityTimelineTreeClipState[treeClipCount];
            for (int i = 0; i < treeClipCount; i++)
                treeClips[i] = new AbilityTimelineTreeClipState(
                    reader.ReadString(),
                    reader.ReadString(),
                    reader.ReadString(),
                    reader.ReadString(),
                    reader.ReadUInt64(),
                    reader.ReadUInt64(),
                    reader.ReadInt32());
            bool hasStopContext = reader.ReadBoolean();
            AbilityTimelineSnapshotStopCause stopCause = ReadTimelineSnapshotStopCause(reader.ReadByte());
            ulong stopLocalLogicTick = reader.ReadUInt64();
            bool initialBoundaryPending = reader.ReadBoolean();
            string timelineId = reader.ReadString();
            bool loop = reader.ReadBoolean();
            string actionId = reader.ReadString();
            string contextId = reader.ReadString();
            ulong actionInstanceId = reader.ReadUInt64();
            ulong predictionKey = reader.ReadUInt64();
            string skillIdValue = reader.ReadString();
            int skillEntryOperationValue = reader.ReadInt32();
            ulong skillExecutionGeneration = reader.ReadUInt64();
            int invocationOperationIndex = reader.ReadInt32();
            string invocationGraphAuthoringId = reader.ReadString();
            string invocationNodeAuthoringId = reader.ReadString();
            string invocationPath = reader.ReadString();
            string operationExecutionPath = reader.ReadString();
            ulong invocationGeneration = reader.ReadUInt64();
            ulong inputSequence = reader.ReadUInt64();
            ulong startTickValue = reader.ReadUInt64();
            if (runtimeHandle == 0 || generation == 0 || executionInstanceId == 0 || startTickValue == 0)
                throw new InvalidDataException("Fixed Character runtime state Timeline snapshot identity is invalid.");
            CharacterSkillId skillId = skillIdValue.Length == 0 ? default : new CharacterSkillId(skillIdValue);
            OperationHandle skillEntryOperation = skillEntryOperationValue < 0 ? OperationHandle.Invalid : new OperationHandle(skillEntryOperationValue);
            var actionContext = new TimelineActionContextIdentity(
                actionId,
                contextId,
                actionInstanceId,
                predictionKey,
                skillId,
                skillEntryOperation,
                skillExecutionGeneration);
            var invocationSource = new AbilityTimelineInvocationSource(
                invocationOperationIndex,
                invocationGraphAuthoringId,
                invocationNodeAuthoringId,
                invocationPath,
                operationExecutionPath,
                invocationGeneration);
            return new AbilityTimelineRuntimeSnapshot(
                runtimeHandle,
                generation,
                requestId,
                ownerIdentity,
                callIdentity,
                executionInstanceId,
                playbackMode,
                contentRevision,
                state,
                cursorTime,
                cycle,
                timeCarry,
                control,
                TimelineSnapshotItems<string>.CopyFrom(treeDecisionExits),
                TimelineSnapshotItems<string>.CopyFrom(pendingTreeDecisionExits),
                sectionId,
                TimelineSnapshotItems<string>.CopyFrom(clips),
                TimelineSnapshotItems<AbilityTimelineTreeClipState>.CopyFrom(treeClips),
                hasStopContext,
                stopCause,
                stopLocalLogicTick,
                initialBoundaryPending,
                timelineId,
                loop,
                actionContext,
                invocationSource,
                inputSequence,
                new SimulationTick(startTickValue));
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
            KeyValuePair<string, SimulationInputRequestState>[] requests)
        {
            writer.WriteInt32(requests.Length);
            for (int i = 0; i < requests.Length; i++)
            {
                writer.WriteString(requests[i].Key);
                SimulationInputRequestStateCodec.Write(writer, requests[i].Value);
            }
        }

        static KeyValuePair<string, SimulationInputRequestState>[] ReadInputRequests(
            CanonicalReader reader,
            FixedGameplayAbilityExecutionInstallationSet installations)
        {
            int count = ReadCount(reader, 1000000, "Fixed Character Input request");
            if (count == 0)
                return Array.Empty<KeyValuePair<string, SimulationInputRequestState>>();
            var requests = new KeyValuePair<string, SimulationInputRequestState>[count];
            string previous = null;
            for (int i = 0; i < count; i++)
            {
                string requestId = SimulationIdentity.Require(reader.ReadString(), "InputRequestId");
                if (previous != null && string.CompareOrdinal(previous, requestId) >= 0 ||
                    !ContainsInputRequest(installations, requestId))
                    throw new InvalidDataException("Fixed Character Input request identities are invalid or not canonically ordered.");
                SimulationInputRequestState value = SimulationInputRequestStateCodec.Read(reader);
                if (value.IsValid && !string.Equals(value.RequestId, requestId, StringComparison.Ordinal))
                    throw new InvalidDataException("Fixed Character Input request state key does not match its value.");
                requests[i] = new(requestId, value);
                previous = requestId;
            }
            return requests;
        }

        static bool ContainsInputRequest(
            FixedGameplayAbilityExecutionInstallationSet installations,
            string requestId)
        {
            for (int i = 0; i < installations.Installations.Count; i++)
            {
                if (installations.Installations[i].Layout.HasInputRequest(requestId))
                    return true;
            }
            return false;
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
                    ReadActionPhase(reader.ReadByte()),
                    ReadActionState(reader.ReadByte()),
                    ReadActionTransition(reader.ReadByte()),
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
                ReadMotionWarpLimitResult(reader.ReadByte()),
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
            ProgramStateValueKind kind = (ProgramStateValueKind)reader.ReadByte();
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
                _ => throw new InvalidDataException($"Fixed Character runtime state enum '{nameof(ProgramStateValueKind)}' value '{(byte)kind}' is invalid.")
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
                ReadProgramScopeKind(scope),
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

        static SimulationActionPhase ReadActionPhase(byte value)
        {
            var result = (SimulationActionPhase)value;
            if (result is not (SimulationActionPhase.Startup or
                SimulationActionPhase.Active or
                SimulationActionPhase.Recovery or
                SimulationActionPhase.Cancel or
                SimulationActionPhase.Ended))
                throw new InvalidDataException($"Fixed Character runtime state enum '{nameof(SimulationActionPhase)}' value '{value}' is invalid.");
            return result;
        }

        static SimulationActionState ReadActionState(byte value)
        {
            var result = (SimulationActionState)value;
            if (result is not (SimulationActionState.Requested or
                SimulationActionState.Predicted or
                SimulationActionState.Confirmed or
                SimulationActionState.Rejected or
                SimulationActionState.Cancelled or
                SimulationActionState.Interrupted or
                SimulationActionState.Aborted or
                SimulationActionState.Ended or
                SimulationActionState.Corrected))
                throw new InvalidDataException($"Fixed Character runtime state enum '{nameof(SimulationActionState)}' value '{value}' is invalid.");
            return result;
        }

        static SimulationActionLifecycleTransitionType ReadActionTransition(byte value)
        {
            var result = (SimulationActionLifecycleTransitionType)value;
            if (result is not (SimulationActionLifecycleTransitionType.None or
                SimulationActionLifecycleTransitionType.Confirm or
                SimulationActionLifecycleTransitionType.Complete or
                SimulationActionLifecycleTransitionType.Cancel or
                SimulationActionLifecycleTransitionType.Interrupt or
                SimulationActionLifecycleTransitionType.Reject or
                SimulationActionLifecycleTransitionType.Correct or
                SimulationActionLifecycleTransitionType.Abort))
                throw new InvalidDataException($"Fixed Character runtime state enum '{nameof(SimulationActionLifecycleTransitionType)}' value '{value}' is invalid.");
            return result;
        }

        static ProgramMotionWarpLimitResult ReadMotionWarpLimitResult(byte value)
        {
            var result = (ProgramMotionWarpLimitResult)value;
            if (result is not (ProgramMotionWarpLimitResult.Applied or ProgramMotionWarpLimitResult.AppliedClamped or
                ProgramMotionWarpLimitResult.PreservedByLimitPolicy))
                throw new InvalidDataException($"Fixed Character runtime state enum '{nameof(ProgramMotionWarpLimitResult)}' value '{value}' is invalid.");
            return result;
        }

        static AbilityTimelineSnapshotMode ReadTimelineSnapshotMode(byte value)
        {
            if (value > (byte)AbilityTimelineSnapshotMode.Loop)
                throw new InvalidDataException($"Fixed Character runtime state enum '{nameof(AbilityTimelineSnapshotMode)}' value '{value}' is invalid.");
            return (AbilityTimelineSnapshotMode)value;
        }

        static AbilityTimelineSnapshotState ReadTimelineSnapshotState(byte value)
        {
            if (value > (byte)AbilityTimelineSnapshotState.Disposed)
                throw new InvalidDataException($"Fixed Character runtime state enum '{nameof(AbilityTimelineSnapshotState)}' value '{value}' is invalid.");
            return (AbilityTimelineSnapshotState)value;
        }

        static AbilityTimelineSnapshotStopCause ReadTimelineSnapshotStopCause(byte value)
        {
            if (value > (byte)AbilityTimelineSnapshotStopCause.Shutdown)
                throw new InvalidDataException($"Fixed Character runtime state enum '{nameof(AbilityTimelineSnapshotStopCause)}' value '{value}' is invalid.");
            return (AbilityTimelineSnapshotStopCause)value;
        }

        static ProgramScopeKind ReadProgramScopeKind(byte value)
        {
            if (value < (byte)ProgramScopeKind.Character || value > (byte)ProgramScopeKind.Frame)
                throw new InvalidDataException($"Fixed Character runtime state enum '{nameof(ProgramScopeKind)}' value '{value}' is invalid.");
            return (ProgramScopeKind)value;
        }

    }
}
