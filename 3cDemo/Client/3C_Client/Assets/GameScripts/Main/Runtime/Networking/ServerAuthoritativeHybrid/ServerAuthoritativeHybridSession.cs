using System;
using System.Collections.Generic;
using ThirdPersonGameplay.Networking;

namespace ThirdPersonGameplay.Networking.ServerAuthoritativeHybrid
{
    public sealed class ServerAuthoritativeHybridSession : IGameplayNetworkModelSession
    {
        public const string StableModelId = "ServerAuthoritativeHybrid";

        readonly Queue<ServerAuthoritativePacket> m_Outgoing = new Queue<ServerAuthoritativePacket>();
        readonly Dictionary<string, Queue<ServerAuthoritativePacket>> m_IncomingBySubject =
            new Dictionary<string, Queue<ServerAuthoritativePacket>>(StringComparer.Ordinal);
        readonly HashSet<string> m_Bindings = new HashSet<string>(StringComparer.Ordinal);
        readonly ServerAuthoritativeDebug m_Debug;
        readonly ServerAuthoritativeHistory m_History;
        readonly IServerAuthoritativeEndpoint m_Endpoint;
        readonly int m_OutgoingCapacity;
        readonly int m_IncomingPerActorCapacity;

        ulong m_NextPacketId = 1;
        ulong m_LastPumpedLocalLogicTick;
        bool m_HasPumped;
        bool m_IsConfigurationLocked;
        bool m_Disposed;

        public ServerAuthoritativeHybridSession(
            string endpointId,
            IServerAuthoritativeEndpoint endpoint,
            int outgoingCapacity,
            int incomingPerActorCapacity,
            int historyCapacity,
            int debugCapacity)
        {
            if (outgoingCapacity < 1)
                throw new ArgumentOutOfRangeException(nameof(outgoingCapacity));
            if (incomingPerActorCapacity < 1)
                throw new ArgumentOutOfRangeException(nameof(incomingPerActorCapacity));
            if (historyCapacity < 1)
                throw new ArgumentOutOfRangeException(nameof(historyCapacity));
            if (debugCapacity < 1)
                throw new ArgumentOutOfRangeException(nameof(debugCapacity));
            if (string.IsNullOrWhiteSpace(endpointId) != (endpoint == null))
                throw new ArgumentException("Endpoint id and endpoint instance must either both be present or both be absent.");

            EndpointId = endpointId ?? string.Empty;
            m_Endpoint = endpoint;
            m_OutgoingCapacity = outgoingCapacity;
            m_IncomingPerActorCapacity = incomingPerActorCapacity;
            m_History = new ServerAuthoritativeHistory(historyCapacity);
            m_Debug = new ServerAuthoritativeDebug(debugCapacity);
        }

        public string ModelId => StableModelId;
        public bool IsConfigurationLocked => m_IsConfigurationLocked;
        public int BindingCount => m_Bindings.Count;
        public IReadOnlyCollection<string> BindingSubjectActorIds => m_Bindings;
        public string EndpointId { get; }
        public IServerAuthoritativeEndpoint Endpoint => m_Endpoint;
        public ServerAuthoritativeDebug Debug => m_Debug;
        public ServerAuthoritativeHistory History => m_History;
        public int PendingOutgoingCount => m_Outgoing.Count;
        public int PendingIncomingCount
        {
            get
            {
                int count = 0;
                foreach (Queue<ServerAuthoritativePacket> queue in m_IncomingBySubject.Values)
                    count += queue.Count;
                return count;
            }
        }

        public void LockConfiguration()
        {
            ThrowIfDisposed();
            m_IsConfigurationLocked = true;
        }

