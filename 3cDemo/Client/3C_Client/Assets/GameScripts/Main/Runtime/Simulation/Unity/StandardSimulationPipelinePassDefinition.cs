using System;
using System.Collections.Generic;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Simulation
{
    public abstract class StandardSimulationPipelinePassDefinition :
        SimulationPipelinePassDefinition,
        IFloat32SimulationPipelinePassRuntimeProvider
    {
        protected sealed override SimulationPipelinePassDescriptor BuildPortableDescriptor(
            SimulationPipelinePassId passId,
            SimulationPipelinePassImplementationVersion implementationVersion)
        {
            SimulationPipelinePassDescriptor descriptor = StandardDescriptor;
            if (!descriptor.PassId.Equals(passId) || !descriptor.ImplementationVersion.Equals(implementationVersion))
                throw new System.InvalidOperationException($"Standard Pass Definition '{name}' must use its canonical identity.");
            return descriptor;
        }

        protected abstract SimulationPipelinePassDescriptor StandardDescriptor { get; }
        public abstract IFloat32PipelinePassRuntimeFactory CreateRuntimeFactory();
        public virtual IReadOnlyList<IFloat32PipelineProductSlotFactory> CreateAdditionalProductSlotFactories() =>
            Array.Empty<IFloat32PipelineProductSlotFactory>();
    }
}
