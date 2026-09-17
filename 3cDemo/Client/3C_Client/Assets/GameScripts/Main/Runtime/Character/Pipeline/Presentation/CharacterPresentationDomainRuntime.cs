using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Animation;
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
        readonly CharacterAnimationEventGraphHost m_EventGraph;
        readonly CharacterEquipmentDomainRuntime m_Equipment;
        readonly CharacterCameraDomainRuntime m_Camera;
        readonly double m_PresentationTimePerTick;
        CharacterPoseNativeDomainInstance m_PoseDomain;
        CharacterAnimationResourceScope m_PoseResourceScope;
        CharacterAnimationVariableFrame m_EventFrame;
        CharacterPresentationTrajectoryIntent m_Trajectory;
        bool m_HasTrajectory;
        bool m_Disposed;
        ulong m_NextPoseResetGeneration = 1;

        internal CharacterPresentationDomainRuntime(
            ActorId actorId,
            CharacterBodyPresentationRuntime body,
            int tickRate,
            CharacterAnimationPresentationProfile presentationProfile,
            CharacterEquipmentDomainRuntime equipment,
            CharacterCameraDomainRuntime camera)
        {
            m_ActorId = actorId;
            m_Body = body ?? throw new ArgumentNullException(nameof(body));
            if (tickRate <= 0)
                throw new ArgumentOutOfRangeException(nameof(tickRate));
            m_PresentationTimePerTick = 1d / tickRate;
            if (!presentationProfile)
                throw new ArgumentNullException(nameof(presentationProfile));
            if (!presentationProfile.EventGraph)
                throw new InvalidOperationException("Presentation domain requires an Animation EventGraph.");
            m_Equipment = equipment;
            m_Camera = camera;
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

        ThirdPersonCharacter.Pipeline.Animation.Lifecycle.TimelineToActionCommandBridge m_TimelineBridge;

        internal void InitializeTimelineHost(
            ThirdPersonCharacter.Pipeline.Animation.Lifecycle.CharacterTimelineHost timelineHost,
            BTSMTL.Timeline.Runtime.TimelineRuntimeNumericTarget numericTarget)
        {
            if (timelineHost == null)
                throw new ArgumentNullException(nameof(timelineHost));
            if (m_PoseActionPublisher == null)
                throw new InvalidOperationException("Timeline playback requires a composed Pose Action command publisher.");
            timelineHost.Initialize(numericTarget);
            BindTimelineBridge(new ThirdPersonCharacter.Pipeline.Animation.Lifecycle.TimelineToActionCommandBridge(
                timelineHost.Host, m_PoseActionPublisher.Inbox));
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

        public void CaptureBodyTransaction(IReadOnlyList<CharacterPresentationBodyInterval> intervals) =>
            m_Body.CaptureTransaction(intervals);

        public void CaptureTrajectoryIntent(CharacterPresentationTrajectoryIntent intent)
        {
            if (intent.ActorId != m_ActorId)
                throw new InvalidOperationException("Presentation trajectory targets another Actor.");
            if (intent.ResetSequence != m_Body.ResetSequence)
                throw new InvalidOperationException("Presentation trajectory reset generation is stale.");
            m_Trajectory = intent;
            m_HasTrajectory = true;
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
            m_EventFrame = null;
            m_Trajectory = default;
            m_HasTrajectory = false;
            if (m_PoseDomain != null && m_PoseDomain.IsAdopted)
            {
                m_NextPoseResetGeneration++;
                m_PoseDomain.Reset(m_NextPoseResetGeneration);
            }
        }

        public CharacterPresentationDomainDiagnosticsSnapshot CaptureDiagnostics() =>
            new(
                0,
                0,
                m_Body.FollowerPositionCorrectionMeters,
                m_Body.FollowerYawCorrectionDegrees);

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
            m_Equipment?.Present();
            CharacterBodyPresentationFrame bodyFrame = m_Body.Present(context);
            if (!bodyFrame.IsValid)
                return;
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
            RunPoseFrame(in bodyFrame, in factFrame, update.Frame, context);
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
                var parameterFrame = CharacterAnimationPoseInputFrame.FromPublishedVariables(
                    eventFrame,
                    m_PoseParameterIds);
                var frameInput = new CharacterPoseNativeFrameInput(
                    m_ActorId,
                    context.RenderFrame,
                    context.RenderFrame,
                    bodyFrame.CurrentTick,
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
                            m_PoseDomain.CommitFrame();
                        else
                            m_PoseDomain.DiscardFrame();
                    }
                    else
                    {
                        m_PoseDomain.Session.Discard(
                            validation.FailureCode != CharacterPoseNativeFailureCode.None
                                ? validation.FailureCode
                                : CharacterPoseNativeFailureCode.FrameInvalid);
                        m_PoseDomain.DiscardFrame();
                    }
                }
                else
                {
                    m_PoseDomain.DiscardFrame();
                }
            }
            catch
            {
                m_PoseDomain.DiscardFrame();
                throw;
            }
        }



        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            m_TimelineBridge?.Dispose();
            m_PoseDomain?.Dispose();
            m_PoseResourceScope?.Dispose();
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
                default,
                default,
                0d,
                m_Body.ResetSequence);
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
