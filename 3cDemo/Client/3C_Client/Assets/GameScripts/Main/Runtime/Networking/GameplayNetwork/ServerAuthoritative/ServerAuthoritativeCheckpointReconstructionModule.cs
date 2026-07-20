using System;
using System.Collections.Generic;
using System.IO;
using ThirdPersonSimulation;
using ThirdPersonSimulation.ServerAuthoritative;
using ThirdPersonSimulation.ServerAuthoritative.Transport;

namespace ThirdPersonGameplay.Networking.ServerAuthoritative
{
    internal enum ServerAuthoritativeCheckpointResultKind : byte
    {
        Ignored = 0,
        BaselineMissing = 1,
        Accepted = 2
    }

    internal readonly struct ServerAuthoritativeCheckpointResult
    {
        public ServerAuthoritativeCheckpointResult(
            ServerAuthoritativeCheckpointResultKind kind,
            ulong snapshotSequence,
            AuthoritativeInputAck ack,
            AuthoritativeActorBaseline baseline,
            RemotePresentationBatch remote)
        {
            Kind = kind;
            SnapshotSequence = snapshotSequence;
            Ack = ack;
            Baseline = baseline;
            Remote = remote;
        }

        public ServerAuthoritativeCheckpointResultKind Kind { get; }
        public ulong SnapshotSequence { get; }
        public AuthoritativeInputAck Ack { get; }
        public AuthoritativeActorBaseline Baseline { get; }
        public RemotePresentationBatch Remote { get; }
    }

    internal readonly struct ServerAuthoritativeCheckpointMetrics
    {
        public ServerAuthoritativeCheckpointMetrics(
            ulong baselineMisses,
            ulong reconstructionFailures,
            ulong latestSnapshotSequence)
        {
            BaselineMisses = baselineMisses;
            ReconstructionFailures = reconstructionFailures;
            LatestSnapshotSequence = latestSnapshotSequence;
        }

        public ulong BaselineMisses { get; }
        public ulong ReconstructionFailures { get; }
        public ulong LatestSnapshotSequence { get; }
    }

    internal sealed class ServerAuthoritativeCheckpointReconstructionModule
    {
        readonly NetworkCheckpointLayout m_Layout;
        readonly ActorId m_OwnerActor;
        readonly int m_Capacity;
        readonly SortedDictionary<ulong, NetworkCheckpoint> m_Checkpoints =
            new SortedDictionary<ulong, NetworkCheckpoint>();
        ulong m_LatestSnapshotSequence;
        ulong m_LatestAuthorityTick;
        ulong m_BaselineMisses;
        ulong m_ReconstructionFailures;
        bool m_FullCheckpointRequested;

        public ServerAuthoritativeCheckpointReconstructionModule(
            NetworkCheckpointLayout layout,
            ActorId ownerActor,
            int capacity)
        {
            m_Layout = layout ?? throw new ArgumentNullException(nameof(layout));
            m_OwnerActor = ownerActor.IsValid
                ? ownerActor
                : throw new ArgumentException("Owner ActorId is invalid.", nameof(ownerActor));
            m_Capacity = capacity > 0 ? capacity : throw new ArgumentOutOfRangeException(nameof(capacity));
        }

        public ulong LatestSnapshotSequence => m_LatestSnapshotSequence;

        public ServerAuthoritativeCheckpointResult AcceptDelta(
            SnapshotDatagram snapshot,
            ActorId remoteActor)
        {
            if (snapshot == null)
                throw new ArgumentNullException(nameof(snapshot));
            if (!remoteActor.IsValid || remoteActor == m_OwnerActor)
                throw new ArgumentException("Remote ActorId is invalid.", nameof(remoteActor));
            if (snapshot.SnapshotSequence <= m_LatestSnapshotSequence)
                return new ServerAuthoritativeCheckpointResult(ServerAuthoritativeCheckpointResultKind.Ignored, snapshot.SnapshotSequence, null, null, null);
            if (!m_Checkpoints.TryGetValue(snapshot.BaseSnapshotSequence, out NetworkCheckpoint baseline))
            {
                m_BaselineMisses++;
                return new ServerAuthoritativeCheckpointResult(ServerAuthoritativeCheckpointResultKind.BaselineMissing, snapshot.SnapshotSequence, null, null, null);
            }
            NetworkCheckpoint checkpoint;
            RemotePresentationBatch remote;
            try
            {
                checkpoint = NetworkCheckpointCodec.ReadDelta(
                    m_Layout,
                    baseline,
                    new SimulationTick(snapshot.AuthorityTick),
                    snapshot.AcknowledgedInputSequence,
                    snapshot.ReliableEventHorizon,
                    remoteActor,
                    snapshot.CopyDeltaPayload(),
                    out remote);
            }
            catch (Exception exception)
            {
                m_ReconstructionFailures++;
                throw new InvalidDataException("Snapshot delta reconstruction failed.", exception);
            }
            if (checkpoint.Baseline.ActorId != m_OwnerActor || remote.ActorId != remoteActor ||
                checkpoint.Baseline.ConfirmedEventHorizon.Sequence != snapshot.ReliableEventHorizon)
            {
                throw new InvalidOperationException("Snapshot owner, remote Actor, or reliable event horizon is invalid.");
            }
            return Accept(
                snapshot.SnapshotSequence,
                checkpoint,
                new AuthoritativeInputAck(
                    m_OwnerActor,
                    checkpoint.Baseline.AuthorityTick,
                    snapshot.AcknowledgedInputSequence,
                    checkpoint.Baseline.ConfirmedEventHorizon),
                remote);
        }

