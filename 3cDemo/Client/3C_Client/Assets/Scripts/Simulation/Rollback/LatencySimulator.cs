using System.Collections.Generic;

namespace ThirdPersonSimulation
{
    public readonly struct LatencyFrameEntry
    {
        public LatencyFrameEntry(SimulationTick tick, PredictionInputFrame frame, SimulationTick arrivalTick)
        {
            Tick = tick;
            Frame = frame;
            ArrivalTick = arrivalTick;
        }

        public SimulationTick Tick { get; }
        public PredictionInputFrame Frame { get; }
        public SimulationTick ArrivalTick { get; }
        public bool HasArrived(SimulationTick currentTick) => currentTick >= ArrivalTick;
    }

    public sealed class LatencySimulator
    {
        readonly SortedDictionary<int, LatencyFrameEntry> entries = new SortedDictionary<int, LatencyFrameEntry>();

        public LatencySimulator(int capacity)
        {
            Capacity = capacity;
        }

        public int Capacity { get; }
        public int Count => entries.Count;

        public void Write(in PredictionInputFrame frame, int delayTicks)
        {
            SimulationTick arrivalTick = frame.Tick.Add(delayTicks);
            entries[frame.Tick.Value] = new LatencyFrameEntry(frame.Tick, frame, arrivalTick);
            TrimToCapacity();
        }

        public bool HasArrived(SimulationTick tick, SimulationTick currentTick)
        {
            if (!entries.TryGetValue(tick.Value, out LatencyFrameEntry entry))
                return false;

            return entry.HasArrived(currentTick);
        }

        public bool TryGet(SimulationTick tick, SimulationTick currentTick, out PredictionInputFrame frame)
        {
            if (!entries.TryGetValue(tick.Value, out LatencyFrameEntry entry))
            {
                frame = default;
                return false;
            }

            if (!entry.HasArrived(currentTick))
            {
                frame = default;
                return false;
            }

            frame = entry.Frame;
            return true;
        }

        public void TrimConfirmedBefore(SimulationTick confirmedTick)
        {
            List<int> keys = new List<int>();
            foreach (int tick in entries.Keys)
            {
                if (tick < confirmedTick.Value)
                    keys.Add(tick);
            }

            for (int i = 0; i < keys.Count; i++)
                entries.Remove(keys[i]);
        }

        void TrimToCapacity()
        {
            while (entries.Count > Capacity)
            {
                int first = FirstKey();
                entries.Remove(first);
            }
        }

        int FirstKey()
        {
            foreach (int tick in entries.Keys)
                return tick;
            return 0;
        }
    }
}
