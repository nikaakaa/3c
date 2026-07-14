using System;

namespace ThirdPersonGameplay.Tick
{
    public enum GameplayAccumulatorOverflowPolicy
    {
        DropRemainder,
        PreserveRemainder
    }

    public enum GameplayTickTimeSource
    {
        Scaled,
        Unscaled
    }

    public readonly struct GameplayTickSettings
    {
        public const int DefaultLocalLogicTickRate = 60;
        public const int DefaultMaxCatchUpTicks = 4;

        public GameplayTickSettings(
            int localLogicTickRate,
            int maxCatchUpTicks,
            GameplayAccumulatorOverflowPolicy overflowPolicy,
            GameplayTickTimeSource timeSource)
        {
            LocalLogicTickRate = Math.Max(1, localLogicTickRate);
            MaxCatchUpTicks = Math.Max(1, maxCatchUpTicks);
            OverflowPolicy = overflowPolicy;
            TimeSource = timeSource;
        }

        public int LocalLogicTickRate { get; }
        public int MaxCatchUpTicks { get; }
        public GameplayAccumulatorOverflowPolicy OverflowPolicy { get; }
        public GameplayTickTimeSource TimeSource { get; }
        public float FixedDeltaSeconds => 1f / LocalLogicTickRate;

        public static GameplayTickSettings Default =>
            new GameplayTickSettings(
                DefaultLocalLogicTickRate,
                DefaultMaxCatchUpTicks,
                GameplayAccumulatorOverflowPolicy.DropRemainder,
                GameplayTickTimeSource.Scaled);
    }
}
