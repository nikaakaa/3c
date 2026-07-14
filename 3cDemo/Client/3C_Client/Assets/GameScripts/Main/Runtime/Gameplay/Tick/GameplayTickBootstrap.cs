using TEngine;
using UnityEngine;

namespace ThirdPersonGameplay.Tick
{
    public static class GameplayTickBootstrap
    {
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
            GameplayTickSystem.Current?.FrameUpdate(Time.deltaTime, Time.unscaledDeltaTime);
        }

        static void FrameLateUpdate()
        {
            GameplayTickSystem.Current?.FrameLateUpdate();
        }
    }
}
