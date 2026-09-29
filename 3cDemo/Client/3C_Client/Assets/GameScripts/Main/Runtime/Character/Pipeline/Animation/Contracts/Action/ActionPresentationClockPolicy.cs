using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Animation.Lifecycle;
using ThirdPersonSimulation;
using ThirdPersonSimulation.Fixed;
using BTSMTL.Timeline;
using BTSMTL.Timeline.Runtime;
using ThirdPersonCharacter.Pipeline.Presentation;

namespace ThirdPersonCharacter.Pipeline.Animation.Presentation
{
    internal interface IActionPresentationClockPolicy
    {
        void DriveClock(
            AnimationClipPlayerRuntime player,
            AnimationChannelId channelId,
            double presentationSampleTick,
            in CharacterPresentationFactFrame factFrame,
            float presentationDeltaSeconds);
    }

    internal interface IActionPresentationClockCoordinator : IDisposable
    {
        ulong ConfirmedTimelineTick { get; }
        void ConfirmTimelineHistory(ulong confirmedTick);
        void AcceptTimelineProgress(in CharacterPresentationCommand command);
        void AcceptTimelinePresentationFrame(in TimelineRuntimePresentationFrame frame);
        void RetireTimelineProgress(in CharacterPresentationCommand command);
        void ReleaseTimeline(ulong actionInstanceId, int operationIndex, string invocationPath, string timelineId, ulong generation);
        bool TrySampleTimeline(ulong actionInstanceId, int operationIndex, string invocationPath, string timelineId, ulong generation,
            ulong presentationFrame, ulong localLogicTick, float interpolationAlpha, out TimelineRuntimePresentationSample sample);
        void BeginSamplingFrame();
        void CommitSamplingFrame();
        void DiscardSamplingFrame();
        void BeginFrame(IReadOnlyList<ActionAnimationPlaybackCommand> commands);
        void ValidateFrame();
        void CommitFrame();
        void DiscardFrame();
        void Reset();
        IActionPresentationClockPolicy CreatePolicy();
    }

    internal sealed class FreeRunPresentationClockPolicy : IActionPresentationClockPolicy
    {
        public static FreeRunPresentationClockPolicy Shared { get; } =
            new FreeRunPresentationClockPolicy();

        public void DriveClock(
            AnimationClipPlayerRuntime player,
            AnimationChannelId channelId,
            double presentationSampleTick,
            in CharacterPresentationFactFrame factFrame,
            float presentationDeltaSeconds)
        {
            player.Advance(presentationDeltaSeconds, player.PlayRate);
        }
    }

    internal sealed class CommittedMovementPresentationClockPolicy : IActionPresentationClockPolicy
    {
        public void DriveClock(
            AnimationClipPlayerRuntime player,
            AnimationChannelId channelId,
            double presentationSampleTick,
            in CharacterPresentationFactFrame factFrame,
            float presentationDeltaSeconds)
        {
            player.SynchronizeMovementClock(
                factFrame.MovementPlaybackTime,
                factFrame.MovementPlaybackClock,
                presentationDeltaSeconds,
                player.PlayRate);
        }
    }

    internal sealed class CommittedFollowPresentationClockPolicy : IActionPresentationClockPolicy
    {
        readonly CommittedFollowPresentationClockCoordinator m_Coordinator;

        internal CommittedFollowPresentationClockPolicy(
            CommittedFollowPresentationClockCoordinator coordinator)
        {
            m_Coordinator = coordinator ??
                throw new ArgumentNullException(nameof(coordinator));
        }

        public void DriveClock(
            AnimationClipPlayerRuntime player,
            AnimationChannelId channelId,
            double presentationSampleTick,
            in CharacterPresentationFactFrame factFrame,
            float presentationDeltaSeconds)
        {
            ProjectedActionPresentationSample sample = m_Coordinator.ProjectSample(
                channelId,
                presentationSampleTick,
                presentationDeltaSeconds,
                player.Duration);
            player.SetRawClock(sample.ProjectedRawSample.ContinuousTime);
        }
    }

    internal sealed class CommittedFollowPresentationClockCoordinator : IActionPresentationClockCoordinator
    {
        struct TimelineProgressEntry
        {
            internal CharacterPresentationCommand Command;
            internal bool Occupied;
            internal bool Corrected;
            internal bool Withdrawn;
            internal ulong PresentationFrame;
            internal TimelineRuntimePresentationSample Sample;
        }

        struct TimelineAnimationFrameEntry
        {
            internal bool Occupied;
            internal TimelineRuntimePlaybackHandle Handle;
            internal ulong Generation;
            internal ulong PresentationFrame;
            internal TimelineRuntimeSampleView<TimelineAnimationContribution> Contributions;
        }

