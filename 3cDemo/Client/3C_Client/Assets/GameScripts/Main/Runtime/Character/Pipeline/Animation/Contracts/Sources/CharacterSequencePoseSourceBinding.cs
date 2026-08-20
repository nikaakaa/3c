using System;
using BTSMTL.Timeline;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public sealed class CharacterSequencePoseSourceBinding : CharacterPresentationPoseSourceBinding
    {
        [SerializeField] CharacterAnimationSequenceAsset m_Sequence;

        public override PresentationPoseSourceKind SourceKind => PresentationPoseSourceKind.Clip;
        public override UnityEngine.Object SourceAsset => m_Sequence;
        public override CharacterAnimationRigDefinition Rig => m_Sequence ? m_Sequence.Rig : null;
        public override string FootAnalysisIdentity => m_Sequence ? m_Sequence.FootAnalysisIdentity : string.Empty;
        public CharacterAnimationSequenceAsset Sequence => m_Sequence;
        public UnityEngine.AnimationClip Clip => m_Sequence ? m_Sequence.Clip : null;
        public bool Loop => m_Sequence && m_Sequence.Loop;
        public float DefaultPlayRate => m_Sequence ? m_Sequence.DefaultPlayRate : 0f;

        public void Configure(
            CharacterSequencePoseSourceSlot slot,
            CharacterAnimationSequenceAsset sequence)
        {
            if (!sequence)
                throw new ArgumentException("Sequence Pose source binding is incomplete.");
            ConfigureCommon(slot);
            m_Sequence = sequence;
            RequireValid(sequence.Rig);
        }

        public override void RequireValid(CharacterAnimationRigDefinition profileRig)
        {
            base.RequireValid(profileRig);
            m_Sequence.RequireValid();
        }
    }
}
