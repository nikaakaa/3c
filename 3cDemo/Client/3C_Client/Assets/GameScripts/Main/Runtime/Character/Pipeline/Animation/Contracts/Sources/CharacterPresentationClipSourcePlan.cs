using System;
using BTSMTL.Diagnostics;
using BTSMTL.Timeline;
using UnityEngine;
using AnimationClip = UnityEngine.AnimationClip;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public enum PresentationPoseSourceKind : byte
    {
        Clip = 1,
        BlendSpace = 2,
        MotionMatching = 3
    }

    public readonly struct PresentationPoseSourceIndex : IEquatable<PresentationPoseSourceIndex>, IComparable<PresentationPoseSourceIndex>
    {
        readonly int m_Encoded;

        public PresentationPoseSourceIndex(int value)
        {
            m_Encoded = value >= 0 ? checked(value + 1) : throw new ArgumentOutOfRangeException(nameof(value));
        }

        public int Value => m_Encoded - 1;
        public bool IsValid => m_Encoded > 0;
        public int CompareTo(PresentationPoseSourceIndex other) => Value.CompareTo(other.Value);
        public bool Equals(PresentationPoseSourceIndex other) => Value == other.Value;
        public override bool Equals(object obj) => obj is PresentationPoseSourceIndex other && Equals(other);
        public override int GetHashCode() => Value;
        public override string ToString() => IsValid ? Value.ToString() : string.Empty;
        public static bool operator ==(PresentationPoseSourceIndex left, PresentationPoseSourceIndex right) => left.Equals(right);
        public static bool operator !=(PresentationPoseSourceIndex left, PresentationPoseSourceIndex right) => !left.Equals(right);
    }

    [Serializable]
    public sealed class CharacterPresentationPoseSourcePlan
    {
        public const string CurrentSchemaVersion = "character-presentation-clip-source-plan/v2";

        [SerializeField] string m_SchemaVersion = CurrentSchemaVersion;
        [SerializeField] int m_SourceIndex = -1;
        [SerializeField] string m_BindingAssetIdentity = string.Empty;
        [SerializeField] string m_DisplayName = string.Empty;
        [SerializeField] AnimationClip m_Clip;
        [SerializeField] CharacterAnimationSamplingBackendKind m_Backend = CharacterAnimationSamplingBackendKind.NativeClip;
        [SerializeField] int m_ResourceCatalogIndex = -1;
        [SerializeField] int m_GroupClipIndex = -1;
        [SerializeField] bool m_Looping;
        [SerializeField] string m_ClipIdentity = string.Empty;
        [SerializeField] string m_FullClipDependencyHash = string.Empty;
        [SerializeField] string m_AnalysisInputHash = string.Empty;
        [SerializeField] string m_RegisteredCurveHash = string.Empty;
        [SerializeField] string m_RigId = string.Empty;
        [SerializeField] string m_RigRevision = string.Empty;
        [SerializeField] float m_SourceDurationSeconds;
        [SerializeField] AnimationCurve m_FootPlacementWeightCurve;
        [SerializeField] AnimationFootStepObservationCurvePair m_FootStepObservation;
        [SerializeField] string m_FootAnalysisIdentity = string.Empty;
        [SerializeField] AnimationFootFeatureCurveSet m_LeftFootFeatures;
        [SerializeField] AnimationFootFeatureCurveSet m_RightFootFeatures;
        [SerializeReference] CharacterAnimationScalarCurvePage m_ScalarPage;
        [SerializeField] string m_ContentRevision = string.Empty;

        internal CharacterPresentationPoseSourcePlan(
            PresentationPoseSourceIndex sourceIndex,
            string bindingAssetIdentity,
            CharacterClipPoseSourceBinding binding,
            CharacterAnimationRigDefinition rig,
            string footAnalysisIdentity,
            string clipIdentity,
            string fullClipDependencyHash,
            string analysisInputHash,
            string registeredCurveHash,
            float sourceDurationSeconds,
            AnimationCurve normalizedFootPlacementWeightCurve,
            AnimationFootStepObservationCurvePair footStepObservation,
            AnimationFootFeaturePair footFeatures,
            CharacterAnimationSamplingBackendKind backend = CharacterAnimationSamplingBackendKind.NativeClip,
            CharacterAnimationScalarCurvePage scalarPage = null,
            int resourceCatalogIndex = -1,
            int groupClipIndex = -1)
        {
            if (!sourceIndex.IsValid || string.IsNullOrWhiteSpace(bindingAssetIdentity) ||
                !binding || !rig || string.IsNullOrWhiteSpace(footAnalysisIdentity) ||
                string.IsNullOrWhiteSpace(clipIdentity) ||
                string.IsNullOrWhiteSpace(fullClipDependencyHash) ||
                string.IsNullOrWhiteSpace(analysisInputHash) ||
                string.IsNullOrWhiteSpace(registeredCurveHash) ||
                !float.IsFinite(sourceDurationSeconds) || sourceDurationSeconds <= 0f ||
                normalizedFootPlacementWeightCurve == null ||
                normalizedFootPlacementWeightCurve.length < 2 || footStepObservation == null ||
                !footFeatures.IsValid)
            {
                throw new ArgumentException("Presentation Clip source compile input is incomplete.");
            }
            binding.RequireValid(rig);
            m_SourceIndex = sourceIndex.Value;
            m_BindingAssetIdentity = bindingAssetIdentity.Trim();
            m_DisplayName = binding.Slot.name;
            m_Backend = backend;
            m_Clip = backend == CharacterAnimationSamplingBackendKind.Acl ? null : binding.Clip;
            m_ResourceCatalogIndex = backend == CharacterAnimationSamplingBackendKind.Acl
                ? resourceCatalogIndex
                : -1;
            m_GroupClipIndex = backend == CharacterAnimationSamplingBackendKind.Acl
                ? groupClipIndex
                : -1;
            m_Looping = binding.Clip.isLooping;
            m_ClipIdentity = clipIdentity.Trim();
            m_FullClipDependencyHash = fullClipDependencyHash.Trim();
            m_AnalysisInputHash = analysisInputHash.Trim();
            m_RegisteredCurveHash = registeredCurveHash.Trim();
            m_RigId = rig.RigId;
            m_RigRevision = rig.Revision;
            m_SourceDurationSeconds = sourceDurationSeconds;
            m_FootPlacementWeightCurve = normalizedFootPlacementWeightCurve;
            m_FootStepObservation = footStepObservation;
            m_FootAnalysisIdentity = footAnalysisIdentity.Trim();
            m_LeftFootFeatures = footFeatures.Left;
            m_RightFootFeatures = footFeatures.Right;
            m_ScalarPage = scalarPage;
            m_ContentRevision = $"{binding.ContentRevision}:{m_RegisteredCurveHash}";
            RequireValid();
        }

        internal CharacterPresentationPoseSourcePlan(
            PresentationPoseSourceIndex sourceIndex,
            string sourceIdentity,
            AnimationClip clip,
            CharacterAnimationRigDefinition rig,
            string footAnalysisIdentity,
            string clipIdentity,
            string fullClipDependencyHash,
            string analysisInputHash,
            string registeredCurveHash,
            float sourceDurationSeconds,
            AnimationCurve normalizedFootPlacementWeightCurve,
            AnimationFootStepObservationCurvePair footStepObservation,
            AnimationFootFeaturePair footFeatures,
            CharacterAnimationSamplingBackendKind backend = CharacterAnimationSamplingBackendKind.NativeClip,
            CharacterAnimationScalarCurvePage scalarPage = null,
            int resourceCatalogIndex = -1,
            int groupClipIndex = -1)
        {
            if (!sourceIndex.IsValid || string.IsNullOrWhiteSpace(sourceIdentity) || !clip || !rig ||
                string.IsNullOrWhiteSpace(footAnalysisIdentity) || string.IsNullOrWhiteSpace(clipIdentity) ||
                string.IsNullOrWhiteSpace(fullClipDependencyHash) || string.IsNullOrWhiteSpace(analysisInputHash) ||
                string.IsNullOrWhiteSpace(registeredCurveHash) || !float.IsFinite(sourceDurationSeconds) ||
                sourceDurationSeconds <= 0f || normalizedFootPlacementWeightCurve == null ||
                normalizedFootPlacementWeightCurve.length < 2 || footStepObservation == null || !footFeatures.IsValid)
                throw new ArgumentException("Presentation direct Clip source compile input is incomplete.");
            m_SourceIndex = sourceIndex.Value;
            m_BindingAssetIdentity = sourceIdentity.Trim();
            m_DisplayName = clip.name;
            m_Backend = backend;
            m_Clip = backend == CharacterAnimationSamplingBackendKind.Acl ? null : clip;
            m_ResourceCatalogIndex = backend == CharacterAnimationSamplingBackendKind.Acl ? resourceCatalogIndex : -1;
            m_GroupClipIndex = backend == CharacterAnimationSamplingBackendKind.Acl ? groupClipIndex : -1;
            m_Looping = clip.isLooping;
            m_ClipIdentity = clipIdentity.Trim();
            m_FullClipDependencyHash = fullClipDependencyHash.Trim();
            m_AnalysisInputHash = analysisInputHash.Trim();
            m_RegisteredCurveHash = registeredCurveHash.Trim();
            m_RigId = rig.RigId;
            m_RigRevision = rig.Revision;
            m_SourceDurationSeconds = sourceDurationSeconds;
            m_FootPlacementWeightCurve = normalizedFootPlacementWeightCurve;
            m_FootStepObservation = footStepObservation;
            m_FootAnalysisIdentity = footAnalysisIdentity.Trim();
            m_LeftFootFeatures = footFeatures.Left;
            m_RightFootFeatures = footFeatures.Right;
            m_ScalarPage = scalarPage;
            m_ContentRevision = $"{m_BindingAssetIdentity}:{m_RegisteredCurveHash}";
            RequireValid();
        }

        public string SchemaVersion => m_SchemaVersion ?? string.Empty;
        public PresentationPoseSourceIndex SourceIndex =>
            m_SourceIndex < 0 ? default : new PresentationPoseSourceIndex(m_SourceIndex);
        public string BindingAssetIdentity => m_BindingAssetIdentity ?? string.Empty;
        public string DisplayName => m_DisplayName ?? string.Empty;
        public AnimationClip Clip => m_Clip;
        public CharacterAnimationSamplingBackendKind Backend => m_Backend;
        public int ResourceCatalogIndex => m_ResourceCatalogIndex;
        public int GroupClipIndex => m_GroupClipIndex;
        public bool IsAcl => Backend == CharacterAnimationSamplingBackendKind.Acl;
        public bool IsLooping => m_Looping;
        public string ClipIdentity => m_ClipIdentity ?? string.Empty;
        public string FullClipDependencyHash => m_FullClipDependencyHash ?? string.Empty;
        public string AnalysisInputHash => m_AnalysisInputHash ?? string.Empty;
        public string RegisteredCurveHash => m_RegisteredCurveHash ?? string.Empty;
        public string RigId => m_RigId ?? string.Empty;
        public string RigRevision => m_RigRevision ?? string.Empty;
        public float SourceDurationSeconds => m_SourceDurationSeconds;
        public PoseParameterId FootPlacementWeightParameterId => AnimationPoseParameterIds.FootPlacementWeight;
        public AnimationFootStepObservationCurvePair FootStepObservation => m_FootStepObservation;
        public string FootAnalysisIdentity => m_FootAnalysisIdentity ?? string.Empty;
        public AnimationFootFeatureCurveSet LeftFootFeatures => m_LeftFootFeatures;
        public AnimationFootFeatureCurveSet RightFootFeatures => m_RightFootFeatures;
        public CharacterAnimationScalarCurvePage ScalarPage => m_ScalarPage;
        public string ContentRevision => m_ContentRevision ?? string.Empty;

        public void RequireValid()
        {
            if (!string.Equals(SchemaVersion, CurrentSchemaVersion, StringComparison.Ordinal) ||
                !SourceIndex.IsValid || string.IsNullOrWhiteSpace(BindingAssetIdentity) ||
                string.IsNullOrWhiteSpace(DisplayName) ||
                !Enum.IsDefined(typeof(CharacterAnimationSamplingBackendKind), Backend) ||
                Backend == CharacterAnimationSamplingBackendKind.NativeClip && !Clip ||
                Backend == CharacterAnimationSamplingBackendKind.Acl && (m_ResourceCatalogIndex < 0 || m_GroupClipIndex < 0) ||
                string.IsNullOrWhiteSpace(ClipIdentity) ||
                string.IsNullOrWhiteSpace(FullClipDependencyHash) ||
                string.IsNullOrWhiteSpace(AnalysisInputHash) ||
                string.IsNullOrWhiteSpace(RegisteredCurveHash) ||
                string.IsNullOrWhiteSpace(RigId) || string.IsNullOrWhiteSpace(RigRevision) ||
                !float.IsFinite(SourceDurationSeconds) || SourceDurationSeconds <= 0f ||
                m_FootPlacementWeightCurve == null || m_FootPlacementWeightCurve.length == 0 ||
                m_FootStepObservation == null ||
                string.IsNullOrWhiteSpace(FootAnalysisIdentity) ||
                m_LeftFootFeatures == null || m_RightFootFeatures == null ||
                string.IsNullOrWhiteSpace(ContentRevision))
            {
                throw new InvalidOperationException($"Compiled Presentation Clip source '{DisplayName}' is invalid.");
            }
            m_FootStepObservation.RequireValid();
            if (ScalarPage != null)
                ScalarPage.RequireValid();
        }

        internal void SampleNativeProperties(
            float normalizedTime,
            float[] values,
            byte[] availability)
        {
            if (IsAcl || ScalarPage == null || values == null || availability == null || values.Length != availability.Length)
                return;
            for (int i = 0; i < ScalarPage.Tracks.Count; i++)
            {
                CharacterAnimationScalarCurveTrack track = ScalarPage.Tracks[i];
                if (track.ParameterIndex < 0 || track.ParameterIndex >= values.Length)
                    throw new InvalidOperationException("Presentation source scalar page parameter index is outside the Program page.");
                values[track.ParameterIndex] = track.Sample(normalizedTime);
                availability[track.ParameterIndex] = 1;
            }
        }

        public float SampleFootPlacementWeight(float normalizedTime)
        {
            RequireValid();
            return SampleFootPlacementWeightPrepared(normalizedTime);
        }

        internal float SampleFootPlacementWeightPrepared(float normalizedTime)
        {
            float value = m_FootPlacementWeightCurve.Evaluate(Mathf.Clamp01(normalizedTime));
            if (!float.IsFinite(value))
                throw new InvalidOperationException($"Presentation Clip source '{DisplayName}' produced an invalid Foot Placement Weight.");
            return Mathf.Clamp01(value);
        }

    }
}
