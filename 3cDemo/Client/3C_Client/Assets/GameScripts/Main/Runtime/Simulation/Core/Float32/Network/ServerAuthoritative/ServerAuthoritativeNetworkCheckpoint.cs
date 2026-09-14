using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;

namespace ThirdPersonSimulation.ServerAuthoritative
{
    public sealed class NetworkCheckpointLayout
    {
        const int SchemaVersion = 16;
        readonly CharacterSimulationProgram m_Program;
        readonly ProgramExecutionLayout m_ExecutionLayout;

        public NetworkCheckpointLayout(CharacterSimulationProgram program)
        {
            m_Program = program ?? throw new ArgumentNullException(nameof(program));
            m_ExecutionLayout = ProgramExecutionLayout.GetOrCreate(program);
            using var writer = new CanonicalWriter();
            writer.WriteString("server-authoritative-network-checkpoint-layout");
            writer.WriteInt32(SchemaVersion);
            writer.WriteString(CharacterSimulationStateCodec.CodecIdentity);
            writer.WriteString(CharacterControlRuntimeStateCodec.CodecIdentity);
            writer.WriteString(program.ProgramHash.ToString());
            writer.WriteString(program.LayoutHash.ToString());
            writer.WriteInt32(program.StateSlots.Count);
            for (int i = 0; i < program.StateSlots.Count; i++)
            {
                ProgramStateSlot slot = program.StateSlots[i];
                writer.WriteInt32(slot.Index);
                writer.WriteByte((byte)slot.ValueKind);
            }
            LayoutIdentity = SimulationCanonicalPayloadHash.Compute(writer.ToArray());
        }

        public CharacterSimulationProgram Program => m_Program;
        internal ProgramExecutionLayout ExecutionLayout => m_ExecutionLayout;
        public ProgramHash ProgramHash => m_Program.ProgramHash;
        public LayoutHash ProgramLayoutHash => m_Program.LayoutHash;
        public string StateCodecIdentity => CharacterSimulationStateCodec.CodecIdentity;
        public StableHash LayoutIdentity { get; }
        public int SlotCount => m_Program.StateSlots.Count;

        public void Require(NetworkCheckpoint checkpoint)
        {
            if (checkpoint == null)
                throw new ArgumentNullException(nameof(checkpoint));
            if (!checkpoint.Baseline.ProgramHash.Equals(ProgramHash) ||
                !checkpoint.Baseline.LayoutHash.Equals(ProgramLayoutHash) ||
                !string.Equals(checkpoint.Baseline.StateCodecIdentity, StateCodecIdentity, StringComparison.Ordinal) ||
                checkpoint.Values.Count != SlotCount)
            {
                throw new InvalidDataException("Network checkpoint does not match the locked Program layout.");
            }
        }
    }

    public sealed class NetworkCheckpoint
    {
        readonly ReadOnlyCollection<byte[]> m_Values;
        readonly byte[] m_ControlStateBytes;
        readonly ulong m_EventSequence;
        readonly ulong m_ActionEventSequence;
        readonly ulong m_HandleAllocator;
        readonly byte[] m_AbilityExecutionStateBytes;
        readonly byte[] m_InputRequestStateBytes;
        readonly byte[] m_ActionActivationRequestStateBytes;
        readonly byte[] m_ActionInstanceStateBytes;
        readonly byte[] m_TimelineRetentionStateBytes;
        readonly byte[] m_MotionWarpStateBytes;
        readonly byte[] m_GameplayEffectStateBytes;
        readonly byte[] m_EquipmentStateBytes;

        internal NetworkCheckpoint(
            AuthoritativeActorBaseline baseline,
            IReadOnlyList<byte[]> values,
            byte[] controlStateBytes,
            ulong eventSequence,
            ulong actionEventSequence,
            ulong handleAllocator,
            byte[] abilityExecutionStateBytes,
            byte[] inputRequestStateBytes,
            byte[] actionActivationRequestStateBytes,
            byte[] actionInstanceStateBytes,
            byte[] timelineRetentionStateBytes,
            byte[] motionWarpStateBytes,
            byte[] gameplayEffectStateBytes,
            byte[] equipmentStateBytes)
        {
            Baseline = baseline ?? throw new ArgumentNullException(nameof(baseline));
            if (values == null || values.Count == 0)
                throw new ArgumentException("Network checkpoint values are missing.", nameof(values));
            var copied = new byte[values.Count][];
            for (int i = 0; i < copied.Length; i++)
                copied[i] = values[i] == null ? throw new ArgumentException("Network checkpoint contains an empty value.", nameof(values)) : (byte[])values[i].Clone();
            m_Values = Array.AsReadOnly(copied);
            m_ControlStateBytes = controlStateBytes == null || controlStateBytes.Length == 0
                ? throw new ArgumentException("Network checkpoint Control state is missing.", nameof(controlStateBytes))
                : (byte[])controlStateBytes.Clone();
            m_EventSequence = eventSequence;
            m_ActionEventSequence = actionEventSequence;
            m_HandleAllocator = handleAllocator;
            m_AbilityExecutionStateBytes = abilityExecutionStateBytes == null || abilityExecutionStateBytes.Length == 0
                ? throw new ArgumentException("Network checkpoint Ability execution state is missing.", nameof(abilityExecutionStateBytes))
                : (byte[])abilityExecutionStateBytes.Clone();
            m_InputRequestStateBytes = inputRequestStateBytes == null || inputRequestStateBytes.Length == 0
                ? throw new ArgumentException("Network checkpoint Input request state is missing.", nameof(inputRequestStateBytes))
                : (byte[])inputRequestStateBytes.Clone();
            m_ActionActivationRequestStateBytes = actionActivationRequestStateBytes == null || actionActivationRequestStateBytes.Length == 0
                ? throw new ArgumentException("Network checkpoint Action activation request state is missing.", nameof(actionActivationRequestStateBytes))
                : (byte[])actionActivationRequestStateBytes.Clone();
            m_ActionInstanceStateBytes = actionInstanceStateBytes == null || actionInstanceStateBytes.Length == 0
                ? throw new ArgumentException("Network checkpoint Action instance state is missing.", nameof(actionInstanceStateBytes))
                : (byte[])actionInstanceStateBytes.Clone();
            m_TimelineRetentionStateBytes = timelineRetentionStateBytes == null || timelineRetentionStateBytes.Length == 0
                ? throw new ArgumentException("Network checkpoint Timeline retention state is missing.", nameof(timelineRetentionStateBytes))
                : (byte[])timelineRetentionStateBytes.Clone();
            m_MotionWarpStateBytes = motionWarpStateBytes == null || motionWarpStateBytes.Length == 0
                ? throw new ArgumentException("Network checkpoint MotionWarp state is missing.", nameof(motionWarpStateBytes))
                : (byte[])motionWarpStateBytes.Clone();
            m_GameplayEffectStateBytes = gameplayEffectStateBytes == null ? null : (byte[])gameplayEffectStateBytes.Clone();
            m_EquipmentStateBytes = equipmentStateBytes == null ? null : (byte[])equipmentStateBytes.Clone();
            CheckpointHash = ComputeHash(baseline, copied, m_ControlStateBytes, m_EventSequence, m_ActionEventSequence, m_HandleAllocator, m_AbilityExecutionStateBytes, m_InputRequestStateBytes, m_ActionActivationRequestStateBytes, m_ActionInstanceStateBytes, m_TimelineRetentionStateBytes, m_MotionWarpStateBytes, m_GameplayEffectStateBytes, m_EquipmentStateBytes);
        }

