using System;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;

namespace ThirdPersonGameplay.Tick
{
    public interface IGameplayRenderFrameInputTarget
    {
        void BeginRenderFrame(ulong renderFrame);
    }

    public interface IGameplayLogicTickTarget
    {
        void LogicTick(GameplayLogicTickContext context);
    }

    public interface IGameplayPresentationFrameTarget
    {
        void PresentationFrame(GameplayPresentationFrameContext context);
    }

    public interface IGameplayPresentationScheduleFrameTarget
    {
        void PresentationScheduleFrame(GameplayPresentationScheduleFrame frame);
    }

    public interface IGameplayTickHook
    {
        IGameplayLogicTickTarget Target { get; }
        void BeforeLogicTick(GameplayLogicTickContext context);
        void AfterLogicTick(GameplayLogicTickContext context);
    }

    public sealed class GameplayTickSystem : IDisposable
    {
        static GameplayTickSystem s_Current;
        static readonly ProfilerMarker InputMarker = new ProfilerMarker("ThirdPerson.Gameplay.Input");
        static readonly ProfilerMarker LogicMarker = new ProfilerMarker("ThirdPerson.Gameplay.Logic");
        static readonly ProfilerMarker PresentationMarker = new ProfilerMarker("ThirdPerson.Gameplay.Presentation");

        readonly List<IGameplayRenderFrameInputTarget> m_InputTargets = new List<IGameplayRenderFrameInputTarget>();
        readonly List<IGameplayLogicTickTarget> m_LogicTargets = new List<IGameplayLogicTickTarget>();
        readonly List<IGameplayPresentationFrameTarget> m_PresentationTargets = new List<IGameplayPresentationFrameTarget>();
        readonly List<IGameplayPresentationScheduleFrameTarget> m_PresentationScheduleTargets =
            new List<IGameplayPresentationScheduleFrameTarget>();
        readonly List<IGameplayTickHook> m_TickHooks = new List<IGameplayTickHook>();
        readonly Queue<GameplayTickDriveCommand> m_DriveCommands = new Queue<GameplayTickDriveCommand>();
        readonly GameplayTickSettings m_Settings;

        GameplayTickDrivePolicy m_DrivePolicy = GameplayTickDrivePolicy.Default;
        float m_AccumulatorSeconds;
        float m_InterpolationAlpha;
        float m_LastScaledDeltaSeconds;
        float m_LastUnscaledDeltaSeconds;
        float m_LastPresentationDeltaSeconds;
        float m_ScriptedPresentationFrameAccumulator;
        GameplayTickDrivePolicy m_SavedScheduleDrivePolicy;
        GameplayScriptedPresentationFrame m_ScriptedPresentationFrame;
        float m_SavedScheduleAccumulatorSeconds;
        ulong m_PresentationScheduleBaseLocalLogicTick;
        ulong m_FrameStartLocalLogicTick;
        int m_NextPresentationScheduleFrameIndex;
        int m_ActivePresentationScheduleFrameIndex = -1;
        GameplayTickDriveMode m_ActivePresentationScheduleFrameMode;
        ulong m_NextDriveCommandSequence;
        ulong m_LastDriveCommandSequence;
        bool m_PresentationScheduleDriveActive;
        bool m_HasScriptedPresentationFrame;
        bool m_PresentationFrameAdvanced;
        bool m_FrameLogicActive;
        bool m_StopCurrentFrameLogicTicks;
        bool m_Disposed;

        public GameplayTickSystem(GameplayTickSettings settings)
        {
            m_Settings = settings;
        }

        public static bool IsInitialized => s_Current != null;
        public static GameplayTickSystem Current => s_Current;

        public ulong RenderFrame { get; private set; }
        public ulong LocalLogicTick { get; private set; }
        public int DroppedLocalLogicTicks { get; private set; }
        public float InterpolationAlpha => m_InterpolationAlpha;
        public GameplayTickSettings Settings => m_Settings;
        public GameplayTickDriveStatusSnapshot DriveStatus => CreateDriveStatusSnapshot();

        public static void Initialize(GameplayTickSettings settings)
        {
            if (s_Current != null)
                return;

            s_Current = new GameplayTickSystem(settings);
        }

        public static void Shutdown()
        {
            s_Current?.Dispose();
            s_Current = null;
        }

