using System;
using Unity.Collections;

namespace ThirdPersonCharacter.Pipeline.Animation.BlendStack
{
    internal readonly struct AnimationBlendSourcePoseReleaseToken
    {
        internal AnimationBlendSourcePoseReleaseToken(
            int sourceIndex,
            AnimationPoseSourceId sourceId)
        {
            if (sourceIndex < 0 || !sourceId.IsValid)
                throw new ArgumentException("Animation source pose release token is invalid.");
            SourceIndex = sourceIndex;
            SourceId = sourceId;
        }

        internal int SourceIndex { get; }
        internal AnimationPoseSourceId SourceId { get; }
        internal bool IsValid => SourceIndex >= 0 && SourceId.IsValid;
    }

    internal sealed class AnimationBlendSourcePoseWorkspace : IDisposable
    {
        internal const int SinglePlayerHandoffCapacity = 2;
        internal const int PhysicalPageCount = 2;

        readonly int m_BoneCount;
        readonly int m_ParameterCount;
        readonly int m_SourceCapacity;
        readonly int m_PhysicalSourceCapacity;
        readonly AnimationPoseSourceId[] m_SourceIds;
        readonly AnimationPoseSourceId[] m_PendingSourceIds;
        readonly byte[] m_CommittedSlots;
        readonly byte[] m_DirtyFlags;
        readonly int[] m_DirtySourceIndices;
        readonly byte[] m_PreparedReleaseSlots;

        NativeArray<AnimationLocalBonePose> m_CurrentPose;
        NativeArray<AnimationBlendBoneVelocity> m_Velocity;
        NativeArray<float> m_PoseParameters;
        NativeArray<byte> m_PoseParameterAvailability;
        NativeArray<AnimationFootFeatureSample> m_LeftFootFeatures;
        NativeArray<AnimationFootFeatureSample> m_RightFootFeatures;
        NativeArray<float> m_VisualTimeScales;
        NativeArray<byte> m_HasFootFeatures;
        NativeArray<byte> m_PreviousAvailable;
        NativeArray<byte> m_HasPrevious;
        NativeArray<ulong> m_CompletedAt;
        NativeArray<AnimationSourcePoseCaptureFailure> m_CaptureFailures;
        NativeArray<ulong> m_SourcePoseContinuityIdentities;
        NativeArray<int> m_SourceOwnerIndices;

        int m_Count;
        int m_PendingCount;
        int m_DirtyCount;
        int m_PreparedReleaseCount;
        ulong m_CompletionIdentity;
        ulong m_CommittedCompletionIdentity;
        ulong m_LastCompletionIdentity;
        bool m_FrameOpen;
        bool m_Disposed;