        public void RegisterBinding(string subjectActorId)
        {
            ThrowIfDisposed();
            if (string.IsNullOrWhiteSpace(subjectActorId))
                throw new ArgumentException("ServerAuthoritative binding requires SubjectActorId.", nameof(subjectActorId));
            if (!m_Bindings.Add(subjectActorId))
                throw new InvalidOperationException($"SubjectActorId '{subjectActorId}' already has a binding.");

            m_IncomingBySubject.Add(subjectActorId, new Queue<ServerAuthoritativePacket>());
            m_IsConfigurationLocked = true;
        }

        public void UnregisterBinding(string subjectActorId)
        {
            if (m_Disposed || string.IsNullOrEmpty(subjectActorId))
                return;

            m_Bindings.Remove(subjectActorId);
            m_IncomingBySubject.Remove(subjectActorId);
        }

        public void EnqueueOutgoing(ServerAuthoritativePacket packet)
        {
            ThrowIfDisposed();
            if (packet.Envelope.PacketKind == ServerAuthoritativePacketKind.None)
                throw new ArgumentException("Cannot enqueue an empty ServerAuthoritative packet.", nameof(packet));
            RequireRegisteredSubject(packet.Envelope.Identity.SubjectActorId);

            if (packet.Envelope.PacketId == 0)
                packet = packet.WithPacketId(NextPacketId());
            if (EnqueueOutgoingBounded(packet))
                m_History.Record(packet);
        }

        public void RecordPolicyDecision(ServerAuthoritativePolicyDecisionDebugRecord record)
        {
            ThrowIfDisposed();
            m_Debug.RecordPolicyDecision(record);
        }

        public void Pump(ulong localLogicTick)
        {
            ThrowIfDisposed();
            if (m_HasPumped)
            {
                if (localLogicTick == m_LastPumpedLocalLogicTick)
                    return;
                if (localLogicTick < m_LastPumpedLocalLogicTick)
                    throw new InvalidOperationException(
                        $"ServerAuthoritative session tick regressed from {m_LastPumpedLocalLogicTick} to {localLogicTick}.");
            }

            m_LastPumpedLocalLogicTick = localLogicTick;
            m_HasPumped = true;
            m_IsConfigurationLocked = true;
            if (m_Endpoint == null)
                return;

            m_Endpoint.Pump(localLogicTick);
            while (m_Endpoint.TryDequeueIncoming(out ServerAuthoritativePacket packet))
            {
                if (packet.Envelope.PacketId == 0)
                    packet = packet.WithPacketId(NextPacketId());

                RouteIncoming(packet);
            }

            SyncEndpointDebug();
        }

        public void FlushOutgoing()
        {
            ThrowIfDisposed();
            if (m_Endpoint == null)
            {
                while (m_Outgoing.Count != 0)
                    m_Debug.RecordDropped(m_Outgoing.Dequeue());
                return;
            }

            while (m_Outgoing.Count != 0)
            {
                ServerAuthoritativePacket packet = m_Outgoing.Dequeue();
                m_Debug.RecordOutgoing(packet);
                m_Endpoint.EnqueueOutgoing(packet);
            }

            SyncEndpointDebug();
        }

        public bool TryDequeueIncoming(string subjectActorId, out ServerAuthoritativePacket packet)
        {
            ThrowIfDisposed();
            RequireRegisteredSubject(subjectActorId);
            Queue<ServerAuthoritativePacket> queue = m_IncomingBySubject[subjectActorId];
            if (queue.Count != 0)
            {
                packet = queue.Dequeue();
                return true;
            }

            packet = default;
            return false;
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;

            m_Outgoing.Clear();
            m_IncomingBySubject.Clear();
            m_Bindings.Clear();
            m_Debug.Clear();
            m_History.Clear();
            if (m_Endpoint is IDisposable disposableEndpoint)
                disposableEndpoint.Dispose();
            m_Disposed = true;
        }