        public static bool RegisterInputTarget(IGameplayRenderFrameInputTarget target)
        {
            if (s_Current == null)
            {
                Debug.LogError("GameplayTickSystem is not initialized.");
                return false;
            }

            s_Current.Register(target);
            return true;
        }

        public static void UnregisterInputTarget(IGameplayRenderFrameInputTarget target)
        {
            s_Current?.Unregister(target);
        }

        public static bool RegisterLogicTarget(IGameplayLogicTickTarget target)
        {
            if (s_Current == null)
            {
                Debug.LogError("GameplayTickSystem is not initialized.");
                return false;
            }

            s_Current.Register(target);
            return true;
        }

        public static void UnregisterLogicTarget(IGameplayLogicTickTarget target)
        {
            s_Current?.Unregister(target);
        }

        public static bool RegisterPresentationTarget(IGameplayPresentationFrameTarget target)
        {
            if (s_Current == null)
            {
                Debug.LogError("GameplayTickSystem is not initialized.");
                return false;
            }

            s_Current.Register(target);
            return true;
        }

        public static void UnregisterPresentationTarget(IGameplayPresentationFrameTarget target)
        {
            s_Current?.Unregister(target);
        }

        public static bool RegisterPresentationScheduleTarget(
            IGameplayPresentationScheduleFrameTarget target)
        {
            if (s_Current == null)
            {
                Debug.LogError("GameplayTickSystem is not initialized.");
                return false;
            }
            s_Current.Register(target);
            return true;
        }

        public static void UnregisterPresentationScheduleTarget(
            IGameplayPresentationScheduleFrameTarget target)
        {
            s_Current?.Unregister(target);
        }

        public static bool RequestCurrentFrameLogicStop() =>
            s_Current != null && s_Current.RequestLogicStop();

        public static bool RegisterTickHook(IGameplayTickHook hook)
        {
            if (s_Current == null)
            {
                Debug.LogError("GameplayTickSystem is not initialized.");
                return false;
            }

            s_Current.Register(hook);
            return true;
        }

        public static void UnregisterTickHook(IGameplayTickHook hook)
        {
            s_Current?.Unregister(hook);
        }

        public static bool EnqueueDriveCommand(GameplayTickDriveCommand command)
        {
            if (s_Current == null)
            {
                Debug.LogError("GameplayTickSystem is not initialized.");
                return false;
            }

            s_Current.Enqueue(command);
            return true;
        }

        public void Register(IGameplayRenderFrameInputTarget target)
        {
            if (m_Disposed || target == null || m_InputTargets.Contains(target))
                return;

            m_InputTargets.Add(target);
        }

        public void Register(IGameplayLogicTickTarget target)
        {
            if (m_Disposed || target == null || m_LogicTargets.Contains(target))
                return;

            m_LogicTargets.Add(target);
        }

        public void Register(IGameplayPresentationFrameTarget target)
        {
            if (m_Disposed || target == null || m_PresentationTargets.Contains(target))
                return;

            m_PresentationTargets.Add(target);
        }

        public void Register(IGameplayPresentationScheduleFrameTarget target)
        {
            if (m_Disposed || target == null || m_PresentationScheduleTargets.Contains(target))
                return;
            m_PresentationScheduleTargets.Add(target);
        }

        public void Register(IGameplayTickHook hook)
        {
            if (m_Disposed || hook == null || hook.Target == null || m_TickHooks.Contains(hook))
                return;

            m_TickHooks.Add(hook);
        }

        public void Unregister(IGameplayRenderFrameInputTarget target)
        {
            if (target == null)
                return;

            m_InputTargets.Remove(target);
        }

        public void Unregister(IGameplayLogicTickTarget target)
        {
            if (target == null)
                return;

            m_LogicTargets.Remove(target);
        }

        public void Unregister(IGameplayPresentationFrameTarget target)
        {
            if (target == null)
                return;

            m_PresentationTargets.Remove(target);
        }

        public void Unregister(IGameplayPresentationScheduleFrameTarget target)
        {
            if (target == null)
                return;
            m_PresentationScheduleTargets.Remove(target);
        }

        public void Unregister(IGameplayTickHook hook)
        {
            if (hook == null)
                return;

            m_TickHooks.Remove(hook);
        }

