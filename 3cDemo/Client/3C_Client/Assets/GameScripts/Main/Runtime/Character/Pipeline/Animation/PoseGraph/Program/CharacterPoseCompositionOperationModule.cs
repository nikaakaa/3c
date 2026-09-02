using System;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal sealed class CharacterPoseCompositionOperationModule
    {
        readonly CharacterPoseExecutionContext m_Context;

        internal CharacterPoseCompositionOperationModule(
            CharacterPoseExecutionContext context)
        {
            m_Context = context ??
                throw new ArgumentNullException(nameof(context));
        }

        internal void EvaluateBlendPose(AnimationPoseGraphNativeOperation operation)
        {
            float weight = operation.Weight;
            if (operation.ParameterIndex >= 0)
            {
                int input = operation.InputValueIndexA;
                if (!m_Context.IsInputReady(input, operation.Index) || operation.ParameterIndex >= m_Context.m_ParameterCount)
                {
                    m_Context.SetInvalid(operation.OutputValueIndex, (ulong)operation.Index + 1UL, AnimationPoseNativeInvalidReason.PoseGraphInputIncomplete, operation.Index);
                    return;
                }
                if (m_Context.m_ValuePoseParameterAvailability[m_Context.ParameterOffset(input) + operation.ParameterIndex] == 0)
                {
                    m_Context.SetInvalid(operation.OutputValueIndex, (ulong)operation.Index + 1UL, AnimationPoseNativeInvalidReason.SlotParameterInvalid, operation.Index);
                    return;
                }
                weight = Mathf.Clamp01(m_Context.m_ValuePoseParameters[m_Context.ParameterOffset(input) + operation.ParameterIndex]);
            }
            EvaluateLayeredBoneBlend(operation.WithWeight(weight));
        }

        internal void EvaluateLayeredBoneBlend(AnimationPoseGraphNativeOperation operation)
        {
            int output = operation.OutputValueIndex;
            int baseValue = operation.InputValueIndexA;
            int overlayValue = operation.InputValueIndexB;
            if (!m_Context.TryRequireInputs(operation, baseValue, overlayValue))
                return;
            if (m_Context.m_ValueAvailability[overlayValue] == AnimationPoseAvailability.NoPose)
            {
                if (!m_Context.TryCopyValue(baseValue, output, operation.Index))
                    m_Context.SetInvalid(output, m_Context.m_ValueContinuityIdentities[baseValue], AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid, operation.Index);
                return;
            }
            if (m_Context.m_ValueAvailability[baseValue] == AnimationPoseAvailability.NoPose)
            {
                if (!m_Context.TryCopyValue(overlayValue, output, operation.Index) ||
                    !m_Context.TryScaleValue(output, operation))
                {
                    m_Context.SetInvalid(output, m_Context.m_ValueContinuityIdentities[overlayValue], AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid, operation.Index);
                }
                return;
            }

            m_Context.m_ValueAvailability[output] = AnimationPoseAvailability.Pose;
            m_Context.m_ValueOutputWeights[output] = CharacterPoseExecutionContext.UnionWeight(
                m_Context.m_ValueOutputWeights[baseValue],
                m_Context.m_ValueOutputWeights[overlayValue] * operation.Weight);
            m_Context.m_ValueContinuityIdentities[output] = CharacterPoseExecutionContext.CombineContinuity(
                m_Context.m_ValueContinuityIdentities[baseValue],
                m_Context.m_ValueContinuityIdentities[overlayValue],
                operation.Index);
            m_Context.m_ValueInvalidReasons[output] = AnimationPoseNativeInvalidReason.None;
            for (int bone = 0; bone < m_Context.m_BoneCount; bone++)
            {
                if (!m_Context.TryGetBoneOutputWeight(overlayValue, bone, out float overlayOutputWeight))
                {
                    m_Context.SetInvalid(output, m_Context.m_ValueContinuityIdentities[output], AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid, operation.Index);
                    return;
                }
                float overlay = Mathf.Clamp01(
                    overlayOutputWeight * m_Context.GetMaskWeight(operation, bone) * operation.Weight);
                if (!CharacterPoseExecutionContext.TryBlendPose(
                        m_Context.m_ValueDenseLocalPoses[m_Context.PoseOffset(baseValue) + bone],
                        m_Context.m_ValueDenseLocalPoses[m_Context.PoseOffset(overlayValue) + bone],
                        overlay,
                        out AnimationLocalBonePose pose))
                {
                    m_Context.SetInvalid(output, m_Context.m_ValueContinuityIdentities[output], AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid, operation.Index);
                    return;
                }
                m_Context.m_ValueDenseLocalPoses[m_Context.PoseOffset(output) + bone] = pose;
            }
            if (!m_Context.TryCopyParameters(baseValue, output) ||
                !m_Context.TryMergeContributions(operation, baseValue, overlayValue, output, false) ||
                !m_Context.TryResolveFootFeatures(operation, baseValue, overlayValue, output, false))
            {
                m_Context.SetInvalid(output, m_Context.m_ValueContinuityIdentities[output], AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid, operation.Index);
            }
        }

        internal void EvaluateAdditivePose(AnimationPoseGraphNativeOperation operation)
        {
            int output = operation.OutputValueIndex;
            int baseValue = operation.InputValueIndexA;
            int additiveValue = operation.InputValueIndexB;
            if (!m_Context.TryRequireInputs(operation, baseValue, additiveValue))
                return;
            if (m_Context.m_ValueAvailability[additiveValue] == AnimationPoseAvailability.NoPose)
            {
                if (!m_Context.TryCopyValue(baseValue, output, operation.Index))
                    m_Context.SetInvalid(output, m_Context.m_ValueContinuityIdentities[baseValue], AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid, operation.Index);
                return;
            }
            if (m_Context.m_ValueAvailability[baseValue] != AnimationPoseAvailability.Pose)
            {
                m_Context.SetInvalid(
                    output,
                    CharacterPoseExecutionContext.CombineContinuity(
                        m_Context.m_ValueContinuityIdentities[baseValue],
                        m_Context.m_ValueContinuityIdentities[additiveValue],
                        operation.Index),
                    AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid,
                    operation.Index);
                return;
            }

            m_Context.m_ValueAvailability[output] = AnimationPoseAvailability.Pose;
            m_Context.m_ValueOutputWeights[output] = CharacterPoseExecutionContext.UnionWeight(
                m_Context.m_ValueOutputWeights[baseValue],
                m_Context.m_ValueOutputWeights[additiveValue] * operation.Weight);
            m_Context.m_ValueContinuityIdentities[output] = CharacterPoseExecutionContext.CombineContinuity(
                m_Context.m_ValueContinuityIdentities[baseValue],
                m_Context.m_ValueContinuityIdentities[additiveValue],
                operation.Index);
            m_Context.m_ValueInvalidReasons[output] = AnimationPoseNativeInvalidReason.None;
            for (int bone = 0; bone < m_Context.m_BoneCount; bone++)
            {
                if (!m_Context.TryGetBoneOutputWeight(additiveValue, bone, out float additiveOutputWeight))
                {
                    m_Context.SetInvalid(output, m_Context.m_ValueContinuityIdentities[output], AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid, operation.Index);
                    return;
                }
                float weight = Mathf.Clamp01(
                    additiveOutputWeight * m_Context.GetMaskWeight(operation, bone) * operation.Weight);
                bool valid = operation.AdditiveReferenceSpace switch
                {
                    AdditiveReferenceSpace.Local => CharacterPoseExecutionContext.TryAddPose(
                        m_Context.m_ValueDenseLocalPoses[m_Context.PoseOffset(baseValue) + bone],
                        m_Context.m_ValueDenseLocalPoses[m_Context.PoseOffset(additiveValue) + bone],
                        m_Context.m_AdditiveReferences[operation.AdditiveReferenceOffset + bone],
                        operation.AdditiveScalePolicy,
                        weight,
                        out AnimationLocalBonePose localPose) &&
                        m_Context.AssignPose(output, bone, localPose),
                    AdditiveReferenceSpace.Mesh => m_Context.TryAddMeshPose(
                        baseValue,
                        additiveValue,
                        output,
                        operation,
                        bone,
                        weight,
                        out AnimationLocalBonePose meshPose) &&
                        m_Context.AssignPose(output, bone, meshPose),
                    _ => false
                };
                if (!valid)
                {
                    m_Context.SetInvalid(output, m_Context.m_ValueContinuityIdentities[output], AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid, operation.Index);
                    return;
                }
            }
            if (!m_Context.TryCopyParameters(baseValue, output) ||
                !m_Context.TryMergeContributions(operation, baseValue, additiveValue, output, true) ||
                !m_Context.TryResolveFootFeatures(operation, baseValue, additiveValue, output, true))
            {
                m_Context.SetInvalid(output, m_Context.m_ValueContinuityIdentities[output], AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid, operation.Index);
            }
        }

        internal void EvaluatePoseParameterResolve(AnimationPoseGraphNativeOperation operation)
        {
            int output = operation.OutputValueIndex;
            int baseValue = operation.InputValueIndexA;
            int parameterSourceValue = operation.InputValueIndexB;
            if (!m_Context.TryRequireInputs(operation, baseValue, parameterSourceValue))
                return;
            if (!m_Context.TryCopyValue(baseValue, output, operation.Index))
            {
                m_Context.SetInvalid(output, m_Context.m_ValueContinuityIdentities[baseValue], AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid, operation.Index);
                return;
            }
            if (m_Context.m_ValueAvailability[parameterSourceValue] == AnimationPoseAvailability.NoPose)
                return;
            if (!m_Context.TryResolveParameters(operation, baseValue, parameterSourceValue, output))
            {
                m_Context.SetInvalid(output, m_Context.m_ValueContinuityIdentities[output], AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid, operation.Index);
                return;
            }
            m_Context.m_ValueContinuityIdentities[output] = CharacterPoseExecutionContext.CombineContinuity(
                m_Context.m_ValueContinuityIdentities[baseValue],
                m_Context.m_ValueContinuityIdentities[parameterSourceValue],
                operation.Index);
        }

    }
}
