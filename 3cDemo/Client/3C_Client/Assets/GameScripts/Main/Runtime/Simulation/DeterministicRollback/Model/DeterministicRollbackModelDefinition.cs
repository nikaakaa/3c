using System;
using ThirdPersonSimulation.Fixed;

namespace ThirdPersonSimulation.DeterministicRollback
{
    public sealed class DeterministicRollbackModelDefinition
    {
        public DeterministicRollbackModelDefinition(
            DeterministicRollbackModelPolicy policy,
            SemanticHash semanticHash,
            ProgramHash fixedProgramHash,
            LayoutHash fixedLayoutHash,
            int tickRate,
            StableHash collisionWorldHash,
            StableHash kccIdentityHash,
            StableHash endpointConfigurationHash)
        {
            Policy = policy ?? throw new ArgumentNullException(nameof(policy));
            if (!semanticHash.IsValid || !fixedProgramHash.IsValid || !fixedLayoutHash.IsValid || tickRate <= 0 ||
                !collisionWorldHash.IsValid || !kccIdentityHash.IsValid || !endpointConfigurationHash.IsValid)
            {
                throw new ArgumentException("Deterministic Rollback ModelDefinition binding is incomplete.");
            }
            SemanticHash = semanticHash;
            FixedProgramHash = fixedProgramHash;
            FixedLayoutHash = fixedLayoutHash;
            TickRate = tickRate;
            CollisionWorldHash = collisionWorldHash;
            KccIdentityHash = kccIdentityHash;
            ModelIdentity = DeterministicRollbackModelIdentity.BuildModel(
                policy,
                semanticHash,
                fixedProgramHash,
                fixedLayoutHash,
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
            Handshake = new RollbackHandshake(
                "definition",
                ModelIdentity,
                semanticHash,
                fixedProgramHash,
                fixedLayoutHash,
                tickRate,
                collisionWorldHash,
                kccIdentityHash,
                DeterministicRollbackModelIdentity.Protocol);
        }

        public DeterministicRollbackModelPolicy Policy { get; }
        public SemanticHash SemanticHash { get; }
        public ProgramHash FixedProgramHash { get; }
        public LayoutHash FixedLayoutHash { get; }
        public int TickRate { get; }
        public StableHash CollisionWorldHash { get; }
        public StableHash KccIdentityHash { get; }
        public SimulationComponentIdentity ModelIdentity { get; }
        public SimulationComponentIdentity EndpointIdentity { get; }
        public SimulationComponentIdentity SourceIdentity { get; }
        public SimulationSessionSourceDescriptor SourceDescriptor { get; }
        public RollbackHandshake Handshake { get; }

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
