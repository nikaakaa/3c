using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Animations;

namespace ThirdPersonCharacter.Pipeline.Animation.BlendStack
{
    internal struct AnimationSlotBlendJob : IAnimationJob
    {
        const float QuaternionTolerance = 0.0000001f;
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

        NativeArray<AnimationSlotBlendInertialNativeState> m_InertialState;
        NativeArray<Vector3> m_InertialPositionResiduals;
        NativeArray<Vector3> m_InertialRotationResiduals;
        NativeArray<Vector3> m_InertialScaleResiduals;
        NativeArray<Vector3> m_InertialLinearVelocityResiduals;
        NativeArray<Vector3> m_InertialAngularVelocityResiduals;
        NativeArray<Vector3> m_InertialScaleVelocityResiduals;
        NativeArray<float> m_InertialParameterResiduals;
        NativeArray<float> m_InertialBoneOutputWeights;

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
        NativeSlice<PoseSlotFrameAvailability> m_FinalAvailability;
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
        readonly int m_PhysicalSlotIndex;
        readonly ulong m_CompletionIdentity;

        internal AnimationSlotBlendJob(
            AnimationSlotBlendPoseWorkspaceBinding workspace,
            AnimationBlendSourcePoseNativeReadBinding sources)
        {
            AnimationSlotBlendFramePlan plan = workspace.FramePlan;
            plan.RequireValidLayout();
            AnimationSlotBlendFramePlanHeader header = plan.Header;
            AnimationPoseSlotNativeWriteBinding final = workspace.FinalWriteBinding;

            RequirePlan(plan, sources.SourceCapacity);
            RequireSourceBinding(sources, header);
            RequireFinalBinding(final, header);
            RequireWorkspaceBinding(workspace, header);

            m_FramePlan = plan;

            m_SourceCurrentPose = sources.CurrentPose;
            m_SourceVelocity = sources.Velocity;
            m_SourcePoseParameters = sources.PoseParameters;
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

            m_InertialState = workspace.Inertial.State;
            m_InertialPositionResiduals = workspace.Inertial.PositionResiduals;
            m_InertialRotationResiduals = workspace.Inertial.RotationResiduals;
            m_InertialScaleResiduals = workspace.Inertial.ScaleResiduals;
            m_InertialLinearVelocityResiduals = workspace.Inertial.LinearVelocityResiduals;
            m_InertialAngularVelocityResiduals = workspace.Inertial.AngularVelocityResiduals;
            m_InertialScaleVelocityResiduals = workspace.Inertial.ScaleVelocityResiduals;
            m_InertialParameterResiduals = workspace.Inertial.ParameterResiduals;
            m_InertialBoneOutputWeights = workspace.Inertial.DenseBoneOutputWeights;

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
            m_PhysicalSlotIndex = header.PhysicalSlotIndex;
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
            if (reason != AnimationPoseNativeInvalidReason.None)
            {
                PublishInvalid(reason);
                return;
            }

            AnimationSlotBlendFramePlanHeader header = m_FramePlan.Header;
            if (header.Availability == PoseSlotFrameAvailability.NoPose)
            {
                ClearStored();
                ClearInertial();
                CommitNoPoseHistory();
                PublishNoPose();
                return;
            }

            reason = PrepareScratchContributions();
            if (reason == AnimationPoseNativeInvalidReason.None)
            {
                reason = header.UsesInertial
                    ? BlendInertial()
                    : BlendCrossFade(deltaSeconds);
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
                header.PhysicalSlotIndex != m_PhysicalSlotIndex ||
                m_SourceCompletedAt.Length != m_SourceCapacity)
            {
                return AnimationPoseNativeInvalidReason.SlotPlanInvalid;
            }

            if (header.HistoryReadPageIndex >= 0 && !IsValidHistory(header.HistoryReadPageIndex, header.HistoryCompletionIdentity))
                return AnimationPoseNativeInvalidReason.SlotPlanInvalid;

            int liveIndex = -1;
            int storedIndex = -1;
            int inertialIndex = -1;
            for (int contributionIndex = 0; contributionIndex < header.ContributionCount; contributionIndex++)
            {
                AnimationSlotBlendFramePlanEntry entry = m_FramePlan.GetEntry(contributionIndex);
                switch (entry.Kind)
                {
                    case AnimationPoseContributionKind.Live:
                        if (!IsValidSource(entry))
                            return AnimationPoseNativeInvalidReason.SourceIncomplete;
                        liveIndex = contributionIndex;
                        break;
                    case AnimationPoseContributionKind.Stored:
                        storedIndex = contributionIndex;
                        break;
                    case AnimationPoseContributionKind.Inertial:
                        inertialIndex = contributionIndex;
                        break;
                    default:
                        return AnimationPoseNativeInvalidReason.SlotContributionInvalid;
                }
            }

            if (header.Availability == PoseSlotFrameAvailability.NoPose)
                return header.ContributionCount == 0 && header.OutputWeight == 0f
                    ? AnimationPoseNativeInvalidReason.None
                    : AnimationPoseNativeInvalidReason.SlotPlanInvalid;

            if (header.Kind == AnimationSlotBlendFramePlanKind.StoredCapture)
            {
                if (storedIndex < 0 || header.HistoryReadPageIndex < 0)
                    return AnimationPoseNativeInvalidReason.SlotPlanInvalid;
            }
            else if (storedIndex >= 0 && !IsValidStored(m_FramePlan.GetEntry(storedIndex)))
            {
                return AnimationPoseNativeInvalidReason.SlotPoseInvalid;
            }

            if (!header.UsesInertial)
                return inertialIndex < 0 ? AnimationPoseNativeInvalidReason.None : AnimationPoseNativeInvalidReason.SlotPlanInvalid;

            if (liveIndex < 0 || inertialIndex < 0)
                return AnimationPoseNativeInvalidReason.SlotPlanInvalid;
            AnimationSlotBlendFramePlanEntry live = m_FramePlan.GetEntry(liveIndex);
            AnimationSlotBlendFramePlanEntry inertial = m_FramePlan.GetEntry(inertialIndex);
            if (header.Kind == AnimationSlotBlendFramePlanKind.InertialContinue)
                return IsValidInertial(live, inertial)
                    ? AnimationPoseNativeInvalidReason.None
                    : AnimationPoseNativeInvalidReason.SlotPoseInvalid;
            return header.HistoryReadPageIndex >= 0
                ? AnimationPoseNativeInvalidReason.None
                : AnimationPoseNativeInvalidReason.SlotPlanInvalid;
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
                if (!float.IsFinite(m_SourcePoseParameters[parameterOffset + parameterIndex]))
                    return false;
            }
            return true;
        }

