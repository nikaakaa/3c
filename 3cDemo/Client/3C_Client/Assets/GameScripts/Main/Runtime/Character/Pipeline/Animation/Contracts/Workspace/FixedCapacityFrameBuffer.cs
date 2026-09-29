using System;
using System.Collections;
using System.Collections.Generic;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal sealed class FixedCapacityFrameBuffer<T> : IReadOnlyList<T>
    {
        readonly T[] m_Items;

        internal FixedCapacityFrameBuffer(int capacity)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));
            m_Items = capacity == 0
                ? Array.Empty<T>()
                : new T[capacity];
        }

        public int Count { get; private set; }
        internal int Capacity => m_Items.Length;
        public T this[int index] =>
            (uint)index < (uint)Count
                ? m_Items[index]
                : throw new ArgumentOutOfRangeException(nameof(index));

        internal ref readonly T ElementAt(int index)
        {
            if ((uint)index >= (uint)Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            return ref m_Items[index];
        }

        internal void Add(in T item)
        {
            if (Count == m_Items.Length)
            {
                throw new InvalidOperationException(
                    $"Fixed frame buffer capacity '{m_Items.Length}' was exceeded.");
            }
            m_Items[Count++] = item;
        }

        internal void Clear()
        {
            if (RuntimeHelpers<T>.ContainsReferences)
                Array.Clear(m_Items, 0, Count);
            Count = 0;
        }

        internal void Sort(Comparison<T> comparison)
        {
            if (comparison == null)
                throw new ArgumentNullException(nameof(comparison));
            for (int i = 1; i < Count; i++)
            {
                T value = m_Items[i];
                int index = i - 1;
                while (index >= 0 &&
                       comparison(m_Items[index], value) > 0)
                {
                    m_Items[index + 1] = m_Items[index];
                    index--;
                }
                m_Items[index + 1] = value;
            }
        }

        public Enumerator GetEnumerator() => new Enumerator(this);
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        internal struct Enumerator : IEnumerator<T>
        {
            readonly FixedCapacityFrameBuffer<T> m_Owner;
            int m_Index;

            internal Enumerator(FixedCapacityFrameBuffer<T> owner)
            {
                m_Owner = owner;
                m_Index = -1;
            }

            public T Current => m_Owner[m_Index];
            object IEnumerator.Current => Current;
            public bool MoveNext() => ++m_Index < m_Owner.Count;
            public void Reset() => m_Index = -1;
            public void Dispose()
            {
            }
        }

        static class RuntimeHelpers<TValue>
        {
            internal static readonly bool ContainsReferences =
                System.Runtime.CompilerServices.RuntimeHelpers
                    .IsReferenceOrContainsReferences<TValue>();
        }
    }
}
