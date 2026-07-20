using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using ThirdPersonSimulation;

namespace ThirdPersonSimulation.ServerAuthoritative.Transport
{
    public sealed class ServerAuthoritativeAuthoritySourceRuntime : IDisposable, IFloat32SourceEgressOutputPort
    {
        readonly ServerAuthoritativeAuthorityHostIdentity m_Host;
        readonly ServerAuthoritativeAuthoritySourcePolicy m_Policy;
        readonly IServerAuthoritativeAuthorityControlTransport m_Control;
        readonly IServerAuthoritativeAuthorityDataTransport m_Data;
        readonly ISimulationDiagnosticsSink m_Diagnostics;
        readonly NetworkCheckpointLayout m_CheckpointLayout;
        readonly ReadOnlyCollection<ActorId> m_ExpectedActors;
        readonly List<ServerAuthoritativeRosterEntry> m_Roster = new List<ServerAuthoritativeRosterEntry>();
        readonly SortedDictionary<ActorId, ServerAuthoritativeAuthorityClientRoute> m_Routes =
            new SortedDictionary<ActorId, ServerAuthoritativeAuthorityClientRoute>();
        readonly Dictionary<ActorId, NetworkCheckpoint> m_LatestCheckpoints =
            new Dictionary<ActorId, NetworkCheckpoint>();
        readonly Queue<ServerAuthoritativeAuthorityReliableEventBatchOutput> m_ReliableOutput =
            new Queue<ServerAuthoritativeAuthorityReliableEventBatchOutput>();
        readonly Queue<ServerAuthoritativeAuthorityFullCheckpointOutput> m_FullCheckpointOutput =
            new Queue<ServerAuthoritativeAuthorityFullCheckpointOutput>();
        ServerAuthoritativeSessionId m_SessionId;
        ulong m_RosterRevision;
        ulong m_LatestAuthorityTick;
        ulong m_LastSourceTick;
        ulong m_LastEvidenceAuthorityTick;
        ulong m_LastHeartbeatAckSequence;
        bool m_RegistrationAccepted;
        bool m_RosterLocked;
        bool m_FullBaselineRequested;
        bool m_Disposed;

        public ServerAuthoritativeAuthoritySourceRuntime(
            SimulationSessionSourceDescriptor descriptor,
            ServerAuthoritativeAuthoritySourcePolicy policy,
            ServerAuthoritativeAuthorityHostIdentity host,
            IEnumerable<ActorId> expectedActors,
            CharacterSimulationProgram program,
            IServerAuthoritativeAuthorityControlTransport control,
            IServerAuthoritativeAuthorityDataTransport data,
            ISimulationDiagnosticsSink diagnostics)
        {
            Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
            m_Policy = policy ?? throw new ArgumentNullException(nameof(policy));
            if (!host.IsValid)
                throw new ArgumentException("Authority Source Host identity is invalid.", nameof(host));
            m_Host = host;
            m_Control = control ?? throw new ArgumentNullException(nameof(control));
            m_Data = data ?? throw new ArgumentNullException(nameof(data));
            m_Diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
            m_CheckpointLayout = new NetworkCheckpointLayout(program ?? throw new ArgumentNullException(nameof(program)));
            var actors = expectedActors == null
                ? new List<ActorId>()
                : new List<ActorId>(expectedActors);
            actors.Sort();
            if (actors.Count == 0)
                throw new ArgumentException("Authority Source expected Actor roster is empty.", nameof(expectedActors));
            for (int i = 0; i < actors.Count; i++)
            {
                if (!actors[i].IsValid || i > 0 && actors[i - 1] == actors[i])
                    throw new ArgumentException("Authority Source expected roster contains an invalid or duplicate ActorId.", nameof(expectedActors));
            }
            m_ExpectedActors = actors.AsReadOnly();
            var accepted = new AcceptedInputPort(this);
            var clock = new AuthorityClockPort(this);
            var baseline = new FullBaselineRequestPort(this);
            var send = new AuthoritySendPort(this);
            RuntimePorts = new SimulationRuntimePortSet(new ISimulationRuntimePort[]
            {
                accepted,
                clock,
                baseline,
                send
            });
            SourceEgress = send;
        }

