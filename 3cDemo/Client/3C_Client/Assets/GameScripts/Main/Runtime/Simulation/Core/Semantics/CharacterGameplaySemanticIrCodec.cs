using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;

namespace ThirdPersonSimulation
{
    public sealed class SemanticIrArtifactVersionException : IOException
    {
        public SemanticIrArtifactVersionException(string message) : base(message) { }
    }

    public readonly struct SemanticIrLoadExpectation
    {
        public SemanticIrLoadExpectation(
            ProgramId programId,
            string compilerVersion,
            OperationSetVersion operationSetVersion,
            int tickRate,
            ProgramRevision sourceRevision,
            SemanticHash semanticHash)
        {
            if (!programId.IsValid)
                throw new ArgumentException("Program id is required.", nameof(programId));
            CompilerVersion = SimulationIdentity.Require(compilerVersion, nameof(compilerVersion));
            if (!operationSetVersion.IsValid)
                throw new ArgumentException("Operation-set version is required.", nameof(operationSetVersion));
            if (tickRate <= 0)
                throw new ArgumentOutOfRangeException(nameof(tickRate));
            if (string.IsNullOrEmpty(sourceRevision.Value))
                throw new ArgumentException("Source revision is required.", nameof(sourceRevision));
            if (!semanticHash.IsValid)
                throw new ArgumentException("Semantic hash is required.", nameof(semanticHash));
            ProgramId = programId;
            OperationSetVersion = operationSetVersion;
            TickRate = tickRate;
            SourceRevision = sourceRevision;
            SemanticHash = semanticHash;
        }

        public ProgramId ProgramId { get; }
        public string CompilerVersion { get; }
        public OperationSetVersion OperationSetVersion { get; }
        public int TickRate { get; }
        public ProgramRevision SourceRevision { get; }
        public SemanticHash SemanticHash { get; }
    }

    public sealed class CharacterGameplaySemanticIrArtifactHeader
    {
        readonly ReadOnlyCollection<string> m_GameplayCapabilities;

        internal CharacterGameplaySemanticIrArtifactHeader(
            uint magic,
            int artifactVersion,
            int payloadVersion,
            ProgramId programId,
            string compilerVersion,
            OperationSetVersion operationSetVersion,
            int tickRate,
            ProgramRevision sourceRevision,
            SemanticHash semanticHash,
            IEnumerable<string> gameplayCapabilities,
            WorldCapability requiredWorldCapabilities)
        {
            Magic = magic;
            ArtifactVersion = artifactVersion;
            PayloadVersion = payloadVersion;
            ProgramId = programId;
            CompilerVersion = SimulationIdentity.Require(compilerVersion, nameof(compilerVersion));
            OperationSetVersion = operationSetVersion;
            TickRate = tickRate;
            SourceRevision = sourceRevision;
            SemanticHash = semanticHash;
            var capabilities = new ProgramCapabilityManifest(gameplayCapabilities, requiredWorldCapabilities);
            m_GameplayCapabilities = new List<string>(capabilities.GameplayCapabilities).AsReadOnly();
            RequiredWorldCapabilities = capabilities.RequiredWorldCapabilities;
        }

        public uint Magic { get; }
        public int ArtifactVersion { get; }
        public int PayloadVersion { get; }
        public ProgramId ProgramId { get; }
        public string CompilerVersion { get; }
        public OperationSetVersion OperationSetVersion { get; }
        public int TickRate { get; }
        public ProgramRevision SourceRevision { get; }
        public SemanticHash SemanticHash { get; }
        public IReadOnlyList<string> GameplayCapabilities => m_GameplayCapabilities;
        public WorldCapability RequiredWorldCapabilities { get; }
    }

    public sealed class ValidatedSemanticIrArtifact
    {
        readonly byte[] m_CanonicalBytes;

        internal ValidatedSemanticIrArtifact(
            CharacterGameplaySemanticIrArtifactHeader header,
            byte[] canonicalBytes,
            CharacterGameplaySemanticIr semanticIr)
        {
            Header = header ?? throw new ArgumentNullException(nameof(header));
            m_CanonicalBytes = canonicalBytes == null ? throw new ArgumentNullException(nameof(canonicalBytes)) : (byte[])canonicalBytes.Clone();
            SemanticIr = semanticIr ?? throw new ArgumentNullException(nameof(semanticIr));
        }

