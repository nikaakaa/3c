using System;
using System.Collections.Generic;
using System.IO;
using static ThirdPersonSimulation.GameplayAbilitySemanticsCodec;

namespace ThirdPersonSimulation
{
    public readonly struct Float32GameplayAbilityExecutionDataLoadExpectation
    {
        public Float32GameplayAbilityExecutionDataLoadExpectation(
            string definitionGuid,
            string abilityId,
            string compilerVersion,
            string operationSetVersion,
            string sourceRevision,
            string semanticHash,
            string numericProfileId,
            int targetAbiVersion,
            string executionIdentity,
            string executionDataHash,
            string stateSchemaHash,
            string canonicalBytesHash,
            GameplayAbilityRootDescriptor root)
        {
            DefinitionGuid = definitionGuid;
            AbilityId = abilityId;
            CompilerVersion = compilerVersion;
            OperationSetVersion = operationSetVersion;
            SourceRevision = sourceRevision;
            SemanticHash = semanticHash;
            NumericProfileId = numericProfileId;
            TargetAbiVersion = targetAbiVersion;
            ExecutionIdentity = executionIdentity;
            ExecutionDataHash = executionDataHash;
            StateSchemaHash = stateSchemaHash;
            CanonicalBytesHash = canonicalBytesHash;
            Root = root;
        }

        public string DefinitionGuid { get; }
        public string AbilityId { get; }
        public string CompilerVersion { get; }
        public string OperationSetVersion { get; }
        public string SourceRevision { get; }
        public string SemanticHash { get; }
        public string NumericProfileId { get; }
        public int TargetAbiVersion { get; }
        public string ExecutionIdentity { get; }
        public string ExecutionDataHash { get; }
        public string StateSchemaHash { get; }
        public string CanonicalBytesHash { get; }
        public GameplayAbilityRootDescriptor Root { get; }
    }

    public static class Float32GameplayAbilityExecutionDataCodec
    {
        const uint ArtifactMagic = 0x46414244;
        const int ArtifactVersion = 3;
        const int PayloadVersion = 3;

        public static byte[] WriteArtifact(Float32GameplayAbilityExecutionData data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            byte[] payload = WritePayload(data);
            using var writer = new CanonicalWriter();
            writer.WriteUInt32(ArtifactMagic);
            writer.WriteInt32(ArtifactVersion);
            writer.WriteString(data.CompilerVersion);
            writer.WriteString(data.OperationSetVersion.Value);
            writer.WriteString(data.SourceRevision.Value);
            writer.WriteString(data.SemanticHash.ToString());
            Float32GameplayAbilityNumericProfileCodec.Write(writer, data.NumericProfile);
            writer.WriteString(data.ExecutionIdentity);
            writer.WriteString(data.ContentHash.ToString());
            writer.WriteString(data.StateSchemaHash.ToString());
            WriteCapabilities(writer, data.Capabilities);
            GameplayAbilityRootDescriptorCodec.Write(writer, data.Root);
            writer.WriteString(data.AbilityId.Value);
            writer.WriteBytes(payload);
            return writer.ToArray();
        }

