using System;
using ThirdPersonCharacter.Pipeline.Animation.Presentation;
using ThirdPersonCharacter.Pipeline.Presentation;
using Unity.Collections;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal sealed class CharacterPoseConstraintOperationModule
    {
        readonly CharacterPoseConstraintRuntime m_Runtime;
        CharacterPoseValuePageSlice m_Values;
        CharacterPoseGraphNativeBinding m_Frame;
        ulong m_FrameSequence;

        internal CharacterPoseConstraintOperationModule(
            CharacterPoseConstraintRuntime runtime)
        {
            m_Runtime = runtime ??
                throw new ArgumentNullException(nameof(runtime));
        }

        internal void BindFrame(
            in CharacterPoseValuePageSlice values,
            CharacterPoseGraphNativeBinding frame)
        {
            if (!values.IsValid)
                throw new ArgumentException("Pose Constraint frame binding is invalid.");
            frame.RequireValid();
            m_Values = values;
            m_Frame = frame;
            m_FrameSequence = 0;
        }

        internal void BeginEvaluation(ulong frameSequence)
        {
            if (frameSequence == 0)
                throw new ArgumentOutOfRangeException(nameof(frameSequence));
            m_FrameSequence = frameSequence;
        }

        internal bool EvaluatePoseBoneIkGoals(
            in CharacterPoseNativeOperationHeader header,
            in CharacterPoseNativeGoalContributionOperation operation)
        {
            CharacterPoseBoneContributionConstraintHandle handle =
                operation.PoseBoneContribution;
            int input = handle.InputPoseValueIndex;
            if (!handle.IsValid ||
                !m_Values.IsInputReady(input, header.Index) ||
                m_Values.Availability(input) != AnimationPoseAvailability.Pose)
            {
                return false;
            }
            NativeSlice<AnimationLocalBonePose> componentPose = new NativeSlice<AnimationLocalBonePose>(
                m_Frame.ValueDenseLocalPoses,
                input * m_Values.BoneCount,
                m_Values.BoneCount);
            CharacterPoseBoneContributionOperationResult result =
                m_Runtime.ExecutePoseBoneContribution(
                    in handle,
                    componentPose,
                    m_FrameSequence,
                    m_Values.CompletionIdentity);
            return result.Matches(
                in handle,
                m_FrameSequence,
                m_Values.CompletionIdentity);
        }

        internal bool EvaluateWorldAwareFootGoal(
            in CharacterPoseNativeOperationHeader header,
            in CharacterPoseNativeGoalContributionOperation operation,
            in CharacterPoseWorldFrameInput worldInput,
            out AnimationPoseNativeInvalidReason invalidReason)
        {
            invalidReason =
                AnimationPoseNativeInvalidReason.FootPlacementInvalid;
            CharacterFootPlacementConstraintHandle handle =
                operation.FootPlacement;
            if (!handle.IsValid ||
                !worldInput.IsValid ||
                worldInput.CompletionIdentity != m_Values.CompletionIdentity ||
                worldInput.PresentationFrame != m_FrameSequence)
            {
                return false;
            }
            CharacterFootPlacementConstraintOperationResult result;
            if (m_Runtime.HasFootPlacement)
            {
                var inputBinding = new AnimationPoseValueNativeReadBinding(
                    in m_Frame,
                    operation.InputPoseValueIndex);
                CharacterFootPlacementFrameInput footPlacement =
                    worldInput.BuildFootPlacement(
                        in inputBinding,
                        operation.ParameterIndex);
                result = m_Runtime.EvaluateFootPlacement(
                    in handle,
                    in footPlacement);
            }
            else
            {
                result = m_Runtime.RecordUnavailableFootPlacement(
                    in handle,
                    m_FrameSequence,
                    m_Values.CompletionIdentity);
            }
            if (!result.Matches(
                    in handle,
                    m_FrameSequence,
                    m_Values.CompletionIdentity))
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

        internal bool EvaluateGoalAssembler(
            in CharacterPoseNativeGoalAssemblerOperation operation)
        {
            CharacterFullBodyIkGoalAssemblerConstraintHandle handle =
                operation.Handle;
            if (!handle.IsValid)
                return false;
            CharacterFullBodyIkGoalAssemblerOperationResult result =
                m_Runtime.ExecuteGoalAssembler(
                    in handle,
                    m_FrameSequence,
                    m_Values.CompletionIdentity);
            return result.Matches(
                in handle,
                m_FrameSequence,
                m_Values.CompletionIdentity);
        }

        internal void EvaluateFullBodyIk(
            in CharacterPoseNativeOperationHeader header,
            in CharacterPoseNativeFullBodyIkOperation operation)
        {
            CharacterFullBodyIkConstraintHandle handle =
                operation.Handle;
            int input = handle.InputPoseValueIndex;
            int output = handle.OutputPoseValueIndex;
            if (!handle.IsValid ||
                !m_Values.IsInputReady(input, header.Index) ||
                !m_Runtime.HasPendingAssembledGoalSet ||
                !m_Values.TryCopyValue(
                    input,
                    output,
                    header.Index))
            {
                m_Values.SetInvalid(output, (ulong)header.Index + 1UL,
                    AnimationPoseNativeInvalidReason.PoseGraphInputIncomplete,
                    header.Index);
                return;
            }
            if (m_Values.Availability(output) != AnimationPoseAvailability.Pose)
                return;
            NativeSlice<AnimationLocalBonePose> outputPose = new NativeSlice<AnimationLocalBonePose>(
                m_Frame.ValueDenseLocalPoses,
                output * m_Values.BoneCount,
                m_Values.BoneCount);
            CharacterFullBodyIkConstraintOperationResult result =
                m_Runtime.ExecuteFullBodyIk(
                    in handle,
                    outputPose,
                    m_FrameSequence,
                    m_Values.CompletionIdentity);
            if (!result.Matches(
                    in handle,
                    m_FrameSequence,
                    m_Values.CompletionIdentity))
            {
                m_Values.SetInvalid(output, m_Values.Continuity(output),
                    AnimationPoseNativeInvalidReason.FullBodyIkSolverInvalid,
                    header.Index);
            }
        }

    }
}
