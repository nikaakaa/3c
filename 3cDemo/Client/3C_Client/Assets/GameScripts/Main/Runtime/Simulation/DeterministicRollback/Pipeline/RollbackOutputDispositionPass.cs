using System;
using System.Collections.Generic;
using ThirdPersonSimulation.Fixed;

namespace ThirdPersonSimulation.DeterministicRollback
{
    public sealed class RollbackOutputDispositionPassRuntimeFactory : IFixedPipelinePassRuntimeFactory
    {
        readonly SimulationPipelinePassFactoryDescriptor m_Descriptor;
        readonly int m_MaximumOutputRecords;

        public RollbackOutputDispositionPassRuntimeFactory(
            SimulationPipelinePassFactoryDescriptor descriptor,
            int maximumOutputRecords)
        {
            m_Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
            if (maximumOutputRecords <= 0)
                throw new ArgumentOutOfRangeException(nameof(maximumOutputRecords));
            m_MaximumOutputRecords = maximumOutputRecords;
        }

        public SimulationPipelinePassFactoryDescriptor Descriptor => m_Descriptor;

        public IFixedCompiledPipelinePassRuntime Create(FixedPipelinePassRuntimeFactoryContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            var reads = new RollbackOutputDispositionReadPorts(
                context.Products.BindAppendReader<SimulationActorTickResult>(SimulationPipelineProducts.FinalizedStepResult),
                context.BindTargetPort<IFixedCompletedStepReadPort>(FixedPipelineRuntimePortIds.CompletedSteps));
            var writes = new RollbackOutputDispositionWritePorts(
                context.Products.BindExclusiveWriter<SimulationPipelineOutputDispositionSet>(SimulationPipelineProducts.OutputDispositionSet));
            return new FixedEgressPassRuntimeAdapter<RollbackOutputDispositionReadPorts, RollbackOutputDispositionWritePorts>(
                new RollbackOutputDispositionPassRuntime(context.Pass.Descriptor, m_MaximumOutputRecords),
                reads,
                writes);
        }
    }

