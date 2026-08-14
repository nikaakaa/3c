using System;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public sealed class CharacterBlendSpacePoseSourceBinding : CharacterPresentationPoseSourceBinding
    {
        [SerializeField] CharacterAnimationBlendSpaceAsset m_BlendSpace;
        [SerializeField] string m_FootAnalysisIdentity = string.Empty;

        public override PresentationPoseSourceKind SourceKind => PresentationPoseSourceKind.BlendSpace;
        public override UnityEngine.Object SourceAsset => m_BlendSpace;
        public override CharacterAnimationRigDefinition Rig => m_BlendSpace ? m_BlendSpace.Rig : null;
        public override string FootAnalysisIdentity => m_FootAnalysisIdentity ?? string.Empty;
        public CharacterAnimationBlendSpaceAsset BlendSpace => m_BlendSpace;

        public void Configure(
            CharacterBlendSpacePoseSourceSlot slot,
            CharacterAnimationBlendSpaceAsset blendSpace,
            CharacterAnimationRigDefinition rig,
            string footAnalysisIdentity)
        {
            if (!blendSpace || blendSpace.Rig != rig)
                throw new ArgumentException("Blend Space Pose source binding is incomplete.");
            ConfigureCommon(slot);
            m_BlendSpace = blendSpace;
            m_FootAnalysisIdentity = footAnalysisIdentity?.Trim() ?? string.Empty;
            RequireValid(rig);
        }

        public override void RequireValid(CharacterAnimationRigDefinition profileRig)
        {
            base.RequireValid(profileRig);
            if (!m_BlendSpace || m_BlendSpace.Rig != profileRig ||
                !CharacterAnimationBlendSpaceValidator.Validate(m_BlendSpace).IsValid)
                throw new InvalidOperationException($"Blend Space Pose source binding '{name}' is invalid.");
        }
    }
}
