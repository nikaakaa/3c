using System;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public enum ActionAnimationPlaybackCommandKind : byte
    {
        Select = 1,
        Sample = 2,
        Complete = 3,
        Release = 4,
        ProjectedSample = 5,
        Withdraw = 6
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

    public readonly struct ActionProjectedSample
    {
        public ActionProjectedSample(ulong localLogicTick, ulong presentationFrame,
            AnimationClip authoringClipIdentity,
            PresentationPoseSampleTime time, float producerWeight)
        {
            LocalLogicTick = localLogicTick;
            PresentationFrame = presentationFrame;
            AuthoringClipIdentity = authoringClipIdentity;
            Time = time;
            ProducerWeight = producerWeight;
            if (!IsValid)
                throw new ArgumentException("Action projected sample is invalid.");
        }

        public ulong LocalLogicTick { get; }
        public ulong PresentationFrame { get; }
        public AnimationClip AuthoringClipIdentity { get; }
        public PresentationPoseSampleTime Time { get; }
        public float ProducerWeight { get; }
        public bool IsValid => LocalLogicTick != 0 && PresentationFrame != 0 &&
            AuthoringClipIdentity && Time.IsValid && float.IsFinite(ProducerWeight) &&
            ProducerWeight > 0f && ProducerWeight <= 1f;
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
            bool hasCommittedRawSample,
            ActionProjectedSample projectedSample = default)
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
            ProjectedSample = projectedSample;
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
        public ActionProjectedSample ProjectedSample { get; }

        public bool IsValid =>
            (byte)Kind >= (byte)ActionAnimationPlaybackCommandKind.Select &&
            (byte)Kind <= (byte)ActionAnimationPlaybackCommandKind.Withdraw &&
            (Kind == ActionAnimationPlaybackCommandKind.ProjectedSample ? !EventId.IsValid : EventId.IsValid) &&
            LocalLogicTick != 0 &&
            PlaybackId.IsValid &&
            ActionInstanceId != 0 &&
            AnimationChannelId.IsValid &&
            PlaybackId.ProducerId.MatchesProgramProducerIdentity(ProgramProducerId) &&
            (Kind == ActionAnimationPlaybackCommandKind.Sample
                ? HasCommittedRawSample && !ProjectedSample.IsValid &&
                  CommittedRawSample.IsValid &&
                  CommittedRawSample.EventId.Equals(EventId) &&
                  CommittedRawSample.LocalLogicTick == LocalLogicTick
                : Kind == ActionAnimationPlaybackCommandKind.ProjectedSample
                    ? !HasCommittedRawSample && ProjectedSample.IsValid &&
                      ProjectedSample.LocalLogicTick == LocalLogicTick
                    : !HasCommittedRawSample && !ProjectedSample.IsValid);

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

        public static ActionAnimationPlaybackCommand PresentSample(
            AnimationPlaybackId playbackId, ulong actionInstanceId, AnimationChannelId animationChannelId,
            string programProducerId, in ActionProjectedSample sample) =>
            new ActionAnimationPlaybackCommand(ActionAnimationPlaybackCommandKind.ProjectedSample,
                default, sample.LocalLogicTick, playbackId, actionInstanceId,
                animationChannelId, programProducerId, default, false, sample);

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

        public static ActionAnimationPlaybackCommand Withdraw(
            EventId eventId,
            ulong localLogicTick,
            AnimationPlaybackId playbackId,
            ulong actionInstanceId,
            AnimationChannelId animationChannelId,
            string programProducerId) => Terminal(
                ActionAnimationPlaybackCommandKind.Withdraw, eventId, localLogicTick, playbackId,
                actionInstanceId, animationChannelId, programProducerId);

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
}
