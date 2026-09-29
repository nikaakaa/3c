using System.Runtime.ExceptionServices;
using System;
using System.Collections.Generic;
using BTSMTL.Diagnostics;
using BTSMTL.Timeline.Runtime;
using ThirdPersonCamera;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.Presentation;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;
using ThirdPersonCharacter.Pipeline.Animation.Resources;
using ThirdPersonGameplay.Tick;
using ThirdPersonSimulation;
using ThirdPersonPerformance.Instrumentation;
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
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Animation.Diagnostics.CharacterPoseWriteMonitor m_PoseWriteMonitor;

        internal void BindPoseWriteMonitor(CharacterAnimationRigBinding binding,
            CharacterAnimationRigPayload rig, CharacterRootHierarchyBinding roots)
        {
            m_PoseWriteMonitor = new Animation.Diagnostics.CharacterPoseWriteMonitor(
                m_ActorId.Value, binding, rig, roots);
        }
#endif

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

        CharacterAnimationVariableContract m_PoseVariableContract;
        ThirdPersonCharacter.Pipeline.Animation.Lifecycle.CharacterPoseActionCommandPublisher m_PoseActionPublisher;
        List<ActionAnimationPlaybackCommand> m_DiagnosticCommands;

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
        ThirdPersonCharacter.Pipeline.Animation.Lifecycle.TimelinePresentationEventBridge m_TimelinePresentationBridge;

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
            timelineHost.PresentationFramePrepared += OnTimelinePresentationFramePrepared;
            timelineHost.Initialize(numericTarget, tickRate);
            BindTimelineBridge(new ThirdPersonCharacter.Pipeline.Animation.Lifecycle.TimelineToActionCommandBridge(
                timelineHost, m_PoseActionPublisher.Inbox));
            m_TimelinePresentationBridge = new ThirdPersonCharacter.Pipeline.Animation.Lifecycle.TimelinePresentationEventBridge(
                timelineHost,
                this,
                m_ActorId,
                m_Camera != null ? m_Camera.RequestCapacity : 0);
        }

        internal void BindTimelineBridge(ThirdPersonCharacter.Pipeline.Animation.Lifecycle.TimelineToActionCommandBridge bridge)
        {
            m_TimelineBridge = bridge ?? throw new ArgumentNullException(nameof(bridge));
        }

        void OnTimelinePresentationFramePrepared(TimelineRuntimePresentationFrame frame)
        {
            m_PresentationClockCoordinator?.AcceptTimelinePresentationFrame(in frame);
        }

        internal void BindPoseActionPublisher(ThirdPersonCharacter.Pipeline.Animation.Lifecycle.CharacterPoseActionCommandPublisher publisher)
        {
            m_PoseActionPublisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
            m_DiagnosticCommands = new List<ActionAnimationPlaybackCommand>(publisher.Inbox.Capacity);
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
            m_PoseVariableContract = CharacterAnimationPoseInputFrame.BindContract(
                m_EventGraph.VariableContract, poseParameterIds);
        }

        public bool AcceptsTrajectoryIntent => true;
        public ulong BodyResetSequence => m_Body.ResetSequence;
        public CharacterLocomotionBodySource LocomotionBodySource => m_LocomotionBinding.BodySource;
        public bool IsPoseResourceReady => m_PoseDomain == null ||
            !m_PoseDomain.IsAdopted ||
            (m_PoseResourceScope?.IsReady ?? false);

        public void AdvancePoseResourcePreparation()
        {
            if (m_PoseDomain != null && m_PoseDomain.IsAdopted)
                m_PoseResourceScope.AdvancePreparation();
        }

        public bool SupportsCheckpointCapture => false;
        public bool SupportsCheckpointRestore => false;

        public bool TryGetCameraBasis(out CameraBasisSnapshot basis)
        {
            if (m_Camera == null)
            {
                basis = CameraBasisSnapshot.Invalid;
                return false;
            }
            basis = m_Camera.BasisSnapshot;
            return basis.Valid;
        }

        public void SetCameraInitialState(in CameraInitialState state)
        {
            if (m_Camera == null)
                throw new InvalidOperationException("Presentation runtime has no composed Camera domain.");
            m_Camera.SetInitialState(in state);
        }

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

        public void CaptureBodyStream(CharacterPresentationBodyInterval[] intervals, int count) =>
            m_Body.CaptureStreamTransaction(intervals, count);

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

        public void ConfirmTimelineHistory(ulong confirmedTick)
        {
            m_PresentationClockCoordinator?.ConfirmTimelineHistory(confirmedTick);
        }

        public void Publish(CharacterPresentationCommand command)
        {
            RequireActor(command.Header.ActorId);
            if (command.Kind == CharacterPresentationCommandKind.TimelineProgress)
            {
                if (m_PresentationClockCoordinator == null)
                    throw new InvalidOperationException("Timeline progress requires an installed Action presentation clock.");
                m_PresentationClockCoordinator.AcceptTimelineProgress(in command);
                return;
            }
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
            if (current.Kind == CharacterPresentationCommandKind.TimelineProgress || replacement.Kind == CharacterPresentationCommandKind.TimelineProgress)
            {
                if (m_PresentationClockCoordinator == null || current.Kind != replacement.Kind)
                    throw new InvalidOperationException("Timeline progress replacement has no matching Action clock contract.");
                if (current.SourceActionInstanceId != replacement.SourceActionInstanceId ||
                    !current.Header.Activation.Source.Equals(replacement.Header.Activation.Source) ||
                    current.TimelineProgress.Generation != replacement.TimelineProgress.Generation ||
                    !string.Equals(current.TimelineProgress.TimelineId, replacement.TimelineProgress.TimelineId, StringComparison.Ordinal))
                    m_PresentationClockCoordinator.RetireTimelineProgress(in current);
                m_PresentationClockCoordinator.AcceptTimelineProgress(in replacement);
                return;
            }
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
            if (command.Kind == CharacterPresentationCommandKind.TimelineProgress)
            {
                if (m_PresentationClockCoordinator == null)
                    throw new InvalidOperationException("Timeline progress retirement requires an Action presentation clock.");
                m_PresentationClockCoordinator.RetireTimelineProgress(in command);
                return;
            }
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
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            m_PoseWriteMonitor.Invalidate();
#endif
            m_TimelinePresentationBridge?.Reset();
            m_Camera?.Reset();
            m_Equipment?.Reset();
            m_Body.Reset();
            m_EventGraph.Reset();
            m_PresentationClockCoordinator?.Reset();
            m_EventFrame = default;
            m_Trajectory = default;
            m_LastLocomotionFactLineage = default;
            m_LocomotionFailureCode = LocomotionPresentationFailureCode.None;
            m_HasTrajectory = false;
            m_HasPoseDiscontinuityIdentity = false;
            m_PoseDiscontinuityIdentity = 0;
            ResetPose();
            PublishLocomotionDiagnostics();
        }

        public bool TryGetPoseDiagnosticTarget(out Animation.Diagnostics.CharacterPoseDiagnosticTarget target)
        {
            if (m_Disposed || m_PoseDomain == null || !m_PoseDomain.IsAdopted)
            {
                target = default;
                return false;
            }
            CharacterPoseNativeDomainSession session = m_PoseDomain.Session;
            target = new Animation.Diagnostics.CharacterPoseDiagnosticTarget(
                m_Diagnostics.CharacterRuntimeId, m_ActorId.Value, session.InstanceId,
                session.GraphRevision, session.ResourceRevision);
            return true;
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

        [PerformanceProbe("presentation.animation")]
        public void PresentationFrame(GameplayPresentationFrameContext context)
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(CharacterPresentationDomainRuntime));
            m_PoseDomain?.Session.RequireAvailable();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            m_PoseWriteMonitor.Invalidate();
