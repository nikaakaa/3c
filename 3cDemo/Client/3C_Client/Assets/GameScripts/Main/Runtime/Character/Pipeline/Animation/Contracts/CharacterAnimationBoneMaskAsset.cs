using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    [CreateAssetMenu(fileName = "CharacterAnimationBoneMask", menuName = "3C/Character/Animation Bone Mask")]
    public sealed class CharacterAnimationBoneMaskAsset : ScriptableObject
    {
        [SerializeField] string m_MaskId = string.Empty;
        [SerializeField] string m_RigId = string.Empty;
        [SerializeField] string m_RigRevision = string.Empty;
        [SerializeField] CharacterAnimationBoneWeight[] m_Weights = Array.Empty<CharacterAnimationBoneWeight>();

        public string MaskId => m_MaskId ?? string.Empty;
        public string RigId => m_RigId ?? string.Empty;
        public string RigRevision => m_RigRevision ?? string.Empty;
        public IReadOnlyList<CharacterAnimationBoneWeight> Weights => m_Weights ?? Array.Empty<CharacterAnimationBoneWeight>();

        public void Configure(string maskId, CharacterAnimationRigDefinition rig, CharacterAnimationBoneWeight[] weights)
        {
            if (!rig)
                throw new ArgumentNullException(nameof(rig));
            rig.RequireValid();
            m_MaskId = PoseNodeId.Require(maskId, nameof(maskId));
            m_RigId = rig.RigId;
            m_RigRevision = rig.Revision;
            m_Weights = weights ?? throw new ArgumentNullException(nameof(weights));
            BuildDense(rig);
        }

        public float[] BuildDense(CharacterAnimationRigDefinition rig)
        {
            if (!rig)
                throw new ArgumentNullException(nameof(rig));
            rig.RequireValid();
            if (!string.Equals(RigId, rig.RigId, StringComparison.Ordinal) ||
                !string.Equals(RigRevision, rig.Revision, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Bone Mask '{name}' was authored for Rig '{RigId}@{RigRevision}', not '{rig.RigId}@{rig.Revision}'.");
            }
            var dense = new float[rig.Bones.Count];
            var seen = new HashSet<AnimationBoneId>();
            for (int i = 0; i < Weights.Count; i++)
            {
                CharacterAnimationBoneWeight weight = Weights[i];
                if (weight == null || !weight.BoneId.IsValid || !seen.Add(weight.BoneId))
                    throw new InvalidOperationException($"Bone Mask '{name}' contains an invalid or duplicate Bone entry.");
                dense[rig.RequireBoneIndex(weight.BoneId)] = weight.Weight;
            }
            if (seen.Count != rig.Bones.Count)
                throw new InvalidOperationException($"Bone Mask '{name}' does not explicitly cover every Rig Bone.");
            return dense;
        }
    }
}
