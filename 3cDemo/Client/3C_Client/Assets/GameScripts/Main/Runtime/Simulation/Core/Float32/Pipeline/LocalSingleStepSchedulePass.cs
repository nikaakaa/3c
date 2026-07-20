using System;
using System.Collections.Generic;

namespace ThirdPersonSimulation
{
    public sealed class LocalSingleStepSchedulePassRuntimeFactory : IFloat32PipelinePassRuntimeFactory
    {
        static readonly SimulationPipelinePassFactoryDescriptor s_Descriptor =
            StandardFloat32PipelinePassContracts.CreateFactoryDescriptor(
                StandardFloat32PipelinePassContracts.LocalSingleStepSchedule);

        public SimulationPipelinePassFactoryDescriptor Descriptor => s_Descriptor;

        public IFloat32CompiledPipelinePassRuntime Create(Float32PipelinePassRuntimeFactoryContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            var reads = new LocalSingleStepScheduleReadPorts(
                context.Products.BindExclusiveReader<Float32CanonicalInputBatch>(SimulationPipelineProducts.CanonicalInputs),
                context.Products.BindExclusiveReader<Float32TypedIngressBatch>(SimulationPipelineProducts.TypedIngress),
                context.BindTargetPort<IFloat32ProgramRuntimePort>(Float32PipelineRuntimePortIds.ProgramRuntime));
            var writes = new LocalSingleStepScheduleWritePorts(
                context.Products.BindExclusiveWriter<SimulationSessionExecutionPlan<Float32SimulationStep>>(
                    SimulationPipelineProducts.ExecutionPlan));
            return new Float32SchedulePassRuntimeAdapter<LocalSingleStepScheduleReadPorts, LocalSingleStepScheduleWritePorts>(
                new LocalSingleStepSchedulePassRuntime(context.Pass.Descriptor),
                reads,
                writes);
        }
    }

    public sealed class LocalSingleStepSchedulePassRuntime :
        Float32PipelinePassRuntimeBase,
        ISimulationExecutionPlanSchedulePassRuntime<LocalSingleStepScheduleReadPorts, LocalSingleStepScheduleWritePorts>
    {
        public LocalSingleStepSchedulePassRuntime(SimulationPipelinePassDescriptor descriptor)
            : base(descriptor)
        {
        }

        public void Execute(
            SimulationPipelineScheduleContext context,
            LocalSingleStepScheduleReadPorts readPorts,
            LocalSingleStepScheduleWritePorts writePorts)
        {
            RequireExecution();
            writePorts.ExecutionPlan.Write(Float32SingleStepScheduleBuilder.Build(
                context,
                readPorts.CanonicalInputs.Read(),
                readPorts.TypedIngress.Read(),
                readPorts.ProgramRuntime,
                SimulationTickSourceKind.LocalLogic));
        }
    }

    public static class Float32SingleStepScheduleBuilder
    {
        public static SimulationSessionExecutionPlan<Float32SimulationStep> Build(
            SimulationPipelineScheduleContext context,
            Float32CanonicalInputBatch canonical,
            Float32TypedIngressBatch typed,
            IFloat32ProgramRuntimePort programRuntime,
            SimulationTickSourceKind expectedSourceKind)
        {
            if (canonical == null || typed == null || programRuntime == null)
                throw new ArgumentNullException("Single-step Schedule input is missing.");
            if (context.Source.Kind != expectedSourceKind || !canonical.Source.Equals(context.Source) ||
                canonical.Inputs.Count != programRuntime.Roster.Count)
            {
                throw new InvalidOperationException("Single-step Schedule input batch does not match the outer Tick or locked roster.");
            }
            IReadOnlyList<ActorId> actorIds = programRuntime.RosterDescriptor.Actors;
            for (int i = 0; i < actorIds.Count; i++)
            {
                if (!canonical.Inputs[i].ActorId.Equals(actorIds[i]))
                    throw new InvalidOperationException("Single-step Schedule input Actor order does not match the locked roster.");
            }
            var tick = new SimulationTick(checked(context.CurrentCompletedTick + 1));
            var step = new Float32SimulationStep(
                tick,
                new SimulationPipelineStepProvenance(
                    SimulationPipelineStepExecutionKind.Forward,
                    context.Source,
                    context.Source.SourceTick),
                canonical.Inputs,
                typed.Ingress,
                ObservedWorldConstraintFrame.Empty(tick));
            return new SimulationSessionExecutionPlan<Float32SimulationStep>(
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

    public sealed class LocalSingleStepScheduleReadPorts : ISimulationPipelineReadPortSet
    {
        public LocalSingleStepScheduleReadPorts(
            IReadOnlySimulationPipelineProductPort<Float32CanonicalInputBatch> canonicalInputs,
            IReadOnlySimulationPipelineProductPort<Float32TypedIngressBatch> typedIngress,
            IFloat32ProgramRuntimePort programRuntime)
        {
            CanonicalInputs = canonicalInputs ?? throw new ArgumentNullException(nameof(canonicalInputs));
            TypedIngress = typedIngress ?? throw new ArgumentNullException(nameof(typedIngress));
            ProgramRuntime = programRuntime ?? throw new ArgumentNullException(nameof(programRuntime));
        }

        public IReadOnlySimulationPipelineProductPort<Float32CanonicalInputBatch> CanonicalInputs { get; }
        public IReadOnlySimulationPipelineProductPort<Float32TypedIngressBatch> TypedIngress { get; }
        public IFloat32ProgramRuntimePort ProgramRuntime { get; }
    }

    public sealed class LocalSingleStepScheduleWritePorts : ISimulationPipelineWritePortSet
    {
        public LocalSingleStepScheduleWritePorts(
            IExclusiveSimulationPipelineProductWriter<SimulationSessionExecutionPlan<Float32SimulationStep>> executionPlan)
        {
            ExecutionPlan = executionPlan ?? throw new ArgumentNullException(nameof(executionPlan));
        }

        public IExclusiveSimulationPipelineProductWriter<SimulationSessionExecutionPlan<Float32SimulationStep>> ExecutionPlan { get; }
    }
}
