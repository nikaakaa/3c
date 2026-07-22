using System;
using ThirdPersonCharacter.Pipeline.Animation.BlendStack;
using Unity.Collections;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal sealed class AnimationPoseNativeWorkspace : IDisposable
    {
        AnimationPoseNativeAggregateLayout m_Layout;
        NativeArray<AnimationPoseSlotNativeRange> m_SlotRanges;
        NativeArray<AnimationLocalBonePose> m_SlotDenseLocalPoses;
        NativeArray<AnimationBlendBoneVelocity> m_SlotDenseVelocities;
        NativeArray<float> m_SlotPoseParameters;
        NativeArray<AnimationPrimitivePoseContribution> m_SlotContributions;
        NativeArray<float> m_SlotDenseContributionWeights;
        NativeArray<int> m_SlotContributionCounts;
        NativeArray<float> m_SlotOutputWeights;
        NativeArray<AnimationFootFeatureSample> m_SlotLeftFootFeatures;
        NativeArray<AnimationFootFeatureSample> m_SlotRightFootFeatures;
        NativeArray<byte> m_SlotHasFootFeatures;
        NativeArray<PoseSlotFrameAvailability> m_SlotAvailability;
        NativeArray<ulong> m_SlotContinuityIdentities;
        NativeArray<AnimationPoseNativeInvalidReason> m_SlotInvalidReasons;
        NativeArray<ulong> m_SlotCompletedAt;
        NativeArray<AnimationLocalBonePose> m_ValueDenseLocalPoses;
        NativeArray<float> m_ValuePoseParameters;
        NativeArray<AnimationPrimitivePoseContribution> m_ValueContributions;
        NativeArray<float> m_ValueDenseContributionWeights;
        NativeArray<int> m_ValueContributionCounts;
        NativeArray<float> m_ValueOutputWeights;
        NativeArray<AnimationFootFeatureSample> m_ValueLeftFootFeatures;
        NativeArray<AnimationFootFeatureSample> m_ValueRightFootFeatures;
        NativeArray<byte> m_ValueHasFootFeatures;
        NativeArray<PoseSlotFrameAvailability> m_ValueAvailability;
        NativeArray<ulong> m_ValueContinuityIdentities;
        NativeArray<AnimationPoseNativeInvalidReason> m_ValueInvalidReasons;
        NativeArray<ulong> m_FrameCacheCompletedAt;
        NativeArray<AnimationPoseNativeInvalidReason> m_PoseGraphInvalidReason;
        NativeArray<int> m_PoseGraphInvalidOperationIndex;
        NativeArray<ulong> m_PoseGraphCompletedAt;
        NativeArray<ulong> m_FinalAppliedAt;
        PoseSlotId[] m_PoseSlotIds;
        CharacterPoseGraphNativeBinding m_FrameBinding;
        ulong m_LastCompletionIdentity;
        ulong m_CurrentCompletionIdentity;
        bool m_Disposed;

        internal AnimationPoseNativeWorkspace(CharacterAnimationPresentationBindingIndex bindings)
        {
            try
            {
                if (bindings == null)
                    throw new ArgumentNullException(nameof(bindings));
                if (!bindings.IsValid || bindings.Projection == null)
                    throw new ArgumentException("Animation Presentation bindings are invalid.", nameof(bindings));

                CharacterPresentationProjection projection = bindings.Projection;
                projection.RequirePosePayload();
                CharacterPresentationPoseProgram program = projection.PoseProgram;
                program.RequireValid();
                int slotCount = program.Slots.Count;
                int boneCount = program.BoneCount;
                int parameterCount = program.Parameters.Count;
                int poseValueCount = program.PoseValueWorkspaceCount;
                if (slotCount <= 0 || boneCount <= 0 || parameterCount <= 0 || poseValueCount <= 0 ||
                    bindings.Slots.Count != slotCount || bindings.Channels.Count != slotCount ||
                    projection.BlendSlots.Count != slotCount ||
                    program.ContributionWorkspaceCount % poseValueCount != 0)
                {
                    throw new InvalidOperationException("Animation Pose Native workspace source layout is invalid.");
                }

                int poseValueContributionStride = program.ContributionWorkspaceCount / poseValueCount;
                if (poseValueContributionStride <= 0)
                    throw new InvalidOperationException("Animation Pose Native contribution stride is invalid.");
                CharacterPresentationPoseOperation outputOperation = program.Operations[program.OutputOperationIndex];
                if (outputOperation == null || outputOperation.Index != program.OutputOperationIndex ||
                    outputOperation.Code != CharacterPoseOperationCode.OutputPose ||
                    outputOperation.OutputPoseValueIndex < 0 || outputOperation.OutputPoseValueIndex >= poseValueCount)
                {
                    throw new InvalidOperationException("Animation Pose Native output operation is invalid.");
                }

                m_PoseSlotIds = new PoseSlotId[slotCount];
                int totalSlotContributionCapacity = 0;
                for (int i = 0; i < slotCount; i++)
                {
                    CharacterPresentationPoseSlotProgramEntry programSlot = program.Slots[i];
                    if (programSlot == null || programSlot.Index != i || !programSlot.PoseSlotId.IsValid ||
                        !programSlot.AnimationChannelId.IsValid ||
                        !bindings.TryGetSlot(programSlot.PoseSlotId, out ResolvedAnimationPoseSlot slot) ||
                        !bindings.TryGetSlot(programSlot.AnimationChannelId, out ResolvedAnimationPoseSlot channelSlot) ||
                        slot.Index != i || channelSlot.Index != i ||
                        slot.PoseSlotId != programSlot.PoseSlotId || channelSlot.PoseSlotId != programSlot.PoseSlotId ||
                        slot.AnimationChannelId != programSlot.AnimationChannelId ||
                        channelSlot.AnimationChannelId != programSlot.AnimationChannelId ||
                        slot.OutputPolicy != programSlot.OutputPolicy || channelSlot.OutputPolicy != programSlot.OutputPolicy ||
                        slot.BlendPayload == null || channelSlot.BlendPayload == null ||
                        !ReferenceEquals(slot.BlendPayload, channelSlot.BlendPayload) ||
                        slot.BlendPayload.PoseSlotId != programSlot.PoseSlotId ||
                        slot.BlendPayload.AnimationChannelId != programSlot.AnimationChannelId ||
                        slot.BlendPayload.OutputPolicy != programSlot.OutputPolicy ||
                        slot.BlendPayload.StackPolicy == null)
                    {
                        throw new InvalidOperationException($"Animation Pose Native Slot #{i} is not bound one-to-one.");
                    }

                    slot.BlendPayload.StackPolicy.RequireValid();
                    int contributionCapacity = checked(slot.BlendPayload.StackPolicy.MaxActiveSourceEntries + 2);
                    totalSlotContributionCapacity = checked(totalSlotContributionCapacity + contributionCapacity);
                    m_PoseSlotIds[i] = programSlot.PoseSlotId;
                }

                if (poseValueContributionStride < totalSlotContributionCapacity)
                {
                    throw new InvalidOperationException(
                        "Animation Pose Native value contribution stride cannot contain all physical Slot contributions.");
                }

                m_SlotRanges = Allocate<AnimationPoseSlotNativeRange>(slotCount);
                int contributionOffset = 0;
                for (int i = 0; i < slotCount; i++)
                {
                    ResolvedAnimationPoseSlot slot = bindings.Slots[m_PoseSlotIds[i]];
                    int contributionCapacity = checked(slot.BlendPayload.StackPolicy.MaxActiveSourceEntries + 2);
                    m_SlotRanges[i] = new AnimationPoseSlotNativeRange(
                        i,
                        checked(i * boneCount),
                        checked(i * boneCount),
                        checked(i * parameterCount),
                        contributionOffset,
                        contributionCapacity,
                        checked(contributionOffset * boneCount));
                    contributionOffset = checked(contributionOffset + contributionCapacity);
                }
                if (contributionOffset != totalSlotContributionCapacity)
                    throw new InvalidOperationException("Animation Pose Native Slot contribution capacity is inconsistent.");

                m_Layout = new AnimationPoseNativeAggregateLayout(
                    slotCount,
                    boneCount,
                    parameterCount,
                    totalSlotContributionCapacity,
                    poseValueCount,
                    poseValueContributionStride,
                    program.Operations.Count,
                    program.FrameCacheCount,
                    outputOperation.OutputPoseValueIndex,
                    m_SlotRanges);

                m_SlotDenseLocalPoses = Allocate<AnimationLocalBonePose>(m_Layout.SlotPoseCapacity);
                m_SlotDenseVelocities = Allocate<AnimationBlendBoneVelocity>(m_Layout.SlotVelocityCapacity);
                m_SlotPoseParameters = Allocate<float>(m_Layout.SlotParameterCapacity);
                m_SlotContributions = Allocate<AnimationPrimitivePoseContribution>(m_Layout.TotalSlotContributionCapacity);
                m_SlotDenseContributionWeights = Allocate<float>(m_Layout.SlotDenseContributionWeightCapacity);
                m_SlotContributionCounts = Allocate<int>(m_Layout.SlotCount);
                m_SlotOutputWeights = Allocate<float>(m_Layout.SlotCount);
                m_SlotLeftFootFeatures = Allocate<AnimationFootFeatureSample>(m_Layout.SlotCount);
                m_SlotRightFootFeatures = Allocate<AnimationFootFeatureSample>(m_Layout.SlotCount);
                m_SlotHasFootFeatures = Allocate<byte>(m_Layout.SlotCount);
                m_SlotAvailability = Allocate<PoseSlotFrameAvailability>(m_Layout.SlotCount);
                m_SlotContinuityIdentities = Allocate<ulong>(m_Layout.SlotCount);
                m_SlotInvalidReasons = Allocate<AnimationPoseNativeInvalidReason>(m_Layout.SlotCount);
                m_SlotCompletedAt = Allocate<ulong>(m_Layout.SlotCount);
                m_ValueDenseLocalPoses = Allocate<AnimationLocalBonePose>(m_Layout.PoseValuePoseCapacity);
                m_ValuePoseParameters = Allocate<float>(m_Layout.PoseValueParameterCapacity);
                m_ValueContributions = Allocate<AnimationPrimitivePoseContribution>(m_Layout.PoseValueContributionCapacity);
                m_ValueDenseContributionWeights = Allocate<float>(m_Layout.PoseValueDenseContributionWeightCapacity);
                m_ValueContributionCounts = Allocate<int>(m_Layout.PoseValueCount);
                m_ValueOutputWeights = Allocate<float>(m_Layout.PoseValueCount);
                m_ValueLeftFootFeatures = Allocate<AnimationFootFeatureSample>(m_Layout.PoseValueCount);
                m_ValueRightFootFeatures = Allocate<AnimationFootFeatureSample>(m_Layout.PoseValueCount);
                m_ValueHasFootFeatures = Allocate<byte>(m_Layout.PoseValueCount);
                m_ValueAvailability = Allocate<PoseSlotFrameAvailability>(m_Layout.PoseValueCount);
                m_ValueContinuityIdentities = Allocate<ulong>(m_Layout.PoseValueCount);
                m_ValueInvalidReasons = Allocate<AnimationPoseNativeInvalidReason>(m_Layout.PoseValueCount);
                m_FrameCacheCompletedAt = Allocate<ulong>(m_Layout.FrameCacheCount);
                m_PoseGraphInvalidReason = Allocate<AnimationPoseNativeInvalidReason>(1);
                m_PoseGraphInvalidOperationIndex = Allocate<int>(1);
                m_PoseGraphCompletedAt = Allocate<ulong>(1);
                m_FinalAppliedAt = Allocate<ulong>(1);
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        internal CharacterPoseGraphNativeBinding BeginFrame(ulong completionIdentity)
        {
            RequireAlive();
            if (completionIdentity == 0 || completionIdentity <= m_LastCompletionIdentity)
                throw new ArgumentOutOfRangeException(nameof(completionIdentity));

            m_LastCompletionIdentity = completionIdentity;
            m_CurrentCompletionIdentity = completionIdentity;
            for (int i = 0; i < m_Layout.SlotCount; i++)
            {
                m_SlotContributionCounts[i] = 0;
                m_SlotOutputWeights[i] = 0f;
                m_SlotHasFootFeatures[i] = 0;
                m_SlotAvailability[i] = PoseSlotFrameAvailability.Invalid;
                m_SlotContinuityIdentities[i] = 0;
                m_SlotInvalidReasons[i] = AnimationPoseNativeInvalidReason.SourceIncomplete;
                m_SlotCompletedAt[i] = 0;
            }
            for (int i = 0; i < m_Layout.PoseValueCount; i++)
            {
                m_ValueContributionCounts[i] = 0;
                m_ValueOutputWeights[i] = 0f;
                m_ValueHasFootFeatures[i] = 0;
                m_ValueAvailability[i] = PoseSlotFrameAvailability.Invalid;
                m_ValueContinuityIdentities[i] = 0;
                m_ValueInvalidReasons[i] = AnimationPoseNativeInvalidReason.PoseGraphInputIncomplete;
            }
            for (int i = 0; i < m_FrameCacheCompletedAt.Length; i++)
                m_FrameCacheCompletedAt[i] = 0;

            m_PoseGraphInvalidReason[0] = AnimationPoseNativeInvalidReason.PoseGraphInputIncomplete;
            m_PoseGraphInvalidOperationIndex[0] = -1;
            m_PoseGraphCompletedAt[0] = 0;
            m_FinalAppliedAt[0] = 0;
            m_FrameBinding = CreateBinding(completionIdentity);
            return m_FrameBinding;
        }

        internal AnimationPoseSlotNativeWriteBinding RequireSlotWriteBinding(
            int physicalSlotIndex,
            ulong completionIdentity)
        {
            RequireFrame(completionIdentity);
            if (physicalSlotIndex < 0 || physicalSlotIndex >= m_Layout.SlotCount)
                throw new ArgumentOutOfRangeException(nameof(physicalSlotIndex));
            return new AnimationPoseSlotNativeWriteBinding(in m_FrameBinding, physicalSlotIndex);
        }

        internal CharacterPoseGraphNativeBinding RequirePoseGraphBinding(ulong completionIdentity)
        {
            RequireFrame(completionIdentity);
            return m_FrameBinding;
        }

        internal AnimationFinalPoseNativeReadBinding RequireFinalReadBinding(ulong completionIdentity)
        {
            RequireFrame(completionIdentity);
            return new AnimationFinalPoseNativeReadBinding(in m_FrameBinding);
        }

        internal PoseSlotId RequirePoseSlotId(int physicalSlotIndex)
        {
            RequireAlive();
            if (physicalSlotIndex < 0 || physicalSlotIndex >= m_PoseSlotIds.Length ||
                !m_PoseSlotIds[physicalSlotIndex].IsValid)
            {
                throw new ArgumentOutOfRangeException(nameof(physicalSlotIndex));
            }
            return m_PoseSlotIds[physicalSlotIndex];
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            m_FrameBinding = default;
            m_CurrentCompletionIdentity = 0;
            DisposeArray(ref m_FinalAppliedAt);
            DisposeArray(ref m_PoseGraphCompletedAt);
            DisposeArray(ref m_PoseGraphInvalidOperationIndex);
            DisposeArray(ref m_PoseGraphInvalidReason);
            DisposeArray(ref m_FrameCacheCompletedAt);
            DisposeArray(ref m_ValueInvalidReasons);
            DisposeArray(ref m_ValueContinuityIdentities);
            DisposeArray(ref m_ValueAvailability);
            DisposeArray(ref m_ValueHasFootFeatures);
            DisposeArray(ref m_ValueRightFootFeatures);
            DisposeArray(ref m_ValueLeftFootFeatures);
            DisposeArray(ref m_ValueOutputWeights);
            DisposeArray(ref m_ValueContributionCounts);
            DisposeArray(ref m_ValueDenseContributionWeights);
            DisposeArray(ref m_ValueContributions);
            DisposeArray(ref m_ValuePoseParameters);
            DisposeArray(ref m_ValueDenseLocalPoses);
            DisposeArray(ref m_SlotCompletedAt);
            DisposeArray(ref m_SlotInvalidReasons);
            DisposeArray(ref m_SlotContinuityIdentities);
            DisposeArray(ref m_SlotAvailability);
            DisposeArray(ref m_SlotHasFootFeatures);
            DisposeArray(ref m_SlotRightFootFeatures);
            DisposeArray(ref m_SlotLeftFootFeatures);
            DisposeArray(ref m_SlotOutputWeights);
            DisposeArray(ref m_SlotContributionCounts);
            DisposeArray(ref m_SlotDenseContributionWeights);
            DisposeArray(ref m_SlotContributions);
            DisposeArray(ref m_SlotPoseParameters);
            DisposeArray(ref m_SlotDenseVelocities);
            DisposeArray(ref m_SlotDenseLocalPoses);
            DisposeArray(ref m_SlotRanges);
            m_PoseSlotIds = null;
        }

        CharacterPoseGraphNativeBinding CreateBinding(ulong completionIdentity)
        {
            return new CharacterPoseGraphNativeBinding(
                m_Layout,
                completionIdentity,
                m_SlotRanges,
                m_SlotDenseLocalPoses,
                m_SlotDenseVelocities,
                m_SlotPoseParameters,
                m_SlotContributions,
                m_SlotDenseContributionWeights,
                m_SlotContributionCounts,
                m_SlotOutputWeights,
                m_SlotLeftFootFeatures,
                m_SlotRightFootFeatures,
                m_SlotHasFootFeatures,
                m_SlotAvailability,
                m_SlotContinuityIdentities,
                m_SlotInvalidReasons,
                m_SlotCompletedAt,
                m_ValueDenseLocalPoses,
                m_ValuePoseParameters,
                m_ValueContributions,
                m_ValueDenseContributionWeights,
                m_ValueContributionCounts,
                m_ValueOutputWeights,
                m_ValueLeftFootFeatures,
                m_ValueRightFootFeatures,
                m_ValueHasFootFeatures,
                m_ValueAvailability,
                m_ValueContinuityIdentities,
                m_ValueInvalidReasons,
                m_FrameCacheCompletedAt,
                m_PoseGraphInvalidReason,
                m_PoseGraphInvalidOperationIndex,
                m_PoseGraphCompletedAt,
                m_FinalAppliedAt);
        }

        void RequireFrame(ulong completionIdentity)
        {
            RequireAlive();
            if (completionIdentity == 0 || completionIdentity != m_CurrentCompletionIdentity ||
                m_FrameBinding.CompletionIdentity != completionIdentity)
            {
                throw new InvalidOperationException("Animation Pose Native frame completion identity does not match the active frame.");
            }
        }

        void RequireAlive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(AnimationPoseNativeWorkspace));
        }

        static NativeArray<T> Allocate<T>(int length) where T : struct
        {
            if (length <= 0)
                throw new ArgumentOutOfRangeException(nameof(length));
            return new NativeArray<T>(length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        }

        static void DisposeArray<T>(ref NativeArray<T> values) where T : struct
        {
            if (values.IsCreated)
                values.Dispose();
            values = default;
        }
    }
}
