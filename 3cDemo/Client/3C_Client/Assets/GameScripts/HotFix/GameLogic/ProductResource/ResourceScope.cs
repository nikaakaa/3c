using System;
using System.Collections.Generic;
using System.Threading;

namespace GameLogic.ProductResource
{
    public sealed class ResourceScope : IDisposable
    {
        private readonly ProductResourceRuntime _runtime;
        private readonly CancellationTokenSource _cancellation = new CancellationTokenSource();
        private readonly HashSet<long> _leaseIds = new HashSet<long>();
        private readonly HashSet<long> _instanceIds = new HashSet<long>();
        private long[] _disposeBuffer;

        internal ResourceScope(ProductResourceRuntime runtime, ResourceScopeId id, ResourceScopeKind kind, string name)
        {
            _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
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

        public int LeaseCount => _leaseIds.Count;

        public int LiveInstanceCount => _instanceIds.Count;

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
            _leaseIds.Clear();
            _instanceIds.Clear();
            _cancellation.Dispose();
        }

        internal bool TryRegisterLease(long leaseId)
        {
            return State == ResourceScopeState.Active && _leaseIds.Add(leaseId);
        }

        internal bool TryRegisterInstance(long instanceId)
        {
            return State == ResourceScopeState.Active && _instanceIds.Add(instanceId);
        }

        internal void RemoveLease(long leaseId)
        {
            _leaseIds.Remove(leaseId);
        }

        internal void RemoveInstance(long instanceId)
        {
            _instanceIds.Remove(instanceId);
        }

        internal void CopyLeaseIdsForDispose(out int count)
        {
            CopyIds(_leaseIds, ref _disposeBuffer, out count);
        }

        internal void CopyInstanceIdsForDispose(out int count)
        {
            CopyIds(_instanceIds, ref _disposeBuffer, out count);
        }

        internal long PeekLeaseId(int index) => _disposeBuffer[index];

        internal long PeekInstanceId(int index) => _disposeBuffer[index];

        private static void CopyIds(HashSet<long> ids, ref long[] buffer, out int count)
        {
            count = ids.Count;
            if (buffer == null || buffer.Length < count)
            {
                buffer = new long[count];
            }

            ids.CopyTo(buffer, 0);
        }
    }
}
