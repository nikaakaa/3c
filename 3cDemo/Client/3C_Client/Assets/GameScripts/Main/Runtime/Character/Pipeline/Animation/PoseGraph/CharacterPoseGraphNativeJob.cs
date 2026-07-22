using System;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Animations;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal struct CharacterPoseGraphNativeJob : IAnimationJob
    {
        const float ScaleEpsilon = 0.000001f;

        [ReadOnly]
        readonly NativeArray<AnimationPoseGraphNativeOperation> m_Operations;
        [ReadOnly]
        readonly NativeArray<float> m_DenseBoneMasks;
        [ReadOnly]
        readonly NativeArray<AnimationLocalBonePose> m_AdditiveReferences;
        [ReadOnly]
        readonly NativeArray<PoseParameterResolvePolicy> m_ParameterPolicies;
        [ReadOnly]
        readonly NativeArray<float> m_ParameterDefaults;
        [ReadOnly]
        readonly NativeArray<int> m_ParentIndices;

        [ReadOnly]
        readonly NativeArray<AnimationPoseSlotNativeRange> m_SlotRanges;
        [ReadOnly]
        readonly NativeArray<AnimationLocalBonePose> m_SlotDenseLocalPoses;
        [ReadOnly]
        readonly NativeArray<float> m_SlotPoseParameters;
        [ReadOnly]
        readonly NativeArray<AnimationPrimitivePoseContribution> m_SlotContributions;
        [ReadOnly]
        readonly NativeArray<float> m_SlotDenseContributionWeights;
        [ReadOnly]
        readonly NativeArray<int> m_SlotContributionCounts;
        [ReadOnly]
        readonly NativeArray<float> m_SlotOutputWeights;
        [ReadOnly]
        readonly NativeArray<AnimationFootFeatureSample> m_SlotLeftFootFeatures;
        [ReadOnly]
        readonly NativeArray<AnimationFootFeatureSample> m_SlotRightFootFeatures;
        [ReadOnly]
        readonly NativeArray<byte> m_SlotHasFootFeatures;
        [ReadOnly]
        readonly NativeArray<PoseSlotFrameAvailability> m_SlotAvailability;
        [ReadOnly]
        readonly NativeArray<ulong> m_SlotContinuityIdentities;
        [ReadOnly]
        readonly NativeArray<AnimationPoseNativeInvalidReason> m_SlotInvalidReasons;
        [ReadOnly]
        readonly NativeArray<ulong> m_SlotCompletedAt;

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

        readonly int m_SlotCount;
        readonly int m_BoneCount;
        readonly int m_ParameterCount;
        readonly int m_PoseValueCount;
        readonly int m_ContributionStride;
        readonly int m_OutputOperationIndex;
        readonly int m_OutputPoseValueIndex;
        readonly int m_LeftFootBoneIndex;
        readonly int m_RightFootBoneIndex;
        readonly ulong m_CompletionIdentity;

        internal CharacterPoseGraphNativeJob(
            CharacterPoseGraphNativeProgram program,
            CharacterPoseGraphNativeBinding binding)
        {
            RequireValidConfiguration(program, binding);

            m_Operations = program.Operations;
            m_DenseBoneMasks = program.DenseBoneMasks;
            m_AdditiveReferences = program.AdditiveReferences;
            m_ParameterPolicies = program.ParameterPolicies;
            m_ParameterDefaults = program.ParameterDefaults;
            m_ParentIndices = program.ParentIndices;

            m_SlotRanges = binding.SlotRanges;
            m_SlotDenseLocalPoses = binding.SlotDenseLocalPoses;
            m_SlotPoseParameters = binding.SlotPoseParameters;
            m_SlotContributions = binding.SlotContributions;
            m_SlotDenseContributionWeights = binding.SlotDenseContributionWeights;
            m_SlotContributionCounts = binding.SlotContributionCounts;
            m_SlotOutputWeights = binding.SlotOutputWeights;
            m_SlotLeftFootFeatures = binding.SlotLeftFootFeatures;
            m_SlotRightFootFeatures = binding.SlotRightFootFeatures;
            m_SlotHasFootFeatures = binding.SlotHasFootFeatures;
            m_SlotAvailability = binding.SlotAvailability;
            m_SlotContinuityIdentities = binding.SlotContinuityIdentities;
            m_SlotInvalidReasons = binding.SlotInvalidReasons;
            m_SlotCompletedAt = binding.SlotCompletedAt;

            m_ValueDenseLocalPoses = binding.ValueDenseLocalPoses;
            m_ValuePoseParameters = binding.ValuePoseParameters;
            m_ValueContributions = binding.ValueContributions;
            m_ValueDenseContributionWeights = binding.ValueDenseContributionWeights;
            m_ValueContributionCounts = binding.ValueContributionCounts;
            m_ValueOutputWeights = binding.ValueOutputWeights;
            m_ValueLeftFootFeatures = binding.ValueLeftFootFeatures;
            m_ValueRightFootFeatures = binding.ValueRightFootFeatures;
            m_ValueHasFootFeatures = binding.ValueHasFootFeatures;
            m_ValueAvailability = binding.ValueAvailability;
            m_ValueContinuityIdentities = binding.ValueContinuityIdentities;
            m_ValueInvalidReasons = binding.ValueInvalidReasons;
            m_FrameCacheCompletedAt = binding.FrameCacheCompletedAt;
            m_PoseGraphInvalidReason = binding.PoseGraphInvalidReason;
            m_PoseGraphInvalidOperationIndex = binding.PoseGraphInvalidOperationIndex;
            m_PoseGraphCompletedAt = binding.PoseGraphCompletedAt;

            m_SlotCount = binding.Layout.SlotCount;
            m_BoneCount = binding.Layout.BoneCount;
            m_ParameterCount = binding.Layout.ParameterCount;
            m_PoseValueCount = binding.Layout.PoseValueCount;
            m_ContributionStride = binding.Layout.PoseValueContributionStride;
            m_OutputOperationIndex = program.OutputOperationIndex;
            m_OutputPoseValueIndex = program.OutputPoseValueIndex;
            m_LeftFootBoneIndex = program.LeftFootBoneIndex;
            m_RightFootBoneIndex = program.RightFootBoneIndex;
            m_CompletionIdentity = binding.CompletionIdentity;
        }

        public void ProcessAnimation(AnimationStream stream)
        {
            BeginEvaluation();
            for (int operationIndex = 0; operationIndex < m_Operations.Length; operationIndex++)
            {
                AnimationPoseGraphNativeOperation operation = m_Operations[operationIndex];
                ResetValue(operation.OutputPoseValueIndex);
                switch (operation.Code)
                {
                    case CharacterPoseOperationCode.PoseSlotInput:
                        EvaluatePoseSlotInput(operation);
                        break;
                    case CharacterPoseOperationCode.LayeredBoneBlend:
                        EvaluateLayeredBoneBlend(operation);
                        break;
                    case CharacterPoseOperationCode.AdditivePose:
                        EvaluateAdditivePose(operation);
                        break;
                    case CharacterPoseOperationCode.PoseCurveResolve:
                        EvaluatePoseCurveResolve(operation);
                        break;
                    case CharacterPoseOperationCode.OutputPose:
                        EvaluateOutputPose(operation);
                        break;
                    default:
                        SetInvalid(
                            operation.OutputPoseValueIndex,
                            (ulong)operation.Index + 1UL,
                            AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid,
                            operation.Index);
                        break;
                }

                if (!TryValidateValue(operation.OutputPoseValueIndex, out AnimationPoseNativeInvalidReason reason))
                {
                    SetInvalid(
                        operation.OutputPoseValueIndex,
                        m_ValueContinuityIdentities[operation.OutputPoseValueIndex],
                        reason,
                        operation.Index);
                }
                m_FrameCacheCompletedAt[operation.FrameCacheIndex] = m_CompletionIdentity;
            }

            if (m_ValueAvailability[m_OutputPoseValueIndex] != PoseSlotFrameAvailability.Pose)
            {
                if (m_ValueAvailability[m_OutputPoseValueIndex] != PoseSlotFrameAvailability.Invalid)
                {
                    SetInvalid(
                        m_OutputPoseValueIndex,
                        m_ValueContinuityIdentities[m_OutputPoseValueIndex],
                        AnimationPoseNativeInvalidReason.PoseGraphOutputInvalid,
                        m_OutputOperationIndex);
                }
                else if (m_PoseGraphInvalidReason[0] == AnimationPoseNativeInvalidReason.None)
                {
                    RecordGraphInvalid(
                        NormalizeInvalidReason(m_ValueInvalidReasons[m_OutputPoseValueIndex]),
                        m_OutputOperationIndex);
                }
            }
            else if (m_ValueContributionCounts[m_OutputPoseValueIndex] <= 0 ||
                     m_ValueInvalidReasons[m_OutputPoseValueIndex] != AnimationPoseNativeInvalidReason.None)
            {
                SetInvalid(
                    m_OutputPoseValueIndex,
                    m_ValueContinuityIdentities[m_OutputPoseValueIndex],
                    AnimationPoseNativeInvalidReason.PoseGraphOutputInvalid,
                    m_OutputOperationIndex);
            }

            m_PoseGraphCompletedAt[0] = m_CompletionIdentity;
        }

        public void ProcessRootMotion(AnimationStream stream)
        {
        }

        void BeginEvaluation()
        {
            for (int i = 0; i < m_FrameCacheCompletedAt.Length; i++)
                m_FrameCacheCompletedAt[i] = 0;
            m_PoseGraphInvalidReason[0] = AnimationPoseNativeInvalidReason.None;
            m_PoseGraphInvalidOperationIndex[0] = -1;
            m_PoseGraphCompletedAt[0] = 0;
        }

        void EvaluatePoseSlotInput(AnimationPoseGraphNativeOperation operation)
        {
            int output = operation.OutputPoseValueIndex;
            int slotIndex = operation.PhysicalSlotIndex;
            ulong continuity = slotIndex >= 0 && slotIndex < m_SlotCount
                ? m_SlotContinuityIdentities[slotIndex]
                : 0UL;
            if (slotIndex < 0 || slotIndex >= m_SlotCount ||
                m_SlotCompletedAt[slotIndex] != m_CompletionIdentity)
            {
                SetInvalid(
                    output,
                    continuity,
                    AnimationPoseNativeInvalidReason.PoseGraphInputIncomplete,
                    operation.Index);
                return;
            }

            PoseSlotFrameAvailability availability = m_SlotAvailability[slotIndex];
            AnimationPoseNativeInvalidReason slotReason = m_SlotInvalidReasons[slotIndex];
            if (availability == PoseSlotFrameAvailability.Invalid)
            {
                SetInvalid(output, continuity, NormalizeInvalidReason(slotReason), operation.Index);
                return;
            }
            if (!IsAvailability(availability) || slotReason != AnimationPoseNativeInvalidReason.None || continuity == 0)
            {
                SetInvalid(
                    output,
                    continuity,
                    AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid,
                    operation.Index);
                return;
            }
            if (availability == PoseSlotFrameAvailability.NoPose &&
                operation.PoseSlotOutputPolicy == PoseSlotOutputPolicy.RequireOutput)
            {
                SetInvalid(output, continuity, AnimationPoseNativeInvalidReason.RequiredPoseMissing, operation.Index);
                return;
            }

            AnimationPoseSlotNativeRange range = m_SlotRanges[slotIndex];
            int contributionCount = m_SlotContributionCounts[slotIndex];
            float outputWeight = m_SlotOutputWeights[slotIndex];
            byte hasFootFeatures = m_SlotHasFootFeatures[slotIndex];
            if (range.PhysicalSlotIndex != slotIndex || contributionCount < 0 ||
                contributionCount > range.ContributionCapacity || contributionCount > m_ContributionStride ||
                !IsWeight(outputWeight) || hasFootFeatures > 1)
            {
                SetInvalid(output, continuity, AnimationPoseNativeInvalidReason.SlotPlanInvalid, operation.Index);
                return;
            }

            for (int parameter = 0; parameter < m_ParameterCount; parameter++)
            {
                float value = m_SlotPoseParameters[range.ParameterOffset + parameter];
                if (!float.IsFinite(value))
                {
                    SetInvalid(output, continuity, AnimationPoseNativeInvalidReason.SlotParameterInvalid, operation.Index);
                    return;
                }
                m_ValuePoseParameters[ParameterOffset(output) + parameter] = value;
            }

            if (availability == PoseSlotFrameAvailability.NoPose)
            {
                if (contributionCount != 0 || outputWeight != 0f || hasFootFeatures != 0)
                {
                    SetInvalid(output, continuity, AnimationPoseNativeInvalidReason.SlotPlanInvalid, operation.Index);
                    return;
                }
                m_ValueAvailability[output] = PoseSlotFrameAvailability.NoPose;
                m_ValueContinuityIdentities[output] = continuity;
                m_ValueInvalidReasons[output] = AnimationPoseNativeInvalidReason.None;
                return;
            }

            if (contributionCount <= 0 || outputWeight <= 0f)
            {
                SetInvalid(output, continuity, AnimationPoseNativeInvalidReason.SlotPlanInvalid, operation.Index);
                return;
            }
            for (int bone = 0; bone < m_BoneCount; bone++)
            {
                AnimationLocalBonePose pose = m_SlotDenseLocalPoses[range.PoseOffset + bone];
                if (!pose.IsValid)
                {
                    SetInvalid(output, continuity, AnimationPoseNativeInvalidReason.SlotPoseInvalid, operation.Index);
                    return;
                }
                m_ValueDenseLocalPoses[PoseOffset(output) + bone] = pose;
            }

            if (hasFootFeatures == 1 &&
                (!IsValidFootFeature(m_SlotLeftFootFeatures[slotIndex]) ||
                 !IsValidFootFeature(m_SlotRightFootFeatures[slotIndex])))
            {
                SetInvalid(output, continuity, AnimationPoseNativeInvalidReason.SlotFootFeatureInvalid, operation.Index);
                return;
            }

            int destinationContributionOffset = ContributionOffset(output);
            int destinationDenseOffset = ContributionBoneOffset(output);
            for (int contribution = 0; contribution < contributionCount; contribution++)
            {
                AnimationPrimitivePoseContribution primitive =
                    m_SlotContributions[range.ContributionOffset + contribution];
                if (!IsValidPrimitiveContribution(primitive) || primitive.PhysicalSlotIndex != slotIndex)
                {
                    SetInvalid(output, continuity, AnimationPoseNativeInvalidReason.SlotContributionInvalid, operation.Index);
                    return;
                }
                m_ValueContributions[destinationContributionOffset + contribution] = primitive;
                for (int bone = 0; bone < m_BoneCount; bone++)
                {
                    float weight = m_SlotDenseContributionWeights[
                        range.DenseContributionWeightOffset + contribution * m_BoneCount + bone];
                    if (!IsWeight(weight))
                    {
                        SetInvalid(output, continuity, AnimationPoseNativeInvalidReason.SlotContributionInvalid, operation.Index);
                        return;
                    }
                    m_ValueDenseContributionWeights[
                        destinationDenseOffset + contribution * m_BoneCount + bone] = weight;
                }
            }

            m_ValueContributionCounts[output] = contributionCount;
            m_ValueOutputWeights[output] = outputWeight;
            m_ValueLeftFootFeatures[output] = hasFootFeatures == 1
                ? m_SlotLeftFootFeatures[slotIndex]
                : default;
            m_ValueRightFootFeatures[output] = hasFootFeatures == 1
                ? m_SlotRightFootFeatures[slotIndex]
                : default;
            m_ValueHasFootFeatures[output] = hasFootFeatures;
            m_ValueAvailability[output] = PoseSlotFrameAvailability.Pose;
            m_ValueContinuityIdentities[output] = continuity;
            m_ValueInvalidReasons[output] = AnimationPoseNativeInvalidReason.None;
        }

        void EvaluateLayeredBoneBlend(AnimationPoseGraphNativeOperation operation)
        {
            int output = operation.OutputPoseValueIndex;
            int baseValue = operation.InputPoseValueIndexA;
            int overlayValue = operation.InputPoseValueIndexB;
            if (!TryRequireInputs(operation, baseValue, overlayValue))
                return;
            if (m_ValueAvailability[overlayValue] == PoseSlotFrameAvailability.NoPose)
            {
                if (!TryCopyValue(baseValue, output, operation.Index))
                    SetInvalid(output, m_ValueContinuityIdentities[baseValue], AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid, operation.Index);
                return;
            }
            if (m_ValueAvailability[baseValue] == PoseSlotFrameAvailability.NoPose)
            {
                if (!TryCopyValue(overlayValue, output, operation.Index) ||
                    !TryScaleValue(output, operation))
                {
                    SetInvalid(output, m_ValueContinuityIdentities[overlayValue], AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid, operation.Index);
                }
                return;
            }

            m_ValueAvailability[output] = PoseSlotFrameAvailability.Pose;
            m_ValueOutputWeights[output] = UnionWeight(
                m_ValueOutputWeights[baseValue],
                m_ValueOutputWeights[overlayValue] * operation.Weight);
            m_ValueContinuityIdentities[output] = CombineContinuity(
                m_ValueContinuityIdentities[baseValue],
                m_ValueContinuityIdentities[overlayValue],
                operation.Index);
            m_ValueInvalidReasons[output] = AnimationPoseNativeInvalidReason.None;
            for (int bone = 0; bone < m_BoneCount; bone++)
            {
                if (!TryGetBoneOutputWeight(overlayValue, bone, out float overlayOutputWeight))
                {
                    SetInvalid(output, m_ValueContinuityIdentities[output], AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid, operation.Index);
                    return;
                }
                float overlay = Mathf.Clamp01(
                    overlayOutputWeight * GetMaskWeight(operation, bone) * operation.Weight);
                if (!TryBlendPose(
                        m_ValueDenseLocalPoses[PoseOffset(baseValue) + bone],
                        m_ValueDenseLocalPoses[PoseOffset(overlayValue) + bone],
                        overlay,
                        out AnimationLocalBonePose pose))
                {
                    SetInvalid(output, m_ValueContinuityIdentities[output], AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid, operation.Index);
                    return;
                }
                m_ValueDenseLocalPoses[PoseOffset(output) + bone] = pose;
            }
            if (!TryResolveParameters(operation, baseValue, overlayValue, output) ||
                !TryMergeContributions(operation, baseValue, overlayValue, output, false) ||
                !TryResolveFootFeatures(operation, baseValue, overlayValue, output, false))
            {
                SetInvalid(output, m_ValueContinuityIdentities[output], AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid, operation.Index);
            }
        }

        void EvaluateAdditivePose(AnimationPoseGraphNativeOperation operation)
        {
            int output = operation.OutputPoseValueIndex;
            int baseValue = operation.InputPoseValueIndexA;
            int additiveValue = operation.InputPoseValueIndexB;
            if (!TryRequireInputs(operation, baseValue, additiveValue))
                return;
            if (m_ValueAvailability[additiveValue] == PoseSlotFrameAvailability.NoPose)
            {
                if (!TryCopyValue(baseValue, output, operation.Index))
                    SetInvalid(output, m_ValueContinuityIdentities[baseValue], AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid, operation.Index);
                return;
            }
            if (m_ValueAvailability[baseValue] != PoseSlotFrameAvailability.Pose)
            {
                SetInvalid(
                    output,
                    CombineContinuity(
                        m_ValueContinuityIdentities[baseValue],
                        m_ValueContinuityIdentities[additiveValue],
                        operation.Index),
                    AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid,
                    operation.Index);
                return;
            }

            m_ValueAvailability[output] = PoseSlotFrameAvailability.Pose;
            m_ValueOutputWeights[output] = UnionWeight(
                m_ValueOutputWeights[baseValue],
                m_ValueOutputWeights[additiveValue] * operation.Weight);
            m_ValueContinuityIdentities[output] = CombineContinuity(
                m_ValueContinuityIdentities[baseValue],
                m_ValueContinuityIdentities[additiveValue],
                operation.Index);
            m_ValueInvalidReasons[output] = AnimationPoseNativeInvalidReason.None;
            for (int bone = 0; bone < m_BoneCount; bone++)
            {
                if (!TryGetBoneOutputWeight(additiveValue, bone, out float additiveOutputWeight))
                {
                    SetInvalid(output, m_ValueContinuityIdentities[output], AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid, operation.Index);
                    return;
                }
                float weight = Mathf.Clamp01(
                    additiveOutputWeight * GetMaskWeight(operation, bone) * operation.Weight);
                bool valid = operation.AdditiveReferenceSpace switch
                {
                    AdditiveReferenceSpace.Local => TryAddPose(
                        m_ValueDenseLocalPoses[PoseOffset(baseValue) + bone],
                        m_ValueDenseLocalPoses[PoseOffset(additiveValue) + bone],
                        m_AdditiveReferences[operation.AdditiveReferenceOffset + bone],
                        operation.AdditiveScalePolicy,
                        weight,
                        out AnimationLocalBonePose localPose) &&
                        AssignPose(output, bone, localPose),
                    AdditiveReferenceSpace.Mesh => TryAddMeshPose(
                        baseValue,
                        additiveValue,
                        output,
                        operation,
                        bone,
                        weight,
                        out AnimationLocalBonePose meshPose) &&
                        AssignPose(output, bone, meshPose),
                    _ => false
                };
                if (!valid)
                {
                    SetInvalid(output, m_ValueContinuityIdentities[output], AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid, operation.Index);
                    return;
                }
            }
            if (!TryResolveParameters(operation, baseValue, additiveValue, output) ||
                !TryMergeContributions(operation, baseValue, additiveValue, output, true) ||
                !TryResolveFootFeatures(operation, baseValue, additiveValue, output, true))
            {
                SetInvalid(output, m_ValueContinuityIdentities[output], AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid, operation.Index);
            }
        }

        void EvaluatePoseCurveResolve(AnimationPoseGraphNativeOperation operation)
        {
            int output = operation.OutputPoseValueIndex;
            int baseValue = operation.InputPoseValueIndexA;
            int parameterSourceValue = operation.InputPoseValueIndexB;
            if (!TryRequireInputs(operation, baseValue, parameterSourceValue))
                return;
            if (!TryCopyValue(baseValue, output, operation.Index))
            {
                SetInvalid(output, m_ValueContinuityIdentities[baseValue], AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid, operation.Index);
                return;
            }
            if (m_ValueAvailability[parameterSourceValue] == PoseSlotFrameAvailability.NoPose)
                return;
            if (!TryResolveParameters(operation, baseValue, parameterSourceValue, output))
            {
                SetInvalid(output, m_ValueContinuityIdentities[output], AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid, operation.Index);
                return;
            }
            m_ValueContinuityIdentities[output] = CombineContinuity(
                m_ValueContinuityIdentities[baseValue],
                m_ValueContinuityIdentities[parameterSourceValue],
                operation.Index);
        }

        void EvaluateOutputPose(AnimationPoseGraphNativeOperation operation)
        {
            int output = operation.OutputPoseValueIndex;
            int input = operation.InputPoseValueIndexA;
            if (!IsInputReady(input, operation.Index))
            {
                SetInvalid(output, (ulong)operation.Index + 1UL, AnimationPoseNativeInvalidReason.PoseGraphInputIncomplete, operation.Index);
                return;
            }
            if (m_ValueAvailability[input] == PoseSlotFrameAvailability.NoPose)
            {
                SetInvalid(output, m_ValueContinuityIdentities[input], AnimationPoseNativeInvalidReason.PoseGraphOutputInvalid, operation.Index);
                return;
            }
            if (!TryCopyValue(input, output, operation.Index))
            {
                SetInvalid(output, m_ValueContinuityIdentities[input], AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid, operation.Index);
                return;
            }
            if (m_ValueAvailability[output] == PoseSlotFrameAvailability.Invalid &&
                m_PoseGraphInvalidReason[0] == AnimationPoseNativeInvalidReason.None)
            {
                RecordGraphInvalid(NormalizeInvalidReason(m_ValueInvalidReasons[output]), operation.Index);
            }
        }

        bool TryRequireInputs(AnimationPoseGraphNativeOperation operation, int inputA, int inputB)
        {
            int output = operation.OutputPoseValueIndex;
            if (!IsInputReady(inputA, operation.Index) || !IsInputReady(inputB, operation.Index))
            {
                SetInvalid(output, (ulong)operation.Index + 1UL, AnimationPoseNativeInvalidReason.PoseGraphInputIncomplete, operation.Index);
                return false;
            }
            PoseSlotFrameAvailability availabilityA = m_ValueAvailability[inputA];
            PoseSlotFrameAvailability availabilityB = m_ValueAvailability[inputB];
            if (!IsAvailability(availabilityA) || !IsAvailability(availabilityB))
            {
                SetInvalid(output, (ulong)operation.Index + 1UL, AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid, operation.Index);
                return false;
            }
            if (availabilityA == PoseSlotFrameAvailability.Invalid ||
                availabilityB == PoseSlotFrameAvailability.Invalid)
            {
                AnimationPoseNativeInvalidReason reason = availabilityA == PoseSlotFrameAvailability.Invalid
                    ? NormalizeInvalidReason(m_ValueInvalidReasons[inputA])
                    : NormalizeInvalidReason(m_ValueInvalidReasons[inputB]);
                SetInvalid(
                    output,
                    CombineContinuity(
                        m_ValueContinuityIdentities[inputA],
                        m_ValueContinuityIdentities[inputB],
                        output),
                    reason,
                    operation.Index);
                return false;
            }
            return true;
        }

        bool TryCopyValue(int source, int destination, int operationIndex)
        {
            if (source < 0 || source >= m_PoseValueCount || destination < 0 || destination >= m_PoseValueCount)
                return false;
            int contributionCount = m_ValueContributionCounts[source];
            if (contributionCount < 0 || contributionCount > m_ContributionStride)
                return false;

            PoseSlotFrameAvailability availability = m_ValueAvailability[source];
            m_ValueAvailability[destination] = availability;
            m_ValueOutputWeights[destination] = m_ValueOutputWeights[source];
            m_ValueContinuityIdentities[destination] = CombineContinuity(
                m_ValueContinuityIdentities[source],
                (ulong)operationIndex + 1UL,
                operationIndex);
            m_ValueInvalidReasons[destination] = m_ValueInvalidReasons[source];
            if (availability == PoseSlotFrameAvailability.Pose)
            {
                for (int bone = 0; bone < m_BoneCount; bone++)
                {
                    m_ValueDenseLocalPoses[PoseOffset(destination) + bone] =
                        m_ValueDenseLocalPoses[PoseOffset(source) + bone];
                }
            }
            for (int parameter = 0; parameter < m_ParameterCount; parameter++)
            {
                m_ValuePoseParameters[ParameterOffset(destination) + parameter] =
                    m_ValuePoseParameters[ParameterOffset(source) + parameter];
            }
            m_ValueContributionCounts[destination] = contributionCount;
            for (int contribution = 0; contribution < contributionCount; contribution++)
            {
                m_ValueContributions[ContributionOffset(destination) + contribution] =
                    m_ValueContributions[ContributionOffset(source) + contribution];
                for (int bone = 0; bone < m_BoneCount; bone++)
                {
                    m_ValueDenseContributionWeights[
                        ContributionBoneOffset(destination) + contribution * m_BoneCount + bone] =
                        m_ValueDenseContributionWeights[
                            ContributionBoneOffset(source) + contribution * m_BoneCount + bone];
                }
            }
            m_ValueLeftFootFeatures[destination] = m_ValueLeftFootFeatures[source];
            m_ValueRightFootFeatures[destination] = m_ValueRightFootFeatures[source];
            m_ValueHasFootFeatures[destination] = m_ValueHasFootFeatures[source];
            return true;
        }

        bool TryScaleValue(int value, AnimationPoseGraphNativeOperation operation)
        {
            if (m_ValueAvailability[value] != PoseSlotFrameAvailability.Pose)
                return true;
            float outputWeight = m_ValueOutputWeights[value] * operation.Weight;
            if (!IsWeight(outputWeight))
                return false;
            m_ValueOutputWeights[value] = outputWeight;
            int count = m_ValueContributionCounts[value];
            for (int contribution = 0; contribution < count; contribution++)
            {
                AnimationPrimitivePoseContribution source =
                    m_ValueContributions[ContributionOffset(value) + contribution];
                float scalarWeight = source.Weight * operation.Weight;
                float leftWeight = source.LeftFootWeight * GetMaskWeight(operation, m_LeftFootBoneIndex) * operation.Weight;
                float rightWeight = source.RightFootWeight * GetMaskWeight(operation, m_RightFootBoneIndex) * operation.Weight;
                if (!IsWeight(scalarWeight) || !IsWeight(leftWeight) || !IsWeight(rightWeight))
                    return false;
                m_ValueContributions[ContributionOffset(value) + contribution] =
                    new AnimationPrimitivePoseContribution(
                        source.PhysicalSlotIndex,
                        source.PhysicalSourceIndex,
                        source.PhysicalSourceGeneration,
                        source.Kind,
                        source.ProgramProducerIndex,
                        source.ContributionContinuityIdentity,
                        scalarWeight,
                        leftWeight,
                        rightWeight);
                for (int bone = 0; bone < m_BoneCount; bone++)
                {
                    float weight = GetContributionBoneWeight(value, contribution, bone) *
                                   GetMaskWeight(operation, bone) * operation.Weight;
                    if (!IsWeight(weight))
                        return false;
                    SetContributionBoneWeight(value, contribution, bone, weight);
                }
            }
            return true;
        }

        bool TryResolveParameters(
            AnimationPoseGraphNativeOperation operation,
            int baseValue,
            int overlayValue,
            int output)
        {
            float baseWeight = m_ValueOutputWeights[baseValue];
            float overlayWeight = m_ValueOutputWeights[overlayValue] * operation.Weight;
            if (!IsWeight(baseWeight) || !IsWeight(overlayWeight))
                return false;
            for (int parameter = 0; parameter < m_ParameterCount; parameter++)
            {
                float baseParameter = m_ValuePoseParameters[ParameterOffset(baseValue) + parameter];
                float overlayParameter = m_ValuePoseParameters[ParameterOffset(overlayValue) + parameter];
                if (!float.IsFinite(baseParameter) || !float.IsFinite(overlayParameter))
                    return false;
                PoseParameterResolvePolicy policy =
                    m_ParameterPolicies[operation.ParameterPolicyOffset + parameter];
                float value;
                switch (policy)
                {
                    case PoseParameterResolvePolicy.Base:
                        value = baseParameter;
                        break;
                    case PoseParameterResolvePolicy.Overlay:
                        value = overlayWeight > 0f ? overlayParameter : baseParameter;
                        break;
                    case PoseParameterResolvePolicy.Weighted:
                        float total = baseWeight + overlayWeight;
                        value = total > 0f
                            ? (baseParameter * baseWeight + overlayParameter * overlayWeight) / total
                            : 0f;
                        break;
                    case PoseParameterResolvePolicy.Max:
                        value = Mathf.Max(baseParameter, overlayParameter);
                        break;
                    case PoseParameterResolvePolicy.Min:
                        value = Mathf.Min(baseParameter, overlayParameter);
                        break;
                    default:
                        return false;
                }
                if (!float.IsFinite(value))
                    return false;
                m_ValuePoseParameters[ParameterOffset(output) + parameter] = value;
            }
            return true;
        }

        bool TryMergeContributions(
            AnimationPoseGraphNativeOperation operation,
            int baseValue,
            int overlayValue,
            int output,
            bool additive)
        {
            for (int contribution = 0; contribution < m_ValueContributionCounts[baseValue]; contribution++)
            {
                if (!TryAddContribution(
                        operation,
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
                        operation,
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

        bool TryAddContribution(
            AnimationPoseGraphNativeOperation operation,
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
                scalarFactor = operation.Weight;
                leftFactor = GetMaskWeight(operation, m_LeftFootBoneIndex) * operation.Weight;
                rightFactor = GetMaskWeight(operation, m_RightFootBoneIndex) * operation.Weight;
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
                scalarFactor = 1f - m_ValueOutputWeights[overlayValue] * operation.Weight;
                leftFactor = 1f - leftOverlay * GetMaskWeight(operation, m_LeftFootBoneIndex) * operation.Weight;
                rightFactor = 1f - rightOverlay * GetMaskWeight(operation, m_RightFootBoneIndex) * operation.Weight;
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
                m_ValueContributions[ContributionOffset(output) + targetIndex] =
                    new AnimationPrimitivePoseContribution(
                        source.PhysicalSlotIndex,
                        source.PhysicalSourceIndex,
                        source.PhysicalSourceGeneration,
                        source.Kind,
                        source.ProgramProducerIndex,
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
                        current.PhysicalSlotIndex,
                        current.PhysicalSourceIndex,
                        current.PhysicalSourceGeneration,
                        current.Kind,
                        current.ProgramProducerIndex,
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
                    factor = GetMaskWeight(operation, bone) * operation.Weight;
                }
                else if (additive)
                {
                    factor = 1f;
                }
                else
                {
                    if (!TryGetBoneOutputWeight(overlayValue, bone, out float overlayOutput))
                        return false;
                    factor = 1f - overlayOutput * GetMaskWeight(operation, bone) * operation.Weight;
                }
                float weight = GetContributionBoneWeight(sourceValue, sourceIndex, bone) * Mathf.Clamp01(factor);
                float combined = Mathf.Clamp01(GetContributionBoneWeight(output, targetIndex, bone) + weight);
                if (!IsWeight(combined))
                    return false;
                SetContributionBoneWeight(output, targetIndex, bone, combined);
            }
            return true;
        }

        bool TryResolveFootFeatures(
            AnimationPoseGraphNativeOperation operation,
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
            float left = leftOutput * GetMaskWeight(operation, m_LeftFootBoneIndex) * operation.Weight;
            float right = rightOutput * GetMaskWeight(operation, m_RightFootBoneIndex) * operation.Weight;
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
                    out AnimationFootFeatureSample leftFeature) ||
                !TryResolveFeature(
                    hasBase,
                    m_ValueRightFootFeatures[baseValue],
                    hasOverlay,
                    m_ValueRightFootFeatures[overlayValue],
                    right,
                    out AnimationFootFeatureSample rightFeature))
            {
                return false;
            }
            m_ValueLeftFootFeatures[output] = leftFeature;
            m_ValueRightFootFeatures[output] = rightFeature;
            m_ValueHasFootFeatures[output] = leftFeature.IsValid && rightFeature.IsValid ? (byte)1 : (byte)0;
            return true;
        }

        bool TryValidateValue(int value, out AnimationPoseNativeInvalidReason reason)
        {
            reason = AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid;
            PoseSlotFrameAvailability availability = m_ValueAvailability[value];
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
                if (!float.IsFinite(m_ValuePoseParameters[ParameterOffset(value) + parameter]))
                    return false;
            }

            if (availability == PoseSlotFrameAvailability.Invalid)
            {
                reason = NormalizeInvalidReason(invalidReason);
                return invalidReason != AnimationPoseNativeInvalidReason.None &&
                       contributionCount == 0 && m_ValueOutputWeights[value] == 0f && hasFootFeatures == 0;
            }
            if (invalidReason != AnimationPoseNativeInvalidReason.None)
                return false;
            if (availability == PoseSlotFrameAvailability.NoPose)
            {
                return contributionCount == 0 && m_ValueOutputWeights[value] == 0f && hasFootFeatures == 0;
            }
            if (contributionCount <= 0)
                return false;
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
            if (hasFootFeatures == 1 &&
                (!IsValidFootFeature(m_ValueLeftFootFeatures[value]) ||
                 !IsValidFootFeature(m_ValueRightFootFeatures[value])))
            {
                return false;
            }
            reason = AnimationPoseNativeInvalidReason.None;
            return true;
        }

        bool IsInputReady(int value, int operationIndex)
        {
            if (value < 0 || value >= m_PoseValueCount)
                return false;
            for (int i = 0; i < operationIndex; i++)
            {
                AnimationPoseGraphNativeOperation candidate = m_Operations[i];
                if (candidate.OutputPoseValueIndex == value)
                    return m_FrameCacheCompletedAt[candidate.FrameCacheIndex] == m_CompletionIdentity;
            }
            return false;
        }

        bool TryGetBoneOutputWeight(int value, int bone, out float result)
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

        int FindContribution(int value, AnimationPrimitivePoseContribution source)
        {
            int count = m_ValueContributionCounts[value];
            for (int contribution = 0; contribution < count; contribution++)
            {
                AnimationPrimitivePoseContribution candidate =
                    m_ValueContributions[ContributionOffset(value) + contribution];
                if (candidate.PhysicalSlotIndex == source.PhysicalSlotIndex &&
                    candidate.PhysicalSourceIndex == source.PhysicalSourceIndex &&
                    candidate.PhysicalSourceGeneration == source.PhysicalSourceGeneration &&
                    candidate.Kind == source.Kind &&
                    candidate.ProgramProducerIndex == source.ProgramProducerIndex &&
                    candidate.ContributionContinuityIdentity == source.ContributionContinuityIdentity)
                {
                    return contribution;
                }
            }
            return -1;
        }

        void ResetValue(int value)
        {
            m_ValueContributionCounts[value] = 0;
            m_ValueOutputWeights[value] = 0f;
            m_ValueLeftFootFeatures[value] = default;
            m_ValueRightFootFeatures[value] = default;
            m_ValueHasFootFeatures[value] = 0;
            m_ValueAvailability[value] = PoseSlotFrameAvailability.Invalid;
            m_ValueContinuityIdentities[value] = 1;
            m_ValueInvalidReasons[value] = AnimationPoseNativeInvalidReason.None;
            for (int parameter = 0; parameter < m_ParameterCount; parameter++)
            {
                m_ValuePoseParameters[ParameterOffset(value) + parameter] = m_ParameterDefaults[parameter];
            }
            int denseOffset = ContributionBoneOffset(value);
            int denseCount = m_ContributionStride * m_BoneCount;
            for (int i = 0; i < denseCount; i++)
                m_ValueDenseContributionWeights[denseOffset + i] = 0f;
        }

        void SetInvalid(
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
            m_ValueAvailability[value] = PoseSlotFrameAvailability.Invalid;
            m_ValueContinuityIdentities[value] = RequireIdentity(continuity);
            m_ValueInvalidReasons[value] = reason;
            RecordGraphInvalid(reason, operationIndex);
        }

        void RecordGraphInvalid(AnimationPoseNativeInvalidReason reason, int operationIndex)
        {
            if (m_PoseGraphInvalidReason[0] != AnimationPoseNativeInvalidReason.None)
                return;
            m_PoseGraphInvalidReason[0] = NormalizeInvalidReason(reason);
            m_PoseGraphInvalidOperationIndex[0] = operationIndex;
        }

        bool TryAddMeshPose(
            int baseValue,
            int additiveValue,
            int outputValue,
            AnimationPoseGraphNativeOperation operation,
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

        bool TryResolveModelPose(int value, int bone, out AnimationLocalBonePose result)
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

        bool AssignPose(int value, int bone, AnimationLocalBonePose pose)
        {
            if (!pose.IsValid)
                return false;
            m_ValueDenseLocalPoses[PoseOffset(value) + bone] = pose;
            return true;
        }

        float GetMaskWeight(AnimationPoseGraphNativeOperation operation, int bone) =>
            m_DenseBoneMasks[operation.BoneMaskOffset + bone];

        float GetContributionBoneWeight(int value, int contribution, int bone) =>
            m_ValueDenseContributionWeights[
                ContributionBoneOffset(value) + contribution * m_BoneCount + bone];

        void SetContributionBoneWeight(int value, int contribution, int bone, float weight)
        {
            m_ValueDenseContributionWeights[
                ContributionBoneOffset(value) + contribution * m_BoneCount + bone] = weight;
        }

        int PoseOffset(int value) => value * m_BoneCount;
        int ParameterOffset(int value) => value * m_ParameterCount;
        int ContributionOffset(int value) => value * m_ContributionStride;
        int ContributionBoneOffset(int value) => value * m_ContributionStride * m_BoneCount;

        static bool TryBlendPose(
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

        static bool TryAddPose(
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

        static bool TryToModel(
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

        static bool TryToLocal(
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

        static bool TryDivide(Vector3 value, Vector3 divisor, out Vector3 result)
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

        static bool TryCreatePose(
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

        static bool TryResolveFeature(
            bool hasBase,
            AnimationFootFeatureSample baseValue,
            bool hasOverlay,
            AnimationFootFeatureSample overlayValue,
            float weight,
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
            float landing = Mathf.LerpUnclamped(
                baseValue.NextLandingConfidence,
                overlayValue.NextLandingConfidence,
                t);
            float delay = Mathf.LerpUnclamped(
                baseValue.NextLandingDelaySeconds,
                overlayValue.NextLandingDelaySeconds,
                t);
            Vector2 offset = Vector2.LerpUnclamped(
                baseValue.NextLandingLocalOffset,
                overlayValue.NextLandingLocalOffset,
                t);
            if (!IsFinite(velocity) || !float.IsFinite(height) || !IsWeight(plant) ||
                !IsWeight(landing) || !float.IsFinite(delay) || delay < 0f || !IsFinite(offset))
            {
                return false;
            }
            result = new AnimationFootFeatureSample(velocity, height, plant, landing, delay, offset);
            return result.IsValid;
        }

        static bool IsValidPrimitiveContribution(AnimationPrimitivePoseContribution contribution)
        {
            int kind = (int)contribution.Kind;
            bool live = contribution.Kind == AnimationPoseContributionKind.Live;
            return contribution.PhysicalSlotIndex >= 0 &&
                   kind >= (int)AnimationPoseContributionKind.Live &&
                   kind <= (int)AnimationPoseContributionKind.Inertial &&
                   (live
                       ? contribution.PhysicalSourceIndex >= 0 &&
                         contribution.PhysicalSourceGeneration != 0 &&
                         contribution.ProgramProducerIndex >= 0
                       : contribution.PhysicalSourceIndex == -1 &&
                         contribution.PhysicalSourceGeneration == 0 &&
                         contribution.ProgramProducerIndex == -1) &&
                   contribution.ContributionContinuityIdentity != 0 &&
                   IsWeight(contribution.Weight) &&
                   IsWeight(contribution.LeftFootWeight) &&
                   IsWeight(contribution.RightFootWeight);
        }

        static bool IsValidFootFeature(AnimationFootFeatureSample sample) =>
            sample.IsValid &&
            IsFinite(sample.SoleLocalVelocity) &&
            float.IsFinite(sample.SoleHeight) &&
            IsWeight(sample.PlantConfidence) &&
            IsWeight(sample.NextLandingConfidence) &&
            float.IsFinite(sample.NextLandingDelaySeconds) &&
            sample.NextLandingDelaySeconds >= 0f &&
            IsFinite(sample.NextLandingLocalOffset);

        static AnimationPoseNativeInvalidReason NormalizeInvalidReason(
            AnimationPoseNativeInvalidReason reason)
        {
            int value = (int)reason;
            return value > (int)AnimationPoseNativeInvalidReason.None &&
                   value <= (int)AnimationPoseNativeInvalidReason.FinalStreamWriteInvalid
                ? reason
                : AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid;
        }

        static bool IsAvailability(PoseSlotFrameAvailability availability)
        {
            int value = (int)availability;
            return value >= (int)PoseSlotFrameAvailability.Pose &&
                   value <= (int)PoseSlotFrameAvailability.Invalid;
        }

        static bool IsWeight(float value) =>
            float.IsFinite(value) && value >= 0f && value <= 1f;

        static float UnionWeight(float a, float b) =>
            Mathf.Clamp01(1f - (1f - Mathf.Clamp01(a)) * (1f - Mathf.Clamp01(b)));

        static ulong CombineContinuity(ulong a, ulong b, int operation)
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

        static ulong RequireIdentity(ulong value) => value == 0 ? 1UL : value;

        static bool IsFinite(Vector2 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y);

        static bool IsFinite(Vector3 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);

        static bool IsFinite(Quaternion value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) &&
            float.IsFinite(value.z) && float.IsFinite(value.w);

        static void RequireValidConfiguration(
            CharacterPoseGraphNativeProgram program,
            CharacterPoseGraphNativeBinding binding)
        {
            if (program == null)
                throw new ArgumentNullException(nameof(program));
            program.RequireValid();
            binding.RequireValid();
            AnimationPoseNativeAggregateLayout layout = binding.Layout;
            if (layout.BoneCount != program.BoneCount ||
                layout.ParameterCount != program.ParameterCount ||
                layout.PoseValueCount != program.PoseValueCount ||
                layout.PoseValueContributionStride != program.ContributionStride ||
                layout.OperationCount != program.Operations.Length ||
                layout.FrameCacheCount != program.FrameCacheCount ||
                layout.OutputPoseValueIndex != program.OutputPoseValueIndex ||
                program.OutputOperationIndex < 0 ||
                program.OutputOperationIndex >= program.Operations.Length ||
                program.LeftFootBoneIndex < 0 || program.LeftFootBoneIndex >= program.BoneCount ||
                program.RightFootBoneIndex < 0 || program.RightFootBoneIndex >= program.BoneCount)
            {
                throw new ArgumentException("Animation Pose Graph Native Job layout is invalid.", nameof(binding));
            }

            for (int bone = 0; bone < program.BoneCount; bone++)
            {
                int parentIndex = program.ParentIndices[bone];
                if (parentIndex < -1 || parentIndex >= bone)
                    throw new ArgumentException($"Animation Pose Graph Native Job parent #{bone} is invalid.", nameof(program));
            }
            for (int parameter = 0; parameter < program.ParameterCount; parameter++)
            {
                if (!float.IsFinite(program.ParameterDefaults[parameter]))
                    throw new ArgumentException($"Animation Pose Graph Native Job parameter #{parameter} is invalid.", nameof(program));
            }

            int outputCount = 0;
            for (int i = 0; i < program.Operations.Length; i++)
            {
                AnimationPoseGraphNativeOperation operation = program.Operations[i];
                if (operation.Index != i || operation.FrameCacheIndex != i ||
                    operation.OutputPoseValueIndex < 0 ||
                    operation.OutputPoseValueIndex >= program.PoseValueCount ||
                    !float.IsFinite(operation.Weight) || operation.Weight < 0f || operation.Weight > 1f)
                {
                    throw new ArgumentException($"Animation Pose Graph Native Job operation #{i} is invalid.", nameof(program));
                }
                bool inputA = operation.InputPoseValueIndexA >= 0 &&
                              operation.InputPoseValueIndexA < operation.OutputPoseValueIndex;
                bool inputB = operation.InputPoseValueIndexB >= 0 &&
                              operation.InputPoseValueIndexB < operation.OutputPoseValueIndex;
                bool valid = operation.Code switch
                {
                    CharacterPoseOperationCode.PoseSlotInput =>
                        operation.InputPoseValueIndexA == -1 && operation.InputPoseValueIndexB == -1 &&
                        operation.PhysicalSlotIndex >= 0 && operation.PhysicalSlotIndex < layout.SlotCount &&
                        Enum.IsDefined(typeof(PoseSlotOutputPolicy), operation.PoseSlotOutputPolicy) &&
                        operation.BoneMaskOffset == -1 && operation.AdditiveReferenceOffset == -1 &&
                        operation.ParameterPolicyOffset == -1,
                    CharacterPoseOperationCode.LayeredBoneBlend =>
                        inputA && inputB && HasSpan(program.DenseBoneMasks, operation.BoneMaskOffset, program.BoneCount) &&
                        operation.AdditiveReferenceOffset == -1 &&
                        HasSpan(program.ParameterPolicies, operation.ParameterPolicyOffset, program.ParameterCount),
                    CharacterPoseOperationCode.AdditivePose =>
                        inputA && inputB && HasSpan(program.DenseBoneMasks, operation.BoneMaskOffset, program.BoneCount) &&
                        HasSpan(program.AdditiveReferences, operation.AdditiveReferenceOffset, program.BoneCount) &&
                        Enum.IsDefined(typeof(AdditiveReferenceSpace), operation.AdditiveReferenceSpace) &&
                        Enum.IsDefined(typeof(AdditiveScalePolicy), operation.AdditiveScalePolicy) &&
                        HasSpan(program.ParameterPolicies, operation.ParameterPolicyOffset, program.ParameterCount),
                    CharacterPoseOperationCode.PoseCurveResolve =>
                        inputA && inputB && operation.BoneMaskOffset == -1 && operation.AdditiveReferenceOffset == -1 &&
                        HasSpan(program.ParameterPolicies, operation.ParameterPolicyOffset, program.ParameterCount),
                    CharacterPoseOperationCode.OutputPose =>
                        inputA && operation.InputPoseValueIndexB == -1 && operation.BoneMaskOffset == -1 &&
                        operation.AdditiveReferenceOffset == -1 && operation.ParameterPolicyOffset == -1,
                    _ => false
                };
                if (!valid)
                    throw new ArgumentException($"Animation Pose Graph Native Job operation #{i} layout is invalid.", nameof(program));
                if (operation.Code == CharacterPoseOperationCode.OutputPose)
                {
                    outputCount++;
                    if (i != program.OutputOperationIndex ||
                        operation.OutputPoseValueIndex != program.OutputPoseValueIndex)
                    {
                        throw new ArgumentException("Animation Pose Graph Native Job output identity is invalid.", nameof(program));
                    }
                }
            }
            if (outputCount != 1)
                throw new ArgumentException("Animation Pose Graph Native Job requires one output operation.", nameof(program));
        }

        static bool HasSpan<T>(NativeArray<T> values, int offset, int count) where T : struct =>
            offset >= 0 && count > 0 && offset <= values.Length - count;
    }
}
