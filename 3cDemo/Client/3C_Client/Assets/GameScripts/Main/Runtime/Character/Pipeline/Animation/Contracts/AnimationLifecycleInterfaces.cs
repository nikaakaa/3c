using System.Collections.Generic;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public readonly struct AnimationPoseContribution
    {
        public AnimationPoseContribution(
            AnimationChannelId animationChannelId,
            int programProducerIndex,
            AnimationPlaybackId playbackId,
            float visualSampleTime,
            float normalizedTime,
            int cycle,
            float visualTimeScale,
            float weight)
        {
            AnimationChannelId = animationChannelId;
            ProgramProducerIndex = programProducerIndex;
            PlaybackId = playbackId;
            VisualSampleTime = visualSampleTime;
            NormalizedTime = normalizedTime;
            Cycle = cycle;
            VisualTimeScale = visualTimeScale;
            Weight = weight;
        }

        public AnimationChannelId AnimationChannelId { get; }
        public int ProgramProducerIndex { get; }
        public AnimationProducerId ProducerId => PlaybackId.ProducerId;
        public AnimationPlaybackId PlaybackId { get; }
        public float VisualSampleTime { get; }
        public float NormalizedTime { get; }
        public int Cycle { get; }
        public float VisualTimeScale { get; }
        public float Weight { get; }
        public bool IsValid => AnimationChannelId.IsValid &&
                               ProgramProducerIndex >= 0 &&
                               PlaybackId.IsValid &&
                               !float.IsNaN(VisualSampleTime) &&
                               !float.IsInfinity(VisualSampleTime) &&
                               NormalizedTime >= 0f && NormalizedTime <= 1f &&
                               Cycle >= 0 &&
                               !float.IsNaN(VisualTimeScale) &&
                               !float.IsInfinity(VisualTimeScale) &&
                               VisualTimeScale >= 0f &&
                               Weight > 0f && Weight <= 1f;
    }

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
        void EnqueueSelection(AnimationChannelSelection selection);
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
        void CollectPoseContributions(string layerId, List<AnimationPoseContribution> destination);
        void Release(AnimationPlaybackId playbackId);
        void Clear();
    }
}