        readonly TimelineProgressEntry[] m_TimelineProgress = new TimelineProgressEntry[64];
        readonly TimelineProgressEntry[] m_TimelineFrameBaseline = new TimelineProgressEntry[64];
        readonly TimelineAnimationFrameEntry[] m_TimelineAnimationFrames = new TimelineAnimationFrameEntry[64];
        bool m_SamplingFrameActive;
        ulong m_ConfirmedTimelineTick;
        public ulong ConfirmedTimelineTick => m_ConfirmedTimelineTick;

        public void BeginSamplingFrame()
        {
            RequireAlive();
            if (m_SamplingFrameActive)
                throw new InvalidOperationException("Action sampling frame is already open.");
            Array.Copy(m_TimelineProgress, m_TimelineFrameBaseline, m_TimelineProgress.Length);
            Array.Clear(m_TimelineAnimationFrames, 0, m_TimelineAnimationFrames.Length);
            m_SamplingFrameActive = true;
        }

        public void CommitSamplingFrame()
        {
            m_SamplingFrameActive = false;
            Array.Clear(m_TimelineFrameBaseline, 0, m_TimelineFrameBaseline.Length);
            Array.Clear(m_TimelineAnimationFrames, 0, m_TimelineAnimationFrames.Length);
        }

        public void DiscardSamplingFrame()
        {
            if (!m_SamplingFrameActive)
                return;
            Array.Copy(m_TimelineFrameBaseline, m_TimelineProgress, m_TimelineProgress.Length);
            Array.Clear(m_TimelineFrameBaseline, 0, m_TimelineFrameBaseline.Length);
            Array.Clear(m_TimelineAnimationFrames, 0, m_TimelineAnimationFrames.Length);
            m_SamplingFrameActive = false;
        }

        public void ConfirmTimelineHistory(ulong confirmedTick)
        {
            RequireAlive();
            if (m_SamplingFrameActive || m_ProjectorActive || confirmedTick < m_ConfirmedTimelineTick)
                throw new InvalidOperationException("Timeline confirmation must advance outside a presentation frame.");
            if (confirmedTick == m_ConfirmedTimelineTick)
                return;
            m_ConfirmedTimelineTick = confirmedTick;
            for (int index = 0; index < m_TimelineProgress.Length; index++)
                if (m_TimelineProgress[index].Occupied && m_TimelineProgress[index].Withdrawn)
                    m_TimelineProgress[index].PresentationFrame = 0;
        }

        public void AcceptTimelineProgress(in CharacterPresentationCommand command)
        {
            RequireAlive();
            if (command.Kind != CharacterPresentationCommandKind.TimelineProgress || !command.TimelineProgress.IsValid)
                throw new ArgumentException("Action clock requires a typed Timeline progress command.", nameof(command));
            if (m_ProjectorActive || m_SamplingFrameActive)
                throw new InvalidOperationException("Timeline progress cannot change inside an open presentation frame.");
            int slot = -1;
            for (int i = 0; i < m_TimelineProgress.Length; i++)
            {
                ref TimelineProgressEntry entry = ref m_TimelineProgress[i];
                if (!entry.Occupied)
                {
                    if (slot < 0) slot = i;
                    continue;
                }
                if (entry.Command.SourceActionInstanceId != command.SourceActionInstanceId ||
                    !entry.Command.Header.Activation.Source.Equals(command.Header.Activation.Source) ||
                    !string.Equals(entry.Command.TimelineProgress.TimelineId, command.TimelineProgress.TimelineId, StringComparison.Ordinal))
                    continue;
                if (entry.Command.TimelineProgress.Generation != command.TimelineProgress.Generation)
                {
                    entry.Withdrawn = true;
                    entry.PresentationFrame = 0;
                    continue;
                }
                if (!entry.Withdrawn && entry.Command.Header.EventId.Equals(command.Header.EventId) && entry.Command.TimelineProgress.Equals(command.TimelineProgress))
                    return;
                entry.Corrected = entry.Withdrawn ||
                    command.Header.Tick.Value <= entry.Command.Header.Tick.Value;
                entry.Withdrawn = false;
                entry.Command = command;
                entry.PresentationFrame = 0;
                return;
            }
            if (slot < 0)
                throw new InvalidOperationException("Action Timeline progress capacity was exceeded.");
            m_TimelineProgress[slot] = new TimelineProgressEntry { Occupied = true, Command = command };
        }

