#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using GameLogic.ProductResource;
using YooAsset;

namespace GameLogic.ProductDiagnostics
{
    public enum ProductFaultCommand
    {
        CancelCurrentDownloader = 0,
        CorruptSelectedCacheBundle = 1,
        ConcurrentAcquireTwenty = 2,
        DisposeScope = 3,
        LowMemory = 4
    }

    public sealed class ProductFaultEvent
    {
        public ProductFaultEvent(long sequence, DateTimeOffset capturedAt, ProductFaultCommand command, bool succeeded, string target, string safeResult)
        {
            Sequence = sequence;
            CapturedAt = capturedAt;
            Command = command;
            Succeeded = succeeded;
            Target = target ?? string.Empty;
            SafeResult = safeResult ?? string.Empty;
        }

        public long Sequence { get; }
        public DateTimeOffset CapturedAt { get; }
        public ProductFaultCommand Command { get; }
        public bool Succeeded { get; }
        public string Target { get; }
        public string SafeResult { get; }
    }

    public readonly struct ProductCacheBundleId : IEquatable<ProductCacheBundleId>
    {
        public ProductCacheBundleId(string value)
        {
            Value = string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Bundle id is required.", nameof(value)) : value.Trim();
        }

        public string Value { get; }
        public bool Equals(ProductCacheBundleId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is ProductCacheBundleId other && Equals(other);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value;
    }

    public interface IProductCacheFaultBoundary
    {
        IReadOnlyList<ProductCacheBundleId> SelectableBundles { get; }
        UniTask CorruptSelectedBundleAsync(ProductCacheBundleId bundleId, CancellationToken cancellationToken);
    }

    public interface IProductFaultEventSource
    {
        IReadOnlyList<ProductFaultEvent> History { get; }
        event Action<ProductFaultEvent> Changed;
    }

    public sealed class ProductYooAssetCacheFaultBoundary : IProductCacheFaultBoundary
    {
        readonly ResourcePackage _package;

        public ProductYooAssetCacheFaultBoundary(string packageName)
        {
            if (string.IsNullOrWhiteSpace(packageName))
            {
                throw new ArgumentException("Package name is required.", nameof(packageName));
            }
            _package = YooAssets.GetPackage(packageName.Trim());
        }

        public IReadOnlyList<ProductCacheBundleId> SelectableBundles
        {
            get
            {
                IReadOnlyList<CachedBundleFileInfo> files = _package.GetCachedBundleFileInfos();
                var result = new ProductCacheBundleId[files.Count];
                for (int index = 0; index < files.Count; index++)
                {
                    result[index] = new ProductCacheBundleId(files[index].BundleId);
                }
                return result;
            }
        }

        public UniTask CorruptSelectedBundleAsync(
            ProductCacheBundleId bundleId,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!_package.CorruptCachedBundleFile(bundleId.Value))
            {
                throw new InvalidOperationException("Selected cache bundle is no longer available.");
            }
            return UniTask.CompletedTask;
        }
    }

    public sealed class ProductFaultLab : IProductFaultEventSource
    {
        private const int ConcurrentRequestCount = 20;
        private readonly ProductResourceRuntime _resources;
        private readonly IProductDownloadCancellationBoundary _downloadCancellation;
        private readonly IProductCacheFaultBoundary _cacheFaultBoundary;
        private readonly int _historyCapacity;
        private readonly Queue<ProductFaultEvent> _history = new Queue<ProductFaultEvent>();
        private long _sequence;

        public ProductFaultLab(ProductResourceRuntime resources, IProductDownloadCancellationBoundary downloadCancellation, IProductCacheFaultBoundary cacheFaultBoundary, int historyCapacity)
        {
            _resources = resources ?? throw new ArgumentNullException(nameof(resources));
            _downloadCancellation = downloadCancellation ?? throw new ArgumentNullException(nameof(downloadCancellation));
            _cacheFaultBoundary = cacheFaultBoundary;
            _historyCapacity = historyCapacity > 0 ? historyCapacity : throw new ArgumentOutOfRangeException(nameof(historyCapacity));
        }

