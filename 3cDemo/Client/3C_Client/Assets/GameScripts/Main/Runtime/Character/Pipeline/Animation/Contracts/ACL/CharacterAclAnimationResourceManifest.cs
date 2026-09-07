using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    [Serializable]
    public sealed class CharacterAclAnimationResourceManifest
    {
        public const string CurrentSchemaVersion = "character-acl-animation-resource/v3";
        public const int PayloadFormatVersion = 10;
        public const string OfficialAclVersion = "2.1.0";
        public const string OfficialAclCommit = "414689d5cff4286a7898487a46dc5e48005d38da";
        public const string OfficialNativePlatform = "Windows-x64";
        public const int OfficialNativeAbiVersion = 2;

        [SerializeField] string m_SchemaVersion = CurrentSchemaVersion;
        [SerializeField] string m_ResourceIdentity = string.Empty;
        [SerializeField] string m_ResourceAddress = string.Empty;
        [SerializeField] string m_SourceIdentity = string.Empty;
        [SerializeField] string m_FormalClipIdentity = string.Empty;
        [SerializeField] string m_SourceDependencyHash = string.Empty;
        [SerializeField] string m_RigId = string.Empty;
        [SerializeField] string m_RigRevision = string.Empty;
        [SerializeField] string m_ReferencePoseIdentity = string.Empty;
        [SerializeField] string m_BindingHash = string.Empty;
        [SerializeField] string m_ContentHash = string.Empty;
        [SerializeField] string m_BuildRevision = string.Empty;
        [SerializeField] string m_GroupContentHash = string.Empty;
        [SerializeField] string m_NativeArtifactIdentity = string.Empty;
        [SerializeField] string m_NativeBinarySha256 = string.Empty;
        [SerializeField] string m_NativePlatform = string.Empty;
        [SerializeField] int m_NativeAbiVersion;
        [SerializeField] int m_NativePayloadFormatVersion;
        [SerializeField] int m_GroupClipIndex = -1;
        [SerializeField] CharacterAnimationSamplingBackendKind m_Backend = CharacterAnimationSamplingBackendKind.Acl;
        [SerializeField] int m_PoseBoneCount;
        [SerializeField] int m_PhysicalBoneCount;
        [SerializeField] int m_TransformTrackCount;
        [SerializeField] int m_ScalarTrackCount;
        [SerializeField] float m_SampleRate;
        [SerializeField] float m_StartTimeSeconds;
        [SerializeField] float m_StopTimeSeconds;
        [SerializeField] bool m_Looping;
        [SerializeField] bool m_DatabaseUsed;
        [SerializeField] CharacterAclCompressionSettings m_Compression;
        [SerializeField] CharacterAclDataBlockDescriptor m_Transform;
        [SerializeField] CharacterAclDataBlockDescriptor m_Scalar;
        [SerializeField] CharacterAclDataBlockDescriptor m_DatabaseHeader;
        [SerializeField] CharacterAclDataBlockDescriptor m_BulkMedium;
        [SerializeField] CharacterAclDataBlockDescriptor m_BulkLow;
        [SerializeField] CharacterAclTransformTrackBinding[] m_TransformBindings = Array.Empty<CharacterAclTransformTrackBinding>();
        [SerializeField] CharacterAclScalarTrackBinding[] m_ScalarBindings = Array.Empty<CharacterAclScalarTrackBinding>();

        public string SchemaVersion => m_SchemaVersion ?? string.Empty;
        public string ResourceIdentity => m_ResourceIdentity ?? string.Empty;
        public string ResourceAddress => m_ResourceAddress ?? string.Empty;
        public string SourceIdentity => m_SourceIdentity ?? string.Empty;
        public string FormalClipIdentity => m_FormalClipIdentity ?? string.Empty;
        public string SourceDependencyHash => m_SourceDependencyHash ?? string.Empty;
        public string RigId => m_RigId ?? string.Empty;
        public string RigRevision => m_RigRevision ?? string.Empty;
        public string ReferencePoseIdentity => m_ReferencePoseIdentity ?? string.Empty;
        public string BindingHash => m_BindingHash ?? string.Empty;
        public string ContentHash => m_ContentHash ?? string.Empty;
        public string BuildRevision => m_BuildRevision ?? string.Empty;
        public string GroupContentHash => m_GroupContentHash ?? string.Empty;
        public string NativeArtifactIdentity => m_NativeArtifactIdentity ?? string.Empty;
        public string NativeBinarySha256 => m_NativeBinarySha256 ?? string.Empty;
        public string NativePlatform => m_NativePlatform ?? string.Empty;
        public int NativeAbiVersion => m_NativeAbiVersion;
        public int NativePayloadFormatVersion => m_NativePayloadFormatVersion;
        public int GroupClipIndex => m_GroupClipIndex;
        public CharacterAnimationSamplingBackendKind Backend => m_Backend;
        public int PoseBoneCount => m_PoseBoneCount;
        public int PhysicalBoneCount => m_PhysicalBoneCount;
        public int TransformTrackCount => m_TransformTrackCount;
        public int ScalarTrackCount => m_ScalarTrackCount;
        public float SampleRate => m_SampleRate;
        public float StartTimeSeconds => m_StartTimeSeconds;
        public float StopTimeSeconds => m_StopTimeSeconds;
        public bool Looping => m_Looping;
        public bool DatabaseUsed => m_DatabaseUsed;
        public CharacterAclCompressionSettings Compression => m_Compression;
        public CharacterAclDataBlockDescriptor Transform => m_Transform;
        public CharacterAclDataBlockDescriptor Scalar => m_Scalar;
        public CharacterAclDataBlockDescriptor DatabaseHeader => m_DatabaseHeader;
        public CharacterAclDataBlockDescriptor BulkMedium => m_BulkMedium;
        public CharacterAclDataBlockDescriptor BulkLow => m_BulkLow;
        public IReadOnlyList<CharacterAclTransformTrackBinding> TransformBindings => m_TransformBindings ?? Array.Empty<CharacterAclTransformTrackBinding>();
        public IReadOnlyList<CharacterAclScalarTrackBinding> ScalarBindings => m_ScalarBindings ?? Array.Empty<CharacterAclScalarTrackBinding>();

        public CharacterAclAnimationResourceManifest() { }

        public CharacterAclAnimationResourceManifest(
            string resourceIdentity,
            string resourceAddress,
            string sourceIdentity,
            string formalClipIdentity,
            string sourceDependencyHash,
            string rigId,
            string rigRevision,
            string referencePoseIdentity,
            string bindingHash,
            string contentHash,
            string buildRevision,
            string groupContentHash,
            string nativeArtifactIdentity,
            string nativeBinarySha256,
            string nativePlatform,
            int nativeAbiVersion,
            int nativePayloadFormatVersion,
            int groupClipIndex,
            int poseBoneCount,
            int physicalBoneCount,
            int transformTrackCount,
            int scalarTrackCount,
            float sampleRate,
            float startTimeSeconds,
            float stopTimeSeconds,
            bool looping,
            bool databaseUsed,
            CharacterAclCompressionSettings compression,
            CharacterAclDataBlockDescriptor transform,
            CharacterAclDataBlockDescriptor scalar,
            CharacterAclDataBlockDescriptor databaseHeader,
            CharacterAclDataBlockDescriptor bulkMedium,
            CharacterAclDataBlockDescriptor bulkLow,
            CharacterAclTransformTrackBinding[] transformBindings,
            CharacterAclScalarTrackBinding[] scalarBindings)
        {
            m_SchemaVersion = CurrentSchemaVersion;
            m_ResourceIdentity = PoseIdentity.Require(resourceIdentity, nameof(resourceIdentity));
            m_ResourceAddress = PoseIdentity.Require(resourceAddress, nameof(resourceAddress));
            m_SourceIdentity = PoseIdentity.Require(sourceIdentity, nameof(sourceIdentity));
            m_FormalClipIdentity = PoseIdentity.Require(formalClipIdentity, nameof(formalClipIdentity));
            m_SourceDependencyHash = PoseIdentity.Require(sourceDependencyHash, nameof(sourceDependencyHash));
            m_RigId = PoseIdentity.Require(rigId, nameof(rigId));
            m_RigRevision = PoseIdentity.Require(rigRevision, nameof(rigRevision));
            m_ReferencePoseIdentity = PoseIdentity.Require(referencePoseIdentity, nameof(referencePoseIdentity));
            m_BindingHash = PoseIdentity.Require(bindingHash, nameof(bindingHash));
            m_ContentHash = PoseIdentity.Require(contentHash, nameof(contentHash));
            m_BuildRevision = PoseIdentity.Require(buildRevision, nameof(buildRevision));
            m_GroupContentHash = PoseIdentity.Require(groupContentHash, nameof(groupContentHash));
            m_NativeArtifactIdentity = PoseIdentity.Require(nativeArtifactIdentity, nameof(nativeArtifactIdentity));
            m_NativeBinarySha256 = PoseIdentity.Require(nativeBinarySha256, nameof(nativeBinarySha256));
            m_NativePlatform = PoseIdentity.Require(nativePlatform, nameof(nativePlatform));
            m_NativeAbiVersion = nativeAbiVersion;
            m_NativePayloadFormatVersion = nativePayloadFormatVersion;
            m_GroupClipIndex = groupClipIndex;
            m_Backend = CharacterAnimationSamplingBackendKind.Acl;
            m_PoseBoneCount = poseBoneCount;
            m_PhysicalBoneCount = physicalBoneCount;
            m_TransformTrackCount = transformTrackCount;
            m_ScalarTrackCount = scalarTrackCount;
            m_SampleRate = sampleRate;
            m_StartTimeSeconds = startTimeSeconds;
            m_StopTimeSeconds = stopTimeSeconds;
            m_Looping = looping;
            m_DatabaseUsed = databaseUsed;
            m_Compression = compression ?? throw new ArgumentNullException(nameof(compression));
            m_Transform = transform ?? throw new ArgumentNullException(nameof(transform));
            m_Scalar = scalar ?? throw new ArgumentNullException(nameof(scalar));
            m_DatabaseHeader = databaseHeader ?? throw new ArgumentNullException(nameof(databaseHeader));
            m_BulkMedium = bulkMedium ?? throw new ArgumentNullException(nameof(bulkMedium));
            m_BulkLow = bulkLow ?? throw new ArgumentNullException(nameof(bulkLow));
            m_TransformBindings = transformBindings ?? Array.Empty<CharacterAclTransformTrackBinding>();
            m_ScalarBindings = scalarBindings ?? Array.Empty<CharacterAclScalarTrackBinding>();
        }

        public void RequireValid()
        {
            if (!string.Equals(SchemaVersion, CurrentSchemaVersion, StringComparison.Ordinal) ||
                Backend != CharacterAnimationSamplingBackendKind.Acl ||
                string.IsNullOrWhiteSpace(ResourceIdentity) ||
                string.IsNullOrWhiteSpace(ResourceAddress) ||
                string.IsNullOrWhiteSpace(SourceIdentity) ||
                string.IsNullOrWhiteSpace(FormalClipIdentity) ||
                string.IsNullOrWhiteSpace(SourceDependencyHash) ||
                string.IsNullOrWhiteSpace(RigId) ||
                string.IsNullOrWhiteSpace(RigRevision) ||
                string.IsNullOrWhiteSpace(ReferencePoseIdentity) ||
                !CharacterAclHash.IsSha256(BindingHash) ||
                !CharacterAclHash.IsSha256(ContentHash) ||
                string.IsNullOrWhiteSpace(BuildRevision) ||
                !CharacterAclHash.IsSha256(GroupContentHash) ||
                !CharacterAclHash.IsSha256(NativeArtifactIdentity) ||
                !CharacterAclHash.IsSha256(NativeBinarySha256) ||
                !string.Equals(NativePlatform, OfficialNativePlatform, StringComparison.Ordinal) ||
                NativeAbiVersion != OfficialNativeAbiVersion ||
                NativePayloadFormatVersion != PayloadFormatVersion ||
                GroupClipIndex < 0 ||
                PoseBoneCount <= 0 ||
                PhysicalBoneCount <= 0 || PhysicalBoneCount > PoseBoneCount ||
                TransformTrackCount != PhysicalBoneCount ||
                ScalarTrackCount < 0 ||
                !float.IsFinite(SampleRate) || SampleRate <= 0f ||
                !float.IsFinite(StartTimeSeconds) || StartTimeSeconds < 0f ||
                !float.IsFinite(StopTimeSeconds) || StopTimeSeconds <= StartTimeSeconds)
            {
                throw new InvalidOperationException("ACL animation resource manifest is invalid.");
            }
            Compression.RequireValid();
            Transform.RequireValid(CharacterAclDataBlockKind.Transform, true, PayloadFormatVersion);
            Scalar.RequireValid(CharacterAclDataBlockKind.Scalar, ScalarTrackCount > 0, PayloadFormatVersion);
            DatabaseHeader.RequireValid(CharacterAclDataBlockKind.DatabaseHeader, DatabaseUsed, PayloadFormatVersion);
            BulkMedium.RequireValid(CharacterAclDataBlockKind.BulkMedium, DatabaseUsed && Compression.EnableMediumTier, PayloadFormatVersion);
            BulkLow.RequireValid(CharacterAclDataBlockKind.BulkLow, DatabaseUsed && Compression.EnableLowTier, PayloadFormatVersion);
            if (TransformBindings.Count != TransformTrackCount ||
                ScalarTrackCount > 0 && ScalarBindings.Count == 0)
                throw new InvalidOperationException("ACL animation resource track binding counts are invalid.");
            var poseBones = new HashSet<int>();
            var transformTracks = new HashSet<int>();
            for (int i = 0; i < TransformBindings.Count; i++)
            {
                CharacterAclTransformTrackBinding binding = TransformBindings[i] ?? throw new InvalidOperationException("ACL transform binding is missing.");
                binding.RequireValid(PhysicalBoneCount);
                if (!poseBones.Add(binding.PoseBoneIndex) || !transformTracks.Add(binding.TrackIndex))
                    throw new InvalidOperationException("ACL transform bindings contain a duplicate index.");
            }
            var parameters = new HashSet<PoseParameterId>();
            var scalarTracks = new HashSet<int>();
            for (int i = 0; i < ScalarBindings.Count; i++)
            {
                CharacterAclScalarTrackBinding binding = ScalarBindings[i] ?? throw new InvalidOperationException("ACL scalar binding is missing.");
                binding.RequireValid(int.MaxValue, ScalarTrackCount);
                if (!parameters.Add(binding.ParameterId) || !binding.IsConstant && !scalarTracks.Add(binding.TrackIndex))
                    throw new InvalidOperationException("ACL scalar bindings contain a duplicate identity.");
            }
        }
    }
}
