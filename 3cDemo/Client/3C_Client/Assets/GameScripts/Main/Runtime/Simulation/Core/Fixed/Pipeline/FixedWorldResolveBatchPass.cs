using ThirdPersonSimulation;
using System;

namespace ThirdPersonSimulation.Fixed
{
    public sealed class FixedWorldResolveBatchPassRuntimeFactory : IFixedPipelinePassRuntimeFactory
    {
        static readonly SimulationPipelinePassFactoryDescriptor s_Descriptor =
            StandardFixedPipelinePassContracts.CreateFactoryDescriptor(
                StandardFixedPipelinePassContracts.WorldResolveBatch);

        public SimulationPipelinePassFactoryDescriptor Descriptor => s_Descriptor;

        public IFixedCompiledPipelinePassRuntime Create(FixedPipelinePassRuntimeFactoryContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            var reads = new FixedWorldResolveBatchReadPorts(
                context.Products.BindExclusiveReader<WorldSolveBatchRequest>(SimulationPipelineProducts.WorldSolveBatchRequest),
                context.BindSolverPort<IFixedWorldSolverRuntimePort>(FixedPipelineRuntimePortIds.WorldSolver),
                context.BindDiagnosticsPort<IFixedDiagnosticsRuntimePort>(FixedPipelineRuntimePortIds.Diagnostics));
            var writes = new FixedWorldResolveBatchWritePorts(
                context.Products.BindExclusiveWriter<WorldSolveBatchResult>(SimulationPipelineProducts.WorldSolveBatchResult));
            return new FixedStepPassRuntimeAdapter<FixedWorldResolveBatchReadPorts, FixedWorldResolveBatchWritePorts>(
                new FixedWorldResolveBatchPassRuntime(context.Pass.Descriptor),
                reads,
                writes);
        }
    }

    public sealed class FixedWorldResolveBatchPassRuntime :
        FixedPipelinePassRuntimeBase,
        ISimulationStepPassRuntime<FixedWorldResolveBatchReadPorts, FixedWorldResolveBatchWritePorts>
    {
        public FixedWorldResolveBatchPassRuntime(SimulationPipelinePassDescriptor descriptor)
            : base(descriptor)
        {
        }

        public void Execute(
            SimulationPipelineStepTransactionContext context,
            FixedWorldResolveBatchReadPorts readPorts,
            FixedWorldResolveBatchWritePorts writePorts)
        {
            RequireExecution();
            WorldSolveBatchRequest request = readPorts.Request.Read();
            if (request.Tick != context.Tick)
                throw new InvalidOperationException("World Resolve Pass request belongs to another Step.");
            ICharacterWorldSolver solver = readPorts.Solver.Solver;
            WorldSolveBatchResult result = solver.ResolveBatch(request, readPorts.Diagnostics.Sink) ??
                throw new InvalidOperationException("World Solver returned no batch result.");
            if (result.Tick != context.Tick || !result.Request.RequestHash.Equals(request.RequestHash) ||
                !result.SolverId.Equals(solver.Descriptor.ImplementationId) ||
                !string.Equals(result.SolverVersion, solver.Descriptor.Version, StringComparison.Ordinal) ||
                result.Results.Count != request.Requests.Count)
            {
                throw new InvalidOperationException("World Solver returned a batch for another request, Tick or Solver identity.");
            }
            writePorts.Result.Write(result);
        }
    }

    public sealed class FixedWorldResolveBatchReadPorts : ISimulationPipelineReadPortSet
    {
        public FixedWorldResolveBatchReadPorts(
            IReadOnlySimulationPipelineProductPort<WorldSolveBatchRequest> request,
            IFixedWorldSolverRuntimePort solver,
            IFixedDiagnosticsRuntimePort diagnostics)
        {
            Request = request ?? throw new ArgumentNullException(nameof(request));
            Solver = solver ?? throw new ArgumentNullException(nameof(solver));
            Diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
        }

        public IReadOnlySimulationPipelineProductPort<WorldSolveBatchRequest> Request { get; }
        public IFixedWorldSolverRuntimePort Solver { get; }
        public IFixedDiagnosticsRuntimePort Diagnostics { get; }
    }

    public sealed class FixedWorldResolveBatchWritePorts : ISimulationPipelineWritePortSet
    {
        public FixedWorldResolveBatchWritePorts(
            IExclusiveSimulationPipelineProductWriter<WorldSolveBatchResult> result)
        {
            Result = result ?? throw new ArgumentNullException(nameof(result));
        }

        public IExclusiveSimulationPipelineProductWriter<WorldSolveBatchResult> Result { get; }
    }
}

