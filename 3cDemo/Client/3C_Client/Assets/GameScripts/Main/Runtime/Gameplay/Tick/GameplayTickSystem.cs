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
        readonly List<IGameplayTickHook> m_TickHooks = new List<IGameplayTickHook>();
        readonly Queue<GameplayTickDriveCommand> m_DriveCommands = new Queue<GameplayTickDriveCommand>();
        readonly GameplayTickSettings m_Settings;

        GameplayTickDrivePolicy m_DrivePolicy = GameplayTickDrivePolicy.Default;
        float m_AccumulatorSeconds;
        float m_InterpolationAlpha;
        float m_LastScaledDeltaSeconds;
        float m_LastUnscaledDeltaSeconds;
        float m_LastPresentationDeltaSeconds;
        ulong m_NextDriveCommandSequence;
        ulong m_LastDriveCommandSequence;
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

            RenderFrame++;
            m_LastScaledDeltaSeconds = Math.Max(0f, scaledDeltaSeconds);
            m_LastUnscaledDeltaSeconds = Math.Max(0f, unscaledDeltaSeconds);
            float fixedDeltaSeconds = m_Settings.FixedDeltaSeconds;
            ProcessDriveCommands();
            int advancedLogicTicks = 0;
            AdmitAccumulatorDelta();
            using (InputMarker.Auto())
                BeginTargetRenderFrame(RenderFrame);

            if (m_DrivePolicy.Mode == GameplayTickDriveMode.ManualStep)
            {
                advancedLogicTicks = RunManualLogicTicks(fixedDeltaSeconds);
            }
            else
            {
                advancedLogicTicks = RunAccumulatorLogicTicks(fixedDeltaSeconds);
                ApplyAccumulatorOverflow(fixedDeltaSeconds);
            }

            m_InterpolationAlpha = m_DrivePolicy.Mode == GameplayTickDriveMode.ManualStep ||
                                   m_DrivePolicy.Mode == GameplayTickDriveMode.Paused
                ? 0f
                : fixedDeltaSeconds > 0f
                ? Mathf.Clamp01(m_AccumulatorSeconds / fixedDeltaSeconds)
                : 0f;
            m_LastPresentationDeltaSeconds = CalculatePresentationDeltaSeconds(
                fixedDeltaSeconds,
                advancedLogicTicks);
        }

        public void FrameLateUpdate()
        {
            if (m_Disposed)
                return;

            using (PresentationMarker.Auto())
            {
                for (int i = 0; i < m_PresentationTargets.Count; i++)
                {
                    IGameplayPresentationFrameTarget target = m_PresentationTargets[i];
                    if (target == null)
                        continue;

                    var context = new GameplayPresentationFrameContext(
                        m_LastScaledDeltaSeconds,
                        m_LastUnscaledDeltaSeconds,
                        m_LastPresentationDeltaSeconds,
                        m_DrivePolicy.PresentationClockMode,
                        RenderFrame,
                        LocalLogicTick,
                        m_InterpolationAlpha);
                    target.PresentationFrame(context);
                }
            }
        }

        public void Dispose()
        {
            m_InputTargets.Clear();
            m_LogicTargets.Clear();
            m_PresentationTargets.Clear();
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
                m_LastPresentationDeltaSeconds);
        }

        void ProcessDriveCommands()
        {
            while (m_DriveCommands.Count > 0)
            {
                GameplayTickDriveCommand command = m_DriveCommands.Dequeue();
                m_LastDriveCommandSequence = command.Sequence;

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
                        m_DrivePolicy = m_DrivePolicy.WithMode(GameplayTickDriveMode.Paused, 0);
                        m_AccumulatorSeconds = 0f;
                        break;
                    case GameplayTickDriveCommandKind.Step:
                        m_DrivePolicy = new GameplayTickDrivePolicy(
                            GameplayTickDriveMode.ManualStep,
                            m_DrivePolicy.PresentationClockMode,
                            m_DrivePolicy.RateMultiplier,
                            m_DrivePolicy.QueuedManualTicks + Math.Max(1UL, command.StepCount));
                        m_AccumulatorSeconds = 0f;
                        break;
                    case GameplayTickDriveCommandKind.SetRatePlayback:
                        m_DrivePolicy = m_DrivePolicy.WithRatePlayback(command.RateMultiplier);
                        m_AccumulatorSeconds = 0f;
                        break;
                    case GameplayTickDriveCommandKind.SetPresentationClock:
                        m_DrivePolicy = m_DrivePolicy.WithPresentationClock(command.PresentationClockMode);
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
                    m_AccumulatorSeconds += SelectAccumulatorDeltaSeconds();
                    break;
                case GameplayTickDriveMode.RatePlayback:
                    m_AccumulatorSeconds += SelectAccumulatorDeltaSeconds() * m_DrivePolicy.RateMultiplier;
                    break;
                case GameplayTickDriveMode.Paused:
                case GameplayTickDriveMode.ManualStep:
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
            }

            return catchUpTicks;
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
