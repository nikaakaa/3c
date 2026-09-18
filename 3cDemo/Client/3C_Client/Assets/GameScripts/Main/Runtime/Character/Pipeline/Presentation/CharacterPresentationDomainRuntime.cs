using System;
using System.Collections.Generic;
using BTSMTL.Diagnostics;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.Presentation;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;
using ThirdPersonCharacter.Pipeline.Animation.Resources;
using ThirdPersonGameplay.Tick;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    internal sealed class CharacterPresentationDomainRuntime :
        IDisposable,
        ICharacterPresentationDomainRuntime,
        ICharacterPoseNativeEventFrameSource
    {
        readonly ActorId m_ActorId;
        readonly CharacterBodyPresentationRuntime m_Body;
        readonly PreparedCharacterLocomotionPresentationBinding m_LocomotionBinding;
        readonly RuntimeDiagnosticsContext m_Diagnostics;
        readonly CharacterAnimationEventGraphHost m_EventGraph;
        readonly CharacterEquipmentDomainRuntime m_Equipment;
        readonly CharacterCameraDomainRuntime m_Camera;
        readonly IActionPresentationClockCoordinator m_PresentationClockCoordinator;
        readonly double m_PresentationTimePerTick;
        CharacterPoseNativeDomainInstance m_PoseDomain;
        CharacterAnimationResourceScope m_PoseResourceScope;
        CharacterAnimationVariableFrame m_EventFrame;
        CharacterPresentationTrajectoryIntent m_Trajectory;
        CharacterLocomotionPresentationFactLineage m_LastLocomotionFactLineage;
        LocomotionPresentationFailureCode m_LocomotionFailureCode;
        bool m_HasTrajectory;
        bool m_HasPoseDiscontinuityIdentity;
        ulong m_PoseDiscontinuityIdentity;
        bool m_Disposed;
        ulong m_NextPoseResetGeneration = 1;

        internal CharacterPresentationDomainRuntime(
            ActorId actorId,
            CharacterBodyPresentationRuntime body,
            PreparedCharacterLocomotionPresentationBinding locomotionBinding,
            int tickRate,
            CharacterAnimationPresentationProfile presentationProfile,
            CharacterEquipmentDomainRuntime equipment,
            CharacterCameraDomainRuntime camera,
            IActionPresentationClockCoordinator presentationClockCoordinator,
            RuntimeDiagnosticsContext diagnostics)
        {
            m_ActorId = actorId;
            m_Body = body ?? throw new ArgumentNullException(nameof(body));
            locomotionBinding.RequireValid();
            m_LocomotionBinding = locomotionBinding;
            m_Diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
            if (tickRate <= 0)
                throw new ArgumentOutOfRangeException(nameof(tickRate));
            m_PresentationTimePerTick = 1d / tickRate;
            if (!presentationProfile)
                throw new ArgumentNullException(nameof(presentationProfile));
            if (!presentationProfile.EventGraph)
                throw new InvalidOperationException("Presentation domain requires an Animation EventGraph.");
            m_Equipment = equipment;
            m_Camera = camera;
            m_PresentationClockCoordinator = presentationClockCoordinator;
            m_EventGraph = new CharacterAnimationEventGraphHost(
                presentationProfile.EventGraph,
                actorId);
        }

        PoseParameterId[] m_PoseParameterIds;
        ThirdPersonCharacter.Pipeline.Animation.Lifecycle.CharacterPoseActionCommandPublisher m_PoseActionPublisher;

        internal bool TryGetPoseCommittedPose(out ComposedAnimationPoseFrame frame)
        {
            if (m_PoseDomain == null || !m_PoseDomain.IsAdopted)
            {
                frame = default;
                return false;
            }
            return m_PoseDomain.Session.TryObserveFinalPose(out frame);
        }

        internal string PoseGraphRevision => m_PoseDomain != null && m_PoseDomain.IsAdopted
            ? m_PoseDomain.Session.GraphRevision
            : string.Empty;

        internal ulong PoseInstanceId => m_PoseDomain != null && m_PoseDomain.IsAdopted
            ? m_PoseDomain.Session.InstanceId
            : 0;

        internal ulong PoseResetGeneration => m_PoseDomain != null && m_PoseDomain.IsAdopted
            ? m_PoseDomain.Session.ResetGeneration
            : 0;

        ThirdPersonCharacter.Pipeline.Animation.Lifecycle.CharacterTimelineHost m_TimelineHost;
        ThirdPersonCharacter.Pipeline.Animation.Lifecycle.TimelineToActionCommandBridge m_TimelineBridge;

        internal void InitializeTimelineHost(
            ThirdPersonCharacter.Pipeline.Animation.Lifecycle.CharacterTimelineHost timelineHost,
            BTSMTL.Timeline.Runtime.TimelineRuntimeNumericTarget numericTarget,
            int tickRate)
        {
            if (timelineHost == null)
                throw new ArgumentNullException(nameof(timelineHost));
            if (m_PoseActionPublisher == null)
                throw new InvalidOperationException("Timeline playback requires a composed Pose Action command publisher.");
            m_TimelineHost = timelineHost;
            timelineHost.Initialize(numericTarget, tickRate);
            BindTimelineBridge(new ThirdPersonCharacter.Pipeline.Animation.Lifecycle.TimelineToActionCommandBridge(
                timelineHost, m_PoseActionPublisher.Inbox));
        }

        internal void BindTimelineBridge(ThirdPersonCharacter.Pipeline.Animation.Lifecycle.TimelineToActionCommandBridge bridge)
        {
            m_TimelineBridge = bridge ?? throw new ArgumentNullException(nameof(bridge));
        }

        internal void BindPoseActionPublisher(ThirdPersonCharacter.Pipeline.Animation.Lifecycle.CharacterPoseActionCommandPublisher publisher)
        {
            m_PoseActionPublisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
        }

        internal void BindPoseDomain(
            CharacterPoseNativeDomainInstance poseDomain,
            CharacterAnimationResourceScope resourceScope,
            System.Collections.Generic.IReadOnlyList<CharacterPoseParameterDeclaration> poseParameterIds)
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(CharacterPresentationDomainRuntime));
            m_PoseDomain = poseDomain ?? throw new ArgumentNullException(nameof(poseDomain));
            m_PoseResourceScope = resourceScope ?? throw new ArgumentNullException(nameof(resourceScope));
            var ids = new PoseParameterId[poseParameterIds.Count];
            for (int i = 0; i < ids.Length; i++)
                ids[i] = poseParameterIds[i].ParameterId;
            m_PoseParameterIds = ids;
        }

        public bool AcceptsTrajectoryIntent => true;
        public ulong BodyResetSequence => m_Body.ResetSequence;
        public CharacterLocomotionBodySource LocomotionBodySource => m_LocomotionBinding.BodySource;
        public bool SupportsCheckpointCapture => false;
        public bool SupportsCheckpointRestore => false;

        public bool TryGetLatestBody(out CharacterPresentationBodyState body) =>
            m_Body.TryGetLatestBody(out body);

        public CharacterPresentationDomainObservation CaptureObservation()
        {
            bool poseComposed = TryGetPoseCommittedPose(out ComposedAnimationPoseFrame pose);
            return new CharacterPresentationDomainObservation(
                poseComposed,
                PoseGraphRevision,
                PoseInstanceId,
                PoseResetGeneration,
                poseComposed ? pose.Availability.ToString() : string.Empty,
                poseComposed ? pose.PoseBoneCount : 0,
                poseComposed ? pose.Contributions.Count : 0,
                poseComposed ? pose.CompletionIdentity : 0);
        }

        public CharacterDomainRuntimeAssemblyFacts CaptureDomainFacts()
        {
            var facts = new List<CharacterDomainRuntimeFact>
            {
                m_PoseDomain != null && m_PoseDomain.IsAdopted
                    ? new CharacterDomainRuntimeFact(
                        CharacterDomainRuntimeFactKind.Pose,
                        CharacterDomainRuntimeFactState.Adopted,
                        $"pose:{PoseGraphRevision}",
                        $"pose:{PoseGraphRevision}:{PoseInstanceId}:{PoseResetGeneration}",
                        string.Empty)
                    : new CharacterDomainRuntimeFact(
                        CharacterDomainRuntimeFactKind.Pose,
                        CharacterDomainRuntimeFactState.Unavailable,
                        "pose",
                        string.Empty,
                        "Pose Native domain is not installed."),
                m_Camera != null
                    ? m_Camera.CaptureDomainFact()
                    : new CharacterDomainRuntimeFact(
                        CharacterDomainRuntimeFactKind.Camera,
                        CharacterDomainRuntimeFactState.Unavailable,
                        "camera",
                        string.Empty,
                        "Camera domain is owner-only for this presentation role.")
            };
            return new CharacterDomainRuntimeAssemblyFacts(facts);
        }

        public void CaptureBodyStream(IReadOnlyList<CharacterPresentationBodyInterval> intervals) =>
            m_Body.CaptureStreamTransaction(intervals);

        public CharacterLocomotionPresentationFactLineage CreateLocomotionFactLineage(
            in CommittedMovementPlaybackClock movementClock) =>
            m_LocomotionBinding.CreateFactLineage(in movementClock);

        public LocomotionPresentationFailureCode CaptureTrajectoryIntent(
            CharacterPresentationTrajectoryIntent intent)
        {
            if (intent.ActorId != m_ActorId)
                throw new InvalidOperationException("Presentation trajectory targets another Actor.");
            if (intent.ResetSequence != m_Body.ResetSequence)
                throw new InvalidOperationException("Presentation trajectory reset generation is stale.");
            CharacterLocomotionPresentationFactLineage locomotionFactLineage = intent.LocomotionFactLineage;
            CommittedMovementPlaybackClock movementPlaybackClock = intent.MovementPlaybackClock;
            CommittedLocomotionPlanarMotionTimeline locomotionMotionTimeline = intent.LocomotionMotionTimeline;
            LocomotionPresentationFailureCode failureCode = m_LocomotionBinding.ValidateFact(
                in locomotionFactLineage,
                in movementPlaybackClock,
                in locomotionMotionTimeline);
            if (failureCode != LocomotionPresentationFailureCode.None)
            {
                m_LocomotionFailureCode = failureCode;
                PublishLocomotionDiagnostics();
                return failureCode;
            }
            if (m_HasPoseDiscontinuityIdentity &&
                m_PoseDiscontinuityIdentity != intent.PoseDiscontinuityIdentity)
            {
                ResetPose();
            }
            m_PoseDiscontinuityIdentity = intent.PoseDiscontinuityIdentity;
            m_HasPoseDiscontinuityIdentity = true;
            m_Trajectory = intent;
            m_LastLocomotionFactLineage = intent.LocomotionFactLineage;
            m_LocomotionFailureCode = LocomotionPresentationFailureCode.None;
            m_HasTrajectory = true;
            PublishLocomotionDiagnostics();
            return LocomotionPresentationFailureCode.None;
        }

        public void CaptureEquipmentSelections(IReadOnlyList<EquipmentVisualSelection> selections)
        {
            if (m_Equipment == null)
                throw new InvalidOperationException(
                    "Equipment presentation requires a composed Equipment runtime.");
            m_Equipment.Capture(selections);
        }

        public void Publish(CharacterPresentationCommand command)
        {
            RequireActor(command.Header.ActorId);
            if (command.Kind == CharacterPresentationCommandKind.Camera)
            {
                if (m_Camera == null)
                    throw new InvalidOperationException("Camera PresentationCommand reached a presentation without a Camera domain.");
                m_Camera.Publish(command);
                return;
            }
            m_PoseActionPublisher?.Publish(command);
        }

        public void Replace(CharacterPresentationCommand current, CharacterPresentationCommand replacement)
        {
            RequireActor(current.Header.ActorId);
            RequireActor(replacement.Header.ActorId);
            if (current.Kind == CharacterPresentationCommandKind.Camera ||
                replacement.Kind == CharacterPresentationCommandKind.Camera)
            {
                if (m_Camera == null)
                    throw new InvalidOperationException("Camera PresentationCommand reached a presentation without a Camera domain.");
                m_Camera.Replace(current, replacement);
                return;
            }
            m_PoseActionPublisher?.Replace(current, replacement);
        }

        public void Retire(CharacterPresentationCommand command)
        {
            RequireActor(command.Header.ActorId);
            if (command.Kind == CharacterPresentationCommandKind.Camera)
            {
                if (m_Camera == null)
                    throw new InvalidOperationException("Camera PresentationCommand reached a presentation without a Camera domain.");
                m_Camera.Retire(command);
                return;
            }
            m_PoseActionPublisher?.Retire(command);
        }

        public void Reset()
        {
            m_Camera?.Reset();
            m_Equipment?.Reset();
            m_Body.Reset();
            m_EventGraph.Reset();
            m_PresentationClockCoordinator?.Reset();
            m_EventFrame = null;
            m_Trajectory = default;
            m_LastLocomotionFactLineage = default;
            m_LocomotionFailureCode = LocomotionPresentationFailureCode.None;
            m_HasTrajectory = false;
            m_HasPoseDiscontinuityIdentity = false;
            m_PoseDiscontinuityIdentity = 0;
            ResetPose();
            PublishLocomotionDiagnostics();
        }

        public CharacterPresentationDomainDiagnosticsSnapshot CaptureDiagnostics() =>
            new(
                0,
                0,
                m_Body.FollowerPositionCorrectionMeters,
                m_Body.FollowerYawCorrectionDegrees,
                new CharacterLocomotionPresentationDiagnosticSnapshot(
                    m_LocomotionBinding.PlanIdentity,
                    m_LocomotionBinding.ClockMode,
                    m_LocomotionBinding.BodySource,
                    m_LocomotionBinding.CorrectionMode,
                    m_LocomotionBinding.BodyProfileIdentity,
                    m_LastLocomotionFactLineage,
                    m_Body.ResetSequence,
                    m_Body.ResetReason,
                    m_LocomotionFailureCode));

        public bool TryCaptureCheckpoint(SimulationSessionCheckpoint checkpoint, out string error)
        {
            if (checkpoint == null)
                throw new ArgumentNullException(nameof(checkpoint));
            error = "Character Presentation checkpoint capture is not composed.";
            return false;
        }

        public bool TryRestoreCheckpoint(SimulationSessionCheckpoint checkpoint, out string error)
        {
            if (checkpoint == null)
                throw new ArgumentNullException(nameof(checkpoint));
            error = "Character Presentation checkpoint restore is not composed.";
            return false;
        }

        public void PresentationFrame(GameplayPresentationFrameContext context)
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(CharacterPresentationDomainRuntime));
            m_Diagnostics.BeginPresentationFrame(context.RenderFrame);
            m_Equipment?.Present();
            CharacterBodyPresentationFrame bodyFrame = m_Body.Present(context);
            if (!bodyFrame.IsValid)
                return;
            m_TimelineHost?.Present(context);
            m_Camera?.Present(bodyFrame, context);
            CharacterPresentationFactFrame factFrame = CreateFactFrame(in bodyFrame);
            CharacterAnimationVariableUpdateResult update = m_EventGraph.Update(
                in factFrame,
                Mathf.Max(0f, context.PresentationDeltaSeconds),
                context.RenderFrame);
            if (!update.Succeeded)
                throw new InvalidOperationException(
                    $"Animation EventGraph update failed: {update.Failure.Message}");
            m_EventFrame = update.Frame;
            if (m_LocomotionBinding.RequiresMovementFact && !m_HasTrajectory)
            {
                m_LocomotionFailureCode = LocomotionPresentationFailureCode.MissingMovementFact;
                PublishLocomotionDiagnostics();
                return;
            }
            PublishLocomotionDiagnostics();
            RunPoseFrame(in bodyFrame, in factFrame, update.Frame, context);
        }

        void PublishLocomotionDiagnostics()
        {
            if (!m_Diagnostics.ShouldPublish(
                    RuntimeTraceChannel.Animation,
                    RuntimeTraceEventKind.LocomotionPresentation))
            {
                return;
            }
            CharacterLocomotionPresentationFactLineage lineage = m_LastLocomotionFactLineage;
            m_Diagnostics.Publish(
                RuntimeTraceChannel.Animation,
                RuntimeTraceDomain.Presentation,
                RuntimeTraceEventKind.LocomotionPresentation,
                RuntimeSourceElementHandle.Invalid,
                RuntimeInstanceKey.Character(m_Diagnostics.CharacterRuntimeId),
                new RuntimeTracePayload
                {
                    Status = m_LocomotionFailureCode == LocomotionPresentationFailureCode.None
                        ? "Ready"
                        : "Rejected",
                    Name = "LocomotionPresentation",
                    Cause = m_LocomotionFailureCode.ToString(),
                    OwnerId = m_LocomotionBinding.PlanIdentity,
                    RelatedElementId = lineage.MovementSegmentIdentity,
                    Detail = $"plan={m_LocomotionBinding.PlanIdentity};clockMode={m_LocomotionBinding.ClockMode};bodySource={m_LocomotionBinding.BodySource};correctionMode={m_LocomotionBinding.CorrectionMode};profile={m_LocomotionBinding.BodyProfileIdentity};lineagePlan={lineage.PlanIdentity};lineageBodySource={lineage.BodySource};movementClock={lineage.MovementClockIdentity};lineageGeneration={lineage.LineageGeneration};movementSegment={lineage.MovementSegmentIdentity};resetSequence={m_Body.ResetSequence};resetReason={m_Body.ResetReason};failure={m_LocomotionFailureCode}",
                    Value = DebugValueSnapshot.Capture((int)m_LocomotionFailureCode)
                });
        }

        void RunPoseFrame(
            in CharacterBodyPresentationFrame bodyFrame,
            in CharacterPresentationFactFrame factFrame,
            CharacterAnimationVariableFrame eventFrame,
            GameplayPresentationFrameContext context)
        {
            if (m_PoseDomain == null || !m_PoseDomain.IsAdopted)
                return;
            float deltaSeconds = Mathf.Max(0f, context.PresentationDeltaSeconds);
            m_PoseResourceScope.AdvancePreparation();
            m_PoseDomain.BeginFrame(context.RenderFrame);
            CharacterPoseNativePreparationResult preparation;
            try
            {
                if (!m_PoseDomain.TryGetCommands(m_ActorId, context.RenderFrame, out var actionCommands))
                    throw new InvalidOperationException("Pose Action command source did not produce the opened frame commands.");
                m_PresentationClockCoordinator?.BeginFrame(actionCommands);
                var parameterFrame = CharacterAnimationPoseInputFrame.FromPublishedVariables(
                    eventFrame,
                    m_PoseParameterIds);
                var frameInput = new CharacterPoseNativeFrameInput(
                    m_ActorId,
                    context.RenderFrame,
                    context.RenderFrame,
                    bodyFrame.CurrentTick,
                    bodyFrame.CurrentTick + deltaSeconds / m_PresentationTimePerTick,
                    deltaSeconds,
                    bodyFrame,
                    factFrame,
                    parameterFrame,
                    actionCommands);
                preparation = m_PoseDomain.Session.BeginFrame(in frameInput);
                if (preparation.IsValid && preparation.Status == CharacterPoseNativeFrameStatus.Prepared)
                {
                    m_PoseDomain.Session.PrepareEvaluation(context.RenderFrame);
                    CharacterPoseNativeEvaluationResult evaluation = m_PoseDomain.Session.Evaluate(context.RenderFrame);
                    CharacterPoseNativeValidationResult validation = m_PoseDomain.Session.ValidatePending();
                    if (validation.IsValidated)
                    {
                        CharacterPoseNativePublicationResult commit =
                            m_PoseDomain.Session.Commit(false);
                        if (commit.Status == CharacterPoseNativeFrameStatus.Committed)
                        {
                            m_PoseDomain.CommitFrame();
                            m_PresentationClockCoordinator?.CommitFrame();
                        }
                        else
                        {
                            m_PoseDomain.DiscardFrame();
                            m_PresentationClockCoordinator?.DiscardFrame();
                        }
                    }
                    else
                    {
                        m_PoseDomain.Session.Discard(
                            validation.FailureCode != CharacterPoseNativeFailureCode.None
                                ? validation.FailureCode
                                : CharacterPoseNativeFailureCode.FrameInvalid);
                        m_PoseDomain.DiscardFrame();
                        m_PresentationClockCoordinator?.DiscardFrame();
                    }
                }
                else
                {
                    m_PoseDomain.DiscardFrame();
                    m_PresentationClockCoordinator?.DiscardFrame();
                }
            }
            catch
            {
                m_PoseDomain.DiscardFrame();
                m_PresentationClockCoordinator?.DiscardFrame();
                throw;
            }
        }



        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            m_TimelineBridge?.Dispose();
            m_TimelineHost = null;
            m_PoseDomain?.Dispose();
            m_PoseResourceScope?.Dispose();
            m_PresentationClockCoordinator?.Dispose();
            m_Camera?.Dispose();
            m_Equipment?.Dispose();
            m_EventGraph.Dispose();
            m_Body.Dispose();
        }

        bool ICharacterPoseNativeEventFrameSource.TryGetFrame(
            ActorId actorId,
            ulong frameIdentity,
            out CharacterAnimationVariableFrame frame)
        {
            frame = m_EventFrame;
            return actorId == m_ActorId && frame != null && frame.RenderFrame == frameIdentity;
        }

        CharacterPresentationFactFrame CreateFactFrame(
            in CharacterBodyPresentationFrame bodyFrame)
        {
            var identity = new CharacterPresentationFactFrameIdentity(
                m_ActorId,
                bodyFrame.CurrentTick);
            Vector2 desiredVelocity = m_HasTrajectory ? m_Trajectory.DesiredPlanarVelocity : Vector2.zero;
            Vector2 desiredFacing = m_HasTrajectory
                ? m_Trajectory.DesiredFacing
                : NormalizeFacing(bodyFrame.TargetRotation * Vector3.forward);
            return new CharacterPresentationFactFrame(
                identity,
                new SimulationTick(bodyFrame.CurrentTick),
                bodyFrame.CurrentTick * m_PresentationTimePerTick,
                bodyFrame.TargetGrounded,
                bodyFrame.TargetVelocity,
                bodyFrame.TargetRotation,
                desiredFacing,
                m_HasTrajectory && m_Trajectory.HasMotion,
                m_Trajectory.LocomotionPlanarBasis,
                desiredVelocity,
                m_HasTrajectory
                    ? m_Trajectory.MovementModeId
                    : CharacterPresentationTrajectoryIntent.StationaryMovementModeId,
                m_HasTrajectory ? m_Trajectory.MovementPlaybackClock : default,
                m_HasTrajectory ? m_Trajectory.LocomotionMotionTimeline : default,
                m_HasTrajectory && m_Trajectory.MovementPlaybackClock.IsValid
                    ? m_Trajectory.MovementPlaybackClock.ElapsedSeconds
                    : 0d,
                m_Body.ResetSequence,
                m_HasTrajectory ? m_Trajectory.LocomotionFactLineage : default,
                m_HasTrajectory ? m_Trajectory.PoseDiscontinuityIdentity : 0);
        }

        void ResetPose()
        {
            if (m_PoseDomain == null || !m_PoseDomain.IsAdopted)
                return;
            m_NextPoseResetGeneration++;
            m_PoseDomain.Reset(m_NextPoseResetGeneration);
        }

        static Vector2 NormalizeFacing(Vector3 forward)
        {
            var value = new Vector2(forward.x, forward.z);
            return value.sqrMagnitude > 0.000001f ? value.normalized : Vector2.up;
        }

        void RequireActor(ActorId actorId)
        {
            if (actorId != m_ActorId)
                throw new InvalidOperationException("Presentation input targets another Actor.");
        }
    }
}

