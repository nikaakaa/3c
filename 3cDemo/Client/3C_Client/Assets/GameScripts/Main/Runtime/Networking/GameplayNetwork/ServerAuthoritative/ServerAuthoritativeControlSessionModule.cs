using System;
using System.Collections.Generic;
using System.Threading;
using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;
using ThirdPersonGameplay.Networking.Fantasy;
using ThirdPersonSimulation;
using ThirdPersonSimulation.ServerAuthoritative;

namespace ThirdPersonGameplay.Networking.ServerAuthoritative
{
    internal enum ServerAuthoritativeControlSessionEventKind : byte
    {
        TransportConnected = 1,
        SessionReady = 2,
        TransportFailed = 3,
        TransportDisconnected = 4
    }

    internal readonly struct ServerAuthoritativeControlSessionEvent
    {
        public ServerAuthoritativeControlSessionEvent(ServerAuthoritativeControlSessionEventKind kind, string detail)
        {
            Kind = kind;
            Detail = detail ?? string.Empty;
        }

        public ServerAuthoritativeControlSessionEventKind Kind { get; }
        public string Detail { get; }
    }

    internal readonly struct ServerAuthoritativeRosterUpdateResult
    {
        public ServerAuthoritativeRosterUpdateResult(
            bool changed,
            bool locked,
            ulong revision,
            IReadOnlyList<ServerAuthoritativeRosterEntry> roster)
        {
            Changed = changed;
            Locked = locked;
            Revision = revision;
            Roster = roster;
        }

        public bool Changed { get; }
        public bool Locked { get; }
        public ulong Revision { get; }
        public IReadOnlyList<ServerAuthoritativeRosterEntry> Roster { get; }
    }

    internal readonly struct ServerAuthoritativeHeartbeatAckResult
    {
        public ServerAuthoritativeHeartbeatAckResult(
            bool changed,
            ulong sequence,
            long clientUnixMilliseconds,
            long serverUnixMilliseconds,
            long roundTripMilliseconds,
            long jitterMilliseconds)
        {
            Changed = changed;
            Sequence = sequence;
            ClientUnixMilliseconds = clientUnixMilliseconds;
            ServerUnixMilliseconds = serverUnixMilliseconds;
            RoundTripMilliseconds = roundTripMilliseconds;
            JitterMilliseconds = jitterMilliseconds;
        }

        public bool Changed { get; }
        public ulong Sequence { get; }
        public long ClientUnixMilliseconds { get; }
        public long ServerUnixMilliseconds { get; }
        public long RoundTripMilliseconds { get; }
        public long JitterMilliseconds { get; }
    }

    internal readonly struct ServerAuthoritativeControlMetrics
    {
        public ServerAuthoritativeControlMetrics(
            long controlSentPackets,
            long controlReceivedPackets,
            long controlSentPayloadBytes,
            long controlReceivedPayloadBytes,
            long reliableSentPackets,
            long reliableReceivedPackets,
            long reliableSentPayloadBytes,
            long reliableReceivedPayloadBytes,
            long fullCheckpointSentPackets,
            long fullCheckpointReceivedPackets,
            long fullCheckpointSentPayloadBytes,
            long fullCheckpointReceivedPayloadBytes,
            ulong heartbeatSequence,
            ulong heartbeatAckSequence,
            long roundTripMilliseconds,
            long jitterMilliseconds)
        {
            ControlSentPackets = controlSentPackets;
            ControlReceivedPackets = controlReceivedPackets;
            ControlSentPayloadBytes = controlSentPayloadBytes;
            ControlReceivedPayloadBytes = controlReceivedPayloadBytes;
            ReliableSentPackets = reliableSentPackets;
            ReliableReceivedPackets = reliableReceivedPackets;
            ReliableSentPayloadBytes = reliableSentPayloadBytes;
            ReliableReceivedPayloadBytes = reliableReceivedPayloadBytes;
            FullCheckpointSentPackets = fullCheckpointSentPackets;
            FullCheckpointReceivedPackets = fullCheckpointReceivedPackets;
            FullCheckpointSentPayloadBytes = fullCheckpointSentPayloadBytes;
            FullCheckpointReceivedPayloadBytes = fullCheckpointReceivedPayloadBytes;
            HeartbeatSequence = heartbeatSequence;
            HeartbeatAckSequence = heartbeatAckSequence;
            RoundTripMilliseconds = roundTripMilliseconds;
            JitterMilliseconds = jitterMilliseconds;
        }

        public long ControlSentPackets { get; }
        public long ControlReceivedPackets { get; }
        public long ControlSentPayloadBytes { get; }
        public long ControlReceivedPayloadBytes { get; }
        public long ReliableSentPackets { get; }
        public long ReliableReceivedPackets { get; }
        public long ReliableSentPayloadBytes { get; }
        public long ReliableReceivedPayloadBytes { get; }
        public long FullCheckpointSentPackets { get; }
        public long FullCheckpointReceivedPackets { get; }
        public long FullCheckpointSentPayloadBytes { get; }
        public long FullCheckpointReceivedPayloadBytes { get; }
        public ulong HeartbeatSequence { get; }
        public ulong HeartbeatAckSequence { get; }
        public long RoundTripMilliseconds { get; }
        public long JitterMilliseconds { get; }
    }

