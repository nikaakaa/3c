using System;
using System.Collections.Generic;
using System.Net;

namespace ThirdPersonSimulation.DeterministicRollback
{
    public readonly struct RollbackRelayPeerInputFrontier
    {
        public RollbackRelayPeerInputFrontier(string peerId, ActorId actorId, ulong tick)
        {
            PeerId = peerId;
            ActorId = actorId;
            Tick = tick;
        }

        public string PeerId { get; }
        public ActorId ActorId { get; }
        public ulong Tick { get; }
    }

    public readonly struct RollbackRelayPeerStatus
    {
        public RollbackRelayPeerStatus(
            string peerId,
            string playerId,
            ActorId actorId,
            bool handshakeAccepted,
            bool hasInputFrontier,
            ulong inputFrontier)
        {
            PeerId = peerId;
            PlayerId = playerId;
            ActorId = actorId;
            HandshakeAccepted = handshakeAccepted;
            HasInputFrontier = hasInputFrontier;
            InputFrontier = inputFrontier;
        }

        public string PeerId { get; }
        public string PlayerId { get; }
        public ActorId ActorId { get; }
        public bool HandshakeAccepted { get; }
        public bool HasInputFrontier { get; }
        public ulong InputFrontier { get; }
    }

    public sealed class RollbackInputRelayRuntime : IDisposable
    {
        sealed class PeerState
        {
            public PeerState(RollbackRosterEntry roster, RollbackDatagramChannel channel)
            {
                Roster = roster;
                Channel = channel;
            }

            public RollbackRosterEntry Roster { get; }
            public RollbackDatagramChannel Channel { get; }
            public bool HandshakeAccepted { get; set; }
        }

        readonly RollbackEndpointDefinition m_Definition;
        readonly DeterministicRollbackModelPolicy m_Policy;
        readonly RollbackHandshake m_RelayHandshake;
        readonly RollbackRoster m_Roster;
        readonly RollbackDatagramEndpoint m_Endpoint;
        readonly int m_InputRedundancyCount;
        readonly Dictionary<string, RollbackRosterEntry> m_ExpectedPeers = new Dictionary<string, RollbackRosterEntry>(StringComparer.Ordinal);
        readonly Dictionary<string, PeerState> m_Peers = new Dictionary<string, PeerState>(StringComparer.Ordinal);
        RollbackCanonicalInputAssembler m_Assembler;
        ulong m_AssembledCount;
        ulong m_InputBatchCount;
        ulong m_ExplicitRelayBroadcastCount;
        ulong m_DeduplicatedInputCount;
        ulong m_InvalidInputCount;
        ulong m_ConfirmationBroadcastCount;
        ulong m_HashReportCount;
        ulong m_LastConfirmedBroadcastTick;
        bool m_Disposed;

        public RollbackInputRelayRuntime(
            RollbackEndpointDefinition definition,
            DeterministicRollbackModelPolicy policy,
            RollbackHandshake handshakeTemplate,
            string relayServerPeerId,
            RollbackRoster roster,
            int inputRedundancyCount)
        {
            m_Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            m_Policy = policy ?? throw new ArgumentNullException(nameof(policy));
            if (handshakeTemplate == null)
                throw new ArgumentNullException(nameof(handshakeTemplate));
            m_Roster = roster ?? throw new ArgumentNullException(nameof(roster));
            if (inputRedundancyCount <= 0 || inputRedundancyCount > definition.MaximumQueuedMessages)
                throw new ArgumentOutOfRangeException(nameof(inputRedundancyCount));
            m_InputRedundancyCount = inputRedundancyCount;
            m_RelayHandshake = new RollbackHandshake(
                relayServerPeerId,
                handshakeTemplate.Model,
                handshakeTemplate.SemanticHash,
                handshakeTemplate.FixedProgramHash,
                handshakeTemplate.FixedLayoutHash,
                handshakeTemplate.TickRate,
                handshakeTemplate.CollisionWorldHash,
                handshakeTemplate.KccIdentityHash,
                handshakeTemplate.Protocol);
            for (int i = 0; i < roster.Entries.Count; i++)
                m_ExpectedPeers.Add(roster.Entries[i].PeerId, roster.Entries[i]);
            m_Endpoint = new RollbackDatagramEndpoint(
                new IPEndPoint(definition.Address, definition.Port),
                definition.MaximumQueuedMessages,
                definition.MaximumDatagramBytes);
        }

