using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Animation;

namespace ThirdPersonCharacter.Pipeline.Animation.Resources
{
    internal sealed class CharacterAclResourceStoreEntry
    {
        internal readonly CharacterAnimationCompiledResourceDescriptor Descriptor;
        internal readonly long ResidentBytes;
        internal CharacterAclAnimationResourcePreparation Preparation;
        internal IntPtr Group;
        internal CharacterAclResourceReadinessResult Readiness;
        internal int LeaseCount;
        internal ulong Generation;
        internal ulong LastUsed;
        internal bool Requested;

        internal CharacterAclResourceStoreEntry(
            CharacterAnimationCompiledResourceDescriptor descriptor,
            long residentBytes,
            CharacterAclAnimationResourcePreparation preparation)
        {
            Descriptor = descriptor;
            ResidentBytes = residentBytes;
            Preparation = preparation;
            Readiness = CharacterAclResourceReadinessResult.Pending();
        }
    }

    internal sealed class CharacterAclResourceStoreCatalog
    {
        readonly Dictionary<string, int> m_Indices =
            new Dictionary<string, int>(StringComparer.Ordinal);
        readonly Dictionary<int, int> m_CatalogIndices =
            new Dictionary<int, int>();
        readonly List<CharacterAclResourceStoreEntry> m_Entries =
            new List<CharacterAclResourceStoreEntry>();

        internal int Count => m_Entries.Count;

        internal CharacterAclResourceStoreEntry RequireEntry(int resourceIndex)
        {
            if ((uint)resourceIndex >= (uint)m_Entries.Count)
                throw new ArgumentOutOfRangeException(nameof(resourceIndex));
            return m_Entries[resourceIndex];
        }

        internal bool TryGetResourceIdentity(
            string resourceIdentity,
            out int storeIndex) =>
            m_Indices.TryGetValue(resourceIdentity, out storeIndex);

        internal int RequireIndex(int resourceCatalogIndex)
        {
            if (!m_CatalogIndices.TryGetValue(
                    resourceCatalogIndex,
                    out int storeIndex))
            {
                throw new InvalidOperationException(
                    "ACL resource catalog index is not registered.");
            }
            return storeIndex;
        }

        internal int RequireIndex(
            CharacterAnimationCompiledResourceDescriptor descriptor)
        {
            if (descriptor == null ||
                !m_Indices.TryGetValue(descriptor.ResourceIdentity, out int index))
            {
                throw new InvalidOperationException(
                    "ACL resource is not in the registered resource closure.");
            }
            CharacterAnimationCompiledResourceDescriptor registered =
                m_Entries[index].Descriptor;
            if (!string.Equals(
                    registered.GroupContentHash,
                    descriptor.GroupContentHash,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "ACL resource group identity resolves to different content.");
            }
            return index;
        }

        internal void Add(
            string resourceIdentity,
            int resourceCatalogIndex,
            CharacterAclResourceStoreEntry entry)
        {
            int index = m_Entries.Count;
            m_Entries.Add(entry);
            m_Indices.Add(resourceIdentity, index);
            m_CatalogIndices.Add(resourceCatalogIndex, index);
        }

        internal void Clear()
        {
            m_Entries.Clear();
            m_Indices.Clear();
            m_CatalogIndices.Clear();
        }
    }
}
