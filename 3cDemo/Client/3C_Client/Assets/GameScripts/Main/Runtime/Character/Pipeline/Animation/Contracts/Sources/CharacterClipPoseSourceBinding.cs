using System;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public sealed class CharacterClipPoseSourceBinding : CharacterPresentationPoseSourceBinding
    {
        [SerializeField] AnimationClip m_Clip;

        public override PresentationPoseSourceKind SourceKind => PresentationPoseSourceKind.Clip;
        public override UnityEngine.Object SourceAsset => m_Clip;
        public AnimationClip Clip => m_Clip;

        public void Configure(CharacterClipPoseSourceSlot slot, AnimationClip clip)
        {
            if (!clip)
                throw new ArgumentException("Clip Pose source binding is incomplete.");
            ConfigureCommon(slot);
            m_Clip = clip;
        }

        public override void RequireValid(CharacterAnimationRigDefinition profileRig)
        {
            base.RequireValid(profileRig);
            if (!m_Clip || !float.IsFinite(m_Clip.length) || m_Clip.length <= 0f)
                throw new InvalidOperationException($"Clip Pose source binding '{name}' is invalid.");
        }
    }
}
