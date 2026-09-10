using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;

namespace ThirdPersonCharacter.Editor.CharacterSimulation
{
    internal sealed class CharacterMotionMatchingPosePlanCompilation
    {
        internal CharacterMotionMatchingPosePlanCompilation(
            CharacterMotionMatchingPosePlanDescriptor[] nodes,
            CharacterPoseHistoryCollectorPlanDescriptor[] collectors,
            CharacterMotionMatchingEntryProgramDescriptor[] entryPrograms,
            CharacterMotionMatchingBlendPlanDescriptor[] blendPlans)
        {
            Nodes = nodes ??
                Array.Empty<CharacterMotionMatchingPosePlanDescriptor>();
            Collectors = collectors ??
                Array.Empty<CharacterPoseHistoryCollectorPlanDescriptor>();
            EntryPrograms = entryPrograms ??
                Array.Empty<CharacterMotionMatchingEntryProgramDescriptor>();
            BlendPlans = blendPlans ??
                Array.Empty<CharacterMotionMatchingBlendPlanDescriptor>();
            int contributionCapacity = 0;
            for (int i = 0; i < Nodes.Length; i++)
            {
                CharacterMotionMatchingPosePlanDescriptor node = Nodes[i] ??
                    throw new InvalidOperationException(
                        $"Motion Matching Pose plan #{i} is missing.");
                contributionCapacity = checked(
                    contributionCapacity +
                    node.LiveEntryCapacity +
                    node.StoredPoseCapacity);
            }
            ContributionCapacity = contributionCapacity;
        }

        internal CharacterMotionMatchingPosePlanDescriptor[] Nodes { get; }
        internal CharacterPoseHistoryCollectorPlanDescriptor[] Collectors
        {
            get;
        }
        internal CharacterMotionMatchingEntryProgramDescriptor[] EntryPrograms
        {
            get;
        }
        internal CharacterMotionMatchingBlendPlanDescriptor[] BlendPlans
        {
            get;
        }
        internal int ContributionCapacity { get; }
    }

    internal static class CharacterMotionMatchingPosePlanCompiler
    {
        readonly struct ScopedNode
        {
            internal ScopedNode(
                CharacterPoseCanvasGraph graph,
                CharacterPoseCanvasNode node,
                PoseNodeId scopedNodeId,
                string scope)
            {
                Graph = graph;
                Node = node;
                ScopedNodeId = scopedNodeId;
                Scope = scope ?? string.Empty;
            }

            internal CharacterPoseCanvasGraph Graph { get; }
            internal CharacterPoseCanvasNode Node { get; }
            internal PoseNodeId ScopedNodeId { get; }
            internal string Scope { get; }
        }

