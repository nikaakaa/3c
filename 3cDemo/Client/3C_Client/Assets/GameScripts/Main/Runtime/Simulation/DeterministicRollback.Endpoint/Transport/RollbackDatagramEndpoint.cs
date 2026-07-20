using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace ThirdPersonSimulation.DeterministicRollback
{
    public sealed class RollbackReceivedDatagram
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
            byte[] bytes = RollbackDatagramCodec.Write(packet, m_MaximumDatagramBytes);
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
            m_SendQueue.Enqueue(new PendingSend(bytes, Clone(remoteEndPoint)));
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
                        throw new IOException($"Rollback datagram wrote '{sent}' of '{pending.Bytes.Length}' bytes.");
                    Interlocked.Increment(ref m_TotalSentDatagrams);
                }
                catch (Exception exception) when (exception is SocketException || exception is ObjectDisposedException || exception is IOException)
                {
                    Fail(exception);
                    ThrowIfUnavailable();
                }
            }
        }

        public bool TryReceive(out RollbackReceivedDatagram datagram)
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
                EndPoint remote = LocalEndPoint.AddressFamily == AddressFamily.InterNetworkV6
                    ? new IPEndPoint(IPAddress.IPv6Any, 0)
                    : new IPEndPoint(IPAddress.Any, 0);
                try
                {
                    int received = m_Socket.ReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref remote);
                    if (received <= 0 || received > m_MaximumDatagramBytes)
                        continue;
                    var bytes = new byte[received];
                    Buffer.BlockCopy(buffer, 0, bytes, 0, received);
                    RollbackDatagramPacket packet;
                    try
                    {
                        packet = RollbackDatagramCodec.Read(bytes, m_MaximumDatagramBytes);
                    }
                    catch (Exception exception) when (exception is InvalidDataException || exception is ArgumentException)
                    {
                        continue;
                    }
                    int receiveDepth = Interlocked.Increment(ref m_ReceiveCount);
                    UpdateMaximum(ref m_MaximumReceiveDepth, receiveDepth);
                    if (receiveDepth > m_QueueCapacity)
                    {
                        Interlocked.Decrement(ref m_ReceiveCount);
                        Interlocked.Increment(ref m_DroppedReceivedDatagrams);
                        continue;
                    }
                    m_ReceiveQueue.Enqueue(new RollbackReceivedDatagram(packet, Clone((IPEndPoint)remote)));
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
            while (m_ReceiveQueue.TryDequeue(out _))
            {
            }
            while (m_SendQueue.TryDequeue(out _))
            {
            }
            Interlocked.Exchange(ref m_ReceiveCount, 0);
            Interlocked.Exchange(ref m_SendCount, 0);
            m_Socket.Dispose();
        }

        static IPEndPoint Clone(IPEndPoint value) => new IPEndPoint(value.Address, value.Port);

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