        public IReadOnlyList<ProductFaultEvent> History => _history.ToArray();

        public bool SupportsCacheCorruption => _cacheFaultBoundary != null;

        public IReadOnlyList<ProductCacheBundleId> SelectableCacheBundles =>
            _cacheFaultBoundary?.SelectableBundles ?? Array.Empty<ProductCacheBundleId>();

        public event Action<ProductFaultEvent> Changed;

        public void CancelCurrentDownloader()
        {
            _downloadCancellation.CancelCurrentGeneration();
            Publish(ProductFaultCommand.CancelCurrentDownloader, true, string.Empty, "Current download generation cancelled through the formal boundary.");
        }

        public async UniTask CorruptSelectedCacheBundleAsync(ProductCacheBundleId bundleId, CancellationToken cancellationToken = default)
        {
            if (_cacheFaultBoundary == null)
            {
                throw new InvalidOperationException("No formal cache corruption boundary is configured.");
            }
            bool selectable = false;
            foreach (ProductCacheBundleId candidate in _cacheFaultBoundary.SelectableBundles)
            {
                if (candidate.Equals(bundleId))
                {
                    selectable = true;
                    break;
                }
            }
            if (!selectable)
            {
                Publish(ProductFaultCommand.CorruptSelectedCacheBundle, false, bundleId.Value, "Bundle id is not in the selectable cache set.");
                throw new InvalidOperationException("Bundle id is not selectable.");
            }

            await _cacheFaultBoundary.CorruptSelectedBundleAsync(bundleId, cancellationToken);
            Publish(ProductFaultCommand.CorruptSelectedCacheBundle, true, bundleId.Value, "Selected cache bundle was corrupted through the cache boundary.");
        }

        public async UniTask ConcurrentAcquireTwentyAsync(string location, Type assetType, CancellationToken cancellationToken = default)
        {
            ResourceScope scope = _resources.CreateTransientScope("FaultLab.ConcurrentAcquireTwenty");
            try
            {
                var tasks = new UniTask<ResourceLease>[ConcurrentRequestCount];
                for (int index = 0; index < tasks.Length; index++)
                {
                    tasks[index] = _resources.AcquireAsync(scope, location, assetType, cancellationToken);
                }

                await UniTask.WhenAll(tasks);
                Publish(ProductFaultCommand.ConcurrentAcquireTwenty, true, location, "Twenty logical requests completed in one transient scope.");
            }
            catch (Exception exception)
            {
                Publish(ProductFaultCommand.ConcurrentAcquireTwenty, false, location, exception.Message);
                throw;
            }
            finally
            {
                scope.Dispose();
            }
        }

        public void DisposeScope(ResourceScopeId scopeId)
        {
            if (!_resources.TryGetScope(scopeId, out ResourceScope scope))
            {
                Publish(ProductFaultCommand.DisposeScope, false, scopeId.ToString(), "Scope does not exist.");
                throw new InvalidOperationException("Scope does not exist.");
            }
            if (scope.Kind == ResourceScopeKind.Global)
            {
                Publish(ProductFaultCommand.DisposeScope, false, scopeId.ToString(), "Global scope cannot be disposed by Fault Lab.");
                throw new InvalidOperationException("Fault Lab cannot dispose Global scope.");
            }

            scope.Dispose();
            Publish(ProductFaultCommand.DisposeScope, true, scopeId.ToString(), "Scope disposed through its formal owner boundary.");
        }

        public async UniTask RunLowMemoryAsync(CancellationToken cancellationToken = default)
        {
            await _resources.RunMaintenanceAsync(ResourceMaintenanceReason.LowMemory, cancellationToken);
            Publish(ProductFaultCommand.LowMemory, true, string.Empty, "Formal low-memory maintenance completed.");
        }

        private void Publish(ProductFaultCommand command, bool succeeded, string target, string result)
        {
            var item = new ProductFaultEvent(++_sequence, DateTimeOffset.UtcNow, command, succeeded, target, result);
            _history.Enqueue(item);
            while (_history.Count > _historyCapacity)
            {
                _history.Dequeue();
            }
            Changed?.Invoke(item);
        }
    }
}
#endif
