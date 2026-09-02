using System;
using System.Collections.Generic;
using Animancer;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Pipeline.Animation.Presentation;
using ThirdPersonCharacter.Pipeline.Animation.Sources;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonSimulation;
using Unity.Profiling;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal sealed class CharacterPoseFrameCoordinator
    {
        static readonly ProfilerMarker PrepareMarker =
            new ProfilerMarker("ThirdPerson.Presentation.Animation.Prepare");
        static readonly ProfilerMarker PrepareWorkspaceMarker =
            new ProfilerMarker("ThirdPerson.Presentation.Animation.Prepare.Workspace");
        static readonly ProfilerMarker PrepareStackMarker =
            new ProfilerMarker("ThirdPerson.Presentation.Animation.Prepare.Stack");
        static readonly ProfilerMarker PrepareDirectMarker =
            new ProfilerMarker("ThirdPerson.Presentation.Animation.Prepare.Direct");
        static readonly ProfilerMarker PrepareClipMarker =
            new ProfilerMarker("ThirdPerson.Presentation.Animation.Prepare.Clip");
        static readonly ProfilerMarker PrepareBlendSpaceMarker =
            new ProfilerMarker("ThirdPerson.Presentation.Animation.Prepare.BlendSpace");
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
        readonly AnimancerComponent m_Animancer;
        readonly CharacterPresentationProjection m_Projection;
        readonly CharacterPoseProgramRuntime m_Program;
        readonly CharacterPoseSourceModule m_Source;
        readonly CharacterPoseConstraintRuntime m_Constraints;
        readonly CharacterFinalPosePublication m_Publication;
        readonly CharacterPoseDiagnosticsRuntime m_Diagnostics;
        readonly CharacterPoseTuningCoordinator m_Tuning;
        ulong m_CompletionIdentity = 1;
        ulong m_FrameCompletionContext;
        CharacterPoseProgramFrameLease m_ActiveFrameLease;
        bool m_CommitValidated;
        bool m_HasOpenFrame;
        AnimationPresentationFrameOutcome m_PendingOutcome;
        AnimationPresentationDiagnosticsInterest
            m_PendingDiagnosticsInterest;

        internal CharacterPoseFrameCoordinator(
            AnimancerComponent animancer,
            CharacterPresentationProjection projection,
            CharacterPoseRuntimeComposition modules,
            CharacterPoseTuningCoordinator tuning)
        {
            m_Animancer = animancer ? animancer :
                throw new ArgumentNullException(nameof(animancer));
            m_Projection = projection ??
                throw new ArgumentNullException(nameof(projection));
            if (modules == null)
                throw new ArgumentNullException(nameof(modules));
            m_Program = modules.Program;
            m_Source = modules.Source;
            m_Constraints = modules.Constraints;
            m_Publication = modules.Publication;
            m_Diagnostics = modules.Diagnostics;
            m_Tuning = tuning ?? throw new ArgumentNullException(nameof(tuning));
        }

        internal CharacterPoseProgramFrameLease ActiveLease =>
            m_ActiveFrameLease;
        internal ulong CompletionIdentity => m_CompletionIdentity;
        internal ulong FrameCompletionContext => m_FrameCompletionContext;
        internal bool HasOpenFrame => m_HasOpenFrame;
        internal bool CommitValidated => m_CommitValidated;
        internal AnimationPresentationFrameOutcome PendingOutcome =>
            m_PendingOutcome;

        internal CharacterPoseProgramFrameLease Begin(
            in CharacterPoseFrameLineage lineage,
            AnimationPresentationDiagnosticsInterest diagnosticsInterest,
            bool captureFootIkDiagnostics,
            CharacterLinkedPoseRuntimeSession linkedPose,
            out CharacterPoseSourceFrameLease sourceLease,
            out CharacterPoseConstraintFrameLease constraintLease,
            out CharacterFinalPosePublicationFrameLease publicationLease)
        {
            sourceLease = default;
            constraintLease = default;
            publicationLease = default;
            var programLease = new CharacterPoseProgramFrameLease(in lineage);
            if (linkedPose == null)
                throw new ArgumentNullException(nameof(linkedPose));
            if (m_ActiveFrameLease.IsValid)
            {
                throw new InvalidOperationException(
                    "Pose Plan frame mutation is already open.");
            }
            if (m_Program.HasPreparedEvaluation)
            {
                throw new InvalidOperationException(
                    "Pose Program prepared state from the previous frame was not consumed.");
            }
            if (m_CommitValidated)
            {
                throw new InvalidOperationException(
                    "Pose Plan committed-frame finalization is still pending.");
            }
            if (m_Program.HasPreparedStandaloneSourceRetirement)
            {
                throw new InvalidOperationException(
                    "Pose Plan standalone source releases from the committed frame were not finalized.");
            }
            if (m_Source.ReleaseAcknowledgementsValidated)
            {
                throw new InvalidOperationException(
                    "Pose Plan action backend acknowledgements from the previous frame were not applied.");
            }
            if (m_Program.HasPreparedMotionMatchingPoseCompletion)
            {
                throw new InvalidOperationException(
                    "Pose Plan Motion Matching completion from the previous frame was not consumed.");
            }
            bool constraintsOpen = false;
            bool sourceOpen = false;
            bool publicationOpen = false;
            try
            {
                m_Tuning.Committed.RequireGeneration(
                    lineage.TuningGeneration);
                constraintLease = m_Constraints.BeginFrame(
                    in lineage,
                    diagnosticsInterest,
                    captureFootIkDiagnostics);
                constraintsOpen = true;
                m_Program.BeginSourceRetirementFrame();
                sourceLease = m_Source.BeginFrame(in lineage);
                sourceOpen = true;
                publicationLease = m_Publication.BeginFrame(
                    in lineage,
                    diagnosticsInterest,
                    captureFootIkDiagnostics);
                publicationOpen = true;
                m_PendingOutcome = AnimationPresentationFrameOutcome.None;
                m_PendingDiagnosticsInterest = diagnosticsInterest;
                m_Program.BeginFrame(programLease);
                m_Program.BeginActorStateFrame(
                    programLease,
                    linkedPose,
                    m_Projection.LinkedPose.Groups,
                    m_CompletionIdentity);
                m_HasOpenFrame = true;
                m_ActiveFrameLease = programLease;
                return m_ActiveFrameLease;
            }
            catch
            {
                if (publicationOpen)
                    m_Publication.DiscardPending(publicationLease);
                if (constraintsOpen)
                    m_Constraints.DiscardFrame(constraintLease);
                if (m_Program.HasOpenFrame)
                    m_Program.DiscardFrame(programLease);
                if (sourceOpen)
                    m_Source.DiscardFrame(sourceLease);
                m_Program.CompleteSourceRetirementFrame();
                m_Program.ClearLinkedPoseFrameSelection();
                throw;
            }
        }

        internal void ValidatePendingSeal(
            CharacterPoseProgramFrameLease lease,
            CharacterPoseSourceFrameLease sourceLease)
        {
            RequireMutation(lease);
            m_Program.ValidateSourceRetirements(lease, sourceLease);
            m_CommitValidated = true;
        }

        internal void Advance(
            float presentationDeltaSeconds,
            in CharacterPresentationFactFrame factFrame,
            in CharacterPresentationProgramParameterFrame parameterFrame)
        {
            RequireOpenMutation();
            if (!float.IsFinite(presentationDeltaSeconds) ||
                presentationDeltaSeconds < 0f || !factFrame.IsValid ||
                !parameterFrame.IsValid)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(presentationDeltaSeconds));
            }
            CharacterPoseSourceTuningView sourceTuning =
                m_Source.RequireTuning(
                    m_ActiveFrameLease.Lineage.TuningGeneration);
            m_Program.Advance(
                m_ActiveFrameLease,
                presentationDeltaSeconds,
                in factFrame,
                in parameterFrame,
                in sourceTuning);
        }

        internal void FinalizePoseState(
            in CharacterPresentationFactFrame factFrame)
        {
            RequireOpenMutation();
            if (!factFrame.IsValid)
            {
                throw new ArgumentException(
                    "Pose State frame finalization is invalid.",
                    nameof(factFrame));
            }
            m_Program.FinalizePoseStateFrame(
                m_ActiveFrameLease,
                in factFrame);
        }

        internal CharacterPoseSourceDemand CreateSourceDemand(
            CharacterPoseProgramFrameLease programLease,
            CharacterPoseSourceFrameLease sourceLease,
            int actionSourceCount,
            int providerSourceCount)
        {
            RequireMutation(programLease);
            m_Source.RequirePendingOpen(sourceLease);
            CharacterPoseFrameLineage openLineage = programLease.Lineage;
            if (!openLineage.IsOpenValid ||
                openLineage.CompletionIdentity != 0 ||
                !sourceLease.Matches(openLineage))
            {
                throw new ArgumentException(
                    "Pose Program source demand lineage is invalid.",
                    nameof(openLineage));
            }
            ulong completionIdentity = OpenCompletion();
            CharacterPoseFrameLineage lineage =
                openLineage.WithCompletion(completionIdentity);
            CharacterPoseSourcePreparationView preparations =
                m_Program.BeginSourceDemand(
                    programLease,
                    completionIdentity);
            var demand = new CharacterPoseSourceDemand(
                in lineage,
                in preparations,
                m_Program.ProviderDemands,
                actionSourceCount,
                providerSourceCount);
            m_Program.BindSourceDemand(programLease, in demand);
            m_Source.BindDemand(sourceLease, in demand);
            return demand;
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
            RequireOpenMutation();
            CharacterPoseSourceDemand pendingDemand =
                m_Source.RequireDemand(sourceLease);
            CharacterPoseSourceDemand programDemand =
                m_Program.RequireSourceDemand(
                    m_ActiveFrameLease,
                    in sourceDemand);
            CharacterPoseSourcePreparationView sourcePreparations =
                sourceDemand.Preparations;
            CharacterPoseSourcePreparationView pendingPreparations =
                pendingDemand.Preparations;
            if (m_Program.HasPreparedEvaluation)
            {
                throw new InvalidOperationException(
                    "Pose Program evaluation is already prepared for the active frame.");
            }
            if (!sourceDemand.IsValid ||
                sourceDemand.Lineage != pendingDemand.Lineage ||
                sourceDemand.Lineage != programDemand.Lineage ||
                !sourcePreparations.Matches(in pendingPreparations) ||
                sourceDemand.ActionSourceCount !=
                    pendingDemand.ActionSourceCount ||
                sourceDemand.ProviderSourceCount !=
                    pendingDemand.ProviderSourceCount ||
                !ReferenceEquals(
                    sourceDemand.ProviderDemands,
                    pendingDemand.ProviderDemands) ||
                sourceDemand.Lineage.FrameIdentity !=
                    m_ActiveFrameLease.FrameIdentity ||
                sourceDemand.Lineage.CompletionIdentity !=
                    m_FrameCompletionContext)
            {
                throw new ArgumentException(
                    "Pose Program source demand is not active.",
                    nameof(sourceDemand));
            }
            if (!float.IsFinite(presentationDeltaSeconds) ||
                presentationDeltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(presentationDeltaSeconds));
            }
            if (actionSourceSamples == null)
                throw new ArgumentNullException(nameof(actionSourceSamples));
            ulong completionIdentity =
                sourceDemand.Lineage.CompletionIdentity;
            using (PrepareMarker.Auto())
            {
                m_Source.BeginReleaseDiagnostics(recordDiagnostics);
                m_Source.ClearActionSlotReleaseCompletions();
                using (PrepareWorkspaceMarker.Auto())
                {
                    m_Program.BeginSourceEvaluation(
                        m_ActiveFrameLease,
                        completionIdentity);
                }
                using (PrepareStackMarker.Auto())
                {
                    m_Program.PrepareStackSources(
                        m_ActiveFrameLease,
                        sourceLease,
                        in sourcePreparations,
                        presentationDeltaSeconds,
                        actionSourceSamples,
                        m_Program.ProviderSourceSamples);
                }
                using (PrepareDirectMarker.Auto())
                {
                    m_Program.PrepareDirectSources(
                        m_ActiveFrameLease,
                        sourceLease,
                        in sourcePreparations,
                        presentationDeltaSeconds,
                        m_Program.ProviderSourceSamples);
                }
                using (PrepareClipMarker.Auto())
                {
                    CharacterPoseSourceTuningView sourceTuning =
                        m_Source.RequireTuning(
                            sourceDemand.Lineage.TuningGeneration);
                    m_Program.PrepareSequenceSources(
                        m_ActiveFrameLease,
                        sourceLease,
                        in sourcePreparations,
                        presentationDeltaSeconds,
                        in sourceTuning);
                }
                using (PrepareBlendSpaceMarker.Auto())
                {
                    m_Program.PrepareBlendSpaceSources(
                        m_ActiveFrameLease,
                        sourceLease,
                        in sourcePreparations,
                        presentationDeltaSeconds);
                }
            }
            CharacterPoseSourcePreparedResources preparedSources =
                m_Source.RequirePreparedResources(sourceLease);
            CharacterFinalPosePublicationOutputBinding finalOutput;
            using (ValidateMarker.Auto())
            {
                m_Program.PrepareEvaluationJobsAndRetirements(
                    m_ActiveFrameLease,
                    in preparedSources,
                    completionIdentity);
                finalOutput = m_Publication.BindProgramOutput(
                    publicationLease);
                m_Program.BindEvaluationExecution(
                    m_ActiveFrameLease,
                    sourceDemand.Lineage,
                    in finalOutput,
                    recordDiagnostics);
                m_Publication.ValidateWriterBeforeEvaluate(in finalOutput);
            }
            CharacterPoseSourceFrameResult sourceFrame =
                m_Source.PrepareFrameResult(
                    sourceLease,
                    in preparedSources,
                    actionSourceSamples,
                    m_Program.ProviderSourceSamples);
            if (!sourceFrame.IsReady)
            {
                throw new InvalidOperationException(
                    $"Pose source frame ended as '{sourceFrame.Outcome}'.");
            }
            var prepared = new CharacterPoseProgramPrepared(in sourceFrame);
            m_Program.PrepareEvaluation(
                m_ActiveFrameLease,
                in prepared,
                presentationDeltaSeconds);
            return prepared;
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
            RequireOpenMutation();
            ActorId actorId = prepared.Lineage.ActorId;
            ulong renderFrame = prepared.Lineage.PresentationFrame;
            if (!actorId.IsValid)
                throw new ArgumentException("Pose Plan Actor identity is invalid.", nameof(actorId));
            if (renderFrame == 0)
                throw new ArgumentOutOfRangeException(nameof(renderFrame));
            if (!bodyFrame.IsValid)
                throw new ArgumentException("Pose Plan Body frame is invalid.", nameof(bodyFrame));
            if (!factFrame.IsValid)
            {
                throw new ArgumentException(
                    "Pose Plan Presentation Fact frame is invalid.",
                    nameof(factFrame));
            }
            if (!prepared.IsValid ||
                prepared.Lineage.CompletionIdentity !=
                    m_FrameCompletionContext ||
                !m_Program.HasPendingEvaluationFrame ||
                m_Program.PendingEvaluationCompletionIdentity !=
                    prepared.Lineage.CompletionIdentity)
            {
                throw new ArgumentException(
                    "Pose Plan prepared evaluation is not the active Pending frame.",
                    nameof(prepared));
            }
            if (enterEvaluateBarrier == null)
                throw new ArgumentNullException(nameof(enterEvaluateBarrier));
            ulong completionIdentity = prepared.Lineage.CompletionIdentity;
            float presentationDeltaSeconds =
                m_Program.RequirePreparedEvaluationDeltaSeconds(
                    m_ActiveFrameLease,
                    in prepared);
            enterEvaluateBarrier();
            m_Source.EnterEvaluateBarrier(sourceLease);
            using (GraphEvaluateMarker.Auto())
                m_Animancer.Evaluate(presentationDeltaSeconds);
            CharacterPoseProgramOutputResult programOutput;
            using (PoseGraphExecuteMarker.Auto())
            {
                programOutput = m_Program.CompleteEvaluation(
                    m_ActiveFrameLease,
                    in prepared,
                    in bodyFrame,
                    in factFrame);
            }
            CharacterPoseFrameLineage completedLineage = prepared.Lineage;
            CharacterPoseProgramResult programResult = CreateProgramResult(
                in completedLineage,
                in programOutput);
            CharacterPoseConstraintResult constraintResult =
                m_Constraints.CompleteFrame(
                    constraintLease,
                    in completedLineage,
                    programResult.OutputAvailability,
                    programResult.OutputInvalidReason,
                    programResult.GraphInvalidReason);
            CharacterFinalPosePublicationResult publicationResult;
            using (FinalWriteMarker.Auto())
            {
                publicationResult = m_Publication.PreparePending(
                    publicationLease,
                    in completedLineage,
                    in programResult,
                    in constraintResult,
                    in programOutput);
                m_PendingOutcome = publicationResult.Outcome;
                m_Publication.WritePhysicalPose(publicationLease);
            }
            var executionResult = new CharacterPoseFrameExecutionResult(
                in programResult,
                in constraintResult,
                in publicationResult);
            if (!executionResult.IsValid)
            {
                throw new InvalidOperationException(
                    "Pose frame typed execution results are inconsistent.");
            }
            if (m_PendingOutcome !=
                AnimationPresentationFrameOutcome.Committed)
            {
                return executionResult;
            }
            using (SealMarker.Auto())
            {
                m_Program.CompleteNodeEvaluation(
                    m_ActiveFrameLease,
                    completionIdentity);
                if (m_PendingDiagnosticsInterest !=
                    AnimationPresentationDiagnosticsInterest.None)
                {
                    ComposedAnimationPoseFrame pendingFrame =
                        m_Publication.RequirePendingFrame(publicationLease);
                    m_Program.PreparePendingDiagnostics(
                        m_ActiveFrameLease,
                        in programResult,
                        in programOutput,
                        in pendingFrame,
                        m_PendingDiagnosticsInterest);
                }
            }
            return executionResult;
        }

        static CharacterPoseProgramResult CreateProgramResult(
            in CharacterPoseFrameLineage lineage,
            in CharacterPoseProgramOutputResult output)
        {
            if (!output.IsValid || output.Lineage != lineage)
            {
                throw new InvalidOperationException(
                    "Pose Program output result is inconsistent.");
            }
            bool completed =
                output.Availability == AnimationPoseAvailability.Pose &&
                output.OutputInvalidReason ==
                    AnimationPoseNativeInvalidReason.None &&
                output.GraphInvalidReason ==
                    AnimationPoseNativeInvalidReason.None &&
                output.InvalidOperationIndex == -1;
            return new CharacterPoseProgramResult(
                in lineage,
                completed
                    ? AnimationPresentationFrameOutcome.Committed
                    : AnimationPresentationFrameOutcome.TypedInvalid,
                output.Availability,
                output.OutputInvalidReason,
                output.GraphInvalidReason,
                output.InvalidOperationIndex);
        }

        internal void Seal(
            CharacterPoseProgramFrameLease lease,
            CharacterPoseSourceFrameLease sourceLease,
            CharacterPoseConstraintFrameLease constraintLease,
            CharacterFinalPosePublicationFrameLease publicationLease)
        {
            RequireMutation(lease);
            m_Source.RequirePendingReady(sourceLease);
            m_Publication.ValidatePendingSeal(publicationLease);
            if (!m_CommitValidated)
            {
                throw new InvalidOperationException(
                    "Pose Plan frame mutation was not validated for commit.");
            }
            if (m_PendingOutcome !=
                    AnimationPresentationFrameOutcome.Committed ||
                !m_Program.HasPendingCompletedEvaluationFrame ||
                m_Program.PendingCompletedEvaluationCompletionIdentity == 0)
            {
                throw new InvalidOperationException(
                    "Pose Plan frame has no completed Native page to commit.");
            }
            m_Program.CommitEvaluationFrame(
                lease,
                m_Program.PendingCompletedEvaluationCompletionIdentity);
            m_Program.CommitFrame(lease);
            m_Source.CommitFrame(sourceLease);
            m_Program.CommitActorStateFrame(lease);
            m_Constraints.SealFrame(constraintLease);
            m_HasOpenFrame = false;
            m_ActiveFrameLease = default;
            m_Program.CompleteSourceRetirementFrame();
            m_Program.ClearLinkedPoseFrameSelection();
        }

        internal void Discard(
            CharacterPoseProgramFrameLease lease,
            CharacterPoseSourceFrameLease sourceLease,
            CharacterPoseConstraintFrameLease constraintLease,
            CharacterFinalPosePublicationFrameLease publicationLease)
        {
            RequireMutation(lease);
            m_Source.RequirePendingOpen(sourceLease);
            Exception failure = null;
            DiscardStep(
                () => m_Constraints.DiscardFrame(constraintLease),
                ref failure);
            DiscardStep(
                () => m_Source.DiscardFrame(sourceLease),
                ref failure);
            DiscardStep(
                () => m_Program.DiscardActorNodeFrames(lease),
                ref failure);
            m_Source.ClearActionSlotReleaseCompletions();
            m_Source.CancelReleaseDiagnostics();
            m_Source.ClearUsage();
            DiscardStep(
                () => m_Program.DiscardRootOrientationWarpFrames(lease),
                ref failure);
            if (m_Program.HasOpenFrame)
            {
                DiscardStep(
                    () => m_Program.DiscardFrame(lease),
                    ref failure);
            }
            DiscardStep(
                () => m_Publication.DiscardPending(publicationLease),
                ref failure);
            DiscardStep(m_Diagnostics.DiscardPendingFrame, ref failure);
            DiscardStep(m_Program.DiscardSourceRetirementFrame, ref failure);
            m_Program.ClearStandaloneSourceRetirements();
            m_Source.ClearValidatedReleaseAcknowledgements();
            m_Program.ClearMotionMatchingPoseCompletion();
            m_Program.ClearSourceDemand();
            m_HasOpenFrame = false;
            m_ActiveFrameLease = default;
            m_CommitValidated = false;
            m_PendingOutcome = AnimationPresentationFrameOutcome.None;
            m_PendingDiagnosticsInterest =
                AnimationPresentationDiagnosticsInterest.None;
            m_Source.CancelReleaseDiagnostics();
            m_Program.ClearLinkedPoseFrameSelection();
            if (failure != null)
            {
                throw new AggregateException(
                    "Pose Plan Pending discard failed.",
                    failure);
            }
        }

        internal void DiscardAfterBarrier(
            CharacterPoseConstraintFrameLease constraintLease,
            CharacterFinalPosePublicationFrameLease publicationLease)
        {
            Exception failure = null;
            DiscardStep(
                () => m_Publication.DiscardPending(publicationLease),
                ref failure);
            DiscardStep(
                () => m_Constraints.DiscardFrame(constraintLease),
                ref failure);
            if (failure != null)
            {
                throw new AggregateException(
                    "Pose frame post-barrier Pending discard failed.",
                    failure);
            }
        }

        internal ComposedAnimationPoseFrame FinalizeCommitted(
            CharacterFinalPosePublicationFrameLease publicationLease,
            in CharacterPoseSourceFrameResult sourceFrame)
        {
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
            if (!sourceFrame.IsReady ||
                !publicationLease.Matches(sourceFrame.Lineage))
            {
                throw new InvalidOperationException(
                    "Pose Source committed diagnostics lineage is invalid.");
            }
            m_Program.FinalizeCommittedSourceRetirements(
                m_CompletionIdentity);
            if (m_PendingDiagnosticsInterest !=
                AnimationPresentationDiagnosticsInterest.None)
            {
                m_Source.FreezeCommittedDiagnostics(in sourceFrame);
            }
            ComposedAnimationPoseFrame result =
                m_Publication.CommitPending(publicationLease);
            m_CommitValidated = false;
            m_PendingOutcome = AnimationPresentationFrameOutcome.None;
            m_PendingDiagnosticsInterest =
                AnimationPresentationDiagnosticsInterest.None;
            return result;
        }

        internal ulong OpenCompletion()
        {
            ulong completionIdentity = NextCompletionIdentity();
            m_FrameCompletionContext = completionIdentity;
            return completionIdentity;
        }

        internal ulong NextCompletionIdentity()
        {
            if (m_CompletionIdentity == ulong.MaxValue)
            {
                throw new InvalidOperationException(
                    "Animation Pose completion identity was exhausted.");
            }
            m_CompletionIdentity++;
            return m_CompletionIdentity;
        }

        internal void SetPendingOutcome(
            AnimationPresentationFrameOutcome outcome) =>
            m_PendingOutcome = outcome;

        internal void ResetState()
        {
            m_PendingOutcome = AnimationPresentationFrameOutcome.None;
            m_CommitValidated = false;
            m_PendingDiagnosticsInterest =
                AnimationPresentationDiagnosticsInterest.None;
        }

        internal void RequireMutation(CharacterPoseProgramFrameLease lease)
        {
            if (!lease.IsValid || !m_ActiveFrameLease.IsValid ||
                lease.Lineage != m_ActiveFrameLease.Lineage ||
                !m_HasOpenFrame)
            {
                throw new InvalidOperationException(
                    "Pose Plan frame mutation lease is invalid.");
            }
        }

        internal void RequireOpenMutation()
        {
            if (!m_ActiveFrameLease.IsValid || !m_HasOpenFrame)
            {
                throw new InvalidOperationException(
                    "Pose Plan mutation must be open before frame evaluation.");
            }
        }

        internal void RequireNoOpenMutation()
        {
            if (m_ActiveFrameLease.IsValid || m_HasOpenFrame)
            {
                throw new InvalidOperationException(
                    "Pose Plan frame mutation must close before reset.");
            }
        }

        static void DiscardStep(Action action, ref Exception failure)
        {
            try
            {
                action();
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
