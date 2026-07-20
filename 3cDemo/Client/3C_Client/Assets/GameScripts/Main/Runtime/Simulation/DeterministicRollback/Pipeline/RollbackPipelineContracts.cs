using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ThirdPersonSimulation.Fixed;

namespace ThirdPersonSimulation.DeterministicRollback
{
    public sealed class RollbackIngressBatch
    {
        readonly ReadOnlyCollection<RollbackActorInputFrame> m_RelayedExplicitArrivals;
        readonly ReadOnlyCollection<RollbackCanonicalInputBundle> m_CanonicalArrivals;

        public RollbackIngressBatch(
            RollbackCanonicalInputBundle predicted,
            IEnumerable<RollbackActorInputFrame> relayedExplicitArrivals,
            IEnumerable<RollbackCanonicalInputBundle> canonicalArrivals,
            SimulationTick confirmedTick,
            FixedTypedIngressBatch typedIngress)
        {
            Predicted = predicted ?? throw new ArgumentNullException(nameof(predicted));
            ConfirmedTick = confirmedTick;
            TypedIngress = typedIngress ?? throw new ArgumentNullException(nameof(typedIngress));
            var explicitValues = relayedExplicitArrivals == null
                ? new List<RollbackActorInputFrame>()
                : new List<RollbackActorInputFrame>(relayedExplicitArrivals);
            explicitValues.Sort((left, right) =>
            {
                int tick = left.Tick.CompareTo(right.Tick);
                return tick != 0 ? tick : left.ActorId.CompareTo(right.ActorId);
            });
            for (int i = 0; i < explicitValues.Count; i++)
            {
                if (explicitValues[i] == null ||
                    explicitValues[i].Provenance != RollbackInputProvenance.RelayedExplicit ||
                    i > 0 && explicitValues[i - 1].Tick == explicitValues[i].Tick &&
                    explicitValues[i - 1].ActorId.Equals(explicitValues[i].ActorId))
                {
                    throw new ArgumentException("Rollback relayed explicit arrival batch is invalid.", nameof(relayedExplicitArrivals));
                }
            }
            m_RelayedExplicitArrivals = explicitValues.AsReadOnly();
            var values = canonicalArrivals == null
                ? new List<RollbackCanonicalInputBundle>()
                : new List<RollbackCanonicalInputBundle>(canonicalArrivals);
            values.Sort((left, right) => left.Tick.CompareTo(right.Tick));
            for (int i = 0; i < values.Count; i++)
            {
                if (values[i] == null || i > 0 && values[i - 1].Tick == values[i].Tick)
                    throw new ArgumentException("Rollback canonical arrival batch contains a missing or duplicate Tick.", nameof(canonicalArrivals));
            }
            m_CanonicalArrivals = values.AsReadOnly();
        }

        public RollbackCanonicalInputBundle Predicted { get; }
        public IReadOnlyList<RollbackActorInputFrame> RelayedExplicitArrivals => m_RelayedExplicitArrivals;
        public IReadOnlyList<RollbackCanonicalInputBundle> CanonicalArrivals => m_CanonicalArrivals;
        public SimulationTick ConfirmedTick { get; }
        public FixedTypedIngressBatch TypedIngress { get; }
    }

    public interface IRollbackInputSourceCheckpoint
    {
    }

    public readonly struct RollbackLocalInputDiagnosticsSnapshot
    {
        public RollbackLocalInputDiagnosticsSnapshot(
            int pendingOffensiveRequestCount,
            ulong oldestCaptureTick,
            ulong oldestEligibleTick)
        {
            PendingOffensiveRequestCount = pendingOffensiveRequestCount;
            OldestCaptureTick = oldestCaptureTick;
            OldestEligibleTick = oldestEligibleTick;
        }

        public int PendingOffensiveRequestCount { get; }
        public ulong OldestCaptureTick { get; }
        public ulong OldestEligibleTick { get; }
    }

