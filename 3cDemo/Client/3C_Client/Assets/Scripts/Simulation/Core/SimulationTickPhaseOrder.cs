using System;
using System.Collections.Generic;

namespace ThirdPersonSimulation
{
    public static class SimulationTickPhaseOrder
    {
        static readonly SimulationTickPhase[] OrderedPhases =
        {
            SimulationTickPhase.ReadInput,
            SimulationTickPhase.UpdateInputBuffer,
            SimulationTickPhase.GameplayDecision,
            SimulationTickPhase.BuildMotion,
            SimulationTickPhase.ExecuteMotion,
            SimulationTickPhase.WriteSnapshotAndEvents,
            SimulationTickPhase.PresentationBridge
        };

        public static IReadOnlyList<SimulationTickPhase> Phases { get; } = Array.AsReadOnly(OrderedPhases);
    }
}
