using ThirdPersonSimulation;
using System;
using System.Collections.Generic;
using System.IO;
using static ThirdPersonSimulation.SimulationProgramSemanticsCodec;

namespace ThirdPersonSimulation.Fixed
{
    public sealed class CharacterSimulationProgramArtifactVersionException : IOException
    {
        public CharacterSimulationProgramArtifactVersionException(int version)
            : base($"Character Simulation Program artifact version '{version}' is unsupported.")
        {
            Version = version;
        }

        public int Version { get; }
    }

    internal static class SimulationNumericProfileCodec
    {
        public static void Write(CanonicalWriter writer, SimulationNumericProfile profile)
        {
            writer.WriteString(profile.Id.Value);
            writer.WriteInt32(profile.AbiVersion.Value);
            writer.WriteInt32(profile.ScalarBits);
            writer.WriteByte((byte)profile.Rounding);
            writer.WriteByte((byte)profile.Overflow);
            writer.WriteBoolean(profile.DeterministicReplay);
        }

        public static SimulationNumericProfile Read(CanonicalReader reader)
        {
            return new SimulationNumericProfile(
                new NumericProfileId(reader.ReadString()),
                new TargetAbiVersion(reader.ReadInt32()),
                reader.ReadInt32(),
                ReadEnum<SimulationNumericRoundingMode>(reader.ReadByte()),
                ReadEnum<SimulationNumericOverflowMode>(reader.ReadByte()),
                reader.ReadBoolean());
        }

        static T ReadEnum<T>(byte value) where T : struct, Enum
        {
            object candidate = Enum.ToObject(typeof(T), value);
            if (!Enum.IsDefined(typeof(T), candidate))
                throw new InvalidDataException($"Enum value '{value}' is invalid for '{typeof(T).Name}'.");
            return (T)candidate;
        }
    }

    public readonly struct ProgramLoadExpectation
    {
        public ProgramLoadExpectation(
            string compilerVersion,
            OperationSetVersion operationSetVersion,
            ProgramRevision sourceRevision,
            SemanticHash semanticHash,
            SimulationNumericProfile numericProfile)
        {
            CompilerVersion = SimulationIdentity.Require(compilerVersion, nameof(compilerVersion));
            if (!operationSetVersion.IsValid)
                throw new ArgumentException("Operation-set version is required.", nameof(operationSetVersion));
            if (string.IsNullOrEmpty(sourceRevision.Value))
                throw new ArgumentException("Source revision is required.", nameof(sourceRevision));
            if (!semanticHash.IsValid)
                throw new ArgumentException("Semantic hash is required.", nameof(semanticHash));
            OperationSetVersion = operationSetVersion;
            SourceRevision = sourceRevision;
            SemanticHash = semanticHash;
            NumericProfile = numericProfile;
        }
        public string CompilerVersion { get; }
        public OperationSetVersion OperationSetVersion { get; }
        public ProgramRevision SourceRevision { get; }
        public SemanticHash SemanticHash { get; }
        public SimulationNumericProfile NumericProfile { get; }
    }

    public readonly struct CharacterSimulationProgramArtifactHeader
    {
        public CharacterSimulationProgramArtifactHeader(
            string compilerVersion,
            OperationSetVersion operationSetVersion,
            ProgramRevision sourceRevision,
            SemanticHash semanticHash,
            SimulationNumericProfile numericProfile,
            ProgramHash programHash,
            LayoutHash layoutHash,
            WorldCapability requiredWorldCapabilities,
            IReadOnlyList<string> gameplayCapabilities)
        {
            CompilerVersion = compilerVersion;
            OperationSetVersion = operationSetVersion;
            SourceRevision = sourceRevision;
            SemanticHash = semanticHash;
            NumericProfile = numericProfile;
            ProgramHash = programHash;
            LayoutHash = layoutHash;
            RequiredWorldCapabilities = requiredWorldCapabilities;
            GameplayCapabilities = gameplayCapabilities;
        }

        public string CompilerVersion { get; }
        public OperationSetVersion OperationSetVersion { get; }
        public ProgramRevision SourceRevision { get; }
        public SemanticHash SemanticHash { get; }
        public SimulationNumericProfile NumericProfile { get; }
        public ProgramHash ProgramHash { get; }
        public LayoutHash LayoutHash { get; }
        public WorldCapability RequiredWorldCapabilities { get; }
        public IReadOnlyList<string> GameplayCapabilities { get; }
    }

