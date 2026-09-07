using System;
using System.Collections.Generic;
using BTSMTL.Diagnostics;
using KK.GeneratedDiagnosticSampling;
using ThirdPersonCharacter.Pipeline;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;
using ThirdPersonCharacter.Pipeline.Diagnostics;
using ThirdPersonGameplay.Tick;
using ThirdPersonPerformance.Instrumentation;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    public sealed class CharacterSimulationPresentationRuntime :
        ICharacterPresentationRuntime,
        ISimulationPresentationOutputPort,
        IAnimationPresentationRuntimeSnapshotProvider
    {
        readonly ActorId m_ActorId;
        readonly CharacterPresentationProjection m_Projection;
        readonly CharacterBodyPresentationRuntime m_Body;
        readonly CharacterPresentationFactProjector m_FactProjector;
        readonly CharacterAnimationPresentationRuntime m_Animation;
        readonly CharacterEquipmentVisualRuntime m_Equipment;
        readonly CharacterEquipmentLinkedPoseRuntime m_LinkedPose;
        readonly CharacterCameraPresentationRuntime m_Camera;
        readonly Transform m_VisualRoot;
        readonly Transform m_PoseRoot;
        readonly RuntimeDiagnosticsContext m_Diagnostics;
        readonly Guid m_RuntimeInstanceId;
        CharacterPresentationFactFrame m_LastProjectedFactFrame;
        readonly CharacterPoseWorkerPresentationSession
            m_WorkerPresentationSession;
        readonly List<CharacterPresentationCommand> m_CurrentFrameSignals =
            new List<CharacterPresentationCommand>();

        bool m_PoseHasOutput;
        ulong m_LastBodyResetSequence;
        double m_LastAnimationSampleTick;
        bool m_AnimationClockInitialized;
        ulong m_AnimationBranchReplacementCount;
        bool m_ReportedPresentationFailure;
        FinalAnimationPoseFrame m_LastFinalPose;
        CharacterPosePlanStageSnapshot m_PosePlanStages;
        GameplayPresentationFrameContext m_PendingPresentationContext;
        CharacterBodyPresentationFrame m_PendingBodyFrame;
        bool m_PresentationFrameActive;
        bool m_PendingAnimationFrame;
        bool m_PendingCameraFrame;
        bool m_PerformanceContextActive;
        bool m_Disposed;

        internal CharacterSimulationPresentationRuntime(
            ActorId actorId,
            CharacterPresentationProjection projection,
            CharacterBodyPresentationRuntime body,
            CharacterAnimationPresentationRuntime animation,
            CharacterEquipmentVisualRuntime equipment,
            CharacterCameraPresentationRuntime camera,
            Transform poseRoot,
            RuntimeDiagnosticsContext diagnostics,
            Guid runtimeInstanceId,
            CharacterPoseWorkerPresentationSession workerPresentationSession)
        {
            if (!actorId.IsValid)
                throw new ArgumentException("Presentation Runtime Actor identity is invalid.", nameof(actorId));
            m_ActorId = actorId;
            m_Projection = projection ?? throw new ArgumentNullException(nameof(projection));
            m_Body = body ?? throw new ArgumentNullException(nameof(body));
            m_FactProjector = new CharacterPresentationFactProjector(actorId);
            m_Animation = animation ?? throw new ArgumentNullException(nameof(animation));
            m_Equipment = equipment ?? throw new ArgumentNullException(nameof(equipment));
            m_LinkedPose = new CharacterEquipmentLinkedPoseRuntime(actorId, projection);
            bool requiresFootPlacement = projection.PosePlan.FootPlacements.Count == 1;
            if (requiresFootPlacement != m_Animation.HasFootPlacement)
                throw new InvalidOperationException("Foot Placement runtime must match the compiled Pose Graph node exactly.");
            m_Camera = camera;
            m_RuntimeInstanceId = runtimeInstanceId != Guid.Empty
                ? runtimeInstanceId
                : throw new ArgumentException(
                    "Presentation runtime identity is invalid.",
                    nameof(runtimeInstanceId));
            m_VisualRoot = m_Body.VisualRoot;
            m_PoseRoot = poseRoot
                ? poseRoot
                : throw new ArgumentNullException(nameof(poseRoot));
            if (m_PoseRoot.parent != m_VisualRoot)
                throw new InvalidOperationException("PoseRoot must be a direct child of the Presentation VisualRoot.");
            m_Diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
            m_WorkerPresentationSession = workerPresentationSession ??
                throw new ArgumentNullException(nameof(workerPresentationSession));
        }

        internal CharacterPoseWorkerPresentationSession
            WorkerPresentationSession => m_WorkerPresentationSession;

        public void CaptureBodyInterval(CharacterPresentationBodyInterval interval)
        {
            RequireAlive();
            if (interval.ActorId != m_ActorId)
                throw new InvalidOperationException("Presentation Body interval targets another Actor.");
            m_Body.Capture(interval);
            m_FactProjector.CaptureBodyBranch(
                m_Body.ResetSequence,
                m_Body.ResetReason);
        }

        public bool AcceptsTrajectoryIntent => true;
        public bool MotionMatchingRuntimeEnabled => m_Animation.MotionMatchingRuntimeEnabled;
        public AnimationPresentationDiagnosticsInterest DiagnosticsInterest =>
            m_Animation.DiagnosticsInterest;
        internal CharacterPoseTuningLayout TuningLayout =>
            m_Animation.TuningLayout;
        internal CharacterPoseTuningParameterBlock ActiveTuningBlock =>
            m_Animation.ActiveTuningBlock;
        internal CharacterPoseTuningRuntimeState TuningState =>
            m_Animation.TuningState;
        internal bool SubmitTuningCandidate(
            CharacterPoseTuningCandidate candidate,
            out string error) =>
            m_Animation.SubmitTuningCandidate(candidate, out error);
        internal void ClearPendingTuningCandidate() =>
            m_Animation.ClearPendingTuningCandidate();
        public ulong BodyResetSequence => m_Body.ResetSequence;
        public CharacterPosePlanStageSnapshot PosePlanStages => m_PosePlanStages;

        public bool TryGetAnimationPresentationDebugView(
            out AnimationPresentationDebugView debugView)
        {
            if (m_Disposed || !m_Animation.HasDebugView)
            {
                debugView = null;
                return false;
            }
            debugView = m_Animation.DebugView;
            return true;
        }

        public bool TryGetPosePlanStages(out CharacterPosePlanStageSnapshot snapshot)
        {
            snapshot = m_PosePlanStages;
            return !m_Disposed && snapshot.IsValid;
        }

        public bool TryCaptureMotionMatchingSearchReplay(
            string providerId,
            out MotionMatchingSearchReplayArtifact artifact)
        {
            if (m_Disposed)
            {
                artifact = null;
                return false;
            }
            return m_Animation.TryCaptureMotionMatchingSearchReplay(providerId, out artifact);
        }

        public void SetPoseWatchInterests(Guid ownerId, IReadOnlyList<AnimationPoseWatchIdentity> interests)
        {
            RequireAlive();
            m_Animation.SetPoseWatchInterests(ownerId, interests);
        }

        public void RemovePoseWatchInterests(Guid ownerId)
        {
            if (!m_Disposed)
                m_Animation.RemovePoseWatchInterests(ownerId);
        }

        public void SetDiagnosticsInterest(
            Guid ownerId,
            AnimationPresentationDiagnosticsInterest interest)
        {
            RequireAlive();
            m_Animation.SetDiagnosticsInterest(ownerId, interest);
        }

        public void RemoveDiagnosticsInterest(Guid ownerId)
        {
            if (!m_Disposed)
                m_Animation.RemoveDiagnosticsInterest(ownerId);
        }

        public void CaptureEquipmentSelections(IReadOnlyList<EquipmentVisualSelection> selections)
        {
            RequireAlive();
            m_LinkedPose.Capture(selections);
            m_Equipment.Capture(selections);
        }

        public void CaptureTrajectoryIntent(CharacterPresentationTrajectoryIntent intent)
        {
            RequireAlive();
            if (intent.ActorId != m_ActorId)
                throw new InvalidOperationException("Presentation Trajectory Intent targets another Actor.");
            m_FactProjector.CaptureIntent(intent);
            if (m_Animation.AcceptsMotionMatchingTrajectoryIntent)
                m_Animation.CaptureMotionMatchingTrajectoryIntent(intent);
        }

        public void CaptureBodyTransaction(IReadOnlyList<CharacterPresentationBodyInterval> intervals)
        {
            RequireAlive();
            m_Body.CaptureTransaction(intervals);
            m_FactProjector.CaptureBodyBranch(
                m_Body.ResetSequence,
                m_Body.ResetReason);
        }

        public void Publish(PresentationCommand command) =>
            Publish(CharacterPresentationCommand.FromFloat32(command));

        public void Publish(CharacterPresentationCommand command)
        {
            RequireAlive();
            if (command.Header.ActorId != m_ActorId)
                throw new InvalidOperationException("Presentation command targets another Actor.");
            if (command.Kind == CharacterPresentationCommandKind.DomainEvent)
            {
                m_Animation.NotifyDomainEvent(command);
                return;
            }
            CharacterPresentationProducerEntry producer = RequireProducer(command.ProducerId);
            switch (command.Kind)
            {
                case CharacterPresentationCommandKind.SelectProducer:
                case CharacterPresentationCommandKind.SampleProducer:
                case CharacterPresentationCommandKind.CompleteProducer:
                case CharacterPresentationCommandKind.ReleaseProducer:
                    m_Animation.Publish(command, producer);
                    break;
                case CharacterPresentationCommandKind.Camera:
                    RequireCamera().Publish(command, producer);
                    break;
                case CharacterPresentationCommandKind.Cue:
                    if (producer.Kind != CharacterPresentationProducerKind.Cue || producer.Cue == null)
                    {
                        throw new InvalidOperationException(
                            $"Cue command targets invalid Projection producer '{producer.ProgramProducerIdentity}'.");
                    }
                    m_CurrentFrameSignals.Add(command);
                    break;
                case CharacterPresentationCommandKind.ForceProducer:
                    if (producer.Kind != CharacterPresentationProducerKind.Camera)
                        throw new InvalidOperationException("Force Presentation command requires a Camera producer.");
                    RequireCamera().Force(command, producer);
                    break;
                case CharacterPresentationCommandKind.Vfx:
                case CharacterPresentationCommandKind.Ui:
                    m_CurrentFrameSignals.Add(command);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(command.Kind), command.Kind, null);
            }
        }

        public void Retire(CharacterPresentationCommand command)
        {
            RequireAlive();
            if (command.Header.ActorId != m_ActorId)
                throw new InvalidOperationException("Presentation retirement targets another Actor.");
            CharacterPresentationProducerEntry producer = RequireProducer(command.ProducerId);
            switch (command.Kind)
            {
                case CharacterPresentationCommandKind.SelectProducer:
                case CharacterPresentationCommandKind.SampleProducer:
                case CharacterPresentationCommandKind.CompleteProducer:
                case CharacterPresentationCommandKind.ReleaseProducer:
                    m_Animation.Retire(command, producer);
                    break;
                case CharacterPresentationCommandKind.Camera:
                    RequireCamera().Retire(command, producer);
                    break;
                case CharacterPresentationCommandKind.ForceProducer:
                    if (producer.Kind != CharacterPresentationProducerKind.Camera)
                        throw new InvalidOperationException("Force Presentation retirement requires a Camera producer.");
                    RequireCamera().Force(command, producer);
                    break;
                case CharacterPresentationCommandKind.Cue:
                case CharacterPresentationCommandKind.Vfx:
                case CharacterPresentationCommandKind.Ui:
                    RetireSignal(command.Header.EventId);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(command.Kind), command.Kind, null);
            }
        }

        public void Replace(
            CharacterPresentationCommand current,
            CharacterPresentationCommand replacement)
        {
            RequireAlive();
            if (current.Header.ActorId != m_ActorId || replacement.Header.ActorId != m_ActorId)
                throw new InvalidOperationException("Presentation replacement targets another Actor.");
            CharacterPresentationProducerEntry currentProducer = RequireProducer(current.ProducerId);
            CharacterPresentationProducerEntry replacementProducer = RequireProducer(replacement.ProducerId);
            switch (replacement.Kind)
            {
                case CharacterPresentationCommandKind.SelectProducer:
                case CharacterPresentationCommandKind.SampleProducer:
                case CharacterPresentationCommandKind.CompleteProducer:
                case CharacterPresentationCommandKind.ReleaseProducer:
                    m_AnimationBranchReplacementCount = checked(m_AnimationBranchReplacementCount + 1);
                    m_Animation.Replace(current, replacement, currentProducer, replacementProducer);
                    break;
                case CharacterPresentationCommandKind.Camera:
                    RequireCamera().Retire(current, currentProducer);
                    RequireCamera().Publish(replacement, replacementProducer);
                    break;
                case CharacterPresentationCommandKind.Cue:
                case CharacterPresentationCommandKind.Vfx:
                case CharacterPresentationCommandKind.Ui:
                    RetireSignal(current.Header.EventId);
                    m_CurrentFrameSignals.Add(replacement);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(replacement.Kind), replacement.Kind, null);
            }
        }

        internal void BeginPresentationFrame(
            GameplayPresentationFrameContext context)
        {
            RequireAlive();
            if (m_PresentationFrameActive)
            {
                throw new InvalidOperationException(
                    "Character Presentation already has an active frame.");
            }
            m_PendingPresentationContext = context;
            m_PresentationFrameActive = true;
            m_Diagnostics.BeginPresentationFrame(context.RenderFrame);
            PerformanceInstrumentationContextRuntime.BeginActor(
                PerformanceInstrumentationIdentity.Hash64(m_ActorId.Value),
                PerformanceInstrumentationIdentity.Hash64(m_Projection.ProgramId),
                PerformanceInstrumentationIdentity.Hash64(m_Projection.ContractHash));
            m_PerformanceContextActive = true;
            try
            {
                m_Equipment.Present();
                m_PendingBodyFrame = m_Body.Present(context);
                if (!m_PendingBodyFrame.IsValid)
                {
                    m_AnimationClockInitialized = false;
                    ResetPoseIfNeeded(
                        context.RenderFrame,
                        m_LastBodyResetSequence,
                        CharacterFootPlacementResetReason.MissingAnimationOutput,
                        CharacterBodyPresentationResetReason.Initialization);
                    return;
                }
                if (m_PendingBodyFrame.ResetSequence !=
                    m_LastBodyResetSequence)
                {
                    m_AnimationClockInitialized = false;
                    if (m_PendingBodyFrame.ResetReason ==
                        CharacterBodyPresentationResetReason
                            .CommittedBranchReplacement)
                    {
                        m_Animation.RetargetBodyBranch(
                            m_PendingBodyFrame.ResetSequence);
                        m_Animation.RetargetFootPlacement(
                            m_PendingBodyFrame.ResetSequence);
                    }
                    else
                    {
                        m_Animation.ResetPoseBranch(
                            m_PendingBodyFrame.ResetSequence);
                        m_Animation.ResetFootPlacement(
                            new CharacterFootPlacementReset(
                                m_ActorId,
                                context.RenderFrame,
                                m_PendingBodyFrame.ResetSequence,
                                CharacterFootPlacementResetReason
                                    .BodyStreamReset,
                                m_PendingBodyFrame.ResetReason));
                        m_PoseHasOutput = false;
                    }
                    m_LastBodyResetSequence =
                        m_PendingBodyFrame.ResetSequence;
                }
                float animationDeltaSeconds = ResolveAnimationDeltaSeconds(
                    in context,
                    in m_PendingBodyFrame);
                m_PendingCameraFrame = m_Camera != null;
                if (animationDeltaSeconds <= 0f)
                    return;
                CharacterPresentationFactFrame factFrame = m_FactProjector.Project(
                    context.RenderFrame,
                    animationDeltaSeconds,
                    in m_PendingBodyFrame);
                m_LastProjectedFactFrame = factFrame;
                try
                {
                    CharacterPresentationProgramParameterFrame parameterFrame =
                        CharacterPresentationProgramParameterFrame.FromFact(
                            in factFrame);
                    m_PendingAnimationFrame = m_Animation.BeginPresentation(
                        context.RenderFrame,
                        m_PendingBodyFrame.AnimationSampleTick,
                        m_PendingBodyFrame.AnimationSampleAlpha,
                        animationDeltaSeconds,
                        in m_PendingBodyFrame,
                        in factFrame,
                        in parameterFrame,
                        m_LinkedPose.Session,
                        m_Diagnostics);
                }
                catch (Exception exception)
                {
                    ReportPresentationFailure(exception);
                    throw;
                }
            }
            catch
            {
                m_CurrentFrameSignals.Clear();
                ClearPendingPresentationFrame();
                throw;
            }
        }

        internal bool TryAdvancePresentationFrame(
            out CharacterPoseWorkerStageLease workerLease)
        {
            if (!m_PresentationFrameActive)
            {
                throw new InvalidOperationException(
                    "Character Presentation has no active frame.");
            }
            workerLease = default;
            if (!m_PendingAnimationFrame)
                return false;
            try
            {
                return m_Animation.TryAdvancePresentation(out workerLease);
            }
            catch (Exception exception)
            {
                ReportPresentationFailure(exception);
                throw;
            }
        }


        internal void CompletePresentationFrame()
        {
            if (!m_PresentationFrameActive)
            {
                throw new InvalidOperationException(
                    "Character Presentation has no active frame.");
            }
            try
            {
                if (m_PendingAnimationFrame)
                {
                    ComposedAnimationPoseFrame animationPose;
                    try
                    {
                        animationPose = m_Animation.CompletePresentation();
                    }
                    catch (Exception exception)
                    {
                        ReportPresentationFailure(exception);
                        throw;
                    }
                    CommitFinalPose(
                        m_PendingBodyFrame,
                        m_PendingPresentationContext,
                        in animationPose);
                }
                if (m_PendingCameraFrame)
                    m_Camera.Present(
                        m_PendingBodyFrame,
                        m_PendingPresentationContext
                            .PresentationDeltaSeconds);
#if KK_DIAGNOSTIC_SAMPLING
                PublishPresentationReplicationDiagnostics();
#endif
            }
            finally
            {
                m_CurrentFrameSignals.Clear();
                ClearPendingPresentationFrame();
            }
        }