        public CharacterGameplaySemanticIrArtifactHeader Header { get; }
        public ReadOnlyMemory<byte> CanonicalBytes => m_CanonicalBytes;
        public CharacterGameplaySemanticIr SemanticIr { get; }
        public byte[] ToArray() => (byte[])m_CanonicalBytes.Clone();
    }

    public static class CharacterGameplaySemanticIrCodec
    {
        const uint ArtifactMagic = 0x52495343;
        const int ArtifactVersion = 12;
        const int PayloadVersion = 12;

        public static byte[] WriteArtifact(CharacterGameplaySemanticIr semanticIr)
        {
            return CreateValidatedArtifact(semanticIr).ToArray();
        }

        public static ValidatedSemanticIrArtifact CreateValidatedArtifact(CharacterGameplaySemanticIr semanticIr)
        {
            if (semanticIr == null)
                throw new ArgumentNullException(nameof(semanticIr));
            byte[] payload = WritePayload(semanticIr);
            using var writer = new CanonicalWriter();
            writer.WriteUInt32(ArtifactMagic);
            writer.WriteInt32(ArtifactVersion);
            writer.WriteInt32(PayloadVersion);
            writer.WriteString(semanticIr.Manifest.ProgramId.Value);
            writer.WriteString(semanticIr.Manifest.CompilerVersion);
            writer.WriteString(semanticIr.Manifest.OperationSetVersion.Value);
            writer.WriteInt32(semanticIr.Manifest.TickRate);
            writer.WriteString(semanticIr.Manifest.SourceRevision.Value);
            writer.WriteString(semanticIr.SemanticHash.ToString());
            writer.WriteInt32(semanticIr.Manifest.Capabilities.GameplayCapabilities.Count);
            for (int i = 0; i < semanticIr.Manifest.Capabilities.GameplayCapabilities.Count; i++)
                writer.WriteString(semanticIr.Manifest.Capabilities.GameplayCapabilities[i]);
            writer.WriteUInt64((ulong)semanticIr.Manifest.Capabilities.RequiredWorldCapabilities);
            writer.WriteBytes(payload);
            byte[] bytes = writer.ToArray();
            return ReadValidatedArtifact(
                bytes,
                new SemanticIrLoadExpectation(
                    semanticIr.Manifest.ProgramId,
                    semanticIr.Manifest.CompilerVersion,
                    semanticIr.Manifest.OperationSetVersion,
                    semanticIr.Manifest.TickRate,
                    semanticIr.Manifest.SourceRevision,
                    semanticIr.SemanticHash));
        }

        public static CharacterGameplaySemanticIrArtifactHeader ReadArtifactHeader(byte[] bytes)
        {
            return ReadEnvelope(bytes).Header;
        }

        public static ValidatedSemanticIrArtifact ReadValidatedArtifact(byte[] bytes)
        {
            ArtifactEnvelope envelope = ReadEnvelope(bytes);
            CharacterGameplaySemanticIr semanticIr = ReadPayload(envelope.Payload);
            ValidateHeaderAgainstPayload(envelope.Header, semanticIr);
            return new ValidatedSemanticIrArtifact(envelope.Header, bytes, semanticIr);
        }

        public static ValidatedSemanticIrArtifact ReadValidatedArtifact(byte[] bytes, SemanticIrLoadExpectation expectation)
        {
            ValidatedSemanticIrArtifact artifact = ReadValidatedArtifact(bytes);
            ValidateExpectation(artifact.Header, expectation);
            return artifact;
        }

        public static CharacterGameplaySemanticIr ReadArtifact(byte[] bytes, SemanticIrLoadExpectation expectation)
        {
            return ReadValidatedArtifact(bytes, expectation).SemanticIr;
        }

