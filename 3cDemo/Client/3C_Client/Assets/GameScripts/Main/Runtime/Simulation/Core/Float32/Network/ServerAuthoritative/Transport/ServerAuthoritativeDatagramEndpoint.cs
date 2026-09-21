using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace ThirdPersonSimulation.ServerAuthoritative.Transport
{
    public interface IServerAuthoritativeAuthorityDataTransport : IDisposable
    {
        IPEndPoint LocalEndPoint { get; }
        int ReceiveQueueDepth { get; }
        int SendQueueDepth { get; }
        bool IsFailed { get; }
        ServerAuthoritativeDatagramMetrics CaptureMetrics();
        void BindRemote(ServerAuthoritativeDatagramIdentity identity, IPEndPoint remoteEndPoint);
        void RevokeRemote(ServerAuthoritativeDatagramIdentity identity);
        void EnqueueSend(ServerAuthoritativeDatagramPacket packet);
        void PumpSend();
        bool TryReceive(out ServerAuthoritativeReceivedDatagram datagram);
        void ReturnReceiveEndPoint(IPEndPoint remoteEndPoint);
        void ReturnReceivedPacket(ServerAuthoritativeDatagramPacket packet);
        void ThrowIfUnavailable();
    }

    public readonly struct ServerAuthoritativeDatagramMetrics
    {
        public ServerAuthoritativeDatagramMetrics(
            long sentPackets,
            long sentBytes,
            long receivedPackets,
            long receivedBytes,
            long malformedDrops,
            long unknownRouteDrops,
            long oversizeDrops,
            long endpointMismatchDrops)
        {
            SentPackets = sentPackets;
            SentBytes = sentBytes;
            ReceivedPackets = receivedPackets;
            ReceivedBytes = receivedBytes;
            MalformedDrops = malformedDrops;
            UnknownRouteDrops = unknownRouteDrops;
            OversizeDrops = oversizeDrops;
            EndpointMismatchDrops = endpointMismatchDrops;
        }

        public long SentPackets { get; }
        public long SentBytes { get; }
        public long ReceivedPackets { get; }
        public long ReceivedBytes { get; }
        public long MalformedDrops { get; }
        public long UnknownRouteDrops { get; }
        public long OversizeDrops { get; }
        public long EndpointMismatchDrops { get; }
    }

    public readonly struct ServerAuthoritativeReceivedDatagram
    {
        public ServerAuthoritativeReceivedDatagram(ServerAuthoritativeDatagramPacket packet, IPEndPoint remoteEndPoint)
        {
            Packet = packet ?? throw new ArgumentNullException(nameof(packet));
            RemoteEndPoint = remoteEndPoint ?? throw new ArgumentNullException(nameof(remoteEndPoint));
        }

        public ServerAuthoritativeDatagramPacket Packet { get; }
        public IPEndPoint RemoteEndPoint { get; }
    }

    public sealed class ServerAuthoritativeDatagramEndpoint : IServerAuthoritativeAuthorityDataTransport
    {
        readonly Socket m_Socket;
        readonly Thread m_ReceiveThread;
        readonly ConcurrentQueue<ServerAuthoritativeReceivedDatagram> m_ReceiveQueue = new ConcurrentQueue<ServerAuthoritativeReceivedDatagram>();
        readonly ConcurrentStack<IPEndPoint> m_ReceiveEndPoints = new ConcurrentStack<IPEndPoint>();
        readonly ConcurrentStack<ServerAuthoritativeDatagramPacket> m_ReceivePackets = new ConcurrentStack<ServerAuthoritativeDatagramPacket>();
        readonly ConcurrentStack<byte[]> m_ReceivePayloads = new ConcurrentStack<byte[]>();
        readonly ConcurrentQueue<PendingSend> m_SendQueue = new ConcurrentQueue<PendingSend>();
        readonly ConcurrentStack<IPEndPoint> m_SendEndPoints = new ConcurrentStack<IPEndPoint>();
        readonly ConcurrentStack<byte[]> m_SendBuffers = new ConcurrentStack<byte[]>();
        readonly ThreadLocal<CanonicalWriter> m_SendWriter;
        readonly Dictionary<ServerAuthoritativeDatagramIdentity, IPEndPoint> m_Routes =
            new Dictionary<ServerAuthoritativeDatagramIdentity, IPEndPoint>();
        readonly object m_RouteLock = new object();
        readonly EndPoint m_ReceiveFromEndPoint;
        readonly int m_QueueCapacity;
        readonly int m_MaximumDatagramBytes;
        int m_ReceiveCount;
        int m_SendCount;
        int m_Disposed;
        Exception m_Failure;
        long m_SentPackets;
        long m_SentBytes;
        long m_ReceivedPackets;
        long m_ReceivedBytes;
        long m_MalformedDrops;
        long m_UnknownRouteDrops;
        long m_OversizeDrops;
        long m_EndpointMismatchDrops;

        public ServerAuthoritativeDatagramEndpoint(
            IPEndPoint localEndPoint,
            int queueCapacity,
            int maximumDatagramBytes)
        {
            if (localEndPoint == null)
                throw new ArgumentNullException(nameof(localEndPoint));
            if (queueCapacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(queueCapacity));
            if (maximumDatagramBytes < 256 || maximumDatagramBytes > 1200)
                throw new ArgumentOutOfRangeException(nameof(maximumDatagramBytes));
            m_QueueCapacity = queueCapacity;
            m_MaximumDatagramBytes = maximumDatagramBytes;
            m_SendWriter = new ThreadLocal<CanonicalWriter>(() => new CanonicalWriter(new byte[maximumDatagramBytes]));
            m_Socket = new Socket(localEndPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp)
            {
                ReceiveTimeout = 250,
                SendTimeout = 250
            };
            m_Socket.Bind(localEndPoint);
            LocalEndPoint = (IPEndPoint)m_Socket.LocalEndPoint;
            m_ReceiveFromEndPoint = LocalEndPoint.AddressFamily == AddressFamily.InterNetworkV6
                ? new IPEndPoint(IPAddress.IPv6Any, 0)
                : new IPEndPoint(IPAddress.Any, 0);
            m_ReceiveThread = new Thread(ReceiveLoop)
            {
                IsBackground = true,
                Name = "ServerAuthoritativeGameplayDatagram"
            };
            m_ReceiveThread.Start();
        }

        public IPEndPoint LocalEndPoint { get; }
        public int ReceiveQueueDepth => Volatile.Read(ref m_ReceiveCount);
        public int SendQueueDepth => Volatile.Read(ref m_SendCount);
        public bool IsFailed => Volatile.Read(ref m_Failure) != null;

        public ServerAuthoritativeDatagramMetrics CaptureMetrics() => new ServerAuthoritativeDatagramMetrics(
            Interlocked.Read(ref m_SentPackets),
            Interlocked.Read(ref m_SentBytes),
            Interlocked.Read(ref m_ReceivedPackets),
            Interlocked.Read(ref m_ReceivedBytes),
            Interlocked.Read(ref m_MalformedDrops),
            Interlocked.Read(ref m_UnknownRouteDrops),
            Interlocked.Read(ref m_OversizeDrops),
            Interlocked.Read(ref m_EndpointMismatchDrops));

        public void BindRemote(ServerAuthoritativeDatagramIdentity identity, IPEndPoint remoteEndPoint)
        {
            ThrowIfUnavailable();
            if (remoteEndPoint == null)
                throw new ArgumentNullException(nameof(remoteEndPoint));
            lock (m_RouteLock)
            {
                if (m_Routes.TryGetValue(identity, out IPEndPoint current))
                {
                    if (!EndPointEquals(current, remoteEndPoint))
                        throw new InvalidOperationException($"Gameplay data endpoint for '{identity}' cannot change while active.");
                    return;
                }
                m_Routes.Add(identity, Clone(remoteEndPoint));
            }
        }

        public void RevokeRemote(ServerAuthoritativeDatagramIdentity identity)
        {
            lock (m_RouteLock)
                m_Routes.Remove(identity);
        }

        public void EnqueueSend(ServerAuthoritativeDatagramPacket packet)
        {
            ThrowIfUnavailable();
            if (packet == null)
                throw new ArgumentNullException(nameof(packet));
            IPEndPoint remote;
            lock (m_RouteLock)
            {
                if (!m_Routes.TryGetValue(packet.Header.Identity, out remote))
                    throw new InvalidOperationException($"Gameplay data route '{packet.Header.Identity}' is not bound.");
            }
            CanonicalWriter writer = m_SendWriter.Value;
            int length = ServerAuthoritativeGameplayDatagramCodec.Write(packet, writer, m_MaximumDatagramBytes);
            byte[] bytes = RentSendBuffer();
            writer.WrittenSpan.CopyTo(bytes);
            if (Interlocked.Increment(ref m_SendCount) > m_QueueCapacity)
            {
                Interlocked.Decrement(ref m_SendCount);
                ReturnSendBuffer(bytes);
                Fail(new InvalidOperationException("Gameplay datagram send queue overflow."));
                ThrowIfUnavailable();
            }
            m_SendQueue.Enqueue(new PendingSend(bytes, length, RentSendEndPoint(remote)));
        }

        public void PumpSend()
        {
            ThrowIfUnavailable();
            while (m_SendQueue.TryDequeue(out PendingSend pending))
            {
                Interlocked.Decrement(ref m_SendCount);
                IPEndPoint sendEndPoint = pending.RemoteEndPoint;
                try
                {
                    try
                    {
                        int sent = m_Socket.SendTo(pending.Bytes, 0, pending.Length, SocketFlags.None, sendEndPoint);
                        if (sent != pending.Length)
                            throw new IOException($"Gameplay datagram send wrote '{sent}' of '{pending.Length}' bytes.");
                        Interlocked.Increment(ref m_SentPackets);
                        Interlocked.Add(ref m_SentBytes, sent);
                    }
                    finally
                    {
                        ReturnSendBuffer(pending.Bytes);
                        ReturnSendEndPoint(sendEndPoint);
                    }
                }
                catch (Exception exception) when (exception is SocketException || exception is ObjectDisposedException || exception is IOException)
                {
                    Fail(exception);
                    ThrowIfUnavailable();
                }
            }
        }

        public bool TryReceive(out ServerAuthoritativeReceivedDatagram datagram)
        {
            ThrowIfUnavailable();
            if (!m_ReceiveQueue.TryDequeue(out datagram))
                return false;
            Interlocked.Decrement(ref m_ReceiveCount);
            return true;
        }

        public void ReturnReceiveEndPoint(IPEndPoint remoteEndPoint)
        {
            if (m_ReceiveEndPoints.Count < m_QueueCapacity)
                m_ReceiveEndPoints.Push(remoteEndPoint);
        }

        public void ReturnReceivedPacket(ServerAuthoritativeDatagramPacket packet)
        {
            byte[] payload = packet.Release();
            if (m_ReceivePackets.Count < m_QueueCapacity)
                m_ReceivePackets.Push(packet);
            if (m_ReceivePayloads.Count < m_QueueCapacity)
                m_ReceivePayloads.Push(payload);
        }

        public void ThrowIfUnavailable()
        {
            if (Volatile.Read(ref m_Disposed) != 0)
                throw new ObjectDisposedException(nameof(ServerAuthoritativeDatagramEndpoint));
            Exception failure = Volatile.Read(ref m_Failure);
            if (failure != null)
                throw new InvalidOperationException("Gameplay datagram endpoint failed.", failure);
        }

        void ReceiveLoop()
        {
            var buffer = new byte[m_MaximumDatagramBytes + 1];
            while (Volatile.Read(ref m_Disposed) == 0 && Volatile.Read(ref m_Failure) == null)
            {
                EndPoint remote = m_ReceiveFromEndPoint;
                try
                {
                    int received = m_Socket.ReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref remote);
                    if (received <= 0)
                        continue;
                    if (received > m_MaximumDatagramBytes)
                    {
                        Interlocked.Increment(ref m_OversizeDrops);
                        continue;
                    }
                    ServerAuthoritativeDatagramPacket packet = RentReceivePacket();
                    byte[] payloadBuffer = RentReceivePayload();
                    try
                    {
                        packet = ServerAuthoritativeGameplayDatagramCodec.Read(
                            new ArraySegment<byte>(buffer, 0, received),
                            m_MaximumDatagramBytes,
                            packet,
                            payloadBuffer);
                    }
                    catch (Exception exception) when (exception is InvalidDataException || exception is ArgumentException)
                    {
                        ReturnReceivedPacket(packet);
                        Interlocked.Increment(ref m_MalformedDrops);
                        continue;
                    }
                    lock (m_RouteLock)
                    {
                        if (m_Routes.TryGetValue(packet.Header.Identity, out IPEndPoint expected))
                        {
                            if (!EndPointEquals(expected, (IPEndPoint)remote))
                            {
                                Interlocked.Increment(ref m_EndpointMismatchDrops);
                                Fail(new InvalidOperationException($"Gameplay data endpoint changed for '{packet.Header.Identity}'."));
                                ReturnReceivedPacket(packet);
                                continue;
                            }
                        }
                        else if (packet.Header.Kind != ServerAuthoritativeDatagramKind.DataPlaneHello)
                        {
                            Interlocked.Increment(ref m_UnknownRouteDrops);
                            ReturnReceivedPacket(packet);
                            continue;
                        }
                    }
                    IPEndPoint remoteEndPoint = RentReceiveEndPoint((IPEndPoint)remote);
                    if (Interlocked.Increment(ref m_ReceiveCount) > m_QueueCapacity)
                    {
                        Interlocked.Decrement(ref m_ReceiveCount);
                        ReturnReceiveEndPoint(remoteEndPoint);
                        ReturnReceivedPacket(packet);
                        Fail(new InvalidOperationException("Gameplay datagram receive queue overflow."));
                        continue;
                    }
                    m_ReceiveQueue.Enqueue(new ServerAuthoritativeReceivedDatagram(packet, remoteEndPoint));
                    Interlocked.Increment(ref m_ReceivedPackets);
                    Interlocked.Add(ref m_ReceivedBytes, received);
                }
                catch (SocketException exception) when (
                    exception.SocketErrorCode == SocketError.TimedOut ||
                    exception.SocketErrorCode == SocketError.WouldBlock ||
                    exception.SocketErrorCode == SocketError.Interrupted && Volatile.Read(ref m_Disposed) != 0)
                {
                }
                catch (ObjectDisposedException) when (Volatile.Read(ref m_Disposed) != 0)
                {
                }
                catch (Exception exception)
                {
                    if (Volatile.Read(ref m_Disposed) == 0)
                        Fail(exception);
                }
            }
        }

        void Fail(Exception exception)
        {
            if (exception == null)
                return;
            Interlocked.CompareExchange(ref m_Failure, exception, null);
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref m_Disposed, 1) != 0)
                return;
            try
            {
                m_Socket.Close();
            }
            catch
            {
            }
            if (m_ReceiveThread.IsAlive)
                m_ReceiveThread.Join(1000);
            while (m_ReceiveQueue.TryDequeue(out ServerAuthoritativeReceivedDatagram datagram))
            {
                ReturnReceivedPacket(datagram.Packet);
                ReturnReceiveEndPoint(datagram.RemoteEndPoint);
            }
            while (m_SendQueue.TryDequeue(out PendingSend pending))
            {
                ReturnSendBuffer(pending.Bytes);
                ReturnSendEndPoint(pending.RemoteEndPoint);
            }
            m_SendWriter.Dispose();
            Interlocked.Exchange(ref m_ReceiveCount, 0);
            Interlocked.Exchange(ref m_SendCount, 0);
            lock (m_RouteLock)
                m_Routes.Clear();
            m_Socket.Dispose();
        }

        static IPEndPoint Clone(IPEndPoint value) => new IPEndPoint(value.Address, value.Port);

        IPEndPoint RentReceiveEndPoint(IPEndPoint value)
        {
            if (!m_ReceiveEndPoints.TryPop(out IPEndPoint endpoint))
                endpoint = new IPEndPoint(value.Address, value.Port);
            endpoint.Address = value.Address;
            endpoint.Port = value.Port;
            return endpoint;
        }

        ServerAuthoritativeDatagramPacket RentReceivePacket()
        {
            if (m_ReceivePackets.TryPop(out ServerAuthoritativeDatagramPacket packet))
                return packet;
            return new ServerAuthoritativeDatagramPacket();
        }

        byte[] RentReceivePayload()
        {
            if (m_ReceivePayloads.TryPop(out byte[] payload))
                return payload;
            return new byte[m_MaximumDatagramBytes];
        }

        static bool EndPointEquals(IPEndPoint left, IPEndPoint right) =>
            left.Port == right.Port && left.Address.Equals(right.Address);

        IPEndPoint RentSendEndPoint(IPEndPoint value)
        {
            if (!m_SendEndPoints.TryPop(out IPEndPoint endpoint))
                endpoint = new IPEndPoint(value.Address, value.Port);
            endpoint.Address = value.Address;
            endpoint.Port = value.Port;
            return endpoint;
        }

        void ReturnSendEndPoint(IPEndPoint value)
        {
            if (m_SendEndPoints.Count < m_QueueCapacity)
                m_SendEndPoints.Push(value);
        }

        byte[] RentSendBuffer()
        {
            if (m_SendBuffers.TryPop(out byte[] buffer))
                return buffer;
            return new byte[m_MaximumDatagramBytes];
        }

        void ReturnSendBuffer(byte[] buffer)
        {
            if (m_SendBuffers.Count < m_QueueCapacity)
                m_SendBuffers.Push(buffer);
        }

        readonly struct PendingSend
        {
            public PendingSend(byte[] bytes, int length, IPEndPoint remoteEndPoint)
            {
                Bytes = bytes;
                Length = length;
                RemoteEndPoint = remoteEndPoint;
            }

            public byte[] Bytes { get; }
            public int Length { get; }
            public IPEndPoint RemoteEndPoint { get; }
        }
    }
}
