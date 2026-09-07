using System;
using ThirdPersonCharacter.Editor.CharacterSimulation.Compilation.Animation;
using ThirdPersonCharacter.Pipeline.Animation;

namespace ThirdPersonCharacter.Editor.CharacterSimulation.Compilation.Animation.ACL
{
    internal static class CharacterAclAnimationArtifactIdentity
    {
        const string ResourceIdentityPrefix = "acl-animation-resource-v3";
        const string OutputFolderRoot = "Assets/AssetRaw/Product/Gameplay/ACL";
        internal const string AssetStemPrefix = "acl-";
        internal const string StagingFolderPrefix = "__s-";
        internal const string BackupFolderPrefix = "__b-";

        internal static string RequireOwnerAssetGuid(string ownerAssetGuid)
        {
            if (string.IsNullOrWhiteSpace(ownerAssetGuid))
                throw new ArgumentException("Character owner asset GUID is required.", nameof(ownerAssetGuid));
            string normalized = ownerAssetGuid.Trim().ToLowerInvariant();
            if (normalized.Length != 32)
                throw new ArgumentException("Character owner asset GUID is invalid.", nameof(ownerAssetGuid));
            for (int i = 0; i < normalized.Length; i++)
            {
                char value = normalized[i];
                if ((value < '0' || value > '9') && (value < 'a' || value > 'f'))
                    throw new ArgumentException("Character owner asset GUID is invalid.", nameof(ownerAssetGuid));
            }
            return normalized;
        }

        internal static string RequireGroupContentHash(string groupContentHash)
        {
            if (!CharacterAclHash.IsSha256(groupContentHash))
                throw new ArgumentException("ACL animation group content hash is invalid.", nameof(groupContentHash));
            return groupContentHash;
        }

        internal static string GetResourceIdentity(
            string ownerAssetGuid,
            string groupContentHash) =>
            $"{ResourceIdentityPrefix}-{RequireOwnerAssetGuid(ownerAssetGuid)}-{RequireGroupContentHash(groupContentHash)}";

        internal static string GetResourceIdentityPrefix(string ownerAssetGuid) =>
            $"{ResourceIdentityPrefix}-{RequireOwnerAssetGuid(ownerAssetGuid)}-";

        internal static string GetOutputFolder(string ownerAssetGuid) =>
            $"{OutputFolderRoot}/{RequireOwnerAssetGuid(ownerAssetGuid)}";

        internal static string GetAssetStem(string groupContentHash) =>
            $"{AssetStemPrefix}{RequireGroupContentHash(groupContentHash)}";

        internal static string GetAssetStemPrefix() => AssetStemPrefix;
    }

    [Serializable]
    public sealed class CharacterAclAnimationQualityReport
    {
        public string schema = "character-acl-animation-quality-report/v3";
        public string formalClipIdentity = string.Empty;
        public string sourceIdentity = string.Empty;
        public string transformPayloadHash = string.Empty;
        public string scalarPayloadHash = string.Empty;
        public int sampleCount;
        public float sampleRate;
        public float maxClipToSamplingPositionError;
        public float maxClipToSamplingRotationError;
        public float maxClipToSamplingScaleError;
        public float maxSamplingToAclPositionError;
        public float maxSamplingToAclRotationError;
        public float maxSamplingToAclScaleError;
        public float maxClipToSamplingScalarError;
        public float maxSamplingToAclScalarError;
        public float maxClipToAclPositionError;
        public float maxClipToAclRotationError;
        public float maxClipToAclScaleError;
        public float maxClipToAclScalarError;
        public bool zzzRestorationEvaluated;
        public float zzzRestorationError;
        public bool publishable;
        public string[] errors = Array.Empty<string>();
        public CharacterAclAnimationTrackQualityReport[] tracks =
            Array.Empty<CharacterAclAnimationTrackQualityReport>();
        public CharacterAnimationSamplingQualityReport sampling;
    }

    [Serializable]
    public sealed class CharacterAclAnimationTrackQualityReport
    {
        public string trackIdentity = string.Empty;
        public int physicalBoneIndex = -1;
        public int scalarParameterIndex = -1;
        public float maxClipToSamplingPositionError;
        public float maxClipToSamplingRotationError;
        public float maxClipToSamplingScaleError;
        public float maxSamplingToAclPositionError;
        public float maxSamplingToAclRotationError;
        public float maxSamplingToAclScaleError;
        public float maxClipToSamplingScalarError;
        public float maxSamplingToAclScalarError;
        public float maxClipToAclPositionError;
        public float maxClipToAclRotationError;
        public float maxClipToAclScaleError;
        public float maxClipToAclScalarError;
        public string[] errors = Array.Empty<string>();
    }

