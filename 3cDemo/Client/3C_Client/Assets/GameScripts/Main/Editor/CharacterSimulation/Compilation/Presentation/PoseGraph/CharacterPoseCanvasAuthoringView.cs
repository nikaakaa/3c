using BTSMTL.Authoring.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Editor;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Editor.CharacterSimulation
{
    internal enum CharacterPoseCanvasGraphRole : byte
    {
        Root = 1,
        StatePose = 2,
        Subgraph = 3,
        LinkedPoseEntry = 4,
        AnimationLayer = 5,
        TransitionRule = 6,
        ControlRig = 7
    }

    internal sealed class CharacterPoseCanvasAuthoringView
    {
        readonly CharacterPresentationPoseGraphAsset m_Owner;
        readonly CharacterPoseCanvasGraph[] m_Graphs;
        readonly HashSet<PoseGraphId> m_StatePoseGraphs;

        internal CharacterPoseCanvasAuthoringView(
            CharacterPresentationPoseGraphAsset owner)
        {
            m_Owner = owner ? owner :
                throw new ArgumentNullException(nameof(owner));
            m_Graphs = owner.EnumerateGraphs()
                .Where(value => value)
                .ToArray();
            m_StatePoseGraphs = m_Graphs
                .SelectMany(value => value.Nodes)
                .Select(value => value?.Payload)
                .OfType<CharacterPoseStateMachineNodePayload>()
                .SelectMany(value => value.StateMachine?.States ??
                    Array.Empty<CharacterPoseStateDefinition>())
                .Where(value => value != null && value.PoseGraphId.IsValid)
                .Select(value => value.PoseGraphId)
                .ToHashSet();
        }

        internal CharacterPresentationPoseGraphAsset OwnerAsset => m_Owner;
        internal CharacterPoseCanvasGraph RootGraph => m_Owner.Graph;
        internal IReadOnlyList<CharacterPoseCanvasGraph> Graphs => m_Graphs;
        internal IReadOnlyList<CharacterPresentationPoseSourceSlot> SourceSlots =>
            m_Owner.SourceSlots;

        internal CharacterPoseCanvasGraph RequireGraph(PoseGraphId graphId) =>
            m_Graphs.SingleOrDefault(value => value.GraphId == graphId) ??
            throw new InvalidOperationException(
                $"Pose Canvas graph '{graphId}' does not belong to '{m_Owner.name}'.");

        internal CharacterPoseCanvasNode RequireNode(
            PoseGraphId graphId,
            PoseNodeId nodeId) =>
            RequireGraph(graphId).RequireNode(nodeId);

        internal CharacterPoseNodeDefinition RequireDefinition(
            CharacterPoseCanvasNode node) =>
            CharacterPoseNodeDefinitionModule.Shared.Require(
                node?.Kind ?? throw new ArgumentNullException(nameof(node)));

        internal IReadOnlyList<GraphAuthoringDynamicPortProjection> ProjectPorts(
            CharacterPoseCanvasNode node) =>
            RequireDefinition(node).ProjectPortShape(node);

        internal CharacterPoseCanvasGraphRole ResolveRole(
            CharacterPoseCanvasGraph graph)
        {
            if (ReferenceEquals(graph, RootGraph))
                return CharacterPoseCanvasGraphRole.Root;
            switch (graph.Role)
            {
                case CharacterPoseAuthoringGraphRole.AnimationLayer:
                    return CharacterPoseCanvasGraphRole.AnimationLayer;
                case CharacterPoseAuthoringGraphRole.TransitionRule:
                    return CharacterPoseCanvasGraphRole.TransitionRule;
                case CharacterPoseAuthoringGraphRole.ControlRig:
                    return CharacterPoseCanvasGraphRole.ControlRig;
                case CharacterPoseAuthoringGraphRole.LinkedPoseEntry:
                    return CharacterPoseCanvasGraphRole.LinkedPoseEntry;
                case CharacterPoseAuthoringGraphRole.StatePose:
                    return CharacterPoseCanvasGraphRole.StatePose;
                case CharacterPoseAuthoringGraphRole.Subgraph:
                    return CharacterPoseCanvasGraphRole.Subgraph;
            }
            return m_StatePoseGraphs.Contains(graph.GraphId)
                ? CharacterPoseCanvasGraphRole.StatePose
                : CharacterPoseCanvasGraphRole.Subgraph;
        }

        internal string SourceMapPath(
            CharacterPoseCanvasGraph graph,
            CharacterPoseCanvasNode node) =>
            $"{AssetDatabase.GetAssetPath(m_Owner)}#graph/{graph.GraphId}/node/{node.NodeId}";

        internal IEnumerable<UnityEngine.Object> EnumerateResourceReferences()
        {
            foreach (CharacterPresentationPoseSourceSlot slot in SourceSlots)
                if (slot)
                    yield return slot;
            foreach (CharacterPoseResourceSlot slot in m_Owner.ResourceSlots)
                if (slot)
                    yield return slot;
        }
    }
}
