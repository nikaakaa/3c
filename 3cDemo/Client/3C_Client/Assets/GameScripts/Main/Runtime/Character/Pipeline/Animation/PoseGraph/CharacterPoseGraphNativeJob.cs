using System;
using ThirdPersonCharacter.Pipeline.Animation.BlendStack;
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
        readonly NativeArray<PoseInertializationNativeNode> m_Inertializations;
        [ReadOnly]
        readonly NativeArray<AnimationPoseGraphNativeModifyBone> m_ModifyBones;
        [ReadOnly]
        readonly NativeArray<PoseInertializationNativeRule> m_InertialRules;
        [ReadOnly]
        readonly NativeArray<AnimationBlendCurveSegment> m_InertialCurveSegments;
        [ReadOnly]
        readonly NativeArray<float> m_InertialDenseProfiles;
        [ReadOnly]
        readonly NativeArray<PoseParameterInertializationMode> m_InertialParameterModes;
        NativeArray<PoseInertializationNativeState> m_InertialStates;
        NativeArray<AnimationLocalBonePose> m_InertialHistory;
        NativeArray<AnimationBlendBoneVelocity> m_InertialHistoryVelocities;
        NativeArray<float> m_InertialHistoryParameters;
        NativeArray<byte> m_InertialHistoryParameterAvailability;
        NativeArray<AnimationFootFeatureSample> m_InertialHistoryLeftFeet;
        NativeArray<AnimationFootFeatureSample> m_InertialHistoryRightFeet;
        NativeArray<byte> m_InertialHistoryHasFeet;
        NativeArray<AnimationFootFeatureSample> m_InertialAccumulatorLeftFeet;
        NativeArray<AnimationFootFeatureSample> m_InertialAccumulatorRightFeet;
        NativeArray<byte> m_InertialAccumulatorHasFeet;
        NativeArray<Vector3> m_InertialPositionResiduals;
        NativeArray<Vector3> m_InertialRotationResiduals;
        NativeArray<Vector3> m_InertialScaleResiduals;
        NativeArray<Vector3> m_InertialLinearVelocityResiduals;
        NativeArray<Vector3> m_InertialAngularVelocityResiduals;
        NativeArray<Vector3> m_InertialScaleVelocityResiduals;
        NativeArray<float> m_InertialParameterResiduals;

        [ReadOnly]
        readonly NativeArray<AnimationPlayerPoseNativeRange> m_SlotRanges;
        [ReadOnly]
        readonly NativeArray<AnimationLocalBonePose> m_SlotDenseLocalPoses;
        [ReadOnly]
        readonly NativeArray<AnimationBlendBoneVelocity> m_SlotDenseVelocities;
        [ReadOnly]
        readonly NativeArray<float> m_SlotPoseParameters;
        [ReadOnly]
        readonly NativeArray<byte> m_SlotPoseParameterAvailability;
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
        readonly NativeArray<AnimationPoseAvailability> m_SlotAvailability;
        [ReadOnly]
        readonly NativeArray<ulong> m_SlotContinuityIdentities;
        [ReadOnly]
        readonly NativeArray<PoseDiscontinuity> m_SlotDiscontinuities;
        [ReadOnly]
        readonly NativeArray<AnimationPoseNativeInvalidReason> m_SlotInvalidReasons;
        [ReadOnly]
        readonly NativeArray<ulong> m_SlotCompletedAt;

        NativeArray<AnimationLocalBonePose> m_ValueDenseLocalPoses;
        NativeArray<AnimationBlendBoneVelocity> m_ValueDenseVelocities;
        NativeArray<float> m_ValuePoseParameters;
        NativeArray<byte> m_ValuePoseParameterAvailability;
        NativeArray<AnimationPrimitivePoseContribution> m_ValueContributions;
        NativeArray<float> m_ValueDenseContributionWeights;
        NativeArray<int> m_ValueContributionCounts;
        NativeArray<float> m_ValueOutputWeights;
        NativeArray<AnimationFootFeatureSample> m_ValueLeftFootFeatures;
        NativeArray<AnimationFootFeatureSample> m_ValueRightFootFeatures;
        NativeArray<byte> m_ValueHasFootFeatures;
        NativeArray<AnimationPoseAvailability> m_ValueAvailability;
        NativeArray<ulong> m_ValueContinuityIdentities;
        NativeArray<PoseDiscontinuity> m_ValueDiscontinuities;
        NativeArray<AnimationPoseNativeInvalidReason> m_ValueInvalidReasons;
        NativeArray<ulong> m_FrameCacheCompletedAt;
        NativeArray<AnimationPoseNativeInvalidReason> m_PoseGraphInvalidReason;
        NativeArray<int> m_PoseGraphInvalidOperationIndex;
        NativeArray<ulong> m_PoseGraphCompletedAt;

        readonly int m_PlayerCount;
        readonly int m_BoneCount;
        readonly int m_ParameterCount;
        readonly int m_PoseValueCount;
        readonly int m_ContributionStride;
        readonly int m_OutputOperationIndex;
        readonly int m_OutputValueIndex;
        readonly int m_LeftFootBoneIndex;
        readonly int m_RightFootBoneIndex;
        readonly ulong m_CompletionIdentity;

        internal CharacterPoseGraphNativeJob(
            CharacterPoseGraphNativeProgram program,
            PoseInertializationNativeProgram inertializationProgram,
            CharacterPoseGraphNativeBinding binding)
        {
            RequireValidConfiguration(program, inertializationProgram, binding);

            m_Operations = program.Operations;
            m_DenseBoneMasks = program.DenseBoneMasks;
            m_AdditiveReferences = program.AdditiveReferences;
            m_ParameterPolicies = program.ParameterPolicies;
            m_ParameterDefaults = program.ParameterDefaults;
            m_ParentIndices = program.ParentIndices;
            m_Inertializations = inertializationProgram.Nodes;
            m_ModifyBones = program.ModifyBones;
            m_InertialRules = inertializationProgram.Rules;
            m_InertialCurveSegments = inertializationProgram.CurveSegments;
            m_InertialDenseProfiles = inertializationProgram.DenseProfiles;
            m_InertialParameterModes = inertializationProgram.ParameterModes;
            m_InertialStates = inertializationProgram.States;
            m_InertialHistory = inertializationProgram.HistoryPoses;
            m_InertialHistoryVelocities = inertializationProgram.HistoryVelocities;
            m_InertialHistoryParameters = inertializationProgram.HistoryParameters;
            m_InertialHistoryParameterAvailability = inertializationProgram.HistoryParameterAvailability;
            m_InertialHistoryLeftFeet = inertializationProgram.HistoryLeftFeet;
            m_InertialHistoryRightFeet = inertializationProgram.HistoryRightFeet;
            m_InertialHistoryHasFeet = inertializationProgram.HistoryHasFeet;
            m_InertialAccumulatorLeftFeet = inertializationProgram.AccumulatorLeftFeet;
            m_InertialAccumulatorRightFeet = inertializationProgram.AccumulatorRightFeet;
            m_InertialAccumulatorHasFeet = inertializationProgram.AccumulatorHasFeet;
            m_InertialPositionResiduals = inertializationProgram.PositionResiduals;
            m_InertialRotationResiduals = inertializationProgram.RotationResiduals;
            m_InertialScaleResiduals = inertializationProgram.ScaleResiduals;
            m_InertialLinearVelocityResiduals = inertializationProgram.LinearVelocityResiduals;
            m_InertialAngularVelocityResiduals = inertializationProgram.AngularVelocityResiduals;
            m_InertialScaleVelocityResiduals = inertializationProgram.ScaleVelocityResiduals;
            m_InertialParameterResiduals = inertializationProgram.ParameterResiduals;

            m_SlotRanges = binding.SlotRanges;
            m_SlotDenseLocalPoses = binding.SlotDenseLocalPoses;
            m_SlotDenseVelocities = binding.SlotDenseVelocities;
            m_SlotPoseParameters = binding.SlotPoseParameters;
            m_SlotPoseParameterAvailability = binding.SlotPoseParameterAvailability;
            m_SlotContributions = binding.SlotContributions;
            m_SlotDenseContributionWeights = binding.SlotDenseContributionWeights;
            m_SlotContributionCounts = binding.SlotContributionCounts;
            m_SlotOutputWeights = binding.SlotOutputWeights;
            m_SlotLeftFootFeatures = binding.SlotLeftFootFeatures;
            m_SlotRightFootFeatures = binding.SlotRightFootFeatures;
            m_SlotHasFootFeatures = binding.SlotHasFootFeatures;
            m_SlotAvailability = binding.SlotAvailability;
            m_SlotContinuityIdentities = binding.SlotContinuityIdentities;
            m_SlotDiscontinuities = binding.SlotDiscontinuities;
            m_SlotInvalidReasons = binding.SlotInvalidReasons;
            m_SlotCompletedAt = binding.SlotCompletedAt;

            m_ValueDenseLocalPoses = binding.ValueDenseLocalPoses;
            m_ValueDenseVelocities = binding.ValueDenseVelocities;
            m_ValuePoseParameters = binding.ValuePoseParameters;
            m_ValuePoseParameterAvailability = binding.ValuePoseParameterAvailability;
            m_ValueContributions = binding.ValueContributions;
            m_ValueDenseContributionWeights = binding.ValueDenseContributionWeights;
            m_ValueContributionCounts = binding.ValueContributionCounts;
            m_ValueOutputWeights = binding.ValueOutputWeights;
            m_ValueLeftFootFeatures = binding.ValueLeftFootFeatures;
            m_ValueRightFootFeatures = binding.ValueRightFootFeatures;
            m_ValueHasFootFeatures = binding.ValueHasFootFeatures;
            m_ValueAvailability = binding.ValueAvailability;
            m_ValueContinuityIdentities = binding.ValueContinuityIdentities;
            m_ValueDiscontinuities = binding.ValueDiscontinuities;
            m_ValueInvalidReasons = binding.ValueInvalidReasons;
            m_FrameCacheCompletedAt = binding.FrameCacheCompletedAt;
            m_PoseGraphInvalidReason = binding.PoseGraphInvalidReason;
            m_PoseGraphInvalidOperationIndex = binding.PoseGraphInvalidOperationIndex;
            m_PoseGraphCompletedAt = binding.PoseGraphCompletedAt;

            m_PlayerCount = binding.Layout.PlayerCount;
            m_BoneCount = binding.Layout.BoneCount;
            m_ParameterCount = binding.Layout.ParameterCount;
            m_PoseValueCount = binding.Layout.PoseValueCount;
            m_ContributionStride = binding.Layout.PoseValueContributionStride;
            m_OutputOperationIndex = program.OutputOperationIndex;
            m_OutputValueIndex = program.OutputValueIndex;
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
                ResetValue(operation.OutputValueIndex);
                switch (operation.Code)
                {
                    case CharacterPoseOperationCode.SelectedPosePlayer:
                    case CharacterPoseOperationCode.BlendSpacePlayer:
                    case CharacterPoseOperationCode.BlendStack:
                        EvaluatePlayerInput(operation);
                        break;
                    case CharacterPoseOperationCode.Inertialization:
                        EvaluateInertialization(operation, stream.deltaTime);
                        break;
                    case CharacterPoseOperationCode.BlendPose:
                        EvaluateBlendPose(operation);
                        break;
                    case CharacterPoseOperationCode.LayeredBoneBlend:
                        EvaluateLayeredBoneBlend(operation);
                        break;
                    case CharacterPoseOperationCode.AdditivePose:
                        EvaluateAdditivePose(operation);
                        break;
                    case CharacterPoseOperationCode.PoseParameterResolve:
                        EvaluatePoseParameterResolve(operation);
                        break;
                    case CharacterPoseOperationCode.ModifyBone:
                        EvaluateModifyBone(operation);
                        break;
                    case CharacterPoseOperationCode.FootPlacement:
                        EvaluateWorldAwareBoundary(operation);
                        break;
                    case CharacterPoseOperationCode.OutputPose:
                        EvaluateOutputPose(operation);
                        break;
                    default:
                        SetInvalid(
                            operation.OutputValueIndex,
                            (ulong)operation.Index + 1UL,
                            AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid,
                            operation.Index);
                        break;
                }

                if (!TryValidateValue(operation.OutputValueIndex, out AnimationPoseNativeInvalidReason reason))
                {
                    SetInvalid(
                        operation.OutputValueIndex,
                        m_ValueContinuityIdentities[operation.OutputValueIndex],
                        reason,
                        operation.Index);
                }
                m_FrameCacheCompletedAt[operation.FrameCacheIndex] = m_CompletionIdentity;
            }

            if (m_ValueAvailability[m_OutputValueIndex] != AnimationPoseAvailability.Pose)
            {
                if (m_ValueAvailability[m_OutputValueIndex] != AnimationPoseAvailability.Invalid)
                {
                    SetInvalid(
                        m_OutputValueIndex,
                        m_ValueContinuityIdentities[m_OutputValueIndex],
                        AnimationPoseNativeInvalidReason.PoseGraphOutputInvalid,
                        m_OutputOperationIndex);
                }
                else if (m_PoseGraphInvalidReason[0] == AnimationPoseNativeInvalidReason.None)
                {
                    RecordGraphInvalid(
                        NormalizeInvalidReason(m_ValueInvalidReasons[m_OutputValueIndex]),
                        m_OutputOperationIndex);
                }
            }
            else if (m_ValueContributionCounts[m_OutputValueIndex] <= 0 ||
                     m_ValueInvalidReasons[m_OutputValueIndex] != AnimationPoseNativeInvalidReason.None)
            {
                SetInvalid(
                    m_OutputValueIndex,
                    m_ValueContinuityIdentities[m_OutputValueIndex],
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

        void EvaluatePlayerInput(AnimationPoseGraphNativeOperation operation)
        {
            int output = operation.OutputValueIndex;
            int slotIndex = operation.PhysicalPlayerIndex;
            ulong continuity = slotIndex >= 0 && slotIndex < m_PlayerCount
                ? m_SlotContinuityIdentities[slotIndex]
                : 0UL;
            if (slotIndex < 0 || slotIndex >= m_PlayerCount ||
                m_SlotCompletedAt[slotIndex] != m_CompletionIdentity)
            {
                SetInvalid(
                    output,
                    continuity,
                    AnimationPoseNativeInvalidReason.PoseGraphInputIncomplete,
                    operation.Index);
                return;
            }

            AnimationPoseAvailability availability = m_SlotAvailability[slotIndex];
            AnimationPoseNativeInvalidReason slotReason = m_SlotInvalidReasons[slotIndex];
            PoseDiscontinuity discontinuity = m_SlotDiscontinuities[slotIndex];
            if (availability == AnimationPoseAvailability.Invalid)
            {
                SetInvalid(output, continuity, NormalizeInvalidReason(slotReason), operation.Index);
                return;
            }
            if (!IsAvailability(availability) || slotReason != AnimationPoseNativeInvalidReason.None || continuity == 0 ||
                !discontinuity.IsValid || discontinuity.IsPresent && discontinuity.CompletionIdentity != m_CompletionIdentity)
            {
                SetInvalid(
                    output,
                    continuity,
                    AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid,
                    operation.Index);
                return;
            }
            if (availability == AnimationPoseAvailability.NoPose &&
                operation.AnimationSelectionAvailabilityPolicy == AnimationSelectionAvailabilityPolicy.RequireSelection)
            {
                SetInvalid(output, continuity, AnimationPoseNativeInvalidReason.RequiredPoseMissing, operation.Index);
                return;
            }

            AnimationPlayerPoseNativeRange range = m_SlotRanges[slotIndex];
            int contributionCount = m_SlotContributionCounts[slotIndex];
            float outputWeight = m_SlotOutputWeights[slotIndex];
            byte hasFootFeatures = m_SlotHasFootFeatures[slotIndex];
            if (range.PhysicalPlayerIndex != slotIndex || contributionCount < 0 ||
                contributionCount > range.ContributionCapacity || contributionCount > m_ContributionStride ||
                !IsWeight(outputWeight) || hasFootFeatures > 1)
            {
                SetInvalid(output, continuity, AnimationPoseNativeInvalidReason.SlotPlanInvalid, operation.Index);
                return;
            }

            for (int parameter = 0; parameter < m_ParameterCount; parameter++)
            {
                float value = m_SlotPoseParameters[range.ParameterOffset + parameter];
                byte parameterAvailable = m_SlotPoseParameterAvailability[range.ParameterOffset + parameter];
                if (!float.IsFinite(value) || parameterAvailable > 1)
                {
                    SetInvalid(output, continuity, AnimationPoseNativeInvalidReason.SlotParameterInvalid, operation.Index);
                    return;
                }
                m_ValuePoseParameters[ParameterOffset(output) + parameter] = value;
                m_ValuePoseParameterAvailability[ParameterOffset(output) + parameter] = parameterAvailable;
            }

            if (availability == AnimationPoseAvailability.NoPose)
            {
                if (contributionCount != 0 || outputWeight != 0f || hasFootFeatures != 0)
                {
                    SetInvalid(output, continuity, AnimationPoseNativeInvalidReason.SlotPlanInvalid, operation.Index);
                    return;
                }
                m_ValueAvailability[output] = AnimationPoseAvailability.NoPose;
                m_ValueContinuityIdentities[output] = continuity;
                m_ValueInvalidReasons[output] = AnimationPoseNativeInvalidReason.None;
                return;
            }

            if (contributionCount <= 0)
            {
                SetInvalid(output, continuity, AnimationPoseNativeInvalidReason.SlotPlanInvalid, operation.Index);
                return;
            }
            for (int bone = 0; bone < m_BoneCount; bone++)
            {
                AnimationLocalBonePose pose = m_SlotDenseLocalPoses[range.PoseOffset + bone];
                AnimationBlendBoneVelocity velocity = m_SlotDenseVelocities[range.VelocityOffset + bone];
                if (!pose.IsValid || !velocity.IsValid)
                {
                    SetInvalid(output, continuity, AnimationPoseNativeInvalidReason.SlotPoseInvalid, operation.Index);
                    return;
                }
                m_ValueDenseLocalPoses[PoseOffset(output) + bone] = pose;
                m_ValueDenseVelocities[PoseOffset(output) + bone] = velocity;
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
                if (!IsValidPrimitiveContribution(primitive) || primitive.PhysicalPlayerIndex != slotIndex)
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
            m_ValueAvailability[output] = AnimationPoseAvailability.Pose;
            m_ValueContinuityIdentities[output] = continuity;
            m_ValueDiscontinuities[output] = m_SlotDiscontinuities[slotIndex];
            m_ValueInvalidReasons[output] = AnimationPoseNativeInvalidReason.None;
        }

        void EvaluateInertialization(AnimationPoseGraphNativeOperation operation, float deltaSeconds)
        {
            int output = operation.OutputValueIndex;
            int input = operation.InputValueIndexA;
            if (!IsInputReady(input, operation.Index) ||
                (uint)operation.InertializationIndex >= (uint)m_Inertializations.Length ||
                !float.IsFinite(deltaSeconds) || deltaSeconds < 0f)
            {
                ClearInertialState(operation.InertializationIndex, PoseInertializationRuntimeState.Invalid);
                SetInvalid(output, (ulong)operation.Index + 1UL, AnimationPoseNativeInvalidReason.PoseGraphInputIncomplete, operation.Index);
                return;
            }
            PoseDiscontinuity discontinuity = m_ValueDiscontinuities[input];
            if (!TryCopyValue(input, output, operation.Index))
            {
                ClearInertialState(operation.InertializationIndex, PoseInertializationRuntimeState.Invalid);
                SetInvalid(output, m_ValueContinuityIdentities[input], AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid, operation.Index);
                return;
            }
            int stateIndex = operation.InertializationIndex;
            PoseInertializationNativeState state = m_InertialStates[stateIndex];
            if (m_ValueAvailability[input] != AnimationPoseAvailability.Pose)
            {
                ClearInertialState(stateIndex, PoseInertializationRuntimeState.Reset);
                return;
            }

            if (discontinuity.IsReset)
            {
                state = default;
                state.LastEventIdentity = discontinuity.EventIdentity;
                state.RuntimeState = PoseInertializationRuntimeState.Reset;
                state.LastResetReason = discontinuity.ResetReason;
                state.LastResetSequence = discontinuity.ResetSequence;
                state.OutputCompletionIdentity = m_CompletionIdentity;
            }
            else if (discontinuity.IsPresent)
            {
                if (discontinuity.EventIdentity <= state.LastEventIdentity ||
                    discontinuity.HasPreviousEndpoint != 1 || discontinuity.HasCurrentEndpoint != 1)
                {
                    ClearInertialState(stateIndex, PoseInertializationRuntimeState.Invalid);
                    SetInvalid(output, m_ValueContinuityIdentities[input], AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid, operation.Index);
                    return;
                }
                int ruleIndex = RequireInertialRule(
                    stateIndex,
                    discontinuity.PreviousEndpoint.ProgramProducerIndex,
                    discontinuity.CurrentEndpoint.ProgramProducerIndex);
                if (ruleIndex < 0)
                {
                    ClearInertialState(stateIndex, PoseInertializationRuntimeState.Invalid);
                    SetInvalid(output, m_ValueContinuityIdentities[input], AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid, operation.Index);
                    return;
                }
                PoseInertializationNativeRule rule = m_InertialRules[ruleIndex];
                bool rebase = state.Active != 0;
                state.LastEventIdentity = discontinuity.EventIdentity;
                state.LastReason = discontinuity.Reason;
                state.PreviousEndpoint = discontinuity.PreviousEndpoint;
                state.CurrentEndpoint = discontinuity.CurrentEndpoint;
                state.PreviousContinuityIdentity = discontinuity.PreviousContinuityIdentity;
                state.CurrentContinuityIdentity = discontinuity.CurrentContinuityIdentity;
                if (state.HasHistory != 0 && rule.Mode == PoseInertializationMode.Inertialize)
                {
                    CaptureInertialResidual(stateIndex, input, ruleIndex, ref state);
                    state.RuntimeState = rebase
                        ? PoseInertializationRuntimeState.Rebase
                        : PoseInertializationRuntimeState.Capture;
                }
                else
                {
                    state.Active = 0;
                    state.ElapsedSeconds = 0f;
                    state.ActiveRuleIndex = ruleIndex;
                    state.RuntimeState = rule.Mode == PoseInertializationMode.HardCut
                        ? PoseInertializationRuntimeState.HardCut
                        : PoseInertializationRuntimeState.Anchor;
                }
            }

            if (state.Active != 0)
            {
                if (!discontinuity.IsPresent)
                    state.RuntimeState = PoseInertializationRuntimeState.Continue;
                PoseInertializationNativeRule rule = m_InertialRules[state.ActiveRuleIndex];
                bool anyActive = false;
                for (int bone = 0; bone < m_BoneCount; bone++)
                {
                    int residualIndex = stateIndex * m_BoneCount + bone;
                    float duration = rule.DurationSeconds * m_InertialDenseProfiles[rule.ProfileOffset + bone];
                    EvaluateInertialEnvelope(rule, state.ElapsedSeconds, duration, out float envelope, out float weight, out float derivative);
                    anyActive |= state.ElapsedSeconds < duration;
                    AnimationLocalBonePose target = m_ValueDenseLocalPoses[PoseOffset(input) + bone];
                    AnimationBlendBoneVelocity targetVelocity = m_ValueDenseVelocities[PoseOffset(input) + bone];
                    Vector3 positionBase = m_InertialPositionResiduals[residualIndex] +
                                           state.ElapsedSeconds * m_InertialLinearVelocityResiduals[residualIndex];
                    Vector3 rotationBase = m_InertialRotationResiduals[residualIndex] +
                                           state.ElapsedSeconds * m_InertialAngularVelocityResiduals[residualIndex];
                    Vector3 scaleBase = m_InertialScaleResiduals[residualIndex] +
                                        state.ElapsedSeconds * m_InertialScaleVelocityResiduals[residualIndex];
                    Vector3 linear = targetVelocity.Linear + derivative * positionBase +
                                     weight * m_InertialLinearVelocityResiduals[residualIndex];
                    Vector3 angular = targetVelocity.Angular + derivative * rotationBase +
                                      weight * m_InertialAngularVelocityResiduals[residualIndex];
                    Vector3 scaleVelocity = targetVelocity.Scale + derivative * scaleBase +
                                            weight * m_InertialScaleVelocityResiduals[residualIndex];
                    if (!IsFinite(linear) || !IsFinite(angular) || !IsFinite(scaleVelocity))
                    {
                        ClearInertialState(stateIndex, PoseInertializationRuntimeState.Invalid);
                        SetInvalid(output, m_ValueContinuityIdentities[input], AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid, operation.Index);
                        return;
                    }
                    m_ValueDenseLocalPoses[PoseOffset(output) + bone] = new AnimationLocalBonePose(
                        target.Position + weight * positionBase,
                        AnimationPoseMath.QuaternionExp(weight * rotationBase) * target.Rotation,
                        target.Scale + weight * scaleBase);
                    m_ValueDenseVelocities[PoseOffset(output) + bone] =
                        new AnimationBlendBoneVelocity(linear, angular, scaleVelocity);
                }
                ApplyInertialParameters(stateIndex, input, output, rule, state.ElapsedSeconds);
                ApplyInertialFootFeatures(stateIndex, input, output, rule, state.ElapsedSeconds);
                state.ElapsedSeconds += deltaSeconds;
                state.LastDeltaSeconds = deltaSeconds;
                if (!anyActive)
                {
                    state.Active = 0;
                    state.RuntimeState = PoseInertializationRuntimeState.Complete;
                }
            }

            CommitInertialHistory(stateIndex, output, ref state);
            if (state.RuntimeState == 0)
                state.RuntimeState = PoseInertializationRuntimeState.Anchor;
            state.OutputCompletionIdentity = m_CompletionIdentity;
            m_InertialStates[stateIndex] = state;
        }

        void CaptureInertialResidual(
            int stateIndex,
            int input,
            int ruleIndex,
            ref PoseInertializationNativeState state)
        {
            int historyPoseOffset = (stateIndex * 2 + state.HistoryPage) * m_BoneCount;
            int residualOffset = stateIndex * m_BoneCount;
            for (int bone = 0; bone < m_BoneCount; bone++)
            {
                AnimationLocalBonePose previous = m_InertialHistory[historyPoseOffset + bone];
                AnimationBlendBoneVelocity previousVelocity = m_InertialHistoryVelocities[historyPoseOffset + bone];
                AnimationLocalBonePose target = m_ValueDenseLocalPoses[PoseOffset(input) + bone];
                AnimationBlendBoneVelocity targetVelocity = m_ValueDenseVelocities[PoseOffset(input) + bone];
                m_InertialPositionResiduals[residualOffset + bone] = previous.Position - target.Position;
                m_InertialRotationResiduals[residualOffset + bone] =
                    AnimationPoseMath.QuaternionLog(previous.Rotation * Quaternion.Inverse(target.Rotation));
                m_InertialScaleResiduals[residualOffset + bone] = previous.Scale - target.Scale;
                m_InertialLinearVelocityResiduals[residualOffset + bone] = previousVelocity.Linear - targetVelocity.Linear;
                m_InertialAngularVelocityResiduals[residualOffset + bone] = previousVelocity.Angular - targetVelocity.Angular;
                m_InertialScaleVelocityResiduals[residualOffset + bone] = previousVelocity.Scale - targetVelocity.Scale;
            }
            PoseInertializationNativeRule rule = m_InertialRules[ruleIndex];
            int historyParameterOffset = (stateIndex * 2 + state.HistoryPage) * m_ParameterCount;
            int residualParameterOffset = stateIndex * m_ParameterCount;
            for (int parameter = 0; parameter < m_ParameterCount; parameter++)
            {
                m_InertialParameterResiduals[residualParameterOffset + parameter] =
                    m_InertialParameterModes[rule.ParameterModeOffset + parameter] == PoseParameterInertializationMode.Inertialize &&
                    m_InertialHistoryParameterAvailability[historyParameterOffset + parameter] != 0 &&
                    m_ValuePoseParameterAvailability[ParameterOffset(input) + parameter] != 0
                        ? m_InertialHistoryParameters[historyParameterOffset + parameter] -
                          m_ValuePoseParameters[ParameterOffset(input) + parameter]
                        : 0f;
            }
            int historyFootIndex = stateIndex * 2 + state.HistoryPage;
            m_InertialAccumulatorLeftFeet[stateIndex] = m_InertialHistoryLeftFeet[historyFootIndex];
            m_InertialAccumulatorRightFeet[stateIndex] = m_InertialHistoryRightFeet[historyFootIndex];
            m_InertialAccumulatorHasFeet[stateIndex] = m_InertialHistoryHasFeet[historyFootIndex];
            state.ActiveRuleIndex = ruleIndex;
            state.ElapsedSeconds = 0f;
            state.AccumulatorGeneration++;
            state.Active = 1;
        }

        void ApplyInertialParameters(
            int stateIndex,
            int input,
            int output,
            PoseInertializationNativeRule rule,
            float elapsedSeconds)
        {
            EvaluateInertialEnvelope(rule, elapsedSeconds, rule.DurationSeconds, out _, out float weight, out _);
            int residualOffset = stateIndex * m_ParameterCount;
            for (int parameter = 0; parameter < m_ParameterCount; parameter++)
            {
                if (m_InertialParameterModes[rule.ParameterModeOffset + parameter] == PoseParameterInertializationMode.Inertialize &&
                    m_ValuePoseParameterAvailability[ParameterOffset(input) + parameter] != 0)
                {
                    m_ValuePoseParameters[ParameterOffset(output) + parameter] =
                        m_ValuePoseParameters[ParameterOffset(input) + parameter] +
                        weight * m_InertialParameterResiduals[residualOffset + parameter];
                }
            }
        }

        void ApplyInertialFootFeatures(
            int stateIndex,
            int input,
            int output,
            PoseInertializationNativeRule rule,
            float elapsedSeconds)
        {
            if (m_InertialAccumulatorHasFeet[stateIndex] == 0 || m_ValueHasFootFeatures[input] == 0)
                return;
            float leftDuration = rule.DurationSeconds * m_InertialDenseProfiles[rule.ProfileOffset + m_LeftFootBoneIndex];
            float rightDuration = rule.DurationSeconds * m_InertialDenseProfiles[rule.ProfileOffset + m_RightFootBoneIndex];
            EvaluateInertialEnvelope(rule, elapsedSeconds, leftDuration, out float leftEnvelope, out _, out _);
            EvaluateInertialEnvelope(rule, elapsedSeconds, rightDuration, out float rightEnvelope, out _, out _);
            if (TryResolveFeature(
                    true,
                    m_InertialAccumulatorLeftFeet[stateIndex],
                    true,
                    m_ValueLeftFootFeatures[input],
                    leftEnvelope,
                    out AnimationFootFeatureSample left) &&
                TryResolveFeature(
                    true,
                    m_InertialAccumulatorRightFeet[stateIndex],
                    true,
                    m_ValueRightFootFeatures[input],
                    rightEnvelope,
                    out AnimationFootFeatureSample right))
            {
                m_ValueLeftFootFeatures[output] = left;
                m_ValueRightFootFeatures[output] = right;
                m_ValueHasFootFeatures[output] = 1;
                ScaleContributionFootWeights(output, leftEnvelope, rightEnvelope);
            }
        }

        void ScaleContributionFootWeights(int value, float leftEnvelope, float rightEnvelope)
        {
            int count = m_ValueContributionCounts[value];
            for (int contribution = 0; contribution < count; contribution++)
            {
                int index = ContributionOffset(value) + contribution;
                AnimationPrimitivePoseContribution source = m_ValueContributions[index];
                m_ValueContributions[index] = new AnimationPrimitivePoseContribution(
                    source.PhysicalPlayerIndex,
                    source.PhysicalSourceIndex,
                    source.PhysicalSourceGeneration,
                    source.Kind,
                    source.ProgramProducerIndex,
                    source.ContributionContinuityIdentity,
                    source.Weight,
                    source.LeftFootWeight * leftEnvelope,
                    source.RightFootWeight * rightEnvelope);
            }
        }

        void CommitInertialHistory(int stateIndex, int output, ref PoseInertializationNativeState state)
        {
            int page = state.HasHistory == 0 ? 0 : 1 - state.HistoryPage;
            int poseOffset = (stateIndex * 2 + page) * m_BoneCount;
            for (int bone = 0; bone < m_BoneCount; bone++)
            {
                m_InertialHistory[poseOffset + bone] = m_ValueDenseLocalPoses[PoseOffset(output) + bone];
                m_InertialHistoryVelocities[poseOffset + bone] = m_ValueDenseVelocities[PoseOffset(output) + bone];
            }
            int parameterOffset = (stateIndex * 2 + page) * m_ParameterCount;
            for (int parameter = 0; parameter < m_ParameterCount; parameter++)
            {
                m_InertialHistoryParameters[parameterOffset + parameter] = m_ValuePoseParameters[ParameterOffset(output) + parameter];
                m_InertialHistoryParameterAvailability[parameterOffset + parameter] =
                    m_ValuePoseParameterAvailability[ParameterOffset(output) + parameter];
            }
            int footIndex = stateIndex * 2 + page;
            m_InertialHistoryLeftFeet[footIndex] = m_ValueLeftFootFeatures[output];
            m_InertialHistoryRightFeet[footIndex] = m_ValueRightFootFeatures[output];
            m_InertialHistoryHasFeet[footIndex] = m_ValueHasFootFeatures[output];
            state.HistoryPage = page;
            state.HasHistory = 1;
            state.HistoryCompletionIdentity = m_CompletionIdentity;
        }

        void ClearInertialState(int stateIndex, PoseInertializationRuntimeState runtimeState)
        {
            if ((uint)stateIndex < (uint)m_InertialStates.Length)
            {
                m_InertialStates[stateIndex] = new PoseInertializationNativeState
                {
                    RuntimeState = runtimeState,
                    OutputCompletionIdentity = m_CompletionIdentity
                };
            }
        }

        int RequireInertialRule(int stateIndex, int sourceProducerIndex, int targetProducerIndex)
        {
            PoseInertializationNativeNode node = m_Inertializations[stateIndex];
            int match = -1;
            for (int i = 0; i < node.RuleCount; i++)
            {
                int index = node.RuleOffset + i;
                PoseInertializationNativeRule rule = m_InertialRules[index];
                if (rule.SourceProducerIndex != sourceProducerIndex || rule.TargetProducerIndex != targetProducerIndex)
                    continue;
                if (match >= 0)
                    return -1;
                match = index;
            }
            return match;
        }

        void EvaluateInertialEnvelope(
            PoseInertializationNativeRule rule,
            float elapsedSeconds,
            float durationSeconds,
            out float envelope,
            out float residualWeight,
            out float residualDerivativePerSecond)
        {
            if (durationSeconds <= 0f || elapsedSeconds >= durationSeconds)
            {
                envelope = 1f;
                residualWeight = 0f;
                residualDerivativePerSecond = 0f;
                return;
            }
            float normalized = Mathf.Clamp01(elapsedSeconds / durationSeconds);
            EvaluateInertialCurve(rule, normalized, out float curve, out float derivative);
            EvaluateInertialCurve(rule, 0f, out _, out float startDerivative);
            EvaluateInertialCurve(rule, 1f, out _, out float endDerivative);
            float s2 = normalized * normalized;
            float s3 = s2 * normalized;
            float h10 = s3 - 2f * s2 + normalized;
            float h11 = s3 - s2;
            float h10Derivative = 3f * s2 - 4f * normalized + 1f;
            float h11Derivative = 3f * s2 - 2f * normalized;
            envelope = Mathf.Clamp01(curve - startDerivative * h10 - endDerivative * h11);
            float envelopeDerivative = derivative - startDerivative * h10Derivative - endDerivative * h11Derivative;
            residualWeight = 1f - envelope;
            residualDerivativePerSecond = -envelopeDerivative / durationSeconds;
        }

        void EvaluateInertialCurve(
            PoseInertializationNativeRule rule,
            float normalizedTime,
            out float value,
            out float derivative)
        {
            float time = Mathf.Clamp01(normalizedTime);
            AnimationBlendCurveSegment segment = m_InertialCurveSegments[rule.CurveOffset + rule.CurveCount - 1];
            for (int i = 0; i < rule.CurveCount; i++)
            {
                AnimationBlendCurveSegment candidate = m_InertialCurveSegments[rule.CurveOffset + i];
                if (time <= candidate.EndTime)
                {
                    segment = candidate;
                    break;
                }
            }
            float u = (time - segment.StartTime) / (segment.EndTime - segment.StartTime);
            value = Mathf.Clamp01(((segment.A * u + segment.B) * u + segment.C) * u + segment.D);
            derivative = ((3f * segment.A * u + 2f * segment.B) * u + segment.C) /
                         (segment.EndTime - segment.StartTime);
        }

        void EvaluateBlendPose(AnimationPoseGraphNativeOperation operation)
        {
            float weight = operation.Weight;
            if (operation.ParameterIndex >= 0)
            {
                int input = operation.InputValueIndexA;
                if (!IsInputReady(input, operation.Index) || operation.ParameterIndex >= m_ParameterCount)
                {
                    SetInvalid(operation.OutputValueIndex, (ulong)operation.Index + 1UL, AnimationPoseNativeInvalidReason.PoseGraphInputIncomplete, operation.Index);
                    return;
                }
                if (m_ValuePoseParameterAvailability[ParameterOffset(input) + operation.ParameterIndex] == 0)
                {
                    SetInvalid(operation.OutputValueIndex, (ulong)operation.Index + 1UL, AnimationPoseNativeInvalidReason.SlotParameterInvalid, operation.Index);
                    return;
                }
                weight = Mathf.Clamp01(m_ValuePoseParameters[ParameterOffset(input) + operation.ParameterIndex]);
            }
            EvaluateLayeredBoneBlend(operation.WithWeight(weight));
        }

        void EvaluateModifyBone(AnimationPoseGraphNativeOperation operation)
        {
            int input = operation.InputValueIndexA;
            int output = operation.OutputValueIndex;
            if (!IsInputReady(input, operation.Index) ||
                (uint)operation.ModifyBoneIndex >= (uint)m_ModifyBones.Length ||
                !TryCopyValue(input, output, operation.Index))
            {
                SetInvalid(output, (ulong)operation.Index + 1UL, AnimationPoseNativeInvalidReason.PoseGraphInputIncomplete, operation.Index);
                return;
            }
            if (m_ValueAvailability[output] != AnimationPoseAvailability.Pose)
                return;
            AnimationPoseGraphNativeModifyBone modify = m_ModifyBones[operation.ModifyBoneIndex];
            AnimationLocalBonePose current;
            if (modify.ReferenceSpace == ModifyBoneReferenceSpace.Local)
            {
                current = m_ValueDenseLocalPoses[PoseOffset(output) + modify.BoneIndex];
            }
            else if (!TryResolveModelPose(output, modify.BoneIndex, out current))
            {
                SetInvalid(output, m_ValueContinuityIdentities[output], AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid, operation.Index);
                return;
            }
            Vector3 position = (modify.Operations & ModifyBoneOperationMask.Position) != 0
                ? current.Position + modify.Position * operation.Weight
                : current.Position;
            Quaternion rotation = (modify.Operations & ModifyBoneOperationMask.Rotation) != 0
                ? Quaternion.SlerpUnclamped(Quaternion.identity, modify.Rotation, operation.Weight) * current.Rotation
                : current.Rotation;
            Vector3 scale = (modify.Operations & ModifyBoneOperationMask.Scale) != 0
                ? Vector3.Scale(current.Scale, Vector3.LerpUnclamped(Vector3.one, modify.Scale, operation.Weight))
                : current.Scale;
            var modified = new AnimationLocalBonePose(position, rotation, scale);
            if (modify.ReferenceSpace == ModifyBoneReferenceSpace.Mesh && modify.ParentBoneIndex >= 0)
            {
                if (!TryResolveModelPose(output, modify.ParentBoneIndex, out AnimationLocalBonePose parent) ||
                    !TryToLocal(parent, modified, out modified))
                {
                    SetInvalid(output, m_ValueContinuityIdentities[output], AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid, operation.Index);
                    return;
                }
            }
            m_ValueDenseLocalPoses[PoseOffset(output) + modify.BoneIndex] = modified;
        }

        void EvaluateWorldAwareBoundary(AnimationPoseGraphNativeOperation operation)
        {
            int input = operation.InputValueIndexA;
            if (!IsInputReady(input, operation.Index) || !TryCopyValue(input, operation.OutputValueIndex, operation.Index))
                SetInvalid(operation.OutputValueIndex, (ulong)operation.Index + 1UL, AnimationPoseNativeInvalidReason.PoseGraphInputIncomplete, operation.Index);
        }

        void EvaluateLayeredBoneBlend(AnimationPoseGraphNativeOperation operation)
        {
            int output = operation.OutputValueIndex;
            int baseValue = operation.InputValueIndexA;
            int overlayValue = operation.InputValueIndexB;
            if (!TryRequireInputs(operation, baseValue, overlayValue))
                return;
            if (m_ValueAvailability[overlayValue] == AnimationPoseAvailability.NoPose)
            {
                if (!TryCopyValue(baseValue, output, operation.Index))
                    SetInvalid(output, m_ValueContinuityIdentities[baseValue], AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid, operation.Index);
                return;
            }
            if (m_ValueAvailability[baseValue] == AnimationPoseAvailability.NoPose)
            {
                if (!TryCopyValue(overlayValue, output, operation.Index) ||
                    !TryScaleValue(output, operation))
                {
                    SetInvalid(output, m_ValueContinuityIdentities[overlayValue], AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid, operation.Index);
                }
                return;
            }

            m_ValueAvailability[output] = AnimationPoseAvailability.Pose;
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
            if (!TryCopyParameters(baseValue, output) ||
                !TryMergeContributions(operation, baseValue, overlayValue, output, false) ||
                !TryResolveFootFeatures(operation, baseValue, overlayValue, output, false))
            {
                SetInvalid(output, m_ValueContinuityIdentities[output], AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid, operation.Index);
            }
        }

        void EvaluateAdditivePose(AnimationPoseGraphNativeOperation operation)
        {
            int output = operation.OutputValueIndex;
            int baseValue = operation.InputValueIndexA;
            int additiveValue = operation.InputValueIndexB;
            if (!TryRequireInputs(operation, baseValue, additiveValue))
                return;
            if (m_ValueAvailability[additiveValue] == AnimationPoseAvailability.NoPose)
            {
                if (!TryCopyValue(baseValue, output, operation.Index))
                    SetInvalid(output, m_ValueContinuityIdentities[baseValue], AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid, operation.Index);
                return;
            }
            if (m_ValueAvailability[baseValue] != AnimationPoseAvailability.Pose)
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

            m_ValueAvailability[output] = AnimationPoseAvailability.Pose;
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
            if (!TryCopyParameters(baseValue, output) ||
                !TryMergeContributions(operation, baseValue, additiveValue, output, true) ||
                !TryResolveFootFeatures(operation, baseValue, additiveValue, output, true))
            {
                SetInvalid(output, m_ValueContinuityIdentities[output], AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid, operation.Index);
            }
        }

        void EvaluatePoseParameterResolve(AnimationPoseGraphNativeOperation operation)
        {
            int output = operation.OutputValueIndex;
            int baseValue = operation.InputValueIndexA;
            int parameterSourceValue = operation.InputValueIndexB;
            if (!TryRequireInputs(operation, baseValue, parameterSourceValue))
                return;
            if (!TryCopyValue(baseValue, output, operation.Index))
            {
                SetInvalid(output, m_ValueContinuityIdentities[baseValue], AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid, operation.Index);
                return;
            }
            if (m_ValueAvailability[parameterSourceValue] == AnimationPoseAvailability.NoPose)
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
            int output = operation.OutputValueIndex;
            int input = operation.InputValueIndexA;
            if (!IsInputReady(input, operation.Index))
            {
                SetInvalid(output, (ulong)operation.Index + 1UL, AnimationPoseNativeInvalidReason.PoseGraphInputIncomplete, operation.Index);
                return;
            }
            if (m_ValueAvailability[input] == AnimationPoseAvailability.NoPose)
            {
                SetInvalid(output, m_ValueContinuityIdentities[input], AnimationPoseNativeInvalidReason.PoseGraphOutputInvalid, operation.Index);
                return;
            }
            if (!TryCopyValue(input, output, operation.Index))
            {
                SetInvalid(output, m_ValueContinuityIdentities[input], AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid, operation.Index);
                return;
            }
            if (m_ValueAvailability[output] == AnimationPoseAvailability.Invalid &&
                m_PoseGraphInvalidReason[0] == AnimationPoseNativeInvalidReason.None)
            {
                RecordGraphInvalid(NormalizeInvalidReason(m_ValueInvalidReasons[output]), operation.Index);
            }
        }

        bool TryRequireInputs(AnimationPoseGraphNativeOperation operation, int inputA, int inputB)
        {
            int output = operation.OutputValueIndex;
            if (!IsInputReady(inputA, operation.Index) || !IsInputReady(inputB, operation.Index))
            {
                SetInvalid(output, (ulong)operation.Index + 1UL, AnimationPoseNativeInvalidReason.PoseGraphInputIncomplete, operation.Index);
                return false;
            }
            AnimationPoseAvailability availabilityA = m_ValueAvailability[inputA];
            AnimationPoseAvailability availabilityB = m_ValueAvailability[inputB];
            if (!IsAvailability(availabilityA) || !IsAvailability(availabilityB))
            {
                SetInvalid(output, (ulong)operation.Index + 1UL, AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid, operation.Index);
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
                for (int bone = 0; bone < m_BoneCount; bone++)
                {
                    m_ValueDenseLocalPoses[PoseOffset(destination) + bone] =
                        m_ValueDenseLocalPoses[PoseOffset(source) + bone];
                    m_ValueDenseVelocities[PoseOffset(destination) + bone] =
                        m_ValueDenseVelocities[PoseOffset(source) + bone];
                }
            }
            for (int parameter = 0; parameter < m_ParameterCount; parameter++)
            {
                m_ValuePoseParameters[ParameterOffset(destination) + parameter] =
                    m_ValuePoseParameters[ParameterOffset(source) + parameter];
                m_ValuePoseParameterAvailability[ParameterOffset(destination) + parameter] =
                    m_ValuePoseParameterAvailability[ParameterOffset(source) + parameter];
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
            if (m_ValueAvailability[value] != AnimationPoseAvailability.Pose)
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
                        source.PhysicalPlayerIndex,
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
                    m_ParameterPolicies[operation.ParameterPolicyOffset + parameter];
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

        bool TryCopyParameters(int source, int output)
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
                        source.PhysicalPlayerIndex,
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
                        current.PhysicalPlayerIndex,
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
            for (int i = 0; i < m_Operations.Length; i++)
            {
                AnimationPoseGraphNativeOperation candidate = m_Operations[i];
                if (candidate.Index < operationIndex && candidate.OutputValueIndex == value)
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
                if (candidate.PhysicalPlayerIndex == source.PhysicalPlayerIndex &&
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
            m_ValueAvailability[value] = AnimationPoseAvailability.Invalid;
            m_ValueContinuityIdentities[value] = 1;
            m_ValueDiscontinuities[value] = default;
            m_ValueInvalidReasons[value] = AnimationPoseNativeInvalidReason.None;
            for (int parameter = 0; parameter < m_ParameterCount; parameter++)
            {
                m_ValuePoseParameters[ParameterOffset(value) + parameter] = m_ParameterDefaults[parameter];
                m_ValuePoseParameterAvailability[ParameterOffset(value) + parameter] = 0;
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
            m_ValueAvailability[value] = AnimationPoseAvailability.Invalid;
            m_ValueContinuityIdentities[value] = RequireIdentity(continuity);
            m_ValueDiscontinuities[value] = default;
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
            operation.BoneMaskOffset < 0 ? 1f : m_DenseBoneMasks[operation.BoneMaskOffset + bone];

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
            return contribution.PhysicalPlayerIndex >= 0 &&
                   kind >= (int)AnimationPoseContributionKind.Live &&
                   kind <= (int)AnimationPoseContributionKind.Stored &&
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

        static bool IsAvailability(AnimationPoseAvailability availability)
        {
            int value = (int)availability;
            return value >= (int)AnimationPoseAvailability.Pose &&
                   value <= (int)AnimationPoseAvailability.Invalid;
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

        static Vector3 ClampMagnitude(Vector3 value, float maximum) =>
            maximum <= 0f ? Vector3.zero : Vector3.ClampMagnitude(value, maximum);

        static Vector3 RotationResidual(Quaternion previous, Quaternion current, float maximumDegrees)
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

        static void RequireValidConfiguration(
            CharacterPoseGraphNativeProgram program,
            PoseInertializationNativeProgram inertializationProgram,
            CharacterPoseGraphNativeBinding binding)
        {
            if (program == null)
                throw new ArgumentNullException(nameof(program));
            program.RequireValid();
            if (inertializationProgram == null || inertializationProgram.BoneCount != program.BoneCount ||
                inertializationProgram.ParameterCount != program.ParameterCount)
                throw new ArgumentException("Pose Inertialization Native Program is invalid.", nameof(inertializationProgram));
            binding.RequireValid();
            AnimationPoseNativeAggregateLayout layout = binding.Layout;
            if (layout.BoneCount != program.BoneCount ||
                layout.ParameterCount != program.ParameterCount ||
                layout.PoseValueCount != program.PoseValueCount ||
                layout.PoseValueContributionStride != program.ContributionStride ||
                layout.OperationCount != program.FrameCacheCount ||
                layout.FrameCacheCount != program.FrameCacheCount ||
                layout.OutputValueIndex != program.OutputValueIndex ||
                program.OutputOperationIndex < 0 ||
                program.OutputOperationIndex >= program.FrameCacheCount ||
                program.OutputNativeOperationIndex < 0 ||
                program.OutputNativeOperationIndex >= program.Operations.Length ||
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
                if (operation.Index < 0 || operation.Index >= program.FrameCacheCount ||
                    operation.FrameCacheIndex != operation.Index ||
                    operation.OutputValueIndex < 0 ||
                    operation.OutputValueIndex >= program.PoseValueCount ||
                    !float.IsFinite(operation.Weight) || operation.Weight < 0f || operation.Weight > 1f)
                {
                    throw new ArgumentException($"Animation Pose Graph Native Job operation #{i} is invalid.", nameof(program));
                }
                bool inputA = operation.InputValueIndexA >= 0 &&
                              operation.InputValueIndexA < operation.OutputValueIndex;
                bool inputB = operation.InputValueIndexB >= 0 &&
                              operation.InputValueIndexB < operation.OutputValueIndex;
                bool valid = operation.Code switch
                {
                    CharacterPoseOperationCode.SelectedPosePlayer or CharacterPoseOperationCode.BlendSpacePlayer or CharacterPoseOperationCode.BlendStack =>
                        operation.InputValueIndexA == -1 && operation.InputValueIndexB == -1 &&
                        operation.PhysicalPlayerIndex >= 0 && operation.PhysicalPlayerIndex < layout.PlayerCount &&
                        IsOutputPolicy(operation.AnimationSelectionAvailabilityPolicy) &&
                        operation.BoneMaskOffset == -1 && operation.AdditiveReferenceOffset == -1 &&
                        operation.ParameterPolicyOffset == -1,
                    CharacterPoseOperationCode.Inertialization =>
                        inputA && operation.InputValueIndexB == -1 &&
                        operation.InertializationIndex >= 0 && operation.InertializationIndex < inertializationProgram.Nodes.Length,
                    CharacterPoseOperationCode.BlendPose =>
                        inputA && inputB && operation.BoneMaskOffset == -1 &&
                        operation.AdditiveReferenceOffset == -1 && operation.ParameterPolicyOffset == -1 &&
                        operation.ParameterIndex < program.ParameterCount,
                    CharacterPoseOperationCode.LayeredBoneBlend =>
                        inputA && inputB && HasSpan(program.DenseBoneMasks, operation.BoneMaskOffset, program.BoneCount) &&
                        operation.AdditiveReferenceOffset == -1 &&
                        operation.ParameterPolicyOffset == -1,
                    CharacterPoseOperationCode.AdditivePose =>
                        inputA && inputB && HasSpan(program.DenseBoneMasks, operation.BoneMaskOffset, program.BoneCount) &&
                        HasSpan(program.AdditiveReferences, operation.AdditiveReferenceOffset, program.BoneCount) &&
                        IsAdditiveReferenceSpace(operation.AdditiveReferenceSpace) &&
                        IsAdditiveScalePolicy(operation.AdditiveScalePolicy) &&
                        operation.ParameterPolicyOffset == -1,
                    CharacterPoseOperationCode.PoseParameterResolve =>
                        inputA && inputB && operation.BoneMaskOffset == -1 && operation.AdditiveReferenceOffset == -1 &&
                        HasSpan(program.ParameterPolicies, operation.ParameterPolicyOffset, program.ParameterCount),
                    CharacterPoseOperationCode.ModifyBone =>
                        inputA && operation.InputValueIndexB == -1 &&
                        operation.ModifyBoneIndex >= 0 && operation.ModifyBoneIndex < program.ModifyBones.Length,
                    CharacterPoseOperationCode.FootPlacement =>
                        inputA && operation.InputValueIndexB == -1 && operation.FootPlacementIndex >= 0,
                    CharacterPoseOperationCode.OutputPose =>
                        inputA && operation.InputValueIndexB == -1 && operation.BoneMaskOffset == -1 &&
                        operation.AdditiveReferenceOffset == -1 && operation.ParameterPolicyOffset == -1,
                    _ => false
                };
                if (!valid)
                    throw new ArgumentException($"Animation Pose Graph Native Job operation #{i} layout is invalid.", nameof(program));
                if (operation.Code == CharacterPoseOperationCode.OutputPose)
                {
                    outputCount++;
                    if (i != program.OutputNativeOperationIndex ||
                        operation.Index != program.OutputOperationIndex ||
                        operation.OutputValueIndex != program.OutputValueIndex)
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

        static bool IsOutputPolicy(AnimationSelectionAvailabilityPolicy value) =>
            (int)value >= (int)AnimationSelectionAvailabilityPolicy.RequireSelection &&
            (int)value <= (int)AnimationSelectionAvailabilityPolicy.AllowEmpty;

        static bool IsAdditiveReferenceSpace(AdditiveReferenceSpace value) =>
            (int)value >= (int)AdditiveReferenceSpace.Local &&
            (int)value <= (int)AdditiveReferenceSpace.Mesh;

        static bool IsAdditiveScalePolicy(AdditiveScalePolicy value) =>
            (int)value >= (int)AdditiveScalePolicy.Multiply &&
            (int)value <= (int)AdditiveScalePolicy.Ignore;
    }
}