        public IPEndPoint LocalEndPoint => m_Endpoint.LocalEndPoint;
        public bool IsRosterLocked => m_Assembler != null;
        public SimulationTick NextCanonicalTick => m_Assembler == null ? default : m_Assembler.NextTick;
        public int ReceiveQueueDepth => m_Endpoint.ReceiveQueueDepth;
        public int MaximumReceiveQueueDepth => m_Endpoint.MaximumReceiveQueueDepth;
        public int SendQueueDepth => m_Endpoint.SendQueueDepth;
        public int MaximumSendQueueDepth => m_Endpoint.MaximumSendQueueDepth;
        public long TotalReceivedDatagrams => m_Endpoint.TotalReceivedDatagrams;
        public long TotalSentDatagrams => m_Endpoint.TotalSentDatagrams;
        public long DroppedReceivedDatagrams => m_Endpoint.DroppedReceivedDatagrams;
        public ulong InputBatchCount => m_InputBatchCount;
        public ulong CanonicalBundleCount => m_AssembledCount;
        public ulong ExplicitRelayBroadcastCount => m_ExplicitRelayBroadcastCount;
        public ulong DeduplicatedInputCount => m_DeduplicatedInputCount;
        public ulong InvalidInputCount => m_InvalidInputCount;
        public ulong ConfirmationBroadcastCount => m_ConfirmationBroadcastCount;
        public ulong ConfirmedCanonicalTick => m_Assembler?.ConfirmedTick ?? 0;
        public ulong HashReportCount => m_HashReportCount;
        public int PendingReliableCount
        {
            get
            {
                int result = 0;
                foreach (PeerState peer in m_Peers.Values)
                    result = checked(result + peer.Channel.PendingReliableCount);
                return result;
            }
        }

        public IReadOnlyList<RollbackRelayPeerStatus> CapturePeerStatus()
        {
            IReadOnlyList<RollbackRelayPeerInputFrontier> frontiers = CapturePeerInputFrontiers();
            var result = new RollbackRelayPeerStatus[m_Roster.Entries.Count];
            for (int i = 0; i < result.Length; i++)
            {
                RollbackRosterEntry roster = m_Roster.Entries[i];
                bool hasFrontier = false;
                ulong frontier = 0;
                for (int j = 0; j < frontiers.Count; j++)
                {
                    if (!frontiers[j].ActorId.Equals(roster.ActorId))
                        continue;
                    hasFrontier = true;
                    frontier = frontiers[j].Tick;
                    break;
                }
                result[i] = new RollbackRelayPeerStatus(
                    roster.PeerId,
                    roster.PlayerId,
                    roster.ActorId,
                    m_Peers.TryGetValue(roster.PeerId, out PeerState peer) && peer.HandshakeAccepted,
                    hasFrontier,
                    frontier);
            }
            return result;
        }

        public IReadOnlyList<RollbackRelayPeerInputFrontier> CapturePeerInputFrontiers()
        {
            if (m_Assembler == null)
                return Array.Empty<RollbackRelayPeerInputFrontier>();
            IReadOnlyList<RollbackExplicitInputFrontier> source = m_Assembler.CaptureExplicitInputFrontiers();
            var result = new RollbackRelayPeerInputFrontier[source.Count];
            for (int i = 0; i < source.Count; i++)
            {
                RollbackExplicitInputFrontier frontier = source[i];
                RollbackRosterEntry roster = null;
                for (int rosterIndex = 0; rosterIndex < m_Roster.Entries.Count; rosterIndex++)
                {
                    if (m_Roster.Entries[rosterIndex].ActorId.Equals(frontier.ActorId))
                    {
                        roster = m_Roster.Entries[rosterIndex];
                        break;
                    }
                }
                if (roster == null)
                    throw new InvalidOperationException($"Rollback Relay diagnostics Actor '{frontier.ActorId}' is absent from the roster.");
                result[i] = new RollbackRelayPeerInputFrontier(roster.PeerId, frontier.ActorId, frontier.Tick);
            }
            return result;
        }