    public readonly struct RollbackRemoteActorInputDiagnosticsSnapshot
    {
        public RollbackRemoteActorInputDiagnosticsSnapshot(
            ActorId actorId,
            ulong exactInputHitCount,
            ulong predictedFallbackCount,
            long lastArrivalDeltaTicks)
        {
            ActorId = actorId;
            ExactInputHitCount = exactInputHitCount;
            PredictedFallbackCount = predictedFallbackCount;
            LastArrivalDeltaTicks = lastArrivalDeltaTicks;
        }

        public ActorId ActorId { get; }
        public ulong ExactInputHitCount { get; }
        public ulong PredictedFallbackCount { get; }
        public long LastArrivalDeltaTicks { get; }
    }

    public readonly struct RollbackInputSourceDiagnosticsSnapshot
    {
        readonly RollbackRemoteActorInputDiagnosticsSnapshot[] m_RemoteActors;

        public RollbackInputSourceDiagnosticsSnapshot(
            RollbackLocalInputDiagnosticsSnapshot local,
            RollbackRemoteActorInputDiagnosticsSnapshot[] remoteActors,
            ulong relayedArrivalCount,
            ulong relayedArrivalLeadCount,
            ulong relayedArrivalLateCount,
            long lastRelayedArrivalDeltaTicks)
        {
            Local = local;
            m_RemoteActors = remoteActors ?? Array.Empty<RollbackRemoteActorInputDiagnosticsSnapshot>();
            RelayedArrivalCount = relayedArrivalCount;
            RelayedArrivalLeadCount = relayedArrivalLeadCount;
            RelayedArrivalLateCount = relayedArrivalLateCount;
            LastRelayedArrivalDeltaTicks = lastRelayedArrivalDeltaTicks;
        }

        public RollbackLocalInputDiagnosticsSnapshot Local { get; }
        public IReadOnlyList<RollbackRemoteActorInputDiagnosticsSnapshot> RemoteActors =>
            m_RemoteActors ?? Array.Empty<RollbackRemoteActorInputDiagnosticsSnapshot>();
        public ulong RelayedArrivalCount { get; }
        public ulong RelayedArrivalLeadCount { get; }
        public ulong RelayedArrivalLateCount { get; }
        public long LastRelayedArrivalDeltaTicks { get; }
    }

    public interface IRollbackInputSourcePort : ISimulationRuntimePort
    {
        RollbackIngressBatch Read(
            SimulationTickSourceIdentity outerSource,
            SimulationTick nextSimulationTick,
            IReadOnlyList<SimulationActorBinding> roster);
        IRollbackInputSourceCheckpoint CaptureCheckpoint();
        void RestoreCheckpoint(IRollbackInputSourceCheckpoint checkpoint);
        RollbackInputSourceDiagnosticsSnapshot CaptureDiagnostics();
    }

    public static class RollbackSourcePortContracts
    {
        public const string InputPortId = "deterministic-rollback.source.input";
        public const string InputSchemaId = "deterministic-rollback-input-source/3";

        public static readonly SimulationPipelinePortRequirement InputRequirement =
            new SimulationPipelinePortRequirement(
                SimulationPipelineBindingPortRole.Source,
                InputPortId,
                InputSchemaId,
                3,
                SimulationPortDirection.Input);
    }

    public static class RollbackPipelineProducts
    {
        public static readonly SimulationPipelineProductContract Ingress =
            new SimulationPipelineProductContract(
                new SimulationPipelineProductId("deterministic-rollback.ingress"),
                new SimulationPipelineProductSchemaVersion(3),
                "thirdperson.network-model.deterministic-rollback",
                SimulationPipelineProductMultiplicity.Exclusive,
                "deterministic-rollback-ingress/3",
                "relayed-explicit/predicted/canonical/relay-confirmed/typed-ingress",
                SimulationPipelinePhaseMask.Ingress,
                SimulationPipelinePhaseMask.Schedule,
                SimulationPipelineProductConsumption.InternalRequired);

