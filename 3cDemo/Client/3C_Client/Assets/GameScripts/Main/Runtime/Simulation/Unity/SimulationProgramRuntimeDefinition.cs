using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation
{
    public abstract class SimulationProgramRuntimeDefinition : ScriptableObject
    {
        public abstract SimulationProgramRuntimeDescriptor BuildDescriptor();
        public abstract ISimulationSessionComposer CreateComposer();
    }
}