        public AnimationBlendSourcePoseWorkspace(
            CharacterAnimationRigPayload rig,
            int parameterCount,
            int sourceCapacity)
        {
            if (rig == null)
                throw new ArgumentNullException(nameof(rig));
            rig.RequireValid();
            if (parameterCount <= 0 || sourceCapacity <= 0)
                throw new ArgumentOutOfRangeException();

            m_BoneCount = rig.PoseBoneCount;
            m_ParameterCount = parameterCount;
            m_SourceCapacity = sourceCapacity;
            m_PhysicalSourceCapacity = checked(sourceCapacity * PhysicalPageCount);
            m_SourceIds = new AnimationPoseSourceId[sourceCapacity];
            m_PendingSourceIds = new AnimationPoseSourceId[sourceCapacity];
            m_CommittedSlots = new byte[sourceCapacity];
            m_DirtyFlags = new byte[sourceCapacity];
            m_DirtySourceIndices = new int[sourceCapacity];
            m_PreparedReleaseSlots = new byte[sourceCapacity];

            int poseCapacity = checked(m_PhysicalSourceCapacity * m_BoneCount);
            int parameterCapacity = checked(m_PhysicalSourceCapacity * m_ParameterCount);
            try
            {
                m_CurrentPose = Allocate<AnimationLocalBonePose>(poseCapacity);
                m_Velocity = Allocate<AnimationBlendBoneVelocity>(poseCapacity);
                m_PoseParameters = Allocate<float>(parameterCapacity);
                m_PoseParameterAvailability = Allocate<byte>(parameterCapacity);
                m_LeftFootFeatures = Allocate<AnimationFootFeatureSample>(m_PhysicalSourceCapacity);
                m_RightFootFeatures = Allocate<AnimationFootFeatureSample>(m_PhysicalSourceCapacity);
                m_VisualTimeScales = Allocate<float>(m_PhysicalSourceCapacity);
                m_HasFootFeatures = Allocate<byte>(m_PhysicalSourceCapacity);
                m_PreviousAvailable = Allocate<byte>(m_PhysicalSourceCapacity);
                m_HasPrevious = Allocate<byte>(m_PhysicalSourceCapacity);
                m_CompletedAt = Allocate<ulong>(m_PhysicalSourceCapacity);
                m_CaptureFailures = Allocate<AnimationSourcePoseCaptureFailure>(m_PhysicalSourceCapacity);
                m_SourcePoseContinuityIdentities = Allocate<ulong>(m_PhysicalSourceCapacity);
                m_SourceOwnerIndices = Allocate<int>(m_PhysicalSourceCapacity);
            }
            catch
            {
                DisposeNativeArrays();
                throw;
            }
        }

        public int Count => m_FrameOpen ? m_PendingCount : m_Count;
        public int BoneCount => m_BoneCount;
        public int ParameterCount => m_ParameterCount;
        public ulong CompletionIdentity => m_CompletionIdentity;
        internal bool HasOpenFrame => m_FrameOpen;

        public void BeginFrame(ulong completionIdentity)
        {
            RequireNotDisposed();
            if (m_FrameOpen)
                throw new InvalidOperationException("Animation source pose frame is already open.");
            if (m_DirtyCount != 0)
                throw new InvalidOperationException("Animation source pose dirty journal was not closed.");
            if (m_PreparedReleaseCount != 0)
                throw new InvalidOperationException("Animation source pose prepared releases were not applied.");
            if (completionIdentity == 0 || completionIdentity <= m_LastCompletionIdentity)
                throw new ArgumentException("Animation source pose frame identity is invalid.", nameof(completionIdentity));

            m_PendingCount = m_Count;
            m_CompletionIdentity = completionIdentity;
            m_LastCompletionIdentity = completionIdentity;
            m_FrameOpen = true;
        }

        internal void CommitFrame(ulong completionIdentity)
        {
            RequireOpenFrame(completionIdentity);
            for (int i = 0; i < m_DirtyCount; i++)
            {
                int sourceIndex = m_DirtySourceIndices[i];
                int pendingSlot = PendingPhysicalIndex(sourceIndex);
                if (m_CompletedAt[pendingSlot] != completionIdentity ||
                    m_CaptureFailures[pendingSlot] != AnimationSourcePoseCaptureFailure.None)
                {
                    throw new InvalidOperationException(
                        "Animation source pose pending entry did not complete successfully.");
                }
            }

            for (int i = 0; i < m_DirtyCount; i++)
            {
                int sourceIndex = m_DirtySourceIndices[i];
                AnimationPoseSourceId pendingSourceId = m_PendingSourceIds[sourceIndex];
                if (pendingSourceId.IsValid)
                {
                    if (m_SourceIds[sourceIndex].IsValid)
                        throw new InvalidOperationException("Animation source pose pending identity overlaps a committed source.");
                    m_SourceIds[sourceIndex] = pendingSourceId;
                    m_PendingSourceIds[sourceIndex] = default;
                }
                m_CommittedSlots[sourceIndex] ^= 1;
                m_DirtyFlags[sourceIndex] = 0;
                m_DirtySourceIndices[i] = 0;
            }

            m_Count = m_PendingCount;
            m_DirtyCount = 0;
            m_CommittedCompletionIdentity = completionIdentity;
            m_FrameOpen = false;
        }

