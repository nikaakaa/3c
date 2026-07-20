using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using Fantasy;
using Fantasy.Async;
using Fantasy.Entitas;
using Fantasy.Network;
using ThirdPersonGameplay.Networking.Fantasy;
using ThirdPersonSimulation;
using ThirdPersonSimulation.ServerAuthoritative;
using ThirdPersonSimulation.ServerAuthoritative.Transport;

namespace ThirdPersonGameplay.Networking.ServerAuthoritative
{
    internal static class ServerAuthoritativeFantasyEndpointRuntime
    {
        public static IServerAuthoritativeEndpointConnection Create(
            ServerAuthoritativeFantasyEndpointDefinition definition,
            ServerAuthoritativeProcessIdentity process,
            ServerAuthoritativePipelineCompatibilityIdentity compatibility,
            ServerAuthoritativeModelPolicy policy,
            CharacterSimulationProgram program,
            ServerAuthoritativeDataPlaneLaunch dataPlane,
            SimulationWorldIdentityDescriptor worldIdentity,
            StableHash modelConfigurationHash,
            ISimulationDiagnosticsSink diagnostics)
        {
            if (!definition)
                throw new ArgumentNullException(nameof(definition));
            if (compatibility == null || policy == null || program == null || diagnostics == null)
                throw new ArgumentNullException("ServerAuthoritative Endpoint dependency is missing.");
            if (!program.ProgramHash.Equals(compatibility.ProgramHash) || !program.LayoutHash.Equals(compatibility.LayoutHash))
                throw new InvalidOperationException("ServerAuthoritative Endpoint Program does not match the locked compatibility identity.");
            if (process.IsAuthority != dataPlane.IsAuthority)
                throw new InvalidOperationException("ServerAuthoritative process and data-plane launch roles do not match.");
            return process.IsAuthority
                ? new ServerAuthoritativeFantasyAuthorityConnection(definition, process, compatibility, policy, program, dataPlane, worldIdentity, modelConfigurationHash, diagnostics)
                : new ServerAuthoritativeFantasyPredictionConnection(definition, process, compatibility, policy, program, dataPlane, worldIdentity, modelConfigurationHash, diagnostics);
        }
    }

    internal sealed class ServerAuthoritativeEndpointSessionBinding : Entity
    {
        public ServerAuthoritativeFantasyConnection Runtime;
    }

    internal abstract class ServerAuthoritativeFantasyConnection : IServerAuthoritativeEndpointConnection
    {
        readonly ServerAuthoritativeFantasyEndpointDefinition m_Definition;
        readonly ServerAuthoritativeProcessIdentity m_Process;
        readonly ServerAuthoritativePipelineCompatibilityIdentity m_Compatibility;
        readonly ServerAuthoritativeModelPolicy m_Policy;
        readonly SimulationWorldIdentityDescriptor m_WorldIdentity;
        readonly StableHash m_ModelConfigurationHash;
        readonly ISimulationDiagnosticsSink m_Diagnostics;
        readonly ServerAuthoritativeConnectionCoordinator m_Coordinator;
        readonly ServerAuthoritativeControlSessionModule m_Control;
        readonly ServerAuthoritativeDatagramChannelModule m_Datagram;
        ServerAuthoritativeAuthorityHostIdentity m_AuthorityHost;
        ServerAuthoritativeWorldIdentity m_AuthorityWorld;
        bool m_HandshakeAccepted;
        ulong m_LastSourceTick;
        ulong m_LastTransportMetricsTick;
        long m_PreviousControlPackets;
        long m_PreviousControlPayloadBytes;
        long m_PreviousReliablePackets;
        long m_PreviousReliablePayloadBytes;
        long m_PreviousFullCheckpointPackets;
        long m_PreviousFullCheckpointPayloadBytes;
        ServerAuthoritativeDatagramMetrics m_LastDatagramMetrics;

        protected ServerAuthoritativeFantasyConnection(
            ServerAuthoritativeFantasyEndpointDefinition definition,
            ServerAuthoritativeProcessIdentity process,
            ServerAuthoritativePipelineCompatibilityIdentity compatibility,
            ServerAuthoritativeModelPolicy policy,
            CharacterSimulationProgram program,
            ServerAuthoritativeDataPlaneLaunch dataPlane,
            SimulationWorldIdentityDescriptor worldIdentity,
            StableHash modelConfigurationHash,
            ISimulationDiagnosticsSink diagnostics)
        {
            m_Definition = definition ? definition : throw new ArgumentNullException(nameof(definition));
            m_Process = process;
            m_Compatibility = compatibility ?? throw new ArgumentNullException(nameof(compatibility));
            m_Policy = policy ?? throw new ArgumentNullException(nameof(policy));
            m_WorldIdentity = worldIdentity ?? throw new ArgumentNullException(nameof(worldIdentity));
            WorldCapability requiredSolverCapabilities = process.IsAuthority
                ? compatibility.AuthoritySolverRequiredCapabilities
                : compatibility.PredictionSolverRequiredCapabilities;
            if ((m_WorldIdentity.Solver.Capabilities & requiredSolverCapabilities) != requiredSolverCapabilities)
            {
                throw new InvalidOperationException(
                    $"ServerAuthoritative Endpoint Solver lacks required role capabilities '{requiredSolverCapabilities}'.");
            }
            Program = program ?? throw new ArgumentNullException(nameof(program));
            CheckpointLayout = new NetworkCheckpointLayout(program);
            DataPlane = dataPlane;
            m_ModelConfigurationHash = modelConfigurationHash.IsValid
                ? modelConfigurationHash
                : throw new ArgumentException("Model configuration hash is invalid.", nameof(modelConfigurationHash));
            m_Diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
            m_Coordinator = new ServerAuthoritativeConnectionCoordinator();
            m_Control = new ServerAuthoritativeControlSessionModule(definition, process, compatibility.TickRate);
            m_Datagram = new ServerAuthoritativeDatagramChannelModule(
                dataPlane,
                definition.DatagramQueueCapacity,
                policy.MaxGameplayDatagramBytes);
            m_Control.Start(this);
        }

