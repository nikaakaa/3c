using System.Collections.Generic;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public readonly struct AnimationPlaybackVisualSnapshot
    {
        public AnimationPlaybackVisualSnapshot(string stateKey, float sampleTime)
        {
            StateKey = stateKey ?? string.Empty;
            SampleTime = sampleTime;
        }

        public string StateKey { get; }
        public float SampleTime { get; }
        public bool IsValid => !string.IsNullOrEmpty(StateKey);
    }

    public interface IAnimationPlaybackCommandSink
    {
        void EnqueueSelection(AnimationLayerSelection selection);
        void EnqueueSample(ulong localLogicTick, AnimationProducerSample sample);
        void EnqueuePlaybackComplete(ulong localLogicTick, AnimationPlaybackId playbackId);
        void EnqueuePlaybackRelease(ulong localLogicTick, AnimationPlaybackId playbackId);
    }

    public interface IAnimationPlaybackBatchSource
    {
        int PendingCount { get; }
        void CopyPendingTo(List<AnimationPlaybackCommand> destination);
        void Acknowledge(IReadOnlyList<AnimationPlaybackCommand> commands);
        void Clear();
    }

    public interface IAnimationPlaybackAdapter
    {
        void Play(
            ResolvedAnimationLayer layer,
            ResolvedAnimationProducerBinding binding,
            AnimationProducerSample sample);
        void UpdateSample(AnimationProducerSample sample);
        void FadeToEmpty(ResolvedAnimationLayer layer, Animancer.Easing.Function easing);
        void Evaluate(float presentationDeltaSeconds);
        bool IsRetired(AnimationPlaybackId playbackId);
        float GetWeight(AnimationPlaybackId playbackId);
        float GetFadeProgress(AnimationPlaybackId playbackId);
        bool TryGetVisualSnapshot(
            AnimationPlaybackId playbackId,
            out AnimationPlaybackVisualSnapshot snapshot);
        void Release(AnimationPlaybackId playbackId);
        void Clear();
    }
}
