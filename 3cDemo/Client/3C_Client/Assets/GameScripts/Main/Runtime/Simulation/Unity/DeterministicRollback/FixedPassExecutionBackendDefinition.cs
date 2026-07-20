using System;
using ThirdPersonSimulation;
using ThirdPersonSimulation.DeterministicRollback;
using ThirdPersonSimulation.Fixed;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation.DeterministicRollback
{
    [CreateAssetMenu(fileName = "FixedPassExecutionBackend", menuName = "3C/Simulation/Deterministic Rollback/Fixed Pass Backend")]
    public sealed class FixedPassExecutionBackendDefinition : SimulationExecutionBackendDefinition
    {
        public override SimulationExecutionBackendDescriptor BuildPortableDescriptor() =>
            FixedPassExecutionBackend.Descriptor;

        public override SimulationPipelinePassFactoryCatalog BuildPortableFactoryCatalog(
            SimulationPipelineDefinition pipeline)
        {
            if (pipeline is not IDeterministicRollbackPipelineRuntimePackageProvider provider)
                throw new InvalidOperationException("Fixed Pass Backend requires a Deterministic Rollback Pipeline Definition.");
            return RollbackPipelineRuntimePackageBuilder.CreatePortableFactoryCatalog(provider.BuildPolicy());
        }
    }
}
