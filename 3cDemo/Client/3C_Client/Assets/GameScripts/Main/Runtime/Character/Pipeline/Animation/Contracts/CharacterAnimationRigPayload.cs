using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    [Serializable]
    public sealed class CharacterAnimationRigBonePayload
    {
        [SerializeField] string m_BoneId = string.Empty;
        [SerializeField] int m_ParentIndex = -1;
        [SerializeField] Vector3 m_ReferenceLocalPosition;
        [SerializeField] Quaternion m_ReferenceLocalRotation = Quaternion.identity;
        [SerializeField] Vector3 m_ReferenceLocalScale = Vector3.one;

        public CharacterAnimationRigBonePayload(CharacterAnimationBoneDefinition source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            m_BoneId = source.BoneId.Value;
            m_ParentIndex = source.ParentIndex;
            m_ReferenceLocalPosition = source.ReferenceLocalPosition;
            m_ReferenceLocalRotation = source.ReferenceLocalRotation;
            m_ReferenceLocalScale = source.ReferenceLocalScale;
        }

        public AnimationBoneId BoneId => string.IsNullOrWhiteSpace(m_BoneId) ? default : new AnimationBoneId(m_BoneId);
        public int ParentIndex => m_ParentIndex;
        public Vector3 ReferenceLocalPosition => m_ReferenceLocalPosition;
        public Quaternion ReferenceLocalRotation => m_ReferenceLocalRotation;
        public Vector3 ReferenceLocalScale => m_ReferenceLocalScale;
    }

    [Serializable]
    public sealed class CharacterAnimationRigPayload
    {
        public const string SchemaVersion = "character-animation-rig-payload/v1";

        [SerializeField] string m_Schema = SchemaVersion;
        [SerializeField] string m_RigId = string.Empty;
        [SerializeField] string m_RigRevision = string.Empty;
        [SerializeField] CharacterAnimationRootBonePolicy m_RootBonePolicy;
        [SerializeField] CharacterAnimationScalePolicy m_ScalePolicy;
        [SerializeField] int m_RootBoneIndex = -1;
        [SerializeField] int m_LeftFootBoneIndex = -1;
        [SerializeField] int m_RightFootBoneIndex = -1;
        [SerializeField] CharacterAnimationRigBonePayload[] m_Bones = Array.Empty<CharacterAnimationRigBonePayload>();

        public CharacterAnimationRigPayload(CharacterAnimationRigDefinition source)
        {
            if (!source)
                throw new ArgumentNullException(nameof(source));
            source.RequireValid();
            m_Schema = SchemaVersion;
            m_RigId = source.RigId;
            m_RigRevision = source.Revision;
            m_RootBonePolicy = source.RootBonePolicy;
            m_ScalePolicy = source.ScalePolicy;
            m_RootBoneIndex = source.RequireRootBoneIndex();
            m_LeftFootBoneIndex = source.RequireBoneIndex(source.LeftFootBoneId);
            m_RightFootBoneIndex = source.RequireBoneIndex(source.RightFootBoneId);
            m_Bones = new CharacterAnimationRigBonePayload[source.Bones.Count];
            for (int i = 0; i < m_Bones.Length; i++)
                m_Bones[i] = new CharacterAnimationRigBonePayload(source.Bones[i]);
            RequireValid();
        }

        public string Schema => m_Schema ?? string.Empty;
        public string RigId => m_RigId ?? string.Empty;
        public string RigRevision => m_RigRevision ?? string.Empty;
        public CharacterAnimationRootBonePolicy RootBonePolicy => m_RootBonePolicy;
        public CharacterAnimationScalePolicy ScalePolicy => m_ScalePolicy;
        public int RootBoneIndex => m_RootBoneIndex;
        public int LeftFootBoneIndex => m_LeftFootBoneIndex;
        public int RightFootBoneIndex => m_RightFootBoneIndex;
        public IReadOnlyList<CharacterAnimationRigBonePayload> Bones => m_Bones ?? Array.Empty<CharacterAnimationRigBonePayload>();

        public void RequireValid()
        {
            if (!string.Equals(Schema, SchemaVersion, StringComparison.Ordinal) ||
                string.IsNullOrEmpty(RigId) || string.IsNullOrEmpty(RigRevision) || Bones.Count == 0 ||
                !Enum.IsDefined(typeof(CharacterAnimationRootBonePolicy), RootBonePolicy) ||
                !Enum.IsDefined(typeof(CharacterAnimationScalePolicy), ScalePolicy) ||
                RootBoneIndex < 0 || RootBoneIndex >= Bones.Count ||
                LeftFootBoneIndex < 0 || LeftFootBoneIndex >= Bones.Count ||
                RightFootBoneIndex < 0 || RightFootBoneIndex >= Bones.Count ||
                LeftFootBoneIndex == RightFootBoneIndex)
            {
                throw new InvalidOperationException("Compiled Character Animation Rig payload is invalid.");
            }

            var ids = new HashSet<AnimationBoneId>();
            int rootCount = 0;
            for (int i = 0; i < Bones.Count; i++)
            {
                CharacterAnimationRigBonePayload bone = Bones[i];
                if (bone == null || !bone.BoneId.IsValid || !ids.Add(bone.BoneId) ||
                    bone.ParentIndex < -1 || bone.ParentIndex >= i)
                {
                    throw new InvalidOperationException($"Compiled Character Animation Rig Bone #{i} is invalid.");
                }
                if (bone.ParentIndex == -1)
                {
                    rootCount++;
                    if (i != RootBoneIndex)
                        throw new InvalidOperationException("Compiled Character Animation Rig root index is inconsistent.");
                }
            }
            if (rootCount != 1)
                throw new InvalidOperationException("Compiled Character Animation Rig requires exactly one root Bone.");
        }
    }
}