        public void Pump()
        {
            ThrowIfDisposed();
            while (m_Endpoint.TryReceive(out RollbackReceivedDatagram received))
            {
                RollbackDatagramPacket packet = received.Packet;
                if (!string.Equals(packet.SessionId, m_Definition.SessionId, StringComparison.Ordinal) ||
                    !m_ExpectedPeers.TryGetValue(packet.SenderPeerId, out RollbackRosterEntry rosterEntry))
                {
                    m_InvalidInputCount = checked(m_InvalidInputCount + 1);
                    throw new InvalidOperationException("Rollback Relay received a datagram for an unknown Session or Peer.");
                }
                if (!m_Peers.TryGetValue(packet.SenderPeerId, out PeerState peer))
                {
                    peer = new PeerState(
                        rosterEntry,
                        new RollbackDatagramChannel(
                            m_Endpoint,
                            m_Definition,
                            m_RelayHandshake.PeerId,
                            packet.SenderPeerId,
                            received.RemoteEndPoint));
                    m_Peers.Add(packet.SenderPeerId, peer);
                }
                peer.Channel.Process(received);
            }
            foreach (PeerState peer in m_Peers.Values)
            {
                while (peer.Channel.TryReceive(out RollbackProtocolEnvelope envelope))
                {
                    try
                    {
                        Process(peer, envelope.Payload);
                    }
                    catch
                    {
                        m_InvalidInputCount = checked(m_InvalidInputCount + 1);
                        throw;
                    }
                }
            }
            TryLockRoster();
            AssembleDueBundles();
            FlushCanonicalConfirmation();
            foreach (PeerState peer in m_Peers.Values)
                peer.Channel.Pump();
            m_Endpoint.PumpSend();
        }

