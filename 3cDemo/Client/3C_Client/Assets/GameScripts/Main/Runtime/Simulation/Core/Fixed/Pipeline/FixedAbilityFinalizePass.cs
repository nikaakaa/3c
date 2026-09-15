using ThirdPersonSimulation;
using System;
using System.Collections.Generic;

namespace ThirdPersonSimulation.Fixed
{
    public sealed class FixedAbilityFinalizePassRuntimeFactory : IFixedPipelinePassRuntimeFactory
    {
        static readonly SimulationPipelinePassFactoryDescriptor s_Descriptor =
            StandardFixedPipelinePassContracts.CreateFactoryDescriptor(
                StandardFixedPipelinePassContracts.AbilityFinalize);

        public SimulationPipelinePassFactoryDescriptor Descriptor => s_Descriptor;

        public IFixedCompiledPipelinePassRuntime Create(FixedPipelinePassRuntimeFactoryContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            var reads = new FixedAbilityFinalizeReadPorts(
                context.Products.BindExclusiveReader<FixedCharacterEvaluationResultBatch>(SimulationPipelineProducts.CharacterEvaluationResults),
                context.Products.BindExclusiveReader<WorldSolveBatchResult>(SimulationPipelineProducts.WorldSolveBatchResult),
                context.BindTargetPort<IFixedCharacterRuntimePort>(FixedPipelineRuntimePortIds.CharacterRuntime),
                context.BindTargetPort<IFixedWorkingStateReadPort>(FixedPipelineRuntimePortIds.WorkingState),
                context.BindDiagnosticsPort<IFixedDiagnosticsRuntimePort>(FixedPipelineRuntimePortIds.Diagnostics));
            var writes = new FixedAbilityFinalizeWritePorts(
                context.Products.BindAppendWriter<FixedFinalizedActorResult>(SimulationPipelineProducts.FinalizedStepResult));
            return new FixedStepPassRuntimeAdapter<FixedAbilityFinalizeReadPorts, FixedAbilityFinalizeWritePorts>(
                new FixedAbilityFinalizePassRuntime(context.Pass.Descriptor),
                reads,
                writes);
        }
    }

    public sealed class FixedAbilityFinalizePassRuntime :
        FixedPipelinePassRuntimeBase,
        ISimulationStepPassRuntime<FixedAbilityFinalizeReadPorts, FixedAbilityFinalizeWritePorts>
    {
        public FixedAbilityFinalizePassRuntime(SimulationPipelinePassDescriptor descriptor)
            : base(descriptor)
        {
        }

        public void Execute(
            SimulationPipelineStepTransactionContext context,
            FixedAbilityFinalizeReadPorts readPorts,
            FixedAbilityFinalizeWritePorts writePorts)
        {
            RequireExecution();
            FixedCharacterEvaluationResultBatch evaluations = readPorts.CharacterEvaluationResults.Read();
            WorldSolveBatchResult world = readPorts.World.Read();
            FixedSimulationStep step = readPorts.WorkingState.Step ??
                throw new InvalidOperationException("Ability Finalize Pass has no current Step.");
            if (evaluations.Tick != context.Tick || world.Tick != context.Tick || step.Tick != context.Tick ||
                evaluations.Evaluations.Count != world.Results.Count ||
                evaluations.Evaluations.Count != readPorts.CharacterRuntime.Roster.Count)
                throw new InvalidOperationException("Ability Finalize Pass inputs do not match the current Step roster.");

            for (int i = 0; i < evaluations.Evaluations.Count; i++)
            {
                FixedCharacterEvaluationResult evaluation = evaluations.Evaluations[i];
                CharacterWorldSolveResult worldResult = world.Results[i];
                SimulationActorBinding actor = readPorts.CharacterRuntime.Roster[i];
                if (!evaluation.ActorId.Equals(actor.ActorId) || !worldResult.ActorId.Equals(actor.ActorId))
                    throw new InvalidOperationException("Ability Finalize Pass Actor order does not match the locked roster.");
                CharacterWorldSolveRequest expected = evaluation.WorldRequest;
                if (worldResult.NumericProfile != readPorts.CharacterRuntime.Runtime.NumericProfile ||
                    !worldResult.RequestId.Equals(expected.RequestId) ||
                    worldResult.Tick != expected.Tick ||
                    !worldResult.SolverId.Equals(world.SolverId) ||
                    worldResult.FinalBody.ActorId != evaluation.ActorId)
                {
                    throw new InvalidOperationException(
                        $"World result '{worldResult.RequestId}' does not match character evaluation request '{expected.RequestId}'.");
                }
                var bodySample = new CharacterBodySample(
                    evaluation.ActorId,
                    evaluation.Tick,
                    expected.BeforeBody,
                    worldResult.FinalBody,
                    worldResult.AppliedDisplacement,
                    worldResult.AppliedYawDegrees);
                FixedCharacterRuntimeState finalState = evaluation.CandidateState;
                var result = new SimulationActorTickResult(
                    evaluation.ActorId,
                    evaluation.Tick,
                    finalState,
                    FixedCharacterRuntimeStateCodec.ComputeHash(finalState),
                    bodySample,
                    expected.Motion,
                    evaluation.GameplayFacts,
                    evaluation.PresentationCommands,
                    evaluation.TraceRecords);
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
                evaluation.Consume();
            }
        }
    }

    public sealed class FixedAbilityFinalizeReadPorts : ISimulationPipelineReadPortSet
    {
        public FixedAbilityFinalizeReadPorts(
            IReadOnlySimulationPipelineProductPort<FixedCharacterEvaluationResultBatch> characterEvaluationResults,
            IReadOnlySimulationPipelineProductPort<WorldSolveBatchResult> world,
            IFixedCharacterRuntimePort characterRuntime,
            IFixedWorkingStateReadPort workingState,
            IFixedDiagnosticsRuntimePort diagnostics)
        {
            CharacterEvaluationResults = characterEvaluationResults ?? throw new ArgumentNullException(nameof(characterEvaluationResults));
            World = world ?? throw new ArgumentNullException(nameof(world));
            CharacterRuntime = characterRuntime ?? throw new ArgumentNullException(nameof(characterRuntime));
            WorkingState = workingState ?? throw new ArgumentNullException(nameof(workingState));
            Diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
        }

        public IReadOnlySimulationPipelineProductPort<FixedCharacterEvaluationResultBatch> CharacterEvaluationResults { get; }
        public IReadOnlySimulationPipelineProductPort<WorldSolveBatchResult> World { get; }
        public IFixedCharacterRuntimePort CharacterRuntime { get; }
        public IFixedWorkingStateReadPort WorkingState { get; }
        public IFixedDiagnosticsRuntimePort Diagnostics { get; }
    }

    public sealed class FixedAbilityFinalizeWritePorts : ISimulationPipelineWritePortSet
    {
        public FixedAbilityFinalizeWritePorts(
            IAppendOnlySimulationPipelineProductWriter<FixedFinalizedActorResult> results)
        {
            Results = results ?? throw new ArgumentNullException(nameof(results));
        }

        public IAppendOnlySimulationPipelineProductWriter<FixedFinalizedActorResult> Results { get; }
    }

    static class FixedPipelineDiagnostics
    {
        public static void PublishOperations(
            ISimulationDiagnosticsSink sink,
            IReadOnlyList<SimulationTraceRecord> records,
            int start)
        {
            if (sink == null || !sink.IsEnabled || records == null)
                return;
            for (int i = Math.Max(0, start); i < records.Count; i++)
                sink.PublishOperation(records[i]);
        }
    }
}
