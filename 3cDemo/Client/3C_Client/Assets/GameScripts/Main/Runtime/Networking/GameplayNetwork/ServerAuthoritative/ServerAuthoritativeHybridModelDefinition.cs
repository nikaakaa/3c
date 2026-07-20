using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Simulation;
using ThirdPersonSimulation;
using ThirdPersonSimulation.ServerAuthoritative;
using UnityEngine;

namespace ThirdPersonGameplay.Networking.ServerAuthoritative
{
    [CreateAssetMenu(
        fileName = "ServerAuthoritativeHybridModel",
        menuName = "3C/Networking/Server Authoritative Hybrid Model")]
    public sealed class ServerAuthoritativeHybridModelDefinition : GameplayNetworkModelDefinition
    {
        [SerializeField] ServerAuthoritativeFantasyEndpointDefinition m_Endpoint;
        [SerializeField] SimulationPipelineDefinition m_PredictionPipeline;
        [SerializeField] SimulationPipelineDefinition m_AuthorityPipeline;
        [SerializeField, Min(1)] int m_SimulationTickRate;
        [SerializeField, Min(1)] int m_CommandPacketRate;
        [SerializeField, Min(1)] int m_SnapshotPacketRate;
        [SerializeField, Min(1)] int m_CommandSlackTicks;
        [SerializeField, Min(1)] int m_MaximumRemoteBodyExtrapolationTicks;
        [SerializeField, Range(256, 1200)] int m_MaxGameplayDatagramBytes;
        [SerializeField, Min(1)] int m_HistoryCapacity;
        [SerializeField, Min(0)] int m_MaximumInputLeadTicks;
        [SerializeField, Min(0)] int m_MaximumInputLagTicks;
        [SerializeField, Min(1)] int m_MaximumReplayTicksPerOuterTick;
        [SerializeField, Min(0f)] float m_BodyPositionTolerance;
        [SerializeField, Min(0f)] float m_BodyYawToleranceDegrees;
        [SerializeField] ServerAuthoritativeHardRecoveryPolicy m_HardRecoveryPolicy;
        [SerializeField] ServerAuthoritativeMissingInputPolicy m_MissingInputPolicy;
        [SerializeField] ServerAuthoritativeReliableGameplayFactKinds m_ReliableGameplayFactKinds;
        [SerializeField] List<string> m_ReliableProducerIds = new List<string>();

        public ServerAuthoritativeFantasyEndpointDefinition Endpoint => Require(m_Endpoint, "Fantasy Endpoint");
        public SimulationPipelineDefinition PredictionPipeline => Require(m_PredictionPipeline, "Prediction Pipeline");
        public SimulationPipelineDefinition AuthorityPipeline => Require(m_AuthorityPipeline, "Authority Pipeline");
        public int SimulationTickRate => Policy.SimulationTickRate;
        public ServerAuthoritativeModelPolicy Policy => new ServerAuthoritativeModelPolicy(
            m_SimulationTickRate,
            m_CommandPacketRate,
            m_SnapshotPacketRate,
            m_CommandSlackTicks,
            m_MaximumRemoteBodyExtrapolationTicks,
            m_MaxGameplayDatagramBytes,
            m_HistoryCapacity,
            m_MaximumInputLeadTicks,
            m_MaximumInputLagTicks,
            m_MaximumReplayTicksPerOuterTick,
            m_BodyPositionTolerance,
            m_BodyYawToleranceDegrees,
            m_HardRecoveryPolicy,
            m_MissingInputPolicy);
        public ServerAuthoritativeReplicationPolicy ReplicationPolicy => new ServerAuthoritativeReplicationPolicy(
            m_ReliableGameplayFactKinds,
            m_ReliableProducerIds);

