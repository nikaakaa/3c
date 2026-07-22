using System;

namespace ThirdPersonCharacter.Pipeline.Animation.Lifecycle
{
    internal readonly struct AnimationPhysicalSourceIndex : IEquatable<AnimationPhysicalSourceIndex>
    {
        readonly int m_EncodedValue;

        internal AnimationPhysicalSourceIndex(int value)
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(value));
            m_EncodedValue = checked(value + 1);
        }

        internal int Value => m_EncodedValue - 1;
        internal bool IsValid => m_EncodedValue > 0;

        public bool Equals(AnimationPhysicalSourceIndex other) => m_EncodedValue == other.m_EncodedValue;
        public override bool Equals(object obj) => obj is AnimationPhysicalSourceIndex other && Equals(other);
        public override int GetHashCode() => m_EncodedValue;
        public static bool operator ==(AnimationPhysicalSourceIndex left, AnimationPhysicalSourceIndex right) => left.Equals(right);
        public static bool operator !=(AnimationPhysicalSourceIndex left, AnimationPhysicalSourceIndex right) => !left.Equals(right);
    }

    internal readonly struct AnimationPhysicalSourceIdentity : IEquatable<AnimationPhysicalSourceIdentity>
    {
        internal AnimationPhysicalSourceIdentity(AnimationPhysicalSourceIndex index, ulong generation)
        {
            if (!index.IsValid)
                throw new ArgumentException("Animation physical source index is invalid.", nameof(index));
            if (generation == 0)
                throw new ArgumentOutOfRangeException(nameof(generation));
            Index = index;
            Generation = generation;
        }

        internal AnimationPhysicalSourceIndex Index { get; }
        internal ulong Generation { get; }
        internal bool IsValid => Index.IsValid && Generation != 0;

        public bool Equals(AnimationPhysicalSourceIdentity other) =>
            Index.Equals(other.Index) && Generation == other.Generation;

        public override bool Equals(object obj) => obj is AnimationPhysicalSourceIdentity other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Index, Generation);
        public static bool operator ==(AnimationPhysicalSourceIdentity left, AnimationPhysicalSourceIdentity right) =>
            left.Equals(right);
        public static bool operator !=(AnimationPhysicalSourceIdentity left, AnimationPhysicalSourceIdentity right) =>
            !left.Equals(right);
    }

    internal sealed class AnimationPoseSourcePhysicalRegistry : IDisposable
    {
        AnimationPoseSourceId[] m_SourceIds;
        PoseSlotId[] m_PoseSlotIds;
        int[] m_ProgramProducerIndices;
        ulong[] m_Generations;
        int m_Count;
        ulong m_LastGeneration;
        bool m_Disposed;

        internal AnimationPoseSourcePhysicalRegistry(int capacity)
        {
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));
            m_SourceIds = new AnimationPoseSourceId[capacity];
            m_PoseSlotIds = new PoseSlotId[capacity];
            m_ProgramProducerIndices = new int[capacity];
            m_Generations = new ulong[capacity];
            for (int i = 0; i < m_ProgramProducerIndices.Length; i++)
                m_ProgramProducerIndices[i] = -1;
        }

        internal int Capacity
        {
            get
            {
                RequireAlive();
                return m_SourceIds.Length;
            }
        }

        internal int Count
        {
            get
            {
                RequireAlive();
                return m_Count;
            }
        }

        internal AnimationPhysicalSourceIdentity Register(
            AnimationPoseSourceId sourceId,
            PoseSlotId poseSlotId,
            int programProducerIndex)
        {
            RequireAlive();
            if (!sourceId.IsValid || !poseSlotId.IsValid || programProducerIndex < 0)
                throw new ArgumentException("Animation physical source identity is invalid.");
            if (TryFind(sourceId, out int existing))
            {
                if (!m_PoseSlotIds[existing].Equals(poseSlotId) ||
                    m_ProgramProducerIndices[existing] != programProducerIndex)
                {
                    throw new InvalidOperationException(
                        $"Animation pose source '{sourceId}' is already registered with different physical metadata.");
                }
                return CreateIdentity(existing);
            }

            int index = FindFreeIndex();
            ulong generation = AllocateGeneration();
            m_SourceIds[index] = sourceId;
            m_PoseSlotIds[index] = poseSlotId;
            m_ProgramProducerIndices[index] = programProducerIndex;
            m_Generations[index] = generation;
            m_Count++;
            return new AnimationPhysicalSourceIdentity(new AnimationPhysicalSourceIndex(index), generation);
        }

        internal AnimationPhysicalSourceIdentity RequireIdentity(AnimationPoseSourceId sourceId)
        {
            RequireAlive();
            if (!sourceId.IsValid)
                throw new ArgumentException("Animation pose source identity is invalid.", nameof(sourceId));
            if (!TryFind(sourceId, out int index))
                throw new InvalidOperationException($"Animation pose source '{sourceId}' has no physical identity.");
            return CreateIdentity(index);
        }

        internal AnimationPoseSourceId RequireSourceId(AnimationPhysicalSourceIdentity identity)
        {
            int value = RequireOccupied(identity);
            return m_SourceIds[value];
        }

        internal PoseSlotId RequirePoseSlotId(AnimationPhysicalSourceIdentity identity)
        {
            int value = RequireOccupied(identity);
            return m_PoseSlotIds[value];
        }

        internal int RequireProgramProducerIndex(AnimationPhysicalSourceIdentity identity)
        {
            int value = RequireOccupied(identity);
            return m_ProgramProducerIndices[value];
        }

        internal void Release(AnimationPhysicalSourceIdentity identity, AnimationPoseSourceId sourceId)
        {
            RequireAlive();
            if (!sourceId.IsValid)
                throw new ArgumentException("Animation pose source identity is invalid.", nameof(sourceId));
            int index = RequireOccupied(identity);
            if (!m_SourceIds[index].Equals(sourceId))
            {
                throw new InvalidOperationException(
                    $"Animation physical source identity does not belong to source '{sourceId}'.");
            }
            Clear(index);
            m_Count--;
        }

        internal void Reset()
        {
            RequireAlive();
            Array.Clear(m_SourceIds, 0, m_SourceIds.Length);
            Array.Clear(m_PoseSlotIds, 0, m_PoseSlotIds.Length);
            Array.Clear(m_Generations, 0, m_Generations.Length);
            for (int i = 0; i < m_ProgramProducerIndices.Length; i++)
                m_ProgramProducerIndices[i] = -1;
            m_Count = 0;
        }

        AnimationPhysicalSourceIdentity CreateIdentity(int index)
        {
            if (m_Generations[index] == 0)
                throw new InvalidOperationException($"Animation physical source slot {index} has no occupancy generation.");
            return new AnimationPhysicalSourceIdentity(
                new AnimationPhysicalSourceIndex(index),
                m_Generations[index]);
        }

        int RequireOccupied(AnimationPhysicalSourceIdentity identity)
        {
            RequireAlive();
            if (!identity.IsValid || identity.Index.Value < 0 || identity.Index.Value >= m_SourceIds.Length)
                throw new ArgumentOutOfRangeException(nameof(identity));
            int index = identity.Index.Value;
            if (!m_SourceIds[index].IsValid || !m_PoseSlotIds[index].IsValid ||
                m_ProgramProducerIndices[index] < 0 || m_Generations[index] == 0)
            {
                throw new InvalidOperationException(
                    $"Animation physical source identity ({index}, {identity.Generation}) is no longer occupied.");
            }
            if (m_Generations[index] != identity.Generation)
            {
                throw new InvalidOperationException(
                    $"Animation physical source identity ({index}, {identity.Generation}) is stale; current generation is {m_Generations[index]}.");
            }
            return index;
        }

        bool TryFind(AnimationPoseSourceId sourceId, out int index)
        {
            for (int i = 0; i < m_SourceIds.Length; i++)
            {
                if (!m_SourceIds[i].Equals(sourceId))
                    continue;
                index = i;
                return true;
            }
            index = -1;
            return false;
        }

        int FindFreeIndex()
        {
            for (int i = 0; i < m_SourceIds.Length; i++)
            {
                if (!m_SourceIds[i].IsValid)
                    return i;
            }
            throw new InvalidOperationException("Animation physical source capacity was exceeded.");
        }

        ulong AllocateGeneration()
        {
            if (m_LastGeneration == ulong.MaxValue)
                throw new InvalidOperationException("Animation physical source generation was exhausted.");
            m_LastGeneration++;
            return m_LastGeneration;
        }

        void Clear(int index)
        {
            m_SourceIds[index] = default;
            m_PoseSlotIds[index] = default;
            m_ProgramProducerIndices[index] = -1;
            m_Generations[index] = 0;
        }

        void RequireAlive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(AnimationPoseSourcePhysicalRegistry));
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            Reset();
            m_SourceIds = null;
            m_PoseSlotIds = null;
            m_ProgramProducerIndices = null;
            m_Generations = null;
            m_Disposed = true;
        }
    }
}
