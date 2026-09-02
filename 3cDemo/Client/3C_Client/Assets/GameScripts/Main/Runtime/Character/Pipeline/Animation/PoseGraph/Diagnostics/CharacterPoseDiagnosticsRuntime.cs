using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Animation.Lifecycle;
using ThirdPersonCharacter.Pipeline.Animation.Presentation;
using ThirdPersonCharacter.Pipeline.Animation.Sources;
using ThirdPersonCharacter.Pipeline.Presentation;
using Unity.Profiling;

namespace ThirdPersonCharacter.Pipeline.Animation.Diagnostics
{
    internal sealed class CharacterPoseDiagnosticsRuntime : IDisposable
    {
        static readonly ProfilerMarker DiagnosticsMarker =
            new ProfilerMarker(
                "ThirdPerson.Presentation.Animation.Diagnostics");

        readonly AnimationPresentationRuntimeSnapshotPublisher m_Publisher;
        readonly CharacterFootIkCommittedCaptureViewProjector
            m_FootIkProjector;
        readonly CharacterPoseActorCommittedDiagnosticsProjector
            m_ActorProjector;
        readonly CharacterPoseProgramCommittedDiagnosticsProjector
            m_ProgramProjector;
        readonly CharacterPoseCommittedDiagnosticsEventPublisher
            m_EventPublisher;
        bool m_Disposed;

        internal CharacterPoseDiagnosticsRuntime(
            CharacterPresentationProjection projection,
            in AnimationPoseNativeAggregateLayout initialLayout,
            int physicalSourceCapacity,
            CharacterPoseProgramExecutionView executionView,
            CharacterPoseCommittedDiagnosticsEventPublisher eventPublisher)
        {
            m_EventPublisher = eventPublisher ??
                throw new ArgumentNullException(nameof(eventPublisher));
            m_Publisher = new AnimationPresentationRuntimeSnapshotPublisher(
                projection ?? throw new ArgumentNullException(nameof(projection)),
                in initialLayout,
                physicalSourceCapacity);
            m_FootIkProjector =
                new CharacterFootIkCommittedCaptureViewProjector();
            m_ActorProjector =
                new CharacterPoseActorCommittedDiagnosticsProjector(
                    projection,
                    in initialLayout);
            m_ProgramProjector =
                new CharacterPoseProgramCommittedDiagnosticsProjector(
                    executionView ??
                    throw new ArgumentNullException(nameof(executionView)));
        }

        internal bool HasCurrent => m_Publisher.HasCurrent;
        internal AnimationPresentationRuntimeSnapshot Current =>
            m_Publisher.Current;
        internal AnimationPresentationDiagnosticsInterest Interest =>
            m_Publisher.Interest;
        internal ulong NoInterestSkipCount =>
            m_Publisher.NoInterestSkipCount;
        internal bool HasFootCaptureInterest =>
            m_EventPublisher.HasFootCaptureInterest;

        internal void BeginFrame()
        {
            RequireAlive();
            m_ActorProjector.BeginFrame();
        }

        internal void SetPoseWatchInterests(
            Guid ownerId,
            IReadOnlyList<AnimationPoseWatchIdentity> interests)
        {
            RequireAlive();
            m_Publisher.SetPoseWatchInterests(ownerId, interests);
        }

        internal void RemovePoseWatchInterests(Guid ownerId)
        {
            RequireAlive();
            m_Publisher.RemovePoseWatchInterests(ownerId);
        }

        internal void SetDiagnosticsInterest(
            Guid ownerId,
            AnimationPresentationDiagnosticsInterest interest)
        {
            RequireAlive();
            m_Publisher.SetDiagnosticsInterest(ownerId, interest);
        }

        internal void RemoveDiagnosticsInterest(Guid ownerId)
        {
            RequireAlive();
            m_Publisher.RemoveDiagnosticsInterest(ownerId);
        }

        internal AnimationPresentationDiagnosticsInterest
            ResolveFrameInterest(
                AnimationPresentationDiagnosticsInterest transientInterest)
        {
            RequireAlive();
            return m_Publisher.ResolveFrameInterest(transientInterest);
        }

        internal void RecordNoInterestSkip()
        {
            RequireAlive();
            m_Publisher.RecordNoInterestSkip();
        }

        internal void DiscardPendingFrame()
        {
            RequireAlive();
            Exception failure = null;
            DiscardStep(m_Publisher.DiscardPendingFrame, ref failure);
            DiscardStep(m_FootIkProjector.DiscardPendingFrame, ref failure);
            if (failure != null)
                throw failure;
        }

