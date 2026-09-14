using System;
using ThirdPersonSimulation;
using ThirdPersonSimulation.Fixed;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation.Fixed
{
    public sealed class FixedGameplayAbilityDataAsset : ScriptableObject
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

        public FixedGameplayAbilityExecutionData Load(GameplayAbilityProviderBinding providerBinding)
        {
            var root = new SimulationProgramRootDescriptor(
                (SimulationProgramRootKind)m_RootKind,
                m_RootIdentity,
                m_EntryIdentity,
                m_ContentIdentity);
            return ThirdPersonSimulation.Fixed.FixedGameplayAbilityExecutionDataLoader.Load(
                m_CanonicalArtifact,
                new ThirdPersonSimulation.Fixed.FixedGameplayAbilityExecutionDataLoadExpectation(
                    m_AbilityGuid,
                    m_AbilityId,
                    m_CompilerVersion,
                    m_OperationSetVersion,
                    m_SourceRevision,
                    m_SemanticHash,
                    m_NumericProfileId,
                    m_TargetAbiVersion,
                    m_ProgramId,
                    m_AbilityDataHash,
                    m_AbilityLayoutHash,
                    m_CanonicalBytesHash,
                    root),
                providerBinding);
        }

#if UNITY_EDITOR
        public void SetCompiledArtifact(ThirdPersonSimulation.Fixed.LoadedCharacterTargetProgramArtifact artifact)
        {
            if (artifact == null)
                throw new ArgumentNullException(nameof(artifact));
            ThirdPersonSimulation.Fixed.CharacterSimulationProgram program = artifact.Program;
            if (!program.Manifest.Root.IsAbility)
                throw new InvalidOperationException("Fixed Gameplay Ability Data asset requires an Ability root.");
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

        static string RequireAbilityId(string entryIdentity)
        {
            const string prefix = "ability:";
            if (string.IsNullOrEmpty(entryIdentity) || !entryIdentity.StartsWith(prefix, StringComparison.Ordinal))
                throw new InvalidOperationException("Fixed Gameplay Ability Data artifact entry identity is invalid.");
            return new CharacterSkillId(entryIdentity.Substring(prefix.Length)).Value;
        }
    }
}