        public SimulationSessionSourceDescriptor Descriptor { get; }
        public ServerAuthoritativeAuthoritySourcePolicy Policy => m_Policy;
        public SimulationRuntimePortSet RuntimePorts { get; }
        public IFloat32SourceEgressOutputPort SourceEgress { get; }
        public NetworkCheckpointLayout CheckpointLayout => m_CheckpointLayout;
        public IReadOnlyList<ServerAuthoritativeRosterEntry> Roster => m_Roster;
        public ulong LatestAuthorityTick => m_LatestAuthorityTick;
        public bool IsReady
        {
            get
            {
                PumpTransport();
                if (!m_RegistrationAccepted || !m_RosterLocked)
                    return false;
                foreach (ServerAuthoritativeAuthorityClientRoute route in m_Routes.Values)
                {
                    if (!route.DataPlaneReady || !route.HasInput)
                        return false;
                }
                return true;
            }
        }

        public void Step(SimulationTickSourceIdentity source)
        {
            ThrowIfDisposed();
            RequireAuthoritySource(source);
            if (m_LastSourceTick != 0 && source.SourceTick < m_LastSourceTick)
                throw new InvalidOperationException("Authority Source outer Tick regressed.");
            m_LastSourceTick = source.SourceTick;
            m_Control.Step(source);
            PumpTransport();
        }

        public void PumpTransport()
        {
            ThrowIfDisposed();
            RequireControlAvailable();
            PumpControl();
            m_Data.ThrowIfUnavailable();
            while (m_Data.TryReceive(out ServerAuthoritativeReceivedDatagram received))
                ReceiveDatagram(received);
            m_Data.PumpSend();
            FlushControlOutputs();
        }

        public AcceptedAuthorityInputBatch ReadAcceptedInputs(SimulationTickSourceIdentity source)
        {
            RequireAuthoritySource(source);
            Step(source);
            foreach (ServerAuthoritativeAuthorityClientRoute route in m_Routes.Values)
            {
                if (route.DataPlaneReady && route.LastCommandSourceTick != 0 &&
                    source.SourceTick > route.LastCommandSourceTick + (ulong)m_Policy.CommandLivenessTimeoutTicks)
                {
                    Fail(
                        "server_authoritative_command_liveness_failed",
                        $"Authority received no command for Actor '{route.Roster.ActorId}' during '{source.SourceTick - route.LastCommandSourceTick}' source ticks.");
                }
            }
            ulong authorityTick = checked(m_LatestAuthorityTick + 1);
            var values = new List<AcceptedAuthorityInput>(m_Routes.Count);
            foreach (ServerAuthoritativeAuthorityClientRoute route in m_Routes.Values)
                values.Add(route.Select(authorityTick, m_Policy.ModelPolicy.MaximumInputLagTicks));
            return new AcceptedAuthorityInputBatch(new SimulationTick(authorityTick), values);
        }

        public SimulationTick ReadAuthorityTick(SimulationTickSourceIdentity source)
        {
            RequireAuthoritySource(source);
            RequireControlAvailable();
            return new SimulationTick(checked(m_LatestAuthorityTick + 1));
        }

        public bool IsFullBaselineRequested
        {
            get
            {
                ThrowIfDisposed();
                PumpTransport();
                return m_FullBaselineRequested || HasPendingCheckpointRequest();
            }
        }

        public void Commit(Float32SourceEgressRecord record)
        {
            ThrowIfDisposed();
            PumpTransport();
            if (record == null ||
                !string.Equals(record.ChannelId, ServerAuthoritativeEgressChannels.AuthorityReplication, StringComparison.Ordinal) ||
                !string.Equals(record.SchemaId, ServerAuthoritativeEgressChannels.AuthorityReplicationSchema, StringComparison.Ordinal) ||
                record.SchemaVersion != ServerAuthoritativeEgressChannels.AuthorityReplicationSchemaVersion)
            {
                throw new InvalidOperationException("Authority Source accepts only canonical AuthorityReplication egress.");
            }
            AuthorityReplicationBatch batch = ServerAuthoritativeEgressCodec.ReadAuthorityReplication(record.CopyPayload());
            if (batch.AuthorityTick.Value != checked(m_LatestAuthorityTick + 1))
                throw new InvalidOperationException("Authority replication Tick is not contiguous with the Authority Source clock.");
            m_LatestAuthorityTick = batch.AuthorityTick.Value;
            CaptureCheckpoints(batch);
            QueueReliableEvents(batch);
            WriteAuthorityEvidence(batch);
            ulong interval = checked((ulong)(m_Policy.ModelPolicy.SimulationTickRate / m_Policy.ModelPolicy.SnapshotPacketRate));
            if (batch.AuthorityTick.Value == 1 || batch.AuthorityTick.Value % interval == 0)
                SendSnapshots(batch);
            m_FullBaselineRequested = HasPendingCheckpointRequest();
            FlushControlOutputs();
            m_Data.PumpSend();
        }

