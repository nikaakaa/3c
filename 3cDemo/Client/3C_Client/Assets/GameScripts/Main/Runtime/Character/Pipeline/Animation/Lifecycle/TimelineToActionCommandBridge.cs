using System;
using System.Collections.Generic;
using System.Globalization;
using BTSMTL.Timeline;
using BTSMTL.Timeline.Runtime;
using ThirdPersonCamera;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation.Lifecycle
{
    internal sealed class TimelineToActionCommandBridge : IDisposable
    {
        sealed class ProducerState
        {
            internal ProducerState(
                AnimationPlaybackId playbackId,
                ulong actionInstanceId,
                AnimationChannelId animationChannelId,
                string programProducerId)
            {
                PlaybackId = playbackId;
                ActionInstanceId = actionInstanceId;
                AnimationChannelId = animationChannelId;
                ProgramProducerId = programProducerId;
            }

            internal AnimationPlaybackId PlaybackId { get; }
            internal ulong ActionInstanceId { get; }
            internal AnimationChannelId AnimationChannelId { get; }
            internal string ProgramProducerId { get; }
        }

        sealed class PlaybackState
        {
            internal PlaybackState(ulong generation, ulong logicTick, ulong presentationFrame)
            {
                Generation = generation;
                LogicTick = logicTick;
                PresentationFrame = presentationFrame;
            }

            internal ulong Generation { get; }
            internal ulong LogicTick { get; set; }
            internal ulong PresentationFrame { get; set; }
            internal readonly Dictionary<AnimationProducerId, ProducerState> Producers = new();
        }

        struct FramePlaybackSnapshot
        {
            internal ulong Handle;
            internal PlaybackState State;
            internal ulong LogicTick;
            internal ulong PresentationFrame;
            internal int ProducerOffset;
            internal int ProducerCount;
        }

        readonly FramePlaybackSnapshot[] m_FramePlaybacks;
        readonly KeyValuePair<AnimationProducerId, ProducerState>[] m_FrameProducers;
        int m_FramePlaybackCount;
        int m_FrameProducerCount;
        ulong m_FramePublicationSequence;
        bool m_FrameActive;

        internal void BeginFrame()
        {
            if (m_FrameActive)
                throw new InvalidOperationException("Timeline animation command frame is already open.");
            int producerCount = 0;
            foreach (PlaybackState state in m_Playbacks.Values)
                producerCount += state.Producers.Count;
            if (m_Playbacks.Count > m_FramePlaybacks.Length || producerCount > m_FrameProducers.Length)
                throw new InvalidOperationException("Timeline animation frame exceeds its prepared command capacity.");
            m_FramePlaybackCount = 0;
            m_FrameProducerCount = 0;
            foreach (var pair in m_Playbacks)
            {
                PlaybackState state = pair.Value;
                m_FramePlaybacks[m_FramePlaybackCount++] = new FramePlaybackSnapshot
                {
                    Handle = pair.Key, State = state, LogicTick = state.LogicTick,
                    PresentationFrame = state.PresentationFrame,
                    ProducerOffset = m_FrameProducerCount, ProducerCount = state.Producers.Count
                };
                foreach (var producer in state.Producers)
                    m_FrameProducers[m_FrameProducerCount++] = producer;
            }
            m_FramePublicationSequence = m_Inbox.PublicationSequence;
            m_FrameActive = true;
        }

        internal void CommitFrame()
        {
            m_FrameActive = false;
            ClearFrameSnapshot();
        }

        internal void DiscardFrame()
        {
            if (!m_FrameActive)
                return;
            m_Inbox.DiscardPublicationsAfter(m_FramePublicationSequence);
            m_Playbacks.Clear();
            for (int i = 0; i < m_FramePlaybackCount; i++)
            {
                FramePlaybackSnapshot snapshot = m_FramePlaybacks[i];
                PlaybackState state = snapshot.State;
                state.LogicTick = snapshot.LogicTick;
                state.PresentationFrame = snapshot.PresentationFrame;
                state.Producers.Clear();
                for (int j = 0; j < snapshot.ProducerCount; j++)
                {
                    var producer = m_FrameProducers[snapshot.ProducerOffset + j];
                    state.Producers.Add(producer.Key, producer.Value);
                }
                m_Playbacks.Add(snapshot.Handle, state);
            }
            m_FrameActive = false;
            ClearFrameSnapshot();
        }

        void ClearFrameSnapshot()
        {
            Array.Clear(m_FramePlaybacks, 0, m_FramePlaybackCount);
            Array.Clear(m_FrameProducers, 0, m_FrameProducerCount);
            m_FramePlaybackCount = 0;
            m_FrameProducerCount = 0;
        }

        readonly CharacterTimelineHost m_TimelineHost;
        readonly ActionPlaybackCommandInbox m_Inbox;
        readonly Dictionary<ulong, PlaybackState> m_Playbacks = new();
        readonly Dictionary<AnimationProducerId, TimelineAnimationContribution> m_Samples = new();
        readonly List<AnimationProducerId> m_SampleOrder = new();
        readonly List<AnimationProducerId> m_Releases = new();
        bool m_Disposed;

        internal TimelineToActionCommandBridge(
            CharacterTimelineHost timelineHost,
            ActionPlaybackCommandInbox inbox)
        {
            m_TimelineHost = timelineHost ?? throw new ArgumentNullException(nameof(timelineHost));
            m_Inbox = inbox ?? throw new ArgumentNullException(nameof(inbox));
            m_FramePlaybacks = new FramePlaybackSnapshot[inbox.Capacity];
            m_FrameProducers = new KeyValuePair<AnimationProducerId, ProducerState>[inbox.Capacity];
            m_TimelineHost.PresentationFramePrepared += OnPresentationFrame;
            m_TimelineHost.PresentationPlaybackEndPrepared += OnPresentationPlaybackEnded;
        }

        void OnPresentationFrame(TimelineRuntimePresentationFrame frame)
        {
            if (!m_TimelineHost.TryGetPlaybackActionContext(frame.Handle, out TimelinePlaybackActionContext actionContext))
                throw new InvalidOperationException($"Timeline playback '{frame.Handle.Value}' has no Action context.");
            ulong logicTick = frame.LogicTick != 0
                ? frame.LogicTick
                : actionContext.StartLocalLogicTick;
            if (logicTick == 0)
                throw new InvalidOperationException($"Timeline playback '{frame.Handle.Value}' has no Logic tick for presentation output.");

            if (!m_Playbacks.TryGetValue(frame.Handle.Value, out PlaybackState playback))
            {
                playback = new PlaybackState(frame.Generation, logicTick, frame.PresentationFrame);
                m_Playbacks.Add(frame.Handle.Value, playback);
            }
            else if (playback.Generation != frame.Generation)
            {
                ReleaseAll(playback, frame.Handle.Value, logicTick, frame.PresentationFrame, "generation-replaced", true);
                playback = new PlaybackState(frame.Generation, logicTick, frame.PresentationFrame);
                m_Playbacks[frame.Handle.Value] = playback;
            }
            else
            {
                playback.LogicTick = logicTick;
                playback.PresentationFrame = frame.PresentationFrame;
            }

            CollectSamples(frame.Operations.AnimationContributions);
            ReleaseInactive(playback, frame.Handle.Value, logicTick, frame.PresentationFrame, frame.Reason == TimelinePresentationSampleReason.Correction);
            for (int index = 0; index < m_SampleOrder.Count; index++)
            {
                AnimationProducerId producerId = m_SampleOrder[index];
                TimelineAnimationContribution contribution = m_Samples[producerId];
                if (!playback.Producers.TryGetValue(producerId, out ProducerState producer))
                {
                    producer = CreateProducer(frame, actionContext, producerId, contribution);
                    playback.Producers.Add(producerId, producer);
                    PublishSelect(producer, frame.Handle.Value, logicTick, frame.PresentationFrame);
                }
                else if (producer.ActionInstanceId != actionContext.ActionInstanceId ||
                         !producer.AnimationChannelId.Equals(contribution.AnimationChannelId))
                {
                    throw new InvalidOperationException(
                        $"Timeline Animation producer '{producerId}' changed ownership within one playback generation.");
                }
                PublishSample(producer, contribution, frame.Handle.Value, logicTick, frame.PresentationFrame);
            }
        }

        void OnPresentationPlaybackEnded(TimelineRuntimePlaybackHandle handle, ulong generation, TimelinePresentationSampleReason reason)
        {
            if (!m_Playbacks.TryGetValue(handle.Value, out PlaybackState playback) || playback.Generation != generation)
                return;
            ReleaseAll(
                playback,
                handle.Value,
                playback.LogicTick,
                playback.PresentationFrame,
                "playback-ended", reason == TimelinePresentationSampleReason.Withdrawn);
            m_Playbacks.Remove(handle.Value);
        }

        void CollectSamples(TimelineRuntimeSampleView<TimelineAnimationContribution> contributions)
        {
            m_Samples.Clear();
            m_SampleOrder.Clear();
            for (int index = 0; index < contributions.Count; index++)
            {
                TimelineAnimationContribution contribution = contributions[index];
                var producerId = new AnimationProducerId(
                    contribution.TimelineAuthoringId,
                    contribution.TrackAuthoringId);
                if (!producerId.IsValid || !contribution.AnimationChannelId.IsValid)
                    throw new InvalidOperationException("Timeline Animation contribution has no stable producer identity.");
                if (!m_Samples.TryGetValue(producerId, out TimelineAnimationContribution current))
                {
                    m_Samples.Add(producerId, contribution);
                    m_SampleOrder.Add(producerId);
                    continue;
                }
                if (contribution.Weight > current.Weight ||
                    contribution.Weight == current.Weight &&
                    string.CompareOrdinal(contribution.ClipAuthoringId, current.ClipAuthoringId) < 0)
                {
                    m_Samples[producerId] = contribution;
                }
            }
        }

        void ReleaseInactive(
            PlaybackState playback,
            ulong handle,
            ulong logicTick,
            ulong presentationFrame,
            bool withdrawn)
        {
            m_Releases.Clear();
            foreach (KeyValuePair<AnimationProducerId, ProducerState> pair in playback.Producers)
            {
                if (!m_Samples.ContainsKey(pair.Key))
                    m_Releases.Add(pair.Key);
            }
            for (int index = 0; index < m_Releases.Count; index++)
            {
                AnimationProducerId producerId = m_Releases[index];
                PublishRelease(
                    playback.Producers[producerId],
                    handle,
                    logicTick,
                    presentationFrame,
                    "clip-ended", withdrawn);
                playback.Producers.Remove(producerId);
            }
        }

        void ReleaseAll(
            PlaybackState playback,
            ulong handle,
            ulong logicTick,
            ulong presentationFrame,
            string reason,
            bool withdrawn = false)
        {
            m_Releases.Clear();
            foreach (AnimationProducerId producerId in playback.Producers.Keys)
                m_Releases.Add(producerId);
            for (int index = 0; index < m_Releases.Count; index++)
            {
                AnimationProducerId producerId = m_Releases[index];
                PublishRelease(
                    playback.Producers[producerId],
                    handle,
                    logicTick,
                    presentationFrame,
                    reason,
                    withdrawn);
            }
            playback.Producers.Clear();
        }

        static ProducerState CreateProducer(
            TimelineRuntimePresentationFrame frame,
            TimelinePlaybackActionContext actionContext,
            AnimationProducerId producerId,
            TimelineAnimationContribution contribution)
        {
            var playbackId = new AnimationPlaybackId(producerId, frame.Generation);
            return new ProducerState(
                playbackId,
                actionContext.ActionInstanceId,
                contribution.AnimationChannelId,
                producerId.ProgramProducerIdentity);
        }

        void PublishSelect(
            ProducerState producer,
            ulong handle,
            ulong logicTick,
            ulong presentationFrame)
        {
            m_Inbox.Publish(ActionAnimationPlaybackCommand.Select(
                CreateEventId("select", producer, handle, presentationFrame, string.Empty),
                logicTick,
                producer.PlaybackId,
                producer.ActionInstanceId,
                producer.AnimationChannelId,
                producer.ProgramProducerId));
        }

        void PublishSample(
            ProducerState producer,
            TimelineAnimationContribution contribution,
            ulong handle,
            ulong logicTick,
            ulong presentationFrame)
        {
            EventId eventId = CreateEventId(
                "sample",
                producer,
                handle,
                presentationFrame,
                contribution.ClipAuthoringId);
            var sample = new ActionProjectedSample(
                eventId,
                logicTick,
                presentationFrame,
                new PresentationPoseSampleTime(contribution.ClipTime, contribution.ContinuousClipTime,
                    contribution.Cycle, contribution.IsLooping, 1f),
                contribution.Weight);
            m_Inbox.Publish(ActionAnimationPlaybackCommand.PresentSample(
                producer.PlaybackId,
                producer.ActionInstanceId,
                producer.AnimationChannelId,
                producer.ProgramProducerId,
                in sample));
        }

        void PublishRelease(
            ProducerState producer,
            ulong handle,
            ulong logicTick,
            ulong presentationFrame,
            string reason,
            bool withdrawn = false)
        {
            EventId eventId = CreateEventId(withdrawn ? "withdraw" : "release", producer, handle, presentationFrame, reason);
            m_Inbox.Publish(withdrawn
                ? ActionAnimationPlaybackCommand.Withdraw(eventId, logicTick, producer.PlaybackId,
                    producer.ActionInstanceId, producer.AnimationChannelId, producer.ProgramProducerId)
                : ActionAnimationPlaybackCommand.Release(eventId, logicTick, producer.PlaybackId,
                    producer.ActionInstanceId, producer.AnimationChannelId, producer.ProgramProducerId));
        }

        static EventId CreateEventId(
            string kind,
            ProducerState producer,
            ulong handle,
            ulong presentationFrame,
            string detail)
        {
            return new EventId(StableHash.Compute(
                "timeline-presentation-animation",
                kind,
                handle.ToString(CultureInfo.InvariantCulture),
                producer.PlaybackId.Generation.ToString(CultureInfo.InvariantCulture),
                producer.PlaybackId.ProducerId.TimelineAuthoringId,
                producer.PlaybackId.ProducerId.TrackAuthoringId,
                presentationFrame.ToString(CultureInfo.InvariantCulture),
                detail ?? string.Empty));
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            foreach (KeyValuePair<ulong, PlaybackState> pair in m_Playbacks)
            {
                ReleaseAll(
                    pair.Value,
                    pair.Key,
                    pair.Value.LogicTick,
                    pair.Value.PresentationFrame,
                    "bridge-disposed");
            }
            m_Playbacks.Clear();
            m_TimelineHost.PresentationFramePrepared -= OnPresentationFrame;
            m_TimelineHost.PresentationPlaybackEndPrepared -= OnPresentationPlaybackEnded;
        }
    }

internal sealed class TimelinePresentationEventBridge : IDisposable
{
    readonly struct CameraEventKey : IEquatable<CameraEventKey>
    {
        CameraEventKey(ulong handle, ulong generation, string marker, string track, string producer, int cycle)
        {
            Handle = handle;
            Generation = generation;
            Marker = marker;
            Track = track;
            Producer = producer;
            Cycle = cycle;
        }

        internal static CameraEventKey ForMarker(ulong handle, ulong generation, string marker, string producer) =>
            new(handle, generation, marker, string.Empty, producer, 0);
        internal static CameraEventKey ForClip(ulong handle, ulong generation, string track, string clip, int cycle) =>
            new(handle, generation, null, track, clip, cycle);
        internal readonly ulong Handle;
        internal readonly ulong Generation;
        internal readonly string Marker;
        internal readonly string Track;
        internal readonly string Producer;
        internal readonly int Cycle;
        public bool Equals(CameraEventKey other) => Handle == other.Handle && Generation == other.Generation && Cycle == other.Cycle &&
            string.Equals(Marker, other.Marker, StringComparison.Ordinal) && string.Equals(Track, other.Track, StringComparison.Ordinal) &&
            string.Equals(Producer, other.Producer, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is CameraEventKey other && Equals(other);
        public override int GetHashCode() => unchecked((((Handle.GetHashCode() * 397 ^ Generation.GetHashCode()) * 397 ^ Cycle) * 397 ^
            (Marker == null ? 0 : StringComparer.Ordinal.GetHashCode(Marker))) * 397 ^
            StringComparer.Ordinal.GetHashCode(Track) ^ StringComparer.Ordinal.GetHashCode(Producer));
    }

    readonly struct CameraEventState
    {
        internal CameraEventState(
            CameraEventKey key,
            ulong playbackHandle,
            CharacterPresentationCommand activation,
            CharacterPresentationCommand retirement,
            ulong generation,
            long markerTime = 0,
            int markerCycle = 0)
        {
            Key = key;
            PlaybackHandle = playbackHandle;
            Generation = generation;
            Activation = activation;
            Retirement = retirement;
            MarkerTime = markerTime;
            MarkerCycle = markerCycle;
        }

        internal CameraEventKey Key { get; }
        internal long MarkerTime { get; }
        internal int MarkerCycle { get; }
        internal ulong PlaybackHandle { get; }
        internal ulong Generation { get; }
        internal CharacterPresentationCommand Activation { get; }
        internal CharacterPresentationCommand Retirement { get; }
    }

    readonly CharacterTimelineHost m_TimelineHost;
    readonly ICharacterPresentationDomainRuntime m_Runtime;
    readonly ActorId m_ActorId;
    readonly Dictionary<CameraEventKey, CameraEventState> m_Active;
    readonly Dictionary<CameraEventKey, CameraEventState> m_FrameBaseline;
    readonly HashSet<CameraEventKey> m_Alive;
    readonly List<CameraEventKey> m_Retired;
    readonly int m_RequestCapacity;
    bool m_FrameOpen;
    bool m_Disposed;

    internal TimelinePresentationEventBridge(
        CharacterTimelineHost timelineHost,
        ICharacterPresentationDomainRuntime runtime,
        ActorId actorId,
        int requestCapacity)
    {
        m_TimelineHost = timelineHost ?? throw new ArgumentNullException(nameof(timelineHost));
        m_Runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        m_ActorId = actorId;
        if (requestCapacity < 0)
            throw new ArgumentOutOfRangeException(nameof(requestCapacity));
        m_RequestCapacity = requestCapacity;
        m_Active = new Dictionary<CameraEventKey, CameraEventState>(requestCapacity);
        m_FrameBaseline = new Dictionary<CameraEventKey, CameraEventState>(requestCapacity);
        m_Alive = new HashSet<CameraEventKey>(requestCapacity);
        m_Retired = new List<CameraEventKey>(requestCapacity);
        m_TimelineHost.PresentationFramePrepared += OnPresentationFrame;
        m_TimelineHost.PresentationPlaybackEndPrepared += OnPresentationPlaybackEnded;
    }

    internal void BeginFrame()
    {
        if (m_FrameOpen)
            throw new InvalidOperationException("Timeline Camera event candidate is already open.");
        m_FrameBaseline.Clear();
        foreach (KeyValuePair<CameraEventKey, CameraEventState> entry in m_Active)
            m_FrameBaseline.Add(entry.Key, entry.Value);
        m_FrameOpen = true;
    }

    internal void CommitFrame()
    {
        m_FrameBaseline.Clear();
        m_FrameOpen = false;
    }

    internal void DiscardFrame()
    {
        if (!m_FrameOpen)
            return;
        m_Active.Clear();
        foreach (KeyValuePair<CameraEventKey, CameraEventState> entry in m_FrameBaseline)
            m_Active.Add(entry.Key, entry.Value);
        m_FrameBaseline.Clear();
        m_FrameOpen = false;
    }

    void OnPresentationFrame(TimelineRuntimePresentationFrame frame)
    {
        m_Alive.Clear();
        if (frame.Operations.CameraStates.Count != 0 || frame.Operations.CameraCues.Count != 0 ||
            frame.Operations.CameraResponses.Count != 0 || frame.Operations.CameraResources.Count != 0 || frame.Events.Count != 0)
        {
            if (!m_TimelineHost.TryGetPresentationExecutionContext(frame.Handle, out TimelinePresentationExecutionContext context))
                throw new InvalidOperationException($"Timeline playback '{frame.Handle.Value}' has no Presentation execution identity.");
            CollectCameraEvents(frame, context, m_Alive);
            for (int index = 0; index < frame.Events.Count; index++)
            {
                TimelineRuntimePresentationEvent marker = frame.Events[index];
                ReadOnlySpan<PresentationGraphCameraOutput> outputs = m_TimelineHost.EvaluatePresentationMarker(in marker, frame.PresentationFrame);
                EventId eventId = marker.EventId;
                for (int outputIndex = 0; outputIndex < outputs.Length; outputIndex++)
                    AddMarkerCamera(frame, context, marker, eventId, outputs[outputIndex]);
            }
        }
        foreach (KeyValuePair<CameraEventKey, CameraEventState> entry in m_Active)
            if (entry.Key.Marker != null && entry.Key.Handle == frame.Handle.Value && entry.Key.Generation == frame.Generation &&
                (entry.Value.MarkerCycle < frame.Cycle || entry.Value.MarkerCycle == frame.Cycle && entry.Value.MarkerTime <= frame.Time.Raw))
                m_Alive.Add(entry.Key);
        RetireInactive(m_Alive, frame.Handle.Value, frame.Reason == TimelinePresentationSampleReason.Correction);
    }

    void OnPresentationPlaybackEnded(TimelineRuntimePlaybackHandle handle, ulong generation, TimelinePresentationSampleReason reason)
    {
        m_Alive.Clear();
        RetireInactive(m_Alive, handle.Value, reason == TimelinePresentationSampleReason.Withdrawn, generation);
    }

    void AddMarkerCamera(TimelineRuntimePresentationFrame frame, in TimelinePresentationExecutionContext context,
        in TimelineRuntimePresentationEvent marker, EventId eventId, in PresentationGraphCameraOutput output)
    {
        var key = CameraEventKey.ForMarker(frame.Handle.Value, frame.Generation, marker.MarkerAuthoringId, output.Producer);
        if (m_Active.TryGetValue(key, out CameraEventState previous))
        {
            m_Runtime.Retire(previous.Activation);
            m_Active.Remove(key);
        }
        if (m_RequestCapacity == 0 || m_Active.Count == m_RequestCapacity)
            throw new InvalidOperationException("Presentation Marker Camera output exceeds the composed domain capacity.");
        var header = new CharacterPresentationEventHeader(eventId, m_ActorId, new SimulationTick(frame.LogicTick),
            context.Activation, marker.TraversalIndex, "timeline.marker.camera");
        var activation = new CharacterPresentationCommand(header, CharacterPresentationCommandKind.Camera,
            output.Producer, marker.Time.ToSingle(), output.Request.Weight, context.Activation.Generation,
            marker.Cycle, context.ActionInstanceId, 1f, cameraRequest: output.Request);
        var retirement = new CharacterPresentationCommand(header, CharacterPresentationCommandKind.Camera,
            output.Producer, marker.Time.ToSingle(), output.Retirement.Weight, context.Activation.Generation,
            marker.Cycle, context.ActionInstanceId, 1f, cameraRequest: output.Retirement);
        m_Runtime.Publish(activation);
        m_Active.Add(key, new CameraEventState(key, frame.Handle.Value, activation, retirement, frame.Generation, marker.Time.Raw, marker.Cycle));
    }

    void CollectCameraEvents(
        TimelineRuntimePresentationFrame frame,
        in TimelinePresentationExecutionContext context,
        HashSet<CameraEventKey> alive)
    {
        for (int index = 0; index < frame.Operations.CameraStates.Count; index++)
        {
            TimelineCameraStateSample sample = frame.Operations.CameraStates[index];
            CameraEventKey key = CameraEventKey.ForClip(frame.Handle.Value, frame.Generation,
                sample.TrackAuthoringId, sample.ClipAuthoringId, 0);
            if (!m_Active.ContainsKey(key))
            {
                PresentationCameraRequest activation = PresentationCameraRequest.Sequence(
                    PresentationCameraRequestLifecycle.Activate,
                    RequireSequenceId(sample.SequenceId),
                    (int)sample.Mode,
                    (int)sample.InterruptPolicy,
                    sample.Priority,
                    sample.Weight,
                    sample.BlendInSeconds,
                    sample.BlendOutSeconds,
                    sample.TargetKey,
                    context.Activation.Source.Identity);
                PresentationCameraRequest retirement = PresentationCameraRequest.Sequence(
                    PresentationCameraRequestLifecycle.Retire,
                    RequireSequenceId(sample.SequenceId),
                    (int)sample.Mode,
                    (int)sample.InterruptPolicy,
                    sample.Priority,
                    sample.Weight,
                    sample.BlendInSeconds,
                    sample.BlendOutSeconds,
                    sample.TargetKey,
                    context.Activation.Source.Identity);
                AddCamera(key, frame, context, activation, retirement);
            }

            alive.Add(key);
        }

        for (int index = 0; index < frame.Operations.CameraCues.Count; index++)
        {
            TimelineCameraCueSample sample = frame.Operations.CameraCues[index];
            CameraEventKey key = CameraEventKey.ForClip(frame.Handle.Value, frame.Generation,
                sample.TrackAuthoringId, sample.ClipAuthoringId, sample.Cycle);
            if (!m_Active.ContainsKey(key))
            {
                string requestId = sample.ClipAuthoringId;
                PresentationCameraRequest activation = PresentationCameraRequest.Effect(
                    PresentationCameraRequestLifecycle.Activate,
                    requestId,
                    (int)RequireCueEffectKind(sample.CueKind),
                    sample.ResourceId,
                    sample.Priority,
                    Mathf.Clamp01(sample.Intensity),
                    context.Activation.Source.Identity);
                PresentationCameraRequest retirement = PresentationCameraRequest.Effect(
                    PresentationCameraRequestLifecycle.Retire,
                    requestId,
                    (int)RequireCueEffectKind(sample.CueKind),
                    sample.ResourceId,
                    sample.Priority,
                    Mathf.Clamp01(sample.Intensity),
                    context.Activation.Source.Identity);
                AddCamera(key, frame, context, activation, retirement);
            }

            alive.Add(key);
        }

        for (int index = 0; index < frame.Operations.CameraResponses.Count; index++)
        {
            TimelineCameraResponseSample sample = frame.Operations.CameraResponses[index];
            CameraEventKey key = CameraEventKey.ForClip(frame.Handle.Value, frame.Generation,
                sample.TrackAuthoringId, sample.ClipAuthoringId, 0);
            if (!m_Active.ContainsKey(key))
            {
                PresentationCameraRequest activation = PresentationCameraRequest.Response(
                    PresentationCameraRequestLifecycle.Activate,
                    (int)sample.LookResponse,
                    sample.ManualOrbitWeight,
                    sample.PitchResponseWeight,
                    sample.YawResponseWeight,
                    sample.Priority,
                    sample.Weight,
                    context.Activation.Source.Identity);
                PresentationCameraRequest retirement = PresentationCameraRequest.Response(
                    PresentationCameraRequestLifecycle.Retire,
                    (int)sample.LookResponse,
                    sample.ManualOrbitWeight,
                    sample.PitchResponseWeight,
                    sample.YawResponseWeight,
                    sample.Priority,
                    sample.Weight,
                    context.Activation.Source.Identity);
                AddCamera(key, frame, context, activation, retirement);
            }

            alive.Add(key);
        }

        for (int index = 0; index < frame.Operations.CameraResources.Count; index++)
        {
            TimelineCameraResourceSample sample = frame.Operations.CameraResources[index];
            CameraEventKey key = CameraEventKey.ForClip(frame.Handle.Value, frame.Generation,
                sample.TrackAuthoringId, sample.ClipAuthoringId, 0);
            if (!m_Active.ContainsKey(key))
            {
                string requestId = sample.ClipAuthoringId;
                PresentationCameraRequest activation = PresentationCameraRequest.Effect(
                    PresentationCameraRequestLifecycle.Activate,
                    requestId,
                    (int)RequireResourceEffectKind(sample.Kind),
                    sample.ResourceId,
                    sample.Priority,
                    sample.Weight,
                    context.Activation.Source.Identity);
                PresentationCameraRequest retirement = PresentationCameraRequest.Effect(
                    PresentationCameraRequestLifecycle.Retire,
                    requestId,
                    (int)RequireResourceEffectKind(sample.Kind),
                    sample.ResourceId,
                    sample.Priority,
                    sample.Weight,
                    context.Activation.Source.Identity);
                AddCamera(key, frame, context, activation, retirement);
            }

            alive.Add(key);
        }
    }

    void AddCamera(
        CameraEventKey key,
        TimelineRuntimePresentationFrame frame,
        in TimelinePresentationExecutionContext context,
        PresentationCameraRequest activationRequest,
        PresentationCameraRequest retirementRequest)
    {
        if (m_RequestCapacity == 0)
            throw new InvalidOperationException("Timeline Camera outputs require a composed Camera domain.");
        if (m_Active.Count == m_RequestCapacity)
            throw new InvalidOperationException($"Timeline Camera event capacity {m_RequestCapacity} is exhausted.");
        EventId eventId = new(StableHash.Compute(
            "timeline-presentation-camera",
            frame.ExecutionIdentity.OwnerIdentity,
            key.Track,
            key.Producer,
            key.Cycle.ToString(CultureInfo.InvariantCulture),
            key.Handle.ToString(CultureInfo.InvariantCulture),
            key.Generation.ToString(CultureInfo.InvariantCulture)));
        var activationHeader = new CharacterPresentationEventHeader(
            eventId,
            m_ActorId,
            context.Tick,
            context.Activation,
            frame.PresentationFrame,
            "timeline.camera");
        var retirementHeader = new CharacterPresentationEventHeader(
            eventId,
            m_ActorId,
            context.Tick,
            context.Activation,
            frame.PresentationFrame + 1,
            "timeline.camera");
        string producerId = $"timeline-camera:{key.Handle}:{key.Generation}:{key.Track}:{key.Producer}:{key.Cycle}";
        var activation = new CharacterPresentationCommand(
            activationHeader,
            CharacterPresentationCommandKind.Camera,
            producerId,
            frame.InterpolationAlpha,
            activationRequest.Weight,
            context.Activation.Generation,
            key.Cycle,
            context.ActionInstanceId,
            1f,
            null,
            activationRequest);
        var retirement = new CharacterPresentationCommand(
            retirementHeader,
            CharacterPresentationCommandKind.Camera,
            producerId,
            frame.InterpolationAlpha,
            retirementRequest.Weight,
            context.Activation.Generation,
            key.Cycle,
            context.ActionInstanceId,
            1f,
            null,
            retirementRequest);
        var state = new CameraEventState(key, frame.Handle.Value, activation, retirement, frame.Generation);
        m_Active.Add(key, state);
        m_Runtime.Publish(activation);
    }

    void RetireInactive(HashSet<CameraEventKey> alive, ulong handle, bool withdrawn, ulong generation = 0)
    {
        m_Retired.Clear();
        foreach (KeyValuePair<CameraEventKey, CameraEventState> pair in m_Active)
        {
            if ((handle == 0 || pair.Value.PlaybackHandle == handle) &&
                (generation == 0 || pair.Value.Generation == generation) && !alive.Contains(pair.Key))
                m_Retired.Add(pair.Key);
        }

        for (int index = 0; index < m_Retired.Count; index++)
        {
            CameraEventState state = m_Active[m_Retired[index]];
            m_Runtime.Retire(withdrawn ? state.Activation : state.Retirement);
            m_Active.Remove(m_Retired[index]);
        }
    }

    internal void Reset()
    {
        if (m_Disposed)
            return;
        DiscardFrame();
        m_Alive.Clear();
        RetireInactive(m_Alive, 0, true);
    }

    static string RequireSequenceId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException("Timeline camera state has no SequenceId.");
        return value.Trim();
    }

    static CameraEffectKind RequireCueEffectKind(TimelineCameraCueKind kind)
    {
        return kind switch
        {
            TimelineCameraCueKind.Shake => CameraEffectKind.Shake,
            TimelineCameraCueKind.FovKick => CameraEffectKind.Zoom,
            TimelineCameraCueKind.Recoil => CameraEffectKind.Shake,
            TimelineCameraCueKind.Override => CameraEffectKind.Override,
            TimelineCameraCueKind.Shot => CameraEffectKind.Shot,
            _ => throw new InvalidOperationException(
                $"Timeline camera cue '{kind}' has no formal Camera domain effect mapping.")
        };
    }

    static CameraEffectKind RequireResourceEffectKind(TimelineCameraResourceKind kind)
    {
        return Enum.IsDefined(typeof(CameraEffectKind), (byte)kind)
            ? (CameraEffectKind)kind
            : throw new InvalidOperationException(
                $"Timeline camera resource '{kind}' is not a formal Camera domain effect.");
    }

    public void Dispose()
    {
        if (m_Disposed)
            return;
        Reset();
        m_Disposed = true;
        m_TimelineHost.PresentationFramePrepared -= OnPresentationFrame;
        m_TimelineHost.PresentationPlaybackEndPrepared -= OnPresentationPlaybackEnded;
    }
}
}
