using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace ThirdPersonSimulation.DeterministicRollback
{
    internal readonly struct RollbackReceivedDatagram
    {
        public RollbackReceivedDatagram(RollbackDatagramPacket packet, IPEndPoint remoteEndPoint)
        {
            Packet = packet ?? throw new ArgumentNullException(nameof(packet));
            RemoteEndPoint = remoteEndPoint ?? throw new ArgumentNullException(nameof(remoteEndPoint));
        }

        public RollbackDatagramPacket Packet { get; }
        public IPEndPoint RemoteEndPoint { get; }
    }

    public sealed class RollbackDatagramEndpoint : IDisposable
    {
        readonly Socket m_Socket;
        readonly Thread m_ReceiveThread;
        readonly ConcurrentQueue<RollbackReceivedDatagram> m_ReceiveQueue = new ConcurrentQueue<RollbackReceivedDatagram>();
        readonly ConcurrentQueue<PendingSend> m_SendQueue = new ConcurrentQueue<PendingSend>();
        readonly ConcurrentStack<byte[]> m_SendBuffers = new ConcurrentStack<byte[]>();
        readonly ConcurrentStack<IPEndPoint> m_ReceiveEndPoints = new ConcurrentStack<IPEndPoint>();
        readonly ConcurrentStack<RollbackDatagramPacket> m_ReceivePackets = new ConcurrentStack<RollbackDatagramPacket>();
        readonly ConcurrentStack<byte[]> m_ReceivePayloads = new ConcurrentStack<byte[]>();
        readonly ThreadLocal<CanonicalWriter> m_SendWriter;
        readonly EndPoint m_ReceiveFromEndPoint;
        readonly int m_MaximumDatagramBytes;
        readonly int m_QueueCapacity;
        int m_ReceiveCount;
        int m_SendCount;
        int m_MaximumReceiveDepth;
        int m_MaximumSendDepth;
        int m_Disposed;
        long m_TotalReceivedDatagrams;
        long m_TotalSentDatagrams;
        long m_DroppedReceivedDatagrams;
        Exception m_Failure;

        public RollbackDatagramEndpoint(IPEndPoint localEndPoint, int queueCapacity, int maximumDatagramBytes)
        {
            if (localEndPoint == null)
                throw new ArgumentNullException(nameof(localEndPoint));
            if (queueCapacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(queueCapacity));
            if (maximumDatagramBytes < 256 || maximumDatagramBytes > 1200)
                throw new ArgumentOutOfRangeException(nameof(maximumDatagramBytes));
            m_MaximumDatagramBytes = maximumDatagramBytes;
            m_QueueCapacity = queueCapacity;
            m_SendWriter = new ThreadLocal<CanonicalWriter>(() => new CanonicalWriter(new byte[m_MaximumDatagramBytes]));
            m_ReceiveFromEndPoint = LocalEndPoint.AddressFamily == AddressFamily.InterNetworkV6
                ? new IPEndPoint(IPAddress.IPv6Any, 0)
                : new IPEndPoint(IPAddress.Any, 0);
            m_Socket = new Socket(localEndPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp)
            {
                ReceiveTimeout = 250,
                SendTimeout = 250
            };
            m_Socket.Bind(localEndPoint);
            LocalEndPoint = Clone((IPEndPoint)m_Socket.LocalEndPoint);
            m_ReceiveThread = new Thread(ReceiveLoop)
            {
                IsBackground = true,
                Name = "DeterministicRollbackDatagram"
            };
            m_ReceiveThread.Start();
        }

        public IPEndPoint LocalEndPoint { get; }
        public int ReceiveQueueDepth => Volatile.Read(ref m_ReceiveCount);
        public int SendQueueDepth => Volatile.Read(ref m_SendCount);
        public int MaximumReceiveQueueDepth => Volatile.Read(ref m_MaximumReceiveDepth);
        public int MaximumSendQueueDepth => Volatile.Read(ref m_MaximumSendDepth);
        public long TotalReceivedDatagrams => Interlocked.Read(ref m_TotalReceivedDatagrams);
        public long TotalSentDatagrams => Interlocked.Read(ref m_TotalSentDatagrams);
        public long DroppedReceivedDatagrams => Interlocked.Read(ref m_DroppedReceivedDatagrams);

        public void EnqueueSend(RollbackDatagramPacket packet, IPEndPoint remoteEndPoint)
        {
            ThrowIfUnavailable();
            if (packet == null)
                throw new ArgumentNullException(nameof(packet));
            if (remoteEndPoint == null)
                throw new ArgumentNullException(nameof(remoteEndPoint));
            CanonicalWriter writer = m_SendWriter.Value;
            int length = RollbackDatagramCodec.Write(packet, writer, m_MaximumDatagramBytes);
            byte[] bytes = RentSendBuffer();
            writer.WrittenSpan.CopyTo(bytes);
            if (Volatile.Read(ref m_SendCount) >= m_QueueCapacity)
                PumpSend();
            int sendDepth = Interlocked.Increment(ref m_SendCount);
            UpdateMaximum(ref m_MaximumSendDepth, sendDepth);
            if (sendDepth > m_QueueCapacity)
            {
                Interlocked.Decrement(ref m_SendCount);
                Fail(new InvalidOperationException("Rollback datagram send queue capacity is exhausted."));
                ThrowIfUnavailable();
            }
            m_SendQueue.Enqueue(new PendingSend(bytes, length, Clone(remoteEndPoint)));
        }

        public void PumpSend()
        {
            ThrowIfUnavailable();
            while (m_SendQueue.TryDequeue(out PendingSend pending))
            {
                Interlocked.Decrement(ref m_SendCount);
                try
                {
                    int sent;
                    try
                    {
                        sent = m_Socket.SendTo(pending.Bytes, 0, pending.Length, SocketFlags.None, pending.RemoteEndPoint);
                    }
                    finally
                    {
                        ReturnSendBuffer(pending.Bytes);
                    }
                    if (sent != pending.Length)
                        throw new IOException($"Rollback datagram wrote '{sent}' of '{pending.Length}' bytes.");
                    Interlocked.Increment(ref m_TotalSentDatagrams);
                }
                catch (Exception exception) when (exception is SocketException || exception is ObjectDisposedException || exception is IOException)
                {
                    Fail(exception);
                    ThrowIfUnavailable();
                }
            }
        }

        internal bool TryReceive(out RollbackReceivedDatagram datagram)
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
                throw new ObjectDisposedException(nameof(RollbackDatagramEndpoint));
            Exception failure = Volatile.Read(ref m_Failure);
            if (failure != null)
                throw new InvalidOperationException("Rollback datagram endpoint failed.", failure);
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
                    if (received <= 0 || received > m_MaximumDatagramBytes)
                        continue;
                    RollbackDatagramPacket packet = RentReceivePacket();
                    byte[] payloadBuffer = RentReceivePayload();
                    try
                    {
                        packet = RollbackDatagramCodec.Read(new ArraySegment<byte>(buffer, 0, received), m_MaximumDatagramBytes, packet, payloadBuffer);
                    }
                    catch (Exception exception) when (exception is InvalidDataException || exception is ArgumentException)
                    {
                        ReturnReceivedPacket(packet);
                        continue;
                    }
                    int receiveDepth = Interlocked.Increment(ref m_ReceiveCount);
                    UpdateMaximum(ref m_MaximumReceiveDepth, receiveDepth);
                    if (receiveDepth > m_QueueCapacity)
                    {
                        Interlocked.Decrement(ref m_ReceiveCount);
                        Interlocked.Increment(ref m_DroppedReceivedDatagrams);
                        ReturnReceivedPacket(packet);
                        continue;
                    }
                    m_ReceiveQueue.Enqueue(new RollbackReceivedDatagram(packet, RentReceiveEndPoint((IPEndPoint)remote)));
                    Interlocked.Increment(ref m_TotalReceivedDatagrams);
                }
                catch (SocketException exception) when (
                    exception.SocketErrorCode == SocketError.TimedOut ||
                    exception.SocketErrorCode == SocketError.WouldBlock ||
                    exception.SocketErrorCode == SocketError.ConnectionReset ||
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
            if (exception != null)
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
            while (m_ReceiveQueue.TryDequeue(out RollbackReceivedDatagram datagram))
            {
                ReturnReceivedPacket(datagram.Packet);
                ReturnReceiveEndPoint(datagram.RemoteEndPoint);
            }
            while (m_SendQueue.TryDequeue(out PendingSend pending))
            {
                ReturnSendBuffer(pending.Bytes);
            }
            m_SendWriter.Dispose();
            Interlocked.Exchange(ref m_ReceiveCount, 0);
            Interlocked.Exchange(ref m_SendCount, 0);
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

        internal void ReturnReceiveEndPoint(IPEndPoint value)
        {
            if (m_ReceiveEndPoints.Count < m_QueueCapacity)
                m_ReceiveEndPoints.Push(value);
        }

        RollbackDatagramPacket RentReceivePacket()
        {
            if (m_ReceivePackets.TryPop(out RollbackDatagramPacket packet))
                return packet;
            return new RollbackDatagramPacket();
        }

        byte[] RentReceivePayload()
        {
            if (m_ReceivePayloads.TryPop(out byte[] payload))
                return payload;
            return new byte[m_MaximumDatagramBytes];
        }

        internal void ReturnReceivedPacket(RollbackDatagramPacket packet)
        {
            byte[] payload = packet.Release();
            if (m_ReceivePackets.Count < m_QueueCapacity)
                m_ReceivePackets.Push(packet);
            if (m_ReceivePayloads.Count < m_QueueCapacity)
                m_ReceivePayloads.Push(payload);
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

        static void UpdateMaximum(ref int maximum, int value)
        {
            int current = Volatile.Read(ref maximum);
            while (value > current)
            {
                int observed = Interlocked.CompareExchange(ref maximum, value, current);
                if (observed == current)
                    return;
                current = observed;
            }
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
