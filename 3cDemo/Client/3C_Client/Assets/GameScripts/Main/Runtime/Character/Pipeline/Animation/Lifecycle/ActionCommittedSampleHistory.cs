using System;
using System.Collections.Generic;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Animation.Lifecycle
{
    public readonly struct ActionSampleHistoryMutationLease
    {
        internal ActionSampleHistoryMutationLease(ulong identity)
        {
            Identity = identity;
        }

        public ulong Identity { get; }
        public bool IsValid => Identity != 0;
    }

    public sealed class ActionCommittedSampleHistory
    {
        enum MutationKind : byte
        {
            Upsert = 1,
            RemovePlayback = 2
        }

        readonly struct Mutation
        {
            internal Mutation(
                MutationKind kind,
                AnimationPlaybackId playbackId,
                ActionCommittedRawSample sample)
                : this(default, kind, playbackId, sample)
            {
            }

            Mutation(
                AnimationPresentationMutationJournalHeader header,
                MutationKind kind,
                AnimationPlaybackId playbackId,
                ActionCommittedRawSample sample)
            {
                Header = header;
                Kind = kind;
                PlaybackId = playbackId;
                Sample = sample;
            }

            internal AnimationPresentationMutationJournalHeader Header
            {
                get;
            }
            internal MutationKind Kind { get; }
            internal AnimationPlaybackId PlaybackId { get; }
            internal ActionCommittedRawSample Sample { get; }

            internal Mutation WithHeader(
                AnimationPresentationMutationJournalHeader header) =>
                new Mutation(
                    header,
                    Kind,
                    PlaybackId,
                    Sample);
        }

        sealed class Entry
        {
            internal readonly ActionCommittedRawSample[] Samples;
            internal bool Occupied;
            internal AnimationPlaybackId PlaybackId;
            internal int Count;

            internal Entry(int sampleCapacity)
            {
                Samples = new ActionCommittedRawSample[sampleCapacity];
            }

            internal void Clear()
            {
                if (Count > 0)
                    Array.Clear(Samples, 0, Count);
                Occupied = false;
                PlaybackId = default;
                Count = 0;
            }
        }

        readonly Entry[] m_Entries;
        readonly Entry[] m_PreparedEntries;
        readonly int[] m_PreparedCommittedIndices;
        readonly int[] m_PreparedTargetIndices;
        readonly bool[] m_ReservedEntrySlots;
        readonly Mutation[] m_Mutations;
        readonly ActionCommittedRawSample[] m_WindowSamples;
        readonly AnimationPlaybackId[] m_PrunePlaybackIds;
        readonly EventId[] m_PruneKeepFromEventIds;
        int m_MutationCount;
        int m_PruneCount;
        int m_WindowSampleCount;
        int m_PreparedEntryCount;
        ulong m_NextLeaseIdentity;
        ActionSampleHistoryMutationLease m_ActiveLease;
        bool m_Validated;

        public ActionCommittedSampleHistory(
            int playbackCapacity,
            int commandCapacity)
        {
            if (playbackCapacity <= 0 || commandCapacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(playbackCapacity));
            int sampleCapacity = checked(commandCapacity + 2);
            m_Entries = new Entry[playbackCapacity];
            m_PreparedEntries = new Entry[playbackCapacity];
            for (int i = 0; i < playbackCapacity; i++)
            {
                m_Entries[i] = new Entry(sampleCapacity);
                m_PreparedEntries[i] = new Entry(sampleCapacity);
            }
            m_PreparedCommittedIndices = new int[playbackCapacity];
            m_PreparedTargetIndices = new int[playbackCapacity];
            m_ReservedEntrySlots = new bool[playbackCapacity];
            m_Mutations = new Mutation[
                checked(commandCapacity + playbackCapacity * 2)];
            m_WindowSamples =
                new ActionCommittedRawSample[sampleCapacity];
            m_PrunePlaybackIds =
                new AnimationPlaybackId[playbackCapacity];
            m_PruneKeepFromEventIds = new EventId[playbackCapacity];
        }

        public ActionSampleHistoryMutationLease BeginMutation()
        {
            if (m_ActiveLease.IsValid)
            {
                throw new InvalidOperationException(
                    "Action committed sample history already has an active mutation.");
            }
            m_NextLeaseIdentity++;
            if (m_NextLeaseIdentity == 0)
                m_NextLeaseIdentity++;
            m_MutationCount = 0;
            m_PruneCount = 0;
            m_Validated = false;
            m_ActiveLease =
                new ActionSampleHistoryMutationLease(m_NextLeaseIdentity);
            return m_ActiveLease;
        }

        internal int MutationCapacity => m_Mutations.Length;

        public void ApplyCommands(
            ActionSampleHistoryMutationLease lease,
            IReadOnlyList<ActionPlaybackInboxEntry> entries)
        {
            RequireLease(lease);
            if (entries == null)
                throw new ArgumentNullException(nameof(entries));
            for (int i = 0; i < entries.Count; i++)
            {
                ActionAnimationPlaybackCommand command = entries[i].Command;
                if (command.Kind != ActionAnimationPlaybackCommandKind.Sample)
                    continue;
                AppendMutation(
                    new Mutation(
                        MutationKind.Upsert,
                        command.PlaybackId,
                        command.CommittedRawSample));
            }
        }

        public void ReplaceSample(
            ActionSampleHistoryMutationLease lease,
            AnimationPlaybackId playbackId,
            EventId targetEventId,
            ActionCommittedRawSample replacement)
        {
            RequireLease(lease);
            if (!playbackId.IsValid ||
                !targetEventId.IsValid ||
                !replacement.IsValid ||
                !targetEventId.Equals(replacement.EventId))
            {
                throw new ArgumentException(
                    "Action committed sample replacement is invalid.");
            }
            BuildWindowSamples(playbackId);
            if (FindEvent(m_WindowSamples, m_WindowSampleCount, targetEventId) < 0)
            {
                throw new InvalidOperationException(
                    "Action committed sample replacement target does not exist.");
            }
            AppendMutation(
                new Mutation(
                    MutationKind.Upsert,
                    playbackId,
                    replacement));
        }

        public bool TryGetProjectionWindow(
            ActionSampleHistoryMutationLease lease,
            AnimationPlaybackId playbackId,
            double presentationSampleTick,
            out ActionCommittedSampleWindow window)
        {
            RequireLease(lease);
            if (!playbackId.IsValid ||
                !double.IsFinite(presentationSampleTick) ||
                presentationSampleTick < 0d)
            {
                throw new ArgumentException(
                    "Action projection window request is invalid.");
            }
            BuildWindowSamples(playbackId);
            ValidateProjectedCapacity();
            if (m_WindowSampleCount == 0)
            {
                window = default;
                return false;
            }
            int previousIndex = 0;
            for (int i = 1; i < m_WindowSampleCount; i++)
            {
                if (presentationSampleTick <
                    m_WindowSamples[i].LocalLogicTick)
                {
                    break;
                }
                previousIndex = i;
            }
            bool hasNext = previousIndex + 1 < m_WindowSampleCount;
            window = new ActionCommittedSampleWindow(
                m_WindowSamples[previousIndex],
                hasNext ? m_WindowSamples[previousIndex + 1] : default,
                hasNext);
            if (previousIndex > 1)
            {
                SetPruneRequest(
                    playbackId,
                    m_WindowSamples[previousIndex - 1].EventId);
            }
            return true;
        }

        public void RemovePlayback(
            ActionSampleHistoryMutationLease lease,
            AnimationPlaybackId playbackId)
        {
            RequireLease(lease);
            if (!playbackId.IsValid)
            {
                throw new ArgumentException(
                    "Action playback id is invalid.",
                    nameof(playbackId));
            }
            AppendMutation(
                new Mutation(
                    MutationKind.RemovePlayback,
                    playbackId,
                    default));
        }

        public void ValidateFrame(ActionSampleHistoryMutationLease lease)
        {
            RequireLease(lease);
            ValidateMutationJournal();
            ValidateProjectedCapacity();
            ClearPreparedEntries();
            for (int i = 0; i < m_MutationCount; i++)
            {
                Mutation mutation = m_Mutations[i];
                if (!IsFirstMutationForPlayback(
                        i,
                        mutation.PlaybackId))
                {
                    continue;
                }
                PrepareEntry(mutation.PlaybackId);
            }
            for (int i = 0; i < m_PruneCount; i++)
            {
                AnimationPlaybackId playbackId =
                    m_PrunePlaybackIds[i];
                if (FindPreparedEntry(playbackId) < 0)
                    PrepareEntry(playbackId);
            }
            Array.Clear(
                m_ReservedEntrySlots,
                0,
                m_ReservedEntrySlots.Length);
            for (int i = 0; i < m_Entries.Length; i++)
            {
                if (m_Entries[i].Occupied)
                    m_ReservedEntrySlots[i] = true;
            }
            for (int i = 0; i < m_PreparedEntryCount; i++)
            {
                int committedIndex =
                    m_PreparedCommittedIndices[i];
                m_PreparedTargetIndices[i] = committedIndex;
                if (!m_PreparedEntries[i].Occupied &&
                    committedIndex >= 0)
                {
                    m_ReservedEntrySlots[committedIndex] = false;
                }
            }
            for (int i = 0; i < m_PreparedEntryCount; i++)
            {
                if (!m_PreparedEntries[i].Occupied ||
                    m_PreparedTargetIndices[i] >= 0)
                {
                    continue;
                }
                int targetIndex = FindFreeReservedEntry();
                if (targetIndex < 0)
                {
                    throw new InvalidOperationException(
                        "Action committed sample playback capacity was exceeded.");
                }
                m_PreparedTargetIndices[i] = targetIndex;
                m_ReservedEntrySlots[targetIndex] = true;
            }
            m_Validated = true;
        }

        public void Commit(ActionSampleHistoryMutationLease lease)
        {
            RequireLease(lease);
            if (!m_Validated)
            {
                throw new InvalidOperationException(
                    "Action committed sample history was not validated before Seal.");
            }
            for (int i = 0; i < m_PreparedEntryCount; i++)
            {
                Entry prepared = m_PreparedEntries[i];
                int targetIndex = m_PreparedTargetIndices[i];
                if (!prepared.Occupied && targetIndex >= 0)
                    m_Entries[targetIndex].Clear();
            }
            for (int i = 0; i < m_PreparedEntryCount; i++)
            {
                Entry prepared = m_PreparedEntries[i];
                if (!prepared.Occupied)
                    continue;
                Entry target =
                    m_Entries[m_PreparedTargetIndices[i]];
                target.Clear();
                target.Occupied = true;
                target.PlaybackId = prepared.PlaybackId;
                target.Count = prepared.Count;
                Array.Copy(
                    prepared.Samples,
                    0,
                    target.Samples,
                    0,
                    prepared.Count);
            }
            Close();
        }

        public void Discard(ActionSampleHistoryMutationLease lease)
        {
            RequireLease(lease);
            Close();
        }

        public void Reset()
        {
            if (m_ActiveLease.IsValid)
            {
                throw new InvalidOperationException(
                    "Action committed sample history cannot reset during mutation.");
            }
            for (int i = 0; i < m_Entries.Length; i++)
                m_Entries[i].Clear();
            m_MutationCount = 0;
            m_PruneCount = 0;
        }

        void BuildWindowSamples(AnimationPlaybackId playbackId)
        {
            m_WindowSampleCount = 0;
            int entryIndex = FindEntry(playbackId);
            if (entryIndex >= 0)
            {
                Entry entry = m_Entries[entryIndex];
                Array.Copy(
                    entry.Samples,
                    m_WindowSamples,
                    entry.Count);
                m_WindowSampleCount = entry.Count;
            }
            for (int i = 0; i < m_MutationCount; i++)
            {
                Mutation mutation = m_Mutations[i];
                if (!mutation.PlaybackId.Equals(playbackId))
                    continue;
                if (mutation.Kind == MutationKind.RemovePlayback)
                {
                    m_WindowSampleCount = 0;
                    continue;
                }
                int sampleIndex = FindEvent(
                    m_WindowSamples,
                    m_WindowSampleCount,
                    mutation.Sample.EventId);
                if (sampleIndex >= 0)
                {
                    m_WindowSamples[sampleIndex] = mutation.Sample;
                    continue;
                }
                if (m_WindowSampleCount == m_WindowSamples.Length)
                {
                    throw new InvalidOperationException(
                        "Action committed sample history capacity was exceeded.");
                }
                m_WindowSamples[m_WindowSampleCount++] = mutation.Sample;
            }
            SortSamples(m_WindowSamples, m_WindowSampleCount);
            ValidateOrder(m_WindowSamples, m_WindowSampleCount);
        }

        void PrepareEntry(AnimationPlaybackId playbackId)
        {
            if (m_PreparedEntryCount >= m_PreparedEntries.Length)
            {
                throw new InvalidOperationException(
                    "Action committed sample prepared entry capacity was exceeded.");
            }
            BuildWindowSamples(playbackId);
            ApplyPendingPrune(playbackId);
            Entry prepared =
                m_PreparedEntries[m_PreparedEntryCount];
            prepared.Clear();
            MutationKind finalKind =
                LastMutationKind(playbackId);
            int committedIndex = FindEntry(playbackId);
            m_PreparedCommittedIndices[m_PreparedEntryCount] =
                committedIndex;
            if (finalKind != MutationKind.RemovePlayback)
            {
                prepared.Occupied = true;
                prepared.PlaybackId = playbackId;
                prepared.Count = m_WindowSampleCount;
                Array.Copy(
                    m_WindowSamples,
                    0,
                    prepared.Samples,
                    0,
                    m_WindowSampleCount);
            }
            m_PreparedEntryCount++;
        }

        void ApplyPendingPrune(AnimationPlaybackId playbackId)
        {
            for (int i = 0; i < m_PruneCount; i++)
            {
                if (!m_PrunePlaybackIds[i].Equals(playbackId))
                    continue;
                int keepIndex = FindEvent(
                    m_WindowSamples,
                    m_WindowSampleCount,
                    m_PruneKeepFromEventIds[i]);
                if (keepIndex <= 0)
                    return;
                int remaining = m_WindowSampleCount - keepIndex;
                Array.Copy(
                    m_WindowSamples,
                    keepIndex,
                    m_WindowSamples,
                    0,
                    remaining);
                Array.Clear(
                    m_WindowSamples,
                    remaining,
                    m_WindowSampleCount - remaining);
                m_WindowSampleCount = remaining;
                return;
            }
        }

        int FindPreparedEntry(AnimationPlaybackId playbackId)
        {
            for (int i = 0; i < m_PreparedEntryCount; i++)
            {
                Entry prepared = m_PreparedEntries[i];
                if (prepared.PlaybackId.Equals(playbackId) ||
                    !prepared.Occupied &&
                    m_PreparedCommittedIndices[i] >= 0 &&
                    m_Entries[m_PreparedCommittedIndices[i]]
                        .PlaybackId.Equals(playbackId))
                {
                    return i;
                }
            }
            return -1;
        }

        int FindFreeReservedEntry()
        {
            for (int i = 0; i < m_ReservedEntrySlots.Length; i++)
            {
                if (!m_ReservedEntrySlots[i])
                    return i;
            }
            return -1;
        }

        void ClearPreparedEntries()
        {
            for (int i = 0; i < m_PreparedEntryCount; i++)
                m_PreparedEntries[i].Clear();
            m_PreparedEntryCount = 0;
        }

        void AppendMutation(in Mutation mutation)
        {
            m_Validated = false;
            if (m_MutationCount == m_Mutations.Length)
            {
                throw new InvalidOperationException(
                    "Action sample history mutation journal capacity was exceeded.");
            }
            for (int i = 0; i < m_MutationCount; i++)
            {
                Mutation existing = m_Mutations[i];
                if (!existing.PlaybackId.Equals(mutation.PlaybackId))
                    continue;
                if (existing.Kind == MutationKind.RemovePlayback ||
                    existing.Kind == MutationKind.Upsert &&
                    mutation.Kind == MutationKind.Upsert &&
                    existing.Sample.EventId.Equals(mutation.Sample.EventId))
                {
                    throw new InvalidOperationException(
                        "Action sample history mutation journal contains a duplicate or invalid order.");
                }
            }
            int payloadIndex = m_MutationCount;
            var header = new AnimationPresentationMutationJournalHeader(
                AnimationPresentationMutationOwnerDomain.ActionSampleHistory,
                mutation.Kind == MutationKind.Upsert
                    ? AnimationPresentationMutationOperationKind.Upsert
                    : AnimationPresentationMutationOperationKind.Remove,
                payloadIndex,
                payloadIndex);
            m_Mutations[payloadIndex] = mutation.WithHeader(header);
            m_MutationCount++;
        }

        void ValidateMutationJournal()
        {
            for (int i = 0; i < m_MutationCount; i++)
            {
                Mutation mutation = m_Mutations[i];
                AnimationPresentationMutationJournalHeader header =
                    mutation.Header;
                AnimationPresentationMutationOperationKind operation =
                    mutation.Kind == MutationKind.Upsert
                        ? AnimationPresentationMutationOperationKind.Upsert
                        : AnimationPresentationMutationOperationKind.Remove;
                if (!header.IsValid ||
                    header.OwnerDomain !=
                        AnimationPresentationMutationOwnerDomain.ActionSampleHistory ||
                    header.OperationKind != operation ||
                    header.PayloadIndex != i ||
                    header.SequenceIndex != i ||
                    !mutation.PlaybackId.IsValid ||
                    mutation.Kind == MutationKind.Upsert &&
                    !mutation.Sample.IsValid)
                {
                    throw new InvalidOperationException(
                        "Action sample history mutation journal identity or order is invalid.");
                }
            }
        }

        void ValidateProjectedCapacity()
        {
            int occupied = 0;
            for (int i = 0; i < m_Entries.Length; i++)
            {
                Entry entry = m_Entries[i];
                if (!entry.Occupied)
                    continue;
                if (LastMutationKind(entry.PlaybackId) !=
                    MutationKind.RemovePlayback)
                {
                    occupied++;
                }
            }
            for (int i = 0; i < m_MutationCount; i++)
            {
                Mutation mutation = m_Mutations[i];
                if (FindEntry(mutation.PlaybackId) >= 0 ||
                    !IsFirstMutationForPlayback(i, mutation.PlaybackId) ||
                    LastMutationKind(mutation.PlaybackId) ==
                        MutationKind.RemovePlayback)
                {
                    continue;
                }
                occupied++;
            }
            if (occupied > m_Entries.Length)
            {
                throw new InvalidOperationException(
                    "Action committed sample playback capacity was exceeded.");
            }
        }

        MutationKind LastMutationKind(AnimationPlaybackId playbackId)
        {
            for (int i = m_MutationCount - 1; i >= 0; i--)
            {
                if (m_Mutations[i].PlaybackId.Equals(playbackId))
                    return m_Mutations[i].Kind;
            }
            return default;
        }

        bool IsFirstMutationForPlayback(
            int mutationIndex,
            AnimationPlaybackId playbackId)
        {
            for (int i = 0; i < mutationIndex; i++)
            {
                if (m_Mutations[i].PlaybackId.Equals(playbackId))
                    return false;
            }
            return true;
        }

        void SetPruneRequest(
            AnimationPlaybackId playbackId,
            EventId keepFromEventId)
        {
            m_Validated = false;
            for (int i = 0; i < m_PruneCount; i++)
            {
                if (!m_PrunePlaybackIds[i].Equals(playbackId))
                    continue;
                m_PruneKeepFromEventIds[i] = keepFromEventId;
                return;
            }
            if (m_PruneCount == m_PrunePlaybackIds.Length)
            {
                throw new InvalidOperationException(
                    "Action sample prune cursor capacity was exceeded.");
            }
            m_PrunePlaybackIds[m_PruneCount] = playbackId;
            m_PruneKeepFromEventIds[m_PruneCount] = keepFromEventId;
            m_PruneCount++;
        }

        int FindEntry(AnimationPlaybackId playbackId)
        {
            for (int i = 0; i < m_Entries.Length; i++)
            {
                if (m_Entries[i].Occupied &&
                    m_Entries[i].PlaybackId.Equals(playbackId))
                {
                    return i;
                }
            }
            return -1;
        }

        void RequireLease(ActionSampleHistoryMutationLease lease)
        {
            if (!lease.IsValid ||
                !m_ActiveLease.IsValid ||
                lease.Identity != m_ActiveLease.Identity)
            {
                throw new InvalidOperationException(
                    "Action committed sample history lease is invalid.");
            }
        }

        void Close()
        {
            ClearPreparedEntries();
            if (m_MutationCount > 0)
                Array.Clear(m_Mutations, 0, m_MutationCount);
            if (m_PruneCount > 0)
            {
                Array.Clear(m_PrunePlaybackIds, 0, m_PruneCount);
                Array.Clear(m_PruneKeepFromEventIds, 0, m_PruneCount);
            }
            m_MutationCount = 0;
            m_PruneCount = 0;
            m_WindowSampleCount = 0;
            m_ActiveLease = default;
            m_Validated = false;
        }

        static int FindEvent(
            ActionCommittedRawSample[] samples,
            int count,
            EventId eventId)
        {
            for (int i = 0; i < count; i++)
            {
                if (samples[i].EventId.Equals(eventId))
                    return i;
            }
            return -1;
        }

        static void SortSamples(
            ActionCommittedRawSample[] samples,
            int count)
        {
            for (int i = 1; i < count; i++)
            {
                ActionCommittedRawSample value = samples[i];
                int index = i - 1;
                while (index >= 0 &&
                       CompareSamples(samples[index], value) > 0)
                {
                    samples[index + 1] = samples[index];
                    index--;
                }
                samples[index + 1] = value;
            }
        }

        static int CompareSamples(
            ActionCommittedRawSample left,
            ActionCommittedRawSample right)
        {
            int tick = left.LocalLogicTick.CompareTo(right.LocalLogicTick);
            return tick != 0
                ? tick
                : left.CommittedSequence.CompareTo(
                    right.CommittedSequence);
        }

        static void ValidateOrder(
            ActionCommittedRawSample[] samples,
            int count)
        {
            for (int i = 1; i < count; i++)
            {
                ActionCommittedRawSample previous = samples[i - 1];
                ActionCommittedRawSample current = samples[i];
                if (previous.LocalLogicTick == current.LocalLogicTick &&
                    previous.CommittedSequence == current.CommittedSequence)
                {
                    throw new InvalidOperationException(
                        "Action committed sample history duplicates a tick and sequence.");
                }
                if (current.ContinuousVisualTime + 0.000001d <
                    previous.ContinuousVisualTime)
                {
                    throw new InvalidOperationException(
                        "Action committed raw visual time moved backwards.");
                }
            }
        }
    }
}
