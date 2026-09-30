using System;
using ThirdPersonSimulation.Fixed;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ThirdPersonSimulation;
using UnityEngine;

namespace BTSMTL.Timeline.Runtime
{
    internal static class TimelineRuntimeEvaluator
    {
        public static TimelineRuntimeEvaluationResult Evaluate(
            TimelineRuntimeEvaluationStorage storage,
            TimelineData timeline,
            TimelineRuntimeMotionSampling motionSampling,
            TimelineContentUnit content,
            FixedScalar previousPosition,
            int previousCycle,
            FixedScalar currentPosition,
            int currentCycle,
            bool loop,
            IReadOnlyList<TimelineRuntimeClipBoundary> boundaries,
            TimelineExecutionIdentity executionIdentity,
            ulong generation,
            ulong logicTick,
            bool includeStartBoundary,
            IReadOnlyList<string> exitedTreeDecisionClips)
        {
            if (timeline == null)
                throw new ArgumentNullException(nameof(timeline));
            if (content == null)
                throw new ArgumentNullException(nameof(content));
            TimelineRuntimeSampleBuffer<TimelineAnimationContribution> animations = storage.AnimationContributions;
            TimelineRuntimeSampleBuffer<TimelineMotionCurveContribution> motions = storage.MotionContributions;
            TimelineRuntimeSampleBuffer<TimelineCameraStateSample> cameraStates = storage.CameraStates;
            TimelineRuntimeSampleBuffer<TimelineCameraResponseSample> cameraResponses = storage.CameraResponses;
            TimelineRuntimeSampleBuffer<TimelineCameraResourceSample> cameraResources = storage.CameraResources;
            TimelineRuntimeSampleBuffer<TimelineRuntimeTreeClipRequest> treeClips = storage.TreeClips;
            TimelineRuntimeSampleBuffer<TimelineRuntimeMarkerRequest> markers = storage.Markers;
            TimelineRuntimeSampleBuffer<TimelineRuntimeScenePresentationSample> scenePresentation = storage.ScenePresentation;
            TimelineRuntimeSampleBuffer<TimelineRuntimeMotionWarpRequest> motionWarps = storage.MotionWarps;
            TimelineRuntimeSampleBuffer<TimelineRuntimeClipSample> clipSamples = storage.ClipSamples;
            TimelineRuntimeSampleBuffer<TimelineRuntimeTraceOutput> traces = storage.Traces;
            var segments = new TimelineRuntimeEvaluationSegments(
                previousPosition,
                previousCycle,
                currentPosition,
                currentCycle,
                content.Duration,
                loop);
            AppendMarkerRequests(
                content,
                generation,
                previousPosition,
                previousCycle,
                currentPosition,
                currentCycle,
                loop,
                includeStartBoundary,
                markers);
            for (int trackIndex = 0; trackIndex < timeline.Tracks.Count; trackIndex++)
            {
                if (timeline.Tracks[trackIndex] is MotionCurveTrack motionTrack &&
                    !motionTrack.PersistentMuted && motionTrack.ExecutionDomain == TimelineExecutionDomain.Logic)
                    motionSampling.Sample(motionTrack, segments, timeline.AuthoringId, timeline.Name, motions);
            }
            for (int segmentIndex = 0; segmentIndex < segments.Count; segmentIndex++)
            {
                TimelineRuntimeEvaluationSegment segment = segments[segmentIndex];
                for (int trackIndex = 0; trackIndex < timeline.Tracks.Count; trackIndex++)
                {
                    Track track = timeline.Tracks[trackIndex];
                    if (track == null || track.PersistentMuted || track.ExecutionDomain != TimelineExecutionDomain.Logic)
                        continue;
                    if (track is AnimationTrack animationTrack)
                    {
                        animationTrack.Sample(
                            segment.PreviousTime,
                            segment.CurrentTime,
                            trackIndex,
                            timeline.AuthoringId,
                            timeline.Name,
                            animations,
                            loop,
                            segment.Cycle);
                    }
                    else if (track is MotionWarpTrack motionWarpTrack && !track.PersistentMuted)
                    {
                        for (int clipIndex = 0; clipIndex < motionWarpTrack.Clips.Count; clipIndex++)
                        {
                            if (motionWarpTrack.Clips[clipIndex] is not MotionWarpClip motionWarpClip ||
                                motionWarpClip.ExecutionDomain != TimelineExecutionDomain.Logic ||
                                segment.CurrentTime <= motionWarpClip.StartTime ||
                                segment.PreviousTime >= motionWarpClip.EndTime)
                                continue;
                            motionWarps.Add(motionSampling.SampleWarp(
                                motionWarpClip.AuthoringId, segment.PreviousTime, segment.CurrentTime, segment.Cycle));
                        }
                    }
                }
            }
            FixedScalar currentTime = currentPosition;
            for (int boundaryIndex = 0; boundaryIndex < (boundaries?.Count ?? 0); boundaryIndex++)
            {
                TimelineRuntimeClipBoundary boundary = boundaries[boundaryIndex];
                if (!TryResolveClip(timeline, boundary.AuthoringId, out Clip boundaryClip) ||
                    boundaryClip.ExecutionDomain != TimelineExecutionDomain.Logic)
                {
                    continue;
                }
                clipSamples.Add(new TimelineRuntimeClipSample(
                    boundary.AuthoringId,
                    boundary.TrackAuthoringId,
                    ResolveContractKind(timeline, boundary.AuthoringId),
                    boundary.Kind == TimelineRuntimeClipBoundaryKind.Enter
                        ? TimelineRuntimeTreeClipEventKind.Enter
                        : TimelineRuntimeTreeClipEventKind.Exit,
                    boundary.Time,
                    boundary.Cycle,
                    boundary.Kind == TimelineRuntimeClipBoundaryKind.Enter ? 0f : 1f));
                traces.Add(new TimelineRuntimeTraceOutput(
                    timeline.AuthoringId,
                    boundary.TrackAuthoringId,
                    boundary.AuthoringId,
                    boundary.Kind == TimelineRuntimeClipBoundaryKind.Enter
                        ? "timeline.clip.enter"
                        : "timeline.clip.exit",
                    TimelineTraceSeverity.Detail,
                    boundary.Kind == TimelineRuntimeClipBoundaryKind.Enter
                        ? "Clip boundary Enter." : "Clip boundary Exit.",
                    executionIdentity,
                    generation,
                    logicTick,
                    boundary.Time,
                    boundary.Cycle));
                if (!TryResolveTreeClip(
                        timeline,
                        boundary.AuthoringId,
                        out TreeClip treeClip))
                    continue;
                if (!TimelineClipExecutionPolicy.FromDomain(treeClip.ExecutionDomain).IsLogic)
                    continue;
                if (!TryGetTreeContract(
                        content,
                        treeClip,
                        out string treeGraphId,
                        out string treeGraphRevision))
                    continue;
                treeClips.Add(new TimelineRuntimeTreeClipRequest(
                    treeClip.AuthoringId,
                    treeClip.Track.AuthoringId,
                    treeGraphId,
                    treeGraphRevision,
                    treeClip.ExecutionPhase,
                    treeClip.ClipExitSource,
                    boundary.Kind == TimelineRuntimeClipBoundaryKind.Enter
                        ? TimelineRuntimeTreeClipEventKind.Enter
                        : TimelineRuntimeTreeClipEventKind.Exit,
                    boundary.Time,
                    boundary.Cycle,
                    boundary.Kind == TimelineRuntimeClipBoundaryKind.Enter ? 0f : 1f,
                    generation,
                    TimelineRuntimeTreeClipRequest.ComposeBranchRevision(generation, boundary.Cycle)));
            }
            for (int trackIndex = 0; trackIndex < timeline.Tracks.Count; trackIndex++)
            {
                if (timeline.Tracks[trackIndex] is not ScenePresentationParameterTrack track ||
                    track.PersistentMuted || track.ExecutionDomain != TimelineExecutionDomain.Logic)
                    continue;
                for (int clipIndex = 0; clipIndex < track.Clips.Count; clipIndex++)
                {
                    if (track.Clips[clipIndex] is not ScenePresentationParameterCurveClip clip ||
                        clip.ExecutionDomain != TimelineExecutionDomain.Logic ||
                        currentTime < clip.StartTime || currentTime > clip.EndTime)
                        continue;
                    float duration = Mathf.Max(0.0001f, clip.DurationTime.ToSingle());
                    float local = Mathf.Clamp01((currentTime - clip.StartTime).ToSingle() / duration);
                    scenePresentation.Add(new TimelineRuntimeScenePresentationSample(
                        clip.AuthoringId,
                        clip.TargetBindingId,
                        clip.ParameterBindingId,
                        clip.ParameterValueKind,
                        clip.ValueCurve.Evaluate(local),
                        local,
                        currentTime,
                        currentCycle,
                        executionIdentity,
                        generation));
                }
            }
            for (int trackIndex = 0; trackIndex < timeline.Tracks.Count; trackIndex++)
            {
                Track track = timeline.Tracks[trackIndex];
                if (track == null || track.PersistentMuted || track.ExecutionDomain != TimelineExecutionDomain.Logic)
                    continue;
                if (track is CameraStateTrack cameraStateTrack)
                    cameraStateTrack.Sample(
                        currentTime,
                        timeline.AuthoringId,
                        timeline.Name,
                        cameraStates);
                else if (track is CameraResponseTrack cameraResponseTrack)
                    cameraResponseTrack.Sample(
                        currentTime,
                        timeline.AuthoringId,
                        timeline.Name,
                        cameraResponses);
                else if (track is CameraEffectTrack cameraEffectTrack)
                    cameraEffectTrack.Sample(
                        currentTime,
                        timeline.AuthoringId,
                        timeline.Name,
                        cameraResources);
            }
            for (int trackIndex = 0; trackIndex < timeline.Tracks.Count; trackIndex++)
            {
                if (timeline.Tracks[trackIndex] is not TreeTrack treeTrack || treeTrack.PersistentMuted)
                    continue;
                for (int clipIndex = 0; clipIndex < treeTrack.Clips.Count; clipIndex++)
                {
                    if (treeTrack.Clips[clipIndex] is not TreeClip treeClip ||
                        treeTrack.PersistentMuted ||
                        treeClip.ExecutionDomain != TimelineExecutionDomain.Logic)
                        continue;
                    bool treeDecisionExit =
                        treeClip.ClipExitSource == TimelineClipExitSource.TreeDecision;
                    if (TimelineRuntimePlayback.ContainsClip(exitedTreeDecisionClips, treeClip.AuthoringId))
                        continue;
                    if (currentTime <= treeClip.StartTime)
                        continue;
                    if (!treeDecisionExit && currentTime >= treeClip.EndTime)
                        continue;
                    if (!TryGetTreeContract(
                            content,
                            treeClip,
                            out string treeGraphId,
                            out string treeGraphRevision))
                        continue;
                    float duration = Mathf.Max(0.0001f, treeClip.DurationTime.ToSingle());
                    float local = Mathf.Clamp01((currentTime - treeClip.StartTime).ToSingle() / duration);
                    treeClips.Add(new TimelineRuntimeTreeClipRequest(
                        treeClip.AuthoringId,
                        treeTrack.AuthoringId,
                        treeGraphId,
                        treeGraphRevision,
                        treeClip.ExecutionPhase,
                        treeClip.ClipExitSource,
                        TimelineRuntimeTreeClipEventKind.Update,
                        currentPosition,
                        currentCycle,
                        local,
                        generation,
                        TimelineRuntimeTreeClipRequest.ComposeBranchRevision(generation, currentCycle)));
                    traces.Add(new TimelineRuntimeTraceOutput(
                        timeline.AuthoringId,
                        treeTrack.AuthoringId,
                        treeClip.AuthoringId,
                        "timeline.treeclip.update",
                        TimelineTraceSeverity.Detail,
                        "TreeClip update candidate.",
                        executionIdentity,
                        generation,
                        logicTick,
                        currentPosition,
                        currentCycle));
                }
            }
            for (int trackIndex = 0; trackIndex < timeline.Tracks.Count; trackIndex++)
            {
                Track track = timeline.Tracks[trackIndex];
                if (track == null || track.PersistentMuted || track.ExecutionDomain != TimelineExecutionDomain.Logic)
                    continue;
                for (int clipIndex = 0; clipIndex < track.Clips.Count; clipIndex++)
                {
                    Clip clip = track.Clips[clipIndex];
                    if (clip == null ||
                        clip.ExecutionDomain != TimelineExecutionDomain.Logic ||
                        currentTime <= clip.StartTime || currentTime >= clip.EndTime)
                        continue;
                    float duration = Mathf.Max(0.0001f, clip.DurationTime.ToSingle());
                    clipSamples.Add(new TimelineRuntimeClipSample(
                        clip.AuthoringId,
                        track.AuthoringId,
                        clip.ContractKind,
                        TimelineRuntimeTreeClipEventKind.Update,
                        currentPosition,
                        currentCycle,
                        (currentTime - clip.StartTime).ToSingle() / duration));
                }
            }
            return new TimelineRuntimeEvaluationResult(storage);
        }