        void PumpControl()
        {
            RequireControlAvailable();
            while (m_Control.TryTakeRegistration(out ServerAuthoritativeAuthorityRegistrationResult registration))
            {
                if (!registration.Host.Equals(m_Host))
                    Fail("authority_registration_identity_mismatch", "Authority registration result targets another Host.");
                if (m_RegistrationAccepted && !m_SessionId.Equals(registration.SessionId))
                    Fail("authority_registration_changed", "Authority registration SessionId changed while active.");
                m_RegistrationAccepted = true;
                m_SessionId = registration.SessionId;
            }
            while (m_Control.TryTakeRoster(out ServerAuthoritativeAuthorityRosterLock roster))
            {
                if (!roster.Host.Equals(m_Host) ||
                    m_RegistrationAccepted && !roster.SessionId.Equals(m_SessionId))
                {
                    Fail("authority_roster_identity_mismatch", "Authority roster lock targets another Host or Session.");
                }
                if (roster.Revision < m_RosterRevision)
                    continue;
                if (roster.Roster.Count != m_ExpectedActors.Count)
                    Fail("authority_roster_count_mismatch", "Authority roster lock does not match the expected Actor count.");
                for (int i = 0; i < m_ExpectedActors.Count; i++)
                {
                    if (roster.Roster[i].ActorId != m_ExpectedActors[i])
                        Fail("authority_roster_route_mismatch", "Authority roster lock does not match the expected Actor routes.");
                }
                if (!m_RosterLocked)
                {
                    for (int i = 0; i < roster.Roster.Count; i++)
                    {
                        ServerAuthoritativeRosterEntry entry = roster.Roster[i];
                        m_Roster.Add(entry);
                        m_Routes.Add(entry.ActorId, new ServerAuthoritativeAuthorityClientRoute(entry, m_Policy.CommandQueueCapacity));
                    }
                }
                else
                {
                    for (int i = 0; i < m_Roster.Count; i++)
                    {
                        if (!m_Roster[i].Equals(roster.Roster[i]))
                            Fail("authority_roster_changed", "Authority roster changed after it was locked.");
                    }
                }
                m_RosterRevision = roster.Revision;
                m_RosterLocked = true;
            }
            while (m_Control.TryTakeTicket(out ServerAuthoritativeAuthorityDataPlaneTicket ticket))
                AcceptTicket(ticket);
            while (m_Control.TryTakeHeartbeatAck(out ServerAuthoritativeAuthorityHeartbeatAck heartbeat))
            {
                if (heartbeat.Sequence > m_LastHeartbeatAckSequence)
                    m_LastHeartbeatAckSequence = heartbeat.Sequence;
            }
            while (m_Control.TryTakeFullCheckpointRequest(out ServerAuthoritativeAuthorityFullCheckpointRequest request))
            {
                if (!m_Routes.TryGetValue(request.ActorId, out ServerAuthoritativeAuthorityClientRoute route) ||
                    !route.Roster.PlayerId.Equals(request.PlayerId))
                {
                    Fail("authority_full_checkpoint_route_unknown", "Full checkpoint request targets an unknown Authority route.");
                }
                route.RequestFullCheckpoint(request.RequestSequence);
                m_FullBaselineRequested = true;
            }
        }

