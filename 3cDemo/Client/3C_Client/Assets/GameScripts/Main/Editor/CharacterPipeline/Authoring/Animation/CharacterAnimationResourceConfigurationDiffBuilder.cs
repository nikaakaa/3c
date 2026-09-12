using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ThirdPersonCharacter.Pipeline.Animation;
using UnityEditor;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    internal static class CharacterAnimationResourceConfigurationDiffBuilder
    {
        internal static List<CharacterAnimationPropertyImportDiff> Build(
            CharacterAnimationPresentationProfile profile,
            CharacterPresentationPoseGraphAsset poseGraph,
            IReadOnlyList<CharacterAnimationPropertyImportClipTarget> clips,
            IReadOnlyList<CharacterAnimationPropertyImportCurveTarget> curves,
            IReadOnlyDictionary<string, CharacterPoseParameterDeclaration[]> graphParameters,
            CharacterPoseCanvasNode rootResolveNode,
            IReadOnlyList<CharacterPoseParameterPolicy> rootPolicies,
            IReadOnlyList<CharacterAnimationPropertyAuthoringBinding> propertyBindings,
            CharacterAclCompressionSettings compression,
            IReadOnlyList<CharacterAnimationPropertyImportPrefabTarget> prefabs,
            string meshContentHash)
        {
            var diffs = new List<CharacterAnimationPropertyImportDiff>();
            var desiredClips = new HashSet<UnityEngine.AnimationClip>(clips.Select(value => value.Clip));
            foreach (CharacterAnimationPropertyImportClipTarget clip in clips)
            {
                CharacterAnimationSourceResourceBinding current =
                    profile.FindSourceResourceBinding(clip.Clip);
                if (current == null)
                {
                    AddDiff(
                        diffs,
                        CharacterAnimationPropertyImportDiffKind.Add,
                        "profile.SourceResourceBindings:" + clip.AssetPath,
                        "missing",
                        "Acl",
                        "Formal Definition closure requires this source resource.");
                }
                else if (current.Backend != CharacterAnimationSamplingBackendKind.Acl)
                {
                    AddDiff(
                        diffs,
                        CharacterAnimationPropertyImportDiffKind.Update,
                        "profile.SourceResourceBindings:" + clip.AssetPath,
                        current.Backend.ToString(),
                        "Acl",
                        "Formal Definition closure requires ACL sampling.");
                }
                if (clip.LinearizationCount > 0)
                {
                    AddDiff(
                        diffs,
                        CharacterAnimationPropertyImportDiffKind.Update,
                        clip.AssetPath + ":blendShape-curves",
                        clip.BlendShapeCurveHash,
                        "linear-left-right",
                        $"linearization_count={clip.LinearizationCount}");
                }
                foreach (CharacterAnimationPropertyImportCurveTarget curve in curves.Where(value => value.RequiresRename))
                {
                    AddDiff(
                        diffs,
                        CharacterAnimationPropertyImportDiffKind.Rename,
                        clip.AssetPath + ":" + curve.SourcePropertyName,
                        curve.SourcePropertyName,
                        curve.TargetPropertyName,
                        "Move the complete AnimationCurve and remove the old binding.");
                }
            }
            foreach (CharacterAnimationSourceResourceBinding current in profile.SourceResourceBindings)
            {
                if (current == null || !current.AuthoringClip || !desiredClips.Contains(current.AuthoringClip))
                {
                    AddDiff(
                        diffs,
                        CharacterAnimationPropertyImportDiffKind.Remove,
                        "profile.SourceResourceBindings",
                        current?.AuthoringClip
                            ? AssetDatabase.GetAssetPath(current.AuthoringClip)
                            : "missing",
                        "formal Definition closure",
                        "Remove source resources outside the current Definition closure.");
                }
            }

            var propertyByParameter = profile.AnimationPropertyBindings
                .Where(value => value != null && value.ParameterId.IsValid)
                .GroupBy(value => value.ParameterId)
                .ToDictionary(value => value.Key, value => value.First());
            var desiredPropertyIds = new HashSet<PoseParameterId>();
            foreach (CharacterAnimationPropertyAuthoringBinding desired in propertyBindings)
            {
                desiredPropertyIds.Add(desired.ParameterId);
                if (!propertyByParameter.TryGetValue(
                        desired.ParameterId,
                        out CharacterAnimationPropertyAuthoringBinding current))
                {
                    AddDiff(
                        diffs,
                        CharacterAnimationPropertyImportDiffKind.Add,
                        "profile.AnimationPropertyBindings:" + desired.ParameterId,
                        "missing",
                        desired.BlendShapeName,
                        "Add the formal Mesh-backed AnimatedProperty binding.");
                    continue;
                }
                if (current.RendererBindingId != desired.RendererBindingId ||
                    current.AnimationCurvePath != desired.AnimationCurvePath ||
                    current.ExpectedMesh != desired.ExpectedMesh ||
                    !string.Equals(current.MeshContentHash, meshContentHash, StringComparison.Ordinal) ||
                    current.BlendShapeName != desired.BlendShapeName ||
                    current.BlendShapeIndex != desired.BlendShapeIndex)
                {
                    AddDiff(
                        diffs,
                        CharacterAnimationPropertyImportDiffKind.Update,
                        "profile.AnimationPropertyBindings:" + desired.ParameterId,
                        current.BlendShapeName,
                        desired.BlendShapeName,
                        "Refresh Renderer, Mesh, hash, curve path and BlendShape index as one binding.");
                }
            }
            foreach (CharacterAnimationPropertyAuthoringBinding current in profile.AnimationPropertyBindings)
            {
                if (current == null ||
                    !current.ParameterId.IsValid ||
                    !desiredPropertyIds.Contains(current.ParameterId))
                {
                    AddDiff(
                        diffs,
                        CharacterAnimationPropertyImportDiffKind.Remove,
                        "profile.AnimationPropertyBindings",
                        current?.ParameterId.Value ?? "missing",
                        "formal BlendShape contract",
                        "Remove property bindings outside the formal curve set.");
                }
            }

            if (!SameCompression(profile.AnimationCompression, compression))
            {
                AddCompressionDiff(diffs, profile.AnimationCompression, compression, "SampleRate", value => value.SampleRate.ToString(CultureInfo.InvariantCulture));
                AddCompressionDiff(diffs, profile.AnimationCompression, compression, "TransformPrecision", value => value.TransformPrecision.ToString("R", CultureInfo.InvariantCulture));
                AddCompressionDiff(diffs, profile.AnimationCompression, compression, "ScalePrecision", value => value.ScalePrecision.ToString("R", CultureInfo.InvariantCulture));
                AddCompressionDiff(diffs, profile.AnimationCompression, compression, "RotationPrecisionDegrees", value => value.RotationPrecisionDegrees.ToString("R", CultureInfo.InvariantCulture));
                AddCompressionDiff(diffs, profile.AnimationCompression, compression, "ScalarPrecision", value => value.ScalarPrecision.ToString("R", CultureInfo.InvariantCulture));
                AddCompressionDiff(diffs, profile.AnimationCompression, compression, "ShellDistance", value => value.ShellDistance.ToString("R", CultureInfo.InvariantCulture));
                AddCompressionDiff(diffs, profile.AnimationCompression, compression, "OptimizeLoops", value => value.OptimizeLoops.ToString());
                AddCompressionDiff(diffs, profile.AnimationCompression, compression, "EnableDatabase", value => value.EnableDatabase.ToString());
                AddCompressionDiff(diffs, profile.AnimationCompression, compression, "EnableMediumTier", value => value.EnableMediumTier.ToString());
                AddCompressionDiff(diffs, profile.AnimationCompression, compression, "EnableLowTier", value => value.EnableLowTier.ToString());
                AddCompressionDiff(diffs, profile.AnimationCompression, compression, "EnablePerTrackRounding", value => value.EnablePerTrackRounding.ToString());
                AddCompressionDiff(diffs, profile.AnimationCompression, compression, "MediumImportanceTierProportion", value => value.MediumImportanceTierProportion.ToString("R", CultureInfo.InvariantCulture));
                AddCompressionDiff(diffs, profile.AnimationCompression, compression, "LowImportanceTierProportion", value => value.LowImportanceTierProportion.ToString("R", CultureInfo.InvariantCulture));
                AddCompressionDiff(diffs, profile.AnimationCompression, compression, "MaxDatabaseChunkSize", value => value.MaxDatabaseChunkSize.ToString(CultureInfo.InvariantCulture));
                AddCompressionDiff(diffs, profile.AnimationCompression, compression, "CompilerOptions", value => value.CompilerOptions);
            }

            foreach (KeyValuePair<string, CharacterPoseParameterDeclaration[]> graph in graphParameters)
            {
                CharacterPoseCanvasGraph current = poseGraph.RequireGraph(new PoseGraphId(graph.Key));
                foreach (CharacterPoseParameterDeclaration previous in current.Parameters)
                {
                    if (previous != null && !CharacterPoseParameterAccess.IsBlackboardInput(previous))
                    {
                        AddDiff(
                            diffs,
                            CharacterAnimationPropertyImportDiffKind.Remove,
                            $"poseGraph:{graph.Key}.parameters:{previous.ParameterId}",
                            DescribeParameter(previous),
                            "formal Pose input contract",
                            "Remove the graph declaration; curve and internal playback data remain in their owning contract.");
                    }
                }
            }

            if (rootResolveNode != null)
            {
                CharacterPoseParameterResolvePayload currentPayload =
                    rootResolveNode.RequirePayload<CharacterPoseParameterResolvePayload>();
                foreach (CharacterPoseParameterPolicy desired in rootPolicies.Where(value => desiredPropertyIds.Contains(value.ParameterId)))
                {
                    CharacterPoseParameterPolicy previous = currentPayload.Policies
                        .FirstOrDefault(value => value != null && value.ParameterId.Equals(desired.ParameterId));
                    if (previous == null)
                    {
                        AddDiff(
                            diffs,
                            CharacterAnimationPropertyImportDiffKind.Add,
                            $"poseGraph:{poseGraph.Graph.GraphId}.node:{rootResolveNode.NodeId}.parameter-policies:{desired.ParameterId}",
                            "missing",
                            PoseParameterResolvePolicy.Weighted.ToString(),
                            "Declare the AnimatedProperty resolve policy explicitly.");
                    }
                    else if (previous.Policy != PoseParameterResolvePolicy.Weighted)
                    {
                        AddDiff(
                            diffs,
                            CharacterAnimationPropertyImportDiffKind.Update,
                            $"poseGraph:{poseGraph.Graph.GraphId}.node:{rootResolveNode.NodeId}.parameter-policies:{desired.ParameterId}",
                            previous.Policy.ToString(),
                            PoseParameterResolvePolicy.Weighted.ToString(),
                            "AnimatedProperty parameters require explicit Weighted resolution.");
                    }
                }
                foreach (CharacterPoseParameterPolicy previous in currentPayload.Policies)
                {
                    if (previous != null &&
                        previous.ParameterId.Value.StartsWith("animation.blendshape.", StringComparison.Ordinal) &&
                        !desiredPropertyIds.Contains(previous.ParameterId))
                    {
                        AddDiff(
                            diffs,
                            CharacterAnimationPropertyImportDiffKind.Remove,
                            $"poseGraph:{poseGraph.Graph.GraphId}.node:{rootResolveNode.NodeId}.parameter-policies:{previous.ParameterId}",
                            previous.Policy.ToString(),
                            "formal BlendShape policy",
                            "Remove an AnimatedProperty policy outside the formal curve set.");
                    }
                }
            }

            foreach (CharacterAnimationPropertyImportPrefabTarget prefab in prefabs)
            {
                bool matches = !string.IsNullOrWhiteSpace(prefab.CurrentBindingId) &&
                               string.Equals(prefab.CurrentRendererPath, prefab.RendererPath, StringComparison.Ordinal) &&
                               string.Equals(prefab.CurrentMeshGuid, prefab.MeshGuid, StringComparison.Ordinal) &&
                               prefab.CurrentMeshLocalFileId == prefab.MeshLocalFileId &&
                               string.Equals(prefab.CurrentMeshContentHash, prefab.MeshContentHash, StringComparison.Ordinal);
                if (string.IsNullOrWhiteSpace(prefab.CurrentBindingId))
                {
                    AddDiff(
                        diffs,
                        CharacterAnimationPropertyImportDiffKind.Add,
                        "prefab:" + prefab.AssetPath + ":renderer-binding",
                        "missing",
                        prefab.BindingId + ":" + prefab.MeshGuid + ":" + prefab.MeshLocalFileId,
                        "Add the formal Renderer binding and Mesh content hash.");
                }
                else if (!matches)
                {
                    AddDiff(
                        diffs,
                        CharacterAnimationPropertyImportDiffKind.Update,
                        "prefab:" + prefab.AssetPath + ":renderer-binding",
                        prefab.CurrentRendererPath + ":" + prefab.CurrentMeshContentHash,
                        prefab.RendererPath + ":" + prefab.MeshContentHash,
                        "Refresh the exact Renderer, Mesh identity and content hash.");
                }
            }
            return diffs;
        }

        static void AddCompressionDiff(
            ICollection<CharacterAnimationPropertyImportDiff> diffs,
            CharacterAclCompressionSettings current,
            CharacterAclCompressionSettings desired,
            string field,
            Func<CharacterAclCompressionSettings, string> format)
        {
            string before = current == null ? "missing" : format(current);
            string after = format(desired);
            if (!string.Equals(before, after, StringComparison.Ordinal))
            {
                AddDiff(
                    diffs,
                    CharacterAnimationPropertyImportDiffKind.Update,
                    "profile.AnimationCompression." + field,
                    before,
                    after,
                    "Apply the formal ACL compression contract.");
            }
        }

        static void AddDiff(
            ICollection<CharacterAnimationPropertyImportDiff> diffs,
            CharacterAnimationPropertyImportDiffKind kind,
            string target,
            string before,
            string after,
            string detail) =>
            diffs.Add(new CharacterAnimationPropertyImportDiff(kind, target, before, after, detail));

        static bool SameCompression(
            CharacterAclCompressionSettings left,
            CharacterAclCompressionSettings right)
        {
            return left != null &&
                   right != null &&
                   left.Revision == right.Revision &&
                   left.SampleRate == right.SampleRate &&
                   left.TransformPrecision.Equals(right.TransformPrecision) &&
                   left.ScalePrecision.Equals(right.ScalePrecision) &&
                   left.RotationPrecisionDegrees.Equals(right.RotationPrecisionDegrees) &&
                   left.ScalarPrecision.Equals(right.ScalarPrecision) &&
                   left.ShellDistance.Equals(right.ShellDistance) &&
                   left.OptimizeLoops == right.OptimizeLoops &&
                   left.EnableDatabase == right.EnableDatabase &&
                   left.EnableMediumTier == right.EnableMediumTier &&
                   left.EnableLowTier == right.EnableLowTier &&
                   left.EnablePerTrackRounding == right.EnablePerTrackRounding &&
                   left.MediumImportanceTierProportion.Equals(right.MediumImportanceTierProportion) &&
                   left.LowImportanceTierProportion.Equals(right.LowImportanceTierProportion) &&
                   left.MaxDatabaseChunkSize == right.MaxDatabaseChunkSize &&
                   left.CompilerOptions == right.CompilerOptions;
        }

        static string DescribeParameter(CharacterPoseParameterDeclaration value) =>
            $"{value.ParameterId}:{value.ValueType}:{value.Usage}:{value.Unit}:{value.DefaultValue.ToString("R", CultureInfo.InvariantCulture)}";
    }
}
