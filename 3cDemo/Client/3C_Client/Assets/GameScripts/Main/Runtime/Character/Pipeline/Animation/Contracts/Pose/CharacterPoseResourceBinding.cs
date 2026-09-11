using System;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;
using ThirdPersonCharacter.Pipeline.Motion.RootMotion;
using ThirdPersonCharacter.Pipeline.Presentation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public enum CharacterPoseResourceKind : byte
    {
        BlendPolicy = 1,
        InertializationPolicy = 2,
        BoneMask = 3,
        RootMotionCurve = 4,
        FootPlacementProfile = 5,
        FootPlacementCalibration = 6,
        MotionMatchingBinding = 7,
        BlendCurve = 8,
        BlendProfile = 9
    }

    [Serializable]
    public sealed class CharacterPoseResourceBinding
    {
        [SerializeField] CharacterPoseResourceSlot m_Slot;
        [SerializeField] UnityEngine.Object m_Resource;

        public CharacterPoseResourceSlot Slot => m_Slot;
        public UnityEngine.Object Resource => m_Resource;

        public void Configure(CharacterPoseResourceSlot slot, UnityEngine.Object resource)
        {
            if (!slot || !slot.Accepts(resource))
                throw new ArgumentException("Pose Resource Binding is incomplete.");
            m_Slot = slot;
            m_Resource = resource;
        }

        public void RequireValid()
        {
            if (!m_Slot || !m_Slot.Accepts(m_Resource))
                throw new InvalidOperationException("Pose Resource Binding is invalid.");
            m_Slot.RequireValid();
        }
    }
}
