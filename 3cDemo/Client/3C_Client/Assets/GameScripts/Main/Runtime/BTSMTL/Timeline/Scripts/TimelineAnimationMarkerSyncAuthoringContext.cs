using System.Collections.Generic;
using ThirdPersonSimulation;

namespace BTSMTL.Timeline
{
    public readonly struct TimelineAnimationMarkerSyncAuthoringIssue
    {
        public TimelineAnimationMarkerSyncAuthoringIssue(
            string code,
            string message,
            string authoringPath,
            string relatedIdentity)
        {
            Code = code ?? string.Empty;
            Message = message ?? string.Empty;
            AuthoringPath = authoringPath ?? string.Empty;
            RelatedIdentity = relatedIdentity ?? string.Empty;
        }

        public string Code { get; }
        public string Message { get; }
        public string AuthoringPath { get; }
        public string RelatedIdentity { get; }
    }

    public readonly struct TimelineAnimationMarkerSyncGroupMember
    {
        public TimelineAnimationMarkerSyncGroupMember(
            string producerIdentity,
            string displayName,
            AnimationChannelId animationChannelId,
            string syncGroupId,
            string directedPairCoverage,
            IReadOnlyList<string> markerIds)
        {
            ProducerIdentity = producerIdentity ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            AnimationChannelId = animationChannelId;
            SyncGroupId = syncGroupId ?? string.Empty;
            DirectedPairCoverage = directedPairCoverage ?? string.Empty;
            MarkerIds = markerIds ?? System.Array.Empty<string>();
        }

        public string ProducerIdentity { get; }
        public string DisplayName { get; }
        public AnimationChannelId AnimationChannelId { get; }
        public string SyncGroupId { get; }
        public string DirectedPairCoverage { get; }
        public IReadOnlyList<string> MarkerIds { get; }
    }

    public interface ITimelineAnimationMarkerSyncAuthoringContext
    {
        void CollectAnimationMarkerSyncAuthoringIssues(
            TimelineData timeline,
            string targetTrackAuthoringId,
            List<TimelineAnimationMarkerSyncAuthoringIssue> destination);
        void CollectAnimationMarkerSyncGroupMembers(
            TimelineData timeline,
            string targetTrackAuthoringId,
            List<TimelineAnimationMarkerSyncGroupMember> destination);
    }
}
