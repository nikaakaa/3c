using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.Pipeline.Animation;

namespace ThirdPersonCharacter.Editor.CharacterSimulation
{
    internal sealed class CharacterPoseValueLifetime
    {
        internal CharacterPoseValueLifetime(
            IReadOnlyList<int> poseProducers,
            IReadOnlyList<int> poseLastUses,
            IReadOnlyList<int> goalContributionProducers,
            IReadOnlyList<int> goalContributionLastUses,
            IReadOnlyList<int> goalSetProducers,
            IReadOnlyList<int> goalSetLastUses,
            int parameterValueCount,
            int outputPoseValueIndex,
            int operationCount)
        {
            PoseProducers = CopyPair(
                poseProducers,
                poseLastUses,
                out IReadOnlyList<int> poseUses);
            PoseLastUses = poseUses;
            GoalContributionProducers = CopyPair(
                goalContributionProducers,
                goalContributionLastUses,
                out IReadOnlyList<int> contributionUses);
            GoalContributionLastUses = contributionUses;
            GoalSetProducers = CopyPair(
                goalSetProducers,
                goalSetLastUses,
                out IReadOnlyList<int> goalSetUses);
            GoalSetLastUses = goalSetUses;
            if (parameterValueCount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(parameterValueCount));
            }
            ParameterAddresses = Enumerable.Range(
                0,
                parameterValueCount).ToArray();
            DiscontinuityProducers = PoseProducers;
            DiscontinuityLastUses = PoseLastUses;
            OutputPoseValueIndex = outputPoseValueIndex >= 0
                ? outputPoseValueIndex
                : throw new ArgumentOutOfRangeException(
                    nameof(outputPoseValueIndex));
            OperationCount = operationCount > 0
                ? operationCount
                : throw new ArgumentOutOfRangeException(
                    nameof(operationCount));
        }

        internal IReadOnlyList<int> PoseProducers { get; }
        internal IReadOnlyList<int> PoseLastUses { get; }
        internal IReadOnlyList<int> GoalContributionProducers { get; }
        internal IReadOnlyList<int> GoalContributionLastUses { get; }
        internal IReadOnlyList<int> GoalSetProducers { get; }
        internal IReadOnlyList<int> GoalSetLastUses { get; }
        internal IReadOnlyList<int> ParameterAddresses { get; }
        internal IReadOnlyList<int> DiscontinuityProducers { get; }
        internal IReadOnlyList<int> DiscontinuityLastUses { get; }
        internal int OutputPoseValueIndex { get; }
        internal int OperationCount { get; }

