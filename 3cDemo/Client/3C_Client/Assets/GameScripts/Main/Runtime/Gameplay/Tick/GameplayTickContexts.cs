using System;

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
            : this(
                scaledDeltaSeconds,
                unscaledDeltaSeconds,
                scaledDeltaSeconds,
                GameplayPresentationDebugClockMode.LivePresentation,
                renderFrame,
                localLogicTick,
                interpolationAlpha,
                1f,
                1f,
                false)
        {
        }

        public GameplayPresentationFrameContext(
            float scaledDeltaSeconds,
            float unscaledDeltaSeconds,
            float presentationDeltaSeconds,
            GameplayPresentationDebugClockMode presentationClockMode,
            ulong renderFrame,
            ulong localLogicTick,
            float interpolationAlpha)
            : this(
                scaledDeltaSeconds,
                unscaledDeltaSeconds,
                presentationDeltaSeconds,
                presentationClockMode,
                renderFrame,
                localLogicTick,
                interpolationAlpha,
                1f,
                1f,
                false)
        {
        }

        public GameplayPresentationFrameContext(
            float scaledDeltaSeconds,
            float unscaledDeltaSeconds,
            float presentationDeltaSeconds,
            GameplayPresentationDebugClockMode presentationClockMode,
            ulong renderFrame,
            ulong localLogicTick,
            float interpolationAlpha,
            float ownerTimeScale,
            float localAvatarTimeScale,
            bool paused)
        {
            if (!float.IsFinite(scaledDeltaSeconds) || scaledDeltaSeconds < 0f ||
                !float.IsFinite(unscaledDeltaSeconds) || unscaledDeltaSeconds < 0f ||
                !float.IsFinite(presentationDeltaSeconds) || presentationDeltaSeconds < 0f ||
                !float.IsFinite(interpolationAlpha) || interpolationAlpha < 0f || interpolationAlpha > 1f ||
                !float.IsFinite(ownerTimeScale) || ownerTimeScale < 0f ||
                !float.IsFinite(localAvatarTimeScale) || localAvatarTimeScale < 0f)
                throw new ArgumentOutOfRangeException(nameof(ownerTimeScale));
            ScaledDeltaSeconds = scaledDeltaSeconds;
            UnscaledDeltaSeconds = unscaledDeltaSeconds;
            PresentationDeltaSeconds = presentationDeltaSeconds;
            PresentationClockMode = presentationClockMode;
            RenderFrame = renderFrame;
            LocalLogicTick = localLogicTick;
            InterpolationAlpha = interpolationAlpha;
            OwnerTimeScale = ownerTimeScale;
            LocalAvatarTimeScale = localAvatarTimeScale;
            Paused = paused;
        }

        public float ScaledDeltaSeconds { get; }
        public float UnscaledDeltaSeconds { get; }
        public float PresentationDeltaSeconds { get; }
        public GameplayPresentationDebugClockMode PresentationClockMode { get; }
        public ulong RenderFrame { get; }
        public ulong LocalLogicTick { get; }
        public float InterpolationAlpha { get; }
        public float OwnerTimeScale { get; }
        public float LocalAvatarTimeScale { get; }
        public bool Paused { get; }
    }
}
