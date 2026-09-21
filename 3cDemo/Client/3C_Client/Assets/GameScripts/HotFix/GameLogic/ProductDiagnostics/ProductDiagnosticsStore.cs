using System;
using System.Collections.Generic;
using System.Collections;

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
        private readonly BoundedHistory<ProductCheckpointSnapshot> _history;

        public ProductDiagnosticsStore(int capacity)
        {
            _capacity = capacity > 0 ? capacity : throw new ArgumentOutOfRangeException(nameof(capacity));
            _history = new BoundedHistory<ProductCheckpointSnapshot>(_capacity);
        }

        public ProductCheckpointSnapshot Current { get; private set; }

        public IReadOnlyList<ProductCheckpointSnapshot> History => _history;

        public event Action<ProductCheckpointSnapshot> Changed;

        public ProductCheckpointSnapshot Freeze(string checkpoint, ResourceRuntimeSnapshot resources, MemoryRuntimeSnapshot memory, NetworkRuntimeSnapshot network)
        {
            Current = new ProductCheckpointSnapshot(checkpoint, DateTimeOffset.UtcNow, resources, memory, network);
            _history.Add(Current);

            Changed?.Invoke(Current);
            return Current;
        }
    }

    internal sealed class BoundedHistory<T> : IReadOnlyList<T>
    {
        private readonly List<T> _items;
        private readonly int _capacity;

        public BoundedHistory(int capacity)
        {
            _capacity = capacity;
            _items = new List<T>(checked(capacity + 1));
        }

        public T this[int index] => _items[index];
        public int Count => _items.Count;

        public void Add(T item)
        {
            _items.Add(item);
            while (_items.Count > _capacity)
            {
                _items.RemoveAt(0);
            }
        }

        public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
