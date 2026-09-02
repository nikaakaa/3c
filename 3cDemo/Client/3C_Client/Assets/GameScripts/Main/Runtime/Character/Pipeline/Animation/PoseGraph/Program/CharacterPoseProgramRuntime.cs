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

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal sealed class CharacterPoseProgramRuntime : IDisposable
    {
        readonly CharacterPoseProgramImage m_Image;
        readonly PresentationFrameWorkspace m_PresentationWorkspace;
        readonly CharacterPoseProgramMotionMatchingRuntime m_MotionMatching;
        readonly CharacterPoseProgramTuningRuntime m_Tuning;
        readonly CharacterPoseProgramActionRuntime m_Action;
        readonly CharacterPoseProgramEvaluationRuntime m_Evaluation;
        readonly CharacterPoseProgramSourcePreparationRuntime
            m_SourcePreparation;
        readonly CharacterPoseProgramSourceRetirementRuntime
            m_SourceRetirement;
        CharacterPoseProgramFrameLease m_ActiveFrameLease;
        CharacterPoseProgramFrameLease m_CommittingFrameLease;
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
            AnimancerComponent animancerComponent = animancer ? animancer :
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
            CharacterPoseSourceModule source = sourceModule ??
                throw new ArgumentNullException(nameof(sourceModule));
            CharacterPoseWorldContextAdapter world = worldContext ??
                throw new ArgumentNullException(nameof(worldContext));
            m_PresentationWorkspace = presentationWorkspace ??
                throw new ArgumentNullException(nameof(presentationWorkspace));
            CharacterPoseConstraintRuntime constraintRuntime = poseConstraints ??
                throw new ArgumentNullException(nameof(poseConstraints));
            m_Action = new CharacterPoseProgramActionRuntime(
                ActorState,
                source,
                m_PresentationWorkspace);
            m_SourcePreparation =
                new CharacterPoseProgramSourcePreparationRuntime(
                    animancerComponent,
                    ActorState,
                    FramePages,
                    source);
            m_SourceRetirement =
                new CharacterPoseProgramSourceRetirementRuntime(
                    ActorState,
                    source);
            Executor = new CharacterPoseProgramExecutor(
                ExecutionView,
                FramePages,
                ActorState.Inertialization,
                constraintRuntime);
            m_Evaluation = new CharacterPoseProgramEvaluationRuntime(
                ExecutionView,
                ActorState,
                FramePages,
                Executor,
                world,
                m_SourcePreparation);
            m_MotionMatching =
                new CharacterPoseProgramMotionMatchingRuntime(
                    image,
                    ActorState,
                    FramePages,
                    m_Evaluation.State,
                    source,
                    m_PresentationWorkspace);
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
            m_Action.SourceUsages;
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
            m_Action.Begin(frameIdentity, presentationFrame);
        }

        internal void BeginAnimationSlotFrame(ulong frameIdentity)
        {
            RequireAlive();
            m_Action.BeginSlot(frameIdentity);
        }

        internal void BeginActionSamplingFrame(
            ulong frameIdentity,
            ulong presentationFrame,
            bool captureDiagnostics)
        {
            RequireAlive();
            m_Action.BeginSampling(
                frameIdentity,
                presentationFrame,
                captureDiagnostics);
        }

        internal void CommitAnimationSlotFrame(
            CharacterPoseProgramFrameLease lease)
        {
            RequireFrame(lease);
            m_Action.CommitSlot(lease.Lineage.FrameIdentity);
        }

        internal void CommitActionPlaybackFrame(
            CharacterPoseProgramFrameLease lease)
        {
            RequireFrame(lease);
            m_Action.CommitAction(lease.Lineage.FrameIdentity);
        }

        internal void DiscardAnimationSlotFrame(ulong frameIdentity)
        {
            RequireAlive();
            m_Action.DiscardSlot(frameIdentity);
        }

        internal void DiscardActionPlaybackFrame(ulong frameIdentity)
        {
            RequireAlive();
            m_Action.DiscardAction(frameIdentity);
        }

        internal void CommitPresentationWorkspaceFrame(
            CharacterPoseProgramFrameLease lease)
        {
            RequireFrame(lease);
            m_Action.CommitWorkspace(lease.Lineage.FrameIdentity);
        }

        internal void DiscardPresentationWorkspaceFrame(
            ulong frameIdentity)
        {
            RequireAlive();
            m_Action.DiscardWorkspace(frameIdentity);
        }

        internal void PublishActionCommand(
            in ActionAnimationPlaybackCommand command) =>
            m_Action.Publish(in command);

        internal void RetireActionCommand(
            in ActionAnimationPlaybackCommand command) =>
            m_Action.Retire(in command);

        internal void ReplaceActionCommand(
            EventId targetEventId,
            in ActionAnimationPlaybackCommand replacement) =>
            m_Action.Replace(targetEventId, in replacement);

        internal IReadOnlyList<ActionAnimationPlaybackLifecycleFrame>
            PrepareActionLifecycleFrame(
                CharacterPoseProgramFrameLease lease)
        {
            RequireFrame(lease);
            return m_Action.PrepareLifecycle(
                lease.Lineage.FrameIdentity);
        }

        internal void ProjectActionPresentationSamples(
            CharacterPoseProgramFrameLease lease,
            IReadOnlyList<ActionAnimationPlaybackLifecycleFrame> lifecycle,
            double presentationSampleTick,
            float presentationDeltaSeconds)
        {
            RequireFrame(lease);
            m_Action.ProjectSamples(
                lease.Lineage.FrameIdentity,
                lifecycle,
                presentationSampleTick,
                presentationDeltaSeconds);
        }

        internal void ResolveActionPresentationFrames(
            CharacterPoseProgramFrameLease lease)
        {
            RequireFrame(lease);
            m_Action.ResolveFrames(lease.Lineage.FrameIdentity);
        }

        internal void ValidateActionSamplingFrame(
            CharacterPoseProgramFrameLease lease)
        {
            RequireFrame(lease);
            m_Action.ValidateSampling(lease.Lineage.FrameIdentity);
        }

        internal void CommitActionSamplingFrame(
            CharacterPoseProgramFrameLease lease)
        {
            RequireFrame(lease);
            m_Action.CommitSampling(lease.Lineage.FrameIdentity);
        }

        internal void DiscardActionSamplingFrame(ulong frameIdentity)
        {
            RequireAlive();
            m_Action.DiscardSampling(frameIdentity);
        }

        internal void BuildCommittedActionTimeSnapshots(
            FixedCapacityFrameBuffer<ActionPresentationTimeSnapshot>
                destination)
        {
            RequireAlive();
            m_Action.BuildCommittedTimeSnapshots(destination);
        }

        internal void ResetActionSampling()
        {
            RequireAlive();
            m_Action.ResetSampling();
        }

        internal void PublishActionSources(
            CharacterPoseProgramFrameLease lease,
            IDictionary<AnimationPlayerSourceSampleKey,
                AnimationResolvedPoseSourceSample> sourceSamples)
        {
            RequireFrame(lease);
            m_Action.PublishSources(
                lease.Lineage.FrameIdentity,
                sourceSamples);
        }

        internal void CompleteActionReleaseProtocol(
            CharacterPoseProgramFrameLease lease)
        {
            RequireFrame(lease);
            m_Action.CompleteReleaseProtocol(
                lease.Lineage.FrameIdentity);
        }

        internal void ValidateActionFrame(
            CharacterPoseProgramFrameLease lease)
        {
            RequireFrame(lease);
            m_Action.ValidateAction(lease.Lineage.FrameIdentity);
        }

        internal IReadOnlyList<ActionAnimationPlaybackLifecycleSnapshot>
            BuildCommittedActionLifecycleSnapshot() =>
            m_Action.BuildCommittedLifecycleSnapshot();

        internal IReadOnlyList<AnimationPlaybackId>
            RetiredActionPlaybacks => m_Action.RetiredPlaybacks;

        internal void ResetAnimationSlots()
        {
            RequireAlive();
            m_Action.ResetSlots();
        }

        internal void ResetActionPlayback()
        {
            RequireAlive();
            m_Action.ResetPlayback();
        }

        internal void ResetPresentationWorkspace()
        {
            RequireAlive();
            m_PresentationWorkspace.Reset();
            m_MotionMatching.ClearSelections();
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
            m_SourceRetirement
                .ExecutePreparedActionBackendReleaseRequests();
        }

        internal bool HasSequencePreview =>
            m_SourcePreparation.HasSequencePreview;

        bool ApplySequencePreview()
        {
            RequireFrame(m_ActiveFrameLease);
            return m_SourcePreparation.ApplySequencePreview();
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
            m_Action.RequireWorkspace(lease.Lineage.FrameIdentity);
            PresentationFrameWorkspaceLease workspaceFrame =
                m_Action.WorkspaceFrame;
            if (!factFrame.IsValid ||
                !workspaceFrame.IsValid)
            {
                throw new ArgumentException(
                    "Pose State frame finalization is invalid.",
                    nameof(factFrame));
            }
            if (m_SourcePreparation.HasSequencePreview)
                return;
            ActorState.PoseStateSources.EvaluateTransitions(
                in factFrame,
                FramePages,
                m_PresentationWorkspace,
                workspaceFrame);
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
            m_SourcePreparation.IsSequencePreviewPlayer(playerIndex);

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
            if (m_ActiveFrameLease.IsValid)
            {
                throw new ArgumentException(
                    "Clip Preview sample is invalid.");
            }
            m_SourcePreparation.SetSequencePreview(
                m_Image,
                sourceIndex,
                sampleTime,
                resetContinuity);
        }

        internal void ClearSequencePreview()
        {
            RequireAlive();
            if (m_ActiveFrameLease.IsValid)
            {
                throw new InvalidOperationException(
                    "Clip Preview cannot clear during a frame.");
            }
            m_SourcePreparation.ClearSequencePreview();
        }

        internal void BeginFrame(CharacterPoseProgramFrameLease lease)
        {
            RequireAlive();
            if (!lease.IsValid ||
                m_ActiveFrameLease.IsValid ||
                m_CommittingFrameLease.IsValid ||
                !m_Action.MatchesOpenFrame(
                    lease.Lineage.FrameIdentity))
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
            m_Action.ClearLeaseState();
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
            m_Evaluation.BeginFrame(completionIdentity);
        }

        internal void BeginSourceEvaluation(
            CharacterPoseProgramFrameLease lease,
            ulong completionIdentity)
        {
            BeginEvaluationFrame(lease, completionIdentity);
            m_SourcePreparation.BeginFrame(completionIdentity);
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
            m_SourcePreparation.PrepareStacks(
                sourceLease,
                in preparations,
                presentationDeltaSeconds,
                actionSourceSamples,
                providerSourceSamples);
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
            m_SourcePreparation.PrepareDirectPlayers(
                sourceLease,
                in preparations,
                presentationDeltaSeconds,
                sourceSamples);
        }

        internal void PrepareSequenceSources(
            CharacterPoseProgramFrameLease lease,
            CharacterPoseSourceFrameLease sourceLease,
            in CharacterPoseSourcePreparationView preparations,
            float presentationDeltaSeconds,
            in CharacterPoseSourceTuningView sourceTuning)
        {
            RequireFrame(lease);
            m_SourcePreparation.PrepareClipPlayers(
                sourceLease,
                in preparations,
                presentationDeltaSeconds,
                in sourceTuning);
        }

        internal void PrepareBlendSpaceSources(
            CharacterPoseProgramFrameLease lease,
            CharacterPoseSourceFrameLease sourceLease,
            in CharacterPoseSourcePreparationView preparations,
            float presentationDeltaSeconds)
        {
            RequireFrame(lease);
            m_SourcePreparation.PrepareBlendSpacePlayers(
                sourceLease,
                in preparations,
                presentationDeltaSeconds);
        }

        internal void PrepareEvaluationJobsAndRetirements(
            CharacterPoseProgramFrameLease lease,
            in CharacterPoseSourcePreparedResources preparedSources,
            ulong completionIdentity)
        {
            RequireFrame(lease);
            m_SourcePreparation.PrepareJobs(
                in preparedSources,
                completionIdentity);
            m_SourceRetirement.StageCompletedSources(completionIdentity);
        }

        internal void ValidateSourceRetirements(
            CharacterPoseProgramFrameLease lease,
            CharacterPoseSourceFrameLease sourceLease)
        {
            RequireFrame(lease);
            m_SourceRetirement.Validate(sourceLease);
        }

        internal void FinalizeCommittedSourceRetirements(
            ulong completionIdentity)
        {
            RequireAlive();
            m_SourceRetirement.FinalizeCommitted(completionIdentity);
        }

        internal void ReleaseCompletedSources(ulong completionIdentity)
        {
            RequireAlive();
            m_SourceRetirement.ReleaseCompleted(completionIdentity);
        }

        internal void ReleasePlayerSources()
        {
            RequireAlive();
            m_SourceRetirement.ReleasePlayers();
        }

        internal void BindEvaluationExecution(
            CharacterPoseProgramFrameLease lease,
            in CharacterPoseFrameLineage lineage,
            in CharacterFinalPosePublicationOutputBinding finalOutput,
            bool recordDiagnostics)
        {
            RequireFrame(lease);
            CharacterPoseProgramTuningView tuning =
                m_Tuning.Require(lineage.TuningGeneration);
            m_Evaluation.Bind(
                lease,
                in lineage,
                in tuning,
                in finalOutput,
                recordDiagnostics);
            m_SourcePreparation.InstallOrUpdateJobs();
        }

        internal void DetachExecutionJobs() =>
            m_SourcePreparation.DetachJobs();

        internal void CommitEvaluationFrame(
            CharacterPoseProgramFrameLease lease,
            ulong completionIdentity)
        {
            RequireFrame(lease);
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

        internal void PrepareEvaluation(
            CharacterPoseProgramFrameLease lease,
            in CharacterPoseProgramPrepared prepared,
            float presentationDeltaSeconds)
        {
            RequireFrame(lease);
            m_Evaluation.Prepare(
                lease,
                in prepared,
                presentationDeltaSeconds);
        }

        internal CharacterPoseProgramOutputResult CompleteEvaluation(
            CharacterPoseProgramFrameLease lease,
            in CharacterPoseProgramPrepared prepared,
            in CharacterBodyPresentationFrame bodyFrame,
            in CharacterPresentationFactFrame factFrame)
        {
            RequireFrame(lease);
            return m_Evaluation.Complete(
                lease,
                in prepared,
                in bodyFrame,
                in factFrame);
        }

        internal float RequirePreparedEvaluationDeltaSeconds(
            CharacterPoseProgramFrameLease lease,
            in CharacterPoseProgramPrepared prepared)
        {
            RequireFrame(lease);
            return m_Evaluation.RequireDeltaSeconds(in prepared);
        }

        internal void CompleteNodeEvaluation(
            CharacterPoseProgramFrameLease lease,
            ulong completionIdentity)
        {
            RequireFrame(lease);
            m_Evaluation.CompleteNodes(completionIdentity);
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
            m_Action.RequireWorkspace(lease.Lineage.FrameIdentity);
            return m_MotionMatching.BuildDemandBatch(
                presentationFrame,
                resetSequence,
                m_Action.WorkspaceFrame);
        }

        internal void ApplyMotionMatchingSelections(
            CharacterPoseProgramFrameLease lease,
            in MotionMatchingFrameResolution resolution)
        {
            RequireFrame(lease);
            m_Action.RequireWorkspace(lease.Lineage.FrameIdentity);
            m_MotionMatching.ApplySelections(
                in resolution,
                m_Action.WorkspaceFrame);
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
            RequireAlive();
            return m_Evaluation.CaptureCommittedDiagnostics(
                projector,
                in result,
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
