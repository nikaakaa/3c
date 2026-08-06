using System;
using System.Collections.Generic;
using System.Linq;
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
            CharacterAnimationRigDefinition rig,
            CharacterFootPlacementAnalysisSource analysisSource,
            IMotionMatchingProjectionParameterCurveResolver parameterCurveResolver)
        {
            if (!profile)
                throw new ArgumentNullException(nameof(profile));
            if (!analysisSource)
                throw new ArgumentNullException(nameof(analysisSource));
            if (!poseGraphAsset || poseGraphAsset.Graph == null || !rig)
                throw new ArgumentNullException(nameof(poseGraphAsset));
            if (parameterCurveResolver == null)
                throw new ArgumentNullException(nameof(parameterCurveResolver));
            CharacterMotionMatchingAuthoringValidator.RequireProfile(profile);
            profile.RequireRigClosure(rig);
            MotionMatchingNodeAuthoringBinding[] nodeBindings = ResolveNodeBindings(poseGraphAsset).ToArray();
            if (nodeBindings.Length == 0)
                throw new InvalidOperationException("Motion Matching Projection requires at least one MotionMatchingPose node.");
            var databases = new List<MotionMatchingDatabasePayload>();
            var bindings =
                new MotionMatchingNodeBindingPayload[nodeBindings.Length];
            for (int bindingIndex = 0;
                 bindingIndex < nodeBindings.Length;
                 bindingIndex++)
            {
                MotionMatchingNodeAuthoringBinding nodeBinding = nodeBindings[bindingIndex];
                CharacterMotionMatchingBinding binding = nodeBinding.Payload.Binding;
                binding.RequireValid(rig);
                if (binding.Profile != profile)
                    throw new InvalidOperationException($"Motion Matching node binding '{binding.name}' is outside the compiled Profile.");
                CharacterMotionMatchingDatabaseDefinition[] selectedDatabases = binding.Chooser.Rules
                    .SelectMany(value => value.Databases)
                    .Distinct()
                    .ToArray();
                int firstDatabase = databases.Count;
                var databaseIndices =
                    new Dictionary<CharacterMotionMatchingDatabaseDefinition, int>();
                for (int databaseIndex = 0; databaseIndex < selectedDatabases.Length; databaseIndex++)
                {
                    databaseIndices.Add(
                        selectedDatabases[databaseIndex],
                        firstDatabase + databaseIndex);
                    databases.Add(CompileDatabase(
                        profile,
                        selectedDatabases[databaseIndex],
                        analysisSource,
                        parameterCurveResolver));
                }
                bindings[bindingIndex] = new MotionMatchingNodeBindingPayload(
                    binding.BindingId,
                    binding.Revision,
                    nodeBinding.ScopedNodeId,
                    CompileChooser(binding.Chooser, databaseIndices),
                    firstDatabase,
                    selectedDatabases.Length);
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

        static MotionMatchingDatabaseChooserPayload CompileChooser(
            CharacterMotionMatchingDatabaseChooser chooser,
            IReadOnlyDictionary<CharacterMotionMatchingDatabaseDefinition, int> databaseIndices)
        {
            var rules =
                new MotionMatchingDatabaseChooserRulePayload[chooser.Rules.Count];
            for (int ruleIndex = 0; ruleIndex < rules.Length; ruleIndex++)
            {
                CharacterMotionMatchingDatabaseChooserRule source =
                    chooser.Rules[ruleIndex];
                var predicates =
                    new MotionMatchingFactPredicatePayload[source.Predicates.Count];
                for (int predicateIndex = 0;
                     predicateIndex < predicates.Length;
                     predicateIndex++)
                {
                    CharacterMotionMatchingFactPredicate predicate =
                        source.Predicates[predicateIndex];
                    predicates[predicateIndex] =
                        new MotionMatchingFactPredicatePayload(
                            predicate.FactId,
                            predicate.ValueKind,
                            predicate.Operator,
                            predicate.BoolValue,
                            predicate.FloatValue,
                            predicate.Vector2Value,
                            predicate.EnumValue,
                            predicate.UInt64Value,
                            predicate.IdentityValue);
                }
                var ruleDatabaseIndices = new int[source.Databases.Count];
                for (int databaseIndex = 0;
                     databaseIndex < ruleDatabaseIndices.Length;
                     databaseIndex++)
                {
                    CharacterMotionMatchingDatabaseDefinition database =
                        source.Databases[databaseIndex];
                    if (!databaseIndices.TryGetValue(
                            database,
                            out ruleDatabaseIndices[databaseIndex]))
                    {
                        throw new InvalidOperationException(
                            $"Motion Matching Chooser '{chooser.ChooserId}' rule #{ruleIndex} Database is outside its compiled closure.");
                    }
                }
                rules[ruleIndex] =
                    new MotionMatchingDatabaseChooserRulePayload(
                        source.Priority,
                        source.Exclusive,
                        predicates,
                        ruleDatabaseIndices,
                        source.ShouldSearch,
                        source.InterruptMode,
                        source.SearchPolicyOverrideId);
            }
            return new MotionMatchingDatabaseChooserPayload(
                chooser.ChooserId,
                chooser.Revision,
                chooser.SearchDomainId,
                rules);
        }

        readonly struct MotionMatchingNodeAuthoringBinding
        {
            internal MotionMatchingNodeAuthoringBinding(PoseNodeId scopedNodeId, CharacterMotionMatchingPosePayload payload)
            {
                ScopedNodeId = scopedNodeId;
                Payload = payload ?? throw new ArgumentNullException(nameof(payload));
            }

            internal PoseNodeId ScopedNodeId { get; }
            internal CharacterMotionMatchingPosePayload Payload { get; }
        }

        static IEnumerable<MotionMatchingNodeAuthoringBinding> ResolveNodeBindings(
            CharacterPresentationPoseGraphAsset owner)
        {
            var result = new List<MotionMatchingNodeAuthoringBinding>();
            ResolveNodeBindings(owner, owner.Graph, string.Empty, result);
            return result;
        }

        static void ResolveNodeBindings(
            CharacterPresentationPoseGraphAsset owner,
            CharacterTypedPoseGraph graph,
            string scope,
            ICollection<MotionMatchingNodeAuthoringBinding> result)
        {
            for (int i = 0; i < graph.Nodes.Count; i++)
            {
                CharacterTypedPoseNode node = graph.Nodes[i];
                if (node == null)
                    continue;
                PoseNodeId scopedNodeId = string.IsNullOrEmpty(scope)
                    ? node.NodeId
                    : new PoseNodeId(scope + "/" + node.NodeId.Value);
                if (node.Payload is CharacterMotionMatchingPosePayload payload)
                {
                    result.Add(new MotionMatchingNodeAuthoringBinding(scopedNodeId, payload));
                    continue;
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
                        ResolveNodeBindings(
                            owner,
                            stateGraph,
                            scopedNodeId.Value +
                            "/state/" +
                            state.StateId.Value,
                            result);
                    }
                }
                if (node.Kind != CharacterPoseNodeKind.PoseSubgraph ||
                    node.Subgraph == null ||
                    !node.Subgraph.PoseGraphId.IsValid)
                    continue;
                CharacterTypedPoseGraph child =
                    owner.RequireGraph(node.Subgraph.PoseGraphId);
                ResolveNodeBindings(
                    owner,
                    child,
                    scopedNodeId.Value + "/" + child.GraphId,
                    result);
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
