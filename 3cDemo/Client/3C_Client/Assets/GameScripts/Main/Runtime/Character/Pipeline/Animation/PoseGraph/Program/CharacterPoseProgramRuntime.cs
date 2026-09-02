using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Animation.BlendStack;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;
using ThirdPersonCharacter.Pipeline.Animation.Presentation;
using ThirdPersonCharacter.Pipeline.Animation.Sources;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonSimulation;
using Unity.Collections;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal sealed class CharacterPoseProgramRuntime :
        IDisposable,
        IPoseStateSourceSelectionSink
    {
        struct PreparedMotionMatchingHistoryRead
        {
            internal MotionMatchingSelectionBatchItem Selection;
            internal int PlayerIndex;
            internal bool SourceUsed;
        }

        readonly struct PreparedEvaluationState
        {
            internal PreparedEvaluationState(
                in CharacterPoseFrameLineage lineage,
                float presentationDeltaSeconds,
                in CharacterPoseGraphNativeBinding frame)
            {
                Lineage = lineage;
                PresentationDeltaSeconds = presentationDeltaSeconds;
                Frame = frame;
            }

            internal CharacterPoseFrameLineage Lineage { get; }
            internal float PresentationDeltaSeconds { get; }
            internal CharacterPoseGraphNativeBinding Frame { get; }
            internal bool IsValid =>
                Lineage.IsValid &&
                float.IsFinite(PresentationDeltaSeconds) &&
                PresentationDeltaSeconds >= 0f &&
                Frame.CompletionIdentity == Lineage.CompletionIdentity;
        }

        struct PreparedEvaluationPage
        {
            CharacterPoseFrameLineage m_Lineage;
            CharacterPoseGraphNativeBinding m_Frame;
            float m_PresentationDeltaSeconds;
            bool m_HasValue;

            internal bool HasValue => m_HasValue;

            internal float RequireDeltaSeconds(
                in CharacterPoseProgramPrepared prepared)
            {
                if (!m_HasValue ||
                    !prepared.IsValid ||
                    prepared.Lineage != m_Lineage)
                {
                    throw new ArgumentException(
                        "Pose Program prepared page does not match the requested frame.",
                        nameof(prepared));
                }
                return m_PresentationDeltaSeconds;
            }

            internal void Prepare(
                in CharacterPoseProgramPrepared prepared,
                float presentationDeltaSeconds,
                in CharacterPoseGraphNativeBinding frame)
            {
                if (m_HasValue)
                {
                    throw new InvalidOperationException(
                        "Pose Program prepared page already contains a frame.");
                }
                if (!prepared.IsValid ||
                    !float.IsFinite(presentationDeltaSeconds) ||
                    presentationDeltaSeconds < 0f ||
                    frame.CompletionIdentity !=
                        prepared.Lineage.CompletionIdentity)
                {
                    throw new ArgumentException(
                        "Pose Program prepared page input is invalid.",
                        nameof(prepared));
                }
                m_Lineage = prepared.Lineage;
                m_PresentationDeltaSeconds = presentationDeltaSeconds;
                m_Frame = frame;
                m_HasValue = true;
            }

            internal PreparedEvaluationState Consume(
                in CharacterPoseProgramPrepared prepared)
            {
                if (!m_HasValue ||
                    !prepared.IsValid ||
                    prepared.Lineage != m_Lineage)
                {
                    throw new ArgumentException(
                        "Pose Program prepared page does not match the requested frame.",
                        nameof(prepared));
                }
                var state = new PreparedEvaluationState(
                    in m_Lineage,
                    m_PresentationDeltaSeconds,
                    in m_Frame);
                Clear();
                if (!state.IsValid)
                {
                    throw new InvalidOperationException(
                        "Pose Program prepared page state is inconsistent.");
                }
                return state;
            }

            internal void Clear()
            {
                m_Lineage = default;
                m_Frame = default;
                m_PresentationDeltaSeconds = 0f;
                m_HasValue = false;
            }
        }

        readonly CharacterPoseProgramImage m_Image;
        readonly CharacterPoseSourceModule m_SourceModule;
        readonly CharacterPoseWorldContextAdapter m_WorldContext;
        readonly int m_FootPlacementWeightParameterIndex;
        readonly MotionMatchingPosePlanHistoryCompletion[]
            m_MotionMatchingHistoryCompletions;
        readonly PreparedMotionMatchingHistoryRead[]
            m_PreparedMotionMatchingHistoryReads;
        CharacterPoseProgramFrameLease m_ActiveFrameLease;
        CharacterPoseProgramFrameLease m_CommittingFrameLease;
        PreparedEvaluationPage m_PreparedEvaluation;
        CharacterPoseGraphNativeBinding m_CommittedEvaluationFrame;
        CharacterPoseGraphNativeBinding m_PendingCompletedEvaluationFrame;
        int m_SequencePreviewPlayerIndex = -1;
        int m_SequencePreviewOperationIndex = -1;
        double m_SequencePreviewTime;
        bool m_SequencePreviewReset;
        bool m_HasSequencePreview;
        int m_PreparedMotionMatchingHistoryReadCount;
        ulong m_PreparedMotionMatchingPresentationFrame;
        ulong m_PreparedMotionMatchingResetSequence;
        ulong m_PreparedMotionMatchingSelectionCompletionIdentity;
        ulong m_PreparedMotionMatchingPoseCompletionIdentity;
        bool m_MotionMatchingPoseCompletionPrepared;
        int m_MotionMatchingHistoryCompletionCount;
        bool m_HasCommittedEvaluationFrame;
        bool m_HasPendingCompletedEvaluationFrame;
        bool m_Disposed;

        internal CharacterPoseProgramRuntime(
            CharacterPoseProgramImage image,
            CharacterPoseProgramExecutionView executionView,
            CharacterPoseActorState actorState,
            CharacterPoseProgramFramePages framePages,
            CharacterPoseProgramTuningState tuning,
            CharacterPoseSourceModule sourceModule,
            CharacterPoseWorldContextAdapter worldContext,
            CharacterPoseConstraintRuntime poseConstraints)
        {
            m_Image = image ?? throw new ArgumentNullException(nameof(image));
            ExecutionView = executionView ??
                throw new ArgumentNullException(nameof(executionView));
            ActorState = actorState ??
                throw new ArgumentNullException(nameof(actorState));
            FramePages = framePages ??
                throw new ArgumentNullException(nameof(framePages));
            Tuning = tuning ?? throw new ArgumentNullException(nameof(tuning));
            m_SourceModule = sourceModule ??
                throw new ArgumentNullException(nameof(sourceModule));
            m_WorldContext = worldContext ??
                throw new ArgumentNullException(nameof(worldContext));
            CharacterPoseConstraintRuntime constraintRuntime = poseConstraints ??
                throw new ArgumentNullException(nameof(poseConstraints));
            m_FootPlacementWeightParameterIndex =
                image.RequireParameterIndex(
                    AnimationPoseParameterIds.FootPlacementWeight);
            m_MotionMatchingHistoryCompletions =
                new MotionMatchingPosePlanHistoryCompletion[
                    ActorState.PoseStateSources
                        .MotionMatchingProviderCount];
            m_PreparedMotionMatchingHistoryReads =
                new PreparedMotionMatchingHistoryRead[
                    m_MotionMatchingHistoryCompletions.Length];
            Executor = new CharacterPoseGraphStagedExecutor(
                ExecutionView,
                FramePages,
                ActorState.Inertialization,
                constraintRuntime);
            if (!string.Equals(
                    m_Image.ProgramId,
                    ExecutionView.ProgramId.ToString(),
                    StringComparison.Ordinal) ||
                !string.Equals(
                    m_Image.ProjectionRevision,
                    ExecutionView.ProjectionRevision.ToString(),
                    StringComparison.Ordinal) ||
                !string.Equals(
                    m_Image.PoseProgramImageHash,
                    ExecutionView.PoseProgramImageHash.ToString(),
                    StringComparison.Ordinal) ||
                !string.Equals(
                    m_Image.RigId,
                    ExecutionView.RigId.ToString(),
                    StringComparison.Ordinal) ||
                !string.Equals(
                    m_Image.RigRevision,
                    ExecutionView.RigRevision.ToString(),
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Pose Program Runtime inputs do not share one Image identity.");
            }
        }

        internal CharacterPoseProgramImage Image => m_Image;
        CharacterPoseProgramExecutionView ExecutionView { get; }
        CharacterPoseActorState ActorState { get; }
        CharacterPoseProgramFramePages FramePages { get; }
        CharacterPoseProgramTuningState Tuning { get; }
        CharacterPoseGraphStagedExecutor Executor { get; }
        internal PoseInertializationNativeProgram Inertialization =>
            ActorState.Inertialization;
        internal CharacterPoseProgramSourceRetirementState SourceRetirement =>
            ActorState.SourceRetirement;
        internal AnimationBlendStackRuntime[] Stacks => ActorState.Stacks;
        internal CharacterAnimationTransitionRouteRuntime[] Routes =>
            ActorState.Routes;
        internal AnimationSelectedPosePlayerRuntime[] DirectPlayers =>
            ActorState.DirectPlayers;
        internal PoseStateAndSourceRuntime PoseStateSources =>
            ActorState.PoseStateSources;
        internal RootOrientationWarpRuntime[] RootOrientationWarps =>
            ActorState.RootOrientationWarps;
        internal CharacterPoseProgramNodeRuntimeIndex NodeRuntimeIndex =>
            ActorState.NodeRuntimeIndex;
        CharacterPoseLinkedFragmentState LinkedFragments =>
            ActorState.LinkedFragments;
        internal CharacterActionPlaybackRuntime ActionPlayback =>
            ActorState.ActionPlayback;
        internal AnimationSlotRuntime AnimationSlots =>
            ActorState.AnimationSlots;
        internal bool HasOpenFrame => m_ActiveFrameLease.IsValid;
        internal bool HasPreparedEvaluation => m_PreparedEvaluation.HasValue;
        internal bool HasPendingEvaluationFrame =>
            FramePages.HasPendingEvaluationFrame;
        internal ulong PendingEvaluationCompletionIdentity =>
            FramePages.PendingEvaluationCompletionIdentity;
        internal long DenseDoublePageResidentPayloadBytes =>
            FramePages.DenseDoublePageResidentPayloadBytes;
        internal bool HasCommittedEvaluationFrame =>
            m_HasCommittedEvaluationFrame;
        internal bool HasPendingCompletedEvaluationFrame =>
            m_HasPendingCompletedEvaluationFrame;
        internal ulong CommittedEvaluationCompletionIdentity =>
            m_HasCommittedEvaluationFrame
                ? m_CommittedEvaluationFrame.CompletionIdentity
                : 0;
        internal ulong PendingCompletedEvaluationCompletionIdentity =>
            m_HasPendingCompletedEvaluationFrame
                ? m_PendingCompletedEvaluationFrame.CompletionIdentity
                : 0;

        internal ulong NextPresentationRequestSequence() =>
            ActorState.NextPresentationRequestSequence();

        internal bool HasSequencePreview => m_HasSequencePreview;

        bool ApplySequencePreview()
        {
            RequireFrame(m_ActiveFrameLease);
            if (!m_HasSequencePreview)
                return false;
            for (int i = 0;
                 i < ActorState.PoseStateSources.ClipPlayers.Length;
                 i++)
            {
                AnimationClipPlayerRuntime player =
                    ActorState.PoseStateSources.ClipPlayers[i];
                bool selected = i == m_SequencePreviewPlayerIndex;
                player.SetRelevant(selected);
                if (selected)
                {
                    player.SetPreviewTime(
                        m_SequencePreviewTime,
                        m_SequencePreviewReset);
                }
            }
            for (int i = 0;
                 i < ActorState.PoseStateSources.BlendSpacePlayers.Length;
                 i++)
            {
                ActorState.PoseStateSources.BlendSpacePlayers[i]
                    .SetRelevant(false);
            }
            return true;
        }

        internal void Advance(
            CharacterPoseProgramFrameLease lease,
            float presentationDeltaSeconds,
            in CharacterPresentationFactFrame factFrame,
            in CharacterPresentationProgramParameterFrame parameterFrame,
            in CharacterPoseSourceTuningView sourceTuning)
        {
            RequireFrame(lease);
            if (!float.IsFinite(presentationDeltaSeconds) ||
                presentationDeltaSeconds < 0f ||
                !factFrame.IsValid ||
                !parameterFrame.IsValid)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(presentationDeltaSeconds));
            }
            if (ApplySequencePreview())
                return;
            ActorState.PoseStateSources.PrepareFrame(
                presentationDeltaSeconds,
                in factFrame);
            for (int i = 0; i < ActorState.Routes.Length; i++)
            {
                AnimationBlendStackRuntime stack = ActorState.Stacks[i];
                if (!LinkedFragments.IsPlayerActive(stack.PlayerIndex))
                    continue;
                CharacterAnimationTransitionRouteRuntime route =
                    ActorState.Routes[i];
                route.FlushReleaseCompletion();
                if (!route.IsAnimationSlot)
                    continue;
                CharacterAnimationSlotNativeControl control =
                    route.NativeControl;
                FramePages.SetAnimationSlotControl(
                    route.AnimationSlotIndex,
                    in control);
            }
            for (int i = 0; i < ActorState.Stacks.Length; i++)
            {
                AnimationBlendStackRuntime stack = ActorState.Stacks[i];
                if (LinkedFragments.IsPlayerActive(stack.PlayerIndex))
                    stack.Advance(presentationDeltaSeconds);
            }
            ActorState.PoseStateSources.AdvanceSources(
                presentationDeltaSeconds,
                in factFrame,
                in parameterFrame,
                sourceTuning);
        }

        internal void FinalizePoseStateFrame(
            CharacterPoseProgramFrameLease lease,
            in CharacterPresentationFactFrame factFrame,
            PresentationFrameWorkspace workspace,
            PresentationFrameWorkspaceLease workspaceLease)
        {
            RequireFrame(lease);
            if (!factFrame.IsValid ||
                workspace == null ||
                !workspaceLease.IsValid)
            {
                throw new ArgumentException(
                    "Pose State frame finalization is invalid.",
                    nameof(factFrame));
            }
            if (m_HasSequencePreview)
                return;
            ActorState.PoseStateSources.EvaluateTransitions(
                in factFrame,
                FramePages,
                workspace,
                workspaceLease);
            for (int i = 0;
                 i < ActorState.RootOrientationWarps.Length;
                 i++)
            {
                if (!LinkedFragments.IsRootOrientationWarpActive(i))
                    continue;
                CharacterRootOrientationWarpNativeControl control =
                    ActorState.RootOrientationWarps[i].Prepare(in factFrame);
                FramePages.SetRootOrientationWarpControl(i, in control);
            }
        }

        internal bool IsSequencePreviewPlayer(int playerIndex) =>
            m_HasSequencePreview &&
            playerIndex == m_SequencePreviewPlayerIndex;

        internal bool IsPlayerActive(int playerIndex) =>
            LinkedFragments.IsPlayerActive(playerIndex);

        internal void PrepareLinkedPoseSelection(
            CharacterLinkedPoseRuntimeSession linkedPose,
            IReadOnlyList<CharacterLinkedPoseGroupProjectionDescriptor> groups)
        {
            RequireFrame(m_ActiveFrameLease);
            if (linkedPose == null)
                throw new ArgumentNullException(nameof(linkedPose));
            if (groups == null)
                throw new ArgumentNullException(nameof(groups));
            LinkedFragments.Clear();
            for (int groupIndex = 0; groupIndex < groups.Count; groupIndex++)
            {
                CharacterLinkedPoseGroupProjectionDescriptor group =
                    groups[groupIndex];
                CharacterLinkedPoseGenerationHandle selection =
                    linkedPose.RequireIncoming(group.GroupId);
                SetLinkedPoseGroupSelection(in selection);
                LinkedFragments.ApplySelection(in selection);
            }
        }

        internal void ApplyLinkedPoseGenerationResets(
            ulong resetCompletionIdentity)
        {
            RequireFrame(m_ActiveFrameLease);
            if (resetCompletionIdentity == 0)
                throw new ArgumentOutOfRangeException(
                    nameof(resetCompletionIdentity));
            if (!LinkedFragments.HasFragments)
                return;
            for (int i = 0; i < ActorState.Stacks.Length; i++)
            {
                AnimationBlendStackRuntime stack = ActorState.Stacks[i];
                if (!LinkedFragments.RequiresPlayerReset(stack.PlayerIndex))
                    continue;
                ActorState.Routes[i].Reset();
                stack.Reset(resetCompletionIdentity);
            }
            for (int i = 0; i < ActorState.DirectPlayers.Length; i++)
            {
                AnimationSelectedPosePlayerRuntime player =
                    ActorState.DirectPlayers[i];
                if (LinkedFragments.RequiresPlayerReset(player.PlayerIndex))
                {
                    player.Reset(
                        PoseDiscontinuityResetReason.BranchReplacement);
                }
            }
            ActorState.PoseStateSources.ApplyLinkedPoseGenerationResets();
            for (int i = 0;
                 i < m_Image.Inertializations.Count;
                 i++)
            {
                if (LinkedFragments.RequiresInertializationReset(i))
                    ActorState.Inertialization.RequestReset(i);
            }
            for (int i = 0;
                 i < ActorState.RootOrientationWarps.Length;
                 i++)
            {
                if (LinkedFragments.RequiresRootOrientationWarpReset(i))
                    ActorState.RootOrientationWarps[i].Reset();
            }
        }

        internal void ClearLinkedPoseFrameSelection()
        {
            RequireAlive();
            LinkedFragments.Clear();
        }

        internal void SetSequencePreview(
            PresentationPoseSourceIndex sourceIndex,
            double sampleTime,
            bool resetContinuity)
        {
            RequireAlive();
            if (m_ActiveFrameLease.IsValid ||
                !sourceIndex.IsValid ||
                !double.IsFinite(sampleTime) ||
                sampleTime < 0d)
            {
                throw new ArgumentException(
                    "Clip Preview sample is invalid.");
            }
            int playerIndex = -1;
            for (int i = 0;
                 i < ActorState.PoseStateSources.ClipPlayers.Length;
                 i++)
            {
                if (ActorState.PoseStateSources.ClipPlayers[i].SourceIndex !=
                    sourceIndex)
                {
                    continue;
                }
                playerIndex = i;
                break;
            }
            if (playerIndex < 0)
            {
                throw new InvalidOperationException(
                    $"Clip Preview source #{sourceIndex.Value} has no compiled Clip Player.");
            }
            int operationIndex = -1;
            for (int i = 0; i < m_Image.Operations.Count; i++)
            {
                CharacterPresentationPoseOperation operation =
                    m_Image.Operations[i];
                if (operation.Code != CharacterPoseOperationCode.ClipPlayer ||
                    operation.ClipPlayerIndex != playerIndex)
                {
                    continue;
                }
                operationIndex = operation.Index;
                break;
            }
            if (operationIndex < 0)
            {
                throw new InvalidOperationException(
                    $"Clip Preview source #{sourceIndex.Value} has no compiled Pose operation.");
            }
            m_SequencePreviewPlayerIndex = playerIndex;
            m_SequencePreviewOperationIndex = operationIndex;
            m_SequencePreviewTime = sampleTime;
            m_SequencePreviewReset = resetContinuity;
            m_HasSequencePreview = true;
        }

        internal void ClearSequencePreview()
        {
            RequireAlive();
            if (m_ActiveFrameLease.IsValid)
            {
                throw new InvalidOperationException(
                    "Clip Preview cannot clear during a frame.");
            }
            m_SequencePreviewPlayerIndex = -1;
            m_SequencePreviewOperationIndex = -1;
            m_SequencePreviewTime = 0d;
            m_SequencePreviewReset = false;
            m_HasSequencePreview = false;
        }

        internal void BeginFrame(CharacterPoseProgramFrameLease lease)
        {
            RequireAlive();
            if (!lease.IsValid ||
                m_ActiveFrameLease.IsValid ||
                m_CommittingFrameLease.IsValid)
            {
                throw new InvalidOperationException(
                    "Character Pose Program frame cannot begin.");
            }
            FramePages.BeginFrame();
            m_ActiveFrameLease = lease;
        }

        internal void CommitFrame(CharacterPoseProgramFrameLease lease)
        {
            RequireFrame(lease);
            ActorState.Inertialization.CommitFrame();
            FramePages.CommitFrame();
            m_ActiveFrameLease = default;
            m_CommittingFrameLease = lease;
        }

        internal void DiscardFrame(CharacterPoseProgramFrameLease lease)
        {
            RequireFrame(lease);
            Exception failure = null;
            if (FramePages.HasPendingEvaluationFrame)
            {
                ulong completionIdentity =
                    FramePages.PendingEvaluationCompletionIdentity;
                DiscardStep(
                    () => FramePages.DiscardEvaluationFrame(
                        completionIdentity),
                    ref failure);
            }
            if (ActorState.Inertialization.HasOpenFrame)
            {
                DiscardStep(
                    ActorState.Inertialization.DiscardFrame,
                    ref failure);
            }
            DiscardStep(FramePages.DiscardFrame, ref failure);
            m_PreparedEvaluation.Clear();
            m_PendingCompletedEvaluationFrame = default;
            m_HasPendingCompletedEvaluationFrame = false;
            m_ActiveFrameLease = default;
            m_CommittingFrameLease = default;
            if (failure != null)
            {
                throw new AggregateException(
                    "Character Pose Program frame discard failed.",
                    failure);
            }
        }

        internal void BeginActorStateFrame(
            CharacterPoseProgramFrameLease lease,
            CharacterLinkedPoseRuntimeSession linkedPose,
            IReadOnlyList<CharacterLinkedPoseGroupProjectionDescriptor> groups,
            ulong resetCompletionIdentity)
        {
            RequireFrame(lease);
            bool nodeFramesOpen = false;
            try
            {
                PrepareLinkedPoseSelection(linkedPose, groups);
                ActorState.Inertialization.BeginFrame();
                for (int i = 0; i < ActorState.Routes.Length; i++)
                    ActorState.Routes[i].BeginFrame();
                for (int i = 0;
                     i < ActorState.RootOrientationWarps.Length;
                     i++)
                {
                    ActorState.RootOrientationWarps[i].BeginFrame();
                }
                BeginNodeFrames();
                nodeFramesOpen = true;
                ApplyLinkedPoseGenerationResets(resetCompletionIdentity);
            }
            catch
            {
                if (nodeFramesOpen)
                    DiscardNodeFrames();
                for (int i = ActorState.Routes.Length - 1; i >= 0; i--)
                {
                    if (ActorState.Routes[i].HasOpenFrame)
                        ActorState.Routes[i].DiscardFrame();
                }
                for (int i = ActorState.RootOrientationWarps.Length - 1;
                     i >= 0;
                     i--)
                {
                    if (ActorState.RootOrientationWarps[i].HasOpenFrame)
                        ActorState.RootOrientationWarps[i].DiscardFrame();
                }
                if (ActorState.Inertialization.HasOpenFrame)
                    ActorState.Inertialization.DiscardFrame();
                LinkedFragments.Clear();
                throw;
            }
        }

        internal void CommitActorStateFrame(
            CharacterPoseProgramFrameLease lease)
        {
            RequireAlive();
            if (!lease.IsValid ||
                !m_CommittingFrameLease.IsValid ||
                lease.Lineage != m_CommittingFrameLease.Lineage)
            {
                throw new InvalidOperationException(
                    "Character Pose Program committing frame lease is stale.");
            }
            for (int i = 0; i < ActorState.Routes.Length; i++)
                ActorState.Routes[i].CommitFrame();
            for (int i = 0;
                 i < ActorState.RootOrientationWarps.Length;
                 i++)
            {
                ActorState.RootOrientationWarps[i].CommitFrame();
            }
            for (int i = 0; i < ActorState.Stacks.Length; i++)
                ActorState.Stacks[i].CommitFrame();
            for (int i = 0; i < ActorState.DirectPlayers.Length; i++)
                ActorState.DirectPlayers[i].CommitFrame();
            ActorState.PoseStateSources.CommitFrame();
            m_CommittingFrameLease = default;
        }

        internal void DiscardActorNodeFrames(
            CharacterPoseProgramFrameLease lease)
        {
            RequireFrame(lease);
            Exception failure = null;
            for (int i = ActorState.Routes.Length - 1; i >= 0; i--)
            {
                CharacterAnimationTransitionRouteRuntime route =
                    ActorState.Routes[i];
                if (route.HasOpenFrame)
                    DiscardStep(route.DiscardFrame, ref failure);
            }
            DiscardStep(DiscardNodeFrames, ref failure);
            if (failure != null)
            {
                throw new AggregateException(
                    "Character Pose Program actor node discard failed.",
                    failure);
            }
        }

        internal void DiscardRootOrientationWarpFrames(
            CharacterPoseProgramFrameLease lease)
        {
            RequireFrame(lease);
            Exception failure = null;
            for (int i = ActorState.RootOrientationWarps.Length - 1;
                 i >= 0;
                 i--)
            {
                RootOrientationWarpRuntime warp =
                    ActorState.RootOrientationWarps[i];
                if (warp.HasOpenFrame)
                    DiscardStep(warp.DiscardFrame, ref failure);
            }
            if (failure != null)
            {
                throw new AggregateException(
                    "Character Pose Program Root Orientation Warp discard failed.",
                    failure);
            }
        }

        internal void BeginEvaluationFrame(
            CharacterPoseProgramFrameLease lease,
            ulong completionIdentity)
        {
            RequireFrame(lease);
            if (m_PreparedEvaluation.HasValue ||
                m_HasPendingCompletedEvaluationFrame)
            {
                throw new InvalidOperationException(
                    "Character Pose Program has an unfinished evaluation.");
            }
            FramePages.BeginEvaluationFrame(completionIdentity);
        }

        internal void CommitEvaluationFrame(
            CharacterPoseProgramFrameLease lease,
            ulong completionIdentity)
        {
            RequireFrame(lease);
            if (!m_HasPendingCompletedEvaluationFrame ||
                m_PendingCompletedEvaluationFrame.CompletionIdentity !=
                    completionIdentity)
            {
                throw new InvalidOperationException(
                    "Character Pose Program has no completed evaluation to commit.");
            }
            FramePages.CommitEvaluationFrame(completionIdentity);
            m_CommittedEvaluationFrame = m_PendingCompletedEvaluationFrame;
            m_HasCommittedEvaluationFrame = true;
            m_PendingCompletedEvaluationFrame = default;
            m_HasPendingCompletedEvaluationFrame = false;
        }

        internal CharacterPoseSourcePreparationView BeginSourceDemand(
            CharacterPoseProgramFrameLease lease,
            ulong completionIdentity)
        {
            RequireFrame(lease);
            return FramePages.BeginSourceDemand(completionIdentity);
        }

        internal void BindSourceDemand(
            CharacterPoseProgramFrameLease lease,
            in CharacterPoseSourceDemand demand)
        {
            RequireFrame(lease);
            if (!lease.Matches(demand.Lineage))
            {
                throw new ArgumentException(
                    "Character Pose Program source demand lineage is invalid.",
                    nameof(demand));
            }
            FramePages.BindSourceDemand(in demand);
        }

        internal CharacterPoseSourceDemand RequireSourceDemand(
            CharacterPoseProgramFrameLease lease,
            in CharacterPoseSourceDemand demand)
        {
            RequireFrame(lease);
            return FramePages.RequireSourceDemand(in demand);
        }

        internal int AddSourcePreparation(
            CharacterPoseProgramFrameLease lease,
            in CharacterPoseSourcePreparation preparation)
        {
            RequireFrame(lease);
            return FramePages.AddSourcePreparation(in preparation);
        }

        internal void ClearSourceDemand() => FramePages.ClearSourceDemand();

        void ResetRootOrientationWarpControl(
            int rootOrientationWarpIndex,
            in CharacterRootOrientationWarpNativeControl control)
        {
            RequireAlive();
            if (m_ActiveFrameLease.IsValid)
            {
                throw new InvalidOperationException(
                    "Character Pose Program Root Orientation Warp cannot reset during a frame.");
            }
            FramePages.SetRootOrientationWarpControl(
                rootOrientationWarpIndex,
                in control);
        }

        internal AnimationPlayerPoseNativeWriteBinding
            RequirePlayerWriteBinding(
                CharacterPoseProgramFrameLease lease,
                int physicalSlotIndex,
                ulong completionIdentity)
        {
            RequireFrame(lease);
            return FramePages.RequirePlayerWriteBinding(
                physicalSlotIndex,
                completionIdentity);
        }

        internal void BindEvaluation(
            CharacterPoseProgramFrameLease lease,
            in CharacterPoseFrameLineage lineage,
            ulong tuningGeneration,
            in CharacterFinalPosePublicationOutputBinding finalOutput,
            bool recordDiagnostics)
        {
            RequireFrame(lease);
            if (!lineage.IsValid ||
                !lease.Matches(lineage) ||
                !FramePages.HasPendingEvaluationFrame ||
                FramePages.PendingEvaluationCompletionIdentity !=
                    lineage.CompletionIdentity)
            {
                throw new ArgumentException(
                    "Character Pose Program evaluation binding is invalid.",
                    nameof(lineage));
            }
            CharacterPoseGraphNativeBinding frame =
                FramePages.RequirePoseGraphBinding(
                    lineage.CompletionIdentity);
            CharacterPoseProgramTuningView tuning =
                Tuning.RequireCommitted(tuningGeneration);
            Executor.BindFrame(
                in tuning,
                frame,
                in finalOutput,
                recordDiagnostics);
        }

        internal void PrepareEvaluation(
            CharacterPoseProgramFrameLease lease,
            in CharacterPoseProgramPrepared prepared,
            float presentationDeltaSeconds)
        {
            RequireFrame(lease);
            if (!prepared.IsValid ||
                !lease.Matches(prepared.Lineage) ||
                !FramePages.HasPendingEvaluationFrame ||
                FramePages.PendingEvaluationCompletionIdentity !=
                    prepared.Lineage.CompletionIdentity)
            {
                throw new ArgumentException(
                    "Character Pose Program prepared evaluation is invalid.",
                    nameof(prepared));
            }
            CharacterPoseGraphNativeBinding frame =
                FramePages.RequirePoseGraphBinding(
                    prepared.Lineage.CompletionIdentity);
            m_PreparedEvaluation.Prepare(
                in prepared,
                presentationDeltaSeconds,
                in frame);
        }

        internal CharacterPoseProgramOutputResult CompleteEvaluation(
            CharacterPoseProgramFrameLease lease,
            in CharacterPoseProgramPrepared prepared,
            in CharacterBodyPresentationFrame bodyFrame,
            in CharacterPresentationFactFrame factFrame)
        {
            RequireFrame(lease);
            if (!prepared.IsValid ||
                !lease.Matches(prepared.Lineage) ||
                !bodyFrame.IsValid ||
                !factFrame.IsValid ||
                !FramePages.HasPendingEvaluationFrame ||
                FramePages.PendingEvaluationCompletionIdentity !=
                    prepared.Lineage.CompletionIdentity)
            {
                throw new ArgumentException(
                    "Character Pose Program evaluation input is invalid.",
                    nameof(prepared));
            }
            PreparedEvaluationState state =
                m_PreparedEvaluation.Consume(in prepared);
            Executor.BeginStagedEvaluation(
                state.Lineage.PresentationFrame);
            CharacterPoseProgramOutputResult output;
            if (m_HasSequencePreview)
            {
                output = Executor.ExecuteSequencePreview(
                    m_SequencePreviewOperationIndex);
            }
            else
            {
                var worldInput = new CharacterPoseWorldFrameInput(
                    m_WorldContext,
                    state.Lineage.ActorId,
                    state.Lineage.PresentationFrame,
                    state.PresentationDeltaSeconds,
                    in bodyFrame,
                    in factFrame,
                    state.Lineage.CompletionIdentity);
                for (int stageIndex = 0;
                     stageIndex < ExecutionView.Stages.Length;
                     stageIndex++)
                {
                    AnimationPoseGraphNativeStage stage =
                        ExecutionView.Stages[stageIndex];
                    if (!Executor.ExecuteStage(
                            stageIndex,
                            state.PresentationDeltaSeconds,
                            in worldInput))
                    {
                        break;
                    }
                }
                output = Executor.CompleteStagedEvaluation();
            }
            FramePages.RequireEvaluationStagesCompleted(
                state.Lineage.CompletionIdentity);
            return output;
        }

        internal float RequirePreparedEvaluationDeltaSeconds(
            CharacterPoseProgramFrameLease lease,
            in CharacterPoseProgramPrepared prepared)
        {
            RequireFrame(lease);
            return m_PreparedEvaluation.RequireDeltaSeconds(in prepared);
        }

        void MarkEvaluationCompleted(
            CharacterPoseProgramFrameLease lease,
            ulong completionIdentity)
        {
            RequireFrame(lease);
            if (m_PreparedEvaluation.HasValue ||
                m_HasPendingCompletedEvaluationFrame)
            {
                throw new InvalidOperationException(
                    "Character Pose Program evaluation completion is invalid.");
            }
            FramePages.RequireEvaluationStagesCompleted(completionIdentity);
            m_PendingCompletedEvaluationFrame =
                FramePages.RequirePoseGraphBinding(completionIdentity);
            m_HasPendingCompletedEvaluationFrame = true;
        }

        internal void CompleteNodeEvaluation(
            CharacterPoseProgramFrameLease lease,
            ulong completionIdentity)
        {
            RequireFrame(lease);
            for (int i = 0; i < ActorState.Stacks.Length; i++)
                ActorState.Stacks[i].CompleteFrame(completionIdentity);
            for (int i = 0; i < ActorState.DirectPlayers.Length; i++)
                ActorState.DirectPlayers[i].CompleteFrame();
            for (int i = 0;
                 i < ActorState.PoseStateSources.ClipPlayers.Length;
                 i++)
            {
                ActorState.PoseStateSources.ClipPlayers[i].CompleteFrame();
            }
            for (int i = 0;
                 i < ActorState.PoseStateSources.BlendSpacePlayers.Length;
                 i++)
            {
                ActorState.PoseStateSources.BlendSpacePlayers[i]
                    .CompleteFrame();
            }
            for (int i = 0; i < ActorState.Routes.Length; i++)
            {
                ActorState.Routes[i].NotifyNativeFrameCompleted(
                    ActorState.Inertialization,
                    completionIdentity);
            }
            ActorState.PoseStateSources.NotifyNativeFrameCompleted(
                ActorState.Inertialization,
                completionIdentity);
            MarkEvaluationCompleted(lease, completionIdentity);
        }

        internal bool TryCopyPendingPlayerPose(
            int playerIndex,
            int[] rigBoneIndices,
            UnityEngine.Vector3[] positions,
            out AnimationFootPlacementSample footPlacement)
        {
            RequireAlive();
            if (playerIndex < 0 ||
                rigBoneIndices == null || positions == null ||
                rigBoneIndices.Length == 0 ||
                positions.Length != rigBoneIndices.Length)
            {
                throw new ArgumentException(
                    "Animation Player history copy input is invalid.");
            }
            if (!m_HasPendingCompletedEvaluationFrame)
            {
                footPlacement = default;
                return false;
            }
            var read = new AnimationPlayerPoseNativeWriteBinding(
                in m_PendingCompletedEvaluationFrame,
                playerIndex);
            if (read.CompletedAt[0] !=
                    m_PendingCompletedEvaluationFrame.CompletionIdentity ||
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

        void ResetEvaluation()
        {
            RequireAlive();
            if (m_ActiveFrameLease.IsValid)
            {
                throw new InvalidOperationException(
                    "Character Pose Program evaluation cannot reset during a frame.");
            }
            m_PreparedEvaluation.Clear();
            m_CommittedEvaluationFrame = default;
            m_PendingCompletedEvaluationFrame = default;
            m_HasCommittedEvaluationFrame = false;
            m_HasPendingCompletedEvaluationFrame = false;
        }

        internal void BeginReset()
        {
            ResetEvaluation();
            FramePages.ClearSourceDemand();
            ActorState.Inertialization.Reset();
        }

        internal void ResetBlendState(ulong completionIdentity)
        {
            RequireAlive();
            if (m_ActiveFrameLease.IsValid ||
                m_CommittingFrameLease.IsValid ||
                completionIdentity == 0)
            {
                throw new InvalidOperationException(
                    "Character Pose Program Blend reset is invalid.");
            }
            for (int i = 0; i < ActorState.Routes.Length; i++)
                ActorState.Routes[i].Reset();
            for (int i = 0; i < ActorState.Stacks.Length; i++)
                ActorState.Stacks[i].Reset(completionIdentity);
        }

        internal void ResetPoseState(
            PoseDiscontinuityResetReason reason)
        {
            RequireAlive();
            if (m_ActiveFrameLease.IsValid ||
                m_CommittingFrameLease.IsValid ||
                reason == PoseDiscontinuityResetReason.None)
            {
                throw new InvalidOperationException(
                    "Character Pose Program Pose State reset is invalid.");
            }
            for (int i = 0; i < ActorState.DirectPlayers.Length; i++)
                ActorState.DirectPlayers[i].Reset(reason);
            ActorState.PoseStateSources.Reset(reason);
            for (int i = 0;
                 i < ActorState.RootOrientationWarps.Length;
                 i++)
            {
                ActorState.RootOrientationWarps[i].Reset();
                var control = new CharacterRootOrientationWarpNativeControl(
                    false,
                    0f);
                ResetRootOrientationWarpControl(i, in control);
            }
        }

        internal bool HasPreparedMotionMatchingPoseCompletion =>
            m_MotionMatchingPoseCompletionPrepared;

        internal MotionMatchingPoseStateDemandBatch
            BuildMotionMatchingDemandBatch(
                CharacterPoseProgramFrameLease lease,
                ulong presentationFrame,
                ulong resetSequence,
                PresentationFrameWorkspace workspace,
                PresentationFrameWorkspaceLease workspaceLease)
        {
            RequireFrame(lease);
            return ActorState.PoseStateSources.BuildMotionMatchingDemandBatch(
                presentationFrame,
                resetSequence,
                workspace,
                workspaceLease);
        }

        internal void ApplyMotionMatchingSelections(
            CharacterPoseProgramFrameLease lease,
            in MotionMatchingFrameResolution resolution,
            IDictionary<AnimationPlayerSourceSampleKey,
                PresentationPoseSourceSample> sourceSamples,
            PresentationFrameWorkspace workspace,
            PresentationFrameWorkspaceLease workspaceLease)
        {
            RequireFrame(lease);
            ActorState.PoseStateSources.ApplyMotionMatchingSelections(
                in resolution,
                sourceSamples,
                workspace,
                workspaceLease,
                this);
        }

        internal void PrepareMotionMatchingPosePlanCompletion(
            CharacterPoseProgramFrameLease lease,
            in MotionMatchingFrameResolution resolution,
            ulong poseCompletionIdentity)
        {
            RequireFrame(lease);
            if (m_MotionMatchingPoseCompletionPrepared ||
                resolution.PresentationFrame == 0 ||
                resolution.CompletionIdentity == 0 ||
                poseCompletionIdentity == 0 ||
                !FramePages.HasPendingEvaluationFrame ||
                FramePages.PendingEvaluationCompletionIdentity !=
                    poseCompletionIdentity)
            {
                throw new InvalidOperationException(
                    "Motion Matching Pose Plan completion preparation is invalid.");
            }
            m_SourceModule.BeginUsage(poseCompletionIdentity);
            m_MotionMatchingHistoryCompletionCount = 0;
            m_PreparedMotionMatchingHistoryReadCount = 0;
            for (int stackIndex = 0;
                 stackIndex < ActorState.Stacks.Length;
                 stackIndex++)
            {
                AnimationBlendStackRuntime stack =
                    ActorState.Stacks[stackIndex];
                for (int entryIndex = 0;
                     entryIndex < stack.EntryCount;
                     entryIndex++)
                {
                    AnimationBlendEntryId entry =
                        stack.GetEntryId(entryIndex);
                    if (!entry.SourcePoseTarget &&
                        entry.SourceId.SourceKind ==
                        AnimationPoseSourceKind.MotionMatching)
                    {
                        AddMotionMatchingSourceUsage(
                            stack.PoseNodeId,
                            entry.SourceId,
                            poseCompletionIdentity);
                    }
                }
            }
            for (int playerIndex = 0;
                 playerIndex < ActorState.DirectPlayers.Length;
                 playerIndex++)
            {
                AnimationSelectedPosePlayerRuntime player =
                    ActorState.DirectPlayers[playerIndex];
                if (player.HasSelection &&
                    player.SourceId.SourceKind ==
                    AnimationPoseSourceKind.MotionMatching)
                {
                    AddMotionMatchingSourceUsage(
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
                if (m_PreparedMotionMatchingHistoryReadCount >=
                        m_PreparedMotionMatchingHistoryReads.Length ||
                    !ActorState.NodeRuntimeIndex.TryGetPlayerIndex(
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
                bool sourceUsed = ActorState.NodeRuntimeIndex.PlayerUsesSource(
                    selection.PlayerNodeId,
                    selection.SourceIdentity);
                m_PreparedMotionMatchingHistoryReads[
                    m_PreparedMotionMatchingHistoryReadCount++] =
                    new PreparedMotionMatchingHistoryRead
                    {
                        Selection = selection,
                        PlayerIndex = playerIndex,
                        SourceUsed = sourceUsed
                    };
            }
            m_PreparedMotionMatchingPresentationFrame =
                resolution.PresentationFrame;
            m_PreparedMotionMatchingResetSequence =
                resolution.ResetSequence;
            m_PreparedMotionMatchingSelectionCompletionIdentity =
                resolution.CompletionIdentity;
            m_PreparedMotionMatchingPoseCompletionIdentity =
                poseCompletionIdentity;
            m_MotionMatchingPoseCompletionPrepared = true;
        }

        internal MotionMatchingPosePlanCompletion
            BuildMotionMatchingPosePlanCompletion(
                CharacterPoseProgramFrameLease lease)
        {
            RequireFrame(lease);
            if (!m_MotionMatchingPoseCompletionPrepared ||
                !m_HasPendingCompletedEvaluationFrame ||
                m_PendingCompletedEvaluationFrame.CompletionIdentity !=
                    m_PreparedMotionMatchingPoseCompletionIdentity)
            {
                throw new InvalidOperationException(
                    "Motion Matching Pose Plan completion does not match the evaluated frame.");
            }
            for (int i = 0;
                 i < m_PreparedMotionMatchingHistoryReadCount;
                 i++)
            {
                PreparedMotionMatchingHistoryRead prepared =
                    m_PreparedMotionMatchingHistoryReads[i];
                MotionMatchingSelectionBatchItem selection =
                    prepared.Selection;
                AnimationFootPlacementSample footPlacement = default;
                bool poseAvailable =
                    prepared.SourceUsed &&
                    TryCopyPendingPlayerPose(
                        prepared.PlayerIndex,
                        selection.HistoryBoneIndices,
                        selection.HistoryBonePositions,
                        out footPlacement);
                m_MotionMatchingHistoryCompletions[
                    m_MotionMatchingHistoryCompletionCount++] =
                    new MotionMatchingPosePlanHistoryCompletion(
                        selection.ProviderId,
                        selection.PlayerNodeId,
                        selection.SourceIdentity,
                        m_PreparedMotionMatchingSelectionCompletionIdentity,
                        m_PreparedMotionMatchingPoseCompletionIdentity,
                        poseAvailable,
                        in footPlacement);
                m_PreparedMotionMatchingHistoryReads[i] = default;
            }
            m_PreparedMotionMatchingHistoryReadCount = 0;
            m_MotionMatchingPoseCompletionPrepared = false;
            CharacterPoseSourceUsageView sourceUsages =
                m_SourceModule.CaptureUsage(
                    m_PreparedMotionMatchingPoseCompletionIdentity);
            return new MotionMatchingPosePlanCompletion(
                m_PreparedMotionMatchingPresentationFrame,
                m_PreparedMotionMatchingResetSequence,
                m_PreparedMotionMatchingSelectionCompletionIdentity,
                m_PreparedMotionMatchingPoseCompletionIdentity,
                in sourceUsages,
                m_MotionMatchingHistoryCompletions,
                m_MotionMatchingHistoryCompletionCount);
        }

        internal void ClearMotionMatchingPoseCompletion()
        {
            RequireAlive();
            Array.Clear(
                m_PreparedMotionMatchingHistoryReads,
                0,
                m_PreparedMotionMatchingHistoryReadCount);
            m_PreparedMotionMatchingHistoryReadCount = 0;
            m_PreparedMotionMatchingPresentationFrame = 0;
            m_PreparedMotionMatchingResetSequence = 0;
            m_PreparedMotionMatchingSelectionCompletionIdentity = 0;
            m_PreparedMotionMatchingPoseCompletionIdentity = 0;
            m_SourceModule.ClearUsage();
            m_MotionMatchingHistoryCompletionCount = 0;
            m_MotionMatchingPoseCompletionPrepared = false;
        }

        void IPoseStateSourceSelectionSink.PushMotionMatchingSelection(
            PoseNodeId playerNodeId,
            in PresentationPoseSourceSample sample)
        {
            ActorState.NodeRuntimeIndex.PushMotionMatchingSelection(
                m_SourceModule,
                playerNodeId,
                in sample);
        }

        void AddMotionMatchingSourceUsage(
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

        internal CharacterPoseProgramTuningView RequireTuning(
            ulong generation) => Tuning.RequireCommitted(generation);

        internal CharacterPoseProgramCommittedDiagnosticsView
            CaptureCommittedDiagnostics(
                CharacterPoseProgramCommittedDiagnosticsProjector projector,
                in CharacterPoseProgramResult result,
                in CharacterFinalPoseCommittedDiagnosticsView finalOutput,
                AnimationPresentationDiagnosticsInterest interest)
        {
            if (projector == null)
                throw new ArgumentNullException(nameof(projector));
            if (!m_HasCommittedEvaluationFrame)
            {
                throw new InvalidOperationException(
                    "Character Pose Program has no committed evaluation diagnostics.");
            }
            return projector.Capture(
                FramePages,
                in result,
                in m_CommittedEvaluationFrame,
                in finalOutput,
                interest);
        }

        internal string PrepareTuningCandidate(
            CharacterPoseTuningLayout layout,
            CharacterPoseTuningParameterBlock block,
            ulong generation)
        {
            try
            {
                Tuning.PrepareCandidate(layout, block, generation);
            }
            catch (Exception exception)
            {
                return exception.Message;
            }
            for (int i = 0;
                 i < ActorState.PoseStateSources.StateMachines.Length;
                 i++)
            {
                string error = ActorState.PoseStateSources.StateMachines[i]
                    .PrepareTuningCandidate(layout, block);
                if (!string.IsNullOrEmpty(error))
                {
                    DiscardTuningCandidate();
                    return error;
                }
            }
            for (int i = 0; i < ActorState.Stacks.Length; i++)
            {
                string error = ActorState.Stacks[i].PrepareTuningCandidate(
                    layout,
                    block);
                if (!string.IsNullOrEmpty(error))
                {
                    DiscardTuningCandidate();
                    return error;
                }
            }
            string inertializationError = ActorState.Inertialization
                .PrepareTuningCandidate(layout, block);
            if (!string.IsNullOrEmpty(inertializationError))
            {
                DiscardTuningCandidate();
                return inertializationError;
            }
            return string.Empty;
        }

        internal void CommitTuningCandidate(ulong generation)
        {
            for (int i = 0;
                 i < ActorState.PoseStateSources.StateMachines.Length;
                 i++)
            {
                ActorState.PoseStateSources.StateMachines[i]
                    .CommitTuningCandidate();
            }
            for (int i = 0; i < ActorState.Stacks.Length; i++)
                ActorState.Stacks[i].CommitTuningCandidate();
            ActorState.Inertialization.CommitTuningCandidate();
            Tuning.CommitCandidate(generation);
        }

        internal void DiscardTuningCandidate()
        {
            for (int i = 0;
                 i < ActorState.PoseStateSources.StateMachines.Length;
                 i++)
            {
                ActorState.PoseStateSources.StateMachines[i]
                    .DiscardTuningCandidate();
            }
            for (int i = 0; i < ActorState.Stacks.Length; i++)
                ActorState.Stacks[i].DiscardTuningCandidate();
            ActorState.Inertialization.DiscardTuningCandidate();
            Tuning.DiscardCandidate();
        }

        internal void SetLinkedPoseGroupSelection(
            in CharacterLinkedPoseGenerationHandle selection)
        {
            if (!FramePages.HasOpenFrame)
            {
                throw new InvalidOperationException(
                    "Character Pose Program frame pages are not open.");
            }
            if (!selection.IsValid)
            {
                throw new ArgumentException(
                    "Linked Pose generation selection is invalid.",
                    nameof(selection));
            }
            NativeArray<AnimationPoseGraphNativeLinkedPoseCallControl>
                controls = FramePages.LinkedPoseCallControls;
            NativeArray<byte> activeFragments =
                FramePages.LinkedPoseActiveFragments;
            int matchingCallCount = 0;
            for (int callIndex = 0;
                 callIndex < ExecutionView.LinkedPoseCalls.Length;
                 callIndex++)
            {
                if (ExecutionView.GetLinkedPoseCallGroupId(callIndex) !=
                    selection.GroupId)
                {
                    continue;
                }
                matchingCallCount++;
                if (ExecutionView.GetLinkedPoseCallInterfaceId(callIndex) !=
                        selection.InterfaceId ||
                    controls[callIndex].IsActive ||
                    ExecutionView.FindLinkedPoseCandidate(
                        callIndex,
                        selection.ImplementationId) < 0)
                {
                    throw new InvalidOperationException(
                        $"Linked Pose Group '{selection.GroupId}' selection does not match call #{callIndex}.");
                }
            }
            if (matchingCallCount == 0)
            {
                throw new InvalidOperationException(
                    $"Linked Pose Group '{selection.GroupId}' has no compiled calls.");
            }
            for (int callIndex = 0;
                 callIndex < ExecutionView.LinkedPoseCalls.Length;
                 callIndex++)
            {
                if (ExecutionView.GetLinkedPoseCallGroupId(callIndex) !=
                    selection.GroupId)
                {
                    continue;
                }
                int candidateIndex =
                    ExecutionView.FindLinkedPoseCandidate(
                        callIndex,
                        selection.ImplementationId);
                AnimationPoseGraphNativeLinkedPoseCandidate candidate =
                    ExecutionView.LinkedPoseCandidates[candidateIndex];
                controls[callIndex] =
                    new AnimationPoseGraphNativeLinkedPoseCallControl(
                        candidateIndex,
                        selection.Generation,
                        selection.PoseDiscontinuity);
                activeFragments[candidate.FragmentIndex] = 1;
            }
        }

        void BeginNodeFrames()
        {
            int stackCount = 0;
            int directPlayerCount = 0;
            bool poseStateSourcesOpen = false;
            try
            {
                ActorState.PoseStateSources.BeginFrame();
                poseStateSourcesOpen = true;
                for (; stackCount < ActorState.Stacks.Length; stackCount++)
                    ActorState.Stacks[stackCount].BeginFrame();
                for (;
                     directPlayerCount < ActorState.DirectPlayers.Length;
                     directPlayerCount++)
                {
                    ActorState.DirectPlayers[directPlayerCount].BeginFrame();
                }
            }
            catch
            {
                for (int i = directPlayerCount - 1; i >= 0; i--)
                    ActorState.DirectPlayers[i].DiscardFrame();
                for (int i = stackCount - 1; i >= 0; i--)
                    ActorState.Stacks[i].DiscardFrame();
                if (poseStateSourcesOpen)
                    ActorState.PoseStateSources.DiscardFrame();
                throw;
            }
        }

        void DiscardNodeFrames()
        {
            for (int i = ActorState.DirectPlayers.Length - 1; i >= 0; i--)
                ActorState.DirectPlayers[i].DiscardFrame();
            for (int i = ActorState.Stacks.Length - 1; i >= 0; i--)
                ActorState.Stacks[i].DiscardFrame();
            ActorState.PoseStateSources.DiscardFrame();
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            m_ActiveFrameLease = default;
            m_CommittingFrameLease = default;
            m_PreparedEvaluation.Clear();
            m_CommittedEvaluationFrame = default;
            m_PendingCompletedEvaluationFrame = default;
            m_HasCommittedEvaluationFrame = false;
            m_HasPendingCompletedEvaluationFrame = false;
            Exception failure = null;
            DisposeStep(ActorState.Dispose, ref failure);
            DisposeStep(Tuning.Dispose, ref failure);
            DisposeStep(ExecutionView.Dispose, ref failure);
            DisposeStep(FramePages.Dispose, ref failure);
            if (failure != null)
                throw failure;
        }

        void RequireFrame(CharacterPoseProgramFrameLease lease)
        {
            RequireAlive();
            if (!lease.IsValid ||
                !m_ActiveFrameLease.IsValid ||
                lease.Lineage != m_ActiveFrameLease.Lineage)
            {
                throw new InvalidOperationException(
                    "Character Pose Program frame lease is stale.");
            }
        }

        void RequireAlive()
        {
            if (m_Disposed)
            {
                throw new ObjectDisposedException(
                    nameof(CharacterPoseProgramRuntime));
            }
        }

        static void DisposeStep(Action dispose, ref Exception failure)
        {
            try
            {
                dispose();
            }
            catch (Exception exception)
            {
                failure = failure == null
                    ? exception
                    : new AggregateException(failure, exception);
            }
        }

        static void DiscardStep(Action discard, ref Exception failure)
        {
            try
            {
                discard();
            }
            catch (Exception exception)
            {
                failure = failure == null
                    ? exception
                    : new AggregateException(failure, exception);
            }
        }
    }
}
