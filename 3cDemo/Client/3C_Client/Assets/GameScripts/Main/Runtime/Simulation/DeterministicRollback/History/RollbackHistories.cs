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
        readonly Stack<MutableEntry> m_FreeEntries;

        public RollbackInputHistory(int capacity)
        {
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));
            m_Capacity = capacity;
            m_FreeEntries = new Stack<MutableEntry>(capacity);
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

        internal bool TryGetBundles(
            SimulationTick tick,
            out RollbackCanonicalInputBundle predicted,
            out RollbackCanonicalInputBundle canonical)
        {
            predicted = null;
            canonical = null;
            if (!m_Entries.TryGetValue(tick.Value, out MutableEntry entry))
                return false;
            predicted = entry.Predicted;
            canonical = entry.Canonical;
            return true;
        }

        public RollbackInputHistoryEntry[] CaptureEntries()
        {
            var result = new RollbackInputHistoryEntry[m_Entries.Count];
            int index = 0;
            foreach (KeyValuePair<ulong, MutableEntry> pair in m_Entries)
            {
                result[index++] = new RollbackInputHistoryEntry(
                    new SimulationTick(pair.Key),
                    pair.Value.Predicted,
                    pair.Value.Canonical);
            }
            return result;
        }

        public void RestoreEntries(IReadOnlyList<RollbackInputHistoryEntry> entries)
        {
            foreach (KeyValuePair<ulong, MutableEntry> pair in m_Entries)
                Release(pair.Value);
            m_Entries.Clear();
            if (entries == null)
                return;
            for (int i = 0; i < entries.Count; i++)
            {
                RollbackInputHistoryEntry entry = entries[i];
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

        internal bool TryFindEarliestAppliedMismatch(
            IReadOnlyDictionary<ulong, StableHash> appliedGameplayHashes,
            out SimulationTick tick)
        {
            foreach (KeyValuePair<ulong, MutableEntry> pair in m_Entries)
            {
                if (pair.Value.Canonical == null ||
                    !appliedGameplayHashes.TryGetValue(pair.Key, out StableHash applied) ||
                    applied.Equals(pair.Value.Canonical.GameplayHash))
                {
                    continue;
                }
                tick = new SimulationTick(pair.Key);
                return true;
            }
            tick = default;
            return false;
        }

        public void DiscardThrough(ulong confirmedTick)
        {
            while (m_Entries.Count != 0)
            {
                ulong candidate = 0;
                foreach (KeyValuePair<ulong, MutableEntry> pair in m_Entries)
                {
                    candidate = pair.Key;
                    break;
                }
                if (candidate > confirmedTick)
                    break;
                m_Entries.Remove(candidate, out MutableEntry entry);
                Release(entry);
            }
        }

        bool Set(RollbackCanonicalInputBundle bundle, bool canonical)
        {
            if (bundle == null)
                throw new ArgumentNullException(nameof(bundle));
            if (!m_Entries.TryGetValue(bundle.Tick.Value, out MutableEntry entry))
            {
                if (m_Entries.Count >= m_Capacity)
                    throw new InvalidOperationException("Rollback input history capacity is exhausted before confirmed-horizon release.");
                entry = m_FreeEntries.Count == 0 ? new MutableEntry() : m_FreeEntries.Pop();
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
            foreach (KeyValuePair<ulong, MutableEntry> pair in m_Entries)
                return pair.Key;
            return 0;
        }

        ulong LastTick()
        {
            ulong result = 0;
            foreach (KeyValuePair<ulong, MutableEntry> pair in m_Entries)
                result = pair.Key;
            return result;
        }

        internal static void RemoveThrough<T>(SortedDictionary<ulong, T> values, ulong tick)
        {
            while (values.Count != 0)
            {
                ulong candidate = 0;
                foreach (KeyValuePair<ulong, T> value in values)
                {
                    candidate = value.Key;
                    break;
                }
                if (candidate > tick)
                    break;
                values.Remove(candidate);
            }
        }

        void Release(MutableEntry entry)
        {
            entry.Predicted = null;
            entry.Canonical = null;
            m_FreeEntries.Push(entry);
        }
    }

    public sealed class RollbackSnapshotHistory
    {
        readonly int m_Capacity;
        readonly SortedDictionary<ulong, FixedSimulationSessionSnapshot> m_Entries =
            new SortedDictionary<ulong, FixedSimulationSessionSnapshot>();

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
            if (m_Entries.TryGetValue(snapshot.Tick.Value, out FixedSimulationSessionSnapshot current))
            {
                if (current.SnapshotHash.Equals(snapshot.SnapshotHash))
                    return;
                if (!replaceExisting)
                    throw new InvalidOperationException($"Rollback snapshot Tick '{snapshot.Tick}' changed outside replay.");
                m_Entries[snapshot.Tick.Value] = snapshot;
                return;
            }
            if (m_Entries.Count >= m_Capacity)
                throw new InvalidOperationException("Rollback snapshot history capacity is exhausted before confirmed-horizon release.");
            m_Entries.Add(snapshot.Tick.Value, snapshot);
        }

        public FixedSimulationSessionSnapshot GetRequired(SimulationTick tick)
        {
            if (!m_Entries.TryGetValue(tick.Value, out FixedSimulationSessionSnapshot snapshot))
                throw new KeyNotFoundException($"Rollback snapshot history has no Tick '{tick}'.");
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
            foreach (KeyValuePair<ulong, FixedSimulationSessionSnapshot> pair in m_Entries)
                return pair.Key;
            return 0;
        }

        ulong LastTick()
        {
            ulong result = 0;
            foreach (KeyValuePair<ulong, FixedSimulationSessionSnapshot> pair in m_Entries)
                result = pair.Key;
            return result;
        }
    }

}
