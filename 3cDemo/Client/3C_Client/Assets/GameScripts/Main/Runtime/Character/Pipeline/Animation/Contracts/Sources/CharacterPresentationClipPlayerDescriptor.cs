using System;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    [Serializable]
    public sealed class CharacterPresentationClipPlayerDescriptor
    {
        public const string SchemaVersion = "character-presentation-clip-player/v6";

        [SerializeField] string m_SchemaVersion = SchemaVersion;
        [SerializeField] int m_Index;
        [SerializeField] string m_NodeId = string.Empty;
        [SerializeField] int m_PresentationPoseSourceIndex = -1;
        [SerializeField] float m_PlayRate = 1f;
        [SerializeField] float m_InitialTime;
        [SerializeField] bool m_LoopAnimation;
        [SerializeField] bool m_IsLocomotionParticipant;
        [SerializeField] int m_PlayerIndex = -1;

        public CharacterPresentationClipPlayerDescriptor(
            int index,
            PoseNodeId nodeId,
            PresentationPoseSourceIndex presentationPoseSourceIndex,
            float playRate,
            float initialTime,
            bool loopAnimation,
            bool isLocomotionParticipant,
            int playerIndex)
        {
            if (index < 0 || !nodeId.IsValid || !presentationPoseSourceIndex.IsValid ||
                !float.IsFinite(playRate) || playRate <= 0f ||
                !float.IsFinite(initialTime) || initialTime < 0f || playerIndex < 0)
            {
                throw new ArgumentException("Compiled Clip Player descriptor is invalid.");
            }
            m_Index = index;
            m_NodeId = nodeId.Value;
            m_PresentationPoseSourceIndex = presentationPoseSourceIndex.Value;
            m_PlayRate = playRate;
            m_InitialTime = initialTime;
            m_LoopAnimation = loopAnimation;
            m_IsLocomotionParticipant = isLocomotionParticipant;
            m_PlayerIndex = playerIndex;
        }

        public int Index => m_Index;
        public string Version => m_SchemaVersion ?? string.Empty;
        public PoseNodeId NodeId => new PoseNodeId(m_NodeId);
        public PresentationPoseSourceIndex PresentationPoseSourceIndex =>
            m_PresentationPoseSourceIndex < 0
                ? default
                : new PresentationPoseSourceIndex(m_PresentationPoseSourceIndex);
        public float PlayRate => m_PlayRate;
        public float InitialTime => m_InitialTime;
        public bool LoopAnimation => m_LoopAnimation;
        public bool IsLocomotionParticipant => m_IsLocomotionParticipant;
        public int PlayerIndex => m_PlayerIndex;

        public void RequireValid()
        {
            if (!string.Equals(Version, SchemaVersion, StringComparison.Ordinal) ||
                Index < 0 || !NodeId.IsValid || !PresentationPoseSourceIndex.IsValid ||
                !float.IsFinite(PlayRate) || PlayRate <= 0f ||
                !float.IsFinite(InitialTime) || InitialTime < 0f || PlayerIndex < 0)
            {
                throw new InvalidOperationException("Compiled Clip Player descriptor is invalid.");
            }
        }
    }
}
