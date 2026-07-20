using System;
using System.Collections.Generic;
using ThirdPersonSimulation.Fixed;

namespace ThirdPersonSimulation.DeterministicRollback
{
    public sealed class RollbackEndpointRuntimeBridge :
        IFixedSourceEgressOutputPort,
        IRollbackNetworkDiagnosticsSource
    {
        readonly RollbackPeerEndpoint m_Peer;
        readonly RollbackRuntimeState m_State;
        readonly IFixedSimulationSessionSnapshotCodec m_SnapshotCodec;
        readonly DeterministicRollbackModelPolicy m_Policy;
        readonly Dictionary<ulong, RollbackStateHashReport> m_LocalReports =
            new Dictionary<ulong, RollbackStateHashReport>();
        readonly Dictionary<ulong, Dictionary<string, RollbackStateHashReport>> m_RemoteReports =
            new Dictionary<ulong, Dictionary<string, RollbackStateHashReport>>();
        readonly Dictionary<ulong, StableHash> m_RequestedSnapshots =
            new Dictionary<ulong, StableHash>();

        public RollbackEndpointRuntimeBridge(
            RollbackPeerEndpoint peer,
            RollbackRuntimeState state,
            IFixedSimulationSessionSnapshotCodec snapshotCodec,
            DeterministicRollbackModelPolicy policy)
        {
            m_Peer = peer ?? throw new ArgumentNullException(nameof(peer));
            m_State = state ?? throw new ArgumentNullException(nameof(state));
            m_SnapshotCodec = snapshotCodec ?? throw new ArgumentNullException(nameof(snapshotCodec));
            m_Policy = policy ?? throw new ArgumentNullException(nameof(policy));
            if (!m_Peer.IsReady || !string.Equals(m_Peer.LocalPeerId, state.LocalPeerId, StringComparison.Ordinal))
                throw new InvalidOperationException("Rollback Endpoint bridge requires the ready local Peer bound to Runtime state.");
        }

        public string LastDesyncScope { get; private set; } = string.Empty;
        public ulong SnapshotRecoveryCount { get; private set; }

        public RollbackNetworkDiagnosticsSnapshot CaptureNetworkDiagnostics()
        {
            int remoteCount = 0;
            foreach (Dictionary<string, RollbackStateHashReport> reports in m_RemoteReports.Values)
                remoteCount = checked(remoteCount + reports.Count);
            RollbackStateHashReport latest = null;
            ulong latestTick = 0;
            foreach (KeyValuePair<ulong, RollbackStateHashReport> pair in m_LocalReports)
            {
                if (pair.Key < latestTick)
                    continue;
                latestTick = pair.Key;
                latest = pair.Value;
            }
            return new RollbackNetworkDiagnosticsSnapshot(
                latestTick,
                latest?.WorldHash ?? default,
                latest?.KccHash ?? default,
                m_LocalReports.Count,
                remoteCount,
                m_RequestedSnapshots.Count + (m_State.TryGetRequiredRecovery(out _, out _) ? 1 : 0),
                SnapshotRecoveryCount,
                m_Peer.DroppedReceivedDatagrams,
                LastDesyncScope);
        }

        public void Commit(FixedSourceEgressRecord record)
        {
            if (record == null)
                throw new ArgumentNullException(nameof(record));
            if (!string.Equals(record.ChannelId, RollbackSourceEgressChannels.StateHash, StringComparison.Ordinal) ||
                !string.Equals(record.SchemaId, RollbackSourceEgressChannels.StateHashSchema, StringComparison.Ordinal) ||
                record.SchemaVersion != RollbackSourceEgressChannels.StateHashSchemaVersion)
            {
                throw new InvalidOperationException($"Rollback Source Egress channel '{record.ChannelId}' is unsupported.");
            }
            if (RollbackProtocolCodec.ReadCanonicalPayload(record.CopyPayload()) is not RollbackStateHashReport report ||
                !string.Equals(report.PeerId, m_Peer.LocalPeerId, StringComparison.Ordinal) ||
                report.Tick != record.Tick ||
                !report.RosterHash.Equals(m_State.RosterHash))
            {
                throw new InvalidOperationException("Rollback state hash Egress payload does not match the active local Peer.");
            }
            AddLocalReport(report);
            m_Peer.SendStateHash(report);
        }

        public void Pump()
        {
            m_Peer.Pump();
            PumpControl();
            RequestRequiredRecovery();
        }

        void PumpControl()
        {
            while (m_Peer.TryReceiveControl(out IRollbackProtocolPayload payload))
            {
                switch (payload)
                {
                    case RollbackStateHashReport report:
                        ReceiveHash(report);
                        break;
                    case RollbackSnapshotRequest request:
                        ReceiveSnapshotRequest(request);
                        break;
                    case RollbackSnapshotResponse response:
                        ReceiveSnapshotResponse(response);
                        break;
                    default:
                        throw new InvalidOperationException($"Rollback control payload '{payload.Kind}' is unsupported by Runtime.");
                }
            }
            ReleaseOldReports();
        }

        void AddLocalReport(RollbackStateHashReport report)
        {
            if (m_LocalReports.TryGetValue(report.Tick.Value, out RollbackStateHashReport current))
            {
                if (!ReportsEqual(current, report, out _))
                    throw new InvalidOperationException($"Rollback local hash Tick '{report.Tick}' changed after publication.");
                return;
            }
            RequireReportCapacity();
            m_LocalReports.Add(report.Tick.Value, report);
            CompareAvailable(report.Tick.Value);
        }

        void ReceiveHash(RollbackStateHashReport report)
        {
            if (report == null || string.Equals(report.PeerId, m_Peer.LocalPeerId, StringComparison.Ordinal) ||
                !report.RosterHash.Equals(m_State.RosterHash) || !RosterContains(report.PeerId))
            {
                throw new InvalidOperationException("Rollback remote state hash identity is invalid.");
            }
            if (!m_RemoteReports.TryGetValue(report.Tick.Value, out Dictionary<string, RollbackStateHashReport> peers))
            {
                RequireReportCapacity();
                peers = new Dictionary<string, RollbackStateHashReport>(StringComparer.Ordinal);
                m_RemoteReports.Add(report.Tick.Value, peers);
            }
            if (peers.TryGetValue(report.PeerId, out RollbackStateHashReport current))
            {
                if (!ReportsEqual(current, report, out _))
                    throw new InvalidOperationException($"Rollback Peer '{report.PeerId}' changed hash Tick '{report.Tick}'.");
                return;
            }
            peers.Add(report.PeerId, report);
            CompareAvailable(report.Tick.Value);
        }

        void CompareAvailable(ulong tick)
        {
            if (!m_LocalReports.TryGetValue(tick, out RollbackStateHashReport local) ||
                !m_RemoteReports.TryGetValue(tick, out Dictionary<string, RollbackStateHashReport> remotes))
            {
                return;
            }
            foreach (RollbackStateHashReport remote in remotes.Values)
            {
                if (ReportsEqual(local, remote, out string scope))
                    continue;
                LastDesyncScope = scope;
                string authorityPeerId = GetSnapshotAuthorityPeerId();
                if (string.Equals(authorityPeerId, m_Peer.LocalPeerId, StringComparison.Ordinal) ||
                    !string.Equals(remote.PeerId, authorityPeerId, StringComparison.Ordinal) ||
                    m_RequestedSnapshots.ContainsKey(tick))
                {
                    continue;
                }
                if (m_RequestedSnapshots.Count >= m_Policy.MaximumQueuedSnapshots)
                    throw new InvalidOperationException("Rollback snapshot recovery request capacity is exhausted.");
                m_RequestedSnapshots.Add(tick, remote.WorldHash);
                m_Peer.SendSnapshotRequest(new RollbackSnapshotRequest(
                    m_Peer.LocalPeerId,
                    authorityPeerId,
                    new SimulationTick(tick),
                    remote.WorldHash));
            }
        }

        void ReceiveSnapshotRequest(RollbackSnapshotRequest request)
        {
            if (request == null || !RosterContains(request.RequesterPeerId) ||
                !string.Equals(request.AuthorityPeerId, GetSnapshotAuthorityPeerId(), StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Rollback snapshot request authority or requester is invalid.");
            }
            if (!string.Equals(request.AuthorityPeerId, m_Peer.LocalPeerId, StringComparison.Ordinal))
                return;
            FixedSimulationSessionSnapshot snapshot = m_State.Snapshots.GetRequired(request.Tick);
            if (!snapshot.World.WorldHash.Value.Equals(request.ExpectedWorldHash))
                throw new InvalidOperationException("Rollback snapshot request expected WorldHash does not match authority history.");
            m_Peer.SendSnapshotResponse(new RollbackSnapshotResponse(
                m_Peer.LocalPeerId,
                request.RequesterPeerId,
                request.Tick,
                snapshot.SnapshotHash,
                m_SnapshotCodec.Write(snapshot)));
        }

        void ReceiveSnapshotResponse(RollbackSnapshotResponse response)
        {
            if (response == null || !string.Equals(response.AuthorityPeerId, GetSnapshotAuthorityPeerId(), StringComparison.Ordinal) ||
                !RosterContains(response.RequesterPeerId))
            {
                throw new InvalidOperationException("Rollback snapshot response routing identity is invalid.");
            }
            if (!string.Equals(response.RequesterPeerId, m_Peer.LocalPeerId, StringComparison.Ordinal))
                return;
            if (!m_RequestedSnapshots.TryGetValue(response.Tick.Value, out StableHash expectedWorldHash))
                throw new InvalidOperationException("Rollback snapshot response has no pending local request.");
            FixedSimulationSessionSnapshot snapshot = m_SnapshotCodec.Read(response.CopySnapshotBytes());
            if (snapshot.Tick != response.Tick || !snapshot.SnapshotHash.Equals(response.SnapshotHash) ||
                !snapshot.World.WorldHash.Value.Equals(expectedWorldHash))
            {
                throw new InvalidOperationException("Rollback recovery snapshot identity or WorldHash is invalid.");
            }
            m_State.InstallRecoverySnapshot(snapshot);
            m_RequestedSnapshots.Remove(response.Tick.Value);
            SnapshotRecoveryCount = checked(SnapshotRecoveryCount + 1);
        }

        void RequestRequiredRecovery()
        {
            if (!m_State.TryGetRequiredRecovery(out SimulationTick firstAffectedTick, out string reason) ||
                m_RequestedSnapshots.Count != 0)
            {
                return;
            }
            string authorityPeerId = GetSnapshotAuthorityPeerId();
            if (string.Equals(authorityPeerId, m_Peer.LocalPeerId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Rollback snapshot authority cannot recover its own missing history: tick={firstAffectedTick}; reason={reason}.");
            }
            ulong selectedTick = 0;
            RollbackStateHashReport selected = null;
            foreach (KeyValuePair<ulong, Dictionary<string, RollbackStateHashReport>> pair in m_RemoteReports)
            {
                if (pair.Key < firstAffectedTick.Value || pair.Key > m_State.LastCompletedTick || pair.Key < selectedTick ||
                    !pair.Value.TryGetValue(authorityPeerId, out RollbackStateHashReport report))
                {
                    continue;
                }
                selectedTick = pair.Key;
                selected = report;
            }
            if (selected == null)
                return;
            if (m_RequestedSnapshots.Count >= m_Policy.MaximumQueuedSnapshots)
                throw new InvalidOperationException("Rollback snapshot recovery request capacity is exhausted.");
            m_RequestedSnapshots.Add(selectedTick, selected.WorldHash);
            m_Peer.SendSnapshotRequest(new RollbackSnapshotRequest(
                m_Peer.LocalPeerId,
                authorityPeerId,
                new SimulationTick(selectedTick),
                selected.WorldHash));
        }

        string GetSnapshotAuthorityPeerId()
        {
            if (m_Policy.SnapshotAuthority != RollbackSnapshotAuthority.LowestPeerId)
                throw new InvalidOperationException("Rollback snapshot authority policy is unsupported.");
            string authority = null;
            for (int i = 0; i < m_Peer.Roster.Entries.Count; i++)
            {
                string peerId = m_Peer.Roster.Entries[i].PeerId;
                if (authority == null || string.CompareOrdinal(peerId, authority) < 0)
                    authority = peerId;
            }
            return authority ?? throw new InvalidOperationException("Rollback roster has no snapshot authority Peer.");
        }

        bool RosterContains(string peerId)
        {
            for (int i = 0; i < m_Peer.Roster.Entries.Count; i++)
            {
                if (string.Equals(m_Peer.Roster.Entries[i].PeerId, peerId, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        void RequireReportCapacity()
        {
            int count = m_LocalReports.Count;
            foreach (Dictionary<string, RollbackStateHashReport> peers in m_RemoteReports.Values)
                count = checked(count + peers.Count);
            int capacity = checked(m_Policy.HistoryLengthTicks * m_Peer.Roster.Entries.Count);
            if (count >= capacity)
                throw new InvalidOperationException("Rollback state hash report capacity is exhausted.");
        }

        void ReleaseOldReports()
        {
            ulong retain = (ulong)m_Policy.HistoryLengthTicks;
            ulong floor = m_State.ConfirmedTick > retain ? m_State.ConfirmedTick - retain + 1 : 1;
            RemoveBefore(m_LocalReports, floor);
            RemoveBefore(m_RemoteReports, floor);
            RemoveBefore(m_RequestedSnapshots, floor);
        }

        static void RemoveBefore<T>(Dictionary<ulong, T> values, ulong floor)
        {
            var remove = new List<ulong>();
            foreach (ulong tick in values.Keys)
            {
                if (tick < floor)
                    remove.Add(tick);
            }
            for (int i = 0; i < remove.Count; i++)
                values.Remove(remove[i]);
        }

        static bool ReportsEqual(
            RollbackStateHashReport left,
            RollbackStateHashReport right,
            out string scope)
        {
            if (!left.RosterHash.Equals(right.RosterHash))
            {
                scope = "roster";
                return false;
            }
            if (!left.WorldHash.Equals(right.WorldHash))
            {
                if (!left.KccHash.Equals(right.KccHash))
                    scope = "kcc-world";
                else
                    scope = FindActorScope(left.Actors, right.Actors);
                return false;
            }
            scope = string.Empty;
            return true;
        }

        static string FindActorScope(
            IReadOnlyList<RollbackActorHash> left,
            IReadOnlyList<RollbackActorHash> right)
        {
            if (left.Count != right.Count)
                return "actor-roster";
            for (int i = 0; i < left.Count; i++)
            {
                if (!left[i].ActorId.Equals(right[i].ActorId))
                    return "actor-roster";
                if (left[i].ActorHash.Equals(right[i].ActorHash))
                    continue;
                int count = Math.Min(left[i].Modules.Count, right[i].Modules.Count);
                for (int module = 0; module < count; module++)
                {
                    if (!string.Equals(left[i].Modules[module].Key, right[i].Modules[module].Key, StringComparison.Ordinal) ||
                        !left[i].Modules[module].Value.Equals(right[i].Modules[module].Value))
                    {
                        return $"actor:{left[i].ActorId}/module:{left[i].Modules[module].Key}";
                    }
                }
                return $"actor:{left[i].ActorId}";
            }
            return "world";
        }
    }
}
