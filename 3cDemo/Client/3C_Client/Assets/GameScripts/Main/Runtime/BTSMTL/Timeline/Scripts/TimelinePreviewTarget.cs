using System;
using System.Collections.Generic;
using ThirdPersonSimulation;
using UnityEngine;

namespace BTSMTL.Timeline
{
    public readonly struct TimelineAnimationMarkerSyncPreviewCandidate
    {
        public TimelineAnimationMarkerSyncPreviewCandidate(
            string sourceTimelineAuthoringId,
            string sourceTrackAuthoringId,
            string displayName,
            AnimationChannelId animationChannelId,
            string syncGroupId)
        {
            SourceTimelineAuthoringId = sourceTimelineAuthoringId ?? string.Empty;
            SourceTrackAuthoringId = sourceTrackAuthoringId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            AnimationChannelId = animationChannelId.IsValid
                ? animationChannelId
                : throw new ArgumentException("Animation Channel identity is invalid.", nameof(animationChannelId));
            SyncGroupId = syncGroupId ?? string.Empty;
        }

        public string SourceTimelineAuthoringId { get; }
        public string SourceTrackAuthoringId { get; }
        public string DisplayName { get; }
        public AnimationChannelId AnimationChannelId { get; }
        public string SyncGroupId { get; }
        public bool IsValid => !string.IsNullOrEmpty(SourceTimelineAuthoringId) &&
                               !string.IsNullOrEmpty(SourceTrackAuthoringId);
    }

    public readonly struct TimelineAnimationMarkerSyncPreviewState
    {
        public TimelineAnimationMarkerSyncPreviewState(
            string targetTrackAuthoringId,
            string sourceProducerId,
            string targetProducerId,
            AnimationChannelId animationChannelId,
            string syncGroupId,
            string previousMarkerId,
            string nextMarkerId,
            float fraction,
            double rawTime,
            double effectiveTime,
            int effectiveCycle,
            int targetOccurrenceIndex,
            int relationDepth,
            string lifecyclePhase,
            string reason)
        {
            TargetTrackAuthoringId = targetTrackAuthoringId ?? string.Empty;
            SourceProducerId = sourceProducerId ?? string.Empty;
            TargetProducerId = targetProducerId ?? string.Empty;
            AnimationChannelId = animationChannelId.IsValid
                ? animationChannelId
                : throw new ArgumentException("Animation Channel identity is invalid.", nameof(animationChannelId));
            SyncGroupId = syncGroupId ?? string.Empty;
            PreviousMarkerId = previousMarkerId ?? string.Empty;
            NextMarkerId = nextMarkerId ?? string.Empty;
            Fraction = fraction;
            RawTime = rawTime;
            EffectiveTime = effectiveTime;
            EffectiveCycle = effectiveCycle;
            TargetOccurrenceIndex = targetOccurrenceIndex;
            RelationDepth = relationDepth;
            LifecyclePhase = lifecyclePhase ?? string.Empty;
            Reason = reason ?? string.Empty;
        }

        public string TargetTrackAuthoringId { get; }
        public string SourceProducerId { get; }
        public string TargetProducerId { get; }
        public AnimationChannelId AnimationChannelId { get; }
        public string SyncGroupId { get; }
        public string PreviousMarkerId { get; }
        public string NextMarkerId { get; }
        public float Fraction { get; }
        public double RawTime { get; }
        public double EffectiveTime { get; }
        public int EffectiveCycle { get; }
        public int TargetOccurrenceIndex { get; }
        public int RelationDepth { get; }
        public string LifecyclePhase { get; }
        public string Reason { get; }
    }

    public abstract class TimelinePreviewTarget : MonoBehaviour
    {
        public abstract bool CanPreviewTimeline { get; }
        public abstract string PreviewStatus { get; }
        public abstract void EvaluateTimelinePreview(
            Guid sessionId,
            TimelineData timeline,
            float previousTime,
            float currentTime,
            string sourceId,
            string sourceName,
            ulong evaluationTick,
            float presentationDeltaSeconds,
            bool resetLifecycle);
        public abstract void CollectAnimationMarkerSyncPreviewSources(
            TimelineData timeline,
            string targetTrackAuthoringId,
            List<TimelineAnimationMarkerSyncPreviewCandidate> destination);
        public abstract void ConfigureAnimationMarkerSyncPreviewSource(
            Guid sessionId,
            string targetTimelineAuthoringId,
            string targetTrackAuthoringId,
            string sourceTimelineAuthoringId,
            string sourceTrackAuthoringId);
        public abstract bool TryGetAnimationMarkerSyncPreviewState(
            Guid sessionId,
            string targetTrackAuthoringId,
            out TimelineAnimationMarkerSyncPreviewState state);
        public abstract void ClearTimelinePreview(Guid sessionId);
    }
}
