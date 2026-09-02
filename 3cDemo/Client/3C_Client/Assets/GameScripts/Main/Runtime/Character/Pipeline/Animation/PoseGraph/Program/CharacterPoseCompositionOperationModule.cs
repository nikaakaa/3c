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

        internal void EvaluateBlendPose(
            in CharacterPoseNativeOperationHeader header,
            in CharacterPoseNativeBlendOperation operation)
        {
            float weight = header.Weight;
            if (operation.ParameterIndex >= 0)
            {
                int input = operation.InputPoseValueIndexA;
                if (!m_Context.IsInputReady(input, header.Index) || operation.ParameterIndex >= m_Context.m_ParameterCount)
                {
                    m_Context.SetInvalid(operation.OutputPoseValueIndex, (ulong)header.Index + 1UL, AnimationPoseNativeInvalidReason.PoseGraphInputIncomplete, header.Index);
                    return;
                }
                if (m_Context.m_ValuePoseParameterAvailability[m_Context.ParameterOffset(input) + operation.ParameterIndex] == 0)
                {
                    m_Context.SetInvalid(operation.OutputPoseValueIndex, (ulong)header.Index + 1UL, AnimationPoseNativeInvalidReason.SlotParameterInvalid, header.Index);
                    return;
                }
                weight = Mathf.Clamp01(m_Context.m_ValuePoseParameters[m_Context.ParameterOffset(input) + operation.ParameterIndex]);
            }
            EvaluateLayeredBoneBlend(
                in header,
                operation.OutputPoseValueIndex,
                operation.InputPoseValueIndexA,
                operation.InputPoseValueIndexB,
                -1,
                weight);
        }

        internal void EvaluateLayeredBoneBlend(
            in CharacterPoseNativeOperationHeader header,
            in CharacterPoseNativeCompositionOperation operation) =>
            EvaluateLayeredBoneBlend(
                in header,
                operation.OutputPoseValueIndex,
                operation.InputPoseValueIndexA,
                operation.InputPoseValueIndexB,
                operation.BoneMaskOffset,
                header.Weight);

        void EvaluateLayeredBoneBlend(
            in CharacterPoseNativeOperationHeader header,
            int output,
            int baseValue,
            int overlayValue,
            int boneMaskOffset,
            float weight)
        {
            if (!m_Context.TryRequireInputs(
                    in header,
                    output,
                    baseValue,
                    overlayValue))
                return;
            if (m_Context.m_ValueAvailability[overlayValue] == AnimationPoseAvailability.NoPose)
            {
                if (!m_Context.TryCopyValue(baseValue, output, header.Index))
                    m_Context.SetInvalid(output, m_Context.m_ValueContinuityIdentities[baseValue], AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid, header.Index);
                return;
            }
            if (m_Context.m_ValueAvailability[baseValue] == AnimationPoseAvailability.NoPose)
            {
                if (!m_Context.TryCopyValue(overlayValue, output, header.Index) ||
                    !m_Context.TryScaleValue(output, weight, boneMaskOffset))
                {
                    m_Context.SetInvalid(output, m_Context.m_ValueContinuityIdentities[overlayValue], AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid, header.Index);
                }
                return;
            }

            m_Context.m_ValueAvailability[output] = AnimationPoseAvailability.Pose;
            m_Context.m_ValueOutputWeights[output] = CharacterPoseExecutionContext.UnionWeight(
                m_Context.m_ValueOutputWeights[baseValue],
                m_Context.m_ValueOutputWeights[overlayValue] * weight);
            m_Context.m_ValueContinuityIdentities[output] = CharacterPoseExecutionContext.CombineContinuity(
                m_Context.m_ValueContinuityIdentities[baseValue],
                m_Context.m_ValueContinuityIdentities[overlayValue],
                header.Index);
            m_Context.m_ValueInvalidReasons[output] = AnimationPoseNativeInvalidReason.None;
            for (int bone = 0; bone < m_Context.m_BoneCount; bone++)
            {
                if (!m_Context.TryGetBoneOutputWeight(overlayValue, bone, out float overlayOutputWeight))
                {
                    m_Context.SetInvalid(output, m_Context.m_ValueContinuityIdentities[output], AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid, header.Index);
                    return;
                }
                float overlay = Mathf.Clamp01(
                    overlayOutputWeight *
                    m_Context.GetMaskWeight(boneMaskOffset, bone) * weight);
                if (!CharacterPoseExecutionContext.TryBlendPose(
                        m_Context.m_ValueDenseLocalPoses[m_Context.PoseOffset(baseValue) + bone],
                        m_Context.m_ValueDenseLocalPoses[m_Context.PoseOffset(overlayValue) + bone],
                        overlay,
                        out AnimationLocalBonePose pose))
                {
                    m_Context.SetInvalid(output, m_Context.m_ValueContinuityIdentities[output], AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid, header.Index);
                    return;
                }
                m_Context.m_ValueDenseLocalPoses[m_Context.PoseOffset(output) + bone] = pose;
            }
            if (!m_Context.TryCopyParameters(baseValue, output) ||
                !m_Context.TryMergeContributions(weight, boneMaskOffset, baseValue, overlayValue, output, false) ||
                !m_Context.TryResolveFootFeatures(weight, boneMaskOffset, baseValue, overlayValue, output, false))
            {
                m_Context.SetInvalid(output, m_Context.m_ValueContinuityIdentities[output], AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid, header.Index);
            }
        }

        internal void EvaluateAdditivePose(
            in CharacterPoseNativeOperationHeader header,
            in CharacterPoseNativeCompositionOperation operation)
        {
            int output = operation.OutputPoseValueIndex;
            int baseValue = operation.InputPoseValueIndexA;
            int additiveValue = operation.InputPoseValueIndexB;
            if (!m_Context.TryRequireInputs(
                    in header,
                    output,
                    baseValue,
                    additiveValue))
                return;
            if (m_Context.m_ValueAvailability[additiveValue] == AnimationPoseAvailability.NoPose)
            {
                if (!m_Context.TryCopyValue(baseValue, output, header.Index))
                    m_Context.SetInvalid(output, m_Context.m_ValueContinuityIdentities[baseValue], AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid, header.Index);
                return;
            }
            if (m_Context.m_ValueAvailability[baseValue] != AnimationPoseAvailability.Pose)
            {
                m_Context.SetInvalid(
                    output,
                    CharacterPoseExecutionContext.CombineContinuity(
                        m_Context.m_ValueContinuityIdentities[baseValue],
                        m_Context.m_ValueContinuityIdentities[additiveValue],
                        header.Index),
                    AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid,
                    header.Index);
                return;
            }

            m_Context.m_ValueAvailability[output] = AnimationPoseAvailability.Pose;
            m_Context.m_ValueOutputWeights[output] = CharacterPoseExecutionContext.UnionWeight(
                m_Context.m_ValueOutputWeights[baseValue],
                m_Context.m_ValueOutputWeights[additiveValue] * header.Weight);
            m_Context.m_ValueContinuityIdentities[output] = CharacterPoseExecutionContext.CombineContinuity(
                m_Context.m_ValueContinuityIdentities[baseValue],
                m_Context.m_ValueContinuityIdentities[additiveValue],
                header.Index);
            m_Context.m_ValueInvalidReasons[output] = AnimationPoseNativeInvalidReason.None;
            for (int bone = 0; bone < m_Context.m_BoneCount; bone++)
            {
                if (!m_Context.TryGetBoneOutputWeight(additiveValue, bone, out float additiveOutputWeight))
                {
                    m_Context.SetInvalid(output, m_Context.m_ValueContinuityIdentities[output], AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid, header.Index);
                    return;
                }
                float weight = Mathf.Clamp01(
                    additiveOutputWeight *
                    m_Context.GetMaskWeight(operation.BoneMaskOffset, bone) *
                    header.Weight);
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
                    m_Context.SetInvalid(output, m_Context.m_ValueContinuityIdentities[output], AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid, header.Index);
                    return;
                }
            }
            if (!m_Context.TryCopyParameters(baseValue, output) ||
                !m_Context.TryMergeContributions(header.Weight, operation.BoneMaskOffset, baseValue, additiveValue, output, true) ||
                !m_Context.TryResolveFootFeatures(header.Weight, operation.BoneMaskOffset, baseValue, additiveValue, output, true))
            {
                m_Context.SetInvalid(output, m_Context.m_ValueContinuityIdentities[output], AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid, header.Index);
            }
        }

        internal void EvaluatePoseParameterResolve(
            in CharacterPoseNativeOperationHeader header,
            in CharacterPoseNativeParameterResolveOperation operation)
        {
            int output = operation.OutputPoseValueIndex;
            int baseValue = operation.InputPoseValueIndexA;
            int parameterSourceValue = operation.InputPoseValueIndexB;
            if (!m_Context.TryRequireInputs(
                    in header,
                    output,
                    baseValue,
                    parameterSourceValue))
                return;
            if (!m_Context.TryCopyValue(baseValue, output, header.Index))
            {
                m_Context.SetInvalid(output, m_Context.m_ValueContinuityIdentities[baseValue], AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid, header.Index);
                return;
            }
            if (m_Context.m_ValueAvailability[parameterSourceValue] == AnimationPoseAvailability.NoPose)
                return;
            if (!m_Context.TryResolveParameters(
                    header.Weight,
                    operation.ParameterPolicyOffset,
                    baseValue,
                    parameterSourceValue,
                    output))
            {
                m_Context.SetInvalid(output, m_Context.m_ValueContinuityIdentities[output], AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid, header.Index);
                return;
            }
            m_Context.m_ValueContinuityIdentities[output] = CharacterPoseExecutionContext.CombineContinuity(
                m_Context.m_ValueContinuityIdentities[baseValue],
                m_Context.m_ValueContinuityIdentities[parameterSourceValue],
                header.Index);
        }

    }
}
