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
        [SerializeField] string m_ExecutionIdentity = string.Empty;
        [SerializeField] string m_ExecutionDataHash = string.Empty;
        [SerializeField] string m_StateSchemaHash = string.Empty;
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
        public string ExecutionIdentity => m_ExecutionIdentity;
        public string ExecutionDataHash => m_ExecutionDataHash;
        public string StateSchemaHash => m_StateSchemaHash;
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
            var root = new SimulationProgramRootDescriptor(
                (SimulationProgramRootKind)m_RootKind,
                m_RootIdentity,
                m_EntryIdentity,
                m_ContentIdentity);
            Float32GameplayAbilityExecutionData data = Float32GameplayAbilityExecutionDataCodec.ReadArtifact(
                m_CanonicalArtifact,
                new Float32GameplayAbilityExecutionDataLoadExpectation(
                    m_AbilityGuid,
                    m_AbilityId,
                    m_CompilerVersion,
                    m_OperationSetVersion,
                    m_SourceRevision,
                    m_SemanticHash,
                    m_NumericProfileId,
                    m_TargetAbiVersion,
                    m_ExecutionIdentity,
                    m_ExecutionDataHash,
                    m_StateSchemaHash,
                    m_CanonicalBytesHash,
                    root));
            data.ProviderContract.RequireBinding(providerBinding);
            return data;
        }

#if UNITY_EDITOR
        public void SetCompiledExecutionData(Float32GameplayAbilityExecutionCompilationResult compilation)
        {
            if (compilation == null)
                throw new ArgumentNullException(nameof(compilation));
            Float32GameplayAbilityExecutionData data = compilation.Data;
            if (!data.Root.IsAbility)
                throw new InvalidOperationException("Gameplay Ability Data asset requires an Ability root.");
            m_AbilityGuid = data.Root.RootIdentity;
            m_AbilityId = data.AbilityId.Value;
            m_CanonicalArtifact = Float32GameplayAbilityExecutionDataCodec.WriteArtifact(data);
            m_CompilerVersion = data.CompilerVersion;
            m_OperationSetVersion = data.OperationSetVersion.Value;
            m_SourceRevision = data.SourceRevision.Value;
            m_SemanticHash = data.SemanticHash.ToString();
            m_NumericProfileId = data.NumericProfile.Id.Value;
            m_TargetAbiVersion = data.NumericProfile.AbiVersion.Value;
            m_ExecutionIdentity = data.ExecutionIdentity;
            m_ExecutionDataHash = data.ContentHash.ToString();
            m_StateSchemaHash = data.StateSchemaHash.ToString();
            m_CanonicalBytesHash = Float32GameplayAbilityExecutionDataCodec
                .ComputeCanonicalBytesHash(m_CanonicalArtifact)
                .ToString();
            m_RootKind = (byte)data.Root.Kind;
            m_RootIdentity = data.Root.RootIdentity;
            m_EntryIdentity = data.Root.EntryIdentity;
            m_ContentIdentity = data.Root.ContentIdentity;
        }
#endif

    }
}
