using System;
using ThirdPersonCharacter.Pipeline.Presentation;

namespace ThirdPersonCharacter.Pipeline.Animation.MotionMatching
{
    internal readonly struct MotionMatchingPoseStateDemand
    {
        internal MotionMatchingPoseStateDemand(
            string providerId,
            int stateMachineIndex,
            int stateIndex,
            int playerIndex,
            PoseNodeId playerNodeId,
            float relevanceWeight,
            ulong relevanceGeneration,
            ulong resetSequence)
        {
            if (string.IsNullOrWhiteSpace(providerId) || stateMachineIndex < 0 || stateIndex < 0 ||
                playerIndex < 0 || !playerNodeId.IsValid || !float.IsFinite(relevanceWeight) ||
                relevanceWeight <= 0f || relevanceWeight > 1f || relevanceGeneration == 0)
            {
                throw new ArgumentException("Motion Matching Pose State demand is invalid.");
            }
            ProviderId = providerId;
            StateMachineIndex = stateMachineIndex;
            StateIndex = stateIndex;
            PlayerIndex = playerIndex;
            PlayerNodeId = playerNodeId;
            RelevanceWeight = relevanceWeight;
            RelevanceGeneration = relevanceGeneration;
            ResetSequence = resetSequence;
        }

        internal string ProviderId { get; }
        internal int StateMachineIndex { get; }
        internal int StateIndex { get; }
        internal int PlayerIndex { get; }
        internal PoseNodeId PlayerNodeId { get; }
        internal float RelevanceWeight { get; }
        internal ulong RelevanceGeneration { get; }
        internal ulong ResetSequence { get; }
    }

    internal readonly struct MotionMatchingPoseStateDemandBatch
    {
        readonly MotionMatchingPoseStateDemand[] m_Items;

        internal MotionMatchingPoseStateDemandBatch(
            ulong presentationFrame,
            ulong resetSequence,
            MotionMatchingPoseStateDemand[] items,
            int count)
        {
            if (presentationFrame == 0 || items == null || count < 0 || count > items.Length)
                throw new ArgumentException("Motion Matching Pose State demand batch is invalid.");
            PresentationFrame = presentationFrame;
            ResetSequence = resetSequence;
            m_Items = items;
            Count = count;
        }

        internal ulong PresentationFrame { get; }
        internal ulong ResetSequence { get; }
        internal int Count { get; }

        internal MotionMatchingPoseStateDemand GetDemand(int index) =>
            (uint)index < (uint)Count
                ? m_Items[index]
                : throw new ArgumentOutOfRangeException(nameof(index));
    }

    internal readonly struct MotionMatchingSelectionBatchItem
    {
        internal MotionMatchingSelectionBatchItem(
            string providerId,
            int stateMachineIndex,
            int stateIndex,
            int playerIndex,
            PoseNodeId playerNodeId,
            PoseSourceProviderDemandGeneration demandGeneration,
            in PresentationPoseSourceSample sourceSample,
            bool submitToPlayer,
            bool requiresHistory,
            int[] historyBoneIndices,
            UnityEngine.Vector3[] historyBonePositions)
        {
            if (string.IsNullOrWhiteSpace(providerId) || stateMachineIndex < 0 || stateIndex < 0 ||
                playerIndex < 0 || !playerNodeId.IsValid ||
                !demandGeneration.IsValid ||
                sourceSample == null || !sourceSample.IsValid ||
                sourceSample.ProviderId !=
                    new PresentationPoseSourceProviderId(providerId) ||
                sourceSample.PlayerNodeId != playerNodeId ||
                sourceSample.SourceKind !=
                    AnimationPoseSourceKind.MotionMatching ||
                requiresHistory && (historyBoneIndices == null || historyBonePositions == null ||
                                    historyBoneIndices.Length == 0 ||
                                    historyBoneIndices.Length != historyBonePositions.Length))
            {
                throw new ArgumentException("Motion Matching Selection batch item is invalid.");
            }
            ProviderId = providerId;
            StateMachineIndex = stateMachineIndex;
            StateIndex = stateIndex;
            PlayerIndex = playerIndex;
            PlayerNodeId = playerNodeId;
            DemandGeneration = demandGeneration;
            SourceSample = sourceSample;
            SubmitToPlayer = submitToPlayer;
            RequiresHistory = requiresHistory;
            HistoryBoneIndices = historyBoneIndices;
            HistoryBonePositions = historyBonePositions;
        }

        internal string ProviderId { get; }
        internal int StateMachineIndex { get; }
        internal int StateIndex { get; }
        internal int PlayerIndex { get; }
        internal PoseNodeId PlayerNodeId { get; }
        internal PoseSourceProviderDemandGeneration
            DemandGeneration { get; }
        internal PresentationPoseSourceSample SourceSample { get; }
        internal AnimationPoseSourceId SourceIdentity =>
            new AnimationPoseSourceId(
                SourceSample.SourceIndex,
                SourceSample.SourceKind,
                new AnimationPoseSelectionGeneration(
                    SourceSample.SourceGeneration.Value));
        internal bool SubmitToPlayer { get; }
        internal bool RequiresHistory { get; }
        internal int[] HistoryBoneIndices { get; }
        internal UnityEngine.Vector3[] HistoryBonePositions { get; }
    }

