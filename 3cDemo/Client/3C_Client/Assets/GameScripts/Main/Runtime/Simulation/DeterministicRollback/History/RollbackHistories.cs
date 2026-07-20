using System;
using System.Collections.Generic;
using ThirdPersonSimulation.Fixed;

namespace ThirdPersonSimulation.DeterministicRollback
{
    public enum RollbackInputComparison : byte
    {
        AwaitingCanonical = 1,
        Match = 2,
        Mismatch = 3
    }

    public sealed class RollbackInputHistoryEntry
    {
        public RollbackInputHistoryEntry(
            SimulationTick tick,
            RollbackCanonicalInputBundle predicted,
            RollbackCanonicalInputBundle canonical)
        {
            if (!tick.IsValid || predicted == null && canonical == null ||
                predicted != null && predicted.Tick != tick || canonical != null && canonical.Tick != tick)
            {
                throw new ArgumentException("Rollback input history entry is invalid.");
            }
            Tick = tick;
            Predicted = predicted;
            Canonical = canonical;
        }

        public SimulationTick Tick { get; }
        public RollbackCanonicalInputBundle Predicted { get; }
        public RollbackCanonicalInputBundle Canonical { get; }
        public RollbackInputComparison Comparison => Canonical == null
            ? RollbackInputComparison.AwaitingCanonical
            : Predicted != null && Predicted.GameplayHash.Equals(Canonical.GameplayHash)
                ? RollbackInputComparison.Match
                : RollbackInputComparison.Mismatch;
    }

    public sealed class RollbackInputHistory
    {
        sealed class MutableEntry
        {
            public RollbackCanonicalInputBundle Predicted;
            public RollbackCanonicalInputBundle Canonical;
        }

        readonly int m_Capacity;
        readonly SortedDictionary<ulong, MutableEntry> m_Entries = new SortedDictionary<ulong, MutableEntry>();