        internal void BeginCommittedFrame(
            AnimationPresentationDiagnosticsInterest interest,
            bool captureFootIk,
            CharacterLinkedPoseRuntimeSession linkedPose,
            CharacterPoseProgramRuntime program,
            CharacterPoseSourceModule source,
            CharacterPoseConstraintRuntime constraints,
            CharacterFinalPosePublication publication,
            in CharacterPoseSourceFrameResult sourceFrame,
            in CharacterPoseFrameExecutionResult executionResult)
        {
            RequireAlive();
            if (linkedPose == null)
                throw new ArgumentNullException(nameof(linkedPose));
            if (program == null)
                throw new ArgumentNullException(nameof(program));
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (constraints == null)
                throw new ArgumentNullException(nameof(constraints));
            if (publication == null)
                throw new ArgumentNullException(nameof(publication));
            bool publishRuntimeSnapshot =
                interest != AnimationPresentationDiagnosticsInterest.None;
            if (!publishRuntimeSnapshot && !captureFootIk)
                return;
            if (captureFootIk && !constraints.HasFootPlacement)
            {
                throw new InvalidOperationException(
                    "Foot IK capture requires the compiled Foot Placement capability.");
            }
            if (!sourceFrame.IsReady ||
                sourceFrame.Lineage != executionResult.Lineage ||
                !executionResult.IsPublished ||
                !program.HasCommittedEvaluationFrame ||
                program.CommittedEvaluationCompletionIdentity == 0 ||
                executionResult.Lineage.CompletionIdentity !=
                program.CommittedEvaluationCompletionIdentity)
            {
                throw new InvalidOperationException(
                    "Animation diagnostics requires a successfully sealed committed Pose page.");
            }
            using (DiagnosticsMarker.Auto())
            {
                CharacterPoseProgramResult programResult =
                    executionResult.Program;
                CharacterPoseConstraintResult constraintResult =
                    executionResult.Constraint;
                CharacterFinalPosePublicationResult publicationResult =
                    executionResult.Publication;
                CharacterPoseFrameLineage frame = executionResult.Lineage;
                if (constraintResult.Lineage != frame ||
                    publicationResult.Lineage != frame)
                {
                    throw new InvalidOperationException(
                        "Animation diagnostics committed lineage is inconsistent.");
                }
                if (captureFootIk)
                {
                    m_EventPublisher.PublishCommittedFootDiagnosticEvent(
                        in frame,
                        in constraintResult,
                        in publicationResult,
                        program,
                        constraints,
                        publication);
                }
                if (!publishRuntimeSnapshot)
                    return;
                bool requiresFoot = constraints.HasFootPlacement &&
                    CharacterPoseConstraintRuntime.RequiresFootDiagnostics(
                        interest);
                bool requiresSolver = CharacterPoseConstraintRuntime
                    .RequiresFullBodyIkDiagnostics(interest);
                bool requiresPhysical = CharacterFinalPosePublication
                    .RequiresPhysicalDiagnostics(interest);
                CharacterPoseActorCommittedDiagnosticsView actorDiagnostics =
                    m_ActorProjector.Capture(
                        in programResult,
                        program.Stacks,
                        program.Routes,
                        program.PoseStateSources.StateMachines,
                        program.Inertialization,
                        program.PoseStateSources.ClipPlayers,
                        program.PoseStateSources.BlendSpacePlayers,
                        program.RootOrientationWarps,
                        interest,
                        false);
                CharacterPoseConstraintCommittedDiagnosticsView
                    constraintDiagnostics =
                        constraints.CaptureCommittedDiagnostics(
                            in constraintResult,
                            interest,
                            default);
                CharacterFinalPoseCommittedDiagnosticsView
                    publicationDiagnostics =
                        publication.CaptureCommittedDiagnostics(
                            in publicationResult);
                AnimationPhysicalBoneWriteDiagnostics physicalWrite =
                    publicationDiagnostics.PhysicalWrite;
                CharacterFootLandingPredictionDiagnostics footDiagnostics =
                    constraintDiagnostics.FootLandingPrediction;
                CharacterFullBodyIkSolverDiagnostics solverDiagnostics =
                    constraintDiagnostics.Solver;
                if (!actorDiagnostics.IsValid ||
                    actorDiagnostics.Result.Lineage != frame ||
                    !constraintDiagnostics.IsValid ||
                    constraintDiagnostics.Result.Lineage != frame ||
                    !publicationDiagnostics.IsValid ||
                    publicationDiagnostics.Result.Lineage != frame ||
                    requiresFoot &&
                    (!footDiagnostics.IsCompleted ||
                     footDiagnostics.FrameSequence != frame.PresentationFrame ||
                     footDiagnostics.CompletionIdentity !=
                     program.CommittedEvaluationCompletionIdentity) ||
                    requiresSolver &&
                    solverDiagnostics.OutputCompletionIdentity !=
                    program.CommittedEvaluationCompletionIdentity ||
                    requiresPhysical &&
                    (!physicalWrite.IsAvailable ||
                     physicalWrite.CompletionIdentity !=
                     program.CommittedEvaluationCompletionIdentity))
                {
                    throw new InvalidOperationException(
                        "Animation diagnostics committed lineage is inconsistent.");
                }
                bool includeFootBasicState =
                    (interest &
                     (AnimationPresentationDiagnosticsInterest.LiveState |
                      AnimationPresentationDiagnosticsInterest.Capture)) != 0;
                m_FootIkProjector.BeginFrame(
                    in executionResult,
                    in actorDiagnostics,
                    in constraintDiagnostics,
                    in publicationDiagnostics,
                    includeFootBasicState);
                try
                {
                    CharacterPoseSourceCommittedDiagnosticsView
                        sourceDiagnostics =
                            source.CaptureCommittedDiagnostics(
                                in sourceFrame);
                    CharacterPoseProgramCommittedDiagnosticsView
                        programDiagnostics =
                            program.CaptureCommittedDiagnostics(
                                m_ProgramProjector,
                                in programResult,
                                in publicationDiagnostics,
                                interest);
                    CharacterLinkedPoseCommittedDiagnosticsView
                        linkedPoseDiagnostics =
                            linkedPose.CaptureCommittedDiagnostics(
                                in programResult);
                    if (!sourceDiagnostics.IsValid ||
                        sourceDiagnostics.Result.Lineage != frame ||
                        !programDiagnostics.IsValid ||
                        programDiagnostics.Result.Lineage != frame ||
                        !linkedPoseDiagnostics.IsValid ||
                        linkedPoseDiagnostics.Result.Lineage != frame)
                    {
                        throw new InvalidOperationException(
                            "Animation runtime Snapshot lineage is inconsistent.");
                    }
                    m_Publisher.BeginFrame(
                        in executionResult,
                        in sourceDiagnostics,
                        in programDiagnostics,
                        in linkedPoseDiagnostics,
                        in actorDiagnostics,
                        in constraintDiagnostics,
                        in publicationDiagnostics,
                        interest);
                }
                catch
                {
                    m_FootIkProjector.DiscardPendingFrame();
                    throw;
                }
            }
        }

