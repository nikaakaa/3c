using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    public static class CharacterMotionMatchingPoseMutation
    {
        public static CharacterPresentationMutationTransaction CreatePose(
            string graphAssetId,
            PoseGraphId stateGraphId,
            PoseNodeId nodeId,
            PoseGraphId entryGraphId,
            CharacterPoseResourceSlot bindingSlot,
            CharacterPoseResourceSlot jumpBlendPolicySlot,
            CharacterMotionMatchingRelevanceResetPolicy relevanceResetPolicy,
            CharacterMotionMatchingSearchCadencePolicy searchCadencePolicy,
            Vector2 position)
        {
            RequireGraphIds(stateGraphId, entryGraphId);
            var transaction = new CharacterPresentationMutationTransaction(
                $"create-motion-matching-pose/{nodeId.Value}",
                "Create Motion Matching Pose");
            transaction.Add(new CreatePoseGraphMutation(
                graphAssetId,
                CreateIdentityEntryGraph(entryGraphId)));
            transaction.Add(new CreatePoseNodeMutation(
                stateGraphId.Value,
                new CharacterPoseCanvasNode(
                    nodeId,
                    "Motion Matching Pose",
                    new CharacterMotionMatchingPosePayload(
                        bindingSlot,
                        jumpBlendPolicySlot,
                        entryGraphId,
                        relevanceResetPolicy,
                        searchCadencePolicy)),
                position));
            return transaction;
        }

        public static CharacterPresentationMutationTransaction ConfigurePose(
            PoseGraphId stateGraphId,
            PoseNodeId nodeId,
            CharacterPoseResourceSlot bindingSlot,
            CharacterPoseResourceSlot jumpBlendPolicySlot,
            CharacterMotionMatchingRelevanceResetPolicy relevanceResetPolicy,
            CharacterMotionMatchingSearchCadencePolicy searchCadencePolicy)
        {
            var transaction = new CharacterPresentationMutationTransaction(
                $"configure-motion-matching-pose/{nodeId.Value}",
                "Configure Motion Matching Pose");
            transaction.Add(new SetPoseNodeFieldMutation(stateGraphId.Value, nodeId, "binding", bindingSlot));
            transaction.Add(new SetPoseNodeFieldMutation(stateGraphId.Value, nodeId, "jump-blend-policy", jumpBlendPolicySlot));
            transaction.Add(new SetPoseNodeFieldMutation(stateGraphId.Value, nodeId, "relevance-reset-policy", relevanceResetPolicy));
            transaction.Add(new SetPoseNodeFieldMutation(stateGraphId.Value, nodeId, "search-cadence-policy", searchCadencePolicy));
            return transaction;
        }

        public static CharacterPresentationMutationTransaction DeletePose(
            string graphAssetId,
            PoseGraphId stateGraphId,
            PoseNodeId nodeId,
            PoseGraphId entryGraphId,
            int entryGraphReferenceCount)
        {
            if (entryGraphReferenceCount != 1)
                throw new InvalidOperationException($"Motion Matching entry graph '{entryGraphId}' has {entryGraphReferenceCount} owners.");
            var transaction = new CharacterPresentationMutationTransaction(
                $"delete-motion-matching-pose/{nodeId.Value}",
                "Delete Motion Matching Pose");
            transaction.Add(new DeletePoseGraphMutation(graphAssetId, entryGraphId));
            transaction.Add(new DeletePoseNodeMutation(stateGraphId.Value, nodeId));
            return transaction;
        }

        public static CharacterPresentationMutationTransaction DuplicatePose(
            string graphAssetId,
            PoseGraphId stateGraphId,
            CharacterPoseCanvasNode sourceNode,
            CharacterPoseCanvasGraph sourceEntryGraph,
            PoseNodeId targetNodeId,
            PoseGraphId targetEntryGraphId,
            Vector2 position)
        {
            CharacterMotionMatchingPosePayload source = sourceNode?.Payload as CharacterMotionMatchingPosePayload ??
                throw new ArgumentException("Source node is not a Motion Matching Pose.", nameof(sourceNode));
            if (sourceEntryGraph == null || source.EntryGraph == null || source.EntryGraph.PoseGraphId != sourceEntryGraph.GraphId)
                throw new InvalidOperationException("Motion Matching source node and entry graph identity do not match.");
            CharacterPoseCanvasGraph clone = CloneEntryGraph(sourceEntryGraph, targetEntryGraphId);
            var transaction = new CharacterPresentationMutationTransaction(
                $"duplicate-motion-matching-pose/{targetNodeId.Value}",
                "Duplicate Motion Matching Pose");
            transaction.Add(new CreatePoseGraphMutation(graphAssetId, clone));
            transaction.Add(new CreatePoseNodeMutation(
                stateGraphId.Value,
                new CharacterPoseCanvasNode(
                    targetNodeId,
                    sourceNode.DisplayName,
                    new CharacterMotionMatchingPosePayload(
                        source.BindingSlot,
                        source.JumpBlendPolicySlot,
                        targetEntryGraphId,
                        source.RelevanceResetPolicy,
                        source.SearchCadencePolicy),
                    sourceNode.DynamicPorts.ToArray()),
                position));
            return transaction;
        }

        static CharacterPoseCanvasGraph CreateIdentityEntryGraph(PoseGraphId graphId)
        {
            PoseNodeId inputId = new PoseNodeId(graphId.Value + "/entry-pose-input");
            PoseNodeId outputId = new PoseNodeId(graphId.Value + "/graph-output");
            var outputPort = new CharacterPoseDynamicPort(
                CharacterMotionMatchingPosePorts.LocalPoseOutput,
                "Local Pose",
                CharacterPosePortKind.LocalPose,
                CharacterPosePortDirection.Input,
                true,
                0,
                new PoseInterfacePortId("entry.pose"));
            return CharacterPoseCanvasGraph.CreateAuthoring(
                graphId,
                graphId.Value + ".identity.v1",
                Array.Empty<CharacterPoseParameterDeclaration>(),
                new[]
                {
                    new CharacterPoseCanvasNode(inputId, "Entry Pose Input", new CharacterEntryPoseInputPayload()),
                    new CharacterPoseCanvasNode(outputId, "Graph Output", new CharacterGraphOutputPosePayload(), new[] { outputPort })
                },
                new[]
                {
                    new CharacterPoseCanvasConnection(
                        graphId.Value + "/identity-edge",
                        inputId,
                        CharacterMotionMatchingPosePorts.LocalPoseOutput,
                        outputId,
                        CharacterMotionMatchingPosePorts.LocalPoseOutput)
                },
                new[]
                {
                    new CharacterPoseGraphLayoutEntry(inputId, new Vector2(-180f, 0f)),
                    new CharacterPoseGraphLayoutEntry(outputId, new Vector2(180f, 0f))
                });
        }

        static CharacterPoseCanvasGraph CloneEntryGraph(CharacterPoseCanvasGraph source, PoseGraphId targetGraphId)
        {
            var nodeMap = new Dictionary<PoseNodeId, PoseNodeId>();
            CharacterPoseCanvasNode[] nodes = source.Nodes.Select((node, index) =>
            {
                PoseNodeId targetId = new PoseNodeId(targetGraphId.Value + "/node-" + index);
                nodeMap.Add(node.NodeId, targetId);
                return new CharacterPoseCanvasNode(
                    targetId,
                    node.DisplayName,
                    node.Payload,
                    node.DynamicPorts.ToArray());
            }).ToArray();
            CharacterPoseCanvasConnection[] edges = source.Edges.Select((edge, index) =>
                new CharacterPoseCanvasConnection(
                    targetGraphId.Value + "/edge-" + index,
                    nodeMap[edge.SourceNodeId],
                    edge.SourcePortId,
                    nodeMap[edge.TargetNodeId],
                    edge.TargetPortId)).ToArray();
            CharacterPoseGraphLayoutEntry[] layout = source.Layout.Select(value =>
                new CharacterPoseGraphLayoutEntry(nodeMap[value.NodeId], value.Position)).ToArray();
            return CharacterPoseCanvasGraph.CreateAuthoring(
                targetGraphId,
                targetGraphId.Value + ".copy.v1",
                source.Parameters.ToArray(),
                nodes,
                edges,
                layout);
        }

        static void RequireGraphIds(PoseGraphId stateGraphId, PoseGraphId entryGraphId)
        {
            if (!stateGraphId.IsValid || !entryGraphId.IsValid || stateGraphId == entryGraphId)
                throw new ArgumentException("Motion Matching state and entry graph identities are invalid.");
        }
    }

    internal static class CharacterMotionMatchingPoseFieldMutation
    {
        internal static CharacterPoseNodePayload Set(CharacterMotionMatchingPosePayload current, string field, object value) =>
            new CharacterMotionMatchingPosePayload(
                field == "binding" ? Require<CharacterPoseResourceSlot>(value, field) : current.BindingSlot,
                field == "jump-blend-policy" ? Require<CharacterPoseResourceSlot>(value, field) : current.JumpBlendPolicySlot,
                field == "entry-graph-id" ? new PoseGraphId(Convert.ToString(value)) : current.EntryGraph.PoseGraphId,
                field == "relevance-reset-policy" ? EnumValue<CharacterMotionMatchingRelevanceResetPolicy>(value) : current.RelevanceResetPolicy,
                field == "search-cadence-policy" ? EnumValue<CharacterMotionMatchingSearchCadencePolicy>(value) : current.SearchCadencePolicy);

        static T Require<T>(object value, string field) where T : UnityEngine.Object =>
            value is T typed
                ? typed
                : throw new InvalidOperationException($"Pose field '{field}' requires '{typeof(T).Name}'.");

        static T EnumValue<T>(object value) where T : struct =>
            value is T typed ? typed : Enum.Parse<T>(Convert.ToString(value), false);
    }
}
