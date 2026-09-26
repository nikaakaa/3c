using System;
using System.Collections.Generic;
using BTSMTL.Diagnostics;
using BTSMTL.Diagnostics.Editor;
using ThirdPersonSimulation.Fixed;

namespace BTSMTL.Timeline.Editor
{
    public sealed class RuntimeTimelinePlaybackProjection
    {
        readonly Dictionary<string, Track> m_RuntimeTracks = new Dictionary<string, Track>(StringComparer.Ordinal);
        readonly HashSet<string> m_RuntimeClips = new HashSet<string>(StringComparer.Ordinal);
        TimelineData m_Source;
        TimelineData m_SourceSnapshot;
        TimelineData m_Runtime;
        RuntimeInstanceKey m_Playback;
        RuntimeDebugViewModel m_Observation;
        ulong m_LastEventSequence;

        public bool StructureChanged { get; private set; }

        public bool Matches(TimelineData source, RuntimeInstanceKey playback) =>
            ReferenceEquals(m_Source, source) && m_Playback.Equals(playback);

        public TimelineData Update(
            TimelineData source,
            RuntimeInstanceKey playback,
            IReadOnlyList<RuntimeDebugEventView> events,
            RuntimeDebugViewModel observation,
            RuntimeTimelinePlaybackDebugSummary summary)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            ulong latestSequence = 0;
            for (int index = 0; index < events.Count; index++)
                latestSequence = Math.Max(latestSequence, events[index].Event.Sequence);
            bool reset = !Matches(source, playback) || !ReferenceEquals(m_Observation, observation) ||
                         latestSequence < m_LastEventSequence;
            if (reset)
            {
                Reset(source, playback);
                m_Observation = observation;
            }

            m_LastEventSequence = latestSequence;
            bool changed = false;
            for (int index = 0; index < events.Count; index++)
            {
                RuntimeSourceElementKey sourceKey = events[index].Source;
                if (sourceKey.Kind == RuntimeSourceElementKind.Track)
                {
                    bool exists = m_RuntimeTracks.ContainsKey(sourceKey.TrackAuthoringId);
                    changed |= !exists && EnsureTrack(sourceKey.TrackAuthoringId) != null;
                }
                else if (sourceKey.Kind is RuntimeSourceElementKind.Clip or RuntimeSourceElementKind.TreeClip)
                    changed |= EnsureClip(sourceKey.TrackAuthoringId, sourceKey.ClipAuthoringId);
            }
            if (changed)
                m_Runtime.Init();
            StructureChanged = reset || changed;
            if (UpdateOpenClipEnds(events, summary))
                m_Runtime.Init();
            return m_Runtime;
        }

        bool UpdateOpenClipEnds(IReadOnlyList<RuntimeDebugEventView> events, RuntimeTimelinePlaybackDebugSummary summary)
        {
            bool changed = false;
            foreach (Track track in m_RuntimeTracks.Values)
            {
                for (int clipIndex = 0; clipIndex < track.Clips.Count; clipIndex++)
                {
                    if (track.Clips[clipIndex] is not TreeClip clip ||
                        clip.ClipExitSource != TimelineClipExitSource.TreeDecision)
                        continue;
                    bool presentation = clip.ExecutionDomain == TimelineExecutionDomain.Presentation;
                    RuntimeTraceDomain domain = presentation ? RuntimeTraceDomain.Presentation : RuntimeTraceDomain.Logic;
                    int cycle = presentation ? summary.VisualCycle : summary.LogicCycle;
                    float end = presentation ? summary.VisualTime : summary.LogicTime;
                    ulong latestSequence = 0;
                    for (int eventIndex = 0; eventIndex < events.Count; eventIndex++)
                    {
                        RuntimeDebugEventView item = events[eventIndex];
                        if (item.Event.Domain != domain || item.Event.Payload.Cycle != cycle ||
                            !string.Equals(item.Source.ClipAuthoringId, clip.AuthoringId, StringComparison.Ordinal) ||
                            item.Event.Sequence <= latestSequence ||
                            item.Event.Kind is not (RuntimeTraceEventKind.TreeClipEntered or RuntimeTraceEventKind.TreeClipUpdated or
                                RuntimeTraceEventKind.TreeClipExited or RuntimeTraceEventKind.TreeClipDestroyed))
                            continue;
                        latestSequence = item.Event.Sequence;
                        end = item.Event.Kind is RuntimeTraceEventKind.TreeClipExited or RuntimeTraceEventKind.TreeClipDestroyed
                            ? item.Event.Payload.Time : presentation ? summary.VisualTime : summary.LogicTime;
                    }
                    FixedScalar endTime = FixedScalar.FromDouble(Math.Max(0d, end));
                    endTime = endTime < clip.StartTime ? clip.StartTime : endTime;
                    if (clip.EndTime == endTime)
                        continue;
                    clip.ConfigureTimeRange(clip.StartTime, endTime);
                    changed = true;
                }
            }
            return changed;
        }