    public static class CharacterSimulationProgramCodec
    {
        const uint ArtifactMagic = 0x58494643;
        const int ArtifactVersion = 15;
        const int ProgramFormatVersion = 16;
        const int LayoutFormatVersion = 9;
        const int SourceMapStringTableVersion = 3;

        public static CharacterSimulationProgramArtifactHeader ReadArtifactHeader(byte[] bytes)
        {
            var reader = new CanonicalReader(bytes);
            if (reader.ReadUInt32() != ArtifactMagic)
                throw new InvalidDataException("Fixed Character Simulation Program artifact magic is invalid.");
            int artifactVersion = reader.ReadInt32();
            if (artifactVersion != ArtifactVersion)
                throw new CharacterSimulationProgramArtifactVersionException(artifactVersion);
            string compilerVersion = reader.ReadString();
            var operationSetVersion = new OperationSetVersion(reader.ReadString());
            var sourceRevision = new ProgramRevision(reader.ReadString());
            var semanticHash = new SemanticHash(new StableHash(reader.ReadString()));
            SimulationNumericProfile numericProfile = SimulationNumericProfileCodec.Read(reader);
            var programHash = new ProgramHash(new StableHash(reader.ReadString()));
            var layoutHash = new LayoutHash(new StableHash(reader.ReadString()));
            var requiredWorldCapabilities = (WorldCapability)reader.ReadUInt64();
            int gameplayCapabilityCount = ReadCount(reader);
            var gameplayCapabilities = new string[gameplayCapabilityCount];
            for (int i = 0; i < gameplayCapabilityCount; i++)
                gameplayCapabilities[i] = reader.ReadString();
            reader.ReadBytes();
            reader.RequireComplete();
            return new CharacterSimulationProgramArtifactHeader(
                compilerVersion,
                operationSetVersion,
                sourceRevision,
                semanticHash,
                numericProfile,
                programHash,
                layoutHash,
                requiredWorldCapabilities,
                Array.AsReadOnly(gameplayCapabilities));
        }

        public static byte[] WriteArtifact(CharacterSimulationProgram program)
        {
            if (program == null)
                throw new ArgumentNullException(nameof(program));
            byte[] payload = WritePayload(program);
            using var writer = new CanonicalWriter();
            writer.WriteUInt32(ArtifactMagic);
            writer.WriteInt32(ArtifactVersion);
            writer.WriteString(program.Manifest.CompilerVersion);
            writer.WriteString(program.Manifest.OperationSetVersion.Value);
            writer.WriteString(program.Manifest.SourceRevision.Value);
            writer.WriteString(program.Manifest.SemanticHash.ToString());
            SimulationNumericProfileCodec.Write(writer, program.Manifest.NumericProfile);
            writer.WriteString(program.ProgramHash.ToString());
            writer.WriteString(program.LayoutHash.ToString());
            writer.WriteUInt64((ulong)program.Manifest.Capabilities.RequiredWorldCapabilities);
            writer.WriteInt32(program.Manifest.Capabilities.GameplayCapabilities.Count);
            for (int i = 0; i < program.Manifest.Capabilities.GameplayCapabilities.Count; i++)
                writer.WriteString(program.Manifest.Capabilities.GameplayCapabilities[i]);
            writer.WriteBytes(payload);
            return writer.ToArray();
        }

