using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using GameLogic.ProductDiagnostics;
using TEngine;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GameLogic.ProductResource
{
    public sealed class ProductResourceRuntime : IDisposable, IResourceRuntimeSnapshotSource
    {
        private sealed class LeaseRecord
        {
            public ResourceScope Scope;
            public ResourceIdentity Identity;
            public Object Asset;
        }

        private readonly IResourceModule _resourceModule;
        private readonly IObjectPoolModule _objectPoolModule;
        private readonly List<ObjectPoolBase> _poolMetricsBuffer;
        private readonly string _packageName;
        private readonly int _historyCapacity;
        private readonly Dictionary<ResourceScopeId, ResourceScope> _scopes = new Dictionary<ResourceScopeId, ResourceScope>();
        private readonly Dictionary<long, LeaseRecord> _leases = new Dictionary<long, LeaseRecord>();
        private readonly Dictionary<ResourceIdentity, UniTaskCompletionSource<Object>> _inFlight = new Dictionary<ResourceIdentity, UniTaskCompletionSource<Object>>();
        private readonly HashSet<ResourceIdentity> _knownPhysicalAssets = new HashSet<ResourceIdentity>();
        private readonly Dictionary<ResourceIdentity, int> _ownedReferenceCounts = new Dictionary<ResourceIdentity, int>();
        private readonly Dictionary<ResourceIdentity, int> _pendingAcquireCounts = new Dictionary<ResourceIdentity, int>();
        private readonly HashSet<string> _preparedTags = new HashSet<string>(StringComparer.Ordinal);
        private string[] _preparedTagSnapshot = Array.Empty<string>();
        private readonly Stack<LeaseRecord> _leaseRecordPool = new Stack<LeaseRecord>();
        private readonly List<ResourceIdentity> _unownedIdentityScratch = new List<ResourceIdentity>();
        private ResourceScope[] _disposeScopeBuffer = Array.Empty<ResourceScope>();
        private readonly BoundedHistory<ResourceRuntimeSnapshot> _history;
        private readonly CancellationTokenSource _runtimeCancellation = new CancellationTokenSource();

        private long _nextScopeId;
        private long _nextLeaseId;
        private long _snapshotSequence;
        private long _logicalLoadCount;
        private long _physicalLoadCount;
        private long _inFlightJoinCount;
        private long _cacheHitCount;
        private long _duplicateDisposeCount;
        private bool _maintenanceRunning;
        private bool _disposed;
        private ResourceMaintenanceSnapshot _lastMaintenance;

        public ProductResourceRuntime(IResourceModule resourceModule, IObjectPoolModule objectPoolModule, string packageName, int snapshotHistoryCapacity)
        {
            _resourceModule = resourceModule ?? throw new ArgumentNullException(nameof(resourceModule));
            _objectPoolModule = objectPoolModule ?? throw new ArgumentNullException(nameof(objectPoolModule));
            _poolMetricsBuffer = new List<ObjectPoolBase>(_objectPoolModule.Count);
            _packageName = string.IsNullOrWhiteSpace(packageName) ? throw new ArgumentException("Package name is required.", nameof(packageName)) : packageName.Trim();
            _historyCapacity = snapshotHistoryCapacity > 0 ? snapshotHistoryCapacity : throw new ArgumentOutOfRangeException(nameof(snapshotHistoryCapacity));
            _history = new BoundedHistory<ResourceRuntimeSnapshot>(_historyCapacity);
            GlobalScope = CreateScopeInternal(ResourceScopeKind.Global, "Global");
            Application.lowMemory += OnLowMemory;
            PublishSnapshot();
        }

        public ResourceScope GlobalScope { get; }

        public ResourceRuntimeSnapshot Current { get; private set; }

        public IReadOnlyList<ResourceRuntimeSnapshot> History => _history;

        public event Action<ResourceRuntimeSnapshot> Changed;

        public ResourceScope CreateHomeScope(string name = "Home")
        {
            return CreateUniqueScope(ResourceScopeKind.Home, name);
        }

        public ResourceScope CreateGameplayScope(string name = "Gameplay")
        {
            return CreateUniqueScope(ResourceScopeKind.Gameplay, name);
        }

        public ResourceScope CreateTransientScope(string name)
        {
            ThrowIfDisposed();
            ResourceScope scope = CreateScopeInternal(ResourceScopeKind.Transient, name);
            PublishSnapshot();
            return scope;
        }

        public async UniTask AcquireAsync(ResourceScope scope, string location, Type assetType, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            ValidateActiveScope(scope);
            var identity = new ResourceIdentity(_packageName, location, assetType);
            _logicalLoadCount++;
            AddPendingAcquire(identity);
            PublishSnapshot();

            try
            {
                CancellationTokenSource linked = cancellationToken.CanBeCanceled
                    ? CancellationTokenSource.CreateLinkedTokenSource(scope.LifetimeToken, cancellationToken)
                    : null;
                CancellationToken cancellation = linked == null ? scope.LifetimeToken : linked.Token;
                try
                {
                    bool knownPhysicalReuse = _knownPhysicalAssets.Contains(identity);
                    await EnsurePhysicalAssetAsync(identity).AttachExternalCancellation(cancellation);
                    if (knownPhysicalReuse)
                    {
                        _cacheHitCount++;
                    }
                    Object asset = await _resourceModule.LoadAssetAsync(identity.Location, identity.AssetType, cancellation, identity.PackageName);
                    if (!asset)
                    {
                        throw new InvalidOperationException($"TEngine failed to acquire resource '{identity}'.");
                    }

                    long leaseId = ++_nextLeaseId;
                    if (!scope.TryRegisterLease())
                    {
                        _resourceModule.UnloadAsset(asset);
                        throw new OperationCanceledException($"Resource scope '{scope.Name}' closed before lease commit.", cancellation);
                    }

                    LeaseRecord leaseRecord = RentLeaseRecord();
                    leaseRecord.Scope = scope;
                    leaseRecord.Identity = identity;
                    leaseRecord.Asset = asset;
                    _leases.Add(leaseId, leaseRecord);
                    AddOwnedReference(identity);
                    PublishSnapshot();
                }
                finally
                {
                    linked?.Dispose();
                }
            }
            finally
            {
                RemovePendingAcquire(identity);
            }
        }

        public UniTask AcquireAsync<T>(ResourceScope scope, string location, CancellationToken cancellationToken = default) where T : Object
        {
            return AcquireAsync(scope, location, typeof(T), cancellationToken);
        }

        public bool ValidateSceneLocation(string location)
        {
            ThrowIfDisposed();
            return _resourceModule.CheckLocationValid(location, _packageName);
        }

        public void RecordPreparedTag(string tag)
        {
            ThrowIfDisposed();
            if (string.IsNullOrWhiteSpace(tag))
            {
                throw new ArgumentException("Tag is required.", nameof(tag));
            }

            if (_preparedTags.Add(tag.Trim()))
            {
                _preparedTagSnapshot = new string[_preparedTags.Count];
                _preparedTags.CopyTo(_preparedTagSnapshot);
                Array.Sort(_preparedTagSnapshot, StringComparer.Ordinal);
                PublishSnapshot();
            }
        }

        public async UniTask<ResourceMaintenanceSnapshot> RunMaintenanceAsync(ResourceMaintenanceReason reason, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            while (_maintenanceRunning)
            {
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }

            _maintenanceRunning = true;
            DateTimeOffset startedAt = DateTimeOffset.UtcNow;
            GetAssetPoolMetrics(out _, out int before, out _);
            try
            {
                _resourceModule.UnloadUnusedAssets();
                await Resources.UnloadUnusedAssets().ToUniTask(cancellationToken: cancellationToken);
                RemoveUnownedPhysicalKnowledge();
                GetAssetPoolMetrics(out _, out int after, out _);
                _lastMaintenance = new ResourceMaintenanceSnapshot(reason, startedAt, DateTimeOffset.UtcNow, before, after);
                PublishSnapshot();
                return _lastMaintenance;
            }
            finally
            {
                _maintenanceRunning = false;
            }
        }

        public bool TryGetScope(ResourceScopeId id, out ResourceScope scope)
        {
            return _scopes.TryGetValue(id, out scope);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            Application.lowMemory -= OnLowMemory;
            _runtimeCancellation.Cancel();

            int scopeCount = 0;
            foreach (ResourceScope scope in _scopes.Values)
            {
                _disposeScopeBuffer[scopeCount++] = scope;
            }
            Array.Sort(_disposeScopeBuffer, 0, scopeCount, ScopeDisposeOrder.Instance);
            for (int index = 0; index < scopeCount; index++)
            {
                DisposeScope(_disposeScopeBuffer[index]);
            }
            Array.Clear(_disposeScopeBuffer, 0, scopeCount);

            _runtimeCancellation.Dispose();
        }

        internal void DisposeScope(ResourceScope scope)
        {
            if (scope == null || !_scopes.TryGetValue(scope.Id, out ResourceScope owned) || !ReferenceEquals(owned, scope))
            {
                throw new InvalidOperationException("Resource scope is not owned by this runtime.");
            }

            if (!scope.TryBeginClosing())
            {
                return;
            }

            ReleaseScopeLeases(scope);

            _scopes.Remove(scope.Id);
            scope.CompleteDispose();
            if (!_disposed)
            {
                PublishSnapshot();
            }
        }

        internal bool ReleaseLease(long leaseId)
        {
            if (!_leases.TryGetValue(leaseId, out LeaseRecord record))
            {
                RecordDuplicateDispose();
                return false;
            }

            _leases.Remove(leaseId);
            record.Scope.RemoveLease();
            RemoveOwnedReference(record.Identity);
            _resourceModule.UnloadAsset(record.Asset);
            ReturnLeaseRecord(record);
            if (!_disposed)
            {
                PublishSnapshot();
            }

            return true;
        }

        internal void RecordDuplicateDispose()
        {
            _duplicateDisposeCount++;
            if (!_disposed)
            {
                PublishSnapshot();
            }
        }

        private void ReleaseScopeLeases(ResourceScope scope)
        {
            while (scope.LeaseCount > 0)
            {
                ReleaseFirstScopeLease(scope);
            }
        }

        private void ReleaseFirstScopeLease(ResourceScope scope)
        {
            foreach (KeyValuePair<long, LeaseRecord> lease in _leases)
            {
                if (!ReferenceEquals(lease.Value.Scope, scope))
                {
                    continue;
                }

                ReleaseLease(lease.Key);
                return;
            }

            throw new InvalidOperationException("Resource scope lease registry is incomplete.");
        }

        private ResourceScope CreateUniqueScope(ResourceScopeKind kind, string name)
        {
            ThrowIfDisposed();
            foreach (ResourceScope scope in _scopes.Values)
            {
                if (scope.Kind == kind && scope.State != ResourceScopeState.Disposed)
                {
                    throw new InvalidOperationException($"An active {kind} scope already exists.");
                }
            }

            ResourceScope created = CreateScopeInternal(kind, name);
            PublishSnapshot();
            return created;
        }

        private ResourceScope CreateScopeInternal(ResourceScopeKind kind, string name)
        {
            var scope = new ResourceScope(this, new ResourceScopeId(++_nextScopeId), kind, name, _runtimeCancellation.Token);
            _scopes.Add(scope.Id, scope);
            if (_disposeScopeBuffer.Length < _scopes.Count)
            {
                Array.Resize(ref _disposeScopeBuffer, _scopes.Count);
            }
            return scope;
        }

        private LeaseRecord RentLeaseRecord()
        {
            return _leaseRecordPool.Count > 0 ? _leaseRecordPool.Pop() : new LeaseRecord();
        }

        private void ReturnLeaseRecord(LeaseRecord record)
        {
            record.Scope = null;
            record.Identity = default;
            record.Asset = null;
            _leaseRecordPool.Push(record);
        }

        private void ValidateActiveScope(ResourceScope scope)
        {
            if (scope == null)
            {
                throw new ArgumentNullException(nameof(scope));
            }

            if (!_scopes.TryGetValue(scope.Id, out ResourceScope owned) || !ReferenceEquals(scope, owned))
            {
                throw new InvalidOperationException("Resource scope is not owned by this runtime.");
            }

            if (scope.State != ResourceScopeState.Active)
            {
                throw new InvalidOperationException($"Resource scope '{scope.Name}' is {scope.State}.");
            }
        }

        private UniTask<Object> EnsurePhysicalAssetAsync(ResourceIdentity identity)
        {
            if (_knownPhysicalAssets.Contains(identity))
            {
                return UniTask.FromResult<Object>(null);
            }

            if (_inFlight.TryGetValue(identity, out UniTaskCompletionSource<Object> existing))
            {
                _inFlightJoinCount++;
                PublishSnapshot();
                return existing.Task;
            }

            var created = new UniTaskCompletionSource<Object>();
            _inFlight.Add(identity, created);
            _physicalLoadCount++;
            PublishSnapshot();
            LoadPhysicalAssetAsync(identity, created).Forget();
            return created.Task;
        }

        private async UniTaskVoid LoadPhysicalAssetAsync(ResourceIdentity identity, UniTaskCompletionSource<Object> completion)
        {
            try
            {
                Object asset = await _resourceModule.LoadAssetAsync(identity.Location, identity.AssetType, _runtimeCancellation.Token, identity.PackageName);
                if (!asset)
                {
                    throw new InvalidOperationException($"TEngine failed to load physical resource '{identity}'.");
                }

                _knownPhysicalAssets.Add(identity);
                _resourceModule.UnloadAsset(asset);
                completion.TrySetResult(asset);
            }
            catch (Exception exception)
            {
                completion.TrySetException(exception);
            }
            finally
            {
                _inFlight.Remove(identity);
                if (!_disposed)
                {
                    PublishSnapshot();
                }
            }
        }

        private void AddOwnedReference(ResourceIdentity identity)
        {
            _ownedReferenceCounts.TryGetValue(identity, out int count);
            _ownedReferenceCounts[identity] = count + 1;
        }

        private void RemoveOwnedReference(ResourceIdentity identity)
        {
            if (!_ownedReferenceCounts.TryGetValue(identity, out int count))
            {
                return;
            }

            if (count <= 1)
            {
                _ownedReferenceCounts.Remove(identity);
                if (!_pendingAcquireCounts.ContainsKey(identity))
                {
                    _knownPhysicalAssets.Remove(identity);
                }
            }
            else
            {
                _ownedReferenceCounts[identity] = count - 1;
            }
        }

        private void RemoveUnownedPhysicalKnowledge()
        {
            _unownedIdentityScratch.Clear();
            foreach (ResourceIdentity identity in _knownPhysicalAssets)
            {
                if (!_ownedReferenceCounts.ContainsKey(identity) && !_pendingAcquireCounts.ContainsKey(identity))
                {
                    _unownedIdentityScratch.Add(identity);
                }
            }

            for (int index = 0; index < _unownedIdentityScratch.Count; index++)
            {
                _knownPhysicalAssets.Remove(_unownedIdentityScratch[index]);
            }

            _unownedIdentityScratch.Clear();
        }

        private void AddPendingAcquire(ResourceIdentity identity)
        {
            _pendingAcquireCounts.TryGetValue(identity, out int count);
            _pendingAcquireCounts[identity] = count + 1;
        }

        private void RemovePendingAcquire(ResourceIdentity identity)
        {
            if (!_pendingAcquireCounts.TryGetValue(identity, out int count))
            {
                return;
            }

            if (count <= 1)
            {
                _pendingAcquireCounts.Remove(identity);
                if (!_ownedReferenceCounts.ContainsKey(identity))
                {
                    _knownPhysicalAssets.Remove(identity);
                }
            }
            else
            {
                _pendingAcquireCounts[identity] = count - 1;
            }
        }

        private void PublishSnapshot()
        {
            GetAssetPoolMetrics(out int poolCount, out int assetPoolObjects, out int assetPoolReleasable);

            var scopeSnapshots = new ResourceScopeSnapshot[_scopes.Count];
            int scopeIndex = 0;
            foreach (ResourceScope scope in _scopes.Values)
            {
                scopeSnapshots[scopeIndex++] = new ResourceScopeSnapshot(scope.Id, scope.Kind, scope.Name, scope.State, scope.LeaseCount);
            }
            Array.Sort(scopeSnapshots, ScopeSnapshotSort.Instance);

            string packageVersion;
            try
            {
                packageVersion = _resourceModule.GetPackageVersion(_packageName) ?? string.Empty;
            }
            catch
            {
                packageVersion = _resourceModule.PackageVersion ?? string.Empty;
            }

            Current = new ResourceRuntimeSnapshot(
                ++_snapshotSequence,
                DateTimeOffset.UtcNow,
                _logicalLoadCount,
                _physicalLoadCount,
                _inFlightJoinCount,
                _cacheHitCount,
                _duplicateDisposeCount,
                _leases.Count,
                _inFlight.Count,
                poolCount,
                assetPoolObjects,
                assetPoolReleasable,
                _packageName,
                packageVersion,
                _preparedTagSnapshot,
                scopeSnapshots,
                _lastMaintenance);

            _history.Add(Current);

            Changed?.Invoke(Current);
        }

        private void GetAssetPoolMetrics(out int poolCount, out int count, out int releasable)
        {
            poolCount = 0;
            count = 0;
            releasable = 0;
            try
            {
                _objectPoolModule.GetAllObjectPools(_poolMetricsBuffer);
                poolCount = _poolMetricsBuffer.Count;
                for (int i = 0; i < _poolMetricsBuffer.Count; i++)
                {
                    ObjectPoolBase pool = _poolMetricsBuffer[i];
                    if (string.Equals(pool.Name, "Asset Pool", StringComparison.Ordinal))
                    {
                        count += pool.Count;
                        releasable += pool.CanReleaseCount;
                    }
                }
            }
            finally
            {
                _poolMetricsBuffer.Clear();
            }
        }

        private void OnLowMemory()
        {
            RunMaintenanceAsync(ResourceMaintenanceReason.LowMemory, _runtimeCancellation.Token).Forget();
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(ProductResourceRuntime));
            }
        }

        private sealed class ScopeSnapshotSort : IComparer<ResourceScopeSnapshot>
        {
            internal static readonly ScopeSnapshotSort Instance = new ScopeSnapshotSort();

            public int Compare(ResourceScopeSnapshot left, ResourceScopeSnapshot right)
            {
                return left.Id.Value.CompareTo(right.Id.Value);
            }
        }

        private sealed class ScopeDisposeOrder : IComparer<ResourceScope>
        {
            internal static readonly ScopeDisposeOrder Instance = new ScopeDisposeOrder();

            public int Compare(ResourceScope left, ResourceScope right)
            {
                return right.Kind.CompareTo(left.Kind);
            }
        }
    }
}
