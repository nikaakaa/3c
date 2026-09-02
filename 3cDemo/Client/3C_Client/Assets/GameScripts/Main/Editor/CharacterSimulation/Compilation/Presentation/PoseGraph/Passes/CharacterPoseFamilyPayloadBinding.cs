using System;
using ThirdPersonCharacter.Animation.TransitionRouting;
using ThirdPersonCharacter.Pipeline.Animation;

namespace ThirdPersonCharacter.Editor.CharacterSimulation
{
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
            CharacterPresentationPoseOperation[] operations,
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
        }

        internal CharacterPoseBoundFamilyPayloads Payloads { get; }
        internal CharacterPresentationPoseOperation[] Operations { get; }
        internal CharacterPresentationPoseSourceMapEntry[] SourceMap { get; }
        internal string[] GraphDependencies { get; }
        internal CharacterPoseBoundProgramLayout Layout { get; }
    }
}
