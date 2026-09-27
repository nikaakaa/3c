using BTSMTL.Authoring.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.Editor.CharacterSimulation;
using ThirdPersonCharacter.Pipeline.Animation;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    internal static class CharacterPoseCanvasMutationPreflight
    {
        internal static void RequireValid(
            PoseGraphId graphId,
            IReadOnlyList<CharacterPoseCanvasNode> nodes,
            IReadOnlyList<CharacterPoseCanvasConnection> edges)
        {
            if (!graphId.IsValid)
                throw new InvalidOperationException(
                    "Pose Canvas mutation requires a valid Graph identity.");
            CharacterPoseCanvasNode[] nodeValues = (nodes ??
                throw new ArgumentNullException(nameof(nodes))).ToArray();
            CharacterPoseCanvasConnection[] edgeValues = (edges ??
                throw new ArgumentNullException(nameof(edges))).ToArray();
            var nodeById = new Dictionary<PoseNodeId, CharacterPoseCanvasNode>();
            var portsByNode = new Dictionary<PoseNodeId,
                IReadOnlyDictionary<PosePortId, CharacterPosePortDefinition>>();
            foreach (CharacterPoseCanvasNode node in nodeValues)
            {
                if (node == null || !node.NodeId.IsValid ||
                    !nodeById.TryAdd(node.NodeId, node))
                    throw new InvalidOperationException(
                        $"Pose Canvas graph '{graphId}' contains a missing or duplicate Node identity.");
                CharacterPoseNodeDefinition definition =
                    CharacterPoseNodeDefinitionModule.Shared.Require(node.Kind);
                definition.RequirePayload(node.Payload);
                portsByNode.Add(
                    node.NodeId,
                    definition.ProjectPortShape(node)
                        .Select(value => new CharacterPosePortDefinition(
                            new PosePortId(value.PortId.Value),
                            value.DisplayName,
                            CharacterPoseAuthoringPortProjection.Kind(value.ValueTypeId),
                            value.Direction == GraphAuthoringPortDirection.Input
                                ? CharacterPosePortDirection.Input
                                : CharacterPosePortDirection.Output,
                            value.Required,
                            string.IsNullOrWhiteSpace(value.InterfacePortId)
                                ? default
                                : new PoseInterfacePortId(value.InterfacePortId)))
                        .ToDictionary(value => value.PortId));
            }
            var edgeIds = new HashSet<string>(StringComparer.Ordinal);
            var targetPorts = new HashSet<string>(StringComparer.Ordinal);
            foreach (CharacterPoseCanvasConnection edge in edgeValues)
            {
                if (edge == null || string.IsNullOrWhiteSpace(edge.EdgeId) ||
                    !edgeIds.Add(edge.EdgeId) ||
                    !nodeById.TryGetValue(edge.SourceNodeId, out _) ||
                    !nodeById.TryGetValue(edge.TargetNodeId, out _) ||
                    !portsByNode[edge.SourceNodeId].TryGetValue(
                        edge.SourcePortId,
                        out CharacterPosePortDefinition sourcePort) ||
                    !portsByNode[edge.TargetNodeId].TryGetValue(
                        edge.TargetPortId,
                        out CharacterPosePortDefinition targetPort) ||
                    sourcePort.Direction != CharacterPosePortDirection.Output ||
                    targetPort.Direction != CharacterPosePortDirection.Input ||
                    sourcePort.Kind != targetPort.Kind ||
                    !targetPorts.Add(edge.TargetNodeId.Value + "\0" + edge.TargetPortId.Value))
                {
                    throw new InvalidOperationException(
                        $"Pose Canvas graph '{graphId}' contains an invalid or conflicting Edge.");
                }
            }
        }

        internal static void RequireValid(CharacterPoseCanvasGraph graph)
        {
            if (!graph)
                throw new ArgumentNullException(nameof(graph));
            RequireValid(graph.GraphId, graph.Nodes, graph.Edges);
        }
    }
}