        void AcceptTicket(ServerAuthoritativeAuthorityDataPlaneTicket ticket)
        {
            if (!ticket.Host.Equals(m_Host) ||
                !m_RegistrationAccepted || !ticket.SessionId.Equals(m_SessionId) ||
                ticket.ExpiresAtUnixMilliseconds <= DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
            {
                Fail("authority_data_ticket_invalid", "Authority received an invalid or expired data-plane ticket.");
            }
            if (!m_Routes.TryGetValue(ticket.ActorId, out ServerAuthoritativeAuthorityClientRoute route) ||
                !route.Roster.PlayerId.Equals(ticket.PlayerId))
            {
                Fail("authority_data_ticket_route_unknown", "Authority data-plane ticket targets an Actor outside the locked roster.");
            }
            route.SetTicket(
                ticket,
                new ServerAuthoritativeDatagramIdentity(
                    m_Host.RoomId,
                    m_SessionId,
                    route.Roster.PlayerId,
                    route.Roster.ActorId));
        }

        void ReceiveDatagram(ServerAuthoritativeReceivedDatagram received)
        {
            ServerAuthoritativeDatagramPacket packet = received.Packet;
            if (!m_Routes.TryGetValue(packet.Header.Identity.ActorId, out ServerAuthoritativeAuthorityClientRoute route) ||
                !route.Identity.Equals(packet.Header.Identity))
            {
                return;
            }
            if (packet.Header.Kind == ServerAuthoritativeDatagramKind.DataPlaneHello)
            {
                ReceiveHello(route, received);
                return;
            }
            if (!route.DataPlaneReady || !route.AcceptPacketSequence(packet.Header.PacketSequence))
                return;
            if (packet.Header.Kind != ServerAuthoritativeDatagramKind.Command)
                Fail("authority_datagram_kind_invalid", $"Authority received unexpected gameplay datagram '{packet.Header.Kind}'.");
            byte[] payload = packet.CopyPayload();
            CommandDatagram command = ServerAuthoritativeDatagramPayloadCodec.ReadCommand(payload);
            route.RecordCommand(payload.Length, command.SourceTick);
            ReceiveCommand(route, command);
        }

        void ReceiveHello(
            ServerAuthoritativeAuthorityClientRoute route,
            ServerAuthoritativeReceivedDatagram received)
        {
            if (route.Ticket == null ||
                route.Ticket.ExpiresAtUnixMilliseconds <= DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
            {
                Fail("authority_data_hello_without_ticket", "Authority received Hello without a live ticket.");
            }
            DataPlaneHello hello = ServerAuthoritativeDatagramPayloadCodec.ReadHello(received.Packet.CopyPayload());
            if (!string.Equals(hello.TicketId, route.Ticket.TicketId, StringComparison.Ordinal) ||
                !string.Equals(hello.Nonce, route.Ticket.Nonce, StringComparison.Ordinal))
            {
                Fail("authority_data_hello_ticket_mismatch", "Authority received Hello with a mismatched ticket or nonce.");
            }
            m_Data.BindRemote(route.Identity, received.RemoteEndPoint);
            route.DataPlaneReady = true;
            route.AcceptHelloSequence(received.Packet.Header.PacketSequence);
            byte[] payload = ServerAuthoritativeDatagramPayloadCodec.Write(
                new DataPlaneHelloAck(m_LatestAuthorityTick, hello.ClientClockMicros, ClockMicros()));
            SendPacket(route, ServerAuthoritativeDatagramKind.DataPlaneHelloAck, payload);
            if (!route.TicketConsumptionReported)
            {
                route.TicketConsumptionReported = true;
                m_Control.SendTicketConsumed(route.Ticket);
            }
        }

        void ReceiveCommand(ServerAuthoritativeAuthorityClientRoute route, CommandDatagram command)
        {
            route.AcknowledgeSnapshot(command.LatestSnapshotSequence, command.LatestBaseSnapshotSequence);
            for (int i = command.Samples.Count - 1; i >= 0; i--)
            {
                CanonicalInputSample sample = command.Samples[i];
                if (sample.TargetAuthorityTick + (ulong)m_Policy.ModelPolicy.MaximumInputLagTicks < m_LatestAuthorityTick)
                    continue;
                if (sample.TargetAuthorityTick > m_LatestAuthorityTick + (ulong)m_Policy.ModelPolicy.MaximumInputLeadTicks + 1)
                {
                    Fail(
                        "authority_command_lead_exceeded",
                        $"Command target Tick '{sample.TargetAuthorityTick}' exceeds the authority lead window at '{m_LatestAuthorityTick}'.");
                }
                route.RecordCommandLead(sample.TargetAuthorityTick, m_LatestAuthorityTick);
                route.Enqueue(sample);
            }
        }

        void CaptureCheckpoints(AuthorityReplicationBatch batch)
        {
            for (int i = 0; i < batch.Baselines.Count; i++)
            {
                AuthoritativeActorBaseline baseline = batch.Baselines[i];
                m_LatestCheckpoints[baseline.ActorId] = NetworkCheckpointCodec.Capture(m_CheckpointLayout, baseline);
            }
        }

        void SendSnapshots(AuthorityReplicationBatch batch)
        {
            foreach (ServerAuthoritativeAuthorityClientRoute route in m_Routes.Values)
            {
                if (!m_LatestCheckpoints.TryGetValue(route.Roster.ActorId, out NetworkCheckpoint target) ||
                    target.Baseline.AuthorityTick != batch.AuthorityTick)
                {
                    m_FullBaselineRequested = true;
                    continue;
                }
                RemotePresentationBatch remote = FindRemote(batch, route.Roster.ActorId);
                if (route.PendingCheckpointRequest != 0)
                {
                    QueueFullCheckpoint(route, target, route.PendingCheckpointRequest);
                    route.PendingCheckpointRequest = 0;
                    continue;
                }
                if (route.AcknowledgedCheckpoint == null)
                {
                    QueueFullCheckpoint(route, target, 0);
                    continue;
                }
                ulong sequence = route.NextSnapshotSequence();
                byte[] delta = NetworkCheckpointCodec.WriteDelta(m_CheckpointLayout, route.AcknowledgedCheckpoint, target, remote);
                var snapshot = new SnapshotDatagram(
                    sequence,
                    route.AcknowledgedSnapshotSequence,
                    batch.AuthorityTick.Value,
                    FindAck(batch, route.Roster.ActorId).ConfirmedInputSequence,
                    target.Baseline.ConfirmedEventHorizon.Sequence,
                    delta);
                byte[] payload = ServerAuthoritativeDatagramPayloadCodec.Write(snapshot);
                ServerAuthoritativeDatagramPacket packet = Packet(route, ServerAuthoritativeDatagramKind.Snapshot, payload);
                try
                {
                    _ = ServerAuthoritativeGameplayDatagramCodec.Write(packet, m_Policy.ModelPolicy.MaxGameplayDatagramBytes);
                    m_Data.EnqueueSend(packet);
                    route.StoreSent(sequence, target);
                    route.RecordDeltaSnapshot(payload.Length);
                    Publish(
                        SimulationModelTraceKind.Transport,
                        "authority_snapshot_queued",
                        $"actor={route.Roster.ActorId};bytes={payload.Length};base={route.AcknowledgedSnapshotSequence};target={sequence}",
                        route.Roster.ActorId,
                        batch.AuthorityTick.Value,
                        FindAck(batch, route.Roster.ActorId).ConfirmedInputSequence,
                        route.AcknowledgedSnapshotSequence,
                        m_Data.SendQueueDepth,
                        true,
                        sequence);
                }
                catch (InvalidDataException)
                {
                    route.RecordDeltaMtuExceeded(payload.Length);
                    Publish(
                        SimulationModelTraceKind.Transport,
                        "server_authoritative_delta_mtu_exceeded",
                        $"actor={route.Roster.ActorId};deltaBytes={payload.Length};mtu={m_Policy.ModelPolicy.MaxGameplayDatagramBytes};base={route.AcknowledgedSnapshotSequence};target={sequence}",
                        route.Roster.ActorId,
                        batch.AuthorityTick.Value,
                        FindAck(batch, route.Roster.ActorId).ConfirmedInputSequence,
                        route.AcknowledgedSnapshotSequence,
                        m_Data.SendQueueDepth,
                        false);
                    QueueFullCheckpoint(route, target, 0, sequence);
                }
            }
        }

        void QueueFullCheckpoint(
            ServerAuthoritativeAuthorityClientRoute route,
            NetworkCheckpoint checkpoint,
            ulong requestSequence,
            ulong reservedSequence = 0)
        {
            if (m_FullCheckpointOutput.Count >= m_Policy.FullCheckpointOutputQueueCapacity)
                Fail("authority_full_checkpoint_queue_overflow", "Authority full checkpoint output queue overflowed.");
            ulong snapshotSequence = reservedSequence == 0 ? route.NextSnapshotSequence() : reservedSequence;
            byte[] payload = NetworkCheckpointCodec.WriteFull(m_CheckpointLayout, checkpoint);
            m_FullCheckpointOutput.Enqueue(new ServerAuthoritativeAuthorityFullCheckpointOutput(
                route.Roster.PlayerId,
                route.Roster.ActorId,
                requestSequence,
                snapshotSequence,
                checkpoint,
                payload));
            route.StoreSent(snapshotSequence, checkpoint);
            route.RecordFullCheckpoint(payload.Length);
            Publish(
                SimulationModelTraceKind.Transport,
                "authority_full_checkpoint_queued",
                $"actor={route.Roster.ActorId};bytes={payload.Length};target={snapshotSequence};request={requestSequence}",
                route.Roster.ActorId,
                checkpoint.Baseline.AuthorityTick.Value,
                checkpoint.Baseline.ConfirmedInputSequence,
                route.AcknowledgedSnapshotSequence,
                m_FullCheckpointOutput.Count,
                true,
                snapshotSequence);
        }

        void QueueReliableEvents(AuthorityReplicationBatch batch)
        {
            for (int sourceIndex = 0; sourceIndex < batch.RemotePresentation.Count; sourceIndex++)
            {
                RemotePresentationBatch source = batch.RemotePresentation[sourceIndex];
                if (source.ReliableEvents.Count == 0)
                    continue;
                ServerAuthoritativeAuthorityClientRoute recipient = null;
                foreach (ServerAuthoritativeAuthorityClientRoute route in m_Routes.Values)
                {
                    if (route.Roster.ActorId != source.ActorId)
                        recipient = recipient == null ? route : throw new InvalidOperationException("Authority has more than one remote event recipient.");
                }
                if (recipient == null)
                    throw new InvalidOperationException("Authority reliable event has no remote recipient.");
                if (m_ReliableOutput.Count >= m_Policy.ReliableOutputQueueCapacity)
                    Fail("authority_reliable_output_queue_overflow", "Authority reliable event output queue overflowed.");
                var events = new ServerAuthoritativeAuthorityReliableEventOutput[source.ReliableEvents.Count];
                for (int i = 0; i < events.Length; i++)
                {
                    ServerAuthoritativeReliableEvent reliable = source.ReliableEvents[i];
                    byte[] payload = ServerAuthoritativeEgressCodec.WriteRemotePresentation(new RemotePresentationBatch(
                        source.ActorId,
                        Array.Empty<CharacterBodySample>(),
                        Array.Empty<PresentationCommand>(),
                        new[] { reliable },
                        false));
                    events[i] = new ServerAuthoritativeAuthorityReliableEventOutput(
                        recipient.Roster.ActorId,
                        source.ActorId,
                        reliable,
                        payload);
                }
                m_ReliableOutput.Enqueue(new ServerAuthoritativeAuthorityReliableEventBatchOutput(
                    recipient.Roster.ActorId,
                    source.ActorId,
                    events));
            }
        }

        void FlushControlOutputs()
        {
            while (m_ReliableOutput.Count != 0)
                m_Control.SendReliableEvents(m_ReliableOutput.Dequeue());
            while (m_FullCheckpointOutput.Count != 0)
                m_Control.SendFullCheckpoint(m_FullCheckpointOutput.Dequeue());
        }

        void SendPacket(
            ServerAuthoritativeAuthorityClientRoute route,
            ServerAuthoritativeDatagramKind kind,
            byte[] payload)
        {
            m_Data.EnqueueSend(Packet(route, kind, payload));
        }

        static ServerAuthoritativeDatagramPacket Packet(
            ServerAuthoritativeAuthorityClientRoute route,
            ServerAuthoritativeDatagramKind kind,
            byte[] payload)
        {
            var header = new ServerAuthoritativeDatagramHeader(
                route.Identity,
                kind,
                route.NextSendPacketSequence(),
                payload.Length);
            return new ServerAuthoritativeDatagramPacket(header, payload);
        }

        bool HasPendingCheckpointRequest()
        {
            foreach (ServerAuthoritativeAuthorityClientRoute route in m_Routes.Values)
            {
                if (route.PendingCheckpointRequest != 0)
                    return true;
            }
            return false;
        }

        static AuthoritativeInputAck FindAck(AuthorityReplicationBatch batch, ActorId actorId)
        {
            for (int i = 0; i < batch.Acks.Count; i++)
            {
                if (batch.Acks[i].ActorId == actorId)
                    return batch.Acks[i];
            }
            throw new InvalidOperationException($"Authority replication has no ack for Actor '{actorId}'.");
        }

        static RemotePresentationBatch FindRemote(AuthorityReplicationBatch batch, ActorId owner)
        {
            RemotePresentationBatch remote = null;
            for (int i = 0; i < batch.RemotePresentation.Count; i++)
            {
                if (batch.RemotePresentation[i].ActorId == owner)
                    continue;
                remote = remote == null
                    ? batch.RemotePresentation[i]
                    : throw new InvalidOperationException("Authority replication has more than one remote Actor.");
            }
            return remote ?? throw new InvalidOperationException("Authority replication has no remote Actor.");
        }

        void WriteAuthorityEvidence(AuthorityReplicationBatch batch)
        {
            if (!m_Diagnostics.IsEnabled)
                return;
            ulong interval = checked((ulong)m_Policy.ModelPolicy.SimulationTickRate * 5UL);
            if (batch.AuthorityTick.Value != 1 && batch.AuthorityTick.Value % interval != 0)
                return;
            float elapsedSeconds = m_LastEvidenceAuthorityTick == 0
                ? Math.Max(1f, batch.AuthorityTick.Value / (float)m_Policy.ModelPolicy.SimulationTickRate)
                : (batch.AuthorityTick.Value - m_LastEvidenceAuthorityTick) / (float)m_Policy.ModelPolicy.SimulationTickRate;
            m_LastEvidenceAuthorityTick = batch.AuthorityTick.Value;
            var routes = new List<string>(m_Routes.Count);
            foreach (ServerAuthoritativeAuthorityClientRoute route in m_Routes.Values)
                routes.Add(route.DescribeMetrics(elapsedSeconds));
            Publish(
                SimulationModelTraceKind.Transport,
                "server_authoritative_authority_stream_metrics",
                $"tick={batch.AuthorityTick.Value};routes={string.Join("|", routes)}",
                default,
                batch.AuthorityTick.Value,
                0,
                m_LastHeartbeatAckSequence,
                m_Data.ReceiveQueueDepth + m_Data.SendQueueDepth,
                true);
        }

        void RequireAuthoritySource(SimulationTickSourceIdentity source)
        {
            if (source.Kind != SimulationTickSourceKind.Authoritative || source.SourceTick == 0)
                throw new InvalidOperationException("Authority Source requires an Authoritative tick source.");
        }

        void RequireControlAvailable()
        {
            if (m_Control.ControlStatus != ServerAuthoritativeAuthorityControlTransportStatus.Failed)
                return;
            ServerAuthoritativeAuthorityControlFailure failure = m_Control.ControlFailure;
            throw new InvalidOperationException(
                failure == null
                    ? "Authority control transport failed without diagnostics."
                    : $"{failure.Code}: {failure.Message}");
        }

        void Fail(string code, string message)
        {
            m_Control.SendFailure(code, message);
            Publish(SimulationModelTraceKind.Failure, code, message, default, m_LatestAuthorityTick, 0, 0, 0, false);
            throw new InvalidOperationException($"{code}: {message}");
        }

        void Publish(
            SimulationModelTraceKind kind,
            string code,
            string detail,
            ActorId actorId,
            ulong authorityTick,
            ulong inputSequence,
            ulong ackSequence,
            int queueDepth,
            bool success,
            ulong snapshotSequence = 0)
        {
            if (!m_Diagnostics.IsEnabled)
                return;
            m_Diagnostics.PublishModel(new SimulationModelTraceRecord(
                kind,
                code,
                detail,
                actorId,
                m_LastSourceTick,
                authorityTick,
                inputSequence,
                ackSequence,
                queueDepth,
                0,
                0f,
                0f,
                success,
                snapshotSequence));
        }

        void ThrowIfDisposed()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(ServerAuthoritativeAuthoritySourceRuntime));
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            var failures = new List<Exception>();
            try
            {
                m_Control.SendLeave("authority_source_disposed");
            }
            catch (Exception exception)
            {
                failures.Add(exception);
            }
            try
            {
                m_Control.Dispose();
            }
            catch (Exception exception)
            {
                failures.Add(exception);
            }
            if (!ReferenceEquals(m_Control, m_Data))
            {
                try
                {
                    m_Data.Dispose();
                }
                catch (Exception exception)
                {
                    failures.Add(exception);
                }
            }
            m_ReliableOutput.Clear();
            m_FullCheckpointOutput.Clear();
            m_Routes.Clear();
            m_Roster.Clear();
            m_LatestCheckpoints.Clear();
            if (failures.Count != 0)
                throw new AggregateException("Authority Source failed to release completely.", failures);
        }