        public ServerAuthoritativeEndpointConnectionStatus Status => m_Coordinator.Status;
        public ServerAuthoritativeEndpointFailure Failure => m_Coordinator.Failure;
        protected ServerAuthoritativeProcessIdentity Process => m_Process;
        protected ServerAuthoritativePipelineCompatibilityIdentity Compatibility => m_Compatibility;
        protected ServerAuthoritativeModelPolicy Policy => m_Policy;
        protected CharacterSimulationProgram Program { get; }
        protected NetworkCheckpointLayout CheckpointLayout { get; }
        protected ServerAuthoritativeDataPlaneLaunch DataPlane { get; }
        protected ServerAuthoritativeControlSessionModule Control => m_Control;
        protected ServerAuthoritativeDatagramChannelModule Datagram => m_Datagram;
        protected ISimulationDiagnosticsSink Diagnostics => m_Diagnostics;
        protected IReadOnlyList<ServerAuthoritativeRosterEntry> Roster => m_Control.Roster;
        protected ServerAuthoritativeAuthorityHostIdentity AuthorityHost => m_AuthorityHost;
        protected ServerAuthoritativeWorldIdentity AuthorityWorld => m_AuthorityWorld;
        protected SimulationWorldIdentityDescriptor WorldIdentity => m_WorldIdentity;
        protected ServerAuthoritativeSessionId SessionId => m_Control.SessionId;
        protected ulong RosterRevision => m_Control.RosterRevision;
        protected ulong LastSourceTick => m_LastSourceTick;
        protected int EndpointTimeoutTicks => m_Definition.ConnectTimeoutTicks;

        public void Step(SimulationSessionLogicTickContext context)
        {
            StepSource(context.Source);
        }

        protected void StepSource(SimulationTickSourceIdentity source)
        {
            if (m_Coordinator.IsDisposed)
                throw new ObjectDisposedException(GetType().Name);
            if (Status == ServerAuthoritativeEndpointConnectionStatus.Failed)
                return;
            if (string.IsNullOrWhiteSpace(source.ClockId) || source.SourceTick == 0)
                throw new ArgumentException("Fantasy Endpoint source Tick is incomplete.", nameof(source));
            m_LastSourceTick = source.SourceTick;
            try
            {
                PumpCallbackIngress();
                PumpControlSessionEvents();
                PumpDataPlane();
                m_Control.PumpHeartbeat(source.SourceTick, m_AuthorityHost);
                PublishTransportMetrics(source.SourceTick);
                if (Status != ServerAuthoritativeEndpointConnectionStatus.Pending)
                    return;
                if (m_Coordinator.AdvancePreparation(m_Definition.ConnectTimeoutTicks))
                {
                    Fail("server_authoritative_endpoint_timeout", "Endpoint did not complete control and data-plane preparation before its configured timeout.");
                    return;
                }
                if (m_Control.IsSessionDisposed)
                {
                    Fail("fantasy_session_disposed", "Fantasy control Session was disposed during Source preparation.");
                    return;
                }
                if (m_Control.TryBeginHandshake())
                    BeginHandshakeAsync().Coroutine();
            }
            catch (Exception exception)
            {
                Fail("server_authoritative_endpoint_step_failed", exception.Message);
            }
        }

        public ServerAuthoritativeEndpointHandshake TakeHandshake()
        {
            RequireOperational();
            return m_Coordinator.TakeHandshake();
        }

        public void ReceiveRoster(ServerAuthoritativeRosterMessage message)
        {
            if (message == null || Status == ServerAuthoritativeEndpointConnectionStatus.Failed)
                return;
            try
            {
                ServerAuthoritativeRosterUpdateResult result = m_Control.AcceptRoster(message, m_AuthorityHost);
                if (!result.Changed || !result.Locked)
                    return;
                OnRosterLocked(result.Roster);
                TryBecomeReady();
            }
            catch (Exception exception)
            {
                Fail("fantasy_roster_invalid", exception.Message);
            }
        }

        public void ReceiveFailure(string sessionId, int resultCode, string reason)
        {
            m_Control.RecordControlReceived();
            if (m_Control.SessionId.IsValid && !string.Equals(sessionId, m_Control.SessionId.Value, StringComparison.Ordinal))
            {
                Fail("fantasy_failure_session_mismatch", "Fantasy failure targeted another SessionId.");
                return;
            }
            Fail($"fantasy_room_failed_{resultCode}", string.IsNullOrWhiteSpace(reason) ? "Fantasy Room failed without a reason." : reason);
        }

