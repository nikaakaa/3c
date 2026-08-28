using System;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public enum AnimationFootStepObservationLockMode : byte
    {
        Unlocked = 0,
        Sliding = 1,
        Locked = 2
    }

    public readonly struct AnimationFootStepObservationSample
    {
        internal AnimationFootStepObservationSample(
            float timeToLandingSeconds,
            float distance,
            float footHeight,
            float toeHeight,
            float toeSpeed,
            float positionError,
            float rotationError,
            float contact,
            AnimationFootStepObservationLockMode lockMode,
            float lockWeight,
            float support)
        {
            if (!float.IsFinite(timeToLandingSeconds) || timeToLandingSeconds < 0f ||
                !float.IsFinite(distance) || distance < 0f ||
                !float.IsFinite(footHeight) || footHeight < 0f ||
                !float.IsFinite(toeHeight) ||
                !float.IsFinite(toeSpeed) || toeSpeed < 0f ||
                !float.IsFinite(positionError) || positionError < 0f ||
                !float.IsFinite(rotationError) || rotationError < 0f ||
                !Normalized(contact) || !Enum.IsDefined(typeof(AnimationFootStepObservationLockMode), lockMode) ||
                !Normalized(lockWeight) || !Normalized(support))
            {
                throw new ArgumentOutOfRangeException(nameof(timeToLandingSeconds));
            }
            TimeToLandingSeconds = timeToLandingSeconds;
            Distance = distance;
            FootHeight = footHeight;
            ToeHeight = toeHeight;
            ToeSpeed = toeSpeed;
            PositionError = positionError;
            RotationError = rotationError;
            Contact = contact;
            LockMode = lockMode;
            LockWeight = lockWeight;
            Support = support;
            m_IsSpecified = 1;
        }

        readonly byte m_IsSpecified;
        public float TimeToLandingSeconds { get; }
        public float Distance { get; }
        public float FootHeight { get; }
        public float ToeHeight { get; }
        public float ToeSpeed { get; }
        public float PositionError { get; }
        public float RotationError { get; }
        public float Contact { get; }
        public AnimationFootStepObservationLockMode LockMode { get; }
        public float LockWeight { get; }
        public float Support { get; }
        public bool IsValid => m_IsSpecified != 0;

        static bool Normalized(float value) =>
            float.IsFinite(value) && value >= 0f && value <= 1f;
    }

    internal readonly struct AnimationFootMotionRuntimeSample
    {
        internal AnimationFootMotionRuntimeSample(
            AnimationFootStepObservationSample observation,
            AnimationFootMotionLandingEventReference contactEvent,
            AnimationFootMotionLandingEventReference previousLandingEvent,
            AnimationFootMotionLandingEventReference nextLandingEvent,
            int frameSourceCycle,
            float frameNormalizedTime,
            float sourceDurationSeconds,
            ulong contributionContinuityIdentity)
        {
            if (!observation.IsValid ||
                observation.TimeToLandingSeconds > 0.000001f &&
                !nextLandingEvent.IsValid ||
                nextLandingEvent.IsValid &&
                Mathf.Abs(observation.Distance - nextLandingEvent.Distance) > 0.02f ||
                (observation.Contact > 0.0001f ||
                 observation.LockMode != AnimationFootStepObservationLockMode.Unlocked ||
                 observation.LockWeight > 0.0001f ||
                 observation.Support > 0.0001f) &&
                !contactEvent.IsValid)
            {
                throw new ArgumentException("Foot Motion Runtime sample is invalid.");
            }
            Observation = observation;
            ContactEvent = contactEvent;
            PreviousLandingEvent = previousLandingEvent;
            NextLandingEvent = nextLandingEvent;
            PredictionStep = nextLandingEvent.IsValid
                ? new AnimationFootMotionStep(
                    observation,
                    previousLandingEvent,
                    nextLandingEvent,
                    frameSourceCycle,
                    frameNormalizedTime,
                    sourceDurationSeconds,
                    contributionContinuityIdentity)
                : default;
            ContactStep = contactEvent.IsValid &&
                          (observation.Contact > 0.0001f ||
                           observation.LockMode !=
                           AnimationFootStepObservationLockMode.Unlocked ||
                           observation.LockWeight > 0.0001f ||
                           observation.Support > 0.0001f)
                ? new AnimationFootMotionStep(
                    contactEvent,
                    frameSourceCycle,
                    contributionContinuityIdentity)
                : default;
            m_IsSpecified = 1;
        }

        readonly byte m_IsSpecified;
        internal AnimationFootStepObservationSample Observation { get; }
        internal AnimationFootMotionLandingEventReference ContactEvent { get; }
        internal AnimationFootMotionLandingEventReference PreviousLandingEvent { get; }
        internal AnimationFootMotionLandingEventReference NextLandingEvent { get; }
        internal AnimationFootMotionStep PredictionStep { get; }
        internal AnimationFootMotionStep ContactStep { get; }
        internal bool IsValid => m_IsSpecified != 0;
    }

    internal readonly struct AnimationFootMotionRuntimeFrame
    {
        internal AnimationFootMotionRuntimeFrame(
            ulong completionIdentity,
            PoseNodeId nodeId,
            AnimationPoseSourceId sourceId,
            ulong contributionContinuityIdentity,
            string sourceIdentity,
            int clipBindingIndex,
            int cycle,
            float sourceWeight,
            float normalizedTime,
            float sourceDurationSeconds,
            AnimationFootStepObservationCurvePair curves)
        {
            if (completionIdentity == 0 || !nodeId.IsValid || !sourceId.IsValid ||
                contributionContinuityIdentity == 0 || clipBindingIndex < 0 ||
                string.IsNullOrWhiteSpace(sourceIdentity) ||
                !float.IsFinite(sourceWeight) || sourceWeight < 0f || sourceWeight > 1f ||
                !float.IsFinite(normalizedTime) || normalizedTime < 0f || normalizedTime > 1f ||
                !float.IsFinite(sourceDurationSeconds) || sourceDurationSeconds <= 0f ||
                curves == null)
            {
                throw new ArgumentException("Foot Motion Runtime frame is invalid.");
            }
            curves.RequireValid();
            AnimationFootStepObservationSample leftObservation =
                curves.Left.Sample(normalizedTime);
            AnimationFootStepObservationSample rightObservation =
                curves.Right.Sample(normalizedTime);
            CompletionIdentity = completionIdentity;
            NodeId = nodeId;
            SourceId = sourceId;
            ContributionContinuityIdentity = contributionContinuityIdentity;
            SourceIdentity = sourceIdentity.Trim();
            ClipBindingIndex = clipBindingIndex;
            Cycle = cycle;
            SourceWeight = sourceWeight;
            NormalizedTime = normalizedTime;
            SourceDurationSeconds = sourceDurationSeconds;
            Left = new AnimationFootMotionRuntimeSample(
                leftObservation,
                curves.Left.LandingEvents.ResolveContact(
                    sourceIdentity,
                    contributionContinuityIdentity,
                    cycle,
                    normalizedTime,
                    sourceDurationSeconds,
                    AnimationFootMotionSide.Left),
                curves.Left.LandingEvents.ResolvePrevious(
                    sourceIdentity,
                    contributionContinuityIdentity,
                    cycle,
                    normalizedTime,
                    sourceDurationSeconds,
                    AnimationFootMotionSide.Left),
                curves.Left.LandingEvents.ResolveNext(
                    sourceIdentity,
                    contributionContinuityIdentity,
                    cycle,
                    normalizedTime,
                    sourceDurationSeconds,
                    leftObservation.TimeToLandingSeconds,
                    AnimationFootMotionSide.Left),
                cycle,
                normalizedTime,
                sourceDurationSeconds,
                contributionContinuityIdentity);
            Right = new AnimationFootMotionRuntimeSample(
                rightObservation,
                curves.Right.LandingEvents.ResolveContact(
                    sourceIdentity,
                    contributionContinuityIdentity,
                    cycle,
                    normalizedTime,
                    sourceDurationSeconds,
                    AnimationFootMotionSide.Right),
                curves.Right.LandingEvents.ResolvePrevious(
                    sourceIdentity,
                    contributionContinuityIdentity,
                    cycle,
                    normalizedTime,
                    sourceDurationSeconds,
                    AnimationFootMotionSide.Right),
                curves.Right.LandingEvents.ResolveNext(
                    sourceIdentity,
                    contributionContinuityIdentity,
                    cycle,
                    normalizedTime,
                    sourceDurationSeconds,
                    rightObservation.TimeToLandingSeconds,
                    AnimationFootMotionSide.Right),
                cycle,
                normalizedTime,
                sourceDurationSeconds,
                contributionContinuityIdentity);
            m_IsSpecified = 1;
        }

        readonly byte m_IsSpecified;
        internal ulong CompletionIdentity { get; }
        internal PoseNodeId NodeId { get; }
        internal AnimationPoseSourceId SourceId { get; }
        internal ulong ContributionContinuityIdentity { get; }
        internal string SourceIdentity { get; }
        internal int ClipBindingIndex { get; }
        internal int Cycle { get; }
        internal float SourceWeight { get; }
        internal float NormalizedTime { get; }
        internal float SourceDurationSeconds { get; }
        internal AnimationFootMotionRuntimeSample Left { get; }
        internal AnimationFootMotionRuntimeSample Right { get; }
        internal bool IsValid => m_IsSpecified != 0;
    }

    [Serializable]
    public sealed class AnimationFootStepObservationCurveSet
    {
        [SerializeField] AnimationCurve m_TimeToLandingSeconds;
        [SerializeField] AnimationCurve m_Distance;
        [SerializeField] AnimationCurve m_FootHeight;
        [SerializeField] AnimationCurve m_ToeHeight;
        [SerializeField] AnimationCurve m_ToeSpeed;
        [SerializeField] AnimationCurve m_PositionError;
        [SerializeField] AnimationCurve m_RotationError;
        [SerializeField] AnimationCurve m_Contact;
        [SerializeField] AnimationCurve m_LockMode;
        [SerializeField] AnimationCurve m_LockWeight;
        [SerializeField] AnimationCurve m_Support;
        [SerializeField] AnimationFootStepLandingEventTable m_LandingEvents;

        public AnimationFootStepObservationCurveSet(
            AnimationCurve timeToLandingSeconds,
            AnimationCurve distance,
            AnimationCurve footHeight,
            AnimationCurve toeHeight,
            AnimationCurve toeSpeed,
            AnimationCurve positionError,
            AnimationCurve rotationError,
            AnimationCurve contact,
            AnimationCurve lockMode,
            AnimationCurve lockWeight,
            AnimationCurve support,
            AnimationFootStepLandingEventTable landingEvents)
        {
            m_TimeToLandingSeconds = AnimationPredictedFootStepCurveSet.Copy(timeToLandingSeconds);
            m_Distance = AnimationPredictedFootStepCurveSet.Copy(distance);
            m_FootHeight = AnimationPredictedFootStepCurveSet.Copy(footHeight);
            m_ToeHeight = AnimationPredictedFootStepCurveSet.Copy(toeHeight);
            m_ToeSpeed = AnimationPredictedFootStepCurveSet.Copy(toeSpeed);
            m_PositionError = AnimationPredictedFootStepCurveSet.Copy(positionError);
            m_RotationError = AnimationPredictedFootStepCurveSet.Copy(rotationError);
            m_Contact = AnimationPredictedFootStepCurveSet.Copy(contact);
            m_LockMode = AnimationPredictedFootStepCurveSet.Copy(lockMode);
            m_LockWeight = AnimationPredictedFootStepCurveSet.Copy(lockWeight);
            m_Support = AnimationPredictedFootStepCurveSet.Copy(support);
            m_LandingEvents = landingEvents ??
                throw new ArgumentNullException(nameof(landingEvents));
            RequireValid();
        }

        public AnimationCurve TimeToLandingSeconds => m_TimeToLandingSeconds;
        public AnimationCurve Distance => m_Distance;
        public AnimationCurve FootHeight => m_FootHeight;
        public AnimationCurve ToeHeight => m_ToeHeight;
        public AnimationCurve ToeSpeed => m_ToeSpeed;
        public AnimationCurve PositionError => m_PositionError;
        public AnimationCurve RotationError => m_RotationError;
        public AnimationCurve Contact => m_Contact;
        public AnimationCurve LockMode => m_LockMode;
        public AnimationCurve LockWeight => m_LockWeight;
        public AnimationCurve Support => m_Support;
        public AnimationFootStepLandingEventTable LandingEvents => m_LandingEvents;

        public AnimationFootStepObservationSample Sample(float normalizedTime)
        {
            RequireValid();
            float time = Mathf.Clamp01(normalizedTime);
            return new AnimationFootStepObservationSample(
                m_TimeToLandingSeconds.Evaluate(time),
                m_Distance.Evaluate(time),
                m_FootHeight.Evaluate(time),
                m_ToeHeight.Evaluate(time),
                m_ToeSpeed.Evaluate(time),
                m_PositionError.Evaluate(time),
                m_RotationError.Evaluate(time),
                m_Contact.Evaluate(time),
                (AnimationFootStepObservationLockMode)Mathf.RoundToInt(m_LockMode.Evaluate(time)),
                m_LockWeight.Evaluate(time),
                m_Support.Evaluate(time));
        }

        public void RequireValid()
        {
            AnimationPredictedFootStepCurveSet.RequireCurve(
                m_TimeToLandingSeconds,
                nameof(m_TimeToLandingSeconds),
                false,
                true);
            AnimationPredictedFootStepCurveSet.RequireCurve(
                m_Distance,
                nameof(m_Distance),
                false,
                true);
            AnimationPredictedFootStepCurveSet.RequireCurve(
                m_FootHeight,
                nameof(m_FootHeight),
                false,
                true);
            AnimationPredictedFootStepCurveSet.RequireCurve(
                m_ToeHeight,
                nameof(m_ToeHeight),
                false,
                false);
            AnimationPredictedFootStepCurveSet.RequireCurve(
                m_ToeSpeed,
                nameof(m_ToeSpeed),
                false,
                true);
            AnimationPredictedFootStepCurveSet.RequireCurve(
                m_PositionError,
                nameof(m_PositionError),
                false,
                true);
            AnimationPredictedFootStepCurveSet.RequireCurve(
                m_RotationError,
                nameof(m_RotationError),
                false,
                true);
            AnimationPredictedFootStepCurveSet.RequireCurve(
                m_Contact,
                nameof(m_Contact),
                true,
                false);
            RequireLockModeCurve(m_LockMode);
            AnimationPredictedFootStepCurveSet.RequireCurve(
                m_LockWeight,
                nameof(m_LockWeight),
                true,
                false);
            AnimationPredictedFootStepCurveSet.RequireCurve(
                m_Support,
                nameof(m_Support),
                true,
                false);
            if (m_LandingEvents == null)
                throw new InvalidOperationException(
                    "Foot Step Landing Event table is missing.");
            m_LandingEvents.RequireValid();
        }

        static void RequireLockModeCurve(AnimationCurve curve)
        {
            AnimationPredictedFootStepCurveSet.RequireCurve(
                curve,
                nameof(m_LockMode),
                false,
                true);
            Keyframe[] keys = curve.keys;
            for (int i = 0; i < keys.Length; i++)
            {
                float value = keys[i].value;
                if (value > (float)AnimationFootStepObservationLockMode.Locked ||
                    !Mathf.Approximately(value, Mathf.Round(value)))
                {
                    throw new InvalidOperationException("Foot Step observation Lock Mode Curve is invalid.");
                }
            }
        }
    }

    [Serializable]
    public sealed class AnimationFootStepObservationCurvePair
    {
        [SerializeField] AnimationFootStepObservationCurveSet m_Left;
        [SerializeField] AnimationFootStepObservationCurveSet m_Right;

        public AnimationFootStepObservationCurvePair(
            AnimationFootStepObservationCurveSet left,
            AnimationFootStepObservationCurveSet right)
        {
            m_Left = left ?? throw new ArgumentNullException(nameof(left));
            m_Right = right ?? throw new ArgumentNullException(nameof(right));
            RequireValid();
        }

        public AnimationFootStepObservationCurveSet Left => m_Left;
        public AnimationFootStepObservationCurveSet Right => m_Right;

        public void RequireValid()
        {
            if (m_Left == null || m_Right == null)
                throw new InvalidOperationException("Foot Step observation Curve pair is incomplete.");
            m_Left.RequireValid();
            m_Right.RequireValid();
        }
    }
}
