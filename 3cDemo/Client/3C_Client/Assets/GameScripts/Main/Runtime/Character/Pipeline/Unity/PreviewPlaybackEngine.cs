using System;
using System.Collections.Generic;
using Animancer;
using BTSMTL.Timeline;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.Lifecycle;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline
{
    internal sealed class PreviewPlaybackEngine : IDisposable
    {
        readonly CharacterSimulationProgram m_Program;
        readonly CharacterPresentationProjection m_Projection;
        readonly CharacterAnimationPlaybackRuntime m_Playback;
        readonly ActorId m_PreviewActorId;
        readonly OperationHandle m_TimelineOperation;
        readonly HashSet<string> m_SelectedLayers = new HashSet<string>(StringComparer.Ordinal);
        readonly Dictionary<string, ActivePreviewProducer> m_Active =
            new Dictionary<string, ActivePreviewProducer>(StringComparer.Ordinal);
        readonly Dictionary<string, ActivePreviewProducer> m_NextActive =
            new Dictionary<string, ActivePreviewProducer>(StringComparer.Ordinal);
        readonly Dictionary<string, AnimationPlaybackId> m_TargetPlaybacks =
            new Dictionary<string, AnimationPlaybackId>(StringComparer.Ordinal);
        CharacterPresentationProducerEntry m_ComparisonTarget;
        CharacterPresentationProducerEntry m_ComparisonSource;
        bool m_ComparisonSourceSeeded;
        ulong m_EventSequence;

        public PreviewPlaybackEngine(
            CharacterPipelineDefinition definition,
            CharacterSimulationProgram program,
            CharacterPresentationProjection projection,
            AnimancerComponent animancer,
            TimelineData timeline,
            Guid previewSessionId)
        {
            if (!definition.AnimationPresentationProfile)
                throw new InvalidOperationException("Timeline preview requires the Definition's Animation Presentation Profile.");
            if (program == null || projection == null)
                throw new InvalidOperationException("Timeline preview requires compiled Program and Presentation Projection data.");
            if (timeline == null || previewSessionId == Guid.Empty)
                throw new ArgumentException("Timeline preview identity is incomplete.");
            m_Program = program;
            m_Projection = projection;
            m_Projection.RequireProgram(m_Program);
            m_PreviewActorId = new ActorId($"TimelinePreview/{previewSessionId:N}");
            m_TimelineOperation = CharacterPipelinePreviewProgram.FindTimelineOperation(
                m_Program,
                timeline.AuthoringId);
            m_Playback = new CharacterAnimationPlaybackRuntime(
                m_Program,
                m_Projection,
                animancer,
                false,
                AnimationTransitionEvaluationMode.Immediate);
        }

        public IReadOnlyList<AnimationPlaybackLifecycleSnapshot> Snapshots => m_Playback.Snapshots;

        public void ConfigureMarkerSyncSource(
            string targetTimelineAuthoringId,
            string targetTrackAuthoringId,
            string sourceTimelineAuthoringId,
            string sourceTrackAuthoringId)
        {
            m_ComparisonTarget = null;
            m_ComparisonSource = null;
            m_ComparisonSourceSeeded = false;
            if (string.IsNullOrEmpty(targetTimelineAuthoringId) || string.IsNullOrEmpty(targetTrackAuthoringId) ||
                string.IsNullOrEmpty(sourceTimelineAuthoringId) || string.IsNullOrEmpty(sourceTrackAuthoringId))
                return;
            string targetIdentity = $"producer:{targetTimelineAuthoringId}:{targetTrackAuthoringId}";
            string sourceIdentity = $"producer:{sourceTimelineAuthoringId}:{sourceTrackAuthoringId}";
            if (!m_Projection.TryGetProducer(targetIdentity, out CharacterPresentationProducerEntry target) ||
                !m_Projection.TryGetProducer(sourceIdentity, out CharacterPresentationProducerEntry source) ||
                target.Kind != CharacterPresentationProducerKind.Animation ||
                source.Kind != CharacterPresentationProducerKind.Animation ||
                target.Animation?.MarkerSync == null || source.Animation?.MarkerSync == null ||
                !target.Animation.MarkerSync.IsMarkerGroup || !source.Animation.MarkerSync.IsMarkerGroup ||
                !string.Equals(target.LayerId, source.LayerId, StringComparison.Ordinal) ||
                !string.Equals(
                    target.Animation.MarkerSync.CanonicalGroupId,
                    source.Animation.MarkerSync.CanonicalGroupId,
                    StringComparison.Ordinal))
                throw new InvalidOperationException(
                    $"Marker Sync preview source '{sourceIdentity}' is not compatible with target '{targetIdentity}'.");
            m_ComparisonTarget = target;
            m_ComparisonSource = source;
        }

        public void Evaluate(PreviewSession session)
        {
            if (session == null || !session.HasEvaluation)
                throw new ArgumentException("Timeline preview session has no evaluation.", nameof(session));

            m_SelectedLayers.Clear();
            m_NextActive.Clear();
            m_TargetPlaybacks.Clear();
            var tick = new SimulationTick(session.EvaluationTick);
            var activation = new ActivationId(
                m_TimelineOperation,
                session.Generation,
                $"timeline-preview:{session.Timeline.AuthoringId}");
            PrepareComparisonSource(session, tick, activation);
            for (int trackIndex = 0; trackIndex < session.Timeline.Tracks.Count; trackIndex++)
            {
                if (session.Timeline.Tracks[trackIndex] is not AnimationTrack track)
                    continue;

                string producerIdentity = $"producer:{session.Timeline.AuthoringId}:{track.AuthoringId}";
                if (!m_Projection.TryGetProducer(producerIdentity, out CharacterPresentationProducerEntry producer) ||
                    producer.Kind != CharacterPresentationProducerKind.Animation ||
                    producer.Animation == null)
                    throw new InvalidOperationException(
                        $"Timeline preview producer '{producerIdentity}' has no compiled animation projection.");
                if (!m_SelectedLayers.Add(producer.LayerId))
                    throw new InvalidOperationException(
                        $"Timeline preview contains multiple selected producers for layer '{producer.LayerId}'.");

                var playbackId = new AnimationPlaybackId(producer.ProducerId, session.Generation);
                m_TargetPlaybacks[track.AuthoringId] = playbackId;
                AnimationProducerSample probe =
                    producer.Animation.Sample(producer, playbackId, session.CurrentTime, 0);
                if (!probe.IsValid)
                    throw new InvalidOperationException(
                        $"Timeline preview producer '{producer.ProducerId}' produced an invalid sample.");
                if (!probe.HasOutput)
                    continue;
                var active = new ActivePreviewProducer(producer, session.Generation);
                m_NextActive.Add(producer.LayerId, active);
                m_Playback.Publish(
                    CreateCommand(
                        PresentationCommandKind.SelectProducer,
                        producer,
                        session.Generation,
                        tick,
                        activation,
                        session.CurrentTime,
                        "preview.animation.select"),
                    producer);
                m_Playback.Publish(
                    CreateCommand(
                        PresentationCommandKind.SampleProducer,
                        producer,
                        session.Generation,
                        tick,
                        activation,
                        session.CurrentTime,
                        "preview.animation.sample"),
                    producer);
            }

            foreach (KeyValuePair<string, ActivePreviewProducer> item in m_Active)
            {
                if (m_NextActive.ContainsKey(item.Key))
                    continue;
                ActivePreviewProducer active = item.Value;
                m_Playback.Publish(
                    CreateCommand(
                        PresentationCommandKind.ReleaseProducer,
                        active.Producer,
                        active.Generation,
                        tick,
                        new ActivationId(
                            m_TimelineOperation,
                            active.Generation,
                            $"timeline-preview:{session.Timeline.AuthoringId}"),
                        session.CurrentTime,
                        "preview.animation.release"),
                    active.Producer);
            }

            m_Active.Clear();
            foreach (KeyValuePair<string, ActivePreviewProducer> item in m_NextActive)
                m_Active.Add(item.Key, item.Value);
            m_Playback.Present(
                session.EvaluationTick,
                1f,
                session.PresentationDeltaSeconds,
                null);
        }

        public void RetireAndReset(ulong evaluationTick)
        {
            ulong tickValue = Math.Max(1UL, evaluationTick);
            var tick = new SimulationTick(tickValue);
            foreach (ActivePreviewProducer active in m_Active.Values)
            {
                var activation = new ActivationId(
                    m_TimelineOperation,
                    active.Generation,
                    "timeline-preview:retire");
                m_Playback.Publish(
                    CreateCommand(
                        PresentationCommandKind.ReleaseProducer,
                        active.Producer,
                        active.Generation,
                        tick,
                        activation,
                        0f,
                        "preview.animation.seek-retire"),
                    active.Producer);
            }
            if (m_Active.Count > 0)
                m_Playback.Present(tickValue, 1f, 0f, null);
            m_Playback.Reset();
            m_Active.Clear();
            m_NextActive.Clear();
            m_SelectedLayers.Clear();
            m_TargetPlaybacks.Clear();
            m_ComparisonSourceSeeded = false;
        }

        public bool TryGetMarkerSyncPreviewState(
            string targetTrackAuthoringId,
            out TimelineAnimationMarkerSyncPreviewState state)
        {
            if (!m_TargetPlaybacks.TryGetValue(targetTrackAuthoringId ?? string.Empty, out AnimationPlaybackId playbackId))
            {
                state = default;
                return false;
            }
            IReadOnlyList<AnimationMarkerSyncPlaybackSnapshot> playbacks = m_Playback.MarkerSyncPlaybackSnapshots;
            for (int i = 0; i < playbacks.Count; i++)
            {
                AnimationMarkerSyncPlaybackSnapshot playback = playbacks[i];
                if (!playback.PlaybackId.Equals(playbackId))
                    continue;
                AnimationMarkerSyncRelationSnapshot relation = default;
                bool hasRelation = false;
                IReadOnlyList<AnimationMarkerSyncRelationSnapshot> relations = m_Playback.MarkerSyncSnapshots;
                for (int relationIndex = 0; relationIndex < relations.Count; relationIndex++)
                {
                    if (!relations[relationIndex].Target.Equals(playbackId))
                        continue;
                    relation = relations[relationIndex];
                    hasRelation = true;
                    break;
                }
                state = new TimelineAnimationMarkerSyncPreviewState(
                    targetTrackAuthoringId,
                    hasRelation ? relation.Source.ToString() : string.Empty,
                    playbackId.ProducerId.ToString(),
                    playback.LayerId,
                    playback.SyncGroupId,
                    playback.PreviousMarkerId,
                    playback.NextMarkerId,
                    playback.Fraction,
                    playback.RawTime,
                    playback.EffectiveTime,
                    playback.EffectiveCycle,
                    hasRelation ? relation.TargetOccurrenceIndex : -1,
                    hasRelation ? relation.RelationDepth : 0,
                    hasRelation ? relation.TargetLifecyclePhase.ToString() : ResolveLifecyclePhase(playbackId),
                    hasRelation ? relation.Reason.ToString() : "NoCurrentSource");
                return true;
            }
            state = default;
            return false;
        }

        public void Dispose()
        {
            m_Playback.Dispose();
        }

        void PrepareComparisonSource(
            PreviewSession session,
            SimulationTick tick,
            ActivationId activation)
        {
            if (m_ComparisonSource == null || m_ComparisonTarget == null)
                return;
            var playbackId = new AnimationPlaybackId(m_ComparisonSource.ProducerId, session.Generation);
            bool visible = m_ComparisonSourceSeeded && IsVisible(playbackId);
            if (m_ComparisonSourceSeeded && !visible)
                return;
            float sampleTime = NormalizeComparisonTime(
                m_ComparisonSource.Animation.MarkerSync,
                session.CurrentTime);
            if (!m_ComparisonSourceSeeded)
            {
                m_Playback.Publish(
                    CreateCommand(
                        PresentationCommandKind.SelectProducer,
                        m_ComparisonSource,
                        session.Generation,
                        tick,
                        activation,
                        sampleTime,
                        "preview.animation.marker-source-select"),
                    m_ComparisonSource);
            }
            m_Playback.Publish(
                CreateCommand(
                    PresentationCommandKind.SampleProducer,
                    m_ComparisonSource,
                    session.Generation,
                    tick,
                    activation,
                    sampleTime,
                    "preview.animation.marker-source-sample"),
                m_ComparisonSource);
            if (!m_ComparisonSourceSeeded)
            {
                m_Playback.Present(session.EvaluationTick, 1f, 0f, null);
                m_ComparisonSourceSeeded = true;
            }
        }

        bool IsVisible(AnimationPlaybackId playbackId)
        {
            IReadOnlyList<AnimationPlaybackLifecycleSnapshot> snapshots = m_Playback.Snapshots;
            for (int i = 0; i < snapshots.Count; i++)
            {
                if (snapshots[i].PlaybackId.Equals(playbackId) &&
                    snapshots[i].Phase != AnimationPlaybackLifecyclePhase.Retired)
                    return true;
            }
            return false;
        }

        string ResolveLifecyclePhase(AnimationPlaybackId playbackId)
        {
            IReadOnlyList<AnimationPlaybackLifecycleSnapshot> snapshots = m_Playback.Snapshots;
            for (int i = 0; i < snapshots.Count; i++)
            {
                if (snapshots[i].PlaybackId.Equals(playbackId))
                    return snapshots[i].Phase.ToString();
            }
            return AnimationPlaybackLifecyclePhase.Retired.ToString();
        }

        static float NormalizeComparisonTime(AnimationMarkerSyncBinding binding, float time)
        {
            float duration = Math.Max(0.0001f, binding.DurationSeconds);
            if (binding.SequenceTopology == AnimationMarkerSequenceTopology.Cyclic)
            {
                float value = time % duration;
                return value < 0f ? value + duration : value;
            }
            return Math.Max(0f, Math.Min(duration, time));
        }

        PresentationCommand CreateCommand(
            PresentationCommandKind kind,
            CharacterPresentationProducerEntry producer,
            ulong generation,
            SimulationTick tick,
            ActivationId activation,
            float sampleTime,
            string channel)
        {
            m_EventSequence++;
            if (m_EventSequence == 0)
                throw new OverflowException("Timeline preview Event sequence overflowed.");
            EventId eventId = EventId.Create(
                m_Program.ProgramHash,
                m_PreviewActorId,
                activation,
                tick,
                m_EventSequence,
                channel);
            var header = new SimulationEventHeader(
                m_Program.Manifest.NumericProfile,
                eventId,
                m_PreviewActorId,
                tick,
                activation,
                m_EventSequence,
                channel);
            return new PresentationCommand(
                header,
                kind,
                producer.ProgramProducerIdentity,
                Float32Scalar.FromSingle(Math.Max(0f, sampleTime)),
                Float32Scalar.One,
                generation,
                0);
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
        }
    }
}
