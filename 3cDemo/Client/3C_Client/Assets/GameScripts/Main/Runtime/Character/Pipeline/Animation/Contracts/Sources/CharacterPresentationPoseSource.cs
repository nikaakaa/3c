using System;
using BTSMTL.Timeline;
using UnityEngine;
using AnimationClip = UnityEngine.AnimationClip;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public enum PresentationPoseSourceKind : byte
    {
        Sequence = 1,
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
    public sealed class PresentationPoseSourceMarker
    {
        [SerializeField] string m_AuthoringId = string.Empty;
        [SerializeField] string m_MarkerId = string.Empty;
        [SerializeField] int m_Frame;

        public string AuthoringId => m_AuthoringId ?? string.Empty;
        public string MarkerId => m_MarkerId ?? string.Empty;
        public int Frame => m_Frame;

        public PresentationPoseSourceMarker()
        {
        }

        public PresentationPoseSourceMarker(string authoringId, string markerId, int frame)
        {
            if (string.IsNullOrWhiteSpace(authoringId) || string.IsNullOrWhiteSpace(markerId) || frame < 0)
                throw new ArgumentException("Presentation Pose source marker is invalid.");
            m_AuthoringId = authoringId.Trim();
            m_MarkerId = markerId.Trim();
            m_Frame = frame;
        }
    }

    [Serializable]
    public sealed class CharacterPresentationPoseSourcePlan
    {
        public const string CurrentSchemaVersion = "character-presentation-pose-source-plan/v3";

        [SerializeField] string m_SchemaVersion = CurrentSchemaVersion;
        [SerializeField] int m_SourceIndex = -1;
        [SerializeField] string m_BindingAssetIdentity = string.Empty;
        [SerializeField] string m_DisplayName = string.Empty;
        [SerializeField] AnimationClip m_Clip;
        [SerializeField] string m_RigId = string.Empty;
        [SerializeField] string m_RigRevision = string.Empty;
        [SerializeField] bool m_Loop;
        [SerializeField] float m_DefaultPlayRate;
        [SerializeField] AnimationMarkerSyncBinding m_MarkerSync;
        [SerializeField] AnimationCurve m_FootPlacementWeightCurve;
        [SerializeField] string m_FootAnalysisIdentity = string.Empty;
        [SerializeField] AnimationFootFeatureCurveSet m_LeftFootFeatures;
        [SerializeField] AnimationFootFeatureCurveSet m_RightFootFeatures;
        [SerializeField] string m_ContentRevision = string.Empty;

        internal CharacterPresentationPoseSourcePlan(
            PresentationPoseSourceIndex sourceIndex,
            string bindingAssetIdentity,
            CharacterSequencePoseSourceBinding binding,
            AnimationFootFeaturePair footFeatures)
        {
            if (!sourceIndex.IsValid || string.IsNullOrWhiteSpace(bindingAssetIdentity) ||
                !binding || !footFeatures.IsValid)
                throw new ArgumentException("Presentation Pose source compile input is incomplete.");
            binding.RequireValid(binding.Rig);
            m_SourceIndex = sourceIndex.Value;
            m_BindingAssetIdentity = bindingAssetIdentity.Trim();
            m_DisplayName = binding.Slot.name;
            m_Clip = binding.Clip;
            m_RigId = binding.Rig.RigId;
            m_RigRevision = binding.Rig.Revision;
            m_Loop = binding.Loop;
            m_DefaultPlayRate = binding.DefaultPlayRate;
            m_MarkerSync = CompileMarkerSync(binding);
            m_FootPlacementWeightCurve = binding.CopyFootPlacementWeightCurve();
            m_FootAnalysisIdentity = binding.FootAnalysisIdentity;
            m_LeftFootFeatures = footFeatures.Left;
            m_RightFootFeatures = footFeatures.Right;
            m_ContentRevision = binding.ContentRevision;
            RequireValid();
        }

        public string SchemaVersion => m_SchemaVersion ?? string.Empty;
        public PresentationPoseSourceIndex SourceIndex =>
            m_SourceIndex < 0 ? default : new PresentationPoseSourceIndex(m_SourceIndex);
        public string BindingAssetIdentity => m_BindingAssetIdentity ?? string.Empty;
        public string DisplayName => m_DisplayName ?? string.Empty;
        public AnimationClip Clip => m_Clip;
        public string RigId => m_RigId ?? string.Empty;
        public string RigRevision => m_RigRevision ?? string.Empty;
        public bool Loop => m_Loop;
        public float DefaultPlayRate => m_DefaultPlayRate;
        public AnimationMarkerSyncBinding MarkerSync => m_MarkerSync;
        public PoseParameterId FootPlacementWeightParameterId => AnimationPoseParameterIds.FootPlacementWeight;
        public string FootAnalysisIdentity => m_FootAnalysisIdentity ?? string.Empty;
        public AnimationFootFeatureCurveSet LeftFootFeatures => m_LeftFootFeatures;
        public AnimationFootFeatureCurveSet RightFootFeatures => m_RightFootFeatures;
        public string ContentRevision => m_ContentRevision ?? string.Empty;

        public void RequireValid()
        {
            if (!string.Equals(SchemaVersion, CurrentSchemaVersion, StringComparison.Ordinal) ||
                !SourceIndex.IsValid || string.IsNullOrWhiteSpace(BindingAssetIdentity) ||
                string.IsNullOrWhiteSpace(DisplayName) || !Clip || string.IsNullOrWhiteSpace(RigId) ||
                string.IsNullOrWhiteSpace(RigRevision) ||
                !float.IsFinite(DefaultPlayRate) || DefaultPlayRate <= 0f ||
                m_MarkerSync == null || !m_MarkerSync.TryValidate(out _) ||
                m_FootPlacementWeightCurve == null || m_FootPlacementWeightCurve.length == 0 ||
                string.IsNullOrWhiteSpace(FootAnalysisIdentity) ||
                m_LeftFootFeatures == null || m_RightFootFeatures == null ||
                string.IsNullOrWhiteSpace(ContentRevision))
            {
                throw new InvalidOperationException($"Compiled Presentation Pose source '{DisplayName}' is invalid.");
            }
        }

        public float SampleFootPlacementWeight(float normalizedTime)
        {
            RequireValid();
            float value = m_FootPlacementWeightCurve.Evaluate(Mathf.Clamp01(normalizedTime));
            if (!float.IsFinite(value))
                throw new InvalidOperationException($"Presentation Pose source '{DisplayName}' produced an invalid Foot Placement Weight.");
            return Mathf.Clamp01(value);
        }

        static AnimationMarkerSyncBinding CompileMarkerSync(CharacterSequencePoseSourceBinding binding)
        {
            if (binding.Markers.Count == 0)
                return new AnimationMarkerSyncBinding();
            int durationFrame = Mathf.Max(1, Mathf.RoundToInt(binding.Clip.length * binding.Clip.frameRate));
            var markers = new AnimationMarkerSyncMarkerBinding[binding.Markers.Count];
            for (int i = 0; i < markers.Length; i++)
            {
                PresentationPoseSourceMarker marker = binding.Markers[i];
                markers[i] = new AnimationMarkerSyncMarkerBinding(
                    marker.AuthoringId,
                    marker.MarkerId,
                    marker.Frame,
                    marker.Frame / binding.Clip.frameRate);
            }
            int segmentCount = markers.Length - 1 +
                               (binding.MarkerTopology == AnimationMarkerSequenceTopology.Cyclic ? 1 : 0);
            var segments = new AnimationMarkerSyncSegmentOccurrence[segmentCount];
            for (int i = 0; i < markers.Length - 1; i++)
            {
                segments[i] = new AnimationMarkerSyncSegmentOccurrence(
                    i,
                    i,
                    i + 1,
                    markers[i].MarkerId,
                    markers[i + 1].MarkerId,
                    markers[i].TimeSeconds,
                    markers[i + 1].TimeSeconds,
                    false);
            }
            if (binding.MarkerTopology == AnimationMarkerSequenceTopology.Cyclic)
            {
                int index = segments.Length - 1;
                segments[index] = new AnimationMarkerSyncSegmentOccurrence(
                    index,
                    markers.Length - 1,
                    0,
                    markers[markers.Length - 1].MarkerId,
                    markers[0].MarkerId,
                    markers[markers.Length - 1].TimeSeconds,
                    binding.Clip.length + markers[0].TimeSeconds,
                    true);
            }
            return new AnimationMarkerSyncBinding(
                AnimationSyncMode.MarkerGroup,
                binding.MarkerGroupId,
                binding.MarkerTopology,
                binding.SyncRole,
                durationFrame,
                binding.Clip.length,
                markers,
                segments);
        }
    }
}
