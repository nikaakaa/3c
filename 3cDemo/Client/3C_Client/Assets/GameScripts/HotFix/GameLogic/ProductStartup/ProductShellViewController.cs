using System;
using System.Text;
using Cysharp.Threading.Tasks;
using GameLogic.ProductDiagnostics;
using GameLogic.ProductResource;
using ThirdPerson.ProductStartup;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GameLogic.ProductStartup
{
    public sealed class ProductShellBindings
    {
        public ProductShellBindings(
            Func<string, UniTask> guestLogin,
            Func<UniTask> planGameplayDownload,
            Func<UniTask> confirmGameplayDownload,
            IProductStartupSnapshotSource startupSnapshots,
            IProductRuntimeSnapshotSource productSnapshots,
            IResourceRuntimeSnapshotSource resourceSnapshots,
            IGameplayDownloadSnapshotSource gameplayDownloadSnapshots,
            INetworkRuntimeSnapshotSource networkSnapshots,
            IProductCheckpointSnapshotSource checkpointSnapshots
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            , ProductFaultLab faultLab
#endif
            )
        {
            GuestLogin = guestLogin ?? throw new ArgumentNullException(nameof(guestLogin));
            PlanGameplayDownload = planGameplayDownload ?? throw new ArgumentNullException(nameof(planGameplayDownload));
            ConfirmGameplayDownload = confirmGameplayDownload ?? throw new ArgumentNullException(nameof(confirmGameplayDownload));
            StartupSnapshots = startupSnapshots ?? throw new ArgumentNullException(nameof(startupSnapshots));
            ProductSnapshots = productSnapshots ?? throw new ArgumentNullException(nameof(productSnapshots));
            ResourceSnapshots = resourceSnapshots ?? throw new ArgumentNullException(nameof(resourceSnapshots));
            GameplayDownloadSnapshots = gameplayDownloadSnapshots ?? throw new ArgumentNullException(nameof(gameplayDownloadSnapshots));
            NetworkSnapshots = networkSnapshots ?? throw new ArgumentNullException(nameof(networkSnapshots));
            CheckpointSnapshots = checkpointSnapshots ?? throw new ArgumentNullException(nameof(checkpointSnapshots));
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            FaultLab = faultLab ?? throw new ArgumentNullException(nameof(faultLab));
#endif
        }

        public Func<string, UniTask> GuestLogin { get; }
        public Func<UniTask> PlanGameplayDownload { get; }
        public Func<UniTask> ConfirmGameplayDownload { get; }
        public IProductStartupSnapshotSource StartupSnapshots { get; }
        public IProductRuntimeSnapshotSource ProductSnapshots { get; }
        public IResourceRuntimeSnapshotSource ResourceSnapshots { get; }
        public IGameplayDownloadSnapshotSource GameplayDownloadSnapshots { get; }
        public INetworkRuntimeSnapshotSource NetworkSnapshots { get; }
        public IProductCheckpointSnapshotSource CheckpointSnapshots { get; }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public ProductFaultLab FaultLab { get; }
#endif
    }

    public interface IProductShellViewController
    {
        void Bind(ProductShellBindings bindings);
        void ShowLogin(string safeMessage);
        void ShowHome();
        void ShowHomeUnavailable(string safeError);
        void ShowGameplayDownloadConsent(GameplayDownloadSnapshot snapshot);
        void ShowBusy(string operation);
        void ShowError(string safeError);
    }

    public sealed class ProductShellViewController : MonoBehaviour, IProductShellViewController
    {
        private static readonly Color PanelColor = new Color(0.055f, 0.07f, 0.10f, 0.96f);
        private static readonly Color AccentColor = new Color(0.15f, 0.55f, 0.95f, 1f);
        private static readonly Color TextColor = new Color(0.91f, 0.94f, 0.98f, 1f);
        private static ProductShellViewController _loaded;

        private GameObject loginRoot;
        private GameObject homeRoot;
        private GameObject diagnosticsRoot;
        private GameObject gameplayDownloadConsentRoot;
        private GameObject busyRoot;
        private InputField _guestAccountInput;
        private Text _statusText;
        private Text _busyText;
        private Text _downloadText;
        private Text _startupDiagnosticsText;
        private Text _productDiagnosticsText;
        private Text _resourceDiagnosticsText;
        private Text _networkDiagnosticsText;
        private Text _memoryDiagnosticsText;
        private Button _gameplayPlanButton;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private GameObject faultLabRoot;
        private InputField _faultLocationInput;
        private InputField _faultBundleInput;
        private InputField _faultScopeInput;
        private Text _faultResultText;
        private readonly StringBuilder _faultResultDiagnostics = new StringBuilder(256);
#endif
        private ProductShellBindings _bindings;
        private const string WaitingForSnapshots = "Waiting for snapshots...";
        private readonly StringBuilder _startupDiagnostics = new StringBuilder(512);
        private readonly StringBuilder _productDiagnostics = new StringBuilder(256);
        private readonly StringBuilder _resourceDiagnostics = new StringBuilder(1024);
        private readonly StringBuilder _networkDiagnostics = new StringBuilder(512);
        private readonly StringBuilder _memoryDiagnostics = new StringBuilder(512);
        private readonly StringBuilder _downloadDiagnostics = new StringBuilder(512);

        public static ProductShellViewController CreateLoadedRoot()
        {
            if (_loaded)
            {
                throw new InvalidOperationException("ProductShell runtime root already exists.");
            }
            if (FindObjectsOfType<ProductShellViewController>(true).Length != 0)
            {
                throw new InvalidOperationException("ProductShell scene contains a second ProductShellViewController.");
            }

            var runtimeRoot = new GameObject("ProductShell.Runtime");
            ProductShellViewController controller = runtimeRoot.AddComponent<ProductShellViewController>();
            controller.BuildUi(runtimeRoot.transform);
            _loaded = controller;
            controller.ShowLogin(string.Empty);
            return controller;
        }

        public void Bind(ProductShellBindings bindings)
        {
            if (_bindings != null)
            {
                throw new InvalidOperationException("ProductShell view is already bound.");
            }

            _bindings = bindings ?? throw new ArgumentNullException(nameof(bindings));
            _bindings.StartupSnapshots.SnapshotChanged += OnStartupChanged;
            _bindings.ProductSnapshots.Changed += OnProductChanged;
            _bindings.ResourceSnapshots.Changed += OnResourceChanged;
            _bindings.GameplayDownloadSnapshots.Changed += OnGameplayDownloadChanged;
            _bindings.NetworkSnapshots.Changed += OnNetworkChanged;
            _bindings.CheckpointSnapshots.Changed += OnCheckpointChanged;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _bindings.FaultLab.Changed += OnFaultChanged;
#endif
            RefreshDiagnostics();
        }

        public void ShowLogin(string safeMessage)
        {
            Set(loginRoot, true);
            Set(homeRoot, false);
            Set(gameplayDownloadConsentRoot, false);
            Set(busyRoot, false);
            Set(diagnosticsRoot, true);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Set(faultLabRoot, false);
#endif
            SetStatus(string.IsNullOrEmpty(safeMessage) ? "Guest demo login. One active session is guaranteed inside one AuthGateway Scene." : safeMessage);
        }

        public void ShowHome()
        {
            Set(loginRoot, false);
            Set(homeRoot, true);
            Set(gameplayDownloadConsentRoot, false);
            Set(busyRoot, false);
            Set(diagnosticsRoot, true);
            _gameplayPlanButton.interactable = true;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Set(faultLabRoot, true);
#endif
            SetStatus("HomeReady. Gameplay entry is now available.");
        }

        public void ShowHomeUnavailable(string safeError)
        {
            Set(loginRoot, false);
            Set(homeRoot, true);
            Set(gameplayDownloadConsentRoot, false);
            Set(busyRoot, false);
            Set(diagnosticsRoot, true);
            _gameplayPlanButton.interactable = false;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Set(faultLabRoot, true);
#endif
            SetStatus(safeError);
        }

        public void ShowGameplayDownloadConsent(GameplayDownloadSnapshot snapshot)
        {
            Set(gameplayDownloadConsentRoot, true);
            Set(busyRoot, false);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Set(faultLabRoot, false);
#endif
            RefreshDownload(snapshot);
            SetStatus("Gameplay download is planned. Confirm to begin the real tagged download.");
        }

        public void ShowBusy(string operation)
        {
            Set(gameplayDownloadConsentRoot, false);
            Set(busyRoot, true);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Set(faultLabRoot, false);
#endif
            _busyText.text = operation;
        }

        public void ShowError(string safeError)
        {
            Set(busyRoot, false);
            SetStatus(safeError);
        }

        private void BuildUi(Transform runtimeRoot)
        {
            if (EventSystem.current == null)
            {
                new GameObject("ProductShell.EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            }

            var canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(runtimeRoot, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            loginRoot = CreatePanel(canvasObject.transform, "Login", new Vector2(0.03f, 0.55f), new Vector2(0.47f, 0.94f));
            CreateText(loginRoot.transform, "Title", "COMMERCIAL CLIENT STARTUP", 30, new Vector2(0.05f, 0.76f), new Vector2(0.95f, 0.95f));
            CreateText(loginRoot.transform, "Hint", "WSS guest authentication\nNo token is displayed or cached to skip login", 18, new Vector2(0.05f, 0.50f), new Vector2(0.95f, 0.74f));
            _guestAccountInput = CreateInput(loginRoot.transform, "GuestAccount", "Guest Account ID", new Vector2(0.05f, 0.28f), new Vector2(0.95f, 0.47f));
            CreateButton(loginRoot.transform, "LoginButton", "CONNECT + GUEST LOGIN", new Vector2(0.05f, 0.06f), new Vector2(0.95f, 0.23f), () => RequestGuestLogin(_guestAccountInput.text));

            homeRoot = CreatePanel(canvasObject.transform, "Home", new Vector2(0.03f, 0.57f), new Vector2(0.47f, 0.94f));
            CreateText(homeRoot.transform, "Title", "HOME READY", 34, new Vector2(0.05f, 0.70f), new Vector2(0.95f, 0.94f));
            CreateText(homeRoot.transform, "Summary", "Authentication committed\nHome preload barriers committed\nGameplay runtime has not been created here", 19, new Vector2(0.05f, 0.34f), new Vector2(0.95f, 0.70f));
            _gameplayPlanButton = CreateButton(homeRoot.transform, "GameplayButton", "PLAN GAMEPLAY DOWNLOAD", new Vector2(0.05f, 0.07f), new Vector2(0.95f, 0.27f), RequestGameplayDownloadPlan);

            gameplayDownloadConsentRoot = CreatePanel(canvasObject.transform, "GameplayDownloadConsent", new Vector2(0.03f, 0.17f), new Vector2(0.47f, 0.53f));
            _downloadText = CreateText(gameplayDownloadConsentRoot.transform, "DownloadPlan", string.Empty, 18, new Vector2(0.05f, 0.28f), new Vector2(0.95f, 0.94f));
            CreateButton(gameplayDownloadConsentRoot.transform, "ConfirmButton", "CONFIRM DOWNLOAD", new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.25f), ConfirmGameplayDownload);

            busyRoot = CreatePanel(canvasObject.transform, "Busy", new Vector2(0.03f, 0.05f), new Vector2(0.47f, 0.14f));
            _busyText = CreateText(busyRoot.transform, "Operation", string.Empty, 20, new Vector2(0.03f, 0.08f), new Vector2(0.97f, 0.92f));

            diagnosticsRoot = CreatePanel(canvasObject.transform, "Diagnostics", new Vector2(0.50f, 0.08f), new Vector2(0.98f, 0.94f));
            _startupDiagnosticsText = CreateText(diagnosticsRoot.transform, "StartupText", WaitingForSnapshots, 14, new Vector2(0.025f, 0.80f), new Vector2(0.975f, 0.975f));
            _productDiagnosticsText = CreateText(diagnosticsRoot.transform, "ProductText", WaitingForSnapshots, 14, new Vector2(0.025f, 0.68f), new Vector2(0.975f, 0.80f));
            _resourceDiagnosticsText = CreateText(diagnosticsRoot.transform, "ResourceText", WaitingForSnapshots, 14, new Vector2(0.025f, 0.36f), new Vector2(0.975f, 0.68f));
            _networkDiagnosticsText = CreateText(diagnosticsRoot.transform, "NetworkText", WaitingForSnapshots, 14, new Vector2(0.025f, 0.18f), new Vector2(0.975f, 0.36f));
            _memoryDiagnosticsText = CreateText(diagnosticsRoot.transform, "MemoryText", WaitingForSnapshots, 14, new Vector2(0.025f, 0.025f), new Vector2(0.975f, 0.18f));
            ConfigureDiagnosticsText(_startupDiagnosticsText);
            ConfigureDiagnosticsText(_productDiagnosticsText);
            ConfigureDiagnosticsText(_resourceDiagnosticsText);
            ConfigureDiagnosticsText(_networkDiagnosticsText);
            ConfigureDiagnosticsText(_memoryDiagnosticsText);

            _statusText = CreateText(canvasObject.transform, "Status", string.Empty, 17, new Vector2(0.03f, 0.005f), new Vector2(0.98f, 0.07f));
            _statusText.alignment = TextAnchor.MiddleLeft;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            faultLabRoot = CreatePanel(canvasObject.transform, "FaultLab", new Vector2(0.03f, 0.05f), new Vector2(0.47f, 0.68f));
            CreateText(faultLabRoot.transform, "Title", "DEVELOPMENT FAULT LAB", 22, new Vector2(0.04f, 0.90f), new Vector2(0.96f, 0.98f));
            _faultLocationInput = CreateInput(faultLabRoot.transform, "Location", "Texture location for 20 concurrent requests", new Vector2(0.04f, 0.78f), new Vector2(0.96f, 0.88f));
            _faultBundleInput = CreateInput(faultLabRoot.transform, "Bundle", "Selected cache Bundle ID", new Vector2(0.04f, 0.66f), new Vector2(0.96f, 0.76f));
            CreateButton(faultLabRoot.transform, "Concurrent", "CONCURRENT x20", new Vector2(0.04f, 0.54f), new Vector2(0.47f, 0.64f), RunConcurrentFault);
            CreateButton(faultLabRoot.transform, "Cancel", "CANCEL DOWNLOAD", new Vector2(0.52f, 0.54f), new Vector2(0.96f, 0.64f), RunCancelDownloadFault);
            CreateButton(faultLabRoot.transform, "SelectCache", "SELECT CACHE", new Vector2(0.04f, 0.42f), new Vector2(0.47f, 0.52f), SelectCacheBundleFault);
            CreateButton(faultLabRoot.transform, "CorruptCache", "CORRUPT SELECTED", new Vector2(0.52f, 0.42f), new Vector2(0.96f, 0.52f), RunCorruptCacheFault);
            _faultScopeInput = CreateInput(faultLabRoot.transform, "Scope", "Non-Global Scope ID", new Vector2(0.04f, 0.30f), new Vector2(0.48f, 0.40f));
            CreateButton(faultLabRoot.transform, "DisposeScope", "DISPOSE SCOPE", new Vector2(0.52f, 0.30f), new Vector2(0.96f, 0.40f), RunDisposeScopeFault);
            CreateButton(faultLabRoot.transform, "LowMemory", "LOW MEMORY MAINTENANCE", new Vector2(0.04f, 0.18f), new Vector2(0.96f, 0.28f), RunLowMemoryFault);
            _faultResultText = CreateText(faultLabRoot.transform, "Result", "Only formal public boundaries are invoked.", 14, new Vector2(0.04f, 0.02f), new Vector2(0.96f, 0.16f));
#endif
        }

        private void RequestGuestLogin(string guestAccountId)
        {
            RunUiCommandAsync(() => RequireBindings().GuestLogin(guestAccountId)).Forget();
        }

        private void RequestGameplayDownloadPlan()
        {
            RunUiCommandAsync(() => RequireBindings().PlanGameplayDownload()).Forget();
        }

        private void ConfirmGameplayDownload()
        {
            RunUiCommandAsync(() => RequireBindings().ConfirmGameplayDownload()).Forget();
        }

        private async UniTaskVoid RunUiCommandAsync(Func<UniTask> command)
        {
            try
            {
                await command();
            }
            catch (Exception exception)
            {
                ShowError($"Operation failed ({exception.GetType().Name}). See diagnostics.");
            }
        }

        private void RefreshDiagnostics()
        {
            if (_bindings == null)
            {
                return;
            }

            RefreshStartupDiagnostics(_bindings.StartupSnapshots.Current);
            RefreshProductDiagnostics(_bindings.ProductSnapshots.Current);
            RefreshResourceDiagnostics(_bindings.ResourceSnapshots.Current);
            RefreshNetworkDiagnostics(_bindings.NetworkSnapshots.Current);
            RefreshMemoryDiagnostics(_bindings.CheckpointSnapshots.Current);
            RefreshDownload(_bindings.GameplayDownloadSnapshots.Current);
        }

        private void RefreshDownload(GameplayDownloadSnapshot snapshot)
        {
            if (!_downloadText || !snapshot.IsValid)
            {
                return;
            }

            _downloadDiagnostics.Clear();
            _downloadDiagnostics.AppendLine("GAMEPLAY PACKAGE");
            _downloadDiagnostics.Append("State ").AppendLine(snapshot.State.ToString());
            _downloadDiagnostics.Append("Files ").Append(snapshot.CompletedFiles).Append('/').Append(snapshot.TotalFiles).AppendLine();
            _downloadDiagnostics.Append("Bytes "); AppendBytes(_downloadDiagnostics, snapshot.CompletedBytes); _downloadDiagnostics.Append('/'); AppendBytes(_downloadDiagnostics, snapshot.TotalBytes); _downloadDiagnostics.AppendLine();
            _downloadDiagnostics.Append("Disk required "); AppendBytes(_downloadDiagnostics, snapshot.RequiredDiskBytes); _downloadDiagnostics.Append("  Available "); AppendBytes(_downloadDiagnostics, snapshot.AvailableDiskBytes); _downloadDiagnostics.AppendLine();
            _downloadDiagnostics.Append(snapshot.CurrentFile).AppendLine();
            _downloadDiagnostics.Append(snapshot.SafeError);
            _downloadText.text = _downloadDiagnostics.ToString();
        }

        private void RefreshStartupDiagnostics(ProductStartupSnapshot startup)
        {
            if (!_startupDiagnosticsText)
            {
                return;
            }

            _startupDiagnostics.Clear();
            if (startup == null)
            {
                _startupDiagnostics.Append(WaitingForSnapshots);
            }
            else
            {
                _startupDiagnostics.AppendLine("STARTUP");
                _startupDiagnostics.Append("Stage ").Append(startup.Stage).Append("  Gen ").Append(startup.Generation).AppendLine();
                _startupDiagnostics.Append("Client ").Append(startup.ClientBuildVersion).Append("  Resource ").Append(startup.ResourcePackageVersion).Append("  Protocol ").Append(startup.AuthProtocolVersion).AppendLine();
                _startupDiagnostics.Append("Files ").Append(startup.CompletedFileCount).Append('/').Append(startup.TotalFileCount).Append("  Bytes "); AppendBytes(_startupDiagnostics, startup.CompletedBytes); _startupDiagnostics.Append('/'); AppendBytes(_startupDiagnostics, startup.TotalBytes); _startupDiagnostics.AppendLine();
                _startupDiagnostics.Append("Security CRC High | HTTPS | Manifest signature: out of scope");
            }

            _startupDiagnosticsText.text = _startupDiagnostics.ToString();
        }

        private void RefreshProductDiagnostics(ProductRuntimeSnapshot product)
        {
            if (!_productDiagnosticsText)
            {
                return;
            }

            _productDiagnostics.Clear();
            if (product == null)
            {
                _productDiagnostics.Append(WaitingForSnapshots);
            }
            else
            {
                _productDiagnostics.AppendLine("PRODUCT");
                _productDiagnostics.Append(product.Stage).Append(" | Auth ").Append(product.AuthState).Append(" | Home ").Append(product.HomeState).Append(" | Gameplay ").Append(product.GameplayState);
                if (!string.IsNullOrEmpty(product.SafeError)) _productDiagnostics.AppendLine().Append("Error ").Append(product.SafeError);
            }

            _productDiagnosticsText.text = _productDiagnostics.ToString();
        }

        private void RefreshResourceDiagnostics(ResourceRuntimeSnapshot resources)
        {
            if (!_resourceDiagnosticsText)
            {
                return;
            }

            _resourceDiagnostics.Clear();
            if (!resources.IsValid)
            {
                _resourceDiagnostics.Append(WaitingForSnapshots);
            }
            else
            {
                _resourceDiagnostics.AppendLine("RESOURCE");
                _resourceDiagnostics.Append("Logical ").Append(resources.LogicalLoadCount).Append("  Physical preflight ").Append(resources.PhysicalLoadCount).Append("  Join ").Append(resources.InFlightJoinCount).Append("  Known physical reuse ").Append(resources.CacheHitCount).AppendLine();
                _resourceDiagnostics.Append("Leases ").Append(resources.ActiveLeaseCount).Append("  TEngine pool ").Append(resources.TEngineAssetPoolObjectCount).Append(" (free ").Append(resources.TEngineAssetPoolReleasableCount).Append(')').AppendLine();
                _resourceDiagnostics.Append("Package ").Append(resources.PackageName).Append(" @ ").Append(resources.PackageVersion);
                foreach (ResourceScopeSnapshot scope in resources.Scopes)
                {
                    _resourceDiagnostics.AppendLine().Append('#').Append(scope.Id.Value).Append(' ').Append(scope.Kind).Append(' ').Append(scope.State).Append(" L").Append(scope.LeaseCount);
                }
            }

            _resourceDiagnosticsText.text = _resourceDiagnostics.ToString();
        }

        private void RefreshNetworkDiagnostics(NetworkRuntimeSnapshot network)
        {
            if (!_networkDiagnosticsText)
            {
                return;
            }

            _networkDiagnostics.Clear();
            if (network == null)
            {
                _networkDiagnostics.Append(WaitingForSnapshots);
            }
            else
            {
                _networkDiagnostics.AppendLine("NETWORK");
                _networkDiagnostics.Append(network.ProductId).Append(" | ").Append(network.Transport).Append(" TLS=").Append(network.TlsEnabled).Append("  ").Append(network.RedactedEndpoint).Append("  ").Append(network.ConnectionState).AppendLine();
                _networkDiagnostics.Append("Account ").Append(network.RedactedAccountId).Append("  Client ").Append(network.RedactedClientInstanceId).Append("  Generation ").Append(network.SessionGeneration).AppendLine();
                _networkDiagnostics.Append("Token expires ").Append(network.TokenExpiresAt?.ToString("O") ?? "-").Append("  RTT ").Append(network.RoundTripMilliseconds).Append("ms  Error ").Append(network.LastErrorCode);
            }

            _networkDiagnosticsText.text = _networkDiagnostics.ToString();
        }

        private void RefreshMemoryDiagnostics(ProductCheckpointSnapshot checkpoint)
        {
            if (!_memoryDiagnosticsText)
            {
                return;
            }

            _memoryDiagnostics.Clear();
            if (checkpoint == null)
            {
                _memoryDiagnostics.Append(WaitingForSnapshots);
            }
            else
            {
                MemoryRuntimeSnapshot memory = checkpoint.Memory;
                _memoryDiagnostics.AppendLine("MEMORY");
                _memoryDiagnostics.Append(checkpoint.Checkpoint).Append("  Used "); AppendBytes(_memoryDiagnostics, memory.TotalUsedBytes); _memoryDiagnostics.Append("  Reserved "); AppendBytes(_memoryDiagnostics, memory.TotalReservedBytes); _memoryDiagnostics.AppendLine();
                _memoryDiagnostics.Append("GC "); AppendBytes(_memoryDiagnostics, memory.GcUsedBytes); _memoryDiagnostics.Append("  Texture "); AppendBytes(_memoryDiagnostics, memory.TextureBytes); _memoryDiagnostics.Append("  Mesh "); AppendBytes(_memoryDiagnostics, memory.MeshBytes); _memoryDiagnostics.AppendLine();
                _memoryDiagnostics.Append("Budget ").Append(memory.BudgetName).Append(' '); AppendBytes(_memoryDiagnostics, memory.BudgetBytes); _memoryDiagnostics.Append("  Over=").Append(memory.IsOverBudget);
                if (!string.IsNullOrEmpty(memory.ConfigurationError)) _memoryDiagnostics.AppendLine().Append(memory.ConfigurationError);
            }

            _memoryDiagnosticsText.text = _memoryDiagnostics.ToString();
        }

        private void OnStartupChanged(ProductStartupSnapshot snapshot) => RefreshStartupDiagnostics(snapshot);
        private void OnProductChanged(ProductRuntimeSnapshot snapshot) => RefreshProductDiagnostics(snapshot);
        private void OnResourceChanged(ResourceRuntimeSnapshot snapshot) => RefreshResourceDiagnostics(snapshot);
        private void OnNetworkChanged(NetworkRuntimeSnapshot snapshot) => RefreshNetworkDiagnostics(snapshot);
        private void OnCheckpointChanged(ProductCheckpointSnapshot snapshot) => RefreshMemoryDiagnostics(snapshot);

        private void OnGameplayDownloadChanged(GameplayDownloadSnapshot snapshot)
        {
            RefreshDownload(snapshot);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void RunConcurrentFault()
        {
            RunFaultAsync(() => RequireBindings().FaultLab.ConcurrentAcquireTwentyAsync(_faultLocationInput.text, typeof(Texture2D))).Forget();
        }

        private void RunCancelDownloadFault()
        {
            try
            {
                RequireBindings().FaultLab.CancelCurrentDownloader();
            }
            catch (Exception exception)
            {
                _faultResultText.text = exception.Message;
            }
        }

        private void SelectCacheBundleFault()
        {
            var bundles = RequireBindings().FaultLab.SelectableCacheBundles;
            if (bundles.Count == 0)
            {
                _faultResultText.text = "No verified cache Bundle is selectable.";
                return;
            }
            _faultBundleInput.text = bundles[0].Value;
            _faultResultText.text = $"Selected cache Bundle {bundles[0].Value}.";
        }

        private void RunCorruptCacheFault()
        {
            ProductCacheBundleId bundleId;
            try
            {
                bundleId = new ProductCacheBundleId(_faultBundleInput.text);
            }
            catch (Exception exception)
            {
                _faultResultText.text = exception.Message;
                return;
            }
            RunFaultAsync(() => RequireBindings().FaultLab.CorruptSelectedCacheBundleAsync(bundleId)).Forget();
        }

        private void RunDisposeScopeFault()
        {
            if (!long.TryParse(_faultScopeInput.text, out long value) || value <= 0)
            {
                _faultResultText.text = "Scope ID must be a positive integer.";
                return;
            }
            try
            {
                RequireBindings().FaultLab.DisposeScope(new ResourceScopeId(value));
            }
            catch (Exception exception)
            {
                _faultResultText.text = exception.Message;
            }
        }

        private void RunLowMemoryFault()
        {
            RunFaultAsync(() => RequireBindings().FaultLab.RunLowMemoryAsync()).Forget();
        }

        private async UniTaskVoid RunFaultAsync(Func<UniTask> command)
        {
            try
            {
                await command();
            }
            catch (Exception exception)
            {
                _faultResultText.text = exception.Message;
            }
        }

        private void OnFaultChanged(ProductFaultEvent fault)
        {
            if (_faultResultText)
            {
                _faultResultDiagnostics.Clear();
                _faultResultDiagnostics.Append(fault.Command).Append(": ").Append(fault.Succeeded ? "OK" : "FAILED").AppendLine().Append(fault.SafeResult);
                _faultResultText.text = _faultResultDiagnostics.ToString();
            }
        }
#endif

        private void OnDestroy()
        {
            if (_bindings != null)
            {
                _bindings.StartupSnapshots.SnapshotChanged -= OnStartupChanged;
                _bindings.ProductSnapshots.Changed -= OnProductChanged;
                _bindings.ResourceSnapshots.Changed -= OnResourceChanged;
                _bindings.GameplayDownloadSnapshots.Changed -= OnGameplayDownloadChanged;
                _bindings.NetworkSnapshots.Changed -= OnNetworkChanged;
                _bindings.CheckpointSnapshots.Changed -= OnCheckpointChanged;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                _bindings.FaultLab.Changed -= OnFaultChanged;
#endif
            }
            if (ReferenceEquals(_loaded, this))
            {
                _loaded = null;
            }
        }

        private ProductShellBindings RequireBindings()
        {
            return _bindings ?? throw new InvalidOperationException("ProductShell view is not bound.");
        }

        private void SetStatus(string value)
        {
            if (_statusText)
            {
                _statusText.text = value ?? string.Empty;
            }
        }

        private static GameObject CreatePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(parent, false);
            RectTransform rect = panel.GetComponent<RectTransform>();
            SetRect(rect, anchorMin, anchorMax);
            panel.GetComponent<Image>().color = PanelColor;
            return panel;
        }

        private static Text CreateText(Transform parent, string name, string value, int fontSize, Vector2 anchorMin, Vector2 anchorMax)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(parent, false);
            RectTransform rect = textObject.GetComponent<RectTransform>();
            SetRect(rect, anchorMin, anchorMax);
            Text text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = fontSize;
            text.color = TextColor;
            text.alignment = TextAnchor.MiddleCenter;
            text.text = value;
            return text;
        }

        private static InputField CreateInput(Transform parent, string name, string placeholder, Vector2 anchorMin, Vector2 anchorMax)
        {
            var inputObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(InputField));
            inputObject.transform.SetParent(parent, false);
            SetRect(inputObject.GetComponent<RectTransform>(), anchorMin, anchorMax);
            inputObject.GetComponent<Image>().color = new Color(0.10f, 0.13f, 0.18f, 1f);
            Text value = CreateText(inputObject.transform, "Text", string.Empty, 18, new Vector2(0.04f, 0.05f), new Vector2(0.96f, 0.95f));
            value.alignment = TextAnchor.MiddleLeft;
            Text hint = CreateText(inputObject.transform, "Placeholder", placeholder, 16, new Vector2(0.04f, 0.05f), new Vector2(0.96f, 0.95f));
            hint.alignment = TextAnchor.MiddleLeft;
            hint.color = new Color(TextColor.r, TextColor.g, TextColor.b, 0.45f);
            InputField input = inputObject.GetComponent<InputField>();
            input.textComponent = value;
            input.placeholder = hint;
            return input;
        }

        private static Button CreateButton(Transform parent, string name, string label, Vector2 anchorMin, Vector2 anchorMax, UnityEngine.Events.UnityAction action)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            SetRect(buttonObject.GetComponent<RectTransform>(), anchorMin, anchorMax);
            buttonObject.GetComponent<Image>().color = AccentColor;
            Button button = buttonObject.GetComponent<Button>();
            button.onClick.AddListener(action);
            CreateText(buttonObject.transform, "Label", label, 17, Vector2.zero, Vector2.one);
            return button;
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void Set(GameObject target, bool active)
        {
            if (target)
            {
                target.SetActive(active);
            }
        }

        private static void ConfigureDiagnosticsText(Text text)
        {
            text.alignment = TextAnchor.UpperLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
        }

        private static void AppendBytes(StringBuilder builder, long value)
        {
            if (value < 1024)
            {
                builder.Append(value).Append(" B");
                return;
            }

            if (value < 1024L * 1024L)
            {
                AppendScaledBytes(builder, value, 1024L, 1);
                builder.Append(" KiB");
                return;
            }

            if (value < 1024L * 1024L * 1024L)
            {
                AppendScaledBytes(builder, value, 1024L * 1024L, 1);
                builder.Append(" MiB");
                return;
            }

            AppendScaledBytes(builder, value, 1024L * 1024L * 1024L, 2);
            builder.Append(" GiB");
        }

        private static void AppendScaledBytes(StringBuilder builder, long value, long divisor, int decimalCount)
        {
            long whole = Math.DivRem(value, divisor, out long remainder);
            long scale = decimalCount == 1 ? 10 : 100;
            long fraction = (remainder * scale + divisor / 2) / divisor;
            if (fraction == scale)
            {
                whole++;
                fraction = 0;
            }

            builder.Append(whole).Append('.');
            if (decimalCount == 1 || fraction >= 10)
            {
                builder.Append(fraction / 10);
            }
            else
            {
                builder.Append('0');
            }
            builder.Append(fraction % 10);
        }
    }
}
