using System;
using System.Collections.Generic;
using BTSMTL.Timeline;
using ThirdPersonCharacter.Animation.TransitionRouting;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Pipeline.Animation.Lifecycle;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation.Presentation
{
    internal interface IPoseStateSourceSelectionSink
    {
        void PushMotionMatchingSelection(
            PoseNodeId playerNodeId,
            in PresentationPoseSourceSample sample);
    }

    internal sealed class PoseStateAndSourceRuntime :
        ICharacterPoseStateSourceRuntime
    {
        sealed class SourceSyncRelationSlot
        {
            internal SourceSyncRelationSlot(CharacterPoseStateSourceSyncPlan plan)
            {
                RelationId = plan?.RelationId ?? throw new ArgumentNullException(nameof(plan));
                TimeMapping = plan.TimeMapping;
                PlanIdentity = plan.FootPhaseWarp?.PlanIdentity ?? string.Empty;
            }

            internal string RelationId { get; }
            internal MarkerSegmentRelationCursor Cursor { get; } =
                new MarkerSegmentRelationCursor();
            internal bool Active;
            internal bool Journaled;
            internal AnimationSyncTimeMapping TimeMapping { get; }
            internal string PlanIdentity { get; }
            internal float LeaderFraction;
            internal float FollowerFraction;
            internal int LeaderOccurrenceIndex = -1;
            internal int FollowerOccurrenceIndex = -1;
            internal double FollowerEffectiveTime;
            internal bool FiniteLeaderReleased;
        }

        struct SourceSyncRelationJournalEntry
        {
            internal SourceSyncRelationSlot Slot;
            internal bool Active;
            internal bool Initialized;
            internal long LeaderOrdinal;
            internal long FollowerOrdinal;
            internal float LeaderFraction;
            internal float FollowerFraction;
            internal int LeaderOccurrenceIndex;
            internal int FollowerOccurrenceIndex;
            internal double FollowerEffectiveTime;
            internal bool FiniteLeaderReleased;
        }

        internal sealed class MotionMatchingRelevance
        {
            struct State
            {
                internal ulong Generation;
                internal bool Relevant;
                internal PoseSourceProviderDemandKind
                    DemandKind;
                internal ulong LastResolvedFrame;
                internal PoseSourceProviderStatus Status;
            }

            internal MotionMatchingRelevance(
                int stateMachineIndex,
                PoseStateSourceProviderPlan usage,
                string providerId)
            {
                if (stateMachineIndex < 0 ||
                    usage == null ||
                    string.IsNullOrWhiteSpace(providerId))
                {
                    throw new ArgumentException(
                        "Motion Matching Pose State relevance binding is invalid.");
                }
                StateMachineIndex = stateMachineIndex;
                Usage = usage;
                ProviderId = providerId;
                m_CommittedState.Generation = 1;
                m_NextGeneration = 2;
                m_CommittedState.Status =
                    PoseSourceProviderStatus.Pending(
                        usage.ProviderId);
            }

            ulong m_NextGeneration;
            State m_CommittedState;
            State m_PendingState;
            bool m_FrameOpen;

            internal int StateMachineIndex { get; }
            internal PoseStateSourceProviderPlan Usage { get; }
            internal string ProviderId { get; }
            internal ulong Generation
            {
                get => m_FrameOpen ? m_PendingState.Generation : m_CommittedState.Generation;
                private set
                {
                    if (m_FrameOpen)
                        m_PendingState.Generation = value;
                    else
                        m_CommittedState.Generation = value;
                }
            }
            internal bool Relevant
            {
                get => m_FrameOpen ? m_PendingState.Relevant : m_CommittedState.Relevant;
                private set
                {
                    if (m_FrameOpen)
                        m_PendingState.Relevant = value;
                    else
                        m_CommittedState.Relevant = value;
                }
            }
            internal PoseSourceProviderDemandKind DemandKind
            {
                get => m_FrameOpen ? m_PendingState.DemandKind : m_CommittedState.DemandKind;
                private set
                {
                    if (m_FrameOpen)
                        m_PendingState.DemandKind = value;
                    else
                        m_CommittedState.DemandKind = value;
                }
            }
            internal ulong LastResolvedFrame
            {
                get => m_FrameOpen ? m_PendingState.LastResolvedFrame : m_CommittedState.LastResolvedFrame;
                private set
                {
                    if (m_FrameOpen)
                        m_PendingState.LastResolvedFrame = value;
                    else
                        m_CommittedState.LastResolvedFrame = value;
                }
            }
            internal PoseSourceProviderStatus Status
            {
                get => m_FrameOpen ? m_PendingState.Status : m_CommittedState.Status;
                private set
                {
                    if (m_FrameOpen)
                        m_PendingState.Status = value;
                    else
                        m_CommittedState.Status = value;
                }
            }

            internal void SetRelevant(
                bool relevant,
                PoseSourceProviderDemandKind demandKind)
            {
                if (relevant &&
                    !Enum.IsDefined(
                        typeof(PoseSourceProviderDemandKind),
                        demandKind))
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(demandKind));
                }
                if (Relevant == relevant)
                {
                    if (relevant)
                        DemandKind = demandKind;
                    return;
                }
                Relevant = relevant;
                if (!relevant)
                {
                    DemandKind = default;
                    return;
                }
                DemandKind = demandKind;
                Generation = AllocateGeneration();
                LastResolvedFrame = 0;
                Status =
                    PoseSourceProviderStatus.Pending(
                        Usage.ProviderId);
            }

            internal void Reset()
            {
                Generation = AllocateGeneration();
                LastResolvedFrame = 0;
                Status =
                    PoseSourceProviderStatus.Pending(
                        Usage.ProviderId);
            }

            internal void MarkReady(ulong presentationFrame)
            {
                if (!Relevant || presentationFrame == 0)
                {
                    throw new InvalidOperationException(
                        "Motion Matching provider cannot become ready outside an active demand.");
                }
                LastResolvedFrame = presentationFrame;
                Status =
                    PoseSourceProviderStatus.Ready(
                        Usage.ProviderId);
            }

            internal void MarkInvalid(
                ulong presentationFrame,
                PresentationPoseSourceFailureReason failureReason)
            {
                if (!Relevant ||
                    presentationFrame == 0 ||
                    failureReason ==
                    PresentationPoseSourceFailureReason.None)
                {
                    throw new InvalidOperationException(
                        "Motion Matching provider invalid result is malformed.");
                }
                LastResolvedFrame = presentationFrame;
                Status =
                    PoseSourceProviderStatus.Invalid(
                        Usage.ProviderId,
                        failureReason);
            }

            internal void BeginFrame()
            {
                if (m_FrameOpen)
                    throw new InvalidOperationException("Motion Matching relevance frame is already open.");
                m_PendingState = m_CommittedState;
                m_FrameOpen = true;
            }

            internal void DiscardFrame()
            {
                if (!m_FrameOpen)
                    return;
                m_FrameOpen = false;
            }

            internal void CommitFrame()
            {
                if (!m_FrameOpen)
                    throw new InvalidOperationException("Motion Matching relevance frame is not open.");
                m_CommittedState = m_PendingState;
                m_FrameOpen = false;
            }

            ulong AllocateGeneration()
            {
                if (m_NextGeneration == ulong.MaxValue)
                {
                    throw new InvalidOperationException(
                        "Motion Matching Pose State relevance generation was exhausted.");
                }
                return m_NextGeneration++;
            }
        }

        readonly AnimationSequencePlayerRuntime[] m_SequencePlayers;
        readonly AnimationBlendSpacePlayerRuntime[] m_BlendSpacePlayers;
        readonly CharacterPoseStateMachineRuntime[] m_StateMachines;
        readonly Dictionary<PoseNodeId, AnimationSequencePlayerRuntime>
            m_SequenceByNode =
                new Dictionary<PoseNodeId,
                    AnimationSequencePlayerRuntime>();
        readonly Dictionary<int, AnimationSequencePlayerRuntime>
            m_SequenceByPlayerIndex =
                new Dictionary<int,
                    AnimationSequencePlayerRuntime>();
        readonly Dictionary<PoseNodeId, AnimationBlendSpacePlayerRuntime>
            m_BlendSpaceByNode =
                new Dictionary<PoseNodeId,
                    AnimationBlendSpacePlayerRuntime>();
        readonly Dictionary<int, AnimationBlendSpacePlayerRuntime>
            m_BlendSpaceByPlayerIndex =
                new Dictionary<int,
                    AnimationBlendSpacePlayerRuntime>();
        readonly Dictionary<string, SourceSyncRelationSlot>
            m_SourceSyncRelations;
        readonly SourceSyncRelationJournalEntry[]
            m_SourceSyncRelationJournal;
        readonly MotionMatchingRelevance[] m_MotionMatching;
        readonly MotionMatchingRelevance[] m_MotionMatchingByOperation;
        readonly MotionMatchingPoseStateDemand[] m_MotionMatchingDemands;
        readonly int[] m_PlayerLinkedPoseFragmentIndices;
        readonly int[] m_StateMachineLinkedPoseFragmentIndices;
        readonly bool[] m_StateControlledPlayers;
        readonly bool[] m_LinkedPoseActiveFragments;
        readonly bool[] m_LinkedPoseResetFragments;
        int m_SourceSyncRelationJournalCount;
        bool m_FrameOpen;

        internal PoseStateAndSourceRuntime(
            CharacterPresentationPosePlan plan,
            AnimationSequencePlayerRuntime[] sequencePlayers,
            AnimationBlendSpacePlayerRuntime[] blendSpacePlayers,
            int[] playerLinkedPoseFragmentIndices,
            int[] stateMachineLinkedPoseFragmentIndices,
            bool[] linkedPoseActiveFragments,
            bool[] linkedPoseResetFragments)
        {
            if (plan == null)
                throw new ArgumentNullException(nameof(plan));
            m_SequencePlayers = sequencePlayers ??
                throw new ArgumentNullException(nameof(sequencePlayers));
            m_BlendSpacePlayers = blendSpacePlayers ??
                throw new ArgumentNullException(nameof(blendSpacePlayers));
            m_PlayerLinkedPoseFragmentIndices =
                playerLinkedPoseFragmentIndices ??
                throw new ArgumentNullException(
                    nameof(playerLinkedPoseFragmentIndices));
            m_StateMachineLinkedPoseFragmentIndices =
                stateMachineLinkedPoseFragmentIndices ??
                throw new ArgumentNullException(
                    nameof(stateMachineLinkedPoseFragmentIndices));
            m_StateControlledPlayers =
                BuildStateControlledPlayers(plan);
            m_LinkedPoseActiveFragments =
                linkedPoseActiveFragments ??
                throw new ArgumentNullException(
                    nameof(linkedPoseActiveFragments));
            m_LinkedPoseResetFragments =
                linkedPoseResetFragments ??
                throw new ArgumentNullException(
                    nameof(linkedPoseResetFragments));
            if (m_StateMachineLinkedPoseFragmentIndices.Length !=
                    plan.StateMachines.Count ||
                m_LinkedPoseActiveFragments.Length !=
                    plan.LinkedPoseFragments.Count ||
                m_LinkedPoseResetFragments.Length !=
                    plan.LinkedPoseFragments.Count)
            {
                throw new InvalidOperationException(
                    "Pose State Linked Pose ownership does not match the compiled plan.");
            }
            for (int i = 0; i < m_SequencePlayers.Length; i++)
            {
                AnimationSequencePlayerRuntime player =
                    m_SequencePlayers[i] ??
                    throw new InvalidOperationException(
                        $"Sequence Player runtime #{i} is missing.");
                m_SequenceByNode.Add(player.NodeId, player);
                m_SequenceByPlayerIndex.Add(
                    player.PlayerIndex,
                    player);
            }
            for (int i = 0; i < m_BlendSpacePlayers.Length; i++)
            {
                AnimationBlendSpacePlayerRuntime player =
                    m_BlendSpacePlayers[i] ??
                    throw new InvalidOperationException(
                        $"Blend Space Player runtime #{i} is missing.");
                m_BlendSpaceByNode.Add(player.NodeId, player);
                m_BlendSpaceByPlayerIndex.Add(
                    player.PlayerIndex,
                    player);
            }
            m_StateMachines =
                new CharacterPoseStateMachineRuntime[
                    plan.StateMachines.Count];
            for (int i = 0; i < m_StateMachines.Length; i++)
            {
                m_StateMachines[i] =
                    new CharacterPoseStateMachineRuntime(
                        plan.StateMachines[i]);
            }
            m_SourceSyncRelations =
                BuildSourceSyncRelations(plan);
            m_SourceSyncRelationJournal =
                new SourceSyncRelationJournalEntry[
                    m_SourceSyncRelations.Count];
            m_MotionMatching =
                BuildMotionMatchingRelevance(plan);
            m_MotionMatchingByOperation =
                new MotionMatchingRelevance[
                    plan.Operations.Count];
            for (int i = 0; i < m_MotionMatching.Length; i++)
            {
                MotionMatchingRelevance relevance =
                    m_MotionMatching[i];
                int operationIndex =
                    relevance.Usage.OperationIndex;
                if (m_MotionMatchingByOperation[
                        operationIndex] != null)
                {
                    throw new InvalidOperationException(
                        $"Motion Matching Pose State usage operation #{operationIndex} is duplicated.");
                }
                m_MotionMatchingByOperation[
                    operationIndex] = relevance;
            }
            m_MotionMatchingDemands =
                new MotionMatchingPoseStateDemand[
                    m_MotionMatching.Length];
        }

        internal AnimationSequencePlayerRuntime[]
            SequencePlayers => m_SequencePlayers;
        internal AnimationBlendSpacePlayerRuntime[]
            BlendSpacePlayers => m_BlendSpacePlayers;
        internal CharacterPoseStateMachineRuntime[]
            StateMachines => m_StateMachines;
        internal bool CanApplyNextActivation
        {
            get
            {
                for (int i = 0; i < m_StateMachines.Length; i++)
                    if (m_StateMachines[i].HasActiveTransition)
                        return false;
                return true;
            }
        }
        internal int MotionMatchingProviderCount =>
            m_MotionMatching.Length;

        internal void BeginFrame()
        {
            if (m_FrameOpen || m_SourceSyncRelationJournalCount != 0)
                throw new InvalidOperationException("Pose State and source frame is already open.");
            int sequenceCount = 0;
            int blendSpaceCount = 0;
            int stateMachineCount = 0;
            int motionMatchingCount = 0;
            try
            {
                for (; sequenceCount < m_SequencePlayers.Length; sequenceCount++)
                    m_SequencePlayers[sequenceCount].BeginFrame();
                for (; blendSpaceCount < m_BlendSpacePlayers.Length; blendSpaceCount++)
                    m_BlendSpacePlayers[blendSpaceCount].BeginFrame();
                for (; stateMachineCount < m_StateMachines.Length; stateMachineCount++)
                    m_StateMachines[stateMachineCount].BeginFrame();
                for (; motionMatchingCount < m_MotionMatching.Length; motionMatchingCount++)
                    m_MotionMatching[motionMatchingCount].BeginFrame();
                m_FrameOpen = true;
            }
            catch
            {
                for (int i = motionMatchingCount - 1; i >= 0; i--)
                    m_MotionMatching[i].DiscardFrame();
                for (int i = stateMachineCount - 1; i >= 0; i--)
                    m_StateMachines[i].DiscardFrame();
                for (int i = blendSpaceCount - 1; i >= 0; i--)
                    m_BlendSpacePlayers[i].DiscardFrame();
                for (int i = sequenceCount - 1; i >= 0; i--)
                    m_SequencePlayers[i].DiscardFrame();
                throw;
            }
        }

        internal void DiscardFrame()
        {
            if (!m_FrameOpen)
                return;
            RestoreSourceSyncRelations();
            for (int i = m_MotionMatching.Length - 1; i >= 0; i--)
                m_MotionMatching[i].DiscardFrame();
            for (int i = m_StateMachines.Length - 1; i >= 0; i--)
                m_StateMachines[i].DiscardFrame();
            for (int i = m_BlendSpacePlayers.Length - 1; i >= 0; i--)
                m_BlendSpacePlayers[i].DiscardFrame();
            for (int i = m_SequencePlayers.Length - 1; i >= 0; i--)
                m_SequencePlayers[i].DiscardFrame();
            m_FrameOpen = false;
        }

        internal void CommitFrame()
        {
            if (!m_FrameOpen)
                throw new InvalidOperationException("Pose State and source frame is not open.");
            for (int i = 0; i < m_SequencePlayers.Length; i++)
                m_SequencePlayers[i].CommitFrame();
            for (int i = 0; i < m_BlendSpacePlayers.Length; i++)
                m_BlendSpacePlayers[i].CommitFrame();
            for (int i = 0; i < m_StateMachines.Length; i++)
                m_StateMachines[i].CommitFrame();
            for (int i = 0; i < m_MotionMatching.Length; i++)
                m_MotionMatching[i].CommitFrame();
            ClearSourceSyncJournal();
            m_FrameOpen = false;
        }

        internal void CopySourceSyncSnapshots(
            List<PoseStateSourceSyncSnapshot>
                destination)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(
                    nameof(destination));
            }
            destination.Clear();
            foreach (KeyValuePair<string,
                         SourceSyncRelationSlot> relation
                     in m_SourceSyncRelations)
            {
                if (!relation.Value.Active)
                    continue;
                destination.Add(
                    new PoseStateSourceSyncSnapshot(
                        relation.Key,
                        relation.Value.Cursor.Initialized,
                        relation.Value.Cursor.LeaderOrdinal,
                        relation.Value.Cursor.FollowerOrdinal,
                        relation.Value.TimeMapping,
                        relation.Value.PlanIdentity,
                        relation.Value.LeaderFraction,
                        relation.Value.FollowerFraction,
                        relation.Value.LeaderOccurrenceIndex,
                        relation.Value.FollowerOccurrenceIndex,
                        relation.Value.FollowerEffectiveTime));
            }
            destination.Sort(
                (left, right) =>
                    string.CompareOrdinal(
                        left.RelationId,
                        right.RelationId));
        }

        internal MotionMatchingPoseStateDemandBatch
            BuildMotionMatchingDemandBatch(
                ulong presentationFrame,
                ulong resetSequence,
                PresentationFrameWorkspace workspace,
                PresentationFrameWorkspaceLease lease)
        {
            if (presentationFrame == 0 ||
                workspace == null ||
                !lease.IsValid ||
                lease.PresentationFrame !=
                    presentationFrame)
                throw new ArgumentOutOfRangeException(
                    nameof(presentationFrame));
            int count = 0;
            for (int i = 0; i < m_MotionMatching.Length; i++)
            {
                MotionMatchingRelevance relevance =
                    m_MotionMatching[i];
                if (!IsOperationActive(
                        relevance.Usage.OperationIndex))
                {
                    continue;
                }
                if (!relevance.Relevant)
                    continue;
                workspace.AddProviderDemand(
                    lease,
                    new PoseSourceProviderDemand(
                        new PresentationPoseSourceProviderId(
                            relevance.ProviderId),
                        relevance.Usage.PlayerNodeId,
                        relevance.Usage
                            .PresentationPoseSourceIndex,
                        AnimationPoseSourceKind.MotionMatching,
                        new PoseSourceProviderDemandGeneration(
                            relevance.Generation),
                        relevance.DemandKind,
                        presentationFrame));
                m_MotionMatchingDemands[count++] =
                    new MotionMatchingPoseStateDemand(
                        relevance.ProviderId,
                        relevance.StateMachineIndex,
                        relevance.Usage.StateIndex,
                        relevance.Usage.PlayerIndex,
                        relevance.Usage.PlayerNodeId,
                        1f,
                        relevance.Generation,
                        resetSequence);
            }
            return new MotionMatchingPoseStateDemandBatch(
                presentationFrame,
                resetSequence,
                m_MotionMatchingDemands,
                count);
        }

        internal void ApplyMotionMatchingSelections(
            in MotionMatchingFrameResolution resolution,
            IDictionary<AnimationPlayerSourceSampleKey,
                PresentationPoseSourceSample> sourceSamples,
            PresentationFrameWorkspace workspace,
            PresentationFrameWorkspaceLease lease,
            IPoseStateSourceSelectionSink selectionSink)
        {
            if (resolution.CompletionIdentity == 0 ||
                sourceSamples == null ||
                workspace == null ||
                !lease.IsValid ||
                lease.PresentationFrame !=
                    resolution.PresentationFrame ||
                selectionSink == null)
            {
                throw new ArgumentException(
                    "Motion Matching Selection batch application is invalid.");
            }
            for (int i = 0;
                 i < resolution.SelectionCount;
                 i++)
            {
                MotionMatchingSelectionBatchItem item =
                    resolution.GetSelection(i);
                MotionMatchingRelevance relevance =
                    RequireMotionMatching(item);
                relevance.MarkReady(
                    resolution.PresentationFrame);
                PresentationPoseSourceSample sample =
                    item.SourceSample;
                if (item.SubmitToPlayer)
                {
                    workspace.SetProviderSample(
                        lease,
                        sample);
                    selectionSink.PushMotionMatchingSelection(
                        item.PlayerNodeId,
                        in sample);
                }
                var key =
                    new AnimationPlayerSourceSampleKey(
                        item.PlayerNodeId,
                        item.SourceIdentity);
                if (sourceSamples.ContainsKey(key))
                {
                    throw new InvalidOperationException(
                        $"Motion Matching Selection batch duplicates Player source '{item.PlayerNodeId}/{sample.SourceIndex}'.");
                }
                sourceSamples.Add(key, sample);
            }
            for (int i = 0; i < m_MotionMatching.Length; i++)
            {
                MotionMatchingRelevance relevance =
                    m_MotionMatching[i];
                if (IsOperationActive(
                        relevance.Usage.OperationIndex) &&
                    relevance.Relevant &&
                    relevance.LastResolvedFrame !=
                    resolution.PresentationFrame)
                {
                    relevance.MarkInvalid(
                        resolution.PresentationFrame,
                        PresentationPoseSourceFailureReason
                            .ProviderUnavailable);
                }
            }
        }

        internal void PrepareFrame(
            float presentationDeltaSeconds,
            in CharacterPresentationFactFrame factFrame)
        {
            if (!float.IsFinite(presentationDeltaSeconds) ||
                presentationDeltaSeconds < 0f ||
                !factFrame.IsValid)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(presentationDeltaSeconds));
            }
            for (int i = 0; i < m_StateMachines.Length; i++)
            {
                if (!IsStateMachineActive(i))
                    continue;
                m_StateMachines[i].PrepareFrame(
                    presentationDeltaSeconds,
                    in factFrame,
                    this);
            }
        }

        internal void AdvanceSources(
            float presentationDeltaSeconds,
            in CharacterPresentationFactFrame factFrame,
            in CharacterPresentationProgramParameterFrame
                parameterFrame)
        {
            if (!float.IsFinite(presentationDeltaSeconds) ||
                presentationDeltaSeconds < 0f ||
                !factFrame.IsValid ||
                !parameterFrame.IsValid)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(presentationDeltaSeconds));
            }
            for (int i = 0;
                 i < m_SequencePlayers.Length;
                 i++)
            {
                AnimationSequencePlayerRuntime player =
                    m_SequencePlayers[i];
                bool active =
                    IsPlayerActive(player.PlayerIndex);
                if (!m_StateControlledPlayers[player.PlayerIndex])
                    player.SetRelevant(active);
                if (!active)
                {
                    continue;
                }
                if (player.ClockSource == CharacterSequencePlayerClockSource.CommittedMovement)
                    player.SynchronizeMovementClock(
                        factFrame.MovementPlaybackTime,
                        factFrame.MovementPlaybackClock,
                        presentationDeltaSeconds);
                else
                    player.Advance(presentationDeltaSeconds);
            }
            for (int i = 0;
                 i < m_BlendSpacePlayers.Length;
                 i++)
            {
                AnimationBlendSpacePlayerRuntime player =
                    m_BlendSpacePlayers[i];
                bool active =
                    IsPlayerActive(player.PlayerIndex);
                if (!m_StateControlledPlayers[player.PlayerIndex])
                    player.SetRelevant(active);
                if (!active)
                {
                    continue;
                }
                player.SetParameterFrame(in parameterFrame);
                player.Advance(presentationDeltaSeconds);
            }
        }

        internal void EvaluateTransitions(
            in CharacterPresentationFactFrame factFrame,
            CharacterPoseGraphNativeProgram nativePlan,
            PresentationFrameWorkspace workspace,
            PresentationFrameWorkspaceLease lease)
        {
            if (!factFrame.IsValid)
                throw new ArgumentException(
                    "Pose State fact frame is invalid.");
            if (nativePlan == null)
                throw new ArgumentNullException(
                    nameof(nativePlan));
            if (workspace == null ||
                !lease.IsValid)
            {
                throw new ArgumentException(
                    "Pose State frame workspace is invalid.");
            }
            for (int i = 0;
                 i < m_StateMachines.Length;
                 i++)
            {
                if (!IsStateMachineActive(i))
                    continue;
                CharacterPoseStateMachineRuntime machine =
                    m_StateMachines[i];
                machine.EvaluateTransitions(
                    in factFrame,
                    this);
                if (!machine.CanPublishPose)
                {
                    PresentationFrameFailure failure =
                        machine.FrameFailure;
                    if (!failure.IsValid)
                    {
                        throw new InvalidOperationException(
                            "Pose StateMachine rejected its frame without a typed failure.");
                    }
                    workspace.Fail(
                        lease,
                        failure);
                    throw new InvalidOperationException(
                        failure.Detail);
                }
                CharacterPoseStateMachineNativeControl control =
                    machine.BuildNativeControl();
                nativePlan.SetStateMachineControl(
                    machine.Index,
                    in control);
            }
        }

        internal void NotifyNativeFrameCompleted(
            PoseInertializationNativeProgram inertializations,
            ulong completionIdentity)
        {
            if (inertializations == null ||
                completionIdentity == 0)
            {
                throw new ArgumentException(
                    "Pose State native completion is invalid.");
            }
            for (int i = 0;
                 i < m_StateMachines.Length;
                 i++)
            {
                if (!IsStateMachineActive(i))
                    continue;
                CharacterPoseStateMachineRuntime machine =
                    m_StateMachines[i];
                if (inertializations.TryGetStateMachineState(
                        machine.Index,
                        out PoseInertializationNativeState state))
                {
                    machine.NotifyNativeFrameCompleted(
                        in state,
                        completionIdentity);
                }
            }
        }

        internal void ApplyLinkedPoseGenerationResets()
        {
            if (!m_FrameOpen)
            {
                throw new InvalidOperationException(
                    "Pose State Linked Pose reset requires an open frame.");
            }
            for (int i = 0; i < m_SequencePlayers.Length; i++)
            {
                AnimationSequencePlayerRuntime player =
                    m_SequencePlayers[i];
                if (!RequiresPlayerReset(player.PlayerIndex))
                    continue;
                player.SetRelevant(false);
                player.ResetForStateEntry();
            }
            for (int i = 0; i < m_BlendSpacePlayers.Length; i++)
            {
                AnimationBlendSpacePlayerRuntime player =
                    m_BlendSpacePlayers[i];
                if (!RequiresPlayerReset(player.PlayerIndex))
                    continue;
                player.SetRelevant(false);
                player.ResetForStateEntry();
            }
            for (int i = 0; i < m_MotionMatching.Length; i++)
            {
                MotionMatchingRelevance relevance =
                    m_MotionMatching[i];
                int fragmentIndex =
                    RequireOperationFragmentIndex(
                        relevance.Usage.OperationIndex);
                if (RequiresFragmentReset(fragmentIndex))
                    relevance.Reset();
            }
            for (int i = 0; i < m_StateMachines.Length; i++)
            {
                if (!RequiresFragmentReset(
                        m_StateMachineLinkedPoseFragmentIndices[i]))
                {
                    continue;
                }
                m_StateMachines[i].Reset(
                    this,
                    TransitionRoutingResetReason.Explicit);
            }
        }

        bool IsStateMachineActive(int stateMachineIndex) =>
            IsFragmentActive(
                m_StateMachineLinkedPoseFragmentIndices[
                    stateMachineIndex]);

        bool IsPlayerActive(int playerIndex) =>
            IsFragmentActive(
                RequirePlayerFragmentIndex(playerIndex));

        bool RequiresPlayerReset(int playerIndex) =>
            RequiresFragmentReset(
                RequirePlayerFragmentIndex(playerIndex));

        bool IsOperationActive(int operationIndex) =>
            IsFragmentActive(
                RequireOperationFragmentIndex(operationIndex));

        int RequireOperationFragmentIndex(int operationIndex)
        {
            if (operationIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(operationIndex));
            MotionMatchingRelevance relevance =
                operationIndex < m_MotionMatchingByOperation.Length
                    ? m_MotionMatchingByOperation[operationIndex]
                    : null;
            if (relevance == null)
            {
                throw new InvalidOperationException(
                    $"Pose operation #{operationIndex} has no Motion Matching relevance ownership.");
            }
            return RequirePlayerFragmentIndex(
                relevance.Usage.PlayerIndex);
        }

        int RequirePlayerFragmentIndex(int playerIndex)
        {
            if ((uint)playerIndex >=
                (uint)m_PlayerLinkedPoseFragmentIndices.Length)
            {
                throw new InvalidOperationException(
                    $"Pose Player #{playerIndex} is outside the compiled Linked Pose ownership table.");
            }
            return m_PlayerLinkedPoseFragmentIndices[playerIndex];
        }

        bool IsFragmentActive(int fragmentIndex) =>
            fragmentIndex < 0 ||
            m_LinkedPoseActiveFragments[fragmentIndex];

        bool RequiresFragmentReset(int fragmentIndex) =>
            fragmentIndex >= 0 &&
            m_LinkedPoseResetFragments[fragmentIndex];

        internal void Reset(
            PoseDiscontinuityResetReason reason)
        {
            if (reason ==
                PoseDiscontinuityResetReason.None)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(reason));
            }
            for (int i = 0;
                 i < m_SequencePlayers.Length;
                 i++)
            {
                m_SequencePlayers[i].Reset(reason);
            }
            for (int i = 0;
                 i < m_BlendSpacePlayers.Length;
                 i++)
            {
                m_BlendSpacePlayers[i].Reset(reason);
            }
            for (int i = 0;
                 i < m_StateMachines.Length;
                 i++)
            {
                m_StateMachines[i].Reset(
                    this,
                    TransitionRoutingResetReason.Explicit);
            }
            ClearSourceSyncRelations();
        }

        void ICharacterPoseStateSourceRuntime.SetRelevant(
            PoseStateSourceProviderPlan usage,
            bool relevant,
            PoseSourceProviderDemandKind demandKind)
        {
            if (usage?.SourceKind ==
                AnimationPoseSourceKind.MotionMatching)
            {
                RequireMotionMatching(usage)
                    .SetRelevant(relevant, demandKind);
                return;
            }
            if (usage?.SourceKind ==
                AnimationPoseSourceKind.BlendSpace)
            {
                RequireBlendSpace(usage)
                    .SetRelevant(relevant);
                return;
            }
            RequireSequence(usage)
                .SetRelevant(relevant, demandKind);
        }

        void ICharacterPoseStateSourceRuntime.Reset(
            PoseStateSourceProviderPlan usage)
        {
            if (usage?.SourceKind ==
                AnimationPoseSourceKind.MotionMatching)
            {
                RequireMotionMatching(usage).Reset();
                return;
            }
            if (usage?.SourceKind ==
                AnimationPoseSourceKind.BlendSpace)
            {
                RequireBlendSpace(usage)
                    .ResetForStateEntry();
                return;
            }
            RequireSequence(usage)
                .ResetForStateEntry();
        }

        PoseSourceProviderStatus
            ICharacterPoseStateSourceRuntime.GetStatus(
                PoseStateSourceProviderPlan usage)
        {
            if (usage?.SourceKind ==
                AnimationPoseSourceKind.MotionMatching)
            {
                return RequireMotionMatching(usage).Status;
            }
            if (usage?.SourceKind ==
                AnimationPoseSourceKind.BlendSpace)
            {
                return RequireBlendSpace(usage).IsRelevant
                    ? PoseSourceProviderStatus.Ready(
                        usage.ProviderId)
                    : PoseSourceProviderStatus.Pending(
                        usage.ProviderId);
            }
            return RequireSequence(usage).IsRelevant
                ? PoseSourceProviderStatus.Ready(
                    usage.ProviderId)
                : PoseSourceProviderStatus.Pending(
                    usage.ProviderId);
        }

        float ICharacterPoseStateSourceRuntime
            .GetRemainingTime(
                PoseStateSourceProviderPlan usage)
        {
            if (usage?.SourceKind ==
                AnimationPoseSourceKind.MotionMatching)
            {
                return float.MaxValue;
            }
            if (usage?.SourceKind ==
                AnimationPoseSourceKind.BlendSpace)
            {
                return RequireBlendSpace(usage)
                    .RemainingTime;
            }
            return RequireSequence(usage)
                .RemainingTime;
        }

        bool ICharacterPoseStateSourceRuntime.TrySynchronize(
            CharacterPoseStateSourceSyncPlan plan,
            bool establishRelation,
            out double targetEffectiveTime)
        {
            targetEffectiveTime = 0d;
            if (plan == null)
                throw new ArgumentNullException(nameof(plan));
            if (plan.Mode == PoseStateSourceSyncMode.None)
                return true;
            if (plan.Mode !=
                    PoseStateSourceSyncMode.MarkerGroup ||
                !TryGetSourceClocks(
                    plan.SourcePlayerIndex,
                    out AnimationMarkerSyncBinding
                        sourceMarkerSync,
                    out double sourceRawTime,
                    out double sourceEffectiveTime) ||
                !TryGetSourceClocks(
                    plan.TargetPlayerIndex,
                    out AnimationMarkerSyncBinding
                        targetMarkerSync,
                    out double targetRawTime,
                    out double targetEffectiveSourceTime))
            {
                return false;
            }
            if (!m_SourceSyncRelations.TryGetValue(
                    plan.RelationId,
                    out SourceSyncRelationSlot relation))
            {
                throw new InvalidOperationException(
                    $"Pose State source sync relation '{plan.RelationId}' is not compiled into the active plan.");
            }
            if (!relation.Active && !establishRelation)
                return false;
            JournalSourceSyncRelation(relation);
            if (!relation.Active)
            {
                relation.Active = true;
                relation.Cursor.Initialized = false;
                relation.Cursor.LeaderOrdinal = 0;
                relation.Cursor.FollowerOrdinal = 0;
                relation.FiniteLeaderReleased = false;
            }
            if (relation.FiniteLeaderReleased)
            {
                return TryGetSourceEffectiveClock(
                    plan.TargetPlayerIndex,
                    out targetEffectiveTime);
            }
            AnimationMarkerSyncBinding leaderBinding = plan.SourceIsLeader
                ? sourceMarkerSync
                : targetMarkerSync;
            double leaderEffectiveTime = plan.SourceIsLeader
                ? sourceEffectiveTime
                : targetEffectiveSourceTime;
            MarkerMappedTime mapped =
                MarkerSegmentTimeMapper.MapDetailed(
                    leaderBinding,
                    leaderEffectiveTime,
                    plan.SourceIsLeader
                        ? targetMarkerSync
                        : sourceMarkerSync,
                    plan.SourceIsLeader
                        ? targetRawTime
                        : sourceRawTime,
                    relation.Cursor,
                    plan.FootPhaseWarp);
            double effective = mapped.ContinuousTime;
            relation.LeaderFraction = mapped.LeaderSegmentFraction;
            relation.FollowerFraction = mapped.FollowerSegmentFraction;
            relation.LeaderOccurrenceIndex = mapped.LeaderOccurrenceIndex;
            relation.FollowerOccurrenceIndex = mapped.FollowerOccurrenceIndex;
            relation.FollowerEffectiveTime = mapped.ContinuousTime;
            int followerPlayerIndex =
                plan.SourceIsLeader
                    ? plan.TargetPlayerIndex
                    : plan.SourcePlayerIndex;
            SetSourceClock(
                followerPlayerIndex,
                effective);
            relation.FiniteLeaderReleased =
                leaderBinding.SequenceTopology ==
                AnimationMarkerSequenceTopology.Finite &&
                leaderEffectiveTime >= leaderBinding.DurationSeconds;
            if (m_SequenceByPlayerIndex.TryGetValue(
                    plan.SourcePlayerIndex,
                    out AnimationSequencePlayerRuntime sourceSequence) &&
                m_SequenceByPlayerIndex.TryGetValue(
                    plan.TargetPlayerIndex,
                    out AnimationSequencePlayerRuntime targetSequence) &&
                sourceSequence.ClockSource ==
                    CharacterSequencePlayerClockSource.CommittedMovement &&
                targetSequence.ClockSource ==
                    CharacterSequencePlayerClockSource.CommittedMovement)
            {
                targetSequence.AlignMovementMarkerEpoch(sourceSequence);
            }
            if (!TryGetSourceEffectiveClock(
                    plan.TargetPlayerIndex,
                    out targetEffectiveTime))
            {
                throw new InvalidOperationException(
                    "Pose State synchronization target disappeared from the active runtime.");
            }
            return true;
        }

        void ICharacterPoseStateSourceRuntime
            .ReleaseSynchronization(
                CharacterPoseStateSourceSyncPlan plan)
        {
            ClearSynchronization(plan, true);
        }

        void ICharacterPoseStateSourceRuntime
            .ResetSynchronization(
                CharacterPoseStateSourceSyncPlan plan)
        {
            ClearSynchronization(plan, false);
        }

        void ClearSynchronization(
            CharacterPoseStateSourceSyncPlan plan,
            bool anchorFollower)
        {
            if (plan != null &&
                plan.Mode ==
                PoseStateSourceSyncMode.MarkerGroup &&
                m_SourceSyncRelations.TryGetValue(
                    plan.RelationId,
                    out SourceSyncRelationSlot relation) &&
                relation.Active)
            {
                if (anchorFollower)
                {
                    int followerPlayerIndex = plan.SourceIsLeader
                        ? plan.TargetPlayerIndex
                        : plan.SourcePlayerIndex;
                    AnchorSourceClock(followerPlayerIndex);
                }
                JournalSourceSyncRelation(relation);
                relation.Active = false;
            }
        }

        AnimationSequencePlayerRuntime RequireSequence(
            PoseStateSourceProviderPlan usage)
        {
            if (usage == null)
                throw new ArgumentNullException(nameof(usage));
            if (usage.SourceKind !=
                    AnimationPoseSourceKind.Sequence ||
                !m_SequenceByPlayerIndex.TryGetValue(
                    usage.PlayerIndex,
                    out AnimationSequencePlayerRuntime player) ||
                player.NodeId != usage.PlayerNodeId)
            {
                throw new InvalidOperationException(
                    $"Pose State source '{usage.PlayerNodeId}' is not installed in the active Sequence runtime.");
            }
            return player;
        }

        AnimationBlendSpacePlayerRuntime RequireBlendSpace(
            PoseStateSourceProviderPlan usage)
        {
            if (usage == null ||
                usage.SourceKind !=
                    AnimationPoseSourceKind.BlendSpace ||
                !m_BlendSpaceByNode.TryGetValue(
                    usage.PlayerNodeId,
                    out AnimationBlendSpacePlayerRuntime
                        player) ||
                player.PlayerIndex != usage.PlayerIndex)
            {
                throw new InvalidOperationException(
                    $"Pose State source '{usage?.PlayerNodeId}' is not installed in the active Blend Space runtime.");
            }
            return player;
        }

        MotionMatchingRelevance RequireMotionMatching(
            PoseStateSourceProviderPlan usage)
        {
            if (usage == null ||
                usage.SourceKind !=
                    AnimationPoseSourceKind.MotionMatching ||
                (uint)usage.OperationIndex >=
                (uint)m_MotionMatchingByOperation.Length ||
                m_MotionMatchingByOperation[
                    usage.OperationIndex] is not
                    MotionMatchingRelevance relevance ||
                relevance.Usage.StateIndex !=
                    usage.StateIndex ||
                relevance.Usage.PlayerNodeId !=
                    usage.PlayerNodeId)
            {
                throw new InvalidOperationException(
                    $"Motion Matching Pose State source '{usage?.PlayerNodeId}' is not installed in the active Pose Plan.");
            }
            return relevance;
        }

        MotionMatchingRelevance RequireMotionMatching(
            in MotionMatchingSelectionBatchItem item)
        {
            for (int i = 0;
                 i < m_MotionMatching.Length;
                 i++)
            {
                MotionMatchingRelevance relevance =
                    m_MotionMatching[i];
                if (string.Equals(
                        relevance.ProviderId,
                        item.ProviderId,
                        StringComparison.Ordinal) &&
                    relevance.StateMachineIndex ==
                        item.StateMachineIndex &&
                    relevance.Usage.StateIndex ==
                        item.StateIndex &&
                    relevance.Usage.PlayerIndex ==
                        item.PlayerIndex &&
                    relevance.Usage.PlayerNodeId ==
                        item.PlayerNodeId &&
                    relevance.Usage
                        .PresentationPoseSourceIndex ==
                        item.SourceSample.SourceIndex &&
                    item.SourceSample.ProviderId ==
                        relevance.Usage.ProviderId &&
                    (!item.SubmitToPlayer ||
                     item.DemandGeneration.Value ==
                        relevance.Generation))
                {
                    return relevance;
                }
            }
            throw new InvalidOperationException(
                $"Motion Matching provider '{item.ProviderId}' is not demanded by the active Pose Plan.");
        }

        bool TryGetSourceClocks(
            int playerIndex,
            out AnimationMarkerSyncBinding markerSync,
            out double rawContinuousTime,
            out double effectiveContinuousTime)
        {
            if (m_SequenceByPlayerIndex.TryGetValue(
                    playerIndex,
                    out AnimationSequencePlayerRuntime
                        sequence))
            {
                markerSync = sequence.MarkerSync;
                rawContinuousTime =
                    sequence.RawContinuousTime;
                effectiveContinuousTime = sequence.ContinuousTime;
                return true;
            }
            if (m_BlendSpaceByPlayerIndex.TryGetValue(
                    playerIndex,
                    out AnimationBlendSpacePlayerRuntime
                        blendSpace))
            {
                markerSync = blendSpace.MarkerSync;
                rawContinuousTime =
                    blendSpace.RawContinuousTime;
                effectiveContinuousTime = blendSpace.ContinuousTime;
                return true;
            }
            markerSync = null;
            rawContinuousTime = 0d;
            effectiveContinuousTime = 0d;
            return false;
        }

        void AnchorSourceClock(int playerIndex)
        {
            if (m_SequenceByPlayerIndex.TryGetValue(
                    playerIndex,
                    out AnimationSequencePlayerRuntime sequence))
            {
                if (sequence.IsRelevant)
                    sequence.AnchorSynchronizedTime();
                return;
            }
            if (m_BlendSpaceByPlayerIndex.TryGetValue(
                    playerIndex,
                    out AnimationBlendSpacePlayerRuntime blendSpace))
            {
                if (blendSpace.IsRelevant)
                    blendSpace.AnchorSynchronizedTime();
                return;
            }
            throw new InvalidOperationException(
                $"Pose State Player index '{playerIndex}' has no continuation source runtime.");
        }

        void SetSourceClock(
            int playerIndex,
            double continuousTime)
        {
            if (m_SequenceByPlayerIndex.TryGetValue(
                    playerIndex,
                    out AnimationSequencePlayerRuntime
                        sequence))
            {
                sequence.SetSynchronizedTime(
                    continuousTime);
                return;
            }
            if (m_BlendSpaceByPlayerIndex.TryGetValue(
                    playerIndex,
                    out AnimationBlendSpacePlayerRuntime
                        blendSpace))
            {
                blendSpace.SetSynchronizedTime(
                    continuousTime);
                return;
            }
            throw new InvalidOperationException(
                $"Pose State Player index '{playerIndex}' has no synchronized source runtime.");
        }

        bool TryGetSourceEffectiveClock(
            int playerIndex,
            out double continuousTime)
        {
            if (m_SequenceByPlayerIndex.TryGetValue(
                    playerIndex,
                    out AnimationSequencePlayerRuntime sequence))
            {
                continuousTime = sequence.ContinuousTime;
                return true;
            }
            if (m_BlendSpaceByPlayerIndex.TryGetValue(
                    playerIndex,
                    out AnimationBlendSpacePlayerRuntime blendSpace))
            {
                continuousTime = blendSpace.ContinuousTime;
                return true;
            }
            continuousTime = 0d;
            return false;
        }

        void JournalSourceSyncRelation(SourceSyncRelationSlot slot)
        {
            if (!m_FrameOpen || slot.Journaled)
                return;
            if (m_SourceSyncRelationJournalCount ==
                m_SourceSyncRelationJournal.Length)
            {
                throw new InvalidOperationException(
                    "Pose State source sync journal capacity was exceeded.");
            }
            m_SourceSyncRelationJournal[
                m_SourceSyncRelationJournalCount++] =
                new SourceSyncRelationJournalEntry
                {
                    Slot = slot,
                    Active = slot.Active,
                    Initialized = slot.Cursor.Initialized,
                    LeaderOrdinal = slot.Cursor.LeaderOrdinal,
                    FollowerOrdinal = slot.Cursor.FollowerOrdinal,
                    LeaderFraction = slot.LeaderFraction,
                    FollowerFraction = slot.FollowerFraction,
                    LeaderOccurrenceIndex = slot.LeaderOccurrenceIndex,
                    FollowerOccurrenceIndex = slot.FollowerOccurrenceIndex,
                    FollowerEffectiveTime = slot.FollowerEffectiveTime,
                    FiniteLeaderReleased = slot.FiniteLeaderReleased
                };
            slot.Journaled = true;
        }

        void RestoreSourceSyncRelations()
        {
            for (int i = m_SourceSyncRelationJournalCount - 1; i >= 0; i--)
            {
                SourceSyncRelationJournalEntry entry =
                    m_SourceSyncRelationJournal[i];
                entry.Slot.Active = entry.Active;
                entry.Slot.Cursor.Initialized = entry.Initialized;
                entry.Slot.Cursor.LeaderOrdinal = entry.LeaderOrdinal;
                entry.Slot.Cursor.FollowerOrdinal = entry.FollowerOrdinal;
                entry.Slot.LeaderFraction = entry.LeaderFraction;
                entry.Slot.FollowerFraction = entry.FollowerFraction;
                entry.Slot.LeaderOccurrenceIndex = entry.LeaderOccurrenceIndex;
                entry.Slot.FollowerOccurrenceIndex = entry.FollowerOccurrenceIndex;
                entry.Slot.FollowerEffectiveTime = entry.FollowerEffectiveTime;
                entry.Slot.FiniteLeaderReleased = entry.FiniteLeaderReleased;
            }
            ClearSourceSyncJournal();
        }

        void ClearSourceSyncJournal()
        {
            for (int i = 0; i < m_SourceSyncRelationJournalCount; i++)
            {
                SourceSyncRelationSlot slot =
                    m_SourceSyncRelationJournal[i].Slot;
                if (slot != null)
                    slot.Journaled = false;
                m_SourceSyncRelationJournal[i] = default;
            }
            m_SourceSyncRelationJournalCount = 0;
        }

        void ClearSourceSyncRelations()
        {
            foreach (SourceSyncRelationSlot slot in
                     m_SourceSyncRelations.Values)
            {
                if (!slot.Active)
                    continue;
                JournalSourceSyncRelation(slot);
                slot.Active = false;
            }
        }

        static Dictionary<string, SourceSyncRelationSlot>
            BuildSourceSyncRelations(
                CharacterPresentationPosePlan plan)
        {
            var result =
                new Dictionary<string, SourceSyncRelationSlot>(
                    StringComparer.Ordinal);
            for (int machineIndex = 0;
                 machineIndex < plan.StateMachines.Count;
                 machineIndex++)
            {
                CharacterPoseStateMachineDescriptor machine =
                    plan.StateMachines[machineIndex];
                for (int transitionIndex = 0;
                     transitionIndex < machine.Transitions.Count;
                     transitionIndex++)
                {
                    CharacterPoseStateSourceSyncPlan sync =
                        machine.Transitions[transitionIndex].SourceSync;
                    if (sync == null ||
                        sync.Mode != PoseStateSourceSyncMode.MarkerGroup ||
                        result.ContainsKey(sync.RelationId))
                    {
                        continue;
                    }
                    result.Add(
                        sync.RelationId,
                        new SourceSyncRelationSlot(sync));
                }
            }
            return result;
        }

        static MotionMatchingRelevance[]
            BuildMotionMatchingRelevance(
                CharacterPresentationPosePlan plan)
        {
            var result =
                new List<MotionMatchingRelevance>();
            for (int machineIndex = 0;
                 machineIndex < plan.StateMachines.Count;
                 machineIndex++)
            {
                CharacterPoseStateMachineDescriptor machine =
                    plan.StateMachines[machineIndex];
                for (int stateIndex = 0;
                     stateIndex < machine.States.Count;
                     stateIndex++)
                {
                    CharacterPoseStateDescriptor state =
                        machine.States[stateIndex];
                    for (int usageIndex = 0;
                         usageIndex <
                         state.SourceProviders.Count;
                         usageIndex++)
                    {
                        PoseStateSourceProviderPlan usage =
                            state.SourceProviders[usageIndex];
                        if (usage.SourceKind !=
                            AnimationPoseSourceKind
                                .MotionMatching)
                        {
                            continue;
                        }
                        if ((uint)usage.OperationIndex >=
                            (uint)plan.Operations.Count)
                        {
                            throw new InvalidOperationException(
                                "Motion Matching Pose State usage operation is invalid.");
                        }
                        CharacterPresentationPoseOperation
                            operation =
                                plan.Operations[
                                    usage.OperationIndex];
                        if ((operation.Code !=
                             CharacterPoseOperationCode
                                 .SelectedPosePlayer &&
                             operation.Code !=
                             CharacterPoseOperationCode
                                 .BlendStack) ||
                            operation.NodeId !=
                                usage.PlayerNodeId ||
                            operation.PlayerIndex !=
                                usage.PlayerIndex)
                        {
                            throw new InvalidOperationException(
                                $"Motion Matching Pose State Player '{usage.PlayerNodeId}' has no exact compiled Player operation.");
                        }
                        result.Add(
                            new MotionMatchingRelevance(
                                machine.Index,
                                usage,
                                usage.ProviderId.Value));
                    }
                }
            }
            return result.ToArray();
        }

        static bool[] BuildStateControlledPlayers(
            CharacterPresentationPosePlan plan)
        {
            var result = new bool[plan.PlayerCount];
            for (int machineIndex = 0;
                 machineIndex < plan.StateMachines.Count;
                 machineIndex++)
            {
                CharacterPoseStateMachineDescriptor machine =
                    plan.StateMachines[machineIndex];
                for (int stateIndex = 0;
                     stateIndex < machine.States.Count;
                     stateIndex++)
                {
                    IReadOnlyList<PoseStateSourceProviderPlan> providers =
                        machine.States[stateIndex].SourceProviders;
                    for (int providerIndex = 0;
                         providerIndex < providers.Count;
                         providerIndex++)
                    {
                        int playerIndex =
                            providers[providerIndex].PlayerIndex;
                        if ((uint)playerIndex >= (uint)result.Length)
                        {
                            throw new InvalidOperationException(
                                "Pose State source provider Player index is invalid.");
                        }
                        result[playerIndex] = true;
                    }
                }
            }
            return result;
        }

    }
}
