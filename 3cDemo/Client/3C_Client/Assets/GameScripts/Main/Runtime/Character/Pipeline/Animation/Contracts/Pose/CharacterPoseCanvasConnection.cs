using System;
using NodeCanvas.Framework;
using ParadoxNotion;
using ParadoxNotion.Design;
using ParadoxNotion.Serialization.FullSerializer;
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

#if UNITY_EDITOR
        public override int SetSourceNode(Node source, int index = -1)
        {
            CharacterPoseCanvasGraph owner = graph as CharacterPoseCanvasGraph;
            if (owner?.EditorWriteRouter == null)
                throw new InvalidOperationException("Pose connection relinking requires its editor session.");
            Connection result = owner.EditorWriteRouter.Reconnect(this, source, index, targetNode, -1);
            return result.sourceNode.outConnections.IndexOf(result);
        }

        public override int SetTargetNode(Node target, int index = -1)
        {
            CharacterPoseCanvasGraph owner = graph as CharacterPoseCanvasGraph;
            if (owner?.EditorWriteRouter == null)
                throw new InvalidOperationException("Pose connection relinking requires its editor session.");
            Connection result = owner.EditorWriteRouter.Reconnect(this, sourceNode, -1, target, index);
            return result.targetNode.inConnections.IndexOf(result);
        }
#endif
    }
}
