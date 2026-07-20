using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GameLogic.ProductResource
{
    public sealed class ResourceLease : IDisposable
    {
        private readonly ProductResourceRuntime _runtime;
        private bool _disposed;

        internal ResourceLease(ProductResourceRuntime runtime, long leaseId, ResourceScopeId scopeId, ResourceIdentity identity, Object asset)
        {
            _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            LeaseId = leaseId;
            ScopeId = scopeId;
            Identity = identity;
            Asset = asset ? asset : throw new ArgumentNullException(nameof(asset));
        }

        public long LeaseId { get; }

        public ResourceScopeId ScopeId { get; }

        public ResourceIdentity Identity { get; }

        public Object Asset { get; }

        public bool IsDisposed => _disposed;

        public T Get<T>() where T : Object
        {
            if (Asset is T typed)
            {
                return typed;
            }

            throw new InvalidCastException($"Resource '{Identity}' is not {typeof(T).FullName}.");
        }

        public void Dispose()
        {
            if (_disposed)
            {
                _runtime.RecordDuplicateDispose();
                return;
            }

            if (_runtime.ReleaseLease(LeaseId))
            {
                _disposed = true;
            }
        }
    }
}