        public AuthoritativeActorBaseline Baseline { get; }
        public IReadOnlyList<byte[]> Values => m_Values;
        internal byte[] ControlStateBytes => m_ControlStateBytes;
        internal ulong EventSequence => m_EventSequence;
        internal ulong ActionEventSequence => m_ActionEventSequence;
        internal ulong HandleAllocator => m_HandleAllocator;
        internal byte[] AbilityExecutionStateBytes => m_AbilityExecutionStateBytes;
        internal byte[] InputRequestStateBytes => m_InputRequestStateBytes;
        internal byte[] ActionActivationRequestStateBytes => m_ActionActivationRequestStateBytes;
        internal byte[] ActionInstanceStateBytes => m_ActionInstanceStateBytes;
        internal byte[] TimelineRetentionStateBytes => m_TimelineRetentionStateBytes;
        internal byte[] MotionWarpStateBytes => m_MotionWarpStateBytes;
        internal byte[] GameplayEffectStateBytes => m_GameplayEffectStateBytes;
        internal byte[] EquipmentStateBytes => m_EquipmentStateBytes;
        public StableHash CheckpointHash { get; }

        static StableHash ComputeHash(
            AuthoritativeActorBaseline baseline,
            IReadOnlyList<byte[]> values,
            byte[] controlStateBytes,
            ulong eventSequence,
            ulong actionEventSequence,
            ulong handleAllocator,
            byte[] abilityExecutionStateBytes,
            byte[] inputRequestStateBytes,
            byte[] actionActivationRequestStateBytes,
            byte[] actionInstanceStateBytes,
            byte[] timelineRetentionStateBytes,
            byte[] motionWarpStateBytes,
            byte[] gameplayEffectStateBytes,
            byte[] equipmentStateBytes)
        {
            using var writer = new CanonicalWriter();
            writer.WriteString("server-authoritative-network-checkpoint/13");
            writer.WriteString(baseline.ActorId.Value);
            writer.WriteUInt64(baseline.AuthorityTick.Value);
            writer.WriteString(baseline.StateCodecIdentity);
            writer.WriteString(baseline.StateHash.ToString());
            writer.WriteString(baseline.BodyHash.ToString());
            writer.WriteUInt64(baseline.ConfirmedInputSequence);
            writer.WriteUInt64(baseline.ConfirmedEventHorizon.Sequence);
            writer.WriteInt32(values.Count);
            for (int i = 0; i < values.Count; i++)
                writer.WriteBytes(values[i]);
            writer.WriteBytes(controlStateBytes);
            writer.WriteUInt64(eventSequence);
            writer.WriteUInt64(actionEventSequence);
            writer.WriteUInt64(handleAllocator);
            writer.WriteBytes(abilityExecutionStateBytes);
            writer.WriteBytes(inputRequestStateBytes);
            writer.WriteBytes(actionActivationRequestStateBytes);
            writer.WriteBytes(actionInstanceStateBytes);
            writer.WriteBytes(timelineRetentionStateBytes);
            writer.WriteBytes(motionWarpStateBytes);
            writer.WriteBoolean(gameplayEffectStateBytes != null);
            if (gameplayEffectStateBytes != null)
                writer.WriteBytes(gameplayEffectStateBytes);
            writer.WriteBoolean(equipmentStateBytes != null);
            if (equipmentStateBytes != null)
                writer.WriteBytes(equipmentStateBytes);
            return SimulationCanonicalPayloadHash.Compute(writer.ToArray());
        }
    }

    public static class NetworkCheckpointCodec
    {
        const uint FullMagic = 0x50434E53;
        const uint DeltaMagic = 0x44434E53;
        const int FullVersion = 17;
        const int DeltaVersion = 20;
        const string PresentationChannel = "Presentation";

