using System;
using Unity.Collections;

namespace ThirdPersonCharacter.Pipeline.Animation.BlendStack
{
    internal sealed class AnimationBlendSourcePoseWorkspace : IDisposable
    {
        readonly int m_BoneCount;
        readonly int m_ParameterCount;
        readonly AnimationPoseSourceId[] m_SourceIds;

        NativeArray<AnimationLocalBonePose> m_CurrentPose;
        NativeArray<AnimationLocalBonePose> m_PreviousPose;
        NativeArray<AnimationBlendBoneVelocity> m_Velocity;
        NativeArray<float> m_PoseParameters;
        NativeArray<AnimationFootFeatureSample> m_LeftFootFeatures;
        NativeArray<AnimationFootFeatureSample> m_RightFootFeatures;
        NativeArray<float> m_VisualTimeScales;
        NativeArray<byte> m_HasFootFeatures;
        NativeArray<byte> m_HasPrevious;
        NativeArray<ulong> m_PreparedAt;
        NativeArray<ulong> m_CompletedAt;
        NativeArray<ulong> m_SourcePoseContinuityIdentities;
        NativeArray<int> m_ProgramProducerIndices;

        int m_Count;
        ulong m_CompletionIdentity;
        ulong m_LastCompletionIdentity;
        bool m_Disposed;

        public AnimationBlendSourcePoseWorkspace(
            CharacterAnimationRigPayload rig,
            int parameterCount,
            int sourceCapacity)
        {
            if (rig == null)
                throw new ArgumentNullException(nameof(rig));
            rig.RequireValid();
            if (parameterCount <= 0 || sourceCapacity < 2)
                throw new ArgumentOutOfRangeException();
            m_BoneCount = rig.Bones.Count;
            m_ParameterCount = parameterCount;
            m_SourceIds = new AnimationPoseSourceId[sourceCapacity];
            int poseCapacity = checked(sourceCapacity * m_BoneCount);
            int parameterCapacity = checked(sourceCapacity * m_ParameterCount);
            try
            {
                m_CurrentPose = Allocate<AnimationLocalBonePose>(poseCapacity);
                m_PreviousPose = Allocate<AnimationLocalBonePose>(poseCapacity);
                m_Velocity = Allocate<AnimationBlendBoneVelocity>(poseCapacity);
                m_PoseParameters = Allocate<float>(parameterCapacity);
                m_LeftFootFeatures = Allocate<AnimationFootFeatureSample>(sourceCapacity);
                m_RightFootFeatures = Allocate<AnimationFootFeatureSample>(sourceCapacity);
                m_VisualTimeScales = Allocate<float>(sourceCapacity);
                m_HasFootFeatures = Allocate<byte>(sourceCapacity);
                m_HasPrevious = Allocate<byte>(sourceCapacity);
                m_PreparedAt = Allocate<ulong>(sourceCapacity);
                m_CompletedAt = Allocate<ulong>(sourceCapacity);
                m_SourcePoseContinuityIdentities = Allocate<ulong>(sourceCapacity);
                m_ProgramProducerIndices = Allocate<int>(sourceCapacity);
            }
            catch
            {
                DisposeNativeArrays();
                throw;
            }
        }

        public int Count => m_Count;
        public int BoneCount => m_BoneCount;
        public int ParameterCount => m_ParameterCount;
        public ulong CompletionIdentity => m_CompletionIdentity;

        public void BeginFrame(ulong completionIdentity)
        {
            RequireNotDisposed();
            if (completionIdentity == 0 || completionIdentity <= m_LastCompletionIdentity)
                throw new ArgumentException("Animation source pose frame identity is invalid.", nameof(completionIdentity));
            m_CompletionIdentity = completionIdentity;
            m_LastCompletionIdentity = completionIdentity;
        }

