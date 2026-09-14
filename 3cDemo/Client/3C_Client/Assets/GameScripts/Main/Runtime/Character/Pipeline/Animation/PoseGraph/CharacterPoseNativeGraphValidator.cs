using System;
using System.Collections.Generic;
using System.Linq;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal sealed class CharacterPoseNativeGraphValidationException : InvalidOperationException
    {
        internal CharacterPoseNativeGraphValidationException(
            CharacterPoseNativeFailureCode code,
            string source,
            string message)
            : base(message)
        {
            Code = code;
            Origin = source;
        }

        internal CharacterPoseNativeFailureCode Code { get; }
        internal string Origin { get; }
    }

    internal static class CharacterPoseNativeGraphValidator
    {
        enum BoundaryKind : byte
        {
            Root = 1,
            State = 2,
            Boundary = 3
        }

        internal static void RequireValid(
            CharacterPresentationPoseGraphAsset graphAsset,
            CharacterPoseCanvasGraph root)
        {
            if (!graphAsset || root == null)
                Fail(CharacterPoseNativeFailureCode.GraphMissing, "Pose", "Pose graph asset or root graph is missing.");
            var visiting = new HashSet<PoseGraphId>();
            var visited = new Dictionary<PoseGraphId, BoundaryKind>();
            ValidateGraph(graphAsset, root, BoundaryKind.Root, visiting, visited);
        }

        static void ValidateGraph(
            CharacterPresentationPoseGraphAsset graphAsset,
            CharacterPoseCanvasGraph graph,
            BoundaryKind boundary,
            ISet<PoseGraphId> visiting,
            IDictionary<PoseGraphId, BoundaryKind> visited)
        {
            if (!graph.GraphId.IsValid)
                Fail(CharacterPoseNativeFailureCode.GraphInvalid, "Pose Graph", "Pose graph identity is invalid.");
            if (visited.TryGetValue(graph.GraphId, out BoundaryKind previousBoundary))
            {
                if (previousBoundary != boundary)
                {
                    Fail(
                        CharacterPoseNativeFailureCode.GraphInvalid,
                        graph.GraphId.Value,
                        $"Pose graph '{graph.GraphId}' is reused with conflicting boundaries '{previousBoundary}' and '{boundary}'.");
                }
                return;
            }
            if (!visiting.Add(graph.GraphId))
            {
                Fail(
                    CharacterPoseNativeFailureCode.Cycle,
                    graph.GraphId.Value,
                    $"Pose graph reference cycle contains '{graph.GraphId}'.");
            }

            try
            {
                graph.RequireValid();
                CharacterPoseCanvasNode[] nodes = graph.Nodes.ToArray();
                var byId = nodes.ToDictionary(node => node.NodeId);
                var shapes = new Dictionary<PoseNodeId, IReadOnlyList<CharacterPosePortDefinition>>();
                for (int i = 0; i < nodes.Length; i++)
                {
                    CharacterPoseCanvasNode node = nodes[i];
                    IReadOnlyList<CharacterPosePortDefinition> shape =
                        CharacterPoseCanvasNativePorts.GetRuntimeShape(node);
                    bool interfaceBoundary =
                        node.Kind == CharacterPoseNodeKind.GraphInput ||
                        node.Kind == CharacterPoseNodeKind.GraphOutput ||
                        node.Kind == CharacterPoseNodeKind.PoseSubgraph ||
                        node.Kind == CharacterPoseNodeKind.LinkedPoseCall;
                    var ports = new HashSet<PosePortId>();
                    var interfacePorts = new HashSet<PoseInterfacePortId>();
                    for (int portIndex = 0; portIndex < shape.Count; portIndex++)
                    {
                        CharacterPosePortDefinition port = shape[portIndex];
                        if (!port.PortId.IsValid || !ports.Add(port.PortId) ||
                            !Enum.IsDefined(typeof(CharacterPosePortKind), port.Kind) ||
                            !Enum.IsDefined(typeof(CharacterPosePortDirection), port.Direction))
                        {
                            Fail(
                                CharacterPoseNativeFailureCode.PortInvalid,
                                $"{graph.GraphId}/{node.NodeId}",
                                $"Pose node '{node.NodeId}' has an invalid or duplicate port.");
                        }
                        if (interfaceBoundary && port.InterfacePortId.IsValid &&
                            !interfacePorts.Add(port.InterfacePortId))
                        {
                            Fail(
                                CharacterPoseNativeFailureCode.PortInvalid,
                                $"{graph.GraphId}/{node.NodeId}/{port.PortId}",
                                $"Pose node '{node.NodeId}' has a duplicate interface port identity.");
                        }
                        if (interfaceBoundary != port.InterfacePortId.IsValid)
                        {
                            Fail(
                                CharacterPoseNativeFailureCode.PortInvalid,
                                $"{graph.GraphId}/{node.NodeId}/{port.PortId}",
                                $"Pose node '{node.NodeId}' has an invalid interface port identity.");
                        }
                    }
                    shapes.Add(node.NodeId, shape);
                }

                var targetConnections = new HashSet<string>(StringComparer.Ordinal);
                var indegree = nodes.ToDictionary(node => node.NodeId, _ => 0);
                var outgoing = nodes.ToDictionary(
                    node => node.NodeId,
                    _ => new HashSet<PoseNodeId>());
                foreach (CharacterPoseCanvasConnection connection in graph.Connections)
                {
                    CharacterPoseCanvasNode source = null;
                    CharacterPoseCanvasNode target = null;
                    IReadOnlyList<CharacterPosePortDefinition> sourceShape = null;
                    IReadOnlyList<CharacterPosePortDefinition> targetShape = null;
                    if (!byId.TryGetValue(connection.SourceNodeId, out source) ||
                        !byId.TryGetValue(connection.TargetNodeId, out target) ||
                        !shapes.TryGetValue(source.NodeId, out sourceShape) ||
                        !shapes.TryGetValue(target.NodeId, out targetShape))
                    {
                        throw new CharacterPoseNativeGraphValidationException(
                            CharacterPoseNativeFailureCode.PortInvalid,
                            $"{graph.GraphId}/{connection.EdgeId}",
                            "Pose connection references an unknown node.");
                    }
                    CharacterPosePortDefinition sourcePort = RequirePort(
                        sourceShape,
                        connection.SourcePortId,
                        CharacterPosePortDirection.Output,
                        graph.GraphId,
                        connection.EdgeId);
                    CharacterPosePortDefinition targetPort = RequirePort(
                        targetShape,
                        connection.TargetPortId,
                        CharacterPosePortDirection.Input,
                        graph.GraphId,
                        connection.EdgeId);
                    if (CharacterPoseCanvasNativePorts.RuntimeBindingType(sourcePort.Kind) !=
                        CharacterPoseCanvasNativePorts.RuntimeBindingType(targetPort.Kind))
                    {
                        Fail(
                            CharacterPoseNativeFailureCode.PortInvalid,
                            $"{graph.GraphId}/{connection.EdgeId}",
                            $"Pose connection '{connection.EdgeId}' has incompatible typed ports.");
                    }
                    string targetKey = target.NodeId.Value + "/" + targetPort.PortId.Value;
                    if (!targetConnections.Add(targetKey))
                    {
                        Fail(
                            CharacterPoseNativeFailureCode.PortInvalid,
                            $"{graph.GraphId}/{targetKey}",
                            "Pose input port has more than one connection.");
                    }
                    if (sourcePort.Kind != CharacterPosePortKind.PoseHistory)
                    {
                        if (outgoing[source.NodeId].Add(target.NodeId))
                            indegree[target.NodeId]++;
                    }
                }

                foreach (CharacterPoseCanvasNode node in nodes)
                {
                    foreach (CharacterPosePortDefinition port in shapes[node.NodeId])
                    {
                        if (port.Direction != CharacterPosePortDirection.Input ||
                            !port.Required)
                            continue;
                        string targetKey = node.NodeId.Value + "/" + port.PortId.Value;
                        if (!targetConnections.Contains(targetKey))
                        {
                            Fail(
                                CharacterPoseNativeFailureCode.PortInvalid,
                                $"{graph.GraphId}/{targetKey}",
                                $"Pose required input '{targetKey}' is not connected.");
                        }
                    }
                }

                EnsureAcyclic(graph, nodes, indegree, outgoing);
                ValidateBoundary(graph, nodes, boundary);
                ValidateReferencedGraphs(graphAsset, graph, nodes, visiting, visited);
            }
            catch (CharacterPoseNativeGraphValidationException)
            {
                throw;
            }
            catch (Exception exception)
            {
                Fail(
                    CharacterPoseNativeFailureCode.GraphInvalid,
                    graph.GraphId.Value,
                    exception.Message);
            }
            finally
            {
                visiting.Remove(graph.GraphId);
            }
            visited.Add(graph.GraphId, boundary);
        }

        static CharacterPosePortDefinition RequirePort(
            IReadOnlyList<CharacterPosePortDefinition> shape,
            PosePortId portId,
            CharacterPosePortDirection direction,
            PoseGraphId graphId,
            string edgeId)
        {
            CharacterPosePortDefinition port = shape.SingleOrDefault(value =>
                value.PortId.Equals(portId) && value.Direction == direction);
            return port ?? throw new CharacterPoseNativeGraphValidationException(
                CharacterPoseNativeFailureCode.PortInvalid,
                $"{graphId}/{edgeId}/{portId}",
                $"Pose connection '{edgeId}' references missing {direction} port '{portId}'.");
        }

        static void EnsureAcyclic(
            CharacterPoseCanvasGraph graph,
            IReadOnlyList<CharacterPoseCanvasNode> nodes,
            IDictionary<PoseNodeId, int> indegree,
            IReadOnlyDictionary<PoseNodeId, HashSet<PoseNodeId>> outgoing)
        {
            var ready = new SortedSet<PoseNodeId>(
                indegree.Where(value => value.Value == 0).Select(value => value.Key));
            int count = 0;
            while (ready.Count != 0)
            {
                PoseNodeId current = ready.Min;
                ready.Remove(current);
                count++;
                foreach (PoseNodeId target in outgoing[current].OrderBy(value => value))
                {
                    int next = indegree[target] - 1;
                    indegree[target] = next;
                    if (next == 0)
                        ready.Add(target);
                }
            }
            if (count != nodes.Count)
            {
                Fail(
                    CharacterPoseNativeFailureCode.Cycle,
                    graph.GraphId.Value,
                    $"Pose graph '{graph.GraphId}' contains an executable cycle.");
            }
        }

        static void ValidateBoundary(
            CharacterPoseCanvasGraph graph,
            IReadOnlyList<CharacterPoseCanvasNode> nodes,
            BoundaryKind boundary)
        {
            int outputCount = nodes.Count(value => value.Kind == CharacterPoseNodeKind.OutputPose);
            int graphInputCount = nodes.Count(value =>
                value.Kind == CharacterPoseNodeKind.GraphInput ||
                value.Kind == CharacterPoseNodeKind.EntryPoseInput);
            int graphOutputCount = nodes.Count(value => value.Kind == CharacterPoseNodeKind.GraphOutput);
            bool graphBoundary = boundary == BoundaryKind.Boundary;
            bool rootOrState = boundary == BoundaryKind.Root || boundary == BoundaryKind.State;
            if (rootOrState &&
                (outputCount != 1 || graphInputCount != 0 || graphOutputCount != 0) ||
                graphBoundary &&
                (outputCount != 0 || graphInputCount != 1 || graphOutputCount != 1))
            {
                Fail(
                    CharacterPoseNativeFailureCode.GraphInvalid,
                    graph.GraphId.Value,
                    graphBoundary
                        ? "Pose boundary graph must contain exactly one Graph Input, one Graph Output and no Output Pose."
                        : "Pose root or state graph must contain exactly one Output Pose and no Graph Input or Graph Output.");
            }
            if (graphBoundary)
            {
                CharacterPoseCanvasNode graphOutput = nodes.Single(value =>
                    value.Kind == CharacterPoseNodeKind.GraphOutput);
                int outputPortCount = CharacterPoseCanvasNativePorts
                    .GetRuntimeShape(graphOutput)
                    .Count(value =>
                        value.Direction == CharacterPosePortDirection.Input);
                if (outputPortCount == 0)
                {
                    Fail(
                        CharacterPoseNativeFailureCode.PortInvalid,
                        graph.GraphId.Value,
                        "Pose boundary graph Graph Output must declare at least one input port.");
                }
            }
        }

        static void ValidateReferencedGraphs(
            CharacterPresentationPoseGraphAsset graphAsset,
            CharacterPoseCanvasGraph graph,
            IReadOnlyList<CharacterPoseCanvasNode> nodes,
            ISet<PoseGraphId> visiting,
            IDictionary<PoseGraphId, BoundaryKind> visited)
        {
            foreach (CharacterPoseCanvasNode node in nodes)
            {
                if (node.Payload is CharacterPoseStateMachineNodePayload stateMachine)
                {
                    CharacterPoseStateMachineAuthoringValidator.RequireValid(
                        stateMachine.StateMachine,
                        graphAsset.RequireGraph);
                    foreach (CharacterPoseStateDefinition state in stateMachine.StateMachine.States)
                        ValidateGraph(
                            graphAsset,
                            graphAsset.RequireGraph(state.PoseGraphId),
                            BoundaryKind.State,
                            visiting,
                            visited);
                }
                else if (node.Payload is CharacterPoseSubgraphPayload subgraph)
                {
                    CharacterPoseCanvasGraph child =
                        graphAsset.RequireGraph(subgraph.Subgraph.PoseGraphId);
                    CharacterPoseSubgraphSignatureValidator.RequireMatch(
                        node,
                        child);
                    ValidateGraph(
                        graphAsset,
                        child,
                        BoundaryKind.Boundary,
                        visiting,
                        visited);
                }
                else if (node.Payload is CharacterMotionMatchingPosePayload motionMatching)
                {
                    CharacterPoseCanvasGraph entryGraph =
                        graphAsset.RequireGraph(motionMatching.EntryGraph.PoseGraphId);
                    ValidateMotionMatchingEntryBoundary(entryGraph);
                    ValidateGraph(
                        graphAsset,
                        entryGraph,
                        BoundaryKind.Boundary,
                        visiting,
                        visited);
                }
            }
        }

        static void ValidateMotionMatchingEntryBoundary(
            CharacterPoseCanvasGraph graph)
        {
            int entryCount = graph.Nodes.Count(value =>
                value.Kind == CharacterPoseNodeKind.EntryPoseInput);
            if (entryCount != 1)
            {
                Fail(
                    CharacterPoseNativeFailureCode.GraphInvalid,
                    graph.GraphId.Value,
                    "Motion Matching entry graph must contain exactly one Entry Pose Input.");
            }
        }

        static void Fail(
            CharacterPoseNativeFailureCode code,
            string source,
            string message) =>
            throw new CharacterPoseNativeGraphValidationException(code, source, message);
    }
}