    internal sealed class CharacterAclAnimationCompressionResult
    {
        internal CharacterAclAnimationCompressionResult(
            int abiVersion,
            int payloadFormatVersion,
            string nativeArtifactIdentity,
            byte[] transformPayload,
            byte[] scalarPayload,
            byte[] databaseHeaderPayload,
            byte[] bulkMediumPayload,
            byte[] bulkLowPayload,
            CharacterAclTransformTrackBinding[] transformBindings,
            CharacterAclScalarTrackBinding[] scalarBindings,
            int groupClipIndex = 0)
        {
            AbiVersion = abiVersion;
            PayloadFormatVersion = payloadFormatVersion;
            NativeArtifactIdentity = string.IsNullOrWhiteSpace(nativeArtifactIdentity) ||
                !CharacterAclHash.IsSha256(nativeArtifactIdentity)
                ? throw new ArgumentException(
                    "ACL native artifact identity is invalid.",
                    nameof(nativeArtifactIdentity))
                : nativeArtifactIdentity;
            TransformPayload = transformPayload ?? throw new ArgumentNullException(nameof(transformPayload));
            ScalarPayload = scalarPayload ?? throw new ArgumentNullException(nameof(scalarPayload));
            DatabaseHeaderPayload = databaseHeaderPayload ?? throw new ArgumentNullException(nameof(databaseHeaderPayload));
            BulkMediumPayload = bulkMediumPayload ?? throw new ArgumentNullException(nameof(bulkMediumPayload));
            BulkLowPayload = bulkLowPayload ?? throw new ArgumentNullException(nameof(bulkLowPayload));
            TransformBindings = transformBindings ?? throw new ArgumentNullException(nameof(transformBindings));
            ScalarBindings = scalarBindings ?? throw new ArgumentNullException(nameof(scalarBindings));
            GroupClipIndex = groupClipIndex >= 0
                ? groupClipIndex
                : throw new ArgumentOutOfRangeException(nameof(groupClipIndex));
            if (abiVersion <= 0 || payloadFormatVersion <= 0 || TransformPayload.Length == 0)
                throw new ArgumentException("ACL animation compression result is invalid.");
        }

        internal int AbiVersion { get; }
        internal int PayloadFormatVersion { get; }
        internal string NativeArtifactIdentity { get; }
        internal byte[] TransformPayload { get; }
        internal byte[] ScalarPayload { get; }
        internal byte[] DatabaseHeaderPayload { get; }
        internal byte[] BulkMediumPayload { get; }
        internal byte[] BulkLowPayload { get; }
        internal CharacterAclTransformTrackBinding[] TransformBindings { get; }
        internal CharacterAclScalarTrackBinding[] ScalarBindings { get; }
        internal int GroupClipIndex { get; }
        internal string TransformPayloadHash => CharacterAclHash.Compute(TransformPayload);
        internal string ScalarPayloadHash => ScalarPayload.Length == 0
            ? string.Empty
            : CharacterAclHash.Compute(ScalarPayload);
    }

    internal sealed class CharacterAclAnimationBuildRequest
    {
        internal CharacterAclAnimationBuildRequest(
            CharacterAnimationAuthoringReadRequest authoringRequest,
            CharacterAclCompressionSettings settings)
        {
            AuthoringRequest = authoringRequest ?? throw new ArgumentNullException(nameof(authoringRequest));
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        internal CharacterAnimationAuthoringReadRequest AuthoringRequest { get; }
        internal CharacterAclCompressionSettings Settings { get; }
    }

    internal sealed class CharacterAclAnimationGroupArtifact
    {
        internal CharacterAclAnimationGroupArtifact(
            int groupIndex,
            string groupContentHash,
            string buildInputIdentity,
            CharacterAclAnimationResourceManifest[] manifests,
            CharacterAclAnimationQualityReport[] qualityReports)
        {
            GroupIndex = groupIndex >= 0
                ? groupIndex
                : throw new ArgumentOutOfRangeException(nameof(groupIndex));
            GroupContentHash = string.IsNullOrWhiteSpace(groupContentHash)
                ? throw new ArgumentException("ACL animation group content hash is required.", nameof(groupContentHash))
                : groupContentHash;
            BuildInputIdentity = string.IsNullOrWhiteSpace(buildInputIdentity)
                ? throw new ArgumentException("ACL animation group build input identity is required.", nameof(buildInputIdentity))
                : buildInputIdentity;
            Manifests = manifests ?? throw new ArgumentNullException(nameof(manifests));
            QualityReports = qualityReports ?? throw new ArgumentNullException(nameof(qualityReports));
            if (Manifests.Length == 0 ||
                QualityReports.Length != Manifests.Length)
                throw new ArgumentException("ACL animation group artifact lengths are inconsistent.");
            for (int i = 0; i < Manifests.Length; i++)
            {
                CharacterAclAnimationResourceManifest manifest = Manifests[i] ??
                    throw new ArgumentException("ACL animation group manifest is missing.", nameof(manifests));
                if (manifest.GroupClipIndex != i ||
                    !string.Equals(manifest.GroupContentHash, GroupContentHash, StringComparison.Ordinal))
                    throw new ArgumentException("ACL animation group manifest mapping is invalid.", nameof(manifests));
            }
        }

