using System;
using System.Collections.Generic;
using System.Globalization;
using BTSMTL.Timeline;
using BTSMTL.Timeline.Runtime;
using ThirdPersonSimulation;

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
            var sample = new ActionCommittedRawSample(
                eventId,
                logicTick,
                presentationFrame,
                contribution.ClipTime,
                contribution.ContinuousClipTime,
                contribution.Cycle,
                contribution.IsLooping,
                1f,
                contribution.Weight);
            m_Inbox.Publish(ActionAnimationPlaybackCommand.Sample(
                producer.PlaybackId,
                producer.ActionInstanceId,
                producer.AnimationChannelId,
                producer.ProgramProducerId,
                sample));
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
}