        public void ReceiveControlHeartbeatAck(
            string roomId,
            string sessionId,
            ulong sequence,
            long clientUnixMilliseconds,
            long serverUnixMilliseconds)
        {
            ServerAuthoritativeHeartbeatAckResult result = m_Control.AcceptHeartbeatAck(
                roomId,
                sessionId,
                sequence,
                clientUnixMilliseconds,
                serverUnixMilliseconds);
            if (!result.Changed)
                return;
            Publish(
                SimulationModelTraceKind.Transport,
                "server_authoritative_control_heartbeat_ack",
                $"role={m_Process.Role};sequence={sequence};rttMs={result.RoundTripMilliseconds};serverUnixMs={serverUnixMilliseconds}",
                m_Process.ActorId,
                m_LastSourceTick,
                0,
                0,
                sequence,
                0,
                true);
            OnControlHeartbeatAcknowledged(sequence, clientUnixMilliseconds, serverUnixMilliseconds);
        }

        public void ReceiveTicketRevoked(string sessionId, string ticketId, string reason)
        {
            m_Control.RecordControlReceived();
            if (!m_Control.SessionId.IsValid || !string.Equals(sessionId, m_Control.SessionId.Value, StringComparison.Ordinal))
            {
                Fail("data_plane_ticket_revoke_mismatch", "Data-plane ticket revocation targeted another SessionId.");
                return;
            }
            OnTicketRevoked(ticketId, reason);
        }

        public void Dispatch(Action action)
        {
            m_Coordinator.EnqueueCallback(action);
        }

        public virtual void ReceiveTicket(ServerAuthoritativeDataPlaneTicketMessage ticket) => RejectUnexpected(nameof(ServerAuthoritativeDataPlaneTicketMessage));
        public virtual void ReceiveReliableEvents(G2C_ServerAuthoritativeReliableGameplayEventBatch message) => RejectUnexpected(nameof(G2C_ServerAuthoritativeReliableGameplayEventBatch));
        public virtual void ReceiveFullCheckpointRequest(G2W_ServerAuthoritativeFullCheckpointRequest message) => RejectUnexpected(nameof(G2W_ServerAuthoritativeFullCheckpointRequest));
        public virtual void ReceiveFullCheckpoint(G2C_ServerAuthoritativeFullCheckpointResponse message) => RejectUnexpected(nameof(G2C_ServerAuthoritativeFullCheckpointResponse));

        protected void SetSessionId(string value)
        {
            m_Control.SetSessionId(value);
        }

        protected void MarkHandshakeAccepted()
        {
            m_HandshakeAccepted = true;
            TryBecomeReady();
        }

        protected void TryBecomeReady()
        {
            if (Status != ServerAuthoritativeEndpointConnectionStatus.Pending || !m_HandshakeAccepted ||
                !m_Control.SessionId.IsValid || m_Control.Roster == null || !m_AuthorityHost.IsValid || m_AuthorityWorld == null || !CanBecomeReady())
            {
                return;
            }
            var handshake = new ServerAuthoritativeEndpointHandshake(
                m_Process,
                m_AuthorityHost,
                m_AuthorityWorld,
                m_Compatibility,
                m_Control.Roster);
            m_Coordinator.MarkReady(handshake);
            Publish(
                SimulationModelTraceKind.Identity,
                "server_authoritative_ready",
                $"role={m_Process.Role};room={m_Process.RoomId};session={m_Control.SessionId};host={m_AuthorityHost};udp={m_Datagram.LocalEndPoint};layout={CheckpointLayout.LayoutIdentity}",
                m_Process.ActorId,
                m_LastSourceTick,
                0,
                0,
                0,
                m_Datagram.ReceiveQueueDepth + m_Datagram.SendQueueDepth,
                true);
        }

        protected virtual bool CanBecomeReady() => true;
        protected virtual void OnRosterLocked(IReadOnlyList<ServerAuthoritativeRosterEntry> roster) { }
        protected virtual void OnControlHeartbeatAcknowledged(
            ulong sequence,
            long clientUnixMilliseconds,
            long serverUnixMilliseconds) { }
        protected virtual void OnTicketRevoked(string ticketId, string reason) =>
            Fail("data_plane_ticket_revoked", $"Data-plane ticket '{ticketId}' was revoked: {reason}");
        protected abstract FTask BeginHandshakeAsync();
        protected abstract void PumpDataPlane();
        protected abstract void SendLeave();

        protected void RequireOperational()
        {
            if (m_Coordinator.IsDisposed)
                throw new ObjectDisposedException(GetType().Name);
            if (Status == ServerAuthoritativeEndpointConnectionStatus.Failed)
                throw new InvalidOperationException(Failure == null ? "ServerAuthoritative Endpoint failed." : $"{Failure.Code}: {Failure.Message}");
            if (!m_Control.HasSession)
                throw new InvalidOperationException("Fantasy control Session is unavailable.");
            m_Datagram.ThrowIfUnavailable();
        }

        protected void RequireOperational(ulong sourceTick)
        {
            RequireOperational();
            m_LastSourceTick = sourceTick;
            m_Control.PumpHeartbeat(sourceTick, m_AuthorityHost);
            PublishTransportMetrics(sourceTick);
        }

        protected void RequireControlIdentity(string roomId, string sessionId) =>
            m_Control.RequireIdentity(roomId, sessionId);

        protected void RecordControlSent(int payloadBytes = 0) => m_Control.RecordControlSent(payloadBytes);

        protected void RecordControlReceived(int payloadBytes = 0) => m_Control.RecordControlReceived(payloadBytes);

        protected void RecordReliableSent(int payloadBytes) => m_Control.RecordReliableSent(payloadBytes);

        protected void RecordReliableReceived(int payloadBytes) => m_Control.RecordReliableReceived(payloadBytes);

        protected void RecordFullCheckpointSent(int payloadBytes) => m_Control.RecordFullCheckpointSent(payloadBytes);

