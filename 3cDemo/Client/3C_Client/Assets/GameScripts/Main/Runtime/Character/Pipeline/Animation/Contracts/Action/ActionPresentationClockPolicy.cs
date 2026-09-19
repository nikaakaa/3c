using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Animation.Lifecycle;
using ThirdPersonSimulation;

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
        void BeginFrame(IReadOnlyList<ActionAnimationPlaybackCommand> commands);
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
        readonly ActionAnimationPlaybackLifecycleRegistry m_Registry =
            new ActionAnimationPlaybackLifecycleRegistry(64, 8, 128);
        readonly ActionCommittedSampleHistory m_History =
            new ActionCommittedSampleHistory(64, 128);
        readonly ActionPresentationSampleProjector m_Projector =
            new ActionPresentationSampleProjector(64);
        readonly List<ActionPlaybackInboxEntry> m_Entries =
            new List<ActionPlaybackInboxEntry>(128);
        ActionLifecycleMutationLease m_RegistryLease;
        ActionSampleHistoryMutationLease m_HistoryLease;
        ActionSampleProjectionMutationLease m_ProjectorLease;
        ulong m_NextCommandSequence;
        bool m_RegistryActive;
        bool m_HistoryActive;
        bool m_ProjectorActive;
        bool m_Disposed;

        public void BeginFrame(IReadOnlyList<ActionAnimationPlaybackCommand> commands)
        {
            RequireAlive();
            if (m_RegistryActive || m_HistoryActive || m_ProjectorActive)
                throw new InvalidOperationException(
                    "Committed follow clock frame is already open.");
            m_Entries.Clear();
            if (commands != null)
            {
                for (int i = 0; i < commands.Count; i++)
                {
                    ActionAnimationPlaybackCommand command = commands[i];
                    if (!command.IsValid)
                        throw new InvalidOperationException(
                            "Committed follow clock received an invalid command.");
                    m_NextCommandSequence++;
                    if (m_NextCommandSequence == 0)
                        throw new InvalidOperationException(
                            "Committed follow clock command sequence was exhausted.");
                    m_Entries.Add(new ActionPlaybackInboxEntry(
                        m_NextCommandSequence,
                        command));
                }
            }
            try
            {
                m_RegistryLease = m_Registry.BeginMutation();
                m_RegistryActive = true;
                m_HistoryLease = m_History.BeginMutation();
                m_HistoryActive = true;
                m_ProjectorLease = m_Projector.BeginMutation();
                m_ProjectorActive = true;
                m_Registry.ApplyCommands(m_RegistryLease, m_Entries);
                m_History.ApplyCommands(m_HistoryLease, m_Entries);
                for (int i = 0; i < m_Entries.Count; i++)
                {
                    ActionAnimationPlaybackCommand command = m_Entries[i].Command;
                    if (command.Kind == ActionAnimationPlaybackCommandKind.Release)
                        m_Projector.RemovePlayback(m_ProjectorLease, command.PlaybackId);
                }
                m_Registry.ValidateFrame(m_RegistryLease);
                m_History.ValidateFrame(m_HistoryLease);
            }
            catch
            {
                DiscardFrame();
                throw;
            }
        }

        public void CommitFrame()
        {
            RequireAlive();
            RequireActiveFrame();
            try
            {
                m_Projector.ValidateFrame(m_ProjectorLease);
                m_History.ValidateFrame(m_HistoryLease);
                m_Registry.Commit(m_RegistryLease);
                m_RegistryActive = false;
                m_History.Commit(m_HistoryLease);
                m_HistoryActive = false;
                m_Projector.Commit(m_ProjectorLease);
                m_ProjectorActive = false;
            }
            catch
            {
                DiscardFrame();
                throw;
            }
            finally
            {
                m_Entries.Clear();
            }
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
            if (m_RegistryActive)
            {
                m_Registry.Discard(m_RegistryLease);
                m_RegistryActive = false;
            }
            m_Entries.Clear();
        }

        public void Reset()
        {
            RequireAlive();
            if (m_RegistryActive || m_HistoryActive || m_ProjectorActive)
                throw new InvalidOperationException(
                    "Committed follow clock cannot reset during an open frame.");
            m_Registry.Reset();
            m_History.Reset();
            m_Projector.Reset();
            m_Entries.Clear();
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
            if (!m_Registry.TryGetLatestPlayback(
                    channelId,
                    out AnimationPlaybackId playbackId,
                    out ActionAnimationPlaybackLifecyclePhase phase))
                throw new InvalidOperationException($"Committed follow channel '{channelId.Value}' has no committed playback sample.");
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

        public void Dispose()
        {
            if (m_Disposed)
                return;
            DiscardFrame();
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
            if (!m_RegistryActive || !m_HistoryActive || !m_ProjectorActive)
                throw new InvalidOperationException(
                    "Committed follow clock frame is not open.");
        }
    }
}
