using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;
using ThirdPersonCharacter.Pipeline.Simulation.Editor;

namespace ThirdPersonCharacter.Editor.MotionMatching
{
    public interface IMotionMatchingProjectionParameterCurveResolver
    {
        MotionMatchingPoseParameterCurvePayload ResolveRequired(
            UnityEngine.AnimationClip clip,
            PoseParameterId parameterId);
    }

    public static class MotionMatchingProjectionPayloadCompiler
    {
        public static MotionMatchingProjectionPayload Compile(
            CharacterMotionMatchingProfile profile,
            CharacterPresentationPoseGraphAsset poseGraphAsset,
            IReadOnlyList<CharacterMotionMatchingPoseSourceBinding> sourceBindings,
            IReadOnlyDictionary<CharacterPresentationPoseSourceSlot, PresentationPoseSourceIndex> sourceIndices,
            CharacterFootPlacementAnalysisSource analysisSource,
            IMotionMatchingProjectionParameterCurveResolver parameterCurveResolver)
        {
            if (!profile)
                throw new ArgumentNullException(nameof(profile));
            if (!analysisSource)
                throw new ArgumentNullException(nameof(analysisSource));
            if (!poseGraphAsset || poseGraphAsset.Graph == null || sourceBindings == null || sourceIndices == null)
                throw new ArgumentNullException(nameof(poseGraphAsset));
            CharacterTypedPoseGraph poseGraph = poseGraphAsset.Graph;
            if (parameterCurveResolver == null)
                throw new ArgumentNullException(nameof(parameterCurveResolver));
            CharacterMotionMatchingAuthoringValidator.RequireProfile(profile);
            var databases = new List<MotionMatchingDatabasePayload>();
            var bindings =
                new MotionMatchingProviderBindingPayload[
                    sourceBindings.Count];
            for (int bindingIndex = 0;
                 bindingIndex < sourceBindings.Count;
                 bindingIndex++)
            {
                CharacterMotionMatchingPoseSourceBinding binding = sourceBindings[bindingIndex];
                binding.RequireValid(binding.Rig);
                if (binding.Profile != profile || !sourceIndices.TryGetValue(binding.Slot, out PresentationPoseSourceIndex sourceIndex))
                    throw new InvalidOperationException($"Motion Matching Pose source binding '{binding.name}' is outside the compiled source catalog.");
                int firstDatabase = databases.Count;
                for (int databaseIndex = 0; databaseIndex < binding.Databases.Count; databaseIndex++)
                {
                    databases.Add(CompileDatabase(
                        profile,
                        binding.Databases[databaseIndex],
                        analysisSource,
                        parameterCurveResolver));
                }
                if (!(binding.Slot is CharacterMotionMatchingPoseSourceSlot slot))
                    throw new InvalidOperationException($"Motion Matching Pose source binding '{binding.name}' has the wrong Source Slot kind.");
                PoseNodeId playerNodeId = ResolveProviderNodeId(poseGraphAsset, poseGraph, slot);
                bindings[bindingIndex] = new MotionMatchingProviderBindingPayload(
                    $"pose-provider/{playerNodeId}",
                    sourceIndex,
                    playerNodeId,
                    binding.SearchDomainId,
                    firstDatabase,
                    binding.Databases.Count);
            }
            MotionMatchingFeatureSchemaPayload featureSchema = MotionMatchingAuthoringPayloadCompiler.CompileFeatureSchema(
                profile.FeatureSchema, profile.TrajectoryPolicy);
            return new MotionMatchingProjectionPayload(
                profile.ProfileId,
                profile.Revision,
                featureSchema,
                MotionMatchingAuthoringPayloadCompiler.CompileTrajectoryPolicy(profile.TrajectoryPolicy),
                MotionMatchingAuthoringPayloadCompiler.CompileCostProfile(profile.CostProfile, featureSchema),
                MotionMatchingAuthoringPayloadCompiler.CompileSearchPolicy(profile.SearchPolicy),
                databases.ToArray(),
                bindings);
        }

        static PoseNodeId ResolveProviderNodeId(
            CharacterPresentationPoseGraphAsset owner,
            CharacterTypedPoseGraph graph,
            CharacterMotionMatchingPoseSourceSlot slot)
        {
            PoseNodeId result = default;
            ResolveProviderNodeId(
                owner,
                graph,
                slot,
                string.Empty,
                ref result);
            return result.IsValid
                ? result
                : throw new InvalidOperationException(
                    $"Motion Matching Source Slot '{slot.name}' has no matching player node in Pose Graph '{graph.GraphId}'.");
        }