        internal CharacterAclAnimationGroupArtifact(
            int groupIndex,
            string groupContentHash,
            string buildInputIdentity,
            CharacterAclAnimationResourceManifest[] manifests,
            byte[][] transformPayloads,
            byte[][] scalarPayloads,
            byte[] databaseHeaderPayload,
            byte[] bulkMediumPayload,
            byte[] bulkLowPayload,
            CharacterAclAnimationQualityReport[] qualityReports)
        {
            GroupIndex = groupIndex >= 0
                ? groupIndex
                : throw new ArgumentOutOfRangeException(nameof(groupIndex));
            GroupContentHash = string.IsNullOrWhiteSpace(groupContentHash)
                ? throw new ArgumentException("ACL animation group content hash is required.", nameof(groupContentHash))
                : groupContentHash;
            BuildInputIdentity = string.IsNullOrWhiteSpace(buildInputIdentity)
                ? throw new ArgumentException("ACL animation group build input identity is required.", nameof(buildInputIdentity))
                : buildInputIdentity;
            Manifests = manifests ?? throw new ArgumentNullException(nameof(manifests));
            TransformPayloads = transformPayloads ?? throw new ArgumentNullException(nameof(transformPayloads));
            ScalarPayloads = scalarPayloads ?? throw new ArgumentNullException(nameof(scalarPayloads));
            DatabaseHeaderPayload = databaseHeaderPayload ?? throw new ArgumentNullException(nameof(databaseHeaderPayload));
            BulkMediumPayload = bulkMediumPayload ?? throw new ArgumentNullException(nameof(bulkMediumPayload));
            BulkLowPayload = bulkLowPayload ?? throw new ArgumentNullException(nameof(bulkLowPayload));
            QualityReports = qualityReports ?? throw new ArgumentNullException(nameof(qualityReports));
            if (Manifests.Length == 0 ||
                TransformPayloads.Length != Manifests.Length ||
                ScalarPayloads.Length != Manifests.Length ||
                QualityReports.Length != Manifests.Length)
                throw new ArgumentException("ACL animation group artifact lengths are inconsistent.");
            for (int i = 0; i < Manifests.Length; i++)
            {
                CharacterAclAnimationResourceManifest manifest = Manifests[i] ??
                    throw new ArgumentException("ACL animation group manifest is missing.", nameof(manifests));
                if (manifest.GroupClipIndex != i ||
                    !string.Equals(manifest.GroupContentHash, GroupContentHash, StringComparison.Ordinal))
                    throw new ArgumentException("ACL animation group manifest mapping is invalid.", nameof(manifests));
            }
        }

        internal int GroupIndex { get; }
        internal string GroupContentHash { get; }
        internal string BuildInputIdentity { get; }
        internal CharacterAclAnimationResourceManifest[] Manifests { get; }
        internal byte[][] TransformPayloads { get; }
        internal byte[][] ScalarPayloads { get; }
        internal byte[] DatabaseHeaderPayload { get; }
        internal byte[] BulkMediumPayload { get; }
        internal byte[] BulkLowPayload { get; }
        internal CharacterAclAnimationQualityReport[] QualityReports { get; }
        internal bool HasPayloads => TransformPayloads != null;

        internal CharacterAnimationCompiledResourceDescriptor CreateDescriptor(
            int resourceIndex)
        {
            CharacterAclAnimationResourceManifest manifest = Manifests[0];
            return new CharacterAnimationCompiledResourceDescriptor(
                resourceIndex,
                GroupIndex,
                manifest.ResourceAddress,
                GroupContentHash,
                (CharacterAclAnimationResourceManifest[])Manifests.Clone());
        }
    }

    [Serializable]
    internal sealed class CharacterAclAnimationQualityBundle
    {
        public string schema = "character-acl-animation-quality-bundle/v1";
        public CharacterAclAnimationQualityReport[] reports = Array.Empty<CharacterAclAnimationQualityReport>();

        internal CharacterAclAnimationQualityBundle(
            CharacterAclAnimationQualityReport[] qualityReports)
        {
            reports = qualityReports ?? throw new ArgumentNullException(nameof(qualityReports));
        }
    }
}
