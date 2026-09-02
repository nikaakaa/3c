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
        readonly CharacterPoseExecutionContext m_Context;
        readonly CharacterPoseInertializationOperationModule
            m_Inertialization;

        internal CharacterPosePlayerOperationModule(
            CharacterPoseExecutionContext context,
            CharacterPoseInertializationOperationModule inertialization)
        {
            m_Context = context ??
                throw new ArgumentNullException(nameof(context));
            m_Inertialization = inertialization ??
                throw new ArgumentNullException(nameof(inertialization));
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
            ulong continuity = slotIndex >= 0 && slotIndex < m_Context.m_PlayerCount
                ? m_Context.m_SlotContinuityIdentities[slotIndex]
                : 0UL;
            if (slotIndex < 0 || slotIndex >= m_Context.m_PlayerCount ||
                m_Context.m_SlotCompletedAt[slotIndex] != m_Context.m_CompletionIdentity)
            {
                m_Context.SetInvalid(
                    output,
                    continuity,
                    AnimationPoseNativeInvalidReason.PoseGraphInputIncomplete,
                    header.Index);
                return;
            }

            AnimationPoseAvailability availability = m_Context.m_SlotAvailability[slotIndex];
            AnimationPoseNativeInvalidReason slotReason = m_Context.m_SlotInvalidReasons[slotIndex];
            PoseDiscontinuityNative discontinuity = m_Context.m_SlotDiscontinuities[slotIndex];
            if (availability == AnimationPoseAvailability.Invalid)
            {
                m_Context.SetInvalid(output, continuity, CharacterPoseExecutionContext.NormalizeInvalidReason(slotReason), header.Index);
                return;
            }
            if (!CharacterPoseExecutionContext.IsAvailability(availability) || slotReason != AnimationPoseNativeInvalidReason.None || continuity == 0 ||
                !discontinuity.IsValid || discontinuity.IsPresent && discontinuity.CompletionIdentity != m_Context.m_CompletionIdentity)
            {
                m_Context.SetInvalid(
                    output,
                    continuity,
                    AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid,
                    header.Index);
                return;
            }
            if (availability == AnimationPoseAvailability.NoPose &&
                selectionAvailability == AnimationSelectionAvailabilityPolicy.RequireSelection)
            {
                m_Context.SetInvalid(output, continuity, AnimationPoseNativeInvalidReason.RequiredPoseMissing, header.Index);
                return;
            }

            AnimationPlayerPoseNativeRange range = m_Context.m_SlotRanges[slotIndex];
            int contributionCount = m_Context.m_SlotContributionCounts[slotIndex];
            float outputWeight = m_Context.m_SlotOutputWeights[slotIndex];
            byte hasFootFeatures = m_Context.m_SlotHasFootFeatures[slotIndex];
            if (range.PhysicalPlayerIndex != slotIndex || contributionCount < 0 ||
                contributionCount > range.ContributionCapacity || contributionCount > m_Context.m_ContributionStride ||
                !CharacterPoseExecutionContext.IsWeight(outputWeight) || hasFootFeatures > 1)
            {
                m_Context.SetInvalid(output, continuity, AnimationPoseNativeInvalidReason.SlotPlanInvalid, header.Index);
                return;
            }

            for (int parameter = 0; parameter < m_Context.m_ParameterCount; parameter++)
            {
                float value = m_Context.m_SlotPoseParameters[range.ParameterOffset + parameter];
                byte parameterAvailable = m_Context.m_SlotPoseParameterAvailability[range.ParameterOffset + parameter];
                if (!float.IsFinite(value) || parameterAvailable > 1)
                {
                    m_Context.SetInvalid(output, continuity, AnimationPoseNativeInvalidReason.SlotParameterInvalid, header.Index);
                    return;
                }
                m_Context.m_ValuePoseParameters[m_Context.ParameterOffset(output) + parameter] = value;
                m_Context.m_ValuePoseParameterAvailability[m_Context.ParameterOffset(output) + parameter] = parameterAvailable;
            }

            if (availability == AnimationPoseAvailability.NoPose)
            {
                if (contributionCount != 0 || outputWeight != 0f || hasFootFeatures != 0)
                {
                    m_Context.SetInvalid(output, continuity, AnimationPoseNativeInvalidReason.SlotPlanInvalid, header.Index);
                    return;
                }
                m_Context.m_ValueAvailability[output] = AnimationPoseAvailability.NoPose;
                m_Context.m_ValueContinuityIdentities[output] = continuity;
                m_Context.m_ValueInvalidReasons[output] = AnimationPoseNativeInvalidReason.None;
                return;
            }

            if (contributionCount <= 0)
            {
                m_Context.SetInvalid(output, continuity, AnimationPoseNativeInvalidReason.SlotPlanInvalid, header.Index);
                return;
            }
            for (int bone = 0; bone < m_Context.m_BoneCount; bone++)
            {
                AnimationLocalBonePose pose = m_Context.m_SlotDenseLocalPoses[range.PoseOffset + bone];
                AnimationBlendBoneVelocity velocity = m_Context.m_SlotDenseVelocities[range.VelocityOffset + bone];
                if (!pose.IsValid || !velocity.IsValid)
                {
                    m_Context.SetInvalid(output, continuity, AnimationPoseNativeInvalidReason.SlotPoseInvalid, header.Index);
                    return;
                }
                m_Context.m_ValueDenseLocalPoses[m_Context.PoseOffset(output) + bone] = pose;
                m_Context.m_ValueDenseVelocities[m_Context.PoseOffset(output) + bone] = velocity;
            }

            if (hasFootFeatures == 1 &&
                (!CharacterPoseExecutionContext.IsValidFootFeature(m_Context.m_SlotLeftFootFeatures[slotIndex]) ||
                 !CharacterPoseExecutionContext.IsValidFootFeature(m_Context.m_SlotRightFootFeatures[slotIndex])))
            {
                m_Context.SetInvalid(output, continuity, AnimationPoseNativeInvalidReason.SlotFootFeatureInvalid, header.Index);
                return;
            }

            int destinationContributionOffset = m_Context.ContributionOffset(output);
            int destinationDenseOffset = m_Context.ContributionBoneOffset(output);
            for (int contribution = 0; contribution < contributionCount; contribution++)
            {
                AnimationPrimitivePoseContribution primitive =
                    m_Context.m_SlotContributions[range.ContributionOffset + contribution];
                if (!CharacterPoseExecutionContext.IsValidPrimitiveContribution(primitive) || primitive.PhysicalPlayerIndex != slotIndex)
                {
                    m_Context.SetInvalid(output, continuity, AnimationPoseNativeInvalidReason.SlotContributionInvalid, header.Index);
                    return;
                }
                m_Context.m_ValueContributions[destinationContributionOffset + contribution] = primitive;
                for (int bone = 0; bone < m_Context.m_BoneCount; bone++)
                {
                    float weight = m_Context.m_SlotDenseContributionWeights[
                        range.DenseContributionWeightOffset + contribution * m_Context.m_BoneCount + bone];
                    if (!CharacterPoseExecutionContext.IsWeight(weight))
                    {
                        m_Context.SetInvalid(output, continuity, AnimationPoseNativeInvalidReason.SlotContributionInvalid, header.Index);
                        return;
                    }
                    m_Context.m_ValueDenseContributionWeights[
                        destinationDenseOffset + contribution * m_Context.m_BoneCount + bone] = weight;
                }
            }

            m_Context.m_ValueContributionCounts[output] = contributionCount;
            m_Context.m_ValueOutputWeights[output] = outputWeight;
            m_Context.m_ValueLeftFootFeatures[output] = hasFootFeatures == 1
                ? m_Context.m_SlotLeftFootFeatures[slotIndex]
                : default;
            m_Context.m_ValueRightFootFeatures[output] = hasFootFeatures == 1
                ? m_Context.m_SlotRightFootFeatures[slotIndex]
                : default;
            m_Context.m_ValueHasFootFeatures[output] = hasFootFeatures;
            m_Context.m_ValueAvailability[output] = AnimationPoseAvailability.Pose;
            m_Context.m_ValueContinuityIdentities[output] = continuity;
            m_Context.m_ValueDiscontinuities[output] = m_Context.m_SlotDiscontinuities[slotIndex];
            m_Context.m_ValueInvalidReasons[output] = AnimationPoseNativeInvalidReason.None;
        }

        internal void EvaluateAnimationSlot(
            in CharacterPoseNativeOperationHeader header,
            in CharacterPoseNativeAnimationSlotOperation operation,
            float deltaSeconds)
        {
            int source = operation.InputPoseValueIndex;
            int output = operation.OutputPoseValueIndex;
            if (!m_Context.IsInputReady(source, header.Index) ||
                m_Context.m_ValueAvailability[source] != AnimationPoseAvailability.Pose)
            {
                m_Context.SetInvalid(
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
            if (m_Context.m_ValueAvailability[output] == AnimationPoseAvailability.Invalid)
                return;
            if (m_Context.m_ValueAvailability[output] == AnimationPoseAvailability.NoPose)
            {
                if (!m_Context.TryCopyValue(source, output, header.Index))
                {
                    m_Context.SetInvalid(
                        output,
                        m_Context.m_ValueContinuityIdentities[source],
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

            int actionContributionCount = m_Context.m_ValueContributionCounts[output];
            int actionContributionStart = m_Context.m_ContributionStride - actionContributionCount;
            if (actionContributionCount <= 0 || actionContributionStart < 0)
            {
                m_Context.SetInvalid(
                    output,
                    m_Context.m_ValueContinuityIdentities[output],
                    AnimationPoseNativeInvalidReason.SlotContributionInvalid,
                    header.Index);
                return;
            }
            for (int contribution = actionContributionCount - 1; contribution >= 0; contribution--)
            {
                int target = actionContributionStart + contribution;
                m_Context.m_ValueContributions[m_Context.ContributionOffset(output) + target] =
                    m_Context.m_ValueContributions[m_Context.ContributionOffset(output) + contribution];
                for (int bone = 0; bone < m_Context.m_BoneCount; bone++)
                {
                    m_Context.SetContributionBoneWeight(
                        output,
                        target,
                        bone,
                        m_Context.GetContributionBoneWeight(output, contribution, bone));
                }
            }

            ulong actionContinuity = m_Context.m_ValueContinuityIdentities[output];
            float actionOutputWeight = m_Context.m_ValueOutputWeights[output];
            byte actionHasFootFeatures = m_Context.m_ValueHasFootFeatures[output];
            AnimationFootFeatureSample actionLeftFoot = m_Context.m_ValueLeftFootFeatures[output];
            AnimationFootFeatureSample actionRightFoot = m_Context.m_ValueRightFootFeatures[output];
            if (!m_Context.TryGetBoneOutputWeight(output, m_Context.m_LeftFootBoneIndex, out float actionLeftFootWeight) ||
                !m_Context.TryGetBoneOutputWeight(output, m_Context.m_RightFootBoneIndex, out float actionRightFootWeight))
            {
                m_Context.SetInvalid(
                    output,
                    actionContinuity,
                    AnimationPoseNativeInvalidReason.SlotContributionInvalid,
                    header.Index);
                return;
            }
            for (int bone = 0; bone < m_Context.m_BoneCount; bone++)
            {
                if (!m_Context.TryGetBoneOutputWeight(output, bone, out float actionBoneWeight))
                {
                    m_Context.SetInvalid(
                        output,
                        actionContinuity,
                        AnimationPoseNativeInvalidReason.SlotContributionInvalid,
                        header.Index);
                    return;
                }
                AnimationLocalBonePose actionPose = m_Context.m_ValueDenseLocalPoses[m_Context.PoseOffset(output) + bone];
                AnimationBlendBoneVelocity actionVelocity = m_Context.m_ValueDenseVelocities[m_Context.PoseOffset(output) + bone];
                AnimationBlendBoneVelocity sourceVelocity = m_Context.m_ValueDenseVelocities[m_Context.PoseOffset(source) + bone];
                if (!CharacterPoseExecutionContext.TryBlendPose(
                        m_Context.m_ValueDenseLocalPoses[m_Context.PoseOffset(source) + bone],
                        actionPose,
                        actionBoneWeight,
                        out AnimationLocalBonePose pose))
                {
                    m_Context.SetInvalid(
                        output,
                        actionContinuity,
                        AnimationPoseNativeInvalidReason.SlotPoseInvalid,
                        header.Index);
                    return;
                }
                m_Context.m_ValueDenseLocalPoses[m_Context.PoseOffset(output) + bone] = pose;
                m_Context.m_ValueDenseVelocities[m_Context.PoseOffset(output) + bone] = new AnimationBlendBoneVelocity(
                    Vector3.LerpUnclamped(sourceVelocity.Linear, actionVelocity.Linear, actionBoneWeight),
                    Vector3.LerpUnclamped(sourceVelocity.Angular, actionVelocity.Angular, actionBoneWeight),
                    Vector3.LerpUnclamped(sourceVelocity.Scale, actionVelocity.Scale, actionBoneWeight));
            }

            m_Context.m_ValueContributionCounts[output] = 0;
            m_Context.m_ValueOutputWeights[output] = actionOutputWeight;
            for (int contribution = 0; contribution < actionContributionCount; contribution++)
            {
                if (!m_Context.TryAddContribution(
                        header.Weight,
                        -1,
                        output,
                        actionContributionStart + contribution,
                        output,
                        output,
                        true,
                        false))
                {
                    m_Context.SetInvalid(
                        output,
                        actionContinuity,
                        AnimationPoseNativeInvalidReason.SlotContributionInvalid,
                        header.Index);
                    return;
                }
            }
            for (int contribution = 0; contribution < m_Context.m_ValueContributionCounts[source]; contribution++)
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
                    m_Context.SetInvalid(
                        output,
                        actionContinuity,
                        AnimationPoseNativeInvalidReason.SlotContributionInvalid,
                        header.Index);
                    return;
                }
            }

            m_Context.m_ValueAvailability[output] = AnimationPoseAvailability.Pose;
            m_Context.m_ValueOutputWeights[output] = CharacterPoseExecutionContext.UnionWeight(
                m_Context.m_ValueOutputWeights[source],
                actionOutputWeight);
            m_Context.m_ValueContinuityIdentities[output] = CharacterPoseExecutionContext.CombineContinuity(
                m_Context.m_ValueContinuityIdentities[source],
                actionContinuity,
                header.Index);
            m_Context.m_ValueInvalidReasons[output] = AnimationPoseNativeInvalidReason.None;
            if (!m_Context.TryCopyParameters(source, output) ||
                !TryResolveAnimationSlotFootFeatures(
                    source,
                    output,
                    actionHasFootFeatures,
                    actionLeftFoot,
                    actionRightFoot,
                    actionLeftFootWeight,
                    actionRightFootWeight))
            {
                m_Context.SetInvalid(
                    output,
                    m_Context.m_ValueContinuityIdentities[output],
                    AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid,
                    header.Index);
            }
            if (m_Context.m_ValueAvailability[output] == AnimationPoseAvailability.Pose)
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
            if ((uint)operation.AnimationSlotIndex >= (uint)m_Context.m_AnimationSlotControls.Length ||
                !float.IsFinite(deltaSeconds) || deltaSeconds < 0f)
            {
                m_Context.SetInvalid(
                    output,
                    m_Context.m_ValueContinuityIdentities[output],
                    AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid,
                    header.Index);
                return;
            }
            int stateIndex = m_Context.m_AnimationSlotNodeOffset + operation.AnimationSlotIndex;
            if ((uint)stateIndex >= (uint)m_Context.m_InertialStates.Length)
            {
                m_Context.SetInvalid(
                    output,
                    m_Context.m_ValueContinuityIdentities[output],
                    AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid,
                    header.Index);
                return;
            }
            CharacterAnimationSlotNativeControl control =
                m_Context.m_AnimationSlotControls[operation.AnimationSlotIndex];
            PoseInertializationNativeState state = m_Inertialization.CommittedInertialState(stateIndex);
            m_Inertialization.PrepareInertialNode(stateIndex, in state);
            if (control.Generation == 0 || control.Generation < state.LastEventIdentity)
            {
                m_Context.SetInvalid(
                    output,
                    m_Context.m_ValueContinuityIdentities[output],
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
                        m_Context.m_InertialRules[ruleIndex].Mode != PoseInertializationMode.Inertialize)
                    {
                        state.RuntimeState = PoseInertializationRuntimeState.Invalid;
                        m_Context.m_InertialStates[stateIndex] = state;
                        m_Context.SetInvalid(
                            output,
                            m_Context.m_ValueContinuityIdentities[output],
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
                PoseInertializationNativeRule rule = m_Context.m_InertialRules[state.ActiveRuleIndex];
                bool anyActive = false;
                for (int bone = 0; bone < m_Context.m_BoneCount; bone++)
                {
                    int residualIndex = stateIndex * m_Context.m_BoneCount + bone;
                    float duration = state.ActiveDurationSeconds *
                                     m_Context.m_InertialDenseProfiles[rule.ProfileOffset + bone];
                    m_Inertialization.EvaluateInertialEnvelope(
                        rule,
                        state.ElapsedSeconds,
                        duration,
                        out _,
                        out float weight,
                        out float derivative);
                    anyActive |= state.ElapsedSeconds < duration;
                    AnimationLocalBonePose target = m_Context.m_ValueDenseLocalPoses[m_Context.PoseOffset(output) + bone];
                    AnimationBlendBoneVelocity targetVelocity = m_Context.m_ValueDenseVelocities[m_Context.PoseOffset(output) + bone];
                    Vector3 positionBase = m_Context.m_InertialPositionResiduals[residualIndex] +
                                           state.ElapsedSeconds * m_Context.m_InertialLinearVelocityResiduals[residualIndex];
                    Vector3 rotationBase = m_Context.m_InertialRotationResiduals[residualIndex] +
                                           state.ElapsedSeconds * m_Context.m_InertialAngularVelocityResiduals[residualIndex];
                    Vector3 scaleBase = m_Context.m_InertialScaleResiduals[residualIndex] +
                                        state.ElapsedSeconds * m_Context.m_InertialScaleVelocityResiduals[residualIndex];
                    Vector3 linear = targetVelocity.Linear + derivative * positionBase +
                                     weight * m_Context.m_InertialLinearVelocityResiduals[residualIndex];
                    Vector3 angular = targetVelocity.Angular + derivative * rotationBase +
                                      weight * m_Context.m_InertialAngularVelocityResiduals[residualIndex];
                    Vector3 scaleVelocity = targetVelocity.Scale + derivative * scaleBase +
                                            weight * m_Context.m_InertialScaleVelocityResiduals[residualIndex];
                    if (!CharacterPoseExecutionContext.IsFinite(linear) || !CharacterPoseExecutionContext.IsFinite(angular) || !CharacterPoseExecutionContext.IsFinite(scaleVelocity))
                    {
                        state.RuntimeState = PoseInertializationRuntimeState.Invalid;
                        m_Context.m_InertialStates[stateIndex] = state;
                        m_Context.SetInvalid(
                            output,
                            m_Context.m_ValueContinuityIdentities[output],
                            AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid,
                            header.Index);
                        return;
                    }
                    m_Context.m_ValueDenseLocalPoses[m_Context.PoseOffset(output) + bone] = new AnimationLocalBonePose(
                        target.Position + weight * positionBase,
                        AnimationPoseMath.QuaternionExp(weight * rotationBase) * target.Rotation,
                        target.Scale + weight * scaleBase);
                    m_Context.m_ValueDenseVelocities[m_Context.PoseOffset(output) + bone] =
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
            state.OutputCompletionIdentity = m_Context.m_CompletionIdentity;
            m_Context.m_InertialStates[stateIndex] = state;
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
                m_Context.m_ValueContributions[m_Context.ContributionOffset(sourceValue) + sourceIndex];
            if (!CharacterPoseExecutionContext.IsValidPrimitiveContribution(source))
                return false;
            float scalarWeight = source.Weight * Mathf.Clamp01(1f - actionOutputWeight);
            float leftWeight = source.LeftFootWeight * Mathf.Clamp01(1f - actionLeftFootWeight);
            float rightWeight = source.RightFootWeight * Mathf.Clamp01(1f - actionRightFootWeight);
            if (!CharacterPoseExecutionContext.IsWeight(scalarWeight) || !CharacterPoseExecutionContext.IsWeight(leftWeight) || !CharacterPoseExecutionContext.IsWeight(rightWeight))
                return false;

            int targetIndex = m_Context.FindContribution(output, source);
            if (targetIndex < 0)
            {
                targetIndex = m_Context.m_ValueContributionCounts[output];
                if (targetIndex >= m_Context.m_ContributionStride)
                    return false;
                m_Context.m_ValueContributionCounts[output] = targetIndex + 1;
                m_Context.ClearContributionWeights(output, targetIndex);
                m_Context.m_ValueContributions[m_Context.ContributionOffset(output) + targetIndex] =
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
                    m_Context.m_ValueContributions[m_Context.ContributionOffset(output) + targetIndex];
                m_Context.m_ValueContributions[m_Context.ContributionOffset(output) + targetIndex] =
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

            for (int bone = 0; bone < m_Context.m_BoneCount; bone++)
            {
                float actionWeight = 0f;
                for (int action = 0; action < actionContributionCount; action++)
                {
                    actionWeight += m_Context.GetContributionBoneWeight(
                        output,
                        actionContributionStart + action,
                        bone);
                }
                if (!float.IsFinite(actionWeight))
                    return false;
                float weight = m_Context.GetContributionBoneWeight(sourceValue, sourceIndex, bone) *
                               Mathf.Clamp01(1f - actionWeight);
                float combined = Mathf.Clamp01(
                    m_Context.GetContributionBoneWeight(output, targetIndex, bone) + weight);
                if (!CharacterPoseExecutionContext.IsWeight(combined))
                    return false;
                m_Context.SetContributionBoneWeight(output, targetIndex, bone, combined);
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
            bool hasSource = m_Context.m_ValueHasFootFeatures[source] == 1;
            bool hasAction = actionHasFootFeatures == 1;
            if (!hasSource && !hasAction)
            {
                m_Context.m_ValueHasFootFeatures[output] = 0;
                m_Context.m_ValueLeftFootFeatures[output] = default;
                m_Context.m_ValueRightFootFeatures[output] = default;
                return true;
            }
            if (!CharacterPoseExecutionContext.TryResolveFeature(
                    hasSource,
                    m_Context.m_ValueLeftFootFeatures[source],
                    hasAction,
                    actionLeftFoot,
                    actionLeftFootWeight,
                    hasAction && actionLeftFootWeight > 0f,
                    out AnimationFootFeatureSample left) ||
                !CharacterPoseExecutionContext.TryResolveFeature(
                    hasSource,
                    m_Context.m_ValueRightFootFeatures[source],
                    hasAction,
                    actionRightFoot,
                    actionRightFootWeight,
                    hasAction && actionRightFootWeight > 0f,
                    out AnimationFootFeatureSample right))
            {
                return false;
            }
            m_Context.m_ValueLeftFootFeatures[output] = left;
            m_Context.m_ValueRightFootFeatures[output] = right;
            m_Context.m_ValueHasFootFeatures[output] = left.IsValid && right.IsValid ? (byte)1 : (byte)0;
            return true;
        }

    }
}
