using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Animation;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    public static class CharacterAnimationResourceConfigurationAnalyzer
    {
        public static CharacterAnimationPropertyImportPlan Analyze(
            CharacterPipelineDefinition definition,
            string rendererBindingId,
            string animationCurvePath)
        {
            if (!definition)
                throw new ArgumentNullException(nameof(definition));
            rendererBindingId = RequireIdentity(rendererBindingId, nameof(rendererBindingId));
            animationCurvePath = RequireIdentity(animationCurvePath, nameof(animationCurvePath));
            string definitionPath = RequireAssetPath(
                definition,
                ".asset",
                "Character Pipeline Definition");
            CharacterAnimationPresentationProfile profile = definition.AnimationPresentationProfile ??
                throw new InvalidOperationException(
                    $"Character Definition '{definition.name}' has no Animation Presentation Profile.");
            CharacterPresentationPoseGraphAsset poseGraph = profile.PoseGraph ??
                throw new InvalidOperationException(
                    $"Animation Presentation Profile '{profile.name}' has no Pose Graph.");
            CharacterAnimationRigDefinition rig = profile.RigDefinition ??
                throw new InvalidOperationException(
                    $"Animation Presentation Profile '{profile.name}' has no Rig Definition.");
            rig.RequireValid();
            string profilePath = RequireAssetPath(
                profile,
                ".asset",
                "Animation Presentation Profile");
            string poseGraphPath = RequireAssetPath(
                poseGraph,
                ".asset",
                "Presentation Pose Graph");
            if (string.IsNullOrWhiteSpace(AssetDatabase.AssetPathToGUID(profilePath)))
                throw new InvalidOperationException(
                    "Animation Presentation Profile has no stable asset GUID.");

            IReadOnlyList<CharacterAnimationResourceClosureEntry> closure =
                CharacterAnimationResourceClosureAnalyzer.Analyze(definition, profile);
            CharacterAnimationRigPrefabAnalysis rigAnalysis =
                CharacterAnimationRigPrefabAnalyzer.Analyze(
                    rig,
                    rendererBindingId,
                    animationCurvePath);
            CharacterAnimationPropertyCurveAnalysis curveAnalysis =
                CharacterAnimationPropertyCurveAnalyzer.Analyze(
                    closure,
                    animationCurvePath,
                    rigAnalysis.Mesh);
            CharacterAnimationPropertyContractAnalysis contract =
                CharacterAnimationPropertyContractFactory.Create(
                    profile,
                    poseGraph,
                    curveAnalysis.Clips,
                    curveAnalysis.Curves,
                    rigAnalysis.Mesh,
                    rigAnalysis.MeshIdentity.ContentHash,
                    rendererBindingId,
                    animationCurvePath);
            List<CharacterAnimationPropertyImportDiff> diffs =
                CharacterAnimationResourceConfigurationDiffBuilder.Build(
                    profile,
                    poseGraph,
                    curveAnalysis.Clips,
                    curveAnalysis.Curves,
                    contract.GraphParameters,
                    contract.RootResolveNode,
                    contract.RootPolicies,
                    contract.PropertyBindings,
                    contract.Compression.Settings,
                    rigAnalysis.Prefabs,
                    rigAnalysis.MeshIdentity.ContentHash);
            string planHash = CharacterAnimationResourceConfigurationPlanHasher.Compute(
                definitionPath,
                profilePath,
                poseGraphPath,
                rendererBindingId,
                animationCurvePath,
                rigAnalysis.MeshIdentity,
                curveAnalysis.Clips,
                curveAnalysis.Curves,
                contract.PoseGraphs,
                contract.GraphParameters,
                contract.RootResolveNode,
                contract.RootPolicies,
                contract.Compression.Settings,
                rigAnalysis.Prefabs,
                diffs);
            return new CharacterAnimationPropertyImportPlan(
                definition,
                profile,
                poseGraph,
                rigAnalysis.Mesh,
                definitionPath,
                profilePath,
                poseGraphPath,
                rendererBindingId,
                animationCurvePath,
                rigAnalysis.MeshIdentity.AssetGuid,
                rigAnalysis.MeshIdentity.LocalFileId,
                rigAnalysis.MeshIdentity.ContentHash,
                curveAnalysis.Clips,
                curveAnalysis.Curves,
                contract.PoseGraphs,
                rigAnalysis.Prefabs,
                diffs,
                contract.Compression,
                contract.PropertyParameters,
                contract.GraphParameters,
                contract.RootPolicies,
                contract.SourceResourceBindings,
                contract.PropertyBindings,
                contract.RootResolveNode?.NodeId.Value ?? string.Empty,
                planHash);
        }

        static string RequireAssetPath(
            UnityEngine.Object asset,
            string extension,
            string typeName)
        {
            if (!asset)
                throw new ArgumentNullException(typeName);
            string path = AssetDatabase.GetAssetPath(asset)?.Replace('\\', '/') ?? string.Empty;
            if (!path.StartsWith("Assets/", StringComparison.Ordinal) ||
                !path.EndsWith(extension, StringComparison.OrdinalIgnoreCase) ||
                path.Contains(".."))
            {
                throw new InvalidOperationException(
                    $"{typeName} path '{path}' must be a persisted Assets/...{extension} asset.");
            }
            return path;
        }

        static string RequireIdentity(string value, string parameterName) =>
            string.IsNullOrWhiteSpace(value)
                ? throw new ArgumentException(
                    "Animation resource configuration identity is missing.",
                    parameterName)
                : value.Trim().Replace('\\', '/');
    }
}
