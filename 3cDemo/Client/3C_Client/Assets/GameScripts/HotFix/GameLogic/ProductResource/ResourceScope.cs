using System;
using System.Threading;

namespace GameLogic.ProductResource
{
    public sealed class ResourceScope : IDisposable
    {
        private readonly ProductResourceRuntime _runtime;
        private readonly CancellationTokenSource _cancellation = new CancellationTokenSource();
        private readonly CancellationTokenSource _lifetimeCancellation;
        private int _leaseCount;

        internal ResourceScope(ProductResourceRuntime runtime, ResourceScopeId id, ResourceScopeKind kind, string name, CancellationToken runtimeCancellation)
        {
            _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            _lifetimeCancellation = CancellationTokenSource.CreateLinkedTokenSource(runtimeCancellation);
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

        internal CancellationToken LifetimeToken => _lifetimeCancellation.Token;

        public int LeaseCount => _leaseCount;

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
            _lifetimeCancellation.Dispose();
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
