using System;
using Fantasy;
using Fantasy.Async;
using Fantasy.Network;

namespace ThirdPersonGameplay.Networking.Fantasy
{
    public abstract class FantasyClientSessionOwner : IDisposable
    {
        private Scene m_Scene;
        private Session m_Session;
        private bool m_Disposed;

        protected FantasyClientSessionOwner()
        {
            FantasyClientBootstrap.RegisterOwner();
        }

        public Session Session => m_Session;

        public bool IsConnected => m_Session is { IsDisposed: false };

        protected async FTask<Session> ConnectOwnedAsync(
            FantasyConnectionSettings settings,
            Action onConnectComplete,
            Action onConnectFail,
            Action onConnectDisconnect)
        {
            if (m_Disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }

            await FantasyClientBootstrap.InitializeAsync();
            DisconnectOwned();
            m_Scene = await Scene.Create();
            m_Session = m_Scene.Connect(
                $"{settings.Host}:{settings.Port}",
                settings.ToFantasyProtocol(),
                onConnectComplete,
                onConnectFail,
                onConnectDisconnect,
                settings.EnableHttps,
                settings.ConnectTimeoutMilliseconds);
            return m_Session;
        }

        protected void DisconnectOwned()
        {
            Session session = m_Session;
            Scene scene = m_Scene;
            m_Session = null;
            m_Scene = null;
            if (session is { IsDisposed: false })
            {
                session.Dispose();
            }

            if (scene is { IsDisposed: false })
            {
                scene.Dispose();
            }
        }

        public void Dispose()
        {
            if (m_Disposed)
            {
                return;
            }

            m_Disposed = true;
            OnDisposing();
            DisconnectOwned();
            FantasyClientBootstrap.UnregisterOwner();
        }

        protected virtual void OnDisposing()
        {
        }
    }
}
