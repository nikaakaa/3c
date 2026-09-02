using System;
using ThirdPersonCharacter.Pipeline.Animation.Presentation;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonSimulation;
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
            if (!tuning.IsValid ||
                !operationWeights.IsCreated ||
                operationWeights.Length != program.Operations.Length)
            {
                throw new ArgumentException(
                    "Pose Program tuning view is invalid.",
                    nameof(tuning));
            }
            if (poseConstraints == null ||
                program.FullBodyIkCount != 1 ||
                !poseConstraints.MatchesCompiledLayout(
                    program.FullBodyIkGoalContributionCount,
                    program.FullBodyIkContributionGoalCount))
            {
                throw new ArgumentException("FinalIK Full Body solver layout is invalid.", nameof(poseConstraints));
            }
            if (inertializationProgram == null || inertializationProgram.BoneCount != program.PoseBoneCount ||
                inertializationProgram.ParameterCount != program.ParameterCount ||
                inertializationProgram.ResetRequests.Length != inertializationProgram.Nodes.Length)
                throw new ArgumentException("Pose Inertialization Native Program is invalid.", nameof(inertializationProgram));
            binding.RequireValid();
            if (!finalOutput.IsValid ||
                finalOutput.CompletionIdentity != binding.CompletionIdentity ||
                finalOutput.Layout.OutputOperationIndex !=
                program.OutputOperationIndex ||
                finalOutput.Layout.OutputValueIndex !=
                program.OutputValueIndex)
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
                program.OutputNativeOperationIndex >= program.Operations.Length ||
                program.LeftFootBoneIndex < 0 || program.LeftFootBoneIndex >= program.PoseBoneCount ||
                program.RightFootBoneIndex < 0 || program.RightFootBoneIndex >= program.PoseBoneCount)
            {
                throw new ArgumentException("Animation Pose Graph Native Job layout is invalid.", nameof(binding));
            }

            for (int bone = 0; bone < program.PoseBoneCount; bone++)
            {
                int parentIndex = program.ParentIndices[bone];
                if (parentIndex < -1 || parentIndex >= bone)
                    throw new ArgumentException($"Animation Pose Graph Native Job parent #{bone} is invalid.", nameof(program));
            }
            for (int parameter = 0; parameter < program.ParameterCount; parameter++)
            {
                if (!float.IsFinite(program.ParameterDefaults[parameter]))
                    throw new ArgumentException($"Animation Pose Graph Native Job parameter #{parameter} is invalid.", nameof(program));
            }

            int outputCount = 0;
            int nativeOperationStart = 0;
            for (int stageIndex = 0; stageIndex < program.Stages.Length; stageIndex++)
            {
                AnimationPoseGraphNativeStage stage = program.Stages[stageIndex];
                if (stage.Index != stageIndex || stage.OperationStart != nativeOperationStart ||
                    stage.OperationCount < 0 ||
                    stage.OperationStart > program.Operations.Length - stage.OperationCount ||
                    stage.CompletionIndex != stageIndex || stage.DiagnosticIndex != stageIndex)
                {
                    throw new ArgumentException(
                        $"Animation Pose Graph Native Job stage #{stageIndex} is invalid.", nameof(program));
                }
                nativeOperationStart += stage.OperationCount;
            }
            if (nativeOperationStart != program.Operations.Length)
                throw new ArgumentException("Animation Pose Graph Native Job stages are incomplete.", nameof(program));
            for (int i = 0; i < program.Operations.Length; i++)
            {
                AnimationPoseGraphNativeOperation operation = program.Operations[i];
                if (operation.Index < 0 || operation.Index >= program.FrameCacheCount ||
                    operation.FrameCacheIndex != operation.Index ||
                    operation.OutputValueIndex < -1 ||
                    operation.OutputValueIndex >= program.PoseValueCount ||
                    operation.OutputFullBodyIkGoalContributionValueIndex < -1 ||
                    operation.OutputFullBodyIkGoalContributionValueIndex >=
                    program.FullBodyIkGoalContributionCount ||
                    operation.OutputFullBodyIkGoalSetValueIndex < -1 ||
                    operation.OutputFullBodyIkGoalSetValueIndex >=
                    program.FullBodyIkGoalSetValueCount ||
                    operation.InputFullBodyIkGoalSetValueIndex < -1 ||
                    operation.InputFullBodyIkGoalSetValueIndex >=
                    program.FullBodyIkGoalSetValueCount ||
                    operation.FullBodyIkGoalContributionInputStart < -1 ||
                    operation.FullBodyIkGoalContributionInputCount < 0 ||
                    operation.LinkedPoseCallIndex < -1 ||
                    operation.LinkedPoseFragmentIndex < -1 ||
                    operation.LinkedPoseFragmentIndex >=
                    framePages.LinkedPoseActiveFragments.Length ||
                    !float.IsFinite(operationWeights[i]) ||
                    operationWeights[i] < 0f ||
                    operationWeights[i] > 1f)
                {
                    throw new ArgumentException($"Animation Pose Graph Native Job operation #{i} is invalid.", nameof(program));
                }
                bool validPoseInputA = operation.InputValueIndexA >= 0 &&
                                       operation.InputValueIndexA < program.PoseValueCount;
                bool validPoseInputB = operation.InputValueIndexB >= 0 &&
                                       operation.InputValueIndexB < program.PoseValueCount;
                bool inputA = validPoseInputA &&
                              operation.OutputValueIndex >= 0 &&
                              operation.InputValueIndexA < operation.OutputValueIndex;
                bool inputB = validPoseInputB &&
                              operation.OutputValueIndex >= 0 &&
                              operation.InputValueIndexB < operation.OutputValueIndex;
                bool valid = operation.Code switch
                {
                    CharacterPoseOperationCode.SelectedPosePlayer or CharacterPoseOperationCode.BlendSpacePlayer or
                        CharacterPoseOperationCode.ClipPlayer or CharacterPoseOperationCode.BlendStack =>
                        operation.InputValueIndexA == -1 && operation.InputValueIndexB == -1 &&
                        operation.PhysicalPlayerIndex >= 0 && operation.PhysicalPlayerIndex < layout.PlayerCount &&
                        IsOutputPolicy(operation.AnimationSelectionAvailabilityPolicy) &&
                        operation.BoneMaskOffset == -1 && operation.AdditiveReferenceOffset == -1 &&
                        operation.ParameterPolicyOffset == -1,
                    CharacterPoseOperationCode.AnimationSlot =>
                        inputA && operation.InputValueIndexB == -1 &&
                        operation.PhysicalPlayerIndex >= 0 && operation.PhysicalPlayerIndex < layout.PlayerCount &&
                        operation.AnimationSlotIndex >= 0 &&
                        operation.AnimationSlotIndex <
                        framePages.AnimationSlotControls.Length &&
                        inertializationProgram.SlotNodeOffset + operation.AnimationSlotIndex <
                        inertializationProgram.Nodes.Length &&
                        operation.AnimationSelectionAvailabilityPolicy == AnimationSelectionAvailabilityPolicy.AllowEmpty &&
                        operation.BoneMaskOffset == -1 && operation.AdditiveReferenceOffset == -1 &&
                        operation.ParameterPolicyOffset == -1,
                    CharacterPoseOperationCode.Inertialization =>
                        inputA && operation.InputValueIndexB == -1 &&
                        operation.InertializationIndex >= 0 && operation.InertializationIndex < inertializationProgram.Nodes.Length,
                    CharacterPoseOperationCode.BlendPose =>
                        inputA && inputB && operation.BoneMaskOffset == -1 &&
                        operation.AdditiveReferenceOffset == -1 && operation.ParameterPolicyOffset == -1 &&
                        operation.ParameterIndex < program.ParameterCount,
                    CharacterPoseOperationCode.LayeredBoneBlend =>
                        inputA && inputB && HasSpan(program.DenseBoneMasks, operation.BoneMaskOffset, program.PoseBoneCount) &&
                        operation.AdditiveReferenceOffset == -1 &&
                        operation.ParameterPolicyOffset == -1,
                    CharacterPoseOperationCode.AdditivePose =>
                        inputA && inputB && HasSpan(program.DenseBoneMasks, operation.BoneMaskOffset, program.PoseBoneCount) &&
                        HasSpan(program.AdditiveReferences, operation.AdditiveReferenceOffset, program.PoseBoneCount) &&
                        IsAdditiveReferenceSpace(operation.AdditiveReferenceSpace) &&
                        IsAdditiveScalePolicy(operation.AdditiveScalePolicy) &&
                        operation.ParameterPolicyOffset == -1,
                    CharacterPoseOperationCode.PoseParameterResolve =>
                        inputA && inputB && operation.BoneMaskOffset == -1 && operation.AdditiveReferenceOffset == -1 &&
                        HasSpan(program.ParameterPolicies, operation.ParameterPolicyOffset, program.ParameterCount),
                    CharacterPoseOperationCode.ModifyBone =>
                        inputA && operation.InputValueIndexB == -1 &&
                        operation.ModifyBoneIndex >= 0 && operation.ModifyBoneIndex < program.ModifyBones.Length,
                    CharacterPoseOperationCode.RootOrientationWarp =>
                        inputA && operation.InputValueIndexB == -1 &&
                        operation.RootOrientationWarpIndex >= 0 &&
                        operation.RootOrientationWarpIndex < program.RootOrientationWarps.Length,
                    CharacterPoseOperationCode.PoseBoneIKGoals =>
                        operation.OutputValueIndex == -1 && validPoseInputA &&
                        operation.InputValueIndexB == -1 &&
                        operation.OutputFullBodyIkGoalContributionValueIndex >= 0 &&
                        operation.OutputFullBodyIkGoalSetValueIndex == -1 &&
                        operation.InputFullBodyIkGoalSetValueIndex == -1 &&
                        operation.FullBodyIkGoalContributionInputCount == 0 &&
                        operation.PoseBoneContribution.IsValid,
                    CharacterPoseOperationCode.FootPlacement =>
                        operation.OutputValueIndex == -1 && validPoseInputA &&
                        operation.InputValueIndexB == -1 &&
                        operation.OutputFullBodyIkGoalContributionValueIndex >= 0 &&
                        operation.OutputFullBodyIkGoalSetValueIndex == -1 &&
                        operation.InputFullBodyIkGoalSetValueIndex == -1 &&
                        operation.FullBodyIkGoalContributionInputCount == 0 &&
                        operation.FootPlacementConstraint.IsValid &&
                        operation.FootPlacementConstraint.FootPlacementIndex <
                        program.FootPlacementCount,
                    CharacterPoseOperationCode.FullBodyIkGoalAssembler =>
                        operation.OutputValueIndex == -1 &&
                        operation.InputValueIndexA == -1 &&
                        operation.InputValueIndexB == -1 &&
                        operation.OutputFullBodyIkGoalContributionValueIndex == -1 &&
                        operation.OutputFullBodyIkGoalSetValueIndex >= 0 &&
                        operation.InputFullBodyIkGoalSetValueIndex == -1 &&
                        program.GoalAssemblers.Contains(
                            operation.GoalAssemblerConstraint),
                    CharacterPoseOperationCode.FullBodyIK =>
                        inputA && operation.InputValueIndexB == -1 &&
                        operation.OutputFullBodyIkGoalContributionValueIndex == -1 &&
                        operation.OutputFullBodyIkGoalSetValueIndex == -1 &&
                        operation.InputFullBodyIkGoalSetValueIndex >= 0 &&
                        program.ContainsFullBodyIkConstraint(
                            operation.FullBodyIkConstraint) &&
                        operation.FullBodyIkGoalContributionInputCount == 0 &&
                        poseConstraints.IsFullBodyIkPrepared,
                    CharacterPoseOperationCode.LinkedPoseCall =>
                        validPoseInputA && operation.InputValueIndexB == -1 &&
                        operation.LinkedPoseCallIndex >= 0 &&
                        operation.LinkedPoseCallIndex < program.LinkedPoseCalls.Length &&
                        operation.LinkedPoseFragmentIndex == -1 &&
                        operation.OutputValueIndex >= 0 &&
                        operation.OutputFullBodyIkGoalContributionValueIndex == -1 &&
                        operation.OutputFullBodyIkGoalSetValueIndex == -1 &&
                        operation.InputFullBodyIkGoalSetValueIndex == -1 &&
                        operation.FullBodyIkGoalContributionInputCount == 0,
                    CharacterPoseOperationCode.LocalToComponentPose or
                        CharacterPoseOperationCode.ComponentToLocalPose =>
                        inputA && operation.InputValueIndexB == -1 &&
                        operation.BoneMaskOffset == -1 &&
                        operation.AdditiveReferenceOffset == -1 &&
                        operation.ParameterPolicyOffset == -1,
                    CharacterPoseOperationCode.StatePoseOutput =>
                        inputA && operation.InputValueIndexB == -1,
                    CharacterPoseOperationCode.PoseStateMachine =>
                        operation.InputValueIndexA == -1 && operation.InputValueIndexB == -1 &&
                        operation.StateMachineIndex >= 0 &&
                        operation.StateMachineIndex <
                        framePages.StateMachineControls.Length,
                    CharacterPoseOperationCode.OutputPose =>
                        inputA && operation.InputValueIndexB == -1 && operation.BoneMaskOffset == -1 &&
                        operation.AdditiveReferenceOffset == -1 && operation.ParameterPolicyOffset == -1,
                    _ => false
                };
                if (!valid)
                    throw new ArgumentException($"Animation Pose Graph Native Job operation #{i} layout is invalid.", nameof(program));
                if (operation.Code == CharacterPoseOperationCode.OutputPose)
                {
                    outputCount++;
                    if (i != program.OutputNativeOperationIndex ||
                        operation.Index != program.OutputOperationIndex ||
                        operation.OutputValueIndex != program.OutputValueIndex)
                    {
                        throw new ArgumentException("Animation Pose Graph Native Job output identity is invalid.", nameof(program));
                    }
                }
            }
            if (outputCount != 1)
                throw new ArgumentException("Animation Pose Graph Native Job requires one output operation.", nameof(program));
        }

        static bool HasSpan<T>(NativeArray<T> values, int offset, int count) where T : struct =>
            offset >= 0 && count > 0 && offset <= values.Length - count;

        static bool IsOutputPolicy(AnimationSelectionAvailabilityPolicy value) =>
            (int)value >= (int)AnimationSelectionAvailabilityPolicy.RequireSelection &&
            (int)value <= (int)AnimationSelectionAvailabilityPolicy.AllowEmpty;

        static bool IsAdditiveReferenceSpace(AdditiveReferenceSpace value) =>
            (int)value >= (int)AdditiveReferenceSpace.Local &&
            (int)value <= (int)AdditiveReferenceSpace.Mesh;

        static bool IsAdditiveScalePolicy(AdditiveScalePolicy value) =>
            (int)value >= (int)AdditiveScalePolicy.Multiply &&
            (int)value <= (int)AdditiveScalePolicy.Ignore;
    }
}
