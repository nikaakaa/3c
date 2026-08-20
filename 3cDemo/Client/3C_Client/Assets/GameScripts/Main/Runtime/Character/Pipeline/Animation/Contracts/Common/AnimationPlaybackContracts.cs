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
        BlendSpace = 3,
        Clip = 4
    }

    public readonly struct AnimationPoseSourceId : IEquatable<AnimationPoseSourceId>
    {
        public AnimationPoseSourceId(
            AnimationPlaybackId playbackId,
            AnimationPoseSourceKind sourceKind,
            AnimationPoseSelectionGeneration selectionGeneration,
            ulong sourceActionInstanceId = 0)
        {
            if (!playbackId.IsValid ||
                sourceKind != AnimationPoseSourceKind.Timeline ||
                !selectionGeneration.IsValid)
            {
                throw new ArgumentException("Animation pose source identity is invalid.");
            }
            PlaybackId = playbackId;
            PresentationPoseSourceIndex = default;
            SourceKind = sourceKind;
            SelectionGeneration = selectionGeneration;
            SourceActionInstanceId = sourceActionInstanceId;
        }

        public AnimationPoseSourceId(
            PresentationPoseSourceIndex presentationPoseSourceIndex,
            AnimationPoseSourceKind sourceKind,
            AnimationPoseSelectionGeneration selectionGeneration)
        {
            if (!presentationPoseSourceIndex.IsValid ||
                sourceKind != AnimationPoseSourceKind.Clip &&
                sourceKind != AnimationPoseSourceKind.BlendSpace &&
                sourceKind != AnimationPoseSourceKind.MotionMatching ||
                !selectionGeneration.IsValid)
                throw new ArgumentException("Presentation pose source identity is invalid.");
            PlaybackId = default;
            PresentationPoseSourceIndex = presentationPoseSourceIndex;
            SourceKind = sourceKind;
            SelectionGeneration = selectionGeneration;
            SourceActionInstanceId = 0;
        }

        public AnimationPlaybackId PlaybackId { get; }
        public PresentationPoseSourceIndex PresentationPoseSourceIndex { get; }
        public AnimationPoseSourceKind SourceKind { get; }
        public AnimationPoseSelectionGeneration SelectionGeneration { get; }
        public ulong SourceActionInstanceId { get; }
        public bool IsValid => (byte)SourceKind >= (byte)AnimationPoseSourceKind.Timeline &&
                               (byte)SourceKind <= (byte)AnimationPoseSourceKind.Clip &&
                               SelectionGeneration.IsValid &&
                               (SourceKind == AnimationPoseSourceKind.Clip ||
                                SourceKind == AnimationPoseSourceKind.BlendSpace ||
                                SourceKind == AnimationPoseSourceKind.MotionMatching
                                    ? PresentationPoseSourceIndex.IsValid && !PlaybackId.IsValid
                                   : PlaybackId.IsValid && !PresentationPoseSourceIndex.IsValid) &&
                               (SourceKind == AnimationPoseSourceKind.Timeline ||
                                SourceActionInstanceId == 0);

        public bool Equals(AnimationPoseSourceId other) =>
            PlaybackId.Equals(other.PlaybackId) &&
            PresentationPoseSourceIndex.Equals(other.PresentationPoseSourceIndex) &&
            SourceKind == other.SourceKind &&
            SelectionGeneration == other.SelectionGeneration &&
            SourceActionInstanceId == other.SourceActionInstanceId;

        public override bool Equals(object obj) => obj is AnimationPoseSourceId other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = PlaybackId.GetHashCode();
                hash = hash * 397 ^ PresentationPoseSourceIndex.GetHashCode();
                hash = hash * 397 ^ (int)SourceKind;
                hash = hash * 397 ^ SelectionGeneration.GetHashCode();
                return hash * 397 ^ SourceActionInstanceId.GetHashCode();
            }
        }

        public override string ToString() =>
            SourceKind == AnimationPoseSourceKind.Clip ||
            SourceKind == AnimationPoseSourceKind.BlendSpace ||
            SourceKind == AnimationPoseSourceKind.MotionMatching
            ? $"{PresentationPoseSourceIndex}/{SourceKind}/{SelectionGeneration}"
            : SourceActionInstanceId == 0
                ? $"{PlaybackId}/{SourceKind}/{SelectionGeneration}"
                : $"{PlaybackId}/{SourceKind}/{SelectionGeneration}/ActionInstance:{SourceActionInstanceId}";
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
        public PoseDiscontinuityEndpoint(AnimationPoseSourceId sourceId)
        {
            if (!sourceId.IsValid)
                throw new ArgumentException("Pose Discontinuity endpoint is invalid.");
            SourceId = sourceId;
        }

        public AnimationPoseSourceId SourceId { get; }
        public bool IsValid => SourceId.IsValid;

        public bool Equals(PoseDiscontinuityEndpoint other) =>
            SourceId.Equals(other.SourceId);
        public override bool Equals(object obj) => obj is PoseDiscontinuityEndpoint other && Equals(other);
        public override int GetHashCode() => SourceId.GetHashCode();

        internal static PoseDiscontinuityEndpoint From(
            in AnimationPoseSampleRequest request) =>
                new PoseDiscontinuityEndpoint(request.SourceId);

        public static PoseDiscontinuityEndpoint From(
            in PresentationPoseSourceSample sample) =>
                new PoseDiscontinuityEndpoint(
                    new AnimationPoseSourceId(
                        sample.SourceIndex,
                        sample.SourceKind,
                        new AnimationPoseSelectionGeneration(
                            sample.SourceGeneration.Value)));
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
                               (byte)Reason >= (byte)PoseDiscontinuityReason.SourceIdentityChanged &&
                               (byte)Reason <= (byte)PoseDiscontinuityReason.Reset &&
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

    internal readonly struct AnimationPoseSampleRequest
    {
        internal AnimationPoseSampleRequest(
            AnimationPoseSourceId sourceId,
            ulong sourcePoseContinuityIdentity,
            ulong presentationRequestSequence,
            int sourceOwnerIndex,
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
            SourceId = sourceId;
            SourcePoseContinuityIdentity = sourcePoseContinuityIdentity;
            PresentationRequestSequence = presentationRequestSequence;
            SourceOwnerIndex = sourceOwnerIndex;
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
                throw new ArgumentException("Animation pose sample request is invalid.");
        }

        internal AnimationPoseSourceId SourceId { get; }
        internal ulong SourcePoseContinuityIdentity { get; }
        internal ulong PresentationRequestSequence { get; }
        internal int SourceOwnerIndex { get; }
        internal float VisualSampleTime { get; }
        internal double ContinuousVisualTime { get; }
        internal int Cycle { get; }
        internal bool Loop { get; }
        internal float VisualTimeScale { get; }
        internal AnimationReadOnlyBuffer<ClipSamplePlan> Clips { get; }
        internal PresentationParameterPageId ParameterPageId { get; }
        internal AnimationReadOnlyBuffer<float> PoseParameters { get; }
        internal AnimationReadOnlyBuffer<byte> PoseParameterAvailability { get; }

        internal bool IsValid
        {
            get
            {
                if (!SourceId.IsValid ||
                    SourcePoseContinuityIdentity == 0 || PresentationRequestSequence == 0 || SourceOwnerIndex < 0 ||
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

    internal sealed class AnimationResolvedPoseSourceSample
    {
        internal AnimationResolvedPoseSourceSample(
            AnimationPoseSampleRequest request,
            in AnimationFootFeatureSample leftFootFeatures,
            in AnimationFootFeatureSample rightFootFeatures,
            bool hasFootFeatures)
        {
            Request = request;
            m_LeftFootFeatures = leftFootFeatures;
            m_RightFootFeatures = rightFootFeatures;
            HasFootFeatures = hasFootFeatures;
            if (!IsValid)
                throw new ArgumentException("Animation source pose sample is invalid.");
        }

        readonly AnimationFootFeatureSample m_LeftFootFeatures;
        readonly AnimationFootFeatureSample m_RightFootFeatures;
        internal AnimationPoseSampleRequest Request { get; }
        internal ref readonly AnimationFootFeatureSample LeftFootFeatures =>
            ref m_LeftFootFeatures;
        internal ref readonly AnimationFootFeatureSample RightFootFeatures =>
            ref m_RightFootFeatures;
        internal bool HasFootFeatures { get; }
        internal bool IsValid => Request.IsValid && (HasFootFeatures
            ? LeftFootFeatures.IsValid && RightFootFeatures.IsValid
            : !LeftFootFeatures.IsValid && !RightFootFeatures.IsValid);
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

}
