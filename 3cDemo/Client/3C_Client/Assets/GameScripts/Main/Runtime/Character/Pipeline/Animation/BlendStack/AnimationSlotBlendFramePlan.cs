using System;
using Unity.Collections;

namespace ThirdPersonCharacter.Pipeline.Animation.BlendStack
{
    internal enum AnimationSlotBlendFramePlanKind : byte
    {
        CrossFade = 1,
        StoredCapture = 2,
        InertialContinue = 3,
        InertialCapture = 4,
        InertialRebase = 5
    }

    internal readonly struct AnimationSlotBlendFramePlanPreparation
    {
        internal AnimationSlotBlendFramePlanPreparation(
            int pageIndex,
            ulong preparationIdentity,
            ulong completionIdentity)
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
            int kindValue = (int)kind;
            bool live = kind == AnimationPoseContributionKind.Live;
            if (kindValue < (int)AnimationPoseContributionKind.Live ||
                kindValue > (int)AnimationPoseContributionKind.Inertial ||
                live && (sourceCaptureIndex < 0 || physicalSourceIndex < 0 ||
                         physicalSourceGeneration == 0 || programProducerIndex < 0) ||
                !live && (sourceCaptureIndex != -1 || physicalSourceIndex != -1 ||
                          physicalSourceGeneration != 0 || programProducerIndex != -1) ||
                contributionContinuityIdentity == 0 ||
                !IsNormalized(scalarWeight) || !IsNormalized(leftFootWeight) || !IsNormalized(rightFootWeight))
            {
                throw new ArgumentException("Animation Slot Blend frame plan entry is invalid.");
            }

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
                int kindValue = (int)Kind;
                bool live = Kind == AnimationPoseContributionKind.Live;
                return kindValue >= (int)AnimationPoseContributionKind.Live &&
                       kindValue <= (int)AnimationPoseContributionKind.Inertial &&
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

    internal readonly struct AnimationSlotBlendInertialBonePlan
    {
        internal AnimationSlotBlendInertialBonePlan(
            float residualWeight,
            float residualTimeSeconds,
            float residualWeightDerivativePerSecond)
        {
            if (!float.IsFinite(residualWeight) ||
                !float.IsFinite(residualTimeSeconds) || residualTimeSeconds < 0f ||
                !float.IsFinite(residualWeightDerivativePerSecond))
            {
                throw new ArgumentException("Animation Slot Blend Inertial Bone plan is invalid.");
            }

            ResidualWeight = residualWeight;
            ResidualTimeSeconds = residualTimeSeconds;
            ResidualWeightDerivativePerSecond = residualWeightDerivativePerSecond;
        }

        internal float ResidualWeight { get; }
        internal float ResidualTimeSeconds { get; }
        internal float ResidualWeightDerivativePerSecond { get; }
        internal bool IsValid =>
            float.IsFinite(ResidualWeight) &&
            float.IsFinite(ResidualTimeSeconds) && ResidualTimeSeconds >= 0f &&
            float.IsFinite(ResidualWeightDerivativePerSecond);
    }

    internal readonly struct AnimationSlotBlendFramePlanHeader
    {
        internal AnimationSlotBlendFramePlanHeader(
            int pageIndex,
            ulong planGeneration,
            int physicalSlotIndex,
            ulong completionIdentity,
            ulong continuityIdentity,
            AnimationSlotBlendFramePlanKind kind,
            PoseSlotOutputPolicy outputPolicy,
            CharacterAnimationScalePolicy scalePolicy,
            PoseSlotFrameAvailability availability,
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
            PhysicalSlotIndex = physicalSlotIndex;
            CompletionIdentity = completionIdentity;
            ContinuityIdentity = continuityIdentity;
            Kind = kind;
            OutputPolicy = outputPolicy;
            ScalePolicy = scalePolicy;
            Availability = availability;
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
        internal int PhysicalSlotIndex { get; }
        internal ulong CompletionIdentity { get; }
        internal ulong ContinuityIdentity { get; }
        internal AnimationSlotBlendFramePlanKind Kind { get; }
        internal PoseSlotOutputPolicy OutputPolicy { get; }
        internal CharacterAnimationScalePolicy ScalePolicy { get; }
        internal PoseSlotFrameAvailability Availability { get; }
        internal float OutputWeight { get; }
        internal int ContributionCount { get; }
        internal int MaxActiveSourceEntries { get; }
        internal int ContributionCapacity { get; }
        internal int BoneCount { get; }
        internal int ParameterCount { get; }
        internal int HistoryReadPageIndex { get; }
        internal int HistoryWritePageIndex { get; }
        internal ulong HistoryCompletionIdentity { get; }

        internal bool UsesInertial =>
            Kind == AnimationSlotBlendFramePlanKind.InertialContinue ||
            Kind == AnimationSlotBlendFramePlanKind.InertialCapture ||
            Kind == AnimationSlotBlendFramePlanKind.InertialRebase;

        internal bool CapturesHistory =>
            Kind == AnimationSlotBlendFramePlanKind.StoredCapture ||
            Kind == AnimationSlotBlendFramePlanKind.InertialCapture ||
            Kind == AnimationSlotBlendFramePlanKind.InertialRebase;

        internal void RequireValid()
        {
            int kindValue = (int)Kind;
            bool pose = Availability == PoseSlotFrameAvailability.Pose;
            bool noPose = Availability == PoseSlotFrameAvailability.NoPose;
            bool validHistoryRead = HistoryReadPageIndex == -1
                ? HistoryCompletionIdentity == 0
                : (uint)HistoryReadPageIndex <= 1u && HistoryCompletionIdentity != 0;
            if ((uint)PageIndex > 1u || PlanGeneration == 0 || PhysicalSlotIndex < 0 ||
                CompletionIdentity == 0 || ContinuityIdentity == 0 ||
                kindValue < (int)AnimationSlotBlendFramePlanKind.CrossFade ||
                kindValue > (int)AnimationSlotBlendFramePlanKind.InertialRebase ||
                !IsOutputPolicy(OutputPolicy) ||
                !IsScalePolicy(ScalePolicy) ||
                (!pose && !noPose) ||
                !float.IsFinite(OutputWeight) || OutputWeight < 0f || OutputWeight > 1f ||
                MaxActiveSourceEntries < 2 || ContributionCapacity != checked(MaxActiveSourceEntries + 2) ||
                ContributionCount < 0 || ContributionCount > ContributionCapacity ||
                BoneCount <= 0 || ParameterCount <= 0 ||
                !validHistoryRead || (uint)HistoryWritePageIndex > 1u ||
                HistoryReadPageIndex >= 0 && HistoryReadPageIndex == HistoryWritePageIndex ||
                CapturesHistory && HistoryReadPageIndex < 0 ||
                pose && ContributionCount == 0 ||
                noPose && (OutputWeight != 0f || ContributionCount != 0 || Kind != AnimationSlotBlendFramePlanKind.CrossFade) ||
                noPose && OutputPolicy == PoseSlotOutputPolicy.RequireOutput ||
                UsesInertial && !pose)
            {
                throw new InvalidOperationException("Animation Slot Blend frame plan header is invalid.");
            }
        }

        static bool IsOutputPolicy(PoseSlotOutputPolicy value) =>
            (int)value >= (int)PoseSlotOutputPolicy.RequireOutput &&
            (int)value <= (int)PoseSlotOutputPolicy.AllowEmpty;

        static bool IsScalePolicy(CharacterAnimationScalePolicy value) =>
            (int)value >= (int)CharacterAnimationScalePolicy.PreserveReferenceScale &&
            (int)value <= (int)CharacterAnimationScalePolicy.BlendLocalScale;
    }

    internal readonly struct AnimationSlotBlendFramePlan
    {
        readonly AnimationSlotBlendFramePlanHeader m_Header;
        readonly NativeSlice<AnimationSlotBlendFramePlanEntry> m_Entries;
        readonly NativeSlice<float> m_DenseBoneWeights;
        readonly NativeSlice<AnimationSlotBlendInertialBonePlan> m_InertialBones;
        readonly NativeSlice<float> m_InertialParameterResidualWeights;

        internal AnimationSlotBlendFramePlan(
            AnimationSlotBlendFramePlanHeader header,
            NativeArray<AnimationSlotBlendFramePlanEntry> entries,
            NativeArray<float> denseBoneWeights,
            NativeArray<AnimationSlotBlendInertialBonePlan> inertialBones,
            NativeArray<float> inertialParameterResidualWeights)
        {
            header.RequireValid();
            int entryOffset = checked(header.PageIndex * header.ContributionCapacity);
            int denseOffset = checked(entryOffset * header.BoneCount);
            int inertialBoneOffset = checked(header.PageIndex * header.BoneCount);
            int inertialParameterOffset = checked(header.PageIndex * header.ParameterCount);
            if (!entries.IsCreated || entries.Length != checked(header.ContributionCapacity * 2) ||
                !denseBoneWeights.IsCreated ||
                denseBoneWeights.Length != checked(header.ContributionCapacity * header.BoneCount * 2) ||
                !inertialBones.IsCreated || inertialBones.Length != checked(header.BoneCount * 2) ||
                !inertialParameterResidualWeights.IsCreated ||
                inertialParameterResidualWeights.Length != checked(header.ParameterCount * 2))
            {
                throw new ArgumentException("Animation Slot Blend frame plan Native layout is invalid.");
            }

            m_Header = header;
            m_Entries = new NativeSlice<AnimationSlotBlendFramePlanEntry>(
                entries,
                entryOffset,
                header.ContributionCapacity);
            m_DenseBoneWeights = new NativeSlice<float>(
                denseBoneWeights,
                denseOffset,
                checked(header.ContributionCapacity * header.BoneCount));
            m_InertialBones = new NativeSlice<AnimationSlotBlendInertialBonePlan>(
                inertialBones,
                inertialBoneOffset,
                header.BoneCount);
            m_InertialParameterResidualWeights = new NativeSlice<float>(
                inertialParameterResidualWeights,
                inertialParameterOffset,
                header.ParameterCount);
        }

        internal AnimationSlotBlendFramePlanHeader Header => m_Header;
        internal int ContributionCount => m_Header.ContributionCount;
        internal int ContributionCapacity => m_Header.ContributionCapacity;
        internal int BoneCount => m_Header.BoneCount;
        internal int ParameterCount => m_Header.ParameterCount;
        internal bool IsCreated =>
            m_Header.PlanGeneration != 0 &&
            m_Entries.Length == m_Header.ContributionCapacity &&
            m_DenseBoneWeights.Length == checked(m_Header.ContributionCapacity * m_Header.BoneCount) &&
            m_InertialBones.Length == m_Header.BoneCount &&
            m_InertialParameterResidualWeights.Length == m_Header.ParameterCount;

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

        internal AnimationSlotBlendInertialBonePlan GetInertialBone(int boneIndex)
        {
            if ((uint)boneIndex >= (uint)BoneCount)
                throw new ArgumentOutOfRangeException(nameof(boneIndex));
            return m_InertialBones[boneIndex];
        }

        internal float GetInertialParameterResidualWeight(int parameterIndex)
        {
            if ((uint)parameterIndex >= (uint)ParameterCount)
                throw new ArgumentOutOfRangeException(nameof(parameterIndex));
            return m_InertialParameterResidualWeights[parameterIndex];
        }

        internal void RequireValidLayout()
        {
            m_Header.RequireValid();
            if (!IsCreated || m_Entries.Length != ContributionCapacity ||
                m_DenseBoneWeights.Length != checked(ContributionCapacity * BoneCount) ||
                m_InertialBones.Length != BoneCount ||
                m_InertialParameterResidualWeights.Length != ParameterCount)
            {
                throw new InvalidOperationException("Animation Slot Blend frame plan is not created.");
            }
        }
    }
}
