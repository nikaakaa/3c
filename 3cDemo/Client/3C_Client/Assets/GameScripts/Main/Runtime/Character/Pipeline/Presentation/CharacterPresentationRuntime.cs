using System;
using System.Collections.Generic;
using Animancer;
using BTSMTL.Diagnostics;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Pipeline.Animation.Lifecycle;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;
using ThirdPersonCharacter.Pipeline.Animation.Presentation;
using ThirdPersonSimulation;
using Unity.Profiling;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    public sealed class CharacterAnimationPresentationRuntime : IDisposable
    {
        static readonly ProfilerMarker ActionLifecycleMarker =
            new ProfilerMarker("ThirdPerson.Presentation.Animation.ActionLifecycle");
        static readonly ProfilerMarker TransactionBeginMarker =
            new ProfilerMarker("ThirdPerson.Presentation.Animation.TransactionBegin");
        static readonly ProfilerMarker ActionSamplingMarker =
            new ProfilerMarker("ThirdPerson.Presentation.Animation.ActionSampling");
        static readonly ProfilerMarker PoseRoutingMarker =
            new ProfilerMarker("ThirdPerson.Presentation.Animation.PoseRouting");
        static readonly ProfilerMarker MotionMatchingMarker =
            new ProfilerMarker("ThirdPerson.Presentation.Animation.MotionMatching");
        static readonly ProfilerMarker ReleaseProtocolMarker =
            new ProfilerMarker("ThirdPerson.Presentation.Animation.ReleaseProtocol");
        static readonly ProfilerMarker FrameCommitMarker =
            new ProfilerMarker("ThirdPerson.Presentation.Animation.FrameCommit");
        static readonly ProfilerMarker PostCommitMarker =
            new ProfilerMarker("ThirdPerson.Presentation.Animation.PostCommit");

        readonly ActorId m_ActorId;
        readonly CharacterAnimationPresentationBindings m_Bindings;
        readonly PosePlanExecutionRuntime m_PoseRuntime;
        readonly List<ActionAnimationPlaybackLifecycleSnapshot>
            m_ActionSnapshots;
        readonly List<ActionPresentationTimeSnapshot>
            m_ActionTimeSnapshots;
        readonly List<PoseStateSourceSyncSnapshot>
            m_PoseStateSourceSyncSnapshots;
        readonly List<AnimationPlaybackId> m_RetiredPlaybacks;
        readonly Dictionary<AnimationPlayerSourceSampleKey,
            AnimationResolvedPoseSourceSample>
            m_ActionSourceSamples;
        readonly CharacterPoseFrameTransaction m_FrameTransaction;
        readonly Action m_EnterEvaluateBarrier;
        readonly AnimationPresentationRuntimeCapacityMetrics
            m_CapacityMetrics;
        CharacterPoseTuningRuntimeBinding m_TuningBinding;
        CharacterPoseTuningTargetIdentity m_TuningTarget;

        ulong m_TuningGeneration = 1;
        ulong m_NextFrameTransactionIdentity;
        AnimationPresentationDebugView m_DebugView;
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
            bool ownsGraphClock)
        {
            m_ActorId = actorId.IsValid
                ? actorId
                : throw new ArgumentException(
                    "Animation Presentation Actor identity is invalid.",
                    nameof(actorId));
            m_Bindings = bindings ??
                throw new ArgumentNullException(nameof(bindings));
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
            m_ActionSnapshots =
                new List<ActionAnimationPlaybackLifecycleSnapshot>(
                    frameCapacity);
            m_ActionTimeSnapshots =
                new List<ActionPresentationTimeSnapshot>(frameCapacity);
            m_PoseStateSourceSyncSnapshots =
                new List<PoseStateSourceSyncSnapshot>(
                    CalculateSourceSyncCapacity(
                        bindings.Projection.PosePlan));
            m_RetiredPlaybacks =
                new List<AnimationPlaybackId>(frameCapacity);
            m_ActionSourceSamples =
                new Dictionary<AnimationPlayerSourceSampleKey,
                    AnimationResolvedPoseSourceSample>(frameCapacity);
            m_FrameTransaction =
                new CharacterPoseFrameTransaction(frameCapacity);
            m_EnterEvaluateBarrier =
                m_FrameTransaction.EnterEvaluateBarrier;
            try
            {
                m_PoseRuntime =
                    new PosePlanExecutionRuntime(
                        runtimeInstanceId,
                        animancer,
                        rigBinding,
                        rootHierarchy,
                        bindings.Projection,
                        actionPlayback,
                        motionMatching,
                        animationSlots,
                        presentationWorkspace,
                        footPlacement,
                        ownsGraphClock);
                m_CapacityMetrics =
                    m_PoseRuntime.CreateCapacityMetrics(
                        actionPlayback.JournalCapacity,
                        m_PoseRuntime.ActionSamplingJournalCapacity,
                        Math.Max(1, animationSlots.SlotCount));
            }
            catch
            {
                motionMatching?.Dispose();
                m_PoseRuntime?.Dispose();
                throw;
            }
        }

        public IReadOnlyList<AnimationPlaybackId> RetiredPlaybacks =>
            m_RetiredPlaybacks;
        public IReadOnlyList<ActionAnimationPlaybackLifecycleSnapshot>
            ActionSnapshots => m_ActionSnapshots;
        public IReadOnlyList<ActionSlotSourceUsage> ActionSourceUsages =>
            m_PoseRuntime.ActionSourceUsages;
        public bool HasRuntimeDiagnosticsSnapshot =>
            m_PoseRuntime.HasDiagnosticsSnapshot;
        public AnimationPresentationRuntimeSnapshot
            RuntimeDiagnosticsSnapshot =>
                m_PoseRuntime.DiagnosticsSnapshot;
        public bool HasDebugView =>
            m_DebugView != null &&
            m_PoseRuntime.HasDiagnosticsSnapshot;
        public AnimationPresentationDebugView DebugView =>
            HasDebugView
                ? m_DebugView
                : throw new InvalidOperationException(
                    "Animation Presentation Debug View is unavailable.");
        public bool MotionMatchingRuntimeEnabled =>
            m_PoseRuntime.MotionMatchingRuntimeEnabled;
        public AnimationPresentationDiagnosticsInterest DiagnosticsInterest =>
            m_PoseRuntime.DiagnosticsInterest;
        internal bool HasFootPlacement => m_PoseRuntime.HasFootPlacement;

        internal void ResetFootPlacement(in CharacterFootPlacementReset reset) =>
            m_PoseRuntime.ResetFootPlacement(in reset);

        internal void RetargetFootPlacement(ulong resetSequence) =>
            m_PoseRuntime.RetargetFootPlacement(resetSequence);
        public AnimationPresentationRuntimeMetrics RuntimeMetrics =>
            new AnimationPresentationRuntimeMetrics(
                in m_CapacityMetrics,
                m_LastFrameOutcome,
                m_DiscardCount,
                m_Faulted ? m_Fault.Phase : default,
                m_PoseRuntime.DiagnosticsNoInterestSkipCount);
        public bool AcceptsMotionMatchingTrajectoryIntent =>
            m_PoseRuntime.AcceptsMotionMatchingTrajectoryIntent;
        public bool IsFaulted => m_Faulted;
        public AnimationPresentationFault Fault =>
            m_Faulted
                ? m_Fault
                : throw new InvalidOperationException(
                    "Animation Presentation Runtime is not faulted.");

        public void SetPoseWatchInterests(
            Guid ownerId,
            IReadOnlyList<AnimationPoseWatchIdentity> interests) =>
            m_PoseRuntime.SetPoseWatchInterests(
                ownerId,
                interests);

        public void RemovePoseWatchInterests(Guid ownerId) =>
            m_PoseRuntime.RemovePoseWatchInterests(ownerId);

        public void SetDiagnosticsInterest(
            Guid ownerId,
            AnimationPresentationDiagnosticsInterest interest) =>
            m_PoseRuntime.SetDiagnosticsInterest(ownerId, interest);

        public void RemoveDiagnosticsInterest(Guid ownerId) =>
            m_PoseRuntime.RemoveDiagnosticsInterest(ownerId);

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
            return m_PoseRuntime.TryCaptureMotionMatchingSearchReplay(
                providerId,
                out artifact);
        }

        public void CaptureMotionMatchingTrajectoryIntent(
            CharacterPresentationTrajectoryIntent intent)
        {
            RequireAlive();
            m_PoseRuntime.CaptureMotionMatchingTrajectoryIntent(intent);
        }

        internal void CaptureMotionMatchingPreviewQuery(
            string providerId,
            MotionMatchingSearchReplayArtifact query)
        {
            RequireAlive();
            m_PoseRuntime.CaptureMotionMatchingPreviewQuery(
                providerId,
                query);
        }

        public void Publish(
            PresentationCommand command,
            CharacterPresentationProducerEntry producer) =>
            Publish(
                CharacterPresentationCommand.FromFloat32(command),
                producer);

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
            m_PoseRuntime.PublishActionCommand(in actionCommand);
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
            m_PoseRuntime.RetireActionCommand(in actionCommand);
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
            m_PoseRuntime.ReplaceActionCommand(
                currentCommand.EventId,
                in replacementCommand);
        }

        internal ComposedAnimationPoseFrame Present(
            ulong presentationFrame,
            ulong latestSimulationTick,
            float interpolationAlpha,
            float presentationDeltaSeconds,
            in CharacterBodyPresentationFrame bodyFrame,
            in CharacterPresentationFactFrame factFrame,
            CharacterLinkedPoseRuntimeSession linkedPose,
            RuntimeDiagnosticsContext diagnostics = null)
        {
            CharacterPresentationProgramParameterFrame parameterFrame =
                CharacterPresentationProgramParameterFrame.FromFact(
                    in factFrame);
            return Present(
                presentationFrame,
                latestSimulationTick,
                interpolationAlpha,
                presentationDeltaSeconds,
                in bodyFrame,
                in factFrame,
                in parameterFrame,
                linkedPose,
                diagnostics);
        }

        internal ComposedAnimationPoseFrame PresentSequencePreview(
            PresentationPoseSourceIndex sourceIndex,
            double sampleTime,
            bool resetContinuity,
            ulong presentationFrame,
            ulong latestSimulationTick,
            float presentationDeltaSeconds,
            in CharacterBodyPresentationFrame bodyFrame,
            in CharacterPresentationFactFrame factFrame,
            CharacterLinkedPoseRuntimeSession linkedPose)
        {
            m_PoseRuntime.SetSequencePreview(
                sourceIndex,
                sampleTime,
                resetContinuity);
            try
            {
                return Present(
                    presentationFrame,
                    latestSimulationTick,
                    1f,
                    presentationDeltaSeconds,
                    in bodyFrame,
                    in factFrame,
                    linkedPose,
                    null);
            }
            finally
            {
                m_PoseRuntime.ClearSequencePreview();
            }
        }

        internal ComposedAnimationPoseFrame Present(
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
                m_PoseRuntime.ResolveDiagnosticsInterest(
                    traceInterest);
            bool publishStateDiagnostics =
                RequiresStateDiagnostics(diagnosticsInterest);
            if (diagnosticsInterest ==
                AnimationPresentationDiagnosticsInterest.None)
            {
                ClearPublishedDiagnostics();
            }

            CharacterPoseFrameTransaction transaction;
            using (TransactionBeginMarker.Auto())
            {
                transaction = BeginFrameTransaction(
                    presentationFrame,
                    latestSimulationTick,
                    publishStateDiagnostics,
                    diagnosticsInterest,
                    linkedPose);
            }
            string frameStage = "ActionLifecycle";
            try
            {
                IReadOnlyList<ActionAnimationPlaybackLifecycleFrame>
                    lifecycle;
                using (ActionLifecycleMarker.Auto())
                {
                    lifecycle =
                        m_PoseRuntime.PrepareActionLifecycleFrame(
                            transaction.PoseLease);
                }

                double presentationSampleTick =
                    bodyFrame.PreviousTick +
                    (bodyFrame.CurrentTick -
                     bodyFrame.PreviousTick) *
                     (double)bodyFrame.SampleAlpha;
                using (ActionSamplingMarker.Auto())
                {
                    frameStage = "ActionSampling";
                    m_PoseRuntime.ProjectActionPresentationSamples(
                        transaction.PoseLease,
                        lifecycle,
                        presentationSampleTick,
                        presentationDeltaSeconds);
                    m_PoseRuntime.ResolveActionPresentationFrames(
                        transaction.PoseLease);
                }

                using (PoseRoutingMarker.Auto())
                {
                    frameStage = "PoseAdvance";
                    m_PoseRuntime.Advance(
                        presentationDeltaSeconds,
                        in factFrame,
                        in parameterFrame);
                }
                MotionMatchingFrameResolution motionMatchingResolution;
                bool hasMotionMatchingResolution;
                using (MotionMatchingMarker.Auto())
                {
                    frameStage = "MotionMatching";
                    motionMatchingResolution = ResolveMotionMatching(
                        presentationFrame,
                        presentationDeltaSeconds,
                        in bodyFrame,
                        out hasMotionMatchingResolution);
                }
                using (PoseRoutingMarker.Auto())
                {
                    frameStage = "PoseFinalize";
                    m_PoseRuntime.FinalizePoseStateFrame(
                        in factFrame);
                    m_PoseRuntime.PublishActionSources(
                        transaction.PoseLease,
                        m_ActionSourceSamples);
                }
                CharacterPoseSourceDemand sourceDemand =
                    m_PoseRuntime.CreateSourceDemand(
                        transaction.PoseLease,
                        transaction.SourceLease,
                        m_ActionSourceSamples.Count,
                        m_PoseRuntime.ProviderSourceSampleCount);
                CharacterPoseProgramPrepared preparedPose =
                    m_PoseRuntime.PrepareEvaluation(
                        transaction.SourceLease,
                        transaction.PublicationLease,
                        in sourceDemand,
                        presentationDeltaSeconds,
                        m_ActionSourceSamples,
                        diagnosticsInterest !=
                            AnimationPresentationDiagnosticsInterest.None);
                CharacterPoseSourceFrameResult sourceFrame =
                    preparedPose.SourceFrame;
                transaction.BindSourceResults(
                    in sourceDemand,
                    in sourceFrame);
                if (hasMotionMatchingResolution)
                {
                    m_PoseRuntime.PrepareMotionMatchingFrameCompletion(
                        in motionMatchingResolution,
                        preparedPose.Lineage.CompletionIdentity);
                }
                using (ReleaseProtocolMarker.Auto())
                {
                    frameStage = "ReleaseProtocol";
                    m_PoseRuntime.CompleteActionReleaseProtocol(
                        transaction.PoseLease);
                    m_PoseRuntime.ValidateActionSamplingFrame(
                        transaction.PoseLease);
                    m_PoseRuntime.ValidateActionFrame(
                        transaction.PoseLease);
                    m_PoseRuntime.ValidatePendingSeal(
                        transaction.PoseLease,
                        transaction.SourceLease);
                    transaction.MarkValidated();
                }

                frameStage = "EvaluateBarrier";
                CharacterPoseFrameExecutionResult executionResult =
                    m_PoseRuntime.ExecuteEvaluateBarrier(
                    in bodyFrame,
                    in factFrame,
                    transaction.SourceLease,
                    transaction.ConstraintLease,
                    transaction.PublicationLease,
                    in preparedPose,
                    m_EnterEvaluateBarrier);
                transaction.BindExecutionResults(in executionResult);
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
                        $"completion={preparedPose.Lineage.CompletionIdentity}{solverFailure}.");
                }
                ComposedAnimationPoseFrame composedPose;
                using (FrameCommitMarker.Auto())
                {
                    frameStage = "FrameCommit";
                    if (hasMotionMatchingResolution)
                    {
                        m_PoseRuntime.CompleteMotionMatchingFrame();
                    }
                    CommitFrameTransaction(
                        transaction,
                        linkedPose);
                    composedPose =
                        m_PoseRuntime.FinalizeCommittedFrame(
                            transaction.PublicationLease);
                }

                using (PostCommitMarker.Auto())
                {
                    frameStage = "PostCommit";
                    m_PoseRuntime
                        .ApplyValidatedActionBackendReleaseCompletionAcknowledgements();
                    m_PoseRuntime
                        .ExecutePreparedActionBackendReleaseRequests();
                    bool publishRuntimeDiagnostics =
                        diagnosticsInterest !=
                        AnimationPresentationDiagnosticsInterest.None;
                    bool captureFootIk =
                        transaction.CaptureFootIkDiagnostics;
                    if (publishRuntimeDiagnostics ||
                        captureFootIk)
                    {
                        if (publishStateDiagnostics)
                            BuildCommittedSnapshots(transaction);
                        CharacterPoseSourceFrameResult committedSourceFrame =
                            transaction.SourceFrame;
                        m_PoseRuntime.BeginCommittedDiagnostics(
                            diagnosticsInterest,
                            captureFootIk,
                            linkedPose,
                            in committedSourceFrame,
                            in executionResult);
                        if (publishRuntimeDiagnostics)
                        {
                            CharacterFootIkCommittedCaptureViewLease
                                footIkCaptureView =
                                    m_PoseRuntime.PublishDiagnostics();
                            if (publishStateDiagnostics)
                                PublishCommittedSnapshots(transaction);
                            else
                                ClearCommittedStateSnapshots();
                            PublishCommittedDebugView(
                                publishStateDiagnostics);
                            AnimationPresentationTracePublisher
                                .PublishCompletedFootPlacement(
                                    m_ActorId,
                                    footIkCaptureView);
                            if (traceInterest !=
                                AnimationPresentationDiagnosticsInterest.None)
                            {
                                AnimationPresentationTracePublisher.Publish(
                                    diagnostics,
                                    m_DebugView,
                                    m_RetiredPlaybacks);
                            }
                        }
                    }
                    else
                    {
                        m_PoseRuntime.RecordNoDiagnosticsInterest();
                    }
                    if (hasMotionMatchingResolution)
                    {
                        m_PoseRuntime
                            .PublishMotionMatchingFrameDiagnostics(
                                diagnostics,
                                in motionMatchingResolution);
                    }
                }
                m_LastFrameOutcome =
                    AnimationPresentationFrameOutcome.Committed;
                return composedPose;
            }
            catch (Exception frameFailure)
            {
                if (transaction.Phase <
                    AnimationPresentationFramePhase.EvaluateBarrier)
                {
                    Exception discardFailure =
                        DiscardFrameTransaction(
                            transaction,
                            linkedPose);
                    if (discardFailure != null)
                    {
                        throw new AggregateException(
                            "Animation Presentation frame and Pending discard both failed.",
                            frameFailure,
                            discardFailure);
                    }
                }
                else
                {
                    m_PoseRuntime.DiscardPoseFrameAfterBarrier(
                        transaction.ConstraintLease,
                        transaction.PublicationLease);
                    MarkFaulted(transaction);
                    linkedPose.Discard();
                }
                throw new InvalidOperationException(
                    $"Animation Presentation failed during '{frameStage}'.",
                    frameFailure);
            }
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
                activation: m_PoseRuntime.CanApplyNextActivation,
                (block, resetOwnerState) =>
                    m_PoseRuntime.ApplyTuning(
                        m_Bindings.Projection.TuningLayout,
                        block,
                        checked(m_TuningGeneration + 1),
                        resetOwnerState),
                out _);
            if (applied)
                m_TuningGeneration++;
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
            m_PoseRuntime.Reset(reason);
            m_PoseRuntime.ResetAnimationSlots();
            m_PoseRuntime.ResetActionSampling();
            m_PoseRuntime.ResetActionPlayback();
            m_PoseRuntime.ResetPresentationWorkspace();
            m_PoseRuntime.ResetMotionMatching(
                0,
                MotionMatchingPresentationResetReason
                    .PresentationReset);
            ClearPublishedState();
        }

        internal void ResetPoseBranch(ulong resetSequence)
        {
            RequireAlive();
            m_PoseRuntime.Reset(
                PoseDiscontinuityResetReason.BranchReplacement);
            m_PoseRuntime.ResetAnimationSlots();
            m_PoseRuntime.ResetActionSampling();
            m_PoseRuntime.ResetPresentationWorkspace();
            m_PoseRuntime.ResetMotionMatching(
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
            m_PoseRuntime.ResetMotionMatching(
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
                m_PoseRuntime.Dispose();
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
                m_PoseRuntime.BeginActionPlaybackFrame(
                    frameIdentity,
                    presentationFrame);
                actionFrameOpen = true;
                m_PoseRuntime.BeginActionSamplingFrame(
                    frameIdentity,
                    presentationFrame,
                    captureDiagnostics);
                actionSamplingFrameOpen = true;
                m_PoseRuntime.BeginAnimationSlotFrame(frameIdentity);
                animationSlotFrameOpen = true;
                motionMatchingFrameOpen =
                    m_PoseRuntime.BeginMotionMatchingFrame(
                        frameIdentity);
                pose = m_PoseRuntime.BeginPendingFrame(
                    in lineage,
                    diagnosticsInterest,
                    linkedPose,
                    out source,
                    out constraint,
                    out publication,
                    out bool captureFootIkDiagnostics);
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
                        () => m_PoseRuntime.DiscardPendingFrame(
                            pose,
                            source,
                            constraint,
                            publication),
                        ref discardFailure);
                }
                if (motionMatchingFrameOpen)
                {
                    DiscardStep(
                        () => m_PoseRuntime
                            .DiscardMotionMatchingFrame(frameIdentity),
                        ref discardFailure);
                }
                if (animationSlotFrameOpen)
                {
                    DiscardStep(
                        () => m_PoseRuntime
                            .DiscardAnimationSlotFrame(frameIdentity),
                        ref discardFailure);
                }
                if (actionSamplingFrameOpen)
                {
                    DiscardStep(
                        () => m_PoseRuntime
                            .DiscardActionSamplingFrame(frameIdentity),
                        ref discardFailure);
                }
                if (actionFrameOpen)
                {
                    DiscardStep(
                        () => m_PoseRuntime
                            .DiscardActionPlaybackFrame(frameIdentity),
                        ref discardFailure);
                    DiscardStep(
                        () => m_PoseRuntime
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
            return m_PoseRuntime.ResolveMotionMatching(
                presentationFrame,
                presentationDeltaSeconds,
                in bodyFrame,
                out hasResolution);
        }

        void BuildCommittedSnapshots(
            CharacterPoseFrameTransaction transaction)
        {
            CopyActionSnapshots(
                m_PoseRuntime.BuildCommittedActionLifecycleSnapshot(),
                transaction.ActionSnapshots);
            m_PoseRuntime.BuildCommittedActionTimeSnapshots(
                transaction.TimeSnapshots);
            foreach (AnimationPlaybackId playbackId in
                     m_PoseRuntime.RetiredActionPlaybacks)
            {
                transaction.RetiredPlaybacks.Add(playbackId);
            }
            transaction.RetiredPlaybacks.Sort(
                ComparePlayback);
        }

        void PublishCommittedSnapshots(
            CharacterPoseFrameTransaction transaction)
        {
            CopyActionSnapshots(
                transaction.ActionSnapshots,
                m_ActionSnapshots);
            Copy(
                transaction.TimeSnapshots,
                m_ActionTimeSnapshots);
            Copy(
                transaction.RetiredPlaybacks,
                m_RetiredPlaybacks);
        }

        void PublishCommittedDebugView(
            bool includeStateDiagnostics)
        {
            if (!m_PoseRuntime.HasDiagnosticsSnapshot)
            {
                m_PoseStateSourceSyncSnapshots.Clear();
                m_DebugView = null;
                return;
            }
            if (includeStateDiagnostics)
            {
                m_PoseRuntime.CopySourceSyncSnapshots(
                    m_PoseStateSourceSyncSnapshots);
            }
            else
            {
                m_PoseStateSourceSyncSnapshots.Clear();
            }
            AnimationPresentationRuntimeSnapshot posePlan =
                m_PoseRuntime.DiagnosticsSnapshot;
            m_DebugView =
                new AnimationPresentationDebugView(
                    in posePlan,
                    m_ActionSnapshots,
                    m_ActionTimeSnapshots,
                    m_PoseStateSourceSyncSnapshots);
        }

        void ClearCommittedStateSnapshots()
        {
            m_ActionSnapshots.Clear();
            m_ActionTimeSnapshots.Clear();
            m_RetiredPlaybacks.Clear();
            m_PoseStateSourceSyncSnapshots.Clear();
        }

        void ClearPublishedDiagnostics()
        {
            if (m_DebugView == null &&
                !m_PoseRuntime.HasDiagnosticsSnapshot &&
                m_ActionSnapshots.Count == 0 &&
                m_ActionTimeSnapshots.Count == 0 &&
                m_RetiredPlaybacks.Count == 0 &&
                m_PoseStateSourceSyncSnapshots.Count == 0)
            {
                return;
            }
            ClearCommittedStateSnapshots();
            m_DebugView = null;
            m_PoseRuntime.InvalidateDiagnosticsSnapshot();
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
            m_PoseRuntime.CommitPresentationWorkspaceFrame(
                transaction.PoseLease);
            m_PoseRuntime.CommitActionSamplingFrame(
                transaction.PoseLease);
            m_PoseRuntime.CommitAnimationSlotFrame(
                transaction.PoseLease);
            m_PoseRuntime.CommitActionPlaybackFrame(
                transaction.PoseLease);
            m_PoseRuntime.CommitMotionMatchingFrame(
                transaction.PoseLease);
            m_PoseRuntime.SealFrame(
                transaction.PoseLease,
                transaction.SourceLease,
                transaction.ConstraintLease,
                transaction.PublicationLease);
            linkedPose.Seal();
            transaction.MarkSealed();
        }

        Exception DiscardFrameTransaction(
            CharacterPoseFrameTransaction transaction,
            CharacterLinkedPoseRuntimeSession linkedPose)
        {
            if (transaction == null ||
                transaction.Closed)
            {
                return null;
            }
            Exception failure = null;
            DiscardStep(
                () => m_PoseRuntime.DiscardPendingFrame(
                    transaction.PoseLease,
                    transaction.SourceLease,
                    transaction.ConstraintLease,
                    transaction.PublicationLease),
                ref failure);
            DiscardStep(
                () => m_PoseRuntime.DiscardMotionMatchingFrame(
                    transaction.Lineage.FrameIdentity),
                ref failure);
            DiscardStep(
                () => m_PoseRuntime.DiscardAnimationSlotFrame(
                    transaction.Lineage.FrameIdentity),
                ref failure);
            DiscardStep(
                () => m_PoseRuntime.DiscardActionSamplingFrame(
                    transaction.Lineage.FrameIdentity),
                ref failure);
            DiscardStep(
                () => m_PoseRuntime.DiscardActionPlaybackFrame(
                    transaction.Lineage.FrameIdentity),
                ref failure);
            DiscardStep(
                () => m_PoseRuntime.DiscardPresentationWorkspaceFrame(
                    transaction.Lineage.FrameIdentity),
                ref failure);
            DiscardStep(
                linkedPose.Discard,
                ref failure);
            transaction.MarkDiscarded();
            m_LastFrameOutcome =
                AnimationPresentationFrameOutcome.None;
            if (m_DiscardCount != ulong.MaxValue)
                m_DiscardCount++;
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

        static void CopyActionSnapshots(
            IReadOnlyList<ActionAnimationPlaybackLifecycleSnapshot>
                source,
            FixedCapacityFrameBuffer<ActionAnimationPlaybackLifecycleSnapshot>
                destination)
        {
            destination.Clear();
            for (int i = 0; i < source.Count; i++)
                destination.Add(source[i]);
        }

        static void CopyActionSnapshots(
            IReadOnlyList<ActionAnimationPlaybackLifecycleSnapshot>
                source,
            List<ActionAnimationPlaybackLifecycleSnapshot>
                destination)
        {
            Copy(source, destination);
        }

        static void Copy<T>(
            IReadOnlyList<T> source,
            List<T> destination)
        {
            destination.Clear();
            for (int i = 0; i < source.Count; i++)
                destination.Add(source[i]);
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
            m_ActionSnapshots.Clear();
            m_ActionTimeSnapshots.Clear();
            m_PoseStateSourceSyncSnapshots.Clear();
            m_RetiredPlaybacks.Clear();
            m_ActionSourceSamples.Clear();
            m_DebugView = null;
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
                m_PoseRuntime.FrameCompletionContext);
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

        static int ComparePlayback(
            AnimationPlaybackId left,
            AnimationPlaybackId right)
        {
            int producer = string.Compare(
                left.ProducerId.ProgramProducerIdentity,
                right.ProducerId.ProgramProducerIdentity,
                StringComparison.Ordinal);
            return producer != 0
                ? producer
                : left.Generation.CompareTo(right.Generation);
        }

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
