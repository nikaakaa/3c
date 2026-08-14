using System;

namespace ThirdPersonCharacter.Pipeline.Animation.Diagnostics
{
    public enum ActionPresentationProjectionKind : byte
    {
        LatestCommitted = 1,
        Interpolation = 2,
        BoundedExtrapolation = 3
    }

    public readonly struct ActionPresentationTimeSnapshot
    {
        public ActionPresentationTimeSnapshot(
            AnimationPlaybackId playbackId,
            ulong actionInstanceId,
            ulong presentationFrame,
            ActionAnimationPlaybackLifecyclePhase lifecyclePhase,
            ActionCommittedSampleWindow committedWindow,
            PresentationPoseSampleTime projectedRawSample,
            PresentationPoseSampleTime markerEffectiveSample,
            bool retentionProjection,
            string previousMarkerId,
            string nextMarkerId,
            float markerSegmentFraction,
            bool markerMapped,
            bool markerRebased)
        {
            PlaybackId = playbackId;
            ActionInstanceId = actionInstanceId;
            PresentationFrame = presentationFrame;
            LifecyclePhase = lifecyclePhase;
            CommittedWindow = committedWindow;
            ProjectedRawSample = projectedRawSample;
            MarkerEffectiveSample = markerEffectiveSample;
            RetentionProjection = retentionProjection;
            PreviousMarkerId = previousMarkerId?.Trim() ?? string.Empty;
            NextMarkerId = nextMarkerId?.Trim() ?? string.Empty;
            MarkerSegmentFraction = markerSegmentFraction;
            MarkerMapped = markerMapped;
            MarkerRebased = markerRebased;
            ProjectionKind = retentionProjection
                ? ActionPresentationProjectionKind.BoundedExtrapolation
                : committedWindow.HasNext
                    ? ActionPresentationProjectionKind.Interpolation
                    : ActionPresentationProjectionKind.LatestCommitted;
            if (!IsValid)
                throw new ArgumentException("Action Presentation time snapshot is invalid.");
        }

        public AnimationPlaybackId PlaybackId { get; }
        public ulong ActionInstanceId { get; }
        public ulong PresentationFrame { get; }
        public ActionAnimationPlaybackLifecyclePhase LifecyclePhase { get; }
        public ActionCommittedSampleWindow CommittedWindow { get; }
        public PresentationPoseSampleTime ProjectedRawSample { get; }
        public PresentationPoseSampleTime MarkerEffectiveSample { get; }
        public ActionPresentationProjectionKind ProjectionKind { get; }
        public bool RetentionProjection { get; }
        public string PreviousMarkerId { get; }
        public string NextMarkerId { get; }
        public float MarkerSegmentFraction { get; }
        public bool MarkerMapped { get; }
        public bool MarkerRebased { get; }
        public bool IsValid =>
            PlaybackId.IsValid &&
            ActionInstanceId != 0 &&
            PresentationFrame != 0 &&
            Enum.IsDefined(typeof(ActionAnimationPlaybackLifecyclePhase), LifecyclePhase) &&
            LifecyclePhase != ActionAnimationPlaybackLifecyclePhase.PendingFirstSample &&
            LifecyclePhase != ActionAnimationPlaybackLifecyclePhase.Retired &&
            CommittedWindow.IsValid &&
            ProjectedRawSample.IsValid &&
            MarkerEffectiveSample.IsValid &&
            Enum.IsDefined(typeof(ActionPresentationProjectionKind), ProjectionKind) &&
            float.IsFinite(MarkerSegmentFraction) &&
            MarkerSegmentFraction >= 0f &&
            MarkerSegmentFraction <= 1f;
    }
}
