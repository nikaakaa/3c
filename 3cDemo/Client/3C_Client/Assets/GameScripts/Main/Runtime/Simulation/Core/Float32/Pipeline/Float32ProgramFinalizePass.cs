using System;

namespace ThirdPersonSimulation
{
    public sealed class Float32ProgramFinalizePassRuntimeFactory : IFloat32PipelinePassRuntimeFactory
    {
        static readonly SimulationPipelinePassFactoryDescriptor s_Descriptor =
            StandardFloat32PipelinePassContracts.CreateFactoryDescriptor(
                StandardFloat32PipelinePassContracts.ProgramFinalize);

        public SimulationPipelinePassFactoryDescriptor Descriptor => s_Descriptor;

        public IFloat32CompiledPipelinePassRuntime Create(Float32PipelinePassRuntimeFactoryContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            var reads = new Float32ProgramFinalizeReadPorts(
                context.Products.BindExclusiveReader<Float32PendingEvaluationBatch>(SimulationPipelineProducts.PendingActorEvaluations),
                context.Products.BindExclusiveReader<WorldSolveBatchResult>(SimulationPipelineProducts.WorldSolveBatchResult),
                context.BindTargetPort<IFloat32ProgramRuntimePort>(Float32PipelineRuntimePortIds.ProgramRuntime),
                context.BindTargetPort<IFloat32WorkingStateReadPort>(Float32PipelineRuntimePortIds.WorkingState),
                context.BindDiagnosticsPort<IFloat32DiagnosticsRuntimePort>(Float32PipelineRuntimePortIds.Diagnostics));
            var writes = new Float32ProgramFinalizeWritePorts(
                context.Products.BindAppendWriter<Float32FinalizedActorResult>(SimulationPipelineProducts.FinalizedStepResult));
            return new Float32StepPassRuntimeAdapter<Float32ProgramFinalizeReadPorts, Float32ProgramFinalizeWritePorts>(
                new Float32ProgramFinalizePassRuntime(context.Pass.Descriptor),
                reads,
                writes);
        }
    }

    public sealed class Float32ProgramFinalizePassRuntime :
        Float32PipelinePassRuntimeBase,
        ISimulationStepPassRuntime<Float32ProgramFinalizeReadPorts, Float32ProgramFinalizeWritePorts>
    {
        public Float32ProgramFinalizePassRuntime(SimulationPipelinePassDescriptor descriptor)
            : base(descriptor)
        {
        }

        public void Execute(
            SimulationPipelineStepTransactionContext context,
            Float32ProgramFinalizeReadPorts readPorts,
            Float32ProgramFinalizeWritePorts writePorts)
        {
            RequireExecution();
            Float32PendingEvaluationBatch pending = readPorts.Pending.Read();
            WorldSolveBatchResult world = readPorts.World.Read();
            Float32SimulationStep step = readPorts.WorkingState.Step ??
                throw new InvalidOperationException("Program Finalize Pass has no current Step.");
            if (pending.Tick != context.Tick || world.Tick != context.Tick || step.Tick != context.Tick ||
                pending.Evaluations.Count != world.Results.Count ||
                pending.Evaluations.Count != readPorts.ProgramRuntime.Roster.Count)
            {
                throw new InvalidOperationException("Program Finalize Pass inputs do not match the current Step roster.");
            }
            for (int i = 0; i < pending.Evaluations.Count; i++)
            {
                PendingCharacterEvaluation evaluation = pending.Evaluations[i];
                CharacterWorldSolveResult worldResult = world.Results[i];
                SimulationActorBinding actor = readPorts.ProgramRuntime.Roster[i];
                if (!evaluation.ActorId.Equals(actor.ActorId) || !worldResult.ActorId.Equals(actor.ActorId))
                    throw new InvalidOperationException("Program Finalize Pass Actor order does not match the locked roster.");
                SimulationActorTickResult result = readPorts.ProgramRuntime.Kernel.Finalize(
                    new SimulationFinalizeRequest(evaluation, worldResult, world.SolverId, context.Performance));
                Float32PipelineDiagnostics.PublishOperations(
                    readPorts.Diagnostics.Sink,
                    result.TraceRecords,
                    0);
                writePorts.Results.Append(
                    new SimulationPipelineAppendEntryIdentity(
                        actor.ActorId,
                        context.Tick,
                        1,
                        step.Source),
                    new Float32FinalizedActorResult(result));
            }
        }
    }

    public sealed class Float32ProgramFinalizeReadPorts : ISimulationPipelineReadPortSet
    {
        public Float32ProgramFinalizeReadPorts(
            IReadOnlySimulationPipelineProductPort<Float32PendingEvaluationBatch> pending,
            IReadOnlySimulationPipelineProductPort<WorldSolveBatchResult> world,
            IFloat32ProgramRuntimePort programRuntime,
            IFloat32WorkingStateReadPort workingState,
            IFloat32DiagnosticsRuntimePort diagnostics)
        {
            Pending = pending ?? throw new ArgumentNullException(nameof(pending));
            World = world ?? throw new ArgumentNullException(nameof(world));
            ProgramRuntime = programRuntime ?? throw new ArgumentNullException(nameof(programRuntime));
            WorkingState = workingState ?? throw new ArgumentNullException(nameof(workingState));
            Diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
        }

        public IReadOnlySimulationPipelineProductPort<Float32PendingEvaluationBatch> Pending { get; }
        public IReadOnlySimulationPipelineProductPort<WorldSolveBatchResult> World { get; }
        public IFloat32ProgramRuntimePort ProgramRuntime { get; }
        public IFloat32WorkingStateReadPort WorkingState { get; }
        public IFloat32DiagnosticsRuntimePort Diagnostics { get; }
    }

    public sealed class Float32ProgramFinalizeWritePorts : ISimulationPipelineWritePortSet
    {
        public Float32ProgramFinalizeWritePorts(
            IAppendOnlySimulationPipelineProductWriter<Float32FinalizedActorResult> results)
        {
            Results = results ?? throw new ArgumentNullException(nameof(results));
        }

        public IAppendOnlySimulationPipelineProductWriter<Float32FinalizedActorResult> Results { get; }
    }
}