    internal sealed class ServerAuthoritativeControlSessionModule
    {
        readonly object m_EventGate = new object();
        readonly Queue<ServerAuthoritativeControlSessionEvent> m_Events =
            new Queue<ServerAuthoritativeControlSessionEvent>();
        readonly ServerAuthoritativeFantasyEndpointDefinition m_Definition;
        readonly ServerAuthoritativeProcessIdentity m_Process;
        readonly int m_TickRate;
        readonly ServerAuthoritativeControlSessionOwner m_SessionOwner =
            new ServerAuthoritativeControlSessionOwner();
        Session m_Session;
        ServerAuthoritativeSessionId m_SessionId;
        IReadOnlyList<ServerAuthoritativeRosterEntry> m_Roster;
        ulong m_RosterRevision;
        ulong m_LastHeartbeatSourceTick;
        ulong m_HeartbeatSequence;
        ulong m_LastHeartbeatAckSequence;
        long m_ControlSentPackets;
        long m_ControlReceivedPackets;
        long m_ControlSentPayloadBytes;
        long m_ControlReceivedPayloadBytes;
        long m_ReliableSentPackets;
        long m_ReliableReceivedPackets;
        long m_ReliableSentPayloadBytes;
        long m_ReliableReceivedPayloadBytes;
        long m_FullCheckpointSentPackets;
        long m_FullCheckpointReceivedPackets;
        long m_FullCheckpointSentPayloadBytes;
        long m_FullCheckpointReceivedPayloadBytes;
        long m_LastControlRttMilliseconds;
        long m_LastControlJitterMilliseconds;
        bool m_TransportConnected;
        bool m_HandshakeStarted;
        bool m_Disposed;

        public ServerAuthoritativeControlSessionModule(
            ServerAuthoritativeFantasyEndpointDefinition definition,
            ServerAuthoritativeProcessIdentity process,
            int tickRate)
        {
            m_Definition = definition ? definition : throw new ArgumentNullException(nameof(definition));
            m_Process = process;
            m_TickRate = tickRate > 0 ? tickRate : throw new ArgumentOutOfRangeException(nameof(tickRate));
        }

        public ServerAuthoritativeSessionId SessionId => m_SessionId;
        public IReadOnlyList<ServerAuthoritativeRosterEntry> Roster => m_Roster;
        public ulong RosterRevision => m_RosterRevision;
        public bool HasSession => m_Session is { IsDisposed: false };
        public bool IsSessionDisposed => m_Session is { IsDisposed: true };

        public async FTask<G2W_ServerAuthoritativeAuthorityRegisterResponse> RegisterAuthorityAsync(
            W2G_ServerAuthoritativeAuthorityRegisterRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            G2W_ServerAuthoritativeAuthorityRegisterResponse response =
                await RequireSession().Call(request) as G2W_ServerAuthoritativeAuthorityRegisterResponse ??
                throw new InvalidOperationException("Fantasy worker register returned an unexpected response type.");
            RecordControlSent();
            RecordControlReceived();
            return response;
        }

        public async FTask<G2C_ServerAuthoritativeClientJoinResponse> JoinPredictionAsync(
            C2G_ServerAuthoritativeClientJoinRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            G2C_ServerAuthoritativeClientJoinResponse response =
                await RequireSession().Call(request) as G2C_ServerAuthoritativeClientJoinResponse ??
                throw new InvalidOperationException("Fantasy client join returned an unexpected response type.");
            RecordControlSent();
            RecordControlReceived();
            return response;
        }

