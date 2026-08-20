using System;
using System.Collections.Generic;
using Animancer;
using BTSMTL.Diagnostics;
using BTSMTL.Timeline;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Pipeline.Animation.Lifecycle;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;
using ThirdPersonCharacter.Pipeline.Diagnostics;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonCharacter.Pipeline.Simulation;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline
{
    internal sealed class AnimationPreviewRuntime : IDisposable
    {
        readonly CharacterPresentationProjection m_Projection;
        readonly CharacterAnimationPresentationRuntime m_Playback;
        readonly CharacterEquipmentLinkedPoseRuntime m_LinkedPose;
        readonly CharacterEquipmentPreviewFixture
            m_EquipmentFixture;
        readonly CharacterFootPlacementRuntime m_FootPlacement;
        readonly bool m_WorldContextAvailable;
        readonly ActorId m_PreviewActorId;
        readonly Guid m_DiagnosticsOwnerId;
        readonly OperationHandle m_TimelineOperation;
        readonly TimelineActionPreviewAdapter
            m_TimelineActionPreview;
        readonly PoseGraphFactPreviewAdapter
            m_PoseGraphFactPreview;
        readonly HashSet<AnimationChannelId> m_SelectedChannels = new HashSet<AnimationChannelId>();
        readonly Dictionary<AnimationChannelId, ActivePreviewProducer> m_Active =
            new Dictionary<AnimationChannelId, ActivePreviewProducer>();
        readonly Dictionary<AnimationChannelId, ActivePreviewProducer> m_NextActive =
            new Dictionary<AnimationChannelId, ActivePreviewProducer>();
        readonly Dictionary<string, AnimationPlaybackId> m_TargetPlaybacks =
            new Dictionary<string, AnimationPlaybackId>(StringComparer.Ordinal);
        readonly List<AnimationPlaybackId>
            m_ReleasedPreviewPlaybacks =
                new List<AnimationPlaybackId>();
        ulong m_PresentationFrame;
        CharacterPosePlanStageSnapshot m_PosePlanStages;

        public AnimationPreviewRuntime(
            CharacterPipelineDefinition definition,
            CharacterSimulationProgram program,
            CharacterPresentationProjection projection,
            AnimancerComponent animancer,
            CharacterAnimationRigBinding animationRigBinding,
            CharacterPresentationBodyState bodyFixture,
            CharacterWorldAwarePresentationBinding worldAwareBinding,
            PhysicsScene physicsScene,
            CharacterEquipmentPreviewFixture equipmentFixture,
            TimelineData timeline,
            Guid previewSessionId)
        {
            if (!definition.AnimationPresentationProfile)
                throw new InvalidOperationException("Animation preview requires the Definition's Animation Presentation Profile.");
            if (program == null || projection == null)
                throw new InvalidOperationException("Animation preview requires compiled Program and Presentation Projection data.");
            if (previewSessionId == Guid.Empty)
                throw new ArgumentException("Animation preview identity is incomplete.");
            m_Projection = projection;
            CharacterPresentationSemanticContract contract =
                Float32CharacterPresentationContractAdapter.Create(program);
            m_Projection.RequireContract(contract);
            m_Projection.RequirePosePayload();
            m_Projection.RequireTuningPayload();
            m_PreviewActorId = new ActorId($"AnimationPreview/{previewSessionId:N}");
            m_LinkedPose = new CharacterEquipmentLinkedPoseRuntime(
                m_PreviewActorId,
                m_Projection);
            m_EquipmentFixture = equipmentFixture;
            CaptureLinkedPoseSelections();
            m_DiagnosticsOwnerId = previewSessionId;
            m_TimelineActionPreview =
                new TimelineActionPreviewAdapter(
                    program,
                    m_PreviewActorId);
            m_PoseGraphFactPreview =
            new PoseGraphFactPreviewAdapter(
                m_PreviewActorId,
                bodyFixture);
            m_TimelineOperation = timeline == null
                ? default
                : CharacterPipelinePreviewProgram.FindTimelineOperation(
                    program,
                    timeline.AuthoringId);
            CharacterAnimationPresentationBindings animationBindings =
                CharacterAnimationPresentationBindingFactory.Build(
                    contract,
                    m_Projection);
            CharacterMotionMatchingPresentationModule motionMatching = m_Projection.MotionMatching == null
                ? null
                : new CharacterMotionMatchingPresentationModule(
                    m_PreviewActorId,
                    CharacterBodyPresentationSourceMode.CommittedStream,
                    m_Projection);
            CharacterAnimationPresentationRuntime playback = null;
            CharacterFootPlacementRuntime footPlacement = null;
            try
            {
                playback = new CharacterAnimationPresentationRuntime(
                    m_PreviewActorId,
                    animationBindings,
                    motionMatching,
                    animancer,
                    animationRigBinding,
                    false);
                playback.SetDiagnosticsInterest(
                    m_DiagnosticsOwnerId,
                    AnimationPresentationDiagnosticsInterest.LiveState |
                    AnimationPresentationDiagnosticsInterest.OperationDetail |
                    AnimationPresentationDiagnosticsInterest.FinalPoseDetail);
                motionMatching = null;
                if (m_Projection.PosePlan.FootPlacements.Count == 1 && worldAwareBinding)
                {
                    worldAwareBinding.RequireValid();
                    if (!physicsScene.IsValid())
                        throw new InvalidOperationException("Pose Graph Preview Foot Placement requires the target Scene PhysicsScene.");
                    CharacterPresentationFootPlacementDescriptor descriptor =
                        m_Projection.PosePlan.FootPlacements[0];
                    CharacterFootPlacementPublicationValidation.Require(m_Projection, descriptor.Calibration);
                    var rig = new CharacterFootPlacementPoseRig(
                        descriptor.Calibration,
                        m_Projection.Rig,
                        animationRigBinding,
                        worldAwareBinding);
                    rig.RequireValid();
                    CharacterFootPlacementRuntimeSettings footPlacementSettings =
                        descriptor.Profile.BuildSettings(m_Projection, rig);
                    var worldQuery = new CharacterFootPlacementWorldQueryBackend(
                        physicsScene,
                        rig,
                        footPlacementSettings.LandingPrediction.HitCapacity,
                        footPlacementSettings.GroundDetection.SegmentHitCapacity);
                    footPlacement = new CharacterFootPlacementRuntime(
                        m_PreviewActorId,
                        footPlacementSettings,
                        rig,
                        null,
                        worldQuery);
                }
                var tuningTarget = new CharacterPoseTuningTargetIdentity(
                    m_PreviewActorId.Value,
                    m_Projection.ProgramId,
                    m_Projection.ProjectionRevision,
                    m_Projection.PosePlan.PlanHash,
                    m_Projection.Rig.RigId,
                    m_Projection.Rig.RigRevision,
                    m_Projection.TuningLayout.LayoutHash);
                playback.SetTuningBinding(
                    new CharacterPoseTuningRuntimeBinding(
                        tuningTarget,
                        m_Projection.TuningLayout,
                        m_Projection.TuningDefaultBlock,
                        m_Projection.PublishedParameterRevision));
                m_Playback = playback;
                m_FootPlacement = footPlacement;
                m_WorldContextAvailable =
                    m_Projection.PosePlan.FootPlacements.Count == 0 ||
                    m_FootPlacement != null;
            }
            catch
            {
                footPlacement?.Dispose();
                playback?.Dispose();
                motionMatching?.Dispose();
                throw;
            }
        }

        public bool HasDebugView =>
            m_Playback.HasDebugView;
        public AnimationPresentationDebugView DebugView =>
            m_Playback.DebugView;
        public CharacterPosePlanStageSnapshot PosePlanStages => m_PosePlanStages;

        internal CharacterPoseTuningRuntimeState TuningState =>
            m_Playback.TuningState;

        internal CharacterPoseTuningLayout TuningLayout =>
            m_Projection.TuningLayout;

        internal CharacterPoseTuningParameterBlock ActiveTuningBlock =>
            m_Playback.ActiveTuningBlock;

        internal bool SubmitPoseTuningCandidate(
            string sourceAuthoringRevision,
            string candidateRevision,
            CharacterPoseTuningParameterBlock block,
            out string error)
        {
            if (m_Projection.TuningLayout == null || block == null)
            {
                error = "Pose tuning payload is unavailable for this preview target.";
                return false;
            }
            var target = new CharacterPoseTuningTargetIdentity(
                m_PreviewActorId.Value,
                m_Projection.ProgramId,
                m_Projection.ProjectionRevision,
                m_Projection.PosePlan.PlanHash,
                m_Projection.Rig.RigId,
                m_Projection.Rig.RigRevision,
                m_Projection.TuningLayout.LayoutHash);
            return m_Playback.SubmitTuningCandidate(
                new CharacterPoseTuningCandidate(
                    target,
                    sourceAuthoringRevision,
                    candidateRevision,
                    block),
                out error);
        }

        internal bool SubmitPoseTuningCandidate(
            CharacterPoseTuningCandidate candidate,
            out string error) =>
            m_Playback.SubmitTuningCandidate(candidate, out error);

        internal void ClearPendingPoseTuningCandidate() =>
            m_Playback.ClearPendingTuningCandidate();

        public void SetLinkedPosePreviewOverride(
            LinkedPoseGroupId groupId,
            LinkedPoseImplementationId implementationId)
        {
            m_LinkedPose.SetPreviewOverride(groupId, implementationId);
        }

        public void ClearLinkedPosePreviewOverride(LinkedPoseGroupId groupId)
        {
            m_LinkedPose.ClearPreviewOverride(groupId);
        }

        public void ClearLinkedPosePreviewOverrides()
        {
            m_LinkedPose.ClearPreviewOverrides();
        }

        public void Evaluate(PreviewSession session)
        {
            if (session == null || !session.HasEvaluation)
                throw new ArgumentException("Timeline preview session has no evaluation.", nameof(session));

            m_SelectedChannels.Clear();
            m_NextActive.Clear();
            m_TargetPlaybacks.Clear();
            m_ReleasedPreviewPlaybacks.Clear();
            var tick = new SimulationTick(session.EvaluationTick);
            var activation = new ActivationId(
                m_TimelineOperation,
                session.Generation,
                $"timeline-preview:{session.Timeline.AuthoringId}");
            for (int trackIndex = 0; trackIndex < session.Timeline.Tracks.Count; trackIndex++)
            {
                if (session.Timeline.Tracks[trackIndex] is not AnimationTrack track)
                    continue;

                string producerIdentity = $"producer:{session.Timeline.AuthoringId}:{track.AuthoringId}";
                if (!m_Projection.TryGetProducer(producerIdentity, out CharacterPresentationProducerEntry producer) ||
                    producer.Kind != CharacterPresentationProducerKind.Animation ||
                    !producer.IsValid)
                    throw new InvalidOperationException(
                        $"Timeline preview producer '{producerIdentity}' has no compiled animation projection.");
                if (producer.Animation == null)
                    throw new InvalidOperationException(
                        $"Timeline preview producer '{producerIdentity}' is not a finite Action Timeline source. Open Pose Graph Preview for continuous Pose sources.");
                if (!m_SelectedChannels.Add(producer.AnimationChannelId))
                    throw new InvalidOperationException(
                        $"Timeline preview contains multiple selected producers for Animation Channel '{producer.AnimationChannelId}'.");

                var playbackId = new AnimationPlaybackId(producer.ProducerId, session.Generation);
                m_TargetPlaybacks[track.AuthoringId] = playbackId;
                var active = new ActivePreviewProducer(producer, session.Generation);
                m_NextActive.Add(producer.AnimationChannelId, active);
                bool alreadyActive =
                    m_Active.TryGetValue(
                        producer.AnimationChannelId,
                        out ActivePreviewProducer previous) &&
                    previous.Matches(active);
                if (!alreadyActive)
                {
                    if (previous.IsValid)
                    {
                        PublishRelease(
                            previous,
                            tick,
                            session.Timeline.AuthoringId,
                            session.CurrentTime);
                    }
                    m_Playback.Publish(
                        m_TimelineActionPreview.CreateCommand(
                            PresentationCommandKind
                                .SelectProducer,
                            producer,
                            session.Generation,
                            tick,
                            activation,
                            session.CurrentTime,
                            "preview.animation.select"),
                        producer);
                }
                m_Playback.Publish(
                    m_TimelineActionPreview.CreateCommand(
                        PresentationCommandKind.SampleProducer,
                        producer,
                        session.Generation,
                        tick,
                        activation,
                        session.CurrentTime,
                        "preview.animation.sample"),
                    producer);
            }

            foreach (KeyValuePair<AnimationChannelId, ActivePreviewProducer> item in m_Active)
            {
                if (m_NextActive.TryGetValue(
                        item.Key,
                        out ActivePreviewProducer next) &&
                    item.Value.Matches(next))
                {
                    continue;
                }
                if (!m_ReleasedPreviewPlaybacks.Contains(
                        item.Value.PlaybackId))
                {
                    PublishRelease(
                        item.Value,
                        tick,
                        session.Timeline.AuthoringId,
                        session.CurrentTime);
                }
            }

            m_Active.Clear();
            foreach (KeyValuePair<AnimationChannelId, ActivePreviewProducer> item in m_NextActive)
                m_Active.Add(item.Key, item.Value);
            CharacterBodyPresentationFrame bodyFrame =
                m_PoseGraphFactPreview.CreateBodyFrame(
                    session.EvaluationTick);
            ulong presentationFrame = ++m_PresentationFrame;
            CharacterPresentationFactFrame factFrame =
                m_PoseGraphFactPreview.CreateFactFrame(
                    presentationFrame,
                    session.EvaluationTick,
                    session.CurrentTime,
                    in bodyFrame);
            ComposedAnimationPoseFrame composed = m_Playback.Present(
                presentationFrame,
                session.EvaluationTick,
                1f,
                session.PresentationDeltaSeconds,
                in bodyFrame,
                in factFrame,
                m_LinkedPose.Session,
                m_FootPlacement,
                null);
            m_PosePlanStages = CharacterPosePlanStageSnapshotFactory.Preview(
                m_Projection.PosePlan,
                in composed,
                m_WorldContextAvailable);
            ForgetReleasedPreviewPlaybacks();
        }

        public void EvaluatePoseGraph(
            ulong evaluationTick,
            float presentationDeltaSeconds,
            double presentationTime,
            bool grounded,
            float horizontalSpeed,
            float horizontalAcceleration,
            float verticalSpeed,
            Vector2 movementDirection,
            Vector2 desiredDirection,
            float facingError,
            CharacterPresentationMotionPhase motionPhase,
            IReadOnlyList<PoseParameterId> directParameterIds = null,
            IReadOnlyList<float> directParameterValues = null)
        {
            if (evaluationTick == 0 ||
                !float.IsFinite(presentationDeltaSeconds) ||
                presentationDeltaSeconds < 0f)
            {
                throw new ArgumentException("Pose Graph Preview frame is invalid.");
            }

            CharacterBodyPresentationFrame bodyFrame =
                m_PoseGraphFactPreview.CreateBodyFrame(
                    evaluationTick,
                    grounded,
                    horizontalSpeed,
                    verticalSpeed,
                    movementDirection);
            ulong presentationFrame = ++m_PresentationFrame;
            CharacterPresentationFactFrame factFrame =
                m_PoseGraphFactPreview.CreateFactFrame(
                    presentationFrame,
                    evaluationTick,
                    presentationTime,
                    in bodyFrame,
                    grounded,
                    horizontalSpeed,
                    horizontalAcceleration,
                    verticalSpeed,
                    movementDirection,
                    desiredDirection,
                    facingError,
                    motionPhase);
            ComposedAnimationPoseFrame composed;
            if (directParameterIds != null && directParameterIds.Count > 0)
            {
                CharacterPresentationProgramParameterFrame parameterFrame =
                    CharacterPresentationProgramParameterFrame.FromDirect(
                        directParameterIds,
                        directParameterValues);
                composed = m_Playback.Present(
                    presentationFrame,
                    evaluationTick,
                    1f,
                    presentationDeltaSeconds,
                    in bodyFrame,
                    in factFrame,
                    in parameterFrame,
                    m_LinkedPose.Session,
                    m_FootPlacement,
                    null);
            }
            else
            {
                composed = m_Playback.Present(
                    presentationFrame,
                    evaluationTick,
                    1f,
                    presentationDeltaSeconds,
                    in bodyFrame,
                    in factFrame,
                    m_LinkedPose.Session,
                    m_FootPlacement,
                    null);
            }
            m_PosePlanStages = CharacterPosePlanStageSnapshotFactory.Preview(
                m_Projection.PosePlan,
                in composed,
                m_WorldContextAvailable);
        }

        public ComposedAnimationPoseFrame
            EvaluateMotionMatchingQuery(
                string providerId,
                MotionMatchingSearchReplayArtifact query)
        {
            if (string.IsNullOrWhiteSpace(providerId) ||
                query == null)
            {
                throw new ArgumentException(
                    "Motion Matching Query Preview input is invalid.");
            }
            ulong presentationFrame =
                ++m_PresentationFrame;
            m_Playback.CaptureMotionMatchingPreviewQuery(
                providerId,
                query);
            CharacterBodyPresentationFrame bodyFrame =
                m_PoseGraphFactPreview.CreateBodyFrame(
                    presentationFrame);
            CharacterPresentationFactFrame factFrame =
                m_PoseGraphFactPreview.CreateFactFrame(
                    presentationFrame,
                    presentationFrame,
                    0d,
                    in bodyFrame);
            ComposedAnimationPoseFrame composed =
                m_Playback.Present(
                    presentationFrame,
                    presentationFrame,
                    1f,
                    0f,
                    in bodyFrame,
                    in factFrame,
                    m_LinkedPose.Session,
                    m_FootPlacement,
                    null);
            m_PosePlanStages =
                CharacterPosePlanStageSnapshotFactory
                    .Preview(
                        m_Projection.PosePlan,
                        in composed,
                        m_WorldContextAvailable);
            return composed;
        }

        public void RetireAndReset(ulong evaluationTick)
        {
            ulong tickValue = Math.Max(1UL, evaluationTick);
            var tick = new SimulationTick(tickValue);
            m_ReleasedPreviewPlaybacks.Clear();
            foreach (ActivePreviewProducer active in m_Active.Values)
            {
                PublishRelease(
                    active,
                    tick,
                    "retire",
                    0f,
                    "preview.animation.seek-retire");
            }
            if (m_Active.Count > 0)
            {
                CharacterBodyPresentationFrame bodyFrame =
                    m_PoseGraphFactPreview.CreateBodyFrame(
                        tickValue);
                ulong presentationFrame = ++m_PresentationFrame;
                CharacterPresentationFactFrame factFrame =
                    m_PoseGraphFactPreview.CreateFactFrame(
                        presentationFrame,
                        tickValue,
                        0d,
                        in bodyFrame);
                ComposedAnimationPoseFrame composed = m_Playback.Present(
                    presentationFrame,
                    tickValue,
                    1f,
                    0f,
                    in bodyFrame,
                    in factFrame,
                    m_LinkedPose.Session,
                    m_FootPlacement,
                    null);
                m_PosePlanStages = CharacterPosePlanStageSnapshotFactory.Preview(
                    m_Projection.PosePlan,
                    in composed,
                    m_WorldContextAvailable);
                ForgetReleasedPreviewPlaybacks();
            }
            m_Playback.Reset(PoseDiscontinuityResetReason.PreviewSeek);
            m_LinkedPose.Reset();
            CaptureLinkedPoseSelections();
            ResetFootPlacement(tickValue);
            m_TimelineActionPreview.Reset();
            m_Active.Clear();
            m_NextActive.Clear();
            m_SelectedChannels.Clear();
            m_TargetPlaybacks.Clear();
            m_ReleasedPreviewPlaybacks.Clear();
            m_PosePlanStages = default;
        }

        public void SetPoseWatchInterests(Guid ownerId, IReadOnlyList<AnimationPoseWatchIdentity> interests) =>
            m_Playback.SetPoseWatchInterests(ownerId, interests);

        public void RemovePoseWatchInterests(Guid ownerId) => m_Playback.RemovePoseWatchInterests(ownerId);

        public void Dispose()
        {
            m_FootPlacement?.Dispose();
            m_Playback.RemoveDiagnosticsInterest(
                m_DiagnosticsOwnerId);
            m_Playback.Dispose();
        }

        void ResetFootPlacement(ulong renderFrame)
        {
            m_FootPlacement?.Reset(
                new CharacterFootPlacementReset(
                    m_PreviewActorId,
                    renderFrame,
                    1,
                    CharacterFootPlacementResetReason.PresentationReset,
                    CharacterBodyPresentationResetReason.Initialization));
        }

        void CaptureLinkedPoseSelections()
        {
            if (m_Projection.LinkedPose.EquipmentSelectors.Count == 0)
            {
                m_LinkedPose.Capture(
                    Array.Empty<EquipmentVisualSelection>());
                return;
            }
            if (!m_EquipmentFixture)
            {
                throw new InvalidOperationException(
                    "Linked Pose Preview requires an explicit CharacterEquipmentPreviewFixture with committed Equipment selections.");
            }
            m_LinkedPose.Capture(
                m_EquipmentFixture.BuildSelections(
                    m_PreviewActorId,
                    m_Projection.LinkedPose));
        }

        void PublishRelease(
            in ActivePreviewProducer active,
            SimulationTick tick,
            string timelineIdentity,
            float sampleTime,
            string channel =
                "preview.animation.release")
        {
            if (!active.IsValid)
            {
                throw new ArgumentException(
                    "Timeline Action Preview release target is invalid.",
                    nameof(active));
            }
            m_Playback.Publish(
                m_TimelineActionPreview.CreateCommand(
                    PresentationCommandKind.ReleaseProducer,
                    active.Producer,
                    active.Generation,
                    tick,
                    new ActivationId(
                        m_TimelineOperation,
                        active.Generation,
                        $"timeline-preview:{timelineIdentity}"),
                    sampleTime,
                    channel),
                active.Producer);
            m_ReleasedPreviewPlaybacks.Add(
                active.PlaybackId);
        }

        void ForgetReleasedPreviewPlaybacks()
        {
            for (int i = 0;
                 i < m_ReleasedPreviewPlaybacks.Count;
                 i++)
            {
                m_TimelineActionPreview.Forget(
                    m_ReleasedPreviewPlaybacks[i]);
            }
            m_ReleasedPreviewPlaybacks.Clear();
        }

        readonly struct ActivePreviewProducer
        {
            public ActivePreviewProducer(CharacterPresentationProducerEntry producer, ulong generation)
            {
                Producer = producer;
                Generation = generation;
            }

            public CharacterPresentationProducerEntry Producer { get; }
            public ulong Generation { get; }
            public bool IsValid =>
                Producer != null &&
                Producer.IsValid &&
                Generation != 0;
            public AnimationPlaybackId PlaybackId =>
                new AnimationPlaybackId(
                    Producer.ProducerId,
                    Generation);

            public bool Matches(
                in ActivePreviewProducer other) =>
                IsValid &&
                other.IsValid &&
                Producer.ProducerId.Equals(
                    other.Producer.ProducerId) &&
                Generation == other.Generation;
        }
    }
}
