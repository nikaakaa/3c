using System;
using System.Collections.Generic;
using Animancer;
using BTSMTL.Diagnostics;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Pipeline.Animation.Lifecycle;
using ThirdPersonCharacter.Pipeline.Presentation.Animancer;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    public enum AnimationTransitionEvaluationMode
    {
        Timed = 0,
        Immediate = 1
    }

    public sealed class CharacterAnimationPlaybackRuntime : IDisposable
    {
        readonly AnimancerPlaybackAdapter m_Adapter;
        readonly AnimationPlaybackLifecycle m_Lifecycle;
        readonly AnimationMarkerSyncRuntime m_MarkerSync = new AnimationMarkerSyncRuntime();
        readonly CharacterAnimationTracePublisher m_TracePublisher = new CharacterAnimationTracePublisher();
        readonly CharacterAnimationPlaybackCommandQueue m_Commands = new CharacterAnimationPlaybackCommandQueue();
        readonly List<AnimationPlaybackCommand> m_CommandBuffer = new List<AnimationPlaybackCommand>();
        readonly List<AnimationPlaybackLifecycleSnapshot> m_Snapshots = new List<AnimationPlaybackLifecycleSnapshot>();
        readonly List<AnimationMarkerSyncRelationSnapshot> m_MarkerSyncSnapshots = new List<AnimationMarkerSyncRelationSnapshot>();
        readonly List<AnimationMarkerSyncPlaybackSnapshot> m_MarkerSyncPlaybackSnapshots = new List<AnimationMarkerSyncPlaybackSnapshot>();
        readonly HashSet<AnimationPlaybackId> m_DemandedPlaybacks = new HashSet<AnimationPlaybackId>();
        readonly List<AnimationPlaybackId> m_RetiredPlaybacks = new List<AnimationPlaybackId>();
        readonly Dictionary<string, AnimationSelectionState> m_Selections =
            new Dictionary<string, AnimationSelectionState>(StringComparer.Ordinal);
        readonly Dictionary<AnimationPlaybackId, AnimationSamplingState> m_Sampling =
            new Dictionary<AnimationPlaybackId, AnimationSamplingState>();
        readonly Dictionary<AnimationPlaybackId, AnimationMarkerSyncRawSample> m_RawSamples =
            new Dictionary<AnimationPlaybackId, AnimationMarkerSyncRawSample>();
        readonly Dictionary<AnimationPlaybackId, AnimationMarkerSyncEffectiveSample> m_EffectiveSamples =
            new Dictionary<AnimationPlaybackId, AnimationMarkerSyncEffectiveSample>();
        readonly List<AnimationPlaybackId> m_RemoveSampling = new List<AnimationPlaybackId>();
        readonly List<AnimationTerminalState> m_Terminals = new List<AnimationTerminalState>();
        readonly List<string> m_RequiredLayers = new List<string>();

        ulong m_SelectionSequence;
        bool m_Disposed;

        public CharacterAnimationPlaybackRuntime(
            CharacterSimulationProgram program,
            CharacterPresentationProjection projection,
            AnimancerComponent animancer,
            bool ownsGraphClock,
            AnimationTransitionEvaluationMode transitionEvaluationMode)
            : this(RequireExactProgram(program, projection), projection, animancer, ownsGraphClock, transitionEvaluationMode)
        {
        }

        public CharacterAnimationPlaybackRuntime(
            CharacterPresentationProgramIdentity program,
            CharacterPresentationProjection projection,
            AnimancerComponent animancer,
            bool ownsGraphClock,
            AnimationTransitionEvaluationMode transitionEvaluationMode)
        {
            if (program == null)
                throw new ArgumentNullException(nameof(program));
            if (projection == null)
                throw new ArgumentNullException(nameof(projection));
            projection.RequireSemanticProgram(program);
            var errors = new List<string>();
            CharacterAnimationPresentationBindingIndex bindings =
                CharacterAnimationPresentationBindingIndex.Build(projection, program, errors);
            if (!bindings.IsValid)
                throw new InvalidOperationException(string.Join("\n", errors));
            foreach (KeyValuePair<string, ResolvedAnimationLayer> pair in bindings.Layers)
            {
                if (pair.Value.OutputPolicy == AnimationLayerOutputPolicy.RequireOutput)
                    m_RequiredLayers.Add(pair.Key);
            }
            m_RequiredLayers.Sort(StringComparer.Ordinal);
            m_Adapter = new AnimancerPlaybackAdapter(
                animancer,
                bindings,
                ownsGraphClock,
                transitionEvaluationMode);
            m_Lifecycle = new AnimationPlaybackLifecycle(bindings, m_Adapter);
        }

        static CharacterPresentationProgramIdentity RequireExactProgram(
            CharacterSimulationProgram program,
            CharacterPresentationProjection projection)
        {
            if (program == null)
                throw new ArgumentNullException(nameof(program));
            if (projection == null)
                throw new ArgumentNullException(nameof(projection));
            projection.RequireProgram(program);
            return CharacterPresentationProgramIdentity.From(program);
        }

        public IReadOnlyList<AnimationPlaybackId> RetiredPlaybacks => m_RetiredPlaybacks;
        public IReadOnlyList<AnimationPlaybackLifecycleSnapshot> Snapshots => m_Snapshots;
        public IReadOnlyList<AnimationMarkerSyncRelationSnapshot> MarkerSyncSnapshots => m_MarkerSyncSnapshots;
        public IReadOnlyList<AnimationMarkerSyncPlaybackSnapshot> MarkerSyncPlaybackSnapshots => m_MarkerSyncPlaybackSnapshots;

        internal void CollectPoseContributions(
            string layerId,
            List<AnimationPoseContribution> destination)
        {
            RequireAlive();
            if (string.IsNullOrEmpty(layerId))
                throw new ArgumentException("Pose source layer identity is required.", nameof(layerId));
            m_Adapter.CollectPoseContributions(layerId, destination);
        }
        public bool HasRequiredOutput
        {
            get
            {
                RequireAlive();
                for (int i = 0; i < m_RequiredLayers.Count; i++)
                {
                    if (!m_Selections.TryGetValue(m_RequiredLayers[i], out AnimationSelectionState selection) ||
                        !m_Sampling.ContainsKey(selection.PlaybackId))
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        public void Publish(PresentationCommand command, CharacterPresentationProducerEntry producer) =>
            Publish(CharacterPresentationCommand.FromFloat32(command), producer);

        public void Publish(CharacterPresentationCommand command, CharacterPresentationProducerEntry producer)
        {
            RequireAlive();
            RequireAnimationProducer(producer, command.Kind);
            switch (command.Kind)
            {
                case CharacterPresentationCommandKind.SelectProducer:
                    PublishSelection(command, producer);
                    break;
                case CharacterPresentationCommandKind.SampleProducer:
                    PublishSample(command, producer);
                    break;
                case CharacterPresentationCommandKind.CompleteProducer:
                case CharacterPresentationCommandKind.ReleaseProducer:
                    PublishTerminal(command, producer);
                    break;
                default:
                    throw new ArgumentException($"Presentation command '{command.Kind}' is not an animation playback command.", nameof(command));
            }
        }

        public void Retire(PresentationCommand command, CharacterPresentationProducerEntry producer) =>
            Retire(CharacterPresentationCommand.FromFloat32(command), producer);

        public void Retire(CharacterPresentationCommand command, CharacterPresentationProducerEntry producer)
        {
            RequireAlive();
            RequireAnimationProducer(producer, command.Kind);
            var playbackId = new AnimationPlaybackId(producer.ProducerId, command.ProducerGeneration);
            switch (command.Kind)
            {
                case CharacterPresentationCommandKind.SelectProducer:
                    if (m_Selections.TryGetValue(producer.LayerId, out AnimationSelectionState selection) &&
                        selection.EventId.Equals(command.Header.EventId))
                    {
                        m_Selections.Remove(producer.LayerId);
                    }
                    break;
                case CharacterPresentationCommandKind.SampleProducer:
                    if (m_Sampling.TryGetValue(playbackId, out AnimationSamplingState sampling) &&
                        sampling.EventId.Equals(command.Header.EventId) &&
                        !IsSamplingRetained(playbackId))
                    {
                        m_Sampling.Remove(playbackId);
                    }
                    break;
                case CharacterPresentationCommandKind.CompleteProducer:
                case CharacterPresentationCommandKind.ReleaseProducer:
                    for (int i = m_Terminals.Count - 1; i >= 0; i--)
                    {
                        if (m_Terminals[i].EventId.Equals(command.Header.EventId))
                            m_Terminals.RemoveAt(i);
                    }
                    break;
                default:
                    throw new ArgumentException($"Presentation command '{command.Kind}' is not an animation playback command.", nameof(command));
            }
        }

        public void Replace(
            CharacterPresentationCommand current,
            CharacterPresentationCommand replacement,
            CharacterPresentationProducerEntry currentProducer,
            CharacterPresentationProducerEntry replacementProducer)
        {
            RequireAlive();
            RequireAnimationProducer(currentProducer, current.Kind);
            RequireAnimationProducer(replacementProducer, replacement.Kind);
            switch (replacement.Kind)
            {
                case CharacterPresentationCommandKind.SelectProducer:
                {
                    if (current.Kind != CharacterPresentationCommandKind.SelectProducer ||
                        !string.Equals(currentProducer.LayerId, replacementProducer.LayerId, StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException("Animation selection replacement changed its layer or command kind.");
                    }
                    var playbackId = new AnimationPlaybackId(
                        replacementProducer.ProducerId,
                        replacement.ProducerGeneration);
                    m_Selections[replacementProducer.LayerId] = new AnimationSelectionState(
                        replacementProducer.LayerId,
                        playbackId,
                        replacement.Header.EventId,
                        replacement.Header.Tick.Value,
                        replacement.Header.Sequence);
                    break;
                }
                case CharacterPresentationCommandKind.SampleProducer:
                {
                    if (current.Kind != CharacterPresentationCommandKind.SampleProducer)
                        throw new InvalidOperationException("Animation sample replacement changed its command kind.");
                    var currentPlayback = new AnimationPlaybackId(
                        currentProducer.ProducerId,
                        current.ProducerGeneration);
                    var replacementPlayback = new AnimationPlaybackId(
                        replacementProducer.ProducerId,
                        replacement.ProducerGeneration);
                    if (!currentPlayback.Equals(replacementPlayback))
                        throw new InvalidOperationException("Animation sample replacement changed its playback identity.");
                    if (m_Sampling.TryGetValue(currentPlayback, out AnimationSamplingState sampling))
                        sampling.Replace(replacement);
                    else
                        m_Sampling.Add(replacementPlayback, new AnimationSamplingState(replacementProducer, replacement));
                    break;
                }
                case CharacterPresentationCommandKind.CompleteProducer:
                case CharacterPresentationCommandKind.ReleaseProducer:
                    if (current.Kind != CharacterPresentationCommandKind.CompleteProducer &&
                        current.Kind != CharacterPresentationCommandKind.ReleaseProducer)
                    {
                        throw new InvalidOperationException("Animation terminal replacement changed its command family.");
                    }
                    Retire(current, currentProducer);
                    PublishTerminal(replacement, replacementProducer);
                    break;
                default:
                    throw new ArgumentException(
                        $"Presentation command '{replacement.Kind}' is not an animation playback command.",
                        nameof(replacement));
            }
        }

        public void Present(
            ulong latestSimulationTick,
            float interpolationAlpha,
            float presentationDeltaSeconds,
            RuntimeDiagnosticsContext diagnostics = null)
        {
            RequireAlive();
            foreach (AnimationSelectionState selection in m_Selections.Values)
            {
                m_Commands.EnqueueSelection(AnimationLayerSelection.Select(
                    selection.LayerId,
                    selection.PlaybackId,
                    latestSimulationTick,
                    NextSelectionSequence()));
            }
            for (int i = 0; i < m_Terminals.Count; i++)
            {
                AnimationTerminalState terminal = m_Terminals[i];
                if (terminal.Kind == CharacterPresentationCommandKind.CompleteProducer)
                    m_Commands.EnqueuePlaybackComplete(latestSimulationTick, terminal.PlaybackId);
                else
                    m_Commands.EnqueuePlaybackRelease(latestSimulationTick, terminal.PlaybackId);
            }
            m_Terminals.Clear();

            m_Commands.CopyPendingTo(m_CommandBuffer);
            m_Lifecycle.CollectSampleDemand(m_CommandBuffer, m_DemandedPlaybacks);
            m_RawSamples.Clear();
            foreach (AnimationPlaybackId playbackId in m_DemandedPlaybacks)
            {
                if (!m_Sampling.TryGetValue(playbackId, out AnimationSamplingState sampling))
                    continue;
                m_RawSamples.Add(playbackId, sampling.ResolveRawSample(
                    playbackId,
                    latestSimulationTick,
                    interpolationAlpha,
                    presentationDeltaSeconds));
            }
            try
            {
                m_MarkerSync.BeginFrame();
                foreach (AnimationSelectionState selection in m_Selections.Values)
                {
                    if (!m_RawSamples.TryGetValue(selection.PlaybackId, out AnimationMarkerSyncRawSample target))
                        continue;
                    if (!m_Lifecycle.TryGetCurrentPlayback(selection.LayerId, out AnimationPlaybackId sourcePlayback) ||
                        sourcePlayback.Equals(selection.PlaybackId))
                    {
                        m_MarkerSync.RecordNoCurrentSource(target);
                        continue;
                    }
                    if (!m_RawSamples.TryGetValue(sourcePlayback, out AnimationMarkerSyncRawSample source))
                        throw new AnimationMarkerSyncException(
                            AnimationMarkerSyncInvalidReason.SourceSampleMissing,
                            sourcePlayback);
                    m_MarkerSync.EnsureHandoff(source, target);
                }
                m_MarkerSync.Evaluate(m_RawSamples, m_EffectiveSamples);
            }
            catch (AnimationMarkerSyncException failure)
            {
                m_RawSamples.TryGetValue(failure.PlaybackId, out AnimationMarkerSyncRawSample failedSample);
                m_TracePublisher.PublishMarkerSyncFailure(diagnostics, failure, failedSample);
                throw;
            }
            foreach (AnimationPlaybackId playbackId in m_DemandedPlaybacks)
            {
                if (!m_Sampling.TryGetValue(playbackId, out AnimationSamplingState sampling) ||
                    !m_EffectiveSamples.TryGetValue(playbackId, out AnimationMarkerSyncEffectiveSample effective))
                    continue;
                AnimationProducerSample sample = sampling.Producer.Animation.Sample(
                    sampling.Producer,
                    playbackId,
                    effective.LocalTime,
                    effective.Cycle);
                if (sample.HasOutput)
                    m_Commands.EnqueueSample(latestSimulationTick, sample);
            }

            m_Commands.CopyPendingTo(m_CommandBuffer);
            m_Lifecycle.Apply(m_CommandBuffer, presentationDeltaSeconds, m_RetiredPlaybacks);
            m_Lifecycle.BuildSnapshot(m_Snapshots);
            m_MarkerSync.BuildPlaybackSnapshot(m_MarkerSyncPlaybackSnapshots);
            m_MarkerSync.BuildRelationSnapshot(m_MarkerSyncSnapshots);
            AttachMarkerLifecyclePhases();
            if (diagnostics != null)
            {
                m_TracePublisher.PublishPlaybackLifecycle(
                    diagnostics,
                    m_CommandBuffer,
                    m_Snapshots,
                    m_MarkerSyncSnapshots,
                    m_RetiredPlaybacks);
            }
            m_Commands.Acknowledge(m_CommandBuffer);
            m_CommandBuffer.Clear();
            m_MarkerSync.Retire(m_RetiredPlaybacks);
            for (int i = 0; i < m_RetiredPlaybacks.Count; i++)
                m_Sampling.Remove(m_RetiredPlaybacks[i]);
            PruneUnreferencedSampling();
        }

        public void Reset()
        {
            if (m_Disposed)
                return;
            ResetState();
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Adapter.Dispose();
            m_Disposed = true;
        }

        void PublishSelection(CharacterPresentationCommand command, CharacterPresentationProducerEntry producer)
        {
            var playbackId = new AnimationPlaybackId(producer.ProducerId, command.ProducerGeneration);
            if (!m_Selections.TryGetValue(producer.LayerId, out AnimationSelectionState current) ||
                IsNewer(command.Header, current.Tick, current.Sequence))
            {
                m_Selections[producer.LayerId] = new AnimationSelectionState(
                    producer.LayerId,
                    playbackId,
                    command.Header.EventId,
                    command.Header.Tick.Value,
                    command.Header.Sequence);
            }
        }

        void PublishSample(CharacterPresentationCommand command, CharacterPresentationProducerEntry producer)
        {
            var playbackId = new AnimationPlaybackId(producer.ProducerId, command.ProducerGeneration);
            if (!m_Sampling.TryGetValue(playbackId, out AnimationSamplingState sampling))
            {
                m_Sampling.Add(playbackId, new AnimationSamplingState(producer, command));
                return;
            }
            sampling.Capture(command);
        }

        void PublishTerminal(CharacterPresentationCommand command, CharacterPresentationProducerEntry producer)
        {
            var playbackId = new AnimationPlaybackId(producer.ProducerId, command.ProducerGeneration);
            m_Terminals.Add(new AnimationTerminalState(command.Kind, playbackId, command.Header.EventId));
        }

        void ResetState()
        {
            m_Commands.Clear();
            m_Lifecycle.Reset();
            m_CommandBuffer.Clear();
            m_Snapshots.Clear();
            m_MarkerSyncSnapshots.Clear();
            m_MarkerSyncPlaybackSnapshots.Clear();
            m_DemandedPlaybacks.Clear();
            m_RetiredPlaybacks.Clear();
            m_Selections.Clear();
            m_Sampling.Clear();
            m_RawSamples.Clear();
            m_EffectiveSamples.Clear();
            m_RemoveSampling.Clear();
            m_MarkerSync.Reset();
            m_Terminals.Clear();
        }

        void AttachMarkerLifecyclePhases()
        {
            for (int markerIndex = 0; markerIndex < m_MarkerSyncSnapshots.Count; markerIndex++)
            {
                AnimationMarkerSyncRelationSnapshot marker = m_MarkerSyncSnapshots[markerIndex];
                for (int lifecycleIndex = 0; lifecycleIndex < m_Snapshots.Count; lifecycleIndex++)
                {
                    AnimationPlaybackLifecycleSnapshot lifecycle = m_Snapshots[lifecycleIndex];
                    if (!lifecycle.PlaybackId.Equals(marker.Target))
                        continue;
                    m_MarkerSyncSnapshots[markerIndex] = marker.WithLifecyclePhase(lifecycle.Phase);
                    break;
                }
            }
        }

        static void RequireAnimationProducer(
            CharacterPresentationProducerEntry producer,
            CharacterPresentationCommandKind commandKind)
        {
            if (producer == null || producer.Kind != CharacterPresentationProducerKind.Animation || producer.Animation == null)
                throw new InvalidOperationException(
                    $"Presentation command '{commandKind}' targets a non-animation producer.");
        }

        bool IsSamplingRetained(AnimationPlaybackId playbackId)
        {
            foreach (AnimationSelectionState selection in m_Selections.Values)
            {
                if (selection.PlaybackId.Equals(playbackId))
                    return true;
            }
            return m_Lifecycle.Retains(playbackId);
        }

        void PruneUnreferencedSampling()
        {
            m_RemoveSampling.Clear();
            foreach (AnimationPlaybackId playbackId in m_Sampling.Keys)
            {
                if (!IsSamplingRetained(playbackId))
                    m_RemoveSampling.Add(playbackId);
            }
            for (int i = 0; i < m_RemoveSampling.Count; i++)
                m_Sampling.Remove(m_RemoveSampling[i]);
        }

        ulong NextSelectionSequence()
        {
            m_SelectionSequence++;
            if (m_SelectionSequence == 0)
                throw new OverflowException("Presentation selection sequence overflowed.");
            return m_SelectionSequence;
        }

        void RequireAlive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(CharacterAnimationPlaybackRuntime));
        }

        static bool IsNewer(CharacterPresentationEventHeader header, ulong tick, ulong sequence)
        {
            return header.Tick.Value > tick || header.Tick.Value == tick && header.Sequence > sequence;
        }

        readonly struct AnimationSelectionState
        {
            public AnimationSelectionState(
                string layerId,
                AnimationPlaybackId playbackId,
                EventId eventId,
                ulong tick,
                ulong sequence)
            {
                LayerId = layerId;
                PlaybackId = playbackId;
                EventId = eventId;
                Tick = tick;
                Sequence = sequence;
            }

            public string LayerId { get; }
            public AnimationPlaybackId PlaybackId { get; }
            public EventId EventId { get; }
            public ulong Tick { get; }
            public ulong Sequence { get; }
        }

        sealed class AnimationSamplingState
        {
            float m_PreviousTime;
            float m_CurrentTime;
            float m_VisualTime;
            double m_PreviousContinuousTime;
            double m_CurrentContinuousTime;
            double m_VisualContinuousTime;
            int m_PreviousCycle;
            ulong m_PreviousTick;
            ulong m_CurrentTick;
            ulong m_CurrentSequence;

            public AnimationSamplingState(CharacterPresentationProducerEntry producer, CharacterPresentationCommand command)
            {
                Producer = producer;
                m_CurrentTime = command.SampleTime;
                m_PreviousTime = m_CurrentTime;
                m_VisualTime = m_CurrentTime;
                Cycle = command.Cycle;
                m_PreviousCycle = Cycle;
                m_CurrentTick = command.Header.Tick.Value;
                m_PreviousTick = m_CurrentTick;
                m_CurrentSequence = command.Header.Sequence;
                EventId = command.Header.EventId;
                m_CurrentContinuousTime = ToContinuousTime(command.SampleTime, command.Cycle);
                m_PreviousContinuousTime = m_CurrentContinuousTime;
                m_VisualContinuousTime = m_CurrentContinuousTime;
            }

            public CharacterPresentationProducerEntry Producer { get; }
            public int Cycle { get; private set; }
            public EventId EventId { get; private set; }

            public void Capture(CharacterPresentationCommand command)
            {
                if (!IsNewer(command.Header, m_CurrentTick, m_CurrentSequence))
                    return;
                m_PreviousTime = m_CurrentTime;
                m_PreviousTick = m_CurrentTick;
                m_PreviousCycle = Cycle;
                m_PreviousContinuousTime = m_CurrentContinuousTime;
                m_CurrentTime = command.SampleTime;
                m_CurrentTick = command.Header.Tick.Value;
                m_CurrentSequence = command.Header.Sequence;
                EventId = command.Header.EventId;
                Cycle = command.Cycle;
                m_CurrentContinuousTime = ToContinuousTime(command.SampleTime, command.Cycle);
            }

            public void Replace(CharacterPresentationCommand command)
            {
                m_PreviousTime = m_VisualTime;
                m_PreviousTick = command.Header.Tick.Value > 1
                    ? command.Header.Tick.Value - 1
                    : command.Header.Tick.Value;
                m_PreviousCycle = command.Cycle;
                m_PreviousContinuousTime = m_VisualContinuousTime;
                m_CurrentTime = command.SampleTime;
                m_CurrentTick = command.Header.Tick.Value;
                m_CurrentSequence = command.Header.Sequence;
                EventId = command.Header.EventId;
                Cycle = command.Cycle;
                m_CurrentContinuousTime = ToContinuousTime(command.SampleTime, command.Cycle);
            }

            public AnimationMarkerSyncRawSample ResolveRawSample(
                AnimationPlaybackId playbackId,
                ulong latestTick,
                float alpha,
                float deltaSeconds)
            {
                if (m_CurrentTick < latestTick)
                {
                    m_VisualTime = Math.Max(0f, m_VisualTime + Math.Max(0f, deltaSeconds));
                    m_VisualContinuousTime = Math.Max(0d, m_VisualContinuousTime + Math.Max(0f, deltaSeconds));
                }
                else if (m_PreviousTick < m_CurrentTick &&
                         (IsCyclicMarkerGroup || m_PreviousCycle == Cycle))
                {
                    m_VisualTime = m_PreviousTime + (m_CurrentTime - m_PreviousTime) * Math.Clamp(alpha, 0f, 1f);
                    m_VisualContinuousTime = m_PreviousContinuousTime +
                                             (m_CurrentContinuousTime - m_PreviousContinuousTime) *
                                             Math.Clamp(alpha, 0f, 1f);
                }
                else
                {
                    m_VisualTime = m_CurrentTime;
                    m_VisualContinuousTime = m_CurrentContinuousTime;
                }

                AnimationMarkerSyncBinding binding = Producer.Animation.MarkerSync;
                if (binding != null && binding.IsMarkerGroup)
                {
                    if (binding.SequenceTopology == BTSMTL.Timeline.AnimationMarkerSequenceTopology.Finite)
                    {
                        m_VisualContinuousTime = Math.Clamp(m_VisualContinuousTime, 0d, binding.DurationSeconds);
                        m_VisualTime = (float)m_VisualContinuousTime;
                        return new AnimationMarkerSyncRawSample(
                            playbackId,
                            Producer.LayerId,
                            binding,
                            m_VisualTime,
                            m_VisualContinuousTime,
                            0);
                    }
                    int cycle = (int)Math.Floor(m_VisualContinuousTime / binding.DurationSeconds);
                    float localTime = (float)(m_VisualContinuousTime - cycle * binding.DurationSeconds);
                    return new AnimationMarkerSyncRawSample(
                        playbackId,
                        Producer.LayerId,
                        binding,
                        localTime,
                        m_VisualContinuousTime,
                        cycle);
                }
                return new AnimationMarkerSyncRawSample(
                    playbackId,
                    Producer.LayerId,
                    binding,
                    m_VisualTime,
                    m_VisualContinuousTime,
                    Cycle);
            }

            bool IsCyclicMarkerGroup =>
                Producer.Animation.MarkerSync != null &&
                Producer.Animation.MarkerSync.IsMarkerGroup &&
                Producer.Animation.MarkerSync.SequenceTopology == BTSMTL.Timeline.AnimationMarkerSequenceTopology.Cyclic;

            double ToContinuousTime(float sampleTime, int cycle)
            {
                AnimationMarkerSyncBinding binding = Producer.Animation.MarkerSync;
                return binding != null && binding.IsMarkerGroup &&
                       binding.SequenceTopology == BTSMTL.Timeline.AnimationMarkerSequenceTopology.Cyclic
                    ? Math.Max(0d, cycle * (double)binding.DurationSeconds + sampleTime)
                    : Math.Max(0f, sampleTime);
            }
        }

        readonly struct AnimationTerminalState
        {
            public AnimationTerminalState(
                CharacterPresentationCommandKind kind,
                AnimationPlaybackId playbackId,
                EventId eventId)
            {
                Kind = kind;
                PlaybackId = playbackId;
                EventId = eventId;
            }

            public CharacterPresentationCommandKind Kind { get; }
            public AnimationPlaybackId PlaybackId { get; }
            public EventId EventId { get; }
        }
    }
}
