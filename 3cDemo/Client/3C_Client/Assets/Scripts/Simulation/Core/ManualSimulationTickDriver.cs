using System;

namespace ThirdPersonSimulation
{
    public sealed class ManualSimulationTickDriver
    {
        SimulationTick nextTick;

        public ManualSimulationTickDriver(SimulationTickRate tickRate, SimulationTickRole role)
            : this(tickRate, role, SimulationTick.Zero)
        {
        }

        public ManualSimulationTickDriver(SimulationTickRate tickRate, SimulationTickRole role, SimulationTick startTick)
        {
            TickRate = tickRate;
            Role = role;
            nextTick = startTick;
        }

        public SimulationTickRate TickRate { get; }
        public SimulationTickRole Role { get; }
        public SimulationTick NextTick => nextTick;

        public SimulationTickContext Step()
        {
            SimulationTickContext context = new SimulationTickContext(nextTick, TickRate, Role);
            nextTick = nextTick.Next;
            return context;
        }

        public int Step(SimulationTickRunner runner)
        {
            if (runner == null)
                throw new ArgumentNullException(nameof(runner));

            SimulationTickContext context = Step();
            return runner.Run(in context);
        }

        public void Reset(SimulationTick startTick)
        {
            nextTick = startTick;
        }
    }
}
