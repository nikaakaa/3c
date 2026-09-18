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
        internal static void RequireValid(
            CharacterPresentationPoseGraphAsset graphAsset,
            CharacterPoseCanvasGraph root,
            CharacterPoseNativeGraphBoundary boundary)
        {
            if (!graphAsset || root == null)
                Fail(CharacterPoseNativeFailureCode.GraphMissing, "Pose", "Pose graph asset or root graph is missing.");
            var visiting = new HashSet<PoseGraphId>();
            var visited = new Dictionary<PoseGraphId, CharacterPoseNativeGraphBoundary>();
            ValidateGraph(graphAsset, root, boundary, visiting, visited);
        }

        static void ValidateGraph(
            CharacterPresentationPoseGraphAsset graphAsset,
            CharacterPoseCanvasGraph graph,
            CharacterPoseNativeGraphBoundary boundary,
            ISet<PoseGraphId> visiting,
            IDictionary<PoseGraphId, CharacterPoseNativeGraphBoundary> visited)
        {
            if (!graph.GraphId.IsValid)
                Fail(CharacterPoseNativeFailureCode.GraphInvalid, "Pose Graph", "Pose graph identity is invalid.");
            if (visited.TryGetValue(graph.GraphId, out CharacterPoseNativeGraphBoundary previousBoundary))
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
                ValidateBoundary(
                    graph,
                    nodes,
                    graph.Connections,
                    boundary);
                ValidateNativeTopology(graph, nodes, graph.Connections, boundary);
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
                    $"{exception.GetType().Name}: {exception.Message}");
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
            IReadOnlyList<CharacterPoseCanvasConnection> connections,
            CharacterPoseNativeGraphBoundary boundary)
        {
            int outputCount = nodes.Count(value => value.Kind == CharacterPoseNodeKind.OutputPose);
            int graphInputCount = nodes.Count(value =>
                value.Kind == CharacterPoseNodeKind.GraphInput ||
                value.Kind == CharacterPoseNodeKind.EntryPoseInput);
            int graphOutputCount = nodes.Count(value => value.Kind == CharacterPoseNodeKind.GraphOutput);
            bool graphBoundary = boundary == CharacterPoseNativeGraphBoundary.Subgraph;
            bool rootOrState = boundary == CharacterPoseNativeGraphBoundary.Root ||
                boundary == CharacterPoseNativeGraphBoundary.State;
            if (rootOrState &&
                (outputCount != 1 || graphInputCount != 0 || graphOutputCount != 0) ||
                graphBoundary &&
                (outputCount != 0 || graphInputCount != 1 || graphOutputCount != 1))
            {
                Fail(
                    CharacterPoseNativeFailureCode.GraphInvalid,
                    graph.GraphId.Value,
                    graphBoundary
                        ? "Pose boundary graph must contain exactly one Graph Input or Entry Pose Input, one Graph Output and no Output Pose."
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

                CharacterPoseCanvasNode entryPose = nodes.SingleOrDefault(
                    value => value.Kind == CharacterPoseNodeKind.EntryPoseInput);
                if (entryPose != null)
                {
                    var reachable = new HashSet<PoseNodeId>();
                    var pending = new Queue<PoseNodeId>();
                    pending.Enqueue(graphOutput.NodeId);
                    while (pending.Count != 0)
                    {
                        PoseNodeId current = pending.Dequeue();
                        if (!reachable.Add(current))
                            continue;
                        for (int i = 0; i < connections.Count; i++)
                        {
                            CharacterPoseCanvasConnection connection = connections[i];
                            if (connection.TargetNodeId == current)
                                pending.Enqueue(connection.SourceNodeId);
                        }
                    }
                    if (!reachable.Contains(entryPose.NodeId))
                    {
                        Fail(
                            CharacterPoseNativeFailureCode.PortInvalid,
                            graph.GraphId.Value,
                            $"Pose Entry Pose Input '{entryPose.NodeId}' is not connected to the Graph Output path.");
                    }
                }
            }
        }

        static void ValidateNativeTopology(
            CharacterPoseCanvasGraph graph,
            IReadOnlyList<CharacterPoseCanvasNode> nodes,
            IReadOnlyList<CharacterPoseCanvasConnection> connections,
            CharacterPoseNativeGraphBoundary boundary)
        {
            PoseNodeId outputId = boundary == CharacterPoseNativeGraphBoundary.Subgraph
                ? nodes.Single(value => value.Kind == CharacterPoseNodeKind.GraphOutput).NodeId
                : nodes.Single(value => value.Kind == CharacterPoseNodeKind.OutputPose).NodeId;
            HashSet<PoseNodeId> activeNodes = CollectAncestors(connections, outputId);
            var byId = nodes.ToDictionary(value => value.NodeId);

            List<CharacterPoseCanvasNode> assemblers = activeNodes
                .Where(nodeId => byId[nodeId].Kind == CharacterPoseNodeKind.FullBodyIkGoalAssembler)
                .Select(nodeId => byId[nodeId])
                .ToList();
            List<CharacterPoseCanvasNode> solvers = activeNodes
                .Where(nodeId => byId[nodeId].Kind == CharacterPoseNodeKind.FullBodyIK)
                .Select(nodeId => byId[nodeId])
                .ToList();
            if (assemblers.Count > 1 || solvers.Count > 1 ||
                solvers.Count == 1 && assemblers.Count == 0)
            {
                Fail(
                    CharacterPoseNativeFailureCode.GraphInvalid,
                    graph.GraphId.Value,
                    $"Pose output path must contain zero or one paired Goal Assembler and Full Body IK; found {assemblers.Count} and {solvers.Count}.");
            }
            if (assemblers.Count == 1 && solvers.Count == 1)
            {
                HashSet<PoseNodeId> assemblerOutputs = CollectReachable(
                    CreateForwardConnections(connections), assemblers[0].NodeId);
                if (!assemblerOutputs.Contains(solvers[0].NodeId))
                {
                    Fail(
                        CharacterPoseNativeFailureCode.GraphInvalid,
                        $"{graph.GraphId}/{assemblers[0].NodeId}",
                        "Pose Full Body Ik Goal Assembler is not connected to Full Body Ik.");
                }
            }

            ValidateGoalSlots(graph, byId, activeNodes);
            ValidateModifyBoneConflicts(graph, byId, activeNodes, connections);
        }

        static void ValidateGoalSlots(
            CharacterPoseCanvasGraph graph,
            IReadOnlyDictionary<PoseNodeId, CharacterPoseCanvasNode> byId,
            HashSet<PoseNodeId> activeNodes)
        {
            var slots = new HashSet<CharacterFullBodyIkEffectorSlot>();
            foreach (PoseNodeId nodeId in activeNodes)
            {
                if (byId[nodeId].Payload is not CharacterPoseBoneIkGoalsPayload payload)
                    continue;
                foreach (CharacterPoseBoneIkGoalBinding binding in payload.Bindings)
                {
                    if (!slots.Add(binding.EffectorSlot))
                    {
                        Fail(
                            CharacterPoseNativeFailureCode.GraphInvalid,
                            $"{graph.GraphId}/{nodeId}",
                            $"Pose Full Body Ik Goal Slot '{binding.EffectorSlot}' is already supplied.");
                    }
                }
            }
        }

        static void ValidateModifyBoneConflicts(
            CharacterPoseCanvasGraph graph,
            IReadOnlyDictionary<PoseNodeId, CharacterPoseCanvasNode> byId,
            HashSet<PoseNodeId> activeNodes,
            IReadOnlyList<CharacterPoseCanvasConnection> connections)
        {
            var writersByBone = new Dictionary<AnimationBoneId, List<(PoseNodeId NodeId, ModifyBoneOperationMask Operations)>>();
            foreach (PoseNodeId nodeId in activeNodes)
            {
                if (byId[nodeId].Payload is not CharacterModifyBonePosePayload payload ||
                    payload.Operations == ModifyBoneOperationMask.None)
                    continue;
                if (!writersByBone.TryGetValue(payload.BoneId, out var writers))
                {
                    writers = new List<(PoseNodeId, ModifyBoneOperationMask)>();
                    writersByBone.Add(payload.BoneId, writers);
                }
                writers.Add((nodeId, payload.Operations));
            }

            foreach (KeyValuePair<AnimationBoneId, List<(PoseNodeId NodeId, ModifyBoneOperationMask Operations)>> pair in writersByBone)
            {
                for (int left = 0; left < pair.Value.Count; left++)
                {
                    for (int right = left + 1; right < pair.Value.Count; right++)
                    {
                        ModifyBoneOperationMask overlap =
                            pair.Value[left].Operations & pair.Value[right].Operations;
                        if (overlap == ModifyBoneOperationMask.None)
                            continue;
                        HashSet<PoseNodeId> leftOutputs = CollectReachable(
                            CreateForwardConnections(connections), pair.Value[left].NodeId);
                        if (leftOutputs.Contains(pair.Value[right].NodeId))
                            continue;
                        HashSet<PoseNodeId> rightOutputs = CollectReachable(
                            CreateForwardConnections(connections), pair.Value[right].NodeId);
                        if (rightOutputs.Contains(pair.Value[left].NodeId))
                            continue;
                        Fail(
                            CharacterPoseNativeFailureCode.GraphInvalid,
                            $"{graph.GraphId}/{pair.Key}",
                            $"Pose Modify Bone nodes '{pair.Value[left].NodeId}' and '{pair.Value[right].NodeId}' write conflicting operations on '{pair.Key}'.");
                    }
                }
            }
        }

        static HashSet<PoseNodeId> CollectReachable(
            IReadOnlyList<(PoseNodeId Source, PoseNodeId Target)> connections,
            PoseNodeId outputId)
        {
            var outgoing = connections
                .GroupBy(value => value.Source)
                .ToDictionary(value => value.Key, value => value.Select(item => item.Target).ToList());
            var reachable = new HashSet<PoseNodeId> { outputId };
            var pending = new Queue<PoseNodeId>();
            pending.Enqueue(outputId);
            while (pending.Count != 0)
            {
                PoseNodeId current = pending.Dequeue();
                if (!outgoing.TryGetValue(current, out List<PoseNodeId> targets))
                    continue;
                foreach (PoseNodeId target in targets)
                {
                    if (reachable.Add(target))
                        pending.Enqueue(target);
                }
            }
            return reachable;
        }

        static HashSet<PoseNodeId> CollectAncestors(
            IReadOnlyList<CharacterPoseCanvasConnection> connections,
            PoseNodeId outputId)
        {
            var incoming = connections
                .GroupBy(value => value.TargetNodeId)
                .ToDictionary(value => value.Key, value => value.Select(item => item.SourceNodeId).ToList());
            var ancestors = new HashSet<PoseNodeId> { outputId };
            var pending = new Queue<PoseNodeId>();
            pending.Enqueue(outputId);
            while (pending.Count != 0)
            {
                PoseNodeId current = pending.Dequeue();
                if (!incoming.TryGetValue(current, out List<PoseNodeId> sources))
                    continue;
                foreach (PoseNodeId source in sources)
                {
                    if (ancestors.Add(source))
                        pending.Enqueue(source);
                }
            }
            return ancestors;
        }

        static IReadOnlyList<(PoseNodeId Source, PoseNodeId Target)> CreateForwardConnections(
            IReadOnlyList<CharacterPoseCanvasConnection> connections) =>
            connections
                .Select(value => (value.SourceNodeId, value.TargetNodeId))
                .ToList();

        static void ValidateReferencedGraphs(
            CharacterPresentationPoseGraphAsset graphAsset,
            CharacterPoseCanvasGraph graph,
            IReadOnlyList<CharacterPoseCanvasNode> nodes,
            ISet<PoseGraphId> visiting,
            IDictionary<PoseGraphId, CharacterPoseNativeGraphBoundary> visited)
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
                            CharacterPoseNativeGraphBoundary.State,
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
                        CharacterPoseNativeGraphBoundary.Subgraph,
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
                        CharacterPoseNativeGraphBoundary.Subgraph,
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
