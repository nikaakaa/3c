using System;
using ThirdPersonSimulation.Fixed;

namespace ThirdPersonSimulation.DeterministicRollback
{
    public sealed class RollbackInputIngressPassRuntimeFactory : IFixedPipelinePassRuntimeFactory
    {
        readonly SimulationPipelinePassFactoryDescriptor m_Descriptor;
        readonly RollbackRuntimeState m_State;

        public RollbackInputIngressPassRuntimeFactory(
            SimulationPipelinePassFactoryDescriptor descriptor,
            RollbackRuntimeState state)
        {
            m_Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
            m_State = state ?? throw new ArgumentNullException(nameof(state));
        }

        public SimulationPipelinePassFactoryDescriptor Descriptor => m_Descriptor;

        public IFixedCompiledPipelinePassRuntime Create(FixedPipelinePassRuntimeFactoryContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            var reads = new RollbackInputIngressReadPorts(
                context.BindSourcePort<IRollbackInputSourcePort>(RollbackSourcePortContracts.InputPortId),
                context.BindTargetPort<IFixedProgramRuntimePort>(FixedPipelineRuntimePortIds.ProgramRuntime));
            var writes = new RollbackInputIngressWritePorts(
                context.Products.BindExclusiveWriter<RollbackIngressBatch>(RollbackPipelineProducts.Ingress));
            return new FixedIngressPassRuntimeAdapter<RollbackInputIngressReadPorts, RollbackInputIngressWritePorts>(
                new RollbackInputIngressPassRuntime(context.Pass.Descriptor, m_State),
                reads,
                writes);
        }
    }

    public sealed class RollbackInputIngressPassRuntime :
        FixedPipelinePassRuntimeBase,
        ISimulationIngressPassRuntime<RollbackInputIngressReadPorts, RollbackInputIngressWritePorts>
    {
        readonly RollbackRuntimeState m_State;

        public RollbackInputIngressPassRuntime(
            SimulationPipelinePassDescriptor descriptor,
            RollbackRuntimeState state) : base(descriptor)
        {
            m_State = state ?? throw new ArgumentNullException(nameof(state));
        }

        public void Execute(
            SimulationPipelineIngressContext context,
            RollbackInputIngressReadPorts readPorts,
            RollbackInputIngressWritePorts writePorts)
        {
            RequireExecution();
            m_State.BeginOuterTransaction(context.CurrentCompletedTick);
            var nextTick = new SimulationTick(checked(context.CurrentCompletedTick + 1));
            RollbackIngressBatch batch = readPorts.Source.Read(
                context.Source,
                nextTick,
                readPorts.ProgramRuntime.Roster) ??
                throw new InvalidOperationException("Rollback input Source returned no ingress batch.");
            if (batch.Predicted.Tick != nextTick || batch.Predicted.Actors.Count != readPorts.ProgramRuntime.Roster.Count)
                throw new InvalidOperationException("Rollback predicted bundle does not match the next Tick or locked roster.");
            for (int i = 0; i < batch.Predicted.Actors.Count; i++)
            {
                if (!batch.Predicted.Actors[i].ActorId.Equals(readPorts.ProgramRuntime.Roster[i].ActorId))
                    throw new InvalidOperationException("Rollback predicted bundle Actor order does not match the locked roster.");
            }
            for (int i = 0; i < batch.RelayedExplicitArrivals.Count; i++)
                m_State.RecordRelayedExplicit(batch.RelayedExplicitArrivals[i]);
            m_State.RecordPredicted(batch.Predicted);
            for (int i = 0; i < batch.CanonicalArrivals.Count; i++)
                m_State.RecordCanonical(batch.CanonicalArrivals[i]);
            m_State.RecordRelayConfirmedTick(batch.ConfirmedTick);
            writePorts.Ingress.Write(batch);
        }
    }

    public sealed class RollbackInputIngressReadPorts : ISimulationPipelineReadPortSet
    {
        public RollbackInputIngressReadPorts(
            IRollbackInputSourcePort source,
            IFixedProgramRuntimePort programRuntime)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
            ProgramRuntime = programRuntime ?? throw new ArgumentNullException(nameof(programRuntime));
        }

        public IRollbackInputSourcePort Source { get; }
        public IFixedProgramRuntimePort ProgramRuntime { get; }
    }

    public sealed class RollbackInputIngressWritePorts : ISimulationPipelineWritePortSet
    {
        public RollbackInputIngressWritePorts(
            IExclusiveSimulationPipelineProductWriter<RollbackIngressBatch> ingress)
        {
            Ingress = ingress ?? throw new ArgumentNullException(nameof(ingress));
        }

        public IExclusiveSimulationPipelineProductWriter<RollbackIngressBatch> Ingress { get; }
    }
}
