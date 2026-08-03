using System;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public sealed class CharacterBlendSpacePoseSourceBinding : CharacterPresentationPoseSourceBinding
    {
        [SerializeField] CharacterAnimationBlendSpaceAsset m_BlendSpace;

        public override PresentationPoseSourceKind SourceKind => PresentationPoseSourceKind.BlendSpace;
        public override UnityEngine.Object SourceAsset => m_BlendSpace;
        public CharacterAnimationBlendSpaceAsset BlendSpace => m_BlendSpace;

        public void Configure(
            CharacterBlendSpacePoseSourceSlot slot,
            CharacterAnimationBlendSpaceAsset blendSpace,
            CharacterAnimationRigDefinition rig,
            string footAnalysisIdentity)
        {
            if (!blendSpace || blendSpace.Rig != rig)
                throw new ArgumentException("Blend Space Pose source binding is incomplete.");
            ConfigureCommon(slot, rig, footAnalysisIdentity);
            m_BlendSpace = blendSpace;
            RequireValid(rig);
        }

        public override void RequireValid(CharacterAnimationRigDefinition profileRig)
        {
            base.RequireValid(profileRig);
            if (!m_BlendSpace || m_BlendSpace.Rig != Rig ||
                !CharacterAnimationBlendSpaceValidator.Validate(m_BlendSpace).IsValid)
                throw new InvalidOperationException($"Blend Space Pose source binding '{name}' is invalid.");
        }
    }
}
