using System;
using System.Collections.Generic;
using ThirdPersonSimulation;

namespace ThirdPersonSimulation.ServerAuthoritative
{
    internal sealed class ServerAuthoritativePredictionDispositionJournal
    {
        readonly int m_Capacity;
        SortedDictionary<EventId, ServerAuthoritativeJournalEntry> m_Entries =
            new SortedDictionary<EventId, ServerAuthoritativeJournalEntry>();

        public ServerAuthoritativePredictionDispositionJournal(int historyCapacity)
        {
            if (historyCapacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(historyCapacity));
            m_Capacity = checked(historyCapacity * 64);
        }

        public ulong Cursor { get; private set; }
        public int Count => m_Entries.Count;
        public int LastRejectedCount { get; private set; }

        public bool WasCommitted(EventId eventId) =>
            m_Entries.TryGetValue(eventId, out ServerAuthoritativeJournalEntry entry) &&
            entry.Disposition != ServerAuthoritativeEventDisposition.PredictedRejected;

        public void Record(ServerAuthoritativeJournalEntry entry, ulong firstRetainedHistoryTick)
        {
            Restore(PrepareRecord(entry, firstRetainedHistoryTick));
        }

        public ServerAuthoritativePredictionJournalCheckpoint PrepareConfirmation(
            SimulationTick authorityTick,
            ServerAuthoritativeEventHorizon horizon,
            ulong firstRetainedHistoryTick)
        {
            var entries = CopyEntries(m_Entries);
            ulong cursor = Cursor;
            int rejectedCount = 0;
            var updates = new List<ServerAuthoritativeJournalEntry>();
            foreach (ServerAuthoritativeJournalEntry entry in m_Entries.Values)
            {
                if (entry.Tick.Value > authorityTick.Value ||
                    entry.Disposition == ServerAuthoritativeEventDisposition.AuthorityConfirmed ||
                    entry.Disposition == ServerAuthoritativeEventDisposition.PredictedRejected)
                {
                    continue;
                }
                bool confirmed = !horizon.IsEmpty &&
                    (entry.Sequence < horizon.Sequence ||
                     entry.Sequence == horizon.Sequence && entry.EventId.Equals(horizon.EventId));
                if (!confirmed)
                    rejectedCount++;
                updates.Add(new ServerAuthoritativeJournalEntry(
                    entry.EventId,
                    entry.Tick,
                    entry.Sequence,
                    confirmed
                        ? ServerAuthoritativeEventDisposition.AuthorityConfirmed
                        : ServerAuthoritativeEventDisposition.PredictedRejected));
            }
            for (int i = 0; i < updates.Count; i++)
            {
                Record(
                    entries,
                    ref cursor,
                    updates[i],
                    firstRetainedHistoryTick,
                    m_Capacity);
            }
            if (!horizon.IsEmpty && !entries.ContainsKey(horizon.EventId))
            {
                Record(
                    entries,
                    ref cursor,
                    new ServerAuthoritativeJournalEntry(
                        horizon.EventId,
                        authorityTick,
                        horizon.Sequence,
                        ServerAuthoritativeEventDisposition.AuthorityConfirmed),
                    firstRetainedHistoryTick,
                    m_Capacity);
            }
            return new ServerAuthoritativePredictionJournalCheckpoint(entries, cursor, rejectedCount);
        }

        public ServerAuthoritativePredictionJournalCheckpoint PreparePrune(
            ServerAuthoritativePredictionJournalCheckpoint checkpoint,
            ulong firstRetainedHistoryTick)
        {
            if (checkpoint == null)
                throw new ArgumentNullException(nameof(checkpoint));
            var entries = CopyEntries(checkpoint.Entries);
            Prune(entries, firstRetainedHistoryTick);
            return new ServerAuthoritativePredictionJournalCheckpoint(
                entries,
                checkpoint.Cursor,
                checkpoint.LastRejectedCount);
        }

        public void Prune(ulong firstRetainedHistoryTick)
        {
            Prune(m_Entries, firstRetainedHistoryTick);
        }

