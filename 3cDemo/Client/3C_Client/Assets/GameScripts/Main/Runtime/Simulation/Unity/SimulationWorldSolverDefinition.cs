using System;
using System.Collections.Generic;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation
{
    public abstract class SimulationWorldSolverDefinition : ScriptableObject
    {
        public abstract SimulationWorldSolverDefinitionDescriptor BuildDescriptor(int tickRate);
        public abstract SimulationWorldIdentityDescriptor BuildWorldIdentity(
            int tickRate,
            SimulationWorldId worldId,
            string mapId,
            WorldRevision worldRevision);
    }

    public abstract class Float32WorldSolverDefinition : SimulationWorldSolverDefinition
    {
        internal ICharacterWorldSolver CreateSolver(
            int tickRate,
            IReadOnlyList<IFloat32SimulationActorRegistration> registrations) =>
            CreateSolverCore(tickRate, registrations) ?? throw new InvalidOperationException(
                $"World Solver Definition '{name}' returned no solver.");

        protected abstract ICharacterWorldSolver CreateSolverCore(
            int tickRate,
            IReadOnlyList<IFloat32SimulationActorRegistration> registrations);
    }
}
