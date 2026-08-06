using System;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public static class CharacterLinkedPoseRuntimeLayoutBuilder
    {
        public static CharacterLinkedPoseRuntimeLayoutCatalog Build(CharacterPresentationProjection projection)
        {
            projection = projection ?? throw new ArgumentNullException(nameof(projection));
            projection.RequirePosePayload();
            CharacterLinkedPoseProjectionPayload linkedPose = projection.LinkedPose;
            CharacterPresentationPosePlan posePlan = projection.PosePlan;
            var orderedGroups = new CharacterLinkedPoseGroupProjectionDescriptor[linkedPose.Groups.Count];
            for (int i = 0; i < orderedGroups.Length; i++)
                orderedGroups[i] = linkedPose.Groups[i];
            Array.Sort(orderedGroups, (left, right) => left.GroupId.CompareTo(right.GroupId));
            var groups = new CharacterLinkedPoseGroupRuntimeLayout[orderedGroups.Length];
            for (int groupIndex = 0; groupIndex < orderedGroups.Length; groupIndex++)
                groups[groupIndex] = BuildGroup(linkedPose, posePlan, orderedGroups[groupIndex]);
            return new CharacterLinkedPoseRuntimeLayoutCatalog(groups);
        }

        static CharacterLinkedPoseGroupRuntimeLayout BuildGroup(
            CharacterLinkedPoseProjectionPayload linkedPose,
            CharacterPresentationPosePlan posePlan,
            CharacterLinkedPoseGroupProjectionDescriptor group)
        {
            CharacterLinkedPoseCompiledSelectorDescriptor selector = RequireSelector(linkedPose, group.GroupId);
            CharacterLinkedPoseInterfaceProjectionDescriptor linkedInterface = RequireInterface(linkedPose, group.InterfaceId);
            var implementationIds = new LinkedPoseImplementationId[selector.CandidateImplementationIds.Count];
            for (int i = 0; i < implementationIds.Length; i++)
                implementationIds[i] = new LinkedPoseImplementationId(selector.CandidateImplementationIds[i]);
            Array.Sort(implementationIds);
            var implementations = new CharacterLinkedPoseImplementationRuntimeLayout[implementationIds.Length];
            CharacterLinkedPoseRuntimeCapacity maximum = default;
            for (int implementationIndex = 0; implementationIndex < implementationIds.Length; implementationIndex++)
            {
                implementations[implementationIndex] = BuildImplementation(
                    posePlan,
                    group.GroupId,
                    implementationIds[implementationIndex],
                    linkedInterface);
                CharacterLinkedPoseRuntimeCapacity capacity = implementations[implementationIndex].Capacity;
                maximum = CharacterLinkedPoseRuntimeCapacity.Max(in maximum, in capacity);
            }
            return new CharacterLinkedPoseGroupRuntimeLayout(group.GroupId, in maximum, implementations);
        }

        static CharacterLinkedPoseImplementationRuntimeLayout BuildImplementation(
            CharacterPresentationPosePlan posePlan,
            LinkedPoseGroupId groupId,
            LinkedPoseImplementationId implementationId,
            CharacterLinkedPoseInterfaceProjectionDescriptor linkedInterface)
        {
            var entries = new CharacterLinkedPoseEntryRuntimeLayout[linkedInterface.Entries.Count];
            int operationOffset = 0;
            int poseValueOffset = 0;
            int goalSetValueOffset = 0;
            int playerOffset = 0;
            int stateMachineOffset = 0;
            int inertializationOffset = 0;
            int rootOrientationWarpOffset = 0;
            int motionMatchingProviderOffset = 0;
            int sourceDemandOffset = 0;
            int frameCompletionOffset = 0;
            int playerCompletionOffset = 0;
            int stageCompletionOffset = 0;
            int operationDiagnosticOffset = 0;
            int stageDiagnosticOffset = 0;
            for (int entryIndex = 0; entryIndex < entries.Length; entryIndex++)
            {
                LinkedPoseEntryId entryId = linkedInterface.Entries[entryIndex].EntryId;
                CharacterLinkedPoseEntryFragmentPlanDescriptor fragment = RequireFragment(
                    posePlan,
                    groupId,
                    implementationId,
                    entryId);
                entries[entryIndex] = new CharacterLinkedPoseEntryRuntimeLayout(
                    entryId,
                    fragment.Index,
                    new CharacterLinkedPoseLayoutRange(operationOffset, fragment.OperationCount),
                    new CharacterLinkedPoseLayoutRange(poseValueOffset, fragment.PoseValueCount),
                    new CharacterLinkedPoseLayoutRange(goalSetValueOffset, fragment.GoalSetValueCount),
                    new CharacterLinkedPoseLayoutRange(playerOffset, fragment.PlayerCount),
                    new CharacterLinkedPoseLayoutRange(stateMachineOffset, fragment.StateMachineCount),
                    new CharacterLinkedPoseLayoutRange(inertializationOffset, fragment.InertializationCount),
                    new CharacterLinkedPoseLayoutRange(rootOrientationWarpOffset, fragment.RootOrientationWarpCount),
                    new CharacterLinkedPoseLayoutRange(motionMatchingProviderOffset, fragment.MotionMatchingProviderCount),
                    new CharacterLinkedPoseLayoutRange(sourceDemandOffset, fragment.SourceIndices.Count),
                    new CharacterLinkedPoseLayoutRange(frameCompletionOffset, fragment.FrameCompletionCount),
                    new CharacterLinkedPoseLayoutRange(playerCompletionOffset, fragment.PlayerCompletionCount),
                    new CharacterLinkedPoseLayoutRange(stageCompletionOffset, fragment.StageCompletionCount),
                    new CharacterLinkedPoseLayoutRange(operationDiagnosticOffset, fragment.OperationDiagnosticCount),
                    new CharacterLinkedPoseLayoutRange(stageDiagnosticOffset, fragment.StageDiagnosticCount));
                operationOffset = checked(operationOffset + fragment.OperationCount);
                poseValueOffset = checked(poseValueOffset + fragment.PoseValueCount);
                goalSetValueOffset = checked(goalSetValueOffset + fragment.GoalSetValueCount);
                playerOffset = checked(playerOffset + fragment.PlayerCount);
                stateMachineOffset = checked(stateMachineOffset + fragment.StateMachineCount);
                inertializationOffset = checked(inertializationOffset + fragment.InertializationCount);
                rootOrientationWarpOffset = checked(rootOrientationWarpOffset + fragment.RootOrientationWarpCount);
                motionMatchingProviderOffset = checked(motionMatchingProviderOffset + fragment.MotionMatchingProviderCount);
                sourceDemandOffset = checked(sourceDemandOffset + fragment.SourceIndices.Count);
                frameCompletionOffset = checked(frameCompletionOffset + fragment.FrameCompletionCount);
                playerCompletionOffset = checked(playerCompletionOffset + fragment.PlayerCompletionCount);
                stageCompletionOffset = checked(stageCompletionOffset + fragment.StageCompletionCount);
                operationDiagnosticOffset = checked(operationDiagnosticOffset + fragment.OperationDiagnosticCount);
                stageDiagnosticOffset = checked(stageDiagnosticOffset + fragment.StageDiagnosticCount);
            }
            var capacity = new CharacterLinkedPoseRuntimeCapacity(
                operationOffset,
                poseValueOffset,
                goalSetValueOffset,
                playerOffset,
                stateMachineOffset,
                inertializationOffset,
                rootOrientationWarpOffset,
                motionMatchingProviderOffset,
                sourceDemandOffset,
                frameCompletionOffset,
                playerCompletionOffset,
                stageCompletionOffset,
                operationDiagnosticOffset,
                stageDiagnosticOffset);
            return new CharacterLinkedPoseImplementationRuntimeLayout(implementationId, in capacity, entries);
        }

        static CharacterLinkedPoseCompiledSelectorDescriptor RequireSelector(
            CharacterLinkedPoseProjectionPayload linkedPose,
            LinkedPoseGroupId groupId)
        {
            CharacterLinkedPoseCompiledSelectorDescriptor result = null;
            for (int i = 0; i < linkedPose.Selectors.Count; i++)
            {
                CharacterLinkedPoseCompiledSelectorDescriptor selector = linkedPose.Selectors[i];
                if (selector.GroupId != groupId)
                    continue;
                if (result != null)
                    throw new InvalidOperationException($"Linked Pose Group '{groupId}' has duplicate compiled selectors.");
                result = selector;
            }
            return result ?? throw new InvalidOperationException($"Linked Pose Group '{groupId}' has no compiled selector.");
        }

        static CharacterLinkedPoseInterfaceProjectionDescriptor RequireInterface(
            CharacterLinkedPoseProjectionPayload linkedPose,
            LinkedPoseInterfaceId interfaceId)
        {
            CharacterLinkedPoseInterfaceProjectionDescriptor result = null;
            for (int i = 0; i < linkedPose.Interfaces.Count; i++)
            {
                CharacterLinkedPoseInterfaceProjectionDescriptor linkedInterface = linkedPose.Interfaces[i];
                if (linkedInterface.InterfaceId != interfaceId)
                    continue;
                if (result != null)
                    throw new InvalidOperationException($"Linked Pose Interface '{interfaceId}' is duplicated.");
                result = linkedInterface;
            }
            return result ?? throw new InvalidOperationException($"Linked Pose Interface '{interfaceId}' is absent from the Projection.");
        }

        static CharacterLinkedPoseEntryFragmentPlanDescriptor RequireFragment(
            CharacterPresentationPosePlan posePlan,
            LinkedPoseGroupId groupId,
            LinkedPoseImplementationId implementationId,
            LinkedPoseEntryId entryId)
        {
            CharacterLinkedPoseEntryFragmentPlanDescriptor result = null;
            for (int i = 0; i < posePlan.LinkedPoseFragments.Count; i++)
            {
                CharacterLinkedPoseEntryFragmentPlanDescriptor fragment = posePlan.LinkedPoseFragments[i];
                if (fragment.GroupId != groupId || fragment.ImplementationId != implementationId || fragment.EntryId != entryId)
                    continue;
                if (result != null)
                {
                    throw new InvalidOperationException(
                        $"Linked Pose Group '{groupId}' Implementation '{implementationId}' Entry '{entryId}' has duplicate fragments.");
                }
                result = fragment;
            }
            return result ?? throw new InvalidOperationException(
                $"Linked Pose Group '{groupId}' Implementation '{implementationId}' Entry '{entryId}' has no compiled fragment.");
        }
    }
}
