using System;
using System.Collections.Generic;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Animation.Lifecycle
{
    public sealed class CharacterAnimationPlaybackCommandQueue : IAnimationPlaybackCommandSink, IAnimationPlaybackBatchSource
    {
        readonly List<AnimationPlaybackCommand> m_Commands = new List<AnimationPlaybackCommand>();
        ulong m_NextSequence;

        public int PendingCount => m_Commands.Count;

        public void EnqueueSelection(AnimationChannelSelection selection)
        {
            if (!selection.IsValid)
                throw new ArgumentException("Animation selection is invalid.", nameof(selection));

            Enqueue(new AnimationPlaybackCommand(
                AnimationPlaybackCommandKind.Selection,
                selection.LocalLogicTick,
                NextSequence(),
                selection,
                default,
                selection.PlaybackId));
        }

        public void EnqueuePoseRequest(ulong localLogicTick, AnimationSelectionFrame poseRequest)
        {
            if (!poseRequest.IsValid)
                throw new ArgumentException("Resolved animation pose request is invalid.", nameof(poseRequest));

            Enqueue(new AnimationPlaybackCommand(
                AnimationPlaybackCommandKind.PoseRequest,
                localLogicTick,
                NextSequence(),
                default,
                poseRequest,
                poseRequest.SourceId.PlaybackId));
        }

        public void EnqueuePoseUnavailable(
            ulong localLogicTick,
            AnimationChannelId channelId,
            AnimationPlaybackId playbackId)
        {
            if (!channelId.IsValid || !playbackId.IsValid)
                throw new ArgumentException("Unavailable animation pose identity is invalid.");

            ulong sequence = NextSequence();
            Enqueue(new AnimationPlaybackCommand(
                AnimationPlaybackCommandKind.PoseUnavailable,
                localLogicTick,
                sequence,
                AnimationChannelSelection.Select(channelId, playbackId, localLogicTick, sequence),
                default,
                playbackId));
        }

        public void EnqueuePlaybackComplete(ulong localLogicTick, AnimationPlaybackId playbackId)
        {
            EnqueuePlaybackCommand(AnimationPlaybackCommandKind.Complete, localLogicTick, playbackId);
        }

        public void EnqueuePlaybackRelease(ulong localLogicTick, AnimationPlaybackId playbackId)
        {
            EnqueuePlaybackCommand(AnimationPlaybackCommandKind.Release, localLogicTick, playbackId);
        }

        public void CopyPendingTo(List<AnimationPlaybackCommand> destination)
        {
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));

            destination.Clear();
            destination.AddRange(m_Commands);
            destination.Sort(CompareCommands);
        }

        public void Acknowledge(IReadOnlyList<AnimationPlaybackCommand> commands)
        {
            if (commands == null || commands.Count == 0)
                return;

            ulong maxSequence = 0;
            for (int i = 0; i < commands.Count; i++)
                maxSequence = Math.Max(maxSequence, commands[i].Sequence);

            for (int i = m_Commands.Count - 1; i >= 0; i--)
            {
                if (m_Commands[i].Sequence <= maxSequence)
                    m_Commands.RemoveAt(i);
            }
        }

        public void Clear()
        {
            m_Commands.Clear();
        }

        public void DiscardFrameLocalPoseCommands()
        {
            for (int i = m_Commands.Count - 1; i >= 0; i--)
            {
                AnimationPlaybackCommandKind kind = m_Commands[i].Kind;
                if (kind == AnimationPlaybackCommandKind.PoseRequest ||
                    kind == AnimationPlaybackCommandKind.PoseUnavailable)
                {
                    m_Commands.RemoveAt(i);
                }
            }
        }

        void EnqueuePlaybackCommand(
            AnimationPlaybackCommandKind kind,
            ulong localLogicTick,
            AnimationPlaybackId playbackId)
        {
            if (!playbackId.IsValid)
                throw new ArgumentException("Animation playback id is invalid.", nameof(playbackId));

            Enqueue(new AnimationPlaybackCommand(
                kind,
                localLogicTick,
                NextSequence(),
                default,
                default,
                playbackId));
        }

        void Enqueue(AnimationPlaybackCommand command)
        {
            m_Commands.Add(command);
        }

        ulong NextSequence()
        {
            m_NextSequence++;
            if (m_NextSequence == 0)
                m_NextSequence++;
            return m_NextSequence;
        }

        static int CompareCommands(AnimationPlaybackCommand left, AnimationPlaybackCommand right)
        {
            int tick = left.LocalLogicTick.CompareTo(right.LocalLogicTick);
            return tick != 0 ? tick : left.Sequence.CompareTo(right.Sequence);
        }
    }
}
