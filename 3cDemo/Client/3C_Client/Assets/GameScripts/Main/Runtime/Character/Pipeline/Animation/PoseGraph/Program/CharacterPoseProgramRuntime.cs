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
        readonly CharacterPoseProgramActorRuntime m_ActorRuntime;
        readonly CharacterPoseActorCommittedDiagnosticsProjector
            m_ActorDiagnostics;
        readonly CharacterPoseWorkerActorRegistration m_WorkerRegistration;
        CharacterPoseProgramFrameLease m_ActiveFrameLease;
        CharacterPoseProgramFrameLease m_CommittingFrameLease;
        CharacterPoseProgramCommittedDiagnosticsView
            m_FrameProgramDiagnostics;
        CharacterPoseActorCommittedDiagnosticsView m_FrameActorDiagnostics;
        AnimationPresentationDiagnosticsInterest m_FrameDiagnosticsInterest;
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
            PresentationFrameWorkspace presentationWorkspace,
            CharacterPoseActorCommittedDiagnosticsProjector actorDiagnostics,
            CharacterPoseProgramCommittedDiagnosticsProjector
                programDiagnostics,
            ActorId actorId,
            CharacterPoseWorkerActorRegistration workerRegistration)
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
            m_ActorDiagnostics = actorDiagnostics ??
                throw new ArgumentNullException(nameof(actorDiagnostics));
            CharacterPoseProgramCommittedDiagnosticsProjector diagnostics =
                programDiagnostics ??
                throw new ArgumentNullException(nameof(programDiagnostics));
            CharacterPoseConstraintRuntime constraintRuntime = poseConstraints ??
                throw new ArgumentNullException(nameof(poseConstraints));
            m_WorkerRegistration = workerRegistration ??
                throw new ArgumentNullException(nameof(workerRegistration));
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
            m_ActorRuntime = new CharacterPoseProgramActorRuntime(
                image,
                ExecutionView,
                ActorState,
                FramePages,
                m_Action,
                m_PresentationWorkspace,
                source,
                m_SourcePreparation);
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
                m_SourcePreparation,
                diagnostics,
                actorId,
                m_WorkerRegistration);
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

        internal void NotifyActionDomainEvent(
            ulong actionInstanceId,
            EventId causeEventId) =>
            m_Action.NotifyDomainEvent(actionInstanceId, causeEventId);

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

        internal void Advance(
            CharacterPoseProgramFrameLease lease,
            float presentationDeltaSeconds,
            in CharacterPresentationFactFrame factFrame,
            in CharacterAnimationPoseInputFrame parameterFrame,
            in CharacterPoseSourceTuningView sourceTuning)
        {
            RequireFrame(lease);
            m_ActorRuntime.Advance(
                lease,
                presentationDeltaSeconds,
                in factFrame,
                in parameterFrame,
                in sourceTuning);
        }

        internal void FinalizePoseStateFrame(
            CharacterPoseProgramFrameLease lease,
            in CharacterPresentationFactFrame factFrame,
            in CharacterAnimationPoseInputFrame parameterFrame)
        {
            RequireFrame(lease);
            m_ActorRuntime.FinalizePoseStateFrame(
                lease,
                in factFrame,
                in parameterFrame);
        }

        internal bool IsSequencePreviewPlayer(int playerIndex) =>
            m_SourcePreparation.IsSequencePreviewPlayer(playerIndex);

        internal bool IsPlayerActive(int playerIndex) =>
            m_ActorRuntime.IsPlayerActive(playerIndex);

        internal void ClearLinkedPoseFrameSelection()
        {
            RequireAlive();
            m_ActorRuntime.ClearLinkedPoseFrameSelection();
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
            m_FrameProgramDiagnostics = default;
            m_FrameActorDiagnostics = default;
            m_FrameDiagnosticsInterest =
                AnimationPresentationDiagnosticsInterest.None;
            FramePages.BeginFrame();
            m_ActorDiagnostics.BeginFrame();
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
            m_FrameProgramDiagnostics = default;
            m_FrameActorDiagnostics = default;
            m_FrameDiagnosticsInterest =
                AnimationPresentationDiagnosticsInterest.None;
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
            m_ActorRuntime.BeginFrame(
                linkedPose,
                groups,
                resetCompletionIdentity);
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
            m_ActorRuntime.CommitFrame();
            m_CommittingFrameLease = default;
        }

        internal void DiscardActorNodeFrames(
            CharacterPoseProgramFrameLease lease)
        {
            RequireFrame(lease);
            m_ActorRuntime.DiscardNodeFrames();
        }

        internal void DiscardRootOrientationWarpFrames(
            CharacterPoseProgramFrameLease lease)
        {
            RequireFrame(lease);
            m_ActorRuntime.DiscardRootOrientationWarpFrames();
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

        internal void BeginEvaluationExecution(
            CharacterPoseProgramFrameLease lease,
            in CharacterPoseProgramPrepared prepared,
            in CharacterBodyPresentationFrame bodyFrame,
            in CharacterPresentationFactFrame factFrame,
            in CharacterAnimationPoseInputFrame parameterFrame)
        {
            RequireFrame(lease);
            m_Evaluation.BeginCompletion(
                lease,
                in prepared,
                in bodyFrame,
                in factFrame,
                in parameterFrame);
        }

        internal bool TryAdvanceEvaluationExecution(
            CharacterPoseProgramFrameLease lease,
            out CharacterPoseWorkerStageLease workerLease)
        {
            RequireFrame(lease);
            return m_Evaluation.TryAdvanceCompletion(out workerLease);
        }

        internal CharacterPoseProgramOutputResult FinishEvaluationExecution(
            CharacterPoseProgramFrameLease lease)
        {
            RequireFrame(lease);
            return m_Evaluation.FinishCompletion();
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
            m_ActorDiagnostics.Reset();
            m_FrameProgramDiagnostics = default;
            m_FrameActorDiagnostics = default;
            m_FrameDiagnosticsInterest =
                AnimationPresentationDiagnosticsInterest.None;
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
            m_ActorRuntime.ResetBlendState(completionIdentity);
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
            m_ActorRuntime.ResetPoseState(reason);
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
                in CharacterPoseProgramResult result,
                AnimationPresentationDiagnosticsInterest interest)
        {
            RequireAlive();
            if (m_ActiveFrameLease.IsValid ||
                m_CommittingFrameLease.IsValid ||
                !result.IsCompleted ||
                !m_FrameProgramDiagnostics.IsValid ||
                m_FrameProgramDiagnostics.Result.Lineage != result.Lineage ||
                (interest & ~m_FrameDiagnosticsInterest) != 0)
            {
                throw new InvalidOperationException(
                    "Character Pose Program committed diagnostics are unavailable.");
            }
            return m_FrameProgramDiagnostics;
        }

        internal CharacterPoseActorCommittedDiagnosticsView
            CaptureCommittedActorDiagnostics(
                in CharacterPoseProgramResult result,
                AnimationPresentationDiagnosticsInterest interest)
        {
            RequireAlive();
            if (m_ActiveFrameLease.IsValid ||
                m_CommittingFrameLease.IsValid ||
                !result.IsCompleted ||
                !m_FrameActorDiagnostics.IsValid ||
                m_FrameActorDiagnostics.Result.Lineage != result.Lineage ||
                (interest & ~m_FrameDiagnosticsInterest) != 0)
            {
                throw new InvalidOperationException(
                    "Character Pose Actor committed diagnostics are unavailable.");
            }
            return m_FrameActorDiagnostics;
        }

        internal void PreparePendingDiagnostics(
            CharacterPoseProgramFrameLease lease,
            in CharacterPoseProgramResult result,
            in CharacterPoseProgramOutputResult output,
            in ComposedAnimationPoseFrame outputFrame,
            AnimationPresentationDiagnosticsInterest interest)
        {
            RequireFrame(lease);
            if (interest == AnimationPresentationDiagnosticsInterest.None ||
                m_FrameActorDiagnostics.IsValid ||
                m_FrameProgramDiagnostics.IsValid)
            {
                throw new InvalidOperationException(
                    "Character Pose Program Pending diagnostics request is invalid.");
            }
            m_FrameActorDiagnostics = m_ActorDiagnostics.Capture(
                in result,
                ActorState.Stacks,
                ActorState.Routes,
                ActorState.PoseStateSources.StateMachines,
                ActorState.Inertialization,
                ActorState.PoseStateSources.ClipPlayers,
                ActorState.PoseStateSources.BlendSpacePlayers,
                ActorState.RootOrientationWarps,
                interest,
                false);
            m_FrameProgramDiagnostics =
                m_Evaluation.CapturePendingDiagnostics(
                    in result,
                    in output,
                    in outputFrame,
                    interest);
            m_FrameDiagnosticsInterest = interest;
        }

        internal string PrepareTuningCandidate(
            CharacterPoseTuningLayout layout,
            CharacterPoseTuningParameterBlock block,
            ulong generation) =>
            m_Tuning.Prepare(layout, block, generation);

        internal void CommitTuningCandidate(ulong generation) =>
            m_Tuning.Commit(generation);

        internal void DiscardTuningCandidate() => m_Tuning.Discard();
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
            DisposeStep(m_WorkerRegistration.Dispose, ref failure);
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
