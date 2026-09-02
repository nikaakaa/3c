using System;
using System.Collections.Generic;
using Animancer;
using ThirdPersonCharacter.Pipeline.Animation.BlendStack;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Pipeline.Animation.Lifecycle;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;
using ThirdPersonCharacter.Pipeline.Animation.Presentation;
using ThirdPersonCharacter.Pipeline.Animation.Sources;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonSimulation;
using Unity.Collections;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal sealed class CharacterPoseProgramRuntime : IDisposable
    {
        readonly CharacterPoseProgramImage m_Image;
        readonly AnimancerComponent m_Animancer;
        readonly CharacterPoseSourceModule m_SourceModule;
        readonly CharacterPoseWorldContextAdapter m_WorldContext;
        readonly PresentationFrameWorkspace m_PresentationWorkspace;
        readonly CharacterPoseProgramMotionMatchingRuntime m_MotionMatching;
        readonly CharacterPoseProgramTuningRuntime m_Tuning;
        readonly List<ActionBackendReleaseCompletion>
            m_ActionBackendReleaseCompletions;
        readonly List<AnimationSlotSourceReleaseCompletion>
            m_ActionSlotReleaseCompletions;
        readonly FixedCapacityFrameBuffer<ActionBackendReleaseCompletion>
            m_ConsumedActionBackendReleaseCompletions;
        readonly FixedCapacityFrameBuffer<AnimationPlaybackId>
            m_RetiredActionPlaybacks;
        readonly AnimationSlotBlendJob[] m_SlotJobs;
        readonly AnimationSelectedPosePlayerJob[] m_DirectPlayerJobs;
        readonly AnimationSelectedPosePlayerJob[] m_ClipPlayerJobs;
        readonly AnimationSelectedPosePlayerJob[] m_BlendSpacePlayerJobs;
        AnimationScriptPlayable[] m_SlotPlayables;
        AnimationScriptPlayable[] m_DirectPlayerPlayables;
        AnimationScriptPlayable[] m_ClipPlayerPlayables;
        AnimationScriptPlayable[] m_BlendSpacePlayerPlayables;
        CharacterActionPlaybackFrameTransaction m_ActionFrame;
        AnimationSlotMutationLease m_AnimationSlotFrame;
        PresentationFrameWorkspaceLease m_PresentationWorkspaceFrame;
        CharacterPoseProgramFrameLease m_ActiveFrameLease;
        CharacterPoseProgramFrameLease m_CommittingFrameLease;
        readonly CharacterPoseProgramEvaluationState m_Evaluation =
            new CharacterPoseProgramEvaluationState();
        int m_SequencePreviewPlayerIndex = -1;
        int m_SequencePreviewOperationIndex = -1;
        double m_SequencePreviewTime;
        bool m_SequencePreviewReset;
        bool m_HasSequencePreview;
        IReadOnlyList<AnimationSlotFramePlan> m_AnimationSlotPlans =
            Array.Empty<AnimationSlotFramePlan>();
        bool m_JobsInstalled;
        bool m_Disposed;

        internal CharacterPoseProgramRuntime(
            AnimancerComponent animancer,
            CharacterPoseProgramImage image,
            CharacterPoseProgramExecutionView executionView,
            CharacterPoseActorState actorState,
            CharacterPoseProgramFramePages framePages,
            CharacterPoseProgramTuningState tuning,
            CharacterPoseSourceModule sourceModule,
            CharacterPoseWorldContextAdapter worldContext,
            CharacterPoseConstraintRuntime poseConstraints,
            PresentationFrameWorkspace presentationWorkspace)
        {
            m_Animancer = animancer ? animancer :
                throw new ArgumentNullException(nameof(animancer));
            m_Image = image ?? throw new ArgumentNullException(nameof(image));
            ExecutionView = executionView ??
                throw new ArgumentNullException(nameof(executionView));
            ActorState = actorState ??
                throw new ArgumentNullException(nameof(actorState));
            FramePages = framePages ??
                throw new ArgumentNullException(nameof(framePages));
            m_Tuning = new CharacterPoseProgramTuningRuntime(
                tuning,
                ActorState);
            m_SourceModule = sourceModule ??
                throw new ArgumentNullException(nameof(sourceModule));
            m_WorldContext = worldContext ??
                throw new ArgumentNullException(nameof(worldContext));
            m_PresentationWorkspace = presentationWorkspace ??
                throw new ArgumentNullException(nameof(presentationWorkspace));
            CharacterPoseConstraintRuntime constraintRuntime = poseConstraints ??
                throw new ArgumentNullException(nameof(poseConstraints));
            m_MotionMatching =
                new CharacterPoseProgramMotionMatchingRuntime(
                    image,
                    ActorState,
                    FramePages,
                    m_Evaluation,
                    m_SourceModule,
                    m_PresentationWorkspace);
            int actionFrameCapacity = ActorState.ActionPlayback.FrameCapacity;
            int backendReleaseCompletionCapacity =
                ActorState.ActionPlayback.BackendReleaseCompletionCapacity;
            m_ActionBackendReleaseCompletions =
                new List<ActionBackendReleaseCompletion>(
                    backendReleaseCompletionCapacity);
            m_ActionSlotReleaseCompletions =
                new List<AnimationSlotSourceReleaseCompletion>(
                    actionFrameCapacity);
            m_ConsumedActionBackendReleaseCompletions =
                new FixedCapacityFrameBuffer<ActionBackendReleaseCompletion>(
                    backendReleaseCompletionCapacity);
            m_RetiredActionPlaybacks =
                new FixedCapacityFrameBuffer<AnimationPlaybackId>(
                    actionFrameCapacity);
            m_SlotJobs =
                new AnimationSlotBlendJob[ActorState.Stacks.Length];
            m_DirectPlayerJobs =
                new AnimationSelectedPosePlayerJob[
                    ActorState.DirectPlayers.Length];
            m_ClipPlayerJobs =
                new AnimationSelectedPosePlayerJob[
                    ActorState.PoseStateSources.ClipPlayers.Length];
            m_BlendSpacePlayerJobs =
                new AnimationSelectedPosePlayerJob[
                    ActorState.PoseStateSources.BlendSpacePlayers.Length];
            Executor = new CharacterPoseProgramExecutor(
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
        CharacterPoseProgramExecutor Executor { get; }
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
        internal bool HasOpenFrame => m_ActiveFrameLease.IsValid;
        internal bool HasPreparedEvaluation => m_Evaluation.HasPrepared;
        internal bool HasPendingEvaluationFrame =>
            FramePages.HasPendingEvaluationFrame;
        internal ulong PendingEvaluationCompletionIdentity =>
            FramePages.PendingEvaluationCompletionIdentity;
        internal long DenseDoublePageResidentPayloadBytes =>
            FramePages.DenseDoublePageResidentPayloadBytes;
        internal int SourceRetirementStandaloneCapacity =>
            ActorState.SourceRetirement.StandaloneCapacity;
        internal int PendingPoseSourceRetirementCount =>
            ActorState.SourceRetirement.PendingPoseCount;
        internal bool HasPreparedStandaloneSourceRetirement =>
            ActorState.SourceRetirement.HasPreparedStandalone;
        internal bool CanApplyNextActivation =>
            ActorState.PoseStateSources.CanApplyNextActivation;
        internal bool HasCommittedEvaluationFrame =>
            m_Evaluation.HasCommitted;
        internal bool HasPendingCompletedEvaluationFrame =>
            m_Evaluation.HasPendingCompleted;
        internal IReadOnlyList<ActionSlotSourceUsage> ActionSourceUsages =>
            m_PresentationWorkspace.ActionUsages;
        internal IReadOnlyList<PoseSourceProviderDemand> ProviderDemands =>
            m_PresentationWorkspace.ProviderDemands;
        internal IReadOnlyDictionary<AnimationPlayerSourceSampleKey,
            PresentationPoseSourceSample> ProviderSourceSamples =>
            m_MotionMatching.ProviderSourceSamples;
        internal int ProviderSourceSampleCount =>
            m_MotionMatching.ProviderSourceSampleCount;
        internal ulong CommittedEvaluationCompletionIdentity =>
            m_Evaluation.CommittedCompletionIdentity;
        internal ulong PendingCompletedEvaluationCompletionIdentity =>
            m_Evaluation.PendingCompletedCompletionIdentity;

        internal void BeginActionPlaybackFrame(
            ulong frameIdentity,
            ulong presentationFrame)
        {
            RequireAlive();
            if (m_ActionFrame?.IsValid == true ||
                m_AnimationSlotFrame.IsValid ||
                m_PresentationWorkspaceFrame.IsValid)
            {
                throw new InvalidOperationException(
                    "Character Pose Program Action frame is already open.");
            }
            m_PresentationWorkspaceFrame =
                m_PresentationWorkspace.Begin(
                    frameIdentity,
                    presentationFrame);
            try
            {
                m_ActionFrame = ActorState.ActionPlayback.BeginFrame(
                    frameIdentity,
                    presentationFrame);
            }
            catch
            {
                m_PresentationWorkspace.Discard(
                    m_PresentationWorkspaceFrame);
                m_PresentationWorkspaceFrame = default;
                throw;
            }
        }

        internal void BeginAnimationSlotFrame(ulong frameIdentity)
        {
            RequireActionFrame(frameIdentity);
            if (m_AnimationSlotFrame.IsValid)
            {
                throw new InvalidOperationException(
                    "Character Pose Program Animation Slot frame is already open.");
            }
            m_AnimationSlotFrame =
                ActorState.AnimationSlots.BeginFrame(frameIdentity);
        }

        internal void BeginActionSamplingFrame(
            ulong frameIdentity,
            ulong presentationFrame,
            bool captureDiagnostics)
        {
            RequireActionFrame(frameIdentity);
            m_SourceModule.BeginActionSamplingFrame(
                frameIdentity,
                presentationFrame,
                captureDiagnostics);
        }

        internal void CommitAnimationSlotFrame(
            CharacterPoseProgramFrameLease lease)
        {
            RequireFrame(lease);
            RequireAnimationSlotFrame(lease.Lineage.FrameIdentity);
            ActorState.AnimationSlots.CommitFrame(m_AnimationSlotFrame);
            m_AnimationSlotFrame = default;
        }

        internal void CommitActionPlaybackFrame(
            CharacterPoseProgramFrameLease lease)
        {
            RequireFrame(lease);
            RequireActionFrame(lease.Lineage.FrameIdentity);
            if (m_AnimationSlotFrame.IsValid)
            {
                throw new InvalidOperationException(
                    "Character Pose Program Animation Slot frame must commit first.");
            }
            ActorState.ActionPlayback.Commit(m_ActionFrame);
            m_ActionFrame = null;
        }

        internal void DiscardAnimationSlotFrame(ulong frameIdentity)
        {
            RequireAlive();
            RequireAnimationSlotFrame(frameIdentity);
            ActorState.AnimationSlots.DiscardFrame(m_AnimationSlotFrame);
            m_AnimationSlotFrame = default;
        }

        internal void DiscardActionPlaybackFrame(ulong frameIdentity)
        {
            RequireAlive();
            RequireActionFrame(frameIdentity);
            if (m_AnimationSlotFrame.IsValid)
            {
                throw new InvalidOperationException(
                    "Character Pose Program Animation Slot frame must discard first.");
            }
            ActorState.ActionPlayback.DiscardFrame(m_ActionFrame);
            m_ActionFrame = null;
            ClearActionFrameViews();
        }

        internal void CommitPresentationWorkspaceFrame(
            CharacterPoseProgramFrameLease lease)
        {
            RequireFrame(lease);
            RequirePresentationWorkspaceFrame(
                lease.Lineage.FrameIdentity);
            m_PresentationWorkspace.Commit(m_PresentationWorkspaceFrame);
            m_PresentationWorkspaceFrame = default;
        }

        internal void DiscardPresentationWorkspaceFrame(
            ulong frameIdentity)
        {
            RequireAlive();
            RequirePresentationWorkspaceFrame(frameIdentity);
            m_PresentationWorkspace.Discard(m_PresentationWorkspaceFrame);
            m_PresentationWorkspaceFrame = default;
        }

        internal void PublishActionCommand(
            in ActionAnimationPlaybackCommand command) =>
            ActorState.ActionPlayback.Publish(command);

        internal void RetireActionCommand(
            in ActionAnimationPlaybackCommand command) =>
            ActorState.ActionPlayback.Retire(command);

        internal void ReplaceActionCommand(
            EventId targetEventId,
            in ActionAnimationPlaybackCommand replacement) =>
            ActorState.ActionPlayback.Replace(
                targetEventId,
                replacement);

        internal IReadOnlyList<ActionAnimationPlaybackLifecycleFrame>
            PrepareActionLifecycleFrame(
                CharacterPoseProgramFrameLease lease)
        {
            RequireFrame(lease);
            RequireActionFrame(lease.Lineage.FrameIdentity);
            RequireAnimationSlotFrame(lease.Lineage.FrameIdentity);
            RequirePresentationWorkspaceFrame(
                lease.Lineage.FrameIdentity);
            m_ConsumedActionBackendReleaseCompletions.Clear();
            m_RetiredActionPlaybacks.Clear();
            m_SourceModule.CopyActionBackendReleaseCompletions(
                m_ActionBackendReleaseCompletions);
            for (int i = 0;
                 i < m_ActionBackendReleaseCompletions.Count;
                 i++)
            {
                ActionBackendReleaseCompletion completion =
                    m_ActionBackendReleaseCompletions[i];
                m_PresentationWorkspace.AddReleaseCompletion(
                    m_PresentationWorkspaceFrame,
                    completion);
                m_ConsumedActionBackendReleaseCompletions.Add(
                    completion);
            }
            m_SourceModule.ValidateActionBackendReleaseAcknowledgements(
                m_ConsumedActionBackendReleaseCompletions);
            ActorState.ActionPlayback.ApplyBackendReleaseCompletions(
                m_ActionFrame,
                m_ActionBackendReleaseCompletions);
            IReadOnlyList<ActionAnimationPlaybackLifecycleFrame> lifecycle =
                ActorState.ActionPlayback.BuildLifecycleFrame(m_ActionFrame);
            RebaseRetiredActionPlaybacks(lifecycle);
            m_AnimationSlotPlans =
                ActorState.AnimationSlots.BuildFramePlans(
                    m_AnimationSlotFrame,
                    lifecycle);
            return lifecycle;
        }

        internal void ProjectActionPresentationSamples(
            CharacterPoseProgramFrameLease lease,
            IReadOnlyList<ActionAnimationPlaybackLifecycleFrame> lifecycle,
            double presentationSampleTick,
            float presentationDeltaSeconds)
        {
            RequireFrame(lease);
            RequireActionFrame(lease.Lineage.FrameIdentity);
            m_SourceModule.ProjectActionPresentationSamples(
                ActorState.ActionPlayback,
                m_ActionFrame,
                lifecycle,
                presentationSampleTick,
                presentationDeltaSeconds);
        }

        internal void ResolveActionPresentationFrames(
            CharacterPoseProgramFrameLease lease)
        {
            RequireFrame(lease);
            RequirePresentationWorkspaceFrame(
                lease.Lineage.FrameIdentity);
            m_SourceModule.ResolveActionPresentationFrames(
                lease.Lineage.FrameIdentity,
                m_PresentationWorkspace,
                m_PresentationWorkspaceFrame);
        }

        internal void ValidateActionSamplingFrame(
            CharacterPoseProgramFrameLease lease)
        {
            RequireFrame(lease);
            m_SourceModule.ValidateActionSamplingFrame(
                lease.Lineage.FrameIdentity);
        }

        internal void CommitActionSamplingFrame(
            CharacterPoseProgramFrameLease lease)
        {
            RequireFrame(lease);
            m_SourceModule.CommitActionSamplingFrame(
                lease.Lineage.FrameIdentity);
        }

        internal void DiscardActionSamplingFrame(ulong frameIdentity)
        {
            RequireAlive();
            m_SourceModule.DiscardActionSamplingFrame(frameIdentity);
        }

        internal void BuildCommittedActionTimeSnapshots(
            FixedCapacityFrameBuffer<ActionPresentationTimeSnapshot>
                destination)
        {
            RequireAlive();
            m_SourceModule.BuildCommittedActionTimeSnapshots(destination);
        }

        internal void ResetActionSampling()
        {
            RequireAlive();
            m_SourceModule.ResetActionSampling();
        }

        internal void PublishActionSources(
            CharacterPoseProgramFrameLease lease,
            IDictionary<AnimationPlayerSourceSampleKey,
                AnimationResolvedPoseSourceSample> sourceSamples)
        {
            RequireFrame(lease);
            RequireActionFrame(lease.Lineage.FrameIdentity);
            RequireAnimationSlotFrame(lease.Lineage.FrameIdentity);
            RequirePresentationWorkspaceFrame(
                lease.Lineage.FrameIdentity);
            if (sourceSamples == null)
            {
                throw new ArgumentException(
                    "Character Pose Program Action source input is invalid.");
            }
            IReadOnlyList<AnimationSlotActionSourcePlan> sourcePlans =
                ActorState.AnimationSlots.CollectActionSourcePlans(
                    m_AnimationSlotFrame);
            if (sourcePlans.Count > ActorState.ActionPlayback.FrameCapacity)
            {
                throw new InvalidOperationException(
                    "Action source sample capacity was exceeded.");
            }
            IReadOnlyDictionary<AnimationPlaybackId,
                ActionAnimationPlaybackFrame> frames =
                    m_PresentationWorkspace.ActionFrames;
            for (int i = 0; i < sourcePlans.Count; i++)
            {
                AnimationSlotActionSourcePlan sourcePlan = sourcePlans[i];
                if (!frames.TryGetValue(
                        sourcePlan.PlaybackId,
                        out ActionAnimationPlaybackFrame frame) ||
                    !ActorState.ActionPlayback.Bindings.TryGet(
                        sourcePlan.PlaybackId.ProducerId,
                        out ResolvedActionAnimationBinding binding) ||
                    binding.SlotId != sourcePlan.SlotId ||
                    binding.SlotNodeId != sourcePlan.SlotNodeId)
                {
                    throw new InvalidOperationException(
                        $"Animation Slot Action source '{sourcePlan.PlaybackId}' has no exact resolved frame.");
                }
                PublishActionSourceFrame(
                    lease,
                    in frame,
                    in binding,
                    sourcePlan.SelectionGeneration,
                    ActorState.NextPresentationRequestSequence(),
                    sourceSamples,
                    sourcePlan.Current);
            }
            for (int i = 0; i < m_AnimationSlotPlans.Count; i++)
            {
                AnimationSlotFramePlan plan = m_AnimationSlotPlans[i];
                if (!plan.TargetsSourcePose)
                    continue;
                PublishActionSourcePose(
                    lease,
                    plan.SlotId,
                    plan.SlotNodeId,
                    ActorState.NextPresentationRequestSequence());
            }
        }

        internal void CompleteActionReleaseProtocol(
            CharacterPoseProgramFrameLease lease)
        {
            RequireFrame(lease);
            RequireActionFrame(lease.Lineage.FrameIdentity);
            RequireAnimationSlotFrame(lease.Lineage.FrameIdentity);
            RequirePresentationWorkspaceFrame(
                lease.Lineage.FrameIdentity);
            m_SourceModule.CopyActionSlotReleaseCompletions(
                m_ActionSlotReleaseCompletions);
            for (int i = 0;
                 i < m_ActionSlotReleaseCompletions.Count;
                 i++)
            {
                AnimationSlotSourceReleaseCompletion completion =
                    m_ActionSlotReleaseCompletions[i];
                ActorState.AnimationSlots.CompleteSourceRelease(
                    m_AnimationSlotFrame,
                    completion.SlotId,
                    completion.PlaybackId);
            }
            ActorState.AnimationSlots.PublishActionUsages(
                m_AnimationSlotFrame,
                m_PresentationWorkspace,
                m_PresentationWorkspaceFrame);
            ActorState.ActionPlayback.ReplaceSlotUsageBatch(
                m_ActionFrame,
                m_PresentationWorkspace.ActionUsages);
            IReadOnlyList<ActionAnimationPlaybackLifecycleFrame> lifecycle =
                ActorState.ActionPlayback.BuildLifecycleFrame(m_ActionFrame);
            ActorState.AnimationSlots.PublishRetirementPermissions(
                m_AnimationSlotFrame,
                lifecycle,
                m_PresentationWorkspace,
                m_PresentationWorkspaceFrame);
            ActorState.ActionPlayback.ApplyRetirementPermissions(
                m_ActionFrame,
                m_PresentationWorkspace.RetirementPermissions);
            lifecycle =
                ActorState.ActionPlayback.BuildLifecycleFrame(m_ActionFrame);
            for (int i = 0; i < lifecycle.Count; i++)
            {
                ActionAnimationPlaybackLifecycleFrame snapshot = lifecycle[i];
                if (snapshot.Phase !=
                        ActionAnimationPlaybackLifecyclePhase
                            .RetirementPermitted ||
                    snapshot.BackendReleaseRequestIdentity != 0)
                {
                    continue;
                }
                if (TryPrepareActionBackendReleaseRequest(
                        snapshot.PlaybackId,
                        out ActionBackendReleaseRequest request))
                {
                    ActorState.ActionPlayback.RegisterBackendReleaseRequest(
                        m_ActionFrame,
                        request);
                    m_PresentationWorkspace.AddReleaseRequest(
                        m_PresentationWorkspaceFrame,
                        request);
                    continue;
                }
                ActorState.ActionPlayback.RetireWithoutBackendResources(
                    m_ActionFrame,
                    snapshot.PlaybackId);
                TryAddRetiredActionPlayback(snapshot.PlaybackId);
            }
        }

        internal void ValidateActionFrame(
            CharacterPoseProgramFrameLease lease)
        {
            RequireFrame(lease);
            RequireActionFrame(lease.Lineage.FrameIdentity);
            ActorState.ActionPlayback.ValidateFrame(m_ActionFrame);
        }

        internal IReadOnlyList<ActionAnimationPlaybackLifecycleSnapshot>
            BuildCommittedActionLifecycleSnapshot() =>
            ActorState.ActionPlayback.BuildCommittedLifecycleSnapshot();

        internal IReadOnlyList<AnimationPlaybackId>
            RetiredActionPlaybacks => m_RetiredActionPlaybacks;

        internal void ResetAnimationSlots()
        {
            RequireAlive();
            ActorState.AnimationSlots.Reset();
            m_AnimationSlotPlans = Array.Empty<AnimationSlotFramePlan>();
        }

        internal void ResetActionPlayback()
        {
            RequireAlive();
            ActorState.ActionPlayback.Reset();
            ClearActionFrameViews();
        }

        internal void ResetPresentationWorkspace()
        {
            RequireAlive();
            m_PresentationWorkspace.Reset();
            m_MotionMatching.ClearSelections();
        }

        internal AnimationPoseSourceId PublishActionSourceFrame(
            CharacterPoseProgramFrameLease lease,
            in ActionAnimationPlaybackFrame frame,
            in ResolvedActionAnimationBinding binding,
            AnimationPoseSelectionGeneration selectionGeneration,
            ulong presentationRequestSequence,
            IDictionary<AnimationPlayerSourceSampleKey,
                AnimationResolvedPoseSourceSample> sourceSamples,
            bool select)
        {
            RequireFrame(lease);
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
                !ActorState.NodeRuntimeIndex.TryGetStackRoute(
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
            CharacterPoseProgramFrameLease lease,
            AnimationSlotId slotId,
            PoseNodeId slotNodeId,
            ulong presentationRequestSequence)
        {
            RequireFrame(lease);
            if (!slotId.IsValid ||
                !slotNodeId.IsValid ||
                presentationRequestSequence == 0 ||
                !ActorState.NodeRuntimeIndex.TryGetStackRoute(
                    slotNodeId,
                    out AnimationBlendStackRuntime stack,
                    out CharacterAnimationTransitionRouteRuntime route) ||
                !route.IsAnimationSlot ||
                route.SlotId != slotId)
            {
                throw new InvalidOperationException(
                    "Source Pose target has no exact compiled Animation Slot route.");
            }
            route.PushSourcePose(stack, presentationRequestSequence);
        }

        internal void BeginSourceRetirementFrame()
        {
            RequireAlive();
            ActorState.SourceRetirement.BeginFrame();
        }

        internal void CompleteSourceRetirementFrame()
        {
            RequireAlive();
            ActorState.SourceRetirement.CompleteFrame();
        }

        internal void DiscardSourceRetirementFrame()
        {
            RequireAlive();
            ActorState.SourceRetirement.DiscardFrame();
        }

        internal void ClearStandaloneSourceRetirements()
        {
            RequireAlive();
            ActorState.SourceRetirement.ClearStandalone();
        }

        internal void ClearSourceRetirements()
        {
            RequireAlive();
            ActorState.SourceRetirement.Clear();
        }

        internal void CopySourceSyncSnapshots(
            List<PoseStateSourceSyncSnapshot> destination)
        {
            RequireAlive();
            ActorState.PoseStateSources.CopySourceSyncSnapshots(destination);
        }

        internal bool HasPendingActionBackendSources(
            AnimationPlaybackId playbackId)
        {
            RequireAlive();
            return ActorState.SourceRetirement.HasPendingAction(playbackId);
        }

        internal bool TryPrepareActionBackendReleaseRequest(
            AnimationPlaybackId playbackId,
            out ActionBackendReleaseRequest request)
        {
            RequireAlive();
            return ActorState.SourceRetirement.TryPrepareActionRequest(
                playbackId,
                out request);
        }

        internal void ExecutePreparedActionBackendReleaseRequests()
        {
            RequireAlive();
            ActorState.SourceRetirement.ExecutePreparedActions(
                m_SourceModule);
        }

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
            in CharacterPresentationFactFrame factFrame)
        {
            RequireFrame(lease);
            RequirePresentationWorkspaceFrame(
                lease.Lineage.FrameIdentity);
            if (!factFrame.IsValid ||
                !m_PresentationWorkspaceFrame.IsValid)
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
                m_PresentationWorkspace,
                m_PresentationWorkspaceFrame);
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
            for (int i = 0; i < m_Image.OperationHeaders.Count; i++)
            {
                CharacterPoseOperationHeader operation =
                    m_Image.OperationHeaders[i];
                if (operation.Code != CharacterPoseOperationCode.ClipPlayer ||
                    ((CharacterPosePlayerOperationPayload)
                        m_Image.OperationPages.RequirePayload(operation))
                    .ClipPlayerIndex != playerIndex)
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
                m_CommittingFrameLease.IsValid ||
                m_ActionFrame?.IsValid != true ||
                m_ActionFrame.Identity != lease.Lineage.FrameIdentity ||
                !m_AnimationSlotFrame.IsValid ||
                m_AnimationSlotFrame.FrameIdentity !=
                    lease.Lineage.FrameIdentity ||
                !m_PresentationWorkspaceFrame.IsValid ||
                m_PresentationWorkspaceFrame.Identity !=
                    lease.Lineage.FrameIdentity)
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
            m_Evaluation.DiscardPending();
            m_ActiveFrameLease = default;
            m_CommittingFrameLease = default;
            m_ActionFrame = null;
            m_AnimationSlotFrame = default;
            m_PresentationWorkspaceFrame = default;
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

        void BeginEvaluationFrame(
            CharacterPoseProgramFrameLease lease,
            ulong completionIdentity)
        {
            RequireFrame(lease);
            if (m_Evaluation.HasPrepared ||
                m_Evaluation.HasPendingCompleted)
            {
                throw new InvalidOperationException(
                    "Character Pose Program has an unfinished evaluation.");
            }
            FramePages.BeginEvaluationFrame(completionIdentity);
        }

        internal void BeginSourceEvaluation(
            CharacterPoseProgramFrameLease lease,
            ulong completionIdentity)
        {
            BeginEvaluationFrame(lease, completionIdentity);
            for (int i = 0; i < ActorState.Stacks.Length; i++)
                ActorState.Stacks[i].BeginSourceFrame(completionIdentity);
            for (int i = 0; i < ActorState.DirectPlayers.Length; i++)
                ActorState.DirectPlayers[i].BeginFrame(completionIdentity);
            for (int i = 0;
                 i < ActorState.PoseStateSources.ClipPlayers.Length;
                 i++)
            {
                ActorState.PoseStateSources.ClipPlayers[i]
                    .BeginFrame(completionIdentity);
            }
            for (int i = 0;
                 i < ActorState.PoseStateSources.BlendSpacePlayers.Length;
                 i++)
            {
                ActorState.PoseStateSources.BlendSpacePlayers[i]
                    .BeginFrame(completionIdentity);
            }
        }

        internal void PrepareStackSources(
            CharacterPoseProgramFrameLease lease,
            CharacterPoseSourceFrameLease sourceLease,
            in CharacterPoseSourcePreparationView preparations,
            float presentationDeltaSeconds,
            IReadOnlyDictionary<AnimationPlayerSourceSampleKey,
                AnimationResolvedPoseSourceSample> actionSourceSamples,
            IReadOnlyDictionary<AnimationPlayerSourceSampleKey,
                PresentationPoseSourceSample> providerSourceSamples)
        {
            RequireFrame(lease);
            for (int stackIndex = 0;
                 stackIndex < ActorState.Stacks.Length;
                 stackIndex++)
            {
                AnimationBlendStackRuntime stack =
                    ActorState.Stacks[stackIndex];
                if (!LinkedFragments.IsPlayerActive(stack.PlayerIndex) ||
                    !stack.HasCurrentSelectionSample)
                {
                    continue;
                }
                for (int entryIndex = 0;
                     entryIndex < stack.EntryCount;
                     entryIndex++)
                {
                    AnimationBlendEntryId entry =
                        stack.GetEntryId(entryIndex);
                    if (entry.SourcePoseTarget ||
                        HasEarlierSource(
                            stack,
                            entryIndex,
                            entry.SourceId))
                    {
                        continue;
                    }
                    PrepareStackSource(
                        stack,
                        sourceLease,
                        in preparations,
                        entry.SourceId,
                        presentationDeltaSeconds,
                        actionSourceSamples,
                        providerSourceSamples);
                }
            }
        }

        internal void PrepareDirectSources(
            CharacterPoseProgramFrameLease lease,
            CharacterPoseSourceFrameLease sourceLease,
            in CharacterPoseSourcePreparationView preparations,
            float presentationDeltaSeconds,
            IReadOnlyDictionary<AnimationPlayerSourceSampleKey,
                PresentationPoseSourceSample> sourceSamples)
        {
            RequireFrame(lease);
            for (int playerIndex = 0;
                 playerIndex < ActorState.DirectPlayers.Length;
                 playerIndex++)
            {
                AnimationSelectedPosePlayerRuntime player =
                    ActorState.DirectPlayers[playerIndex];
                if (!LinkedFragments.IsPlayerActive(player.PlayerIndex) ||
                    !player.HasCurrentSample)
                {
                    continue;
                }
                var key = new AnimationPlayerSourceSampleKey(
                    player.NodeId,
                    player.SourceId);
                if (!sourceSamples.TryGetValue(
                        key,
                        out PresentationPoseSourceSample sample))
                {
                    throw new InvalidOperationException(
                        $"Animation Pose Source '{player.SourceId}' has no current resolved request.");
                }
                AnimationPoseSourceCaptureBinding capture =
                    player.PrepareCapture(
                        in sample,
                        presentationDeltaSeconds);
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
        }

        internal void PrepareSequenceSources(
            CharacterPoseProgramFrameLease lease,
            CharacterPoseSourceFrameLease sourceLease,
            in CharacterPoseSourcePreparationView preparations,
            float presentationDeltaSeconds,
            in CharacterPoseSourceTuningView sourceTuning)
        {
            RequireFrame(lease);
            for (int playerIndex = 0;
                 playerIndex < ActorState.PoseStateSources.ClipPlayers.Length;
                 playerIndex++)
            {
                AnimationClipPlayerRuntime player =
                    ActorState.PoseStateSources.ClipPlayers[playerIndex];
                bool selectedPreview =
                    IsSequencePreviewPlayer(playerIndex);
                if (!selectedPreview &&
                        !LinkedFragments.IsPlayerActive(player.PlayerIndex) ||
                    !player.IsRelevant)
                {
                    continue;
                }
                AnimationPoseSourceCaptureBinding capture =
                    player.PrepareCapture(
                        presentationDeltaSeconds,
                        sourceTuning.RequireClipPlayRate(playerIndex));
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
        }

        internal void PrepareBlendSpaceSources(
            CharacterPoseProgramFrameLease lease,
            CharacterPoseSourceFrameLease sourceLease,
            in CharacterPoseSourcePreparationView preparations,
            float presentationDeltaSeconds)
        {
            RequireFrame(lease);
            for (int playerIndex = 0;
                 playerIndex <
                 ActorState.PoseStateSources.BlendSpacePlayers.Length;
                 playerIndex++)
            {
                AnimationBlendSpacePlayerRuntime player =
                    ActorState.PoseStateSources.BlendSpacePlayers[
                        playerIndex];
                if (!LinkedFragments.IsPlayerActive(player.PlayerIndex) ||
                    !player.IsRelevant)
                {
                    continue;
                }
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
        }

        void PrepareStackSource(
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
            var key = new AnimationPlayerSourceSampleKey(
                stack.PoseNodeId,
                sourceId);
            if (sourceId.SourceKind == AnimationPoseSourceKind.Timeline)
            {
                if (!actionSourceSamples.TryGetValue(
                        key,
                        out AnimationResolvedPoseSourceSample sourceSample))
                {
                    throw new InvalidOperationException(
                        $"Action Pose Source '{sourceId}' has no current resolved request.");
                }
                AnimationPoseSampleRequest request = sourceSample.Request;
                AnimationPoseSourceCaptureBinding capture =
                    stack.PrepareCapture(
                        sourceSample,
                        presentationDeltaSeconds);
                CharacterPoseSourcePreparation preparation =
                    CharacterPoseSourcePreparation.Action(
                        in request,
                        in capture,
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
                !ActorState.NodeRuntimeIndex.TryGetSourceOwnerIndex(
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
            AnimationPoseSampleRequest providerRequest = resolved.Request;
            AnimationPoseSourceCaptureBinding providerCapture =
                stack.PrepareCapture(
                    resolved,
                    presentationDeltaSeconds);
            CharacterPoseSourcePreparation providerPreparation =
                CharacterPoseSourcePreparation.Provider(
                    in providerRequest,
                    in providerSample,
                    in providerCapture,
                    stack.PoseNodeId);
            SubmitSourcePreparation(
                sourceLease,
                in preparations,
                in providerPreparation);
        }

        void SubmitSourcePreparation(
            CharacterPoseSourceFrameLease sourceLease,
            in CharacterPoseSourcePreparationView preparations,
            in CharacterPoseSourcePreparation preparation)
        {
            int index = FramePages.AddSourcePreparation(in preparation);
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
                if (!candidate.SourcePoseTarget &&
                    candidate.SourceId.Equals(sourceId))
                {
                    return true;
                }
            }
            return false;
        }

        internal void PrepareEvaluationJobsAndRetirements(
            CharacterPoseProgramFrameLease lease,
            in CharacterPoseSourcePreparedResources preparedSources,
            ulong completionIdentity)
        {
            RequireFrame(lease);
            for (int slotIndex = 0;
                 slotIndex < ActorState.Stacks.Length;
                 slotIndex++)
            {
                AnimationBlendStackRuntime stack =
                    ActorState.Stacks[slotIndex];
                AnimationPlayerPoseNativeWriteBinding write =
                    FramePages.RequirePlayerWriteBinding(
                        stack.PlayerIndex,
                        completionIdentity);
                m_SlotJobs[slotIndex] = stack.PrepareSlotJob(
                    completionIdentity,
                    in write,
                    m_SourceModule);
            }
            for (int slotIndex = 0;
                 slotIndex < ActorState.Stacks.Length;
                 slotIndex++)
            {
                ActorState.Stacks[slotIndex].PrepareCompletion(
                    completionIdentity);
            }
            for (int playerIndex = 0;
                 playerIndex < ActorState.DirectPlayers.Length;
                 playerIndex++)
            {
                AnimationSelectedPosePlayerRuntime player =
                    ActorState.DirectPlayers[playerIndex];
                AnimationPlayerPoseNativeWriteBinding write =
                    FramePages.RequirePlayerWriteBinding(
                        player.PlayerIndex,
                        completionIdentity);
                CharacterPoseSourceBinding sourceBinding =
                    preparedSources.RequireDirectBinding(playerIndex);
                m_DirectPlayerJobs[playerIndex] = player.PrepareJob(
                    completionIdentity,
                    in write,
                    sourceBinding.PhysicalIdentity,
                    sourceBinding.SourceIndex);
            }
            for (int playerIndex = 0;
                 playerIndex < ActorState.PoseStateSources.ClipPlayers.Length;
                 playerIndex++)
            {
                AnimationClipPlayerRuntime player =
                    ActorState.PoseStateSources.ClipPlayers[playerIndex];
                AnimationPlayerPoseNativeWriteBinding write =
                    FramePages.RequirePlayerWriteBinding(
                        player.PlayerIndex,
                        completionIdentity);
                CharacterPoseSourceBinding sourceBinding =
                    preparedSources.RequireClipBinding(playerIndex);
                m_ClipPlayerJobs[playerIndex] = player.PrepareJob(
                    completionIdentity,
                    in write,
                    sourceBinding.PhysicalIdentity,
                    sourceBinding.SourceIndex);
            }
            for (int playerIndex = 0;
                 playerIndex <
                 ActorState.PoseStateSources.BlendSpacePlayers.Length;
                 playerIndex++)
            {
                AnimationBlendSpacePlayerRuntime player =
                    ActorState.PoseStateSources.BlendSpacePlayers[
                        playerIndex];
                AnimationPlayerPoseNativeWriteBinding write =
                    FramePages.RequirePlayerWriteBinding(
                        player.PlayerIndex,
                        completionIdentity);
                CharacterPoseSourceBinding sourceBinding =
                    preparedSources.RequireBlendSpaceBinding(playerIndex);
                m_BlendSpacePlayerJobs[playerIndex] = player.PrepareJob(
                    completionIdentity,
                    in write,
                    sourceBinding.PhysicalIdentity,
                    sourceBinding.SourceIndex);
            }
            StageCompletedSources(completionIdentity);
        }

        internal void ValidateSourceRetirements(
            CharacterPoseProgramFrameLease lease,
            CharacterPoseSourceFrameLease sourceLease)
        {
            RequireFrame(lease);
            m_SourceModule.ValidatePhysicalFrame();
            int standaloneReleaseCount = 0;
            for (int i = 0;
                 i < ActorState.DirectPlayers.Length;
                 i++)
            {
                AnimationSelectedPosePlayerRuntime player =
                    ActorState.DirectPlayers[i];
                int releaseCount = player.PendingReleaseCount;
                standaloneReleaseCount = checked(
                    standaloneReleaseCount + releaseCount);
                for (int releaseIndex = 0;
                     releaseIndex < releaseCount;
                     releaseIndex++)
                {
                    AnimationPlayerReleaseToken playerRelease =
                        player.PrepareRelease(releaseIndex);
                    CharacterPoseSourceRetirementHandle sourceRelease =
                        PrepareSourceRetirement(
                            playerRelease.SourceId,
                            player.NodeId,
                            default);
                    ActorState.SourceRetirement.PrepareDirect(
                        i,
                        in sourceRelease,
                        in playerRelease);
                }
            }
            for (int i = 0;
                 i < ActorState.PoseStateSources.ClipPlayers.Length;
                 i++)
            {
                AnimationClipPlayerRuntime player =
                    ActorState.PoseStateSources.ClipPlayers[i];
                int releaseCount = player.PendingReleaseCount;
                standaloneReleaseCount = checked(
                    standaloneReleaseCount + releaseCount);
                for (int releaseIndex = 0;
                     releaseIndex < releaseCount;
                     releaseIndex++)
                {
                    AnimationPlayerReleaseToken playerRelease =
                        player.PrepareRelease(releaseIndex);
                    CharacterPoseSourceRetirementHandle sourceRelease =
                        PrepareSourceRetirement(
                            playerRelease.SourceId,
                            player.NodeId,
                            default);
                    ActorState.SourceRetirement.PrepareClip(
                        i,
                        in sourceRelease,
                        in playerRelease);
                }
            }
            for (int i = 0;
                 i < ActorState.PoseStateSources.BlendSpacePlayers.Length;
                 i++)
            {
                AnimationBlendSpacePlayerRuntime player =
                    ActorState.PoseStateSources.BlendSpacePlayers[i];
                int releaseCount = player.PendingReleaseCount;
                standaloneReleaseCount = checked(
                    standaloneReleaseCount + releaseCount);
                for (int releaseIndex = 0;
                     releaseIndex < releaseCount;
                     releaseIndex++)
                {
                    AnimationPlayerReleaseToken playerRelease =
                        player.PrepareRelease(releaseIndex);
                    CharacterPoseSourceRetirementHandle sourceRelease =
                        PrepareSourceRetirement(
                            playerRelease.SourceId,
                            player.NodeId,
                            default);
                    ActorState.SourceRetirement.PrepareBlendSpace(
                        i,
                        in sourceRelease,
                        in playerRelease);
                }
            }
            int preparedActionReleaseCount =
                ActorState.SourceRetirement.PreparePendingRetirements(
                    m_SourceModule);
            m_SourceModule.RequireReleaseDiagnosticsCapacity(
                checked(
                    standaloneReleaseCount +
                    ActorState.SourceRetirement.PendingPoseCount +
                    preparedActionReleaseCount));
            m_SourceModule.RequireActionBackendReleaseCompletionCapacity(
                checked(preparedActionReleaseCount * 2));
            m_SourceModule.ValidateFrame(sourceLease);
        }

        internal void FinalizeCommittedSourceRetirements(
            ulong completionIdentity)
        {
            RequireAlive();
            ActorState.SourceRetirement.ApplyStandalone(
                m_SourceModule,
                ActorState.DirectPlayers,
                ActorState.PoseStateSources.ClipPlayers,
                ActorState.PoseStateSources.BlendSpacePlayers,
                completionIdentity);
            ActorState.SourceRetirement.ApplyPendingPose(m_SourceModule);
        }

        internal void ReleaseCompletedSources(ulong completionIdentity)
        {
            RequireAlive();
            for (int stackIndex = 0;
                 stackIndex < ActorState.Stacks.Length;
                 stackIndex++)
            {
                AnimationBlendStackRuntime stack =
                    ActorState.Stacks[stackIndex];
                CharacterAnimationTransitionRouteRuntime route =
                    ActorState.Routes[stackIndex];
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
                    m_SourceModule.ApplyRetirement(in sourceRelease);
                    stack.ApplyPreparedRelease(in stackRelease);
                    releasedAny = true;
                }
                if (releasedAny)
                    route.NotifySourcesReleased();
            }
        }

        internal void ReleasePlayerSources()
        {
            RequireAlive();
            for (int i = 0; i < ActorState.DirectPlayers.Length; i++)
                ReleasePlayerSources(ActorState.DirectPlayers[i]);
            for (int i = 0;
                 i < ActorState.PoseStateSources.ClipPlayers.Length;
                 i++)
            {
                ReleasePlayerSources(
                    ActorState.PoseStateSources.ClipPlayers[i]);
            }
            for (int i = 0;
                 i < ActorState.PoseStateSources.BlendSpacePlayers.Length;
                 i++)
            {
                ReleasePlayerSources(
                    ActorState.PoseStateSources.BlendSpacePlayers[i]);
            }
        }

        void ReleasePlayerSources(AnimationSelectedPosePlayerRuntime player)
        {
            int releaseCount = player.PendingReleaseCount;
            for (int releaseIndex = 0;
                 releaseIndex < releaseCount;
                 releaseIndex++)
            {
                AnimationPlayerReleaseToken playerRelease =
                    player.PrepareRelease(releaseIndex);
                CharacterPoseSourceRetirementHandle sourceRelease =
                    PrepareSourceRetirement(
                        playerRelease.SourceId,
                        player.NodeId,
                        default);
                m_SourceModule.ApplyRetirement(in sourceRelease);
                player.ApplyPreparedRelease(in playerRelease);
            }
        }

        void ReleasePlayerSources(AnimationClipPlayerRuntime player)
        {
            int releaseCount = player.PendingReleaseCount;
            for (int releaseIndex = 0;
                 releaseIndex < releaseCount;
                 releaseIndex++)
            {
                AnimationPlayerReleaseToken playerRelease =
                    player.PrepareRelease(releaseIndex);
                CharacterPoseSourceRetirementHandle sourceRelease =
                    PrepareSourceRetirement(
                        playerRelease.SourceId,
                        player.NodeId,
                        default);
                m_SourceModule.ApplyRetirement(in sourceRelease);
                player.ApplyPreparedRelease(in playerRelease);
            }
        }

        void ReleasePlayerSources(AnimationBlendSpacePlayerRuntime player)
        {
            int releaseCount = player.PendingReleaseCount;
            for (int releaseIndex = 0;
                 releaseIndex < releaseCount;
                 releaseIndex++)
            {
                AnimationPlayerReleaseToken playerRelease =
                    player.PrepareRelease(releaseIndex);
                CharacterPoseSourceRetirementHandle sourceRelease =
                    PrepareSourceRetirement(
                        playerRelease.SourceId,
                        player.NodeId,
                        default);
                m_SourceModule.ApplyRetirement(in sourceRelease);
                player.ApplyPreparedRelease(in playerRelease);
            }
        }

        CharacterPoseSourceRetirementHandle PrepareSourceRetirement(
            AnimationPoseSourceId sourceId,
            PoseNodeId poseNodeId,
            AnimationPhysicalSourceIdentity expectedPhysicalIdentity)
        {
            var permission = new CharacterPoseSourceRetirementPermission(
                sourceId,
                poseNodeId,
                expectedPhysicalIdentity);
            return m_SourceModule.PrepareRetirement(in permission);
        }

        internal void BindEvaluationExecution(
            CharacterPoseProgramFrameLease lease,
            in CharacterPoseFrameLineage lineage,
            in CharacterFinalPosePublicationOutputBinding finalOutput,
            bool recordDiagnostics)
        {
            BindEvaluation(
                lease,
                in lineage,
                lineage.TuningGeneration,
                in finalOutput,
                recordDiagnostics);
            InstallOrUpdateJobs();
        }

        internal void DetachExecutionJobs()
        {
            if (!m_JobsInstalled ||
                !m_Animancer ||
                !m_Animancer.IsGraphInitialized)
            {
                return;
            }
            for (int i = m_SlotPlayables.Length - 1; i >= 0; i--)
                AnimancerUtilities.RemovePlayable(m_SlotPlayables[i]);
            for (int i = m_DirectPlayerPlayables.Length - 1; i >= 0; i--)
                AnimancerUtilities.RemovePlayable(m_DirectPlayerPlayables[i]);
            for (int i = m_ClipPlayerPlayables.Length - 1; i >= 0; i--)
                AnimancerUtilities.RemovePlayable(m_ClipPlayerPlayables[i]);
            for (int i = m_BlendSpacePlayerPlayables.Length - 1;
                 i >= 0;
                 i--)
            {
                AnimancerUtilities.RemovePlayable(
                    m_BlendSpacePlayerPlayables[i]);
            }
            m_JobsInstalled = false;
        }

        void StageCompletedSources(ulong completionIdentity)
        {
            if (ActorState.SourceRetirement.PendingPoseCount != 0)
            {
                throw new InvalidOperationException(
                    "Pose source releases from the previous committed frame were not finalized.");
            }
            for (int stackIndex = 0;
                 stackIndex < ActorState.Stacks.Length;
                 stackIndex++)
            {
                AnimationBlendStackRuntime stack =
                    ActorState.Stacks[stackIndex];
                CharacterAnimationTransitionRouteRuntime route =
                    ActorState.Routes[stackIndex];
                if (!route.CanReleaseSources)
                    continue;
                int releaseCount = stack.PendingPriorFrameReleaseCount(
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
                            .IsFiniteActionSource(release.SourceId))
                    {
                        ActorState.SourceRetirement.StageAction(
                            m_SourceModule,
                            route.SlotId,
                            stack,
                            route,
                            in stackRelease,
                            physical);
                        continue;
                    }
                    ActorState.SourceRetirement.StagePose(
                        m_SourceModule,
                        stack,
                        route,
                        in stackRelease,
                        physical);
                }
            }
        }

        void InstallOrUpdateJobs()
        {
            if (!m_JobsInstalled)
            {
                m_BlendSpacePlayerPlayables =
                    new AnimationScriptPlayable[
                        m_BlendSpacePlayerJobs.Length];
                for (int i = 0;
                     i < m_BlendSpacePlayerJobs.Length;
                     i++)
                {
                    m_BlendSpacePlayerPlayables[i] =
                        m_Animancer.Graph.InsertOutputJob(
                            m_BlendSpacePlayerJobs[i]);
                    m_BlendSpacePlayerPlayables[i].SetProcessInputs(true);
                }
                m_ClipPlayerPlayables =
                    new AnimationScriptPlayable[m_ClipPlayerJobs.Length];
                for (int i = 0; i < m_ClipPlayerJobs.Length; i++)
                {
                    m_ClipPlayerPlayables[i] =
                        m_Animancer.Graph.InsertOutputJob(
                            m_ClipPlayerJobs[i]);
                    m_ClipPlayerPlayables[i].SetProcessInputs(true);
                }
                m_DirectPlayerPlayables =
                    new AnimationScriptPlayable[m_DirectPlayerJobs.Length];
                for (int i = 0; i < m_DirectPlayerJobs.Length; i++)
                {
                    m_DirectPlayerPlayables[i] =
                        m_Animancer.Graph.InsertOutputJob(
                            m_DirectPlayerJobs[i]);
                    m_DirectPlayerPlayables[i].SetProcessInputs(true);
                }
                m_SlotPlayables =
                    new AnimationScriptPlayable[m_SlotJobs.Length];
                for (int i = 0; i < m_SlotJobs.Length; i++)
                {
                    m_SlotPlayables[i] =
                        m_Animancer.Graph.InsertOutputJob(m_SlotJobs[i]);
                    m_SlotPlayables[i].SetProcessInputs(true);
                }
                m_JobsInstalled = true;
                return;
            }
            for (int i = 0;
                 i < m_BlendSpacePlayerJobs.Length;
                 i++)
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

        internal void CommitEvaluationFrame(
            CharacterPoseProgramFrameLease lease,
            ulong completionIdentity)
        {
            RequireFrame(lease);
            if (!m_Evaluation.HasPendingCompleted ||
                m_Evaluation.PendingCompletedCompletionIdentity !=
                    completionIdentity)
            {
                throw new InvalidOperationException(
                    "Character Pose Program has no completed evaluation to commit.");
            }
            FramePages.CommitEvaluationFrame(completionIdentity);
            m_Evaluation.Commit(completionIdentity);
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

        void BindEvaluation(
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
                m_Tuning.Require(tuningGeneration);
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
            m_Evaluation.Prepare(
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
            CharacterPosePreparedEvaluationState state =
                m_Evaluation.Consume(in prepared);
            Executor.BeginEvaluation(
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
                output = Executor.CompleteEvaluation();
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
            return m_Evaluation.RequireDeltaSeconds(in prepared);
        }

        void MarkEvaluationCompleted(
            CharacterPoseProgramFrameLease lease,
            ulong completionIdentity)
        {
            RequireFrame(lease);
            if (m_Evaluation.HasPrepared ||
                m_Evaluation.HasPendingCompleted)
            {
                throw new InvalidOperationException(
                    "Character Pose Program evaluation completion is invalid.");
            }
            FramePages.RequireEvaluationStagesCompleted(completionIdentity);
            CharacterPoseGraphNativeBinding completed =
                FramePages.RequirePoseGraphBinding(completionIdentity);
            m_Evaluation.MarkCompleted(in completed);
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

        void ResetEvaluation()
        {
            RequireAlive();
            if (m_ActiveFrameLease.IsValid)
            {
                throw new InvalidOperationException(
                    "Character Pose Program evaluation cannot reset during a frame.");
            }
            m_Evaluation.Reset();
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
            m_MotionMatching.HasPreparedCompletion;

        internal MotionMatchingPoseStateDemandBatch
            BuildMotionMatchingDemandBatch(
                CharacterPoseProgramFrameLease lease,
                ulong presentationFrame,
                ulong resetSequence)
        {
            RequireFrame(lease);
            RequirePresentationWorkspaceFrame(
                lease.Lineage.FrameIdentity);
            return m_MotionMatching.BuildDemandBatch(
                presentationFrame,
                resetSequence,
                m_PresentationWorkspaceFrame);
        }

        internal void ApplyMotionMatchingSelections(
            CharacterPoseProgramFrameLease lease,
            in MotionMatchingFrameResolution resolution)
        {
            RequireFrame(lease);
            RequirePresentationWorkspaceFrame(
                lease.Lineage.FrameIdentity);
            m_MotionMatching.ApplySelections(
                in resolution,
                m_PresentationWorkspaceFrame);
        }

        internal void ClearMotionMatchingSelections(
            CharacterPoseProgramFrameLease lease)
        {
            RequireFrame(lease);
            m_MotionMatching.ClearSelections();
        }

        internal void PrepareMotionMatchingPosePlanCompletion(
            CharacterPoseProgramFrameLease lease,
            in MotionMatchingFrameResolution resolution,
            ulong poseCompletionIdentity)
        {
            RequireFrame(lease);
            m_MotionMatching.PrepareCompletion(
                in resolution,
                poseCompletionIdentity);
        }

        internal MotionMatchingPosePlanCompletion
            BuildMotionMatchingPosePlanCompletion(
                CharacterPoseProgramFrameLease lease)
        {
            RequireFrame(lease);
            return m_MotionMatching.BuildCompletion();
        }

        internal void ClearMotionMatchingPoseCompletion()
        {
            RequireAlive();
            m_MotionMatching.ClearCompletion();
        }
        internal CharacterPoseProgramTuningView RequireTuning(
            ulong generation) => m_Tuning.Require(generation);

        internal CharacterPoseProgramCommittedDiagnosticsView
            CaptureCommittedDiagnostics(
                CharacterPoseProgramCommittedDiagnosticsProjector projector,
                in CharacterPoseProgramResult result,
                in CharacterFinalPoseCommittedDiagnosticsView finalOutput,
                AnimationPresentationDiagnosticsInterest interest)
        {
            if (projector == null)
                throw new ArgumentNullException(nameof(projector));
            if (!m_Evaluation.HasCommitted)
            {
                throw new InvalidOperationException(
                    "Character Pose Program has no committed evaluation diagnostics.");
            }
            CharacterPoseGraphNativeBinding committed =
                m_Evaluation.RequireCommitted();
            return projector.Capture(
                FramePages,
                in result,
                in committed,
                in finalOutput,
                interest);
        }

        internal string PrepareTuningCandidate(
            CharacterPoseTuningLayout layout,
            CharacterPoseTuningParameterBlock block,
            ulong generation) =>
            m_Tuning.Prepare(layout, block, generation);

        internal void CommitTuningCandidate(ulong generation) =>
            m_Tuning.Commit(generation);

        internal void DiscardTuningCandidate() => m_Tuning.Discard();
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

        void RebaseRetiredActionPlaybacks(
            IReadOnlyList<ActionAnimationPlaybackLifecycleFrame> lifecycle)
        {
            for (int i = 0; i < lifecycle.Count; i++)
            {
                ActionAnimationPlaybackLifecycleFrame snapshot = lifecycle[i];
                if (snapshot.Phase !=
                    ActionAnimationPlaybackLifecyclePhase.Retired)
                {
                    continue;
                }
                bool completedThisFrame = false;
                for (int completionIndex = 0;
                     completionIndex <
                     m_ConsumedActionBackendReleaseCompletions.Count;
                     completionIndex++)
                {
                    if (m_ConsumedActionBackendReleaseCompletions[
                            completionIndex]
                        .PlaybackId.Equals(snapshot.PlaybackId))
                    {
                        completedThisFrame = true;
                        break;
                    }
                }
                if (completedThisFrame)
                    TryAddRetiredActionPlayback(snapshot.PlaybackId);
            }
        }

        bool TryAddRetiredActionPlayback(AnimationPlaybackId playbackId)
        {
            for (int i = 0; i < m_RetiredActionPlaybacks.Count; i++)
            {
                if (m_RetiredActionPlaybacks[i].Equals(playbackId))
                    return false;
            }
            m_RetiredActionPlaybacks.Add(playbackId);
            return true;
        }

        void ClearActionFrameViews()
        {
            m_ActionBackendReleaseCompletions.Clear();
            m_ActionSlotReleaseCompletions.Clear();
            m_ConsumedActionBackendReleaseCompletions.Clear();
            m_RetiredActionPlaybacks.Clear();
            m_AnimationSlotPlans = Array.Empty<AnimationSlotFramePlan>();
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
            m_Evaluation.Reset();
            m_MotionMatching.ClearSelections();
            Exception failure = null;
            DisposeStep(DetachExecutionJobs, ref failure);
            DisposeStep(ActorState.Dispose, ref failure);
            DisposeStep(m_Tuning.Dispose, ref failure);
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

        void RequireActionFrame(ulong frameIdentity)
        {
            if (frameIdentity == 0 ||
                m_ActionFrame?.IsValid != true ||
                m_ActionFrame.Identity != frameIdentity)
            {
                throw new InvalidOperationException(
                    "Character Pose Program Action frame is stale.");
            }
        }

        void RequireAnimationSlotFrame(ulong frameIdentity)
        {
            if (frameIdentity == 0 ||
                !m_AnimationSlotFrame.IsValid ||
                m_AnimationSlotFrame.FrameIdentity != frameIdentity)
            {
                throw new InvalidOperationException(
                    "Character Pose Program Animation Slot frame is stale.");
            }
        }

        void RequirePresentationWorkspaceFrame(ulong frameIdentity)
        {
            if (frameIdentity == 0 ||
                !m_PresentationWorkspaceFrame.IsValid ||
                m_PresentationWorkspaceFrame.Identity != frameIdentity)
            {
                throw new InvalidOperationException(
                    "Character Pose Program Presentation workspace frame is stale.");
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