        public static CharacterSimulationProgram ReadArtifact(byte[] bytes, ProgramLoadExpectation expectation)
        {
            var reader = new CanonicalReader(bytes);
            if (reader.ReadUInt32() != ArtifactMagic)
                throw new InvalidDataException("Character Simulation Program artifact magic is invalid.");
            int artifactVersion = reader.ReadInt32();
            if (artifactVersion != ArtifactVersion)
                throw new CharacterSimulationProgramArtifactVersionException(artifactVersion);
            string compilerVersion = reader.ReadString();
            string operationSetVersion = reader.ReadString();
            string sourceRevision = reader.ReadString();
            var semanticHash = new SemanticHash(new StableHash(reader.ReadString()));
            SimulationNumericProfile numericProfile = SimulationNumericProfileCodec.Read(reader);
            var expectedProgramHash = new ProgramHash(new StableHash(reader.ReadString()));
            var expectedLayoutHash = new LayoutHash(new StableHash(reader.ReadString()));
            var expectedWorldCapabilities = (WorldCapability)reader.ReadUInt64();
            int expectedGameplayCapabilityCount = ReadCount(reader);
            var expectedGameplayCapabilities = new string[expectedGameplayCapabilityCount];
            for (int i = 0; i < expectedGameplayCapabilityCount; i++)
                expectedGameplayCapabilities[i] = reader.ReadString();
            byte[] payload = reader.ReadBytes();
            reader.RequireComplete();
            if (!string.Equals(compilerVersion, expectation.CompilerVersion, StringComparison.Ordinal))
                throw new InvalidDataException($"Program compiler version '{compilerVersion}' does not match expected '{expectation.CompilerVersion}'.");
            if (!string.Equals(operationSetVersion, expectation.OperationSetVersion.Value, StringComparison.Ordinal))
                throw new InvalidDataException($"Program operation-set version '{operationSetVersion}' does not match expected '{expectation.OperationSetVersion}'.");
            if (!string.Equals(sourceRevision, expectation.SourceRevision.Value, StringComparison.Ordinal))
                throw new InvalidDataException($"Program source revision '{sourceRevision}' does not match expected '{expectation.SourceRevision.Value}'.");
            if (!semanticHash.Equals(expectation.SemanticHash))
                throw new InvalidDataException($"Program SemanticHash '{semanticHash}' does not match expected '{expectation.SemanticHash}'.");
            if (numericProfile != expectation.NumericProfile)
                throw new InvalidDataException($"Program Numeric Profile '{numericProfile.Id}' does not match expected '{expectation.NumericProfile.Id}'.");
            CharacterSimulationProgram program = ReadPayload(payload);
            if (!string.Equals(program.Manifest.CompilerVersion, compilerVersion, StringComparison.Ordinal) ||
                !string.Equals(program.Manifest.OperationSetVersion.Value, operationSetVersion, StringComparison.Ordinal) ||
                !string.Equals(program.Manifest.SourceRevision.Value, sourceRevision, StringComparison.Ordinal) ||
                !program.Manifest.SemanticHash.Equals(semanticHash) ||
                program.Manifest.NumericProfile != numericProfile)
                throw new InvalidDataException("Program artifact header does not match its payload manifest.");
            if (!program.ProgramHash.Equals(expectedProgramHash))
                throw new InvalidDataException($"Program hash mismatch. Expected '{expectedProgramHash}', actual '{program.ProgramHash}'.");
            if (!program.LayoutHash.Equals(expectedLayoutHash))
                throw new InvalidDataException($"Program layout hash mismatch. Expected '{expectedLayoutHash}', actual '{program.LayoutHash}'.");
            if (program.Manifest.Capabilities.RequiredWorldCapabilities != expectedWorldCapabilities ||
                program.Manifest.Capabilities.GameplayCapabilities.Count != expectedGameplayCapabilities.Length)
                throw new InvalidDataException("Program capability manifest does not match its artifact header.");
            for (int i = 0; i < expectedGameplayCapabilities.Length; i++)
            {
                if (!string.Equals(program.Manifest.Capabilities.GameplayCapabilities[i], expectedGameplayCapabilities[i], StringComparison.Ordinal))
                    throw new InvalidDataException("Program gameplay capability manifest does not match its artifact header.");
            }
            return program;
        }

        public static CharacterSimulationProgram ReadFile(string path, ProgramLoadExpectation expectation)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Program file path is required.", nameof(path));
            return ReadArtifact(File.ReadAllBytes(path), expectation);
        }

        public static ProgramHash ComputeProgramHash(CharacterSimulationProgram program)
        {
            using var writer = new CanonicalWriter();
            WritePayload(writer, program);
            return new ProgramHash(writer.ComputeHash());
        }

