using System;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal sealed class CharacterPoseTransformOperationModule
    {
        readonly CharacterPoseExecutionContext m_Context;

        internal CharacterPoseTransformOperationModule(
            CharacterPoseExecutionContext context)
        {
            m_Context = context ??
                throw new ArgumentNullException(nameof(context));
        }

        internal void EvaluateModifyBone(AnimationPoseGraphNativeOperation operation)
        {
            int input = operation.InputValueIndexA;
            int output = operation.OutputValueIndex;
            if (!m_Context.IsInputReady(input, operation.Index) ||
                (uint)operation.ModifyBoneIndex >= (uint)m_Context.m_ModifyBones.Length ||
                !m_Context.TryCopyValue(input, output, operation.Index))
            {
                m_Context.SetInvalid(output, (ulong)operation.Index + 1UL, AnimationPoseNativeInvalidReason.PoseGraphInputIncomplete, operation.Index);
                return;
            }
            if (m_Context.m_ValueAvailability[output] != AnimationPoseAvailability.Pose)
                return;
            AnimationPoseGraphNativeModifyBone modify = m_Context.m_ModifyBones[operation.ModifyBoneIndex];
            int inputOffset = m_Context.PoseOffset(input);
            int outputOffset = m_Context.PoseOffset(output);
            AnimationLocalBonePose current = m_Context.m_ValueDenseLocalPoses[outputOffset + modify.BoneIndex];
            CharacterComponentBonePose parentComponent = default;
            if (modify.ReferenceSpace == ModifyBoneReferenceSpace.Local && modify.ParentBoneIndex >= 0)
            {
                parentComponent = AsComponent(m_Context.m_ValueDenseLocalPoses[outputOffset + modify.ParentBoneIndex]);
                if (!CharacterPoseConstraintMath.TryCreateLocal(AsComponent(current), parentComponent, out current))
                {
                    m_Context.SetInvalid(output, m_Context.m_ValueContinuityIdentities[output], AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid, operation.Index);
                    return;
                }
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
            if (modify.ReferenceSpace == ModifyBoneReferenceSpace.Local && modify.ParentBoneIndex >= 0)
            {
                if (!CharacterPoseConstraintMath.TryCreateComponent(
                        modified,
                        parentComponent,
                        out CharacterComponentBonePose component))
                {
                    m_Context.SetInvalid(output, m_Context.m_ValueContinuityIdentities[output], AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid, operation.Index);
                    return;
                }
                modified = ToPose(component);
            }
            m_Context.m_ValueDenseLocalPoses[outputOffset + modify.BoneIndex] = modified;
            if (!RebuildComponentDescendants(inputOffset, outputOffset, modify.BoneIndex))
                m_Context.SetInvalid(output, m_Context.m_ValueContinuityIdentities[output], AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid, operation.Index);
        }

        internal void EvaluateRootOrientationWarp(
            AnimationPoseGraphNativeOperation operation)
        {
            int input = operation.InputValueIndexA;
            int output = operation.OutputValueIndex;
            if (!m_Context.IsInputReady(input, operation.Index) ||
                (uint)operation.RootOrientationWarpIndex >=
                (uint)m_Context.m_RootOrientationWarps.Length ||
                (uint)operation.RootOrientationWarpIndex >=
                (uint)m_Context.m_RootOrientationWarpControls.Length ||
                !m_Context.TryCopyValue(input, output, operation.Index))
            {
                m_Context.SetInvalid(output, (ulong)operation.Index + 1UL,
                    AnimationPoseNativeInvalidReason.PoseGraphInputIncomplete,
                    operation.Index);
                return;
            }
            if (m_Context.m_ValueAvailability[output] !=
                AnimationPoseAvailability.Pose)
                return;
            CharacterRootOrientationWarpNativeControl control =
                m_Context.m_RootOrientationWarpControls[
                    operation.RootOrientationWarpIndex];
            if (!control.IsValid)
            {
                m_Context.SetInvalid(output,
                    m_Context.m_ValueContinuityIdentities[output],
                    AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid,
                    operation.Index);
                return;
            }
            if (control.Active == 0 ||
                Math.Abs(control.YawOffsetDegrees) <= 0.0001f)
                return;
            int root = m_Context.m_RootOrientationWarps[
                operation.RootOrientationWarpIndex]
                .RootPhysicalBoneIndex;
            if ((uint)root >= (uint)m_Context.m_BoneCount)
            {
                m_Context.SetInvalid(output,
                    m_Context.m_ValueContinuityIdentities[output],
                    AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid,
                    operation.Index);
                return;
            }
            int offset = m_Context.PoseOffset(output) + root;
            AnimationLocalBonePose current =
                m_Context.m_ValueDenseLocalPoses[offset];
            m_Context.m_ValueDenseLocalPoses[offset] =
                new AnimationLocalBonePose(
                    current.Position,
                    Quaternion.AngleAxis(
                        control.YawOffsetDegrees,
                        Vector3.up) * current.Rotation,
                    current.Scale);
        }

        bool RebuildComponentDescendants(int inputOffset, int outputOffset, int rootIndex)
        {
            for (int bone = rootIndex + 1; bone < m_Context.m_BoneCount; bone++)
            {
                if (!IsDescendant(bone, rootIndex))
                    continue;
                int parent = m_Context.m_ParentIndices[bone];
                CharacterComponentBonePose inputBone = AsComponent(m_Context.m_ValueDenseLocalPoses[inputOffset + bone]);
                CharacterComponentBonePose inputParent = AsComponent(m_Context.m_ValueDenseLocalPoses[inputOffset + parent]);
                CharacterComponentBonePose outputParent = AsComponent(m_Context.m_ValueDenseLocalPoses[outputOffset + parent]);
                if (!CharacterPoseConstraintMath.TryCreateLocal(inputBone, inputParent, out AnimationLocalBonePose local) ||
                    !CharacterPoseConstraintMath.TryCreateComponent(local, outputParent, out CharacterComponentBonePose rebuilt))
                    return false;
                m_Context.m_ValueDenseLocalPoses[outputOffset + bone] = ToPose(rebuilt);
            }
            return true;
        }

        bool IsDescendant(int bone, int ancestor)
        {
            int cursor = bone;
            while (cursor >= 0)
            {
                if (cursor == ancestor)
                    return true;
                cursor = m_Context.m_ParentIndices[cursor];
            }
            return false;
        }

        static CharacterComponentBonePose AsComponent(AnimationLocalBonePose value) =>
            new CharacterComponentBonePose(value.Position, value.Rotation, value.Scale);

        static AnimationLocalBonePose ToPose(CharacterComponentBonePose value) =>
            new AnimationLocalBonePose(value.Position, value.Rotation, value.Scale);

        internal void EvaluateLocalToComponentPose(
            AnimationPoseGraphNativeOperation operation)
        {
            int input = operation.InputValueIndexA;
            int output = operation.OutputValueIndex;
            if (!m_Context.IsInputReady(input, operation.Index) ||
                !m_Context.TryCopyValueWithoutPose(
                    input,
                    output,
                    operation.Index))
            {
                m_Context.SetInvalid(output, (ulong)operation.Index + 1UL,
                    AnimationPoseNativeInvalidReason.PoseGraphInputIncomplete,
                    operation.Index);
                return;
            }
            if (m_Context.m_ValueAvailability[output] != AnimationPoseAvailability.Pose)
                return;
            for (int bone = 0; bone < m_Context.m_BoneCount; bone++)
            {
                int offset = m_Context.PoseOffset(output) + bone;
                int parent = m_Context.m_ParentIndices[bone];
                AnimationLocalBonePose local =
                    m_Context.m_ValueDenseLocalPoses[m_Context.PoseOffset(input) + bone];
                if (parent >= 0 &&
                    !CharacterPoseExecutionContext.TryToModel(
                        m_Context.m_ValueDenseLocalPoses[m_Context.PoseOffset(output) + parent],
                        local,
                        out local))
                {
                    m_Context.SetInvalid(output, m_Context.m_ValueContinuityIdentities[output],
                        AnimationPoseNativeInvalidReason.PoseSpaceConversionInvalid,
                        operation.Index);
                    return;
                }
                m_Context.m_ValueDenseLocalPoses[offset] = local;
            }
        }

        internal void EvaluateComponentToLocalPose(
            AnimationPoseGraphNativeOperation operation)
        {
            int input = operation.InputValueIndexA;
            int output = operation.OutputValueIndex;
            if (!m_Context.IsInputReady(input, operation.Index) ||
                !m_Context.TryCopyValueWithoutPose(
                    input,
                    output,
                    operation.Index))
            {
                m_Context.SetInvalid(output, (ulong)operation.Index + 1UL,
                    AnimationPoseNativeInvalidReason.PoseGraphInputIncomplete,
                    operation.Index);
                return;
            }
            if (m_Context.m_ValueAvailability[output] != AnimationPoseAvailability.Pose)
                return;
            for (int bone = 0; bone < m_Context.m_BoneCount; bone++)
            {
                int parent = m_Context.m_ParentIndices[bone];
                AnimationLocalBonePose component =
                    m_Context.m_ValueDenseLocalPoses[m_Context.PoseOffset(input) + bone];
                if (parent >= 0 &&
                    !CharacterPoseExecutionContext.TryToLocal(
                        m_Context.m_ValueDenseLocalPoses[m_Context.PoseOffset(input) + parent],
                        component,
                        out component))
                {
                    m_Context.SetInvalid(output, m_Context.m_ValueContinuityIdentities[output],
                        AnimationPoseNativeInvalidReason.PoseSpaceConversionInvalid,
                        operation.Index);
                    return;
                }
                m_Context.m_ValueDenseLocalPoses[m_Context.PoseOffset(output) + bone] = component;
            }
        }

    }
}
