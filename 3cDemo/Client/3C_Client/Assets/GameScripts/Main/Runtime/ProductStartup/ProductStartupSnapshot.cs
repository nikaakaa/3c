using System;
using System.Collections.Generic;

namespace ThirdPerson.ProductStartup
{
    public sealed class ProductStartupSnapshot
    {
        public ProductStartupSnapshot(
            ProductStartupStage stage,
            int generation,
            string clientBuildVersion,
            string minimumClientBuildVersion,
            string resourcePackageVersion,
            string authProtocolVersion,
            int totalFileCount,
            int completedFileCount,
            long totalBytes,
            long completedBytes,
            string currentFile,
            double bytesPerSecond,
            TimeSpan estimatedRemaining,
            DateTimeOffset stageStartedAt,
            TimeSpan stageElapsed,
            int retryCount,
            ProductStartupErrorCode errorCode,
            string safeError,
            bool retryable,
            int validCacheFileCount,
            int invalidCacheFileCount,
            float cacheVerificationProgress,
            long requiredDiskBytes,
            long availableDiskBytes,
            string resourceEndpointHost,
            string resourceTag,
            bool waitingForConsent)
        {
            Stage = stage;
            Generation = generation;
            ClientBuildVersion = clientBuildVersion ?? string.Empty;
            MinimumClientBuildVersion = minimumClientBuildVersion ?? string.Empty;
            ResourcePackageVersion = resourcePackageVersion ?? string.Empty;
            AuthProtocolVersion = authProtocolVersion ?? string.Empty;
            TotalFileCount = totalFileCount;
            CompletedFileCount = completedFileCount;
            TotalBytes = totalBytes;
            CompletedBytes = completedBytes;
            CurrentFile = currentFile ?? string.Empty;
            BytesPerSecond = bytesPerSecond;
            EstimatedRemaining = estimatedRemaining;
            StageStartedAt = stageStartedAt;
            StageElapsed = stageElapsed;
            RetryCount = retryCount;
            ErrorCode = errorCode;
            SafeError = safeError ?? string.Empty;
            Retryable = retryable;
            ValidCacheFileCount = validCacheFileCount;
            InvalidCacheFileCount = invalidCacheFileCount;
            CacheVerificationProgress = cacheVerificationProgress;
            RequiredDiskBytes = requiredDiskBytes;
            AvailableDiskBytes = availableDiskBytes;
            ResourceEndpointHost = resourceEndpointHost ?? string.Empty;
            ResourceTag = resourceTag ?? string.Empty;
            WaitingForConsent = waitingForConsent;
        }

        public ProductStartupStage Stage { get; }
        public int Generation { get; }
        public string ClientBuildVersion { get; }
        public string MinimumClientBuildVersion { get; }
        public string ResourcePackageVersion { get; }
        public string AuthProtocolVersion { get; }
        public int TotalFileCount { get; }
        public int CompletedFileCount { get; }
        public long TotalBytes { get; }
        public long CompletedBytes { get; }
        public string CurrentFile { get; }
        public double BytesPerSecond { get; }
        public TimeSpan EstimatedRemaining { get; }
        public DateTimeOffset StageStartedAt { get; }
        public TimeSpan StageElapsed { get; }
        public int RetryCount { get; }
        public ProductStartupErrorCode ErrorCode { get; }
        public string SafeError { get; }
        public bool Retryable { get; }
        public int ValidCacheFileCount { get; }
        public int InvalidCacheFileCount { get; }
        public float CacheVerificationProgress { get; }
        public long RequiredDiskBytes { get; }
        public long AvailableDiskBytes { get; }
        public string ResourceEndpointHost { get; }
        public string ResourceTag { get; }
        public bool WaitingForConsent { get; }
        public bool HasError => ErrorCode != ProductStartupErrorCode.None;