        public static NetworkCheckpoint Capture(NetworkCheckpointLayout layout, AuthoritativeActorBaseline baseline)
        {
            if (layout == null)
                throw new ArgumentNullException(nameof(layout));
            if (baseline == null)
                throw new ArgumentNullException(nameof(baseline));
            if (!baseline.ProgramHash.Equals(layout.ProgramHash) ||
                !baseline.LayoutHash.Equals(layout.ProgramLayoutHash) ||
                !string.Equals(baseline.StateCodecIdentity, layout.StateCodecIdentity, StringComparison.Ordinal))
                throw new InvalidDataException("Authority baseline does not match the Network Checkpoint layout.");
            CharacterSimulationState state = CharacterSimulationStateCodec.Read(baseline.CopyCharacterStateBytes(), layout.Program);
            if (state.LastCompletedTick != baseline.AuthorityTick.Value || !CharacterSimulationStateCodec.ComputeHash(state).Equals(baseline.StateHash))
                throw new InvalidDataException("Authority baseline Character state does not match its Tick or hash.");
            return new NetworkCheckpoint(
                baseline,
                EncodeValues(layout, state),
                CharacterControlRuntimeStateCodec.Write(state.ControlState),
                state.EventSequence,
                state.ActionEventSequence,
                state.HandleAllocator,
                CharacterSimulationStateCodec.WriteAbilityExecutionState(state.AbilityExecutionState, layout.ExecutionLayout),
                CharacterSimulationStateCodec.WriteInputRequests(state.InputRequests, layout.ExecutionLayout.InputRequestIds),
                CharacterSimulationStateCodec.WriteActionActivationRequests(state.ActionActivationRequests, layout.ExecutionLayout),
                CharacterSimulationStateCodec.WriteActionInstances(state.ActionInstances, layout.ExecutionLayout),
                CharacterSimulationStateCodec.WriteTimelineRetentions(state.TimelineRetainedActionContexts, layout.ExecutionLayout),
                CharacterSimulationStateCodec.WriteMotionWarpStates(state.MotionWarpStates, layout.ExecutionLayout),
                EncodeGameplayEffectState(layout, state),
                EncodeEquipmentState(state));
        }

        public static byte[] WriteFull(NetworkCheckpointLayout layout, NetworkCheckpoint checkpoint)
        {
            layout.Require(checkpoint);
            AuthoritativeActorBaseline baseline = checkpoint.Baseline;
            using var writer = new CanonicalWriter();
            writer.WriteUInt32(FullMagic);
            writer.WriteInt32(FullVersion);
            writer.WriteString(layout.LayoutIdentity.ToString());
            writer.WriteString(layout.ProgramHash.ToString());
            writer.WriteString(layout.ProgramLayoutHash.ToString());
            writer.WriteString(baseline.StateCodecIdentity);
            writer.WriteString(baseline.ActorId.Value);
            writer.WriteUInt64(baseline.AuthorityTick.Value);
            writer.WriteString(baseline.StateHash.ToString());
            writer.WriteString(baseline.WorldRevision.Value);
            writer.WriteString(baseline.SolverId.Value);
            writer.WriteString(baseline.SolverVersion);
            writer.WriteUInt64((ulong)baseline.SolverCapabilities);
            WriteBody(writer, baseline.Body);
            writer.WriteUInt64(baseline.ConfirmedInputSequence);
            WriteHorizon(writer, baseline.ConfirmedEventHorizon);
            writer.WriteInt32(checkpoint.Values.Count);
            for (int i = 0; i < checkpoint.Values.Count; i++)
                writer.WriteBytes(checkpoint.Values[i]);
            writer.WriteBytes(checkpoint.ControlStateBytes);
            writer.WriteUInt64(checkpoint.EventSequence);
            writer.WriteUInt64(checkpoint.ActionEventSequence);
            writer.WriteUInt64(checkpoint.HandleAllocator);
            writer.WriteBytes(checkpoint.AbilityExecutionStateBytes);
            writer.WriteBytes(checkpoint.InputRequestStateBytes);
            writer.WriteBytes(checkpoint.ActionActivationRequestStateBytes);
            writer.WriteBytes(checkpoint.ActionInstanceStateBytes);
            writer.WriteBytes(checkpoint.TimelineRetentionStateBytes);
            writer.WriteBytes(checkpoint.MotionWarpStateBytes);
            WriteOptionalBytes(writer, checkpoint.GameplayEffectStateBytes);
            WriteOptionalBytes(writer, checkpoint.EquipmentStateBytes);
            writer.WriteString(checkpoint.CheckpointHash.ToString());
            return writer.ToArray();
        }

