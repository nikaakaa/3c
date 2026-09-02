using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Animation;

namespace ThirdPersonCharacter.Editor.CharacterSimulation
{
    internal readonly struct CharacterPoseLinkedFragmentStageRange
    {
        internal CharacterPoseLinkedFragmentStageRange(
            int fragmentIndex,
            int stageStart,
            int stageCount)
        {
            if (fragmentIndex < 0 || stageStart < 0 || stageCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(fragmentIndex));
            FragmentIndex = fragmentIndex;
            StageStart = stageStart;
            StageCount = stageCount;
        }

        internal int FragmentIndex { get; }
        internal int StageStart { get; }
        internal int StageCount { get; }
    }

    internal sealed class CharacterPoseStageSchedule
    {
        internal CharacterPoseStageSchedule(
            IReadOnlyList<CharacterPresentationPoseStage> stages,
            IReadOnlyList<CharacterPoseLinkedFragmentStageRange>
                fragmentRanges,
            int operationCount)
        {
            Stages = stages?.ToArray() ??
                throw new ArgumentNullException(nameof(stages));
            FragmentRanges = fragmentRanges?.ToArray() ??
                throw new ArgumentNullException(nameof(fragmentRanges));
            OperationCount = operationCount > 0
                ? operationCount
                : throw new ArgumentOutOfRangeException(
                    nameof(operationCount));
            int nextOperation = 0;
            for (int stageIndex = 0; stageIndex < Stages.Count; stageIndex++)
            {
                CharacterPresentationPoseStage stage = Stages[stageIndex];
                if (stage == null ||
                    stage.Index != stageIndex ||
                    stage.OperationStart != nextOperation ||
                    stage.OperationCount <= 0)
                {
                    throw new InvalidOperationException(
                        "Pose Stage Schedule is not a contiguous Operation partition.");
                }
                nextOperation = checked(
                    nextOperation + stage.OperationCount);
            }
            if (nextOperation != OperationCount)
            {
                throw new InvalidOperationException(
                    "Pose Stage Schedule does not cover every Operation exactly once.");
            }
        }

        internal IReadOnlyList<CharacterPresentationPoseStage> Stages
        {
            get;
        }
        internal IReadOnlyList<CharacterPoseLinkedFragmentStageRange>
            FragmentRanges { get; }
        internal int OperationCount { get; }
    }

    internal static class CharacterPoseStageSchedulePass
    {
        internal static CharacterPoseStageSchedule Run(
            IReadOnlyList<CharacterPresentationPoseOperation> operations,
            IReadOnlyList<CharacterLinkedPoseEntryFragmentPlanDescriptor>
                fragments)
        {
            if (operations == null || operations.Count == 0)
            {
                throw new InvalidOperationException(
                    "Pose Stage Schedule requires Operations.");
            }
            if (fragments == null)
                throw new ArgumentNullException(nameof(fragments));
            var stages = new List<CharacterPresentationPoseStage>();
            int operationStart = 0;
            int nativeOperationStart = 0;
            while (operationStart < operations.Count)
            {
                CharacterPresentationPoseOperation first =
                    RequireOperation(operations, operationStart);
                CharacterPoseExecutionDomain domain = first.ExecutionDomain;
                CharacterPoseSpace outputSpace = first.OutputPoseSpace;
                int operationEnd = operationStart + 1;
                while (operationEnd < operations.Count)
                {
                    CharacterPresentationPoseOperation candidate =
                        RequireOperation(operations, operationEnd);
                    if (candidate.ExecutionDomain != domain ||
                        candidate.OutputPoseSpace != outputSpace ||
                        candidate.LinkedPoseFragmentIndex !=
                            first.LinkedPoseFragmentIndex)
                    {
                        break;
                    }
                    operationEnd++;
                }

                CharacterPoseSpace inputSpace = CharacterPoseSpace.None;
                int nativeOperationCount = 0;
                int minPoseValue = int.MaxValue;
                int maxPoseValue = -1;
                for (int operationIndex = operationStart;
                     operationIndex < operationEnd;
                     operationIndex++)
                {
                    CharacterPresentationPoseOperation operation =
                        operations[operationIndex];
                    if (inputSpace == CharacterPoseSpace.None &&
                        operation.InputPoseSpace != CharacterPoseSpace.None)
                    {
                        inputSpace = operation.InputPoseSpace;
                    }
                    if (IsNativePoseOperation(operation.Code))
                        nativeOperationCount++;
                    if (operation.OutputValueIndex < 0)
                        continue;
                    minPoseValue = Math.Min(
                        minPoseValue,
                        operation.OutputValueIndex);
                    maxPoseValue = Math.Max(
                        maxPoseValue,
                        operation.OutputValueIndex);
                }

                stages.Add(new CharacterPresentationPoseStage(
                    stages.Count,
                    domain,
                    inputSpace,
                    outputSpace,
                    operationStart,
                    operationEnd - operationStart,
                    nativeOperationStart,
                    nativeOperationCount,
                    maxPoseValue < 0 ? 0 : minPoseValue,
                    maxPoseValue < 0
                        ? 0
                        : maxPoseValue - minPoseValue + 1));
                nativeOperationStart += nativeOperationCount;
                operationStart = operationEnd;
            }
            CharacterPoseLinkedFragmentStageRange[] ranges =
                BuildFragmentRanges(fragments, stages);
            return new CharacterPoseStageSchedule(
                stages,
                ranges,
                operations.Count);
        }