        public void FrameUpdate(float scaledDeltaSeconds, float unscaledDeltaSeconds)
        {
            if (m_Disposed)
                return;

            m_PresentationFrameAdvanced = false;
            ProcessDriveCommands();
            bool scripted = m_DrivePolicy.Mode ==
                GameplayTickDriveMode.ScriptedPresentationFrame;
            if (scripted && !AdmitScriptedPresentationFrame())
                return;
            m_PresentationFrameAdvanced = true;
            RenderFrame++;
            m_LastScaledDeltaSeconds = Math.Max(0f, scaledDeltaSeconds);
            m_LastUnscaledDeltaSeconds = Math.Max(0f, unscaledDeltaSeconds);
            m_FrameStartLocalLogicTick = LocalLogicTick;
            m_StopCurrentFrameLogicTicks = false;
            float fixedDeltaSeconds = m_Settings.FixedDeltaSeconds;
            if (scripted)
                PrepareScriptedPresentationFrame();
            int advancedLogicTicks = 0;
            AdmitAccumulatorDelta();
            using (InputMarker.Auto())
                BeginTargetRenderFrame(RenderFrame);

            m_FrameLogicActive = true;
            try
            {
                if (m_DrivePolicy.Mode == GameplayTickDriveMode.ManualStep)
                {
                    advancedLogicTicks = RunManualLogicTicks(fixedDeltaSeconds);
                }
                else if (scripted)
                {
                    advancedLogicTicks =
                        RunScriptedLogicTicks(fixedDeltaSeconds);
                }
                else
                {
                    advancedLogicTicks =
                        RunAccumulatorLogicTicks(fixedDeltaSeconds);
                    if (!m_StopCurrentFrameLogicTicks)
                        ApplyAccumulatorOverflow(fixedDeltaSeconds);
                }
            }
            finally
            {
                m_FrameLogicActive = false;
            }

            if (scripted)
            {
                CompleteScriptedPresentationFrame();
            }
            else
            {
                m_InterpolationAlpha =
                    m_DrivePolicy.Mode == GameplayTickDriveMode.ManualStep ||
                    m_DrivePolicy.Mode == GameplayTickDriveMode.Paused
                        ? 0f
                        : fixedDeltaSeconds > 0f
                        ? Mathf.Clamp01(
                            m_AccumulatorSeconds / fixedDeltaSeconds)
                        : 0f;
                m_LastPresentationDeltaSeconds =
                    CalculatePresentationDeltaSeconds(
                        fixedDeltaSeconds,
                        advancedLogicTicks);
            }
        }

        public void FrameLateUpdate()
        {
            if (m_Disposed || !m_PresentationFrameAdvanced)
                return;

            using (PresentationMarker.Auto())
            {
                var context = new GameplayPresentationFrameContext(
                    m_LastScaledDeltaSeconds,
                    m_LastUnscaledDeltaSeconds,
                    m_LastPresentationDeltaSeconds,
                    m_DrivePolicy.PresentationClockMode,
                    RenderFrame,
                    LocalLogicTick,
                    m_InterpolationAlpha);
                for (int i = 0; i < m_PresentationTargets.Count; i++)
                {
                    IGameplayPresentationFrameTarget target =
                        m_PresentationTargets[i];
                    if (target != null)
                        target.PresentationFrame(context);
                }
                PublishPresentationScheduleFrame();
            }
        }

        public void Dispose()
        {
            m_InputTargets.Clear();
            m_LogicTargets.Clear();
            m_PresentationTargets.Clear();
            m_PresentationScheduleTargets.Clear();
            m_TickHooks.Clear();
            m_DriveCommands.Clear();
            m_Disposed = true;
        }

        public void Enqueue(GameplayTickDriveCommand command)
        {
            if (m_Disposed)
                return;

            m_NextDriveCommandSequence++;
            m_DriveCommands.Enqueue(command.WithSequence(m_NextDriveCommandSequence));
        }

        GameplayTickDriveStatusSnapshot CreateDriveStatusSnapshot()
        {
            return new GameplayTickDriveStatusSnapshot(
                m_DrivePolicy,
                RenderFrame,
                LocalLogicTick,
                m_InterpolationAlpha,
                DroppedLocalLogicTicks,
                (ulong)m_DriveCommands.Count,
                m_LastDriveCommandSequence,
                m_LastPresentationDeltaSeconds,
                m_PresentationScheduleDriveActive,
                m_ActivePresentationScheduleFrameIndex);
        }

