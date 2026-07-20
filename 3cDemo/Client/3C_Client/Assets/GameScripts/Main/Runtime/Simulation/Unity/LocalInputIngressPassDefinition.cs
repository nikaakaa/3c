using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation
{
    [CreateAssetMenu(fileName = "LocalInputIngressPass", menuName = "3C/Simulation/Passes/Local Input Ingress")]
    public sealed class LocalInputIngressPassDefinition : StandardSimulationPipelinePassDefinition
    {
        public override SimulationPipelinePhase Phase => SimulationPipelinePhase.Ingress;
        protected override SimulationPipelinePassDescriptor StandardDescriptor => StandardFloat32PipelinePassContracts.LocalInputIngress;
        public override IFloat32PipelinePassRuntimeFactory CreateRuntimeFactory() => new LocalInputIngressPassRuntimeFactory();
    }
}
