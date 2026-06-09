using System;

namespace ThirdPersonSimulation
{
    public readonly struct SimulationTickRate : IEquatable<SimulationTickRate>
    {
        public const int DefaultTicksPerSecond = 60;

        public SimulationTickRate(int ticksPerSecond)
        {
            if (ticksPerSecond <= 0)
                throw new ArgumentOutOfRangeException(nameof(ticksPerSecond), ticksPerSecond, "Tick rate must be greater than zero.");

            TicksPerSecond = ticksPerSecond;
        }

        public int TicksPerSecond { get; }
        public double FixedDeltaSeconds => 1d / TicksPerSecond;
        public float FixedDeltaSecondsFloat => 1f / TicksPerSecond;
        public static SimulationTickRate Default => new SimulationTickRate(DefaultTicksPerSecond);

        public bool Equals(SimulationTickRate other)
        {
            return TicksPerSecond == other.TicksPerSecond;
        }

        public override bool Equals(object obj)
        {
            return obj is SimulationTickRate other && Equals(other);
        }

        public override int GetHashCode()
        {
            return TicksPerSecond;
        }

        public override string ToString()
        {
            return $"{TicksPerSecond}Hz";
        }
    }
}
