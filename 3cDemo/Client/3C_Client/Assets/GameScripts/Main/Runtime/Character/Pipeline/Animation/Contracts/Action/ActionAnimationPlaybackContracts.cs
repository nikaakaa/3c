using System;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public enum ActionAnimationPlaybackCommandKind : byte
    {
        Select = 1,
        Sample = 2,
        Complete = 3,
        Release = 4
    }

    public enum ActionAnimationPlaybackLifecyclePhase : byte
    {
        PendingFirstSample = 1,
        Selected = 2,
        Retained = 3,
        RetirementPermitted = 4,
        Retired = 5
    }

    public readonly struct ActionCommittedRawSample
    {
        public ActionCommittedRawSample(
            EventId eventId,
            ulong localLogicTick,
            ulong committedSequence,
            float visualTime,
            double continuousVisualTime,
            int cycle,
            bool loop,
            float visualTimeScale,
            float producerWeight)
        {
            EventId = eventId;
            LocalLogicTick = localLogicTick;
            CommittedSequence = committedSequence;
            VisualTime = visualTime;
            ContinuousVisualTime = continuousVisualTime;
            Cycle = cycle;
            Loop = loop;
            VisualTimeScale = visualTimeScale;
            ProducerWeight = producerWeight;
            if (!IsValid)
                throw new ArgumentException("Action committed raw sample is invalid.");
        }

        public EventId EventId { get; }
        public ulong LocalLogicTick { get; }
        public ulong CommittedSequence { get; }
        public float VisualTime { get; }
        public double ContinuousVisualTime { get; }
        public int Cycle { get; }
        public bool Loop { get; }
        public float VisualTimeScale { get; }
        public float ProducerWeight { get; }
        public bool IsValid =>
            EventId.IsValid &&
            LocalLogicTick != 0 &&
            CommittedSequence != 0 &&
            float.IsFinite(VisualTime) &&
            VisualTime >= 0f &&
            double.IsFinite(ContinuousVisualTime) &&
            ContinuousVisualTime >= VisualTime &&
            Cycle >= 0 &&
            float.IsFinite(VisualTimeScale) &&
            VisualTimeScale >= 0f &&
            float.IsFinite(ProducerWeight) &&
            ProducerWeight >= 0f &&
            ProducerWeight <= 1f;
    }

    public readonly struct ActionAnimationPlaybackCommand
    {
        ActionAnimationPlaybackCommand(
            ActionAnimationPlaybackCommandKind kind,
            EventId eventId,
            ulong localLogicTick,
            AnimationPlaybackId playbackId,
            ulong actionInstanceId,
            AnimationChannelId animationChannelId,
            string programProducerId,
            ActionCommittedRawSample committedRawSample,
            bool hasCommittedRawSample)
        {
            Kind = kind;
            EventId = eventId;
            LocalLogicTick = localLogicTick;
            PlaybackId = playbackId;
            ActionInstanceId = actionInstanceId;
            AnimationChannelId = animationChannelId;
            ProgramProducerId = programProducerId?.Trim() ?? string.Empty;
            CommittedRawSample = committedRawSample;
            HasCommittedRawSample = hasCommittedRawSample;
            if (!IsValid)
                throw new ArgumentException("Action animation playback command is invalid.");
        }

        public ActionAnimationPlaybackCommandKind Kind { get; }
        public EventId EventId { get; }
        public ulong LocalLogicTick { get; }
        public AnimationPlaybackId PlaybackId { get; }
        public ulong ActionInstanceId { get; }
        public AnimationChannelId AnimationChannelId { get; }
        public string ProgramProducerId { get; }
        public ulong Generation => PlaybackId.Generation;
        public ActionCommittedRawSample CommittedRawSample { get; }
        public bool HasCommittedRawSample { get; }

        public bool IsValid =>
            (byte)Kind >= (byte)ActionAnimationPlaybackCommandKind.Select &&
            (byte)Kind <= (byte)ActionAnimationPlaybackCommandKind.Release &&
            EventId.IsValid &&
            LocalLogicTick != 0 &&
            PlaybackId.IsValid &&
            ActionInstanceId != 0 &&
            AnimationChannelId.IsValid &&
            string.Equals(
                ProgramProducerId,
                PlaybackId.ProducerId.ProgramProducerIdentity,
                StringComparison.Ordinal) &&
            (Kind == ActionAnimationPlaybackCommandKind.Sample
                ? HasCommittedRawSample &&
                  CommittedRawSample.IsValid &&
                  CommittedRawSample.EventId.Equals(EventId) &&
                  CommittedRawSample.LocalLogicTick == LocalLogicTick
                : !HasCommittedRawSample);

        public static ActionAnimationPlaybackCommand Select(
            EventId eventId,
            ulong localLogicTick,
            AnimationPlaybackId playbackId,
            ulong actionInstanceId,
            AnimationChannelId animationChannelId,
            string programProducerId)
        {
            return new ActionAnimationPlaybackCommand(
                ActionAnimationPlaybackCommandKind.Select,
                eventId,
                localLogicTick,
                playbackId,
                actionInstanceId,
                animationChannelId,
                programProducerId,
                default,
                false);
        }

        public static ActionAnimationPlaybackCommand Sample(
            AnimationPlaybackId playbackId,
            ulong actionInstanceId,
            AnimationChannelId animationChannelId,
            string programProducerId,
            ActionCommittedRawSample committedRawSample)
        {
            return new ActionAnimationPlaybackCommand(
                ActionAnimationPlaybackCommandKind.Sample,
                committedRawSample.EventId,
                committedRawSample.LocalLogicTick,
                playbackId,
                actionInstanceId,
                animationChannelId,
                programProducerId,
                committedRawSample,
                true);
        }

        public static ActionAnimationPlaybackCommand Complete(
            EventId eventId,
            ulong localLogicTick,
            AnimationPlaybackId playbackId,
            ulong actionInstanceId,
            AnimationChannelId animationChannelId,
            string programProducerId)
        {
            return Terminal(
                ActionAnimationPlaybackCommandKind.Complete,
                eventId,
                localLogicTick,
                playbackId,
                actionInstanceId,
                animationChannelId,
                programProducerId);
        }

        public static ActionAnimationPlaybackCommand Release(
            EventId eventId,
            ulong localLogicTick,
            AnimationPlaybackId playbackId,
            ulong actionInstanceId,
            AnimationChannelId animationChannelId,
            string programProducerId)
        {
            return Terminal(
                ActionAnimationPlaybackCommandKind.Release,
                eventId,
                localLogicTick,
                playbackId,
                actionInstanceId,
                animationChannelId,
                programProducerId);
        }

        static ActionAnimationPlaybackCommand Terminal(
            ActionAnimationPlaybackCommandKind kind,
            EventId eventId,
            ulong localLogicTick,
            AnimationPlaybackId playbackId,
            ulong actionInstanceId,
            AnimationChannelId animationChannelId,
            string programProducerId)
        {
            return new ActionAnimationPlaybackCommand(
                kind,
                eventId,
                localLogicTick,
                playbackId,
                actionInstanceId,
                animationChannelId,
                programProducerId,
                default,
                false);
        }
    }

    public sealed class ActionAnimationPlaybackFrame
    {
        public const string SchemaVersion = "action-animation-playback-frame/v2";

        public ActionAnimationPlaybackFrame(
            EventId latestEventId,
            AnimationPlaybackId playbackId,
            ulong actionInstanceId,
            ulong sourcePoseContinuityIdentity,
            AnimationChannelId animationChannelId,
            string programProducerId,
            ulong latestCommandSequence,
            ActionAnimationPlaybackLifecyclePhase lifecyclePhase,
            ActionCommittedRawSample latestCommittedRawSample,
            PresentationPoseSampleTime projectedSampleTime,
            PresentationPoseSampleTime effectiveSampleTime,
            string previousMarkerId,
            string nextMarkerId,
            float markerSegmentFraction,
            bool markerMapped,
            bool markerRebased,
            bool retentionProjection,
            AnimationReadOnlyBuffer<ClipSamplePlan> clips,
            PresentationParameterPageId parameterPageId,
            AnimationReadOnlyBuffer<float> poseParameters,
            AnimationReadOnlyBuffer<byte> poseParameterAvailability,
            AnimationFootFeatureSample leftFootFeatures,
            AnimationFootFeatureSample rightFootFeatures)
        {
            LatestEventId = latestEventId;
            PlaybackId = playbackId;
            ActionInstanceId = actionInstanceId;
            SourcePoseContinuityIdentity = sourcePoseContinuityIdentity;
            AnimationChannelId = animationChannelId;
            ProgramProducerId = programProducerId?.Trim() ?? string.Empty;
            LatestCommandSequence = latestCommandSequence;
            LifecyclePhase = lifecyclePhase;
            LatestCommittedRawSample = latestCommittedRawSample;
            ProjectedSampleTime = projectedSampleTime;
            EffectiveSampleTime = effectiveSampleTime;
            PreviousMarkerId = previousMarkerId?.Trim() ?? string.Empty;
            NextMarkerId = nextMarkerId?.Trim() ?? string.Empty;
            MarkerSegmentFraction = markerSegmentFraction;
            MarkerMapped = markerMapped;
            MarkerRebased = markerRebased;
            RetentionProjection = retentionProjection;
            Clips = clips;
            ParameterPageId = parameterPageId;
            PoseParameters = poseParameters;
            PoseParameterAvailability = poseParameterAvailability;
            LeftFootFeatures = leftFootFeatures;
            RightFootFeatures = rightFootFeatures;
            if (!IsValid)
                throw new ArgumentException("Action animation playback frame is invalid.");
        }

        public EventId LatestEventId { get; }
        public AnimationPlaybackId PlaybackId { get; }
        public ulong ActionInstanceId { get; }
        public ulong SourcePoseContinuityIdentity { get; }
        public AnimationChannelId AnimationChannelId { get; }
        public string ProgramProducerId { get; }
        public ulong LatestCommandSequence { get; }
        public ActionAnimationPlaybackLifecyclePhase LifecyclePhase { get; }
        public ActionCommittedRawSample LatestCommittedRawSample { get; }
        public PresentationPoseSampleTime ProjectedSampleTime { get; }
        public PresentationPoseSampleTime EffectiveSampleTime { get; }
        public string PreviousMarkerId { get; }
        public string NextMarkerId { get; }
        public float MarkerSegmentFraction { get; }
        public bool MarkerMapped { get; }
        public bool MarkerRebased { get; }
        public bool RetentionProjection { get; }
        public AnimationReadOnlyBuffer<ClipSamplePlan> Clips { get; }
        public PresentationParameterPageId ParameterPageId { get; }
        public AnimationReadOnlyBuffer<float> PoseParameters { get; }
        public AnimationReadOnlyBuffer<byte> PoseParameterAvailability { get; }
        public AnimationFootFeatureSample LeftFootFeatures { get; }
        public AnimationFootFeatureSample RightFootFeatures { get; }

        public bool IsValid
        {
            get
            {
                if (!LatestEventId.IsValid ||
                    !PlaybackId.IsValid ||
                    ActionInstanceId == 0 ||
                    SourcePoseContinuityIdentity == 0 ||
                    !AnimationChannelId.IsValid ||
                    !string.Equals(
                        ProgramProducerId,
                        PlaybackId.ProducerId.ProgramProducerIdentity,
                        StringComparison.Ordinal) ||
                    LatestCommandSequence == 0 ||
                    (byte)LifecyclePhase < (byte)ActionAnimationPlaybackLifecyclePhase.PendingFirstSample ||
                    (byte)LifecyclePhase > (byte)ActionAnimationPlaybackLifecyclePhase.Retired ||
                    LifecyclePhase == ActionAnimationPlaybackLifecyclePhase.PendingFirstSample ||
                    LifecyclePhase == ActionAnimationPlaybackLifecyclePhase.Retired ||
                    !LatestCommittedRawSample.IsValid ||
                    !ProjectedSampleTime.IsValid ||
                    !EffectiveSampleTime.IsValid ||
                    !float.IsFinite(MarkerSegmentFraction) ||
                    MarkerSegmentFraction < 0f ||
                    MarkerSegmentFraction > 1f ||
                    (string.IsNullOrEmpty(PreviousMarkerId) !=
                     string.IsNullOrEmpty(NextMarkerId)) ||
                    Clips.Count == 0 ||
                    !ParameterPageId.IsValid ||
                    PoseParameters.Count == 0 ||
                    PoseParameters.Count != PoseParameterAvailability.Count ||
                    !LeftFootFeatures.IsValid ||
                    !RightFootFeatures.IsValid)
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

                return true;
            }
        }
    }
}
