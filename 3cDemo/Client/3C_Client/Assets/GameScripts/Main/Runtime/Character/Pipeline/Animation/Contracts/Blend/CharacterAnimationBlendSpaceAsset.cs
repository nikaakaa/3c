using System;
using System.Collections.Generic;
using BTSMTL.Timeline;
using UnityEngine;
using UnityAnimationClip = UnityEngine.AnimationClip;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public readonly struct CharacterAnimationBlendSpaceId : IEquatable<CharacterAnimationBlendSpaceId>, IComparable<CharacterAnimationBlendSpaceId>
    {
        public CharacterAnimationBlendSpaceId(string value) { Value = PoseIdentity.Require(value, nameof(value)); }
        public string Value { get; }
        public bool IsValid => !string.IsNullOrEmpty(Value);
        public int CompareTo(CharacterAnimationBlendSpaceId other) => string.CompareOrdinal(Value, other.Value);
        public bool Equals(CharacterAnimationBlendSpaceId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is CharacterAnimationBlendSpaceId other && Equals(other);
        public override int GetHashCode() => Value?.GetHashCode() ?? 0;
        public override string ToString() => Value ?? string.Empty;
    }

    public readonly struct CharacterAnimationBlendSpaceSampleId : IEquatable<CharacterAnimationBlendSpaceSampleId>, IComparable<CharacterAnimationBlendSpaceSampleId>
    {
        public CharacterAnimationBlendSpaceSampleId(string value) { Value = PoseIdentity.Require(value, nameof(value)); }
        public string Value { get; }
        public bool IsValid => !string.IsNullOrEmpty(Value);
        public int CompareTo(CharacterAnimationBlendSpaceSampleId other) => string.CompareOrdinal(Value, other.Value);
        public bool Equals(CharacterAnimationBlendSpaceSampleId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is CharacterAnimationBlendSpaceSampleId other && Equals(other);
        public override int GetHashCode() => Value?.GetHashCode() ?? 0;
        public override string ToString() => Value ?? string.Empty;
    }

    public enum CharacterAnimationBlendSpaceMode : byte
    {
        Linear1D = 1,
        FreeformCartesian2D = 2,
        FreeformDirectional2D = 3
    }

    public enum CharacterAnimationBlendSpacePhasePolicy : byte
    {
        SharedNormalizedPhase = 1,
        MarkerSegmentPhase = 2,
        GeneratedFootPhase = 3
    }

    public enum CharacterAnimationBlendSpaceSampleRole : byte
    {
        DynamicCycle = 1,
        StationaryPose = 2
    }

    public enum CharacterAnimationBlendSpaceParameterPolicy : byte
    {
        RequireAllSamplesWeighted = 1,
        WeightedAvailableSamples = 2,
        Unavailable = 3
    }

    [Serializable]
    public sealed class CharacterAnimationBlendSpaceAxis
    {
        [SerializeField] string m_ParameterId = string.Empty;
        [SerializeField] PoseParameterValueType m_ValueType = PoseParameterValueType.Float;
        [SerializeField] string m_Unit = string.Empty;
        [SerializeField] float m_Minimum;
        [SerializeField] float m_Maximum = 1f;

        public PoseParameterId ParameterId => string.IsNullOrWhiteSpace(m_ParameterId) ? default : new PoseParameterId(m_ParameterId);
        public PoseParameterValueType ValueType => m_ValueType;
        public string Unit => m_Unit ?? string.Empty;
        public float Minimum => m_Minimum;
        public float Maximum => m_Maximum;

        internal void Configure(PoseParameterId parameterId, string unit, float minimum, float maximum)
        {
            m_ParameterId = parameterId.IsValid ? parameterId.Value : throw new ArgumentException("Blend Space axis Parameter identity is invalid.", nameof(parameterId));
            if (string.IsNullOrWhiteSpace(unit) || !float.IsFinite(minimum) || !float.IsFinite(maximum) || minimum >= maximum)
                throw new ArgumentException("Blend Space axis contract is invalid.");
            m_ValueType = PoseParameterValueType.Float;
            m_Unit = unit.Trim();
            m_Minimum = minimum;
            m_Maximum = maximum;
        }
    }

    [Serializable]
    public sealed class CharacterAnimationBlendSpaceSampleParameter
    {
        [SerializeField] string m_ParameterId = string.Empty;
        [SerializeField] float m_Value;

        public CharacterAnimationBlendSpaceSampleParameter() { }

        public CharacterAnimationBlendSpaceSampleParameter(PoseParameterId parameterId, float value)
        {
            if (!parameterId.IsValid || !float.IsFinite(value))
                throw new ArgumentException("Blend Space Sample parameter is invalid.");
            m_ParameterId = parameterId.Value;
            m_Value = value;
        }

        public PoseParameterId ParameterId => string.IsNullOrWhiteSpace(m_ParameterId) ? default : new PoseParameterId(m_ParameterId);
        public float Value => m_Value;
    }

    [Serializable]
    public sealed class CharacterAnimationBlendSpaceSample
    {
        [SerializeField] string m_SampleId = string.Empty;
        [SerializeField] CharacterAnimationSequenceAsset m_Sequence;
        [SerializeField] Vector2 m_Position;
        [SerializeField] CharacterAnimationBlendSpaceSampleRole m_Role = CharacterAnimationBlendSpaceSampleRole.DynamicCycle;
        [SerializeField, Range(0f, 1f)] float m_StationaryNormalizedTime;
        [SerializeField] CharacterAnimationBlendSpaceSampleParameter[] m_Parameters = Array.Empty<CharacterAnimationBlendSpaceSampleParameter>();

        public CharacterAnimationBlendSpaceSampleId SampleId => string.IsNullOrWhiteSpace(m_SampleId) ? default : new CharacterAnimationBlendSpaceSampleId(m_SampleId);
        public CharacterAnimationSequenceAsset Sequence => m_Sequence;
        public UnityAnimationClip Clip => m_Sequence ? m_Sequence.Clip : null;
        public string ClipContentIdentity => m_Sequence ? m_Sequence.ContentRevision : string.Empty;
        public Vector2 Position => m_Position;
        public CharacterAnimationBlendSpaceSampleRole Role => m_Role;
        public float StationaryNormalizedTime => m_StationaryNormalizedTime;
        public IReadOnlyList<CharacterAnimationBlendSpaceSampleParameter> Parameters => m_Parameters ?? Array.Empty<CharacterAnimationBlendSpaceSampleParameter>();

        internal CharacterAnimationBlendSpaceSample Clone(CharacterAnimationBlendSpaceSampleId sampleId)
        {
            return new CharacterAnimationBlendSpaceSample
            {
                m_SampleId = sampleId.IsValid ? sampleId.Value : throw new ArgumentException("Blend Space Sample identity is invalid.", nameof(sampleId)),
                m_Sequence = m_Sequence,
                m_Position = m_Position,
                m_Role = m_Role,
                m_StationaryNormalizedTime = m_StationaryNormalizedTime,
                m_Parameters = m_Parameters == null ? Array.Empty<CharacterAnimationBlendSpaceSampleParameter>() : (CharacterAnimationBlendSpaceSampleParameter[])m_Parameters.Clone()
            };
        }

        internal void Initialize(CharacterAnimationBlendSpaceSampleId sampleId, Vector2 position)
        {
            m_SampleId = sampleId.IsValid ? sampleId.Value : throw new ArgumentException("Blend Space Sample identity is invalid.", nameof(sampleId));
            m_Position = RequireFinite(position);
            m_Role = CharacterAnimationBlendSpaceSampleRole.DynamicCycle;
            m_StationaryNormalizedTime = 0f;
            m_Parameters = Array.Empty<CharacterAnimationBlendSpaceSampleParameter>();
        }

        internal void SetPosition(Vector2 position) => m_Position = RequireFinite(position);

        internal void SetSequence(CharacterAnimationSequenceAsset sequence)
        {
            if (!sequence)
                throw new ArgumentException("Blend Space Sample Sequence binding is invalid.");
            sequence.RequireValid();
            m_Sequence = sequence;
        }

        internal void SetRole(CharacterAnimationBlendSpaceSampleRole role, float stationaryNormalizedTime)
        {
            if (!Enum.IsDefined(typeof(CharacterAnimationBlendSpaceSampleRole), role) ||
                !float.IsFinite(stationaryNormalizedTime) || stationaryNormalizedTime < 0f || stationaryNormalizedTime > 1f)
                throw new ArgumentException("Blend Space Sample role is invalid.");
            m_Role = role;
            m_StationaryNormalizedTime = role == CharacterAnimationBlendSpaceSampleRole.StationaryPose ? stationaryNormalizedTime : 0f;
        }

        internal void SetParameters(CharacterAnimationBlendSpaceSampleParameter[] parameters)
        {
            m_Parameters = parameters == null
                ? Array.Empty<CharacterAnimationBlendSpaceSampleParameter>()
                : (CharacterAnimationBlendSpaceSampleParameter[])parameters.Clone();
        }

        static Vector2 RequireFinite(Vector2 value)
        {
            if (!float.IsFinite(value.x) || !float.IsFinite(value.y))
                throw new ArgumentOutOfRangeException(nameof(value));
            return value;
        }
    }

    [Serializable]
    public sealed class CharacterAnimationBlendSpacePoseParameterPolicy
    {
        [SerializeField] string m_ParameterId = string.Empty;
        [SerializeField] CharacterAnimationBlendSpaceParameterPolicy m_Policy;

        public PoseParameterId ParameterId => string.IsNullOrWhiteSpace(m_ParameterId) ? default : new PoseParameterId(m_ParameterId);
        public CharacterAnimationBlendSpaceParameterPolicy Policy => m_Policy;

        public CharacterAnimationBlendSpacePoseParameterPolicy() { }

        public CharacterAnimationBlendSpacePoseParameterPolicy(PoseParameterId parameterId, CharacterAnimationBlendSpaceParameterPolicy policy)
        {
            if (!parameterId.IsValid || !Enum.IsDefined(typeof(CharacterAnimationBlendSpaceParameterPolicy), policy))
                throw new ArgumentException("Blend Space Pose Parameter policy is invalid.");
            m_ParameterId = parameterId.Value;
            m_Policy = policy;
        }
    }

    [Serializable]
    public sealed class CharacterAnimationBlendSpacePreviewSettings
    {
        [SerializeField] Vector2 m_Parameter;
        [SerializeField, Range(0f, 1f)] float m_NormalizedTime;

        public Vector2 Parameter => m_Parameter;
        public float NormalizedTime => m_NormalizedTime;

        internal void Configure(Vector2 parameter, float normalizedTime)
        {
            if (!float.IsFinite(parameter.x) || !float.IsFinite(parameter.y) ||
                !float.IsFinite(normalizedTime) || normalizedTime < 0f || normalizedTime > 1f)
                throw new ArgumentException("Blend Space Preview settings are invalid.");
            m_Parameter = parameter;
            m_NormalizedTime = normalizedTime;
        }
    }

    [CreateAssetMenu(fileName = "CharacterAnimationBlendSpace", menuName = "3C/Character/Animation Blend Space")]
    public sealed class CharacterAnimationBlendSpaceAsset : ScriptableObject
    {
        [SerializeField] string m_BlendSpaceId = string.Empty;
        [SerializeField] string m_ContentRevision = string.Empty;
        [SerializeField] CharacterAnimationRigDefinition m_Rig;
        [SerializeField] CharacterAnimationBlendSpaceMode m_Mode = CharacterAnimationBlendSpaceMode.Linear1D;
        [SerializeField] CharacterAnimationBlendSpaceAxis m_XAxis = new CharacterAnimationBlendSpaceAxis();
        [SerializeReference] CharacterAnimationBlendSpaceAxis m_YAxis;
        [SerializeField] CharacterAnimationBlendSpacePhasePolicy m_PhasePolicy = CharacterAnimationBlendSpacePhasePolicy.SharedNormalizedPhase;
        [SerializeField] string m_PhaseReferenceSampleId = string.Empty;
        [SerializeField] CharacterAnimationBlendSpaceSample[] m_Samples = Array.Empty<CharacterAnimationBlendSpaceSample>();
        [SerializeField] CharacterAnimationBlendSpacePoseParameterPolicy[] m_PoseParameterPolicies = Array.Empty<CharacterAnimationBlendSpacePoseParameterPolicy>();
        [SerializeField] CharacterAnimationBlendSpacePreviewSettings m_Preview = new CharacterAnimationBlendSpacePreviewSettings();

        public CharacterAnimationBlendSpaceId BlendSpaceId => string.IsNullOrWhiteSpace(m_BlendSpaceId) ? default : new CharacterAnimationBlendSpaceId(m_BlendSpaceId);
        public string ContentRevision => m_ContentRevision ?? string.Empty;
        public CharacterAnimationRigDefinition Rig => m_Rig;
        public CharacterAnimationBlendSpaceMode Mode => m_Mode;
        public CharacterAnimationBlendSpaceAxis XAxis => m_XAxis;
        public CharacterAnimationBlendSpaceAxis YAxis => m_YAxis;
        public CharacterAnimationBlendSpacePhasePolicy PhasePolicy => m_PhasePolicy;
        public CharacterAnimationBlendSpaceSampleId PhaseReferenceSampleId => string.IsNullOrWhiteSpace(m_PhaseReferenceSampleId) ? default : new CharacterAnimationBlendSpaceSampleId(m_PhaseReferenceSampleId);
        public IReadOnlyList<CharacterAnimationBlendSpaceSample> Samples => m_Samples ?? Array.Empty<CharacterAnimationBlendSpaceSample>();
        public IReadOnlyList<CharacterAnimationBlendSpacePoseParameterPolicy> PoseParameterPolicies => m_PoseParameterPolicies ?? Array.Empty<CharacterAnimationBlendSpacePoseParameterPolicy>();
        public CharacterAnimationBlendSpacePreviewSettings Preview => m_Preview;
        public int AxisCount => m_Mode == CharacterAnimationBlendSpaceMode.Linear1D ? 1 : 2;

        internal void Initialize(CharacterAnimationBlendSpaceId blendSpaceId)
        {
            m_BlendSpaceId = blendSpaceId.IsValid ? blendSpaceId.Value : throw new ArgumentException("Blend Space identity is invalid.", nameof(blendSpaceId));
            TouchContentRevision();
        }

        internal void SetRig(CharacterAnimationRigDefinition rig)
        {
            m_Rig = rig ? rig : throw new ArgumentNullException(nameof(rig));
            TouchContentRevision();
        }

        internal void SetMode(CharacterAnimationBlendSpaceMode mode)
        {
            if (!Enum.IsDefined(typeof(CharacterAnimationBlendSpaceMode), mode))
                throw new ArgumentOutOfRangeException(nameof(mode));
            m_Mode = mode;
            if (mode == CharacterAnimationBlendSpaceMode.Linear1D)
                m_YAxis = null;
            else if (m_YAxis == null)
                m_YAxis = new CharacterAnimationBlendSpaceAxis();
            TouchContentRevision();
        }

        internal void SetAxis(int axisIndex, PoseParameterId parameterId, string unit, float minimum, float maximum)
        {
            if (axisIndex == 0)
            {
                m_XAxis ??= new CharacterAnimationBlendSpaceAxis();
                m_XAxis.Configure(parameterId, unit, minimum, maximum);
            }
            else if (axisIndex == 1 && AxisCount == 2)
            {
                m_YAxis ??= new CharacterAnimationBlendSpaceAxis();
                m_YAxis.Configure(parameterId, unit, minimum, maximum);
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(axisIndex));
            }
            TouchContentRevision();
        }

        internal void SetPhase(CharacterAnimationBlendSpacePhasePolicy policy, CharacterAnimationBlendSpaceSampleId referenceSampleId)
        {
            if (!Enum.IsDefined(typeof(CharacterAnimationBlendSpacePhasePolicy), policy))
                throw new ArgumentOutOfRangeException(nameof(policy));
            if ((policy == CharacterAnimationBlendSpacePhasePolicy.MarkerSegmentPhase ||
                 policy == CharacterAnimationBlendSpacePhasePolicy.GeneratedFootPhase) && !referenceSampleId.IsValid)
                throw new ArgumentException("Marker synchronized Blend Space requires a Phase Reference Sample.", nameof(referenceSampleId));
            if (policy == CharacterAnimationBlendSpacePhasePolicy.SharedNormalizedPhase && referenceSampleId.IsValid)
                throw new ArgumentException("Shared normalized Blend Space cannot retain a Phase Reference Sample.", nameof(referenceSampleId));
            m_PhasePolicy = policy;
            m_PhaseReferenceSampleId = referenceSampleId.Value ?? string.Empty;
            TouchContentRevision();
        }

        internal void SetSamples(CharacterAnimationBlendSpaceSample[] samples)
        {
            CharacterAnimationBlendSpaceSample[] next =
                samples == null
                    ? Array.Empty<CharacterAnimationBlendSpaceSample>()
                    : (CharacterAnimationBlendSpaceSample[])samples.Clone();
            if ((m_PhasePolicy == CharacterAnimationBlendSpacePhasePolicy.MarkerSegmentPhase ||
                 m_PhasePolicy == CharacterAnimationBlendSpacePhasePolicy.GeneratedFootPhase) &&
                PhaseReferenceSampleId.IsValid &&
                !Array.Exists(next, sample => sample != null && sample.SampleId.Equals(PhaseReferenceSampleId)))
            {
                throw new InvalidOperationException(
                    $"Marker synchronized Blend Space cannot remove its Phase Reference Sample '{PhaseReferenceSampleId}'.");
            }
            m_Samples = next;
            TouchContentRevision();
        }

        internal void SetPoseParameterPolicies(CharacterAnimationBlendSpacePoseParameterPolicy[] policies)
        {
            m_PoseParameterPolicies = policies == null ? Array.Empty<CharacterAnimationBlendSpacePoseParameterPolicy>() : (CharacterAnimationBlendSpacePoseParameterPolicy[])policies.Clone();
            TouchContentRevision();
        }

        internal void SetPreview(Vector2 parameter, float normalizedTime)
        {
            m_Preview ??= new CharacterAnimationBlendSpacePreviewSettings();
            m_Preview.Configure(parameter, normalizedTime);
        }

        internal void TouchContentRevision() => m_ContentRevision = Guid.NewGuid().ToString("N");

        public CharacterAnimationBlendSpaceSample FindSample(CharacterAnimationBlendSpaceSampleId sampleId)
        {
            for (int i = 0; i < Samples.Count; i++)
            {
                CharacterAnimationBlendSpaceSample sample = Samples[i];
                if (sample != null && sample.SampleId.Equals(sampleId))
                    return sample;
            }
            return null;
        }
    }
}
