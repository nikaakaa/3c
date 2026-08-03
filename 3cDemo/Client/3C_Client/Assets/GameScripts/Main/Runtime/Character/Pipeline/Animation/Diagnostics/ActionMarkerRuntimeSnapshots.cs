using System;

namespace ThirdPersonCharacter.Pipeline.Animation.Diagnostics
{
    public readonly struct ActionMarkerPlaybackSnapshot
    {
        public ActionMarkerPlaybackSnapshot(
            AnimationPlaybackId playbackId,
            PresentationPoseSampleTime projectedRawSample,
            PresentationPoseSampleTime effectiveSample,
            string previousMarkerId,
            string nextMarkerId,
            float markerSegmentFraction,
            bool mapped,
            bool rebased)
        {
            PlaybackId = playbackId;
            ProjectedRawSample = projectedRawSample;
            EffectiveSample = effectiveSample;
            PreviousMarkerId =
                previousMarkerId?.Trim() ?? string.Empty;
            NextMarkerId =
                nextMarkerId?.Trim() ?? string.Empty;
            MarkerSegmentFraction = markerSegmentFraction;
            Mapped = mapped;
            Rebased = rebased;
            if (!IsValid)
            {
                throw new ArgumentException(
                    "Action Marker playback snapshot is invalid.");
            }
        }

        public AnimationPlaybackId PlaybackId { get; }
        public PresentationPoseSampleTime ProjectedRawSample { get; }
        public PresentationPoseSampleTime EffectiveSample { get; }
        public string PreviousMarkerId { get; }
        public string NextMarkerId { get; }
        public float MarkerSegmentFraction { get; }
        public bool Mapped { get; }
        public bool Rebased { get; }
        public bool IsValid =>
            PlaybackId.IsValid &&
            ProjectedRawSample.IsValid &&
            EffectiveSample.IsValid &&
            float.IsFinite(MarkerSegmentFraction) &&
            MarkerSegmentFraction >= 0f &&
            MarkerSegmentFraction <= 1f;
    }

    public readonly struct ActionMarkerRelationSnapshot
    {
        public ActionMarkerRelationSnapshot(
            ActionMarkerRelationId relationId,
            AnimationPlaybackId sourcePlaybackId,
            AnimationPlaybackId targetPlaybackId,
            PresentationPoseSampleTime sourceEffectiveSample,
            PresentationPoseSampleTime targetProjectedRawSample,
            PresentationPoseSampleTime targetEffectiveSample,
            string previousMarkerId,
            string nextMarkerId,
            float markerSegmentFraction)
        {
            RelationId = relationId;
            SourcePlaybackId = sourcePlaybackId;
            TargetPlaybackId = targetPlaybackId;
            SourceEffectiveSample = sourceEffectiveSample;
            TargetProjectedRawSample = targetProjectedRawSample;
            TargetEffectiveSample = targetEffectiveSample;
            PreviousMarkerId =
                previousMarkerId?.Trim() ?? string.Empty;
            NextMarkerId =
                nextMarkerId?.Trim() ?? string.Empty;
            MarkerSegmentFraction = markerSegmentFraction;
            if (!IsValid)
            {
                throw new ArgumentException(
                    "Action Marker relation snapshot is invalid.");
            }
        }

        public ActionMarkerRelationId RelationId { get; }
        public AnimationPlaybackId SourcePlaybackId { get; }
        public AnimationPlaybackId TargetPlaybackId { get; }
        public PresentationPoseSampleTime SourceEffectiveSample { get; }
        public PresentationPoseSampleTime TargetProjectedRawSample { get; }
        public PresentationPoseSampleTime TargetEffectiveSample { get; }
        public string PreviousMarkerId { get; }
        public string NextMarkerId { get; }
        public float MarkerSegmentFraction { get; }
        public bool IsValid =>
            RelationId.IsValid &&
            SourcePlaybackId.IsValid &&
            TargetPlaybackId.IsValid &&
            !SourcePlaybackId.Equals(TargetPlaybackId) &&
            SourceEffectiveSample.IsValid &&
            TargetProjectedRawSample.IsValid &&
            TargetEffectiveSample.IsValid &&
            float.IsFinite(MarkerSegmentFraction) &&
            MarkerSegmentFraction >= 0f &&
            MarkerSegmentFraction <= 1f;
    }
}