    internal readonly struct MotionMatchingFrameResolution
    {
        readonly MotionMatchingSelectionBatchItem[] m_Selections;

        internal MotionMatchingFrameResolution(
            ulong presentationFrame,
            ulong resetSequence,
            ulong completionIdentity,
            MotionMatchingSelectionBatchItem[] selections,
            int selectionCount,
            int resolvedProviderCount,
            bool requiresHistoryCompletion)
        {
            if (presentationFrame == 0 || completionIdentity == 0 || selections == null ||
                selectionCount < 0 || selectionCount > selections.Length || resolvedProviderCount < 0)
            {
                throw new ArgumentException("Motion Matching frame resolution is invalid.");
            }
            PresentationFrame = presentationFrame;
            ResetSequence = resetSequence;
            CompletionIdentity = completionIdentity;
            m_Selections = selections;
            SelectionCount = selectionCount;
            ResolvedProviderCount = resolvedProviderCount;
            RequiresHistoryCompletion = requiresHistoryCompletion;
        }

        internal ulong PresentationFrame { get; }
        internal ulong ResetSequence { get; }
        internal ulong CompletionIdentity { get; }
        internal int SelectionCount { get; }
        internal int ResolvedProviderCount { get; }
        internal bool RequiresHistoryCompletion { get; }

        internal MotionMatchingSelectionBatchItem GetSelection(int index) =>
            (uint)index < (uint)SelectionCount
                ? m_Selections[index]
                : throw new ArgumentOutOfRangeException(nameof(index));
    }

    internal readonly struct MotionMatchingPosePlanSourceUsage
    {
        internal MotionMatchingPosePlanSourceUsage(
            PoseNodeId playerNodeId,
            AnimationPoseSourceId sourceId,
            ulong completionIdentity)
        {
            if (!playerNodeId.IsValid || !sourceId.IsValid || completionIdentity == 0)
                throw new ArgumentException("Motion Matching Pose Plan source usage is invalid.");
            PlayerNodeId = playerNodeId;
            SourceId = sourceId;
            CompletionIdentity = completionIdentity;
        }

        internal PoseNodeId PlayerNodeId { get; }
        internal AnimationPoseSourceId SourceId { get; }
        internal ulong CompletionIdentity { get; }
    }

    internal readonly struct MotionMatchingPreparedFrameCompletion
    {
        internal MotionMatchingPreparedFrameCompletion(
            ulong presentationFrame,
            ulong resetSequence,
            ulong selectionCompletionIdentity,
            ulong posePlanCompletionIdentity,
            int historyCount)
        {
            if (presentationFrame == 0 ||
                selectionCompletionIdentity == 0 ||
                posePlanCompletionIdentity == 0 ||
                historyCount < 0)
            {
                throw new ArgumentException(
                    "Motion Matching prepared frame completion is invalid.");
            }
            PresentationFrame = presentationFrame;
            ResetSequence = resetSequence;
            SelectionCompletionIdentity =
                selectionCompletionIdentity;
            PosePlanCompletionIdentity = posePlanCompletionIdentity;
            HistoryCount = historyCount;
        }

        internal ulong PresentationFrame { get; }
        internal ulong ResetSequence { get; }
        internal ulong SelectionCompletionIdentity { get; }
        internal ulong PosePlanCompletionIdentity { get; }
        internal int HistoryCount { get; }
        internal bool IsValid =>
            PresentationFrame != 0 &&
            SelectionCompletionIdentity != 0 &&
            PosePlanCompletionIdentity != 0 &&
            HistoryCount >= 0;
    }

