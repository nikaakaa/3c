using System;
using System.Collections.Generic;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation
{
    [CreateAssetMenu(fileName = "StandardLocalSimulationPipeline", menuName = "3C/Simulation/Standard Local Pipeline Definition")]
    public sealed class StandardLocalSimulationPipelineDefinition :
        SimulationPipelineDefinition,
        IFloat32SimulationPipelineRuntimePackageProvider
    {
        public const string StandardPipelineId = "thirdperson.simulation.pipeline.standard-local";
        public const string StandardRevision = "1";

        public override SimulationPipelineDescriptor BuildPortableDescriptor()
        {
            SimulationPipelineDescriptor descriptor = base.BuildPortableDescriptor();
            if (!string.Equals(descriptor.PipelineId.Value, StandardPipelineId, StringComparison.Ordinal) ||
                !string.Equals(descriptor.Revision.Value, StandardRevision, StringComparison.Ordinal) ||
                descriptor.SchemaVersion.Value != 1)
            {
                throw new InvalidOperationException("Standard Local Pipeline identity must use its canonical id, revision and schema.");
            }
            RequirePhase(descriptor, SimulationPipelinePhase.Ingress, StandardFloat32PipelinePassContracts.LocalInputIngress);
            RequirePhase(descriptor, SimulationPipelinePhase.Schedule, StandardFloat32PipelinePassContracts.LocalSingleStepSchedule);
            RequirePhase(
                descriptor,
                SimulationPipelinePhase.Step,
                StandardFloat32PipelinePassContracts.ProgramEvaluate,
                StandardFloat32PipelinePassContracts.WorldResolveBatch,
                StandardFloat32PipelinePassContracts.ProgramFinalize);
            RequirePhase(descriptor, SimulationPipelinePhase.Egress, StandardFloat32PipelinePassContracts.LocalImmediateOutput);
            return descriptor;
        }

        public Float32SimulationPipelineRuntimePackage BuildRuntimePackage() =>
            Float32SimulationPipelineRuntimePackageBuilder.BuildPassAuthored(this);

        static void RequirePhase(
            SimulationPipelineDescriptor descriptor,
            SimulationPipelinePhase phase,
            params SimulationPipelinePassDescriptor[] expected)
        {
            IReadOnlyList<SimulationPipelinePassDescriptor> actual = descriptor.GetPhase(phase);
            if (actual.Count != expected.Length)
                throw new InvalidOperationException($"Standard Local Pipeline phase '{phase}' has another Pass count.");
            for (int i = 0; i < expected.Length; i++)
            {
                if (!actual[i].DescriptorHash.Equals(expected[i].DescriptorHash))
                    throw new InvalidOperationException($"Standard Local Pipeline phase '{phase}' Pass {i} is not canonical.");
            }
        }
    }
}