        static IReadOnlyList<int> CopyPair(
            IReadOnlyList<int> producers,
            IReadOnlyList<int> lastUses,
            out IReadOnlyList<int> copiedLastUses)
        {
            if (producers == null ||
                lastUses == null ||
                producers.Count != lastUses.Count)
            {
                throw new ArgumentException(
                    "Pose Value lifetime pages do not match.");
            }
            int[] producerCopy = producers.ToArray();
            int[] lastUseCopy = lastUses.ToArray();
            for (int index = 0; index < producerCopy.Length; index++)
            {
                if (producerCopy[index] < 0 ||
                    lastUseCopy[index] < producerCopy[index])
                {
                    throw new InvalidOperationException(
                        $"Pose Value lifetime #{index} is invalid.");
                }
            }
            copiedLastUses = lastUseCopy;
            return producerCopy;
        }
    }

    internal sealed class CharacterPoseWorkspacePlan
    {
        internal CharacterPoseWorkspacePlan(
            int poseValueCapacity,
            int parameterValueCapacity,
            int contributionCapacity,
            int motionMatchingContributionCapacity,
            int frameCacheCapacity,
            int rigPoseBoneCount,
            int rigPhysicalBoneCount,
            int rigVirtualBoneCount,
            int playerStateCount,
            int inertializationStateCount,
            int stateMachineStateCount,
            int stateMachineTransitionCount,
            int blendStackEntryCount,
            int sourceCatalogCount,
            int constraintOperationCount,
            int constraintGoalCapacity,
            int diagnosticStageCount,
            int diagnosticOperationCount,
            CharacterPoseValueLifetime valueLifetime)
        {
            if (poseValueCapacity <= 0 ||
                parameterValueCapacity <= 0 ||
                contributionCapacity <= 0 ||
                motionMatchingContributionCapacity < 0 ||
                frameCacheCapacity <= 0 ||
                rigPoseBoneCount <= 0 ||
                rigPhysicalBoneCount <= 0 ||
                rigVirtualBoneCount < 0 ||
                rigPhysicalBoneCount + rigVirtualBoneCount !=
                    rigPoseBoneCount ||
                playerStateCount <= 0 ||
                inertializationStateCount < 0 ||
                stateMachineStateCount < 0 ||
                stateMachineTransitionCount < 0 ||
                blendStackEntryCount < 0 ||
                sourceCatalogCount <= 0 ||
                constraintOperationCount <= 0 ||
                constraintGoalCapacity <= 0 ||
                diagnosticStageCount <= 0 ||
                diagnosticOperationCount != frameCacheCapacity)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(poseValueCapacity));
            }
            PoseValueCapacity = poseValueCapacity;
            ParameterValueCapacity = parameterValueCapacity;
            ContributionCapacity = contributionCapacity;
            MotionMatchingContributionCapacity =
                motionMatchingContributionCapacity;
            FrameCacheCapacity = frameCacheCapacity;
            RigPoseBoneCount = rigPoseBoneCount;
            RigPhysicalBoneCount = rigPhysicalBoneCount;
            RigVirtualBoneCount = rigVirtualBoneCount;
            PlayerStateCount = playerStateCount;
            InertializationStateCount = inertializationStateCount;
            StateMachineStateCount = stateMachineStateCount;
            StateMachineTransitionCount = stateMachineTransitionCount;
            BlendStackEntryCount = blendStackEntryCount;
            SourceCatalogCount = sourceCatalogCount;
            ConstraintOperationCount = constraintOperationCount;
            ConstraintGoalCapacity = constraintGoalCapacity;
            DiagnosticStageCount = diagnosticStageCount;
            DiagnosticOperationCount = diagnosticOperationCount;
            ValueLifetime = valueLifetime ??
                throw new ArgumentNullException(nameof(valueLifetime));
        }

        internal int PoseValueCapacity { get; }
        internal int ParameterValueCapacity { get; }
        internal int ContributionCapacity { get; }
        internal int MotionMatchingContributionCapacity { get; }
        internal int FrameCacheCapacity { get; }
        internal int RigPoseBoneCount { get; }
        internal int RigPhysicalBoneCount { get; }
        internal int RigVirtualBoneCount { get; }
        internal int PlayerStateCount { get; }
        internal int InertializationStateCount { get; }
        internal int StateMachineStateCount { get; }
        internal int StateMachineTransitionCount { get; }
        internal int BlendStackEntryCount { get; }
        internal int SourceCatalogCount { get; }
        internal int ConstraintOperationCount { get; }
        internal int ConstraintGoalCapacity { get; }
        internal int DiagnosticStageCount { get; }
        internal int DiagnosticOperationCount { get; }
        internal CharacterPoseValueLifetime ValueLifetime { get; }
    }

    internal static class CharacterPoseValueLifetimePass
    {
        internal static CharacterPoseValueLifetime Run(
            IReadOnlyList<CharacterPoseBoundOperation> operations,
            CharacterPoseStageSchedule schedule,
            int poseValueCount,
            int parameterValueCount,
            int goalContributionValueCount,
            int goalSetValueCount,
            IReadOnlyList<int> goalContributionInputValueIndices,
            IReadOnlyList<CharacterLinkedPoseCallPlanDescriptor> linkedCalls,
            IReadOnlyList<CharacterLinkedPoseEntryFragmentPlanDescriptor>
                linkedFragments,
            int outputOperationIndex)
        {
            if (operations == null ||
                operations.Count == 0 ||
                schedule == null ||
                schedule.OperationCount != operations.Count ||
                poseValueCount <= 0 ||
                parameterValueCount <= 0 ||
                goalContributionValueCount < 0 ||
                goalSetValueCount < 0 ||
                goalContributionInputValueIndices == null ||
                linkedCalls == null ||
                linkedFragments == null)
            {
                throw new ArgumentException(
                    "Pose Value Lifetime input is invalid.");
            }
            int[] poseProducer = EmptyPage(poseValueCount);
            int[] poseLastUse = EmptyPage(poseValueCount);
            int[] contributionProducer =
                EmptyPage(goalContributionValueCount);
            int[] contributionLastUse =
                EmptyPage(goalContributionValueCount);
            int[] goalSetProducer = EmptyPage(goalSetValueCount);
            int[] goalSetLastUse = EmptyPage(goalSetValueCount);
            for (int operationIndex = 0;
                 operationIndex < operations.Count;
                 operationIndex++)
            {
                CharacterPoseBoundOperation operation =
                    operations[operationIndex];
                if (operation == null || operation.Index != operationIndex)
                {
                    throw new InvalidOperationException(
                        $"Pose Operation #{operationIndex} is missing or not linearly indexed.");
                }
                RegisterOutput(
                    operation.OutputValueIndex,
                    operationIndex,
                    poseProducer,
                    poseLastUse,
                    operation.NodeId,
                    "Pose");
                RegisterInput(
                    operation.InputValueIndexA,
                    operationIndex,
                    poseProducer,
                    poseLastUse,
                    operation.NodeId,
                    "Pose");
                RegisterInput(
                    operation.InputValueIndexB,
                    operationIndex,
                    poseProducer,
                    poseLastUse,
                    operation.NodeId,
                    "Pose");
                RegisterOutput(
                    operation.OutputFullBodyIkGoalContributionValueIndex,
                    operationIndex,
                    contributionProducer,
                    contributionLastUse,
                    operation.NodeId,
                    "Full Body IK Goal Contribution");
                RegisterOutput(
                    operation.OutputFullBodyIkGoalSetValueIndex,
                    operationIndex,
                    goalSetProducer,
                    goalSetLastUse,
                    operation.NodeId,
                    "Full Body IK Goal Set");
                for (int inputIndex = 0;
                     inputIndex <
                     operation.FullBodyIkGoalContributionInputCount;
                     inputIndex++)
                {
                    int flatIndex = checked(
                        operation.FullBodyIkGoalContributionInputStart +
                        inputIndex);
                    if ((uint)flatIndex >=
                        (uint)goalContributionInputValueIndices.Count)
                    {
                        throw new InvalidOperationException(
                            $"Pose Operation '{operation.NodeId}' Goal Contribution input range is invalid.");
                    }
                    RegisterInput(
                        goalContributionInputValueIndices[flatIndex],
                        operationIndex,
                        contributionProducer,
                        contributionLastUse,
                        operation.NodeId,
                        "Full Body IK Goal Contribution");
                }
                RegisterInput(
                    operation.InputFullBodyIkGoalSetValueIndex,
                    operationIndex,
                    goalSetProducer,
                    goalSetLastUse,
                    operation.NodeId,
                    "Full Body IK Goal Set");
                if (operation.Code !=
                    CharacterPoseOperationCode.LinkedPoseCall)
                {
                    continue;
                }
                if ((uint)operation.LinkedPoseCallIndex >=
                    (uint)linkedCalls.Count)
                {
                    throw new InvalidOperationException(
                        $"Linked Pose Call '{operation.NodeId}' index is invalid.");
                }
                CharacterLinkedPoseCallPlanDescriptor call =
                    linkedCalls[operation.LinkedPoseCallIndex];
                for (int fragmentOffset = 0;
                     fragmentOffset < call.FragmentIndices.Count;
                     fragmentOffset++)
                {
                    int fragmentIndex =
                        call.FragmentIndices[fragmentOffset];
                    if ((uint)fragmentIndex >=
                        (uint)linkedFragments.Count)
                    {
                        throw new InvalidOperationException(
                            $"Linked Pose Call '{operation.NodeId}' Fragment index is invalid.");
                    }
                    CharacterLinkedPoseEntryFragmentPlanDescriptor fragment =
                        linkedFragments[fragmentIndex];
                    for (int outputIndex = 0;
                         outputIndex < fragment.Outputs.Count;
                         outputIndex++)
                    {
                        CharacterLinkedPosePortValueBinding binding =
                            fragment.Outputs[outputIndex];
                        if (binding.Kind == CharacterPosePortKind.LocalPose ||
                            binding.Kind ==
                            CharacterPosePortKind.ComponentPose)
                        {
                            RegisterInput(
                                binding.ValueIndex,
                                operationIndex,
                                poseProducer,
                                poseLastUse,
                                operation.NodeId,
                                "Pose");
                        }
                        else if (binding.Kind ==
                                     CharacterPosePortKind.FullBodyIkGoals ||
                                 binding.Kind ==
                                     CharacterPosePortKind
                                         .FullBodyIkGoalContribution)
                        {
                            throw new InvalidOperationException(
                                $"Linked Pose Call '{operation.NodeId}' cannot pass Full Body IK Goals or Contributions.");
                        }
                    }
                }
            }
            if ((uint)outputOperationIndex >= (uint)operations.Count)
            {
                throw new InvalidOperationException(
                    "Pose output Operation is outside the linear schedule.");
            }
            CharacterPoseBoundOperation output =
                operations[outputOperationIndex];
            if (output.OutputValueIndex != poseValueCount - 1)
            {
                throw new InvalidOperationException(
                    "Pose output Operation does not publish the final Pose Value.");
            }
            poseLastUse[output.OutputValueIndex] = operations.Count;
            RequireComplete(poseProducer, poseLastUse, false, "Pose");
            RequireComplete(
                contributionProducer,
                contributionLastUse,
                true,
                "Full Body IK Goal Contribution");
            RequireComplete(
                goalSetProducer,
                goalSetLastUse,
                true,
                "Full Body IK Goal Set");
            return new CharacterPoseValueLifetime(
                poseProducer,
                poseLastUse,
                contributionProducer,
                contributionLastUse,
                goalSetProducer,
                goalSetLastUse,
                parameterValueCount,
                output.OutputValueIndex,
                operations.Count);
        }

        static int[] EmptyPage(int count) =>
            Enumerable.Repeat(-1, count).ToArray();

        static void RegisterOutput(
            int valueIndex,
            int operationIndex,
            int[] producer,
            int[] lastUse,
            PoseNodeId nodeId,
            string label)
        {
            if (valueIndex < 0)
                return;
            if ((uint)valueIndex >= (uint)producer.Length ||
                producer[valueIndex] >= 0)
            {
                throw new InvalidOperationException(
                    $"Pose Node '{nodeId}' publishes an invalid or duplicate {label} Value '{valueIndex}'.");
            }
            producer[valueIndex] = operationIndex;
            lastUse[valueIndex] = operationIndex;
        }

        static void RegisterInput(
            int valueIndex,
            int operationIndex,
            int[] producer,
            int[] lastUse,
            PoseNodeId nodeId,
            string label)
        {
            if (valueIndex < 0)
                return;
            if ((uint)valueIndex >= (uint)producer.Length ||
                producer[valueIndex] < 0 ||
                producer[valueIndex] >= operationIndex)
            {
                throw new InvalidOperationException(
                    $"Pose Node '{nodeId}' consumes {label} Value '{valueIndex}' before it is published.");
            }
            lastUse[valueIndex] = Math.Max(
                lastUse[valueIndex],
                operationIndex);
        }

        static void RequireComplete(
            IReadOnlyList<int> producer,
            IReadOnlyList<int> lastUse,
            bool requireConsumer,
            string label)
        {
            for (int index = 0; index < producer.Count; index++)
            {
                if (producer[index] < 0 ||
                    lastUse[index] < producer[index] ||
                    requireConsumer && lastUse[index] == producer[index])
                {
                    throw new InvalidOperationException(
                        $"{label} Value '{index}' has an invalid lifetime.");
                }
            }
        }
    }

    internal static class CharacterPoseWorkspacePlanPass
    {
        internal static CharacterPoseWorkspacePlan Run(
            CharacterPoseValueLifetime lifetime,
            CharacterPoseStageSchedule schedule,
            CharacterAnimationRigDefinition rig,
            IReadOnlyList<CharacterPoseBoundOperation> operations,
            IReadOnlyList<AnimationBlendNodePayload> blendNodes,
            int playerStateCount,
            int inertializationStateCount,
            IReadOnlyList<CharacterPoseStateMachineDescriptor> stateMachines,
            int sourceCatalogCount,
            IReadOnlyList<CharacterPresentationPoseBoneIkGoalsDescriptor>
                poseBoneIkGoals,
            IReadOnlyList<CharacterPresentationFootPlacementDescriptor>
                footPlacements,
            IReadOnlyList<CharacterPresentationFullBodyIkDescriptor>
                fullBodyIks,
            int constraintGoalCapacity,
            int motionMatchingContributionCapacity)
        {
            if (lifetime == null ||
                schedule == null ||
                !rig ||
                operations == null ||
                operations.Count != lifetime.OperationCount ||
                schedule.OperationCount != operations.Count ||
                blendNodes == null ||
                playerStateCount <= 0 ||
                inertializationStateCount < 0 ||
                stateMachines == null ||
                sourceCatalogCount <= 0 ||
                poseBoneIkGoals == null ||
                footPlacements == null ||
                fullBodyIks == null ||
                constraintGoalCapacity <= 0 ||
                motionMatchingContributionCapacity < 0)
            {
                throw new ArgumentException(
                    "Pose Workspace Plan input is invalid.");
            }
            rig.RequireValid();
            int contributionCapacityPerValue = 0;
            int blendStackEntryCount = 0;
            for (int operationIndex = 0;
                 operationIndex < operations.Count;
                 operationIndex++)
            {
                CharacterPoseBoundOperation operation =
                    operations[operationIndex];
                if (operation.Code ==
                        CharacterPoseOperationCode.SelectedPosePlayer ||
                    operation.Code ==
                        CharacterPoseOperationCode.BlendSpacePlayer ||
                    operation.Code ==
                        CharacterPoseOperationCode.ClipPlayer)
                {
                    contributionCapacityPerValue = checked(
                        contributionCapacityPerValue + 1);
                    continue;
                }
                if (operation.Code !=
                        CharacterPoseOperationCode.BlendStack &&
                    operation.Code !=
                        CharacterPoseOperationCode.AnimationSlot)
                {
                    continue;
                }
                if ((uint)operation.BlendNodeIndex >=
                    (uint)blendNodes.Count)
                {
                    throw new InvalidOperationException(
                        $"Pose Blend Stack '{operation.NodeId}' has no policy payload.");
                }
                AnimationBlendNodePayload blendNode =
                    blendNodes[operation.BlendNodeIndex];
                if (blendNode?.StackPolicy == null ||
                    blendNode.StackPolicy.MaxActiveSourceEntries <= 0)
                {
                    throw new InvalidOperationException(
                        $"Pose Blend Stack '{operation.NodeId}' has an invalid contribution capacity.");
                }
                contributionCapacityPerValue = checked(
                    contributionCapacityPerValue +
                    blendNode.StackPolicy.MaxActiveSourceEntries +
                    1);
                blendStackEntryCount = checked(
                    blendStackEntryCount +
                    blendNode.StackPolicy.MaxActiveSourceEntries);
            }
            contributionCapacityPerValue = checked(
                contributionCapacityPerValue +
                motionMatchingContributionCapacity);
            if (contributionCapacityPerValue <= 0 ||
                lifetime.OutputPoseValueIndex <= 0)
            {
                throw new InvalidOperationException(
                    "Pose Workspace requires Player contributions and a non-final Pose Value.");
            }
            int stateMachineStateCount = stateMachines.Sum(
                value => value?.StateWorkspaceCount ?? 0);
            int stateMachineTransitionCount = stateMachines.Sum(
                value => value?.TransitionWorkspaceCount ?? 0);
            int constraintOperationCount = checked(
                poseBoneIkGoals.Count +
                footPlacements.Count +
                fullBodyIks.Count +
                1);
            return new CharacterPoseWorkspacePlan(
                lifetime.OutputPoseValueIndex,
                lifetime.ParameterAddresses.Count,
                checked(
                    lifetime.OutputPoseValueIndex *
                        contributionCapacityPerValue),
                motionMatchingContributionCapacity,
                operations.Count,
                rig.PoseBoneCount,
                rig.PhysicalBoneCount,
                rig.VirtualBoneCount,
                playerStateCount,
                inertializationStateCount,
                stateMachineStateCount,
                stateMachineTransitionCount,
                blendStackEntryCount,
                sourceCatalogCount,
                constraintOperationCount,
                constraintGoalCapacity,
                schedule.Stages.Count,
                operations.Count,
                lifetime);
        }
    }
}