        static void AppendMarkerRequests(
            TimelineContentUnit content,
            ulong generation,
            FixedScalar previousPosition,
            int previousCycle,
            FixedScalar currentPosition,
            int currentCycle,
            bool loop,
            bool includeStartBoundary,
            TimelineRuntimeSampleBuffer<TimelineRuntimeMarkerRequest> markers)
        {
            FixedScalar duration = content.Duration;
            if (duration <= FixedScalar.Zero)
                return;
            FixedScalar previousTime = previousPosition;
            FixedScalar currentTime = currentPosition;
            if (currentCycle < previousCycle || currentCycle == previousCycle && currentTime < previousTime)
                throw new InvalidOperationException("Timeline logic cursor moved backward without a generation reset.");
            int firstCycle = loop ? previousCycle : 0;
            int lastCycle = loop ? currentCycle : 0;
            for (int cycle = firstCycle; cycle <= lastCycle; cycle++)
            {
                for (int index = 0; index < content.Markers.Count; index++)
                {
                    TimelineContentMarker marker = content.Markers[index];
                    if (!marker.ExecutionPolicy.IsLogic || marker.TrackMuted)
                        continue;
                    bool initial = includeStartBoundary && cycle == previousCycle && marker.Time == previousTime;
                    bool afterPrevious = cycle > previousCycle || cycle == previousCycle && marker.Time > previousTime;
                    bool beforeCurrent = cycle < currentCycle || cycle == currentCycle && marker.Time <= currentTime;
                    if ((!initial && !afterPrevious) || !beforeCurrent)
                        continue;
                    markers.Add(new TimelineRuntimeMarkerRequest(
                        marker.MarkerId,
                        marker.TrackAuthoringId,
                        marker.GraphId,
                        marker.GraphRevision,
                        marker.Time,
                        cycle,
                        generation));
                }
            }
        }

