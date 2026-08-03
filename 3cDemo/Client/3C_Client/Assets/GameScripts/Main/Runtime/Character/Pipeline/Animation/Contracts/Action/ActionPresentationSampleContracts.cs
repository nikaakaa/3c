using System;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public readonly struct ActionCommittedSampleWindow
    {
        internal ActionCommittedSampleWindow(
            ActionCommittedRawSample previous,
            ActionCommittedRawSample next,
            bool hasNext)
        {
            Previous = previous;
            Next = next;
            HasNext = hasNext;
            if (!IsValid)
                throw new ArgumentException("Action committed sample window is invalid.");
        }

        public ActionCommittedRawSample Previous { get; }
        public ActionCommittedRawSample Next { get; }
        public bool HasNext { get; }
        public bool IsValid =>
            Previous.IsValid &&
            (!HasNext ||
             Next.IsValid &&
             (Next.LocalLogicTick > Previous.LocalLogicTick ||
              Next.LocalLogicTick == Previous.LocalLogicTick &&
              Next.CommittedSequence > Previous.CommittedSequence));
    }

    public readonly struct ProjectedActionPresentationSample
    {
        public ProjectedActionPresentationSample(
            AnimationPlaybackId playbackId,
            EventId latestCommittedEventId,
            PresentationPoseSampleTime projectedRawSample,
            bool retentionProjection)
        {
            PlaybackId = playbackId;
            LatestCommittedEventId = latestCommittedEventId;
            ProjectedRawSample = projectedRawSample;
            RetentionProjection = retentionProjection;
            if (!IsValid)
                throw new ArgumentException("Projected Action presentation sample is invalid.");
        }

        public AnimationPlaybackId PlaybackId { get; }
        public EventId LatestCommittedEventId { get; }
        public PresentationPoseSampleTime ProjectedRawSample { get; }
        public bool RetentionProjection { get; }
        public bool IsValid =>
            PlaybackId.IsValid &&
            LatestCommittedEventId.IsValid &&
            ProjectedRawSample.IsValid;
    }
}
