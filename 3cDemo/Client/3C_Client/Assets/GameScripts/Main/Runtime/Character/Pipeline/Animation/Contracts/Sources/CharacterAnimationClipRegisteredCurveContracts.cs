using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public static class CharacterAnimationClipRegisteredCurveChannels
    {
        public const string LocomotionPhase = "presentation.locomotion-phase";
        public const string FootPlacementWeight = "presentation.foot-placement-weight";
        public const string LocomotionPhaseProperty = "m_LocomotionPhase";
        public const string FootPlacementWeightProperty = "m_FootPlacementWeight";
    }

    [AddComponentMenu("")]
    public sealed class CharacterAnimationClipAuthoringCurveReceiver : MonoBehaviour
    {
        [SerializeField] float m_LocomotionPhase;
        [SerializeField, Range(0f, 1f)] float m_FootPlacementWeight;
    }
}
