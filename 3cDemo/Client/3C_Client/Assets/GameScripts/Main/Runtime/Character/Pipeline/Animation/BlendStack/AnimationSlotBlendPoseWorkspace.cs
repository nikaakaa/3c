using System;
using System.Threading;
using Unity.Collections;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation.BlendStack
{
    internal struct AnimationSlotBlendStoredPoseNativeState
    {
        internal byte Active;
        internal byte HasFootFeatures;
        internal ulong CapturedAtCompletionIdentity;
        internal ulong SourceHistoryCompletionIdentity;
        internal ulong ContributionContinuityIdentity;
        internal float OutputWeight;
        internal AnimationFootFeatureSample LeftFootFeatures;
        internal AnimationFootFeatureSample RightFootFeatures;
    }

    internal struct AnimationSlotBlendHistoryNativeState
    {
        internal AnimationPoseAvailability Availability;
        internal byte HasFootFeatures;
        internal ulong CompletionIdentity;
        internal ulong ContinuityIdentity;
        internal float OutputWeight;
        internal AnimationFootFeatureSample LeftFootFeatures;
        internal AnimationFootFeatureSample RightFootFeatures;
    }

    internal struct AnimationSlotBlendScratchNativeState
    {
        internal AnimationPoseAvailability Availability;
        internal AnimationPoseNativeInvalidReason InvalidReason;
        internal byte HasFootFeatures;
        internal int ContributionCount;
        internal ulong ContinuityIdentity;
        internal float OutputWeight;
        internal AnimationFootFeatureSample LeftFootFeatures;
        internal AnimationFootFeatureSample RightFootFeatures;
    }

    internal readonly struct AnimationSlotBlendStoredPoseWorkspaceBinding
    {
        internal AnimationSlotBlendStoredPoseWorkspaceBinding(
            NativeArray<AnimationSlotBlendStoredPoseNativeState> state,
            NativeArray<AnimationLocalBonePose> denseLocalPose,
            NativeArray<AnimationBlendBoneVelocity> denseVelocity,
            NativeArray<float> poseParameters,
            NativeArray<float> denseBoneOutputWeights)
        {
            State = state;
            DenseLocalPose = denseLocalPose;
            DenseVelocity = denseVelocity;
            PoseParameters = poseParameters;
            DenseBoneOutputWeights = denseBoneOutputWeights;
        }

        internal NativeArray<AnimationSlotBlendStoredPoseNativeState> State { get; }
        internal NativeArray<AnimationLocalBonePose> DenseLocalPose { get; }
        internal NativeArray<AnimationBlendBoneVelocity> DenseVelocity { get; }
        internal NativeArray<float> PoseParameters { get; }
        internal NativeArray<float> DenseBoneOutputWeights { get; }
    }

    internal readonly struct AnimationSlotBlendHistoryWorkspaceBinding
    {
        internal AnimationSlotBlendHistoryWorkspaceBinding(
            NativeArray<AnimationSlotBlendHistoryNativeState> states,
            NativeArray<AnimationLocalBonePose> denseLocalPoses,
            NativeArray<AnimationBlendBoneVelocity> denseVelocities,
            NativeArray<float> poseParameters,
            NativeArray<float> denseBoneOutputWeights)
        {
            States = states;
            DenseLocalPoses = denseLocalPoses;
            DenseVelocities = denseVelocities;
            PoseParameters = poseParameters;
            DenseBoneOutputWeights = denseBoneOutputWeights;
        }

        internal NativeArray<AnimationSlotBlendHistoryNativeState> States { get; }
        internal NativeArray<AnimationLocalBonePose> DenseLocalPoses { get; }
        internal NativeArray<AnimationBlendBoneVelocity> DenseVelocities { get; }
        internal NativeArray<float> PoseParameters { get; }
        internal NativeArray<float> DenseBoneOutputWeights { get; }
    }

    internal readonly struct AnimationSlotBlendScratchWorkspaceBinding
    {
        internal AnimationSlotBlendScratchWorkspaceBinding(
            NativeArray<AnimationSlotBlendScratchNativeState> state,
            NativeArray<AnimationLocalBonePose> denseLocalPose,
            NativeArray<AnimationBlendBoneVelocity> denseVelocity,
            NativeArray<float> poseParameters,
            NativeArray<AnimationPrimitivePoseContribution> contributions,
            NativeArray<float> denseContributionWeights,
            NativeArray<Vector3> positionSums,
            NativeArray<Vector4> rotationSums,
            NativeArray<Vector3> scaleSums,
            NativeArray<Vector3> linearVelocitySums,
            NativeArray<Vector3> angularVelocitySums,
            NativeArray<Vector3> scaleVelocitySums,
            NativeArray<float> poseWeightSums,
            NativeArray<AnimationFootFeatureBlendAccumulator> footFeatureAccumulators)
        {
            State = state;
            DenseLocalPose = denseLocalPose;
            DenseVelocity = denseVelocity;
            PoseParameters = poseParameters;
            Contributions = contributions;
            DenseContributionWeights = denseContributionWeights;
            PositionSums = positionSums;
            RotationSums = rotationSums;
            ScaleSums = scaleSums;
            LinearVelocitySums = linearVelocitySums;
            AngularVelocitySums = angularVelocitySums;
            ScaleVelocitySums = scaleVelocitySums;
            PoseWeightSums = poseWeightSums;
            FootFeatureAccumulators = footFeatureAccumulators;
        }

        internal NativeArray<AnimationSlotBlendScratchNativeState> State { get; }
        internal NativeArray<AnimationLocalBonePose> DenseLocalPose { get; }
        internal NativeArray<AnimationBlendBoneVelocity> DenseVelocity { get; }
        internal NativeArray<float> PoseParameters { get; }
        internal NativeArray<AnimationPrimitivePoseContribution> Contributions { get; }
        internal NativeArray<float> DenseContributionWeights { get; }
        internal NativeArray<Vector3> PositionSums { get; }
        internal NativeArray<Vector4> RotationSums { get; }
        internal NativeArray<Vector3> ScaleSums { get; }
        internal NativeArray<Vector3> LinearVelocitySums { get; }
        internal NativeArray<Vector3> AngularVelocitySums { get; }
        internal NativeArray<Vector3> ScaleVelocitySums { get; }
        internal NativeArray<float> PoseWeightSums { get; }
        internal NativeArray<AnimationFootFeatureBlendAccumulator> FootFeatureAccumulators { get; }
    }

    internal readonly struct AnimationSlotBlendPoseWorkspaceBinding
    {
        internal AnimationSlotBlendPoseWorkspaceBinding(
            AnimationSlotBlendFramePlan framePlan,
            AnimationPlayerPoseNativeWriteBinding finalWriteBinding,
            AnimationSlotBlendStoredPoseWorkspaceBinding storedPose,
            AnimationSlotBlendHistoryWorkspaceBinding history,
            AnimationSlotBlendScratchWorkspaceBinding scratch)
        {
            framePlan.RequireValidLayout();
            if (framePlan.Header.CompletionIdentity != finalWriteBinding.CompletionIdentity ||
                framePlan.Header.PhysicalPlayerIndex != finalWriteBinding.Range.PhysicalPlayerIndex)
            {
                throw new ArgumentException("Animation Slot Blend workspace binding is not aligned to its final Slot output.");
            }

            FramePlan = framePlan;
            FinalWriteBinding = finalWriteBinding;
            StoredPose = storedPose;
            History = history;
            Scratch = scratch;
        }

        internal AnimationSlotBlendFramePlan FramePlan { get; }
        internal AnimationPlayerPoseNativeWriteBinding FinalWriteBinding { get; }
        internal AnimationSlotBlendStoredPoseWorkspaceBinding StoredPose { get; }
        internal AnimationSlotBlendHistoryWorkspaceBinding History { get; }
        internal AnimationSlotBlendScratchWorkspaceBinding Scratch { get; }
    }

    internal sealed class AnimationSlotBlendPoseWorkspace : IDisposable
    {
        const float WeightTolerance = 0.0001f;

        readonly int m_PhysicalPlayerIndex;
        readonly int m_MaxActiveSourceEntries;
        readonly int m_ContributionCapacity;
        readonly int m_BoneCount;
        readonly int m_ParameterCount;

        NativeArray<AnimationSlotBlendFramePlanHeader> m_PlanHeaders;
        NativeArray<AnimationSlotBlendFramePlanEntry> m_PlanEntries;
        NativeArray<float> m_PlanDenseBoneWeights;
        NativeArray<byte> m_PlanEntryWritten;
        NativeArray<byte> m_PlanDenseBoneWeightWritten;

        NativeArray<AnimationSlotBlendStoredPoseNativeState> m_StoredState;
        NativeArray<AnimationLocalBonePose> m_StoredPose;
        NativeArray<AnimationBlendBoneVelocity> m_StoredVelocity;
        NativeArray<float> m_StoredParameters;
        NativeArray<float> m_StoredBoneOutputWeights;

        NativeArray<AnimationSlotBlendHistoryNativeState> m_HistoryStates;
        NativeArray<AnimationLocalBonePose> m_HistoryPoses;
        NativeArray<AnimationBlendBoneVelocity> m_HistoryVelocities;
        NativeArray<float> m_HistoryParameters;
        NativeArray<float> m_HistoryBoneOutputWeights;

        NativeArray<AnimationSlotBlendScratchNativeState> m_ScratchState;
        NativeArray<AnimationLocalBonePose> m_ScratchPose;
        NativeArray<AnimationBlendBoneVelocity> m_ScratchVelocity;
        NativeArray<float> m_ScratchParameters;
        NativeArray<AnimationPrimitivePoseContribution> m_ScratchContributions;
        NativeArray<float> m_ScratchDenseContributionWeights;
        NativeArray<Vector3> m_ScratchPositionSums;
        NativeArray<Vector4> m_ScratchRotationSums;
        NativeArray<Vector3> m_ScratchScaleSums;
        NativeArray<Vector3> m_ScratchLinearVelocitySums;
        NativeArray<Vector3> m_ScratchAngularVelocitySums;
        NativeArray<Vector3> m_ScratchScaleVelocitySums;
        NativeArray<float> m_ScratchPoseWeightSums;
        NativeArray<AnimationFootFeatureBlendAccumulator> m_ScratchFootFeatureAccumulators;

        AnimationPlayerPoseNativeWriteBinding m_PageZeroFinalWriteBinding;
        AnimationPlayerPoseNativeWriteBinding m_PageOneFinalWriteBinding;
        int m_ActivePageIndex = -1;
        int m_PreparationPageIndex = -1;
        ulong m_PreparationIdentity;
        ulong m_LastPreparationIdentity;
        ulong m_LastCommittedCompletionIdentity;
        int m_CommittedActivePageIndex = -1;
        ulong m_CommittedCompletionIdentity;
        bool m_PreparationValidated;
        bool m_FrameOpen;
        bool m_Disposed;

        internal AnimationSlotBlendPoseWorkspace(
            int maxActiveSourceEntries,
            in AnimationPlayerPoseNativeWriteBinding initialFinalWriteBinding)
        {
            if (maxActiveSourceEntries < 2)
                throw new ArgumentOutOfRangeException(nameof(maxActiveSourceEntries));
            if (initialFinalWriteBinding.DenseLocalPoses.Length <= 0 ||
                initialFinalWriteBinding.PoseParameters.Length <= 0)
            {
                throw new ArgumentException("Animation Slot Blend final write layout is invalid.", nameof(initialFinalWriteBinding));
            }

            m_PhysicalPlayerIndex = initialFinalWriteBinding.Range.PhysicalPlayerIndex;
            m_MaxActiveSourceEntries = maxActiveSourceEntries;
            m_ContributionCapacity = checked(maxActiveSourceEntries + 1);
            m_BoneCount = initialFinalWriteBinding.DenseLocalPoses.Length;
            m_ParameterCount = initialFinalWriteBinding.PoseParameters.Length;
            RequireFinalWriteBinding(in initialFinalWriteBinding);

            try
            {
                m_PlanHeaders = Allocate<AnimationSlotBlendFramePlanHeader>(2);
                m_PlanEntries = Allocate<AnimationSlotBlendFramePlanEntry>(checked(m_ContributionCapacity * 2));
                m_PlanDenseBoneWeights = Allocate<float>(checked(m_ContributionCapacity * m_BoneCount * 2));
                m_PlanEntryWritten = Allocate<byte>(checked(m_ContributionCapacity * 2));
                m_PlanDenseBoneWeightWritten = Allocate<byte>(checked(m_ContributionCapacity * m_BoneCount * 2));

                m_StoredState = Allocate<AnimationSlotBlendStoredPoseNativeState>(1);
                m_StoredPose = Allocate<AnimationLocalBonePose>(m_BoneCount);
                m_StoredVelocity = Allocate<AnimationBlendBoneVelocity>(m_BoneCount);
                m_StoredParameters = Allocate<float>(m_ParameterCount);
                m_StoredBoneOutputWeights = Allocate<float>(m_BoneCount);

                m_HistoryStates = Allocate<AnimationSlotBlendHistoryNativeState>(2);
                m_HistoryPoses = Allocate<AnimationLocalBonePose>(checked(m_BoneCount * 2));
                m_HistoryVelocities = Allocate<AnimationBlendBoneVelocity>(checked(m_BoneCount * 2));
                m_HistoryParameters = Allocate<float>(checked(m_ParameterCount * 2));
                m_HistoryBoneOutputWeights = Allocate<float>(checked(m_BoneCount * 2));

                m_ScratchState = Allocate<AnimationSlotBlendScratchNativeState>(1);
                m_ScratchPose = Allocate<AnimationLocalBonePose>(m_BoneCount);
                m_ScratchVelocity = Allocate<AnimationBlendBoneVelocity>(m_BoneCount);
                m_ScratchParameters = Allocate<float>(m_ParameterCount);
                m_ScratchContributions = Allocate<AnimationPrimitivePoseContribution>(m_ContributionCapacity);
                m_ScratchDenseContributionWeights = Allocate<float>(checked(m_ContributionCapacity * m_BoneCount));
                m_ScratchPositionSums = Allocate<Vector3>(m_BoneCount);
                m_ScratchRotationSums = Allocate<Vector4>(m_BoneCount);
                m_ScratchScaleSums = Allocate<Vector3>(m_BoneCount);
                m_ScratchLinearVelocitySums = Allocate<Vector3>(m_BoneCount);
                m_ScratchAngularVelocitySums = Allocate<Vector3>(m_BoneCount);
                m_ScratchScaleVelocitySums = Allocate<Vector3>(m_BoneCount);
                m_ScratchPoseWeightSums = Allocate<float>(m_BoneCount);
                m_ScratchFootFeatureAccumulators = Allocate<AnimationFootFeatureBlendAccumulator>(2);
            }
            catch
            {
                DisposeOwnedArrays();
                throw;
            }
        }

        internal int PhysicalPlayerIndex => m_PhysicalPlayerIndex;
        internal int MaxActiveSourceEntries => m_MaxActiveSourceEntries;
        internal int ContributionCapacity => m_ContributionCapacity;
        internal int BoneCount => m_BoneCount;
        internal int ParameterCount => m_ParameterCount;
        internal bool HasActivePlan => Volatile.Read(ref m_ActivePageIndex) >= 0;

        internal void BeginFrame()
        {
            RequireAlive();
            if (m_FrameOpen || m_PreparationPageIndex >= 0)
                throw new InvalidOperationException("Animation Slot Blend workspace frame is already open.");
            m_CommittedActivePageIndex =
                Volatile.Read(ref m_ActivePageIndex);
            m_CommittedCompletionIdentity =
                m_LastCommittedCompletionIdentity;
            m_FrameOpen = true;
        }

        internal void DiscardFrame()
        {
            RequireAlive();
            if (!m_FrameOpen)
                return;
            int pendingPageIndex =
                Volatile.Read(ref m_ActivePageIndex);
            if (m_PreparationPageIndex >= 0)
                pendingPageIndex = m_PreparationPageIndex;
            if (pendingPageIndex >= 0 &&
                pendingPageIndex != m_CommittedActivePageIndex)
            {
                ClearPlanPage(pendingPageIndex);
                SetFinalWriteBinding(pendingPageIndex, default);
            }
            Volatile.Write(
                ref m_ActivePageIndex,
                m_CommittedActivePageIndex);
            m_LastCommittedCompletionIdentity =
                m_CommittedCompletionIdentity;
            ClearPreparation();
            m_FrameOpen = false;
        }

        internal void CommitFrame()
        {
            RequireAlive();
            if (!m_FrameOpen || m_PreparationPageIndex >= 0)
                throw new InvalidOperationException("Animation Slot Blend workspace frame is not sealed.");
            m_FrameOpen = false;
        }

        internal AnimationSlotBlendFramePlanPreparation PrepareInactivePage(
            in AnimationPlayerPoseNativeWriteBinding finalWriteBinding,
            AnimationSlotBlendFramePlanKind kind,
            AnimationSelectionAvailabilityPolicy outputPolicy,
            CharacterAnimationScalePolicy scalePolicy,
            AnimationPoseAvailability availability,
            AnimationPoseNativeInvalidReason invalidReason,
            float outputWeight,
            int contributionCount,
            ulong continuityIdentity,
            ulong historyCompletionIdentity)
        {
            RequireAlive();
            if (m_PreparationPageIndex >= 0)
                throw new InvalidOperationException("Animation Slot Blend inactive plan page is already being prepared.");
            RequireFinalWriteBinding(in finalWriteBinding);
            if (finalWriteBinding.CompletionIdentity <= m_LastCommittedCompletionIdentity)
                throw new InvalidOperationException("Animation Slot Blend frame completion identity is not strictly increasing.");
            if (m_LastPreparationIdentity == ulong.MaxValue)
                throw new InvalidOperationException("Animation Slot Blend frame plan generation overflowed.");

            int activePage = Volatile.Read(ref m_ActivePageIndex);
            int pageIndex = activePage < 0 ? 0 : activePage ^ 1;
            int historyReadPageIndex = historyCompletionIdentity == 0
                ? -1
                : RequireHistoryPage(historyCompletionIdentity);
            int newestHistoryPageIndex = FindNewestHistoryPage();
            int historyWritePageIndex = historyReadPageIndex >= 0
                ? historyReadPageIndex ^ 1
                : newestHistoryPageIndex < 0 ? 0 : newestHistoryPageIndex ^ 1;
            ulong preparationIdentity = ++m_LastPreparationIdentity;

            ClearPlanPage(pageIndex);
            m_PlanHeaders[pageIndex] = new AnimationSlotBlendFramePlanHeader(
                pageIndex,
                preparationIdentity,
                m_PhysicalPlayerIndex,
                finalWriteBinding.CompletionIdentity,
                continuityIdentity,
                kind,
                outputPolicy,
                scalePolicy,
                availability,
                invalidReason,
                outputWeight,
                contributionCount,
                m_MaxActiveSourceEntries,
                m_ContributionCapacity,
                m_BoneCount,
                m_ParameterCount,
                historyReadPageIndex,
                historyWritePageIndex,
                historyCompletionIdentity);
            SetFinalWriteBinding(pageIndex, finalWriteBinding);
            m_PreparationPageIndex = pageIndex;
            m_PreparationIdentity = preparationIdentity;
            m_PreparationValidated = false;
            return new AnimationSlotBlendFramePlanPreparation(
                pageIndex,
                preparationIdentity,
                finalWriteBinding.CompletionIdentity);
        }

        internal void SetPreparedEntry(
            in AnimationSlotBlendFramePlanPreparation preparation,
            int contributionIndex,
            in AnimationSlotBlendFramePlanEntry entry)
        {
            AnimationSlotBlendFramePlanHeader header = RequirePreparing(preparation);
            if ((uint)contributionIndex >= (uint)header.ContributionCount)
                throw new ArgumentOutOfRangeException(nameof(contributionIndex));
            if (!entry.IsValid)
                throw new ArgumentException("Animation Slot Blend frame plan entry is invalid.", nameof(entry));
            int index = checked(header.PageIndex * m_ContributionCapacity + contributionIndex);
            if (m_PlanEntryWritten[index] != 0)
                throw new InvalidOperationException($"Animation Slot Blend frame plan entry #{contributionIndex} was written twice.");
            m_PlanEntries[index] = entry;
            m_PlanEntryWritten[index] = 1;
        }

        internal void SetPreparedDenseBoneWeight(
            in AnimationSlotBlendFramePlanPreparation preparation,
            int contributionIndex,
            int boneIndex,
            float weight)
        {
            AnimationSlotBlendFramePlanHeader header = RequirePreparing(preparation);
            if ((uint)contributionIndex >= (uint)header.ContributionCount || (uint)boneIndex >= (uint)m_BoneCount)
                throw new ArgumentOutOfRangeException();
            if (!IsNormalized(weight))
                throw new ArgumentOutOfRangeException(nameof(weight));
            int pageOffset = checked(header.PageIndex * m_ContributionCapacity * m_BoneCount);
            int index = checked(pageOffset + contributionIndex * m_BoneCount + boneIndex);
            if (m_PlanDenseBoneWeightWritten[index] != 0)
                throw new InvalidOperationException("Animation Slot Blend dense Bone weight was written twice.");
            m_PlanDenseBoneWeights[index] = weight;
            m_PlanDenseBoneWeightWritten[index] = 1;
        }

        internal void ValidateInactivePage(in AnimationSlotBlendFramePlanPreparation preparation)
        {
            RequireAlive();
            if (m_PreparationValidated)
            {
                RequirePreparation(preparation);
                return;
            }
            AnimationSlotBlendFramePlanHeader header = RequirePreparing(preparation);
            RequirePreparedPageContent(header);
            m_PreparationValidated = true;
        }

        internal AnimationSlotBlendFramePlan CommitInactivePage(
            in AnimationSlotBlendFramePlanPreparation preparation)
        {
            RequireAlive();
            RequirePreparation(preparation);
            if (!m_PreparationValidated)
                throw new InvalidOperationException("Animation Slot Blend inactive plan page was not validated.");

            AnimationSlotBlendFramePlanHeader header = m_PlanHeaders[m_PreparationPageIndex];
            RequirePreparedPageContent(header);
            int committedPage = m_PreparationPageIndex;
            Interlocked.Exchange(ref m_ActivePageIndex, committedPage);
            m_LastCommittedCompletionIdentity = header.CompletionIdentity;
            ClearPreparation();
            return CreatePlan(committedPage);
        }

        internal void AbortInactivePage(in AnimationSlotBlendFramePlanPreparation preparation)
        {
            RequireAlive();
            RequirePreparation(preparation);
            int pageIndex = m_PreparationPageIndex;
            ClearPreparation();
            ClearPlanPage(pageIndex);
            SetFinalWriteBinding(pageIndex, default);
        }

        internal AnimationSlotBlendFramePlan RequireActivePlan()
        {
            RequireAlive();
            int pageIndex = Volatile.Read(ref m_ActivePageIndex);
            if (pageIndex < 0)
                throw new InvalidOperationException("Animation Slot Blend workspace has no committed frame plan.");
            return CreatePlan(pageIndex);
        }

        internal AnimationSlotBlendPoseWorkspaceBinding RequireActiveBinding()
        {
            AnimationSlotBlendFramePlan plan = RequireActivePlan();
            AnimationPlayerPoseNativeWriteBinding finalWriteBinding = GetFinalWriteBinding(plan.Header.PageIndex);
            RequireFinalWriteBinding(in finalWriteBinding);
            return new AnimationSlotBlendPoseWorkspaceBinding(
                plan,
                finalWriteBinding,
                new AnimationSlotBlendStoredPoseWorkspaceBinding(
                    m_StoredState,
                    m_StoredPose,
                    m_StoredVelocity,
                    m_StoredParameters,
                    m_StoredBoneOutputWeights),
                new AnimationSlotBlendHistoryWorkspaceBinding(
                    m_HistoryStates,
                    m_HistoryPoses,
                    m_HistoryVelocities,
                    m_HistoryParameters,
                    m_HistoryBoneOutputWeights),
                new AnimationSlotBlendScratchWorkspaceBinding(
                    m_ScratchState,
                    m_ScratchPose,
                    m_ScratchVelocity,
                    m_ScratchParameters,
                    m_ScratchContributions,
                    m_ScratchDenseContributionWeights,
                    m_ScratchPositionSums,
                    m_ScratchRotationSums,
                    m_ScratchScaleSums,
                    m_ScratchLinearVelocitySums,
                    m_ScratchAngularVelocitySums,
                    m_ScratchScaleVelocitySums,
                    m_ScratchPoseWeightSums,
                    m_ScratchFootFeatureAccumulators));
        }

        internal void Reset()
        {
            RequireAlive();
            Clear(m_PlanHeaders);
            Clear(m_PlanEntries);
            Clear(m_PlanDenseBoneWeights);
            Clear(m_PlanEntryWritten);
            Clear(m_PlanDenseBoneWeightWritten);
            Clear(m_StoredState);
            Clear(m_StoredPose);
            Clear(m_StoredVelocity);
            Clear(m_StoredParameters);
            Clear(m_StoredBoneOutputWeights);
            Clear(m_HistoryStates);
            Clear(m_HistoryPoses);
            Clear(m_HistoryVelocities);
            Clear(m_HistoryParameters);
            Clear(m_HistoryBoneOutputWeights);
            Clear(m_ScratchState);
            Clear(m_ScratchPose);
            Clear(m_ScratchVelocity);
            Clear(m_ScratchParameters);
            Clear(m_ScratchContributions);
            Clear(m_ScratchDenseContributionWeights);
            Clear(m_ScratchPositionSums);
            Clear(m_ScratchRotationSums);
            Clear(m_ScratchScaleSums);
            Clear(m_ScratchLinearVelocitySums);
            Clear(m_ScratchAngularVelocitySums);
            Clear(m_ScratchScaleVelocitySums);
            Clear(m_ScratchPoseWeightSums);
            Clear(m_ScratchFootFeatureAccumulators);
            m_PageZeroFinalWriteBinding = default;
            m_PageOneFinalWriteBinding = default;
            Volatile.Write(ref m_ActivePageIndex, -1);
            ClearPreparation();
            m_LastCommittedCompletionIdentity = 0;
        }

        void RequirePreparedPageContent(AnimationSlotBlendFramePlanHeader header)
        {
            header.RequireValid();
            AnimationPlayerPoseNativeWriteBinding finalWriteBinding = GetFinalWriteBinding(header.PageIndex);
            RequireFinalWriteBinding(in finalWriteBinding);
            if (finalWriteBinding.CompletionIdentity != header.CompletionIdentity)
                throw new InvalidOperationException("Animation Slot Blend plan and final write completion identities differ.");
            if (header.HistoryReadPageIndex >= 0)
            {
                AnimationSlotBlendHistoryNativeState history = m_HistoryStates[header.HistoryReadPageIndex];
                if (history.CompletionIdentity != header.HistoryCompletionIdentity ||
                    history.Availability != AnimationPoseAvailability.Pose ||
                    history.ContinuityIdentity == 0 ||
                    !IsNormalized(history.OutputWeight))
                {
                    throw new InvalidOperationException("Animation Slot Blend plan history capture boundary is invalid.");
                }
            }

            int liveCount = 0;
            int storedCount = 0;
            float scalarWeight = 0f;
            float leftFootWeight = 0f;
            float rightFootWeight = 0f;
            int entryOffset = checked(header.PageIndex * m_ContributionCapacity);
            for (int i = 0; i < header.ContributionCount; i++)
            {
                int entryIndex = entryOffset + i;
                if (m_PlanEntryWritten[entryIndex] != 1)
                    throw new InvalidOperationException($"Animation Slot Blend frame plan entry #{i} is incomplete.");
                AnimationSlotBlendFramePlanEntry entry = m_PlanEntries[entryIndex];
                if (!entry.IsValid)
                    throw new InvalidOperationException($"Animation Slot Blend frame plan entry #{i} is invalid.");
                for (int previous = 0; previous < i; previous++)
                {
                    AnimationSlotBlendFramePlanEntry existing = m_PlanEntries[entryOffset + previous];
                    if (entry.ContributionContinuityIdentity == existing.ContributionContinuityIdentity)
                        throw new InvalidOperationException("Animation Slot Blend frame plan duplicates contribution continuity identity.");
                }

                switch (entry.Kind)
                {
                    case AnimationPoseContributionKind.Live:
                        liveCount++;
                        break;
                    case AnimationPoseContributionKind.Stored:
                        storedCount++;
                        break;
                }
                scalarWeight += entry.ScalarWeight;
                leftFootWeight += entry.LeftFootWeight;
                rightFootWeight += entry.RightFootWeight;
            }

            if (liveCount > m_MaxActiveSourceEntries || storedCount > 1 ||
                scalarWeight > 1f + WeightTolerance || leftFootWeight > 1f + WeightTolerance ||
                rightFootWeight > 1f + WeightTolerance ||
                Mathf.Abs(scalarWeight - header.OutputWeight) > WeightTolerance)
            {
                throw new InvalidOperationException("Animation Slot Blend frame plan contribution budget is invalid.");
            }

            if (header.Kind == AnimationSlotBlendFramePlanKind.StoredCapture && storedCount != 1)
                throw new InvalidOperationException("Animation Slot Blend Stored capture plan has no Stored contribution.");

            bool hasOutputWeight = header.OutputWeight > 0f;
            int densePageOffset = checked(header.PageIndex * m_ContributionCapacity * m_BoneCount);
            for (int boneIndex = 0; boneIndex < m_BoneCount; boneIndex++)
            {
                float boneWeight = 0f;
                for (int contributionIndex = 0; contributionIndex < header.ContributionCount; contributionIndex++)
                {
                    int denseIndex = checked(densePageOffset + contributionIndex * m_BoneCount + boneIndex);
                    if (m_PlanDenseBoneWeightWritten[denseIndex] != 1 ||
                        !IsNormalized(m_PlanDenseBoneWeights[denseIndex]))
                    {
                        throw new InvalidOperationException("Animation Slot Blend dense Bone weight plan is incomplete or invalid.");
                    }
                    boneWeight += m_PlanDenseBoneWeights[denseIndex];
                }
                if (boneWeight > 1f + WeightTolerance)
                {
                    throw new InvalidOperationException($"Animation Slot Blend Bone #{boneIndex} contribution budget is invalid.");
                }
                hasOutputWeight |= boneWeight > 0f;
            }
            if (header.Availability == AnimationPoseAvailability.Pose && !hasOutputWeight)
                throw new InvalidOperationException("Animation Slot Blend Pose plan has no scalar or dense output weight.");

        }

        AnimationSlotBlendFramePlanHeader RequirePreparing(
            in AnimationSlotBlendFramePlanPreparation preparation)
        {
            RequireAlive();
            RequirePreparation(preparation);
            if (m_PreparationValidated)
                throw new InvalidOperationException("Animation Slot Blend inactive plan page is already validated.");
            return m_PlanHeaders[m_PreparationPageIndex];
        }

        void RequirePreparation(in AnimationSlotBlendFramePlanPreparation preparation)
        {
            if (!preparation.IsValid || m_PreparationPageIndex < 0 ||
                preparation.PageIndex != m_PreparationPageIndex ||
                preparation.PreparationIdentity != m_PreparationIdentity ||
                preparation.CompletionIdentity != m_PlanHeaders[m_PreparationPageIndex].CompletionIdentity)
            {
                throw new InvalidOperationException("Animation Slot Blend frame plan preparation handle is stale.");
            }
        }

        int RequireHistoryPage(ulong completionIdentity)
        {
            int pageIndex = -1;
            for (int i = 0; i < m_HistoryStates.Length; i++)
            {
                AnimationSlotBlendHistoryNativeState state = m_HistoryStates[i];
                if (state.CompletionIdentity != completionIdentity)
                    continue;
                if (pageIndex >= 0)
                    throw new InvalidOperationException("Animation Slot Blend history completion identity is duplicated.");
                pageIndex = i;
            }
            if (pageIndex < 0)
                throw new InvalidOperationException("Animation Slot Blend requested history completion is unavailable.");
            return pageIndex;
        }

        int FindNewestHistoryPage()
        {
            int pageIndex = -1;
            ulong completionIdentity = 0;
            for (int i = 0; i < m_HistoryStates.Length; i++)
            {
                ulong candidate = m_HistoryStates[i].CompletionIdentity;
                if (candidate <= completionIdentity)
                    continue;
                completionIdentity = candidate;
                pageIndex = i;
            }
            return pageIndex;
        }

        AnimationSlotBlendFramePlan CreatePlan(int pageIndex)
        {
            return new AnimationSlotBlendFramePlan(
                m_PlanHeaders[pageIndex],
                m_PlanEntries,
                m_PlanDenseBoneWeights);
        }

        void ClearPlanPage(int pageIndex)
        {
            m_PlanHeaders[pageIndex] = default;
            ClearRange(m_PlanEntries, checked(pageIndex * m_ContributionCapacity), m_ContributionCapacity);
            ClearRange(m_PlanDenseBoneWeights,
                checked(pageIndex * m_ContributionCapacity * m_BoneCount),
                checked(m_ContributionCapacity * m_BoneCount));
            ClearRange(m_PlanEntryWritten, checked(pageIndex * m_ContributionCapacity), m_ContributionCapacity);
            ClearRange(m_PlanDenseBoneWeightWritten,
                checked(pageIndex * m_ContributionCapacity * m_BoneCount),
                checked(m_ContributionCapacity * m_BoneCount));
        }

        void ClearPreparation()
        {
            m_PreparationPageIndex = -1;
            m_PreparationIdentity = 0;
            m_PreparationValidated = false;
        }

        AnimationPlayerPoseNativeWriteBinding GetFinalWriteBinding(int pageIndex) =>
            pageIndex == 0 ? m_PageZeroFinalWriteBinding : m_PageOneFinalWriteBinding;

        void SetFinalWriteBinding(int pageIndex, AnimationPlayerPoseNativeWriteBinding binding)
        {
            if (pageIndex == 0)
                m_PageZeroFinalWriteBinding = binding;
            else
                m_PageOneFinalWriteBinding = binding;
        }

        void RequireFinalWriteBinding(in AnimationPlayerPoseNativeWriteBinding binding)
        {
            if (binding.CompletionIdentity == 0 ||
                binding.Range.PhysicalPlayerIndex != m_PhysicalPlayerIndex ||
                binding.Range.ContributionCapacity != m_ContributionCapacity ||
                binding.DenseLocalPoses.Length != m_BoneCount ||
                binding.DenseVelocities.Length != m_BoneCount ||
                binding.PoseParameters.Length != m_ParameterCount ||
                binding.Contributions.Length != m_ContributionCapacity ||
                binding.DenseContributionWeights.Length != checked(m_ContributionCapacity * m_BoneCount) ||
                binding.ContributionCount.Length != 1 ||
                binding.OutputWeight.Length != 1 ||
                binding.LeftFootFeatures.Length != 1 ||
                binding.RightFootFeatures.Length != 1 ||
                binding.HasFootFeatures.Length != 1 ||
                binding.Availability.Length != 1 ||
                binding.ContinuityIdentity.Length != 1 ||
                binding.InvalidReason.Length != 1 ||
                binding.CompletedAt.Length != 1)
            {
                throw new ArgumentException("Animation Slot Blend final write binding is invalid.");
            }
        }

        void RequireAlive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(AnimationSlotBlendPoseWorkspace));
        }

        static bool IsNormalized(float value) => float.IsFinite(value) && value >= 0f && value <= 1f;

        static NativeArray<T> Allocate<T>(int length) where T : struct =>
            new NativeArray<T>(length, Allocator.Persistent, NativeArrayOptions.ClearMemory);

        static void Clear<T>(NativeArray<T> values) where T : struct =>
            ClearRange(values, 0, values.Length);

        static void ClearRange<T>(NativeArray<T> values, int offset, int count) where T : struct
        {
            for (int i = 0; i < count; i++)
                values[offset + i] = default;
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            DisposeOwnedArrays();
            m_PageZeroFinalWriteBinding = default;
            m_PageOneFinalWriteBinding = default;
            m_ActivePageIndex = -1;
            ClearPreparation();
            m_LastCommittedCompletionIdentity = 0;
            m_Disposed = true;
        }

        void DisposeOwnedArrays()
        {
            DisposeArray(ref m_ScratchFootFeatureAccumulators);
            DisposeArray(ref m_ScratchPoseWeightSums);
            DisposeArray(ref m_ScratchScaleVelocitySums);
            DisposeArray(ref m_ScratchAngularVelocitySums);
            DisposeArray(ref m_ScratchLinearVelocitySums);
            DisposeArray(ref m_ScratchScaleSums);
            DisposeArray(ref m_ScratchRotationSums);
            DisposeArray(ref m_ScratchPositionSums);
            DisposeArray(ref m_ScratchDenseContributionWeights);
            DisposeArray(ref m_ScratchContributions);
            DisposeArray(ref m_ScratchParameters);
            DisposeArray(ref m_ScratchVelocity);
            DisposeArray(ref m_ScratchPose);
            DisposeArray(ref m_ScratchState);
            DisposeArray(ref m_HistoryBoneOutputWeights);
            DisposeArray(ref m_HistoryParameters);
            DisposeArray(ref m_HistoryVelocities);
            DisposeArray(ref m_HistoryPoses);
            DisposeArray(ref m_HistoryStates);
            DisposeArray(ref m_StoredBoneOutputWeights);
            DisposeArray(ref m_StoredParameters);
            DisposeArray(ref m_StoredVelocity);
            DisposeArray(ref m_StoredPose);
            DisposeArray(ref m_StoredState);
            DisposeArray(ref m_PlanDenseBoneWeightWritten);
            DisposeArray(ref m_PlanEntryWritten);
            DisposeArray(ref m_PlanDenseBoneWeights);
            DisposeArray(ref m_PlanEntries);
            DisposeArray(ref m_PlanHeaders);
        }

        static void DisposeArray<T>(ref NativeArray<T> values) where T : struct
        {
            if (values.IsCreated)
                values.Dispose();
            values = default;
        }
    }
}
