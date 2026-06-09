using System;

namespace ThirdPersonSimulation
{
    public readonly struct SimulationTick : IEquatable<SimulationTick>, IComparable<SimulationTick>
    {
        public SimulationTick(int value)
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(value), value, "Simulation tick cannot be negative.");

            Value = value;
        }

        public int Value { get; }
        public static SimulationTick Zero => new SimulationTick(0);
        public SimulationTick Next => Add(1);

        public SimulationTick Add(int offset)
        {
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset), offset, "Use Subtract for negative offsets.");

            return new SimulationTick(checked(Value + offset));
        }

        public SimulationTick Subtract(int offset)
        {
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset), offset, "Offset cannot be negative.");

            return new SimulationTick(checked(Value - offset));
        }

        public int DifferenceFrom(SimulationTick other)
        {
            return checked(Value - other.Value);
        }

        public int CompareTo(SimulationTick other)
        {
            return Value.CompareTo(other.Value);
        }

        public bool Equals(SimulationTick other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is SimulationTick other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value;
        }

        public override string ToString()
        {
            return Value.ToString();
        }

        public static bool operator ==(SimulationTick left, SimulationTick right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SimulationTick left, SimulationTick right)
        {
            return !left.Equals(right);
        }

        public static bool operator <(SimulationTick left, SimulationTick right)
        {
            return left.Value < right.Value;
        }

        public static bool operator >(SimulationTick left, SimulationTick right)
        {
            return left.Value > right.Value;
        }

        public static bool operator <=(SimulationTick left, SimulationTick right)
        {
            return left.Value <= right.Value;
        }

        public static bool operator >=(SimulationTick left, SimulationTick right)
        {
            return left.Value >= right.Value;
        }
    }
}
