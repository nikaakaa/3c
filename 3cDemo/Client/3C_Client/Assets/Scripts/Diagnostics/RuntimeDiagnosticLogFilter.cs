using System;
using System.Collections.Generic;

namespace ThirdPersonDiagnostics
{
    public sealed class RuntimeDiagnosticLogFilter
    {
        readonly bool[] categoryEnabled;
        readonly Dictionary<string, bool> channelEnabled = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        public RuntimeDiagnosticLogFilter(bool defaultEnabled = true)
        {
            int count = Enum.GetValues(typeof(RuntimeDiagnosticLogCategory)).Length;
            categoryEnabled = new bool[count];
            SetAll(defaultEnabled);
        }

        public bool IsEnabled(RuntimeDiagnosticLogCategory category)
        {
            int index = (int)category;
            return index >= 0 && index < categoryEnabled.Length && categoryEnabled[index];
        }

        public bool IsChannelEnabled(string channelKey)
        {
            if (string.IsNullOrWhiteSpace(channelKey))
                return true;

            string normalizedKey = NormalizeChannelKey(channelKey);
            return !channelEnabled.TryGetValue(normalizedKey, out bool enabled) || enabled;
        }

        public void SetEnabled(RuntimeDiagnosticLogCategory category, bool enabled)
        {
            int index = (int)category;
            if (index < 0 || index >= categoryEnabled.Length)
                return;

            categoryEnabled[index] = enabled;
        }

        public void SetChannelEnabled(string channelKey, bool enabled)
        {
            if (string.IsNullOrWhiteSpace(channelKey))
                return;

            channelEnabled[NormalizeChannelKey(channelKey)] = enabled;
        }

        public void SetAll(bool enabled)
        {
            for (int i = 0; i < categoryEnabled.Length; i++)
                categoryEnabled[i] = enabled;

            SetAllChannels(enabled);
        }

        public void Reset(bool defaultEnabled = true)
        {
            for (int i = 0; i < categoryEnabled.Length; i++)
                categoryEnabled[i] = defaultEnabled;

            channelEnabled.Clear();
        }

        public void SetAllChannels(bool enabled)
        {
            string[] keys = GetKnownChannelKeys();
            for (int i = 0; i < keys.Length; i++)
                channelEnabled[keys[i]] = enabled;
        }

        public void RegisterChannel(string channelKey, bool defaultEnabled = true)
        {
            if (string.IsNullOrWhiteSpace(channelKey))
                return;

            string normalizedKey = NormalizeChannelKey(channelKey);
            if (!channelEnabled.ContainsKey(normalizedKey))
                channelEnabled.Add(normalizedKey, defaultEnabled);
        }

        public string[] GetKnownChannelKeys()
        {
            string[] keys = new string[channelEnabled.Count];
            channelEnabled.Keys.CopyTo(keys, 0);
            Array.Sort(keys, StringComparer.OrdinalIgnoreCase);
            return keys;
        }

        public bool ShouldEmit(in RuntimeDiagnosticLogEvent diagnosticEvent)
        {
            RegisterChannel(diagnosticEvent.ChannelKey);
            return IsEnabled(diagnosticEvent.Category) && IsChannelEnabled(diagnosticEvent.ChannelKey);
        }

        static string NormalizeChannelKey(string channelKey)
        {
            return channelKey.Trim();
        }
    }
}
