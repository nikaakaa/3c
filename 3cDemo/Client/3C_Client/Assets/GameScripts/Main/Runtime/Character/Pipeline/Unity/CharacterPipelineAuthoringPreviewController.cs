using System;
using System.Collections.Generic;
using System.Linq;
using Animancer;
using BTSMTL.Timeline;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Pipeline.Animation.Lifecycle;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonCharacter.Pipeline.Simulation;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline
{
    internal sealed class CharacterPipelinePreviewController : IDisposable
    {
        readonly CharacterPipelineHost m_Host;
        readonly CharacterPipelineDefinition m_Definition;
        readonly AnimancerComponent m_Animancer;
        readonly CharacterAnimationRigBinding m_AnimationRigBinding;
        readonly Float32WorldBodyBinding m_WorldBodyBinding;
        readonly CharacterWorldAwarePresentationBinding m_WorldAwareBinding;
        readonly CharacterEquipmentPreviewFixture
            m_EquipmentFixture;
        readonly CharacterPresentationBodyState m_BodyFixture;
        readonly PhysicsScene m_PhysicsScene;
        readonly CharacterSimulationProgram m_Program;
        readonly CharacterPresentationProjection m_Projection;
        readonly TimelineMotionAuthoringPreviewEvaluator m_MotionPreview =
            new TimelineMotionAuthoringPreviewEvaluator();
        PreviewSession m_Session;
        CharacterPoseTuningParameterBlock m_PendingPoseTuningBlock;
        string m_PendingPoseTuningSourceRevision = string.Empty;
        string m_PendingPoseTuningCandidateRevision = string.Empty;
        Guid m_SessionId;
        ulong m_Generation;
        bool m_OwnsGraphClock;
        bool m_HasOriginalVisualPose;
        Vector3 m_OriginalVisualPosition;
        Quaternion m_OriginalVisualRotation;
        readonly Dictionary<LinkedPoseGroupId, LinkedPoseImplementationId> m_LinkedPosePreviewOverrides =
            new Dictionary<LinkedPoseGroupId, LinkedPoseImplementationId>();

        public CharacterPipelinePreviewController(CharacterPipelineHost host)
        {
            m_Host = host ? host : throw new ArgumentNullException(nameof(host));
            m_Definition = host.Definition ? host.Definition : throw new InvalidOperationException("Animation preview requires a Character Pipeline Definition.");
            m_Animancer = host.Animancer ? host.Animancer : throw new InvalidOperationException("Animation preview requires Animancer.");
            m_AnimationRigBinding = host.AnimationRigBinding
                ? host.AnimationRigBinding
                : throw new InvalidOperationException("Animation preview requires an Animation Rig Binding.");
            m_WorldBodyBinding = host.WorldBodyBinding
                ? host.WorldBodyBinding
                : throw new InvalidOperationException("Animation preview requires the target's formal World Body Binding fixture.");
            if (m_WorldBodyBinding.gameObject.scene != host.gameObject.scene)
                throw new InvalidOperationException("Animation preview Body fixture must belong to the target Scene.");
            if (!m_Definition.SimulationProgram || !m_Definition.PresentationProjection)
                throw new InvalidOperationException("Animation preview requires compiled Program and Presentation Projection assets.");
            m_Program = m_Definition.SimulationProgram.Load();
            m_Projection = m_Definition.PresentationProjection.Load(
                Float32CharacterPresentationContractAdapter.Create(m_Program));
            m_AnimationRigBinding.RequireValid(m_Projection.Rig);
            m_BodyFixture = CharacterPresentationBodyState.FromFloat32(
                m_WorldBodyBinding.InitialBody);
            m_PhysicsScene = host.gameObject.scene.GetPhysicsScene();
            m_WorldAwareBinding = ResolveWorldAwareBinding(host);
            m_EquipmentFixture = ResolveEquipmentFixture(host);
        }

        public bool HasAnimationDebugView =>
            m_Session != null &&
            m_Session.Engine.HasDebugView;
        public AnimationPresentationDebugView AnimationDebugView =>
            m_Session != null
                ? m_Session.Engine.DebugView
                : throw new InvalidOperationException(
                    "Animation Preview Debug View is unavailable.");
        public CharacterPosePlanStageSnapshot PosePlanStages =>
            m_Session != null
                ? m_Session.Engine.PosePlanStages
                : default;

        internal CharacterPoseTuningLayout TuningLayout =>
            m_Session?.Engine.TuningLayout ?? m_Projection.TuningLayout;

        internal CharacterPoseTuningParameterBlock ActiveTuningBlock =>
            m_Session?.Engine.ActiveTuningBlock ?? m_Projection.TuningDefaultBlock;

        internal CharacterPoseTuningRuntimeState TuningState =>
            m_Session?.Engine.TuningState ?? new CharacterPoseTuningRuntimeState(
                CharacterPoseTuningRuntimeStatus.Applied,
                m_Projection.PublishedParameterRevision,
                string.Empty,
                string.Empty,
                0,
                string.Empty);

        internal bool SubmitPoseTuningCandidate(
            string sourceAuthoringRevision,
            string candidateRevision,
            CharacterPoseTuningParameterBlock block,
            out string error)
        {
            if (block == null)
            {
                error = "Pose tuning block is missing.";
                return false;
            }
            if (m_Session == null)
            {
                m_PendingPoseTuningBlock = block.Clone();
                m_PendingPoseTuningSourceRevision = sourceAuthoringRevision ?? string.Empty;
                m_PendingPoseTuningCandidateRevision = candidateRevision ?? string.Empty;
                error = string.Empty;
                return true;
            }
            return m_Session.Engine.SubmitPoseTuningCandidate(
                sourceAuthoringRevision,
                candidateRevision,
                block,
                out error);
        }

        public void SetLinkedPosePreviewOverride(
            Guid sessionId,
            LinkedPoseGroupId groupId,
            LinkedPoseImplementationId implementationId)
        {
            if (sessionId == Guid.Empty)
                throw new ArgumentException("Linked Pose preview session identity is incomplete.", nameof(sessionId));
            if (m_Session != null && m_SessionId != sessionId)
                throw new InvalidOperationException(
                    $"Animation preview target '{m_Host.name}' is already owned by session '{m_SessionId}'.");
            m_LinkedPosePreviewOverrides[groupId] = implementationId;
            m_Session?.Engine.SetLinkedPosePreviewOverride(groupId, implementationId);
        }

        public void ClearLinkedPosePreviewOverride(
            Guid sessionId,
            LinkedPoseGroupId groupId)
        {
            if (sessionId == Guid.Empty ||
                m_Session != null && m_SessionId != sessionId)
                return;
            m_LinkedPosePreviewOverrides.Remove(groupId);
            m_Session?.Engine.ClearLinkedPosePreviewOverride(groupId);
        }

        public void ClearLinkedPosePreviewOverrides(Guid sessionId)
        {
            if (sessionId == Guid.Empty ||
                m_Session != null && m_SessionId != sessionId)
                return;
            m_LinkedPosePreviewOverrides.Clear();
            m_Session?.Engine.ClearLinkedPosePreviewOverrides();
        }

        public bool Matches(CharacterPipelineDefinition definition, AnimancerComponent animancer)
        {
            return m_Definition == definition &&
                   m_Animancer == animancer &&
                   m_AnimationRigBinding == m_Host.AnimationRigBinding &&
                   m_WorldBodyBinding == m_Host.WorldBodyBinding &&
                   m_WorldAwareBinding == ResolveWorldAwareBinding(m_Host) &&
                   m_EquipmentFixture == ResolveEquipmentFixture(m_Host);
        }

        public void Evaluate(
            Guid sessionId,
            TimelineData timeline,
            float previousTime,
            float currentTime,
            string sourceId,
            string sourceName,
            ulong evaluationTick,
            float presentationDeltaSeconds,
            bool resetLifecycle)
        {
            if (sessionId == Guid.Empty || timeline == null)
                throw new ArgumentException("Timeline preview identity is incomplete.");
            if (evaluationTick == 0)
                throw new InvalidOperationException("Timeline preview evaluation tick must be non-zero.");
            if (m_Session != null && m_SessionId != sessionId)
                throw new InvalidOperationException(
                    $"Timeline preview target '{m_Host.name}' is already owned by session '{m_SessionId}'.");

            bool created = EnsureSession(sessionId, timeline);

            if (resetLifecycle && !created)
            {
                m_Session.Engine.RetireAndReset(evaluationTick);
                m_Session.Generation = NextGeneration();
            }

            m_Session.Capture(
                timeline,
                previousTime,
                currentTime,
                sourceId,
                sourceName,
                evaluationTick,
                presentationDeltaSeconds);
            m_Session.Engine.Evaluate(m_Session);
            ApplyMotionPreview(timeline, currentTime);
        }

        public void EvaluatePoseGraph(
            Guid sessionId,
            double presentationTime,
            ulong evaluationTick,
            float presentationDeltaSeconds,
            bool resetLifecycle,
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
            if (sessionId == Guid.Empty || evaluationTick == 0)
                throw new ArgumentException("Pose Graph Preview identity is incomplete.");
            if (m_Session != null && m_SessionId != sessionId)
                throw new InvalidOperationException(
                    $"Animation preview target '{m_Host.name}' is already owned by session '{m_SessionId}'.");

            bool created = EnsureSession(sessionId, null);

            if (resetLifecycle && !created)
            {
                m_Session.Engine.RetireAndReset(evaluationTick);
                m_Session.Generation = NextGeneration();
            }

            m_Session.Engine.EvaluatePoseGraph(
                evaluationTick,
                presentationDeltaSeconds,
                presentationTime,
                grounded,
                horizontalSpeed,
                horizontalAcceleration,
                verticalSpeed,
                movementDirection,
                desiredDirection,
                facingError,
                motionPhase,
                directParameterIds,
                directParameterValues);
        }

        public bool TrySetPoseWatchInterests(
            Guid sessionId,
            Guid ownerId,
            IReadOnlyList<AnimationPoseWatchIdentity> interests)
        {
            if (m_Session == null || m_SessionId != sessionId)
                return false;
            m_Session.Engine.SetPoseWatchInterests(ownerId, interests);
            return true;
        }

        public void RemovePoseWatchInterests(Guid ownerId)
        {
            m_Session?.Engine.RemovePoseWatchInterests(ownerId);
        }

        public void Clear(Guid sessionId)
        {
            if (sessionId == Guid.Empty || m_Session == null || m_SessionId != sessionId)
                return;
            ClearSession();
        }

        public void ClearPoseTuningCandidate()
        {
            m_PendingPoseTuningBlock = null;
            m_PendingPoseTuningSourceRevision = string.Empty;
            m_PendingPoseTuningCandidateRevision = string.Empty;
            m_Session?.Engine.ClearPendingPoseTuningCandidate();
        }

        public void Dispose()
        {
            ClearSession();
        }

        bool EnsureSession(Guid sessionId, TimelineData timeline)
        {
            if (m_Session != null)
            {
                if (m_SessionId != sessionId)
                    throw new InvalidOperationException(
                        $"Animation preview target '{m_Host.name}' is already owned by session '{m_SessionId}'.");
                return false;
            }
            CaptureVisualPose();
            var runtime = new AnimationPreviewRuntime(
                m_Definition,
                m_Program,
                m_Projection,
                m_Animancer,
                m_AnimationRigBinding,
                m_BodyFixture,
                m_WorldAwareBinding,
                m_PhysicsScene,
                m_EquipmentFixture,
                timeline,
                sessionId);
            ApplyLinkedPosePreviewOverrides(runtime);
            m_Session = new PreviewSession(
                NextGeneration(),
                runtime);
            m_SessionId = sessionId;
            AcquireGraphClock();
            ApplyPendingPoseTuningCandidate();
            return true;
        }

        void ClearSession()
        {
            ClearPoseTuningCandidate();
            m_Session?.Dispose();
            m_Session = null;
            m_SessionId = Guid.Empty;
            RestoreVisualPose();
            m_HasOriginalVisualPose = false;
            ReleaseGraphClock();
        }

        void ApplyPendingPoseTuningCandidate()
        {
            if (m_PendingPoseTuningBlock == null)
                return;
            if (!m_Session.Engine.SubmitPoseTuningCandidate(
                    m_PendingPoseTuningSourceRevision,
                    m_PendingPoseTuningCandidateRevision,
                    m_PendingPoseTuningBlock,
                    out string error))
            {
                ClearPoseTuningCandidate();
                throw new InvalidOperationException(error);
            }
            m_PendingPoseTuningBlock = null;
            m_PendingPoseTuningSourceRevision = string.Empty;
            m_PendingPoseTuningCandidateRevision = string.Empty;
        }

        void ApplyLinkedPosePreviewOverrides(AnimationPreviewRuntime runtime)
        {
            foreach (KeyValuePair<LinkedPoseGroupId, LinkedPoseImplementationId> value in
                     m_LinkedPosePreviewOverrides)
                runtime.SetLinkedPosePreviewOverride(value.Key, value.Value);
        }

        void AcquireGraphClock()
        {
            if (m_OwnsGraphClock)
                return;
            m_Animancer.Graph.PauseGraph();
            m_OwnsGraphClock = true;
        }

        void ReleaseGraphClock()
        {
            if (!m_OwnsGraphClock)
                return;
            if (!Application.isPlaying && m_Animancer && m_Animancer.IsGraphInitialized)
                m_Animancer.Graph.UnpauseGraph();
            m_OwnsGraphClock = false;
        }

        ulong NextGeneration()
        {
            m_Generation++;
            if (m_Generation == 0)
                m_Generation++;
            return m_Generation;
        }

        CharacterWorldAwarePresentationBinding ResolveWorldAwareBinding(
            CharacterPipelineHost host)
        {
            if (m_Projection.PosePlan.FootPlacements.Count == 0)
                return null;
            CharacterWorldAwarePresentationBinding binding =
                host.WorldAwarePresentation;
            if (!binding ||
                binding.gameObject.scene != host.gameObject.scene ||
                binding.PresentationRoot != host.VisualRoot ||
                !m_PhysicsScene.IsValid())
            {
                return null;
            }
            try
            {
                binding.RequireValid();
                return binding;
            }
            catch
            {
                return null;
            }
        }

        CharacterEquipmentPreviewFixture ResolveEquipmentFixture(
            CharacterPipelineHost host)
        {
            CharacterEquipmentPreviewFixture fixture =
                host.EquipmentPreviewFixture;
            if (m_Projection.LinkedPose.EquipmentSelectors.Count == 0)
                return fixture;
            if (!fixture || fixture.gameObject != host.gameObject)
            {
                throw new InvalidOperationException(
                    "Linked Pose Preview requires a CharacterEquipmentPreviewFixture on the target CharacterPipelineHost.");
            }
            return fixture;
        }

        void CaptureVisualPose()
        {
            if (m_HasOriginalVisualPose)
                return;
            Transform visualRoot = m_Host.VisualRoot;
            if (!visualRoot)
                throw new InvalidOperationException("Timeline preview requires a visual root.");
            m_OriginalVisualPosition = visualRoot.position;
            m_OriginalVisualRotation = visualRoot.rotation;
            m_HasOriginalVisualPose = true;
        }

        void RestoreVisualPose()
        {
            if (!m_HasOriginalVisualPose || !m_Host.VisualRoot)
                return;
            m_Host.VisualRoot.SetPositionAndRotation(
                m_OriginalVisualPosition,
                m_OriginalVisualRotation);
        }

        void ApplyMotionPreview(TimelineData timeline, float time)
        {
            if (!m_HasOriginalVisualPose || !m_Host.VisualRoot)
                return;
            TimelineMotionPreviewPose pose = m_MotionPreview.Evaluate(
                timeline,
                time,
                m_OriginalVisualRotation);
            m_Host.VisualRoot.SetPositionAndRotation(
                m_OriginalVisualPosition + pose.WorldDisplacement,
                m_OriginalVisualRotation * Quaternion.Euler(0f, pose.YawDegrees, 0f));
        }
    }

    internal readonly struct TimelineMotionPreviewPose
    {
        public TimelineMotionPreviewPose(Vector3 worldDisplacement, float yawDegrees)
        {
            WorldDisplacement = worldDisplacement;
            YawDegrees = yawDegrees;
        }

        public Vector3 WorldDisplacement { get; }
        public float YawDegrees { get; }
    }

    internal sealed class TimelineMotionAuthoringPreviewEvaluator
    {
        readonly List<TimelineMotionCurveContribution> m_Contributions =
            new List<TimelineMotionCurveContribution>();

        public TimelineMotionPreviewPose Evaluate(
            TimelineData timeline,
            float time,
            Quaternion originRotation)
        {
            if (timeline == null)
                throw new ArgumentNullException(nameof(timeline));
            if (TimelineUtility.FrameRate <= 0)
                throw new InvalidOperationException("Timeline preview requires a positive frame rate.");

            float targetTime = Mathf.Clamp(time, 0f, timeline.Duration);
            float previousTime = 0f;
            Vector3 worldDisplacement = Vector3.zero;
            float yawDegrees = 0f;
            int completeFrames = Mathf.FloorToInt(targetTime * TimelineUtility.FrameRate + 0.00001f);
            for (int frame = 1; frame <= completeFrames; frame++)
            {
                float currentTime = frame / (float)TimelineUtility.FrameRate;
                EvaluateSegment(
                    timeline,
                    previousTime,
                    currentTime,
                    originRotation,
                    ref worldDisplacement,
                    ref yawDegrees);
                previousTime = currentTime;
            }

            if (targetTime > previousTime + 0.000001f)
            {
                EvaluateSegment(
                    timeline,
                    previousTime,
                    targetTime,
                    originRotation,
                    ref worldDisplacement,
                    ref yawDegrees);
            }

            return new TimelineMotionPreviewPose(worldDisplacement, yawDegrees);
        }

        void EvaluateSegment(
            TimelineData timeline,
            float previousTime,
            float currentTime,
            Quaternion originRotation,
            ref Vector3 worldDisplacement,
            ref float yawDegrees)
        {
            m_Contributions.Clear();
            for (int trackIndex = 0; trackIndex < timeline.Tracks.Count; trackIndex++)
            {
                if (timeline.Tracks[trackIndex] is not MotionCurveTrack track)
                    continue;
                track.Sample(
                    previousTime,
                    currentTime,
                    timeline.AuthoringId,
                    "Timeline Authoring Preview",
                    m_Contributions);
            }

            if (m_Contributions.Count == 0)
                return;
            if (m_Contributions.Count > 1)
            {
                throw new InvalidOperationException(
                    $"Timeline '{timeline.AuthoringId}' MotionCurve preview found {m_Contributions.Count} overlapping contributions between {previousTime:0.###}s and {currentTime:0.###}s. Cross-source Motion arbitration requires a formal Simulation Session and Live Debug.");
            }

            TimelineMotionCurveContribution contribution = m_Contributions[0];
            Vector3 displacement = contribution.Displacement * contribution.Weight;
            if (contribution.Space == TimelineMotionContributionSpace.Local)
            {
                Quaternion currentRotation =
                    originRotation * Quaternion.Euler(0f, yawDegrees, 0f);
                displacement = currentRotation * displacement;
            }

            float yawDelta = contribution.YawDegrees * contribution.Weight;
            if (!IsFinite(displacement) || !IsFinite(yawDelta))
            {
                throw new InvalidOperationException(
                    $"Timeline '{timeline.AuthoringId}' MotionCurve '{contribution.CurveId}' produced a non-finite preview pose.");
            }
            worldDisplacement += displacement;
            yawDegrees += yawDelta;
        }

        static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        }

        static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