        internal void DiscardFrame(ulong completionIdentity)
        {
            RequireOpenFrame(completionIdentity);
            ClearDirtyJournal();
            DiscardPreparedReleases();
            m_PendingCount = m_Count;
            m_CompletionIdentity = m_CommittedCompletionIdentity;
            m_FrameOpen = false;
        }

        public AnimationPoseSourceCaptureBinding PrepareCapture(
            in AnimationResolvedPoseSourceSample sourceSample,
            float presentationDeltaSeconds)
        {
            AnimationPoseSampleRequest request = sourceSample.Request;
            if (!sourceSample.IsValid)
                throw new ArgumentException("Animation source pose capture request is invalid.");
            return PrepareCapture(
                request.SourceId,
                request.SourcePoseContinuityIdentity,
                request.SourceOwnerIndex,
                request.VisualTimeScale,
                request.PoseParameters,
                request.PoseParameterAvailability,
                sourceSample.LeftFootFeatures,
                sourceSample.RightFootFeatures,
                sourceSample.HasFootFeatures,
                presentationDeltaSeconds);
        }

        public AnimationPoseSourceCaptureBinding PrepareCapture(
            AnimationPoseSourceId sourceId,
            ulong sourcePoseContinuityIdentity,
            int sourceOwnerIndex,
            float visualTimeScale,
            AnimationReadOnlyBuffer<float> poseParameters,
            AnimationReadOnlyBuffer<byte> poseParameterAvailability,
            AnimationFootFeatureSample leftFootFeatures,
            AnimationFootFeatureSample rightFootFeatures,
            bool hasFootFeatures,
            float presentationDeltaSeconds)
        {
            RequireNotDisposed();
            if (!m_FrameOpen)
                throw new InvalidOperationException("Animation source pose frame is not open.");
            if (m_CompletionIdentity == 0)
                throw new InvalidOperationException("Animation source pose workspace has not begun a frame.");
            if (!sourceId.IsValid || sourcePoseContinuityIdentity == 0 || sourceOwnerIndex < 0 ||
                poseParameters.Count != m_ParameterCount ||
                poseParameterAvailability.Count != m_ParameterCount ||
                !float.IsFinite(visualTimeScale) || visualTimeScale < 0f ||
                hasFootFeatures != (leftFootFeatures.IsValid && rightFootFeatures.IsValid) ||
                !float.IsFinite(presentationDeltaSeconds) || presentationDeltaSeconds < 0f)
            {
                throw new ArgumentException("Animation source pose capture request is invalid.");
            }
            for (int parameterIndex = 0; parameterIndex < m_ParameterCount; parameterIndex++)
            {
                float value = poseParameters[parameterIndex];
                byte available = poseParameterAvailability[parameterIndex];
                if (!float.IsFinite(value) || available > 1)
                    throw new ArgumentException($"Animation source pose parameter #{parameterIndex} is invalid.");
            }

            bool found = TryFind(sourceId, out int sourceIndex);
            if (found && m_DirtyFlags[sourceIndex] != 0)
                throw new InvalidOperationException($"Animation source pose '{sourceId}' was prepared twice in one frame.");

            bool committed = found && m_SourceIds[sourceIndex].IsValid;
            if (!found)
            {
                sourceIndex = FindFreeSourceIndex();
                m_PendingSourceIds[sourceIndex] = sourceId;
                m_PendingCount++;
            }

            int committedSlot = CommittedPhysicalIndex(sourceIndex);
            if (committed && m_SourceOwnerIndices[committedSlot] != sourceOwnerIndex)
                throw new InvalidOperationException($"Animation source pose '{sourceId}' changed source owner identity.");

            int pendingSlot = PendingPhysicalIndex(sourceIndex);
            m_DirtyFlags[sourceIndex] = 1;
            m_DirtySourceIndices[m_DirtyCount++] = sourceIndex;
            m_PreviousAvailable[pendingSlot] =
                committed && m_SourcePoseContinuityIdentities[committedSlot] == sourcePoseContinuityIdentity
                    ? m_HasPrevious[committedSlot]
                    : (byte)0;
            m_HasPrevious[pendingSlot] = 0;
            m_CompletedAt[pendingSlot] = 0;
            m_CaptureFailures[pendingSlot] = AnimationSourcePoseCaptureFailure.None;
            m_SourcePoseContinuityIdentities[pendingSlot] = sourcePoseContinuityIdentity;
            m_SourceOwnerIndices[pendingSlot] = sourceOwnerIndex;
            m_LeftFootFeatures[pendingSlot] = leftFootFeatures;
            m_RightFootFeatures[pendingSlot] = rightFootFeatures;
            m_HasFootFeatures[pendingSlot] = hasFootFeatures ? (byte)1 : (byte)0;
            m_VisualTimeScales[pendingSlot] = visualTimeScale;

            int parameterOffset = pendingSlot * m_ParameterCount;
            for (int parameterIndex = 0; parameterIndex < m_ParameterCount; parameterIndex++)
            {
                m_PoseParameters[parameterOffset + parameterIndex] = poseParameters[parameterIndex];
                m_PoseParameterAvailability[parameterOffset + parameterIndex] =
                    poseParameterAvailability[parameterIndex];
            }

            int pendingPoseOffset = pendingSlot * m_BoneCount;
            int committedPoseOffset = committedSlot * m_BoneCount;
            return new AnimationPoseSourceCaptureBinding(
                sourceId,
                pendingSlot,
                m_CompletionIdentity,
                new NativeSlice<AnimationLocalBonePose>(m_CurrentPose, pendingPoseOffset, m_BoneCount),
                new NativeSlice<AnimationLocalBonePose>(m_CurrentPose, committedPoseOffset, m_BoneCount),
                new NativeSlice<AnimationBlendBoneVelocity>(m_Velocity, pendingPoseOffset, m_BoneCount),
                m_PreviousAvailable,
                m_HasPrevious,
                m_CompletedAt,
                m_CaptureFailures,
                presentationDeltaSeconds);
        }