        public static NetworkCheckpoint ReadFull(NetworkCheckpointLayout layout, byte[] payload)
        {
            if (layout == null)
                throw new ArgumentNullException(nameof(layout));
            var reader = new CanonicalReader(payload ?? throw new ArgumentNullException(nameof(payload)));
            if (reader.ReadUInt32() != FullMagic || reader.ReadInt32() != FullVersion ||
                !string.Equals(reader.ReadString(), layout.LayoutIdentity.ToString(), StringComparison.Ordinal) ||
                !string.Equals(reader.ReadString(), layout.ProgramHash.ToString(), StringComparison.Ordinal) ||
                !string.Equals(reader.ReadString(), layout.ProgramLayoutHash.ToString(), StringComparison.Ordinal))
            {
                throw new InvalidDataException("Full Network Checkpoint identity is invalid.");
            }
            string stateCodecIdentity = reader.ReadString();
            if (!string.Equals(stateCodecIdentity, layout.StateCodecIdentity, StringComparison.Ordinal))
                throw new InvalidDataException("Full Network Checkpoint state codec identity is invalid.");
            var actorId = new ActorId(reader.ReadString());
            var tick = new SimulationTick(reader.ReadUInt64());
            var stateHash = new CharacterStateHash(new StableHash(reader.ReadString()));
            var worldRevision = new WorldRevision(reader.ReadString());
            var solverId = new SolverImplementationId(reader.ReadString());
            string solverVersion = reader.ReadString();
            var capabilities = (WorldCapability)reader.ReadUInt64();
            WorldBodyState body = ReadBody(reader, actorId);
            ulong inputSequence = reader.ReadUInt64();
            ServerAuthoritativeEventHorizon horizon = ReadHorizon(reader);
            byte[][] values = ReadValues(reader, layout.SlotCount);
            byte[] controlStateBytes = reader.ReadBytes();
            ulong eventSequence = reader.ReadUInt64();
            ulong actionEventSequence = reader.ReadUInt64();
            ulong handleAllocator = reader.ReadUInt64();
            byte[] abilityExecutionStateBytes = reader.ReadBytes();
            byte[] inputRequestStateBytes = reader.ReadBytes();
            byte[] actionActivationRequestStateBytes = reader.ReadBytes();
            byte[] actionInstanceStateBytes = reader.ReadBytes();
            byte[] timelineRetentionStateBytes = reader.ReadBytes();
            byte[] motionWarpStateBytes = reader.ReadBytes();
            byte[] gameplayEffectStateBytes = ReadOptionalBytes(reader);
            byte[] equipmentStateBytes = ReadOptionalBytes(reader);
            StableHash expectedCheckpointHash = new StableHash(reader.ReadString());
            reader.RequireComplete();
            AuthoritativeActorBaseline baseline = BuildBaseline(
                layout,
                actorId,
                tick,
                stateHash,
                worldRevision,
                solverId,
                solverVersion,
                capabilities,
                body,
                inputSequence,
                horizon,
                values,
                stateCodecIdentity,
                controlStateBytes,
                eventSequence,
                actionEventSequence,
                handleAllocator,
                abilityExecutionStateBytes,
                inputRequestStateBytes,
                actionActivationRequestStateBytes,
                actionInstanceStateBytes,
                timelineRetentionStateBytes,
                motionWarpStateBytes,
                gameplayEffectStateBytes,
                equipmentStateBytes);
            var checkpoint = new NetworkCheckpoint(baseline, values, controlStateBytes, eventSequence, actionEventSequence, handleAllocator, abilityExecutionStateBytes, inputRequestStateBytes, actionActivationRequestStateBytes, actionInstanceStateBytes, timelineRetentionStateBytes, motionWarpStateBytes, gameplayEffectStateBytes, equipmentStateBytes);
            if (!checkpoint.CheckpointHash.Equals(expectedCheckpointHash))
                throw new InvalidDataException("Full Network Checkpoint hash is invalid.");
            return checkpoint;
        }

        public static byte[] WriteDelta(
            NetworkCheckpointLayout layout,
            NetworkCheckpoint baseline,
            NetworkCheckpoint target,
            RemotePresentationBatch remote)
        {
            layout.Require(baseline);
            layout.Require(target);
            if (baseline.Baseline.ActorId != target.Baseline.ActorId || target.Baseline.AuthorityTick.CompareTo(baseline.Baseline.AuthorityTick) <= 0)
                throw new InvalidDataException("Network Checkpoint delta endpoints are invalid.");
            if (remote == null || remote.BodySamples.Count != 1)
                throw new InvalidDataException("Routine Network Checkpoint delta requires one remote body sample.");
            byte[] bitset = new byte[(layout.SlotCount + 7) / 8];
            var changed = new List<byte[]>();
            for (int i = 0; i < layout.SlotCount; i++)
            {
                byte[] targetValue = target.Values[i];
                if (targetValue == null || targetValue.Length == 0 || targetValue[0] != (byte)layout.Program.StateSlots[i].ValueKind)
                    throw new InvalidDataException($"Network Checkpoint slot '{i}' value kind is invalid.");
                if (Equal(baseline.Values[i], target.Values[i]))
                    continue;
                bitset[i >> 3] |= (byte)(1 << (i & 7));
                changed.Add(targetValue);
            }
            using var writer = new CanonicalWriter();
            writer.WriteUInt32(DeltaMagic);
            writer.WriteInt32(DeltaVersion);
            writer.WriteString(layout.StateCodecIdentity);
            WriteHash(writer, target.Baseline.StateHash.Value);
            WriteCompactBody(writer, target.Baseline.Body);
            WriteDeltaHorizon(writer, baseline.Baseline.ConfirmedEventHorizon, target.Baseline.ConfirmedEventHorizon);
            writer.WriteRawBytes(bitset, 0, bitset.Length);
            if (changed.Count > ushort.MaxValue)
                throw new InvalidDataException("Network Checkpoint delta changed value count exceeds its wire boundary.");
            writer.WriteUInt16((ushort)changed.Count);
            for (int i = 0; i < changed.Count; i++)
            {
                byte[] value = changed[i];
                int payloadLength = value.Length - 1;
                if (payloadLength < 0 || payloadLength > ushort.MaxValue)
                    throw new InvalidDataException("Network Checkpoint changed value exceeds its wire boundary.");
                writer.WriteUInt16((ushort)payloadLength);
                writer.WriteRawBytes(value, 1, payloadLength);
            }
            writer.WriteBytes(target.ControlStateBytes);
            writer.WriteUInt64(target.EventSequence);
            writer.WriteUInt64(target.ActionEventSequence);
            writer.WriteUInt64(target.HandleAllocator);
            writer.WriteBytes(target.AbilityExecutionStateBytes);
            writer.WriteBytes(target.InputRequestStateBytes);
            writer.WriteBytes(target.ActionActivationRequestStateBytes);
            writer.WriteBytes(target.ActionInstanceStateBytes);
            writer.WriteBytes(target.TimelineRetentionStateBytes);
            writer.WriteBytes(target.MotionWarpStateBytes);
            WriteOptionalBytes(writer, target.GameplayEffectStateBytes);
            WriteOptionalBytes(writer, target.EquipmentStateBytes);
            WriteCompactRemote(writer, layout, target.Baseline.AuthorityTick, remote);
            return writer.ToArray();
        }

