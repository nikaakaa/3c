using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;

namespace ThirdPersonCharacter.Editor.CharacterSimulation
{
    internal static class CharacterMotionMatchingPosePlanCompiler
    {
        readonly struct ScopedNode
        {
            internal ScopedNode(
                CharacterTypedPoseGraph graph,
                CharacterTypedPoseNode node,
                PoseNodeId scopedNodeId,
                string scope)
            {
                Graph = graph;
                Node = node;
                ScopedNodeId = scopedNodeId;
                Scope = scope ?? string.Empty;
            }

            internal CharacterTypedPoseGraph Graph { get; }
            internal CharacterTypedPoseNode Node { get; }
            internal PoseNodeId ScopedNodeId { get; }
            internal string Scope { get; }
        }

        internal static void Compile(
            CharacterPresentationPosePlan plan,
            CharacterPresentationPoseGraphAsset graphAsset,
            CharacterAnimationRigDefinition rig,
            MotionMatchingProjectionPayload motionMatching,
            IReadOnlyDictionary<string, int> curveIndices,
            IReadOnlyDictionary<string, int> profileIndicesByIdentity)
        {
            if (plan == null || !graphAsset || !rig)
                throw new ArgumentException("Motion Matching Pose plan compilation input is incomplete.");
            ScopedNode[] nodes = Enumerate(graphAsset)
                .Where(value => value.Node.Payload is CharacterMotionMatchingPosePayload)
                .OrderBy(value => value.ScopedNodeId)
                .ToArray();
            if (nodes.Length == 0)
            {
                plan.ConfigureMotionMatching(
                    Array.Empty<CharacterMotionMatchingPosePlanDescriptor>(),
                    Array.Empty<CharacterPoseHistoryCollectorPlanDescriptor>(),
                    Array.Empty<CharacterMotionMatchingEntryProgramDescriptor>(),
                    Array.Empty<CharacterMotionMatchingBlendPlanDescriptor>());
                return;
            }
            if (motionMatching == null || curveIndices == null || profileIndicesByIdentity == null)
                throw new InvalidOperationException("Motion Matching Pose nodes require compiled Projection and Blend catalogs.");
            if (!string.Equals(plan.RigId, rig.RigId, StringComparison.Ordinal) ||
                !string.Equals(plan.RigRevision, rig.Revision, StringComparison.Ordinal) ||
                motionMatching.NodeBindingCount != nodes.Length)
            {
                throw new InvalidOperationException("Motion Matching Pose plan Rig or node binding closure is inconsistent.");
            }

            var operations = plan.Operations.ToDictionary(value => value.NodeId);
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
                payload.RequireValid(rig);
                MotionMatchingNodeBindingPayload binding = RequireBinding(
                    motionMatching,
                    scoped.ScopedNodeId);
                CharacterPresentationPoseOperation operation = RequireOperation(
                    operations,
                    scoped.ScopedNodeId,
                    CharacterPoseOperationCode.MotionMatchingPose);
                CharacterPoseEdge historyEdge = scoped.Graph.Edges.Single(edge =>
                    edge != null && edge.TargetNodeId == scoped.Node.NodeId &&
                    CharacterMotionMatchingPosePorts.History.Equals(edge.TargetPortId));
                CharacterTypedPoseNode collectorNode = scoped.Graph.Nodes.Single(value =>
                    value != null && value.NodeId == historyEdge.SourceNodeId &&
                    value.Payload is CharacterPoseHistoryCollectorPayload);
                PoseNodeId scopedCollectorId = Scope(collectorNode.NodeId, scoped.Scope);
                if (!collectorIds.Add(scopedCollectorId))
                    throw new InvalidOperationException($"Pose History Collector '{scopedCollectorId}' has competing Motion Matching writers.");
                CharacterPresentationPoseOperation collectorOperation = RequireOperation(
                    operations,
                    scopedCollectorId,
                    CharacterPoseOperationCode.PoseHistoryRead);
                if (collectorOperation.InputValueIndexA != operation.OutputValueIndex ||
                    collectorOperation.Index <= operation.Index)
                {
                    throw new InvalidOperationException($"Pose History Collector '{scopedCollectorId}' does not commit Motion Matching base Pose after node '{scoped.ScopedNodeId}'.");
                }
                var collectorPayload = (CharacterPoseHistoryCollectorPayload)collectorNode.Payload;
                int collectorIndex = collectors.Count;
                collectors.Add(new CharacterPoseHistoryCollectorPlanDescriptor(
                    scopedCollectorId,
                    collectorPayload.HistoryId,
                    collectorOperation.InputValueIndexA,
                    collectorOperation.OutputValueIndex,
                    collectorIndex,
                    motionMatching.SearchPolicy.HistoryCapacity,
                    operation.Index,
                    collectorOperation.Index));

                PoseGraphId entryGraphId = payload.EntryGraph.PoseGraphId;
                if (!entryGraphIds.Add(entryGraphId))
                    throw new InvalidOperationException($"Motion Matching entry graph '{entryGraphId}' has more than one owner.");
                CharacterTypedPoseGraph entryGraph = graphAsset.RequireGraph(entryGraphId);
                CharacterMotionMatchingEntryGraphPolicy.RequireValid(entryGraph);
                int entryProgramIndex = entryPrograms.Count;
                entryPrograms.Add(new CharacterMotionMatchingEntryProgramDescriptor(
                    entryGraphId,
                    0,
                    entryGraph.Nodes.Count(value => value != null &&
                        value.Kind != CharacterPoseNodeKind.EntryPoseInput &&
                        value.Kind != CharacterPoseNodeKind.GraphOutput),
                    CountEntryStateCapacity(entryGraph)));

                CharacterAnimationBlendPolicy blendPolicy = payload.JumpBlendPolicy;
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
                    operation.OutputValueIndex,
                    motionMatching.SearchPolicy.MaximumAdmittedSampleCount,
                    motionMatching.FeatureSchema.DenseFeatureCount,
                    checked(stackPolicy.MaxActiveSourceEntries + 1),
                    1,
                    motionMatching.SearchPolicy.DiagnosticDetailCapacity,
                    payload.RelevanceResetPolicy,
                    payload.SearchCadencePolicy);
            }
            plan.ConfigureMotionMatching(
                compiledNodes,
                collectors.ToArray(),
                entryPrograms.ToArray(),
                blends.ToArray());
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
            CharacterTypedPoseGraph graph,
            string scope,
            ICollection<ScopedNode> result)
        {
            for (int i = 0; i < graph.Nodes.Count; i++)
            {
                CharacterTypedPoseNode node = graph.Nodes[i];
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
                    CharacterTypedPoseGraph child = owner.RequireGraph(subgraph.Subgraph.PoseGraphId);
                    Collect(
                        owner,
                        child,
                        scopedNodeId.Value + "/" + child.GraphId.Value,
                        result);
                }
            }
        }

        static int CountEntryStateCapacity(CharacterTypedPoseGraph graph) =>
            graph.Nodes.Count(value => value != null &&
                value.Kind != CharacterPoseNodeKind.EntryPoseInput &&
                value.Kind != CharacterPoseNodeKind.GraphOutput &&
                CharacterPoseCompilerHandlerRegistry.Shared.Require(value.Kind).ExecutionDomain != CharacterPoseExecutionDomain.PurePose);

        static PoseNodeId Scope(PoseNodeId nodeId, string scope) =>
            string.IsNullOrEmpty(scope)
                ? nodeId
                : new PoseNodeId(scope + "/" + nodeId.Value);

        static CharacterPresentationPoseOperation RequireOperation(
            IReadOnlyDictionary<PoseNodeId, CharacterPresentationPoseOperation> operations,
            PoseNodeId nodeId,
            CharacterPoseOperationCode code)
        {
            if (!operations.TryGetValue(nodeId, out CharacterPresentationPoseOperation operation) ||
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