        public ServerAuthoritativePredictionJournalCheckpoint Capture() =>
            new ServerAuthoritativePredictionJournalCheckpoint(m_Entries, Cursor, LastRejectedCount);

        public void Restore(ServerAuthoritativePredictionJournalCheckpoint checkpoint)
        {
            if (checkpoint == null)
                throw new ArgumentNullException(nameof(checkpoint));
            m_Entries = CopyEntries(checkpoint.Entries);
            Cursor = checkpoint.Cursor;
            LastRejectedCount = checkpoint.LastRejectedCount;
        }

        ServerAuthoritativePredictionJournalCheckpoint PrepareRecord(
            ServerAuthoritativeJournalEntry entry,
            ulong firstRetainedHistoryTick)
        {
            var entries = CopyEntries(m_Entries);
            ulong cursor = Cursor;
            Record(entries, ref cursor, entry, firstRetainedHistoryTick, m_Capacity);
            return new ServerAuthoritativePredictionJournalCheckpoint(entries, cursor, LastRejectedCount);
        }

        static void Record(
            SortedDictionary<EventId, ServerAuthoritativeJournalEntry> entries,
            ref ulong cursor,
            ServerAuthoritativeJournalEntry entry,
            ulong firstRetainedHistoryTick,
            int capacity)
        {
            if (entries.TryGetValue(entry.EventId, out ServerAuthoritativeJournalEntry existing))
            {
                if (existing.Disposition == ServerAuthoritativeEventDisposition.AuthorityConfirmed ||
                    existing.Tick == entry.Tick && existing.Sequence == entry.Sequence && existing.Disposition == entry.Disposition)
                {
                    return;
                }
            }
            else
            {
                Prune(entries, firstRetainedHistoryTick);
                if (entries.Count >= capacity)
                    throw new InvalidOperationException("Prediction disposition journal capacity is exhausted by live predicted events.");
            }
            entries[entry.EventId] = entry;
            cursor = checked(cursor + 1);
        }

        static void Prune(
            SortedDictionary<EventId, ServerAuthoritativeJournalEntry> entries,
            ulong firstRetainedHistoryTick)
        {
            var remove = new List<EventId>();
            foreach (KeyValuePair<EventId, ServerAuthoritativeJournalEntry> pair in entries)
            {
                if (pair.Value.Tick.Value >= firstRetainedHistoryTick ||
                    pair.Value.Disposition == ServerAuthoritativeEventDisposition.PredictedCommitted ||
                    pair.Value.Disposition == ServerAuthoritativeEventDisposition.SuppressedDuplicate)
                {
                    continue;
                }
                remove.Add(pair.Key);
            }
            for (int i = 0; i < remove.Count; i++)
                entries.Remove(remove[i]);
        }

        static SortedDictionary<EventId, ServerAuthoritativeJournalEntry> CopyEntries(
            IEnumerable<KeyValuePair<EventId, ServerAuthoritativeJournalEntry>> entries)
        {
            var copy = new SortedDictionary<EventId, ServerAuthoritativeJournalEntry>();
            foreach (KeyValuePair<EventId, ServerAuthoritativeJournalEntry> pair in entries)
                copy.Add(pair.Key, pair.Value);
            return copy;
        }
    }

    internal sealed class ServerAuthoritativePredictionJournalCheckpoint
    {
        public ServerAuthoritativePredictionJournalCheckpoint(
            IEnumerable<KeyValuePair<EventId, ServerAuthoritativeJournalEntry>> entries,
            ulong cursor,
            int lastRejectedCount)
        {
            Entries = new List<KeyValuePair<EventId, ServerAuthoritativeJournalEntry>>(entries).AsReadOnly();
            Cursor = cursor;
            LastRejectedCount = lastRejectedCount;
        }

        public IReadOnlyList<KeyValuePair<EventId, ServerAuthoritativeJournalEntry>> Entries { get; }
        public ulong Cursor { get; }
        public int LastRejectedCount { get; }
    }
}
