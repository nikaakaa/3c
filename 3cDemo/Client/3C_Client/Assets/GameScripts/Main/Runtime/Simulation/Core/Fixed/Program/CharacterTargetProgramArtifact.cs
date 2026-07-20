using ThirdPersonSimulation;
using System;
using System.IO;
using System.Security.Cryptography;

namespace ThirdPersonSimulation.Fixed
{
    public readonly struct CharacterTargetProgramArtifactExpectation
    {
        public CharacterTargetProgramArtifactExpectation(
            string definitionGuid,
            ProgramId programId,
            string compilerVersion,
            OperationSetVersion operationSetVersion,
            ProgramRevision sourceRevision,
            SemanticHash semanticHash,
            SimulationNumericProfile numericProfile,
            ProgramHash programHash,
            LayoutHash layoutHash,
            WorldCapability requiredWorldCapabilities)
        {
            DefinitionGuid = CharacterTargetProgramArtifactLoader.RequireDefinitionGuid(definitionGuid);
            if (!programId.IsValid)
                throw new ArgumentException("ProgramId is required.", nameof(programId));
            ProgramId = programId;
            Program = new ProgramLoadExpectation(
                compilerVersion,
                operationSetVersion,
                sourceRevision,
                semanticHash,
                numericProfile);
            if (!programHash.IsValid)
                throw new ArgumentException("ProgramHash is required.", nameof(programHash));
            if (!layoutHash.IsValid)
                throw new ArgumentException("LayoutHash is required.", nameof(layoutHash));
            ProgramHash = programHash;
            LayoutHash = layoutHash;
            RequiredWorldCapabilities = requiredWorldCapabilities;
        }

        public string DefinitionGuid { get; }
        public ProgramId ProgramId { get; }
        public ProgramLoadExpectation Program { get; }
        public ProgramHash ProgramHash { get; }
        public LayoutHash LayoutHash { get; }
        public WorldCapability RequiredWorldCapabilities { get; }
    }

    public readonly struct CharacterTargetProgramArtifactDescriptor : IEquatable<CharacterTargetProgramArtifactDescriptor>
    {
        public CharacterTargetProgramArtifactDescriptor(
            string definitionGuid,
            CharacterSimulationProgram program,
            StableHash canonicalBytesHash,
            int canonicalByteLength)
        {
            if (program == null)
                throw new ArgumentNullException(nameof(program));
            if (!canonicalBytesHash.IsValid || canonicalByteLength <= 0)
                throw new ArgumentException("Canonical Program artifact identity is incomplete.");
            DefinitionGuid = CharacterTargetProgramArtifactLoader.RequireDefinitionGuid(definitionGuid);
            ProgramId = program.Manifest.ProgramId;
            CompilerVersion = program.Manifest.CompilerVersion;
            OperationSetVersion = program.Manifest.OperationSetVersion;
            SourceRevision = program.Manifest.SourceRevision;
            SemanticHash = program.Manifest.SemanticHash;
            NumericProfileId = program.Manifest.NumericProfile.Id;
            TargetAbiVersion = program.Manifest.NumericProfile.AbiVersion;
            ProgramHash = program.ProgramHash;
            LayoutHash = program.LayoutHash;
            RequiredWorldCapabilities = program.Manifest.Capabilities.RequiredWorldCapabilities;
            CanonicalBytesHash = canonicalBytesHash;
            CanonicalByteLength = canonicalByteLength;
        }

        public string DefinitionGuid { get; }
        public ProgramId ProgramId { get; }
        public string CompilerVersion { get; }
        public OperationSetVersion OperationSetVersion { get; }
        public ProgramRevision SourceRevision { get; }
        public SemanticHash SemanticHash { get; }
        public NumericProfileId NumericProfileId { get; }
        public TargetAbiVersion TargetAbiVersion { get; }
        public ProgramHash ProgramHash { get; }
        public LayoutHash LayoutHash { get; }
        public WorldCapability RequiredWorldCapabilities { get; }
        public StableHash CanonicalBytesHash { get; }
        public int CanonicalByteLength { get; }

        public bool Equals(CharacterTargetProgramArtifactDescriptor other)
        {
            return string.Equals(DefinitionGuid, other.DefinitionGuid, StringComparison.Ordinal) &&
                   ProgramId.Equals(other.ProgramId) &&
                   string.Equals(CompilerVersion, other.CompilerVersion, StringComparison.Ordinal) &&
                   OperationSetVersion.Equals(other.OperationSetVersion) &&
                   SourceRevision.Equals(other.SourceRevision) &&
                   SemanticHash.Equals(other.SemanticHash) &&
                   NumericProfileId.Equals(other.NumericProfileId) &&
                   TargetAbiVersion.Equals(other.TargetAbiVersion) &&
                   ProgramHash.Equals(other.ProgramHash) &&
                   LayoutHash.Equals(other.LayoutHash) &&
                   RequiredWorldCapabilities == other.RequiredWorldCapabilities &&
                   CanonicalBytesHash.Equals(other.CanonicalBytesHash) &&
                   CanonicalByteLength == other.CanonicalByteLength;
        }

        public override bool Equals(object obj) => obj is CharacterTargetProgramArtifactDescriptor other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(DefinitionGuid, ProgramId, ProgramHash, LayoutHash, CanonicalBytesHash);
    }

