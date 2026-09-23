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
            Resources = resources.IsValid ? resources : throw new ArgumentNullException(nameof(resources));
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
        private readonly T[] _items;
        private readonly int _capacity;
        private int _head;
        private int _count;

        public BoundedHistory(int capacity)
        {
            _capacity = capacity;
            _items = new T[checked(capacity + 1)];
        }

        public T this[int index]
        {
            get
            {
                if ((uint)index >= (uint)_count)
                    throw new ArgumentOutOfRangeException(nameof(index));
                return _items[(_head + index) % _items.Length];
            }
        }

        public int Count => _count;

        public void Add(T item)
        {
            int tail = (_head + _count) % _items.Length;
            _items[tail] = item;
            if (_count == _capacity)
            {
                _head = (_head + 1) % _items.Length;
            }
            else
            {
                _count++;
            }
        }

        public Enumerator GetEnumerator() => new Enumerator(this);

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct Enumerator : IEnumerator<T>
        {
            readonly BoundedHistory<T> _history;
            int _index;
            T _current;

            internal Enumerator(BoundedHistory<T> history)
            {
                _history = history;
                _index = 0;
                _current = default;
            }

            public T Current => _current;
            object IEnumerator.Current => Current;

            public bool MoveNext()
            {
                if (_index >= _history._count)
                    return false;
                _current = _history[_index++];
                return true;
            }

            public void Reset()
            {
                _index = 0;
                _current = default;
            }

            public void Dispose()
            {
            }
        }
    }
}
