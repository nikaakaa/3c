using System;
using System.Collections.Generic;

namespace ThirdPersonSimulation
{
    public sealed class PredictionSnapshotHistory
    {
        readonly SortedDictionary<int, CharacterSimulationSnapshot> snapshots = new SortedDictionary<int, CharacterSimulationSnapshot>();

        public PredictionSnapshotHistory(int capacity)
        {
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "Capacity must be positive.");

            Capacity = capacity;
        }

        public int Capacity { get; }
        public int Count => snapshots.Count;

        public void Write(in CharacterSimulationSnapshot snapshot)
        {
            snapshots[snapshot.Tick.Value] = snapshot;
            TrimToCapacity();
        }

        public bool TryGet(SimulationTick tick, out CharacterSimulationSnapshot snapshot)
        {
            return snapshots.TryGetValue(tick.Value, out snapshot);
        }

        public bool TryGetLatestRecoverableTick(out SimulationTick tick)
        {
            if (snapshots.Count == 0)
            {
                tick = default;
                return false;
            }

            int latest = 0;
            foreach (int key in snapshots.Keys)
                latest = key;

            tick = new SimulationTick(latest);
            return true;
        }

        public void TrimConfirmedBefore(SimulationTick confirmedTick)
        {
            List<int> keys = new List<int>();
            foreach (int tick in snapshots.Keys)
            {
                if (tick < confirmedTick.Value)
                    keys.Add(tick);
            }

            for (int i = 0; i < keys.Count; i++)
                snapshots.Remove(keys[i]);
        }

        void TrimToCapacity()
        {
            while (snapshots.Count > Capacity)
                snapshots.Remove(FirstKey());
        }

        int FirstKey()
        {
            foreach (int tick in snapshots.Keys)
                return tick;

            return 0;
        }
    }
}
