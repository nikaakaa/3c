using System;
using System.Diagnostics;
using System.Threading;
using Cysharp.Threading.Tasks;
using GameLogic.ProductDiagnostics;
using ThirdPerson.ProductStartup;
using ThirdPersonGameplay.Networking.Fantasy;
using UnityEngine;

namespace GameLogic.ProductStartup
{
    internal sealed class FantasyProductAuthenticationFlow : IProductAuthenticationFlow, IDisposable
    {
        const string StartupServerProductId = "thirdperson.startup.server";
        const string ClientInstanceKey = "ThirdPerson.ProductStartup.ClientInstanceId.v1";

        readonly Uri _endpoint;
        readonly ProductStartupProfile _profile;
        readonly string _clientInstanceId;
        readonly NetworkRuntimeSnapshotStore _snapshots;
        ProductAuthSessionOwner _owner;
        CancellationTokenSource _eventPump;
        int _connectionGeneration;
        bool _disposed;

        public FantasyProductAuthenticationFlow(ProductStartupProfile profile)
        {
            _profile = profile ? profile : throw new ArgumentNullException(nameof(profile));
            if (!profile.TryValidate(out ProductStartupErrorCode code, out string safeError))
            {
                throw new InvalidOperationException($"Product startup profile is invalid: {code} {safeError}");
            }

            _endpoint = new Uri(profile.AuthEndpoint, UriKind.Absolute);
            _clientInstanceId = LoadOrCreateClientInstanceId();
            _snapshots = new NetworkRuntimeSnapshotStore(
                StartupServerProductId,
                _endpoint.Host,
                _clientInstanceId);
            _snapshots.Publish("Disconnected", string.Empty, 0, null, 0, string.Empty);
        }

        public INetworkRuntimeSnapshotSource NetworkSnapshots => _snapshots;

        public event Action<string> SessionReplaced;

        public async UniTask ConnectAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            await DisconnectAsync(CancellationToken.None);
            cancellationToken.ThrowIfCancellationRequested();
            await FantasyClientBootstrap.InitializeAsync();

            int generation = ++_connectionGeneration;
            var connected = new UniTaskCompletionSource();
            _owner = new ProductAuthSessionOwner(
                _endpoint,
                checked(_profile.RequestTimeoutSeconds * 1000));
            _snapshots.Publish("Connecting", string.Empty, 0, null, 0, string.Empty);
            try
            {
                await _owner.ConnectAsync(
                    () => connected.TrySetResult(),
                    () => connected.TrySetException(new InvalidOperationException("Auth Gateway connection failed.")),
                    () => OnDisconnected(generation));
                await connected.Task.AttachExternalCancellation(cancellationToken);
            }
            catch
            {
                if (generation == _connectionGeneration)
                {
                    _snapshots.Publish("Failed", string.Empty, 0, null, 0, "connect_failed");
                }
                DisposeOwner();
                throw;
            }

            cancellationToken.ThrowIfCancellationRequested();
            if (generation != _connectionGeneration || _owner == null)
            {
                throw new OperationCanceledException("Auth connection generation was replaced.");
            }

            _snapshots.Publish("Connected", string.Empty, 0, null, 0, string.Empty);
            _eventPump = new CancellationTokenSource();
            PumpEventsAsync(_owner, generation, _eventPump.Token).Forget();
        }

        public async UniTask<ProductAuthenticationSession> LoginGuestAsync(
            string guestAccountId,
            CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            ProductAuthSessionOwner owner = _owner ?? throw new InvalidOperationException("Auth Session is not connected.");
            if (string.IsNullOrWhiteSpace(guestAccountId))
            {
                throw new ArgumentException("Guest Account ID is required.", nameof(guestAccountId));
            }
            if (!_profile.TryGetAuthProtocolVersion(out AuthProtocolVersion protocolVersion))
            {
                throw new InvalidOperationException("Auth protocol version is invalid.");
            }

            var command = new GuestLoginCommand(
                guestAccountId.Trim(),
                _clientInstanceId,
                _profile.ClientBuildVersionText,
                checked((uint)protocolVersion.Value));
            var stopwatch = Stopwatch.StartNew();
            GuestLoginResult result = await owner.LoginAsync(command);
            stopwatch.Stop();
            cancellationToken.ThrowIfCancellationRequested();
            if (!result.Succeeded)
            {
                _snapshots.Publish(
                    "Connected",
                    string.Empty,
                    0,
                    null,
                    stopwatch.ElapsedMilliseconds,
                    result.Error.Code.ToString());
                throw new InvalidOperationException(result.Error.Message);
            }

            DateTimeOffset expiresAt = DateTimeOffset.FromUnixTimeMilliseconds(result.State.TokenExpiresAt);
            long sessionGeneration = checked((long)result.State.Generation);
            _snapshots.Publish(
                "Authenticated",
                result.State.AccountId,
                sessionGeneration,
                expiresAt,
                stopwatch.ElapsedMilliseconds,
                string.Empty);
            return new ProductAuthenticationSession(
                result.State.AccountId,
                sessionGeneration,
                expiresAt);
        }

