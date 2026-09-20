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
            m_TimelineHost.PresentationFrameProduced += OnPresentationFrame;
            m_TimelineHost.PresentationPlaybackEnded += OnPresentationPlaybackEnded;
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
                ReleaseAll(playback, frame.Handle.Value, logicTick, frame.PresentationFrame, "generation-replaced");
                playback = new PlaybackState(frame.Generation, logicTick, frame.PresentationFrame);
                m_Playbacks[frame.Handle.Value] = playback;
            }
            else
            {
                playback.LogicTick = logicTick;
                playback.PresentationFrame = frame.PresentationFrame;
            }

            CollectSamples(frame.Operations.AnimationContributions);
            ReleaseInactive(playback, frame.Handle.Value, logicTick, frame.PresentationFrame);
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

        void OnPresentationPlaybackEnded(TimelineRuntimePlaybackHandle handle)
        {
            if (!m_Playbacks.TryGetValue(handle.Value, out PlaybackState playback))
                return;
            ReleaseAll(
                playback,
                handle.Value,
                playback.LogicTick,
                playback.PresentationFrame,
                "playback-ended");
            m_Playbacks.Remove(handle.Value);
        }

        void CollectSamples(IReadOnlyList<TimelineAnimationContribution> contributions)
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
            ulong presentationFrame)
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
                    "clip-ended");
                playback.Producers.Remove(producerId);
            }
        }

        void ReleaseAll(
            PlaybackState playback,
            ulong handle,
            ulong logicTick,
            ulong presentationFrame,
            string reason)
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
                    reason);
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
            string reason)
        {
            m_Inbox.Publish(ActionAnimationPlaybackCommand.Release(
                CreateEventId("release", producer, handle, presentationFrame, reason),
                logicTick,
                producer.PlaybackId,
                producer.ActionInstanceId,
                producer.AnimationChannelId,
                producer.ProgramProducerId));
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
            m_TimelineHost.PresentationFrameProduced -= OnPresentationFrame;
            m_TimelineHost.PresentationPlaybackEnded -= OnPresentationPlaybackEnded;
        }
    }

internal sealed class TimelinePresentationEventBridge : IDisposable
{
    sealed class CameraEventState
    {
        internal CameraEventState(
            string key,
            CharacterPresentationCommand activation,
            CharacterPresentationCommand retirement)
        {
            Key = key;
            Activation = activation;
            Retirement = retirement;
        }

        internal string Key { get; }
        internal CharacterPresentationCommand Activation { get; }
        internal CharacterPresentationCommand Retirement { get; }
    }

    readonly CharacterTimelineHost m_TimelineHost;
    readonly ICharacterPresentationDomainRuntime m_Runtime;
    readonly ActorId m_ActorId;
    readonly Dictionary<string, CameraEventState> m_Active = new(StringComparer.Ordinal);
    bool m_Disposed;

    internal TimelinePresentationEventBridge(
        CharacterTimelineHost timelineHost,
        ICharacterPresentationDomainRuntime runtime,
        ActorId actorId)
    {
        m_TimelineHost = timelineHost ?? throw new ArgumentNullException(nameof(timelineHost));
        m_Runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        m_ActorId = actorId;
        m_TimelineHost.PresentationFrameProduced += OnPresentationFrame;
        m_TimelineHost.PresentationPlaybackEnded += OnPresentationPlaybackEnded;
    }

    void OnPresentationFrame(TimelineRuntimePresentationFrame frame)
    {
        if (!m_TimelineHost.TryGetPresentationExecutionContext(
                frame.Handle,
                out TimelinePresentationExecutionContext context))
        {
            throw new InvalidOperationException(
                $"Timeline playback '{frame.Handle.Value}' has no Presentation execution identity.");
        }

        var alive = new HashSet<string>(StringComparer.Ordinal);
        CollectCameraEvents(frame, context, alive);
        RetireInactive(alive, frame.Handle.Value);
    }

    void OnPresentationPlaybackEnded(TimelineRuntimePlaybackHandle handle)
    {
        RetireInactive(new HashSet<string>(StringComparer.Ordinal), handle.Value);
    }

