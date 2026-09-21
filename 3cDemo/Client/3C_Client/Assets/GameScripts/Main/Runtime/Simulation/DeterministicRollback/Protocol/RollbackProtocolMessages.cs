using System;
using System.Collections.Generic;

namespace ThirdPersonSimulation.DeterministicRollback
{
    public enum RollbackProtocolMessageKind : byte
    {
        Handshake = 1,
        Roster = 2,
        ActorInputBatch = 3,
        CanonicalBundle = 4,
        StateHash = 5,
        SnapshotRequest = 6,
        SnapshotResponse = 7,
        Leave = 8,
        CanonicalConfirmation = 9,
        RelayedExplicitInputBatch = 10
    }

    public interface IRollbackProtocolPayload
    {
        RollbackProtocolMessageKind Kind { get; }
    }

    public sealed class RollbackHandshake : IRollbackProtocolPayload
    {
        public RollbackHandshake(
            string peerId,
            SimulationComponentIdentity model,
            GameplayContentHash gameplayContentHash,
            StableHash stateSchemaHash,
            int tickRate,
            StableHash collisionWorldHash,
            StableHash kccIdentityHash,
            SimulationProtocolIdentity protocol)
        {
            PeerId = SimulationIdentity.Require(peerId, nameof(peerId));
            if (!model.IsValid || model.Role != SimulationComponentRole.Model || !gameplayContentHash.IsValid ||
                !stateSchemaHash.IsValid ||
                tickRate <= 0 ||
                !collisionWorldHash.IsValid || !kccIdentityHash.IsValid || !protocol.IsValid)
            {
                throw new ArgumentException("Rollback handshake identity is incomplete.");
            }
            Model = model;
            GameplayContentHash = gameplayContentHash;
            StateSchemaHash = stateSchemaHash;
            TickRate = tickRate;
            CollisionWorldHash = collisionWorldHash;
            KccIdentityHash = kccIdentityHash;
            Protocol = protocol;
        }

        public RollbackProtocolMessageKind Kind => RollbackProtocolMessageKind.Handshake;
        public string PeerId { get; }
        public SimulationComponentIdentity Model { get; }
        public GameplayContentHash GameplayContentHash { get; }
        public StableHash StateSchemaHash { get; }
        public int TickRate { get; }
        public StableHash CollisionWorldHash { get; }
        public StableHash KccIdentityHash { get; }
        public SimulationProtocolIdentity Protocol { get; }

        public void RequireCompatible(RollbackHandshake other)
        {
            if (other == null || !Model.Equals(other.Model) || !GameplayContentHash.Equals(other.GameplayContentHash) ||
                !StateSchemaHash.Equals(other.StateSchemaHash) ||
                TickRate != other.TickRate || !CollisionWorldHash.Equals(other.CollisionWorldHash) ||
                !KccIdentityHash.Equals(other.KccIdentityHash) || !Protocol.Equals(other.Protocol))
            {
                throw new InvalidOperationException("Rollback handshake Character Runtime content, state schema, world, KCC, TickRate, Model, or protocol is incompatible.");
            }
        }
    }

    public sealed class RollbackRosterEntry
    {
        public RollbackRosterEntry(string peerId, string playerId, ActorId actorId)
        {
            PeerId = SimulationIdentity.Require(peerId, nameof(peerId));
            PlayerId = SimulationIdentity.Require(playerId, nameof(playerId));
            if (!actorId.IsValid)
                throw new ArgumentException("Rollback roster ActorId is invalid.", nameof(actorId));
            ActorId = actorId;
        }

        public string PeerId { get; }
        public string PlayerId { get; }
        public ActorId ActorId { get; }
    }

    public sealed class RollbackRoster : IRollbackProtocolPayload
    {
        readonly IReadOnlyList<RollbackRosterEntry> m_Entries;

        public RollbackRoster(ulong revision, IReadOnlyList<RollbackRosterEntry> entries)
            : this(revision, RollbackProtocolArray.Copy(entries), true)
        {
        }

