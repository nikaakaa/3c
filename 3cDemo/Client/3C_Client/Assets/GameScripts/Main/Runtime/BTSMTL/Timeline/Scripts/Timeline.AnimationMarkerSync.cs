using System;
using System.Collections.Generic;
using BTSMTL.Diagnostics;
using UnityEngine;

namespace BTSMTL.Timeline
{
    public enum AnimationSyncMode
    {
        Unspecified = 0,
        None = 1,
        MarkerGroup = 2
    }

    public enum AnimationMarkerSequenceTopology
    {
        Unspecified = 0,
        Finite = 1,
        Cyclic = 2
    }

    public enum AnimationMarkerSyncRole
    {
        Unspecified = 0,
        CanBeLeader = 1,
        AlwaysLeader = 2,
        AlwaysFollower = 3
    }

    [Serializable]
    public sealed class AnimationSyncMarker
    {
        [SerializeField] string m_AuthoringId;
        [SerializeField] string m_MarkerId;
        [SerializeField] int m_Frame;

        public string AuthoringId => m_AuthoringId ?? string.Empty;
        public string MarkerId => m_MarkerId ?? string.Empty;
        public int Frame => m_Frame;

        internal AnimationSyncMarker(string authoringId, string markerId, int frame)
        {
            m_AuthoringId = authoringId ?? string.Empty;
            m_MarkerId = markerId ?? string.Empty;
            m_Frame = frame;
        }

#if UNITY_EDITOR
        internal bool EnsureAuthoringIdentity()
        {
            if (AuthoringIdentity.IsValid(m_AuthoringId))
                return false;
            m_AuthoringId = AuthoringIdentity.Create();
            return true;
        }

        internal void RegenerateAuthoringIdentity()
        {
            m_AuthoringId = AuthoringIdentity.Create();
        }

        internal void Rename(string markerId)
        {
            m_MarkerId = markerId;
        }

        internal void Move(int frame)
        {
            m_Frame = frame;
        }
#endif
    }

    public partial class AnimationTrack
    {
        [SerializeField] AnimationSyncMode m_SyncMode;
        [SerializeField] string m_SyncGroupId;
        [SerializeField] AnimationMarkerSequenceTopology m_SequenceTopology;
        [SerializeField] AnimationMarkerSyncRole m_SyncRole;
        [SerializeField] List<AnimationSyncMarker> m_SyncMarkers = new List<AnimationSyncMarker>();

        public AnimationSyncMode SyncMode => m_SyncMode;
        public string SyncGroupId => m_SyncGroupId ?? string.Empty;
        public AnimationMarkerSequenceTopology SequenceTopology => m_SequenceTopology;
        public AnimationMarkerSyncRole SyncRole => m_SyncRole;
        public IReadOnlyList<AnimationSyncMarker> SyncMarkers => m_SyncMarkers;

#if UNITY_EDITOR
        public void ConfigureNone()
        {
            m_SyncMode = AnimationSyncMode.None;
            m_SyncGroupId = string.Empty;
            m_SequenceTopology = AnimationMarkerSequenceTopology.Unspecified;
            m_SyncRole = AnimationMarkerSyncRole.Unspecified;
            m_SyncMarkers.Clear();
            RebindTimeline();
        }

        public void ConfigureMarkerGroup(
            string syncGroupId,
            AnimationMarkerSequenceTopology topology,
            AnimationMarkerSyncRole syncRole)
        {
            string canonicalGroupId = AnimationMarkerSyncAuthoring.NormalizeId(syncGroupId);
            if (string.IsNullOrEmpty(canonicalGroupId))
                throw new ArgumentException("SyncGroupId is required.", nameof(syncGroupId));
            if (topology != AnimationMarkerSequenceTopology.Finite &&
                topology != AnimationMarkerSequenceTopology.Cyclic)
                throw new ArgumentOutOfRangeException(nameof(topology));
            if (syncRole != AnimationMarkerSyncRole.CanBeLeader &&
                syncRole != AnimationMarkerSyncRole.AlwaysLeader &&
                syncRole != AnimationMarkerSyncRole.AlwaysFollower)
                throw new ArgumentOutOfRangeException(nameof(syncRole));
            m_SyncMode = AnimationSyncMode.MarkerGroup;
            m_SyncGroupId = canonicalGroupId;
            m_SequenceTopology = topology;
            m_SyncRole = syncRole;
            RebindTimeline();
        }

