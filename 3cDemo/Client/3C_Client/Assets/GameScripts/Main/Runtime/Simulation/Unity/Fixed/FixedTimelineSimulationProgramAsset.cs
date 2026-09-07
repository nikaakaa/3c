using System;
using ThirdPersonSimulation;
using ThirdPersonSimulation.Fixed;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation.Fixed
{
    public sealed class FixedTimelineSimulationProgramAsset : ScriptableObject
    {
        [SerializeField] byte[] m_CanonicalArtifact = Array.Empty<byte>();
        [SerializeField] string m_CompilerVersion = string.Empty;
        [SerializeField] string m_OperationSetVersion = string.Empty;
        [SerializeField] string m_SourceRevision = string.Empty;
        [SerializeField] string m_SemanticHash = string.Empty;
        [SerializeField] string m_ProgramId = string.Empty;
        [SerializeField] string m_ProgramHash = string.Empty;
        [SerializeField] string m_LayoutHash = string.Empty;
        [SerializeField] string m_CanonicalBytesHash = string.Empty;
        [SerializeField] byte m_RootKind;
        [SerializeField] string m_RootIdentity = string.Empty;
        [SerializeField] string m_EntryIdentity = string.Empty;
        [SerializeField] string m_ContentIdentity = string.Empty;

        public string CompilerVersion => m_CompilerVersion;
        public string OperationSetVersion => m_OperationSetVersion;
        public string SourceRevision => m_SourceRevision;
        public string SemanticHash => m_SemanticHash;
        public string ProgramId => m_ProgramId;
        public string ProgramHash => m_ProgramHash;
        public string LayoutHash => m_LayoutHash;
        public string CanonicalBytesHash => m_CanonicalBytesHash;
        public SimulationProgramRootKind RootKind => (SimulationProgramRootKind)m_RootKind;
        public string RootIdentity => m_RootIdentity;
        public string EntryIdentity => m_EntryIdentity;
        public string ContentIdentity => m_ContentIdentity;
        public int CanonicalByteLength => m_CanonicalArtifact?.Length ?? 0;

        public byte[] CopyCanonicalArtifact() =>
            m_CanonicalArtifact == null ? Array.Empty<byte>() : (byte[])m_CanonicalArtifact.Clone();

        public ThirdPersonSimulation.Fixed.CharacterSimulationProgram Load()
        {
            if (m_CanonicalArtifact == null || m_CanonicalArtifact.Length == 0)
                throw new InvalidOperationException($"Fixed Timeline Program asset '{name}' has no compiled artifact.");
            StableHash bytesHash = ThirdPersonSimulation.Fixed.CharacterTargetProgramArtifactLoader.ComputeBytesHash(m_CanonicalArtifact);
            if (!bytesHash.IsValid || !string.Equals(bytesHash.Value, m_CanonicalBytesHash, StringComparison.Ordinal))
                throw new InvalidOperationException($"Fixed Timeline Program asset '{name}' canonical bytes hash is invalid.");
            var root = new SimulationProgramRootDescriptor(
                (SimulationProgramRootKind)m_RootKind,
                m_RootIdentity,
                m_EntryIdentity,
                m_ContentIdentity);
            if (!root.IsTimeline)
                throw new InvalidOperationException($"Fixed Timeline Program asset '{name}' does not contain a Timeline root.");
            var expectation = new ThirdPersonSimulation.Fixed.ProgramLoadExpectation(
                m_CompilerVersion,
                new OperationSetVersion(m_OperationSetVersion),
                new ProgramRevision(m_SourceRevision),
                new SemanticHash(new StableHash(m_SemanticHash)),
                FixedSimulationNumericProfile.Value,
                root);
            ThirdPersonSimulation.Fixed.CharacterSimulationProgram program =
                ThirdPersonSimulation.Fixed.CharacterSimulationProgramCodec.ReadArtifact(
                    m_CanonicalArtifact,
                    expectation);
            if (!string.Equals(program.Manifest.ProgramId.Value, m_ProgramId, StringComparison.Ordinal) ||
                !string.Equals(program.Manifest.OperationSetVersion.Value, m_OperationSetVersion, StringComparison.Ordinal) ||
                !string.Equals(program.Manifest.SemanticHash.ToString(), m_SemanticHash, StringComparison.Ordinal) ||
                !string.Equals(program.ProgramHash.ToString(), m_ProgramHash, StringComparison.Ordinal) ||
                !string.Equals(program.LayoutHash.ToString(), m_LayoutHash, StringComparison.Ordinal))
                throw new InvalidOperationException($"Fixed Timeline Program asset '{name}' metadata does not match its artifact.");
            return program;
        }

#if UNITY_EDITOR
        public void SetCompiledArtifact(ThirdPersonSimulation.Fixed.LoadedCharacterTargetProgramArtifact artifact)
        {
            if (artifact == null)
                throw new ArgumentNullException(nameof(artifact));
            ThirdPersonSimulation.Fixed.CharacterTargetProgramArtifactDescriptor descriptor = artifact.Descriptor;
            ThirdPersonSimulation.Fixed.CharacterSimulationProgram program = artifact.Program;
            if (!program.Manifest.Root.IsTimeline)
                throw new InvalidOperationException("Fixed Timeline Program asset requires a Timeline root.");
            if (!descriptor.ProgramId.Equals(program.Manifest.ProgramId) ||
                !descriptor.ProgramHash.Equals(program.ProgramHash) ||
                !descriptor.LayoutHash.Equals(program.LayoutHash) ||
                !descriptor.NumericProfileId.Equals(FixedSimulationNumericProfile.Value.Id) ||
                !descriptor.TargetAbiVersion.Equals(FixedSimulationNumericProfile.Value.AbiVersion))
                throw new InvalidOperationException("Fixed Target artifact descriptor does not match its Program.");
            m_CanonicalArtifact = artifact.CopyCanonicalBytes();
            m_CompilerVersion = program.Manifest.CompilerVersion;
            m_OperationSetVersion = program.Manifest.OperationSetVersion.Value;
            m_SourceRevision = program.Manifest.SourceRevision.Value;
            m_SemanticHash = program.Manifest.SemanticHash.ToString();
            m_ProgramId = program.Manifest.ProgramId.Value;
            m_ProgramHash = program.ProgramHash.ToString();
            m_LayoutHash = program.LayoutHash.ToString();
            m_CanonicalBytesHash = descriptor.CanonicalBytesHash.Value;
            m_RootKind = (byte)program.Manifest.Root.Kind;
            m_RootIdentity = program.Manifest.Root.RootIdentity;
            m_EntryIdentity = program.Manifest.Root.EntryIdentity;
            m_ContentIdentity = program.Manifest.Root.ContentIdentity;
        }
#endif
    }
}
