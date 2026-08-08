using System;
using System.Collections.Generic;
using ThirdPersonSimulation.Fixed;

namespace ThirdPersonSimulation.DeterministicRollback
{
    [Serializable]
    public sealed class DeterministicRollbackServerManifest
    {
        public const int CurrentSchemaVersion = 3;

        public int schemaVersion;
        public string buildId = string.Empty;
        public string productId = string.Empty;
        public string manifestHash = string.Empty;
        public string sessionId = string.Empty;
        public string listenAddress = string.Empty;
        public int listenPort;
        public string relayServerPeerId = string.Empty;
        public DeterministicRollbackServerPeerManifest[] peers = Array.Empty<DeterministicRollbackServerPeerManifest>();
        public string modelId = string.Empty;
        public string modelVersion = string.Empty;
        public string modelConfigurationHash = string.Empty;
        public string protocolId = string.Empty;
        public string protocolVersion = string.Empty;
        public string protocolSchemaHash = string.Empty;
        public int tickRate;
        public string programId = string.Empty;
        public string sourceRevision = string.Empty;
        public string projectionRevision = string.Empty;
        public string semanticHash = string.Empty;
        public string fixedProgramHash = string.Empty;
        public string fixedLayoutHash = string.Empty;
        public string collisionWorldHash = string.Empty;
        public string kccIdentityHash = string.Empty;
        public int offensiveRequestDelayTicks;
        public int confirmationDelayTicks;
        public int historyLengthTicks;
        public int hashCadenceTicks;
        public int maximumRollbackDepthTicks;
        public int maximumPredictionLeadTicks;
        public int maximumQueuedBundles;
        public int maximumQueuedSnapshots;
        public int maximumOutputRecords;
        public string missingInputPolicy = string.Empty;
        public string snapshotAuthority = string.Empty;
        public int maximumDatagramBytes;
        public int maximumQueuedMessages;
        public int maximumFragmentsPerMessage;
        public int reliableResendMilliseconds;
        public int inputRedundancyCount;