        protected void RecordFullCheckpointReceived(int payloadBytes) => m_Control.RecordFullCheckpointReceived(payloadBytes);

        void PumpCallbackIngress()
        {
            while (m_Coordinator.TryTakeCallback(out Action callback))
            {
                try
                {
                    callback();
                }
                catch (Exception exception)
                {
                    Fail("fantasy_control_message_invalid", exception.Message);
                    return;
                }
            }
        }

        void PumpControlSessionEvents()
        {
            while (m_Control.TryTakeEvent(out ServerAuthoritativeControlSessionEvent value))
            {
                switch (value.Kind)
                {
                    case ServerAuthoritativeControlSessionEventKind.TransportConnected:
                    case ServerAuthoritativeControlSessionEventKind.SessionReady:
                        break;
                    case ServerAuthoritativeControlSessionEventKind.TransportFailed:
                        Fail("fantasy_transport_connect_failed", value.Detail);
                        return;
                    case ServerAuthoritativeControlSessionEventKind.TransportDisconnected:
                        Fail("fantasy_transport_disconnected", value.Detail);
                        return;
                    default:
                        Fail("fantasy_control_event_invalid", $"Unknown control session event '{value.Kind}'.");
                        return;
                }
            }
        }

        void PublishTransportMetrics(ulong sourceTick)
        {
            if (!m_Diagnostics.IsEnabled || sourceTick == 0)
                return;
            ulong interval = checked((ulong)m_Compatibility.TickRate * 5UL);
            if (m_LastTransportMetricsTick == 0)
            {
                m_LastTransportMetricsTick = sourceTick;
                m_LastDatagramMetrics = m_Datagram.CaptureMetrics();
                return;
            }
            if (sourceTick < m_LastTransportMetricsTick + interval)
                return;
            ulong elapsedTicks = sourceTick - m_LastTransportMetricsTick;
            float elapsedSeconds = elapsedTicks / (float)m_Compatibility.TickRate;
            ServerAuthoritativeDatagramMetrics current = m_Datagram.CaptureMetrics();
            long sentPackets = current.SentPackets - m_LastDatagramMetrics.SentPackets;
            long sentBytes = current.SentBytes - m_LastDatagramMetrics.SentBytes;
            long receivedPackets = current.ReceivedPackets - m_LastDatagramMetrics.ReceivedPackets;
            long receivedBytes = current.ReceivedBytes - m_LastDatagramMetrics.ReceivedBytes;
            ServerAuthoritativeControlMetrics control = m_Control.CaptureMetrics();
            long controlPackets = control.ControlSentPackets + control.ControlReceivedPackets;
            long controlBytes = control.ControlSentPayloadBytes + control.ControlReceivedPayloadBytes;
            long reliablePackets = control.ReliableSentPackets + control.ReliableReceivedPackets;
            long reliableBytes = control.ReliableSentPayloadBytes + control.ReliableReceivedPayloadBytes;
            long fullPackets = control.FullCheckpointSentPackets + control.FullCheckpointReceivedPackets;
            long fullBytes = control.FullCheckpointSentPayloadBytes + control.FullCheckpointReceivedPayloadBytes;
            long controlOutstanding = checked((long)(control.HeartbeatSequence - control.HeartbeatAckSequence));
            m_Diagnostics.PublishModel(new SimulationModelTraceRecord(
                SimulationModelTraceKind.Transport,
                "server_authoritative_transport_metrics",
                $"role={m_Process.Role};controlSent={control.ControlSentPackets};controlReceived={control.ControlReceivedPackets};controlPayloadSent={control.ControlSentPayloadBytes};controlPayloadReceived={control.ControlReceivedPayloadBytes};controlPacketsPerSecond={(controlPackets - m_PreviousControlPackets) / elapsedSeconds:0.##};controlBytesPerSecond={(controlBytes - m_PreviousControlPayloadBytes) / elapsedSeconds:0.##};controlOutstanding={controlOutstanding};reliableSent={control.ReliableSentPackets};reliableReceived={control.ReliableReceivedPackets};reliableBytesSent={control.ReliableSentPayloadBytes};reliableBytesReceived={control.ReliableReceivedPayloadBytes};reliablePacketsPerSecond={(reliablePackets - m_PreviousReliablePackets) / elapsedSeconds:0.##};reliableBytesPerSecond={(reliableBytes - m_PreviousReliablePayloadBytes) / elapsedSeconds:0.##};fullSent={control.FullCheckpointSentPackets};fullReceived={control.FullCheckpointReceivedPackets};fullBytesSent={control.FullCheckpointSentPayloadBytes};fullBytesReceived={control.FullCheckpointReceivedPayloadBytes};fullPacketsPerSecond={(fullPackets - m_PreviousFullCheckpointPackets) / elapsedSeconds:0.##};fullBytesPerSecond={(fullBytes - m_PreviousFullCheckpointPayloadBytes) / elapsedSeconds:0.##};udpPacketsPerSecond={(sentPackets + receivedPackets) / elapsedSeconds:0.##};udpBytesPerSecond={(sentBytes + receivedBytes) / elapsedSeconds:0.##};udpSent={current.SentPackets};udpReceived={current.ReceivedPackets};malformed={current.MalformedDrops};unknownRoute={current.UnknownRouteDrops};oversize={current.OversizeDrops};endpointMismatch={current.EndpointMismatchDrops}",
                m_Process.ActorId,
                sourceTick,
                0,
                0,
                control.HeartbeatAckSequence,
                m_Datagram.ReceiveQueueDepth + m_Datagram.SendQueueDepth,
                0,
                control.RoundTripMilliseconds,
                control.JitterMilliseconds,
                true));
            m_LastTransportMetricsTick = sourceTick;
            m_LastDatagramMetrics = current;
            m_PreviousControlPackets = controlPackets;
            m_PreviousControlPayloadBytes = controlBytes;
            m_PreviousReliablePackets = reliablePackets;
            m_PreviousReliablePayloadBytes = reliableBytes;
            m_PreviousFullCheckpointPackets = fullPackets;
            m_PreviousFullCheckpointPayloadBytes = fullBytes;
        }

