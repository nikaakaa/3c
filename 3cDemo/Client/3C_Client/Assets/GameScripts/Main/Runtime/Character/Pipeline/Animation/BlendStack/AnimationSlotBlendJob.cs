using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Animations;
using static ThirdPersonCharacter.Pipeline.Animation.BlendStack.AnimationSlotBlendJobMath;

namespace ThirdPersonCharacter.Pipeline.Animation.BlendStack
{
    internal struct AnimationSlotBlendJob : IAnimationJob
    {
        const float WeightTolerance = 0.0001f;

        [ReadOnly]
        readonly AnimationSlotBlendFramePlan m_FramePlan;

        [ReadOnly]
        readonly NativeArray<AnimationLocalBonePose> m_SourceCurrentPose;
        [ReadOnly]
        readonly NativeArray<AnimationBlendBoneVelocity> m_SourceVelocity;
        [ReadOnly]
        readonly NativeArray<float> m_SourcePoseParameters;
        [ReadOnly]
        readonly NativeArray<byte> m_SourcePoseParameterAvailability;
        [ReadOnly]
        readonly NativeArray<AnimationFootFeatureSample> m_SourceLeftFootFeatures;
        [ReadOnly]
        readonly NativeArray<AnimationFootFeatureSample> m_SourceRightFootFeatures;
        [ReadOnly]
        readonly NativeArray<float> m_SourceVisualTimeScales;
        [ReadOnly]
        readonly NativeArray<byte> m_SourceHasFootFeatures;
        [ReadOnly]
        readonly NativeArray<ulong> m_SourceCompletedAt;
        [ReadOnly]
        readonly NativeArray<int> m_SourceProgramProducerIndices;

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

        [NativeDisableContainerSafetyRestriction]
        NativeSlice<AnimationLocalBonePose> m_FinalDenseLocalPoses;
        [NativeDisableContainerSafetyRestriction]
        NativeSlice<AnimationBlendBoneVelocity> m_FinalDenseVelocities;
        [NativeDisableContainerSafetyRestriction]
        NativeSlice<float> m_FinalPoseParameters;
        [NativeDisableContainerSafetyRestriction]
        NativeSlice<byte> m_FinalPoseParameterAvailability;
        [NativeDisableContainerSafetyRestriction]
        NativeSlice<AnimationPrimitivePoseContribution> m_FinalContributions;
        [NativeDisableContainerSafetyRestriction]
        NativeSlice<float> m_FinalDenseContributionWeights;
        [NativeDisableContainerSafetyRestriction]
        NativeSlice<int> m_FinalContributionCount;
        [NativeDisableContainerSafetyRestriction]
        NativeSlice<float> m_FinalOutputWeight;
        [NativeDisableContainerSafetyRestriction]
        NativeSlice<AnimationFootFeatureSample> m_FinalLeftFootFeatures;
        [NativeDisableContainerSafetyRestriction]
        NativeSlice<AnimationFootFeatureSample> m_FinalRightFootFeatures;
        [NativeDisableContainerSafetyRestriction]
        NativeSlice<byte> m_FinalHasFootFeatures;
        [NativeDisableContainerSafetyRestriction]
        NativeSlice<AnimationPoseAvailability> m_FinalAvailability;
        [NativeDisableContainerSafetyRestriction]
        NativeSlice<ulong> m_FinalContinuityIdentity;
        [NativeDisableContainerSafetyRestriction]
        NativeSlice<AnimationPoseNativeInvalidReason> m_FinalInvalidReason;
        [NativeDisableContainerSafetyRestriction]
        NativeSlice<ulong> m_FinalCompletedAt;

        readonly int m_BoneCount;
        readonly int m_ParameterCount;
        readonly int m_SourceCapacity;
        readonly int m_ContributionCapacity;
        readonly int m_PhysicalPlayerIndex;
        readonly ulong m_CompletionIdentity;

        internal AnimationSlotBlendJob(
            AnimationSlotBlendPoseWorkspaceBinding workspace,
            AnimationBlendSourcePoseNativeReadBinding sources)
        {
            AnimationSlotBlendFramePlan plan = workspace.FramePlan;
            plan.RequireValidLayout();
            AnimationSlotBlendFramePlanHeader header = plan.Header;
            AnimationPlayerPoseNativeWriteBinding final = workspace.FinalWriteBinding;

            RequirePlan(plan, sources.SourceCapacity);
            RequireSourceBinding(sources, header);
            RequireFinalBinding(final, header);
            RequireWorkspaceBinding(workspace, header);

            m_FramePlan = plan;

            m_SourceCurrentPose = sources.CurrentPose;
            m_SourceVelocity = sources.Velocity;
            m_SourcePoseParameters = sources.PoseParameters;
            m_SourcePoseParameterAvailability = sources.PoseParameterAvailability;
            m_SourceLeftFootFeatures = sources.LeftFootFeatures;
            m_SourceRightFootFeatures = sources.RightFootFeatures;
            m_SourceVisualTimeScales = sources.VisualTimeScales;
            m_SourceHasFootFeatures = sources.HasFootFeatures;
            m_SourceCompletedAt = sources.CompletedAt;
            m_SourceProgramProducerIndices = sources.ProgramProducerIndices;

            m_StoredState = workspace.StoredPose.State;
            m_StoredPose = workspace.StoredPose.DenseLocalPose;
            m_StoredVelocity = workspace.StoredPose.DenseVelocity;
            m_StoredParameters = workspace.StoredPose.PoseParameters;
            m_StoredBoneOutputWeights = workspace.StoredPose.DenseBoneOutputWeights;

            m_HistoryStates = workspace.History.States;
            m_HistoryPoses = workspace.History.DenseLocalPoses;
            m_HistoryVelocities = workspace.History.DenseVelocities;
            m_HistoryParameters = workspace.History.PoseParameters;
            m_HistoryBoneOutputWeights = workspace.History.DenseBoneOutputWeights;

            m_ScratchState = workspace.Scratch.State;
            m_ScratchPose = workspace.Scratch.DenseLocalPose;
            m_ScratchVelocity = workspace.Scratch.DenseVelocity;
            m_ScratchParameters = workspace.Scratch.PoseParameters;
            m_ScratchContributions = workspace.Scratch.Contributions;
            m_ScratchDenseContributionWeights = workspace.Scratch.DenseContributionWeights;
            m_ScratchPositionSums = workspace.Scratch.PositionSums;
            m_ScratchRotationSums = workspace.Scratch.RotationSums;
            m_ScratchScaleSums = workspace.Scratch.ScaleSums;
            m_ScratchLinearVelocitySums = workspace.Scratch.LinearVelocitySums;
            m_ScratchAngularVelocitySums = workspace.Scratch.AngularVelocitySums;
            m_ScratchScaleVelocitySums = workspace.Scratch.ScaleVelocitySums;
            m_ScratchPoseWeightSums = workspace.Scratch.PoseWeightSums;
            m_ScratchFootFeatureAccumulators = workspace.Scratch.FootFeatureAccumulators;

            m_FinalDenseLocalPoses = final.DenseLocalPoses;
            m_FinalDenseVelocities = final.DenseVelocities;
            m_FinalPoseParameters = final.PoseParameters;
            m_FinalPoseParameterAvailability = final.PoseParameterAvailability;
            m_FinalContributions = final.Contributions;
            m_FinalDenseContributionWeights = final.DenseContributionWeights;
            m_FinalContributionCount = final.ContributionCount;
            m_FinalOutputWeight = final.OutputWeight;
            m_FinalLeftFootFeatures = final.LeftFootFeatures;
            m_FinalRightFootFeatures = final.RightFootFeatures;
            m_FinalHasFootFeatures = final.HasFootFeatures;
            m_FinalAvailability = final.Availability;
            m_FinalContinuityIdentity = final.ContinuityIdentity;
            m_FinalInvalidReason = final.InvalidReason;
            m_FinalCompletedAt = final.CompletedAt;

            m_BoneCount = header.BoneCount;
            m_ParameterCount = header.ParameterCount;
            m_SourceCapacity = sources.SourceCapacity;
            m_ContributionCapacity = header.ContributionCapacity;
            m_PhysicalPlayerIndex = header.PhysicalPlayerIndex;
            m_CompletionIdentity = header.CompletionIdentity;
        }

