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
        readonly NativeArray<AnimationBlendCurveNativeEntry> m_BlendCurves;
        readonly NativeArray<AnimationBlendCurveSegment> m_BlendCurveSegments;
        readonly NativeArray<AnimationBlendProfileNativeEntry> m_BlendProfiles;
        readonly NativeArray<float> m_BlendDenseProfiles;
        readonly NativeArray<float> m_ParameterDefaults;
        NativeArray<CharacterPoseStateMachineNativeControl> m_Controls;
        CharacterPoseValuePageSlice m_Values;

        internal CharacterPoseStateOperationModule(
            NativeArray<AnimationBlendCurveNativeEntry> blendCurves,
            NativeArray<AnimationBlendCurveSegment> blendCurveSegments,
            NativeArray<AnimationBlendProfileNativeEntry> blendProfiles,
            NativeArray<float> blendDenseProfiles,
            NativeArray<float> parameterDefaults)
        {
            m_BlendCurves = blendCurves;
            m_BlendCurveSegments = blendCurveSegments;
            m_BlendProfiles = blendProfiles;
            m_BlendDenseProfiles = blendDenseProfiles;
            m_ParameterDefaults = parameterDefaults;
        }

        internal void BindFrame(
            in CharacterPoseValuePageSlice values,
            NativeArray<CharacterPoseStateMachineNativeControl> controls)
        {
            if (!values.IsValid || !controls.IsCreated)
                throw new ArgumentException("Pose State frame binding is invalid.");
            m_Values = values;
            m_Controls = controls;
        }

        internal void EvaluateStatePoseOutput(
            in CharacterPoseNativeOperationHeader header,
            in CharacterPoseNativeStateMachineOperation operation)
        {
            int input = operation.InputPoseValueIndex;
            if (!m_Values.IsInputReady(input, header.Index) ||
                !m_Values.TryCopyValue(
                    input,
                    operation.OutputPoseValueIndex,
                    header.Index))
            {
                m_Values.SetInvalid(
                    operation.OutputPoseValueIndex,
                    (ulong)header.Index + 1UL,
                    AnimationPoseNativeInvalidReason.PoseGraphInputIncomplete,
                    header.Index);
            }
        }

        internal void EvaluatePoseStateMachine(
            in CharacterPoseNativeOperationHeader header,
            in CharacterPoseNativeStateMachineOperation operation)
        {
            if ((uint)operation.StateMachineIndex >= (uint)m_Controls.Length)
            {
                m_Values.SetInvalid(
                    operation.OutputPoseValueIndex,
                    (ulong)header.Index + 1UL,
                    AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid,
                    header.Index);
                return;
            }
            CharacterPoseStateMachineNativeControl control =
                m_Controls[operation.StateMachineIndex];
            if (control.Generation == 0 ||
                control.SourcePoseValueIndex < 0 ||
                control.SourcePoseValueIndex >= operation.OutputPoseValueIndex ||
                control.TargetPoseValueIndex < 0 ||
                control.TargetPoseValueIndex >= operation.OutputPoseValueIndex ||
                control.PredictionPoseValueIndex < -1 ||
                control.PredictionPoseValueIndex >= operation.OutputPoseValueIndex)
            {
                m_Values.SetInvalid(
                    operation.OutputPoseValueIndex,
                    (ulong)header.Index + 1UL,
                    AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid,
                    header.Index);
                return;
            }
            if (control.BlendMode == CharacterPoseStateMachineBlendMode.Single ||
                control.BlendMode == CharacterPoseStateMachineBlendMode.Inertialization)
            {
                if (!m_Values.IsInputReady(control.TargetPoseValueIndex, header.Index) ||
                    !m_Values.TryCopyValue(
                        control.TargetPoseValueIndex,
                        operation.OutputPoseValueIndex,
                        header.Index) ||
                    !TryApplyStateMachinePrediction(
                        operation.OutputPoseValueIndex,
                        control.PredictionPoseValueIndex,
                        header.Index))
                {
                    m_Values.SetInvalid(
                        operation.OutputPoseValueIndex,
                        (ulong)header.Index + 1UL,
                        AnimationPoseNativeInvalidReason.PoseGraphInputIncomplete,
                        header.Index);
                }
                return;
            }
            if (control.BlendMode != CharacterPoseStateMachineBlendMode.Standard)
            {
                m_Values.SetInvalid(
                    operation.OutputPoseValueIndex,
                    (ulong)header.Index + 1UL,
                    AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid,
                    header.Index);
                return;
            }
            EvaluateStateMachineStandardBlend(
                in header,
                in operation,
                control);
        }

        void EvaluateStateMachineStandardBlend(
            in CharacterPoseNativeOperationHeader header,
            in CharacterPoseNativeStateMachineOperation operation,
            CharacterPoseStateMachineNativeControl control)
        {
            int output = operation.OutputPoseValueIndex;
            int source = control.SourcePoseValueIndex;
            int target = control.TargetPoseValueIndex;
            if (!m_Values.TryRequireInputs(
                    in header,
                    output,
                    source,
                    target) ||
                (uint)control.CurveIndex >= (uint)m_BlendCurves.Length ||
                (control.DurationSeconds > 0f &&
                 (uint)control.BlendProfileIndex >= (uint)m_BlendProfiles.Length))
            {
                m_Values.SetInvalid(
                    output,
                    (ulong)header.Index + 1UL,
                    AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid,
                    header.Index);
                return;
            }
            if (control.DurationSeconds <= 0f)
            {
                if (!m_Values.TryCopyValue(target, output, header.Index))
                {
                    m_Values.SetInvalid(
                        output,
                        m_Values.Continuity(target),
                        AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid,
                        header.Index);
                }
                return;
            }
            if (m_Values.Availability(source) != AnimationPoseAvailability.Pose ||
                m_Values.Availability(target) != AnimationPoseAvailability.Pose)
            {
                m_Values.SetInvalid(
                    output,
                    CharacterPosePureMath.CombineContinuity(
                        m_Values.Continuity(source),
                        m_Values.Continuity(target),
                        header.Index),
                    AnimationPoseNativeInvalidReason.PoseGraphInputIncomplete,
                    header.Index);
                return;
            }

            AnimationBlendProfileNativeEntry profile =
                m_BlendProfiles[control.BlendProfileIndex];
            float globalDuration = control.DurationSeconds *
                                   profile.GlobalDurationMultiplier;
            float globalWeight = EvaluateStandardBlendCurve(
                control.CurveIndex,
                control.ElapsedSeconds,
                globalDuration);
            float leftWeight = EvaluateStandardBlendBoneWeight(
                control,
                profile,
                m_Values.LeftFootBoneIndex);
            float rightWeight = EvaluateStandardBlendBoneWeight(
                control,
                profile,
                m_Values.RightFootBoneIndex);

            m_Values.SetAvailability(output, AnimationPoseAvailability.Pose);
            m_Values.SetOutputWeight(
                output,
                CharacterPosePureMath.UnionWeight(
                    m_Values.OutputWeight(source),
                    m_Values.OutputWeight(target) * globalWeight));
            m_Values.SetContinuity(
                output,
                CharacterPosePureMath.CombineContinuity(
                    m_Values.Continuity(source),
                    m_Values.Continuity(target),
                    header.Index));
            PoseDiscontinuityNative targetDiscontinuity =
                m_Values.Discontinuity(target);
            m_Values.SetDiscontinuity(output, in targetDiscontinuity);
            m_Values.SetInvalidReason(
                output,
                AnimationPoseNativeInvalidReason.None);
            for (int bone = 0; bone < m_Values.BoneCount; bone++)
            {
                float weight = EvaluateStandardBlendBoneWeight(
                    control,
                    profile,
                    bone);
                if (!CharacterPosePureMath.TryBlendPose(
                        m_Values.Pose(source, bone),
                        m_Values.Pose(target, bone),
                        weight,
                        out AnimationLocalBonePose pose))
                {
                    m_Values.SetInvalid(
                        output,
                        m_Values.Continuity(output),
                        AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid,
                        header.Index);
                    return;
                }
                m_Values.SetPose(output, bone, in pose);
                AnimationBlendBoneVelocity sourceVelocity =
                    m_Values.Velocity(source, bone);
                AnimationBlendBoneVelocity targetVelocity =
                    m_Values.Velocity(target, bone);
                var outputVelocity =
                    new AnimationBlendBoneVelocity(
                        Vector3.LerpUnclamped(sourceVelocity.Linear, targetVelocity.Linear, weight),
                        Vector3.LerpUnclamped(sourceVelocity.Angular, targetVelocity.Angular, weight),
                        Vector3.LerpUnclamped(sourceVelocity.Scale, targetVelocity.Scale, weight));
                m_Values.SetVelocity(output, bone, in outputVelocity);
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
                    header.Index))
            {
                m_Values.SetInvalid(
                    output,
                    m_Values.Continuity(output),
                    AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid,
                    header.Index);
            }
        }

        float EvaluateStandardBlendBoneWeight(
            CharacterPoseStateMachineNativeControl control,
            AnimationBlendProfileNativeEntry profile,
            int bone)
        {
            float duration = control.DurationSeconds *
                             profile.GlobalDurationMultiplier *
                             m_BlendDenseProfiles[profile.DenseOffset + bone];
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
            AnimationBlendCurveNativeEntry curve = m_BlendCurves[curveIndex];
            AnimationBlendCurveSegment segment =
                m_BlendCurveSegments[curve.SegmentOffset + curve.SegmentCount - 1];
            for (int i = 0; i < curve.SegmentCount; i++)
            {
                AnimationBlendCurveSegment candidate =
                    m_BlendCurveSegments[curve.SegmentOffset + i];
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
            for (int parameter = 0; parameter < m_Values.ParameterCount; parameter++)
            {
                float sourceValue = m_Values.Parameter(source, parameter);
                float targetValue = m_Values.Parameter(target, parameter);
                if (!float.IsFinite(sourceValue) || !float.IsFinite(targetValue))
                    return false;
                bool sourceAvailable =
                    m_Values.ParameterAvailable(source, parameter) != 0;
                bool targetAvailable =
                    m_Values.ParameterAvailable(target, parameter) != 0;
                bool available = sourceAvailable && targetWeight < 1f ||
                                 targetAvailable && targetWeight > 0f;
                float value = sourceAvailable && targetAvailable
                    ? Mathf.LerpUnclamped(sourceValue, targetValue, targetWeight)
                    : targetAvailable && targetWeight > 0f
                        ? targetValue
                        : sourceAvailable && targetWeight < 1f
                            ? sourceValue
                            : m_ParameterDefaults[parameter];
                if (!float.IsFinite(value))
                    return false;
                m_Values.SetParameter(
                    output,
                    parameter,
                    value,
                    available ? (byte)1 : (byte)0);
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
            m_Values.SetContributionCount(output, 0);
            for (int contribution = 0;
                 contribution < m_Values.ContributionCount(source);
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
                 contribution < m_Values.ContributionCount(target);
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
                m_Values.Contribution(sourceValue, sourceIndex);
            if (!CharacterPosePureMath.IsValidPrimitiveContribution(source))
                return false;
            float scalarWeight = source.Weight * Mathf.Clamp01(scalarFactor);
            float leftWeight = source.LeftFootWeight * Mathf.Clamp01(leftFactor);
            float rightWeight = source.RightFootWeight * Mathf.Clamp01(rightFactor);
            if (!CharacterPosePureMath.IsWeight(scalarWeight) || !CharacterPosePureMath.IsWeight(leftWeight) || !CharacterPosePureMath.IsWeight(rightWeight))
                return false;

            int targetIndex = m_Values.FindContribution(output, source);
            if (targetIndex < 0)
            {
                targetIndex = m_Values.ContributionCount(output);
                if (targetIndex >= m_Values.ContributionStride)
                    return false;
                m_Values.SetContributionCount(output, targetIndex + 1);
                m_Values.ClearContributionWeights(output, targetIndex);
                var contribution = new AnimationPrimitivePoseContribution(
                        source.PhysicalPlayerIndex,
                        source.PhysicalSourceIndex,
                        source.PhysicalSourceGeneration,
                        source.Kind,
                        source.SourceOwnerIndex,
                        source.ContributionContinuityIdentity,
                        scalarWeight,
                        leftWeight,
                        rightWeight);
                m_Values.SetContribution(
                    output,
                    targetIndex,
                    in contribution);
            }
            else
            {
                AnimationPrimitivePoseContribution current =
                    m_Values.Contribution(output, targetIndex);
                var contribution = new AnimationPrimitivePoseContribution(
                        current.PhysicalPlayerIndex,
                        current.PhysicalSourceIndex,
                        current.PhysicalSourceGeneration,
                        current.Kind,
                        current.SourceOwnerIndex,
                        current.ContributionContinuityIdentity,
                        Mathf.Clamp01(current.Weight + scalarWeight),
                        Mathf.Clamp01(current.LeftFootWeight + leftWeight),
                        Mathf.Clamp01(current.RightFootWeight + rightWeight));
                m_Values.SetContribution(
                    output,
                    targetIndex,
                    in contribution);
            }
            for (int bone = 0; bone < m_Values.BoneCount; bone++)
            {
                float blendWeight = EvaluateStandardBlendBoneWeight(
                    control,
                    profile,
                    bone);
                float factor = target ? blendWeight : 1f - blendWeight;
                float weight = m_Values.GetContributionBoneWeight(
                                   sourceValue,
                                   sourceIndex,
                                   bone) * Mathf.Clamp01(factor);
                float combined = Mathf.Clamp01(
                    m_Values.GetContributionBoneWeight(output, targetIndex, bone) + weight);
                if (!CharacterPosePureMath.IsWeight(combined))
                    return false;
                m_Values.SetContributionBoneWeight(output, targetIndex, bone, combined);
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
            bool hasSource = m_Values.HasFootFeatures(source) != 0;
            bool hasTarget = m_Values.HasFootFeatures(target) != 0;
            if (!hasSource && !hasTarget)
            {
                m_Values.SetFootFeatures(output, default, default, 0);
                return true;
            }
            if (!CharacterPosePureMath.TryResolveStateMachineFeature(
                    hasSource,
                    m_Values.LeftFoot(source),
                    hasTarget,
                    m_Values.LeftFoot(target),
                    leftWeight,
                    out AnimationFootFeatureSample left) ||
                !CharacterPosePureMath.TryResolveStateMachineFeature(
                    hasSource,
                    m_Values.RightFoot(source),
                    hasTarget,
                    m_Values.RightFoot(target),
                    rightWeight,
                    out AnimationFootFeatureSample right))
                return false;
            m_Values.SetFootFeatures(
                output,
                in left,
                in right,
                left.IsValid && right.IsValid ? (byte)1 : (byte)0);
            return true;
        }

        bool TryApplyStateMachinePrediction(
            int output,
            int prediction,
            int operationIndex)
        {
            if (prediction < 0)
                return true;
            if (!m_Values.IsInputReady(prediction, operationIndex) ||
                m_Values.HasFootFeatures(output) == 0 ||
                m_Values.HasFootFeatures(prediction) == 0)
            {
                return false;
            }
            for (int contribution = 0;
                 contribution < m_Values.ContributionCount(prediction);
                 contribution++)
            {
                AnimationPrimitivePoseContribution source =
                    m_Values.Contribution(prediction, contribution);
                if (!CharacterPosePureMath.IsValidPrimitiveContribution(source))
                    return false;
                if (m_Values.FindContribution(output, source) >= 0)
                    continue;
                int targetIndex = m_Values.ContributionCount(output);
                if (targetIndex >= m_Values.ContributionStride)
                    return false;
                m_Values.SetContributionCount(output, targetIndex + 1);
                m_Values.ClearContributionWeights(output, targetIndex);
                var contributionEntry = new AnimationPrimitivePoseContribution(
                        source.PhysicalPlayerIndex,
                        source.PhysicalSourceIndex,
                        source.PhysicalSourceGeneration,
                        source.Kind,
                        source.SourceOwnerIndex,
                        source.ContributionContinuityIdentity,
                        0f,
                        0f,
                        0f);
                m_Values.SetContribution(
                    output,
                    targetIndex,
                    in contributionEntry);
            }
            AnimationFootFeatureSample left = ApplyStateMachinePrediction(
                m_Values.LeftFoot(output),
                m_Values.LeftFoot(prediction));
            AnimationFootFeatureSample right = ApplyStateMachinePrediction(
                m_Values.RightFoot(output),
                m_Values.RightFoot(prediction));
            if (!left.IsValid || !right.IsValid)
                return false;
            m_Values.SetFootFeatures(output, in left, in right, 1);
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
