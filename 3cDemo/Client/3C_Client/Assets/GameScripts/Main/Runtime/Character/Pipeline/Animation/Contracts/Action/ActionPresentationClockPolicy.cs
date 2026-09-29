using System;
using System.Collections.Generic;
using ThirdPersonSimulation;
using BTSMTL.Timeline.Runtime;
using ThirdPersonCharacter.Pipeline.Presentation;

namespace ThirdPersonCharacter.Pipeline.Animation.Presentation
{
    internal interface IActionPresentationClockPolicy
    {
        void DriveClock(
            AnimationClipPlayerRuntime player,
            AnimationChannelId channelId,
            double presentationSampleTick,
            in CharacterPresentationFactFrame factFrame,
            float presentationDeltaSeconds);
    }

    internal interface IActionPresentationClockCoordinator : IDisposable
    {
        ulong ConfirmedTimelineTick { get; }
        void ConfirmTimelineHistory(ulong confirmedTick);
        void AcceptTimelineProgress(in CharacterPresentationCommand command);
        void AcceptTimelinePresentationFrame(in TimelineRuntimePresentationFrame frame);
        void RetireTimelineProgress(in CharacterPresentationCommand command);
        void ReleaseTimeline(ulong actionInstanceId, int operationIndex, string invocationPath, string timelineId, ulong generation);
        bool TrySampleTimeline(ulong actionInstanceId, int operationIndex, string invocationPath, string timelineId, ulong generation,
            ulong presentationFrame, ulong localLogicTick, float interpolationAlpha, out TimelineRuntimePresentationSample sample);
        void BeginSamplingFrame();
        void CommitSamplingFrame();
        void DiscardSamplingFrame();
        void BeginFrame(IReadOnlyList<ActionAnimationPlaybackCommand> commands);
        void ValidateFrame();
        void CommitFrame();
        void DiscardFrame();
        void Reset();
        IActionPresentationClockPolicy CreatePolicy();
    }

    internal sealed class FreeRunPresentationClockPolicy : IActionPresentationClockPolicy
    {
        public static FreeRunPresentationClockPolicy Shared { get; } =
            new FreeRunPresentationClockPolicy();

        public void DriveClock(
            AnimationClipPlayerRuntime player,
            AnimationChannelId channelId,
            double presentationSampleTick,
            in CharacterPresentationFactFrame factFrame,
            float presentationDeltaSeconds)
        {
            player.Advance(presentationDeltaSeconds, player.PlayRate);
        }
    }

    internal sealed class CommittedMovementPresentationClockPolicy : IActionPresentationClockPolicy
    {
        public void DriveClock(
            AnimationClipPlayerRuntime player,
            AnimationChannelId channelId,
            double presentationSampleTick,
            in CharacterPresentationFactFrame factFrame,
            float presentationDeltaSeconds)
        {
            player.SynchronizeMovementClock(
                factFrame.MovementPlaybackTime,
                factFrame.MovementPlaybackClock,
                in factFrame.LocomotionMotionTimeline,
                presentationDeltaSeconds,
                player.PlayRate);
        }
    }
}
