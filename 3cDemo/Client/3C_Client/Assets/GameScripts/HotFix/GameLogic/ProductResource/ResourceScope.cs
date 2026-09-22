using System;
using System.Threading;
using GameLogic.ProductDiagnostics;

namespace GameLogic.ProductResource
{
    public sealed class ResourceScope : IDisposable
    {
        private readonly ProductResourceRuntime _runtime;
        private readonly CancellationTokenSource _cancellation;
        private int _leaseCount;
        private ResourceScopeSnapshot _snapshot;

        internal ResourceScope(ProductResourceRuntime runtime, ResourceScopeId id, ResourceScopeKind kind, string name, CancellationToken runtimeCancellation)
        {
            _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            _cancellation = CancellationTokenSource.CreateLinkedTokenSource(runtimeCancellation);
            Id = id;
            Kind = kind;
            Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Scope name is required.", nameof(name)) : name.Trim();
            State = ResourceScopeState.Active;
        }

        public ResourceScopeId Id { get; }

        public ResourceScopeKind Kind { get; }

        public string Name { get; }

        public ResourceScopeState State { get; private set; }

        public CancellationToken CancellationToken => _cancellation.Token;

        public int LeaseCount => _leaseCount;

        internal ResourceScopeSnapshot GetSnapshot()
        {
            if (_snapshot == null || _snapshot.State != State || _snapshot.LeaseCount != _leaseCount)
            {
                _snapshot = new ResourceScopeSnapshot(Id, Kind, Name, State, _leaseCount);
            }

            return _snapshot;
        }

        public void Dispose()
        {
            if (State == ResourceScopeState.Disposed)
            {
                return;
            }
            _runtime.DisposeScope(this);
        }

        internal bool TryBeginClosing()
        {
            if (State != ResourceScopeState.Active)
            {
                return false;
            }

            State = ResourceScopeState.Closing;
            _cancellation.Cancel();
            return true;
        }

        internal void CompleteDispose()
        {
            State = ResourceScopeState.Disposed;
            _leaseCount = 0;
            _cancellation.Dispose();
        }

        internal bool TryRegisterLease()
        {
            if (State != ResourceScopeState.Active)
            {
                return false;
            }

            _leaseCount++;
            return true;
        }

        internal void RemoveLease()
        {
            _leaseCount--;
        }
    }
}