        bool IsValidHistory(int pageIndex, ulong completionIdentity)
        {
            if ((uint)pageIndex > 1u)
                return false;
            AnimationSlotBlendHistoryNativeState state = m_HistoryStates[pageIndex];
            if (state.Availability != PoseSlotFrameAvailability.Pose ||
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

        bool IsValidInertial(
            AnimationSlotBlendFramePlanEntry live,
            AnimationSlotBlendFramePlanEntry inertial)
        {
            AnimationSlotBlendInertialNativeState state = m_InertialState[0];
            if (state.Active != 1 || state.TargetSourceCaptureIndex != live.SourceCaptureIndex ||
                state.TargetPhysicalSourceIndex != live.PhysicalSourceIndex ||
                state.TargetPhysicalSourceGeneration != live.PhysicalSourceGeneration ||
                state.TargetProgramProducerIndex != live.ProgramProducerIndex ||
                state.CapturedAtCompletionIdentity == 0 || state.SourceHistoryCompletionIdentity == 0 ||
                state.ContributionContinuityIdentity != inertial.ContributionContinuityIdentity ||
                !IsNormalized(state.OutputWeight) || state.SourceHasFootFeatures > 1 ||
                state.SourceHasFootFeatures != 0 && (!IsValidFoot(state.LeftFootFeatures) || !IsValidFoot(state.RightFootFeatures)) ||
                state.SourceHasFootFeatures == 0 && (state.LeftFootFeatures.IsValid || state.RightFootFeatures.IsValid))
            {
                return false;
            }
            bool preserveScale = m_FramePlan.Header.ScalePolicy == CharacterAnimationScalePolicy.PreserveReferenceScale;
            for (int boneIndex = 0; boneIndex < m_BoneCount; boneIndex++)
            {
                if (!AnimationBlendPoseMath.IsFinite(m_InertialPositionResiduals[boneIndex]) ||
                    !AnimationBlendPoseMath.IsFinite(m_InertialRotationResiduals[boneIndex]) ||
                    !AnimationBlendPoseMath.IsFinite(m_InertialScaleResiduals[boneIndex]) ||
                    !AnimationBlendPoseMath.IsFinite(m_InertialLinearVelocityResiduals[boneIndex]) ||
                    !AnimationBlendPoseMath.IsFinite(m_InertialAngularVelocityResiduals[boneIndex]) ||
                    !AnimationBlendPoseMath.IsFinite(m_InertialScaleVelocityResiduals[boneIndex]) ||
                    !IsNormalized(m_InertialBoneOutputWeights[boneIndex]) ||
                    preserveScale && (m_InertialScaleResiduals[boneIndex] != Vector3.zero ||
                                      m_InertialScaleVelocityResiduals[boneIndex] != Vector3.zero))
                {
                    return false;
                }
            }
            for (int parameterIndex = 0; parameterIndex < m_ParameterCount; parameterIndex++)
            {
                if (!float.IsFinite(m_InertialParameterResiduals[parameterIndex]))
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
                    header.PhysicalSlotIndex,
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
                    rotationSum += AnimationBlendPoseMath.AlignAndScale(pose.Rotation, rotationReference, weight);
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

        AnimationPoseNativeInvalidReason BlendInertial()
        {
            FindInertialEntries(out AnimationSlotBlendFramePlanEntry live, out _);
            bool capture = m_FramePlan.Header.Kind == AnimationSlotBlendFramePlanKind.InertialCapture ||
                           m_FramePlan.Header.Kind == AnimationSlotBlendFramePlanKind.InertialRebase;
            int sourcePoseOffset = checked(live.SourceCaptureIndex * m_BoneCount);
            int historyPoseOffset = checked(m_FramePlan.Header.HistoryReadPageIndex * m_BoneCount);
            for (int boneIndex = 0; boneIndex < m_BoneCount; boneIndex++)
            {
                AnimationLocalBonePose targetPose = m_SourceCurrentPose[sourcePoseOffset + boneIndex];
                AnimationBlendBoneVelocity targetVelocity = m_SourceVelocity[sourcePoseOffset + boneIndex];
                Vector3 positionResidual;
                Vector3 rotationResidual;
                Vector3 scaleResidual;
                Vector3 linearVelocityResidual;
                Vector3 angularVelocityResidual;
                Vector3 scaleVelocityResidual;
                if (capture)
                {
                    AnimationLocalBonePose sourcePose = m_HistoryPoses[historyPoseOffset + boneIndex];
                    AnimationBlendBoneVelocity sourceVelocity = m_HistoryVelocities[historyPoseOffset + boneIndex];
                    positionResidual = sourcePose.Position - targetPose.Position;
                    rotationResidual = AnimationBlendPoseMath.QuaternionLog(sourcePose.Rotation * Quaternion.Inverse(targetPose.Rotation));
                    linearVelocityResidual = sourceVelocity.Linear - targetVelocity.Linear;
                    angularVelocityResidual = sourceVelocity.Angular - targetVelocity.Angular;
                    if (m_FramePlan.Header.ScalePolicy == CharacterAnimationScalePolicy.BlendLocalScale)
                    {
                        scaleResidual = sourcePose.Scale - targetPose.Scale;
                        scaleVelocityResidual = sourceVelocity.Scale - targetVelocity.Scale;
                    }
                    else
                    {
                        scaleResidual = Vector3.zero;
                        scaleVelocityResidual = Vector3.zero;
                    }
                }
                else
                {
                    positionResidual = m_InertialPositionResiduals[boneIndex];
                    rotationResidual = m_InertialRotationResiduals[boneIndex];
                    scaleResidual = m_InertialScaleResiduals[boneIndex];
                    linearVelocityResidual = m_InertialLinearVelocityResiduals[boneIndex];
                    angularVelocityResidual = m_InertialAngularVelocityResiduals[boneIndex];
                    scaleVelocityResidual = m_InertialScaleVelocityResiduals[boneIndex];
                }

                AnimationSlotBlendInertialBonePlan plan = m_FramePlan.GetInertialBone(boneIndex);
                Vector3 positionBase = positionResidual + plan.ResidualTimeSeconds * linearVelocityResidual;
                Vector3 rotationBase = rotationResidual + plan.ResidualTimeSeconds * angularVelocityResidual;
                Vector3 scaleBase = scaleResidual + plan.ResidualTimeSeconds * scaleVelocityResidual;
                Vector3 outputPosition = targetPose.Position + plan.ResidualWeight * positionBase;
                Vector3 outputScale = targetPose.Scale + plan.ResidualWeight * scaleBase;
                Vector3 outputLinearVelocity = targetVelocity.Linear +
                                               plan.ResidualWeightDerivativePerSecond * positionBase +
                                               plan.ResidualWeight * linearVelocityResidual;
                Vector3 outputAngularVelocity = targetVelocity.Angular +
                                                plan.ResidualWeightDerivativePerSecond * rotationBase +
                                                plan.ResidualWeight * angularVelocityResidual;
                Vector3 outputScaleVelocity = targetVelocity.Scale +
                                              plan.ResidualWeightDerivativePerSecond * scaleBase +
                                              plan.ResidualWeight * scaleVelocityResidual;
                Vector3 outputRotationResidual = plan.ResidualWeight * rotationBase;
                if (!AnimationBlendPoseMath.IsFinite(outputPosition) ||
                    !AnimationBlendPoseMath.IsFinite(outputScale) ||
                    !TryQuaternionExp(outputRotationResidual, out Quaternion residualRotation))
                {
                    return AnimationPoseNativeInvalidReason.SlotPoseInvalid;
                }
                Quaternion outputRotation = residualRotation * targetPose.Rotation;
                if (!IsFinite(outputRotation) || Quaternion.Dot(outputRotation, outputRotation) <= QuaternionTolerance)
                    return AnimationPoseNativeInvalidReason.SlotPoseInvalid;
                m_ScratchPose[boneIndex] = new AnimationLocalBonePose(outputPosition, outputRotation, outputScale);
                if (!TryCreateVelocity(
                        outputLinearVelocity,
                        outputAngularVelocity,
                        outputScaleVelocity,
                        out AnimationBlendBoneVelocity outputVelocity))
                {
                    return AnimationPoseNativeInvalidReason.SlotVelocityInvalid;
                }
                m_ScratchVelocity[boneIndex] = outputVelocity;

                float weightSum = 0f;
                for (int contributionIndex = 0; contributionIndex < m_FramePlan.ContributionCount; contributionIndex++)
                    weightSum += m_FramePlan.GetDenseBoneWeight(contributionIndex, boneIndex);
                m_ScratchPoseWeightSums[boneIndex] = weightSum;
            }

            int sourceParameterOffset = checked(live.SourceCaptureIndex * m_ParameterCount);
            int historyParameterOffset = checked(m_FramePlan.Header.HistoryReadPageIndex * m_ParameterCount);
            for (int parameterIndex = 0; parameterIndex < m_ParameterCount; parameterIndex++)
            {
                float target = m_SourcePoseParameters[sourceParameterOffset + parameterIndex];
                float residual = capture
                    ? m_HistoryParameters[historyParameterOffset + parameterIndex] - target
                    : m_InertialParameterResiduals[parameterIndex];
                float output = target + residual * m_FramePlan.GetInertialParameterResidualWeight(parameterIndex);
                if (!float.IsFinite(output))
                    return AnimationPoseNativeInvalidReason.SlotParameterInvalid;
                m_ScratchParameters[parameterIndex] = output;
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
                Availability = PoseSlotFrameAvailability.Pose,
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
            if (state.Availability != PoseSlotFrameAvailability.Pose ||
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
            if (entry.Kind == AnimationPoseContributionKind.Inertial)
            {
                if (m_FramePlan.Header.Kind == AnimationSlotBlendFramePlanKind.InertialCapture ||
                    m_FramePlan.Header.Kind == AnimationSlotBlendFramePlanKind.InertialRebase)
                {
                    AnimationSlotBlendHistoryNativeState history = m_HistoryStates[m_FramePlan.Header.HistoryReadPageIndex];
                    left = history.LeftFootFeatures;
                    right = history.RightFootFeatures;
                    hasFeatures = history.HasFootFeatures != 0;
                }
                else
                {
                    AnimationSlotBlendInertialNativeState inertial = m_InertialState[0];
                    left = inertial.LeftFootFeatures;
                    right = inertial.RightFootFeatures;
                    hasFeatures = inertial.SourceHasFootFeatures != 0;
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
            AnimationSlotBlendFramePlanHeader header = m_FramePlan.Header;
            if (header.UsesInertial)
            {
                if (header.Kind == AnimationSlotBlendFramePlanKind.InertialCapture ||
                    header.Kind == AnimationSlotBlendFramePlanKind.InertialRebase)
                {
                    CommitInertialCapture();
                }
                ClearStored();
                return;
            }

            ClearInertial();
            int storedIndex = FindContribution(AnimationPoseContributionKind.Stored);
            if (header.Kind == AnimationSlotBlendFramePlanKind.StoredCapture)
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

        void CommitInertialCapture()
        {
            FindInertialEntries(out AnimationSlotBlendFramePlanEntry live, out AnimationSlotBlendFramePlanEntry inertial);
            int historyPage = m_FramePlan.Header.HistoryReadPageIndex;
            int historyPoseOffset = checked(historyPage * m_BoneCount);
            int sourcePoseOffset = checked(live.SourceCaptureIndex * m_BoneCount);
            for (int boneIndex = 0; boneIndex < m_BoneCount; boneIndex++)
            {
                AnimationLocalBonePose from = m_HistoryPoses[historyPoseOffset + boneIndex];
                AnimationLocalBonePose to = m_SourceCurrentPose[sourcePoseOffset + boneIndex];
                AnimationBlendBoneVelocity fromVelocity = m_HistoryVelocities[historyPoseOffset + boneIndex];
                AnimationBlendBoneVelocity toVelocity = m_SourceVelocity[sourcePoseOffset + boneIndex];
                m_InertialPositionResiduals[boneIndex] = from.Position - to.Position;
                m_InertialRotationResiduals[boneIndex] = AnimationBlendPoseMath.QuaternionLog(from.Rotation * Quaternion.Inverse(to.Rotation));
                m_InertialLinearVelocityResiduals[boneIndex] = fromVelocity.Linear - toVelocity.Linear;
                m_InertialAngularVelocityResiduals[boneIndex] = fromVelocity.Angular - toVelocity.Angular;
                m_InertialBoneOutputWeights[boneIndex] = m_HistoryBoneOutputWeights[historyPoseOffset + boneIndex];
                if (m_FramePlan.Header.ScalePolicy == CharacterAnimationScalePolicy.BlendLocalScale)
                {
                    m_InertialScaleResiduals[boneIndex] = from.Scale - to.Scale;
                    m_InertialScaleVelocityResiduals[boneIndex] = fromVelocity.Scale - toVelocity.Scale;
                }
                else
                {
                    m_InertialScaleResiduals[boneIndex] = Vector3.zero;
                    m_InertialScaleVelocityResiduals[boneIndex] = Vector3.zero;
                }
            }
            int historyParameterOffset = checked(historyPage * m_ParameterCount);
            int sourceParameterOffset = checked(live.SourceCaptureIndex * m_ParameterCount);
            for (int parameterIndex = 0; parameterIndex < m_ParameterCount; parameterIndex++)
            {
                m_InertialParameterResiduals[parameterIndex] =
                    m_HistoryParameters[historyParameterOffset + parameterIndex] -
                    m_SourcePoseParameters[sourceParameterOffset + parameterIndex];
            }
            AnimationSlotBlendHistoryNativeState history = m_HistoryStates[historyPage];
            m_InertialState[0] = new AnimationSlotBlendInertialNativeState
            {
                Active = 1,
                SourceHasFootFeatures = history.HasFootFeatures,
                TargetSourceCaptureIndex = live.SourceCaptureIndex,
                TargetPhysicalSourceIndex = live.PhysicalSourceIndex,
                TargetPhysicalSourceGeneration = live.PhysicalSourceGeneration,
                TargetProgramProducerIndex = live.ProgramProducerIndex,
                CapturedAtCompletionIdentity = m_CompletionIdentity,
                SourceHistoryCompletionIdentity = history.CompletionIdentity,
                ContributionContinuityIdentity = inertial.ContributionContinuityIdentity,
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
                Availability = PoseSlotFrameAvailability.Pose,
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
                Availability = PoseSlotFrameAvailability.NoPose,
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
                m_FinalPoseParameters[parameterIndex] = m_ScratchParameters[parameterIndex];
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
            m_FinalAvailability[0] = PoseSlotFrameAvailability.Pose;
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
                Availability = PoseSlotFrameAvailability.NoPose,
                InvalidReason = AnimationPoseNativeInvalidReason.None,
                ContinuityIdentity = header.ContinuityIdentity
            };
            m_FinalAvailability[0] = PoseSlotFrameAvailability.NoPose;
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
                Availability = PoseSlotFrameAvailability.Invalid,
                InvalidReason = reason,
                ContinuityIdentity = continuityIdentity
            };
            m_FinalAvailability[0] = PoseSlotFrameAvailability.Invalid;
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
                m_FinalPoseParameters[parameterIndex] = 0f;
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

        void ClearInertial()
        {
            m_InertialState[0] = default;
            for (int boneIndex = 0; boneIndex < m_BoneCount; boneIndex++)
            {
                m_InertialPositionResiduals[boneIndex] = Vector3.zero;
                m_InertialRotationResiduals[boneIndex] = Vector3.zero;
                m_InertialScaleResiduals[boneIndex] = Vector3.zero;
                m_InertialLinearVelocityResiduals[boneIndex] = Vector3.zero;
                m_InertialAngularVelocityResiduals[boneIndex] = Vector3.zero;
                m_InertialScaleVelocityResiduals[boneIndex] = Vector3.zero;
                m_InertialBoneOutputWeights[boneIndex] = 0f;
            }
            for (int parameterIndex = 0; parameterIndex < m_ParameterCount; parameterIndex++)
                m_InertialParameterResiduals[parameterIndex] = 0f;
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

        void FindInertialEntries(
            out AnimationSlotBlendFramePlanEntry live,
            out AnimationSlotBlendFramePlanEntry inertial)
        {
            live = default;
            inertial = default;
            for (int i = 0; i < m_FramePlan.ContributionCount; i++)
            {
                AnimationSlotBlendFramePlanEntry entry = m_FramePlan.GetEntry(i);
                if (entry.Kind == AnimationPoseContributionKind.Live)
                    live = entry;
                else if (entry.Kind == AnimationPoseContributionKind.Inertial)
                    inertial = entry;
            }
        }

        static bool TryResolveWeightedPose(
            Vector3 positionSum,
            Vector4 rotationSum,
            Vector3 scaleSum,
            float weight,
            out AnimationLocalBonePose pose)
        {
            if (!float.IsFinite(weight) || weight <= 0f ||
                !AnimationBlendPoseMath.IsFinite(positionSum) ||
                !AnimationBlendPoseMath.IsFinite(scaleSum) ||
                !IsFinite(rotationSum))
            {
                pose = default;
                return false;
            }
            Vector3 position = positionSum / weight;
            Vector3 scale = scaleSum / weight;
            Quaternion rotation = new Quaternion(rotationSum.x, rotationSum.y, rotationSum.z, rotationSum.w);
            float magnitude = Quaternion.Dot(rotation, rotation);
            if (!AnimationBlendPoseMath.IsFinite(position) || !AnimationBlendPoseMath.IsFinite(scale) ||
                !float.IsFinite(magnitude) || magnitude <= QuaternionTolerance)
            {
                pose = default;
                return false;
            }
            rotation = rotation.normalized;
            if (!IsFinite(rotation))
            {
                pose = default;
                return false;
            }
            pose = new AnimationLocalBonePose(position, rotation, scale);
            return true;
        }

        static bool TryCreateVelocity(
            Vector3 linear,
            Vector3 angular,
            Vector3 scale,
            out AnimationBlendBoneVelocity velocity)
        {
            if (!AnimationBlendPoseMath.IsFinite(linear) ||
                !AnimationBlendPoseMath.IsFinite(angular) ||
                !AnimationBlendPoseMath.IsFinite(scale))
            {
                velocity = default;
                return false;
            }
            velocity = new AnimationBlendBoneVelocity(linear, angular, scale);
            return true;
        }

        static bool TryDifferentiate(
            AnimationLocalBonePose previous,
            AnimationLocalBonePose current,
            float deltaSeconds,
            out AnimationBlendBoneVelocity velocity)
        {
            Vector3 linear = (current.Position - previous.Position) / deltaSeconds;
            Vector3 angular = AnimationBlendPoseMath.QuaternionLog(current.Rotation * Quaternion.Inverse(previous.Rotation)) / deltaSeconds;
            Vector3 scale = (current.Scale - previous.Scale) / deltaSeconds;
            return TryCreateVelocity(linear, angular, scale, out velocity);
        }

        static bool TryQuaternionExp(Vector3 value, out Quaternion result)
        {
            if (!AnimationBlendPoseMath.IsFinite(value))
            {
                result = default;
                return false;
            }
            double x = value.x;
            double y = value.y;
            double z = value.z;
            double angle = Math.Sqrt(x * x + y * y + z * z);
            if (double.IsNaN(angle) || double.IsInfinity(angle))
            {
                result = default;
                return false;
            }
            if (angle <= QuaternionTolerance)
            {
                result = Quaternion.identity;
                return true;
            }
            double half = angle * 0.5d;
            double scale = Math.Sin(half) / angle;
            result = new Quaternion(
                (float)(x * scale),
                (float)(y * scale),
                (float)(z * scale),
                (float)Math.Cos(half));
            if (!IsFinite(result) || Quaternion.Dot(result, result) <= QuaternionTolerance)
            {
                result = default;
                return false;
            }
            result = result.normalized;
            return IsFinite(result);
        }

        static bool AccumulateFoot(
            AnimationFootFeatureSample sample,
            float weight,
            float visualTimeScale,
            ref float totalWeight,
            ref Vector3 velocity,
            ref float height,
            ref float plantConfidence,
            ref float landingConfidence,
            ref float landingWeight,
            ref float landingDelay,
            ref Vector2 landingOffset)
        {
            if (!IsValidFoot(sample) || !float.IsFinite(weight) || weight <= 0f ||
                !float.IsFinite(visualTimeScale) || visualTimeScale < 0f)
            {
                return false;
            }
            float effectiveLandingConfidence = visualTimeScale > 0.000001f
                ? sample.NextLandingConfidence
                : 0f;
            float nextLandingWeight = weight * effectiveLandingConfidence;
            totalWeight += weight;
            velocity += sample.SoleLocalVelocity * visualTimeScale * weight;
            height += sample.SoleHeight * weight;
            plantConfidence += sample.PlantConfidence * weight;
            landingConfidence += effectiveLandingConfidence * weight;
            landingWeight += nextLandingWeight;
            if (nextLandingWeight > 0f)
                landingDelay += sample.NextLandingDelaySeconds / visualTimeScale * nextLandingWeight;
            landingOffset += sample.NextLandingLocalOffset * nextLandingWeight;
            return float.IsFinite(totalWeight) && AnimationBlendPoseMath.IsFinite(velocity) &&
                   float.IsFinite(height) && float.IsFinite(plantConfidence) &&
                   float.IsFinite(landingConfidence) && float.IsFinite(landingWeight) &&
                   float.IsFinite(landingDelay) && IsFinite(landingOffset);
        }

        static bool TryResolveFoot(
            float weight,
            Vector3 velocity,
            float height,
            float plantConfidence,
            float landingConfidence,
            float landingWeight,
            float landingDelay,
            Vector2 landingOffset,
            out AnimationFootFeatureSample sample)
        {
            float inverseWeight = 1f / weight;
            float resolvedPlant = plantConfidence * inverseWeight;
            float resolvedLandingConfidence = landingConfidence * inverseWeight;
            float resolvedLandingDelay = landingWeight > 0f ? landingDelay / landingWeight : 0f;
            Vector2 resolvedLandingOffset = landingWeight > 0f ? landingOffset / landingWeight : Vector2.zero;
            Vector3 resolvedVelocity = velocity * inverseWeight;
            float resolvedHeight = height * inverseWeight;
            if (!AnimationBlendPoseMath.IsFinite(resolvedVelocity) || !float.IsFinite(resolvedHeight) ||
                !IsNormalized(resolvedPlant) || !IsNormalized(resolvedLandingConfidence) ||
                !float.IsFinite(resolvedLandingDelay) || resolvedLandingDelay < 0f ||
                !IsFinite(resolvedLandingOffset))
            {
                sample = default;
                return false;
            }
            sample = new AnimationFootFeatureSample(
                resolvedVelocity,
                resolvedHeight,
                resolvedPlant,
                resolvedLandingConfidence,
                resolvedLandingDelay,
                resolvedLandingOffset);
            return true;
        }

        static bool IsValidFoot(AnimationFootFeatureSample sample) =>
            sample.IsValid && AnimationBlendPoseMath.IsFinite(sample.SoleLocalVelocity) &&
            float.IsFinite(sample.SoleHeight) && IsNormalized(sample.PlantConfidence) &&
            IsNormalized(sample.NextLandingConfidence) &&
            float.IsFinite(sample.NextLandingDelaySeconds) && sample.NextLandingDelaySeconds >= 0f &&
            IsFinite(sample.NextLandingLocalOffset);

        static bool IsNormalized(float value) =>
            float.IsFinite(value) && value >= 0f && value <= 1f;

        static bool IsFinite(Vector2 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y);

        static bool IsFinite(Vector4 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) &&
            float.IsFinite(value.z) && float.IsFinite(value.w);

        static bool IsFinite(Quaternion value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) &&
            float.IsFinite(value.z) && float.IsFinite(value.w);

        static void RequirePlan(AnimationSlotBlendFramePlan plan, int sourceCapacity)
        {
            plan.RequireValidLayout();
            AnimationSlotBlendFramePlanHeader header = plan.Header;
            if (sourceCapacity != checked(header.MaxActiveSourceEntries + 1))
                throw new ArgumentException();

            int liveCount = 0;
            int storedCount = 0;
            int inertialCount = 0;
            float scalarWeight = 0f;
            float leftFootWeight = 0f;
            float rightFootWeight = 0f;
            for (int contributionIndex = 0; contributionIndex < header.ContributionCount; contributionIndex++)
            {
                AnimationSlotBlendFramePlanEntry entry = plan.GetEntry(contributionIndex);
                if (!entry.IsValid ||
                    entry.Kind == AnimationPoseContributionKind.Live &&
                    (uint)entry.SourceCaptureIndex >= (uint)sourceCapacity)
                {
                    throw new ArgumentException();
                }
                for (int previousIndex = 0; previousIndex < contributionIndex; previousIndex++)
                {
                    if (entry.ContributionContinuityIdentity == plan.GetEntry(previousIndex).ContributionContinuityIdentity)
                        throw new ArgumentException();
                }
                if (entry.Kind == AnimationPoseContributionKind.Live)
                    liveCount++;
                else if (entry.Kind == AnimationPoseContributionKind.Stored)
                    storedCount++;
                else if (entry.Kind == AnimationPoseContributionKind.Inertial)
                    inertialCount++;
                scalarWeight += entry.ScalarWeight;
                leftFootWeight += entry.LeftFootWeight;
                rightFootWeight += entry.RightFootWeight;
            }
            if (!float.IsFinite(scalarWeight) || !float.IsFinite(leftFootWeight) || !float.IsFinite(rightFootWeight) ||
                scalarWeight > 1f + WeightTolerance || leftFootWeight > 1f + WeightTolerance ||
                rightFootWeight > 1f + WeightTolerance ||
                Mathf.Abs(scalarWeight - header.OutputWeight) > WeightTolerance ||
                liveCount > header.MaxActiveSourceEntries || storedCount > 1 || inertialCount > 1)
            {
                throw new ArgumentException();
            }
            if (header.UsesInertial)
            {
                if (header.ContributionCount != 2 || liveCount != 1 || storedCount != 0 || inertialCount != 1)
                    throw new ArgumentException();
            }
            else if (inertialCount != 0 ||
                     header.Kind == AnimationSlotBlendFramePlanKind.StoredCapture && storedCount != 1)
            {
                throw new ArgumentException();
            }
            bool hasOutputWeight = header.OutputWeight > 0f;
            for (int boneIndex = 0; boneIndex < header.BoneCount; boneIndex++)
            {
                float boneWeight = 0f;
                for (int contributionIndex = 0; contributionIndex < header.ContributionCount; contributionIndex++)
                {
                    float weight = plan.GetDenseBoneWeight(contributionIndex, boneIndex);
                    if (!IsNormalized(weight))
                        throw new ArgumentException();
                    boneWeight += weight;
                }
                if (!float.IsFinite(boneWeight) || boneWeight > 1f + WeightTolerance)
                {
                    throw new ArgumentException();
                }
                hasOutputWeight |= boneWeight > 0f;
                if (header.UsesInertial && !plan.GetInertialBone(boneIndex).IsValid)
                    throw new ArgumentException();
            }
            if (header.Availability == PoseSlotFrameAvailability.Pose && !hasOutputWeight)
                throw new ArgumentException();
            if (header.UsesInertial)
            {
                for (int parameterIndex = 0; parameterIndex < header.ParameterCount; parameterIndex++)
                {
                    if (!float.IsFinite(plan.GetInertialParameterResidualWeight(parameterIndex)))
                        throw new ArgumentException();
                }
            }
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
            RequireLength(binding.LeftFootFeatures, binding.SourceCapacity);
            RequireLength(binding.RightFootFeatures, binding.SourceCapacity);
            RequireLength(binding.VisualTimeScales, binding.SourceCapacity);
            RequireLength(binding.HasFootFeatures, binding.SourceCapacity);
            RequireLength(binding.CompletedAt, binding.SourceCapacity);
            RequireLength(binding.ProgramProducerIndices, binding.SourceCapacity);
        }

        static void RequireFinalBinding(
            AnimationPoseSlotNativeWriteBinding binding,
            AnimationSlotBlendFramePlanHeader header)
        {
            if (binding.CompletionIdentity != header.CompletionIdentity ||
                binding.Range.PhysicalSlotIndex != header.PhysicalSlotIndex ||
                binding.Range.ContributionCapacity != header.ContributionCapacity)
            {
                throw new ArgumentException();
            }
            RequireLength(binding.DenseLocalPoses, header.BoneCount);
            RequireLength(binding.DenseVelocities, header.BoneCount);
            RequireLength(binding.PoseParameters, header.ParameterCount);
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

            RequireLength(binding.Inertial.State, 1);
            RequireLength(binding.Inertial.PositionResiduals, header.BoneCount);
            RequireLength(binding.Inertial.RotationResiduals, header.BoneCount);
            RequireLength(binding.Inertial.ScaleResiduals, header.BoneCount);
            RequireLength(binding.Inertial.LinearVelocityResiduals, header.BoneCount);
            RequireLength(binding.Inertial.AngularVelocityResiduals, header.BoneCount);
            RequireLength(binding.Inertial.ScaleVelocityResiduals, header.BoneCount);
            RequireLength(binding.Inertial.ParameterResiduals, header.ParameterCount);
            RequireLength(binding.Inertial.DenseBoneOutputWeights, header.BoneCount);

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
