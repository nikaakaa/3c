using System;
using Unity.Collections;

namespace ThirdPersonCharacter.Pipeline.Animation.BlendStack
{
    internal enum AnimationSlotBlendFramePlanKind : byte
    {
        CrossFade = 1,
        StoredCapture = 2,
        Unavailable = 3
    }

    internal readonly struct AnimationSlotBlendFramePlanPreparation
    {
        internal AnimationSlotBlendFramePlanPreparation(int pageIndex, ulong preparationIdentity, ulong completionIdentity)
        {
            if ((uint)pageIndex > 1u || preparationIdentity == 0 || completionIdentity == 0)
                throw new ArgumentException("Animation Slot Blend frame plan preparation is invalid.");
            PageIndex = pageIndex;
            PreparationIdentity = preparationIdentity;
            CompletionIdentity = completionIdentity;
        }

        internal int PageIndex { get; }
        internal ulong PreparationIdentity { get; }
        internal ulong CompletionIdentity { get; }
        internal bool IsValid => (uint)PageIndex <= 1u && PreparationIdentity != 0 && CompletionIdentity != 0;
    }

    internal readonly struct AnimationSlotBlendFramePlanEntry
    {
        internal AnimationSlotBlendFramePlanEntry(
            int sourceCaptureIndex,
            int physicalSourceIndex,
            ulong physicalSourceGeneration,
            AnimationPoseContributionKind kind,
            int programProducerIndex,
            ulong contributionContinuityIdentity,
            float scalarWeight,
            float leftFootWeight,
            float rightFootWeight)
        {
            bool live = kind == AnimationPoseContributionKind.Live;
            bool stored = kind == AnimationPoseContributionKind.Stored;
            if ((!live && !stored) ||
                live && (sourceCaptureIndex < 0 || physicalSourceIndex < 0 ||
                         physicalSourceGeneration == 0 || programProducerIndex < 0) ||
                stored && (sourceCaptureIndex != -1 || physicalSourceIndex != -1 ||
                           physicalSourceGeneration != 0 || programProducerIndex != -1) ||
                contributionContinuityIdentity == 0 ||
                !IsNormalized(scalarWeight) || !IsNormalized(leftFootWeight) || !IsNormalized(rightFootWeight))
                throw new ArgumentException("Animation Slot Blend frame plan entry is invalid.");

            SourceCaptureIndex = sourceCaptureIndex;
            PhysicalSourceIndex = physicalSourceIndex;
            PhysicalSourceGeneration = physicalSourceGeneration;
            Kind = kind;
            ProgramProducerIndex = programProducerIndex;
            ContributionContinuityIdentity = contributionContinuityIdentity;
            ScalarWeight = scalarWeight;
            LeftFootWeight = leftFootWeight;
            RightFootWeight = rightFootWeight;
        }

        internal int SourceCaptureIndex { get; }
        internal int PhysicalSourceIndex { get; }
        internal ulong PhysicalSourceGeneration { get; }
        internal AnimationPoseContributionKind Kind { get; }
        internal int ProgramProducerIndex { get; }
        internal ulong ContributionContinuityIdentity { get; }
        internal float ScalarWeight { get; }
        internal float LeftFootWeight { get; }
        internal float RightFootWeight { get; }

        internal bool IsValid
        {
            get
            {
                bool live = Kind == AnimationPoseContributionKind.Live;
                bool stored = Kind == AnimationPoseContributionKind.Stored;
                return (live || stored) &&
                       (live
                           ? SourceCaptureIndex >= 0 && PhysicalSourceIndex >= 0 &&
                             PhysicalSourceGeneration != 0 && ProgramProducerIndex >= 0
                           : SourceCaptureIndex == -1 && PhysicalSourceIndex == -1 &&
                             PhysicalSourceGeneration == 0 && ProgramProducerIndex == -1) &&
                       ContributionContinuityIdentity != 0 &&
                       IsNormalized(ScalarWeight) && IsNormalized(LeftFootWeight) && IsNormalized(RightFootWeight);
            }
        }

        static bool IsNormalized(float value) => float.IsFinite(value) && value >= 0f && value <= 1f;
    }

