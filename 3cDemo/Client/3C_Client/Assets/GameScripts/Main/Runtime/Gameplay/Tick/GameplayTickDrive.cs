using System;

namespace ThirdPersonGameplay.Tick
{
    public enum GameplayTickDriveMode
    {
        Realtime = 0,
        Paused = 1,
        ManualStep = 2,
        RatePlayback = 3
    }

    public enum GameplayPresentationDebugClockMode
    {
        LivePresentation = 0,
        LogicLockedPresentation = 1
    }

    public enum GameplayTickDriveCommandKind
    {
        SetRealtime = 0,
        Pause = 1,
        Step = 2,
        SetRatePlayback = 3,
        SetPresentationClock = 4
    }

    public readonly struct GameplayTickDrivePolicy
    {
        public GameplayTickDrivePolicy(
            GameplayTickDriveMode mode,
            GameplayPresentationDebugClockMode presentationClockMode,
            float rateMultiplier,
            ulong queuedManualTicks)
        {
            Mode = mode;
            PresentationClockMode = presentationClockMode;
            RateMultiplier = Math.Max(0.01f, rateMultiplier);
            QueuedManualTicks = queuedManualTicks;
        }

        public static GameplayTickDrivePolicy Default => new GameplayTickDrivePolicy(
            GameplayTickDriveMode.Realtime,
            GameplayPresentationDebugClockMode.LivePresentation,
            1f,
            0);

        public GameplayTickDriveMode Mode { get; }
        public GameplayPresentationDebugClockMode PresentationClockMode { get; }
        public float RateMultiplier { get; }
        public ulong QueuedManualTicks { get; }

        public GameplayTickDrivePolicy WithMode(GameplayTickDriveMode mode, ulong queuedManualTicks)
        {
            return new GameplayTickDrivePolicy(
                mode,
                PresentationClockMode,
                RateMultiplier,
                queuedManualTicks);
        }

        public GameplayTickDrivePolicy WithRatePlayback(float rateMultiplier)
        {
            return new GameplayTickDrivePolicy(
                GameplayTickDriveMode.RatePlayback,
                PresentationClockMode,
                rateMultiplier,
                0);
        }

        public GameplayTickDrivePolicy WithPresentationClock(GameplayPresentationDebugClockMode mode)
        {
            return new GameplayTickDrivePolicy(
                Mode,
                mode,
                RateMultiplier,
                QueuedManualTicks);
        }
    }

    public readonly struct GameplayTickDriveCommand
    {
        GameplayTickDriveCommand(
            GameplayTickDriveCommandKind kind,
            ulong stepCount,
            float rateMultiplier,
            GameplayPresentationDebugClockMode presentationClockMode,
            ulong sequence)
        {
            Kind = kind;
            StepCount = stepCount;
            RateMultiplier = rateMultiplier;
            PresentationClockMode = presentationClockMode;
            Sequence = sequence;
        }

        public GameplayTickDriveCommandKind Kind { get; }
        public ulong StepCount { get; }
        public float RateMultiplier { get; }
        public GameplayPresentationDebugClockMode PresentationClockMode { get; }
        public ulong Sequence { get; }

        public static GameplayTickDriveCommand SetRealtime()
        {
            return new GameplayTickDriveCommand(
                GameplayTickDriveCommandKind.SetRealtime,
                0,
                1f,
                GameplayPresentationDebugClockMode.LivePresentation,
                0);
        }

        public static GameplayTickDriveCommand Pause()
        {
            return new GameplayTickDriveCommand(
                GameplayTickDriveCommandKind.Pause,
                0,
                1f,
                GameplayPresentationDebugClockMode.LivePresentation,
                0);
        }

        public static GameplayTickDriveCommand Step(ulong stepCount)
        {
            return new GameplayTickDriveCommand(
                GameplayTickDriveCommandKind.Step,
                Math.Max(1UL, stepCount),
                1f,
                GameplayPresentationDebugClockMode.LivePresentation,
                0);
        }

        public static GameplayTickDriveCommand SetRatePlayback(float rateMultiplier)
        {
            return new GameplayTickDriveCommand(
                GameplayTickDriveCommandKind.SetRatePlayback,
                0,
                Math.Max(0.01f, rateMultiplier),
                GameplayPresentationDebugClockMode.LivePresentation,
                0);
        }

        public static GameplayTickDriveCommand SetPresentationClock(GameplayPresentationDebugClockMode mode)
        {
            return new GameplayTickDriveCommand(
                GameplayTickDriveCommandKind.SetPresentationClock,
                0,
                1f,
                mode,
                0);
        }

        public GameplayTickDriveCommand WithSequence(ulong sequence)
        {
            return new GameplayTickDriveCommand(
                Kind,
                StepCount,
                RateMultiplier,
                PresentationClockMode,
                sequence);
        }
    }

    public readonly struct GameplayTickDriveStatusSnapshot
    {
        public GameplayTickDriveStatusSnapshot(
            GameplayTickDrivePolicy policy,
            ulong renderFrame,
            ulong localLogicTick,
            float interpolationAlpha,
            int droppedLocalLogicTicks,
            ulong pendingCommandCount,
            ulong lastCommandSequence,
            float presentationDeltaSeconds)
        {
            Policy = policy;
            RenderFrame = renderFrame;
            LocalLogicTick = localLogicTick;
            InterpolationAlpha = interpolationAlpha;
            DroppedLocalLogicTicks = droppedLocalLogicTicks;
            PendingCommandCount = pendingCommandCount;
            LastCommandSequence = lastCommandSequence;
            PresentationDeltaSeconds = presentationDeltaSeconds;
        }

        public GameplayTickDrivePolicy Policy { get; }
        public GameplayTickDriveMode Mode => Policy.Mode;
        public GameplayPresentationDebugClockMode PresentationClockMode => Policy.PresentationClockMode;
        public float RateMultiplier => Policy.RateMultiplier;
        public ulong QueuedManualTicks => Policy.QueuedManualTicks;
        public ulong RenderFrame { get; }
        public ulong LocalLogicTick { get; }
        public float InterpolationAlpha { get; }
        public int DroppedLocalLogicTicks { get; }
        public ulong PendingCommandCount { get; }
        public ulong LastCommandSequence { get; }
        public float PresentationDeltaSeconds { get; }
    }
}