        public static Float32GameplayAbilityExecutionData ReadArtifact(
            byte[] bytes,
            Float32GameplayAbilityExecutionDataLoadExpectation expectation)
        {
            if (bytes == null || bytes.Length == 0)
                throw new InvalidOperationException("Float32 Gameplay Ability artifact is empty.");
            RequireDefinitionGuid(expectation.DefinitionGuid);
            StableHash bytesHash = ComputeCanonicalBytesHash(bytes);
            if (!bytesHash.IsValid || !string.Equals(bytesHash.ToString(), expectation.CanonicalBytesHash, StringComparison.Ordinal))
                throw new InvalidOperationException("Float32 Gameplay Ability artifact canonical bytes hash is invalid.");
            var reader = new CanonicalReader(bytes);
            if (reader.ReadUInt32() != ArtifactMagic)
                throw new InvalidDataException("Float32 Gameplay Ability artifact magic is invalid.");
            if (reader.ReadInt32() != ArtifactVersion)
                throw new InvalidDataException("Float32 Gameplay Ability artifact version is unsupported.");
            string compilerVersion = reader.ReadString();
            string operationSetVersion = reader.ReadString();
            string sourceRevision = reader.ReadString();
            string semanticHash = reader.ReadString();
            SimulationNumericProfile numericProfile = Float32GameplayAbilityNumericProfileCodec.Read(reader);
            string executionIdentity = reader.ReadString();
            string contentHash = reader.ReadString();
            string stateSchemaHash = reader.ReadString();
            GameplayAbilityCapabilityManifest capabilities = ReadCapabilities(reader);
            GameplayAbilityRootDescriptor root = GameplayAbilityRootDescriptorCodec.Read(reader);
            string abilityId = reader.ReadString();
            byte[] payload = reader.ReadBytes();
            reader.RequireComplete();
            RequireMetadata(
                compilerVersion,
                operationSetVersion,
                sourceRevision,
                semanticHash,
                numericProfile,
                executionIdentity,
                contentHash,
                stateSchemaHash,
                root,
                expectation);
            CharacterSkillId expectedAbilityId = new CharacterSkillId(expectation.AbilityId);
            if (!string.Equals(abilityId, expectedAbilityId.Value, StringComparison.Ordinal))
                throw new InvalidDataException("Float32 Gameplay Ability artifact Ability identity is inconsistent.");
            Float32GameplayAbilityExecutionData data = ReadPayload(payload);
            if (!string.Equals(data.CompilerVersion, compilerVersion, StringComparison.Ordinal) ||
                !data.OperationSetVersion.Equals(new OperationSetVersion(operationSetVersion)) ||
                !data.SourceRevision.Equals(new ProgramRevision(sourceRevision)) ||
                !data.SemanticHash.Equals(new SemanticHash(new StableHash(semanticHash))) ||
                data.NumericProfile != numericProfile ||
                !string.Equals(data.ExecutionIdentity, executionIdentity, StringComparison.Ordinal) ||
                !data.ContentHash.Equals(new StableHash(contentHash)) ||
                !data.StateSchemaHash.Equals(new StableHash(stateSchemaHash)) ||
                data.Root != root ||
                !data.AbilityId.Equals(expectedAbilityId) ||
                !CapabilitiesEqual(data.Capabilities, capabilities))
                throw new InvalidDataException("Float32 Gameplay Ability artifact header does not match its payload.");
            return data;
        }

