using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    public enum AnimationFootMotionEventKind : byte
    {
        Landing = 1,
        LiftOff = 2
    }

    public enum AnimationFootLockMode : byte
    {
        Unlocked = 0,
        Sliding = 1,
        Locked = 2
    }

    public enum AnimationFootMotionDiagnosticCode : byte
    {
        NoStep = 1,
        ZeroLengthStep = 2,
        LoopCycleBoundary = 3,
        FiniteTerminalSegment = 4
    }

    public readonly struct AnimationFootMotionDiagnostic
    {
        public AnimationFootMotionDiagnostic(AnimationFootMotionDiagnosticCode code, int sampleIndex)
        {
            Code = code;
            SampleIndex = sampleIndex;
            if (!Enum.IsDefined(typeof(AnimationFootMotionDiagnosticCode), code) || sampleIndex < -1)
                throw new InvalidOperationException("Foot Motion diagnostic is invalid.");
        }

        public AnimationFootMotionDiagnosticCode Code { get; }
        public int SampleIndex { get; }
    }

    public readonly struct AnimationFootMotionPose
    {
        public AnimationFootMotionPose(
            Vector3 rootLocalPosition,
            Quaternion rootLocalRotation,
            Vector3 motionPosition,
            Quaternion motionRotation)
        {
            RootLocalPosition = rootLocalPosition;
            RootLocalRotation = rootLocalRotation.normalized;
            MotionPosition = motionPosition;
            MotionRotation = motionRotation.normalized;
            RequireValid();
        }

        public Vector3 RootLocalPosition { get; }
        public Quaternion RootLocalRotation { get; }
        public Vector3 MotionPosition { get; }
        public Quaternion MotionRotation { get; }

        public void RequireValid()
        {
            RequireFinite(RootLocalPosition);
            RequireFinite(MotionPosition);
            RequireFinite(RootLocalRotation);
            RequireFinite(MotionRotation);
        }

        static void RequireFinite(Vector3 value)
        {
            if (!float.IsFinite(value.x) || !float.IsFinite(value.y) || !float.IsFinite(value.z))
                throw new InvalidOperationException("Foot Motion pose position is invalid.");
        }

        static void RequireFinite(Quaternion value)
        {
            if (!float.IsFinite(value.x) || !float.IsFinite(value.y) ||
                !float.IsFinite(value.z) || !float.IsFinite(value.w))
                throw new InvalidOperationException("Foot Motion pose rotation is invalid.");
        }
    }

    public readonly struct AnimationFootMotionRootSample
    {
        public AnimationFootMotionRootSample(float timeSeconds, Vector3 position, Quaternion rotation)
        {
            TimeSeconds = timeSeconds;
            Position = position;
            Rotation = rotation.normalized;
            if (!float.IsFinite(timeSeconds) || timeSeconds < 0f ||
                !float.IsFinite(position.x) || !float.IsFinite(position.y) || !float.IsFinite(position.z) ||
                !float.IsFinite(rotation.x) || !float.IsFinite(rotation.y) ||
                !float.IsFinite(rotation.z) || !float.IsFinite(rotation.w))
                throw new InvalidOperationException("Foot Motion root sample is invalid.");
        }

        public float TimeSeconds { get; }
        public Vector3 Position { get; }
        public Quaternion Rotation { get; }
    }

    public readonly struct AnimationFootMotionRawSample
    {
        public AnimationFootMotionRawSample(
            float timeSeconds,
            AnimationFootMotionPose hip,
            AnimationFootMotionPose knee,
            AnimationFootMotionPose ankle,
            AnimationFootMotionPose heel,
            AnimationFootMotionPose toe,
            AnimationFootMotionPose sole,
            Vector3 soleVelocity,
            Vector3 toeVelocity,
            Vector3 soleAngularVelocity)
        {
            TimeSeconds = timeSeconds;
            Hip = hip;
            Knee = knee;
            Ankle = ankle;
            Heel = heel;
            Toe = toe;
            Sole = sole;
            SoleVelocity = soleVelocity;
            ToeVelocity = toeVelocity;
            SoleAngularVelocity = soleAngularVelocity;
            RequireValid();
        }

        public float TimeSeconds { get; }
        public AnimationFootMotionPose Hip { get; }
        public AnimationFootMotionPose Knee { get; }
        public AnimationFootMotionPose Ankle { get; }
        public AnimationFootMotionPose Heel { get; }
        public AnimationFootMotionPose Toe { get; }
        public AnimationFootMotionPose Sole { get; }
        public Vector3 SoleVelocity { get; }
        public Vector3 ToeVelocity { get; }
        public Vector3 SoleAngularVelocity { get; }

        public void RequireValid()
        {
            if (!float.IsFinite(TimeSeconds) || TimeSeconds < 0f)
                throw new InvalidOperationException("Foot Motion raw sample time is invalid.");
            Hip.RequireValid();
            Knee.RequireValid();
            Ankle.RequireValid();
            Heel.RequireValid();
            Toe.RequireValid();
            Sole.RequireValid();
            RequireFinite(SoleVelocity);
            RequireFinite(ToeVelocity);
            RequireFinite(SoleAngularVelocity);
        }

        static void RequireFinite(Vector3 value)
        {
            if (!float.IsFinite(value.x) || !float.IsFinite(value.y) || !float.IsFinite(value.z))
                throw new InvalidOperationException("Foot Motion raw velocity is invalid.");
        }
    }

    public sealed class AnimationFootMotionRawFootPage
    {
        readonly AnimationFootMotionRawSample[] m_Samples;

        public AnimationFootMotionRawFootPage(float rigLegLength, AnimationFootMotionRawSample[] samples)
        {
            RigLegLength = rigLegLength;
            m_Samples = samples == null
                ? throw new ArgumentNullException(nameof(samples))
                : (AnimationFootMotionRawSample[])samples.Clone();
            RequireValid();
        }

        public float RigLegLength { get; }
        public IReadOnlyList<AnimationFootMotionRawSample> Samples => m_Samples;

        public void RequireValid()
        {
            if (!float.IsFinite(RigLegLength) || RigLegLength <= 0f || m_Samples.Length < 3)
                throw new InvalidOperationException("Foot Motion raw foot page is invalid.");
            for (int i = 0; i < m_Samples.Length; i++)
            {
                m_Samples[i].RequireValid();
                if (i > 0 && m_Samples[i].TimeSeconds <= m_Samples[i - 1].TimeSeconds)
                    throw new InvalidOperationException("Foot Motion raw sample time is not increasing.");
            }
        }
    }

    public sealed class AnimationFootMotionRawPage
    {
        readonly AnimationFootMotionRootSample[] m_RootSamples;

        public AnimationFootMotionRawPage(
            float sampleRate,
            float durationSeconds,
            float groundReferenceHeight,
            AnimationFootMotionRootSample[] rootSamples,
            AnimationFootMotionRawFootPage left,
            AnimationFootMotionRawFootPage right)
        {
            SampleRate = sampleRate;
            DurationSeconds = durationSeconds;
            GroundReferenceHeight = groundReferenceHeight;
            m_RootSamples = rootSamples == null
                ? throw new ArgumentNullException(nameof(rootSamples))
                : (AnimationFootMotionRootSample[])rootSamples.Clone();
            Left = left ?? throw new ArgumentNullException(nameof(left));
            Right = right ?? throw new ArgumentNullException(nameof(right));
            RequireValid();
        }

        public float SampleRate { get; }
        public float DurationSeconds { get; }
        public float GroundReferenceHeight { get; }
        public IReadOnlyList<AnimationFootMotionRootSample> RootSamples => m_RootSamples;
        public AnimationFootMotionRawFootPage Left { get; }
        public AnimationFootMotionRawFootPage Right { get; }

        public void RequireValid()
        {
            if (!float.IsFinite(SampleRate) || SampleRate <= 0f ||
                !float.IsFinite(DurationSeconds) || DurationSeconds <= 0f ||
                !float.IsFinite(GroundReferenceHeight) || m_RootSamples.Length < 3)
                throw new InvalidOperationException("Foot Motion raw page timing is invalid.");
            Left.RequireValid();
            Right.RequireValid();
            if (Left.Samples.Count != m_RootSamples.Length || Right.Samples.Count != m_RootSamples.Length)
                throw new InvalidOperationException("Foot Motion raw page sample counts do not match.");
            for (int i = 0; i < m_RootSamples.Length; i++)
            {
                if (i > 0 && m_RootSamples[i].TimeSeconds <= m_RootSamples[i - 1].TimeSeconds)
                    throw new InvalidOperationException("Foot Motion root sample time is not increasing.");
            }
            if (Mathf.Abs(m_RootSamples[m_RootSamples.Length - 1].TimeSeconds - DurationSeconds) > 0.00001f)
                throw new InvalidOperationException("Foot Motion raw page does not cover the source duration.");
        }
    }

    public readonly struct AnimationFootMotionEvent
    {
        public AnimationFootMotionEvent(
            AnimationFootMotionEventKind kind,
            int sampleIndex,
            int ordinal,
            int cycleOffset,
            Vector3 rootLocalSolePosition,
            Vector3 motionSolePosition,
            Quaternion soleRotation)
        {
            Kind = kind;
            SampleIndex = sampleIndex;
            Ordinal = ordinal;
            CycleOffset = cycleOffset;
            RootLocalSolePosition = rootLocalSolePosition;
            MotionSolePosition = motionSolePosition;
            SoleRotation = soleRotation.normalized;
            if (kind == 0 || sampleIndex < 0 || ordinal <= 0 ||
                !Finite(rootLocalSolePosition) || !Finite(motionSolePosition) || !Finite(SoleRotation))
                throw new InvalidOperationException("Foot Motion event is invalid.");
        }

        public AnimationFootMotionEventKind Kind { get; }
        public int SampleIndex { get; }
        public int Ordinal { get; }
        public int CycleOffset { get; }
        public Vector3 RootLocalSolePosition { get; }
        public Vector3 MotionSolePosition { get; }
        public Quaternion SoleRotation { get; }

        static bool Finite(Vector3 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);

        static bool Finite(Quaternion value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) &&
            float.IsFinite(value.z) && float.IsFinite(value.w);
    }

    public readonly struct AnimationFootMotionStepEvidence
    {
        public AnimationFootMotionStepEvidence(
            bool available,
            int landingOrdinal,
            float timeSeconds,
            float distance,
            float pathProgress,
            float baselineHeight,
            float animationHeight,
            float heightAbovePath)
        {
            Available = available;
            LandingOrdinal = landingOrdinal;
            TimeSeconds = timeSeconds;
            Distance = distance;
            PathProgress = pathProgress;
            BaselineHeight = baselineHeight;
            AnimationHeight = animationHeight;
            HeightAbovePath = heightAbovePath;
            RequireValid();
        }

        public bool Available { get; }
        public int LandingOrdinal { get; }
        public float TimeSeconds { get; }
        public float Distance { get; }
        public float PathProgress { get; }
        public float BaselineHeight { get; }
        public float AnimationHeight { get; }
        public float HeightAbovePath { get; }

        public void RequireValid()
        {
            if (!float.IsFinite(TimeSeconds) || !float.IsFinite(Distance) ||
                !float.IsFinite(PathProgress) || !float.IsFinite(BaselineHeight) ||
                !float.IsFinite(AnimationHeight) || !float.IsFinite(HeightAbovePath) ||
                Available && (LandingOrdinal <= 0 || TimeSeconds < 0f || Distance < 0f ||
                              PathProgress < 0f || PathProgress > 1f || HeightAbovePath < 0f))
                throw new InvalidOperationException("Foot Motion step evidence is invalid.");
        }
    }

    public readonly struct AnimationFootMotionFilterEvidence
    {
        public AnimationFootMotionFilterEvidence(
            float toeHeight,
            float toeSpeed,
            float positionError,
            float rotationError,
            float contact)
        {
            ToeHeight = toeHeight;
            ToeSpeed = toeSpeed;
            PositionError = positionError;
            RotationError = rotationError;
            Contact = contact;
            if (!float.IsFinite(toeHeight) || !float.IsFinite(toeSpeed) || toeSpeed < 0f ||
                !float.IsFinite(positionError) || positionError < 0f ||
                !float.IsFinite(rotationError) || rotationError < 0f ||
                !float.IsFinite(contact) || contact < 0f || contact > 1f)
                throw new InvalidOperationException("Foot Motion filter evidence is invalid.");
        }

        public float ToeHeight { get; }
        public float ToeSpeed { get; }
        public float PositionError { get; }
        public float RotationError { get; }
        public float Contact { get; }
    }

    public readonly struct AnimationFootMotionConstraintEvidence
    {
        public AnimationFootMotionConstraintEvidence(
            AnimationFootLockMode lockMode,
            float lockWeight,
            float supportCandidate,
            float support)
        {
            LockMode = lockMode;
            LockWeight = lockWeight;
            SupportCandidate = supportCandidate;
            Support = support;
            if ((byte)lockMode > 2 || !float.IsFinite(lockWeight) || lockWeight < 0f || lockWeight > 1f ||
                !float.IsFinite(supportCandidate) || supportCandidate < 0f || supportCandidate > 1f ||
                !float.IsFinite(support) || support < 0f || support > 1f)
                throw new InvalidOperationException("Foot Motion constraint evidence is invalid.");
        }

        public AnimationFootLockMode LockMode { get; }
        public float LockWeight { get; }
        public float SupportCandidate { get; }
        public float Support { get; }
    }

    public readonly struct AnimationFootMotionDerivedSample
    {
        public AnimationFootMotionDerivedSample(
            float timeSeconds,
            AnimationFootMotionStepEvidence step,
            AnimationFootMotionFilterEvidence filter,
            AnimationFootMotionConstraintEvidence constraint)
        {
            TimeSeconds = timeSeconds;
            Step = step;
            Filter = filter;
            Constraint = constraint;
            if (!float.IsFinite(timeSeconds) || timeSeconds < 0f)
                throw new InvalidOperationException("Foot Motion derived sample time is invalid.");
            Step.RequireValid();
        }

        public float TimeSeconds { get; }
        public AnimationFootMotionStepEvidence Step { get; }
        public AnimationFootMotionFilterEvidence Filter { get; }
        public AnimationFootMotionConstraintEvidence Constraint { get; }
    }

    public sealed class AnimationFootMotionFootPage
    {
        readonly AnimationFootMotionEvent[] m_Events;
        readonly AnimationFootMotionDiagnostic[] m_Diagnostics;
        readonly AnimationFootMotionDerivedSample[] m_Samples;

        public AnimationFootMotionFootPage(
            AnimationFootMotionEvent[] events,
            AnimationFootMotionDiagnostic[] diagnostics,
            AnimationFootMotionDerivedSample[] samples,
            string diagnostic)
        {
            m_Events = events == null
                ? throw new ArgumentNullException(nameof(events))
                : (AnimationFootMotionEvent[])events.Clone();
            m_Samples = samples == null
                ? throw new ArgumentNullException(nameof(samples))
                : (AnimationFootMotionDerivedSample[])samples.Clone();
            m_Diagnostics = diagnostics == null
                ? throw new ArgumentNullException(nameof(diagnostics))
                : (AnimationFootMotionDiagnostic[])diagnostics.Clone();
            Diagnostic = diagnostic ?? string.Empty;
            RequireValid();
        }

        public IReadOnlyList<AnimationFootMotionEvent> Events => m_Events;
        public IReadOnlyList<AnimationFootMotionDiagnostic> Diagnostics => m_Diagnostics;
        public IReadOnlyList<AnimationFootMotionDerivedSample> Samples => m_Samples;
        public string Diagnostic { get; }
        public bool CanBuildCurves => string.IsNullOrEmpty(Diagnostic) && m_Samples.Length >= 3;

        public void RequireValid()
        {
            if (m_Samples.Length < 3)
                throw new InvalidOperationException("Foot Motion derived page requires at least three samples.");
            for (int i = 0; i < m_Samples.Length; i++)
            {
                if (i > 0 && m_Samples[i].TimeSeconds <= m_Samples[i - 1].TimeSeconds)
                    throw new InvalidOperationException("Foot Motion derived sample time is not increasing.");
            }
            for (int i = 1; i < m_Events.Length; i++)
            {
                if (m_Events[i].SampleIndex < m_Events[i - 1].SampleIndex)
                    throw new InvalidOperationException("Foot Motion event order is invalid.");
            }
        }
    }

    public sealed class AnimationFootMotionDataDescriptor
    {
        public AnimationFootMotionDataDescriptor(
            AnimationFootMotionRawPage raw,
            AnimationFootMotionFootPage left,
            AnimationFootMotionFootPage right)
        {
            Raw = raw ?? throw new ArgumentNullException(nameof(raw));
            Left = left ?? throw new ArgumentNullException(nameof(left));
            Right = right ?? throw new ArgumentNullException(nameof(right));
            Raw.RequireValid();
            Left.RequireValid();
            Right.RequireValid();
            if (Raw.RootSamples.Count != Left.Samples.Count || Raw.RootSamples.Count != Right.Samples.Count)
                throw new InvalidOperationException("Foot Motion data sample counts do not match.");
        }

        public AnimationFootMotionRawPage Raw { get; }
        public AnimationFootMotionFootPage Left { get; }
        public AnimationFootMotionFootPage Right { get; }
    }
}
