using System;
using ThirdPersonSimulation.Fixed;

namespace ThirdPersonSimulation.DeterministicRollback
{
    public sealed class DeterministicRollbackModelDefinition
    {
        public DeterministicRollbackModelDefinition(
            DeterministicRollbackModelPolicy policy,
            int tickRate,
            StableHash collisionWorldHash,
            StableHash kccIdentityHash,
            StableHash endpointConfigurationHash)
        {
            Policy = policy ?? throw new ArgumentNullException(nameof(policy));
            if (tickRate <= 0 ||
                !collisionWorldHash.IsValid || !kccIdentityHash.IsValid || !endpointConfigurationHash.IsValid)
            {
                throw new ArgumentException("Deterministic Rollback ModelDefinition binding is incomplete.");
            }
            TickRate = tickRate;
            CollisionWorldHash = collisionWorldHash;
            KccIdentityHash = kccIdentityHash;
            ModelIdentity = DeterministicRollbackModelIdentity.BuildModel(
                policy,
                collisionWorldHash,
                kccIdentityHash);
            EndpointIdentity = new SimulationComponentIdentity(
                SimulationComponentRole.Endpoint,
                DeterministicRollbackModelIdentity.EndpointId,
                DeterministicRollbackModelIdentity.EndpointVersion,
                endpointConfigurationHash);
            SourceIdentity = new SimulationComponentIdentity(
                SimulationComponentRole.SessionSource,
                "thirdperson.simulation.session-source.deterministic-rollback",
                "2",
                StableHash.Compute(
                    "deterministic-rollback-session-source/2",
                    ModelIdentity.ToString(),
                    EndpointIdentity.ToString(),
                    DeterministicRollbackModelIdentity.Protocol.ToString(),
                    tickRate.ToString()));
            SourceDescriptor = BuildSourceDescriptor();
        }

        public DeterministicRollbackModelPolicy Policy { get; }
        public int TickRate { get; }
        public StableHash CollisionWorldHash { get; }
        public StableHash KccIdentityHash { get; }
        public SimulationComponentIdentity ModelIdentity { get; }
        public SimulationComponentIdentity EndpointIdentity { get; }
        public SimulationComponentIdentity SourceIdentity { get; }
        public SimulationSessionSourceDescriptor SourceDescriptor { get; }

        SimulationSessionSourceDescriptor BuildSourceDescriptor()
        {
            SimulationPipelineExecutionSupport support =
                SimulationPipelineExecutionSupport.Forward |
                SimulationPipelineExecutionSupport.Replay |
                SimulationPipelineExecutionSupport.Restore;
            return new SimulationSessionSourceDescriptor(
                SourceIdentity,
                FixedSimulationNumericProfile.Value.Id,
                FixedSimulationNumericProfile.Value.AbiVersion,
                SimulationTickSourceKind.LocalLogic,
                support,
                true,
                FixedPassExecutionBackend.BackendId,
                new SimulationPipelineId(DeterministicRollbackModelIdentity.PipelineId),
                ModelIdentity,
                EndpointIdentity,
                DeterministicRollbackModelIdentity.Protocol,
                DeterministicRollbackRuntimeLauncher.RequiredWorldCapabilities,
                new[]
                {
                    Requirement(RollbackPipelinePassIds.Ingress, SimulationPipelinePhase.Ingress),
                    Requirement(RollbackPipelinePassIds.Schedule, SimulationPipelinePhase.Schedule),
                    Requirement(RollbackPipelinePassIds.History, SimulationPipelinePhase.Step),
                    Requirement(RollbackPipelinePassIds.HashEgress, SimulationPipelinePhase.Egress),
                    Requirement(RollbackPipelinePassIds.OutputDisposition, SimulationPipelinePhase.Egress)
                },
                new[] { RollbackSourcePortContracts.InputRequirement });
        }

        static SimulationPipelinePassRequirement Requirement(string id, SimulationPipelinePhase phase)
        {
            return new SimulationPipelinePassRequirement(
                new SimulationPipelinePassId(id),
                new SimulationPipelinePassImplementationVersion("3"),
                phase);
        }
    }
}
