using System;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public readonly struct AnimationProducerId : IEquatable<AnimationProducerId>
    {
        public AnimationProducerId(string timelineAuthoringId, string trackAuthoringId)
        {
            TimelineAuthoringId = timelineAuthoringId ?? string.Empty;
            TrackAuthoringId = trackAuthoringId ?? string.Empty;
        }

        public string TimelineAuthoringId { get; }
        public string TrackAuthoringId { get; }
        public bool IsValid => !string.IsNullOrEmpty(TimelineAuthoringId) &&
                               !string.IsNullOrEmpty(TrackAuthoringId);

        public bool Equals(AnimationProducerId other)
        {
            return string.Equals(TimelineAuthoringId, other.TimelineAuthoringId, StringComparison.Ordinal) &&
                   string.Equals(TrackAuthoringId, other.TrackAuthoringId, StringComparison.Ordinal);
        }

        public override bool Equals(object obj) => obj is AnimationProducerId other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return ((TimelineAuthoringId?.GetHashCode() ?? 0) * 397) ^
                       (TrackAuthoringId?.GetHashCode() ?? 0);
            }
        }

        public override string ToString()
        {
            return IsValid ? $"{TimelineAuthoringId}/{TrackAuthoringId}" : "Invalid";
        }
    }

    public readonly struct AnimationPlaybackId : IEquatable<AnimationPlaybackId>
    {
        public AnimationPlaybackId(AnimationProducerId producerId, ulong generation)
        {
            ProducerId = producerId;
            Generation = generation;
        }

        public AnimationProducerId ProducerId { get; }
        public ulong Generation { get; }
        public bool IsValid => ProducerId.IsValid && Generation != 0;

        public bool Equals(AnimationPlaybackId other)
        {
            return ProducerId.Equals(other.ProducerId) && Generation == other.Generation;
        }

        public override bool Equals(object obj) => obj is AnimationPlaybackId other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return (ProducerId.GetHashCode() * 397) ^ Generation.GetHashCode();
            }
        }

        public override string ToString()
        {
            return IsValid ? $"{ProducerId}@{Generation}" : "Invalid";
        }
    }
}
