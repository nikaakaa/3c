using ThirdPersonSimulation;
using System;

namespace ThirdPersonSimulation.Fixed
{
    public sealed class FixedProgramFinalizePassRuntimeFactory : IFixedPipelinePassRuntimeFactory
    {
        static readonly SimulationPipelinePassFactoryDescriptor s_Descriptor =
            StandardFixedPipelinePassContracts.CreateFactoryDescriptor(
                StandardFixedPipelinePassContracts.ProgramFinalize);

        public SimulationPipelinePassFactoryDescriptor Descriptor => s_Descriptor;

        public IFixedCompiledPipelinePassRuntime Create(FixedPipelinePassRuntimeFactoryContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            var reads = new FixedProgramFinalizeReadPorts(
                context.Products.BindExclusiveReader<FixedPendingEvaluationBatch>(SimulationPipelineProducts.PendingActorEvaluations),
                context.Products.BindExclusiveReader<WorldSolveBatchResult>(SimulationPipelineProducts.WorldSolveBatchResult),
                context.BindTargetPort<IFixedProgramRuntimePort>(FixedPipelineRuntimePortIds.ProgramRuntime),
                context.BindTargetPort<IFixedWorkingStateReadPort>(FixedPipelineRuntimePortIds.WorkingState),
                context.BindDiagnosticsPort<IFixedDiagnosticsRuntimePort>(FixedPipelineRuntimePortIds.Diagnostics));
            var writes = new FixedProgramFinalizeWritePorts(
                context.Products.BindAppendWriter<FixedFinalizedActorResult>(SimulationPipelineProducts.FinalizedStepResult));
            return new FixedStepPassRuntimeAdapter<FixedProgramFinalizeReadPorts, FixedProgramFinalizeWritePorts>(
                new FixedProgramFinalizePassRuntime(context.Pass.Descriptor),
                reads,
                writes);
        }
    }

    public sealed class FixedProgramFinalizePassRuntime :
        FixedPipelinePassRuntimeBase,
        ISimulationStepPassRuntime<FixedProgramFinalizeReadPorts, FixedProgramFinalizeWritePorts>
    {
        public FixedProgramFinalizePassRuntime(SimulationPipelinePassDescriptor descriptor)
            : base(descriptor)
        {
        }

        public void Execute(
            SimulationPipelineStepTransactionContext context,
            FixedProgramFinalizeReadPorts readPorts,
            FixedProgramFinalizeWritePorts writePorts)
        {
            RequireExecution();
            FixedPendingEvaluationBatch pending = readPorts.Pending.Read();
            WorldSolveBatchResult world = readPorts.World.Read();
            FixedSimulationStep step = readPorts.WorkingState.Step ??
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
                FixedPipelineDiagnostics.PublishOperations(
                    readPorts.Diagnostics.Sink,
                    result.TraceRecords,
                    0);
                writePorts.Results.Append(
                    new SimulationPipelineAppendEntryIdentity(
                        actor.ActorId,
                        context.Tick,
                        1,
                        step.Source),
                    new FixedFinalizedActorResult(result));
            }
        }
    }

    public sealed class FixedProgramFinalizeReadPorts : ISimulationPipelineReadPortSet
    {
        public FixedProgramFinalizeReadPorts(
            IReadOnlySimulationPipelineProductPort<FixedPendingEvaluationBatch> pending,
            IReadOnlySimulationPipelineProductPort<WorldSolveBatchResult> world,
            IFixedProgramRuntimePort programRuntime,
            IFixedWorkingStateReadPort workingState,
            IFixedDiagnosticsRuntimePort diagnostics)
        {
            Pending = pending ?? throw new ArgumentNullException(nameof(pending));
            World = world ?? throw new ArgumentNullException(nameof(world));
            ProgramRuntime = programRuntime ?? throw new ArgumentNullException(nameof(programRuntime));
            WorkingState = workingState ?? throw new ArgumentNullException(nameof(workingState));
            Diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
        }

        public IReadOnlySimulationPipelineProductPort<FixedPendingEvaluationBatch> Pending { get; }
        public IReadOnlySimulationPipelineProductPort<WorldSolveBatchResult> World { get; }
        public IFixedProgramRuntimePort ProgramRuntime { get; }
        public IFixedWorkingStateReadPort WorkingState { get; }
        public IFixedDiagnosticsRuntimePort Diagnostics { get; }
    }

    public sealed class FixedProgramFinalizeWritePorts : ISimulationPipelineWritePortSet
    {
        public FixedProgramFinalizeWritePorts(
            IAppendOnlySimulationPipelineProductWriter<FixedFinalizedActorResult> results)
        {
            Results = results ?? throw new ArgumentNullException(nameof(results));
        }

        public IAppendOnlySimulationPipelineProductWriter<FixedFinalizedActorResult> Results { get; }
    }
}

