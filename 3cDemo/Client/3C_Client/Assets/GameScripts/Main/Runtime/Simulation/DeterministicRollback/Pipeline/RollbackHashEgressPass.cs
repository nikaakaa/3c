using System;
using System.Collections.Generic;
using ThirdPersonSimulation.Fixed;

namespace ThirdPersonSimulation.DeterministicRollback
{
    public static class RollbackSourceEgressChannels
    {
        public const string StateHash = "deterministic-rollback.state-hash";
        public const string StateHashSchema = "deterministic-rollback-state-hash";
        public const int StateHashSchemaVersion = 2;
    }

    public sealed class RollbackHashEgressPassRuntimeFactory : IFixedPipelinePassRuntimeFactory
    {
        readonly SimulationPipelinePassFactoryDescriptor m_Descriptor;
        readonly DeterministicRollbackModelPolicy m_Policy;
        readonly RollbackRuntimeState m_State;

        public RollbackHashEgressPassRuntimeFactory(
            SimulationPipelinePassFactoryDescriptor descriptor,
            DeterministicRollbackModelPolicy policy,
            RollbackRuntimeState state)
        {
            m_Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
            m_Policy = policy ?? throw new ArgumentNullException(nameof(policy));
            m_State = state ?? throw new ArgumentNullException(nameof(state));
        }

        public SimulationPipelinePassFactoryDescriptor Descriptor => m_Descriptor;

        public IFixedCompiledPipelinePassRuntime Create(FixedPipelinePassRuntimeFactoryContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            var reads = new RollbackHashEgressReadPorts(
                context.BindTargetPort<IFixedCompletedStepReadPort>(FixedPipelineRuntimePortIds.CompletedSteps));
            var writes = new RollbackHashEgressWritePorts(
                context.Products.BindAppendWriter<FixedSourceEgressRecord>(SimulationPipelineProducts.SourceEgress));
            return new FixedEgressPassRuntimeAdapter<RollbackHashEgressReadPorts, RollbackHashEgressWritePorts>(
                new RollbackHashEgressPassRuntime(context.Pass.Descriptor, m_Policy, m_State),
                reads,
                writes);
        }
    }

    public sealed class RollbackHashEgressPassRuntime :
        FixedPipelinePassRuntimeBase,
        ISimulationEgressPassRuntime<RollbackHashEgressReadPorts, RollbackHashEgressWritePorts>
    {
        readonly DeterministicRollbackModelPolicy m_Policy;
        readonly RollbackRuntimeState m_State;

        public RollbackHashEgressPassRuntime(
            SimulationPipelinePassDescriptor descriptor,
            DeterministicRollbackModelPolicy policy,
            RollbackRuntimeState state) : base(descriptor)
        {
            m_Policy = policy ?? throw new ArgumentNullException(nameof(policy));
            m_State = state ?? throw new ArgumentNullException(nameof(state));
        }

        public void Execute(
            SimulationPipelineEgressContext context,
            RollbackHashEgressReadPorts readPorts,
            RollbackHashEgressWritePorts writePorts)
        {
            RequireExecution();
            while (m_State.TryReserveNextHashTick(m_Policy.HashCadenceTicks, out SimulationTick tick))
            {
                FixedSimulationSessionSnapshot snapshot = GetSnapshot(tick, readPorts.CompletedSteps.Steps);
                RollbackStateHashReport report = BuildReport(snapshot, m_State.LocalPeerId, m_State.RosterHash);
                ActorId owner = snapshot.World.Actors[0].ActorId;
                writePorts.Egress.Append(
                    new SimulationPipelineAppendEntryIdentity(owner, tick, tick.Value, context.Source),
                    new FixedSourceEgressRecord(
                        owner,
                        tick,
                        RollbackSourceEgressChannels.StateHash,
                        RollbackSourceEgressChannels.StateHashSchema,
                        RollbackSourceEgressChannels.StateHashSchemaVersion,
                        RollbackProtocolCodec.WriteCanonicalPayload(report)));
            }
        }

        FixedSimulationSessionSnapshot GetSnapshot(
            SimulationTick tick,
            IReadOnlyList<FixedCompletedSimulationStep> completed)
        {
            for (int i = 0; i < completed.Count; i++)
            {
                FixedSimulationStepSnapshot step = completed[i].StepSnapshot;
                if (step != null && step.Tick == tick)
                    return new FixedSimulationSessionSnapshot(step.CompositionIdentity, step.World, step.PipelineProjection);
            }
            return m_State.Snapshots.GetRequired(tick);
        }

        static RollbackStateHashReport BuildReport(
            FixedSimulationSessionSnapshot snapshot,
            string localPeerId,
            StableHash rosterHash)
        {
            SimulationWorldSnapshot world = snapshot.World;
            WorldSimulationState worldState = world.DecodeWorldState();
            StableHash kccHash = SimulationCanonicalPayloadHash.Compute(
                worldState.SolverStatePayload.ToArray());
            var actors = new RollbackActorHash[world.Actors.Count];
            for (int i = 0; i < actors.Length; i++)
            {
                SimulationActorSnapshot actor = world.Actors[i];
                actors[i] = new RollbackActorHash(
                    actor.ActorId,
                    actor.StateHash.Value,
                    new[]
                    {
                        new KeyValuePair<string, StableHash>("character-state", actor.StateHash.Value),
                        new KeyValuePair<string, StableHash>("program", actor.ProgramHash.Value),
                        new KeyValuePair<string, StableHash>("layout", actor.LayoutHash.Value)
                    });
            }
            return new RollbackStateHashReport(
                localPeerId,
                world.Tick,
                world.WorldHash.Value,
                rosterHash,
                kccHash,
                actors);
        }
    }

    public sealed class RollbackHashEgressReadPorts : ISimulationPipelineReadPortSet
    {
        public RollbackHashEgressReadPorts(IFixedCompletedStepReadPort completedSteps)
        {
            CompletedSteps = completedSteps ?? throw new ArgumentNullException(nameof(completedSteps));
        }

        public IFixedCompletedStepReadPort CompletedSteps { get; }
    }

    public sealed class RollbackHashEgressWritePorts : ISimulationPipelineWritePortSet
    {
        public RollbackHashEgressWritePorts(IAppendOnlySimulationPipelineProductWriter<FixedSourceEgressRecord> egress)
        {
            Egress = egress ?? throw new ArgumentNullException(nameof(egress));
        }

        public IAppendOnlySimulationPipelineProductWriter<FixedSourceEgressRecord> Egress { get; }
    }
}
