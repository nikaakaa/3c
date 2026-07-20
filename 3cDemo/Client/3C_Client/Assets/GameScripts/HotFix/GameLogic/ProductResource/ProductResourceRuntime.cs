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
        private sealed class InFlightLoad
        {
            public readonly UniTaskCompletionSource<Object> Completion = new UniTaskCompletionSource<Object>();
        }

        private sealed class LeaseRecord
        {
            public ResourceScope Scope;
            public ResourceIdentity Identity;
            public Object Asset;
        }

        private sealed class InstanceRecord
        {
            public ResourceScope Scope;
            public ResourceIdentity Identity;
            public GameObject Instance;
        }

        private readonly IResourceModule _resourceModule;
        private readonly IObjectPoolModule _objectPoolModule;
        private readonly string _packageName;
        private readonly int _historyCapacity;
        private readonly Dictionary<ResourceScopeId, ResourceScope> _scopes = new Dictionary<ResourceScopeId, ResourceScope>();
        private readonly Dictionary<long, LeaseRecord> _leases = new Dictionary<long, LeaseRecord>();
        private readonly Dictionary<long, InstanceRecord> _instances = new Dictionary<long, InstanceRecord>();
        private readonly Dictionary<ResourceIdentity, InFlightLoad> _inFlight = new Dictionary<ResourceIdentity, InFlightLoad>();
        private readonly HashSet<ResourceIdentity> _knownPhysicalAssets = new HashSet<ResourceIdentity>();
        private readonly Dictionary<ResourceIdentity, int> _ownedReferenceCounts = new Dictionary<ResourceIdentity, int>();
        private readonly Dictionary<ResourceIdentity, int> _pendingAcquireCounts = new Dictionary<ResourceIdentity, int>();
        private readonly HashSet<string> _preparedTags = new HashSet<string>(StringComparer.Ordinal);
        private readonly Queue<ResourceRuntimeSnapshot> _history = new Queue<ResourceRuntimeSnapshot>();
        private readonly CancellationTokenSource _runtimeCancellation = new CancellationTokenSource();

        private long _nextScopeId;
        private long _nextLeaseId;
        private long _nextInstanceId;
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
            _packageName = string.IsNullOrWhiteSpace(packageName) ? throw new ArgumentException("Package name is required.", nameof(packageName)) : packageName.Trim();
            _historyCapacity = snapshotHistoryCapacity > 0 ? snapshotHistoryCapacity : throw new ArgumentOutOfRangeException(nameof(snapshotHistoryCapacity));
            GlobalScope = CreateScopeInternal(ResourceScopeKind.Global, "Global");
            Application.lowMemory += OnLowMemory;
            PublishSnapshot();
        }

        public ResourceScope GlobalScope { get; }

        public ResourceRuntimeSnapshot Current { get; private set; }

        public IReadOnlyList<ResourceRuntimeSnapshot> History => _history.ToArray();

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

        public async UniTask<ResourceLease> AcquireAsync(ResourceScope scope, string location, Type assetType, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            ValidateActiveScope(scope);
            var identity = new ResourceIdentity(_packageName, location, assetType);
            _logicalLoadCount++;
            AddPendingAcquire(identity);
            PublishSnapshot();

            try
            {
                using (CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(scope.CancellationToken, cancellationToken, _runtimeCancellation.Token))
                {
                    bool knownPhysicalReuse = _knownPhysicalAssets.Contains(identity);
                    await EnsurePhysicalAssetAsync(identity).AttachExternalCancellation(linked.Token);
                    if (knownPhysicalReuse)
                    {
                        _cacheHitCount++;
                    }
                    Object asset = await _resourceModule.LoadAssetAsync(identity.Location, identity.AssetType, linked.Token, identity.PackageName);
                    if (!asset)
                    {
                        throw new InvalidOperationException($"TEngine failed to acquire resource '{identity}'.");
                    }

                    long leaseId = ++_nextLeaseId;
                    if (!scope.TryRegisterLease(leaseId))
                    {
                        _resourceModule.UnloadAsset(asset);
                        throw new OperationCanceledException($"Resource scope '{scope.Name}' closed before lease commit.", linked.Token);
                    }

                    _leases.Add(leaseId, new LeaseRecord
                    {
                        Scope = scope,
                        Identity = identity,
                        Asset = asset
                    });
                    AddOwnedReference(identity);
                    var lease = new ResourceLease(this, leaseId, scope.Id, identity, asset);
                    PublishSnapshot();
                    return lease;
                }
            }
            finally
            {
                RemovePendingAcquire(identity);
            }
        }

        public UniTask<ResourceLease> AcquireAsync<T>(ResourceScope scope, string location, CancellationToken cancellationToken = default) where T : Object
        {
            return AcquireAsync(scope, location, typeof(T), cancellationToken);
        }

        public async UniTask<ResourceInstanceLease> InstantiateAsync(ResourceScope scope, string location, Transform parent = null, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            ValidateActiveScope(scope);
            var identity = new ResourceIdentity(_packageName, location, typeof(GameObject));
            _logicalLoadCount++;
            AddPendingAcquire(identity);
            PublishSnapshot();

            try
            {
                using (CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(scope.CancellationToken, cancellationToken, _runtimeCancellation.Token))
                {
                    bool knownPhysicalReuse = _knownPhysicalAssets.Contains(identity);
                    await EnsurePhysicalAssetAsync(identity).AttachExternalCancellation(linked.Token);
                    if (knownPhysicalReuse)
                    {
                        _cacheHitCount++;
                    }
                    GameObject instance = await _resourceModule.LoadGameObjectAsync(identity.Location, parent, linked.Token, identity.PackageName);
                    if (!instance)
                    {
                        throw new InvalidOperationException($"TEngine failed to instantiate prefab '{identity}'.");
                    }

                    long instanceId = ++_nextInstanceId;
                    if (!scope.TryRegisterInstance(instanceId))
                    {
                        Object.Destroy(instance);
                        throw new OperationCanceledException($"Resource scope '{scope.Name}' closed before instance commit.", linked.Token);
                    }

                    _instances.Add(instanceId, new InstanceRecord
                    {
                        Scope = scope,
                        Identity = identity,
                        Instance = instance
                    });
                    AddOwnedReference(identity);
                    var lease = new ResourceInstanceLease(this, instanceId, scope.Id, identity, instance);
                    PublishSnapshot();
                    return lease;
                }
            }
            finally
            {
                RemovePendingAcquire(identity);
            }
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
            GetAssetPoolMetrics(out int before, out _);
            try
            {
                _resourceModule.UnloadUnusedAssets();
                await Resources.UnloadUnusedAssets().ToUniTask(cancellationToken: cancellationToken);
                RemoveUnownedPhysicalKnowledge();
                GetAssetPoolMetrics(out int after, out _);
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

            var scopes = new List<ResourceScope>(_scopes.Values);
            scopes.Sort((left, right) => right.Kind.CompareTo(left.Kind));
            foreach (ResourceScope scope in scopes)
            {
                DisposeScope(scope);
            }

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

            foreach (long instanceId in scope.CopyInstanceIds())
            {
                ReleaseInstance(instanceId);
            }

            foreach (long leaseId in scope.CopyLeaseIds())
            {
                ReleaseLease(leaseId);
            }

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
            record.Scope.RemoveLease(leaseId);
            RemoveOwnedReference(record.Identity);
            _resourceModule.UnloadAsset(record.Asset);
            if (!_disposed)
            {
                PublishSnapshot();
            }

            return true;
        }

        internal bool ReleaseInstance(long instanceId)
        {
            if (!_instances.TryGetValue(instanceId, out InstanceRecord record))
            {
                RecordDuplicateDispose();
                return false;
            }

            _instances.Remove(instanceId);
            record.Scope.RemoveInstance(instanceId);
            RemoveOwnedReference(record.Identity);
            if (record.Instance)
            {
                Object.Destroy(record.Instance);
            }

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
            var scope = new ResourceScope(this, new ResourceScopeId(++_nextScopeId), kind, name);
            _scopes.Add(scope.Id, scope);
            return scope;
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

            if (_inFlight.TryGetValue(identity, out InFlightLoad existing))
            {
                _inFlightJoinCount++;
                PublishSnapshot();
                return existing.Completion.Task;
            }

            var created = new InFlightLoad();
            _inFlight.Add(identity, created);
            _physicalLoadCount++;
            PublishSnapshot();
            LoadPhysicalAssetAsync(identity, created).Forget();
            return created.Completion.Task;
        }

        private async UniTaskVoid LoadPhysicalAssetAsync(ResourceIdentity identity, InFlightLoad inFlight)
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
                inFlight.Completion.TrySetResult(asset);
            }
            catch (Exception exception)
            {
                inFlight.Completion.TrySetException(exception);
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
            _knownPhysicalAssets.RemoveWhere(identity =>
                !_ownedReferenceCounts.ContainsKey(identity) &&
                !_pendingAcquireCounts.ContainsKey(identity));
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
            ObjectPoolBase[] pools = _objectPoolModule.GetAllObjectPools();
            GetAssetPoolMetrics(out int assetPoolObjects, out int assetPoolReleasable);

            var scopeSnapshots = new List<ResourceScopeSnapshot>(_scopes.Count);
            foreach (ResourceScope scope in _scopes.Values)
            {
                scopeSnapshots.Add(new ResourceScopeSnapshot(scope.Id, scope.Kind, scope.Name, scope.State, scope.LeaseCount, scope.LiveInstanceCount));
            }
            scopeSnapshots.Sort((left, right) => left.Id.Value.CompareTo(right.Id.Value));

            var tags = new List<string>(_preparedTags);
            tags.Sort(StringComparer.Ordinal);
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
                _instances.Count,
                _inFlight.Count,
                pools.Length,
                assetPoolObjects,
                assetPoolReleasable,
                _packageName,
                packageVersion,
                tags.ToArray(),
                scopeSnapshots.ToArray(),
                _lastMaintenance);

            _history.Enqueue(Current);
            while (_history.Count > _historyCapacity)
            {
                _history.Dequeue();
            }

            Changed?.Invoke(Current);
        }

        private void GetAssetPoolMetrics(out int count, out int releasable)
        {
            count = 0;
            releasable = 0;
            foreach (ObjectPoolBase pool in _objectPoolModule.GetAllObjectPools())
            {
                if (string.Equals(pool.Name, "Asset Pool", StringComparison.Ordinal))
                {
                    count += pool.Count;
                    releasable += pool.CanReleaseCount;
                }
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
    }
}
