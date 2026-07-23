using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Editor.CharacterSimulation
{
    static class CharacterPresentationInertializationPlanCompiler
    {
        public static CharacterPresentationPosePlan Compile(
            CharacterPresentationPosePlan source,
            CharacterPoseGraphData graph,
            CharacterAnimationRigDefinition rig,
            IReadOnlyList<CharacterPresentationProducerEntry> producers,
            IReadOnlyDictionary<string, int> curveIndices,
            IReadOnlyDictionary<string, int> profileIndicesByIdentity,
            List<string> errors)
        {
            try
            {
                source.RequireValid();
                var policies = new Dictionary<PoseNodeId, CharacterPoseInertializationPolicy>();
                CollectPolicies(graph, string.Empty, policies);
                int inertializationCount = source.Operations.Count(value =>
                    value.Code == CharacterPoseOperationCode.Inertialization);
                var descriptors = new CharacterPresentationInertializationDescriptor[inertializationCount];
                for (int index = 0; index < descriptors.Length; index++)
                {
                    CharacterPresentationPoseOperation operation = source.Operations.Single(value =>
                        value.Code == CharacterPoseOperationCode.Inertialization && value.InertializationIndex == index);
                    CharacterPresentationPoseOperation player = source.Operations.SingleOrDefault(value =>
                        value.Index < operation.Index && value.OutputValueIndex == operation.InputValueIndexA);
                    if (player == null || player.Code != CharacterPoseOperationCode.SelectedPosePlayer &&
                        player.Code != CharacterPoseOperationCode.BlendSpacePlayer)
                        throw new InvalidOperationException($"Inertialization '{operation.NodeId}' must receive Pose directly from one single-source Player.");
                    if (!policies.TryGetValue(operation.NodeId, out CharacterPoseInertializationPolicy policy) || !policy)
                        throw new InvalidOperationException($"Inertialization '{operation.NodeId}' has no authoring Policy.");
                    policy.RequireValid(rig);
                    CharacterPresentationSelectionInputEntry selection = source.SelectionInputs[player.SelectionInputIndex];
                    CharacterPresentationProducerEntry[] reachable = producers
                        .Where(value => value != null && value.Kind == CharacterPresentationProducerKind.Animation &&
                                        value.AnimationChannelId == selection.AnimationChannelId &&
                                        (string.IsNullOrEmpty(selection.ProgramProducerId) ||
                                         string.Equals(value.ProgramProducerIdentity, selection.ProgramProducerId, StringComparison.Ordinal)))
                        .OrderBy(value => value.ProgramProducerIndex)
                        .ToArray();
                    if (reachable.Length == 0)
                        throw new InvalidOperationException($"Inertialization '{operation.NodeId}' has no reachable Player endpoint.");
                    ValidateOverrides(operation.NodeId, policy, reachable);
                    var rules = new List<CharacterPresentationInertializationRuleDescriptor>(checked(reachable.Length * reachable.Length));
                    for (int sourceIndex = 0; sourceIndex < reachable.Length; sourceIndex++)
                    {
                        for (int targetIndex = 0; targetIndex < reachable.Length; targetIndex++)
                        {
                            CharacterPresentationProducerEntry from = reachable[sourceIndex];
                            CharacterPresentationProducerEntry to = reachable[targetIndex];
                            CharacterPoseInertializationRule rule = ResolveRule(
                                policy, from.ProgramProducerIdentity, to.ProgramProducerIdentity);
                            PoseParameterInertializationMode[] parameterModes = CompileParameterModes(
                                operation.NodeId, rule, source.Parameters);
                            int curveIndex = -1;
                            int profileIndex = -1;
                            if (rule.Mode == PoseInertializationMode.Inertialize)
                            {
                                string curveKey = AnimationBlendCanonicalPayload.CurveKey(rule.Curve.Compile());
                                if (!curveIndices.TryGetValue(curveKey, out curveIndex) ||
                                    !profileIndicesByIdentity.TryGetValue(rule.BlendProfile.ProfileId, out profileIndex))
                                    throw new InvalidOperationException($"Inertialization '{operation.NodeId}' exact rule catalog payload is missing.");
                            }
                            rules.Add(new CharacterPresentationInertializationRuleDescriptor(
                                from.ProgramProducerIndex,
                                to.ProgramProducerIndex,
                                rule.Mode,
                                rule.Mode == PoseInertializationMode.Inertialize ? rule.DurationSeconds : 0f,
                                curveIndex,
                                profileIndex,
                                parameterModes));
                        }
                    }
                    descriptors[index] = new CharacterPresentationInertializationDescriptor(
                        index,
                        operation.NodeId,
                        player.NodeId,
                        player.PlayerIndex,
                        policy.PolicyId,
                        policy.Revision,
                        rules.ToArray());
                }
                CharacterPresentationPosePlan result = Rebuild(source, rig, descriptors);
                result.RequireInertializationValid();
                return result;
            }
            catch (Exception exception)
            {
                errors?.Add(exception.Message);
                return null;
            }
        }

        static CharacterPresentationPosePlan Rebuild(
            CharacterPresentationPosePlan source,
            CharacterAnimationRigDefinition rig,
            CharacterPresentationInertializationDescriptor[] descriptors)
        {
            var hashTokens = new List<string> { source.PlanHash, CharacterPoseInertializationPolicy.SchemaVersion };
            for (int i = 0; i < descriptors.Length; i++)
            {
                CharacterPresentationInertializationDescriptor descriptor = descriptors[i];
                hashTokens.Add($"node:{descriptor.NodeId}:{descriptor.InputPlayerNodeId}:{descriptor.InputPlayerIndex}:{descriptor.PolicyId}:{descriptor.PolicyRevision}");
                for (int ruleIndex = 0; ruleIndex < descriptor.Rules.Count; ruleIndex++)
                {
                    CharacterPresentationInertializationRuleDescriptor rule = descriptor.Rules[ruleIndex];
                    hashTokens.Add(FormattableString.Invariant(
                        $"rule:{rule.SourceProgramProducerIndex}:{rule.TargetProgramProducerIndex}:{(int)rule.Mode}:{rule.DurationSeconds:R}:{rule.CurveIndex}:{rule.ProfileIndex}:{string.Join(",", rule.ParameterModes.Select(value => ((int)value).ToString(CultureInfo.InvariantCulture)))}"));
                }
            }
            return new CharacterPresentationPosePlan(
                source.PoseGraphId,
                source.ContentRevision,
                StableHash.Compute(hashTokens.ToArray()).ToString(),
                rig,
                source.SelectionInputs.ToArray(),
                source.Parameters.ToArray(),
                source.BlendNodes.ToArray(),
                descriptors,
                source.BoneMasks.ToArray(),
                source.AdditiveReferences.ToArray(),
                source.ModifyBones.ToArray(),
                source.FootPlacementNodes.ToArray(),
                source.Operations.ToArray(),
                source.SourceMap.ToArray(),
                source.SelectionWorkspaceCount,
                source.PoseValueWorkspaceCount,
                source.ParameterWorkspaceCount,
                source.ContributionWorkspaceCount,
                source.FrameCacheCount,
                source.OutputOperationIndex);
        }

        static PoseParameterInertializationMode[] CompileParameterModes(
            PoseNodeId nodeId,
            CharacterPoseInertializationRule rule,
            IReadOnlyList<CharacterPresentationPoseParameterEntry> parameters)
        {
            if (rule.ParameterFilters.Count != parameters.Count)
                throw new InvalidOperationException($"Inertialization '{nodeId}' rule must declare every Pose Parameter exactly once.");
            var authored = new Dictionary<PoseParameterId, PoseParameterInertializationMode>();
            for (int i = 0; i < rule.ParameterFilters.Count; i++)
            {
                CharacterPoseParameterInertializationFilter filter = rule.ParameterFilters[i];
                if (filter == null || !authored.TryAdd(filter.ParameterId, filter.Mode))
                    throw new InvalidOperationException($"Inertialization '{nodeId}' parameter filter #{i} is invalid or duplicated.");
            }
            var result = new PoseParameterInertializationMode[parameters.Count];
            for (int i = 0; i < parameters.Count; i++)
            {
                if (!authored.TryGetValue(parameters[i].ParameterId, out result[i]))
                    throw new InvalidOperationException($"Inertialization '{nodeId}' is missing Parameter '{parameters[i].ParameterId}'.");
            }
            return result;
        }

        static CharacterPoseInertializationRule ResolveRule(
            CharacterPoseInertializationPolicy policy,
            string sourceIdentity,
            string targetIdentity)
        {
            CharacterPoseInertializationRule result = policy.DefaultRule;
            for (int i = 0; i < policy.Overrides.Count; i++)
            {
                CharacterPoseInertializationOverride value = policy.Overrides[i];
                if (string.Equals(value.SourceProducerIdentity, sourceIdentity, StringComparison.Ordinal) &&
                    string.Equals(value.TargetProducerIdentity, targetIdentity, StringComparison.Ordinal))
                    return value.Rule;
            }
            return result;
        }

        static void ValidateOverrides(
            PoseNodeId nodeId,
            CharacterPoseInertializationPolicy policy,
            IReadOnlyList<CharacterPresentationProducerEntry> reachable)
        {
            var identities = new HashSet<string>(reachable.Select(value => value.ProgramProducerIdentity), StringComparer.Ordinal);
            for (int i = 0; i < policy.Overrides.Count; i++)
            {
                CharacterPoseInertializationOverride value = policy.Overrides[i];
                if (!identities.Contains(value.SourceProducerIdentity) || !identities.Contains(value.TargetProducerIdentity))
                    throw new InvalidOperationException($"Inertialization '{nodeId}' override #{i} references an endpoint outside its direct Player.");
            }
        }

        static void CollectPolicies(
            CharacterPoseGraphData graph,
            string scope,
            Dictionary<PoseNodeId, CharacterPoseInertializationPolicy> result)
        {
            for (int i = 0; i < graph.Nodes.Count; i++)
            {
                CharacterPoseNodeDefinition node = graph.Nodes[i];
                PoseNodeId scopedNodeId = string.IsNullOrEmpty(scope)
                    ? node.NodeId
                    : new PoseNodeId(scope + "/" + node.NodeId.Value);
                if (node.Kind == CharacterPoseNodeKind.Inertialization)
                    result.Add(scopedNodeId, node.InertializationPolicy);
                if (node.Kind != CharacterPoseNodeKind.PoseSubgraph || node.Subgraph == null || !node.Subgraph.IsExclusive)
                    continue;
                CharacterPoseGraphData child = node.Subgraph.HasInline ? node.Subgraph.Inline : node.Subgraph.Shared.Graph;
                CollectPolicies(child, scopedNodeId.Value + "/" + child.GraphId, result);
            }
        }
    }
}