#if KK_DIAGNOSTIC_SAMPLING
        void PublishPresentationReplicationDiagnostics()
        {
            var target = new DiagnosticEventTargetKey(
                CharacterPresentationReplicationDiagnosticEvent.TargetTypeIdentity,
                m_RuntimeInstanceId);
            if (!CharacterPresentationReplicationDiagnosticEvent.IsInterested(
                    in target))
            {
                return;
            }
            AnimationPresentationRuntimeSnapshot animation =
                m_Animation.HasRuntimeDiagnosticsSnapshot
                    ? m_Animation.RuntimeDiagnosticsSnapshot
                    : default;
            CharacterAnimationPresentationCaptureFrame animationFacts =
                new CharacterAnimationPresentationCaptureFrame(
                    m_PendingPresentationContext.RenderFrame,
                    m_PendingPresentationContext.LocalLogicTick,
                    m_PendingBodyFrame.ResetSequence,
                    m_PendingPresentationContext.PresentationDeltaSeconds,
                    in animation);
            CharacterCameraPresentationCaptureFrame cameraFacts =
                m_PendingCameraFrame
                    ? m_Camera.LastPresentationFrame.WithPresentationContext(
                        m_PendingPresentationContext.RenderFrame,
                        m_PendingPresentationContext.LocalLogicTick,
                        m_PendingBodyFrame.ResetSequence,
                        m_PendingPresentationContext.PresentationDeltaSeconds)
                    : CharacterCameraPresentationCaptureFrame.Empty;
            var commandFacts = new CharacterPresentationCommandCaptureFacts(
                m_CurrentFrameSignals);
            var factFacts = new CharacterPresentationFactCaptureFrame(
                in m_LastProjectedFactFrame);
            var lineage = new DiagnosticLineageKey(
                CharacterPresentationReplicationDiagnosticEvent.LineageTypeIdentity,
                m_PendingPresentationContext.RenderFrame,
                animation.CompletionIdentity != 0
                    ? animation.CompletionIdentity
                    : m_PendingPresentationContext.LocalLogicTick);
            CharacterPresentationReplicationDiagnosticEvent.Publish(
                in target,
                in lineage,
                in animationFacts,
                in cameraFacts,
                in factFacts,
                in commandFacts);
        }
