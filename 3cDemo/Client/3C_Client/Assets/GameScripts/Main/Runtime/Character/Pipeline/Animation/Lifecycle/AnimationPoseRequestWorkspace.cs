using System;

namespace ThirdPersonCharacter.Pipeline.Animation.Lifecycle
{
    internal readonly struct AnimationPoseRequestRowIndex : IEquatable<AnimationPoseRequestRowIndex>
    {
        readonly int m_EncodedValue;

        internal AnimationPoseRequestRowIndex(int value)
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(value));
            m_EncodedValue = checked(value + 1);
        }

        internal int Value => m_EncodedValue - 1;
        internal bool IsValid => m_EncodedValue > 0;

        public bool Equals(AnimationPoseRequestRowIndex other) => m_EncodedValue == other.m_EncodedValue;
        public override bool Equals(object obj) => obj is AnimationPoseRequestRowIndex other && Equals(other);
        public override int GetHashCode() => m_EncodedValue;
        public static bool operator ==(AnimationPoseRequestRowIndex left, AnimationPoseRequestRowIndex right) =>
            left.Equals(right);
        public static bool operator !=(AnimationPoseRequestRowIndex left, AnimationPoseRequestRowIndex right) =>
            !left.Equals(right);
    }

    internal readonly struct AnimationPoseRequestWorkspaceRow
    {
        internal AnimationPoseRequestWorkspaceRow(
            AnimationPoseSourceId sourceId,
            AnimationPoseRequestRowIndex rowIndex,
            ulong leaseGeneration,
            ulong preparedAtCompletion,
            ClipSamplePlan[] clips,
            int clipOffset,
            int clipCapacity,
            float[] poseParameters,
            byte[] poseParameterAvailability,
            int parameterOffset,
            int parameterCount)
        {
            if (!sourceId.IsValid || !rowIndex.IsValid || leaseGeneration == 0 || preparedAtCompletion == 0 ||
                clips == null || clipOffset < 0 || clipCapacity <= 0 ||
                clipOffset > clips.Length - clipCapacity || poseParameters == null || poseParameterAvailability == null ||
                poseParameterAvailability.Length != poseParameters.Length || parameterOffset < 0 ||
                parameterCount <= 0 || parameterOffset > poseParameters.Length - parameterCount)
            {
                throw new ArgumentException("Animation pose request workspace row is invalid.");
            }

            SourceId = sourceId;
            RowIndex = rowIndex;
            LeaseGeneration = leaseGeneration;
            PreparedAtCompletion = preparedAtCompletion;
            Clips = clips;
            ClipOffset = clipOffset;
            ClipCapacity = clipCapacity;
            PoseParameters = poseParameters;
            PoseParameterAvailability = poseParameterAvailability;
            ParameterOffset = parameterOffset;
            ParameterCount = parameterCount;
        }

        internal AnimationPoseSourceId SourceId { get; }
        internal AnimationPoseRequestRowIndex RowIndex { get; }
        internal ulong LeaseGeneration { get; }
        internal ulong PreparedAtCompletion { get; }
        internal ClipSamplePlan[] Clips { get; }
        internal int ClipOffset { get; }
        internal int ClipCapacity { get; }
        internal float[] PoseParameters { get; }
        internal byte[] PoseParameterAvailability { get; }
        internal int ParameterOffset { get; }
        internal int ParameterCount { get; }

        internal bool IsValid =>
            SourceId.IsValid && RowIndex.IsValid && LeaseGeneration != 0 && PreparedAtCompletion != 0 &&
            Clips != null && ClipOffset >= 0 && ClipCapacity > 0 && ClipOffset <= Clips.Length - ClipCapacity &&
            PoseParameters != null && PoseParameterAvailability != null &&
            PoseParameterAvailability.Length == PoseParameters.Length && ParameterOffset >= 0 && ParameterCount > 0 &&
            ParameterOffset <= PoseParameters.Length - ParameterCount;
    }

    internal sealed class AnimationPoseRequestWorkspace : IDisposable, IAnimationReadOnlyBufferLease
    {
        readonly AnimationPoseRequestWorkspaceLayout m_Layout;
        readonly AnimationPoseSourceId[] m_SourceIds;
        readonly ulong[] m_LeaseGenerations;
        readonly ulong[] m_PreparedAt;
        readonly ClipSamplePlan[] m_Clips;
        readonly float[] m_PoseParameters;
        readonly byte[] m_PoseParameterAvailability;

        int m_Count;
        ulong m_CompletionIdentity;
        ulong m_LastCompletionIdentity;
        ulong m_LastLeaseGeneration;
        bool m_Disposed;

        internal AnimationPoseRequestWorkspace(AnimationPoseRequestWorkspaceLayout layout)
        {
            if (!layout.IsValid)
                throw new ArgumentException("Animation pose request workspace layout is invalid.", nameof(layout));
            m_Layout = layout;
            m_SourceIds = new AnimationPoseSourceId[layout.SourceCapacity];
            m_LeaseGenerations = new ulong[layout.SourceCapacity];
            m_PreparedAt = new ulong[layout.SourceCapacity];
            m_Clips = new ClipSamplePlan[layout.ClipPlanCapacity];
            m_PoseParameters = new float[layout.PoseParameterCapacity];
            m_PoseParameterAvailability = new byte[layout.PoseParameterCapacity];
        }

        internal int Count
        {
            get
            {
                RequireAlive();
                return m_Count;
            }
        }

        internal ulong CompletionIdentity
        {
            get
            {
                RequireAlive();
                return m_CompletionIdentity;
            }
        }

        internal void BeginFrame(ulong completionIdentity)
        {
            RequireAlive();
            if (completionIdentity == 0 || completionIdentity <= m_LastCompletionIdentity)
                throw new ArgumentOutOfRangeException(nameof(completionIdentity));
            Array.Clear(m_SourceIds, 0, m_SourceIds.Length);
            Array.Clear(m_LeaseGenerations, 0, m_LeaseGenerations.Length);
            Array.Clear(m_PreparedAt, 0, m_PreparedAt.Length);
            m_Count = 0;
            m_CompletionIdentity = completionIdentity;
            m_LastCompletionIdentity = completionIdentity;
        }

        internal AnimationPoseRequestWorkspaceRow PrepareRow(AnimationPoseSourceId sourceId)
        {
            RequireAlive();
            if (m_CompletionIdentity == 0)
                throw new InvalidOperationException("Animation pose request workspace has not begun a frame.");
            if (!sourceId.IsValid)
                throw new ArgumentException("Animation pose source identity is invalid.", nameof(sourceId));

            bool existing = TryFind(sourceId, out int rowIndex);
            if (!existing)
            {
                rowIndex = FindFreeRowIndex();
                m_SourceIds[rowIndex] = sourceId;
                m_Count++;
            }
            m_LeaseGenerations[rowIndex] = AllocateLeaseGeneration();
            if (m_LeaseGenerations[rowIndex] == 0)
                throw new InvalidOperationException($"Animation pose request row {rowIndex} has no active lease.");
            if (m_PreparedAt[rowIndex] == m_CompletionIdentity)
                throw new InvalidOperationException($"Animation pose source '{sourceId}' was prepared twice in one frame.");

            int clipOffset = checked(rowIndex * m_Layout.ClipStride);
            int parameterOffset = checked(rowIndex * m_Layout.ParameterStride);
            Array.Clear(m_Clips, clipOffset, m_Layout.ClipStride);
            Array.Clear(m_PoseParameters, parameterOffset, m_Layout.ParameterStride);
            Array.Clear(m_PoseParameterAvailability, parameterOffset, m_Layout.ParameterStride);
            m_PreparedAt[rowIndex] = m_CompletionIdentity;
            return new AnimationPoseRequestWorkspaceRow(
                sourceId,
                new AnimationPoseRequestRowIndex(rowIndex),
                m_LeaseGenerations[rowIndex],
                m_CompletionIdentity,
                m_Clips,
                clipOffset,
                m_Layout.ClipStride,
                m_PoseParameters,
                m_PoseParameterAvailability,
                parameterOffset,
                m_Layout.ParameterStride);
        }

        internal void RequireCurrent(AnimationPoseRequestWorkspaceRow row)
        {
            RequireAlive();
            if (!row.IsValid)
                throw new ArgumentException("Animation pose request workspace row is invalid.", nameof(row));
            if (m_CompletionIdentity == 0 || row.PreparedAtCompletion != m_CompletionIdentity)
            {
                throw new InvalidOperationException(
                    $"Animation pose request row was prepared for completion {row.PreparedAtCompletion}, not current completion {m_CompletionIdentity}.");
            }

            int rowIndex = row.RowIndex.Value;
            if (rowIndex < 0 || rowIndex >= m_SourceIds.Length)
                throw new ArgumentOutOfRangeException(nameof(row));
            if (!ReferenceEquals(row.Clips, m_Clips) || !ReferenceEquals(row.PoseParameters, m_PoseParameters) ||
                !ReferenceEquals(row.PoseParameterAvailability, m_PoseParameterAvailability) ||
                row.ClipOffset != checked(rowIndex * m_Layout.ClipStride) ||
                row.ClipCapacity != m_Layout.ClipStride ||
                row.ParameterOffset != checked(rowIndex * m_Layout.ParameterStride) ||
                row.ParameterCount != m_Layout.ParameterStride)
            {
                throw new InvalidOperationException("Animation pose request row does not belong to this workspace.");
            }
            if (!m_SourceIds[rowIndex].Equals(row.SourceId) ||
                m_LeaseGenerations[rowIndex] == 0 ||
                m_LeaseGenerations[rowIndex] != row.LeaseGeneration ||
                m_PreparedAt[rowIndex] != row.PreparedAtCompletion)
            {
                throw new InvalidOperationException(
                    $"Animation pose request row {rowIndex} lease is stale or no longer occupied by source '{row.SourceId}'.");
            }
        }

        internal void Reset()
        {
            RequireAlive();
            Array.Clear(m_SourceIds, 0, m_SourceIds.Length);
            Array.Clear(m_LeaseGenerations, 0, m_LeaseGenerations.Length);
            Array.Clear(m_PreparedAt, 0, m_PreparedAt.Length);
            Array.Clear(m_Clips, 0, m_Clips.Length);
            Array.Clear(m_PoseParameters, 0, m_PoseParameters.Length);
            Array.Clear(m_PoseParameterAvailability, 0, m_PoseParameterAvailability.Length);
            m_Count = 0;
            m_CompletionIdentity = 0;
        }

        bool TryFind(AnimationPoseSourceId sourceId, out int rowIndex)
        {
            for (int i = 0; i < m_SourceIds.Length; i++)
            {
                if (!m_SourceIds[i].Equals(sourceId))
                    continue;
                rowIndex = i;
                return true;
            }
            rowIndex = -1;
            return false;
        }

        int FindFreeRowIndex()
        {
            for (int i = 0; i < m_SourceIds.Length; i++)
            {
                if (!m_SourceIds[i].IsValid)
                    return i;
            }
            throw new InvalidOperationException("Animation pose request workspace capacity was exceeded.");
        }

        ulong AllocateLeaseGeneration()
        {
            if (m_LastLeaseGeneration == ulong.MaxValue)
                throw new InvalidOperationException("Animation pose request row lease generation was exhausted.");
            m_LastLeaseGeneration++;
            return m_LastLeaseGeneration;
        }

        void ClearRow(int rowIndex)
        {
            int clipOffset = checked(rowIndex * m_Layout.ClipStride);
            int parameterOffset = checked(rowIndex * m_Layout.ParameterStride);
            Array.Clear(m_Clips, clipOffset, m_Layout.ClipStride);
            Array.Clear(m_PoseParameters, parameterOffset, m_Layout.ParameterStride);
            Array.Clear(m_PoseParameterAvailability, parameterOffset, m_Layout.ParameterStride);
            m_SourceIds[rowIndex] = default;
            m_LeaseGenerations[rowIndex] = 0;
            m_PreparedAt[rowIndex] = 0;
        }

        void RequireAlive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(AnimationPoseRequestWorkspace));
        }

        void IAnimationReadOnlyBufferLease.RequireValid(ulong leaseIdentity)
        {
            RequireAlive();
            if (leaseIdentity == 0)
                throw new InvalidOperationException("Animation pose request buffer lease identity is invalid.");
            for (int i = 0; i < m_LeaseGenerations.Length; i++)
            {
                if (m_LeaseGenerations[i] != leaseIdentity)
                    continue;
                if (!m_SourceIds[i].IsValid || m_PreparedAt[i] == 0 ||
                    m_PreparedAt[i] != m_CompletionIdentity)
                {
                    break;
                }
                return;
            }
            throw new InvalidOperationException("Animation pose request buffer lease is stale.");
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            Reset();
            m_Disposed = true;
        }
    }
}
