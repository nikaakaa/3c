using System;
using ThirdPersonCharacter.Pipeline.Animation.BlendStack;
using Unity.Collections;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal enum AnimationPoseNativeInvalidReason : byte
    {
        None = 0,
        SourceIncomplete = 1,
        SlotPlanInvalid = 2,
        SlotPoseInvalid = 3,
        SlotVelocityInvalid = 4,
        SlotParameterInvalid = 5,
        SlotContributionInvalid = 6,
        SlotFootFeatureInvalid = 7,
        RequiredPoseMissing = 8,
        PoseGraphInputIncomplete = 9,
        PoseGraphOperationInvalid = 10,
        PoseGraphOutputInvalid = 11,
        FinalStreamWriteInvalid = 12
    }

    internal readonly struct AnimationPrimitivePoseContribution
    {
        internal AnimationPrimitivePoseContribution(
            int physicalSlotIndex,
            int physicalSourceIndex,
            ulong physicalSourceGeneration,
            AnimationPoseContributionKind kind,
            int programProducerIndex,
            ulong contributionContinuityIdentity,
            float weight,
            float leftFootWeight,
            float rightFootWeight)
        {
            int kindValue = (int)kind;
            if (physicalSlotIndex < 0 ||
                kindValue < (int)AnimationPoseContributionKind.Live ||
                kindValue > (int)AnimationPoseContributionKind.Inertial ||
                kind == AnimationPoseContributionKind.Live &&
                (physicalSourceIndex < 0 || physicalSourceGeneration == 0 || programProducerIndex < 0) ||
                kind != AnimationPoseContributionKind.Live &&
                (physicalSourceIndex != -1 || physicalSourceGeneration != 0 || programProducerIndex != -1) ||
                contributionContinuityIdentity == 0 ||
                !float.IsFinite(weight) || weight < 0f || weight > 1f ||
                !float.IsFinite(leftFootWeight) || leftFootWeight < 0f || leftFootWeight > 1f ||
                !float.IsFinite(rightFootWeight) || rightFootWeight < 0f || rightFootWeight > 1f)
            {
                throw new ArgumentException("Primitive animation pose contribution is invalid.");
            }

            PhysicalSlotIndex = physicalSlotIndex;
            PhysicalSourceIndex = physicalSourceIndex;
            PhysicalSourceGeneration = physicalSourceGeneration;
            Kind = kind;
            ProgramProducerIndex = programProducerIndex;
            ContributionContinuityIdentity = contributionContinuityIdentity;
            Weight = weight;
            LeftFootWeight = leftFootWeight;
            RightFootWeight = rightFootWeight;
        }

        internal int PhysicalSlotIndex { get; }
        internal int PhysicalSourceIndex { get; }
        internal ulong PhysicalSourceGeneration { get; }
        internal AnimationPoseContributionKind Kind { get; }
        internal int ProgramProducerIndex { get; }
        internal ulong ContributionContinuityIdentity { get; }
        internal float Weight { get; }
        internal float LeftFootWeight { get; }
        internal float RightFootWeight { get; }
    }

    internal readonly struct AnimationPoseSlotNativeRange
    {
        internal AnimationPoseSlotNativeRange(
            int physicalSlotIndex,
            int poseOffset,
            int velocityOffset,
            int parameterOffset,
            int contributionOffset,
            int contributionCapacity,
            int denseContributionWeightOffset)
        {
            if (physicalSlotIndex < 0 || poseOffset < 0 || velocityOffset < 0 || parameterOffset < 0 ||
                contributionOffset < 0 || contributionCapacity <= 0 || denseContributionWeightOffset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(physicalSlotIndex));
            }

            PhysicalSlotIndex = physicalSlotIndex;
            PoseOffset = poseOffset;
            VelocityOffset = velocityOffset;
            ParameterOffset = parameterOffset;
            ContributionOffset = contributionOffset;
            ContributionCapacity = contributionCapacity;
            DenseContributionWeightOffset = denseContributionWeightOffset;
        }

        internal int PhysicalSlotIndex { get; }
        internal int PoseOffset { get; }
        internal int VelocityOffset { get; }
        internal int ParameterOffset { get; }
        internal int ContributionOffset { get; }
        internal int ContributionCapacity { get; }
        internal int DenseContributionWeightOffset { get; }
    }

    internal readonly struct AnimationPoseNativeAggregateLayout
    {
        internal AnimationPoseNativeAggregateLayout(
            int slotCount,
            int boneCount,
            int parameterCount,
            int totalSlotContributionCapacity,
            int poseValueCount,
            int poseValueContributionStride,
            int operationCount,
            int frameCacheCount,
            int outputPoseValueIndex,
            NativeArray<AnimationPoseSlotNativeRange> slotRanges)
        {
            if (slotCount <= 0 || boneCount <= 0 || parameterCount <= 0 || totalSlotContributionCapacity <= 0 ||
                poseValueCount <= 0 || poseValueContributionStride <= 0 || operationCount <= 0 || frameCacheCount <= 0 ||
                outputPoseValueIndex < 0 || outputPoseValueIndex >= poseValueCount)
            {
                throw new ArgumentOutOfRangeException(nameof(slotCount));
            }

            SlotCount = slotCount;
            BoneCount = boneCount;
            ParameterCount = parameterCount;
            TotalSlotContributionCapacity = totalSlotContributionCapacity;
            PoseValueCount = poseValueCount;
            PoseValueContributionStride = poseValueContributionStride;
            OperationCount = operationCount;
            FrameCacheCount = frameCacheCount;
            OutputPoseValueIndex = outputPoseValueIndex;
            SlotPoseCapacity = checked(slotCount * boneCount);
            SlotVelocityCapacity = checked(slotCount * boneCount);
            SlotParameterCapacity = checked(slotCount * parameterCount);
            SlotDenseContributionWeightCapacity = checked(totalSlotContributionCapacity * boneCount);
            PoseValuePoseCapacity = checked(poseValueCount * boneCount);
            PoseValueParameterCapacity = checked(poseValueCount * parameterCount);
            PoseValueContributionCapacity = checked(poseValueCount * poseValueContributionStride);
            PoseValueDenseContributionWeightCapacity = checked(PoseValueContributionCapacity * boneCount);
            RequireSlotRanges(slotRanges);
        }

        internal int SlotCount { get; }
        internal int BoneCount { get; }
        internal int ParameterCount { get; }
        internal int TotalSlotContributionCapacity { get; }
        internal int PoseValueCount { get; }
        internal int PoseValueContributionStride { get; }
        internal int OperationCount { get; }
        internal int FrameCacheCount { get; }
        internal int OutputPoseValueIndex { get; }
        internal int SlotPoseCapacity { get; }
        internal int SlotVelocityCapacity { get; }
        internal int SlotParameterCapacity { get; }
        internal int SlotDenseContributionWeightCapacity { get; }
        internal int PoseValuePoseCapacity { get; }
        internal int PoseValueParameterCapacity { get; }
        internal int PoseValueContributionCapacity { get; }
        internal int PoseValueDenseContributionWeightCapacity { get; }

        internal void RequireValid()
        {
            if (SlotCount <= 0 || BoneCount <= 0 || ParameterCount <= 0 || TotalSlotContributionCapacity <= 0 ||
                PoseValueCount <= 0 || PoseValueContributionStride <= 0 || OperationCount <= 0 || FrameCacheCount <= 0 ||
                OutputPoseValueIndex < 0 || OutputPoseValueIndex >= PoseValueCount ||
                checked(SlotCount * BoneCount) != SlotPoseCapacity ||
                checked(SlotCount * BoneCount) != SlotVelocityCapacity ||
                checked(SlotCount * ParameterCount) != SlotParameterCapacity ||
                checked(TotalSlotContributionCapacity * BoneCount) != SlotDenseContributionWeightCapacity ||
                checked(PoseValueCount * BoneCount) != PoseValuePoseCapacity ||
                checked(PoseValueCount * ParameterCount) != PoseValueParameterCapacity ||
                checked(PoseValueCount * PoseValueContributionStride) != PoseValueContributionCapacity ||
                checked(PoseValueContributionCapacity * BoneCount) != PoseValueDenseContributionWeightCapacity)
            {
                throw new InvalidOperationException("Animation pose Native aggregate layout is invalid.");
            }
        }

        internal void RequireSlotRanges(NativeArray<AnimationPoseSlotNativeRange> slotRanges)
        {
            RequireValid();
            if (!slotRanges.IsCreated || slotRanges.Length != SlotCount)
                throw new ArgumentException("Animation pose Slot ranges are invalid.", nameof(slotRanges));

            int contributionOffset = 0;
            for (int i = 0; i < slotRanges.Length; i++)
            {
                AnimationPoseSlotNativeRange range = slotRanges[i];
                int poseOffset = checked(i * BoneCount);
                int parameterOffset = checked(i * ParameterCount);
                int denseContributionWeightOffset = checked(contributionOffset * BoneCount);
                if (range.PhysicalSlotIndex != i || range.PoseOffset != poseOffset || range.VelocityOffset != poseOffset ||
                    range.ParameterOffset != parameterOffset || range.ContributionOffset != contributionOffset ||
                    range.ContributionCapacity <= 0 ||
                    range.DenseContributionWeightOffset != denseContributionWeightOffset)
                {
                    throw new ArgumentException($"Animation pose Slot range #{i} is not compact or stable.", nameof(slotRanges));
                }

                contributionOffset = checked(contributionOffset + range.ContributionCapacity);
            }

            if (contributionOffset != TotalSlotContributionCapacity ||
                checked(contributionOffset * BoneCount) != SlotDenseContributionWeightCapacity)
            {
                throw new ArgumentException("Animation pose Slot ranges do not close the aggregate capacity.", nameof(slotRanges));
            }
        }
    }

    internal readonly struct CharacterPoseGraphNativeBinding
    {
        internal CharacterPoseGraphNativeBinding(
            AnimationPoseNativeAggregateLayout layout,
            ulong completionIdentity,
            NativeArray<AnimationPoseSlotNativeRange> slotRanges,
            NativeArray<AnimationLocalBonePose> slotDenseLocalPoses,
            NativeArray<AnimationBlendBoneVelocity> slotDenseVelocities,
            NativeArray<float> slotPoseParameters,
            NativeArray<AnimationPrimitivePoseContribution> slotContributions,
            NativeArray<float> slotDenseContributionWeights,
            NativeArray<int> slotContributionCounts,
            NativeArray<float> slotOutputWeights,
            NativeArray<AnimationFootFeatureSample> slotLeftFootFeatures,
            NativeArray<AnimationFootFeatureSample> slotRightFootFeatures,
            NativeArray<byte> slotHasFootFeatures,
            NativeArray<PoseSlotFrameAvailability> slotAvailability,
            NativeArray<ulong> slotContinuityIdentities,
            NativeArray<AnimationPoseNativeInvalidReason> slotInvalidReasons,
            NativeArray<ulong> slotCompletedAt,
            NativeArray<AnimationLocalBonePose> valueDenseLocalPoses,
            NativeArray<float> valuePoseParameters,
            NativeArray<AnimationPrimitivePoseContribution> valueContributions,
            NativeArray<float> valueDenseContributionWeights,
            NativeArray<int> valueContributionCounts,
            NativeArray<float> valueOutputWeights,
            NativeArray<AnimationFootFeatureSample> valueLeftFootFeatures,
            NativeArray<AnimationFootFeatureSample> valueRightFootFeatures,
            NativeArray<byte> valueHasFootFeatures,
            NativeArray<PoseSlotFrameAvailability> valueAvailability,
            NativeArray<ulong> valueContinuityIdentities,
            NativeArray<AnimationPoseNativeInvalidReason> valueInvalidReasons,
            NativeArray<ulong> frameCacheCompletedAt,
            NativeArray<AnimationPoseNativeInvalidReason> poseGraphInvalidReason,
            NativeArray<int> poseGraphInvalidOperationIndex,
            NativeArray<ulong> poseGraphCompletedAt,
            NativeArray<ulong> finalAppliedAt)
        {
            if (completionIdentity == 0)
                throw new ArgumentOutOfRangeException(nameof(completionIdentity));

            Layout = layout;
            CompletionIdentity = completionIdentity;
            SlotRanges = slotRanges;
            SlotDenseLocalPoses = slotDenseLocalPoses;
            SlotDenseVelocities = slotDenseVelocities;
            SlotPoseParameters = slotPoseParameters;
            SlotContributions = slotContributions;
            SlotDenseContributionWeights = slotDenseContributionWeights;
            SlotContributionCounts = slotContributionCounts;
            SlotOutputWeights = slotOutputWeights;
            SlotLeftFootFeatures = slotLeftFootFeatures;
            SlotRightFootFeatures = slotRightFootFeatures;
            SlotHasFootFeatures = slotHasFootFeatures;
            SlotAvailability = slotAvailability;
            SlotContinuityIdentities = slotContinuityIdentities;
            SlotInvalidReasons = slotInvalidReasons;
            SlotCompletedAt = slotCompletedAt;
            ValueDenseLocalPoses = valueDenseLocalPoses;
            ValuePoseParameters = valuePoseParameters;
            ValueContributions = valueContributions;
            ValueDenseContributionWeights = valueDenseContributionWeights;
            ValueContributionCounts = valueContributionCounts;
            ValueOutputWeights = valueOutputWeights;
            ValueLeftFootFeatures = valueLeftFootFeatures;
            ValueRightFootFeatures = valueRightFootFeatures;
            ValueHasFootFeatures = valueHasFootFeatures;
            ValueAvailability = valueAvailability;
            ValueContinuityIdentities = valueContinuityIdentities;
            ValueInvalidReasons = valueInvalidReasons;
            FrameCacheCompletedAt = frameCacheCompletedAt;
            PoseGraphInvalidReason = poseGraphInvalidReason;
            PoseGraphInvalidOperationIndex = poseGraphInvalidOperationIndex;
            PoseGraphCompletedAt = poseGraphCompletedAt;
            FinalAppliedAt = finalAppliedAt;
            RequireValid();
        }

        internal AnimationPoseNativeAggregateLayout Layout { get; }
        internal ulong CompletionIdentity { get; }
        internal NativeArray<AnimationPoseSlotNativeRange> SlotRanges { get; }
        internal NativeArray<AnimationLocalBonePose> SlotDenseLocalPoses { get; }
        internal NativeArray<AnimationBlendBoneVelocity> SlotDenseVelocities { get; }
        internal NativeArray<float> SlotPoseParameters { get; }
        internal NativeArray<AnimationPrimitivePoseContribution> SlotContributions { get; }
        internal NativeArray<float> SlotDenseContributionWeights { get; }
        internal NativeArray<int> SlotContributionCounts { get; }
        internal NativeArray<float> SlotOutputWeights { get; }
        internal NativeArray<AnimationFootFeatureSample> SlotLeftFootFeatures { get; }
        internal NativeArray<AnimationFootFeatureSample> SlotRightFootFeatures { get; }
        internal NativeArray<byte> SlotHasFootFeatures { get; }
        internal NativeArray<PoseSlotFrameAvailability> SlotAvailability { get; }
        internal NativeArray<ulong> SlotContinuityIdentities { get; }
        internal NativeArray<AnimationPoseNativeInvalidReason> SlotInvalidReasons { get; }
        internal NativeArray<ulong> SlotCompletedAt { get; }
        internal NativeArray<AnimationLocalBonePose> ValueDenseLocalPoses { get; }
        internal NativeArray<float> ValuePoseParameters { get; }
        internal NativeArray<AnimationPrimitivePoseContribution> ValueContributions { get; }
        internal NativeArray<float> ValueDenseContributionWeights { get; }
        internal NativeArray<int> ValueContributionCounts { get; }
        internal NativeArray<float> ValueOutputWeights { get; }
        internal NativeArray<AnimationFootFeatureSample> ValueLeftFootFeatures { get; }
        internal NativeArray<AnimationFootFeatureSample> ValueRightFootFeatures { get; }
        internal NativeArray<byte> ValueHasFootFeatures { get; }
        internal NativeArray<PoseSlotFrameAvailability> ValueAvailability { get; }
        internal NativeArray<ulong> ValueContinuityIdentities { get; }
        internal NativeArray<AnimationPoseNativeInvalidReason> ValueInvalidReasons { get; }
        internal NativeArray<ulong> FrameCacheCompletedAt { get; }
        internal NativeArray<AnimationPoseNativeInvalidReason> PoseGraphInvalidReason { get; }
        internal NativeArray<int> PoseGraphInvalidOperationIndex { get; }
        internal NativeArray<ulong> PoseGraphCompletedAt { get; }
        internal NativeArray<ulong> FinalAppliedAt { get; }

        internal void RequireValid()
        {
            Layout.RequireSlotRanges(SlotRanges);
            if (CompletionIdentity == 0)
                throw new InvalidOperationException("Animation Pose Graph Native completion identity is invalid.");

            RequireLength(SlotDenseLocalPoses, Layout.SlotPoseCapacity);
            RequireLength(SlotDenseVelocities, Layout.SlotVelocityCapacity);
            RequireLength(SlotPoseParameters, Layout.SlotParameterCapacity);
            RequireLength(SlotContributions, Layout.TotalSlotContributionCapacity);
            RequireLength(SlotDenseContributionWeights, Layout.SlotDenseContributionWeightCapacity);
            RequireLength(SlotContributionCounts, Layout.SlotCount);
            RequireLength(SlotOutputWeights, Layout.SlotCount);
            RequireLength(SlotLeftFootFeatures, Layout.SlotCount);
            RequireLength(SlotRightFootFeatures, Layout.SlotCount);
            RequireLength(SlotHasFootFeatures, Layout.SlotCount);
            RequireLength(SlotAvailability, Layout.SlotCount);
            RequireLength(SlotContinuityIdentities, Layout.SlotCount);
            RequireLength(SlotInvalidReasons, Layout.SlotCount);
            RequireLength(SlotCompletedAt, Layout.SlotCount);
            RequireLength(ValueDenseLocalPoses, Layout.PoseValuePoseCapacity);
            RequireLength(ValuePoseParameters, Layout.PoseValueParameterCapacity);
            RequireLength(ValueContributions, Layout.PoseValueContributionCapacity);
            RequireLength(ValueDenseContributionWeights, Layout.PoseValueDenseContributionWeightCapacity);
            RequireLength(ValueContributionCounts, Layout.PoseValueCount);
            RequireLength(ValueOutputWeights, Layout.PoseValueCount);
            RequireLength(ValueLeftFootFeatures, Layout.PoseValueCount);
            RequireLength(ValueRightFootFeatures, Layout.PoseValueCount);
            RequireLength(ValueHasFootFeatures, Layout.PoseValueCount);
            RequireLength(ValueAvailability, Layout.PoseValueCount);
            RequireLength(ValueContinuityIdentities, Layout.PoseValueCount);
            RequireLength(ValueInvalidReasons, Layout.PoseValueCount);
            RequireLength(FrameCacheCompletedAt, Layout.FrameCacheCount);
            RequireLength(PoseGraphInvalidReason, 1);
            RequireLength(PoseGraphInvalidOperationIndex, 1);
            RequireLength(PoseGraphCompletedAt, 1);
            RequireLength(FinalAppliedAt, 1);
        }

        static void RequireLength<T>(NativeArray<T> values, int expectedLength) where T : struct
        {
            if (!values.IsCreated || values.Length != expectedLength)
                throw new ArgumentException("Animation Pose Graph Native container length is invalid.");
        }
    }

    internal readonly struct AnimationPoseSlotNativeWriteBinding
    {
        internal AnimationPoseSlotNativeWriteBinding(
            in CharacterPoseGraphNativeBinding aggregate,
            int physicalSlotIndex)
        {
            aggregate.RequireValid();
            if (physicalSlotIndex < 0 || physicalSlotIndex >= aggregate.Layout.SlotCount)
                throw new ArgumentOutOfRangeException(nameof(physicalSlotIndex));

            AnimationPoseSlotNativeRange range = aggregate.SlotRanges[physicalSlotIndex];
            if (range.PhysicalSlotIndex != physicalSlotIndex)
                throw new ArgumentException("Animation pose Slot Native range identity is invalid.", nameof(physicalSlotIndex));

            Range = range;
            CompletionIdentity = aggregate.CompletionIdentity;
            DenseLocalPoses = new NativeSlice<AnimationLocalBonePose>(aggregate.SlotDenseLocalPoses, range.PoseOffset, aggregate.Layout.BoneCount);
            DenseVelocities = new NativeSlice<AnimationBlendBoneVelocity>(aggregate.SlotDenseVelocities, range.VelocityOffset, aggregate.Layout.BoneCount);
            PoseParameters = new NativeSlice<float>(aggregate.SlotPoseParameters, range.ParameterOffset, aggregate.Layout.ParameterCount);
            Contributions = new NativeSlice<AnimationPrimitivePoseContribution>(aggregate.SlotContributions, range.ContributionOffset, range.ContributionCapacity);
            DenseContributionWeights = new NativeSlice<float>(
                aggregate.SlotDenseContributionWeights,
                range.DenseContributionWeightOffset,
                checked(range.ContributionCapacity * aggregate.Layout.BoneCount));
            ContributionCount = new NativeSlice<int>(aggregate.SlotContributionCounts, physicalSlotIndex, 1);
            OutputWeight = new NativeSlice<float>(aggregate.SlotOutputWeights, physicalSlotIndex, 1);
            LeftFootFeatures = new NativeSlice<AnimationFootFeatureSample>(aggregate.SlotLeftFootFeatures, physicalSlotIndex, 1);
            RightFootFeatures = new NativeSlice<AnimationFootFeatureSample>(aggregate.SlotRightFootFeatures, physicalSlotIndex, 1);
            HasFootFeatures = new NativeSlice<byte>(aggregate.SlotHasFootFeatures, physicalSlotIndex, 1);
            Availability = new NativeSlice<PoseSlotFrameAvailability>(aggregate.SlotAvailability, physicalSlotIndex, 1);
            ContinuityIdentity = new NativeSlice<ulong>(aggregate.SlotContinuityIdentities, physicalSlotIndex, 1);
            InvalidReason = new NativeSlice<AnimationPoseNativeInvalidReason>(aggregate.SlotInvalidReasons, physicalSlotIndex, 1);
            CompletedAt = new NativeSlice<ulong>(aggregate.SlotCompletedAt, physicalSlotIndex, 1);
        }

        internal AnimationPoseSlotNativeRange Range { get; }
        internal ulong CompletionIdentity { get; }
        internal NativeSlice<AnimationLocalBonePose> DenseLocalPoses { get; }
        internal NativeSlice<AnimationBlendBoneVelocity> DenseVelocities { get; }
        internal NativeSlice<float> PoseParameters { get; }
        internal NativeSlice<AnimationPrimitivePoseContribution> Contributions { get; }
        internal NativeSlice<float> DenseContributionWeights { get; }
        internal NativeSlice<int> ContributionCount { get; }
        internal NativeSlice<float> OutputWeight { get; }
        internal NativeSlice<AnimationFootFeatureSample> LeftFootFeatures { get; }
        internal NativeSlice<AnimationFootFeatureSample> RightFootFeatures { get; }
        internal NativeSlice<byte> HasFootFeatures { get; }
        internal NativeSlice<PoseSlotFrameAvailability> Availability { get; }
        internal NativeSlice<ulong> ContinuityIdentity { get; }
        internal NativeSlice<AnimationPoseNativeInvalidReason> InvalidReason { get; }
        internal NativeSlice<ulong> CompletedAt { get; }
    }

    internal readonly struct AnimationFinalPoseNativeReadBinding
    {
        internal AnimationFinalPoseNativeReadBinding(in CharacterPoseGraphNativeBinding aggregate)
        {
            aggregate.RequireValid();
            int valueIndex = aggregate.Layout.OutputPoseValueIndex;
            int poseOffset = checked(valueIndex * aggregate.Layout.BoneCount);
            int parameterOffset = checked(valueIndex * aggregate.Layout.ParameterCount);
            int contributionOffset = checked(valueIndex * aggregate.Layout.PoseValueContributionStride);
            int denseContributionWeightOffset = checked(contributionOffset * aggregate.Layout.BoneCount);

            CompletionIdentity = aggregate.CompletionIdentity;
            OutputPoseValueIndex = valueIndex;
            DenseLocalPoses = new NativeSlice<AnimationLocalBonePose>(aggregate.ValueDenseLocalPoses, poseOffset, aggregate.Layout.BoneCount);
            PoseParameters = new NativeSlice<float>(aggregate.ValuePoseParameters, parameterOffset, aggregate.Layout.ParameterCount);
            Contributions = new NativeSlice<AnimationPrimitivePoseContribution>(
                aggregate.ValueContributions,
                contributionOffset,
                aggregate.Layout.PoseValueContributionStride);
            DenseContributionWeights = new NativeSlice<float>(
                aggregate.ValueDenseContributionWeights,
                denseContributionWeightOffset,
                checked(aggregate.Layout.PoseValueContributionStride * aggregate.Layout.BoneCount));
            ContributionCount = new NativeSlice<int>(aggregate.ValueContributionCounts, valueIndex, 1);
            OutputWeight = new NativeSlice<float>(aggregate.ValueOutputWeights, valueIndex, 1);
            LeftFootFeatures = new NativeSlice<AnimationFootFeatureSample>(aggregate.ValueLeftFootFeatures, valueIndex, 1);
            RightFootFeatures = new NativeSlice<AnimationFootFeatureSample>(aggregate.ValueRightFootFeatures, valueIndex, 1);
            HasFootFeatures = new NativeSlice<byte>(aggregate.ValueHasFootFeatures, valueIndex, 1);
            Availability = new NativeSlice<PoseSlotFrameAvailability>(aggregate.ValueAvailability, valueIndex, 1);
            ContinuityIdentity = new NativeSlice<ulong>(aggregate.ValueContinuityIdentities, valueIndex, 1);
            OutputInvalidReason = new NativeSlice<AnimationPoseNativeInvalidReason>(aggregate.ValueInvalidReasons, valueIndex, 1);
            PoseGraphInvalidReason = new NativeSlice<AnimationPoseNativeInvalidReason>(aggregate.PoseGraphInvalidReason, 0, 1);
            PoseGraphInvalidOperationIndex = new NativeSlice<int>(aggregate.PoseGraphInvalidOperationIndex, 0, 1);
            PoseGraphCompletedAt = new NativeSlice<ulong>(aggregate.PoseGraphCompletedAt, 0, 1);
            AppliedAt = new NativeSlice<ulong>(aggregate.FinalAppliedAt, 0, 1);
        }

        internal ulong CompletionIdentity { get; }
        internal int OutputPoseValueIndex { get; }
        internal NativeSlice<AnimationLocalBonePose> DenseLocalPoses { get; }
        internal NativeSlice<float> PoseParameters { get; }
        internal NativeSlice<AnimationPrimitivePoseContribution> Contributions { get; }
        internal NativeSlice<float> DenseContributionWeights { get; }
        internal NativeSlice<int> ContributionCount { get; }
        internal NativeSlice<float> OutputWeight { get; }
        internal NativeSlice<AnimationFootFeatureSample> LeftFootFeatures { get; }
        internal NativeSlice<AnimationFootFeatureSample> RightFootFeatures { get; }
        internal NativeSlice<byte> HasFootFeatures { get; }
        internal NativeSlice<PoseSlotFrameAvailability> Availability { get; }
        internal NativeSlice<ulong> ContinuityIdentity { get; }
        internal NativeSlice<AnimationPoseNativeInvalidReason> OutputInvalidReason { get; }
        internal NativeSlice<AnimationPoseNativeInvalidReason> PoseGraphInvalidReason { get; }
        internal NativeSlice<int> PoseGraphInvalidOperationIndex { get; }
        internal NativeSlice<ulong> PoseGraphCompletedAt { get; }
        internal NativeSlice<ulong> AppliedAt { get; }
    }
}