        internal AnimationBlendSourcePoseNativeReadBinding RequireNativeReadBinding(ulong completionIdentity)
        {
            RequireNotDisposed();
            if (completionIdentity == 0)
                throw new ArgumentOutOfRangeException(nameof(completionIdentity));
            if (!m_FrameOpen || completionIdentity != m_CompletionIdentity)
                throw new InvalidOperationException("Animation source pose Native read completion identity is not current.");
            return new AnimationBlendSourcePoseNativeReadBinding(
                m_BoneCount,
                m_ParameterCount,
                m_PhysicalSourceCapacity,
                completionIdentity,
                m_CurrentPose,
                m_Velocity,
                m_PoseParameters,
                m_PoseParameterAvailability,
                m_LeftFootFeatures,
                m_RightFootFeatures,
                m_VisualTimeScales,
                m_HasFootFeatures,
                m_CompletedAt,
                m_CaptureFailures,
                m_SourceOwnerIndices);
        }

        internal AnimationBlendSourcePoseReleaseToken PrepareRelease(
            AnimationPoseSourceId sourceId)
        {
            RequireNotDisposed();
            if (!sourceId.IsValid)
                throw new ArgumentException("Animation source identity is invalid.", nameof(sourceId));
            if (!TryFind(sourceId, out int sourceIndex) || !m_SourceIds[sourceIndex].IsValid)
                throw new InvalidOperationException($"Animation source pose '{sourceId}' is not retained.");
            if (m_DirtyFlags[sourceIndex] != 0 ||
                m_PendingSourceIds[sourceIndex].IsValid)
            {
                throw new InvalidOperationException(
                    $"Animation source pose '{sourceId}' has a capture conflict with its release.");
            }
            if (m_PreparedReleaseSlots[sourceIndex] != 0)
                throw new InvalidOperationException($"Animation source pose '{sourceId}' release was prepared twice.");
            m_PreparedReleaseSlots[sourceIndex] = 1;
            m_PreparedReleaseCount++;
            return new AnimationBlendSourcePoseReleaseToken(
                sourceIndex,
                sourceId);
        }

