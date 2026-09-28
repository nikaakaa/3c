using System;
using System.Collections.Generic;
using BTSMTL.Timeline;
using BTSMTL.Timeline.Runtime;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Animation.Lifecycle
{
    internal static class CharacterTimelineSnapshotCodec
    {
        internal static AbilityTimelineRuntimeSnapshot Capture(
            TimelineRuntimePlaybackSnapshot native,
            AbilityTimelineStartRequest request,
            IReadOnlyList<AbilityTimelineTreeClipState> treeClips)
        {
            return new AbilityTimelineRuntimeSnapshot(
                (int)native.Handle.Value,
                native.Generation,
                native.RequestId,
                native.ExecutionIdentity.OwnerIdentity,
                native.ExecutionIdentity.CallIdentity,
                native.ExecutionIdentity.InstanceId,
                MapSnapshotMode(native.PlaybackMode),
                native.ContentRevision,
                MapSnapshotState(native.State),
                native.CursorTime,
                native.Cycle,
                native.TimeCarry,
                native.Control,
                native.TreeDecisionExits,
                native.PendingTreeDecisionExits,
                native.SectionId,
                native.ActiveClipIds,
                TimelineSnapshotItems<AbilityTimelineTreeClipState>.CopyFrom(treeClips),
                native.HasStopContext,
                MapSnapshotStopCause(native.StopContext.Cause),
                native.StopContext.LocalLogicTick,
                native.InitialBoundaryPending,
                request.TimelineId,
                request.Loop,
                request.ActionContext,
                request.InvocationSource,
                request.InputSequence,
                request.Tick);
        }

        internal static TimelineRuntimePlaybackSnapshot Restore(
            AbilityTimelineRuntimeSnapshot snapshot,
            TimelineExecutionIdentity executionIdentity,
            TimelinePlaybackMode playbackMode,
            TimelineRuntimeNumericTarget numericTarget,
            int tickRate)
        {
            var stopCause = MapPlaybackStopCause(snapshot.StopCause);
            return new TimelineRuntimePlaybackSnapshot(
                new TimelineRuntimePlaybackHandle((ulong)snapshot.RuntimeHandle),
                snapshot.Generation,
                snapshot.RequestId,
                executionIdentity,
                playbackMode,
                numericTarget,
                snapshot.ContentRevision,
                MapPlaybackState(snapshot.State),
                snapshot.CursorTime,
                snapshot.Cycle,
                snapshot.SectionId,
                snapshot.ActiveClipIds,
                snapshot.HasStopContext,
                new TimelinePlaybackStopContext(stopCause, snapshot.StopLocalLogicTick),
                snapshot.InitialBoundaryPending,
                snapshot.TimeCarry,
                snapshot.Control,
                snapshot.TreeDecisionExits,
                snapshot.PendingTreeDecisionExits,
                tickRate);
        }

        static AbilityTimelineSnapshotMode MapSnapshotMode(TimelinePlaybackMode mode) => mode switch
        {
            TimelinePlaybackMode.Loop => AbilityTimelineSnapshotMode.Loop,
            _ => AbilityTimelineSnapshotMode.Once
        };

        internal static TimelinePlaybackMode MapPlaybackMode(AbilityTimelineSnapshotMode mode) => mode switch
        {
            AbilityTimelineSnapshotMode.Loop => TimelinePlaybackMode.Loop,
            _ => TimelinePlaybackMode.Once
        };

        static AbilityTimelineSnapshotState MapSnapshotState(TimelineRuntimePlaybackState state) => state switch
        {
            TimelineRuntimePlaybackState.Prepared => AbilityTimelineSnapshotState.Prepared,
            TimelineRuntimePlaybackState.Running => AbilityTimelineSnapshotState.Running,
            TimelineRuntimePlaybackState.Stopping => AbilityTimelineSnapshotState.Stopping,
            TimelineRuntimePlaybackState.Completed => AbilityTimelineSnapshotState.Completed,
            TimelineRuntimePlaybackState.Stopped => AbilityTimelineSnapshotState.Stopped,
            TimelineRuntimePlaybackState.Failed => AbilityTimelineSnapshotState.Failed,
            TimelineRuntimePlaybackState.Disposed => AbilityTimelineSnapshotState.Disposed,
            _ => throw new InvalidOperationException("Timeline playback state is invalid.")
        };

        static TimelineRuntimePlaybackState MapPlaybackState(AbilityTimelineSnapshotState state) => state switch
        {
            AbilityTimelineSnapshotState.Prepared => TimelineRuntimePlaybackState.Prepared,
            AbilityTimelineSnapshotState.Running => TimelineRuntimePlaybackState.Running,
            AbilityTimelineSnapshotState.Stopping => TimelineRuntimePlaybackState.Stopping,
            AbilityTimelineSnapshotState.Completed => TimelineRuntimePlaybackState.Completed,
            AbilityTimelineSnapshotState.Stopped => TimelineRuntimePlaybackState.Stopped,
            AbilityTimelineSnapshotState.Failed => TimelineRuntimePlaybackState.Failed,
            AbilityTimelineSnapshotState.Disposed => TimelineRuntimePlaybackState.Disposed,
            _ => throw new ArgumentOutOfRangeException(nameof(state))
        };

        static AbilityTimelineSnapshotStopCause MapSnapshotStopCause(TimelinePlaybackStopCause cause) => cause switch
        {
            TimelinePlaybackStopCause.SelfAbort => AbilityTimelineSnapshotStopCause.SelfAbort,
            TimelinePlaybackStopCause.LowerPriorityAbort => AbilityTimelineSnapshotStopCause.LowerPriorityAbort,
            TimelinePlaybackStopCause.ExplicitParentStop => AbilityTimelineSnapshotStopCause.ExplicitParentStop,
            TimelinePlaybackStopCause.StateTransition => AbilityTimelineSnapshotStopCause.StateTransition,
            TimelinePlaybackStopCause.Reset => AbilityTimelineSnapshotStopCause.Reset,
            TimelinePlaybackStopCause.Shutdown => AbilityTimelineSnapshotStopCause.Shutdown,
            _ => AbilityTimelineSnapshotStopCause.None
        };

        static TimelinePlaybackStopCause MapPlaybackStopCause(AbilityTimelineSnapshotStopCause cause) => cause switch
        {
            AbilityTimelineSnapshotStopCause.SelfAbort => TimelinePlaybackStopCause.SelfAbort,
            AbilityTimelineSnapshotStopCause.LowerPriorityAbort => TimelinePlaybackStopCause.LowerPriorityAbort,
            AbilityTimelineSnapshotStopCause.ExplicitParentStop => TimelinePlaybackStopCause.ExplicitParentStop,
            AbilityTimelineSnapshotStopCause.StateTransition => TimelinePlaybackStopCause.StateTransition,
            AbilityTimelineSnapshotStopCause.Reset => TimelinePlaybackStopCause.Reset,
            AbilityTimelineSnapshotStopCause.Shutdown => TimelinePlaybackStopCause.Shutdown,
            _ => TimelinePlaybackStopCause.SelfAbort
        };
    }
}