        public override SimulationComponentIdentity BuildModelIdentity()
        {
            RequireComplete();
            SimulationPipelineDescriptor prediction = PredictionPipeline.BuildPortableDescriptor();
            SimulationPipelineDescriptor authority = AuthorityPipeline.BuildPortableDescriptor();
            SimulationComponentIdentity endpoint = Endpoint.BuildIdentity();
            return new SimulationComponentIdentity(
                SimulationComponentRole.Model,
                ServerAuthoritativeModelIdentity.ModelId,
                ServerAuthoritativeModelIdentity.SemanticVersion,
                StableHash.Compute(
                    "server-authoritative-hybrid-model/4",
                    endpoint.ToString(),
                    prediction.DescriptorHash.ToString(),
                    authority.DescriptorHash.ToString(),
                    Float32SimulationNumericProfile.Value.Id.Value,
                    Float32SimulationNumericProfile.Value.AbiVersion.ToString(),
                    Float32PassExecutionBackend.BackendId,
                    Convert.ToUInt64(ServerAuthoritativeSolverCompatibilityContract.PredictionRequiredCapabilities).ToString(),
                    Convert.ToUInt64(ServerAuthoritativeSolverCompatibilityContract.AuthorityRequiredCapabilities).ToString(),
                    Policy.ConfigurationHash.ToString(),
                    ReplicationPolicy.ConfigurationHash.ToString(),
                    SimulationTickRate.ToString()));
        }

        public void RequireComplete()
        {
            _ = Endpoint;
            _ = PredictionPipeline;
            _ = AuthorityPipeline;
            _ = SimulationTickRate;
            _ = Policy;
            _ = ReplicationPolicy;
            SimulationPipelineDescriptor prediction = PredictionPipeline.BuildPortableDescriptor();
            SimulationPipelineDescriptor authority = AuthorityPipeline.BuildPortableDescriptor();
            if (!string.Equals(prediction.PipelineId.Value, ServerAuthoritativePipelineIdentity.PredictionPipelineId, StringComparison.Ordinal))
                throw new InvalidOperationException($"Model '{name}' Prediction Pipeline identity is invalid.");
            if (!string.Equals(authority.PipelineId.Value, ServerAuthoritativePipelineIdentity.AuthorityPipelineId, StringComparison.Ordinal))
                throw new InvalidOperationException($"Model '{name}' Authority Pipeline identity is invalid.");
        }

        internal GameplayNetworkModelSourceRequirements BuildSourceRequirements(
            string sourceComponentId,
            string sourceVersion,
            SimulationPipelineDefinition pipeline,
            SimulationTickSourceKind outerTickKind,
            SimulationPipelineExecutionSupport executionSupport,
            IReadOnlyList<SimulationPipelinePortRequirement> sourcePorts)
        {
            RequireComplete();
            SimulationPipelineDescriptor descriptor = pipeline.BuildPortableDescriptor();
            var passes = new List<SimulationPipelinePassRequirement>();
            AddPasses(descriptor.GetPhase(SimulationPipelinePhase.Ingress), passes);
            AddPasses(descriptor.GetPhase(SimulationPipelinePhase.Schedule), passes);
            AddPasses(descriptor.GetPhase(SimulationPipelinePhase.Step), passes);
            AddPasses(descriptor.GetPhase(SimulationPipelinePhase.Egress), passes);
            return new GameplayNetworkModelSourceRequirements(
                BuildModelIdentity(),
                ServerAuthoritativeModelIdentity.CreateProtocol(
                    StableHash.Compute("server-authoritative-fantasy-outer-protocol/1")),
                Endpoint.BuildIdentity(),
                sourceComponentId,
                sourceVersion,
                Float32SimulationNumericProfile.Value.Id,
                Float32SimulationNumericProfile.Value.AbiVersion,
                outerTickKind,
                executionSupport,
                false,
                Float32PassExecutionBackend.BackendId,
                descriptor.PipelineId,
                outerTickKind == SimulationTickSourceKind.Authoritative
                    ? ServerAuthoritativeSolverCompatibilityContract.AuthorityRequiredCapabilities
                    : ServerAuthoritativeSolverCompatibilityContract.PredictionRequiredCapabilities,
                passes,
                sourcePorts);
        }