        void Reset(TimelineData source, RuntimeInstanceKey playback)
        {
            m_Source = source;
            m_SourceSnapshot = source.Clone();
            m_SourceSnapshot.Init();
            m_Playback = playback;
            m_Runtime = TimelineData.CreateDefault($"{m_SourceSnapshot.Name} [Runtime]");
            m_Runtime.ConfigureAuthoringIdentity(m_SourceSnapshot.AuthoringId);
            m_Runtime.Loop = m_SourceSnapshot.Loop;
            m_RuntimeTracks.Clear();
            m_RuntimeClips.Clear();
            m_Runtime.Init();
        }

        bool EnsureClip(string trackAuthoringId, string clipAuthoringId)
        {
            if (string.IsNullOrEmpty(clipAuthoringId) || m_RuntimeClips.Contains(clipAuthoringId))
                return false;
            if (!TryFindSourceClip(trackAuthoringId, clipAuthoringId, out Track sourceTrack, out Clip sourceClip))
                return false;
            Track runtimeTrack = EnsureTrack(sourceTrack.AuthoringId);
            if (runtimeTrack == null)
                return false;
            Clip runtimeClip = ManagedReferenceCloneUtility.Clone(sourceClip);
            if (runtimeClip is TreeClip treeClip &&
                treeClip.ClipExitSource == TimelineClipExitSource.TreeDecision)
            {
                runtimeClip.ConfigureTimeRange(runtimeClip.StartTime, m_SourceSnapshot.DurationTime);
            }
            int sourceIndex = sourceTrack.Clips.IndexOf(sourceClip);
            int insertIndex = runtimeTrack.Clips.Count;
            for (int index = 0; index < runtimeTrack.Clips.Count; index++)
            {
                if (!TryFindSourceClip(sourceTrack.AuthoringId, runtimeTrack.Clips[index].AuthoringId, out _, out Clip existing))
                    continue;
                if (sourceTrack.Clips.IndexOf(existing) > sourceIndex)
                {
                    insertIndex = index;
                    break;
                }
            }
            runtimeTrack.Clips.Insert(insertIndex, runtimeClip);
            m_RuntimeClips.Add(clipAuthoringId);
            return true;
        }

        Track EnsureTrack(string trackAuthoringId)
        {
            if (string.IsNullOrEmpty(trackAuthoringId))
                return null;
            if (m_RuntimeTracks.TryGetValue(trackAuthoringId, out Track runtimeTrack))
                return runtimeTrack;
            Track sourceTrack = FindSourceTrack(trackAuthoringId);
            if (sourceTrack == null)
                return null;
            runtimeTrack = ManagedReferenceCloneUtility.Clone(sourceTrack);
            runtimeTrack.Clips.Clear();
            int sourceIndex = m_SourceSnapshot.Tracks.IndexOf(sourceTrack);
            int insertIndex = m_Runtime.Tracks.Count;
            for (int index = 0; index < m_Runtime.Tracks.Count; index++)
            {
                Track existing = m_Runtime.Tracks[index];
                Track existingSource = FindSourceTrack(existing.AuthoringId);
                if (existingSource != null && m_SourceSnapshot.Tracks.IndexOf(existingSource) > sourceIndex)
                {
                    insertIndex = index;
                    break;
                }
            }
            m_Runtime.Tracks.Insert(insertIndex, runtimeTrack);
            m_RuntimeTracks.Add(trackAuthoringId, runtimeTrack);
            return runtimeTrack;
        }

        Track FindSourceTrack(string trackAuthoringId)
        {
            for (int index = 0; index < m_SourceSnapshot.Tracks.Count; index++)
            {
                Track track = m_SourceSnapshot.Tracks[index];
                if (track != null && string.Equals(track.AuthoringId, trackAuthoringId, StringComparison.Ordinal))
                    return track;
            }
            return null;
        }

        bool TryFindSourceClip(
            string trackAuthoringId,
            string clipAuthoringId,
            out Track sourceTrack,
            out Clip sourceClip)
        {
            sourceTrack = FindSourceTrack(trackAuthoringId);
            if (sourceTrack != null)
            {
                for (int index = 0; index < sourceTrack.Clips.Count; index++)
                {
                    Clip clip = sourceTrack.Clips[index];
                    if (clip != null && string.Equals(clip.AuthoringId, clipAuthoringId, StringComparison.Ordinal))
                    {
                        sourceClip = clip;
                        return true;
                    }
                }
            }
            for (int trackIndex = 0; trackIndex < m_SourceSnapshot.Tracks.Count; trackIndex++)
            {
                Track track = m_SourceSnapshot.Tracks[trackIndex];
                if (track == null)
                    continue;
                for (int clipIndex = 0; clipIndex < track.Clips.Count; clipIndex++)
                {
                    Clip clip = track.Clips[clipIndex];
                    if (clip != null && string.Equals(clip.AuthoringId, clipAuthoringId, StringComparison.Ordinal))
                    {
                        sourceTrack = track;
                        sourceClip = clip;
                        return true;
                    }
                }
            }
            sourceClip = null;
            return false;
        }
    }
}
