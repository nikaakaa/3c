using System;
using System.Collections.Generic;
using System.Linq;
using Animancer;
using ThirdPersonCharacter.Animation.TransitionRouting;
using ThirdPersonCharacter.Pipeline.Animation.BlendStack;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;
using ThirdPersonCharacter.Pipeline.Animation.Sources;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonCharacter.Pipeline.Presentation.Animancer;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation.Presentation
{
    internal sealed class CharacterPoseRuntimeComposition : IDisposable
    {
        readonly AnimancerComponent m_Animancer;
        readonly bool m_ManagesGraphClock;
        bool m_Disposed;

        internal CharacterPoseRuntimeComposition(
            AnimancerComponent animancer,
            CharacterPoseProgramRuntime program,
            CharacterPoseSourceModule source,
            CharacterPoseConstraintRuntime constraints,
            CharacterFinalPosePublication publication,
            CharacterPoseDiagnosticsRuntime diagnostics,
            bool managesGraphClock)
        {
            m_Animancer = animancer ? animancer :
                throw new ArgumentNullException(nameof(animancer));
            Program = program ?? throw new ArgumentNullException(nameof(program));
            Source = source ?? throw new ArgumentNullException(nameof(source));
            Constraints = constraints ?? throw new ArgumentNullException(nameof(constraints));
            Publication = publication ?? throw new ArgumentNullException(nameof(publication));
            Diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
            m_ManagesGraphClock = managesGraphClock;
        }

        internal CharacterPoseProgramRuntime Program { get; }
        internal CharacterPoseSourceModule Source { get; }
        internal CharacterPoseConstraintRuntime Constraints { get; }
        internal CharacterFinalPosePublication Publication { get; }
        internal CharacterPoseDiagnosticsRuntime Diagnostics { get; }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            Publication.Invalidate();
            Program.ClearSourceDemand();
            Exception failure = null;
            DisposeStep(Diagnostics.Dispose, ref failure);
            DisposeStep(Program.DetachExecutionJobs, ref failure);
            DisposeStep(Source.Dispose, ref failure);
            DisposeStep(Program.Dispose, ref failure);
            DisposeStep(RestoreGraphClock, ref failure);
            DisposeStep(Constraints.Dispose, ref failure);
            if (failure != null)
                throw failure;
        }

        void RestoreGraphClock()
        {
            if (m_ManagesGraphClock &&
                m_Animancer &&
                m_Animancer.IsGraphInitialized)
            {
                m_Animancer.Graph.UnpauseGraph();
            }
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
    }

    internal static class CharacterPoseRuntimeCompositionFactory
    {
        internal static CharacterPoseRuntimeComposition Create(
            AnimancerComponent animancer,
            CharacterAnimationRigBinding rigBinding,
            CharacterRootHierarchyBinding rootHierarchy,
            CharacterPresentationProjection projection,
            CharacterActionPlaybackRuntime actionPlayback,
            CharacterMotionMatchingPresentationModule motionMatching,
            AnimationSlotRuntime animationSlots,
            PresentationFrameWorkspace presentationWorkspace,
            CharacterFootPlacementModule footPlacement,
            bool managesGraphClock,
            ulong initialCompletionIdentity,
            ICharacterPoseCommittedDiagnosticsEventSink diagnosticsEventSink)
        {
            if (!animancer)
                throw new ArgumentNullException(nameof(animancer));
            if (projection == null)
                throw new ArgumentNullException(nameof(projection));
            Animator animator = animancer.Animator;
            if (!animator ||
                animator.cullingMode != AnimatorCullingMode.AlwaysAnimate)
            {
                throw new InvalidOperationException(
                    "Animation Presentation requires an AlwaysAnimate Animator because Pose jobs produce the frame transaction payload.");
            }
            projection.RequirePosePayload();
            var linkedFragments = new CharacterPoseLinkedFragmentState(
                projection.PosePlan);
            int sourceCapacity = CalculateSourceCapacity(
                projection.PosePlan);
            int physicalSourceCapacity = checked(
                sourceCapacity + projection.PosePlan.ClipPlayers.Count);
            int clipCatalogCapacity =
                AnimationPoseRequestWorkspaceLayoutFactory
                    .RequireClipCatalogCapacity(projection);
            AnimationPoseNativeWorkspace workspace = null;
            CharacterPoseProgramFramePages programFrames = null;
            CharacterPoseProgramTuningState programTuning = null;
            CharacterPoseProgramExecutionView executionView = null;
            CharacterPoseConstraintRuntime constraints = null;
            PoseInertializationNativeProgram inertialization = null;
            CharacterPoseSourceModule source = null;
            AnimationBlendStackRuntime[] stacks = null;
            CharacterAnimationTransitionRouteRuntime[] routes = null;
            AnimationSelectedPosePlayerRuntime[] directPlayers = null;
            AnimationClipPlayerRuntime[] clipPlayers = null;
            AnimationBlendSpacePlayerRuntime[] blendSpacePlayers = null;
            PoseStateAndSourceRuntime poseStateSources = null;
            CharacterPoseDiagnosticsRuntime diagnostics = null;
            CharacterFinalPosePublication publication = null;
            CharacterPoseActorState actorState = null;
            CharacterPoseProgramRuntime program = null;
            bool graphPaused = false;
            var nodeRuntimeIndex =
                new CharacterPoseProgramNodeRuntimeIndex();
            try
            {
                workspace = new AnimationPoseNativeWorkspace(projection);
                CharacterPoseGraphNativeBinding initialFrame =
                    workspace.BeginFrame(initialCompletionIdentity);
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
                programTuning = new CharacterPoseProgramTuningState(
                    projection,
                    executionView.Operations,
                    1);
                if (projection.PosePlan.FullBodyIks.Count != 1)
                {
                    throw new InvalidOperationException(
                        "Pose Plan requires exactly one Full Body IK descriptor.");
                }
                CharacterPresentationFullBodyIkDescriptor fullBodyIk =
                    projection.PosePlan.FullBodyIks[0];
                fullBodyIk.RequireValid();
                var fullBodyIkSolver = new CharacterFinalIkFullBodySolver(
                    projection.Rig,
                    fullBodyIk.Profile,
                    executionView.ParentIndices,
                    executionView.VirtualBones);
                inertialization = new PoseInertializationNativeProgram(
                    projection.PosePlan,
                    projection.BlendCurveCatalog,
                    projection.BlendProfileCatalog);
                stacks = new AnimationBlendStackRuntime[
                    projection.PosePlan.BlendNodes.Count];
                routes = new CharacterAnimationTransitionRouteRuntime[
                    stacks.Length];
                Dictionary<PoseNodeId, CharacterAnimationSlotDescriptor>
                    slotsByNode = projection.PosePlan.AnimationSlots
                        .ToDictionary(value => value.NodeId);
                for (int stackIndex = 0;
                     stackIndex < stacks.Length;
                     stackIndex++)
                {
                    AnimationBlendNodePayload blendNode =
                        projection.PosePlan.BlendNodes[stackIndex] ??
                        throw new InvalidOperationException(
                            $"Pose Plan Blend Stack #{stackIndex} is missing.");
                    CharacterPoseOperationHeader operation =
                        RequireBlendStackOperation(
                            projection.PosePlan,
                            stackIndex,
                            blendNode.NodeId);
                    slotsByNode.TryGetValue(
                        blendNode.NodeId,
                        out CharacterAnimationSlotDescriptor slotDescriptor);
                    var route =
                        new CharacterAnimationTransitionRouteRuntime(
                            blendNode,
                            slotDescriptor);
                    CharacterPoseOperationHeader input =
                        route.IsAnimationSlot
                            ? RequireControlInput(
                                projection.PosePlan,
                                operation)
                            : null;
                    if (!route.IsAnimationSlot &&
                        (!RequireSourceProviderId(
                             projection.PosePlan,
                             operation).IsValid ||
                         !RequireSourceIndex(
                             projection.PosePlan,
                             operation).IsValid))
                    {
                        throw new InvalidOperationException(
                            $"Pose State Blend Stack '{operation.NodeId}' has no compiled provider identity.");
                    }
                    int playerIndex = RequirePlayerIndex(
                        projection.PosePlan,
                        operation);
                    CharacterPoseActionInputOperationPayload actionInput =
                        route.IsAnimationSlot
                            ? (CharacterPoseActionInputOperationPayload)
                                projection.PosePlan.OperationPages
                                    .RequirePayload(input)
                            : null;
                    AnimationPlayerPoseNativeWriteBinding initialWrite =
                        programFrames.RequirePlayerWriteBinding(
                            playerIndex,
                            initialFrame.CompletionIdentity);
                    var stack = new AnimationBlendStackRuntime(
                        blendNode,
                        route.IsAnimationSlot
                            ? actionInput.AnimationChannelId
                            : default,
                        route.IsAnimationSlot
                            ? default
                            : RequireSourceProviderId(
                                projection.PosePlan,
                                operation),
                        route.IsAnimationSlot
                            ? default
                            : RequireSourceIndex(
                                projection.PosePlan,
                                operation),
                        route.IsAnimationSlot
                            ? actionInput.SelectionAvailability
                            : ((CharacterPoseBlendOperationPayload)
                                projection.PosePlan.OperationPages
                                    .RequirePayload(operation))
                            .SelectionAvailability,
                        projection.BlendCurveCatalog,
                        projection.BlendProfileCatalog,
                        projection.Rig,
                        in initialWrite);
                    stacks[stackIndex] = stack;
                    routes[stackIndex] = route;
                    nodeRuntimeIndex.AddStack(
                        blendNode.NodeId,
                        stack,
                        route,
                        playerIndex,
                        route.IsAnimationSlot ? -1 : 0);
                    if (route.IsAnimationSlot)
                    {
                        CharacterAnimationSlotNativeControl control =
                            route.NativeControl;
                        programFrames.SetAnimationSlotControl(
                            route.AnimationSlotIndex,
                            in control);
                    }
                }
                var directPlayerList =
                    new List<AnimationSelectedPosePlayerRuntime>();
                for (int operationIndex = 0;
                     operationIndex < projection.PosePlan.OperationHeaders.Count;
                     operationIndex++)
                {
                    CharacterPoseOperationHeader operation =
                        projection.PosePlan.OperationHeaders[operationIndex];
                    if (operation.Code !=
                        CharacterPoseOperationCode.SelectedPosePlayer)
                    {
                        continue;
                    }
                    CharacterPosePlayerOperationPayload payload =
                        (CharacterPosePlayerOperationPayload)
                        projection.PosePlan.OperationPages
                            .RequirePayload(operation);
                    if (payload.PlayerIndex < 0 ||
                        !payload.SourceProviderId.IsValid ||
                        !payload.SourceIndex.IsValid)
                    {
                        throw new InvalidOperationException(
                            $"Selected Pose Player operation '{operation.NodeId}' has invalid compiled inputs.");
                    }
                    var player = new AnimationSelectedPosePlayerRuntime(
                        operation.NodeId,
                        payload.PlayerIndex,
                        payload.PlayerIndex,
                        payload.SourceProviderId,
                        payload.SelectionAvailability,
                        projection.Rig,
                        projection.PosePlan.Parameters.Count);
                    directPlayerList.Add(player);
                    nodeRuntimeIndex.AddDirect(
                        operation.NodeId,
                        player,
                        payload.PlayerIndex,
                        payload.PlayerIndex);
                }
                directPlayers = directPlayerList.ToArray();
                clipPlayers = new AnimationClipPlayerRuntime[
                    projection.PosePlan.ClipPlayers.Count];
                for (int clipPlayerIndex = 0;
                     clipPlayerIndex < clipPlayers.Length;
                     clipPlayerIndex++)
                {
                    AnimationClipPlayerRuntime clipPlayer =
                        AnimationClipPlayerFactory.Create(
                            projection,
                            projection.PosePlan.ClipPlayers[
                                clipPlayerIndex]);
                    clipPlayers[clipPlayerIndex] = clipPlayer;
                    nodeRuntimeIndex.AddPlayer(
                        clipPlayer.NodeId,
                        clipPlayer.PlayerIndex);
                }
                blendSpacePlayers = new AnimationBlendSpacePlayerRuntime[
                    projection.BlendSpacePlayers.Count];
                for (int blendSpaceIndex = 0;
                     blendSpaceIndex < blendSpacePlayers.Length;
                     blendSpaceIndex++)
                {
                    CharacterAnimationBlendSpacePlayerPlan descriptor =
                        projection.BlendSpacePlayers[blendSpaceIndex];
                    descriptor.RequireValid(projection);
                    var player = new AnimationBlendSpacePlayerRuntime(
                        descriptor,
                        projection.BlendSpaces[
                            descriptor.BlendSpacePlanIndex],
                        projection.PosePlan,
                        projection.Rig,
                        projection.FootAnalysis,
                        projection.ClipPhasePlans);
                    blendSpacePlayers[blendSpaceIndex] = player;
                    nodeRuntimeIndex.AddPlayer(
                        player.NodeId,
                        player.PlayerIndex);
                }
                poseStateSources = new PoseStateAndSourceRuntime(
                    projection.PosePlan,
                    projection.ClipPhasePlans,
                    projection.SourcePhasePlans,
                    clipPlayers,
                    blendSpacePlayers,
                    linkedFragments);
                constraints = new CharacterPoseConstraintRuntime(
                    footPlacement,
                    executionView.PoseBoneContributions,
                    executionView.GoalAssemblers,
                    fullBodyIkSolver,
                    executionView.FullBodyIkGoalContributionCount,
                    executionView.FullBodyIkContributionGoalCount,
                    projection.Rig.RigId,
                    projection.Rig.RigRevision);
                diagnostics = new CharacterPoseDiagnosticsRuntime(
                    projection,
                    in initialLayout,
                    physicalSourceCapacity,
                    executionView,
                    diagnosticsEventSink);
                source = new CharacterPoseSourceModule(
                    animancer,
                    projection,
                    actionPlayback.Bindings,
                    motionMatching,
                    rigBinding,
                    projection.Rig,
                    physicalSourceCapacity,
                    clipCatalogCapacity,
                    directPlayers.Length,
                    clipPlayers.Length,
                    blendSpacePlayers.Length);
                publication = new CharacterFinalPosePublication(
                    projection.PosePlan,
                    projection.Rig,
                    rigBinding,
                    rootHierarchy,
                    source);
                if (managesGraphClock)
                {
                    animancer.Graph.PauseGraph();
                    graphPaused = true;
                }
                programFrames.DiscardEvaluationFrame(
                    initialFrame.CompletionIdentity);
                var rootOrientationWarps = new RootOrientationWarpRuntime[
                    projection.PosePlan.RootOrientationWarps.Count];
                for (int i = 0;
                     i < rootOrientationWarps.Length;
                     i++)
                {
                    CharacterPresentationRootOrientationWarpDescriptor
                        descriptor =
                            projection.PosePlan.RootOrientationWarps[i];
                    rootOrientationWarps[i] =
                        new RootOrientationWarpRuntime(
                            descriptor,
                            clipPlayers[descriptor.ClipPlayerIndex]);
                }
                actorState = new CharacterPoseActorState(
                    stacks,
                    routes,
                    directPlayers,
                    poseStateSources,
                    rootOrientationWarps,
                    inertialization,
                    nodeRuntimeIndex,
                    linkedFragments,
                    actionPlayback,
                    animationSlots,
                    source.Capacity);
                program = new CharacterPoseProgramRuntime(
                    animancer,
                    projection.PosePlan,
                    executionView,
                    actorState,
                    programFrames,
                    programTuning,
                    source,
                    new CharacterPoseWorldContextAdapter(
                        projection,
                        source,
                        publication),
                    constraints,
                    presentationWorkspace);
                return new CharacterPoseRuntimeComposition(
                    animancer,
                    program,
                    source,
                    constraints,
                    publication,
                    diagnostics,
                    managesGraphClock);
            }
            catch
            {
                if (program != null)
                {
                    diagnostics?.Dispose();
                    program.DetachExecutionJobs();
                    source?.Dispose();
                    program.Dispose();
                    constraints?.Dispose();
                }
                else if (actorState != null)
                {
                    source?.Dispose();
                    diagnostics?.Dispose();
                    constraints?.Dispose();
                    actorState.Dispose();
                    programTuning?.Dispose();
                    executionView?.Dispose();
                    programFrames?.Dispose();
                }
                else
                {
                    source?.Dispose();
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
                        for (int i = blendSpacePlayers.Length - 1;
                             i >= 0;
                             i--)
                        {
                            blendSpacePlayers[i]?.Dispose();
                        }
                    }
                    diagnostics?.Dispose();
                    constraints?.Dispose();
                    executionView?.Dispose();
                    inertialization?.Dispose();
                    programTuning?.Dispose();
                    programFrames?.Dispose();
                }
                workspace?.Dispose();
                if (graphPaused &&
                    animancer &&
                    animancer.IsGraphInitialized)
                {
                    animancer.Graph.UnpauseGraph();
                }
                throw;
            }
        }

        static CharacterPoseOperationHeader
            RequireBlendStackOperation(
                CharacterPoseProgramImage plan,
                int blendNodeIndex,
                PoseNodeId nodeId)
        {
            CharacterPoseOperationHeader result = null;
            for (int i = 0; i < plan.OperationHeaders.Count; i++)
            {
                CharacterPoseOperationHeader candidate =
                    plan.OperationHeaders[i];
                if (candidate.Code != CharacterPoseOperationCode.BlendStack &&
                    candidate.Code != CharacterPoseOperationCode.AnimationSlot ||
                    RequireBlendNodeIndex(plan, candidate) != blendNodeIndex ||
                    candidate.NodeId != nodeId)
                {
                    continue;
                }
                if (result != null)
                {
                    throw new InvalidOperationException(
                        $"Pose Plan duplicates Blend Stack operation '{nodeId}'.");
                }
                result = candidate;
            }
            if (result == null ||
                result.Code == CharacterPoseOperationCode.AnimationSlot &&
                plan.OperationPages.FindInputValueIndex(
                    result,
                    CharacterPoseValueReferenceKind.OperationControl) < 0 ||
                result.Code == CharacterPoseOperationCode.BlendStack &&
                (!RequireSourceProviderId(plan, result).IsValid ||
                 !RequireSourceIndex(plan, result).IsValid ||
                 plan.OperationPages.FindInputValueIndex(
                     result,
                     CharacterPoseValueReferenceKind.OperationControl) >= 0))
            {
                throw new InvalidOperationException(
                    $"Pose Plan has no valid animation transition operation '{nodeId}'.");
            }
            return result;
        }

        static CharacterPoseOperationHeader RequireControlInput(
            CharacterPoseProgramImage plan,
            CharacterPoseOperationHeader operation)
        {
            int controlIndex = plan.OperationPages.FindInputValueIndex(
                operation,
                CharacterPoseValueReferenceKind.OperationControl);
            if ((uint)controlIndex >= (uint)operation.Index)
            {
                throw new InvalidOperationException(
                    $"Pose operation '{operation.NodeId}' has no compiled control input.");
            }
            return plan.OperationHeaders[controlIndex];
        }

        static int RequirePlayerIndex(
            CharacterPoseProgramImage plan,
            CharacterPoseOperationHeader operation) => operation.Family switch
        {
            CharacterPoseOperationFamily.Player =>
                ((CharacterPosePlayerOperationPayload)
                    plan.OperationPages.RequirePayload(operation)).PlayerIndex,
            CharacterPoseOperationFamily.Blend =>
                ((CharacterPoseBlendOperationPayload)
                    plan.OperationPages.RequirePayload(operation)).PlayerIndex,
            CharacterPoseOperationFamily.AnimationSlot =>
                ((CharacterPoseAnimationSlotOperationPayload)
                    plan.OperationPages.RequirePayload(operation)).PlayerIndex,
            _ => -1
        };

        static int RequireBlendNodeIndex(
            CharacterPoseProgramImage plan,
            CharacterPoseOperationHeader operation) => operation.Family switch
        {
            CharacterPoseOperationFamily.Blend =>
                ((CharacterPoseBlendOperationPayload)
                    plan.OperationPages.RequirePayload(operation)).BlendNodeIndex,
            CharacterPoseOperationFamily.AnimationSlot =>
                ((CharacterPoseAnimationSlotOperationPayload)
                    plan.OperationPages.RequirePayload(operation)).BlendNodeIndex,
            _ => -1
        };

        static PresentationPoseSourceProviderId RequireSourceProviderId(
            CharacterPoseProgramImage plan,
            CharacterPoseOperationHeader operation) =>
            ((CharacterPoseBlendOperationPayload)
                plan.OperationPages.RequirePayload(operation)).SourceProviderId;

        static PresentationPoseSourceIndex RequireSourceIndex(
            CharacterPoseProgramImage plan,
            CharacterPoseOperationHeader operation) =>
            ((CharacterPoseBlendOperationPayload)
                plan.OperationPages.RequirePayload(operation)).SourceIndex;

        static int CalculateSourceCapacity(CharacterPoseProgramImage plan)
        {
            int capacity = 0;
            for (int i = 0; i < plan.OperationHeaders.Count; i++)
            {
                CharacterPoseOperationHeader operation =
                    plan.OperationHeaders[i];
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
            {
                capacity = checked(
                    capacity +
                    plan.MotionMatchingNodes[i].LiveEntryCapacity);
            }
            return capacity > 0
                ? capacity
                : throw new InvalidOperationException(
                    "Pose Plan has no source capacity.");
        }
    }
}
