using Unity.Collections;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal sealed class CharacterFullBodyIkGoalAssembler
    {
        internal CharacterFullBodyIkResult Assemble(
            NativeSlice<int> contributionIndices,
            NativeArray<CharacterFullBodyIkGoalContributionHeader> contributionHeaders,
            NativeArray<CharacterFullBodyIkGoal> contributionGoals,
            ulong frameSequence,
            ulong completionIdentity,
            FixedString64Bytes expectedRigId,
            FixedString64Bytes expectedRigRevision,
            int producerOperationIndex,
            int producerCallSiteIndex,
            NativeArray<CharacterFullBodyIkGoalSetHeader> goalSetOutput,
            NativeArray<CharacterFullBodyIkGoal> goalOutput,
            NativeArray<int> goalSetIndexOutput)
        {
            if (!contributionHeaders.IsCreated || !contributionGoals.IsCreated ||
                !goalSetOutput.IsCreated || goalSetOutput.Length != 1 ||
                !goalOutput.IsCreated ||
                goalOutput.Length != CharacterFullBodyIkGoalSetHeader.MaximumGoalCount ||
                !goalSetIndexOutput.IsCreated || goalSetIndexOutput.Length != 1)
            {
                return CharacterFullBodyIkResult.Fail(
                    CharacterFullBodyIkFailure.InvalidGoalWorkspace);
            }
            int count = 0;
            ushort occupiedSlots = 0;
            for (int contributionIndex = 0;
                 contributionIndex < contributionIndices.Length;
                 contributionIndex++)
            {
                int headerIndex = contributionIndices[contributionIndex];
                if ((uint)headerIndex >= (uint)contributionHeaders.Length)
                {
                    return CharacterFullBodyIkResult.Fail(
                        CharacterFullBodyIkFailure.InvalidGoalWorkspace,
                        contributionIndex);
                }
                CharacterFullBodyIkGoalContributionHeader header =
                    contributionHeaders[headerIndex];
                if (!header.IsValid ||
                    header.FrameSequence != frameSequence ||
                    header.CompletionIdentity != completionIdentity ||
                    header.Availability !=
                    CharacterFullBodyIkGoalContributionAvailability.Ready ||
                    !header.RigId.Equals(expectedRigId) ||
                    !header.RigRevision.Equals(expectedRigRevision) ||
                    header.GoalOffset > contributionGoals.Length - header.GoalCount)
                {
                    return CharacterFullBodyIkResult.Fail(
                        CharacterFullBodyIkFailure.GoalLineageMismatch,
                        contributionIndex);
                }
                if (count > goalOutput.Length - header.GoalCount)
                {
                    return CharacterFullBodyIkResult.Fail(
                        CharacterFullBodyIkFailure.InvalidGoalWorkspace,
                        contributionIndex);
                }
                for (int goalIndex = 0; goalIndex < header.GoalCount; goalIndex++)
                {
                    CharacterFullBodyIkGoal goal =
                        contributionGoals[header.GoalOffset + goalIndex];
                    if (!goal.IsValid)
                    {
                        return CharacterFullBodyIkResult.Fail(
                            CharacterFullBodyIkFailure.InvalidGoalWorkspace,
                            contributionIndex,
                            goal.Slot);
                    }
                    int slotBit = 1 << ((int)goal.Slot - 1);
                    if ((occupiedSlots & slotBit) != 0)
                    {
                        return CharacterFullBodyIkResult.Fail(
                            CharacterFullBodyIkFailure.DuplicateEffectorSlot,
                            contributionIndex,
                            goal.Slot);
                    }
                    occupiedSlots = (ushort)(occupiedSlots | slotBit);
                    goalOutput[count++] = goal;
                }
            }
            for (int i = count; i < goalOutput.Length; i++)
                goalOutput[i] = default;
            goalSetOutput[0] = new CharacterFullBodyIkGoalSetHeader(
                frameSequence,
                completionIdentity,
                expectedRigId,
                expectedRigRevision,
                producerOperationIndex,
                producerCallSiteIndex,
                0,
                count,
                CharacterFullBodyIkGoalSetAvailability.Ready);
            goalSetIndexOutput[0] = 0;
            return CharacterFullBodyIkResult.Success(count);
        }
    }
}
