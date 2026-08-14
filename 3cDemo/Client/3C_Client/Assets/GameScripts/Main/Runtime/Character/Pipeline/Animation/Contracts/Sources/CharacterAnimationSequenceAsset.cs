using System;
using BTSMTL.Timeline;
using UnityEngine;
using UnityAnimationClip = UnityEngine.AnimationClip;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    [CreateAssetMenu(
        fileName = "CharacterAnimationSequence",
        menuName = "3C/Character/Animation Sequence")]
    public sealed class CharacterAnimationSequenceAsset : AnimationSequenceAsset, IAnimationSequenceAnalysisReference
    {
        public const string FootPlacementWeightChannelId = "presentation.foot-placement-weight";

        [SerializeField] CharacterAnimationRigDefinition m_Rig;
        [SerializeField] ScriptableObject m_FootAnalysisSource;
        [SerializeField] string m_FootAnalysisIdentity = string.Empty;

        public CharacterAnimationRigDefinition Rig => m_Rig;
        public ScriptableObject FootAnalysisSource => m_FootAnalysisSource;
        public string FootAnalysisIdentity => m_FootAnalysisIdentity ?? string.Empty;
        UnityEngine.Object IAnimationSequenceAnalysisReference.AnalysisSource => m_FootAnalysisSource;
        string IAnimationSequenceAnalysisReference.AnalysisIdentity => FootAnalysisIdentity;
        public AnimationCurve FootPlacementWeightCurve =>
            TryGetCurve(FootPlacementWeightChannelId, out AnimationCurve curve)
                ? curve
                : throw new InvalidOperationException($"Animation Sequence '{name}' is missing Foot Placement Weight.");

        public void Configure(
            string authoringId,
            UnityAnimationClip clip,
            CharacterAnimationRigDefinition rig,
            bool loop,
            float defaultPlayRate,
            ScriptableObject footAnalysisSource,
            string footAnalysisIdentity,
            AnimationCurve footPlacementWeightCurve)
        {
            if (!rig || !footAnalysisSource || string.IsNullOrWhiteSpace(footAnalysisIdentity))
                throw new ArgumentException("Character Animation Sequence is incomplete.");
            ConfigureCore(authoringId, clip, loop, defaultPlayRate);
            m_Rig = rig;
            m_FootAnalysisSource = footAnalysisSource;
            m_FootAnalysisIdentity = footAnalysisIdentity.Trim();
            SetCurve(
                FootPlacementWeightChannelId,
                AnimationSequenceCurveValueDomain.Normalized01,
                footPlacementWeightCurve);
            ConfigureNone();
            RequireValid();
        }

        public override void RequireValid()
        {
            base.RequireValid();
            if (!m_Rig || !m_FootAnalysisSource || string.IsNullOrWhiteSpace(FootAnalysisIdentity))
                throw new InvalidOperationException($"Character Animation Sequence '{name}' owner references are incomplete.");
            m_Rig.RequireValid();
            if (!TryGetCurve(FootPlacementWeightChannelId, out _))
                throw new InvalidOperationException($"Character Animation Sequence '{name}' is missing Foot Placement Weight.");
        }
    }
}
