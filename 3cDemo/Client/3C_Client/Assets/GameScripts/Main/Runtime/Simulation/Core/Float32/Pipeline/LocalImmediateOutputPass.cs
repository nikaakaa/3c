using System;
using System.Collections.Generic;

namespace ThirdPersonSimulation
{
    public sealed class LocalImmediateOutputPassRuntimeFactory : IFloat32PipelinePassRuntimeFactory
    {
        static readonly SimulationPipelinePassFactoryDescriptor s_Descriptor =
            StandardFloat32PipelinePassContracts.CreateFactoryDescriptor(
                StandardFloat32PipelinePassContracts.LocalImmediateOutput);

        public SimulationPipelinePassFactoryDescriptor Descriptor => s_Descriptor;

        public IFloat32CompiledPipelinePassRuntime Create(Float32PipelinePassRuntimeFactoryContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            var reads = new LocalImmediateOutputReadPorts(
                context.Products.BindAppendReader<Float32FinalizedActorResult>(SimulationPipelineProducts.FinalizedStepResult));
            var writes = new LocalImmediateOutputWritePorts(
                context.Products.BindExclusiveWriter<SimulationPipelineOutputDispositionSet>(SimulationPipelineProducts.OutputDispositionSet));
            return new Float32EgressPassRuntimeAdapter<LocalImmediateOutputReadPorts, LocalImmediateOutputWritePorts>(
                new LocalImmediateOutputPassRuntime(context.Pass.Descriptor),
                reads,
                writes);
        }
    }

    public sealed class LocalImmediateOutputPassRuntime :
        Float32PipelinePassRuntimeBase,
        ISimulationEgressPassRuntime<LocalImmediateOutputReadPorts, LocalImmediateOutputWritePorts>
    {
        readonly List<SimulationOutputDisposition> m_Dispositions = new List<SimulationOutputDisposition>();

        public LocalImmediateOutputPassRuntime(SimulationPipelinePassDescriptor descriptor)
            : base(descriptor)
        {
        }

        public void Execute(
            SimulationPipelineEgressContext context,
            LocalImmediateOutputReadPorts readPorts,
            LocalImmediateOutputWritePorts writePorts)
        {
            RequireExecution();
            writePorts.Dispositions.Write(Float32ImmediateOutputDispositionBuilder.Build(
                context.TransactionIdentity,
                readPorts.Results,
                m_Dispositions));
        }
    }

    public static class Float32ImmediateOutputDispositionBuilder
    {
        public static SimulationPipelineOutputDispositionSet Build(
            StableHash transactionIdentity,
            IReadOnlySimulationPipelineAppendPort<Float32FinalizedActorResult> results,
            List<SimulationOutputDisposition> dispositions)
        {
            if (results == null)
                throw new ArgumentNullException(nameof(results));
            if (dispositions == null)
                throw new ArgumentNullException(nameof(dispositions));
            dispositions.Clear();
            try
            {
                for (int i = 0; i < results.Count; i++)
                {
                    SimulationActorTickResult result = results.Get(i).Value.Result;
                    for (int eventIndex = 0; eventIndex < result.GameplayFacts.Count; eventIndex++)
                    {
                        dispositions.Add(new SimulationOutputDisposition(
                            result.GameplayFacts[eventIndex].Header.EventId,
                            result.GameplayFacts[eventIndex].Header.ActorId,
                            SimulationOutputDispositionKind.Publish));
                    }
                    for (int eventIndex = 0; eventIndex < result.PresentationCommands.Count; eventIndex++)
                    {
                        dispositions.Add(new SimulationOutputDisposition(
                            result.PresentationCommands[eventIndex].Header.EventId,
                            result.PresentationCommands[eventIndex].Header.ActorId,
                            SimulationOutputDispositionKind.Publish));
                    }
                }
                return new SimulationPipelineOutputDispositionSet(transactionIdentity, dispositions);
            }
            finally
            {
                dispositions.Clear();
            }
        }
    }

    public sealed class LocalImmediateOutputReadPorts : ISimulationPipelineReadPortSet
    {
        public LocalImmediateOutputReadPorts(
            IReadOnlySimulationPipelineAppendPort<Float32FinalizedActorResult> results)
        {
            Results = results ?? throw new ArgumentNullException(nameof(results));
        }

        public IReadOnlySimulationPipelineAppendPort<Float32FinalizedActorResult> Results { get; }
    }

    public sealed class LocalImmediateOutputWritePorts : ISimulationPipelineWritePortSet
    {
        public LocalImmediateOutputWritePorts(
            IExclusiveSimulationPipelineProductWriter<SimulationPipelineOutputDispositionSet> dispositions)
        {
            Dispositions = dispositions ?? throw new ArgumentNullException(nameof(dispositions));
        }

        public IExclusiveSimulationPipelineProductWriter<SimulationPipelineOutputDispositionSet> Dispositions { get; }
    }
}
