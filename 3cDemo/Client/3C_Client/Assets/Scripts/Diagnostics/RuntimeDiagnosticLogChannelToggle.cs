using System;
using UnityEngine;

namespace ThirdPersonDiagnostics
{
    [Serializable]
    public struct RuntimeDiagnosticLogChannelToggle
    {
        [SerializeField] string key;
        [SerializeField] bool enabled;

        public RuntimeDiagnosticLogChannelToggle(string key, bool enabled)
        {
            this.key = key ?? string.Empty;
            this.enabled = enabled;
        }

        public string Key => key;
        public bool Enabled => enabled;

        public void SetEnabled(bool value)
        {
            enabled = value;
        }
    }
}