        public AnimationPoseSourceCaptureBinding PrepareCapture(
            in ResolvedAnimationPoseRequest request,
            float presentationDeltaSeconds)
        {
            RequireNotDisposed();
            if (m_CompletionIdentity == 0)
                throw new InvalidOperationException("Animation source pose workspace has not begun a frame.");
            bool footStateValid = request.HasFootFeatures
                ? request.LeftFootFeatures.IsValid && request.RightFootFeatures.IsValid
                : !request.LeftFootFeatures.IsValid && !request.RightFootFeatures.IsValid;
            if (!request.IsValid || request.PoseParameters.Count != m_ParameterCount || !footStateValid ||
                !float.IsFinite(request.VisualTimeScale) || request.VisualTimeScale < 0f ||
                !float.IsFinite(presentationDeltaSeconds) || presentationDeltaSeconds < 0f)
            {
                throw new ArgumentException("Animation source pose capture request is invalid.");
            }
            bool existing = TryFind(request.SourceId, out int sourceIndex);
            if (!existing)
            {
                sourceIndex = FindFreeSourceIndex();
                ClearSourceData(sourceIndex);
                m_SourceIds[sourceIndex] = request.SourceId;
                m_Count++;
            }
            if (m_PreparedAt[sourceIndex] == m_CompletionIdentity)
                throw new InvalidOperationException($"Animation source pose '{request.SourceId}' was prepared twice in one frame.");
            if (existing && m_ProgramProducerIndices[sourceIndex] != request.ProgramProducerIndex)
                throw new InvalidOperationException($"Animation source pose '{request.SourceId}' changed producer identity.");
            if (m_SourcePoseContinuityIdentities[sourceIndex] != request.SourcePoseContinuityIdentity)
            {
                ClearHistory(sourceIndex);
                m_SourcePoseContinuityIdentities[sourceIndex] = request.SourcePoseContinuityIdentity;
            }
            for (int parameterIndex = 0; parameterIndex < m_ParameterCount; parameterIndex++)
            {
                float value = request.PoseParameters[parameterIndex];
                if (!float.IsFinite(value))
                    throw new ArgumentException($"Animation source pose parameter #{parameterIndex} is invalid.");
                m_PoseParameters[sourceIndex * m_ParameterCount + parameterIndex] = value;
            }
            m_ProgramProducerIndices[sourceIndex] = request.ProgramProducerIndex;
            m_LeftFootFeatures[sourceIndex] = request.LeftFootFeatures;
            m_RightFootFeatures[sourceIndex] = request.RightFootFeatures;
            m_HasFootFeatures[sourceIndex] = request.HasFootFeatures ? (byte)1 : (byte)0;
            m_VisualTimeScales[sourceIndex] = request.VisualTimeScale;
            m_PreparedAt[sourceIndex] = m_CompletionIdentity;
            m_CompletedAt[sourceIndex] = 0;
            int poseOffset = sourceIndex * m_BoneCount;
            return new AnimationPoseSourceCaptureBinding(
                request.SourceId,
                sourceIndex,
                m_CompletionIdentity,
                new NativeSlice<AnimationLocalBonePose>(m_CurrentPose, poseOffset, m_BoneCount),
                new NativeSlice<AnimationLocalBonePose>(m_PreviousPose, poseOffset, m_BoneCount),
                new NativeSlice<AnimationBlendBoneVelocity>(m_Velocity, poseOffset, m_BoneCount),
                m_HasPrevious,
                m_CompletedAt,
                presentationDeltaSeconds);
        }

        internal AnimationBlendSourcePoseNativeReadBinding RequireNativeReadBinding(ulong completionIdentity)
        {
            RequireNotDisposed();
            if (completionIdentity == 0)
                throw new ArgumentOutOfRangeException(nameof(completionIdentity));
            if (completionIdentity != m_CompletionIdentity)
                throw new InvalidOperationException("Animation source pose Native read completion identity is not current.");
            return new AnimationBlendSourcePoseNativeReadBinding(
                m_BoneCount,
                m_ParameterCount,
                m_SourceIds.Length,
                completionIdentity,
                m_CurrentPose,
                m_Velocity,
                m_PoseParameters,
                m_LeftFootFeatures,
                m_RightFootFeatures,
                m_VisualTimeScales,
                m_HasFootFeatures,
                m_CompletedAt,
                m_ProgramProducerIndices);
        }

