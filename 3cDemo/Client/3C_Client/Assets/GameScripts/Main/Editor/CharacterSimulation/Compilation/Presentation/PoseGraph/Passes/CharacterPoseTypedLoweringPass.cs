using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Editor;
using ThirdPersonCharacter.Pipeline.Simulation.Editor;

namespace ThirdPersonCharacter.Editor.CharacterSimulation
{
    internal sealed class CharacterPoseTypedIrGraph
    {
        readonly Dictionary<PoseNodeId, CharacterTypedPoseNode> m_AuthoredNodes;
        readonly Dictionary<PoseNodeId, IReadOnlyList<CharacterPoseEdge>>
            m_Incoming;
        readonly Dictionary<PoseNodeId, CharacterPoseIrNode> m_Nodes;

        internal CharacterPoseTypedIrGraph(
            CharacterPoseGraphClosureEntry closure,
            IReadOnlyDictionary<PoseNodeId,
                CharacterTypedPoseNode> authoredNodes,
            IReadOnlyDictionary<PoseNodeId,
                IReadOnlyList<CharacterPoseEdge>> incoming,
            IReadOnlyDictionary<PoseNodeId, CharacterPoseIrNode> nodes)
        {
            Closure = closure ??
                throw new ArgumentNullException(nameof(closure));
            m_AuthoredNodes = new Dictionary<PoseNodeId,
                CharacterTypedPoseNode>(authoredNodes ??
                throw new ArgumentNullException(nameof(authoredNodes)));
            m_Incoming = new Dictionary<PoseNodeId,
                IReadOnlyList<CharacterPoseEdge>>(incoming ??
                throw new ArgumentNullException(nameof(incoming)));
            m_Nodes = new Dictionary<PoseNodeId, CharacterPoseIrNode>(
                nodes ?? throw new ArgumentNullException(nameof(nodes)));
            if (m_AuthoredNodes.Count != m_Incoming.Count ||
                m_AuthoredNodes.Count != m_Nodes.Count ||
                m_AuthoredNodes.Keys.Any(value =>
                    !m_Incoming.ContainsKey(value) ||
                    !m_Nodes.ContainsKey(value)))
            {
                throw new InvalidOperationException(
                    "Typed Pose IR Graph node catalogs do not match.");
            }
        }

        internal CharacterPoseGraphClosureEntry Closure { get; }
        internal CharacterTypedPoseGraph Source => Closure.Graph;
        internal IReadOnlyDictionary<PoseNodeId,
            CharacterTypedPoseNode> AuthoredNodes => m_AuthoredNodes;
        internal IReadOnlyDictionary<PoseNodeId,
            IReadOnlyList<CharacterPoseEdge>> Incoming => m_Incoming;

        internal CharacterPoseIrNode RequireNode(PoseNodeId nodeId) =>
            m_Nodes.TryGetValue(nodeId, out CharacterPoseIrNode node)
                ? node
                : throw new InvalidOperationException(
                    $"Typed Pose IR Graph '{Source.GraphId}' has no Node '{nodeId}'.");
    }

    internal sealed class CharacterPoseTypedIrCatalog
    {
        readonly Dictionary<string, CharacterPoseTypedIrGraph> m_Graphs;

        internal CharacterPoseTypedIrCatalog(
            IReadOnlyList<CharacterPoseTypedIrGraph> graphs)
        {
            Graphs = graphs?.ToArray() ??
                throw new ArgumentNullException(nameof(graphs));
            m_Graphs = Graphs.ToDictionary(
                value => CharacterPoseGraphClosure.Key(
                    value.Closure.OwnerIdentity,
                    value.Source.GraphId),
                StringComparer.Ordinal);
        }

        internal IReadOnlyList<CharacterPoseTypedIrGraph> Graphs { get; }

        internal CharacterPoseTypedIrGraph RequireGraph(
            CharacterPresentationPoseGraphAsset owner,
            PoseGraphId graphId)
        {
            string ownerIdentity =
                CharacterPresentationAssetObjectIdentity.Require(owner);
            string key = CharacterPoseGraphClosure.Key(
                ownerIdentity,
                graphId);
            return m_Graphs.TryGetValue(key, out CharacterPoseTypedIrGraph graph)
                ? graph
                : throw new InvalidOperationException(
                    $"Typed Pose IR catalog has no Graph '{graphId}' for '{ownerIdentity}'.");
        }
    }

    internal sealed class CharacterPoseTypedLoweringPassResult
    {
        internal CharacterPoseTypedLoweringPassResult(
            CharacterPoseTypedIrCatalog catalog,
            IReadOnlyList<CharacterPoseCompilationDiagnostic> diagnostics)
        {
            Catalog = catalog;
            Diagnostics = diagnostics?.ToArray() ??
                Array.Empty<CharacterPoseCompilationDiagnostic>();
            IsSuccess = catalog != null &&
                Diagnostics.All(value =>
                    value.Severity !=
                    CharacterPoseCompilationDiagnosticSeverity.Error);
        }

        internal bool IsSuccess { get; }
        internal CharacterPoseTypedIrCatalog Catalog { get; }
        internal IReadOnlyList<CharacterPoseCompilationDiagnostic>
            Diagnostics { get; }
    }