        RollbackRoster(ulong revision, RollbackRosterEntry[] entries, bool _)
        {
            if (revision == 0)
                throw new ArgumentOutOfRangeException(nameof(revision));
            Array.Sort(entries, (left, right) => left.ActorId.CompareTo(right.ActorId));
            if (entries.Length == 0)
                throw new ArgumentException("Rollback roster is empty.", nameof(entries));
            var peers = new HashSet<string>(StringComparer.Ordinal);
            var players = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i] == null || !peers.Add(entries[i].PeerId) || !players.Add(entries[i].PlayerId) ||
                    i > 0 && entries[i - 1].ActorId.Equals(entries[i].ActorId))
                {
                    throw new ArgumentException("Rollback roster identity is duplicated.", nameof(entries));
                }
            }
            Revision = revision;
            m_Entries = entries;
            RosterHash = ComputeHash(entries);
        }

        public static RollbackRoster FromOwnedEntries(ulong revision, RollbackRosterEntry[] entries) =>
            new RollbackRoster(revision, entries ?? throw new ArgumentNullException(nameof(entries)), true);

        public RollbackProtocolMessageKind Kind => RollbackProtocolMessageKind.Roster;
        public ulong Revision { get; }
        public IReadOnlyList<RollbackRosterEntry> Entries => m_Entries;
        public StableHash RosterHash { get; }

        static StableHash ComputeHash(IReadOnlyList<RollbackRosterEntry> entries)
        {
            var values = new string[entries.Count + 1];
            values[0] = "deterministic-rollback-roster/1";
            for (int i = 0; i < entries.Count; i++)
                values[i + 1] = $"{entries[i].ActorId.Value}|{entries[i].PeerId}|{entries[i].PlayerId}";
            return StableHash.Compute(values);
        }
    }

    public sealed class RollbackActorHash
    {
        public RollbackActorHash(
            ActorId actorId,
            GameplayContentHash gameplayContentHash,
            CharacterStateHash characterStateHash)
        {
            if (!actorId.IsValid || !gameplayContentHash.IsValid || !characterStateHash.IsValid)
                throw new ArgumentException("Rollback Actor hash is incomplete.");
            ActorId = actorId;
            GameplayContentHash = gameplayContentHash;
            CharacterStateHash = characterStateHash;
        }

        public ActorId ActorId { get; }
        public GameplayContentHash GameplayContentHash { get; }
        public CharacterStateHash CharacterStateHash { get; }
    }

    public sealed class RollbackStateHashReport : IRollbackProtocolPayload
    {
        readonly IReadOnlyList<RollbackActorHash> m_Actors;

        public RollbackStateHashReport(
            string peerId,
            SimulationTick tick,
            StableHash worldHash,
            StableHash rosterHash,
            StableHash kccHash,
            IReadOnlyList<RollbackActorHash> actors)
            : this(peerId, tick, worldHash, rosterHash, kccHash, RollbackProtocolArray.Copy(actors), true)
        {
        }

        RollbackStateHashReport(
            string peerId,
            SimulationTick tick,
            StableHash worldHash,
            StableHash rosterHash,
            StableHash kccHash,
            RollbackActorHash[] actors,
            bool _)
        {
            PeerId = SimulationIdentity.Require(peerId, nameof(peerId));
            if (!tick.IsValid || !worldHash.IsValid || !rosterHash.IsValid || !kccHash.IsValid)
                throw new ArgumentException("Rollback state hash report is incomplete.");
            Array.Sort(actors, (left, right) => left.ActorId.CompareTo(right.ActorId));
            for (int i = 0; i < actors.Length; i++)
            {
                if (actors[i] == null || i > 0 && actors[i - 1].ActorId.Equals(actors[i].ActorId))
                    throw new ArgumentException("Rollback state hash Actor order is invalid.", nameof(actors));
            }
            Tick = tick;
            WorldHash = worldHash;
            RosterHash = rosterHash;
            KccHash = kccHash;
            m_Actors = actors;
        }

        public static RollbackStateHashReport FromOwnedActors(
            string peerId,
            SimulationTick tick,
            StableHash worldHash,
            StableHash rosterHash,
            StableHash kccHash,
            RollbackActorHash[] actors) =>
            new RollbackStateHashReport(
                peerId,
                tick,
                worldHash,
                rosterHash,
                kccHash,
                actors ?? throw new ArgumentNullException(nameof(actors)),
                true);

        public RollbackProtocolMessageKind Kind => RollbackProtocolMessageKind.StateHash;
        public string PeerId { get; }
        public SimulationTick Tick { get; }
        public StableHash WorldHash { get; }
        public StableHash RosterHash { get; }
        public StableHash KccHash { get; }
        public IReadOnlyList<RollbackActorHash> Actors => m_Actors;
    }

    public sealed class RollbackSnapshotRequest : IRollbackProtocolPayload
    {
        public RollbackSnapshotRequest(
            string requesterPeerId,
            string authorityPeerId,
            SimulationTick tick,
            StableHash expectedWorldHash)
        {
            RequesterPeerId = SimulationIdentity.Require(requesterPeerId, nameof(requesterPeerId));
            AuthorityPeerId = SimulationIdentity.Require(authorityPeerId, nameof(authorityPeerId));
            if (string.Equals(RequesterPeerId, AuthorityPeerId, StringComparison.Ordinal))
                throw new ArgumentException("Rollback snapshot requester and authority must be different Peers.");
            if (!tick.IsValid || !expectedWorldHash.IsValid)
                throw new ArgumentException("Rollback snapshot request is incomplete.");
            Tick = tick;
            ExpectedWorldHash = expectedWorldHash;
        }

        public RollbackProtocolMessageKind Kind => RollbackProtocolMessageKind.SnapshotRequest;
        public string RequesterPeerId { get; }
        public string AuthorityPeerId { get; }
        public SimulationTick Tick { get; }
        public StableHash ExpectedWorldHash { get; }
    }

    public sealed class RollbackSnapshotResponse : IRollbackProtocolPayload
    {
        readonly byte[] m_SnapshotBytes;

        public RollbackSnapshotResponse(
            string authorityPeerId,
            string requesterPeerId,
            SimulationTick tick,
            StableHash snapshotHash,
            ReadOnlySpan<byte> snapshotBytes)
        {
            AuthorityPeerId = SimulationIdentity.Require(authorityPeerId, nameof(authorityPeerId));
            RequesterPeerId = SimulationIdentity.Require(requesterPeerId, nameof(requesterPeerId));
            if (string.Equals(AuthorityPeerId, RequesterPeerId, StringComparison.Ordinal))
                throw new ArgumentException("Rollback snapshot authority and requester must be different Peers.");
            if (!tick.IsValid || !snapshotHash.IsValid || snapshotBytes.IsEmpty)
                throw new ArgumentException("Rollback snapshot response is incomplete.");
            Tick = tick;
            SnapshotHash = snapshotHash;
            m_SnapshotBytes = snapshotBytes.ToArray();
        }

        public RollbackProtocolMessageKind Kind => RollbackProtocolMessageKind.SnapshotResponse;
        public string AuthorityPeerId { get; }
        public string RequesterPeerId { get; }
        public SimulationTick Tick { get; }
        public StableHash SnapshotHash { get; }
        public ReadOnlySpan<byte> SnapshotBytes => m_SnapshotBytes;
        public byte[] CopySnapshotBytes() => (byte[])m_SnapshotBytes.Clone();
    }

    public sealed class RollbackLeave : IRollbackProtocolPayload
    {
        public RollbackLeave(string peerId, string reason)
        {
            PeerId = SimulationIdentity.Require(peerId, nameof(peerId));
            Reason = reason ?? string.Empty;
        }

        public RollbackProtocolMessageKind Kind => RollbackProtocolMessageKind.Leave;
        public string PeerId { get; }
        public string Reason { get; }
    }

    public sealed class RollbackProtocolEnvelope
    {
        public RollbackProtocolEnvelope(string sessionId, string senderPeerId, ulong sequence, IRollbackProtocolPayload payload)
        {
            SessionId = SimulationIdentity.Require(sessionId, nameof(sessionId));
            SenderPeerId = SimulationIdentity.Require(senderPeerId, nameof(senderPeerId));
            if (sequence == 0)
                throw new ArgumentOutOfRangeException(nameof(sequence));
            Sequence = sequence;
            Payload = payload ?? throw new ArgumentNullException(nameof(payload));
        }

        public string SessionId { get; }
        public string SenderPeerId { get; }
        public ulong Sequence { get; }
        public IRollbackProtocolPayload Payload { get; }
    }
}
