using System;
using ThirdPersonCharacter.Pipeline.Animation.BlendStack;
using ThirdPersonCharacter.Pipeline.Animation.Presentation;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonSimulation;
using Unity.Collections;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal sealed class CharacterPosePlayerOperationModule
    {
        readonly CharacterPoseManagedValuePage m_Values;
        readonly CharacterPoseInertializationOperationModule
            m_Inertialization;
        NativeArray<AnimationPlayerPoseNativeRange> m_SlotRanges;
        NativeArray<AnimationLocalBonePose> m_SlotDenseLocalPoses;
        NativeArray<AnimationBlendBoneVelocity> m_SlotDenseVelocities;
        NativeArray<float> m_SlotPoseParameters;
        NativeArray<byte> m_SlotPoseParameterAvailability;
        NativeArray<AnimationPrimitivePoseContribution> m_SlotContributions;
        NativeArray<float> m_SlotDenseContributionWeights;
        NativeArray<int> m_SlotContributionCounts;
        NativeArray<float> m_SlotOutputWeights;
        NativeArray<AnimationFootFeatureSample> m_SlotLeftFootFeatures;
        NativeArray<AnimationFootFeatureSample> m_SlotRightFootFeatures;
        NativeArray<byte> m_SlotHasFootFeatures;
        NativeArray<AnimationPoseAvailability> m_SlotAvailability;
        NativeArray<ulong> m_SlotContinuityIdentities;
        NativeArray<PoseDiscontinuityNative> m_SlotDiscontinuities;
        NativeArray<AnimationPoseNativeInvalidReason> m_SlotInvalidReasons;
        NativeArray<ulong> m_SlotCompletedAt;
        NativeArray<CharacterAnimationSlotNativeControl> m_AnimationSlotControls;

        internal CharacterPosePlayerOperationModule(
            CharacterPoseManagedValuePage values,
            CharacterPoseInertializationOperationModule inertialization)
        {
            m_Values = values ??
                throw new ArgumentNullException(nameof(values));
            m_Inertialization = inertialization ??
                throw new ArgumentNullException(nameof(inertialization));
        }

        internal void BindFrame(
            CharacterPoseGraphNativeBinding binding,
            NativeArray<CharacterAnimationSlotNativeControl> controls)
        {
            binding.RequireValid();
            if (!controls.IsCreated)
                throw new ArgumentException("Animation Slot controls are invalid.");
            m_SlotRanges = binding.SlotRanges;
            m_SlotDenseLocalPoses = binding.SlotDenseLocalPoses;
            m_SlotDenseVelocities = binding.SlotDenseVelocities;
            m_SlotPoseParameters = binding.SlotPoseParameters;
            m_SlotPoseParameterAvailability =
                binding.SlotPoseParameterAvailability;
            m_SlotContributions = binding.SlotContributions;
            m_SlotDenseContributionWeights =
                binding.SlotDenseContributionWeights;
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
            m_AnimationSlotControls = controls;
        }

        internal void EvaluatePlayerInput(
            in CharacterPoseNativeOperationHeader header,
            in CharacterPoseNativePlayerOperation operation) =>
            EvaluatePlayerInput(
                in header,
                operation.OutputPoseValueIndex,
                operation.PlayerIndex,
                operation.SelectionAvailability);

        internal void EvaluatePlayerInput(
            in CharacterPoseNativeOperationHeader header,
            in CharacterPoseNativeBlendOperation operation) =>
            EvaluatePlayerInput(
                in header,
                operation.OutputPoseValueIndex,
                operation.PlayerIndex,
                operation.SelectionAvailability);

        void EvaluatePlayerInput(
            in CharacterPoseNativeOperationHeader header,
            int output,
            int slotIndex,
            AnimationSelectionAvailabilityPolicy selectionAvailability)
        {
            ulong continuity = slotIndex >= 0 && slotIndex < m_SlotRanges.Length
                ? m_SlotContinuityIdentities[slotIndex]
                : 0UL;
            if (slotIndex < 0 || slotIndex >= m_SlotRanges.Length ||
                m_SlotCompletedAt[slotIndex] != m_Values.m_CompletionIdentity)
            {
                m_Values.SetInvalid(
                    output,
                    continuity,
                    AnimationPoseNativeInvalidReason.PoseGraphInputIncomplete,
                    header.Index);
                return;
            }

            AnimationPoseAvailability availability = m_SlotAvailability[slotIndex];
            AnimationPoseNativeInvalidReason slotReason = m_SlotInvalidReasons[slotIndex];
            PoseDiscontinuityNative discontinuity = m_SlotDiscontinuities[slotIndex];
            if (availability == AnimationPoseAvailability.Invalid)
            {
                m_Values.SetInvalid(output, continuity, CharacterPosePureMath.NormalizeInvalidReason(slotReason), header.Index);
                return;
            }
            if (!CharacterPosePureMath.IsAvailability(availability) || slotReason != AnimationPoseNativeInvalidReason.None || continuity == 0 ||
                !discontinuity.IsValid || discontinuity.IsPresent && discontinuity.CompletionIdentity != m_Values.m_CompletionIdentity)
            {
                m_Values.SetInvalid(
                    output,
                    continuity,
                    AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid,
                    header.Index);
                return;
            }
            if (availability == AnimationPoseAvailability.NoPose &&
                selectionAvailability == AnimationSelectionAvailabilityPolicy.RequireSelection)
            {
                m_Values.SetInvalid(output, continuity, AnimationPoseNativeInvalidReason.RequiredPoseMissing, header.Index);
                return;
            }

            AnimationPlayerPoseNativeRange range = m_SlotRanges[slotIndex];
            int contributionCount = m_SlotContributionCounts[slotIndex];
            float outputWeight = m_SlotOutputWeights[slotIndex];
            byte hasFootFeatures = m_SlotHasFootFeatures[slotIndex];
            if (range.PhysicalPlayerIndex != slotIndex || contributionCount < 0 ||
                contributionCount > range.ContributionCapacity || contributionCount > m_Values.m_ContributionStride ||
                !CharacterPosePureMath.IsWeight(outputWeight) || hasFootFeatures > 1)
            {
                m_Values.SetInvalid(output, continuity, AnimationPoseNativeInvalidReason.SlotPlanInvalid, header.Index);
                return;
            }

            for (int parameter = 0; parameter < m_Values.m_ParameterCount; parameter++)
            {
                float value = m_SlotPoseParameters[range.ParameterOffset + parameter];
                byte parameterAvailable = m_SlotPoseParameterAvailability[range.ParameterOffset + parameter];
                if (!float.IsFinite(value) || parameterAvailable > 1)
                {
                    m_Values.SetInvalid(output, continuity, AnimationPoseNativeInvalidReason.SlotParameterInvalid, header.Index);
                    return;
                }
                m_Values.m_ValuePoseParameters[m_Values.ParameterOffset(output) + parameter] = value;
                m_Values.m_ValuePoseParameterAvailability[m_Values.ParameterOffset(output) + parameter] = parameterAvailable;
            }

            if (availability == AnimationPoseAvailability.NoPose)
            {
                if (contributionCount != 0 || outputWeight != 0f || hasFootFeatures != 0)
                {
                    m_Values.SetInvalid(output, continuity, AnimationPoseNativeInvalidReason.SlotPlanInvalid, header.Index);
                    return;
                }
                m_Values.m_ValueAvailability[output] = AnimationPoseAvailability.NoPose;
                m_Values.m_ValueContinuityIdentities[output] = continuity;
                m_Values.m_ValueInvalidReasons[output] = AnimationPoseNativeInvalidReason.None;
                return;
            }

            if (contributionCount <= 0)
            {
                m_Values.SetInvalid(output, continuity, AnimationPoseNativeInvalidReason.SlotPlanInvalid, header.Index);
                return;
            }
            for (int bone = 0; bone < m_Values.m_BoneCount; bone++)
            {
                AnimationLocalBonePose pose = m_SlotDenseLocalPoses[range.PoseOffset + bone];
                AnimationBlendBoneVelocity velocity = m_SlotDenseVelocities[range.VelocityOffset + bone];
                if (!pose.IsValid || !velocity.IsValid)
                {
                    m_Values.SetInvalid(output, continuity, AnimationPoseNativeInvalidReason.SlotPoseInvalid, header.Index);
                    return;
                }
                m_Values.m_ValueDenseLocalPoses[m_Values.PoseOffset(output) + bone] = pose;
                m_Values.m_ValueDenseVelocities[m_Values.PoseOffset(output) + bone] = velocity;
            }

            if (hasFootFeatures == 1 &&
                (!CharacterPosePureMath.IsValidFootFeature(m_SlotLeftFootFeatures[slotIndex]) ||
                 !CharacterPosePureMath.IsValidFootFeature(m_SlotRightFootFeatures[slotIndex])))
            {
                m_Values.SetInvalid(output, continuity, AnimationPoseNativeInvalidReason.SlotFootFeatureInvalid, header.Index);
                return;
            }

            int destinationContributionOffset = m_Values.ContributionOffset(output);
            int destinationDenseOffset = m_Values.ContributionBoneOffset(output);
            for (int contribution = 0; contribution < contributionCount; contribution++)
            {
                AnimationPrimitivePoseContribution primitive =
                    m_SlotContributions[range.ContributionOffset + contribution];
                if (!CharacterPosePureMath.IsValidPrimitiveContribution(primitive) || primitive.PhysicalPlayerIndex != slotIndex)
                {
                    m_Values.SetInvalid(output, continuity, AnimationPoseNativeInvalidReason.SlotContributionInvalid, header.Index);
                    return;
                }
                m_Values.m_ValueContributions[destinationContributionOffset + contribution] = primitive;
                for (int bone = 0; bone < m_Values.m_BoneCount; bone++)
                {
                    float weight = m_SlotDenseContributionWeights[
                        range.DenseContributionWeightOffset + contribution * m_Values.m_BoneCount + bone];
                    if (!CharacterPosePureMath.IsWeight(weight))
                    {
                        m_Values.SetInvalid(output, continuity, AnimationPoseNativeInvalidReason.SlotContributionInvalid, header.Index);
                        return;
                    }
                    m_Values.m_ValueDenseContributionWeights[
                        destinationDenseOffset + contribution * m_Values.m_BoneCount + bone] = weight;
                }
            }

            m_Values.m_ValueContributionCounts[output] = contributionCount;
            m_Values.m_ValueOutputWeights[output] = outputWeight;
            m_Values.m_ValueLeftFootFeatures[output] = hasFootFeatures == 1
                ? m_SlotLeftFootFeatures[slotIndex]
                : default;
            m_Values.m_ValueRightFootFeatures[output] = hasFootFeatures == 1
                ? m_SlotRightFootFeatures[slotIndex]
                : default;
            m_Values.m_ValueHasFootFeatures[output] = hasFootFeatures;
            m_Values.m_ValueAvailability[output] = AnimationPoseAvailability.Pose;
            m_Values.m_ValueContinuityIdentities[output] = continuity;
            m_Values.m_ValueDiscontinuities[output] = m_SlotDiscontinuities[slotIndex];
            m_Values.m_ValueInvalidReasons[output] = AnimationPoseNativeInvalidReason.None;
        }

        internal void EvaluateAnimationSlot(
            in CharacterPoseNativeOperationHeader header,
            in CharacterPoseNativeAnimationSlotOperation operation,
            float deltaSeconds)
        {
            int source = operation.InputPoseValueIndex;
            int output = operation.OutputPoseValueIndex;
            if (!m_Values.IsInputReady(source, header.Index) ||
                m_Values.m_ValueAvailability[source] != AnimationPoseAvailability.Pose)
            {
                m_Values.SetInvalid(
                    output,
                    (ulong)header.Index + 1UL,
                    AnimationPoseNativeInvalidReason.PoseGraphInputIncomplete,
                    header.Index);
                return;
            }

            EvaluatePlayerInput(
                in header,
                output,
                operation.PlayerIndex,
                operation.SelectionAvailability);
            if (m_Values.m_ValueAvailability[output] == AnimationPoseAvailability.Invalid)
                return;
            if (m_Values.m_ValueAvailability[output] == AnimationPoseAvailability.NoPose)
            {
                if (!m_Values.TryCopyValue(source, output, header.Index))
                {
                    m_Values.SetInvalid(
                        output,
                        m_Values.m_ValueContinuityIdentities[source],
                        AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid,
                        header.Index);
                }
                else
                {
                    EvaluateAnimationSlotInertialization(
                        in header,
                        in operation,
                        deltaSeconds);
                }
                return;
            }

            int actionContributionCount = m_Values.m_ValueContributionCounts[output];
            int actionContributionStart = m_Values.m_ContributionStride - actionContributionCount;
            if (actionContributionCount <= 0 || actionContributionStart < 0)
            {
                m_Values.SetInvalid(
                    output,
                    m_Values.m_ValueContinuityIdentities[output],
                    AnimationPoseNativeInvalidReason.SlotContributionInvalid,
                    header.Index);
                return;
            }
            for (int contribution = actionContributionCount - 1; contribution >= 0; contribution--)
            {
                int target = actionContributionStart + contribution;
                m_Values.m_ValueContributions[m_Values.ContributionOffset(output) + target] =
                    m_Values.m_ValueContributions[m_Values.ContributionOffset(output) + contribution];
                for (int bone = 0; bone < m_Values.m_BoneCount; bone++)
                {
                    m_Values.SetContributionBoneWeight(
                        output,
                        target,
                        bone,
                        m_Values.GetContributionBoneWeight(output, contribution, bone));
                }
            }

            ulong actionContinuity = m_Values.m_ValueContinuityIdentities[output];
            float actionOutputWeight = m_Values.m_ValueOutputWeights[output];
            byte actionHasFootFeatures = m_Values.m_ValueHasFootFeatures[output];
            AnimationFootFeatureSample actionLeftFoot = m_Values.m_ValueLeftFootFeatures[output];
            AnimationFootFeatureSample actionRightFoot = m_Values.m_ValueRightFootFeatures[output];
            if (!m_Values.TryGetBoneOutputWeight(output, m_Values.m_LeftFootBoneIndex, out float actionLeftFootWeight) ||
                !m_Values.TryGetBoneOutputWeight(output, m_Values.m_RightFootBoneIndex, out float actionRightFootWeight))
            {
                m_Values.SetInvalid(
                    output,
                    actionContinuity,
                    AnimationPoseNativeInvalidReason.SlotContributionInvalid,
                    header.Index);
                return;
            }
            for (int bone = 0; bone < m_Values.m_BoneCount; bone++)
            {
                if (!m_Values.TryGetBoneOutputWeight(output, bone, out float actionBoneWeight))
                {
                    m_Values.SetInvalid(
                        output,
                        actionContinuity,
                        AnimationPoseNativeInvalidReason.SlotContributionInvalid,
                        header.Index);
                    return;
                }
                AnimationLocalBonePose actionPose = m_Values.m_ValueDenseLocalPoses[m_Values.PoseOffset(output) + bone];
                AnimationBlendBoneVelocity actionVelocity = m_Values.m_ValueDenseVelocities[m_Values.PoseOffset(output) + bone];
                AnimationBlendBoneVelocity sourceVelocity = m_Values.m_ValueDenseVelocities[m_Values.PoseOffset(source) + bone];
                if (!CharacterPosePureMath.TryBlendPose(
                        m_Values.m_ValueDenseLocalPoses[m_Values.PoseOffset(source) + bone],
                        actionPose,
                        actionBoneWeight,
                        out AnimationLocalBonePose pose))
                {
                    m_Values.SetInvalid(
                        output,
                        actionContinuity,
                        AnimationPoseNativeInvalidReason.SlotPoseInvalid,
                        header.Index);
                    return;
                }
                m_Values.m_ValueDenseLocalPoses[m_Values.PoseOffset(output) + bone] = pose;
                m_Values.m_ValueDenseVelocities[m_Values.PoseOffset(output) + bone] = new AnimationBlendBoneVelocity(
                    Vector3.LerpUnclamped(sourceVelocity.Linear, actionVelocity.Linear, actionBoneWeight),
                    Vector3.LerpUnclamped(sourceVelocity.Angular, actionVelocity.Angular, actionBoneWeight),
                    Vector3.LerpUnclamped(sourceVelocity.Scale, actionVelocity.Scale, actionBoneWeight));
            }

            m_Values.m_ValueContributionCounts[output] = 0;
            m_Values.m_ValueOutputWeights[output] = actionOutputWeight;
            for (int contribution = 0; contribution < actionContributionCount; contribution++)
            {
                if (!m_Values.TryAddUnmaskedContribution(
                        header.Weight,
                        output,
                        actionContributionStart + contribution,
                        output,
                        output,
                        true,
                        false))
                {
                    m_Values.SetInvalid(
                        output,
                        actionContinuity,
                        AnimationPoseNativeInvalidReason.SlotContributionInvalid,
                        header.Index);
                    return;
                }
            }
            for (int contribution = 0; contribution < m_Values.m_ValueContributionCounts[source]; contribution++)
            {
                if (!TryAddAnimationSlotBaseContribution(
                        source,
                        contribution,
                        output,
                        actionContributionStart,
                        actionContributionCount,
                        actionOutputWeight,
                        actionLeftFootWeight,
                        actionRightFootWeight))
                {
                    m_Values.SetInvalid(
                        output,
                        actionContinuity,
                        AnimationPoseNativeInvalidReason.SlotContributionInvalid,
                        header.Index);
                    return;
                }
            }

            m_Values.m_ValueAvailability[output] = AnimationPoseAvailability.Pose;
            m_Values.m_ValueOutputWeights[output] = CharacterPosePureMath.UnionWeight(
                m_Values.m_ValueOutputWeights[source],
                actionOutputWeight);
            m_Values.m_ValueContinuityIdentities[output] = CharacterPosePureMath.CombineContinuity(
                m_Values.m_ValueContinuityIdentities[source],
                actionContinuity,
                header.Index);
            m_Values.m_ValueInvalidReasons[output] = AnimationPoseNativeInvalidReason.None;
            if (!m_Values.TryCopyParameters(source, output) ||
                !TryResolveAnimationSlotFootFeatures(
                    source,
                    output,
                    actionHasFootFeatures,
                    actionLeftFoot,
                    actionRightFoot,
                    actionLeftFootWeight,
                    actionRightFootWeight))
            {
                m_Values.SetInvalid(
                    output,
                    m_Values.m_ValueContinuityIdentities[output],
                    AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid,
                    header.Index);
            }
            if (m_Values.m_ValueAvailability[output] == AnimationPoseAvailability.Pose)
                EvaluateAnimationSlotInertialization(
                    in header,
                    in operation,
                    deltaSeconds);
        }

        void EvaluateAnimationSlotInertialization(
            in CharacterPoseNativeOperationHeader header,
            in CharacterPoseNativeAnimationSlotOperation operation,
            float deltaSeconds)
        {
            int output = operation.OutputPoseValueIndex;
            if ((uint)operation.AnimationSlotIndex >= (uint)m_AnimationSlotControls.Length ||
                !float.IsFinite(deltaSeconds) || deltaSeconds < 0f)
            {
                m_Values.SetInvalid(
                    output,
                    m_Values.m_ValueContinuityIdentities[output],
                    AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid,
                    header.Index);
                return;
            }
            int stateIndex = m_Inertialization.AnimationSlotNodeOffset +
                             operation.AnimationSlotIndex;
            if ((uint)stateIndex >= (uint)m_Inertialization.StateCount)
            {
                m_Values.SetInvalid(
                    output,
                    m_Values.m_ValueContinuityIdentities[output],
                    AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid,
                    header.Index);
                return;
            }
            CharacterAnimationSlotNativeControl control =
                m_AnimationSlotControls[operation.AnimationSlotIndex];
            PoseInertializationNativeState state = m_Inertialization.CommittedInertialState(stateIndex);
            m_Inertialization.PrepareInertialNode(stateIndex, in state);
            if (control.Generation == 0 || control.Generation < state.LastEventIdentity)
            {
                m_Values.SetInvalid(
                    output,
                    m_Values.m_ValueContinuityIdentities[output],
                    AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid,
                    header.Index);
                return;
            }
            if (control.Generation > state.LastEventIdentity)
            {
                state.LastEventIdentity = control.Generation;
                state.Active = 0;
                state.ElapsedSeconds = 0f;
                if (control.Mode == CharacterAnimationSlotNativeTransitionMode.Inertialization)
                {
                    int ruleIndex = m_Inertialization.RequireInertialRule(
                        stateIndex,
                        control.SourceProducerIndex,
                        control.TargetProducerIndex);
                    if (ruleIndex < 0 ||
                        m_Inertialization.Rule(ruleIndex).Mode !=
                        PoseInertializationMode.Inertialize)
                    {
                        state.RuntimeState = PoseInertializationRuntimeState.Invalid;
                        m_Inertialization.SetState(stateIndex, in state);
                        m_Values.SetInvalid(
                            output,
                            m_Values.m_ValueContinuityIdentities[output],
                            AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid,
                            header.Index);
                        return;
                    }
                    if (state.HasHistory != 0)
                    {
                        m_Inertialization.CaptureInertialResidual(stateIndex, output, ruleIndex, ref state);
                    }
                    else
                    {
                        state.ActiveRuleIndex = ruleIndex;
                        state.RuntimeState = PoseInertializationRuntimeState.Anchor;
                    }
                }
                else
                {
                    state.RuntimeState =
                        control.Mode == CharacterAnimationSlotNativeTransitionMode.StandardBlend
                            ? PoseInertializationRuntimeState.HardCut
                            : PoseInertializationRuntimeState.Anchor;
                }
            }
            else if (state.Active != 0)
            {
                state.RuntimeState = PoseInertializationRuntimeState.Continue;
            }

            if (state.Active != 0)
            {
                PoseInertializationNativeRule rule =
                    m_Inertialization.Rule(state.ActiveRuleIndex);
                bool anyActive = false;
                for (int bone = 0; bone < m_Values.m_BoneCount; bone++)
                {
                    int residualIndex = stateIndex * m_Values.m_BoneCount + bone;
                    float duration = state.ActiveDurationSeconds *
                                     m_Inertialization.DenseProfileWeight(
                                         in rule,
                                         bone);
                    m_Inertialization.EvaluateInertialEnvelope(
                        rule,
                        state.ElapsedSeconds,
                        duration,
                        out _,
                        out float weight,
                        out float derivative);
                    anyActive |= state.ElapsedSeconds < duration;
                    AnimationLocalBonePose target = m_Values.m_ValueDenseLocalPoses[m_Values.PoseOffset(output) + bone];
                    AnimationBlendBoneVelocity targetVelocity = m_Values.m_ValueDenseVelocities[m_Values.PoseOffset(output) + bone];
                    Vector3 positionBase =
                        m_Inertialization.PositionResidual(residualIndex) +
                        state.ElapsedSeconds *
                        m_Inertialization.LinearVelocityResidual(residualIndex);
                    Vector3 rotationBase =
                        m_Inertialization.RotationResidual(residualIndex) +
                        state.ElapsedSeconds *
                        m_Inertialization.AngularVelocityResidual(residualIndex);
                    Vector3 scaleBase =
                        m_Inertialization.ScaleResidual(residualIndex) +
                        state.ElapsedSeconds *
                        m_Inertialization.ScaleVelocityResidual(residualIndex);
                    Vector3 linear = targetVelocity.Linear + derivative * positionBase +
                                     weight * m_Inertialization
                                         .LinearVelocityResidual(residualIndex);
                    Vector3 angular = targetVelocity.Angular + derivative * rotationBase +
                                      weight * m_Inertialization
                                          .AngularVelocityResidual(residualIndex);
                    Vector3 scaleVelocity = targetVelocity.Scale + derivative * scaleBase +
                                            weight * m_Inertialization
                                                .ScaleVelocityResidual(residualIndex);
                    if (!CharacterPosePureMath.IsFinite(linear) || !CharacterPosePureMath.IsFinite(angular) || !CharacterPosePureMath.IsFinite(scaleVelocity))
                    {
                        state.RuntimeState = PoseInertializationRuntimeState.Invalid;
                        m_Inertialization.SetState(stateIndex, in state);
                        m_Values.SetInvalid(
                            output,
                            m_Values.m_ValueContinuityIdentities[output],
                            AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid,
                            header.Index);
                        return;
                    }
                    m_Values.m_ValueDenseLocalPoses[m_Values.PoseOffset(output) + bone] = new AnimationLocalBonePose(
                        target.Position + weight * positionBase,
                        AnimationPoseMath.QuaternionExp(weight * rotationBase) * target.Rotation,
                        target.Scale + weight * scaleBase);
                    m_Values.m_ValueDenseVelocities[m_Values.PoseOffset(output) + bone] =
                        new AnimationBlendBoneVelocity(linear, angular, scaleVelocity);
                }
                m_Inertialization.ApplyInertialParameters(stateIndex, output, output, rule, state.ActiveDurationSeconds, state.ElapsedSeconds);
                m_Inertialization.ApplyInertialFootFeatures(stateIndex, output, output, rule, state.ActiveDurationSeconds, state.ElapsedSeconds);
                state.ElapsedSeconds += deltaSeconds;
                state.LastDeltaSeconds = deltaSeconds;
                if (!anyActive)
                {
                    state.Active = 0;
                    state.RuntimeState = PoseInertializationRuntimeState.Complete;
                }
            }
            m_Inertialization.CommitInertialHistory(stateIndex, output, ref state);
            if (state.RuntimeState == 0)
                state.RuntimeState = PoseInertializationRuntimeState.Anchor;
            state.OutputCompletionIdentity = m_Values.m_CompletionIdentity;
            m_Inertialization.SetState(stateIndex, in state);
        }

        bool TryAddAnimationSlotBaseContribution(
            int sourceValue,
            int sourceIndex,
            int output,
            int actionContributionStart,
            int actionContributionCount,
            float actionOutputWeight,
            float actionLeftFootWeight,
            float actionRightFootWeight)
        {
            AnimationPrimitivePoseContribution source =
                m_Values.m_ValueContributions[m_Values.ContributionOffset(sourceValue) + sourceIndex];
            if (!CharacterPosePureMath.IsValidPrimitiveContribution(source))
                return false;
            float scalarWeight = source.Weight * Mathf.Clamp01(1f - actionOutputWeight);
            float leftWeight = source.LeftFootWeight * Mathf.Clamp01(1f - actionLeftFootWeight);
            float rightWeight = source.RightFootWeight * Mathf.Clamp01(1f - actionRightFootWeight);
            if (!CharacterPosePureMath.IsWeight(scalarWeight) || !CharacterPosePureMath.IsWeight(leftWeight) || !CharacterPosePureMath.IsWeight(rightWeight))
                return false;

            int targetIndex = m_Values.FindContribution(output, source);
            if (targetIndex < 0)
            {
                targetIndex = m_Values.m_ValueContributionCounts[output];
                if (targetIndex >= m_Values.m_ContributionStride)
                    return false;
                m_Values.m_ValueContributionCounts[output] = targetIndex + 1;
                m_Values.ClearContributionWeights(output, targetIndex);
                m_Values.m_ValueContributions[m_Values.ContributionOffset(output) + targetIndex] =
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
                    m_Values.m_ValueContributions[m_Values.ContributionOffset(output) + targetIndex];
                m_Values.m_ValueContributions[m_Values.ContributionOffset(output) + targetIndex] =
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

            for (int bone = 0; bone < m_Values.m_BoneCount; bone++)
            {
                float actionWeight = 0f;
                for (int action = 0; action < actionContributionCount; action++)
                {
                    actionWeight += m_Values.GetContributionBoneWeight(
                        output,
                        actionContributionStart + action,
                        bone);
                }
                if (!float.IsFinite(actionWeight))
                    return false;
                float weight = m_Values.GetContributionBoneWeight(sourceValue, sourceIndex, bone) *
                               Mathf.Clamp01(1f - actionWeight);
                float combined = Mathf.Clamp01(
                    m_Values.GetContributionBoneWeight(output, targetIndex, bone) + weight);
                if (!CharacterPosePureMath.IsWeight(combined))
                    return false;
                m_Values.SetContributionBoneWeight(output, targetIndex, bone, combined);
            }
            return true;
        }

        bool TryResolveAnimationSlotFootFeatures(
            int source,
            int output,
            byte actionHasFootFeatures,
            AnimationFootFeatureSample actionLeftFoot,
            AnimationFootFeatureSample actionRightFoot,
            float actionLeftFootWeight,
            float actionRightFootWeight)
        {
            bool hasSource = m_Values.m_ValueHasFootFeatures[source] == 1;
            bool hasAction = actionHasFootFeatures == 1;
            if (!hasSource && !hasAction)
            {
                m_Values.m_ValueHasFootFeatures[output] = 0;
                m_Values.m_ValueLeftFootFeatures[output] = default;
                m_Values.m_ValueRightFootFeatures[output] = default;
                return true;
            }
            if (!CharacterPosePureMath.TryResolveFeature(
                    hasSource,
                    m_Values.m_ValueLeftFootFeatures[source],
                    hasAction,
                    actionLeftFoot,
                    actionLeftFootWeight,
                    hasAction && actionLeftFootWeight > 0f,
                    out AnimationFootFeatureSample left) ||
                !CharacterPosePureMath.TryResolveFeature(
                    hasSource,
                    m_Values.m_ValueRightFootFeatures[source],
                    hasAction,
                    actionRightFoot,
                    actionRightFootWeight,
                    hasAction && actionRightFootWeight > 0f,
                    out AnimationFootFeatureSample right))
            {
                return false;
            }
            m_Values.m_ValueLeftFootFeatures[output] = left;
            m_Values.m_ValueRightFootFeatures[output] = right;
            m_Values.m_ValueHasFootFeatures[output] = left.IsValid && right.IsValid ? (byte)1 : (byte)0;
            return true;
        }

    }
}
