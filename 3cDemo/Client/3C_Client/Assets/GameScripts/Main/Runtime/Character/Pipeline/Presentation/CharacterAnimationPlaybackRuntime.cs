using System;
using System.Collections.Generic;
using Animancer;
using BTSMTL.Diagnostics;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.BlendStack;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Pipeline.Animation.Lifecycle;
using ThirdPersonCharacter.Pipeline.Animation.Presentation;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    public sealed class CharacterAnimationPlaybackRuntime : IDisposable
    {
        readonly CharacterAnimationPresentationBindingIndex m_Bindings;
        readonly AnimationPoseRequestWorkspace m_RequestWorkspace;
        readonly AnimationPosePlayableGraphRuntime m_PoseRuntime;
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
        readonly Dictionary<AnimationChannelId, AnimationSelectionState> m_Selections =
            new Dictionary<AnimationChannelId, AnimationSelectionState>();
        readonly Dictionary<AnimationPlaybackId, AnimationSamplingState> m_Sampling =
            new Dictionary<AnimationPlaybackId, AnimationSamplingState>();
        readonly Dictionary<AnimationPlaybackId, AnimationMarkerSyncRawSample> m_RawSamples =
            new Dictionary<AnimationPlaybackId, AnimationMarkerSyncRawSample>();
        readonly Dictionary<AnimationPlaybackId, AnimationMarkerSyncEffectiveSample> m_EffectiveSamples =
            new Dictionary<AnimationPlaybackId, AnimationMarkerSyncEffectiveSample>();
        readonly Dictionary<AnimationPoseSourceId, ResolvedAnimationPoseRequest> m_ResolvedRequests =
            new Dictionary<AnimationPoseSourceId, ResolvedAnimationPoseRequest>();
        readonly List<AnimationPlaybackId> m_RemoveSampling = new List<AnimationPlaybackId>();
        readonly List<AnimationTerminalState> m_Terminals = new List<AnimationTerminalState>();

        ulong m_SelectionSequence;
        ulong m_PresentationRequestSequence;
        ulong m_SourceContinuityIdentity;
        ulong m_RequestWorkspaceCompletionIdentity;
        bool m_Disposed;

        public CharacterAnimationPlaybackRuntime(
            CharacterPresentationSemanticContract contract,
            CharacterPresentationProjection projection,
            AnimancerComponent animancer,
            CharacterAnimationRigBinding rigBinding,
            bool ownsGraphClock)
        {
            if (contract == null)
                throw new ArgumentNullException(nameof(contract));
            if (projection == null)
                throw new ArgumentNullException(nameof(projection));
            projection.RequireContract(contract);
            var errors = new List<string>();
            m_Bindings = CharacterAnimationPresentationBindingIndex.Build(projection, contract, errors);
            if (!m_Bindings.IsValid)
                throw new InvalidOperationException(string.Join("\n", errors));
            try
            {
                m_RequestWorkspace = new AnimationPoseRequestWorkspace(m_Bindings.WorkspaceLayout);
                m_PoseRuntime = new AnimationPosePlayableGraphRuntime(
                    animancer,
                    rigBinding,
                    m_Bindings,
                    ownsGraphClock);
                m_Lifecycle = new AnimationPlaybackLifecycle(m_Bindings);
            }
            catch
            {
                m_PoseRuntime?.Dispose();
                m_RequestWorkspace?.Dispose();
                throw;
            }
        }

        public IReadOnlyList<AnimationPlaybackId> RetiredPlaybacks => m_RetiredPlaybacks;
        public IReadOnlyList<AnimationPlaybackLifecycleSnapshot> Snapshots => m_Snapshots;
        public IReadOnlyList<AnimationMarkerSyncRelationSnapshot> MarkerSyncSnapshots => m_MarkerSyncSnapshots;
        public IReadOnlyList<AnimationMarkerSyncPlaybackSnapshot> MarkerSyncPlaybackSnapshots => m_MarkerSyncPlaybackSnapshots;
        public bool HasRequiredOutput
        {
            get
            {
                foreach (KeyValuePair<PoseSlotId, ResolvedAnimationPoseSlot> pair in m_Bindings.Slots)
                {
                    ResolvedAnimationPoseSlot slot = pair.Value;
                    if (slot.OutputPolicy != PoseSlotOutputPolicy.RequireOutput)
                        continue;
                    if (!m_Selections.TryGetValue(slot.AnimationChannelId, out AnimationSelectionState selection) ||
                        !selection.HasPlayback || !m_Sampling.ContainsKey(selection.PlaybackId))
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
                    throw new ArgumentException(
                        $"Presentation command '{command.Kind}' is not an animation playback command.",
                        nameof(command));
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
                    if (m_Selections.TryGetValue(producer.AnimationChannelId, out AnimationSelectionState selection) &&
                        selection.EventId.Equals(command.Header.EventId))
                    {
                        m_Selections[producer.AnimationChannelId] = AnimationSelectionState.Empty(
                            producer.AnimationChannelId,
                            command.Header.Tick.Value,
                            command.Header.Sequence);
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
                    throw new ArgumentException(
                        $"Presentation command '{command.Kind}' is not an animation playback command.",
                        nameof(command));
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
                    if (current.Kind != CharacterPresentationCommandKind.SelectProducer ||
                        currentProducer.AnimationChannelId != replacementProducer.AnimationChannelId)
                    {
                        throw new InvalidOperationException(
                            "Animation selection replacement changed its Animation Channel or command kind.");
                    }
                    PublishSelection(replacement, replacementProducer);
                    break;
                case CharacterPresentationCommandKind.SampleProducer:
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
                        m_Sampling.Add(replacementPlayback, CreateSamplingState(replacementProducer, replacement));
                    break;
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

        public FinalAnimationPoseFrame Present(
            ulong latestSimulationTick,
            float interpolationAlpha,
            float presentationDeltaSeconds,
            RuntimeDiagnosticsContext diagnostics = null)
        {
            RequireAlive();
            if (!float.IsFinite(interpolationAlpha) || !float.IsFinite(presentationDeltaSeconds) ||
                presentationDeltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(presentationDeltaSeconds));
            }

            foreach (AnimationSelectionState selection in m_Selections.Values)
            {
                m_Commands.EnqueueSelection(selection.HasPlayback
                    ? AnimationChannelSelection.Select(
                        selection.AnimationChannelId,
                        selection.PlaybackId,
                        latestSimulationTick,
                        NextSelectionSequence())
                    : AnimationChannelSelection.Empty(
                        selection.AnimationChannelId,
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
            m_Lifecycle.CollectSampleDemand(m_CommandBuffer, m_PoseRuntime.Stacks, m_DemandedPlaybacks);
            ResolveRawAndEffectiveSamples(
                latestSimulationTick,
                interpolationAlpha,
                presentationDeltaSeconds,
                diagnostics);

            m_PoseRuntime.Advance(presentationDeltaSeconds);
            m_RequestWorkspace.BeginFrame(NextRequestWorkspaceCompletionIdentity());
            m_ResolvedRequests.Clear();
            foreach (AnimationPlaybackId playbackId in m_DemandedPlaybacks)
            {
                if (!m_Sampling.TryGetValue(playbackId, out AnimationSamplingState sampling) ||
                    !m_EffectiveSamples.TryGetValue(playbackId, out AnimationMarkerSyncEffectiveSample effective))
                {
                    continue;
                }
                AnimationBlendStackRuntime stack = m_PoseRuntime.RequireStack(sampling.Producer.AnimationChannelId);
                AnimationBlendTransitionIdentity transition = stack.ResolveExpectedTransitionIdentity(
                    sampling.Producer.ProgramProducerIndex,
                    false);
                ResolvedAnimationPoseRequest request = TimelineAnimationPoseRequestResolver.Resolve(
                    m_Bindings,
                    m_RequestWorkspace,
                    sampling.Producer.AnimationChannelId,
                    sampling.SourceId,
                    sampling.SourcePoseContinuityIdentity,
                    NextPresentationRequestSequence(),
                    sampling.Producer.ProgramProducerIndex,
                    effective.LocalTime,
                    effective.ContinuousTime,
                    effective.Cycle,
                    sampling.ResolveVisualTimeScale(effective, presentationDeltaSeconds),
                    sampling.IsTrackLooping,
                    transition);
                m_ResolvedRequests.Add(request.SourceId, request);
                m_Commands.EnqueuePoseRequest(latestSimulationTick, request);
            }

            m_Commands.CopyPendingTo(m_CommandBuffer);
            m_Lifecycle.Apply(
                m_CommandBuffer,
                m_PoseRuntime,
                NextPresentationRequestSequence);
            FinalAnimationPoseFrame finalPose = m_PoseRuntime.Evaluate(
                presentationDeltaSeconds,
                m_ResolvedRequests);
            m_Lifecycle.BuildSnapshot(m_PoseRuntime.Stacks, m_Snapshots);
            m_MarkerSync.BuildPlaybackSnapshot(m_MarkerSyncPlaybackSnapshots);
            m_MarkerSync.BuildRelationSnapshot(m_MarkerSyncSnapshots);
            AttachMarkerLifecyclePhases();
            PruneUnreferencedSampling();
            m_MarkerSync.Retire(m_RetiredPlaybacks);
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
            return finalPose;
        }

        public void Reset()
        {
            if (m_Disposed)
                return;
            m_PoseRuntime.Reset();
            m_RequestWorkspace.Reset();
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
            m_ResolvedRequests.Clear();
            m_RemoveSampling.Clear();
            m_MarkerSync.Reset();
            m_Terminals.Clear();
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            m_PoseRuntime.Dispose();
            m_RequestWorkspace.Dispose();
        }

        void ResolveRawAndEffectiveSamples(
            ulong latestSimulationTick,
            float interpolationAlpha,
            float presentationDeltaSeconds,
            RuntimeDiagnosticsContext diagnostics)
        {
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
                    if (!selection.HasPlayback ||
                        !m_RawSamples.TryGetValue(selection.PlaybackId, out AnimationMarkerSyncRawSample target))
                    {
                        continue;
                    }
                    AnimationBlendStackRuntime stack = m_PoseRuntime.RequireStack(selection.AnimationChannelId);
                    AnimationPlaybackId sourcePlayback = default;
                    for (int entryIndex = stack.EntryCount - 1; entryIndex >= 0; entryIndex--)
                    {
                        AnimationBlendEntryId entry = stack.GetEntryId(entryIndex);
                        if (entry.EmptyTarget || entry.SourceId.PlaybackId.Equals(selection.PlaybackId))
                            continue;
                        sourcePlayback = entry.SourceId.PlaybackId;
                        break;
                    }
                    if (!sourcePlayback.IsValid)
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
        }

        void PublishSelection(CharacterPresentationCommand command, CharacterPresentationProducerEntry producer)
        {
            var playbackId = new AnimationPlaybackId(producer.ProducerId, command.ProducerGeneration);
            if (!m_Selections.TryGetValue(producer.AnimationChannelId, out AnimationSelectionState current) ||
                IsNewer(command.Header, current.Tick, current.Sequence))
            {
                m_Selections[producer.AnimationChannelId] = AnimationSelectionState.Select(
                    producer.AnimationChannelId,
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
                m_Sampling.Add(playbackId, CreateSamplingState(producer, command));
                return;
            }
            sampling.Capture(command);
        }

        AnimationSamplingState CreateSamplingState(
            CharacterPresentationProducerEntry producer,
            CharacterPresentationCommand command)
        {
            return new AnimationSamplingState(
                producer,
                command,
                NextSourceContinuityIdentity());
        }

        void PublishTerminal(CharacterPresentationCommand command, CharacterPresentationProducerEntry producer)
        {
            var playbackId = new AnimationPlaybackId(producer.ProducerId, command.ProducerGeneration);
            m_Terminals.Add(new AnimationTerminalState(command.Kind, playbackId, command.Header.EventId));
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
            if (producer == null || producer.Kind != CharacterPresentationProducerKind.Animation ||
                producer.Animation == null || !producer.AnimationChannelId.IsValid)
            {
                throw new InvalidOperationException(
                    $"Presentation command '{commandKind}' targets a non-animation producer.");
            }
        }

        bool IsSamplingRetained(AnimationPlaybackId playbackId) =>
            m_Lifecycle.Retains(playbackId, m_PoseRuntime.Stacks);

        void PruneUnreferencedSampling()
        {
            m_RetiredPlaybacks.Clear();
            m_RemoveSampling.Clear();
            foreach (AnimationPlaybackId playbackId in m_Sampling.Keys)
            {
                if (!IsSamplingRetained(playbackId))
                    m_RemoveSampling.Add(playbackId);
            }
            for (int i = 0; i < m_RemoveSampling.Count; i++)
            {
                AnimationPlaybackId playbackId = m_RemoveSampling[i];
                m_Sampling.Remove(playbackId);
                m_RetiredPlaybacks.Add(playbackId);
            }
        }

        ulong NextSelectionSequence() => Next(ref m_SelectionSequence, "selection");
        ulong NextPresentationRequestSequence() => Next(ref m_PresentationRequestSequence, "pose request");
        ulong NextSourceContinuityIdentity() => Next(ref m_SourceContinuityIdentity, "source continuity");
        ulong NextRequestWorkspaceCompletionIdentity() => Next(ref m_RequestWorkspaceCompletionIdentity, "request workspace completion");

        static ulong Next(ref ulong value, string name)
        {
            if (value == ulong.MaxValue)
                throw new InvalidOperationException($"Animation {name} identity was exhausted.");
            value++;
            return value;
        }

        void RequireAlive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(CharacterAnimationPlaybackRuntime));
        }

        static bool IsNewer(CharacterPresentationEventHeader header, ulong tick, ulong sequence) =>
            header.Tick.Value > tick || header.Tick.Value == tick && header.Sequence > sequence;

        readonly struct AnimationSelectionState
        {
            AnimationSelectionState(
                AnimationChannelId animationChannelId,
                AnimationPlaybackId playbackId,
                bool hasPlayback,
                EventId eventId,
                ulong tick,
                ulong sequence)
            {
                AnimationChannelId = animationChannelId;
                PlaybackId = playbackId;
                HasPlayback = hasPlayback;
                EventId = eventId;
                Tick = tick;
                Sequence = sequence;
            }

            public AnimationChannelId AnimationChannelId { get; }
            public AnimationPlaybackId PlaybackId { get; }
            public bool HasPlayback { get; }
            public EventId EventId { get; }
            public ulong Tick { get; }
            public ulong Sequence { get; }

            public static AnimationSelectionState Select(
                AnimationChannelId animationChannelId,
                AnimationPlaybackId playbackId,
                EventId eventId,
                ulong tick,
                ulong sequence) =>
                new AnimationSelectionState(animationChannelId, playbackId, true, eventId, tick, sequence);

            public static AnimationSelectionState Empty(
                AnimationChannelId animationChannelId,
                ulong tick,
                ulong sequence) =>
                new AnimationSelectionState(animationChannelId, default, false, default, tick, sequence);
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
            double m_PreviousPresentedEffectiveTime;
            bool m_HasPresentedEffectiveTime;
            bool m_WasRebased;

            public AnimationSamplingState(
                CharacterPresentationProducerEntry producer,
                CharacterPresentationCommand command,
                ulong sourcePoseContinuityIdentity)
            {
                Producer = producer ?? throw new ArgumentNullException(nameof(producer));
                if (sourcePoseContinuityIdentity == 0)
                    throw new ArgumentOutOfRangeException(nameof(sourcePoseContinuityIdentity));
                var playbackId = new AnimationPlaybackId(producer.ProducerId, command.ProducerGeneration);
                SourceId = new AnimationPoseSourceId(
                    playbackId,
                    AnimationPoseSourceKind.Timeline,
                    new AnimationPoseSelectionGeneration(command.ProducerGeneration));
                SourcePoseContinuityIdentity = sourcePoseContinuityIdentity;
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
                m_PreviousPresentedEffectiveTime = m_CurrentContinuousTime;
            }

            public CharacterPresentationProducerEntry Producer { get; }
            public AnimationPoseSourceId SourceId { get; }
            public ulong SourcePoseContinuityIdentity { get; }
            public int Cycle { get; private set; }
            public EventId EventId { get; private set; }
            public bool IsTrackLooping =>
                Producer.Animation.MarkerSync != null &&
                Producer.Animation.MarkerSync.IsMarkerGroup &&
                Producer.Animation.MarkerSync.SequenceTopology == BTSMTL.Timeline.AnimationMarkerSequenceTopology.Cyclic;

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

            public float ResolveVisualTimeScale(
                AnimationMarkerSyncEffectiveSample effective,
                float presentationDeltaSeconds)
            {
                double current = effective.ContinuousTime;
                if (double.IsNaN(current) || double.IsInfinity(current) || current < 0d)
                    throw new InvalidOperationException($"Animation playback '{effective.PlaybackId}' produced an invalid effective time.");
                bool beganRebase = effective.Rebased && !m_WasRebased;
                m_WasRebased = effective.Rebased;
                if (!m_HasPresentedEffectiveTime || beganRebase || presentationDeltaSeconds <= 0.000001f)
                {
                    m_PreviousPresentedEffectiveTime = current;
                    m_HasPresentedEffectiveTime = true;
                    return 0f;
                }
                double elapsed = current - m_PreviousPresentedEffectiveTime;
                m_PreviousPresentedEffectiveTime = current;
                if (elapsed < -0.000001d)
                    throw new InvalidOperationException($"Animation playback '{effective.PlaybackId}' effective time moved backwards without a rebase.");
                float scale = (float)(Math.Max(0d, elapsed) / presentationDeltaSeconds);
                if (!float.IsFinite(scale))
                    throw new InvalidOperationException($"Animation playback '{effective.PlaybackId}' produced an invalid visual time scale.");
                return scale;
            }

            public void Replace(CharacterPresentationCommand command)
            {
                m_HasPresentedEffectiveTime = false;
                m_WasRebased = false;
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
                         (IsTrackLooping || m_PreviousCycle == Cycle))
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
                            Producer.AnimationChannelId,
                            binding,
                            m_VisualTime,
                            m_VisualContinuousTime,
                            0);
                    }
                    int cycle = (int)Math.Floor(m_VisualContinuousTime / binding.DurationSeconds);
                    float localTime = (float)(m_VisualContinuousTime - cycle * binding.DurationSeconds);
                    return new AnimationMarkerSyncRawSample(
                        playbackId,
                        Producer.AnimationChannelId,
                        binding,
                        localTime,
                        m_VisualContinuousTime,
                        cycle);
                }
                return new AnimationMarkerSyncRawSample(
                    playbackId,
                    Producer.AnimationChannelId,
                    binding,
                    m_VisualTime,
                    m_VisualContinuousTime,
                    Cycle);
            }

            double ToContinuousTime(float sampleTime, int cycle) =>
                Math.Max(0d, cycle * (double)Producer.Animation.DurationSeconds + sampleTime);
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
