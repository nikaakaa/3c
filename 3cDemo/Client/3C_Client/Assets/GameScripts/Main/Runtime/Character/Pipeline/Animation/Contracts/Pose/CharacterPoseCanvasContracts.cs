using System;
using System.Collections.Generic;
using System.Linq;
using NodeCanvas.Framework;
using ParadoxNotion;
using ParadoxNotion.Design;
using ParadoxNotion.Serialization.FullSerializer;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonCharacter.Pipeline.Motion.RootMotion;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    [Serializable]
    public sealed class CharacterPoseCanvasConnection : Connection
    {
        [SerializeField] string m_EdgeId = string.Empty;
        [SerializeField] string m_SourceNodeId = string.Empty;
        [SerializeField] string m_SourcePortId = string.Empty;
        [SerializeField] string m_TargetNodeId = string.Empty;
        [SerializeField] string m_TargetPortId = string.Empty;

        public string EdgeId => m_EdgeId ?? string.Empty;
        public PoseNodeId SourceNodeId => string.IsNullOrWhiteSpace(m_SourceNodeId)
            ? SourceNode?.NodeId ?? default
            : new PoseNodeId(m_SourceNodeId);
        public PosePortId SourcePortId => string.IsNullOrWhiteSpace(m_SourcePortId)
            ? default
            : new PosePortId(m_SourcePortId);
        public PoseNodeId TargetNodeId => string.IsNullOrWhiteSpace(m_TargetNodeId)
            ? TargetNode?.NodeId ?? default
            : new PoseNodeId(m_TargetNodeId);
        public PosePortId TargetPortId => string.IsNullOrWhiteSpace(m_TargetPortId)
            ? default
            : new PosePortId(m_TargetPortId);
        public CharacterPoseCanvasNode SourceNode => sourceNode as CharacterPoseCanvasNode;
        public CharacterPoseCanvasNode TargetNode => targetNode as CharacterPoseCanvasNode;

        public CharacterPoseCanvasConnection() { }

        public CharacterPoseCanvasConnection(
            string edgeId,
            CharacterPoseCanvasNode sourceNode,
            PosePortId sourcePortId,
            CharacterPoseCanvasNode targetNode,
            PosePortId targetPortId)
        {
            SetAuthoring(edgeId, sourceNode, sourcePortId, targetNode, targetPortId);
        }

        internal void SetAuthoring(
            string edgeId,
            CharacterPoseCanvasNode source,
            PosePortId sourcePortId,
            CharacterPoseCanvasNode target,
            PosePortId targetPortId)
        {
            m_EdgeId = PoseIdentity.Require(edgeId, nameof(edgeId));
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (target == null)
                throw new ArgumentNullException(nameof(target));
            if (!sourcePortId.IsValid)
                throw new ArgumentException("Pose Canvas source port identity is invalid.", nameof(sourcePortId));
            if (!targetPortId.IsValid)
                throw new ArgumentException("Pose Canvas target port identity is invalid.", nameof(targetPortId));
            m_SourcePortId = sourcePortId.Value;
            m_TargetPortId = targetPortId.Value;
            m_SourceNodeId = source.NodeId.Value;
            m_TargetNodeId = target.NodeId.Value;
            sourceNode = source;
            targetNode = target;
        }

        public CharacterPoseCanvasConnection(
            string edgeId,
            PoseNodeId sourceNodeId,
            PosePortId sourcePortId,
            PoseNodeId targetNodeId,
            PosePortId targetPortId)
        {
            m_EdgeId = PoseIdentity.Require(edgeId, nameof(edgeId));
            m_SourceNodeId = sourceNodeId.IsValid
                ? sourceNodeId.Value
                : throw new ArgumentException("Pose Canvas source node identity is invalid.", nameof(sourceNodeId));
            m_SourcePortId = sourcePortId.IsValid
                ? sourcePortId.Value
                : throw new ArgumentException("Pose Canvas source port identity is invalid.", nameof(sourcePortId));
            m_TargetNodeId = targetNodeId.IsValid
                ? targetNodeId.Value
                : throw new ArgumentException("Pose Canvas target node identity is invalid.", nameof(targetNodeId));
            m_TargetPortId = targetPortId.IsValid
                ? targetPortId.Value
                : throw new ArgumentException("Pose Canvas target port identity is invalid.", nameof(targetPortId));
        }

        internal void BindNodes(
            CharacterPoseCanvasNode source,
            CharacterPoseCanvasNode target)
        {
            if (source == null || target == null)
                throw new ArgumentNullException(source == null ? nameof(source) : nameof(target));
            if (SourceNodeId != source.NodeId || TargetNodeId != target.NodeId)
                throw new InvalidOperationException("Pose Canvas connection endpoint identity is invalid.");
            sourceNode = source;
            targetNode = target;
        }
    }

    [Serializable]
    public sealed class CharacterPoseCanvasNode : Node
    {
        [SerializeField] string m_NodeId = string.Empty;
        [SerializeField] string m_DisplayName = string.Empty;
        [SerializeField, fsSerializeAsReference] CharacterPoseNodePayload m_Payload;
        [SerializeField] CharacterPoseDynamicPort[] m_DynamicPorts = Array.Empty<CharacterPoseDynamicPort>();

        public PoseNodeId NodeId => string.IsNullOrWhiteSpace(m_NodeId)
            ? default
            : new PoseNodeId(m_NodeId);
        public string DisplayName => m_DisplayName ?? string.Empty;
        public CharacterPoseNodePayload Payload => m_Payload;
        public CharacterPoseNodeKind Kind =>
            m_Payload?.Kind ??
            throw new InvalidOperationException($"Pose Canvas node '{NodeId}' has no typed payload.");
        public IReadOnlyList<CharacterPoseDynamicPort> DynamicPorts =>
            m_DynamicPorts ?? Array.Empty<CharacterPoseDynamicPort>();

        public override string name
        {
            get => string.IsNullOrWhiteSpace(m_DisplayName) ? Kind.ToString() : m_DisplayName;
            set => m_DisplayName = value ?? string.Empty;
        }

        public override int maxInConnections => -1;
        public override int maxOutConnections => -1;
        public override Type outConnectionType => typeof(CharacterPoseCanvasConnection);
        public override bool allowAsPrime => false;
        public override bool canSelfConnect => false;
        public override Alignment2x2 commentsAlignment => Alignment2x2.Default;
        public override Alignment2x2 iconAlignment => Alignment2x2.Default;

        public T RequirePayload<T>() where T : CharacterPoseNodePayload =>
            m_Payload as T ??
            throw new InvalidOperationException(
                $"Pose Canvas node '{NodeId}' does not own payload '{typeof(T).Name}'.");

        public AnimationChannelId AnimationChannelId => m_Payload switch
        {
            CharacterActionPlaybackInputPosePayload value => value.AnimationChannelId,
            CharacterAnimationSlotPosePayload value => value.AnimationChannelId,
            _ => default
        };
        public PoseParameterId ParameterId =>
            (m_Payload as CharacterProgramParameterInputPosePayload)?.ParameterId ?? default;
        public AnimationSelectionAvailabilityPolicy SelectionAvailability =>
            (m_Payload as CharacterAnimationSlotPosePayload)?.SelectionAvailability ??
            AnimationSelectionAvailabilityPolicy.RequireSelection;
        public CharacterAnimationBlendSpaceInputRangePolicy BlendSpaceInputRangePolicy =>
            (m_Payload as CharacterBlendSpacePlayerPosePayload)?.InputRangePolicy ??
            CharacterAnimationBlendSpaceInputRangePolicy.Clamp;
        public CharacterAnimationBlendPolicy BlendPolicy => m_Payload switch
        {
            CharacterAnimationSlotPosePayload value => value.BlendPolicy,
            CharacterBlendStackPosePayload value => value.BlendPolicy,
            _ => null
        };
        public CharacterPoseInertializationPolicy InertializationPolicy =>
            (m_Payload as CharacterInertializationPosePayload)?.Policy;
        public CharacterAnimationBoneMaskAsset BoneMask =>
            (m_Payload as CharacterLayeredBoneBlendPosePayload)?.BoneMask;
        public float Weight => m_Payload switch
        {
            CharacterBlendPosePayload value => value.Weight,
            CharacterLayeredBoneBlendPosePayload value => value.Weight,
            CharacterAdditivePosePayload value => value.Weight,
            _ => 1f
        };
        public IReadOnlyList<CharacterPoseParameterPolicy> ParameterPolicies =>
            (m_Payload as CharacterPoseParameterResolvePayload)?.Policies ??
            Array.Empty<CharacterPoseParameterPolicy>();
        public string AdditiveReferencePoseId =>
            (m_Payload as CharacterAdditivePosePayload)?.ReferencePoseId ?? string.Empty;
        public AdditiveReferenceSpace AdditiveReferenceSpace =>
            (m_Payload as CharacterAdditivePosePayload)?.ReferenceSpace ??
            global::ThirdPersonCharacter.Pipeline.Animation.AdditiveReferenceSpace.Local;
        public AdditiveScalePolicy AdditiveScalePolicy =>
            (m_Payload as CharacterAdditivePosePayload)?.ScalePolicy ??
            global::ThirdPersonCharacter.Pipeline.Animation.AdditiveScalePolicy.Multiply;
        public AnimationBoneId BoneId =>
            (m_Payload as CharacterModifyBonePosePayload)?.BoneId ?? default;
        public ModifyBoneReferenceSpace ModifyBoneReferenceSpace =>
            (m_Payload as CharacterModifyBonePosePayload)?.ReferenceSpace ??
            global::ThirdPersonCharacter.Pipeline.Animation.ModifyBoneReferenceSpace.Local;
        public ModifyBoneOperationMask ModifyBoneOperations =>
            (m_Payload as CharacterModifyBonePosePayload)?.Operations ??
            ModifyBoneOperationMask.None;
        public Vector3 ModifyPosition =>
            (m_Payload as CharacterModifyBonePosePayload)?.Position ?? Vector3.zero;
        public Quaternion ModifyRotation =>
            (m_Payload as CharacterModifyBonePosePayload)?.Rotation ?? Quaternion.identity;
        public Vector3 ModifyScale =>
            (m_Payload as CharacterModifyBonePosePayload)?.Scale ?? Vector3.one;
        public RootMotionCurveAsset RootOrientationYawCurve =>
            (m_Payload as CharacterRootOrientationWarpPosePayload)?.YawCurve;
        public IReadOnlyList<CharacterPoseBoneIkGoalBinding> PoseBoneIkGoalBindings =>
            (m_Payload as CharacterPoseBoneIkGoalsPayload)?.Bindings ??
            Array.Empty<CharacterPoseBoneIkGoalBinding>();
        public CharacterFootPlacementProfile FootPlacementProfile =>
            (m_Payload as CharacterFootPlacementPosePayload)?.Profile;
        public CharacterFootPlacementRigCalibration FootPlacementCalibration =>
            (m_Payload as CharacterFootPlacementPosePayload)?.Calibration;
        public LinkedPoseGroupId LinkedPoseGroupId =>
            (m_Payload as CharacterLinkedPoseCallPayload)?.GroupId ?? default;
        public LinkedPoseInterfaceId LinkedPoseInterfaceId =>
            (m_Payload as CharacterLinkedPoseCallPayload)?.InterfaceId ?? default;
        public LinkedPoseEntryId LinkedPoseEntryId =>
            (m_Payload as CharacterLinkedPoseCallPayload)?.EntryId ?? default;
        public CharacterPoseSubgraphReference Subgraph =>
            (m_Payload as CharacterPoseSubgraphPayload)?.Subgraph;
        public CharacterPresentationPoseSourceSlot PresentationPoseSourceSlot => m_Payload switch
        {
            CharacterSelectedPosePlayerPayload value => value.SourceSlot,
            CharacterBlendSpacePlayerPosePayload value => value.SourceSlot,
            CharacterClipPlayerPosePayload value => value.SourceSlot,
            CharacterBlendStackPosePayload value => value.SourceSlot,
            _ => null
        };
        public float ClipPlayRate =>
            (m_Payload as CharacterClipPlayerPosePayload)?.PlayRate ?? 1f;
        public float ClipInitialTime =>
            (m_Payload as CharacterClipPlayerPosePayload)?.InitialTime ?? 0f;
        public CharacterClipPlayerClockSource ClipClockSource =>
            (m_Payload as CharacterClipPlayerPosePayload)?.ClockSource ??
            CharacterClipPlayerClockSource.PresentationDelta;
        public CharacterPoseStateMachineDefinition PoseStateMachine =>
            (m_Payload as CharacterPoseStateMachineNodePayload)?.StateMachine;
        public AnimationSlotId AnimationSlotId =>
            (m_Payload as CharacterAnimationSlotPosePayload)?.SlotId ?? default;
        public string AnimationSlotRoutingOwnerId =>
            AnimationSlotId.IsValid ? $"animation-slot/{AnimationSlotId}" : string.Empty;
        public bool AnimationSlotAllowEmpty =>
            m_Payload is CharacterAnimationSlotPosePayload value &&
            value.SelectionAvailability == AnimationSelectionAvailabilityPolicy.AllowEmpty;
        public int AnimationSlotBlendStackCapacity =>
            m_Payload is CharacterAnimationSlotPosePayload value && value.BlendPolicy
                ? value.BlendPolicy.StackPolicy.MaxActiveSourceEntries
                : 0;

        public CharacterPoseCanvasNode() { }

        public CharacterPoseCanvasNode(
            PoseNodeId nodeId,
            string displayName,
            CharacterPoseNodePayload payload,
            CharacterPoseDynamicPort[] dynamicPorts = null,
            Vector2 position = default)
        {
            SetAuthoring(nodeId, displayName, payload, dynamicPorts);
            this.position = position;
        }

        internal void SetAuthoring(
            PoseNodeId nodeId,
            string displayName,
            CharacterPoseNodePayload payload,
            CharacterPoseDynamicPort[] dynamicPorts = null)
        {
            m_NodeId = nodeId.IsValid
                ? nodeId.Value
                : throw new ArgumentException("Pose Canvas node identity is invalid.", nameof(nodeId));
            m_DisplayName = displayName ?? string.Empty;
            m_Payload = payload ?? throw new ArgumentNullException(nameof(payload));
            m_DynamicPorts = dynamicPorts ?? Array.Empty<CharacterPoseDynamicPort>();
        }

        public CharacterPoseCanvasNode CloneAuthoring() =>
            new CharacterPoseCanvasNode(
                NodeId,
                DisplayName,
                Payload,
                DynamicPorts.ToArray(),
                position);
    }

    [Serializable]
    public sealed class CharacterPoseGraphLayoutEntry
    {
        [SerializeField] string m_NodeId = string.Empty;
        [SerializeField] Vector2 m_Position;

        public PoseNodeId NodeId => string.IsNullOrWhiteSpace(m_NodeId)
            ? default
            : new PoseNodeId(m_NodeId);
        public Vector2 Position => m_Position;

        public CharacterPoseGraphLayoutEntry() { }

        public CharacterPoseGraphLayoutEntry(PoseNodeId nodeId, Vector2 position)
        {
            m_NodeId = nodeId.IsValid
                ? nodeId.Value
                : throw new ArgumentException("Pose Node identity is invalid.", nameof(nodeId));
            m_Position = position;
        }
    }

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

        public new Node AddNode(Type nodeType, Vector2 pos = default) =>
            throw new InvalidOperationException("Pose Canvas nodes must be changed through the Presentation Mutation chain.");

        public new T AddNode<T>(Vector2 pos = default) where T : Node =>
            throw new InvalidOperationException("Pose Canvas nodes must be changed through the Presentation Mutation chain.");

        public new void RemoveNode(Node node, bool recordUndo = true, bool force = false) =>
            throw new InvalidOperationException("Pose Canvas nodes must be changed through the Presentation Mutation chain.");

        public new Connection ConnectNodes(
            Node sourceNode,
            Node targetNode,
            int sourceIndex = -1,
            int targetIndex = -1) =>
            throw new InvalidOperationException("Pose Canvas connections must be changed through the Presentation Mutation chain.");

        public new void RemoveConnection(Connection connection, bool recordUndo = true) =>
            throw new InvalidOperationException("Pose Canvas connections must be changed through the Presentation Mutation chain.");

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
}
