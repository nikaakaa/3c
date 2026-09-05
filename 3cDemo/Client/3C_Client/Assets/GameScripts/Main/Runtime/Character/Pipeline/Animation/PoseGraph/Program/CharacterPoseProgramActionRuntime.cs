using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Animation.BlendStack;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Pipeline.Animation.Lifecycle;
using ThirdPersonCharacter.Pipeline.Animation.Presentation;
using ThirdPersonCharacter.Pipeline.Animation.Sources;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal sealed class CharacterPoseProgramActionRuntime
    {
        readonly CharacterPoseActorState m_ActorState;
        readonly CharacterPoseSourceModule m_SourceModule;
        readonly PresentationFrameWorkspace m_Workspace;
        readonly List<ActionBackendReleaseCompletion> m_BackendReleases;
        readonly List<AnimationSlotSourceReleaseCompletion> m_SlotReleases;
        readonly FixedCapacityFrameBuffer<ActionBackendReleaseCompletion>
            m_ConsumedBackendReleases;
        readonly FixedCapacityFrameBuffer<AnimationPlaybackId>
            m_RetiredPlaybacks;
        CharacterActionPlaybackFrameTransaction m_ActionFrame;
        AnimationSlotMutationLease m_SlotFrame;
        PresentationFrameWorkspaceLease m_WorkspaceFrame;
        IReadOnlyList<AnimationSlotFramePlan> m_SlotPlans =
            Array.Empty<AnimationSlotFramePlan>();

        internal CharacterPoseProgramActionRuntime(
            CharacterPoseActorState actorState,
            CharacterPoseSourceModule sourceModule,
            PresentationFrameWorkspace workspace)
        {
            m_ActorState = actorState ??
                throw new ArgumentNullException(nameof(actorState));
            m_SourceModule = sourceModule ??
                throw new ArgumentNullException(nameof(sourceModule));
            m_Workspace = workspace ??
                throw new ArgumentNullException(nameof(workspace));
            int frameCapacity = actorState.ActionPlayback.FrameCapacity;
            int releaseCapacity =
                actorState.ActionPlayback.BackendReleaseCompletionCapacity;
            m_BackendReleases =
                new List<ActionBackendReleaseCompletion>(releaseCapacity);
            m_SlotReleases =
                new List<AnimationSlotSourceReleaseCompletion>(frameCapacity);
            m_ConsumedBackendReleases =
                new FixedCapacityFrameBuffer<ActionBackendReleaseCompletion>(
                    releaseCapacity);
            m_RetiredPlaybacks =
                new FixedCapacityFrameBuffer<AnimationPlaybackId>(
                    frameCapacity);
        }

        internal PresentationFrameWorkspaceLease WorkspaceFrame =>
            m_WorkspaceFrame;
        internal IReadOnlyList<ActionSlotSourceUsage> SourceUsages =>
            m_Workspace.ActionUsages;
        internal IReadOnlyList<AnimationPlaybackId> RetiredPlaybacks =>
            m_RetiredPlaybacks;

        internal bool MatchesOpenFrame(ulong frameIdentity) =>
            frameIdentity != 0 &&
            m_ActionFrame?.IsValid == true &&
            m_ActionFrame.Identity == frameIdentity &&
            m_SlotFrame.IsValid &&
            m_SlotFrame.FrameIdentity == frameIdentity &&
            m_WorkspaceFrame.IsValid &&
            m_WorkspaceFrame.Identity == frameIdentity;

        internal void RequireWorkspace(ulong frameIdentity) =>
            RequireWorkspaceFrame(frameIdentity);

        internal void Begin(ulong frameIdentity, ulong presentationFrame)
        {
            if (m_ActionFrame?.IsValid == true || m_SlotFrame.IsValid ||
                m_WorkspaceFrame.IsValid)
            {
                throw new InvalidOperationException(
                    "Character Pose Program Action frame is already open.");
            }
            m_WorkspaceFrame = m_Workspace.Begin(
                frameIdentity,
                presentationFrame);
            try
            {
                m_ActionFrame = m_ActorState.ActionPlayback.BeginFrame(
                    frameIdentity,
                    presentationFrame);
            }
            catch
            {
                m_Workspace.Discard(m_WorkspaceFrame);
                m_WorkspaceFrame = default;
                throw;
            }
        }

        internal void BeginSlot(ulong frameIdentity)
        {
            RequireActionFrame(frameIdentity);
            if (m_SlotFrame.IsValid)
            {
                throw new InvalidOperationException(
                    "Character Pose Program Animation Slot frame is already open.");
            }
            m_SlotFrame = m_ActorState.AnimationSlots.BeginFrame(
                frameIdentity);
        }

        internal void BeginSampling(
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

        internal void CommitSlot(ulong frameIdentity)
        {
            RequireSlotFrame(frameIdentity);
            m_ActorState.AnimationSlots.CommitFrame(m_SlotFrame);
            m_SlotFrame = default;
        }

        internal void CommitAction(ulong frameIdentity)
        {
            RequireActionFrame(frameIdentity);
            if (m_SlotFrame.IsValid)
            {
                throw new InvalidOperationException(
                    "Character Pose Program Animation Slot frame must commit first.");
            }
            m_ActorState.ActionPlayback.Commit(m_ActionFrame);
            m_ActionFrame = null;
        }

        internal void CommitWorkspace(ulong frameIdentity)
        {
            RequireWorkspaceFrame(frameIdentity);
            m_Workspace.Commit(m_WorkspaceFrame);
            m_WorkspaceFrame = default;
        }

        internal void DiscardSlot(ulong frameIdentity)
        {
            RequireSlotFrame(frameIdentity);
            m_ActorState.AnimationSlots.DiscardFrame(m_SlotFrame);
            m_SlotFrame = default;
        }

        internal void DiscardAction(ulong frameIdentity)
        {
            RequireActionFrame(frameIdentity);
            if (m_SlotFrame.IsValid)
            {
                throw new InvalidOperationException(
                    "Character Pose Program Animation Slot frame must discard first.");
            }
            m_ActorState.ActionPlayback.DiscardFrame(m_ActionFrame);
            m_ActionFrame = null;
            ClearFrameViews();
        }

        internal void DiscardWorkspace(ulong frameIdentity)
        {
            RequireWorkspaceFrame(frameIdentity);
            m_Workspace.Discard(m_WorkspaceFrame);
            m_WorkspaceFrame = default;
        }

        internal void ClearLeaseState()
        {
            m_ActionFrame = null;
            m_SlotFrame = default;
            m_WorkspaceFrame = default;
        }

        internal void Publish(in ActionAnimationPlaybackCommand command) =>
            m_ActorState.ActionPlayback.Publish(command);

        internal void Retire(in ActionAnimationPlaybackCommand command) =>
            m_ActorState.ActionPlayback.Retire(command);

        internal void Replace(
            EventId targetEventId,
            in ActionAnimationPlaybackCommand replacement) =>
            m_ActorState.ActionPlayback.Replace(targetEventId, replacement);

        internal IReadOnlyList<ActionAnimationPlaybackLifecycleFrame>
            PrepareLifecycle(ulong frameIdentity)
        {
            RequireAllFrames(frameIdentity);
            m_ConsumedBackendReleases.Clear();
            m_RetiredPlaybacks.Clear();
            m_SourceModule.CopyActionBackendReleaseCompletions(
                m_BackendReleases);
            for (int i = 0; i < m_BackendReleases.Count; i++)
            {
                ActionBackendReleaseCompletion completion =
                    m_BackendReleases[i];
                m_Workspace.AddReleaseCompletion(
                    m_WorkspaceFrame,
                    completion);
                m_ConsumedBackendReleases.Add(completion);
            }
            m_SourceModule.ValidateActionBackendReleaseAcknowledgements(
                m_ConsumedBackendReleases);
            m_ActorState.ActionPlayback.ApplyBackendReleaseCompletions(
                m_ActionFrame,
                m_BackendReleases);
            IReadOnlyList<ActionAnimationPlaybackLifecycleFrame> lifecycle =
                m_ActorState.ActionPlayback.BuildLifecycleFrame(m_ActionFrame);
            RebaseRetiredPlaybacks(lifecycle);
            m_SlotPlans = m_ActorState.AnimationSlots.BuildFramePlans(
                m_SlotFrame,
                lifecycle);
            return lifecycle;
        }

        internal void ProjectSamples(
            ulong frameIdentity,
            IReadOnlyList<ActionAnimationPlaybackLifecycleFrame> lifecycle,
            double presentationSampleTick,
            float presentationDeltaSeconds)
        {
            RequireActionFrame(frameIdentity);
            m_SourceModule.ProjectActionPresentationSamples(
                m_ActorState.ActionPlayback,
                m_ActionFrame,
                lifecycle,
                presentationSampleTick,
                presentationDeltaSeconds);
        }

        internal void ResolveFrames(ulong frameIdentity)
        {
            RequireWorkspaceFrame(frameIdentity);
            m_SourceModule.ResolveActionPresentationFrames(
                frameIdentity,
                m_Workspace,
                m_WorkspaceFrame);
        }

        internal void ValidateSampling(ulong frameIdentity) =>
            m_SourceModule.ValidateActionSamplingFrame(frameIdentity);

        internal void CommitSampling(ulong frameIdentity) =>
            m_SourceModule.CommitActionSamplingFrame(frameIdentity);

        internal void DiscardSampling(ulong frameIdentity) =>
            m_SourceModule.DiscardActionSamplingFrame(frameIdentity);

        internal void BuildCommittedTimeSnapshots(
            FixedCapacityFrameBuffer<ActionPresentationTimeSnapshot>
                destination) =>
            m_SourceModule.BuildCommittedActionTimeSnapshots(destination);

        internal void ResetSampling() =>
            m_SourceModule.ResetActionSampling();

        internal void PublishSources(
            ulong frameIdentity,
            IDictionary<AnimationPlayerSourceSampleKey,
                AnimationResolvedPoseSourceSample> sourceSamples)
        {
            RequireAllFrames(frameIdentity);
            if (sourceSamples == null)
            {
                throw new ArgumentException(
                    "Character Pose Program Action source input is invalid.");
            }
            IReadOnlyList<AnimationSlotActionSourcePlan> sourcePlans =
                m_ActorState.AnimationSlots.CollectActionSourcePlans(
                    m_SlotFrame);
            if (sourcePlans.Count > m_ActorState.ActionPlayback.FrameCapacity)
            {
                throw new InvalidOperationException(
                    "Action source sample capacity was exceeded.");
            }
            IReadOnlyDictionary<AnimationPlaybackId,
                ActionAnimationPlaybackFrame> frames =
                    m_Workspace.ActionFrames;
            for (int i = 0; i < sourcePlans.Count; i++)
            {
                AnimationSlotActionSourcePlan sourcePlan = sourcePlans[i];
                if (!frames.TryGetValue(
                        sourcePlan.PlaybackId,
                        out ActionAnimationPlaybackFrame frame) ||
                    !m_ActorState.ActionPlayback.Bindings.TryGet(
                        sourcePlan.PlaybackId.ProducerId,
                        out ResolvedActionAnimationBinding binding) ||
                    binding.SlotId != sourcePlan.SlotId ||
                    binding.SlotNodeId != sourcePlan.SlotNodeId)
                {
                    throw new InvalidOperationException(
                        $"Animation Slot Action source '{sourcePlan.PlaybackId}' has no exact resolved frame.");
                }
                PublishSourceFrame(
                    in frame,
                    in binding,
                    sourcePlan.SelectionGeneration,
                    m_ActorState.NextPresentationRequestSequence(),
                    sourceSamples,
                    sourcePlan.Current);
            }
            for (int i = 0; i < m_SlotPlans.Count; i++)
            {
                AnimationSlotFramePlan plan = m_SlotPlans[i];
                if (!plan.TargetsSourcePose)
                    continue;
                PublishSourcePose(
                    plan.SlotId,
                    plan.SlotNodeId,
                    m_ActorState.NextPresentationRequestSequence());
            }
        }

        internal void CompleteReleaseProtocol(ulong frameIdentity)
        {
            RequireAllFrames(frameIdentity);
            m_SourceModule.CopyActionSlotReleaseCompletions(m_SlotReleases);
            for (int i = 0; i < m_SlotReleases.Count; i++)
            {
                AnimationSlotSourceReleaseCompletion completion =
                    m_SlotReleases[i];
                m_ActorState.AnimationSlots.CompleteSourceRelease(
                    m_SlotFrame,
                    completion.SlotId,
                    completion.PlaybackId);
            }
            m_ActorState.AnimationSlots.PublishActionUsages(
                m_SlotFrame,
                m_Workspace,
                m_WorkspaceFrame);
            m_ActorState.ActionPlayback.ReplaceSlotUsageBatch(
                m_ActionFrame,
                m_Workspace.ActionUsages);
            IReadOnlyList<ActionAnimationPlaybackLifecycleFrame> lifecycle =
                m_ActorState.ActionPlayback.BuildLifecycleFrame(m_ActionFrame);
            m_ActorState.AnimationSlots.PublishRetirementPermissions(
                m_SlotFrame,
                lifecycle,
                m_Workspace,
                m_WorkspaceFrame);
            m_ActorState.ActionPlayback.ApplyRetirementPermissions(
                m_ActionFrame,
                m_Workspace.RetirementPermissions);
            lifecycle = m_ActorState.ActionPlayback.BuildLifecycleFrame(
                m_ActionFrame);
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
                if (m_ActorState.SourceRetirement
                    .TryPrepareActionRequest(
                        snapshot.PlaybackId,
                        out ActionBackendReleaseRequest request))
                {
                    m_ActorState.ActionPlayback.RegisterBackendReleaseRequest(
                        m_ActionFrame,
                        request);
                    m_Workspace.AddReleaseRequest(
                        m_WorkspaceFrame,
                        request);
                    continue;
                }
                m_ActorState.ActionPlayback.RetireWithoutBackendResources(
                    m_ActionFrame,
                    snapshot.PlaybackId);
                TryAddRetiredPlayback(snapshot.PlaybackId);
            }
        }

        internal void ValidateAction(ulong frameIdentity)
        {
            RequireActionFrame(frameIdentity);
            m_ActorState.ActionPlayback.ValidateFrame(m_ActionFrame);
        }

        internal IReadOnlyList<ActionAnimationPlaybackLifecycleSnapshot>
            BuildCommittedLifecycleSnapshot() =>
            m_ActorState.ActionPlayback.BuildCommittedLifecycleSnapshot();

        internal void ResetSlots()
        {
            m_ActorState.AnimationSlots.Reset();
            m_SlotPlans = Array.Empty<AnimationSlotFramePlan>();
        }

        internal void ResetPlayback()
        {
            m_ActorState.ActionPlayback.Reset();
            ClearFrameViews();
        }

        void PublishSourceFrame(
            in ActionAnimationPlaybackFrame frame,
            in ResolvedActionAnimationBinding binding,
            AnimationPoseSelectionGeneration selectionGeneration,
            ulong presentationRequestSequence,
            IDictionary<AnimationPlayerSourceSampleKey,
                AnimationResolvedPoseSourceSample> sourceSamples,
            bool select)
        {
            if (!frame.IsValid || !binding.IsValid ||
                !frame.PlaybackId.ProducerId.Equals(binding.ProducerId) ||
                frame.AnimationChannelId != binding.AnimationChannelId ||
                !string.Equals(
                    frame.ProgramProducerId,
                    binding.ProgramProducerId,
                    StringComparison.Ordinal) ||
                !selectionGeneration.IsValid ||
                presentationRequestSequence == 0 || sourceSamples == null ||
                !m_ActorState.NodeRuntimeIndex.TryGetStackRoute(
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
            PresentationPoseSampleTime sampleTime = frame.ProjectedSampleTime;
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
            {
                CharacterPoseSourceReadinessTarget target =
                    CharacterPoseSourceReadinessTarget.FromClips(
                        CharacterPoseSourcePreparationKind.Action,
                        sourceId,
                        binding.SlotNodeId,
                        -1,
                        request.Clips);
                if (!m_SourceModule.TryDeferSource(in target))
                {
                    route.PushSelection(stack, in request);
                }
            }
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
        }

        void PublishSourcePose(
            AnimationSlotId slotId,
            PoseNodeId slotNodeId,
            ulong presentationRequestSequence)
        {
            if (!slotId.IsValid || !slotNodeId.IsValid ||
                presentationRequestSequence == 0 ||
                !m_ActorState.NodeRuntimeIndex.TryGetStackRoute(
                    slotNodeId,
                    out AnimationBlendStackRuntime stack,
                    out CharacterAnimationTransitionRouteRuntime route) ||
                !route.IsAnimationSlot || route.SlotId != slotId)
            {
                throw new InvalidOperationException(
                    "Source Pose target has no exact compiled Animation Slot route.");
            }
            route.PushSourcePose(stack, presentationRequestSequence);
        }

        void RebaseRetiredPlaybacks(
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
                     completionIndex < m_ConsumedBackendReleases.Count;
                     completionIndex++)
                {
                    if (m_ConsumedBackendReleases[completionIndex]
                        .PlaybackId.Equals(snapshot.PlaybackId))
                    {
                        completedThisFrame = true;
                        break;
                    }
                }
                if (completedThisFrame)
                    TryAddRetiredPlayback(snapshot.PlaybackId);
            }
        }

        bool TryAddRetiredPlayback(AnimationPlaybackId playbackId)
        {
            for (int i = 0; i < m_RetiredPlaybacks.Count; i++)
            {
                if (m_RetiredPlaybacks[i].Equals(playbackId))
                    return false;
            }
            m_RetiredPlaybacks.Add(playbackId);
            return true;
        }

        void ClearFrameViews()
        {
            m_BackendReleases.Clear();
            m_SlotReleases.Clear();
            m_ConsumedBackendReleases.Clear();
            m_RetiredPlaybacks.Clear();
            m_SlotPlans = Array.Empty<AnimationSlotFramePlan>();
        }

        void RequireAllFrames(ulong frameIdentity)
        {
            RequireActionFrame(frameIdentity);
            RequireSlotFrame(frameIdentity);
            RequireWorkspaceFrame(frameIdentity);
        }

        void RequireActionFrame(ulong frameIdentity)
        {
            if (frameIdentity == 0 || m_ActionFrame?.IsValid != true ||
                m_ActionFrame.Identity != frameIdentity)
            {
                throw new InvalidOperationException(
                    "Character Pose Program Action frame is stale.");
            }
        }

        void RequireSlotFrame(ulong frameIdentity)
        {
            if (frameIdentity == 0 || !m_SlotFrame.IsValid ||
                m_SlotFrame.FrameIdentity != frameIdentity)
            {
                throw new InvalidOperationException(
                    "Character Pose Program Animation Slot frame is stale.");
            }
        }

        void RequireWorkspaceFrame(ulong frameIdentity)
        {
            if (frameIdentity == 0 || !m_WorkspaceFrame.IsValid ||
                m_WorkspaceFrame.Identity != frameIdentity)
            {
                throw new InvalidOperationException(
                    "Character Pose Program Presentation workspace frame is stale.");
            }
        }
    }
}
