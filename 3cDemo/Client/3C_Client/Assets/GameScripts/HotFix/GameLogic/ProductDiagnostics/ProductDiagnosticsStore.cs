using System;
using System.Collections.Generic;

namespace GameLogic.ProductDiagnostics
{
    public sealed class ProductCheckpointSnapshot
    {
        public ProductCheckpointSnapshot(string checkpoint, DateTimeOffset capturedAt, ResourceRuntimeSnapshot resources, MemoryRuntimeSnapshot memory, NetworkRuntimeSnapshot network)
        {
            Checkpoint = string.IsNullOrWhiteSpace(checkpoint) ? throw new ArgumentException("Checkpoint is required.", nameof(checkpoint)) : checkpoint.Trim();
            CapturedAt = capturedAt;
            Resources = resources ?? throw new ArgumentNullException(nameof(resources));
            Memory = memory ?? throw new ArgumentNullException(nameof(memory));
            Network = network;
        }

        public string Checkpoint { get; }
        public DateTimeOffset CapturedAt { get; }
        public ResourceRuntimeSnapshot Resources { get; }
        public MemoryRuntimeSnapshot Memory { get; }
        public NetworkRuntimeSnapshot Network { get; }
    }

    public interface IProductCheckpointSnapshotSource
    {
        ProductCheckpointSnapshot Current { get; }
        IReadOnlyList<ProductCheckpointSnapshot> History { get; }
        event Action<ProductCheckpointSnapshot> Changed;
    }

    public sealed class ProductDiagnosticsStore : IProductCheckpointSnapshotSource
    {
        private readonly int _capacity;
        private readonly Queue<ProductCheckpointSnapshot> _history = new Queue<ProductCheckpointSnapshot>();

        public ProductDiagnosticsStore(int capacity)
        {
            _capacity = capacity > 0 ? capacity : throw new ArgumentOutOfRangeException(nameof(capacity));
        }

        public ProductCheckpointSnapshot Current { get; private set; }

        public IReadOnlyList<ProductCheckpointSnapshot> History => _history.ToArray();

        public event Action<ProductCheckpointSnapshot> Changed;

        public ProductCheckpointSnapshot Freeze(string checkpoint, ResourceRuntimeSnapshot resources, MemoryRuntimeSnapshot memory, NetworkRuntimeSnapshot network)
        {
            Current = new ProductCheckpointSnapshot(checkpoint, DateTimeOffset.UtcNow, resources, memory, network);
            _history.Enqueue(Current);
            while (_history.Count > _capacity)
            {
                _history.Dequeue();
            }

            Changed?.Invoke(Current);
            return Current;
        }
    }
}