        public static NetworkCheckpoint ReadDelta(
            NetworkCheckpointLayout layout,
            NetworkCheckpoint baseline,
            SimulationTick authorityTick,
            ulong confirmedInputSequence,
            ulong confirmedEventHorizon,
            ActorId remoteActor,
            byte[] payload,
            out RemotePresentationBatch remote)
        {
            layout.Require(baseline);
            if (!authorityTick.IsValid || !remoteActor.IsValid)
                throw new InvalidDataException("Network Checkpoint delta route identity is invalid.");
            var reader = new CanonicalReader(payload ?? throw new ArgumentNullException(nameof(payload)));
            if (reader.ReadUInt32() != DeltaMagic || reader.ReadInt32() != DeltaVersion)
                throw new InvalidDataException("Network Checkpoint delta schema is invalid.");
            if (!string.Equals(reader.ReadString(), layout.StateCodecIdentity, StringComparison.Ordinal))
                throw new InvalidDataException("Network Checkpoint delta state codec identity is invalid.");
            var stateHash = new CharacterStateHash(ReadHash(reader));
            WorldBodyState body = ReadCompactBody(reader, baseline.Baseline.ActorId);
            ServerAuthoritativeEventHorizon horizon = ReadDeltaHorizon(reader, baseline.Baseline.ConfirmedEventHorizon, confirmedEventHorizon);
            byte[] bitset = reader.ReadRawBytes((layout.SlotCount + 7) / 8);
            int changedCount = reader.ReadUInt16();
            if (changedCount < 0 || changedCount > layout.SlotCount)
                throw new InvalidDataException("Network Checkpoint delta changed value count is invalid.");
            var values = new byte[layout.SlotCount][];
            int readChanged = 0;
            for (int i = 0; i < values.Length; i++)
            {
                if ((bitset[i >> 3] & 1 << (i & 7)) != 0)
                {
                    int payloadLength = reader.ReadUInt16();
                    byte[] payloadValue = reader.ReadRawBytes(payloadLength);
                    var value = new byte[payloadLength + 1];
                    value[0] = (byte)layout.Program.StateSlots[i].ValueKind;
                    Buffer.BlockCopy(payloadValue, 0, value, 1, payloadLength);
                    values[i] = value;
                    readChanged++;
                }
                else
                {
                    values[i] = baseline.Values[i];
                }
            }
            if (readChanged != changedCount)
                throw new InvalidDataException("Network Checkpoint delta changed value count does not match its bitset.");
            byte[] controlStateBytes = reader.ReadBytes();
            ulong eventSequence = reader.ReadUInt64();
            ulong actionEventSequence = reader.ReadUInt64();
            ulong handleAllocator = reader.ReadUInt64();
            byte[] abilityExecutionStateBytes = reader.ReadBytes();
            byte[] inputRequestStateBytes = reader.ReadBytes();
            byte[] actionActivationRequestStateBytes = reader.ReadBytes();
            byte[] actionInstanceStateBytes = reader.ReadBytes();
            byte[] timelineRetentionStateBytes = reader.ReadBytes();
            byte[] motionWarpStateBytes = reader.ReadBytes();
            byte[] gameplayEffectStateBytes = ReadOptionalBytes(reader);
            byte[] equipmentStateBytes = ReadOptionalBytes(reader);
            remote = ReadCompactRemote(reader, layout, authorityTick, remoteActor);
            reader.RequireComplete();
            CharacterSimulationState state = DecodeState(layout, authorityTick, values, controlStateBytes, eventSequence, actionEventSequence, handleAllocator, abilityExecutionStateBytes, inputRequestStateBytes, actionActivationRequestStateBytes, actionInstanceStateBytes, timelineRetentionStateBytes, motionWarpStateBytes, gameplayEffectStateBytes, equipmentStateBytes);
            byte[] stateBytes = CharacterSimulationStateCodec.Write(state);
            if (!CharacterSimulationStateCodec.ComputeHash(state).Equals(stateHash))
                throw new InvalidDataException("Network Checkpoint delta reconstructed Character state hash is invalid.");
            if (remote.BodySamples.Count == 0)
                throw new InvalidDataException("Network Checkpoint delta has no remote body sample.");
            AuthoritativeActorBaseline previous = baseline.Baseline;
            var rebuilt = new AuthoritativeActorBaseline(
                previous.ActorId,
                authorityTick,
                previous.NumericProfile,
                previous.TargetAbiVersion,
                previous.StateCodecIdentity,
                previous.ProgramHash,
                previous.LayoutHash,
                previous.OperationSetVersion,
                stateBytes,
                stateHash,
                previous.WorldRevision,
                previous.SolverId,
                previous.SolverVersion,
                previous.SolverCapabilities,
                body,
                confirmedInputSequence,
                horizon);
            return new NetworkCheckpoint(rebuilt, values, controlStateBytes, eventSequence, actionEventSequence, handleAllocator, abilityExecutionStateBytes, inputRequestStateBytes, actionActivationRequestStateBytes, actionInstanceStateBytes, timelineRetentionStateBytes, motionWarpStateBytes, gameplayEffectStateBytes, equipmentStateBytes);
        }

        static void WriteCompactRemote(
            CanonicalWriter writer,
            NetworkCheckpointLayout layout,
            SimulationTick authorityTick,
            RemotePresentationBatch remote)
        {
            CharacterBodySample body = remote.BodySamples[0];
            if (body.ActorId != remote.ActorId || body.Tick != authorityTick)
                throw new InvalidDataException("Remote body sample does not match the routine snapshot Tick.");
            WriteCompactBody(writer, body.BeforeBody);
            WriteCompactBody(writer, body.FinalBody);
            writer.WriteVector3(body.AppliedDisplacement);
            writer.WriteScalar(body.AppliedYawDegrees);
            if (remote.SampleCommands.Count > ushort.MaxValue)
                throw new InvalidDataException("Remote sample command count exceeds its wire boundary.");
            writer.WriteUInt16((ushort)remote.SampleCommands.Count);
            for (int i = 0; i < remote.SampleCommands.Count; i++)
                WriteCompactSampleCommand(writer, layout, authorityTick, remote.ActorId, remote.SampleCommands[i]);
        }

