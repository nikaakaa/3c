using System;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation
{
    public sealed class GameplayAbilityDataAsset : ScriptableObject
    {
        [SerializeField] byte[] m_CanonicalArtifact = Array.Empty<byte>();
        [SerializeField] string m_AbilityGuid = string.Empty;
        [SerializeField] string m_AbilityId = string.Empty;
        [SerializeField] string m_CompilerVersion = string.Empty;
        [SerializeField] string m_OperationSetVersion = string.Empty;
        [SerializeField] string m_SourceRevision = string.Empty;
        [SerializeField] string m_SemanticHash = string.Empty;
        [SerializeField] string m_NumericProfileId = string.Empty;
        [SerializeField] int m_TargetAbiVersion;
        [SerializeField] string m_ProgramId = string.Empty;
        [SerializeField] string m_AbilityDataHash = string.Empty;
        [SerializeField] string m_AbilityLayoutHash = string.Empty;
        [SerializeField] string m_CanonicalBytesHash = string.Empty;
        [SerializeField] byte m_RootKind;
        [SerializeField] string m_RootIdentity = string.Empty;
        [SerializeField] string m_EntryIdentity = string.Empty;
        [SerializeField] string m_ContentIdentity = string.Empty;

        public string AbilityGuid => m_AbilityGuid;
        public string AbilityId => m_AbilityId;
        public string CompilerVersion => m_CompilerVersion;
        public string OperationSetVersion => m_OperationSetVersion;
        public string SourceRevision => m_SourceRevision;
        public string SemanticHash => m_SemanticHash;
        public string NumericProfileId => m_NumericProfileId;
        public int TargetAbiVersion => m_TargetAbiVersion;
        public string ProgramId => m_ProgramId;
        public string AbilityDataHash => m_AbilityDataHash;
        public string AbilityLayoutHash => m_AbilityLayoutHash;
        public string CanonicalBytesHash => m_CanonicalBytesHash;
        public SimulationProgramRootKind RootKind => (SimulationProgramRootKind)m_RootKind;
        public string RootIdentity => m_RootIdentity;
        public string EntryIdentity => m_EntryIdentity;
        public string ContentIdentity => m_ContentIdentity;
        public int CanonicalByteLength => m_CanonicalArtifact?.Length ?? 0;

        public byte[] CopyCanonicalArtifact() =>
            m_CanonicalArtifact == null ? Array.Empty<byte>() : (byte[])m_CanonicalArtifact.Clone();

        public Float32GameplayAbilityExecutionData Load(GameplayAbilityProviderBinding providerBinding)
        {
            if (m_CanonicalArtifact == null || m_CanonicalArtifact.Length == 0)
                throw new InvalidOperationException($"Gameplay Ability Data asset '{name}' has no compiled artifact.");
            Float32Loader.RequireDefinitionGuid(m_AbilityGuid);
            if (string.IsNullOrEmpty(m_AbilityId))
                throw new InvalidOperationException($"Gameplay Ability Data asset '{name}' has no Ability identity.");
            StableHash bytesHash = Float32Loader.ComputeBytesHash(m_CanonicalArtifact);
            if (!bytesHash.IsValid || !string.Equals(bytesHash.ToString(), m_CanonicalBytesHash, StringComparison.Ordinal))
                throw new InvalidOperationException($"Gameplay Ability Data asset '{name}' canonical bytes hash is invalid.");
            if (!string.Equals(m_NumericProfileId, Float32SimulationNumericProfile.Value.Id.Value, StringComparison.Ordinal) ||
                m_TargetAbiVersion != Float32SimulationNumericProfile.Value.AbiVersion.Value)
                throw new InvalidOperationException($"Gameplay Ability Data asset '{name}' Numeric Target is not Float32.");
            var root = new SimulationProgramRootDescriptor(
                (SimulationProgramRootKind)m_RootKind,
                m_RootIdentity,
                m_EntryIdentity,
                m_ContentIdentity);
            RequireMetadataRoot(root);
            CharacterSimulationProgram program = CharacterSimulationProgramCodec.ReadArtifact(
                m_CanonicalArtifact,
                new ProgramLoadExpectation(
                    m_CompilerVersion,
                    new OperationSetVersion(m_OperationSetVersion),
                    new ProgramRevision(m_SourceRevision),
                    new SemanticHash(new StableHash(m_SemanticHash)),
                    Float32SimulationNumericProfile.Value,
                    root));
            RequireProgramMetadata(program, root);
            program.AbilityPrograms.Require(new CharacterSkillId(m_AbilityId));
            GameplayAbilityProviderContract providerContract = GameplayAbilityProviderContract
                .Create(program.CatalogEntries, index => program.Constants[index].Int32);
            providerContract.RequireBinding(providerBinding);
            return Float32GameplayAbilityExecutionData.FromProgram(
                program,
                new CharacterSkillId(m_AbilityId),
                providerContract);
        }

#if UNITY_EDITOR
        public void SetCompiledArtifact(LoadedCharacterTargetProgramArtifact artifact)
        {
            if (artifact == null)
                throw new ArgumentNullException(nameof(artifact));
            CharacterSimulationProgram program = artifact.Program;
            if (!program.Manifest.Root.IsAbility)
                throw new InvalidOperationException("Gameplay Ability Data asset requires an Ability root.");
            m_AbilityGuid = artifact.Descriptor.DefinitionGuid;
            m_AbilityId = RequireAbilityId(program.Manifest.Root.EntryIdentity);
            m_CanonicalArtifact = artifact.CopyCanonicalBytes();
            m_CompilerVersion = program.Manifest.CompilerVersion;
            m_OperationSetVersion = program.Manifest.OperationSetVersion.Value;
            m_SourceRevision = program.Manifest.SourceRevision.Value;
            m_SemanticHash = program.Manifest.SemanticHash.ToString();
            m_NumericProfileId = program.Manifest.NumericProfile.Id.Value;
            m_TargetAbiVersion = program.Manifest.NumericProfile.AbiVersion.Value;
            m_ProgramId = program.Manifest.ProgramId.Value;
            m_AbilityDataHash = program.ProgramHash.ToString();
            m_AbilityLayoutHash = program.LayoutHash.ToString();
            m_CanonicalBytesHash = artifact.Descriptor.CanonicalBytesHash.ToString();
            m_RootKind = (byte)program.Manifest.Root.Kind;
            m_RootIdentity = program.Manifest.Root.RootIdentity;
            m_EntryIdentity = program.Manifest.Root.EntryIdentity;
            m_ContentIdentity = program.Manifest.Root.ContentIdentity;
        }
#endif

        void RequireMetadataRoot(SimulationProgramRootDescriptor root)
        {
            if (!root.IsAbility || !string.Equals(root.RootIdentity, m_AbilityGuid, StringComparison.Ordinal) ||
                !string.Equals(root.EntryIdentity, $"ability:{m_AbilityId}", StringComparison.Ordinal))
                throw new InvalidOperationException($"Gameplay Ability Data asset '{name}' root metadata is invalid.");
        }

        void RequireProgramMetadata(CharacterSimulationProgram program, SimulationProgramRootDescriptor root)
        {
            if (!program.Manifest.Root.Equals(root) ||
                !string.Equals(program.Manifest.ProgramId.Value, m_ProgramId, StringComparison.Ordinal) ||
                !string.Equals(program.Manifest.OperationSetVersion.Value, m_OperationSetVersion, StringComparison.Ordinal) ||
                !string.Equals(program.Manifest.SemanticHash.ToString(), m_SemanticHash, StringComparison.Ordinal) ||
                !string.Equals(program.ProgramHash.ToString(), m_AbilityDataHash, StringComparison.Ordinal) ||
                !string.Equals(program.LayoutHash.ToString(), m_AbilityLayoutHash, StringComparison.Ordinal))
                throw new InvalidOperationException($"Gameplay Ability Data asset '{name}' metadata does not match its canonical artifact.");
        }

        static string RequireAbilityId(string entryIdentity)
        {
            const string prefix = "ability:";
            if (string.IsNullOrEmpty(entryIdentity) || !entryIdentity.StartsWith(prefix, StringComparison.Ordinal))
                throw new InvalidOperationException("Gameplay Ability Data artifact entry identity is invalid.");
            return new CharacterSkillId(entryIdentity.Substring(prefix.Length)).Value;
        }

        static class Float32Loader
        {
            public static void RequireDefinitionGuid(string value) =>
                CharacterTargetProgramArtifactLoader.RequireDefinitionGuid(value);

            public static StableHash ComputeBytesHash(byte[] value) =>
                CharacterTargetProgramArtifactLoader.ComputeBytesHash(value);
        }
    }
}
