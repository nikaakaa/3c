using System;
using ThirdPersonCharacter.Pipeline.Animation.Presentation;
using ThirdPersonCharacter.Pipeline.Presentation;
using Unity.Collections;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal static class CharacterPoseProgramExecutorConfiguration
    {
        internal static void Require(
            CharacterPoseProgramExecutionView program,
            CharacterPoseProgramFramePages framePages,
            in CharacterPoseProgramTuningView tuning,
            PoseInertializationNativeProgram inertializationProgram,
            CharacterPoseGraphNativeBinding binding,
            in CharacterFinalPosePublicationOutputBinding finalOutput,
            CharacterPoseConstraintRuntime poseConstraints)
        {
            if (program == null)
                throw new ArgumentNullException(nameof(program));
            if (framePages == null)
                throw new ArgumentNullException(nameof(framePages));
            program.RequireValid();
            framePages.RequireValid();
            if (framePages.RootOrientationWarpControls.Length !=
                    program.RootOrientationWarps.Length ||
                framePages.LinkedPoseCallControls.Length !=
                    program.LinkedPoseCalls.Length ||
                framePages.LinkedPoseActiveFragments.Length !=
                    program.LinkedPoseFragmentCount)
            {
                throw new ArgumentException(
                    "Pose Program frame pages do not match the Execution View.",
                    nameof(framePages));
            }
            NativeArray<float> operationWeights = tuning.OperationWeights;
            if (!tuning.IsValid || !operationWeights.IsCreated ||
                operationWeights.Length != program.OperationHeaders.Length)
            {
                throw new ArgumentException(
                    "Pose Program tuning view is invalid.",
                    nameof(tuning));
            }
            if (poseConstraints == null || program.FullBodyIkCount != 1 ||
                !poseConstraints.MatchesCompiledLayout(
                    program.FullBodyIkGoalContributionCount,
                    program.FullBodyIkContributionGoalCount))
            {
                throw new ArgumentException(
                    "FinalIK Full Body solver layout is invalid.",
                    nameof(poseConstraints));
            }
            if (inertializationProgram == null ||
                inertializationProgram.BoneCount != program.PoseBoneCount ||
                inertializationProgram.ParameterCount != program.ParameterCount ||
                inertializationProgram.ResetRequests.Length !=
                    inertializationProgram.Nodes.Length)
            {
                throw new ArgumentException(
                    "Pose Inertialization Native Program is invalid.",
                    nameof(inertializationProgram));
            }
            binding.RequireValid();
            if (!finalOutput.IsValid ||
                finalOutput.CompletionIdentity != binding.CompletionIdentity ||
                finalOutput.Layout.OutputOperationIndex !=
                    program.OutputOperationIndex ||
                finalOutput.Layout.OutputValueIndex != program.OutputValueIndex)
            {
                throw new ArgumentException(
                    "Final Pose Publication output binding is invalid.",
                    nameof(finalOutput));
            }
            AnimationPoseNativeAggregateLayout layout = binding.Layout;
            if (layout.BoneCount != program.PoseBoneCount ||
                layout.ParameterCount != program.ParameterCount ||
                layout.PoseValueCount != program.PoseValueCount ||
                layout.PoseValueContributionStride != program.ContributionStride ||
                layout.OperationCount != program.FrameCacheCount ||
                layout.FrameCacheCount != program.FrameCacheCount ||
                layout.StageCount != program.Stages.Length ||
                layout.OutputValueIndex != program.OutputValueIndex ||
                program.OutputOperationIndex < 0 ||
                program.OutputOperationIndex >= program.FrameCacheCount ||
                program.OutputNativeOperationIndex < 0 ||
                program.OutputNativeOperationIndex >=
                    program.OperationHeaders.Length ||
                program.LeftFootBoneIndex < 0 ||
                program.LeftFootBoneIndex >= program.PoseBoneCount ||
                program.RightFootBoneIndex < 0 ||
                program.RightFootBoneIndex >= program.PoseBoneCount)
            {
                throw new ArgumentException(
                    "Animation Pose Graph Native Job layout is invalid.",
                    nameof(binding));
            }
            for (int bone = 0; bone < program.PoseBoneCount; bone++)
            {
                int parentIndex = program.ParentIndices[bone];
                if (parentIndex < -1 || parentIndex >= bone)
                {
                    throw new ArgumentException(
                        $"Animation Pose Graph Native Job parent #{bone} is invalid.",
                        nameof(program));
                }
            }
            for (int parameter = 0; parameter < program.ParameterCount; parameter++)
            {
                if (!float.IsFinite(program.ParameterDefaults[parameter]))
                {
                    throw new ArgumentException(
                        $"Animation Pose Graph Native Job parameter #{parameter} is invalid.",
                        nameof(program));
                }
            }
            int nativeOperationStart = 0;
            for (int stageIndex = 0; stageIndex < program.Stages.Length; stageIndex++)
            {
                AnimationPoseGraphNativeStage stage = program.Stages[stageIndex];
                if (stage.Index != stageIndex ||
                    stage.OperationStart != nativeOperationStart ||
                    stage.OperationCount < 0 ||
                    stage.OperationStart >
                        program.OperationHeaders.Length - stage.OperationCount ||
                    stage.CompletionIndex != stageIndex ||
                    stage.DiagnosticIndex != stageIndex)
                {
                    throw new ArgumentException(
                        $"Animation Pose Graph Native Job stage #{stageIndex} is invalid.",
                        nameof(program));
                }
                nativeOperationStart += stage.OperationCount;
            }
            if (nativeOperationStart != program.OperationHeaders.Length)
            {
                throw new ArgumentException(
                    "Animation Pose Graph Native Job stages are incomplete.",
                    nameof(program));
            }
            int outputCount = 0;
            for (int i = 0; i < program.OperationHeaders.Length; i++)
            {
                CharacterPoseNativeOperationHeader header =
                    program.OperationHeaders[i];
                if (header.Index < 0 || header.Index >= program.FrameCacheCount ||
                    header.FrameCacheIndex != header.Index ||
                    header.OutputPoseValueIndex < -1 ||
                    header.OutputPoseValueIndex >= program.PoseValueCount ||
                    header.LinkedPoseFragmentIndex < -1 ||
                    header.LinkedPoseFragmentIndex >=
                        framePages.LinkedPoseActiveFragments.Length ||
                    CharacterPoseOperationFamilies.RequireFamily(header.Code) !=
                        header.Family ||
                    !float.IsFinite(operationWeights[i]) ||
                    operationWeights[i] < 0f || operationWeights[i] > 1f ||
                    !RequireFamilyOperation(
                        program,
                        framePages,
                        inertializationProgram,
                        poseConstraints,
                        in layout,
                        in header))
                {
                    throw new ArgumentException(
                        $"Animation Pose Graph Native Job operation #{i} is invalid.",
                        nameof(program));
                }
                if (header.Code != CharacterPoseOperationCode.OutputPose)
                    continue;
                outputCount++;
                if (i != program.OutputNativeOperationIndex ||
                    header.Index != program.OutputOperationIndex ||
                    header.OutputPoseValueIndex != program.OutputValueIndex)
                {
                    throw new ArgumentException(
                        "Animation Pose Graph Native Job output identity is invalid.",
                        nameof(program));
                }
            }
            if (outputCount != 1)
            {
                throw new ArgumentException(
                    "Animation Pose Graph Native Job requires one output operation.",
                    nameof(program));
            }
        }

        static bool RequireFamilyOperation(
            CharacterPoseProgramExecutionView program,
            CharacterPoseProgramFramePages framePages,
            PoseInertializationNativeProgram inertializationProgram,
            CharacterPoseConstraintRuntime poseConstraints,
            in AnimationPoseNativeAggregateLayout layout,
            in CharacterPoseNativeOperationHeader header)
        {
            int index = header.FamilyPayloadIndex;
            switch (header.Family)
            {
                case CharacterPoseOperationFamily.ParameterResolve:
                    if (!HasIndex(program.ParameterResolveOperations, index))
                        return false;
                    CharacterPoseNativeParameterResolveOperation resolve =
                        program.ParameterResolveOperations[index];
                    return HasPoseInputs(
                            in header,
                            resolve.InputPoseValueIndexA,
                            resolve.InputPoseValueIndexB,
                            program.PoseValueCount) &&
                        resolve.OutputPoseValueIndex == header.OutputPoseValueIndex &&
                        HasSpan(
                            program.ParameterPolicies,
                            resolve.ParameterPolicyOffset,
                            program.ParameterCount);
                case CharacterPoseOperationFamily.Player:
                    if (!HasIndex(program.PlayerOperations, index))
                        return false;
                    CharacterPoseNativePlayerOperation player =
                        program.PlayerOperations[index];
                    return player.OutputPoseValueIndex == header.OutputPoseValueIndex &&
                        (uint)player.PlayerIndex < (uint)layout.PlayerCount;
                case CharacterPoseOperationFamily.StateMachine:
                    if (!HasIndex(program.StateMachineOperations, index))
                        return false;
                    CharacterPoseNativeStateMachineOperation state =
                        program.StateMachineOperations[index];
                    return state.OutputPoseValueIndex == header.OutputPoseValueIndex &&
                        (header.Code == CharacterPoseOperationCode.StatePoseOutput
                            ? IsPoseInput(
                                state.InputPoseValueIndex,
                                header.OutputPoseValueIndex,
                                program.PoseValueCount)
                            : (uint)state.StateMachineIndex <
                                (uint)framePages.StateMachineControls.Length);
                case CharacterPoseOperationFamily.AnimationSlot:
                    if (!HasIndex(program.AnimationSlotOperations, index))
                        return false;
                    CharacterPoseNativeAnimationSlotOperation slot =
                        program.AnimationSlotOperations[index];
                    return slot.OutputPoseValueIndex == header.OutputPoseValueIndex &&
                        IsPoseInput(
                            slot.InputPoseValueIndex,
                            header.OutputPoseValueIndex,
                            program.PoseValueCount) &&
                        (uint)slot.PlayerIndex < (uint)layout.PlayerCount &&
                        (uint)slot.AnimationSlotIndex <
                            (uint)framePages.AnimationSlotControls.Length &&
                        inertializationProgram.SlotNodeOffset +
                            slot.AnimationSlotIndex <
                            inertializationProgram.Nodes.Length;
                case CharacterPoseOperationFamily.Blend:
                    if (!HasIndex(program.BlendOperations, index))
                        return false;
                    CharacterPoseNativeBlendOperation blend =
                        program.BlendOperations[index];
                    return blend.OutputPoseValueIndex == header.OutputPoseValueIndex &&
                        (header.Code == CharacterPoseOperationCode.BlendStack
                            ? (uint)blend.PlayerIndex < (uint)layout.PlayerCount
                            : HasPoseInputs(
                                in header,
                                blend.InputPoseValueIndexA,
                                blend.InputPoseValueIndexB,
                                program.PoseValueCount) &&
                              blend.ParameterIndex < program.ParameterCount);
                case CharacterPoseOperationFamily.Inertialization:
                    if (!HasIndex(program.InertializationOperations, index))
                        return false;
                    CharacterPoseNativeInertializationOperation inertial =
                        program.InertializationOperations[index];
                    return inertial.OutputPoseValueIndex == header.OutputPoseValueIndex &&
                        IsPoseInput(
                            inertial.InputPoseValueIndex,
                            header.OutputPoseValueIndex,
                            program.PoseValueCount) &&
                        (uint)inertial.InertializationIndex <
                            (uint)inertializationProgram.Nodes.Length;
                case CharacterPoseOperationFamily.Composition:
                    if (!HasIndex(program.CompositionOperations, index))
                        return false;
                    CharacterPoseNativeCompositionOperation composition =
                        program.CompositionOperations[index];
                    return composition.OutputPoseValueIndex == header.OutputPoseValueIndex &&
                        HasPoseInputs(
                            in header,
                            composition.InputPoseValueIndexA,
                            composition.InputPoseValueIndexB,
                            program.PoseValueCount) &&
                        HasSpan(
                            program.DenseBoneMasks,
                            composition.BoneMaskOffset,
                            program.PoseBoneCount) &&
                        (header.Code == CharacterPoseOperationCode.LayeredBoneBlend ||
                         HasSpan(
                             program.AdditiveReferences,
                             composition.AdditiveReferenceOffset,
                             program.PoseBoneCount));
                case CharacterPoseOperationFamily.SpaceConversion:
                    if (!HasIndex(program.SpaceConversionOperations, index))
                        return false;
                    CharacterPoseNativeSpaceConversionOperation space =
                        program.SpaceConversionOperations[index];
                    return space.OutputPoseValueIndex == header.OutputPoseValueIndex &&
                        IsPoseInput(
                            space.InputPoseValueIndex,
                            header.OutputPoseValueIndex,
                            program.PoseValueCount);
                case CharacterPoseOperationFamily.ComponentControl:
                    if (!HasIndex(program.ComponentControlOperations, index))
                        return false;
                    CharacterPoseNativeComponentControlOperation component =
                        program.ComponentControlOperations[index];
                    return component.OutputPoseValueIndex == header.OutputPoseValueIndex &&
                        IsPoseInput(
                            component.InputPoseValueIndex,
                            header.OutputPoseValueIndex,
                            program.PoseValueCount) &&
                        (header.Code == CharacterPoseOperationCode.ModifyBone
                            ? (uint)component.ModifyBoneIndex <
                                (uint)program.ModifyBones.Length
                            : (uint)component.RootOrientationWarpIndex <
                                (uint)program.RootOrientationWarps.Length);
                case CharacterPoseOperationFamily.GoalContribution:
                    if (!HasIndex(program.GoalContributionOperations, index) ||
                        header.OutputPoseValueIndex != -1)
                        return false;
                    CharacterPoseNativeGoalContributionOperation goal =
                        program.GoalContributionOperations[index];
                    return (uint)goal.InputPoseValueIndex <
                            (uint)program.PoseValueCount &&
                        (header.Code == CharacterPoseOperationCode.PoseBoneIKGoals
                            ? goal.PoseBoneContribution.IsValid
                            : goal.FootPlacement.IsValid &&
                              goal.FootPlacement.FootPlacementIndex <
                                program.FootPlacementCount);
                case CharacterPoseOperationFamily.GoalAssembler:
                    return header.OutputPoseValueIndex == -1 &&
                        HasIndex(program.GoalAssemblerOperations, index) &&
                        program.GoalAssemblers.Contains(
                            program.GoalAssemblerOperations[index].Handle);
                case CharacterPoseOperationFamily.FullBodyIk:
                    return HasIndex(program.FullBodyIkOperations, index) &&
                        program.ContainsFullBodyIkConstraint(
                            program.FullBodyIkOperations[index].Handle) &&
                        program.FullBodyIkOperations[index].Handle
                            .OutputPoseValueIndex == header.OutputPoseValueIndex &&
                        poseConstraints.IsFullBodyIkPrepared;
                case CharacterPoseOperationFamily.LinkedPose:
                    if (!HasIndex(program.LinkedPoseOperations, index) ||
                        header.LinkedPoseFragmentIndex != -1)
                        return false;
                    CharacterPoseNativeLinkedPoseOperation linked =
                        program.LinkedPoseOperations[index];
                    return linked.OutputPoseValueIndex == header.OutputPoseValueIndex &&
                        (uint)linked.LinkedPoseCallIndex <
                            (uint)program.LinkedPoseCalls.Length;
                case CharacterPoseOperationFamily.Output:
                    return HasIndex(program.OutputOperations, index) &&
                        IsPoseInput(
                            program.OutputOperations[index].InputPoseValueIndex,
                            header.OutputPoseValueIndex,
                            program.PoseValueCount);
                default:
                    return false;
            }
        }

        static bool HasPoseInputs(
            in CharacterPoseNativeOperationHeader header,
            int inputA,
            int inputB,
            int poseValueCount) =>
            IsPoseInput(inputA, header.OutputPoseValueIndex, poseValueCount) &&
            IsPoseInput(inputB, header.OutputPoseValueIndex, poseValueCount);

        static bool IsPoseInput(
            int input,
            int output,
            int poseValueCount) =>
            input >= 0 && input < output && output < poseValueCount;

        static bool HasIndex<T>(NativeArray<T> values, int index)
            where T : struct => (uint)index < (uint)values.Length;

        static bool HasSpan<T>(NativeArray<T> values, int offset, int count)
            where T : struct =>
            offset >= 0 && count > 0 && offset <= values.Length - count;
    }
}