        public StableHash ValidateAndComputeHash()
        {
            if (schemaVersion != CurrentSchemaVersion || string.IsNullOrWhiteSpace(buildId) ||
                string.IsNullOrWhiteSpace(productId) || string.IsNullOrWhiteSpace(sessionId) ||
                string.IsNullOrWhiteSpace(relayServerPeerId) || tickRate <= 0 ||
                string.IsNullOrWhiteSpace(programId) ||
                string.IsNullOrWhiteSpace(sourceRevision) ||
                string.IsNullOrWhiteSpace(projectionRevision) ||
                !string.Equals(modelId, DeterministicRollbackModelIdentity.ModelId, StringComparison.Ordinal) ||
                !string.Equals(modelVersion, DeterministicRollbackModelIdentity.SemanticVersion, StringComparison.Ordinal) ||
                !string.Equals(protocolId, DeterministicRollbackModelIdentity.ProtocolId, StringComparison.Ordinal) ||
                !string.Equals(protocolVersion, DeterministicRollbackModelIdentity.ProtocolVersion.ToString(), StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Deterministic Rollback Server manifest identity is invalid.");
            }

            RollbackEndpointDefinition endpoint = BuildEndpointDefinition();
            DeterministicRollbackModelPolicy policy = BuildPolicy();
            RollbackRoster roster = BuildRoster();
            RollbackHandshake handshake = BuildHandshake();
            SimulationComponentIdentity expectedModel = DeterministicRollbackModelIdentity.BuildModel(
                policy,
                handshake.SemanticHash,
                handshake.FixedProgramHash,
                handshake.FixedLayoutHash,
                handshake.CollisionWorldHash,
                handshake.KccIdentityHash);
            if (!string.Equals(endpoint.SessionId, sessionId, StringComparison.Ordinal) ||
                roster.Entries.Count == 0 || policy.HistoryLengthTicks <= 0 ||
                !handshake.Model.Equals(expectedModel) ||
                !handshake.Protocol.Equals(DeterministicRollbackModelIdentity.Protocol))
            {
                throw new InvalidOperationException("Deterministic Rollback Server manifest configuration is inconsistent.");
            }

            var values = new List<string>
            {
                "deterministic-rollback-server-manifest/3",
                schemaVersion.ToString(), buildId, productId, sessionId, listenAddress, listenPort.ToString(),
                relayServerPeerId, modelId, modelVersion, modelConfigurationHash, protocolId, protocolVersion,
                protocolSchemaHash, tickRate.ToString(), programId, sourceRevision, projectionRevision,
                semanticHash, fixedProgramHash, fixedLayoutHash,
                collisionWorldHash, kccIdentityHash, offensiveRequestDelayTicks.ToString(),
                confirmationDelayTicks.ToString(), historyLengthTicks.ToString(), hashCadenceTicks.ToString(),
                maximumRollbackDepthTicks.ToString(), maximumQueuedBundles.ToString(),
                maximumPredictionLeadTicks.ToString(),
                maximumQueuedSnapshots.ToString(), maximumOutputRecords.ToString(), missingInputPolicy,
                snapshotAuthority, maximumDatagramBytes.ToString(), maximumQueuedMessages.ToString(),
                maximumFragmentsPerMessage.ToString(), reliableResendMilliseconds.ToString(),
                inputRedundancyCount.ToString()
            };
            for (int i = 0; i < peers.Length; i++)
                values.Add(peers[i].CanonicalIdentity());
            return StableHash.Compute(values.ToArray());
        }

        public void RequireValidHash()
        {
            StableHash computed = ValidateAndComputeHash();
            if (!string.Equals(manifestHash, computed.Value, StringComparison.Ordinal))
                throw new InvalidOperationException("Deterministic Rollback Server manifest hash is stale or invalid.");
        }

        public RollbackEndpointDefinition BuildEndpointDefinition() => new RollbackEndpointDefinition(
            listenAddress,
            listenPort,
            sessionId,
            maximumDatagramBytes,
            maximumQueuedMessages,
            maximumFragmentsPerMessage,
            reliableResendMilliseconds);

        public DeterministicRollbackModelPolicy BuildPolicy() => new DeterministicRollbackModelPolicy(
            offensiveRequestDelayTicks,
            historyLengthTicks,
            hashCadenceTicks,
            maximumRollbackDepthTicks,
            maximumPredictionLeadTicks,
            confirmationDelayTicks,
            maximumQueuedBundles,
            maximumQueuedSnapshots,
            maximumOutputRecords,
            RequireEnum<RollbackMissingInputPolicy>(missingInputPolicy),
            RequireEnum<RollbackSnapshotAuthority>(snapshotAuthority));

        public RollbackRoster BuildRoster()
        {
            DeterministicRollbackServerPeerManifest[] source = peers ?? Array.Empty<DeterministicRollbackServerPeerManifest>();
            if (source.Length == 0)
                throw new InvalidOperationException("Deterministic Rollback Server manifest requires a Peer roster.");
            var entries = new RollbackRosterEntry[source.Length];
            string previous = null;
            for (int i = 0; i < source.Length; i++)
            {
                DeterministicRollbackServerPeerManifest peer = source[i] ??
                    throw new InvalidOperationException("Deterministic Rollback Server manifest contains a missing Peer.");
                if (previous != null && string.CompareOrdinal(previous, peer.peerId) >= 0)
                    throw new InvalidOperationException("Deterministic Rollback Server Peer roster must use stable PeerId order.");
                entries[i] = new RollbackRosterEntry(peer.peerId, peer.playerId, new ActorId(peer.actorId));
                previous = peer.peerId;
            }
            return new RollbackRoster(1, entries);
        }

        public RollbackHandshake BuildHandshake() => new RollbackHandshake(
            relayServerPeerId,
            new SimulationComponentIdentity(
                SimulationComponentRole.Model,
                modelId,
                modelVersion,
                new StableHash(modelConfigurationHash)),
            new SemanticHash(new StableHash(semanticHash)),
            new ProgramHash(new StableHash(fixedProgramHash)),
            new LayoutHash(new StableHash(fixedLayoutHash)),
            tickRate,
            new StableHash(collisionWorldHash),
            new StableHash(kccIdentityHash),
            new SimulationProtocolIdentity(protocolId, protocolVersion, new StableHash(protocolSchemaHash)));

        static T RequireEnum<T>(string value) where T : struct
        {
            if (!Enum.TryParse(value, false, out T result) || !Enum.IsDefined(typeof(T), result))
                throw new InvalidOperationException($"Deterministic Rollback Server manifest enum '{typeof(T).Name}' is invalid.");
            return result;
        }
    }

    [Serializable]
    public sealed class DeterministicRollbackServerPeerManifest
    {
        public string peerId = string.Empty;
        public string playerId = string.Empty;
        public string actorId = string.Empty;

        public string CanonicalIdentity()
        {
            if (string.IsNullOrWhiteSpace(peerId) || string.IsNullOrWhiteSpace(playerId) || string.IsNullOrWhiteSpace(actorId))
                throw new InvalidOperationException("Deterministic Rollback Server Peer identity is incomplete.");
            return $"{peerId}\u001f{playerId}\u001f{actorId}";
        }
    }
}
