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
        readonly CharacterActionPlaybackRuntime m_ActionPlayback;
        readonly ActionPresentationSamplingRuntime m_ActionSampling;
        readonly AnimationSlotRuntime m_AnimationSlots;
        readonly PresentationFrameWorkspace m_FrameWorkspace;
        readonly PosePlanExecutionRuntime m_PoseRuntime;
        readonly CharacterMotionMatchingPresentationModule m_MotionMatching;
        readonly List<ActionAnimationPlaybackLifecycleSnapshot>
            m_ActionSnapshots;
        readonly List<ActionPresentationTimeSnapshot>
            m_ActionTimeSnapshots;
        readonly List<PoseStateSourceSyncSnapshot>
            m_PoseStateSourceSyncSnapshots;
        readonly List<AnimationPlaybackId> m_RetiredPlaybacks;
        IReadOnlyList<AnimationSlotFramePlan> m_SlotPlans =
            Array.Empty<AnimationSlotFramePlan>();
        IReadOnlyList<AnimationSlotActionSourcePlan>
            m_ActionSourcePlans =
                Array.Empty<AnimationSlotActionSourcePlan>();
        readonly List<AnimationSlotSourceReleaseCompletion>
            m_SlotReleaseCompletions;
        readonly List<ActionBackendReleaseCompletion>
            m_BackendReleaseCompletions;
        readonly Dictionary<AnimationPlayerSourceSampleKey,
            AnimationResolvedPoseSourceSample>
            m_ActionSourceSamples;
        readonly Dictionary<AnimationPlayerSourceSampleKey,
            PresentationPoseSourceSample>
            m_ProviderSourceSamples;
        readonly FixedCapacityFrameBuffer<AnimationPlaybackId>
            m_RetiredThisFrame;
        readonly AnimationPresentationFrameTransaction m_FrameTransaction;
        readonly Action m_EnterEvaluateBarrier;
        readonly AnimationPresentationRuntimeCapacityMetrics
            m_CapacityMetrics;
        readonly int m_ActionSourceSampleCapacity;
        readonly int m_ProviderSourceSampleCapacity;
        CharacterPoseTuningRuntimeBinding m_TuningBinding;
        CharacterPoseTuningTargetIdentity m_TuningTarget;

        ulong m_NextFrameTransactionIdentity;
        ulong m_NextPresentationRequestSequence;
        AnimationPresentationDebugView m_DebugView;
        AnimationPresentationFault m_Fault;
        AnimationPresentationFrameOutcome m_LastFrameOutcome;
        ulong m_DiscardCount;
        bool m_Faulted;
        bool m_Disposed;

        internal CharacterAnimationPresentationRuntime(
            ActorId actorId,
            CharacterAnimationPresentationBindings bindings,
            CharacterMotionMatchingPresentationModule motionMatching,
            AnimancerComponent animancer,
            CharacterAnimationRigBinding rigBinding,
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
            m_ActionPlayback =
                new CharacterActionPlaybackRuntime(
                    bindings.ActionPlayback);
            m_ActionSampling =
                new ActionPresentationSamplingRuntime(
                    bindings.ActionPlayback);
            m_AnimationSlots =
                new AnimationSlotRuntime(
                    bindings.ActionPlayback);
            int frameCapacity = m_ActionPlayback.FrameCapacity;
            int providerCapacity =
                bindings.Projection.MotionMatching?.NodeBindingCount ?? 0;
            m_ActionSourceSampleCapacity = frameCapacity;
            m_ProviderSourceSampleCapacity = providerCapacity;
            int releaseCompletionCapacity =
                m_ActionPlayback.BackendReleaseCompletionCapacity;
            int failureCapacity = Math.Max(
                1,
                checked(
                    providerCapacity +
                    bindings.Projection.PosePlan.StateMachines.Count));
            m_FrameWorkspace =
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
            m_ProviderSourceSamples =
                new Dictionary<AnimationPlayerSourceSampleKey,
                    PresentationPoseSourceSample>(providerCapacity);
            m_RetiredThisFrame =
                new FixedCapacityFrameBuffer<AnimationPlaybackId>(
                    frameCapacity);
            m_SlotReleaseCompletions =
                new List<AnimationSlotSourceReleaseCompletion>(
                    frameCapacity);
            m_BackendReleaseCompletions =
                new List<ActionBackendReleaseCompletion>(
                    releaseCompletionCapacity);
            m_FrameTransaction =
                new AnimationPresentationFrameTransaction(
                    frameCapacity,
                    releaseCompletionCapacity);
            m_EnterEvaluateBarrier =
                m_FrameTransaction.EnterEvaluateBarrier;
            try
            {
                m_PoseRuntime =
                    new PosePlanExecutionRuntime(
                        animancer,
                        rigBinding,
                        bindings.Projection,
                        footPlacement,
                        ownsGraphClock);
                m_CapacityMetrics =
                    m_PoseRuntime.CreateCapacityMetrics(
                        m_ActionPlayback.JournalCapacity,
                        m_ActionSampling.JournalCapacity,
                        Math.Max(1, m_AnimationSlots.SlotCount));
                m_MotionMatching = motionMatching;
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
            m_FrameWorkspace.ActionUsages;
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
            m_MotionMatching != null &&
            m_MotionMatching.Enabled;
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
            m_MotionMatching?.AcceptsTrajectoryIntent == true;
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
            artifact = null;
            return m_MotionMatching != null &&
                   m_MotionMatching.TryCaptureSearchReplay(
                       providerId,
                       out artifact);
        }

        public void CaptureMotionMatchingTrajectoryIntent(
            CharacterPresentationTrajectoryIntent intent)
        {
            RequireAlive();
            if (m_MotionMatching == null)
            {
                throw new InvalidOperationException(
                    "Presentation without a Motion Matching payload cannot accept trajectory intent.");
            }
            m_MotionMatching.CaptureTrajectoryIntent(intent);
        }

        internal void CaptureMotionMatchingPreviewQuery(
            string providerId,
            MotionMatchingSearchReplayArtifact query)
        {
            RequireAlive();
            RequireMotionMatchingModule().CapturePreviewQuery(
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
            m_ActionPlayback.Publish(
                ActionAnimationPlaybackCommandFactory.Create(
                    command,
                    in binding));
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
            m_ActionPlayback.Retire(actionCommand);
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
            m_ActionPlayback.Replace(
                currentCommand.EventId,
                replacementCommand);
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

            AnimationPresentationFrameTransaction transaction;
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
                    ConsumeBackendReleaseCompletions(transaction);
                    m_PoseRuntime
                        .ValidateActionBackendReleaseCompletionAcknowledgements(
                            transaction.ConsumedReleaseCompletions);
                    lifecycle =
                        m_ActionPlayback.BuildLifecycleFrame(
                            transaction.ActionTransaction);
                    RebaseRetiredMarkerSources(
                        transaction,
                        lifecycle);
                    m_SlotPlans =
                        m_AnimationSlots.BuildFramePlans(
                            transaction.SlotLease,
                            lifecycle);
                }

                double presentationSampleTick =
                    bodyFrame.PreviousTick +
                    (bodyFrame.CurrentTick -
                     bodyFrame.PreviousTick) *
                     (double)bodyFrame.SampleAlpha;
                using (ActionSamplingMarker.Auto())
                {
                    frameStage = "ActionSampling";
                    m_ActionSampling.ProjectPresentationSamples(
                        transaction.SamplingTransaction,
                        m_ActionPlayback,
                        transaction.ActionTransaction,
                        lifecycle,
                        presentationSampleTick,
                        presentationDeltaSeconds);
                    m_ActionSampling.ResolvePresentationFrames(
                        transaction.SamplingTransaction,
                        m_FrameWorkspace,
                        transaction.WorkspaceLease);
                    m_ActionSourcePlans =
                        m_AnimationSlots.CollectActionSourcePlans(
                            transaction.SlotLease);
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
                        transaction,
                        presentationFrame,
                        presentationDeltaSeconds,
                        in bodyFrame,
                        diagnostics,
                        out hasMotionMatchingResolution);
                }
                using (PoseRoutingMarker.Auto())
                {
                    frameStage = "PoseFinalize";
                    m_PoseRuntime.FinalizePoseStateFrame(
                        in factFrame,
                        m_FrameWorkspace,
                        transaction.WorkspaceLease);
                    PublishActionSources();
                    PublishSlotTargets();
                }
                PosePlanPreparedEvaluation preparedPose =
                    m_PoseRuntime.PrepareEvaluation(
                        presentationDeltaSeconds,
                        m_ActionSourceSamples,
                        m_ProviderSourceSamples,
                        diagnosticsInterest !=
                            AnimationPresentationDiagnosticsInterest.None);
                if (hasMotionMatchingResolution)
                {
                    m_PoseRuntime
                        .PrepareMotionMatchingPosePlanCompletion(
                            in motionMatchingResolution,
                            preparedPose.CompletionIdentity);
                    m_MotionMatching.PrepareFrameCompletion(
                        in motionMatchingResolution,
                        preparedPose.CompletionIdentity);
                }
                using (ReleaseProtocolMarker.Auto())
                {
                    frameStage = "ReleaseProtocol";
                    CompleteSlotSourceReleases(transaction);
                    PublishActionUsageAndRetirement(transaction);
                    m_ActionSampling.ValidateFrame(
                        transaction.SamplingTransaction);
                    m_ActionPlayback.ValidateFrame(
                        transaction.ActionTransaction);
                    m_PoseRuntime.ValidatePendingSeal(
                        transaction.PoseLease);
                    transaction.MarkValidated();
                }

                frameStage = "EvaluateBarrier";
                m_PoseRuntime.ExecuteEvaluateBarrier(
                    m_ActorId,
                    presentationFrame,
                    in bodyFrame,
                    in factFrame,
                    in preparedPose,
                    m_EnterEvaluateBarrier);
                AnimationPresentationFrameOutcome poseOutcome =
                    m_PoseRuntime.PendingFrameOutcome;
                if (poseOutcome !=
                    AnimationPresentationFrameOutcome.Committed)
                {
                    AnimationFinalPoseNativeReadBinding finalRead =
                        preparedPose.FinalRead;
                    int invalidOperation = finalRead.PoseGraphInvalidOperationIndex[0];
                    string solverFailure =
                        finalRead.PoseGraphInvalidReason[0] ==
                            AnimationPoseNativeInvalidReason.FullBodyIkSolverInvalid &&
                        m_PoseRuntime.TryGetFullBodyIkFailure(
                            invalidOperation,
                            preparedPose.CompletionIdentity,
                            out CharacterFullBodyIkResult fullBodyIkResult)
                            ? $", solverFailure={fullBodyIkResult.Failure}, " +
                              $"failedGoalSet={fullBodyIkResult.FailedGoalSetIndex}, " +
                              $"failedSlot={fullBodyIkResult.FailedSlot}, " +
                              $"appliedGoals={fullBodyIkResult.AppliedGoalCount}" +
                              FormatFullBodyIkFailure(fullBodyIkResult)
                            : string.Empty;
                    throw new InvalidOperationException(
                        $"Animation Presentation frame produced '{poseOutcome}' after the Evaluate Barrier: " +
                        $"availability={finalRead.Availability[0]}, " +
                        $"outputReason={finalRead.OutputInvalidReason[0]}, " +
                        $"graphReason={finalRead.PoseGraphInvalidReason[0]}, " +
                        $"operation={invalidOperation}, " +
                        $"completion={preparedPose.CompletionIdentity}{solverFailure}.");
                }
                ComposedAnimationPoseFrame composedPose;
                using (FrameCommitMarker.Auto())
                {
                    frameStage = "FrameCommit";
                    if (hasMotionMatchingResolution)
                    {
                        MotionMatchingPosePlanCompletion completion =
                            m_PoseRuntime
                                .BuildMotionMatchingPosePlanCompletion();
                        m_MotionMatching.CompleteFrame(
                            in completion);
                    }
                    CommitFrameTransaction(
                        transaction,
                        linkedPose);
                    composedPose =
                        m_PoseRuntime.FinalizeCommittedFrame();
                }

                using (PostCommitMarker.Auto())
                {
                    frameStage = "PostCommit";
                    m_PoseRuntime
                        .ApplyValidatedActionBackendReleaseCompletionAcknowledgements();
                    m_PoseRuntime
                        .ExecutePreparedActionBackendReleaseRequests();
                    if (diagnosticsInterest !=
                        AnimationPresentationDiagnosticsInterest.None)
                    {
                        if (publishStateDiagnostics)
                            BuildCommittedSnapshots(transaction);
                        m_PoseRuntime.BeginCommittedDiagnostics(
                            diagnosticsInterest,
                            linkedPose);
                        m_PoseRuntime.PublishDiagnostics();
                        if (publishStateDiagnostics)
                            PublishCommittedSnapshots(transaction);
                        else
                            ClearCommittedStateSnapshots();
                        PublishCommittedDebugView(
                            publishStateDiagnostics);
                        AnimationPresentationTracePublisher.PublishCompletedFootPlacement(
                            m_ActorId,
                            m_DebugView.PosePlan.FootPlacement);
                        if (traceInterest !=
                            AnimationPresentationDiagnosticsInterest.None)
                        {
                            AnimationPresentationTracePublisher.Publish(
                                diagnostics,
                                m_DebugView,
                                m_RetiredPlaybacks);
                        }
                    }
                    else
                    {
                        m_PoseRuntime.RecordNoDiagnosticsInterest();
                    }
                    if (hasMotionMatchingResolution)
                    {
                        m_MotionMatching
                            .PublishCommittedFrameDiagnostics(
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
                    m_PoseRuntime.DiscardPoseConstraintsAfterBarrier();
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
            CharacterPoseTuningParameterBlock previous =
                m_TuningBinding.ActiveBlock;
            m_TuningBinding.TryApplyPending(
                m_TuningTarget,
                presentationFrame,
                activation: m_PoseRuntime.CanApplyNextActivation,
                (block, resetOwnerState) =>
                {
                    string error = m_PoseRuntime.ApplyTuning(
                        m_Bindings.Projection.TuningLayout,
                        block,
                        resetOwnerState);
                    if (string.IsNullOrEmpty(error) || previous == null)
                        return error;
                    string rollbackError = m_PoseRuntime.ApplyTuning(
                        m_Bindings.Projection.TuningLayout,
                        previous,
                        false);
                    return string.IsNullOrEmpty(rollbackError)
                        ? error
                        : $"{error} Rollback failed: {rollbackError}";
                },
                out _);
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
            m_AnimationSlots.Reset();
            m_ActionSampling.Reset();
            m_ActionPlayback.Reset();
            m_FrameWorkspace.Reset();
            m_MotionMatching?.Reset(
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
            m_AnimationSlots.Reset();
            m_ActionSampling.Reset();
            m_FrameWorkspace.Reset();
            m_MotionMatching?.Reset(
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
            m_MotionMatching?.Reset(
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
            try
            {
                m_MotionMatching?.Dispose();
            }
            catch (Exception exception)
            {
                if (failure == null)
                    failure = exception;
            }
            ClearPublishedState();
            if (failure != null)
                throw failure;
        }

        AnimationPresentationFrameTransaction BeginFrameTransaction(
            ulong presentationFrame,
            ulong bodyTick,
            bool captureDiagnostics,
            AnimationPresentationDiagnosticsInterest diagnosticsInterest,
            CharacterLinkedPoseRuntimeSession linkedPose)
        {
            PresentationFrameWorkspaceLease workspaceLease = default;
            CharacterActionPlaybackFrameTransaction action = null;
            ActionPresentationSamplingFrameTransaction sampling = null;
            AnimationSlotMutationLease slot = default;
            PosePlanFrameLease pose = default;
            MotionMatchingFrameMutationLease motionMatching =
                default;
            m_NextFrameTransactionIdentity++;
            if (m_NextFrameTransactionIdentity == 0)
                m_NextFrameTransactionIdentity++;
            ulong frameIdentity = m_NextFrameTransactionIdentity;
            bool linkedPosePrepared = false;
            try
            {
                linkedPose.Prepare();
                linkedPosePrepared = true;
                workspaceLease =
                    m_FrameWorkspace.Begin(
                        frameIdentity,
                        presentationFrame);
                action =
                    m_ActionPlayback.BeginFrame(
                        frameIdentity,
                        presentationFrame);
                sampling =
                    m_ActionSampling.BeginFrame(
                        frameIdentity,
                        presentationFrame,
                        captureDiagnostics);
                slot =
                    m_AnimationSlots.BeginFrame(frameIdentity);
                if (m_MotionMatching != null)
                {
                    motionMatching =
                        m_MotionMatching.BeginPendingFrame(
                            frameIdentity);
                }
                pose = m_PoseRuntime.BeginPendingFrame(
                    frameIdentity,
                    presentationFrame,
                    diagnosticsInterest,
                    linkedPose);
                m_FrameTransaction.Begin(
                    frameIdentity,
                    presentationFrame,
                    bodyTick,
                    workspaceLease,
                    action,
                    sampling,
                    slot,
                    pose,
                    motionMatching,
                    m_MotionMatching != null);
                m_FrameTransaction.BeginPrepare();
                return m_FrameTransaction;
            }
            catch (Exception beginFailure)
            {
                Exception discardFailure = null;
                if (pose.IsValid)
                {
                    DiscardStep(
                        () => m_PoseRuntime.DiscardPendingFrame(pose),
                        ref discardFailure);
                }
                if (motionMatching.IsValid)
                {
                    DiscardStep(
                        () => m_MotionMatching.DiscardFrame(
                            motionMatching),
                        ref discardFailure);
                }
                if (slot.IsValid)
                {
                    DiscardStep(
                        () => m_AnimationSlots.DiscardFrame(slot),
                        ref discardFailure);
                }
                if (sampling?.IsValid == true)
                {
                    DiscardStep(
                        () => m_ActionSampling.DiscardFrame(
                            sampling),
                        ref discardFailure);
                }
                if (action?.IsValid == true)
                {
                    DiscardStep(
                        () => m_ActionPlayback.DiscardFrame(action),
                        ref discardFailure);
                }
                if (workspaceLease.IsValid)
                {
                    DiscardStep(
                        () => m_FrameWorkspace.Discard(
                            workspaceLease),
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

        void ConsumeBackendReleaseCompletions(
            AnimationPresentationFrameTransaction transaction)
        {
            m_PoseRuntime.CopyActionBackendReleaseCompletions(
                m_BackendReleaseCompletions);
            if (m_BackendReleaseCompletions.Count == 0)
                return;
            for (int i = 0;
                 i < m_BackendReleaseCompletions.Count;
                 i++)
            {
                ActionBackendReleaseCompletion completion =
                    m_BackendReleaseCompletions[i];
                m_FrameWorkspace.AddReleaseCompletion(
                    transaction.WorkspaceLease,
                    completion);
                transaction.ConsumedReleaseCompletions.Add(
                    completion);
            }
            m_ActionPlayback.ApplyBackendReleaseCompletions(
                transaction.ActionTransaction,
                m_BackendReleaseCompletions);
        }

        void RebaseRetiredMarkerSources(
            AnimationPresentationFrameTransaction transaction,
            IReadOnlyList<ActionAnimationPlaybackLifecycleFrame>
                lifecycle)
        {
            m_RetiredThisFrame.Clear();
            for (int i = 0; i < lifecycle.Count; i++)
            {
                ActionAnimationPlaybackLifecycleFrame snapshot =
                    lifecycle[i];
                if (snapshot.Phase !=
                    ActionAnimationPlaybackLifecyclePhase.Retired)
                {
                    continue;
                }
                bool completedThisFrame = false;
                for (int completionIndex = 0;
                     completionIndex <
                     transaction.ConsumedReleaseCompletions.Count;
                     completionIndex++)
                {
                    if (transaction
                        .ConsumedReleaseCompletions[completionIndex]
                        .PlaybackId.Equals(snapshot.PlaybackId))
                    {
                        completedThisFrame = true;
                        break;
                    }
                }
                if (!completedThisFrame ||
                    !TryAddRetiredThisFrame(snapshot.PlaybackId))
                {
                    continue;
                }
            }
        }

        MotionMatchingFrameResolution ResolveMotionMatching(
            AnimationPresentationFrameTransaction transaction,
            ulong presentationFrame,
            float presentationDeltaSeconds,
            in CharacterBodyPresentationFrame bodyFrame,
            RuntimeDiagnosticsContext diagnostics,
            out bool hasResolution)
        {
            m_ActionSourceSamples.Clear();
            m_ProviderSourceSamples.Clear();
            hasResolution = false;
            if (m_MotionMatching == null)
                return default;
            MotionMatchingPoseStateDemandBatch demands =
                m_PoseRuntime.BuildMotionMatchingDemandBatch(
                    presentationFrame,
                    bodyFrame.ResetSequence,
                    m_FrameWorkspace,
                    transaction.WorkspaceLease);
            if (!m_MotionMatching.HasFrameWork(in demands))
                return default;
            MotionMatchingFrameResolution resolution =
                m_MotionMatching.ResolveFrame(
                    presentationFrame,
                    presentationDeltaSeconds,
                    in bodyFrame,
                    in demands,
                    null);
            if (resolution.SelectionCount >
                m_ProviderSourceSampleCapacity)
            {
                throw new InvalidOperationException(
                    "Motion Matching source sample capacity was exceeded.");
            }
            m_PoseRuntime.ApplyMotionMatchingSelections(
                in resolution,
                m_ProviderSourceSamples,
                m_FrameWorkspace,
                transaction.WorkspaceLease);
            hasResolution = true;
            return resolution;
        }

        void PublishActionSources()
        {
            if (m_ActionSourcePlans.Count >
                m_ActionSourceSampleCapacity)
            {
                throw new InvalidOperationException(
                    "Action source sample capacity was exceeded.");
            }
            IReadOnlyDictionary<AnimationPlaybackId,
                ActionAnimationPlaybackFrame> frames =
                    m_FrameWorkspace.ActionFrames;
            for (int i = 0; i < m_ActionSourcePlans.Count; i++)
            {
                AnimationSlotActionSourcePlan sourcePlan =
                    m_ActionSourcePlans[i];
                if (!frames.TryGetValue(
                        sourcePlan.PlaybackId,
                        out ActionAnimationPlaybackFrame frame) ||
                    !m_Bindings.ActionPlayback.TryGet(
                        sourcePlan.PlaybackId.ProducerId,
                        out ResolvedActionAnimationBinding binding) ||
                    binding.SlotId != sourcePlan.SlotId ||
                    binding.SlotNodeId != sourcePlan.SlotNodeId)
                {
                    throw new InvalidOperationException(
                        $"Animation Slot Action source '{sourcePlan.PlaybackId}' has no exact resolved frame.");
                }
                if (sourcePlan.Current)
                {
                    m_PoseRuntime.PublishActionFrame(
                        in frame,
                        in binding,
                        sourcePlan.SelectionGeneration,
                        NextPresentationRequestSequence(),
                        m_ActionSourceSamples);
                }
                else
                {
                    m_PoseRuntime.PublishRetainedActionFrame(
                        in frame,
                        in binding,
                        sourcePlan.SelectionGeneration,
                        NextPresentationRequestSequence(),
                        m_ActionSourceSamples);
                }
            }
        }

        void PublishSlotTargets()
        {
            for (int i = 0; i < m_SlotPlans.Count; i++)
            {
                AnimationSlotFramePlan plan = m_SlotPlans[i];
                if (!plan.TargetsSourcePose)
                    continue;
                m_PoseRuntime.PublishActionSourcePose(
                    plan.SlotId,
                    plan.SlotNodeId,
                    NextPresentationRequestSequence());
            }
        }

        void CompleteSlotSourceReleases(
            AnimationPresentationFrameTransaction transaction)
        {
            m_PoseRuntime.CopyActionSlotReleaseCompletions(
                m_SlotReleaseCompletions);
            for (int i = 0;
                 i < m_SlotReleaseCompletions.Count;
                 i++)
            {
                AnimationSlotSourceReleaseCompletion completion =
                    m_SlotReleaseCompletions[i];
                m_AnimationSlots.CompleteSourceRelease(
                    transaction.SlotLease,
                    completion.SlotId,
                    completion.PlaybackId);
            }
        }

        void PublishActionUsageAndRetirement(
            AnimationPresentationFrameTransaction transaction)
        {
            m_AnimationSlots.PublishActionUsages(
                transaction.SlotLease,
                m_FrameWorkspace,
                transaction.WorkspaceLease);
            m_ActionPlayback.ReplaceSlotUsageBatch(
                transaction.ActionTransaction,
                m_FrameWorkspace.ActionUsages);

            IReadOnlyList<ActionAnimationPlaybackLifecycleFrame>
                lifecycle =
                    m_ActionPlayback.BuildLifecycleFrame(
                        transaction.ActionTransaction);
            m_AnimationSlots.PublishRetirementPermissions(
                transaction.SlotLease,
                lifecycle,
                m_FrameWorkspace,
                transaction.WorkspaceLease);
            m_ActionPlayback.ApplyRetirementPermissions(
                transaction.ActionTransaction,
                m_FrameWorkspace.RetirementPermissions);

            lifecycle =
                m_ActionPlayback.BuildLifecycleFrame(
                    transaction.ActionTransaction);
            for (int i = 0; i < lifecycle.Count; i++)
            {
                ActionAnimationPlaybackLifecycleFrame snapshot =
                    lifecycle[i];
                if (snapshot.Phase !=
                        ActionAnimationPlaybackLifecyclePhase
                            .RetirementPermitted ||
                    snapshot.BackendReleaseRequestIdentity != 0)
                {
                    continue;
                }
                if (m_PoseRuntime
                    .TryPrepareActionBackendReleaseRequest(
                        snapshot.PlaybackId,
                        out ActionBackendReleaseRequest request))
                {
                    m_ActionPlayback.RegisterBackendReleaseRequest(
                        transaction.ActionTransaction,
                        request);
                    m_FrameWorkspace.AddReleaseRequest(
                        transaction.WorkspaceLease,
                        request);
                    continue;
                }
                m_ActionPlayback.RetireWithoutBackendResources(
                    transaction.ActionTransaction,
                    snapshot.PlaybackId);
                TryAddRetiredThisFrame(snapshot.PlaybackId);
            }
        }

        void BuildCommittedSnapshots(
            AnimationPresentationFrameTransaction transaction)
        {
            CopyActionSnapshots(
                m_ActionPlayback.BuildCommittedLifecycleSnapshot(),
                transaction.ActionSnapshots);
            m_ActionSampling.BuildCommittedTimeSnapshots(
                transaction.TimeSnapshots);
            foreach (AnimationPlaybackId playbackId in
                     m_RetiredThisFrame)
            {
                transaction.RetiredPlaybacks.Add(playbackId);
            }
            transaction.RetiredPlaybacks.Sort(
                ComparePlayback);
        }

        bool TryAddRetiredThisFrame(AnimationPlaybackId playbackId)
        {
            for (int i = 0; i < m_RetiredThisFrame.Count; i++)
            {
                if (m_RetiredThisFrame[i].Equals(playbackId))
                    return false;
            }
            m_RetiredThisFrame.Add(playbackId);
            return true;
        }

        void PublishCommittedSnapshots(
            AnimationPresentationFrameTransaction transaction)
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
            AnimationPresentationFrameTransaction transaction,
            CharacterLinkedPoseRuntimeSession linkedPose)
        {
            if (transaction == null ||
                !transaction.IsValid)
            {
                throw new InvalidOperationException(
                    "Animation Presentation frame transaction cannot commit.");
            }
            m_FrameWorkspace.Commit(
                transaction.WorkspaceLease);
            m_ActionSampling.SealFrame(
                transaction.SamplingTransaction);
            m_AnimationSlots.CommitFrame(
                transaction.SlotLease);
            m_ActionPlayback.Commit(
                transaction.ActionTransaction);
            if (transaction.HasMotionMatchingLease)
            {
                m_MotionMatching.SealFrame(
                    transaction.MotionMatchingLease);
            }
            m_PoseRuntime.SealFrame(
                transaction.PoseLease);
            linkedPose.Seal();
            transaction.MarkSealed();
        }

        Exception DiscardFrameTransaction(
            AnimationPresentationFrameTransaction transaction,
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
                    transaction.PoseLease),
                ref failure);
            if (transaction.HasMotionMatchingLease)
            {
                DiscardStep(
                    () => m_MotionMatching.DiscardFrame(
                        transaction.MotionMatchingLease),
                    ref failure);
            }
            DiscardStep(
                () => m_AnimationSlots.DiscardFrame(
                    transaction.SlotLease),
                ref failure);
            DiscardStep(
                () => m_ActionSampling.DiscardFrame(
                    transaction.SamplingTransaction),
                ref failure);
            DiscardStep(
                () => m_ActionPlayback.DiscardFrame(
                    transaction.ActionTransaction),
                ref failure);
            DiscardStep(
                () => m_FrameWorkspace.Discard(
                    transaction.WorkspaceLease),
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

        CharacterMotionMatchingPresentationModule
            RequireMotionMatchingModule() =>
                m_MotionMatching ??
                throw new InvalidOperationException(
                    "Presentation has no Motion Matching module.");

        ulong NextPresentationRequestSequence()
        {
            m_NextPresentationRequestSequence++;
            if (m_NextPresentationRequestSequence == 0)
            {
                throw new InvalidOperationException(
                    "Animation Presentation request identity was exhausted.");
            }
            return m_NextPresentationRequestSequence;
        }

        void ClearPublishedState()
        {
            m_ActionSnapshots.Clear();
            m_ActionTimeSnapshots.Clear();
            m_PoseStateSourceSyncSnapshots.Clear();
            m_RetiredPlaybacks.Clear();
            m_SlotPlans = Array.Empty<AnimationSlotFramePlan>();
            m_ActionSourcePlans =
                Array.Empty<AnimationSlotActionSourcePlan>();
            m_SlotReleaseCompletions.Clear();
            m_BackendReleaseCompletions.Clear();
            m_ActionSourceSamples.Clear();
            m_ProviderSourceSamples.Clear();
            m_RetiredThisFrame.Clear();
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
            AnimationPresentationFrameTransaction transaction)
        {
            if (m_Faulted)
                return;
            AnimationPresentationFramePhase phase =
                transaction.Phase;
            m_Fault = new AnimationPresentationFault(
                m_ActorId,
                transaction.PresentationFrame,
                transaction.BodyTick,
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
            CharacterPresentationPosePlan plan)
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