    internal static class CharacterPoseTypedLoweringPass
    {
        sealed class TypedLoweringFailure : Exception
        {
            internal TypedLoweringFailure(
                string reason,
                string message,
                PoseGraphId graphId,
                PoseNodeId nodeId = default,
                PosePortId portId = default,
                string sourcePath = "")
                : base(message)
            {
                Reason = reason;
                GraphId = graphId;
                NodeId = nodeId;
                PortId = portId;
                SourcePath = sourcePath;
            }

            internal string Reason { get; }
            internal PoseGraphId GraphId { get; }
            internal PoseNodeId NodeId { get; }
            internal PosePortId PortId { get; }
            internal string SourcePath { get; }
        }

        internal static CharacterPoseTypedLoweringPassResult Run(
            CharacterPoseCompilationRequest request,
            CharacterPoseGraphClosure closure)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            if (closure == null)
                throw new ArgumentNullException(nameof(closure));
            try
            {
                var graphs = new List<CharacterPoseTypedIrGraph>(
                    closure.Entries.Count);
                for (int graphIndex = 0;
                     graphIndex < closure.Entries.Count;
                     graphIndex++)
                {
                    graphs.Add(LowerGraph(
                        request,
                        closure.Entries[graphIndex]));
                }
                return new CharacterPoseTypedLoweringPassResult(
                    new CharacterPoseTypedIrCatalog(graphs),
                    Array.Empty<CharacterPoseCompilationDiagnostic>());
            }
            catch (TypedLoweringFailure failure)
            {
                return Failure(
                    failure.Reason,
                    failure.Message,
                    failure.GraphId,
                    failure.NodeId,
                    failure.PortId,
                    failure.SourcePath);
            }
            catch (Exception exception)
            {
                return Failure(
                    "typed-lowering-invalid",
                    exception.Message,
                    request.Asset.Graph.GraphId);
            }
        }