        internal void ApplyPreparedRelease(
            in AnimationBlendSourcePoseReleaseToken token)
        {
            int sourceIndex = token.SourceIndex;
            m_SourceIds[sourceIndex] = default;
            m_PendingSourceIds[sourceIndex] = default;
            ClearSourceData(sourceIndex);
            m_CommittedSlots[sourceIndex] = 0;
            m_PreparedReleaseSlots[sourceIndex] = 0;
            m_Count--;
            m_PendingCount = m_Count;
            m_PreparedReleaseCount--;
        }

        internal void DiscardPreparedReleases()
        {
            RequireNotDisposed();
            Array.Clear(
                m_PreparedReleaseSlots,
                0,
                m_PreparedReleaseSlots.Length);
            m_PreparedReleaseCount = 0;
        }

        public void ResetContinuity()
        {
            RequireNotDisposed();
            if (m_FrameOpen)
                throw new InvalidOperationException("Animation source pose frame is open.");
            if (m_DirtyCount != 0)
                throw new InvalidOperationException("Animation source pose dirty journal was not closed.");

            for (int i = 0; i < m_SourceCapacity; i++)
            {
                if (!m_SourceIds[i].IsValid)
                    continue;
                ClearHistory(PhysicalIndex(i, 0));
                ClearFrameMetadata(PhysicalIndex(i, 0));
                ClearHistory(PhysicalIndex(i, 1));
                ClearFrameMetadata(PhysicalIndex(i, 1));
            }
            m_CompletionIdentity = 0;
            m_CommittedCompletionIdentity = 0;
        }

        bool TryFind(AnimationPoseSourceId sourceId, out int index)
        {
            for (int i = 0; i < m_SourceCapacity; i++)
            {
                if (!m_SourceIds[i].Equals(sourceId) && !m_PendingSourceIds[i].Equals(sourceId))
                    continue;
                index = i;
                return true;
            }
            index = -1;
            return false;
        }

        int FindFreeSourceIndex()
        {
            for (int i = 0; i < m_SourceCapacity; i++)
            {
                if (!m_SourceIds[i].IsValid && !m_PendingSourceIds[i].IsValid)
                    return i;
            }
            throw new InvalidOperationException("Animation source pose workspace capacity was exceeded.");
        }

        int CommittedPhysicalIndex(int sourceIndex) =>
            PhysicalIndex(sourceIndex, m_CommittedSlots[sourceIndex]);

        int PendingPhysicalIndex(int sourceIndex) =>
            PhysicalIndex(sourceIndex, m_CommittedSlots[sourceIndex] ^ 1);

        static int PhysicalIndex(int sourceIndex, int slot) =>
            checked(sourceIndex * 2 + slot);

        void ClearDirtyJournal()
        {
            for (int i = 0; i < m_DirtyCount; i++)
            {
                int sourceIndex = m_DirtySourceIndices[i];
                m_PendingSourceIds[sourceIndex] = default;
                m_DirtyFlags[sourceIndex] = 0;
                m_DirtySourceIndices[i] = 0;
            }
            m_DirtyCount = 0;
        }

        void ClearSourceData(int sourceIndex)
        {
            ClearPhysicalSlot(PhysicalIndex(sourceIndex, 0));
            ClearPhysicalSlot(PhysicalIndex(sourceIndex, 1));
        }