        void Process(PeerState peer, IRollbackProtocolPayload payload)
        {
            if (!peer.HandshakeAccepted)
            {
                if (payload is not RollbackHandshake handshake ||
                    !string.Equals(handshake.PeerId, peer.Roster.PeerId, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("Rollback peer must begin with its own handshake.");
                }
                m_RelayHandshake.RequireCompatible(handshake);
                peer.HandshakeAccepted = true;
                peer.Channel.Send(m_RelayHandshake, true);
                return;
            }
            if (!IsRosterLocked)
                throw new InvalidOperationException("Rollback peer sent gameplay data before the roster was locked.");
            switch (payload)
            {
                case RollbackActorInputBatch input:
                    try
                    {
                        ReceiveInput(peer, input);
                    }
                    catch (Exception exception)
                    {
                        Broadcast(
                            new RollbackLeave(m_RelayHandshake.PeerId, $"input_protocol_conflict:{exception.Message}"),
                            true,
                            null);
                        throw;
                    }
                    break;
                case RollbackStateHashReport report:
                    if (!string.Equals(report.PeerId, peer.Roster.PeerId, StringComparison.Ordinal))
                        throw new InvalidOperationException("Rollback state hash report PeerId does not match its UDP channel.");
                    m_HashReportCount = checked(m_HashReportCount + 1);
                    Broadcast(report, false, peer.Roster.PeerId);
                    break;
                case RollbackSnapshotRequest request:
                    if (!string.Equals(request.RequesterPeerId, peer.Roster.PeerId, StringComparison.Ordinal) ||
                        !m_ExpectedPeers.ContainsKey(request.AuthorityPeerId))
                    {
                        throw new InvalidOperationException("Rollback snapshot request routing identity is invalid.");
                    }
                    Broadcast(request, true, peer.Roster.PeerId);
                    break;
                case RollbackSnapshotResponse response:
                    if (!string.Equals(response.AuthorityPeerId, peer.Roster.PeerId, StringComparison.Ordinal) ||
                        !m_ExpectedPeers.ContainsKey(response.RequesterPeerId))
                    {
                        throw new InvalidOperationException("Rollback snapshot response routing identity is invalid.");
                    }
                    Broadcast(response, true, peer.Roster.PeerId);
                    break;
                case RollbackLeave leave when string.Equals(leave.PeerId, peer.Roster.PeerId, StringComparison.Ordinal):
                    throw new InvalidOperationException($"Rollback Peer '{leave.PeerId}' left: {leave.Reason}");
                default:
                    throw new InvalidOperationException($"Rollback payload '{payload.Kind}' is not valid for the input Relay.");
            }
        }

        void ReceiveInput(PeerState peer, RollbackActorInputBatch input)
        {
            if (input == null || input.Frames.Count > m_InputRedundancyCount ||
                !input.ActorId.Equals(peer.Roster.ActorId))
            {
                throw new InvalidOperationException("Rollback peer input ownership or provenance is invalid.");
            }
            for (int i = 0; i < input.Frames.Count; i++)
            {
                RollbackActorInputFrame frame = input.Frames[i];
                if (frame.Provenance != RollbackInputProvenance.LocalExplicit ||
                    !frame.ActorId.Equals(peer.Roster.ActorId))
                    throw new InvalidOperationException("Rollback peer input frame ownership or provenance is invalid.");
            }
            m_InputBatchCount = checked(m_InputBatchCount + 1);
            IReadOnlyList<RollbackActorInputFrame> accepted = m_Assembler.SubmitBatch(input.Frames);
            m_DeduplicatedInputCount = checked(
                m_DeduplicatedInputCount + (ulong)(input.Frames.Count - accepted.Count));
            if (accepted.Count == 0)
                return;
            var relayed = new RollbackActorInputFrame[accepted.Count];
            for (int i = 0; i < accepted.Count; i++)
            {
                RollbackActorInputFrame source = accepted[i];
                relayed[i] = new RollbackActorInputFrame(
                    source.ActorId,
                    source.Tick,
                    source.InputSequence,
                    source.Input,
                    RollbackInputProvenance.RelayedExplicit);
            }
            Broadcast(new RollbackRelayedExplicitInputBatch(relayed), true, peer.Roster.PeerId);
            m_ExplicitRelayBroadcastCount = checked(m_ExplicitRelayBroadcastCount + (ulong)relayed.Length);
        }

        void FlushCanonicalConfirmation()
        {
            if (!IsRosterLocked || m_Assembler.ConfirmedTick <= m_LastConfirmedBroadcastTick)
                return;
            ulong confirmedTick = m_Assembler.ConfirmedTick;
            IReadOnlyList<RollbackCanonicalInputBundle> bundles = m_Assembler.CaptureCanonicalRange(
                m_LastConfirmedBroadcastTick,
                confirmedTick);
            Broadcast(
                new RollbackCanonicalConfirmation(
                    m_LastConfirmedBroadcastTick,
                    new SimulationTick(confirmedTick),
                    bundles),
                true,
                null);
            m_LastConfirmedBroadcastTick = confirmedTick;
            m_ConfirmationBroadcastCount = checked(m_ConfirmationBroadcastCount + 1);
        }

        void TryLockRoster()
        {
            if (IsRosterLocked || m_Peers.Count != m_ExpectedPeers.Count)
                return;
            foreach (PeerState peer in m_Peers.Values)
            {
                if (!peer.HandshakeAccepted)
                    return;
            }
            m_Assembler = new RollbackCanonicalInputAssembler(
                m_Roster,
                m_Policy,
                new SimulationTick(1),
                "deterministic-rollback-input-relay",
                m_RelayHandshake.PeerId);
            Broadcast(m_Roster, true, null);
        }

        void AssembleDueBundles()
        {
            if (!IsRosterLocked)
                return;
            while (m_Assembler.HasExplicitInputForEveryActor(m_Assembler.NextTick))
                BroadcastAssembledBundle();
        }

        void BroadcastAssembledBundle()
        {
            RollbackCanonicalInputBundle bundle = m_Assembler.AssembleNext();
            Broadcast(bundle, true, null);
            m_AssembledCount = checked(m_AssembledCount + 1);
        }

        void Broadcast(IRollbackProtocolPayload payload, bool reliable, string excludedPeerId)
        {
            foreach (PeerState peer in m_Peers.Values)
            {
                if (excludedPeerId != null && string.Equals(peer.Roster.PeerId, excludedPeerId, StringComparison.Ordinal))
                    continue;
                peer.Channel.Send(payload, reliable);
            }
        }

        void ThrowIfDisposed()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(RollbackInputRelayRuntime));
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            m_Endpoint.Dispose();
            m_Peers.Clear();
            m_Assembler = null;
        }
    }
}