        public void ProcessAnimation(AnimationStream stream)
        {
            ClearScratch();
            if (m_FinalCompletedAt[0] == m_CompletionIdentity)
            {
                PublishInvalid(AnimationPoseNativeInvalidReason.SlotPlanInvalid);
                return;
            }

            float deltaSeconds = stream.deltaTime;
            if (!float.IsFinite(deltaSeconds) || deltaSeconds < 0f)
            {
                PublishInvalid(AnimationPoseNativeInvalidReason.SlotVelocityInvalid);
                return;
            }

            AnimationPoseNativeInvalidReason reason = ValidateRuntimeInputs();
            if (m_FramePlan.Header.Availability == AnimationPoseAvailability.Invalid)
            {
                PublishInvalid(m_FramePlan.Header.InvalidReason);
                return;
            }
            if (reason != AnimationPoseNativeInvalidReason.None)
            {
                PublishInvalid(reason);
                return;
            }

            AnimationSlotBlendFramePlanHeader header = m_FramePlan.Header;
            if (header.Availability == AnimationPoseAvailability.NoPose)
            {
                ClearStored();
                CommitNoPoseHistory();
                PublishNoPose();
                return;
            }

            reason = PrepareScratchContributions();
            if (reason == AnimationPoseNativeInvalidReason.None)
            {
                reason = BlendCrossFade(deltaSeconds);
            }
            if (reason == AnimationPoseNativeInvalidReason.None)
                reason = BlendFootFeatures();
            if (reason == AnimationPoseNativeInvalidReason.None)
                reason = ValidateScratchOutput();
            if (reason != AnimationPoseNativeInvalidReason.None)
            {
                PublishInvalid(reason);
                return;
            }

            CommitPersistentState();
            CommitPoseHistory();
            PublishPose();
        }

        public void ProcessRootMotion(AnimationStream stream)
        {
        }

        AnimationPoseNativeInvalidReason ValidateRuntimeInputs()
        {
            AnimationSlotBlendFramePlanHeader header = m_FramePlan.Header;
            if (header.CompletionIdentity != m_CompletionIdentity ||
                header.PhysicalPlayerIndex != m_PhysicalPlayerIndex ||
                m_SourceCompletedAt.Length != m_SourceCapacity)
                return AnimationPoseNativeInvalidReason.SlotPlanInvalid;

            if (header.HistoryReadPageIndex >= 0 &&
                !IsValidHistory(header.HistoryReadPageIndex, header.HistoryCompletionIdentity))
                return AnimationPoseNativeInvalidReason.SlotPlanInvalid;

            int storedIndex = -1;
            for (int contributionIndex = 0; contributionIndex < header.ContributionCount; contributionIndex++)
            {
                AnimationSlotBlendFramePlanEntry entry = m_FramePlan.GetEntry(contributionIndex);
                if (entry.Kind == AnimationPoseContributionKind.Live)
                {
                    if (!IsValidSource(entry))
                        return AnimationPoseNativeInvalidReason.SourceIncomplete;
                }
                else if (entry.Kind == AnimationPoseContributionKind.Stored)
                {
                    storedIndex = contributionIndex;
                }
                else
                {
                    return AnimationPoseNativeInvalidReason.SlotContributionInvalid;
                }
            }

            if (header.Availability == AnimationPoseAvailability.NoPose)
                return header.ContributionCount == 0 && header.OutputWeight == 0f
                    ? AnimationPoseNativeInvalidReason.None
                    : AnimationPoseNativeInvalidReason.SlotPlanInvalid;

            if (header.Kind == AnimationSlotBlendFramePlanKind.StoredCapture)
                return storedIndex >= 0 && header.HistoryReadPageIndex >= 0
                    ? AnimationPoseNativeInvalidReason.None
                    : AnimationPoseNativeInvalidReason.SlotPlanInvalid;

            return storedIndex < 0 || IsValidStored(m_FramePlan.GetEntry(storedIndex))
                ? AnimationPoseNativeInvalidReason.None
                : AnimationPoseNativeInvalidReason.SlotPoseInvalid;
        }

        bool IsValidSource(AnimationSlotBlendFramePlanEntry entry)
        {
            int sourceIndex = entry.SourceCaptureIndex;
            if ((uint)sourceIndex >= (uint)m_SourceCapacity ||
                m_SourceCompletedAt[sourceIndex] != m_CompletionIdentity ||
                m_SourceProgramProducerIndices[sourceIndex] != entry.ProgramProducerIndex ||
                m_SourceHasFootFeatures[sourceIndex] > 1 ||
                !float.IsFinite(m_SourceVisualTimeScales[sourceIndex]) || m_SourceVisualTimeScales[sourceIndex] < 0f)
            {
                return false;
            }

            bool hasFoot = m_SourceHasFootFeatures[sourceIndex] != 0;
            if (hasFoot != (m_SourceLeftFootFeatures[sourceIndex].IsValid && m_SourceRightFootFeatures[sourceIndex].IsValid) ||
                hasFoot && (!IsValidFoot(m_SourceLeftFootFeatures[sourceIndex]) || !IsValidFoot(m_SourceRightFootFeatures[sourceIndex])))
            {
                return false;
            }

            int poseOffset = checked(sourceIndex * m_BoneCount);
            for (int boneIndex = 0; boneIndex < m_BoneCount; boneIndex++)
            {
                if (!m_SourceCurrentPose[poseOffset + boneIndex].IsValid ||
                    !m_SourceVelocity[poseOffset + boneIndex].IsValid)
                {
                    return false;
                }
            }
            int parameterOffset = checked(sourceIndex * m_ParameterCount);
            for (int parameterIndex = 0; parameterIndex < m_ParameterCount; parameterIndex++)
            {
                if (!float.IsFinite(m_SourcePoseParameters[parameterOffset + parameterIndex]) ||
                    m_SourcePoseParameterAvailability[parameterOffset + parameterIndex] != 1)
                    return false;
            }
            return true;
        }

