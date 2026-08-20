using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    [AddComponentMenu("")]
    public sealed class CharacterAnimationClipAuthoringCurveReceiver : MonoBehaviour
    {
        [SerializeField] float m_LocomotionPhase;
        [SerializeField, Range(0f, 1f)] float m_FootPlacementWeight;
    }
}
