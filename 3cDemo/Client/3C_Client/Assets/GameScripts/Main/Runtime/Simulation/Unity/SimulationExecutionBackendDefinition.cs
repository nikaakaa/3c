using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation
{
    public abstract class SimulationExecutionBackendDefinition : ScriptableObject
    {
        public abstract SimulationExecutionBackendDescriptor BuildPortableDescriptor();
        public abstract SimulationPipelinePassFactoryCatalog BuildPortableFactoryCatalog(
            SimulationPipelineDefinition pipeline);
    }

}
