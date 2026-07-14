using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonGameplay.Tick
{
    public interface IGameplayTickTarget
    {
        void BeginRenderFrame(ulong renderFrame);
        void LogicTick(GameplayLogicTickContext context);
        void PresentationFrame(GameplayPresentationFrameContext context);
    }

    public interface IGameplayTickHook
    {
        IGameplayTickTarget Target { get; }
        void BeforeLogicTick(GameplayLogicTickContext context);
        void AfterLogicTick(GameplayLogicTickContext context);
    }

    public sealed class GameplayTickSystem : IDisposable
    {
        static GameplayTickSystem s_Current;

        readonly List<IGameplayTickTarget> m_Targets = new List<IGameplayTickTarget>();
        readonly List<IGameplayTickHook> m_TickHooks = new List<IGameplayTickHook>();
        readonly GameplayTickSettings m_Settings;

        float m_AccumulatorSeconds;
        float m_InterpolationAlpha;
        float m_LastScaledDeltaSeconds;
        float m_LastUnscaledDeltaSeconds;
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

        public static bool RegisterTarget(IGameplayTickTarget target)
        {
            if (s_Current == null)
            {
                Debug.LogError("GameplayTickSystem is not initialized.");
                return false;
            }

            s_Current.Register(target);
            return true;
        }

        public static void UnregisterTarget(IGameplayTickTarget target)
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

        public void Register(IGameplayTickTarget target)
        {
            if (m_Disposed || target == null || m_Targets.Contains(target))
                return;

            m_Targets.Add(target);
        }

        public void Register(IGameplayTickHook hook)
        {
            if (m_Disposed || hook == null || hook.Target == null || m_TickHooks.Contains(hook))
                return;

            m_TickHooks.Add(hook);
        }

        public void Unregister(IGameplayTickTarget target)
        {
            if (target == null)
                return;

            m_Targets.Remove(target);
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
            m_AccumulatorSeconds += SelectAccumulatorDeltaSeconds();
            BeginTargetRenderFrame(RenderFrame);

            float fixedDeltaSeconds = m_Settings.FixedDeltaSeconds;
            int catchUpTicks = 0;
            while (m_AccumulatorSeconds + float.Epsilon >= fixedDeltaSeconds &&
                   catchUpTicks < m_Settings.MaxCatchUpTicks)
            {
                LocalLogicTick++;
                TickTargets(fixedDeltaSeconds);
                m_AccumulatorSeconds -= fixedDeltaSeconds;
                catchUpTicks++;
            }

            if (m_AccumulatorSeconds >= fixedDeltaSeconds &&
                m_Settings.OverflowPolicy == GameplayAccumulatorOverflowPolicy.DropRemainder)
            {
                DroppedLocalLogicTicks += (int)(m_AccumulatorSeconds / fixedDeltaSeconds);
                m_AccumulatorSeconds = 0f;
            }

            m_InterpolationAlpha = fixedDeltaSeconds > 0f
                ? Mathf.Clamp01(m_AccumulatorSeconds / fixedDeltaSeconds)
                : 0f;
        }

        public void FrameLateUpdate()
        {
            if (m_Disposed)
                return;

            for (int i = 0; i < m_Targets.Count; i++)
            {
                IGameplayTickTarget target = m_Targets[i];
                if (target == null)
                    continue;

                var context = new GameplayPresentationFrameContext(
                    m_LastScaledDeltaSeconds,
                    m_LastUnscaledDeltaSeconds,
                    RenderFrame,
                    LocalLogicTick,
                    m_InterpolationAlpha);
                target.PresentationFrame(context);
            }
        }

        public void Dispose()
        {
            m_Targets.Clear();
            m_TickHooks.Clear();
            m_Disposed = true;
        }

        void TickTargets(float fixedDeltaSeconds)
        {
            for (int i = 0; i < m_Targets.Count; i++)
            {
                IGameplayTickTarget target = m_Targets[i];
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

        void BeforeTargetLogicTick(IGameplayTickTarget target, GameplayLogicTickContext context)
        {
            for (int i = 0; i < m_TickHooks.Count; i++)
            {
                IGameplayTickHook hook = m_TickHooks[i];
                if (hook != null && hook.Target == target)
                    hook.BeforeLogicTick(context);
            }
        }

        void AfterTargetLogicTick(IGameplayTickTarget target, GameplayLogicTickContext context)
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
            for (int i = 0; i < m_Targets.Count; i++)
                m_Targets[i]?.BeginRenderFrame(renderFrame);
        }
    }
}