        internal GameplayNetworkModelSourceRequirements BuildPredictionSourceRequirements()
        {
            return BuildSourceRequirements(
                ServerAuthoritativePredictionSessionSourceDefinition.ComponentId,
                ServerAuthoritativePredictionSessionSourceDefinition.SemanticVersion,
                PredictionPipeline,
                SimulationTickSourceKind.LocalLogic,
                SimulationPipelineExecutionSupport.Forward |
                SimulationPipelineExecutionSupport.Replay |
                SimulationPipelineExecutionSupport.Restore,
                new[]
                {
                    Float32LocalInputSourcePortContract.Requirement,
                    ServerAuthoritativeSourcePortContracts.Observation,
                    ServerAuthoritativeSourcePortContracts.PredictionRestore,
                    ServerAuthoritativeSourcePortContracts.PredictionState,
                    ServerAuthoritativeSourcePortContracts.PredictionSend
                });
        }

        internal GameplayNetworkModelSourceRequirements BuildAuthoritySourceRequirements()
        {
            return BuildSourceRequirements(
                ServerAuthoritativeAuthoritySessionSourceDefinition.ComponentId,
                ServerAuthoritativeAuthoritySessionSourceDefinition.SemanticVersion,
                AuthorityPipeline,
                SimulationTickSourceKind.Authoritative,
                SimulationPipelineExecutionSupport.Forward |
                SimulationPipelineExecutionSupport.Authoritative,
                new[]
                {
                    ServerAuthoritativeSourcePortContracts.AcceptedInput,
                    ServerAuthoritativeSourcePortContracts.AuthorityClock,
                    ServerAuthoritativeSourcePortContracts.FullBaselineRequest,
                    ServerAuthoritativeSourcePortContracts.AuthoritySend
                });
        }

        internal ServerAuthoritativePipelineCompatibilityIdentity BuildCompatibility(
            IFloat32SimulationActorRegistration registration,
            SimulationProgramRuntimeDescriptor programRuntime,
            SimulationExecutionBackendDefinition executionBackend)
        {
            if (registration == null)
                throw new ArgumentNullException(nameof(registration));
            return BuildCompatibility(registration.Program, programRuntime, executionBackend);
        }

        public ServerAuthoritativePipelineCompatibilityIdentity BuildCompatibility(
            CharacterSimulationProgram program,
            SimulationProgramRuntimeDescriptor programRuntime,
            SimulationExecutionBackendDefinition executionBackend)
        {
            if (program == null)
                throw new ArgumentNullException(nameof(program));
            if (programRuntime == null)
                throw new ArgumentNullException(nameof(programRuntime));
            if (!executionBackend)
                throw new ArgumentNullException(nameof(executionBackend));
            SimulationNumericProfile requiredProfile = Float32SimulationNumericProfile.Value;
            if (!programRuntime.NumericProfileId.Equals(requiredProfile.Id) ||
                !programRuntime.TargetAbiVersion.Equals(requiredProfile.AbiVersion))
            {
                throw new InvalidOperationException(
                    $"ServerAuthoritative Program Runtime '{programRuntime.Identity}' does not satisfy Float32 ABI '{requiredProfile.Id}/{requiredProfile.AbiVersion}'.");
            }
            SimulationExecutionBackendDescriptor backend = executionBackend.BuildPortableDescriptor();
            if (!string.Equals(backend.BackendId, Float32PassExecutionBackend.BackendId, StringComparison.Ordinal))
                throw new InvalidOperationException($"ServerAuthoritative Backend '{backend.Identity}' is not the formal Float32 Pass Backend.");
            ReplicationPolicy.RequireProgramCoverage(program);
            SimulationPipelineIdentity prediction = CompilePipelineIdentity(
                program,
                PredictionPipeline,
                BuildPredictionSourceRequirements(),
                programRuntime,
                executionBackend,
                backend);
            SimulationPipelineIdentity authority = CompilePipelineIdentity(
                program,
                AuthorityPipeline,
                BuildAuthoritySourceRequirements(),
                programRuntime,
                executionBackend,
                backend);
            return new ServerAuthoritativePipelineCompatibilityIdentity(
                program.Manifest.ProgramId,
                program.ProgramHash,
                program.LayoutHash,
                program.Manifest.OperationSetVersion,
                SimulationTickRate,
                prediction,
                authority,
                backend.Identity,
                ServerAuthoritativeSolverCompatibilityContract.PredictionRequiredCapabilities,
                ServerAuthoritativeSolverCompatibilityContract.AuthorityRequiredCapabilities);
        }

