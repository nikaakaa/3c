using System;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    enum AnimationFootMotionSide : byte
    {
        Left = 1,
        Right = 2
    }

    public readonly struct AnimationFootMotionLandingEventReference
    {
        internal AnimationFootMotionLandingEventReference(
            ulong identity,
            int ordinal,
            int sourceCycle,
            float normalizedTime,
            float distance,
            Vector3 rootLocalLanding)
        {
            if (identity == 0 || ordinal <= 0 || sourceCycle < 0 ||
                !float.IsFinite(normalizedTime) || normalizedTime < 0f || normalizedTime > 1f ||
                !float.IsFinite(distance) || distance < 0f || !Finite(rootLocalLanding))
            {
                throw new ArgumentException("Foot Motion Landing Event reference is invalid.");
            }
            Identity = identity;
            Ordinal = ordinal;
            SourceCycle = sourceCycle;
            NormalizedTime = normalizedTime;
            Distance = distance;
            RootLocalLanding = rootLocalLanding;
            m_IsSpecified = 1;
        }

        readonly byte m_IsSpecified;
        public ulong Identity { get; }
        public int Ordinal { get; }
        public int SourceCycle { get; }
        public float NormalizedTime { get; }
        public float Distance { get; }
        public Vector3 RootLocalLanding { get; }
        public bool IsValid => m_IsSpecified != 0;

        static bool Finite(Vector3 value) =>
            float.IsFinite(value.x) &&
            float.IsFinite(value.y) &&
            float.IsFinite(value.z);
    }

    public readonly struct AnimationFootMotionStep
    {
        internal AnimationFootMotionStep(
            AnimationFootMotionLandingEventReference contactEvent,
            int frameSourceCycle,
            ulong contributionContinuityIdentity)
        {
            if (!contactEvent.IsValid || frameSourceCycle < 0 ||
                contributionContinuityIdentity == 0)
            {
                throw new ArgumentException("Foot Motion Contact Step is invalid.");
            }
            EventOrdinal = contactEvent.Ordinal;
            SourceLandingCycleOffset = contactEvent.SourceCycle - frameSourceCycle;
            SourceSampleCycle = contactEvent.SourceCycle;
            ContributionContinuityIdentity = contributionContinuityIdentity;
            LandingEventIdentity = contactEvent.Identity;
            TimeToLandingSeconds = 0f;
            Distance = contactEvent.Distance;
            RootLocalLanding = contactEvent.RootLocalLanding;
            EventPhase = 1f;
            LiftOffPhase = 0f;
            ApproachContactPhase = 1f;
            LandingPhase = 1f;
            IsPreSwing = false;
            IsSwing = false;
            m_IsSpecified = 1;
        }

        internal AnimationFootMotionStep(
            AnimationFootStepObservationSample observation,
            AnimationFootMotionLandingEventReference previousLandingEvent,
            AnimationFootMotionLandingEventReference nextLandingEvent,
            int frameSourceCycle,
            float frameNormalizedTime,
            float sourceDurationSeconds,
            ulong contributionContinuityIdentity)
        {
            if (!observation.IsValid || !nextLandingEvent.IsValid ||
                frameSourceCycle < 0 ||
                !float.IsFinite(frameNormalizedTime) || frameNormalizedTime < 0f || frameNormalizedTime > 1f ||
                !float.IsFinite(sourceDurationSeconds) || sourceDurationSeconds <= 0f ||
                contributionContinuityIdentity == 0)
            {
                throw new ArgumentException("Foot Motion Step is invalid.");
            }
            double current = frameSourceCycle + frameNormalizedTime;
            double next = nextLandingEvent.SourceCycle +
                          nextLandingEvent.NormalizedTime;
            double previous = previousLandingEvent.IsValid
                ? previousLandingEvent.SourceCycle +
                  previousLandingEvent.NormalizedTime
                : current;
            float progress = next > previous + 0.000001d
                ? Mathf.Clamp01((float)((current - previous) / (next - previous)))
                : 0f;
            EventOrdinal = nextLandingEvent.Ordinal;
            SourceLandingCycleOffset = nextLandingEvent.SourceCycle - frameSourceCycle;
            SourceSampleCycle = nextLandingEvent.SourceCycle;
            ContributionContinuityIdentity = contributionContinuityIdentity;
            LandingEventIdentity = nextLandingEvent.Identity;
            TimeToLandingSeconds = observation.TimeToLandingSeconds;
            Distance = observation.Distance;
            RootLocalLanding = nextLandingEvent.RootLocalLanding;
            EventPhase = progress;
            LiftOffPhase = 0f;
            ApproachContactPhase = 1f;
            LandingPhase = 1f;
            bool hasFutureLanding = observation.TimeToLandingSeconds > 0.000001f;
            bool contactActive = observation.Contact > 0.0001f ||
                                 observation.LockMode !=
                                 AnimationFootStepObservationLockMode.Unlocked;
            IsPreSwing = hasFutureLanding && contactActive;
            IsSwing = hasFutureLanding && !contactActive;
            m_IsSpecified = 1;
        }

        readonly byte m_IsSpecified;
        public int EventOrdinal { get; }
        public int SourceLandingCycleOffset { get; }
        public int SourceSampleCycle { get; }
        public ulong ContributionContinuityIdentity { get; }
        public ulong LandingEventIdentity { get; }
        public float TimeToLandingSeconds { get; }
        public float Distance { get; }
        public Vector3 RootLocalLanding { get; }
        public float EventPhase { get; }
        public float LiftOffPhase { get; }
        public float ApproachContactPhase { get; }
        public float LandingPhase { get; }
        public float Confidence => IsValid ? 1f : 0f;
        public bool IsValid => m_IsSpecified != 0;
        public bool IsAuthoritative => IsValid;
        public bool HasConsistentLandingEventIdentity => IsValid;
        public bool IsPreSwing { get; }
        public bool IsSwing { get; }
    }

    [Serializable]
    public struct AnimationFootStepLandingEvent
    {
        [SerializeField] float m_NormalizedTime;
        [SerializeField] int m_Ordinal;
        [SerializeField] int m_CycleOffset;
        [SerializeField] float m_Distance;
        [SerializeField] Vector3 m_RootLocalLanding;

        public AnimationFootStepLandingEvent(
            float normalizedTime,
            int ordinal,
            int cycleOffset,
            float distance,
            Vector3 rootLocalLanding)
        {
            m_NormalizedTime = normalizedTime;
            m_Ordinal = ordinal;
            m_CycleOffset = cycleOffset;
            m_Distance = distance;
            m_RootLocalLanding = rootLocalLanding;
            RequireValid();
        }

        public float NormalizedTime => m_NormalizedTime;
        public int Ordinal => m_Ordinal;
        public int CycleOffset => m_CycleOffset;
        public float Distance => m_Distance;
        public Vector3 RootLocalLanding => m_RootLocalLanding;

        public void RequireValid()
        {
            if (!float.IsFinite(m_NormalizedTime) ||
                m_NormalizedTime < 0f ||
                m_NormalizedTime > 1f ||
                m_Ordinal <= 0 ||
                m_CycleOffset < 0 ||
                !float.IsFinite(m_Distance) ||
                m_Distance < 0f ||
                !Finite(m_RootLocalLanding))
            {
                throw new InvalidOperationException(
                    "Foot Step Landing Event is invalid.");
            }
        }

        static bool Finite(Vector3 value) =>
            float.IsFinite(value.x) &&
            float.IsFinite(value.y) &&
            float.IsFinite(value.z);
    }

    [Serializable]
    public sealed class AnimationFootStepLandingEventTable
    {
        [SerializeField] AnimationFootStepLandingEvent[] m_Events =
            Array.Empty<AnimationFootStepLandingEvent>();

        public AnimationFootStepLandingEventTable(
            AnimationFootStepLandingEvent[] events)
        {
            m_Events = events == null
                ? throw new ArgumentNullException(nameof(events))
                : (AnimationFootStepLandingEvent[])events.Clone();
            RequireValid();
        }

        public int Count => m_Events?.Length ?? 0;
        public AnimationFootStepLandingEvent EventAt(int index) => m_Events[index];

        internal AnimationFootMotionLandingEventReference ResolveNext(
            string sourceIdentity,
            ulong contributionContinuityIdentity,
            int sourceCycle,
            float normalizedTime,
            float sourceDurationSeconds,
            float formalTimeToLandingSeconds,
            AnimationFootMotionSide side) =>
            Resolve(
                sourceIdentity,
                contributionContinuityIdentity,
                sourceCycle,
                normalizedTime,
                sourceDurationSeconds,
                formalTimeToLandingSeconds,
                side,
                true);

        internal AnimationFootMotionLandingEventReference ResolveContact(
            string sourceIdentity,
            ulong contributionContinuityIdentity,
            int sourceCycle,
            float normalizedTime,
            float sourceDurationSeconds,
            AnimationFootMotionSide side) =>
            Resolve(
                sourceIdentity,
                contributionContinuityIdentity,
                sourceCycle,
                normalizedTime,
                sourceDurationSeconds,
                0f,
                side,
                false);

        internal AnimationFootMotionLandingEventReference ResolvePrevious(
            string sourceIdentity,
            ulong contributionContinuityIdentity,
            int sourceCycle,
            float normalizedTime,
            float sourceDurationSeconds,
            AnimationFootMotionSide side)
        {
            RequireValid();
            if (m_Events.Length == 0)
                return default;
            double current = sourceCycle + normalizedTime;
            double best = double.NegativeInfinity;
            int bestEventIndex = -1;
            int bestOccurrenceCycle = 0;
            for (int i = 0; i < m_Events.Length; i++)
            {
                AnimationFootStepLandingEvent footEvent = m_Events[i];
                for (int cycleDelta = -2; cycleDelta <= 1; cycleDelta++)
                {
                    int occurrenceCycle = checked(
                        sourceCycle + footEvent.CycleOffset + cycleDelta);
                    if (occurrenceCycle < 0)
                        continue;
                    double occurrence = occurrenceCycle + footEvent.NormalizedTime;
                    if (occurrence > current + 0.000001d || occurrence <= best)
                        continue;
                    best = occurrence;
                    bestEventIndex = i;
                    bestOccurrenceCycle = occurrenceCycle;
                }
            }
            if (bestEventIndex < 0)
                return default;
            AnimationFootStepLandingEvent selected = m_Events[bestEventIndex];
            return new AnimationFootMotionLandingEventReference(
                ResolveIdentity(
                    sourceIdentity,
                    contributionContinuityIdentity,
                    bestOccurrenceCycle,
                    selected.Ordinal,
                    side),
                selected.Ordinal,
                bestOccurrenceCycle,
                selected.NormalizedTime,
                selected.Distance,
                selected.RootLocalLanding);
        }

        AnimationFootMotionLandingEventReference Resolve(
            string sourceIdentity,
            ulong contributionContinuityIdentity,
            int sourceCycle,
            float normalizedTime,
            float sourceDurationSeconds,
            float formalTimeToLandingSeconds,
            AnimationFootMotionSide side,
            bool next)
        {
            RequireValid();
            if (string.IsNullOrWhiteSpace(sourceIdentity) ||
                contributionContinuityIdentity == 0 || sourceCycle < 0 ||
                !float.IsFinite(normalizedTime) || normalizedTime < 0f || normalizedTime > 1f ||
                !float.IsFinite(sourceDurationSeconds) || sourceDurationSeconds <= 0f ||
                !float.IsFinite(formalTimeToLandingSeconds) || formalTimeToLandingSeconds < 0f ||
                side != AnimationFootMotionSide.Left && side != AnimationFootMotionSide.Right)
            {
                throw new ArgumentException("Foot Motion Landing Event query is invalid.");
            }
            if (m_Events.Length == 0)
                return default;

            double current = sourceCycle + normalizedTime;
            double bestScore = double.PositiveInfinity;
            double bestDelta = double.PositiveInfinity;
            int bestEventIndex = -1;
            int bestOccurrenceCycle = 0;
            for (int i = 0; i < m_Events.Length; i++)
            {
                AnimationFootStepLandingEvent footEvent = m_Events[i];
                for (int cycleDelta = -1; cycleDelta <= 2; cycleDelta++)
                {
                    int occurrenceCycle = checked(
                        sourceCycle + footEvent.CycleOffset + cycleDelta);
                    if (occurrenceCycle < 0)
                        continue;
                    double occurrence = occurrenceCycle + footEvent.NormalizedTime;
                    double delta = occurrence - current;
                    if (next && delta < -0.000001d)
                        continue;
                    double score = next
                        ? Math.Abs(delta * sourceDurationSeconds - formalTimeToLandingSeconds)
                        : Math.Abs(delta * sourceDurationSeconds);
                    if (score > bestScore + 0.0000001d ||
                        Math.Abs(score - bestScore) <= 0.0000001d &&
                        delta >= bestDelta)
                    {
                        continue;
                    }
                    bestScore = score;
                    bestDelta = delta;
                    bestEventIndex = i;
                    bestOccurrenceCycle = occurrenceCycle;
                }
            }
            if (bestEventIndex < 0)
                return default;
            AnimationFootStepLandingEvent selected = m_Events[bestEventIndex];
            ulong identity = ResolveIdentity(
                sourceIdentity,
                contributionContinuityIdentity,
                bestOccurrenceCycle,
                selected.Ordinal,
                side);
            return new AnimationFootMotionLandingEventReference(
                identity,
                selected.Ordinal,
                bestOccurrenceCycle,
                selected.NormalizedTime,
                selected.Distance,
                selected.RootLocalLanding);
        }

        static ulong ResolveIdentity(
            string sourceIdentity,
            ulong contributionContinuityIdentity,
            int sourceCycle,
            int ordinal,
            AnimationFootMotionSide side)
        {
            const ulong offset = 14695981039346656037UL;
            const ulong prime = 1099511628211UL;
            ulong sourceHash = offset;
            string trimmed = sourceIdentity.Trim();
            for (int i = 0; i < trimmed.Length; i++)
                sourceHash = (sourceHash ^ trimmed[i]) * prime;
            ulong value = offset;
            value = (value ^ contributionContinuityIdentity) * prime;
            value = (value ^ sourceHash) * prime;
            value = (value ^ unchecked((ulong)(long)sourceCycle)) * prime;
            value = (value ^ (ulong)(uint)ordinal) * prime;
            value = (value ^ (ulong)side) * prime;
            return value == 0 ? 1UL : value;
        }

        public void RequireValid()
        {
            if (m_Events == null)
                throw new InvalidOperationException(
                    "Foot Step Landing Event table is missing.");
            for (int i = 0; i < m_Events.Length; i++)
            {
                m_Events[i].RequireValid();
                if (i > 0 &&
                    m_Events[i].NormalizedTime <=
                    m_Events[i - 1].NormalizedTime)
                {
                    throw new InvalidOperationException(
                        "Foot Step Landing Event table is unordered.");
                }
            }
        }
    }
}