        static RemotePresentationBatch ReadCompactRemote(
            CanonicalReader reader,
            NetworkCheckpointLayout layout,
            SimulationTick authorityTick,
            ActorId actorId)
        {
            WorldBodyState before = ReadCompactBody(reader, actorId);
            WorldBodyState final = ReadCompactBody(reader, actorId);
            var body = new CharacterBodySample(
                actorId,
                authorityTick,
                before,
                final,
                reader.ReadVector3(),
                reader.ReadScalar());
            int commandCount = reader.ReadUInt16();
            var commands = new PresentationCommand[commandCount];
            for (int i = 0; i < commands.Length; i++)
                commands[i] = ReadCompactSampleCommand(reader, layout, authorityTick, actorId);
            return new RemotePresentationBatch(actorId, new[] { body }, commands, Array.Empty<ServerAuthoritativeReliableEvent>(), false);
        }

        static void WriteCompactSampleCommand(
            CanonicalWriter writer,
            NetworkCheckpointLayout layout,
            SimulationTick authorityTick,
            ActorId actorId,
            PresentationCommand command)
        {
            SimulationEventHeader header = command.Header;
            if (command.Kind != PresentationCommandKind.SampleProducer || header.ActorId != actorId ||
                header.Tick != authorityTick || !header.NumericProfile.Equals(layout.Program.Manifest.NumericProfile))
            {
                throw new InvalidDataException("Remote sample command does not match the routine snapshot route.");
            }
            if (!header.Activation.Source.IsSkillOperation)
                throw new InvalidDataException("Remote sample command requires a Skill operation execution source.");
            OperationHandle operation = header.Activation.Source.Operation;
            ProgramProducer producer = ResolveCompactProducer(layout, operation);
            string executionPath = layout.ExecutionLayout.SourcePath(operation);
            SimulationExecutionSource source = SimulationExecutionSource.FromSkillOperation(operation, executionPath);
            var expectedEventId = EventId.Create(
                layout.ProgramHash,
                actorId,
                new ActivationId(source, header.Activation.Generation),
                authorityTick,
                header.Sequence,
                PresentationChannel);
            if (header.Activation.Source != source ||
                !string.Equals(header.Channel, PresentationChannel, StringComparison.Ordinal) ||
                !string.Equals(command.ProducerId, producer.Identity, StringComparison.Ordinal) ||
                !header.EventId.Equals(expectedEventId))
            {
                throw new InvalidDataException("Remote sample command does not match its locked Program identity.");
            }
            writer.WriteInt32(operation.Value);
            writer.WriteUInt64(header.Activation.Generation);
            writer.WriteUInt64(header.Sequence);
            writer.WriteScalar(command.SampleTime);
            writer.WriteScalar(command.Weight);
            writer.WriteUInt64(command.ProducerGeneration);
            writer.WriteInt32(command.Cycle);
            writer.WriteUInt64(command.SourceActionInstanceId);
            writer.WriteScalar(command.VisualTimeScale);
        }

        static PresentationCommand ReadCompactSampleCommand(
            CanonicalReader reader,
            NetworkCheckpointLayout layout,
            SimulationTick authorityTick,
            ActorId actorId)
        {
            var operation = new OperationHandle(reader.ReadInt32());
            ProgramProducer producer = ResolveCompactProducer(layout, operation);
            var activation = new ActivationId(
                SimulationExecutionSource.FromSkillOperation(operation, layout.ExecutionLayout.SourcePath(operation)),
                reader.ReadUInt64());
            ulong sequence = reader.ReadUInt64();
            var header = new SimulationEventHeader(
                layout.Program.Manifest.NumericProfile,
                EventId.Create(layout.ProgramHash, actorId, activation, authorityTick, sequence, PresentationChannel),
                actorId,
                authorityTick,
                activation,
                sequence,
                PresentationChannel);
            return new PresentationCommand(
                header,
                PresentationCommandKind.SampleProducer,
                producer.Identity,
                reader.ReadScalar(),
                reader.ReadScalar(),
                reader.ReadUInt64(),
                reader.ReadInt32(),
                reader.ReadUInt64(),
                reader.ReadScalar());
        }

        static ProgramProducer ResolveCompactProducer(NetworkCheckpointLayout layout, OperationHandle operation)
        {
            IReadOnlyList<ProgramReference> references = layout.ExecutionLayout.References(operation, ProgramReferenceKind.Producer);
            if (references.Count != 1)
                throw new InvalidDataException($"Remote sample operation '{operation}' does not have exactly one producer reference.");
            int producerIndex = references[0].TargetIndex;
            if (producerIndex < 0 || producerIndex >= layout.Program.Producers.Count)
                throw new InvalidDataException($"Remote sample operation '{operation}' producer reference is invalid.");
            return layout.Program.Producers[producerIndex];
        }

        static void WriteCompactBody(CanonicalWriter writer, WorldBodyState body)
        {
            writer.WriteVector3(body.Position);
            writer.WriteYaw(body.Yaw);
            writer.WriteVector3(body.Velocity);
            writer.WriteScalar(body.VerticalVelocity);
            writer.WriteBoolean(body.Grounded);
            writer.WriteUInt32((uint)body.Collision);
        }

