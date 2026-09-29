using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Diagnostics;
using BTSMTL.Timeline;
using BTSMTL.Timeline.Runtime;
using ThirdPersonSimulation;
using ThirdPersonSimulation.Fixed;
using ThirdPersonCharacter.Pipeline.Animation.Presentation;
using ThirdPersonGameplay.Tick;
using TimelinePlaybackStatus = BTSMTL.Timeline.TimelinePlaybackStatus;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation.Lifecycle
{
    public readonly struct CharacterTimelinePendingAdvance
    {
        internal CharacterTimelinePendingAdvance(
            TimelinePlaybackHandle handle,
            int runtimeHandle,
            TimelineRuntimeAdvanceResult result,
            string timelineId,
            AbilityTimelineRuntimeStatus status,
            ulong sequence)
        {
            Handle = handle;
            Result = result;
            Status = status;
            AbilityTimelineProgress progress = !result.IsValid ? default : new AbilityTimelineProgress(
                timelineId,
                result.ContentRevision,
                result.Generation,
                result.LogicTick,
                result.Duration,
                result.PreviousTime,
                result.Time,
                result.PreviousCycle,
                result.Cycle,
                result.PlaybackMode == TimelinePlaybackMode.Loop,
                result.Completes ? AbilityTimelineProgressState.Completed : AbilityTimelineProgressState.Active, result.Control);
            Pending = result.IsValid ? new AbilityTimelineAdvancePending(runtimeHandle, sequence, progress) : default;
        }

        public TimelinePlaybackHandle Handle { get; }
        public AbilityTimelineRuntimeStatus Status { get; }
        public AbilityTimelineAdvancePending Pending { get; }
        internal TimelineRuntimeAdvanceResult Result { get; }
    }

    public readonly struct CharacterTimelinePendingStop
    {
        internal CharacterTimelinePendingStop(
            TimelinePlaybackHandle handle,
            int runtimeHandle,
            TimelineRuntimeStopRequest request,
            string timelineId,
            AbilityTimelineRuntimeStatus status,
            ulong sequence)
        {
            Handle = handle;
            Request = request;
            Status = status;
            TimelineRuntimePlayback playback = request.Playback;
            AbilityTimelineProgress progress = playback == null || request.Reason.LocalLogicTick == 0 ? default : new AbilityTimelineProgress(
                timelineId, playback.ContentRevision, playback.Generation, request.Reason.LocalLogicTick,
                playback.Content.Duration, playback.CursorTime, playback.CursorTime, playback.Cycle, playback.Cycle,
                playback.PlaybackMode == TimelinePlaybackMode.Loop, AbilityTimelineProgressState.Stopped, playback.Control);
            Pending = playback != null ? new AbilityTimelineStopPending(runtimeHandle, sequence, progress) : default;
        }

        public TimelinePlaybackHandle Handle { get; }
        public AbilityTimelineStopPending Pending { get; }
        internal TimelineRuntimeStopRequest Request { get; }
        public AbilityTimelineRuntimeStatus Status { get; }
    }
}