        public static LayoutHash ComputeLayoutHash(CharacterSimulationProgram program)
        {
            using var writer = new CanonicalWriter();
            writer.WriteInt32(LayoutFormatVersion);
            writer.WriteString(program.Manifest.OperationSetVersion.Value);
            SimulationNumericProfileCodec.Write(writer, program.Manifest.NumericProfile);
            writer.WriteInt32(program.StateSlots.Count);
            for (int i = 0; i < program.StateSlots.Count; i++)
                WriteStateSlot(writer, program.StateSlots[i], false);
            writer.WriteInt32(program.Scopes.Count);
            for (int i = 0; i < program.Scopes.Count; i++)
                WriteScope(writer, program.Scopes[i]);
            WriteTable(writer, program.ConstantInputBindings, WriteConstantInputBinding);
            WriteTable(writer, program.MotionModifiers, WriteMotionModifier);
            return new LayoutHash(writer.ComputeHash());
        }

        static byte[] WritePayload(CharacterSimulationProgram program)
        {
            using var writer = new CanonicalWriter();
            WritePayload(writer, program);
            return writer.ToArray();
        }

        static void WritePayload(CanonicalWriter writer, CharacterSimulationProgram program)
        {
            writer.WriteInt32(ProgramFormatVersion);
            WriteManifest(writer, program.Manifest);
            WriteBodyMotion(writer, program.BodyMotion);
            writer.WriteString(program.LayoutHash.ToString());
            WriteTable(writer, program.Constants, WriteConstant);
            WriteTable(writer, program.OperationDefinitions, WriteOperationDefinition);
            WriteTable(writer, program.Operations, WriteOperation);
            WriteTable(writer, program.ConstantInputBindings, WriteConstantInputBinding);
            WriteTable(writer, program.ControlFlow, WriteControlFlow);
            WriteTable(writer, program.References, WriteReference);
            WriteTable(writer, program.StateSlots, (target, value) => WriteStateSlot(target, value, true));
            WriteTable(writer, program.Scopes, WriteScope);
            WriteTable(writer, program.WorldRequests, WriteWorldRequest);
            WriteTable(writer, program.OutputChannels, WriteOutputChannel);
            WriteTable(writer, program.CatalogEntries, WriteCatalogEntry);
            WriteTable(writer, program.MotionModifiers, WriteMotionModifier);
            WriteSourceMapTable(writer, program.SourceMap);
            WriteTable(writer, program.Producers, WriteProducer);
        }

        static CharacterSimulationProgram ReadPayload(byte[] bytes)
        {
            var reader = new CanonicalReader(bytes);
            if (reader.ReadInt32() != ProgramFormatVersion)
                throw new InvalidDataException("Character Simulation Program payload version is unsupported.");
            CharacterSimulationProgramManifest manifest = ReadManifest(reader);
            ProgramBodyMotionDescriptor bodyMotion = ReadBodyMotion(reader);
            var expectedLayoutHash = new LayoutHash(new StableHash(reader.ReadString()));
            ProgramConstant[] constants = ReadTable(reader, ReadConstant);
            SimulationOperationDefinition[] operationDefinitions = ReadTable(reader, ReadOperationDefinition);
            int operationCount = ReadCount(reader);
            var operations = new SimulationOperation[operationCount];
            for (int i = 0; i < operationCount; i++)
                operations[i] = ReadOperation(reader, operationDefinitions);
            ProgramConstantInputBinding[] constantInputBindings = ReadTable(reader, ReadConstantInputBinding);
            ProgramControlFlowEdge[] controlFlow = ReadTable(reader, ReadControlFlow);
            ProgramReference[] references = ReadTable(reader, ReadReference);
            ProgramStateSlot[] stateSlots = ReadTable(reader, ReadStateSlot);
            ProgramScopeLayout[] scopes = ReadTable(reader, ReadScope);
            ProgramWorldRequestLayout[] worldRequests = ReadTable(reader, ReadWorldRequest);
            ProgramOutputChannelLayout[] outputChannels = ReadTable(reader, ReadOutputChannel);
            ProgramCatalogEntry[] catalogEntries = ReadTable(reader, ReadCatalogEntry);
            ProgramMotionModifierDescriptor[] motionModifiers = ReadTable(reader, ReadMotionModifier);
            ProgramSourceMapEntry[] sourceMap = ReadSourceMapTable(reader);
            ProgramProducer[] producers = ReadTable(reader, ReadProducer);
            reader.RequireComplete();
            var program = new CharacterSimulationProgram(
                manifest,
                bodyMotion,
                operationDefinitions,
                operations,
                constants,
                constantInputBindings,
                controlFlow,
                references,
                stateSlots,
                scopes,
                worldRequests,
                outputChannels,
                catalogEntries,
                motionModifiers,
                sourceMap,
                producers);
            if (!program.LayoutHash.Equals(expectedLayoutHash))
                throw new InvalidDataException($"Program payload layout hash mismatch. Expected '{expectedLayoutHash}', actual '{program.LayoutHash}'.");
            return program;
        }

