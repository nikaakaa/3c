namespace ThirdPersonCharacter.Pipeline.Animation
{
    public enum AnimationPlaybackCommandKind
    {
        Selection,
        PoseRequest,
        PoseUnavailable,
        Complete,
        Release
    }

    public readonly struct AnimationPlaybackCommand
    {
        public AnimationPlaybackCommand(
            AnimationPlaybackCommandKind kind,
            ulong localLogicTick,
            ulong sequence,
            AnimationChannelSelection selection,
            AnimationSelectionFrame poseRequest,
            AnimationPlaybackId playbackId)
        {
            Kind = kind;
            LocalLogicTick = localLogicTick;
            Sequence = sequence;
            Selection = selection;
            PoseRequest = poseRequest;
            PlaybackId = playbackId;
        }

        public AnimationPlaybackCommandKind Kind { get; }
        public ulong LocalLogicTick { get; }
        public ulong Sequence { get; }
        public AnimationChannelSelection Selection { get; }
        public AnimationSelectionFrame PoseRequest { get; }
        public AnimationPlaybackId PlaybackId { get; }
    }
}
