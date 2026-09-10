using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ThirdPersonCharacter.Pipeline.Animation;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    internal static class CharacterAnimationResourceConfigurationPlanHasher
    {
        internal static string Compute(
            string definitionPath,
            string profilePath,
            string poseGraphPath,
            string rendererBindingId,
            string animationCurvePath,
            CharacterAnimationMeshContentIdentity mesh,
            IReadOnlyList<CharacterAnimationPropertyImportClipTarget> clips,
            IReadOnlyList<CharacterAnimationPropertyImportCurveTarget> curves,
            IReadOnlyList<CharacterAnimationPropertyImportPoseGraphTarget> poseGraphs,
            IReadOnlyDictionary<string, CharacterPoseParameterDeclaration[]> graphParameters,
            CharacterPoseCanvasNode rootResolveNode,
            IReadOnlyList<CharacterPoseParameterPolicy> rootPolicies,
            CharacterAclCompressionSettings compression,
            IReadOnlyList<CharacterAnimationPropertyImportPrefabTarget> prefabs,
            IReadOnlyList<CharacterAnimationPropertyImportDiff> diffs)
        {
            var values = new List<string>
            {
                "character-animation-property-import/v2",
                definitionPath,
                profilePath,
                poseGraphPath,
                rendererBindingId,
                animationCurvePath,
                mesh.AssetGuid,
                mesh.LocalFileId.ToString(CultureInfo.InvariantCulture),
                mesh.DependencyHash,
                mesh.ContentHash,
                rootResolveNode?.NodeId.Value ?? string.Empty
            };
            foreach (CharacterAnimationPropertyImportClipTarget clip in clips)
            {
                values.Add(clip.AssetGuid);
                values.Add(clip.LocalFileId.ToString(CultureInfo.InvariantCulture));
                values.Add(clip.AssetPath);
                values.Add(clip.SourceCategory);
                values.Add(clip.CurrentBackendName);
                values.Add(clip.TargetBackend);
                values.Add(clip.DependencyHash);
                values.Add(clip.BlendShapeCurveHash);
                values.Add(clip.LinearizationCount.ToString(CultureInfo.InvariantCulture));
                values.Add(clip.RootEvidenceCount.ToString(CultureInfo.InvariantCulture));
                foreach (string binding in clip.RootEvidenceBindings)
                    values.Add(binding);
                values.Add(clip.RootEvidenceHash);
            }
            foreach (CharacterAnimationPropertyImportCurveTarget curve in curves)
            {
                values.Add(curve.SourceBlendShapeName);
                values.Add(curve.TargetBlendShapeName);
                values.Add(curve.BlendShapeIndex.ToString(CultureInfo.InvariantCulture));
                values.Add(curve.ParameterId);
                values.Add(curve.RequiresRename.ToString());
            }
            foreach (CharacterAnimationPropertyImportPoseGraphTarget graph in poseGraphs)
            {
                values.Add(graph.GraphId);
                values.Add(graph.CurrentParameterCount.ToString(CultureInfo.InvariantCulture));
                values.Add(graph.TargetParameterCount.ToString(CultureInfo.InvariantCulture));
                values.Add(graph.CurrentPolicyCount.ToString(CultureInfo.InvariantCulture));
                values.Add(graph.TargetPolicyCount.ToString(CultureInfo.InvariantCulture));
                foreach (CharacterPoseParameterDeclaration parameter in graphParameters[graph.GraphId])
                    values.Add(DescribeParameter(parameter));
            }
            foreach (CharacterPoseParameterPolicy policy in rootPolicies)
            {
                values.Add(policy.ParameterId.Value);
                values.Add(policy.Policy.ToString());
            }
            AddCompressionValues(values, compression);
            foreach (CharacterAnimationPropertyImportPrefabTarget prefab in prefabs)
            {
                values.Add(prefab.AssetPath);
                values.Add(prefab.RigId);
                values.Add(prefab.RigRevision);
                values.Add(prefab.RendererPath);
                values.Add(prefab.BindingId);
                values.Add(prefab.CurrentBindingId);
                values.Add(prefab.CurrentRendererPath);
                values.Add(prefab.CurrentMeshGuid);
                values.Add(prefab.CurrentMeshLocalFileId.ToString(CultureInfo.InvariantCulture));
                values.Add(prefab.CurrentMeshContentHash);
                values.Add(prefab.MeshGuid);
                values.Add(prefab.MeshLocalFileId.ToString(CultureInfo.InvariantCulture));
                values.Add(prefab.MeshContentHash);
            }
            foreach (CharacterAnimationPropertyImportDiff diff in diffs)
            {
                values.Add(diff.Kind.ToString());
                values.Add(diff.Target);
                values.Add(diff.Before);
                values.Add(diff.After);
                values.Add(diff.Detail);
            }
            return CharacterAclHash.ComputeStrings(values);
        }

        static void AddCompressionValues(
            ICollection<string> values,
            CharacterAclCompressionSettings settings)
        {
            values.Add(settings.Revision);
            values.Add(settings.SampleRate.ToString(CultureInfo.InvariantCulture));
            values.Add(settings.TransformPrecision.ToString("R", CultureInfo.InvariantCulture));
            values.Add(settings.ScalePrecision.ToString("R", CultureInfo.InvariantCulture));
            values.Add(settings.RotationPrecisionDegrees.ToString("R", CultureInfo.InvariantCulture));
            values.Add(settings.ScalarPrecision.ToString("R", CultureInfo.InvariantCulture));
            values.Add(settings.ShellDistance.ToString("R", CultureInfo.InvariantCulture));
            values.Add(settings.OptimizeLoops.ToString());
            values.Add(settings.EnableDatabase.ToString());
            values.Add(settings.EnableMediumTier.ToString());
            values.Add(settings.EnableLowTier.ToString());
            values.Add(settings.EnablePerTrackRounding.ToString());
            values.Add(settings.MediumImportanceTierProportion.ToString("R", CultureInfo.InvariantCulture));
            values.Add(settings.LowImportanceTierProportion.ToString("R", CultureInfo.InvariantCulture));
            values.Add(settings.MaxDatabaseChunkSize.ToString(CultureInfo.InvariantCulture));
            values.Add(settings.CompilerOptions);
        }

        static string DescribeParameter(CharacterPoseParameterDeclaration value) =>
            $"{value.ParameterId}:{value.ValueType}:{value.Usage}:{value.Unit}:{value.DefaultValue.ToString("R", CultureInfo.InvariantCulture)}";
    }
}