        static CharacterPoseLinkedFragmentStageRange[] BuildFragmentRanges(
            IReadOnlyList<CharacterLinkedPoseEntryFragmentPlanDescriptor>
                fragments,
            IReadOnlyList<CharacterPresentationPoseStage> stages)
        {
            var ranges =
                new CharacterPoseLinkedFragmentStageRange[fragments.Count];
            int stageIndex = 0;
            for (int fragmentIndex = 0;
                 fragmentIndex < fragments.Count;
                 fragmentIndex++)
            {
                CharacterLinkedPoseEntryFragmentPlanDescriptor fragment =
                    fragments[fragmentIndex] ??
                    throw new InvalidOperationException(
                        $"Linked Pose fragment #{fragmentIndex} is missing.");
                if (fragment.Index != fragmentIndex ||
                    fragment.OperationCount <= 0)
                {
                    throw new InvalidOperationException(
                        $"Linked Pose fragment #{fragmentIndex} Operation range is invalid.");
                }
                int operationEnd = checked(
                    fragment.OperationStart + fragment.OperationCount);
                while (stageIndex < stages.Count &&
                       stages[stageIndex].OperationStart +
                       stages[stageIndex].OperationCount <=
                       fragment.OperationStart)
                {
                    stageIndex++;
                }
                int stageStart = stageIndex;
                while (stageIndex < stages.Count &&
                       stages[stageIndex].OperationStart < operationEnd)
                {
                    CharacterPresentationPoseStage stage = stages[stageIndex];
                    if (stage.OperationStart < fragment.OperationStart ||
                        stage.OperationStart + stage.OperationCount >
                            operationEnd)
                    {
                        throw new InvalidOperationException(
                            $"Linked Pose fragment #{fragment.Index} does not own an isolated Stage range.");
                    }
                    stageIndex++;
                }
                ranges[fragmentIndex] =
                    new CharacterPoseLinkedFragmentStageRange(
                        fragmentIndex,
                        stageStart,
                        stageIndex - stageStart);
            }
            return ranges;
        }

        static CharacterPresentationPoseOperation RequireOperation(
            IReadOnlyList<CharacterPresentationPoseOperation> operations,
            int index)
        {
            CharacterPresentationPoseOperation operation = operations[index];
            if (operation == null || operation.Index != index)
            {
                throw new InvalidOperationException(
                    $"Pose Operation #{index} is missing or not linearly indexed.");
            }
            return operation;
        }

        static bool IsNativePoseOperation(
            CharacterPoseOperationCode code) => code switch
        {
            CharacterPoseOperationCode.SelectedPosePlayer or
            CharacterPoseOperationCode.BlendSpacePlayer or
            CharacterPoseOperationCode.ClipPlayer or
            CharacterPoseOperationCode.BlendStack or
            CharacterPoseOperationCode.AnimationSlot or
            CharacterPoseOperationCode.Inertialization or
            CharacterPoseOperationCode.BlendPose or
            CharacterPoseOperationCode.LayeredBoneBlend or
            CharacterPoseOperationCode.AdditivePose or
            CharacterPoseOperationCode.PoseParameterResolve or
            CharacterPoseOperationCode.ModifyBone or
            CharacterPoseOperationCode.RootOrientationWarp or
            CharacterPoseOperationCode.FootPlacement or
            CharacterPoseOperationCode.PoseBoneIKGoals or
            CharacterPoseOperationCode.FullBodyIkGoalAssembler or
            CharacterPoseOperationCode.FullBodyIK or
            CharacterPoseOperationCode.LocalToComponentPose or
            CharacterPoseOperationCode.ComponentToLocalPose or
            CharacterPoseOperationCode.StatePoseOutput or
            CharacterPoseOperationCode.PoseStateMachine or
            CharacterPoseOperationCode.LinkedPoseCall or
            CharacterPoseOperationCode.MotionMatchingPose or
            CharacterPoseOperationCode.PoseHistoryRead or
            CharacterPoseOperationCode.OutputPose => true,
            _ => false
        };
    }
}