        public AnimationSyncMarker AddMarker(string markerId, int frame)
        {
            return EnsureMarker(AuthoringIdentity.Create(), markerId, frame);
        }

        public AnimationSyncMarker EnsureMarker(string markerAuthoringId, string markerId, int frame)
        {
            if (m_SyncMode != AnimationSyncMode.MarkerGroup)
                throw new InvalidOperationException("AnimationTrack must use MarkerGroup before markers can be edited.");
            if (!AuthoringIdentity.IsValid(markerAuthoringId))
                throw new ArgumentException("Marker authoring identity is invalid.", nameof(markerAuthoringId));
            RequireMarkerId(markerId);
            if (frame < 0)
                throw new ArgumentOutOfRangeException(nameof(frame));

            AnimationSyncMarker marker = FindMarker(markerAuthoringId);
            if (marker == null)
            {
                marker = new AnimationSyncMarker(markerAuthoringId, markerId, frame);
                m_SyncMarkers.Add(marker);
            }
            else
            {
                marker.Rename(markerId);
                marker.Move(frame);
            }
            SortMarkers();
            RebindTimeline();
            return marker;
        }

        public void RenameMarker(string markerAuthoringId, string markerId)
        {
            RequireMarkerId(markerId);
            RequireMarker(markerAuthoringId).Rename(markerId);
            RebindTimeline();
        }

        public void MoveMarker(string markerAuthoringId, int frame)
        {
            if (frame < 0)
                throw new ArgumentOutOfRangeException(nameof(frame));
            RequireMarker(markerAuthoringId).Move(frame);
            SortMarkers();
            RebindTimeline();
        }

        public void DeleteMarker(string markerAuthoringId)
        {
            AnimationSyncMarker marker = RequireMarker(markerAuthoringId);
            m_SyncMarkers.Remove(marker);
            RebindTimeline();
        }

        public bool EnsureOwnedAuthoringIdentities()
        {
            bool changed = false;
            for (int i = 0; i < m_SyncMarkers.Count; i++)
                changed |= m_SyncMarkers[i]?.EnsureAuthoringIdentity() ?? false;
            return changed;
        }

        public void RegenerateOwnedAuthoringIdentities()
        {
            for (int i = 0; i < m_SyncMarkers.Count; i++)
                m_SyncMarkers[i]?.RegenerateAuthoringIdentity();
        }

        AnimationSyncMarker RequireMarker(string markerAuthoringId)
        {
            AnimationSyncMarker marker = FindMarker(markerAuthoringId);
            return marker ?? throw new KeyNotFoundException($"Animation marker '{markerAuthoringId}' was not found.");
        }

        AnimationSyncMarker FindMarker(string markerAuthoringId)
        {
            for (int i = 0; i < m_SyncMarkers.Count; i++)
            {
                AnimationSyncMarker marker = m_SyncMarkers[i];
                if (marker != null && string.Equals(marker.AuthoringId, markerAuthoringId, StringComparison.Ordinal))
                    return marker;
            }
            return null;
        }

        static void RequireMarkerId(string markerId)
        {
            if (string.IsNullOrEmpty(markerId) || !string.Equals(markerId, markerId.Trim(), StringComparison.Ordinal))
                throw new ArgumentException("MarkerId must be non-empty and cannot contain leading or trailing whitespace.", nameof(markerId));
        }

        void SortMarkers()
        {
            m_SyncMarkers.Sort((left, right) =>
            {
                if (ReferenceEquals(left, right))
                    return 0;
                if (left == null)
                    return 1;
                if (right == null)
                    return -1;
                int frame = left.Frame.CompareTo(right.Frame);
                return frame != 0 ? frame : string.CompareOrdinal(left.AuthoringId, right.AuthoringId);
            });
        }
#endif
    }

