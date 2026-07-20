using System;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using TEngine;
using UnityEngine;
using YooAsset;

namespace ThirdPerson.ProductStartup
{
    public readonly struct ProductResourceInitializationResult
    {
        public ProductResourceInitializationResult(
            ProductStartupStageResult stageResult,
            int validCacheFileCount,
            int invalidCacheFileCount)
        {
            StageResult = stageResult;
            ValidCacheFileCount = validCacheFileCount;
            InvalidCacheFileCount = invalidCacheFileCount;
        }

        public ProductStartupStageResult StageResult { get; }
        public int ValidCacheFileCount { get; }
        public int InvalidCacheFileCount { get; }
    }

    public readonly struct ProductResourceProgress
    {
        public ProductResourceProgress(
            int totalFileCount,
            int completedFileCount,
            long totalBytes,
            long completedBytes,
            string currentFile,
            int retryCount)
        {
            TotalFileCount = totalFileCount;
            CompletedFileCount = completedFileCount;
            TotalBytes = totalBytes;
            CompletedBytes = completedBytes;
            CurrentFile = currentFile ?? string.Empty;
            RetryCount = retryCount;
        }

        public int TotalFileCount { get; }
        public int CompletedFileCount { get; }
        public long TotalBytes { get; }
        public long CompletedBytes { get; }
        public string CurrentFile { get; }
        public int RetryCount { get; }
    }

    public sealed class ProductCoreDownloadPlan
    {
        internal ProductCoreDownloadPlan(ResourceDownloaderOperation downloader)
        {
            Downloader = downloader ?? throw new ArgumentNullException(nameof(downloader));
        }

        internal ResourceDownloaderOperation Downloader { get; }
        public int TotalFileCount => Downloader.TotalDownloadCount;
        public long TotalBytes => Downloader.TotalDownloadBytes;
        public long RemainingBytes => Math.Max(0L, Downloader.TotalDownloadBytes - Downloader.CurrentDownloadBytes);
    }

    public interface IProjectResourceInitializationAdapter
    {
        string PackageName { get; }

        UniTask<ProductResourceInitializationResult> InitializePackageAndVerifyCacheAsync(
            ProductStartupProfile profile,
            Action<float> verificationProgress,
            CancellationToken cancellationToken);

        UniTask<(ProductStartupStageResult Result, string PackageVersion)> RequestPackageVersionAsync(
            int timeoutSeconds,
            CancellationToken cancellationToken);

        void CommitPackageVersion(string packageVersion);

        UniTask<ProductStartupStageResult> UpdatePackageManifestAsync(
            string packageVersion,
            int timeoutSeconds,
            CancellationToken cancellationToken);

        UniTask<(ProductStartupStageResult Result, ProductCoreDownloadPlan Plan)> PlanCoreDownloadAsync(
            ProductStartupProfile profile,
            CancellationToken cancellationToken);

        UniTask<ProductStartupStageResult> DownloadCoreAsync(
            ProductCoreDownloadPlan plan,
            ProductStartupProfile profile,
            Action<ProductResourceProgress> progress,
            CancellationToken cancellationToken);

        UniTask<ProductStartupStageResult> ClearObsoleteCacheAsync(CancellationToken cancellationToken);
        void CancelActiveDownload();
    }

    public interface IProductTagDownloadService
    {
        ResourceDownloaderOperation CreateDownloader(
            string packageName,
            string tag,
            int maxConcurrency,
            int retryCount);
    }