        public ServerAuthoritativeCheckpointResult AcceptFull(
            ulong snapshotSequence,
            ulong authorityTick,
            ulong confirmedInputSequence,
            ulong reliableEventHorizon,
            string layoutHash,
            string checkpointHash,
            byte[] payload)
        {
            if (snapshotSequence == 0 || authorityTick == 0 || payload == null)
                throw new InvalidOperationException("Full checkpoint identity or payload boundary is invalid.");
            if (!string.Equals(layoutHash, m_Layout.LayoutIdentity.ToString(), StringComparison.Ordinal))
                throw new InvalidOperationException("Full checkpoint layout does not match the active Program.");
            NetworkCheckpoint checkpoint = NetworkCheckpointCodec.ReadFull(m_Layout, payload);
            if (checkpoint.Baseline.ActorId != m_OwnerActor || checkpoint.Baseline.AuthorityTick.Value != authorityTick ||
                checkpoint.Baseline.ConfirmedInputSequence != confirmedInputSequence ||
                checkpoint.Baseline.ConfirmedEventHorizon.Sequence != reliableEventHorizon ||
                !string.Equals(checkpoint.CheckpointHash.ToString(), checkpointHash, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Full checkpoint metadata does not match its canonical payload.");
            }
            if (snapshotSequence <= m_LatestSnapshotSequence)
                return new ServerAuthoritativeCheckpointResult(ServerAuthoritativeCheckpointResultKind.Ignored, snapshotSequence, null, null, null);
            if (authorityTick < m_LatestAuthorityTick)
            {
                throw new InvalidOperationException(
                    $"Full checkpoint sequence advances while authority Tick regresses: snapshot={snapshotSequence};authorityTick={authorityTick};latestSnapshot={m_LatestSnapshotSequence};latestAuthorityTick={m_LatestAuthorityTick}.");
            }
            m_FullCheckpointRequested = false;
            return Accept(
                snapshotSequence,
                checkpoint,
                new AuthoritativeInputAck(
                    m_OwnerActor,
                    checkpoint.Baseline.AuthorityTick,
                    checkpoint.Baseline.ConfirmedInputSequence,
                    checkpoint.Baseline.ConfirmedEventHorizon),
                null);
        }

        public bool TryBeginFullCheckpointRequest()
        {
            if (m_FullCheckpointRequested)
                return false;
            m_FullCheckpointRequested = true;
            return true;
        }

        public ServerAuthoritativeCheckpointMetrics CaptureMetrics() =>
            new ServerAuthoritativeCheckpointMetrics(
                m_BaselineMisses,
                m_ReconstructionFailures,
                m_LatestSnapshotSequence);

        ServerAuthoritativeCheckpointResult Accept(
            ulong snapshotSequence,
            NetworkCheckpoint checkpoint,
            AuthoritativeInputAck ack,
            RemotePresentationBatch remote)
        {
            Store(snapshotSequence, checkpoint);
            m_LatestSnapshotSequence = snapshotSequence;
            m_LatestAuthorityTick = checkpoint.Baseline.AuthorityTick.Value;
            return new ServerAuthoritativeCheckpointResult(
                ServerAuthoritativeCheckpointResultKind.Accepted,
                snapshotSequence,
                ack,
                checkpoint.Baseline,
                remote);
        }

        void Store(ulong sequence, NetworkCheckpoint checkpoint)
        {
            m_Checkpoints[sequence] = checkpoint;
            while (m_Checkpoints.Count > m_Capacity)
            {
                using IEnumerator<ulong> iterator = m_Checkpoints.Keys.GetEnumerator();
                iterator.MoveNext();
                m_Checkpoints.Remove(iterator.Current);
            }
        }
    }
}
