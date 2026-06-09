namespace ThirdPersonSimulation
{
    public readonly struct SimulationTickContext
    {
        public SimulationTickContext(SimulationTick tick, SimulationTickRate tickRate, SimulationTickRole role)
        {
            Tick = tick;
            TickRate = tickRate;
            Role = role;
        }

        public SimulationTick Tick { get; }
        public SimulationTickRate TickRate { get; }
        public SimulationTickRole Role { get; }
        public int TickValue => Tick.Value;
        public double FixedDeltaSeconds => TickRate.FixedDeltaSeconds;
        public float FixedDeltaSecondsFloat => TickRate.FixedDeltaSecondsFloat;
    }
}
