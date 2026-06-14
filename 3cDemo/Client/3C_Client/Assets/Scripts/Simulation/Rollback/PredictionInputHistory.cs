using System;
using System.Collections.Generic;

namespace ThirdPersonSimulation
{
    public sealed class PredictionInputHistory
    {
        readonly SortedDictionary<int, PredictionInputFrame> frames = new SortedDictionary<int, PredictionInputFrame>();

        public PredictionInputHistory(int capacity)
        {
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "Capacity must be positive.");

            Capacity = capacity;
        }

        public int Capacity { get; }
        public int Count => frames.Count;

        public void Write(in PredictionInputFrame frame)
        {
            frames[frame.Tick.Value] = frame;
            TrimToCapacity();
        }

        public bool TryGet(SimulationTick tick, out PredictionInputFrame frame)
        {
            return frames.TryGetValue(tick.Value, out frame);
        }

        public PredictionHistoryQueryResult TryReadRange(SimulationTick fromInclusive, SimulationTick toInclusive, List<PredictionInputFrame> output)
        {
            if (output == null)
                throw new ArgumentNullException(nameof(output));

            output.Clear();

            if (toInclusive < fromInclusive)
                return PredictionHistoryQueryResult.Ok(fromInclusive);

            for (int tick = fromInclusive.Value; tick <= toInclusive.Value; tick++)
            {
                if (!frames.TryGetValue(tick, out PredictionInputFrame frame))
                    return PredictionHistoryQueryResult.Missing(new SimulationTick(tick));

                output.Add(frame);
            }

            return PredictionHistoryQueryResult.Ok(toInclusive);
        }

        public void TrimConfirmedBefore(SimulationTick confirmedTick)
        {
            List<int> keys = new List<int>();
            foreach (int tick in frames.Keys)
            {
                if (tick < confirmedTick.Value)
                    keys.Add(tick);
            }

            for (int i = 0; i < keys.Count; i++)
                frames.Remove(keys[i]);
        }

        void TrimToCapacity()
        {
            while (frames.Count > Capacity)
            {
                int first = FirstKey();
                frames.Remove(first);
            }
        }

        int FirstKey()
        {
            foreach (int tick in frames.Keys)
                return tick;

            return 0;
        }
    }
}
