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
        FootPlacementInvalid = 17,
        PoseSpaceConversionInvalid = 18,
        WorldContextUnavailable = 19,
        FullBodyIkGoalSetInvalid = 20,
        FullBodyIkSolverInvalid = 21
    }

    internal static class AnimationPoseNativeInvalidReasonContract
    {
        internal static bool IsDefined(AnimationPoseNativeInvalidReason value) =>
            (byte)value >= (byte)AnimationPoseNativeInvalidReason.None &&
            (byte)value <= (byte)AnimationPoseNativeInvalidReason.FullBodyIkSolverInvalid;

        internal static AnimationPoseNativeInvalidReason NormalizeFailure(
            AnimationPoseNativeInvalidReason value) =>
            value != AnimationPoseNativeInvalidReason.None && IsDefined(value)
                ? value
                : AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid;
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
                : (SourceKind == AnimationPoseSourceKind.Clip ||
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
            float rightFootWeight,
            in AnimationFootMotionSourceSample footMotion)
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
            FootMotion = footMotion;
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
        internal readonly AnimationFootMotionSourceSample FootMotion;
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


    internal struct AnimationPlayerPoseNativeWriteBinding
    {
        internal AnimationPlayerPoseNativeWriteBinding(
            int physicalPlayerIndex,
            int boneCount,
            int parameterCount,
            int contributionCapacity,
            ulong completionIdentity,
            NativeSlice<AnimationLocalBonePose> denseLocalPoses,
            NativeSlice<AnimationBlendBoneVelocity> denseVelocities,
            NativeSlice<float> poseParameters,
            NativeSlice<byte> poseParameterAvailability,
            NativeSlice<AnimationPrimitivePoseContribution> contributions,
            NativeSlice<float> denseContributionWeights,
            NativeSlice<int> contributionCount,
            NativeSlice<float> outputWeight,
            NativeSlice<AnimationFootFeatureSample> leftFootFeatures,
            NativeSlice<AnimationFootFeatureSample> rightFootFeatures,
            NativeSlice<byte> hasFootFeatures,
            NativeSlice<AnimationPoseAvailability> availability,
            NativeSlice<ulong> continuityIdentity,
            NativeSlice<PoseDiscontinuityNative> discontinuity,
            NativeSlice<AnimationPoseNativeInvalidReason> invalidReason,
            NativeSlice<ulong> completedAt)
        {
            if (physicalPlayerIndex < 0 || boneCount <= 0 || parameterCount <= 0 ||
                contributionCapacity <= 0 || completionIdentity == 0 ||
                denseLocalPoses.Length != boneCount ||
                denseVelocities.Length != boneCount ||
                poseParameters.Length != parameterCount ||
                poseParameterAvailability.Length != parameterCount ||
                contributions.Length != contributionCapacity ||
                denseContributionWeights.Length !=
                    checked(contributionCapacity * boneCount) ||
                contributionCount.Length != 1 || outputWeight.Length != 1 ||
                leftFootFeatures.Length != 1 || rightFootFeatures.Length != 1 ||
                hasFootFeatures.Length != 1 || availability.Length != 1 ||
                continuityIdentity.Length != 1 || discontinuity.Length != 1 ||
                invalidReason.Length != 1 || completedAt.Length != 1)
            {
                throw new ArgumentException(
                    "Animation pose native node write binding is invalid.");
            }
            Range = new AnimationPlayerPoseNativeRange(
                physicalPlayerIndex,
                0,
                0,
                0,
                0,
                contributionCapacity,
                0);
            CompletionIdentity = completionIdentity;
            DenseLocalPoses = denseLocalPoses;
            DenseVelocities = denseVelocities;
            PoseParameters = poseParameters;
            PoseParameterAvailability = poseParameterAvailability;
            Contributions = contributions;
            DenseContributionWeights = denseContributionWeights;
            ContributionCount = contributionCount;
            OutputWeight = outputWeight;
            LeftFootFeatures = leftFootFeatures;
            RightFootFeatures = rightFootFeatures;
            HasFootFeatures = hasFootFeatures;
            Availability = availability;
            ContinuityIdentity = continuityIdentity;
            Discontinuity = discontinuity;
            InvalidReason = invalidReason;
            CompletedAt = completedAt;
        }


        internal AnimationPlayerPoseNativeRange Range;
        internal ulong CompletionIdentity;
        internal NativeSlice<AnimationLocalBonePose> DenseLocalPoses;
        internal NativeSlice<AnimationBlendBoneVelocity> DenseVelocities;
        internal NativeSlice<float> PoseParameters;
        internal NativeSlice<byte> PoseParameterAvailability;
        internal NativeSlice<AnimationPrimitivePoseContribution> Contributions;
        internal NativeSlice<float> DenseContributionWeights;
        internal NativeSlice<int> ContributionCount;
        internal NativeSlice<float> OutputWeight;
        internal NativeSlice<AnimationFootFeatureSample> LeftFootFeatures;
        internal NativeSlice<AnimationFootFeatureSample> RightFootFeatures;
        internal NativeSlice<byte> HasFootFeatures;
        internal NativeSlice<AnimationPoseAvailability> Availability;
        internal NativeSlice<ulong> ContinuityIdentity;
        internal NativeSlice<PoseDiscontinuityNative> Discontinuity;
        internal NativeSlice<AnimationPoseNativeInvalidReason> InvalidReason;
        internal NativeSlice<ulong> CompletedAt;
    }

    internal readonly struct AnimationPoseValueNativeReadBinding
    {

        internal AnimationPoseValueNativeReadBinding(
            in CharacterPoseNativePoseReadBinding native)
        {
            if (!native.IsValid)
                throw new ArgumentException(
                    "Animation pose native read binding is invalid.",
                    nameof(native));
            CompletionIdentity = native.CompletionIdentity;
            ValueIndex = -1;
            DensePoses = native.DenseLocalPoses;
            PoseParameters = native.PoseParameters;
            PoseParameterAvailability = native.PoseParameterAvailability;
            Contributions = native.Contributions;
            DenseContributionWeights = native.DenseContributionWeights;
            ContributionCount = native.ContributionCount;
            OutputWeight = native.OutputWeight;
            LeftFootFeatures = native.LeftFootFeatures;
            RightFootFeatures = native.RightFootFeatures;
            HasFootFeatures = native.HasFootFeatures;
            Availability = native.Availability;
            ContinuityIdentity = native.ContinuityIdentity;
            InvalidReason = native.InvalidReason;
            PoseGraphInvalidOperationIndex = default;
        }


        internal ulong CompletionIdentity { get; }
        internal int ValueIndex { get; }
        internal readonly NativeSlice<AnimationLocalBonePose> DensePoses;
        internal readonly NativeSlice<float> PoseParameters;
        internal readonly NativeSlice<byte> PoseParameterAvailability;
        internal readonly NativeSlice<AnimationPrimitivePoseContribution> Contributions;
        internal readonly NativeSlice<float> DenseContributionWeights;
        internal readonly NativeSlice<int> ContributionCount;
        internal readonly NativeSlice<float> OutputWeight;
        internal readonly NativeSlice<AnimationFootFeatureSample> LeftFootFeatures;
        internal readonly NativeSlice<AnimationFootFeatureSample> RightFootFeatures;
        internal readonly NativeSlice<byte> HasFootFeatures;
        internal readonly NativeSlice<AnimationPoseAvailability> Availability;
        internal readonly NativeSlice<ulong> ContinuityIdentity;
        internal readonly NativeSlice<AnimationPoseNativeInvalidReason> InvalidReason;
        internal readonly NativeSlice<int> PoseGraphInvalidOperationIndex;
    }

}
