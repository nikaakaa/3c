using System;
using System.Collections.Generic;
using Animancer;
using BTSMTL.Diagnostics;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Pipeline.Animation.Lifecycle;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;
using ThirdPersonCharacter.Pipeline.Animation.Sources;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonCharacter.Pipeline.Presentation.Animancer;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Animation.Presentation
{
    internal sealed partial class PosePlanExecutionRuntime :
        IDisposable,
        ICharacterPoseCommittedDiagnosticsEventSink
    {
        readonly AnimancerComponent m_Animancer;
        readonly CharacterPresentationProjection m_Projection;
        readonly CharacterPoseRuntimeComposition m_Modules;
        readonly CharacterPoseTuningCoordinator m_Tuning;
        readonly CharacterPoseFrameCoordinator m_Frame;
        readonly CharacterPoseMotionMatchingCoordinator m_MotionMatching;

        CharacterPoseProgramRuntime m_ProgramRuntime => m_Modules.Program;
        CharacterPoseConstraintRuntime m_PoseConstraints =>
            m_Modules.Constraints;
        CharacterPoseSourceModule m_SourceModule => m_Modules.Source;
        CharacterFinalPosePublication m_FinalPublication =>
            m_Modules.Publication;
        CharacterPoseDiagnosticsRuntime m_Diagnostics =>
            m_Modules.Diagnostics;

        PoseInertializationNativeProgram m_InertializationPlan =>
            m_ProgramRuntime.Inertialization;

        bool m_Disposed;

        internal PosePlanExecutionRuntime(
            Guid runtimeInstanceId,
            AnimancerComponent animancer,
            CharacterAnimationRigBinding rigBinding,
            CharacterRootHierarchyBinding rootHierarchy,
            CharacterPresentationProjection projection,
            CharacterActionPlaybackRuntime actionPlayback,
            CharacterMotionMatchingPresentationModule motionMatching,
            AnimationSlotRuntime animationSlots,
            PresentationFrameWorkspace presentationWorkspace,
            CharacterFootPlacementModule footPlacement,
            bool managesGraphClock)
        {
            if (runtimeInstanceId == Guid.Empty)
            {
                throw new ArgumentException(
                    "Animation Presentation runtime identity is invalid.",
                    nameof(runtimeInstanceId));
            }
            m_Animancer = animancer ? animancer :
                throw new ArgumentNullException(nameof(animancer));
            m_Projection = projection ??
                throw new ArgumentNullException(nameof(projection));
            CharacterPoseRuntimeComposition modules = null;
            try
            {
                modules = CharacterPoseRuntimeCompositionFactory.Create(
                    animancer,
                    rigBinding,
                    rootHierarchy,
                    projection,
                    actionPlayback,
                    motionMatching,
                    animationSlots,
                    presentationWorkspace,
                    footPlacement,
                    managesGraphClock,
                    1,
                    this);
                m_Modules = modules;
                m_Tuning = new CharacterPoseTuningCoordinator(
                    m_ProgramRuntime,
                    m_SourceModule,
                    m_PoseConstraints,
                    1);
                m_Frame = new CharacterPoseFrameCoordinator(
                    animancer,
                    projection,
                    m_Modules,
                    m_Tuning);
                m_MotionMatching =
                    new CharacterPoseMotionMatchingCoordinator(
                        m_ProgramRuntime,
                        m_SourceModule,
                        m_Frame);
            }
            catch
            {
                modules?.Dispose();
                throw;
            }
        }

        internal bool HasDiagnosticsSnapshot => m_Diagnostics.HasCurrent;
        internal AnimationPresentationRuntimeSnapshot DiagnosticsSnapshot =>
            m_Diagnostics.Current;
        internal AnimationPresentationDiagnosticsInterest DiagnosticsInterest =>
            m_Diagnostics.Interest;
        internal ulong DiagnosticsNoInterestSkipCount =>
            m_Diagnostics.NoInterestSkipCount;
        internal bool HasFootPlacement => m_PoseConstraints.HasFootPlacement;
        internal bool MotionMatchingRuntimeEnabled =>
            m_MotionMatching.Enabled;
        internal bool AcceptsMotionMatchingTrajectoryIntent =>
            m_MotionMatching.AcceptsTrajectoryIntent;
        internal int ProviderSourceSampleCount =>
            m_ProgramRuntime.ProviderSourceSampleCount;
        internal void ResetFootPlacement(in CharacterFootPlacementReset reset) =>
            m_PoseConstraints.ResetFootPlacement(in reset);

        internal void RetargetFootPlacement(ulong resetSequence) =>
            m_PoseConstraints.RetargetFootPlacement(resetSequence);

        internal void DiscardPoseFrameAfterBarrier(
            CharacterPoseConstraintFrameLease constraintLease,
            CharacterFinalPosePublicationFrameLease publicationLease) =>
            m_Frame.DiscardAfterBarrier(
                constraintLease,
                publicationLease);

        internal string ApplyTuning(
            CharacterPoseTuningLayout layout,
            CharacterPoseTuningParameterBlock block,
            ulong candidateGeneration,
            bool resetOwnerState)
            =>
            m_Tuning.Apply(
                layout,
                block,
                candidateGeneration,
                resetOwnerState);

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
            m_Diagnostics.RecordNoInterestSkip();
        internal ulong FrameCompletionContext =>
            m_Frame.FrameCompletionContext;

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

        internal bool BeginMotionMatchingFrame(ulong frameIdentity)
        {
            RequireAlive();
            return m_MotionMatching.BeginFrame(frameIdentity);
        }

        internal void BeginActionSamplingFrame(
            ulong frameIdentity,
            ulong presentationFrame,
            bool captureDiagnostics)
        {
            RequireAlive();
            m_ProgramRuntime.BeginActionSamplingFrame(
                frameIdentity,
                presentationFrame,
                captureDiagnostics);
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

        internal void CommitMotionMatchingFrame(
            CharacterPoseProgramFrameLease lease)
        {
            RequireMutation(lease);
            m_MotionMatching.CommitFrame(lease);
        }

        internal void DiscardMotionMatchingFrame(ulong frameIdentity)
        {
            RequireAlive();
            m_MotionMatching.DiscardFrame(frameIdentity);
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
                CharacterPoseProgramFrameLease lease)
        {
            RequireMutation(lease);
            return m_ProgramRuntime.PrepareActionLifecycleFrame(
                lease);
        }

        internal void ProjectActionPresentationSamples(
            CharacterPoseProgramFrameLease lease,
            IReadOnlyList<ActionAnimationPlaybackLifecycleFrame> lifecycle,
            double presentationSampleTick,
            float presentationDeltaSeconds)
        {
            RequireMutation(lease);
            m_ProgramRuntime.ProjectActionPresentationSamples(
                lease,
                lifecycle,
                presentationSampleTick,
                presentationDeltaSeconds);
        }

        internal void ResolveActionPresentationFrames(
            CharacterPoseProgramFrameLease lease)
        {
            RequireMutation(lease);
            m_ProgramRuntime.ResolveActionPresentationFrames(
                lease);
        }

        internal void ValidateActionSamplingFrame(
            CharacterPoseProgramFrameLease lease)
        {
            RequireMutation(lease);
            m_ProgramRuntime.ValidateActionSamplingFrame(lease);
        }

        internal void CommitActionSamplingFrame(
            CharacterPoseProgramFrameLease lease)
        {
            RequireMutation(lease);
            m_ProgramRuntime.CommitActionSamplingFrame(lease);
        }

        internal void DiscardActionSamplingFrame(ulong frameIdentity)
        {
            RequireAlive();
            m_ProgramRuntime.DiscardActionSamplingFrame(frameIdentity);
        }

        internal void PublishActionSources(
            CharacterPoseProgramFrameLease lease,
            IDictionary<AnimationPlayerSourceSampleKey,
                AnimationResolvedPoseSourceSample> sourceSamples)
        {
            RequireMutation(lease);
            m_ProgramRuntime.PublishActionSources(
                lease,
                sourceSamples);
        }

        internal void CompleteActionReleaseProtocol(
            CharacterPoseProgramFrameLease lease)
        {
            RequireMutation(lease);
            m_ProgramRuntime.CompleteActionReleaseProtocol(
                lease);
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
        internal IReadOnlyList<ActionSlotSourceUsage> ActionSourceUsages =>
            m_ProgramRuntime.ActionSourceUsages;
        internal int ActionSamplingJournalCapacity =>
            m_SourceModule.ActionSamplingJournalCapacity;

        internal void ResetAnimationSlots()
        {
            RequireAlive();
            RequireNoOpenMutation();
            m_ProgramRuntime.ResetAnimationSlots();
        }

        internal void ResetActionSampling()
        {
            RequireAlive();
            RequireNoOpenMutation();
            m_ProgramRuntime.ResetActionSampling();
        }

        internal void ResetActionPlayback()
        {
            RequireAlive();
            RequireNoOpenMutation();
            m_ProgramRuntime.ResetActionPlayback();
        }

        internal void ResetPresentationWorkspace()
        {
            RequireAlive();
            RequireNoOpenMutation();
            m_ProgramRuntime.ResetPresentationWorkspace();
        }

        internal void BuildCommittedActionTimeSnapshots(
            FixedCapacityFrameBuffer<ActionPresentationTimeSnapshot>
                destination)
        {
            RequireAlive();
            RequireNoOpenMutation();
            m_ProgramRuntime.BuildCommittedActionTimeSnapshots(destination);
        }

        internal void CommitPresentationWorkspaceFrame(
            CharacterPoseProgramFrameLease lease)
        {
            RequireMutation(lease);
            m_ProgramRuntime.CommitPresentationWorkspaceFrame(lease);
        }

        internal void DiscardPresentationWorkspaceFrame(
            ulong frameIdentity)
        {
            RequireAlive();
            m_ProgramRuntime.DiscardPresentationWorkspaceFrame(
                frameIdentity);
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
            CharacterLinkedPoseRuntimeSession linkedPose,
            out CharacterPoseSourceFrameLease sourceLease,
            out CharacterPoseConstraintFrameLease constraintLease,
            out CharacterFinalPosePublicationFrameLease publicationLease,
            out bool captureFootIkDiagnostics)
        {
            captureFootIkDiagnostics = false;
            QueryFootDiagnosticEventInterest(
                ref captureFootIkDiagnostics);
            RequireAlive();
            return m_Frame.Begin(
                in lineage,
                diagnosticsInterest,
                captureFootIkDiagnostics,
                linkedPose,
                out sourceLease,
                out constraintLease,
                out publicationLease);
        }
        internal void SealFrame(
            CharacterPoseProgramFrameLease lease,
            CharacterPoseSourceFrameLease sourceLease,
            CharacterPoseConstraintFrameLease constraintLease,
            CharacterFinalPosePublicationFrameLease publicationLease)
        {
            RequireAlive();
            m_Frame.Seal(
                lease,
                sourceLease,
                constraintLease,
                publicationLease);
        }

        internal void ValidatePendingSeal(
            CharacterPoseProgramFrameLease lease,
            CharacterPoseSourceFrameLease sourceLease)
        {
            RequireAlive();
            m_Frame.ValidatePendingSeal(lease, sourceLease);
        }

        internal void DiscardPendingFrame(
            CharacterPoseProgramFrameLease lease,
            CharacterPoseSourceFrameLease sourceLease,
            CharacterPoseConstraintFrameLease constraintLease,
            CharacterFinalPosePublicationFrameLease publicationLease)
        {
            RequireAlive();
            m_Frame.Discard(
                lease,
                sourceLease,
                constraintLease,
                publicationLease);
        }

        internal ComposedAnimationPoseFrame
            FinalizeCommittedFrame(
                CharacterFinalPosePublicationFrameLease publicationLease)
        {
            RequireAlive();
            return m_Frame.FinalizeCommitted(publicationLease);
        }
        void ClearValidatedActionBackendAcknowledgements()
        {
            m_SourceModule.ClearValidatedReleaseAcknowledgements();
        }

        internal void SetPoseWatchInterests(Guid ownerId, IReadOnlyList<AnimationPoseWatchIdentity> interests) =>
            m_Diagnostics.SetPoseWatchInterests(ownerId, interests);

        internal void RemovePoseWatchInterests(Guid ownerId) =>
            m_Diagnostics.RemovePoseWatchInterests(ownerId);

        internal void SetDiagnosticsInterest(
            Guid ownerId,
            AnimationPresentationDiagnosticsInterest interest) =>
            m_Diagnostics.SetDiagnosticsInterest(ownerId, interest);

        internal void RemoveDiagnosticsInterest(Guid ownerId) =>
            m_Diagnostics.RemoveDiagnosticsInterest(ownerId);

        internal AnimationPresentationDiagnosticsInterest ResolveDiagnosticsInterest(
            AnimationPresentationDiagnosticsInterest transientInterest) =>
            m_Diagnostics.ResolveFrameInterest(transientInterest);

        internal void InvalidateDiagnosticsSnapshot() =>
            InvalidateDiagnostics();

        void InvalidateDiagnostics()
        {
            m_Diagnostics.Invalidate();
        }

        internal MotionMatchingFrameResolution ResolveMotionMatching(
            ulong presentationFrame,
            float presentationDeltaSeconds,
            in CharacterBodyPresentationFrame bodyFrame,
            out bool hasResolution)
        {
            RequireAlive();
            return m_MotionMatching.Resolve(
                presentationFrame,
                presentationDeltaSeconds,
                in bodyFrame,
                out hasResolution);
        }

        internal void PrepareMotionMatchingFrameCompletion(
            in MotionMatchingFrameResolution resolution,
            ulong poseCompletionIdentity)
        {
            RequireAlive();
            m_MotionMatching.PrepareCompletion(
                in resolution,
                poseCompletionIdentity);
        }

        internal void CompleteMotionMatchingFrame()
        {
            RequireAlive();
            m_MotionMatching.CompleteFrame();
        }

        internal bool TryCaptureMotionMatchingSearchReplay(
            string providerId,
            out MotionMatchingSearchReplayArtifact artifact)
        {
            RequireAlive();
            return m_MotionMatching.TryCaptureSearchReplay(
                providerId,
                out artifact);
        }

        internal void CaptureMotionMatchingTrajectoryIntent(
            CharacterPresentationTrajectoryIntent intent)
        {
            RequireAlive();
            m_MotionMatching.CaptureTrajectoryIntent(intent);
        }

        internal void CaptureMotionMatchingPreviewQuery(
            string providerId,
            MotionMatchingSearchReplayArtifact query)
        {
            RequireAlive();
            m_MotionMatching.CapturePreviewQuery(providerId, query);
        }

        internal void PublishMotionMatchingFrameDiagnostics(
            RuntimeDiagnosticsContext diagnostics,
            in MotionMatchingFrameResolution resolution) =>
            m_MotionMatching.PublishCommittedFrameDiagnostics(
                diagnostics,
                in resolution);

        internal void ResetMotionMatching(
            ulong resetSequence,
            MotionMatchingPresentationResetReason reason)
        {
            RequireAlive();
            RequireNoOpenMutation();
            m_MotionMatching.Reset(resetSequence, reason);
        }
        internal void BeginCommittedDiagnostics(
            AnimationPresentationDiagnosticsInterest interest,
            bool captureFootIk,
            CharacterLinkedPoseRuntimeSession linkedPose,
            in CharacterPoseSourceFrameResult sourceFrame,
            in CharacterPoseFrameExecutionResult executionResult)
        {
            RequireAlive();
            RequireNoOpenMutation();
            m_Diagnostics.BeginCommittedFrame(
                interest,
                captureFootIk,
                linkedPose,
                m_ProgramRuntime,
                m_SourceModule,
                m_PoseConstraints,
                m_FinalPublication,
                in sourceFrame,
                in executionResult);
        }

        partial void QueryFootDiagnosticEventInterest(
            ref bool interested);

        partial void PublishCommittedFootDiagnosticEvent(
            in CharacterPoseFrameLineage frame,
            in CharacterPoseConstraintResult constraintResult,
            in CharacterFinalPosePublicationResult publicationResult);

        void ICharacterPoseCommittedDiagnosticsEventSink
            .PublishCommittedFootDiagnosticEvent(
                in CharacterPoseFrameLineage frame,
                in CharacterPoseConstraintResult constraintResult,
                in CharacterFinalPosePublicationResult publicationResult) =>
            PublishCommittedFootDiagnosticEvent(
                in frame,
                in constraintResult,
                in publicationResult);

        internal CharacterFootIkCommittedCaptureViewLease
            PublishDiagnostics()
        {
            RequireAlive();
            RequireNoOpenMutation();
            return m_Diagnostics.Publish();
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
            m_Frame.Advance(
                presentationDeltaSeconds,
                in factFrame,
                in parameterFrame);
        }

        internal void FinalizePoseStateFrame(
            in CharacterPresentationFactFrame factFrame)
        {
            RequireAlive();
            m_Frame.FinalizePoseState(in factFrame);
        }

        internal CharacterPoseSourceDemand CreateSourceDemand(
            CharacterPoseProgramFrameLease programLease,
            CharacterPoseSourceFrameLease sourceLease,
            int actionSourceCount,
            int providerSourceCount)
        {
            RequireAlive();
            return m_Frame.CreateSourceDemand(
                programLease,
                sourceLease,
                actionSourceCount,
                providerSourceCount);
        }

        internal CharacterPoseProgramPrepared PrepareEvaluation(
            CharacterPoseSourceFrameLease sourceLease,
            CharacterFinalPosePublicationFrameLease publicationLease,
            in CharacterPoseSourceDemand sourceDemand,
            float presentationDeltaSeconds,
            IReadOnlyDictionary<AnimationPlayerSourceSampleKey,
                AnimationResolvedPoseSourceSample> actionSourceSamples,
            bool recordDiagnostics)
        {
            RequireAlive();
            return m_Frame.PrepareEvaluation(
                sourceLease,
                publicationLease,
                in sourceDemand,
                presentationDeltaSeconds,
                actionSourceSamples,
                recordDiagnostics);
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
            return m_Frame.ExecuteEvaluateBarrier(
                in bodyFrame,
                in factFrame,
                sourceLease,
                constraintLease,
                publicationLease,
                in prepared,
                enterEvaluateBarrier);
        }
        internal void Reset(PoseDiscontinuityResetReason reason)
        {
            RequireAlive();
            RequireNoOpenMutation();
            if (reason == PoseDiscontinuityResetReason.None)
                throw new ArgumentOutOfRangeException(nameof(reason));
            m_FinalPublication.Invalidate();
            m_Diagnostics.Reset();
            m_SourceModule.CancelReleaseDiagnostics();
            m_SourceModule.ClearActionSlotReleaseCompletions();
            m_ProgramRuntime.ClearSourceRetirements();
            ClearValidatedActionBackendAcknowledgements();
            m_ProgramRuntime.ClearMotionMatchingPoseCompletion();
            m_SourceModule.ClearActionBackendReleaseCompletions();
            m_ProgramRuntime.BeginReset();
            m_Frame.ResetState();
            m_PoseConstraints.ResetSolvers();
            ulong completionIdentity = m_Frame.NextCompletionIdentity();
            m_ProgramRuntime.ResetBlendState(completionIdentity);
            m_ProgramRuntime.ReleaseCompletedSources(completionIdentity);
            m_ProgramRuntime.ResetPoseState(reason);
            m_ProgramRuntime.ReleasePlayerSources();
            m_SourceModule.Clear();
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            m_Frame.ResetState();
            m_Modules.Dispose();
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

        void RequireMutation(
            CharacterPoseProgramFrameLease lease)
        {
            RequireAlive();
            m_Frame.RequireMutation(lease);
        }

        void RequireOpenMutation() => m_Frame.RequireOpenMutation();

        void RequireNoOpenMutation() => m_Frame.RequireNoOpenMutation();

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

        void RequireAlive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(PosePlanExecutionRuntime));
        }
    }
}