    public readonly struct AnimationMarkerSyncCallSite
    {
        public AnimationMarkerSyncCallSite(string authoringIdentity, TimelinePlaybackMode playbackMode)
        {
            AuthoringIdentity = authoringIdentity ?? string.Empty;
            PlaybackMode = playbackMode;
        }

        public string AuthoringIdentity { get; }
        public TimelinePlaybackMode PlaybackMode { get; }
    }

    public readonly struct AnimationMarkerSyncAuthoringInput
    {
        public AnimationMarkerSyncAuthoringInput(
            string producerIdentity,
            TimelineData timeline,
            AnimationTrack track,
            IReadOnlyList<AnimationMarkerSyncCallSite> callSites)
        {
            ProducerIdentity = producerIdentity ?? string.Empty;
            Timeline = timeline;
            Track = track;
            CallSites = callSites ?? Array.Empty<AnimationMarkerSyncCallSite>();
        }

        public string ProducerIdentity { get; }
        public TimelineData Timeline { get; }
        public AnimationTrack Track { get; }
        public IReadOnlyList<AnimationMarkerSyncCallSite> CallSites { get; }
    }

    public readonly struct AnimationMarkerSyncAuthoringIssue
    {
        public AnimationMarkerSyncAuthoringIssue(
            string code,
            string message,
            string producerIdentity,
            string authoringPath,
            string relatedIdentity = "")
        {
            Code = code ?? string.Empty;
            Message = message ?? string.Empty;
            ProducerIdentity = producerIdentity ?? string.Empty;
            AuthoringPath = authoringPath ?? string.Empty;
            RelatedIdentity = relatedIdentity ?? string.Empty;
        }

        public string Code { get; }
        public string Message { get; }
        public string ProducerIdentity { get; }
        public string AuthoringPath { get; }
        public string RelatedIdentity { get; }
    }

    public static class AnimationMarkerSyncIssueCodes
    {
        public const string UnspecifiedMode = "animation_marker_sync_unspecified_mode";
        public const string NoneResidue = "animation_marker_sync_none_residue";
        public const string MissingGroup = "animation_marker_sync_missing_group";
        public const string MissingTopology = "animation_marker_sync_missing_topology";
        public const string MissingRole = "animation_marker_sync_missing_role";
        public const string MarkerCount = "animation_marker_sync_marker_count";
        public const string MarkerId = "animation_marker_sync_marker_id";
        public const string MarkerAuthoringId = "animation_marker_sync_marker_authoring_id";
        public const string MarkerOrder = "animation_marker_sync_marker_order";
        public const string Duration = "animation_marker_sync_duration";
        public const string MarkerBounds = "animation_marker_sync_marker_bounds";
        public const string PlaybackMode = "animation_marker_sync_playback_mode";
        public const string OutputCoverage = "animation_marker_sync_output_coverage";
        public const string GroupPairMismatch = "animation_marker_sync_group_pair_mismatch";
    }

