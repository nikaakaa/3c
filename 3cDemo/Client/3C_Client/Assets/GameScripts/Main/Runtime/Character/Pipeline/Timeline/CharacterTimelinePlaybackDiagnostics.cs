using System;
using BTSMTL.Diagnostics;
using BTSMTL.Timeline;
using BTSMTL.Timeline.Runtime;

namespace ThirdPersonCharacter.Pipeline.Animation.Lifecycle
{
    internal readonly struct CharacterTimelinePlaybackTrace
    {
        internal CharacterTimelinePlaybackTrace(TimelinePlaybackHandle handle, TimelineData timeline, IDebugSourceMap sourceMap,
            RuntimeInstanceKey runtimeInstance, RuntimeTimelinePlaybackProvenance provenance, ulong actionInstanceId)
        {
            Handle = handle;
            Timeline = timeline;
            SourceMap = sourceMap;
            RuntimeInstance = runtimeInstance;
            Provenance = provenance;
            ActionInstanceId = actionInstanceId;
        }

        internal TimelinePlaybackHandle Handle { get; }
        internal TimelineData Timeline { get; }
        internal IDebugSourceMap SourceMap { get; }
        internal RuntimeInstanceKey RuntimeInstance { get; }
        internal RuntimeTimelinePlaybackProvenance Provenance { get; }
        internal ulong ActionInstanceId { get; }
    }

    internal static class CharacterTimelinePlaybackDiagnostics
    {
        internal static void PublishActiveTimelineElements(
            RuntimeDiagnosticsContext diagnostics, in CharacterTimelinePlaybackTrace active,
            TimelineRuntimeCommittedEvaluation evaluation,
            float time)
        {
            if (active.Timeline == null || diagnostics == null)
                return;
            bool publishTracks = diagnostics.ShouldPublish(RuntimeTraceChannel.Timeline, RuntimeTraceEventKind.TrackActive);
            bool publishClips = diagnostics.ShouldPublish(RuntimeTraceChannel.Timeline, RuntimeTraceEventKind.ClipActive);
            if (!publishTracks && !publishClips)
                return;
            TimelineRuntimeSampleView<string> activeClipIds = evaluation.ActiveClipIds;
            for (int trackIndex = 0; trackIndex < active.Timeline.Tracks.Count; trackIndex++)
            {
                Track track = active.Timeline.Tracks[trackIndex];
                if (track == null)
                    continue;
                bool trackPublished = false;
                for (int clipIndex = 0; clipIndex < track.Clips.Count; clipIndex++)
                {
                    Clip clip = track.Clips[clipIndex];
                    if (clip == null)
                        continue;
                    bool isActive = false;
                    for (int activeIndex = 0; activeIndex < activeClipIds.Count; activeIndex++)
                    {
                        if (!string.Equals(activeClipIds[activeIndex], clip.AuthoringId, StringComparison.Ordinal))
                            continue;
                        isActive = true;
                        break;
                    }
                    if (!isActive)
                        continue;
                    if (publishTracks && !trackPublished)
                    {
                        PublishTimelineEvent(diagnostics, in active, RuntimeTraceDomain.Logic, RuntimeTraceEventKind.TrackActive,
                            RuntimeSourceElementKey.Track(active.Timeline.AuthoringId, track.AuthoringId),
                            "Active", string.Empty, time, evaluation.Cycle);
                        trackPublished = true;
                    }
                    if (publishClips)
                        PublishTimelineEvent(diagnostics, in active, RuntimeTraceDomain.Logic, RuntimeTraceEventKind.ClipActive,
                            RuntimeSourceElementKey.Clip(active.Timeline.AuthoringId, track.AuthoringId,
                                clip.AuthoringId, clip is TreeClip), "Active", string.Empty, time, evaluation.Cycle);
                }
            }
        }

        internal static void PublishTreeClipEvents(
            RuntimeDiagnosticsContext diagnostics, in CharacterTimelinePlaybackTrace active,
            TimelineRuntimeCommittedEvaluation evaluation)
        {
            PublishTreeClipEvents(diagnostics, in active, evaluation.Evaluation.TreeClips, RuntimeTraceDomain.Logic);
        }

