using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public enum PoseInertializationMode : byte
    {
        HardCut = 1,
        Inertialize = 2
    }

    public enum PoseParameterInertializationMode : byte
    {
        Snap = 1,
        Inertialize = 2
    }

    [Serializable]
    public sealed class CharacterPoseParameterInertializationFilter
    {
        [SerializeField] string m_ParameterId = string.Empty;
        [SerializeField] PoseParameterInertializationMode m_Mode = PoseParameterInertializationMode.Snap;

        public PoseParameterId ParameterId => string.IsNullOrWhiteSpace(m_ParameterId) ? default : new PoseParameterId(m_ParameterId);
        public PoseParameterInertializationMode Mode => m_Mode;

        public CharacterPoseParameterInertializationFilter() { }

        public CharacterPoseParameterInertializationFilter(PoseParameterId parameterId, PoseParameterInertializationMode mode)
        {
            if (!parameterId.IsValid || !Enum.IsDefined(typeof(PoseParameterInertializationMode), mode))
                throw new ArgumentException("Pose Parameter inertialization filter is invalid.");
            m_ParameterId = parameterId.Value;
            m_Mode = mode;
        }
    }

    [Serializable]
    public sealed class CharacterPoseInertializationRule
    {
        [SerializeField] PoseInertializationMode m_Mode = PoseInertializationMode.Inertialize;
        [SerializeField, Min(0f)] float m_DurationSeconds = 0.2f;
        [SerializeField] CharacterAnimationBlendCurve m_Curve = new CharacterAnimationBlendCurve();
        [SerializeField] CharacterAnimationBlendProfile m_BlendProfile;
        [SerializeField] CharacterPoseParameterInertializationFilter[] m_ParameterFilters = Array.Empty<CharacterPoseParameterInertializationFilter>();

        public PoseInertializationMode Mode => m_Mode;
        public float DurationSeconds => m_DurationSeconds;
        public CharacterAnimationBlendCurve Curve => m_Curve;
        public CharacterAnimationBlendProfile BlendProfile => m_BlendProfile;
        public IReadOnlyList<CharacterPoseParameterInertializationFilter> ParameterFilters => m_ParameterFilters ?? Array.Empty<CharacterPoseParameterInertializationFilter>();

        public void Configure(
            PoseInertializationMode mode,
            float durationSeconds,
            CharacterAnimationBlendCurve curve,
            CharacterAnimationBlendProfile blendProfile,
            CharacterPoseParameterInertializationFilter[] parameterFilters)
        {
            m_Mode = mode;
            m_DurationSeconds = durationSeconds;
            m_Curve = curve;
            m_BlendProfile = blendProfile;
            m_ParameterFilters = parameterFilters ?? throw new ArgumentNullException(nameof(parameterFilters));
        }

        public void RequireValid(CharacterAnimationRigDefinition rig)
        {
            if (!Enum.IsDefined(typeof(PoseInertializationMode), Mode) ||
                !float.IsFinite(DurationSeconds) || DurationSeconds < 0f ||
                Mode == PoseInertializationMode.Inertialize &&
                (DurationSeconds <= 0f || Curve == null || !BlendProfile))
                throw new InvalidOperationException("Pose Inertialization rule is incomplete.");
            if (Mode == PoseInertializationMode.Inertialize)
            {
                Curve.RequireValid();
                BlendProfile.BuildDense(rig);
            }
            var parameters = new HashSet<PoseParameterId>();
            for (int i = 0; i < ParameterFilters.Count; i++)
            {
                CharacterPoseParameterInertializationFilter filter = ParameterFilters[i];
                if (filter == null || !filter.ParameterId.IsValid ||
                    !Enum.IsDefined(typeof(PoseParameterInertializationMode), filter.Mode) ||
                    !parameters.Add(filter.ParameterId))
                    throw new InvalidOperationException($"Pose Inertialization parameter filter #{i} is invalid or duplicated.");
            }
        }
    }

    [Serializable]
    public sealed class CharacterPoseInertializationOverride
    {
        [SerializeField] string m_SourceProducerIdentity = string.Empty;
        [SerializeField] string m_TargetProducerIdentity = string.Empty;
        [SerializeField] CharacterPoseInertializationRule m_Rule = new CharacterPoseInertializationRule();

        public string SourceProducerIdentity => m_SourceProducerIdentity ?? string.Empty;
        public string TargetProducerIdentity => m_TargetProducerIdentity ?? string.Empty;
        public CharacterPoseInertializationRule Rule => m_Rule;
        public string CanonicalKey => SourceProducerIdentity + "|" + TargetProducerIdentity;

        public void Configure(string sourceProducerIdentity, string targetProducerIdentity, CharacterPoseInertializationRule rule)
        {
            m_SourceProducerIdentity = sourceProducerIdentity ?? string.Empty;
            m_TargetProducerIdentity = targetProducerIdentity ?? string.Empty;
            m_Rule = rule ?? throw new ArgumentNullException(nameof(rule));
        }

        public void RequireValid(CharacterAnimationRigDefinition rig)
        {
            if (string.IsNullOrWhiteSpace(SourceProducerIdentity) ||
                string.IsNullOrWhiteSpace(TargetProducerIdentity) || Rule == null)
                throw new InvalidOperationException("Pose Inertialization override endpoint is incomplete.");
            Rule.RequireValid(rig);
        }
    }

    [CreateAssetMenu(fileName = "CharacterPoseInertializationPolicy", menuName = "3C/Character/Pose Inertialization Policy")]
    public sealed class CharacterPoseInertializationPolicy : ScriptableObject
    {
        public const string SchemaVersion = "character-pose-inertialization-policy/v2";

        [SerializeField] string m_Schema = SchemaVersion;
        [SerializeField] string m_PolicyId = string.Empty;
        [SerializeField] string m_Revision = string.Empty;
        [SerializeField] CharacterPoseInertializationRule m_DefaultRule = new CharacterPoseInertializationRule();
        [SerializeField] CharacterPoseInertializationOverride[] m_Overrides = Array.Empty<CharacterPoseInertializationOverride>();

        public string PolicyId => m_PolicyId ?? string.Empty;
        public string Revision => m_Revision ?? string.Empty;
        public CharacterPoseInertializationRule DefaultRule => m_DefaultRule;
        public IReadOnlyList<CharacterPoseInertializationOverride> Overrides => m_Overrides ?? Array.Empty<CharacterPoseInertializationOverride>();

        public void Configure(
            string policyId,
            string revision,
            CharacterPoseInertializationRule defaultRule,
            CharacterPoseInertializationOverride[] overrides,
            CharacterAnimationRigDefinition rig)
        {
            m_Schema = SchemaVersion;
            m_PolicyId = PoseNodeId.Require(policyId, nameof(policyId));
            m_Revision = PoseNodeId.Require(revision, nameof(revision));
            m_DefaultRule = defaultRule ?? throw new ArgumentNullException(nameof(defaultRule));
            m_Overrides = overrides ?? throw new ArgumentNullException(nameof(overrides));
            RequireValid(rig);
        }

        public void RequireValid(CharacterAnimationRigDefinition rig)
        {
            if (!rig || !string.Equals(m_Schema, SchemaVersion, StringComparison.Ordinal) ||
                string.IsNullOrWhiteSpace(PolicyId) || string.IsNullOrWhiteSpace(Revision) || DefaultRule == null)
                throw new InvalidOperationException($"Pose Inertialization Policy '{name}' is invalid.");
            rig.RequireValid();
            DefaultRule.RequireValid(rig);
            var keys = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < Overrides.Count; i++)
            {
                CharacterPoseInertializationOverride value = Overrides[i];
                if (value == null)
                    throw new InvalidOperationException($"Pose Inertialization Policy '{name}' override #{i} is missing.");
                value.RequireValid(rig);
                if (!keys.Add(value.CanonicalKey))
                    throw new InvalidOperationException($"Pose Inertialization Policy '{name}' duplicates override '{value.CanonicalKey}'.");
            }
        }
    }
}
