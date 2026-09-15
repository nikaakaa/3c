using System;
using System.Collections.Generic;

namespace ThirdPersonGameplay.Networking.ServerAuthoritative
{
    internal sealed class ServerAuthoritativeConnectionCoordinator
    {
        readonly object m_CallbackGate = new object();
        readonly Queue<Action> m_Callbacks = new Queue<Action>();
        ServerAuthoritativeEndpointHandshake m_Handshake;
        bool m_CallbackIngressOpen = true;
        bool m_HandshakeTaken;
        bool m_Disposed;
        int m_PreparationTicks;

        public ServerAuthoritativeEndpointConnectionStatus Status { get; private set; } =
            ServerAuthoritativeEndpointConnectionStatus.Pending;
        public ServerAuthoritativeEndpointFailure Failure { get; private set; }
        public bool IsDisposed => m_Disposed;

        public void EnqueueCallback(Action callback)
        {
            if (callback == null)
                return;
            lock (m_CallbackGate)
            {
                if (!m_CallbackIngressOpen || Status == ServerAuthoritativeEndpointConnectionStatus.Failed)
                    return;
                m_Callbacks.Enqueue(callback);
            }
        }

        public bool TryTakeCallback(out Action callback)
        {
            lock (m_CallbackGate)
            {
                if (m_Callbacks.Count == 0)
                {
                    callback = null;
                    return false;
                }
                callback = m_Callbacks.Dequeue();
                return true;
            }
        }

        public bool AdvancePreparation(int timeoutTicks)
        {
            if (Status != ServerAuthoritativeEndpointConnectionStatus.Pending)
                return false;
            m_PreparationTicks++;
            return m_PreparationTicks > timeoutTicks;
        }

        public void MarkReady(ServerAuthoritativeEndpointHandshake handshake)
        {
            if (Status != ServerAuthoritativeEndpointConnectionStatus.Pending || handshake == null)
                throw new InvalidOperationException("Endpoint readiness transition is invalid.");
            m_Handshake = handshake;
            Status = ServerAuthoritativeEndpointConnectionStatus.Ready;
        }

        public ServerAuthoritativeEndpointHandshake TakeHandshake()
        {
            if (m_Disposed || Status != ServerAuthoritativeEndpointConnectionStatus.Ready ||
                m_Handshake == null || m_HandshakeTaken)
            {
                throw new InvalidOperationException("ServerAuthoritative Endpoint handshake is unavailable or was already consumed.");
            }
            m_HandshakeTaken = true;
            return m_Handshake;
        }

        public bool TryFail(string code, string message, out ServerAuthoritativeEndpointFailure failure)
        {
            if (m_Disposed || Status == ServerAuthoritativeEndpointConnectionStatus.Failed)
            {
                failure = Failure;
                return false;
            }
            Failure = failure = new ServerAuthoritativeEndpointFailure(code, message);
            Status = ServerAuthoritativeEndpointConnectionStatus.Failed;
            return true;
        }

        public bool BeginDispose()
        {
            if (m_Disposed)
                return false;
            m_Disposed = true;
            lock (m_CallbackGate)
            {
                m_CallbackIngressOpen = false;
                m_Callbacks.Clear();
            }
            m_Handshake = null;
            return true;
        }
    }
}
