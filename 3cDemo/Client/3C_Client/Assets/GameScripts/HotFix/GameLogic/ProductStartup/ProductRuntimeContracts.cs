using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using GameLogic.ProductDiagnostics;
using GameLogic.ProductResource;
using ThirdPerson.ProductStartup;

namespace GameLogic.ProductStartup
{
    public enum ProductAuthState
    {
        Disconnected = 0,
        Connecting = 1,
        AwaitingGuestLogin = 2,
        Authenticating = 3,
        Authenticated = 4,
        Replaced = 5,
        Failed = 6
    }

    public enum ProductHomeState
    {
        Unavailable = 0,
        Preloading = 1,
        Ready = 2,
        Failed = 3,
        Disposed = 4
    }

    public enum ProductGameplayState
    {
        Unavailable = 0,
        PlanningDownload = 1,
        AwaitingDownloadConsent = 2,
        Downloading = 3,
        Preloading = 4,
        EnteringScene = 5,
        Ready = 6,
        ReturningHome = 7,
        Failed = 8
    }

    public enum ProductRuntimeStage
    {
        None = 0,
        LoadProductShell = 1,
        ConnectAuthGateway = 2,
        AwaitGuestLogin = 3,
        PreloadHome = 4,
        HomeReady = 5,
        PlanGameplayDownload = 6,
        DownloadGameplay = 7,
        PreloadGameplay = 8,
        EnterGameplay = 9,
        GameplayReady = 10,
        ReturnHome = 11,
        Failed = 12,
        Disposed = 13
    }

    public sealed class ProductAuthenticationSession
    {
        public ProductAuthenticationSession(string accountId, long generation, DateTimeOffset tokenExpiresAt)
        {
            AccountId = string.IsNullOrWhiteSpace(accountId) ? throw new ArgumentException("Account id is required.", nameof(accountId)) : accountId.Trim();
            Generation = generation > 0 ? generation : throw new ArgumentOutOfRangeException(nameof(generation));
            TokenExpiresAt = tokenExpiresAt;
        }

        public string AccountId { get; }
        public long Generation { get; }
        public DateTimeOffset TokenExpiresAt { get; }
    }

    public interface IProductAuthenticationFlow
    {
        INetworkRuntimeSnapshotSource NetworkSnapshots { get; }
        event Action<string> SessionReplaced;
        UniTask ConnectAsync(CancellationToken cancellationToken);
        UniTask<ProductAuthenticationSession> LoginGuestAsync(string guestAccountId, CancellationToken cancellationToken);
        UniTask DisconnectAsync(CancellationToken cancellationToken);
    }

    public interface IGameplaySessionTeardownBoundary
    {
        UniTask StopSimulationSessionAsync(CancellationToken cancellationToken);
        UniTask DestroyActorRegistrationsAndEndpointsAsync(CancellationToken cancellationToken);
        UniTask CleanupSceneRuntimeAsync(CancellationToken cancellationToken);
    }

    public sealed class ProductRuntimeDefinition
    {
        public ProductRuntimeDefinition(string productShellSceneLocation, string gameplaySceneLocation, PreloadPlan homePreloadPlan, PreloadPlan gameplayPreloadPlan, int diagnosticsHistoryCapacity)
        {
            ProductShellSceneLocation = Require(productShellSceneLocation, nameof(productShellSceneLocation));
            GameplaySceneLocation = Require(gameplaySceneLocation, nameof(gameplaySceneLocation));
            HomePreloadPlan = homePreloadPlan ?? throw new ArgumentNullException(nameof(homePreloadPlan));
            GameplayPreloadPlan = gameplayPreloadPlan ?? throw new ArgumentNullException(nameof(gameplayPreloadPlan));
            DiagnosticsHistoryCapacity = diagnosticsHistoryCapacity > 0 ? diagnosticsHistoryCapacity : throw new ArgumentOutOfRangeException(nameof(diagnosticsHistoryCapacity));
        }

        public string ProductShellSceneLocation { get; }
        public string GameplaySceneLocation { get; }
        public PreloadPlan HomePreloadPlan { get; }
        public PreloadPlan GameplayPreloadPlan { get; }
        public int DiagnosticsHistoryCapacity { get; }