        public void AcceptTimelinePresentationFrame(in TimelineRuntimePresentationFrame frame)
        {
            RequireAlive();
            if (!m_SamplingFrameActive || !frame.Handle.IsValid || frame.Generation == 0 ||
                frame.PresentationFrame == 0)
                throw new InvalidOperationException("Timeline presentation frame requires an open sampling frame.");
            for (int index = 0; index < m_TimelineAnimationFrames.Length; index++)
            {
                ref TimelineAnimationFrameEntry entry = ref m_TimelineAnimationFrames[index];
                if (!entry.Occupied || entry.Handle != frame.Handle || entry.Generation != frame.Generation)
                    continue;
                entry.PresentationFrame = frame.PresentationFrame;
                entry.Contributions = frame.Operations.AnimationContributions;
                return;
            }
            for (int index = 0; index < m_TimelineAnimationFrames.Length; index++)
            {
                ref TimelineAnimationFrameEntry entry = ref m_TimelineAnimationFrames[index];
                if (entry.Occupied)
                    continue;
                entry = new TimelineAnimationFrameEntry
                {
                    Occupied = true,
                    Handle = frame.Handle,
                    Generation = frame.Generation,
                    PresentationFrame = frame.PresentationFrame,
                    Contributions = frame.Operations.AnimationContributions
                };
                return;
            }
            throw new InvalidOperationException("Timeline animation presentation capacity was exceeded.");
        }

        public void RetireTimelineProgress(in CharacterPresentationCommand command)
        {
            RequireAlive();
            if (m_ProjectorActive || m_SamplingFrameActive)
                throw new InvalidOperationException("Timeline progress cannot retire inside an open presentation frame.");
            for (int i = 0; i < m_TimelineProgress.Length; i++)
                if (m_TimelineProgress[i].Occupied && m_TimelineProgress[i].Command.Header.EventId.Equals(command.Header.EventId))
                {
                    m_TimelineProgress[i].Withdrawn = true;
                    m_TimelineProgress[i].PresentationFrame = 0;
                }
        }

        public void ReleaseTimeline(ulong actionInstanceId, int operationIndex, string invocationPath, string timelineId, ulong generation)
        {
            for (int i = 0; i < m_TimelineProgress.Length; i++)
            {
                ref TimelineProgressEntry entry = ref m_TimelineProgress[i];
                if (entry.Occupied && entry.Command.SourceActionInstanceId == actionInstanceId &&
                    entry.Command.TimelineProgress.Generation == generation &&
                    entry.Command.Header.Activation.Source.Operation.Value == operationIndex &&
                    string.Equals(entry.Command.Header.Activation.Source.ExecutionPath, invocationPath, StringComparison.Ordinal) &&
                    string.Equals(entry.Command.TimelineProgress.TimelineId, timelineId, StringComparison.Ordinal))
                    entry = default;
            }
        }

        public bool TrySampleTimeline(ulong actionInstanceId, int operationIndex, string invocationPath, string timelineId, ulong generation,
            ulong presentationFrame, ulong localLogicTick, float interpolationAlpha, out TimelineRuntimePresentationSample sample)
        {
            RequireAlive();
            if (presentationFrame == 0 || !float.IsFinite(interpolationAlpha) || interpolationAlpha < 0f || interpolationAlpha > 1f)
                throw new ArgumentOutOfRangeException(nameof(presentationFrame));
            for (int i = 0; i < m_TimelineProgress.Length; i++)
            {
                ref TimelineProgressEntry entry = ref m_TimelineProgress[i];
                if (!entry.Occupied || entry.Command.SourceActionInstanceId != actionInstanceId ||
                    entry.Command.TimelineProgress.Generation != generation ||
                    entry.Command.Header.Activation.Source.Operation.Value != operationIndex ||
                    !string.Equals(entry.Command.Header.Activation.Source.ExecutionPath, invocationPath, StringComparison.Ordinal) ||
                    !string.Equals(entry.Command.TimelineProgress.TimelineId, timelineId, StringComparison.Ordinal))
                    continue;
                if (entry.PresentationFrame == presentationFrame)
                {
                    sample = entry.Sample;
                    return true;
                }
                AbilityTimelineProgress progress = entry.Command.TimelineProgress;
                decimal deltaRaw = (decimal)(progress.Cycle - progress.PreviousCycle) * progress.Duration.Raw +
                    progress.Time.Raw - progress.PreviousTime.Raw;
                decimal alpha = localLogicTick > progress.LogicTick ? 1m :
                    localLogicTick < progress.LogicTick ? 0m : (decimal)interpolationAlpha;
                decimal position = decimal.Round((decimal)progress.PreviousCycle * progress.Duration.Raw +
                    progress.PreviousTime.Raw + deltaRaw * alpha, 0, MidpointRounding.ToEven);
                int cycle = 0;
                if (progress.Loop && progress.Duration.Raw > 0)
                {
                    cycle = checked((int)(position / progress.Duration.Raw));
                    position %= progress.Duration.Raw;
                }
                TimelinePresentationSampleReason reason = entry.Corrected
                    ? TimelinePresentationSampleReason.Correction : TimelinePresentationSampleReason.Advance;
                if (entry.Withdrawn || progress.State == AbilityTimelineProgressState.Stopped)
                {
                    position = progress.Time.Raw;
                    cycle = progress.Cycle;
                    reason = entry.Withdrawn ? TimelinePresentationSampleReason.Withdrawn : TimelinePresentationSampleReason.Stopped;
                }
                else if (!entry.Corrected && progress.State == AbilityTimelineProgressState.Completed && alpha == 1m)
                    reason = TimelinePresentationSampleReason.Completed;
                else if (!entry.Corrected && progress.Control.IsPaused)
                    reason = TimelinePresentationSampleReason.Paused;
                sample = new TimelineRuntimePresentationSample(progress.Generation, progress.LogicTick, progress.ContentRevision,
                    FixedScalar.FromRaw(checked((long)position)), cycle, reason,
                    entry.Withdrawn && entry.Command.Header.Tick.Value > m_ConfirmedTimelineTick);
                entry.Sample = sample;
                entry.PresentationFrame = presentationFrame;
                entry.Corrected = false;
                return true;
            }
            sample = default;
            return false;
        }

