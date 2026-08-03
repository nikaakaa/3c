using System;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public readonly struct ActionMarkerRelationId :
        IEquatable<ActionMarkerRelationId>,
        IComparable<ActionMarkerRelationId>
    {
        public ActionMarkerRelationId(
            AnimationSlotId slotId,
            ulong generation)
        {
            SlotId = slotId.IsValid
                ? slotId
                : throw new ArgumentException(
                    "Action Marker relation Slot is required.",
                    nameof(slotId));
            Generation = generation != 0
                ? generation
                : throw new ArgumentOutOfRangeException(
                    nameof(generation));
        }

        public AnimationSlotId SlotId { get; }
        public ulong Generation { get; }
        public bool IsValid => SlotId.IsValid && Generation != 0;
        public int CompareTo(ActionMarkerRelationId other)
        {
            int slot = SlotId.CompareTo(other.SlotId);
            return slot != 0
                ? slot
                : Generation.CompareTo(other.Generation);
        }
        public bool Equals(ActionMarkerRelationId other) =>
            SlotId.Equals(other.SlotId) &&
            Generation == other.Generation;
        public override bool Equals(object obj) =>
            obj is ActionMarkerRelationId other && Equals(other);
        public override int GetHashCode() =>
            HashCode.Combine(SlotId, Generation);
        public override string ToString() =>
            IsValid
                ? $"animation-slot/{SlotId}/marker/{Generation}"
                : string.Empty;
    }

    public readonly struct ActionMarkerEffectiveSample
    {
        public ActionMarkerEffectiveSample(
            AnimationPlaybackId playbackId,
            PresentationPoseSampleTime projectedRawSample,
            PresentationPoseSampleTime effectiveSample,
            string previousMarkerId,
            string nextMarkerId,
            float segmentFraction,
            bool mapped,
            bool rebased)
        {
            PlaybackId = playbackId;
            ProjectedRawSample = projectedRawSample;
            EffectiveSample = effectiveSample;
            PreviousMarkerId = previousMarkerId?.Trim() ?? string.Empty;
            NextMarkerId = nextMarkerId?.Trim() ?? string.Empty;
            SegmentFraction = segmentFraction;
            Mapped = mapped;
            Rebased = rebased;
            if (!IsValid)
                throw new ArgumentException(
                    "Action Marker effective sample is invalid.");
        }

        public AnimationPlaybackId PlaybackId { get; }
        public PresentationPoseSampleTime ProjectedRawSample { get; }
        public PresentationPoseSampleTime EffectiveSample { get; }
        public string PreviousMarkerId { get; }
        public string NextMarkerId { get; }
        public float SegmentFraction { get; }
        public bool Mapped { get; }
        public bool Rebased { get; }
        public bool IsValid =>
            PlaybackId.IsValid &&
            ProjectedRawSample.IsValid &&
            EffectiveSample.IsValid &&
            float.IsFinite(SegmentFraction) &&
            SegmentFraction >= 0f &&
            SegmentFraction <= 1f &&
            (string.IsNullOrEmpty(PreviousMarkerId) ==
             string.IsNullOrEmpty(NextMarkerId));
    }
}
