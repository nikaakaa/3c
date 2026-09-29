using System;
using ThirdPersonSimulation.Fixed;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ThirdPersonSimulation;
using UnityEngine;

namespace BTSMTL.Timeline.Runtime
{
    public readonly struct TimelineRuntimeSampleView<T>
    {
        readonly TimelineRuntimeSampleBuffer<T> m_Buffer;
        readonly ulong m_Version;

        internal TimelineRuntimeSampleView(TimelineRuntimeSampleBuffer<T> buffer)
        {
            m_Buffer = buffer;
            m_Version = buffer.Version;
        }

        internal bool IsCurrent => m_Buffer != null && m_Buffer.Version == m_Version;

        public int Count
        {
            get
            {
                if (m_Buffer == null)
                    return 0;
                RequireCurrent();
                return m_Buffer.Count;
            }
        }

        public T this[int index]
        {
            get
            {
                RequireCurrent();
                return m_Buffer[index];
            }
        }

        void RequireCurrent()
        {
            if (m_Buffer == null || m_Buffer.Version != m_Version)
                throw new InvalidOperationException("Timeline sample view no longer belongs to a live frame.");
        }
    }

    sealed class TimelineRuntimeSampleBuffer<T> : ICollection<T>, IReadOnlyList<T>
    {
        readonly T[] m_Values;

        public TimelineRuntimeSampleBuffer(int capacity)
        {
            m_Values = capacity == 0 ? Array.Empty<T>() : new T[capacity];
        }

        public int Count { get; private set; }
        public ulong Version { get; private set; }
        public bool IsReadOnly => false;
        public T this[int index] => (uint)index < (uint)Count
            ? m_Values[index] : throw new ArgumentOutOfRangeException(nameof(index));
        public TimelineRuntimeSampleView<T> View => new(this);

        public void Add(T value)
        {
            if (Count == m_Values.Length)
                throw new InvalidOperationException("Timeline sample count exceeds the prepared content capacity.");
            m_Values[Count++] = value;
        }

        public void Clear()
        {
            Array.Clear(m_Values, 0, Count);
            Count = 0;
            Version = checked(Version + 1);
        }

        public void RemoveAt(int index)
        {
            if ((uint)index >= (uint)Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            int remaining = Count - index - 1;
            if (remaining != 0)
                Array.Copy(m_Values, index + 1, m_Values, index, remaining);
            m_Values[--Count] = default;
        }

        public void Sort(IComparer<T> comparer) => Array.Sort(m_Values, 0, Count, comparer);

        public bool Contains(T value) => Array.IndexOf(m_Values, value, 0, Count) >= 0;
        public void CopyTo(T[] array, int arrayIndex) => Array.Copy(m_Values, 0, array, arrayIndex, Count);
        public bool Remove(T value) => throw new NotSupportedException();
        public IEnumerator<T> GetEnumerator() => throw new NotSupportedException("Use the indexed Timeline sample view.");
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
