using System;
using System.Collections.Generic;
using System.Linq;
using NodeCanvas.Editor;
using NodeCanvas.Framework;
using ThirdPersonCharacter.Editor.CharacterSimulation;
using ThirdPersonCharacter.Pipeline.Animation;
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

        public CharacterPoseCanvasEditorWriteSession(
            CharacterPresentationPoseGraphAsset asset,
            CharacterPoseCanvasGraph graph)
        {
            if (asset == null)
                throw new ArgumentNullException(nameof(asset));
            m_Owner = new CharacterPoseGraphAssetMutationOwner(asset);
            m_Service = new CharacterPresentationMutationService();
            m_Graph = graph ? graph : throw new ArgumentNullException(nameof(graph));
        }

        public CharacterPoseCanvasGraph Graph => m_Graph;

        public Node CreateNode(Type nodeType, Vector2 position) =>
            throw new InvalidOperationException(
                "Pose Canvas nodes are created from their Node Definition; use the Definition-driven creation menu.");

        public Node CreateNodeFromCapability(
            string capabilityIdentity,
            Vector2 position,
            Node connectSource,
            int connectSourcePortIndex)
        {
            CharacterPoseNodeDefinition definition =
                CharacterPoseNodeDefinitionModule.Shared.RequireCapability(
                    capabilityIdentity);
            if (definition.CanvasCreation == CharacterPoseCanvasCreationKind.DedicatedSurface)
                throw new InvalidOperationException(
                    $"Pose node '{definition.Capability.DisplayName}' is created from its dedicated authoring surface.");
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
                PosePortId sourcePort = RequireOutputPort(source, connectSourcePortIndex);
                PosePortId targetPort = RequireInputPort(node);
                transaction.Add(new ConnectPosePortMutation(
                    m_Graph.GraphId.Value,
                    NewEdgeId(),
                    source.NodeId,
                    sourcePort,
                    node.NodeId,
                    targetPort));
            }
            m_Service.ApplyGraphMutationsInPlace(m_Owner, transaction);
            CharacterPoseCanvasNode created = m_Graph.RequireNode(node.NodeId);
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
            m_Service.ApplyGraphMutationsInPlace(m_Owner, transaction);
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
            transaction.Add(new ConnectPosePortMutation(
                m_Graph.GraphId.Value,
                edgeId,
                source.NodeId,
                RequireOutputPort(source, sourceIndex),
                target.NodeId,
                RequireInputPort(target)));
            m_Service.ApplyGraphMutationsInPlace(m_Owner, transaction);
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
            m_Service.ApplyGraphMutationsInPlace(m_Owner, transaction);
        }

        public GenericMenu BuildNodeCreationMenu(
            NodeCanvas.Framework.Graph.NodeCreationRequestContext request)
        {
            var menu = new GenericMenu();
            IEnumerable<CharacterPoseNodeDefinition> definitions =
                CharacterPoseNodeDefinitionModule.Shared.All
                    .Where(value => value.CanvasCreation != CharacterPoseCanvasCreationKind.DedicatedSurface)
                    .OrderBy(value => value.Capability.Category, StringComparer.Ordinal)
                    .ThenBy(value => value.Capability.DisplayName, StringComparer.Ordinal);
            foreach (CharacterPoseNodeDefinition definition in definitions)
            {
                string category = definition.Capability.Category;
                string label = string.IsNullOrEmpty(category)
                    ? definition.Capability.DisplayName
                    : $"{category}/{definition.Capability.DisplayName}";
                string capabilityIdentity = definition.Capability.CapabilityId.Value;
                menu.AddItem(
                    new GUIContent(label),
                    false,
                    () => CreateNodeFromCapability(
                        capabilityIdentity,
                        request.position,
                        request.connectSource,
                        request.connectSourcePortIndex));
            }
            if (menu.GetItemCount() == 0)
                menu.AddDisabledItem(new GUIContent("No Pose Node Definitions"));
            return menu;
        }

        static string NewEdgeId() => $"pose-edge:{Guid.NewGuid():N}";

        static PosePortId RequireOutputPort(
            CharacterPoseCanvasNode node,
            int sourceIndex)
        {
            IReadOnlyList<CharacterPosePortDefinition> ports =
                CharacterPoseAuthoringPortProjection.Get(node);
            CharacterPosePortDefinition[] outputs = ports
                .Where(value => value.Direction == CharacterPosePortDirection.Output)
                .ToArray();
            if (outputs.Length == 0)
                throw new InvalidOperationException(
                    $"Pose node '{node.NodeId}' has no output port.");
            return (sourceIndex >= 0 && sourceIndex < outputs.Length
                ? outputs[sourceIndex]
                : outputs[0]).PortId;
        }

        static PosePortId RequireInputPort(CharacterPoseCanvasNode node)
        {
            IReadOnlyList<CharacterPosePortDefinition> ports =
                CharacterPoseAuthoringPortProjection.Get(node);
            CharacterPosePortDefinition input = ports.FirstOrDefault(
                    value => value.Direction == CharacterPosePortDirection.Input) ??
                throw new InvalidOperationException(
                    $"Pose node '{node.NodeId}' has no input port.");
            return input.PortId;
        }
    }
}