        void RouteIncoming(ServerAuthoritativePacket packet)
        {
            string subjectActorId = packet.Envelope.Identity.SubjectActorId;
            if (string.IsNullOrEmpty(subjectActorId) || !m_IncomingBySubject.TryGetValue(subjectActorId, out Queue<ServerAuthoritativePacket> queue))
            {
                m_Debug.RecordDropped(packet);
                return;
            }

            if (EnqueueIncomingBounded(queue, packet))
            {
                m_History.Record(packet);
                m_Debug.RecordIncoming(packet);
            }
        }

        bool EnqueueOutgoingBounded(ServerAuthoritativePacket packet)
        {
            if (m_Outgoing.Count < m_OutgoingCapacity)
            {
                m_Outgoing.Enqueue(packet);
                return true;
            }

            if (!IsReplaceableStream(packet.Envelope.PacketKind))
            {
                throw new InvalidOperationException(
                    $"ServerAuthoritative outgoing reliable queue overflowed while enqueuing '{packet.Envelope.PacketKind}'.");
            }

            if (RemoveOldestMatchingStream(m_Outgoing, packet, out ServerAuthoritativePacket replaced))
                m_Debug.RecordDropped(replaced);
            else
            {
                m_Debug.RecordDropped(packet);
                return false;
            }

            m_Outgoing.Enqueue(packet);
            return true;
        }

        bool EnqueueIncomingBounded(Queue<ServerAuthoritativePacket> queue, ServerAuthoritativePacket packet)
        {
            if (queue.Count < m_IncomingPerActorCapacity)
            {
                queue.Enqueue(packet);
                return true;
            }

            if (!IsReplaceableStream(packet.Envelope.PacketKind))
            {
                throw new InvalidOperationException(
                    $"ServerAuthoritative incoming reliable queue for '{packet.Envelope.Identity.SubjectActorId}' overflowed while routing '{packet.Envelope.PacketKind}'.");
            }

            if (RemoveOldestMatchingStream(queue, packet, out ServerAuthoritativePacket replaced))
            {
                m_Debug.RecordDropped(replaced);
                queue.Enqueue(packet);
                return true;
            }

            m_Debug.RecordDropped(packet);
            return false;
        }

        static bool RemoveOldestMatchingStream(
            Queue<ServerAuthoritativePacket> queue,
            ServerAuthoritativePacket incoming,
            out ServerAuthoritativePacket removed)
        {
            removed = default;
            bool found = false;
            int count = queue.Count;
            for (int i = 0; i < count; i++)
            {
                ServerAuthoritativePacket candidate = queue.Dequeue();
                if (!found &&
                    candidate.Envelope.PacketKind == incoming.Envelope.PacketKind &&
                    string.Equals(
                        candidate.Envelope.Identity.SubjectActorId,
                        incoming.Envelope.Identity.SubjectActorId,
                        StringComparison.Ordinal))
                {
                    removed = candidate;
                    found = true;
                    continue;
                }

                queue.Enqueue(candidate);
            }

            return found;
        }

        static bool IsReplaceableStream(ServerAuthoritativePacketKind packetKind)
        {
            return packetKind == ServerAuthoritativePacketKind.MotionCommand ||
                   packetKind == ServerAuthoritativePacketKind.MotionSnapshot;
        }

        void RequireRegisteredSubject(string subjectActorId)
        {
            if (string.IsNullOrEmpty(subjectActorId) || !m_Bindings.Contains(subjectActorId))
                throw new InvalidOperationException($"SubjectActorId '{subjectActorId}' has no ServerAuthoritative binding.");
        }

        ulong NextPacketId()
        {
            ulong packetId = m_NextPacketId++;
            if (m_NextPacketId == 0)
                m_NextPacketId = 1;
            return packetId;
        }

        void SyncEndpointDebug()
        {
            m_Debug.SetPending(m_Endpoint?.PendingDebugRecords);
            m_Debug.SetEndpointDropped(m_Endpoint?.DroppedDebugRecords);
        }

        void ThrowIfDisposed()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(ServerAuthoritativeHybridSession));
        }
    }
}