    internal readonly struct AnimationSlotBlendFramePlanHeader
    {
        internal AnimationSlotBlendFramePlanHeader(
            int pageIndex,
            ulong planGeneration,
            int physicalPlayerIndex,
            ulong completionIdentity,
            ulong continuityIdentity,
            AnimationSlotBlendFramePlanKind kind,
            AnimationSelectionAvailabilityPolicy outputPolicy,
            CharacterAnimationScalePolicy scalePolicy,
            AnimationPoseAvailability availability,
            AnimationPoseNativeInvalidReason invalidReason,
            float outputWeight,
            int contributionCount,
            int maxActiveSourceEntries,
            int contributionCapacity,
            int boneCount,
            int parameterCount,
            int historyReadPageIndex,
            int historyWritePageIndex,
            ulong historyCompletionIdentity)
        {
            PageIndex = pageIndex;
            PlanGeneration = planGeneration;
            PhysicalPlayerIndex = physicalPlayerIndex;
            CompletionIdentity = completionIdentity;
            ContinuityIdentity = continuityIdentity;
            Kind = kind;
            OutputPolicy = outputPolicy;
            ScalePolicy = scalePolicy;
            Availability = availability;
            InvalidReason = invalidReason;
            OutputWeight = outputWeight;
            ContributionCount = contributionCount;
            MaxActiveSourceEntries = maxActiveSourceEntries;
            ContributionCapacity = contributionCapacity;
            BoneCount = boneCount;
            ParameterCount = parameterCount;
            HistoryReadPageIndex = historyReadPageIndex;
            HistoryWritePageIndex = historyWritePageIndex;
            HistoryCompletionIdentity = historyCompletionIdentity;
            RequireValid();
        }

        internal int PageIndex { get; }
        internal ulong PlanGeneration { get; }
        internal int PhysicalPlayerIndex { get; }
        internal ulong CompletionIdentity { get; }
        internal ulong ContinuityIdentity { get; }
        internal AnimationSlotBlendFramePlanKind Kind { get; }
        internal AnimationSelectionAvailabilityPolicy OutputPolicy { get; }
        internal CharacterAnimationScalePolicy ScalePolicy { get; }
        internal AnimationPoseAvailability Availability { get; }
        internal AnimationPoseNativeInvalidReason InvalidReason { get; }
        internal float OutputWeight { get; }
        internal int ContributionCount { get; }
        internal int MaxActiveSourceEntries { get; }
        internal int ContributionCapacity { get; }
        internal int BoneCount { get; }
        internal int ParameterCount { get; }
        internal int HistoryReadPageIndex { get; }
        internal int HistoryWritePageIndex { get; }
        internal ulong HistoryCompletionIdentity { get; }
        internal bool CapturesHistory => Kind == AnimationSlotBlendFramePlanKind.StoredCapture;

        internal void RequireValid()
        {
            bool pose = Availability == AnimationPoseAvailability.Pose;
            bool noPose = Availability == AnimationPoseAvailability.NoPose;
            bool invalid = Availability == AnimationPoseAvailability.Invalid;
            bool validHistoryRead = HistoryReadPageIndex == -1
                ? HistoryCompletionIdentity == 0
                : (uint)HistoryReadPageIndex <= 1u && HistoryCompletionIdentity != 0;
            if ((uint)PageIndex > 1u || PlanGeneration == 0 || PhysicalPlayerIndex < 0 ||
                CompletionIdentity == 0 || ContinuityIdentity == 0 ||
                Kind != AnimationSlotBlendFramePlanKind.CrossFade &&
                Kind != AnimationSlotBlendFramePlanKind.StoredCapture &&
                Kind != AnimationSlotBlendFramePlanKind.Unavailable ||
                !Enum.IsDefined(typeof(AnimationSelectionAvailabilityPolicy), OutputPolicy) ||
                !Enum.IsDefined(typeof(CharacterAnimationScalePolicy), ScalePolicy) ||
                (!pose && !noPose && !invalid) ||
                !Enum.IsDefined(typeof(AnimationPoseNativeInvalidReason), InvalidReason) ||
                !float.IsFinite(OutputWeight) || OutputWeight < 0f || OutputWeight > 1f ||
                MaxActiveSourceEntries < 2 || ContributionCapacity != checked(MaxActiveSourceEntries + 1) ||
                ContributionCount < 0 || ContributionCount > ContributionCapacity ||
                BoneCount <= 0 || ParameterCount <= 0 ||
                !validHistoryRead || (uint)HistoryWritePageIndex > 1u ||
                HistoryReadPageIndex >= 0 && HistoryReadPageIndex == HistoryWritePageIndex ||
                CapturesHistory && HistoryReadPageIndex < 0 ||
                pose && ContributionCount == 0 ||
                noPose && (OutputWeight != 0f || ContributionCount != 0 || Kind != AnimationSlotBlendFramePlanKind.CrossFade) ||
                noPose && OutputPolicy == AnimationSelectionAvailabilityPolicy.RequireSelection ||
                invalid && (Kind != AnimationSlotBlendFramePlanKind.Unavailable ||
                            InvalidReason == AnimationPoseNativeInvalidReason.None ||
                            OutputWeight != 0f || ContributionCount != 0 || HistoryReadPageIndex >= 0) ||
                !invalid && InvalidReason != AnimationPoseNativeInvalidReason.None)
                throw new InvalidOperationException("Animation Slot Blend frame plan header is invalid.");
        }
    }

