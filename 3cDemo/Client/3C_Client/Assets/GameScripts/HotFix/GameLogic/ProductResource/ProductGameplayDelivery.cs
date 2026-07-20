using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using ThirdPerson.ProductStartup;
using TEngine;
using YooAsset;

namespace GameLogic.ProductResource
{
    public enum GameplayDownloadState
    {
        None = 0,
        Planned = 1,
        Downloading = 2,
        Completed = 3,
        Cancelled = 4,
        Failed = 5
    }

    public sealed class GameplayDownloadSnapshot
    {
        public GameplayDownloadSnapshot(int generation, GameplayDownloadState state, int totalFiles, int completedFiles, long totalBytes, long completedBytes, long requiredDiskBytes, long availableDiskBytes, string currentFile, string safeError)
        {
            Generation = generation;
            State = state;
            TotalFiles = totalFiles;
            CompletedFiles = completedFiles;
            TotalBytes = totalBytes;
            CompletedBytes = completedBytes;
            RequiredDiskBytes = requiredDiskBytes;
            AvailableDiskBytes = availableDiskBytes;
            CurrentFile = currentFile ?? string.Empty;
            SafeError = safeError ?? string.Empty;
        }

        public int Generation { get; }
        public GameplayDownloadState State { get; }
        public int TotalFiles { get; }
        public int CompletedFiles { get; }
        public long TotalBytes { get; }
        public long CompletedBytes { get; }
        public long RequiredDiskBytes { get; }
        public long AvailableDiskBytes { get; }
        public string CurrentFile { get; }
        public string SafeError { get; }
    }

    public interface IGameplayDownloadSnapshotSource
    {
        GameplayDownloadSnapshot Current { get; }
        event Action<GameplayDownloadSnapshot> Changed;
    }

    public interface IProductDownloadCancellationBoundary
    {
        void CancelCurrentGeneration();
    }

    public sealed class GameplayDownloadPlan
    {
        internal GameplayDownloadPlan(int generation, ResourceDownloaderOperation downloader, long requiredDiskBytes, long availableDiskBytes)
        {
            Generation = generation;
            Downloader = downloader;
            RequiredDiskBytes = requiredDiskBytes;
            AvailableDiskBytes = availableDiskBytes;
        }

        internal ResourceDownloaderOperation Downloader { get; }
        public int Generation { get; }
        public int TotalFiles => Downloader.TotalDownloadCount;
        public long TotalBytes => Downloader.TotalDownloadBytes;
        public long RequiredDiskBytes { get; }
        public long AvailableDiskBytes { get; }
    }

    public sealed class ProductGameplayDelivery : IGameplayDownloadSnapshotSource, IProductDownloadCancellationBoundary
    {
        public const string GameplayTag = "Gameplay";

        private readonly IResourceModule _resourceModule;
        private readonly ProductResourceRuntime _resources;
        private readonly IProductDiskSpaceProbe _diskSpaceProbe;
        private readonly string _packageName;
        private readonly int _downloadConcurrency;
        private readonly int _retryCount;
        private readonly long _diskSafetyMarginBytes;
        private GameplayDownloadPlan _currentPlan;
        private int _generation;

        private readonly IProductTagDownloadService _tagDownloadService;

        public ProductGameplayDelivery(IResourceModule resourceModule, ProductResourceRuntime resources, IProductDiskSpaceProbe diskSpaceProbe, IProductTagDownloadService tagDownloadService, ProductStartupProfile profile, string packageName)
        {
            _resourceModule = resourceModule ?? throw new ArgumentNullException(nameof(resourceModule));
            _resources = resources ?? throw new ArgumentNullException(nameof(resources));
            _diskSpaceProbe = diskSpaceProbe ?? throw new ArgumentNullException(nameof(diskSpaceProbe));
            _tagDownloadService = tagDownloadService ?? throw new ArgumentNullException(nameof(tagDownloadService));
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }
            if (!profile.TryValidate(out ProductStartupErrorCode errorCode, out string safeError))
            {
                throw new InvalidOperationException($"Product startup profile is invalid: {errorCode} {safeError}");
            }

            _packageName = string.IsNullOrWhiteSpace(packageName) ? throw new ArgumentException("Package name is required.", nameof(packageName)) : packageName.Trim();
            _downloadConcurrency = profile.DownloadMaxConcurrency;
            _retryCount = profile.DownloadRetryCount;
            _diskSafetyMarginBytes = profile.DiskSafetyMarginBytes;
            Current = new GameplayDownloadSnapshot(0, GameplayDownloadState.None, 0, 0, 0, 0, 0, 0, string.Empty, string.Empty);
        }

        public GameplayDownloadSnapshot Current { get; private set; }

        public event Action<GameplayDownloadSnapshot> Changed;

