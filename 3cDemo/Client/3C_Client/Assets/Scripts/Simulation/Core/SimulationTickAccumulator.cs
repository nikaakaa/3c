using System;
using System.Collections.Generic;

namespace ThirdPersonSimulation
{
    public sealed class SimulationTickAccumulator
    {
        double accumulatedSeconds;
        SimulationTick nextTick;

        public SimulationTickAccumulator(SimulationTickRate tickRate, int maxTicksPerStep)
            : this(tickRate, maxTicksPerStep, SimulationTick.Zero)
        {
        }

        public SimulationTickAccumulator(SimulationTickRate tickRate, int maxTicksPerStep, SimulationTick startTick)
        {
            if (maxTicksPerStep <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxTicksPerStep), maxTicksPerStep, "Max ticks per step must be greater than zero.");

            TickRate = tickRate;
            MaxTicksPerStep = maxTicksPerStep;
            nextTick = startTick;
        }

        public SimulationTickRate TickRate { get; }
        public int MaxTicksPerStep { get; }
        public SimulationTick NextTick => nextTick;
        public double AccumulatedSeconds => accumulatedSeconds;

        public int Accumulate(double deltaSeconds, IList<SimulationTickContext> output, SimulationTickRole role)
        {
            if (deltaSeconds < 0d)
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds), deltaSeconds, "Delta seconds cannot be negative.");

            if (output == null)
                throw new ArgumentNullException(nameof(output));

            accumulatedSeconds += deltaSeconds;
            double fixedDelta = TickRate.FixedDeltaSeconds;
            int availableTicks = (int)Math.Floor(accumulatedSeconds / fixedDelta);
            int ticksToEmit = availableTicks > MaxTicksPerStep ? MaxTicksPerStep : availableTicks;

            for (int i = 0; i < ticksToEmit; i++)
            {
                output.Add(new SimulationTickContext(nextTick, TickRate, role));
                nextTick = nextTick.Next;
            }

            accumulatedSeconds -= ticksToEmit * fixedDelta;
            if (accumulatedSeconds < 0d && accumulatedSeconds > -0.000000001d)
                accumulatedSeconds = 0d;

            return ticksToEmit;
        }

        public void Reset(SimulationTick startTick)
        {
            nextTick = startTick;
            accumulatedSeconds = 0d;
        }
    }
}
