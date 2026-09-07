using System;
using Unity.Collections;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal sealed class CharacterAnimationScalarMixer : IDisposable
    {
        NativeArray<float> m_Values;
        NativeArray<byte> m_Availability;
        readonly byte[] m_Targets;
        bool m_Disposed;

        internal CharacterAnimationScalarMixer(int parameterCount)
        {
            if (parameterCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(parameterCount));
            m_Values = new NativeArray<float>(
                parameterCount,
                Allocator.Persistent,
                NativeArrayOptions.ClearMemory);
            m_Availability = new NativeArray<byte>(
                parameterCount,
                Allocator.Persistent,
                NativeArrayOptions.ClearMemory);
            m_Targets = new byte[parameterCount];
        }

        internal NativeArray<float> Values => m_Values;
        internal NativeArray<byte> Availability => m_Availability;
        internal int ParameterCount => m_Values.Length;

        internal void RegisterParameter(int parameterIndex)
        {
            RequireAlive();
            if ((uint)parameterIndex >= (uint)m_Targets.Length)
                throw new ArgumentOutOfRangeException(nameof(parameterIndex));
            m_Targets[parameterIndex] = 1;
        }

        internal void ClearTargets()
        {
            RequireAlive();
            for (int i = 0; i < m_Targets.Length; i++)
            {
                m_Targets[i] = 0;
                m_Values[i] = 0f;
                m_Availability[i] = 0;
            }
        }

        internal void Clear()
        {
            RequireAlive();
            for (int i = 0; i < m_Values.Length; i++)
            {
                if (m_Targets[i] != 0)
                {
                    m_Values[i] = 0f;
                    m_Availability[i] = 0;
                }
            }
        }

        internal void Accumulate(
            CharacterAclScalarTrackBinding[] bindings,
            int bindingCount,
            NativeArray<float> decodedValues,
            float normalizedWeight)
        {
            RequireAlive();
            if (bindings == null || bindingCount < 0 || bindingCount > bindings.Length ||
                !decodedValues.IsCreated ||
                !float.IsFinite(normalizedWeight) || normalizedWeight <= 0f)
                throw new ArgumentException("Animation scalar mix input is invalid.");
            for (int i = 0; i < bindingCount; i++)
            {
                CharacterAclScalarTrackBinding binding = bindings[i];
                if (binding == null || binding.ParameterIndex < 0 ||
                    binding.ParameterIndex >= m_Values.Length ||
                    m_Targets[binding.ParameterIndex] == 0)
                    throw new InvalidOperationException("Animation scalar binding is outside the parameter layout.");
                float value = binding.IsConstant
                    ? binding.DefaultValue
                    : decodedValues[binding.TrackIndex];
                if (!float.IsFinite(value))
                    throw new InvalidOperationException("Animation scalar sample is not finite.");
                m_Values[binding.ParameterIndex] += value * normalizedWeight;
                m_Availability[binding.ParameterIndex] = 1;
            }
        }

        internal void CopyTo(
            NativeSlice<float> values,
            NativeSlice<byte> availability)
        {
            RequireAlive();
            if (values.Length != m_Values.Length || availability.Length != m_Availability.Length)
                throw new ArgumentException("Animation scalar mix destination is invalid.");
            for (int i = 0; i < m_Values.Length; i++)
            {
                if (m_Targets[i] != 0)
                {
                    values[i] = m_Values[i];
                    availability[i] = m_Availability[i];
                }
            }
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Values.Dispose();
            m_Availability.Dispose();
            m_Disposed = true;
        }

        void RequireAlive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(CharacterAnimationScalarMixer));
        }
    }
}
