namespace ThirdPersonGameplay.Tick
{
    public readonly struct GameplayLogicTickContext
    {
        public GameplayLogicTickContext(
            float fixedDeltaSeconds,
            ulong renderFrame,
            ulong localLogicTick,
            ulong inputSequence)
        {
            FixedDeltaSeconds = fixedDeltaSeconds;
            RenderFrame = renderFrame;
            LocalLogicTick = localLogicTick;
            InputSequence = inputSequence;
        }

        public float FixedDeltaSeconds { get; }
        public ulong RenderFrame { get; }
        public ulong LocalLogicTick { get; }
        public ulong InputSequence { get; }

        public GameplayLogicTickContext WithInputSequence(ulong inputSequence)
        {
            return new GameplayLogicTickContext(
                FixedDeltaSeconds,
                RenderFrame,
                LocalLogicTick,
                inputSequence);
        }
    }

    public readonly struct GameplayPresentationFrameContext
    {
        public GameplayPresentationFrameContext(
            float scaledDeltaSeconds,
            float unscaledDeltaSeconds,
            ulong renderFrame,
            ulong localLogicTick,
            float interpolationAlpha)
        {
            ScaledDeltaSeconds = scaledDeltaSeconds;
            UnscaledDeltaSeconds = unscaledDeltaSeconds;
            RenderFrame = renderFrame;
            LocalLogicTick = localLogicTick;
            InterpolationAlpha = interpolationAlpha;
        }

        public float ScaledDeltaSeconds { get; }
        public float UnscaledDeltaSeconds { get; }
        public ulong RenderFrame { get; }
        public ulong LocalLogicTick { get; }
        public float InterpolationAlpha { get; }
    }
}
