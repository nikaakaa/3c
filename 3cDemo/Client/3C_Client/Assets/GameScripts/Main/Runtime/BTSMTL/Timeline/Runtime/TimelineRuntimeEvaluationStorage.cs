using System;
using ThirdPersonSimulation.Fixed;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ThirdPersonSimulation;
using UnityEngine;

namespace BTSMTL.Timeline.Runtime
{
    sealed class TimelineRuntimeEvaluationStoragePool
    {
        readonly Dictionary<(string ContentRevision, TimelinePlaybackMode Mode), Stack<TimelineRuntimeEvaluationStorage>> m_Recycled = new();

        public void Prepare(TimelineRuntimePreparedContent content, TimelinePlaybackMode mode)
        {
            var key = (content.ContentRevision, mode);
            if (m_Recycled.ContainsKey(key))
                return;
            var recycled = new Stack<TimelineRuntimeEvaluationStorage>(2);
            recycled.Push(new TimelineRuntimeEvaluationStorage(content.SourceTimeline, content.Content, mode, recycled));
            recycled.Push(new TimelineRuntimeEvaluationStorage(content.SourceTimeline, content.Content, mode, recycled));
            m_Recycled.Add(key, recycled);
        }

        public TimelineRuntimeEvaluationStorage Rent(TimelineRuntimePlayback playback)
        {
            var key = (playback.ContentRevision, playback.PlaybackMode);
            Stack<TimelineRuntimeEvaluationStorage> recycled = m_Recycled[key];
            return recycled.Count == 0
                ? new TimelineRuntimeEvaluationStorage(playback.SourceTimeline, playback.Content, playback.PlaybackMode, recycled) : recycled.Pop();
        }

        public void Clear() => m_Recycled.Clear();
    }

    sealed class TimelineRuntimeEvaluationStorage
    {
        readonly Stack<TimelineRuntimeEvaluationStorage> m_Recycled;

        public TimelineRuntimeEvaluationStorage(
            TimelineData timeline,
            TimelineContentUnit content,
            TimelinePlaybackMode mode,
            Stack<TimelineRuntimeEvaluationStorage> recycled)
        {
            m_Recycled = recycled;
            int animations = 0, motions = 0, cameraStates = 0;
            int cameraResponses = 0, cameraResources = 0, treeClips = 0;
            int markers = 0, scenePresentation = 0, motionWarps = 0, clips = 0;
            for (int index = 0; index < timeline.Tracks.Count; index++)
            {
                Track track = timeline.Tracks[index];
                if (track == null || track.PersistentMuted || track.ExecutionDomain != TimelineExecutionDomain.Logic)
                    continue;
                int count = track.Clips.Count;
                clips = checked(clips + count);
                switch (track)
                {
                    case AnimationTrack: animations = checked(animations + count); break;
                    case MotionCurveTrack: motions = checked(motions + count); break;
                    case CameraStateTrack: cameraStates = checked(cameraStates + count); break;
                    case CameraResponseTrack: cameraResponses = checked(cameraResponses + count); break;
                    case CameraEffectTrack: cameraResources = checked(cameraResources + count); break;
                    case TreeTrack: treeClips = checked(treeClips + count); break;
                    case ScenePresentationParameterTrack: scenePresentation = checked(scenePresentation + count); break;
                    case MotionWarpTrack: motionWarps = checked(motionWarps + count); break;
                }
            }
            for (int index = 0; index < content.Markers.Count; index++)
            {
                TimelineContentMarker marker = content.Markers[index];
                if (!marker.TrackMuted && marker.ExecutionPolicy.IsLogic)
                    markers++;
            }
            int traversals = mode == TimelinePlaybackMode.Loop
                ? TimelineRuntimeEvaluationSegments.MaximumCycleAdvance + 1 : 1;
            int lifecycleSamples = checked(2 * traversals + 1);
            ActiveClipIds = new(content.Clips.Count);
            Boundaries = new(checked(2 * clips * traversals));
            AnimationContributions = new(checked(animations * traversals));
            MotionContributions = new(checked(motions * traversals));
            CameraStates = new(checked(cameraStates));
            CameraResponses = new(checked(cameraResponses));
            CameraResources = new(checked(cameraResources));
            TreeClips = new(checked(treeClips * lifecycleSamples));
            Markers = new(checked(markers * traversals));
            ScenePresentation = new(checked(scenePresentation));
            MotionWarps = new(checked(motionWarps * traversals));
            ClipSamples = new(checked(clips * lifecycleSamples));
            Traces = new(checked(2 * clips * traversals + treeClips));
        }

        public readonly TimelineRuntimeSampleBuffer<string> ActiveClipIds;
        public readonly TimelineRuntimeSampleBuffer<TimelineRuntimeClipBoundary> Boundaries;
        public readonly TimelineRuntimeSampleBuffer<TimelineAnimationContribution> AnimationContributions;
        public readonly TimelineRuntimeSampleBuffer<TimelineMotionCurveContribution> MotionContributions;
        public readonly TimelineRuntimeSampleBuffer<TimelineCameraStateSample> CameraStates;
        public readonly TimelineRuntimeSampleBuffer<TimelineCameraResponseSample> CameraResponses;
        public readonly TimelineRuntimeSampleBuffer<TimelineCameraResourceSample> CameraResources;
        public readonly TimelineRuntimeSampleBuffer<TimelineRuntimeTreeClipRequest> TreeClips;
        public readonly TimelineRuntimeSampleBuffer<TimelineRuntimeMarkerRequest> Markers;
        public readonly TimelineRuntimeSampleBuffer<TimelineRuntimeScenePresentationSample> ScenePresentation;
        public readonly TimelineRuntimeSampleBuffer<TimelineRuntimeMotionWarpRequest> MotionWarps;
        public readonly TimelineRuntimeSampleBuffer<TimelineRuntimeClipSample> ClipSamples;
        public readonly TimelineRuntimeSampleBuffer<TimelineRuntimeTraceOutput> Traces;

        public void Recycle() => m_Recycled.Push(this);

        public void Clear()
        {
            ActiveClipIds.Clear();
            Boundaries.Clear();
            AnimationContributions.Clear();
            MotionContributions.Clear();
            CameraStates.Clear();
            CameraResponses.Clear();
            CameraResources.Clear();
            TreeClips.Clear();
            Markers.Clear();
            ScenePresentation.Clear();
            MotionWarps.Clear();
            ClipSamples.Clear();
            Traces.Clear();
        }
    }
}