        protected void Fail(string code, string message)
        {
            if (!m_Coordinator.TryFail(code, message, out _))
                return;
            Publish(SimulationModelTraceKind.Failure, code, message, m_Process.ActorId, m_LastSourceTick, 0, 0, 0, 0, false);
        }

        protected void Publish(
            SimulationModelTraceKind kind,
            string code,
            string detail,
            ActorId actorId,
            ulong localSourceTick,
            ulong authorityTick,
            ulong inputSequence,
            ulong ackSequence,
            int queueDepth,
            bool success)
        {
            if (!m_Diagnostics.IsEnabled)
                return;
            m_Diagnostics.PublishModel(new SimulationModelTraceRecord(
                kind,
                code,
                detail,
                actorId,
                localSourceTick,
                authorityTick,
                inputSequence,
                ackSequence,
                queueDepth,
                0,
                0f,
                0f,
                success));
        }

        protected ServerAuthoritativeProtocolIdentityMessage CreateProtocolIdentity() => new ServerAuthoritativeProtocolIdentityMessage
        {
            ModelProtocolVersion = ServerAuthoritativeModelIdentity.ProtocolVersion,
            ModelId = ServerAuthoritativeModelIdentity.ModelId,
            ModelConfigurationHash = m_ModelConfigurationHash.ToString(),
            EndpointId = ServerAuthoritativeFantasyEndpointDefinition.EndpointId
        };

        protected ServerAuthoritativeProgramIdentityMessage CreateProgramIdentity() => new ServerAuthoritativeProgramIdentityMessage
        {
            ProgramId = Compatibility.ProgramId.Value,
            ProgramHash = Compatibility.ProgramHash.ToString(),
            LayoutHash = Compatibility.LayoutHash.ToString(),
            OperationSetId = CharacterGameplayOperationSet.Id,
            OperationSetVersion = Compatibility.OperationSetVersion.Value
        };

        protected void SetAuthorityHost(ServerAuthoritativeAuthorityHostIdentity host)
        {
            if (!host.IsValid || !host.RoomId.Equals(Process.RoomId))
                throw new InvalidOperationException("Authority Host identity does not match the Endpoint Room.");
            if (m_AuthorityHost.IsValid && !m_AuthorityHost.Equals(host))
                throw new InvalidOperationException("Authority Host identity changed while the Endpoint was active.");
            m_AuthorityHost = host;
        }

        protected void SetAuthorityWorld(ServerAuthoritativeWorldIdentity world)
        {
            if (world == null || !WorldContractMatches(world))
                throw new InvalidOperationException("Authority World identity does not satisfy the locked Session contract.");
            if (m_AuthorityWorld != null && !m_AuthorityWorld.Matches(world))
                throw new InvalidOperationException("Authority World identity changed while the Endpoint was active.");
            m_AuthorityWorld = world;
        }

        protected ServerAuthoritativeAuthorityHostIdentity ParseAuthorityHost(
            ServerAuthoritativeAuthorityHostIdentityMessage message)
        {
            if (message == null)
                throw new InvalidOperationException("Fantasy response omitted the Authority Host identity.");
            return new ServerAuthoritativeAuthorityHostIdentity(
                new HostProductId(message.HostProductId),
                message.HostId,
                (ServerAuthoritativeAuthorityHostRouteKind)message.RouteKind,
                new ServerAuthoritativeRoomId(message.RoomId));
        }

        protected ServerAuthoritativeAuthorityHostIdentityMessage CreateAuthorityHostIdentity(
            ServerAuthoritativeAuthorityHostIdentity host) => new ServerAuthoritativeAuthorityHostIdentityMessage
        {
            HostProductId = host.HostProductId.Value,
            HostId = host.HostId,
            RouteKind = (int)host.RouteKind,
            RoomId = host.RoomId.Value
        };

        protected ServerAuthoritativeWorldIdentityMessage CreateWorldIdentity() => new ServerAuthoritativeWorldIdentityMessage
        {
            SolverId = WorldIdentity.Solver.ImplementationId.Value,
            SolverVersion = WorldIdentity.Solver.ImplementationVersion,
            SolverCapabilities = (ulong)WorldIdentity.Solver.Capabilities,
            SolverFeatures = (ulong)WorldIdentity.Solver.Features,
            WorldId = WorldIdentity.WorldId.Value,
            MapId = WorldIdentity.MapId,
            WorldRevision = WorldIdentity.WorldRevision.Value,
            WorldConfigurationHash = WorldIdentity.WorldConfigurationHash.ToString(),
            NavigationSurfaceArtifactHash = WorldIdentity.NavigationSurfaceArtifactHash.ToString(),
            QueryProfileHash = WorldIdentity.QueryProfileHash.ToString()
        };

