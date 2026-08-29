using System;
using ThirdPersonCharacter.Pipeline.Presentation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public enum AnimationFootStepObservationLockMode : byte
    {
        Unlocked = 0,
        Sliding = 1,
        Locked = 2
    }

    public readonly struct AnimationFootMotionRuntimeSample
    {
        internal AnimationFootMotionRuntimeSample(
            float footHeight,
            float toeHeight,
            float toeSpeed,
            float positionError,
            float rotationError,
            float contact,
            AnimationFootStepObservationLockMode lockMode,
            float lockWeight,
            float support,
            in AnimationFootMotionEventFrame events)
        {
            if (!float.IsFinite(footHeight) || footHeight < 0f ||
                !float.IsFinite(toeHeight) ||
                !float.IsFinite(toeSpeed) || toeSpeed < 0f ||
                !float.IsFinite(positionError) || positionError < 0f ||
                !float.IsFinite(rotationError) || rotationError < 0f ||
                !Normalized(contact) || !Enum.IsDefined(typeof(AnimationFootStepObservationLockMode), lockMode) ||
                !Normalized(lockWeight) || !Normalized(support) ||
                !events.IsValid)
            {
                throw new ArgumentOutOfRangeException(nameof(footHeight));
            }
            FootHeight = footHeight;
            ToeHeight = toeHeight;
            ToeSpeed = toeSpeed;
            PositionError = positionError;
            RotationError = rotationError;
            Contact = contact;
            LockMode = lockMode;
            LockWeight = lockWeight;
            Support = support;
            Events = events;
            m_IsSpecified = 1;
        }

        readonly byte m_IsSpecified;
        public float FootHeight { get; }
        public float ToeHeight { get; }
        public float ToeSpeed { get; }
        public float PositionError { get; }
        public float RotationError { get; }
        public float Contact { get; }
        public AnimationFootStepObservationLockMode LockMode { get; }
        public float LockWeight { get; }
        public float Support { get; }
        public AnimationFootMotionEventFrame Events { get; }
        public bool IsValid => m_IsSpecified != 0;
        public bool HasPredictiveLanding =>
            IsValid && Events.NextLanding.IsBound &&
            (Events.Phase == AnimationFootMotionEventPhase.PreSwing ||
             Events.Phase == AnimationFootMotionEventPhase.Swing ||
             Events.Phase == AnimationFootMotionEventPhase.ApproachContact);
        public bool IsPreSwing =>
            IsValid && Events.Phase == AnimationFootMotionEventPhase.PreSwing;
        public bool IsSwing =>
            IsValid &&
            (Events.Phase == AnimationFootMotionEventPhase.Swing ||
             Events.Phase == AnimationFootMotionEventPhase.ApproachContact);
        public ulong LandingEventIdentity =>
            HasPredictiveLanding ? Events.NextLanding.Identity : 0;
        public float TimeToLandingSeconds =>
            HasPredictiveLanding ? Events.TimeToLandingSeconds : 0f;
        public float Distance =>
            HasPredictiveLanding ? Events.NextLanding.Distance : 0f;
        public Vector3 RootLocalLanding =>
            HasPredictiveLanding ? Events.NextLanding.RootLocalLanding : default;
        public float SwingProgress =>
            HasPredictiveLanding ? Events.SwingProgress : 0f;
        public float ApproachContactToLandingProgress =>
            HasPredictiveLanding && Events.InApproachContactToLanding
                ? Events.ApproachContactToLandingProgress
                : 0f;
        public int EventOrdinal =>
            HasPredictiveLanding ? Events.NextLanding.Ordinal : 0;
        public int SourceSampleCycle =>
            HasPredictiveLanding ? Events.NextLanding.LandingCycle : 0;
        public ulong SourceSampleIdentity =>
            HasPredictiveLanding ? Events.NextLanding.SourceSampleIdentity : 0;
        public ulong ContributionContinuityIdentity =>
            HasPredictiveLanding
                ? Events.NextLanding.ContributionContinuityIdentity
                : 0;
        public bool IsAuthoritative => IsValid;
        public bool HasConsistentLandingEventIdentity =>
            HasPredictiveLanding && LandingEventIdentity != 0;
        public bool HasCurrentContactEvent =>
            IsValid && Events.CurrentContact.IsBound;
        public ulong CurrentContactEventIdentity =>
            HasCurrentContactEvent ? Events.CurrentContact.Identity : 0;

        internal AnimationFootMotionRuntimeSample BindEventLineage(
            ulong sourceSampleIdentity,
            ulong contributionContinuityIdentity,
            CharacterFootSide side) =>
            new AnimationFootMotionRuntimeSample(
                FootHeight,
                ToeHeight,
                ToeSpeed,
                PositionError,
                RotationError,
                Contact,
                LockMode,
                LockWeight,
                Support,
                Events.Bind(
                    sourceSampleIdentity,
                    contributionContinuityIdentity,
                    side));

        static bool Normalized(float value) =>
            float.IsFinite(value) && value >= 0f && value <= 1f;
    }

    internal readonly struct AnimationFootMotionRuntimeFrame
    {
        internal AnimationFootMotionRuntimeFrame(
            ulong completionIdentity,
            PoseNodeId nodeId,
            AnimationPoseSourceId sourceId,
            ulong contributionContinuityIdentity,
            string sourceIdentity,
            ulong sourceSampleIdentity,
            int clipBindingIndex,
            int cycle,
            float sourceWeight,
            float normalizedTime,
            AnimationFootMotionRuntimeSample left,
            AnimationFootMotionRuntimeSample right)
        {
            if (completionIdentity == 0 || !nodeId.IsValid || !sourceId.IsValid ||
                contributionContinuityIdentity == 0 || clipBindingIndex < 0 ||
                sourceSampleIdentity == 0 ||
                string.IsNullOrWhiteSpace(sourceIdentity) ||
                !float.IsFinite(sourceWeight) || sourceWeight < 0f || sourceWeight > 1f ||
                !float.IsFinite(normalizedTime) || normalizedTime < 0f || normalizedTime > 1f ||
                !left.IsValid || !right.IsValid)
            {
                throw new ArgumentException("Foot Step observation frame is invalid.");
            }
            CompletionIdentity = completionIdentity;
            NodeId = nodeId;
            SourceId = sourceId;
            ContributionContinuityIdentity = contributionContinuityIdentity;
            SourceIdentity = sourceIdentity.Trim();
            SourceSampleIdentity = sourceSampleIdentity;
            ClipBindingIndex = clipBindingIndex;
            Cycle = cycle;
            SourceWeight = sourceWeight;
            NormalizedTime = normalizedTime;
            Left = left.BindEventLineage(
                sourceSampleIdentity,
                contributionContinuityIdentity,
                CharacterFootSide.Left);
            Right = right.BindEventLineage(
                sourceSampleIdentity,
                contributionContinuityIdentity,
                CharacterFootSide.Right);
            m_IsSpecified = 1;
        }

        readonly byte m_IsSpecified;
        internal ulong CompletionIdentity { get; }
        internal PoseNodeId NodeId { get; }
        internal AnimationPoseSourceId SourceId { get; }
        internal ulong ContributionContinuityIdentity { get; }
        internal string SourceIdentity { get; }
        internal ulong SourceSampleIdentity { get; }
        internal int ClipBindingIndex { get; }
        internal int Cycle { get; }
        internal float SourceWeight { get; }
        internal float NormalizedTime { get; }
        internal AnimationFootMotionRuntimeSample Left { get; }
        internal AnimationFootMotionRuntimeSample Right { get; }
        internal bool IsValid => m_IsSpecified != 0;
    }

    [Serializable]
    public sealed class AnimationFootStepObservationCurveSet
    {
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

        public AnimationFootMotionRuntimeSample Sample(
            float normalizedTime,
            int sourceCycle,
            float sourceDurationSeconds,
            bool looping)
        {
            RequireValid();
            float time = Mathf.Clamp01(normalizedTime);
            AnimationFootMotionEventFrame events = m_LandingEvents.Resolve(
                time,
                sourceCycle,
                sourceDurationSeconds,
                looping);
            return new AnimationFootMotionRuntimeSample(
                m_FootHeight.Evaluate(time),
                m_ToeHeight.Evaluate(time),
                m_ToeSpeed.Evaluate(time),
                m_PositionError.Evaluate(time),
                m_RotationError.Evaluate(time),
                m_Contact.Evaluate(time),
                (AnimationFootStepObservationLockMode)Mathf.RoundToInt(m_LockMode.Evaluate(time)),
                m_LockWeight.Evaluate(time),
                m_Support.Evaluate(time),
                in events);
        }

        public void RequireValid()
        {
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