        readonly ActionAnimationPlaybackRuntime m_Playback;
        readonly ActionCommittedSampleHistory m_History = new ActionCommittedSampleHistory(64, 128);
        readonly ActionPresentationSampleProjector m_Projector = new ActionPresentationSampleProjector(64);
        ActionSampleHistoryMutationLease m_HistoryLease;
        ActionSampleProjectionMutationLease m_ProjectorLease;
        bool m_HistoryActive;
        bool m_ProjectorActive;
        bool m_Disposed;

        internal CommittedFollowPresentationClockCoordinator(ActionAnimationPlaybackRuntime playback)
        {
            m_Playback = playback ?? throw new ArgumentNullException(nameof(playback));
        }

        public void BeginFrame(IReadOnlyList<ActionAnimationPlaybackCommand> commands)
        {
            RequireAlive();
            if (m_Playback.IsFrameOpen || m_HistoryActive || m_ProjectorActive)
                throw new InvalidOperationException("Committed follow clock frame is already open.");
            try
            {
                m_Playback.BeginFrame(commands);
                m_HistoryLease = m_History.BeginMutation();
                m_HistoryActive = true;
                m_ProjectorLease = m_Projector.BeginMutation();
                m_ProjectorActive = true;
                m_History.ApplyCommands(m_HistoryLease, m_Playback.Commands);
                for (int i = 0; i < commands.Count; i++)
                {
                    ActionAnimationPlaybackCommand command = commands[i];
                    if (command.Kind == ActionAnimationPlaybackCommandKind.Release || command.Kind == ActionAnimationPlaybackCommandKind.Withdraw)
                        m_Projector.RemovePlayback(m_ProjectorLease, command.PlaybackId);
                }
            }
            catch
            {
                DiscardFrame();
                throw;
            }
        }

        public void ValidateFrame()
        {
            RequireAlive();
            RequireActiveFrame();
            if (!m_SamplingFrameActive)
                throw new InvalidOperationException("Action sampling frame is not open.");
            m_Playback.ValidateFrame();
            for (int i = 0; i < m_Playback.Retirements.Count; i++)
            {
                AnimationPlaybackId playbackId = m_Playback.Retirements[i].PlaybackId;
                m_History.RemovePlayback(m_HistoryLease, playbackId);
                m_Projector.RemovePlayback(m_ProjectorLease, playbackId);
            }
            m_Projector.ValidateFrame(m_ProjectorLease);
            m_History.ValidateFrame(m_HistoryLease);
        }

        public void CommitFrame()
        {
            m_Playback.CommitFrame();
            m_History.Commit(m_HistoryLease);
            m_HistoryActive = false;
            m_Projector.Commit(m_ProjectorLease);
            m_ProjectorActive = false;
        }

