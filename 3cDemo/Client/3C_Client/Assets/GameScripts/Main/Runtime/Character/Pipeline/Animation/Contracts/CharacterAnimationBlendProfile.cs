using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    [Serializable]
    public sealed class CharacterAnimationBoneDurationMultiplier
    {
        [SerializeField] string m_BoneId = string.Empty;
        [SerializeField] float m_Multiplier = 1f;

        public AnimationBoneId BoneId => string.IsNullOrWhiteSpace(m_BoneId) ? default : new AnimationBoneId(m_BoneId);
        public float Multiplier => m_Multiplier;

        public CharacterAnimationBoneDurationMultiplier() { }

        public CharacterAnimationBoneDurationMultiplier(AnimationBoneId boneId, float multiplier)
        {
            if (!boneId.IsValid)
                throw new ArgumentException("Animation Bone identity is invalid.", nameof(boneId));
            if (!float.IsFinite(multiplier) || multiplier <= 0f)
                throw new ArgumentOutOfRangeException(nameof(multiplier));
            m_BoneId = boneId.Value;
            m_Multiplier = multiplier;
        }
    }

    [CreateAssetMenu(fileName = "CharacterAnimationBlendProfile", menuName = "3C/Character/Animation Blend Profile")]
    public sealed class CharacterAnimationBlendProfile : ScriptableObject
    {
        public const string SchemaVersion = "character-animation-blend-profile/v1";

        [SerializeField] string m_Schema = SchemaVersion;
        [SerializeField] string m_ProfileId = string.Empty;
        [SerializeField] string m_RigId = string.Empty;
        [SerializeField] string m_RigRevision = string.Empty;
        [SerializeField] float m_GlobalDurationMultiplier = 1f;
        [SerializeField] CharacterAnimationBoneDurationMultiplier[] m_BoneOverrides = Array.Empty<CharacterAnimationBoneDurationMultiplier>();

        public string Schema => m_Schema ?? string.Empty;
        public string ProfileId => m_ProfileId ?? string.Empty;
        public string RigId => m_RigId ?? string.Empty;
        public string RigRevision => m_RigRevision ?? string.Empty;
        public float GlobalDurationMultiplier => m_GlobalDurationMultiplier;
        public IReadOnlyList<CharacterAnimationBoneDurationMultiplier> BoneOverrides => m_BoneOverrides ?? Array.Empty<CharacterAnimationBoneDurationMultiplier>();

        public void Configure(
            string profileId,
            CharacterAnimationRigDefinition rig,
            float globalDurationMultiplier,
            CharacterAnimationBoneDurationMultiplier[] boneOverrides)
        {
            if (!rig)
                throw new ArgumentNullException(nameof(rig));
            rig.RequireValid();
            m_Schema = SchemaVersion;
            m_ProfileId = PoseNodeId.Require(profileId, nameof(profileId));
            m_RigId = rig.RigId;
            m_RigRevision = rig.Revision;
            m_GlobalDurationMultiplier = globalDurationMultiplier;
            m_BoneOverrides = boneOverrides ?? throw new ArgumentNullException(nameof(boneOverrides));
            BuildDense(rig);
        }

        public float[] BuildDense(CharacterAnimationRigDefinition rig)
        {
            if (!rig)
                throw new ArgumentNullException(nameof(rig));
            rig.RequireValid();
            if (!string.Equals(Schema, SchemaVersion, StringComparison.Ordinal) ||
                string.IsNullOrEmpty(ProfileId) ||
                !string.Equals(RigId, rig.RigId, StringComparison.Ordinal) ||
                !string.Equals(RigRevision, rig.Revision, StringComparison.Ordinal) ||
                !float.IsFinite(GlobalDurationMultiplier) || GlobalDurationMultiplier <= 0f)
            {
                throw new InvalidOperationException($"Animation Blend Profile '{name}' does not match Rig '{rig.RigId}@{rig.Revision}'.");
            }

            var dense = new float[rig.Bones.Count];
            for (int i = 0; i < dense.Length; i++)
                dense[i] = GlobalDurationMultiplier;

            var seen = new HashSet<AnimationBoneId>();
            for (int i = 0; i < BoneOverrides.Count; i++)
            {
                CharacterAnimationBoneDurationMultiplier value = BoneOverrides[i];
                if (value == null || !value.BoneId.IsValid || !seen.Add(value.BoneId) ||
                    !float.IsFinite(value.Multiplier) || value.Multiplier <= 0f)
                {
                    throw new InvalidOperationException($"Animation Blend Profile '{name}' Bone override #{i} is invalid or duplicated.");
                }
                dense[rig.RequireBoneIndex(value.BoneId)] = value.Multiplier;
            }
            return dense;
        }
    }
}
