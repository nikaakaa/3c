using System;
using System.Collections.Generic;
using Animancer;
using BTSMTL.Diagnostics;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Pipeline.Animation.Lifecycle;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;
using ThirdPersonCharacter.Pipeline.Animation.Presentation;
using ThirdPersonCharacter.Pipeline.Animation.Resources;
using ThirdPersonCharacter.Pipeline.Animation.Sources;
using ThirdPersonCharacter.Pipeline.Diagnostics;
using ThirdPersonPerformance.Instrumentation;
using ThirdPersonSimulation;
using Unity.Profiling;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    public sealed class CharacterAnimationPresentationRuntime : IDisposable
    {
        static readonly ProfilerMarker ActionLifecycleMarker =
            new ProfilerMarker(CharacterPerformanceMetrics.ActionLifecycleName);
        static readonly ProfilerMarker TransactionBeginMarker =
            new ProfilerMarker(CharacterPerformanceMetrics.TransactionBeginName);
        static readonly ProfilerMarker ActionSamplingMarker =
            new ProfilerMarker(CharacterPerformanceMetrics.ActionSamplingName);
        static readonly ProfilerMarker PoseRoutingMarker =
            new ProfilerMarker(CharacterPerformanceMetrics.PoseRoutingName);
        static readonly ProfilerMarker MotionMatchingMarker =
            new ProfilerMarker(CharacterPerformanceMetrics.MotionMatchingName);
        static readonly ProfilerMarker ReleaseProtocolMarker =
            new ProfilerMarker(CharacterPerformanceMetrics.ReleaseProtocolName);
        static readonly ProfilerMarker FrameCommitMarker =
            new ProfilerMarker(CharacterPerformanceMetrics.FrameCommitName);
        static readonly ProfilerMarker PostCommitMarker =
            new ProfilerMarker(CharacterPerformanceMetrics.PostCommitName);

        readonly ActorId m_ActorId;
        readonly CharacterAnimationPresentationBindings m_Bindings;
        readonly CharacterPoseRuntimeComposition m_PoseModules;
        readonly CharacterPoseTuningCoordinator m_PoseTuning;
        readonly CharacterPoseFrameCoordinator m_PoseFrame;
        readonly CharacterPoseMotionMatchingCoordinator m_PoseMotionMatching;
        readonly CharacterAnimationPresentationDiagnosticsCoordinator
            m_PresentationDiagnostics;
        readonly Dictionary<AnimationPlayerSourceSampleKey,
            AnimationResolvedPoseSourceSample>
            m_ActionSourceSamples;
        readonly CharacterPoseFrameTransaction m_FrameTransaction;
        readonly Action m_EnterEvaluateBarrier;
        readonly AnimationPresentationRuntimeCapacityMetrics
            m_CapacityMetrics;
        CharacterPoseTuningRuntimeBinding m_TuningBinding;
        CharacterPoseTuningTargetIdentity m_TuningTarget;
        CharacterPoseFrameTransaction m_PendingTransaction;
        CharacterPoseProgramPrepared m_PendingPreparedPose;
        MotionMatchingFrameResolution m_PendingMotionMatchingResolution;
        AnimationPresentationDiagnosticsInterest m_PendingDiagnosticsInterest;
        AnimationPresentationDiagnosticsInterest m_PendingTraceInterest;
        CharacterLinkedPoseRuntimeSession m_PendingLinkedPose;
        RuntimeDiagnosticsContext m_PendingDiagnostics;
        string m_PendingFrameStage;
        bool m_PendingHasMotionMatchingResolution;
        bool m_PresentationActive;

        ulong m_TuningGeneration = 1;
        ulong m_NextFrameTransactionIdentity;
        AnimationPresentationFault m_Fault;
        AnimationPresentationFrameOutcome m_LastFrameOutcome;
        ulong m_DiscardCount;
        bool m_Faulted;
        bool m_Disposed;

        internal CharacterAnimationPresentationRuntime(
            ActorId actorId,
            Guid runtimeInstanceId,
            CharacterAnimationPresentationBindings bindings,
            CharacterMotionMatchingPresentationModule motionMatching,
            AnimancerComponent animancer,
            CharacterAnimationRigBinding rigBinding,
            CharacterRootHierarchyBinding rootHierarchy,
            CharacterFootPlacementModule footPlacement,
            bool ownsGraphClock,
            CharacterPoseWorkerScheduler workerScheduler,
            CharacterAnimationResourceScope resourceScope)
        {
            m_ActorId = actorId.IsValid
                ? actorId
                : throw new ArgumentException(
                    "Animation Presentation Actor identity is invalid.",
                    nameof(actorId));
            m_Bindings = bindings ??
                throw new ArgumentNullException(nameof(bindings));
            workerScheduler = workerScheduler ??
                throw new ArgumentNullException(nameof(workerScheduler));
            resourceScope = resourceScope ??
                throw new ArgumentNullException(nameof(resourceScope));
            var actionPlayback =
                new CharacterActionPlaybackRuntime(
                    bindings.ActionPlayback);
            var animationSlots =
                new AnimationSlotRuntime(
                    bindings.ActionPlayback);
            int frameCapacity = actionPlayback.FrameCapacity;
            int providerCapacity =
                bindings.Projection.MotionMatching?.NodeBindingCount ?? 0;
            int releaseCompletionCapacity =
                actionPlayback.BackendReleaseCompletionCapacity;
            int failureCapacity = Math.Max(
                1,
                checked(
                    providerCapacity +
                    bindings.Projection.PosePlan.StateMachines.Count));
            var presentationWorkspace =
                new PresentationFrameWorkspace(
                    providerCapacity,
                    frameCapacity,
                    releaseCompletionCapacity,
                    failureCapacity);
            m_ActionSourceSamples =
                new Dictionary<AnimationPlayerSourceSampleKey,
                    AnimationResolvedPoseSourceSample>(frameCapacity);
            m_FrameTransaction =
                new CharacterPoseFrameTransaction(frameCapacity);
            m_EnterEvaluateBarrier =
                m_FrameTransaction.EnterEvaluateBarrier;
            CharacterPoseRuntimeComposition poseModules = null;
            try
            {
                var diagnosticsEvents =
                    new CharacterPoseCommittedDiagnosticsEventPublisher(
                        runtimeInstanceId);
                poseModules = CharacterPoseRuntimeCompositionFactory.Create(
                    actorId,
                    workerScheduler,
                    animancer,
                    rigBinding,
                    rootHierarchy,
                    bindings.Projection,
                    resourceScope,
                    actionPlayback,
                    motionMatching,
                    animationSlots,
                    presentationWorkspace,
                    footPlacement,
                    ownsGraphClock,
                    1,
                    diagnosticsEvents);
                m_PoseModules = poseModules;
                m_PoseTuning = new CharacterPoseTuningCoordinator(
                    PoseProgram,
                    PoseSource,
                    PoseConstraints,
                    1);
                m_PoseFrame = new CharacterPoseFrameCoordinator(
                    animancer,
                    bindings.Projection,
                    m_PoseModules,
                    m_PoseTuning);
                m_PoseMotionMatching =
                    new CharacterPoseMotionMatchingCoordinator(
                        PoseProgram,
                        PoseSource,
                        m_PoseFrame);
                m_PresentationDiagnostics =
                    new CharacterAnimationPresentationDiagnosticsCoordinator(
                        m_ActorId,
                        m_PoseModules,
                        frameCapacity,
                        CalculateSourceSyncCapacity(
                            bindings.Projection.PosePlan));
                m_CapacityMetrics =
                    CreateCapacityMetrics(
                        actionPlayback.JournalCapacity,
                        PoseSource.ActionSamplingJournalCapacity,
                        Math.Max(1, animationSlots.SlotCount));
            }
            catch
            {
                motionMatching?.Dispose();
                poseModules?.Dispose();
                throw;
            }
        }

        CharacterPoseProgramRuntime PoseProgram => m_PoseModules.Program;
        CharacterPoseSourceModule PoseSource => m_PoseModules.Source;
        CharacterPoseConstraintRuntime PoseConstraints =>
            m_PoseModules.Constraints;
        CharacterFinalPosePublication PosePublication =>
            m_PoseModules.Publication;
        CharacterPoseDiagnosticsRuntime PoseDiagnostics =>
            m_PoseModules.Diagnostics;

        public IReadOnlyList<AnimationPlaybackId> RetiredPlaybacks =>
            m_PresentationDiagnostics.RetiredPlaybacks;
        public IReadOnlyList<ActionAnimationPlaybackLifecycleSnapshot>
            ActionSnapshots => m_PresentationDiagnostics.ActionSnapshots;
        public IReadOnlyList<ActionSlotSourceUsage> ActionSourceUsages =>
            PoseProgram.ActionSourceUsages;
        public bool HasRuntimeDiagnosticsSnapshot =>
            m_PresentationDiagnostics.HasRuntimeSnapshot;
        public AnimationPresentationRuntimeSnapshot
            RuntimeDiagnosticsSnapshot =>
                m_PresentationDiagnostics.RuntimeSnapshot;
        public bool HasDebugView => m_PresentationDiagnostics.HasDebugView;
        public AnimationPresentationDebugView DebugView =>
            m_PresentationDiagnostics.DebugView;
        public bool MotionMatchingRuntimeEnabled =>
            m_PoseMotionMatching.Enabled;
        public AnimationPresentationDiagnosticsInterest DiagnosticsInterest =>
            m_PresentationDiagnostics.Interest;
        internal bool HasFootPlacement => PoseConstraints.HasFootPlacement;

        internal void ResetFootPlacement(in CharacterFootPlacementReset reset) =>
            PoseConstraints.ResetFootPlacement(in reset);

        internal void RetargetFootPlacement(ulong resetSequence) =>
            PoseConstraints.RetargetFootPlacement(resetSequence);
        public AnimationPresentationRuntimeMetrics RuntimeMetrics =>
            new AnimationPresentationRuntimeMetrics(
                in m_CapacityMetrics,
                m_LastFrameOutcome,
                m_DiscardCount,
                m_Faulted ? m_Fault.Phase : default,
                m_PresentationDiagnostics.NoInterestSkipCount);
        public bool AcceptsMotionMatchingTrajectoryIntent =>
            m_PoseMotionMatching.AcceptsTrajectoryIntent;
        public bool IsFaulted => m_Faulted;
        public AnimationPresentationFault Fault =>
            m_Faulted
                ? m_Fault
                : throw new InvalidOperationException(
                    "Animation Presentation Runtime is not faulted.");

        public void SetPoseWatchInterests(
            Guid ownerId,
            IReadOnlyList<AnimationPoseWatchIdentity> interests) =>
            m_PresentationDiagnostics.SetPoseWatchInterests(
                ownerId,
                interests);

        public void RemovePoseWatchInterests(Guid ownerId) =>
            m_PresentationDiagnostics.RemovePoseWatchInterests(ownerId);

        public void SetDiagnosticsInterest(
            Guid ownerId,
            AnimationPresentationDiagnosticsInterest interest) =>
            m_PresentationDiagnostics.SetDiagnosticsInterest(
                ownerId,
                interest);

        public void RemoveDiagnosticsInterest(Guid ownerId) =>
            m_PresentationDiagnostics.RemoveDiagnosticsInterest(ownerId);

        internal void SetTuningBinding(CharacterPoseTuningRuntimeBinding binding)
        {
            if (binding == null)
                throw new ArgumentNullException(nameof(binding));
            m_TuningBinding = binding;
            m_TuningTarget = new CharacterPoseTuningTargetIdentity(
                m_ActorId.Value,
                m_Bindings.Projection.ProgramId,
                m_Bindings.Projection.ProjectionRevision,
                m_Bindings.Projection.PosePlan.PlanHash,
                m_Bindings.Projection.Rig.RigId,
                m_Bindings.Projection.Rig.RigRevision,
                m_Bindings.Projection.TuningLayout.LayoutHash);
        }

        internal CharacterPoseTuningRuntimeState TuningState =>
            m_TuningBinding?.State ?? default;

        internal CharacterPoseTuningLayout TuningLayout =>
            m_Bindings.Projection.TuningLayout;

        internal CharacterPoseTuningParameterBlock ActiveTuningBlock =>
            m_TuningBinding?.ActiveBlock;

        internal bool SubmitTuningCandidate(
            CharacterPoseTuningCandidate candidate,
            out string error)
        {
            if (m_TuningBinding == null)
            {
                error = "Pose tuning is unavailable for this presentation target.";
                return false;
            }
            return m_TuningBinding.SubmitPending(candidate, out error);
        }

        internal void ClearPendingTuningCandidate() =>
            m_TuningBinding?.ClearPending();

        public bool TryCaptureMotionMatchingSearchReplay(
            string providerId,
            out MotionMatchingSearchReplayArtifact artifact)
        {
            RequireAlive();
            return m_PoseMotionMatching.TryCaptureSearchReplay(
                providerId,
                out artifact);
        }

        public void CaptureMotionMatchingTrajectoryIntent(
            CharacterPresentationTrajectoryIntent intent)
        {
            RequireAlive();
            m_PoseMotionMatching.CaptureTrajectoryIntent(intent);
        }

        internal void CaptureMotionMatchingPreviewQuery(
            string providerId,
            MotionMatchingSearchReplayArtifact query)
        {
            RequireAlive();
            m_PoseMotionMatching.CapturePreviewQuery(
                providerId,
                query);
        }

        public void Publish(
            PresentationCommand command,
            CharacterPresentationProducerEntry producer) =>
            Publish(
                CharacterPresentationCommand.FromFloat32(command),
                producer);

        public void NotifyDomainEvent(CharacterPresentationCommand command)
        {
            RequireAlive();
            if (command.Kind != CharacterPresentationCommandKind.DomainEvent ||
                command.SourceActionInstanceId == 0)
            {
                throw new ArgumentException(
                    "Domain event notification requires a Domain event command with an Action instance.",
                    nameof(command));
            }
            m_PoseFrame.RequireNoOpenMutation();
            PoseProgram.NotifyActionDomainEvent(
                command.SourceActionInstanceId,
                command.Header.EventId);
        }

        public void Publish(
            CharacterPresentationCommand command,
            CharacterPresentationProducerEntry producer)
        {
            RequireAlive();
            ResolvedActionAnimationBinding binding =
                RequireActionBinding(command, producer);
            ActionAnimationPlaybackCommand actionCommand =
                ActionAnimationPlaybackCommandFactory.Create(
                    command,
                    in binding);
            m_PoseFrame.RequireNoOpenMutation();
            PoseProgram.PublishActionCommand(in actionCommand);
        }

        public void Retire(
            PresentationCommand command,
            CharacterPresentationProducerEntry producer) =>
            Retire(
                CharacterPresentationCommand.FromFloat32(command),
                producer);

        public void Retire(
            CharacterPresentationCommand command,
            CharacterPresentationProducerEntry producer)
        {
            RequireAlive();
            ResolvedActionAnimationBinding binding =
                RequireActionBinding(command, producer);
            ActionAnimationPlaybackCommand actionCommand =
                ActionAnimationPlaybackCommandFactory.Create(
                    command,
                    in binding);
            m_PoseFrame.RequireNoOpenMutation();
            PoseProgram.RetireActionCommand(in actionCommand);
        }

        public void Replace(
            CharacterPresentationCommand current,
            CharacterPresentationCommand replacement,
            CharacterPresentationProducerEntry currentProducer,
            CharacterPresentationProducerEntry replacementProducer)
        {
            RequireAlive();
            ResolvedActionAnimationBinding currentBinding =
                RequireActionBinding(
                    current,
                    currentProducer);
            ResolvedActionAnimationBinding replacementBinding =
                RequireActionBinding(
                    replacement,
                    replacementProducer);
            ActionAnimationPlaybackCommand currentCommand =
                ActionAnimationPlaybackCommandFactory.Create(
                    current,
                    in currentBinding);
            ActionAnimationPlaybackCommand replacementCommand =
                ActionAnimationPlaybackCommandFactory.Create(
                    replacement,
                    in replacementBinding);
            m_PoseFrame.RequireNoOpenMutation();
            PoseProgram.ReplaceActionCommand(
                currentCommand.EventId,
                in replacementCommand);
        }

        [PerformanceProbe("presentation.animation")]
        internal bool BeginPresentation(
            ulong presentationFrame,
            ulong latestSimulationTick,
            float interpolationAlpha,
            float presentationDeltaSeconds,
            in CharacterBodyPresentationFrame bodyFrame,
            in CharacterPresentationFactFrame factFrame,
            in CharacterPresentationProgramParameterFrame parameterFrame,
            CharacterLinkedPoseRuntimeSession linkedPose,
            RuntimeDiagnosticsContext diagnostics = null)
        {
            RequirePresentable();
            if (m_PresentationActive)
            {
                throw new InvalidOperationException(
                    "Animation Presentation already has an active frame.");
            }
            ApplyPendingTuning(presentationFrame);
            if (linkedPose == null)
                throw new ArgumentNullException(nameof(linkedPose));
            ValidateFrame(
                presentationFrame,
                latestSimulationTick,
                interpolationAlpha,
                presentationDeltaSeconds,
                in factFrame,
                in parameterFrame);
            AnimationPresentationDiagnosticsInterest traceInterest =
                AnimationPresentationTracePublisher.ResolveInterest(
                    diagnostics);
            AnimationPresentationDiagnosticsInterest diagnosticsInterest =
                m_PresentationDiagnostics.ResolveInterest(
                    traceInterest);
            bool publishStateDiagnostics =
                RequiresStateDiagnostics(diagnosticsInterest);
            m_PresentationDiagnostics.ClearWhenUnobserved(
                diagnosticsInterest);

            using (TransactionBeginMarker.Auto())
            {
                m_PendingTransaction = BeginFrameTransaction(
                    presentationFrame,
                    latestSimulationTick,
                    publishStateDiagnostics,
                    diagnosticsInterest,
                    linkedPose);
            }
            m_PendingDiagnosticsInterest = diagnosticsInterest;
            m_PendingTraceInterest = traceInterest;
            m_PendingLinkedPose = linkedPose;
            m_PendingDiagnostics = diagnostics;
            m_PendingFrameStage = "ActionLifecycle";
            m_PresentationActive = true;
            try
            {
                IReadOnlyList<ActionAnimationPlaybackLifecycleFrame>
                    lifecycle;
                using (ActionLifecycleMarker.Auto())
                {
                    lifecycle =
                        PoseProgram.PrepareActionLifecycleFrame(
                            m_PendingTransaction.PoseLease);
                }

                double presentationSampleTick =
                    bodyFrame.PreviousTick +
                    (bodyFrame.CurrentTick -
                     bodyFrame.PreviousTick) *
                     (double)bodyFrame.SampleAlpha;
                using (ActionSamplingMarker.Auto())
                {
                    m_PendingFrameStage = "ActionSampling";
                    PoseProgram.ProjectActionPresentationSamples(
                        m_PendingTransaction.PoseLease,
                        lifecycle,
                        presentationSampleTick,
                        presentationDeltaSeconds);
                    PoseProgram.ResolveActionPresentationFrames(
                        m_PendingTransaction.PoseLease);
                }

                using (PoseRoutingMarker.Auto())
                {
                    m_PendingFrameStage = "PoseAdvance";
                    m_PoseFrame.Advance(
                        presentationDeltaSeconds,
                        in factFrame,
                        in parameterFrame);
                }
                MotionMatchingFrameResolution motionMatchingResolution;
                bool hasMotionMatchingResolution;
                using (MotionMatchingMarker.Auto())
                {
                    m_PendingFrameStage = "MotionMatching";
                    motionMatchingResolution = ResolveMotionMatching(
                        presentationFrame,
                        presentationDeltaSeconds,
                        in bodyFrame,
                        out hasMotionMatchingResolution);
                }
                using (PoseRoutingMarker.Auto())
                {
                    m_PendingFrameStage = "PoseFinalize";
                    m_PoseFrame.FinalizePoseState(
                        in factFrame);
                    PoseProgram.PublishActionSources(
                        m_PendingTransaction.PoseLease,
                        m_ActionSourceSamples);
                }
                CharacterPoseSourceDemand sourceDemand =
                    m_PoseFrame.CreateSourceDemand(
                        m_PendingTransaction.PoseLease,
                        m_PendingTransaction.SourceLease,
                        m_ActionSourceSamples.Count,
                        PoseProgram.ProviderSourceSampleCount);
                m_PendingPreparedPose =
                    m_PoseFrame.PrepareEvaluation(
                        m_PendingTransaction.SourceLease,
                        m_PendingTransaction.PublicationLease,
                        in sourceDemand,
                        presentationDeltaSeconds,
                        m_ActionSourceSamples,
                        diagnosticsInterest !=
                            AnimationPresentationDiagnosticsInterest.None);
                if (!m_PendingPreparedPose.IsReady)
                {
                    if (!m_PendingPreparedPose.IsValid ||
                        m_PendingPreparedPose.Outcome !=
                            CharacterPoseSourceFrameOutcome.AwaitingSample)
                    {
                        throw new InvalidOperationException(
                            $"Animation Presentation source preparation is invalid: outcome={m_PendingPreparedPose.Outcome}, failure={m_PendingPreparedPose.SourceFrame.FailureReason}.");
                    }
                    Exception pendingCleanup = CleanupPendingPresentation();
                    if (pendingCleanup != null)
                        throw new AggregateException(
                            "Animation Presentation pending source cleanup failed.",
                            pendingCleanup);
                    return false;
                }
                CharacterPoseSourceFrameResult sourceFrame =
                    m_PendingPreparedPose.SourceFrame;
                m_PendingTransaction.BindSourceResults(
                    in sourceDemand,
                    in sourceFrame);
                if (hasMotionMatchingResolution)
                {
                    m_PoseMotionMatching.PrepareCompletion(
                        in motionMatchingResolution,
                        m_PendingPreparedPose.Lineage.CompletionIdentity);
                }
                m_PendingMotionMatchingResolution =
                    motionMatchingResolution;
                m_PendingHasMotionMatchingResolution =
                    hasMotionMatchingResolution;
                using (ReleaseProtocolMarker.Auto())
                {
                    m_PendingFrameStage = "ReleaseProtocol";
                    PoseProgram.CompleteActionReleaseProtocol(
                        m_PendingTransaction.PoseLease);
                    PoseProgram.ValidateActionSamplingFrame(
                        m_PendingTransaction.PoseLease);
                    PoseProgram.ValidateActionFrame(
                        m_PendingTransaction.PoseLease);
                    m_PoseFrame.ValidatePendingSeal(
                        m_PendingTransaction.PoseLease,
                        m_PendingTransaction.SourceLease);
                    m_PendingTransaction.MarkValidated();
                }

                m_PendingFrameStage = "EvaluateBarrier";
                m_PoseFrame.BeginEvaluateBarrier(
                    in bodyFrame,
                    in factFrame,
                    m_PendingTransaction.SourceLease,
                    in m_PendingPreparedPose,
                    m_EnterEvaluateBarrier);
                return true;
            }
            catch (Exception frameFailure)
            {
                throw ClosePendingPresentationFailure(frameFailure);
            }
        }

        internal bool TryGetCommittedPose(
            out ComposedAnimationPoseFrame frame) =>
            m_PoseModules.Publication.TryGetCommittedFrame(out frame);

        [PerformanceProbe("presentation.animation")]
        internal bool TryAdvancePresentation(
            out CharacterPoseWorkerStageLease workerLease)
        {
            if (!m_PresentationActive)
            {
                throw new InvalidOperationException(
                    "Animation Presentation has no active frame.");
            }
            try
            {
                m_PendingFrameStage = "EvaluateBarrier";
                return m_PoseFrame.TryAdvanceEvaluateBarrier(
                    out workerLease);
            }
            catch (Exception frameFailure)
            {
                throw ClosePendingPresentationFailure(frameFailure);
            }
        }

        [PerformanceProbe("presentation.animation")]
        internal ComposedAnimationPoseFrame CompletePresentation()
        {
            if (!m_PresentationActive)
            {
                throw new InvalidOperationException(
                    "Animation Presentation has no active frame.");
            }
            try
            {
                CharacterPoseFrameExecutionResult executionResult =
                    m_PoseFrame.CompleteEvaluateBarrier(
                        m_PendingTransaction.ConstraintLease,
                        m_PendingTransaction.PublicationLease,
                        in m_PendingPreparedPose);
                m_PendingTransaction.BindExecutionResults(in executionResult);
                CharacterPoseProgramResult programResult =
                    executionResult.Program;
                CharacterPoseConstraintResult constraintResult =
                    executionResult.Constraint;
                AnimationPresentationFrameOutcome poseOutcome =
                    executionResult.Publication.Outcome;
                if (!executionResult.IsPublished)
                {
                    int invalidOperation =
                        programResult.InvalidOperationIndex;
                    CharacterFullBodyIkResult fullBodyIkResult =
                        constraintResult.FullBodyIk;
                    string solverFailure =
                        constraintResult.SolverProduced &&
                        !constraintResult.FullBodyIk.Succeeded
                            ? $", solverFailure={fullBodyIkResult.Failure}, " +
                              $"failedGoalSet={fullBodyIkResult.FailedGoalSetIndex}, " +
                              $"failedSlot={fullBodyIkResult.FailedSlot}, " +
                              $"appliedGoals={fullBodyIkResult.AppliedGoalCount}" +
                              FormatFullBodyIkFailure(fullBodyIkResult)
                            : string.Empty;
                    throw new InvalidOperationException(
                        $"Animation Presentation frame produced '{poseOutcome}' after the Evaluate Barrier: " +
                        $"availability={programResult.OutputAvailability}, " +
                        $"outputReason={programResult.OutputInvalidReason}, " +
                        $"graphReason={programResult.GraphInvalidReason}, " +
                        $"operation={invalidOperation}, " +
                        $"completion={m_PendingPreparedPose.Lineage.CompletionIdentity}{solverFailure}.");
                }
                ComposedAnimationPoseFrame composedPose;
                using (FrameCommitMarker.Auto())
                {
                    m_PendingFrameStage = "FrameCommit";
                    if (m_PendingHasMotionMatchingResolution)
                    {
                        m_PoseMotionMatching.CompleteFrame();
                    }
                    CommitFrameTransaction(
                        m_PendingTransaction,
                        m_PendingLinkedPose);
                    CharacterPoseSourceFrameResult committedSourceFrame =
                        m_PendingTransaction.SourceFrame;
                    composedPose =
                        m_PoseFrame.FinalizeCommitted(
                            m_PendingTransaction.PublicationLease,
                            in committedSourceFrame);
                }

                using (PostCommitMarker.Auto())
                {
                    m_PendingFrameStage = "PostCommit";
                    PoseSource.ApplyActionBackendReleaseAcknowledgements();
                    PoseProgram.ExecutePreparedActionBackendReleaseRequests();
                    m_PresentationDiagnostics.PublishCommittedFrame(
                        m_PendingTransaction,
                        m_PendingLinkedPose,
                        in executionResult,
                        m_PendingDiagnosticsInterest,
                        RequiresStateDiagnostics(m_PendingDiagnosticsInterest),
                        m_PendingTraceInterest,
                        m_PendingDiagnostics);
                    if (m_PendingHasMotionMatchingResolution)
                    {
                        m_PoseMotionMatching
                            .PublishCommittedFrameDiagnostics(
                                m_PendingDiagnostics,
                                in m_PendingMotionMatchingResolution);
                    }
                }
                m_LastFrameOutcome =
                    AnimationPresentationFrameOutcome.Committed;
                ClearPendingPresentation();
                return composedPose;
            }
            catch (Exception frameFailure)
            {
                throw ClosePendingPresentationFailure(frameFailure);
            }
        }

        internal Exception AbortPresentation()
        {
            return m_PresentationActive
                ? CleanupPendingPresentation()
                : null;
        }

        Exception ClosePendingPresentationFailure(Exception frameFailure)
        {
            string frameStage = m_PendingFrameStage;
            Exception cleanupFailure = CleanupPendingPresentation();
            Exception failure = cleanupFailure == null
                ? frameFailure
                : new AggregateException(
                    "Animation Presentation frame and cleanup both failed.",
                    frameFailure,
                    cleanupFailure);
            return new InvalidOperationException(
                $"Animation Presentation failed during '{frameStage}'.",
                failure);
        }

        Exception CleanupPendingPresentation()
        {
            CharacterPoseFrameTransaction transaction =
                m_PendingTransaction;
            CharacterLinkedPoseRuntimeSession linkedPose =
                m_PendingLinkedPose;
            Exception cleanupFailure = null;
            if (transaction != null && !transaction.Closed)
            {
                if (transaction.Phase <
                    AnimationPresentationFramePhase.EvaluateBarrier)
                {
                    cleanupFailure = CleanupFrameTransaction(
                        transaction,
                        linkedPose,
                        false);
                }
                else
                {
                    cleanupFailure = CleanupFrameTransaction(
                        transaction,
                        linkedPose,
                        true);
                }
            }
            ClearPendingPresentation();
            return cleanupFailure;
        }

        void ClearPendingPresentation()
        {
            m_PendingTransaction = null;
            m_PendingPreparedPose = default;
            m_PendingMotionMatchingResolution = default;
            m_PendingDiagnosticsInterest = default;
            m_PendingTraceInterest = default;
            m_PendingLinkedPose = null;
            m_PendingDiagnostics = null;
            m_PendingFrameStage = null;
            m_PendingHasMotionMatchingResolution = false;
            m_PresentationActive = false;
        }

        static string FormatFullBodyIkFailure(CharacterFullBodyIkResult result)
        {
            if (result.Failure == CharacterFullBodyIkFailure.FootEffectorSolverResidualExceeded)
            {
                UnityEngine.Vector3 target = result.FailedTargetPosition;
                UnityEngine.Vector3 solver = result.FailedSolverPosition;
                UnityEngine.Vector3 solved = result.FailedSolvedPosition;
                return $", sourceKind={result.FailedSourceKind}, " +
                       $"targetComponent=({target.x:R},{target.y:R},{target.z:R}), " +
                       $"solverNodeComponent=({solver.x:R},{solver.y:R},{solver.z:R}), " +
                       $"solvedComponent=({solved.x:R},{solved.y:R},{solved.z:R}), " +
                       $"solverResidual={result.FailedSolverResidual:R}, " +
                       $"positionResidual={result.FailedPositionResidual:R}";
            }
            return string.IsNullOrEmpty(result.FailureDetail)
                ? string.Empty
                : $", solverDetail={result.FailureDetail}";
        }

        void ApplyPendingTuning(
            ulong presentationFrame)
        {
            if (m_TuningBinding is null)
                return;
            bool applied = m_TuningBinding.TryApplyPending(
                m_TuningTarget,
                presentationFrame,
                activation: PoseProgram.CanApplyNextActivation,
                (block, resetOwnerState) =>
                    m_PoseTuning.Apply(
                        m_Bindings.Projection.TuningLayout,
                        block,
                        checked(m_TuningGeneration + 1),
                        resetOwnerState),
                out _);
            if (applied)
                m_TuningGeneration++;
        }

        void ResetPoseModules(PoseDiscontinuityResetReason reason)
        {
            m_PoseFrame.RequireNoOpenMutation();
            if (reason == PoseDiscontinuityResetReason.None)
                throw new ArgumentOutOfRangeException(nameof(reason));
            PosePublication.ResetToDefaults();
            PoseDiagnostics.Reset();
            PoseSource.CancelReleaseDiagnostics();
            PoseSource.ClearActionSlotReleaseCompletions();
            PoseProgram.ClearSourceRetirements();
            PoseSource.ClearValidatedReleaseAcknowledgements();
            PoseProgram.ClearMotionMatchingPoseCompletion();
            PoseSource.ClearActionBackendReleaseCompletions();
            PoseProgram.BeginReset();
            m_PoseFrame.ResetState();
            PoseConstraints.ResetSolvers();
            ulong completionIdentity =
                m_PoseFrame.NextCompletionIdentity();
            PoseProgram.ResetBlendState(completionIdentity);
            PoseProgram.ReleaseCompletedSources(completionIdentity);
            PoseProgram.ResetPoseState(reason);
            PoseProgram.ReleasePlayerSources();
            PoseSource.Clear();
        }

        public void Reset()
        {
            Reset(
                PoseDiscontinuityResetReason.PresentationReset);
        }

        internal void Reset(PoseDiscontinuityResetReason reason)
        {
            if (m_Disposed)
                return;
            ResetPoseModules(reason);
            PoseProgram.ResetAnimationSlots();
            PoseProgram.ResetActionSampling();
            PoseProgram.ResetActionPlayback();
            PoseProgram.ResetPresentationWorkspace();
            m_PoseMotionMatching.Reset(
                0,
                MotionMatchingPresentationResetReason
                    .PresentationReset);
            ClearPublishedState();
        }

        internal void ResetPoseBranch(ulong resetSequence)
        {
            RequireAlive();
            ResetPoseModules(
                PoseDiscontinuityResetReason.BranchReplacement);
            PoseProgram.ResetAnimationSlots();
            PoseProgram.ResetActionSampling();
            PoseProgram.ResetPresentationWorkspace();
            m_PoseMotionMatching.Reset(
                resetSequence,
                MotionMatchingPresentationResetReason
                    .BodyStreamReset);
            ClearPublishedState();
        }

        internal void RetargetBodyBranch(ulong resetSequence)
        {
            RequireAlive();
            if (resetSequence == 0)
                throw new ArgumentOutOfRangeException(
                    nameof(resetSequence));
            m_PoseMotionMatching.Reset(
                resetSequence,
                MotionMatchingPresentationResetReason
                    .BodyStreamReset);
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            Exception failure = null;
            try
            {
                m_PoseFrame.ResetState();
                m_PoseModules.Dispose();
            }
            catch (Exception exception)
            {
                failure = exception;
            }
            ClearPublishedState();
            if (failure != null)
                throw failure;
        }

        CharacterPoseFrameTransaction BeginFrameTransaction(
            ulong presentationFrame,
            ulong bodyTick,
            bool captureDiagnostics,
            AnimationPresentationDiagnosticsInterest diagnosticsInterest,
            CharacterLinkedPoseRuntimeSession linkedPose)
        {
            CharacterPoseProgramFrameLease pose = default;
            CharacterPoseSourceFrameLease source = default;
            CharacterPoseConstraintFrameLease constraint = default;
            CharacterFinalPosePublicationFrameLease publication = default;
            m_NextFrameTransactionIdentity++;
            if (m_NextFrameTransactionIdentity == 0)
                m_NextFrameTransactionIdentity++;
            ulong frameIdentity = m_NextFrameTransactionIdentity;
            var lineage = new CharacterPoseFrameLineage(
                m_ActorId,
                frameIdentity,
                0,
                presentationFrame,
                bodyTick,
                m_Bindings.Projection.ProgramId,
                m_Bindings.Projection.PosePlan.PoseGraphId,
                m_Bindings.Projection.PosePlan.ContentRevision,
                m_Bindings.Projection.PosePlan.PlanHash,
                m_Bindings.Projection.ProjectionRevision,
                m_Bindings.Projection.Rig.RigId,
                m_Bindings.Projection.Rig.RigRevision,
                m_TuningGeneration);
            bool linkedPosePrepared = false;
            bool actionFrameOpen = false;
            bool actionSamplingFrameOpen = false;
            bool animationSlotFrameOpen = false;
            bool motionMatchingFrameOpen = false;
            try
            {
                linkedPose.Prepare();
                linkedPosePrepared = true;
                PoseProgram.BeginActionPlaybackFrame(
                    frameIdentity,
                    presentationFrame);
                actionFrameOpen = true;
                PoseProgram.BeginActionSamplingFrame(
                    frameIdentity,
                    presentationFrame,
                    captureDiagnostics);
                actionSamplingFrameOpen = true;
                PoseProgram.BeginAnimationSlotFrame(frameIdentity);
                animationSlotFrameOpen = true;
                motionMatchingFrameOpen =
                    m_PoseMotionMatching.BeginFrame(frameIdentity);
                bool captureFootIkDiagnostics =
                    PoseDiagnostics.HasFootCaptureInterest;
                pose = m_PoseFrame.Begin(
                    in lineage,
                    diagnosticsInterest,
                    captureFootIkDiagnostics,
                    linkedPose,
                    out source,
                    out constraint,
                    out publication);
                m_FrameTransaction.Begin(
                    in lineage,
                    pose,
                    source,
                    constraint,
                    publication,
                    captureFootIkDiagnostics);
                m_FrameTransaction.BeginPrepare();
                return m_FrameTransaction;
            }
            catch (Exception beginFailure)
            {
                Exception discardFailure = null;
                if (pose.IsValid)
                {
                    DiscardStep(
                        () => m_PoseFrame.Discard(
                            pose,
                            source,
                            constraint,
                            publication),
                        ref discardFailure);
                }
                if (motionMatchingFrameOpen)
                {
                    DiscardStep(
                        () => m_PoseMotionMatching
                            .DiscardFrame(frameIdentity),
                        ref discardFailure);
                }
                if (animationSlotFrameOpen)
                {
                    DiscardStep(
                        () => PoseProgram
                            .DiscardAnimationSlotFrame(frameIdentity),
                        ref discardFailure);
                }
                if (actionSamplingFrameOpen)
                {
                    DiscardStep(
                        () => PoseProgram
                            .DiscardActionSamplingFrame(frameIdentity),
                        ref discardFailure);
                }
                if (actionFrameOpen)
                {
                    DiscardStep(
                        () => PoseProgram
                            .DiscardActionPlaybackFrame(frameIdentity),
                        ref discardFailure);
                    DiscardStep(
                        () => PoseProgram
                            .DiscardPresentationWorkspaceFrame(
                                frameIdentity),
                        ref discardFailure);
                }
                if (linkedPosePrepared)
                {
                    DiscardStep(
                        linkedPose.Discard,
                        ref discardFailure);
                }
                m_LastFrameOutcome =
                    AnimationPresentationFrameOutcome.None;
                if (m_DiscardCount != ulong.MaxValue)
                    m_DiscardCount++;
                if (discardFailure != null)
                {
                    throw new AggregateException(
                        "Animation Presentation frame begin and Pending discard both failed.",
                        beginFailure,
                        discardFailure);
                }
                throw;
            }
        }

        MotionMatchingFrameResolution ResolveMotionMatching(
            ulong presentationFrame,
            float presentationDeltaSeconds,
            in CharacterBodyPresentationFrame bodyFrame,
            out bool hasResolution)
        {
            m_ActionSourceSamples.Clear();
            return m_PoseMotionMatching.Resolve(
                presentationFrame,
                presentationDeltaSeconds,
                in bodyFrame,
                out hasResolution);
        }

        static bool RequiresStateDiagnostics(
            AnimationPresentationDiagnosticsInterest interest) =>
            (interest &
             (AnimationPresentationDiagnosticsInterest.LiveState |
              AnimationPresentationDiagnosticsInterest.Capture)) != 0;

        void CommitFrameTransaction(
            CharacterPoseFrameTransaction transaction,
            CharacterLinkedPoseRuntimeSession linkedPose)
        {
            if (transaction == null ||
                !transaction.IsValid)
            {
                throw new InvalidOperationException(
                    "Animation Presentation frame transaction cannot commit.");
            }
            m_PoseFrame.RequireMutation(transaction.PoseLease);
            PoseProgram.CommitPresentationWorkspaceFrame(
                transaction.PoseLease);
            m_PoseFrame.RequireMutation(transaction.PoseLease);
            PoseProgram.CommitActionSamplingFrame(
                transaction.PoseLease);
            m_PoseFrame.RequireMutation(transaction.PoseLease);
            PoseProgram.CommitAnimationSlotFrame(
                transaction.PoseLease);
            m_PoseFrame.RequireMutation(transaction.PoseLease);
            PoseProgram.CommitActionPlaybackFrame(
                transaction.PoseLease);
            m_PoseFrame.RequireMutation(transaction.PoseLease);
            m_PoseMotionMatching.CommitFrame(
                transaction.PoseLease);
            m_PoseFrame.Seal(
                transaction.PoseLease,
                transaction.SourceLease,
                transaction.ConstraintLease,
                transaction.PublicationLease);
            linkedPose.Seal();
            transaction.MarkSealed();
        }

        Exception CleanupFrameTransaction(
            CharacterPoseFrameTransaction transaction,
            CharacterLinkedPoseRuntimeSession linkedPose,
            bool faulted)
        {
            if (transaction == null ||
                transaction.Closed)
            {
                return null;
            }
            Exception failure = null;
            DiscardStep(
                () => m_PoseFrame.Discard(
                    transaction.PoseLease,
                    transaction.SourceLease,
                    transaction.ConstraintLease,
                    transaction.PublicationLease),
                ref failure);
            DiscardStep(
                () => m_PoseMotionMatching.DiscardFrame(
                    transaction.Lineage.FrameIdentity),
                ref failure);
            DiscardStep(
                () => PoseProgram.DiscardAnimationSlotFrame(
                    transaction.Lineage.FrameIdentity),
                ref failure);
            DiscardStep(
                () => PoseProgram.DiscardActionSamplingFrame(
                    transaction.Lineage.FrameIdentity),
                ref failure);
            DiscardStep(
                () => PoseProgram.DiscardActionPlaybackFrame(
                    transaction.Lineage.FrameIdentity),
                ref failure);
            DiscardStep(
                () => PoseProgram.DiscardPresentationWorkspaceFrame(
                    transaction.Lineage.FrameIdentity),
                ref failure);
            DiscardStep(
                linkedPose.Discard,
                ref failure);
            if (faulted)
            {
                DiscardStep(
                    () => MarkFaulted(transaction),
                    ref failure);
            }
            else
            {
                DiscardStep(
                    transaction.MarkDiscarded,
                    ref failure);
                m_LastFrameOutcome =
                    AnimationPresentationFrameOutcome.None;
                if (m_DiscardCount != ulong.MaxValue)
                    m_DiscardCount++;
            }
            return failure;
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

        ResolvedActionAnimationBinding RequireActionBinding(
            CharacterPresentationCommand command,
            CharacterPresentationProducerEntry producer)
        {
            if (producer == null ||
                producer.Kind !=
                    CharacterPresentationProducerKind.Animation ||
                !string.Equals(
                    producer.ProgramProducerIdentity,
                    command.ProducerId,
                    StringComparison.Ordinal) ||
                !m_Bindings.ActionPlayback.TryGet(
                    producer.ProducerId,
                    out ResolvedActionAnimationBinding binding) ||
                !string.Equals(
                    binding.ProgramProducerId,
                    producer.ProgramProducerIdentity,
                    StringComparison.Ordinal) ||
                binding.AnimationChannelId !=
                    producer.AnimationChannelId)
            {
                throw new InvalidOperationException(
                    $"Presentation command targets non-Action animation producer '{command.ProducerId}'.");
            }
            return binding;
        }

        void ClearPublishedState()
        {
            m_PresentationDiagnostics.ClearPublishedState();
            m_ActionSourceSamples.Clear();
        }

        void RequireAlive()
        {
            if (m_Disposed)
            {
                throw new ObjectDisposedException(
                    nameof(CharacterAnimationPresentationRuntime));
            }
        }

        void RequirePresentable()
        {
            RequireAlive();
            if (m_Faulted)
            {
                throw new InvalidOperationException(
                    $"Animation Presentation Runtime for Actor '{m_ActorId}' is faulted at frame {m_Fault.PresentationFrame}, phase {m_Fault.Phase}.");
            }
        }

        void MarkFaulted(
            CharacterPoseFrameTransaction transaction)
        {
            if (m_Faulted)
                return;
            AnimationPresentationFramePhase phase =
                transaction.Phase;
            m_Fault = new AnimationPresentationFault(
                m_ActorId,
                transaction.Lineage.PresentationFrame,
                transaction.Lineage.BodyTick,
                phase,
                m_PoseFrame.FrameCompletionContext);
            m_Faulted = true;
            m_LastFrameOutcome =
                AnimationPresentationFrameOutcome.Faulted;
            transaction.MarkFaulted();
        }

        static void ValidateFrame(
            ulong presentationFrame,
            ulong latestSimulationTick,
            float interpolationAlpha,
            float presentationDeltaSeconds,
            in CharacterPresentationFactFrame factFrame,
            in CharacterPresentationProgramParameterFrame parameterFrame)
        {
            if (presentationFrame == 0 ||
                !float.IsFinite(interpolationAlpha) ||
                !float.IsFinite(presentationDeltaSeconds) ||
                presentationDeltaSeconds < 0f ||
                !factFrame.IsValid ||
                !parameterFrame.IsValid ||
                factFrame.Identity.RenderFrame != presentationFrame ||
                factFrame.SimulationTick.Value !=
                    latestSimulationTick)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(presentationDeltaSeconds));
            }
        }

        AnimationPresentationRuntimeCapacityMetrics CreateCapacityMetrics(
            int actionJournalCapacity,
            int samplingJournalCapacity,
            int slotJournalCapacity) =>
            new AnimationPresentationRuntimeCapacityMetrics(
                PoseProgram.DenseDoublePageResidentPayloadBytes,
                PoseInertializationNativeProgramPayloadMetrics
                    .CalculateDoublePageResidentPayloadBytes(
                        PoseProgram.Inertialization),
                PosePublication.DenseDoublePageResidentPayloadBytes,
                actionJournalCapacity,
                samplingJournalCapacity,
                slotJournalCapacity,
                PoseProgram.SourceRetirementStandaloneCapacity,
                PoseSource.Capacity,
                PoseProgram.SourceRetirementStandaloneCapacity);

        static int CalculateSourceSyncCapacity(
            CharacterPoseProgramImage plan)
        {
            int capacity = 0;
            for (int i = 0; i < plan.StateMachines.Count; i++)
            {
                capacity = checked(
                    capacity +
                    plan.StateMachines[i].Transitions.Count);
            }
            return capacity;
        }
    }
}