        bool IsValidHistory(int pageIndex, ulong completionIdentity)
        {
            if ((uint)pageIndex > 1u)
                return false;
            AnimationSlotBlendHistoryNativeState state = m_HistoryStates[pageIndex];
            if (state.Availability != AnimationPoseAvailability.Pose ||
                state.CompletionIdentity != completionIdentity || state.ContinuityIdentity == 0 ||
                !IsNormalized(state.OutputWeight) || state.HasFootFeatures > 1 ||
                state.HasFootFeatures != 0 && (!IsValidFoot(state.LeftFootFeatures) || !IsValidFoot(state.RightFootFeatures)) ||
                state.HasFootFeatures == 0 && (state.LeftFootFeatures.IsValid || state.RightFootFeatures.IsValid))
            {
                return false;
            }
            int poseOffset = checked(pageIndex * m_BoneCount);
            for (int boneIndex = 0; boneIndex < m_BoneCount; boneIndex++)
            {
                if (!m_HistoryPoses[poseOffset + boneIndex].IsValid ||
                    !m_HistoryVelocities[poseOffset + boneIndex].IsValid ||
                    !IsNormalized(m_HistoryBoneOutputWeights[poseOffset + boneIndex]))
                {
                    return false;
                }
            }
            int parameterOffset = checked(pageIndex * m_ParameterCount);
            for (int parameterIndex = 0; parameterIndex < m_ParameterCount; parameterIndex++)
            {
                if (!float.IsFinite(m_HistoryParameters[parameterOffset + parameterIndex]))
                    return false;
            }
            return true;
        }

        bool IsValidStored(AnimationSlotBlendFramePlanEntry entry)
        {
            AnimationSlotBlendStoredPoseNativeState state = m_StoredState[0];
            if (state.Active != 1 || state.CapturedAtCompletionIdentity == 0 ||
                state.SourceHistoryCompletionIdentity == 0 ||
                state.ContributionContinuityIdentity != entry.ContributionContinuityIdentity ||
                !IsNormalized(state.OutputWeight) || state.HasFootFeatures > 1 ||
                state.HasFootFeatures != 0 && (!IsValidFoot(state.LeftFootFeatures) || !IsValidFoot(state.RightFootFeatures)) ||
                state.HasFootFeatures == 0 && (state.LeftFootFeatures.IsValid || state.RightFootFeatures.IsValid))
            {
                return false;
            }
            for (int boneIndex = 0; boneIndex < m_BoneCount; boneIndex++)
            {
                if (!m_StoredPose[boneIndex].IsValid || !m_StoredVelocity[boneIndex].IsValid ||
                    !IsNormalized(m_StoredBoneOutputWeights[boneIndex]))
                {
                    return false;
                }
            }
            for (int parameterIndex = 0; parameterIndex < m_ParameterCount; parameterIndex++)
            {
                if (!float.IsFinite(m_StoredParameters[parameterIndex]))
                    return false;
            }
            return true;
        }

        AnimationPoseNativeInvalidReason PrepareScratchContributions()
        {
            AnimationSlotBlendFramePlanHeader header = m_FramePlan.Header;
            for (int contributionIndex = 0; contributionIndex < header.ContributionCount; contributionIndex++)
            {
                AnimationSlotBlendFramePlanEntry entry = m_FramePlan.GetEntry(contributionIndex);
                m_ScratchContributions[contributionIndex] = new AnimationPrimitivePoseContribution(
                    header.PhysicalPlayerIndex,
                    entry.PhysicalSourceIndex,
                    entry.PhysicalSourceGeneration,
                    entry.Kind,
                    entry.ProgramProducerIndex,
                    entry.ContributionContinuityIdentity,
                    entry.ScalarWeight,
                    entry.LeftFootWeight,
                    entry.RightFootWeight);
                for (int boneIndex = 0; boneIndex < m_BoneCount; boneIndex++)
                {
                    float weight = m_FramePlan.GetDenseBoneWeight(contributionIndex, boneIndex);
                    if (!IsNormalized(weight))
                        return AnimationPoseNativeInvalidReason.SlotContributionInvalid;
                    m_ScratchDenseContributionWeights[contributionIndex * m_BoneCount + boneIndex] = weight;
                }
            }
            return AnimationPoseNativeInvalidReason.None;
        }

