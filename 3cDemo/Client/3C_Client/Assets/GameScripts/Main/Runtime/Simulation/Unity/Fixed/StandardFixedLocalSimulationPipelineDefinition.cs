using ThirdPersonSimulation;
using ThirdPersonSimulation.Fixed;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation.Fixed
{
    [CreateAssetMenu(fileName = "StandardFixedLocalSimulationPipeline", menuName = "3C/Simulation/Fixed/Standard Local Pipeline")]
    public sealed class StandardFixedLocalSimulationPipelineDefinition :
        SimulationPipelineDefinition,
        IFixedSimulationPipelineDefinition
    {
        public override SimulationPipelineDescriptor BuildPortableDescriptor() =>
            StandardFixedLocalPipeline.CreateDescriptor();

        public SimulationPipelinePassFactoryCatalog BuildFixedPortableFactoryCatalog() =>
            StandardFixedLocalPipeline.CreatePortableFactoryCatalog();

        public FixedSimulationPipelineRuntimePackage BuildRuntimePackage() =>
            StandardFixedLocalPipeline.CreateRuntimePackage();
    }
}
