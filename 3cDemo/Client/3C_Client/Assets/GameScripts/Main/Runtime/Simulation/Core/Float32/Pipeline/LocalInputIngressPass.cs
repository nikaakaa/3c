using System;

namespace ThirdPersonSimulation
{
    public sealed class LocalInputIngressPassRuntimeFactory : IFloat32PipelinePassRuntimeFactory
    {
        static readonly SimulationPipelinePassFactoryDescriptor s_Descriptor =
            StandardFloat32PipelinePassContracts.CreateFactoryDescriptor(
                StandardFloat32PipelinePassContracts.LocalInputIngress);

        public SimulationPipelinePassFactoryDescriptor Descriptor => s_Descriptor;

        public IFloat32CompiledPipelinePassRuntime Create(Float32PipelinePassRuntimeFactoryContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            var reads = new LocalInputIngressReadPorts(
                context.BindSourcePort<IFloat32LocalInputSourcePort>(Float32LocalInputSourcePortContract.PortId),
                context.BindTargetPort<IFloat32ProgramRuntimePort>(Float32PipelineRuntimePortIds.ProgramRuntime));
            var writes = new LocalInputIngressWritePorts(
                context.Products.BindExclusiveWriter<Float32CanonicalInputBatch>(SimulationPipelineProducts.CanonicalInputs),
                context.Products.BindExclusiveWriter<Float32TypedIngressBatch>(SimulationPipelineProducts.TypedIngress));
            return new Float32IngressPassRuntimeAdapter<LocalInputIngressReadPorts, LocalInputIngressWritePorts>(
                new LocalInputIngressPassRuntime(context.Pass.Descriptor),
                reads,
                writes);
        }
    }

    public sealed class LocalInputIngressPassRuntime :
        Float32PipelinePassRuntimeBase,
        ISimulationIngressPassRuntime<LocalInputIngressReadPorts, LocalInputIngressWritePorts>
    {
        public LocalInputIngressPassRuntime(SimulationPipelinePassDescriptor descriptor)
            : base(descriptor)
        {
        }

        public void Execute(
            SimulationPipelineIngressContext context,
            LocalInputIngressReadPorts readPorts,
            LocalInputIngressWritePorts writePorts)
        {
            RequireExecution();
            SimulationProgramCatalog catalog = readPorts.ProgramRuntime.Catalog;
            var nextTick = new SimulationTick(checked(context.CurrentCompletedTick + 1));
            Float32LocalInputFrame frame = readPorts.Source.Read(
                context.Source,
                nextTick,
                catalog.NumericProfile,
                catalog.TickRate,
                readPorts.ProgramRuntime.Roster);
            writePorts.CanonicalInputs.Write(frame.CanonicalInputs);
            writePorts.TypedIngress.Write(frame.TypedIngress);
        }
    }

    public sealed class LocalInputIngressReadPorts : ISimulationPipelineReadPortSet
    {
        public LocalInputIngressReadPorts(
            IFloat32LocalInputSourcePort source,
            IFloat32ProgramRuntimePort programRuntime)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
            ProgramRuntime = programRuntime ?? throw new ArgumentNullException(nameof(programRuntime));
        }

        public IFloat32LocalInputSourcePort Source { get; }
        public IFloat32ProgramRuntimePort ProgramRuntime { get; }
    }

    public sealed class LocalInputIngressWritePorts : ISimulationPipelineWritePortSet
    {
        public LocalInputIngressWritePorts(
            IExclusiveSimulationPipelineProductWriter<Float32CanonicalInputBatch> canonicalInputs,
            IExclusiveSimulationPipelineProductWriter<Float32TypedIngressBatch> typedIngress)
        {
            CanonicalInputs = canonicalInputs ?? throw new ArgumentNullException(nameof(canonicalInputs));
            TypedIngress = typedIngress ?? throw new ArgumentNullException(nameof(typedIngress));
        }

        public IExclusiveSimulationPipelineProductWriter<Float32CanonicalInputBatch> CanonicalInputs { get; }
        public IExclusiveSimulationPipelineProductWriter<Float32TypedIngressBatch> TypedIngress { get; }
    }
}