        public static IFixedPipelineProductSlotFactory CreateRuntimeFactory()
        {
            return new FixedExclusiveProductSlotFactory<RollbackIngressBatch>(
                Ingress,
                FixedPipelineProductLifetime.OuterTransaction);
        }
    }

    public sealed class RollbackPipelinePassSet
    {
        public RollbackPipelinePassSet(DeterministicRollbackModelPolicy policy)
        {
            if (policy == null)
                throw new ArgumentNullException(nameof(policy));
            SimulationPipelineExecutionSupport all =
                SimulationPipelineExecutionSupport.Forward |
                SimulationPipelineExecutionSupport.Replay |
                SimulationPipelineExecutionSupport.Restore;
            Ingress = Create(
                RollbackPipelinePassIds.Ingress,
                SimulationPipelinePhase.Ingress,
                all,
                SimulationPipelinePassStateClass.ExternalSource,
                "deterministic-rollback-session-source",
                policy,
                new[] { Produce(RollbackPipelineProducts.Ingress) },
                new[]
                {
                    RollbackSourcePortContracts.InputRequirement,
                    Target(FixedPipelineRuntimePortIds.ProgramRuntime, FixedPipelineRuntimePortIds.ProgramRuntimeSchema)
                });
            Schedule = Create(
                RollbackPipelinePassIds.Schedule,
                SimulationPipelinePhase.Schedule,
                all,
                SimulationPipelinePassStateClass.Stateless,
                string.Empty,
                policy,
                new[]
                {
                    Consume(RollbackPipelineProducts.Ingress),
                    Produce(SimulationPipelineProducts.ExecutionPlan)
                },
                new[] { Target(FixedPipelineRuntimePortIds.ProgramRuntime, FixedPipelineRuntimePortIds.ProgramRuntimeSchema) });
            History = Create(
                RollbackPipelinePassIds.History,
                SimulationPipelinePhase.Step,
                all,
                SimulationPipelinePassStateClass.SnapshotParticipant,
                RollbackPipelinePassIds.HistoryStateOwner,
                policy,
                new[] { Consume(SimulationPipelineProducts.FinalizedStepResult) },
                Array.Empty<SimulationPipelinePortRequirement>());
            HashEgress = Create(
                RollbackPipelinePassIds.HashEgress,
                SimulationPipelinePhase.Egress,
                all,
                SimulationPipelinePassStateClass.Stateless,
                string.Empty,
                policy,
                new[] { Append(SimulationPipelineProducts.SourceEgress) },
                new[] { Target(FixedPipelineRuntimePortIds.CompletedSteps, FixedPipelineRuntimePortIds.CompletedStepsSchema) });
            OutputDisposition = Create(
                RollbackPipelinePassIds.OutputDisposition,
                SimulationPipelinePhase.Egress,
                all,
                SimulationPipelinePassStateClass.Stateless,
                string.Empty,
                policy,
                new[]
                {
                    Consume(SimulationPipelineProducts.FinalizedStepResult),
                    Produce(SimulationPipelineProducts.OutputDispositionSet)
                },
                new[] { Target(FixedPipelineRuntimePortIds.CompletedSteps, FixedPipelineRuntimePortIds.CompletedStepsSchema) });
        }

        public SimulationPipelinePassDescriptor Ingress { get; }
        public SimulationPipelinePassDescriptor Schedule { get; }
        public SimulationPipelinePassDescriptor History { get; }
        public SimulationPipelinePassDescriptor HashEgress { get; }
        public SimulationPipelinePassDescriptor OutputDisposition { get; }

