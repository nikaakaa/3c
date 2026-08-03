using System;
using System.Collections.Generic;
using BTSMTL.Timeline;
using UnityEngine;
using AnimationClip = UnityEngine.AnimationClip;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public sealed class CharacterSequencePoseSourceBinding : CharacterPresentationPoseSourceBinding
    {
        [SerializeField] AnimationClip m_Clip;
        [SerializeField] bool m_Loop;
        [SerializeField] float m_DefaultPlayRate = 1f;
        [SerializeField] string m_MarkerGroupId = string.Empty;
        [SerializeField] AnimationMarkerSequenceTopology m_MarkerTopology;
        [SerializeField] AnimationMarkerSyncRole m_SyncRole;
        [SerializeField] PresentationPoseSourceMarker[] m_Markers = Array.Empty<PresentationPoseSourceMarker>();
        [SerializeField] AnimationCurve m_FootPlacementWeightCurve = AnimationCurve.Constant(0f, 1f, 1f);

        public override PresentationPoseSourceKind SourceKind => PresentationPoseSourceKind.Sequence;
        public override UnityEngine.Object SourceAsset => m_Clip;
        public AnimationClip Clip => m_Clip;
        public bool Loop => m_Loop;
        public float DefaultPlayRate => m_DefaultPlayRate;
        public string MarkerGroupId => m_MarkerGroupId ?? string.Empty;
        public AnimationMarkerSequenceTopology MarkerTopology => m_MarkerTopology;
        public AnimationMarkerSyncRole SyncRole => m_SyncRole;
        public IReadOnlyList<PresentationPoseSourceMarker> Markers =>
            Array.AsReadOnly(m_Markers ?? Array.Empty<PresentationPoseSourceMarker>());
        public AnimationCurve FootPlacementWeightCurve => CopyCurve(m_FootPlacementWeightCurve);

        public void Configure(
            CharacterSequencePoseSourceSlot slot,
            AnimationClip clip,
            CharacterAnimationRigDefinition rig,
            bool loop,
            float defaultPlayRate,
            string markerGroupId,
            AnimationMarkerSequenceTopology markerTopology,
            AnimationMarkerSyncRole syncRole,
            PresentationPoseSourceMarker[] markers,
            AnimationCurve footPlacementWeightCurve,
            string footAnalysisIdentity)
        {
            if (!clip || !float.IsFinite(defaultPlayRate) || defaultPlayRate <= 0f)
                throw new ArgumentException("Sequence Pose source binding is incomplete.");
            ConfigureCommon(slot, rig, footAnalysisIdentity);
            m_Clip = clip;
            m_Loop = loop;
            m_DefaultPlayRate = defaultPlayRate;
            m_MarkerGroupId = markerGroupId?.Trim() ?? string.Empty;
            m_MarkerTopology = markerTopology;
            m_SyncRole = syncRole;
            m_Markers = CopyMarkers(markers);
            m_FootPlacementWeightCurve = CopyCurve(footPlacementWeightCurve);
            RequireValid(rig);
        }

        public override void RequireValid(CharacterAnimationRigDefinition profileRig)
        {
            base.RequireValid(profileRig);
            if (!m_Clip || !float.IsFinite(m_Clip.length) || m_Clip.length <= 0f ||
                !float.IsFinite(m_Clip.frameRate) || m_Clip.frameRate <= 0f ||
                !float.IsFinite(m_DefaultPlayRate) || m_DefaultPlayRate <= 0f ||
                !HasValidCurve(m_FootPlacementWeightCurve))
            {
                throw new InvalidOperationException($"Sequence Pose source binding '{name}' is invalid.");
            }
            if (Markers.Count == 0)
            {
                if (!string.IsNullOrEmpty(MarkerGroupId) ||
                    MarkerTopology != AnimationMarkerSequenceTopology.Unspecified ||
                    SyncRole != AnimationMarkerSyncRole.Unspecified)
                    throw new InvalidOperationException($"Sequence Pose source binding '{name}' retains incomplete marker topology.");
                return;
            }
            if (string.IsNullOrWhiteSpace(MarkerGroupId) || Markers.Count < 2 ||
                MarkerTopology != (Loop ? AnimationMarkerSequenceTopology.Cyclic : AnimationMarkerSequenceTopology.Finite) ||
                SyncRole != AnimationMarkerSyncRole.CanBeLeader &&
                SyncRole != AnimationMarkerSyncRole.AlwaysLeader &&
                SyncRole != AnimationMarkerSyncRole.AlwaysFollower)
                throw new InvalidOperationException($"Sequence Pose source binding '{name}' marker topology is invalid.");
            int durationFrame = Mathf.Max(1, Mathf.RoundToInt(Clip.length * Clip.frameRate));
            var authoringIds = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < Markers.Count; i++)
            {
                PresentationPoseSourceMarker marker = Markers[i];
                if (marker == null || string.IsNullOrWhiteSpace(marker.AuthoringId) ||
                    string.IsNullOrWhiteSpace(marker.MarkerId) || !authoringIds.Add(marker.AuthoringId) ||
                    marker.Frame < 0 || marker.Frame >= durationFrame ||
                    i > 0 && marker.Frame <= Markers[i - 1].Frame)
                    throw new InvalidOperationException($"Sequence Pose source binding '{name}' marker #{i} is invalid.");
            }
        }

        internal AnimationCurve CopyFootPlacementWeightCurve() => CopyCurve(m_FootPlacementWeightCurve);

        static PresentationPoseSourceMarker[] CopyMarkers(PresentationPoseSourceMarker[] markers)
        {
            if (markers == null || markers.Length == 0)
                return Array.Empty<PresentationPoseSourceMarker>();
            var copy = new PresentationPoseSourceMarker[markers.Length];
            for (int i = 0; i < markers.Length; i++)
            {
                PresentationPoseSourceMarker marker = markers[i] ??
                    throw new ArgumentException($"Presentation Pose source marker #{i} is missing.", nameof(markers));
                copy[i] = new PresentationPoseSourceMarker(marker.AuthoringId, marker.MarkerId, marker.Frame);
            }
            return copy;
        }

        static AnimationCurve CopyCurve(AnimationCurve curve)
        {
            if (curve == null)
                throw new ArgumentNullException(nameof(curve));
            return new AnimationCurve(curve.keys)
            {
                preWrapMode = curve.preWrapMode,
                postWrapMode = curve.postWrapMode
            };
        }

        static bool HasValidCurve(AnimationCurve curve)
        {
            if (curve == null || curve.length == 0)
                return false;
            Keyframe[] keys = curve.keys;
            for (int i = 0; i < keys.Length; i++)
            {
                Keyframe key = keys[i];
                if (!float.IsFinite(key.time) || !float.IsFinite(key.value) ||
                    key.time < 0f || key.time > 1f || key.value < 0f || key.value > 1f ||
                    i > 0 && key.time <= keys[i - 1].time)
                    return false;
            }
            return true;
        }
    }
}