        static void WriteBodyMotion(CanonicalWriter writer, ProgramBodyMotionDescriptor descriptor)
        {
            writer.WriteString(descriptor.SourceIdentity);
            writer.WriteString(descriptor.ContentRevision.Value);
            writer.WriteInt32(descriptor.SemanticVersion);
            writer.WriteScalar(descriptor.GravityAcceleration);
            writer.WriteScalar(descriptor.MaximumFallSpeed);
        }

        static ProgramBodyMotionDescriptor ReadBodyMotion(CanonicalReader reader)
        {
            return new ProgramBodyMotionDescriptor(
                reader.ReadString(),
                new StableHash(reader.ReadString()),
                reader.ReadInt32(),
                reader.ReadScalar(),
                reader.ReadScalar());
        }

        static void WriteConstantInputBinding(CanonicalWriter writer, ProgramConstantInputBinding binding)
        {
            writer.WriteInt32(binding.TargetOperation.Value);
            writer.WriteString(binding.TargetPort);
            writer.WriteInt32(binding.ConstantIndex);
            writer.WriteByte((byte)binding.ResolvedValueKind);
        }

        static void WriteMotionModifier(CanonicalWriter writer, ProgramMotionModifierDescriptor value)
        {
            writer.WriteInt32(value.Index);
            writer.WriteByte((byte)value.Kind);
            writer.WriteByte((byte)value.Channel);
            writer.WriteInt32(value.Operation.Value);
            writer.WriteInt32(value.SourceMotionOperation.Value);
            writer.WriteInt32(value.TimelineOwnerOperation.Value);
            writer.WriteString(value.ActionContextIdentity);
            writer.WriteInt32(value.CatalogEntryIndex);
            writer.WriteInt32(value.StateSlotStart);
            writer.WriteInt32(value.StateSlotCount);
            writer.WriteByte((byte)value.TranslationMode);
            writer.WriteByte((byte)value.TargetOffsetSpace);
            writer.WriteByte((byte)value.RotationMode);
            writer.WriteByte((byte)value.RotationMethod);
            writer.WriteInt32(value.TargetPlanarOffsetConstantIndex);
            writer.WriteInt32(value.TargetYawOffsetConstantIndex);
            writer.WriteInt32(value.MaximumPositionCorrectionConstantIndex);
            writer.WriteInt32(value.MaximumYawCorrectionConstantIndex);
            writer.WriteInt32(value.MaximumYawRateConstantIndex);
            writer.WriteByte((byte)value.LimitPolicy);
            writer.WriteInt32(value.PositionProgressCurveConstantIndex);
            writer.WriteInt32(value.YawProgressCurveConstantIndex);
        }

        static ProgramMotionModifierDescriptor ReadMotionModifier(CanonicalReader reader)
        {
            return new ProgramMotionModifierDescriptor(
                reader.ReadInt32(),
                (ProgramMotionModifierKind)reader.ReadByte(),
                (ProgramMotionModifierChannel)reader.ReadByte(),
                new OperationHandle(reader.ReadInt32()),
                new OperationHandle(reader.ReadInt32()),
                new OperationHandle(reader.ReadInt32()),
                reader.ReadString(),
                reader.ReadInt32(),
                reader.ReadInt32(),
                reader.ReadInt32(),
                (ProgramMotionWarpTranslationMode)reader.ReadByte(),
                (ProgramMotionWarpTargetOffsetSpace)reader.ReadByte(),
                (ProgramMotionWarpRotationMode)reader.ReadByte(),
                (ProgramMotionWarpRotationMethod)reader.ReadByte(),
                reader.ReadInt32(),
                reader.ReadInt32(),
                reader.ReadInt32(),
                reader.ReadInt32(),
                reader.ReadInt32(),
                (ProgramMotionWarpLimitPolicy)reader.ReadByte(),
                reader.ReadInt32(),
                reader.ReadInt32());
        }