    public sealed class LoadedCharacterTargetProgramArtifact
    {
        readonly byte[] m_CanonicalBytes;

        internal LoadedCharacterTargetProgramArtifact(
            CharacterTargetProgramArtifactDescriptor descriptor,
            CharacterSimulationProgram program,
            byte[] canonicalBytes)
        {
            Descriptor = descriptor;
            Program = program ?? throw new ArgumentNullException(nameof(program));
            m_CanonicalBytes = canonicalBytes == null
                ? throw new ArgumentNullException(nameof(canonicalBytes))
                : (byte[])canonicalBytes.Clone();
        }

        public CharacterTargetProgramArtifactDescriptor Descriptor { get; }
        public CharacterSimulationProgram Program { get; }
        public byte[] CopyCanonicalBytes() => (byte[])m_CanonicalBytes.Clone();
    }

    public static class CharacterTargetProgramArtifactLoader
    {
        public static LoadedCharacterTargetProgramArtifact Load(
            byte[] canonicalBytes,
            CharacterTargetProgramArtifactExpectation expectation)
        {
            LoadedCharacterTargetProgramArtifact artifact = Inspect(expectation.DefinitionGuid, canonicalBytes);
            CharacterTargetProgramArtifactDescriptor descriptor = artifact.Descriptor;
            ProgramLoadExpectation program = expectation.Program;
            if (!descriptor.ProgramId.Equals(expectation.ProgramId) ||
                !string.Equals(descriptor.CompilerVersion, program.CompilerVersion, StringComparison.Ordinal) ||
                !descriptor.OperationSetVersion.Equals(program.OperationSetVersion) ||
                !descriptor.SourceRevision.Equals(program.SourceRevision) ||
                !descriptor.SemanticHash.Equals(program.SemanticHash) ||
                !descriptor.NumericProfileId.Equals(program.NumericProfile.Id) ||
                !descriptor.TargetAbiVersion.Equals(program.NumericProfile.AbiVersion) ||
                !descriptor.ProgramHash.Equals(expectation.ProgramHash) ||
                !descriptor.LayoutHash.Equals(expectation.LayoutHash) ||
                descriptor.RequiredWorldCapabilities != expectation.RequiredWorldCapabilities)
            {
                throw new InvalidDataException("Target Program artifact does not match its build expectation.");
            }
            return artifact;
        }

        public static LoadedCharacterTargetProgramArtifact LoadFile(
            string path,
            CharacterTargetProgramArtifactExpectation expectation)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Target Program artifact path is required.", nameof(path));
            return Load(File.ReadAllBytes(path), expectation);
        }

        public static LoadedCharacterTargetProgramArtifact Inspect(string definitionGuid, byte[] canonicalBytes)
        {
            RequireDefinitionGuid(definitionGuid);
            if (canonicalBytes == null || canonicalBytes.Length == 0)
                throw new InvalidDataException("Target Program artifact is empty.");
            CharacterSimulationProgramArtifactHeader header = CharacterSimulationProgramCodec.ReadArtifactHeader(canonicalBytes);
            var expectation = new ProgramLoadExpectation(
                header.CompilerVersion,
                header.OperationSetVersion,
                header.SourceRevision,
                header.SemanticHash,
                header.NumericProfile);
            CharacterSimulationProgram program = CharacterSimulationProgramCodec.ReadArtifact(canonicalBytes, expectation);
            var descriptor = new CharacterTargetProgramArtifactDescriptor(
                definitionGuid,
                program,
                ComputeBytesHash(canonicalBytes),
                canonicalBytes.Length);
            return new LoadedCharacterTargetProgramArtifact(descriptor, program, canonicalBytes);
        }

        public static StableHash ComputeBytesHash(byte[] bytes)
        {
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes));
            using SHA256 sha = SHA256.Create();
            byte[] hash = sha.ComputeHash(bytes);
            var chars = new char[hash.Length * 2];
            const string hex = "0123456789abcdef";
            for (int i = 0; i < hash.Length; i++)
            {
                chars[i * 2] = hex[hash[i] >> 4];
                chars[i * 2 + 1] = hex[hash[i] & 15];
            }
            return new StableHash(new string(chars));
        }

        public static string RequireDefinitionGuid(string definitionGuid)
        {
            if (string.IsNullOrEmpty(definitionGuid) || definitionGuid.Length != 32)
                throw new ArgumentException("Definition GUID must contain 32 lowercase hexadecimal characters.", nameof(definitionGuid));
            for (int i = 0; i < definitionGuid.Length; i++)
            {
                char value = definitionGuid[i];
                if (!((value >= '0' && value <= '9') || (value >= 'a' && value <= 'f')))
                    throw new ArgumentException("Definition GUID must contain 32 lowercase hexadecimal characters.", nameof(definitionGuid));
            }
            return definitionGuid;
        }
    }
}