    internal readonly struct AnimationSlotBlendFramePlan
    {
        readonly AnimationSlotBlendFramePlanHeader m_Header;
        readonly NativeSlice<AnimationSlotBlendFramePlanEntry> m_Entries;
        readonly NativeSlice<float> m_DenseBoneWeights;

        internal AnimationSlotBlendFramePlan(
            AnimationSlotBlendFramePlanHeader header,
            NativeArray<AnimationSlotBlendFramePlanEntry> entries,
            NativeArray<float> denseBoneWeights)
        {
            header.RequireValid();
            int entryOffset = checked(header.PageIndex * header.ContributionCapacity);
            int denseOffset = checked(entryOffset * header.BoneCount);
            if (!entries.IsCreated || entries.Length != checked(header.ContributionCapacity * 2) ||
                !denseBoneWeights.IsCreated ||
                denseBoneWeights.Length != checked(header.ContributionCapacity * header.BoneCount * 2))
                throw new ArgumentException("Animation Slot Blend frame plan Native layout is invalid.");

            m_Header = header;
            m_Entries = new NativeSlice<AnimationSlotBlendFramePlanEntry>(
                entries, entryOffset, header.ContributionCapacity);
            m_DenseBoneWeights = new NativeSlice<float>(
                denseBoneWeights, denseOffset, checked(header.ContributionCapacity * header.BoneCount));
        }

        internal AnimationSlotBlendFramePlanHeader Header => m_Header;
        internal int ContributionCount => m_Header.ContributionCount;
        internal int ContributionCapacity => m_Header.ContributionCapacity;
        internal int BoneCount => m_Header.BoneCount;
        internal int ParameterCount => m_Header.ParameterCount;
        internal bool IsCreated =>
            m_Header.PlanGeneration != 0 &&
            m_Entries.Length == m_Header.ContributionCapacity &&
            m_DenseBoneWeights.Length == checked(m_Header.ContributionCapacity * m_Header.BoneCount);

        internal AnimationSlotBlendFramePlanEntry GetEntry(int contributionIndex)
        {
            if ((uint)contributionIndex >= (uint)ContributionCount)
                throw new ArgumentOutOfRangeException(nameof(contributionIndex));
            return m_Entries[contributionIndex];
        }

        internal float GetDenseBoneWeight(int contributionIndex, int boneIndex)
        {
            if ((uint)contributionIndex >= (uint)ContributionCount || (uint)boneIndex >= (uint)BoneCount)
                throw new ArgumentOutOfRangeException();
            return m_DenseBoneWeights[contributionIndex * BoneCount + boneIndex];
        }

        internal void RequireValidLayout()
        {
            m_Header.RequireValid();
            if (!IsCreated)
                throw new InvalidOperationException("Animation Slot Blend frame plan is not created.");
        }
    }
}
