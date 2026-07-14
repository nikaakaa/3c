using System;
using System.Collections.Generic;
using System.Globalization;

namespace BTSMTL.Diagnostics
{
    public readonly struct DebugSourceMapEntry
    {
        public DebugSourceMapEntry(
            RuntimeSourceElementHandle handle,
            RuntimeSourceElementKey source,
            RuntimeSourceElementHandle parent,
            string displayName,
            string contentHash)
        {
            Handle = handle;
            Source = source;
            Parent = parent;
            DisplayName = displayName ?? string.Empty;
            ContentHash = contentHash ?? string.Empty;
        }

        public RuntimeSourceElementHandle Handle { get; }
        public RuntimeSourceElementKey Source { get; }
        public RuntimeSourceElementHandle Parent { get; }
        public string DisplayName { get; }
        public string ContentHash { get; }
    }

    public interface IDebugSourceMap
    {
        RuntimeProgramRevision Revision { get; }
        IReadOnlyList<DebugSourceMapEntry> Entries { get; }
        bool TryGet(RuntimeSourceElementHandle handle, out DebugSourceMapEntry entry);
        bool TryGetHandle(RuntimeSourceElementKey source, out RuntimeSourceElementHandle handle);
        IReadOnlyList<RuntimeSourceElementHandle> FindHandles(RuntimeSourceElementKey source);
    }

    public sealed class DebugSourceMap : IDebugSourceMap, IRuntimeDebugProgram
    {
        readonly List<DebugSourceMapEntry> m_Entries = new List<DebugSourceMapEntry>();
        readonly Dictionary<int, DebugSourceMapEntry> m_ByHandle = new Dictionary<int, DebugSourceMapEntry>();
        readonly Dictionary<RuntimeSourceElementKey, List<RuntimeSourceElementHandle>> m_BySource = new Dictionary<RuntimeSourceElementKey, List<RuntimeSourceElementHandle>>();
        bool m_Sealed;

        public DebugSourceMap(RuntimeProgramRevision revision)
        {
            Revision = revision;
        }

        public RuntimeProgramRevision Revision { get; }
        public IDebugSourceMap SourceMap => this;
        public IReadOnlyList<DebugSourceMapEntry> Entries => m_Entries;

        public RuntimeSourceElementHandle Add(
            RuntimeSourceElementKey source,
            RuntimeSourceElementHandle parent,
            string displayName,
            string contentHash)
        {
            if (m_Sealed)
                throw new InvalidOperationException("Debug Source Map is sealed.");
            if (!source.IsValid)
                throw new InvalidOperationException("Debug Source Map source identity is invalid.");

            var handle = new RuntimeSourceElementHandle(m_Entries.Count + 1, source.Kind);
            var entry = new DebugSourceMapEntry(handle, source, parent, displayName, contentHash);
            m_Entries.Add(entry);
            m_ByHandle.Add(handle.Value, entry);
            if (!m_BySource.TryGetValue(source, out List<RuntimeSourceElementHandle> handles))
            {
                handles = new List<RuntimeSourceElementHandle>();
                m_BySource.Add(source, handles);
            }
            handles.Add(handle);
            return handle;
        }

        public void Seal()
        {
            if (!Revision.IsValid)
                throw new InvalidOperationException("Debug Source Map revision is invalid.");
            for (int i = 0; i < m_Entries.Count; i++)
            {
                DebugSourceMapEntry entry = m_Entries[i];
                if (entry.Parent.IsValid && !m_ByHandle.ContainsKey(entry.Parent.Value))
                    throw new InvalidOperationException($"Debug Source Map parent handle is missing: {entry.Parent}.");
            }
            m_Sealed = true;
        }

        public bool TryGet(RuntimeSourceElementHandle handle, out DebugSourceMapEntry entry)
        {
            if (!handle.IsValid || !m_ByHandle.TryGetValue(handle.Value, out entry))
            {
                entry = default;
                return false;
            }

            return entry.Handle.Kind == handle.Kind;
        }

        public bool TryGetHandle(RuntimeSourceElementKey source, out RuntimeSourceElementHandle handle)
        {
            if (m_BySource.TryGetValue(source, out List<RuntimeSourceElementHandle> handles) && handles.Count > 0)
            {
                handle = handles[0];
                return true;
            }
            handle = default;
            return false;
        }

        public IReadOnlyList<RuntimeSourceElementHandle> FindHandles(RuntimeSourceElementKey source)
        {
            return m_BySource.TryGetValue(source, out List<RuntimeSourceElementHandle> handles)
                ? handles
                : Array.Empty<RuntimeSourceElementHandle>();
        }
    }

    public static class SourceContentHasher
    {
        public static string Hash(params string[] values)
        {
            ulong hash = 14695981039346656037UL;
            if (values != null)
            {
                for (int i = 0; i < values.Length; i++)
                {
                    string value = values[i] ?? string.Empty;
                    for (int character = 0; character < value.Length; character++)
                    {
                        hash ^= value[character];
                        hash *= 1099511628211UL;
                    }
                    hash ^= 0xff;
                    hash *= 1099511628211UL;
                }
            }
            return hash.ToString("x16", CultureInfo.InvariantCulture);
        }
    }
}