        static WorldBodyState ReadCompactBody(CanonicalReader reader, ActorId actorId) => new WorldBodyState(
            actorId,
            reader.ReadVector3(),
            reader.ReadYaw(),
            reader.ReadVector3(),
            reader.ReadScalar(),
            reader.ReadBoolean(),
            (WorldCollisionSummary)reader.ReadUInt32());

        static void WriteDeltaHorizon(
            CanonicalWriter writer,
            ServerAuthoritativeEventHorizon baseline,
            ServerAuthoritativeEventHorizon target)
        {
            if (target.Sequence < baseline.Sequence)
                throw new InvalidDataException("Network Checkpoint event horizon cannot regress.");
            if (target.Sequence == baseline.Sequence && !target.EventId.Equals(baseline.EventId))
                throw new InvalidDataException("Network Checkpoint event horizon changed EventId without advancing its sequence.");
            bool changed = target.Sequence != baseline.Sequence;
            writer.WriteBoolean(changed);
            if (changed && !target.IsEmpty)
                WriteHash(writer, target.EventId.Value);
        }

        static ServerAuthoritativeEventHorizon ReadDeltaHorizon(
            CanonicalReader reader,
            ServerAuthoritativeEventHorizon baseline,
            ulong sequence)
        {
            bool changed = reader.ReadBoolean();
            if (!changed)
            {
                if (sequence != baseline.Sequence)
                    throw new InvalidDataException("Network Checkpoint event horizon sequence changed without an EventId delta.");
                return baseline;
            }
            if (sequence <= baseline.Sequence)
                throw new InvalidDataException("Network Checkpoint event horizon delta did not advance its sequence.");
            if (sequence == 0)
                return ServerAuthoritativeEventHorizon.Empty;
            return new ServerAuthoritativeEventHorizon(sequence, new EventId(ReadHash(reader)));
        }

        static void WriteHash(CanonicalWriter writer, StableHash hash)
        {
            string value = hash.IsValid ? hash.Value : throw new InvalidDataException("Stable hash is invalid.");
            var bytes = new byte[32];
            for (int i = 0; i < bytes.Length; i++)
                bytes[i] = (byte)((Hex(value[i * 2]) << 4) | Hex(value[i * 2 + 1]));
            writer.WriteRawBytes(bytes, 0, bytes.Length);
        }

        static StableHash ReadHash(CanonicalReader reader)
        {
            byte[] bytes = reader.ReadRawBytes(32);
            var chars = new char[64];
            const string hex = "0123456789abcdef";
            for (int i = 0; i < bytes.Length; i++)
            {
                chars[i * 2] = hex[bytes[i] >> 4];
                chars[i * 2 + 1] = hex[bytes[i] & 15];
            }
            return new StableHash(new string(chars));
        }

        static int Hex(char value) => value >= '0' && value <= '9' ? value - '0' : value - 'a' + 10;

        static byte[][] EncodeValues(NetworkCheckpointLayout layout, CharacterSimulationState state)
        {
            var values = new byte[layout.SlotCount][];
            for (int i = 0; i < values.Length; i++)
            {
                using var writer = new CanonicalWriter();
                ProgramStateSlot slot = layout.Program.StateSlots[i];
                CharacterSimulationStateCodec.WriteValue(writer, state.Get(i, slot.ValueKind), layout.ExecutionLayout);
                values[i] = writer.ToArray();
            }
            return values;
        }

        static CharacterSimulationState DecodeState(
            NetworkCheckpointLayout layout,
            SimulationTick tick,
            IReadOnlyList<byte[]> values,
            byte[] controlStateBytes,
            ulong eventSequence,
            ulong actionEventSequence,
            ulong handleAllocator,
            byte[] abilityExecutionStateBytes,
            byte[] inputRequestStateBytes,
            byte[] actionActivationRequestStateBytes,
            byte[] actionInstanceStateBytes,
            byte[] timelineRetentionStateBytes,
            byte[] motionWarpStateBytes,
            byte[] gameplayEffectStateBytes,
            byte[] equipmentStateBytes)
        {
            if (!tick.IsValid || values == null || values.Count != layout.SlotCount)
                throw new InvalidDataException("Network Checkpoint dense state boundary is invalid.");
            var decoded = new CharacterStateValue[values.Count];
            for (int i = 0; i < decoded.Length; i++)
            {
                var reader = new CanonicalReader(values[i]);
                decoded[i] = CharacterSimulationStateCodec.ReadValue(reader, layout.ExecutionLayout);
                reader.RequireComplete();
                if (decoded[i].Kind != layout.Program.StateSlots[i].ValueKind)
                    throw new InvalidDataException($"Network Checkpoint slot '{i}' value kind is invalid.");
            }
            CharacterControlRuntimeState controlState = CharacterControlRuntimeStateCodec.Read(controlStateBytes);
            if (controlState.LastCompletedTick != tick.Value)
                throw new InvalidDataException("Network Checkpoint Control state Tick does not match its authority Tick.");
            GameplayEffectStateAggregate gameplayEffectState = null;
            if (gameplayEffectStateBytes != null)
            {
                var gameplayEffectReader = new CanonicalReader(gameplayEffectStateBytes);
                gameplayEffectState = GameplayEffectStateAggregateCodec.Read(gameplayEffectReader, layout.ExecutionLayout.GameplayEffectProgram);
                gameplayEffectReader.RequireComplete();
            }
            if ((gameplayEffectStateBytes != null) != layout.Program.Manifest.Capabilities.HasGameplayCapability("GameplayEffect"))
                throw new InvalidDataException("Network Checkpoint Gameplay Effect state presence does not match its runtime capability.");
            EquipmentStateAggregate equipmentState = null;
            if (equipmentStateBytes != null)
            {
                var equipmentReader = new CanonicalReader(equipmentStateBytes);
                equipmentState = EquipmentStateAggregateCodec.Read(equipmentReader, layout.ExecutionLayout.Equipment);
                equipmentReader.RequireComplete();
            }
            if ((equipmentStateBytes != null) != layout.ExecutionLayout.Equipment.CapabilityEnabled)
                throw new InvalidDataException("Network Checkpoint Equipment state presence does not match its runtime binding.");
            return CharacterSimulationState.Create(layout.Program, layout.ExecutionLayout, tick.Value, decoded, eventSequence, actionEventSequence, handleAllocator, abilityExecutionStateBytes, inputRequestStateBytes, actionActivationRequestStateBytes, actionInstanceStateBytes, timelineRetentionStateBytes, motionWarpStateBytes, controlState, gameplayEffectState, equipmentState);
        }

