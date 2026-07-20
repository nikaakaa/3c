using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

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
            SemanticHash semanticHash,
            ProgramHash fixedProgramHash,
            LayoutHash fixedLayoutHash,
            int tickRate,
            StableHash collisionWorldHash,
            StableHash kccIdentityHash,
            SimulationProtocolIdentity protocol)
        {
            PeerId = SimulationIdentity.Require(peerId, nameof(peerId));
            if (!model.IsValid || model.Role != SimulationComponentRole.Model || !semanticHash.IsValid ||
                !fixedProgramHash.IsValid || !fixedLayoutHash.IsValid || tickRate <= 0 ||
                !collisionWorldHash.IsValid || !kccIdentityHash.IsValid || !protocol.IsValid)
            {
                throw new ArgumentException("Rollback handshake identity is incomplete.");
            }
            Model = model;
            SemanticHash = semanticHash;
            FixedProgramHash = fixedProgramHash;
            FixedLayoutHash = fixedLayoutHash;
            TickRate = tickRate;
            CollisionWorldHash = collisionWorldHash;
            KccIdentityHash = kccIdentityHash;
            Protocol = protocol;
        }

        public RollbackProtocolMessageKind Kind => RollbackProtocolMessageKind.Handshake;
        public string PeerId { get; }
        public SimulationComponentIdentity Model { get; }
        public SemanticHash SemanticHash { get; }
        public ProgramHash FixedProgramHash { get; }
        public LayoutHash FixedLayoutHash { get; }
        public int TickRate { get; }
        public StableHash CollisionWorldHash { get; }
        public StableHash KccIdentityHash { get; }
        public SimulationProtocolIdentity Protocol { get; }

        public void RequireCompatible(RollbackHandshake other)
        {
            if (other == null || !Model.Equals(other.Model) || !SemanticHash.Equals(other.SemanticHash) ||
                !FixedProgramHash.Equals(other.FixedProgramHash) || !FixedLayoutHash.Equals(other.FixedLayoutHash) ||
                TickRate != other.TickRate || !CollisionWorldHash.Equals(other.CollisionWorldHash) ||
                !KccIdentityHash.Equals(other.KccIdentityHash) || !Protocol.Equals(other.Protocol))
            {
                throw new InvalidOperationException("Rollback handshake Program, world, KCC, TickRate, Model, or protocol is incompatible.");
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
        readonly ReadOnlyCollection<RollbackRosterEntry> m_Entries;

        public RollbackRoster(ulong revision, IEnumerable<RollbackRosterEntry> entries)
        {
            if (revision == 0)
                throw new ArgumentOutOfRangeException(nameof(revision));
            var values = new List<RollbackRosterEntry>(entries ?? throw new ArgumentNullException(nameof(entries)));
            values.Sort((left, right) => left.ActorId.CompareTo(right.ActorId));
            if (values.Count == 0)
                throw new ArgumentException("Rollback roster is empty.", nameof(entries));
            var peers = new HashSet<string>(StringComparer.Ordinal);
            var players = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < values.Count; i++)
            {
                if (values[i] == null || !peers.Add(values[i].PeerId) || !players.Add(values[i].PlayerId) ||
                    i > 0 && values[i - 1].ActorId.Equals(values[i].ActorId))
                {
                    throw new ArgumentException("Rollback roster identity is duplicated.", nameof(entries));
                }
            }
            Revision = revision;
            m_Entries = values.AsReadOnly();
            RosterHash = ComputeHash(values);
        }

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
        readonly ReadOnlyCollection<KeyValuePair<string, StableHash>> m_Modules;

        public RollbackActorHash(ActorId actorId, StableHash actorHash, IEnumerable<KeyValuePair<string, StableHash>> modules)
        {
            if (!actorId.IsValid || !actorHash.IsValid)
                throw new ArgumentException("Rollback Actor hash is incomplete.");
            var values = new List<KeyValuePair<string, StableHash>>(modules ?? Array.Empty<KeyValuePair<string, StableHash>>());
            values.Sort((left, right) => string.CompareOrdinal(left.Key, right.Key));
            for (int i = 0; i < values.Count; i++)
            {
                SimulationIdentity.Require(values[i].Key, nameof(modules));
                if (!values[i].Value.IsValid || i > 0 && string.Equals(values[i - 1].Key, values[i].Key, StringComparison.Ordinal))
                    throw new ArgumentException("Rollback module hash is invalid or duplicated.", nameof(modules));
            }
            ActorId = actorId;
            ActorHash = actorHash;
            m_Modules = values.AsReadOnly();
        }

        public ActorId ActorId { get; }
        public StableHash ActorHash { get; }
        public IReadOnlyList<KeyValuePair<string, StableHash>> Modules => m_Modules;
    }

    public sealed class RollbackStateHashReport : IRollbackProtocolPayload
    {
        readonly ReadOnlyCollection<RollbackActorHash> m_Actors;

        public RollbackStateHashReport(
            string peerId,
            SimulationTick tick,
            StableHash worldHash,
            StableHash rosterHash,
            StableHash kccHash,
            IEnumerable<RollbackActorHash> actors)
        {
            PeerId = SimulationIdentity.Require(peerId, nameof(peerId));
            if (!tick.IsValid || !worldHash.IsValid || !rosterHash.IsValid || !kccHash.IsValid)
                throw new ArgumentException("Rollback state hash report is incomplete.");
            var values = new List<RollbackActorHash>(actors ?? throw new ArgumentNullException(nameof(actors)));
            values.Sort((left, right) => left.ActorId.CompareTo(right.ActorId));
            for (int i = 0; i < values.Count; i++)
            {
                if (values[i] == null || i > 0 && values[i - 1].ActorId.Equals(values[i].ActorId))
                    throw new ArgumentException("Rollback state hash Actor order is invalid.", nameof(actors));
            }
            Tick = tick;
            WorldHash = worldHash;
            RosterHash = rosterHash;
            KccHash = kccHash;
            m_Actors = values.AsReadOnly();
        }

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
            byte[] snapshotBytes)
        {
            AuthorityPeerId = SimulationIdentity.Require(authorityPeerId, nameof(authorityPeerId));
            RequesterPeerId = SimulationIdentity.Require(requesterPeerId, nameof(requesterPeerId));
            if (string.Equals(AuthorityPeerId, RequesterPeerId, StringComparison.Ordinal))
                throw new ArgumentException("Rollback snapshot authority and requester must be different Peers.");
            if (!tick.IsValid || !snapshotHash.IsValid || snapshotBytes == null || snapshotBytes.Length == 0)
                throw new ArgumentException("Rollback snapshot response is incomplete.");
            Tick = tick;
            SnapshotHash = snapshotHash;
            m_SnapshotBytes = (byte[])snapshotBytes.Clone();
        }

        public RollbackProtocolMessageKind Kind => RollbackProtocolMessageKind.SnapshotResponse;
        public string AuthorityPeerId { get; }
        public string RequesterPeerId { get; }
        public SimulationTick Tick { get; }
        public StableHash SnapshotHash { get; }
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
