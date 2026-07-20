using System;
using ThirdPersonSimulation;
using ThirdPersonSimulation.Fixed;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation.DeterministicRollback
{
    public sealed class FixedCharacterSimulationProgramAsset : ScriptableObject
    {
        [SerializeField] byte[] m_CanonicalArtifact = Array.Empty<byte>();
        [SerializeField] string m_DefinitionGuid = string.Empty;
        [SerializeField] string m_CompilerVersion = string.Empty;
        [SerializeField] string m_OperationSetVersion = string.Empty;
        [SerializeField] string m_SourceRevision = string.Empty;
        [SerializeField] string m_SemanticHash = string.Empty;
        [SerializeField] string m_ProgramId = string.Empty;
        [SerializeField] string m_ProgramHash = string.Empty;
        [SerializeField] string m_LayoutHash = string.Empty;
        [SerializeField] string m_CanonicalBytesHash = string.Empty;

        public string DefinitionGuid => m_DefinitionGuid;
        public string CompilerVersion => m_CompilerVersion;
        public string OperationSetVersion => m_OperationSetVersion;
        public string SourceRevision => m_SourceRevision;
        public string SemanticHash => m_SemanticHash;
        public string ProgramId => m_ProgramId;
        public string ProgramHash => m_ProgramHash;
        public string LayoutHash => m_LayoutHash;
        public string CanonicalBytesHash => m_CanonicalBytesHash;
        public int CanonicalByteLength => m_CanonicalArtifact?.Length ?? 0;

        public byte[] CopyCanonicalArtifact() =>
            m_CanonicalArtifact == null ? Array.Empty<byte>() : (byte[])m_CanonicalArtifact.Clone();

        public ThirdPersonSimulation.Fixed.CharacterSimulationProgram Load()
        {
            if (m_CanonicalArtifact == null || m_CanonicalArtifact.Length == 0)
                throw new InvalidOperationException($"Fixed Character Program asset '{name}' has no compiled artifact.");
            StableHash bytesHash = ThirdPersonSimulation.Fixed.CharacterTargetProgramArtifactLoader.ComputeBytesHash(
                m_CanonicalArtifact);
            if (!bytesHash.IsValid || !string.Equals(bytesHash.Value, m_CanonicalBytesHash, StringComparison.Ordinal))
                throw new InvalidOperationException($"Fixed Character Program asset '{name}' canonical bytes hash is invalid.");
            var expectation = new ThirdPersonSimulation.Fixed.ProgramLoadExpectation(
                m_CompilerVersion,
                new OperationSetVersion(m_OperationSetVersion),
                new ProgramRevision(m_SourceRevision),
                new SemanticHash(new StableHash(m_SemanticHash)),
                FixedSimulationNumericProfile.Value);
            ThirdPersonSimulation.Fixed.CharacterSimulationProgram program =
                ThirdPersonSimulation.Fixed.CharacterSimulationProgramCodec.ReadArtifact(
                    m_CanonicalArtifact,
                    expectation);
            if (!string.Equals(program.Manifest.ProgramId.Value, m_ProgramId, StringComparison.Ordinal) ||
                !string.Equals(program.ProgramHash.ToString(), m_ProgramHash, StringComparison.Ordinal) ||
                !string.Equals(program.LayoutHash.ToString(), m_LayoutHash, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Fixed Character Program asset '{name}' metadata does not match its artifact.");
            }
            return program;
        }

#if UNITY_EDITOR
        public void SetCompiledArtifact(ThirdPersonSimulation.Fixed.LoadedCharacterTargetProgramArtifact artifact)
        {
            if (artifact == null)
                throw new ArgumentNullException(nameof(artifact));
            ThirdPersonSimulation.Fixed.CharacterTargetProgramArtifactDescriptor descriptor = artifact.Descriptor;
            ThirdPersonSimulation.Fixed.CharacterSimulationProgram program = artifact.Program;
            if (!descriptor.ProgramId.Equals(program.Manifest.ProgramId) ||
                !descriptor.ProgramHash.Equals(program.ProgramHash) ||
                !descriptor.LayoutHash.Equals(program.LayoutHash) ||
                !descriptor.NumericProfileId.Equals(FixedSimulationNumericProfile.Value.Id) ||
                !descriptor.TargetAbiVersion.Equals(FixedSimulationNumericProfile.Value.AbiVersion))
            {
                throw new InvalidOperationException("Fixed Target artifact descriptor does not match its Program.");
            }
            m_CanonicalArtifact = artifact.CopyCanonicalBytes();
            m_DefinitionGuid = descriptor.DefinitionGuid;
            m_CompilerVersion = program.Manifest.CompilerVersion;
            m_OperationSetVersion = program.Manifest.OperationSetVersion.Value;
            m_SourceRevision = program.Manifest.SourceRevision.Value;
            m_SemanticHash = program.Manifest.SemanticHash.ToString();
            m_ProgramId = program.Manifest.ProgramId.Value;
            m_ProgramHash = program.ProgramHash.ToString();
            m_LayoutHash = program.LayoutHash.ToString();
            m_CanonicalBytesHash = descriptor.CanonicalBytesHash.Value;
        }
#endif
    }
}