        void ProcessDriveCommands()
        {
            while (m_DriveCommands.Count > 0)
            {
                GameplayTickDriveCommand command = m_DriveCommands.Dequeue();
                m_LastDriveCommandSequence = command.Sequence;
                if (m_PresentationScheduleDriveActive &&
                    command.Kind != GameplayTickDriveCommandKind.ScriptedPresentationFrame &&
                    command.Kind != GameplayTickDriveCommandKind.EndPresentationSchedule &&
                    command.Kind != GameplayTickDriveCommandKind.CancelPresentationSchedule &&
                    command.Kind != GameplayTickDriveCommandKind.SetRatePlayback)
                {
                    throw new InvalidOperationException(
                        "Presentation Schedule drive owns the Gameplay Tick System.");
                }

                switch (command.Kind)
                {
                    case GameplayTickDriveCommandKind.SetRealtime:
                        m_DrivePolicy = new GameplayTickDrivePolicy(
                            GameplayTickDriveMode.Realtime,
                            m_DrivePolicy.PresentationClockMode,
                            1f,
                            0);
                        m_AccumulatorSeconds = 0f;
                        break;
                    case GameplayTickDriveCommandKind.Pause:
                        m_DrivePolicy = m_DrivePolicy.WithMode(
                            GameplayTickDriveMode.Paused,
                            0);
                        m_AccumulatorSeconds = 0f;
                        break;
                    case GameplayTickDriveCommandKind.Step:
                        m_DrivePolicy = new GameplayTickDrivePolicy(
                            GameplayTickDriveMode.ManualStep,
                            m_DrivePolicy.PresentationClockMode,
                            m_DrivePolicy.RateMultiplier,
                            m_DrivePolicy.QueuedManualTicks +
                            Math.Max(1UL, command.StepCount));
                        m_AccumulatorSeconds = 0f;
                        break;
                    case GameplayTickDriveCommandKind.SetRatePlayback:
                        SetRatePlayback(command.RateMultiplier);
                        break;
                    case GameplayTickDriveCommandKind.SetPresentationClock:
                        m_DrivePolicy = m_DrivePolicy.WithPresentationClock(
                            command.PresentationClockMode);
                        break;
                    case GameplayTickDriveCommandKind.BeginLivePresentationScheduleCapture:
                        BeginPresentationScheduleDrive(
                            GameplayTickDriveMode.LivePresentationScheduleCapture);
                        break;
                    case GameplayTickDriveCommandKind.BeginScriptedPresentationSchedule:
                        BeginPresentationScheduleDrive(
                            GameplayTickDriveMode.ScriptedPresentationFrame);
                        break;
                    case GameplayTickDriveCommandKind.ScriptedPresentationFrame:
                        QueueScriptedPresentationFrame(
                            command.ScriptedPresentationFrame);
                        break;
                    case GameplayTickDriveCommandKind.EndPresentationSchedule:
                        EndPresentationScheduleDrive();
                        break;
                    case GameplayTickDriveCommandKind.CancelPresentationSchedule:
                        CancelPresentationScheduleDrive();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        void AdmitAccumulatorDelta()
        {
            switch (m_DrivePolicy.Mode)
            {
                case GameplayTickDriveMode.Realtime:
                case GameplayTickDriveMode.LivePresentationScheduleCapture:
                    m_AccumulatorSeconds += SelectAccumulatorDeltaSeconds();
                    break;
                case GameplayTickDriveMode.RatePlayback:
                    m_AccumulatorSeconds +=
                        SelectAccumulatorDeltaSeconds() *
                        m_DrivePolicy.RateMultiplier;
                    break;
                case GameplayTickDriveMode.Paused:
                case GameplayTickDriveMode.ManualStep:
                case GameplayTickDriveMode.ScriptedPresentationFrame:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        int RunManualLogicTicks(float fixedDeltaSeconds)
        {
            int tickBudget = Math.Max(0, m_Settings.MaxCatchUpTicks);
            ulong queuedTicks = m_DrivePolicy.QueuedManualTicks;
            int ticksToRun = (int)Math.Min((ulong)tickBudget, queuedTicks);
            int advancedTicks = 0;

            for (int i = 0; i < ticksToRun; i++)
            {
                AdvanceLogicTick(fixedDeltaSeconds);
                advancedTicks++;
            }

            ulong remainingTicks = queuedTicks - (ulong)advancedTicks;
            m_DrivePolicy = remainingTicks > 0
                ? m_DrivePolicy.WithMode(GameplayTickDriveMode.ManualStep, remainingTicks)
                : m_DrivePolicy.WithMode(GameplayTickDriveMode.Paused, 0);
            m_AccumulatorSeconds = 0f;
            return advancedTicks;
        }

        int RunAccumulatorLogicTicks(float fixedDeltaSeconds)
        {
            int catchUpTicks = 0;
            while (m_AccumulatorSeconds + float.Epsilon >= fixedDeltaSeconds &&
                   catchUpTicks < m_Settings.MaxCatchUpTicks)
            {
                AdvanceLogicTick(fixedDeltaSeconds);
                m_AccumulatorSeconds -= fixedDeltaSeconds;
                catchUpTicks++;
                if (m_StopCurrentFrameLogicTicks)
                    break;
            }
            return catchUpTicks;
        }

        int RunScriptedLogicTicks(float fixedDeltaSeconds)
        {
            int tickCount = m_ScriptedPresentationFrame.LogicTickCount;
            if (tickCount > m_Settings.MaxCatchUpTicks)
            {
                throw new InvalidOperationException(
                    "Scripted Presentation Frame exceeds the formal Logic Tick budget.");
            }
            for (int i = 0; i < tickCount; i++)
                AdvanceLogicTick(fixedDeltaSeconds);
            return tickCount;
        }

        void ApplyAccumulatorOverflow(float fixedDeltaSeconds)
        {
            if (m_AccumulatorSeconds >= fixedDeltaSeconds &&
                m_Settings.OverflowPolicy == GameplayAccumulatorOverflowPolicy.DropRemainder)
            {
                DroppedLocalLogicTicks += (int)(m_AccumulatorSeconds / fixedDeltaSeconds);
                m_AccumulatorSeconds = 0f;
            }
        }

        float CalculatePresentationDeltaSeconds(float fixedDeltaSeconds, int advancedLogicTicks)
        {
            if (m_DrivePolicy.PresentationClockMode == GameplayPresentationDebugClockMode.LivePresentation)
                return m_LastScaledDeltaSeconds;

            if (m_DrivePolicy.Mode == GameplayTickDriveMode.RatePlayback)
                return m_LastScaledDeltaSeconds * m_DrivePolicy.RateMultiplier;

            if (advancedLogicTicks > 0)
                return fixedDeltaSeconds;

            if (m_DrivePolicy.Mode == GameplayTickDriveMode.Paused ||
                m_DrivePolicy.Mode == GameplayTickDriveMode.ManualStep)
            {
                return 0f;
            }

            return m_LastScaledDeltaSeconds;
        }

        void BeginPresentationScheduleDrive(GameplayTickDriveMode mode)
        {
            if (m_PresentationScheduleDriveActive ||
                m_DrivePolicy.Mode != GameplayTickDriveMode.Realtime ||
                m_DrivePolicy.PresentationClockMode !=
                GameplayPresentationDebugClockMode.LivePresentation)
            {
                throw new InvalidOperationException(
                    "Presentation Schedule drive requires the canonical realtime Gameplay Tick policy.");
            }
            m_SavedScheduleDrivePolicy = m_DrivePolicy;
            m_SavedScheduleAccumulatorSeconds = m_AccumulatorSeconds;
            m_PresentationScheduleDriveActive = true;
            m_PresentationScheduleBaseLocalLogicTick = LocalLogicTick;
            m_NextPresentationScheduleFrameIndex = 0;
            m_ActivePresentationScheduleFrameIndex = -1;
            m_HasScriptedPresentationFrame = false;
            m_DrivePolicy = new GameplayTickDrivePolicy(
                mode,
                GameplayPresentationDebugClockMode.LivePresentation,
                1f,
                0);
            if (mode == GameplayTickDriveMode.ScriptedPresentationFrame)
                m_AccumulatorSeconds = 0f;
            m_ScriptedPresentationFrameAccumulator = 0f;
        }

        void SetRatePlayback(float rateMultiplier)
        {
            if (!m_PresentationScheduleDriveActive)
            {
                m_DrivePolicy = m_DrivePolicy.WithRatePlayback(rateMultiplier);
                m_AccumulatorSeconds = 0f;
                return;
            }
            if (m_DrivePolicy.Mode != GameplayTickDriveMode.ScriptedPresentationFrame)
                throw new InvalidOperationException(
                    "Playback rate is only available for scripted Presentation Schedule replay.");
            if (rateMultiplier > 1f)
                throw new InvalidOperationException(
                    "Scripted Presentation Schedule replay rate cannot exceed 1x.");
            m_DrivePolicy = m_DrivePolicy.WithRateMultiplier(rateMultiplier);
            m_ScriptedPresentationFrameAccumulator = 0f;
        }

        bool AdmitScriptedPresentationFrame()
        {
            float rateMultiplier = m_DrivePolicy.RateMultiplier;
            if (rateMultiplier >= 1f)
            {
                m_ScriptedPresentationFrameAccumulator = 0f;
                return true;
            }
            m_ScriptedPresentationFrameAccumulator += rateMultiplier;
            if (m_ScriptedPresentationFrameAccumulator + float.Epsilon < 1f)
                return false;
            m_ScriptedPresentationFrameAccumulator = Math.Max(
                0f,
                m_ScriptedPresentationFrameAccumulator - 1f);
            return true;
        }

        void QueueScriptedPresentationFrame(
            GameplayScriptedPresentationFrame frame)
        {
            if (!m_PresentationScheduleDriveActive ||
                m_DrivePolicy.Mode !=
                GameplayTickDriveMode.ScriptedPresentationFrame ||
                m_HasScriptedPresentationFrame ||
                frame.FrameIndex != m_NextPresentationScheduleFrameIndex)
            {
                throw new InvalidOperationException(
                    "Scripted Presentation Frame sequence is invalid.");
            }
            m_ScriptedPresentationFrame = frame;
            m_HasScriptedPresentationFrame = true;
        }

        void EndPresentationScheduleDrive()
        {
            if (!m_PresentationScheduleDriveActive)
                throw new InvalidOperationException(
                    "Presentation Schedule drive is not active.");
            if (m_HasScriptedPresentationFrame)
                throw new InvalidOperationException(
                    "Presentation Schedule cannot end with an unconsumed scripted frame.");
            RestorePresentationScheduleDrive();
        }

        void CancelPresentationScheduleDrive()
        {
            if (!m_PresentationScheduleDriveActive)
                return;
            RestorePresentationScheduleDrive();
        }

        void RestorePresentationScheduleDrive()
        {
            m_DrivePolicy = m_SavedScheduleDrivePolicy;
            m_AccumulatorSeconds = m_SavedScheduleAccumulatorSeconds;
            m_PresentationScheduleDriveActive = false;
            m_HasScriptedPresentationFrame = false;
            m_ScriptedPresentationFrame = default;
            m_ActivePresentationScheduleFrameIndex = -1;
            m_ActivePresentationScheduleFrameMode = default;
            m_NextPresentationScheduleFrameIndex = 0;
            m_PresentationScheduleBaseLocalLogicTick = 0;
            m_SavedScheduleDrivePolicy = default;
            m_SavedScheduleAccumulatorSeconds = 0f;
            m_ScriptedPresentationFrameAccumulator = 0f;
        }

        void PrepareScriptedPresentationFrame()
        {
            if (!m_PresentationScheduleDriveActive ||
                !m_HasScriptedPresentationFrame)
            {
                throw new InvalidOperationException(
                    "Gameplay Tick System received no Scripted Presentation Frame.");
            }
            GameplayScriptedPresentationFrame frame =
                m_ScriptedPresentationFrame;
            ulong relativeStart = LocalLogicTick -
                m_PresentationScheduleBaseLocalLogicTick;
            if (frame.RelativeStartLocalLogicTick != relativeStart)
            {
                throw new InvalidOperationException(
                    "Scripted Presentation Frame starts at the wrong relative Logic Tick.");
            }
            m_LastScaledDeltaSeconds = frame.ScaledDeltaSeconds;
            m_LastUnscaledDeltaSeconds = frame.UnscaledDeltaSeconds;
            m_DrivePolicy = m_DrivePolicy.WithPresentationClock(
                frame.PresentationClockMode);
            m_ActivePresentationScheduleFrameIndex = frame.FrameIndex;
            m_ActivePresentationScheduleFrameMode =
                GameplayTickDriveMode.ScriptedPresentationFrame;
        }

        void CompleteScriptedPresentationFrame()
        {
            GameplayScriptedPresentationFrame frame =
                m_ScriptedPresentationFrame;
            ulong relativeEnd = LocalLogicTick -
                m_PresentationScheduleBaseLocalLogicTick;
            if (frame.RelativeEndLocalLogicTick != relativeEnd)
            {
                throw new InvalidOperationException(
                    "Scripted Presentation Frame ended at the wrong relative Logic Tick.");
            }
            m_InterpolationAlpha = frame.InterpolationAlpha;
            m_LastPresentationDeltaSeconds =
                frame.PresentationDeltaSeconds;
            m_HasScriptedPresentationFrame = false;
            m_NextPresentationScheduleFrameIndex++;
        }

        bool RequestLogicStop()
        {
            if (!m_FrameLogicActive ||
                m_DrivePolicy.Mode !=
                GameplayTickDriveMode.LivePresentationScheduleCapture)
            {
                return false;
            }
            m_StopCurrentFrameLogicTicks = true;
            return true;
        }

        void PublishPresentationScheduleFrame()
        {
            if (!m_PresentationScheduleDriveActive)
                return;
            if (m_DrivePolicy.Mode ==
                GameplayTickDriveMode.LivePresentationScheduleCapture)
            {
                m_ActivePresentationScheduleFrameIndex =
                    m_NextPresentationScheduleFrameIndex++;
                m_ActivePresentationScheduleFrameMode =
                    GameplayTickDriveMode.LivePresentationScheduleCapture;
            }
            if (m_ActivePresentationScheduleFrameIndex < 0)
                return;
            var frame = new GameplayPresentationScheduleFrame(
                m_ActivePresentationScheduleFrameMode,
                m_ActivePresentationScheduleFrameIndex,
                RenderFrame,
                m_FrameStartLocalLogicTick,
                LocalLogicTick,
                m_FrameStartLocalLogicTick -
                m_PresentationScheduleBaseLocalLogicTick,
                LocalLogicTick -
                m_PresentationScheduleBaseLocalLogicTick,
                m_LastScaledDeltaSeconds,
                m_LastUnscaledDeltaSeconds,
                m_LastPresentationDeltaSeconds,
                m_InterpolationAlpha,
                m_DrivePolicy.PresentationClockMode);
            for (int i = 0; i < m_PresentationScheduleTargets.Count; i++)
                m_PresentationScheduleTargets[i]?.PresentationScheduleFrame(frame);
            if (m_ActivePresentationScheduleFrameMode ==
                GameplayTickDriveMode.ScriptedPresentationFrame)
            {
                m_ActivePresentationScheduleFrameIndex = -1;
            }
        }

        void TickTargets(float fixedDeltaSeconds)
        {
            for (int i = 0; i < m_LogicTargets.Count; i++)
            {
                IGameplayLogicTickTarget target = m_LogicTargets[i];
                if (target == null)
                    continue;

                var context = new GameplayLogicTickContext(
                    fixedDeltaSeconds,
                    RenderFrame,
                    LocalLogicTick,
                    0);
                BeforeTargetLogicTick(target, context);
                target.LogicTick(context);
                AfterTargetLogicTick(target, context);
            }
        }

        void AdvanceLogicTick(float fixedDeltaSeconds)
        {
            LocalLogicTick++;
            using (LogicMarker.Auto())
                TickTargets(fixedDeltaSeconds);
        }

        void BeforeTargetLogicTick(IGameplayLogicTickTarget target, GameplayLogicTickContext context)
        {
            for (int i = 0; i < m_TickHooks.Count; i++)
            {
                IGameplayTickHook hook = m_TickHooks[i];
                if (hook != null && hook.Target == target)
                    hook.BeforeLogicTick(context);
            }
        }

        void AfterTargetLogicTick(IGameplayLogicTickTarget target, GameplayLogicTickContext context)
        {
            for (int i = 0; i < m_TickHooks.Count; i++)
            {
                IGameplayTickHook hook = m_TickHooks[i];
                if (hook != null && hook.Target == target)
                    hook.AfterLogicTick(context);
            }
        }

        float SelectAccumulatorDeltaSeconds()
        {
            return m_Settings.TimeSource == GameplayTickTimeSource.Unscaled
                ? m_LastUnscaledDeltaSeconds
                : m_LastScaledDeltaSeconds;
        }

        void BeginTargetRenderFrame(ulong renderFrame)
        {
            for (int i = 0; i < m_InputTargets.Count; i++)
                m_InputTargets[i]?.BeginRenderFrame(renderFrame);
        }
    }
}