        internal static CharacterMotionMatchingPosePlanCompilation Compile(
            CharacterPoseFamilyPayloadPlan pose,
            CharacterPresentationPoseGraphAsset graphAsset,
            CharacterAnimationRigDefinition rig,
            MotionMatchingProjectionPayload motionMatching,
            CharacterPresentationPoseResourceCompilationCatalog resources,
            IReadOnlyDictionary<string, int> curveIndices,
            IReadOnlyDictionary<string, int> profileIndicesByIdentity)
        {
            if (pose == null || !graphAsset || !rig || resources == null)
                throw new ArgumentException("Motion Matching Pose plan compilation input is incomplete.");
            ScopedNode[] nodes = Enumerate(graphAsset)
                .Where(value => value.Node.Payload is CharacterMotionMatchingPosePayload)
                .OrderBy(value => value.ScopedNodeId)
                .ToArray();
            if (nodes.Length == 0)
            {
                return new CharacterMotionMatchingPosePlanCompilation(
                    Array.Empty<CharacterMotionMatchingPosePlanDescriptor>(),
                    Array.Empty<CharacterPoseHistoryCollectorPlanDescriptor>(),
                    Array.Empty<CharacterMotionMatchingEntryProgramDescriptor>(),
                    Array.Empty<CharacterMotionMatchingBlendPlanDescriptor>());
            }
            if (motionMatching == null || curveIndices == null || profileIndicesByIdentity == null)
                throw new InvalidOperationException("Motion Matching Pose nodes require compiled Projection and Blend catalogs.");
            if (motionMatching.NodeBindingCount != nodes.Length)
            {
                throw new InvalidOperationException("Motion Matching Pose plan Rig or node binding closure is inconsistent.");
            }

            var operations = pose.Operations.ToDictionary(
                value => value.NodeId);
            var collectors = new List<CharacterPoseHistoryCollectorPlanDescriptor>(nodes.Length);
            var entryPrograms = new List<CharacterMotionMatchingEntryProgramDescriptor>(nodes.Length);
            var blends = new List<CharacterMotionMatchingBlendPlanDescriptor>(nodes.Length);
            var compiledNodes = new CharacterMotionMatchingPosePlanDescriptor[nodes.Length];
            var collectorIds = new HashSet<PoseNodeId>();
            var entryGraphIds = new HashSet<PoseGraphId>();

            for (int nodeIndex = 0; nodeIndex < nodes.Length; nodeIndex++)
            {
                ScopedNode scoped = nodes[nodeIndex];
                var payload = (CharacterMotionMatchingPosePayload)scoped.Node.Payload;
                payload.RequireValid();
                MotionMatchingNodeBindingPayload binding = RequireBinding(
                    motionMatching,
                    scoped.ScopedNodeId);
                CharacterPoseBoundOperation operation = RequireOperation(
                    operations,
                    scoped.ScopedNodeId,
                    CharacterPoseOperationCode.MotionMatchingPose);
                CharacterPoseCanvasConnection historyEdge = scoped.Graph.Edges.Single(edge =>
                    edge != null && edge.TargetNodeId == scoped.Node.NodeId &&
                    CharacterMotionMatchingPosePorts.History.Equals(edge.TargetPortId));
                CharacterPoseCanvasNode collectorNode = scoped.Graph.Nodes.Single(value =>
                    value != null && value.NodeId == historyEdge.SourceNodeId &&
                    value.Payload is CharacterPoseHistoryCollectorPayload);
                PoseNodeId scopedCollectorId = Scope(collectorNode.NodeId, scoped.Scope);
                if (!collectorIds.Add(scopedCollectorId))
                    throw new InvalidOperationException($"Pose History Collector '{scopedCollectorId}' has competing Motion Matching writers.");
                CharacterPoseBoundOperation collectorOperation = RequireOperation(
                    operations,
                    scopedCollectorId,
                    CharacterPoseOperationCode.PoseHistoryRead);
                int operationOutput = operation.OutputValueIndex;
                int collectorInput = collectorOperation.InputValueIndexA;
                int collectorOutput = collectorOperation.OutputValueIndex;
                if (collectorInput != operationOutput ||
                    collectorOperation.Index <= operation.Index)
                {
                    throw new InvalidOperationException($"Pose History Collector '{scopedCollectorId}' does not commit Motion Matching base Pose after node '{scoped.ScopedNodeId}'.");
                }
                var collectorPayload = (CharacterPoseHistoryCollectorPayload)collectorNode.Payload;
                int collectorIndex = collectors.Count;
                collectors.Add(new CharacterPoseHistoryCollectorPlanDescriptor(
                    scopedCollectorId,
                    collectorPayload.HistoryId,
                    collectorInput,
                    collectorOutput,
                    collectorIndex,
                    motionMatching.SearchPolicy.HistoryCapacity,
                    operation.Index,
                    collectorOperation.Index));

                PoseGraphId entryGraphId = payload.EntryGraph.PoseGraphId;
                if (!entryGraphIds.Add(entryGraphId))
                    throw new InvalidOperationException($"Motion Matching entry graph '{entryGraphId}' has more than one owner.");
                CharacterPoseCanvasGraph entryGraph = graphAsset.RequireGraph(entryGraphId);
                CharacterMotionMatchingEntryGraphPolicy.RequireValid(entryGraph);
                int entryProgramIndex = entryPrograms.Count;
                entryPrograms.Add(new CharacterMotionMatchingEntryProgramDescriptor(
                    entryGraphId,
                    0,
                    entryGraph.Nodes.Count(value => value != null &&
                        value.Kind != CharacterPoseNodeKind.EntryPoseInput &&
                        value.Kind != CharacterPoseNodeKind.GraphOutput),
                    CountEntryStateCapacity(entryGraph)));

                CharacterAnimationBlendPolicy blendPolicy = resources.BlendPolicy(
                    payload.JumpBlendPolicySlot,
                    scoped.ScopedNodeId.Value);
                CharacterAnimationBlendTransitionRule transition = blendPolicy.DefaultTransition;
                string curveKey = AnimationBlendCanonicalPayload.CurveKey(transition.CompileCurve());
                if (!curveIndices.TryGetValue(curveKey, out int curveIndex) ||
                    !profileIndicesByIdentity.TryGetValue(transition.BlendProfile.ProfileId, out int profileIndex))
                {
                    throw new InvalidOperationException($"Motion Matching Pose '{scoped.ScopedNodeId}' Jump Blend catalogs are incomplete.");
                }
                int blendPlanIndex = blends.Count;
                var stackPolicy = new AnimationBlendStackPolicyPayload(blendPolicy.StackPolicy);
                blends.Add(new CharacterMotionMatchingBlendPlanDescriptor(
                    blendPolicy.PolicyId,
                    blendPolicy.Revision,
                    stackPolicy,
                    transition.DurationSeconds,
                    curveIndex,
                    profileIndex));

                compiledNodes[nodeIndex] = new CharacterMotionMatchingPosePlanDescriptor(
                    scoped.ScopedNodeId,
                    binding.BindingId,
                    binding.BindingRevision,
                    motionMatching.ProfileId,
                    motionMatching.ProfileRevision,
                    binding.ChooserId,
                    binding.ChooserRevision,
                    binding.SearchDomainId,
                    binding.FirstDatabaseIndex,
                    binding.DatabaseCount,
                    collectorIndex,
                    entryProgramIndex,
                    blendPlanIndex,
                    operationOutput,
                    motionMatching.SearchPolicy.MaximumAdmittedSampleCount,
                    motionMatching.FeatureSchema.DenseFeatureCount,
                    checked(stackPolicy.MaxActiveSourceEntries + 1),
                    1,
                    motionMatching.SearchPolicy.DiagnosticDetailCapacity,
                    payload.RelevanceResetPolicy,
                    payload.SearchCadencePolicy);
            }
            return new CharacterMotionMatchingPosePlanCompilation(
                compiledNodes,
                collectors.ToArray(),
                entryPrograms.ToArray(),
                blends.ToArray());
        }

