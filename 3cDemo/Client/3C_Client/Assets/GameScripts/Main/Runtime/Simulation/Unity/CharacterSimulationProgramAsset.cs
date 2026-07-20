using System;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation
{
    public sealed class CharacterSimulationProgramAsset : ScriptableObject
    {
        [SerializeField] byte[] m_CanonicalArtifact = Array.Empty<byte>();
        [SerializeField] string m_CompilerVersion = string.Empty;
        [SerializeField] string m_OperationSetVersion = string.Empty;
        [SerializeField] string m_SourceRevision = string.Empty;
        [SerializeField] string m_SemanticHash = string.Empty;
        [SerializeField] string m_NumericProfileId = string.Empty;
        [SerializeField] int m_TargetAbiVersion;
        [SerializeField] string m_ProgramId = string.Empty;
        [SerializeField] string m_ProgramHash = string.Empty;
        [SerializeField] string m_LayoutHash = string.Empty;
        [SerializeField] string m_CanonicalBytesHash = string.Empty;

        public string CompilerVersion => m_CompilerVersion;
        public string OperationSetVersion => m_OperationSetVersion;
        public string SourceRevision => m_SourceRevision;
        public string SemanticHash => m_SemanticHash;
        public string NumericProfileId => m_NumericProfileId;
        public int TargetAbiVersion => m_TargetAbiVersion;
        public string ProgramId => m_ProgramId;
        public string ProgramHash => m_ProgramHash;
        public string LayoutHash => m_LayoutHash;
        public string CanonicalBytesHash => m_CanonicalBytesHash;
        public int CanonicalByteLength => m_CanonicalArtifact?.Length ?? 0;

        public byte[] CopyCanonicalArtifact()
        {
            return m_CanonicalArtifact == null ? Array.Empty<byte>() : (byte[])m_CanonicalArtifact.Clone();
        }

        public CharacterSimulationProgram Load()
        {
            return Load(Float32SimulationNumericProfile.Value);
        }

        public CharacterSimulationProgram Load(SimulationNumericProfile numericProfile)
        {
            if (m_CanonicalArtifact == null || m_CanonicalArtifact.Length == 0)
                throw new InvalidOperationException($"Character Simulation Program asset '{name}' has no compiled artifact.");
            StableHash canonicalBytesHash = CharacterTargetProgramArtifactLoader.ComputeBytesHash(m_CanonicalArtifact);
            if (!canonicalBytesHash.IsValid || !string.Equals(canonicalBytesHash.ToString(), m_CanonicalBytesHash, StringComparison.Ordinal))
                throw new InvalidOperationException($"Character Simulation Program asset '{name}' canonical bytes hash is invalid.");
            if (!numericProfile.IsValid)
                throw new ArgumentException("Character Simulation Program Numeric Target is invalid.", nameof(numericProfile));
            if (!string.Equals(m_NumericProfileId, numericProfile.Id.Value, StringComparison.Ordinal) || m_TargetAbiVersion != numericProfile.AbiVersion.Value)
                throw new InvalidOperationException($"Character Simulation Program asset '{name}' Numeric Target is not installed.");
            CharacterSimulationProgram program = CharacterSimulationProgramCodec.ReadArtifact(
                m_CanonicalArtifact,
                new ProgramLoadExpectation(
                    m_CompilerVersion,
                    new OperationSetVersion(m_OperationSetVersion),
                    new ProgramRevision(m_SourceRevision),
                    new SemanticHash(new StableHash(m_SemanticHash)),
                    numericProfile));
            if (!string.Equals(program.Manifest.ProgramId.Value, m_ProgramId, StringComparison.Ordinal) ||
                !string.Equals(program.Manifest.OperationSetVersion.Value, m_OperationSetVersion, StringComparison.Ordinal) ||
                !string.Equals(program.Manifest.SemanticHash.ToString(), m_SemanticHash, StringComparison.Ordinal) ||
                !string.Equals(program.ProgramHash.ToString(), m_ProgramHash, StringComparison.Ordinal) ||
                !string.Equals(program.LayoutHash.ToString(), m_LayoutHash, StringComparison.Ordinal))
                throw new InvalidOperationException($"Character Simulation Program asset '{name}' metadata does not match its canonical artifact.");
            return program;
        }

#if UNITY_EDITOR
        public void SetCompiledArtifact(LoadedCharacterTargetProgramArtifact artifact)
        {
            if (artifact == null)
                throw new ArgumentNullException(nameof(artifact));
            CharacterSimulationProgram program = artifact.Program;
            CharacterTargetProgramArtifactDescriptor descriptor = artifact.Descriptor;
            if (!descriptor.ProgramId.Equals(program.Manifest.ProgramId) ||
                !descriptor.ProgramHash.Equals(program.ProgramHash) ||
                !descriptor.LayoutHash.Equals(program.LayoutHash))
                throw new InvalidOperationException("Target Program artifact descriptor does not match its Program.");
            m_CanonicalArtifact = artifact.CopyCanonicalBytes();
            m_CompilerVersion = program.Manifest.CompilerVersion;
            m_OperationSetVersion = program.Manifest.OperationSetVersion.Value;
            m_SourceRevision = program.Manifest.SourceRevision.Value;
            m_SemanticHash = program.Manifest.SemanticHash.ToString();
            m_NumericProfileId = program.Manifest.NumericProfile.Id.Value;
            m_TargetAbiVersion = program.Manifest.NumericProfile.AbiVersion.Value;
            m_ProgramId = program.Manifest.ProgramId.Value;
            m_ProgramHash = program.ProgramHash.ToString();
            m_LayoutHash = program.LayoutHash.ToString();
            m_CanonicalBytesHash = descriptor.CanonicalBytesHash.ToString();
        }
#endif
    }
}
