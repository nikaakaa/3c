using System;
using System.Collections.Generic;
using Animancer;
using BTSMTL.Timeline;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.Lifecycle;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline
{
    internal sealed class CharacterPipelinePreviewController : IDisposable
    {
        readonly CharacterPipelineHost m_Host;
        readonly CharacterPipelineDefinition m_Definition;
        readonly AnimancerComponent m_Animancer;
        readonly CharacterSimulationProgram m_Program;
        readonly CharacterPresentationProjection m_Projection;
        readonly TimelineMotionAuthoringPreviewEvaluator m_MotionPreview =
            new TimelineMotionAuthoringPreviewEvaluator();
        PreviewSession m_Session;
        Guid m_SessionId;
        ulong m_Generation;
        bool m_OwnsGraphClock;
        bool m_HasOriginalVisualPose;
        Vector3 m_OriginalVisualPosition;
        Quaternion m_OriginalVisualRotation;
        string m_TargetTimelineId = string.Empty;
        string m_TargetTrackId = string.Empty;
        string m_SourceTimelineId = string.Empty;
        string m_SourceTrackId = string.Empty;

        public CharacterPipelinePreviewController(CharacterPipelineHost host)
        {
            m_Host = host ? host : throw new ArgumentNullException(nameof(host));
            m_Definition = host.Definition ? host.Definition : throw new InvalidOperationException("Timeline preview requires a Character Pipeline Definition.");
            m_Animancer = host.Animancer ? host.Animancer : throw new InvalidOperationException("Timeline preview requires Animancer.");
            if (!m_Definition.SimulationProgram || !m_Definition.PresentationProjection)
                throw new InvalidOperationException("Timeline preview requires compiled Program and Presentation Projection assets.");
            m_Program = m_Definition.SimulationProgram.Load();
            m_Projection = m_Definition.PresentationProjection.Load(m_Program);
            m_Projection.RequireProgram(m_Program);
        }

        public IReadOnlyList<AnimationPlaybackLifecycleSnapshot> AnimationSnapshots =>
            m_Session != null
                ? m_Session.Engine.Snapshots
                : Array.Empty<AnimationPlaybackLifecycleSnapshot>();

        public bool Matches(CharacterPipelineDefinition definition, AnimancerComponent animancer)
        {
            return m_Definition == definition && m_Animancer == animancer;
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

            bool created = m_Session == null;
            if (created)
            {
                CaptureVisualPose();
                m_Session = new PreviewSession(
                    NextGeneration(),
                    new PreviewPlaybackEngine(m_Definition, m_Program, m_Projection, m_Animancer, timeline, sessionId));
                m_SessionId = sessionId;
                AcquireGraphClock();
                m_Session.Engine.ConfigureMarkerSyncSource(
                    m_TargetTimelineId,
                    m_TargetTrackId,
                    m_SourceTimelineId,
                    m_SourceTrackId);
            }

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

        public void CollectMarkerSyncSources(
            TimelineData timeline,
            string targetTrackAuthoringId,
            List<TimelineAnimationMarkerSyncPreviewCandidate> destination)
        {
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));
            destination.Clear();
            if (timeline == null || string.IsNullOrEmpty(targetTrackAuthoringId))
                return;
            string targetIdentity = $"producer:{timeline.AuthoringId}:{targetTrackAuthoringId}";
            if (!m_Projection.TryGetProducer(targetIdentity, out CharacterPresentationProducerEntry target) ||
                target.Kind != CharacterPresentationProducerKind.Animation ||
                target.Animation?.MarkerSync == null ||
                !target.Animation.MarkerSync.IsMarkerGroup)
                return;

            IReadOnlyList<CharacterPresentationProducerEntry> producers = m_Projection.AnimationProducers;
            for (int i = 0; i < producers.Count; i++)
            {
                CharacterPresentationProducerEntry source = producers[i];
                if (source.ProducerId.Equals(target.ProducerId) || source.Animation?.MarkerSync == null ||
                    !source.Animation.MarkerSync.IsMarkerGroup ||
                    !string.Equals(source.LayerId, target.LayerId, StringComparison.Ordinal) ||
                    !string.Equals(
                        source.Animation.MarkerSync.CanonicalGroupId,
                        target.Animation.MarkerSync.CanonicalGroupId,
                        StringComparison.Ordinal))
                    continue;
                destination.Add(new TimelineAnimationMarkerSyncPreviewCandidate(
                    source.ProducerId.TimelineAuthoringId,
                    source.ProducerId.TrackAuthoringId,
                    string.IsNullOrEmpty(source.SourceDisplayPath)
                        ? source.Animation.TrackName
                        : source.SourceDisplayPath,
                    source.LayerId,
                    source.Animation.MarkerSync.CanonicalGroupId));
            }
            destination.Sort((left, right) =>
            {
                int display = string.CompareOrdinal(left.DisplayName, right.DisplayName);
                if (display != 0)
                    return display;
                int timelineId = string.CompareOrdinal(left.SourceTimelineAuthoringId, right.SourceTimelineAuthoringId);
                return timelineId != 0
                    ? timelineId
                    : string.CompareOrdinal(left.SourceTrackAuthoringId, right.SourceTrackAuthoringId);
            });
        }

        public void ConfigureMarkerSyncSource(
            Guid sessionId,
            string targetTimelineAuthoringId,
            string targetTrackAuthoringId,
            string sourceTimelineAuthoringId,
            string sourceTrackAuthoringId)
        {
            if (m_Session != null && m_SessionId != sessionId)
                throw new InvalidOperationException(
                    $"Timeline preview target '{m_Host.name}' is already owned by session '{m_SessionId}'.");
            m_TargetTimelineId = targetTimelineAuthoringId ?? string.Empty;
            m_TargetTrackId = targetTrackAuthoringId ?? string.Empty;
            m_SourceTimelineId = sourceTimelineAuthoringId ?? string.Empty;
            m_SourceTrackId = sourceTrackAuthoringId ?? string.Empty;
            if (m_Session == null)
                return;
            m_Session.Engine.RetireAndReset(Math.Max(1UL, m_Session.EvaluationTick));
            m_Session.Generation = NextGeneration();
            m_Session.Engine.ConfigureMarkerSyncSource(
                m_TargetTimelineId,
                m_TargetTrackId,
                m_SourceTimelineId,
                m_SourceTrackId);
        }

        public bool TryGetMarkerSyncPreviewState(
            Guid sessionId,
            string targetTrackAuthoringId,
            out TimelineAnimationMarkerSyncPreviewState state)
        {
            if (m_Session != null && m_SessionId == sessionId)
                return m_Session.Engine.TryGetMarkerSyncPreviewState(targetTrackAuthoringId, out state);
            state = default;
            return false;
        }

        public void Clear(Guid sessionId)
        {
            if (sessionId == Guid.Empty || m_Session == null || m_SessionId != sessionId)
                return;
            ClearSession();
        }

        public void Dispose()
        {
            ClearSession();
        }

        void ClearSession()
        {
            m_Session?.Dispose();
            m_Session = null;
            m_SessionId = Guid.Empty;
            RestoreVisualPose();
            m_HasOriginalVisualPose = false;
            ReleaseGraphClock();
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
