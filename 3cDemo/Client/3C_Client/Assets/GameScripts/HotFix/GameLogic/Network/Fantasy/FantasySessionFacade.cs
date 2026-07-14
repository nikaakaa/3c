using System;
using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Platform.Unity;

namespace GameLogic.Network.Fantasy
{
    public sealed class FantasySessionFacade
    {
        private Scene _scene;

        public Session Session { get; private set; }

        public bool IsConnected => Session is { IsDisposed: false };

        public async FTask<Session> ConnectAsync(
            FantasyConnectionSettings settings,
            Action onConnectComplete = null,
            Action onConnectFail = null,
            Action onConnectDisconnect = null)
        {
            await FantasyClientBootstrap.InitializeAsync();
            Disconnect();

            _scene = await Entry.CreateScene();
            Session = _scene.Connect(
                $"{settings.Host}:{settings.Port}",
                settings.ToFantasyProtocol(),
                onConnectComplete,
                onConnectFail,
                onConnectDisconnect,
                settings.EnableHttps,
                settings.ConnectTimeoutMilliseconds);

            return Session;
        }

        public void Disconnect()
        {
            if (Session != null)
            {
                Session.Dispose();
                Session = null;
            }

            if (_scene != null)
            {
                _scene.Dispose();
                _scene = null;
            }
        }
    }
}
