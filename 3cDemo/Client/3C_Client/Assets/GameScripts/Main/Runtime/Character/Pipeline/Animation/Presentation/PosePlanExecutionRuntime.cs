using System;
using System.Collections.Generic;
using System.Linq;
using Animancer;
using ThirdPersonCharacter.Animation.TransitionRouting;
using ThirdPersonCharacter.Pipeline.Animation.BlendStack;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Pipeline.Animation.Lifecycle;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;
using ThirdPersonCharacter.Pipeline.Animation.Sources;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonCharacter.Pipeline.Presentation.Animancer;
using ThirdPersonSimulation;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using Unity.Profiling;

namespace ThirdPersonCharacter.Pipeline.Animation.Presentation
{
    internal sealed class PosePlanExecutionRuntime :
        IDisposable,
        IPoseStateSourceSelectionSink
    {
        readonly struct CharacterPoseProgramPreparedState
        {
            internal CharacterPoseProgramPreparedState(
                in CharacterPoseFrameLineage lineage,
                float presentationDeltaSeconds,
                in CharacterPoseGraphNativeBinding frame,
                CharacterPoseGraphStagedExecutor executor,
                in AnimationFinalPoseNativeReadBinding finalRead)
            {
                Lineage = lineage;
                PresentationDeltaSeconds = presentationDeltaSeconds;
                Frame = frame;
                Executor = executor;
                FinalRead = finalRead;
            }

            internal CharacterPoseFrameLineage Lineage { get; }
            internal float PresentationDeltaSeconds { get; }
            internal CharacterPoseGraphNativeBinding Frame { get; }
            internal CharacterPoseGraphStagedExecutor Executor { get; }
            internal AnimationFinalPoseNativeReadBinding FinalRead { get; }
            internal bool IsValid =>
                Lineage.IsValid &&
                float.IsFinite(PresentationDeltaSeconds) &&
                PresentationDeltaSeconds >= 0f &&
                Executor != null &&
                Frame.CompletionIdentity == Lineage.CompletionIdentity &&
                FinalRead.CompletionIdentity == Lineage.CompletionIdentity;
        }

        struct CharacterPoseProgramPreparedPage
        {
            CharacterPoseFrameLineage m_Lineage;
            CharacterPoseGraphNativeBinding m_Frame;
            CharacterPoseGraphStagedExecutor m_Executor;
            AnimationFinalPoseNativeReadBinding m_FinalRead;
            float m_PresentationDeltaSeconds;
            bool m_HasValue;

            internal bool HasValue => m_HasValue;

            internal void Prepare(
                in CharacterPoseProgramPrepared prepared,
                float presentationDeltaSeconds,
                in CharacterPoseGraphNativeBinding frame,
                CharacterPoseGraphStagedExecutor executor,
                in AnimationFinalPoseNativeReadBinding finalRead)
            {
                if (m_HasValue)
                {
                    throw new InvalidOperationException(
                        "Pose Program prepared page already contains a frame.");
                }
                if (!prepared.IsValid ||
                    !float.IsFinite(presentationDeltaSeconds) ||
                    presentationDeltaSeconds < 0f ||
                    executor == null ||
                    frame.CompletionIdentity !=
                        prepared.Lineage.CompletionIdentity ||
                    finalRead.CompletionIdentity !=
                        prepared.Lineage.CompletionIdentity)
                {
                    throw new ArgumentException(
                        "Pose Program prepared page input is invalid.",
                        nameof(prepared));
                }
                m_Lineage = prepared.Lineage;
                m_PresentationDeltaSeconds = presentationDeltaSeconds;
                m_Frame = frame;
                m_Executor = executor;
                m_FinalRead = finalRead;
                m_HasValue = true;
            }

            internal CharacterPoseProgramPreparedState Consume(
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
                var state = new CharacterPoseProgramPreparedState(
                    in m_Lineage,
                    m_PresentationDeltaSeconds,
                    in m_Frame,
                    m_Executor,
                    in m_FinalRead);
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
                m_Executor = default;
                m_FinalRead = default;
                m_PresentationDeltaSeconds = 0f;
                m_HasValue = false;
            }
        }

        static readonly ProfilerMarker PrepareMarker =
            new ProfilerMarker("ThirdPerson.Presentation.Animation.Prepare");
        static readonly ProfilerMarker PrepareWorkspaceMarker =
            new ProfilerMarker("ThirdPerson.Presentation.Animation.Prepare.Workspace");
        static readonly ProfilerMarker PrepareStackMarker =
            new ProfilerMarker("ThirdPerson.Presentation.Animation.Prepare.Stack");
        static readonly ProfilerMarker PrepareDirectMarker =
            new ProfilerMarker("ThirdPerson.Presentation.Animation.Prepare.Direct");
        static readonly ProfilerMarker PrepareSequenceMarker =
            new ProfilerMarker("ThirdPerson.Presentation.Animation.Prepare.Clip");
        static readonly ProfilerMarker PrepareBlendSpaceMarker =
            new ProfilerMarker("ThirdPerson.Presentation.Animation.Prepare.BlendSpace");
        static readonly ProfilerMarker ValidateMarker =
            new ProfilerMarker("ThirdPerson.Presentation.Animation.Validate");
        static readonly ProfilerMarker GraphEvaluateMarker =
            new ProfilerMarker("ThirdPerson.Presentation.Animation.GraphEvaluate");
        static readonly ProfilerMarker PoseGraphExecuteMarker =
            new ProfilerMarker("ThirdPerson.Presentation.Animation.PoseGraphExecute");
        static readonly ProfilerMarker FinalWriteMarker =
            new ProfilerMarker("ThirdPerson.Presentation.Animation.FinalWrite");
        static readonly ProfilerMarker SealMarker =
            new ProfilerMarker("ThirdPerson.Presentation.Animation.Seal");
        static readonly ProfilerMarker DiagnosticsMarker =
            new ProfilerMarker("ThirdPerson.Presentation.Animation.Diagnostics");
        struct PreparedMotionMatchingHistoryRead
        {
            internal MotionMatchingSelectionBatchItem Selection;
            internal int PlayerIndex;
            internal bool SourceUsed;
        }

        readonly AnimancerComponent m_Animancer;
        readonly CharacterPresentationProjection m_Projection;
        readonly CharacterPoseProgramRuntime m_ProgramRuntime;
        readonly CharacterPoseConstraintRuntime m_PoseConstraints;
        readonly CharacterPoseSourceModule m_SourceModule;
        readonly CharacterFinalPosePublication m_FinalPublication;
        readonly AnimationPoseSourceContribution[]
            m_FootPlacementContributions;
        readonly AnimationPresentationRuntimeSnapshotPublisher m_DiagnosticsPublisher;
        readonly CharacterFootIkCommittedCaptureViewProjector
            m_FootIkDiagnosticsProjector =
                new CharacterFootIkCommittedCaptureViewProjector();
        readonly CharacterPoseActorCommittedDiagnosticsProjector
            m_ActorDiagnosticsProjector;
        readonly CharacterPoseProgramCommittedDiagnosticsProjector
            m_ProgramDiagnosticsProjector;
        readonly AnimationSlotBlendJob[] m_SlotJobs;
        readonly AnimationSelectedPosePlayerJob[] m_DirectPlayerJobs;
        readonly AnimationSelectedPosePlayerJob[] m_ClipPlayerJobs;
        readonly AnimationSelectedPosePlayerJob[] m_BlendSpacePlayerJobs;
        readonly MotionMatchingPosePlanHistoryCompletion[] m_MotionMatchingHistoryCompletions;
        readonly PreparedMotionMatchingHistoryRead[]
            m_PreparedMotionMatchingHistoryReads;
        readonly bool m_ManagesGraphClock;
        readonly int m_FootPlacementWeightParameterIndex;
        readonly bool[] m_LinkedPoseActiveFragments;
        readonly bool[] m_LinkedPoseResetFragments;
        readonly int[] m_PlayerLinkedPoseFragmentIndices;
        readonly int[] m_StateMachineLinkedPoseFragmentIndices;
        readonly int[] m_RootOrientationWarpLinkedPoseFragmentIndices;
        readonly int[] m_InertializationLinkedPoseFragmentIndices;

        CharacterPoseProgramFramePages m_ProgramFrames =>
            m_ProgramRuntime.FramePages;
        CharacterPoseProgramTuningState m_ProgramTuning =>
            m_ProgramRuntime.Tuning;
        CharacterPoseProgramExecutionView m_ExecutionView =>
            m_ProgramRuntime.ExecutionView;
        CharacterPoseActorState m_ActorState =>
            m_ProgramRuntime.ActorState;
        PoseInertializationNativeProgram m_InertializationPlan =>
            m_ActorState.Inertialization;
        CharacterPoseProgramSourceRetirementState m_SourceRetirementState =>
            m_ActorState.SourceRetirement;
        AnimationBlendStackRuntime[] m_Stacks => m_ActorState.Stacks;
        CharacterAnimationTransitionRouteRuntime[] m_StackRoutes =>
            m_ActorState.Routes;
        AnimationSelectedPosePlayerRuntime[] m_DirectPlayers =>
            m_ActorState.DirectPlayers;
        PoseStateAndSourceRuntime m_PoseStateSources =>
            m_ActorState.PoseStateSources;
        RootOrientationWarpRuntime[] m_RootOrientationWarps =>
            m_ActorState.RootOrientationWarps;
        CharacterPoseProgramNodeRuntimeIndex m_NodeRuntimeIndex =>
            m_ActorState.NodeRuntimeIndex;

        AnimationScriptPlayable[] m_SlotPlayables;
        AnimationScriptPlayable[] m_DirectPlayerPlayables;
        AnimationScriptPlayable[] m_ClipPlayerPlayables;
        AnimationScriptPlayable[] m_BlendSpacePlayerPlayables;
        ulong m_CompletionIdentity = 1;
        ulong m_FrameCompletionContext;
        CharacterPoseProgramPreparedPage m_PreparedPage;
        int m_PreparedMotionMatchingHistoryReadCount;
        ulong m_PreparedMotionMatchingPresentationFrame;
        ulong m_PreparedMotionMatchingResetSequence;
        ulong m_PreparedMotionMatchingSelectionCompletionIdentity;
        ulong m_PreparedMotionMatchingPoseCompletionIdentity;
        bool m_MotionMatchingPoseCompletionPrepared;
        int m_MotionMatchingHistoryCompletionCount;
        CharacterPoseGraphNativeBinding m_LastCompletedFrame;
        CharacterPoseGraphNativeBinding m_PendingCompletedFrame;
        CharacterPoseProgramFrameLease m_ActiveFrameLease;
        bool m_CommitValidated;
        bool m_HasCompletedFrame;
        bool m_HasPendingCompletedFrame;
        bool m_HasOpenFrame;
        AnimationPresentationFrameOutcome m_PendingFrameOutcome;
        int m_SequencePreviewPlayerIndex = -1;
        int m_SequencePreviewOperationIndex = -1;
        double m_SequencePreviewTime;
        bool m_SequencePreviewReset;
        bool m_HasSequencePreview;
        bool m_JobsInstalled;
        bool m_Disposed;

        internal PosePlanExecutionRuntime(
            AnimancerComponent animancer,
            CharacterAnimationRigBinding rigBinding,
            CharacterRootHierarchyBinding rootHierarchy,
            CharacterPresentationProjection projection,
            CharacterActionPlaybackRuntime actionPlayback,
            AnimationSlotRuntime animationSlots,
            CharacterFootPlacementModule footPlacement,
            bool managesGraphClock)
        {
            m_Animancer = animancer ? animancer : throw new ArgumentNullException(nameof(animancer));
            m_Projection = projection ?? throw new ArgumentNullException(nameof(projection));
            Animator animator = m_Animancer.Animator;
            if (!animator || animator.cullingMode != AnimatorCullingMode.AlwaysAnimate)
            {
                throw new InvalidOperationException(
                    "Animation Presentation requires an AlwaysAnimate Animator because Pose jobs produce the frame transaction payload.");
            }
            projection.RequirePosePayload();
            m_LinkedPoseActiveFragments =
                new bool[projection.PosePlan.LinkedPoseFragments.Count];
            m_LinkedPoseResetFragments =
                new bool[projection.PosePlan.LinkedPoseFragments.Count];
            m_PlayerLinkedPoseFragmentIndices =
                BuildPlayerLinkedPoseFragmentIndices(
                    projection.PosePlan);
            m_StateMachineLinkedPoseFragmentIndices =
                BuildStateMachineLinkedPoseFragmentIndices(
                    projection.PosePlan);
            m_RootOrientationWarpLinkedPoseFragmentIndices =
                BuildRootOrientationWarpLinkedPoseFragmentIndices(
                    projection.PosePlan);
            m_InertializationLinkedPoseFragmentIndices =
                BuildInertializationLinkedPoseFragmentIndices(
                    projection.PosePlan);
            int sourceCapacity = CalculateSourceCapacity(projection.PosePlan);
            int physicalSourceCapacity = checked(
                sourceCapacity +
                projection.PosePlan.ClipPlayers.Count);
            int clipCatalogCapacity =
                AnimationPoseRequestWorkspaceLayoutFactory
                    .RequireClipCatalogCapacity(projection);

            AnimationPoseNativeWorkspace workspace = null;
            CharacterPoseProgramFramePages programFrames = null;
            CharacterPoseProgramTuningState programTuning = null;
            CharacterPoseProgramExecutionView executionView = null;
            CharacterFinalIkFullBodySolver fullBodyIkSolver = null;
            CharacterPoseConstraintRuntime poseConstraints = null;
            PoseInertializationNativeProgram inertializationProgram = null;
            CharacterPoseSourceModule sourceModule = null;
            AnimationBlendStackRuntime[] stacks = null;
            CharacterAnimationTransitionRouteRuntime[] stackRoutes = null;
            AnimationSelectedPosePlayerRuntime[] directPlayers = null;
            AnimationClipPlayerRuntime[] clipPlayers = null;
            AnimationBlendSpacePlayerRuntime[] blendSpacePlayers = null;
            PoseStateAndSourceRuntime poseStateSources = null;
            AnimationPresentationRuntimeSnapshotPublisher diagnosticsPublisher = null;
            CharacterPoseActorCommittedDiagnosticsProjector
                actorDiagnosticsProjector = null;
            CharacterPoseProgramCommittedDiagnosticsProjector
                programDiagnosticsProjector = null;
            CharacterFinalPosePublication finalPublication = null;
            var nodeRuntimeIndex =
                new CharacterPoseProgramNodeRuntimeIndex();
            try
            {
                workspace = new AnimationPoseNativeWorkspace(projection);
                CharacterPoseGraphNativeBinding initialFrame = workspace.BeginFrame(m_CompletionIdentity);
                AnimationPoseNativeAggregateLayout initialLayout =
                    initialFrame.Layout;
                programFrames = new CharacterPoseProgramFramePages(
                    projection.PosePlan,
                    physicalSourceCapacity,
                    workspace);
                workspace = null;
                executionView = new CharacterPoseProgramExecutionView(
                    projection,
                    in initialLayout);
                programDiagnosticsProjector =
                    new CharacterPoseProgramCommittedDiagnosticsProjector(
                        executionView);
                programTuning = new CharacterPoseProgramTuningState(
                    projection,
                    executionView.Operations,
                    1);
                if (projection.PosePlan.FullBodyIks.Count != 1)
                    throw new InvalidOperationException(
                        "Pose Plan requires exactly one Full Body IK descriptor.");
                CharacterPresentationFullBodyIkDescriptor fullBodyIkDescriptor =
                    projection.PosePlan.FullBodyIks[0];
                fullBodyIkDescriptor.RequireValid();
                fullBodyIkSolver = new CharacterFinalIkFullBodySolver(
                    projection.Rig,
                    fullBodyIkDescriptor.Profile,
                    executionView.ParentIndices,
                    executionView.VirtualBones);
                inertializationProgram = new PoseInertializationNativeProgram(
                    projection.PosePlan,
                    projection.BlendCurveCatalog,
                    projection.BlendProfileCatalog);
                stacks = new AnimationBlendStackRuntime[projection.PosePlan.BlendNodes.Count];
                stackRoutes = new CharacterAnimationTransitionRouteRuntime[stacks.Length];
                Dictionary<PoseNodeId, CharacterAnimationSlotDescriptor> slotsByNode =
                    projection.PosePlan.AnimationSlots.ToDictionary(value => value.NodeId);
                for (int stackIndex = 0; stackIndex < stacks.Length; stackIndex++)
                {
                    AnimationBlendNodePayload blendNode = projection.PosePlan.BlendNodes[stackIndex] ??
                        throw new InvalidOperationException($"Pose Plan Blend Stack #{stackIndex} is missing.");
                    CharacterPresentationPoseOperation operation = RequireBlendStackOperation(
                        projection.PosePlan,
                        stackIndex,
                        blendNode.NodeId);
                    slotsByNode.TryGetValue(
                        blendNode.NodeId,
                        out CharacterAnimationSlotDescriptor
                            slotDescriptor);
                    var route =
                        new CharacterAnimationTransitionRouteRuntime(
                            blendNode,
                            slotDescriptor);
                    CharacterPresentationPoseOperation input =
                        route.IsAnimationSlot
                            ? RequireControlInput(
                                projection.PosePlan,
                                operation)
                            : null;
                    if (!route.IsAnimationSlot &&
                        (!operation
                             .PresentationPoseSourceProviderId
                             .IsValid ||
                         !operation.PresentationPoseSourceIndex
                             .IsValid))
                    {
                        throw new InvalidOperationException(
                            $"Pose State Blend Stack '{operation.NodeId}' has no compiled provider identity.");
                    }
                    AnimationPlayerPoseNativeWriteBinding initialWrite =
                        programFrames.RequirePlayerWriteBinding(
                            operation.PlayerIndex,
                            initialFrame.CompletionIdentity);
                    var stack = new AnimationBlendStackRuntime(
                        blendNode,
                        route.IsAnimationSlot
                            ? input.AnimationChannelId
                            : default,
                        route.IsAnimationSlot
                            ? default
                            : operation
                                .PresentationPoseSourceProviderId,
                        route.IsAnimationSlot
                            ? default
                            : operation.PresentationPoseSourceIndex,
                        operation.SelectionAvailability,
                        projection.BlendCurveCatalog,
                        projection.BlendProfileCatalog,
                        projection.Rig,
                        in initialWrite);
                    stacks[stackIndex] = stack;
                    stackRoutes[stackIndex] = route;
                    nodeRuntimeIndex.AddStack(
                        blendNode.NodeId,
                        stack,
                        route,
                        operation.PlayerIndex,
                        route.IsAnimationSlot ? -1 : 0);
                    if (route.IsAnimationSlot)
                    {
                        CharacterAnimationSlotNativeControl control = route.NativeControl;
                        programFrames.SetAnimationSlotControl(
                            route.AnimationSlotIndex,
                            in control);
                    }
                }
                var directPlayerList = new List<AnimationSelectedPosePlayerRuntime>();
                for (int operationIndex = 0; operationIndex < projection.PosePlan.Operations.Count; operationIndex++)
                {
                    CharacterPresentationPoseOperation operation = projection.PosePlan.Operations[operationIndex];
                    if (operation.Code != CharacterPoseOperationCode.SelectedPosePlayer)
                        continue;
                    if (operation.PlayerIndex < 0 ||
                        !operation
                            .PresentationPoseSourceProviderId
                            .IsValid ||
                        !operation.PresentationPoseSourceIndex
                            .IsValid)
                        throw new InvalidOperationException($"Selected Pose Player operation '{operation.NodeId}' has invalid compiled inputs.");
                    var player = new AnimationSelectedPosePlayerRuntime(
                        operation.NodeId,
                        operation.PlayerIndex,
                        operation.PlayerIndex,
                        operation
                            .PresentationPoseSourceProviderId,
                        operation.SelectionAvailability,
                        projection.Rig,
                        projection.PosePlan.Parameters.Count);
                    directPlayerList.Add(player);
                    nodeRuntimeIndex.AddDirect(
                        operation.NodeId,
                        player,
                        operation.PlayerIndex,
                        operation.PlayerIndex);
                }
                directPlayers = directPlayerList.ToArray();
                clipPlayers = new AnimationClipPlayerRuntime[projection.PosePlan.ClipPlayers.Count];
                for (int clipPlayerIndex = 0; clipPlayerIndex < clipPlayers.Length; clipPlayerIndex++)
                {
                    AnimationClipPlayerRuntime clipPlayer = AnimationClipPlayerFactory.Create(
                        projection,
                        projection.PosePlan.ClipPlayers[clipPlayerIndex]);
                    clipPlayers[clipPlayerIndex] = clipPlayer;
                    nodeRuntimeIndex.AddPlayer(
                        clipPlayer.NodeId,
                        clipPlayer.PlayerIndex);
                }
                blendSpacePlayers =
                    new AnimationBlendSpacePlayerRuntime[projection.BlendSpacePlayers.Count];
                for (int blendSpaceIndex = 0;
                     blendSpaceIndex < blendSpacePlayers.Length;
                     blendSpaceIndex++)
                {
                    CharacterAnimationBlendSpacePlayerPlan descriptor =
                        projection.BlendSpacePlayers[blendSpaceIndex];
                    descriptor.RequireValid(projection);
                    var player = new AnimationBlendSpacePlayerRuntime(
                        descriptor,
                        projection.BlendSpaces[descriptor.BlendSpacePlanIndex],
                        projection.PosePlan,
                        projection.Rig,
                        projection.FootAnalysis,
                        projection.ClipPhasePlans);
                    blendSpacePlayers[blendSpaceIndex] = player;
                    nodeRuntimeIndex.AddPlayer(
                        player.NodeId,
                        player.PlayerIndex);
                }
                poseStateSources =
                    new PoseStateAndSourceRuntime(
                        projection.PosePlan,
                        projection.ClipPhasePlans,
                        projection.SourcePhasePlans,
                        clipPlayers,
                        blendSpacePlayers,
                        m_PlayerLinkedPoseFragmentIndices,
                        m_StateMachineLinkedPoseFragmentIndices,
                        m_LinkedPoseActiveFragments,
                        m_LinkedPoseResetFragments);
                poseConstraints = new CharacterPoseConstraintRuntime(
                    footPlacement,
                    executionView.PoseBoneContributions,
                    executionView.GoalAssemblers,
                    fullBodyIkSolver,
                    executionView.FullBodyIkGoalContributionCount,
                    executionView.FullBodyIkContributionGoalCount,
                    projection.Rig.RigId,
                    projection.Rig.RigRevision);
                diagnosticsPublisher = new AnimationPresentationRuntimeSnapshotPublisher(
                    projection,
                    in initialLayout,
                    physicalSourceCapacity);
                actorDiagnosticsProjector =
                    new CharacterPoseActorCommittedDiagnosticsProjector(
                        projection,
                        in initialLayout);

                sourceModule = new CharacterPoseSourceModule(
                    animancer,
                    projection,
                    rigBinding,
                    projection.Rig,
                    physicalSourceCapacity,
                    clipCatalogCapacity,
                    directPlayers.Length,
                    clipPlayers.Length,
                    blendSpacePlayers.Length);
                finalPublication = new CharacterFinalPosePublication(
                    projection.PosePlan,
                    projection.Rig,
                    rigBinding,
                    rootHierarchy);
                if (managesGraphClock)
                    animancer.Graph.PauseGraph();
                programFrames.DiscardEvaluationFrame(
                    initialFrame.CompletionIdentity);
            }
            catch
            {
                sourceModule?.Dispose();
                if (stacks != null)
                {
                    for (int i = stacks.Length - 1; i >= 0; i--)
                        stacks[i]?.Dispose();
                }
                if (directPlayers != null)
                {
                    for (int i = directPlayers.Length - 1; i >= 0; i--)
                        directPlayers[i]?.Dispose();
                }
                if (clipPlayers != null)
                {
                    for (int i = clipPlayers.Length - 1; i >= 0; i--)
                        clipPlayers[i]?.Dispose();
                }
                if (blendSpacePlayers != null)
                {
                    for (int i = blendSpacePlayers.Length - 1; i >= 0; i--)
                        blendSpacePlayers[i]?.Dispose();
                }
                diagnosticsPublisher?.Dispose();
                poseConstraints?.Dispose();
                executionView?.Dispose();
                inertializationProgram?.Dispose();
                programTuning?.Dispose();
                programFrames?.Dispose();
                workspace?.Dispose();
                throw;
            }

            m_PoseConstraints = poseConstraints;
            m_SourceModule = sourceModule;
            var rootOrientationWarps =
                new RootOrientationWarpRuntime[
                    projection.PosePlan.RootOrientationWarps.Count];
            for (int i = 0; i < rootOrientationWarps.Length; i++)
            {
                CharacterPresentationRootOrientationWarpDescriptor descriptor =
                    projection.PosePlan.RootOrientationWarps[i];
                rootOrientationWarps[i] =
                    new RootOrientationWarpRuntime(
                        descriptor,
                        clipPlayers[descriptor.ClipPlayerIndex]);
            }
            var actorState = new CharacterPoseActorState(
                stacks,
                stackRoutes,
                directPlayers,
                poseStateSources,
                rootOrientationWarps,
                inertializationProgram,
                nodeRuntimeIndex,
                actionPlayback,
                animationSlots,
                sourceModule.Capacity);
            m_ProgramRuntime = new CharacterPoseProgramRuntime(
                projection.PosePlan,
                executionView,
                actorState,
                programFrames,
                programTuning,
                poseConstraints);
            m_MotionMatchingHistoryCompletions =
                new MotionMatchingPosePlanHistoryCompletion[
                    m_PoseStateSources
                        .MotionMatchingProviderCount];
            m_PreparedMotionMatchingHistoryReads =
                new PreparedMotionMatchingHistoryRead[
                    m_MotionMatchingHistoryCompletions.Length];
            m_SlotJobs = new AnimationSlotBlendJob[stacks.Length];
            m_DirectPlayerJobs = new AnimationSelectedPosePlayerJob[directPlayers.Length];
            m_ClipPlayerJobs = new AnimationSelectedPosePlayerJob[clipPlayers.Length];
            m_BlendSpacePlayerJobs =
                new AnimationSelectedPosePlayerJob[blendSpacePlayers.Length];
            m_FinalPublication = finalPublication;
            m_FootPlacementContributions =
                new AnimationPoseSourceContribution[
                    projection.PosePlan.ContributionWorkspaceCount /
                    projection.PosePlan.PoseValueWorkspaceCount];
            m_DiagnosticsPublisher = diagnosticsPublisher;
            m_ActorDiagnosticsProjector = actorDiagnosticsProjector;
            m_ProgramDiagnosticsProjector =
                programDiagnosticsProjector;
            m_ManagesGraphClock = managesGraphClock;
            m_FootPlacementWeightParameterIndex = projection.PosePlan.RequireParameterIndex(
                AnimationPoseParameterIds.FootPlacementWeight);
        }

        internal bool HasDiagnosticsSnapshot => m_DiagnosticsPublisher.HasCurrent;
        internal AnimationPresentationRuntimeSnapshot DiagnosticsSnapshot => m_DiagnosticsPublisher.Current;
        internal AnimationPresentationDiagnosticsInterest DiagnosticsInterest =>
            m_DiagnosticsPublisher.Interest;
        internal ulong DiagnosticsNoInterestSkipCount =>
            m_DiagnosticsPublisher.NoInterestSkipCount;
        internal bool HasFootPlacement => m_PoseConstraints.HasFootPlacement;
        internal CharacterPoseConstraintRuntime PoseConstraints =>
            m_PoseConstraints;
        internal CharacterActionPlaybackRuntime ActionPlayback =>
            m_ActorState.ActionPlayback;
        internal AnimationSlotRuntime AnimationSlots =>
            m_ActorState.AnimationSlots;

        internal ulong NextPresentationRequestSequence() =>
            m_ActorState.NextPresentationRequestSequence();

        internal void ResetFootPlacement(in CharacterFootPlacementReset reset) =>
            m_PoseConstraints.ResetFootPlacement(in reset);

        internal void RetargetFootPlacement(ulong resetSequence) =>
            m_PoseConstraints.RetargetFootPlacement(resetSequence);

        internal void DiscardPoseFrameAfterBarrier(
            CharacterPoseConstraintFrameLease constraintLease,
            CharacterFinalPosePublicationFrameLease publicationLease)
        {
            Exception failure = null;
            DiscardStep(
                () => m_FinalPublication.DiscardPending(publicationLease),
                ref failure);
            DiscardStep(
                () => m_PoseConstraints.DiscardFrame(constraintLease),
                ref failure);
            if (failure != null)
            {
                throw new AggregateException(
                    "Pose frame post-barrier Pending discard failed.",
                    failure);
            }
        }

        internal string ApplyTuning(
            CharacterPoseTuningLayout layout,
            CharacterPoseTuningParameterBlock block,
            ulong candidateGeneration,
            bool resetOwnerState)
        {
            if (layout == null || block == null)
                return "Pose tuning payload is missing.";
            string sourceError = m_SourceModule.PrepareTuningCandidate(
                layout,
                block,
                candidateGeneration);
            if (!string.IsNullOrEmpty(sourceError))
                return sourceError;
            try
            {
                m_ProgramTuning.PrepareCandidate(
                    layout,
                    block,
                    candidateGeneration);
            }
            catch (Exception exception)
            {
                m_SourceModule.DiscardTuningCandidate();
                return exception.Message;
            }
            string error;
            try
            {
                error = ApplyMutableTuning(
                    layout,
                    block,
                    resetOwnerState);
            }
            catch
            {
                m_ProgramTuning.DiscardCandidate();
                m_SourceModule.DiscardTuningCandidate();
                throw;
            }
            if (!string.IsNullOrEmpty(error))
            {
                m_ProgramTuning.DiscardCandidate();
                m_SourceModule.DiscardTuningCandidate();
                return error;
            }
            m_ProgramTuning.CommitCandidate(candidateGeneration);
            m_SourceModule.CommitTuningCandidate(candidateGeneration);
            return string.Empty;
        }

        internal string RestoreMutableTuning(
            CharacterPoseTuningLayout layout,
            CharacterPoseTuningParameterBlock block) =>
            ApplyMutableTuning(layout, block, false);

        string ApplyMutableTuning(
            CharacterPoseTuningLayout layout,
            CharacterPoseTuningParameterBlock block,
            bool resetOwnerState)
        {
            for (int i = 0; i < m_PoseStateSources.StateMachines.Length; i++)
            {
                string error = m_PoseStateSources.StateMachines[i].ApplyTuning(
                    layout,
                    block);
                if (!string.IsNullOrEmpty(error))
                    return error;
            }
            for (int i = 0; i < m_Stacks.Length; i++)
            {
                string error = m_Stacks[i].ApplyTuning(layout, block);
                if (!string.IsNullOrEmpty(error))
                    return error;
            }
            string inertializationError =
                m_InertializationPlan.ApplyTuning(layout, block);
            if (!string.IsNullOrEmpty(inertializationError))
                return inertializationError;
            return m_PoseConstraints.ApplyTuning(
                layout,
                block,
                resetOwnerState);
        }

        internal bool CanApplyNextActivation =>
            m_PoseStateSources.CanApplyNextActivation;
        internal AnimationPresentationRuntimeCapacityMetrics
            CreateCapacityMetrics(
                int actionJournalCapacity,
                int samplingJournalCapacity,
                int slotJournalCapacity) =>
            new AnimationPresentationRuntimeCapacityMetrics(
                m_ProgramFrames.DenseDoublePageResidentPayloadBytes,
                PoseInertializationNativeProgramPayloadMetrics
                    .CalculateDoublePageResidentPayloadBytes(
                        m_InertializationPlan),
                m_FinalPublication
                    .DenseDoublePageResidentPayloadBytes,
                actionJournalCapacity,
                samplingJournalCapacity,
                slotJournalCapacity,
                m_SourceRetirementState.StandaloneCapacity,
                m_SourceModule.Capacity,
                m_SourceRetirementState.StandaloneCapacity);

        internal void RecordNoDiagnosticsInterest() =>
            m_DiagnosticsPublisher.RecordNoInterestSkip();
        internal ulong FrameCompletionContext =>
            m_FrameCompletionContext;

        internal void CopySourceSyncSnapshots(
            List<PoseStateSourceSyncSnapshot>
                destination)
        {
            RequireAlive();
            RequireNoOpenMutation();
            m_PoseStateSources.CopySourceSyncSnapshots(
                destination);
        }

        internal CharacterPoseProgramFrameLease BeginPendingFrame(
            in CharacterPoseFrameLineage lineage,
            AnimationPresentationDiagnosticsInterest diagnosticsInterest,
            CharacterFootIkCaptureInterest footIkCaptureInterest,
            CharacterLinkedPoseRuntimeSession linkedPose,
            out CharacterPoseSourceFrameLease sourceLease,
            out CharacterPoseConstraintFrameLease constraintLease,
            out CharacterFinalPosePublicationFrameLease publicationLease)
        {
            sourceLease = default;
            constraintLease = default;
            publicationLease = default;
            RequireAlive();
            var programLease =
                new CharacterPoseProgramFrameLease(in lineage);
            if (linkedPose == null)
                throw new ArgumentNullException(nameof(linkedPose));
            ulong frameIdentity = lineage.FrameIdentity;
            ulong presentationFrame = lineage.PresentationFrame;
            if (m_ActiveFrameLease.IsValid)
            {
                throw new InvalidOperationException(
                    "Pose Plan frame mutation is already open.");
            }
            if (m_PreparedPage.HasValue)
            {
                throw new InvalidOperationException(
                    "Pose Program prepared state from the previous frame was not consumed.");
            }
            if (m_CommitValidated)
            {
                throw new InvalidOperationException(
                    "Pose Plan committed-frame finalization is still pending.");
            }
            if (m_SourceRetirementState.HasPreparedStandalone)
            {
                throw new InvalidOperationException(
                    "Pose Plan standalone source releases from the committed frame were not finalized.");
            }
            if (m_SourceModule.ReleaseAcknowledgementsValidated)
            {
                throw new InvalidOperationException(
                    "Pose Plan action backend acknowledgements from the previous frame were not applied.");
            }
            if (m_MotionMatchingPoseCompletionPrepared)
            {
                throw new InvalidOperationException(
                    "Pose Plan Motion Matching completion from the previous frame was not consumed.");
            }
            bool modulesOpen = false;
            bool poseConstraintsOpen = false;
            bool sourceOpen = false;
            bool publicationOpen = false;
            try
            {
                m_ProgramTuning.RequireCommitted(
                    lineage.TuningGeneration);
                constraintLease = m_PoseConstraints.BeginFrame(
                    in lineage,
                    diagnosticsInterest,
                    footIkCaptureInterest);
                poseConstraintsOpen = true;
                m_SourceRetirementState.BeginFrame();
                sourceLease = m_SourceModule.BeginFrame(in lineage);
                sourceOpen = true;
                publicationLease = m_FinalPublication.BeginFrame(
                    in lineage,
                    footIkCaptureInterest);
                publicationOpen = true;
                m_PendingCompletedFrame = default;
                m_HasPendingCompletedFrame = false;
                m_PendingFrameOutcome = AnimationPresentationFrameOutcome.None;
                m_ProgramFrames.BeginFrame();
                m_ActorDiagnosticsProjector.BeginFrame();
                PrepareLinkedPoseSelection(linkedPose);
                m_InertializationPlan.BeginFrame();
                for (int i = 0; i < m_StackRoutes.Length; i++)
                    m_StackRoutes[i].BeginFrame();
                for (int i = 0; i < m_RootOrientationWarps.Length; i++)
                    m_RootOrientationWarps[i].BeginFrame();
                BeginPendingModuleFrames();
                modulesOpen = true;
                ApplyLinkedPoseGenerationResets();
                m_HasOpenFrame = true;
                m_ActiveFrameLease = programLease;
                return m_ActiveFrameLease;
            }
            catch
            {
                if (publicationOpen)
                    m_FinalPublication.DiscardPending(publicationLease);
                if (poseConstraintsOpen)
                    m_PoseConstraints.DiscardFrame(constraintLease);
                if (modulesOpen)
                    DiscardPendingModuleFrames();
                for (int i = m_StackRoutes.Length - 1; i >= 0; i--)
                {
                    if (m_StackRoutes[i].HasOpenFrame)
                        m_StackRoutes[i].DiscardFrame();
                }
                for (int i = m_RootOrientationWarps.Length - 1; i >= 0; i--)
                {
                    if (m_RootOrientationWarps[i].HasOpenFrame)
                        m_RootOrientationWarps[i].DiscardFrame();
                }
                if (m_InertializationPlan.HasOpenFrame)
                    m_InertializationPlan.DiscardFrame();
                if (m_ProgramFrames.HasOpenFrame)
                    m_ProgramFrames.DiscardFrame();
                if (sourceOpen)
                    m_SourceModule.DiscardFrame(sourceLease);
                m_SourceRetirementState.CompleteFrame();
                ClearLinkedPoseFrameSelection();
                throw;
            }
        }

        internal void SealFrame(
            CharacterPoseProgramFrameLease lease,
            CharacterPoseSourceFrameLease sourceLease,
            CharacterPoseConstraintFrameLease constraintLease,
            CharacterFinalPosePublicationFrameLease publicationLease)
        {
            RequireMutation(lease);
            m_SourceModule.RequirePendingReady(sourceLease);
            m_FinalPublication.ValidatePendingSeal(publicationLease);
            if (!m_CommitValidated)
            {
                throw new InvalidOperationException(
                    "Pose Plan frame mutation was not validated for commit.");
            }
            if (m_PendingFrameOutcome != AnimationPresentationFrameOutcome.Committed ||
                !m_HasPendingCompletedFrame ||
                m_PendingCompletedFrame.CompletionIdentity == 0)
            {
                throw new InvalidOperationException(
                    "Pose Plan frame has no completed Native page to commit.");
            }
            m_ProgramFrames.CommitEvaluationFrame(
                m_PendingCompletedFrame.CompletionIdentity);
            m_InertializationPlan.CommitFrame();
            m_ProgramFrames.CommitFrame();
            m_SourceModule.CommitFrame(sourceLease);
            for (int i = 0; i < m_StackRoutes.Length; i++)
                m_StackRoutes[i].CommitFrame();
            for (int i = 0; i < m_RootOrientationWarps.Length; i++)
                m_RootOrientationWarps[i].CommitFrame();
            for (int i = 0; i < m_Stacks.Length; i++)
                m_Stacks[i].CommitFrame();
            for (int i = 0; i < m_DirectPlayers.Length; i++)
                m_DirectPlayers[i].CommitFrame();
            m_PoseStateSources.CommitFrame();
            m_PoseConstraints.SealFrame(constraintLease);
            m_LastCompletedFrame = m_PendingCompletedFrame;
            m_HasCompletedFrame = true;
            m_PendingCompletedFrame = default;
            m_HasPendingCompletedFrame = false;
            m_PreparedPage.Clear();
            m_HasOpenFrame = false;
            m_ActiveFrameLease = default;
            m_SourceRetirementState.CompleteFrame();
            ClearLinkedPoseFrameSelection();
        }

        internal void ValidatePendingSeal(
            CharacterPoseProgramFrameLease lease,
            CharacterPoseSourceFrameLease sourceLease)
        {
            RequireMutation(lease);
            m_SourceModule.ValidatePhysicalFrame();
            int standaloneReleaseCount = 0;
            for (int i = 0; i < m_DirectPlayers.Length; i++)
            {
                AnimationSelectedPosePlayerRuntime player =
                    m_DirectPlayers[i];
                int releaseCount = player.PendingReleaseCount;
                standaloneReleaseCount = checked(
                    standaloneReleaseCount +
                    releaseCount);
                for (int releaseIndex = 0;
                     releaseIndex < releaseCount;
                     releaseIndex++)
                {
                    AnimationPlayerReleaseToken playerRelease =
                        player.PrepareRelease(releaseIndex);
                    AnimationPoseSourceId sourceId =
                        playerRelease.SourceId;
                    CharacterPoseSourceRetirementHandle sourceRelease =
                        PrepareSourceRetirement(
                            sourceId,
                            player.NodeId,
                            default);
                    m_SourceRetirementState.PrepareDirect(
                        i,
                        in sourceRelease,
                        in playerRelease);
                }
            }
            for (int i = 0;
                 i < m_PoseStateSources.ClipPlayers.Length;
                 i++)
            {
                AnimationClipPlayerRuntime player =
                    m_PoseStateSources.ClipPlayers[i];
                int releaseCount = player.PendingReleaseCount;
                standaloneReleaseCount = checked(
                    standaloneReleaseCount +
                    releaseCount);
                for (int releaseIndex = 0;
                     releaseIndex < releaseCount;
                     releaseIndex++)
                {
                    AnimationPlayerReleaseToken playerRelease =
                        player.PrepareRelease(releaseIndex);
                    AnimationPoseSourceId sourceId =
                        playerRelease.SourceId;
                    CharacterPoseSourceRetirementHandle sourceRelease =
                        PrepareSourceRetirement(
                            sourceId,
                            player.NodeId,
                            default);
                    m_SourceRetirementState.PrepareClip(
                        i,
                        in sourceRelease,
                        in playerRelease);
                }
            }
            for (int i = 0;
                 i < m_PoseStateSources.BlendSpacePlayers.Length;
                 i++)
            {
                AnimationBlendSpacePlayerRuntime player =
                    m_PoseStateSources.BlendSpacePlayers[i];
                int releaseCount = player.PendingReleaseCount;
                standaloneReleaseCount = checked(
                    standaloneReleaseCount +
                    releaseCount);
                for (int releaseIndex = 0;
                     releaseIndex < releaseCount;
                     releaseIndex++)
                {
                    AnimationPlayerReleaseToken playerRelease =
                        player.PrepareRelease(releaseIndex);
                    AnimationPoseSourceId sourceId =
                        playerRelease.SourceId;
                    CharacterPoseSourceRetirementHandle sourceRelease =
                        PrepareSourceRetirement(
                            sourceId,
                            player.NodeId,
                            default);
                    m_SourceRetirementState.PrepareBlendSpace(
                        i,
                        in sourceRelease,
                        in playerRelease);
                }
            }
            int preparedActionReleaseCount =
                m_SourceRetirementState.PreparePendingRetirements(
                    m_SourceModule);
            m_SourceModule.RequireReleaseDiagnosticsCapacity(
                checked(
                    standaloneReleaseCount +
                    m_SourceRetirementState.PendingPoseCount +
                    preparedActionReleaseCount));
            m_SourceModule
                .RequireActionBackendReleaseCompletionCapacity(
                    checked(preparedActionReleaseCount * 2));
            m_SourceModule.ValidateFrame(sourceLease);
            m_CommitValidated = true;
        }

        internal void DiscardPendingFrame(
            CharacterPoseProgramFrameLease lease,
            CharacterPoseSourceFrameLease sourceLease,
            CharacterPoseConstraintFrameLease constraintLease,
            CharacterFinalPosePublicationFrameLease publicationLease)
        {
            RequireMutation(lease);
            m_SourceModule.RequirePendingOpen(sourceLease);
            Exception failure = null;
            DiscardStep(
                () => m_PoseConstraints.DiscardFrame(constraintLease),
                ref failure);
            DiscardStep(
                () => m_SourceModule.DiscardFrame(sourceLease),
                ref failure);
            for (int i = m_StackRoutes.Length - 1; i >= 0; i--)
            {
                CharacterAnimationTransitionRouteRuntime route = m_StackRoutes[i];
                if (route.HasOpenFrame)
                    DiscardStep(route.DiscardFrame, ref failure);
            }
            DiscardStep(
                DiscardPendingModuleFrames,
                ref failure);
            for (int i = m_RootOrientationWarps.Length - 1; i >= 0; i--)
            {
                RootOrientationWarpRuntime warp = m_RootOrientationWarps[i];
                if (warp.HasOpenFrame)
                    DiscardStep(warp.DiscardFrame, ref failure);
            }
            if (m_ProgramFrames.HasPendingEvaluationFrame)
            {
                ulong pendingCompletionIdentity =
                    m_ProgramFrames.PendingEvaluationCompletionIdentity;
                DiscardStep(
                    () => m_ProgramFrames.DiscardEvaluationFrame(
                        pendingCompletionIdentity),
                    ref failure);
            }
            if (m_InertializationPlan.HasOpenFrame)
            {
                DiscardStep(
                    m_InertializationPlan.DiscardFrame,
                    ref failure);
            }
            if (m_ProgramFrames.HasOpenFrame)
            {
                DiscardStep(
                    m_ProgramFrames.DiscardFrame,
                    ref failure);
            }
            DiscardStep(
                () => m_FinalPublication.DiscardPending(publicationLease),
                ref failure);
            DiscardStep(
                m_DiagnosticsPublisher
                    .DiscardPendingFrame,
                ref failure);
            DiscardStep(
                m_FootIkDiagnosticsProjector.DiscardPendingFrame,
                ref failure);
            DiscardStep(
                DiscardPendingReleasePreparation,
                ref failure);
            m_SourceRetirementState.ClearStandalone();
            ClearValidatedActionBackendAcknowledgements();
            ClearPreparedMotionMatchingPoseCompletion();
            m_PreparedPage.Clear();
            m_ProgramFrames.ClearSourceDemand();
            m_HasOpenFrame = false;
            m_ActiveFrameLease = default;
            m_CommitValidated = false;
            m_PendingCompletedFrame = default;
            m_HasPendingCompletedFrame = false;
            m_PendingFrameOutcome = AnimationPresentationFrameOutcome.None;
            m_SourceModule.CancelReleaseDiagnostics();
            ClearLinkedPoseFrameSelection();
            if (failure != null)
            {
                throw new AggregateException(
                    "Pose Plan Pending discard failed.",
                    failure);
            }
        }

        internal ComposedAnimationPoseFrame
            FinalizeCommittedFrame(
                CharacterFinalPosePublicationFrameLease publicationLease)
        {
            RequireAlive();
            if (m_ActiveFrameLease.IsValid)
            {
                throw new InvalidOperationException(
                    "Pose Plan frame mutation must commit before physical releases.");
            }
            if (!m_CommitValidated)
            {
                throw new InvalidOperationException(
                    "Pose Plan committed frame was not validated.");
            }
            m_SourceRetirementState.ApplyStandalone(
                m_SourceModule,
                m_DirectPlayers,
                m_PoseStateSources.ClipPlayers,
                m_PoseStateSources.BlendSpacePlayers,
                m_CompletionIdentity);
            m_SourceRetirementState.ApplyPendingPose(m_SourceModule);
            ComposedAnimationPoseFrame result =
                m_FinalPublication.CommitPending(publicationLease);
            m_CommitValidated = false;
            m_PendingFrameOutcome = AnimationPresentationFrameOutcome.None;
            return result;
        }

        void ClearValidatedActionBackendAcknowledgements()
        {
            m_SourceModule.ClearValidatedReleaseAcknowledgements();
        }

        void ClearPreparedMotionMatchingPoseCompletion()
        {
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

        internal void SetPoseWatchInterests(Guid ownerId, IReadOnlyList<AnimationPoseWatchIdentity> interests) =>
            m_DiagnosticsPublisher.SetPoseWatchInterests(ownerId, interests);

        internal void RemovePoseWatchInterests(Guid ownerId) =>
            m_DiagnosticsPublisher.RemovePoseWatchInterests(ownerId);

        internal void SetDiagnosticsInterest(
            Guid ownerId,
            AnimationPresentationDiagnosticsInterest interest) =>
            m_DiagnosticsPublisher.SetDiagnosticsInterest(ownerId, interest);

        internal void RemoveDiagnosticsInterest(Guid ownerId) =>
            m_DiagnosticsPublisher.RemoveDiagnosticsInterest(ownerId);

        internal AnimationPresentationDiagnosticsInterest ResolveDiagnosticsInterest(
            AnimationPresentationDiagnosticsInterest transientInterest) =>
            m_DiagnosticsPublisher.ResolveFrameInterest(transientInterest);

        internal void InvalidateDiagnosticsSnapshot() =>
            InvalidateDiagnostics();

        void InvalidateDiagnostics()
        {
            m_DiagnosticsPublisher.Invalidate();
            m_FootIkDiagnosticsProjector.Invalidate();
        }

        internal MotionMatchingPoseStateDemandBatch BuildMotionMatchingDemandBatch(
            ulong presentationFrame,
            ulong resetSequence,
            PresentationFrameWorkspace workspace,
            PresentationFrameWorkspaceLease lease)
        {
            RequireAlive();
            RequireOpenMutation();
            return m_PoseStateSources
                .BuildMotionMatchingDemandBatch(
                    presentationFrame,
                    resetSequence,
                    workspace,
                    lease);
        }

        internal void ApplyMotionMatchingSelections(
            in MotionMatchingFrameResolution resolution,
            IDictionary<AnimationPlayerSourceSampleKey,
                PresentationPoseSourceSample> sourceSamples,
            PresentationFrameWorkspace workspace,
            PresentationFrameWorkspaceLease lease)
        {
            RequireAlive();
            RequireOpenMutation();
            m_PoseStateSources.ApplyMotionMatchingSelections(
                in resolution,
                sourceSamples,
                workspace,
                lease,
                this);
        }

        internal void PrepareMotionMatchingPosePlanCompletion(
            in MotionMatchingFrameResolution resolution,
            ulong poseCompletionIdentity)
        {
            RequireAlive();
            RequireOpenMutation();
            if (m_MotionMatchingPoseCompletionPrepared ||
                resolution.PresentationFrame == 0 ||
                resolution.CompletionIdentity == 0 ||
                poseCompletionIdentity == 0 ||
                poseCompletionIdentity != m_FrameCompletionContext)
            {
                throw new InvalidOperationException(
                    "Motion Matching Pose Plan completion preparation is invalid.");
            }
            m_SourceModule.BeginUsage(poseCompletionIdentity);
            m_MotionMatchingHistoryCompletionCount = 0;
            m_PreparedMotionMatchingHistoryReadCount = 0;
            for (int stackIndex = 0; stackIndex < m_Stacks.Length; stackIndex++)
            {
                AnimationBlendStackRuntime stack = m_Stacks[stackIndex];
                for (int entryIndex = 0; entryIndex < stack.EntryCount; entryIndex++)
                {
                    AnimationBlendEntryId entry = stack.GetEntryId(entryIndex);
                    if (!entry.SourcePoseTarget && entry.SourceId.SourceKind == AnimationPoseSourceKind.MotionMatching)
                        AddMotionMatchingSourceUsage(
                            stack.PoseNodeId,
                            entry.SourceId,
                            poseCompletionIdentity);
                }
            }
            for (int playerIndex = 0; playerIndex < m_DirectPlayers.Length; playerIndex++)
            {
                AnimationSelectedPosePlayerRuntime player = m_DirectPlayers[playerIndex];
                if (player.HasSelection && player.SourceId.SourceKind == AnimationPoseSourceKind.MotionMatching)
                    AddMotionMatchingSourceUsage(
                        player.NodeId,
                        player.SourceId,
                        poseCompletionIdentity);
            }
            for (int selectionIndex = 0; selectionIndex < resolution.SelectionCount; selectionIndex++)
            {
                MotionMatchingSelectionBatchItem selection = resolution.GetSelection(selectionIndex);
                if (!selection.RequiresHistory)
                    continue;
                if (m_PreparedMotionMatchingHistoryReadCount >=
                    m_PreparedMotionMatchingHistoryReads.Length ||
                    !m_NodeRuntimeIndex.TryGetPlayerIndex(
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
                        (uint)m_Projection.Rig.PoseBoneCount)
                    {
                        throw new InvalidOperationException(
                            "Motion Matching history Bone index is outside the compiled Rig.");
                    }
                }
                bool sourceUsed = PlayerUsesSource(
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
            BuildMotionMatchingPosePlanCompletion()
        {
            RequireAlive();
            RequireOpenMutation();
            if (!m_MotionMatchingPoseCompletionPrepared ||
                !m_HasPendingCompletedFrame ||
                m_PendingFrameOutcome !=
                    AnimationPresentationFrameOutcome.Committed ||
                m_PendingCompletedFrame.CompletionIdentity !=
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
                    TryCopyCompletedPlayerPose(
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

        internal void BeginCommittedDiagnostics(
            AnimationPresentationDiagnosticsInterest interest,
            in CharacterFootIkCaptureBinding footIkCapture,
            CharacterLinkedPoseRuntimeSession linkedPose,
            in CharacterPoseSourceFrameResult sourceFrame,
            in CharacterPoseFrameExecutionResult executionResult)
        {
            RequireAlive();
            RequireNoOpenMutation();
            if (linkedPose == null)
                throw new ArgumentNullException(nameof(linkedPose));
            bool publishRuntimeSnapshot =
                interest != AnimationPresentationDiagnosticsInterest.None;
            bool captureFootIk = footIkCapture.IsValid;
            if (!publishRuntimeSnapshot && !captureFootIk)
                return;
            if (captureFootIk &&
                !m_PoseConstraints.HasFootPlacement)
            {
                throw new InvalidOperationException(
                    "Foot IK capture requires the compiled Foot Placement capability.");
            }
            if (!sourceFrame.IsReady ||
                sourceFrame.Lineage != executionResult.Lineage ||
                !executionResult.IsPublished ||
                !m_HasCompletedFrame ||
                m_LastCompletedFrame.CompletionIdentity == 0 ||
                executionResult.Lineage.CompletionIdentity !=
                m_LastCompletedFrame.CompletionIdentity ||
                !m_ProgramFrames.TryGetCommittedFinalReadBinding(
                    out AnimationFinalPoseNativeReadBinding finalRead) ||
                finalRead.CompletionIdentity != m_LastCompletedFrame.CompletionIdentity)
            {
                throw new InvalidOperationException(
                    "Animation diagnostics requires a successfully sealed committed Pose page.");
            }
            using (DiagnosticsMarker.Auto())
            {
                CharacterPoseProgramResult committedProgramResult =
                    executionResult.Program;
                CharacterPoseConstraintResult committedConstraintResult =
                    executionResult.Constraint;
                CharacterFinalPosePublicationResult committedPublicationResult =
                    executionResult.Publication;
                CharacterPoseFrameLineage committedFrame =
                    executionResult.Lineage;
                if (executionResult.Constraint.Lineage !=
                        executionResult.Lineage ||
                    executionResult.Publication.Lineage !=
                        executionResult.Lineage)
                {
                    throw new InvalidOperationException(
                        "Animation diagnostics committed lineage is inconsistent.");
                }
                if (captureFootIk)
                {
                    CaptureCommittedFootIk(
                        in footIkCapture,
                        in committedFrame,
                        in committedConstraintResult,
                        in committedPublicationResult);
                }
                if (!publishRuntimeSnapshot)
                    return;
                bool requiresFoot = m_PoseConstraints.HasFootPlacement &&
                    CharacterPoseConstraintRuntime
                        .RequiresFootDiagnostics(interest);
                bool requiresSolver = CharacterPoseConstraintRuntime
                    .RequiresFullBodyIkDiagnostics(interest);
                bool requiresPhysical = CharacterFinalPosePublication
                    .RequiresPhysicalDiagnostics(interest);
                CharacterPoseActorCommittedDiagnosticsView actorDiagnostics =
                    m_ActorDiagnosticsProjector.Capture(
                        in committedProgramResult,
                        m_Stacks,
                        m_StackRoutes,
                        m_PoseStateSources.StateMachines,
                        m_InertializationPlan,
                        m_PoseStateSources.ClipPlayers,
                        m_PoseStateSources.BlendSpacePlayers,
                        m_RootOrientationWarps,
                        interest,
                        false);
                CharacterPoseConstraintCommittedDiagnosticsView
                    constraintDiagnostics =
                        m_PoseConstraints.CaptureCommittedDiagnostics(
                            in committedConstraintResult,
                            interest,
                            default);
                CharacterFinalPoseCommittedDiagnosticsView
                    publicationDiagnostics =
                        m_FinalPublication.CaptureCommittedDiagnostics(
                            in committedPublicationResult);
                AnimationPhysicalBoneWriteDiagnostics physicalWrite =
                    publicationDiagnostics.PhysicalWrite;
                CharacterFootLandingPredictionDiagnostics footDiagnostics =
                    constraintDiagnostics.FootLandingPrediction;
                CharacterFullBodyIkSolverDiagnostics solverDiagnostics =
                    constraintDiagnostics.Solver;
                if (!actorDiagnostics.IsValid ||
                    actorDiagnostics.Result.Lineage !=
                    executionResult.Lineage ||
                    !constraintDiagnostics.IsValid ||
                    constraintDiagnostics.Result.Lineage !=
                    executionResult.Lineage ||
                    !publicationDiagnostics.IsValid ||
                    publicationDiagnostics.Result.Lineage !=
                    executionResult.Lineage ||
                    requiresFoot &&
                    (!footDiagnostics.IsCompleted ||
                     footDiagnostics.FrameSequence !=
                     executionResult.Lineage.PresentationFrame ||
                     footDiagnostics.CompletionIdentity !=
                     m_LastCompletedFrame.CompletionIdentity) ||
                    requiresSolver &&
                    solverDiagnostics.OutputCompletionIdentity !=
                    m_LastCompletedFrame.CompletionIdentity ||
                    requiresPhysical &&
                    (!physicalWrite.IsAvailable ||
                     physicalWrite.CompletionIdentity !=
                     m_LastCompletedFrame.CompletionIdentity))
                {
                    throw new InvalidOperationException(
                        "Animation diagnostics committed lineage is inconsistent.");
                }
                bool includeFootBasicState =
                    (interest &
                     (AnimationPresentationDiagnosticsInterest.LiveState |
                      AnimationPresentationDiagnosticsInterest.Capture)) != 0;
                m_FootIkDiagnosticsProjector.BeginFrame(
                    in executionResult,
                    in actorDiagnostics,
                    in constraintDiagnostics,
                    in publicationDiagnostics,
                    includeFootBasicState);
                try
                {
                    CharacterPoseSourceCommittedDiagnosticsView
                        sourceDiagnostics =
                            m_SourceModule.CaptureCommittedDiagnostics(
                                in sourceFrame);
                    CharacterPoseProgramCommittedDiagnosticsView
                        programDiagnostics =
                            m_ProgramDiagnosticsProjector.Capture(
                                m_ProgramFrames,
                                in committedProgramResult,
                                in m_LastCompletedFrame,
                                interest);
                    CharacterLinkedPoseCommittedDiagnosticsView
                        linkedPoseDiagnostics =
                            linkedPose.CaptureCommittedDiagnostics(
                                in committedProgramResult);
                    if (!sourceDiagnostics.IsValid ||
                        sourceDiagnostics.Result.Lineage !=
                        executionResult.Lineage ||
                        !programDiagnostics.IsValid ||
                        programDiagnostics.Result.Lineage !=
                        executionResult.Lineage ||
                        !linkedPoseDiagnostics.IsValid ||
                        linkedPoseDiagnostics.Result.Lineage !=
                        executionResult.Lineage)
                    {
                        throw new InvalidOperationException(
                            "Animation runtime Snapshot lineage is inconsistent.");
                    }
                    m_DiagnosticsPublisher.BeginFrame(
                        in executionResult,
                        in sourceDiagnostics,
                        in programDiagnostics,
                        in linkedPoseDiagnostics,
                        in actorDiagnostics,
                        in constraintDiagnostics,
                        in publicationDiagnostics,
                        interest);
                }
                catch
                {
                    m_FootIkDiagnosticsProjector
                        .DiscardPendingFrame();
                    throw;
                }
            }
        }

        void CaptureCommittedFootIk(
            in CharacterFootIkCaptureBinding binding,
            in CharacterPoseFrameLineage frame,
            in CharacterPoseConstraintResult constraintResult,
            in CharacterFinalPosePublicationResult publicationResult)
        {
            m_PoseConstraints.RequireCommittedFootIkCapture(
                in constraintResult);
            ComposedAnimationPoseFrame finalFrame =
                m_FinalPublication.RequireCommittedFrame(
                    in publicationResult);
            CharacterFootLandingPredictionDiagnostics landing =
                m_PoseConstraints.CommittedFootLandingPrediction;
            if (!landing.IsCompleted ||
                landing.FrameSequence != frame.PresentationFrame ||
                landing.CompletionIdentity != frame.CompletionIdentity)
            {
                throw new InvalidOperationException(
                    "Committed Foot IK landing facts are inconsistent.");
            }
            CharacterFullBodyIkSolverDiagnostics solver = default;
            CharacterFullBodyIkEffectorDiagnostics pelvis = default;
            CharacterFullBodyIkEffectorDiagnostics leftEffector = default;
            CharacterFullBodyIkEffectorDiagnostics rightEffector = default;
            CharacterFullBodyIkLimbDiagnostics leftLeg = default;
            CharacterFullBodyIkLimbDiagnostics rightLeg = default;
            CharacterFullBodyIkSolverDiagnostics candidate =
                m_PoseConstraints.CommittedFullBodyIkSolver;
            if (candidate.IsCompleted &&
                candidate.InputCompletionIdentity == frame.CompletionIdentity &&
                candidate.FrameSequence == landing.FrameSequence)
            {
                bool containsFoot = false;
                for (int i = 0;
                     i < m_PoseConstraints.CommittedSolverEffectorCount;
                     i++)
                {
                    CharacterFullBodyIkEffectorDiagnostics effector =
                        m_PoseConstraints.GetCommittedSolverEffector(i);
                    if (effector.Slot ==
                        CharacterFullBodyIkEffectorSlot.PelvisPreSolveTranslation)
                    {
                        pelvis = effector;
                        containsFoot = true;
                    }
                    else if (effector.Slot ==
                             CharacterFullBodyIkEffectorSlot.LeftFoot)
                    {
                        leftEffector = effector;
                        containsFoot = true;
                    }
                    else if (effector.Slot ==
                             CharacterFullBodyIkEffectorSlot.RightFoot)
                    {
                        rightEffector = effector;
                        containsFoot = true;
                    }
                }
                if (containsFoot)
                {
                    for (int i = 0;
                         i < m_PoseConstraints.CommittedSolverLimbCount;
                         i++)
                    {
                        CharacterFullBodyIkLimbDiagnostics limb =
                            m_PoseConstraints.GetCommittedSolverLimb(i);
                        if (limb.Limb == CharacterFullBodyIkLimbSlot.LeftLeg)
                            leftLeg = limb;
                        else if (limb.Limb ==
                                 CharacterFullBodyIkLimbSlot.RightLeg)
                            rightLeg = limb;
                    }
                    solver = candidate;
                }
            }
            CharacterFootLandingPredictionFootDiagnostics leftFoot =
                landing.Left;
            CharacterFootLandingPredictionFootDiagnostics rightFoot =
                landing.Right;
            CharacterFootLandingPredictionInputDiagnostics input =
                landing.Input;
            CharacterFootStepObservationInputDiagnostics formalInput =
                input.FootStepObservation;
            AnimationFootMotionRuntimeSample leftFormalInput =
                formalInput.Left;
            AnimationFootMotionRuntimeSample rightFormalInput =
                formalInput.Right;
            ResolveCommittedFootMotion(
                in finalFrame,
                out AnimationFootMotionRuntimeSample leftFormalOutput,
                out AnimationFootMotionRuntimeSample rightFormalOutput);
            CharacterFullBodyIkGoal pelvisGoal = landing.PelvisGoal;
            CharacterFootPrimarySupportDiagnostics primarySupport =
                landing.PrimarySupport;
            CharacterFootStrideHipsDiagnostics stride = landing.StrideHips;
            try
            {
                binding.Consumer.TryCapture(
                    in frame,
                    in leftEffector,
                    in leftFoot,
                    in leftFormalInput,
                    in leftFormalOutput,
                    in leftLeg,
                    in rightEffector,
                    in rightFoot,
                    in rightFormalInput,
                    in rightFormalOutput,
                    in rightLeg,
                    in input,
                    in pelvis,
                    in pelvisGoal,
                    in primarySupport,
                    in solver,
                    in stride);
            }
            catch (Exception failure)
            {
                try
                {
                    binding.Consumer.CaptureFault(failure);
                }
                catch
                {
                }
            }
        }

        void ResolveCommittedFootMotion(
            in ComposedAnimationPoseFrame finalFrame,
            out AnimationFootMotionRuntimeSample left,
            out AnimationFootMotionRuntimeSample right)
        {
            AnimationPoseSourceId sourceId = default;
            float sourceWeight = -1f;
            AnimationReadOnlyBuffer<AnimationPoseSourceContribution>
                contributions = finalFrame.Contributions;
            for (int i = 0; i < contributions.Count; i++)
            {
                AnimationPoseSourceContribution contribution =
                    contributions[i];
                if (contribution.Kind !=
                        AnimationPoseContributionKind.Live ||
                    contribution.Weight <= sourceWeight)
                {
                    continue;
                }
                sourceId = contribution.SourceId;
                sourceWeight = contribution.Weight;
            }
            if (!sourceId.IsValid)
            {
                left = default;
                right = default;
                return;
            }
            for (int i = 0;
                 i < m_PoseStateSources.ClipPlayers.Length;
                 i++)
            {
                AnimationClipPlayerRuntime player =
                    m_PoseStateSources.ClipPlayers[i];
                if (player.SourceId.Equals(sourceId))
                {
                    player.CreateFootMotionSamples(
                        sourceWeight,
                        out left,
                        out right);
                    return;
                }
            }
            left = default;
            right = default;
        }

        internal CharacterFootIkCommittedCaptureViewLease PublishDiagnostics()
        {
            bool publishRuntimeSnapshot =
                m_DiagnosticsPublisher.HasPendingFrame;
            bool publishFootIkView =
                m_FootIkDiagnosticsProjector.HasPendingFrame;
            if (!publishRuntimeSnapshot && !publishFootIkView)
                return default;
            if (publishRuntimeSnapshot && !publishFootIkView)
            {
                throw new InvalidOperationException(
                    "Foot IK committed capture view is not pending.");
            }
            CharacterFootIkCommittedCaptureViewLease footIkCaptureView;
            using (DiagnosticsMarker.Auto())
            {
                footIkCaptureView =
                    m_FootIkDiagnosticsProjector.Publish();
                if (publishRuntimeSnapshot)
                {
                    try
                    {
                        AnimationPresentationRuntimeSnapshot snapshot =
                            m_DiagnosticsPublisher.Publish(
                                footIkCaptureView);
                        if (!snapshot.FootIkCommittedCaptureView.Lineage.Equals(
                                footIkCaptureView.Lineage))
                        {
                            throw new InvalidOperationException(
                                "Animation diagnostics Foot IK view was not preserved.");
                        }
                    }
                    catch
                    {
                        m_FootIkDiagnosticsProjector.Invalidate();
                        throw;
                    }
                }
            }
            return footIkCaptureView;
        }

        internal void CopyActionSlotReleaseCompletions(
            List<AnimationSlotSourceReleaseCompletion> destination)
        {
            RequireAlive();
            m_SourceModule.CopyActionSlotReleaseCompletions(
                destination);
        }

        internal bool HasPendingActionBackendSources(
            AnimationPlaybackId playbackId)
        {
            RequireAlive();
            return m_SourceRetirementState.HasPendingAction(
                playbackId);
        }

        internal bool TryPrepareActionBackendReleaseRequest(
            AnimationPlaybackId playbackId,
            out ActionBackendReleaseRequest request)
        {
            RequireAlive();
            return m_SourceRetirementState.TryPrepareActionRequest(
                playbackId,
                out request);
        }

        internal void CopyActionBackendReleaseCompletions(
            List<ActionBackendReleaseCompletion> destination)
        {
            RequireAlive();
            m_SourceModule.CopyActionBackendReleaseCompletions(
                destination);
        }

        internal void ApplyValidatedActionBackendReleaseCompletionAcknowledgements()
        {
            RequireAlive();
            m_SourceModule.ApplyActionBackendReleaseAcknowledgements();
        }

        internal void
            ValidateActionBackendReleaseCompletionAcknowledgements(
                IReadOnlyList<ActionBackendReleaseCompletion>
                    completions)
        {
            RequireAlive();
            m_SourceModule
                .ValidateActionBackendReleaseAcknowledgements(
                    completions);
        }

        internal void ExecutePreparedActionBackendReleaseRequests()
        {
            RequireAlive();
            m_SourceRetirementState.ExecutePreparedActions(
                m_SourceModule);
        }

        internal AnimationPoseSourceId PublishActionFrame(
            in ActionAnimationPlaybackFrame frame,
            in ResolvedActionAnimationBinding binding,
            AnimationPoseSelectionGeneration selectionGeneration,
            ulong presentationRequestSequence,
            IDictionary<AnimationPlayerSourceSampleKey,
                AnimationResolvedPoseSourceSample> sourceSamples)
        {
            return PublishActionSourceFrame(
                in frame,
                in binding,
                selectionGeneration,
                presentationRequestSequence,
                sourceSamples,
                true);
        }

        internal AnimationPoseSourceId PublishRetainedActionFrame(
            in ActionAnimationPlaybackFrame frame,
            in ResolvedActionAnimationBinding binding,
            AnimationPoseSelectionGeneration selectionGeneration,
            ulong presentationRequestSequence,
            IDictionary<AnimationPlayerSourceSampleKey,
                AnimationResolvedPoseSourceSample> sourceSamples)
        {
            return PublishActionSourceFrame(
                in frame,
                in binding,
                selectionGeneration,
                presentationRequestSequence,
                sourceSamples,
                false);
        }

        AnimationPoseSourceId PublishActionSourceFrame(
            in ActionAnimationPlaybackFrame frame,
            in ResolvedActionAnimationBinding binding,
            AnimationPoseSelectionGeneration selectionGeneration,
            ulong presentationRequestSequence,
            IDictionary<AnimationPlayerSourceSampleKey,
                AnimationResolvedPoseSourceSample> sourceSamples,
            bool select)
        {
            RequireAlive();
            RequireOpenMutation();
            if (!frame.IsValid ||
                !binding.IsValid ||
                !frame.PlaybackId.ProducerId.Equals(binding.ProducerId) ||
                frame.AnimationChannelId != binding.AnimationChannelId ||
                !string.Equals(
                    frame.ProgramProducerId,
                    binding.ProgramProducerId,
                    StringComparison.Ordinal) ||
                !selectionGeneration.IsValid ||
                presentationRequestSequence == 0 ||
                sourceSamples == null ||
                !m_NodeRuntimeIndex.TryGetStackRoute(
                    binding.SlotNodeId,
                    out AnimationBlendStackRuntime stack,
                    out CharacterAnimationTransitionRouteRuntime route) ||
                !route.IsAnimationSlot)
            {
                throw new InvalidOperationException(
                    "Action frame has no exact compiled Animation Slot route.");
            }

            var sourceId = new AnimationPoseSourceId(
                frame.PlaybackId,
                AnimationPoseSourceKind.Timeline,
                selectionGeneration,
                frame.ActionInstanceId);
            PresentationPoseSampleTime sampleTime =
                frame.ProjectedSampleTime;
            var request = new AnimationPoseSampleRequest(
                sourceId,
                frame.SourcePoseContinuityIdentity,
                presentationRequestSequence,
                binding.ProgramProducerIndex,
                sampleTime.SampleTime,
                sampleTime.ContinuousTime,
                sampleTime.Cycle,
                sampleTime.Loop,
                sampleTime.TimeScale,
                frame.Clips,
                frame.ParameterPageId,
                frame.PoseParameters,
                frame.PoseParameterAvailability);
            if (select)
                route.PushSelection(stack, in request);
            var key = new AnimationPlayerSourceSampleKey(
                binding.SlotNodeId,
                sourceId);
            if (sourceSamples.ContainsKey(key))
            {
                throw new InvalidOperationException(
                    $"Animation Slot '{binding.SlotId}' received a duplicate Action source.");
            }
            sourceSamples.Add(
                key,
                new AnimationResolvedPoseSourceSample(
                    request,
                    in frame.LeftFootFeatures,
                    in frame.RightFootFeatures,
                    true));
            return sourceId;
        }

        internal void PublishActionSourcePose(
            AnimationSlotId slotId,
            PoseNodeId slotNodeId,
            ulong presentationRequestSequence)
        {
            RequireAlive();
            RequireOpenMutation();
            if (!slotId.IsValid ||
                !slotNodeId.IsValid ||
                presentationRequestSequence == 0 ||
                !m_NodeRuntimeIndex.TryGetStackRoute(
                    slotNodeId,
                    out AnimationBlendStackRuntime stack,
                    out CharacterAnimationTransitionRouteRuntime route) ||
                !route.IsAnimationSlot ||
                route.SlotId != slotId)
            {
                throw new InvalidOperationException(
                    "Source Pose target has no exact compiled Animation Slot route.");
            }
            route.PushSourcePose(
                stack,
                presentationRequestSequence);
        }

        internal void Advance(
            float presentationDeltaSeconds,
            in CharacterPresentationFactFrame factFrame,
            in CharacterPresentationProgramParameterFrame parameterFrame)
        {
            RequireAlive();
            RequireOpenMutation();
            if (!float.IsFinite(presentationDeltaSeconds) || presentationDeltaSeconds < 0f ||
                !factFrame.IsValid || !parameterFrame.IsValid)
                throw new ArgumentOutOfRangeException(nameof(presentationDeltaSeconds));
            if (m_HasSequencePreview)
            {
                for (int i = 0; i < m_PoseStateSources.ClipPlayers.Length; i++)
                {
                    AnimationClipPlayerRuntime player =
                        m_PoseStateSources.ClipPlayers[i];
                    bool selected = i == m_SequencePreviewPlayerIndex;
                    player.SetRelevant(selected);
                    if (selected)
                        player.SetPreviewTime(
                            m_SequencePreviewTime,
                            m_SequencePreviewReset);
                }
                for (int i = 0; i < m_PoseStateSources.BlendSpacePlayers.Length; i++)
                    m_PoseStateSources.BlendSpacePlayers[i].SetRelevant(false);
                return;
            }
            m_PoseStateSources.PrepareFrame(
                presentationDeltaSeconds,
                in factFrame);
            for (int i = 0; i < m_StackRoutes.Length; i++)
            {
                if (!IsPlayerActive(m_Stacks[i].PlayerIndex))
                    continue;
                CharacterAnimationTransitionRouteRuntime route = m_StackRoutes[i];
                route.FlushReleaseCompletion();
                if (!route.IsAnimationSlot)
                    continue;
                CharacterAnimationSlotNativeControl control = route.NativeControl;
                m_ProgramFrames.SetAnimationSlotControl(
                    route.AnimationSlotIndex,
                    in control);
            }
            for (int i = 0; i < m_Stacks.Length; i++)
            {
                if (IsPlayerActive(m_Stacks[i].PlayerIndex))
                    m_Stacks[i].Advance(presentationDeltaSeconds);
            }
            m_PoseStateSources.AdvanceSources(
                presentationDeltaSeconds,
                in factFrame,
                in parameterFrame,
                m_SourceModule.RequireTuning(
                    m_ActiveFrameLease.Lineage.TuningGeneration));
        }

        internal void FinalizePoseStateFrame(
            in CharacterPresentationFactFrame factFrame,
            PresentationFrameWorkspace workspace,
            PresentationFrameWorkspaceLease lease)
        {
            RequireAlive();
            RequireOpenMutation();
            if (!factFrame.IsValid ||
                workspace == null ||
                !lease.IsValid)
                throw new ArgumentException(
                    "Pose State frame finalization is invalid.",
                    nameof(factFrame));
            if (m_HasSequencePreview)
                return;
            m_PoseStateSources.EvaluateTransitions(
                in factFrame,
                m_ProgramFrames,
                workspace,
                lease);
            for (int i = 0; i < m_RootOrientationWarps.Length; i++)
            {
                if (!IsFragmentActive(
                        m_RootOrientationWarpLinkedPoseFragmentIndices[i]))
                {
                    continue;
                }
                CharacterRootOrientationWarpNativeControl control =
                    m_RootOrientationWarps[i].Prepare(
                        in factFrame);
                m_ProgramFrames.SetRootOrientationWarpControl(
                    i,
                    in control);
            }
        }

        internal CharacterPoseSourceDemand CreateSourceDemand(
            CharacterPoseProgramFrameLease programLease,
            CharacterPoseSourceFrameLease sourceLease,
            IReadOnlyList<PoseSourceProviderDemand> providerDemands,
            int actionSourceCount,
            int providerSourceCount)
        {
            RequireAlive();
            RequireMutation(programLease);
            m_SourceModule.RequirePendingOpen(sourceLease);
            CharacterPoseFrameLineage openLineage = programLease.Lineage;
            if (!openLineage.IsOpenValid ||
                openLineage.CompletionIdentity != 0 ||
                !sourceLease.Matches(openLineage))
            {
                throw new ArgumentException(
                    "Pose Program source demand lineage is invalid.",
                    nameof(openLineage));
            }
            ulong completionIdentity = NextCompletionIdentity();
            m_FrameCompletionContext = completionIdentity;
            CharacterPoseFrameLineage lineage =
                openLineage.WithCompletion(completionIdentity);
            CharacterPoseSourcePreparationView preparations =
                m_ProgramFrames.BeginSourceDemand(
                    completionIdentity);
            var demand = new CharacterPoseSourceDemand(
                in lineage,
                in preparations,
                providerDemands,
                actionSourceCount,
                providerSourceCount);
            m_ProgramFrames.BindSourceDemand(in demand);
            m_SourceModule.BindDemand(sourceLease, in demand);
            return demand;
        }

        internal CharacterPoseProgramPrepared PrepareEvaluation(
            CharacterPoseSourceFrameLease sourceLease,
            in CharacterPoseSourceDemand sourceDemand,
            float presentationDeltaSeconds,
            IReadOnlyDictionary<AnimationPlayerSourceSampleKey,
                AnimationResolvedPoseSourceSample> actionSourceSamples,
            IReadOnlyDictionary<AnimationPlayerSourceSampleKey,
                PresentationPoseSourceSample> providerSourceSamples,
            bool recordDiagnostics)
        {
            RequireAlive();
            RequireOpenMutation();
            CharacterPoseSourceDemand pendingDemand =
                m_SourceModule.RequireDemand(sourceLease);
            CharacterPoseSourceDemand programDemand =
                m_ProgramFrames.RequireSourceDemand(
                    in sourceDemand);
            CharacterPoseSourcePreparationView sourcePreparations =
                sourceDemand.Preparations;
            CharacterPoseSourcePreparationView pendingPreparations =
                pendingDemand.Preparations;
            if (m_PreparedPage.HasValue)
            {
                throw new InvalidOperationException(
                    "Pose Program evaluation is already prepared for the active frame.");
            }
            if (!sourceDemand.IsValid ||
                sourceDemand.Lineage != pendingDemand.Lineage ||
                sourceDemand.Lineage != programDemand.Lineage ||
                !sourcePreparations.Matches(
                    in pendingPreparations) ||
                sourceDemand.ActionSourceCount !=
                    pendingDemand.ActionSourceCount ||
                sourceDemand.ProviderSourceCount !=
                    pendingDemand.ProviderSourceCount ||
                !ReferenceEquals(
                    sourceDemand.ProviderDemands,
                    pendingDemand.ProviderDemands) ||
                sourceDemand.Lineage.FrameIdentity !=
                    m_ActiveFrameLease.FrameIdentity ||
                sourceDemand.Lineage.CompletionIdentity !=
                    m_FrameCompletionContext)
            {
                throw new ArgumentException(
                    "Pose Program source demand is not active.",
                    nameof(sourceDemand));
            }
            if (!float.IsFinite(presentationDeltaSeconds) || presentationDeltaSeconds < 0f)
                throw new ArgumentOutOfRangeException(nameof(presentationDeltaSeconds));
            if (actionSourceSamples == null)
                throw new ArgumentNullException(
                    nameof(actionSourceSamples));
            if (providerSourceSamples == null)
                throw new ArgumentNullException(
                    nameof(providerSourceSamples));

            ulong completionIdentity =
                sourceDemand.Lineage.CompletionIdentity;
            CharacterPoseGraphNativeBinding frame;
            using (PrepareMarker.Auto())
            {
                m_SourceModule.BeginReleaseDiagnostics(
                    recordDiagnostics);
                m_SourceModule.ClearActionSlotReleaseCompletions();
                using (PrepareWorkspaceMarker.Auto())
                {
                    frame = m_ProgramFrames.BeginEvaluationFrame(
                        completionIdentity);
                    for (int i = 0; i < m_Stacks.Length; i++)
                        m_Stacks[i].BeginSourceFrame(completionIdentity);
                    for (int i = 0; i < m_DirectPlayers.Length; i++)
                        m_DirectPlayers[i].BeginFrame(completionIdentity);
                    for (int i = 0;
                         i < m_PoseStateSources.ClipPlayers.Length;
                         i++)
                        m_PoseStateSources.ClipPlayers[i]
                            .BeginFrame(completionIdentity);
                    for (int i = 0;
                         i < m_PoseStateSources.BlendSpacePlayers.Length;
                         i++)
                        m_PoseStateSources.BlendSpacePlayers[i]
                            .BeginFrame(completionIdentity);
                }
                using (PrepareStackMarker.Auto())
                {
                    for (int stackIndex = 0; stackIndex < m_Stacks.Length; stackIndex++)
                    {
                        if (IsPlayerActive(m_Stacks[stackIndex].PlayerIndex))
                        {
                            PrepareStackSources(
                                m_Stacks[stackIndex],
                                sourceLease,
                                in sourcePreparations,
                                presentationDeltaSeconds,
                                actionSourceSamples,
                                providerSourceSamples);
                        }
                    }
                }
                using (PrepareDirectMarker.Auto())
                {
                    for (int playerIndex = 0; playerIndex < m_DirectPlayers.Length; playerIndex++)
                    {
                        if (IsPlayerActive(m_DirectPlayers[playerIndex].PlayerIndex))
                        {
                            PrepareDirectSource(
                                playerIndex,
                                sourceLease,
                                in sourcePreparations,
                                presentationDeltaSeconds,
                                providerSourceSamples);
                        }
                    }
                }
                using (PrepareSequenceMarker.Auto())
                {
                    CharacterPoseSourceTuningView sourceTuning =
                        m_SourceModule.RequireTuning(
                            sourceDemand.Lineage.TuningGeneration);
                    for (int playerIndex = 0;
                         playerIndex <
                         m_PoseStateSources.ClipPlayers.Length;
                         playerIndex++)
                        PrepareSequenceSource(
                            playerIndex,
                            sourceLease,
                            in sourcePreparations,
                            presentationDeltaSeconds,
                            sourceTuning.RequireClipPlayRate(
                                playerIndex));
                }
                using (PrepareBlendSpaceMarker.Auto())
                {
                    for (int playerIndex = 0;
                         playerIndex <
                         m_PoseStateSources.BlendSpacePlayers.Length;
                         playerIndex++)
                    {
                        PrepareBlendSpaceSource(
                            playerIndex,
                            sourceLease,
                            in sourcePreparations,
                            presentationDeltaSeconds);
                    }
                }
            }

            CharacterPoseSourcePreparedResources preparedSources =
                m_SourceModule.RequirePreparedResources(sourceLease);
            CharacterPoseGraphStagedExecutor poseExecutor;
            AnimationFinalPoseNativeReadBinding finalRead;
            using (ValidateMarker.Auto())
            {
                for (int slotIndex = 0; slotIndex < m_Stacks.Length; slotIndex++)
                {
                    AnimationBlendStackRuntime stack = m_Stacks[slotIndex];
                    AnimationPlayerPoseNativeWriteBinding write =
                        m_ProgramFrames.RequirePlayerWriteBinding(
                            stack.PlayerIndex,
                            completionIdentity);
                    m_SlotJobs[slotIndex] = stack.PrepareSlotJob(
                        completionIdentity,
                        in write,
                        m_SourceModule);
                }
                for (int slotIndex = 0;
                     slotIndex < m_Stacks.Length;
                     slotIndex++)
                {
                    m_Stacks[slotIndex].PrepareCompletion(
                        completionIdentity);
                }
                for (int playerIndex = 0; playerIndex < m_DirectPlayers.Length; playerIndex++)
                {
                    AnimationSelectedPosePlayerRuntime player = m_DirectPlayers[playerIndex];
                    AnimationPlayerPoseNativeWriteBinding write =
                        m_ProgramFrames.RequirePlayerWriteBinding(
                            player.PlayerIndex,
                            completionIdentity);
                    CharacterPoseSourceBinding sourceBinding =
                        preparedSources.RequireDirectBinding(
                            playerIndex);
                    m_DirectPlayerJobs[playerIndex] = player.PrepareJob(
                        completionIdentity,
                        in write,
                        sourceBinding.PhysicalIdentity,
                        sourceBinding.SourceIndex);
                }
                for (int playerIndex = 0;
                     playerIndex <
                     m_PoseStateSources.ClipPlayers.Length;
                     playerIndex++)
                {
                    AnimationClipPlayerRuntime player =
                        m_PoseStateSources.ClipPlayers[
                            playerIndex];
                    AnimationPlayerPoseNativeWriteBinding write =
                        m_ProgramFrames.RequirePlayerWriteBinding(
                            player.PlayerIndex,
                            completionIdentity);
                    CharacterPoseSourceBinding sourceBinding =
                        preparedSources.RequireClipBinding(
                            playerIndex);
                    m_ClipPlayerJobs[playerIndex] = player.PrepareJob(
                        completionIdentity,
                        in write,
                        sourceBinding.PhysicalIdentity,
                        sourceBinding.SourceIndex);
                }
                for (int playerIndex = 0;
                     playerIndex <
                     m_PoseStateSources.BlendSpacePlayers.Length;
                     playerIndex++)
                {
                    AnimationBlendSpacePlayerRuntime player =
                        m_PoseStateSources.BlendSpacePlayers[
                            playerIndex];
                    AnimationPlayerPoseNativeWriteBinding write =
                        m_ProgramFrames.RequirePlayerWriteBinding(
                            player.PlayerIndex,
                            completionIdentity);
                    CharacterPoseSourceBinding sourceBinding =
                        preparedSources.RequireBlendSpaceBinding(
                            playerIndex);
                    m_BlendSpacePlayerJobs[playerIndex] = player.PrepareJob(
                        completionIdentity,
                        in write,
                        sourceBinding.PhysicalIdentity,
                        sourceBinding.SourceIndex);
                }
                StageCompletedSources(completionIdentity);
                CharacterPoseProgramTuningView programTuning =
                    m_ProgramTuning.RequireCommitted(
                        sourceDemand.Lineage.TuningGeneration);
                poseExecutor = m_ProgramRuntime.BindExecutor(
                    in programTuning,
                    m_ProgramFrames.RequirePoseGraphBinding(
                        completionIdentity),
                    recordDiagnostics);
                finalRead =
                    m_ProgramFrames.RequireFinalReadBinding(
                        completionIdentity);
                InstallOrUpdateJobs();
                m_FinalPublication.ValidateWriterBeforeEvaluate(
                    in finalRead);
            }

            CharacterPoseSourceFrameResult sourceFrame =
                m_SourceModule.PrepareFrameResult(
                sourceLease,
                in preparedSources,
                actionSourceSamples,
                providerSourceSamples);
            if (!sourceFrame.IsReady)
            {
                throw new InvalidOperationException(
                    $"Pose source frame ended as '{sourceFrame.Outcome}'.");
            }
            var prepared = new CharacterPoseProgramPrepared(in sourceFrame);
            m_PreparedPage.Prepare(
                in prepared,
                presentationDeltaSeconds,
                in frame,
                poseExecutor,
                in finalRead);
            return prepared;
        }

        internal CharacterPoseFrameExecutionResult ExecuteEvaluateBarrier(
            in CharacterBodyPresentationFrame bodyFrame,
            in CharacterPresentationFactFrame factFrame,
            CharacterPoseSourceFrameLease sourceLease,
            CharacterPoseConstraintFrameLease constraintLease,
            CharacterFinalPosePublicationFrameLease publicationLease,
            in CharacterPoseProgramPrepared prepared,
            Action enterEvaluateBarrier)
        {
            RequireAlive();
            RequireOpenMutation();
            ActorId actorId = prepared.Lineage.ActorId;
            ulong renderFrame = prepared.Lineage.PresentationFrame;
            if (!actorId.IsValid)
                throw new ArgumentException(
                    "Pose Plan Actor identity is invalid.",
                    nameof(actorId));
            if (renderFrame == 0)
                throw new ArgumentOutOfRangeException(nameof(renderFrame));
            if (!bodyFrame.IsValid)
                throw new ArgumentException(
                    "Pose Plan Body frame is invalid.",
                    nameof(bodyFrame));
            if (!factFrame.IsValid)
                throw new ArgumentException(
                    "Pose Plan Presentation Fact frame is invalid.",
                    nameof(factFrame));
            if (!prepared.IsValid ||
                prepared.Lineage.CompletionIdentity != m_FrameCompletionContext ||
                !m_ProgramFrames.HasPendingEvaluationFrame ||
                m_ProgramFrames.PendingEvaluationCompletionIdentity !=
                    prepared.Lineage.CompletionIdentity)
            {
                throw new ArgumentException(
                    "Pose Plan prepared evaluation is not the active Pending frame.",
                    nameof(prepared));
            }
            if (enterEvaluateBarrier == null)
                throw new ArgumentNullException(nameof(enterEvaluateBarrier));

            ulong completionIdentity = prepared.Lineage.CompletionIdentity;
            CharacterPoseProgramPreparedState preparedState =
                m_PreparedPage.Consume(in prepared);
            float presentationDeltaSeconds =
                preparedState.PresentationDeltaSeconds;
            CharacterPoseGraphNativeBinding frame =
                preparedState.Frame;
            CharacterPoseGraphStagedExecutor poseExecutor =
                preparedState.Executor;
            AnimationFinalPoseNativeReadBinding finalRead =
                preparedState.FinalRead;

            enterEvaluateBarrier();
            m_SourceModule.EnterEvaluateBarrier(
                sourceLease);
            using (GraphEvaluateMarker.Auto())
                m_Animancer.Evaluate(presentationDeltaSeconds);
            using (PoseGraphExecuteMarker.Auto())
            {
                poseExecutor.BeginStagedEvaluation(renderFrame);
                if (m_HasSequencePreview)
                {
                    poseExecutor.ExecuteSequencePreview(
                        m_SequencePreviewOperationIndex);
                }
                else
                {
                    for (int stageIndex = 0;
                         stageIndex < m_ExecutionView.Stages.Length;
                         stageIndex++)
                    {
                        AnimationPoseGraphNativeStage stage =
                            m_ExecutionView.Stages[stageIndex];
                        CharacterPoseWorldAwareStageInput worldInput = default;
                        if (stage.ExecutionDomain ==
                            CharacterPoseExecutionDomain.WorldAwareValue)
                        {
                            worldInput = BuildWorldAwareStageInput(
                                actorId,
                                renderFrame,
                                presentationDeltaSeconds,
                                in bodyFrame,
                                in factFrame,
                                completionIdentity,
                                in stage);
                        }
                        if (!poseExecutor.ExecuteStage(
                                stageIndex,
                                presentationDeltaSeconds,
                                in worldInput))
                            break;
                    }
                    poseExecutor.CompleteStagedEvaluation();
                }
                m_ProgramFrames.RequireEvaluationStagesCompleted(
                    completionIdentity);
            }
            CharacterPoseFrameLineage completedLineage =
                prepared.Lineage;
            CharacterPoseProgramResult programResult =
                CreateProgramResult(
                    in completedLineage,
                    in finalRead);
            CharacterPoseConstraintResult constraintResult =
                m_PoseConstraints.CompleteFrame(
                    constraintLease,
                    in completedLineage,
                    programResult.OutputAvailability,
                    programResult.OutputInvalidReason,
                    programResult.GraphInvalidReason);
            CharacterFinalPosePublicationResult publicationResult;
            using (FinalWriteMarker.Auto())
            {
                publicationResult = m_FinalPublication.PreparePending(
                    publicationLease,
                    in completedLineage,
                    in programResult,
                    in constraintResult,
                    in finalRead,
                    m_SourceModule);
                m_PendingFrameOutcome = publicationResult.Outcome;
                m_FinalPublication.WritePhysicalPose(
                    publicationLease,
                    in finalRead);
            }
            var executionResult = new CharacterPoseFrameExecutionResult(
                in programResult,
                in constraintResult,
                in publicationResult);
            if (!executionResult.IsValid)
            {
                throw new InvalidOperationException(
                    "Pose frame typed execution results are inconsistent.");
            }
            if (m_PendingFrameOutcome !=
                AnimationPresentationFrameOutcome.Committed)
            {
                return executionResult;
            }

            using (SealMarker.Auto())
            {
                for (int i = 0; i < m_Stacks.Length; i++)
                    m_Stacks[i].CompleteFrame(completionIdentity);
                for (int i = 0; i < m_DirectPlayers.Length; i++)
                    m_DirectPlayers[i].CompleteFrame();
                for (int i = 0;
                     i < m_PoseStateSources.ClipPlayers.Length;
                     i++)
                    m_PoseStateSources.ClipPlayers[i]
                        .CompleteFrame();
                for (int i = 0;
                     i < m_PoseStateSources.BlendSpacePlayers.Length;
                     i++)
                    m_PoseStateSources.BlendSpacePlayers[i]
                        .CompleteFrame();
                for (int i = 0; i < m_StackRoutes.Length; i++)
                    m_StackRoutes[i].NotifyNativeFrameCompleted(m_InertializationPlan, completionIdentity);
                m_PoseStateSources.NotifyNativeFrameCompleted(
                    m_InertializationPlan,
                    completionIdentity);
                m_PendingCompletedFrame = frame;
                m_HasPendingCompletedFrame = true;
            }
            return executionResult;
        }

        CharacterPoseProgramResult CreateProgramResult(
            in CharacterPoseFrameLineage lineage,
            in AnimationFinalPoseNativeReadBinding finalRead)
        {
            bool completed =
                finalRead.Availability[0] == AnimationPoseAvailability.Pose &&
                finalRead.OutputInvalidReason[0] ==
                    AnimationPoseNativeInvalidReason.None &&
                finalRead.PoseGraphInvalidReason[0] ==
                    AnimationPoseNativeInvalidReason.None &&
                finalRead.PoseGraphInvalidOperationIndex[0] == -1;
            return new CharacterPoseProgramResult(
                in lineage,
                completed
                    ? AnimationPresentationFrameOutcome.Committed
                    : AnimationPresentationFrameOutcome.TypedInvalid,
                finalRead.Availability[0],
                finalRead.OutputInvalidReason[0],
                finalRead.PoseGraphInvalidReason[0],
                finalRead.PoseGraphInvalidOperationIndex[0]);
        }

        CharacterPoseWorldAwareStageInput BuildWorldAwareStageInput(
            ActorId actorId,
            ulong renderFrame,
            float presentationDeltaSeconds,
            in CharacterBodyPresentationFrame bodyFrame,
            in CharacterPresentationFactFrame factFrame,
            ulong completionIdentity,
            in AnimationPoseGraphNativeStage stage)
        {
            CharacterPoseWorldAwareStageInput result = default;
            for (int operationIndex = stage.OperationStart;
                 operationIndex < stage.OperationStart +
                 stage.OperationCount;
                 operationIndex++)
            {
                AnimationPoseGraphNativeOperation operation =
                    m_ExecutionView.Operations[operationIndex];
                switch (operation.Code)
                {
                    case CharacterPoseOperationCode.FootPlacement:
                        if (result.HasFootPlacement)
                        {
                            throw new InvalidOperationException(
                                "World-Aware Pose stage contains multiple Foot Placement operations.");
                        }
                        result = BuildFootPlacementInput(
                            actorId,
                            renderFrame,
                            presentationDeltaSeconds,
                            in bodyFrame,
                            in factFrame,
                            completionIdentity,
                            in operation);
                        break;
                }
            }
            if (!result.HasFootPlacement)
            {
                throw new InvalidOperationException(
                    "World-Aware Pose stage has no supported planner operation.");
            }
            return result;
        }

        CharacterPoseWorldAwareStageInput BuildFootPlacementInput(
            ActorId actorId,
            ulong renderFrame,
            float presentationDeltaSeconds,
            in CharacterBodyPresentationFrame bodyFrame,
            in CharacterPresentationFactFrame factFrame,
            ulong completionIdentity,
            in AnimationPoseGraphNativeOperation operation)
        {
            CharacterFootPlacementConstraintHandle constraint =
                operation.FootPlacementConstraint;
            if (!m_PoseConstraints.HasFootPlacement)
            {
                return new CharacterPoseWorldAwareStageInput(
                    constraint);
            }
            AnimationPoseValueNativeReadBinding inputBinding =
                m_ProgramFrames.RequirePoseValueReadBinding(
                    operation.InputValueIndexA,
                    completionIdentity);
            int contributionCount =
                m_FinalPublication.ResolveContributions(
                    in inputBinding,
                    m_SourceModule,
                    m_FootPlacementContributions);
            AnimationFootMotionRuntimeFrame footStepObservation =
                ResolveFootStepObservationFrame(
                    completionIdentity,
                    m_FootPlacementContributions,
                    contributionCount);
            var input = new CharacterFootPlacementPoseInput(
                m_Projection.PosePlan.PlanHash,
                in inputBinding,
                in footStepObservation,
                m_FootPlacementContributions,
                contributionCount);
            if ((uint)operation.ParameterIndex >=
                    (uint)inputBinding.PoseParameters.Length ||
                inputBinding.PoseParameterAvailability[operation.ParameterIndex] == 0 ||
                !float.IsFinite(inputBinding.PoseParameters[operation.ParameterIndex]))
            {
                throw new InvalidOperationException(
                    "Foot Placement parameter input is unavailable.");
            }
            float footPlacementWeight =
                inputBinding.PoseParameters[operation.ParameterIndex];
            var planningFrame = new CharacterFootPlacementFrameInput(
                actorId,
                renderFrame,
                presentationDeltaSeconds,
                footPlacementWeight,
                bodyFrame,
                in factFrame,
                in input);
            return new CharacterPoseWorldAwareStageInput(
                constraint,
                in planningFrame);
        }

        AnimationFootMotionRuntimeFrame ResolveFootStepObservationFrame(
            ulong completionIdentity,
            AnimationPoseSourceContribution[] contributions,
            int contributionCount)
        {
            AnimationPoseSourceContribution contribution =
                RequireFootStepObservationContribution(
                    contributions,
                    contributionCount);
            ClipSamplePlan clipSample = m_SourceModule.RequireDominantClipSample(
                contribution.SourceId,
                contribution.NodeId,
                completionIdentity);
            AnimationFootStepObservationCurvePair curves;
            string sourceIdentity;
            ulong sourceSampleIdentity;
            switch (contribution.SourceId.SourceKind)
            {
                case AnimationPoseSourceKind.Timeline:
                    if ((uint)contribution.SourceOwnerIndex >=
                        (uint)m_Projection.Producers.Count)
                    {
                        throw new InvalidOperationException(
                            $"Timeline Foot Step source '{contribution.SourceId}' has no exact producer.");
                    }
                    CharacterPresentationAnimationBinding animation =
                        m_Projection.Producers[contribution.SourceOwnerIndex]?.Animation ??
                        throw new InvalidOperationException(
                            $"Timeline Foot Step source '{contribution.SourceId}' has no animation binding.");
                    if ((uint)clipSample.ClipBindingIndex >=
                        (uint)animation.Clips.Count)
                    {
                        throw new InvalidOperationException(
                            $"Timeline Foot Step source '{contribution.SourceId}' Clip binding is outside its producer catalog.");
                    }
                    CharacterPresentationAnimationClipBinding binding =
                        animation.Clips[clipSample.ClipBindingIndex] ??
                        throw new InvalidOperationException(
                            $"Timeline Foot Step source '{contribution.SourceId}' Clip binding is missing.");
                    binding.RequireSampleable(clipSample.ClipBindingIndex);
                    if (!ReferenceEquals(binding.Clip, clipSample.Clip))
                    {
                        throw new InvalidOperationException(
                            $"Timeline Foot Step source '{contribution.SourceId}' Clip sample does not match its compiled binding.");
                    }
                    curves = binding.FootStepObservation;
                    sourceIdentity = binding.ClipIdentity;
                    sourceSampleIdentity = AnimationFootMotionIdentity.Source(
                        binding.ClipAuthoringId);
                    break;
                case AnimationPoseSourceKind.Clip:
                    if (!m_Projection.TryGetPoseSource(
                            contribution.SourceId.PresentationPoseSourceIndex,
                            out CharacterPresentationPoseSourcePlan source) ||
                        clipSample.ClipBindingIndex != 0 ||
                        !ReferenceEquals(source.Clip, clipSample.Clip))
                    {
                        throw new InvalidOperationException(
                            $"Clip Foot Step source '{contribution.SourceId}' does not match its compiled source plan.");
                    }
                    source.RequireValid();
                    curves = source.FootStepObservation;
                    sourceIdentity = source.ClipIdentity;
                    sourceSampleIdentity = AnimationFootMotionIdentity.Source(
                        contribution.SourceId);
                    break;
                default:
                    throw new InvalidOperationException(
                        $"Foot Step source kind '{contribution.SourceId.SourceKind}' has no formal observation contract.");
            }
            curves.RequireValid();
            int cycle = checked((int)Math.Floor(
                clipSample.ContinuousClipTime / clipSample.Clip.length));
            return new AnimationFootMotionRuntimeFrame(
                completionIdentity,
                contribution.NodeId,
                contribution.SourceId,
                contribution.ContributionContinuityIdentity,
                sourceIdentity,
                sourceSampleIdentity,
                clipSample.ClipBindingIndex,
                cycle,
                contribution.Weight,
                clipSample.NormalizedTime,
                curves.Left.Sample(
                    clipSample.NormalizedTime,
                    cycle,
                    clipSample.Clip.length,
                    clipSample.Clip.isLooping),
                curves.Right.Sample(
                    clipSample.NormalizedTime,
                    cycle,
                    clipSample.Clip.length,
                    clipSample.Clip.isLooping));
        }

        static AnimationPoseSourceContribution
            RequireFootStepObservationContribution(
                AnimationPoseSourceContribution[] contributions,
                int contributionCount)
        {
            if (contributions == null || contributionCount <= 0 ||
                contributionCount > contributions.Length)
            {
                throw new ArgumentException(
                    "Foot Step observation contribution input is invalid.");
            }
            AnimationPoseSourceContribution selected = default;
            float selectedWeight = -1f;
            for (int i = 0; i < contributionCount; i++)
            {
                AnimationPoseSourceContribution candidate = contributions[i];
                if (candidate.Kind != AnimationPoseContributionKind.Live ||
                    candidate.Weight <= selectedWeight)
                {
                    continue;
                }
                selected = candidate;
                selectedWeight = candidate.Weight;
            }
            if (!selected.SourceId.IsValid)
            {
                throw new InvalidOperationException(
                    "Foot Placement has no Live Foot Step observation source.");
            }
            return selected;
        }

        private bool TryCopyCompletedPlayerPose(
            int playerIndex,
            int[] rigBoneIndices,
            Vector3[] positions,
            out AnimationFootPlacementSample footPlacement)
        {
            RequireAlive();
            if (playerIndex < 0 ||
                rigBoneIndices == null || positions == null ||
                rigBoneIndices.Length == 0 || positions.Length != rigBoneIndices.Length)
                throw new ArgumentException("Animation Player history copy input is invalid.");
            if (!m_HasPendingCompletedFrame)
            {
                footPlacement = default;
                return false;
            }
            var read = new AnimationPlayerPoseNativeWriteBinding(in m_PendingCompletedFrame, playerIndex);
            if (read.CompletedAt[0] != m_PendingCompletedFrame.CompletionIdentity ||
                read.Availability[0] != AnimationPoseAvailability.Pose || read.HasFootFeatures[0] == 0 ||
                read.PoseParameterAvailability[m_FootPlacementWeightParameterIndex] == 0)
            {
                footPlacement = default;
                return false;
            }
            for (int i = 0; i < rigBoneIndices.Length; i++)
            {
                int boneIndex = rigBoneIndices[i];
                if ((uint)boneIndex >= (uint)read.DenseLocalPoses.Length)
                    throw new InvalidOperationException("Motion Matching history Bone index is outside the completed Player pose.");
                positions[i] = read.DenseLocalPoses[boneIndex].Position;
            }
            footPlacement = new AnimationFootPlacementSample(
                read.PoseParameters[m_FootPlacementWeightParameterIndex],
                read.LeftFootFeatures[0],
                read.RightFootFeatures[0]);
            return true;
        }

        internal void Reset(PoseDiscontinuityResetReason reason)
        {
            RequireAlive();
            RequireNoOpenMutation();
            if (reason == PoseDiscontinuityResetReason.None)
                throw new ArgumentOutOfRangeException(nameof(reason));
            m_FinalPublication.Invalidate();
            InvalidateDiagnostics();
            m_ActorDiagnosticsProjector.Reset();
            m_SourceModule.CancelReleaseDiagnostics();
            m_SourceModule.ClearActionSlotReleaseCompletions();
            ClearReleaseJournals();
            m_SourceModule.ClearActionBackendReleaseCompletions();
            m_LastCompletedFrame = default;
            m_PendingCompletedFrame = default;
            m_HasCompletedFrame = false;
            m_HasPendingCompletedFrame = false;
            m_PreparedPage.Clear();
            m_ProgramFrames.ClearSourceDemand();
            m_PendingFrameOutcome = AnimationPresentationFrameOutcome.None;
            m_InertializationPlan.Reset();
            m_PoseConstraints.ResetSolvers();
            ulong completionIdentity = NextCompletionIdentity();
            for (int i = 0; i < m_StackRoutes.Length; i++)
                m_StackRoutes[i].Reset();
            for (int i = 0; i < m_Stacks.Length; i++)
                m_Stacks[i].Reset(completionIdentity);
            ReleaseCompletedSources(completionIdentity);
            for (int i = 0; i < m_DirectPlayers.Length; i++)
                m_DirectPlayers[i].Reset(reason);
            m_PoseStateSources.Reset(reason);
            for (int i = 0; i < m_RootOrientationWarps.Length; i++)
            {
                m_RootOrientationWarps[i].Reset();
                var control =
                    new CharacterRootOrientationWarpNativeControl(
                        false,
                        0f);
                m_ProgramFrames.SetRootOrientationWarpControl(
                    i,
                    in control);
            }
            ReleaseDirectSources();
            ReleaseSequenceSources();
            ReleaseBlendSpaceSources();
            m_SourceModule.Clear();
            m_CommitValidated = false;
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            m_FinalPublication.Invalidate();
            m_FootIkDiagnosticsProjector.Invalidate();
            m_LastCompletedFrame = default;
            m_PendingCompletedFrame = default;
            m_HasCompletedFrame = false;
            m_HasPendingCompletedFrame = false;
            m_PreparedPage.Clear();
            m_ProgramFrames.ClearSourceDemand();
            m_PendingFrameOutcome = AnimationPresentationFrameOutcome.None;
            Exception failure = null;
            DisposeStep(m_DiagnosticsPublisher.Dispose, ref failure);
            DisposeStep(RemoveJobs, ref failure);
            DisposeStep(m_SourceModule.Dispose, ref failure);
            DisposeStep(m_ProgramRuntime.Dispose, ref failure);
            DisposeStep(RestoreGraphClock, ref failure);
            if (failure != null)
                throw failure;
        }

        void PrepareStackSources(
            AnimationBlendStackRuntime stack,
            CharacterPoseSourceFrameLease sourceLease,
            in CharacterPoseSourcePreparationView preparations,
            float presentationDeltaSeconds,
            IReadOnlyDictionary<AnimationPlayerSourceSampleKey,
                AnimationResolvedPoseSourceSample> actionSourceSamples,
            IReadOnlyDictionary<AnimationPlayerSourceSampleKey,
                PresentationPoseSourceSample> providerSourceSamples)
        {
            if (!stack.HasCurrentSelectionSample)
                return;
            for (int entryIndex = 0; entryIndex < stack.EntryCount; entryIndex++)
            {
                AnimationBlendEntryId entry = stack.GetEntryId(entryIndex);
                if (entry.SourcePoseTarget || HasEarlierSource(stack, entryIndex, entry.SourceId))
                    continue;
                PrepareSource(
                    stack,
                    sourceLease,
                    in preparations,
                    entry.SourceId,
                    presentationDeltaSeconds,
                    actionSourceSamples,
                    providerSourceSamples);
            }
        }

        void PrepareSource(
            AnimationBlendStackRuntime stack,
            CharacterPoseSourceFrameLease sourceLease,
            in CharacterPoseSourcePreparationView preparations,
            AnimationPoseSourceId sourceId,
            float presentationDeltaSeconds,
            IReadOnlyDictionary<AnimationPlayerSourceSampleKey,
                AnimationResolvedPoseSourceSample> actionSourceSamples,
            IReadOnlyDictionary<AnimationPlayerSourceSampleKey,
                PresentationPoseSourceSample> providerSourceSamples)
        {
            var key = new AnimationPlayerSourceSampleKey(stack.PoseNodeId, sourceId);
            if (sourceId.SourceKind ==
                AnimationPoseSourceKind.Timeline)
            {
                if (!actionSourceSamples.TryGetValue(
                        key,
                        out AnimationResolvedPoseSourceSample
                            sourceSample))
                {
                    throw new InvalidOperationException(
                        $"Action Pose Source '{sourceId}' has no current resolved request.");
                }
                AnimationPoseSampleRequest timelineRequest =
                    sourceSample.Request;
                AnimationPoseSourceCaptureBinding timelineCapture =
                    stack.PrepareCapture(
                        sourceSample,
                        presentationDeltaSeconds);
                CharacterPoseSourcePreparation preparation =
                    CharacterPoseSourcePreparation.Action(
                        in timelineRequest,
                        in timelineCapture,
                        stack.PoseNodeId);
                SubmitSourcePreparation(
                    sourceLease,
                    in preparations,
                    in preparation);
                return;
            }
            if (!providerSourceSamples.TryGetValue(
                    key,
                    out PresentationPoseSourceSample providerSample) ||
                !m_NodeRuntimeIndex.TryGetSourceOwnerIndex(
                    stack.PoseNodeId,
                    out int sourceOwnerIndex))
            {
                throw new InvalidOperationException(
                    $"Presentation Pose Source '{sourceId}' has no current resolved request.");
            }
            AnimationResolvedPoseSourceSample resolved =
                m_SourceModule.ResolveProviderSample(
                    in providerSample,
                    sourceOwnerIndex);
            AnimationPoseSampleRequest request =
                resolved.Request;
            AnimationPoseSourceCaptureBinding capture = stack.PrepareCapture(
                resolved,
                presentationDeltaSeconds);
            CharacterPoseSourcePreparation providerPreparation =
                CharacterPoseSourcePreparation.Provider(
                    in request,
                    in providerSample,
                    in capture,
                    stack.PoseNodeId);
            SubmitSourcePreparation(
                sourceLease,
                in preparations,
                in providerPreparation);
        }

        void PrepareDirectSource(
            int playerIndex,
            CharacterPoseSourceFrameLease sourceLease,
            in CharacterPoseSourcePreparationView preparations,
            float presentationDeltaSeconds,
            IReadOnlyDictionary<AnimationPlayerSourceSampleKey,
                PresentationPoseSourceSample> sourceSamples)
        {
            AnimationSelectedPosePlayerRuntime player = m_DirectPlayers[playerIndex];
            if (!player.HasCurrentSample)
                return;
            var key = new AnimationPlayerSourceSampleKey(player.NodeId, player.SourceId);
            if (!sourceSamples.TryGetValue(
                    key,
                    out PresentationPoseSourceSample sample))
                throw new InvalidOperationException($"Animation Pose Source '{player.SourceId}' has no current resolved request.");
            AnimationPoseSourceCaptureBinding capture = player.PrepareCapture(in sample, presentationDeltaSeconds);
            CharacterPoseSourcePreparation preparation =
                CharacterPoseSourcePreparation.DirectPlayer(
                    playerIndex,
                    player.SourceId,
                    player.SourceOwnerIndex,
                    sample.Clips,
                    in sample,
                    in capture,
                    player.NodeId);
            SubmitSourcePreparation(
                sourceLease,
                in preparations,
                in preparation);
        }

        void PrepareSequenceSource(
            int playerIndex,
            CharacterPoseSourceFrameLease sourceLease,
            in CharacterPoseSourcePreparationView preparations,
            float presentationDeltaSeconds,
            float playRate)
        {
            AnimationClipPlayerRuntime player =
                m_PoseStateSources.ClipPlayers[playerIndex];
            bool selectedPreview = m_HasSequencePreview &&
                                   playerIndex == m_SequencePreviewPlayerIndex;
            if (!selectedPreview && !IsPlayerActive(player.PlayerIndex) ||
                !player.IsRelevant)
                return;
            AnimationPoseSourceCaptureBinding capture = player.PrepareCapture(
                presentationDeltaSeconds,
                playRate);
            CharacterPoseSourcePreparation preparation =
                CharacterPoseSourcePreparation.ClipPlayer(
                    playerIndex,
                    player.SourceId,
                    player.PlayerIndex,
                    player.ClipSamples,
                    in capture,
                    player.NodeId);
            SubmitSourcePreparation(
                sourceLease,
                in preparations,
                in preparation);
        }

        internal void SetSequencePreview(
            PresentationPoseSourceIndex sourceIndex,
            double sampleTime,
            bool resetContinuity)
        {
            RequireAlive();
            RequireNoOpenMutation();
            if (!sourceIndex.IsValid || !double.IsFinite(sampleTime) || sampleTime < 0d)
                throw new ArgumentException("Clip Preview sample is invalid.");
            int playerIndex = -1;
            for (int i = 0; i < m_PoseStateSources.ClipPlayers.Length; i++)
            {
                if (m_PoseStateSources.ClipPlayers[i].SourceIndex != sourceIndex)
                    continue;
                playerIndex = i;
                break;
            }
            if (playerIndex < 0)
                throw new InvalidOperationException(
                    $"Clip Preview source #{sourceIndex.Value} has no compiled Clip Player.");
            int operationIndex = -1;
            for (int i = 0; i < m_Projection.PosePlan.Operations.Count; i++)
            {
                CharacterPresentationPoseOperation operation =
                    m_Projection.PosePlan.Operations[i];
                if (operation.Code != CharacterPoseOperationCode.ClipPlayer ||
                    operation.ClipPlayerIndex != playerIndex)
                    continue;
                operationIndex = operation.Index;
                break;
            }
            if (operationIndex < 0)
                throw new InvalidOperationException(
                    $"Clip Preview source #{sourceIndex.Value} has no compiled Pose operation.");
            m_SequencePreviewPlayerIndex = playerIndex;
            m_SequencePreviewOperationIndex = operationIndex;
            m_SequencePreviewTime = sampleTime;
            m_SequencePreviewReset = resetContinuity;
            m_HasSequencePreview = true;
        }

        internal void ClearSequencePreview()
        {
            RequireAlive();
            RequireNoOpenMutation();
            m_SequencePreviewPlayerIndex = -1;
            m_SequencePreviewOperationIndex = -1;
            m_SequencePreviewTime = 0d;
            m_SequencePreviewReset = false;
            m_HasSequencePreview = false;
        }

        void PrepareBlendSpaceSource(
            int playerIndex,
            CharacterPoseSourceFrameLease sourceLease,
            in CharacterPoseSourcePreparationView preparations,
            float presentationDeltaSeconds)
        {
            AnimationBlendSpacePlayerRuntime player =
                m_PoseStateSources.BlendSpacePlayers[
                    playerIndex];
            if (!IsPlayerActive(player.PlayerIndex) || !player.IsRelevant)
                return;
            AnimationPoseSourceCaptureBinding capture =
                player.PrepareCapture(presentationDeltaSeconds);
            CharacterPoseSourcePreparation preparation =
                CharacterPoseSourcePreparation.BlendSpacePlayer(
                    playerIndex,
                    player.SourceId,
                    player.PlayerIndex,
                    player.ClipSamples,
                    in capture,
                    player.NodeId);
            SubmitSourcePreparation(
                sourceLease,
                in preparations,
                in preparation);
        }

        void SubmitSourcePreparation(
            CharacterPoseSourceFrameLease sourceLease,
            in CharacterPoseSourcePreparationView preparations,
            in CharacterPoseSourcePreparation preparation)
        {
            int index = m_ProgramFrames.AddSourcePreparation(
                in preparation);
            m_SourceModule.Prepare(
                sourceLease,
                in preparations,
                index);
        }

        static bool HasEarlierSource(
            AnimationBlendStackRuntime stack,
            int entryIndex,
            AnimationPoseSourceId sourceId)
        {
            for (int i = 0; i < entryIndex; i++)
            {
                AnimationBlendEntryId candidate = stack.GetEntryId(i);
                if (!candidate.SourcePoseTarget && candidate.SourceId.Equals(sourceId))
                    return true;
            }
            return false;
        }

        void InstallOrUpdateJobs()
        {
            if (!m_JobsInstalled)
            {
                m_BlendSpacePlayerPlayables =
                    new AnimationScriptPlayable[m_BlendSpacePlayerJobs.Length];
                for (int i = 0; i < m_BlendSpacePlayerJobs.Length; i++)
                {
                    m_BlendSpacePlayerPlayables[i] =
                        m_Animancer.Graph.InsertOutputJob(
                            m_BlendSpacePlayerJobs[i]);
                    m_BlendSpacePlayerPlayables[i].SetProcessInputs(true);
                }
                m_ClipPlayerPlayables = new AnimationScriptPlayable[m_ClipPlayerJobs.Length];
                for (int i = 0; i < m_ClipPlayerJobs.Length; i++)
                {
                    m_ClipPlayerPlayables[i] = m_Animancer.Graph.InsertOutputJob(m_ClipPlayerJobs[i]);
                    m_ClipPlayerPlayables[i].SetProcessInputs(true);
                }
                m_DirectPlayerPlayables = new AnimationScriptPlayable[m_DirectPlayerJobs.Length];
                for (int i = 0; i < m_DirectPlayerJobs.Length; i++)
                {
                    m_DirectPlayerPlayables[i] = m_Animancer.Graph.InsertOutputJob(m_DirectPlayerJobs[i]);
                    m_DirectPlayerPlayables[i].SetProcessInputs(true);
                }
                m_SlotPlayables = new AnimationScriptPlayable[m_SlotJobs.Length];
                for (int i = 0; i < m_SlotJobs.Length; i++)
                {
                    m_SlotPlayables[i] = m_Animancer.Graph.InsertOutputJob(m_SlotJobs[i]);
                    m_SlotPlayables[i].SetProcessInputs(true);
                }
                m_JobsInstalled = true;
                return;
            }
            for (int i = 0; i < m_BlendSpacePlayerJobs.Length; i++)
            {
                m_BlendSpacePlayerPlayables[i].SetJobData(
                    m_BlendSpacePlayerJobs[i]);
            }
            for (int i = 0; i < m_ClipPlayerJobs.Length; i++)
                m_ClipPlayerPlayables[i].SetJobData(m_ClipPlayerJobs[i]);
            for (int i = 0; i < m_DirectPlayerJobs.Length; i++)
                m_DirectPlayerPlayables[i].SetJobData(m_DirectPlayerJobs[i]);
            for (int i = 0; i < m_SlotJobs.Length; i++)
                m_SlotPlayables[i].SetJobData(m_SlotJobs[i]);
        }

        CharacterPoseSourceRetirementHandle PrepareSourceRetirement(
            AnimationPoseSourceId sourceId,
            PoseNodeId poseNodeId,
            AnimationPhysicalSourceIdentity expectedPhysicalIdentity)
        {
            var permission =
                new CharacterPoseSourceRetirementPermission(
                    sourceId,
                    poseNodeId,
                    expectedPhysicalIdentity);
            return m_SourceModule.PrepareRetirement(in permission);
        }

        void ReleaseCompletedSources(
            ulong completionIdentity)
        {
            for (int stackIndex = 0; stackIndex < m_Stacks.Length; stackIndex++)
            {
                AnimationBlendStackRuntime stack = m_Stacks[stackIndex];
                CharacterAnimationTransitionRouteRuntime route = m_StackRoutes[stackIndex];
                if (!route.CanReleaseSources)
                    continue;
                bool releasedAny = false;
                int releaseCount = stack.PendingReleaseCount;
                for (int releaseIndex = 0;
                     releaseIndex < releaseCount;
                     releaseIndex++)
                {
                    AnimationBlendStackSourceReleaseToken stackRelease =
                        stack.PrepareRelease(
                            releaseIndex,
                            completionIdentity);
                    AnimationBlendStackRelease release =
                        stackRelease.Release;
                    CharacterPoseSourceRetirementHandle sourceRelease =
                        PrepareSourceRetirement(
                            release.SourceId,
                            release.PoseNodeId,
                            default);
                    m_SourceModule.ApplyRetirement(
                        in sourceRelease);
                    stack.ApplyPreparedRelease(
                        in stackRelease);
                    releasedAny = true;
                }
                if (releasedAny)
                    route.NotifySourcesReleased();
            }
        }

        void RemoveJobs()
        {
            if (!m_JobsInstalled || !m_Animancer || !m_Animancer.IsGraphInitialized)
                return;
            for (int i = m_SlotPlayables.Length - 1; i >= 0; i--)
                AnimancerUtilities.RemovePlayable(m_SlotPlayables[i]);
            for (int i = m_DirectPlayerPlayables.Length - 1; i >= 0; i--)
                AnimancerUtilities.RemovePlayable(m_DirectPlayerPlayables[i]);
            for (int i = m_ClipPlayerPlayables.Length - 1; i >= 0; i--)
                AnimancerUtilities.RemovePlayable(m_ClipPlayerPlayables[i]);
            for (int i = m_BlendSpacePlayerPlayables.Length - 1; i >= 0; i--)
                AnimancerUtilities.RemovePlayable(m_BlendSpacePlayerPlayables[i]);
            m_JobsInstalled = false;
        }

        ulong NextCompletionIdentity()
        {
            if (m_CompletionIdentity == ulong.MaxValue)
                throw new InvalidOperationException("Animation Pose completion identity was exhausted.");
            m_CompletionIdentity++;
            return m_CompletionIdentity;
        }

        void StageCompletedSources(ulong completionIdentity)
        {
            if (m_SourceRetirementState.PendingPoseCount != 0)
            {
                throw new InvalidOperationException(
                    "Pose source releases from the previous committed frame were not finalized.");
            }
            for (int stackIndex = 0;
                 stackIndex < m_Stacks.Length;
                 stackIndex++)
            {
                AnimationBlendStackRuntime stack =
                    m_Stacks[stackIndex];
                CharacterAnimationTransitionRouteRuntime route =
                    m_StackRoutes[stackIndex];
                if (!route.CanReleaseSources)
                    continue;
                int releaseCount =
                    stack.PendingPriorFrameReleaseCount(
                        completionIdentity);
                for (int releaseIndex = 0;
                     releaseIndex < releaseCount;
                     releaseIndex++)
                {
                    AnimationBlendStackSourceReleaseToken stackRelease =
                        stack.PrepareRelease(
                            releaseIndex,
                            completionIdentity);
                    AnimationBlendStackRelease release =
                        stackRelease.Release;
                    AnimationPhysicalSourceIdentity physical =
                        m_SourceModule.RequireIdentity(
                            release.SourceId,
                            release.PoseNodeId);
                    if (route.IsAnimationSlot &&
                        CharacterPoseProgramSourceRetirementState
                            .IsFiniteActionSource(
                            release.SourceId))
                    {
                        m_SourceRetirementState.StageAction(
                            m_SourceModule,
                            route.SlotId,
                            stack,
                            route,
                            in stackRelease,
                            physical);
                        continue;
                    }
                    m_SourceRetirementState.StagePose(
                        m_SourceModule,
                        stack,
                        route,
                        in stackRelease,
                        physical);
                }
            }
        }

        void ClearReleaseJournals()
        {
            m_SourceRetirementState.Clear();
            ClearValidatedActionBackendAcknowledgements();
            ClearPreparedMotionMatchingPoseCompletion();
        }

        void DiscardPendingReleasePreparation()
        {
            m_SourceRetirementState.DiscardFrame();
        }

        void PrepareLinkedPoseSelection(
            CharacterLinkedPoseRuntimeSession linkedPose)
        {
            ClearLinkedPoseFrameSelection();
            IReadOnlyList<CharacterLinkedPoseGroupProjectionDescriptor> groups =
                m_Projection.LinkedPose.Groups;
            for (int groupIndex = 0; groupIndex < groups.Count; groupIndex++)
            {
                CharacterLinkedPoseGroupProjectionDescriptor group =
                    groups[groupIndex];
                CharacterLinkedPoseGenerationHandle selection =
                    linkedPose.RequireIncoming(group.GroupId);
                m_ProgramRuntime.SetLinkedPoseGroupSelection(
                    in selection);
                int activeCount = 0;
                for (int fragmentIndex = 0;
                     fragmentIndex <
                     m_Projection.PosePlan.LinkedPoseFragments.Count;
                     fragmentIndex++)
                {
                    CharacterLinkedPoseEntryFragmentPlanDescriptor fragment =
                        m_Projection.PosePlan.LinkedPoseFragments[
                            fragmentIndex];
                    if (fragment.GroupId != group.GroupId)
                        continue;
                    if (selection.PoseDiscontinuity)
                        m_LinkedPoseResetFragments[fragmentIndex] = true;
                    if (fragment.ImplementationId !=
                        selection.ImplementationId)
                    {
                        continue;
                    }
                    m_LinkedPoseActiveFragments[fragmentIndex] = true;
                    activeCount++;
                }
                if (activeCount == 0)
                {
                    throw new InvalidOperationException(
                        $"Linked Pose Group '{group.GroupId}' selection '{selection.ImplementationId}' has no Entry fragments.");
                }
            }
        }

        void ApplyLinkedPoseGenerationResets()
        {
            if (m_LinkedPoseResetFragments.Length == 0)
                return;
            for (int i = 0; i < m_Stacks.Length; i++)
            {
                int fragmentIndex =
                    RequirePlayerFragmentIndex(
                        m_Stacks[i].PlayerIndex);
                if (!RequiresFragmentReset(fragmentIndex))
                    continue;
                m_StackRoutes[i].Reset();
                m_Stacks[i].Reset(m_CompletionIdentity);
            }
            for (int i = 0; i < m_DirectPlayers.Length; i++)
            {
                int fragmentIndex =
                    RequirePlayerFragmentIndex(
                        m_DirectPlayers[i].PlayerIndex);
                if (RequiresFragmentReset(fragmentIndex))
                {
                    m_DirectPlayers[i].Reset(
                        PoseDiscontinuityResetReason.BranchReplacement);
                }
            }
            m_PoseStateSources.ApplyLinkedPoseGenerationResets();
            for (int i = 0; i < m_InertializationLinkedPoseFragmentIndices.Length; i++)
            {
                if (RequiresFragmentReset(
                        m_InertializationLinkedPoseFragmentIndices[i]))
                {
                    m_InertializationPlan.RequestReset(i);
                }
            }
            for (int i = 0; i < m_RootOrientationWarps.Length; i++)
            {
                if (RequiresFragmentReset(
                        m_RootOrientationWarpLinkedPoseFragmentIndices[i]))
                {
                    m_RootOrientationWarps[i].Reset();
                }
            }
        }

        bool IsPlayerActive(int playerIndex) =>
            IsFragmentActive(
                RequirePlayerFragmentIndex(playerIndex));

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

        void ClearLinkedPoseFrameSelection()
        {
            Array.Clear(
                m_LinkedPoseActiveFragments,
                0,
                m_LinkedPoseActiveFragments.Length);
            Array.Clear(
                m_LinkedPoseResetFragments,
                0,
                m_LinkedPoseResetFragments.Length);
        }

        void BeginPendingModuleFrames()
        {
            int stackCount = 0;
            int directPlayerCount = 0;
            bool poseStateSourcesOpen = false;
            try
            {
                m_PoseStateSources.BeginFrame();
                poseStateSourcesOpen = true;
                for (; stackCount < m_Stacks.Length; stackCount++)
                    m_Stacks[stackCount].BeginFrame();
                for (; directPlayerCount < m_DirectPlayers.Length; directPlayerCount++)
                    m_DirectPlayers[directPlayerCount].BeginFrame();
            }
            catch
            {
                for (int i = directPlayerCount - 1; i >= 0; i--)
                    m_DirectPlayers[i].DiscardFrame();
                for (int i = stackCount - 1; i >= 0; i--)
                    m_Stacks[i].DiscardFrame();
                if (poseStateSourcesOpen)
                    m_PoseStateSources.DiscardFrame();
                throw;
            }
        }

        void DiscardPendingModuleFrames()
        {
            for (int i = m_DirectPlayers.Length - 1; i >= 0; i--)
                m_DirectPlayers[i].DiscardFrame();
            for (int i = m_Stacks.Length - 1; i >= 0; i--)
                m_Stacks[i].DiscardFrame();
            m_PoseStateSources.DiscardFrame();
            m_SourceModule.ClearActionSlotReleaseCompletions();
            m_SourceModule.CancelReleaseDiagnostics();
            m_SourceModule.ClearUsage();
            m_MotionMatchingHistoryCompletionCount = 0;
        }

        void RequireMutation(
            CharacterPoseProgramFrameLease lease)
        {
            RequireAlive();
            if (!lease.IsValid ||
                !m_ActiveFrameLease.IsValid ||
                lease.Lineage != m_ActiveFrameLease.Lineage ||
                !m_HasOpenFrame)
            {
                throw new InvalidOperationException(
                    "Pose Plan frame mutation lease is invalid.");
            }
        }

        void RequireOpenMutation()
        {
            if (!m_ActiveFrameLease.IsValid ||
                !m_HasOpenFrame)
            {
                throw new InvalidOperationException(
                    "Pose Plan mutation must be open before frame evaluation.");
            }
        }

        void RequireNoOpenMutation()
        {
            if (m_ActiveFrameLease.IsValid ||
                m_HasOpenFrame)
            {
                throw new InvalidOperationException(
                    "Pose Plan frame mutation must close before reset.");
            }
        }

        void RestoreGraphClock()
        {
            if (m_ManagesGraphClock && m_Animancer && m_Animancer.IsGraphInitialized)
                m_Animancer.Graph.UnpauseGraph();
        }

        static void DisposeStep(Action action, ref Exception failure)
        {
            try
            {
                action();
            }
            catch (Exception exception)
            {
                if (failure == null)
                    failure = exception;
            }
        }

        static void DiscardStep(
            Action action,
            ref Exception failure)
        {
            try
            {
                action();
            }
            catch (Exception exception)
            {
                failure = failure == null
                    ? exception
                    : new AggregateException(
                        failure,
                        exception);
            }
        }

        void ReleaseDirectSources()
        {
            for (int playerIndex = 0; playerIndex < m_DirectPlayers.Length; playerIndex++)
            {
                AnimationSelectedPosePlayerRuntime player = m_DirectPlayers[playerIndex];
                int releaseCount = player.PendingReleaseCount;
                for (int releaseIndex = 0;
                     releaseIndex < releaseCount;
                     releaseIndex++)
                {
                    AnimationPlayerReleaseToken playerRelease =
                        player.PrepareRelease(releaseIndex);
                    AnimationPoseSourceId sourceId =
                        playerRelease.SourceId;
                    CharacterPoseSourceRetirementHandle sourceRelease =
                        PrepareSourceRetirement(
                            sourceId,
                            player.NodeId,
                            default);
                    m_SourceModule.ApplyRetirement(
                        in sourceRelease);
                    player.ApplyPreparedRelease(
                        in playerRelease);
                }
            }
        }

        void ReleaseSequenceSources()
        {
            for (int playerIndex = 0;
                 playerIndex <
                 m_PoseStateSources.ClipPlayers.Length;
                 playerIndex++)
            {
                AnimationClipPlayerRuntime player =
                    m_PoseStateSources.ClipPlayers[
                        playerIndex];
                int releaseCount = player.PendingReleaseCount;
                for (int releaseIndex = 0;
                     releaseIndex < releaseCount;
                     releaseIndex++)
                {
                    AnimationPlayerReleaseToken playerRelease =
                        player.PrepareRelease(releaseIndex);
                    AnimationPoseSourceId sourceId =
                        playerRelease.SourceId;
                    CharacterPoseSourceRetirementHandle sourceRelease =
                        PrepareSourceRetirement(
                            sourceId,
                            player.NodeId,
                            default);
                    m_SourceModule.ApplyRetirement(
                        in sourceRelease);
                    player.ApplyPreparedRelease(
                        in playerRelease);
                }
            }
        }

        void ReleaseBlendSpaceSources()
        {
            for (int playerIndex = 0;
                 playerIndex <
                 m_PoseStateSources.BlendSpacePlayers.Length;
                 playerIndex++)
            {
                AnimationBlendSpacePlayerRuntime player =
                    m_PoseStateSources.BlendSpacePlayers[
                        playerIndex];
                int releaseCount = player.PendingReleaseCount;
                for (int releaseIndex = 0;
                     releaseIndex < releaseCount;
                     releaseIndex++)
                {
                    AnimationPlayerReleaseToken playerRelease =
                        player.PrepareRelease(releaseIndex);
                    AnimationPoseSourceId sourceId =
                        playerRelease.SourceId;
                    CharacterPoseSourceRetirementHandle sourceRelease =
                        PrepareSourceRetirement(
                            sourceId,
                            player.NodeId,
                            default);
                    m_SourceModule.ApplyRetirement(
                        in sourceRelease);
                    player.ApplyPreparedRelease(
                        in playerRelease);
                }
            }
        }

        static CharacterPresentationPoseOperation RequireBlendStackOperation(
            CharacterPoseProgramImage plan,
            int blendNodeIndex,
            PoseNodeId nodeId)
        {
            CharacterPresentationPoseOperation result = null;
            for (int i = 0; i < plan.Operations.Count; i++)
            {
                CharacterPresentationPoseOperation candidate = plan.Operations[i];
                if (candidate.Code != CharacterPoseOperationCode.BlendStack &&
                    candidate.Code != CharacterPoseOperationCode.AnimationSlot ||
                    candidate.BlendNodeIndex != blendNodeIndex || candidate.NodeId != nodeId)
                    continue;
                if (result != null)
                    throw new InvalidOperationException($"Pose Plan duplicates Blend Stack operation '{nodeId}'.");
                result = candidate;
            }
            if (result == null ||
                (result.Code ==
                     CharacterPoseOperationCode.AnimationSlot &&
                 result.ControlInputOperationIndex < 0) ||
                (result.Code ==
                     CharacterPoseOperationCode.BlendStack &&
                 (!result.PresentationPoseSourceProviderId.IsValid ||
                  !result.PresentationPoseSourceIndex.IsValid ||
                  result.ControlInputOperationIndex >= 0)))
                throw new InvalidOperationException($"Pose Plan has no valid animation transition operation '{nodeId}'.");
            return result;
        }

        static CharacterPresentationPoseOperation RequireControlInput(
            CharacterPoseProgramImage plan,
            CharacterPresentationPoseOperation operation)
        {
            int controlIndex = operation.ControlInputOperationIndex;
            if ((uint)controlIndex >= (uint)operation.Index)
                throw new InvalidOperationException(
                    $"Pose operation '{operation.NodeId}' has no compiled control input.");
            CharacterPresentationPoseOperation control =
                plan.Operations[controlIndex];
            return control;
        }

        void IPoseStateSourceSelectionSink
            .PushMotionMatchingSelection(
                PoseNodeId playerNodeId,
                in PresentationPoseSourceSample sample)
        {
            m_NodeRuntimeIndex.PushMotionMatchingSelection(
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

        bool PlayerUsesSource(PoseNodeId playerNodeId, AnimationPoseSourceId sourceId)
        {
            return m_NodeRuntimeIndex.PlayerUsesSource(
                playerNodeId,
                sourceId);
        }

        static int[] BuildPlayerLinkedPoseFragmentIndices(
            CharacterPoseProgramImage plan)
        {
            var result = CreateUnassignedOwnership(plan.PlayerCount);
            for (int operationIndex = 0;
                 operationIndex < plan.Operations.Count;
                 operationIndex++)
            {
                CharacterPresentationPoseOperation operation =
                    plan.Operations[operationIndex];
                if (operation.PlayerIndex < 0)
                    continue;
                SetLinkedPoseOwnership(
                    result,
                    operation.PlayerIndex,
                    operation.LinkedPoseFragmentIndex,
                    "Player");
            }
            RequireCompleteLinkedPoseOwnership(result, "Player");
            return result;
        }

        static int[] BuildStateMachineLinkedPoseFragmentIndices(
            CharacterPoseProgramImage plan)
        {
            var result =
                CreateUnassignedOwnership(plan.StateMachines.Count);
            for (int operationIndex = 0;
                 operationIndex < plan.Operations.Count;
                 operationIndex++)
            {
                CharacterPresentationPoseOperation operation =
                    plan.Operations[operationIndex];
                if (operation.Code !=
                    CharacterPoseOperationCode.PoseStateMachine)
                {
                    continue;
                }
                SetLinkedPoseOwnership(
                    result,
                    operation.StateMachineIndex,
                    operation.LinkedPoseFragmentIndex,
                    "StateMachine");
            }
            RequireCompleteLinkedPoseOwnership(
                result,
                "StateMachine");
            return result;
        }

        static int[] BuildRootOrientationWarpLinkedPoseFragmentIndices(
            CharacterPoseProgramImage plan)
        {
            var result =
                CreateUnassignedOwnership(
                    plan.RootOrientationWarps.Count);
            for (int operationIndex = 0;
                 operationIndex < plan.Operations.Count;
                 operationIndex++)
            {
                CharacterPresentationPoseOperation operation =
                    plan.Operations[operationIndex];
                if (operation.Code !=
                    CharacterPoseOperationCode.RootOrientationWarp)
                {
                    continue;
                }
                SetLinkedPoseOwnership(
                    result,
                    operation.RootOrientationWarpIndex,
                    operation.LinkedPoseFragmentIndex,
                    "Root Orientation Warp");
            }
            RequireCompleteLinkedPoseOwnership(
                result,
                "Root Orientation Warp");
            return result;
        }

        static int[] BuildInertializationLinkedPoseFragmentIndices(
            CharacterPoseProgramImage plan)
        {
            var result =
                CreateUnassignedOwnership(plan.Inertializations.Count);
            for (int operationIndex = 0;
                 operationIndex < plan.Operations.Count;
                 operationIndex++)
            {
                CharacterPresentationPoseOperation operation =
                    plan.Operations[operationIndex];
                if (operation.Code !=
                    CharacterPoseOperationCode.Inertialization)
                {
                    continue;
                }
                SetLinkedPoseOwnership(
                    result,
                    operation.InertializationIndex,
                    operation.LinkedPoseFragmentIndex,
                    "Inertialization");
            }
            RequireCompleteLinkedPoseOwnership(
                result,
                "Inertialization");
            return result;
        }

        static int[] CreateUnassignedOwnership(int count)
        {
            var result = new int[count];
            for (int i = 0; i < result.Length; i++)
                result[i] = int.MinValue;
            return result;
        }

        static void SetLinkedPoseOwnership(
            int[] ownership,
            int index,
            int fragmentIndex,
            string kind)
        {
            if ((uint)index >= (uint)ownership.Length ||
                ownership[index] != int.MinValue)
            {
                throw new InvalidOperationException(
                    $"{kind} #{index} has invalid Linked Pose ownership.");
            }
            ownership[index] = fragmentIndex;
        }

        static void RequireCompleteLinkedPoseOwnership(
            int[] ownership,
            string kind)
        {
            for (int i = 0; i < ownership.Length; i++)
            {
                if (ownership[i] == int.MinValue)
                {
                    throw new InvalidOperationException(
                        $"{kind} #{i} has no compiled Linked Pose ownership.");
                }
            }
        }

        static int CalculateSourceCapacity(
            CharacterPoseProgramImage plan)
        {
            int capacity = 0;
            for (int i = 0; i < plan.Operations.Count; i++)
            {
                CharacterPresentationPoseOperation operation =
                    plan.Operations[i];
                switch (operation.Code)
                {
                    case CharacterPoseOperationCode.SelectedPosePlayer:
                    case CharacterPoseOperationCode.BlendSpacePlayer:
                        capacity = checked(capacity + 1);
                        break;
                    case CharacterPoseOperationCode.BlendStack:
                    case CharacterPoseOperationCode.AnimationSlot:
                        AnimationBlendNodePayload blendNode =
                            plan.RequireBlendNode(operation.NodeId);
                        if (blendNode.StackPolicy == null ||
                            blendNode.StackPolicy.MaxActiveSourceEntries <= 0)
                        {
                            throw new InvalidOperationException(
                                $"Pose Player '{operation.NodeId}' has no source capacity.");
                        }
                        capacity = checked(
                            capacity +
                            blendNode.StackPolicy.MaxActiveSourceEntries +
                            1);
                        break;
                }
            }
            for (int i = 0; i < plan.MotionMatchingNodes.Count; i++)
                capacity = checked(capacity + plan.MotionMatchingNodes[i].LiveEntryCapacity);
            return capacity > 0
                ? capacity
                : throw new InvalidOperationException(
                    "Pose Plan has no source capacity.");
        }

        void RequireAlive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(PosePlanExecutionRuntime));
        }
    }
}
