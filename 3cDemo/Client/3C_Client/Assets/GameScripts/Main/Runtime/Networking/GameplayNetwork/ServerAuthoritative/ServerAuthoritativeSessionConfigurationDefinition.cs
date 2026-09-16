using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Simulation;
using ThirdPersonSimulation;
using ThirdPersonSimulation.ServerAuthoritative;
using UnityEngine;

namespace ThirdPersonGameplay.Networking.ServerAuthoritative
{
    [CreateAssetMenu(
        fileName = "ServerAuthoritativeSessionConfiguration",
        menuName = "3C/Networking/Server Authoritative Session Configuration")]
    public sealed class ServerAuthoritativeSessionConfigurationDefinition : GameplayNetworkModelDefinition
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
            ServerAuthoritativeModelPolicy policy = Policy;
            ServerAuthoritativeReplicationPolicy replication = ReplicationPolicy;
            return new SimulationComponentIdentity(
                SimulationComponentRole.Model,
                ServerAuthoritativeModelIdentity.ModelId,
                ServerAuthoritativeModelIdentity.SemanticVersion,
                StableHash.Compute(
                    "server-authoritative-session-configuration/1",
                    Endpoint.BuildIdentity().ToString(),
                    prediction.DescriptorHash.ToString(),
                    authority.DescriptorHash.ToString(),
                    Float32SimulationNumericProfile.Value.Id.Value,
                    Float32SimulationNumericProfile.Value.AbiVersion.ToString(),
                    Float32PassExecutionBackend.BackendId,
                    Convert.ToUInt64(ServerAuthoritativeSolverCompatibilityContract.PredictionRequiredCapabilities).ToString(),
                    Convert.ToUInt64(ServerAuthoritativeSolverCompatibilityContract.AuthorityRequiredCapabilities).ToString(),
                    policy.ConfigurationHash.ToString(),
                    replication.ConfigurationHash.ToString(),
                    SimulationTickRate.ToString()));
        }

        public ServerAuthoritativePipelineCompatibilityIdentity BuildCompatibility(
            Float32CharacterRuntime characterRuntime,
            SimulationExecutionBackendDefinition executionBackend)
        {
            if (characterRuntime == null)
                throw new ArgumentNullException(nameof(characterRuntime));
            if (!executionBackend)
                throw new ArgumentNullException(nameof(executionBackend));
            RequireComplete();
            if (characterRuntime.TickRate != SimulationTickRate)
                throw new InvalidOperationException($"Session Configuration '{name}' TickRate does not match the Character Runtime.");
            SimulationExecutionBackendDescriptor backend = executionBackend.BuildPortableDescriptor();
            if (!backend.Identity.Equals(Float32PassExecutionBackend.Descriptor.Identity))
                throw new InvalidOperationException($"Session Configuration '{name}' requires the formal Float32 Pass Backend.");
            ReplicationPolicy.RequireCharacterCoverage(characterRuntime);
            SimulationPipelineIdentity prediction = Identity(PredictionPipeline.BuildPortableDescriptor());
            SimulationPipelineIdentity authority = Identity(AuthorityPipeline.BuildPortableDescriptor());
            return new ServerAuthoritativePipelineCompatibilityIdentity(
                characterRuntime.GameplayContentHash,
                characterRuntime.StateSchemaHash,
                characterRuntime.OperationSetVersion,
                SimulationTickRate,
                prediction,
                authority,
                backend.Identity,
                ServerAuthoritativeSolverCompatibilityContract.PredictionRequiredCapabilities,
                ServerAuthoritativeSolverCompatibilityContract.AuthorityRequiredCapabilities);
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
                    StableHash.Compute("server-authoritative-fantasy-outer-protocol/2")),
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
                CharacterControlSourceCapability.CommittedObservation,
                passes,
                sourcePorts);
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
                throw new InvalidOperationException($"Session Configuration '{name}' Prediction Pipeline identity is invalid.");
            if (!string.Equals(authority.PipelineId.Value, ServerAuthoritativePipelineIdentity.AuthorityPipelineId, StringComparison.Ordinal))
                throw new InvalidOperationException($"Session Configuration '{name}' Authority Pipeline identity is invalid.");
        }

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

        static SimulationPipelineIdentity Identity(SimulationPipelineDescriptor descriptor) =>
            new SimulationPipelineIdentity(
                descriptor.PipelineId,
                descriptor.Revision,
                descriptor.SchemaVersion,
                new SimulationPipelineHash(descriptor.DescriptorHash));

        static void AddPasses(
            IReadOnlyList<SimulationPipelinePassDescriptor> source,
            ICollection<SimulationPipelinePassRequirement> destination)
        {
            for (int i = 0; i < source.Count; i++)
                destination.Add(new SimulationPipelinePassRequirement(
                    source[i].PassId,
                    source[i].ImplementationVersion,
                    source[i].Phase));
        }

        T Require<T>(T value, string field) where T : UnityEngine.Object =>
            value ? value : throw new InvalidOperationException($"Session Configuration '{name}' requires explicit {field}.");
    }
}