        static long ClockMicros() => checked(Stopwatch.GetTimestamp() * 1000000L / Stopwatch.Frequency);

        sealed class AcceptedInputPort : IServerAuthoritativeAcceptedInputSourcePort
        {
            readonly ServerAuthoritativeAuthoritySourceRuntime m_Runtime;

            public AcceptedInputPort(ServerAuthoritativeAuthoritySourceRuntime runtime)
            {
                m_Runtime = runtime;
                Descriptor = SimulationPortDescriptor.CreateSource(
                    ServerAuthoritativeSourcePortContracts.AcceptedInput,
                    runtime.Descriptor.Identity);
            }

            public SimulationPortDescriptor Descriptor { get; }
            public AcceptedAuthorityInputBatch Read(SimulationTickSourceIdentity source) => m_Runtime.ReadAcceptedInputs(source);
        }

        sealed class AuthorityClockPort : IServerAuthoritativeAuthorityClockSourcePort
        {
            readonly ServerAuthoritativeAuthoritySourceRuntime m_Runtime;

            public AuthorityClockPort(ServerAuthoritativeAuthoritySourceRuntime runtime)
            {
                m_Runtime = runtime;
                Descriptor = SimulationPortDescriptor.CreateSource(
                    ServerAuthoritativeSourcePortContracts.AuthorityClock,
                    runtime.Descriptor.Identity);
            }