        static ProgramConstantInputBinding ReadConstantInputBinding(CanonicalReader reader)
        {
            var operation = new OperationHandle(reader.ReadInt32());
            string port = reader.ReadString();
            int constant = reader.ReadInt32();
            byte kindValue = reader.ReadByte();
            if (!Enum.IsDefined(typeof(SemanticValueKind), kindValue))
                throw new InvalidDataException($"Program constant input contains unknown value kind '{kindValue}'.");
            return new ProgramConstantInputBinding(operation, port, constant, (SemanticValueKind)kindValue);
        }

        static void WriteSourceMapTable(CanonicalWriter writer, IReadOnlyList<ProgramSourceMapEntry> values)
        {
            writer.WriteInt32(SourceMapStringTableVersion);
            var strings = new SortedSet<string>(StringComparer.Ordinal) { string.Empty };
            for (int i = 0; i < values.Count; i++)
            {
                ProgramSourceMapEntry value = values[i];
                strings.Add(value.SourceType);
                strings.Add(value.GraphId);
                strings.Add(value.NodeId);
                strings.Add(value.PortId);
                strings.Add(value.EdgeId);
                strings.Add(value.DeclarationId);
                strings.Add(value.TimelineId);
                strings.Add(value.TrackId);
                strings.Add(value.ClipId);
                strings.Add(value.ContentHash);
                string[] segments = SplitDisplayPath(value.DisplayPath);
                for (int segmentIndex = 0; segmentIndex < segments.Length; segmentIndex++)
                    strings.Add(segments[segmentIndex]);
            }

            var stringIndex = new Dictionary<string, int>(strings.Count, StringComparer.Ordinal);
            writer.WriteInt32(strings.Count);
            int index = 0;
            foreach (string value in strings)
            {
                writer.WriteString(value);
                stringIndex.Add(value, index++);
            }

            writer.WriteInt32(values.Count);
            for (int i = 0; i < values.Count; i++)
            {
                ProgramSourceMapEntry value = values[i];
                writer.WriteByte((byte)value.TargetKind);
                writer.WriteInt32(value.TargetIndex);
                writer.WriteInt32(stringIndex[value.SourceType]);
                writer.WriteInt32(stringIndex[value.GraphId]);
                writer.WriteInt32(stringIndex[value.NodeId]);
                writer.WriteInt32(stringIndex[value.PortId]);
                writer.WriteInt32(stringIndex[value.EdgeId]);
                writer.WriteInt32(stringIndex[value.DeclarationId]);
                writer.WriteInt32(stringIndex[value.TimelineId]);
                writer.WriteInt32(stringIndex[value.TrackId]);
                writer.WriteInt32(stringIndex[value.ClipId]);
                writer.WriteInt32(stringIndex[value.ContentHash]);
                string[] segments = SplitDisplayPath(value.DisplayPath);
                writer.WriteInt32(segments.Length);
                for (int segmentIndex = 0; segmentIndex < segments.Length; segmentIndex++)
                    writer.WriteInt32(stringIndex[segments[segmentIndex]]);
            }
        }

        static ProgramSourceMapEntry[] ReadSourceMapTable(CanonicalReader reader)
        {
            if (reader.ReadInt32() != SourceMapStringTableVersion)
                throw new InvalidDataException("Program source-map string-table version is unsupported.");
            int stringCount = ReadCount(reader);
            if (stringCount == 0)
                throw new InvalidDataException("Program source-map string table is empty.");
            var strings = new string[stringCount];
            for (int i = 0; i < stringCount; i++)
            {
                strings[i] = reader.ReadString();
                if (i == 0 && strings[i].Length != 0)
                    throw new InvalidDataException("Program source-map string table must begin with the empty string.");
                if (i > 0 && string.CompareOrdinal(strings[i - 1], strings[i]) >= 0)
                    throw new InvalidDataException("Program source-map string table is not canonical.");
            }

            int entryCount = ReadCount(reader);
            var entries = new ProgramSourceMapEntry[entryCount];
            for (int i = 0; i < entryCount; i++)
            {
                ProgramSourceTargetKind targetKind = ReadEnum<ProgramSourceTargetKind>(reader.ReadByte());
                int targetIndex = reader.ReadInt32();
                string sourceType = ReadSourceMapString(reader, strings);
                string graphId = ReadSourceMapString(reader, strings);
                string nodeId = ReadSourceMapString(reader, strings);
                string portId = ReadSourceMapString(reader, strings);
                string edgeId = ReadSourceMapString(reader, strings);
                string declarationId = ReadSourceMapString(reader, strings);
                string timelineId = ReadSourceMapString(reader, strings);
                string trackId = ReadSourceMapString(reader, strings);
                string clipId = ReadSourceMapString(reader, strings);
                string contentHash = ReadSourceMapString(reader, strings);
                int segmentCount = ReadCount(reader);
                var pathSegments = new string[segmentCount];
                for (int segmentIndex = 0; segmentIndex < segmentCount; segmentIndex++)
                    pathSegments[segmentIndex] = ReadSourceMapString(reader, strings);
                entries[i] = new ProgramSourceMapEntry(
                    targetKind,
                    targetIndex,
                    sourceType,
                    graphId,
                    nodeId,
                    portId,
                    edgeId,
                    declarationId,
                    timelineId,
                    trackId,
                    clipId,
                    string.Join("/", pathSegments),
                    contentHash);
            }
            return entries;
        }