        internal static void ValidateTreeContracts(
            TimelineData timeline,
            TimelineContentUnit content,
            List<string> errors)
        {
            for (int clipIndex = 0; clipIndex < content.Clips.Count; clipIndex++)
            {
                TimelineContentClip contentClip = content.Clips[clipIndex];
                if (!contentClip.ExecutionPolicy.IsLogic ||
                    !TimelineRuntimeEvaluator.TryResolveTreeClip(
                        timeline,
                        contentClip.AuthoringId,
                        out TreeClip treeClip))
                    continue;
                if (!TimelineRuntimeEvaluator.TryGetTreeContract(
                        content,
                        treeClip,
                        out string treeGraphId,
                        out string treeGraphRevision))
                    errors.Add(
                        $"timeline_tree_contract_missing:{treeClip.AuthoringId}");
            }
        }

        internal static bool TryGetTreeContract(
            TimelineContentUnit content,
            TreeClip treeClip,
            out string treeGraphId,
            out string treeGraphRevision)
        {
            treeGraphId = string.Empty;
            treeGraphRevision = string.Empty;
            for (int index = 0; index < content.Clips.Count; index++)
            {
                TimelineContentClip clip = content.Clips[index];
                if (clip.AuthoringId != treeClip.AuthoringId)
                    continue;
                treeGraphId = clip.GraphBinding.GraphId;
                treeGraphRevision = clip.GraphBinding.Revision;
                return true;
            }
            return false;
        }