        internal static string ComputeProgramHash(
            string baseHash,
            CharacterMotionMatchingPosePlanCompilation plan)
        {
            if (string.IsNullOrWhiteSpace(baseHash))
                throw new ArgumentException(nameof(baseHash));
            if (plan == null)
                throw new ArgumentNullException(nameof(plan));
            var revision = new List<string>
            {
                baseHash,
                "motion-matching-pose-plan/v1"
            };
            for (int i = 0; i < plan.Nodes.Length; i++)
            {
                CharacterMotionMatchingPosePlanDescriptor node =
                    plan.Nodes[i];
                revision.Add(
                    $"node:{node.NodeId}:{node.BindingId}:{node.BindingRevision}:{node.ProfileId}:{node.ProfileRevision}:{node.ChooserId}:{node.ChooserRevision}:{node.SearchDomainId}:{node.FirstDatabaseIndex}:{node.DatabaseCount}:{node.CollectorIndex}:{node.EntryProgramIndex}:{node.BlendPlanIndex}:{node.OutputPoseValueIndex}:{node.CandidateCapacity}:{node.FeatureCapacity}:{node.LiveEntryCapacity}:{node.StoredPoseCapacity}:{node.DiagnosticCapacity}:{(int)node.RelevanceResetPolicy}:{(int)node.SearchCadencePolicy}");
            }
            for (int i = 0; i < plan.BlendPlans.Length; i++)
            {
                CharacterMotionMatchingBlendPlanDescriptor blend =
                    plan.BlendPlans[i];
                revision.Add(
                    $"blend:{blend.PolicyId}:{blend.PolicyRevision}:{blend.StackPolicy.MaxActiveSourceEntries}:{(int)blend.StackPolicy.StoredPosePolicy}:{blend.StackPolicy.MaxBlendInTimeToReplaceNewest:R}:{blend.StackPolicy.DepthBlendTimeMultiplier:R}:{blend.JumpDurationSeconds:R}:{blend.CurveIndex}:{blend.ProfileIndex}");
            }
            return ThirdPersonSimulation.StableHash
                .Compute(string.Join("|", revision))
                .ToString();
        }

