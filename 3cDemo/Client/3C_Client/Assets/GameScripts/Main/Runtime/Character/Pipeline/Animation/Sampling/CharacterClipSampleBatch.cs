using System;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal sealed class CharacterClipSampleBatch
    {
        readonly ClipSamplePlan[] m_Plans;
        readonly float[] m_NormalizedWeights;
        int m_Count;

        internal CharacterClipSampleBatch(int capacity)
        {
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));
            m_Plans = new ClipSamplePlan[capacity];
            m_NormalizedWeights = new float[capacity];
        }

        internal int Count => m_Count;
        internal ClipSamplePlan this[int index] =>
            (uint)index < (uint)m_Count
                ? m_Plans[index]
                : throw new ArgumentOutOfRangeException(nameof(index));
        internal float GetNormalizedWeight(int index) =>
            (uint)index < (uint)m_Count
                ? m_NormalizedWeights[index]
                : throw new ArgumentOutOfRangeException(nameof(index));

        internal ClipSamplePlan RequireDominant()
        {
            if (m_Count == 0)
                throw new InvalidOperationException("Animation Clip sample batch is empty.");
            int selectedIndex = 0;
            for (int i = 1; i < m_Count; i++)
            {
                if (m_NormalizedWeights[i] > m_NormalizedWeights[selectedIndex])
                    selectedIndex = i;
            }
            return m_Plans[selectedIndex];
        }

        internal void CopyFrom(
            AnimationReadOnlyBuffer<ClipSamplePlan> source)
        {
            if (source.Count <= 0 || source.Count > m_Plans.Length)
                throw new ArgumentException("Animation Clip sample batch count is invalid.", nameof(source));
            float totalWeight = 0f;
            for (int i = 0; i < source.Count; i++)
            {
                ClipSamplePlan plan = source[i];
                if (!plan.IsValid || plan.Weight <= 0f)
                    throw new InvalidOperationException("Animation Clip sample batch contains an invalid plan.");
                for (int previous = 0; previous < i; previous++)
                {
                    if (m_Plans[previous].ClipBindingIndex == plan.ClipBindingIndex)
                        throw new InvalidOperationException("Animation Clip sample batch contains a duplicate Clip binding.");
                }
                m_Plans[i] = plan;
                totalWeight += plan.Weight;
            }
            if (!float.IsFinite(totalWeight) || totalWeight <= 0f)
                throw new InvalidOperationException("Animation Clip sample batch has no positive total weight.");
            for (int i = 0; i < source.Count; i++)
                m_NormalizedWeights[i] = m_Plans[i].Weight / totalWeight;
            Array.Clear(m_Plans, source.Count, m_Plans.Length - source.Count);
            Array.Clear(m_NormalizedWeights, source.Count, m_NormalizedWeights.Length - source.Count);
            m_Count = source.Count;
        }

        internal void Clear()
        {
            Array.Clear(m_Plans, 0, m_Plans.Length);
            Array.Clear(m_NormalizedWeights, 0, m_NormalizedWeights.Length);
            m_Count = 0;
        }
    }
}