        static ArtifactEnvelope ReadEnvelope(byte[] bytes)
        {
            var reader = new CanonicalReader(bytes);
            uint magic = reader.ReadUInt32();
            if (magic != ArtifactMagic)
                throw new InvalidDataException("Gameplay Semantic IR artifact magic is invalid.");
            int artifactVersion = reader.ReadInt32();
            if (artifactVersion != ArtifactVersion)
                throw new SemanticIrArtifactVersionException("Gameplay Semantic IR artifact version is unsupported.");
            int payloadVersion = reader.ReadInt32();
            if (payloadVersion != PayloadVersion)
                throw new SemanticIrArtifactVersionException("Gameplay Semantic IR payload version is unsupported.");
            var programId = new ProgramId(reader.ReadString());
            string compilerVersion = reader.ReadString();
            var operationSetVersion = new OperationSetVersion(reader.ReadString());
            int tickRate = reader.ReadInt32();
            if (tickRate <= 0)
                throw new InvalidDataException("Gameplay Semantic IR tick rate is invalid.");
            var sourceRevision = new ProgramRevision(reader.ReadString());
            var semanticHash = new SemanticHash(new StableHash(reader.ReadString()));
            int capabilityCount = SimulationProgramSemanticsCodec.ReadCount(reader);
            var gameplayCapabilities = new string[capabilityCount];
            for (int i = 0; i < capabilityCount; i++)
                gameplayCapabilities[i] = reader.ReadString();
            ulong worldCapabilityValue = reader.ReadUInt64();
            const WorldCapability knownWorldCapabilities = WorldCapability.BodyMotion |
                                                           WorldCapability.Grounding |
                                                           WorldCapability.Collision |
                                                           WorldCapability.Reconstructible |
                                                           WorldCapability.Snapshotable |
                                                           WorldCapability.DeterministicReplay |
                                                           WorldCapability.AirborneVerticalMotion;
            if ((worldCapabilityValue & ~(ulong)knownWorldCapabilities) != 0)
                throw new InvalidDataException("Gameplay Semantic IR world capability mask is invalid.");
            byte[] payload = reader.ReadBytes();
            reader.RequireComplete();
            var header = new CharacterGameplaySemanticIrArtifactHeader(
                magic,
                artifactVersion,
                payloadVersion,
                programId,
                compilerVersion,
                operationSetVersion,
                tickRate,
                sourceRevision,
                semanticHash,
                gameplayCapabilities,
                (WorldCapability)worldCapabilityValue);
            if (!SequenceEqual(gameplayCapabilities, header.GameplayCapabilities))
                throw new InvalidDataException("Gameplay Semantic IR capability identities are not in canonical order.");
            return new ArtifactEnvelope(header, payload);
        }

        static void ValidateHeaderAgainstPayload(CharacterGameplaySemanticIrArtifactHeader header, CharacterGameplaySemanticIr semanticIr)
        {
            CharacterGameplaySemanticIrManifest manifest = semanticIr.Manifest;
            if (!manifest.ProgramId.Equals(header.ProgramId) ||
                !string.Equals(manifest.CompilerVersion, header.CompilerVersion, StringComparison.Ordinal) ||
                !manifest.OperationSetVersion.Equals(header.OperationSetVersion) ||
                manifest.TickRate != header.TickRate ||
                !manifest.SourceRevision.Equals(header.SourceRevision) ||
                !semanticIr.SemanticHash.Equals(header.SemanticHash) ||
                manifest.Capabilities.RequiredWorldCapabilities != header.RequiredWorldCapabilities ||
                !SequenceEqual(manifest.Capabilities.GameplayCapabilities, header.GameplayCapabilities))
            {
                throw new InvalidDataException("Semantic IR artifact header does not match its payload manifest.");
            }
        }

        static void ValidateExpectation(CharacterGameplaySemanticIrArtifactHeader header, SemanticIrLoadExpectation expectation)
        {
            if (!header.ProgramId.Equals(expectation.ProgramId))
                throw new InvalidDataException($"Semantic IR ProgramId '{header.ProgramId}' does not match expected '{expectation.ProgramId}'.");
            if (!string.Equals(header.CompilerVersion, expectation.CompilerVersion, StringComparison.Ordinal))
                throw new InvalidDataException($"Semantic IR compiler version '{header.CompilerVersion}' does not match expected '{expectation.CompilerVersion}'.");
            if (!header.OperationSetVersion.Equals(expectation.OperationSetVersion))
                throw new InvalidDataException($"Semantic IR operation-set version '{header.OperationSetVersion}' does not match expected '{expectation.OperationSetVersion}'.");
            if (header.TickRate != expectation.TickRate)
                throw new InvalidDataException($"Semantic IR tick rate '{header.TickRate}' does not match expected '{expectation.TickRate}'.");
            if (!header.SourceRevision.Equals(expectation.SourceRevision))
                throw new InvalidDataException($"Semantic IR source revision '{header.SourceRevision}' does not match expected '{expectation.SourceRevision}'.");
            if (!header.SemanticHash.Equals(expectation.SemanticHash))
                throw new InvalidDataException($"Semantic IR hash '{header.SemanticHash}' does not match expected '{expectation.SemanticHash}'.");
        }