        public ProductStartupSnapshot With(
            ProductStartupStage? stage = null,
            int? generation = null,
            string minimumClientBuildVersion = null,
            string resourcePackageVersion = null,
            int? totalFileCount = null,
            int? completedFileCount = null,
            long? totalBytes = null,
            long? completedBytes = null,
            string currentFile = null,
            double? bytesPerSecond = null,
            TimeSpan? estimatedRemaining = null,
            DateTimeOffset? stageStartedAt = null,
            TimeSpan? stageElapsed = null,
            int? retryCount = null,
            ProductStartupErrorCode? errorCode = null,
            string safeError = null,
            bool? retryable = null,
            int? validCacheFileCount = null,
            int? invalidCacheFileCount = null,
            float? cacheVerificationProgress = null,
            long? requiredDiskBytes = null,
            long? availableDiskBytes = null,
            string resourceTag = null,
            bool? waitingForConsent = null)
        {
            return new ProductStartupSnapshot(
                stage ?? Stage,
                generation ?? Generation,
                ClientBuildVersion,
                minimumClientBuildVersion ?? MinimumClientBuildVersion,
                resourcePackageVersion ?? ResourcePackageVersion,
                AuthProtocolVersion,
                totalFileCount ?? TotalFileCount,
                completedFileCount ?? CompletedFileCount,
                totalBytes ?? TotalBytes,
                completedBytes ?? CompletedBytes,
                currentFile ?? CurrentFile,
                bytesPerSecond ?? BytesPerSecond,
                estimatedRemaining ?? EstimatedRemaining,
                stageStartedAt ?? StageStartedAt,
                stageElapsed ?? StageElapsed,
                retryCount ?? RetryCount,
                errorCode ?? ErrorCode,
                safeError ?? SafeError,
                retryable ?? Retryable,
                validCacheFileCount ?? ValidCacheFileCount,
                invalidCacheFileCount ?? InvalidCacheFileCount,
                cacheVerificationProgress ?? CacheVerificationProgress,
                requiredDiskBytes ?? RequiredDiskBytes,
                availableDiskBytes ?? AvailableDiskBytes,
                ResourceEndpointHost,
                resourceTag ?? ResourceTag,
                waitingForConsent ?? WaitingForConsent);
        }
    }

    public interface IProductStartupSnapshotSource
    {
        ProductStartupSnapshot Current { get; }
        IReadOnlyList<ProductStartupSnapshot> History { get; }
        event Action<ProductStartupSnapshot> SnapshotChanged;
    }

    public interface IProductStartupCommands
    {
        void Retry();
        void ConfirmCoreDownload();
        void Exit();
    }

    public sealed class ProductStartupSnapshotStore : IProductStartupSnapshotSource
    {
        const int HistoryCapacity = 64;
        readonly object m_Sync = new object();
        readonly Queue<ProductStartupSnapshot> m_History = new Queue<ProductStartupSnapshot>(HistoryCapacity);
        ProductStartupSnapshot m_Current;
        int m_Generation;

        public ProductStartupSnapshot Current
        {
            get
            {
                lock (m_Sync)
                {
                    return m_Current;
                }
            }
        }

        public IReadOnlyList<ProductStartupSnapshot> History
        {
            get
            {
                lock (m_Sync)
                {
                    return m_History.ToArray();
                }
            }
        }

        public event Action<ProductStartupSnapshot> SnapshotChanged;

        internal void BeginGeneration(int generation, ProductStartupSnapshot snapshot)
        {
            if (snapshot == null || snapshot.Generation != generation)
            {
                throw new ArgumentException("Generation snapshot does not match the store generation.", nameof(snapshot));
            }

            lock (m_Sync)
            {
                if (generation <= m_Generation)
                {
                    throw new InvalidOperationException("Startup generation must increase monotonically.");
                }

                m_Generation = generation;
            }

            PublishCore(snapshot);
        }

        internal bool TryPublish(int generation, ProductStartupSnapshot snapshot)
        {
            if (snapshot == null || snapshot.Generation != generation)
            {
                return false;
            }

            lock (m_Sync)
            {
                if (generation != m_Generation)
                {
                    return false;
                }
            }

            PublishCore(snapshot);
            return true;
        }

        void PublishCore(ProductStartupSnapshot snapshot)
        {
            lock (m_Sync)
            {
                m_Current = snapshot;
                m_History.Enqueue(snapshot);
                while (m_History.Count > HistoryCapacity)
                {
                    m_History.Dequeue();
                }
            }

            SnapshotChanged?.Invoke(snapshot);
        }
    }
}
