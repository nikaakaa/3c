using Fantasy.Async;
using Fantasy.Platform.Unity;
using UnityEngine;

namespace GameLogic.Network.Fantasy
{
    public static class FantasyClientBootstrap
    {
        private static bool _initialized;

        public static FantasySessionFacade SessionFacade { get; } = new FantasySessionFacade();

        public static bool IsInitialized => _initialized;

        public static async FTask InitializeAsync()
        {
            if (_initialized)
            {
                return;
            }

            await Entry.Initialize();
            _initialized = true;
            Debug.Log("Fantasy.Unity client initialized.");
        }

        public static void Shutdown()
        {
            SessionFacade.Disconnect();
            _initialized = false;
        }
    }
}