        protected ServerAuthoritativeWorldIdentity ParseWorldIdentity(ServerAuthoritativeWorldIdentityMessage message)
        {
            if (message == null)
                throw new InvalidOperationException("Fantasy response omitted the Authority World identity.");
            return new ServerAuthoritativeWorldIdentity(
                new SolverImplementationId(message.SolverId),
                message.SolverVersion,
                (WorldCapability)message.SolverCapabilities,
                (WorldFeature)message.SolverFeatures,
                new SimulationWorldId(message.WorldId),
                message.MapId,
                new WorldRevision(message.WorldRevision),
                new StableHash(message.WorldConfigurationHash),
                new StableHash(message.NavigationSurfaceArtifactHash),
                new StableHash(message.QueryProfileHash));
        }

        protected bool WorldContractMatches(ServerAuthoritativeWorldIdentity world) =>
            (world.SolverCapabilities & Compatibility.AuthoritySolverRequiredCapabilities) ==
                Compatibility.AuthoritySolverRequiredCapabilities &&
            world.WorldId.Equals(WorldIdentity.WorldId) &&
            string.Equals(world.MapId, WorldIdentity.MapId, StringComparison.Ordinal) &&
            world.WorldRevision.Equals(WorldIdentity.WorldRevision);

        protected ServerAuthoritativeWorldIdentity BuildLocalWorldIdentity() =>
            new ServerAuthoritativeWorldIdentity(
                WorldIdentity.Solver.ImplementationId,
                WorldIdentity.Solver.ImplementationVersion,
                WorldIdentity.Solver.Capabilities,
                WorldIdentity.Solver.Features,
                WorldIdentity.WorldId,
                WorldIdentity.MapId,
                WorldIdentity.WorldRevision,
                WorldIdentity.WorldConfigurationHash,
                WorldIdentity.NavigationSurfaceArtifactHash,
                WorldIdentity.QueryProfileHash);

        protected static ServerAuthoritativePipelineIdentityMessage CreatePipelineIdentity(
            SimulationPipelineIdentity pipeline,
            ServerAuthoritativePipelineCompatibilityIdentity compatibility,
            ServerAuthoritativeWorldIdentity world) => new ServerAuthoritativePipelineIdentityMessage
        {
            PipelineId = pipeline.Id.Value,
            PipelineHash = pipeline.Hash.ToString(),
            BackendId = compatibility.Backend.ComponentId,
            SolverId = world.SolverId.Value,
            SolverVersion = world.SolverVersion,
            TickRate = checked((uint)compatibility.TickRate),
            SolverCapabilities = (ulong)world.SolverCapabilities,
            SolverFeatures = (ulong)world.SolverFeatures
        };

        protected static long ClockMicros() => checked(Stopwatch.GetTimestamp() * 1000000L / Stopwatch.Frequency);

        void RejectUnexpected(string message) =>
            Fail("fantasy_message_role_mismatch", $"Process role '{Process.Role}' received unexpected message '{message}'.");

        public void Dispose()
        {
            if (!m_Coordinator.BeginDispose())
                return;
            m_Control.Dispose(this, SendLeave);
            m_Datagram.Dispose();
            m_AuthorityHost = default;
            m_AuthorityWorld = null;
        }
    }