        public void DiscardFrame()
        {
            if (m_ProjectorActive)
            {
                m_Projector.Discard(m_ProjectorLease);
                m_ProjectorActive = false;
            }
            if (m_HistoryActive)
            {
                m_History.Discard(m_HistoryLease);
                m_HistoryActive = false;
            }
            m_Playback.DiscardFrame();
        }

        public void Reset()
        {
            RequireAlive();
            if (m_Playback.IsFrameOpen || m_HistoryActive || m_ProjectorActive || m_SamplingFrameActive)
                throw new InvalidOperationException(
                    "Committed follow clock cannot reset during an open frame.");
            Array.Clear(m_TimelineProgress, 0, m_TimelineProgress.Length);
            Array.Clear(m_TimelineAnimationFrames, 0, m_TimelineAnimationFrames.Length);
            m_ConfirmedTimelineTick = 0;
            m_Playback.Reset();
            m_History.Reset();
            m_Projector.Reset();
        }

        public IActionPresentationClockPolicy CreatePolicy()
        {
            RequireAlive();
            return new CommittedFollowPresentationClockPolicy(this);
        }

        internal ProjectedActionPresentationSample ProjectSample(
            AnimationChannelId channelId,
            double presentationSampleTick,
            float presentationDeltaSeconds,
            float sourceDurationSeconds)
        {
            RequireAlive();
            RequireActiveFrame();
            if (!channelId.IsValid)
                throw new ArgumentException("Committed follow sampling requires a valid animation channel.", nameof(channelId));
            if (!m_Playback.TryGetLatestPlayback(
                    channelId,
                    out AnimationPlaybackId playbackId,
                    out ActionAnimationPlaybackLifecyclePhase phase))
                throw new InvalidOperationException($"Committed follow channel '{channelId.Value}' has no committed playback sample.");
            m_Playback.TryGetProjectedSample(playbackId, out ActionProjectedSample projected, out EventId sourceEventId);
            if (TryGetTimelineAnimationSample(playbackId, channelId, out PresentationPoseSampleTime timelineSample))
                return new ProjectedActionPresentationSample(playbackId, sourceEventId, timelineSample, false);
            if (projected.IsValid)
                return new ProjectedActionPresentationSample(playbackId, sourceEventId, projected.Time, false);
            if (!m_History.TryGetProjectionWindow(
                    m_HistoryLease,
                    playbackId,
                    presentationSampleTick,
                    out ActionCommittedSampleWindow window))
                throw new InvalidOperationException("Committed follow playback has no committed projection window.");
            return m_Projector.Project(
                m_ProjectorLease,
                playbackId,
                in window,
                presentationSampleTick,
                presentationDeltaSeconds,
                sourceDurationSeconds,
                sourceDurationSeconds,
                phase);
        }

        bool TryGetTimelineAnimationSample(
            AnimationPlaybackId playbackId,
            AnimationChannelId channelId,
            out PresentationPoseSampleTime sample)
        {
            sample = default;
            float weight = float.NegativeInfinity;
            string selectedClipId = null;
            for (int frameIndex = 0; frameIndex < m_TimelineAnimationFrames.Length; frameIndex++)
            {
                TimelineAnimationFrameEntry entry = m_TimelineAnimationFrames[frameIndex];
                if (!entry.Occupied || entry.Generation != playbackId.Generation)
                    continue;
                for (int contributionIndex = 0; contributionIndex < entry.Contributions.Count; contributionIndex++)
                {
                    TimelineAnimationContribution contribution = entry.Contributions[contributionIndex];
                    var producerId = new AnimationProducerId(contribution.TimelineAuthoringId, contribution.TrackAuthoringId);
                    if (!producerId.Equals(playbackId.ProducerId) || !contribution.AnimationChannelId.Equals(channelId) ||
                        contribution.Weight < weight || contribution.Weight == weight &&
                        string.CompareOrdinal(contribution.ClipAuthoringId, selectedClipId) >= 0)
                        continue;
                    weight = contribution.Weight;
                    selectedClipId = contribution.ClipAuthoringId;
                    sample = new PresentationPoseSampleTime(
                        contribution.ClipTime,
                        contribution.ContinuousClipTime,
                        contribution.Cycle,
                        contribution.IsLooping,
                        1f);
                }
            }
            return sample.IsValid;
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            DiscardFrame();
            DiscardSamplingFrame();
            m_Disposed = true;
        }

        void RequireAlive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(
                    nameof(CommittedFollowPresentationClockCoordinator));
        }

        void RequireActiveFrame()
        {
            if (!m_Playback.IsFrameOpen || !m_HistoryActive || !m_ProjectorActive)
                throw new InvalidOperationException(
                    "Committed follow clock frame is not open.");
        }
    }
}
