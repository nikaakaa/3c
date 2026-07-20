using System;
using System.Collections.Generic;
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
        public bool Equals(GameplayTagId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is GameplayTagId other && Equals(other);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
        public int CompareTo(GameplayTagId other) => string.Compare(Value, other.Value, StringComparison.Ordinal);
        public override string ToString() => Value;
        public static bool operator ==(GameplayTagId left, GameplayTagId right) => left.Equals(right);
        public static bool operator !=(GameplayTagId left, GameplayTagId right) => !left.Equals(right);

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
        public bool Contains(GameplayTagId tagId) => tagId.IsValid && m_Indices.ContainsKey(tagId);
        public bool TryGetIndex(GameplayTagId tagId, out int index) => m_Indices.TryGetValue(tagId, out index);

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

        static bool ValidateList(
            IReadOnlyList<GameplayTagId> values,
            string label,
            GameplayTagCatalogRuntimeData catalog,
            string owner,
            List<string> errors)
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
}
