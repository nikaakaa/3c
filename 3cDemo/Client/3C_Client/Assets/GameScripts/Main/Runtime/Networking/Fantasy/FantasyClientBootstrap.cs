using Fantasy.Async;
using Fantasy.Platform.Unity;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonGameplay.Networking.Fantasy
{
    public static class FantasyClientBootstrap
    {
        private static bool s_Initialized;
        private static bool s_Initializing;
        private static int s_ActiveOwners;
        private static readonly List<FTask> s_InitializationWaiters = new List<FTask>();

        public static bool IsInitialized => s_Initialized;

        public static async FTask InitializeAsync()
        {
            if (s_Initialized)
            {
                return;
            }

            if (s_Initializing)
            {
                FTask waiter = FTask.Create(false);
                s_InitializationWaiters.Add(waiter);
                await waiter;
                return;
            }

            s_Initializing = true;
            try
            {
                await Entry.Initialize();
                s_Initialized = true;
                CompleteInitializationWaiters(null);
                Debug.Log("Fantasy.Unity client initialized.");
            }
            catch (System.Exception exception)
            {
                CompleteInitializationWaiters(exception);
                throw;
            }
            finally
            {
                s_Initializing = false;
            }
        }

        public static void Shutdown()
        {
            if (s_ActiveOwners != 0)
            {
                throw new System.InvalidOperationException("Fantasy runtime still has active session owners.");
            }

            if (s_Initializing)
            {
                throw new System.InvalidOperationException("Fantasy runtime initialization is still running.");
            }

            if (FantasyObject.FantasyObjectGameObject != null)
            {
                Object.Destroy(FantasyObject.FantasyObjectGameObject);
            }

            s_Initialized = false;
        }

        internal static void RegisterOwner()
        {
            checked
            {
                s_ActiveOwners++;
            }
        }

        internal static void UnregisterOwner()
        {
            if (s_ActiveOwners <= 0)
            {
                throw new System.InvalidOperationException("Fantasy session owner count is invalid.");
            }

            s_ActiveOwners--;
        }

        private static void CompleteInitializationWaiters(System.Exception exception)
        {
            for (int i = 0; i < s_InitializationWaiters.Count; i++)
            {
                if (exception == null)
                {
                    s_InitializationWaiters[i].SetResult();
                }
                else
                {
                    s_InitializationWaiters[i].SetException(exception);
                }
            }

            s_InitializationWaiters.Clear();
        }
    }
}
