using System;
using System.Collections.Generic;
using ThirdPersonSimulation;
using ThirdPersonSimulation.Fixed;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation.Fixed
{
    [CreateAssetMenu(fileName = "FixedProgramRuntime", menuName = "3C/Simulation/Fixed/Program Runtime")]
    public sealed class FixedProgramRuntimeDefinition : SimulationProgramRuntimeDefinition
    {
        public override SimulationProgramRuntimeDescriptor BuildDescriptor() =>
            FixedProgramRuntime.DescriptorDefinition;

        public override ISimulationSessionComposer CreateComposer() =>
            new UnityFixedSimulationSessionComposer(this);

        internal FixedProgramRuntime CreateRuntime(IReadOnlyList<IFixedSimulationActorRegistration> registrations)
        {
            if (registrations == null || registrations.Count == 0)
                throw new ArgumentException("Fixed Program Runtime requires an Actor roster.", nameof(registrations));
            var bindings = new ThirdPersonSimulation.Fixed.SimulationActorBinding[registrations.Count];
            for (int i = 0; i < registrations.Count; i++)
                bindings[i] = registrations[i].ProgramIdentity;
            return FixedProgramRuntime.Create(bindings);
        }
    }
}