        static string ReadSourceMapString(CanonicalReader reader, IReadOnlyList<string> strings)
        {
            int index = reader.ReadInt32();
            if (index < 0 || index >= strings.Count)
                throw new InvalidDataException($"Program source-map string index '{index}' is out of range.");
            return strings[index];
        }

        static string[] SplitDisplayPath(string value)
        {
            return (value ?? string.Empty).Split(new[] { '/' }, StringSplitOptions.None);
        }

        static void WriteManifest(CanonicalWriter writer, CharacterSimulationProgramManifest manifest)
        {
            writer.WriteString(manifest.ProgramId.Value);
            writer.WriteString(manifest.CompilerVersion);
            writer.WriteString(manifest.OperationSetVersion.Value);
            writer.WriteInt32(manifest.TickRate);
            writer.WriteString(manifest.SourceRevision.Value);
            writer.WriteString(manifest.SemanticHash.ToString());
            SimulationNumericProfileCodec.Write(writer, manifest.NumericProfile);
            writer.WriteInt32(manifest.Capabilities.GameplayCapabilities.Count);
            for (int i = 0; i < manifest.Capabilities.GameplayCapabilities.Count; i++)
                writer.WriteString(manifest.Capabilities.GameplayCapabilities[i]);
            writer.WriteUInt64((ulong)manifest.Capabilities.RequiredWorldCapabilities);
        }

        static CharacterSimulationProgramManifest ReadManifest(CanonicalReader reader)
        {
            var programId = new ProgramId(reader.ReadString());
            string compilerVersion = reader.ReadString();
            var operationSetVersion = new OperationSetVersion(reader.ReadString());
            int tickRate = reader.ReadInt32();
            var sourceRevision = new ProgramRevision(reader.ReadString());
            var semanticHash = new SemanticHash(new StableHash(reader.ReadString()));
            SimulationNumericProfile numericProfile = SimulationNumericProfileCodec.Read(reader);
            int capabilityCount = ReadCount(reader);
            var gameplayCapabilities = new string[capabilityCount];
            for (int i = 0; i < capabilityCount; i++)
                gameplayCapabilities[i] = reader.ReadString();
            var capabilities = new ProgramCapabilityManifest(gameplayCapabilities, (WorldCapability)reader.ReadUInt64());
            return new CharacterSimulationProgramManifest(programId, compilerVersion, operationSetVersion, tickRate, sourceRevision, semanticHash, numericProfile, capabilities);
        }

        static void WriteConstant(CanonicalWriter writer, ProgramConstant value)
        {
            writer.WriteInt32(value.Index);
            writer.WriteString(value.Identity);
            writer.WriteByte((byte)value.Kind);
            switch (value.Kind)
            {
                case ProgramConstantKind.Boolean: writer.WriteBoolean(value.Boolean); break;
                case ProgramConstantKind.Int32: writer.WriteInt32(value.Int32); break;
                case ProgramConstantKind.UInt64: writer.WriteUInt64(value.UInt64); break;
                case ProgramConstantKind.Scalar: writer.WriteScalar(value.Scalar); break;
                case ProgramConstantKind.Vector2: writer.WriteVector2(value.Vector2); break;
                case ProgramConstantKind.Vector3: writer.WriteVector3(value.Vector3); break;
                case ProgramConstantKind.Yaw: writer.WriteYaw(value.Yaw); break;
                case ProgramConstantKind.String: writer.WriteString(value.Text); break;
                case ProgramConstantKind.Bytes: writer.WriteBytes(value.Bytes.ToArray()); break;
                default: throw new InvalidDataException($"Unsupported constant kind '{value.Kind}'.");
            }
        }