    public sealed class ProjectTagDownloadService : IProductTagDownloadService
    {
        public ResourceDownloaderOperation CreateDownloader(
            string packageName,
            string tag,
            int maxConcurrency,
            int retryCount)
        {
            if (string.IsNullOrWhiteSpace(packageName))
            {
                throw new ArgumentException("Package name is required.", nameof(packageName));
            }
            if (string.IsNullOrWhiteSpace(tag))
            {
                throw new ArgumentException("Resource tag is required.", nameof(tag));
            }
            if (maxConcurrency <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxConcurrency));
            }
            if (retryCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(retryCount));
            }

            ResourcePackage package = YooAssets.GetPackage(packageName.Trim());
            return package.CreateResourceDownloader(tag.Trim(), maxConcurrency, retryCount);
        }
    }

    public sealed class ProjectResourceInitializationAdapter : IProjectResourceInitializationAdapter
    {
        public const string CoreTag = "Core";
        readonly IResourceModule m_ResourceModule;
        ResourceDownloaderOperation m_ActiveDownloader;

        public ProjectResourceInitializationAdapter(IResourceModule resourceModule)
        {
            m_ResourceModule = resourceModule ?? throw new ArgumentNullException(nameof(resourceModule));
        }

        public string PackageName => m_ResourceModule.DefaultPackageName;

        public async UniTask<ProductResourceInitializationResult> InitializePackageAndVerifyCacheAsync(
            ProductStartupProfile profile,
            Action<float> verificationProgress,
            CancellationToken cancellationToken)
        {
            if (!YooAssets.Initialized)
            {
                return InitializationFailure(
                    ProductStartupErrorCode.YooAssetsNotInitialized,
                    "YooAsset runtime is not initialized.",
                    false);
            }

            var options = new ResourcePackageInitializationOptions(
                PackageName,
                profile.ResourceEndpoint,
                EFileVerifyLevel.High,
                profile.CacheVerifyMaxConcurrency,
                profile.DownloadMaxConcurrency,
                profile.DownloadMaxRequestPerFrame,
                profile.DownloadWatchDogSeconds,
                profile.ResumeDownloadMinimumBytes,
                profile.ResumeDownloadResponseCodes);
            ResourcePackageInitializationResult initialization;
            try
            {
                initialization = await m_ResourceModule.InitPackage(options, verificationProgress);
            }
            catch (ArgumentException exception)
            {
                return InitializationFailure(
                    ProductStartupErrorCode.ResourceEndpointNotHttps,
                    Sanitize(exception.Message),
                    false);
            }

            cancellationToken.ThrowIfCancellationRequested();
            InitializationOperation operation = initialization.Operation;
            if (operation == null || operation.Status != EOperationStatus.Succeed)
            {
                return InitializationFailure(
                    ProductStartupErrorCode.PackageInitializationFailed,
                    operation == null ? "Resource package initialization returned no operation." : Sanitize(operation.Error),
                    true);
            }

            return new ProductResourceInitializationResult(
                ProductStartupStageResult.Success(ProductStartupStage.InitializePackageAndVerifyCache),
                initialization.ValidCacheFileCount,
                initialization.InvalidCacheFileCount);
        }

        public async UniTask<(ProductStartupStageResult Result, string PackageVersion)> RequestPackageVersionAsync(
            int timeoutSeconds,
            CancellationToken cancellationToken)
        {
            var operation = m_ResourceModule.RequestPackageVersionAsync(false, timeoutSeconds);
            await AwaitNonCancelableOperationAsync(operation, cancellationToken);
            if (operation.Status != EOperationStatus.Succeed || string.IsNullOrWhiteSpace(operation.PackageVersion))
            {
                return (
                    ProductStartupStageResult.Failure(
                        ProductStartupStage.RequestPackageVersion,
                        ProductStartupErrorCode.PackageVersionRequestFailed,
                        Sanitize(operation.Error),
                        true),
                    string.Empty);
            }

            return (
                ProductStartupStageResult.Success(ProductStartupStage.RequestPackageVersion),
                operation.PackageVersion);
        }

        public void CommitPackageVersion(string packageVersion)
        {
            if (string.IsNullOrWhiteSpace(packageVersion))
            {
                throw new ArgumentException("Package version is required.", nameof(packageVersion));
            }

            m_ResourceModule.PackageVersion = packageVersion;
        }

        public async UniTask<ProductStartupStageResult> UpdatePackageManifestAsync(
            string packageVersion,
            int timeoutSeconds,
            CancellationToken cancellationToken)
        {
            var operation = m_ResourceModule.UpdatePackageManifestAsync(packageVersion, timeoutSeconds);
            await AwaitNonCancelableOperationAsync(operation, cancellationToken);
            return operation.Status == EOperationStatus.Succeed
                ? ProductStartupStageResult.Success(ProductStartupStage.UpdatePackageManifest)
                : ProductStartupStageResult.Failure(
                    ProductStartupStage.UpdatePackageManifest,
                    ProductStartupErrorCode.ManifestUpdateFailed,
                    Sanitize(operation.Error),
                    true);
        }

        public UniTask<(ProductStartupStageResult Result, ProductCoreDownloadPlan Plan)> PlanCoreDownloadAsync(
            ProductStartupProfile profile,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var package = YooAssets.GetPackage(PackageName);
                var downloader = package.CreateResourceDownloader(
                    CoreTag,
                    profile.DownloadMaxConcurrency,
                    profile.DownloadRetryCount);
                var plan = new ProductCoreDownloadPlan(downloader);
                return UniTask.FromResult((
                    ProductStartupStageResult.Success(ProductStartupStage.PlanCoreDownload),
                    plan));
            }
            catch (Exception)
            {
                return UniTask.FromResult((
                    ProductStartupStageResult.Failure(
                        ProductStartupStage.PlanCoreDownload,
                        ProductStartupErrorCode.CoreDownloadPlanningFailed,
                        "Core download plan could not be created.",
                        true),
                    (ProductCoreDownloadPlan)null));
            }
        }

        public async UniTask<ProductStartupStageResult> DownloadCoreAsync(
            ProductCoreDownloadPlan plan,
            ProductStartupProfile profile,
            Action<ProductResourceProgress> progress,
            CancellationToken cancellationToken)
        {
            if (plan == null)
            {
                return ProductStartupStageResult.Failure(
                    ProductStartupStage.DownloadCore,
                    ProductStartupErrorCode.CoreDownloadPlanningFailed,
                    "Core download plan is missing.",
                    true);
            }

            if (plan.Downloader.IsDone)
            {
                var package = YooAssets.GetPackage(PackageName);
                plan = new ProductCoreDownloadPlan(package.CreateResourceDownloader(
                    CoreTag,
                    profile.DownloadMaxConcurrency,
                    profile.DownloadRetryCount));
            }

            var currentFile = string.Empty;
            var retryCount = 0;
            m_ActiveDownloader = plan.Downloader;
            m_ActiveDownloader.DownloadFileBeginCallback = data =>
            {
                currentFile = data.FileName ?? string.Empty;
            };
            m_ActiveDownloader.DownloadUpdateCallback = data =>
            {
                progress?.Invoke(new ProductResourceProgress(
                    data.TotalDownloadCount,
                    data.CurrentDownloadCount,
                    data.TotalDownloadBytes,
                    data.CurrentDownloadBytes,
                    currentFile,
                    retryCount));
            };
            m_ActiveDownloader.DownloadErrorCallback = _ => retryCount++;

            if (m_ActiveDownloader.TotalDownloadCount == 0)
            {
                m_ActiveDownloader = null;
                return ProductStartupStageResult.Success(ProductStartupStage.DownloadCore);
            }

            m_ActiveDownloader.BeginDownload();
            var cancellationIssued = false;
            while (!m_ActiveDownloader.IsDone)
            {
                if (cancellationToken.IsCancellationRequested && !cancellationIssued)
                {
                    cancellationIssued = true;
                    m_ActiveDownloader.CancelDownload();
                }

                await UniTask.Yield(PlayerLoopTiming.Update);
            }

            var operation = m_ActiveDownloader;
            m_ActiveDownloader = null;
            if (cancellationIssued)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            return operation.Status == EOperationStatus.Succeed
                ? ProductStartupStageResult.Success(ProductStartupStage.DownloadCore)
                : ProductStartupStageResult.Failure(
                    ProductStartupStage.DownloadCore,
                    ProductStartupErrorCode.CoreDownloadFailed,
                    Sanitize(operation.Error),
                    true);
        }

        public async UniTask<ProductStartupStageResult> ClearObsoleteCacheAsync(CancellationToken cancellationToken)
        {
            var operation = m_ResourceModule.ClearCacheFilesAsync(EFileClearMode.ClearUnusedBundleFiles);
            await AwaitNonCancelableOperationAsync(operation, cancellationToken);
            return operation.Status == EOperationStatus.Succeed
                ? ProductStartupStageResult.Success(ProductStartupStage.ClearObsoleteCache)
                : ProductStartupStageResult.Failure(
                    ProductStartupStage.ClearObsoleteCache,
                    ProductStartupErrorCode.CacheCleanupFailed,
                    Sanitize(operation.Error),
                    true);
        }

        public void CancelActiveDownload()
        {
            m_ActiveDownloader?.CancelDownload();
        }

        static async UniTask AwaitNonCancelableOperationAsync(
            AsyncOperationBase operation,
            CancellationToken cancellationToken)
        {
            while (!operation.IsDone)
            {
                await UniTask.Yield(PlayerLoopTiming.Update);
            }

            cancellationToken.ThrowIfCancellationRequested();
        }

        static ProductResourceInitializationResult InitializationFailure(
            ProductStartupErrorCode code,
            string message,
            bool retryable)
        {
            return new ProductResourceInitializationResult(
                ProductStartupStageResult.Failure(
                    ProductStartupStage.InitializePackageAndVerifyCache,
                    code,
                    message,
                    retryable),
                -1,
                -1);
        }

        static string Sanitize(string message)
        {
            return string.IsNullOrWhiteSpace(message) ? "Resource operation failed." : message.Trim();
        }
    }

    public readonly struct DiskSpaceResult
    {
        public DiskSpaceResult(bool succeeded, long availableBytes, string safeError)
        {
            Succeeded = succeeded;
            AvailableBytes = availableBytes;
            SafeError = safeError ?? string.Empty;
        }

        public bool Succeeded { get; }
        public long AvailableBytes { get; }
        public string SafeError { get; }
    }

    public interface IProductDiskSpaceProbe
    {
        DiskSpaceResult QueryAvailableBytes();
    }

    public sealed class ProductDiskSpaceProbe : IProductDiskSpaceProbe
    {
        public DiskSpaceResult QueryAvailableBytes()
        {
            try
            {
                var root = Path.GetPathRoot(Application.persistentDataPath);
                if (string.IsNullOrWhiteSpace(root))
                {
                    return new DiskSpaceResult(false, 0L, "Persistent storage root is unavailable.");
                }

                var drive = new DriveInfo(root);
                return new DiskSpaceResult(true, drive.AvailableFreeSpace, string.Empty);
            }
            catch (Exception)
            {
                return new DiskSpaceResult(false, 0L, "Available disk space could not be queried.");
            }
        }
    }
}
