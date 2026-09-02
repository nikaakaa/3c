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
using Unity.Profiling;

namespace ThirdPersonCharacter.Pipeline.Animation.Presentation
{
    internal sealed class PosePlanExecutionRuntime : IDisposable
    {
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
        readonly AnimancerComponent m_Animancer;
        readonly CharacterPresentationProjection m_Projection;
        readonly CharacterPoseProgramRuntime m_ProgramRuntime;
        readonly CharacterPoseConstraintRuntime m_PoseConstraints;
        readonly CharacterPoseSourceModule m_SourceModule;
        readonly CharacterFinalPosePublication m_FinalPublication;
        readonly AnimationPresentationRuntimeSnapshotPublisher m_DiagnosticsPublisher;
        readonly CharacterFootIkCommittedCaptureViewProjector
            m_FootIkDiagnosticsProjector =
                new CharacterFootIkCommittedCaptureViewProjector();
        readonly CharacterPoseActorCommittedDiagnosticsProjector
            m_ActorDiagnosticsProjector;
        readonly CharacterPoseProgramCommittedDiagnosticsProjector
            m_ProgramDiagnosticsProjector;
        readonly bool m_ManagesGraphClock;

        PoseInertializationNativeProgram m_InertializationPlan =>
            m_ProgramRuntime.Inertialization;
        AnimationBlendStackRuntime[] m_Stacks => m_ProgramRuntime.Stacks;
        CharacterAnimationTransitionRouteRuntime[] m_StackRoutes =>
            m_ProgramRuntime.Routes;
        PoseStateAndSourceRuntime m_PoseStateSources =>
            m_ProgramRuntime.PoseStateSources;
        RootOrientationWarpRuntime[] m_RootOrientationWarps =>
            m_ProgramRuntime.RootOrientationWarps;

