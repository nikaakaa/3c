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
        internal ref readonly ClipSamplePlan ElementAt(int index)
        {
            if ((uint)index >= (uint)m_Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            return ref m_Plans[index];
        }
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
            ref readonly ClipSamplePlan selected = ref m_Plans[selectedIndex];
            return selected;
        }

        internal void CopyFrom(
            in AnimationReadOnlyBuffer<ClipSamplePlan> source)
        {
            if (source.Count <= 0 || source.Count > m_Plans.Length)
                throw new ArgumentException("Animation Clip sample batch count is invalid.", nameof(source));
            float totalWeight = 0f;
            for (int i = 0; i < source.Count; i++)
            {
                ref readonly ClipSamplePlan plan = ref source.ElementAt(i);
                if (!plan.IsValid || plan.Weight <= 0f)
                    throw new InvalidOperationException("Animation Clip sample batch contains an invalid plan.");
                for (int previous = 0; previous < i; previous++)
                {
                    ref readonly ClipSamplePlan existing = ref m_Plans[previous];
                    if (existing.ClipBindingIndex == plan.ClipBindingIndex)
                        throw new InvalidOperationException("Animation Clip sample batch contains a duplicate Clip binding.");
                }
                m_Plans[i] = plan;
                totalWeight += plan.Weight;
            }
            if (!float.IsFinite(totalWeight) || totalWeight <= 0f)
                throw new InvalidOperationException("Animation Clip sample batch has no positive total weight.");
            for (int i = 0; i < source.Count; i++)
            {
                ref readonly ClipSamplePlan plan = ref m_Plans[i];
                m_NormalizedWeights[i] = plan.Weight / totalWeight;
            }
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