        public GameplayDownloadPlan CreatePlan()
        {
            if (_currentPlan != null && Current.State == GameplayDownloadState.Downloading)
            {
                throw new InvalidOperationException("Gameplay download is already active.");
            }

            ResourceDownloaderOperation downloader = _tagDownloadService.CreateDownloader(
                _packageName,
                GameplayTag,
                _downloadConcurrency,
                _retryCount);
            _resourceModule.Downloader = downloader;
            DiskSpaceResult disk = _diskSpaceProbe.QueryAvailableBytes();
            if (!disk.Succeeded)
            {
                throw new InvalidOperationException(disk.SafeError);
            }

            long requiredBytes = checked(downloader.TotalDownloadBytes * 2L + _diskSafetyMarginBytes);
            if (disk.AvailableBytes < requiredBytes)
            {
                throw new InvalidOperationException($"Gameplay download requires {requiredBytes} bytes but only {disk.AvailableBytes} bytes are available.");
            }

            _currentPlan = new GameplayDownloadPlan(++_generation, downloader, requiredBytes, disk.AvailableBytes);
            Publish(GameplayDownloadState.Planned, string.Empty, string.Empty);
            return _currentPlan;
        }

        public async UniTask DownloadConfirmedPlanAsync(GameplayDownloadPlan plan, CancellationToken cancellationToken = default)
        {
            if (plan == null || !ReferenceEquals(plan, _currentPlan) || plan.Generation != _generation)
            {
                throw new InvalidOperationException("Gameplay download plan is stale or foreign.");
            }

            ResourceDownloaderOperation downloader = plan.Downloader;
            if (downloader.TotalDownloadCount == 0)
            {
                _resources.RecordPreparedTag(GameplayTag);
                Publish(GameplayDownloadState.Completed, string.Empty, string.Empty);
                return;
            }

            downloader.DownloadUpdateCallback = data =>
            {
                if (plan.Generation == _generation)
                {
                    Current = new GameplayDownloadSnapshot(
                        plan.Generation,
                        GameplayDownloadState.Downloading,
                        data.TotalDownloadCount,
                        data.CurrentDownloadCount,
                        data.TotalDownloadBytes,
                        data.CurrentDownloadBytes,
                        plan.RequiredDiskBytes,
                        plan.AvailableDiskBytes,
                        Current.CurrentFile,
                        string.Empty);
                    Changed?.Invoke(Current);
                }
            };
            downloader.DownloadFileBeginCallback = data =>
            {
                if (plan.Generation == _generation)
                {
                    Publish(GameplayDownloadState.Downloading, data.FileName, string.Empty);
                }
            };
            downloader.DownloadErrorCallback = data =>
            {
                if (plan.Generation == _generation)
                {
                    Publish(GameplayDownloadState.Failed, data.FileName, data.ErrorInfo);
                }
            };

            Publish(GameplayDownloadState.Downloading, string.Empty, string.Empty);
            downloader.BeginDownload();
            try
            {
                await downloader.ToUniTask().AttachExternalCancellation(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                if (plan.Generation == _generation)
                {
                    CancelCurrentGeneration();
                }
                throw;
            }

            if (plan.Generation != _generation)
            {
                throw new OperationCanceledException("Gameplay download generation was replaced.");
            }
            if (downloader.Status != EOperationStatus.Succeed)
            {
                Publish(GameplayDownloadState.Failed, Current.CurrentFile, downloader.Error);
                throw new InvalidOperationException($"Gameplay download failed: {downloader.Error}");
            }

            _resources.RecordPreparedTag(GameplayTag);
            Publish(GameplayDownloadState.Completed, string.Empty, string.Empty);
        }

        public void CancelCurrentGeneration()
        {
            GameplayDownloadPlan plan = _currentPlan;
            if (plan == null)
            {
                return;
            }

            plan.Downloader.CancelDownload();
            int cancelledGeneration = _generation;
            _generation++;
            Current = new GameplayDownloadSnapshot(
                cancelledGeneration,
                GameplayDownloadState.Cancelled,
                plan.TotalFiles,
                plan.Downloader.CurrentDownloadCount,
                plan.TotalBytes,
                plan.Downloader.CurrentDownloadBytes,
                plan.RequiredDiskBytes,
                plan.AvailableDiskBytes,
                Current.CurrentFile,
                "Gameplay download was cancelled.");
            Changed?.Invoke(Current);
        }

        private void Publish(GameplayDownloadState state, string currentFile, string safeError)
        {
            GameplayDownloadPlan plan = _currentPlan;
            Current = plan == null
                ? new GameplayDownloadSnapshot(_generation, state, 0, 0, 0, 0, 0, 0, currentFile, safeError)
                : new GameplayDownloadSnapshot(
                    plan.Generation,
                    state,
                    plan.TotalFiles,
                    plan.Downloader.CurrentDownloadCount,
                    plan.TotalBytes,
                    plan.Downloader.CurrentDownloadBytes,
                    plan.RequiredDiskBytes,
                    plan.AvailableDiskBytes,
                    currentFile,
                    safeError);
            Changed?.Invoke(Current);
        }
    }
}
