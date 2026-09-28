using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using ThirdPersonCharacter.Pipeline.Animation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    public enum CharacterAnimationPropertyImportDiffKind : byte
    {
        Add = 1,
        Update = 2,
        Remove = 3,
        Rename = 4
    }

    public sealed class CharacterAnimationPropertyImportDiff
    {
        public CharacterAnimationPropertyImportDiff(
            CharacterAnimationPropertyImportDiffKind kind,
            string target,
            string before,
            string after,
            string detail)
        {
            Kind = kind;
            Target = target ?? string.Empty;
            Before = before ?? string.Empty;
            After = after ?? string.Empty;
            Detail = detail ?? string.Empty;
        }

        public CharacterAnimationPropertyImportDiffKind Kind { get; }
        public string Target { get; }
        public string Before { get; }
        public string After { get; }
        public string Detail { get; }
    }

    public sealed class CharacterAnimationPropertyImportClipTarget
    {
        internal CharacterAnimationPropertyImportClipTarget(
            AnimationClip clip,
            CharacterAnimationClipContentIdentity identity,
            string sourceCategory,
            CharacterAnimationSamplingBackendKind currentBackend,
            string blendShapeCurveHash,
            bool hasBlendShapeCurves,
            int linearizationCount,
            IReadOnlyList<string> rootEvidenceBindings,
            string rootEvidenceHash)
        {
            Clip = clip ? clip : throw new ArgumentNullException(nameof(clip));
            Identity = identity;
            SourceCategory = sourceCategory ?? string.Empty;
            CurrentBackend = currentBackend;
            BlendShapeCurveHash = blendShapeCurveHash ?? string.Empty;
            HasBlendShapeCurves = hasBlendShapeCurves;
            LinearizationCount = linearizationCount;
            RootEvidenceBindings = rootEvidenceBindings?.ToArray() ?? Array.Empty<string>();
            RootEvidenceHash = rootEvidenceHash ?? string.Empty;
        }

        internal AnimationClip Clip { get; }
        internal CharacterAnimationClipContentIdentity Identity { get; }
        internal CharacterAnimationSamplingBackendKind CurrentBackend { get; }
        public string AssetPath => Identity.AssetPath;
        public string AssetGuid => Identity.AssetGuid;
        public long LocalFileId => Identity.LocalFileId;
        public string DependencyHash => Identity.FullDependencyHash;
        public string SourceCategory { get; }
        public string CurrentBackendName => CurrentBackend.ToString();
        public string TargetBackend => CharacterAnimationSamplingBackendKind.Acl.ToString();
        public string BlendShapeCurveHash { get; }
        public bool HasBlendShapeCurves { get; }
        public int LinearizationCount { get; }
        public IReadOnlyList<string> RootEvidenceBindings { get; }
        public int RootEvidenceCount => RootEvidenceBindings.Count;
        public string RootEvidenceHash { get; }
    }

    public sealed class CharacterAnimationPropertyImportCurveTarget
    {
        internal CharacterAnimationPropertyImportCurveTarget(
            string sourceBlendShapeName,
            string targetBlendShapeName,
            int blendShapeIndex,
            string parameterId,
            bool requiresRename)
        {
            SourceBlendShapeName = sourceBlendShapeName ?? string.Empty;
            TargetBlendShapeName = targetBlendShapeName ?? string.Empty;
            BlendShapeIndex = blendShapeIndex;
            ParameterId = parameterId ?? string.Empty;
            RequiresRename = requiresRename;
        }

        public string SourceBlendShapeName { get; }
        public string TargetBlendShapeName { get; }
        public int BlendShapeIndex { get; }
        public string ParameterId { get; }
        public bool RequiresRename { get; }
        public string SourcePropertyName => "blendShape." + SourceBlendShapeName;
        public string TargetPropertyName => "blendShape." + TargetBlendShapeName;
    }

    public sealed class CharacterAnimationPropertyImportPoseGraphTarget
    {
        internal CharacterAnimationPropertyImportPoseGraphTarget(
            string graphId,
            int currentParameterCount,
            int targetParameterCount,
            int currentPolicyCount,
            int targetPolicyCount)
        {
            GraphId = graphId ?? string.Empty;
            CurrentParameterCount = currentParameterCount;
            TargetParameterCount = targetParameterCount;
            CurrentPolicyCount = currentPolicyCount;
            TargetPolicyCount = targetPolicyCount;
        }

        public string GraphId { get; }
        public int CurrentParameterCount { get; }
        public int TargetParameterCount { get; }
        public int CurrentPolicyCount { get; }
        public int TargetPolicyCount { get; }
        public bool IsRoot { get; internal set; }
    }

    public sealed class CharacterAnimationPropertyImportPrefabTarget
    {
        internal CharacterAnimationPropertyImportPrefabTarget(
            string assetPath,
            string rigId,
            string rigRevision,
            string rendererPath,
            string bindingId,
            string currentBindingId,
            string currentRendererPath,
            string currentMeshGuid,
            long currentMeshLocalFileId,
            string currentMeshContentHash,
            string meshGuid,
            long meshLocalFileId,
            string meshContentHash)
        {
            AssetPath = assetPath ?? string.Empty;
            RigId = rigId ?? string.Empty;
            RigRevision = rigRevision ?? string.Empty;
            RendererPath = rendererPath ?? string.Empty;
            BindingId = bindingId ?? string.Empty;
            CurrentBindingId = currentBindingId ?? string.Empty;
            CurrentRendererPath = currentRendererPath ?? string.Empty;
            CurrentMeshGuid = currentMeshGuid ?? string.Empty;
            CurrentMeshLocalFileId = currentMeshLocalFileId;
            CurrentMeshContentHash = currentMeshContentHash ?? string.Empty;
            MeshGuid = meshGuid ?? string.Empty;
            MeshLocalFileId = meshLocalFileId;
            MeshContentHash = meshContentHash ?? string.Empty;
        }

        public string AssetPath { get; }
        public string RigId { get; }
        public string RigRevision { get; }
        public string RendererPath { get; }
        public string BindingId { get; }
        public string CurrentBindingId { get; }
        public string CurrentRendererPath { get; }
        public string CurrentMeshGuid { get; }
        public long CurrentMeshLocalFileId { get; }
        public string CurrentMeshContentHash { get; }
        public string MeshGuid { get; }
        public long MeshLocalFileId { get; }
        public string MeshContentHash { get; }
    }

    public sealed class CharacterAnimationPropertyImportCompressionTarget
    {
        internal CharacterAnimationPropertyImportCompressionTarget(
            CharacterAclCompressionSettings settings)
        {
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        internal CharacterAclCompressionSettings Settings { get; }
        public string Revision => Settings.Revision;
        public int SampleRate => Settings.SampleRate;
        public float TransformPrecision => Settings.TransformPrecision;
        public float ScalePrecision => Settings.ScalePrecision;
        public float RotationPrecisionDegrees => Settings.RotationPrecisionDegrees;
        public float ScalarPrecision => Settings.ScalarPrecision;
        public float ShellDistance => Settings.ShellDistance;
        public bool OptimizeLoops => Settings.OptimizeLoops;
        public bool EnableDatabase => Settings.EnableDatabase;
        public bool EnableMediumTier => Settings.EnableMediumTier;
        public bool EnableLowTier => Settings.EnableLowTier;
        public bool EnablePerTrackRounding => Settings.EnablePerTrackRounding;
        public float MediumImportanceTierProportion => Settings.MediumImportanceTierProportion;
        public float LowImportanceTierProportion => Settings.LowImportanceTierProportion;
        public int MaxDatabaseChunkSize => Settings.MaxDatabaseChunkSize;
        public string CompilerOptions => Settings.CompilerOptions;
    }

    public sealed class CharacterAnimationPropertyImportPlan
    {
        internal CharacterAnimationPropertyImportPlan(
            CharacterPipelineDefinition definition,
            CharacterAnimationPresentationProfile profile,
            CharacterPresentationPoseGraphAsset poseGraph,
            Mesh mesh,
            string definitionAssetPath,
            string profileAssetPath,
            string poseGraphAssetPath,
            string rendererBindingId,
            string animationCurvePath,
            string meshGuid,
            long meshLocalFileId,
            string meshContentHash,
            IReadOnlyList<CharacterAnimationPropertyImportClipTarget> sourceClosure,
            IReadOnlyList<CharacterAnimationPropertyImportCurveTarget> curveSet,
            IReadOnlyList<CharacterAnimationPropertyImportPoseGraphTarget> poseGraphs,
            IReadOnlyList<CharacterAnimationPropertyImportPrefabTarget> prefabTargets,
            IReadOnlyList<CharacterAnimationPropertyImportDiff> diffs,
            CharacterAnimationPropertyImportCompressionTarget compression,
            IReadOnlyList<CharacterPoseParameterDeclaration> propertyParameters,
            IReadOnlyDictionary<string, CharacterPoseParameterDeclaration[]> graphParameters,
            IReadOnlyList<CharacterPoseParameterPolicy> rootPolicies,
            IReadOnlyList<CharacterAnimationSourceResourceBinding> sourceResourceBindings,
            IReadOnlyList<CharacterAnimationPropertyAuthoringBinding> propertyBindings,
            string rootResolveNodeId,
            string planHash)
        {
            Definition = definition ? definition : throw new ArgumentNullException(nameof(definition));
            Profile = profile ? profile : throw new ArgumentNullException(nameof(profile));
            PoseGraph = poseGraph ? poseGraph : throw new ArgumentNullException(nameof(poseGraph));
            Mesh = mesh ? mesh : throw new ArgumentNullException(nameof(mesh));
            DefinitionAssetPath = definitionAssetPath ?? string.Empty;
            ProfileAssetPath = profileAssetPath ?? string.Empty;
            PoseGraphAssetPath = poseGraphAssetPath ?? string.Empty;
            RendererBindingId = rendererBindingId ?? string.Empty;
            AnimationCurvePath = animationCurvePath ?? string.Empty;
            MeshGuid = meshGuid ?? string.Empty;
            MeshLocalFileId = meshLocalFileId;
            MeshContentHash = meshContentHash ?? string.Empty;
            SourceClosure = sourceClosure ?? Array.Empty<CharacterAnimationPropertyImportClipTarget>();
            CurveSet = curveSet ?? Array.Empty<CharacterAnimationPropertyImportCurveTarget>();
            PoseGraphs = poseGraphs ?? Array.Empty<CharacterAnimationPropertyImportPoseGraphTarget>();
            PrefabTargets = prefabTargets ?? Array.Empty<CharacterAnimationPropertyImportPrefabTarget>();
            Diffs = diffs ?? Array.Empty<CharacterAnimationPropertyImportDiff>();
            Compression = compression ?? throw new ArgumentNullException(nameof(compression));
            PropertyParameters = propertyParameters ?? Array.Empty<CharacterPoseParameterDeclaration>();
            GraphParameters = graphParameters ?? throw new ArgumentNullException(nameof(graphParameters));
            RootPolicies = rootPolicies ?? Array.Empty<CharacterPoseParameterPolicy>();
            SourceResourceBindings = sourceResourceBindings ?? Array.Empty<CharacterAnimationSourceResourceBinding>();
            PropertyBindings = propertyBindings ?? Array.Empty<CharacterAnimationPropertyAuthoringBinding>();
            RootResolveNodeId = rootResolveNodeId ?? string.Empty;
            PlanHash = planHash ?? string.Empty;
        }

        internal CharacterPipelineDefinition Definition { get; }
        internal CharacterAnimationPresentationProfile Profile { get; }
        internal CharacterPresentationPoseGraphAsset PoseGraph { get; }
        internal Mesh Mesh { get; }
        internal IReadOnlyList<CharacterPoseParameterDeclaration> PropertyParameters { get; }
        internal IReadOnlyDictionary<string, CharacterPoseParameterDeclaration[]> GraphParameters { get; }
        internal IReadOnlyList<CharacterPoseParameterPolicy> RootPolicies { get; }
        internal IReadOnlyList<CharacterAnimationSourceResourceBinding> SourceResourceBindings { get; }
        internal IReadOnlyList<CharacterAnimationPropertyAuthoringBinding> PropertyBindings { get; }

        public string DefinitionAssetPath { get; }
        public string ProfileAssetPath { get; }
        public string PoseGraphAssetPath { get; }
        public string RendererBindingId { get; }
        public string AnimationCurvePath { get; }
        public string MeshGuid { get; }
        public long MeshLocalFileId { get; }
        public string MeshContentHash { get; }
        public IReadOnlyList<CharacterAnimationPropertyImportClipTarget> SourceClosure { get; }
        public IReadOnlyList<CharacterAnimationPropertyImportCurveTarget> CurveSet { get; }
        public IReadOnlyList<CharacterAnimationPropertyImportPoseGraphTarget> PoseGraphs { get; }
        public IReadOnlyList<CharacterAnimationPropertyImportPrefabTarget> PrefabTargets { get; }
        public IReadOnlyList<CharacterAnimationPropertyImportDiff> Diffs { get; }
        public CharacterAnimationPropertyImportCompressionTarget Compression { get; }
        public string RootResolveNodeId { get; }
        public string PlanHash { get; }

        internal CharacterAnimationPropertyImportClipTarget[] ClipTargets() =>
            SourceClosure as CharacterAnimationPropertyImportClipTarget[] ??
            new List<CharacterAnimationPropertyImportClipTarget>(SourceClosure).ToArray();

        internal void RequirePlanHash(string expectedPlanHash)
        {
            if (string.IsNullOrWhiteSpace(expectedPlanHash) ||
                !string.Equals(expectedPlanHash.Trim(), PlanHash, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Animation resource configuration plan hash changed; analyze must be rerun.");
            }
        }
    }

    public static class CharacterAnimationPropertyImportIdentity
    {
        public static string ParameterId(string blendShapeName)
        {
            if (string.IsNullOrWhiteSpace(blendShapeName))
                throw new ArgumentException("BlendShape name is missing.", nameof(blendShapeName));
            string value = blendShapeName.Trim();
            var builder = new StringBuilder("animation.blendshape.");
            for (int index = 0; index < value.Length;)
            {
                int codePoint = char.ConvertToUtf32(value, index);
                int width = codePoint > 0xffff ? 2 : 1;
                if (IsSafe(codePoint))
                    builder.Append(char.ConvertFromUtf32(codePoint));
                else
                    builder.Append('u').Append(codePoint.ToString("x4", CultureInfo.InvariantCulture));
                index += width;
            }
            return builder.ToString();
        }

        static bool IsSafe(int codePoint)
        {
            if (codePoint <= char.MaxValue && char.IsLetterOrDigit((char)codePoint))
                return true;
            return codePoint == '.' || codePoint == '_' || codePoint == '-' || codePoint == '/';
        }
    }
}
