using System;
using ThirdPersonCharacter.Pipeline.Presentation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public enum AnimationFootMotionEventPhase : byte
    {
        Unavailable = 0,
        PreSwing = 1,
        Swing = 2,
        ApproachContact = 3,
        Contact = 4
    }

    public readonly struct AnimationFootMotionEventOccurrence
    {
        internal AnimationFootMotionEventOccurrence(
            int ordinal,
            int landingCycle,
            float normalizedTime,
            float distance,
            Vector3 rootLocalLanding)
            : this(
                ordinal,
                landingCycle,
                normalizedTime,
                distance,
                rootLocalLanding,
                0,
                0,
                0)
        {
        }

        AnimationFootMotionEventOccurrence(
            int ordinal,
            int landingCycle,
            float normalizedTime,
            float distance,
            Vector3 rootLocalLanding,
            ulong sourceSampleIdentity,
            ulong contributionContinuityIdentity,
            ulong identity)
        {
            if (ordinal <= 0 ||
                !float.IsFinite(normalizedTime) ||
                normalizedTime < 0f ||
                normalizedTime > 1f ||
                !float.IsFinite(distance) ||
                distance < 0f ||
                !Finite(rootLocalLanding))
            {
                throw new ArgumentOutOfRangeException(nameof(ordinal));
            }
            Ordinal = ordinal;
            LandingCycle = landingCycle;
            NormalizedTime = normalizedTime;
            Distance = distance;
            RootLocalLanding = rootLocalLanding;
            SourceSampleIdentity = sourceSampleIdentity;
            ContributionContinuityIdentity = contributionContinuityIdentity;
            Identity = identity;
            m_IsSpecified = 1;
        }

        readonly byte m_IsSpecified;
        public int Ordinal { get; }
        public int LandingCycle { get; }
        public float NormalizedTime { get; }
        public float Distance { get; }
        public Vector3 RootLocalLanding { get; }
        public ulong SourceSampleIdentity { get; }
        public ulong ContributionContinuityIdentity { get; }
        public ulong Identity { get; }
        public bool IsValid => m_IsSpecified != 0;
        public bool IsBound => IsValid && SourceSampleIdentity != 0 && Identity != 0;

        internal AnimationFootMotionEventOccurrence Bind(
            ulong sourceSampleIdentity,
            ulong contributionContinuityIdentity,
            CharacterFootSide side)
        {
            if (!IsValid || sourceSampleIdentity == 0 ||
                contributionContinuityIdentity == 0 ||
                side != CharacterFootSide.Left && side != CharacterFootSide.Right)
            {
                throw new ArgumentException("Foot Motion Event lineage is invalid.");
            }
            return new AnimationFootMotionEventOccurrence(
                Ordinal,
                LandingCycle,
                NormalizedTime,
                Distance,
                RootLocalLanding,
                sourceSampleIdentity,
                contributionContinuityIdentity,
                AnimationFootMotionIdentity.LandingEvent(
                    contributionContinuityIdentity,
                    sourceSampleIdentity,
                    LandingCycle,
                    Ordinal,
                    side));
        }

        static bool Finite(Vector3 value) =>
            float.IsFinite(value.x) &&
            float.IsFinite(value.y) &&
            float.IsFinite(value.z);
    }

    public readonly struct AnimationFootMotionEventFrame
    {
        internal AnimationFootMotionEventFrame(
            AnimationFootMotionEventOccurrence currentContact,
            AnimationFootMotionEventOccurrence nextLanding,
            AnimationFootMotionEventPhase phase,
            float timeToLandingSeconds,
            float swingProgress,
            float approachContactToLandingProgress)
        {
            if (!Enum.IsDefined(typeof(AnimationFootMotionEventPhase), phase) ||
                !float.IsFinite(timeToLandingSeconds) ||
                timeToLandingSeconds < 0f ||
                !float.IsFinite(swingProgress) ||
                swingProgress < 0f || swingProgress > 1f ||
                !float.IsFinite(approachContactToLandingProgress) ||
                approachContactToLandingProgress < 0f ||
                approachContactToLandingProgress > 1f ||
                phase != AnimationFootMotionEventPhase.ApproachContact &&
                approachContactToLandingProgress != 0f ||
                phase == AnimationFootMotionEventPhase.Unavailable &&
                (currentContact.IsValid || nextLanding.IsValid) ||
                phase == AnimationFootMotionEventPhase.Contact &&
                !currentContact.IsValid ||
                phase >= AnimationFootMotionEventPhase.PreSwing &&
                phase <= AnimationFootMotionEventPhase.ApproachContact &&
                !nextLanding.IsValid)
            {
                throw new ArgumentException("Foot Motion Event frame is invalid.");
            }
            CurrentContact = currentContact;
            NextLanding = nextLanding;
            Phase = phase;
            TimeToLandingSeconds = timeToLandingSeconds;
            SwingProgress = swingProgress;
            ApproachContactToLandingProgress =
                approachContactToLandingProgress;
            m_IsSpecified = 1;
        }

        readonly byte m_IsSpecified;
        public AnimationFootMotionEventOccurrence CurrentContact { get; }
        public AnimationFootMotionEventOccurrence NextLanding { get; }
        public AnimationFootMotionEventPhase Phase { get; }
        public float TimeToLandingSeconds { get; }
        public float SwingProgress { get; }
        public float ApproachContactToLandingProgress { get; }
        public bool IsValid => m_IsSpecified != 0;
        public bool InApproachContactToLanding =>
            IsValid && Phase == AnimationFootMotionEventPhase.ApproachContact;

        internal AnimationFootMotionEventFrame Bind(
            ulong sourceSampleIdentity,
            ulong contributionContinuityIdentity,
            CharacterFootSide side)
        {
            if (!IsValid)
                throw new InvalidOperationException("Foot Motion Event frame is unavailable.");
            return new AnimationFootMotionEventFrame(
                CurrentContact.IsValid
                    ? CurrentContact.Bind(
                        sourceSampleIdentity,
                        contributionContinuityIdentity,
                        side)
                    : default,
                NextLanding.IsValid
                    ? NextLanding.Bind(
                        sourceSampleIdentity,
                        contributionContinuityIdentity,
                        side)
                    : default,
                Phase,
                TimeToLandingSeconds,
                SwingProgress,
                ApproachContactToLandingProgress);
        }
    }

    internal static class AnimationFootMotionIdentity
    {
        internal static ulong Source(AnimationPoseSourceId sourceId)
        {
            if (!sourceId.IsValid)
                throw new ArgumentException("Foot Motion source is invalid.", nameof(sourceId));
            return HashText(sourceId.ToString());
        }

        internal static ulong Source(string stableIdentity)
        {
            if (string.IsNullOrWhiteSpace(stableIdentity))
                throw new ArgumentException("Foot Motion source is invalid.", nameof(stableIdentity));
            return HashText(stableIdentity.Trim());
        }

        internal static ulong LandingEvent(
            ulong contributionContinuityIdentity,
            ulong sourceSampleIdentity,
            int sourceSampleCycle,
            int eventOrdinal,
            CharacterFootSide side)
        {
            if (contributionContinuityIdentity == 0 || sourceSampleIdentity == 0 ||
                eventOrdinal <= 0 ||
                side != CharacterFootSide.Left && side != CharacterFootSide.Right)
            {
                throw new ArgumentException("Foot Motion Landing Event lineage is invalid.");
            }
            return Hash(
                contributionContinuityIdentity,
                sourceSampleIdentity,
                unchecked((ulong)(long)sourceSampleCycle),
                (ulong)(uint)eventOrdinal,
                (ulong)side);
        }

        static ulong Hash(ulong a, ulong b, ulong c, ulong d, ulong e)
        {
            const ulong offset = 14695981039346656037UL;
            const ulong prime = 1099511628211UL;
            ulong value = offset;
            value = (value ^ a) * prime;
            value = (value ^ b) * prime;
            value = (value ^ c) * prime;
            value = (value ^ d) * prime;
            value = (value ^ e) * prime;
            return value == 0 ? 1UL : value;
        }

        static ulong HashText(string value)
        {
            const ulong offset = 14695981039346656037UL;
            const ulong prime = 1099511628211UL;
            ulong hash = offset;
            for (int i = 0; i < value.Length; i++)
                hash = (hash ^ value[i]) * prime;
            return hash == 0 ? 1UL : hash;
        }
    }

    [Serializable]
    public struct AnimationFootStepLandingEvent
    {
        [SerializeField] float m_NormalizedTime;
        [SerializeField] int m_Ordinal;
        [SerializeField] int m_CycleOffset;
        [SerializeField] float m_Distance;
        [SerializeField] Vector3 m_RootLocalLanding;
        [SerializeField] byte m_HasSwingBoundaries;
        [SerializeField] float m_PreSwingLeadSeconds;
        [SerializeField] float m_SwingLeadSeconds;
        [SerializeField] float m_ApproachContactLeadSeconds;

        public AnimationFootStepLandingEvent(
            float normalizedTime,
            int ordinal,
            int cycleOffset,
            float distance,
            Vector3 rootLocalLanding,
            bool hasSwingBoundaries,
            float preSwingLeadSeconds,
            float swingLeadSeconds,
            float approachContactLeadSeconds)
        {
            m_NormalizedTime = normalizedTime;
            m_Ordinal = ordinal;
            m_CycleOffset = cycleOffset;
            m_Distance = distance;
            m_RootLocalLanding = rootLocalLanding;
            m_HasSwingBoundaries = hasSwingBoundaries ? (byte)1 : (byte)0;
            m_PreSwingLeadSeconds = preSwingLeadSeconds;
            m_SwingLeadSeconds = swingLeadSeconds;
            m_ApproachContactLeadSeconds = approachContactLeadSeconds;
            RequireValid();
        }

        public float NormalizedTime => m_NormalizedTime;
        public int Ordinal => m_Ordinal;
        public int CycleOffset => m_CycleOffset;
        public float Distance => m_Distance;
        public Vector3 RootLocalLanding => m_RootLocalLanding;
        public bool HasSwingBoundaries => m_HasSwingBoundaries != 0;
        public float PreSwingLeadSeconds => m_PreSwingLeadSeconds;
        public float SwingLeadSeconds => m_SwingLeadSeconds;
        public float ApproachContactLeadSeconds => m_ApproachContactLeadSeconds;

        public void RequireValid()
        {
            if (!float.IsFinite(m_NormalizedTime) ||
                m_NormalizedTime < 0f ||
                m_NormalizedTime > 1f ||
                m_Ordinal <= 0 ||
                m_CycleOffset < 0 ||
                !float.IsFinite(m_Distance) ||
                m_Distance < 0f ||
                !Finite(m_RootLocalLanding) ||
                !float.IsFinite(m_PreSwingLeadSeconds) ||
                !float.IsFinite(m_SwingLeadSeconds) ||
                !float.IsFinite(m_ApproachContactLeadSeconds) ||
                m_PreSwingLeadSeconds < 0f ||
                m_SwingLeadSeconds < 0f ||
                m_SwingLeadSeconds > m_PreSwingLeadSeconds ||
                m_ApproachContactLeadSeconds < 0f ||
                m_ApproachContactLeadSeconds > m_SwingLeadSeconds ||
                !HasSwingBoundaries &&
                (m_PreSwingLeadSeconds != 0f ||
                 m_SwingLeadSeconds != 0f ||
                 m_ApproachContactLeadSeconds != 0f) ||
                HasSwingBoundaries && m_SwingLeadSeconds <= 0f)
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

        internal AnimationFootMotionEventFrame Resolve(
            float normalizedTime,
            int sourceCycle,
            float sourceDurationSeconds,
            bool looping)
        {
            RequireValid();
            if (!float.IsFinite(normalizedTime) ||
                normalizedTime < 0f ||
                normalizedTime > 1f ||
                !float.IsFinite(sourceDurationSeconds) ||
                sourceDurationSeconds <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(normalizedTime));
            }
            if (m_Events.Length == 0)
            {
                return new AnimationFootMotionEventFrame(
                    default,
                    default,
                    AnimationFootMotionEventPhase.Unavailable,
                    0f,
                    0f,
                    0f);
            }

            const float boundaryTolerance = 0.000001f;
            int previousIndex = -1;
            int nextIndex = -1;
            for (int i = 0; i < m_Events.Length; i++)
            {
                if (m_Events[i].NormalizedTime <= normalizedTime + boundaryTolerance)
                {
                    previousIndex = i;
                    continue;
                }
                if (m_Events[i].HasSwingBoundaries)
                {
                    nextIndex = i;
                    break;
                }
            }

            int previousCycle = sourceCycle;
            int nextCycle = sourceCycle;
            if (previousIndex < 0 && looping)
            {
                previousIndex = m_Events.Length - 1;
                previousCycle = checked(sourceCycle - 1);
            }
            if (nextIndex < 0 && looping)
            {
                for (int i = 0; i < m_Events.Length; i++)
                {
                    if (!m_Events[i].HasSwingBoundaries)
                        continue;
                    nextIndex = i;
                    nextCycle = checked(sourceCycle + 1);
                    break;
                }
            }

            AnimationFootMotionEventOccurrence currentContact =
                previousIndex >= 0
                    ? Occurrence(m_Events[previousIndex], previousCycle)
                    : default;
            if (nextIndex < 0)
            {
                return currentContact.IsValid
                    ? new AnimationFootMotionEventFrame(
                        currentContact,
                        default,
                        AnimationFootMotionEventPhase.Contact,
                        0f,
                        0f,
                        0f)
                    : new AnimationFootMotionEventFrame(
                        default,
                        default,
                        AnimationFootMotionEventPhase.Unavailable,
                        0f,
                        0f,
                        0f);
            }

            AnimationFootStepLandingEvent nextEvent = m_Events[nextIndex];
            AnimationFootMotionEventOccurrence nextLanding =
                Occurrence(nextEvent, nextCycle);
            double sourceTime = sourceCycle + normalizedTime;
            double landingTime = nextLanding.LandingCycle + nextEvent.NormalizedTime;
            float timeToLandingSeconds = (float)(
                (landingTime - sourceTime) * sourceDurationSeconds);
            if (!float.IsFinite(timeToLandingSeconds) || timeToLandingSeconds < -0.0001f)
                throw new InvalidOperationException("Foot Motion Event time is invalid.");
            timeToLandingSeconds = Mathf.Max(0f, timeToLandingSeconds);
            AnimationFootMotionEventPhase phase =
                nextEvent.ApproachContactLeadSeconds > boundaryTolerance &&
                timeToLandingSeconds <=
                nextEvent.ApproachContactLeadSeconds + 0.0001f
                    ? AnimationFootMotionEventPhase.ApproachContact
                    : timeToLandingSeconds <= nextEvent.SwingLeadSeconds + 0.0001f
                        ? AnimationFootMotionEventPhase.Swing
                        : timeToLandingSeconds <= nextEvent.PreSwingLeadSeconds + 0.0001f
                            ? AnimationFootMotionEventPhase.PreSwing
                            : currentContact.IsValid
                                ? AnimationFootMotionEventPhase.Contact
                                : AnimationFootMotionEventPhase.Unavailable;
            if (phase == AnimationFootMotionEventPhase.Unavailable)
            {
                return new AnimationFootMotionEventFrame(
                    default,
                    default,
                    AnimationFootMotionEventPhase.Unavailable,
                    0f,
                    0f,
                    0f);
            }
            float swingProgress = phase == AnimationFootMotionEventPhase.Swing ||
                                  phase == AnimationFootMotionEventPhase.ApproachContact
                ? Mathf.Clamp01(1f - timeToLandingSeconds / nextEvent.SwingLeadSeconds)
                : 0f;
            float approachContactToLandingProgress =
                phase == AnimationFootMotionEventPhase.ApproachContact
                    ? Mathf.Clamp01(
                        1f - timeToLandingSeconds /
                        nextEvent.ApproachContactLeadSeconds)
                    : 0f;
            return new AnimationFootMotionEventFrame(
                currentContact,
                nextLanding,
                phase,
                timeToLandingSeconds,
                swingProgress,
                approachContactToLandingProgress);
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
                for (int previous = 0; previous < i; previous++)
                {
                    if (m_Events[i].Ordinal == m_Events[previous].Ordinal)
                        throw new InvalidOperationException("Foot Step Landing Event ordinal is duplicated.");
                }
            }
        }

        static AnimationFootMotionEventOccurrence Occurrence(
            AnimationFootStepLandingEvent value,
            int sourceCycle) =>
            new AnimationFootMotionEventOccurrence(
                value.Ordinal,
                checked(sourceCycle + value.CycleOffset),
                value.NormalizedTime,
                value.Distance,
                value.RootLocalLanding);
    }
}
