using System;
using System.Collections.Generic;
using System.Linq;
using Animancer;
using ThirdPersonCharacter.Animation.TransitionRouting;
using ThirdPersonCharacter.Pipeline.Animation.BlendStack;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Pipeline.Animation.Lifecycle;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonCharacter.Pipeline.Presentation.Animancer;
using ThirdPersonSimulation;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using Unity.Profiling;

namespace ThirdPersonCharacter.Pipeline.Animation.Presentation
{
    internal readonly struct PosePlanFrameLease
    {
        internal PosePlanFrameLease(
            ulong frameIdentity)
        {
            FrameIdentity = frameIdentity;
        }

        internal ulong FrameIdentity { get; }
        internal bool IsValid => FrameIdentity != 0;
    }

    internal readonly struct PosePlanPreparedEvaluation
    {
        internal PosePlanPreparedEvaluation(
            ulong completionIdentity,
            float presentationDeltaSeconds,
            in CharacterPoseGraphNativeBinding frame,
            in CharacterPoseGraphStagedExecutor poseExecutor,
            in AnimationFinalPoseNativeReadBinding finalRead,
            bool hasCommittedFinal,
            in AnimationFinalPoseNativeReadBinding committedFinalRead)
        {
            CompletionIdentity = completionIdentity;
            PresentationDeltaSeconds = presentationDeltaSeconds;
            Frame = frame;
            PoseExecutor = poseExecutor;
            FinalRead = finalRead;
            HasCommittedFinal = hasCommittedFinal;
            CommittedFinalRead = committedFinalRead;
        }

        internal ulong CompletionIdentity { get; }
        internal float PresentationDeltaSeconds { get; }
        internal CharacterPoseGraphNativeBinding Frame { get; }
        internal CharacterPoseGraphStagedExecutor PoseExecutor { get; }
        internal AnimationFinalPoseNativeReadBinding FinalRead { get; }
        internal bool HasCommittedFinal { get; }
        internal AnimationFinalPoseNativeReadBinding CommittedFinalRead { get; }
        internal bool IsValid =>
            CompletionIdentity != 0 &&
            float.IsFinite(PresentationDeltaSeconds) &&
            PresentationDeltaSeconds >= 0f &&
            Frame.CompletionIdentity == CompletionIdentity &&
            FinalRead.CompletionIdentity == CompletionIdentity;
    }