#endif
            m_Diagnostics.BeginPresentationFrame(context.RenderFrame);
            m_Equipment?.Present();
            CharacterBodyPresentationFrame bodyFrame = m_Body.Present(context);
            if (!bodyFrame.IsValid)
                return;
            m_PresentationClockCoordinator?.BeginSamplingFrame();
            Exception failure = null;
            CharacterPoseNativePublicationResult publication = default;
            AnimationPresentationFramePhase phase = AnimationPresentationFramePhase.Begin;
            try
            {
                m_Camera?.BeginFrame();
                m_TimelinePresentationBridge?.BeginFrame();
                m_TimelineBridge?.BeginFrame();
                if (m_PoseDomain != null && m_PoseDomain.IsAdopted)
                {
                    m_PoseResourceScope.AdvancePreparation();
                    if (!IsPoseResourceReady)
                        return;
                }
                CharacterPresentationFactFrame factFrame = CreateFactFrame(in bodyFrame, context.RenderFrame);
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
                if (m_TimelineHost != null)
                {
                    Vector3 position = bodyFrame.TargetPosition;
                    Vector3 velocity = factFrame.Velocity;
                    var graphFacts = new Float32PresentationGraphFacts(context.RenderFrame,
                        new Float32Vector3(Float32Scalar.FromSingle(position.x), Float32Scalar.FromSingle(position.y), Float32Scalar.FromSingle(position.z)),
                        new Float32Vector3(Float32Scalar.FromSingle(velocity.x), Float32Scalar.FromSingle(velocity.y), Float32Scalar.FromSingle(velocity.z)),
                        new Float32Yaw(Float32Scalar.FromSingle(factFrame.Rotation.eulerAngles.y)), factFrame.Grounded,
                        factFrame.BodyDiscontinuityGeneration);
                    m_TimelineHost.Present(context, m_PresentationClockCoordinator, in graphFacts);
                }
                IReadOnlyList<ActionAnimationPlaybackCommand> actionCommands = Array.Empty<ActionAnimationPlaybackCommand>();
                if (m_PoseDomain != null && m_PoseDomain.IsAdopted)
                {
                    m_PoseDomain.BeginFrame(context.RenderFrame);
                    if (!m_PoseDomain.TryGetCommands(m_ActorId, context.RenderFrame, out actionCommands))
                        throw new InvalidOperationException("Pose Action command source did not produce the opened frame commands.");
                    m_PresentationClockCoordinator?.BeginFrame(actionCommands);
                    PublishAnimationCommands(actionCommands);
                }
#if KK_DIAGNOSTIC_SAMPLING
                m_DiagnosticCommands?.Clear();
                if (Animation.Diagnostics.CharacterNativePresentationDiagnosticEvent.IsInterested(m_Diagnostics.CharacterRuntimeId))
                    for (int i = 0; i < actionCommands.Count; i++)
                        m_DiagnosticCommands.Add(actionCommands[i]);
#endif
                m_Camera?.ValidateFrame();
                if (!RunPoseFrame(in bodyFrame, in factFrame, update.Frame, context, actionCommands, out publication))
                    return;
                phase = AnimationPresentationFramePhase.TimelineCommit;
                m_TimelineHost?.CommitPresentationFrame(context.RenderFrame, m_PresentationClockCoordinator);
                phase = AnimationPresentationFramePhase.ActionBridgeCommit;
                m_TimelineBridge?.CommitFrame();
                phase = AnimationPresentationFramePhase.SamplingClockCommit;
                m_PresentationClockCoordinator?.CommitSamplingFrame();
                phase = AnimationPresentationFramePhase.CameraCommit;
                m_Camera?.CommitFrame();
                phase = AnimationPresentationFramePhase.PresentationBridgeCommit;
                m_TimelinePresentationBridge?.CommitFrame();
                phase = AnimationPresentationFramePhase.CameraPresent;
                m_Camera?.Present(bodyFrame, context);
                PublishCommittedPoseObservations(in publication, in bodyFrame, in factFrame, context, m_DiagnosticCommands);
            }
            catch (Exception exception)
            {
                failure = exception;
            }
            finally
            {
                if (failure == null)
                    phase = AnimationPresentationFramePhase.FrameCleanup;
                try { m_PoseDomain?.DiscardFrame(); }
                catch (Exception cleanup) { CharacterPresentationCleanup.Record(ref failure, cleanup); }
                try { m_PresentationClockCoordinator?.DiscardFrame(); }
                catch (Exception cleanup) { CharacterPresentationCleanup.Record(ref failure, cleanup); }
                try { m_TimelineHost?.DiscardPresentationFrame(context.RenderFrame); }
                catch (Exception cleanup) { CharacterPresentationCleanup.Record(ref failure, cleanup); }
                try { m_TimelineBridge?.DiscardFrame(); }
                catch (Exception cleanup) { CharacterPresentationCleanup.Record(ref failure, cleanup); }
                try { m_Camera?.DiscardFrame(); }
                catch (Exception cleanup) { CharacterPresentationCleanup.Record(ref failure, cleanup); }
                try { m_TimelinePresentationBridge?.DiscardFrame(); }
                catch (Exception cleanup) { CharacterPresentationCleanup.Record(ref failure, cleanup); }
                try { m_PresentationClockCoordinator?.DiscardSamplingFrame(); }
                catch (Exception cleanup) { CharacterPresentationCleanup.Record(ref failure, cleanup); }
                if (failure != null)
                {
                    if (publication.Status == CharacterPoseNativeFrameStatus.Committed)
                    {
                        var fault = new AnimationPresentationFault(m_ActorId, context.RenderFrame,
                            bodyFrame.CurrentTick, phase, publication.Lineage.CompletionIdentity);
                        failure = m_PoseDomain.Session.RecordPresentationFault(in fault, failure);
                    }
#if KK_DIAGNOSTIC_SAMPLING
                    Animation.Diagnostics.CharacterPoseCaptureFailure.Report(m_Diagnostics.CharacterRuntimeId,
                        Animation.Diagnostics.CharacterNativePresentationDiagnosticEvent.EventId, failure);
#endif
                    ExceptionDispatchInfo.Capture(failure).Throw();
                }
            }
        }

        void PublishCommittedPoseObservations(in CharacterPoseNativePublicationResult publication,
            in CharacterBodyPresentationFrame body,
            in CharacterPresentationFactFrame facts, GameplayPresentationFrameContext context,
            IReadOnlyList<ActionAnimationPlaybackCommand> commands)
        {
            if (publication.Status != CharacterPoseNativeFrameStatus.Committed)
                return;
            try
            {
                CharacterPoseNativeFrameLineage lineage = publication.Lineage;
                m_PoseDomain.Session.PublishFootDiagnostics(m_Diagnostics.CharacterRuntimeId, in lineage);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                m_PoseWriteMonitor.Capture(context.RenderFrame, lineage.CompletionIdentity);
#endif
#if KK_DIAGNOSTIC_SAMPLING
                if (!Animation.Diagnostics.CharacterNativePresentationDiagnosticEvent.IsInterested(m_Diagnostics.CharacterRuntimeId))
                    return;
                var diagnosticFrame = m_PoseDomain.Session.CaptureDiagnosticFrame(in lineage);
                var animation = m_PoseDomain.Session.CapturePresentationDiagnostics(in diagnosticFrame);
                var bodyCapture = new Animation.Diagnostics.CharacterNativeBodyCaptureFrame(in body, in facts);
                var commandCapture = new Animation.Diagnostics.CharacterNativeCommandCaptureFrame(commands);
                var camera = default(Animation.Diagnostics.CharacterNativeCameraCaptureFrame);
                if (m_Camera != null)
                {
                    var plan = m_Camera.AppliedPlan;
                    var basis = m_Camera.BasisSnapshot;
                    camera = new Animation.Diagnostics.CharacterNativeCameraCaptureFrame(
                        context.RenderFrame, body.ResetSequence, context.PresentationDeltaSeconds, in plan, in basis, m_Camera.AppliedTargetValid, m_Camera.AppliedResetReason,
                        in context, m_Camera.EffectContributions, m_Camera.ProfileId, m_Camera.ProfileRevision);
                }
                Animation.Diagnostics.CharacterNativePresentationDiagnosticEvent.Publish(
                    m_Diagnostics.CharacterRuntimeId, in diagnosticFrame,
                    in animation, in camera, in bodyCapture, in commandCapture);
#endif
            }
            catch (Exception exception)
            {
#if KK_DIAGNOSTIC_SAMPLING
                Animation.Diagnostics.CharacterPoseCaptureFailure.Report(m_Diagnostics.CharacterRuntimeId,
                    Animation.Diagnostics.CharacterNativePresentationDiagnosticEvent.EventId, exception);
#else
                Animation.Diagnostics.CharacterPoseCaptureFailure.Report(m_Diagnostics.CharacterRuntimeId,
                    nameof(PublishCommittedPoseObservations), exception);
#endif
            }
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

        void PublishAnimationCommands(IReadOnlyList<ActionAnimationPlaybackCommand> commands)
        {
            for (int i = 0; i < commands.Count; i++)
            {
                ActionAnimationPlaybackCommand command = commands[i];
                RuntimeTraceEventKind eventKind = command.Kind switch
                {
                    ActionAnimationPlaybackCommandKind.Select => RuntimeTraceEventKind.AnimationSelectionSubmitted,
                    ActionAnimationPlaybackCommandKind.Sample or
                        ActionAnimationPlaybackCommandKind.ProjectedSample => RuntimeTraceEventKind.AnimationProducerSampled,
                    ActionAnimationPlaybackCommandKind.Complete => RuntimeTraceEventKind.AnimationPlaybackCompleted,
                    ActionAnimationPlaybackCommandKind.Release => RuntimeTraceEventKind.AnimationPlaybackReleased,
                    ActionAnimationPlaybackCommandKind.Withdraw => RuntimeTraceEventKind.AnimationPlaybackRetired,
                    _ => RuntimeTraceEventKind.None
                };
                if (eventKind == RuntimeTraceEventKind.None ||
                    !m_Diagnostics.ShouldPublish(RuntimeTraceChannel.Animation, eventKind))
                {
                    continue;
                }
                float time = 0f;
                float secondaryTime = 0f;
                float weight = 0f;
                int cycle = 0;
                bool loop = false;
                ulong inputSequence = 0;
                if (command.Kind == ActionAnimationPlaybackCommandKind.Sample)
                {
                    ActionCommittedRawSample sample = command.CommittedRawSample;
                    time = sample.VisualTime;
                    secondaryTime = (float)sample.ContinuousVisualTime;
                    weight = sample.ProducerWeight;
                    cycle = sample.Cycle;
                    loop = sample.Loop;
                    inputSequence = sample.CommittedSequence;
                }
                else if (command.Kind == ActionAnimationPlaybackCommandKind.ProjectedSample)
                {
                    ActionProjectedSample sample = command.ProjectedSample;
                    time = sample.Time.SampleTime;
                    secondaryTime = (float)sample.Time.ContinuousTime;
                    weight = sample.ProducerWeight;
                    cycle = sample.Time.Cycle;
                    loop = sample.Time.Loop;
                }
                m_Diagnostics.Publish(
                    RuntimeTraceChannel.Animation,
                    RuntimeTraceDomain.Presentation,
                    eventKind,
                    RuntimeSourceElementHandle.Invalid,
                    RuntimeInstanceKey.Character(m_Diagnostics.CharacterRuntimeId),
                    new RuntimeTracePayload
                    {
                        Status = command.Kind.ToString(),
                        Name = command.ProgramProducerId,
                        OwnerId = command.PlaybackId.ToString(),
                        RelatedElementId = command.EventId.IsValid
                            ? command.EventId.ToString()
                            : command.ProjectedSample.IsValid && command.ProjectedSample.AuthoringClipIdentity
                                ? $"{command.ProjectedSample.AuthoringClipIdentity.name}#{command.ProjectedSample.AuthoringClipIdentity.GetInstanceID()}"
                                : string.Empty,
                        AnimationChannelId = command.AnimationChannelId.Value,
                        ActionInstanceId = command.ActionInstanceId,
                        ActivationGeneration = command.Generation,
                        InputSequence = inputSequence,
                        Time = time,
                        SecondaryTime = secondaryTime,
                        Weight = weight,
                        Cycle = cycle,
                        Flag = loop,
                        Detail = $"logicTick={command.LocalLogicTick};playback={command.PlaybackId};channel={command.AnimationChannelId.Value};action={command.ActionInstanceId};generation={command.Generation};time={time:0.######};continuous={secondaryTime:0.######};cycle={cycle};weight={weight:0.######};loop={loop}"
                    });
            }
        }

        bool RunPoseFrame(
            in CharacterBodyPresentationFrame bodyFrame,
            in CharacterPresentationFactFrame factFrame,
            CharacterAnimationVariableFrame eventFrame,
            GameplayPresentationFrameContext context,
            IReadOnlyList<ActionAnimationPlaybackCommand> actionCommands,
            out CharacterPoseNativePublicationResult publication)
        {
            publication = default;
            if (m_PoseDomain == null || !m_PoseDomain.IsAdopted)
                return m_TimelineHost == null;
            float deltaSeconds = Mathf.Max(0f, context.PresentationDeltaSeconds);
            var parameterFrame = CharacterAnimationPoseInputFrame.FromPublishedVariables(
                eventFrame,
                m_PoseVariableContract);
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
            publication = m_PoseDomain.Session.RunFrame(
                in frameInput, m_Diagnostics.CharacterRuntimeId, m_PresentationClockCoordinator);
            return true;
        }



        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            Exception failure = null;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            CharacterPresentationCleanup.Dispose(m_PoseWriteMonitor, ref failure);
#endif
            CharacterPresentationCleanup.Dispose(m_TimelineBridge, ref failure);
            CharacterPresentationCleanup.Dispose(m_TimelinePresentationBridge, ref failure);
            if (m_TimelineHost != null)
                m_TimelineHost.PresentationFramePrepared -= OnTimelinePresentationFramePrepared;
            m_TimelineHost = null;
            CharacterPresentationCleanup.Dispose(m_PoseDomain, ref failure);
            CharacterPresentationCleanup.Dispose(m_PoseResourceScope, ref failure);
            CharacterPresentationCleanup.Dispose(m_PresentationClockCoordinator, ref failure);
            CharacterPresentationCleanup.Dispose(m_Camera, ref failure);
            CharacterPresentationCleanup.Dispose(m_Equipment, ref failure);
            CharacterPresentationCleanup.Dispose(m_EventGraph, ref failure);
            CharacterPresentationCleanup.Dispose(m_Body, ref failure);
            if (failure != null)
                ExceptionDispatchInfo.Capture(failure).Throw();
        }

        CharacterAnimationVariableContract ICharacterPoseNativeEventFrameSource.VariableContract =>
            m_EventGraph.VariableContract;

        bool ICharacterPoseNativeEventFrameSource.TryGetFrame(
            ActorId actorId,
            ulong frameIdentity,
            out CharacterAnimationVariableFrame frame)
        {
            frame = m_EventFrame;
            return actorId == m_ActorId && frame.IsValid && frame.RenderFrame == frameIdentity;
        }

        CharacterPresentationFactFrame CreateFactFrame(
            in CharacterBodyPresentationFrame bodyFrame,
            ulong renderFrame)
        {
            var identity = new CharacterPresentationFactFrameIdentity(
                m_ActorId,
                renderFrame);
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
            CharacterPoseNativeResetResult result = m_PoseDomain.Reset(m_NextPoseResetGeneration);
            if (!result.IsReset)
                throw new InvalidOperationException(
                    $"Pose graph reset failed: {result.FailureCode} {result.Message}");
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
