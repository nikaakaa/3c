using ThirdPersonSimulation;
using System;
using System.Collections.Generic;

namespace ThirdPersonSimulation.Fixed
{
    public static class StandardFixedPipelinePassContracts
    {
        public const string ImplementationVersion = "1";
        public const string ProgramEvaluatePassId = "thirdperson.simulation.fixed-program-evaluate";
        public const string WorldResolveBatchPassId = "thirdperson.simulation.fixed-world-resolve-batch";
        public const string ProgramFinalizePassId = "thirdperson.simulation.fixed-program-finalize";

        static readonly SimulationPipelineExecutionSupport s_AllExecution =
            SimulationPipelineExecutionSupport.Forward |
            SimulationPipelineExecutionSupport.Replay |
            SimulationPipelineExecutionSupport.Restore |
            SimulationPipelineExecutionSupport.Authoritative;

        static readonly SimulationPipelinePassDescriptor s_ProgramEvaluate = Create(
            ProgramEvaluatePassId,
            new[]
            {
                Produce(SimulationPipelineProducts.PendingActorEvaluations),
                Produce(SimulationPipelineProducts.WorldSolveBatchRequest)
            },
            new[]
            {
                Target(FixedPipelineRuntimePortIds.ProgramRuntime, FixedPipelineRuntimePortIds.ProgramRuntimeSchema),
                Target(FixedPipelineRuntimePortIds.WorkingState, FixedPipelineRuntimePortIds.WorkingStateSchema),
                Diagnostics()
            });

        static readonly SimulationPipelinePassDescriptor s_WorldResolveBatch = Create(
            WorldResolveBatchPassId,
            new[]
            {
                Consume(SimulationPipelineProducts.WorldSolveBatchRequest),
                Produce(SimulationPipelineProducts.WorldSolveBatchResult)
            },
            new[]
            {
                Solver(),
                Diagnostics()
            });

        static readonly SimulationPipelinePassDescriptor s_ProgramFinalize = Create(
            ProgramFinalizePassId,
            new[]
            {
                Consume(SimulationPipelineProducts.PendingActorEvaluations),
                Consume(SimulationPipelineProducts.WorldSolveBatchResult),
                Append(SimulationPipelineProducts.FinalizedStepResult)
            },
            new[]
            {
                Target(FixedPipelineRuntimePortIds.ProgramRuntime, FixedPipelineRuntimePortIds.ProgramRuntimeSchema),
                Target(FixedPipelineRuntimePortIds.WorkingState, FixedPipelineRuntimePortIds.WorkingStateSchema),
                Diagnostics()
            });

        public static SimulationPipelinePassDescriptor ProgramEvaluate => s_ProgramEvaluate;
        public static SimulationPipelinePassDescriptor WorldResolveBatch => s_WorldResolveBatch;
        public static SimulationPipelinePassDescriptor ProgramFinalize => s_ProgramFinalize;

        public static SimulationPipelinePassFactoryDescriptor CreateFactoryDescriptor(
            SimulationPipelinePassDescriptor descriptor)
        {
            if (descriptor == null)
                throw new ArgumentNullException(nameof(descriptor));
            return new SimulationPipelinePassFactoryDescriptor(
                new SimulationPipelinePassFactoryIdentity(
                    descriptor.PassId,
                    descriptor.ImplementationVersion,
                    "1",
                    StableHash.Compute("fixed-standard-pass-binding/1", descriptor.DescriptorHash.ToString())),
                descriptor.Phase,
                FixedPassExecutionBackend.BackendId,
                FixedPassExecutionBackend.SemanticVersion,
                descriptor.ConfigurationHash,
                descriptor.ExecutionSupport,
                true,
                false,
                false,
                false);
        }

        static SimulationPipelinePassDescriptor Create(
            string passId,
            IEnumerable<SimulationPipelineProductAccess> products,
            IEnumerable<SimulationPipelinePortRequirement> ports)
        {
            return new SimulationPipelinePassDescriptor(
                new SimulationPipelinePassId(passId),
                new SimulationPipelinePassImplementationVersion(ImplementationVersion),
                SimulationPipelinePhase.Step,
                StableHash.Compute("standard-fixed-pass-config/1", passId),
                FixedSimulationNumericProfile.Value.Id,
                FixedSimulationNumericProfile.Value.AbiVersion,
                FixedPassExecutionBackend.BackendId,
                FixedPassExecutionBackend.SemanticVersion,
                WorldCapability.None,
                s_AllExecution,
                SimulationPipelinePassStateClass.Stateless,
                string.Empty,
                products,
                ports);
        }

        static SimulationPipelineProductAccess Produce(SimulationPipelineProductContract product) =>
            new SimulationPipelineProductAccess(product, SimulationPipelineProductAccessKind.ExclusiveProducer);

        static SimulationPipelineProductAccess Append(SimulationPipelineProductContract product) =>
            new SimulationPipelineProductAccess(product, SimulationPipelineProductAccessKind.AppendOnlyProducer);

        static SimulationPipelineProductAccess Consume(SimulationPipelineProductContract product) =>
            new SimulationPipelineProductAccess(product, SimulationPipelineProductAccessKind.ReadOnlyConsumer);

        static SimulationPipelinePortRequirement Target(string portId, string schemaId) =>
            new SimulationPipelinePortRequirement(
                SimulationPipelineBindingPortRole.Target,
                portId,
                schemaId,
                1,
                SimulationPortDirection.Input);

        static SimulationPipelinePortRequirement Solver() =>
            new SimulationPipelinePortRequirement(
                SimulationPipelineBindingPortRole.Solver,
                FixedPipelineRuntimePortIds.WorldSolver,
                FixedPipelineRuntimePortIds.WorldSolverSchema,
                1,
                SimulationPortDirection.Input);

        static SimulationPipelinePortRequirement Diagnostics() =>
            new SimulationPipelinePortRequirement(
                SimulationPipelineBindingPortRole.Diagnostics,
                FixedPipelineRuntimePortIds.Diagnostics,
                FixedPipelineRuntimePortIds.DiagnosticsSchema,
                1,
                SimulationPortDirection.Input);
    }
}
