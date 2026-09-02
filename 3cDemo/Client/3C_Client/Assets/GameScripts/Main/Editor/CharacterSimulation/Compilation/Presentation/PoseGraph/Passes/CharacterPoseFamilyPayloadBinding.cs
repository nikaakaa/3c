using System;
using ThirdPersonCharacter.Animation.TransitionRouting;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Editor.CharacterSimulation
{
    internal sealed class CharacterPoseBoundOperation
    {
        internal CharacterPoseBoundOperation(
            int index,
            CharacterPoseExecutionDomain executionDomain,
            CharacterPoseSpace inputPoseSpace,
            CharacterPoseSpace outputPoseSpace,
            CharacterPoseOperationCode code,
            CharacterPoseOperationFamily family,
            PoseNodeId nodeId,
            PresentationPoseSourceProviderId presentationPoseSourceProviderId,
            PresentationPoseSourceIndex presentationPoseSourceIndex,
            int outputValueIndex,
            int inputValueIndexA,
            int inputValueIndexB,
            int controlInputOperationIndex,
            AnimationChannelId animationChannelId,
            AnimationSelectionAvailabilityPolicy selectionAvailability,
            int parameterIndex,
            int parameterIndexB,
            CharacterAnimationBlendSpaceInputRangePolicy blendSpaceInputRangePolicy,
            int playerIndex,
            int blendNodeIndex,
            int inertializationIndex,
            int boneMaskIndex,
            int additiveReferenceIndex,
            int modifyBoneIndex,
            int rootOrientationWarpIndex,
            int poseBoneIkGoalsIndex,
            int footPlacementIndex,
            int fullBodyIkIndex,
            int outputFullBodyIkGoalContributionValueIndex,
            int outputFullBodyIkGoalSetValueIndex,
            int inputFullBodyIkGoalSetValueIndex,
            int fullBodyIkGoalContributionInputStart,
            int fullBodyIkGoalContributionInputCount,
            int clipPlayerIndex,
            int stateMachineIndex,
            int animationSlotIndex,
            int linkedPoseCallIndex,
            int linkedPoseFragmentIndex,
            float weight,
            PoseParameterResolvePolicy[] parameterPolicies)
        {
            if (index < 0 ||
                !Enum.IsDefined(typeof(CharacterPoseExecutionDomain), executionDomain) ||
                !Enum.IsDefined(typeof(CharacterPoseSpace), inputPoseSpace) ||
                !Enum.IsDefined(typeof(CharacterPoseSpace), outputPoseSpace) ||
                !Enum.IsDefined(typeof(CharacterPoseOperationCode), code) ||
                !Enum.IsDefined(typeof(CharacterPoseOperationFamily), family) ||
                family == CharacterPoseOperationFamily.None ||
                CharacterPoseOperationFamilies.RequireFamily(code) != family ||
                !nodeId.IsValid ||
                !float.IsFinite(weight) || weight < 0f || weight > 1f ||
                outputFullBodyIkGoalContributionValueIndex < -1 ||
                outputFullBodyIkGoalSetValueIndex < -1 ||
                inputFullBodyIkGoalSetValueIndex < -1 ||
                fullBodyIkGoalContributionInputStart < -1 ||
                fullBodyIkGoalContributionInputCount < 0 ||
                linkedPoseCallIndex < -1 || linkedPoseFragmentIndex < -1 ||
                (fullBodyIkGoalContributionInputCount == 0) !=
                (fullBodyIkGoalContributionInputStart == -1) ||
                !Enum.IsDefined(
                    typeof(CharacterAnimationBlendSpaceInputRangePolicy),
                    blendSpaceInputRangePolicy))
            {
                throw new ArgumentException(
                    "Bound Pose operation is invalid.");
            }
            Index = index;
            ExecutionDomain = executionDomain;
            InputPoseSpace = inputPoseSpace;
            OutputPoseSpace = outputPoseSpace;
            Code = code;
            Family = family;
            NodeId = nodeId;
            PresentationPoseSourceProviderId =
                presentationPoseSourceProviderId;
            PresentationPoseSourceIndex = presentationPoseSourceIndex;
            OutputValueIndex = outputValueIndex;
            InputValueIndexA = inputValueIndexA;
            InputValueIndexB = inputValueIndexB;
            ControlInputOperationIndex = controlInputOperationIndex;
            AnimationChannelId = animationChannelId;
            SelectionAvailability = selectionAvailability;
            ParameterIndex = parameterIndex;
            ParameterIndexB = parameterIndexB;
            BlendSpaceInputRangePolicy = blendSpaceInputRangePolicy;
            PlayerIndex = playerIndex;
            BlendNodeIndex = blendNodeIndex;
            InertializationIndex = inertializationIndex;
            BoneMaskIndex = boneMaskIndex;
            AdditiveReferenceIndex = additiveReferenceIndex;
            ModifyBoneIndex = modifyBoneIndex;
            RootOrientationWarpIndex = rootOrientationWarpIndex;
            PoseBoneIkGoalsIndex = poseBoneIkGoalsIndex;
            FootPlacementIndex = footPlacementIndex;
            FullBodyIkIndex = fullBodyIkIndex;
            OutputFullBodyIkGoalContributionValueIndex =
                outputFullBodyIkGoalContributionValueIndex;
            OutputFullBodyIkGoalSetValueIndex =
                outputFullBodyIkGoalSetValueIndex;
            InputFullBodyIkGoalSetValueIndex =
                inputFullBodyIkGoalSetValueIndex;
            FullBodyIkGoalContributionInputStart =
                fullBodyIkGoalContributionInputStart;
            FullBodyIkGoalContributionInputCount =
                fullBodyIkGoalContributionInputCount;
            ClipPlayerIndex = clipPlayerIndex;
            StateMachineIndex = stateMachineIndex;
            AnimationSlotIndex = animationSlotIndex;
            LinkedPoseCallIndex = linkedPoseCallIndex;
            LinkedPoseFragmentIndex = linkedPoseFragmentIndex;
            Weight = weight;
            ParameterPolicies = parameterPolicies ??
                Array.Empty<PoseParameterResolvePolicy>();
        }

        internal int Index { get; }
        internal CharacterPoseExecutionDomain ExecutionDomain { get; }
        internal CharacterPoseSpace InputPoseSpace { get; }
        internal CharacterPoseSpace OutputPoseSpace { get; }
        internal CharacterPoseOperationCode Code { get; }
        internal CharacterPoseOperationFamily Family { get; }
        internal PoseNodeId NodeId { get; }
        internal PresentationPoseSourceProviderId PresentationPoseSourceProviderId { get; }
        internal PresentationPoseSourceIndex PresentationPoseSourceIndex { get; }
        internal int OutputValueIndex { get; }
        internal int InputValueIndexA { get; }
        internal int InputValueIndexB { get; }
        internal int ControlInputOperationIndex { get; }
        internal AnimationChannelId AnimationChannelId { get; }
        internal AnimationSelectionAvailabilityPolicy SelectionAvailability { get; }
        internal int ParameterIndex { get; }
        internal int ParameterIndexB { get; }
        internal CharacterAnimationBlendSpaceInputRangePolicy BlendSpaceInputRangePolicy { get; }
        internal int PlayerIndex { get; }
        internal int BlendNodeIndex { get; }
        internal int InertializationIndex { get; }
        internal int BoneMaskIndex { get; }
        internal int AdditiveReferenceIndex { get; }
        internal int ModifyBoneIndex { get; }
        internal int RootOrientationWarpIndex { get; }
        internal int PoseBoneIkGoalsIndex { get; }
        internal int FootPlacementIndex { get; }
        internal int FullBodyIkIndex { get; }
        internal int OutputFullBodyIkGoalContributionValueIndex { get; }
        internal int OutputFullBodyIkGoalSetValueIndex { get; }
        internal int InputFullBodyIkGoalSetValueIndex { get; }
        internal int FullBodyIkGoalContributionInputStart { get; }
        internal int FullBodyIkGoalContributionInputCount { get; }
        internal int ClipPlayerIndex { get; }
        internal int StateMachineIndex { get; }
        internal int AnimationSlotIndex { get; }
        internal int LinkedPoseCallIndex { get; }
        internal int LinkedPoseFragmentIndex { get; }
        internal float Weight { get; }
        internal PoseParameterResolvePolicy[] ParameterPolicies { get; }
    }

    internal sealed class CharacterPoseBoundFamilyPayloads
    {
        internal CharacterPoseBoundFamilyPayloads(
            CharacterPresentationPoseParameterEntry[] parameters,
            AnimationBlendNodePayload[] blendNodes,
            CharacterPresentationDenseBoneMask[] boneMasks,
            CharacterPresentationAdditiveReferenceDescriptor[] additiveReferences,
            CharacterPresentationModifyBoneDescriptor[] modifyBones,
            CharacterPresentationRootOrientationWarpDescriptor[] rootOrientationWarps,
            CharacterPresentationPoseBoneIkGoalsDescriptor[] poseBoneIkGoalSources,
            CharacterPresentationFootPlacementDescriptor[] footPlacements,
            CharacterPresentationFullBodyIkDescriptor[] fullBodyIks,
            int[] fullBodyIkGoalContributionInputValueIndices,
            CharacterPresentationClipPlayerDescriptor[] clipPlayers,
            CharacterPoseStateMachineDescriptor[] stateMachines,
            CharacterAnimationSlotDescriptor[] animationSlots,
            ActionPlaybackInputPlan[] actionPlaybackInputs,
            CharacterLinkedPoseEntryFragmentPlanDescriptor[] linkedPoseFragments,
            CharacterLinkedPoseCallPlanDescriptor[] linkedPoseCalls)
        {
            Parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
            BlendNodes = blendNodes ?? throw new ArgumentNullException(nameof(blendNodes));
            BoneMasks = boneMasks ?? throw new ArgumentNullException(nameof(boneMasks));
            AdditiveReferences = additiveReferences ?? throw new ArgumentNullException(nameof(additiveReferences));
            ModifyBones = modifyBones ?? throw new ArgumentNullException(nameof(modifyBones));
            RootOrientationWarps = rootOrientationWarps ?? throw new ArgumentNullException(nameof(rootOrientationWarps));
            PoseBoneIkGoalSources = poseBoneIkGoalSources ?? throw new ArgumentNullException(nameof(poseBoneIkGoalSources));
            FootPlacements = footPlacements ?? throw new ArgumentNullException(nameof(footPlacements));
            FullBodyIks = fullBodyIks ?? throw new ArgumentNullException(nameof(fullBodyIks));
            FullBodyIkGoalContributionInputValueIndices =
                fullBodyIkGoalContributionInputValueIndices ??
                throw new ArgumentNullException(nameof(fullBodyIkGoalContributionInputValueIndices));
            ClipPlayers = clipPlayers ?? throw new ArgumentNullException(nameof(clipPlayers));
            StateMachines = stateMachines ?? throw new ArgumentNullException(nameof(stateMachines));
            AnimationSlots = animationSlots ?? throw new ArgumentNullException(nameof(animationSlots));
            ActionPlaybackInputs = actionPlaybackInputs ?? throw new ArgumentNullException(nameof(actionPlaybackInputs));
            LinkedPoseFragments = linkedPoseFragments ?? throw new ArgumentNullException(nameof(linkedPoseFragments));
            LinkedPoseCalls = linkedPoseCalls ?? throw new ArgumentNullException(nameof(linkedPoseCalls));
        }

        internal CharacterPresentationPoseParameterEntry[] Parameters { get; }
        internal AnimationBlendNodePayload[] BlendNodes { get; }
        internal CharacterPresentationDenseBoneMask[] BoneMasks { get; }
        internal CharacterPresentationAdditiveReferenceDescriptor[] AdditiveReferences { get; }
        internal CharacterPresentationModifyBoneDescriptor[] ModifyBones { get; }
        internal CharacterPresentationRootOrientationWarpDescriptor[] RootOrientationWarps { get; }
        internal CharacterPresentationPoseBoneIkGoalsDescriptor[] PoseBoneIkGoalSources { get; }
        internal CharacterPresentationFootPlacementDescriptor[] FootPlacements { get; }
        internal CharacterPresentationFullBodyIkDescriptor[] FullBodyIks { get; }
        internal int[] FullBodyIkGoalContributionInputValueIndices { get; }
        internal CharacterPresentationClipPlayerDescriptor[] ClipPlayers { get; }
        internal CharacterPoseStateMachineDescriptor[] StateMachines { get; }
        internal CharacterAnimationSlotDescriptor[] AnimationSlots { get; }
        internal ActionPlaybackInputPlan[] ActionPlaybackInputs { get; }
        internal CharacterLinkedPoseEntryFragmentPlanDescriptor[] LinkedPoseFragments { get; }
        internal CharacterLinkedPoseCallPlanDescriptor[] LinkedPoseCalls { get; }
    }

    internal readonly struct CharacterPoseBoundProgramLayout
    {
        internal CharacterPoseBoundProgramLayout(
            int poseSourceCount,
            int poseValueCount,
            int fullBodyIkGoalContributionValueCount,
            int fullBodyIkGoalSetValueCount,
            int fullBodyIkGoalContributionGoalWorkspaceCount,
            int playerCount,
            int inertializationCount,
            int outputOperationIndex)
        {
            if (poseSourceCount < 0 ||
                poseValueCount <= 0 ||
                fullBodyIkGoalContributionValueCount < 0 ||
                fullBodyIkGoalSetValueCount < 0 ||
                fullBodyIkGoalContributionGoalWorkspaceCount < 0 ||
                playerCount < 0 ||
                inertializationCount < 0 ||
                outputOperationIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(poseValueCount));
            }
            PoseSourceCount = poseSourceCount;
            PoseValueCount = poseValueCount;
            FullBodyIkGoalContributionValueCount =
                fullBodyIkGoalContributionValueCount;
            FullBodyIkGoalSetValueCount = fullBodyIkGoalSetValueCount;
            FullBodyIkGoalContributionGoalWorkspaceCount =
                fullBodyIkGoalContributionGoalWorkspaceCount;
            PlayerCount = playerCount;
            InertializationCount = inertializationCount;
            OutputOperationIndex = outputOperationIndex;
        }

        internal int PoseSourceCount { get; }
        internal int PoseValueCount { get; }
        internal int FullBodyIkGoalContributionValueCount { get; }
        internal int FullBodyIkGoalSetValueCount { get; }
        internal int FullBodyIkGoalContributionGoalWorkspaceCount { get; }
        internal int PlayerCount { get; }
        internal int InertializationCount { get; }
        internal int OutputOperationIndex { get; }
    }

    internal sealed class CharacterPoseFamilyPayloadBinding
    {
        internal CharacterPoseFamilyPayloadBinding(
            CharacterPoseBoundFamilyPayloads payloads,
            CharacterPoseBoundOperation[] operations,
            CharacterPresentationPoseSourceMapEntry[] sourceMap,
            string[] graphDependencies,
            in CharacterPoseBoundProgramLayout layout)
        {
            Payloads = payloads ?? throw new ArgumentNullException(nameof(payloads));
            Operations = operations ?? throw new ArgumentNullException(nameof(operations));
            SourceMap = sourceMap ?? throw new ArgumentNullException(nameof(sourceMap));
            GraphDependencies = graphDependencies ?? throw new ArgumentNullException(nameof(graphDependencies));
            if (Operations.Length == 0 ||
                (uint)layout.OutputOperationIndex >= (uint)Operations.Length)
            {
                throw new ArgumentException(
                    "Pose Family Payload binding has no valid Output operation.",
                    nameof(layout));
            }
            Layout = layout;
            OperationPages = CharacterPoseOperationPageBinding.Create(
                Operations,
                Payloads);
        }

        internal CharacterPoseBoundFamilyPayloads Payloads { get; }
        internal CharacterPoseBoundOperation[] Operations { get; }
        internal CharacterPresentationPoseSourceMapEntry[] SourceMap { get; }
        internal string[] GraphDependencies { get; }
        internal CharacterPoseBoundProgramLayout Layout { get; }
        internal CharacterPoseOperationPages OperationPages { get; }
    }
}
