using System;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    [CreateAssetMenu(fileName = "CharacterPresentationPoseGraph", menuName = "3C/Character/Presentation Pose Graph")]
    public sealed class CharacterPresentationPoseGraphAsset : ScriptableObject
    {
        [SerializeField] CharacterPoseGraphData m_Graph = new CharacterPoseGraphData();

        public CharacterPoseGraphData Graph => m_Graph;

        public void SetGraph(CharacterPoseGraphData graph)
        {
            m_Graph = graph ?? throw new ArgumentNullException(nameof(graph));
        }
    }
}
