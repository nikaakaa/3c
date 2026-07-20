using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ThirdPerson.ProductStartup
{
    public sealed class ProductBootstrapRunner : IProductStartupCommands, IDisposable
    {
        readonly object m_Sync = new object();
        readonly ProductStartupProfile m_Profile;
        readonly ProductStartupSnapshotStore m_SnapshotStore;
        readonly IStartupPolicyClient m_PolicyClient;
        readonly IProjectResourceInitializationAdapter m_ResourceAdapter;
        readonly IProductDiskSpaceProbe m_DiskSpaceProbe;
        readonly IProductStartupHandoffStage m_HandoffStage;
        readonly Dictionary<ProductStartupStage, int> m_RetryCounts = new Dictionary<ProductStartupStage, int>();

        CancellationTokenSource m_GenerationCancellation;
        UniTask m_ActiveRun;
        UniTaskCompletionSource<bool> m_ConsentSource;
        StartupPolicy m_Policy;
        string m_PackageVersion = string.Empty;
        ProductCoreDownloadPlan m_DownloadPlan;
        ProductStartupHandoff m_Handoff;
        ProductStartupStage m_FailedStage;
        int m_Generation;
        bool m_Started;
        bool m_Retrying;
        bool m_Disposed;
        bool m_HandoffCommitted;

        public ProductBootstrapRunner(
            ProductStartupProfile profile,
            ProductStartupSnapshotStore snapshotStore,
            IStartupPolicyClient policyClient,
            IProjectResourceInitializationAdapter resourceAdapter,
            IProductDiskSpaceProbe diskSpaceProbe,
            IProductStartupHandoffStage handoffStage)
        {
            m_Profile = profile ?? throw new ArgumentNullException(nameof(profile));
            m_SnapshotStore = snapshotStore ?? throw new ArgumentNullException(nameof(snapshotStore));
            m_PolicyClient = policyClient ?? throw new ArgumentNullException(nameof(policyClient));
            m_ResourceAdapter = resourceAdapter ?? throw new ArgumentNullException(nameof(resourceAdapter));
            m_DiskSpaceProbe = diskSpaceProbe ?? throw new ArgumentNullException(nameof(diskSpaceProbe));
            m_HandoffStage = handoffStage ?? throw new ArgumentNullException(nameof(handoffStage));
        }

        public IProductStartupSnapshotSource Snapshots => m_SnapshotStore;
        public bool HandoffCommitted => m_HandoffCommitted;

        public void Start()
        {
            lock (m_Sync)
            {
                ThrowIfDisposed();
                if (m_Started)
                {
                    throw new InvalidOperationException("Product bootstrap runner can only start once.");
                }

                m_Started = true;
                m_Generation = 1;
                m_GenerationCancellation = new CancellationTokenSource();
                var snapshot = CreateInitialSnapshot(m_Generation, ProductStartupStage.Launch, 0);
                m_SnapshotStore.BeginGeneration(m_Generation, snapshot);
                m_ActiveRun = ExecuteAsync(
                    m_Generation,
                    ProductStartupStage.RequestStartupPolicy,
                    m_GenerationCancellation.Token);
            }
        }

        public void Retry()
        {
            lock (m_Sync)
            {
                if (m_Disposed || !m_Started || m_Retrying || m_FailedStage == ProductStartupStage.None)
                {
                    return;
                }

                var snapshot = m_SnapshotStore.Current;
                if (snapshot == null || !snapshot.Retryable)
                {
                    return;
                }

                m_Retrying = true;
            }

            RetryAsync().Forget();
        }

        public void ConfirmCoreDownload()
        {
            if (m_SnapshotStore.Current?.Stage == ProductStartupStage.AwaitCoreDownloadConsent)
            {
                m_ConsentSource?.TrySetResult(true);
            }
        }

        public void Exit()
        {
            CancelActiveGeneration();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public void Dispose()
        {
            lock (m_Sync)
            {
                if (m_Disposed)
                {
                    return;
                }

                m_Disposed = true;
            }

            CancelActiveGeneration();
        }

        async UniTaskVoid RetryAsync()
        {
            ProductStartupStage retryStage;
            CancellationTokenSource oldCancellation;
            UniTask oldRun;
            lock (m_Sync)
            {
                retryStage = m_FailedStage;
                oldCancellation = m_GenerationCancellation;
                oldRun = m_ActiveRun;
            }

            oldCancellation?.Cancel();
            m_ResourceAdapter.CancelActiveDownload();
            try
            {
                await oldRun;
            }
            catch (OperationCanceledException)
            {
            }

            lock (m_Sync)
            {
                if (m_Disposed)
                {
                    m_Retrying = false;
                    return;
                }

                oldCancellation?.Dispose();
                m_Generation++;
                m_RetryCounts.TryGetValue(retryStage, out var retryCount);
                retryCount++;
                m_RetryCounts[retryStage] = retryCount;
                ClearFactsAtAndAfter(retryStage);
                m_FailedStage = ProductStartupStage.None;
                m_GenerationCancellation = new CancellationTokenSource();
                var snapshot = CreateInitialSnapshot(m_Generation, retryStage, retryCount);
                m_SnapshotStore.BeginGeneration(m_Generation, snapshot);
                m_ActiveRun = ExecuteAsync(m_Generation, retryStage, m_GenerationCancellation.Token);
                m_Retrying = false;
            }
        }

        async UniTask ExecuteAsync(
            int generation,
            ProductStartupStage startStage,
            CancellationToken cancellationToken)
        {
            var stage = startStage;
            try
            {
                while (stage != ProductStartupStage.Completed)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (!IsCurrentGeneration(generation))
                    {
                        return;
                    }

                    BeginStage(generation, stage);
                    var result = await ExecuteStageAsync(generation, stage, cancellationToken);
                    if (!IsCurrentGeneration(generation))
                    {
                        return;
                    }

                    if (!result.Succeeded)
                    {
                        if (result.ErrorCode == ProductStartupErrorCode.ClientUpdateRequired)
                        {
                            PublishTerminalClientUpdate(generation, result);
                            return;
                        }

                        m_FailedStage = stage;
                        PublishFailure(generation, result);
                        return;
                    }

                    CompleteStage(generation);
                    stage = NextStage(stage);
                }

                PublishCompleted(generation);
            }
            catch (OperationCanceledException)
            {
            }
            catch (ProductStartupException exception)
            {
                if (IsCurrentGeneration(generation))
                {
                    m_FailedStage = stage;
                    PublishFailure(
                        generation,
                        ProductStartupStageResult.Failure(
                            stage,
                            exception.ErrorCode,
                            exception.SafeError,
                            exception.Retryable));
                }
            }
            catch (Exception)
            {
                if (IsCurrentGeneration(generation))
                {
                    m_FailedStage = stage;
                    PublishFailure(
                        generation,
                        ProductStartupStageResult.Failure(
                            stage,
                            ProductStartupErrorCode.UnexpectedFailure,
                            "The startup stage failed unexpectedly.",
                            true));
                }
            }
        }

        async UniTask<ProductStartupStageResult> ExecuteStageAsync(
            int generation,
            ProductStartupStage stage,
            CancellationToken cancellationToken)
        {
            switch (stage)
            {
                case ProductStartupStage.RequestStartupPolicy:
                    return await RequestStartupPolicyAsync(generation, cancellationToken);
                case ProductStartupStage.InitializePackageAndVerifyCache:
                    return await InitializePackageAsync(generation, cancellationToken);
                case ProductStartupStage.RequestPackageVersion:
                    return await RequestPackageVersionAsync(generation, cancellationToken);
                case ProductStartupStage.UpdatePackageManifest:
                    return await m_ResourceAdapter.UpdatePackageManifestAsync(
                        m_PackageVersion,
                        m_Profile.RequestTimeoutSeconds,
                        cancellationToken);
                case ProductStartupStage.PlanCoreDownload:
                    return await PlanCoreDownloadAsync(generation, cancellationToken);
                case ProductStartupStage.AwaitCoreDownloadConsent:
                    return await AwaitCoreDownloadConsentAsync(generation, cancellationToken);
                case ProductStartupStage.DownloadCore:
                    return await DownloadCoreAsync(generation, cancellationToken);
                case ProductStartupStage.ClearObsoleteCache:
                    return await m_ResourceAdapter.ClearObsoleteCacheAsync(cancellationToken);
                case ProductStartupStage.LoadHotUpdateAssemblies:
                    return await LoadHotUpdateAssembliesAsync(cancellationToken);
                case ProductStartupStage.EnterProductRuntime:
                    return await EnterProductRuntimeAsync(cancellationToken);
                default:
                    throw new InvalidOperationException($"Unsupported product startup stage: {stage}.");
            }
        }

        async UniTask<ProductStartupStageResult> RequestStartupPolicyAsync(
            int generation,
            CancellationToken cancellationToken)
        {
            if (!m_Profile.TryValidate(out var validationError, out var validationMessage))
            {
                return ProductStartupStageResult.Failure(
                    ProductStartupStage.RequestStartupPolicy,
                    validationError,
                    validationMessage,
                    false);
            }

            var result = await m_PolicyClient.RequestAsync(
                m_Profile.StartupPolicyUri,
                m_Profile.RequestTimeoutSeconds,
                cancellationToken);
            if (!result.Succeeded)
            {
                return ProductStartupStageResult.Failure(
                    ProductStartupStage.RequestStartupPolicy,
                    result.ErrorCode,
                    result.SafeError,
                    result.Retryable);
            }

            if (!IsCurrentGeneration(generation))
            {
                throw new OperationCanceledException(cancellationToken);
            }

            m_Policy = result.Policy;
            var current = m_SnapshotStore.Current;
            m_SnapshotStore.TryPublish(
                generation,
                current.With(minimumClientBuildVersion: m_Policy.MinimumClientBuildVersion.ToString()));

            m_Profile.TryGetClientBuildVersion(out var clientVersion);
            return clientVersion < m_Policy.MinimumClientBuildVersion
                ? ProductStartupStageResult.Failure(
                    ProductStartupStage.ClientUpdateRequired,
                    ProductStartupErrorCode.ClientUpdateRequired,
                    "This client build is no longer supported.",
                    false)
                : ProductStartupStageResult.Success(ProductStartupStage.RequestStartupPolicy);
        }

        async UniTask<ProductStartupStageResult> InitializePackageAsync(
            int generation,
            CancellationToken cancellationToken)
        {
            var result = await m_ResourceAdapter.InitializePackageAndVerifyCacheAsync(
                m_Profile,
                progress => ReportVerificationProgress(generation, progress),
                cancellationToken);
            if (IsCurrentGeneration(generation))
            {
                var current = m_SnapshotStore.Current;
                m_SnapshotStore.TryPublish(
                    generation,
                    current.With(
                        validCacheFileCount: result.ValidCacheFileCount,
                        invalidCacheFileCount: result.InvalidCacheFileCount,
                        cacheVerificationProgress: result.StageResult.Succeeded ? 1f : current.CacheVerificationProgress));
            }

            return result.StageResult;
        }

        async UniTask<ProductStartupStageResult> RequestPackageVersionAsync(
            int generation,
            CancellationToken cancellationToken)
        {
            var response = await m_ResourceAdapter.RequestPackageVersionAsync(
                m_Profile.RequestTimeoutSeconds,
                cancellationToken);
            if (!response.Result.Succeeded)
            {
                return response.Result;
            }

            if (!IsCurrentGeneration(generation))
            {
                throw new OperationCanceledException(cancellationToken);
            }

            m_PackageVersion = response.PackageVersion;
            m_ResourceAdapter.CommitPackageVersion(response.PackageVersion);
            var current = m_SnapshotStore.Current;
            m_SnapshotStore.TryPublish(
                generation,
                current.With(resourcePackageVersion: response.PackageVersion));
            return response.Result;
        }

        async UniTask<ProductStartupStageResult> PlanCoreDownloadAsync(
            int generation,
            CancellationToken cancellationToken)
        {
            var response = await m_ResourceAdapter.PlanCoreDownloadAsync(m_Profile, cancellationToken);
            if (!response.Result.Succeeded)
            {
                return response.Result;
            }

            m_DownloadPlan = response.Plan;
            if (IsCurrentGeneration(generation))
            {
                var current = m_SnapshotStore.Current;
                m_SnapshotStore.TryPublish(
                    generation,
                    current.With(
                        totalFileCount: m_DownloadPlan.TotalFileCount,
                        completedFileCount: 0,
                        totalBytes: m_DownloadPlan.TotalBytes,
                        completedBytes: 0,
                        resourceTag: ProjectResourceInitializationAdapter.CoreTag));
            }

            return response.Result;
        }

        async UniTask<ProductStartupStageResult> AwaitCoreDownloadConsentAsync(
            int generation,
            CancellationToken cancellationToken)
        {
            if (m_DownloadPlan == null)
            {
                return ProductStartupStageResult.Failure(
                    ProductStartupStage.AwaitCoreDownloadConsent,
                    ProductStartupErrorCode.CoreDownloadPlanningFailed,
                    "Core download plan is missing.",
                    true);
            }

            if (m_DownloadPlan.TotalFileCount == 0)
            {
                return ProductStartupStageResult.Success(ProductStartupStage.AwaitCoreDownloadConsent);
            }

            long requiredBytes;
            try
            {
                requiredBytes = checked(m_DownloadPlan.RemainingBytes * 2L + m_Profile.DiskSafetyMarginBytes);
            }
            catch (OverflowException)
            {
                return ProductStartupStageResult.Failure(
                    ProductStartupStage.AwaitCoreDownloadConsent,
                    ProductStartupErrorCode.InsufficientDiskSpace,
                    "Core download disk budget exceeds the supported size.",
                    false);
            }

            var disk = m_DiskSpaceProbe.QueryAvailableBytes();
            if (!disk.Succeeded)
            {
                return ProductStartupStageResult.Failure(
                    ProductStartupStage.AwaitCoreDownloadConsent,
                    ProductStartupErrorCode.DiskSpaceQueryFailed,
                    disk.SafeError,
                    true);
            }

            var current = m_SnapshotStore.Current;
            m_SnapshotStore.TryPublish(
                generation,
                current.With(
                    requiredDiskBytes: requiredBytes,
                    availableDiskBytes: disk.AvailableBytes,
                    waitingForConsent: disk.AvailableBytes >= requiredBytes));

            if (disk.AvailableBytes < requiredBytes)
            {
                return ProductStartupStageResult.Failure(
                    ProductStartupStage.AwaitCoreDownloadConsent,
                    ProductStartupErrorCode.InsufficientDiskSpace,
                    "Available disk space is below the Core download budget.",
                    true);
            }

            m_ConsentSource = new UniTaskCompletionSource<bool>();
            bool consented;
            try
            {
                consented = await m_ConsentSource.Task.AttachExternalCancellation(cancellationToken);
            }
            finally
            {
                m_ConsentSource = null;
            }

            return consented
                ? ProductStartupStageResult.Success(ProductStartupStage.AwaitCoreDownloadConsent)
                : ProductStartupStageResult.Failure(
                    ProductStartupStage.AwaitCoreDownloadConsent,
                    ProductStartupErrorCode.CoreDownloadConsentRejected,
                    "Core download was not confirmed.",
                    true);
        }

        async UniTask<ProductStartupStageResult> DownloadCoreAsync(
            int generation,
            CancellationToken cancellationToken)
        {
            return await m_ResourceAdapter.DownloadCoreAsync(
                m_DownloadPlan,
                m_Profile,
                progress => ReportDownloadProgress(generation, progress),
                cancellationToken);
        }

        async UniTask<ProductStartupStageResult> LoadHotUpdateAssembliesAsync(CancellationToken cancellationToken)
        {
            m_Handoff = await m_HandoffStage.LoadHotUpdateAssembliesAsync(
                m_ResourceAdapter.PackageName,
                m_PackageVersion,
                cancellationToken);
            if (m_Handoff == null)
            {
                return ProductStartupStageResult.Failure(
                    ProductStartupStage.LoadHotUpdateAssemblies,
                    ProductStartupErrorCode.HotUpdateAssemblyLoadFailed,
                    "Hot-update assembly handoff is missing.",
                    true);
            }

            return ProductStartupStageResult.Success(ProductStartupStage.LoadHotUpdateAssemblies);
        }

        async UniTask<ProductStartupStageResult> EnterProductRuntimeAsync(CancellationToken cancellationToken)
        {
            if (m_HandoffCommitted)
            {
                return ProductStartupStageResult.Failure(
                    ProductStartupStage.EnterProductRuntime,
                    ProductStartupErrorCode.ProductEntryInvocationFailed,
                    "Product runtime handoff was already committed.",
                    false);
            }

            m_HandoffCommitted = true;
            await m_HandoffStage.EnterProductRuntimeAsync(m_Handoff, cancellationToken);
            return ProductStartupStageResult.Success(ProductStartupStage.EnterProductRuntime);
        }

        void BeginStage(int generation, ProductStartupStage stage)
        {
            m_RetryCounts.TryGetValue(stage, out var retryCount);
            var current = m_SnapshotStore.Current;
            var snapshot = current.With(
                stage: stage,
                retryCount: retryCount,
                errorCode: ProductStartupErrorCode.None,
                safeError: string.Empty,
                retryable: false,
                currentFile: string.Empty,
                bytesPerSecond: 0d,
                estimatedRemaining: TimeSpan.Zero,
                stageStartedAt: DateTimeOffset.UtcNow,
                stageElapsed: TimeSpan.Zero,
                waitingForConsent: false);
            m_SnapshotStore.TryPublish(generation, snapshot);
        }

        void CompleteStage(int generation)
        {
            var current = m_SnapshotStore.Current;
            m_SnapshotStore.TryPublish(
                generation,
                current.With(stageElapsed: DateTimeOffset.UtcNow - current.StageStartedAt));
        }

        void PublishFailure(int generation, ProductStartupStageResult result)
        {
            var current = m_SnapshotStore.Current;
            m_SnapshotStore.TryPublish(
                generation,
                current.With(
                    stageElapsed: DateTimeOffset.UtcNow - current.StageStartedAt,
                    errorCode: result.ErrorCode,
                    safeError: result.SafeError,
                    retryable: result.Retryable,
                    waitingForConsent: false));
        }

        void PublishTerminalClientUpdate(int generation, ProductStartupStageResult result)
        {
            var current = m_SnapshotStore.Current;
            m_SnapshotStore.TryPublish(
                generation,
                current.With(
                    stage: ProductStartupStage.ClientUpdateRequired,
                    stageElapsed: DateTimeOffset.UtcNow - current.StageStartedAt,
                    errorCode: result.ErrorCode,
                    safeError: result.SafeError,
                    retryable: false));
        }

        void PublishCompleted(int generation)
        {
            var current = m_SnapshotStore.Current;
            m_SnapshotStore.TryPublish(
                generation,
                current.With(
                    stage: ProductStartupStage.Completed,
                    stageStartedAt: DateTimeOffset.UtcNow,
                    stageElapsed: TimeSpan.Zero,
                    waitingForConsent: false));
        }

        void ReportVerificationProgress(int generation, float progress)
        {
            if (!IsCurrentGeneration(generation))
            {
                return;
            }

            var current = m_SnapshotStore.Current;
            if (current.Stage != ProductStartupStage.InitializePackageAndVerifyCache)
            {
                return;
            }

            m_SnapshotStore.TryPublish(
                generation,
                current.With(
                    cacheVerificationProgress: Mathf.Clamp01(progress),
                    stageElapsed: DateTimeOffset.UtcNow - current.StageStartedAt));
        }

        void ReportDownloadProgress(int generation, ProductResourceProgress progress)
        {
            if (!IsCurrentGeneration(generation))
            {
                return;
            }

            var current = m_SnapshotStore.Current;
            if (current.Stage != ProductStartupStage.DownloadCore)
            {
                return;
            }

            var elapsed = DateTimeOffset.UtcNow - current.StageStartedAt;
            var bytesPerSecond = elapsed.TotalSeconds > 0d ? progress.CompletedBytes / elapsed.TotalSeconds : 0d;
            var remainingBytes = Math.Max(0L, progress.TotalBytes - progress.CompletedBytes);
            var remaining = bytesPerSecond > 0d
                ? TimeSpan.FromSeconds(remainingBytes / bytesPerSecond)
                : TimeSpan.Zero;
            m_SnapshotStore.TryPublish(
                generation,
                current.With(
                    totalFileCount: progress.TotalFileCount,
                    completedFileCount: progress.CompletedFileCount,
                    totalBytes: progress.TotalBytes,
                    completedBytes: progress.CompletedBytes,
                    currentFile: progress.CurrentFile,
                    bytesPerSecond: bytesPerSecond,
                    estimatedRemaining: remaining,
                    retryCount: progress.RetryCount,
                    stageElapsed: elapsed));
        }

        ProductStartupSnapshot CreateInitialSnapshot(
            int generation,
            ProductStartupStage stage,
            int retryCount)
        {
            var endpointHost = Uri.TryCreate(m_Profile.ResourceEndpoint, UriKind.Absolute, out var endpoint)
                ? endpoint.Host
                : string.Empty;
            return new ProductStartupSnapshot(
                stage,
                generation,
                m_Profile.ClientBuildVersionText,
                m_Policy?.MinimumClientBuildVersion.ToString() ?? string.Empty,
                m_PackageVersion,
                m_Profile.AuthProtocolVersionText,
                0,
                0,
                0L,
                0L,
                string.Empty,
                0d,
                TimeSpan.Zero,
                DateTimeOffset.UtcNow,
                TimeSpan.Zero,
                retryCount,
                ProductStartupErrorCode.None,
                string.Empty,
                false,
                -1,
                -1,
                0f,
                0L,
                0L,
                endpointHost,
                string.Empty,
                false);
        }

        void ClearFactsAtAndAfter(ProductStartupStage stage)
        {
            if (stage <= ProductStartupStage.RequestStartupPolicy)
            {
                m_Policy = null;
            }

            if (stage <= ProductStartupStage.RequestPackageVersion)
            {
                m_PackageVersion = string.Empty;
            }

            if (stage <= ProductStartupStage.PlanCoreDownload)
            {
                m_DownloadPlan = null;
            }

            if (stage <= ProductStartupStage.LoadHotUpdateAssemblies)
            {
                m_Handoff = null;
            }
        }

        void CancelActiveGeneration()
        {
            m_GenerationCancellation?.Cancel();
            m_ConsentSource?.TrySetCanceled();
            m_ResourceAdapter.CancelActiveDownload();
        }

        bool IsCurrentGeneration(int generation)
        {
            lock (m_Sync)
            {
                return !m_Disposed && generation == m_Generation;
            }
        }

        static ProductStartupStage NextStage(ProductStartupStage stage)
        {
            switch (stage)
            {
                case ProductStartupStage.RequestStartupPolicy:
                    return ProductStartupStage.InitializePackageAndVerifyCache;
                case ProductStartupStage.InitializePackageAndVerifyCache:
                    return ProductStartupStage.RequestPackageVersion;
                case ProductStartupStage.RequestPackageVersion:
                    return ProductStartupStage.UpdatePackageManifest;
                case ProductStartupStage.UpdatePackageManifest:
                    return ProductStartupStage.PlanCoreDownload;
                case ProductStartupStage.PlanCoreDownload:
                    return ProductStartupStage.AwaitCoreDownloadConsent;
                case ProductStartupStage.AwaitCoreDownloadConsent:
                    return ProductStartupStage.DownloadCore;
                case ProductStartupStage.DownloadCore:
                    return ProductStartupStage.ClearObsoleteCache;
                case ProductStartupStage.ClearObsoleteCache:
                    return ProductStartupStage.LoadHotUpdateAssemblies;
                case ProductStartupStage.LoadHotUpdateAssemblies:
                    return ProductStartupStage.EnterProductRuntime;
                case ProductStartupStage.EnterProductRuntime:
                    return ProductStartupStage.Completed;
                default:
                    throw new InvalidOperationException($"Startup stage has no successor: {stage}.");
            }
        }

        void ThrowIfDisposed()
        {
            if (m_Disposed)
            {
                throw new ObjectDisposedException(nameof(ProductBootstrapRunner));
            }
        }
    }
}