        AnimationPoseNativeInvalidReason BlendCrossFade(float deltaSeconds)
        {
            AnimationSlotBlendFramePlanHeader header = m_FramePlan.Header;
            for (int boneIndex = 0; boneIndex < m_BoneCount; boneIndex++)
            {
                Vector3 positionSum = Vector3.zero;
                Vector3 scaleSum = Vector3.zero;
                Vector3 linearVelocitySum = Vector3.zero;
                Vector3 angularVelocitySum = Vector3.zero;
                Vector3 scaleVelocitySum = Vector3.zero;
                Vector4 rotationSum = Vector4.zero;
                Quaternion rotationReference = default;
                bool hasRotationReference = false;
                float poseWeight = 0f;

                for (int contributionIndex = 0; contributionIndex < header.ContributionCount; contributionIndex++)
                {
                    float weight = m_FramePlan.GetDenseBoneWeight(contributionIndex, boneIndex);
                    if (weight <= 0f)
                        continue;
                    AnimationSlotBlendFramePlanEntry entry = m_FramePlan.GetEntry(contributionIndex);
                    if (!TryGetCrossFadeBone(entry, boneIndex, out AnimationLocalBonePose pose, out AnimationBlendBoneVelocity velocity))
                        return AnimationPoseNativeInvalidReason.SlotPoseInvalid;
                    if (!hasRotationReference)
                    {
                        rotationReference = pose.Rotation;
                        hasRotationReference = true;
                    }
                    positionSum += pose.Position * weight;
                    scaleSum += pose.Scale * weight;
                    linearVelocitySum += velocity.Linear * weight;
                    angularVelocitySum += velocity.Angular * weight;
                    scaleVelocitySum += velocity.Scale * weight;
                    rotationSum += AnimationPoseMath.AlignAndScale(pose.Rotation, rotationReference, weight);
                    poseWeight += weight;
                }

                m_ScratchPositionSums[boneIndex] = positionSum;
                m_ScratchRotationSums[boneIndex] = rotationSum;
                m_ScratchScaleSums[boneIndex] = scaleSum;
                m_ScratchLinearVelocitySums[boneIndex] = linearVelocitySum;
                m_ScratchAngularVelocitySums[boneIndex] = angularVelocitySum;
                m_ScratchScaleVelocitySums[boneIndex] = scaleVelocitySum;
                m_ScratchPoseWeightSums[boneIndex] = poseWeight;
                if (poseWeight <= 0f)
                {
                    if (!TryGetCrossFadeCarrierBone(
                            boneIndex,
                            out AnimationLocalBonePose carrierPose,
                            out AnimationBlendBoneVelocity carrierVelocity))
                    {
                        return AnimationPoseNativeInvalidReason.SlotPoseInvalid;
                    }
                    m_ScratchPose[boneIndex] = carrierPose;
                    m_ScratchVelocity[boneIndex] = carrierVelocity;
                    continue;
                }
                if (!hasRotationReference || !TryResolveWeightedPose(
                        positionSum,
                        rotationSum,
                        scaleSum,
                        poseWeight,
                        out AnimationLocalBonePose outputPose))
                {
                    return AnimationPoseNativeInvalidReason.SlotPoseInvalid;
                }
                m_ScratchPose[boneIndex] = outputPose;
                if (!TryCreateVelocity(
                        linearVelocitySum / poseWeight,
                        angularVelocitySum / poseWeight,
                        scaleVelocitySum / poseWeight,
                        out AnimationBlendBoneVelocity blendedVelocity))
                {
                    return AnimationPoseNativeInvalidReason.SlotVelocityInvalid;
                }
                m_ScratchVelocity[boneIndex] = blendedVelocity;
            }

            if (header.HistoryReadPageIndex >= 0 && deltaSeconds > 0f)
            {
                int historyOffset = checked(header.HistoryReadPageIndex * m_BoneCount);
                for (int boneIndex = 0; boneIndex < m_BoneCount; boneIndex++)
                {
                    if (!TryDifferentiate(
                            m_HistoryPoses[historyOffset + boneIndex],
                            m_ScratchPose[boneIndex],
                            deltaSeconds,
                            out AnimationBlendBoneVelocity velocity))
                    {
                        return AnimationPoseNativeInvalidReason.SlotVelocityInvalid;
                    }
                    m_ScratchVelocity[boneIndex] = velocity;
                }
            }
            else
            {
                for (int boneIndex = 0; boneIndex < m_BoneCount; boneIndex++)
                    m_ScratchVelocity[boneIndex] = default;
            }

            for (int parameterIndex = 0; parameterIndex < m_ParameterCount; parameterIndex++)
            {
                float sum = 0f;
                for (int contributionIndex = 0; contributionIndex < header.ContributionCount; contributionIndex++)
                {
                    AnimationSlotBlendFramePlanEntry entry = m_FramePlan.GetEntry(contributionIndex);
                    if (!TryGetCrossFadeParameter(entry, parameterIndex, out float value))
                        return AnimationPoseNativeInvalidReason.SlotParameterInvalid;
                    sum += value * entry.ScalarWeight;
                }
                float result = header.OutputWeight > 0f ? sum / header.OutputWeight : 0f;
                if (!float.IsFinite(result))
                    return AnimationPoseNativeInvalidReason.SlotParameterInvalid;
                m_ScratchParameters[parameterIndex] = result;
            }
            return AnimationPoseNativeInvalidReason.None;
        }

        AnimationPoseNativeInvalidReason BlendFootFeatures()
        {
            AnimationSlotBlendFramePlanHeader header = m_FramePlan.Header;
            float leftWeight = 0f;
            Vector3 leftVelocity = Vector3.zero;
            float leftHeight = 0f;
            float leftPlant = 0f;
            float leftLandingConfidence = 0f;
            float leftLandingWeight = 0f;
            float leftLandingDelay = 0f;
            Vector2 leftLandingOffset = Vector2.zero;
            float rightWeight = 0f;
            Vector3 rightVelocity = Vector3.zero;
            float rightHeight = 0f;
            float rightPlant = 0f;
            float rightLandingConfidence = 0f;
            float rightLandingWeight = 0f;
            float rightLandingDelay = 0f;
            Vector2 rightLandingOffset = Vector2.zero;
            bool leftValid = true;
            bool rightValid = true;

            for (int contributionIndex = 0; contributionIndex < header.ContributionCount; contributionIndex++)
            {
                AnimationSlotBlendFramePlanEntry entry = m_FramePlan.GetEntry(contributionIndex);
                if (!TryGetFootSource(
                        entry,
                        out AnimationFootFeatureSample left,
                        out AnimationFootFeatureSample right,
                        out bool hasFeatures,
                        out float visualTimeScale))
                {
                    return AnimationPoseNativeInvalidReason.SlotFootFeatureInvalid;
                }

                if (entry.LeftFootWeight > 0f)
                {
                    leftValid &= hasFeatures;
                    if (hasFeatures && !AccumulateFoot(
                            left,
                            entry.LeftFootWeight,
                            visualTimeScale,
                            ref leftWeight,
                            ref leftVelocity,
                            ref leftHeight,
                            ref leftPlant,
                            ref leftLandingConfidence,
                            ref leftLandingWeight,
                            ref leftLandingDelay,
                            ref leftLandingOffset))
                    {
                        return AnimationPoseNativeInvalidReason.SlotFootFeatureInvalid;
                    }
                }
                if (entry.RightFootWeight > 0f)
                {
                    rightValid &= hasFeatures;
                    if (hasFeatures && !AccumulateFoot(
                            right,
                            entry.RightFootWeight,
                            visualTimeScale,
                            ref rightWeight,
                            ref rightVelocity,
                            ref rightHeight,
                            ref rightPlant,
                            ref rightLandingConfidence,
                            ref rightLandingWeight,
                            ref rightLandingDelay,
                            ref rightLandingOffset))
                    {
                        return AnimationPoseNativeInvalidReason.SlotFootFeatureInvalid;
                    }
                }
            }

            bool hasFootFeatures = leftValid && rightValid && leftWeight > 0f && rightWeight > 0f;
            AnimationFootFeatureSample leftResult = default;
            AnimationFootFeatureSample rightResult = default;
            if (hasFootFeatures &&
                (!TryResolveFoot(
                     leftWeight,
                     leftVelocity,
                     leftHeight,
                     leftPlant,
                     leftLandingConfidence,
                     leftLandingWeight,
                     leftLandingDelay,
                     leftLandingOffset,
                     out leftResult) ||
                 !TryResolveFoot(
                     rightWeight,
                     rightVelocity,
                     rightHeight,
                     rightPlant,
                     rightLandingConfidence,
                     rightLandingWeight,
                     rightLandingDelay,
                     rightLandingOffset,
                     out rightResult)))
            {
                return AnimationPoseNativeInvalidReason.SlotFootFeatureInvalid;
            }

            m_ScratchState[0] = new AnimationSlotBlendScratchNativeState
            {
                Availability = AnimationPoseAvailability.Pose,
                InvalidReason = AnimationPoseNativeInvalidReason.None,
                HasFootFeatures = hasFootFeatures ? (byte)1 : (byte)0,
                ContributionCount = header.ContributionCount,
                ContinuityIdentity = header.ContinuityIdentity,
                OutputWeight = header.OutputWeight,
                LeftFootFeatures = leftResult,
                RightFootFeatures = rightResult
            };
            return AnimationPoseNativeInvalidReason.None;
        }