        static bool SequenceEqual(IReadOnlyList<string> left, IReadOnlyList<string> right)
        {
            if (left.Count != right.Count)
                return false;
            for (int i = 0; i < left.Count; i++)
            {
                if (!string.Equals(left[i], right[i], StringComparison.Ordinal))
                    return false;
            }
            return true;
        }

        public static SemanticHash ComputeHash(CharacterGameplaySemanticIr semanticIr)
        {
            using var writer = new CanonicalWriter();
            WritePayload(writer, semanticIr);
            return new SemanticHash(writer.ComputeHash());
        }

        static byte[] WritePayload(CharacterGameplaySemanticIr semanticIr)
        {
            using var writer = new CanonicalWriter();
            WritePayload(writer, semanticIr);
            return writer.ToArray();
        }

        static void WritePayload(CanonicalWriter writer, CharacterGameplaySemanticIr semanticIr)
        {
            writer.WriteInt32(PayloadVersion);
            WriteManifest(writer, semanticIr.Manifest);
            WriteBodyMotion(writer, semanticIr.BodyMotion);
            WriteTable(writer, semanticIr.Literals, WriteLiteral);
            WriteTable(writer, semanticIr.Operations, WriteOperation);
            WriteTable(writer, semanticIr.ConstantInputBindings, WriteConstantInputBinding);
            WriteTable(writer, semanticIr.ControlFlow, SimulationProgramSemanticsCodec.WriteControlFlow);
            WriteTable(writer, semanticIr.References, SimulationProgramSemanticsCodec.WriteReference);
            WriteTable(writer, semanticIr.StateDeclarations, (target, value) => SimulationProgramSemanticsCodec.WriteStateSlot(target, value, true));
            WriteTable(writer, semanticIr.Scopes, SimulationProgramSemanticsCodec.WriteScope);
            WriteTable(writer, semanticIr.WorldRequests, SimulationProgramSemanticsCodec.WriteWorldRequest);
            WriteTable(writer, semanticIr.OutputChannels, SimulationProgramSemanticsCodec.WriteOutputChannel);
            WriteTable(writer, semanticIr.CatalogEntries, SimulationProgramSemanticsCodec.WriteCatalogEntry);
            WriteTable(writer, semanticIr.SourceMap, SimulationProgramSemanticsCodec.WriteSourceMap);
            WriteTable(writer, semanticIr.Producers, SimulationProgramSemanticsCodec.WriteProducer);
        }

        static CharacterGameplaySemanticIr ReadPayload(byte[] bytes)
        {
            var reader = new CanonicalReader(bytes);
            if (reader.ReadInt32() != PayloadVersion)
                throw new SemanticIrArtifactVersionException("Gameplay Semantic IR payload version is unsupported.");
            CharacterGameplaySemanticIrManifest manifest = ReadManifest(reader);
            CharacterBodyMotionSemanticDescriptor bodyMotion = ReadBodyMotion(reader);
            SemanticLiteral[] literals = ReadTable(reader, ReadLiteral);
            SemanticOperation[] operations = ReadTable(reader, ReadOperation);
            SemanticConstantInputBinding[] constantInputBindings = ReadTable(reader, ReadConstantInputBinding);
            ProgramControlFlowEdge[] controlFlow = ReadTable(reader, SimulationProgramSemanticsCodec.ReadControlFlow);
            ProgramReference[] references = ReadTable(reader, SimulationProgramSemanticsCodec.ReadReference);
            ProgramStateSlot[] stateDeclarations = ReadTable(reader, SimulationProgramSemanticsCodec.ReadStateSlot);
            ProgramScopeLayout[] scopes = ReadTable(reader, SimulationProgramSemanticsCodec.ReadScope);
            ProgramWorldRequestLayout[] worldRequests = ReadTable(reader, SimulationProgramSemanticsCodec.ReadWorldRequest);
            ProgramOutputChannelLayout[] outputChannels = ReadTable(reader, SimulationProgramSemanticsCodec.ReadOutputChannel);
            ProgramCatalogEntry[] catalogEntries = ReadTable(reader, SimulationProgramSemanticsCodec.ReadCatalogEntry);
            ProgramSourceMapEntry[] sourceMap = ReadTable(reader, SimulationProgramSemanticsCodec.ReadSourceMap);
            ProgramProducer[] producers = ReadTable(reader, SimulationProgramSemanticsCodec.ReadProducer);
            reader.RequireComplete();
            return new CharacterGameplaySemanticIr(
                manifest,
                bodyMotion,
                operations,
                literals,
                constantInputBindings,
                controlFlow,
                references,
                stateDeclarations,
                scopes,
                worldRequests,
                outputChannels,
                catalogEntries,
                sourceMap,
                producers);
        }