    public static class AnimationMarkerSyncAuthoring
    {
        public static string NormalizeId(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        public static bool Validate(
            IReadOnlyList<AnimationMarkerSyncAuthoringInput> inputs,
            List<AnimationMarkerSyncAuthoringIssue> issues)
        {
            if (inputs == null)
                throw new ArgumentNullException(nameof(inputs));
            if (issues == null)
                throw new ArgumentNullException(nameof(issues));

            int issueStart = issues.Count;
            var pairSets = new Dictionary<string, PairSet>(StringComparer.Ordinal);
            for (int i = 0; i < inputs.Count; i++)
            {
                AnimationMarkerSyncAuthoringInput input = inputs[i];
                if (!ValidateTrack(input, issues, out HashSet<string> pairs))
                    continue;
                if (input.Track.SyncMode != AnimationSyncMode.MarkerGroup)
                    continue;
                string key = $"{input.Track.AnimationChannelId}\n{NormalizeId(input.Track.SyncGroupId)}";
                if (!pairSets.TryGetValue(key, out PairSet expected))
                {
                    pairSets.Add(key, new PairSet(input.ProducerIdentity, pairs));
                    continue;
                }
                if (expected.Pairs.SetEquals(pairs))
                    continue;
                AddIssue(
                    issues,
                    AnimationMarkerSyncIssueCodes.GroupPairMismatch,
                    $"Animation producer '{input.ProducerIdentity}' does not share the directed marker pair set of '{expected.ProducerIdentity}'.",
                    input,
                    "markers",
                    expected.ProducerIdentity);
            }
            return issues.Count == issueStart;
        }

        public static bool ValidateTrack(
            AnimationMarkerSyncAuthoringInput input,
            List<AnimationMarkerSyncAuthoringIssue> issues)
        {
            return ValidateTrack(input, issues, out _);
        }

        static bool ValidateTrack(
            AnimationMarkerSyncAuthoringInput input,
            List<AnimationMarkerSyncAuthoringIssue> issues,
            out HashSet<string> directedPairs)
        {
            directedPairs = new HashSet<string>(StringComparer.Ordinal);
            int issueStart = issues.Count;
            AnimationTrack track = input.Track;
            TimelineData timeline = input.Timeline;
            if (track == null || timeline == null)
            {
                AddIssue(issues, AnimationMarkerSyncIssueCodes.Duration, "Animation marker sync requires a Timeline and AnimationTrack.", input, string.Empty);
                return false;
            }

            if (track.SyncMode == AnimationSyncMode.Unspecified)
            {
                AddIssue(issues, AnimationMarkerSyncIssueCodes.UnspecifiedMode, "AnimationTrack sync mode is Unspecified.", input, "m_SyncMode");
                return false;
            }
            if (track.SyncMode == AnimationSyncMode.None)
            {
                if (!string.IsNullOrEmpty(track.SyncGroupId) ||
                    track.SequenceTopology != AnimationMarkerSequenceTopology.Unspecified ||
                    track.SyncRole != AnimationMarkerSyncRole.Unspecified ||
                    track.SyncMarkers.Count != 0)
                    AddIssue(issues, AnimationMarkerSyncIssueCodes.NoneResidue, "AnimationTrack in None mode retains marker sync data.", input, "m_SyncMode");
                return issues.Count == issueStart;
            }
            if (track.SyncMode != AnimationSyncMode.MarkerGroup)
            {
                AddIssue(issues, AnimationMarkerSyncIssueCodes.UnspecifiedMode, $"Unknown AnimationTrack sync mode '{track.SyncMode}'.", input, "m_SyncMode");
                return false;
            }

            string groupId = NormalizeId(track.SyncGroupId);
            if (string.IsNullOrEmpty(groupId))
                AddIssue(issues, AnimationMarkerSyncIssueCodes.MissingGroup, "MarkerGroup requires a canonical SyncGroupId.", input, "m_SyncGroupId");
            if (track.SequenceTopology != AnimationMarkerSequenceTopology.Finite &&
                track.SequenceTopology != AnimationMarkerSequenceTopology.Cyclic)
                AddIssue(issues, AnimationMarkerSyncIssueCodes.MissingTopology, "MarkerGroup requires Finite or Cyclic topology.", input, "m_SequenceTopology");
            if (track.SyncRole != AnimationMarkerSyncRole.CanBeLeader &&
                track.SyncRole != AnimationMarkerSyncRole.AlwaysLeader &&
                track.SyncRole != AnimationMarkerSyncRole.AlwaysFollower)
                AddIssue(issues, AnimationMarkerSyncIssueCodes.MissingRole, "MarkerGroup requires CanBeLeader, AlwaysLeader, or AlwaysFollower role.", input, "m_SyncRole");
            if (track.SyncMarkers.Count < 2)
                AddIssue(issues, AnimationMarkerSyncIssueCodes.MarkerCount, "MarkerGroup requires at least two markers.", input, "m_SyncMarkers");
            if (timeline.MaxFrame <= 0 || !float.IsFinite(timeline.Duration) || timeline.Duration <= 0f)
                AddIssue(issues, AnimationMarkerSyncIssueCodes.Duration, "MarkerGroup requires a finite positive Timeline duration.", input, "duration");

            var authoringIds = new HashSet<string>(StringComparer.Ordinal);
            int previousFrame = -1;
            for (int i = 0; i < track.SyncMarkers.Count; i++)
            {
                AnimationSyncMarker marker = track.SyncMarkers[i];
                if (marker == null)
                {
                    AddIssue(issues, AnimationMarkerSyncIssueCodes.MarkerAuthoringId, $"Marker #{i} is null.", input, $"m_SyncMarkers.Array.data[{i}]");
                    continue;
                }
                if (!AuthoringIdentity.IsValid(marker.AuthoringId) || !authoringIds.Add(marker.AuthoringId))
                    AddIssue(issues, AnimationMarkerSyncIssueCodes.MarkerAuthoringId, $"Marker #{i} has a missing or duplicate AuthoringId.", input, $"m_SyncMarkers.Array.data[{i}].m_AuthoringId", marker.AuthoringId);
                if (string.IsNullOrEmpty(marker.MarkerId) || !string.Equals(marker.MarkerId, marker.MarkerId.Trim(), StringComparison.Ordinal))
                    AddIssue(issues, AnimationMarkerSyncIssueCodes.MarkerId, $"Marker #{i} has an invalid MarkerId.", input, $"m_SyncMarkers.Array.data[{i}].m_MarkerId", marker.AuthoringId);
                if (marker.Frame <= previousFrame)
                    AddIssue(issues, AnimationMarkerSyncIssueCodes.MarkerOrder, $"Marker #{i} must have a unique frame greater than the previous marker.", input, $"m_SyncMarkers.Array.data[{i}].m_Frame", marker.AuthoringId);
                previousFrame = marker.Frame;
            }

            ValidateBounds(input, issues);
            ValidateCallSites(input, issues);
            ValidateOutputCoverage(input, issues);
            BuildDirectedPairs(track, directedPairs);
            return issues.Count == issueStart;
        }

        static void ValidateBounds(AnimationMarkerSyncAuthoringInput input, List<AnimationMarkerSyncAuthoringIssue> issues)
        {
            AnimationTrack track = input.Track;
            if (track.SyncMarkers.Count == 0)
                return;
            int durationFrame = input.Timeline.MaxFrame;
            if (track.SequenceTopology == AnimationMarkerSequenceTopology.Cyclic)
            {
                for (int i = 0; i < track.SyncMarkers.Count; i++)
                {
                    AnimationSyncMarker marker = track.SyncMarkers[i];
                    if (marker != null && (marker.Frame < 0 || marker.Frame >= durationFrame))
                        AddIssue(issues, AnimationMarkerSyncIssueCodes.MarkerBounds, $"Cyclic marker frame {marker.Frame} must be in [0, {durationFrame}).", input, $"m_SyncMarkers.Array.data[{i}].m_Frame", marker.AuthoringId);
                }
                return;
            }
            if (track.SequenceTopology != AnimationMarkerSequenceTopology.Finite)
                return;
            AnimationSyncMarker first = track.SyncMarkers[0];
            AnimationSyncMarker last = track.SyncMarkers[track.SyncMarkers.Count - 1];
            if (first == null || first.Frame != 0)
                AddIssue(issues, AnimationMarkerSyncIssueCodes.MarkerBounds, "Finite marker sequence must begin at frame 0.", input, "m_SyncMarkers.Array.data[0].m_Frame", first?.AuthoringId);
            if (last == null || last.Frame != durationFrame)
                AddIssue(issues, AnimationMarkerSyncIssueCodes.MarkerBounds, $"Finite marker sequence must end at duration frame {durationFrame}.", input, $"m_SyncMarkers.Array.data[{track.SyncMarkers.Count - 1}].m_Frame", last?.AuthoringId);
        }

        static void ValidateCallSites(AnimationMarkerSyncAuthoringInput input, List<AnimationMarkerSyncAuthoringIssue> issues)
        {
            TimelinePlaybackMode required = input.Track.SequenceTopology == AnimationMarkerSequenceTopology.Cyclic
                ? TimelinePlaybackMode.Loop
                : TimelinePlaybackMode.Once;
            for (int i = 0; i < input.CallSites.Count; i++)
            {
                AnimationMarkerSyncCallSite callSite = input.CallSites[i];
                if (callSite.PlaybackMode != required)
                    AddIssue(issues, AnimationMarkerSyncIssueCodes.PlaybackMode, $"Call site '{callSite.AuthoringIdentity}' uses {callSite.PlaybackMode}, but {input.Track.SequenceTopology} requires {required}.", input, "callSites", callSite.AuthoringIdentity);
            }
        }

        static void ValidateOutputCoverage(AnimationMarkerSyncAuthoringInput input, List<AnimationMarkerSyncAuthoringIssue> issues)
        {
            AnimationTrack track = input.Track;
            int durationFrame = input.Timeline.MaxFrame;
            var ranges = new List<FrameRange>();
            for (int i = 0; i < track.Clips.Count; i++)
            {
                if (track.Clips[i] is not AnimationClip clip || !clip.Clip || clip.EndFrame <= clip.StartFrame)
                    continue;
                int end = clip.ExtraPolationMode == ExtraPolationMode.Hold ? durationFrame : clip.EndFrame;
                ranges.Add(new FrameRange(Mathf.Max(0, clip.StartFrame), Mathf.Min(durationFrame, end)));
            }
            ranges.Sort((left, right) => left.Start != right.Start ? left.Start.CompareTo(right.Start) : left.End.CompareTo(right.End));
            int coveredUntil = 0;
            for (int i = 0; i < ranges.Count; i++)
            {
                FrameRange range = ranges[i];
                if (range.Start > coveredUntil)
                    break;
                coveredUntil = Mathf.Max(coveredUntil, range.End);
                if (coveredUntil >= durationFrame)
                    return;
            }
            AddIssue(issues, AnimationMarkerSyncIssueCodes.OutputCoverage, $"AnimationTrack output covers frames [0, {coveredUntil}] but marker sync requires [0, {durationFrame}].", input, "m_Clips");
        }

        static void BuildDirectedPairs(AnimationTrack track, HashSet<string> pairs)
        {
            for (int i = 1; i < track.SyncMarkers.Count; i++)
            {
                AnimationSyncMarker previous = track.SyncMarkers[i - 1];
                AnimationSyncMarker next = track.SyncMarkers[i];
                if (previous != null && next != null)
                    pairs.Add(PairKey(previous.MarkerId, next.MarkerId));
            }
            if (track.SequenceTopology != AnimationMarkerSequenceTopology.Cyclic || track.SyncMarkers.Count < 2)
                return;
            AnimationSyncMarker last = track.SyncMarkers[track.SyncMarkers.Count - 1];
            AnimationSyncMarker first = track.SyncMarkers[0];
            if (last != null && first != null)
                pairs.Add(PairKey(last.MarkerId, first.MarkerId));
        }

        public static string PairKey(string previousMarkerId, string nextMarkerId)
        {
            return $"{NormalizeId(previousMarkerId)}\u001f{NormalizeId(nextMarkerId)}";
        }

        static void AddIssue(
            List<AnimationMarkerSyncAuthoringIssue> issues,
            string code,
            string message,
            AnimationMarkerSyncAuthoringInput input,
            string relativePath,
            string relatedIdentity = "")
        {
            string path = $"timeline:{input.Timeline?.AuthoringId}/track:{input.Track?.AuthoringId}";
            if (!string.IsNullOrEmpty(relativePath))
                path = $"{path}/{relativePath}";
            issues.Add(new AnimationMarkerSyncAuthoringIssue(code, message, input.ProducerIdentity, path, relatedIdentity));
        }

        readonly struct FrameRange
        {
            public FrameRange(int start, int end)
            {
                Start = start;
                End = end;
            }

            public int Start { get; }
            public int End { get; }
        }

        sealed class PairSet
        {
            public PairSet(string producerIdentity, HashSet<string> pairs)
            {
                ProducerIdentity = producerIdentity;
                Pairs = pairs;
            }

            public string ProducerIdentity { get; }
            public HashSet<string> Pairs { get; }
        }
    }
}