        AnimationPoseNativeInvalidReason ValidateScratchOutput()
        {
            AnimationSlotBlendScratchNativeState state = m_ScratchState[0];
            if (state.Availability != AnimationPoseAvailability.Pose ||
                state.InvalidReason != AnimationPoseNativeInvalidReason.None ||
                state.ContributionCount != m_FramePlan.ContributionCount ||
                state.ContinuityIdentity != m_FramePlan.Header.ContinuityIdentity ||
                !IsNormalized(state.OutputWeight) || state.HasFootFeatures > 1 ||
                state.HasFootFeatures != 0 && (!IsValidFoot(state.LeftFootFeatures) || !IsValidFoot(state.RightFootFeatures)) ||
                state.HasFootFeatures == 0 && (state.LeftFootFeatures.IsValid || state.RightFootFeatures.IsValid))
            {
                return AnimationPoseNativeInvalidReason.SlotPoseInvalid;
            }
            bool hasOutputWeight = state.OutputWeight > 0f;
            for (int boneIndex = 0; boneIndex < m_BoneCount; boneIndex++)
            {
                if (!m_ScratchPose[boneIndex].IsValid)
                    return AnimationPoseNativeInvalidReason.SlotPoseInvalid;
                if (!m_ScratchVelocity[boneIndex].IsValid)
                    return AnimationPoseNativeInvalidReason.SlotVelocityInvalid;
                if (!IsNormalized(m_ScratchPoseWeightSums[boneIndex]))
                    return AnimationPoseNativeInvalidReason.SlotContributionInvalid;
                hasOutputWeight |= m_ScratchPoseWeightSums[boneIndex] > 0f;
            }
            if (!hasOutputWeight)
                return AnimationPoseNativeInvalidReason.SlotContributionInvalid;
            for (int parameterIndex = 0; parameterIndex < m_ParameterCount; parameterIndex++)
            {
                if (!float.IsFinite(m_ScratchParameters[parameterIndex]))
                    return AnimationPoseNativeInvalidReason.SlotParameterInvalid;
            }
            return AnimationPoseNativeInvalidReason.None;
        }

        bool TryGetCrossFadeBone(
            AnimationSlotBlendFramePlanEntry entry,
            int boneIndex,
            out AnimationLocalBonePose pose,
            out AnimationBlendBoneVelocity velocity)
        {
            if (entry.Kind == AnimationPoseContributionKind.Live)
            {
                int offset = checked(entry.SourceCaptureIndex * m_BoneCount + boneIndex);
                pose = m_SourceCurrentPose[offset];
                velocity = m_SourceVelocity[offset];
                return pose.IsValid && velocity.IsValid;
            }
            if (entry.Kind == AnimationPoseContributionKind.Stored)
            {
                if (m_FramePlan.Header.Kind == AnimationSlotBlendFramePlanKind.StoredCapture)
                {
                    int offset = checked(m_FramePlan.Header.HistoryReadPageIndex * m_BoneCount + boneIndex);
                    pose = m_HistoryPoses[offset];
                    velocity = m_HistoryVelocities[offset];
                }
                else
                {
                    pose = m_StoredPose[boneIndex];
                    velocity = m_StoredVelocity[boneIndex];
                }
                return pose.IsValid && velocity.IsValid;
            }
            pose = default;
            velocity = default;
            return false;
        }

        bool TryGetCrossFadeCarrierBone(
            int boneIndex,
            out AnimationLocalBonePose pose,
            out AnimationBlendBoneVelocity velocity)
        {
            for (int contributionIndex = m_FramePlan.ContributionCount - 1; contributionIndex >= 0; contributionIndex--)
            {
                if (TryGetCrossFadeBone(m_FramePlan.GetEntry(contributionIndex), boneIndex, out pose, out velocity))
                    return true;
            }
            pose = default;
            velocity = default;
            return false;
        }

        bool TryGetCrossFadeParameter(
            AnimationSlotBlendFramePlanEntry entry,
            int parameterIndex,
            out float value)
        {
            if (entry.Kind == AnimationPoseContributionKind.Live)
            {
                value = m_SourcePoseParameters[entry.SourceCaptureIndex * m_ParameterCount + parameterIndex];
                return float.IsFinite(value);
            }
            if (entry.Kind == AnimationPoseContributionKind.Stored)
            {
                value = m_FramePlan.Header.Kind == AnimationSlotBlendFramePlanKind.StoredCapture
                    ? m_HistoryParameters[m_FramePlan.Header.HistoryReadPageIndex * m_ParameterCount + parameterIndex]
                    : m_StoredParameters[parameterIndex];
                return float.IsFinite(value);
            }
            value = 0f;
            return false;
        }

        bool TryGetFootSource(
            AnimationSlotBlendFramePlanEntry entry,
            out AnimationFootFeatureSample left,
            out AnimationFootFeatureSample right,
            out bool hasFeatures,
            out float visualTimeScale)
        {
            if (entry.Kind == AnimationPoseContributionKind.Live)
            {
                int sourceIndex = entry.SourceCaptureIndex;
                left = m_SourceLeftFootFeatures[sourceIndex];
                right = m_SourceRightFootFeatures[sourceIndex];
                hasFeatures = m_SourceHasFootFeatures[sourceIndex] != 0;
                visualTimeScale = m_SourceVisualTimeScales[sourceIndex];
                return true;
            }
            if (entry.Kind == AnimationPoseContributionKind.Stored)
            {
                if (m_FramePlan.Header.Kind == AnimationSlotBlendFramePlanKind.StoredCapture)
                {
                    AnimationSlotBlendHistoryNativeState history = m_HistoryStates[m_FramePlan.Header.HistoryReadPageIndex];
                    left = history.LeftFootFeatures;
                    right = history.RightFootFeatures;
                    hasFeatures = history.HasFootFeatures != 0;
                }
                else
                {
                    AnimationSlotBlendStoredPoseNativeState stored = m_StoredState[0];
                    left = stored.LeftFootFeatures;
                    right = stored.RightFootFeatures;
                    hasFeatures = stored.HasFootFeatures != 0;
                }
                visualTimeScale = 1f;
                return true;
            }
            left = default;
            right = default;
            hasFeatures = false;
            visualTimeScale = 0f;
            return false;
        }

