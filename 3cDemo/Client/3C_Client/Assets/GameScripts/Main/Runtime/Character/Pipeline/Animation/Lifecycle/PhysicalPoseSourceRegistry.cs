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

    internal readonly struct AnimationPhysicalSourceReleaseToken
    {
        internal AnimationPhysicalSourceReleaseToken(
            AnimationPhysicalSourceIndex sourceIndex,
            ulong generation,
            AnimationPoseSourceId sourceId)
        {
            if (!sourceIndex.IsValid || generation == 0 || !sourceId.IsValid)
                throw new ArgumentException("Animation physical source release token is invalid.");
            SourceIndex = sourceIndex;
            Generation = generation;
            SourceId = sourceId;
        }

        internal AnimationPhysicalSourceIndex SourceIndex { get; }
        internal ulong Generation { get; }
        internal AnimationPoseSourceId SourceId { get; }
        internal bool IsValid => SourceIndex.IsValid && Generation != 0 && SourceId.IsValid;
    }

    internal sealed class PhysicalPoseSourceRegistry : IDisposable
    {
        AnimationPoseSourceId[] m_SourceIds;
        PoseNodeId[] m_PoseNodeIds;
        int[] m_SourceOwnerIndices;
        ulong[] m_Generations;
        AnimationPoseSourceId[] m_PendingSourceIds;
        PoseNodeId[] m_PendingPoseNodeIds;
        int[] m_PendingSourceOwnerIndices;
        ulong[] m_PendingGenerations;
        byte[] m_PreparedReleaseSlots;
        int m_Count;
        int m_PendingCount;
        int m_PreparedReleaseCount;
        ulong m_LastGeneration;
        bool m_FrameOpen;
        bool m_FrameValidated;
        bool m_Disposed;

        internal PhysicalPoseSourceRegistry(int capacity)
        {
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));
            m_SourceIds = new AnimationPoseSourceId[capacity];
            m_PoseNodeIds = new PoseNodeId[capacity];
            m_SourceOwnerIndices = new int[capacity];
            m_Generations = new ulong[capacity];
            m_PendingSourceIds = new AnimationPoseSourceId[capacity];
            m_PendingPoseNodeIds = new PoseNodeId[capacity];
            m_PendingSourceOwnerIndices = new int[capacity];
            m_PendingGenerations = new ulong[capacity];
            m_PreparedReleaseSlots = new byte[capacity];
            for (int i = 0; i < m_SourceOwnerIndices.Length; i++)
            {
                m_SourceOwnerIndices[i] = -1;
                m_PendingSourceOwnerIndices[i] = -1;
            }
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
                return checked(m_Count + m_PendingCount);
            }
        }

        internal bool HasOpenFrame => m_FrameOpen;
        internal int PendingRegistrationCount
        {
            get
            {
                RequireAlive();
                RequireOpenFrame();
                return m_PendingCount;
            }
        }

        internal void BeginFrame()
        {
            RequireAlive();
            if (m_FrameOpen)
                throw new InvalidOperationException("Physical Pose Source frame is already open.");
            if (m_PreparedReleaseCount != 0)
                throw new InvalidOperationException("Physical Pose Source prepared releases were not applied.");
            ClearPending();
            m_FrameOpen = true;
            m_FrameValidated = false;
        }

        internal void ValidateFrame()
        {
            RequireAlive();
            RequireOpenFrame();
            if (checked(m_Count + m_PendingCount) > m_SourceIds.Length)
                throw new InvalidOperationException("Animation physical source capacity was exceeded.");
            m_FrameValidated = true;
        }

        internal void CommitFrame()
        {
            RequireAlive();
            RequireOpenFrame();
            if (!m_FrameValidated)
                throw new InvalidOperationException("Physical Pose Source frame was not validated.");
            for (int i = 0; i < m_PendingSourceIds.Length; i++)
            {
                if (!m_PendingSourceIds[i].IsValid)
                    continue;
                m_SourceIds[i] = m_PendingSourceIds[i];
                m_PoseNodeIds[i] = m_PendingPoseNodeIds[i];
                m_SourceOwnerIndices[i] = m_PendingSourceOwnerIndices[i];
                m_Generations[i] = m_PendingGenerations[i];
            }
            m_Count = checked(m_Count + m_PendingCount);
            ClearPending();
            m_FrameOpen = false;
            m_FrameValidated = false;
        }

        internal void DiscardFrame()
        {
            RequireAlive();
            RequireOpenFrame();
            ClearPending();
            ClearPreparedReleases();
            m_FrameOpen = false;
            m_FrameValidated = false;
        }

        internal AnimationPhysicalSourceIdentity GetPendingRegistration(int ordinal)
        {
            RequireAlive();
            RequireOpenFrame();
            if ((uint)ordinal >= (uint)m_PendingCount)
                throw new ArgumentOutOfRangeException(nameof(ordinal));
            int found = 0;
            for (int i = 0; i < m_PendingGenerations.Length; i++)
            {
                if (m_PendingGenerations[i] == 0)
                    continue;
                if (found++ == ordinal)
                    return new AnimationPhysicalSourceIdentity(
                        new AnimationPhysicalSourceIndex(i),
                        m_PendingGenerations[i]);
            }
            throw new InvalidOperationException("Physical Pose Source pending journal is inconsistent.");
        }

        internal AnimationPhysicalSourceIdentity Register(
            AnimationPoseSourceId sourceId,
            PoseNodeId poseNodeId,
            int sourceOwnerIndex)
        {
            RequireAlive();
            RequireOpenFrame();
            if (!sourceId.IsValid || !poseNodeId.IsValid || sourceOwnerIndex < 0)
                throw new ArgumentException("Animation physical source identity is invalid.");
            if (TryFind(sourceId, poseNodeId, out int existing))
            {
                int existingOwner = PendingIsOccupied(existing)
                    ? m_PendingSourceOwnerIndices[existing]
                    : m_SourceOwnerIndices[existing];
                if (existingOwner != sourceOwnerIndex)
                {
                    throw new InvalidOperationException(
                        $"Animation pose source '{sourceId}' is already registered with different physical metadata.");
                }
                return CreateIdentity(existing);
            }

            int index = FindFreeIndex();
            ulong generation = AllocateGeneration();
            m_PendingSourceIds[index] = sourceId;
            m_PendingPoseNodeIds[index] = poseNodeId;
            m_PendingSourceOwnerIndices[index] = sourceOwnerIndex;
            m_PendingGenerations[index] = generation;
            m_PendingCount++;
            m_FrameValidated = false;
            return new AnimationPhysicalSourceIdentity(new AnimationPhysicalSourceIndex(index), generation);
        }

        internal AnimationPhysicalSourceIdentity RequireIdentity(AnimationPoseSourceId sourceId, PoseNodeId nodeId)
        {
            RequireAlive();
            if (!sourceId.IsValid || !nodeId.IsValid)
                throw new ArgumentException("Animation pose source identity is invalid.");
            if (!TryFind(sourceId, nodeId, out int index))
                throw new InvalidOperationException($"Animation pose source '{sourceId}' has no physical identity for Player '{nodeId}'.");
            return CreateIdentity(index);
        }

        internal AnimationPoseSourceId RequireSourceId(AnimationPhysicalSourceIdentity identity)
        {
            int value = RequireOccupied(identity);
            return PendingIsOccupied(value)
                ? m_PendingSourceIds[value]
                : m_SourceIds[value];
        }

        internal PoseNodeId RequirePoseNodeId(AnimationPhysicalSourceIdentity identity)
        {
            int value = RequireOccupied(identity);
            return PendingIsOccupied(value)
                ? m_PendingPoseNodeIds[value]
                : m_PoseNodeIds[value];
        }

        internal int RequireSourceOwnerIndex(AnimationPhysicalSourceIdentity identity)
        {
            int value = RequireOccupied(identity);
            return PendingIsOccupied(value)
                ? m_PendingSourceOwnerIndices[value]
                : m_SourceOwnerIndices[value];
        }

        internal AnimationPhysicalSourceReleaseToken PrepareRelease(
            AnimationPhysicalSourceIdentity identity,
            AnimationPoseSourceId sourceId)
        {
            RequireAlive();
            if (!identity.IsValid ||
                identity.Index.Value < 0 ||
                identity.Index.Value >= m_SourceIds.Length ||
                !sourceId.IsValid)
            {
                throw new ArgumentException("Animation physical source release identity is invalid.");
            }
            int index = identity.Index.Value;
            if (!m_SourceIds[index].IsValid ||
                !m_PoseNodeIds[index].IsValid ||
                m_SourceOwnerIndices[index] < 0 ||
                m_Generations[index] == 0)
            {
                throw new InvalidOperationException(
                    $"Animation physical source identity ({index}, {identity.Generation}) is no longer committed.");
            }
            if (m_Generations[index] != identity.Generation)
            {
                throw new InvalidOperationException(
                    $"Animation physical source identity ({index}, {identity.Generation}) is stale; current generation is {m_Generations[index]}.");
            }
            if (!m_SourceIds[index].Equals(sourceId))
            {
                throw new InvalidOperationException(
                    $"Animation physical source identity does not belong to source '{sourceId}'.");
            }
            if (m_PreparedReleaseSlots[index] != 0)
                throw new InvalidOperationException("Animation physical source release was prepared twice.");
            m_PreparedReleaseSlots[index] = 1;
            m_PreparedReleaseCount++;
            return new AnimationPhysicalSourceReleaseToken(
                identity.Index,
                identity.Generation,
                sourceId);
        }

        internal void ApplyPreparedRelease(
            in AnimationPhysicalSourceReleaseToken token)
        {
            int index = token.SourceIndex.Value;
            Clear(index);
            m_PreparedReleaseSlots[index] = 0;
            m_Count--;
            m_PreparedReleaseCount--;
        }

        internal void Reset()
        {
            RequireAlive();
            if (m_FrameOpen)
                throw new InvalidOperationException("Physical Pose Source frame is open.");
            Array.Clear(m_SourceIds, 0, m_SourceIds.Length);
            Array.Clear(m_PoseNodeIds, 0, m_PoseNodeIds.Length);
            Array.Clear(m_Generations, 0, m_Generations.Length);
            for (int i = 0; i < m_SourceOwnerIndices.Length; i++)
                m_SourceOwnerIndices[i] = -1;
            m_Count = 0;
            ClearPending();
            ClearPreparedReleases();
        }

        AnimationPhysicalSourceIdentity CreateIdentity(int index)
        {
            ulong generation = PendingIsOccupied(index)
                ? m_PendingGenerations[index]
                : m_Generations[index];
            if (generation == 0)
                throw new InvalidOperationException($"Animation physical source slot {index} has no occupancy generation.");
            return new AnimationPhysicalSourceIdentity(
                new AnimationPhysicalSourceIndex(index),
                generation);
        }

        int RequireOccupied(AnimationPhysicalSourceIdentity identity)
        {
            RequireAlive();
            if (!identity.IsValid || identity.Index.Value < 0 || identity.Index.Value >= m_SourceIds.Length)
                throw new ArgumentOutOfRangeException(nameof(identity));
            int index = identity.Index.Value;
            bool pending = PendingIsOccupied(index);
            AnimationPoseSourceId sourceId = pending ? m_PendingSourceIds[index] : m_SourceIds[index];
            PoseNodeId nodeId = pending ? m_PendingPoseNodeIds[index] : m_PoseNodeIds[index];
            int ownerIndex = pending ? m_PendingSourceOwnerIndices[index] : m_SourceOwnerIndices[index];
            ulong generation = pending ? m_PendingGenerations[index] : m_Generations[index];
            if (!sourceId.IsValid || !nodeId.IsValid || ownerIndex < 0 || generation == 0)
            {
                throw new InvalidOperationException(
                    $"Animation physical source identity ({index}, {identity.Generation}) is no longer occupied.");
            }
            if (generation != identity.Generation)
            {
                throw new InvalidOperationException(
                    $"Animation physical source identity ({index}, {identity.Generation}) is stale; current generation is {generation}.");
            }
            return index;
        }

        bool TryFind(AnimationPoseSourceId sourceId, PoseNodeId nodeId, out int index)
        {
            for (int i = 0; i < m_PendingSourceIds.Length; i++)
            {
                if (!m_PendingSourceIds[i].Equals(sourceId) ||
                    !m_PendingPoseNodeIds[i].Equals(nodeId))
                    continue;
                index = i;
                return true;
            }
            for (int i = 0; i < m_SourceIds.Length; i++)
            {
                if (!m_SourceIds[i].Equals(sourceId) || !m_PoseNodeIds[i].Equals(nodeId))
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
                if (!m_SourceIds[i].IsValid && !m_PendingSourceIds[i].IsValid)
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
            m_PoseNodeIds[index] = default;
            m_SourceOwnerIndices[index] = -1;
            m_Generations[index] = 0;
        }

        bool PendingIsOccupied(int index) =>
            m_FrameOpen && m_PendingGenerations[index] != 0;

        void ClearPending()
        {
            Array.Clear(m_PendingSourceIds, 0, m_PendingSourceIds.Length);
            Array.Clear(m_PendingPoseNodeIds, 0, m_PendingPoseNodeIds.Length);
            Array.Clear(m_PendingGenerations, 0, m_PendingGenerations.Length);
            for (int i = 0; i < m_PendingSourceOwnerIndices.Length; i++)
                m_PendingSourceOwnerIndices[i] = -1;
            m_PendingCount = 0;
        }

        void ClearPreparedReleases()
        {
            Array.Clear(
                m_PreparedReleaseSlots,
                0,
                m_PreparedReleaseSlots.Length);
            m_PreparedReleaseCount = 0;
        }

        void RequireOpenFrame()
        {
            if (!m_FrameOpen)
                throw new InvalidOperationException("Physical Pose Source frame is not open.");
        }

        void RequireAlive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(PhysicalPoseSourceRegistry));
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            Reset();
            m_SourceIds = null;
            m_PoseNodeIds = null;
            m_SourceOwnerIndices = null;
            m_Generations = null;
            m_PendingSourceIds = null;
            m_PendingPoseNodeIds = null;
            m_PendingSourceOwnerIndices = null;
            m_PendingGenerations = null;
            m_PreparedReleaseSlots = null;
            m_Disposed = true;
        }
    }
}