        internal static bool TryResolveTreeClip(
            TimelineData timeline,
            string authoringId,
            out TreeClip treeClip)
        {
            for (int trackIndex = 0; trackIndex < timeline.Tracks.Count; trackIndex++)
            {
                Track track = timeline.Tracks[trackIndex];
                for (int clipIndex = 0; clipIndex < track.Clips.Count; clipIndex++)
                {
                    if (track.Clips[clipIndex] is TreeClip candidate &&
                        string.Equals(candidate.AuthoringId, authoringId, StringComparison.Ordinal))
                    {
                        treeClip = candidate;
                        return true;
                    }
                }
            }
            treeClip = null;
            return false;
        }

        static bool TryResolveClip(
            TimelineData timeline,
            string authoringId,
            out Clip clip)
        {
            for (int trackIndex = 0; trackIndex < timeline.Tracks.Count; trackIndex++)
            {
                Track track = timeline.Tracks[trackIndex];
                if (track == null || track.PersistentMuted || track.ExecutionDomain != TimelineExecutionDomain.Logic)
                    continue;
                for (int clipIndex = 0; clipIndex < track.Clips.Count; clipIndex++)
                {
                    Clip candidate = track.Clips[clipIndex];
                    if (candidate != null &&
                        string.Equals(candidate.AuthoringId, authoringId, StringComparison.Ordinal))
                    {
                        clip = candidate;
                        return true;
                    }
                }
            }
            clip = null;
            return false;
        }

        static string ResolveContractKind(TimelineData timeline, string authoringId)
        {
            for (int trackIndex = 0; trackIndex < timeline.Tracks.Count; trackIndex++)
            {
                Track track = timeline.Tracks[trackIndex];
                for (int clipIndex = 0; clipIndex < track.Clips.Count; clipIndex++)
                {
                    Clip clip = track.Clips[clipIndex];
                    if (clip != null && string.Equals(clip.AuthoringId, authoringId, StringComparison.Ordinal))
                        return clip.ContractKind;
                }
            }
            return string.Empty;
        }


    }
}