        void CommitPersistentState()
        {
            int storedIndex = FindContribution(AnimationPoseContributionKind.Stored);
            if (m_FramePlan.Header.Kind == AnimationSlotBlendFramePlanKind.StoredCapture)
                CommitStoredCapture(m_FramePlan.GetEntry(storedIndex));
            else if (storedIndex < 0)
                ClearStored();
        }

        void CommitStoredCapture(AnimationSlotBlendFramePlanEntry storedEntry)
        {
            int historyPage = m_FramePlan.Header.HistoryReadPageIndex;
            int historyPoseOffset = checked(historyPage * m_BoneCount);
            int historyParameterOffset = checked(historyPage * m_ParameterCount);
            for (int boneIndex = 0; boneIndex < m_BoneCount; boneIndex++)
            {
                m_StoredPose[boneIndex] = m_HistoryPoses[historyPoseOffset + boneIndex];
                m_StoredVelocity[boneIndex] = m_HistoryVelocities[historyPoseOffset + boneIndex];
                m_StoredBoneOutputWeights[boneIndex] = m_HistoryBoneOutputWeights[historyPoseOffset + boneIndex];
            }
            for (int parameterIndex = 0; parameterIndex < m_ParameterCount; parameterIndex++)
                m_StoredParameters[parameterIndex] = m_HistoryParameters[historyParameterOffset + parameterIndex];
            AnimationSlotBlendHistoryNativeState history = m_HistoryStates[historyPage];
            m_StoredState[0] = new AnimationSlotBlendStoredPoseNativeState
            {
                Active = 1,
                HasFootFeatures = history.HasFootFeatures,
                CapturedAtCompletionIdentity = m_CompletionIdentity,
                SourceHistoryCompletionIdentity = history.CompletionIdentity,
                ContributionContinuityIdentity = storedEntry.ContributionContinuityIdentity,
                OutputWeight = history.OutputWeight,
                LeftFootFeatures = history.LeftFootFeatures,
                RightFootFeatures = history.RightFootFeatures
            };
        }

        void CommitPoseHistory()
        {
            AnimationSlotBlendFramePlanHeader header = m_FramePlan.Header;
            int page = header.HistoryWritePageIndex;
            int poseOffset = checked(page * m_BoneCount);
            int parameterOffset = checked(page * m_ParameterCount);
            for (int boneIndex = 0; boneIndex < m_BoneCount; boneIndex++)
            {
                m_HistoryPoses[poseOffset + boneIndex] = m_ScratchPose[boneIndex];
                m_HistoryVelocities[poseOffset + boneIndex] = m_ScratchVelocity[boneIndex];
                m_HistoryBoneOutputWeights[poseOffset + boneIndex] = m_ScratchPoseWeightSums[boneIndex];
            }
            for (int parameterIndex = 0; parameterIndex < m_ParameterCount; parameterIndex++)
                m_HistoryParameters[parameterOffset + parameterIndex] = m_ScratchParameters[parameterIndex];
            AnimationSlotBlendScratchNativeState scratch = m_ScratchState[0];
            m_HistoryStates[page] = new AnimationSlotBlendHistoryNativeState
            {
                Availability = AnimationPoseAvailability.Pose,
                HasFootFeatures = scratch.HasFootFeatures,
                CompletionIdentity = m_CompletionIdentity,
                ContinuityIdentity = header.ContinuityIdentity,
                OutputWeight = header.OutputWeight,
                LeftFootFeatures = scratch.LeftFootFeatures,
                RightFootFeatures = scratch.RightFootFeatures
            };
        }

        void CommitNoPoseHistory()
        {
            AnimationSlotBlendFramePlanHeader header = m_FramePlan.Header;
            int page = header.HistoryWritePageIndex;
            int poseOffset = checked(page * m_BoneCount);
            int parameterOffset = checked(page * m_ParameterCount);
            for (int boneIndex = 0; boneIndex < m_BoneCount; boneIndex++)
            {
                m_HistoryPoses[poseOffset + boneIndex] = default;
                m_HistoryVelocities[poseOffset + boneIndex] = default;
                m_HistoryBoneOutputWeights[poseOffset + boneIndex] = 0f;
            }
            for (int parameterIndex = 0; parameterIndex < m_ParameterCount; parameterIndex++)
                m_HistoryParameters[parameterOffset + parameterIndex] = 0f;
            m_HistoryStates[page] = new AnimationSlotBlendHistoryNativeState
            {
                Availability = AnimationPoseAvailability.NoPose,
                CompletionIdentity = m_CompletionIdentity,
                ContinuityIdentity = header.ContinuityIdentity
            };
        }

        void PublishPose()
        {
            AnimationSlotBlendFramePlanHeader header = m_FramePlan.Header;
            AnimationSlotBlendScratchNativeState scratch = m_ScratchState[0];
            for (int boneIndex = 0; boneIndex < m_BoneCount; boneIndex++)
            {
                m_FinalDenseLocalPoses[boneIndex] = m_ScratchPose[boneIndex];
                m_FinalDenseVelocities[boneIndex] = m_ScratchVelocity[boneIndex];
            }
            for (int parameterIndex = 0; parameterIndex < m_ParameterCount; parameterIndex++)
            {
                m_FinalPoseParameters[parameterIndex] = m_ScratchParameters[parameterIndex];
                m_FinalPoseParameterAvailability[parameterIndex] = 1;
            }
            for (int contributionIndex = 0; contributionIndex < m_ContributionCapacity; contributionIndex++)
            {
                bool active = contributionIndex < header.ContributionCount;
                m_FinalContributions[contributionIndex] = active ? m_ScratchContributions[contributionIndex] : default;
                for (int boneIndex = 0; boneIndex < m_BoneCount; boneIndex++)
                {
                    int index = contributionIndex * m_BoneCount + boneIndex;
                    m_FinalDenseContributionWeights[index] = active ? m_ScratchDenseContributionWeights[index] : 0f;
                }
            }
            m_FinalContributionCount[0] = header.ContributionCount;
            m_FinalOutputWeight[0] = header.OutputWeight;
            m_FinalLeftFootFeatures[0] = scratch.LeftFootFeatures;
            m_FinalRightFootFeatures[0] = scratch.RightFootFeatures;
            m_FinalHasFootFeatures[0] = scratch.HasFootFeatures;
            m_FinalAvailability[0] = AnimationPoseAvailability.Pose;
            m_FinalContinuityIdentity[0] = header.ContinuityIdentity;
            m_FinalInvalidReason[0] = AnimationPoseNativeInvalidReason.None;
            m_FinalCompletedAt[0] = m_CompletionIdentity;
        }

