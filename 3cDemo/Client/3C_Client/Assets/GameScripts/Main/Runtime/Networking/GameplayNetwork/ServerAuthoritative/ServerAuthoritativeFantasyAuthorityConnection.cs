using System;
using System.Collections.Generic;
using System.Net;
using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using ThirdPersonSimulation;
using ThirdPersonSimulation.ServerAuthoritative;
using ThirdPersonSimulation.UnityAuthority;
using ThirdPersonSimulation.ServerAuthoritative.Transport;

namespace ThirdPersonGameplay.Networking.ServerAuthoritative
{
    internal sealed class ServerAuthoritativeFantasyAuthorityConnection :
        ServerAuthoritativeFantasyConnection,
        IServerAuthoritativeAuthorityEndpointConnection
    {
        readonly object m_ControlGate = new object();
        readonly Queue<ServerAuthoritativeAuthorityRegistrationResult> m_Registrations =
            new Queue<ServerAuthoritativeAuthorityRegistrationResult>();
        readonly Queue<ServerAuthoritativeAuthorityRosterLock> m_Rosters =
            new Queue<ServerAuthoritativeAuthorityRosterLock>();
        readonly Queue<ServerAuthoritativeAuthorityDataPlaneTicket> m_Tickets =
            new Queue<ServerAuthoritativeAuthorityDataPlaneTicket>();
        readonly Queue<ServerAuthoritativeAuthorityHeartbeatAck> m_HeartbeatAcks =
            new Queue<ServerAuthoritativeAuthorityHeartbeatAck>();
        readonly Queue<ServerAuthoritativeAuthorityFullCheckpointRequest> m_CheckpointRequests =
            new Queue<ServerAuthoritativeAuthorityFullCheckpointRequest>();
        ServerAuthoritativeAuthoritySourceRuntime m_SourceRuntime;
        bool m_LeaveSent;

        ServerAuthoritativeAuthorityHostIdentity Host =>
            UnityAuthorityHostProduct.CreateWorkerHostIdentity(Process);

        public ServerAuthoritativeFantasyAuthorityConnection(
            ServerAuthoritativeFantasyEndpointDefinition definition,
            ServerAuthoritativeProcessIdentity process,
            ServerAuthoritativePipelineCompatibilityIdentity compatibility,
            ServerAuthoritativeModelPolicy policy,
            CharacterSimulationProgram program,
            ServerAuthoritativeDataPlaneLaunch dataPlane,
            SimulationWorldIdentityDescriptor worldIdentity,
            StableHash modelConfigurationHash,
            ISimulationDiagnosticsSink diagnostics)
            : base(definition, process, compatibility, policy, program, dataPlane, worldIdentity, modelConfigurationHash, diagnostics)
        {
        }

        ServerAuthoritativeAuthorityControlTransportStatus IServerAuthoritativeAuthorityControlTransport.ControlStatus =>
            Status switch
            {
                ServerAuthoritativeEndpointConnectionStatus.Pending => ServerAuthoritativeAuthorityControlTransportStatus.Pending,
                ServerAuthoritativeEndpointConnectionStatus.Ready => ServerAuthoritativeAuthorityControlTransportStatus.Ready,
                ServerAuthoritativeEndpointConnectionStatus.Failed => ServerAuthoritativeAuthorityControlTransportStatus.Failed,
                _ => throw new InvalidOperationException($"Unknown endpoint status '{Status}'.")
            };

        ServerAuthoritativeAuthorityControlFailure IServerAuthoritativeAuthorityControlTransport.ControlFailure =>
            Failure == null ? null : new ServerAuthoritativeAuthorityControlFailure(Failure.Code, Failure.Message);

        IPEndPoint IServerAuthoritativeAuthorityDataTransport.LocalEndPoint => Datagram.LocalEndPoint;
        int IServerAuthoritativeAuthorityDataTransport.ReceiveQueueDepth => Datagram.ReceiveQueueDepth;
        int IServerAuthoritativeAuthorityDataTransport.SendQueueDepth => Datagram.SendQueueDepth;
        bool IServerAuthoritativeAuthorityDataTransport.IsFailed => Datagram.IsFailed;

        public void AttachSourceRuntime(ServerAuthoritativeAuthoritySourceRuntime runtime)
        {
            if (runtime == null)
                throw new ArgumentNullException(nameof(runtime));
            if (m_SourceRuntime != null)
                throw new InvalidOperationException("Fantasy Authority connection already has a portable Source runtime.");
            m_SourceRuntime = runtime;
        }

        protected override async FTask BeginHandshakeAsync()
        {
            try
            {
                SetAuthorityHost(Host);
                SetAuthorityWorld(BuildLocalWorldIdentity());
                using var request = W2G_ServerAuthoritativeAuthorityRegisterRequest.Create();
                request.RoomId = Process.RoomId.Value;
                request.Host = CreateAuthorityHostIdentity(Host);
                request.ProcessRole = (int)Process.Role;
                request.Protocol = CreateProtocolIdentity();
                request.Program = CreateProgramIdentity();
                request.AuthorityPipeline = CreatePipelineIdentity(Compatibility.AuthorityPipeline, Compatibility, AuthorityWorld);
                request.PredictionPipelineId = Compatibility.PredictionPipeline.Id.Value;
                request.PredictionPipelineHash = Compatibility.PredictionPipeline.Hash.ToString();
                request.DataEndpoint = new ServerAuthoritativeDataEndpointMessage
                {
                    Host = DataPlane.AdvertisedEndPoint.Address.ToString(),
                    Port = checked((uint)DataPlane.AdvertisedEndPoint.Port)
                };
                request.World = CreateWorldIdentity();
                using G2W_ServerAuthoritativeAuthorityRegisterResponse response =
                    await Control.RegisterAuthorityAsync(request);
                if (response.ResultCode != 0)
                    throw new InvalidOperationException($"Fantasy worker register failed ({response.ResultCode}): {response.FailureReason}");
                SetSessionId(response.SessionId);
                Enqueue(m_Registrations, new ServerAuthoritativeAuthorityRegistrationResult(
                    SessionId,
                    Host));
                MarkHandshakeAccepted();
            }
            catch (Exception exception)
            {
                Fail("fantasy_worker_register_failed", exception.Message);
            }
        }

        protected override void OnRosterLocked(IReadOnlyList<ServerAuthoritativeRosterEntry> roster)
        {
            Enqueue(m_Rosters, new ServerAuthoritativeAuthorityRosterLock(
                SessionId,
                Host,
                RosterRevision,
                roster));
        }

        protected override void OnControlHeartbeatAcknowledged(
            ulong sequence,
            long clientUnixMilliseconds,
            long serverUnixMilliseconds)
        {
            Enqueue(m_HeartbeatAcks, new ServerAuthoritativeAuthorityHeartbeatAck(
                sequence,
                clientUnixMilliseconds,
                serverUnixMilliseconds));
        }

        public override void ReceiveTicket(ServerAuthoritativeDataPlaneTicketMessage ticket)
        {
            RecordControlReceived();
            if (ticket == null)
                throw new InvalidOperationException("Authority received an empty data-plane ticket.");
            if (!string.Equals(ticket.RoomId, Process.RoomId.Value, StringComparison.Ordinal) ||
                !string.Equals(ticket.HostId, Host.HostId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Authority received a data-plane ticket for another Host.");
            }
            Enqueue(m_Tickets, new ServerAuthoritativeAuthorityDataPlaneTicket(
                new ServerAuthoritativeSessionId(ticket.SessionId),
                Host,
                new ServerAuthoritativePlayerId(ticket.PlayerId),
                new ActorId(ticket.ActorId),
                ticket.TicketId,
                ticket.Nonce,
                ticket.ExpiresAtUnixMilliseconds));
        }

        public override void ReceiveFullCheckpointRequest(G2W_ServerAuthoritativeFullCheckpointRequest message)
        {
            if (message == null)
                throw new InvalidOperationException("Authority received an empty full checkpoint request.");
            RequireControlIdentity(message.RoomId, message.SessionId);
            RecordControlReceived();
            Enqueue(m_CheckpointRequests, new ServerAuthoritativeAuthorityFullCheckpointRequest(
                new ServerAuthoritativePlayerId(message.PlayerId),
                new ActorId(message.ActorId),
                message.RequestSequence));
        }

        protected override bool CanBecomeReady() => m_SourceRuntime != null && m_SourceRuntime.IsReady;

        protected override void PumpDataPlane()
        {
            if (m_SourceRuntime == null)
                throw new InvalidOperationException("Fantasy Authority connection has no portable Source runtime.");
            m_SourceRuntime.PumpTransport();
            TryBecomeReady();
        }

        void IServerAuthoritativeAuthorityControlTransport.Step(SimulationTickSourceIdentity source)
        {
            if (source.Kind != SimulationTickSourceKind.Authoritative)
                throw new InvalidOperationException("Fantasy Authority control transport requires an Authoritative source Tick.");
            StepSource(source);
        }

        bool IServerAuthoritativeAuthorityControlTransport.TryTakeRegistration(
            out ServerAuthoritativeAuthorityRegistrationResult value) => TryDequeue(m_Registrations, out value);

        bool IServerAuthoritativeAuthorityControlTransport.TryTakeRoster(
            out ServerAuthoritativeAuthorityRosterLock value) => TryDequeue(m_Rosters, out value);

        bool IServerAuthoritativeAuthorityControlTransport.TryTakeTicket(
            out ServerAuthoritativeAuthorityDataPlaneTicket value) => TryDequeue(m_Tickets, out value);

        bool IServerAuthoritativeAuthorityControlTransport.TryTakeHeartbeatAck(
            out ServerAuthoritativeAuthorityHeartbeatAck value) => TryDequeue(m_HeartbeatAcks, out value);

        bool IServerAuthoritativeAuthorityControlTransport.TryTakeFullCheckpointRequest(
            out ServerAuthoritativeAuthorityFullCheckpointRequest value) => TryDequeue(m_CheckpointRequests, out value);

        void IServerAuthoritativeAuthorityControlTransport.SendTicketConsumed(
            ServerAuthoritativeAuthorityDataPlaneTicket ticket)
        {
            RequireOperational();
            using var message = W2G_ServerAuthoritativeDataPlaneTicketConsumed.Create();
            message.RoomId = ticket.RoomId.Value;
            message.SessionId = ticket.SessionId.Value;
            message.HostId = Host.HostId;
            message.PlayerId = ticket.PlayerId.Value;
            message.TicketId = ticket.TicketId;
            Control.Send(message);
        }

        void IServerAuthoritativeAuthorityControlTransport.SendReliableEvents(
            ServerAuthoritativeAuthorityReliableEventBatchOutput value)
        {
            RequireOperational();
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            using var message = W2G_ServerAuthoritativeReliableGameplayEventBatch.Create();
            message.RoomId = Process.RoomId.Value;
            message.SessionId = SessionId.Value;
            message.RecipientActorId = value.RecipientActorId.Value;
            int payloadBytes = 0;
            for (int i = 0; i < value.Events.Count; i++)
            {
                ServerAuthoritativeAuthorityReliableEventOutput output = value.Events[i];
                ServerAuthoritativeReliableEvent reliable = output.Value;
                byte[] payload = output.Payload;
                message.Events.Add(new ServerAuthoritativeReliableGameplayEventMessage
                {
                    ActorId = output.SourceActorId.Value,
                    EventId = reliable.Header.EventId.ToString(),
                    EventSequence = reliable.Header.Sequence,
                    AuthorityTick = reliable.Header.Tick.Value,
                    EventKind = reliable.IsGameplay ? "gameplay" : "presentation",
                    PayloadSchemaVersion = 1,
                    PayloadLength = checked((uint)payload.Length),
                    Payload = payload
                });
                payloadBytes = checked(payloadBytes + payload.Length);
            }
            Control.SendReliable(message, payloadBytes);
        }

        void IServerAuthoritativeAuthorityControlTransport.SendFullCheckpoint(
            ServerAuthoritativeAuthorityFullCheckpointOutput value)
        {
            RequireOperational();
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            using var response = W2G_ServerAuthoritativeFullCheckpointResponse.Create();
            response.RoomId = Process.RoomId.Value;
            response.SessionId = SessionId.Value;
            response.PlayerId = value.PlayerId.Value;
            response.ActorId = value.ActorId.Value;
            response.RequestSequence = value.RequestSequence;
            response.AuthorityTick = value.Checkpoint.Baseline.AuthorityTick.Value;
            response.ConfirmedInputSequence = value.Checkpoint.Baseline.ConfirmedInputSequence;
            response.ReliableEventHorizon = value.Checkpoint.Baseline.ConfirmedEventHorizon.Sequence;
            response.CheckpointLayoutHash = CheckpointLayout.LayoutIdentity.ToString();
            response.CheckpointHash = value.Checkpoint.CheckpointHash.ToString();
            response.CheckpointLength = checked((uint)value.Payload.Length);
            response.Checkpoint = value.Payload;
            response.SnapshotSequence = value.SnapshotSequence;
            Control.SendFullCheckpoint(response, value.Payload.Length);
        }

        void IServerAuthoritativeAuthorityControlTransport.SendLeave(string reason)
        {
            SendLeave(reason);
        }

        void IServerAuthoritativeAuthorityControlTransport.SendFailure(string code, string message)
        {
            Fail(code, message);
        }

        protected override void SendLeave()
        {
            SendLeave("authority_source_disposed");
        }

        void SendLeave(string reason)
        {
            if (m_LeaveSent || !SessionId.IsValid)
                return;
            m_LeaveSent = true;
            using var message = W2G_ServerAuthoritativeLeave.Create();
            message.RoomId = Process.RoomId.Value;
            message.SessionId = SessionId.Value;
            message.HostId = Host.HostId;
            message.Reason = string.IsNullOrWhiteSpace(reason) ? throw new ArgumentException("Authority leave reason is required.", nameof(reason)) : reason.Trim();
            Control.Send(message);
        }

        ServerAuthoritativeDatagramMetrics IServerAuthoritativeAuthorityDataTransport.CaptureMetrics() => Datagram.CaptureMetrics();
        void IServerAuthoritativeAuthorityDataTransport.BindRemote(ServerAuthoritativeDatagramIdentity identity, IPEndPoint remoteEndPoint) => Datagram.BindRemote(identity, remoteEndPoint);
        void IServerAuthoritativeAuthorityDataTransport.RevokeRemote(ServerAuthoritativeDatagramIdentity identity) => Datagram.RevokeRemote(identity);
        void IServerAuthoritativeAuthorityDataTransport.EnqueueSend(ServerAuthoritativeDatagramPacket packet) => Datagram.EnqueueSend(packet);
        void IServerAuthoritativeAuthorityDataTransport.PumpSend() => Datagram.PumpSend();
        bool IServerAuthoritativeAuthorityDataTransport.TryReceive(out ServerAuthoritativeReceivedDatagram datagram) => Datagram.TryReceive(out datagram);
        void IServerAuthoritativeAuthorityDataTransport.ThrowIfUnavailable() => Datagram.ThrowIfUnavailable();

        void Enqueue<T>(Queue<T> queue, T value)
        {
            lock (m_ControlGate)
                queue.Enqueue(value);
        }

        bool TryDequeue<T>(Queue<T> queue, out T value)
        {
            lock (m_ControlGate)
            {
                if (queue.Count == 0)
                {
                    value = default;
                    return false;
                }
                value = queue.Dequeue();
                return true;
            }
        }
    }
}