        public static StableHash ComputeCanonicalBytesHash(byte[] bytes)
        {
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes));
            using var writer = new CanonicalWriter();
            writer.WriteRawBytes(bytes, 0, bytes.Length);
            return writer.ComputeHash();
        }

        static byte[] WritePayload(Float32GameplayAbilityExecutionData data)
        {
            using var writer = new CanonicalWriter();
            writer.WriteInt32(PayloadVersion);
            writer.WriteString(data.CompilerVersion);
            writer.WriteString(data.OperationSetVersion.Value);
            writer.WriteInt32(data.TickRate);
            writer.WriteString(data.SourceRevision.Value);
            writer.WriteString(data.SemanticHash.ToString());
            Float32GameplayAbilityNumericProfileCodec.Write(writer, data.NumericProfile);
            writer.WriteString(data.ExecutionIdentity);
            writer.WriteString(data.ContentHash.ToString());
            writer.WriteString(data.StateSchemaHash.ToString());
            WriteCapabilities(writer, data.Capabilities);
            GameplayAbilityRootDescriptorCodec.Write(writer, data.Root);
            writer.WriteString(data.AbilityId.Value);
            WriteTable(writer, data.Constants, WriteConstant);
            WriteTable(writer, data.OperationDefinitions, WriteOperationDefinition);
            WriteTable(writer, data.Operations, WriteOperation);
            WriteTable(writer, data.ConstantInputBindings, WriteConstantInputBinding);
            WriteTable(writer, data.ControlFlow, WriteControlFlow);
            WriteTable(writer, data.References, WriteReference);
            WriteTable(writer, data.GraphCallFrames, WriteGraphCallFrame);
            WriteTable(writer, data.StateSlots, (target, value) => WriteStateSlot(target, value, true));
            WriteTable(writer, data.Scopes, WriteScope);
            WriteTable(writer, data.OutputChannels, WriteOutputChannel);
            WriteTable(writer, data.CatalogEntries, WriteCatalogEntry);
            WriteTable(writer, data.MotionModifiers, WriteMotionModifier);
            WriteSourceMapTable(writer, data.SourceMap);
            WriteTable(writer, data.Producers, WriteProducer);
            return writer.ToArray();
        }

        static Float32GameplayAbilityExecutionData ReadPayload(
            byte[] bytes)
        {
            var reader = new CanonicalReader(bytes);
            if (reader.ReadInt32() != PayloadVersion)
                throw new InvalidDataException("Float32 Gameplay Ability payload version is unsupported.");
            string compilerVersion = reader.ReadString();
            OperationSetVersion operationSetVersion = new OperationSetVersion(reader.ReadString());
            int tickRate = reader.ReadInt32();
            ProgramRevision sourceRevision = new ProgramRevision(reader.ReadString());
            SemanticHash semanticHash = new SemanticHash(new StableHash(reader.ReadString()));
            SimulationNumericProfile numericProfile = Float32GameplayAbilityNumericProfileCodec.Read(reader);
            string executionIdentity = reader.ReadString();
            StableHash contentHash = new StableHash(reader.ReadString());
            StableHash stateSchemaHash = new StableHash(reader.ReadString());
            GameplayAbilityCapabilityManifest capabilities = ReadCapabilities(reader);
            GameplayAbilityRootDescriptor root = GameplayAbilityRootDescriptorCodec.Read(reader);
            CharacterSkillId abilityId = new CharacterSkillId(reader.ReadString());
            ProgramConstant[] constants = ReadTable(reader, ReadConstant);
            SimulationOperationDefinition[] operationDefinitions = ReadTable(reader, ReadOperationDefinition);
            int operationCount = ReadCount(reader);
            var operations = new SimulationOperation[operationCount];
            for (int i = 0; i < operationCount; i++)
                operations[i] = ReadOperation(reader, operationDefinitions);
            ProgramConstantInputBinding[] constantInputBindings = ReadTable(reader, ReadConstantInputBinding);
            ProgramControlFlowEdge[] controlFlow = ReadTable(reader, ReadControlFlow);
            ProgramReference[] references = ReadTable(reader, ReadReference);
            ProgramGraphCallFrame[] graphCallFrames = ReadTable(reader, ReadGraphCallFrame);
            ProgramStateSlot[] stateSlots = ReadTable(reader, ReadStateSlot);
            ProgramScopeLayout[] scopes = ReadTable(reader, ReadScope);
            ProgramOutputChannelLayout[] outputChannels = ReadTable(reader, ReadOutputChannel);
            ProgramCatalogEntry[] catalogEntries = ReadTable(reader, ReadCatalogEntry);
            ProgramMotionModifierDescriptor[] motionModifiers = ReadTable(reader, ReadMotionModifier);
            ProgramSourceMapEntry[] sourceMap = ReadSourceMapTable(reader);
            ProgramProducer[] producers = ReadTable(reader, ReadProducer);
            reader.RequireComplete();
            GameplayAbilityExecutionCatalog catalog = new GameplayAbilityExecutionCatalog(catalogEntries, references);
            GameplayAbilityExecutionBinding binding = catalog.Require(abilityId);
            GameplayAbilityProviderContract providerContract = GameplayAbilityProviderContract.Create(
                catalogEntries,
                index => constants[index].Int32);
            Float32GameplayAbilityExecutionData data = Float32GameplayAbilityExecutionData.Create(
                abilityId,
                binding,
                providerContract,
                compilerVersion,
                operationSetVersion,
                tickRate,
                sourceRevision,
                semanticHash,
                numericProfile,
                capabilities,
                root,
                executionIdentity,
                stateSchemaHash,
                operationDefinitions,
                operations,
                constants,
                constantInputBindings,
                controlFlow,
                references,
                graphCallFrames,
                stateSlots,
                scopes,
                outputChannels,
                catalogEntries,
                motionModifiers,
                sourceMap,
                producers);
            if (!data.ContentHash.Equals(contentHash))
                throw new InvalidDataException("Float32 Gameplay Ability payload content hash is invalid.");
            return data;
        }

        static void RequireMetadata(
            string compilerVersion,
            string operationSetVersion,
            string sourceRevision,
            string semanticHash,
            SimulationNumericProfile numericProfile,
            string executionIdentity,
            string contentHash,
            string stateSchemaHash,
            GameplayAbilityRootDescriptor root,
            Float32GameplayAbilityExecutionDataLoadExpectation expectation)
        {
            if (!string.Equals(compilerVersion, expectation.CompilerVersion, StringComparison.Ordinal) ||
                !string.Equals(operationSetVersion, expectation.OperationSetVersion, StringComparison.Ordinal) ||
                !string.Equals(sourceRevision, expectation.SourceRevision, StringComparison.Ordinal) ||
                !string.Equals(semanticHash, expectation.SemanticHash, StringComparison.Ordinal) ||
                !string.Equals(numericProfile.Id.Value, expectation.NumericProfileId, StringComparison.Ordinal) ||
                numericProfile.AbiVersion.Value != expectation.TargetAbiVersion ||
                !string.Equals(executionIdentity, expectation.ExecutionIdentity, StringComparison.Ordinal) ||
                !string.Equals(contentHash, expectation.ExecutionDataHash, StringComparison.Ordinal) ||
                !string.Equals(stateSchemaHash, expectation.StateSchemaHash, StringComparison.Ordinal) ||
                root != expectation.Root ||
                !root.IsAbility ||
                !string.Equals(root.RootIdentity, expectation.DefinitionGuid, StringComparison.Ordinal) ||
                !string.Equals(root.EntryIdentity, "ability:" + expectation.AbilityId, StringComparison.Ordinal))
                throw new InvalidDataException("Float32 Gameplay Ability artifact metadata is invalid.");
        }

        static void WriteCapabilities(CanonicalWriter writer, GameplayAbilityCapabilityManifest capabilities)
        {
            writer.WriteInt32(capabilities.GameplayCapabilities.Count);
            for (int i = 0; i < capabilities.GameplayCapabilities.Count; i++)
                writer.WriteString(capabilities.GameplayCapabilities[i]);
        }

        static GameplayAbilityCapabilityManifest ReadCapabilities(CanonicalReader reader)
        {
            int count = ReadCount(reader);
            var values = new string[count];
            for (int i = 0; i < count; i++)
                values[i] = reader.ReadString();
            return new GameplayAbilityCapabilityManifest(values);
        }

        static bool CapabilitiesEqual(
            GameplayAbilityCapabilityManifest left,
            GameplayAbilityCapabilityManifest right)
        {
            if (left.GameplayCapabilities.Count != right.GameplayCapabilities.Count)
                return false;
            for (int i = 0; i < left.GameplayCapabilities.Count; i++)
            {
                if (!string.Equals(left.GameplayCapabilities[i], right.GameplayCapabilities[i], StringComparison.Ordinal))
                    return false;
            }
            return true;
        }

        static void RequireDefinitionGuid(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length != 32)
                throw new InvalidDataException("Gameplay Ability definition GUID is invalid.");
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (!((c >= '0' && c <= '9') ||
                      (c >= 'a' && c <= 'f') ||
                      (c >= 'A' && c <= 'F')))
                    throw new InvalidDataException("Gameplay Ability definition GUID is invalid.");
            }
        }

        sealed class Float32GameplayAbilityNumericProfileCodec
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
                    ReadRoundingMode(reader.ReadByte()),
                    ReadOverflowMode(reader.ReadByte()),
                    reader.ReadBoolean());
            }

            static SimulationNumericRoundingMode ReadRoundingMode(byte value)
            {
                if (value < (byte)SimulationNumericRoundingMode.Ieee754NearestEven ||
                    value > (byte)SimulationNumericRoundingMode.FixedNearestEven)
                    throw new InvalidDataException($"Enum value '{value}' is invalid for 'SimulationNumericRoundingMode'.");
                return (SimulationNumericRoundingMode)value;
            }

            static SimulationNumericOverflowMode ReadOverflowMode(byte value)
            {
                if (value < (byte)SimulationNumericOverflowMode.RejectNonFinite ||
                    value > (byte)SimulationNumericOverflowMode.RejectOverflow)
                    throw new InvalidDataException($"Enum value '{value}' is invalid for 'SimulationNumericOverflowMode'.");
                return (SimulationNumericOverflowMode)value;
            }
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
            var kind = (SemanticValueKind)kindValue;
            if (kind < SemanticValueKind.Boolean || kind > SemanticValueKind.Identity)
                throw new InvalidDataException($"Program constant input contains unknown value kind '{kindValue}'.");
            return new ProgramConstantInputBinding(operation, port, constant, kind);
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
                strings.Add(value.GraphInvocationPath);
                strings.Add(value.CompiledPortId);
                strings.Add(value.SourceInvocationPath);
                strings.Add(value.ParentInvocationPath);
                strings.Add(value.InvocationCallerId);
                strings.Add(value.InvocationCallerClipId);
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
                writer.WriteInt32(stringIndex[value.GraphInvocationPath]);
                writer.WriteInt32(stringIndex[value.CompiledPortId]);
                writer.WriteByte((byte)value.ValuePortDirection);
                writer.WriteInt32(stringIndex[value.SourceInvocationPath]);
                writer.WriteInt32(stringIndex[value.ParentInvocationPath]);
                writer.WriteByte((byte)value.InvocationCallerKind);
                writer.WriteInt32(stringIndex[value.InvocationCallerId]);
                writer.WriteInt32(stringIndex[value.InvocationCallerClipId]);
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
                ProgramSourceTargetKind targetKind = ReadSourceTargetKind(reader.ReadByte());
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
                string graphInvocationPath = ReadSourceMapString(reader, strings);
                string compiledPortId = ReadSourceMapString(reader, strings);
                ProgramValuePortDirection valuePortDirection = ReadValuePortDirection(reader.ReadByte());
                string sourceInvocationPath = ReadSourceMapString(reader, strings);
                string parentInvocationPath = ReadSourceMapString(reader, strings);
                ProgramInvocationCallerKind callerKind = ReadInvocationCallerKind(reader.ReadByte());
                string callerId = ReadSourceMapString(reader, strings);
                string callerClipId = ReadSourceMapString(reader, strings);
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
                    contentHash,
                    graphInvocationPath,
                    compiledPortId,
                    valuePortDirection,
                    sourceInvocationPath,
                    parentInvocationPath,
                    callerKind,
                    callerId,
                    callerClipId);
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
            ProgramConstantKind kind = ReadConstantKind(reader.ReadByte());
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

        static ProgramSourceTargetKind ReadSourceTargetKind(byte value)
        {
            return value switch
            {
                1 => ProgramSourceTargetKind.Operation,
                2 => ProgramSourceTargetKind.Constant,
                3 => ProgramSourceTargetKind.StateSlot,
                4 => ProgramSourceTargetKind.Reference,
                5 => ProgramSourceTargetKind.Producer,
                6 => ProgramSourceTargetKind.CatalogEntry,
                7 => ProgramSourceTargetKind.BodyMotion,
                8 => ProgramSourceTargetKind.ControlModule,
                10 => ProgramSourceTargetKind.ControlTransition,
                11 => ProgramSourceTargetKind.OperationPort,
                12 => ProgramSourceTargetKind.GraphInvocation,
                13 => ProgramSourceTargetKind.OptimizedAway,
                _ => throw new InvalidDataException($"Enum value '{value}' is invalid for 'ProgramSourceTargetKind'.")
            };
        }

        static ProgramValuePortDirection ReadValuePortDirection(byte value)
        {
            if (value > (byte)ProgramValuePortDirection.Output)
                throw new InvalidDataException($"Enum value '{value}' is invalid for 'ProgramValuePortDirection'.");
            return (ProgramValuePortDirection)value;
        }

        static ProgramInvocationCallerKind ReadInvocationCallerKind(byte value)
        {
            if (value > (byte)ProgramInvocationCallerKind.PresentationMarker)
                throw new InvalidDataException($"Enum value '{value}' is invalid for 'ProgramInvocationCallerKind'.");
            return (ProgramInvocationCallerKind)value;
        }

        static ProgramConstantKind ReadConstantKind(byte value)
        {
            if (value < (byte)ProgramConstantKind.Boolean || value > (byte)ProgramConstantKind.Bytes)
                throw new InvalidDataException($"Enum value '{value}' is invalid for 'ProgramConstantKind'.");
            return (ProgramConstantKind)value;
        }

        static SimulationOperationCode ReadOperationCode(int value)
        {
            if (value < 0 || value > ushort.MaxValue || !GameplayAbilityOperationSet.IsOperation((SimulationOperationCode)value))
                throw new InvalidDataException($"Enum value '{value}' is invalid for 'SimulationOperationCode'.");
            return (SimulationOperationCode)value;
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
                ReadOperationCode(reader.ReadInt32()),
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
