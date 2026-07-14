using TreeDesigner;

namespace BTSMTL.Timeline
{
    public sealed class TimelineTreeClipRuntimeContext
    {
        public TimelineTreeClipRuntimeContext(
            TreeClip clip,
            ulong playbackIdentity,
            int trackIndex,
            int clipIndex,
            int cycle,
            TimelinePlaybackActionContext actionContext,
            TreeExecutionActivationScope sourceActivation,
            BaseGraph sourceRuntimeGraph)
        {
            Clip = clip;
            PlaybackIdentity = playbackIdentity;
            TrackIndex = trackIndex;
            ClipIndex = clipIndex;
            Cycle = cycle;
            ActionContext = actionContext;
            SourceActivation = sourceActivation;
            SourceRuntimeGraph = sourceRuntimeGraph;
        }

        public TreeClip Clip { get; }
        public TimelineData Timeline => Clip?.Timeline;
        public ulong PlaybackIdentity { get; }
        public int TrackIndex { get; }
        public int ClipIndex { get; }
        public int Cycle { get; }
        public TimelinePlaybackActionContext ActionContext { get; }
        public TreeExecutionActivationScope SourceActivation { get; }
        public BaseGraph SourceRuntimeGraph { get; }
        public float TimelineTime { get; private set; }
        public float ClipTime { get; private set; }
        public float DeltaTime { get; private set; }
        public ulong LocalLogicTick { get; private set; }

        public void Update(float timelineTime, float deltaTime, ulong localLogicTick)
        {
            TimelineTime = timelineTime;
            ClipTime = Clip != null
                ? timelineTime - Clip.StartTime + Clip.ClipInTime
                : 0f;
            DeltaTime = deltaTime;
            LocalLogicTick = localLogicTick;
        }

        public NodeStopContext CreateStopContext(NodeStopOriginCause cause)
        {
            return NodeStopContext.Create(cause, LocalLogicTick, null);
        }
    }
}