        static void WriteBodyMotion(CanonicalWriter writer, CharacterBodyMotionSemanticDescriptor descriptor)
        {
            writer.WriteString(descriptor.SourceIdentity);
            writer.WriteString(descriptor.ContentRevision.Value);
            writer.WriteInt32(descriptor.SemanticVersion);
            writer.WriteDouble(descriptor.GravityAcceleration);
            writer.WriteDouble(descriptor.MaximumFallSpeed);
        }

        static CharacterBodyMotionSemanticDescriptor ReadBodyMotion(CanonicalReader reader)
        {
            return new CharacterBodyMotionSemanticDescriptor(
                reader.ReadString(),
                new StableHash(reader.ReadString()),
                reader.ReadInt32(),
                reader.ReadDouble(),
                reader.ReadDouble());
        }

        static void WriteConstantInputBinding(CanonicalWriter writer, SemanticConstantInputBinding binding)
        {
            writer.WriteInt32(binding.TargetOperation.Value);
            writer.WriteString(binding.TargetPort);
            writer.WriteInt32(binding.ConstantIndex);
            writer.WriteByte((byte)binding.ResolvedValueKind);
        }

        static SemanticConstantInputBinding ReadConstantInputBinding(CanonicalReader reader)
        {
            var operation = new OperationHandle(reader.ReadInt32());
            string port = reader.ReadString();
            int constant = reader.ReadInt32();
            byte kindValue = reader.ReadByte();
            if (!Enum.IsDefined(typeof(SemanticValueKind), kindValue))
                throw new InvalidDataException($"Semantic constant input contains unknown value kind '{kindValue}'.");
            return new SemanticConstantInputBinding(operation, port, constant, (SemanticValueKind)kindValue);
        }

        static void WriteManifest(CanonicalWriter writer, CharacterGameplaySemanticIrManifest manifest)
        {
            writer.WriteString(manifest.ProgramId.Value);
            writer.WriteString(manifest.CompilerVersion);
            writer.WriteString(manifest.OperationSetVersion.Value);
            writer.WriteInt32(manifest.TickRate);
            writer.WriteString(manifest.SourceRevision.Value);
            writer.WriteInt32(manifest.Capabilities.GameplayCapabilities.Count);
            for (int i = 0; i < manifest.Capabilities.GameplayCapabilities.Count; i++)
                writer.WriteString(manifest.Capabilities.GameplayCapabilities[i]);
            writer.WriteUInt64((ulong)manifest.Capabilities.RequiredWorldCapabilities);
        }

        static CharacterGameplaySemanticIrManifest ReadManifest(CanonicalReader reader)
        {
            var programId = new ProgramId(reader.ReadString());
            string compilerVersion = reader.ReadString();
            var operationSetVersion = new OperationSetVersion(reader.ReadString());
            int tickRate = reader.ReadInt32();
            var sourceRevision = new ProgramRevision(reader.ReadString());
            int capabilityCount = SimulationProgramSemanticsCodec.ReadCount(reader);
            var gameplayCapabilities = new string[capabilityCount];
            for (int i = 0; i < capabilityCount; i++)
                gameplayCapabilities[i] = reader.ReadString();
            return new CharacterGameplaySemanticIrManifest(
                programId,
                compilerVersion,
                operationSetVersion,
                tickRate,
                sourceRevision,
                new ProgramCapabilityManifest(gameplayCapabilities, (WorldCapability)reader.ReadUInt64()));
        }

