using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZZZ.Rendering.Restored
{
    public sealed class CorinEffectMaterialSet : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            public string SourceKey;
            public Material Material;
        }

        [SerializeField] List<Entry> entries = new();

        Dictionary<string, Material> lookup;

        public IReadOnlyList<Entry> Entries => entries;

        public Material Find(string sourceKey)
        {
            if (lookup == null)
            {
                lookup = new Dictionary<string, Material>(entries.Count, StringComparer.Ordinal);
                foreach (var entry in entries)
                    lookup.Add(entry.SourceKey, entry.Material);
            }
            return lookup.TryGetValue(sourceKey, out var material) ? material : null;
        }

        public void Initialize(IEnumerable<Entry> source)
        {
            entries.Clear();
            entries.AddRange(source);
            lookup = null;
        }
    }
}
