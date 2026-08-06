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

        public PoseParameterId ParameterId => string.IsNullOrWhiteSpace(m_ParameterId)
            ? default
            : new PoseParameterId(m_ParameterId);
        public PoseParameterInertializationMode Mode => m_Mode;

        public CharacterPoseParameterInertializationFilter() { }

        public CharacterPoseParameterInertializationFilter(
            PoseParameterId parameterId,
            PoseParameterInertializationMode mode)
        {
            if (!parameterId.IsValid ||
                !Enum.IsDefined(typeof(PoseParameterInertializationMode), mode))
            {
                throw new ArgumentException("Pose Parameter inertialization filter is invalid.");
            }
            m_ParameterId = parameterId.Value;
            m_Mode = mode;
        }
    }

    [Serializable]
    public sealed class CharacterPoseInertializationResponse
    {
        [SerializeField] CharacterPoseParameterInertializationFilter[] m_ParameterFilters =
            Array.Empty<CharacterPoseParameterInertializationFilter>();

        public IReadOnlyList<CharacterPoseParameterInertializationFilter> ParameterFilters =>
            m_ParameterFilters ?? Array.Empty<CharacterPoseParameterInertializationFilter>();

        public void Configure(CharacterPoseParameterInertializationFilter[] parameterFilters)
        {
            m_ParameterFilters = parameterFilters ??
                throw new ArgumentNullException(nameof(parameterFilters));
            RequireValid();
        }

        public void RequireValid()
        {
            var parameters = new HashSet<PoseParameterId>();
            for (int i = 0; i < ParameterFilters.Count; i++)
            {
                CharacterPoseParameterInertializationFilter filter = ParameterFilters[i];
                if (filter == null || !filter.ParameterId.IsValid ||
                    !Enum.IsDefined(typeof(PoseParameterInertializationMode), filter.Mode) ||
                    !parameters.Add(filter.ParameterId))
                {
                    throw new InvalidOperationException(
                        $"Pose Inertialization parameter filter #{i} is invalid or duplicated.");
                }
            }
        }
    }

    [Serializable]
    public sealed class CharacterPoseDirectInertializationRule
    {
        [SerializeField] PoseInertializationMode m_Mode = PoseInertializationMode.Inertialize;
        [SerializeField, Min(0f)] float m_DurationSeconds = 0.2f;
        [SerializeField] CharacterAnimationBlendMode m_BlendMode = CharacterAnimationBlendMode.EaseInOut;
        [SerializeField] CharacterAnimationBlendCurveAsset m_CustomBlendCurve;
        [SerializeField] CharacterAnimationBlendProfile m_BlendProfile;

        public PoseInertializationMode Mode => m_Mode;
        public float DurationSeconds => m_DurationSeconds;
        public CharacterAnimationBlendMode BlendMode => m_BlendMode;
        public CharacterAnimationBlendCurveAsset CustomBlendCurve => m_CustomBlendCurve;
        public CharacterAnimationBlendProfile BlendProfile => m_BlendProfile;

        public void Configure(
            PoseInertializationMode mode,
            float durationSeconds,
            CharacterAnimationBlendMode blendMode,
            CharacterAnimationBlendCurveAsset customBlendCurve,
            CharacterAnimationBlendProfile blendProfile)
        {
            m_Mode = mode;
            m_DurationSeconds = durationSeconds;
            m_BlendMode = blendMode;
            m_CustomBlendCurve = customBlendCurve;
            m_BlendProfile = blendProfile;
        }

        public void ApplyDurationTuning(
            CharacterPoseTuningValue value)
        {
            if (Mode != PoseInertializationMode.Inertialize ||
                value.Kind != CharacterPoseTuningValueKind.Float)
            {
                throw new InvalidOperationException(
                    "Direct Player Inertialization duration is not tunable for the current mode.");
            }
            m_DurationSeconds = value.FloatValue;
            if (!float.IsFinite(m_DurationSeconds) ||
                m_DurationSeconds <= 0f)
            {
                throw new InvalidOperationException(
                    "Direct Player Inertialization duration must be greater than zero.");
            }
        }

        public AnimationBlendCurvePayload CompileCurve()
        {
            if (Mode != PoseInertializationMode.Inertialize)
                throw new InvalidOperationException("Hard Cut has no blend curve.");
            return CharacterAnimationBlendCurveCompiler.Compile(BlendMode, CustomBlendCurve);
        }

        public void RequireValid(CharacterAnimationRigDefinition rig)
        {
            if (!Enum.IsDefined(typeof(PoseInertializationMode), Mode) ||
                !float.IsFinite(DurationSeconds) ||
                Mode == PoseInertializationMode.HardCut &&
                (DurationSeconds != 0f || BlendMode != CharacterAnimationBlendMode.Linear ||
                 CustomBlendCurve || BlendProfile) ||
                Mode == PoseInertializationMode.Inertialize &&
                (DurationSeconds <= 0f || !BlendProfile))
            {
                throw new InvalidOperationException("Direct Player Inertialization rule is invalid.");
            }
            if (Mode == PoseInertializationMode.Inertialize)
            {
                CharacterAnimationBlendCurveCompiler.RequireConfiguration(BlendMode, CustomBlendCurve);
                BlendProfile.BuildDense(rig);
            }
        }
    }

    [CreateAssetMenu(
        fileName = "CharacterPoseInertializationPolicy",
        menuName = "3C/Character/Pose Inertialization Policy")]
    public sealed class CharacterPoseInertializationPolicy : ScriptableObject
    {
        public const string SchemaVersion = "character-pose-inertialization-policy/v4";

        [SerializeField] string m_Schema = SchemaVersion;
        [SerializeField] string m_PolicyId = string.Empty;
        [SerializeField] string m_Revision = string.Empty;
        [SerializeField] CharacterPoseInertializationResponse m_Response =
            new CharacterPoseInertializationResponse();
        [SerializeReference] CharacterPoseDirectInertializationRule m_DirectPlayerRule;

        public string Schema => m_Schema ?? string.Empty;
        public string PolicyId => m_PolicyId ?? string.Empty;
        public string Revision => m_Revision ?? string.Empty;
        public CharacterPoseInertializationResponse Response => m_Response;
        public CharacterPoseDirectInertializationRule DirectPlayerRule => m_DirectPlayerRule;

        public void Configure(
            string policyId,
            string revision,
            CharacterPoseInertializationResponse response,
            CharacterPoseDirectInertializationRule directPlayerRule,
            CharacterAnimationRigDefinition rig)
        {
            m_Schema = SchemaVersion;
            m_PolicyId = PoseNodeId.Require(policyId, nameof(policyId));
            m_Revision = PoseNodeId.Require(revision, nameof(revision));
            m_Response = response ?? throw new ArgumentNullException(nameof(response));
            m_DirectPlayerRule = directPlayerRule;
            RequireValid(rig);
        }

        public void RequireValid(CharacterAnimationRigDefinition rig)
        {
            if (!rig || !string.Equals(Schema, SchemaVersion, StringComparison.Ordinal) ||
                string.IsNullOrWhiteSpace(PolicyId) || string.IsNullOrWhiteSpace(Revision) ||
                Response == null)
            {
                throw new InvalidOperationException(
                    $"Pose Inertialization Policy '{name}' is invalid.");
            }
            rig.RequireValid();
            Response.RequireValid();
            DirectPlayerRule?.RequireValid(rig);
        }
    }
}
