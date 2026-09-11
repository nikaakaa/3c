using TreeDesigner.Authoring;
using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Editor.CharacterSimulation;
using ThirdPersonCharacter.Pipeline.Animation;
using TreeDesigner.Editor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    public sealed class CharacterPoseCanvasDefinitionProjection
    {
        readonly CharacterPoseNodeDefinition m_Definition;
        readonly CharacterPoseCanvasNode m_Node;

        CharacterPoseCanvasDefinitionProjection(
            CharacterPoseNodeDefinition definition,
            CharacterPoseCanvasNode node)
        {
            m_Definition = definition ??
                throw new ArgumentNullException(nameof(definition));
            m_Node = node;
            if (node != null && node.Kind != definition.Kind)
                throw new ArgumentException(
                    "Pose Canvas node does not match its Node Definition.",
                    nameof(node));
        }

        public CharacterPoseNodeKind Kind => m_Definition.Kind;
        public GraphAuthoringCapabilityDescriptor Capability =>
            m_Definition.Capability;
        public CharacterPoseExecutionDomain ExecutionDomain =>
            m_Definition.ExecutionDomain;
        public IReadOnlyList<GraphAuthoringDynamicPortProjection> Ports =>
            m_Node == null
                ? m_Definition.ProjectDeclaredPortShape(
                    m_Definition.CreateDefaultPayload())
                : m_Definition.ProjectPortShape(m_Node);
        public IReadOnlyList<GraphAuthoringCommandDescriptor> Commands =>
            Capability.Commands;

        public static CharacterPoseCanvasDefinitionProjection For(
            CharacterPoseCanvasNode node) =>
            new CharacterPoseCanvasDefinitionProjection(
                CharacterPoseNodeDefinitionModule.Shared.Require(
                    node?.Kind ?? throw new ArgumentNullException(nameof(node))),
                node);

        public static CharacterPoseCanvasDefinitionProjection For(
            CharacterPoseNodeKind kind) =>
            new CharacterPoseCanvasDefinitionProjection(
                CharacterPoseNodeDefinitionModule.Shared.Require(kind),
                null);

        public static IReadOnlyList<GraphAuthoringCapabilityDescriptor> CreateMenu(
            GraphAuthoringDocumentRoleId role) =>
            CharacterPoseGraphCapabilityProjector.Catalog.GetAllowed(
                CharacterPoseGraphAuthoringCapabilities.Domain,
                role);

        public GraphAuthoringNodeProjection ProjectNode(
            PoseNodeId nodeId,
            string displayName,
            Vector2 position,
            string status = "")
        {
            if (!nodeId.IsValid)
                throw new ArgumentException(
                    "Pose Canvas node identity is invalid.",
                    nameof(nodeId));
            return new GraphAuthoringNodeProjection(
                new GraphAuthoringElementId(nodeId.Value),
                Capability.CapabilityId,
                displayName,
                position,
                m_Node == null
                    ? Array.Empty<GraphAuthoringDynamicPortProjection>()
                    : m_Definition.ProjectAdditionalPorts(m_Node),
                status);
        }
    }
}
