using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonDiagnostics
{
    public sealed class RuntimeDiagnosticLogInspectorController : MonoBehaviour
    {
        [SerializeField] List<RuntimeDiagnosticLogChannelToggle> channels = new List<RuntimeDiagnosticLogChannelToggle>();
        [SerializeField] string containsFilter = string.Empty;
        [SerializeField] string prefixFilter = string.Empty;
        [SerializeField] string suffixFilter = string.Empty;
        [SerializeField] string manualChannelKey = string.Empty;

        public IReadOnlyList<RuntimeDiagnosticLogChannelToggle> Channels => channels;
        public string ContainsFilter => containsFilter;
        public string PrefixFilter => prefixFilter;
        public string SuffixFilter => suffixFilter;
        public string ManualChannelKey => manualChannelKey;

        void Reset()
        {
            SynchronizeChannels();
            ApplyChannels();
        }

        void OnEnable()
        {
            SynchronizeChannels();
            ApplyChannels();
        }

        void OnValidate()
        {
            SynchronizeChannels();
        }

        public void SynchronizeChannels()
        {
            Dictionary<string, bool> existing = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < channels.Count; i++)
            {
                RuntimeDiagnosticLogChannelToggle channel = channels[i];
                string key = channel.Key;
                if (!string.IsNullOrWhiteSpace(key) && !existing.ContainsKey(key))
                    existing.Add(key.Trim(), channel.Enabled);
            }

            string[] knownKeys = RuntimeDiagnosticLog.Filter.GetKnownChannelKeys();
            for (int i = 0; i < knownKeys.Length; i++)
            {
                string key = knownKeys[i];
                if (!existing.ContainsKey(key))
                    existing.Add(key, RuntimeDiagnosticLog.Filter.IsChannelEnabled(key));
            }

            string[] sortedKeys = new string[existing.Count];
            existing.Keys.CopyTo(sortedKeys, 0);
            Array.Sort(sortedKeys, StringComparer.OrdinalIgnoreCase);

            channels.Clear();
            for (int i = 0; i < sortedKeys.Length; i++)
            {
                string key = sortedKeys[i];
                channels.Add(new RuntimeDiagnosticLogChannelToggle(key, existing[key]));
            }
        }

        public void ApplyChannels()
        {
            SynchronizeChannels();
            for (int i = 0; i < channels.Count; i++)
                RuntimeDiagnosticLog.Filter.SetChannelEnabled(channels[i].Key, channels[i].Enabled);
        }

        public void EnableAllChannels()
        {
            SetAllChannels(true);
            ApplyChannels();
        }

        public void DisableAllChannels()
        {
            SetAllChannels(false);
            ApplyChannels();
        }

        public void ApplyContainsFilter()
        {
            ApplyNameFilter(NameMatchMode.Contains, containsFilter);
        }

        public void ApplyPrefixFilter()
        {
            ApplyNameFilter(NameMatchMode.Prefix, prefixFilter);
        }

        public void ApplySuffixFilter()
        {
            ApplyNameFilter(NameMatchMode.Suffix, suffixFilter);
        }

        public void ApplyContainsFilter(string value)
        {
            containsFilter = value ?? string.Empty;
            ApplyContainsFilter();
        }

        public void ApplyPrefixFilter(string value)
        {
            prefixFilter = value ?? string.Empty;
            ApplyPrefixFilter();
        }

        public void ApplySuffixFilter(string value)
        {
            suffixFilter = value ?? string.Empty;
            ApplySuffixFilter();
        }

        public void SetChannelEnabled(string key, bool enabled)
        {
            if (string.IsNullOrWhiteSpace(key))
                return;

            SynchronizeChannels();
            for (int i = 0; i < channels.Count; i++)
            {
                if (!string.Equals(channels[i].Key, key, StringComparison.OrdinalIgnoreCase))
                    continue;

                RuntimeDiagnosticLogChannelToggle channel = channels[i];
                channel.SetEnabled(enabled);
                channels[i] = channel;
                RuntimeDiagnosticLog.Filter.SetChannelEnabled(channel.Key, enabled);
                return;
            }

            RuntimeDiagnosticLog.Filter.SetChannelEnabled(key, enabled);
            channels.Add(new RuntimeDiagnosticLogChannelToggle(key.Trim(), enabled));
        }

        public void AddManualChannel()
        {
            AddManualChannel(manualChannelKey);
        }

        public void AddManualChannel(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return;

            string normalizedKey = key.Trim();
            RuntimeDiagnosticLog.RegisterChannel(normalizedKey);
            SetChannelEnabled(normalizedKey, true);
            manualChannelKey = normalizedKey;
        }

        void ApplyNameFilter(NameMatchMode mode, string text)
        {
            SynchronizeChannels();
            for (int i = 0; i < channels.Count; i++)
            {
                RuntimeDiagnosticLogChannelToggle channel = channels[i];
                channel.SetEnabled(Matches(channel.Key, mode, text));
                channels[i] = channel;
            }

            ApplyChannels();
        }

        void SetAllChannels(bool enabled)
        {
            SynchronizeChannels();
            for (int i = 0; i < channels.Count; i++)
            {
                RuntimeDiagnosticLogChannelToggle channel = channels[i];
                channel.SetEnabled(enabled);
                channels[i] = channel;
            }
        }

        static bool Matches(string value, NameMatchMode mode, string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return true;

            string filter = text.Trim();
            switch (mode)
            {
                case NameMatchMode.Prefix:
                    return value.StartsWith(filter, StringComparison.OrdinalIgnoreCase);
                case NameMatchMode.Suffix:
                    return value.EndsWith(filter, StringComparison.OrdinalIgnoreCase);
                default:
                    return value.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
            }
        }

        enum NameMatchMode
        {
            Contains,
            Prefix,
            Suffix
        }
    }
}