        public UniTask DisconnectAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _connectionGeneration++;
            _eventPump?.Cancel();
            _eventPump?.Dispose();
            _eventPump = null;
            DisposeOwner();
            _snapshots.Publish("Disconnected", string.Empty, 0, null, 0, string.Empty);
            return UniTask.CompletedTask;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _connectionGeneration++;
            _eventPump?.Cancel();
            _eventPump?.Dispose();
            _eventPump = null;
            DisposeOwner();
        }

        async UniTaskVoid PumpEventsAsync(
            ProductAuthSessionOwner owner,
            int generation,
            CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested &&
                       generation == _connectionGeneration &&
                       ReferenceEquals(owner, _owner))
                {
                    while (owner.TryTakeEvent(out ProductAuthEvent authEvent))
                    {
                        owner.RevokeAuthentication();
                        _snapshots.Publish(
                            "Replaced",
                            string.Empty,
                            checked((long)authEvent.NewGeneration),
                            null,
                            0,
                            "session_replaced");
                        SessionReplaced?.Invoke(string.IsNullOrWhiteSpace(authEvent.Reason)
                            ? "This Guest Demo Identity was authenticated by another client."
                            : authEvent.Reason);
                    }

                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        void OnDisconnected(int generation)
        {
            if (generation == _connectionGeneration)
            {
                _snapshots.Publish("Disconnected", string.Empty, 0, null, 0, "connection_closed");
            }
        }

        void DisposeOwner()
        {
            ProductAuthSessionOwner owner = _owner;
            _owner = null;
            owner?.Dispose();
        }

        void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(FantasyProductAuthenticationFlow));
            }
        }

        static string LoadOrCreateClientInstanceId()
        {
            if (PlayerPrefs.HasKey(ClientInstanceKey))
            {
                string stored = PlayerPrefs.GetString(ClientInstanceKey, string.Empty);
                if (Guid.TryParseExact(stored, "N", out _))
                {
                    return stored;
                }

                throw new InvalidOperationException("Stored ClientInstanceId is invalid.");
            }

            string created = Guid.NewGuid().ToString("N");
            PlayerPrefs.SetString(ClientInstanceKey, created);
            PlayerPrefs.Save();
            return created;
        }
    }

    internal sealed class NetworkRuntimeSnapshotStore : INetworkRuntimeSnapshotSource
    {
        readonly string _productId;
        readonly string _endpointHost;
        readonly string _clientInstanceId;

        public NetworkRuntimeSnapshotStore(
            string productId,
            string endpointHost,
            string clientInstanceId)
        {
            _productId = productId;
            _endpointHost = endpointHost;
            _clientInstanceId = clientInstanceId;
        }

        public NetworkRuntimeSnapshot Current { get; private set; }

        public event Action<NetworkRuntimeSnapshot> Changed;

        public void Publish(
            string connectionState,
            string accountId,
            long generation,
            DateTimeOffset? tokenExpiresAt,
            long roundTripMilliseconds,
            string lastErrorCode)
        {
            Current = new NetworkRuntimeSnapshot(
                DateTimeOffset.UtcNow,
                _productId,
                "WebSocket",
                true,
                _endpointHost,
                connectionState,
                ProductDiagnosticRedaction.Identity(accountId),
                ProductDiagnosticRedaction.Identity(_clientInstanceId),
                generation,
                tokenExpiresAt,
                roundTripMilliseconds,
                lastErrorCode);
            Changed?.Invoke(Current);
        }
    }
}
