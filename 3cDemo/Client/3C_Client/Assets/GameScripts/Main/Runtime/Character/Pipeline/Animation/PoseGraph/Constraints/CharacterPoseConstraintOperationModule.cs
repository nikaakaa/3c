using System;
using ThirdPersonCharacter.Pipeline.Presentation;
using Unity.Collections;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal sealed class CharacterPoseConstraintOperationModule
    {
        readonly CharacterPoseExecutionContext m_Context;

        internal CharacterPoseConstraintOperationModule(
            CharacterPoseExecutionContext context)
        {
            m_Context = context ??
                throw new ArgumentNullException(nameof(context));
        }

        internal bool EvaluatePoseBoneIkGoals(AnimationPoseGraphNativeOperation operation)
        {
            CharacterPoseBoneContributionConstraintHandle handle =
                operation.PoseBoneContribution;
            int input = handle.InputPoseValueIndex;
            if (!handle.IsValid ||
                !m_Context.IsInputReady(input, operation.Index) ||
                m_Context.m_ValueAvailability[input] != AnimationPoseAvailability.Pose)
            {
                return false;
            }
            NativeSlice<AnimationLocalBonePose> componentPose = new NativeSlice<AnimationLocalBonePose>(
                m_Context.m_ValueDenseLocalPoses,
                m_Context.PoseOffset(input),
                m_Context.m_BoneCount);
            CharacterPoseBoneContributionOperationResult result =
                m_Context.m_PoseConstraints.ExecutePoseBoneContribution(
                    in handle,
                    componentPose,
                    m_Context.m_FrameSequence,
                    m_Context.m_CompletionIdentity);
            return result.Matches(
                in handle,
                m_Context.m_FrameSequence,
                m_Context.m_CompletionIdentity);
        }

        internal bool EvaluateWorldAwareFootGoal(
            AnimationPoseGraphNativeOperation operation,
            in CharacterPoseWorldFrameInput worldInput,
            out AnimationPoseNativeInvalidReason invalidReason)
        {
            invalidReason =
                AnimationPoseNativeInvalidReason.FootPlacementInvalid;
            CharacterFootPlacementConstraintHandle handle =
                operation.FootPlacementConstraint;
            if (!handle.IsValid ||
                !worldInput.IsValid ||
                worldInput.CompletionIdentity != m_Context.m_CompletionIdentity ||
                worldInput.PresentationFrame != m_Context.m_FrameSequence)
            {
                return false;
            }
            CharacterFootPlacementConstraintOperationResult result;
            if (m_Context.m_PoseConstraints.HasFootPlacement)
            {
                AnimationPoseValueNativeReadBinding inputBinding =
                    m_Context.m_FramePages.RequirePoseValueReadBinding(
                        operation.InputValueIndexA,
                        m_Context.m_CompletionIdentity);
                CharacterFootPlacementFrameInput footPlacement =
                    worldInput.BuildFootPlacement(
                        in inputBinding,
                        operation.ParameterIndex);
                result = m_Context.m_PoseConstraints.EvaluateFootPlacement(
                    in handle,
                    in footPlacement);
            }
            else
            {
                result = m_Context.m_PoseConstraints.RecordUnavailableFootPlacement(
                    in handle,
                    m_Context.m_FrameSequence,
                    m_Context.m_CompletionIdentity);
            }
            if (!result.Matches(
                    in handle,
                    m_Context.m_FrameSequence,
                    m_Context.m_CompletionIdentity))
            {
                return false;
            }
            if (result.Availability ==
                CharacterFullBodyIkGoalContributionAvailability.Ready)
            {
                invalidReason = AnimationPoseNativeInvalidReason.None;
                return true;
            }
            if (result.Availability ==
                CharacterFullBodyIkGoalContributionAvailability
                    .WorldContextUnavailable)
            {
                invalidReason =
                    AnimationPoseNativeInvalidReason.WorldContextUnavailable;
            }
            return false;
        }

        internal bool EvaluateGoalAssembler(AnimationPoseGraphNativeOperation operation)
        {
            CharacterFullBodyIkGoalAssemblerConstraintHandle handle =
                operation.GoalAssemblerConstraint;
            if (!handle.IsValid)
                return false;
            CharacterFullBodyIkGoalAssemblerOperationResult result =
                m_Context.m_PoseConstraints.ExecuteGoalAssembler(
                    in handle,
                    m_Context.m_FrameSequence,
                    m_Context.m_CompletionIdentity);
            return result.Matches(
                in handle,
                m_Context.m_FrameSequence,
                m_Context.m_CompletionIdentity);
        }

        internal void EvaluateFullBodyIk(AnimationPoseGraphNativeOperation operation)
        {
            CharacterFullBodyIkConstraintHandle handle =
                operation.FullBodyIkConstraint;
            int input = handle.InputPoseValueIndex;
            int output = handle.OutputPoseValueIndex;
            if (!handle.IsValid ||
                !m_Context.IsInputReady(input, operation.Index) ||
                !m_Context.m_PoseConstraints.HasPendingAssembledGoalSet ||
                !m_Context.TryCopyValue(
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
            NativeSlice<AnimationLocalBonePose> outputPose = new NativeSlice<AnimationLocalBonePose>(
                m_Context.m_ValueDenseLocalPoses,
                m_Context.PoseOffset(output),
                m_Context.m_BoneCount);
            CharacterFullBodyIkConstraintOperationResult result =
                m_Context.m_PoseConstraints.ExecuteFullBodyIk(
                    in handle,
                    outputPose,
                    m_Context.m_FrameSequence,
                    m_Context.m_CompletionIdentity);
            if (!result.Matches(
                    in handle,
                    m_Context.m_FrameSequence,
                    m_Context.m_CompletionIdentity))
            {
                m_Context.SetInvalid(output, m_Context.m_ValueContinuityIdentities[output],
                    AnimationPoseNativeInvalidReason.FullBodyIkSolverInvalid,
                    operation.Index);
            }
        }

    }
}
