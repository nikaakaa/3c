using System;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal readonly struct CharacterPoseNativeOperationHeader
    {
        internal CharacterPoseNativeOperationHeader(
            int index,
            CharacterPoseOperationCode code,
            CharacterPoseOperationFamily family,
            int familyPayloadIndex,
            int outputPoseValueIndex,
            int linkedPoseFragmentIndex,
            int frameCacheIndex,
            float weight)
        {
            if (index < 0 ||
                CharacterPoseOperationFamilies.RequireFamily(code) != family ||
                familyPayloadIndex < 0 || outputPoseValueIndex < -1 ||
                linkedPoseFragmentIndex < -1 || frameCacheIndex != index ||
                !float.IsFinite(weight) || weight < 0f || weight > 1f)
            {
                throw new ArgumentException("Native Pose Operation header is invalid.");
            }
            Index = index;
            Code = code;
            Family = family;
            FamilyPayloadIndex = familyPayloadIndex;
            OutputPoseValueIndex = outputPoseValueIndex;
            LinkedPoseFragmentIndex = linkedPoseFragmentIndex;
            FrameCacheIndex = frameCacheIndex;
            Weight = weight;
        }

        internal int Index { get; }
        internal CharacterPoseOperationCode Code { get; }
        internal CharacterPoseOperationFamily Family { get; }
        internal int FamilyPayloadIndex { get; }
        internal int OutputPoseValueIndex { get; }
        internal int LinkedPoseFragmentIndex { get; }
        internal int FrameCacheIndex { get; }
        internal float Weight { get; }

        internal CharacterPoseNativeOperationHeader WithWeight(float value) =>
            new CharacterPoseNativeOperationHeader(
                Index,
                Code,
                Family,
                FamilyPayloadIndex,
                OutputPoseValueIndex,
                LinkedPoseFragmentIndex,
                FrameCacheIndex,
                value);
    }

    internal readonly struct CharacterPoseNativePlayerOperation
    {
        internal CharacterPoseNativePlayerOperation(
            int outputPoseValueIndex,
            int playerIndex,
            AnimationSelectionAvailabilityPolicy selectionAvailability)
        {
            if (outputPoseValueIndex < 0 || playerIndex < 0 ||
                !Enum.IsDefined(
                    typeof(AnimationSelectionAvailabilityPolicy),
                    selectionAvailability))
            {
                throw new ArgumentException("Native Pose Player operation is invalid.");
            }
            OutputPoseValueIndex = outputPoseValueIndex;
            PlayerIndex = playerIndex;
            SelectionAvailability = selectionAvailability;
        }

        internal int OutputPoseValueIndex { get; }
        internal int PlayerIndex { get; }
        internal AnimationSelectionAvailabilityPolicy SelectionAvailability { get; }
    }

    internal readonly struct CharacterPoseNativeBlendOperation
    {
        internal CharacterPoseNativeBlendOperation(
            CharacterPoseOperationCode code,
            int outputPoseValueIndex,
            int inputPoseValueIndexA,
            int inputPoseValueIndexB,
            int parameterIndex,
            int playerIndex,
            AnimationSelectionAvailabilityPolicy selectionAvailability)
        {
            bool stack = code == CharacterPoseOperationCode.BlendStack;
            bool blend = code == CharacterPoseOperationCode.BlendPose;
            if ((!stack && !blend) || outputPoseValueIndex < 0 ||
                stack && (inputPoseValueIndexA != -1 ||
                    inputPoseValueIndexB != -1 || parameterIndex != -1 ||
                    playerIndex < 0) ||
                blend && (inputPoseValueIndexA < 0 ||
                    inputPoseValueIndexB < 0 || parameterIndex < -1 ||
                    playerIndex != -1) ||
                !Enum.IsDefined(
                    typeof(AnimationSelectionAvailabilityPolicy),
                    selectionAvailability))
            {
                throw new ArgumentException("Native Pose Blend operation is invalid.");
            }
            OutputPoseValueIndex = outputPoseValueIndex;
            InputPoseValueIndexA = inputPoseValueIndexA;
            InputPoseValueIndexB = inputPoseValueIndexB;
            ParameterIndex = parameterIndex;
            PlayerIndex = playerIndex;
            SelectionAvailability = selectionAvailability;
        }

        internal int OutputPoseValueIndex { get; }
        internal int InputPoseValueIndexA { get; }
        internal int InputPoseValueIndexB { get; }
        internal int ParameterIndex { get; }
        internal int PlayerIndex { get; }
        internal AnimationSelectionAvailabilityPolicy SelectionAvailability { get; }
    }

    internal readonly struct CharacterPoseNativeStateMachineOperation
    {
        internal CharacterPoseNativeStateMachineOperation(
            CharacterPoseOperationCode code,
            int outputPoseValueIndex,
            int inputPoseValueIndex,
            int stateMachineIndex)
        {
            bool output = code == CharacterPoseOperationCode.StatePoseOutput;
            bool machine = code == CharacterPoseOperationCode.PoseStateMachine;
            if ((!output && !machine) || outputPoseValueIndex < 0 ||
                output && (inputPoseValueIndex < 0 || stateMachineIndex != -1) ||
                machine && (inputPoseValueIndex != -1 || stateMachineIndex < 0))
            {
                throw new ArgumentException("Native Pose State operation is invalid.");
            }
            OutputPoseValueIndex = outputPoseValueIndex;
            InputPoseValueIndex = inputPoseValueIndex;
            StateMachineIndex = stateMachineIndex;
        }

        internal int OutputPoseValueIndex { get; }
        internal int InputPoseValueIndex { get; }
        internal int StateMachineIndex { get; }

    }

    internal readonly struct CharacterPoseNativeAnimationSlotOperation
    {
        internal CharacterPoseNativeAnimationSlotOperation(
            int outputPoseValueIndex,
            int inputPoseValueIndex,
            int playerIndex,
            int animationSlotIndex,
            AnimationSelectionAvailabilityPolicy selectionAvailability)
        {
            if (outputPoseValueIndex < 0 || inputPoseValueIndex < 0 ||
                playerIndex < 0 || animationSlotIndex < 0 ||
                !Enum.IsDefined(
                    typeof(AnimationSelectionAvailabilityPolicy),
                    selectionAvailability))
            {
                throw new ArgumentException("Native Pose Animation Slot operation is invalid.");
            }
            OutputPoseValueIndex = outputPoseValueIndex;
            InputPoseValueIndex = inputPoseValueIndex;
            PlayerIndex = playerIndex;
            AnimationSlotIndex = animationSlotIndex;
            SelectionAvailability = selectionAvailability;
        }

        internal int OutputPoseValueIndex { get; }
        internal int InputPoseValueIndex { get; }
        internal int PlayerIndex { get; }
        internal int AnimationSlotIndex { get; }
        internal AnimationSelectionAvailabilityPolicy SelectionAvailability { get; }
    }

    internal readonly struct CharacterPoseNativeInertializationOperation
    {
        internal CharacterPoseNativeInertializationOperation(
            int outputPoseValueIndex,
            int inputPoseValueIndex,
            int inertializationIndex)
        {
            if (outputPoseValueIndex < 0 || inputPoseValueIndex < 0 ||
                inertializationIndex < 0)
            {
                throw new ArgumentException("Native Pose Inertialization operation is invalid.");
            }
            OutputPoseValueIndex = outputPoseValueIndex;
            InputPoseValueIndex = inputPoseValueIndex;
            InertializationIndex = inertializationIndex;
        }

        internal int OutputPoseValueIndex { get; }
        internal int InputPoseValueIndex { get; }
        internal int InertializationIndex { get; }
    }

    internal readonly struct CharacterPoseNativeCompositionOperation
    {
        internal CharacterPoseNativeCompositionOperation(
            CharacterPoseOperationCode code,
            int outputPoseValueIndex,
            int inputPoseValueIndexA,
            int inputPoseValueIndexB,
            int boneMaskOffset,
            int additiveReferenceOffset,
            AdditiveReferenceSpace additiveReferenceSpace,
            AdditiveScalePolicy additiveScalePolicy)
        {
            if ((code != CharacterPoseOperationCode.LayeredBoneBlend &&
                 code != CharacterPoseOperationCode.AdditivePose) ||
                outputPoseValueIndex < 0 || inputPoseValueIndexA < 0 ||
                inputPoseValueIndexB < 0 || boneMaskOffset < 0 ||
                code == CharacterPoseOperationCode.LayeredBoneBlend &&
                    additiveReferenceOffset != -1 ||
                code == CharacterPoseOperationCode.AdditivePose &&
                    additiveReferenceOffset < 0)
            {
                throw new ArgumentException("Native Pose Composition operation is invalid.");
            }
            OutputPoseValueIndex = outputPoseValueIndex;
            InputPoseValueIndexA = inputPoseValueIndexA;
            InputPoseValueIndexB = inputPoseValueIndexB;
            BoneMaskOffset = boneMaskOffset;
            AdditiveReferenceOffset = additiveReferenceOffset;
            AdditiveReferenceSpace = additiveReferenceSpace;
            AdditiveScalePolicy = additiveScalePolicy;
        }

        internal int OutputPoseValueIndex { get; }
        internal int InputPoseValueIndexA { get; }
        internal int InputPoseValueIndexB { get; }
        internal int BoneMaskOffset { get; }
        internal int AdditiveReferenceOffset { get; }
        internal AdditiveReferenceSpace AdditiveReferenceSpace { get; }
        internal AdditiveScalePolicy AdditiveScalePolicy { get; }
    }

    internal readonly struct CharacterPoseNativeParameterResolveOperation
    {
        internal CharacterPoseNativeParameterResolveOperation(
            int outputPoseValueIndex,
            int inputPoseValueIndexA,
            int inputPoseValueIndexB,
            int parameterPolicyOffset)
        {
            if (outputPoseValueIndex < 0 || inputPoseValueIndexA < 0 ||
                inputPoseValueIndexB < 0 || parameterPolicyOffset < 0)
            {
                throw new ArgumentException("Native Pose Parameter Resolve operation is invalid.");
            }
            OutputPoseValueIndex = outputPoseValueIndex;
            InputPoseValueIndexA = inputPoseValueIndexA;
            InputPoseValueIndexB = inputPoseValueIndexB;
            ParameterPolicyOffset = parameterPolicyOffset;
        }

        internal int OutputPoseValueIndex { get; }
        internal int InputPoseValueIndexA { get; }
        internal int InputPoseValueIndexB { get; }
        internal int ParameterPolicyOffset { get; }
    }

    internal readonly struct CharacterPoseNativeSpaceConversionOperation
    {
        internal CharacterPoseNativeSpaceConversionOperation(
            int outputPoseValueIndex,
            int inputPoseValueIndex)
        {
            if (outputPoseValueIndex < 0 || inputPoseValueIndex < 0)
                throw new ArgumentException("Native Pose Space Conversion operation is invalid.");
            OutputPoseValueIndex = outputPoseValueIndex;
            InputPoseValueIndex = inputPoseValueIndex;
        }

        internal int OutputPoseValueIndex { get; }
        internal int InputPoseValueIndex { get; }
    }

    internal readonly struct CharacterPoseNativeComponentControlOperation
    {
        internal CharacterPoseNativeComponentControlOperation(
            CharacterPoseOperationCode code,
            int outputPoseValueIndex,
            int inputPoseValueIndex,
            int modifyBoneIndex,
            int rootOrientationWarpIndex)
        {
            bool modify = code == CharacterPoseOperationCode.ModifyBone;
            bool warp = code == CharacterPoseOperationCode.RootOrientationWarp;
            if ((!modify && !warp) || outputPoseValueIndex < 0 ||
                inputPoseValueIndex < 0 ||
                modify && (modifyBoneIndex < 0 || rootOrientationWarpIndex != -1) ||
                warp && (modifyBoneIndex != -1 || rootOrientationWarpIndex < 0))
            {
                throw new ArgumentException("Native Pose Component Control operation is invalid.");
            }
            OutputPoseValueIndex = outputPoseValueIndex;
            InputPoseValueIndex = inputPoseValueIndex;
            ModifyBoneIndex = modifyBoneIndex;
            RootOrientationWarpIndex = rootOrientationWarpIndex;
        }

        internal int OutputPoseValueIndex { get; }
        internal int InputPoseValueIndex { get; }
        internal int ModifyBoneIndex { get; }
        internal int RootOrientationWarpIndex { get; }
    }

    internal readonly struct CharacterPoseNativeGoalContributionOperation
    {
        internal CharacterPoseNativeGoalContributionOperation(
            int inputPoseValueIndex,
            int parameterIndex,
            CharacterPoseBoneContributionConstraintHandle poseBoneContribution,
            CharacterFootPlacementConstraintHandle footPlacement)
        {
            bool poseBone = poseBoneContribution.IsValid;
            bool foot = footPlacement.IsValid;
            if (inputPoseValueIndex < 0 || poseBone == foot ||
                poseBone && parameterIndex != -1 ||
                foot && parameterIndex < 0)
            {
                throw new ArgumentException("Native Pose Goal Contribution operation is invalid.");
            }
            InputPoseValueIndex = inputPoseValueIndex;
            ParameterIndex = parameterIndex;
            PoseBoneContribution = poseBoneContribution;
            FootPlacement = footPlacement;
        }

        internal int InputPoseValueIndex { get; }
        internal int ParameterIndex { get; }
        internal CharacterPoseBoneContributionConstraintHandle PoseBoneContribution { get; }
        internal CharacterFootPlacementConstraintHandle FootPlacement { get; }
    }

    internal readonly struct CharacterPoseNativeGoalAssemblerOperation
    {
        internal CharacterPoseNativeGoalAssemblerOperation(
            CharacterFullBodyIkGoalAssemblerConstraintHandle handle)
        {
            if (!handle.IsValid)
                throw new ArgumentException("Native Pose Goal Assembler operation is invalid.");
            Handle = handle;
        }

        internal CharacterFullBodyIkGoalAssemblerConstraintHandle Handle { get; }
    }

    internal readonly struct CharacterPoseNativeFullBodyIkOperation
    {
        internal CharacterPoseNativeFullBodyIkOperation(
            CharacterFullBodyIkConstraintHandle handle)
        {
            if (!handle.IsValid)
                throw new ArgumentException("Native Pose Full Body IK operation is invalid.");
            Handle = handle;
        }

        internal CharacterFullBodyIkConstraintHandle Handle { get; }
    }

    internal readonly struct CharacterPoseNativeLinkedPoseOperation
    {
        internal CharacterPoseNativeLinkedPoseOperation(
            int outputPoseValueIndex,
            int linkedPoseCallIndex)
        {
            if (outputPoseValueIndex < 0 || linkedPoseCallIndex < 0)
                throw new ArgumentException("Native Linked Pose operation is invalid.");
            OutputPoseValueIndex = outputPoseValueIndex;
            LinkedPoseCallIndex = linkedPoseCallIndex;
        }

        internal int OutputPoseValueIndex { get; }
        internal int LinkedPoseCallIndex { get; }
    }

    internal readonly struct CharacterPoseNativeOutputOperation
    {
        internal CharacterPoseNativeOutputOperation(int inputPoseValueIndex)
        {
            if (inputPoseValueIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(inputPoseValueIndex));
            InputPoseValueIndex = inputPoseValueIndex;
        }

        internal int InputPoseValueIndex { get; }
    }
}