        public void Send<T>(T message, int payloadBytes = 0) where T : IMessage
        {
            if (ReferenceEquals(message, null))
                throw new ArgumentNullException(nameof(message));
            RequireSession().Send(message);
            RecordControlSent(payloadBytes);
        }

        public void SendReliable<T>(T message, int payloadBytes) where T : IMessage
        {
            if (ReferenceEquals(message, null))
                throw new ArgumentNullException(nameof(message));
            RequireSession().Send(message);
            RecordReliableSent(payloadBytes);
        }

        public void SendFullCheckpoint<T>(T message, int payloadBytes) where T : IMessage
        {
            if (ReferenceEquals(message, null))
                throw new ArgumentNullException(nameof(message));
            RequireSession().Send(message);
            RecordFullCheckpointSent(payloadBytes);
        }

        public void Start(ServerAuthoritativeFantasyConnection owner)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));
            ConnectAsync(owner).Coroutine();
        }

        public bool TryBeginHandshake()
        {
            if (!m_TransportConnected || m_Session == null || m_Session.IsDisposed || m_HandshakeStarted)
                return false;
            m_HandshakeStarted = true;
            return true;
        }

        public bool TryTakeEvent(out ServerAuthoritativeControlSessionEvent value)
        {
            lock (m_EventGate)
            {
                if (m_Events.Count == 0)
                {
                    value = default;
                    return false;
                }
                value = m_Events.Dequeue();
                return true;
            }
        }

        public void SetSessionId(string value)
        {
            var sessionId = new ServerAuthoritativeSessionId(value);
            if (m_SessionId.IsValid && !m_SessionId.Equals(sessionId))
                throw new InvalidOperationException("Fantasy response changed the SessionId.");
            m_SessionId = sessionId;
        }

        public ServerAuthoritativeRosterUpdateResult AcceptRoster(
            ServerAuthoritativeRosterMessage message,
            ServerAuthoritativeAuthorityHostIdentity authorityHost)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));
            RecordControlReceived();
            if (!string.Equals(message.RoomId, m_Process.RoomId.Value, StringComparison.Ordinal) ||
                string.IsNullOrWhiteSpace(message.SessionId) || string.IsNullOrWhiteSpace(message.HostId) ||
                message.Revision == 0 || message.Members == null || message.Members.Count > 2)
            {
                throw new InvalidOperationException("Fantasy roster identity or member boundary is invalid.");
            }
            var sessionId = new ServerAuthoritativeSessionId(message.SessionId);
            if (m_SessionId.IsValid && !m_SessionId.Equals(sessionId))
                throw new InvalidOperationException("Fantasy roster SessionId changed while active.");
            m_SessionId = sessionId;
            if (message.Revision < m_RosterRevision)
                return new ServerAuthoritativeRosterUpdateResult(false, m_Roster != null, m_RosterRevision, m_Roster);
            var entries = new ServerAuthoritativeRosterEntry[message.Members.Count];
            for (int i = 0; i < entries.Length; i++)
            {
                ServerAuthoritativeRosterMemberMessage member = message.Members[i] ??
                    throw new InvalidOperationException("Fantasy roster contains an empty member.");
                entries[i] = new ServerAuthoritativeRosterEntry(
                    new ServerAuthoritativePlayerId(member.PlayerId),
                    new ActorId(member.ActorId),
                    (ServerAuthoritativeProcessRole)member.ProcessRole);
            }
            Array.Sort(entries);
            for (int i = 1; i < entries.Length; i++)
            {
                if (entries[i - 1].ActorId == entries[i].ActorId || entries[i - 1].PlayerId.Equals(entries[i].PlayerId))
                    throw new InvalidOperationException("Fantasy roster contains duplicate PlayerId or ActorId entries.");
            }
            if (!message.Locked)
            {
                if (entries.Length == 2 || m_Roster != null)
                    throw new InvalidOperationException("Fantasy roster attempted to unlock a complete or already locked Session.");
                m_RosterRevision = message.Revision;
                return new ServerAuthoritativeRosterUpdateResult(true, false, m_RosterRevision, null);
            }
            if (entries.Length != 2)
                throw new InvalidOperationException("Locked Fantasy roster does not contain exactly two clients.");
            if (!authorityHost.IsValid || !string.Equals(message.HostId, authorityHost.HostId, StringComparison.Ordinal))
                throw new InvalidOperationException("Fantasy roster Authority Host identity does not match the accepted handshake.");
            m_RosterRevision = message.Revision;
            m_Roster = entries;
            return new ServerAuthoritativeRosterUpdateResult(true, true, m_RosterRevision, m_Roster);
        }

        public ServerAuthoritativeHeartbeatAckResult AcceptHeartbeatAck(
            string roomId,
            string sessionId,
            ulong sequence,
            long clientUnixMilliseconds,
            long serverUnixMilliseconds)
        {
            RequireIdentity(roomId, sessionId);
            if (sequence == 0 || sequence > m_HeartbeatSequence ||
                clientUnixMilliseconds <= 0 || serverUnixMilliseconds <= 0)
            {
                throw new InvalidOperationException("Fantasy control heartbeat acknowledgement is invalid.");
            }
            if (sequence <= m_LastHeartbeatAckSequence)
                return new ServerAuthoritativeHeartbeatAckResult(false, sequence, clientUnixMilliseconds, serverUnixMilliseconds, 0, 0);
            m_LastHeartbeatAckSequence = sequence;
            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            long rtt = Math.Max(0, now - clientUnixMilliseconds);
            long previousRtt = Interlocked.Exchange(ref m_LastControlRttMilliseconds, rtt);
            long jitter = previousRtt == 0 ? 0 : Math.Abs(rtt - previousRtt);
            Interlocked.Exchange(ref m_LastControlJitterMilliseconds, jitter);
            RecordControlReceived();
            return new ServerAuthoritativeHeartbeatAckResult(
                true,
                sequence,
                clientUnixMilliseconds,
                serverUnixMilliseconds,
                rtt,
                jitter);
        }

        public void RequireIdentity(string roomId, string sessionId)
        {
            if (!string.Equals(roomId, m_Process.RoomId.Value, StringComparison.Ordinal) ||
                !string.Equals(sessionId, m_SessionId.Value, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Fantasy control message targets another Room or Session.");
            }
        }

        public void PumpHeartbeat(ulong sourceTick, ServerAuthoritativeAuthorityHostIdentity authorityHost)
        {
            if (m_Session == null || m_Session.IsDisposed || !m_SessionId.IsValid || sourceTick == 0)
                return;
            if (m_LastHeartbeatSourceTick != 0 && sourceTick < m_LastHeartbeatSourceTick)
                throw new InvalidOperationException("Fantasy control heartbeat source Tick regressed.");
            if (m_LastHeartbeatSourceTick != 0 &&
                sourceTick - m_LastHeartbeatSourceTick < (ulong)m_Definition.ControlHeartbeatTicks)
            {
                return;
            }
            m_LastHeartbeatSourceTick = sourceTick;
            ulong sequence = ++m_HeartbeatSequence;
            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (m_Process.IsAuthority)
            {
                using var heartbeat = W2G_ServerAuthoritativeControlHeartbeat.Create();
                heartbeat.RoomId = m_Process.RoomId.Value;
                heartbeat.SessionId = m_SessionId.Value;
                heartbeat.HostId = authorityHost.HostId;
                heartbeat.Sequence = sequence;
                heartbeat.ClientUnixMilliseconds = now;
                m_Session.Send(heartbeat);
            }
            else
            {
                using var heartbeat = C2G_ServerAuthoritativeControlHeartbeat.Create();
                heartbeat.RoomId = m_Process.RoomId.Value;
                heartbeat.SessionId = m_SessionId.Value;
                heartbeat.PlayerId = m_Process.PlayerId.Value;
                heartbeat.Sequence = sequence;
                heartbeat.ClientUnixMilliseconds = now;
                m_Session.Send(heartbeat);
            }
            RecordControlSent();
        }

        public void RecordControlSent(int payloadBytes = 0) => Record(ref m_ControlSentPackets, ref m_ControlSentPayloadBytes, payloadBytes);
        public void RecordControlReceived(int payloadBytes = 0) => Record(ref m_ControlReceivedPackets, ref m_ControlReceivedPayloadBytes, payloadBytes);
        public void RecordReliableSent(int payloadBytes) => Record(ref m_ReliableSentPackets, ref m_ReliableSentPayloadBytes, payloadBytes);
        public void RecordReliableReceived(int payloadBytes) => Record(ref m_ReliableReceivedPackets, ref m_ReliableReceivedPayloadBytes, payloadBytes);
        public void RecordFullCheckpointSent(int payloadBytes) => Record(ref m_FullCheckpointSentPackets, ref m_FullCheckpointSentPayloadBytes, payloadBytes);
        public void RecordFullCheckpointReceived(int payloadBytes) => Record(ref m_FullCheckpointReceivedPackets, ref m_FullCheckpointReceivedPayloadBytes, payloadBytes);

        public ServerAuthoritativeControlMetrics CaptureMetrics() => new ServerAuthoritativeControlMetrics(
            Interlocked.Read(ref m_ControlSentPackets),
            Interlocked.Read(ref m_ControlReceivedPackets),
            Interlocked.Read(ref m_ControlSentPayloadBytes),
            Interlocked.Read(ref m_ControlReceivedPayloadBytes),
            Interlocked.Read(ref m_ReliableSentPackets),
            Interlocked.Read(ref m_ReliableReceivedPackets),
            Interlocked.Read(ref m_ReliableSentPayloadBytes),
            Interlocked.Read(ref m_ReliableReceivedPayloadBytes),
            Interlocked.Read(ref m_FullCheckpointSentPackets),
            Interlocked.Read(ref m_FullCheckpointReceivedPackets),
            Interlocked.Read(ref m_FullCheckpointSentPayloadBytes),
            Interlocked.Read(ref m_FullCheckpointReceivedPayloadBytes),
            m_HeartbeatSequence,
            m_LastHeartbeatAckSequence,
            Interlocked.Read(ref m_LastControlRttMilliseconds),
            Interlocked.Read(ref m_LastControlJitterMilliseconds));

        Session RequireSession() => HasSession
            ? m_Session
            : throw new InvalidOperationException("Fantasy control Session is unavailable.");

        public void Dispose(ServerAuthoritativeFantasyConnection owner, Action sendLeave)
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            Session session = m_Session;
            if (session is { IsDisposed: false })
            {
                ServerAuthoritativeEndpointSessionBinding binding = session.GetComponent<ServerAuthoritativeEndpointSessionBinding>();
                if (binding != null && ReferenceEquals(binding.Runtime, owner))
                    binding.Runtime = null;
                sendLeave?.Invoke();
            }
            m_SessionOwner.Dispose();
            m_Session = null;
            m_Roster = null;
            lock (m_EventGate)
                m_Events.Clear();
        }

        async FTask ConnectAsync(ServerAuthoritativeFantasyConnection owner)
        {
            try
            {
                Session session = await m_SessionOwner.ConnectAsync(
                    m_Definition.Host,
                    m_Definition.Port,
                    checked(m_Definition.ConnectTimeoutTicks * 1000 / m_TickRate),
                    () => EnqueueEvent(ServerAuthoritativeControlSessionEventKind.TransportConnected, string.Empty),
                    () => EnqueueEvent(ServerAuthoritativeControlSessionEventKind.TransportFailed, "Fantasy control transport connection failed."),
                    () => EnqueueEvent(ServerAuthoritativeControlSessionEventKind.TransportDisconnected, "Fantasy control transport disconnected."));
                if (m_Disposed)
                {
                    m_SessionOwner.Dispose();
                    return;
                }
                m_Session = session ?? throw new InvalidOperationException("Fantasy connection returned no Session.");
                var binding = m_Session.AddComponent<ServerAuthoritativeEndpointSessionBinding>();
                binding.Runtime = owner;
                EnqueueEvent(ServerAuthoritativeControlSessionEventKind.SessionReady, string.Empty);
            }
            catch (Exception exception)
            {
                EnqueueEvent(ServerAuthoritativeControlSessionEventKind.TransportFailed, exception.Message);
            }
        }

        void EnqueueEvent(ServerAuthoritativeControlSessionEventKind kind, string detail)
        {
            lock (m_EventGate)
            {
                if (m_Disposed)
                    return;
                if (kind == ServerAuthoritativeControlSessionEventKind.TransportConnected)
                    m_TransportConnected = true;
                m_Events.Enqueue(new ServerAuthoritativeControlSessionEvent(kind, detail));
            }
        }

        static void Record(ref long packets, ref long bytes, int payloadBytes)
        {
            if (payloadBytes < 0)
                throw new ArgumentOutOfRangeException(nameof(payloadBytes));
            Interlocked.Increment(ref packets);
            Interlocked.Add(ref bytes, payloadBytes);
        }
    }
}