        void ClearPhysicalSlot(int physicalIndex)
        {
            ClearHistory(physicalIndex);
            ClearFrameMetadata(physicalIndex);
            m_SourceOwnerIndices[physicalIndex] = 0;
        }

        void ClearHistory(int physicalIndex)
        {
            m_SourcePoseContinuityIdentities[physicalIndex] = 0;
            m_PreviousAvailable[physicalIndex] = 0;
            m_HasPrevious[physicalIndex] = 0;
            m_CompletedAt[physicalIndex] = 0;
            m_CaptureFailures[physicalIndex] = AnimationSourcePoseCaptureFailure.None;
            int poseOffset = physicalIndex * m_BoneCount;
            for (int i = 0; i < m_BoneCount; i++)
            {
                m_CurrentPose[poseOffset + i] = default;
                m_Velocity[poseOffset + i] = default;
            }
        }

        void ClearFrameMetadata(int physicalIndex)
        {
            m_LeftFootFeatures[physicalIndex] = default;
            m_RightFootFeatures[physicalIndex] = default;
            m_HasFootFeatures[physicalIndex] = 0;
            m_VisualTimeScales[physicalIndex] = 0f;
            int parameterOffset = physicalIndex * m_ParameterCount;
            for (int i = 0; i < m_ParameterCount; i++)
            {
                m_PoseParameters[parameterOffset + i] = 0f;
                m_PoseParameterAvailability[parameterOffset + i] = 0;
            }
        }

        void RequireOpenFrame(ulong completionIdentity)
        {
            RequireNotDisposed();
            if (!m_FrameOpen || completionIdentity == 0 ||
                completionIdentity != m_CompletionIdentity)
            {
                throw new InvalidOperationException("Animation source pose frame lease is invalid.");
            }
        }

        static NativeArray<T> Allocate<T>(int length) where T : struct =>
            new NativeArray<T>(length, Allocator.Persistent, NativeArrayOptions.ClearMemory);

        void RequireNotDisposed()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(AnimationBlendSourcePoseWorkspace));
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            if (m_FrameOpen)
                throw new InvalidOperationException("Animation source pose frame is open.");
            DisposeNativeArrays();
            Array.Clear(m_SourceIds, 0, m_SourceIds.Length);
            Array.Clear(m_PendingSourceIds, 0, m_PendingSourceIds.Length);
            Array.Clear(m_CommittedSlots, 0, m_CommittedSlots.Length);
            Array.Clear(m_DirtyFlags, 0, m_DirtyFlags.Length);
            Array.Clear(m_DirtySourceIndices, 0, m_DirtySourceIndices.Length);
            Array.Clear(m_PreparedReleaseSlots, 0, m_PreparedReleaseSlots.Length);
            m_Count = 0;
            m_PendingCount = 0;
            m_DirtyCount = 0;
            m_PreparedReleaseCount = 0;
            m_CompletionIdentity = 0;
            m_CommittedCompletionIdentity = 0;
            m_LastCompletionIdentity = 0;
            m_Disposed = true;
        }

        void DisposeNativeArrays()
        {
            Dispose(ref m_CurrentPose);
            Dispose(ref m_Velocity);
            Dispose(ref m_PoseParameters);
            Dispose(ref m_PoseParameterAvailability);
            Dispose(ref m_LeftFootFeatures);
            Dispose(ref m_RightFootFeatures);
            Dispose(ref m_VisualTimeScales);
            Dispose(ref m_HasFootFeatures);
            Dispose(ref m_PreviousAvailable);
            Dispose(ref m_HasPrevious);
            Dispose(ref m_CompletedAt);
            Dispose(ref m_CaptureFailures);
            Dispose(ref m_SourcePoseContinuityIdentities);
            Dispose(ref m_SourceOwnerIndices);
        }

        static void Dispose<T>(ref NativeArray<T> array) where T : struct
        {
            if (array.IsCreated)
                array.Dispose();
            array = default;
        }
    }
}