        internal static void PublishTreeClipEvents(RuntimeDiagnosticsContext diagnostics, in CharacterTimelinePlaybackTrace active, TimelineRuntimeSampleView<TimelineRuntimeTreeClipRequest> requests,
            RuntimeTraceDomain domain)
        {
            for (int index = 0; index < requests.Count; index++)
            {
                TimelineRuntimeTreeClipRequest request = requests[index];
                RuntimeTraceEventKind kind = request.EventKind switch
                {
                    TimelineRuntimeTreeClipEventKind.Enter => RuntimeTraceEventKind.TreeClipEntered,
                    TimelineRuntimeTreeClipEventKind.Update => RuntimeTraceEventKind.TreeClipUpdated,
                    TimelineRuntimeTreeClipEventKind.Exit => RuntimeTraceEventKind.TreeClipExited,
                    TimelineRuntimeTreeClipEventKind.Destroy => RuntimeTraceEventKind.TreeClipDestroyed,
                    _ => throw new ArgumentOutOfRangeException()
                };
                if (diagnostics == null || !diagnostics.ShouldPublish(RuntimeTraceChannel.Timeline, kind))
                    continue;
                RuntimeInstanceKey treeClip = RuntimeInstanceKey.TreeClip(
                    active.RuntimeInstance.CharacterRuntimeId,
                    active.Provenance.SourceGraphRuntimeId,
                    active.RuntimeInstance.SourceOperationIndex,
                    active.Handle.Value,
                    request.Cycle,
                    active.ActionInstanceId);
                PublishTimelineEvent(diagnostics, in active,
                    domain,
                    kind,
                    RuntimeSourceElementKey.Clip(
                        active.Timeline.AuthoringId,
                        request.TrackAuthoringId,
                        request.ClipAuthoringId,
                        true),
                    request.EventKind switch
                    {
                        TimelineRuntimeTreeClipEventKind.Enter => "Enter",
                        TimelineRuntimeTreeClipEventKind.Update => "Update",
                        TimelineRuntimeTreeClipEventKind.Exit => "Exit",
                        TimelineRuntimeTreeClipEventKind.Destroy => "Destroy",
                        _ => throw new ArgumentOutOfRangeException()
                    },
                    string.Empty,
                    request.Time.ToSingle(),
                    request.Cycle,
                    treeClip,
                    request.TreeGraphId,
                    request.ExitSource.ToString());
            }
        }

        internal static void PublishTimelineVisualTime(RuntimeDiagnosticsContext diagnostics, in CharacterTimelinePlaybackTrace active, TimelineRuntimePresentationFrame frame)
        {
            PublishTimelineEvent(diagnostics, in active,
                RuntimeTraceDomain.Presentation,
                RuntimeTraceEventKind.TimelineVisualTime,
                RuntimeSourceElementKey.Timeline(active.Timeline.AuthoringId),
                "Presented",
                string.Empty,
                frame.Time.ToSingle(),
                frame.Cycle,
                default,
                string.Empty);
        }

        internal static void PublishTimelineLifecycle(
            RuntimeDiagnosticsContext diagnostics, in CharacterTimelinePlaybackTrace active,
            RuntimeTraceEventKind kind,
            string status,
            string cause)
        {
            if (active.Timeline == null)
                return;
            PublishTimelineEvent(diagnostics, in active,
                RuntimeTraceDomain.Lifecycle,
                kind,
                RuntimeSourceElementKey.Timeline(active.Timeline.AuthoringId),
                status,
                cause,
                0f,
                0);
        }

        internal static void PublishTimelineEvent(
            RuntimeDiagnosticsContext diagnostics, in CharacterTimelinePlaybackTrace active,
            RuntimeTraceDomain domain,
            RuntimeTraceEventKind kind,
            RuntimeSourceElementKey source,
            string status,
            string cause,
            float time,
            int cycle,
            RuntimeInstanceKey runtimeInstance = default,
            string relatedElementId = "",
            string detail = "")
        {
            if (diagnostics == null || !active.RuntimeInstance.IsValid ||
                !diagnostics.ShouldPublish(RuntimeTraceChannel.Timeline, kind))
                return;
            diagnostics.Publish(
                active.SourceMap,
                RuntimeTraceChannel.Timeline,
                domain,
                kind,
                source,
                runtimeInstance.IsValid ? runtimeInstance : active.RuntimeInstance,
                new RuntimeTracePayload
                {
                    Name = active.Timeline.Name,
                    Status = status,
                    Detail = detail,
                    Cause = cause,
                    RelatedElementId = relatedElementId,
                    ActionInstanceId = active.ActionInstanceId,
                    ActivationGeneration = active.Provenance.SourceActivationGeneration,
                    Time = time,
                    Cycle = cycle,
                    TimelinePlayback = active.Provenance
                });
        }

    }
}