        private static string Require(string value, string name)
        {
            return string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Value is required.", name) : value.Trim();
        }
    }

    public sealed class ProductStartupComposition
    {
        public ProductStartupComposition(ProductStartupHandoff handoff, ProductStartupProfile profile, IProductStartupSnapshotSource startupSnapshots, ProductRuntimeDefinition runtimeDefinition, IProductAuthenticationFlow authenticationFlow, IProductDiskSpaceProbe diskSpaceProbe, IProductTagDownloadService tagDownloadService)
        {
            Handoff = handoff ?? throw new ArgumentNullException(nameof(handoff));
            Profile = profile ?? throw new ArgumentNullException(nameof(profile));
            StartupSnapshots = startupSnapshots ?? throw new ArgumentNullException(nameof(startupSnapshots));
            RuntimeDefinition = runtimeDefinition ?? throw new ArgumentNullException(nameof(runtimeDefinition));
            AuthenticationFlow = authenticationFlow ?? throw new ArgumentNullException(nameof(authenticationFlow));
            DiskSpaceProbe = diskSpaceProbe ?? throw new ArgumentNullException(nameof(diskSpaceProbe));
            TagDownloadService = tagDownloadService ?? throw new ArgumentNullException(nameof(tagDownloadService));
        }

        public ProductStartupHandoff Handoff { get; }
        public ProductStartupProfile Profile { get; }
        public IProductStartupSnapshotSource StartupSnapshots { get; }
        public ProductRuntimeDefinition RuntimeDefinition { get; }
        public IProductAuthenticationFlow AuthenticationFlow { get; }
        public IProductDiskSpaceProbe DiskSpaceProbe { get; }
        public IProductTagDownloadService TagDownloadService { get; }
    }

    public sealed class ProductRuntimeSnapshot
    {
        public ProductRuntimeSnapshot(long sequence, DateTimeOffset capturedAt, ProductRuntimeStage stage, ProductAuthState authState, ProductHomeState homeState, ProductGameplayState gameplayState, string safeError)
        {
            Sequence = sequence;
            CapturedAt = capturedAt;
            Stage = stage;
            AuthState = authState;
            HomeState = homeState;
            GameplayState = gameplayState;
            SafeError = safeError ?? string.Empty;
        }

        public long Sequence { get; }
        public DateTimeOffset CapturedAt { get; }
        public ProductRuntimeStage Stage { get; }
        public ProductAuthState AuthState { get; }
        public ProductHomeState HomeState { get; }
        public ProductGameplayState GameplayState { get; }
        public string SafeError { get; }
    }

    public interface IProductRuntimeSnapshotSource
    {
        ProductRuntimeSnapshot Current { get; }
        IReadOnlyList<ProductRuntimeSnapshot> History { get; }
        event Action<ProductRuntimeSnapshot> Changed;
    }

    internal sealed class ProductRuntimeSnapshotStore : IProductRuntimeSnapshotSource
    {
        private readonly int _capacity;
        private readonly Queue<ProductRuntimeSnapshot> _history = new Queue<ProductRuntimeSnapshot>();
        private long _sequence;

        public ProductRuntimeSnapshotStore(int capacity)
        {
            _capacity = capacity > 0 ? capacity : throw new ArgumentOutOfRangeException(nameof(capacity));
        }

        public ProductRuntimeSnapshot Current { get; private set; }
        public IReadOnlyList<ProductRuntimeSnapshot> History => _history.ToArray();
        public event Action<ProductRuntimeSnapshot> Changed;

        public void Publish(ProductRuntimeStage stage, ProductAuthState auth, ProductHomeState home, ProductGameplayState gameplay, string safeError = "")
        {
            Current = new ProductRuntimeSnapshot(++_sequence, DateTimeOffset.UtcNow, stage, auth, home, gameplay, safeError);
            _history.Enqueue(Current);
            while (_history.Count > _capacity)
            {
                _history.Dequeue();
            }

            Changed?.Invoke(Current);
        }
    }
}
