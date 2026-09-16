using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    [Serializable]
    public sealed class CharacterAnimationScalarCurveTrack
    {
        [SerializeField] int m_ParameterIndex = -1;
        [SerializeField] float[] m_Samples = Array.Empty<float>();

        public int ParameterIndex => m_ParameterIndex;
        public IReadOnlyList<float> Samples => m_Samples ?? Array.Empty<float>();

        public CharacterAnimationScalarCurveTrack() { }

        public CharacterAnimationScalarCurveTrack(int parameterIndex, float[] samples)
        {
            if (parameterIndex < 0 || samples == null || samples.Length < 2)
                throw new ArgumentException("Animation scalar curve track is invalid.");
            m_ParameterIndex = parameterIndex;
            m_Samples = (float[])samples.Clone();
            for (int i = 0; i < m_Samples.Length; i++)
            {
                if (!float.IsFinite(m_Samples[i]))
                    throw new ArgumentException("Animation scalar curve sample is invalid.", nameof(samples));
            }
        }

        public float Sample(float normalizedTime)
        {
            if (!float.IsFinite(normalizedTime))
                throw new ArgumentOutOfRangeException(nameof(normalizedTime));
            float clampedTime = Mathf.Clamp01(normalizedTime);
            if (clampedTime <= 0f)
                return Samples[0];
            if (clampedTime >= 1f)
                return Samples[Samples.Count - 1];
            return SamplePosition(clampedTime * (Samples.Count - 1));
        }

        internal float SamplePosition(float samplePosition)
        {
            if (!float.IsFinite(samplePosition))
                throw new ArgumentOutOfRangeException(nameof(samplePosition));
            int lastIndex = Samples.Count - 1;
            if (samplePosition <= 0f)
                return Samples[0];
            if (samplePosition >= lastIndex)
                return Samples[lastIndex];
            int lower = Mathf.FloorToInt(samplePosition);
            float factor = samplePosition - lower;
            float left = Samples[lower];
            float right = Samples[lower + 1];
            return (left - factor * left) + factor * right;
        }

        public void RequireValid()
        {
            if (ParameterIndex < 0 || Samples.Count < 2)
                throw new InvalidOperationException("Animation scalar curve track is invalid.");
            for (int i = 0; i < Samples.Count; i++)
            {
                if (!float.IsFinite(Samples[i]))
                    throw new InvalidOperationException("Animation scalar curve track contains a non-finite sample.");
            }
        }
    }

    [Serializable]
    public sealed class CharacterAnimationScalarCurvePage
    {
        public const string SchemaVersion = "character-animation-scalar-curve-page/v1";

        [SerializeField] string m_SchemaVersion = SchemaVersion;
        [SerializeField] int m_ParameterCount;
        [SerializeField] float m_SampleRate;
        [SerializeField] float m_DurationSeconds;
        [SerializeField] CharacterAnimationScalarCurveTrack[] m_Tracks = Array.Empty<CharacterAnimationScalarCurveTrack>();

        public string Schema => m_SchemaVersion ?? string.Empty;
        public int ParameterCount => m_ParameterCount;
        public float SampleRate => m_SampleRate;
        public float DurationSeconds => m_DurationSeconds;
        public IReadOnlyList<CharacterAnimationScalarCurveTrack> Tracks => m_Tracks ?? Array.Empty<CharacterAnimationScalarCurveTrack>();

        public CharacterAnimationScalarCurvePage() { }

        public CharacterAnimationScalarCurvePage(
            int parameterCount,
            float sampleRate,
            float durationSeconds,
            CharacterAnimationScalarCurveTrack[] tracks)
        {
            m_SchemaVersion = SchemaVersion;
            m_ParameterCount = parameterCount;
            m_SampleRate = sampleRate;
            m_DurationSeconds = durationSeconds;
            m_Tracks = tracks ?? Array.Empty<CharacterAnimationScalarCurveTrack>();
            RequireValid();
        }

        public bool TrySample(int parameterIndex, float timeSeconds, out float value)
        {
            RequireValid();
            if (parameterIndex < 0 || parameterIndex >= ParameterCount)
            {
                value = 0f;
                return false;
            }
            for (int i = 0; i < Tracks.Count; i++)
            {
                CharacterAnimationScalarCurveTrack track = Tracks[i];
                if (track.ParameterIndex != parameterIndex)
                    continue;
                float clampedTime = Mathf.Clamp(timeSeconds, 0f, DurationSeconds);
                float samplePosition = clampedTime >= DurationSeconds
                    ? track.Samples.Count - 1
                    : clampedTime * SampleRate;
                value = track.SamplePosition(samplePosition);
                return true;
            }
            value = 0f;
            return false;
        }

        public void RequireValid()
        {
            if (!string.Equals(Schema, SchemaVersion, StringComparison.Ordinal) ||
                ParameterCount <= 0 || SampleRate <= 0 ||
                !float.IsFinite(DurationSeconds) || DurationSeconds <= 0f)
                throw new InvalidOperationException("Animation scalar curve page is invalid.");
            var indices = new HashSet<int>();
            for (int i = 0; i < Tracks.Count; i++)
            {
                CharacterAnimationScalarCurveTrack track = Tracks[i] ?? throw new InvalidOperationException("Animation scalar curve page contains a missing track.");
                track.RequireValid();
                if (track.ParameterIndex >= ParameterCount || !indices.Add(track.ParameterIndex))
                    throw new InvalidOperationException("Animation scalar curve page contains a duplicate parameter track.");
                if (track.Samples.Count != checked(Mathf.RoundToInt(DurationSeconds * SampleRate) + 1))
                    throw new InvalidOperationException("Animation scalar curve page track sample count is invalid.");
            }
        }
    }

    [Serializable]
    public sealed class CharacterAnimationPropertyAuthoringBinding
    {
        [SerializeField] string m_ParameterId = string.Empty;
        [SerializeField] string m_RendererBindingId = string.Empty;
        [SerializeField] string m_AnimationCurvePath = string.Empty;
        [SerializeField] Mesh m_ExpectedMesh;
        [SerializeField] string m_MeshContentHash = string.Empty;
        [SerializeField] string m_BlendShapeName = string.Empty;
        [SerializeField] int m_BlendShapeIndex = -1;

        public PoseParameterId ParameterId => string.IsNullOrWhiteSpace(m_ParameterId) ? default : new PoseParameterId(m_ParameterId);
        public string RendererBindingId => m_RendererBindingId ?? string.Empty;
        public string AnimationCurvePath => m_AnimationCurvePath ?? string.Empty;
        public Mesh ExpectedMesh => m_ExpectedMesh;
        public string MeshContentHash => m_MeshContentHash ?? string.Empty;
        public string BlendShapeName => m_BlendShapeName ?? string.Empty;
        public int BlendShapeIndex => m_BlendShapeIndex;

        public void Configure(
            PoseParameterId parameterId,
            string rendererBindingId,
            string animationCurvePath,
            Mesh expectedMesh,
            string meshContentHash,
            string blendShapeName,
            int blendShapeIndex)
        {
            if (!parameterId.IsValid)
                throw new ArgumentException("Animation property Parameter identity is invalid.", nameof(parameterId));
            if (string.IsNullOrWhiteSpace(rendererBindingId))
                throw new ArgumentException("Animation property Renderer binding identity is required.", nameof(rendererBindingId));
            if (string.IsNullOrWhiteSpace(animationCurvePath))
                throw new ArgumentException("Animation property curve path is required.", nameof(animationCurvePath));
            if (!expectedMesh)
                throw new ArgumentNullException(nameof(expectedMesh));
            if (!CharacterAclHash.IsSha256(meshContentHash))
                throw new ArgumentException("Animation property Mesh content hash is invalid.", nameof(meshContentHash));
            if (string.IsNullOrWhiteSpace(blendShapeName) || blendShapeIndex < 0 || blendShapeIndex >= expectedMesh.blendShapeCount)
                throw new ArgumentException("Animation property BlendShape binding is invalid.", nameof(blendShapeIndex));
            string actualName = expectedMesh.GetBlendShapeName(blendShapeIndex);
            if (!string.Equals(actualName, blendShapeName, StringComparison.Ordinal))
                throw new ArgumentException("Animation property BlendShape name does not match the Mesh.", nameof(blendShapeName));
            m_ParameterId = parameterId.Value;
            m_RendererBindingId = rendererBindingId.Trim();
            m_AnimationCurvePath = animationCurvePath.Trim();
            m_ExpectedMesh = expectedMesh;
            m_MeshContentHash = meshContentHash.Trim().ToLowerInvariant();
            m_BlendShapeName = blendShapeName.Trim();
            m_BlendShapeIndex = blendShapeIndex;
        }

        public void RequireValid()
        {
            if (!ParameterId.IsValid || string.IsNullOrWhiteSpace(RendererBindingId) ||
                string.IsNullOrWhiteSpace(AnimationCurvePath) ||
                !ExpectedMesh || !CharacterAclHash.IsSha256(MeshContentHash) ||
                string.IsNullOrWhiteSpace(BlendShapeName) || BlendShapeIndex < 0 ||
                BlendShapeIndex >= ExpectedMesh.blendShapeCount ||
                !string.Equals(ExpectedMesh.GetBlendShapeName(BlendShapeIndex), BlendShapeName, StringComparison.Ordinal))
                throw new InvalidOperationException("Animation property authoring binding is invalid.");
        }
    }

    [Serializable]
    public sealed class CharacterPresentationAnimationPropertyBinding
    {
        [SerializeField] string m_BindingId = string.Empty;
        [SerializeField] string m_ParameterId = string.Empty;
        [SerializeField] int m_ParameterIndex = -1;
        [SerializeField] string m_Unit = string.Empty;
        [SerializeField] float m_DefaultValue;
        [SerializeField] string m_RendererBindingId = string.Empty;
        [SerializeField] Mesh m_ExpectedMesh;
        [SerializeField] string m_MeshContentHash = string.Empty;
        [SerializeField] string m_BlendShapeName = string.Empty;
        [SerializeField] int m_BlendShapeIndex = -1;

        public CharacterPresentationAnimationPropertyBinding() { }

        public CharacterPresentationAnimationPropertyBinding(
            string bindingId,
            PoseParameterId parameterId,
            int parameterIndex,
            string unit,
            float defaultValue,
            string rendererBindingId,
            Mesh expectedMesh,
            string meshContentHash,
            string blendShapeName,
            int blendShapeIndex)
        {
            if (string.IsNullOrWhiteSpace(bindingId) || !parameterId.IsValid || parameterIndex < 0 ||
                !float.IsFinite(defaultValue) || string.IsNullOrWhiteSpace(rendererBindingId) ||
                !expectedMesh || !CharacterAclHash.IsSha256(meshContentHash) ||
                string.IsNullOrWhiteSpace(blendShapeName) || blendShapeIndex < 0 ||
                blendShapeIndex >= expectedMesh.blendShapeCount ||
                !string.Equals(expectedMesh.GetBlendShapeName(blendShapeIndex), blendShapeName, StringComparison.Ordinal))
                throw new ArgumentException("Compiled animation property binding is invalid.");
            m_BindingId = bindingId.Trim();
            m_ParameterId = parameterId.Value;
            m_ParameterIndex = parameterIndex;
            m_Unit = unit?.Trim() ?? string.Empty;
            m_DefaultValue = defaultValue;
            m_RendererBindingId = rendererBindingId.Trim();
            m_ExpectedMesh = expectedMesh;
            m_MeshContentHash = meshContentHash.Trim().ToLowerInvariant();
            m_BlendShapeName = blendShapeName.Trim();
            m_BlendShapeIndex = blendShapeIndex;
        }

        public string BindingId => m_BindingId ?? string.Empty;
        public PoseParameterId ParameterId => string.IsNullOrWhiteSpace(m_ParameterId) ? default : new PoseParameterId(m_ParameterId);
        public int ParameterIndex => m_ParameterIndex;
        public string Unit => m_Unit ?? string.Empty;
        public float DefaultValue => m_DefaultValue;
        public string RendererBindingId => m_RendererBindingId ?? string.Empty;
        public Mesh ExpectedMesh => m_ExpectedMesh;
        public string MeshContentHash => m_MeshContentHash ?? string.Empty;
        public string BlendShapeName => m_BlendShapeName ?? string.Empty;
        public int BlendShapeIndex => m_BlendShapeIndex;

    }

    public readonly struct CharacterAnimationPropertyValue
    {
        public CharacterAnimationPropertyValue(
            CharacterPresentationAnimationPropertyBinding binding,
            float value)
        {
            Binding = binding ?? throw new ArgumentNullException(nameof(binding));
            Value = value;
            if (!float.IsFinite(value))
                throw new ArgumentOutOfRangeException(nameof(value));
        }

        public CharacterPresentationAnimationPropertyBinding Binding { get; }
        public float Value { get; }
    }
}