    void CollectCameraEvents(
        TimelineRuntimePresentationFrame frame,
        in TimelinePresentationExecutionContext context,
        HashSet<string> alive)
    {
        for (int index = 0; index < frame.Operations.CameraStates.Count; index++)
        {
            TimelineCameraStateSample sample = frame.Operations.CameraStates[index];
            string key = CreateKey(
                frame,
                "state",
                sample.SourceId,
                sample.TrackName,
                sample.SequenceId,
                sample.Mode.ToString(),
                sample.TargetKey,
                sample.InterruptPolicy.ToString());
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
            string effectKind = RequireCueEffectKind(sample.CueKind).ToString();
            string key = CreateKey(
                frame,
                "cue",
                sample.SourceId,
                sample.TrackName,
                sample.CueId,
                effectKind,
                sample.ResourceId,
                sample.CueType);
            if (!m_Active.ContainsKey(key))
            {
                string requestId = $"{sample.TrackName}/{sample.CueId}";
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
            string key = CreateKey(
                frame,
                "response",
                sample.SourceId,
                sample.TrackName,
                sample.LookResponse.ToString(),
                sample.ManualOrbitWeight.ToString("R", CultureInfo.InvariantCulture),
                sample.PitchResponseWeight.ToString("R", CultureInfo.InvariantCulture),
                sample.YawResponseWeight.ToString("R", CultureInfo.InvariantCulture));
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
            string key = CreateKey(
                frame,
                "resource",
                sample.SourceId,
                sample.TrackAuthoringId,
                sample.ClipAuthoringId,
                sample.Kind.ToString(),
                sample.ResourceId);
            if (!m_Active.ContainsKey(key))
            {
                string requestId = $"{sample.TrackAuthoringId}/{sample.ClipAuthoringId}";
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
        string key,
        TimelineRuntimePresentationFrame frame,
        in TimelinePresentationExecutionContext context,
        PresentationCameraRequest activationRequest,
        PresentationCameraRequest retirementRequest)
    {
        EventId eventId = new(StableHash.Compute(
            "timeline-presentation-camera",
            key,
            frame.Handle.Value.ToString(CultureInfo.InvariantCulture),
            frame.Generation.ToString(CultureInfo.InvariantCulture)));
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
        string producerId = $"timeline-camera:{key}";
        var activation = new CharacterPresentationCommand(
            activationHeader,
            CharacterPresentationCommandKind.Camera,
            producerId,
            frame.InterpolationAlpha,
            activationRequest.Weight,
            context.Activation.Generation,
            0,
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
            0,
            context.ActionInstanceId,
            1f,
            null,
            retirementRequest);
        var state = new CameraEventState(key, activation, retirement);
        m_Active.Add(key, state);
        m_Runtime.Publish(activation);
    }

    void RetireInactive(HashSet<string> alive, ulong handle)
    {
        List<string> retired = new();
        foreach (KeyValuePair<string, CameraEventState> pair in m_Active)
        {
            if (!alive.Contains(pair.Key))
                retired.Add(pair.Key);
        }

        for (int index = 0; index < retired.Count; index++)
        {
            CameraEventState state = m_Active[retired[index]];
            m_Runtime.Retire(state.Retirement);
            m_Active.Remove(retired[index]);
        }
    }

    internal void Reset()
    {
        if (m_Disposed || m_Active.Count == 0)
            return;
        RetireInactive(new HashSet<string>(StringComparer.Ordinal), 0);
    }

    static string CreateKey(
        TimelineRuntimePresentationFrame frame,
        string kind,
        params string[] values)
    {
        var parts = new List<string>(values.Length + 4)
        {
            kind,
            frame.ExecutionIdentity.OwnerIdentity,
            frame.Handle.Value.ToString(CultureInfo.InvariantCulture),
            frame.Generation.ToString(CultureInfo.InvariantCulture)
        };
        parts.AddRange(values);
        return string.Join("|", parts);
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
        m_Disposed = true;
        m_TimelineHost.PresentationFrameProduced -= OnPresentationFrame;
        m_TimelineHost.PresentationPlaybackEnded -= OnPresentationPlaybackEnded;
        Reset();
    }
}
}
