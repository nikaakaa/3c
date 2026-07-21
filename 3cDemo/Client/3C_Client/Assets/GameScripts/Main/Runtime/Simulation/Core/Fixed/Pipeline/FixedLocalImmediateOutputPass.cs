using System;
using System.Collections.Generic;
using ThirdPersonSimulation;

namespace ThirdPersonSimulation.Fixed
{
    public sealed class FixedLocalImmediateOutputPassRuntimeFactory : IFixedPipelinePassRuntimeFactory
    {
        static readonly SimulationPipelinePassFactoryDescriptor s_Descriptor =
            StandardFixedLocalPipelinePassContracts.CreateFactoryDescriptor(
                StandardFixedLocalPipelinePassContracts.LocalImmediateOutput);

        public SimulationPipelinePassFactoryDescriptor Descriptor => s_Descriptor;

        public IFixedCompiledPipelinePassRuntime Create(FixedPipelinePassRuntimeFactoryContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            var reads = new FixedLocalImmediateOutputReadPorts(
                context.Products.BindAppendReader<FixedFinalizedActorResult>(SimulationPipelineProducts.FinalizedStepResult));
            var writes = new FixedLocalImmediateOutputWritePorts(
                context.Products.BindExclusiveWriter<SimulationPipelineOutputDispositionSet>(SimulationPipelineProducts.OutputDispositionSet));
            return new FixedEgressPassRuntimeAdapter<FixedLocalImmediateOutputReadPorts, FixedLocalImmediateOutputWritePorts>(
                new FixedLocalImmediateOutputPassRuntime(context.Pass.Descriptor),
                reads,
                writes);
        }
    }

    public sealed class FixedLocalImmediateOutputPassRuntime :
        FixedPipelinePassRuntimeBase,
        ISimulationEgressPassRuntime<FixedLocalImmediateOutputReadPorts, FixedLocalImmediateOutputWritePorts>
    {
        readonly List<SimulationOutputDisposition> m_Dispositions = new List<SimulationOutputDisposition>();

        public FixedLocalImmediateOutputPassRuntime(SimulationPipelinePassDescriptor descriptor)
            : base(descriptor)
        {
        }

        public void Execute(
            SimulationPipelineEgressContext context,
            FixedLocalImmediateOutputReadPorts readPorts,
            FixedLocalImmediateOutputWritePorts writePorts)
        {
            RequireExecution();
            m_Dispositions.Clear();
            try
            {
                for (int i = 0; i < readPorts.Results.Count; i++)
                {
                    SimulationActorTickResult result = readPorts.Results.Get(i).Value.Result;
                    for (int eventIndex = 0; eventIndex < result.GameplayFacts.Count; eventIndex++)
                    {
                        m_Dispositions.Add(new SimulationOutputDisposition(
                            result.GameplayFacts[eventIndex].Header.EventId,
                            result.GameplayFacts[eventIndex].Header.ActorId,
                            SimulationOutputDispositionKind.Publish));
                    }
                    for (int eventIndex = 0; eventIndex < result.PresentationCommands.Count; eventIndex++)
                    {
                        m_Dispositions.Add(new SimulationOutputDisposition(
                            result.PresentationCommands[eventIndex].Header.EventId,
                            result.PresentationCommands[eventIndex].Header.ActorId,
                            SimulationOutputDispositionKind.Publish));
                    }
                }
                writePorts.Dispositions.Write(new SimulationPipelineOutputDispositionSet(
                    context.TransactionIdentity,
                    m_Dispositions));
            }
            finally
            {
                m_Dispositions.Clear();
            }
        }
    }

    public sealed class FixedLocalImmediateOutputReadPorts : ISimulationPipelineReadPortSet
    {
        public FixedLocalImmediateOutputReadPorts(
            IReadOnlySimulationPipelineAppendPort<FixedFinalizedActorResult> results)
        {
            Results = results ?? throw new ArgumentNullException(nameof(results));
        }

        public IReadOnlySimulationPipelineAppendPort<FixedFinalizedActorResult> Results { get; }
    }

    public sealed class FixedLocalImmediateOutputWritePorts : ISimulationPipelineWritePortSet
    {
        public FixedLocalImmediateOutputWritePorts(
            IExclusiveSimulationPipelineProductWriter<SimulationPipelineOutputDispositionSet> dispositions)
        {
            Dispositions = dispositions ?? throw new ArgumentNullException(nameof(dispositions));
        }

        public IExclusiveSimulationPipelineProductWriter<SimulationPipelineOutputDispositionSet> Dispositions { get; }
    }
}