        SimulationPipelineIdentity CompilePipelineIdentity(
            CharacterSimulationProgram program,
            SimulationPipelineDefinition pipelineDefinition,
            GameplayNetworkModelSourceRequirements requirements,
            SimulationProgramRuntimeDescriptor programRuntime,
            SimulationExecutionBackendDefinition executionBackend,
            SimulationExecutionBackendDescriptor backend)
        {
            SimulationPipelineDescriptor pipeline = pipelineDefinition.BuildPortableDescriptor();
            SimulationSessionSourceDescriptor source = BuildSourceDescriptor(requirements);
            SimulationWorldSolverDefinitionDescriptor compatibilitySolver = BuildCompatibilitySolver(
                program,
                pipeline,
                requirements,
                programRuntime);
            SimulationPipelinePassFactoryCatalog factories =
                executionBackend.BuildPortableFactoryCatalog(pipelineDefinition);
            var snapshotCodec = new SimulationComponentIdentity(
                SimulationComponentRole.SnapshotCodec,
                "thirdperson.simulation.snapshot-codec.server-authoritative-preflight",
                "1",
                StableHash.Compute(
                    "server-authoritative-preflight-snapshot-codec/1",
                    programRuntime.Identity.ToString(),
                    backend.Identity.ToString()));
            SimulationPipelineCompilationResult compilation = SimulationPipelineCompiler.Compile(
                pipeline,
                factories,
                programRuntime,
                program.Manifest.Capabilities.RequiredWorldCapabilities,
                backend,
                source,
                BuildSourcePorts(requirements, source.Identity),
                compatibilitySolver,
                snapshotCodec,
                source.ExecutionSupport,
                false);
            if (!compilation.IsValid)
            {
                var errors = new string[compilation.Errors.Count];
                for (int i = 0; i < errors.Length; i++)
                    errors[i] = compilation.Errors[i].ToString();
                throw new InvalidOperationException(
                    $"ServerAuthoritative Pipeline '{pipeline.PipelineId}' preflight compilation failed: {string.Join(" | ", errors)}");
            }
            return compilation.Plan.Identity;
        }

        static SimulationWorldSolverDefinitionDescriptor BuildCompatibilitySolver(
            CharacterSimulationProgram program,
            SimulationPipelineDescriptor pipeline,
            GameplayNetworkModelSourceRequirements requirements,
            SimulationProgramRuntimeDescriptor programRuntime)
        {
            WorldCapability capabilities =
                program.Manifest.Capabilities.RequiredWorldCapabilities |
                requirements.RequiredSolverCapabilities;
            for (int i = 0; i < pipeline.Passes.Count; i++)
                capabilities |= pipeline.Passes[i].RequiredSolverCapabilities;
            string role = requirements.OuterTickKind == SimulationTickSourceKind.Authoritative
                ? "authority"
                : "prediction";
            var identity = new SimulationComponentIdentity(
                SimulationComponentRole.WorldSolver,
                $"thirdperson.server-authoritative.compatibility-solver.{role}",
                "1",
                StableHash.Compute(
                    "server-authoritative-compatibility-solver/1",
                    role,
                    ((ulong)capabilities).ToString(),
                    ((int)requirements.ExecutionSupport).ToString()));
            return new SimulationWorldSolverDefinitionDescriptor(
                identity,
                programRuntime.NumericProfileId,
                programRuntime.TargetAbiVersion,
                new SolverImplementationId(identity.ComponentId),
                identity.SemanticVersion,
                capabilities,
                WorldFeature.None,
                requirements.ExecutionSupport,
                false);
        }

        static SimulationSessionSourceDescriptor BuildSourceDescriptor(
            GameplayNetworkModelSourceRequirements requirements)
        {
            var identity = new SimulationComponentIdentity(
                SimulationComponentRole.SessionSource,
                requirements.SourceComponentId,
                requirements.SourceSemanticVersion,
                requirements.RequirementsHash);
            return new SimulationSessionSourceDescriptor(
                identity,
                requirements.NumericProfileId,
                requirements.TargetAbiVersion,
                requirements.OuterTickKind,
                requirements.ExecutionSupport,
                requirements.Deterministic,
                requirements.RequiredBackendId,
                requirements.RequiredPipelineId,
                requirements.Model,
                requirements.Endpoint,
                requirements.Protocol,
                requirements.RequiredSolverCapabilities,
                requirements.RequiredPasses,
                requirements.RequiredSourcePorts);
        }

