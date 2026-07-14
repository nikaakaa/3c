using BTSMTL.Diagnostics;
using TreeDesigner;

namespace BTSMTL.Timeline
{
    public readonly struct TimelinePlaybackRequest
    {
        public TimelinePlaybackRequest(
            TimelinePlaybackHandle handle,
            TimelineData timeline,
            string sourceId,
            string sourceName,
            TimelinePlaybackActionContext actionContext,
            TimelinePlaybackMode playbackMode,
            TreeExecutionActivationScope sourceActivation,
            BaseGraph sourceRuntimeGraph,
            RuntimeTimelinePlaybackProvenance diagnosticsProvenance)
        {
            Handle = handle;
            Timeline = timeline;
            SourceId = sourceId;
            SourceName = sourceName;
            ActionContext = actionContext;
            PlaybackMode = playbackMode;
            SourceActivation = sourceActivation;
            SourceRuntimeGraph = sourceRuntimeGraph;
            DiagnosticsProvenance = diagnosticsProvenance;
        }

        public TimelinePlaybackHandle Handle { get; }
        public TimelineData Timeline { get; }
        public string SourceId { get; }
        public string SourceName { get; }
        public TimelinePlaybackActionContext ActionContext { get; }
        public TimelinePlaybackMode PlaybackMode { get; }
        public TreeExecutionActivationScope SourceActivation { get; }
        public BaseGraph SourceRuntimeGraph { get; }
        public RuntimeTimelinePlaybackProvenance DiagnosticsProvenance { get; }
    }

    public interface ITimelinePlaybackService
    {
        bool RequestTimelinePlayback(
            TimelineData timeline,
            string sourceId,
            string sourceName,
            TimelinePlaybackActionContext actionContext,
            TimelinePlaybackMode playbackMode,
            TreeExecutionActivationScope sourceActivation,
            BaseGraph sourceRuntimeGraph,
            out TimelinePlaybackHandle handle);
        TimelinePlaybackStatus GetTimelinePlaybackStatus(TimelinePlaybackHandle handle);
        void CancelTimelinePlayback(TimelinePlaybackHandle handle, TimelinePlaybackStopContext stopContext);
    }
}
