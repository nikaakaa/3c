using UnityEngine;

namespace ThirdPersonGameplay.Tick
{
    public static class GameplayTickDebugHotkeys
    {
        public const ulong MultiStepCount = 8;
        public const float SlowPlaybackRate = 0.25f;

        public static void Pump()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!GameplayTickSystem.IsInitialized)
                return;
            if (Input.GetKeyDown(KeyCode.F5))
                TogglePause();
            if (Input.GetKeyDown(KeyCode.F6))
                Step(1);
            if (Input.GetKeyDown(KeyCode.F7))
                Step(MultiStepCount);
            if (Input.GetKeyDown(KeyCode.F8))
                ResumeLive();
            if (Input.GetKeyDown(KeyCode.O))
                ToggleSlowPlayback();
#endif
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        static void TogglePause()
        {
            GameplayTickDriveStatusSnapshot status = GameplayTickSystem.Current.DriveStatus;
            if (status.Mode == GameplayTickDriveMode.Paused ||
                status.Mode == GameplayTickDriveMode.ManualStep)
            {
                ResumeLive();
                return;
            }
            GameplayTickSystem.EnqueueDriveCommand(
                GameplayTickDriveCommand.SetPresentationClock(GameplayPresentationDebugClockMode.LogicLockedPresentation));
            GameplayTickSystem.EnqueueDriveCommand(GameplayTickDriveCommand.Pause());
        }

        static void Step(ulong count)
        {
            GameplayTickSystem.EnqueueDriveCommand(
                GameplayTickDriveCommand.SetPresentationClock(GameplayPresentationDebugClockMode.LogicLockedPresentation));
            GameplayTickSystem.EnqueueDriveCommand(GameplayTickDriveCommand.Step(count));
        }

        static void ResumeLive()
        {
            GameplayTickSystem.EnqueueDriveCommand(
                GameplayTickDriveCommand.SetPresentationClock(GameplayPresentationDebugClockMode.LivePresentation));
            GameplayTickSystem.EnqueueDriveCommand(GameplayTickDriveCommand.SetRealtime());
        }

        static void ToggleSlowPlayback()
        {
            GameplayTickDriveStatusSnapshot status = GameplayTickSystem.Current.DriveStatus;
            if (status.PresentationScheduleDriveActive)
            {
                float scheduleRate = Mathf.Approximately(
                    status.RateMultiplier,
                    SlowPlaybackRate)
                    ? 1f
                    : SlowPlaybackRate;
                GameplayTickSystem.EnqueueDriveCommand(
                    GameplayTickDriveCommand.SetRatePlayback(scheduleRate));
                return;
            }
            bool slowPlaybackActive = status.Mode == GameplayTickDriveMode.RatePlayback &&
                Mathf.Approximately(status.RateMultiplier, SlowPlaybackRate);
            if (slowPlaybackActive)
            {
                ResumeLive();
                return;
            }

            GameplayTickSystem.EnqueueDriveCommand(
                GameplayTickDriveCommand.SetPresentationClock(GameplayPresentationDebugClockMode.LogicLockedPresentation));
            GameplayTickSystem.EnqueueDriveCommand(
                GameplayTickDriveCommand.SetRatePlayback(SlowPlaybackRate));
        }
#endif
    }
}
