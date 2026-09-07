using System;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
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
}