        public RollbackInputHistory(int capacity)
        {
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));
            m_Capacity = capacity;
        }

        public int Count => m_Entries.Count;
        public ulong FloorTick => FirstTick();
        public ulong CeilingTick => LastTick();

        public bool RecordPredicted(RollbackCanonicalInputBundle bundle)
        {
            return Set(bundle, false);
        }

        public bool RecordCanonical(RollbackCanonicalInputBundle bundle)
        {
            return Set(bundle, true);
        }

        public RollbackInputHistoryEntry GetRequired(SimulationTick tick)
        {
            if (!m_Entries.TryGetValue(tick.Value, out MutableEntry entry))
                throw new KeyNotFoundException($"Rollback input history has no Tick '{tick}'.");
            return new RollbackInputHistoryEntry(tick, entry.Predicted, entry.Canonical);
        }

        public IReadOnlyList<RollbackInputHistoryEntry> CaptureEntries()
        {
            var result = new List<RollbackInputHistoryEntry>(m_Entries.Count);
            foreach (KeyValuePair<ulong, MutableEntry> pair in m_Entries)
            {
                result.Add(new RollbackInputHistoryEntry(
                    new SimulationTick(pair.Key),
                    pair.Value.Predicted,
                    pair.Value.Canonical));
            }
            return result.AsReadOnly();
        }

        public void RestoreEntries(IEnumerable<RollbackInputHistoryEntry> entries)
        {
            m_Entries.Clear();
            if (entries == null)
                return;
            foreach (RollbackInputHistoryEntry entry in entries)
            {
                if (entry == null)
                    throw new ArgumentException("Rollback input history restore contains a missing entry.", nameof(entries));
                if (entry.Predicted != null)
                    Set(entry.Predicted, false);
                if (entry.Canonical != null)
                    Set(entry.Canonical, true);
            }
        }

        public bool TryFindEarliestMismatch(out SimulationTick tick)
        {
            foreach (KeyValuePair<ulong, MutableEntry> pair in m_Entries)
            {
                if (pair.Value.Canonical != null &&
                    (pair.Value.Predicted == null || !pair.Value.Predicted.GameplayHash.Equals(pair.Value.Canonical.GameplayHash)))
                {
                    tick = new SimulationTick(pair.Key);
                    return true;
                }
            }
            tick = default;
            return false;
        }

        public void DiscardThrough(ulong confirmedTick)
        {
            RemoveThrough(m_Entries, confirmedTick);
        }

        bool Set(RollbackCanonicalInputBundle bundle, bool canonical)
        {
            if (bundle == null)
                throw new ArgumentNullException(nameof(bundle));
            if (!m_Entries.TryGetValue(bundle.Tick.Value, out MutableEntry entry))
            {
                if (m_Entries.Count >= m_Capacity)
                    throw new InvalidOperationException("Rollback input history capacity is exhausted before confirmed-horizon release.");
                entry = new MutableEntry();
                m_Entries.Add(bundle.Tick.Value, entry);
            }
            RollbackCanonicalInputBundle current = canonical ? entry.Canonical : entry.Predicted;
            if (current != null)
            {
                if (!canonical)
                {
                    if (current.BundleHash.Equals(bundle.BundleHash))
                        return false;
                    entry.Predicted = bundle;
                    return true;
                }
                if (current.BundleHash.Equals(bundle.BundleHash))
                    return false;
                throw new InvalidOperationException($"Rollback canonical Tick '{bundle.Tick}' changed after publication.");
            }
            if (canonical)
                entry.Canonical = bundle;
            else
                entry.Predicted = bundle;
            return true;
        }

        ulong FirstTick()
        {
            foreach (ulong tick in m_Entries.Keys)
                return tick;
            return 0;
        }

        ulong LastTick()
        {
            ulong result = 0;
            foreach (ulong tick in m_Entries.Keys)
                result = tick;
            return result;
        }

        internal static void RemoveThrough<T>(SortedDictionary<ulong, T> values, ulong tick)
        {
            var remove = new List<ulong>();
            foreach (ulong candidate in values.Keys)
            {
                if (candidate > tick)
                    break;
                remove.Add(candidate);
            }
            for (int i = 0; i < remove.Count; i++)
                values.Remove(remove[i]);
        }
    }

    public sealed class RollbackSnapshotHistory
    {
        sealed class Entry
        {
            public Entry(FixedSimulationSessionSnapshot snapshot)
            {
                Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
            }

            public FixedSimulationSessionSnapshot Snapshot { get; }
        }

        readonly int m_Capacity;
        readonly SortedDictionary<ulong, Entry> m_Entries = new SortedDictionary<ulong, Entry>();

        public RollbackSnapshotHistory(int capacity)
        {
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));
            m_Capacity = capacity;
        }

        public int Count => m_Entries.Count;
        public ulong FloorTick => FirstTick();
        public ulong CeilingTick => LastTick();

        public void Capture(FixedSimulationSessionSnapshot snapshot, bool replaceExisting)
        {
            if (snapshot == null)
                throw new ArgumentNullException(nameof(snapshot));
            if (m_Entries.TryGetValue(snapshot.Tick.Value, out Entry current))
            {
                if (current.Snapshot.SnapshotHash.Equals(snapshot.SnapshotHash))
                    return;
                if (!replaceExisting)
                    throw new InvalidOperationException($"Rollback snapshot Tick '{snapshot.Tick}' changed outside replay.");
                m_Entries[snapshot.Tick.Value] = new Entry(snapshot);
                return;
            }
            if (m_Entries.Count >= m_Capacity)
                throw new InvalidOperationException("Rollback snapshot history capacity is exhausted before confirmed-horizon release.");
            m_Entries.Add(snapshot.Tick.Value, new Entry(snapshot));
        }

        public FixedSimulationSessionSnapshot GetRequired(SimulationTick tick)
        {
            if (!m_Entries.TryGetValue(tick.Value, out Entry entry))
                throw new KeyNotFoundException($"Rollback snapshot history has no Tick '{tick}'.");
            FixedSimulationSessionSnapshot snapshot = entry.Snapshot;
            if (snapshot.Tick != tick)
                throw new InvalidOperationException($"Rollback snapshot history Tick '{tick}' failed canonical hash verification.");
            return snapshot;
        }

        public void DiscardBefore(ulong floorTick)
        {
            if (floorTick == 0)
                return;
            RollbackInputHistory.RemoveThrough(m_Entries, floorTick - 1);
        }

        ulong FirstTick()
        {
            foreach (ulong tick in m_Entries.Keys)
                return tick;
            return 0;
        }

        ulong LastTick()
        {
            ulong result = 0;
            foreach (ulong tick in m_Entries.Keys)
                result = tick;
            return result;
        }
    }

    public sealed class RollbackStateHashHistory
    {
        readonly int m_Capacity;
        readonly Dictionary<string, SortedDictionary<ulong, RollbackStateHashReport>> m_Peers =
            new Dictionary<string, SortedDictionary<ulong, RollbackStateHashReport>>(StringComparer.Ordinal);
        int m_Count;

        public RollbackStateHashHistory(int capacity)
        {
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));
            m_Capacity = capacity;
        }

        public int Count => m_Count;

        public void Record(string peerId, RollbackStateHashReport report)
        {
            peerId = SimulationIdentity.Require(peerId, nameof(peerId));
            if (report == null)
                throw new ArgumentNullException(nameof(report));
            if (!m_Peers.TryGetValue(peerId, out SortedDictionary<ulong, RollbackStateHashReport> entries))
            {
                entries = new SortedDictionary<ulong, RollbackStateHashReport>();
                m_Peers.Add(peerId, entries);
            }
            if (entries.TryGetValue(report.Tick.Value, out RollbackStateHashReport current))
            {
                if (!current.WorldHash.Equals(report.WorldHash))
                    throw new InvalidOperationException($"Rollback peer '{peerId}' reported two hashes for Tick '{report.Tick}'.");
                return;
            }
            if (m_Count >= m_Capacity)
                throw new InvalidOperationException("Rollback state-hash history capacity is exhausted before confirmed-horizon release.");
            entries.Add(report.Tick.Value, report);
            m_Count++;
        }

        public bool TryGet(string peerId, SimulationTick tick, out RollbackStateHashReport report)
        {
            report = null;
            return m_Peers.TryGetValue(peerId, out SortedDictionary<ulong, RollbackStateHashReport> entries) &&
                   entries.TryGetValue(tick.Value, out report);
        }

        public void DiscardThrough(ulong confirmedTick)
        {
            foreach (SortedDictionary<ulong, RollbackStateHashReport> entries in m_Peers.Values)
            {
                int before = entries.Count;
                RollbackInputHistory.RemoveThrough(entries, confirmedTick);
                m_Count -= before - entries.Count;
            }
        }
    }
}
