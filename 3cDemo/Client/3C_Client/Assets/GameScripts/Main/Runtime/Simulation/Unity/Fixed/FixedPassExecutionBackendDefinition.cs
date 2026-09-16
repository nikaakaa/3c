using System;
using System.Collections.Generic;
using ThirdPersonSimulation;
using ThirdPersonSimulation.Fixed;
using UnityEngine;

using BTSMTL.Timeline.Runtime;

namespace ThirdPersonCharacter.Pipeline.Simulation.Fixed
{
    [CreateAssetMenu(fileName = "FixedPassExecutionBackend", menuName = "3C/Simulation/Fixed/Pass Backend")]
    public sealed class FixedPassExecutionBackendDefinition : SimulationExecutionBackendDefinition
    {
        public override TimelineRuntimeNumericTarget TimelineNumericTarget => TimelineRuntimeNumericTarget.Fixed;

        public override SimulationExecutionTargetManifest BuildExecutionTargetManifest() =>
            FixedSimulationTarget.Manifest.ExecutionTarget;

        public override SimulationExecutionBackendDescriptor BuildPortableDescriptor() =>
            FixedPassExecutionBackend.Descriptor;

        public override SimulationPipelinePassFactoryCatalog BuildPortableFactoryCatalog(
            SimulationPipelineDefinition pipeline)
        {
            if (pipeline is not IFixedSimulationPipelineDefinition provider)
                throw new InvalidOperationException("Fixed Pass Backend requires a Fixed Pipeline Definition.");
            return provider.BuildFixedPortableFactoryCatalog();
        }

        public override ISimulationSessionCompositionPreparation CreateSessionPreparation(
            SimulationSessionCompositionDefinition definition,
            IReadOnlyList<ISimulationActorRegistration> registrations) =>
            new FixedSimulationSessionCompositionPreparation(definition, registrations);
    }
}
