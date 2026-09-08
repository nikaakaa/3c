using System;
using System.Collections.Generic;
using System.Linq;
using NodeCanvas.Framework;
using ParadoxNotion;
using ParadoxNotion.Design;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ThirdPersonCharacter.Pipeline.Animation
{
    [Serializable]
    public sealed class CharacterPoseCanvasGraph : NodeCanvas.Framework.Graph
    {
        [SerializeField] string m_GraphId = string.Empty;
        [SerializeField] string m_ContentRevision = string.Empty;
        [SerializeField] CharacterPoseParameterDeclaration[] m_Parameters =
            Array.Empty<CharacterPoseParameterDeclaration>();

        public PoseGraphId GraphId => string.IsNullOrWhiteSpace(m_GraphId)
            ? default
            : new PoseGraphId(m_GraphId);
        public string ContentRevision => m_ContentRevision ?? string.Empty;
        public IReadOnlyList<CharacterPoseParameterDeclaration> Parameters =>
            m_Parameters ?? Array.Empty<CharacterPoseParameterDeclaration>();
        public IReadOnlyList<CharacterPoseCanvasNode> Nodes =>
            (allNodes ?? new List<Node>()).OfType<CharacterPoseCanvasNode>().ToArray();
        public IReadOnlyList<CharacterPoseCanvasConnection> Connections =>
            Nodes.SelectMany(node => node.outConnections.OfType<CharacterPoseCanvasConnection>())
                .Distinct()
                .OrderBy(connection => connection.EdgeId, StringComparer.Ordinal)
                .ToArray();
        public IReadOnlyList<CharacterPoseCanvasConnection> Edges => Connections;
        public IReadOnlyList<CharacterPoseGraphLayoutEntry> Layout =>
            Nodes.OrderBy(node => node.NodeId)
                .Select(node => new CharacterPoseGraphLayoutEntry(node.NodeId, node.position))
                .ToArray();

        public override Type baseNodeType => typeof(CharacterPoseCanvasNode);
        public override bool requiresAgent => false;
        public override bool requiresPrimeNode => false;
        public override bool isTree => false;
        public override PlanarDirection flowDirection => PlanarDirection.Horizontal;
        public override bool allowBlackboardOverrides => false;
        public override bool canAcceptVariableDrops => false;

#if UNITY_EDITOR
        [NonSerialized] internal CharacterPoseCanvasEditorWriteRouter EditorWriteRouter;

        public override Node AddNode(Type nodeType, Vector2 pos = default) =>
            EditorWriteRouter != null
                ? EditorWriteRouter.CreateNode(nodeType, pos)
                : throw new InvalidOperationException(
                    "Pose Canvas nodes must be changed through the Pose Canvas editor session or the Presentation Mutation chain.");

        public override void RemoveNode(Node node, bool recordUndo = true, bool force = false)
        {
            if (EditorWriteRouter == null)
                throw new InvalidOperationException(
                    "Pose Canvas nodes must be changed through the Pose Canvas editor session or the Presentation Mutation chain.");
            EditorWriteRouter.RemoveNode(node);
        }

        public override Connection ConnectNodes(
            Node sourceNode,
            Node targetNode,
            int sourceIndex = -1,
            int targetIndex = -1)
        {
            if (EditorWriteRouter == null)
                throw new InvalidOperationException(
                    "Pose Canvas connections must be changed through the Pose Canvas editor session or the Presentation Mutation chain.");
            return EditorWriteRouter.Connect(sourceNode, targetNode, sourceIndex, targetIndex);
        }

        public override void RemoveConnection(Connection connection, bool recordUndo = true)
        {
            if (EditorWriteRouter == null)
                throw new InvalidOperationException(
                    "Pose Canvas connections must be changed through the Pose Canvas editor session or the Presentation Mutation chain.");
            EditorWriteRouter.RemoveConnection(connection);
        }

        public override GenericMenu GetNodeSelectionMenu(NodeCreationRequestContext request)
        {
            if (EditorWriteRouter == null)
                throw new InvalidOperationException(
                    "Pose Canvas node creation requires the Pose Canvas editor session.");
            return EditorWriteRouter.BuildNodeCreationMenu(request);
        }
#endif

        public CharacterPoseCanvasNode RequireNode(PoseNodeId nodeId) =>
            Nodes.SingleOrDefault(node => node.NodeId == nodeId) ??
            throw new InvalidOperationException(
                $"Pose Canvas node '{nodeId}' does not exist in graph '{GraphId}'.");

        public bool TryGetNode(PoseNodeId nodeId, out CharacterPoseCanvasNode node)
        {
            node = Nodes.SingleOrDefault(value => value.NodeId == nodeId);
            return node != null;
        }

        internal CharacterPoseCanvasGraph SetAuthoring(
            PoseGraphId graphId,
            string contentRevision,
            CharacterPoseParameterDeclaration[] parameters,
            CharacterPoseCanvasNode[] nodes,
            CharacterPoseCanvasConnection[] connections)
        {
            m_GraphId = graphId.IsValid
                ? graphId.Value
                : throw new ArgumentException("Pose Canvas graph identity is invalid.", nameof(graphId));
            m_ContentRevision = PoseIdentity.Require(contentRevision, nameof(contentRevision));
            m_Parameters = parameters ?? Array.Empty<CharacterPoseParameterDeclaration>();
            CharacterPoseCanvasNode[] nodeValues = nodes ?? Array.Empty<CharacterPoseCanvasNode>();
            CharacterPoseCanvasConnection[] connectionValues =
                connections ?? Array.Empty<CharacterPoseCanvasConnection>();
            RequireNodeSet(nodeValues);
            foreach (CharacterPoseCanvasNode node in nodeValues)
                node.outConnections.Clear();
            foreach (CharacterPoseCanvasNode node in nodeValues)
                node.inConnections.Clear();
            allNodes = nodeValues.Cast<Node>().ToList();
            var edges = new HashSet<string>(StringComparer.Ordinal);
            foreach (CharacterPoseCanvasConnection connection in connectionValues)
            {
                if (connection == null || !edges.Add(connection.EdgeId))
                {
                    throw new InvalidOperationException(
                        $"Pose Canvas graph '{GraphId}' contains an invalid or duplicate connection.");
                }
                CharacterPoseCanvasNode source = nodeValues.SingleOrDefault(
                    node => node.NodeId == connection.SourceNodeId);
                CharacterPoseCanvasNode target = nodeValues.SingleOrDefault(
                    node => node.NodeId == connection.TargetNodeId);
                if (source == null || target == null)
                    throw new InvalidOperationException(
                        $"Pose Canvas graph '{GraphId}' connection '{connection.EdgeId}' targets an unknown node.");
                connection.BindNodes(source, target);
                source.outConnections.Add(connection);
                target.inConnections.Add(connection);
            }
            return this;
        }

        public static CharacterPoseCanvasGraph CreateAuthoring(
            PoseGraphId graphId,
            string contentRevision,
            CharacterPoseParameterDeclaration[] parameters,
            CharacterPoseCanvasNode[] nodes,
            CharacterPoseCanvasConnection[] connections,
            IReadOnlyList<CharacterPoseGraphLayoutEntry> layout = null)
        {
            CharacterPoseCanvasGraph graph = CreateInstance<CharacterPoseCanvasGraph>();
            CharacterPoseCanvasNode[] values = (nodes ?? Array.Empty<CharacterPoseCanvasNode>())
                .Select(node => node?.CloneAuthoring() ??
                    throw new InvalidOperationException("Pose Canvas node is missing."))
                .ToArray();
            CharacterPoseCanvasConnection[] edges = (connections ?? Array.Empty<CharacterPoseCanvasConnection>())
                .Select(connection => connection == null
                    ? throw new InvalidOperationException("Pose Canvas connection is missing.")
                    : new CharacterPoseCanvasConnection(
                        connection.EdgeId,
                        connection.SourceNodeId,
                        connection.SourcePortId,
                        connection.TargetNodeId,
                        connection.TargetPortId))
                .ToArray();
            graph.SetAuthoring(graphId, contentRevision, parameters, values, edges);
            foreach (CharacterPoseGraphLayoutEntry entry in layout ?? Array.Empty<CharacterPoseGraphLayoutEntry>())
                graph.SetNodePosition(entry.NodeId, entry.Position);
            return graph;
        }

        internal void SetNodePosition(PoseNodeId nodeId, Vector2 position)
        {
            if (!float.IsFinite(position.x) || !float.IsFinite(position.y))
                throw new ArgumentException("Pose Canvas node position must be finite.", nameof(position));
            RequireNode(nodeId).position = position;
        }

        public void RequireValid()
        {
            if (!GraphId.IsValid || string.IsNullOrWhiteSpace(ContentRevision))
                throw new InvalidOperationException("Pose Canvas graph identity or revision is invalid.");
            CharacterPoseCanvasNode[] nodes = Nodes.ToArray();
            if (allNodes == null || allNodes.Count != nodes.Length)
                throw new InvalidOperationException(
                    $"Pose Canvas graph '{GraphId}' contains a node outside the Pose Canvas node catalog.");
            RequireNodeSet(nodes);
            var edges = new HashSet<string>(StringComparer.Ordinal);
            foreach (CharacterPoseCanvasConnection connection in Connections)
            {
                if (connection == null || string.IsNullOrWhiteSpace(connection.EdgeId) ||
                    !edges.Add(connection.EdgeId) || connection.SourceNode == null ||
                    connection.TargetNode == null || !connection.SourcePortId.IsValid ||
                    !connection.TargetPortId.IsValid)
                {
                    throw new InvalidOperationException(
                        $"Pose Canvas graph '{GraphId}' contains an invalid connection.");
                }
            }
        }

        static void RequireNodeSet(IReadOnlyList<CharacterPoseCanvasNode> nodes)
        {
            var identities = new HashSet<PoseNodeId>();
            for (int i = 0; i < nodes.Count; i++)
            {
                CharacterPoseCanvasNode node = nodes[i];
                if (node == null || !node.NodeId.IsValid || !identities.Add(node.NodeId) ||
                    node.Payload == null || node.Payload.Kind != node.Kind)
                {
                    throw new InvalidOperationException(
                        $"Pose Canvas node #{i} is missing, duplicated or has an invalid payload.");
                }
            }
        }
    }

#if UNITY_EDITOR
    internal interface CharacterPoseCanvasEditorWriteRouter
    {
        Node CreateNode(Type nodeType, Vector2 position);
        Node CreateNodeFromCapability(string capabilityIdentity, Vector2 position, Node connectSource, int connectSourcePortIndex);
        void RemoveNode(Node node);
        Connection Connect(Node sourceNode, Node targetNode, int sourceIndex, int targetIndex);
        void RemoveConnection(Connection connection);
        GenericMenu BuildNodeCreationMenu(NodeCanvas.Framework.Graph.NodeCreationRequestContext request);
    }
#endif
}
