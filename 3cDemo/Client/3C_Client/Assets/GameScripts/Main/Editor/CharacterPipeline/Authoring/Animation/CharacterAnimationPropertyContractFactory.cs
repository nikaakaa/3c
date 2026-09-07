using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.Pipeline.Animation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    internal sealed class CharacterAnimationPropertyContractAnalysis
    {
        internal CharacterAnimationPropertyContractAnalysis(
            CharacterPoseParameterDeclaration[] propertyParameters,
            Dictionary<string, CharacterPoseParameterDeclaration[]> graphParameters,
            CharacterPoseParameterPolicy[] rootPolicies,
            CharacterAnimationPropertyImportPoseGraphTarget[] poseGraphs,
            CharacterAnimationSourceResourceBinding[] sourceResourceBindings,
            CharacterAnimationPropertyAuthoringBinding[] propertyBindings,
            CharacterAnimationPropertyImportCompressionTarget compression,
            CharacterPoseCanvasNode rootResolveNode)
        {
            PropertyParameters = propertyParameters;
            GraphParameters = graphParameters;
            RootPolicies = rootPolicies;
            PoseGraphs = poseGraphs;
            SourceResourceBindings = sourceResourceBindings;
            PropertyBindings = propertyBindings;
            Compression = compression;
            RootResolveNode = rootResolveNode;
        }

        internal CharacterPoseParameterDeclaration[] PropertyParameters { get; }
        internal Dictionary<string, CharacterPoseParameterDeclaration[]> GraphParameters { get; }
        internal CharacterPoseParameterPolicy[] RootPolicies { get; }
        internal CharacterAnimationPropertyImportPoseGraphTarget[] PoseGraphs { get; }
        internal CharacterAnimationSourceResourceBinding[] SourceResourceBindings { get; }
        internal CharacterAnimationPropertyAuthoringBinding[] PropertyBindings { get; }
        internal CharacterAnimationPropertyImportCompressionTarget Compression { get; }
        internal CharacterPoseCanvasNode RootResolveNode { get; }
    }

    internal static class CharacterAnimationPropertyContractFactory
    {
        internal static CharacterAnimationPropertyContractAnalysis Create(
            CharacterAnimationPresentationProfile profile,
            CharacterPresentationPoseGraphAsset poseGraph,
            IReadOnlyList<CharacterAnimationPropertyImportClipTarget> clips,
            IReadOnlyList<CharacterAnimationPropertyImportCurveTarget> curves,
            Mesh mesh,
            string meshContentHash,
            string rendererBindingId,
            string animationCurvePath)
        {
            CharacterPoseParameterDeclaration[] propertyParameters =
                curves.Select(value => new CharacterPoseParameterDeclaration(
                        new PoseParameterId(value.ParameterId),
                        PoseParameterValueType.Float,
                        0f,
                        "percent",
                        CharacterPoseParameterUsage.AnimatedProperty))
                    .ToArray();
            CharacterPoseCanvasGraph rootGraph = poseGraph.Graph ??
                throw new InvalidOperationException("Presentation Pose Graph has no root Graph.");
            CharacterPoseCanvasNode[] resolveNodes = rootGraph.Nodes
                .Where(value => value?.Payload is CharacterPoseParameterResolvePayload)
                .ToArray();
            if (resolveNodes.Length != 1)
                throw new InvalidOperationException(
                    $"Root Pose Graph must contain exactly one Pose Parameter Resolve node; found {resolveNodes.Length}.");
            CharacterPoseCanvasNode rootResolveNode = resolveNodes[0];
            CharacterPoseParameterPolicy[] rootPolicies = BuildRootPolicies(
                rootResolveNode.RequirePayload<CharacterPoseParameterResolvePayload>(),
                propertyParameters);
            var graphParameters = new Dictionary<string, CharacterPoseParameterDeclaration[]>(StringComparer.Ordinal);
            var poseGraphs = new List<CharacterAnimationPropertyImportPoseGraphTarget>();
            foreach (CharacterPoseCanvasGraph graph in poseGraph.EnumerateGraphs()
                         .Where(value => value != null)
                         .OrderBy(value => value.GraphId.Value, StringComparer.Ordinal))
            {
                CharacterPoseParameterDeclaration[] desired = BuildGraphParameters(
                    graph,
                    propertyParameters);
                graphParameters.Add(graph.GraphId.Value, desired);
                poseGraphs.Add(new CharacterAnimationPropertyImportPoseGraphTarget(
                    graph.GraphId.Value,
                    graph.Parameters.Count,
                    desired.Length,
                    graph == rootGraph
                        ? rootResolveNode.RequirePayload<CharacterPoseParameterResolvePayload>().Policies.Count
                        : 0,
                    graph == rootGraph ? rootPolicies.Length : 0)
                {
                    IsRoot = graph == rootGraph
                });
            }
            CharacterAnimationSourceResourceBinding[] sourceResourceBindings =
                clips.Select(value =>
                {
                    var binding = new CharacterAnimationSourceResourceBinding();
                    binding.ConfigureAcl(value.Clip);
                    return binding;
                }).ToArray();
            CharacterAnimationPropertyAuthoringBinding[] propertyBindings =
                curves.Select(value =>
                {
                    var binding = new CharacterAnimationPropertyAuthoringBinding();
                    binding.Configure(
                        new PoseParameterId(value.ParameterId),
                        rendererBindingId,
                        animationCurvePath,
                        mesh,
                        meshContentHash,
                        value.TargetBlendShapeName,
                        value.BlendShapeIndex);
                    return binding;
                }).ToArray();
            return new CharacterAnimationPropertyContractAnalysis(
                propertyParameters,
                graphParameters,
                rootPolicies,
                poseGraphs.ToArray(),
                sourceResourceBindings,
                propertyBindings,
                new CharacterAnimationPropertyImportCompressionTarget(
                    CreateCompressionSettings()),
                rootResolveNode);
        }

        static CharacterPoseParameterDeclaration[] BuildGraphParameters(
            CharacterPoseCanvasGraph graph,
            IReadOnlyList<CharacterPoseParameterDeclaration> propertyParameters)
        {
            var result = new List<CharacterPoseParameterDeclaration>();
            var ids = new HashSet<PoseParameterId>();
            foreach (CharacterPoseParameterDeclaration current in graph.Parameters)
            {
                if (current == null)
                    throw new InvalidOperationException($"Pose Graph '{graph.GraphId}' contains a missing parameter.");
                if (current.Usage == CharacterPoseParameterUsage.AnimatedProperty)
                    continue;
                if (!ids.Add(current.ParameterId))
                    throw new InvalidOperationException($"Pose Graph '{graph.GraphId}' has duplicate parameter '{current.ParameterId}'.");
                result.Add(current);
            }
            foreach (CharacterPoseParameterDeclaration property in propertyParameters)
            {
                if (!ids.Add(property.ParameterId))
                    throw new InvalidOperationException(
                        $"Pose Graph '{graph.GraphId}' already uses property parameter '{property.ParameterId}' as another declaration.");
                result.Add(new CharacterPoseParameterDeclaration(
                    property.ParameterId,
                    property.ValueType,
                    property.DefaultValue,
                    property.Unit,
                    property.Usage));
            }
            return result.ToArray();
        }

        static CharacterPoseParameterPolicy[] BuildRootPolicies(
            CharacterPoseParameterResolvePayload payload,
            IReadOnlyList<CharacterPoseParameterDeclaration> propertyParameters)
        {
            var propertyIds = new HashSet<PoseParameterId>(
                propertyParameters.Select(value => value.ParameterId));
            var result = new List<CharacterPoseParameterPolicy>();
            var existingIds = new HashSet<PoseParameterId>();
            foreach (CharacterPoseParameterPolicy current in payload.Policies)
            {
                if (current == null || !current.ParameterId.IsValid || !existingIds.Add(current.ParameterId))
                    throw new InvalidOperationException("Root Pose Parameter Resolve policies are missing or duplicated.");
                if (!propertyIds.Contains(current.ParameterId))
                    result.Add(new CharacterPoseParameterPolicy(current.ParameterId, current.Policy));
            }
            foreach (CharacterPoseParameterDeclaration property in propertyParameters)
            {
                result.Add(new CharacterPoseParameterPolicy(
                    property.ParameterId,
                    PoseParameterResolvePolicy.Weighted));
            }
            return result.ToArray();
        }

        static CharacterAclCompressionSettings CreateCompressionSettings() =>
            new CharacterAclCompressionSettings(
                960,
                0.01f,
                0.01f,
                0.5f,
                0.001f,
                3f,
                false,
                true,
                false,
                true,
                false,
                0f,
                0.5f,
                1024 * 1024,
                "msvc;fp:precise;simd:on");
    }
}