        static IEnumerable<ScopedNode> Enumerate(
            CharacterPresentationPoseGraphAsset owner)
        {
            var result = new List<ScopedNode>();
            Collect(owner, owner.Graph, string.Empty, result);
            return result;
        }

        static void Collect(
            CharacterPresentationPoseGraphAsset owner,
            CharacterPoseCanvasGraph graph,
            string scope,
            ICollection<ScopedNode> result)
        {
            for (int i = 0; i < graph.Nodes.Count; i++)
            {
                CharacterPoseCanvasNode node = graph.Nodes[i];
                if (node == null)
                    continue;
                PoseNodeId scopedNodeId = Scope(node.NodeId, scope);
                result.Add(new ScopedNode(graph, node, scopedNodeId, scope));
                if (node.Payload is CharacterPoseStateMachineNodePayload machine && machine.StateMachine != null)
                {
                    for (int stateIndex = 0; stateIndex < machine.StateMachine.States.Count; stateIndex++)
                    {
                        CharacterPoseStateDefinition state = machine.StateMachine.States[stateIndex];
                        if (state?.PoseGraphId.IsValid != true)
                            continue;
                        Collect(
                            owner,
                            owner.RequireGraph(state.PoseGraphId),
                            scopedNodeId.Value + "/state/" + state.StateId.Value,
                            result);
                    }
                    continue;
                }
                if (node.Payload is CharacterPoseSubgraphPayload subgraph &&
                    subgraph.Subgraph?.PoseGraphId.IsValid == true)
                {
                    CharacterPoseCanvasGraph child = owner.RequireGraph(subgraph.Subgraph.PoseGraphId);
                    Collect(
                        owner,
                        child,
                        scopedNodeId.Value + "/" + child.GraphId.Value,
                        result);
                }
            }
        }

        static int CountEntryStateCapacity(CharacterPoseCanvasGraph graph) =>
            graph.Nodes.Count(value => value != null &&
                value.Kind != CharacterPoseNodeKind.EntryPoseInput &&
                value.Kind != CharacterPoseNodeKind.GraphOutput &&
                CharacterPoseNodeDefinitionModule.Shared.Require(value.Kind).ExecutionDomain != CharacterPoseExecutionDomain.PurePose);

        static PoseNodeId Scope(PoseNodeId nodeId, string scope) =>
            string.IsNullOrEmpty(scope)
                ? nodeId
                : new PoseNodeId(scope + "/" + nodeId.Value);

        static CharacterPoseBoundOperation RequireOperation(
            IReadOnlyDictionary<PoseNodeId, CharacterPoseBoundOperation>
                operations,
            PoseNodeId nodeId,
            CharacterPoseOperationCode code)
        {
            if (!operations.TryGetValue(
                    nodeId,
                    out CharacterPoseBoundOperation operation) ||
                operation.Code != code)
            {
                throw new InvalidOperationException($"Pose node '{nodeId}' has no compiled '{code}' operation.");
            }
            return operation;
        }

        static MotionMatchingNodeBindingPayload RequireBinding(
            MotionMatchingProjectionPayload payload,
            PoseNodeId nodeId)
        {
            MotionMatchingNodeBindingPayload result = default;
            int count = 0;
            for (int i = 0; i < payload.NodeBindingCount; i++)
            {
                MotionMatchingNodeBindingPayload candidate = payload.GetNodeBinding(i);
                if (candidate.PoseNodeId != nodeId)
                    continue;
                result = candidate;
                count++;
            }
            return count == 1
                ? result
                : throw new InvalidOperationException($"Motion Matching Pose '{nodeId}' does not resolve to one compiled binding.");
        }
    }
}
