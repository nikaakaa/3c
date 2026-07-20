using System;
using UnityEngine;

namespace GameLogic.ProductResource
{
    public sealed class ResourceInstanceLease : IDisposable
    {
        private readonly ProductResourceRuntime _runtime;
        private bool _disposed;

        internal ResourceInstanceLease(ProductResourceRuntime runtime, long instanceId, ResourceScopeId scopeId, ResourceIdentity identity, GameObject instance)
        {
            _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            InstanceId = instanceId;
            ScopeId = scopeId;
            Identity = identity;
            Instance = instance ? instance : throw new ArgumentNullException(nameof(instance));
        }

        public long InstanceId { get; }

        public ResourceScopeId ScopeId { get; }

        public ResourceIdentity Identity { get; }

        public GameObject Instance { get; }

        public bool IsDisposed => _disposed;

        public void Dispose()
        {
            if (_disposed)
            {
                _runtime.RecordDuplicateDispose();
                return;
            }

            if (_runtime.ReleaseInstance(InstanceId))
            {
                _disposed = true;
            }
        }
    }
}
