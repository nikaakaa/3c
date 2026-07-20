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

    public sealed class ServerAuthoritativeReceivedDatagram
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
        readonly ConcurrentQueue<PendingSend> m_SendQueue = new ConcurrentQueue<PendingSend>();
        readonly Dictionary<ServerAuthoritativeDatagramIdentity, IPEndPoint> m_Routes =
            new Dictionary<ServerAuthoritativeDatagramIdentity, IPEndPoint>();
        readonly object m_RouteLock = new object();
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
            m_Socket = new Socket(localEndPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp)
            {
                ReceiveTimeout = 250,
                SendTimeout = 250
            };
            m_Socket.Bind(localEndPoint);
            LocalEndPoint = (IPEndPoint)m_Socket.LocalEndPoint;
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
                remote = Clone(remote);
            }
            byte[] bytes = ServerAuthoritativeGameplayDatagramCodec.Write(packet, m_MaximumDatagramBytes);
            if (Interlocked.Increment(ref m_SendCount) > m_QueueCapacity)
            {
                Interlocked.Decrement(ref m_SendCount);
                Fail(new InvalidOperationException("Gameplay datagram send queue overflow."));
                ThrowIfUnavailable();
            }
            m_SendQueue.Enqueue(new PendingSend(bytes, remote));
        }

        public void PumpSend()
        {
            ThrowIfUnavailable();
            while (m_SendQueue.TryDequeue(out PendingSend pending))
            {
                Interlocked.Decrement(ref m_SendCount);
                try
                {
                    int sent = m_Socket.SendTo(pending.Bytes, pending.RemoteEndPoint);
                    if (sent != pending.Bytes.Length)
                        throw new IOException($"Gameplay datagram send wrote '{sent}' of '{pending.Bytes.Length}' bytes.");
                    Interlocked.Increment(ref m_SentPackets);
                    Interlocked.Add(ref m_SentBytes, sent);
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
                EndPoint remote = LocalEndPoint.AddressFamily == AddressFamily.InterNetworkV6
                    ? new IPEndPoint(IPAddress.IPv6Any, 0)
                    : new IPEndPoint(IPAddress.Any, 0);
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
                    var bytes = new byte[received];
                    Buffer.BlockCopy(buffer, 0, bytes, 0, received);
                    ServerAuthoritativeDatagramPacket packet;
                    try
                    {
                        packet = ServerAuthoritativeGameplayDatagramCodec.Read(bytes, m_MaximumDatagramBytes);
                    }
                    catch (Exception exception) when (exception is InvalidDataException || exception is ArgumentException)
                    {
                        Interlocked.Increment(ref m_MalformedDrops);
                        continue;
                    }
                    var remoteEndPoint = Clone((IPEndPoint)remote);
                    lock (m_RouteLock)
                    {
                        if (m_Routes.TryGetValue(packet.Header.Identity, out IPEndPoint expected))
                        {
                            if (!EndPointEquals(expected, remoteEndPoint))
                            {
                                Interlocked.Increment(ref m_EndpointMismatchDrops);
                                Fail(new InvalidOperationException($"Gameplay data endpoint changed for '{packet.Header.Identity}'."));
                                continue;
                            }
                        }
                        else if (packet.Header.Kind != ServerAuthoritativeDatagramKind.DataPlaneHello)
                        {
                            Interlocked.Increment(ref m_UnknownRouteDrops);
                            continue;
                        }
                    }
                    if (Interlocked.Increment(ref m_ReceiveCount) > m_QueueCapacity)
                    {
                        Interlocked.Decrement(ref m_ReceiveCount);
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
            while (m_ReceiveQueue.TryDequeue(out _))
            {
            }
            while (m_SendQueue.TryDequeue(out _))
            {
            }
            Interlocked.Exchange(ref m_ReceiveCount, 0);
            Interlocked.Exchange(ref m_SendCount, 0);
            lock (m_RouteLock)
                m_Routes.Clear();
            m_Socket.Dispose();
        }

        static IPEndPoint Clone(IPEndPoint value) => new IPEndPoint(value.Address, value.Port);

        static bool EndPointEquals(IPEndPoint left, IPEndPoint right) =>
            left.Port == right.Port && left.Address.Equals(right.Address);

        readonly struct PendingSend
        {
            public PendingSend(byte[] bytes, IPEndPoint remoteEndPoint)
            {
                Bytes = bytes;
                RemoteEndPoint = remoteEndPoint;
            }

            public byte[] Bytes { get; }
            public IPEndPoint RemoteEndPoint { get; }
        }
    }
}
