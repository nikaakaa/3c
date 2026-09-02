using System;
using ThirdPersonCharacter.Pipeline.Animation.BlendStack;
using ThirdPersonCharacter.Pipeline.Animation.Presentation;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonSimulation;
using Unity.Collections;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal sealed class CharacterPoseStateOperationModule
    {
        readonly CharacterPoseExecutionContext m_Context;

        internal CharacterPoseStateOperationModule(
            CharacterPoseExecutionContext context)
        {
            m_Context = context ??
                throw new ArgumentNullException(nameof(context));
        }

        internal void EvaluateStatePoseOutput(AnimationPoseGraphNativeOperation operation)
        {
            int input = operation.InputValueIndexA;
            if (!m_Context.IsInputReady(input, operation.Index) ||
                !m_Context.TryCopyValue(input, operation.OutputValueIndex, operation.Index))
            {
                m_Context.SetInvalid(
                    operation.OutputValueIndex,
                    (ulong)operation.Index + 1UL,
                    AnimationPoseNativeInvalidReason.PoseGraphInputIncomplete,
                    operation.Index);
            }
        }

        internal void EvaluatePoseStateMachine(AnimationPoseGraphNativeOperation operation)
        {
            if ((uint)operation.StateMachineIndex >= (uint)m_Context.m_StateMachineControls.Length)
            {
                m_Context.SetInvalid(
                    operation.OutputValueIndex,
                    (ulong)operation.Index + 1UL,
                    AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid,
                    operation.Index);
                return;
            }
            CharacterPoseStateMachineNativeControl control =
                m_Context.m_StateMachineControls[operation.StateMachineIndex];
            if (control.Generation == 0 ||
                control.SourcePoseValueIndex < 0 ||
                control.SourcePoseValueIndex >= operation.OutputValueIndex ||
                control.TargetPoseValueIndex < 0 ||
                control.TargetPoseValueIndex >= operation.OutputValueIndex ||
                control.PredictionPoseValueIndex < -1 ||
                control.PredictionPoseValueIndex >= operation.OutputValueIndex)
            {
                m_Context.SetInvalid(
                    operation.OutputValueIndex,
                    (ulong)operation.Index + 1UL,
                    AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid,
                    operation.Index);
                return;
            }
            if (control.BlendMode == CharacterPoseStateMachineBlendMode.Single ||
                control.BlendMode == CharacterPoseStateMachineBlendMode.Inertialization)
            {
                if (!m_Context.IsInputReady(control.TargetPoseValueIndex, operation.Index) ||
                    !m_Context.TryCopyValue(
                        control.TargetPoseValueIndex,
                        operation.OutputValueIndex,
                        operation.Index) ||
                    !TryApplyStateMachinePrediction(
                        operation.OutputValueIndex,
                        control.PredictionPoseValueIndex,
                        operation.Index))
                {
                    m_Context.SetInvalid(
                        operation.OutputValueIndex,
                        (ulong)operation.Index + 1UL,
                        AnimationPoseNativeInvalidReason.PoseGraphInputIncomplete,
                        operation.Index);
                }
                return;
            }
            if (control.BlendMode != CharacterPoseStateMachineBlendMode.Standard)
            {
                m_Context.SetInvalid(
                    operation.OutputValueIndex,
                    (ulong)operation.Index + 1UL,
                    AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid,
                    operation.Index);
                return;
            }
            EvaluateStateMachineStandardBlend(operation, control);
        }

        void EvaluateStateMachineStandardBlend(
            AnimationPoseGraphNativeOperation operation,
            CharacterPoseStateMachineNativeControl control)
        {
            int output = operation.OutputValueIndex;
            int source = control.SourcePoseValueIndex;
            int target = control.TargetPoseValueIndex;
            if (!m_Context.TryRequireInputs(operation, source, target) ||
                (uint)control.CurveIndex >= (uint)m_Context.m_BlendCurves.Length ||
                (control.DurationSeconds > 0f &&
                 (uint)control.BlendProfileIndex >= (uint)m_Context.m_BlendProfiles.Length))
            {
                m_Context.SetInvalid(
                    output,
                    (ulong)operation.Index + 1UL,
                    AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid,
                    operation.Index);
                return;
            }
            if (control.DurationSeconds <= 0f)
            {
                if (!m_Context.TryCopyValue(target, output, operation.Index))
                {
                    m_Context.SetInvalid(
                        output,
                        m_Context.m_ValueContinuityIdentities[target],
                        AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid,
                        operation.Index);
                }
                return;
            }
            if (m_Context.m_ValueAvailability[source] != AnimationPoseAvailability.Pose ||
                m_Context.m_ValueAvailability[target] != AnimationPoseAvailability.Pose)
            {
                m_Context.SetInvalid(
                    output,
                    CharacterPoseExecutionContext.CombineContinuity(
                        m_Context.m_ValueContinuityIdentities[source],
                        m_Context.m_ValueContinuityIdentities[target],
                        operation.Index),
                    AnimationPoseNativeInvalidReason.PoseGraphInputIncomplete,
                    operation.Index);
                return;
            }

            AnimationBlendProfileNativeEntry profile =
                m_Context.m_BlendProfiles[control.BlendProfileIndex];
            float globalDuration = control.DurationSeconds *
                                   profile.GlobalDurationMultiplier;
            float globalWeight = EvaluateStandardBlendCurve(
                control.CurveIndex,
                control.ElapsedSeconds,
                globalDuration);
            float leftWeight = EvaluateStandardBlendBoneWeight(
                control,
                profile,
                m_Context.m_LeftFootBoneIndex);
            float rightWeight = EvaluateStandardBlendBoneWeight(
                control,
                profile,
                m_Context.m_RightFootBoneIndex);

            m_Context.m_ValueAvailability[output] = AnimationPoseAvailability.Pose;
            m_Context.m_ValueOutputWeights[output] = CharacterPoseExecutionContext.UnionWeight(
                m_Context.m_ValueOutputWeights[source],
                m_Context.m_ValueOutputWeights[target] * globalWeight);
            m_Context.m_ValueContinuityIdentities[output] = CharacterPoseExecutionContext.CombineContinuity(
                m_Context.m_ValueContinuityIdentities[source],
                m_Context.m_ValueContinuityIdentities[target],
                operation.Index);
            m_Context.m_ValueDiscontinuities[output] = m_Context.m_ValueDiscontinuities[target];
            m_Context.m_ValueInvalidReasons[output] = AnimationPoseNativeInvalidReason.None;
            for (int bone = 0; bone < m_Context.m_BoneCount; bone++)
            {
                float weight = EvaluateStandardBlendBoneWeight(
                    control,
                    profile,
                    bone);
                if (!CharacterPoseExecutionContext.TryBlendPose(
                        m_Context.m_ValueDenseLocalPoses[m_Context.PoseOffset(source) + bone],
                        m_Context.m_ValueDenseLocalPoses[m_Context.PoseOffset(target) + bone],
                        weight,
                        out AnimationLocalBonePose pose))
                {
                    m_Context.SetInvalid(
                        output,
                        m_Context.m_ValueContinuityIdentities[output],
                        AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid,
                        operation.Index);
                    return;
                }
                m_Context.m_ValueDenseLocalPoses[m_Context.PoseOffset(output) + bone] = pose;
                AnimationBlendBoneVelocity sourceVelocity =
                    m_Context.m_ValueDenseVelocities[m_Context.PoseOffset(source) + bone];
                AnimationBlendBoneVelocity targetVelocity =
                    m_Context.m_ValueDenseVelocities[m_Context.PoseOffset(target) + bone];
                m_Context.m_ValueDenseVelocities[m_Context.PoseOffset(output) + bone] =
                    new AnimationBlendBoneVelocity(
                        Vector3.LerpUnclamped(sourceVelocity.Linear, targetVelocity.Linear, weight),
                        Vector3.LerpUnclamped(sourceVelocity.Angular, targetVelocity.Angular, weight),
                        Vector3.LerpUnclamped(sourceVelocity.Scale, targetVelocity.Scale, weight));
            }
            if (!TryBlendStateMachineParameters(source, target, output, globalWeight) ||
                !TryMergeStateMachineContributions(
                    source,
                    target,
                    output,
                    control,
                    profile,
                    globalWeight,
                    leftWeight,
                    rightWeight) ||
                !TryBlendStateMachineFootFeatures(
                    source,
                    target,
                    output,
                    leftWeight,
                    rightWeight) ||
                !TryApplyStateMachinePrediction(
                    output,
                    control.PredictionPoseValueIndex,
                    operation.Index))
            {
                m_Context.SetInvalid(
                    output,
                    m_Context.m_ValueContinuityIdentities[output],
                    AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid,
                    operation.Index);
            }
        }

        float EvaluateStandardBlendBoneWeight(
            CharacterPoseStateMachineNativeControl control,
            AnimationBlendProfileNativeEntry profile,
            int bone)
        {
            float duration = control.DurationSeconds *
                             profile.GlobalDurationMultiplier *
                             m_Context.m_BlendDenseProfiles[profile.DenseOffset + bone];
            return EvaluateStandardBlendCurve(
                control.CurveIndex,
                control.ElapsedSeconds,
                duration);
        }

        float EvaluateStandardBlendCurve(
            int curveIndex,
            float elapsedSeconds,
            float durationSeconds)
        {
            if (durationSeconds <= 0f || elapsedSeconds >= durationSeconds)
                return 1f;
            float time = Mathf.Clamp01(elapsedSeconds / durationSeconds);
            AnimationBlendCurveNativeEntry curve = m_Context.m_BlendCurves[curveIndex];
            AnimationBlendCurveSegment segment =
                m_Context.m_BlendCurveSegments[curve.SegmentOffset + curve.SegmentCount - 1];
            for (int i = 0; i < curve.SegmentCount; i++)
            {
                AnimationBlendCurveSegment candidate =
                    m_Context.m_BlendCurveSegments[curve.SegmentOffset + i];
                if (time <= candidate.EndTime)
                {
                    segment = candidate;
                    break;
                }
            }
            float u = (time - segment.StartTime) /
                      (segment.EndTime - segment.StartTime);
            return Mathf.Clamp01(
                ((segment.A * u + segment.B) * u + segment.C) * u + segment.D);
        }

        bool TryBlendStateMachineParameters(
            int source,
            int target,
            int output,
            float targetWeight)
        {
            for (int parameter = 0; parameter < m_Context.m_ParameterCount; parameter++)
            {
                int sourceOffset = m_Context.ParameterOffset(source) + parameter;
                int targetOffset = m_Context.ParameterOffset(target) + parameter;
                int outputOffset = m_Context.ParameterOffset(output) + parameter;
                float sourceValue = m_Context.m_ValuePoseParameters[sourceOffset];
                float targetValue = m_Context.m_ValuePoseParameters[targetOffset];
                if (!float.IsFinite(sourceValue) || !float.IsFinite(targetValue))
                    return false;
                bool sourceAvailable = m_Context.m_ValuePoseParameterAvailability[sourceOffset] != 0;
                bool targetAvailable = m_Context.m_ValuePoseParameterAvailability[targetOffset] != 0;
                bool available = sourceAvailable && targetWeight < 1f ||
                                 targetAvailable && targetWeight > 0f;
                float value = sourceAvailable && targetAvailable
                    ? Mathf.LerpUnclamped(sourceValue, targetValue, targetWeight)
                    : targetAvailable && targetWeight > 0f
                        ? targetValue
                        : sourceAvailable && targetWeight < 1f
                            ? sourceValue
                            : m_Context.m_ParameterDefaults[parameter];
                if (!float.IsFinite(value))
                    return false;
                m_Context.m_ValuePoseParameters[outputOffset] = value;
                m_Context.m_ValuePoseParameterAvailability[outputOffset] =
                    available ? (byte)1 : (byte)0;
            }
            return true;
        }

        bool TryMergeStateMachineContributions(
            int source,
            int target,
            int output,
            CharacterPoseStateMachineNativeControl control,
            AnimationBlendProfileNativeEntry profile,
            float globalWeight,
            float leftWeight,
            float rightWeight)
        {
            m_Context.m_ValueContributionCounts[output] = 0;
            for (int contribution = 0;
                 contribution < m_Context.m_ValueContributionCounts[source];
                 contribution++)
            {
                if (!TryAddStateMachineContribution(
                        source,
                        contribution,
                        output,
                        control,
                        profile,
                        1f - globalWeight,
                        1f - leftWeight,
                        1f - rightWeight,
                        false))
                    return false;
            }
            for (int contribution = 0;
                 contribution < m_Context.m_ValueContributionCounts[target];
                 contribution++)
            {
                if (!TryAddStateMachineContribution(
                        target,
                        contribution,
                        output,
                        control,
                        profile,
                        globalWeight,
                        leftWeight,
                        rightWeight,
                        true))
                    return false;
            }
            return true;
        }

        bool TryAddStateMachineContribution(
            int sourceValue,
            int sourceIndex,
            int output,
            CharacterPoseStateMachineNativeControl control,
            AnimationBlendProfileNativeEntry profile,
            float scalarFactor,
            float leftFactor,
            float rightFactor,
            bool target)
        {
            AnimationPrimitivePoseContribution source =
                m_Context.m_ValueContributions[m_Context.ContributionOffset(sourceValue) + sourceIndex];
            if (!CharacterPoseExecutionContext.IsValidPrimitiveContribution(source))
                return false;
            float scalarWeight = source.Weight * Mathf.Clamp01(scalarFactor);
            float leftWeight = source.LeftFootWeight * Mathf.Clamp01(leftFactor);
            float rightWeight = source.RightFootWeight * Mathf.Clamp01(rightFactor);
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
                float blendWeight = EvaluateStandardBlendBoneWeight(
                    control,
                    profile,
                    bone);
                float factor = target ? blendWeight : 1f - blendWeight;
                float weight = m_Context.GetContributionBoneWeight(
                                   sourceValue,
                                   sourceIndex,
                                   bone) * Mathf.Clamp01(factor);
                float combined = Mathf.Clamp01(
                    m_Context.GetContributionBoneWeight(output, targetIndex, bone) + weight);
                if (!CharacterPoseExecutionContext.IsWeight(combined))
                    return false;
                m_Context.SetContributionBoneWeight(output, targetIndex, bone, combined);
            }
            return true;
        }

        bool TryBlendStateMachineFootFeatures(
            int source,
            int target,
            int output,
            float leftWeight,
            float rightWeight)
        {
            bool hasSource = m_Context.m_ValueHasFootFeatures[source] != 0;
            bool hasTarget = m_Context.m_ValueHasFootFeatures[target] != 0;
            if (!hasSource && !hasTarget)
            {
                m_Context.m_ValueHasFootFeatures[output] = 0;
                return true;
            }
            if (!CharacterPoseExecutionContext.TryResolveStateMachineFeature(
                    hasSource,
                    m_Context.m_ValueLeftFootFeatures[source],
                    hasTarget,
                    m_Context.m_ValueLeftFootFeatures[target],
                    leftWeight,
                    out AnimationFootFeatureSample left) ||
                !CharacterPoseExecutionContext.TryResolveStateMachineFeature(
                    hasSource,
                    m_Context.m_ValueRightFootFeatures[source],
                    hasTarget,
                    m_Context.m_ValueRightFootFeatures[target],
                    rightWeight,
                    out AnimationFootFeatureSample right))
                return false;
            m_Context.m_ValueLeftFootFeatures[output] = left;
            m_Context.m_ValueRightFootFeatures[output] = right;
            m_Context.m_ValueHasFootFeatures[output] =
                left.IsValid && right.IsValid ? (byte)1 : (byte)0;
            return true;
        }

        bool TryApplyStateMachinePrediction(
            int output,
            int prediction,
            int operationIndex)
        {
            if (prediction < 0)
                return true;
            if (!m_Context.IsInputReady(prediction, operationIndex) ||
                m_Context.m_ValueHasFootFeatures[output] == 0 ||
                m_Context.m_ValueHasFootFeatures[prediction] == 0)
            {
                return false;
            }
            for (int contribution = 0;
                 contribution < m_Context.m_ValueContributionCounts[prediction];
                 contribution++)
            {
                AnimationPrimitivePoseContribution source =
                    m_Context.m_ValueContributions[
                        m_Context.ContributionOffset(prediction) + contribution];
                if (!CharacterPoseExecutionContext.IsValidPrimitiveContribution(source))
                    return false;
                if (m_Context.FindContribution(output, source) >= 0)
                    continue;
                int targetIndex = m_Context.m_ValueContributionCounts[output];
                if (targetIndex >= m_Context.m_ContributionStride)
                    return false;
                m_Context.m_ValueContributionCounts[output] = targetIndex + 1;
                m_Context.ClearContributionWeights(output, targetIndex);
                m_Context.m_ValueContributions[
                    m_Context.ContributionOffset(output) + targetIndex] =
                    new AnimationPrimitivePoseContribution(
                        source.PhysicalPlayerIndex,
                        source.PhysicalSourceIndex,
                        source.PhysicalSourceGeneration,
                        source.Kind,
                        source.SourceOwnerIndex,
                        source.ContributionContinuityIdentity,
                        0f,
                        0f,
                        0f);
            }
            AnimationFootFeatureSample left = ApplyStateMachinePrediction(
                m_Context.m_ValueLeftFootFeatures[output],
                m_Context.m_ValueLeftFootFeatures[prediction]);
            AnimationFootFeatureSample right = ApplyStateMachinePrediction(
                m_Context.m_ValueRightFootFeatures[output],
                m_Context.m_ValueRightFootFeatures[prediction]);
            if (!left.IsValid || !right.IsValid)
                return false;
            m_Context.m_ValueLeftFootFeatures[output] = left;
            m_Context.m_ValueRightFootFeatures[output] = right;
            m_Context.m_ValueHasFootFeatures[output] = 1;
            return true;
        }

        static AnimationFootFeatureSample ApplyStateMachinePrediction(
            AnimationFootFeatureSample output,
            AnimationFootFeatureSample prediction) =>
            output.WithPredictionPair(
                prediction.PredictedStep,
                prediction.IncomingPredictedStep);

    }
}
