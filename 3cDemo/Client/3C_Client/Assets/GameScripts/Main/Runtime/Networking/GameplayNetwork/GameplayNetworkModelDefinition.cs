using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonGameplay.Networking
{
    public interface IGameplayNetworkModelSession : IDisposable
    {
        string ModelId { get; }
        bool IsConfigurationLocked { get; }
        int BindingCount { get; }
        void LockConfiguration();
    }

    public abstract class GameplayNetworkModelDefinition : ScriptableObject
    {
        public abstract string ModelId { get; }
        public abstract bool CollectConfigurationErrors(List<string> errors);
        public abstract IGameplayNetworkModelSession CreateSession();
    }
}
