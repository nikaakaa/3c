using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Animation.BlendStack;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;
using ThirdPersonCharacter.Pipeline.Animation.Presentation;
using ThirdPersonCharacter.Pipeline.Animation.Sources;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal sealed class CharacterPoseProgramMotionMatchingRuntime :
        IPoseStateSourceSelectionSink
    {
        struct PreparedHistoryRead
        {
            internal MotionMatchingSelectionBatchItem Selection;
            internal int PlayerIndex;
            internal bool SourceUsed;
        }

        readonly CharacterPoseProgramImage m_Image;
        readonly CharacterPoseActorState m_ActorState;
        readonly CharacterPoseProgramFramePages m_FramePages;
        readonly CharacterPoseProgramEvaluationState m_Evaluation;
        readonly CharacterPoseSourceModule m_SourceModule;
        readonly PresentationFrameWorkspace m_Workspace;
        readonly int m_FootPlacementWeightParameterIndex;
        readonly MotionMatchingPosePlanHistoryCompletion[] m_HistoryCompletions;
        readonly PreparedHistoryRead[] m_PreparedHistoryReads;
        readonly Dictionary<AnimationPlayerSourceSampleKey,
            PresentationPoseSourceSample> m_ProviderSourceSamples;
        int m_PreparedHistoryReadCount;
        int m_HistoryCompletionCount;
        ulong m_PreparedPresentationFrame;
        ulong m_PreparedResetSequence;
        ulong m_PreparedSelectionCompletionIdentity;
        ulong m_PreparedPoseCompletionIdentity;
        bool m_CompletionPrepared;

        internal CharacterPoseProgramMotionMatchingRuntime(
            CharacterPoseProgramImage image,
            CharacterPoseActorState actorState,
            CharacterPoseProgramFramePages framePages,
            CharacterPoseProgramEvaluationState evaluation,
            CharacterPoseSourceModule sourceModule,
            PresentationFrameWorkspace workspace)
        {
            m_Image = image ?? throw new ArgumentNullException(nameof(image));
            m_ActorState = actorState ??
                throw new ArgumentNullException(nameof(actorState));
            m_FramePages = framePages ??
                throw new ArgumentNullException(nameof(framePages));
            m_Evaluation = evaluation ??
                throw new ArgumentNullException(nameof(evaluation));
            m_SourceModule = sourceModule ??
                throw new ArgumentNullException(nameof(sourceModule));
            m_Workspace = workspace ??
                throw new ArgumentNullException(nameof(workspace));
            m_FootPlacementWeightParameterIndex = image.RequireParameterIndex(
                AnimationPoseParameterIds.FootPlacementWeight);
            int capacity = actorState.PoseStateSources.MotionMatchingProviderCount;
            m_HistoryCompletions =
                new MotionMatchingPosePlanHistoryCompletion[capacity];
            m_PreparedHistoryReads = new PreparedHistoryRead[capacity];
            m_ProviderSourceSamples =
                new Dictionary<AnimationPlayerSourceSampleKey,
                    PresentationPoseSourceSample>(capacity);
        }

        internal IReadOnlyDictionary<AnimationPlayerSourceSampleKey,
            PresentationPoseSourceSample> ProviderSourceSamples =>
            m_ProviderSourceSamples;
        internal int ProviderSourceSampleCount =>
            m_ProviderSourceSamples.Count;
        internal bool HasPreparedCompletion => m_CompletionPrepared;

        internal MotionMatchingPoseStateDemandBatch BuildDemandBatch(
            ulong presentationFrame,
            ulong resetSequence,
            PresentationFrameWorkspaceLease workspaceLease) =>
            m_ActorState.PoseStateSources.BuildMotionMatchingDemandBatch(
                presentationFrame,
                resetSequence,
                m_Workspace,
                workspaceLease);

        internal void ApplySelections(
            in MotionMatchingFrameResolution resolution,
            PresentationFrameWorkspaceLease workspaceLease)
        {
            if (resolution.SelectionCount > m_HistoryCompletions.Length)
            {
                throw new InvalidOperationException(
                    "Motion Matching source sample capacity was exceeded.");
            }
            m_ActorState.PoseStateSources.ApplyMotionMatchingSelections(
                in resolution,
                m_ProviderSourceSamples,
                m_Workspace,
                workspaceLease,
                this);
        }

        internal void ClearSelections() => m_ProviderSourceSamples.Clear();

        internal void PrepareCompletion(
            in MotionMatchingFrameResolution resolution,
            ulong poseCompletionIdentity)
        {
            if (m_CompletionPrepared ||
                resolution.PresentationFrame == 0 ||
                resolution.CompletionIdentity == 0 ||
                poseCompletionIdentity == 0 ||
                !m_FramePages.HasPendingEvaluationFrame ||
                m_FramePages.PendingEvaluationCompletionIdentity !=
                    poseCompletionIdentity)
            {
                throw new InvalidOperationException(
                    "Motion Matching Pose Plan completion preparation is invalid.");
            }
            m_SourceModule.BeginUsage(poseCompletionIdentity);
            m_HistoryCompletionCount = 0;
            m_PreparedHistoryReadCount = 0;
            for (int stackIndex = 0;
                 stackIndex < m_ActorState.Stacks.Length;
                 stackIndex++)
            {
                AnimationBlendStackRuntime stack =
                    m_ActorState.Stacks[stackIndex];
                for (int entryIndex = 0;
                     entryIndex < stack.EntryCount;
                     entryIndex++)
                {
                    AnimationBlendEntryId entry = stack.GetEntryId(entryIndex);
                    if (!entry.SourcePoseTarget &&
                        entry.SourceId.SourceKind ==
                        AnimationPoseSourceKind.MotionMatching)
                    {
                        AddSourceUsage(
                            stack.PoseNodeId,
                            entry.SourceId,
                            poseCompletionIdentity);
                    }
                }
            }
            for (int playerIndex = 0;
                 playerIndex < m_ActorState.DirectPlayers.Length;
                 playerIndex++)
            {
                AnimationSelectedPosePlayerRuntime player =
                    m_ActorState.DirectPlayers[playerIndex];
                if (player.HasSelection &&
                    player.SourceId.SourceKind ==
                    AnimationPoseSourceKind.MotionMatching)
                {
                    AddSourceUsage(
                        player.NodeId,
                        player.SourceId,
                        poseCompletionIdentity);
                }
            }
            for (int selectionIndex = 0;
                 selectionIndex < resolution.SelectionCount;
                 selectionIndex++)
            {
                MotionMatchingSelectionBatchItem selection =
                    resolution.GetSelection(selectionIndex);
                if (!selection.RequiresHistory)
                    continue;
                if (m_PreparedHistoryReadCount >=
                        m_PreparedHistoryReads.Length ||
                    !m_ActorState.NodeRuntimeIndex.TryGetPlayerIndex(
                        selection.PlayerNodeId,
                        out int playerIndex))
                {
                    throw new InvalidOperationException(
                        "Motion Matching Pose Plan history completion exceeds its compiled layout.");
                }
                for (int boneIndex = 0;
                     boneIndex < selection.HistoryBoneIndices.Length;
                     boneIndex++)
                {
                    if ((uint)selection.HistoryBoneIndices[boneIndex] >=
                        (uint)m_Image.PoseBoneCount)
                    {
                        throw new InvalidOperationException(
                            "Motion Matching history Bone index is outside the compiled Rig.");
                    }
                }
                bool sourceUsed =
                    m_ActorState.NodeRuntimeIndex.PlayerUsesSource(
                        selection.PlayerNodeId,
                        selection.SourceIdentity);
                m_PreparedHistoryReads[m_PreparedHistoryReadCount++] =
                    new PreparedHistoryRead
                    {
                        Selection = selection,
                        PlayerIndex = playerIndex,
                        SourceUsed = sourceUsed
                    };
            }
            m_PreparedPresentationFrame = resolution.PresentationFrame;
            m_PreparedResetSequence = resolution.ResetSequence;
            m_PreparedSelectionCompletionIdentity =
                resolution.CompletionIdentity;
            m_PreparedPoseCompletionIdentity = poseCompletionIdentity;
            m_CompletionPrepared = true;
        }

        internal MotionMatchingPosePlanCompletion BuildCompletion()
        {
            if (!m_CompletionPrepared ||
                !m_Evaluation.HasPendingCompleted ||
                m_Evaluation.PendingCompletedCompletionIdentity !=
                    m_PreparedPoseCompletionIdentity)
            {
                throw new InvalidOperationException(
                    "Motion Matching Pose Plan completion does not match the evaluated frame.");
            }
            for (int i = 0; i < m_PreparedHistoryReadCount; i++)
            {
                PreparedHistoryRead prepared = m_PreparedHistoryReads[i];
                MotionMatchingSelectionBatchItem selection =
                    prepared.Selection;
                AnimationFootPlacementSample footPlacement = default;
                bool poseAvailable = prepared.SourceUsed &&
                    TryCopyPendingPlayerPose(
                        prepared.PlayerIndex,
                        selection.HistoryBoneIndices,
                        selection.HistoryBonePositions,
                        out footPlacement);
                m_HistoryCompletions[m_HistoryCompletionCount++] =
                    new MotionMatchingPosePlanHistoryCompletion(
                        selection.ProviderId,
                        selection.PlayerNodeId,
                        selection.SourceIdentity,
                        m_PreparedSelectionCompletionIdentity,
                        m_PreparedPoseCompletionIdentity,
                        poseAvailable,
                        in footPlacement);
                m_PreparedHistoryReads[i] = default;
            }
            m_PreparedHistoryReadCount = 0;
            m_CompletionPrepared = false;
            CharacterPoseSourceUsageView sourceUsages =
                m_SourceModule.CaptureUsage(
                    m_PreparedPoseCompletionIdentity);
            return new MotionMatchingPosePlanCompletion(
                m_PreparedPresentationFrame,
                m_PreparedResetSequence,
                m_PreparedSelectionCompletionIdentity,
                m_PreparedPoseCompletionIdentity,
                in sourceUsages,
                m_HistoryCompletions,
                m_HistoryCompletionCount);
        }

        internal void ClearCompletion()
        {
            Array.Clear(
                m_PreparedHistoryReads,
                0,
                m_PreparedHistoryReadCount);
            m_PreparedHistoryReadCount = 0;
            m_PreparedPresentationFrame = 0;
            m_PreparedResetSequence = 0;
            m_PreparedSelectionCompletionIdentity = 0;
            m_PreparedPoseCompletionIdentity = 0;
            m_SourceModule.ClearUsage();
            m_HistoryCompletionCount = 0;
            m_CompletionPrepared = false;
        }

        bool TryCopyPendingPlayerPose(
            int playerIndex,
            int[] rigBoneIndices,
            UnityEngine.Vector3[] positions,
            out AnimationFootPlacementSample footPlacement)
        {
            if (playerIndex < 0 || rigBoneIndices == null ||
                positions == null || rigBoneIndices.Length == 0 ||
                positions.Length != rigBoneIndices.Length)
            {
                throw new ArgumentException(
                    "Animation Player history copy input is invalid.");
            }
            if (!m_Evaluation.HasPendingCompleted)
            {
                footPlacement = default;
                return false;
            }
            CharacterPoseGraphNativeBinding pending =
                m_Evaluation.RequirePendingCompleted();
            var read = new AnimationPlayerPoseNativeWriteBinding(
                in pending,
                playerIndex);
            if (read.CompletedAt[0] != pending.CompletionIdentity ||
                read.Availability[0] != AnimationPoseAvailability.Pose ||
                read.HasFootFeatures[0] == 0 ||
                read.PoseParameterAvailability[
                    m_FootPlacementWeightParameterIndex] == 0)
            {
                footPlacement = default;
                return false;
            }
            for (int i = 0; i < rigBoneIndices.Length; i++)
            {
                int boneIndex = rigBoneIndices[i];
                if ((uint)boneIndex >= (uint)read.DenseLocalPoses.Length)
                {
                    throw new InvalidOperationException(
                        "Motion Matching history Bone index is outside the completed Player pose.");
                }
                positions[i] = read.DenseLocalPoses[boneIndex].Position;
            }
            footPlacement = new AnimationFootPlacementSample(
                read.PoseParameters[m_FootPlacementWeightParameterIndex],
                read.LeftFootFeatures[0],
                read.RightFootFeatures[0]);
            return true;
        }

        CharacterPoseSourceResourceResolution
            IPoseStateSourceSelectionSink.PushMotionMatchingSelection(
            PoseNodeId playerNodeId,
            in PresentationPoseSourceSample sample)
        {
            return m_ActorState.NodeRuntimeIndex.PushMotionMatchingSelection(
                m_SourceModule,
                playerNodeId,
                in sample);
        }

        void AddSourceUsage(
            PoseNodeId playerNodeId,
            AnimationPoseSourceId sourceId,
            ulong completionIdentity)
        {
            var usage = new CharacterPoseSourceUsage(
                playerNodeId,
                sourceId,
                completionIdentity);
            m_SourceModule.RecordUsage(in usage);
        }
    }
}
