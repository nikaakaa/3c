using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ThirdPersonCharacter.Animation.TransitionRouting;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Editor.CharacterSimulation
{
    static class CharacterPresentationInertializationPlanCompiler
    {
        public static CharacterPresentationInertializationDescriptor[] Compile(
            CharacterPoseFamilyPayloadPlan binding,
            CharacterPresentationPoseGraphAsset graphAsset,
            CharacterAnimationRigDefinition rig,
            IReadOnlyDictionary<string, int> curveIndices,
            IReadOnlyDictionary<string, int> profileIndices)
        {
            if (binding == null)
                throw new ArgumentNullException(nameof(binding));
            if (!graphAsset || graphAsset.Graph == null)
                throw new ArgumentNullException(nameof(graphAsset));
            if (!rig)
                throw new ArgumentNullException(nameof(rig));
            if (curveIndices == null || profileIndices == null)
                throw new ArgumentNullException(nameof(curveIndices));
            CharacterPoseBoundOperation[] operations = binding.Operations;
            CharacterPoseBoundFamilyPayloads payloads = binding.Payloads;
            var policies =
                new Dictionary<PoseNodeId,
                    CharacterPoseInertializationPolicy>();
            CollectPolicies(
                graphAsset,
                graphAsset.Graph,
                string.Empty,
                policies);
            int inertializationCount = operations.Count(value =>
                value.Code == CharacterPoseOperationCode.Inertialization);
            var descriptors =
                new CharacterPresentationInertializationDescriptor[
                    inertializationCount];
            for (int index = 0; index < descriptors.Length; index++)
            {
                CharacterPoseBoundOperation operation =
                    operations.Single(value =>
                        value.Code ==
                            CharacterPoseOperationCode.Inertialization &&
                        value.InertializationIndex == index);
                int inputPose = operation.InputValueIndexA;
                CharacterPoseBoundOperation inputOwner =
                    operations.SingleOrDefault(value =>
                        value.Index < operation.Index &&
                        value.OutputValueIndex == inputPose);
                if (inputOwner == null)
                {
                    throw new InvalidOperationException(
                        $"Inertialization '{operation.NodeId}' has no direct input owner.");
                }
                if (!policies.TryGetValue(
                        operation.NodeId,
                        out CharacterPoseInertializationPolicy policy) ||
                    !policy)
                {
                    throw new InvalidOperationException(
                        $"Inertialization '{operation.NodeId}' has no authoring Policy.");
                }
                policy.RequireValid(rig);
                PoseParameterInertializationMode[] parameterModes =
                    CompileParameterModes(
                        operation.NodeId,
                        policy.Response,
                        payloads.Parameters);
                PoseInertializationTemporalOwnerKind ownerKind;
                int inputOwnerIndex;
                CharacterPresentationInertializationRuleDescriptor[] rules;
                if (inputOwner.Code ==
                    CharacterPoseOperationCode.PoseStateMachine)
                {
                    if (policy.DirectPlayerRule != null)
                    {
                        throw new InvalidOperationException(
                            $"Inertialization '{operation.NodeId}' is owned by PoseStateMachine and cannot declare a Direct Player temporal rule.");
                    }
                    int stateMachineIndex = inputOwner.StateMachineIndex;
                    if ((uint)stateMachineIndex >=
                        (uint)payloads.StateMachines.Length)
                    {
                        throw new InvalidOperationException(
                            $"Inertialization '{operation.NodeId}' StateMachine owner is invalid.");
                    }
                    CharacterPoseStateMachineDescriptor stateMachine =
                        payloads.StateMachines[stateMachineIndex];
                    CharacterPoseStateTransitionDescriptor[] transitions =
                        stateMachine.Transitions
                            .Where(value =>
                                value.BlendLogic ==
                                AnimationTransitionBlendLogic
                                    .Inertialization)
                            .OrderBy(value => value.Index)
                            .ToArray();
                    if (transitions.Length == 0)
                    {
                        throw new InvalidOperationException(
                            $"Inertialization '{operation.NodeId}' PoseStateMachine has no inertial transition.");
                    }
                    rules =
                        new CharacterPresentationInertializationRuleDescriptor[
                            transitions.Length];
                    for (int transitionIndex = 0;
                         transitionIndex < transitions.Length;
                         transitionIndex++)
                    {
                        CharacterPoseStateTransitionDescriptor transition =
                            transitions[transitionIndex];
                        rules[transitionIndex] =
                            new CharacterPresentationInertializationRuleDescriptor(
                                transition.SourceStateIndex,
                                transition.TargetStateIndex,
                                PoseInertializationMode.Inertialize,
                                transition.DurationSeconds,
                                transition.CurveIndex,
                                transition.BlendProfileIndex,
                                parameterModes);
                    }
                    ownerKind =
                        PoseInertializationTemporalOwnerKind
                            .StateMachineTransition;
                    inputOwnerIndex = stateMachineIndex;
                }
                else if (IsDirectPlayer(inputOwner.Code))
                {
                    PresentationPoseSourceIndex sourceIndex =
                        inputOwner.PresentationPoseSourceIndex;
                    if (!sourceIndex.IsValid)
                    {
                        throw new InvalidOperationException(
                            $"Inertialization '{operation.NodeId}' direct Player has no source index.");
                    }
                    CharacterPoseDirectInertializationRule directRule =
                        policy.DirectPlayerRule;
                    if (directRule == null)
                    {
                        throw new InvalidOperationException(
                            $"Inertialization '{operation.NodeId}' direct Player requires one exact temporal rule.");
                    }
                    int curveIndex = -1;
                    int profileIndex = -1;
                    if (directRule.Mode ==
                        PoseInertializationMode.Inertialize)
                    {
                        string curveKey =
                            AnimationBlendCanonicalPayload.CurveKey(
                                directRule.CompileCurve());
                        if (!curveIndices.TryGetValue(
                                curveKey,
                                out curveIndex) ||
                            !profileIndices.TryGetValue(
                                directRule.BlendProfile.ProfileId,
                                out profileIndex))
                        {
                            throw new InvalidOperationException(
                                $"Inertialization '{operation.NodeId}' direct Player temporal assets are absent from the Projection catalog.");
                        }
                    }
                    rules =
                        new[]
                        {
                            new CharacterPresentationInertializationRuleDescriptor(
                                sourceIndex.Value,
                                sourceIndex.Value,
                                directRule.Mode,
                                directRule.DurationSeconds,
                                curveIndex,
                                profileIndex,
                                parameterModes)
                        };
                    ownerKind =
                        PoseInertializationTemporalOwnerKind
                            .DirectPlayerPolicy;
                    inputOwnerIndex = sourceIndex.Value;
                }
                else
                {
                    throw new InvalidOperationException(
                        $"Inertialization '{operation.NodeId}' input '{inputOwner.Code}' has no exact temporal owner contract.");
                }
                descriptors[index] =
                    new CharacterPresentationInertializationDescriptor(
                        index,
                        operation.NodeId,
                        ownerKind,
                        inputOwner.NodeId,
                        inputOwnerIndex,
                        policy.PolicyId,
                        policy.Revision,
                        rules);
            }
            return descriptors;
        }

        public static string ComputeProgramHash(
            string baseHash,
            IReadOnlyList<CharacterPresentationInertializationDescriptor>
                descriptors)
        {
            if (string.IsNullOrWhiteSpace(baseHash))
                throw new ArgumentException(nameof(baseHash));
            if (descriptors == null)
                throw new ArgumentNullException(nameof(descriptors));
            var hashTokens = new List<string>
            {
                baseHash,
                CharacterPoseInertializationPolicy.SchemaVersion
            };
            for (int i = 0; i < descriptors.Count; i++)
            {
                CharacterPresentationInertializationDescriptor descriptor =
                    descriptors[i];
                hashTokens.Add(
                    $"node:{descriptor.NodeId}:{(int)descriptor.TemporalOwnerKind}:{descriptor.InputOwnerNodeId}:{descriptor.InputOwnerIndex}:{descriptor.PolicyId}:{descriptor.PolicyRevision}");
                for (int ruleIndex = 0;
                     ruleIndex < descriptor.Rules.Count;
                     ruleIndex++)
                {
                    CharacterPresentationInertializationRuleDescriptor rule =
                        descriptor.Rules[ruleIndex];
                    hashTokens.Add(FormattableString.Invariant(
                        $"rule:{rule.SourceEndpointIndex}:{rule.TargetEndpointIndex}:{(int)rule.Mode}:{rule.DurationSeconds:R}:{rule.CurveIndex}:{rule.ProfileIndex}:{string.Join(",", rule.ParameterModes.Select(value => ((int)value).ToString(CultureInfo.InvariantCulture)))}"));
                }
            }
            return StableHash.Compute(hashTokens.ToArray()).ToString();
        }

        static PoseParameterInertializationMode[] CompileParameterModes(
            PoseNodeId nodeId,
            CharacterPoseInertializationResponse response,
            IReadOnlyList<CharacterPresentationPoseParameterEntry> parameters)
        {
            if (response.ParameterFilters.Count != parameters.Count)
                throw new InvalidOperationException($"Inertialization '{nodeId}' rule must declare every Pose Parameter exactly once.");
            var authored = new Dictionary<PoseParameterId, PoseParameterInertializationMode>();
            for (int i = 0; i < response.ParameterFilters.Count; i++)
            {
                CharacterPoseParameterInertializationFilter filter = response.ParameterFilters[i];
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

        static bool IsDirectPlayer(CharacterPoseOperationCode code) =>
            code == CharacterPoseOperationCode.SelectedPosePlayer ||
            code == CharacterPoseOperationCode.BlendSpacePlayer ||
            code == CharacterPoseOperationCode.ClipPlayer;

        static void CollectPolicies(
            CharacterPresentationPoseGraphAsset owner,
            CharacterTypedPoseGraph graph,
            string scope,
            Dictionary<PoseNodeId, CharacterPoseInertializationPolicy> result)
        {
            for (int i = 0; i < graph.Nodes.Count; i++)
            {
                CharacterTypedPoseNode node = graph.Nodes[i];
                PoseNodeId scopedNodeId = string.IsNullOrEmpty(scope)
                    ? node.NodeId
                    : new PoseNodeId(scope + "/" + node.NodeId.Value);
                if (node.Kind == CharacterPoseNodeKind.Inertialization)
                    result.Add(scopedNodeId, node.InertializationPolicy);
                if (node.Kind != CharacterPoseNodeKind.PoseSubgraph ||
                    node.Subgraph == null ||
                    !node.Subgraph.PoseGraphId.IsValid)
                    continue;
                CharacterTypedPoseGraph child =
                    owner.RequireGraph(node.Subgraph.PoseGraphId);
                CollectPolicies(
                    owner,
                    child,
                    scopedNodeId.Value + "/" + child.GraphId,
                    result);
            }
        }
    }
}
