using System;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal enum ActionPresentationClockCommandKind : byte
    {
        AdvanceByDelta = 1,
        SeekAbsoluteTime = 2
    }

    internal readonly struct ActionPresentationClockInstruction
    {
        internal ActionPresentationClockInstruction(
            ActionPresentationClockCommandKind kind,
            double absoluteTimeSeconds,
            float deltaSeconds,
            float playRate)
        {
            Kind = kind;
            AbsoluteTimeSeconds = absoluteTimeSeconds;
            DeltaSeconds = deltaSeconds;
            PlayRate = playRate;
        }

        internal ActionPresentationClockCommandKind Kind { get; }
        internal double AbsoluteTimeSeconds { get; }
        internal float DeltaSeconds { get; }
        internal float PlayRate { get; }
    }

    internal interface IActionPresentationClockPolicy
    {
        ActionPresentationClockInstruction Resolve(
            AnimationClipPlayerRuntime player,
            AnimationChannelId channelId,
            double presentationSampleTick,
            float presentationDeltaSeconds);
    }

    internal sealed class FreeRunPresentationClockPolicy : IActionPresentationClockPolicy
    {
        public static FreeRunPresentationClockPolicy Shared { get; } =
            new FreeRunPresentationClockPolicy();

        public ActionPresentationClockInstruction Resolve(
            AnimationClipPlayerRuntime player,
            AnimationChannelId channelId,
            double presentationSampleTick,
            float presentationDeltaSeconds)
        {
            return new ActionPresentationClockInstruction(
                ActionPresentationClockCommandKind.AdvanceByDelta,
                0d,
                presentationDeltaSeconds,
                player.PlayRate);
        }
    }
}
