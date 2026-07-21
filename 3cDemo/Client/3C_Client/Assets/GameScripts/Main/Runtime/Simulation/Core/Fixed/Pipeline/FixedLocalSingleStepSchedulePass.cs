using System;
using System.Collections.Generic;
using ThirdPersonSimulation;

namespace ThirdPersonSimulation.Fixed
{
    public sealed class FixedLocalSingleStepSchedulePassRuntimeFactory : IFixedPipelinePassRuntimeFactory
    {
        static readonly SimulationPipelinePassFactoryDescriptor s_Descriptor =
            StandardFixedLocalPipelinePassContracts.CreateFactoryDescriptor(
                StandardFixedLocalPipelinePassContracts.LocalSingleStepSchedule);

        public SimulationPipelinePassFactoryDescriptor Descriptor => s_Descriptor;

        public IFixedCompiledPipelinePassRuntime Create(FixedPipelinePassRuntimeFactoryContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            var reads = new FixedLocalSingleStepScheduleReadPorts(
                context.Products.BindExclusiveReader<FixedCanonicalInputBatch>(SimulationPipelineProducts.CanonicalInputs),
                context.Products.BindExclusiveReader<FixedTypedIngressBatch>(SimulationPipelineProducts.TypedIngress),
                context.BindTargetPort<IFixedProgramRuntimePort>(FixedPipelineRuntimePortIds.ProgramRuntime));
            var writes = new FixedLocalSingleStepScheduleWritePorts(
                context.Products.BindExclusiveWriter<SimulationSessionExecutionPlan<FixedSimulationStep>>(
                    SimulationPipelineProducts.ExecutionPlan));
            return new FixedSchedulePassRuntimeAdapter<FixedLocalSingleStepScheduleReadPorts, FixedLocalSingleStepScheduleWritePorts>(
                new FixedLocalSingleStepSchedulePassRuntime(context.Pass.Descriptor),
                reads,
                writes);
        }
    }

    public sealed class FixedLocalSingleStepSchedulePassRuntime :
        FixedPipelinePassRuntimeBase,
        ISimulationExecutionPlanSchedulePassRuntime<FixedLocalSingleStepScheduleReadPorts, FixedLocalSingleStepScheduleWritePorts>
    {
        public FixedLocalSingleStepSchedulePassRuntime(SimulationPipelinePassDescriptor descriptor)
            : base(descriptor)
        {
        }

        public void Execute(
            SimulationPipelineScheduleContext context,
            FixedLocalSingleStepScheduleReadPorts readPorts,
            FixedLocalSingleStepScheduleWritePorts writePorts)
        {
            RequireExecution();
            writePorts.ExecutionPlan.Write(Build(
                context,
                readPorts.CanonicalInputs.Read(),
                readPorts.TypedIngress.Read(),
                readPorts.ProgramRuntime));
        }

        static SimulationSessionExecutionPlan<FixedSimulationStep> Build(
            SimulationPipelineScheduleContext context,
            FixedCanonicalInputBatch canonical,
            FixedTypedIngressBatch typed,
            IFixedProgramRuntimePort programRuntime)
        {
            if (canonical == null || typed == null || programRuntime == null)
                throw new ArgumentNullException("Fixed Local single-step Schedule input is missing.");
            if (context.Source.Kind != SimulationTickSourceKind.LocalLogic || !canonical.Source.Equals(context.Source) ||
                canonical.Inputs.Count != programRuntime.Roster.Count)
            {
                throw new InvalidOperationException("Fixed Local single-step input batch does not match the outer Tick or locked roster.");
            }
            IReadOnlyList<ActorId> actorIds = programRuntime.RosterDescriptor.Actors;
            for (int i = 0; i < actorIds.Count; i++)
            {
                if (!canonical.Inputs[i].ActorId.Equals(actorIds[i]))
                    throw new InvalidOperationException("Fixed Local input Actor order does not match the locked roster.");
            }
            var tick = new SimulationTick(checked(context.CurrentCompletedTick + 1));
            var step = new FixedSimulationStep(
                tick,
                new SimulationPipelineStepProvenance(
                    SimulationPipelineStepExecutionKind.Forward,
                    context.Source,
                    context.Source.SourceTick),
                canonical.Inputs,
                typed.Ingress);
            return new SimulationSessionExecutionPlan<FixedSimulationStep>(
                SimulationSessionExecutionPlanStatus.Executable,
                context.Source,
                programRuntime.Catalog.CatalogHash,
                context.Pipeline.Hash,
                programRuntime.RosterDescriptor,
                new[]
                {
                    new SimulationPipelineStepSourceMapping(
                        context.Source.ClockId,
                        context.Source.ClockId,
                        context.Source.Kind)
                },
                null,
                new[] { step },
                SimulationSessionPlanRequirement.WorkingState |
                SimulationSessionPlanRequirement.OutputDisposition);
        }
    }

    public sealed class FixedLocalSingleStepScheduleReadPorts : ISimulationPipelineReadPortSet
    {
        public FixedLocalSingleStepScheduleReadPorts(
            IReadOnlySimulationPipelineProductPort<FixedCanonicalInputBatch> canonicalInputs,
            IReadOnlySimulationPipelineProductPort<FixedTypedIngressBatch> typedIngress,
            IFixedProgramRuntimePort programRuntime)
        {
            CanonicalInputs = canonicalInputs ?? throw new ArgumentNullException(nameof(canonicalInputs));
            TypedIngress = typedIngress ?? throw new ArgumentNullException(nameof(typedIngress));
            ProgramRuntime = programRuntime ?? throw new ArgumentNullException(nameof(programRuntime));
        }

        public IReadOnlySimulationPipelineProductPort<FixedCanonicalInputBatch> CanonicalInputs { get; }
        public IReadOnlySimulationPipelineProductPort<FixedTypedIngressBatch> TypedIngress { get; }
        public IFixedProgramRuntimePort ProgramRuntime { get; }
    }

    public sealed class FixedLocalSingleStepScheduleWritePorts : ISimulationPipelineWritePortSet
    {
        public FixedLocalSingleStepScheduleWritePorts(
            IExclusiveSimulationPipelineProductWriter<SimulationSessionExecutionPlan<FixedSimulationStep>> executionPlan)
        {
            ExecutionPlan = executionPlan ?? throw new ArgumentNullException(nameof(executionPlan));
        }

        public IExclusiveSimulationPipelineProductWriter<SimulationSessionExecutionPlan<FixedSimulationStep>> ExecutionPlan { get; }
    }
}
