using System.Collections.Generic;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation
{
    [CreateAssetMenu(fileName = "Float32ProgramRuntime", menuName = "3C/Simulation/Float32 Program Runtime")]
    public sealed class Float32ProgramRuntimeDefinition : SimulationProgramRuntimeDefinition
    {
        public override SimulationProgramRuntimeDescriptor BuildDescriptor()
        {
            return Float32ProgramRuntime.DescriptorDefinition;
        }

        public override ISimulationSessionComposer CreateComposer()
        {
            return new UnityFloat32SimulationSessionComposer(this);
        }

        internal Float32ProgramRuntime CreateRuntime(IReadOnlyList<IFloat32SimulationActorRegistration> registrations)
        {
            var bindings = new List<SimulationActorBinding>(registrations?.Count ?? 0);
            for (int i = 0; i < registrations.Count; i++)
                bindings.Add(registrations[i].ProgramIdentity);
            return Float32ProgramRuntime.Create(bindings);
        }
    }
}
