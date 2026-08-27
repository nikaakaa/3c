using System;

namespace ThirdPersonGameplay.Tick
{
    public enum GameplayTickDriveMode
    {
        Realtime = 0,
        Paused = 1,
        ManualStep = 2,
        RatePlayback = 3,
        LivePresentationScheduleCapture = 4,
        ScriptedPresentationFrame = 5
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
        SetPresentationClock = 4,
        BeginLivePresentationScheduleCapture = 5,
        BeginScriptedPresentationSchedule = 6,
        ScriptedPresentationFrame = 7,
        EndPresentationSchedule = 8,
        CancelPresentationSchedule = 9
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

        public GameplayTickDrivePolicy WithRateMultiplier(float rateMultiplier)
        {
            return new GameplayTickDrivePolicy(
                Mode,
                PresentationClockMode,
                rateMultiplier,
                QueuedManualTicks);
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

    public readonly struct GameplayScriptedPresentationFrame
    {
        public GameplayScriptedPresentationFrame(
            int frameIndex,
            ulong relativeStartLocalLogicTick,
            ulong relativeEndLocalLogicTick,
            float scaledDeltaSeconds,
            float unscaledDeltaSeconds,
            float presentationDeltaSeconds,
            float interpolationAlpha,
            GameplayPresentationDebugClockMode presentationClockMode)
        {
            if (frameIndex < 0 ||
                relativeEndLocalLogicTick < relativeStartLocalLogicTick ||
                relativeEndLocalLogicTick - relativeStartLocalLogicTick > int.MaxValue ||
                !FiniteNonNegative(scaledDeltaSeconds) ||
                !FiniteNonNegative(unscaledDeltaSeconds) ||
                !FiniteNonNegative(presentationDeltaSeconds) ||
                !float.IsFinite(interpolationAlpha) ||
                interpolationAlpha < 0f || interpolationAlpha > 1f ||
                !Enum.IsDefined(typeof(GameplayPresentationDebugClockMode), presentationClockMode))
            {
                throw new ArgumentException("Scripted Presentation Frame is invalid.");
            }
            FrameIndex = frameIndex;
            RelativeStartLocalLogicTick = relativeStartLocalLogicTick;
            RelativeEndLocalLogicTick = relativeEndLocalLogicTick;
            ScaledDeltaSeconds = scaledDeltaSeconds;
            UnscaledDeltaSeconds = unscaledDeltaSeconds;
            PresentationDeltaSeconds = presentationDeltaSeconds;
            InterpolationAlpha = interpolationAlpha;
            PresentationClockMode = presentationClockMode;
        }

        public int FrameIndex { get; }
        public ulong RelativeStartLocalLogicTick { get; }
        public ulong RelativeEndLocalLogicTick { get; }
        public int LogicTickCount => checked((int)(RelativeEndLocalLogicTick - RelativeStartLocalLogicTick));
        public float ScaledDeltaSeconds { get; }
        public float UnscaledDeltaSeconds { get; }
        public float PresentationDeltaSeconds { get; }
        public float InterpolationAlpha { get; }
        public GameplayPresentationDebugClockMode PresentationClockMode { get; }

        static bool FiniteNonNegative(float value) =>
            float.IsFinite(value) && value >= 0f;
    }

    public readonly struct GameplayPresentationScheduleFrame
    {
        public GameplayPresentationScheduleFrame(
            GameplayTickDriveMode driveMode,
            int frameIndex,
            ulong renderFrame,
            ulong startLocalLogicTick,
            ulong endLocalLogicTick,
            ulong relativeStartLocalLogicTick,
            ulong relativeEndLocalLogicTick,
            float scaledDeltaSeconds,
            float unscaledDeltaSeconds,
            float presentationDeltaSeconds,
            float interpolationAlpha,
            GameplayPresentationDebugClockMode presentationClockMode)
        {
            if (driveMode != GameplayTickDriveMode.LivePresentationScheduleCapture &&
                driveMode != GameplayTickDriveMode.ScriptedPresentationFrame)
            {
                throw new ArgumentOutOfRangeException(nameof(driveMode));
            }
            if (frameIndex < 0 || endLocalLogicTick < startLocalLogicTick ||
                relativeEndLocalLogicTick < relativeStartLocalLogicTick ||
                endLocalLogicTick - startLocalLogicTick !=
                relativeEndLocalLogicTick - relativeStartLocalLogicTick ||
                endLocalLogicTick - startLocalLogicTick > int.MaxValue ||
                !float.IsFinite(scaledDeltaSeconds) || scaledDeltaSeconds < 0f ||
                !float.IsFinite(unscaledDeltaSeconds) || unscaledDeltaSeconds < 0f ||
                !float.IsFinite(presentationDeltaSeconds) || presentationDeltaSeconds < 0f ||
                !float.IsFinite(interpolationAlpha) || interpolationAlpha < 0f ||
                interpolationAlpha > 1f ||
                !Enum.IsDefined(typeof(GameplayPresentationDebugClockMode), presentationClockMode))
            {
                throw new ArgumentException(
                    "Presentation Schedule Frame is invalid.");
            }
            DriveMode = driveMode;
            FrameIndex = frameIndex;
            RenderFrame = renderFrame;
            StartLocalLogicTick = startLocalLogicTick;
            EndLocalLogicTick = endLocalLogicTick;
            RelativeStartLocalLogicTick = relativeStartLocalLogicTick;
            RelativeEndLocalLogicTick = relativeEndLocalLogicTick;
            ScaledDeltaSeconds = scaledDeltaSeconds;
            UnscaledDeltaSeconds = unscaledDeltaSeconds;
            PresentationDeltaSeconds = presentationDeltaSeconds;
            InterpolationAlpha = interpolationAlpha;
            PresentationClockMode = presentationClockMode;
        }

        public GameplayTickDriveMode DriveMode { get; }
        public int FrameIndex { get; }
        public ulong RenderFrame { get; }
        public ulong StartLocalLogicTick { get; }
        public ulong EndLocalLogicTick { get; }
        public int LogicTickCount => checked((int)(EndLocalLogicTick - StartLocalLogicTick));
        public ulong RelativeStartLocalLogicTick { get; }
        public ulong RelativeEndLocalLogicTick { get; }
        public float ScaledDeltaSeconds { get; }
        public float UnscaledDeltaSeconds { get; }
        public float PresentationDeltaSeconds { get; }
        public float InterpolationAlpha { get; }
        public GameplayPresentationDebugClockMode PresentationClockMode { get; }
    }

    public readonly struct GameplayTickDriveCommand
    {
        GameplayTickDriveCommand(
            GameplayTickDriveCommandKind kind,
            ulong stepCount,
            float rateMultiplier,
            GameplayPresentationDebugClockMode presentationClockMode,
            GameplayScriptedPresentationFrame scriptedPresentationFrame,
            ulong sequence)
        {
            Kind = kind;
            StepCount = stepCount;
            RateMultiplier = rateMultiplier;
            PresentationClockMode = presentationClockMode;
            ScriptedPresentationFrame = scriptedPresentationFrame;
            Sequence = sequence;
        }

        public GameplayTickDriveCommandKind Kind { get; }
        public ulong StepCount { get; }
        public float RateMultiplier { get; }
        public GameplayPresentationDebugClockMode PresentationClockMode { get; }
        public GameplayScriptedPresentationFrame ScriptedPresentationFrame { get; }
        public ulong Sequence { get; }

        public static GameplayTickDriveCommand SetRealtime()
        {
            return new GameplayTickDriveCommand(
                GameplayTickDriveCommandKind.SetRealtime,
                0,
                1f,
                GameplayPresentationDebugClockMode.LivePresentation,
                default,
                0);
        }

        public static GameplayTickDriveCommand Pause()
        {
            return new GameplayTickDriveCommand(
                GameplayTickDriveCommandKind.Pause,
                0,
                1f,
                GameplayPresentationDebugClockMode.LivePresentation,
                default,
                0);
        }

        public static GameplayTickDriveCommand Step(ulong stepCount)
        {
            return new GameplayTickDriveCommand(
                GameplayTickDriveCommandKind.Step,
                Math.Max(1UL, stepCount),
                1f,
                GameplayPresentationDebugClockMode.LivePresentation,
                default,
                0);
        }

        public static GameplayTickDriveCommand SetRatePlayback(float rateMultiplier)
        {
            return new GameplayTickDriveCommand(
                GameplayTickDriveCommandKind.SetRatePlayback,
                0,
                Math.Max(0.01f, rateMultiplier),
                GameplayPresentationDebugClockMode.LivePresentation,
                default,
                0);
        }

        public static GameplayTickDriveCommand SetPresentationClock(GameplayPresentationDebugClockMode mode)
        {
            return new GameplayTickDriveCommand(
                GameplayTickDriveCommandKind.SetPresentationClock,
                0,
                1f,
                mode,
                default,
                0);
        }

        public static GameplayTickDriveCommand BeginLivePresentationScheduleCapture() =>
            new GameplayTickDriveCommand(
                GameplayTickDriveCommandKind.BeginLivePresentationScheduleCapture,
                0,
                1f,
                GameplayPresentationDebugClockMode.LivePresentation,
                default,
                0);

        public static GameplayTickDriveCommand BeginScriptedPresentationSchedule() =>
            new GameplayTickDriveCommand(
                GameplayTickDriveCommandKind.BeginScriptedPresentationSchedule,
                0,
                1f,
                GameplayPresentationDebugClockMode.LivePresentation,
                default,
                0);

        public static GameplayTickDriveCommand ScriptedFrame(
            GameplayScriptedPresentationFrame frame) =>
            new GameplayTickDriveCommand(
                GameplayTickDriveCommandKind.ScriptedPresentationFrame,
                0,
                1f,
                frame.PresentationClockMode,
                frame,
                0);

        public static GameplayTickDriveCommand EndPresentationSchedule() =>
            new GameplayTickDriveCommand(
                GameplayTickDriveCommandKind.EndPresentationSchedule,
                0,
                1f,
                GameplayPresentationDebugClockMode.LivePresentation,
                default,
                0);

        public static GameplayTickDriveCommand CancelPresentationSchedule() =>
            new GameplayTickDriveCommand(
                GameplayTickDriveCommandKind.CancelPresentationSchedule,
                0,
                1f,
                GameplayPresentationDebugClockMode.LivePresentation,
                default,
                0);

        public GameplayTickDriveCommand WithSequence(ulong sequence)
        {
            return new GameplayTickDriveCommand(
                Kind,
                StepCount,
                RateMultiplier,
                PresentationClockMode,
                ScriptedPresentationFrame,
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
            float presentationDeltaSeconds,
            bool presentationScheduleDriveActive,
            int activePresentationScheduleFrameIndex)
        {
            Policy = policy;
            RenderFrame = renderFrame;
            LocalLogicTick = localLogicTick;
            InterpolationAlpha = interpolationAlpha;
            DroppedLocalLogicTicks = droppedLocalLogicTicks;
            PendingCommandCount = pendingCommandCount;
            LastCommandSequence = lastCommandSequence;
            PresentationDeltaSeconds = presentationDeltaSeconds;
            PresentationScheduleDriveActive = presentationScheduleDriveActive;
            ActivePresentationScheduleFrameIndex =
                activePresentationScheduleFrameIndex;
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
        public bool PresentationScheduleDriveActive { get; }
        public int ActivePresentationScheduleFrameIndex { get; }
    }
}