        static AuthoritativeActorBaseline BuildBaseline(
            NetworkCheckpointLayout layout,
            ActorId actorId,
            SimulationTick tick,
            CharacterStateHash expectedStateHash,
            WorldRevision worldRevision,
            SolverImplementationId solverId,
            string solverVersion,
            WorldCapability capabilities,
            WorldBodyState body,
            ulong inputSequence,
            ServerAuthoritativeEventHorizon horizon,
            IReadOnlyList<byte[]> values,
            string stateCodecIdentity,
            byte[] controlStateBytes,
            ulong eventSequence,
            ulong actionEventSequence,
            ulong handleAllocator,
            byte[] abilityExecutionStateBytes,
            byte[] inputRequestStateBytes,
            byte[] actionActivationRequestStateBytes,
            byte[] actionInstanceStateBytes,
            byte[] timelineRetentionStateBytes,
            byte[] motionWarpStateBytes,
            byte[] gameplayEffectStateBytes,
            byte[] equipmentStateBytes)
        {
            CharacterSimulationState state = DecodeState(layout, tick, values, controlStateBytes, eventSequence, actionEventSequence, handleAllocator, abilityExecutionStateBytes, inputRequestStateBytes, actionActivationRequestStateBytes, actionInstanceStateBytes, timelineRetentionStateBytes, motionWarpStateBytes, gameplayEffectStateBytes, equipmentStateBytes);
            byte[] stateBytes = CharacterSimulationStateCodec.Write(state);
            CharacterStateHash stateHash = CharacterSimulationStateCodec.ComputeHash(state);
            if (!stateHash.Equals(expectedStateHash))
                throw new InvalidDataException("Network Checkpoint reconstructed Character state hash is invalid.");
            return new AuthoritativeActorBaseline(
                actorId,
                tick,
                layout.Program.Manifest.NumericProfile,
                layout.Program.Manifest.NumericProfile.AbiVersion,
                stateCodecIdentity,
                layout.ProgramHash,
                layout.ProgramLayoutHash,
                layout.Program.Manifest.OperationSetVersion,
                stateBytes,
                stateHash,
                worldRevision,
                solverId,
                solverVersion,
                capabilities,
                body,
                inputSequence,
                horizon);
        }

        static byte[][] ReadValues(CanonicalReader reader, int expectedCount)
        {
            int count = reader.ReadInt32();
            if (count != expectedCount)
                throw new InvalidDataException($"Network Checkpoint has '{count}' values, expected '{expectedCount}'.");
            var values = new byte[count][];
            for (int i = 0; i < count; i++)
                values[i] = reader.ReadBytes();
            return values;
        }

        static byte[] EncodeGameplayEffectState(NetworkCheckpointLayout layout, CharacterSimulationState state)
        {
            if (!state.TryGetGameplayEffectState(out GameplayEffectStateAggregate gameplayEffectState))
                return null;
            using var writer = new CanonicalWriter();
            GameplayEffectStateAggregateCodec.Write(writer, gameplayEffectState, layout.ExecutionLayout.GameplayEffectProgram);
            return writer.ToArray();
        }

        static byte[] EncodeEquipmentState(CharacterSimulationState state)
        {
            if (!state.TryGetEquipmentState(out EquipmentStateAggregate equipmentState))
                return null;
            using var writer = new CanonicalWriter();
            EquipmentStateAggregateCodec.Write(writer, equipmentState);
            return writer.ToArray();
        }

        static void WriteOptionalBytes(CanonicalWriter writer, byte[] bytes)
        {
            writer.WriteBoolean(bytes != null);
            if (bytes != null)
                writer.WriteBytes(bytes);
        }

        static byte[] ReadOptionalBytes(CanonicalReader reader) => reader.ReadBoolean() ? reader.ReadBytes() : null;

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

        static WorldBodyState ReadBody(CanonicalReader reader, ActorId actorId)
        {
            if (!string.Equals(reader.ReadString(), actorId.Value, StringComparison.Ordinal))
                throw new InvalidDataException("Network Checkpoint body ActorId is invalid.");
            return new WorldBodyState(
                actorId,
                reader.ReadVector3(),
                reader.ReadYaw(),
                reader.ReadVector3(),
                reader.ReadScalar(),
                reader.ReadBoolean(),
                (WorldCollisionSummary)reader.ReadUInt32());
        }

        static void WriteHorizon(CanonicalWriter writer, ServerAuthoritativeEventHorizon horizon)
        {
            writer.WriteUInt64(horizon.Sequence);
            writer.WriteString(horizon.IsEmpty ? string.Empty : horizon.EventId.ToString());
        }

        static ServerAuthoritativeEventHorizon ReadHorizon(CanonicalReader reader)
        {
            ulong sequence = reader.ReadUInt64();
            string eventId = reader.ReadString();
            return sequence == 0
                ? ServerAuthoritativeEventHorizon.Empty
                : new ServerAuthoritativeEventHorizon(sequence, new EventId(new StableHash(eventId)));
        }

        static bool Equal(byte[] left, byte[] right)
        {
            if (ReferenceEquals(left, right))
                return true;
            if (left == null || right == null || left.Length != right.Length)
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
