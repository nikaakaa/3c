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

    internal readonly struct AnimationFootStepObservationFrame
    {
        internal AnimationFootStepObservationFrame(
            ulong completionIdentity,
            PoseNodeId nodeId,
            AnimationPoseSourceId sourceId,
            ulong contributionContinuityIdentity,
            string sourceIdentity,
            int clipBindingIndex,
            int cycle,
            float sourceWeight,
            float normalizedTime,
            AnimationFootStepObservationSample left,
            AnimationFootStepObservationSample right)
        {
            if (completionIdentity == 0 || !nodeId.IsValid || !sourceId.IsValid ||
                contributionContinuityIdentity == 0 || clipBindingIndex < 0 ||
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
            ClipBindingIndex = clipBindingIndex;
            Cycle = cycle;
            SourceWeight = sourceWeight;
            NormalizedTime = normalizedTime;
            Left = left;
            Right = right;
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
        internal AnimationFootStepObservationSample Left { get; }
        internal AnimationFootStepObservationSample Right { get; }
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
            AnimationCurve support)
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
