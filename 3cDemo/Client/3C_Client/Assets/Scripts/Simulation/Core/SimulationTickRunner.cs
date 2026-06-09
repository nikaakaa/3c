using System;
using System.Collections.Generic;

namespace ThirdPersonSimulation
{
    public sealed class SimulationTickRunner
    {
        readonly Dictionary<SimulationTickPhase, List<ISimulationTickPhaseHandler>> handlers =
            new Dictionary<SimulationTickPhase, List<ISimulationTickPhaseHandler>>();

        public void Register(SimulationTickPhase phase, ISimulationTickPhaseHandler handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            if (!handlers.TryGetValue(phase, out List<ISimulationTickPhaseHandler> phaseHandlers))
            {
                phaseHandlers = new List<ISimulationTickPhaseHandler>();
                handlers.Add(phase, phaseHandlers);
            }

            phaseHandlers.Add(handler);
        }

        public bool Unregister(SimulationTickPhase phase, ISimulationTickPhaseHandler handler)
        {
            if (handler == null)
                return false;

            return handlers.TryGetValue(phase, out List<ISimulationTickPhaseHandler> phaseHandlers) &&
                   phaseHandlers.Remove(handler);
        }

        public int Run(in SimulationTickContext context)
        {
            int calls = 0;
            IReadOnlyList<SimulationTickPhase> phases = SimulationTickPhaseOrder.Phases;

            for (int phaseIndex = 0; phaseIndex < phases.Count; phaseIndex++)
            {
                SimulationTickPhase phase = phases[phaseIndex];
                if (!handlers.TryGetValue(phase, out List<ISimulationTickPhaseHandler> phaseHandlers))
                    continue;

                for (int handlerIndex = 0; handlerIndex < phaseHandlers.Count; handlerIndex++)
                {
                    phaseHandlers[handlerIndex].Tick(phase, in context);
                    calls++;
                }
            }

            return calls;
        }
    }
}
