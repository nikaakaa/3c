using System;
using System.Globalization;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;
using ThirdPersonSimulation;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Editor.MotionMatching
{
    public enum MotionMatchingSourceClipAssetIdentityStatus : byte
    {
        InvalidEntry = 0,
        AssetGuidUnresolved = 1,
        LocalFileIdUnresolved = 2,
        Resolved = 3
    }

    public enum MotionMatchingSourceClipInspectionStatus : byte
    {
        InvalidEntry = 0,
        AssetIdentityUnresolved = 1,
        AnimationClipMissing = 2,
        ImporterMissing = 3,
        CompatibilityModeInvalid = 4,
        ImporterCompatibilityMismatch = 5,
        ClipCompatibilityMismatch = 6,
        HumanoidAvatarMissing = 7,
        HumanoidAvatarInvalid = 8,
        HumanoidAvatarIdentityMissing = 9,
        GenericRootIdentityMissing = 10,
        GenericHierarchyIdentityMissing = 11,
        Ready = 12
    }

    public readonly struct MotionMatchingSourceClipInspection
    {
        public MotionMatchingSourceClipInspection(
            CharacterMotionMatchingSourceClipId sourceClipId,
            string assetGuid,
            long localFileId,
            MotionMatchingSamplingCompatibilityMode compatibilityMode,
            MotionMatchingSourceClipAssetIdentityStatus assetIdentityStatus,
            MotionMatchingSourceClipInspectionStatus status,
            string diagnostic,
            string assetPath,
            AnimationClip clip,
            AssetImporter importer,
            ModelImporter modelImporter,
            ModelImporterAnimationType declaredAnimationType,
            Avatar sourceAvatar,
            string sourceAvatarIdentity,
            string sourceRootIdentity,
            string sourceHierarchyIdentity,
            int sourceHierarchyPathCount)
        {
            SourceClipId = sourceClipId;
            AssetGuid = assetGuid ?? string.Empty;
            LocalFileId = localFileId;
            CompatibilityMode = compatibilityMode;
            AssetIdentityStatus = assetIdentityStatus;
            Status = status;
            Diagnostic = diagnostic ?? string.Empty;
            AssetPath = assetPath ?? string.Empty;
            Clip = clip;
            Importer = importer;
            ModelImporter = modelImporter;
            DeclaredAnimationType = declaredAnimationType;
            SourceAvatar = sourceAvatar;
            SourceAvatarIdentity = sourceAvatarIdentity ?? string.Empty;
            SourceRootIdentity = sourceRootIdentity ?? string.Empty;
            SourceHierarchyIdentity = sourceHierarchyIdentity ?? string.Empty;
            SourceHierarchyPathCount = sourceHierarchyPathCount;
        }

        public CharacterMotionMatchingSourceClipId SourceClipId { get; }
        public string AssetGuid { get; }
        public long LocalFileId { get; }
        public MotionMatchingSamplingCompatibilityMode CompatibilityMode { get; }
        public MotionMatchingSourceClipAssetIdentityStatus AssetIdentityStatus { get; }
        public MotionMatchingSourceClipInspectionStatus Status { get; }
        public string Diagnostic { get; }
        public string AssetPath { get; }
        public AnimationClip Clip { get; }
        public AssetImporter Importer { get; }
        public ModelImporter ModelImporter { get; }
        public ModelImporterAnimationType DeclaredAnimationType { get; }
        public Avatar SourceAvatar { get; }
        public string SourceAvatarIdentity { get; }
        public string SourceRootIdentity { get; }
        public string SourceHierarchyIdentity { get; }
        public int SourceHierarchyPathCount { get; }
        public bool StableAssetIdentityResolved => AssetIdentityStatus == MotionMatchingSourceClipAssetIdentityStatus.Resolved;
        public bool AnimationClipExists => Clip != null;
        public bool ImporterExists => Importer != null;
        public bool CompatibilityDeclared =>
            ModelImporter != null &&
            (CompatibilityMode == MotionMatchingSamplingCompatibilityMode.HumanoidRetargeted && DeclaredAnimationType == ModelImporterAnimationType.Human ||
             CompatibilityMode == MotionMatchingSamplingCompatibilityMode.ExactGenericRig && DeclaredAnimationType == ModelImporterAnimationType.Generic);
        public bool SourceAvatarIdentityAvailable => SourceAvatar != null && !string.IsNullOrEmpty(SourceAvatarIdentity);
        public bool SourceHierarchyIdentityAvailable =>
            !string.IsNullOrEmpty(SourceRootIdentity) &&
            !string.IsNullOrEmpty(SourceHierarchyIdentity) &&
            SourceHierarchyPathCount > 0;
        public bool HasFormalBuildPrerequisites => Status == MotionMatchingSourceClipInspectionStatus.Ready;
    }

    public static class MotionMatchingSourceClipResolver
    {
        public static MotionMatchingSourceClipInspection Inspect(
            CharacterMotionMatchingSourceClipEntry entry,
            MotionMatchingSamplingCompatibilityMode compatibilityMode)
        {
            if (entry == null)
                return Result(default, string.Empty, 0, compatibilityMode,
                    MotionMatchingSourceClipAssetIdentityStatus.InvalidEntry,
                    MotionMatchingSourceClipInspectionStatus.InvalidEntry,
                    "Motion Matching Source Clip entry is missing.");

            CharacterMotionMatchingSourceClipId sourceClipId = entry.SourceClipId;
            string assetGuid = entry.AnimationClipAssetGuid;
            long localFileId = entry.AnimationClipLocalFileId;
            if (!sourceClipId.IsValid || !IsAssetGuid(assetGuid) || localFileId == 0)
                return Result(sourceClipId, assetGuid, localFileId, compatibilityMode,
                    MotionMatchingSourceClipAssetIdentityStatus.InvalidEntry,
                    MotionMatchingSourceClipInspectionStatus.InvalidEntry,
                    "Motion Matching Source Clip entry has no complete stable asset identity.");

            string assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);
            if (string.IsNullOrEmpty(assetPath))
                return Result(sourceClipId, assetGuid, localFileId, compatibilityMode,
                    MotionMatchingSourceClipAssetIdentityStatus.AssetGuidUnresolved,
                    MotionMatchingSourceClipInspectionStatus.AssetIdentityUnresolved,
                    $"SourceClipId '{sourceClipId}' asset GUID cannot be resolved.");

            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            UnityEngine.Object resolvedAsset = null;
            for (int i = 0; i < assets.Length; i++)
            {
                UnityEngine.Object asset = assets[i];
                if (asset != null &&
                    AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out string resolvedGuid, out long resolvedLocalId) &&
                    string.Equals(resolvedGuid, assetGuid, StringComparison.Ordinal) &&
                    resolvedLocalId == localFileId)
                {
                    resolvedAsset = asset;
                    break;
                }
            }
            if (resolvedAsset == null)
                return Result(sourceClipId, assetGuid, localFileId, compatibilityMode,
                    MotionMatchingSourceClipAssetIdentityStatus.LocalFileIdUnresolved,
                    MotionMatchingSourceClipInspectionStatus.AssetIdentityUnresolved,
                    $"SourceClipId '{sourceClipId}' local file id cannot be resolved.", assetPath);

            AnimationClip clip = resolvedAsset as AnimationClip;
            if (clip == null)
                return Result(sourceClipId, assetGuid, localFileId, compatibilityMode,
                    MotionMatchingSourceClipAssetIdentityStatus.Resolved,
                    MotionMatchingSourceClipInspectionStatus.AnimationClipMissing,
                    $"SourceClipId '{sourceClipId}' stable asset identity does not resolve to an AnimationClip.", assetPath);

            AssetImporter importer = AssetImporter.GetAtPath(assetPath);
            if (importer == null)
                return Result(sourceClipId, assetGuid, localFileId, compatibilityMode,
                    MotionMatchingSourceClipAssetIdentityStatus.Resolved,
                    MotionMatchingSourceClipInspectionStatus.ImporterMissing,
                    $"SourceClipId '{sourceClipId}' has no AssetImporter.", assetPath, clip);

            ModelImporter modelImporter = importer as ModelImporter;
            if (!Enum.IsDefined(typeof(MotionMatchingSamplingCompatibilityMode), compatibilityMode))
                return Result(sourceClipId, assetGuid, localFileId, compatibilityMode,
                    MotionMatchingSourceClipAssetIdentityStatus.Resolved,
                    MotionMatchingSourceClipInspectionStatus.CompatibilityModeInvalid,
                    $"SourceClipId '{sourceClipId}' has no explicit sampling compatibility mode.", assetPath, clip, importer, modelImporter);
            if (modelImporter == null)
                return Result(sourceClipId, assetGuid, localFileId, compatibilityMode,
                    MotionMatchingSourceClipAssetIdentityStatus.Resolved,
                    MotionMatchingSourceClipInspectionStatus.ImporterCompatibilityMismatch,
                    $"SourceClipId '{sourceClipId}' importer does not declare a ModelImporter animation type.", assetPath, clip, importer);

            if (compatibilityMode == MotionMatchingSamplingCompatibilityMode.HumanoidRetargeted)
                return InspectHumanoid(sourceClipId, assetGuid, localFileId, compatibilityMode, assetPath, clip, importer, modelImporter, assets);
            return InspectGeneric(sourceClipId, assetGuid, localFileId, compatibilityMode, assetPath, clip, importer, modelImporter);
        }

        public static AnimationClip Resolve(CharacterMotionMatchingSourceClipEntry entry)
        {
            if (entry == null)
                throw new ArgumentNullException(nameof(entry));
            entry.RequireValid();
            string path = AssetDatabase.GUIDToAssetPath(entry.AnimationClipAssetGuid);
            if (string.IsNullOrEmpty(path))
                throw new InvalidOperationException($"SourceClipId '{entry.SourceClipId}' asset GUID cannot be resolved.");
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            for (int i = 0; i < assets.Length; i++)
            {
                if (!(assets[i] is AnimationClip clip))
                    continue;
                if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(clip, out string guid, out long localId) &&
                    string.Equals(guid, entry.AnimationClipAssetGuid, StringComparison.Ordinal) &&
                    localId == entry.AnimationClipLocalFileId)
                    return clip;
            }
            throw new InvalidOperationException($"SourceClipId '{entry.SourceClipId}' local file id no longer resolves to an AnimationClip.");
        }

        public static string DependencyHash(AnimationClip clip)
        {
            if (!clip)
                throw new ArgumentNullException(nameof(clip));
            string path = AssetDatabase.GetAssetPath(clip);
            if (string.IsNullOrEmpty(path))
                throw new InvalidOperationException("Motion Matching source Clip is not a persisted asset.");
            return AssetDatabase.GetAssetDependencyHash(path).ToString();
        }

        static MotionMatchingSourceClipInspection InspectHumanoid(
            CharacterMotionMatchingSourceClipId sourceClipId,
            string assetGuid,
            long localFileId,
            MotionMatchingSamplingCompatibilityMode compatibilityMode,
            string assetPath,
            AnimationClip clip,
            AssetImporter importer,
            ModelImporter modelImporter,
            UnityEngine.Object[] assets)
        {
            if (modelImporter.animationType != ModelImporterAnimationType.Human)
                return Result(sourceClipId, assetGuid, localFileId, compatibilityMode,
                    MotionMatchingSourceClipAssetIdentityStatus.Resolved,
                    MotionMatchingSourceClipInspectionStatus.ImporterCompatibilityMismatch,
                    $"SourceClipId '{sourceClipId}' importer does not declare Humanoid animation.", assetPath, clip, importer, modelImporter);
            if (!clip.humanMotion)
                return Result(sourceClipId, assetGuid, localFileId, compatibilityMode,
                    MotionMatchingSourceClipAssetIdentityStatus.Resolved,
                    MotionMatchingSourceClipInspectionStatus.ClipCompatibilityMismatch,
                    $"SourceClipId '{sourceClipId}' AnimationClip is not declared as Humanoid motion.", assetPath, clip, importer, modelImporter);

            Avatar avatar = null;
            bool ambiguousEmbeddedAvatar = false;
            if (modelImporter.avatarSetup == ModelImporterAvatarSetup.CopyFromOther)
            {
                avatar = modelImporter.sourceAvatar;
            }
            else if (modelImporter.avatarSetup == ModelImporterAvatarSetup.CreateFromThisModel)
            {
                for (int i = 0; i < assets.Length; i++)
                {
                    if (!(assets[i] is Avatar candidate))
                        continue;
                    if (avatar != null)
                    {
                        ambiguousEmbeddedAvatar = true;
                        avatar = null;
                        break;
                    }
                    avatar = candidate;
                }
            }
            if (avatar == null)
            {
                string diagnostic = ambiguousEmbeddedAvatar
                    ? $"SourceClipId '{sourceClipId}' contains multiple embedded Avatars and has no unique source Avatar identity."
                    : $"SourceClipId '{sourceClipId}' has no formally declared source Avatar.";
                return Result(sourceClipId, assetGuid, localFileId, compatibilityMode,
                    MotionMatchingSourceClipAssetIdentityStatus.Resolved,
                    MotionMatchingSourceClipInspectionStatus.HumanoidAvatarMissing,
                    diagnostic, assetPath, clip, importer, modelImporter);
            }
            if (!avatar.isValid || !avatar.isHuman)
                return Result(sourceClipId, assetGuid, localFileId, compatibilityMode,
                    MotionMatchingSourceClipAssetIdentityStatus.Resolved,
                    MotionMatchingSourceClipInspectionStatus.HumanoidAvatarInvalid,
                    $"SourceClipId '{sourceClipId}' source Avatar is not a valid Humanoid Avatar.", assetPath, clip, importer, modelImporter,
                    avatar: avatar);
            if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(avatar, out string avatarGuid, out long avatarLocalId) ||
                string.IsNullOrEmpty(avatarGuid) || avatarLocalId == 0)
                return Result(sourceClipId, assetGuid, localFileId, compatibilityMode,
                    MotionMatchingSourceClipAssetIdentityStatus.Resolved,
                    MotionMatchingSourceClipInspectionStatus.HumanoidAvatarIdentityMissing,
                    $"SourceClipId '{sourceClipId}' source Avatar has no stable asset identity.", assetPath, clip, importer, modelImporter,
                    avatar: avatar);

            string avatarIdentity = avatarGuid + ":" + avatarLocalId.ToString(CultureInfo.InvariantCulture);
            return Result(sourceClipId, assetGuid, localFileId, compatibilityMode,
                MotionMatchingSourceClipAssetIdentityStatus.Resolved,
                MotionMatchingSourceClipInspectionStatus.Ready,
                string.Empty, assetPath, clip, importer, modelImporter,
                avatar: avatar, avatarIdentity: avatarIdentity);
        }

        static MotionMatchingSourceClipInspection InspectGeneric(
            CharacterMotionMatchingSourceClipId sourceClipId,
            string assetGuid,
            long localFileId,
            MotionMatchingSamplingCompatibilityMode compatibilityMode,
            string assetPath,
            AnimationClip clip,
            AssetImporter importer,
            ModelImporter modelImporter)
        {
            if (modelImporter.animationType != ModelImporterAnimationType.Generic)
                return Result(sourceClipId, assetGuid, localFileId, compatibilityMode,
                    MotionMatchingSourceClipAssetIdentityStatus.Resolved,
                    MotionMatchingSourceClipInspectionStatus.ImporterCompatibilityMismatch,
                    $"SourceClipId '{sourceClipId}' importer does not declare Generic animation.", assetPath, clip, importer, modelImporter);
            if (clip.humanMotion)
                return Result(sourceClipId, assetGuid, localFileId, compatibilityMode,
                    MotionMatchingSourceClipAssetIdentityStatus.Resolved,
                    MotionMatchingSourceClipInspectionStatus.ClipCompatibilityMismatch,
                    $"SourceClipId '{sourceClipId}' AnimationClip is declared as Humanoid motion instead of Generic.", assetPath, clip, importer, modelImporter);

            string rootIdentity = modelImporter.motionNodeName;
            if (string.IsNullOrWhiteSpace(rootIdentity))
                return Result(sourceClipId, assetGuid, localFileId, compatibilityMode,
                    MotionMatchingSourceClipAssetIdentityStatus.Resolved,
                    MotionMatchingSourceClipInspectionStatus.GenericRootIdentityMissing,
                    $"SourceClipId '{sourceClipId}' ModelImporter has no explicit motion root node identity.", assetPath, clip, importer, modelImporter);

            string[] importerPaths = modelImporter.transformPaths;
            if (importerPaths == null || importerPaths.Length == 0)
                return Result(sourceClipId, assetGuid, localFileId, compatibilityMode,
                    MotionMatchingSourceClipAssetIdentityStatus.Resolved,
                    MotionMatchingSourceClipInspectionStatus.GenericHierarchyIdentityMissing,
                    $"SourceClipId '{sourceClipId}' ModelImporter exposes no source hierarchy paths.", assetPath, clip, importer, modelImporter,
                    rootIdentity: rootIdentity);

            string[] canonicalPaths = (string[])importerPaths.Clone();
            for (int i = 0; i < canonicalPaths.Length; i++)
            {
                if (canonicalPaths[i] == null)
                    return Result(sourceClipId, assetGuid, localFileId, compatibilityMode,
                        MotionMatchingSourceClipAssetIdentityStatus.Resolved,
                        MotionMatchingSourceClipInspectionStatus.GenericHierarchyIdentityMissing,
                        $"SourceClipId '{sourceClipId}' ModelImporter source hierarchy contains a missing path.", assetPath, clip, importer, modelImporter,
                        rootIdentity: rootIdentity);
            }
            Array.Sort(canonicalPaths, StringComparer.Ordinal);
            for (int i = 1; i < canonicalPaths.Length; i++)
            {
                if (string.Equals(canonicalPaths[i - 1], canonicalPaths[i], StringComparison.Ordinal))
                    return Result(sourceClipId, assetGuid, localFileId, compatibilityMode,
                        MotionMatchingSourceClipAssetIdentityStatus.Resolved,
                        MotionMatchingSourceClipInspectionStatus.GenericHierarchyIdentityMissing,
                        $"SourceClipId '{sourceClipId}' ModelImporter source hierarchy contains duplicate path '{canonicalPaths[i]}'.", assetPath, clip, importer, modelImporter,
                        rootIdentity: rootIdentity);
            }

            var identityParts = new string[canonicalPaths.Length + 2];
            identityParts[0] = "motion-matching-source-hierarchy/v1";
            identityParts[1] = rootIdentity;
            Array.Copy(canonicalPaths, 0, identityParts, 2, canonicalPaths.Length);
            string hierarchyIdentity = StableHash.Compute(identityParts).Value;
            return Result(sourceClipId, assetGuid, localFileId, compatibilityMode,
                MotionMatchingSourceClipAssetIdentityStatus.Resolved,
                MotionMatchingSourceClipInspectionStatus.Ready,
                string.Empty, assetPath, clip, importer, modelImporter,
                rootIdentity: rootIdentity, hierarchyIdentity: hierarchyIdentity, hierarchyPathCount: canonicalPaths.Length);
        }

        static MotionMatchingSourceClipInspection Result(
            CharacterMotionMatchingSourceClipId sourceClipId,
            string assetGuid,
            long localFileId,
            MotionMatchingSamplingCompatibilityMode compatibilityMode,
            MotionMatchingSourceClipAssetIdentityStatus assetIdentityStatus,
            MotionMatchingSourceClipInspectionStatus status,
            string diagnostic,
            string assetPath = "",
            AnimationClip clip = null,
            AssetImporter importer = null,
            ModelImporter modelImporter = null,
            Avatar avatar = null,
            string avatarIdentity = "",
            string rootIdentity = "",
            string hierarchyIdentity = "",
            int hierarchyPathCount = 0)
        {
            return new MotionMatchingSourceClipInspection(
                sourceClipId, assetGuid, localFileId, compatibilityMode, assetIdentityStatus, status, diagnostic,
                assetPath, clip, importer, modelImporter,
                modelImporter != null ? modelImporter.animationType : default,
                avatar, avatarIdentity, rootIdentity, hierarchyIdentity, hierarchyPathCount);
        }

        static bool IsAssetGuid(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length != 32)
                return false;
            for (int i = 0; i < value.Length; i++)
            {
                char character = value[i];
                if (character < '0' || character > '9' && character < 'a' || character > 'f')
                    return false;
            }
            return true;
        }
    }
}