        static void WriteLiteral(CanonicalWriter writer, SemanticLiteral literal)
        {
            writer.WriteInt32(literal.Index);
            writer.WriteString(literal.Identity);
            writer.WriteByte((byte)literal.Kind);
            writer.WriteByte((byte)literal.Precision);
            switch (literal.Kind)
            {
                case SemanticLiteralKind.Boolean: writer.WriteBoolean(literal.Boolean); break;
                case SemanticLiteralKind.Int32: writer.WriteInt32(literal.Int32); break;
                case SemanticLiteralKind.UInt64: writer.WriteUInt64(literal.UInt64); break;
                case SemanticLiteralKind.Number: writer.WriteDouble(literal.X); break;
                case SemanticLiteralKind.Vector2: writer.WriteDouble(literal.X); writer.WriteDouble(literal.Y); break;
                case SemanticLiteralKind.Vector3: writer.WriteDouble(literal.X); writer.WriteDouble(literal.Y); writer.WriteDouble(literal.Z); break;
                case SemanticLiteralKind.Yaw: writer.WriteDouble(literal.X); break;
                case SemanticLiteralKind.String: writer.WriteString(literal.Text); break;
                case SemanticLiteralKind.Document: WriteDocument(writer, literal.Document); break;
                default: throw new InvalidDataException($"Unsupported Semantic literal kind '{literal.Kind}'.");
            }
        }

        static SemanticLiteral ReadLiteral(CanonicalReader reader)
        {
            int index = reader.ReadInt32();
            string identity = reader.ReadString();
            SemanticLiteralKind kind = SimulationProgramSemanticsCodec.ReadEnum<SemanticLiteralKind>(reader.ReadByte());
            SemanticNumericPrecision precision = SimulationProgramSemanticsCodec.ReadEnum<SemanticNumericPrecision>(reader.ReadByte());
            return kind switch
            {
                SemanticLiteralKind.Boolean => SemanticLiteral.FromBoolean(index, identity, reader.ReadBoolean()),
                SemanticLiteralKind.Int32 => SemanticLiteral.FromInt32(index, identity, reader.ReadInt32()),
                SemanticLiteralKind.UInt64 => SemanticLiteral.FromUInt64(index, identity, reader.ReadUInt64()),
                SemanticLiteralKind.Number => SemanticLiteral.FromNumber(index, identity, reader.ReadDouble(), precision),
                SemanticLiteralKind.Vector2 => SemanticLiteral.FromVector2(index, identity, reader.ReadDouble(), reader.ReadDouble(), precision),
                SemanticLiteralKind.Vector3 => SemanticLiteral.FromVector3(index, identity, reader.ReadDouble(), reader.ReadDouble(), reader.ReadDouble(), precision),
                SemanticLiteralKind.Yaw => SemanticLiteral.FromYaw(index, identity, reader.ReadDouble(), precision),
                SemanticLiteralKind.String => SemanticLiteral.FromString(index, identity, reader.ReadString()),
                SemanticLiteralKind.Document => SemanticLiteral.FromDocument(index, identity, ReadDocument(reader)),
                _ => throw new InvalidDataException($"Unsupported Semantic literal kind '{kind}'.")
            };
        }

        static void WriteDocument(CanonicalWriter writer, SemanticDataDocument document)
        {
            writer.WriteInt32(document.Tokens.Count);
            for (int i = 0; i < document.Tokens.Count; i++)
            {
                SemanticDataToken token = document.Tokens[i];
                writer.WriteByte((byte)token.Kind);
                switch (token.Kind)
                {
                    case SemanticDataTokenKind.Boolean: writer.WriteBoolean(token.Boolean); break;
                    case SemanticDataTokenKind.Int32: writer.WriteInt32(token.Int32); break;
                    case SemanticDataTokenKind.UInt32: writer.WriteUInt32(token.UInt32); break;
                    case SemanticDataTokenKind.UInt64: writer.WriteUInt64(token.UInt64); break;
                    case SemanticDataTokenKind.String: writer.WriteString(token.Text); break;
                    case SemanticDataTokenKind.Number:
                        writer.WriteDouble(token.Number);
                        writer.WriteString(token.SourceIdentity);
                        writer.WriteByte((byte)token.Precision);
                        break;
                    case SemanticDataTokenKind.Bytes: writer.WriteBytes(token.Bytes.ToArray()); break;
                    default: throw new InvalidDataException($"Unsupported Semantic document token '{token.Kind}'.");
                }
            }
        }