#endif

        internal Exception AbortPresentationFrame()
        {
            if (!m_PresentationFrameActive)
                return null;
            Exception failure = m_PendingAnimationFrame
                ? m_Animation.AbortPresentation()
                : null;
            m_CurrentFrameSignals.Clear();
            ClearPendingPresentationFrame();
            return failure;
        }

        void ClearPendingPresentationFrame()
        {
            EndPerformanceContext();
            m_PendingPresentationContext = default;
            m_PendingBodyFrame = default;
            m_PresentationFrameActive = false;
            m_PendingAnimationFrame = false;
            m_PendingCameraFrame = false;
        }

        void EndPerformanceContext()
        {
            if (!m_PerformanceContextActive)
                return;
            PerformanceInstrumentationContextRuntime.EndActor();
            m_PerformanceContextActive = false;
        }

        void ReportPresentationFailure(Exception exception)
        {
            if (m_ReportedPresentationFailure)
                return;
            m_ReportedPresentationFailure = true;
            Debug.LogError(
                $"Presentation failure Actor={m_ActorId}, Frame={m_PendingPresentationContext.RenderFrame}, " +
                $"BodyTick={m_PendingBodyFrame.PreviousTick}->{m_PendingBodyFrame.CurrentTick}@{m_PendingBodyFrame.SampleAlpha:R}, " +
                $"Visible={m_PendingBodyFrame.VisiblePosition:R}, VisualRoot={m_VisualRoot.position:R}, " +
                $"PoseRoot={m_PoseRoot.position:R}, PoseRootLocal={m_PoseRoot.localPosition:R}, " +
                $"CameraSkipped={m_Camera != null}, Error={exception.Message}");
        }

        public CharacterPresentationRuntimeDiagnosticsSnapshot CaptureDiagnostics()
        {
            return new CharacterPresentationRuntimeDiagnosticsSnapshot(
                m_Body.BranchReplacementCount,
                m_AnimationBranchReplacementCount,
                m_Body.FollowerPositionCorrectionMeters,
                m_Body.FollowerYawCorrectionDegrees,
                m_PosePlanStages,
                m_Animation.HasRuntimeDiagnosticsSnapshot,
                m_Animation.HasRuntimeDiagnosticsSnapshot
                    ? m_Animation.RuntimeDiagnosticsSnapshot
                    : default);
        }

        public void Reset()
        {
            if (m_Disposed)
                return;
            m_CurrentFrameSignals.Clear();
            m_LinkedPose.Reset();
            m_Equipment.Reset();
            m_Camera?.Reset();
            m_Animation.ResetFootPlacement(new CharacterFootPlacementReset(
                m_ActorId,
                0,
                0,
                CharacterFootPlacementResetReason.PresentationReset,
                CharacterBodyPresentationResetReason.Initialization));
            m_Animation.Reset();
            m_Body.Reset();
            m_FactProjector.Reset();
            m_PoseHasOutput = false;
            m_LastBodyResetSequence = 0;
            m_LastAnimationSampleTick = 0d;
            m_AnimationClockInitialized = false;
            m_AnimationBranchReplacementCount = 0;
            m_LastFinalPose = default;
            m_PosePlanStages = default;
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            m_CurrentFrameSignals.Clear();
            CharacterPresentationModuleLifetime.Dispose(m_Camera, m_Equipment, m_Animation, m_Body);
        }

        float ResolveAnimationDeltaSeconds(
            in GameplayPresentationFrameContext context,
            in CharacterBodyPresentationFrame bodyFrame)
        {
            if (m_Body.SourceMode != CharacterBodyPresentationSourceMode.CommittedStream)
                return context.PresentationDeltaSeconds;
            double sampleTick = (double)bodyFrame.PreviousTick +
                                ((double)bodyFrame.CurrentTick - bodyFrame.PreviousTick) *
                                (double)bodyFrame.SampleAlpha;
            if (!m_AnimationClockInitialized)
            {
                m_LastAnimationSampleTick = sampleTick;
                m_AnimationClockInitialized = true;
                return m_Body.TickDurationSeconds;
            }
            double deltaTicks = sampleTick - m_LastAnimationSampleTick;
            if (deltaTicks < -0.000001d)
                throw new InvalidOperationException("Animation presentation sample clock cannot move backward.");
            m_LastAnimationSampleTick = sampleTick;
            double deltaSeconds = Math.Max(0d, deltaTicks) * m_Body.TickDurationSeconds;
            if (double.IsNaN(deltaSeconds) || double.IsInfinity(deltaSeconds) || deltaSeconds > float.MaxValue)
                throw new InvalidOperationException("Animation logic delta is invalid.");
            return (float)deltaSeconds;
        }

        [PerformanceProbe("presentation.final-pose")]
        void CommitFinalPose(
            CharacterBodyPresentationFrame bodyFrame,
            GameplayPresentationFrameContext context,
            in ComposedAnimationPoseFrame animationPose)
        {
            AnimationPoseAvailability availability;
            try
            {
                availability = animationPose.Availability;
            }
            catch (InvalidOperationException)
            {
                m_PosePlanStages = ShouldCapturePosePlanStages
                    ? CharacterPosePlanStageSnapshotFactory.Unavailable(
                        m_Projection.PosePlan,
                        AnimationPoseAvailability.Invalid,
                        CharacterPoseStageUnavailableReason.PoseUnavailable)
                    : default;
                ResetPoseIfNeeded(
                    context.RenderFrame,
                    bodyFrame.ResetSequence,
                    CharacterFootPlacementResetReason.MissingAnimationOutput,
                    bodyFrame.ResetReason);
                return;
            }
            if (availability != AnimationPoseAvailability.Pose)
            {
                m_PosePlanStages = ShouldCapturePosePlanStages
                    ? CharacterPosePlanStageSnapshotFactory.Unavailable(
                        m_Projection.PosePlan,
                        availability,
                        CharacterPoseStageUnavailableReason.PoseUnavailable)
                    : default;
                ResetPoseIfNeeded(
                    context.RenderFrame,
                    bodyFrame.ResetSequence,
                    CharacterFootPlacementResetReason.InvalidPose,
                    bodyFrame.ResetReason);
                return;
            }
            m_LastFinalPose = new FinalAnimationPoseFrame(in animationPose, animationPose.CompletionIdentity);
            m_PosePlanStages = ShouldCapturePosePlanStages
                ? CharacterPosePlanStageSnapshotFactory.Completed(
                    m_Projection.PosePlan,
                    in animationPose)
                : default;
            m_PoseHasOutput = true;
        }

        bool ShouldCapturePosePlanStages =>
            (m_Animation.DiagnosticsInterest &
             (AnimationPresentationDiagnosticsInterest.LiveState |
              AnimationPresentationDiagnosticsInterest.Capture)) != 0;

        void ResetPoseIfNeeded(
            ulong renderFrame,
            ulong resetSequence,
            CharacterFootPlacementResetReason reason,
            CharacterBodyPresentationResetReason bodyReason)
        {
            if (!m_PoseHasOutput)
                return;
            m_Animation.ResetFootPlacement(new CharacterFootPlacementReset(
                m_ActorId,
                renderFrame,
                resetSequence,
                reason,
                bodyReason));
            m_PoseHasOutput = false;
            m_LastFinalPose = default;
        }

        CharacterPresentationProducerEntry RequireProducer(string producerId)
        {
            if (!m_Projection.TryGetProducer(producerId, out CharacterPresentationProducerEntry producer))
            {
                throw new InvalidOperationException(
                    $"Presentation producer '{producerId}' is absent from the compiled Projection.");
            }
            return producer;
        }

        CharacterCameraPresentationRuntime RequireCamera()
        {
            return m_Camera ?? throw new InvalidOperationException(
                "Camera PresentationCommand targets an Actor without an explicit Camera composition.");
        }

        void RetireSignal(EventId eventId)
        {
            for (int i = m_CurrentFrameSignals.Count - 1; i >= 0; i--)
            {
                if (m_CurrentFrameSignals[i].Header.EventId.Equals(eventId))
                    m_CurrentFrameSignals.RemoveAt(i);
            }
        }

        void RequireAlive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(CharacterSimulationPresentationRuntime));
        }
    }
}
