using System;
using BTSMTL.Timeline;
using ThirdPersonCharacter.Pipeline.Animation;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    internal static class ActionAnimationPlaybackCommandFactory
    {
        internal static ActionAnimationPlaybackCommand Create(
            CharacterPresentationCommand command,
            in ResolvedActionAnimationBinding binding)
        {
            if (!binding.IsValid ||
                !string.Equals(
                    command.ProducerId,
                    binding.ProgramProducerId,
                    StringComparison.Ordinal) ||
                command.SourceActionInstanceId == 0)
            {
                throw new InvalidOperationException(
                    "Presentation command has no exact finite Action binding or Action instance.");
            }

            var playbackId = new AnimationPlaybackId(
                binding.ProducerId,
                command.ProducerGeneration);
            switch (command.Kind)
            {
                case CharacterPresentationCommandKind.SelectProducer:
                    return ActionAnimationPlaybackCommand.Select(
                        command.Header.EventId,
                        command.Header.Tick.Value,
                        playbackId,
                        command.SourceActionInstanceId,
                        binding.AnimationChannelId,
                        binding.ProgramProducerId);
                case CharacterPresentationCommandKind.SampleProducer:
                    bool loop =
                        binding.PlaybackMode ==
                        TimelinePlaybackMode.Loop;
                    double continuousTime =
                        command.Cycle *
                        (double)binding.Animation.DurationSeconds +
                        command.SampleTime;
                    return ActionAnimationPlaybackCommand.Sample(
                        playbackId,
                        command.SourceActionInstanceId,
                        binding.AnimationChannelId,
                        binding.ProgramProducerId,
                        new ActionCommittedRawSample(
                            command.Header.EventId,
                            command.Header.Tick.Value,
                            command.Header.Sequence,
                            command.SampleTime,
                            continuousTime,
                            command.Cycle,
                            loop,
                            command.VisualTimeScale,
                            command.Weight));
                case CharacterPresentationCommandKind.CompleteProducer:
                    return ActionAnimationPlaybackCommand.Complete(
                        command.Header.EventId,
                        command.Header.Tick.Value,
                        playbackId,
                        command.SourceActionInstanceId,
                        binding.AnimationChannelId,
                        binding.ProgramProducerId);
                case CharacterPresentationCommandKind.ReleaseProducer:
                    return ActionAnimationPlaybackCommand.Release(
                        command.Header.EventId,
                        command.Header.Tick.Value,
                        playbackId,
                        command.SourceActionInstanceId,
                        binding.AnimationChannelId,
                        binding.ProgramProducerId);
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(command),
                        command.Kind,
                        "Presentation command is not a finite Action playback command.");
            }
        }
    }
}
