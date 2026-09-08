using System;
using System.Collections.Generic;
using System.Linq;
using NodeCanvas.Editor;
using NodeCanvas.Framework;
using ThirdPersonCharacter.Editor.CharacterSimulation;
using ThirdPersonCharacter.Pipeline.Animation;
using TreeDesigner.Editor;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    internal sealed class CharacterPoseCanvasEditorWriteSession :
        CharacterPoseCanvasEditorWriteRouter
    {
        readonly CharacterPoseGraphAssetMutationOwner m_Owner;
        readonly CharacterPresentationMutationService m_Service;
        readonly CharacterPoseCanvasGraph m_Graph;
        readonly CharacterPresentationPoseGraphAsset m_Asset;

        public CharacterPoseCanvasEditorWriteSession(
            CharacterPresentationPoseGraphAsset asset,
            CharacterPoseCanvasGraph graph)
        {
            if (asset == null)
                throw new ArgumentNullException(nameof(asset));
            m_Owner = new CharacterPoseGraphAssetMutationOwner(asset);
            m_Asset = asset;
            m_Service = new CharacterPresentationMutationService();
            m_Graph = graph ? graph : throw new ArgumentNullException(nameof(graph));
        }

        public CharacterPoseCanvasGraph Graph => m_Graph;
        public bool ReadOnly { get; set; }

        void Apply(CharacterPresentationMutationTransaction transaction)
        {
            if (ReadOnly)
                throw new InvalidOperationException("Pose Canvas is observing a running instance and is read-only.");
            m_Service.ApplyGraphMutationsInPlace(m_Owner, transaction);
        }

        public Node CreateNode(Type nodeType, Vector2 position) =>
            throw new InvalidOperationException(
                "Pose Canvas nodes are created from their Node Definition; use the Definition-driven creation menu.");

        public Node CreateNodeFromCapability(
            string capabilityIdentity,
            Vector2 position,
            Node connectSource,
            int connectSourcePortIndex,
            int targetPortIndex = -1)
        {
            CharacterPoseNodeDefinition definition =
                CharacterPoseNodeDefinitionModule.Shared.RequireCapability(
                    capabilityIdentity);
            if (definition.CanvasCreation == CharacterPoseCanvasCreationKind.DedicatedSurface)
                throw new InvalidOperationException(
                    $"Pose node '{definition.Capability.DisplayName}' is created from its dedicated authoring surface.");
            CharacterPoseGraphAuthoringCapabilities.Catalog.Require(definition.Capability.CapabilityId,
                CharacterPoseGraphAuthoringCapabilities.Domain, ResolveRole());
            var node = new CharacterPoseCanvasNode(
                new PoseNodeId(Guid.NewGuid().ToString("N")),
                definition.Capability.DisplayName,
                definition.CreateDefaultPayload());
            var transaction = new CharacterPresentationMutationTransaction(
                Guid.NewGuid().ToString("N"),
                $"Add {definition.Capability.DisplayName}");
            transaction.Add(new CreatePoseNodeMutation(
                m_Graph.GraphId.Value,
                node,
                position));
            CharacterPoseCanvasNode source = connectSource as CharacterPoseCanvasNode;
            if (source != null)
            {
                CharacterPosePortDefinition output = RequirePort(source, CharacterPosePortDirection.Output, connectSourcePortIndex);
                PosePortId sourcePort = output.PortId;
                PosePortId targetPort = RequirePort(node, CharacterPosePortDirection.Input, targetPortIndex, output.Kind).PortId;
                transaction.Add(new ConnectPosePortMutation(
                    m_Graph.GraphId.Value,
                    NewEdgeId(),
                    source.NodeId,
                    sourcePort,
                    node.NodeId,
                    targetPort));
            }
            Apply(transaction);
            CharacterPoseCanvasNode created = m_Graph.RequireNode(node.NodeId);
            created.customColor = definition.Capability.Color;
            GraphEditorUtility.activeElement = created;
            return created;
        }

        public void RemoveNode(Node node)
        {
            var target = node as CharacterPoseCanvasNode ??
                throw new InvalidOperationException("Removed node is not a Pose Canvas node.");
            if (GraphEditorUtility.activeElement == target)
                GraphEditorUtility.activeElement = null;
            var transaction = new CharacterPresentationMutationTransaction(
                Guid.NewGuid().ToString("N"),
                $"Delete {target.DisplayName}");
            transaction.Add(new DeletePoseNodeMutation(
                m_Graph.GraphId.Value,
                target.NodeId));
            Apply(transaction);
        }

        public Connection Connect(
            Node sourceNode,
            Node targetNode,
            int sourceIndex,
            int targetIndex)
        {
            var source = sourceNode as CharacterPoseCanvasNode ??
                throw new InvalidOperationException("Connection source is not a Pose Canvas node.");
            var target = targetNode as CharacterPoseCanvasNode ??
                throw new InvalidOperationException("Connection target is not a Pose Canvas node.");
            string edgeId = NewEdgeId();
            var transaction = new CharacterPresentationMutationTransaction(
                Guid.NewGuid().ToString("N"),
                "Connect Pose Ports");
            CharacterPosePortDefinition output = RequirePort(source, CharacterPosePortDirection.Output, sourceIndex);
            transaction.Add(new ConnectPosePortMutation(
                m_Graph.GraphId.Value,
                edgeId,
                source.NodeId,
                output.PortId,
                target.NodeId,
                RequirePort(target, CharacterPosePortDirection.Input, targetIndex, output.Kind).PortId));
            Apply(transaction);
            return m_Graph.Connections.SingleOrDefault(
                value => value.EdgeId == edgeId);
        }

        public void RemoveConnection(Connection connection)
        {
            var edge = connection as CharacterPoseCanvasConnection ??
                throw new InvalidOperationException("Removed connection is not a Pose Canvas connection.");
            if (GraphEditorUtility.activeElement == edge)
                GraphEditorUtility.activeElement = null;
            var transaction = new CharacterPresentationMutationTransaction(
                Guid.NewGuid().ToString("N"),
                "Disconnect Pose Ports");
            transaction.Add(new DisconnectPosePortMutation(
                m_Graph.GraphId.Value,
                edge.EdgeId));
            Apply(transaction);
        }

        public Node DuplicateNode(Node clonedNode)
        {
            var clone = clonedNode as CharacterPoseCanvasNode ??
                throw new InvalidOperationException("Duplicated node is not a Pose Canvas node.");
            var node = new CharacterPoseCanvasNode(
                new PoseNodeId(Guid.NewGuid().ToString("N")),
                clone.DisplayName,
                ParadoxNotion.Serialization.JSONSerializer.Clone<CharacterPoseNodePayload>(clone.Payload),
                clone.DynamicPorts.ToArray(),
                clone.position);
            var transaction = new CharacterPresentationMutationTransaction(
                Guid.NewGuid().ToString("N"),
                $"Duplicate {clone.DisplayName}");
            transaction.Add(new CreatePoseNodeMutation(
                m_Graph.GraphId.Value,
                node,
                clone.position));
            Apply(transaction);
            return m_Graph.RequireNode(node.NodeId);
        }

        public Connection DuplicateConnection(Connection original, Node newSource, Node newTarget)
        {
            var edge = original as CharacterPoseCanvasConnection ??
                throw new InvalidOperationException("Duplicated connection is not a Pose Canvas connection.");
            var source = newSource as CharacterPoseCanvasNode ??
                throw new InvalidOperationException("Connection source is not a Pose Canvas node.");
            var target = newTarget as CharacterPoseCanvasNode ??
                throw new InvalidOperationException("Connection target is not a Pose Canvas node.");
            string edgeId = NewEdgeId();
            var transaction = new CharacterPresentationMutationTransaction(
                Guid.NewGuid().ToString("N"),
                "Duplicate Pose Connection");
            transaction.Add(new ConnectPosePortMutation(
                m_Graph.GraphId.Value,
                edgeId,
                source.NodeId,
                edge.SourcePortId,
                target.NodeId,
                edge.TargetPortId));
            Apply(transaction);
            return m_Graph.Connections.SingleOrDefault(
                value => value.EdgeId == edgeId);
        }

        public void SetNodeField(Node node, string fieldId, object value)
        {
            var target = node as CharacterPoseCanvasNode ??
                throw new InvalidOperationException("Edited node is not a Pose Canvas node.");
            var transaction = new CharacterPresentationMutationTransaction(
                Guid.NewGuid().ToString("N"),
                $"Set {target.DisplayName} Field");
            transaction.Add(new SetPoseNodeFieldMutation(
                m_Graph.GraphId.Value,
                target.NodeId,
                fieldId,
                value));
            Apply(transaction);
            GraphEditorUtility.activeElement = m_Graph.RequireNode(target.NodeId);
        }

        public void MoveNode(Node node, Vector2 position)
        {
            var target = (CharacterPoseCanvasNode)node;
            var transaction = new CharacterPresentationMutationTransaction(Guid.NewGuid().ToString("N"), "Move Pose Node");
            transaction.Add(new MovePoseNodeMutation(m_Graph.GraphId.Value, target.NodeId, position));
            Apply(transaction);
        }

        public void RenameNode(Node node, string name)
        {
            var target = (CharacterPoseCanvasNode)node;
            var transaction = new CharacterPresentationMutationTransaction(Guid.NewGuid().ToString("N"), "Rename Pose Node");
            transaction.Add(new SetPoseNodeNameMutation(m_Graph.GraphId.Value, target.NodeId, name));
            Apply(transaction);
        }

        public Connection Reconnect(Connection connection, Node sourceNode, int sourceIndex, Node targetNode, int targetIndex)
        {
            var edge = (CharacterPoseCanvasConnection)connection;
            var source = (CharacterPoseCanvasNode)sourceNode;
            var target = (CharacterPoseCanvasNode)targetNode;
            CharacterPosePortDefinition output = source == edge.sourceNode && sourceIndex < 0
                ? CharacterPoseAuthoringPortProjection.Get(source).Single(value => value.PortId.Equals(edge.SourcePortId))
                : RequirePort(source, CharacterPosePortDirection.Output, sourceIndex);
            PosePortId input = target == edge.targetNode && targetIndex < 0
                ? edge.TargetPortId
                : RequirePort(target, CharacterPosePortDirection.Input, targetIndex, output.Kind).PortId;
            var transaction = new CharacterPresentationMutationTransaction(Guid.NewGuid().ToString("N"), "Reconnect Pose Ports");
            transaction.Add(new DisconnectPosePortMutation(m_Graph.GraphId.Value, edge.EdgeId));
            transaction.Add(new ConnectPosePortMutation(m_Graph.GraphId.Value, edge.EdgeId,
                source.NodeId, output.PortId, target.NodeId, input));
            Apply(transaction);
            return m_Graph.Connections.Single(value => value.EdgeId == edge.EdgeId);
        }

        public GenericMenu BuildNodeCreationMenu(
            NodeCanvas.Framework.Graph.NodeCreationRequestContext request)
        {
            var menu = new GenericMenu();
            IEnumerable<CharacterPoseNodeDefinition> definitions =
                CharacterPoseNodeDefinitionModule.Shared.All
                    .Where(value => value.CanvasCreation != CharacterPoseCanvasCreationKind.DedicatedSurface)
                    .Where(value => value.Capability.Allows(ResolveRole()))
                    .OrderBy(value => value.Capability.Category, StringComparer.Ordinal)
                    .ThenBy(value => value.Capability.DisplayName, StringComparer.Ordinal);
            foreach (CharacterPoseNodeDefinition definition in definitions)
            {
                string category = definition.Capability.Category;
                string label = string.IsNullOrEmpty(category)
                    ? definition.Capability.DisplayName
                    : $"{category}/{definition.Capability.DisplayName}";
                string capabilityIdentity = definition.Capability.CapabilityId.Value;
                if (request.connectSource is CharacterPoseCanvasNode source)
                {
                    CharacterPosePortKind kind = RequirePort(source, CharacterPosePortDirection.Output,
                        request.connectSourcePortIndex).Kind;
                    var candidate = new CharacterPoseCanvasNode(new PoseNodeId(Guid.NewGuid().ToString("N")),
                        definition.Capability.DisplayName, definition.CreateDefaultPayload());
                    CharacterPosePortDefinition[] inputs = CharacterPoseAuthoringPortProjection.Get(candidate)
                        .Where(value => value.Direction == CharacterPosePortDirection.Input).ToArray();
                    for (int i = 0; i < inputs.Length; i++)
                    {
                        if (inputs[i].Kind != kind)
                            continue;
                        int targetIndex = i;
                        menu.AddItem(new GUIContent($"{label}/{inputs[i].Name}"), false,
                            () => CharacterPoseCanvasPortsGUI.Apply(() => CreateNodeFromCapability(capabilityIdentity,
                                request.position, source, request.connectSourcePortIndex, targetIndex)));
                    }
                }
                else
                    menu.AddItem(new GUIContent(label), false,
                        () => CharacterPoseCanvasPortsGUI.Apply(() => CreateNodeFromCapability(capabilityIdentity,
                            request.position, null, -1)));
            }
            if (menu.GetItemCount() == 0)
                menu.AddDisabledItem(new GUIContent("No Pose Node Definitions"));
            return menu;
        }

        static string NewEdgeId() => $"pose-edge-{Guid.NewGuid():N}";

        public bool HandleCommand(string command, Vector2 position)
        {
            var mutation = new CharacterPoseCanvasMutationAdapter { ReadOnly = ReadOnly };
            var document = new CharacterPoseCanvasGraphDocument(m_Owner, m_Graph.GraphId.Value, ResolveRole(), m_Asset.name);
            var binding = new GraphAuthoringProjectionCanvasBinding(document, CharacterPoseGraphAuthoringCapabilities.Catalog,
                mutation, new CharacterPoseCanvasConnectionPolicy(), new CharacterPoseCanvasGraphClipboardCodec(mutation));
            return CharacterPoseCanvasCommands.Handle(binding, m_Graph, command, position);
        }

        public GenericMenu BuildSelectionMenu(Vector2 position) => CharacterPoseCanvasCommands.Menu(HandleCommand, position);

        GraphAuthoringDocumentRoleId ResolveRole()
        {
            if (m_Asset.Graph == m_Graph)
                return CharacterPoseGraphAuthoringCapabilities.RootGraph;
            bool stateGraph = m_Asset.EnumerateStateMachines().Where(value => value != null)
                .SelectMany(value => value.States).Any(value => value.PoseGraphId == m_Graph.GraphId);
            return stateGraph ? CharacterPoseGraphAuthoringCapabilities.StatePoseGraph : CharacterPoseGraphAuthoringCapabilities.Subgraph;
        }

        internal static CharacterPosePortDefinition RequirePort(
            CharacterPoseCanvasNode node,
            CharacterPosePortDirection direction,
            int index,
            CharacterPosePortKind? kind = null)
        {
            CharacterPosePortDefinition[] ports = CharacterPoseAuthoringPortProjection.Get(node)
                .Where(value => value.Direction == direction)
                .ToArray();
            if (index >= 0)
            {
                if (index >= ports.Length || (kind.HasValue && ports[index].Kind != kind.Value))
                    throw new InvalidOperationException($"Pose node '{node.NodeId}' has no compatible {direction} port at index {index}.");
                return ports[index];
            }
            CharacterPosePortDefinition[] matches = ports.Where(value => !kind.HasValue || value.Kind == kind.Value).ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException($"Pose node '{node.NodeId}' requires an explicit {direction} port; {matches.Length} compatible ports exist.");
            return matches[0];
        }
    }
}
