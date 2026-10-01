using TEngine;
using ThirdPersonPerformance.Instrumentation;
using Unity.Profiling;
using UnityEngine;

namespace ThirdPersonGameplay.Tick
{
    public static class GameplayTickBootstrap
    {
        static readonly ProfilerMarker s_Update = new("GameplayTick.FrameUpdate");
        static readonly ProfilerMarker s_Hotkeys = new("GameplayTick.Hotkeys");

        static bool s_Initialized;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void RuntimeInitialize()
        {
            Initialize(GameplayTickSettings.Default);
        }

        public static void Initialize(GameplayTickSettings settings)
        {
            if (s_Initialized)
                return;

            GameplayTickSystem.Initialize(settings);
            Utility.Unity.AddUpdateListener(FrameUpdate);
            Utility.Unity.AddLateUpdateListener(FrameLateUpdate);
            Utility.Unity.AddDestroyListener(Shutdown);
            s_Initialized = true;
        }

        public static void Shutdown()
        {
            if (!s_Initialized)
                return;

            Utility.Unity.RemoveUpdateListener(FrameUpdate);
            Utility.Unity.RemoveLateUpdateListener(FrameLateUpdate);
            Utility.Unity.RemoveDestroyListener(Shutdown);
            GameplayTickSystem.Shutdown();
            s_Initialized = false;
        }

        static void FrameUpdate()
        {
            using var profilerScope = s_Update.Auto();
            using (s_Hotkeys.Auto())
                GameplayTickDebugHotkeys.Pump();
            GameplayTickSystem.Current?.FrameUpdate(Time.deltaTime, Time.unscaledDeltaTime);
        }

        static void FrameLateUpdate()
        {
            GameplayTickSystem tick = GameplayTickSystem.Current;
            if (tick == null)
                return;
            PerformanceInstrumentationContextRuntime.BeginFrame(tick.RenderFrame);
            try
            {
                tick.FrameLateUpdate();
            }
            finally
            {
                PerformanceInstrumentationContextRuntime.EndFrame();
            }
        }
    }
}
