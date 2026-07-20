using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using GameLogic.ProductDiagnostics;
using GameLogic.ProductResource;
using TEngine;
using ThirdPersonCharacter.Pipeline;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameLogic.ProductStartup
{
    public sealed class ProductStartupCoordinator
    {
        private static ProductStartupCoordinator _current;

        private readonly ProductStartupComposition _composition;
        private readonly IResourceModule _resourceModule;
        private readonly ISceneModule _sceneModule;
        private readonly ProductResourceRuntime _resources;
        private readonly PreloadPlanExecutor _preloadExecutor;
        private readonly ProductGameplayDelivery _gameplayDelivery;
        private readonly ProductMemorySampler _memorySampler;
        private readonly ProductDiagnosticsStore _diagnostics;
        private readonly ProductRuntimeSnapshotStore _snapshots;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private readonly ProductFaultLab _faultLab;
#endif
        private readonly CancellationTokenSource _lifetime = new CancellationTokenSource();

        private IProductShellViewController _view;
        private ResourceScope _homeScope;
        private ResourceScope _gameplayScope;
        private GameplayDownloadPlan _gameplayDownloadPlan;
        private ProductAuthenticationSession _authentication;
        private ProductAuthState _authState = ProductAuthState.Disconnected;
        private ProductHomeState _homeState = ProductHomeState.Unavailable;
        private ProductGameplayState _gameplayState = ProductGameplayState.Unavailable;
        private bool _operationRunning;
        private bool _gameplaySceneCommitted;
        private bool _disposed;

        private ProductStartupCoordinator(ProductStartupComposition composition)
        {
            _composition = composition ?? throw new ArgumentNullException(nameof(composition));
            if (!composition.Profile.TryValidate(out var errorCode, out string safeError))
            {
                throw new InvalidOperationException($"Product startup profile is invalid: {errorCode} {safeError}");
            }

            _resourceModule = ModuleSystem.GetModule<IResourceModule>();
            _sceneModule = ModuleSystem.GetModule<ISceneModule>();
            IObjectPoolModule poolModule = ModuleSystem.GetModule<IObjectPoolModule>();
            int capacity = composition.RuntimeDefinition.DiagnosticsHistoryCapacity;
            _resources = new ProductResourceRuntime(_resourceModule, poolModule, composition.Handoff.PackageName, capacity);
            _preloadExecutor = new PreloadPlanExecutor(_resources);
            _gameplayDelivery = new ProductGameplayDelivery(_resourceModule, _resources, composition.DiskSpaceProbe, composition.TagDownloadService, composition.Profile, composition.Handoff.PackageName);
            _memorySampler = new ProductMemorySampler();
            _diagnostics = new ProductDiagnosticsStore(capacity);
            _snapshots = new ProductRuntimeSnapshotStore(capacity);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _faultLab = new ProductFaultLab(
                _resources,
                _gameplayDelivery,
                new ProductYooAssetCacheFaultBoundary(composition.Handoff.PackageName),
                capacity);
#endif
            composition.AuthenticationFlow.SessionReplaced += OnSessionReplaced;
            Publish(ProductRuntimeStage.None);
        }

        public static ProductStartupCoordinator Current => _current;

        public IProductRuntimeSnapshotSource Snapshots => _snapshots;

        public IResourceRuntimeSnapshotSource ResourceSnapshots => _resources;

        public IGameplayDownloadSnapshotSource GameplayDownloadSnapshots => _gameplayDelivery;

        public IProductCheckpointSnapshotSource CheckpointSnapshots => _diagnostics;

        public static async UniTask<ProductStartupCoordinator> StartAsync(ProductStartupComposition composition, CancellationToken cancellationToken = default)
        {
            if (_current != null)
            {
                throw new InvalidOperationException("ProductStartupCoordinator already exists.");
            }

            var coordinator = new ProductStartupCoordinator(composition);
            _current = coordinator;
            try
            {
                await coordinator.InitializeAsync(cancellationToken);
                return coordinator;
            }
            catch
            {
                await coordinator.ShutdownAsync(null, CancellationToken.None, false);
                throw;
            }
        }

        public static async UniTask StopAsync(IGameplaySessionTeardownBoundary gameplayTeardown = null, CancellationToken cancellationToken = default)
        {
            ProductStartupCoordinator coordinator = _current;
            if (coordinator != null)
            {
                await coordinator.ShutdownAsync(gameplayTeardown, cancellationToken, true);
            }
        }

        public static async UniTask StopForApplicationExitAsync(CancellationToken cancellationToken = default)
        {
            ProductStartupCoordinator coordinator = _current;
            if (coordinator != null)
            {
                await coordinator.ShutdownAsync(null, cancellationToken, false);
            }
        }

        public async UniTask ReturnHomeAsync(IGameplaySessionTeardownBoundary teardown, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            if (_gameplayState != ProductGameplayState.Ready || _gameplayScope == null)
            {
                throw new InvalidOperationException("Gameplay is not ready.");
            }
            if (teardown == null)
            {
                throw new ArgumentNullException(nameof(teardown));
            }

            _gameplayState = ProductGameplayState.ReturningHome;
            Publish(ProductRuntimeStage.ReturnHome);
            try
            {
                await ExecuteGameplayTeardownAsync(teardown, cancellationToken);
            }
            catch (Exception exception)
            {
                _gameplayState = ProductGameplayState.Ready;
                Publish(ProductRuntimeStage.Failed, $"Gameplay teardown failed ({exception.GetType().Name}).");
                throw;
            }

            _gameplayScope.Dispose();
            _gameplayScope = null;
            _gameplaySceneCommitted = false;
            await LoadProductShellAsync(cancellationToken, true);
            _view.ShowBusy("ReclaimGameplayResources");
            await _resources.RunMaintenanceAsync(ResourceMaintenanceReason.ReturnHomeLoading, cancellationToken);
            await PreloadHomeAsync(cancellationToken);
            FreezeCheckpoint("GameplayReclaimed", ProductMemoryBudgetKind.Home);
        }

        private async UniTask InitializeAsync(CancellationToken cancellationToken)
        {
            using (CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _lifetime.Token))
            {
                await LoadProductShellAsync(linked.Token, false);
                await _resources.RunMaintenanceAsync(ResourceMaintenanceReason.SceneTransitionCompleted, linked.Token);
                _authState = ProductAuthState.Connecting;
                Publish(ProductRuntimeStage.ConnectAuthGateway);
                _view.ShowBusy("ConnectAuthGateway");
                try
                {
                    await _composition.AuthenticationFlow.ConnectAsync(linked.Token);
                    _authState = ProductAuthState.AwaitingGuestLogin;
                    Publish(ProductRuntimeStage.AwaitGuestLogin);
                    _view.ShowLogin(string.Empty);
                }
                catch (Exception exception)
                {
                    _authState = ProductAuthState.Failed;
                    Publish(ProductRuntimeStage.Failed, $"Auth gateway connection failed ({exception.GetType().Name}).");
                    _view.ShowLogin("Auth gateway connection failed.");
                }
            }
        }

        private async UniTask LoadProductShellAsync(CancellationToken cancellationToken, bool showLoadingMask)
        {
            Publish(ProductRuntimeStage.LoadProductShell);
            Scene scene = await _sceneModule.LoadSceneAsync(
                _composition.RuntimeDefinition.ProductShellSceneLocation,
                LoadSceneMode.Single,
                false,
                100,
                false);
            cancellationToken.ThrowIfCancellationRequested();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                throw new InvalidOperationException("ProductShell scene failed to load.");
            }

            _view = ProductShellViewController.CreateLoadedRoot();
            _view.Bind(new ProductShellBindings(
                LoginGuestAsync,
                PlanGameplayDownloadAsync,
                ConfirmGameplayDownloadAsync,
                _composition.StartupSnapshots,
                _snapshots,
                _resources,
                _gameplayDelivery,
                _composition.AuthenticationFlow.NetworkSnapshots,
                _diagnostics
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                , _faultLab
#endif
                ));
            if (showLoadingMask)
            {
                _view.ShowBusy("ReturnHome");
            }
            else
            {
                _view.ShowLogin(string.Empty);
            }
        }

        private async UniTask LoginGuestAsync(string guestAccountId)
        {
            ThrowIfDisposed();
            if (_authState != ProductAuthState.AwaitingGuestLogin && _authState != ProductAuthState.Failed)
            {
                throw new InvalidOperationException($"Guest login is not allowed while auth state is {_authState}.");
            }

            BeginOperation();
            try
            {
                if (_authState == ProductAuthState.Failed)
                {
                    _authState = ProductAuthState.Connecting;
                    Publish(ProductRuntimeStage.ConnectAuthGateway);
                    try
                    {
                        await _composition.AuthenticationFlow.ConnectAsync(_lifetime.Token);
                    }
                    catch (Exception exception)
                    {
                        _authState = ProductAuthState.Failed;
                        Publish(ProductRuntimeStage.Failed, $"Auth gateway connection failed ({exception.GetType().Name}).");
                        _view.ShowLogin("Auth gateway connection failed.");
                        throw;
                    }
                }
                _authState = ProductAuthState.Authenticating;
                Publish(ProductRuntimeStage.AwaitGuestLogin);
                _view.ShowBusy("GuestLogin");
                try
                {
                    _authentication = await _composition.AuthenticationFlow.LoginGuestAsync(guestAccountId, _lifetime.Token);
                }
                catch (Exception exception)
                {
                    _authState = ProductAuthState.Failed;
                    Publish(ProductRuntimeStage.Failed, $"Guest login failed ({exception.GetType().Name}).");
                    _view.ShowLogin("Guest login failed.");
                    throw;
                }
                _authState = ProductAuthState.Authenticated;
                try
                {
                    await PreloadHomeAsync(_lifetime.Token);
                }
                catch (Exception exception)
                {
                    _homeState = ProductHomeState.Failed;
                    Publish(ProductRuntimeStage.Failed, $"Home preload failed ({exception.GetType().Name}).");
                    _view.ShowHomeUnavailable("Authenticated, but Home resource preparation failed.");
                    throw;
                }
            }
            finally
            {
                EndOperation();
            }
        }

        private async UniTask PreloadHomeAsync(CancellationToken cancellationToken)
        {
            if (_authentication == null || _authState != ProductAuthState.Authenticated)
            {
                throw new InvalidOperationException("Authenticated session is required before Home preload.");
            }

            if (_homeScope != null)
            {
                throw new InvalidOperationException("Home scope already exists.");
            }

            _homeScope = _resources.CreateHomeScope();
            _homeState = ProductHomeState.Preloading;
            Publish(ProductRuntimeStage.PreloadHome);
            try
            {
                await _preloadExecutor.ExecuteAsync(_composition.RuntimeDefinition.HomePreloadPlan, _homeScope, cancellationToken);
            }
            catch
            {
                _homeScope.Dispose();
                _homeScope = null;
                _homeState = ProductHomeState.Failed;
                throw;
            }

            _homeState = ProductHomeState.Ready;
            _gameplayState = ProductGameplayState.Unavailable;
            Publish(ProductRuntimeStage.HomeReady);
            FreezeCheckpoint("HomeReady", ProductMemoryBudgetKind.Home);
            _view.ShowHome();
        }

        private UniTask PlanGameplayDownloadAsync()
        {
            ThrowIfDisposed();
            if (_authState != ProductAuthState.Authenticated || _homeState != ProductHomeState.Ready)
            {
                throw new InvalidOperationException("Gameplay planning requires authenticated HomeReady state.");
            }

            _gameplayState = ProductGameplayState.PlanningDownload;
            Publish(ProductRuntimeStage.PlanGameplayDownload);
            try
            {
                _gameplayDownloadPlan = _gameplayDelivery.CreatePlan();
                _gameplayState = ProductGameplayState.AwaitingDownloadConsent;
                Publish(ProductRuntimeStage.PlanGameplayDownload);
                _view.ShowGameplayDownloadConsent(_gameplayDelivery.Current);
                return UniTask.CompletedTask;
            }
            catch (Exception exception)
            {
                _gameplayState = ProductGameplayState.Failed;
                Publish(ProductRuntimeStage.Failed, $"Gameplay download planning failed ({exception.GetType().Name}).");
                _view.ShowError("Gameplay download planning failed.");
                throw;
            }
        }

        private async UniTask ConfirmGameplayDownloadAsync()
        {
            ThrowIfDisposed();
            if (_gameplayState != ProductGameplayState.AwaitingDownloadConsent || _gameplayDownloadPlan == null)
            {
                throw new InvalidOperationException("No current Gameplay download plan is awaiting consent.");
            }

            BeginOperation();
            try
            {
                _gameplayState = ProductGameplayState.Downloading;
                Publish(ProductRuntimeStage.DownloadGameplay);
                _view.ShowBusy("DownloadGameplay");
                await _gameplayDelivery.DownloadConfirmedPlanAsync(_gameplayDownloadPlan, _lifetime.Token);
                await PreloadAndEnterGameplayAsync(_lifetime.Token);
            }
            catch (Exception exception)
            {
                if (_gameplayScope != null && !_gameplaySceneCommitted)
                {
                    _gameplayScope.Dispose();
                    _gameplayScope = null;
                }

                _gameplayState = ProductGameplayState.Failed;
                Publish(ProductRuntimeStage.Failed, $"Gameplay entry failed ({exception.GetType().Name}).");
                if (_view != null)
                {
                    _view.ShowError("Gameplay preparation failed. Home remains available.");
                    _view.ShowHome();
                }
                throw;
            }
            finally
            {
                EndOperation();
            }
        }

        private async UniTask PreloadAndEnterGameplayAsync(CancellationToken cancellationToken)
        {
            _gameplayScope = _resources.CreateGameplayScope();
            _gameplayState = ProductGameplayState.Preloading;
            Publish(ProductRuntimeStage.PreloadGameplay);
            await _preloadExecutor.ExecuteAsync(_composition.RuntimeDefinition.GameplayPreloadPlan, _gameplayScope, cancellationToken);

            _gameplayState = ProductGameplayState.EnteringScene;
            Publish(ProductRuntimeStage.EnterGameplay);
            Scene scene = await _sceneModule.LoadSceneAsync(
                _composition.RuntimeDefinition.GameplaySceneLocation,
                LoadSceneMode.Single,
                false,
                100,
                false);
            cancellationToken.ThrowIfCancellationRequested();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                throw new InvalidOperationException("Gameplay Sandbox scene failed to load.");
            }

            _gameplaySceneCommitted = true;
            ProductGameplayReturnController.Install(scene, this);
            _view = null;
            _homeScope.Dispose();
            _homeScope = null;
            _homeState = ProductHomeState.Disposed;
            await _resources.RunMaintenanceAsync(ResourceMaintenanceReason.SceneTransitionCompleted, cancellationToken);
            _gameplayState = ProductGameplayState.Ready;
            Publish(ProductRuntimeStage.GameplayReady);
            FreezeCheckpoint("GameplayReady", ProductMemoryBudgetKind.Gameplay);
        }

        private async UniTask ShutdownAsync(IGameplaySessionTeardownBoundary gameplayTeardown, CancellationToken cancellationToken, bool enforceGameplayOrder)
        {
            if (_disposed)
            {
                return;
            }

            if (_gameplayScope != null)
            {
                if (gameplayTeardown == null && enforceGameplayOrder)
                {
                    throw new InvalidOperationException("Active Gameplay requires a teardown boundary before ProductStartupCoordinator shutdown.");
                }
                if (gameplayTeardown != null)
                {
                    await ExecuteGameplayTeardownAsync(gameplayTeardown, cancellationToken);
                }
                _gameplayScope.Dispose();
                _gameplayScope = null;
            }

            if (_homeScope != null)
            {
                _homeScope.Dispose();
                _homeScope = null;
            }

            _composition.AuthenticationFlow.SessionReplaced -= OnSessionReplaced;
            await _composition.AuthenticationFlow.DisconnectAsync(cancellationToken);
            _lifetime.Cancel();
            _resources.Dispose();
            _memorySampler.Dispose();
            _lifetime.Dispose();
            _disposed = true;
            Publish(ProductRuntimeStage.Disposed);
            if (ReferenceEquals(_current, this))
            {
                _current = null;
            }
        }

        private static async UniTask ExecuteGameplayTeardownAsync(IGameplaySessionTeardownBoundary teardown, CancellationToken cancellationToken)
        {
            await teardown.StopSimulationSessionAsync(cancellationToken);
            await teardown.DestroyActorRegistrationsAndEndpointsAsync(cancellationToken);
            await teardown.CleanupSceneRuntimeAsync(cancellationToken);
        }

        private async UniTask HandleSessionReplacedAsync(string reason)
        {
            if (_disposed)
            {
                return;
            }

            _authentication = null;
            _authState = ProductAuthState.Replaced;
            if (_homeScope != null)
            {
                _homeScope.Dispose();
                _homeScope = null;
            }
            _homeState = ProductHomeState.Unavailable;
            Publish(ProductRuntimeStage.AwaitGuestLogin, reason);
            await _composition.AuthenticationFlow.DisconnectAsync(_lifetime.Token);
            try
            {
                _authState = ProductAuthState.Connecting;
                await _composition.AuthenticationFlow.ConnectAsync(_lifetime.Token);
                _authState = ProductAuthState.AwaitingGuestLogin;
                Publish(ProductRuntimeStage.AwaitGuestLogin, reason);
            }
            catch (Exception exception)
            {
                _authState = ProductAuthState.Failed;
                Publish(ProductRuntimeStage.Failed, $"Auth reconnect failed ({exception.GetType().Name}).");
            }
            if (_view != null)
            {
                _view.ShowLogin(reason);
            }
        }

        private void OnSessionReplaced(string reason)
        {
            HandleSessionReplacedAsync(reason).Forget();
        }

        private void FreezeCheckpoint(string name, ProductMemoryBudgetKind budgetKind)
        {
            ResourceRuntimeSnapshot resources = _resources.Current;
            MemoryRuntimeSnapshot memory = _memorySampler.Capture(resources, _composition.Profile.PlatformMemoryBudget, budgetKind);
            _diagnostics.Freeze(name, resources, memory, _composition.AuthenticationFlow.NetworkSnapshots.Current);
        }

        private void Publish(ProductRuntimeStage stage, string safeError = "")
        {
            _snapshots.Publish(stage, _authState, _homeState, _gameplayState, safeError);
        }

        private void BeginOperation()
        {
            if (_operationRunning)
            {
                throw new InvalidOperationException("Another product operation is already running.");
            }
            _operationRunning = true;
        }

        private void EndOperation()
        {
            _operationRunning = false;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(ProductStartupCoordinator));
            }
        }
    }

    internal sealed class ProductGameplaySessionTeardownBoundary : IGameplaySessionTeardownBoundary
    {
        readonly Scene _scene;
        readonly SimulationSessionHost _sessionHost;
        readonly GameObject _transitionRoot;

        public ProductGameplaySessionTeardownBoundary(
            Scene scene,
            SimulationSessionHost sessionHost,
            GameObject transitionRoot)
        {
            _scene = scene.IsValid() && scene.isLoaded
                ? scene
                : throw new ArgumentException("Gameplay scene must be loaded.", nameof(scene));
            _sessionHost = sessionHost ? sessionHost : throw new ArgumentNullException(nameof(sessionHost));
            _transitionRoot = transitionRoot ? transitionRoot : throw new ArgumentNullException(nameof(transitionRoot));
        }

        public UniTask StopSimulationSessionAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _sessionHost.Quiesce();
            return UniTask.CompletedTask;
        }

        public UniTask DestroyActorRegistrationsAndEndpointsAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _sessionHost.ReleaseSessionRuntime();
            return UniTask.CompletedTask;
        }

        public async UniTask CleanupSceneRuntimeAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            foreach (GameObject root in _scene.GetRootGameObjects())
            {
                if (root != _transitionRoot)
                {
                    UnityEngine.Object.Destroy(root);
                }
            }
            await UniTask.NextFrame();
        }
    }

    [DisallowMultipleComponent]
    internal sealed class ProductGameplayReturnController : MonoBehaviour
    {
        ProductStartupCoordinator _coordinator;
        IGameplaySessionTeardownBoundary _teardown;
        bool _returning;
        string _safeError = string.Empty;

        public static void Install(Scene scene, ProductStartupCoordinator coordinator)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                throw new ArgumentException("Gameplay scene must be loaded.", nameof(scene));
            }
            if (coordinator == null)
            {
                throw new ArgumentNullException(nameof(coordinator));
            }

            var hosts = new System.Collections.Generic.List<SimulationSessionHost>();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                hosts.AddRange(root.GetComponentsInChildren<SimulationSessionHost>(true));
            }
            if (hosts.Count != 1)
            {
                throw new InvalidOperationException($"Sandbox Gameplay requires exactly one SimulationSessionHost, found {hosts.Count}.");
            }

            var rootObject = new GameObject(nameof(ProductGameplayReturnController));
            SceneManager.MoveGameObjectToScene(rootObject, scene);
            var controller = rootObject.AddComponent<ProductGameplayReturnController>();
            controller._coordinator = coordinator;
            controller._teardown = new ProductGameplaySessionTeardownBoundary(scene, hosts[0], rootObject);
        }

        void OnGUI()
        {
            if (_returning)
            {
                GUI.Box(new Rect(0f, 0f, Screen.width, Screen.height), "Returning Home\nStopping Gameplay and reclaiming resources...");
                return;
            }

            if (GUI.Button(new Rect(24f, 24f, 160f, 42f), "Return Home"))
            {
                _returning = true;
                _safeError = string.Empty;
                ReturnHomeAsync().Forget();
            }
            if (!string.IsNullOrWhiteSpace(_safeError))
            {
                GUI.Box(new Rect(24f, 74f, 420f, 54f), _safeError);
            }
        }

        async UniTaskVoid ReturnHomeAsync()
        {
            try
            {
                await _coordinator.ReturnHomeAsync(_teardown);
            }
            catch (Exception exception)
            {
                if (this)
                {
                    _returning = false;
                    _safeError = $"Return Home failed ({exception.GetType().Name}). Gameplay resources were retained.";
                }
            }
        }
    }
}
