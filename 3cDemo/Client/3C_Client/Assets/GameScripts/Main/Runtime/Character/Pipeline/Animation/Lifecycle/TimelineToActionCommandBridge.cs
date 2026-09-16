using System;
using BTSMTL.Timeline;
using BTSMTL.Timeline.Runtime;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Animation.Lifecycle
{
    internal sealed class TimelineToActionCommandBridge : IDisposable
    {
        readonly TimelineRuntimeCompositionHost m_TimelineHost;
        readonly ActionPlaybackCommandInbox m_Inbox;
        bool m_Disposed;

        internal TimelineToActionCommandBridge(
            TimelineRuntimeCompositionHost timelineHost,
            ActionPlaybackCommandInbox inbox)
        {
            m_TimelineHost = timelineHost ?? throw new ArgumentNullException(nameof(timelineHost));
            m_Inbox = inbox ?? throw new ArgumentNullException(nameof(inbox));
            m_TimelineHost.CommittedEvaluation += OnCommittedEvaluation;
        }

        void OnCommittedEvaluation(TimelineRuntimeCommittedEvaluation evaluation)
        {
            TimelineRuntimeEvaluationResult result = evaluation.Evaluation;
            if (result == null || result.AnimationContributions.Count == 0)
                return;
            for (int i = 0; i < result.AnimationContributions.Count; i++)
            {
                var contribution = result.AnimationContributions[i];
                var command = CreateSampleCommand(contribution, evaluation);
                if (command.IsValid)
                    m_Inbox.Publish(command);
            }
        }

        static ActionAnimationPlaybackCommand CreateSampleCommand(
            TimelineAnimationContribution contribution,
            TimelineRuntimeCommittedEvaluation evaluation)
        {
            var producerId = new AnimationProducerId(
                contribution.TimelineAuthoringId,
                contribution.TrackAuthoringId);
            var playbackId = new AnimationPlaybackId(producerId, evaluation.Generation);
            var rawSample = new ActionCommittedRawSample(
                new EventId(StableHash.Compute(
                    contribution.TimelineAuthoringId,
                    contribution.ClipAuthoringId,
                    evaluation.Handle.Value.ToString(System.Globalization.CultureInfo.InvariantCulture))),
                evaluation.LogicTick,
                evaluation.Generation,
                contribution.ClipTime,
                contribution.ContinuousClipTime,
                evaluation.Cycle,
                contribution.IsLooping,
                1f,
                contribution.Weight);
            return ActionAnimationPlaybackCommand.Sample(
                playbackId,
                evaluation.Handle.Value,
                contribution.AnimationChannelId,
                producerId.ProgramProducerIdentity,
                rawSample);
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            m_TimelineHost.CommittedEvaluation -= OnCommittedEvaluation;
        }
    }
}