        static SemanticDataDocument ReadDocument(CanonicalReader reader)
        {
            int count = SimulationProgramSemanticsCodec.ReadCount(reader);
            var tokens = new SemanticDataToken[count];
            for (int i = 0; i < count; i++)
            {
                SemanticDataTokenKind kind = SimulationProgramSemanticsCodec.ReadEnum<SemanticDataTokenKind>(reader.ReadByte());
                tokens[i] = kind switch
                {
                    SemanticDataTokenKind.Boolean => SemanticDataToken.FromBoolean(reader.ReadBoolean()),
                    SemanticDataTokenKind.Int32 => SemanticDataToken.FromInt32(reader.ReadInt32()),
                    SemanticDataTokenKind.UInt32 => SemanticDataToken.FromUInt32(reader.ReadUInt32()),
                    SemanticDataTokenKind.UInt64 => SemanticDataToken.FromUInt64(reader.ReadUInt64()),
                    SemanticDataTokenKind.String => SemanticDataToken.FromString(reader.ReadString()),
                    SemanticDataTokenKind.Number => SemanticDataToken.FromNumber(reader.ReadDouble(), reader.ReadString(), SimulationProgramSemanticsCodec.ReadEnum<SemanticNumericPrecision>(reader.ReadByte())),
                    SemanticDataTokenKind.Bytes => SemanticDataToken.FromBytes(reader.ReadBytes()),
                    _ => throw new InvalidDataException($"Unsupported Semantic document token '{kind}'.")
                };
            }
            return new SemanticDataDocument(tokens);
        }

        static void WriteOperation(CanonicalWriter writer, SemanticOperation operation)
        {
            writer.WriteInt32(operation.Handle.Value);
            writer.WriteString(operation.TemplateIdentity);
            writer.WriteInt32((int)operation.Code);
            SimulationProgramSemanticsCodec.WriteIntArray(writer, operation.Operands);
            SimulationProgramSemanticsCodec.WriteIntArray(writer, operation.LiteralReferences);
            SimulationProgramSemanticsCodec.WriteIntArray(writer, operation.StateSlots);
            writer.WriteInt32(operation.Integer0);
            writer.WriteInt32(operation.Integer1);
            writer.WriteUInt64(operation.Unsigned0);
            writer.WriteDouble(operation.Number0);
            writer.WriteString(operation.Number0SourceIdentity);
            writer.WriteString(operation.Text0);
            writer.WriteUInt32(operation.Flags);
        }

        static SemanticOperation ReadOperation(CanonicalReader reader)
        {
            return new SemanticOperation(
                new OperationHandle(reader.ReadInt32()),
                reader.ReadString(),
                SimulationProgramSemanticsCodec.ReadEnum<SimulationOperationCode>(reader.ReadInt32()),
                SimulationProgramSemanticsCodec.ReadIntArray(reader),
                SimulationProgramSemanticsCodec.ReadIntArray(reader),
                SimulationProgramSemanticsCodec.ReadIntArray(reader),
                reader.ReadInt32(),
                reader.ReadInt32(),
                reader.ReadUInt64(),
                reader.ReadDouble(),
                reader.ReadString(),
                reader.ReadString(),
                reader.ReadUInt32());
        }

        static void WriteTable<T>(CanonicalWriter writer, IReadOnlyList<T> values, Action<CanonicalWriter, T> write)
        {
            writer.WriteInt32(values.Count);
            for (int i = 0; i < values.Count; i++)
                write(writer, values[i]);
        }

        static T[] ReadTable<T>(CanonicalReader reader, Func<CanonicalReader, T> read)
        {
            int count = SimulationProgramSemanticsCodec.ReadCount(reader);
            var values = new T[count];
            for (int i = 0; i < count; i++)
                values[i] = read(reader);
            return values;
        }

        readonly struct ArtifactEnvelope
        {
            public ArtifactEnvelope(CharacterGameplaySemanticIrArtifactHeader header, byte[] payload)
            {
                Header = header;
                Payload = payload;
            }

            public CharacterGameplaySemanticIrArtifactHeader Header { get; }
            public byte[] Payload { get; }
        }
    }
}