    public sealed class RollbackOutputDispositionPassRuntime :
        FixedPipelinePassRuntimeBase,
        ISimulationEgressPassRuntime<RollbackOutputDispositionReadPorts, RollbackOutputDispositionWritePorts>
    {
        readonly int m_MaximumOutputRecords;
        readonly Dictionary<int, SimulationOutputDisposition[]> m_DispositionScratches =
            new Dictionary<int, SimulationOutputDisposition[]>();

        public RollbackOutputDispositionPassRuntime(
            SimulationPipelinePassDescriptor descriptor,
            int maximumOutputRecords)
            : base(descriptor)
        {
            if (maximumOutputRecords <= 0)
                throw new ArgumentOutOfRangeException(nameof(maximumOutputRecords));
            m_MaximumOutputRecords = maximumOutputRecords;
        }

        public void Execute(
            SimulationPipelineEgressContext context,
            RollbackOutputDispositionReadPorts readPorts,
            RollbackOutputDispositionWritePorts writePorts)
        {
            RequireExecution();
            int count = CountDispositions(readPorts.CompletedSteps);
            if (count > m_MaximumOutputRecords)
                throw new InvalidOperationException(
                    $"Rollback output disposition capacity '{m_MaximumOutputRecords}' is exhausted by '{count}' records.");
            SimulationOutputDisposition[] dispositions = RentDispositions(count);
            FillDispositions(readPorts.CompletedSteps, dispositions);
            writePorts.Dispositions.Write(SimulationPipelineOutputDispositionSet.FromOwnedDispositions(
                context.TransactionIdentity,
                dispositions));
        }

        static int CountDispositions(IFixedCompletedStepReadPort completedSteps)
        {
            int count = 0;
            for (int stepIndex = 0; stepIndex < completedSteps.Steps.Count; stepIndex++)
            {
                FixedCompletedSimulationStep step = completedSteps.Steps[stepIndex];
                for (int actorIndex = 0; actorIndex < step.Result.Actors.Count; actorIndex++)
                {
                    SimulationActorTickResult actor = step.Result.Actors[actorIndex];
                    count += actor.GameplayFacts.Count + actor.PresentationCommands.Count;
                }
            }
            return count;
        }

        SimulationOutputDisposition[] RentDispositions(int count)
        {
            if (count == 0)
                return Array.Empty<SimulationOutputDisposition>();
            if (!m_DispositionScratches.TryGetValue(count, out SimulationOutputDisposition[] scratch))
            {
                scratch = new SimulationOutputDisposition[count];
                m_DispositionScratches.Add(count, scratch);
            }
            return scratch;
        }

        static void FillDispositions(
            IFixedCompletedStepReadPort completedSteps,
            SimulationOutputDisposition[] dispositions)
        {
            int index = 0;
            for (int stepIndex = 0; stepIndex < completedSteps.Steps.Count; stepIndex++)
            {
                FixedCompletedSimulationStep step = completedSteps.Steps[stepIndex];
                for (int actorIndex = 0; actorIndex < step.Result.Actors.Count; actorIndex++)
                {
                    SimulationActorTickResult actor = step.Result.Actors[actorIndex];
                    for (int i = 0; i < actor.GameplayFacts.Count; i++)
                        dispositions[index++] = Create(actor.GameplayFacts[i]);
                    for (int i = 0; i < actor.PresentationCommands.Count; i++)
                        dispositions[index++] = Create(actor.PresentationCommands[i]);
                }
            }
        }

        static SimulationOutputDisposition Create(GameplayFact fact)
        {
            SimulationOutputDispositionKind kind = fact.Kind == GameplayFactKind.Cue
                ? SimulationOutputDispositionKind.Defer
                : SimulationOutputDispositionKind.Publish;
            return new SimulationOutputDisposition(fact.Header.EventId, fact.Header.ActorId, kind);
        }

        static SimulationOutputDisposition Create(PresentationCommand command)
        {
            SimulationOutputDispositionKind kind = command.Kind == PresentationCommandKind.Cue ||
                                                   command.Kind == PresentationCommandKind.Vfx ||
                                                   command.Kind == PresentationCommandKind.Ui ||
                                                   command.Kind == PresentationCommandKind.CompleteProducer ||
                                                   command.Kind == PresentationCommandKind.ReleaseProducer ||
                                                   command.Kind == PresentationCommandKind.ForceProducer
                ? SimulationOutputDispositionKind.Defer
                : SimulationOutputDispositionKind.Publish;
            return new SimulationOutputDisposition(command.Header.EventId, command.Header.ActorId, kind);
        }
    }

    public sealed class RollbackOutputDispositionReadPorts : ISimulationPipelineReadPortSet
    {
        public RollbackOutputDispositionReadPorts(
            IReadOnlySimulationPipelineAppendPort<SimulationActorTickResult> results,
            IFixedCompletedStepReadPort completedSteps)
        {
            Results = results ?? throw new ArgumentNullException(nameof(results));
            CompletedSteps = completedSteps ?? throw new ArgumentNullException(nameof(completedSteps));
        }

        public IReadOnlySimulationPipelineAppendPort<SimulationActorTickResult> Results { get; }
        public IFixedCompletedStepReadPort CompletedSteps { get; }
    }

    public sealed class RollbackOutputDispositionWritePorts : ISimulationPipelineWritePortSet
    {
        public RollbackOutputDispositionWritePorts(
            IExclusiveSimulationPipelineProductWriter<SimulationPipelineOutputDispositionSet> dispositions)
        {
            Dispositions = dispositions ?? throw new ArgumentNullException(nameof(dispositions));
        }

        public IExclusiveSimulationPipelineProductWriter<SimulationPipelineOutputDispositionSet> Dispositions { get; }
    }
}