        public void ReleaseSource(AnimationPoseSourceId sourceId)
        {
            RequireNotDisposed();
            if (!sourceId.IsValid)
                throw new ArgumentException("Animation source identity is invalid.", nameof(sourceId));
            if (!TryFind(sourceId, out int sourceIndex))
                throw new InvalidOperationException($"Animation source pose '{sourceId}' is not retained.");
            m_SourceIds[sourceIndex] = default;
            ClearSourceData(sourceIndex);
            m_Count--;
        }

        public void ResetContinuity()
        {
            RequireNotDisposed();
            for (int i = 0; i < m_SourceIds.Length; i++)
            {
                if (!m_SourceIds[i].IsValid)
                    continue;
                ClearHistory(i);
                ClearFrameMetadata(i);
            }
            m_CompletionIdentity = 0;
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

        int FindFreeSourceIndex()
        {
            for (int i = 0; i < m_SourceIds.Length; i++)
            {
                if (!m_SourceIds[i].IsValid)
                    return i;
            }
            throw new InvalidOperationException("Animation source pose workspace capacity was exceeded.");
        }

        void ClearSourceData(int sourceIndex)
        {
            ClearHistory(sourceIndex);
            ClearFrameMetadata(sourceIndex);
            m_ProgramProducerIndices[sourceIndex] = 0;
        }

        void ClearHistory(int sourceIndex)
        {
            m_SourcePoseContinuityIdentities[sourceIndex] = 0;
            m_HasPrevious[sourceIndex] = 0;
            m_PreparedAt[sourceIndex] = 0;
            m_CompletedAt[sourceIndex] = 0;
            int poseOffset = sourceIndex * m_BoneCount;
            for (int i = 0; i < m_BoneCount; i++)
            {
                m_CurrentPose[poseOffset + i] = default;
                m_PreviousPose[poseOffset + i] = default;
                m_Velocity[poseOffset + i] = default;
            }
        }

        void ClearFrameMetadata(int sourceIndex)
        {
            m_LeftFootFeatures[sourceIndex] = default;
            m_RightFootFeatures[sourceIndex] = default;
            m_HasFootFeatures[sourceIndex] = 0;
            m_VisualTimeScales[sourceIndex] = 0f;
            int parameterOffset = sourceIndex * m_ParameterCount;
            for (int i = 0; i < m_ParameterCount; i++)
                m_PoseParameters[parameterOffset + i] = 0f;
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
            DisposeNativeArrays();
            Array.Clear(m_SourceIds, 0, m_SourceIds.Length);
            m_Count = 0;
            m_CompletionIdentity = 0;
            m_LastCompletionIdentity = 0;
            m_Disposed = true;
        }

        void DisposeNativeArrays()
        {
            Dispose(ref m_CurrentPose);
            Dispose(ref m_PreviousPose);
            Dispose(ref m_Velocity);
            Dispose(ref m_PoseParameters);
            Dispose(ref m_LeftFootFeatures);
            Dispose(ref m_RightFootFeatures);
            Dispose(ref m_VisualTimeScales);
            Dispose(ref m_HasFootFeatures);
            Dispose(ref m_HasPrevious);
            Dispose(ref m_PreparedAt);
            Dispose(ref m_CompletedAt);
            Dispose(ref m_SourcePoseContinuityIdentities);
            Dispose(ref m_ProgramProducerIndices);
        }

        static void Dispose<T>(ref NativeArray<T> array) where T : struct
        {
            if (array.IsCreated)
                array.Dispose();
            array = default;
        }
    }
}