        static IReadOnlyList<SimulationPortDescriptor> BuildSourcePorts(
            GameplayNetworkModelSourceRequirements requirements,
            SimulationComponentIdentity source)
        {
            var ports = new SimulationPortDescriptor[requirements.RequiredSourcePorts.Count];
            for (int i = 0; i < ports.Length; i++)
            {
                SimulationPipelinePortRequirement required = requirements.RequiredSourcePorts[i];
                ports[i] = SimulationPortDescriptor.CreateSource(required, source);
            }
            return ports;
        }

        static void AddPasses(
            IReadOnlyList<SimulationPipelinePassDescriptor> source,
            ICollection<SimulationPipelinePassRequirement> destination)
        {
            for (int i = 0; i < source.Count; i++)
                destination.Add(new SimulationPipelinePassRequirement(source[i].PassId, source[i].ImplementationVersion, source[i].Phase));
        }

        T Require<T>(T value, string field) where T : UnityEngine.Object
        {
            return value ? value : throw Missing(field);
        }

        InvalidOperationException Missing(string field) =>
            new InvalidOperationException($"ServerAuthoritative Model '{name}' requires explicit {field}.");

#if UNITY_EDITOR
        public void SetAuthoring(
            ServerAuthoritativeFantasyEndpointDefinition endpoint,
            SimulationPipelineDefinition predictionPipeline,
            SimulationPipelineDefinition authorityPipeline,
            int simulationTickRate,
            int commandPacketRate,
            int snapshotPacketRate,
            int commandSlackTicks,
            int maximumRemoteBodyExtrapolationTicks,
            int maxGameplayDatagramBytes,
            int historyCapacity,
            int maximumInputLeadTicks,
            int maximumInputLagTicks,
            int maximumReplayTicksPerOuterTick,
            float bodyPositionTolerance,
            float bodyYawToleranceDegrees,
            ServerAuthoritativeHardRecoveryPolicy hardRecoveryPolicy,
            ServerAuthoritativeMissingInputPolicy missingInputPolicy,
            ServerAuthoritativeReliableGameplayFactKinds reliableGameplayFactKinds,
            IEnumerable<string> reliableProducerIds)
        {
            m_Endpoint = endpoint ? endpoint : throw new ArgumentNullException(nameof(endpoint));
            m_PredictionPipeline = predictionPipeline ? predictionPipeline : throw new ArgumentNullException(nameof(predictionPipeline));
            m_AuthorityPipeline = authorityPipeline ? authorityPipeline : throw new ArgumentNullException(nameof(authorityPipeline));
            m_SimulationTickRate = simulationTickRate;
            m_CommandPacketRate = commandPacketRate;
            m_SnapshotPacketRate = snapshotPacketRate;
            m_CommandSlackTicks = commandSlackTicks;
            m_MaximumRemoteBodyExtrapolationTicks = maximumRemoteBodyExtrapolationTicks;
            m_MaxGameplayDatagramBytes = maxGameplayDatagramBytes;
            m_HistoryCapacity = historyCapacity;
            m_MaximumInputLeadTicks = maximumInputLeadTicks;
            m_MaximumInputLagTicks = maximumInputLagTicks;
            m_MaximumReplayTicksPerOuterTick = maximumReplayTicksPerOuterTick;
            m_BodyPositionTolerance = bodyPositionTolerance;
            m_BodyYawToleranceDegrees = bodyYawToleranceDegrees;
            m_HardRecoveryPolicy = hardRecoveryPolicy;
            m_MissingInputPolicy = missingInputPolicy;
            m_ReliableGameplayFactKinds = reliableGameplayFactKinds;
            m_ReliableProducerIds = reliableProducerIds == null
                ? new List<string>()
                : new List<string>(reliableProducerIds);
            RequireComplete();
        }
#endif
    }

}
