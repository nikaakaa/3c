using System;
using ThirdPersonSimulation.Fixed;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ThirdPersonSimulation;
using UnityEngine;

namespace BTSMTL.Timeline.Runtime
{
    public enum TimelineRuntimeStepDecision : byte
    {
        Commit = 1,
        Discard = 2
    }

    public readonly struct TimelineRuntimeStepContext
    {
        internal TimelineRuntimeStepContext(
            TimelineRuntimePlayback playback,
            TimelineRuntimeAdvanceRequest request,
            TimelineRuntimeAdvanceResult advance)
        {
            Playback = playback;
            Request = request;
            Advance = advance;
        }

        public TimelineRuntimePlayback Playback { get; }
        public TimelineRuntimeAdvanceRequest Request { get; }
        public TimelineRuntimeAdvanceResult Advance { get; }
    }

    public interface ITimelineRuntimeStepConsumer
    {
        TimelineRuntimeStepDecision Consume(TimelineRuntimeStepContext context);
    }

    public static class TimelineRuntimeStepCoordinator
    {
        public static TimelineRuntimeAdvanceResult Step(
            TimelineRuntimePlayback playback,
            TimelineRuntimeAdvanceRequest request,
            ITimelineRuntimeStepConsumer consumer)
        {
            if (playback == null)
                throw new ArgumentNullException(nameof(playback));
            if (consumer == null)
                throw new ArgumentNullException(nameof(consumer));
            TimelineRuntimeAdvanceResult advance = playback.Advance(request);
            var context = new TimelineRuntimeStepContext(playback, request, advance);
            TimelineRuntimeStepDecision decision;
            try
            {
                decision = consumer.Consume(context);
            }
            catch
            {
                playback.Discard(advance);
                if (consumer is ITimelineRuntimeStepCommitConsumer commitConsumer)
                    commitConsumer.Discard(context);
                throw;
            }
            if (decision == TimelineRuntimeStepDecision.Commit)
            {
                playback.Commit(advance);
                if (consumer is ITimelineRuntimeStepCommitConsumer commitConsumer)
                    commitConsumer.Commit(context);
                return advance;
            }
            if (decision == TimelineRuntimeStepDecision.Discard)
            {
                playback.Discard(advance);
                if (consumer is ITimelineRuntimeStepCommitConsumer commitConsumer)
                    commitConsumer.Discard(context);
                return advance;
            }
            playback.Discard(advance);
            if (consumer is ITimelineRuntimeStepCommitConsumer invalidDecisionConsumer)
                invalidDecisionConsumer.Discard(context);
            throw new InvalidOperationException("Timeline Step consumer returned an invalid decision.");
        }
    }
}
