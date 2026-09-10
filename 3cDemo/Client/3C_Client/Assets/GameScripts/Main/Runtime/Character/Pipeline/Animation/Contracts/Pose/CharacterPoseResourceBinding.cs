using System;
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
        FootPlacementCalibration = 6
    }

    public sealed class CharacterPoseResourceSlot : ScriptableObject
    {
        [SerializeField] CharacterPoseResourceKind m_Kind;

        public CharacterPoseResourceKind Kind => m_Kind;

        internal void Configure(CharacterPoseResourceKind kind)
        {
            if (!Enum.IsDefined(typeof(CharacterPoseResourceKind), kind))
                throw new ArgumentOutOfRangeException(nameof(kind));
            m_Kind = kind;
        }

        public bool Accepts(UnityEngine.Object resource) =>
            resource && Kind switch
            {
                CharacterPoseResourceKind.BlendPolicy => resource is CharacterAnimationBlendPolicy,
                CharacterPoseResourceKind.InertializationPolicy => resource is CharacterPoseInertializationPolicy,
                CharacterPoseResourceKind.BoneMask => resource is CharacterAnimationBoneMaskAsset,
                CharacterPoseResourceKind.RootMotionCurve => resource is RootMotionCurveAsset,
                CharacterPoseResourceKind.FootPlacementProfile => resource is CharacterFootPlacementProfile,
                CharacterPoseResourceKind.FootPlacementCalibration => resource is CharacterFootPlacementRigCalibration,
                _ => false
            };

        public void RequireValid()
        {
            if (string.IsNullOrWhiteSpace(name) ||
                !Enum.IsDefined(typeof(CharacterPoseResourceKind), Kind))
            {
                throw new InvalidOperationException("Pose Resource Slot is invalid.");
            }
        }
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