    internal sealed class ServerAuthoritativeFantasyPredictionConnection :
        ServerAuthoritativeFantasyConnection,
        IServerAuthoritativePredictionEndpointConnection
    {
        readonly ServerAuthoritativeCheckpointReconstructionModule m_Checkpoints;
        readonly ServerAuthoritativePredictionEvidenceModule m_Evidence;

        public ServerAuthoritativeFantasyPredictionConnection(
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
            m_Checkpoints = new ServerAuthoritativeCheckpointReconstructionModule(
                CheckpointLayout,
                process.ActorId,
                definition.DatagramQueueCapacity);
            m_Evidence = new ServerAuthoritativePredictionEvidenceModule(
                process,
                policy,
                definition.ReliableQueueCapacity);
        }

        protected override async FTask BeginHandshakeAsync()
        {
            try
            {
                using var request = C2G_ServerAuthoritativeClientJoinRequest.Create();
                request.RoomId = Process.RoomId.Value;
                request.PlayerId = Process.PlayerId.Value;
                request.ProcessRole = (int)Process.Role;
                request.Protocol = CreateProtocolIdentity();
                request.Program = CreateProgramIdentity();
                request.PredictionPipelineId = Compatibility.PredictionPipeline.Id.Value;
                request.PredictionPipelineHash = Compatibility.PredictionPipeline.Hash.ToString();
                request.PredictionWorld = CreateWorldIdentity();
                using G2C_ServerAuthoritativeClientJoinResponse response =
                    await Control.JoinPredictionAsync(request);
                if (response.ResultCode != 0)
                    throw new InvalidOperationException($"Fantasy client join failed ({response.ResultCode}): {response.FailureReason}");
                if (!string.Equals(response.OwnedActorId, Process.ActorId.Value, StringComparison.Ordinal))
                    throw new InvalidOperationException("Fantasy client join returned a different owned ActorId.");
                ServerAuthoritativeAuthorityHostIdentity host = ParseAuthorityHost(response.AuthorityHost);
                ServerAuthoritativeWorldIdentity authorityWorld = ParseWorldIdentity(response.AuthorityWorld);
                if (!WorldContractMatches(authorityWorld) ||
                    response.AuthorityPipeline == null ||
                    !string.Equals(response.AuthorityPipeline.PipelineId, Compatibility.AuthorityPipeline.Id.Value, StringComparison.Ordinal) ||
                    !string.Equals(response.AuthorityPipeline.PipelineHash, Compatibility.AuthorityPipeline.Hash.ToString(), StringComparison.Ordinal) ||
                    !string.Equals(response.AuthorityPipeline.BackendId, Compatibility.Backend.ComponentId, StringComparison.Ordinal) ||
                    !string.Equals(response.AuthorityPipeline.SolverId, authorityWorld.SolverId.Value, StringComparison.Ordinal) ||
                    !string.Equals(response.AuthorityPipeline.SolverVersion, authorityWorld.SolverVersion, StringComparison.Ordinal) ||
                    response.AuthorityPipeline.SolverCapabilities != (ulong)authorityWorld.SolverCapabilities ||
                    response.AuthorityPipeline.SolverFeatures != (ulong)authorityWorld.SolverFeatures ||
                    response.AuthorityPipeline.TickRate != (uint)Compatibility.TickRate)
                {
                    throw new InvalidOperationException("Fantasy client join returned an incompatible Authority World or Pipeline identity.");
                }
                SetAuthorityHost(host);
                SetAuthorityWorld(authorityWorld);
                SetSessionId(response.SessionId);
                ReceiveRoster(response.Roster);
                MarkHandshakeAccepted();
                using var accepted = C2G_ServerAuthoritativeClientJoinAccepted.Create();
                accepted.RoomId = Process.RoomId.Value;
                accepted.SessionId = SessionId.Value;
                accepted.PlayerId = Process.PlayerId.Value;
                accepted.HostId = AuthorityHost.HostId;
                Control.Send(accepted);
            }
            catch (Exception exception)
            {
                Fail("fantasy_client_join_failed", exception.Message);
            }
        }

        protected override void OnRosterLocked(IReadOnlyList<ServerAuthoritativeRosterEntry> roster)
        {
            int matches = 0;
            for (int i = 0; i < roster.Count; i++)
            {
                if (roster[i].ActorId == Process.ActorId && roster[i].PlayerId.Equals(Process.PlayerId) && roster[i].ClientRole == Process.Role)
                    matches++;
            }
            if (matches != 1)
                throw new InvalidOperationException("Fantasy roster does not contain this client's exact owner identity.");
        }

        public override void ReceiveTicket(ServerAuthoritativeDataPlaneTicketMessage ticket)
        {
            RecordControlReceived();
            if (ticket == null || ticket.AuthorityEndpoint == null ||
                !string.Equals(ticket.RoomId, Process.RoomId.Value, StringComparison.Ordinal) ||
                !string.Equals(ticket.SessionId, SessionId.Value, StringComparison.Ordinal) ||
                !string.Equals(ticket.PlayerId, Process.PlayerId.Value, StringComparison.Ordinal) ||
                !string.Equals(ticket.ActorId, Process.ActorId.Value, StringComparison.Ordinal) ||
                !string.Equals(ticket.HostId, AuthorityHost.HostId, StringComparison.Ordinal) ||
                ticket.ExpiresAtUnixMilliseconds <= DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
            {
                throw new InvalidOperationException("Prediction received an invalid or expired data-plane ticket.");
            }
            IPAddress address = IPAddress.Parse(ticket.AuthorityEndpoint.Host);
            Datagram.AcceptPredictionTicket(
                ticket,
                new ServerAuthoritativeDatagramIdentity(Process.RoomId, SessionId, Process.PlayerId, Process.ActorId),
                new IPEndPoint(address, checked((int)ticket.AuthorityEndpoint.Port)),
                ClockMicros());
        }

        public override void ReceiveReliableEvents(G2C_ServerAuthoritativeReliableGameplayEventBatch message)
        {
            RequireControlIdentity(message.RoomId, message.SessionId);
            RecordReliableReceived(m_Evidence.AcceptReliableEvents(message, RequireRemoteActorId()));
        }

        public override void ReceiveFullCheckpoint(G2C_ServerAuthoritativeFullCheckpointResponse message)
        {
            RequireControlIdentity(message.RoomId, message.SessionId);
            RecordFullCheckpointReceived(message.Checkpoint?.Length ?? 0);
            if (!string.Equals(message.ActorId, Process.ActorId.Value, StringComparison.Ordinal) ||
                message.AuthorityTick == 0 || message.SnapshotSequence == 0 || message.Checkpoint == null ||
                message.CheckpointLength != message.Checkpoint.Length ||
                !string.Equals(message.CheckpointLayoutHash, CheckpointLayout.LayoutIdentity.ToString(), StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Full checkpoint response identity or payload boundary is invalid.");
            }
            HandleCheckpointResult(m_Checkpoints.AcceptFull(
                message.SnapshotSequence,
                message.AuthorityTick,
                message.ConfirmedInputSequence,
                message.ReliableEventHorizon,
                message.CheckpointLayoutHash,
                message.CheckpointHash,
                message.Checkpoint));
        }

        protected override bool CanBecomeReady() => Datagram.IsPredictionReady;

        protected override void PumpDataPlane()
        {
            Datagram.PumpPrediction(ClockMicros());
            while (Datagram.TryTakePredictionEvent(out ServerAuthoritativePredictionDatagramEvent value))
            {
                switch (value.Kind)
                {
                    case ServerAuthoritativePredictionDatagramEventKind.DataPlaneReady:
                        m_Evidence.AcceptDataPlaneReady(value.AuthorityTick, LastSourceTick, ClockMicros());
                        using (var consumed = C2G_ServerAuthoritativeDataPlaneTicketConsumed.Create())
                        {
                            consumed.RoomId = Process.RoomId.Value;
                            consumed.SessionId = SessionId.Value;
                            consumed.PlayerId = Process.PlayerId.Value;
                            consumed.TicketId = Datagram.PredictionTicketId;
                            Control.Send(consumed);
                        }
                        TryBecomeReady();
                        break;
                    case ServerAuthoritativePredictionDatagramEventKind.Snapshot:
                        HandleCheckpointResult(m_Checkpoints.AcceptDelta(value.Snapshot, RequireRemoteActorId()));
                        break;
                    default:
                        throw new InvalidOperationException($"Unknown prediction datagram event '{value.Kind}'.");
                }
            }
        }

        public AuthoritativeObservationBatch DrainObservations(SimulationTickSourceIdentity source)
        {
            RequireOperational(source.SourceTick);
            PumpDataPlane();
            ServerAuthoritativePredictionLivenessResult liveness =
                m_Evidence.EvaluateLiveness(source.SourceTick, EndpointTimeoutTicks);
            if (!liveness.Success)
                Fail(liveness.Code, liveness.Message);
            RequireOperational();
            ActorId remoteActor = RequireRemoteActorId();
            ServerAuthoritativePredictionObservationResult result = m_Evidence.Drain(
                source,
                remoteActor,
                Datagram.CapturePredictionMetrics(),
                m_Checkpoints.CaptureMetrics());
            PublishPredictionEvidence(source.SourceTick, result.Report);
            return result.Batch;
        }

        public void AcknowledgeRemoteEvents(ulong eventHorizon)
        {
            RequireOperational();
            m_Evidence.AcknowledgeRemoteEvents(eventHorizon);
        }

        public void Send(Float32SourceEgressRecord record)
        {
            RequireOperational();
            PumpDataPlane();
            if (record == null || !string.Equals(record.ChannelId, ServerAuthoritativeEgressChannels.ClientInput, StringComparison.Ordinal) ||
                !string.Equals(record.SchemaId, ServerAuthoritativeEgressChannels.ClientInputSchema, StringComparison.Ordinal) ||
                record.SchemaVersion != ServerAuthoritativeEgressChannels.SchemaVersion)
            {
                throw new InvalidOperationException("Prediction Endpoint accepts only canonical ClientInput egress.");
            }
            OwnerCanonicalInputBatch input = ServerAuthoritativeEgressCodec.ReadOwnerInput(record.CopyPayload());
            if (input.ActorId != Process.ActorId)
                throw new InvalidOperationException("Prediction input egress does not belong to the local owner ActorId.");
            Datagram.SendPredictionCommand(input, Policy);
        }

        protected override void SendLeave()
        {
            if (!SessionId.IsValid)
                return;
            using var message = C2G_ServerAuthoritativeLeave.Create();
            message.RoomId = Process.RoomId.Value;
            message.SessionId = SessionId.Value;
            message.PlayerId = Process.PlayerId.Value;
            message.Reason = "prediction_source_disposed";
            Control.Send(message);
        }

        void RequestFullCheckpoint(string reason)
        {
            if (!m_Checkpoints.TryBeginFullCheckpointRequest())
                return;
            using var request = C2G_ServerAuthoritativeFullCheckpointRequest.Create();
            request.RoomId = Process.RoomId.Value;
            request.SessionId = SessionId.Value;
            request.PlayerId = Process.PlayerId.Value;
            request.ActorId = Process.ActorId.Value;
            request.LastUsableSnapshotSequence = m_Checkpoints.LatestSnapshotSequence;
            request.Reason = reason;
            Control.Send(request);
        }

        void HandleCheckpointResult(ServerAuthoritativeCheckpointResult result)
        {
            switch (result.Kind)
            {
                case ServerAuthoritativeCheckpointResultKind.Ignored:
                    return;
                case ServerAuthoritativeCheckpointResultKind.BaselineMissing:
                    RequestFullCheckpoint("unknown_delta_base");
                    return;
                case ServerAuthoritativeCheckpointResultKind.Accepted:
                    Datagram.AcceptLatestSnapshotSequence(result.SnapshotSequence);
                    m_Evidence.AcceptCheckpoint(result, LastSourceTick, ClockMicros());
                    return;
                default:
                    throw new InvalidOperationException($"Unknown checkpoint result '{result.Kind}'.");
            }
        }

        void PublishPredictionEvidence(
            ulong sourceTick,
            ServerAuthoritativePredictionEvidenceReport report)
        {
            if (!report.Available)
                return;
            UnityEngine.Debug.Log($"[ServerAuthoritative.Client] {report.Detail};sendQueue={Datagram.SendQueueDepth}");
            Diagnostics.PublishModel(new SimulationModelTraceRecord(
                SimulationModelTraceKind.Transport,
                "server_authoritative_client_stream_metrics",
                report.Detail,
                Process.ActorId,
                sourceTick,
                report.AuthorityEstimate,
                0,
                report.ConfirmedInputSequence,
                Datagram.ReceiveQueueDepth + Datagram.SendQueueDepth,
                0,
                report.SnapshotAgeTicks,
                0,
                report.Success));
        }

        ActorId RequireRemoteActorId()
        {
            ActorId remote = default;
            if (Roster == null)
                throw new InvalidOperationException("Prediction roster is not locked.");
            for (int i = 0; i < Roster.Count; i++)
            {
                if (Roster[i].ActorId == Process.ActorId)
                    continue;
                if (remote.IsValid)
                    throw new InvalidOperationException("Prediction roster contains more than one remote Actor.");
                remote = Roster[i].ActorId;
            }
            return remote.IsValid ? remote : throw new InvalidOperationException("Prediction roster contains no remote Actor.");
        }
    }

}
