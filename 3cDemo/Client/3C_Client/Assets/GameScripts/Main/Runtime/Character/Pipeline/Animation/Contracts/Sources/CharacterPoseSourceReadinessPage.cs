using System;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal sealed class CharacterPoseSourceReadinessPage
    {
        readonly CharacterPoseSourceReadinessEntry[] m_Entries;
        ulong m_CompletionIdentity;
        ulong m_PageGeneration;
        int m_Count;
        bool m_Sealed;

        internal CharacterPoseSourceReadinessPage(int capacity)
        {
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));
            m_Entries = new CharacterPoseSourceReadinessEntry[capacity];
        }

        internal bool IsOpen => m_CompletionIdentity != 0 && !m_Sealed;
        internal bool IsSealed => m_CompletionIdentity != 0 && m_Sealed;

        internal void Begin(ulong completionIdentity)
        {
            if (completionIdentity == 0)
                throw new ArgumentOutOfRangeException(nameof(completionIdentity));
            Clear();
            m_CompletionIdentity = completionIdentity;
            m_PageGeneration = NextPageGeneration();
        }

        internal void Record(
            CharacterPoseSourceReadinessCategory category,
            in CharacterPoseSourceReadinessKey key,
            in CharacterPoseSourceReadinessView readiness)
        {
            if (!IsOpen ||
                !CharacterPoseSourceReadinessEnumValues.IsValid(category) ||
                !key.IsValid ||
                !readiness.IsValid ||
                readiness.CompletionIdentity != m_CompletionIdentity)
            {
                throw new InvalidOperationException(
                    "Character Pose source readiness does not match its open page.");
            }
            int existing = Find(
                category,
                in key,
                readiness.ResourceCatalogIndex,
                readiness.GroupClipIndex);
            var entry = new CharacterPoseSourceReadinessEntry(
                category,
                in key,
                in readiness);
            if (existing >= 0)
            {
                if (m_Entries[existing].Readiness.IsInvalid &&
                    !readiness.IsInvalid)
                    return;
                m_Entries[existing] = entry;
                return;
            }
            if (m_Count >= m_Entries.Length)
                throw new InvalidOperationException(
                    "Character Pose source readiness capacity was exceeded.");
            m_Entries[m_Count++] = entry;
        }

        internal void Seal()
        {
            if (!IsOpen)
                throw new InvalidOperationException(
                    "Character Pose source readiness page cannot be sealed.");
            m_Sealed = true;
        }

        internal void Remove(
            CharacterPoseSourceReadinessCategory category,
            in CharacterPoseSourceReadinessKey key)
        {
            if (!IsOpen || !CharacterPoseSourceReadinessEnumValues.IsValid(category) ||
                !key.IsValid)
            {
                throw new InvalidOperationException(
                    "Character Pose source readiness cannot remove from its open page.");
            }
            int writeIndex = 0;
            for (int readIndex = 0; readIndex < m_Count; readIndex++)
            {
                CharacterPoseSourceReadinessEntry entry = m_Entries[readIndex];
                if (entry.Category == category && entry.Key.Equals(key))
                    continue;
                if (writeIndex != readIndex)
                    m_Entries[writeIndex] = entry;
                writeIndex++;
            }
            if (writeIndex < m_Count)
                Array.Clear(m_Entries, writeIndex, m_Count - writeIndex);
            m_Count = writeIndex;
        }

        internal CharacterPoseSourceReadinessPageView Capture()
        {
            if (!IsSealed)
                throw new InvalidOperationException(
                    "Character Pose source readiness page is not sealed.");
            return new CharacterPoseSourceReadinessPageView(
                this,
                m_CompletionIdentity,
                m_PageGeneration);
        }

        internal int RequireCount(
            ulong completionIdentity,
            ulong pageGeneration)
        {
            RequireView(completionIdentity, pageGeneration);
            return m_Count;
        }

        internal CharacterPoseSourceReadinessEntry Require(
            int index,
            ulong completionIdentity,
            ulong pageGeneration)
        {
            RequireView(completionIdentity, pageGeneration);
            if ((uint)index >= (uint)m_Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            return m_Entries[index];
        }

        internal CharacterPoseSourceReadinessView Aggregate(
            CharacterPoseSourceReadinessCategory category,
            ulong completionIdentity,
            ulong pageGeneration)
        {
            RequireView(completionIdentity, pageGeneration);
            CharacterPoseSourceReadinessView pending = default;
            CharacterPoseSourceReadinessView ready = default;
            bool hasPending = false;
            bool hasReady = false;
            for (int i = 0; i < m_Count; i++)
            {
                CharacterPoseSourceReadinessEntry entry = m_Entries[i];
                if (entry.Category != category)
                    continue;
                if (entry.Readiness.IsInvalid)
                    return entry.Readiness;
                if (entry.Readiness.IsPending && !hasPending)
                {
                    pending = entry.Readiness;
                    hasPending = true;
                    continue;
                }
                if (entry.Readiness.IsReady &&
                    (!hasReady ||
                     ready.ResourceCatalogIndex < 0 &&
                     entry.Readiness.ResourceCatalogIndex >= 0))
                {
                    ready = entry.Readiness;
                    hasReady = true;
                }
            }
            return hasPending
                ? pending
                : hasReady
                    ? ready
                    : CharacterPoseSourceReadinessView.Ready(
                        completionIdentity);
        }

        internal bool Matches(
            ulong completionIdentity,
            ulong pageGeneration) =>
            m_Sealed &&
            m_CompletionIdentity != 0 &&
            m_PageGeneration != 0 &&
            m_CompletionIdentity == completionIdentity &&
            m_PageGeneration == pageGeneration;

        internal void Clear()
        {
            Array.Clear(m_Entries, 0, m_Count);
            m_CompletionIdentity = 0;
            m_Count = 0;
            m_Sealed = false;
        }

        int Find(
            CharacterPoseSourceReadinessCategory category,
            in CharacterPoseSourceReadinessKey key,
            int resourceCatalogIndex,
            int groupClipIndex)
        {
            for (int i = 0; i < m_Count; i++)
            {
                CharacterPoseSourceReadinessEntry entry = m_Entries[i];
                if (entry.Category == category &&
                    entry.Key.Equals(key) &&
                    entry.Readiness.ResourceCatalogIndex == resourceCatalogIndex &&
                    entry.Readiness.GroupClipIndex == groupClipIndex)
                {
                    return i;
                }
            }
            return -1;
        }

        ulong NextPageGeneration()
        {
            if (m_PageGeneration == ulong.MaxValue)
                throw new InvalidOperationException(
                    "Character Pose source readiness page generation was exhausted.");
            return m_PageGeneration + 1;
        }

        void RequireView(
            ulong completionIdentity,
            ulong pageGeneration)
        {
            if (!Matches(completionIdentity, pageGeneration))
                throw new InvalidOperationException(
                    "Character Pose source readiness page view is stale.");
        }
    }

    internal readonly struct CharacterPoseSourceReadinessPageView
    {
        internal CharacterPoseSourceReadinessPageView(
            CharacterPoseSourceReadinessPage page,
            ulong completionIdentity,
            ulong pageGeneration)
        {
            if (page == null ||
                !page.Matches(completionIdentity, pageGeneration))
            {
                throw new ArgumentException(
                    "Character Pose source readiness page view is invalid.");
            }
            m_Page = page;
            CompletionIdentity = completionIdentity;
            PageGeneration = pageGeneration;
        }

        readonly CharacterPoseSourceReadinessPage m_Page;
        internal ulong CompletionIdentity { get; }
        internal ulong PageGeneration { get; }
        internal int Count => RequirePage().RequireCount(
            CompletionIdentity,
            PageGeneration);
        internal bool IsValid =>
            m_Page != null &&
            m_Page.Matches(CompletionIdentity, PageGeneration);
        internal CharacterPoseSourceReadinessEntry Get(int index) =>
            RequirePage().Require(index, CompletionIdentity, PageGeneration);
        internal CharacterPoseSourceReadinessView Current =>
            RequirePage().Aggregate(
                CharacterPoseSourceReadinessCategory.Current,
                CompletionIdentity,
                PageGeneration);
        internal CharacterPoseSourceReadinessView DeferredTarget =>
            RequirePage().Aggregate(
                CharacterPoseSourceReadinessCategory.DeferredTarget,
                CompletionIdentity,
                PageGeneration);

        CharacterPoseSourceReadinessPage RequirePage() =>
            IsValid
                ? m_Page
                : throw new InvalidOperationException(
                    "Character Pose source readiness page view is stale.");
    }
}