    internal sealed class PosePlanExecutionRuntime :
        IDisposable,
        IPoseStateSourceSelectionSink
    {
        static readonly ProfilerMarker PrepareMarker =
            new ProfilerMarker("ThirdPerson.Presentation.Animation.Prepare");
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
        static readonly Comparison<PendingActionBackendRelease>
            s_PendingActionBackendReleaseComparison =
                (left, right) => left.PlayableSource.CompareTo(
                    right.PlayableSource);

        enum StandaloneSourceReleaseOwner : byte
        {
            Direct = 1,
            Sequence = 2,
            BlendSpace = 3
        }

        struct PreparedStandaloneSourceRelease
        {
            internal StandaloneSourceReleaseOwner Owner;
            internal int PlayerIndex;
            internal AnimationPoseSourceId SourceId;
            internal PoseNodeId NodeId;
            internal AnimationPhysicalSourceIdentity PhysicalSource;
            internal AnimationPhysicalSourceReleaseToken PhysicalRelease;
            internal AnimationPoseSourceReleaseToken BackendRelease;
            internal AnimationPlayerReleaseToken PlayerRelease;
        }

        struct PreparedMotionMatchingHistoryRead
        {
            internal MotionMatchingSelectionBatchItem Selection;
            internal int PlayerIndex;
            internal bool SourceUsed;
        }

        sealed class PendingPoseSourceRelease
        {
            internal bool InUse;
            internal AnimationBlendStackRuntime Stack;
            internal CharacterAnimationTransitionRouteRuntime
                Route;
            internal AnimationBlendStackSourceReleaseToken StackRelease;
            internal AnimationPhysicalSourceIdentity
                PhysicalSource;
            internal AnimationPhysicalSourceReleaseToken
                PhysicalRelease;
            internal AnimationPoseSourceReleaseToken BackendRelease;
            internal bool NotifyRouteAfterApply;

            internal AnimationBlendStackRelease Release =>
                StackRelease.Release;

            internal void Clear()
            {
                InUse = false;
                Stack = null;
                Route = null;
                StackRelease = default;
                PhysicalSource = default;
                PhysicalRelease = default;
                BackendRelease = default;
                NotifyRouteAfterApply = false;
            }
        }

        sealed class PendingActionBackendRelease
        {
            internal bool InUse;
            internal AnimationSlotId SlotId;
            internal AnimationBlendStackRuntime Stack;
            internal CharacterAnimationTransitionRouteRuntime Route;
            internal AnimationBlendStackSourceReleaseToken StackRelease;
            internal AnimationPhysicalSourceIdentity PhysicalSource;
            internal AnimationPhysicalSourceReleaseToken PhysicalRelease;
            internal AnimationPoseSourceReleaseToken BackendRelease;
            internal ActionBackendSourceIdentity PlayableSource;
            internal ActionBackendSourceIdentity StoredPoseSource;
            internal ulong RequestIdentity;
            internal ulong PlayableCompletionIdentity;
            internal ulong StoredPoseCompletionIdentity;
            internal bool NotifyRouteAfterApply;

            internal AnimationBlendStackRelease Release =>
                StackRelease.Release;

            internal void Clear()
            {
                InUse = false;
                SlotId = default;
                Stack = null;
                Route = null;
                StackRelease = default;
                PhysicalSource = default;
                PhysicalRelease = default;
                BackendRelease = default;
                PlayableSource = default;
                StoredPoseSource = default;
                RequestIdentity = 0;
                PlayableCompletionIdentity = 0;
                StoredPoseCompletionIdentity = 0;
                NotifyRouteAfterApply = false;
            }
        }

        sealed class PreparedActionBackendRelease
        {
            internal PreparedActionBackendRelease(
                int releaseCapacity,
                int backendSourceCapacity)
            {
                Request = new ActionBackendReleaseRequest(
                    backendSourceCapacity);
                Sources = new List<PendingActionBackendRelease>(
                    releaseCapacity);
            }

            internal bool InUse;
            internal readonly ActionBackendReleaseRequest Request;
            internal readonly List<PendingActionBackendRelease> Sources;

            internal void Clear()
            {
                InUse = false;
                Request.Clear();
                Sources.Clear();
            }
        }

        readonly AnimancerComponent m_Animancer;
        readonly CharacterPresentationProjection m_Projection;
        readonly AnimationPoseNativeWorkspace m_Workspace;
        readonly CharacterPoseGraphNativeProgram m_PosePlan;
        readonly PoseInertializationNativeProgram m_InertializationPlan;
        readonly PhysicalPoseSourceRegistry m_PhysicalSources;
        readonly AnimancerPoseSamplingBackend m_SourceBackend;
        readonly ComposedAnimationPoseFramePublisher m_FramePublisher;
        readonly AnimationFinalPosePhysicalWriter m_FinalWriter;
        readonly AnimationPoseSourceContribution[]
            m_FootPlacementContributions;
        readonly AnimationPresentationRuntimeSnapshotPublisher m_DiagnosticsPublisher;
        readonly AnimationBlendStackRuntime[] m_Stacks;
        readonly CharacterAnimationTransitionRouteRuntime[] m_StackRoutes;
        readonly AnimationSelectedPosePlayerRuntime[] m_DirectPlayers;
        readonly PoseStateAndSourceRuntime m_PoseStateSources;
        readonly RootOrientationWarpRuntime[] m_RootOrientationWarps;
        readonly AnimationSlotBlendJob[] m_SlotJobs;
        readonly AnimationSelectedPosePlayerJob[] m_DirectPlayerJobs;
        readonly AnimationSelectedPosePlayerJob[] m_SequencePlayerJobs;
        readonly AnimationSelectedPosePlayerJob[] m_BlendSpacePlayerJobs;
        readonly AnimationPhysicalSourceIdentity[] m_DirectPhysicalSources;
        readonly AnimationPhysicalSourceIdentity[] m_SequencePhysicalSources;
        readonly AnimationPhysicalSourceIdentity[] m_BlendSpacePhysicalSources;
        readonly int[] m_DirectSourceIndices;
        readonly int[] m_SequenceSourceIndices;
        readonly int[] m_BlendSpaceSourceIndices;
        readonly AnimationPoseSourceClipBinding[] m_ClipCatalogScratch;
        readonly AnimationReleasedPoseSourceSnapshot[] m_ReleasedSources;
        readonly PreparedStandaloneSourceRelease[]
            m_PreparedStandaloneSourceReleases;
        readonly List<PendingActionBackendRelease>
            m_PendingActionBackendReleases;
        readonly List<PreparedActionBackendRelease>
            m_PreparedActionBackendReleases;
        readonly List<ActionBackendReleaseCompletion>
            m_ActionBackendReleaseCompletions;
        readonly List<AnimationSlotSourceReleaseCompletion>
            m_ActionSlotReleaseCompletions;
        readonly List<PendingPoseSourceRelease>
            m_PendingPoseSourceReleases;
        readonly HashSet<AnimationPhysicalSourceIdentity>
            m_ReleaseValidationIdentities;
        readonly HashSet<ActionBackendSourceIdentity>
            m_ExpectedActionBackendSources;
        readonly PendingPoseSourceRelease[]
            m_PendingPoseSourceReleasePool;
        readonly PendingActionBackendRelease[]
            m_PendingActionBackendReleasePool;
        readonly PreparedActionBackendRelease[]
            m_PreparedActionBackendReleasePool;
        readonly List<PendingActionBackendRelease>
            m_PrepareActionBackendPendingScratch;
        readonly List<ActionBackendSourceIdentity>
            m_PrepareActionBackendSourceScratch;
        readonly bool[] m_ActionBackendAcknowledgementMatches;
        readonly string[] m_PlayableBackendResourceIds;
        readonly string[] m_StoredPoseBackendResourceIds;
        readonly Dictionary<PoseNodeId, AnimationBlendStackRuntime> m_StacksByNode =
            new Dictionary<PoseNodeId, AnimationBlendStackRuntime>();
        readonly Dictionary<PoseNodeId, CharacterAnimationTransitionRouteRuntime> m_StackRoutesByNode =
            new Dictionary<PoseNodeId, CharacterAnimationTransitionRouteRuntime>();
        readonly Dictionary<PoseNodeId, int> m_PlayerIndicesByNode = new Dictionary<PoseNodeId, int>();
        readonly Dictionary<PoseNodeId, int>
            m_SourceOwnerIndicesByNode =
                new Dictionary<PoseNodeId, int>();
        readonly Dictionary<PoseNodeId, AnimationSelectedPosePlayerRuntime> m_DirectPlayersByNode =
            new Dictionary<PoseNodeId, AnimationSelectedPosePlayerRuntime>();
        readonly MotionMatchingPosePlanSourceUsage[] m_MotionMatchingSourceUsages;
        readonly MotionMatchingPosePlanHistoryCompletion[] m_MotionMatchingHistoryCompletions;
        readonly PreparedMotionMatchingHistoryRead[]
            m_PreparedMotionMatchingHistoryReads;
        readonly AnimationMixerPlayable m_SourceFanIn;
        readonly Playable m_PreviousOutputSource;
        readonly float m_PreviousOutputWeight;
        readonly bool m_ManagesGraphClock;
        readonly int m_FootPlacementWeightParameterIndex;

        AnimationScriptPlayable[] m_SlotPlayables;
        AnimationScriptPlayable[] m_DirectPlayerPlayables;
        AnimationScriptPlayable[] m_SequencePlayerPlayables;
        AnimationScriptPlayable[] m_BlendSpacePlayerPlayables;
        ulong m_CompletionIdentity = 1;
        ulong m_FrameCompletionContext;
        ulong m_ActionBackendReleaseRequestIdentity;
        ulong m_ActionBackendReleaseCompletionIdentity;
        int m_ReleasedSourceCount;
        int m_PreparedStandaloneSourceReleaseCount;
        int m_PendingActionBackendReleaseFrameStartCount;
        int m_MotionMatchingSourceUsageCount;
        int m_PreparedMotionMatchingHistoryReadCount;
        ulong m_PreparedMotionMatchingPresentationFrame;
        ulong m_PreparedMotionMatchingResetSequence;
        ulong m_PreparedMotionMatchingSelectionCompletionIdentity;
        ulong m_PreparedMotionMatchingPoseCompletionIdentity;
        bool m_MotionMatchingPoseCompletionPrepared;
        int m_ValidatedActionBackendAcknowledgementSourceCount;
        bool m_ActionBackendAcknowledgementsValidated;
        int m_MotionMatchingHistoryCompletionCount;
        CharacterPoseGraphNativeBinding m_LastCompletedFrame;
        CharacterPoseGraphNativeBinding m_PendingCompletedFrame;
        PosePlanFrameLease m_ActiveFrameLease;
        bool m_CommitValidated;
        bool m_HasCompletedFrame;
        bool m_HasPendingCompletedFrame;
        bool m_HasOpenFrame;
        bool m_RecordReleaseDiagnostics;
        AnimationPresentationFrameOutcome m_PendingFrameOutcome;
        bool m_JobsInstalled;
        bool m_Disposed;

        internal PosePlanExecutionRuntime(
            AnimancerComponent animancer,
            CharacterAnimationRigBinding rigBinding,
            CharacterPresentationProjection projection,
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
            int sourceCapacity = CalculateSourceCapacity(projection.PosePlan);
            int physicalSourceCapacity = checked(
                sourceCapacity +
                projection.PosePlan.SequencePlayers.Count);
            int clipCatalogCapacity =
                AnimationPoseRequestWorkspaceLayoutFactory
                    .RequireClipCatalogCapacity(projection);

            AnimationPoseNativeWorkspace workspace = null;
            CharacterPoseGraphNativeProgram poseProgram = null;
            PoseInertializationNativeProgram inertializationProgram = null;
            PhysicalPoseSourceRegistry physicalSources = null;
            AnimancerPoseSamplingBackend sourceBackend = null;
            AnimationBlendStackRuntime[] stacks = null;
            CharacterAnimationTransitionRouteRuntime[] stackRoutes = null;
            AnimationSelectedPosePlayerRuntime[] directPlayers = null;
            AnimationSequencePlayerRuntime[] sequencePlayers = null;
            AnimationBlendSpacePlayerRuntime[] blendSpacePlayers = null;
            PoseStateAndSourceRuntime poseStateSources = null;
            AnimationPresentationRuntimeSnapshotPublisher diagnosticsPublisher = null;
            AnimationFinalPosePhysicalWriter finalWriter = null;
            AnimationMixerPlayable sourceFanIn = default;
            Playable previousOutputSource = default;
            float previousOutputWeight = 1f;
            try
            {
                workspace = new AnimationPoseNativeWorkspace(projection);
                CharacterPoseGraphNativeBinding initialFrame = workspace.BeginFrame(m_CompletionIdentity);
                poseProgram = new CharacterPoseGraphNativeProgram(
                    projection.PosePlan,
                    projection.Rig,
                    projection.BlendCurveCatalog,
                    projection.BlendProfileCatalog);
                inertializationProgram = new PoseInertializationNativeProgram(
                    projection.PosePlan,
                    projection.BlendCurveCatalog,
                    projection.BlendProfileCatalog);
                physicalSources = new PhysicalPoseSourceRegistry(physicalSourceCapacity);
                sourceBackend = new AnimancerPoseSamplingBackend(
                    animancer,
                    rigBinding,
                    projection.Rig,
                    physicalSourceCapacity,
                    clipCatalogCapacity);
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
                        workspace.RequirePlayerWriteBinding(operation.PlayerIndex, initialFrame.CompletionIdentity);
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
                    m_StacksByNode.Add(blendNode.NodeId, stack);
                    m_StackRoutesByNode.Add(blendNode.NodeId, route);
                    m_PlayerIndicesByNode.Add(blendNode.NodeId, operation.PlayerIndex);
                    if (!route.IsAnimationSlot)
                        m_SourceOwnerIndicesByNode.Add(
                            blendNode.NodeId,
                            0);
                    if (route.IsAnimationSlot)
                    {
                        CharacterAnimationSlotNativeControl control = route.NativeControl;
                        poseProgram.SetAnimationSlotControl(route.AnimationSlotIndex, in control);
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
                    m_PlayerIndicesByNode.Add(operation.NodeId, operation.PlayerIndex);
                    m_SourceOwnerIndicesByNode.Add(
                        operation.NodeId,
                        operation.PlayerIndex);
                    m_DirectPlayersByNode.Add(operation.NodeId, player);
                }
                directPlayers = directPlayerList.ToArray();
                sequencePlayers = new AnimationSequencePlayerRuntime[projection.PosePlan.SequencePlayers.Count];
                for (int sequenceIndex = 0; sequenceIndex < sequencePlayers.Length; sequenceIndex++)
                {
                    AnimationSequencePlayerRuntime sequence = AnimationSequencePlayerFactory.Create(
                        projection,
                        projection.PosePlan.SequencePlayers[sequenceIndex]);
                    sequencePlayers[sequenceIndex] = sequence;
                    m_PlayerIndicesByNode.Add(sequence.NodeId, sequence.PlayerIndex);
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
                        projection.FootAnalysis);
                    blendSpacePlayers[blendSpaceIndex] = player;
                    m_PlayerIndicesByNode.Add(player.NodeId, player.PlayerIndex);
                }
                poseStateSources =
                    new PoseStateAndSourceRuntime(
                        projection.PosePlan,
                        sequencePlayers,
                        blendSpacePlayers);
                diagnosticsPublisher = new AnimationPresentationRuntimeSnapshotPublisher(
                    projection,
                    poseProgram,
                    in initialFrame,
                    workspace,
                    physicalSources.Capacity);
                finalWriter = new AnimationFinalPosePhysicalWriter(
                    rigBinding,
                    projection.Rig);

                PlayableGraph graph = animancer.Graph.PlayableGraph;
                if (!graph.IsValid())
                    throw new InvalidOperationException("Animation Pose Graph requires a valid Animancer PlayableGraph.");
                sourceFanIn = AnimationMixerPlayable.Create(
                    graph,
                    checked(physicalSourceCapacity + 1));
                PlayableOutput output = animancer.Graph.Output;
                previousOutputSource = output.GetSourcePlayable();
                previousOutputWeight = output.GetWeight();
                animancer.Graph.InsertOutputPlayable(sourceFanIn);
                sourceFanIn.SetInputWeight(0, 1f);
                output.SetWeight(0f);
                if (managesGraphClock)
                    animancer.Graph.PauseGraph();
                workspace.DiscardFrame(initialFrame.CompletionIdentity);
            }
            catch
            {
                if (sourceFanIn.IsValid() &&
                    animancer && animancer.IsGraphInitialized)
                {
                    PlayableOutput output = animancer.Graph.Output;
                    if (output.IsOutputValid())
                    {
                        output.SetSourcePlayable(previousOutputSource);
                        output.SetWeight(previousOutputWeight);
                    }
                }
                if (sourceFanIn.IsValid())
                    sourceFanIn.Destroy();
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
                if (sequencePlayers != null)
                {
                    for (int i = sequencePlayers.Length - 1; i >= 0; i--)
                        sequencePlayers[i]?.Dispose();
                }
                if (blendSpacePlayers != null)
                {
                    for (int i = blendSpacePlayers.Length - 1; i >= 0; i--)
                        blendSpacePlayers[i]?.Dispose();
                }
                sourceBackend?.Dispose();
                diagnosticsPublisher?.Dispose();
                physicalSources?.Dispose();
                poseProgram?.Dispose();
                inertializationProgram?.Dispose();
                workspace?.Dispose();
                throw;
            }

            m_Workspace = workspace;
            m_PosePlan = poseProgram;
            m_InertializationPlan = inertializationProgram;
            m_PhysicalSources = physicalSources;
            m_SourceBackend = sourceBackend;
            m_Stacks = stacks;
            m_StackRoutes = stackRoutes;
            m_DirectPlayers = directPlayers;
            m_PoseStateSources = poseStateSources;
            m_RootOrientationWarps =
                new RootOrientationWarpRuntime[
                    projection.PosePlan.RootOrientationWarps.Count];
            for (int i = 0; i < m_RootOrientationWarps.Length; i++)
            {
                CharacterPresentationRootOrientationWarpDescriptor descriptor =
                    projection.PosePlan.RootOrientationWarps[i];
                m_RootOrientationWarps[i] =
                    new RootOrientationWarpRuntime(
                        descriptor,
                        sequencePlayers[descriptor.SequencePlayerIndex]);
            }
            m_MotionMatchingSourceUsages =
                new MotionMatchingPosePlanSourceUsage[sourceCapacity];
            m_MotionMatchingHistoryCompletions =
                new MotionMatchingPosePlanHistoryCompletion[
                    m_PoseStateSources
                        .MotionMatchingProviderCount];
            m_PreparedMotionMatchingHistoryReads =
                new PreparedMotionMatchingHistoryRead[
                    m_MotionMatchingHistoryCompletions.Length];
            m_SlotJobs = new AnimationSlotBlendJob[stacks.Length];
            m_DirectPlayerJobs = new AnimationSelectedPosePlayerJob[directPlayers.Length];
            m_SequencePlayerJobs = new AnimationSelectedPosePlayerJob[sequencePlayers.Length];
            m_BlendSpacePlayerJobs =
                new AnimationSelectedPosePlayerJob[blendSpacePlayers.Length];
            m_DirectPhysicalSources = new AnimationPhysicalSourceIdentity[directPlayers.Length];
            m_SequencePhysicalSources = new AnimationPhysicalSourceIdentity[sequencePlayers.Length];
            m_BlendSpacePhysicalSources =
                new AnimationPhysicalSourceIdentity[blendSpacePlayers.Length];
            m_DirectSourceIndices = new int[directPlayers.Length];
            m_SequenceSourceIndices = new int[sequencePlayers.Length];
            m_BlendSpaceSourceIndices = new int[blendSpacePlayers.Length];
            m_ClipCatalogScratch =
                new AnimationPoseSourceClipBinding[clipCatalogCapacity];
            m_FramePublisher = new ComposedAnimationPoseFramePublisher(projection.PosePlan, projection.Rig);
            m_FinalWriter = finalWriter;
            m_FootPlacementContributions =
                new AnimationPoseSourceContribution[
                    projection.PosePlan.ContributionWorkspaceCount /
                    projection.PosePlan.PoseValueWorkspaceCount];
            m_DiagnosticsPublisher = diagnosticsPublisher;
            m_ReleasedSources = new AnimationReleasedPoseSourceSnapshot[physicalSources.Capacity];
            int releaseCapacity = physicalSources.Capacity;
            m_PreparedStandaloneSourceReleases =
                new PreparedStandaloneSourceRelease[releaseCapacity];
            int backendSourceCapacity = checked(releaseCapacity * 2);
            m_PendingActionBackendReleases =
                new List<PendingActionBackendRelease>(releaseCapacity);
            m_PreparedActionBackendReleases =
                new List<PreparedActionBackendRelease>(releaseCapacity);
            m_ActionBackendReleaseCompletions =
                new List<ActionBackendReleaseCompletion>(backendSourceCapacity);
            m_ActionSlotReleaseCompletions =
                new List<AnimationSlotSourceReleaseCompletion>(releaseCapacity);
            m_PendingPoseSourceReleases =
                new List<PendingPoseSourceRelease>(releaseCapacity);
            m_ReleaseValidationIdentities =
                new HashSet<AnimationPhysicalSourceIdentity>(releaseCapacity);
            m_ExpectedActionBackendSources =
                new HashSet<ActionBackendSourceIdentity>(backendSourceCapacity);
            m_PendingPoseSourceReleasePool =
                new PendingPoseSourceRelease[releaseCapacity];
            m_PendingActionBackendReleasePool =
                new PendingActionBackendRelease[releaseCapacity];
            m_PreparedActionBackendReleasePool =
                new PreparedActionBackendRelease[releaseCapacity];
            m_PrepareActionBackendPendingScratch =
                new List<PendingActionBackendRelease>(releaseCapacity);
            m_PrepareActionBackendSourceScratch =
                new List<ActionBackendSourceIdentity>(backendSourceCapacity);
            m_ActionBackendAcknowledgementMatches =
                new bool[backendSourceCapacity];
            m_PlayableBackendResourceIds = new string[releaseCapacity];
            m_StoredPoseBackendResourceIds = new string[releaseCapacity];
            for (int i = 0; i < releaseCapacity; i++)
            {
                m_PendingPoseSourceReleasePool[i] =
                    new PendingPoseSourceRelease();
                m_PendingActionBackendReleasePool[i] =
                    new PendingActionBackendRelease();
                m_PreparedActionBackendReleasePool[i] =
                    new PreparedActionBackendRelease(
                        releaseCapacity,
                        backendSourceCapacity);
                m_PlayableBackendResourceIds[i] =
                    $"animation-source-slot/{i}/playable";
                m_StoredPoseBackendResourceIds[i] =
                    $"animation-source-slot/{i}/stored-pose";
            }
            m_SourceFanIn = sourceFanIn;
            m_PreviousOutputSource = previousOutputSource;
            m_PreviousOutputWeight = previousOutputWeight;
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
        internal AnimationPresentationRuntimeCapacityMetrics
            CreateCapacityMetrics(
                int actionJournalCapacity,
                int samplingJournalCapacity,
                int slotJournalCapacity) =>
            new AnimationPresentationRuntimeCapacityMetrics(
                m_Workspace.DenseDoublePageResidentPayloadBytes,
                PoseInertializationNativeProgramPayloadMetrics
                    .CalculateDoublePageResidentPayloadBytes(
                        m_InertializationPlan),
                m_FramePublisher
                    .DenseDoublePageResidentPayloadBytes,
                actionJournalCapacity,
                samplingJournalCapacity,
                slotJournalCapacity,
                m_PreparedStandaloneSourceReleases.Length,
                m_PhysicalSources.Capacity,
                m_PreparedStandaloneSourceReleases.Length);

        internal void RecordNoDiagnosticsInterest() =>
            m_DiagnosticsPublisher.RecordNoInterestSkip();
        internal AnimationPresentationFrameOutcome PendingFrameOutcome
        {
            get
            {
                RequireOpenMutation();
                if (m_PendingFrameOutcome == AnimationPresentationFrameOutcome.None)
                    throw new InvalidOperationException("Animation Presentation frame outcome is not available.");
                return m_PendingFrameOutcome;
            }
        }
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

        internal PosePlanFrameLease BeginPendingFrame(
            ulong frameIdentity)
        {
            RequireAlive();
            if (frameIdentity == 0)
                throw new ArgumentOutOfRangeException(nameof(frameIdentity));
            if (m_ActiveFrameLease.IsValid)
            {
                throw new InvalidOperationException(
                    "Pose Plan frame mutation is already open.");
            }
            if (m_CommitValidated)
            {
                throw new InvalidOperationException(
                    "Pose Plan committed-frame finalization is still pending.");
            }
            if (m_PreparedStandaloneSourceReleaseCount != 0)
            {
                throw new InvalidOperationException(
                    "Pose Plan standalone source releases from the committed frame were not finalized.");
            }
            if (m_ActionBackendAcknowledgementsValidated)
            {
                throw new InvalidOperationException(
                    "Pose Plan action backend acknowledgements from the previous frame were not applied.");
            }
            if (m_MotionMatchingPoseCompletionPrepared)
            {
                throw new InvalidOperationException(
                    "Pose Plan Motion Matching completion from the previous frame was not consumed.");
            }
            try
            {
                m_PendingActionBackendReleaseFrameStartCount =
                    m_PendingActionBackendReleases.Count;
                m_SourceBackend.BeginFrame(frameIdentity);
                m_PendingCompletedFrame = default;
                m_HasPendingCompletedFrame = false;
                m_PendingFrameOutcome = AnimationPresentationFrameOutcome.None;
                m_PosePlan.BeginFrame();
                m_InertializationPlan.BeginFrame();
                m_PhysicalSources.BeginFrame();
                for (int i = 0; i < m_StackRoutes.Length; i++)
                    m_StackRoutes[i].BeginFrame();
                for (int i = 0; i < m_RootOrientationWarps.Length; i++)
                    m_RootOrientationWarps[i].BeginFrame();
                BeginPendingModuleFrames();
                m_HasOpenFrame = true;
                m_ActiveFrameLease =
                    new PosePlanFrameLease(frameIdentity);
                return m_ActiveFrameLease;
            }
            catch
            {
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
                if (m_PosePlan.HasOpenFrame)
                    m_PosePlan.DiscardFrame();
                if (m_PhysicalSources.HasOpenFrame)
                    m_PhysicalSources.DiscardFrame();
                if (m_SourceBackend.HasOpenFrame)
                    m_SourceBackend.DiscardFrame(frameIdentity);
                m_PendingActionBackendReleaseFrameStartCount = 0;
                throw;
            }
        }

        internal void SealFrame(
            PosePlanFrameLease lease)
        {
            RequireMutation(lease);
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
            m_Workspace.CommitFrame(
                m_PendingCompletedFrame.CompletionIdentity);
            m_InertializationPlan.CommitFrame();
            m_PosePlan.CommitFrame();
            m_SourceBackend.CommitFrame(lease.FrameIdentity);
            m_PhysicalSources.CommitFrame();
            for (int i = 0; i < m_StackRoutes.Length; i++)
                m_StackRoutes[i].CommitFrame();
            for (int i = 0; i < m_RootOrientationWarps.Length; i++)
                m_RootOrientationWarps[i].CommitFrame();
            for (int i = 0; i < m_Stacks.Length; i++)
                m_Stacks[i].CommitFrame();
            for (int i = 0; i < m_DirectPlayers.Length; i++)
                m_DirectPlayers[i].CommitFrame();
            m_PoseStateSources.CommitFrame();
            m_LastCompletedFrame = m_PendingCompletedFrame;
            m_HasCompletedFrame = true;
            m_PendingCompletedFrame = default;
            m_HasPendingCompletedFrame = false;
            m_HasOpenFrame = false;
            m_ActiveFrameLease = default;
            m_PendingActionBackendReleaseFrameStartCount = 0;
        }

        internal void ValidatePendingSeal(
            PosePlanFrameLease lease)
        {
            RequireMutation(lease);
            m_PhysicalSources.ValidateFrame();
            m_ReleaseValidationIdentities.Clear();
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
                    AnimationPhysicalSourceIdentity physical =
                        ValidatePhysicalRelease(
                        sourceId,
                        player.NodeId,
                        default);
                    AddPreparedStandaloneSourceRelease(
                        StandaloneSourceReleaseOwner.Direct,
                        i,
                        sourceId,
                        player.NodeId,
                        physical,
                        m_PhysicalSources.PrepareRelease(
                            physical,
                            sourceId),
                        m_SourceBackend.StageRelease(
                            sourceId,
                            player.NodeId),
                        in playerRelease);
                }
            }
            for (int i = 0;
                 i < m_PoseStateSources.SequencePlayers.Length;
                 i++)
            {
                AnimationSequencePlayerRuntime player =
                    m_PoseStateSources.SequencePlayers[i];
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
                    AnimationPhysicalSourceIdentity physical =
                        ValidatePhysicalRelease(
                        sourceId,
                        player.NodeId,
                        default);
                    AddPreparedStandaloneSourceRelease(
                        StandaloneSourceReleaseOwner.Sequence,
                        i,
                        sourceId,
                        player.NodeId,
                        physical,
                        m_PhysicalSources.PrepareRelease(
                            physical,
                            sourceId),
                        m_SourceBackend.StageRelease(
                            sourceId,
                            player.NodeId),
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
                    AnimationPhysicalSourceIdentity physical =
                        ValidatePhysicalRelease(
                        sourceId,
                        player.NodeId,
                        default);
                    AddPreparedStandaloneSourceRelease(
                        StandaloneSourceReleaseOwner.BlendSpace,
                        i,
                        sourceId,
                        player.NodeId,
                        physical,
                        m_PhysicalSources.PrepareRelease(
                            physical,
                            sourceId),
                        m_SourceBackend.StageRelease(
                            sourceId,
                            player.NodeId),
                        in playerRelease);
                }
            }
            for (int i = 0;
                 i < m_PendingPoseSourceReleases.Count;
                 i++)
            {
                PendingPoseSourceRelease pending =
                    m_PendingPoseSourceReleases[i];
                ValidatePhysicalRelease(
                    pending.Release.SourceId,
                    pending.Release.PoseNodeId,
                    pending.PhysicalSource);
                pending.PhysicalRelease =
                    m_PhysicalSources.PrepareRelease(
                        pending.PhysicalSource,
                        pending.Release.SourceId);
                pending.BackendRelease =
                    m_SourceBackend.StageRelease(
                        pending.Release.SourceId,
                        pending.Release.PoseNodeId);
            }
            int preparedActionReleaseCount =
                ValidatePreparedActionBackendReleases();
            PrepareRouteReleaseNotifications();
            if (checked(
                    standaloneReleaseCount +
                    m_PendingPoseSourceReleases.Count +
                    preparedActionReleaseCount) >
                m_ReleasedSources.Length)
            {
                throw new InvalidOperationException(
                    "Animation diagnostics release capacity was exceeded.");
            }
            if (checked(
                    m_ActionBackendReleaseCompletions.Count +
                    preparedActionReleaseCount * 2) >
                m_ActionBackendReleaseCompletions.Capacity)
            {
                throw new InvalidOperationException(
                    "Action backend release completion capacity was exceeded.");
            }
            m_ReleaseValidationIdentities.Clear();
            m_SourceBackend.ValidateFrame(
                lease.FrameIdentity);
            m_CommitValidated = true;
        }

        internal void DiscardPendingFrame(
            PosePlanFrameLease lease)
        {
            RequireMutation(lease);
            Exception failure = null;
            if (m_PhysicalSources.HasOpenFrame)
            {
                for (int i = m_PhysicalSources.PendingRegistrationCount - 1; i >= 0; i--)
                {
                    AnimationPhysicalSourceIdentity physical = default;
                    DiscardStep(
                        () => physical = m_PhysicalSources.GetPendingRegistration(i),
                        ref failure);
                    if (!physical.IsValid)
                        continue;
                    DiscardStep(
                        () => DiscardPreparedPhysicalSource(physical),
                        ref failure);
                }
            }
            if (m_SourceBackend.HasOpenFrame)
            {
                DiscardStep(
                    () => m_SourceBackend.DiscardFrame(
                        lease.FrameIdentity),
                    ref failure);
            }
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
            if (m_Workspace.HasPendingFrame)
            {
                ulong pendingCompletionIdentity =
                    m_Workspace.PendingCompletionIdentity;
                DiscardStep(
                    () => m_Workspace.DiscardFrame(
                        pendingCompletionIdentity),
                    ref failure);
            }
            if (m_InertializationPlan.HasOpenFrame)
            {
                DiscardStep(
                    m_InertializationPlan.DiscardFrame,
                    ref failure);
            }
            if (m_PosePlan.HasOpenFrame)
            {
                DiscardStep(
                    m_PosePlan.DiscardFrame,
                    ref failure);
            }
            if (m_PhysicalSources.HasOpenFrame)
            {
                DiscardStep(
                    m_PhysicalSources.DiscardFrame,
                    ref failure);
            }
            DiscardStep(
                m_FramePublisher.DiscardPending,
                ref failure);
            DiscardStep(
                m_DiagnosticsPublisher
                    .DiscardPendingFrame,
                ref failure);
            DiscardStep(
                DiscardPendingReleasePreparation,
                ref failure);
            m_ReleaseValidationIdentities.Clear();
            Array.Clear(
                m_PreparedStandaloneSourceReleases,
                0,
                m_PreparedStandaloneSourceReleaseCount);
            m_PreparedStandaloneSourceReleaseCount = 0;
            ClearValidatedActionBackendAcknowledgements();
            ClearPreparedMotionMatchingPoseCompletion();
            m_HasOpenFrame = false;
            m_ActiveFrameLease = default;
            m_CommitValidated = false;
            m_PendingCompletedFrame = default;
            m_HasPendingCompletedFrame = false;
            m_PendingFrameOutcome = AnimationPresentationFrameOutcome.None;
            m_RecordReleaseDiagnostics = false;
            if (failure != null)
            {
                throw new AggregateException(
                    "Pose Plan Pending discard failed.",
                    failure);
            }
        }

        internal ComposedAnimationPoseFrame
            FinalizeCommittedFrame()
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
            ExecutePreparedStandaloneSourceReleases();
            ExecutePendingPoseSourceReleases();
            ComposedAnimationPoseFrame result =
                m_FramePublisher.CommitPending();
            m_CommitValidated = false;
            m_PendingFrameOutcome = AnimationPresentationFrameOutcome.None;
            return result;
        }

        void ExecutePreparedStandaloneSourceReleases()
        {
            for (int i = 0;
                 i < m_PreparedStandaloneSourceReleaseCount;
                 i++)
            {
                PreparedStandaloneSourceRelease release =
                    m_PreparedStandaloneSourceReleases[i];
                int port = checked(
                    release.PhysicalSource.Index.Value + 1);
                if (m_SourceFanIn.GetInput(port).IsValid())
                    m_SourceFanIn.DisconnectInput(port);
                m_SourceFanIn.SetInputWeight(port, 0f);
                m_SourceBackend.Release(
                    in release.BackendRelease);
                m_PhysicalSources.ApplyPreparedRelease(
                    in release.PhysicalRelease);
                switch (release.Owner)
                {
                    case StandaloneSourceReleaseOwner.Direct:
                        m_DirectPlayers[release.PlayerIndex]
                            .ApplyPreparedRelease(
                                in release.PlayerRelease);
                        break;
                    case StandaloneSourceReleaseOwner.Sequence:
                        m_PoseStateSources.SequencePlayers[
                                release.PlayerIndex]
                            .ApplyPreparedRelease(
                                in release.PlayerRelease);
                        break;
                    case StandaloneSourceReleaseOwner.BlendSpace:
                        m_PoseStateSources.BlendSpacePlayers[
                                release.PlayerIndex]
                            .ApplyPreparedRelease(
                                in release.PlayerRelease);
                        break;
                    default:
                        throw new InvalidOperationException(
                            "Standalone pose source release owner is invalid.");
                }
                if (m_RecordReleaseDiagnostics)
                {
                    m_ReleasedSources[m_ReleasedSourceCount++] =
                        new AnimationReleasedPoseSourceSnapshot(
                            release.NodeId,
                            release.SourceId,
                            m_CompletionIdentity);
                }
                m_PreparedStandaloneSourceReleases[i] = default;
            }
            m_PreparedStandaloneSourceReleaseCount = 0;
        }

        void ClearValidatedActionBackendAcknowledgements()
        {
            Array.Clear(
                m_ActionBackendAcknowledgementMatches,
                0,
                m_ValidatedActionBackendAcknowledgementSourceCount);
            m_ValidatedActionBackendAcknowledgementSourceCount = 0;
            m_ActionBackendAcknowledgementsValidated = false;
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
            m_MotionMatchingSourceUsageCount = 0;
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
            m_DiagnosticsPublisher.Invalidate();

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
            m_MotionMatchingSourceUsageCount = 0;
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
                    !m_PlayerIndicesByNode.TryGetValue(
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
            return new MotionMatchingPosePlanCompletion(
                m_PreparedMotionMatchingPresentationFrame,
                m_PreparedMotionMatchingResetSequence,
                m_PreparedMotionMatchingSelectionCompletionIdentity,
                m_PreparedMotionMatchingPoseCompletionIdentity,
                m_MotionMatchingSourceUsages,
                m_MotionMatchingSourceUsageCount,
                m_MotionMatchingHistoryCompletions,
                m_MotionMatchingHistoryCompletionCount);
        }

        internal void BeginCommittedDiagnostics(
            AnimationPresentationDiagnosticsInterest interest)
        {
            RequireAlive();
            RequireNoOpenMutation();
            if (interest == AnimationPresentationDiagnosticsInterest.None)
                return;
            if (!m_HasCompletedFrame ||
                m_LastCompletedFrame.CompletionIdentity == 0 ||
                !m_Workspace.TryGetCommittedFinalReadBinding(
                    out AnimationFinalPoseNativeReadBinding finalRead) ||
                finalRead.CompletionIdentity != m_LastCompletedFrame.CompletionIdentity)
            {
                throw new InvalidOperationException(
                    "Animation diagnostics requires a successfully sealed committed Pose page.");
            }
            using (DiagnosticsMarker.Auto())
            {
                m_DiagnosticsPublisher.BeginFrame(
                    in m_LastCompletedFrame,
                    in finalRead,
                    m_Stacks,
                    m_StackRoutes,
                    m_PoseStateSources.StateMachines,
                    m_InertializationPlan,
                    m_PhysicalSources,
                    m_RootOrientationWarps,
                    interest);
            }
        }

        internal bool PublishDiagnostics()
        {
            if (!m_DiagnosticsPublisher.HasPendingFrame)
                return false;
            using (DiagnosticsMarker.Auto())
            {
                m_DiagnosticsPublisher.Publish(
                    m_ReleasedSources,
                    m_ReleasedSourceCount,
                    m_PoseStateSources.BlendSpacePlayers);
            }
            return true;
        }

        internal void CopyActionSlotReleaseCompletions(
            List<AnimationSlotSourceReleaseCompletion> destination)
        {
            RequireAlive();
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));
            destination.Clear();
            destination.AddRange(
                m_ActionSlotReleaseCompletions);
        }

        internal bool HasPendingActionBackendSources(
            AnimationPlaybackId playbackId)
        {
            RequireAlive();
            if (!playbackId.IsValid)
                throw new ArgumentException(
                    "Action playback identity is invalid.",
                    nameof(playbackId));
            for (int i = 0;
                 i < m_PendingActionBackendReleases.Count;
                 i++)
            {
                if (m_PendingActionBackendReleases[i]
                    .Release.SourceId.PlaybackId.Equals(
                        playbackId))
                {
                    return true;
                }
            }
            return false;
        }

        internal bool TryPrepareActionBackendReleaseRequest(
            AnimationPlaybackId playbackId,
            out ActionBackendReleaseRequest request)
        {
            RequireAlive();
            if (!playbackId.IsValid)
                throw new ArgumentException(
                    "Action playback identity is invalid.",
                    nameof(playbackId));
            m_PrepareActionBackendSourceScratch.Clear();
            m_PrepareActionBackendPendingScratch.Clear();
            for (int i = 0;
                 i < m_PendingActionBackendReleases.Count;
                 i++)
            {
                PendingActionBackendRelease candidate =
                    m_PendingActionBackendReleases[i];
                if (!candidate.Release.SourceId.PlaybackId
                        .Equals(playbackId))
                {
                    continue;
                }
                if (candidate.RequestIdentity != 0)
                {
                    throw new InvalidOperationException(
                        $"Action playback '{playbackId}' already has a prepared backend release request.");
                }
                AddFixed(
                    m_PrepareActionBackendPendingScratch,
                    candidate,
                    "Action backend pending release scratch");
                AddFixed(
                    m_PrepareActionBackendSourceScratch,
                    candidate.PlayableSource,
                    "Action backend source scratch");
                AddFixed(
                    m_PrepareActionBackendSourceScratch,
                    candidate.StoredPoseSource,
                    "Action backend source scratch");
            }
            if (m_PrepareActionBackendPendingScratch.Count == 0)
            {
                request = null;
                return false;
            }
            m_PrepareActionBackendPendingScratch.Sort(
                s_PendingActionBackendReleaseComparison);
            ulong requestIdentity =
                NextActionBackendReleaseRequestIdentity();
            PreparedActionBackendRelease prepared =
                RentPreparedActionBackendRelease();
            try
            {
                prepared.Request.Prepare(
                    requestIdentity,
                    playbackId,
                    m_PrepareActionBackendSourceScratch);
                for (int i = 0;
                     i < m_PrepareActionBackendPendingScratch.Count;
                     i++)
                {
                    PendingActionBackendRelease pending =
                        m_PrepareActionBackendPendingScratch[i];
                    pending.RequestIdentity = requestIdentity;
                    AddFixed(
                        prepared.Sources,
                        pending,
                        "Prepared Action backend release");
                }
                AddFixed(
                    m_PreparedActionBackendReleases,
                    prepared,
                    "Prepared Action backend release journal");
                request = prepared.Request;
                return true;
            }
            catch
            {
                for (int i = 0;
                     i < m_PrepareActionBackendPendingScratch.Count;
                     i++)
                {
                    if (m_PrepareActionBackendPendingScratch[i]
                            .RequestIdentity == requestIdentity)
                    {
                        m_PrepareActionBackendPendingScratch[i]
                            .RequestIdentity = 0;
                    }
                }
                ReturnPreparedActionBackendRelease(prepared);
                throw;
            }
        }

        internal void CopyActionBackendReleaseCompletions(
            List<ActionBackendReleaseCompletion> destination)
        {
            RequireAlive();
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));
            destination.Clear();
            destination.AddRange(
                m_ActionBackendReleaseCompletions);
        }

        internal void ApplyValidatedActionBackendReleaseCompletionAcknowledgements()
        {
            RequireAlive();
            if (!m_ActionBackendAcknowledgementsValidated ||
                m_ActionBackendReleaseCompletions.Count !=
                m_ValidatedActionBackendAcknowledgementSourceCount)
            {
                throw new InvalidOperationException(
                    "Action backend release acknowledgements were not validated for the committed frame.");
            }
            int writeIndex = 0;
            for (int readIndex = 0;
                 readIndex <
                 m_ValidatedActionBackendAcknowledgementSourceCount;
                 readIndex++)
            {
                if (m_ActionBackendAcknowledgementMatches[readIndex])
                    continue;
                m_ActionBackendReleaseCompletions[writeIndex++] =
                    m_ActionBackendReleaseCompletions[readIndex];
            }
            if (writeIndex <
                m_ActionBackendReleaseCompletions.Count)
            {
                m_ActionBackendReleaseCompletions.RemoveRange(
                    writeIndex,
                    m_ActionBackendReleaseCompletions.Count -
                    writeIndex);
            }
            Array.Clear(
                m_ActionBackendAcknowledgementMatches,
                0,
                m_ValidatedActionBackendAcknowledgementSourceCount);
            m_ValidatedActionBackendAcknowledgementSourceCount = 0;
            m_ActionBackendAcknowledgementsValidated = false;
        }

        internal void
            ValidateActionBackendReleaseCompletionAcknowledgements(
                IReadOnlyList<ActionBackendReleaseCompletion>
                    completions)
        {
            RequireAlive();
            if (completions == null)
                throw new ArgumentNullException(nameof(completions));
            Array.Clear(
                m_ActionBackendAcknowledgementMatches,
                0,
                m_ActionBackendReleaseCompletions.Count);
            for (int i = 0; i < completions.Count; i++)
            {
                ActionBackendReleaseCompletion expected =
                    completions[i];
                int matchIndex = -1;
                for (int candidateIndex = 0;
                     candidateIndex <
                     m_ActionBackendReleaseCompletions.Count;
                     candidateIndex++)
                {
                    ActionBackendReleaseCompletion candidate =
                        m_ActionBackendReleaseCompletions[
                            candidateIndex];
                    if (!Matches(
                            in candidate,
                            in expected))
                    {
                        continue;
                    }
                    if (matchIndex >= 0 ||
                        m_ActionBackendAcknowledgementMatches[
                            candidateIndex])
                    {
                        throw new InvalidOperationException(
                            "Action backend release completion is duplicated.");
                    }
                    matchIndex = candidateIndex;
                }
                if (matchIndex < 0)
                {
                    throw new InvalidOperationException(
                        "Action backend release completion acknowledgement is not exact.");
                }
                m_ActionBackendAcknowledgementMatches[
                    matchIndex] = true;
            }
            m_ValidatedActionBackendAcknowledgementSourceCount =
                m_ActionBackendReleaseCompletions.Count;
            m_ActionBackendAcknowledgementsValidated = true;
        }

        static bool Matches(
            in ActionBackendReleaseCompletion left,
            in ActionBackendReleaseCompletion right) =>
            left.RequestIdentity == right.RequestIdentity &&
            left.PlaybackId.Equals(right.PlaybackId) &&
            left.Source.Equals(right.Source) &&
            left.CompletionIdentity ==
            right.CompletionIdentity;

        internal void ExecutePreparedActionBackendReleaseRequests()
        {
            RequireAlive();
            for (int requestIndex = 0;
                 requestIndex <
                 m_PreparedActionBackendReleases.Count;
                 requestIndex++)
            {
                PreparedActionBackendRelease prepared =
                    m_PreparedActionBackendReleases[requestIndex];
                ExecuteActionBackendReleaseRequest(prepared);
                ReturnPreparedActionBackendRelease(prepared);
            }
            m_PreparedActionBackendReleases.Clear();
            m_PendingActionBackendReleases.Clear();
            m_SourceBackend.ExecuteDeferredReleases();
            m_RecordReleaseDiagnostics = false;
        }

        void ExecuteActionBackendReleaseRequest(
            PreparedActionBackendRelease prepared)
        {
            ActionBackendReleaseRequest request =
                prepared?.Request ??
                    throw new ArgumentNullException(nameof(prepared));
            for (int sourceIndex = 0;
                 sourceIndex < prepared.Sources.Count;
                 sourceIndex++)
            {
                PendingActionBackendRelease release =
                    prepared.Sources[sourceIndex];
                int port = checked(
                    release.PhysicalSource.Index.Value + 1);
                if (m_SourceFanIn.GetInput(port).IsValid())
                    m_SourceFanIn.DisconnectInput(port);
                m_SourceFanIn.SetInputWeight(port, 0f);
                m_SourceBackend.Release(
                    in release.BackendRelease);
                m_PhysicalSources.ApplyPreparedRelease(
                    in release.PhysicalRelease);
                release.Stack.ApplyPreparedRelease(
                    in release.StackRelease);
                if (release.NotifyRouteAfterApply)
                    release.Route.NotifySourcesReleased();
                if (m_RecordReleaseDiagnostics)
                {
                    m_ReleasedSources[m_ReleasedSourceCount++] =
                        new AnimationReleasedPoseSourceSnapshot(
                            release.Release.PoseNodeId,
                            release.Release.SourceId,
                            release.Release.CompletionIdentity);
                }
                AddActionBackendReleaseCompletion(
                    new ActionBackendReleaseCompletion(
                        request.RequestIdentity,
                        request.PlaybackId,
                        release.PlayableSource,
                        release.PlayableCompletionIdentity));
                AddActionBackendReleaseCompletion(
                    new ActionBackendReleaseCompletion(
                        request.RequestIdentity,
                        request.PlaybackId,
                        release.StoredPoseSource,
                        release.StoredPoseCompletionIdentity));
                release.Clear();
            }
        }

        int ValidatePreparedActionBackendReleases()
        {
            int releaseCount = 0;
            for (int requestIndex = 0;
                 requestIndex <
                 m_PreparedActionBackendReleases.Count;
                 requestIndex++)
            {
                PreparedActionBackendRelease prepared =
                    m_PreparedActionBackendReleases[
                        requestIndex];
                ActionBackendReleaseRequest request =
                    prepared?.Request ??
                    throw new InvalidOperationException(
                        "Prepared Action backend release has no request.");
                prepared.Sources.Sort(
                    s_PendingActionBackendReleaseComparison);
                m_ExpectedActionBackendSources.Clear();
                for (int i = 0;
                     i < prepared.Sources.Count;
                     i++)
                {
                    PendingActionBackendRelease candidate =
                        prepared.Sources[i];
                    if (candidate == null ||
                        !m_PendingActionBackendReleases.Contains(
                            candidate) ||
                        candidate.RequestIdentity !=
                        request.RequestIdentity ||
                        !candidate.Release.SourceId.PlaybackId
                            .Equals(request.PlaybackId) ||
                        !m_ExpectedActionBackendSources.Add(
                            candidate.PlayableSource) ||
                        !m_ExpectedActionBackendSources.Add(
                            candidate.StoredPoseSource))
                    {
                        throw new InvalidOperationException(
                            "Action backend release request contains a detached or duplicate source.");
                    }
                    ValidatePhysicalRelease(
                        candidate.Release.SourceId,
                        candidate.Release.PoseNodeId,
                        candidate.PhysicalSource);
                    candidate.PhysicalRelease =
                        m_PhysicalSources.PrepareRelease(
                            candidate.PhysicalSource,
                            candidate.Release.SourceId);
                    candidate.BackendRelease =
                        m_SourceBackend.StageRelease(
                            candidate.Release.SourceId,
                            candidate.Release.PoseNodeId);
                    candidate.PlayableCompletionIdentity =
                        NextActionBackendReleaseCompletionIdentity();
                    candidate.StoredPoseCompletionIdentity =
                        NextActionBackendReleaseCompletionIdentity();
                    releaseCount = checked(
                        releaseCount + 1);
                }
                if (prepared.Sources.Count == 0 ||
                    m_ExpectedActionBackendSources.Count !=
                    request.Sources.Count)
                {
                    throw new InvalidOperationException(
                        "Action backend release request source set is incomplete.");
                }
                for (int i = 0;
                     i < request.Sources.Count;
                     i++)
                {
                    ActionBackendSourceIdentity source =
                        request.Sources[i];
                    if (!m_ExpectedActionBackendSources.Contains(
                            source))
                    {
                        throw new InvalidOperationException(
                            "Action backend release request source set is not exact.");
                    }
                }
            }
            if (releaseCount !=
                m_PendingActionBackendReleases.Count)
            {
                throw new InvalidOperationException(
                    "Action backend release journal contains sources without a prepared request.");
            }
            return releaseCount;
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
                !m_StacksByNode.TryGetValue(
                    binding.SlotNodeId,
                    out AnimationBlendStackRuntime stack) ||
                !m_StackRoutesByNode.TryGetValue(
                    binding.SlotNodeId,
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
                frame.EffectiveSampleTime;
            var request = new AnimationPoseSampleRequest(
                sourceId,
                frame.SourcePoseContinuityIdentity,
                presentationRequestSequence,
                binding.ProgramProducerIndex,
                binding.Animation.MarkerBindingId,
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
                    frame.LeftFootFeatures,
                    frame.RightFootFeatures,
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
                !m_StacksByNode.TryGetValue(
                    slotNodeId,
                    out AnimationBlendStackRuntime stack) ||
                !m_StackRoutesByNode.TryGetValue(
                    slotNodeId,
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
            m_PoseStateSources.PrepareFrame(
                presentationDeltaSeconds,
                in factFrame);
            for (int i = 0; i < m_StackRoutes.Length; i++)
            {
                CharacterAnimationTransitionRouteRuntime route = m_StackRoutes[i];
                route.FlushReleaseCompletion();
                if (!route.IsAnimationSlot)
                    continue;
                CharacterAnimationSlotNativeControl control = route.NativeControl;
                m_PosePlan.SetAnimationSlotControl(route.AnimationSlotIndex, in control);
            }
            for (int i = 0; i < m_Stacks.Length; i++)
                m_Stacks[i].Advance(presentationDeltaSeconds);
            m_PoseStateSources.AdvanceSources(
                presentationDeltaSeconds,
                in parameterFrame);
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
            m_PoseStateSources.EvaluateTransitions(
                in factFrame,
                m_PosePlan,
                workspace,
                lease);
            for (int i = 0; i < m_RootOrientationWarps.Length; i++)
            {
                CharacterRootOrientationWarpNativeControl control =
                    m_RootOrientationWarps[i].Prepare(
                        in factFrame);
                m_PosePlan.SetRootOrientationWarpControl(
                    i,
                    in control);
            }
        }

        internal PosePlanPreparedEvaluation PrepareEvaluation(
            float presentationDeltaSeconds,
            IReadOnlyDictionary<AnimationPlayerSourceSampleKey,
                AnimationResolvedPoseSourceSample> actionSourceSamples,
            IReadOnlyDictionary<AnimationPlayerSourceSampleKey,
                PresentationPoseSourceSample> providerSourceSamples,
            bool recordDiagnostics)
        {
            RequireAlive();
            RequireOpenMutation();
            if (!float.IsFinite(presentationDeltaSeconds) || presentationDeltaSeconds < 0f)
                throw new ArgumentOutOfRangeException(nameof(presentationDeltaSeconds));
            if (actionSourceSamples == null)
                throw new ArgumentNullException(
                    nameof(actionSourceSamples));
            if (providerSourceSamples == null)
                throw new ArgumentNullException(
                    nameof(providerSourceSamples));

            bool hasCommittedFinal =
                m_Workspace.TryGetCommittedFinalReadBinding(
                    out AnimationFinalPoseNativeReadBinding committedFinalRead);

            ulong completionIdentity;
            CharacterPoseGraphNativeBinding frame;
            using (PrepareMarker.Auto())
            {
                completionIdentity = NextCompletionIdentity();
                m_FrameCompletionContext = completionIdentity;
                m_ReleasedSourceCount = 0;
                m_RecordReleaseDiagnostics = recordDiagnostics;
                m_ActionSlotReleaseCompletions.Clear();
                frame = m_Workspace.BeginFrame(completionIdentity);
                for (int i = 0; i < m_Stacks.Length; i++)
                    m_Stacks[i].BeginSourceFrame(completionIdentity);
                for (int i = 0; i < m_DirectPlayers.Length; i++)
                {
                    m_DirectPlayers[i].BeginFrame(completionIdentity);
                    m_DirectPhysicalSources[i] = default;
                    m_DirectSourceIndices[i] = -1;
                }
                for (int i = 0;
                     i < m_PoseStateSources.SequencePlayers.Length;
                     i++)
                {
                    m_PoseStateSources.SequencePlayers[i]
                        .BeginFrame(completionIdentity);
                    m_SequencePhysicalSources[i] = default;
                    m_SequenceSourceIndices[i] = -1;
                }
                for (int i = 0;
                     i < m_PoseStateSources.BlendSpacePlayers.Length;
                     i++)
                {
                    m_PoseStateSources.BlendSpacePlayers[i]
                        .BeginFrame(completionIdentity);
                    m_BlendSpacePhysicalSources[i] = default;
                    m_BlendSpaceSourceIndices[i] = -1;
                }
                for (int stackIndex = 0; stackIndex < m_Stacks.Length; stackIndex++)
                    PrepareStackSources(
                        m_Stacks[stackIndex],
                        presentationDeltaSeconds,
                        actionSourceSamples,
                        providerSourceSamples);
                for (int playerIndex = 0; playerIndex < m_DirectPlayers.Length; playerIndex++)
                    PrepareDirectSource(
                        playerIndex,
                        presentationDeltaSeconds,
                        providerSourceSamples);
                for (int playerIndex = 0;
                     playerIndex <
                     m_PoseStateSources.SequencePlayers.Length;
                     playerIndex++)
                    PrepareSequenceSource(playerIndex, presentationDeltaSeconds);
                for (int playerIndex = 0;
                     playerIndex <
                     m_PoseStateSources.BlendSpacePlayers.Length;
                     playerIndex++)
                {
                    PrepareBlendSpaceSource(playerIndex, presentationDeltaSeconds);
                }
            }

            CharacterPoseGraphStagedExecutor poseExecutor;
            AnimationFinalPoseNativeReadBinding finalRead;
            using (ValidateMarker.Auto())
            {
                for (int slotIndex = 0; slotIndex < m_Stacks.Length; slotIndex++)
                {
                    AnimationBlendStackRuntime stack = m_Stacks[slotIndex];
                    AnimationPlayerPoseNativeWriteBinding write =
                        m_Workspace.RequirePlayerWriteBinding(stack.PlayerIndex, completionIdentity);
                    m_SlotJobs[slotIndex] = stack.PrepareSlotJob(
                        completionIdentity,
                        in write,
                        m_PhysicalSources);
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
                        m_Workspace.RequirePlayerWriteBinding(player.PlayerIndex, completionIdentity);
                    m_DirectPlayerJobs[playerIndex] = player.PrepareJob(
                        completionIdentity,
                        in write,
                        m_DirectPhysicalSources[playerIndex],
                        m_DirectSourceIndices[playerIndex]);
                }
                for (int playerIndex = 0;
                     playerIndex <
                     m_PoseStateSources.SequencePlayers.Length;
                     playerIndex++)
                {
                    AnimationSequencePlayerRuntime player =
                        m_PoseStateSources.SequencePlayers[
                            playerIndex];
                    AnimationPlayerPoseNativeWriteBinding write =
                        m_Workspace.RequirePlayerWriteBinding(player.PlayerIndex, completionIdentity);
                    m_SequencePlayerJobs[playerIndex] = player.PrepareJob(
                        completionIdentity,
                        in write,
                        m_SequencePhysicalSources[playerIndex],
                        m_SequenceSourceIndices[playerIndex]);
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
                        m_Workspace.RequirePlayerWriteBinding(
                            player.PlayerIndex,
                            completionIdentity);
                    m_BlendSpacePlayerJobs[playerIndex] = player.PrepareJob(
                        completionIdentity,
                        in write,
                        m_BlendSpacePhysicalSources[playerIndex],
                        m_BlendSpaceSourceIndices[playerIndex]);
                }
                StageCompletedSources(
                    completionIdentity,
                    recordDiagnostics);
                poseExecutor = new CharacterPoseGraphStagedExecutor(
                    m_PosePlan,
                    m_InertializationPlan,
                    m_Workspace.RequirePoseGraphBinding(completionIdentity));
                finalRead =
                    m_Workspace.RequireFinalReadBinding(completionIdentity);
                InstallOrUpdateJobs();
                m_FinalWriter.ValidateBindingsBeforeEvaluate(
                    in finalRead,
                    hasCommittedFinal,
                    in committedFinalRead);
            }

            return new PosePlanPreparedEvaluation(
                completionIdentity,
                presentationDeltaSeconds,
                in frame,
                in poseExecutor,
                in finalRead,
                hasCommittedFinal,
                in committedFinalRead);
        }

        internal void ExecuteEvaluateBarrier(
            ActorId actorId,
            ulong renderFrame,
            in CharacterBodyPresentationFrame bodyFrame,
            CharacterFootPlacementRuntime footPlacement,
            in PosePlanPreparedEvaluation prepared,
            Action enterEvaluateBarrier)
        {
            RequireAlive();
            RequireOpenMutation();
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
            if (!prepared.IsValid ||
                prepared.CompletionIdentity != m_FrameCompletionContext ||
                !m_Workspace.HasPendingFrame ||
                m_Workspace.PendingCompletionIdentity != prepared.CompletionIdentity)
            {
                throw new ArgumentException(
                    "Pose Plan prepared evaluation is not the active Pending frame.",
                    nameof(prepared));
            }
            if (enterEvaluateBarrier == null)
                throw new ArgumentNullException(nameof(enterEvaluateBarrier));

            ulong completionIdentity = prepared.CompletionIdentity;
            float presentationDeltaSeconds =
                prepared.PresentationDeltaSeconds;
            CharacterPoseGraphNativeBinding frame = prepared.Frame;
            CharacterPoseGraphStagedExecutor poseExecutor =
                prepared.PoseExecutor;
            AnimationFinalPoseNativeReadBinding finalRead =
                prepared.FinalRead;
            AnimationFinalPoseNativeReadBinding committedFinalRead =
                prepared.CommittedFinalRead;
            bool hasCommittedFinal = prepared.HasCommittedFinal;

            enterEvaluateBarrier();
            m_SourceBackend.EnterEvaluateBarrier(
                m_ActiveFrameLease.FrameIdentity);
            using (GraphEvaluateMarker.Auto())
                m_Animancer.Evaluate(presentationDeltaSeconds);
            using (PoseGraphExecuteMarker.Auto())
            {
                poseExecutor.BeginStagedEvaluation();
                for (int stageIndex = 0;
                     stageIndex < m_PosePlan.Stages.Length;
                     stageIndex++)
                {
                    AnimationPoseGraphNativeStage stage =
                        m_PosePlan.Stages[stageIndex];
                    if (stage.ExecutionDomain ==
                        CharacterPoseExecutionDomain.WorldAwarePose)
                    {
                        PrepareWorldAwareStage(
                            actorId,
                            renderFrame,
                            presentationDeltaSeconds,
                            in bodyFrame,
                            footPlacement,
                            completionIdentity,
                            in stage);
                    }
                    if (!poseExecutor.ExecuteStage(
                            stageIndex,
                            presentationDeltaSeconds))
                        break;
                }
                poseExecutor.CompleteStagedEvaluation();
                m_Workspace.RequireStagesCompleted(completionIdentity);
            }
            using (FinalWriteMarker.Auto())
            {
                m_FinalWriter.Write(
                    in finalRead,
                    hasCommittedFinal,
                    in committedFinalRead);
                AnimationFinalPoseWriteOutcome finalWriteOutcome =
                    m_Workspace.RequireFinalWriteOutcome(
                        completionIdentity);
                m_PendingFrameOutcome = finalWriteOutcome switch
                {
                    AnimationFinalPoseWriteOutcome.Committed =>
                        AnimationPresentationFrameOutcome.Committed,
                    AnimationFinalPoseWriteOutcome.TypedInvalid =>
                        AnimationPresentationFrameOutcome.TypedInvalid,
                    _ => throw new InvalidOperationException(
                        $"Unsupported final animation pose writer outcome '{finalWriteOutcome}'.")
                };
                m_FramePublisher.PreparePending(
                    in finalRead,
                    m_PhysicalSources);
            }
            if (m_PendingFrameOutcome !=
                AnimationPresentationFrameOutcome.Committed)
            {
                return;
            }

            using (SealMarker.Auto())
            {
                for (int i = 0; i < m_Stacks.Length; i++)
                    m_Stacks[i].CompleteFrame(completionIdentity);
                for (int i = 0; i < m_DirectPlayers.Length; i++)
                    m_DirectPlayers[i].CompleteFrame();
                for (int i = 0;
                     i < m_PoseStateSources.SequencePlayers.Length;
                     i++)
                    m_PoseStateSources.SequencePlayers[i]
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
        }

        void PrepareWorldAwareStage(
            ActorId actorId,
            ulong renderFrame,
            float presentationDeltaSeconds,
            in CharacterBodyPresentationFrame bodyFrame,
            CharacterFootPlacementRuntime footPlacement,
            ulong completionIdentity,
            in AnimationPoseGraphNativeStage stage)
        {
            int footPlacementOperationIndex = -1;
            for (int operationIndex = stage.OperationStart;
                 operationIndex < stage.OperationStart +
                 stage.OperationCount;
                 operationIndex++)
            {
                AnimationPoseGraphNativeOperation operation =
                    m_PosePlan.Operations[operationIndex];
                if (operation.Code !=
                    CharacterPoseOperationCode.FootPlacement)
                {
                    continue;
                }
                if (footPlacementOperationIndex >= 0)
                {
                    throw new InvalidOperationException(
                        "World-Aware Pose stage contains multiple Foot Placement operations.");
                }
                footPlacementOperationIndex = operationIndex;
            }
            if (footPlacementOperationIndex < 0)
            {
                throw new InvalidOperationException(
                    "World-Aware Pose stage has no supported planner operation.");
            }

            AnimationPoseGraphNativeOperation footPlacementOperation =
                m_PosePlan.Operations[footPlacementOperationIndex];
            CharacterFootPlacementNativeControl control;
            if (footPlacement == null)
            {
                control = CharacterFootPlacementNativeControl
                    .WorldContextUnavailable;
            }
            else
            {
                AnimationPoseValueNativeReadBinding inputBinding =
                    m_Workspace.RequirePoseValueReadBinding(
                        footPlacementOperation.InputValueIndexA,
                        completionIdentity);
                int contributionCount =
                    m_FramePublisher.ResolveContributions(
                        in inputBinding,
                        m_PhysicalSources,
                        m_FootPlacementContributions);
                var input = new CharacterFootPlacementPoseInput(
                    m_Projection.PosePlan.PlanHash,
                    in inputBinding,
                    m_FootPlacementContributions,
                    contributionCount);
                var planningFrame =
                    new CharacterFootPlacementPlanningFrame(
                        actorId,
                        renderFrame,
                        presentationDeltaSeconds,
                        bodyFrame,
                        in input);
                control = footPlacement.Prepare(planningFrame);
            }
            m_PosePlan.SetFootPlacementControl(
                footPlacementOperation.FootPlacementIndex,
                in control);
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
            m_FramePublisher.Invalidate();
            m_DiagnosticsPublisher.Invalidate();
            m_ReleasedSourceCount = 0;
            m_ActionSlotReleaseCompletions.Clear();
            ClearReleaseJournals();
            m_ActionBackendReleaseCompletions.Clear();
            m_LastCompletedFrame = default;
            m_PendingCompletedFrame = default;
            m_HasCompletedFrame = false;
            m_HasPendingCompletedFrame = false;
            m_PendingFrameOutcome = AnimationPresentationFrameOutcome.None;
            m_RecordReleaseDiagnostics = false;
            m_InertializationPlan.Reset();
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
                m_PosePlan.SetRootOrientationWarpControl(
                    i,
                    in control);
            }
            if (m_Projection.PosePlan.FootPlacementNodes.Count == 1)
            {
                CharacterFootPlacementNativeControl control = CharacterFootPlacementNativeControl.Inactive;
                m_PosePlan.SetFootPlacementControl(0, in control);
            }
            ReleaseDirectSources();
            ReleaseSequenceSources();
            ReleaseBlendSpaceSources();
            m_SourceBackend.Clear();
            m_PhysicalSources.Reset();
            m_CommitValidated = false;
            for (int port = 1; port < m_SourceFanIn.GetInputCount(); port++)
            {
                if (m_SourceFanIn.GetInput(port).IsValid())
                    m_SourceFanIn.DisconnectInput(port);
                m_SourceFanIn.SetInputWeight(port, 0f);
            }
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            m_FramePublisher.Invalidate();
            m_LastCompletedFrame = default;
            m_PendingCompletedFrame = default;
            m_HasCompletedFrame = false;
            m_HasPendingCompletedFrame = false;
            m_PendingFrameOutcome = AnimationPresentationFrameOutcome.None;
            Exception failure = null;
            DisposeStep(m_DiagnosticsPublisher.Dispose, ref failure);
            DisposeStep(RemoveJobs, ref failure);
            DisposeStep(m_SourceBackend.Dispose, ref failure);
            DisposeStep(RestoreOutputAndDestroyFanIn, ref failure);
            for (int i = m_Stacks.Length - 1; i >= 0; i--)
            {
                AnimationBlendStackRuntime stack = m_Stacks[i];
                if (stack != null)
                    DisposeStep(stack.Dispose, ref failure);
            }
            for (int i = m_DirectPlayers.Length - 1; i >= 0; i--)
            {
                AnimationSelectedPosePlayerRuntime player = m_DirectPlayers[i];
                if (player != null)
                    DisposeStep(player.Dispose, ref failure);
            }
            for (int i =
                     m_PoseStateSources.SequencePlayers.Length - 1;
                 i >= 0;
                 i--)
            {
                AnimationSequencePlayerRuntime player =
                    m_PoseStateSources.SequencePlayers[i];
                if (player != null)
                    DisposeStep(player.Dispose, ref failure);
            }
            for (int i =
                     m_PoseStateSources.BlendSpacePlayers.Length -
                     1;
                 i >= 0;
                 i--)
            {
                AnimationBlendSpacePlayerRuntime player =
                    m_PoseStateSources.BlendSpacePlayers[i];
                if (player != null)
                    DisposeStep(player.Dispose, ref failure);
            }
            DisposeStep(m_PhysicalSources.Dispose, ref failure);
            DisposeStep(m_PosePlan.Dispose, ref failure);
            DisposeStep(m_InertializationPlan.Dispose, ref failure);
            DisposeStep(m_Workspace.Dispose, ref failure);
            DisposeStep(RestoreGraphClock, ref failure);
            if (failure != null)
                throw failure;
        }

        void ConnectSource(
            AnimationPhysicalSourceIdentity physical,
            AnimationPoseSourcePrepareResult prepared)
        {
            int port = checked(physical.Index.Value + 1);
            Playable current = m_SourceFanIn.GetInput(port);
            if (current.IsValid() && current.Equals(prepared.Output))
            {
                m_SourceFanIn.SetInputWeight(port, 1f);
                return;
            }
            if (current.IsValid())
                m_SourceFanIn.DisconnectInput(port);
            m_SourceFanIn.GetGraph().Connect(prepared.Output, 0, m_SourceFanIn, port);
            m_SourceFanIn.SetInputWeight(port, 1f);
        }

        void PrepareStackSources(
            AnimationBlendStackRuntime stack,
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
                    entry.SourceId,
                    presentationDeltaSeconds,
                    actionSourceSamples,
                    providerSourceSamples);
            }
        }

        void PrepareSource(
            AnimationBlendStackRuntime stack,
            AnimationPoseSourceId sourceId,
            float presentationDeltaSeconds,
            IReadOnlyDictionary<AnimationPlayerSourceSampleKey,
                AnimationResolvedPoseSourceSample> actionSourceSamples,
            IReadOnlyDictionary<AnimationPlayerSourceSampleKey,
                PresentationPoseSourceSample> providerSourceSamples)
        {
            var key = new AnimationPlayerSourceSampleKey(stack.PoseNodeId, sourceId);
            AnimationResolvedPoseSourceSample sourceSample;
            if (sourceId.SourceKind ==
                AnimationPoseSourceKind.Timeline)
            {
                if (!actionSourceSamples.TryGetValue(
                        key,
                        out sourceSample))
                {
                    throw new InvalidOperationException(
                        $"Action Pose Source '{sourceId}' has no current resolved request.");
                }
            }
            else
            {
                if (!providerSourceSamples.TryGetValue(
                        key,
                        out PresentationPoseSourceSample
                            providerSample) ||
                    !m_SourceOwnerIndicesByNode.TryGetValue(
                        stack.PoseNodeId,
                        out int sourceOwnerIndex))
                {
                    throw new InvalidOperationException(
                        $"Presentation Pose Source '{sourceId}' has no current resolved request.");
                }
                sourceSample =
                    BuildResolvedProviderSample(
                        in providerSample,
                        sourceOwnerIndex);
            }
            AnimationPoseSampleRequest request =
                sourceSample.Request;
            AnimationPhysicalSourceIdentity physical = m_PhysicalSources.Register(
                request.SourceId,
                stack.PoseNodeId,
                request.SourceOwnerIndex);
            AnimationPoseSourceCaptureBinding capture = stack.PrepareCapture(
                in sourceSample,
                presentationDeltaSeconds);
            AnimationReadOnlyBuffer<AnimationPoseSourceClipBinding> clipCatalog =
                sourceId.SourceKind == AnimationPoseSourceKind.Timeline
                    ? BuildActionClipCatalog(request.SourceOwnerIndex)
                    : BuildMotionMatchingClipCatalog(providerSourceSamples[key]);
            AnimationPoseSourcePrepareResult prepared = m_SourceBackend.PrepareOrUpdate(
                in request,
                clipCatalog,
                in capture,
                stack.PoseNodeId);
            ConnectSource(physical, prepared);
        }

        void PrepareDirectSource(
            int playerIndex,
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
            AnimationPhysicalSourceIdentity physical = m_PhysicalSources.Register(
                player.SourceId,
                player.NodeId,
                player.SourceOwnerIndex);
            AnimationPoseSourceCaptureBinding capture = player.PrepareCapture(in sample, presentationDeltaSeconds);
            AnimationReadOnlyBuffer<AnimationPoseSourceClipBinding> clipCatalog =
                BuildMotionMatchingClipCatalog(sample);
            AnimationPoseSourcePrepareResult prepared = m_SourceBackend.PrepareOrUpdate(
                player.SourceId,
                sample.Clips,
                clipCatalog,
                in capture,
                player.NodeId);
            ConnectSource(physical, prepared);
            m_DirectPhysicalSources[playerIndex] = physical;
            m_DirectSourceIndices[playerIndex] = capture.SourceIndex;
        }

        static AnimationResolvedPoseSourceSample
            BuildResolvedProviderSample(
                in PresentationPoseSourceSample sample,
                int sourceOwnerIndex)
        {
            if (!sample.IsValid ||
                sample.Availability !=
                    PresentationPoseSourceAvailability.Ready ||
                sourceOwnerIndex < 0)
            {
                throw new ArgumentException(
                    "Presentation Pose source sample cannot be lowered.");
            }
            var sourceId = new AnimationPoseSourceId(
                sample.SourceIndex,
                sample.SourceKind,
                new AnimationPoseSelectionGeneration(
                    sample.SourceGeneration.Value));
            PresentationPoseSampleTime time =
                sample.EffectiveSample;
            var request = new AnimationPoseSampleRequest(
                sourceId,
                sample.SourcePoseContinuityIdentity,
                sample.FrameSequence,
                sourceOwnerIndex,
                default,
                time.SampleTime,
                time.ContinuousTime,
                time.Cycle,
                time.Loop,
                time.TimeScale,
                sample.Clips,
                sample.ParameterPageId,
                sample.PoseParameters,
                sample.PoseParameterAvailability);
            return new AnimationResolvedPoseSourceSample(
                request,
                sample.LeftFootFeatures,
                sample.RightFootFeatures,
                sample.HasFootFeatures);
        }

        void PrepareSequenceSource(int playerIndex, float presentationDeltaSeconds)
        {
            AnimationSequencePlayerRuntime player =
                m_PoseStateSources.SequencePlayers[playerIndex];
            if (!player.IsRelevant)
                return;
            AnimationPhysicalSourceIdentity physical = m_PhysicalSources.Register(
                player.SourceId,
                player.NodeId,
                player.PlayerIndex);
            AnimationPoseSourceCaptureBinding capture = player.PrepareCapture(presentationDeltaSeconds);
            AnimationReadOnlyBuffer<AnimationPoseSourceClipBinding> clipCatalog =
                BuildSequenceClipCatalog(player.SourceId);
            AnimationPoseSourcePrepareResult prepared = m_SourceBackend.PrepareOrUpdate(
                player.SourceId,
                player.ClipSamples,
                clipCatalog,
                in capture,
                player.NodeId);
            ConnectSource(physical, prepared);
            m_SequencePhysicalSources[playerIndex] = physical;
            m_SequenceSourceIndices[playerIndex] = capture.SourceIndex;
        }

        void PrepareBlendSpaceSource(
            int playerIndex,
            float presentationDeltaSeconds)
        {
            AnimationBlendSpacePlayerRuntime player =
                m_PoseStateSources.BlendSpacePlayers[
                    playerIndex];
            if (!player.IsRelevant)
                return;
            AnimationPhysicalSourceIdentity physical = m_PhysicalSources.Register(
                player.SourceId,
                player.NodeId,
                player.PlayerIndex);
            AnimationPoseSourceCaptureBinding capture =
                player.PrepareCapture(presentationDeltaSeconds);
            AnimationReadOnlyBuffer<AnimationPoseSourceClipBinding> clipCatalog =
                BuildBlendSpaceClipCatalog(playerIndex);
            AnimationPoseSourcePrepareResult prepared =
                m_SourceBackend.PrepareOrUpdate(
                    player.SourceId,
                    player.ClipSamples,
                    clipCatalog,
                    in capture,
                    player.NodeId);
            ConnectSource(physical, prepared);
            m_BlendSpacePhysicalSources[playerIndex] = physical;
            m_BlendSpaceSourceIndices[playerIndex] = capture.SourceIndex;
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
                m_SequencePlayerPlayables = new AnimationScriptPlayable[m_SequencePlayerJobs.Length];
                for (int i = 0; i < m_SequencePlayerJobs.Length; i++)
                {
                    m_SequencePlayerPlayables[i] = m_Animancer.Graph.InsertOutputJob(m_SequencePlayerJobs[i]);
                    m_SequencePlayerPlayables[i].SetProcessInputs(true);
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
            for (int i = 0; i < m_SequencePlayerJobs.Length; i++)
                m_SequencePlayerPlayables[i].SetJobData(m_SequencePlayerJobs[i]);
            for (int i = 0; i < m_DirectPlayerJobs.Length; i++)
                m_DirectPlayerPlayables[i].SetJobData(m_DirectPlayerJobs[i]);
            for (int i = 0; i < m_SlotJobs.Length; i++)
                m_SlotPlayables[i].SetJobData(m_SlotJobs[i]);
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
                    AnimationPhysicalSourceIdentity physical =
                        m_PhysicalSources.RequireIdentity(
                            release.SourceId,
                            release.PoseNodeId);
                    AnimationPhysicalSourceReleaseToken physicalRelease =
                        m_PhysicalSources.PrepareRelease(
                            physical,
                            release.SourceId);
                    int port = checked(physical.Index.Value + 1);
                    if (m_SourceFanIn.GetInput(port).IsValid())
                        m_SourceFanIn.DisconnectInput(port);
                    m_SourceFanIn.SetInputWeight(port, 0f);
                    stack.ApplyPreparedRelease(
                        in stackRelease);
                    m_PhysicalSources.ApplyPreparedRelease(
                        in physicalRelease);
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
            for (int i = m_SequencePlayerPlayables.Length - 1; i >= 0; i--)
                AnimancerUtilities.RemovePlayable(m_SequencePlayerPlayables[i]);
            for (int i = m_BlendSpacePlayerPlayables.Length - 1; i >= 0; i--)
                AnimancerUtilities.RemovePlayable(m_BlendSpacePlayerPlayables[i]);
            m_JobsInstalled = false;
        }

        void RestoreOutputAndDestroyFanIn()
        {
            if (!m_SourceFanIn.IsValid() || !m_Animancer || !m_Animancer.IsGraphInitialized)
                return;
            PlayableOutput output = m_Animancer.Graph.Output;
            if (output.IsOutputValid() && output.GetSourcePlayable().Equals(m_SourceFanIn))
            {
                output.SetSourcePlayable(m_PreviousOutputSource);
                output.SetWeight(m_PreviousOutputWeight);
            }
            m_SourceFanIn.Destroy();
        }

        ulong NextCompletionIdentity()
        {
            if (m_CompletionIdentity == ulong.MaxValue)
                throw new InvalidOperationException("Animation Pose completion identity was exhausted.");
            m_CompletionIdentity++;
            return m_CompletionIdentity;
        }

        void StageCompletedSources(
            ulong completionIdentity,
            bool recordDiagnostics)
        {
            if (m_PendingPoseSourceReleases.Count != 0)
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
                    AnimationPhysicalSourceIdentity physical =
                        m_PhysicalSources.RequireIdentity(
                            release.SourceId,
                            release.PoseNodeId);
                    if (route.IsAnimationSlot &&
                        IsFiniteActionSource(
                            release.SourceId))
                    {
                        AddPendingActionBackendRelease(
                            route.SlotId,
                            stack,
                            route,
                            in stackRelease,
                            physical);
                        AddActionSlotReleaseCompletion(
                            new AnimationSlotSourceReleaseCompletion(
                                route.SlotId,
                                release.SourceId.PlaybackId,
                                release.SourceId,
                                release.CompletionIdentity));
                        continue;
                    }
                    if (recordDiagnostics)
                    {
                        if (m_ReleasedSourceCount >=
                            m_ReleasedSources.Length)
                        {
                            throw new InvalidOperationException(
                                "Animation diagnostics release capacity was exceeded.");
                        }
                        m_ReleasedSources[m_ReleasedSourceCount++] =
                            new AnimationReleasedPoseSourceSnapshot(
                                release.PoseNodeId,
                                release.SourceId,
                                release.CompletionIdentity);
                    }
                    PendingPoseSourceRelease pending =
                        RentPendingPoseSourceRelease();
                    pending.Stack = stack;
                    pending.Route = route;
                    pending.StackRelease = stackRelease;
                    pending.PhysicalSource = physical;
                    AddFixed(
                        m_PendingPoseSourceReleases,
                        pending,
                        "Pending Pose source release journal");
                }
            }
        }

        void ExecutePendingPoseSourceReleases()
        {
            for (int releaseIndex = 0;
                 releaseIndex < m_PendingPoseSourceReleases.Count;
                 releaseIndex++)
            {
                PendingPoseSourceRelease pending =
                    m_PendingPoseSourceReleases[releaseIndex];
                CharacterAnimationTransitionRouteRuntime route =
                    pending.Route;
                int port = checked(
                    pending.PhysicalSource.Index.Value + 1);
                if (m_SourceFanIn.GetInput(port).IsValid())
                    m_SourceFanIn.DisconnectInput(port);
                m_SourceFanIn.SetInputWeight(port, 0f);
                m_SourceBackend.Release(
                    in pending.BackendRelease);
                pending.Stack.ApplyPreparedRelease(
                    in pending.StackRelease);
                m_PhysicalSources.ApplyPreparedRelease(
                    in pending.PhysicalRelease);
                bool notifyRoute =
                    pending.NotifyRouteAfterApply;
                pending.Clear();
                if (notifyRoute)
                    route.NotifySourcesReleased();
            }
            m_PendingPoseSourceReleases.Clear();
        }

        ulong NextActionBackendReleaseRequestIdentity()
        {
            m_ActionBackendReleaseRequestIdentity++;
            if (m_ActionBackendReleaseRequestIdentity == 0)
            {
                throw new InvalidOperationException(
                    "Action backend release request identity was exhausted.");
            }
            return m_ActionBackendReleaseRequestIdentity;
        }

        ulong NextActionBackendReleaseCompletionIdentity()
        {
            m_ActionBackendReleaseCompletionIdentity++;
            if (m_ActionBackendReleaseCompletionIdentity == 0)
            {
                throw new InvalidOperationException(
                    "Action backend release completion identity was exhausted.");
            }
            return m_ActionBackendReleaseCompletionIdentity;
        }

        PendingPoseSourceRelease RentPendingPoseSourceRelease()
        {
            for (int i = 0;
                 i < m_PendingPoseSourceReleasePool.Length;
                 i++)
            {
                PendingPoseSourceRelease candidate =
                    m_PendingPoseSourceReleasePool[i];
                if (candidate.InUse)
                    continue;
                candidate.InUse = true;
                return candidate;
            }
            throw new InvalidOperationException(
                "Pending Pose source release journal capacity was exceeded.");
        }

        PendingActionBackendRelease RentPendingActionBackendRelease()
        {
            for (int i = 0;
                 i < m_PendingActionBackendReleasePool.Length;
                 i++)
            {
                PendingActionBackendRelease candidate =
                    m_PendingActionBackendReleasePool[i];
                if (candidate.InUse)
                    continue;
                candidate.InUse = true;
                return candidate;
            }
            throw new InvalidOperationException(
                "Pending Action backend release journal capacity was exceeded.");
        }

        PreparedActionBackendRelease RentPreparedActionBackendRelease()
        {
            for (int i = 0;
                 i < m_PreparedActionBackendReleasePool.Length;
                 i++)
            {
                PreparedActionBackendRelease candidate =
                    m_PreparedActionBackendReleasePool[i];
                if (candidate.InUse)
                    continue;
                candidate.InUse = true;
                return candidate;
            }
            throw new InvalidOperationException(
                "Prepared Action backend release journal capacity was exceeded.");
        }

        static void ReturnPreparedActionBackendRelease(
            PreparedActionBackendRelease prepared)
        {
            if (prepared == null || !prepared.InUse)
            {
                throw new InvalidOperationException(
                    "Prepared Action backend release journal entry is not active.");
            }
            prepared.Clear();
        }

        void AddActionBackendReleaseCompletion(
            ActionBackendReleaseCompletion completion) =>
            AddFixed(
                m_ActionBackendReleaseCompletions,
                completion,
                "Action backend release completion journal");

        void AddActionSlotReleaseCompletion(
            AnimationSlotSourceReleaseCompletion completion) =>
            AddFixed(
                m_ActionSlotReleaseCompletions,
                completion,
                "Animation Slot source release completion journal");

        void ClearReleaseJournals()
        {
            for (int i = 0;
                 i < m_PreparedActionBackendReleases.Count;
                 i++)
            {
                m_PreparedActionBackendReleases[i].Clear();
            }
            m_PreparedActionBackendReleases.Clear();
            for (int i = 0;
                 i < m_PendingActionBackendReleases.Count;
                 i++)
            {
                m_PendingActionBackendReleases[i].Clear();
            }
            m_PendingActionBackendReleases.Clear();
            for (int i = 0;
                 i < m_PendingPoseSourceReleases.Count;
                 i++)
            {
                m_PendingPoseSourceReleases[i].Clear();
            }
            m_PendingPoseSourceReleases.Clear();
            m_PrepareActionBackendPendingScratch.Clear();
            m_PrepareActionBackendSourceScratch.Clear();
            m_ExpectedActionBackendSources.Clear();
            m_ReleaseValidationIdentities.Clear();
            Array.Clear(
                m_PreparedStandaloneSourceReleases,
                0,
                m_PreparedStandaloneSourceReleaseCount);
            m_PreparedStandaloneSourceReleaseCount = 0;
            ClearValidatedActionBackendAcknowledgements();
            ClearPreparedMotionMatchingPoseCompletion();
        }

        void DiscardPendingReleasePreparation()
        {
            for (int i = 0;
                 i < m_PreparedActionBackendReleases.Count;
                 i++)
            {
                PreparedActionBackendRelease prepared =
                    m_PreparedActionBackendReleases[i];
                for (int sourceIndex = 0;
                     sourceIndex < prepared.Sources.Count;
                     sourceIndex++)
                {
                    PendingActionBackendRelease source =
                        prepared.Sources[sourceIndex];
                    source.RequestIdentity = 0;
                    source.PlayableCompletionIdentity = 0;
                    source.StoredPoseCompletionIdentity = 0;
                    source.PhysicalRelease = default;
                    source.BackendRelease = default;
                    source.NotifyRouteAfterApply = false;
                }
                prepared.Clear();
            }
            m_PreparedActionBackendReleases.Clear();
            int committedCount =
                m_PendingActionBackendReleaseFrameStartCount;
            if (committedCount < 0 ||
                committedCount >
                m_PendingActionBackendReleases.Count)
            {
                throw new InvalidOperationException(
                    "Pending Action backend release frame boundary is invalid.");
            }
            for (int i = committedCount;
                 i < m_PendingActionBackendReleases.Count;
                 i++)
            {
                m_PendingActionBackendReleases[i].Clear();
            }
            if (committedCount <
                m_PendingActionBackendReleases.Count)
            {
                m_PendingActionBackendReleases.RemoveRange(
                    committedCount,
                    m_PendingActionBackendReleases.Count -
                    committedCount);
            }
            for (int i = 0;
                 i < m_PendingPoseSourceReleases.Count;
                 i++)
            {
                m_PendingPoseSourceReleases[i].Clear();
            }
            m_PendingPoseSourceReleases.Clear();
            m_PrepareActionBackendPendingScratch.Clear();
            m_PrepareActionBackendSourceScratch.Clear();
            m_ExpectedActionBackendSources.Clear();
            m_PendingActionBackendReleaseFrameStartCount = 0;
        }

        static void AddFixed<T>(
            List<T> destination,
            T value,
            string journalName)
        {
            if (destination.Count >= destination.Capacity)
            {
                throw new InvalidOperationException(
                    $"{journalName} capacity was exceeded.");
            }
            destination.Add(value);
        }

        void AddPendingActionBackendRelease(
            AnimationSlotId slotId,
            AnimationBlendStackRuntime stack,
            CharacterAnimationTransitionRouteRuntime route,
            in AnimationBlendStackSourceReleaseToken stackRelease,
            AnimationPhysicalSourceIdentity physical)
        {
            AnimationBlendStackRelease release =
                stackRelease.Release;
            if (!slotId.IsValid ||
                stack == null ||
                route == null ||
                !stackRelease.IsValid ||
                !physical.IsValid ||
                !IsFiniteActionSource(release.SourceId))
            {
                throw new ArgumentException(
                    "Pending Action backend release is invalid.");
            }
            for (int i = 0;
                 i < m_PendingActionBackendReleases.Count;
                 i++)
            {
                PendingActionBackendRelease existing =
                    m_PendingActionBackendReleases[i];
                if (existing.Release.SourceId.Equals(
                        release.SourceId) &&
                    existing.Release.PoseNodeId ==
                        release.PoseNodeId)
                {
                    throw new InvalidOperationException(
                        $"Action source '{release.SourceId}' already waits for backend release.");
                }
            }
            int resourceIndex = physical.Index.Value;
            if ((uint)resourceIndex >=
                (uint)m_PlayableBackendResourceIds.Length)
            {
                throw new InvalidOperationException(
                    "Action backend physical source index exceeds the release journal capacity.");
            }
            PendingActionBackendRelease pending =
                RentPendingActionBackendRelease();
            pending.SlotId = slotId;
            pending.Stack = stack;
            pending.Route = route;
            pending.StackRelease = stackRelease;
            pending.PhysicalSource = physical;
            pending.PlayableSource =
                new ActionBackendSourceIdentity(
                    ActionBackendSourceKind.Playable,
                    m_PlayableBackendResourceIds[resourceIndex],
                    physical.Generation);
            pending.StoredPoseSource =
                new ActionBackendSourceIdentity(
                    ActionBackendSourceKind.StoredPoseCapture,
                    m_StoredPoseBackendResourceIds[resourceIndex],
                    physical.Generation);
            AddFixed(
                m_PendingActionBackendReleases,
                pending,
                "Pending Action backend release journal");
        }

        void PrepareRouteReleaseNotifications()
        {
            for (int i = 0;
                 i < m_PendingPoseSourceReleases.Count;
                 i++)
            {
                m_PendingPoseSourceReleases[i]
                    .NotifyRouteAfterApply = false;
            }
            for (int i = 0;
                 i < m_PendingActionBackendReleases.Count;
                 i++)
            {
                m_PendingActionBackendReleases[i]
                    .NotifyRouteAfterApply = false;
            }
            for (int poseIndex = 0;
                 poseIndex < m_PendingPoseSourceReleases.Count;
                 poseIndex++)
            {
                PendingPoseSourceRelease candidate =
                    m_PendingPoseSourceReleases[poseIndex];
                if (!HasActionRouteRelease(
                        candidate.Route,
                        candidate.Release.CompletionIdentity) &&
                    !HasLaterPoseRouteRelease(
                        poseIndex,
                        candidate.Route,
                        candidate.Release.CompletionIdentity))
                {
                    candidate.NotifyRouteAfterApply = true;
                }
            }
            for (int requestIndex = 0;
                 requestIndex < m_PreparedActionBackendReleases.Count;
                 requestIndex++)
            {
                PreparedActionBackendRelease request =
                    m_PreparedActionBackendReleases[requestIndex];
                for (int sourceIndex = 0;
                     sourceIndex < request.Sources.Count;
                     sourceIndex++)
                {
                    PendingActionBackendRelease candidate =
                        request.Sources[sourceIndex];
                    if (!HasLaterPreparedActionRouteRelease(
                            requestIndex,
                            sourceIndex,
                            candidate.Route,
                            candidate.Release.CompletionIdentity))
                    {
                        candidate.NotifyRouteAfterApply = true;
                    }
                }
            }
        }

        bool HasActionRouteRelease(
            CharacterAnimationTransitionRouteRuntime route,
            ulong completionIdentity)
        {
            for (int i = 0;
                 i < m_PendingActionBackendReleases.Count;
                 i++)
            {
                PendingActionBackendRelease pending =
                    m_PendingActionBackendReleases[i];
                if (ReferenceEquals(pending.Route, route) &&
                    pending.Release.CompletionIdentity ==
                        completionIdentity)
                {
                    return true;
                }
            }
            return false;
        }

        bool HasLaterPoseRouteRelease(
            int currentIndex,
            CharacterAnimationTransitionRouteRuntime route,
            ulong completionIdentity)
        {
            for (int i = currentIndex + 1;
                 i < m_PendingPoseSourceReleases.Count;
                 i++)
            {
                PendingPoseSourceRelease pending =
                    m_PendingPoseSourceReleases[i];
                if (ReferenceEquals(pending.Route, route) &&
                    pending.Release.CompletionIdentity ==
                        completionIdentity)
                {
                    return true;
                }
            }
            return false;
        }

        bool HasLaterPreparedActionRouteRelease(
            int currentRequestIndex,
            int currentSourceIndex,
            CharacterAnimationTransitionRouteRuntime route,
            ulong completionIdentity)
        {
            for (int requestIndex = currentRequestIndex;
                 requestIndex < m_PreparedActionBackendReleases.Count;
                 requestIndex++)
            {
                PreparedActionBackendRelease request =
                    m_PreparedActionBackendReleases[requestIndex];
                int sourceStart = requestIndex == currentRequestIndex
                    ? currentSourceIndex + 1
                    : 0;
                for (int sourceIndex = sourceStart;
                     sourceIndex < request.Sources.Count;
                     sourceIndex++)
                {
                    PendingActionBackendRelease pending =
                        request.Sources[sourceIndex];
                    if (ReferenceEquals(pending.Route, route) &&
                        pending.Release.CompletionIdentity ==
                            completionIdentity)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        static bool IsFiniteActionSource(
            AnimationPoseSourceId sourceId) =>
            sourceId.IsValid &&
            sourceId.SourceKind ==
                AnimationPoseSourceKind.Timeline &&
            sourceId.SourceActionInstanceId != 0;

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
            m_ActionSlotReleaseCompletions.Clear();
            m_ReleasedSourceCount = 0;
            m_MotionMatchingSourceUsageCount = 0;
            m_MotionMatchingHistoryCompletionCount = 0;
        }

        void RequireMutation(
            PosePlanFrameLease lease)
        {
            RequireAlive();
            if (!lease.IsValid ||
                !m_ActiveFrameLease.IsValid ||
                lease.FrameIdentity !=
                    m_ActiveFrameLease.FrameIdentity ||
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

        AnimationPhysicalSourceIdentity
            ValidatePhysicalRelease(
                AnimationPoseSourceId sourceId,
                PoseNodeId nodeId,
                AnimationPhysicalSourceIdentity expected)
        {
            AnimationPhysicalSourceIdentity current =
                m_PhysicalSources.RequireIdentity(
                    sourceId,
                    nodeId);
            if (expected.IsValid &&
                current != expected)
            {
                throw new InvalidOperationException(
                    "Pose source release physical identity is stale.");
            }
            int port = checked(
                current.Index.Value + 1);
            if (port <= 0 ||
                port >= m_SourceFanIn.GetInputCount() ||
                !m_SourceBackend.ContainsCommitted(
                    sourceId,
                    nodeId) ||
                !m_ReleaseValidationIdentities.Add(
                    current))
            {
                throw new InvalidOperationException(
                    "Pose source release backend identity is not exact.");
            }
            return current;
        }

        void AddPreparedStandaloneSourceRelease(
            StandaloneSourceReleaseOwner owner,
            int playerIndex,
            AnimationPoseSourceId sourceId,
            PoseNodeId nodeId,
            AnimationPhysicalSourceIdentity physicalSource,
            AnimationPhysicalSourceReleaseToken physicalRelease,
            AnimationPoseSourceReleaseToken backendRelease,
            in AnimationPlayerReleaseToken playerRelease)
        {
            if (owner == 0 ||
                playerIndex < 0 ||
                !sourceId.IsValid ||
                !nodeId.IsValid ||
                !physicalSource.IsValid ||
                !physicalRelease.IsValid ||
                !backendRelease.IsValid ||
                !playerRelease.IsValid ||
                m_PreparedStandaloneSourceReleaseCount >=
                m_PreparedStandaloneSourceReleases.Length)
            {
                throw new InvalidOperationException(
                    "Standalone pose source release exceeds its compiled journal.");
            }
            m_PreparedStandaloneSourceReleases[
                m_PreparedStandaloneSourceReleaseCount++] =
                new PreparedStandaloneSourceRelease
                {
                    Owner = owner,
                    PlayerIndex = playerIndex,
                    SourceId = sourceId,
                    NodeId = nodeId,
                    PhysicalSource = physicalSource,
                    PhysicalRelease = physicalRelease,
                    BackendRelease = backendRelease,
                    PlayerRelease = playerRelease
                };
        }

        void DiscardPreparedPhysicalSource(
            AnimationPhysicalSourceIdentity physical)
        {
            int port = checked(physical.Index.Value + 1);
            if (m_SourceFanIn.GetInput(port).IsValid())
                m_SourceFanIn.DisconnectInput(port);
            m_SourceFanIn.SetInputWeight(port, 0f);
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
                    AnimationPhysicalSourceIdentity physical = m_PhysicalSources.RequireIdentity(sourceId, player.NodeId);
                    AnimationPhysicalSourceReleaseToken physicalRelease =
                        m_PhysicalSources.PrepareRelease(
                            physical,
                            sourceId);
                    int port = checked(physical.Index.Value + 1);
                    if (m_SourceFanIn.GetInput(port).IsValid())
                        m_SourceFanIn.DisconnectInput(port);
                    m_SourceFanIn.SetInputWeight(port, 0f);
                    m_PhysicalSources.ApplyPreparedRelease(
                        in physicalRelease);
                    player.ApplyPreparedRelease(
                        in playerRelease);
                }
            }
        }

        void ReleaseSequenceSources()
        {
            for (int playerIndex = 0;
                 playerIndex <
                 m_PoseStateSources.SequencePlayers.Length;
                 playerIndex++)
            {
                AnimationSequencePlayerRuntime player =
                    m_PoseStateSources.SequencePlayers[
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
                    AnimationPhysicalSourceIdentity physical =
                        m_PhysicalSources.RequireIdentity(sourceId, player.NodeId);
                    AnimationPhysicalSourceReleaseToken physicalRelease =
                        m_PhysicalSources.PrepareRelease(
                            physical,
                            sourceId);
                    int port = checked(physical.Index.Value + 1);
                    if (m_SourceFanIn.GetInput(port).IsValid())
                        m_SourceFanIn.DisconnectInput(port);
                    m_SourceFanIn.SetInputWeight(port, 0f);
                    m_PhysicalSources.ApplyPreparedRelease(
                        in physicalRelease);
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
                    AnimationPhysicalSourceIdentity physical =
                        m_PhysicalSources.RequireIdentity(
                            sourceId,
                            player.NodeId);
                    AnimationPhysicalSourceReleaseToken physicalRelease =
                        m_PhysicalSources.PrepareRelease(
                            physical,
                            sourceId);
                    int port = checked(physical.Index.Value + 1);
                    if (m_SourceFanIn.GetInput(port).IsValid())
                        m_SourceFanIn.DisconnectInput(port);
                    m_SourceFanIn.SetInputWeight(port, 0f);
                    m_PhysicalSources.ApplyPreparedRelease(
                        in physicalRelease);
                    player.ApplyPreparedRelease(
                        in playerRelease);
                }
            }
        }

        static CharacterPresentationPoseOperation RequireBlendStackOperation(
            CharacterPresentationPosePlan plan,
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
            CharacterPresentationPosePlan plan,
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
            if (m_StacksByNode.TryGetValue(playerNodeId, out AnimationBlendStackRuntime stack))
            {
                if (sample.PlayerNodeId != playerNodeId ||
                    sample.SourceKind !=
                        AnimationPoseSourceKind.MotionMatching ||
                    !m_SourceOwnerIndicesByNode.TryGetValue(
                        playerNodeId,
                        out int sourceOwnerIndex))
                {
                    throw new InvalidOperationException(
                        $"Motion Matching Selection does not belong to Pose State Player '{playerNodeId}'.");
                }
                AnimationResolvedPoseSourceSample resolved =
                    BuildResolvedProviderSample(
                        in sample,
                        sourceOwnerIndex);
                AnimationPoseSampleRequest request =
                    resolved.Request;
                m_StackRoutesByNode[playerNodeId]
                    .PushSelection(stack, in request);
                return;
            }
            if (m_DirectPlayersByNode.TryGetValue(
                    playerNodeId,
                    out AnimationSelectedPosePlayerRuntime player))
            {
                player.PushSelection(in sample);
                return;
            }
            throw new InvalidOperationException(
                $"Motion Matching Pose State Player '{playerNodeId}' is not installed in the active Pose Plan.");
        }

        void AddMotionMatchingSourceUsage(
            PoseNodeId playerNodeId,
            AnimationPoseSourceId sourceId,
            ulong completionIdentity)
        {
            for (int i = 0; i < m_MotionMatchingSourceUsageCount; i++)
            {
                MotionMatchingPosePlanSourceUsage usage = m_MotionMatchingSourceUsages[i];
                if (usage.PlayerNodeId == playerNodeId && usage.SourceId.Equals(sourceId))
                    return;
            }
            if (m_MotionMatchingSourceUsageCount >= m_MotionMatchingSourceUsages.Length)
                throw new InvalidOperationException("Motion Matching Pose Plan source usage capacity was exceeded.");
            m_MotionMatchingSourceUsages[m_MotionMatchingSourceUsageCount++] =
                new MotionMatchingPosePlanSourceUsage(
                    playerNodeId,
                    sourceId,
                    completionIdentity);
        }

        bool PlayerUsesSource(PoseNodeId playerNodeId, AnimationPoseSourceId sourceId)
        {
            if (m_StacksByNode.TryGetValue(playerNodeId, out AnimationBlendStackRuntime stack))
            {
                for (int i = 0; i < stack.EntryCount; i++)
                {
                    AnimationBlendEntryId entry = stack.GetEntryId(i);
                    if (!entry.SourcePoseTarget && entry.SourceId.Equals(sourceId))
                        return true;
                }
                return false;
            }
            return m_DirectPlayersByNode.TryGetValue(playerNodeId, out AnimationSelectedPosePlayerRuntime player) &&
                   player.HasSelection && player.SourceId.Equals(sourceId);
        }

        AnimationReadOnlyBuffer<AnimationPoseSourceClipBinding>
            BuildActionClipCatalog(int producerIndex)
        {
            if ((uint)producerIndex >= (uint)m_Projection.Producers.Count)
                throw new ArgumentOutOfRangeException(nameof(producerIndex));
            CharacterPresentationAnimationBinding animation =
                m_Projection.Producers[producerIndex]?.Animation ??
                throw new InvalidOperationException(
                    $"Animation producer #{producerIndex} has no clip catalog.");
            if (animation.Clips.Count == 0 ||
                animation.Clips.Count > m_ClipCatalogScratch.Length)
            {
                throw new InvalidOperationException(
                    $"Animation producer #{producerIndex} clip catalog exceeds its compiled capacity.");
            }
            for (int i = 0; i < animation.Clips.Count; i++)
            {
                CharacterPresentationAnimationClipBinding binding =
                    animation.Clips[i] ??
                    throw new InvalidOperationException(
                        $"Animation producer #{producerIndex} clip binding #{i} is missing.");
                m_ClipCatalogScratch[i] =
                    new AnimationPoseSourceClipBinding(i, binding.Clip);
            }
            return new AnimationReadOnlyBuffer<AnimationPoseSourceClipBinding>(
                m_ClipCatalogScratch,
                0,
                animation.Clips.Count);
        }

        AnimationReadOnlyBuffer<AnimationPoseSourceClipBinding>
            BuildSequenceClipCatalog(AnimationPoseSourceId sourceId)
        {
            if (sourceId.SourceKind != AnimationPoseSourceKind.Sequence ||
                !m_Projection.TryGetPoseSource(
                    sourceId.PresentationPoseSourceIndex,
                    out CharacterPresentationPoseSourcePlan source))
            {
                throw new InvalidOperationException(
                    $"Sequence source '{sourceId}' has no compiled clip catalog.");
            }
            m_ClipCatalogScratch[0] =
                new AnimationPoseSourceClipBinding(0, source.Clip);
            return new AnimationReadOnlyBuffer<AnimationPoseSourceClipBinding>(
                m_ClipCatalogScratch,
                0,
                1);
        }

        AnimationReadOnlyBuffer<AnimationPoseSourceClipBinding>
            BuildBlendSpaceClipCatalog(int playerIndex)
        {
            CharacterAnimationBlendSpacePlayerPlan player =
                m_Projection.BlendSpacePlayers[playerIndex];
            CharacterAnimationBlendSpacePlan plan =
                m_Projection.BlendSpaces[player.BlendSpacePlanIndex];
            if (plan.Samples.Count == 0 ||
                plan.Samples.Count > m_ClipCatalogScratch.Length)
            {
                throw new InvalidOperationException(
                    $"Blend Space Player '{player.NodeId}' clip catalog exceeds its compiled capacity.");
            }
            for (int i = 0; i < plan.Samples.Count; i++)
            {
                CharacterAnimationBlendSpaceSamplePlan sample =
                    plan.Samples[i] ??
                    throw new InvalidOperationException(
                        $"Blend Space Player '{player.NodeId}' sample #{i} is missing.");
                m_ClipCatalogScratch[i] =
                    new AnimationPoseSourceClipBinding(i, sample.Clip);
            }
            return new AnimationReadOnlyBuffer<AnimationPoseSourceClipBinding>(
                m_ClipCatalogScratch,
                0,
                plan.Samples.Count);
        }

        AnimationReadOnlyBuffer<AnimationPoseSourceClipBinding>
            BuildMotionMatchingClipCatalog(
                in PresentationPoseSourceSample sample)
        {
            MotionMatchingProjectionPayload motionMatching =
                m_Projection.MotionMatching;
            if (sample.SourceKind != AnimationPoseSourceKind.MotionMatching ||
                motionMatching == null ||
                (uint)sample.ProjectionDatabaseIndex >=
                (uint)motionMatching.DatabaseCount)
            {
                throw new InvalidOperationException(
                    $"Motion Matching source '{sample.SourceIndex}' has no exact Database catalog.");
            }
            MotionMatchingDatabasePayload database =
                motionMatching.GetDatabase(
                    sample.ProjectionDatabaseIndex);
            if (database == null || database.ClipBindingCount == 0 ||
                database.ClipBindingCount > m_ClipCatalogScratch.Length)
            {
                throw new InvalidOperationException(
                    $"Motion Matching Database #{sample.ProjectionDatabaseIndex} clip catalog exceeds its compiled capacity.");
            }
            for (int i = 0; i < database.ClipBindingCount; i++)
            {
                MotionMatchingClipBindingPayload binding =
                    database.GetClipBinding(i) ??
                    throw new InvalidOperationException(
                        $"Motion Matching Database #{sample.ProjectionDatabaseIndex} clip binding #{i} is missing.");
                m_ClipCatalogScratch[i] =
                    new AnimationPoseSourceClipBinding(i, binding.Clip);
            }
            return new AnimationReadOnlyBuffer<AnimationPoseSourceClipBinding>(
                m_ClipCatalogScratch,
                0,
                database.ClipBindingCount);
        }

        static int CalculateSourceCapacity(
            CharacterPresentationPosePlan plan)
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
