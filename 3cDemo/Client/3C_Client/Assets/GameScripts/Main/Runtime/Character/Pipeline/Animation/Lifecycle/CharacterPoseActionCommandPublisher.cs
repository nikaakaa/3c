using System;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Animation.Lifecycle
{
    internal sealed class CharacterPoseActionCommandPublisher
    {
        readonly ActionPlaybackCommandInbox m_Inbox;

        internal CharacterPoseActionCommandPublisher(ActionPlaybackCommandInbox inbox)
        {
            m_Inbox = inbox ?? throw new ArgumentNullException(nameof(inbox));
        }

        internal void Publish(CharacterPresentationCommand command)
        {
            m_Inbox.Publish(Convert(command));
        }

        internal void Replace(CharacterPresentationCommand current, CharacterPresentationCommand replacement)
        {
            m_Inbox.Replace(current.Header.EventId, Convert(replacement));
        }

        internal void Retire(CharacterPresentationCommand command)
        {
            m_Inbox.Retire(Convert(command));
        }

        static ActionAnimationPlaybackCommand Convert(CharacterPresentationCommand command)
        {
            return command.Kind switch
            {
                CharacterPresentationCommandKind.SelectProducer => ActionAnimationPlaybackCommand.Select(
                    command.Header.EventId,
                    command.Header.Tick.Value,
                    BuildPlaybackId(command.ProducerId, command.ProducerGeneration),
                    command.SourceActionInstanceId,
                    new AnimationChannelId(command.Header.Channel),
                    command.ProducerId),
                CharacterPresentationCommandKind.SampleProducer => ActionAnimationPlaybackCommand.Sample(
                    BuildPlaybackId(command.ProducerId, command.ProducerGeneration),
                    command.SourceActionInstanceId,
                    new AnimationChannelId(command.Header.Channel),
                    command.ProducerId,
                    new ActionCommittedRawSample(
                        command.Header.EventId,
                        command.Header.Tick.Value,
                        command.Header.Sequence,
                        command.SampleTime,
                        command.SampleTime,
                        command.Cycle,
                        false,
                        command.VisualTimeScale,
                        command.Weight)),
                CharacterPresentationCommandKind.CompleteProducer => ActionAnimationPlaybackCommand.Complete(
                    command.Header.EventId,
                    command.Header.Tick.Value,
                    BuildPlaybackId(command.ProducerId, command.ProducerGeneration),
                    command.SourceActionInstanceId,
                    new AnimationChannelId(command.Header.Channel),
                    command.ProducerId),
                CharacterPresentationCommandKind.ReleaseProducer => ActionAnimationPlaybackCommand.Release(
                    command.Header.EventId,
                    command.Header.Tick.Value,
                    BuildPlaybackId(command.ProducerId, command.ProducerGeneration),
                    command.SourceActionInstanceId,
                    new AnimationChannelId(command.Header.Channel),
                    command.ProducerId),
                CharacterPresentationCommandKind.ForceReleaseProducer => ActionAnimationPlaybackCommand.Release(
                    command.Header.EventId,
                    command.Header.Tick.Value,
                    BuildPlaybackId(command.ProducerId, command.ProducerGeneration),
                    command.SourceActionInstanceId,
                    new AnimationChannelId(command.Header.Channel),
                    command.ProducerId),
                _ => throw new ArgumentException(
                    $"Presentation command kind '{command.Kind}' is not a playback command.",
                    nameof(command))
            };
        }

        static AnimationPlaybackId BuildPlaybackId(string producerId, ulong generation)
        {
            const string prefix = "producer:";
            if (!producerId.StartsWith(prefix, StringComparison.Ordinal))
                throw new ArgumentException($"Producer identity '{producerId}' does not use the canonical format.", nameof(producerId));
            string remainder = producerId.Substring(prefix.Length);
            int separator = remainder.IndexOf(':');
            if (separator < 0)
                throw new ArgumentException($"Producer identity '{producerId}' has no track segment.", nameof(producerId));
            var id = new AnimationProducerId(remainder.Substring(0, separator), remainder.Substring(separator + 1));
            return new AnimationPlaybackId(id, generation);
        }
    }
}