        static ProgramConstant ReadConstant(CanonicalReader reader)
        {
            int index = reader.ReadInt32();
            string identity = reader.ReadString();
            ProgramConstantKind kind = ReadEnum<ProgramConstantKind>(reader.ReadByte());
            switch (kind)
            {
                case ProgramConstantKind.Boolean: return ProgramConstant.FromBoolean(index, identity, reader.ReadBoolean());
                case ProgramConstantKind.Int32: return ProgramConstant.FromInt32(index, identity, reader.ReadInt32());
                case ProgramConstantKind.UInt64: return ProgramConstant.FromUInt64(index, identity, reader.ReadUInt64());
                case ProgramConstantKind.Scalar: return ProgramConstant.FromScalar(index, identity, reader.ReadScalar());
                case ProgramConstantKind.Vector2: return ProgramConstant.FromVector2(index, identity, reader.ReadVector2());
                case ProgramConstantKind.Vector3: return ProgramConstant.FromVector3(index, identity, reader.ReadVector3());
                case ProgramConstantKind.Yaw: return ProgramConstant.FromYaw(index, identity, reader.ReadYaw());
                case ProgramConstantKind.String: return ProgramConstant.FromString(index, identity, reader.ReadString());
                case ProgramConstantKind.Bytes: return ProgramConstant.FromBytes(index, identity, reader.ReadBytes());
                default: throw new InvalidDataException($"Unsupported constant kind '{kind}'.");
            }
        }

        static void WriteOperationDefinition(CanonicalWriter writer, SimulationOperationDefinition value)
        {
            writer.WriteInt32(value.Index);
            writer.WriteString(value.Identity);
            writer.WriteInt32((int)value.Code);
            WriteIntArray(writer, value.ConstantReferences);
            writer.WriteInt32(value.Integer0);
            writer.WriteInt32(value.Integer1);
            writer.WriteUInt64(value.Unsigned0);
            writer.WriteScalar(value.Scalar0);
            writer.WriteString(value.Text0);
            writer.WriteUInt32(value.Flags);
        }

        static SimulationOperationDefinition ReadOperationDefinition(CanonicalReader reader)
        {
            return new SimulationOperationDefinition(
                reader.ReadInt32(),
                reader.ReadString(),
                ReadEnum<SimulationOperationCode>(reader.ReadInt32()),
                ReadIntArray(reader),
                reader.ReadInt32(),
                reader.ReadInt32(),
                reader.ReadUInt64(),
                reader.ReadScalar(),
                reader.ReadString(),
                reader.ReadUInt32());
        }

        static void WriteOperation(CanonicalWriter writer, SimulationOperation value)
        {
            writer.WriteInt32(value.Handle.Value);
            writer.WriteInt32(value.DefinitionIndex);
            WriteIntArray(writer, value.Operands);
            WriteIntArray(writer, value.StateSlots);
        }

        static SimulationOperation ReadOperation(CanonicalReader reader, IReadOnlyList<SimulationOperationDefinition> definitions)
        {
            var handle = new OperationHandle(reader.ReadInt32());
            int definitionIndex = reader.ReadInt32();
            if (definitionIndex < 0 || definitionIndex >= definitions.Count)
                throw new InvalidDataException($"Operation '{handle}' definition index '{definitionIndex}' is invalid.");
            return new SimulationOperation(
                handle,
                definitions[definitionIndex],
                ReadIntArray(reader),
                ReadIntArray(reader));
        }

        static void WriteTable<T>(CanonicalWriter writer, IReadOnlyList<T> values, Action<CanonicalWriter, T> write)
        {
            writer.WriteInt32(values.Count);
            for (int i = 0; i < values.Count; i++)
                write(writer, values[i]);
        }

        static T[] ReadTable<T>(CanonicalReader reader, Func<CanonicalReader, T> read)
        {
            int count = ReadCount(reader);
            var values = new T[count];
            for (int i = 0; i < count; i++)
                values[i] = read(reader);
            return values;
        }

    }
}

