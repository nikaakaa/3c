using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ThirdPersonPerformance.Runtime
{
    internal sealed class PerformanceLoopbackClient : IDisposable
    {
        readonly object m_Gate = new object();
        readonly Queue<string> m_Incoming = new Queue<string>();
        readonly Queue<string> m_Outgoing = new Queue<string>();
        readonly AutoResetEvent m_OutgoingReady = new AutoResetEvent(false);
        readonly int m_Port;
        readonly string m_Hello;
        readonly int m_ConnectTimeoutMilliseconds;

        TcpClient m_Client;
        NetworkStream m_Stream;
        StreamReader m_Reader;
        Thread m_ReaderThread;
        Thread m_WriterThread;
        bool m_Connected;
        bool m_Stopping;
        bool m_FaultPublished;
        bool m_WriteInProgress;

        public PerformanceLoopbackClient(int port, string hello, int connectTimeoutMilliseconds)
        {
            if (port <= 0 || port > ushort.MaxValue || string.IsNullOrWhiteSpace(hello) ||
                connectTimeoutMilliseconds <= 0)
            {
                throw new ArgumentException("Performance loopback client configuration is invalid.");
            }
            m_Port = port;
            m_Hello = hello;
            m_ConnectTimeoutMilliseconds = connectTimeoutMilliseconds;
        }

        public bool IsConnected
        {
            get
            {
                lock (m_Gate)
                    return m_Connected && !m_Stopping;
            }
        }

        public bool IsSendIdle
        {
            get
            {
                lock (m_Gate)
                    return m_Outgoing.Count == 0 && !m_WriteInProgress;
            }
        }

        public void Start()
        {
            lock (m_Gate)
            {
                if (m_ReaderThread != null)
                    throw new InvalidOperationException("Performance loopback client already started.");
                m_ReaderThread = new Thread(ReadLoop)
                {
                    IsBackground = true,
                    Name = "ThirdPerson Performance Loopback Reader"
                };
                m_ReaderThread.Start();
            }
        }

        public bool TryRead(out string message)
        {
            lock (m_Gate)
            {
                if (m_Incoming.Count == 0)
                {
                    message = string.Empty;
                    return false;
                }
                message = m_Incoming.Dequeue();
                return true;
            }
        }

        public void Send(string message)
        {
            if (string.IsNullOrWhiteSpace(message) || message.Length > 256)
                throw new InvalidDataException("Performance loopback message is invalid.");
            lock (m_Gate)
            {
                if (!m_Connected || m_Stopping)
                    return;
                m_Outgoing.Enqueue(message);
            }
            m_OutgoingReady.Set();
        }

        void ReadLoop()
        {
            try
            {
                var client = new TcpClient(AddressFamily.InterNetwork) { NoDelay = true };
                if (!client.ConnectAsync(IPAddress.Loopback, m_Port).Wait(m_ConnectTimeoutMilliseconds))
                    throw new TimeoutException("Performance Controller loopback connection timed out.");
                NetworkStream stream = client.GetStream();
                var reader = new StreamReader(stream, new UTF8Encoding(false), false, 1024, true);
                lock (m_Gate)
                {
                    if (m_Stopping)
                    {
                        reader.Dispose();
                        stream.Dispose();
                        client.Dispose();
                        return;
                    }
                    m_Client = client;
                    m_Stream = stream;
                    m_Reader = reader;
                    m_Outgoing.Enqueue(m_Hello);
                    m_Connected = true;
                    m_WriterThread = new Thread(WriteLoop)
                    {
                        IsBackground = true,
                        Name = "ThirdPerson Performance Loopback Writer"
                    };
                    m_WriterThread.Start();
                }
                m_OutgoingReady.Set();
                while (true)
                {
                    string line = reader.ReadLineAsync().GetAwaiter().GetResult();
                    if (line == null)
                        throw new EndOfStreamException("Performance Controller loopback connection closed.");
                    if (line.Length > 256)
                        throw new InvalidDataException("Performance Controller command exceeds the protocol limit.");
                    lock (m_Gate)
                        m_Incoming.Enqueue(line);
                }
            }
            catch (Exception exception)
            {
                PublishFault(exception);
            }
        }

        void WriteLoop()
        {
            try
            {
                while (true)
                {
                    m_OutgoingReady.WaitOne();
                    while (TryTakeOutgoing(out NetworkStream stream, out string message))
                    {
                        byte[] bytes = Encoding.UTF8.GetBytes(message + "\n");
                        stream.WriteAsync(bytes, 0, bytes.Length).GetAwaiter().GetResult();
                        CompleteWrite();
                    }
                    lock (m_Gate)
                    {
                        if (m_Stopping)
                            return;
                    }
                }
            }
            catch (Exception exception)
            {
                PublishFault(exception);
            }
        }

        bool TryTakeOutgoing(out NetworkStream stream, out string message)
        {
            lock (m_Gate)
            {
                if (m_Stopping || m_Outgoing.Count == 0 || m_Stream == null)
                {
                    stream = null;
                    message = string.Empty;
                    return false;
                }
                stream = m_Stream;
                message = m_Outgoing.Dequeue();
                m_WriteInProgress = true;
                return true;
            }
        }

        void CompleteWrite()
        {
            lock (m_Gate)
                m_WriteInProgress = false;
        }

        void PublishFault(Exception exception)
        {
            lock (m_Gate)
            {
                if (m_Stopping || m_FaultPublished)
                    return;
                m_FaultPublished = true;
                m_Connected = false;
                m_WriteInProgress = false;
                m_Outgoing.Clear();
                m_Incoming.Enqueue($"TRANSPORT_FAULT|{exception.Message}");
            }
        }

        public void Dispose()
        {
            TcpClient client;
            NetworkStream stream;
            StreamReader reader;
            lock (m_Gate)
            {
                if (m_Stopping)
                    return;
                m_Stopping = true;
                m_Connected = false;
                client = m_Client;
                stream = m_Stream;
                reader = m_Reader;
                m_Client = null;
                m_Stream = null;
                m_Reader = null;
            }
            m_OutgoingReady.Set();
            reader?.Dispose();
            stream?.Dispose();
            client?.Dispose();
            m_OutgoingReady.Dispose();
        }
    }
}
