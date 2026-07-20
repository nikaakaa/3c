using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Timeline;
using ThirdPersonCharacter.Pipeline.Graph;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public static class CharacterAnimationMarkerSyncAuthoringContext
    {
        public static bool Validate(
            CharacterAuthoringTopologyProjection topology,
            List<AnimationMarkerSyncAuthoringIssue> issues)
        {
            if (topology == null)
                throw new ArgumentNullException(nameof(topology));
            if (issues == null)
                throw new ArgumentNullException(nameof(issues));

            var inputs = new List<AnimationMarkerSyncAuthoringInput>();
            CollectInputs(topology, inputs);
            return AnimationMarkerSyncAuthoring.Validate(inputs, issues);
        }

        public static void ValidateTrackContext(
            CharacterAuthoringTopologyProjection topology,
            string timelineAuthoringId,
            string trackAuthoringId,
            List<AnimationMarkerSyncAuthoringIssue> issues)
        {
            if (issues == null)
                throw new ArgumentNullException(nameof(issues));

            string producerIdentity = $"{timelineAuthoringId}/{trackAuthoringId}";
            var allIssues = new List<AnimationMarkerSyncAuthoringIssue>();
            Validate(topology, allIssues);
            for (int i = 0; i < allIssues.Count; i++)
            {
                AnimationMarkerSyncAuthoringIssue issue = allIssues[i];
                if (string.Equals(issue.ProducerIdentity, producerIdentity, StringComparison.Ordinal) ||
                    string.Equals(issue.RelatedIdentity, producerIdentity, StringComparison.Ordinal))
                    issues.Add(issue);
            }
        }

        public static void CollectGroupMembers(
            CharacterAuthoringTopologyProjection topology,
            string timelineAuthoringId,
            string trackAuthoringId,
            List<TimelineAnimationMarkerSyncGroupMember> destination)
        {
            if (topology == null)
                throw new ArgumentNullException(nameof(topology));
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));
            destination.Clear();

            var inputs = new List<AnimationMarkerSyncAuthoringInput>();
            CollectInputs(topology, inputs);
            string targetIdentity = $"{timelineAuthoringId}/{trackAuthoringId}";
            AnimationMarkerSyncAuthoringInput target = default;
            bool found = false;
            for (int i = 0; i < inputs.Count; i++)
            {
                if (!string.Equals(inputs[i].ProducerIdentity, targetIdentity, StringComparison.Ordinal))
                    continue;
                target = inputs[i];
                found = true;
                break;
            }
            if (!found || target.Track.SyncMode != AnimationSyncMode.MarkerGroup)
                return;

            for (int i = 0; i < inputs.Count; i++)
            {
                AnimationMarkerSyncAuthoringInput input = inputs[i];
                if (input.Track.SyncMode != AnimationSyncMode.MarkerGroup ||
                    !string.Equals(input.Track.LayerId, target.Track.LayerId, StringComparison.Ordinal) ||
                    !string.Equals(
                        AnimationMarkerSyncAuthoring.NormalizeId(input.Track.SyncGroupId),
                        AnimationMarkerSyncAuthoring.NormalizeId(target.Track.SyncGroupId),
                        StringComparison.Ordinal))
                    continue;
                destination.Add(new TimelineAnimationMarkerSyncGroupMember(
                    input.ProducerIdentity,
                    $"{input.Timeline.Name}/{input.Track.Name}",
                    input.Track.LayerId,
                    AnimationMarkerSyncAuthoring.NormalizeId(input.Track.SyncGroupId),
                    BuildPairCoverage(input.Track),
                    input.Track.SyncMarkers
                        .Where(marker => marker != null)
                        .Select(marker => AnimationMarkerSyncAuthoring.NormalizeId(marker.MarkerId))
                        .Where(markerId => !string.IsNullOrEmpty(markerId))
                        .Distinct(StringComparer.Ordinal)
                        .OrderBy(markerId => markerId, StringComparer.Ordinal)
                        .ToArray()));
            }
            destination.Sort((left, right) =>
                string.CompareOrdinal(left.ProducerIdentity, right.ProducerIdentity));
        }

        public static void CollectMarkerIds(
            CharacterAuthoringTopologyProjection topology,
            string timelineAuthoringId,
            string trackAuthoringId,
            List<string> destination)
        {
            if (topology == null)
                throw new ArgumentNullException(nameof(topology));
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));
            destination.Clear();
            var inputs = new List<AnimationMarkerSyncAuthoringInput>();
            CollectInputs(topology, inputs);
            string targetIdentity = $"{timelineAuthoringId}/{trackAuthoringId}";
            AnimationMarkerSyncAuthoringInput target = default;
            bool found = false;
            for (int i = 0; i < inputs.Count; i++)
            {
                if (!string.Equals(inputs[i].ProducerIdentity, targetIdentity, StringComparison.Ordinal))
                    continue;
                target = inputs[i];
                found = true;
                break;
            }
            if (!found || target.Track.SyncMode != AnimationSyncMode.MarkerGroup)
                return;
            var markerIds = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < inputs.Count; i++)
            {
                AnimationTrack track = inputs[i].Track;
                if (track.SyncMode != AnimationSyncMode.MarkerGroup ||
                    !string.Equals(track.LayerId, target.Track.LayerId, StringComparison.Ordinal) ||
                    !string.Equals(
                        AnimationMarkerSyncAuthoring.NormalizeId(track.SyncGroupId),
                        AnimationMarkerSyncAuthoring.NormalizeId(target.Track.SyncGroupId),
                        StringComparison.Ordinal))
                    continue;
                for (int markerIndex = 0; markerIndex < track.SyncMarkers.Count; markerIndex++)
                {
                    string markerId = AnimationMarkerSyncAuthoring.NormalizeId(track.SyncMarkers[markerIndex]?.MarkerId);
                    if (!string.IsNullOrEmpty(markerId))
                        markerIds.Add(markerId);
                }
            }
            destination.AddRange(markerIds);
            destination.Sort(StringComparer.Ordinal);
        }

        static string BuildPairCoverage(AnimationTrack track)
        {
            var pairs = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 1; i < track.SyncMarkers.Count; i++)
                pairs.Add($"{track.SyncMarkers[i - 1].MarkerId}->{track.SyncMarkers[i].MarkerId}");
            if (track.SequenceTopology == AnimationMarkerSequenceTopology.Cyclic && track.SyncMarkers.Count > 1)
                pairs.Add($"{track.SyncMarkers[track.SyncMarkers.Count - 1].MarkerId}->{track.SyncMarkers[0].MarkerId}");
            var ordered = new List<string>(pairs);
            ordered.Sort(StringComparer.Ordinal);
            return string.Join(" | ", ordered);
        }

        static void CollectInputs(
            CharacterAuthoringTopologyProjection topology,
            List<AnimationMarkerSyncAuthoringInput> inputs)
        {
            var timelines = new List<TimelineData>();
            var timelineIds = new HashSet<string>(StringComparer.Ordinal);
            var callSites = new Dictionary<string, List<AnimationMarkerSyncCallSite>>(StringComparer.Ordinal);
            for (int i = 0; i < topology.Timelines.Count; i++)
            {
                CharacterAuthoringTimelineEntry entry = topology.Timelines[i];
                if (entry.Timeline == null || entry.Node == null)
                    continue;
                string timelineId = entry.Timeline.AuthoringId;
                if (timelineIds.Add(timelineId))
                    timelines.Add(entry.Timeline);
                if (!callSites.TryGetValue(timelineId, out List<AnimationMarkerSyncCallSite> values))
                {
                    values = new List<AnimationMarkerSyncCallSite>();
                    callSites.Add(timelineId, values);
                }
                values.Add(new AnimationMarkerSyncCallSite(
                    $"{entry.Route}/node:{entry.Node.GUID}",
                    entry.Node.PlaybackMode));
            }

            for (int timelineIndex = 0; timelineIndex < timelines.Count; timelineIndex++)
            {
                TimelineData timeline = timelines[timelineIndex];
                IReadOnlyList<AnimationMarkerSyncCallSite> timelineCallSites = callSites[timeline.AuthoringId];
                for (int trackIndex = 0; trackIndex < timeline.Tracks.Count; trackIndex++)
                {
                    if (timeline.Tracks[trackIndex] is not AnimationTrack track)
                        continue;
                    inputs.Add(new AnimationMarkerSyncAuthoringInput(
                        $"{timeline.AuthoringId}/{track.AuthoringId}",
                        timeline,
                        track,
                        timelineCallSites));
                }
            }
        }
    }
}
