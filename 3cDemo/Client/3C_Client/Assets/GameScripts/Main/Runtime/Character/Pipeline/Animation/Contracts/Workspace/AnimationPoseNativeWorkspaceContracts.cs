using System;
using ThirdPersonCharacter.Pipeline.Animation.BlendStack;
using Unity.Collections;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public enum AnimationPoseNativeInvalidReason : byte
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
        FinalPhysicalWriteInvalid = 12,
        PoseConstraintInvalid = 13,
        SourcePhysicalPoseInvalid = 14,
        SourceVirtualBoneInvalid = 15,
        SourcePoseHistoryInvalid = 16,
        FootGroundingInvalid = 17,
        PoseSpaceConversionInvalid = 18,
        WorldContextUnavailable = 19,
        FullBodyIkGoalSetInvalid = 20,
        FullBodyIkSolverInvalid = 21,
        PredictiveFootPlacementModifierInvalid = 22
    }

    internal enum AnimationFinalPoseWriteOutcome : byte
    {
        None = 0,
        Committed = 1,
        TypedInvalid = 2,
        Faulted = 3
    }

    internal readonly struct PoseDiscontinuityNativeEndpoint
    {
        PoseDiscontinuityNativeEndpoint(
            FixedString128Bytes timelineAuthoringId,
            FixedString128Bytes trackAuthoringId,
            ulong playbackGeneration,
            int presentationPoseSourceIndex,
            AnimationPoseSourceKind sourceKind,
            ulong selectionGeneration,
            ulong sourceActionInstanceId)
        {
            TimelineAuthoringId = timelineAuthoringId;
            TrackAuthoringId = trackAuthoringId;
            PlaybackGeneration = playbackGeneration;
            PresentationPoseSourceIndex = presentationPoseSourceIndex;
            SourceKind = sourceKind;
            SelectionGeneration = selectionGeneration;
            SourceActionInstanceId = sourceActionInstanceId;
        }

        internal FixedString128Bytes TimelineAuthoringId { get; }
        internal FixedString128Bytes TrackAuthoringId { get; }
        internal ulong PlaybackGeneration { get; }
        internal int PresentationPoseSourceIndex { get; }
        internal AnimationPoseSourceKind SourceKind { get; }
        internal ulong SelectionGeneration { get; }
        internal ulong SourceActionInstanceId { get; }
        internal bool IsValid =>
            SelectionGeneration != 0 &&
            (SourceKind == AnimationPoseSourceKind.Timeline
                ? TimelineAuthoringId.Length > 0 &&
                  TrackAuthoringId.Length > 0 &&
                  PlaybackGeneration != 0 &&
                  PresentationPoseSourceIndex < 0
                : (SourceKind == AnimationPoseSourceKind.Sequence ||
                   SourceKind == AnimationPoseSourceKind.BlendSpace ||
                   SourceKind == AnimationPoseSourceKind.MotionMatching) &&
                  TimelineAuthoringId.Length == 0 &&
                  TrackAuthoringId.Length == 0 &&
                  PlaybackGeneration == 0 &&
                  PresentationPoseSourceIndex >= 0 &&
                  SourceActionInstanceId == 0);

        internal static PoseDiscontinuityNativeEndpoint From(
            PoseDiscontinuityEndpoint endpoint)
        {
            if (!endpoint.IsValid)
                throw new ArgumentException(
                    "Pose Discontinuity Native endpoint is invalid.",
                    nameof(endpoint));
            AnimationPoseSourceId source = endpoint.SourceId;
            AnimationProducerId producer = source.PlaybackId.ProducerId;
            return new PoseDiscontinuityNativeEndpoint(
                producer.IsValid
                    ? new FixedString128Bytes(producer.TimelineAuthoringId)
                    : default,
                producer.IsValid
                    ? new FixedString128Bytes(producer.TrackAuthoringId)
                    : default,
                source.PlaybackId.Generation,
                source.PresentationPoseSourceIndex.IsValid
                    ? source.PresentationPoseSourceIndex.Value
                    : -1,
                source.SourceKind,
                source.SelectionGeneration.Value,
                source.SourceActionInstanceId);
        }

        internal PoseDiscontinuityEndpoint ToManaged()
        {
            if (!IsValid)
                throw new InvalidOperationException(
                    "Pose Discontinuity Native endpoint is invalid.");
            AnimationPoseSourceId source =
                SourceKind == AnimationPoseSourceKind.Timeline
                    ? new AnimationPoseSourceId(
                        new AnimationPlaybackId(
                            new AnimationProducerId(
                                TimelineAuthoringId.ToString(),
                                TrackAuthoringId.ToString()),
                            PlaybackGeneration),
                        SourceKind,
                        new AnimationPoseSelectionGeneration(
                            SelectionGeneration),
                        SourceActionInstanceId)
                    : new AnimationPoseSourceId(
                        new PresentationPoseSourceIndex(
                            PresentationPoseSourceIndex),
                        SourceKind,
                        new AnimationPoseSelectionGeneration(
                            SelectionGeneration));
            return new PoseDiscontinuityEndpoint(source);
        }
    }

    internal readonly struct PoseDiscontinuityNative
    {
        PoseDiscontinuityNative(
            ulong eventIdentity,
            ulong completionIdentity,
            PoseDiscontinuityNativeEndpoint previousEndpoint,
            PoseDiscontinuityNativeEndpoint currentEndpoint,
            ulong previousContinuityIdentity,
            ulong currentContinuityIdentity,
            PoseDiscontinuityReason reason,
            PoseDiscontinuityResetReason resetReason,
            ulong resetSequence,
            byte hasPreviousEndpoint,
            byte hasCurrentEndpoint)
        {
            EventIdentity = eventIdentity;
            CompletionIdentity = completionIdentity;
            PreviousEndpoint = previousEndpoint;
            CurrentEndpoint = currentEndpoint;
            PreviousContinuityIdentity = previousContinuityIdentity;
            CurrentContinuityIdentity = currentContinuityIdentity;
            Reason = reason;
            ResetReason = resetReason;
            ResetSequence = resetSequence;
            HasPreviousEndpoint = hasPreviousEndpoint;
            HasCurrentEndpoint = hasCurrentEndpoint;
        }

        internal ulong EventIdentity { get; }
        internal ulong CompletionIdentity { get; }
        internal PoseDiscontinuityNativeEndpoint PreviousEndpoint { get; }
        internal PoseDiscontinuityNativeEndpoint CurrentEndpoint { get; }
        internal ulong PreviousContinuityIdentity { get; }
        internal ulong CurrentContinuityIdentity { get; }
        internal PoseDiscontinuityReason Reason { get; }
        internal PoseDiscontinuityResetReason ResetReason { get; }
        internal ulong ResetSequence { get; }
        internal byte HasPreviousEndpoint { get; }
        internal byte HasCurrentEndpoint { get; }
        internal bool IsPresent => EventIdentity != 0;
        internal bool IsReset =>
            IsPresent && Reason == PoseDiscontinuityReason.Reset;
        internal bool IsValid =>
            !IsPresent ||
            CompletionIdentity != 0 &&
            Reason >= PoseDiscontinuityReason.SourceIdentityChanged &&
            Reason <= PoseDiscontinuityReason.Reset &&
            HasPreviousEndpoint <= 1 &&
            HasCurrentEndpoint <= 1 &&
            (HasPreviousEndpoint == 0 || PreviousEndpoint.IsValid) &&
            (HasCurrentEndpoint == 0 || CurrentEndpoint.IsValid) &&
            (IsReset
                ? ResetReason != PoseDiscontinuityResetReason.None &&
                  ResetSequence != 0
                : ResetReason == PoseDiscontinuityResetReason.None &&
                  ResetSequence == 0 &&
                  HasPreviousEndpoint == 1 &&
                  HasCurrentEndpoint == 1 &&
                  PreviousContinuityIdentity != 0 &&
                  CurrentContinuityIdentity != 0);

        internal static PoseDiscontinuityNative From(
            in PoseDiscontinuity value)
        {
            if (!value.IsValid)
                throw new ArgumentException(
                    "Pose Discontinuity Native value is invalid.",
                    nameof(value));
            if (!value.IsPresent)
                return default;
            return new PoseDiscontinuityNative(
                value.EventIdentity,
                value.CompletionIdentity,
                value.HasPreviousEndpoint != 0
                    ? PoseDiscontinuityNativeEndpoint.From(
                        value.PreviousEndpoint)
                    : default,
                value.HasCurrentEndpoint != 0
                    ? PoseDiscontinuityNativeEndpoint.From(
                        value.CurrentEndpoint)
                    : default,
                value.PreviousContinuityIdentity,
                value.CurrentContinuityIdentity,
                value.Reason,
                value.ResetReason,
                value.ResetSequence,
                value.HasPreviousEndpoint,
                value.HasCurrentEndpoint);
        }
    }

    internal readonly struct AnimationPrimitivePoseContribution
    {
        internal AnimationPrimitivePoseContribution(
            int physicalSlotIndex,
            int physicalSourceIndex,
            ulong physicalSourceGeneration,
            AnimationPoseContributionKind kind,
            int sourceOwnerIndex,
            ulong contributionContinuityIdentity,
            float weight,
            float leftFootWeight,
            float rightFootWeight)
        {
            int kindValue = (int)kind;
            if (physicalSlotIndex < 0 ||
                kindValue < (int)AnimationPoseContributionKind.Live ||
                kindValue > (int)AnimationPoseContributionKind.Stored ||
                kind == AnimationPoseContributionKind.Live &&
                (physicalSourceIndex < 0 || physicalSourceGeneration == 0 || sourceOwnerIndex < 0) ||
                kind != AnimationPoseContributionKind.Live &&
                (physicalSourceIndex != -1 || physicalSourceGeneration != 0 || sourceOwnerIndex != -1) ||
                contributionContinuityIdentity == 0 ||
                !float.IsFinite(weight) || weight < 0f || weight > 1f ||
                !float.IsFinite(leftFootWeight) || leftFootWeight < 0f || leftFootWeight > 1f ||
                !float.IsFinite(rightFootWeight) || rightFootWeight < 0f || rightFootWeight > 1f)
            {
                throw new ArgumentException("Primitive animation pose contribution is invalid.");
            }

            PhysicalPlayerIndex = physicalSlotIndex;
            PhysicalSourceIndex = physicalSourceIndex;
            PhysicalSourceGeneration = physicalSourceGeneration;
            Kind = kind;
            SourceOwnerIndex = sourceOwnerIndex;
            ContributionContinuityIdentity = contributionContinuityIdentity;
            Weight = weight;
            LeftFootWeight = leftFootWeight;
            RightFootWeight = rightFootWeight;
        }

        internal int PhysicalPlayerIndex { get; }
        internal int PhysicalSourceIndex { get; }
        internal ulong PhysicalSourceGeneration { get; }
        internal AnimationPoseContributionKind Kind { get; }
        internal int SourceOwnerIndex { get; }
        internal ulong ContributionContinuityIdentity { get; }
        internal float Weight { get; }
        internal float LeftFootWeight { get; }
        internal float RightFootWeight { get; }
    }

    internal readonly struct AnimationPlayerPoseNativeRange
    {
        internal AnimationPlayerPoseNativeRange(
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

            PhysicalPlayerIndex = physicalSlotIndex;
            PoseOffset = poseOffset;
            VelocityOffset = velocityOffset;
            ParameterOffset = parameterOffset;
            ContributionOffset = contributionOffset;
            ContributionCapacity = contributionCapacity;
            DenseContributionWeightOffset = denseContributionWeightOffset;
        }

        internal int PhysicalPlayerIndex { get; }
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
            int stageCount,
            int outputPoseValueIndex,
            NativeArray<AnimationPlayerPoseNativeRange> slotRanges)
        {
            if (slotCount <= 0 || boneCount <= 0 || parameterCount <= 0 || totalSlotContributionCapacity <= 0 ||
                poseValueCount <= 0 || poseValueContributionStride <= 0 || operationCount <= 0 || frameCacheCount <= 0 ||
                stageCount <= 0 ||
                outputPoseValueIndex < 0 || outputPoseValueIndex >= poseValueCount)
            {
                throw new ArgumentOutOfRangeException(nameof(slotCount));
            }

            PlayerCount = slotCount;
            BoneCount = boneCount;
            ParameterCount = parameterCount;
            TotalPlayerContributionCapacity = totalSlotContributionCapacity;
            PoseValueCount = poseValueCount;
            PoseValueContributionStride = poseValueContributionStride;
            OperationCount = operationCount;
            FrameCacheCount = frameCacheCount;
            StageCount = stageCount;
            OutputValueIndex = outputPoseValueIndex;
            PlayerPoseCapacity = checked(slotCount * boneCount);
            PlayerVelocityCapacity = checked(slotCount * boneCount);
            PlayerParameterCapacity = checked(slotCount * parameterCount);
            PlayerDenseContributionWeightCapacity = checked(totalSlotContributionCapacity * boneCount);
            PoseValuePoseCapacity = checked(poseValueCount * boneCount);
            PoseValueParameterCapacity = checked(poseValueCount * parameterCount);
            PoseValueContributionCapacity = checked(poseValueCount * poseValueContributionStride);
            PoseValueDenseContributionWeightCapacity = checked(PoseValueContributionCapacity * boneCount);
            RequireSlotRanges(slotRanges);
        }

        internal int PlayerCount { get; }
        internal int BoneCount { get; }
        internal int ParameterCount { get; }
        internal int TotalPlayerContributionCapacity { get; }
        internal int PoseValueCount { get; }
        internal int PoseValueContributionStride { get; }
        internal int OperationCount { get; }
        internal int FrameCacheCount { get; }
        internal int StageCount { get; }
        internal int OutputValueIndex { get; }
        internal int PlayerPoseCapacity { get; }
        internal int PlayerVelocityCapacity { get; }
        internal int PlayerParameterCapacity { get; }
        internal int PlayerDenseContributionWeightCapacity { get; }
        internal int PoseValuePoseCapacity { get; }
        internal int PoseValueParameterCapacity { get; }
        internal int PoseValueContributionCapacity { get; }
        internal int PoseValueDenseContributionWeightCapacity { get; }

        internal void RequireValid()
        {
            if (PlayerCount <= 0 || BoneCount <= 0 || ParameterCount <= 0 || TotalPlayerContributionCapacity <= 0 ||
                PoseValueCount <= 0 || PoseValueContributionStride <= 0 || OperationCount <= 0 || FrameCacheCount <= 0 ||
                StageCount <= 0 ||
                OutputValueIndex < 0 || OutputValueIndex >= PoseValueCount ||
                checked(PlayerCount * BoneCount) != PlayerPoseCapacity ||
                checked(PlayerCount * BoneCount) != PlayerVelocityCapacity ||
                checked(PlayerCount * ParameterCount) != PlayerParameterCapacity ||
                checked(TotalPlayerContributionCapacity * BoneCount) != PlayerDenseContributionWeightCapacity ||
                checked(PoseValueCount * BoneCount) != PoseValuePoseCapacity ||
                checked(PoseValueCount * ParameterCount) != PoseValueParameterCapacity ||
                checked(PoseValueCount * PoseValueContributionStride) != PoseValueContributionCapacity ||
                checked(PoseValueContributionCapacity * BoneCount) != PoseValueDenseContributionWeightCapacity)
            {
                throw new InvalidOperationException("Animation pose Native aggregate layout is invalid.");
            }
        }

        internal void RequireSlotRanges(NativeArray<AnimationPlayerPoseNativeRange> slotRanges)
        {
            RequireValid();
            if (!slotRanges.IsCreated || slotRanges.Length != PlayerCount)
                throw new ArgumentException("Animation pose Slot ranges are invalid.", nameof(slotRanges));

            int contributionOffset = 0;
            for (int i = 0; i < slotRanges.Length; i++)
            {
                AnimationPlayerPoseNativeRange range = slotRanges[i];
                int poseOffset = checked(i * BoneCount);
                int parameterOffset = checked(i * ParameterCount);
                int denseContributionWeightOffset = checked(contributionOffset * BoneCount);
                if (range.PhysicalPlayerIndex != i || range.PoseOffset != poseOffset || range.VelocityOffset != poseOffset ||
                    range.ParameterOffset != parameterOffset || range.ContributionOffset != contributionOffset ||
                    range.ContributionCapacity <= 0 ||
                    range.DenseContributionWeightOffset != denseContributionWeightOffset)
                {
                    throw new ArgumentException($"Animation pose Slot range #{i} is not compact or stable.", nameof(slotRanges));
                }

                contributionOffset = checked(contributionOffset + range.ContributionCapacity);
            }

            if (contributionOffset != TotalPlayerContributionCapacity ||
                checked(contributionOffset * BoneCount) != PlayerDenseContributionWeightCapacity)
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
            NativeArray<AnimationPlayerPoseNativeRange> slotRanges,
            NativeArray<AnimationLocalBonePose> slotDenseLocalPoses,
            NativeArray<AnimationBlendBoneVelocity> slotDenseVelocities,
            NativeArray<float> slotPoseParameters,
            NativeArray<byte> slotPoseParameterAvailability,
            NativeArray<AnimationPrimitivePoseContribution> slotContributions,
            NativeArray<float> slotDenseContributionWeights,
            NativeArray<int> slotContributionCounts,
            NativeArray<float> slotOutputWeights,
            NativeArray<AnimationFootFeatureSample> slotLeftFootFeatures,
            NativeArray<AnimationFootFeatureSample> slotRightFootFeatures,
            NativeArray<byte> slotHasFootFeatures,
            NativeArray<AnimationPoseAvailability> slotAvailability,
            NativeArray<ulong> slotContinuityIdentities,
            NativeArray<PoseDiscontinuityNative> slotDiscontinuities,
            NativeArray<AnimationPoseNativeInvalidReason> slotInvalidReasons,
            NativeArray<ulong> slotCompletedAt,
            NativeArray<AnimationLocalBonePose> valueDenseLocalPoses,
            NativeArray<AnimationBlendBoneVelocity> valueDenseVelocities,
            NativeArray<float> valuePoseParameters,
            NativeArray<byte> valuePoseParameterAvailability,
            NativeArray<AnimationPrimitivePoseContribution> valueContributions,
            NativeArray<float> valueDenseContributionWeights,
            NativeArray<int> valueContributionCounts,
            NativeArray<float> valueOutputWeights,
            NativeArray<AnimationFootFeatureSample> valueLeftFootFeatures,
            NativeArray<AnimationFootFeatureSample> valueRightFootFeatures,
            NativeArray<byte> valueHasFootFeatures,
            NativeArray<AnimationPoseAvailability> valueAvailability,
            NativeArray<ulong> valueContinuityIdentities,
            NativeArray<PoseDiscontinuityNative> valueDiscontinuities,
            NativeArray<AnimationPoseNativeInvalidReason> valueInvalidReasons,
            NativeArray<ulong> frameCacheCompletedAt,
            NativeArray<ulong> stageCompletedAt,
            NativeArray<int> stageInvalidOperationIndex,
            NativeArray<AnimationPoseNativeInvalidReason> poseGraphInvalidReason,
            NativeArray<int> poseGraphInvalidOperationIndex,
            NativeArray<ulong> poseGraphCompletedAt,
            NativeArray<ulong> finalAppliedAt,
            NativeArray<AnimationFinalPoseWriteOutcome> finalWriteOutcome)
        {
            if (completionIdentity == 0)
                throw new ArgumentOutOfRangeException(nameof(completionIdentity));

            Layout = layout;
            CompletionIdentity = completionIdentity;
            SlotRanges = slotRanges;
            SlotDenseLocalPoses = slotDenseLocalPoses;
            SlotDenseVelocities = slotDenseVelocities;
            SlotPoseParameters = slotPoseParameters;
            SlotPoseParameterAvailability = slotPoseParameterAvailability;
            SlotContributions = slotContributions;
            SlotDenseContributionWeights = slotDenseContributionWeights;
            SlotContributionCounts = slotContributionCounts;
            SlotOutputWeights = slotOutputWeights;
            SlotLeftFootFeatures = slotLeftFootFeatures;
            SlotRightFootFeatures = slotRightFootFeatures;
            SlotHasFootFeatures = slotHasFootFeatures;
            SlotAvailability = slotAvailability;
            SlotContinuityIdentities = slotContinuityIdentities;
            SlotDiscontinuities = slotDiscontinuities;
            SlotInvalidReasons = slotInvalidReasons;
            SlotCompletedAt = slotCompletedAt;
            ValueDenseLocalPoses = valueDenseLocalPoses;
            ValueDenseVelocities = valueDenseVelocities;
            ValuePoseParameters = valuePoseParameters;
            ValuePoseParameterAvailability = valuePoseParameterAvailability;
            ValueContributions = valueContributions;
            ValueDenseContributionWeights = valueDenseContributionWeights;
            ValueContributionCounts = valueContributionCounts;
            ValueOutputWeights = valueOutputWeights;
            ValueLeftFootFeatures = valueLeftFootFeatures;
            ValueRightFootFeatures = valueRightFootFeatures;
            ValueHasFootFeatures = valueHasFootFeatures;
            ValueAvailability = valueAvailability;
            ValueContinuityIdentities = valueContinuityIdentities;
            ValueDiscontinuities = valueDiscontinuities;
            ValueInvalidReasons = valueInvalidReasons;
            FrameCacheCompletedAt = frameCacheCompletedAt;
            StageCompletedAt = stageCompletedAt;
            StageInvalidOperationIndex = stageInvalidOperationIndex;
            PoseGraphInvalidReason = poseGraphInvalidReason;
            PoseGraphInvalidOperationIndex = poseGraphInvalidOperationIndex;
            PoseGraphCompletedAt = poseGraphCompletedAt;
            FinalAppliedAt = finalAppliedAt;
            FinalWriteOutcome = finalWriteOutcome;
            RequireValid();
        }

        internal AnimationPoseNativeAggregateLayout Layout { get; }
        internal ulong CompletionIdentity { get; }
        internal NativeArray<AnimationPlayerPoseNativeRange> SlotRanges { get; }
        internal NativeArray<AnimationLocalBonePose> SlotDenseLocalPoses { get; }
        internal NativeArray<AnimationBlendBoneVelocity> SlotDenseVelocities { get; }
        internal NativeArray<float> SlotPoseParameters { get; }
        internal NativeArray<byte> SlotPoseParameterAvailability { get; }
        internal NativeArray<AnimationPrimitivePoseContribution> SlotContributions { get; }
        internal NativeArray<float> SlotDenseContributionWeights { get; }
        internal NativeArray<int> SlotContributionCounts { get; }
        internal NativeArray<float> SlotOutputWeights { get; }
        internal NativeArray<AnimationFootFeatureSample> SlotLeftFootFeatures { get; }
        internal NativeArray<AnimationFootFeatureSample> SlotRightFootFeatures { get; }
        internal NativeArray<byte> SlotHasFootFeatures { get; }
        internal NativeArray<AnimationPoseAvailability> SlotAvailability { get; }
        internal NativeArray<ulong> SlotContinuityIdentities { get; }
        internal NativeArray<PoseDiscontinuityNative> SlotDiscontinuities { get; }
        internal NativeArray<AnimationPoseNativeInvalidReason> SlotInvalidReasons { get; }
        internal NativeArray<ulong> SlotCompletedAt { get; }
        internal NativeArray<AnimationLocalBonePose> ValueDenseLocalPoses { get; }
        internal NativeArray<AnimationBlendBoneVelocity> ValueDenseVelocities { get; }
        internal NativeArray<float> ValuePoseParameters { get; }
        internal NativeArray<byte> ValuePoseParameterAvailability { get; }
        internal NativeArray<AnimationPrimitivePoseContribution> ValueContributions { get; }
        internal NativeArray<float> ValueDenseContributionWeights { get; }
        internal NativeArray<int> ValueContributionCounts { get; }
        internal NativeArray<float> ValueOutputWeights { get; }
        internal NativeArray<AnimationFootFeatureSample> ValueLeftFootFeatures { get; }
        internal NativeArray<AnimationFootFeatureSample> ValueRightFootFeatures { get; }
        internal NativeArray<byte> ValueHasFootFeatures { get; }
        internal NativeArray<AnimationPoseAvailability> ValueAvailability { get; }
        internal NativeArray<ulong> ValueContinuityIdentities { get; }
        internal NativeArray<PoseDiscontinuityNative> ValueDiscontinuities { get; }
        internal NativeArray<AnimationPoseNativeInvalidReason> ValueInvalidReasons { get; }
        internal NativeArray<ulong> FrameCacheCompletedAt { get; }
        internal NativeArray<ulong> StageCompletedAt { get; }
        internal NativeArray<int> StageInvalidOperationIndex { get; }
        internal NativeArray<AnimationPoseNativeInvalidReason> PoseGraphInvalidReason { get; }
        internal NativeArray<int> PoseGraphInvalidOperationIndex { get; }
        internal NativeArray<ulong> PoseGraphCompletedAt { get; }
        internal NativeArray<ulong> FinalAppliedAt { get; }
        internal NativeArray<AnimationFinalPoseWriteOutcome> FinalWriteOutcome { get; }

        internal void RequireValid()
        {
            Layout.RequireSlotRanges(SlotRanges);
            if (CompletionIdentity == 0)
                throw new InvalidOperationException("Animation Pose Graph Native completion identity is invalid.");

            RequireLength(SlotDenseLocalPoses, Layout.PlayerPoseCapacity);
            RequireLength(SlotDenseVelocities, Layout.PlayerVelocityCapacity);
            RequireLength(SlotPoseParameters, Layout.PlayerParameterCapacity);
            RequireLength(SlotPoseParameterAvailability, Layout.PlayerParameterCapacity);
            RequireLength(SlotContributions, Layout.TotalPlayerContributionCapacity);
            RequireLength(SlotDenseContributionWeights, Layout.PlayerDenseContributionWeightCapacity);
            RequireLength(SlotContributionCounts, Layout.PlayerCount);
            RequireLength(SlotOutputWeights, Layout.PlayerCount);
            RequireLength(SlotLeftFootFeatures, Layout.PlayerCount);
            RequireLength(SlotRightFootFeatures, Layout.PlayerCount);
            RequireLength(SlotHasFootFeatures, Layout.PlayerCount);
            RequireLength(SlotAvailability, Layout.PlayerCount);
            RequireLength(SlotContinuityIdentities, Layout.PlayerCount);
            RequireLength(SlotDiscontinuities, Layout.PlayerCount);
            RequireLength(SlotInvalidReasons, Layout.PlayerCount);
            RequireLength(SlotCompletedAt, Layout.PlayerCount);
            RequireLength(ValueDenseLocalPoses, Layout.PoseValuePoseCapacity);
            RequireLength(ValueDenseVelocities, Layout.PoseValuePoseCapacity);
            RequireLength(ValuePoseParameters, Layout.PoseValueParameterCapacity);
            RequireLength(ValuePoseParameterAvailability, Layout.PoseValueParameterCapacity);
            RequireLength(ValueContributions, Layout.PoseValueContributionCapacity);
            RequireLength(ValueDenseContributionWeights, Layout.PoseValueDenseContributionWeightCapacity);
            RequireLength(ValueContributionCounts, Layout.PoseValueCount);
            RequireLength(ValueOutputWeights, Layout.PoseValueCount);
            RequireLength(ValueLeftFootFeatures, Layout.PoseValueCount);
            RequireLength(ValueRightFootFeatures, Layout.PoseValueCount);
            RequireLength(ValueHasFootFeatures, Layout.PoseValueCount);
            RequireLength(ValueAvailability, Layout.PoseValueCount);
            RequireLength(ValueContinuityIdentities, Layout.PoseValueCount);
            RequireLength(ValueDiscontinuities, Layout.PoseValueCount);
            RequireLength(ValueInvalidReasons, Layout.PoseValueCount);
            RequireLength(FrameCacheCompletedAt, Layout.FrameCacheCount);
            RequireLength(StageCompletedAt, Layout.StageCount);
            RequireLength(StageInvalidOperationIndex, Layout.StageCount);
            RequireLength(PoseGraphInvalidReason, 1);
            RequireLength(PoseGraphInvalidOperationIndex, 1);
            RequireLength(PoseGraphCompletedAt, 1);
            RequireLength(FinalAppliedAt, 1);
            RequireLength(FinalWriteOutcome, 1);
        }

        static void RequireLength<T>(NativeArray<T> values, int expectedLength) where T : struct
        {
            if (!values.IsCreated || values.Length != expectedLength)
                throw new ArgumentException("Animation Pose Graph Native container length is invalid.");
        }
    }

    internal readonly struct AnimationPlayerPoseNativeWriteBinding
    {
        internal AnimationPlayerPoseNativeWriteBinding(
            in CharacterPoseGraphNativeBinding aggregate,
            int physicalSlotIndex)
        {
            aggregate.RequireValid();
            if (physicalSlotIndex < 0 || physicalSlotIndex >= aggregate.Layout.PlayerCount)
                throw new ArgumentOutOfRangeException(nameof(physicalSlotIndex));

            AnimationPlayerPoseNativeRange range = aggregate.SlotRanges[physicalSlotIndex];
            if (range.PhysicalPlayerIndex != physicalSlotIndex)
                throw new ArgumentException("Animation pose Slot Native range identity is invalid.", nameof(physicalSlotIndex));

            Range = range;
            CompletionIdentity = aggregate.CompletionIdentity;
            DenseLocalPoses = new NativeSlice<AnimationLocalBonePose>(aggregate.SlotDenseLocalPoses, range.PoseOffset, aggregate.Layout.BoneCount);
            DenseVelocities = new NativeSlice<AnimationBlendBoneVelocity>(aggregate.SlotDenseVelocities, range.VelocityOffset, aggregate.Layout.BoneCount);
            PoseParameters = new NativeSlice<float>(aggregate.SlotPoseParameters, range.ParameterOffset, aggregate.Layout.ParameterCount);
            PoseParameterAvailability = new NativeSlice<byte>(aggregate.SlotPoseParameterAvailability, range.ParameterOffset, aggregate.Layout.ParameterCount);
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
            Availability = new NativeSlice<AnimationPoseAvailability>(aggregate.SlotAvailability, physicalSlotIndex, 1);
            ContinuityIdentity = new NativeSlice<ulong>(aggregate.SlotContinuityIdentities, physicalSlotIndex, 1);
            Discontinuity = new NativeSlice<PoseDiscontinuityNative>(aggregate.SlotDiscontinuities, physicalSlotIndex, 1);
            InvalidReason = new NativeSlice<AnimationPoseNativeInvalidReason>(aggregate.SlotInvalidReasons, physicalSlotIndex, 1);
            CompletedAt = new NativeSlice<ulong>(aggregate.SlotCompletedAt, physicalSlotIndex, 1);
        }

        internal AnimationPlayerPoseNativeRange Range { get; }
        internal ulong CompletionIdentity { get; }
        internal NativeSlice<AnimationLocalBonePose> DenseLocalPoses { get; }
        internal NativeSlice<AnimationBlendBoneVelocity> DenseVelocities { get; }
        internal NativeSlice<float> PoseParameters { get; }
        internal NativeSlice<byte> PoseParameterAvailability { get; }
        internal NativeSlice<AnimationPrimitivePoseContribution> Contributions { get; }
        internal NativeSlice<float> DenseContributionWeights { get; }
        internal NativeSlice<int> ContributionCount { get; }
        internal NativeSlice<float> OutputWeight { get; }
        internal NativeSlice<AnimationFootFeatureSample> LeftFootFeatures { get; }
        internal NativeSlice<AnimationFootFeatureSample> RightFootFeatures { get; }
        internal NativeSlice<byte> HasFootFeatures { get; }
        internal NativeSlice<AnimationPoseAvailability> Availability { get; }
        internal NativeSlice<ulong> ContinuityIdentity { get; }
        internal NativeSlice<PoseDiscontinuityNative> Discontinuity { get; }
        internal NativeSlice<AnimationPoseNativeInvalidReason> InvalidReason { get; }
        internal NativeSlice<ulong> CompletedAt { get; }
    }

    internal readonly struct AnimationPoseValueNativeReadBinding
    {
        internal AnimationPoseValueNativeReadBinding(
            in CharacterPoseGraphNativeBinding aggregate,
            int valueIndex)
        {
            aggregate.RequireValid();
            if ((uint)valueIndex >= (uint)aggregate.Layout.PoseValueCount)
                throw new ArgumentOutOfRangeException(nameof(valueIndex));
            int poseOffset = checked(valueIndex * aggregate.Layout.BoneCount);
            int parameterOffset = checked(valueIndex * aggregate.Layout.ParameterCount);
            int contributionOffset = checked(valueIndex * aggregate.Layout.PoseValueContributionStride);
            int denseContributionWeightOffset = checked(contributionOffset * aggregate.Layout.BoneCount);

            CompletionIdentity = aggregate.CompletionIdentity;
            ValueIndex = valueIndex;
            DensePoses = new NativeSlice<AnimationLocalBonePose>(
                aggregate.ValueDenseLocalPoses,
                poseOffset,
                aggregate.Layout.BoneCount);
            PoseParameters = new NativeSlice<float>(
                aggregate.ValuePoseParameters,
                parameterOffset,
                aggregate.Layout.ParameterCount);
            PoseParameterAvailability = new NativeSlice<byte>(
                aggregate.ValuePoseParameterAvailability,
                parameterOffset,
                aggregate.Layout.ParameterCount);
            Contributions = new NativeSlice<AnimationPrimitivePoseContribution>(
                aggregate.ValueContributions,
                contributionOffset,
                aggregate.Layout.PoseValueContributionStride);
            DenseContributionWeights = new NativeSlice<float>(
                aggregate.ValueDenseContributionWeights,
                denseContributionWeightOffset,
                checked(aggregate.Layout.PoseValueContributionStride * aggregate.Layout.BoneCount));
            ContributionCount = new NativeSlice<int>(
                aggregate.ValueContributionCounts,
                valueIndex,
                1);
            OutputWeight = new NativeSlice<float>(
                aggregate.ValueOutputWeights,
                valueIndex,
                1);
            LeftFootFeatures = new NativeSlice<AnimationFootFeatureSample>(
                aggregate.ValueLeftFootFeatures,
                valueIndex,
                1);
            RightFootFeatures = new NativeSlice<AnimationFootFeatureSample>(
                aggregate.ValueRightFootFeatures,
                valueIndex,
                1);
            HasFootFeatures = new NativeSlice<byte>(
                aggregate.ValueHasFootFeatures,
                valueIndex,
                1);
            Availability = new NativeSlice<AnimationPoseAvailability>(
                aggregate.ValueAvailability,
                valueIndex,
                1);
            ContinuityIdentity = new NativeSlice<ulong>(
                aggregate.ValueContinuityIdentities,
                valueIndex,
                1);
            InvalidReason = new NativeSlice<AnimationPoseNativeInvalidReason>(
                aggregate.ValueInvalidReasons,
                valueIndex,
                1);
            PoseGraphInvalidOperationIndex = new NativeSlice<int>(
                aggregate.PoseGraphInvalidOperationIndex,
                0,
                1);
        }

        internal ulong CompletionIdentity { get; }
        internal int ValueIndex { get; }
        internal NativeSlice<AnimationLocalBonePose> DensePoses { get; }
        internal NativeSlice<float> PoseParameters { get; }
        internal NativeSlice<byte> PoseParameterAvailability { get; }
        internal NativeSlice<AnimationPrimitivePoseContribution> Contributions { get; }
        internal NativeSlice<float> DenseContributionWeights { get; }
        internal NativeSlice<int> ContributionCount { get; }
        internal NativeSlice<float> OutputWeight { get; }
        internal NativeSlice<AnimationFootFeatureSample> LeftFootFeatures { get; }
        internal NativeSlice<AnimationFootFeatureSample> RightFootFeatures { get; }
        internal NativeSlice<byte> HasFootFeatures { get; }
        internal NativeSlice<AnimationPoseAvailability> Availability { get; }
        internal NativeSlice<ulong> ContinuityIdentity { get; }
        internal NativeSlice<AnimationPoseNativeInvalidReason> InvalidReason { get; }
        internal NativeSlice<int> PoseGraphInvalidOperationIndex { get; }
    }

    internal readonly struct AnimationFinalPoseNativeReadBinding
    {
        internal AnimationFinalPoseNativeReadBinding(in CharacterPoseGraphNativeBinding aggregate)
        {
            aggregate.RequireValid();
            int valueIndex = aggregate.Layout.OutputValueIndex;
            int poseOffset = checked(valueIndex * aggregate.Layout.BoneCount);
            int parameterOffset = checked(valueIndex * aggregate.Layout.ParameterCount);
            int contributionOffset = checked(valueIndex * aggregate.Layout.PoseValueContributionStride);
            int denseContributionWeightOffset = checked(contributionOffset * aggregate.Layout.BoneCount);

            CompletionIdentity = aggregate.CompletionIdentity;
            OutputValueIndex = valueIndex;
            DenseLocalPoses = new NativeSlice<AnimationLocalBonePose>(aggregate.ValueDenseLocalPoses, poseOffset, aggregate.Layout.BoneCount);
            PoseParameters = new NativeSlice<float>(aggregate.ValuePoseParameters, parameterOffset, aggregate.Layout.ParameterCount);
            PoseParameterAvailability = new NativeSlice<byte>(aggregate.ValuePoseParameterAvailability, parameterOffset, aggregate.Layout.ParameterCount);
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
            Availability = new NativeSlice<AnimationPoseAvailability>(aggregate.ValueAvailability, valueIndex, 1);
            ContinuityIdentity = new NativeSlice<ulong>(aggregate.ValueContinuityIdentities, valueIndex, 1);
            OutputInvalidReason = new NativeSlice<AnimationPoseNativeInvalidReason>(aggregate.ValueInvalidReasons, valueIndex, 1);
            PoseGraphInvalidReason = new NativeSlice<AnimationPoseNativeInvalidReason>(aggregate.PoseGraphInvalidReason, 0, 1);
            PoseGraphInvalidOperationIndex = new NativeSlice<int>(aggregate.PoseGraphInvalidOperationIndex, 0, 1);
            PoseGraphCompletedAt = new NativeSlice<ulong>(aggregate.PoseGraphCompletedAt, 0, 1);
            AppliedAt = new NativeSlice<ulong>(aggregate.FinalAppliedAt, 0, 1);
            WriteOutcome = new NativeSlice<AnimationFinalPoseWriteOutcome>(aggregate.FinalWriteOutcome, 0, 1);
        }

        internal ulong CompletionIdentity { get; }
        internal int OutputValueIndex { get; }
        internal NativeSlice<AnimationLocalBonePose> DenseLocalPoses { get; }
        internal NativeSlice<float> PoseParameters { get; }
        internal NativeSlice<byte> PoseParameterAvailability { get; }
        internal NativeSlice<AnimationPrimitivePoseContribution> Contributions { get; }
        internal NativeSlice<float> DenseContributionWeights { get; }
        internal NativeSlice<int> ContributionCount { get; }
        internal NativeSlice<float> OutputWeight { get; }
        internal NativeSlice<AnimationFootFeatureSample> LeftFootFeatures { get; }
        internal NativeSlice<AnimationFootFeatureSample> RightFootFeatures { get; }
        internal NativeSlice<byte> HasFootFeatures { get; }
        internal NativeSlice<AnimationPoseAvailability> Availability { get; }
        internal NativeSlice<ulong> ContinuityIdentity { get; }
        internal NativeSlice<AnimationPoseNativeInvalidReason> OutputInvalidReason { get; }
        internal NativeSlice<AnimationPoseNativeInvalidReason> PoseGraphInvalidReason { get; }
        internal NativeSlice<int> PoseGraphInvalidOperationIndex { get; }
        internal NativeSlice<ulong> PoseGraphCompletedAt { get; }
        internal NativeSlice<ulong> AppliedAt { get; }
        internal NativeSlice<AnimationFinalPoseWriteOutcome> WriteOutcome { get; }
    }
}