        static CharacterPoseTypedIrGraph LowerGraph(
            CharacterPoseCompilationRequest request,
            CharacterPoseGraphClosureEntry closure)
        {
            CharacterTypedPoseGraph graph = closure.Graph;
            var authoredNodes =
                new Dictionary<PoseNodeId, CharacterTypedPoseNode>();
            for (int nodeIndex = 0;
                 nodeIndex < graph.Nodes.Count;
                 nodeIndex++)
            {
                CharacterTypedPoseNode node = graph.Nodes[nodeIndex];
                if (node == null ||
                    !node.NodeId.IsValid ||
                    !authoredNodes.TryAdd(node.NodeId, node))
                {
                    throw new TypedLoweringFailure(
                        "node-identity-invalid",
                        "Pose node identity is missing or duplicated.",
                        graph.GraphId,
                        node?.NodeId ?? default,
                        sourcePath: GraphPath(graph));
                }
                ValidatePorts(graph, node);
            }
            Dictionary<PoseNodeId, IReadOnlyList<CharacterPoseEdge>> incoming =
                BuildIncoming(graph, authoredNodes);
            var lowered =
                new Dictionary<PoseNodeId, CharacterPoseIrNode>();
            for (int nodeIndex = 0;
                 nodeIndex < graph.Nodes.Count;
                 nodeIndex++)
            {
                CharacterTypedPoseNode node = graph.Nodes[nodeIndex];
                string sourcePath = NodePath(graph, node.NodeId);
                try
                {
                    CharacterPoseNodeDefinition definition =
                        CharacterPoseNodeDefinitionModule.Shared.Require(
                            node.Kind);
                    definition.RequirePayload(node.Payload);
                    definition.ValidatePayload(node.Payload, sourcePath);
                    definition.ValidateRig(
                        node.Payload,
                        request.Rig,
                        sourcePath);
                    IReadOnlyList<CharacterPoseIrInput> inputs = BuildInputs(
                        graph,
                        node,
                        incoming[node.NodeId],
                        authoredNodes,
                        sourcePath);
                    lowered.Add(
                        node.NodeId,
                        definition.Lower(node, inputs, sourcePath));
                }
                catch (TypedLoweringFailure)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    throw new TypedLoweringFailure(
                        "node-lowering-invalid",
                        exception.Message,
                        graph.GraphId,
                        node.NodeId,
                        sourcePath: sourcePath);
                }
            }
            return new CharacterPoseTypedIrGraph(
                closure,
                authoredNodes,
                incoming,
                lowered);
        }

        static void ValidatePorts(
            CharacterTypedPoseGraph graph,
            CharacterTypedPoseNode node)
        {
            var ids = new HashSet<string>(
                CharacterPoseAuthoringPortProjection
                    .GetDeclared(node)
                    .Select(value => value.PortId.Value),
                StringComparer.Ordinal);
            foreach (CharacterPoseDynamicPort port in node.DynamicPorts)
            {
                if (port == null ||
                    !port.PortId.IsValid ||
                    !ids.Add(port.PortId.Value))
                {
                    throw new TypedLoweringFailure(
                        "port-identity-invalid",
                        $"Pose Node '{node.NodeId}' contains an invalid or duplicate dynamic Port.",
                        graph.GraphId,
                        node.NodeId,
                        port?.PortId ?? default,
                        NodePath(graph, node.NodeId));
                }
            }
        }

        static Dictionary<PoseNodeId, IReadOnlyList<CharacterPoseEdge>>
            BuildIncoming(
                CharacterTypedPoseGraph graph,
                IReadOnlyDictionary<PoseNodeId,
                    CharacterTypedPoseNode> nodes)
        {
            var incoming = nodes.Keys.ToDictionary(
                value => value,
                _ => new List<CharacterPoseEdge>());
            var edgeIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (CharacterPoseEdge edge in graph.Edges)
            {
                if (edge == null ||
                    string.IsNullOrWhiteSpace(edge.EdgeId) ||
                    !edgeIds.Add(edge.EdgeId) ||
                    !nodes.ContainsKey(edge.SourceNodeId) ||
                    !nodes.ContainsKey(edge.TargetNodeId))
                {
                    throw new TypedLoweringFailure(
                        "edge-identity-invalid",
                        "Pose edge identity or endpoint is invalid.",
                        graph.GraphId,
                        edge?.TargetNodeId ?? default,
                        edge?.TargetPortId ?? default,
                        GraphPath(graph));
                }
                incoming[edge.TargetNodeId].Add(edge);
            }
            return incoming.ToDictionary(
                value => value.Key,
                value => (IReadOnlyList<CharacterPoseEdge>)
                    value.Value.ToArray());
        }

        static IReadOnlyList<CharacterPoseIrInput> BuildInputs(
            CharacterTypedPoseGraph graph,
            CharacterTypedPoseNode target,
            IReadOnlyList<CharacterPoseEdge> incoming,
            IReadOnlyDictionary<PoseNodeId,
                CharacterTypedPoseNode> nodes,
            string sourcePath)
        {
            var occupied = new HashSet<string>(StringComparer.Ordinal);
            var result = new List<CharacterPoseIrInput>();
            foreach (CharacterPoseEdge edge in incoming.OrderBy(
                         value => value.TargetPortId.Value,
                         StringComparer.Ordinal))
            {
                if (!occupied.Add(edge.TargetPortId.Value))
                {
                    throw new TypedLoweringFailure(
                        "input-source-ambiguous",
                        $"Input Port '{edge.TargetPortId}' has more than one source.",
                        graph.GraphId,
                        target.NodeId,
                        edge.TargetPortId,
                        sourcePath);
                }
                CharacterPosePortDefinition targetPort =
                    CharacterPoseAuthoringPortProjection.Require(
                        target,
                        edge.TargetPortId.Value,
                        CharacterPosePortDirection.Input);
                CharacterPosePortDefinition sourcePort =
                    CharacterPoseAuthoringPortProjection.Require(
                        nodes[edge.SourceNodeId],
                        edge.SourcePortId.Value,
                        CharacterPosePortDirection.Output);
                if (targetPort.Kind != sourcePort.Kind)
                {
                    throw new TypedLoweringFailure(
                        "edge-value-kind-mismatch",
                        $"Pose edge '{edge.EdgeId}' connects different value kinds.",
                        graph.GraphId,
                        target.NodeId,
                        edge.TargetPortId,
                        sourcePath);
                }
                result.Add(new CharacterPoseIrInput(
                    new CharacterPoseIrLinkId(edge.EdgeId),
                    edge.TargetPortId.Value,
                    new CharacterPoseIrNodeId(edge.SourceNodeId.Value),
                    edge.SourcePortId.Value,
                    targetPort.Kind));
            }
            foreach (CharacterPosePortDefinition port in
                     CharacterPoseAuthoringPortProjection.Get(target))
            {
                if (port.Direction == CharacterPosePortDirection.Input &&
                    port.Required &&
                    !occupied.Contains(port.PortId.Value))
                {
                    throw new TypedLoweringFailure(
                        "required-input-missing",
                        $"Required input '{port.PortId}' is not connected.",
                        graph.GraphId,
                        target.NodeId,
                        port.PortId,
                        sourcePath);
                }
            }
            return result;
        }

        static CharacterPoseTypedLoweringPassResult Failure(
            string reason,
            string message,
            PoseGraphId graphId,
            PoseNodeId nodeId = default,
            PosePortId portId = default,
            string sourcePath = "") =>
            new CharacterPoseTypedLoweringPassResult(
                null,
                new[]
                {
                    new CharacterPoseCompilationDiagnostic(
                        CharacterPoseCompilationPass.TypedLowering,
                        CharacterPoseCompilationDiagnosticSeverity.Error,
                        reason,
                        message,
                        graphId,
                        nodeId,
                        portId,
                        sourcePath: sourcePath)
                });

        static string GraphPath(CharacterTypedPoseGraph graph) =>
            $"pose-graphs/{graph.GraphId.Value}";

        static string NodePath(
            CharacterTypedPoseGraph graph,
            PoseNodeId nodeId) =>
            $"{GraphPath(graph)}/nodes/{nodeId.Value}";
    }
}
