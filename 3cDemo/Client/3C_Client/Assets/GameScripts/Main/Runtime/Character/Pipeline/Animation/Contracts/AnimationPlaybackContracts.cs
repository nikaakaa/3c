using System;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public readonly struct AnimationPoseSelectionGeneration : IEquatable<AnimationPoseSelectionGeneration>
    {
        public AnimationPoseSelectionGeneration(ulong value)
        {
            Value = value != 0
                ? value
                : throw new ArgumentOutOfRangeException(nameof(value));
        }

        public ulong Value { get; }
        public bool IsValid => Value != 0;
        public bool Equals(AnimationPoseSelectionGeneration other) => Value == other.Value;
        public override bool Equals(object obj) => obj is AnimationPoseSelectionGeneration other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => Value.ToString();
        public static bool operator ==(AnimationPoseSelectionGeneration left, AnimationPoseSelectionGeneration right) => left.Equals(right);
        public static bool operator !=(AnimationPoseSelectionGeneration left, AnimationPoseSelectionGeneration right) => !left.Equals(right);
    }

    public enum AnimationPoseSourceKind : byte
    {
        Timeline = 1,
        MotionMatching = 2
    }

    public readonly struct AnimationPoseSourceId : IEquatable<AnimationPoseSourceId>
    {
        public AnimationPoseSourceId(
            AnimationPlaybackId playbackId,
            AnimationPoseSourceKind sourceKind,
            AnimationPoseSelectionGeneration selectionGeneration)
        {
            if (!playbackId.IsValid || !Enum.IsDefined(typeof(AnimationPoseSourceKind), sourceKind) ||
                !selectionGeneration.IsValid)
            {
                throw new ArgumentException("Animation pose source identity is invalid.");
            }
            PlaybackId = playbackId;
            SourceKind = sourceKind;
            SelectionGeneration = selectionGeneration;
        }

        public AnimationPlaybackId PlaybackId { get; }
        public AnimationPoseSourceKind SourceKind { get; }
        public AnimationPoseSelectionGeneration SelectionGeneration { get; }
        public bool IsValid => PlaybackId.IsValid &&
                               Enum.IsDefined(typeof(AnimationPoseSourceKind), SourceKind) &&
                               SelectionGeneration.IsValid;

        public bool Equals(AnimationPoseSourceId other) =>
            PlaybackId.Equals(other.PlaybackId) &&
            SourceKind == other.SourceKind &&
            SelectionGeneration == other.SelectionGeneration;

        public override bool Equals(object obj) => obj is AnimationPoseSourceId other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = PlaybackId.GetHashCode();
                hash = hash * 397 ^ (int)SourceKind;
                return hash * 397 ^ SelectionGeneration.GetHashCode();
            }
        }

        public override string ToString() => $"{PlaybackId}/{SourceKind}/{SelectionGeneration}";
        public static bool operator ==(AnimationPoseSourceId left, AnimationPoseSourceId right) => left.Equals(right);
        public static bool operator !=(AnimationPoseSourceId left, AnimationPoseSourceId right) => !left.Equals(right);
    }

    public readonly struct ClipSamplePlan
    {
        public ClipSamplePlan(
            int clipBindingIndex,
            AnimationClip clip,
            float clipTime,
            double continuousClipTime,
            float normalizedTime,
            float weight,
            bool isLooping)
        {
            ClipBindingIndex = clipBindingIndex;
            Clip = clip;
            ClipTime = clipTime;
            ContinuousClipTime = continuousClipTime;
            NormalizedTime = normalizedTime;
            Weight = weight;
            IsLooping = isLooping;
            if (!IsValid)
                throw new ArgumentException("Animation clip sample plan is invalid.");
        }

        public int ClipBindingIndex { get; }
        public AnimationClip Clip { get; }
        public float ClipTime { get; }
        public double ContinuousClipTime { get; }
        public float NormalizedTime { get; }
        public float Weight { get; }
        public bool IsLooping { get; }
        public bool IsValid => ClipBindingIndex >= 0 && Clip &&
                               float.IsFinite(Clip.length) && Clip.length > 0f &&
                               float.IsFinite(ClipTime) && ClipTime >= 0f && ClipTime <= Clip.length &&
                               !double.IsNaN(ContinuousClipTime) && !double.IsInfinity(ContinuousClipTime) && ContinuousClipTime >= 0d &&
                               float.IsFinite(NormalizedTime) && NormalizedTime >= 0f && NormalizedTime <= 1f &&
                               float.IsFinite(Weight) && Weight > 0f && Weight <= 1f;
    }

    public readonly struct ResolvedAnimationPoseRequest
    {
        public ResolvedAnimationPoseRequest(
            AnimationChannelId animationChannelId,
            PoseSlotId poseSlotId,
            AnimationPoseSourceId sourceId,
            ulong sourcePoseContinuityIdentity,
            ulong presentationRequestSequence,
            int programProducerIndex,
            float visualSampleTime,
            double continuousVisualTime,
            int cycle,
            float visualTimeScale,
            AnimationReadOnlyBuffer<ClipSamplePlan> clips,
            AnimationReadOnlyBuffer<float> poseParameters,
            AnimationFootFeatureSample leftFootFeatures,
            AnimationFootFeatureSample rightFootFeatures,
            bool hasFootFeatures,
            AnimationBlendTransitionIdentity exactTransitionIdentity)
        {
            AnimationChannelId = animationChannelId;
            PoseSlotId = poseSlotId;
            SourceId = sourceId;
            SourcePoseContinuityIdentity = sourcePoseContinuityIdentity;
            PresentationRequestSequence = presentationRequestSequence;
            ProgramProducerIndex = programProducerIndex;
            VisualSampleTime = visualSampleTime;
            ContinuousVisualTime = continuousVisualTime;
            Cycle = cycle;
            VisualTimeScale = visualTimeScale;
            Clips = clips;
            PoseParameters = poseParameters;
            LeftFootFeatures = leftFootFeatures;
            RightFootFeatures = rightFootFeatures;
            HasFootFeatures = hasFootFeatures;
            ExactTransitionIdentity = exactTransitionIdentity;
            if (!IsValid)
                throw new ArgumentException("Resolved animation pose request is invalid.");
        }

        public AnimationChannelId AnimationChannelId { get; }
        public PoseSlotId PoseSlotId { get; }
        public AnimationPoseSourceId SourceId { get; }
        public ulong SourcePoseContinuityIdentity { get; }
        public ulong PresentationRequestSequence { get; }
        public int ProgramProducerIndex { get; }
        public float VisualSampleTime { get; }
        public double ContinuousVisualTime { get; }
        public int Cycle { get; }
        public float VisualTimeScale { get; }
        public AnimationReadOnlyBuffer<ClipSamplePlan> Clips { get; }
        public AnimationReadOnlyBuffer<float> PoseParameters { get; }
        public AnimationFootFeatureSample LeftFootFeatures { get; }
        public AnimationFootFeatureSample RightFootFeatures { get; }
        public bool HasFootFeatures { get; }
        public AnimationBlendTransitionIdentity ExactTransitionIdentity { get; }

        public bool IsValid
        {
            get
            {
                bool footFeaturesValid = HasFootFeatures
                    ? LeftFootFeatures.IsValid && RightFootFeatures.IsValid
                    : !LeftFootFeatures.IsValid && !RightFootFeatures.IsValid;
                if (!AnimationChannelId.IsValid || !PoseSlotId.IsValid || !SourceId.IsValid ||
                    SourcePoseContinuityIdentity == 0 || PresentationRequestSequence == 0 || ProgramProducerIndex < 0 ||
                    !float.IsFinite(VisualSampleTime) || VisualSampleTime < 0f ||
                    double.IsNaN(ContinuousVisualTime) || double.IsInfinity(ContinuousVisualTime) || ContinuousVisualTime < 0d ||
                    Cycle < 0 || !float.IsFinite(VisualTimeScale) || VisualTimeScale < 0f ||
                    Clips.Count == 0 || !ExactTransitionIdentity.IsValid ||
                    ExactTransitionIdentity.PoseSlotId != PoseSlotId || ExactTransitionIdentity.TargetEmpty ||
                    ExactTransitionIdentity.TargetProducerIndex != ProgramProducerIndex || !footFeaturesValid)
                {
                    return false;
                }
                for (int i = 0; i < Clips.Count; i++)
                {
                    if (!Clips[i].IsValid)
                        return false;
                    for (int previous = 0; previous < i; previous++)
                    {
                        if (Clips[previous].ClipBindingIndex == Clips[i].ClipBindingIndex)
                            return false;
                    }
                }
                for (int i = 0; i < PoseParameters.Count; i++)
                {
                    if (!float.IsFinite(PoseParameters[i]))
                        return false;
                }
                return true;
            }
        }
    }

    public readonly struct AnimationChannelSelection
    {
        public AnimationChannelSelection(
            AnimationChannelId animationChannelId,
            AnimationPlaybackId playbackId,
            bool hasPlayback,
            ulong localLogicTick,
            ulong sequence)
        {
            AnimationChannelId = animationChannelId;
            PlaybackId = playbackId;
            HasPlayback = hasPlayback;
            LocalLogicTick = localLogicTick;
            Sequence = sequence;
        }

        public AnimationChannelId AnimationChannelId { get; }
        public AnimationPlaybackId PlaybackId { get; }
        public bool HasPlayback { get; }
        public ulong LocalLogicTick { get; }
        public ulong Sequence { get; }
        public bool IsValid => AnimationChannelId.IsValid &&
                               LocalLogicTick != 0 &&
                               Sequence != 0 &&
                               (!HasPlayback || PlaybackId.IsValid);

        public static AnimationChannelSelection Select(
            AnimationChannelId animationChannelId,
            AnimationPlaybackId playbackId,
            ulong localLogicTick,
            ulong sequence)
        {
            return new AnimationChannelSelection(animationChannelId, playbackId, true, localLogicTick, sequence);
        }

        public static AnimationChannelSelection Empty(AnimationChannelId animationChannelId, ulong localLogicTick, ulong sequence)
        {
            return new AnimationChannelSelection(animationChannelId, default, false, localLogicTick, sequence);
        }
    }

}