    internal readonly struct MotionMatchingPosePlanHistoryCompletion
    {
        internal MotionMatchingPosePlanHistoryCompletion(
            string providerId,
            PoseNodeId playerNodeId,
            AnimationPoseSourceId sourceId,
            ulong selectionCompletionIdentity,
            ulong posePlanCompletionIdentity,
            bool poseAvailable,
            in AnimationFootPlacementSample footPlacement)
        {
            if (string.IsNullOrWhiteSpace(providerId) || !playerNodeId.IsValid || !sourceId.IsValid ||
                selectionCompletionIdentity == 0 || posePlanCompletionIdentity == 0 ||
                poseAvailable && !footPlacement.IsValid)
            {
                throw new ArgumentException("Motion Matching Pose Plan history completion is invalid.");
            }
            ProviderId = providerId;
            PlayerNodeId = playerNodeId;
            SourceId = sourceId;
            SelectionCompletionIdentity = selectionCompletionIdentity;
            PosePlanCompletionIdentity = posePlanCompletionIdentity;
            PoseAvailable = poseAvailable;
            FootPlacement = footPlacement;
        }

        internal string ProviderId { get; }
        internal PoseNodeId PlayerNodeId { get; }
        internal AnimationPoseSourceId SourceId { get; }
        internal ulong SelectionCompletionIdentity { get; }
        internal ulong PosePlanCompletionIdentity { get; }
        internal bool PoseAvailable { get; }
        internal AnimationFootPlacementSample FootPlacement { get; }
    }

    internal readonly struct MotionMatchingPosePlanCompletion
    {
        readonly MotionMatchingPosePlanSourceUsage[] m_SourceUsages;
        readonly MotionMatchingPosePlanHistoryCompletion[] m_History;

        internal MotionMatchingPosePlanCompletion(
            ulong presentationFrame,
            ulong resetSequence,
            ulong selectionCompletionIdentity,
            ulong posePlanCompletionIdentity,
            MotionMatchingPosePlanSourceUsage[] sourceUsages,
            int sourceUsageCount,
            MotionMatchingPosePlanHistoryCompletion[] history,
            int historyCount)
        {
            if (presentationFrame == 0 || selectionCompletionIdentity == 0 || posePlanCompletionIdentity == 0 ||
                sourceUsages == null || sourceUsageCount < 0 || sourceUsageCount > sourceUsages.Length ||
                history == null || historyCount < 0 || historyCount > history.Length)
            {
                throw new ArgumentException("Motion Matching Pose Plan completion is invalid.");
            }
            PresentationFrame = presentationFrame;
            ResetSequence = resetSequence;
            SelectionCompletionIdentity = selectionCompletionIdentity;
            PosePlanCompletionIdentity = posePlanCompletionIdentity;
            m_SourceUsages = sourceUsages;
            SourceUsageCount = sourceUsageCount;
            m_History = history;
            HistoryCount = historyCount;
        }

        internal ulong PresentationFrame { get; }
        internal ulong ResetSequence { get; }
        internal ulong SelectionCompletionIdentity { get; }
        internal ulong PosePlanCompletionIdentity { get; }
        internal int SourceUsageCount { get; }
        internal int HistoryCount { get; }

        internal MotionMatchingPosePlanSourceUsage GetSourceUsage(int index) =>
            (uint)index < (uint)SourceUsageCount
                ? m_SourceUsages[index]
                : throw new ArgumentOutOfRangeException(nameof(index));

        internal MotionMatchingPosePlanHistoryCompletion GetHistory(int index) =>
            (uint)index < (uint)HistoryCount
                ? m_History[index]
                : throw new ArgumentOutOfRangeException(nameof(index));
    }
}