        internal CharacterFootIkCommittedCaptureViewLease Publish()
        {
            RequireAlive();
            bool publishRuntimeSnapshot = m_Publisher.HasPendingFrame;
            bool publishFootIkView = m_FootIkProjector.HasPendingFrame;
            if (!publishRuntimeSnapshot && !publishFootIkView)
                return default;
            if (publishRuntimeSnapshot && !publishFootIkView)
            {
                throw new InvalidOperationException(
                    "Foot IK committed capture view is not pending.");
            }
            CharacterFootIkCommittedCaptureViewLease footIkCaptureView;
            using (DiagnosticsMarker.Auto())
            {
                footIkCaptureView = m_FootIkProjector.Publish();
                if (publishRuntimeSnapshot)
                {
                    try
                    {
                        AnimationPresentationRuntimeSnapshot snapshot =
                            m_Publisher.Publish(footIkCaptureView);
                        if (!snapshot.FootIkCommittedCaptureView.Lineage.Equals(
                                footIkCaptureView.Lineage))
                        {
                            throw new InvalidOperationException(
                                "Animation diagnostics Foot IK view was not preserved.");
                        }
                    }
                    catch
                    {
                        m_FootIkProjector.Invalidate();
                        throw;
                    }
                }
            }
            return footIkCaptureView;
        }

        internal void Reset()
        {
            RequireAlive();
            Invalidate();
            m_ActorProjector.Reset();
        }

        internal void Invalidate()
        {
            RequireAlive();
            m_Publisher.Invalidate();
            m_FootIkProjector.Invalidate();
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            m_FootIkProjector.Invalidate();
            m_Publisher.Dispose();
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

        void RequireAlive()
        {
            if (m_Disposed)
            {
                throw new ObjectDisposedException(
                    nameof(CharacterPoseDiagnosticsRuntime));
            }
        }
    }
}