        static void ResolveProviderNodeId(
            CharacterPresentationPoseGraphAsset owner,
            CharacterTypedPoseGraph graph,
            CharacterMotionMatchingPoseSourceSlot slot,
            string scope,
            ref PoseNodeId result)
        {
            for (int i = 0; i < graph.Nodes.Count; i++)
            {
                CharacterTypedPoseNode node = graph.Nodes[i];
                if (node == null)
                    continue;
                PoseNodeId scopedNodeId = string.IsNullOrEmpty(scope)
                    ? node.NodeId
                    : new PoseNodeId(scope + "/" + node.NodeId.Value);
                if ((node.Kind ==
                         CharacterPoseNodeKind.SelectedPosePlayer ||
                     node.Kind ==
                         CharacterPoseNodeKind.BlendStack) &&
                    node.PresentationPoseSourceSlot == slot)
                {
                    if (result.IsValid)
                        throw new InvalidOperationException(
                            $"Motion Matching Source Slot '{slot.name}' matches more than one Pose Graph player node.");
                    result = scopedNodeId;
                }
                if (node.Kind ==
                        CharacterPoseNodeKind.PoseStateMachine &&
                    node.PoseStateMachine != null)
                {
                    for (int stateIndex = 0;
                         stateIndex <
                         node.PoseStateMachine.States.Count;
                         stateIndex++)
                    {
                        CharacterPoseStateDefinition state =
                            node.PoseStateMachine.States[
                                stateIndex];
                        if (state == null ||
                            !state.PoseGraphId.IsValid)
                            continue;
                        CharacterTypedPoseGraph stateGraph =
                            owner.RequireGraph(
                                state.PoseGraphId);
                        ResolveProviderNodeId(
                            owner,
                            stateGraph,
                            slot,
                            scopedNodeId.Value +
                            "/state/" +
                            state.StateId.Value,
                            ref result);
                    }
                }
                if (node.Kind != CharacterPoseNodeKind.PoseSubgraph ||
                    node.Subgraph == null ||
                    !node.Subgraph.PoseGraphId.IsValid)
                    continue;
                CharacterTypedPoseGraph child =
                    owner.RequireGraph(node.Subgraph.PoseGraphId);
                ResolveProviderNodeId(
                    owner,
                    child,
                    slot,
                    scopedNodeId.Value + "/" + child.GraphId,
                    ref result);
            }
        }

        static MotionMatchingDatabasePayload CompileDatabase(
            CharacterMotionMatchingProfile profile,
            CharacterMotionMatchingDatabaseDefinition database,
            CharacterFootPlacementAnalysisSource analysisSource,
            IMotionMatchingProjectionParameterCurveResolver parameterCurveResolver)
        {
            MotionMatchingDatabaseBuildRequest request = MotionMatchingDatabaseBuildRequestFactory.Create(
                profile, database, analysisSource);
            CharacterMotionMatchingArtifactInspection inspection = CharacterMotionMatchingDatabaseArtifactStore.Inspect(
                database, request.ExpectedIdentity);
            if (inspection.Status != CharacterMotionMatchingArtifactStatus.Ready || inspection.Artifact == null)
                throw new InvalidOperationException($"Motion Matching Database '{database.DatabaseId}' Artifact is {inspection.Status}: {inspection.Diagnostic}");
            CharacterMotionMatchingDatabaseArtifact artifact = inspection.Artifact;
            var clipBindings = new MotionMatchingClipBindingPayload[request.ClipCount];
            for (int i = 0; i < clipBindings.Length; i++)
            {
                MotionMatchingResolvedClipBuildInput clip = request.GetClip(i);
                MotionMatchingClipDependencyIdentity dependency = artifact.Identity.GetClipDependency(i);
                if (!dependency.SourceClipId.Equals(clip.SourceClipId) ||
                    !string.Equals(dependency.AssetGuid, clip.AssetGuid, StringComparison.Ordinal) ||
                    dependency.LocalFileId != clip.LocalFileId)
                    throw new InvalidOperationException($"Motion Matching Artifact Clip dependency #{i} does not match the resolved Source Set closure.");
                MotionMatchingPoseParameterCurvePayload curve = parameterCurveResolver.ResolveRequired(
                    clip.Clip, MotionMatchingPoseSourceParameterContract.FootPlacementWeightId);
                clipBindings[i] = new MotionMatchingClipBindingPayload(
                    clip.SourceClipId, clip.AssetGuid, clip.LocalFileId, clip.Clip, true, curve);
            }
            var segments = new MotionMatchingSegmentPayload[artifact.SegmentCount];
            var samples = new MotionMatchingSamplePayload[artifact.SampleCount];
            var normalized = new float[artifact.NormalizedFeatureCount];
            var median = new float[artifact.Capacities.DenseFeatureCount];
            var scale = new float[artifact.Capacities.DenseFeatureCount];
            var active = new bool[artifact.Capacities.DenseFeatureCount];
            var nodes = new MotionMatchingSearchIndexNodePayload[artifact.SearchNodeCount];
            var ordered = new int[artifact.OrderedSampleIndexCount];
            var coverage = new MotionMatchingCoverageSummaryPayload[artifact.CoverageCount];
            for (int i = 0; i < segments.Length; i++) segments[i] = artifact.GetSegment(i);
            for (int i = 0; i < samples.Length; i++) samples[i] = artifact.GetSample(i);
            for (int i = 0; i < normalized.Length; i++) normalized[i] = artifact.GetNormalizedFeature(i);
            for (int i = 0; i < median.Length; i++)
            {
                median[i] = artifact.GetNormalizationMedian(i);
                scale[i] = artifact.GetNormalizationScale(i);
                active[i] = artifact.IsFeatureActive(i);
            }
            for (int i = 0; i < nodes.Length; i++) nodes[i] = artifact.GetSearchNode(i);
            for (int i = 0; i < ordered.Length; i++) ordered[i] = artifact.GetOrderedSampleIndex(i);
            for (int i = 0; i < coverage.Length; i++) coverage[i] = artifact.GetCoverage(i);
            return new MotionMatchingDatabasePayload(
                artifact.Identity, artifact.SearchDomainId, artifact.SampleRate, artifact.Capacities,
                clipBindings, segments, samples, normalized, median, scale, active, nodes, ordered, coverage);
        }
    }
}
