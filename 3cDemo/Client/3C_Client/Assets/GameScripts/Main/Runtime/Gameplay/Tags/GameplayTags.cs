using System;
using System.Collections.Generic;
using ThirdPersonGameplay.Contracts;
using UnityEngine;

namespace ThirdPersonGameplay.Tags
{
    [Serializable]
    public struct GameplayTagId : IEquatable<GameplayTagId>, IComparable<GameplayTagId>
    {
        [SerializeField] string m_Value;

        public GameplayTagId(string value)
        {
            m_Value = Normalize(value);
        }

        public string Value => Normalize(m_Value);
        public bool IsValid => !string.IsNullOrEmpty(Value);

        public bool Equals(GameplayTagId other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is GameplayTagId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(Value);
        }

        public int CompareTo(GameplayTagId other)
        {
            return string.Compare(Value, other.Value, StringComparison.Ordinal);
        }

        public override string ToString()
        {
            return Value;
        }

        public static bool operator ==(GameplayTagId left, GameplayTagId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(GameplayTagId left, GameplayTagId right)
        {
            return !left.Equals(right);
        }

        static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    [Serializable]
    public sealed class GameplayTagDefinition
    {
        [SerializeField] GameplayTagId m_TagId;
        [SerializeField] string m_DisplayName;
        [SerializeField] GameplayTagId m_ParentTag;
        [SerializeField] string m_DebugCategory;

        public GameplayTagId TagId => m_TagId;
        public string DisplayName => m_DisplayName ?? string.Empty;
        public GameplayTagId ParentTag => m_ParentTag;
        public string DebugCategory => m_DebugCategory ?? string.Empty;
    }

    public sealed class GameplayTagCatalogRuntimeData
    {
        readonly GameplayTagId[] m_Tags;
        readonly int[] m_Parents;
        readonly Dictionary<GameplayTagId, int> m_Indices;

        GameplayTagCatalogRuntimeData(GameplayTagId[] tags, int[] parents, Dictionary<GameplayTagId, int> indices)
        {
            m_Tags = tags;
            m_Parents = parents;
            m_Indices = indices;
        }

        public int Count => m_Tags.Length;
        public IReadOnlyList<GameplayTagId> Tags => m_Tags;

        public bool Contains(GameplayTagId tagId)
        {
            return tagId.IsValid && m_Indices.ContainsKey(tagId);
        }

        public bool TryGetIndex(GameplayTagId tagId, out int index)
        {
            return m_Indices.TryGetValue(tagId, out index);
        }

        public bool Matches(GameplayTagId ownedTag, GameplayTagId queryTag)
        {
            if (!TryGetIndex(ownedTag, out int index) || !TryGetIndex(queryTag, out int queryIndex))
                return false;

            while (index >= 0)
            {
                if (index == queryIndex)
                    return true;
                index = m_Parents[index];
            }

            return false;
        }

        public static bool TryBuild(GameplayTagCatalog catalog, out GameplayTagCatalogRuntimeData data, List<string> errors)
        {
            data = null;
            if (!catalog)
            {
                errors?.Add("Gameplay Tag Catalog is missing.");
                return false;
            }

            IReadOnlyList<GameplayTagDefinition> definitions = catalog.Tags;
            var indices = new Dictionary<GameplayTagId, int>();
            var tags = new GameplayTagId[definitions.Count];
            bool valid = true;
            for (int i = 0; i < definitions.Count; i++)
            {
                GameplayTagDefinition definition = definitions[i];
                if (definition == null || !definition.TagId.IsValid)
                {
                    errors?.Add($"{catalog.name}: tag #{i} is missing an id.");
                    valid = false;
                    continue;
                }

                tags[i] = definition.TagId;
                if (!indices.TryAdd(definition.TagId, i))
                {
                    errors?.Add($"{catalog.name}: duplicate tag id '{definition.TagId}'.");
                    valid = false;
                }
            }

            var parents = new int[definitions.Count];
            for (int i = 0; i < definitions.Count; i++)
            {
                parents[i] = -1;
                GameplayTagDefinition definition = definitions[i];
                if (definition == null || !definition.ParentTag.IsValid)
                    continue;
                if (!indices.TryGetValue(definition.ParentTag, out int parentIndex))
                {
                    errors?.Add($"{catalog.name}: tag '{definition.TagId}' references missing parent '{definition.ParentTag}'.");
                    valid = false;
                    continue;
                }
                parents[i] = parentIndex;
            }

            var visit = new byte[definitions.Count];
            for (int i = 0; i < definitions.Count; i++)
            {
                if (!ValidateAcyclic(i, parents, visit))
                {
                    errors?.Add($"{catalog.name}: tag parent cycle contains '{tags[i]}'.");
                    valid = false;
                }
            }

            if (!valid)
                return false;

            data = new GameplayTagCatalogRuntimeData(tags, parents, indices);
            return true;
        }

        static bool ValidateAcyclic(int index, IReadOnlyList<int> parents, byte[] visit)
        {
            if (visit[index] == 2)
                return true;
            if (visit[index] == 1)
                return false;

            visit[index] = 1;
            int parent = parents[index];
            if (parent >= 0 && !ValidateAcyclic(parent, parents, visit))
                return false;
            visit[index] = 2;
            return true;
        }
    }

    [Serializable]
    public sealed class GameplayTagQuery
    {
        [SerializeField] GameplayTagId[] m_All = Array.Empty<GameplayTagId>();
        [SerializeField] GameplayTagId[] m_Any = Array.Empty<GameplayTagId>();
        [SerializeField] GameplayTagId[] m_None = Array.Empty<GameplayTagId>();

        public IReadOnlyList<GameplayTagId> All => m_All ?? Array.Empty<GameplayTagId>();
        public IReadOnlyList<GameplayTagId> Any => m_Any ?? Array.Empty<GameplayTagId>();
        public IReadOnlyList<GameplayTagId> None => m_None ?? Array.Empty<GameplayTagId>();
        public bool IsEmpty => All.Count == 0 && Any.Count == 0 && None.Count == 0;

        public bool CollectConfigurationErrors(GameplayTagCatalogRuntimeData catalog, string owner, List<string> errors)
        {
            bool valid = true;
            valid &= ValidateList(All, "All", catalog, owner, errors);
            valid &= ValidateList(Any, "Any", catalog, owner, errors);
            valid &= ValidateList(None, "None", catalog, owner, errors);
            return valid;
        }

        static bool ValidateList(IReadOnlyList<GameplayTagId> values, string label, GameplayTagCatalogRuntimeData catalog, string owner, List<string> errors)
        {
            bool valid = true;
            var unique = new HashSet<GameplayTagId>();
            for (int i = 0; i < values.Count; i++)
            {
                GameplayTagId value = values[i];
                if (!value.IsValid || !catalog.Contains(value))
                {
                    errors?.Add($"{owner}: {label} tag #{i} '{value}' is not registered.");
                    valid = false;
                }
                else if (!unique.Add(value))
                {
                    errors?.Add($"{owner}: duplicate {label} tag '{value}'.");
                    valid = false;
                }
            }
            return valid;
        }
    }

    public enum GameplayTagSourceKind : byte
    {
        None,
        CharacterInitial,
        ActionInstance,
        ActiveGameplayEffect
    }

    public readonly struct GameplayTagSourceHandle : IEquatable<GameplayTagSourceHandle>
    {
        public GameplayTagSourceHandle(GameplayTagSourceKind kind, ulong value)
        {
            Kind = kind;
            Value = value;
        }

        public GameplayTagSourceKind Kind { get; }
        public ulong Value { get; }
        public bool IsValid => Kind != GameplayTagSourceKind.None && Value != 0;
        public static GameplayTagSourceHandle CharacterInitial => new GameplayTagSourceHandle(GameplayTagSourceKind.CharacterInitial, 1);
        public static GameplayTagSourceHandle ActionInstance(ulong id) => new GameplayTagSourceHandle(GameplayTagSourceKind.ActionInstance, id);
        public static GameplayTagSourceHandle ActiveEffect(ulong id) => new GameplayTagSourceHandle(GameplayTagSourceKind.ActiveGameplayEffect, id);
        public bool Equals(GameplayTagSourceHandle other) => Kind == other.Kind && Value == other.Value;
        public override bool Equals(object obj) => obj is GameplayTagSourceHandle other && Equals(other);
        public override int GetHashCode() => ((int)Kind * 397) ^ Value.GetHashCode();
        public override string ToString() => IsValid ? $"{Kind}:{Value}" : "None";
    }

    public readonly struct GameplayTagCountChange
    {
        public GameplayTagCountChange(GameplayTagId tagId, GameplayTagSourceHandle source, int before, int after)
        {
            TagId = tagId;
            Source = source;
            Before = before;
            After = after;
        }

        public GameplayTagId TagId { get; }
        public GameplayTagSourceHandle Source { get; }
        public int Before { get; }
        public int After { get; }
    }

    public sealed class GameplayTagContainer : IGameplayTagReader, IGameplayTagSourceSink
    {
        readonly GameplayTagCatalogRuntimeData m_Catalog;
        readonly Dictionary<GameplayTagSourceHandle, HashSet<int>> m_SourceTags = new Dictionary<GameplayTagSourceHandle, HashSet<int>>();
        readonly int[] m_Counts;
        readonly List<GameplayTagCountChange> m_Changes = new List<GameplayTagCountChange>();

        public GameplayTagContainer(GameplayTagCatalogRuntimeData catalog)
        {
            m_Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            m_Counts = new int[catalog.Count];
        }

        public bool HasTag(GameplayTagId tagId)
        {
            if (!m_Catalog.TryGetIndex(tagId, out int queryIndex))
                return false;

            for (int i = 0; i < m_Counts.Length; i++)
            {
                if (m_Counts[i] > 0 && m_Catalog.Matches(m_Catalog.Tags[i], m_Catalog.Tags[queryIndex]))
                    return true;
            }
            return false;
        }

        public bool Matches(GameplayTagQuery query)
        {
            return MatchesQuery(query, HasTag);
        }

        public bool Matches(GameplayTagQuery query, IReadOnlyList<GameplayTagId> explicitTags)
        {
            if (explicitTags == null)
                return false;
            return MatchesQuery(query, tag => ContainsMatching(explicitTags, tag));
        }

        public bool SetSourceTags(GameplayTagSourceHandle source, IReadOnlyList<GameplayTagId> tags)
        {
            if (!source.IsValid || tags == null)
                return false;

            var indices = new HashSet<int>();
            for (int i = 0; i < tags.Count; i++)
            {
                if (!m_Catalog.TryGetIndex(tags[i], out int index))
                    return false;
                indices.Add(index);
            }

            RemoveSource(source);
            if (indices.Count == 0)
                return true;

            m_SourceTags.Add(source, indices);
            foreach (int index in indices)
            {
                int before = m_Counts[index];
                m_Counts[index]++;
                if (before == 0)
                    m_Changes.Add(new GameplayTagCountChange(m_Catalog.Tags[index], source, before, 1));
            }
            return true;
        }

        public bool RemoveSource(GameplayTagSourceHandle source)
        {
            if (!source.IsValid || !m_SourceTags.TryGetValue(source, out HashSet<int> indices))
                return false;

            foreach (int index in indices)
            {
                int before = m_Counts[index];
                m_Counts[index] = Math.Max(0, before - 1);
                if (m_Counts[index] == 0)
                    m_Changes.Add(new GameplayTagCountChange(m_Catalog.Tags[index], source, before, 0));
            }
            m_SourceTags.Remove(source);
            return true;
        }

        public void DrainChanges(List<GameplayTagCountChange> destination)
        {
            if (destination == null)
                return;
            destination.AddRange(m_Changes);
            m_Changes.Clear();
        }

        public GameplayTagId[] CopyOwnedTags()
        {
            var values = new List<GameplayTagId>();
            for (int i = 0; i < m_Counts.Length; i++)
            {
                if (m_Counts[i] > 0)
                    values.Add(m_Catalog.Tags[i]);
            }
            return values.ToArray();
        }

        internal GameplayTagContainerSnapshot CaptureTransactionSnapshot()
        {
            var sources = new Dictionary<GameplayTagSourceHandle, int[]>();
            foreach (KeyValuePair<GameplayTagSourceHandle, HashSet<int>> pair in m_SourceTags)
            {
                var indices = new int[pair.Value.Count];
                pair.Value.CopyTo(indices);
                sources.Add(pair.Key, indices);
            }
            return new GameplayTagContainerSnapshot(sources, (int[])m_Counts.Clone());
        }

        internal void RestoreTransactionSnapshot(GameplayTagContainerSnapshot snapshot)
        {
            m_SourceTags.Clear();
            foreach (KeyValuePair<GameplayTagSourceHandle, int[]> pair in snapshot.SourceTags)
                m_SourceTags.Add(pair.Key, new HashSet<int>(pair.Value));
            Array.Copy(snapshot.Counts, m_Counts, m_Counts.Length);
            m_Changes.Clear();
        }

        public void Clear(bool recordChanges)
        {
            if (recordChanges)
            {
                var sources = new List<GameplayTagSourceHandle>(m_SourceTags.Keys);
                for (int i = 0; i < sources.Count; i++)
                    RemoveSource(sources[i]);
            }
            else
            {
                m_SourceTags.Clear();
                Array.Clear(m_Counts, 0, m_Counts.Length);
                m_Changes.Clear();
            }
        }

        bool ContainsMatching(IReadOnlyList<GameplayTagId> tags, GameplayTagId queryTag)
        {
            for (int i = 0; i < tags.Count; i++)
            {
                if (m_Catalog.Matches(tags[i], queryTag))
                    return true;
            }
            return false;
        }

        static bool MatchesQuery(GameplayTagQuery query, Func<GameplayTagId, bool> hasTag)
        {
            if (query == null)
                return true;
            for (int i = 0; i < query.All.Count; i++)
            {
                if (!hasTag(query.All[i]))
                    return false;
            }
            if (query.Any.Count > 0)
            {
                bool any = false;
                for (int i = 0; i < query.Any.Count; i++)
                    any |= hasTag(query.Any[i]);
                if (!any)
                    return false;
            }
            for (int i = 0; i < query.None.Count; i++)
            {
                if (hasTag(query.None[i]))
                    return false;
            }
            return true;
        }
    }

    internal sealed class GameplayTagContainerSnapshot
    {
        public GameplayTagContainerSnapshot(
            Dictionary<GameplayTagSourceHandle, int[]> sourceTags,
            int[] counts)
        {
            SourceTags = sourceTags;
            Counts = counts;
        }

        public Dictionary<GameplayTagSourceHandle, int[]> SourceTags { get; }
        public int[] Counts { get; }
    }
}
