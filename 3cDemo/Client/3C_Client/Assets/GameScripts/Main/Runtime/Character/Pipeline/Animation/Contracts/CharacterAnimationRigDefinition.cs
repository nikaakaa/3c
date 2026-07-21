using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public enum CharacterAnimationRootBonePolicy : byte
    {
        ExcludeSourceRoot = 1,
        CaptureSourceRoot = 2
    }

    public enum CharacterAnimationScalePolicy : byte
    {
        PreserveReferenceScale = 1,
        BlendLocalScale = 2
    }

    [Serializable]
    public sealed class CharacterAnimationBoneDefinition
    {
        [SerializeField] string m_BoneId = string.Empty;
        [SerializeField] int m_ParentIndex = -1;
        [SerializeField] Vector3 m_ReferenceLocalPosition;
        [SerializeField] Quaternion m_ReferenceLocalRotation = Quaternion.identity;
        [SerializeField] Vector3 m_ReferenceLocalScale = Vector3.one;

        public AnimationBoneId BoneId => string.IsNullOrWhiteSpace(m_BoneId) ? default : new AnimationBoneId(m_BoneId);
        public int ParentIndex => m_ParentIndex;
        public Vector3 ReferenceLocalPosition => m_ReferenceLocalPosition;
        public Quaternion ReferenceLocalRotation => m_ReferenceLocalRotation;
        public Vector3 ReferenceLocalScale => m_ReferenceLocalScale;

        public CharacterAnimationBoneDefinition() { }

        public CharacterAnimationBoneDefinition(
            AnimationBoneId boneId,
            int parentIndex,
            Vector3 referenceLocalPosition,
            Quaternion referenceLocalRotation,
            Vector3 referenceLocalScale)
        {
            if (!boneId.IsValid)
                throw new ArgumentException("Animation Bone identity is invalid.", nameof(boneId));
            if (parentIndex < -1)
                throw new ArgumentOutOfRangeException(nameof(parentIndex));
            if (!IsFinite(referenceLocalPosition) || !IsFinite(referenceLocalRotation) || !IsFinite(referenceLocalScale))
                throw new ArgumentException("Animation reference transform is non-finite.");
            m_BoneId = boneId.Value;
            m_ParentIndex = parentIndex;
            m_ReferenceLocalPosition = referenceLocalPosition;
            m_ReferenceLocalRotation = referenceLocalRotation.normalized;
            m_ReferenceLocalScale = referenceLocalScale;
        }

        static bool IsFinite(Vector3 value) => float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
        static bool IsFinite(Quaternion value) => float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z) && float.IsFinite(value.w);
    }

    [CreateAssetMenu(fileName = "CharacterAnimationRigDefinition", menuName = "3C/Character/Animation Rig Definition")]
    public sealed class CharacterAnimationRigDefinition : ScriptableObject
    {
        public const string SchemaVersion = "character-animation-rig/v1";

        [SerializeField] string m_Schema = SchemaVersion;
        [SerializeField] string m_RigId = string.Empty;
        [SerializeField] string m_Revision = string.Empty;
        [SerializeField] CharacterAnimationBoneDefinition[] m_Bones = Array.Empty<CharacterAnimationBoneDefinition>();
        [SerializeField] CharacterAnimationRootBonePolicy m_RootBonePolicy = CharacterAnimationRootBonePolicy.ExcludeSourceRoot;
        [SerializeField] CharacterAnimationScalePolicy m_ScalePolicy = CharacterAnimationScalePolicy.PreserveReferenceScale;
        [SerializeField] string m_LeftFootBoneId = string.Empty;
        [SerializeField] string m_RightFootBoneId = string.Empty;

        public string Schema => m_Schema ?? string.Empty;
        public string RigId => m_RigId ?? string.Empty;
        public string Revision => m_Revision ?? string.Empty;
        public IReadOnlyList<CharacterAnimationBoneDefinition> Bones => m_Bones ?? Array.Empty<CharacterAnimationBoneDefinition>();
        public CharacterAnimationRootBonePolicy RootBonePolicy => m_RootBonePolicy;
        public CharacterAnimationScalePolicy ScalePolicy => m_ScalePolicy;
        public AnimationBoneId LeftFootBoneId => string.IsNullOrWhiteSpace(m_LeftFootBoneId) ? default : new AnimationBoneId(m_LeftFootBoneId);
        public AnimationBoneId RightFootBoneId => string.IsNullOrWhiteSpace(m_RightFootBoneId) ? default : new AnimationBoneId(m_RightFootBoneId);

        public void Configure(
            string rigId,
            string revision,
            CharacterAnimationBoneDefinition[] bones,
            CharacterAnimationRootBonePolicy rootBonePolicy,
            CharacterAnimationScalePolicy scalePolicy,
            AnimationBoneId leftFootBoneId,
            AnimationBoneId rightFootBoneId)
        {
            m_Schema = SchemaVersion;
            m_RigId = PoseSlotId.Require(rigId, nameof(rigId));
            m_Revision = PoseSlotId.Require(revision, nameof(revision));
            m_Bones = bones ?? throw new ArgumentNullException(nameof(bones));
            m_RootBonePolicy = rootBonePolicy;
            m_ScalePolicy = scalePolicy;
            m_LeftFootBoneId = leftFootBoneId.IsValid ? leftFootBoneId.Value : throw new ArgumentException("Left foot Bone identity is invalid.", nameof(leftFootBoneId));
            m_RightFootBoneId = rightFootBoneId.IsValid ? rightFootBoneId.Value : throw new ArgumentException("Right foot Bone identity is invalid.", nameof(rightFootBoneId));
            RequireValid();
        }

        public void RequireValid()
        {
            if (!string.Equals(Schema, SchemaVersion, StringComparison.Ordinal) ||
                string.IsNullOrEmpty(RigId) || string.IsNullOrEmpty(Revision) || Bones.Count == 0 ||
                !Enum.IsDefined(typeof(CharacterAnimationRootBonePolicy), RootBonePolicy) ||
                !Enum.IsDefined(typeof(CharacterAnimationScalePolicy), ScalePolicy))
                throw new InvalidOperationException($"Animation Rig '{name}' is incomplete.");
            var ids = new HashSet<AnimationBoneId>();
            int left = -1;
            int right = -1;
            int root = -1;
            for (int i = 0; i < Bones.Count; i++)
            {
                CharacterAnimationBoneDefinition bone = Bones[i];
                if (bone == null || !bone.BoneId.IsValid || !ids.Add(bone.BoneId) ||
                    bone.ParentIndex < -1 || bone.ParentIndex >= i)
                    throw new InvalidOperationException($"Animation Rig '{name}' bone #{i} is invalid or not parent-first.");
                if (bone.ParentIndex == -1)
                {
                    if (root >= 0)
                        throw new InvalidOperationException($"Animation Rig '{name}' contains multiple root bones.");
                    root = i;
                }
                if (bone.BoneId.Equals(LeftFootBoneId))
                    left = i;
                if (bone.BoneId.Equals(RightFootBoneId))
                    right = i;
            }
            if (root < 0 || left < 0 || right < 0 || left == right)
                throw new InvalidOperationException($"Animation Rig '{name}' has invalid semantic foot bones.");
        }

        public int RequireRootBoneIndex()
        {
            RequireValid();
            for (int i = 0; i < Bones.Count; i++)
            {
                if (Bones[i].ParentIndex == -1)
                    return i;
            }
            throw new InvalidOperationException($"Animation Rig '{name}' has no root Bone.");
        }

        public int RequireBoneIndex(AnimationBoneId boneId)
        {
            for (int i = 0; i < Bones.Count; i++)
            {
                if (Bones[i] != null && Bones[i].BoneId.Equals(boneId))
                    return i;
            }
            throw new InvalidOperationException($"Animation Rig '{name}' does not contain Bone '{boneId}'.");
        }
    }

    [Serializable]
    public sealed class CharacterAnimationBoneWeight
    {
        [SerializeField] string m_BoneId = string.Empty;
        [SerializeField, Range(0f, 1f)] float m_Weight;

        public AnimationBoneId BoneId => string.IsNullOrWhiteSpace(m_BoneId) ? default : new AnimationBoneId(m_BoneId);
        public float Weight => m_Weight;

        public CharacterAnimationBoneWeight() { }

        public CharacterAnimationBoneWeight(AnimationBoneId boneId, float weight)
        {
            if (!boneId.IsValid)
                throw new ArgumentException("Animation Bone identity is invalid.", nameof(boneId));
            if (!float.IsFinite(weight) || weight < 0f || weight > 1f)
                throw new ArgumentOutOfRangeException(nameof(weight));
            m_BoneId = boneId.Value;
            m_Weight = weight;
        }
    }

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
            m_MaskId = PoseSlotId.Require(maskId, nameof(maskId));
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

    public sealed class CharacterAnimationRigBinding : MonoBehaviour
    {
        [SerializeField] Animator m_Animator;
        [SerializeField] string m_RigId = string.Empty;
        [SerializeField] string m_RigRevision = string.Empty;
        [SerializeField] Transform[] m_Bones = Array.Empty<Transform>();

        public Animator Animator => m_Animator;
        public string RigId => m_RigId ?? string.Empty;
        public string RigRevision => m_RigRevision ?? string.Empty;
        public IReadOnlyList<Transform> Bones => m_Bones ?? Array.Empty<Transform>();

        public void Configure(Animator animator, CharacterAnimationRigPayload rig, Transform[] bones)
        {
            if (!animator)
                throw new ArgumentNullException(nameof(animator));
            if (rig == null)
                throw new ArgumentNullException(nameof(rig));
            rig.RequireValid();
            m_Animator = animator;
            m_RigId = rig.RigId;
            m_RigRevision = rig.RigRevision;
            m_Bones = bones ?? throw new ArgumentNullException(nameof(bones));
            RequireValid(rig);
        }

        public void RequireValid(CharacterAnimationRigPayload expected)
        {
            if (expected == null)
                throw new ArgumentNullException(nameof(expected));
            expected.RequireValid();
            if (!m_Animator ||
                !string.Equals(RigId, expected.RigId, StringComparison.Ordinal) ||
                !string.Equals(RigRevision, expected.RigRevision, StringComparison.Ordinal) ||
                Bones.Count != expected.Bones.Count)
            {
                throw new InvalidOperationException($"Animation Rig Binding '{name}' does not match the compiled Projection Rig.");
            }
            var transforms = new HashSet<Transform>();
            for (int i = 0; i < Bones.Count; i++)
            {
                if (!Bones[i] || !transforms.Add(Bones[i]) ||
                    Bones[i] != m_Animator.transform && !Bones[i].IsChildOf(m_Animator.transform))
                {
                    throw new InvalidOperationException($"Animation Rig Binding '{name}' Bone #{i} is missing, duplicated, or outside the Animator hierarchy.");
                }
            }
        }
    }
}
