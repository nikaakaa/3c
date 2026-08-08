using System;
using ThirdPersonSimulation.Fixed;

namespace ThirdPersonSimulation.DeterministicRollback
{
    public enum RollbackMissingInputPolicy : byte
    {
        ContinuousValuesWithEmptyRequests = 1,
        NeutralValuesWithEmptyRequests = 2
    }

    public enum RollbackSnapshotAuthority : byte
    {
        LowestPeerId = 1
    }

    public sealed class DeterministicRollbackModelPolicy
    {
        public DeterministicRollbackModelPolicy(
            int offensiveRequestDelayTicks,
            int historyLengthTicks,
            int hashCadenceTicks,
            int maximumRollbackDepthTicks,
            int maximumPredictionLeadTicks,
            int confirmationDelayTicks,
            int maximumQueuedBundles,
            int maximumQueuedSnapshots,
            int maximumOutputRecords,
            RollbackMissingInputPolicy missingInputPolicy,
            RollbackSnapshotAuthority snapshotAuthority)
        {
            if (offensiveRequestDelayTicks < 0 || historyLengthTicks <= 0 || hashCadenceTicks <= 0 ||
                maximumRollbackDepthTicks <= 0 || maximumRollbackDepthTicks >= historyLengthTicks ||
                maximumPredictionLeadTicks <= 0 || maximumPredictionLeadTicks >= historyLengthTicks ||
                confirmationDelayTicks < 0 || confirmationDelayTicks >= historyLengthTicks ||
                maximumQueuedBundles <= 0 || maximumQueuedSnapshots <= 0 || maximumOutputRecords <= 0 ||
                maximumQueuedSnapshots <= maximumRollbackDepthTicks ||
                !Enum.IsDefined(typeof(RollbackMissingInputPolicy), missingInputPolicy) ||
                !Enum.IsDefined(typeof(RollbackSnapshotAuthority), snapshotAuthority))
            {
                throw new ArgumentException("Deterministic Rollback policy is invalid.");
            }
            OffensiveRequestDelayTicks = offensiveRequestDelayTicks;
            HistoryLengthTicks = historyLengthTicks;
            HashCadenceTicks = hashCadenceTicks;
            MaximumRollbackDepthTicks = maximumRollbackDepthTicks;
            MaximumPredictionLeadTicks = maximumPredictionLeadTicks;
            ConfirmationDelayTicks = confirmationDelayTicks;
            MaximumQueuedBundles = maximumQueuedBundles;
            MaximumQueuedSnapshots = maximumQueuedSnapshots;
            MaximumOutputRecords = maximumOutputRecords;
            MissingInputPolicy = missingInputPolicy;
            SnapshotAuthority = snapshotAuthority;
            ConfigurationHash = StableHash.Compute(
                "deterministic-rollback-policy/3",
                offensiveRequestDelayTicks.ToString(),
                historyLengthTicks.ToString(),
                hashCadenceTicks.ToString(),
                maximumRollbackDepthTicks.ToString(),
                maximumPredictionLeadTicks.ToString(),
                confirmationDelayTicks.ToString(),
                maximumQueuedBundles.ToString(),
                maximumQueuedSnapshots.ToString(),
                maximumOutputRecords.ToString(),
                ((int)missingInputPolicy).ToString(),
                ((int)snapshotAuthority).ToString());
        }

        public int OffensiveRequestDelayTicks { get; }
        public int HistoryLengthTicks { get; }
        public int HashCadenceTicks { get; }
        public int MaximumRollbackDepthTicks { get; }
        public int MaximumPredictionLeadTicks { get; }
        public int ConfirmationDelayTicks { get; }
        public int MaximumQueuedBundles { get; }
        public int MaximumQueuedSnapshots { get; }
        public int MaximumOutputRecords { get; }
        public RollbackMissingInputPolicy MissingInputPolicy { get; }
        public RollbackSnapshotAuthority SnapshotAuthority { get; }
        public StableHash ConfigurationHash { get; }
    }

    public static class DeterministicRollbackModelIdentity
    {
        public const string ModelId = "thirdperson.network-model.deterministic-rollback";
        public const string SemanticVersion = "6";
        public const string PipelineId = "thirdperson.simulation.pipeline.deterministic-rollback";
        public const string PipelineRevision = "6";
        public const string BackendId = "thirdperson.simulation.backend.fixed-pass";
        public const string EndpointId = "thirdperson.network-endpoint.deterministic-rollback";
        public const string EndpointVersion = "4";
        public const string ProtocolId = "thirdperson.rollback-input-protocol";
        public const int ProtocolVersion = 5;

        public static SimulationComponentIdentity BuildModel(
            DeterministicRollbackModelPolicy policy,
            SemanticHash semanticHash,
            ProgramHash fixedProgramHash,
            LayoutHash fixedLayoutHash,
            StableHash collisionWorldHash,
            StableHash kccIdentityHash)
        {
            if (policy == null || !semanticHash.IsValid || !fixedProgramHash.IsValid || !fixedLayoutHash.IsValid ||
                !collisionWorldHash.IsValid || !kccIdentityHash.IsValid)
            {
                throw new ArgumentException("Deterministic Rollback Model identity is incomplete.");
            }
            return new SimulationComponentIdentity(
                SimulationComponentRole.Model,
                ModelId,
                SemanticVersion,
                StableHash.Compute(
                    "deterministic-rollback-model/4",
                    policy.ConfigurationHash.Value,
                    semanticHash.ToString(),
                    fixedProgramHash.ToString(),
                    fixedLayoutHash.ToString(),
                    collisionWorldHash.Value,
                    kccIdentityHash.Value,
                    FixedSimulationNumericProfile.Value.Id.Value,
                    FixedSimulationNumericProfile.Value.AbiVersion.Value.ToString(),
                    PipelineId,
                    PipelineRevision,
                    ProtocolId,
                    ProtocolVersion.ToString()));
        }

        public static SimulationProtocolIdentity Protocol => new SimulationProtocolIdentity(
            ProtocolId,
            ProtocolVersion.ToString(),
            StableHash.Compute(
                "deterministic-rollback-protocol-schema/3",
                "handshake",
                "roster",
                "actor-input-batch",
                "relayed-explicit-input-batch",
                "canonical-bundle",
                "canonical-confirmation",
                "state-hash-with-peer-world-body-v2",
                "snapshot-request-with-routing",
                "snapshot-response-with-routing-session-snapshot-v3"));
    }
}