        void PublishNoPose()
        {
            ClearFinalPayload();
            AnimationSlotBlendFramePlanHeader header = m_FramePlan.Header;
            m_ScratchState[0] = new AnimationSlotBlendScratchNativeState
            {
                Availability = AnimationPoseAvailability.NoPose,
                InvalidReason = AnimationPoseNativeInvalidReason.None,
                ContinuityIdentity = header.ContinuityIdentity
            };
            m_FinalAvailability[0] = AnimationPoseAvailability.NoPose;
            m_FinalContinuityIdentity[0] = header.ContinuityIdentity;
            m_FinalInvalidReason[0] = AnimationPoseNativeInvalidReason.None;
            m_FinalCompletedAt[0] = m_CompletionIdentity;
        }

        void PublishInvalid(AnimationPoseNativeInvalidReason reason)
        {
            ClearFinalPayload();
            ulong continuityIdentity = m_FramePlan.Header.ContinuityIdentity;
            m_ScratchState[0] = new AnimationSlotBlendScratchNativeState
            {
                Availability = AnimationPoseAvailability.Invalid,
                InvalidReason = reason,
                ContinuityIdentity = continuityIdentity
            };
            m_FinalAvailability[0] = AnimationPoseAvailability.Invalid;
            m_FinalContinuityIdentity[0] = continuityIdentity;
            m_FinalInvalidReason[0] = reason;
            m_FinalCompletedAt[0] = m_CompletionIdentity;
        }

        void ClearFinalPayload()
        {
            for (int boneIndex = 0; boneIndex < m_BoneCount; boneIndex++)
            {
                m_FinalDenseLocalPoses[boneIndex] = default;
                m_FinalDenseVelocities[boneIndex] = default;
            }
            for (int parameterIndex = 0; parameterIndex < m_ParameterCount; parameterIndex++)
            {
                m_FinalPoseParameters[parameterIndex] = 0f;
                m_FinalPoseParameterAvailability[parameterIndex] = 0;
            }
            for (int contributionIndex = 0; contributionIndex < m_ContributionCapacity; contributionIndex++)
            {
                m_FinalContributions[contributionIndex] = default;
                for (int boneIndex = 0; boneIndex < m_BoneCount; boneIndex++)
                    m_FinalDenseContributionWeights[contributionIndex * m_BoneCount + boneIndex] = 0f;
            }
            m_FinalContributionCount[0] = 0;
            m_FinalOutputWeight[0] = 0f;
            m_FinalLeftFootFeatures[0] = default;
            m_FinalRightFootFeatures[0] = default;
            m_FinalHasFootFeatures[0] = 0;
        }

        void ClearScratch()
        {
            m_ScratchState[0] = default;
            for (int boneIndex = 0; boneIndex < m_BoneCount; boneIndex++)
            {
                m_ScratchPose[boneIndex] = default;
                m_ScratchVelocity[boneIndex] = default;
                m_ScratchPositionSums[boneIndex] = Vector3.zero;
                m_ScratchRotationSums[boneIndex] = Vector4.zero;
                m_ScratchScaleSums[boneIndex] = Vector3.zero;
                m_ScratchLinearVelocitySums[boneIndex] = Vector3.zero;
                m_ScratchAngularVelocitySums[boneIndex] = Vector3.zero;
                m_ScratchScaleVelocitySums[boneIndex] = Vector3.zero;
                m_ScratchPoseWeightSums[boneIndex] = 0f;
            }
            for (int parameterIndex = 0; parameterIndex < m_ParameterCount; parameterIndex++)
                m_ScratchParameters[parameterIndex] = 0f;
            for (int contributionIndex = 0; contributionIndex < m_ContributionCapacity; contributionIndex++)
            {
                m_ScratchContributions[contributionIndex] = default;
                for (int boneIndex = 0; boneIndex < m_BoneCount; boneIndex++)
                    m_ScratchDenseContributionWeights[contributionIndex * m_BoneCount + boneIndex] = 0f;
            }
            m_ScratchFootFeatureAccumulators[0] = default;
            m_ScratchFootFeatureAccumulators[1] = default;
        }

        void ClearStored()
        {
            m_StoredState[0] = default;
            for (int boneIndex = 0; boneIndex < m_BoneCount; boneIndex++)
            {
                m_StoredPose[boneIndex] = default;
                m_StoredVelocity[boneIndex] = default;
                m_StoredBoneOutputWeights[boneIndex] = 0f;
            }
            for (int parameterIndex = 0; parameterIndex < m_ParameterCount; parameterIndex++)
                m_StoredParameters[parameterIndex] = 0f;
        }

        int FindContribution(AnimationPoseContributionKind kind)
        {
            for (int i = 0; i < m_FramePlan.ContributionCount; i++)
            {
                if (m_FramePlan.GetEntry(i).Kind == kind)
                    return i;
            }
            return -1;
        }

        static void RequirePlan(AnimationSlotBlendFramePlan plan, int sourceCapacity)
        {
            plan.RequireValidLayout();
            AnimationSlotBlendFramePlanHeader header = plan.Header;
            if (sourceCapacity != checked(header.MaxActiveSourceEntries + 1))
                throw new ArgumentException();

            int liveCount = 0;
            int storedCount = 0;
            float scalarWeight = 0f;
            float leftFootWeight = 0f;
            float rightFootWeight = 0f;
            for (int contributionIndex = 0; contributionIndex < header.ContributionCount; contributionIndex++)
            {
                AnimationSlotBlendFramePlanEntry entry = plan.GetEntry(contributionIndex);
                if (!entry.IsValid ||
                    entry.Kind == AnimationPoseContributionKind.Live &&
                    (uint)entry.SourceCaptureIndex >= (uint)sourceCapacity)
                    throw new ArgumentException();
                for (int previousIndex = 0; previousIndex < contributionIndex; previousIndex++)
                {
                    if (entry.ContributionContinuityIdentity ==
                        plan.GetEntry(previousIndex).ContributionContinuityIdentity)
                        throw new ArgumentException();
                }
                if (entry.Kind == AnimationPoseContributionKind.Live)
                    liveCount++;
                else if (entry.Kind == AnimationPoseContributionKind.Stored)
                    storedCount++;
                else
                    throw new ArgumentException();
                scalarWeight += entry.ScalarWeight;
                leftFootWeight += entry.LeftFootWeight;
                rightFootWeight += entry.RightFootWeight;
            }
            if (!float.IsFinite(scalarWeight) || !float.IsFinite(leftFootWeight) ||
                !float.IsFinite(rightFootWeight) ||
                scalarWeight > 1f + WeightTolerance || leftFootWeight > 1f + WeightTolerance ||
                rightFootWeight > 1f + WeightTolerance ||
                Mathf.Abs(scalarWeight - header.OutputWeight) > WeightTolerance ||
                liveCount > header.MaxActiveSourceEntries || storedCount > 1 ||
                header.Kind == AnimationSlotBlendFramePlanKind.StoredCapture && storedCount != 1 ||
                header.Kind == AnimationSlotBlendFramePlanKind.Unavailable &&
                (liveCount != 0 || storedCount != 0 || scalarWeight != 0f))
                throw new ArgumentException();

            bool hasOutputWeight = header.OutputWeight > 0f;
            for (int boneIndex = 0; boneIndex < header.BoneCount; boneIndex++)
            {
                float boneWeight = 0f;
                for (int contributionIndex = 0; contributionIndex < header.ContributionCount; contributionIndex++)
                    boneWeight += plan.GetDenseBoneWeight(contributionIndex, boneIndex);
                if (!float.IsFinite(boneWeight) || boneWeight > 1f + WeightTolerance)
                    throw new ArgumentException();
                hasOutputWeight |= boneWeight > 0f;
            }
            if (header.Availability == AnimationPoseAvailability.Pose && !hasOutputWeight)
                throw new ArgumentException();
        }

