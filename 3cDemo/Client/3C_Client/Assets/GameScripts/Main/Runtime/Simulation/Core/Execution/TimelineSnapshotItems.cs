using System;
using System.Collections.Generic;

namespace ThirdPersonSimulation
{
    public readonly struct TimelineSnapshotItems<T>
    {
        readonly T[] m_Items;

        TimelineSnapshotItems(T[] items) => m_Items = items;

        public int Count => m_Items?.Length ?? 0;
        public T this[int index] => (uint)index < (uint)Count
            ? m_Items[index] : throw new ArgumentOutOfRangeException(nameof(index));
        public ReadOnlySpan<T> Span => m_Items;

        public static TimelineSnapshotItems<T> CopyFrom(IReadOnlyList<T> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (source.Count == 0)
                return default;
            var items = new T[source.Count];
            for (int index = 0; index < items.Length; index++)
                items[index] = source[index];
            return new TimelineSnapshotItems<T>(items);
        }
    }
}
