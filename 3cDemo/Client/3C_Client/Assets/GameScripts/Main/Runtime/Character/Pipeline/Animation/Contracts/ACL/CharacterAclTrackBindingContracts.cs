using System;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    [Serializable]
    public sealed class CharacterAclDataBlockDescriptor
    {
        [SerializeField] bool m_Exists;
        [SerializeField] int m_Length;
        [SerializeField] int m_FormatVersion;
        [SerializeField] string m_ContentHash = string.Empty;

        public bool Exists => m_Exists;
        public int Length => m_Length;
        public int FormatVersion => m_FormatVersion;
        public string ContentHash => m_ContentHash ?? string.Empty;

        public CharacterAclDataBlockDescriptor() { }

        public CharacterAclDataBlockDescriptor(
            bool exists,
            int length,
            int formatVersion,
            string contentHash)
        {
            m_Exists = exists;
            m_Length = length;
            m_FormatVersion = formatVersion;
            m_ContentHash = contentHash?.Trim() ?? string.Empty;
        }

        public void RequireValid(
            CharacterAclDataBlockKind kind,
            bool required,
            int expectedFormatVersion)
        {
            if (!Enum.IsDefined(typeof(CharacterAclDataBlockKind), kind) ||
                expectedFormatVersion <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(kind));
            }
            if (!Exists)
            {
                if (required || Length != 0 || FormatVersion != 0 || !string.IsNullOrEmpty(ContentHash))
                    throw new InvalidOperationException($"ACL data block '{kind}' is missing or has an invalid empty declaration.");
                return;
            }
            if (Length <= 0 || FormatVersion != expectedFormatVersion || !CharacterAclHash.IsSha256(ContentHash))
                throw new InvalidOperationException($"ACL data block '{kind}' descriptor is invalid.");
        }

        public bool Matches(byte[] payload)
        {
            if (!Exists || payload == null || payload.Length != Length)
                return false;
            return string.Equals(CharacterAclHash.Compute(payload), ContentHash, StringComparison.Ordinal);
        }
    }

    [Serializable]
    public sealed class CharacterAclTransformTrackBinding
    {
        [SerializeField] string m_BoneId = string.Empty;
        [SerializeField] int m_PoseBoneIndex = -1;
        [SerializeField] int m_TrackIndex = -1;
        [SerializeField] string m_ReferenceIdentity = string.Empty;

        public string BoneId => m_BoneId ?? string.Empty;
        public int PoseBoneIndex => m_PoseBoneIndex;
        public int TrackIndex => m_TrackIndex;
        public string ReferenceIdentity => m_ReferenceIdentity ?? string.Empty;

        public CharacterAclTransformTrackBinding() { }

        public CharacterAclTransformTrackBinding(
            AnimationBoneId boneId,
            int poseBoneIndex,
            int trackIndex,
            string referenceIdentity)
        {
            m_BoneId = boneId.IsValid ? boneId.Value : throw new ArgumentException("ACL transform Bone identity is invalid.", nameof(boneId));
            m_PoseBoneIndex = poseBoneIndex;
            m_TrackIndex = trackIndex;
            m_ReferenceIdentity = PoseIdentity.Require(referenceIdentity, nameof(referenceIdentity));
        }

        public void RequireValid(int poseBoneCount)
        {
            if (string.IsNullOrWhiteSpace(BoneId) ||
                PoseBoneIndex < 0 || PoseBoneIndex >= poseBoneCount ||
                TrackIndex < 0 ||
                string.IsNullOrWhiteSpace(ReferenceIdentity))
            {
                throw new InvalidOperationException("ACL transform track binding is invalid.");
            }
        }
    }

    [Serializable]
    public sealed class CharacterAclScalarTrackBinding
    {
        [SerializeField] string m_ParameterId = string.Empty;
        [SerializeField] int m_ParameterIndex = -1;
        [SerializeField] int m_TrackIndex = -1;
        [SerializeField] bool m_IsConstant;
        [SerializeField] float m_DefaultValue;
        [SerializeField] string m_Unit = string.Empty;

        public PoseParameterId ParameterId => string.IsNullOrWhiteSpace(m_ParameterId) ? default : new PoseParameterId(m_ParameterId);
        public int ParameterIndex => m_ParameterIndex;
        public int TrackIndex => m_TrackIndex;
        public bool IsConstant => m_IsConstant;
        public float DefaultValue => m_DefaultValue;
        public string Unit => m_Unit ?? string.Empty;

        public CharacterAclScalarTrackBinding() { }

        public CharacterAclScalarTrackBinding(
            PoseParameterId parameterId,
            int parameterIndex,
            int trackIndex,
            bool isConstant,
            float defaultValue,
            string unit)
        {
            if (!parameterId.IsValid)
                throw new ArgumentException("ACL scalar parameter identity is invalid.", nameof(parameterId));
            if (parameterIndex < 0 || !float.IsFinite(defaultValue) || !isConstant && trackIndex < 0)
                throw new ArgumentException("ACL scalar track binding is invalid.");
            m_ParameterId = parameterId.Value;
            m_ParameterIndex = parameterIndex;
            m_TrackIndex = isConstant ? -1 : trackIndex;
            m_IsConstant = isConstant;
            m_DefaultValue = defaultValue;
            m_Unit = unit?.Trim() ?? string.Empty;
        }

        public void RequireValid(int parameterCount, int trackCount = int.MaxValue)
        {
            if (!ParameterId.IsValid || ParameterIndex < 0 || ParameterIndex >= parameterCount ||
                !float.IsFinite(DefaultValue) || !IsConstant &&
                (TrackIndex < 0 || TrackIndex >= trackCount))
                throw new InvalidOperationException("ACL scalar track binding is invalid.");
        }
    }
}
