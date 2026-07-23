using System.Collections.Generic;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public interface IAnimationPlaybackCommandSink
    {
        void EnqueueSelection(AnimationChannelSelection selection);
        void EnqueuePoseRequest(ulong localLogicTick, AnimationSelectionFrame poseRequest);
        void EnqueuePoseUnavailable(ulong localLogicTick, AnimationChannelId channelId, AnimationPlaybackId playbackId);
        void EnqueuePlaybackComplete(ulong localLogicTick, AnimationPlaybackId playbackId);
        void EnqueuePlaybackRelease(ulong localLogicTick, AnimationPlaybackId playbackId);
    }

    public interface IAnimationPlaybackBatchSource
    {
        int PendingCount { get; }
        void CopyPendingTo(List<AnimationPlaybackCommand> destination);
        void Acknowledge(IReadOnlyList<AnimationPlaybackCommand> commands);
        void Clear();
    }
}
