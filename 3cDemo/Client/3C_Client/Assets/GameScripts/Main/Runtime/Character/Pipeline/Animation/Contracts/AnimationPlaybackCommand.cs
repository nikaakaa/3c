namespace ThirdPersonCharacter.Pipeline.Animation
{
    public enum AnimationPlaybackCommandKind
    {
        Selection,
        Sample,
        Complete,
        Release
    }

    public readonly struct AnimationPlaybackCommand
    {
        public AnimationPlaybackCommand(
            AnimationPlaybackCommandKind kind,
            ulong localLogicTick,
            ulong sequence,
            AnimationLayerSelection selection,
            AnimationProducerSample sample,
            AnimationPlaybackId playbackId)
        {
            Kind = kind;
            LocalLogicTick = localLogicTick;
            Sequence = sequence;
            Selection = selection;
            Sample = sample;
            PlaybackId = playbackId;
        }

        public AnimationPlaybackCommandKind Kind { get; }
        public ulong LocalLogicTick { get; }
        public ulong Sequence { get; }
        public AnimationLayerSelection Selection { get; }
        public AnimationProducerSample Sample { get; }
        public AnimationPlaybackId PlaybackId { get; }
    }
}
