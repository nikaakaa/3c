using System;
using ThirdPersonCharacter.Pipeline.Presentation;
using Unity.Collections;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public readonly struct AnimationFootKinematicsSample
    {
        internal AnimationFootKinematicsSample(in AnimationFootFeatureSample source)
        {
            SoleLocalVelocity = source.SoleLocalVelocity;
            SoleHeight = source.SoleHeight;
            PlantConfidence = source.PlantConfidence;
            m_IsSpecified = source.IsValid ? (byte)1 : (byte)0;
        }

        readonly byte m_IsSpecified;
        public Vector3 SoleLocalVelocity { get; }
        public float SoleHeight { get; }
        public float PlantConfidence { get; }
        public bool IsValid => m_IsSpecified != 0;
    }

    public readonly struct AnimationBiomechanicalRoutePage
    {
        internal AnimationBiomechanicalRoutePage(
            FixedList512Bytes<Vector3> rootLocalFoot,
            FixedList512Bytes<Vector3> rootLocalAnkle,
            FixedList512Bytes<Vector3> rootLocalHip,
            FixedList512Bytes<Vector3> authoredFootPlanar,
            FixedList128Bytes<float> animationClearance,
            AnimationFootBiomechanicalRouteSample currentSample)
        {
            RootLocalFoot = rootLocalFoot;
            RootLocalAnkle = rootLocalAnkle;
            RootLocalHip = rootLocalHip;
            AuthoredFootPlanar = authoredFootPlanar;
            AnimationClearance = animationClearance;
            CurrentSample = currentSample;
            RequireValid();
        }

        public FixedList512Bytes<Vector3> RootLocalFoot { get; }
        public FixedList512Bytes<Vector3> RootLocalAnkle { get; }
        public FixedList512Bytes<Vector3> RootLocalHip { get; }
        public FixedList512Bytes<Vector3> AuthoredFootPlanar { get; }
        public FixedList128Bytes<float> AnimationClearance { get; }
        public AnimationFootBiomechanicalRouteSample CurrentSample { get; }
        public Vector3 RootLocalLanding => RootLocalFoot.Length > 0
            ? RootLocalFoot[RootLocalFoot.Length - 1]
            : Vector3.zero;
        public bool IsValid =>
            RootLocalFoot.Length == AnimationPredictedFootStepCurveSet.RouteSampleCount &&
            RootLocalAnkle.Length == AnimationPredictedFootStepCurveSet.RouteSampleCount &&
            RootLocalHip.Length == AnimationPredictedFootStepCurveSet.RouteSampleCount &&
            AuthoredFootPlanar.Length == AnimationPredictedFootStepCurveSet.RouteSampleCount &&
            AnimationClearance.Length == AnimationPredictedFootStepCurveSet.RouteSampleCount &&
            CurrentSample.IsValid;

        void RequireValid()
        {
            if (!IsValid)
                throw new ArgumentException("Biomechanical route page is incomplete.");
            for (int i = 0; i < RootLocalFoot.Length; i++)
            {
                RequireFinite(RootLocalFoot[i]);
                RequireFinite(RootLocalAnkle[i]);
                RequireFinite(RootLocalHip[i]);
                RequireFinite(AuthoredFootPlanar[i]);
                if (Mathf.Abs(AuthoredFootPlanar[i].y) > 0.00001f ||
                    !float.IsFinite(AnimationClearance[i]) || AnimationClearance[i] < -0.00001f)
                {
                    throw new ArgumentException("Biomechanical route page contains an invalid sample.");
                }
            }
        }

        static void RequireFinite(Vector3 value)
        {
            if (!float.IsFinite(value.x) || !float.IsFinite(value.y) || !float.IsFinite(value.z))
                throw new ArgumentException("Biomechanical route page contains a non-finite sample.");
        }
    }

    public readonly struct AnimationBiomechanicalStepHeader
    {
        internal AnimationBiomechanicalStepHeader(
            in AnimationPredictedFootStepSample source,
            CharacterFootSide side)
        {
            if (side != CharacterFootSide.Left && side != CharacterFootSide.Right)
                throw new ArgumentOutOfRangeException(nameof(side));
            EventOrdinal = source.EventOrdinal;
            SourceLandingCycleOffset = source.SourceLandingCycleOffset;
            Confidence = source.Confidence;
            TimeToLandingSeconds = source.TimeToLandingSeconds;
            EventPhase = source.EventPhase;
            ReleasePhase = source.ReleasePhase;
            LiftOffPhase = source.LiftOffPhase;
            ApproachContactPhase = source.ApproachContactPhase;
            LandingPhase = source.LandingPhase;
            ActionStepClock = source.ActionStepClock;
            SourceSampleIdentity = source.SourceSampleIdentity;
            SourceSampleCycle = source.SourceSampleCycle;
            ContributionContinuityIdentity = source.ContributionContinuityIdentity;
            LandingEventIdentity = source.LandingEventIdentity;
            RootLocalLanding = source.IsValid ? source.Route.RootLocalLanding : default;
            m_IsSpecified = source.IsValid ? (byte)1 : (byte)0;
            m_HasLandingEvent = source.HasLandingEvent ? (byte)1 : (byte)0;
            m_IsAuthoritative = source.IsAuthoritative ? (byte)1 : (byte)0;
            m_HasConsistentLandingEventIdentity =
                source.HasConsistentLandingEventIdentity(side) ? (byte)1 : (byte)0;
        }

        readonly byte m_IsSpecified;
        readonly byte m_HasLandingEvent;
        readonly byte m_IsAuthoritative;
        readonly byte m_HasConsistentLandingEventIdentity;
        public int EventOrdinal { get; }
        public int SourceLandingCycleOffset { get; }
        public float Confidence { get; }
        public float TimeToLandingSeconds { get; }
        public float EventPhase { get; }
        public float ReleasePhase { get; }
        public float LiftOffPhase { get; }
        public float ApproachContactPhase { get; }
        public float LandingPhase { get; }
        public AnimationActionStepClockSample ActionStepClock { get; }
        public ulong SourceSampleIdentity { get; }
        public int SourceSampleCycle { get; }
        public ulong ContributionContinuityIdentity { get; }
        public ulong LandingEventIdentity { get; }
        public Vector3 RootLocalLanding { get; }
        public bool IsValid => m_IsSpecified != 0;
        public bool HasLandingEvent => m_HasLandingEvent != 0;
        public bool IsAuthoritative => m_IsAuthoritative != 0;
        public bool HasConsistentLandingEventIdentity =>
            m_HasConsistentLandingEventIdentity != 0;
    }

    public readonly struct AnimationBiomechanicalStepReadPage
    {
        public AnimationBiomechanicalStepReadPage(
            in AnimationFootFeatureSample source,
            CharacterFootSide side)
        {
            if (!source.IsValid)
                throw new ArgumentException("Biomechanical step source is invalid.", nameof(source));
            Kinematics = new AnimationFootKinematicsSample(in source);
            ref readonly AnimationPredictedFootStepSample current = ref source.PredictedStep;
            ref readonly AnimationPredictedFootStepSample incoming = ref source.IncomingPredictedStep;
            CurrentStep = new AnimationBiomechanicalStepHeader(in current, side);
            IncomingStep = new AnimationBiomechanicalStepHeader(in incoming, side);
            m_IsSpecified = 1;
        }

        readonly byte m_IsSpecified;
        public AnimationFootKinematicsSample Kinematics { get; }
        public AnimationBiomechanicalStepHeader CurrentStep { get; }
        public AnimationBiomechanicalStepHeader IncomingStep { get; }
        public bool IsValid => m_IsSpecified != 0 && Kinematics.IsValid;
    }
}
