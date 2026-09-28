using System;
using System.Collections.Generic;
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
        readonly struct ProducerState
        {
            internal ProducerState(ulong handle, AnimationPlaybackId playbackId, ulong actionInstanceId,
                AnimationChannelId animationChannelId, string programProducerId)
            {
                Handle = handle;
                PlaybackId = playbackId;
                ActionInstanceId = actionInstanceId;
                AnimationChannelId = animationChannelId;
                ProgramProducerId = programProducerId;
            }

            internal readonly ulong Handle;
            internal readonly AnimationPlaybackId PlaybackId;
            internal readonly ulong ActionInstanceId;
            internal readonly AnimationChannelId AnimationChannelId;
            internal readonly string ProgramProducerId;
        }

        struct PlaybackState
        {
            internal ulong Handle;
            internal ulong Generation;
            internal ulong LogicTick;
            internal ulong PresentationFrame;
        }

        readonly CharacterTimelineHost m_TimelineHost;
        readonly ActionPlaybackCommandInbox m_Inbox;
        readonly PlaybackState[] m_Playbacks;
        readonly ProducerState[] m_Producers;
        readonly PlaybackState[] m_FramePlaybacks;
        readonly ProducerState[] m_FrameProducers;
        readonly Dictionary<AnimationProducerId, TimelineAnimationContribution> m_Samples;
        readonly List<AnimationProducerId> m_SampleOrder;
        ulong m_FramePublicationSequence;
        bool m_FrameActive;
        bool m_Disposed;

        internal TimelineToActionCommandBridge(CharacterTimelineHost timelineHost, ActionPlaybackCommandInbox inbox)
        {
            m_TimelineHost = timelineHost ?? throw new ArgumentNullException(nameof(timelineHost));
            m_Inbox = inbox ?? throw new ArgumentNullException(nameof(inbox));
            int capacity = inbox.Capacity;
            m_Playbacks = new PlaybackState[capacity];
            m_Producers = new ProducerState[capacity];
            m_FramePlaybacks = new PlaybackState[capacity];
            m_FrameProducers = new ProducerState[capacity];
            m_Samples = new Dictionary<AnimationProducerId, TimelineAnimationContribution>(capacity);
            m_SampleOrder = new List<AnimationProducerId>(capacity);
            m_TimelineHost.PresentationFramePrepared += OnPresentationFrame;
            m_TimelineHost.PresentationPlaybackEndPrepared += OnPresentationPlaybackEnded;
        }

        internal void BeginFrame()
        {
            if (m_FrameActive)
                throw new InvalidOperationException("Timeline animation command frame is already open.");
            Array.Copy(m_Playbacks, m_FramePlaybacks, m_Playbacks.Length);
            Array.Copy(m_Producers, m_FrameProducers, m_Producers.Length);
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
            Array.Copy(m_FramePlaybacks, m_Playbacks, m_Playbacks.Length);
            Array.Copy(m_FrameProducers, m_Producers, m_Producers.Length);
            m_FrameActive = false;
            ClearFrameSnapshot();
        }

        void ClearFrameSnapshot()
        {
            Array.Clear(m_FramePlaybacks, 0, m_FramePlaybacks.Length);
            Array.Clear(m_FrameProducers, 0, m_FrameProducers.Length);
        }

        void OnPresentationFrame(TimelineRuntimePresentationFrame frame)
        {
            if (!m_TimelineHost.TryGetPlaybackActionContext(frame.Handle, out TimelinePlaybackActionContext actionContext))
                throw new InvalidOperationException($"Timeline playback '{frame.Handle.Value}' has no Action context.");
            ulong logicTick = frame.LogicTick != 0 ? frame.LogicTick : actionContext.StartLocalLogicTick;
            if (logicTick == 0)
                throw new InvalidOperationException($"Timeline playback '{frame.Handle.Value}' has no Logic tick for presentation output.");
            int playbackIndex = FindPlayback(frame.Handle.Value);
            if (playbackIndex < 0)
            {
                for (int i = 0; i < m_Playbacks.Length; i++)
                    if (m_Playbacks[i].Handle == 0)
                    {
                        playbackIndex = i;
                        break;
                    }
                if (playbackIndex < 0)
                    throw new InvalidOperationException("Timeline animation playback capacity was exceeded.");
            }
            ref PlaybackState playback = ref m_Playbacks[playbackIndex];
            if (playback.Handle != 0 && playback.Generation != frame.Generation)
                ReleaseAll(frame.Handle.Value, logicTick, frame.PresentationFrame, "generation-replaced", true);
            playback = new PlaybackState
            {
                Handle = frame.Handle.Value, Generation = frame.Generation,
                LogicTick = logicTick, PresentationFrame = frame.PresentationFrame
            };

            CollectSamples(frame.Operations.AnimationContributions);
            for (int i = 0; i < m_Producers.Length; i++)
            {
                ProducerState producer = m_Producers[i];
                if (producer.Handle != frame.Handle.Value || m_Samples.ContainsKey(producer.PlaybackId.ProducerId))
                    continue;
                PublishRelease(producer, logicTick, frame.PresentationFrame, "clip-ended",
                    frame.Reason == TimelinePresentationSampleReason.Correction);
                m_Producers[i] = default;
            }
            for (int index = 0; index < m_SampleOrder.Count; index++)
            {
                AnimationProducerId producerId = m_SampleOrder[index];
                TimelineAnimationContribution contribution = m_Samples[producerId];
                int producerIndex = FindProducer(frame.Handle.Value, producerId);
                if (producerIndex < 0)
                {
                    for (int i = 0; i < m_Producers.Length; i++)
                        if (m_Producers[i].Handle == 0)
                        {
                            producerIndex = i;
                            break;
                        }
                    if (producerIndex < 0)
                        throw new InvalidOperationException("Timeline animation producer capacity was exceeded.");
                    m_Producers[producerIndex] = new ProducerState(frame.Handle.Value,
                        new AnimationPlaybackId(producerId, frame.Generation), actionContext.ActionInstanceId,
                        contribution.AnimationChannelId, m_TimelineHost.Content.RequireAnimationProducerIdentity(producerId));
                    PublishSelect(m_Producers[producerIndex], logicTick, frame.PresentationFrame);
                }
                ProducerState producer = m_Producers[producerIndex];
                if (producer.ActionInstanceId != actionContext.ActionInstanceId ||
                    !producer.AnimationChannelId.Equals(contribution.AnimationChannelId))
                    throw new InvalidOperationException($"Timeline Animation producer '{producerId}' changed ownership within one playback generation.");
                PublishSample(producer, contribution, logicTick, frame.PresentationFrame);
            }
        }

        int FindPlayback(ulong handle)
        {
            for (int i = 0; i < m_Playbacks.Length; i++)
                if (m_Playbacks[i].Handle == handle)
                    return i;
            return -1;
        }

        int FindProducer(ulong handle, AnimationProducerId producerId)
        {
            for (int i = 0; i < m_Producers.Length; i++)
                if (m_Producers[i].Handle == handle && m_Producers[i].PlaybackId.ProducerId.Equals(producerId))
                    return i;
            return -1;
        }

        void OnPresentationPlaybackEnded(TimelineRuntimePlaybackHandle handle, ulong generation,
            TimelinePresentationSampleReason reason, bool retainForCorrection)
        {
            int index = FindPlayback(handle.Value);
            if (index < 0 || m_Playbacks[index].Generation != generation)
                return;
            PlaybackState playback = m_Playbacks[index];
            ReleaseAll(handle.Value, playback.LogicTick, playback.PresentationFrame, "playback-ended",
                reason == TimelinePresentationSampleReason.Withdrawn);
            m_Playbacks[index] = default;
        }

        void CollectSamples(TimelineRuntimeSampleView<TimelineAnimationContribution> contributions)
        {
            m_Samples.Clear();
            m_SampleOrder.Clear();
            for (int index = 0; index < contributions.Count; index++)
            {
                TimelineAnimationContribution contribution = contributions[index];
                var producerId = new AnimationProducerId(contribution.TimelineAuthoringId, contribution.TrackAuthoringId);
                if (!producerId.IsValid || !contribution.AnimationChannelId.IsValid)
                    throw new InvalidOperationException("Timeline Animation contribution has no stable producer identity.");
                if (!m_Samples.TryGetValue(producerId, out TimelineAnimationContribution current))
                {
                    if (m_SampleOrder.Count == m_Producers.Length)
                        throw new InvalidOperationException("Timeline animation sample capacity was exceeded.");
                    m_Samples.Add(producerId, contribution);
                    m_SampleOrder.Add(producerId);
                }
                else if (contribution.Weight > current.Weight || contribution.Weight == current.Weight &&
                    string.CompareOrdinal(contribution.ClipAuthoringId, current.ClipAuthoringId) < 0)
                    m_Samples[producerId] = contribution;
            }
        }

        void ReleaseAll(ulong handle, ulong logicTick, ulong presentationFrame, string reason, bool withdrawn = false)
        {
            for (int i = 0; i < m_Producers.Length; i++)
            {
                if (m_Producers[i].Handle != handle)
                    continue;
                PublishRelease(m_Producers[i], logicTick, presentationFrame, reason, withdrawn);
                m_Producers[i] = default;
            }
        }

        void PublishSelect(ProducerState producer, ulong logicTick, ulong presentationFrame)
        {
            m_Inbox.Publish(ActionAnimationPlaybackCommand.Select(
                CreateEventId("select", producer, presentationFrame, string.Empty), logicTick,
                producer.PlaybackId, producer.ActionInstanceId, producer.AnimationChannelId, producer.ProgramProducerId));
        }

        void PublishSample(ProducerState producer, TimelineAnimationContribution contribution, ulong logicTick, ulong presentationFrame)
        {
            var sample = new ActionProjectedSample(logicTick, presentationFrame,
                contribution.Clip,
                new PresentationPoseSampleTime(contribution.ClipTime, contribution.ContinuousClipTime,
                    contribution.Cycle, contribution.IsLooping, 1f), contribution.Weight);
            m_Inbox.Publish(ActionAnimationPlaybackCommand.PresentSample(producer.PlaybackId,
                producer.ActionInstanceId, producer.AnimationChannelId, producer.ProgramProducerId, in sample));
        }

        void PublishRelease(ProducerState producer, ulong logicTick, ulong presentationFrame, string reason, bool withdrawn)
        {
            EventId eventId = CreateEventId(withdrawn ? "withdraw" : "release", producer, presentationFrame, reason);
            m_Inbox.Publish(withdrawn
                ? ActionAnimationPlaybackCommand.Withdraw(eventId, logicTick, producer.PlaybackId,
                    producer.ActionInstanceId, producer.AnimationChannelId, producer.ProgramProducerId)
                : ActionAnimationPlaybackCommand.Release(eventId, logicTick, producer.PlaybackId,
                    producer.ActionInstanceId, producer.AnimationChannelId, producer.ProgramProducerId));
        }

        static EventId CreateEventId(string kind, ProducerState producer, ulong presentationFrame, string detail)
        {
            Span<byte> block = stackalloc byte[64];
            var builder = new EventIdBuilder(block);
            builder.Append("timeline-presentation-animation");
            builder.Append(kind);
            builder.Append(producer.Handle);
            builder.Append(producer.PlaybackId.Generation);
            builder.Append(producer.PlaybackId.ProducerId.TimelineAuthoringId);
            builder.Append(producer.PlaybackId.ProducerId.TrackAuthoringId);
            builder.Append(presentationFrame);
            builder.Append(detail);
            return builder.Build();
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            for (int i = 0; i < m_Playbacks.Length; i++)
            {
                PlaybackState playback = m_Playbacks[i];
                if (playback.Handle != 0)
                    ReleaseAll(playback.Handle, playback.LogicTick, playback.PresentationFrame, "bridge-disposed");
            }
            Array.Clear(m_Playbacks, 0, m_Playbacks.Length);
            m_TimelineHost.PresentationFramePrepared -= OnPresentationFrame;
            m_TimelineHost.PresentationPlaybackEndPrepared -= OnPresentationPlaybackEnded;
        }
    }

    internal sealed class TimelinePresentationEventBridge : IDisposable
    {
        readonly struct CameraEventKey : IEquatable<CameraEventKey>
        {
            CameraEventKey(ulong handle, ulong generation, string marker, string track, string producer, int cycle,
                string treeGraphId, string treeGraphRevision, string nodeAuthoringId, ulong branchRevision)
            {
                Handle = handle;
                Generation = generation;
                Marker = marker;
                Track = track;
                Producer = producer;
                Cycle = cycle;
                TreeGraphId = treeGraphId;
                TreeGraphRevision = treeGraphRevision;
                NodeAuthoringId = nodeAuthoringId;
                BranchRevision = branchRevision;
            }

        internal static CameraEventKey ForMarker(ulong handle, ulong generation, string marker, string producer, int cycle) =>
            new(handle, generation, marker, string.Empty, producer, cycle, null, null, null, 0);
        internal static CameraEventKey ForClip(ulong handle, ulong generation, string track, string clip, int cycle) =>
            new(handle, generation, null, track, clip, cycle, null, null, null, 0);
        internal static CameraEventKey ForGraphMarker(ulong handle, ulong generation, string marker, string producer, int cycle,
            in Float32PresentationGraphOutputIdentity identity) =>
            new(handle, generation, marker, null, producer, cycle,
                identity.TreeGraphId, identity.TreeGraphRevision, identity.NodeAuthoringId, identity.BranchRevision);
        internal static CameraEventKey ForGraphClip(ulong handle, ulong generation, string track, string producer, int cycle,
            in Float32PresentationGraphOutputIdentity identity) =>
            new(handle, generation, null, track, producer, cycle,
                identity.TreeGraphId, identity.TreeGraphRevision, identity.NodeAuthoringId, identity.BranchRevision);
        internal readonly ulong Handle;
        internal readonly ulong Generation;
        internal readonly string Marker;
        internal readonly string Track;
        internal readonly string Producer;
        internal readonly int Cycle;
        internal readonly string TreeGraphId;
        internal readonly string TreeGraphRevision;
        internal readonly string NodeAuthoringId;
        internal readonly ulong BranchRevision;
        public bool Equals(CameraEventKey other) => Handle == other.Handle && Generation == other.Generation && Cycle == other.Cycle &&
            string.Equals(Marker, other.Marker, StringComparison.Ordinal) && string.Equals(Track, other.Track, StringComparison.Ordinal) &&
            string.Equals(Producer, other.Producer, StringComparison.Ordinal) &&
            string.Equals(TreeGraphId, other.TreeGraphId, StringComparison.Ordinal) &&
            string.Equals(TreeGraphRevision, other.TreeGraphRevision, StringComparison.Ordinal) &&
            string.Equals(NodeAuthoringId, other.NodeAuthoringId, StringComparison.Ordinal) &&
            BranchRevision == other.BranchRevision;
        public override bool Equals(object obj) => obj is CameraEventKey other && Equals(other);
        public override int GetHashCode() => unchecked((((Handle.GetHashCode() * 397 ^ Generation.GetHashCode()) * 397 ^ Cycle) * 397 ^
            (Marker == null ? 0 : StringComparer.Ordinal.GetHashCode(Marker))) * 397 ^
            (Track == null ? 0 : StringComparer.Ordinal.GetHashCode(Track)) ^
            (Producer == null ? 0 : StringComparer.Ordinal.GetHashCode(Producer)) ^
            (TreeGraphId == null ? 0 : StringComparer.Ordinal.GetHashCode(TreeGraphId)) ^
            (TreeGraphRevision == null ? 0 : StringComparer.Ordinal.GetHashCode(TreeGraphRevision)) ^
            (NodeAuthoringId == null ? 0 : StringComparer.Ordinal.GetHashCode(NodeAuthoringId)) ^
            BranchRevision.GetHashCode());
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
            int markerCycle = 0,
            bool suspended = false)
        {
            Key = key;
            PlaybackHandle = playbackHandle;
            Generation = generation;
            Activation = activation;
            Retirement = retirement;
            MarkerTime = markerTime;
            MarkerCycle = markerCycle;
            Suspended = suspended;
        }

        internal bool ResourceTimed => Activation.CameraRequest.Kind == PresentationCameraRequestKind.Effect &&
            Activation.CameraRequest.EffectKind == (int)CameraEffectKind.Shake;
        internal bool Suspended { get; }
        internal CameraEventState WithSuspended(bool suspended) =>
            new CameraEventState(Key, PlaybackHandle, Activation, Retirement, Generation, MarkerTime, MarkerCycle, suspended);
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
    readonly Dictionary<CameraEventKey, CameraEventState> m_Events;
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
        m_Events = new Dictionary<CameraEventKey, CameraEventState>(requestCapacity);
        m_FrameBaseline = new Dictionary<CameraEventKey, CameraEventState>(requestCapacity);
        m_Alive = new HashSet<CameraEventKey>(requestCapacity);
        m_Retired = new List<CameraEventKey>(requestCapacity);
        m_TimelineHost.PresentationFramePrepared += OnPresentationFrame;
        m_TimelineHost.PresentationGraphCameraPrepared += OnGraphCamera;
        m_TimelineHost.PresentationGraphFramePreparing += OnGraphFramePreparing;
        m_TimelineHost.PresentationPlaybackEndPrepared += OnPresentationPlaybackEnded;
    }

    internal void BeginFrame()
    {
        if (m_FrameOpen)
            throw new InvalidOperationException("Timeline Camera event candidate is already open.");
        m_FrameBaseline.Clear();
        foreach (KeyValuePair<CameraEventKey, CameraEventState> entry in m_Events)
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
        m_Events.Clear();
        foreach (KeyValuePair<CameraEventKey, CameraEventState> entry in m_FrameBaseline)
            m_Events.Add(entry.Key, entry.Value);
        m_FrameBaseline.Clear();
        m_FrameOpen = false;
    }

    void OnPresentationFrame(TimelineRuntimePresentationFrame frame)
    {
        m_Alive.Clear();
        if (frame.Reason == TimelinePresentationSampleReason.Stopped || frame.Reason == TimelinePresentationSampleReason.Withdrawn)
        {
            RetireInactive(m_Alive, frame.Handle.Value, frame.Reason == TimelinePresentationSampleReason.Withdrawn,
                frame.Generation, true);
            return;
        }
        if (frame.Operations.CameraStates.Count != 0 ||
            frame.Operations.CameraResponses.Count != 0 || frame.Operations.CameraResources.Count != 0)
        {
            if (!m_TimelineHost.TryGetPresentationExecutionContext(frame.Handle, out TimelinePresentationExecutionContext context))
                throw new InvalidOperationException($"Timeline playback '{frame.Handle.Value}' has no Presentation execution identity.");
            CollectCameraEvents(frame, context, m_Alive);
        }
        foreach (KeyValuePair<CameraEventKey, CameraEventState> entry in m_Events)
            if (entry.Key.Marker != null && entry.Key.Handle == frame.Handle.Value && entry.Key.Generation == frame.Generation &&
                (entry.Value.MarkerCycle < frame.Cycle || entry.Value.MarkerCycle == frame.Cycle && entry.Value.MarkerTime <= frame.Time.Raw))
                m_Alive.Add(entry.Key);
        foreach (KeyValuePair<CameraEventKey, CameraEventState> entry in m_Events)
        {
            if (entry.Key.Marker != null || entry.Key.Handle != frame.Handle.Value || entry.Key.Generation != frame.Generation)
                continue;
            if (entry.Value.ResourceTimed && entry.Key.Cycle == frame.Cycle && entry.Value.MarkerTime <= frame.Time.Raw)
            {
                m_Alive.Add(entry.Key);
                continue;
            }
            for (int index = 0; index < frame.Operations.ActiveTreeClips.Count; index++)
            {
                TimelineRuntimeTreeClipRequest tree = frame.Operations.ActiveTreeClips[index];
                if (entry.Key.Track == tree.ClipAuthoringId && entry.Key.Cycle == tree.Cycle)
                    m_Alive.Add(entry.Key);
            }
        }
        foreach (CameraEventKey key in m_Alive)
        {
            CameraEventState state = m_Events[key];
            if (!state.Suspended)
                continue;
            m_Runtime.Publish(state.Activation);
            m_Events[key] = state.WithSuspended(false);
        }
        RetireInactive(m_Alive, frame.Handle.Value, frame.Reason == TimelinePresentationSampleReason.Correction,
            retainForCorrection: true);
    }

    void OnGraphFramePreparing(TimelineRuntimePresentationFrame frame)
    {
        m_Retired.Clear();
        foreach (KeyValuePair<CameraEventKey, CameraEventState> entry in m_Events)
        {
            if (entry.Key.Marker != null || entry.Key.Handle != frame.Handle.Value || entry.Key.Generation != frame.Generation)
                continue;
            if (entry.Value.ResourceTimed)
                continue;
            for (int index = 0; index < frame.Operations.TreeClips.Count; index++)
            {
                TimelineRuntimeTreeClipRequest tree = frame.Operations.TreeClips[index];
                if ((tree.EventKind == TimelineRuntimeTreeClipEventKind.Exit || tree.EventKind == TimelineRuntimeTreeClipEventKind.Destroy) &&
                    entry.Key.Track == tree.ClipAuthoringId && entry.Key.Cycle == tree.Cycle)
                {
                    m_Runtime.Retire(tree.EventKind == TimelineRuntimeTreeClipEventKind.Destroy ? entry.Value.Activation : entry.Value.Retirement);
                    m_Retired.Add(entry.Key);
                    break;
                }
            }
        }
        for (int index = 0; index < m_Retired.Count; index++)
            m_Events.Remove(m_Retired[index]);
    }

    void OnGraphCamera(TimelinePresentationGraphCameraOutput output)
    {
        TimelineRuntimePresentationFrame frame = output.Frame;
        CameraEventKey key = output.Marker
            ? CameraEventKey.ForGraphMarker(frame.Handle.Value, frame.Generation, output.CallerId, output.Producer,
                output.Cycle, output.Identity)
            : CameraEventKey.ForGraphClip(frame.Handle.Value, frame.Generation, output.CallerId, output.Producer,
                output.Cycle, output.Identity);
        if (output.Retiring)
        {
            if (m_Events.TryGetValue(key, out CameraEventState previous))
            {
                m_Runtime.Retire(previous.Retirement);
                m_Events.Remove(key);
            }
            return;
        }
        if (!m_TimelineHost.TryGetPresentationExecutionContext(frame.Handle, out TimelinePresentationExecutionContext context))
            throw new InvalidOperationException("Presentation graph Camera output has no execution identity.");
        PublishCamera(key, frame, context, output.Activation, output.Retirement, output.Producer, output.Time,
            output.Cycle, output.Identity);
    }

    void OnPresentationPlaybackEnded(TimelineRuntimePlaybackHandle handle, ulong generation, TimelinePresentationSampleReason reason, bool retainForCorrection)
    {
        m_Alive.Clear();
        RetireInactive(m_Alive, handle.Value, reason == TimelinePresentationSampleReason.Withdrawn, generation, retainForCorrection);
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
            PublishCamera(key, frame, context, activation, retirement);

            alive.Add(key);
        }

        for (int index = 0; index < frame.Operations.CameraResponses.Count; index++)
        {
            TimelineCameraResponseSample sample = frame.Operations.CameraResponses[index];
            CameraEventKey key = CameraEventKey.ForClip(frame.Handle.Value, frame.Generation,
                sample.TrackAuthoringId, sample.ClipAuthoringId, 0);
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
            PublishCamera(key, frame, context, activation, retirement);

            alive.Add(key);
        }

        for (int index = 0; index < frame.Operations.CameraResources.Count; index++)
        {
            TimelineCameraResourceSample sample = frame.Operations.CameraResources[index];
            CameraEventKey key = CameraEventKey.ForClip(frame.Handle.Value, frame.Generation,
                sample.TrackAuthoringId, sample.ClipAuthoringId, 0);
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
            PublishCamera(key, frame, context, activation, retirement);

            alive.Add(key);
        }
    }

    void PublishCamera(
        CameraEventKey key,
        TimelineRuntimePresentationFrame frame,
        in TimelinePresentationExecutionContext context,
        PresentationCameraRequest activationRequest,
        PresentationCameraRequest retirementRequest,
        string graphProducer = null, long markerTime = 0, int markerCycle = 0,
        in Float32PresentationGraphOutputIdentity graphIdentity = default)
    {
        if (m_RequestCapacity == 0)
            throw new InvalidOperationException("Timeline Camera outputs require a composed Camera domain.");
        bool existing = m_Events.TryGetValue(key, out CameraEventState previous);
        if (!existing && m_Events.Count == m_RequestCapacity)
            throw new InvalidOperationException($"Timeline Camera event capacity {m_RequestCapacity} is exhausted.");
        EventId eventId;
        if (existing)
            eventId = previous.Activation.Header.EventId;
        else if (graphIdentity.NodeAuthoringId != null)
            eventId = graphIdentity.EventId;
        else
        {
            Span<byte> block = stackalloc byte[64];
            var builder = new EventIdBuilder(block);
            builder.Append("timeline-presentation-camera");
            builder.Append(frame.ExecutionIdentity.OwnerIdentity);
            builder.Append(key.Track);
            builder.Append(key.Marker ?? string.Empty);
            builder.Append(key.Producer);
            builder.Append(checked((ulong)key.Cycle));
            builder.Append(key.Handle);
            builder.Append(key.Generation);
            eventId = builder.Build();
        }
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
        string producerId = existing ? previous.Activation.ProducerId
            : graphProducer ?? $"timeline-camera:{key.Handle}:{key.Generation}:{key.Track}:{key.Producer}:{key.Cycle}";
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
        var state = new CameraEventState(key, frame.Handle.Value, activation, retirement, frame.Generation, markerTime, markerCycle);
        m_Events[key] = state;
        m_Runtime.Publish(activation);
    }

    void RetireInactive(HashSet<CameraEventKey> alive, ulong handle, bool withdrawn, ulong generation = 0,
        bool retainForCorrection = false)
    {
        m_Retired.Clear();
        foreach (KeyValuePair<CameraEventKey, CameraEventState> pair in m_Events)
        {
            if ((handle == 0 || pair.Value.PlaybackHandle == handle) &&
                (generation == 0 || pair.Value.Generation == generation) && !alive.Contains(pair.Key))
                m_Retired.Add(pair.Key);
        }

        for (int index = 0; index < m_Retired.Count; index++)
        {
            CameraEventState state = m_Events[m_Retired[index]];
            if (!state.Suspended && (withdrawn || !state.ResourceTimed))
                m_Runtime.Retire(withdrawn ? state.Activation : state.Retirement);
            if (retainForCorrection && state.Key.Marker != null &&
                state.Activation.CameraRequest.Kind != PresentationCameraRequestKind.Effect)
                m_Events[state.Key] = state.WithSuspended(true);
            else
                m_Events.Remove(state.Key);
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

    static CameraEffectKind RequireResourceEffectKind(TimelineCameraResourceKind kind)
    {
        return kind switch
        {
            TimelineCameraResourceKind.Override => CameraEffectKind.Override,
            TimelineCameraResourceKind.Zoom => CameraEffectKind.Zoom,
            TimelineCameraResourceKind.Stretch => CameraEffectKind.Stretch,
            TimelineCameraResourceKind.Shot => CameraEffectKind.Shot,
            _ => throw new InvalidOperationException(
                $"Timeline camera resource '{kind}' is not a formal Camera domain effect.")
        };
    }

    public void Dispose()
    {
        if (m_Disposed)
            return;
        Reset();
        m_Disposed = true;
        m_TimelineHost.PresentationFramePrepared -= OnPresentationFrame;
        m_TimelineHost.PresentationGraphCameraPrepared -= OnGraphCamera;
        m_TimelineHost.PresentationGraphFramePreparing -= OnGraphFramePreparing;
        m_TimelineHost.PresentationPlaybackEndPrepared -= OnPresentationPlaybackEnded;
    }
}
}