        static void RequireSourceBinding(
            AnimationBlendSourcePoseNativeReadBinding binding,
            AnimationSlotBlendFramePlanHeader header)
        {
            if (binding.BoneCount != header.BoneCount || binding.ParameterCount != header.ParameterCount ||
                binding.SourceCapacity != checked(header.MaxActiveSourceEntries + 1) ||
                binding.CompletionIdentity != header.CompletionIdentity)
            {
                throw new ArgumentException();
            }
            RequireLength(binding.CurrentPose, checked(binding.SourceCapacity * binding.BoneCount));
            RequireLength(binding.Velocity, checked(binding.SourceCapacity * binding.BoneCount));
            RequireLength(binding.PoseParameters, checked(binding.SourceCapacity * binding.ParameterCount));
            RequireLength(binding.PoseParameterAvailability, checked(binding.SourceCapacity * binding.ParameterCount));
            RequireLength(binding.LeftFootFeatures, binding.SourceCapacity);
            RequireLength(binding.RightFootFeatures, binding.SourceCapacity);
            RequireLength(binding.VisualTimeScales, binding.SourceCapacity);
            RequireLength(binding.HasFootFeatures, binding.SourceCapacity);
            RequireLength(binding.CompletedAt, binding.SourceCapacity);
            RequireLength(binding.ProgramProducerIndices, binding.SourceCapacity);
        }

        static void RequireFinalBinding(
            AnimationPlayerPoseNativeWriteBinding binding,
            AnimationSlotBlendFramePlanHeader header)
        {
            if (binding.CompletionIdentity != header.CompletionIdentity ||
                binding.Range.PhysicalPlayerIndex != header.PhysicalPlayerIndex ||
                binding.Range.ContributionCapacity != header.ContributionCapacity)
            {
                throw new ArgumentException();
            }
            RequireLength(binding.DenseLocalPoses, header.BoneCount);
            RequireLength(binding.DenseVelocities, header.BoneCount);
            RequireLength(binding.PoseParameters, header.ParameterCount);
            RequireLength(binding.PoseParameterAvailability, header.ParameterCount);
            RequireLength(binding.Contributions, header.ContributionCapacity);
            RequireLength(binding.DenseContributionWeights, checked(header.ContributionCapacity * header.BoneCount));
            RequireLength(binding.ContributionCount, 1);
            RequireLength(binding.OutputWeight, 1);
            RequireLength(binding.LeftFootFeatures, 1);
            RequireLength(binding.RightFootFeatures, 1);
            RequireLength(binding.HasFootFeatures, 1);
            RequireLength(binding.Availability, 1);
            RequireLength(binding.ContinuityIdentity, 1);
            RequireLength(binding.InvalidReason, 1);
            RequireLength(binding.CompletedAt, 1);
        }

        static void RequireWorkspaceBinding(
            AnimationSlotBlendPoseWorkspaceBinding binding,
            AnimationSlotBlendFramePlanHeader header)
        {
            RequireLength(binding.StoredPose.State, 1);
            RequireLength(binding.StoredPose.DenseLocalPose, header.BoneCount);
            RequireLength(binding.StoredPose.DenseVelocity, header.BoneCount);
            RequireLength(binding.StoredPose.PoseParameters, header.ParameterCount);
            RequireLength(binding.StoredPose.DenseBoneOutputWeights, header.BoneCount);

            RequireLength(binding.History.States, 2);
            RequireLength(binding.History.DenseLocalPoses, checked(header.BoneCount * 2));
            RequireLength(binding.History.DenseVelocities, checked(header.BoneCount * 2));
            RequireLength(binding.History.PoseParameters, checked(header.ParameterCount * 2));
            RequireLength(binding.History.DenseBoneOutputWeights, checked(header.BoneCount * 2));

            RequireLength(binding.Scratch.State, 1);
            RequireLength(binding.Scratch.DenseLocalPose, header.BoneCount);
            RequireLength(binding.Scratch.DenseVelocity, header.BoneCount);
            RequireLength(binding.Scratch.PoseParameters, header.ParameterCount);
            RequireLength(binding.Scratch.Contributions, header.ContributionCapacity);
            RequireLength(binding.Scratch.DenseContributionWeights, checked(header.ContributionCapacity * header.BoneCount));
            RequireLength(binding.Scratch.PositionSums, header.BoneCount);
            RequireLength(binding.Scratch.RotationSums, header.BoneCount);
            RequireLength(binding.Scratch.ScaleSums, header.BoneCount);
            RequireLength(binding.Scratch.LinearVelocitySums, header.BoneCount);
            RequireLength(binding.Scratch.AngularVelocitySums, header.BoneCount);
            RequireLength(binding.Scratch.ScaleVelocitySums, header.BoneCount);
            RequireLength(binding.Scratch.PoseWeightSums, header.BoneCount);
            RequireLength(binding.Scratch.FootFeatureAccumulators, 2);
        }

        static void RequireLength<T>(NativeArray<T> values, int expectedLength) where T : struct
        {
            if (!values.IsCreated || values.Length != expectedLength)
                throw new ArgumentException();
        }

        static void RequireLength<T>(NativeSlice<T> values, int expectedLength) where T : struct
        {
            if (values.Length != expectedLength)
                throw new ArgumentException();
        }
    }
}
