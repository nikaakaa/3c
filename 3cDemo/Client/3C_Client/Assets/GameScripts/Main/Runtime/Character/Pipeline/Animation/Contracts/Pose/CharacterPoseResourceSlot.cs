using System;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;
using ThirdPersonCharacter.Pipeline.Motion.RootMotion;
using ThirdPersonCharacter.Pipeline.Presentation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public sealed class CharacterPoseResourceSlot : ScriptableObject
    {
        [SerializeField] CharacterPoseResourceKind m_Kind;

        public static CharacterPoseResourceSlot Create(CharacterPoseResourceKind kind)
        {
            if (!Enum.IsDefined(typeof(CharacterPoseResourceKind), kind))
                throw new ArgumentOutOfRangeException(nameof(kind));
            CharacterPoseResourceSlot slot = CreateInstance<CharacterPoseResourceSlot>();
            slot.m_Kind = kind;
            return slot;
        }

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
                CharacterPoseResourceKind.MotionMatchingBinding => resource is CharacterMotionMatchingBinding,
                CharacterPoseResourceKind.BlendCurve => resource is CharacterAnimationBlendCurveAsset,
                CharacterPoseResourceKind.BlendProfile => resource is CharacterAnimationBlendProfile,
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
}