        ulong m_CompletionIdentity = 1;
        ulong m_FrameCompletionContext;
        CharacterPoseProgramFrameLease m_ActiveFrameLease;
        CharacterPoseTuningSnapshot m_TuningSnapshot;
        bool m_CommitValidated;
        bool m_HasOpenFrame;
        AnimationPresentationFrameOutcome m_PendingFrameOutcome;
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
            var linkedFragments = new CharacterPoseLinkedFragmentState(
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
                        linkedFragments);
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
                    rootHierarchy,
                    sourceModule);
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
                linkedFragments,
                actionPlayback,
                animationSlots,
                sourceModule.Capacity);
            m_ProgramRuntime = new CharacterPoseProgramRuntime(
                animancer,
                projection.PosePlan,
                executionView,
                actorState,
                programFrames,
                programTuning,
                sourceModule,
                new CharacterPoseWorldContextAdapter(
                    projection,
                    sourceModule,
                    finalPublication),
                poseConstraints);
            m_TuningSnapshot = CaptureTuningSnapshot(1);
            m_FinalPublication = finalPublication;
            m_DiagnosticsPublisher = diagnosticsPublisher;
            m_ActorDiagnosticsProjector = actorDiagnosticsProjector;
            m_ProgramDiagnosticsProjector =
                programDiagnosticsProjector;
            m_ManagesGraphClock = managesGraphClock;
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
            string programError = m_ProgramRuntime.PrepareTuningCandidate(
                layout,
                block,
                candidateGeneration);
            if (!string.IsNullOrEmpty(programError))
            {
                m_SourceModule.DiscardTuningCandidate();
                return programError;
            }
            string constraintError = m_PoseConstraints.PrepareTuningCandidate(
                layout,
                block,
                candidateGeneration,
                resetOwnerState);
            if (!string.IsNullOrEmpty(constraintError))
            {
                m_ProgramRuntime.DiscardTuningCandidate();
                m_SourceModule.DiscardTuningCandidate();
                return constraintError;
            }
            m_ProgramRuntime.CommitTuningCandidate(candidateGeneration);
            m_SourceModule.CommitTuningCandidate(candidateGeneration);
            m_PoseConstraints.CommitTuningCandidate(candidateGeneration);
            m_TuningSnapshot = CaptureTuningSnapshot(candidateGeneration);
            return string.Empty;
        }

        CharacterPoseTuningSnapshot CaptureTuningSnapshot(
            ulong generation)
        {
            CharacterPoseProgramTuningView program =
                m_ProgramRuntime.RequireTuning(generation);
            CharacterPoseSourceTuningView source =
                m_SourceModule.RequireTuning(generation);
            CharacterPoseConstraintTuningView constraint =
                m_PoseConstraints.RequireTuning(generation);
            return new CharacterPoseTuningSnapshot(
                in program,
                in source,
                in constraint);
        }

        internal bool CanApplyNextActivation =>
            m_ProgramRuntime.CanApplyNextActivation;
        internal AnimationPresentationRuntimeCapacityMetrics
            CreateCapacityMetrics(
                int actionJournalCapacity,
                int samplingJournalCapacity,
                int slotJournalCapacity) =>
            new AnimationPresentationRuntimeCapacityMetrics(
                m_ProgramRuntime.DenseDoublePageResidentPayloadBytes,
                PoseInertializationNativeProgramPayloadMetrics
                    .CalculateDoublePageResidentPayloadBytes(
                        m_InertializationPlan),
                m_FinalPublication
                    .DenseDoublePageResidentPayloadBytes,
                actionJournalCapacity,
                samplingJournalCapacity,
                slotJournalCapacity,
                m_ProgramRuntime.SourceRetirementStandaloneCapacity,
                m_SourceModule.Capacity,
                m_ProgramRuntime.SourceRetirementStandaloneCapacity);

        internal void RecordNoDiagnosticsInterest() =>
            m_DiagnosticsPublisher.RecordNoInterestSkip();
        internal ulong FrameCompletionContext =>
            m_FrameCompletionContext;

        internal void BeginActionPlaybackFrame(
            ulong frameIdentity,
            ulong presentationFrame)
        {
            RequireAlive();
            m_ProgramRuntime.BeginActionPlaybackFrame(
                frameIdentity,
                presentationFrame);
        }

        internal void BeginAnimationSlotFrame(ulong frameIdentity)
        {
            RequireAlive();
            m_ProgramRuntime.BeginAnimationSlotFrame(frameIdentity);
        }

        internal void CommitAnimationSlotFrame(
            CharacterPoseProgramFrameLease lease)
        {
            RequireMutation(lease);
            m_ProgramRuntime.CommitAnimationSlotFrame(lease);
        }

        internal void CommitActionPlaybackFrame(
            CharacterPoseProgramFrameLease lease)
        {
            RequireMutation(lease);
            m_ProgramRuntime.CommitActionPlaybackFrame(lease);
        }

        internal void DiscardAnimationSlotFrame(ulong frameIdentity)
        {
            RequireAlive();
            m_ProgramRuntime.DiscardAnimationSlotFrame(frameIdentity);
        }

        internal void DiscardActionPlaybackFrame(ulong frameIdentity)
        {
            RequireAlive();
            m_ProgramRuntime.DiscardActionPlaybackFrame(frameIdentity);
        }

        internal void PublishActionCommand(
            in ActionAnimationPlaybackCommand command)
        {
            RequireAlive();
            RequireNoOpenMutation();
            m_ProgramRuntime.PublishActionCommand(in command);
        }

        internal void RetireActionCommand(
            in ActionAnimationPlaybackCommand command)
        {
            RequireAlive();
            RequireNoOpenMutation();
            m_ProgramRuntime.RetireActionCommand(in command);
        }

        internal void ReplaceActionCommand(
            EventId targetEventId,
            in ActionAnimationPlaybackCommand replacement)
        {
            RequireAlive();
            RequireNoOpenMutation();
            m_ProgramRuntime.ReplaceActionCommand(
                targetEventId,
                in replacement);
        }

        internal IReadOnlyList<ActionAnimationPlaybackLifecycleFrame>
            PrepareActionLifecycleFrame(
                CharacterPoseProgramFrameLease lease,
                PresentationFrameWorkspace workspace,
                PresentationFrameWorkspaceLease workspaceLease)
        {
            RequireMutation(lease);
            return m_ProgramRuntime.PrepareActionLifecycleFrame(
                lease,
                workspace,
                workspaceLease);
        }

        internal void ProjectActionPresentationSamples(
            CharacterPoseProgramFrameLease lease,
            ActionPresentationSamplingRuntime sampling,
            ActionPresentationSamplingFrameTransaction samplingTransaction,
            IReadOnlyList<ActionAnimationPlaybackLifecycleFrame> lifecycle,
            double presentationSampleTick,
            float presentationDeltaSeconds)
        {
            RequireMutation(lease);
            m_ProgramRuntime.ProjectActionPresentationSamples(
                lease,
                sampling,
                samplingTransaction,
                lifecycle,
                presentationSampleTick,
                presentationDeltaSeconds);
        }

        internal void PublishActionSources(
            CharacterPoseProgramFrameLease lease,
            PresentationFrameWorkspace workspace,
            PresentationFrameWorkspaceLease workspaceLease,
            IDictionary<AnimationPlayerSourceSampleKey,
                AnimationResolvedPoseSourceSample> sourceSamples)
        {
            RequireMutation(lease);
            m_ProgramRuntime.PublishActionSources(
                lease,
                workspace,
                workspaceLease,
                sourceSamples);
        }

        internal void CompleteActionReleaseProtocol(
            CharacterPoseProgramFrameLease lease,
            PresentationFrameWorkspace workspace,
            PresentationFrameWorkspaceLease workspaceLease)
        {
            RequireMutation(lease);
            m_ProgramRuntime.CompleteActionReleaseProtocol(
                lease,
                workspace,
                workspaceLease);
        }

        internal void ValidateActionFrame(
            CharacterPoseProgramFrameLease lease)
        {
            RequireMutation(lease);
            m_ProgramRuntime.ValidateActionFrame(lease);
        }

        internal IReadOnlyList<ActionAnimationPlaybackLifecycleSnapshot>
            BuildCommittedActionLifecycleSnapshot()
        {
            RequireAlive();
            RequireNoOpenMutation();
            return m_ProgramRuntime
                .BuildCommittedActionLifecycleSnapshot();
        }

        internal IReadOnlyList<AnimationPlaybackId>
            RetiredActionPlaybacks =>
            m_ProgramRuntime.RetiredActionPlaybacks;

        internal void ResetAnimationSlots()
        {
            RequireAlive();
            RequireNoOpenMutation();
            m_ProgramRuntime.ResetAnimationSlots();
        }

        internal void ResetActionPlayback()
        {
            RequireAlive();
            RequireNoOpenMutation();
            m_ProgramRuntime.ResetActionPlayback();
        }

        internal void CopySourceSyncSnapshots(
            List<PoseStateSourceSyncSnapshot>
                destination)
        {
            RequireAlive();
            RequireNoOpenMutation();
            m_ProgramRuntime.CopySourceSyncSnapshots(destination);
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
            if (m_ProgramRuntime.HasPreparedEvaluation)
            {
                throw new InvalidOperationException(
                    "Pose Program prepared state from the previous frame was not consumed.");
            }
            if (m_CommitValidated)
            {
                throw new InvalidOperationException(
                    "Pose Plan committed-frame finalization is still pending.");
            }
            if (m_ProgramRuntime.HasPreparedStandaloneSourceRetirement)
            {
                throw new InvalidOperationException(
                    "Pose Plan standalone source releases from the committed frame were not finalized.");
            }
            if (m_SourceModule.ReleaseAcknowledgementsValidated)
            {
                throw new InvalidOperationException(
                    "Pose Plan action backend acknowledgements from the previous frame were not applied.");
            }
            if (m_ProgramRuntime.HasPreparedMotionMatchingPoseCompletion)
            {
                throw new InvalidOperationException(
                    "Pose Plan Motion Matching completion from the previous frame was not consumed.");
            }
            bool poseConstraintsOpen = false;
            bool sourceOpen = false;
            bool publicationOpen = false;
            try
            {
                m_TuningSnapshot.RequireGeneration(
                    lineage.TuningGeneration);
                constraintLease = m_PoseConstraints.BeginFrame(
                    in lineage,
                    diagnosticsInterest,
                    footIkCaptureInterest);
                poseConstraintsOpen = true;
                m_ProgramRuntime.BeginSourceRetirementFrame();
                sourceLease = m_SourceModule.BeginFrame(in lineage);
                sourceOpen = true;
                publicationLease = m_FinalPublication.BeginFrame(
                    in lineage,
                    footIkCaptureInterest);
                publicationOpen = true;
                m_PendingFrameOutcome = AnimationPresentationFrameOutcome.None;
                m_ProgramRuntime.BeginFrame(programLease);
                m_ActorDiagnosticsProjector.BeginFrame();
                m_ProgramRuntime.BeginActorStateFrame(
                    programLease,
                    linkedPose,
                    m_Projection.LinkedPose.Groups,
                    m_CompletionIdentity);
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
                if (m_ProgramRuntime.HasOpenFrame)
                    m_ProgramRuntime.DiscardFrame(programLease);
                if (sourceOpen)
                    m_SourceModule.DiscardFrame(sourceLease);
                m_ProgramRuntime.CompleteSourceRetirementFrame();
                m_ProgramRuntime.ClearLinkedPoseFrameSelection();
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
                !m_ProgramRuntime.HasPendingCompletedEvaluationFrame ||
                m_ProgramRuntime.PendingCompletedEvaluationCompletionIdentity == 0)
            {
                throw new InvalidOperationException(
                    "Pose Plan frame has no completed Native page to commit.");
            }
            m_ProgramRuntime.CommitEvaluationFrame(
                lease,
                m_ProgramRuntime.PendingCompletedEvaluationCompletionIdentity);
            m_ProgramRuntime.CommitFrame(lease);
            m_SourceModule.CommitFrame(sourceLease);
            m_ProgramRuntime.CommitActorStateFrame(lease);
            m_PoseConstraints.SealFrame(constraintLease);
            m_HasOpenFrame = false;
            m_ActiveFrameLease = default;
            m_ProgramRuntime.CompleteSourceRetirementFrame();
            m_ProgramRuntime.ClearLinkedPoseFrameSelection();
        }

        internal void ValidatePendingSeal(
            CharacterPoseProgramFrameLease lease,
            CharacterPoseSourceFrameLease sourceLease)
        {
            RequireMutation(lease);
            m_ProgramRuntime.ValidateSourceRetirements(
                lease,
                sourceLease);
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
            DiscardStep(
                () => m_ProgramRuntime.DiscardActorNodeFrames(lease),
                ref failure);
            m_SourceModule.ClearActionSlotReleaseCompletions();
            m_SourceModule.CancelReleaseDiagnostics();
            m_SourceModule.ClearUsage();
            DiscardStep(
                () => m_ProgramRuntime
                    .DiscardRootOrientationWarpFrames(lease),
                ref failure);
            if (m_ProgramRuntime.HasOpenFrame)
            {
                DiscardStep(
                    () => m_ProgramRuntime.DiscardFrame(lease),
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
                m_ProgramRuntime.DiscardSourceRetirementFrame,
                ref failure);
            m_ProgramRuntime.ClearStandaloneSourceRetirements();
            ClearValidatedActionBackendAcknowledgements();
            m_ProgramRuntime.ClearMotionMatchingPoseCompletion();
            m_ProgramRuntime.ClearSourceDemand();
            m_HasOpenFrame = false;
            m_ActiveFrameLease = default;
            m_CommitValidated = false;
            m_PendingFrameOutcome = AnimationPresentationFrameOutcome.None;
            m_SourceModule.CancelReleaseDiagnostics();
            m_ProgramRuntime.ClearLinkedPoseFrameSelection();
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
            m_ProgramRuntime.FinalizeCommittedSourceRetirements(
                m_CompletionIdentity);
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
            return m_ProgramRuntime.BuildMotionMatchingDemandBatch(
                    m_ActiveFrameLease,
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
            m_ProgramRuntime.ApplyMotionMatchingSelections(
                m_ActiveFrameLease,
                in resolution,
                sourceSamples,
                workspace,
                lease);
        }

        internal void PrepareMotionMatchingPosePlanCompletion(
            in MotionMatchingFrameResolution resolution,
            ulong poseCompletionIdentity)
        {
            RequireAlive();
            RequireOpenMutation();
            m_ProgramRuntime.PrepareMotionMatchingPosePlanCompletion(
                m_ActiveFrameLease,
                in resolution,
                poseCompletionIdentity);
        }

        internal MotionMatchingPosePlanCompletion
            BuildMotionMatchingPosePlanCompletion()
        {
            RequireAlive();
            RequireOpenMutation();
            if (m_PendingFrameOutcome !=
                AnimationPresentationFrameOutcome.Committed)
            {
                throw new InvalidOperationException(
                    "Motion Matching Pose Plan completion does not match the evaluated frame.");
            }
            return m_ProgramRuntime.BuildMotionMatchingPosePlanCompletion(
                m_ActiveFrameLease);
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
                !m_ProgramRuntime.HasCommittedEvaluationFrame ||
                m_ProgramRuntime.CommittedEvaluationCompletionIdentity == 0 ||
                executionResult.Lineage.CompletionIdentity !=
                m_ProgramRuntime.CommittedEvaluationCompletionIdentity)
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
                     m_ProgramRuntime.CommittedEvaluationCompletionIdentity) ||
                    requiresSolver &&
                    solverDiagnostics.OutputCompletionIdentity !=
                    m_ProgramRuntime.CommittedEvaluationCompletionIdentity ||
                    requiresPhysical &&
                    (!physicalWrite.IsAvailable ||
                     physicalWrite.CompletionIdentity !=
                     m_ProgramRuntime.CommittedEvaluationCompletionIdentity))
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
                            m_ProgramRuntime.CaptureCommittedDiagnostics(
                                m_ProgramDiagnosticsProjector,
                                in committedProgramResult,
                                in publicationDiagnostics,
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

        internal void ApplyValidatedActionBackendReleaseCompletionAcknowledgements()
        {
            RequireAlive();
            m_SourceModule.ApplyActionBackendReleaseAcknowledgements();
        }

        internal void ExecutePreparedActionBackendReleaseRequests()
        {
            RequireAlive();
            m_ProgramRuntime.ExecutePreparedActionBackendReleaseRequests();
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
            CharacterPoseSourceTuningView sourceTuning =
                m_SourceModule.RequireTuning(
                    m_ActiveFrameLease.Lineage.TuningGeneration);
            m_ProgramRuntime.Advance(
                m_ActiveFrameLease,
                presentationDeltaSeconds,
                in factFrame,
                in parameterFrame,
                in sourceTuning);
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
            m_ProgramRuntime.FinalizePoseStateFrame(
                m_ActiveFrameLease,
                in factFrame,
                workspace,
                lease);
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
                m_ProgramRuntime.BeginSourceDemand(
                    programLease,
                    completionIdentity);
            var demand = new CharacterPoseSourceDemand(
                in lineage,
                in preparations,
                providerDemands,
                actionSourceCount,
                providerSourceCount);
            m_ProgramRuntime.BindSourceDemand(
                programLease,
                in demand);
            m_SourceModule.BindDemand(sourceLease, in demand);
            return demand;
        }

        internal CharacterPoseProgramPrepared PrepareEvaluation(
            CharacterPoseSourceFrameLease sourceLease,
            CharacterFinalPosePublicationFrameLease publicationLease,
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
                m_ProgramRuntime.RequireSourceDemand(
                    m_ActiveFrameLease,
                    in sourceDemand);
            CharacterPoseSourcePreparationView sourcePreparations =
                sourceDemand.Preparations;
            CharacterPoseSourcePreparationView pendingPreparations =
                pendingDemand.Preparations;
            if (m_ProgramRuntime.HasPreparedEvaluation)
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
            using (PrepareMarker.Auto())
            {
                m_SourceModule.BeginReleaseDiagnostics(
                    recordDiagnostics);
                m_SourceModule.ClearActionSlotReleaseCompletions();
                using (PrepareWorkspaceMarker.Auto())
                {
                    m_ProgramRuntime.BeginSourceEvaluation(
                        m_ActiveFrameLease,
                        completionIdentity);
                }
                using (PrepareStackMarker.Auto())
                {
                    m_ProgramRuntime.PrepareStackSources(
                        m_ActiveFrameLease,
                        sourceLease,
                        in sourcePreparations,
                        presentationDeltaSeconds,
                        actionSourceSamples,
                        providerSourceSamples);
                }
                using (PrepareDirectMarker.Auto())
                {
                    m_ProgramRuntime.PrepareDirectSources(
                        m_ActiveFrameLease,
                        sourceLease,
                        in sourcePreparations,
                        presentationDeltaSeconds,
                        providerSourceSamples);
                }
                using (PrepareSequenceMarker.Auto())
                {
                    CharacterPoseSourceTuningView sourceTuning =
                        m_SourceModule.RequireTuning(
                            sourceDemand.Lineage.TuningGeneration);
                    m_ProgramRuntime.PrepareSequenceSources(
                        m_ActiveFrameLease,
                        sourceLease,
                        in sourcePreparations,
                        presentationDeltaSeconds,
                        in sourceTuning);
                }
                using (PrepareBlendSpaceMarker.Auto())
                {
                    m_ProgramRuntime.PrepareBlendSpaceSources(
                        m_ActiveFrameLease,
                        sourceLease,
                        in sourcePreparations,
                        presentationDeltaSeconds);
                }
            }

            CharacterPoseSourcePreparedResources preparedSources =
                m_SourceModule.RequirePreparedResources(sourceLease);
            CharacterFinalPosePublicationOutputBinding finalOutput;
            using (ValidateMarker.Auto())
            {
                m_ProgramRuntime.PrepareEvaluationJobsAndRetirements(
                    m_ActiveFrameLease,
                    in preparedSources,
                    completionIdentity);
                finalOutput =
                    m_FinalPublication.BindProgramOutput(
                        publicationLease);
                m_ProgramRuntime.BindEvaluationExecution(
                    m_ActiveFrameLease,
                    sourceDemand.Lineage,
                    in finalOutput,
                    recordDiagnostics);
                m_FinalPublication.ValidateWriterBeforeEvaluate(
                    in finalOutput);
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
            m_ProgramRuntime.PrepareEvaluation(
                m_ActiveFrameLease,
                in prepared,
                presentationDeltaSeconds);
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
                !m_ProgramRuntime.HasPendingEvaluationFrame ||
                m_ProgramRuntime.PendingEvaluationCompletionIdentity !=
                    prepared.Lineage.CompletionIdentity)
            {
                throw new ArgumentException(
                    "Pose Plan prepared evaluation is not the active Pending frame.",
                    nameof(prepared));
            }
            if (enterEvaluateBarrier == null)
                throw new ArgumentNullException(nameof(enterEvaluateBarrier));

            ulong completionIdentity = prepared.Lineage.CompletionIdentity;
            float presentationDeltaSeconds =
                m_ProgramRuntime.RequirePreparedEvaluationDeltaSeconds(
                    m_ActiveFrameLease,
                    in prepared);

            enterEvaluateBarrier();
            m_SourceModule.EnterEvaluateBarrier(
                sourceLease);
            using (GraphEvaluateMarker.Auto())
                m_Animancer.Evaluate(presentationDeltaSeconds);
            CharacterPoseProgramOutputResult programOutput;
            using (PoseGraphExecuteMarker.Auto())
            {
                programOutput = m_ProgramRuntime.CompleteEvaluation(
                    m_ActiveFrameLease,
                    in prepared,
                    in bodyFrame,
                    in factFrame);
            }
            CharacterPoseFrameLineage completedLineage =
                prepared.Lineage;
            CharacterPoseProgramResult programResult =
                CreateProgramResult(
                    in completedLineage,
                    in programOutput);
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
                    in programOutput);
                m_PendingFrameOutcome = publicationResult.Outcome;
                m_FinalPublication.WritePhysicalPose(
                    publicationLease);
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
                m_ProgramRuntime.CompleteNodeEvaluation(
                    m_ActiveFrameLease,
                    completionIdentity);
            }
            return executionResult;
        }

        CharacterPoseProgramResult CreateProgramResult(
            in CharacterPoseFrameLineage lineage,
            in CharacterPoseProgramOutputResult output)
        {
            if (!output.IsValid || output.Lineage != lineage)
            {
                throw new InvalidOperationException(
                    "Pose Program output result is inconsistent.");
            }
            bool completed =
                output.Availability == AnimationPoseAvailability.Pose &&
                output.OutputInvalidReason ==
                    AnimationPoseNativeInvalidReason.None &&
                output.GraphInvalidReason ==
                    AnimationPoseNativeInvalidReason.None &&
                output.InvalidOperationIndex == -1;
            return new CharacterPoseProgramResult(
                in lineage,
                completed
                    ? AnimationPresentationFrameOutcome.Committed
                    : AnimationPresentationFrameOutcome.TypedInvalid,
                output.Availability,
                output.OutputInvalidReason,
                output.GraphInvalidReason,
                output.InvalidOperationIndex);
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
            m_ProgramRuntime.ClearSourceRetirements();
            ClearValidatedActionBackendAcknowledgements();
            m_ProgramRuntime.ClearMotionMatchingPoseCompletion();
            m_SourceModule.ClearActionBackendReleaseCompletions();
            m_ProgramRuntime.BeginReset();
            m_PendingFrameOutcome = AnimationPresentationFrameOutcome.None;
            m_PoseConstraints.ResetSolvers();
            ulong completionIdentity = NextCompletionIdentity();
            m_ProgramRuntime.ResetBlendState(completionIdentity);
            m_ProgramRuntime.ReleaseCompletedSources(completionIdentity);
            m_ProgramRuntime.ResetPoseState(reason);
            m_ProgramRuntime.ReleasePlayerSources();
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
            m_ProgramRuntime.ClearSourceDemand();
            m_PendingFrameOutcome = AnimationPresentationFrameOutcome.None;
            Exception failure = null;
            DisposeStep(m_DiagnosticsPublisher.Dispose, ref failure);
            DisposeStep(
                m_ProgramRuntime.DetachExecutionJobs,
                ref failure);
            DisposeStep(m_SourceModule.Dispose, ref failure);
            DisposeStep(m_ProgramRuntime.Dispose, ref failure);
            DisposeStep(RestoreGraphClock, ref failure);
            if (failure != null)
                throw failure;
        }

        internal void SetSequencePreview(
            PresentationPoseSourceIndex sourceIndex,
            double sampleTime,
            bool resetContinuity)
        {
            RequireAlive();
            RequireNoOpenMutation();
            m_ProgramRuntime.SetSequencePreview(
                sourceIndex,
                sampleTime,
                resetContinuity);
        }

        internal void ClearSequencePreview()
        {
            RequireAlive();
            RequireNoOpenMutation();
            m_ProgramRuntime.ClearSequencePreview();
        }

        ulong NextCompletionIdentity()
        {
            if (m_CompletionIdentity == ulong.MaxValue)
                throw new InvalidOperationException("Animation Pose completion identity was exhausted.");
            m_CompletionIdentity++;
            return m_CompletionIdentity;
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
