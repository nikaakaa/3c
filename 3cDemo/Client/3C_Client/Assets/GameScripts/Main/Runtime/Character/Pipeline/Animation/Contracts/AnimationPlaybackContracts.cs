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
        MotionMatching = 2,
        BlendSpace = 3
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
            : this(clipBindingIndex, default, clip, clipTime, continuousClipTime, normalizedTime, weight, isLooping)
        {
        }

        public ClipSamplePlan(
            int clipBindingIndex,
            CharacterAnimationBlendSpaceSampleId blendSpaceSampleId,
            AnimationClip clip,
            float clipTime,
            double continuousClipTime,
            float normalizedTime,
            float weight,
            bool isLooping)
        {
            ClipBindingIndex = clipBindingIndex;
            BlendSpaceSampleId = blendSpaceSampleId;
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
        public CharacterAnimationBlendSpaceSampleId BlendSpaceSampleId { get; }
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

    public readonly struct PresentationParameterPageId : IEquatable<PresentationParameterPageId>
    {
        public PresentationParameterPageId(ulong value)
        {
            Value = value != 0 ? value : throw new ArgumentOutOfRangeException(nameof(value));
        }

        public ulong Value { get; }
        public bool IsValid => Value != 0;
        public bool Equals(PresentationParameterPageId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is PresentationParameterPageId other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => Value.ToString();
    }

    public readonly struct AnimationMarkerBindingId : IEquatable<AnimationMarkerBindingId>
    {
        public AnimationMarkerBindingId(string value)
        {
            Value = string.IsNullOrWhiteSpace(value)
                ? throw new ArgumentException("Animation Marker Binding identity is empty.", nameof(value))
                : value.Trim();
        }

        public string Value { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(Value);
        public bool Equals(AnimationMarkerBindingId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is AnimationMarkerBindingId other && Equals(other);
        public override int GetHashCode() => Value?.GetHashCode() ?? 0;
        public override string ToString() => Value ?? string.Empty;
        public static bool operator ==(AnimationMarkerBindingId left, AnimationMarkerBindingId right) => left.Equals(right);
        public static bool operator !=(AnimationMarkerBindingId left, AnimationMarkerBindingId right) => !left.Equals(right);
    }

    public enum PoseDiscontinuityReason : byte
    {
        SourceIdentityChanged = 1,
        SelectionGenerationChanged = 2,
        SourcePoseContinuityChanged = 3,
        Reset = 4
    }

    public enum PoseDiscontinuityResetReason : byte
    {
        None = 0,
        Initialization = 1,
        PresentationReset = 2,
        BranchReplacement = 3,
        PreviewSeek = 4,
        ProjectionReplacement = 5
    }

    public readonly struct PoseDiscontinuityEndpoint : IEquatable<PoseDiscontinuityEndpoint>
    {
        public PoseDiscontinuityEndpoint(
            int programProducerIndex,
            AnimationPoseSourceKind sourceKind,
            ulong playbackGeneration,
            ulong selectionGeneration)
        {
            if (programProducerIndex < 0 || !Enum.IsDefined(typeof(AnimationPoseSourceKind), sourceKind) ||
                playbackGeneration == 0 || selectionGeneration == 0)
                throw new ArgumentException("Pose Discontinuity endpoint is invalid.");
            ProgramProducerIndex = programProducerIndex;
            SourceKind = sourceKind;
            PlaybackGeneration = playbackGeneration;
            SelectionGeneration = selectionGeneration;
        }

        public int ProgramProducerIndex { get; }
        public AnimationPoseSourceKind SourceKind { get; }
        public ulong PlaybackGeneration { get; }
        public ulong SelectionGeneration { get; }
        public bool IsValid => ProgramProducerIndex >= 0 &&
                               Enum.IsDefined(typeof(AnimationPoseSourceKind), SourceKind) &&
                               PlaybackGeneration != 0 && SelectionGeneration != 0;

        public bool Equals(PoseDiscontinuityEndpoint other) =>
            ProgramProducerIndex == other.ProgramProducerIndex && SourceKind == other.SourceKind &&
            PlaybackGeneration == other.PlaybackGeneration && SelectionGeneration == other.SelectionGeneration;
        public override bool Equals(object obj) => obj is PoseDiscontinuityEndpoint other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(ProgramProducerIndex, (int)SourceKind, PlaybackGeneration, SelectionGeneration);

        public static PoseDiscontinuityEndpoint From(in AnimationSelectionFrame selection) => new PoseDiscontinuityEndpoint(
            selection.ProgramProducerIndex,
            selection.SourceId.SourceKind,
            selection.SourceId.PlaybackId.Generation,
            selection.SourceId.SelectionGeneration.Value);
    }

    public readonly struct PoseDiscontinuity
    {
        public const string SchemaVersion = "pose-discontinuity/v1";

        PoseDiscontinuity(
            ulong eventIdentity,
            ulong completionIdentity,
            PoseDiscontinuityEndpoint previousEndpoint,
            PoseDiscontinuityEndpoint currentEndpoint,
            ulong previousContinuityIdentity,
            ulong currentContinuityIdentity,
            PoseDiscontinuityReason reason,
            PoseDiscontinuityResetReason resetReason,
            ulong resetSequence,
            bool hasPreviousEndpoint,
            bool hasCurrentEndpoint)
        {
            EventIdentity = eventIdentity;
            CompletionIdentity = completionIdentity;
            PreviousEndpoint = previousEndpoint;
            CurrentEndpoint = currentEndpoint;
            PreviousContinuityIdentity = previousContinuityIdentity;
            CurrentContinuityIdentity = currentContinuityIdentity;
            Reason = reason;
            ResetReason = resetReason;
            ResetSequence = resetSequence;
            HasPreviousEndpoint = hasPreviousEndpoint ? (byte)1 : (byte)0;
            HasCurrentEndpoint = hasCurrentEndpoint ? (byte)1 : (byte)0;
        }

        public ulong EventIdentity { get; }
        public ulong CompletionIdentity { get; }
        public PoseDiscontinuityEndpoint PreviousEndpoint { get; }
        public PoseDiscontinuityEndpoint CurrentEndpoint { get; }
        public ulong PreviousContinuityIdentity { get; }
        public ulong CurrentContinuityIdentity { get; }
        public PoseDiscontinuityReason Reason { get; }
        public PoseDiscontinuityResetReason ResetReason { get; }
        public ulong ResetSequence { get; }
        public byte HasPreviousEndpoint { get; }
        public byte HasCurrentEndpoint { get; }
        public bool IsPresent => EventIdentity != 0;
        public bool IsReset => IsPresent && Reason == PoseDiscontinuityReason.Reset;
        public bool IsValid => !IsPresent || CompletionIdentity != 0 &&
                               Enum.IsDefined(typeof(PoseDiscontinuityReason), Reason) &&
                               HasPreviousEndpoint <= 1 && HasCurrentEndpoint <= 1 &&
                               (HasPreviousEndpoint == 0 || PreviousEndpoint.IsValid) &&
                               (HasCurrentEndpoint == 0 || CurrentEndpoint.IsValid) &&
                               (IsReset
                                   ? ResetReason != PoseDiscontinuityResetReason.None && ResetSequence != 0
                                   : ResetReason == PoseDiscontinuityResetReason.None && ResetSequence == 0 &&
                                     HasPreviousEndpoint == 1 && HasCurrentEndpoint == 1 &&
                                     PreviousContinuityIdentity != 0 && CurrentContinuityIdentity != 0);

        public static PoseDiscontinuity SourceJump(
            ulong eventIdentity,
            ulong completionIdentity,
            PoseDiscontinuityEndpoint previousEndpoint,
            PoseDiscontinuityEndpoint currentEndpoint,
            ulong previousContinuityIdentity,
            ulong currentContinuityIdentity,
            PoseDiscontinuityReason reason)
        {
            var value = new PoseDiscontinuity(
                eventIdentity, completionIdentity, previousEndpoint, currentEndpoint,
                previousContinuityIdentity, currentContinuityIdentity, reason,
                PoseDiscontinuityResetReason.None, 0, true, true);
            if (!value.IsValid || value.IsReset)
                throw new ArgumentException("Pose Discontinuity source jump is invalid.");
            return value;
        }

        public static PoseDiscontinuity Reset(
            ulong eventIdentity,
            ulong completionIdentity,
            PoseDiscontinuityEndpoint currentEndpoint,
            ulong currentContinuityIdentity,
            PoseDiscontinuityResetReason resetReason,
            ulong resetSequence,
            bool hasCurrentEndpoint)
        {
            var value = new PoseDiscontinuity(
                eventIdentity, completionIdentity, default, currentEndpoint, 0, currentContinuityIdentity,
                PoseDiscontinuityReason.Reset, resetReason, resetSequence, false, hasCurrentEndpoint);
            if (!value.IsValid)
                throw new ArgumentException("Pose Discontinuity reset is invalid.");
            return value;
        }
    }

    public readonly struct AnimationSelectionFrame
    {
        public const string SchemaVersion = "animation-selection-frame/v3";

        public AnimationSelectionFrame(
            AnimationChannelId animationChannelId,
            AnimationPoseSourceId sourceId,
            ulong sourcePoseContinuityIdentity,
            ulong presentationRequestSequence,
            int programProducerIndex,
            AnimationMarkerBindingId markerBindingId,
            float visualSampleTime,
            double continuousVisualTime,
            int cycle,
            bool loop,
            float visualTimeScale,
            AnimationReadOnlyBuffer<ClipSamplePlan> clips,
            PresentationParameterPageId parameterPageId,
            AnimationReadOnlyBuffer<float> poseParameters,
            AnimationReadOnlyBuffer<byte> poseParameterAvailability)
        {
            AnimationChannelId = animationChannelId;
            SourceId = sourceId;
            SourcePoseContinuityIdentity = sourcePoseContinuityIdentity;
            PresentationRequestSequence = presentationRequestSequence;
            ProgramProducerIndex = programProducerIndex;
            MarkerBindingId = markerBindingId;
            VisualSampleTime = visualSampleTime;
            ContinuousVisualTime = continuousVisualTime;
            Cycle = cycle;
            Loop = loop;
            VisualTimeScale = visualTimeScale;
            Clips = clips;
            ParameterPageId = parameterPageId;
            PoseParameters = poseParameters;
            PoseParameterAvailability = poseParameterAvailability;
            if (!IsValid)
                throw new ArgumentException("Animation Selection Frame is invalid.");
        }

        public AnimationChannelId AnimationChannelId { get; }
        public AnimationPoseSourceId SourceId { get; }
        public ulong SourcePoseContinuityIdentity { get; }
        public ulong PresentationRequestSequence { get; }
        public int ProgramProducerIndex { get; }
        public AnimationMarkerBindingId MarkerBindingId { get; }
        public float VisualSampleTime { get; }
        public double ContinuousVisualTime { get; }
        public int Cycle { get; }
        public bool Loop { get; }
        public float VisualTimeScale { get; }
        public AnimationReadOnlyBuffer<ClipSamplePlan> Clips { get; }
        public PresentationParameterPageId ParameterPageId { get; }
        public AnimationReadOnlyBuffer<float> PoseParameters { get; }
        public AnimationReadOnlyBuffer<byte> PoseParameterAvailability { get; }

        public bool IsValid
        {
            get
            {
                if (!AnimationChannelId.IsValid || !SourceId.IsValid ||
                    SourcePoseContinuityIdentity == 0 || PresentationRequestSequence == 0 || ProgramProducerIndex < 0 ||
                    (SourceId.SourceKind == AnimationPoseSourceKind.Timeline) != MarkerBindingId.IsValid ||
                    !float.IsFinite(VisualSampleTime) || VisualSampleTime < 0f ||
                    double.IsNaN(ContinuousVisualTime) || double.IsInfinity(ContinuousVisualTime) || ContinuousVisualTime < 0d ||
                    Cycle < 0 || !float.IsFinite(VisualTimeScale) || VisualTimeScale < 0f ||
                    Clips.Count == 0 || !ParameterPageId.IsValid ||
                    PoseParameters.Count == 0 || PoseParameterAvailability.Count != PoseParameters.Count)
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
                    if (!float.IsFinite(PoseParameters[i]) || PoseParameterAvailability[i] > 1)
                        return false;
                }
                return true;
            }
        }
    }

    public readonly struct AnimationSourcePoseSample
    {
        public AnimationSourcePoseSample(
            AnimationSelectionFrame selection,
            AnimationFootFeatureSample leftFootFeatures,
            AnimationFootFeatureSample rightFootFeatures,
            bool hasFootFeatures)
        {
            Selection = selection;
            LeftFootFeatures = leftFootFeatures;
            RightFootFeatures = rightFootFeatures;
            HasFootFeatures = hasFootFeatures;
            if (!IsValid)
                throw new ArgumentException("Animation source pose sample is invalid.");
        }

        public AnimationSelectionFrame Selection { get; }
        public AnimationFootFeatureSample LeftFootFeatures { get; }
        public AnimationFootFeatureSample RightFootFeatures { get; }
        public bool HasFootFeatures { get; }
        public bool IsValid => Selection.IsValid && (HasFootFeatures
            ? LeftFootFeatures.IsValid && RightFootFeatures.IsValid
            : !LeftFootFeatures.IsValid && !RightFootFeatures.IsValid);
    }

    public enum PlayerSourceUsageKind : byte
    {
        Sample = 1,
        HandoffReference = 2,
        Retained = 3,
        Release = 4
    }

    public readonly struct PlayerSourceUsageFrame
    {
        public const string SchemaVersion = "player-source-usage-frame/v1";

        public PlayerSourceUsageFrame(
            PoseNodeId playerNodeId,
            AnimationPoseSourceId sourceId,
            PlayerSourceUsageKind kind,
            ulong completionIdentity)
        {
            if (!playerNodeId.IsValid || !sourceId.IsValid ||
                !Enum.IsDefined(typeof(PlayerSourceUsageKind), kind) || completionIdentity == 0)
                throw new ArgumentException("Player source usage is invalid.");
            PlayerNodeId = playerNodeId;
            SourceId = sourceId;
            Kind = kind;
            CompletionIdentity = completionIdentity;
        }

        public PoseNodeId PlayerNodeId { get; }
        public AnimationPoseSourceId SourceId { get; }
        public PlayerSourceUsageKind Kind { get; }
        public ulong CompletionIdentity { get; }
    }

    internal readonly struct AnimationPlayerSourceSampleKey : IEquatable<AnimationPlayerSourceSampleKey>
    {
        internal AnimationPlayerSourceSampleKey(PoseNodeId playerNodeId, AnimationPoseSourceId sourceId)
        {
            if (!playerNodeId.IsValid || !sourceId.IsValid)
                throw new ArgumentException("Animation Player source sample key is invalid.");
            PlayerNodeId = playerNodeId;
            SourceId = sourceId;
        }

        internal PoseNodeId PlayerNodeId { get; }
        internal AnimationPoseSourceId SourceId { get; }

        public bool Equals(AnimationPlayerSourceSampleKey other) =>
            PlayerNodeId.Equals(other.PlayerNodeId) && SourceId.Equals(other.SourceId);
        public override bool Equals(object obj) => obj is AnimationPlayerSourceSampleKey other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(PlayerNodeId, SourceId);
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