            public SimulationPortDescriptor Descriptor { get; }
            public SimulationTick ReadAuthorityTick(SimulationTickSourceIdentity source) => m_Runtime.ReadAuthorityTick(source);
        }

        sealed class FullBaselineRequestPort : IServerAuthoritativeFullBaselineRequestSourcePort
        {
            readonly ServerAuthoritativeAuthoritySourceRuntime m_Runtime;

            public FullBaselineRequestPort(ServerAuthoritativeAuthoritySourceRuntime runtime)
            {
                m_Runtime = runtime;
                Descriptor = SimulationPortDescriptor.CreateSource(
                    ServerAuthoritativeSourcePortContracts.FullBaselineRequest,
                    runtime.Descriptor.Identity);
            }

            public SimulationPortDescriptor Descriptor { get; }
            public bool IsRequested => m_Runtime.IsFullBaselineRequested;
        }

        sealed class AuthoritySendPort : IServerAuthoritativeNetworkSendPort
        {
            readonly ServerAuthoritativeAuthoritySourceRuntime m_Runtime;

            public AuthoritySendPort(ServerAuthoritativeAuthoritySourceRuntime runtime)
            {
                m_Runtime = runtime;
                Descriptor = SimulationPortDescriptor.CreateSource(
                    ServerAuthoritativeSourcePortContracts.AuthoritySend,
                    runtime.Descriptor.Identity);
            }

            public SimulationPortDescriptor Descriptor { get; }
            public void Commit(Float32SourceEgressRecord record) => m_Runtime.Commit(record);
        }
    }
}
