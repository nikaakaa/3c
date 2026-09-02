using System;
using ThirdPersonCharacter.Pipeline.Animation.BlendStack;
using ThirdPersonCharacter.Pipeline.Animation.Presentation;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonSimulation;
using Unity.Collections;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal abstract class CharacterPoseValueWorkspace
    {
        internal const float ScaleEpsilon = 0.000001f;

        [ReadOnly]
        internal NativeArray<CharacterPoseNativeOperationHeader> m_OperationHeaders;
        [ReadOnly]
        internal NativeArray<float> m_DenseBoneMasks;
        [ReadOnly]
        internal NativeArray<AnimationLocalBonePose> m_AdditiveReferences;
        [ReadOnly]
        internal NativeArray<PoseParameterResolvePolicy> m_ParameterPolicies;
        [ReadOnly]
        internal NativeArray<float> m_ParameterDefaults;
        [ReadOnly]
        internal NativeArray<int> m_ParentIndices;
        [ReadOnly]
        internal NativeArray<AnimationBlendCurveNativeEntry> m_BlendCurves;
        [ReadOnly]
        internal NativeArray<AnimationBlendCurveSegment> m_BlendCurveSegments;
        [ReadOnly]
        internal NativeArray<AnimationBlendProfileNativeEntry> m_BlendProfiles;
        [ReadOnly]
        internal NativeArray<float> m_BlendDenseProfiles;
        [ReadOnly]
        internal NativeArray<PoseInertializationNativeNode> m_Inertializations;
        [ReadOnly]
        internal NativeArray<AnimationPoseGraphNativeModifyBone> m_ModifyBones;
        [ReadOnly]
        internal NativeArray<AnimationPoseGraphNativeRootOrientationWarp> m_RootOrientationWarps;
        [ReadOnly]
        internal NativeArray<CharacterRootOrientationWarpNativeControl> m_RootOrientationWarpControls;
        [ReadOnly]
        internal NativeArray<AnimationPoseGraphNativeLinkedPoseCall> m_LinkedPoseCalls;
        [ReadOnly]
        internal NativeArray<AnimationPoseGraphNativeLinkedPoseCandidate> m_LinkedPoseCandidates;
        [ReadOnly]
        internal NativeArray<AnimationPoseGraphNativeLinkedPoseCallControl> m_LinkedPoseCallControls;
        [ReadOnly]
        internal NativeArray<byte> m_LinkedPoseActiveFragments;
        [ReadOnly]
        internal NativeArray<CharacterPoseStateMachineNativeControl> m_StateMachineControls;
        [ReadOnly]
        internal NativeArray<CharacterAnimationSlotNativeControl> m_AnimationSlotControls;
        [ReadOnly]
        internal NativeArray<PoseInertializationNativeRule> m_InertialRules;
        [ReadOnly]
        internal NativeArray<AnimationBlendCurveSegment> m_InertialCurveSegments;
        [ReadOnly]
        internal NativeArray<float> m_InertialDenseProfiles;
        [ReadOnly]
        internal NativeArray<PoseParameterInertializationMode> m_InertialParameterModes;
        internal NativeArray<PoseInertializationNativeState> m_InertialStates;
        internal NativeArray<AnimationLocalBonePose> m_InertialHistory;
        internal NativeArray<AnimationBlendBoneVelocity> m_InertialHistoryVelocities;
        internal NativeArray<float> m_InertialHistoryParameters;
        internal NativeArray<byte> m_InertialHistoryParameterAvailability;
        internal NativeArray<AnimationFootFeatureSample> m_InertialHistoryLeftFeet;
        internal NativeArray<AnimationFootFeatureSample> m_InertialHistoryRightFeet;
        internal NativeArray<byte> m_InertialHistoryHasFeet;
        internal NativeArray<AnimationFootFeatureSample> m_InertialAccumulatorLeftFeet;
        internal NativeArray<AnimationFootFeatureSample> m_InertialAccumulatorRightFeet;
        internal NativeArray<byte> m_InertialAccumulatorHasFeet;
        internal NativeArray<Vector3> m_InertialPositionResiduals;
        internal NativeArray<Vector3> m_InertialRotationResiduals;
        internal NativeArray<Vector3> m_InertialScaleResiduals;
        internal NativeArray<Vector3> m_InertialLinearVelocityResiduals;
        internal NativeArray<Vector3> m_InertialAngularVelocityResiduals;
        internal NativeArray<Vector3> m_InertialScaleVelocityResiduals;
        internal NativeArray<float> m_InertialParameterResiduals;
        internal NativeArray<byte> m_InertialResetRequests;
        [ReadOnly]
        internal NativeArray<PoseInertializationNativeState> m_CommittedInertialStates;
        [ReadOnly]
        internal NativeArray<AnimationLocalBonePose> m_CommittedInertialHistory;
        [ReadOnly]
        internal NativeArray<AnimationBlendBoneVelocity> m_CommittedInertialHistoryVelocities;
        [ReadOnly]
        internal NativeArray<float> m_CommittedInertialHistoryParameters;
        [ReadOnly]
        internal NativeArray<byte> m_CommittedInertialHistoryParameterAvailability;
        [ReadOnly]
        internal NativeArray<AnimationFootFeatureSample> m_CommittedInertialHistoryLeftFeet;
        [ReadOnly]
        internal NativeArray<AnimationFootFeatureSample> m_CommittedInertialHistoryRightFeet;
        [ReadOnly]
        internal NativeArray<byte> m_CommittedInertialHistoryHasFeet;
        [ReadOnly]
        internal NativeArray<AnimationFootFeatureSample> m_CommittedInertialAccumulatorLeftFeet;
        [ReadOnly]
        internal NativeArray<AnimationFootFeatureSample> m_CommittedInertialAccumulatorRightFeet;
        [ReadOnly]
        internal NativeArray<byte> m_CommittedInertialAccumulatorHasFeet;
        [ReadOnly]
        internal NativeArray<Vector3> m_CommittedInertialPositionResiduals;
        [ReadOnly]
        internal NativeArray<Vector3> m_CommittedInertialRotationResiduals;
        [ReadOnly]
        internal NativeArray<Vector3> m_CommittedInertialScaleResiduals;
        [ReadOnly]
        internal NativeArray<Vector3> m_CommittedInertialLinearVelocityResiduals;
        [ReadOnly]
        internal NativeArray<Vector3> m_CommittedInertialAngularVelocityResiduals;
        [ReadOnly]
        internal NativeArray<Vector3> m_CommittedInertialScaleVelocityResiduals;
        [ReadOnly]
        internal NativeArray<float> m_CommittedInertialParameterResiduals;

        [ReadOnly]
        internal NativeArray<AnimationPlayerPoseNativeRange> m_SlotRanges;
        [ReadOnly]
        internal NativeArray<AnimationLocalBonePose> m_SlotDenseLocalPoses;
        [ReadOnly]
        internal NativeArray<AnimationBlendBoneVelocity> m_SlotDenseVelocities;
        [ReadOnly]
        internal NativeArray<float> m_SlotPoseParameters;
        [ReadOnly]
        internal NativeArray<byte> m_SlotPoseParameterAvailability;
        [ReadOnly]
        internal NativeArray<AnimationPrimitivePoseContribution> m_SlotContributions;
        [ReadOnly]
        internal NativeArray<float> m_SlotDenseContributionWeights;
        [ReadOnly]
        internal NativeArray<int> m_SlotContributionCounts;
        [ReadOnly]
        internal NativeArray<float> m_SlotOutputWeights;
        [ReadOnly]
        internal NativeArray<AnimationFootFeatureSample> m_SlotLeftFootFeatures;
        [ReadOnly]
        internal NativeArray<AnimationFootFeatureSample> m_SlotRightFootFeatures;
        [ReadOnly]
        internal NativeArray<byte> m_SlotHasFootFeatures;
        [ReadOnly]
        internal NativeArray<AnimationPoseAvailability> m_SlotAvailability;
        [ReadOnly]
        internal NativeArray<ulong> m_SlotContinuityIdentities;
        [ReadOnly]
        internal NativeArray<PoseDiscontinuityNative> m_SlotDiscontinuities;
        [ReadOnly]
        internal NativeArray<AnimationPoseNativeInvalidReason> m_SlotInvalidReasons;
        [ReadOnly]
        internal NativeArray<ulong> m_SlotCompletedAt;

        internal NativeArray<AnimationLocalBonePose> m_ValueDenseLocalPoses;
        internal NativeArray<AnimationBlendBoneVelocity> m_ValueDenseVelocities;
        internal NativeArray<float> m_ValuePoseParameters;
        internal NativeArray<byte> m_ValuePoseParameterAvailability;
        internal NativeArray<AnimationPrimitivePoseContribution> m_ValueContributions;
        internal NativeArray<float> m_ValueDenseContributionWeights;
        internal NativeArray<int> m_ValueContributionCounts;
        internal NativeArray<float> m_ValueOutputWeights;
        internal NativeArray<AnimationFootFeatureSample> m_ValueLeftFootFeatures;
        internal NativeArray<AnimationFootFeatureSample> m_ValueRightFootFeatures;
        internal NativeArray<byte> m_ValueHasFootFeatures;
        internal NativeArray<AnimationPoseAvailability> m_ValueAvailability;
        internal NativeArray<ulong> m_ValueContinuityIdentities;
        internal NativeArray<PoseDiscontinuityNative> m_ValueDiscontinuities;
        internal NativeArray<AnimationPoseNativeInvalidReason> m_ValueInvalidReasons;
        internal CharacterPoseOperationCompletionPage m_OperationCompletions;
        internal NativeArray<ulong> m_StageCompletedAt;
        internal NativeArray<int> m_StageInvalidOperationIndex;
        internal NativeArray<AnimationPoseNativeInvalidReason> m_PoseGraphInvalidReason;
        internal NativeArray<int> m_PoseGraphInvalidOperationIndex;
        internal NativeArray<ulong> m_PoseGraphCompletedAt;
        internal CharacterPoseGraphNativeBinding m_FrameBinding;
        internal CharacterFinalPosePublicationOutputBinding m_FinalOutput;

        internal int m_PlayerCount;
        internal int m_BoneCount;
        internal int m_ParameterCount;
        internal int m_PoseValueCount;
        internal int m_ContributionStride;
        internal int m_OutputOperationIndex;
        internal int m_LeftFootBoneIndex;
        internal int m_RightFootBoneIndex;
        internal FixedString64Bytes m_RigId;
        internal FixedString64Bytes m_RigRevision;
        internal int m_AnimationSlotNodeOffset;
        internal ulong m_CompletionIdentity;
        internal CharacterPoseConstraintRuntime m_PoseConstraints;
        internal CharacterPoseProgramExecutionView m_Program;
        internal CharacterPoseProgramFramePages m_FramePages;
        internal PoseInertializationNativeProgram m_InertializationProgram;
        internal bool m_RecordDiagnostics;
        internal ulong m_FrameSequence;


        internal bool TryRequireInputs(
            in CharacterPoseNativeOperationHeader header,
            int output,
            int inputA,
            int inputB)
        {
            if (!IsInputReady(inputA, header.Index) ||
                !IsInputReady(inputB, header.Index))
            {
                SetInvalid(output, (ulong)header.Index + 1UL, AnimationPoseNativeInvalidReason.PoseGraphInputIncomplete, header.Index);
                return false;
            }
            AnimationPoseAvailability availabilityA = m_ValueAvailability[inputA];
            AnimationPoseAvailability availabilityB = m_ValueAvailability[inputB];
            if (!IsAvailability(availabilityA) || !IsAvailability(availabilityB))
            {
                SetInvalid(output, (ulong)header.Index + 1UL, AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid, header.Index);
                return false;
            }
            if (availabilityA == AnimationPoseAvailability.Invalid ||
                availabilityB == AnimationPoseAvailability.Invalid)
            {
                AnimationPoseNativeInvalidReason reason = availabilityA == AnimationPoseAvailability.Invalid
                    ? NormalizeInvalidReason(m_ValueInvalidReasons[inputA])
                    : NormalizeInvalidReason(m_ValueInvalidReasons[inputB]);
                SetInvalid(
                    output,
                    CombineContinuity(
                        m_ValueContinuityIdentities[inputA],
                        m_ValueContinuityIdentities[inputB],
                        output),
                    reason,
                    header.Index);
                return false;
            }
            return true;
        }

        internal bool TryCopyValue(int source, int destination, int operationIndex)
        {
            return TryCopyValue(
                source,
                destination,
                operationIndex,
                true);
        }

        internal bool TryCopyValueWithoutPose(
            int source,
            int destination,
            int operationIndex)
        {
            return TryCopyValue(
                source,
                destination,
                operationIndex,
                false);
        }

        internal bool TryCopyValue(
            int source,
            int destination,
            int operationIndex,
            bool copyPose)
        {
            if (source < 0 || source >= m_PoseValueCount || destination < 0 || destination >= m_PoseValueCount)
                return false;
            int contributionCount = m_ValueContributionCounts[source];
            if (contributionCount < 0 || contributionCount > m_ContributionStride)
                return false;

            AnimationPoseAvailability availability = m_ValueAvailability[source];
            m_ValueAvailability[destination] = availability;
            m_ValueOutputWeights[destination] = m_ValueOutputWeights[source];
            m_ValueContinuityIdentities[destination] = CombineContinuity(
                m_ValueContinuityIdentities[source],
                (ulong)operationIndex + 1UL,
                operationIndex);
            m_ValueDiscontinuities[destination] = default;
            m_ValueInvalidReasons[destination] = m_ValueInvalidReasons[source];
            if (availability == AnimationPoseAvailability.Pose)
            {
                if (copyPose)
                {
                    NativeArray<AnimationLocalBonePose>.Copy(
                        m_ValueDenseLocalPoses,
                        PoseOffset(source),
                        m_ValueDenseLocalPoses,
                        PoseOffset(destination),
                        m_BoneCount);
                }
                NativeArray<AnimationBlendBoneVelocity>.Copy(
                    m_ValueDenseVelocities,
                    PoseOffset(source),
                    m_ValueDenseVelocities,
                    PoseOffset(destination),
                    m_BoneCount);
            }
            NativeArray<float>.Copy(
                m_ValuePoseParameters,
                ParameterOffset(source),
                m_ValuePoseParameters,
                ParameterOffset(destination),
                m_ParameterCount);
            NativeArray<byte>.Copy(
                m_ValuePoseParameterAvailability,
                ParameterOffset(source),
                m_ValuePoseParameterAvailability,
                ParameterOffset(destination),
                m_ParameterCount);
            m_ValueContributionCounts[destination] = contributionCount;
            if (contributionCount > 0)
            {
                NativeArray<AnimationPrimitivePoseContribution>.Copy(
                    m_ValueContributions,
                    ContributionOffset(source),
                    m_ValueContributions,
                    ContributionOffset(destination),
                    contributionCount);
                NativeArray<float>.Copy(
                    m_ValueDenseContributionWeights,
                    ContributionBoneOffset(source),
                    m_ValueDenseContributionWeights,
                    ContributionBoneOffset(destination),
                    contributionCount * m_BoneCount);
            }
            m_ValueLeftFootFeatures[destination] = m_ValueLeftFootFeatures[source];
            m_ValueRightFootFeatures[destination] = m_ValueRightFootFeatures[source];
            m_ValueHasFootFeatures[destination] = m_ValueHasFootFeatures[source];
            return true;
        }

        internal bool TryScaleValue(
            int value,
            float weight,
            int boneMaskOffset)
        {
            if (m_ValueAvailability[value] != AnimationPoseAvailability.Pose)
                return true;
            float outputWeight = m_ValueOutputWeights[value] * weight;
            if (!IsWeight(outputWeight))
                return false;
            m_ValueOutputWeights[value] = outputWeight;
            int count = m_ValueContributionCounts[value];
            for (int contribution = 0; contribution < count; contribution++)
            {
                AnimationPrimitivePoseContribution source =
                    m_ValueContributions[ContributionOffset(value) + contribution];
                float scalarWeight = source.Weight * weight;
                float leftWeight = source.LeftFootWeight *
                    GetMaskWeight(boneMaskOffset, m_LeftFootBoneIndex) * weight;
                float rightWeight = source.RightFootWeight *
                    GetMaskWeight(boneMaskOffset, m_RightFootBoneIndex) * weight;
                if (!IsWeight(scalarWeight) || !IsWeight(leftWeight) || !IsWeight(rightWeight))
                    return false;
                m_ValueContributions[ContributionOffset(value) + contribution] =
                    new AnimationPrimitivePoseContribution(
                        source.PhysicalPlayerIndex,
                        source.PhysicalSourceIndex,
                        source.PhysicalSourceGeneration,
                        source.Kind,
                        source.SourceOwnerIndex,
                        source.ContributionContinuityIdentity,
                        scalarWeight,
                        leftWeight,
                        rightWeight);
                for (int bone = 0; bone < m_BoneCount; bone++)
                {
                    float boneWeight =
                        GetContributionBoneWeight(value, contribution, bone) *
                        GetMaskWeight(boneMaskOffset, bone) * weight;
                    if (!IsWeight(boneWeight))
                        return false;
                    SetContributionBoneWeight(
                        value,
                        contribution,
                        bone,
                        boneWeight);
                }
            }
            return true;
        }

        internal bool TryResolveParameters(
            float weight,
            int parameterPolicyOffset,
            int baseValue,
            int overlayValue,
            int output)
        {
            float baseWeight = m_ValueOutputWeights[baseValue];
            float overlayWeight = m_ValueOutputWeights[overlayValue] * weight;
            if (!IsWeight(baseWeight) || !IsWeight(overlayWeight))
                return false;
            for (int parameter = 0; parameter < m_ParameterCount; parameter++)
            {
                int baseOffset = ParameterOffset(baseValue) + parameter;
                int overlayOffset = ParameterOffset(overlayValue) + parameter;
                int outputOffset = ParameterOffset(output) + parameter;
                float baseParameter = m_ValuePoseParameters[baseOffset];
                float overlayParameter = m_ValuePoseParameters[overlayOffset];
                if (!float.IsFinite(baseParameter) || !float.IsFinite(overlayParameter))
                    return false;
                bool baseAvailable = m_ValuePoseParameterAvailability[baseOffset] != 0;
                bool overlayAvailable = m_ValuePoseParameterAvailability[overlayOffset] != 0;
                PoseParameterResolvePolicy policy =
                    m_ParameterPolicies[parameterPolicyOffset + parameter];
                float value;
                bool available;
                switch (policy)
                {
                    case PoseParameterResolvePolicy.Base:
                        available = baseAvailable;
                        value = available ? baseParameter : m_ParameterDefaults[parameter];
                        break;
                    case PoseParameterResolvePolicy.Overlay:
                        available = overlayWeight > 0f && overlayAvailable || baseAvailable;
                        value = overlayWeight > 0f && overlayAvailable
                            ? overlayParameter
                            : baseAvailable ? baseParameter : m_ParameterDefaults[parameter];
                        break;
                    case PoseParameterResolvePolicy.Weighted:
                        float resolvedBaseWeight = baseAvailable ? baseWeight : 0f;
                        float resolvedOverlayWeight = overlayAvailable ? overlayWeight : 0f;
                        float total = resolvedBaseWeight + resolvedOverlayWeight;
                        available = total > 0f;
                        value = total > 0f
                            ? (baseParameter * resolvedBaseWeight + overlayParameter * resolvedOverlayWeight) / total
                            : m_ParameterDefaults[parameter];
                        break;
                    case PoseParameterResolvePolicy.Max:
                        available = baseAvailable || overlayAvailable;
                        value = baseAvailable && overlayAvailable
                            ? Mathf.Max(baseParameter, overlayParameter)
                            : baseAvailable ? baseParameter : overlayAvailable ? overlayParameter : m_ParameterDefaults[parameter];
                        break;
                    case PoseParameterResolvePolicy.Min:
                        available = baseAvailable || overlayAvailable;
                        value = baseAvailable && overlayAvailable
                            ? Mathf.Min(baseParameter, overlayParameter)
                            : baseAvailable ? baseParameter : overlayAvailable ? overlayParameter : m_ParameterDefaults[parameter];
                        break;
                    default:
                        return false;
                }
                if (!float.IsFinite(value))
                    return false;
                m_ValuePoseParameters[outputOffset] = value;
                m_ValuePoseParameterAvailability[outputOffset] = available ? (byte)1 : (byte)0;
            }
            return true;
        }

        internal bool TryCopyParameters(int source, int output)
        {
            for (int parameter = 0; parameter < m_ParameterCount; parameter++)
            {
                float value = m_ValuePoseParameters[ParameterOffset(source) + parameter];
                if (!float.IsFinite(value))
                    return false;
                m_ValuePoseParameters[ParameterOffset(output) + parameter] = value;
                m_ValuePoseParameterAvailability[ParameterOffset(output) + parameter] =
                    m_ValuePoseParameterAvailability[ParameterOffset(source) + parameter];
            }
            m_ValueDiscontinuities[output] = m_ValueDiscontinuities[source];
            return true;
        }

        internal bool TryMergeContributions(
            float weight,
            int boneMaskOffset,
            int baseValue,
            int overlayValue,
            int output,
            bool additive)
        {
            for (int contribution = 0; contribution < m_ValueContributionCounts[baseValue]; contribution++)
            {
                if (!TryAddContribution(
                        weight,
                        boneMaskOffset,
                        baseValue,
                        contribution,
                        overlayValue,
                        output,
                        false,
                        additive))
                {
                    return false;
                }
            }
            for (int contribution = 0; contribution < m_ValueContributionCounts[overlayValue]; contribution++)
            {
                if (!TryAddContribution(
                        weight,
                        boneMaskOffset,
                        overlayValue,
                        contribution,
                        overlayValue,
                        output,
                        true,
                        additive))
                {
                    return false;
                }
            }
            return true;
        }

        internal bool TryAddContribution(
            float weight,
            int boneMaskOffset,
            int sourceValue,
            int sourceIndex,
            int overlayValue,
            int output,
            bool overlay,
            bool additive)
        {
            AnimationPrimitivePoseContribution source =
                m_ValueContributions[ContributionOffset(sourceValue) + sourceIndex];
            if (!IsValidPrimitiveContribution(source))
                return false;

            float scalarFactor;
            float leftFactor;
            float rightFactor;
            if (overlay)
            {
                scalarFactor = weight;
                leftFactor = GetMaskWeight(boneMaskOffset, m_LeftFootBoneIndex) * weight;
                rightFactor = GetMaskWeight(boneMaskOffset, m_RightFootBoneIndex) * weight;
            }
            else if (additive)
            {
                scalarFactor = 1f;
                leftFactor = 1f;
                rightFactor = 1f;
            }
            else
            {
                if (!TryGetBoneOutputWeight(overlayValue, m_LeftFootBoneIndex, out float leftOverlay) ||
                    !TryGetBoneOutputWeight(overlayValue, m_RightFootBoneIndex, out float rightOverlay))
                {
                    return false;
                }
                scalarFactor = 1f - m_ValueOutputWeights[overlayValue] * weight;
                leftFactor = 1f - leftOverlay *
                    GetMaskWeight(boneMaskOffset, m_LeftFootBoneIndex) * weight;
                rightFactor = 1f - rightOverlay *
                    GetMaskWeight(boneMaskOffset, m_RightFootBoneIndex) * weight;
            }

            float scalarWeight = source.Weight * Mathf.Clamp01(scalarFactor);
            float leftWeight = source.LeftFootWeight * Mathf.Clamp01(leftFactor);
            float rightWeight = source.RightFootWeight * Mathf.Clamp01(rightFactor);
            if (!IsWeight(scalarWeight) || !IsWeight(leftWeight) || !IsWeight(rightWeight))
                return false;

            int targetIndex = FindContribution(output, source);
            if (targetIndex < 0)
            {
                targetIndex = m_ValueContributionCounts[output];
                if (targetIndex >= m_ContributionStride)
                    return false;
                m_ValueContributionCounts[output] = targetIndex + 1;
                ClearContributionWeights(output, targetIndex);
                m_ValueContributions[ContributionOffset(output) + targetIndex] =
                    new AnimationPrimitivePoseContribution(
                        source.PhysicalPlayerIndex,
                        source.PhysicalSourceIndex,
                        source.PhysicalSourceGeneration,
                        source.Kind,
                        source.SourceOwnerIndex,
                        source.ContributionContinuityIdentity,
                        scalarWeight,
                        leftWeight,
                        rightWeight);
            }
            else
            {
                AnimationPrimitivePoseContribution current =
                    m_ValueContributions[ContributionOffset(output) + targetIndex];
                m_ValueContributions[ContributionOffset(output) + targetIndex] =
                    new AnimationPrimitivePoseContribution(
                        current.PhysicalPlayerIndex,
                        current.PhysicalSourceIndex,
                        current.PhysicalSourceGeneration,
                        current.Kind,
                        current.SourceOwnerIndex,
                        current.ContributionContinuityIdentity,
                        Mathf.Clamp01(current.Weight + scalarWeight),
                        Mathf.Clamp01(current.LeftFootWeight + leftWeight),
                        Mathf.Clamp01(current.RightFootWeight + rightWeight));
            }

            for (int bone = 0; bone < m_BoneCount; bone++)
            {
                float factor;
                if (overlay)
                {
                    factor = GetMaskWeight(boneMaskOffset, bone) * weight;
                }
                else if (additive)
                {
                    factor = 1f;
                }
                else
                {
                    if (!TryGetBoneOutputWeight(overlayValue, bone, out float overlayOutput))
                        return false;
                    factor = 1f - overlayOutput *
                        GetMaskWeight(boneMaskOffset, bone) * weight;
                }
                float boneWeight = GetContributionBoneWeight(
                    sourceValue,
                    sourceIndex,
                    bone) * Mathf.Clamp01(factor);
                float combined = Mathf.Clamp01(
                    GetContributionBoneWeight(output, targetIndex, bone) +
                    boneWeight);
                if (!IsWeight(combined))
                    return false;
                SetContributionBoneWeight(output, targetIndex, bone, combined);
            }
            return true;
        }

        internal bool TryResolveFootFeatures(
            float weight,
            int boneMaskOffset,
            int baseValue,
            int overlayValue,
            int output,
            bool additive)
        {
            bool hasBase = m_ValueHasFootFeatures[baseValue] == 1;
            bool hasOverlay = m_ValueHasFootFeatures[overlayValue] == 1;
            if (!hasBase && !hasOverlay)
                return true;
            if (!TryGetBoneOutputWeight(overlayValue, m_LeftFootBoneIndex, out float leftOutput) ||
                !TryGetBoneOutputWeight(overlayValue, m_RightFootBoneIndex, out float rightOutput))
            {
                return false;
            }
            float left = leftOutput *
                GetMaskWeight(boneMaskOffset, m_LeftFootBoneIndex) * weight;
            float right = rightOutput *
                GetMaskWeight(boneMaskOffset, m_RightFootBoneIndex) * weight;
            if (additive)
            {
                left = left / (1f + left);
                right = right / (1f + right);
            }
            if (!TryResolveFeature(
                    hasBase,
                    m_ValueLeftFootFeatures[baseValue],
                    hasOverlay,
                    m_ValueLeftFootFeatures[overlayValue],
                    left,
                    hasOverlay && left > 0f,
                    out AnimationFootFeatureSample leftFeature) ||
                !TryResolveFeature(
                    hasBase,
                    m_ValueRightFootFeatures[baseValue],
                    hasOverlay,
                    m_ValueRightFootFeatures[overlayValue],
                    right,
                    hasOverlay && right > 0f,
                    out AnimationFootFeatureSample rightFeature))
            {
                return false;
            }
            m_ValueLeftFootFeatures[output] = leftFeature;
            m_ValueRightFootFeatures[output] = rightFeature;
            m_ValueHasFootFeatures[output] = leftFeature.IsValid && rightFeature.IsValid ? (byte)1 : (byte)0;
            return true;
        }

        internal bool TryValidateValueEnvelope(
            int value,
            out AnimationPoseNativeInvalidReason reason)
        {
            reason = AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid;
            AnimationPoseAvailability availability = m_ValueAvailability[value];
            AnimationPoseNativeInvalidReason invalidReason = m_ValueInvalidReasons[value];
            int contributionCount = m_ValueContributionCounts[value];
            byte hasFootFeatures = m_ValueHasFootFeatures[value];
            if (!IsAvailability(availability) ||
                !IsWeight(m_ValueOutputWeights[value]) ||
                m_ValueContinuityIdentities[value] == 0 ||
                contributionCount < 0 || contributionCount > m_ContributionStride ||
                hasFootFeatures > 1)
            {
                return false;
            }
            for (int parameter = 0; parameter < m_ParameterCount; parameter++)
            {
                byte parameterAvailable = m_ValuePoseParameterAvailability[ParameterOffset(value) + parameter];
                if (!float.IsFinite(m_ValuePoseParameters[ParameterOffset(value) + parameter]) || parameterAvailable > 1)
                    return false;
            }

            if (availability == AnimationPoseAvailability.Invalid)
            {
                reason = NormalizeInvalidReason(invalidReason);
                return invalidReason != AnimationPoseNativeInvalidReason.None &&
                       contributionCount == 0 && m_ValueOutputWeights[value] == 0f && hasFootFeatures == 0;
            }
            if (invalidReason != AnimationPoseNativeInvalidReason.None)
                return false;
            if (availability == AnimationPoseAvailability.NoPose)
            {
                return contributionCount == 0 && m_ValueOutputWeights[value] == 0f && hasFootFeatures == 0;
            }
            if (contributionCount <= 0)
                return false;
            if (hasFootFeatures == 1 &&
                (!IsValidFootFeature(m_ValueLeftFootFeatures[value]) ||
                 !IsValidFootFeature(m_ValueRightFootFeatures[value])))
            {
                return false;
            }
            reason = AnimationPoseNativeInvalidReason.None;
            return true;
        }

        internal bool TryValidateValueDeep(
            int value,
            out AnimationPoseNativeInvalidReason reason)
        {
            if (!TryValidateValueEnvelope(value, out reason))
                return false;
            if (m_ValueAvailability[value] != AnimationPoseAvailability.Pose)
                return true;
            int contributionCount = m_ValueContributionCounts[value];
            for (int bone = 0; bone < m_BoneCount; bone++)
            {
                if (!m_ValueDenseLocalPoses[PoseOffset(value) + bone].IsValid)
                    return false;
            }
            for (int contribution = 0; contribution < contributionCount; contribution++)
            {
                if (!IsValidPrimitiveContribution(
                        m_ValueContributions[ContributionOffset(value) + contribution]))
                {
                    return false;
                }
                for (int bone = 0; bone < m_BoneCount; bone++)
                {
                    if (!IsWeight(GetContributionBoneWeight(value, contribution, bone)))
                        return false;
                }
            }
            reason = AnimationPoseNativeInvalidReason.None;
            return true;
        }

        internal bool IsInputReady(int value, int operationIndex)
        {
            if (value < 0 || value >= m_PoseValueCount)
                return false;
            for (int i = 0; i < m_OperationHeaders.Length; i++)
            {
                CharacterPoseNativeOperationHeader candidate =
                    m_OperationHeaders[i];
                if (candidate.Index < operationIndex &&
                    candidate.OutputPoseValueIndex == value)
                    return m_OperationCompletions[
                        candidate.FrameCacheIndex].Matches(
                        m_CompletionIdentity);
            }
            return false;
        }

        internal bool TryGetBoneOutputWeight(int value, int bone, out float result)
        {
            result = 0f;
            int count = m_ValueContributionCounts[value];
            if (count < 0 || count > m_ContributionStride || bone < 0 || bone >= m_BoneCount)
                return false;
            for (int contribution = 0; contribution < count; contribution++)
            {
                float weight = GetContributionBoneWeight(value, contribution, bone);
                if (!IsWeight(weight))
                    return false;
                result += weight;
                if (!float.IsFinite(result))
                    return false;
            }
            result = Mathf.Clamp01(result);
            return true;
        }

        internal int FindContribution(int value, AnimationPrimitivePoseContribution source)
        {
            int count = m_ValueContributionCounts[value];
            for (int contribution = 0; contribution < count; contribution++)
            {
                AnimationPrimitivePoseContribution candidate =
                    m_ValueContributions[ContributionOffset(value) + contribution];
                if (candidate.PhysicalPlayerIndex == source.PhysicalPlayerIndex &&
                    candidate.PhysicalSourceIndex == source.PhysicalSourceIndex &&
                    candidate.PhysicalSourceGeneration == source.PhysicalSourceGeneration &&
                    candidate.Kind == source.Kind &&
                    candidate.SourceOwnerIndex == source.SourceOwnerIndex &&
                    candidate.ContributionContinuityIdentity == source.ContributionContinuityIdentity)
                {
                    return contribution;
                }
            }
            return -1;
        }

        internal void ResetValue(int value)
        {
            m_ValueContributionCounts[value] = 0;
            m_ValueOutputWeights[value] = 0f;
            m_ValueLeftFootFeatures[value] = default;
            m_ValueRightFootFeatures[value] = default;
            m_ValueHasFootFeatures[value] = 0;
            m_ValueAvailability[value] = AnimationPoseAvailability.Invalid;
            m_ValueContinuityIdentities[value] = 1;
            m_ValueDiscontinuities[value] = default;
            m_ValueInvalidReasons[value] = AnimationPoseNativeInvalidReason.None;
            for (int parameter = 0; parameter < m_ParameterCount; parameter++)
            {
                m_ValuePoseParameters[ParameterOffset(value) + parameter] = m_ParameterDefaults[parameter];
                m_ValuePoseParameterAvailability[ParameterOffset(value) + parameter] = 0;
            }
        }

        internal void ClearContributionWeights(int value, int contribution)
        {
            int offset = ContributionBoneOffset(value) + contribution * m_BoneCount;
            for (int bone = 0; bone < m_BoneCount; bone++)
                m_ValueDenseContributionWeights[offset + bone] = 0f;
        }

        internal void SetInvalid(
            int value,
            ulong continuity,
            AnimationPoseNativeInvalidReason reason,
            int operationIndex)
        {
            reason = NormalizeInvalidReason(reason);
            m_ValueContributionCounts[value] = 0;
            m_ValueOutputWeights[value] = 0f;
            m_ValueLeftFootFeatures[value] = default;
            m_ValueRightFootFeatures[value] = default;
            m_ValueHasFootFeatures[value] = 0;
            m_ValueAvailability[value] = AnimationPoseAvailability.Invalid;
            m_ValueContinuityIdentities[value] = RequireIdentity(continuity);
            m_ValueDiscontinuities[value] = default;
            m_ValueInvalidReasons[value] = reason;
            RecordGraphInvalid(reason, operationIndex);
        }

        internal void RecordGraphInvalid(AnimationPoseNativeInvalidReason reason, int operationIndex)
        {
            if (m_PoseGraphInvalidReason[0] != AnimationPoseNativeInvalidReason.None)
                return;
            m_PoseGraphInvalidReason[0] = NormalizeInvalidReason(reason);
            m_PoseGraphInvalidOperationIndex[0] = operationIndex;
        }

        internal bool TryAddMeshPose(
            int baseValue,
            int additiveValue,
            int outputValue,
            in CharacterPoseNativeCompositionOperation operation,
            int bone,
            float weight,
            out AnimationLocalBonePose result)
        {
            result = default;
            if (!TryResolveModelPose(baseValue, bone, out AnimationLocalBonePose basePose) ||
                !TryResolveModelPose(additiveValue, bone, out AnimationLocalBonePose additivePose) ||
                !TryAddPose(
                    basePose,
                    additivePose,
                    m_AdditiveReferences[operation.AdditiveReferenceOffset + bone],
                    operation.AdditiveScalePolicy,
                    weight,
                    out AnimationLocalBonePose modelResult))
            {
                return false;
            }
            int parentIndex = m_ParentIndices[bone];
            if (parentIndex < 0)
            {
                result = modelResult;
                return true;
            }
            return TryResolveModelPose(outputValue, parentIndex, out AnimationLocalBonePose outputParent) &&
                   TryToLocal(outputParent, modelResult, out result);
        }

        internal bool TryResolveModelPose(int value, int bone, out AnimationLocalBonePose result)
        {
            result = m_ValueDenseLocalPoses[PoseOffset(value) + bone];
            if (!result.IsValid)
                return false;
            int parentIndex = m_ParentIndices[bone];
            while (parentIndex >= 0)
            {
                AnimationLocalBonePose parent = m_ValueDenseLocalPoses[PoseOffset(value) + parentIndex];
                if (!TryToModel(parent, result, out result))
                    return false;
                parentIndex = m_ParentIndices[parentIndex];
            }
            return true;
        }

        internal bool AssignPose(int value, int bone, AnimationLocalBonePose pose)
        {
            if (!pose.IsValid)
                return false;
            m_ValueDenseLocalPoses[PoseOffset(value) + bone] = pose;
            return true;
        }

        internal float GetMaskWeight(int boneMaskOffset, int bone) =>
            boneMaskOffset < 0
                ? 1f
                : m_DenseBoneMasks[boneMaskOffset + bone];

        internal float GetContributionBoneWeight(int value, int contribution, int bone) =>
            m_ValueDenseContributionWeights[
                ContributionBoneOffset(value) + contribution * m_BoneCount + bone];

        internal void SetContributionBoneWeight(int value, int contribution, int bone, float weight)
        {
            m_ValueDenseContributionWeights[
                ContributionBoneOffset(value) + contribution * m_BoneCount + bone] = weight;
        }

        internal int PoseOffset(int value) => value * m_BoneCount;
        internal int ParameterOffset(int value) => value * m_ParameterCount;
        internal int ContributionOffset(int value) => value * m_ContributionStride;
        internal int ContributionBoneOffset(int value) => value * m_ContributionStride * m_BoneCount;

        internal static bool TryBlendPose(
            AnimationLocalBonePose from,
            AnimationLocalBonePose to,
            float weight,
            out AnimationLocalBonePose result)
        {
            result = default;
            if (!from.IsValid || !to.IsValid || !IsWeight(weight))
                return false;
            Quaternion target = to.Rotation;
            if (Quaternion.Dot(from.Rotation, target) < 0f)
                target = new Quaternion(-target.x, -target.y, -target.z, -target.w);
            return TryCreatePose(
                Vector3.LerpUnclamped(from.Position, to.Position, weight),
                Quaternion.SlerpUnclamped(from.Rotation, target, weight),
                Vector3.LerpUnclamped(from.Scale, to.Scale, weight),
                out result);
        }

        internal static bool TryAddPose(
            AnimationLocalBonePose basePose,
            AnimationLocalBonePose additivePose,
            AnimationLocalBonePose referencePose,
            AdditiveScalePolicy scalePolicy,
            float weight,
            out AnimationLocalBonePose result)
        {
            result = default;
            if (!basePose.IsValid || !additivePose.IsValid || !referencePose.IsValid || !IsWeight(weight))
                return false;
            Quaternion delta = additivePose.Rotation * Quaternion.Inverse(referencePose.Rotation);
            if (delta.w < 0f)
                delta = new Quaternion(-delta.x, -delta.y, -delta.z, -delta.w);
            Quaternion rotation = basePose.Rotation *
                                  Quaternion.SlerpUnclamped(Quaternion.identity, delta, weight);
            Vector3 scale;
            switch (scalePolicy)
            {
                case AdditiveScalePolicy.Multiply:
                    if (!TryDivide(additivePose.Scale, referencePose.Scale, out Vector3 scaleRatio))
                        return false;
                    scale = Vector3.Scale(
                        basePose.Scale,
                        Vector3.LerpUnclamped(Vector3.one, scaleRatio, weight));
                    break;
                case AdditiveScalePolicy.AddDelta:
                    scale = basePose.Scale + (additivePose.Scale - referencePose.Scale) * weight;
                    break;
                case AdditiveScalePolicy.Ignore:
                    scale = basePose.Scale;
                    break;
                default:
                    return false;
            }
            return TryCreatePose(
                basePose.Position + (additivePose.Position - referencePose.Position) * weight,
                rotation,
                scale,
                out result);
        }

        internal static bool TryToModel(
            AnimationLocalBonePose parent,
            AnimationLocalBonePose local,
            out AnimationLocalBonePose result)
        {
            result = default;
            if (!parent.IsValid || !local.IsValid)
                return false;
            return TryCreatePose(
                parent.Position + parent.Rotation * Vector3.Scale(parent.Scale, local.Position),
                parent.Rotation * local.Rotation,
                Vector3.Scale(parent.Scale, local.Scale),
                out result);
        }

        internal static bool TryToLocal(
            AnimationLocalBonePose parent,
            AnimationLocalBonePose model,
            out AnimationLocalBonePose result)
        {
            result = default;
            if (!parent.IsValid || !model.IsValid)
                return false;
            Quaternion inverse = Quaternion.Inverse(parent.Rotation);
            if (!TryDivide(inverse * (model.Position - parent.Position), parent.Scale, out Vector3 position) ||
                !TryDivide(model.Scale, parent.Scale, out Vector3 scale))
            {
                return false;
            }
            return TryCreatePose(position, inverse * model.Rotation, scale, out result);
        }

        internal static bool TryDivide(Vector3 value, Vector3 divisor, out Vector3 result)
        {
            result = default;
            if (!IsFinite(value) || !IsFinite(divisor) ||
                Mathf.Abs(divisor.x) <= ScaleEpsilon ||
                Mathf.Abs(divisor.y) <= ScaleEpsilon ||
                Mathf.Abs(divisor.z) <= ScaleEpsilon)
            {
                return false;
            }
            result = new Vector3(
                value.x / divisor.x,
                value.y / divisor.y,
                value.z / divisor.z);
            return IsFinite(result);
        }

        internal static bool TryCreatePose(
            Vector3 position,
            Quaternion rotation,
            Vector3 scale,
            out AnimationLocalBonePose result)
        {
            result = default;
            if (!IsFinite(position) || !IsFinite(rotation) || !IsFinite(scale) ||
                Quaternion.Dot(rotation, rotation) <= 0f)
            {
                return false;
            }
            result = new AnimationLocalBonePose(position, rotation, scale);
            return result.IsValid;
        }

        internal static bool TryResolveFeature(
            bool hasBase,
            AnimationFootFeatureSample baseValue,
            bool hasOverlay,
            AnimationFootFeatureSample overlayValue,
            float weight,
            bool overlayPredictionAuthoritative,
            out AnimationFootFeatureSample result)
        {
            result = default;
            if (!hasBase)
            {
                if (!hasOverlay || !IsValidFootFeature(overlayValue))
                    return false;
                result = overlayValue;
                return true;
            }
            if (!IsValidFootFeature(baseValue))
                return false;
            if (!hasOverlay)
            {
                result = baseValue;
                return true;
            }
            if (!IsValidFootFeature(overlayValue) || !float.IsFinite(weight))
                return false;
            float t = Mathf.Clamp01(weight);
            Vector3 velocity = Vector3.LerpUnclamped(
                baseValue.SoleLocalVelocity,
                overlayValue.SoleLocalVelocity,
                t);
            float height = Mathf.LerpUnclamped(baseValue.SoleHeight, overlayValue.SoleHeight, t);
            float plant = Mathf.LerpUnclamped(baseValue.PlantConfidence, overlayValue.PlantConfidence, t);
            AnimationPredictedFootStepSample predicted = overlayPredictionAuthoritative
                ? overlayValue.PredictedStep
                : baseValue.PredictedStep;
            AnimationPredictedFootStepSample incomingPredicted = overlayPredictionAuthoritative
                ? overlayValue.IncomingPredictedStep
                : baseValue.IncomingPredictedStep;
            if (!IsFinite(velocity) || !float.IsFinite(height) || !IsWeight(plant))
            {
                return false;
            }
            result = new AnimationFootFeatureSample(
                velocity,
                height,
                plant,
                predicted,
                incomingPredicted);
            return result.IsValid;
        }

        internal static bool TryResolveStateMachineFeature(
            bool hasSource,
            AnimationFootFeatureSample source,
            bool hasTarget,
            AnimationFootFeatureSample target,
            float weight,
            out AnimationFootFeatureSample result)
        {
            if (!TryResolveFeature(
                    hasSource,
                    source,
                    hasTarget,
                    target,
                    weight,
                    true,
                    out result))
            {
                return false;
            }
            return result.IsValid;
        }

        internal static bool IsValidPrimitiveContribution(AnimationPrimitivePoseContribution contribution)
        {
            int kind = (int)contribution.Kind;
            bool live = contribution.Kind == AnimationPoseContributionKind.Live;
            return contribution.PhysicalPlayerIndex >= 0 &&
                   kind >= (int)AnimationPoseContributionKind.Live &&
                   kind <= (int)AnimationPoseContributionKind.Stored &&
                   (live
                       ? contribution.PhysicalSourceIndex >= 0 &&
                         contribution.PhysicalSourceGeneration != 0 &&
                         contribution.SourceOwnerIndex >= 0
                       : contribution.PhysicalSourceIndex == -1 &&
                         contribution.PhysicalSourceGeneration == 0 &&
                         contribution.SourceOwnerIndex == -1) &&
                   contribution.ContributionContinuityIdentity != 0 &&
                   IsWeight(contribution.Weight) &&
                   IsWeight(contribution.LeftFootWeight) &&
                   IsWeight(contribution.RightFootWeight);
        }

        internal static bool IsValidFootFeature(AnimationFootFeatureSample sample) =>
            sample.IsValid &&
            IsFinite(sample.SoleLocalVelocity) &&
            float.IsFinite(sample.SoleHeight) &&
            IsWeight(sample.PlantConfidence) &&
            (!sample.PredictedStep.IsValid ||
             IsWeight(sample.PredictedStep.Confidence) &&
             float.IsFinite(sample.PredictedStep.TimeToLandingSeconds) &&
             sample.PredictedStep.TimeToLandingSeconds >= 0f &&
             IsWeight(sample.PredictedStep.EventPhase) &&
             IsWeight(sample.PredictedStep.LiftOffPhase) &&
             IsValidRootLocalFootRoute(sample.PredictedStep)) &&
            (!sample.IncomingPredictedStep.IsValid ||
             IsWeight(sample.IncomingPredictedStep.Confidence) &&
             float.IsFinite(sample.IncomingPredictedStep.TimeToLandingSeconds) &&
             sample.IncomingPredictedStep.TimeToLandingSeconds >= 0f &&
             IsWeight(sample.IncomingPredictedStep.EventPhase) &&
             IsWeight(sample.IncomingPredictedStep.LiftOffPhase) &&
             IsValidRootLocalFootRoute(sample.IncomingPredictedStep));

        internal static bool IsValidRootLocalFootRoute(AnimationPredictedFootStepSample value)
        {
            if (value.Route.RootLocalFoot.Length != AnimationPredictedFootStepCurveSet.RouteSampleCount ||
                value.Route.RootLocalAnkle.Length != AnimationPredictedFootStepCurveSet.RouteSampleCount ||
                value.Route.RootLocalHip.Length != AnimationPredictedFootStepCurveSet.RouteSampleCount ||
                value.Route.AuthoredFootPlanar.Length != AnimationPredictedFootStepCurveSet.RouteSampleCount ||
                value.Route.AnimationClearance.Length != AnimationPredictedFootStepCurveSet.RouteSampleCount ||
                !IsWeight(value.LandingPhase) ||
                !IsFinite(value.OpposingRootLocalSoleRotation) ||
                Quaternion.Dot(value.OpposingRootLocalSoleRotation, value.OpposingRootLocalSoleRotation) <= 0.000001f)
                return false;
            for (int i = 0; i < value.Route.RootLocalFoot.Length; i++)
            {
                if (!IsFinite(value.Route.RootLocalFoot[i]) ||
                    !IsFinite(value.Route.RootLocalAnkle[i]) ||
                    !IsFinite(value.Route.RootLocalHip[i]) ||
                    !IsFinite(value.Route.AuthoredFootPlanar[i]) ||
                    !float.IsFinite(value.Route.AnimationClearance[i]) ||
                    value.Route.AnimationClearance[i] < 0f)
                    return false;
            }
            return true;
        }

        internal static AnimationPoseNativeInvalidReason NormalizeInvalidReason(
            AnimationPoseNativeInvalidReason reason) =>
            AnimationPoseNativeInvalidReasonContract.NormalizeFailure(reason);

        internal static bool IsAvailability(AnimationPoseAvailability availability)
        {
            int value = (int)availability;
            return value >= (int)AnimationPoseAvailability.Pose &&
                   value <= (int)AnimationPoseAvailability.Invalid;
        }

        internal static bool IsWeight(float value) =>
            float.IsFinite(value) && value >= 0f && value <= 1f;

        internal static float UnionWeight(float a, float b) =>
            Mathf.Clamp01(1f - (1f - Mathf.Clamp01(a)) * (1f - Mathf.Clamp01(b)));

        internal static ulong CombineContinuity(ulong a, ulong b, int operation)
        {
            unchecked
            {
                ulong value = 1469598103934665603UL;
                value = (value ^ RequireIdentity(a)) * 1099511628211UL;
                value = (value ^ RequireIdentity(b)) * 1099511628211UL;
                value = (value ^ (ulong)(operation + 1)) * 1099511628211UL;
                return RequireIdentity(value);
            }
        }

        internal static ulong RequireIdentity(ulong value) => value == 0 ? 1UL : value;

        internal static bool IsFinite(Vector2 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y);

        internal static bool IsFinite(Vector3 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);

        internal static bool IsFinite(Quaternion value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) &&
            float.IsFinite(value.z) && float.IsFinite(value.w);

        internal static Vector3 ClampMagnitude(Vector3 value, float maximum) =>
            maximum <= 0f ? Vector3.zero : Vector3.ClampMagnitude(value, maximum);

        internal static Vector3 RotationResidual(Quaternion previous, Quaternion current, float maximumDegrees)
        {
            Quaternion delta = previous * Quaternion.Inverse(current);
            if (delta.w < 0f)
                delta = new Quaternion(-delta.x, -delta.y, -delta.z, -delta.w);
            delta.ToAngleAxis(out float angleDegrees, out Vector3 axis);
            if (!float.IsFinite(angleDegrees) || !IsFinite(axis) || axis.sqrMagnitude <= 0.000001f)
                return Vector3.zero;
            if (angleDegrees > 180f)
                angleDegrees -= 360f;
            angleDegrees = Mathf.Clamp(angleDegrees, -maximumDegrees, maximumDegrees);
            return axis.normalized * (angleDegrees * Mathf.Deg2Rad);
        }

    }
}