        public SimulationPipelineDescriptor CreatePipeline()
        {
            return new SimulationPipelineDescriptor(
                new SimulationPipelineId(DeterministicRollbackModelIdentity.PipelineId),
                new SimulationPipelineRevision(DeterministicRollbackModelIdentity.PipelineRevision),
                new SimulationPipelineSchemaVersion(1),
                new[] { Ingress },
                new[] { Schedule },
                new[]
                {
                    StandardFixedPipelinePassContracts.ProgramEvaluate,
                    StandardFixedPipelinePassContracts.WorldResolveBatch,
                    StandardFixedPipelinePassContracts.ProgramFinalize,
                    History
                },
                new[] { HashEgress, OutputDisposition });
        }

        public SimulationPipelinePassFactoryDescriptor CreateFactoryDescriptor(
            SimulationPipelinePassDescriptor descriptor)
        {
            bool stateful = descriptor.StateClass == SimulationPipelinePassStateClass.SnapshotParticipant;
            return new SimulationPipelinePassFactoryDescriptor(
                new SimulationPipelinePassFactoryIdentity(
                    descriptor.PassId,
                    descriptor.ImplementationVersion,
                    "3",
                    StableHash.Compute("deterministic-rollback-pass-binding/3", descriptor.DescriptorHash.Value)),
                descriptor.Phase,
                FixedPassExecutionBackend.BackendId,
                FixedPassExecutionBackend.SemanticVersion,
                descriptor.ConfigurationHash,
                descriptor.ExecutionSupport,
                true,
                stateful,
                stateful,
                false,
                stateful ? RollbackPipelinePassIds.HistoryStateSchema : string.Empty,
                stateful ? 3 : 0);
        }

        static SimulationPipelinePassDescriptor Create(
            string passId,
            SimulationPipelinePhase phase,
            SimulationPipelineExecutionSupport support,
            SimulationPipelinePassStateClass stateClass,
            string stateOwner,
            DeterministicRollbackModelPolicy policy,
            IEnumerable<SimulationPipelineProductAccess> products,
            IEnumerable<SimulationPipelinePortRequirement> ports)
        {
            return new SimulationPipelinePassDescriptor(
                new SimulationPipelinePassId(passId),
                new SimulationPipelinePassImplementationVersion("3"),
                phase,
                StableHash.Compute("deterministic-rollback-pass-config/3", passId, policy.ConfigurationHash.Value),
                FixedSimulationNumericProfile.Value.Id,
                FixedSimulationNumericProfile.Value.AbiVersion,
                FixedPassExecutionBackend.BackendId,
                FixedPassExecutionBackend.SemanticVersion,
                WorldCapability.None,
                support,
                stateClass,
                stateOwner,
                products,
                ports);
        }

        static SimulationPipelineProductAccess Produce(SimulationPipelineProductContract product) =>
            new SimulationPipelineProductAccess(product, SimulationPipelineProductAccessKind.ExclusiveProducer);

        static SimulationPipelineProductAccess Consume(SimulationPipelineProductContract product) =>
            new SimulationPipelineProductAccess(product, SimulationPipelineProductAccessKind.ReadOnlyConsumer);

        static SimulationPipelineProductAccess Append(SimulationPipelineProductContract product) =>
            new SimulationPipelineProductAccess(product, SimulationPipelineProductAccessKind.AppendOnlyProducer);

        static SimulationPipelinePortRequirement Target(string portId, string schemaId) =>
            new SimulationPipelinePortRequirement(
                SimulationPipelineBindingPortRole.Target,
                portId,
                schemaId,
                1,
                SimulationPortDirection.Input);
    }

    public static class RollbackPipelinePassIds
    {
        public const string Ingress = "thirdperson.simulation.deterministic-rollback-input-ingress";
        public const string Schedule = "thirdperson.simulation.deterministic-rollback-schedule";
        public const string History = "thirdperson.simulation.deterministic-rollback-history";
        public const string HashEgress = "thirdperson.simulation.deterministic-rollback-hash-egress";
        public const string OutputDisposition = "thirdperson.simulation.deterministic-rollback-output-disposition";
        public const string HistoryStateOwner = "deterministic-rollback-history";
        public const string HistoryStateSchema = "deterministic-rollback-simulation-projection";
    }
}
