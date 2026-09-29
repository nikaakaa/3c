using System;
using ThirdPersonSimulation.Fixed;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ThirdPersonSimulation;
using UnityEngine;

namespace BTSMTL.Timeline.Runtime
{
    internal static class TimelineRuntimePresentationEvaluator
    {
        public static TimelineRuntimePresentationOperations Evaluate(
            TimelineRuntimePlayback playback,
            FixedScalar previousPosition,
            int previousCycle,
            FixedScalar currentPosition,
            int currentCycle,
            bool loop,
            bool includeStartBoundary,
            TimelineRuntimePresentationBuffer buffer)
        {
            if (playback == null)
                throw new ArgumentNullException(nameof(playback));
            TimelineData timeline = playback.SourceTimeline;
            FixedScalar contentDuration = playback.Content.Duration;
            var animations = buffer.Animations;
            var cameraStates = buffer.CameraStates;
            var cameraResponses = buffer.CameraResponses;
            var cameraResources = buffer.CameraResources;
            var scenePresentation = buffer.ScenePresentation;
            var segments = new TimelineRuntimeEvaluationSegments(
                previousPosition,
                previousCycle,
                currentPosition,
                currentCycle,
                contentDuration,
                loop);
            for (int segmentIndex = 0; segmentIndex < segments.Count; segmentIndex++)
            {
                TimelineRuntimeEvaluationSegment segment = segments[segmentIndex];
                for (int trackIndex = 0; trackIndex < timeline.Tracks.Count; trackIndex++)
                {
                    Track track = timeline.Tracks[trackIndex];
                    if (track == null || track.PersistentMuted || track.ExecutionDomain != TimelineExecutionDomain.Presentation)
                        continue;
                }
            }
            FixedScalar currentTime = currentPosition;
            for (int trackIndex = 0; trackIndex < timeline.Tracks.Count; trackIndex++)
            {
                Track track = timeline.Tracks[trackIndex];
                if (track == null || track.PersistentMuted || track.ExecutionDomain != TimelineExecutionDomain.Presentation)
                    continue;
                if (track is AnimationTrack animationTrack)
                    animationTrack.Sample(
                        currentTime,
                        currentTime,
                        trackIndex,
                        timeline.AuthoringId,
                        timeline.Name,
                        animations,
                        loop,
                        currentCycle);
                else if (track is CameraStateTrack cameraStateTrack)
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
                else if (track is ScenePresentationParameterTrack sceneTrack && !track.PersistentMuted)
                {
                    for (int clipIndex = 0; clipIndex < sceneTrack.Clips.Count; clipIndex++)
                    {
                        if (sceneTrack.Clips[clipIndex] is not ScenePresentationParameterCurveClip clip ||
                            clip.ExecutionDomain != TimelineExecutionDomain.Presentation ||
                            currentTime < clip.StartTime || currentTime > clip.EndTime)
                        {
                            continue;
                        }
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
                            playback.ExecutionIdentity,
                            playback.Generation));
                    }
                }
            }
            return new TimelineRuntimePresentationOperations(buffer);
        }


    }
}
