using System;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public readonly struct PresentationPoseSourceProviderId :
        IEquatable<PresentationPoseSourceProviderId>,
        IComparable<PresentationPoseSourceProviderId>
    {
        public PresentationPoseSourceProviderId(string value)
        {
            Value = string.IsNullOrWhiteSpace(value)
                ? throw new ArgumentException("Presentation Pose source provider id is required.", nameof(value))
                : value.Trim();
        }

        public string Value { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(Value);
        public int CompareTo(PresentationPoseSourceProviderId other) =>
            string.Compare(Value, other.Value, StringComparison.Ordinal);
        public bool Equals(PresentationPoseSourceProviderId other) =>
            string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) =>
            obj is PresentationPoseSourceProviderId other && Equals(other);
        public override int GetHashCode() =>
            StringComparer.Ordinal.GetHashCode(Value ?? string.Empty);
        public override string ToString() => Value ?? string.Empty;
        public static bool operator ==(
            PresentationPoseSourceProviderId left,
            PresentationPoseSourceProviderId right) => left.Equals(right);
        public static bool operator !=(
            PresentationPoseSourceProviderId left,
            PresentationPoseSourceProviderId right) => !left.Equals(right);
    }

    public readonly struct PresentationPoseSourceGeneration :
        IEquatable<PresentationPoseSourceGeneration>,
        IComparable<PresentationPoseSourceGeneration>
    {
        public PresentationPoseSourceGeneration(ulong value)
        {
            Value = value != 0
                ? value
                : throw new ArgumentOutOfRangeException(nameof(value));
        }

        public ulong Value { get; }
        public bool IsValid => Value != 0;
        public int CompareTo(PresentationPoseSourceGeneration other) =>
            Value.CompareTo(other.Value);
        public bool Equals(PresentationPoseSourceGeneration other) =>
            Value == other.Value;
        public override bool Equals(object obj) =>
            obj is PresentationPoseSourceGeneration other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => Value.ToString();
        public static bool operator ==(
            PresentationPoseSourceGeneration left,
            PresentationPoseSourceGeneration right) => left.Equals(right);
        public static bool operator !=(
            PresentationPoseSourceGeneration left,
            PresentationPoseSourceGeneration right) => !left.Equals(right);
    }

    public enum PresentationPoseSourceAvailability : byte
    {
        Pending = 1,
        Ready = 2,
        Invalid = 3
    }

    public enum PresentationPoseSourceFailureReason : byte
    {
        None = 0,
        ProviderUnavailable = 1,
        SourceBindingMissing = 2,
        SourceGenerationStale = 3,
        SampleInvalid = 4,
        BackendFailure = 5
    }

    public readonly struct PresentationPoseSampleTime
    {
        public PresentationPoseSampleTime(
            float sampleTime,
            double continuousTime,
            int cycle,
            bool loop,
            float timeScale)
        {
            SampleTime = sampleTime;
            ContinuousTime = continuousTime;
            Cycle = cycle;
            Loop = loop;
            TimeScale = timeScale;
            if (!IsValid)
                throw new ArgumentException("Presentation Pose sample time is invalid.");
        }

        public float SampleTime { get; }
        public double ContinuousTime { get; }
        public int Cycle { get; }
        public bool Loop { get; }
        public float TimeScale { get; }
        public bool IsValid =>
            float.IsFinite(SampleTime) &&
            SampleTime >= 0f &&
            double.IsFinite(ContinuousTime) &&
            ContinuousTime >= SampleTime &&
            Cycle >= 0 &&
            float.IsFinite(TimeScale) &&
            TimeScale >= 0f;
    }

    public sealed class PresentationPoseSourceSample
    {
        PresentationPoseSourceSample(
            PresentationPoseSourceProviderId providerId,
            PoseNodeId playerNodeId,
            PresentationPoseSourceIndex sourceIndex,
            AnimationPoseSourceKind sourceKind,
            int projectionDatabaseIndex,
            PresentationPoseSourceGeneration sourceGeneration,
            ulong sourcePoseContinuityIdentity,
            ulong frameSequence,
            PresentationPoseSourceAvailability availability,
            PresentationPoseSampleTime rawSample,
            PresentationPoseSampleTime effectiveSample,
            AnimationReadOnlyBuffer<ClipSamplePlan> clips,
            PresentationParameterPageId parameterPageId,
            AnimationReadOnlyBuffer<float> poseParameters,
            AnimationReadOnlyBuffer<byte> poseParameterAvailability,
            in AnimationFootFeatureSample leftFootFeatures,
            in AnimationFootFeatureSample rightFootFeatures,
            bool hasFootFeatures,
            PresentationPoseSourceFailureReason failureReason)
        {
            ProviderId = providerId;
            PlayerNodeId = playerNodeId;
            SourceIndex = sourceIndex;
            SourceKind = sourceKind;
            ProjectionDatabaseIndex = projectionDatabaseIndex;
            SourceGeneration = sourceGeneration;
            SourcePoseContinuityIdentity = sourcePoseContinuityIdentity;
            FrameSequence = frameSequence;
            Availability = availability;
            RawSample = rawSample;
            EffectiveSample = effectiveSample;
            Clips = clips;
            ParameterPageId = parameterPageId;
            PoseParameters = poseParameters;
            PoseParameterAvailability = poseParameterAvailability;
            m_LeftFootFeatures = leftFootFeatures;
            m_RightFootFeatures = rightFootFeatures;
            HasFootFeatures = hasFootFeatures;
            FailureReason = failureReason;
            if (!IsValid)
                throw new ArgumentException("Presentation Pose source sample is invalid.");
        }

        readonly AnimationFootFeatureSample m_LeftFootFeatures;
        readonly AnimationFootFeatureSample m_RightFootFeatures;
        public PresentationPoseSourceProviderId ProviderId { get; }
        public PoseNodeId PlayerNodeId { get; }
        public PresentationPoseSourceIndex SourceIndex { get; }
        public AnimationPoseSourceKind SourceKind { get; }
        public int ProjectionDatabaseIndex { get; }
        public PresentationPoseSourceGeneration SourceGeneration { get; }
        public ulong SourcePoseContinuityIdentity { get; }
        public ulong FrameSequence { get; }
        public PresentationPoseSourceAvailability Availability { get; }
        public PresentationPoseSampleTime RawSample { get; }
        public PresentationPoseSampleTime EffectiveSample { get; }
        public AnimationReadOnlyBuffer<ClipSamplePlan> Clips { get; }
        public PresentationParameterPageId ParameterPageId { get; }
        public AnimationReadOnlyBuffer<float> PoseParameters { get; }
        public AnimationReadOnlyBuffer<byte> PoseParameterAvailability { get; }
        public ref readonly AnimationFootFeatureSample LeftFootFeatures =>
            ref m_LeftFootFeatures;
        public ref readonly AnimationFootFeatureSample RightFootFeatures =>
            ref m_RightFootFeatures;
        public bool HasFootFeatures { get; }
        public PresentationPoseSourceFailureReason FailureReason { get; }

        public bool IsValid
        {
            get
            {
                if (!ProviderId.IsValid ||
                    !PlayerNodeId.IsValid ||
                    !SourceIndex.IsValid ||
                    SourceKind != AnimationPoseSourceKind.Clip &&
                    SourceKind != AnimationPoseSourceKind.BlendSpace &&
                    SourceKind != AnimationPoseSourceKind.MotionMatching ||
                    !SourceGeneration.IsValid ||
                    SourcePoseContinuityIdentity == 0 ||
                    FrameSequence == 0 ||
                    (byte)Availability < (byte)PresentationPoseSourceAvailability.Pending ||
                    (byte)Availability > (byte)PresentationPoseSourceAvailability.Invalid)
                {
                    return false;
                }

                if (Availability == PresentationPoseSourceAvailability.Pending)
                    return ProjectionDatabaseIndex == -1 &&
                           HasEmptyPayload &&
                           FailureReason == PresentationPoseSourceFailureReason.None;
                if (Availability == PresentationPoseSourceAvailability.Invalid)
                    return ProjectionDatabaseIndex == -1 &&
                           HasEmptyPayload &&
                           FailureReason != PresentationPoseSourceFailureReason.None &&
                           (byte)FailureReason <= (byte)PresentationPoseSourceFailureReason.BackendFailure;
                if ((SourceKind == AnimationPoseSourceKind.MotionMatching
                        ? ProjectionDatabaseIndex < 0
                        : ProjectionDatabaseIndex != -1) ||
                    FailureReason != PresentationPoseSourceFailureReason.None ||
                    !RawSample.IsValid ||
                    !EffectiveSample.IsValid ||
                    Clips.Count == 0 ||
                    !ParameterPageId.IsValid ||
                    PoseParameters.Count == 0 ||
                    PoseParameters.Count != PoseParameterAvailability.Count)
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
                    if (!float.IsFinite(PoseParameters[i]) ||
                        PoseParameterAvailability[i] > 1)
                    {
                        return false;
                    }
                }
                return HasFootFeatures
                    ? LeftFootFeatures.IsValid && RightFootFeatures.IsValid
                    : !LeftFootFeatures.IsValid && !RightFootFeatures.IsValid;
            }
        }

        bool HasEmptyPayload =>
            !RawSample.IsValid &&
            !EffectiveSample.IsValid &&
            Clips.Count == 0 &&
            !ParameterPageId.IsValid &&
            PoseParameters.Count == 0 &&
            PoseParameterAvailability.Count == 0 &&
            !LeftFootFeatures.IsValid &&
            !RightFootFeatures.IsValid &&
            !HasFootFeatures;

        public static PresentationPoseSourceSample Pending(
            PresentationPoseSourceProviderId providerId,
            PoseNodeId playerNodeId,
            PresentationPoseSourceIndex sourceIndex,
            AnimationPoseSourceKind sourceKind,
            PresentationPoseSourceGeneration sourceGeneration,
            ulong sourcePoseContinuityIdentity,
            ulong frameSequence)
        {
            return Empty(
                providerId,
                playerNodeId,
                sourceIndex,
                sourceKind,
                sourceGeneration,
                sourcePoseContinuityIdentity,
                frameSequence,
                PresentationPoseSourceAvailability.Pending,
                PresentationPoseSourceFailureReason.None);
        }

        public static PresentationPoseSourceSample Ready(
            PresentationPoseSourceProviderId providerId,
            PoseNodeId playerNodeId,
            PresentationPoseSourceIndex sourceIndex,
            AnimationPoseSourceKind sourceKind,
            int projectionDatabaseIndex,
            PresentationPoseSourceGeneration sourceGeneration,
            ulong sourcePoseContinuityIdentity,
            ulong frameSequence,
            PresentationPoseSampleTime rawSample,
            PresentationPoseSampleTime effectiveSample,
            AnimationReadOnlyBuffer<ClipSamplePlan> clips,
            PresentationParameterPageId parameterPageId,
            AnimationReadOnlyBuffer<float> poseParameters,
            AnimationReadOnlyBuffer<byte> poseParameterAvailability,
            in AnimationFootFeatureSample leftFootFeatures,
            in AnimationFootFeatureSample rightFootFeatures,
            bool hasFootFeatures)
        {
            return new PresentationPoseSourceSample(
                providerId,
                playerNodeId,
                sourceIndex,
                sourceKind,
                projectionDatabaseIndex,
                sourceGeneration,
                sourcePoseContinuityIdentity,
                frameSequence,
                PresentationPoseSourceAvailability.Ready,
                rawSample,
                effectiveSample,
                clips,
                parameterPageId,
                poseParameters,
                poseParameterAvailability,
                in leftFootFeatures,
                in rightFootFeatures,
                hasFootFeatures,
                PresentationPoseSourceFailureReason.None);
        }

        public static PresentationPoseSourceSample Invalid(
            PresentationPoseSourceProviderId providerId,
            PoseNodeId playerNodeId,
            PresentationPoseSourceIndex sourceIndex,
            AnimationPoseSourceKind sourceKind,
            PresentationPoseSourceGeneration sourceGeneration,
            ulong sourcePoseContinuityIdentity,
            ulong frameSequence,
            PresentationPoseSourceFailureReason failureReason)
        {
            if (failureReason == PresentationPoseSourceFailureReason.None)
                throw new ArgumentOutOfRangeException(nameof(failureReason));
            return Empty(
                providerId,
                playerNodeId,
                sourceIndex,
                sourceKind,
                sourceGeneration,
                sourcePoseContinuityIdentity,
                frameSequence,
                PresentationPoseSourceAvailability.Invalid,
                failureReason);
        }

        static PresentationPoseSourceSample Empty(
            PresentationPoseSourceProviderId providerId,
            PoseNodeId playerNodeId,
            PresentationPoseSourceIndex sourceIndex,
            AnimationPoseSourceKind sourceKind,
            PresentationPoseSourceGeneration sourceGeneration,
            ulong sourcePoseContinuityIdentity,
            ulong frameSequence,
            PresentationPoseSourceAvailability availability,
            PresentationPoseSourceFailureReason failureReason)
        {
            AnimationFootFeatureSample emptyLeft = default;
            AnimationFootFeatureSample emptyRight = default;
            return new PresentationPoseSourceSample(
                providerId,
                playerNodeId,
                sourceIndex,
                sourceKind,
                -1,
                sourceGeneration,
                sourcePoseContinuityIdentity,
                frameSequence,
                availability,
                default,
                default,
                default,
                default,
                default,
                default,
                in emptyLeft,
                in emptyRight,
                false,
                failureReason);
        }
    }
}
