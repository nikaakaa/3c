using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Presentation;
using Unity.Collections;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public enum CharacterFootPlacementAnalysisMode : byte
    {
        Disabled = 0,
        GeneratedPerFootFeatures = 1
    }

    public enum AnimationFootConstraintMode : byte
    {
        Unlocked = 0,
        Sliding = 1,
        Locked = 2
    }

    public enum AnimationFootSupportPhase : byte
    {
        Unsupported = 0,
        ApproachingContact = 1,
        Supporting = 2,
        Releasing = 3
    }

    public enum AnimationFootOrientationPolicy : byte
    {
        PreserveAnimation = 0,
        LandingSurface = 1
    }

    public enum AnimationBodyRotationPivotMode : byte
    {
        Pelvis = 0,
        SupportFoot = 1
    }

    public static class AnimationFootConstraintFacts
    {
        public const float GroundedMinimumConfidence = 0.5f;
        public const float LockedMinimumConfidence = 0.75f;

        public static void RequirePhaseOrder(
            float releasePhase,
            float liftOffPhase,
            float approachContactPhase)
        {
            if (!float.IsFinite(releasePhase) || releasePhase < 0f || releasePhase > 1f)
                throw new ArgumentOutOfRangeException(nameof(releasePhase));
            if (!float.IsFinite(liftOffPhase) || liftOffPhase < releasePhase || liftOffPhase > 1f)
                throw new ArgumentOutOfRangeException(nameof(liftOffPhase));
            if (!float.IsFinite(approachContactPhase) ||
                approachContactPhase < liftOffPhase || approachContactPhase > 1f)
            {
                throw new ArgumentOutOfRangeException(nameof(approachContactPhase));
            }
        }

        public static AnimationFootConstraintMode ResolveConstraintMode(
            float eventPhase,
            float releasePhase,
            float liftOffPhase)
        {
            float phase = Mathf.Clamp01(eventPhase);
            return phase < releasePhase
                ? AnimationFootConstraintMode.Locked
                : phase < liftOffPhase
                    ? AnimationFootConstraintMode.Sliding
                    : AnimationFootConstraintMode.Unlocked;
        }

        public static AnimationFootSupportPhase ResolveSupportPhase(
            float eventPhase,
            float releasePhase,
            float liftOffPhase,
            float approachContactPhase)
        {
            float phase = Mathf.Clamp01(eventPhase);
            return phase < releasePhase
                ? AnimationFootSupportPhase.Supporting
                : phase < liftOffPhase
                    ? AnimationFootSupportPhase.Releasing
                    : phase < approachContactPhase
                        ? AnimationFootSupportPhase.Unsupported
                        : AnimationFootSupportPhase.ApproachingContact;
        }

        public static AnimationBodyRotationPivotMode ResolveBodyPivotMode(
            float eventPhase,
            float liftOffPhase) =>
            Mathf.Clamp01(eventPhase) < liftOffPhase
                ? AnimationBodyRotationPivotMode.SupportFoot
                : AnimationBodyRotationPivotMode.Pelvis;
    }

    public readonly struct AnimationActionStepClockSample
    {
        public AnimationActionStepClockSample(
            float phase,
            float liftOffPhase,
            float durationSeconds,
            float timeToLandingSeconds)
        {
            Phase = RequireNormalized(phase, nameof(phase));
            LiftOffPhase = RequireNormalized(liftOffPhase, nameof(liftOffPhase));
            DurationSeconds = RequireNonNegative(durationSeconds, nameof(durationSeconds));
            TimeToLandingSeconds = RequireNonNegative(timeToLandingSeconds, nameof(timeToLandingSeconds));
        }

        public float Phase { get; }
        public float LiftOffPhase { get; }
        public float DurationSeconds { get; }
        public float TimeToLandingSeconds { get; }
        public bool IsPreSwing => Phase < LiftOffPhase;
        public bool IsSwing => Phase >= LiftOffPhase && Phase < 0.9999f;

        static float RequireNormalized(float value, string field)
        {
            if (!float.IsFinite(value) || value < 0f || value > 1f)
                throw new ArgumentOutOfRangeException(field);
            return value;
        }

        static float RequireNonNegative(float value, string field)
        {
            if (!float.IsFinite(value) || value < 0f)
                throw new ArgumentOutOfRangeException(field);
            return value;
        }
    }

    [Serializable]
    public sealed class AnimationPredictedFootStepCurveSet
    {
        public const int RouteSampleCount = 25;

        [SerializeField] AnimationCurve m_Confidence;
        [SerializeField] AnimationCurve m_TimeToLandingSeconds;
        [SerializeField] AnimationCurve m_EventPhase;
        [SerializeField] AnimationCurve m_ReleasePhase;
        [SerializeField] AnimationCurve m_LiftOffPhase;
        [SerializeField] AnimationCurve m_ApproachContactPhase;
        [SerializeField] AnimationCurve m_ActionStepDurationSeconds;
        [SerializeField] AnimationCurve m_EventOrdinal;
        [SerializeField] AnimationCurve m_SourceLandingCycleOffset;
        [SerializeField] AnimationCurve m_OpposingLandingDelaySeconds;
        [SerializeField] AnimationCurve m_OpposingEventOrdinal;
        [SerializeField] AnimationCurve m_OpposingLandingCycleOffset;
        [SerializeField] AnimationCurve m_OpposingRootLocalLandingX;
        [SerializeField] AnimationCurve m_OpposingRootLocalLandingY;
        [SerializeField] AnimationCurve m_OpposingRootLocalLandingZ;
        [SerializeField] AnimationCurve[] m_RootLocalFootRouteX = Array.Empty<AnimationCurve>();
        [SerializeField] AnimationCurve[] m_RootLocalFootRouteY = Array.Empty<AnimationCurve>();
        [SerializeField] AnimationCurve[] m_RootLocalFootRouteZ = Array.Empty<AnimationCurve>();
        [SerializeField] AnimationCurve[] m_RootLocalAnkleRouteX = Array.Empty<AnimationCurve>();
        [SerializeField] AnimationCurve[] m_RootLocalAnkleRouteY = Array.Empty<AnimationCurve>();
        [SerializeField] AnimationCurve[] m_RootLocalAnkleRouteZ = Array.Empty<AnimationCurve>();
        [SerializeField] AnimationCurve[] m_RootLocalHipRouteX = Array.Empty<AnimationCurve>();
        [SerializeField] AnimationCurve[] m_RootLocalHipRouteY = Array.Empty<AnimationCurve>();
        [SerializeField] AnimationCurve[] m_RootLocalHipRouteZ = Array.Empty<AnimationCurve>();
        [SerializeField] AnimationCurve[] m_AuthoredFootPlanarRouteX = Array.Empty<AnimationCurve>();
        [SerializeField] AnimationCurve[] m_AuthoredFootPlanarRouteZ = Array.Empty<AnimationCurve>();
        [SerializeField] AnimationCurve[] m_AnimationClearanceHeight = Array.Empty<AnimationCurve>();
        [SerializeField] AnimationFootBiomechanicalStepCurveSet m_BiomechanicalStep;

        public AnimationPredictedFootStepCurveSet(
            AnimationCurve confidence,
            AnimationCurve timeToLandingSeconds,
            AnimationCurve eventPhase,
            AnimationCurve releasePhase,
            AnimationCurve liftOffPhase,
            AnimationCurve approachContactPhase,
            AnimationCurve actionStepDurationSeconds,
            AnimationCurve eventOrdinal,
            AnimationCurve sourceLandingCycleOffset,
            AnimationCurve opposingLandingDelaySeconds,
            AnimationCurve opposingEventOrdinal,
            AnimationCurve opposingLandingCycleOffset,
            AnimationCurve opposingRootLocalLandingX,
            AnimationCurve opposingRootLocalLandingY,
            AnimationCurve opposingRootLocalLandingZ,
            AnimationCurve[] rootLocalFootRouteX,
            AnimationCurve[] rootLocalFootRouteY,
            AnimationCurve[] rootLocalFootRouteZ,
            AnimationCurve[] rootLocalAnkleRouteX,
            AnimationCurve[] rootLocalAnkleRouteY,
            AnimationCurve[] rootLocalAnkleRouteZ,
            AnimationCurve[] rootLocalHipRouteX,
            AnimationCurve[] rootLocalHipRouteY,
            AnimationCurve[] rootLocalHipRouteZ,
            AnimationCurve[] authoredFootPlanarRouteX,
            AnimationCurve[] authoredFootPlanarRouteZ,
            AnimationCurve[] animationClearanceHeight,
            AnimationFootBiomechanicalStepCurveSet biomechanicalStep)
        {
            m_Confidence = Copy(confidence);
            m_TimeToLandingSeconds = Copy(timeToLandingSeconds);
            m_EventPhase = Copy(eventPhase);
            m_ReleasePhase = Copy(releasePhase);
            m_LiftOffPhase = Copy(liftOffPhase);
            m_ApproachContactPhase = Copy(approachContactPhase);
            m_ActionStepDurationSeconds = Copy(actionStepDurationSeconds);
            m_EventOrdinal = Copy(eventOrdinal);
            m_SourceLandingCycleOffset = Copy(sourceLandingCycleOffset);
            m_OpposingLandingDelaySeconds = Copy(opposingLandingDelaySeconds);
            m_OpposingEventOrdinal = Copy(opposingEventOrdinal);
            m_OpposingLandingCycleOffset = Copy(opposingLandingCycleOffset);
            m_OpposingRootLocalLandingX = Copy(opposingRootLocalLandingX);
            m_OpposingRootLocalLandingY = Copy(opposingRootLocalLandingY);
            m_OpposingRootLocalLandingZ = Copy(opposingRootLocalLandingZ);
            m_RootLocalFootRouteX = CopyRoute(rootLocalFootRouteX);
            m_RootLocalFootRouteY = CopyRoute(rootLocalFootRouteY);
            m_RootLocalFootRouteZ = CopyRoute(rootLocalFootRouteZ);
            m_RootLocalAnkleRouteX = CopyRoute(rootLocalAnkleRouteX);
            m_RootLocalAnkleRouteY = CopyRoute(rootLocalAnkleRouteY);
            m_RootLocalAnkleRouteZ = CopyRoute(rootLocalAnkleRouteZ);
            m_RootLocalHipRouteX = CopyRoute(rootLocalHipRouteX);
            m_RootLocalHipRouteY = CopyRoute(rootLocalHipRouteY);
            m_RootLocalHipRouteZ = CopyRoute(rootLocalHipRouteZ);
            m_AuthoredFootPlanarRouteX = CopyRoute(authoredFootPlanarRouteX);
            m_AuthoredFootPlanarRouteZ = CopyRoute(authoredFootPlanarRouteZ);
            m_AnimationClearanceHeight = CopyRoute(animationClearanceHeight);
            m_BiomechanicalStep = biomechanicalStep ??
                throw new ArgumentNullException(nameof(biomechanicalStep));
            RequireValid();
        }

        public AnimationCurve Confidence => m_Confidence;
        public AnimationCurve TimeToLandingSeconds => m_TimeToLandingSeconds;
        public AnimationCurve EventPhase => m_EventPhase;
        public AnimationCurve ReleasePhase => m_ReleasePhase;
        public AnimationCurve LiftOffPhase => m_LiftOffPhase;
        public AnimationCurve ApproachContactPhase => m_ApproachContactPhase;
        public AnimationCurve ActionStepDurationSeconds => m_ActionStepDurationSeconds;
        public AnimationCurve EventOrdinal => m_EventOrdinal;
        public AnimationCurve SourceLandingCycleOffset => m_SourceLandingCycleOffset;
        public AnimationCurve OpposingLandingDelaySeconds => m_OpposingLandingDelaySeconds;
        public AnimationCurve OpposingEventOrdinal => m_OpposingEventOrdinal;
        public AnimationCurve OpposingLandingCycleOffset => m_OpposingLandingCycleOffset;
        public AnimationCurve OpposingRootLocalLandingX => m_OpposingRootLocalLandingX;
        public AnimationCurve OpposingRootLocalLandingY => m_OpposingRootLocalLandingY;
        public AnimationCurve OpposingRootLocalLandingZ => m_OpposingRootLocalLandingZ;
        public AnimationCurve GetRootLocalFootRouteX(int index) => GetRouteCurve(m_RootLocalFootRouteX, index);
        public AnimationCurve GetRootLocalFootRouteY(int index) => GetRouteCurve(m_RootLocalFootRouteY, index);
        public AnimationCurve GetRootLocalFootRouteZ(int index) => GetRouteCurve(m_RootLocalFootRouteZ, index);
        public AnimationCurve GetRootLocalAnkleRouteX(int index) => GetRouteCurve(m_RootLocalAnkleRouteX, index);
        public AnimationCurve GetRootLocalAnkleRouteY(int index) => GetRouteCurve(m_RootLocalAnkleRouteY, index);
        public AnimationCurve GetRootLocalAnkleRouteZ(int index) => GetRouteCurve(m_RootLocalAnkleRouteZ, index);
        public AnimationCurve GetRootLocalHipRouteX(int index) => GetRouteCurve(m_RootLocalHipRouteX, index);
        public AnimationCurve GetRootLocalHipRouteY(int index) => GetRouteCurve(m_RootLocalHipRouteY, index);
        public AnimationCurve GetRootLocalHipRouteZ(int index) => GetRouteCurve(m_RootLocalHipRouteZ, index);
        public AnimationCurve GetAuthoredFootPlanarRouteX(int index) => GetRouteCurve(m_AuthoredFootPlanarRouteX, index);
        public AnimationCurve GetAuthoredFootPlanarRouteZ(int index) => GetRouteCurve(m_AuthoredFootPlanarRouteZ, index);
        public AnimationCurve GetAnimationClearanceHeight(int index) => GetRouteCurve(m_AnimationClearanceHeight, index);
        public AnimationFootBiomechanicalStepCurveSet BiomechanicalStep => m_BiomechanicalStep;

        public AnimationPredictedFootStepSample Sample(float normalizedTime)
        {
            RequireValid();
            return SamplePrepared(normalizedTime);
        }

        internal AnimationPredictedFootStepSample SamplePrepared(float normalizedTime)
        {
            float time = Mathf.Clamp01(normalizedTime);
            var rootLocalFootRoute = new FixedList512Bytes<Vector3>();
            var rootLocalAnkleRoute = new FixedList512Bytes<Vector3>();
            var rootLocalHipRoute = new FixedList512Bytes<Vector3>();
            var authoredFootPlanarRoute = new FixedList512Bytes<Vector3>();
            var animationClearanceHeights = new FixedList128Bytes<float>();
            for (int i = 0; i < RouteSampleCount; i++)
            {
                rootLocalFootRoute.Add(new Vector3(
                    m_RootLocalFootRouteX[i].Evaluate(time),
                    m_RootLocalFootRouteY[i].Evaluate(time),
                    m_RootLocalFootRouteZ[i].Evaluate(time)));
                rootLocalAnkleRoute.Add(new Vector3(
                    m_RootLocalAnkleRouteX[i].Evaluate(time),
                    m_RootLocalAnkleRouteY[i].Evaluate(time),
                    m_RootLocalAnkleRouteZ[i].Evaluate(time)));
                rootLocalHipRoute.Add(new Vector3(
                    m_RootLocalHipRouteX[i].Evaluate(time),
                    m_RootLocalHipRouteY[i].Evaluate(time),
                    m_RootLocalHipRouteZ[i].Evaluate(time)));
                authoredFootPlanarRoute.Add(new Vector3(
                    m_AuthoredFootPlanarRouteX[i].Evaluate(time),
                    0f,
                    m_AuthoredFootPlanarRouteZ[i].Evaluate(time)));
                animationClearanceHeights.Add(m_AnimationClearanceHeight[i].Evaluate(time));
            }
            float eventPhase = m_EventPhase.Evaluate(time);
            m_BiomechanicalStep.Sample(
                time,
                out float landingPhase,
                out Quaternion opposingRootLocalSoleRotation,
                out FixedList4096Bytes<AnimationFootBiomechanicalRouteSample> biomechanicalRoute);
            float scaledBiomechanicalIndex = Mathf.Clamp01(eventPhase) * (biomechanicalRoute.Length - 1);
            int firstBiomechanicalIndex = Mathf.Min(
                biomechanicalRoute.Length - 1,
                Mathf.FloorToInt(scaledBiomechanicalIndex));
            int secondBiomechanicalIndex = Mathf.Min(
                biomechanicalRoute.Length - 1,
                firstBiomechanicalIndex + 1);
            AnimationFootBiomechanicalRouteSample biomechanicalSample =
                AnimationFootBiomechanicalRouteSample.Interpolate(
                    biomechanicalRoute[firstBiomechanicalIndex],
                    biomechanicalRoute[secondBiomechanicalIndex],
                    scaledBiomechanicalIndex - firstBiomechanicalIndex);
            return new AnimationPredictedFootStepSample(
                Mathf.Max(0, Mathf.RoundToInt(m_EventOrdinal.Evaluate(time))),
                Mathf.Max(0, Mathf.RoundToInt(m_SourceLandingCycleOffset.Evaluate(time))),
                m_Confidence.Evaluate(time),
                m_TimeToLandingSeconds.Evaluate(time),
                eventPhase,
                m_ReleasePhase.Evaluate(time),
                m_LiftOffPhase.Evaluate(time),
                m_ApproachContactPhase.Evaluate(time),
                m_ActionStepDurationSeconds.Evaluate(time),
                Mathf.Max(0, Mathf.RoundToInt(m_OpposingEventOrdinal.Evaluate(time))),
                m_OpposingLandingDelaySeconds.Evaluate(time),
                Mathf.RoundToInt(m_OpposingLandingCycleOffset.Evaluate(time)),
                new Vector3(
                    m_OpposingRootLocalLandingX.Evaluate(time),
                    m_OpposingRootLocalLandingY.Evaluate(time),
                    m_OpposingRootLocalLandingZ.Evaluate(time)),
                rootLocalFootRoute,
                rootLocalAnkleRoute,
                rootLocalHipRoute,
                authoredFootPlanarRoute,
                animationClearanceHeights,
                landingPhase,
                opposingRootLocalSoleRotation,
                biomechanicalSample);
        }

        public void RequireValid()
        {
            RequireCurve(m_Confidence, nameof(m_Confidence), true, false);
            RequireCurve(m_TimeToLandingSeconds, nameof(m_TimeToLandingSeconds), false, true);
            RequireCurve(m_EventPhase, nameof(m_EventPhase), true, false);
            RequireCurve(m_ReleasePhase, nameof(m_ReleasePhase), true, false);
            RequireCurve(m_LiftOffPhase, nameof(m_LiftOffPhase), true, false);
            RequireCurve(m_ApproachContactPhase, nameof(m_ApproachContactPhase), true, false);
            RequireCurve(m_ActionStepDurationSeconds, nameof(m_ActionStepDurationSeconds), false, true);
            RequireCurve(m_EventOrdinal, nameof(m_EventOrdinal), false, true);
            RequireCurve(m_SourceLandingCycleOffset, nameof(m_SourceLandingCycleOffset), false, true);
            RequireCurve(m_OpposingLandingDelaySeconds, nameof(m_OpposingLandingDelaySeconds), false, true);
            RequireCurve(m_OpposingEventOrdinal, nameof(m_OpposingEventOrdinal), false, true);
            RequireCurve(m_OpposingLandingCycleOffset, nameof(m_OpposingLandingCycleOffset), false, false);
            RequireCurve(m_OpposingRootLocalLandingX, nameof(m_OpposingRootLocalLandingX), false, false);
            RequireCurve(m_OpposingRootLocalLandingY, nameof(m_OpposingRootLocalLandingY), false, false);
            RequireCurve(m_OpposingRootLocalLandingZ, nameof(m_OpposingRootLocalLandingZ), false, false);
            RequireRoute(m_RootLocalFootRouteX, nameof(m_RootLocalFootRouteX));
            RequireRoute(m_RootLocalFootRouteY, nameof(m_RootLocalFootRouteY));
            RequireRoute(m_RootLocalFootRouteZ, nameof(m_RootLocalFootRouteZ));
            RequireRoute(m_RootLocalAnkleRouteX, nameof(m_RootLocalAnkleRouteX));
            RequireRoute(m_RootLocalAnkleRouteY, nameof(m_RootLocalAnkleRouteY));
            RequireRoute(m_RootLocalAnkleRouteZ, nameof(m_RootLocalAnkleRouteZ));
            RequireRoute(m_RootLocalHipRouteX, nameof(m_RootLocalHipRouteX));
            RequireRoute(m_RootLocalHipRouteY, nameof(m_RootLocalHipRouteY));
            RequireRoute(m_RootLocalHipRouteZ, nameof(m_RootLocalHipRouteZ));
            RequireRoute(m_AuthoredFootPlanarRouteX, nameof(m_AuthoredFootPlanarRouteX));
            RequireRoute(m_AuthoredFootPlanarRouteZ, nameof(m_AuthoredFootPlanarRouteZ));
            RequireRoute(m_AnimationClearanceHeight, nameof(m_AnimationClearanceHeight));
            if (m_BiomechanicalStep == null)
                throw new InvalidOperationException("Foot Analysis biomechanical step curves are missing.");
            m_BiomechanicalStep.RequireValid();
        }

        static void RequireRoute(AnimationCurve[] route, string field)
        {
            if (route == null || route.Length != RouteSampleCount)
                throw new InvalidOperationException($"Foot Analysis route '{field}' has invalid capacity.");
            for (int i = 0; i < route.Length; i++)
                RequireCurve(route[i], $"{field}[{i}]", false, false);
        }

        static AnimationCurve GetRouteCurve(AnimationCurve[] route, int index)
        {
            if (route == null || index < 0 || index >= route.Length)
                throw new ArgumentOutOfRangeException(nameof(index));
            return route[index];
        }

        static AnimationCurve[] CopyRoute(AnimationCurve[] source)
        {
            if (source == null)
                return null;
            var result = new AnimationCurve[source.Length];
            for (int i = 0; i < source.Length; i++)
                result[i] = Copy(source[i]);
            return result;
        }

        internal static void RequireCurve(AnimationCurve curve, string field, bool normalized, bool nonNegative)
        {
            if (curve == null || curve.length == 0)
                throw new InvalidOperationException($"Foot Analysis curve '{field}' is missing.");
            Keyframe[] keys = curve.keys;
            if (!Mathf.Approximately(keys[0].time, 0f) || !Mathf.Approximately(keys[keys.Length - 1].time, 1f))
                throw new InvalidOperationException($"Foot Analysis curve '{field}' must preserve normalized endpoints.");
            float previous = -1f;
            for (int i = 0; i < keys.Length; i++)
            {
                Keyframe key = keys[i];
                if (!float.IsFinite(key.time) || !float.IsFinite(key.value) || key.time < 0f || key.time > 1f || key.time <= previous)
                    throw new InvalidOperationException($"Foot Analysis curve '{field}' key #{i} is invalid.");
                if (normalized && (key.value < 0f || key.value > 1f) || nonNegative && key.value < 0f)
                    throw new InvalidOperationException($"Foot Analysis curve '{field}' key #{i} is outside its value domain.");
                previous = key.time;
            }
        }

        internal static AnimationCurve Copy(AnimationCurve source)
        {
            if (source == null)
                return null;
            return new AnimationCurve(source.keys)
            {
                preWrapMode = source.preWrapMode,
                postWrapMode = source.postWrapMode
            };
        }
    }

    [Serializable]
    public sealed class AnimationFootFeatureCurveSet
    {
        [SerializeField] AnimationCurve m_SoleLocalVelocityX;
        [SerializeField] AnimationCurve m_SoleLocalVelocityY;
        [SerializeField] AnimationCurve m_SoleLocalVelocityZ;
        [SerializeField] AnimationCurve m_SoleHeight;
        [SerializeField] AnimationCurve m_PlantConfidence;
        [SerializeField] AnimationPredictedFootStepCurveSet m_PredictedStep;
        [SerializeField] AnimationPredictedFootStepCurveSet m_IncomingPredictedStep;

        public AnimationFootFeatureCurveSet(
            AnimationCurve soleLocalVelocityX,
            AnimationCurve soleLocalVelocityY,
            AnimationCurve soleLocalVelocityZ,
            AnimationCurve soleHeight,
            AnimationCurve plantConfidence,
            AnimationPredictedFootStepCurveSet predictedStep,
            AnimationPredictedFootStepCurveSet incomingPredictedStep)
        {
            m_SoleLocalVelocityX = Copy(soleLocalVelocityX);
            m_SoleLocalVelocityY = Copy(soleLocalVelocityY);
            m_SoleLocalVelocityZ = Copy(soleLocalVelocityZ);
            m_SoleHeight = Copy(soleHeight);
            m_PlantConfidence = Copy(plantConfidence);
            m_PredictedStep = predictedStep ?? throw new ArgumentNullException(nameof(predictedStep));
            m_IncomingPredictedStep = incomingPredictedStep ??
                throw new ArgumentNullException(nameof(incomingPredictedStep));
            RequireValid();
        }

        public AnimationCurve SoleLocalVelocityX => m_SoleLocalVelocityX;
        public AnimationCurve SoleLocalVelocityY => m_SoleLocalVelocityY;
        public AnimationCurve SoleLocalVelocityZ => m_SoleLocalVelocityZ;
        public AnimationCurve SoleHeight => m_SoleHeight;
        public AnimationCurve PlantConfidence => m_PlantConfidence;
        public AnimationPredictedFootStepCurveSet PredictedStep => m_PredictedStep;
        public AnimationPredictedFootStepCurveSet IncomingPredictedStep => m_IncomingPredictedStep;

        public AnimationFootFeatureSample Sample(float normalizedTime)
        {
            RequireValid();
            return SamplePrepared(normalizedTime);
        }

        internal AnimationFootFeatureSample SamplePrepared(float normalizedTime)
        {
            float time = Mathf.Clamp01(normalizedTime);
            return new AnimationFootFeatureSample(
                new Vector3(
                    m_SoleLocalVelocityX.Evaluate(time),
                    m_SoleLocalVelocityY.Evaluate(time),
                    m_SoleLocalVelocityZ.Evaluate(time)),
                m_SoleHeight.Evaluate(time),
                m_PlantConfidence.Evaluate(time),
                m_PredictedStep.SamplePrepared(time),
                m_IncomingPredictedStep.SamplePrepared(time));
        }

        public void RequireValid()
        {
            RequireCurve(m_SoleLocalVelocityX, nameof(m_SoleLocalVelocityX), false, false);
            RequireCurve(m_SoleLocalVelocityY, nameof(m_SoleLocalVelocityY), false, false);
            RequireCurve(m_SoleLocalVelocityZ, nameof(m_SoleLocalVelocityZ), false, false);
            RequireCurve(m_SoleHeight, nameof(m_SoleHeight), false, false);
            RequireCurve(m_PlantConfidence, nameof(m_PlantConfidence), true, false);
            if (m_PredictedStep == null || m_IncomingPredictedStep == null)
                throw new InvalidOperationException("Foot Analysis current or incoming step curves are missing.");
            m_PredictedStep.RequireValid();
            m_IncomingPredictedStep.RequireValid();
        }

        static void RequireCurve(AnimationCurve curve, string field, bool normalized, bool nonNegative) =>
            AnimationPredictedFootStepCurveSet.RequireCurve(curve, field, normalized, nonNegative);

        static AnimationCurve Copy(AnimationCurve source) =>
            AnimationPredictedFootStepCurveSet.Copy(source);
    }

    public readonly struct AnimationPredictedFootStepSample
    {
        public AnimationPredictedFootStepSample(
            int eventOrdinal,
            int sourceLandingCycleOffset,
            float confidence,
            float timeToLandingSeconds,
            float eventPhase,
            float releasePhase,
            float liftOffPhase,
            float approachContactPhase,
            float actionStepDurationSeconds,
            int opposingEventOrdinal,
            float opposingLandingDelaySeconds,
            int opposingLandingCycleOffset,
            Vector3 opposingRootLocalLanding,
            FixedList512Bytes<Vector3> rootLocalFootRoute,
            FixedList512Bytes<Vector3> rootLocalAnkleRoute,
            FixedList512Bytes<Vector3> rootLocalHipRoute,
            FixedList512Bytes<Vector3> authoredFootPlanarRoute,
            FixedList128Bytes<float> animationClearanceHeights,
            float landingPhase,
            Quaternion opposingRootLocalSoleRotation,
            AnimationFootBiomechanicalRouteSample biomechanicalSample)
            : this(
                eventOrdinal,
                sourceLandingCycleOffset,
                confidence,
                timeToLandingSeconds,
                eventPhase,
                releasePhase,
                liftOffPhase,
                approachContactPhase,
                actionStepDurationSeconds,
                opposingEventOrdinal,
                opposingLandingDelaySeconds,
                opposingLandingCycleOffset,
                opposingRootLocalLanding,
                rootLocalFootRoute,
                rootLocalAnkleRoute,
                rootLocalHipRoute,
                authoredFootPlanarRoute,
                animationClearanceHeights,
                landingPhase,
                opposingRootLocalSoleRotation,
                biomechanicalSample,
                0,
                0,
                0,
                0,
                0,
                false)
        {
        }

        AnimationPredictedFootStepSample(
            int eventOrdinal,
            int sourceLandingCycleOffset,
            float confidence,
            float timeToLandingSeconds,
            float eventPhase,
            float releasePhase,
            float liftOffPhase,
            float approachContactPhase,
            float actionStepDurationSeconds,
            int opposingEventOrdinal,
            float opposingLandingDelaySeconds,
            int opposingLandingCycleOffset,
            Vector3 opposingRootLocalLanding,
            FixedList512Bytes<Vector3> rootLocalFootRoute,
            FixedList512Bytes<Vector3> rootLocalAnkleRoute,
            FixedList512Bytes<Vector3> rootLocalHipRoute,
            FixedList512Bytes<Vector3> authoredFootPlanarRoute,
            FixedList128Bytes<float> animationClearanceHeights,
            float landingPhase,
            Quaternion opposingRootLocalSoleRotation,
            AnimationFootBiomechanicalRouteSample biomechanicalSample,
            ulong sourceSampleIdentity,
            int sourceSampleCycle,
            ulong contributionContinuityIdentity,
            ulong landingEventIdentity,
            ulong opposingLandingEventIdentity,
            bool usesSynchronizedMarkerIdentity)
        {
            if (eventOrdinal < 0)
                throw new ArgumentOutOfRangeException(nameof(eventOrdinal));
            if (sourceLandingCycleOffset < 0)
                throw new ArgumentOutOfRangeException(nameof(sourceLandingCycleOffset));
            if (opposingEventOrdinal < 0 || opposingLandingCycleOffset < -1 || opposingLandingCycleOffset > 1)
                throw new ArgumentOutOfRangeException(nameof(opposingEventOrdinal));
            EventOrdinal = eventOrdinal;
            SourceLandingCycleOffset = sourceLandingCycleOffset;
            Confidence = RequireNormalized(confidence, nameof(confidence));
            TimeToLandingSeconds = RequireNonNegative(timeToLandingSeconds, nameof(timeToLandingSeconds));
            EventPhase = RequireNormalized(eventPhase, nameof(eventPhase));
            ReleasePhase = RequireNormalized(releasePhase, nameof(releasePhase));
            LiftOffPhase = RequireNormalized(liftOffPhase, nameof(liftOffPhase));
            ApproachContactPhase = RequireNormalized(
                approachContactPhase,
                nameof(approachContactPhase));
            LandingPhase = RequireNormalized(landingPhase, nameof(landingPhase));
            AnimationFootConstraintFacts.RequirePhaseOrder(
                ReleasePhase,
                LiftOffPhase,
                ApproachContactPhase);
            if (LandingPhase + 0.000001f < ApproachContactPhase)
                throw new ArgumentOutOfRangeException(nameof(landingPhase));
            ActionStepClock = new AnimationActionStepClockSample(
                EventPhase,
                LiftOffPhase,
                actionStepDurationSeconds,
                TimeToLandingSeconds);
            OpposingEventOrdinal = opposingEventOrdinal;
            OpposingLandingDelaySeconds = RequireNonNegative(
                opposingLandingDelaySeconds,
                nameof(opposingLandingDelaySeconds));
            OpposingLandingCycleOffset = opposingLandingCycleOffset;
            bool hasOpposingLanding = OpposingEventOrdinal != 0;
            if (hasOpposingLanding != (OpposingLandingDelaySeconds > 0f) ||
                !hasOpposingLanding && OpposingLandingCycleOffset != 0)
                throw new ArgumentException("Predicted opposing landing pair is incomplete.");
            OpposingRootLocalLanding = RequireFinite(
                opposingRootLocalLanding,
                nameof(opposingRootLocalLanding));
            OpposingRootLocalSoleRotation = RequireFinite(
                opposingRootLocalSoleRotation,
                nameof(opposingRootLocalSoleRotation));
            if (!hasOpposingLanding && OpposingRootLocalLanding.sqrMagnitude > 0.000000000001f)
                throw new ArgumentException("Predicted opposing landing position is unpaired.");
            if (!hasOpposingLanding && Quaternion.Angle(
                    OpposingRootLocalSoleRotation,
                    Quaternion.identity) > 0.0001f)
            {
                throw new ArgumentException("Predicted opposing landing rotation is unpaired.");
            }
            Route = new AnimationBiomechanicalRoutePage(
                rootLocalFootRoute,
                rootLocalAnkleRoute,
                rootLocalHipRoute,
                authoredFootPlanarRoute,
                animationClearanceHeights,
                biomechanicalSample);
            SourceSampleIdentity = sourceSampleIdentity;
            SourceSampleCycle = sourceSampleCycle;
            ContributionContinuityIdentity = contributionContinuityIdentity;
            LandingEventIdentity = landingEventIdentity;
            OpposingLandingEventIdentity = opposingLandingEventIdentity;
            m_UsesSynchronizedMarkerIdentity = usesSynchronizedMarkerIdentity ? (byte)1 : (byte)0;
            m_IsSpecified = 1;
        }

        readonly byte m_IsSpecified;
        readonly byte m_UsesSynchronizedMarkerIdentity;
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
        public int OpposingEventOrdinal { get; }
        public float OpposingLandingDelaySeconds { get; }
        public int OpposingLandingCycleOffset { get; }
        public Vector3 OpposingRootLocalLanding { get; }
        public Quaternion OpposingRootLocalSoleRotation { get; }
        public readonly AnimationBiomechanicalRoutePage Route;
        public Vector3 RootLocalLanding => Route.RootLocalLanding;
        public ulong SourceSampleIdentity { get; }
        public int SourceSampleCycle { get; }
        public ulong ContributionContinuityIdentity { get; }
        public ulong LandingEventIdentity { get; }
        public ulong OpposingLandingEventIdentity { get; }
        public bool IsValid => m_IsSpecified != 0;
        public bool HasLandingEvent => IsValid && EventOrdinal > 0 && Confidence > 0f;
        public bool IsSourceBound => HasLandingEvent && SourceSampleIdentity != 0;
        public bool IsAuthoritative => IsSourceBound && ContributionContinuityIdentity != 0 &&
                                       LandingEventIdentity != 0 && Route.IsValid;
        public bool HasOpposingLandingEvent => IsAuthoritative && OpposingEventOrdinal > 0 &&
                                               OpposingLandingDelaySeconds > 0.000001f &&
                                               OpposingLandingEventIdentity != 0;
        public bool IsPreSwing => IsAuthoritative && ActionStepClock.IsPreSwing;
        public bool IsSwing => IsAuthoritative && ActionStepClock.IsSwing;
        public float PredictionLeadSeconds => Mathf.Max(
            0f,
            TimeToLandingSeconds -
            (1f - EventPhase) * ActionStepClock.DurationSeconds);

        public ulong ResolveExpectedLandingEventIdentity(CharacterFootSide side)
        {
            if (!IsSourceBound || ContributionContinuityIdentity == 0 ||
                side != CharacterFootSide.Left && side != CharacterFootSide.Right)
            {
                return 0;
            }
            return ResolveLandingEventIdentity(
                ResolveIdentityContribution(),
                SourceSampleIdentity,
                SourceSampleCycle,
                EventOrdinal,
                side);
        }

        public bool HasConsistentLandingEventIdentity(CharacterFootSide side) =>
            IsAuthoritative && LandingEventIdentity == ResolveExpectedLandingEventIdentity(side);

        public Vector3 EvaluateRootLocalFootRoute(float eventPhase)
        {
            if (Route.RootLocalFoot.Length != AnimationPredictedFootStepCurveSet.RouteSampleCount)
                throw new InvalidOperationException("Predicted foot route is unavailable.");
            float scaled = Mathf.Clamp01(eventPhase) * (Route.RootLocalFoot.Length - 1);
            int first = Mathf.Min(Route.RootLocalFoot.Length - 1, Mathf.FloorToInt(scaled));
            int second = Mathf.Min(Route.RootLocalFoot.Length - 1, first + 1);
            return Vector3.Lerp(Route.RootLocalFoot[first], Route.RootLocalFoot[second], scaled - first);
        }

        public Vector3 EvaluateRootLocalAnkleRoute(float eventPhase)
        {
            if (Route.RootLocalAnkle.Length != AnimationPredictedFootStepCurveSet.RouteSampleCount)
                throw new InvalidOperationException("Predicted ankle route is unavailable.");
            float scaled = Mathf.Clamp01(eventPhase) * (Route.RootLocalAnkle.Length - 1);
            int first = Mathf.Min(Route.RootLocalAnkle.Length - 1, Mathf.FloorToInt(scaled));
            int second = Mathf.Min(Route.RootLocalAnkle.Length - 1, first + 1);
            return Vector3.Lerp(Route.RootLocalAnkle[first], Route.RootLocalAnkle[second], scaled - first);
        }

        public Vector3 EvaluateRootLocalHipRoute(float eventPhase)
        {
            if (Route.RootLocalHip.Length != AnimationPredictedFootStepCurveSet.RouteSampleCount)
                throw new InvalidOperationException("Predicted hip route is unavailable.");
            float scaled = Mathf.Clamp01(eventPhase) * (Route.RootLocalHip.Length - 1);
            int first = Mathf.Min(Route.RootLocalHip.Length - 1, Mathf.FloorToInt(scaled));
            int second = Mathf.Min(Route.RootLocalHip.Length - 1, first + 1);
            return Vector3.Lerp(Route.RootLocalHip[first], Route.RootLocalHip[second], scaled - first);
        }

        public Vector3 EvaluateAuthoredFootPlanarRoute(float eventPhase) =>
            EvaluateVectorRoute(Route.AuthoredFootPlanar, eventPhase, "authored Foot planar route");

        public float EvaluateAnimationClearanceHeight(float eventPhase)
        {
            EvaluateIndices(Route.AnimationClearance.Length, eventPhase, out int first, out int second, out float t);
            return Mathf.Lerp(Route.AnimationClearance[first], Route.AnimationClearance[second], t);
        }

        public float CurrentConstraintWeight => Route.CurrentSample.IsValid
            ? Route.CurrentSample.ConstraintWeight
            : throw new InvalidOperationException("Predicted biomechanical Foot sample is unavailable.");

        public float CurrentSupportWeight => Route.CurrentSample.IsValid
            ? Route.CurrentSample.SupportWeight
            : throw new InvalidOperationException("Predicted biomechanical Foot sample is unavailable.");

        public float EvaluateConstraintWeight(float eventPhase)
        {
            float phase = Mathf.Clamp01(eventPhase);
            if (phase < ReleasePhase)
                return 1f;
            if (phase < LiftOffPhase)
                return 1f - Mathf.InverseLerp(ReleasePhase, LiftOffPhase, phase);
            if (phase < ApproachContactPhase)
                return 0f;
            return Mathf.InverseLerp(ApproachContactPhase, LandingPhase, phase);
        }

        public float EvaluateSupportWeight(float eventPhase) => EvaluateConstraintWeight(eventPhase);

        public AnimationFootConstraintMode EvaluateConstraintMode(float eventPhase) =>
            AnimationFootConstraintFacts.ResolveConstraintMode(
                eventPhase,
                ReleasePhase,
                LiftOffPhase);

        public AnimationFootSupportPhase EvaluateSupportPhase(float eventPhase) =>
            AnimationFootConstraintFacts.ResolveSupportPhase(
                eventPhase,
                ReleasePhase,
                LiftOffPhase,
                ApproachContactPhase);

        public AnimationFootOrientationPolicy EvaluateFootOrientationPolicy(float eventPhase) =>
            EvaluateSupportPhase(eventPhase) == AnimationFootSupportPhase.Unsupported
                ? AnimationFootOrientationPolicy.PreserveAnimation
                : AnimationFootOrientationPolicy.LandingSurface;

        public AnimationBodyRotationPivotMode EvaluateBodyRotationPivotMode(float eventPhase) =>
            AnimationFootConstraintFacts.ResolveBodyPivotMode(eventPhase, LiftOffPhase);

        public static ulong SourceIdentity(AnimationPoseSourceId sourceId, string discriminator = null)
        {
            if (!sourceId.IsValid)
                throw new ArgumentException("Predicted foot step pose source identity is invalid.", nameof(sourceId));
            ulong value = HashText(sourceId.ToString());
            return string.IsNullOrEmpty(discriminator)
                ? value
                : Hash(value, HashText(discriminator), 0, 0, 0);
        }

        public static ulong SourceIdentity(string stableIdentity)
        {
            if (string.IsNullOrWhiteSpace(stableIdentity))
                throw new ArgumentException("Predicted foot step source identity is invalid.", nameof(stableIdentity));
            return HashText(stableIdentity.Trim());
        }

        public AnimationPredictedFootStepSample BindSource(
            ulong sourceSampleIdentity,
            int sourceSampleCycle)
        {
            if (!HasLandingEvent)
                return this;
            if (sourceSampleIdentity == 0 || sourceSampleCycle < 0)
            {
                throw new ArgumentException("Predicted foot step source occurrence is invalid.");
            }
            int sourceLandingCycle = checked(sourceSampleCycle + SourceLandingCycleOffset);
            return new AnimationPredictedFootStepSample(
                EventOrdinal,
                SourceLandingCycleOffset,
                Confidence,
                TimeToLandingSeconds,
                EventPhase,
                ReleasePhase,
                LiftOffPhase,
                ApproachContactPhase,
                ActionStepClock.DurationSeconds,
                OpposingEventOrdinal,
                OpposingLandingDelaySeconds,
                OpposingLandingCycleOffset,
                OpposingRootLocalLanding,
                Route.RootLocalFoot,
                Route.RootLocalAnkle,
                Route.RootLocalHip,
                Route.AuthoredFootPlanar,
                Route.AnimationClearance,
                LandingPhase,
                OpposingRootLocalSoleRotation,
                Route.CurrentSample,
                sourceSampleIdentity,
                sourceLandingCycle,
                0,
                0,
                0,
                false);
        }

        public AnimationPredictedFootStepSample BindSynchronizedMarkerSource(
            ulong markerEpochIdentity,
            int landingMarkerOrdinal,
            int opposingLandingMarkerOrdinal)
        {
            if (!IsSourceBound)
                return this;
            if (markerEpochIdentity == 0 || landingMarkerOrdinal < 0 ||
                (OpposingEventOrdinal > 0) != (opposingLandingMarkerOrdinal >= 0))
            {
                throw new ArgumentException("Synchronized marker landing occurrence is invalid.");
            }
            int opposingOffset = OpposingEventOrdinal > 0
                ? checked(opposingLandingMarkerOrdinal - landingMarkerOrdinal)
                : 0;
            if (opposingOffset < -1 || opposingOffset > 1)
            {
                throw new ArgumentException("Synchronized opposing marker is not adjacent to the owned landing.");
            }
            return new AnimationPredictedFootStepSample(
                1,
                SourceLandingCycleOffset,
                Confidence,
                TimeToLandingSeconds,
                EventPhase,
                ReleasePhase,
                LiftOffPhase,
                ApproachContactPhase,
                ActionStepClock.DurationSeconds,
                OpposingEventOrdinal > 0 ? 1 : 0,
                OpposingLandingDelaySeconds,
                opposingOffset,
                OpposingRootLocalLanding,
                Route.RootLocalFoot,
                Route.RootLocalAnkle,
                Route.RootLocalHip,
                Route.AuthoredFootPlanar,
                Route.AnimationClearance,
                LandingPhase,
                OpposingRootLocalSoleRotation,
                Route.CurrentSample,
                markerEpochIdentity,
                landingMarkerOrdinal,
                0,
                0,
                0,
                true);
        }

        public AnimationPredictedFootStepSample BindContribution(
            ulong contributionContinuityIdentity,
            CharacterFootSide side)
        {
            if (!IsSourceBound)
                return this;
            if (contributionContinuityIdentity == 0 ||
                side != CharacterFootSide.Left && side != CharacterFootSide.Right)
            {
                throw new ArgumentException("Predicted foot step contribution identity is invalid.");
            }
            ulong identity = ResolveLandingEventIdentity(
                m_UsesSynchronizedMarkerIdentity != 0
                    ? 0
                    : contributionContinuityIdentity,
                SourceSampleIdentity,
                SourceSampleCycle,
                EventOrdinal,
                side);
            ulong opposingIdentity = OpposingEventOrdinal > 0
                ? ResolveLandingEventIdentity(
                    m_UsesSynchronizedMarkerIdentity != 0
                        ? 0
                        : contributionContinuityIdentity,
                    SourceSampleIdentity,
                    checked(SourceSampleCycle + OpposingLandingCycleOffset),
                    OpposingEventOrdinal,
                    side == CharacterFootSide.Left ? CharacterFootSide.Right : CharacterFootSide.Left)
                : 0;
            return new AnimationPredictedFootStepSample(
                EventOrdinal,
                SourceLandingCycleOffset,
                Confidence,
                TimeToLandingSeconds,
                EventPhase,
                ReleasePhase,
                LiftOffPhase,
                ApproachContactPhase,
                ActionStepClock.DurationSeconds,
                OpposingEventOrdinal,
                OpposingLandingDelaySeconds,
                OpposingLandingCycleOffset,
                OpposingRootLocalLanding,
                Route.RootLocalFoot,
                Route.RootLocalAnkle,
                Route.RootLocalHip,
                Route.AuthoredFootPlanar,
                Route.AnimationClearance,
                LandingPhase,
                OpposingRootLocalSoleRotation,
                Route.CurrentSample,
                SourceSampleIdentity,
                SourceSampleCycle,
                contributionContinuityIdentity,
                identity,
                opposingIdentity,
                m_UsesSynchronizedMarkerIdentity != 0);
        }

        public AnimationPredictedFootStepSample ApplyTimeScale(float visualTimeScale)
        {
            if (!float.IsFinite(visualTimeScale) || visualTimeScale < 0f)
                throw new ArgumentOutOfRangeException(nameof(visualTimeScale));
            if (!HasLandingEvent || visualTimeScale <= 0.000001f)
                return default;
            return new AnimationPredictedFootStepSample(
                EventOrdinal,
                SourceLandingCycleOffset,
                Confidence,
                TimeToLandingSeconds / visualTimeScale,
                EventPhase,
                ReleasePhase,
                LiftOffPhase,
                ApproachContactPhase,
                ActionStepClock.DurationSeconds / visualTimeScale,
                OpposingEventOrdinal,
                OpposingLandingDelaySeconds / visualTimeScale,
                OpposingLandingCycleOffset,
                OpposingRootLocalLanding,
                Route.RootLocalFoot,
                Route.RootLocalAnkle,
                Route.RootLocalHip,
                Route.AuthoredFootPlanar,
                Route.AnimationClearance,
                LandingPhase,
                OpposingRootLocalSoleRotation,
                Route.CurrentSample,
                SourceSampleIdentity,
                SourceSampleCycle,
                ContributionContinuityIdentity,
                LandingEventIdentity,
                OpposingLandingEventIdentity,
                m_UsesSynchronizedMarkerIdentity != 0);
        }

        internal static AnimationPredictedFootStepSample Select(
            AnimationPredictedFootStepSample current,
            float currentScore,
            AnimationPredictedFootStepSample candidate,
            float candidateScore,
            out bool candidateSelected)
        {
            candidateSelected = false;
            if (!candidate.HasLandingEvent)
                return current;
            if (!current.HasLandingEvent || candidateScore > currentScore + 0.000001f)
            {
                candidateSelected = true;
                return candidate;
            }
            if (Mathf.Abs(candidateScore - currentScore) > 0.000001f)
                return current;
            if (candidate.SourceSampleIdentity != current.SourceSampleIdentity)
            {
                candidateSelected = candidate.SourceSampleIdentity < current.SourceSampleIdentity;
                return candidateSelected ? candidate : current;
            }
            if (candidate.SourceSampleCycle != current.SourceSampleCycle)
            {
                candidateSelected = candidate.SourceSampleCycle < current.SourceSampleCycle;
                return candidateSelected ? candidate : current;
            }
            candidateSelected = candidate.EventOrdinal < current.EventOrdinal;
            return candidateSelected ? candidate : current;
        }

        static Vector3 EvaluateVectorRoute(
            FixedList512Bytes<Vector3> route,
            float eventPhase,
            string label)
        {
            if (route.Length != AnimationPredictedFootStepCurveSet.RouteSampleCount)
                throw new InvalidOperationException($"Predicted {label} is unavailable.");
            EvaluateIndices(route.Length, eventPhase, out int first, out int second, out float t);
            return Vector3.Lerp(route[first], route[second], t);
        }

        static void EvaluateIndices(
            int count,
            float eventPhase,
            out int first,
            out int second,
            out float t)
        {
            if (count != AnimationPredictedFootStepCurveSet.RouteSampleCount)
                throw new InvalidOperationException("Predicted action route is unavailable.");
            float scaled = Mathf.Clamp01(eventPhase) * (count - 1);
            first = Mathf.Min(count - 1, Mathf.FloorToInt(scaled));
            second = Mathf.Min(count - 1, first + 1);
            t = scaled - first;
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

        static ulong ResolveLandingEventIdentity(
            ulong contributionContinuityIdentity,
            ulong sourceSampleIdentity,
            int sourceSampleCycle,
            int eventOrdinal,
            CharacterFootSide side) =>
            Hash(
                contributionContinuityIdentity,
                sourceSampleIdentity,
                unchecked((ulong)(long)sourceSampleCycle),
                (ulong)(uint)eventOrdinal,
                (ulong)side);

        ulong ResolveIdentityContribution() =>
            m_UsesSynchronizedMarkerIdentity != 0
                ? 0
                : ContributionContinuityIdentity;

        static ulong HashText(string value)
        {
            const ulong offset = 14695981039346656037UL;
            const ulong prime = 1099511628211UL;
            ulong hash = offset;
            for (int i = 0; i < value.Length; i++)
                hash = (hash ^ value[i]) * prime;
            return hash == 0 ? 1UL : hash;
        }

        static float RequireNormalized(float value, string field)
        {
            if (!float.IsFinite(value) || value < -0.00001f || value > 1.00001f)
                throw new ArgumentOutOfRangeException(field);
            return Mathf.Clamp01(value);
        }

        static float RequireNonNegative(float value, string field)
        {
            if (!float.IsFinite(value) || value < -0.00001f)
                throw new ArgumentOutOfRangeException(field);
            return Mathf.Max(0f, value);
        }

        static Vector3 RequireFinite(Vector3 value, string field)
        {
            if (!float.IsFinite(value.x) || !float.IsFinite(value.y) || !float.IsFinite(value.z))
                throw new ArgumentOutOfRangeException(field);
            return value;
        }

        static void RequirePlanar(Vector3 value, string field)
        {
            RequireFinite(value, field);
            if (Mathf.Abs(value.y) > 0.00001f)
                throw new ArgumentOutOfRangeException(field);
        }

        static Quaternion RequireFinite(Quaternion value, string field)
        {
            if (!float.IsFinite(value.x) || !float.IsFinite(value.y) ||
                !float.IsFinite(value.z) || !float.IsFinite(value.w) ||
                Quaternion.Dot(value, value) <= 0.000001f)
            {
                throw new ArgumentOutOfRangeException(field);
            }
            return value.normalized;
        }

    }

    public readonly struct AnimationFootFeatureSample
    {
        public AnimationFootFeatureSample(
            Vector3 soleLocalVelocity,
            float soleHeight,
            float plantConfidence,
            AnimationPredictedFootStepSample predictedStep,
            AnimationPredictedFootStepSample incomingPredictedStep)
        {
            SoleLocalVelocity = RequireFinite(soleLocalVelocity, nameof(soleLocalVelocity));
            SoleHeight = RequireFinite(soleHeight, nameof(soleHeight));
            PlantConfidence = RequireNormalized(plantConfidence, nameof(plantConfidence));
            PredictedStep = predictedStep;
            IncomingPredictedStep = incomingPredictedStep;
            m_IsSpecified = 1;
        }

        readonly byte m_IsSpecified;
        public Vector3 SoleLocalVelocity { get; }
        public float SoleHeight { get; }
        public float PlantConfidence { get; }
        public readonly AnimationPredictedFootStepSample PredictedStep;
        public readonly AnimationPredictedFootStepSample IncomingPredictedStep;
        public bool IsValid => m_IsSpecified != 0;

        public AnimationFootFeatureSample WithPredictionPair(
            AnimationPredictedFootStepSample predictedStep,
            AnimationPredictedFootStepSample incomingPredictedStep) =>
            new AnimationFootFeatureSample(
                SoleLocalVelocity,
                SoleHeight,
                PlantConfidence,
                predictedStep,
                incomingPredictedStep);

        public AnimationFootFeatureSample BindPredictionSource(
            ulong sourceIdentity,
            int sourceCycle) =>
            new AnimationFootFeatureSample(
                SoleLocalVelocity,
                SoleHeight,
                PlantConfidence,
                PredictedStep.BindSource(
                    sourceIdentity,
                    sourceCycle),
                IncomingPredictedStep.BindSource(
                    sourceIdentity,
                    sourceCycle));

        public AnimationFootFeatureSample BindPredictionContribution(
            ulong contributionContinuityIdentity,
            CharacterFootSide side) =>
            new AnimationFootFeatureSample(
                SoleLocalVelocity,
                SoleHeight,
                PlantConfidence,
                PredictedStep.BindContribution(contributionContinuityIdentity, side),
                IncomingPredictedStep.BindContribution(contributionContinuityIdentity, side));

        static float RequireNormalized(float value, string field)
        {
            if (!float.IsFinite(value) || value < -0.00001f || value > 1.00001f)
                throw new ArgumentOutOfRangeException(field);
            return Mathf.Clamp01(value);
        }

        static float RequireFinite(float value, string field)
        {
            if (!float.IsFinite(value))
                throw new ArgumentOutOfRangeException(field);
            return value;
        }

        static Vector3 RequireFinite(Vector3 value, string field)
        {
            if (!float.IsFinite(value.x) || !float.IsFinite(value.y) || !float.IsFinite(value.z))
                throw new ArgumentOutOfRangeException(field);
            return value;
        }
    }

    public readonly struct AnimationFootFeaturePair
    {
        public AnimationFootFeaturePair(AnimationFootFeatureCurveSet left, AnimationFootFeatureCurveSet right)
        {
            Left = left ?? throw new ArgumentNullException(nameof(left));
            Right = right ?? throw new ArgumentNullException(nameof(right));
            Left.RequireValid();
            Right.RequireValid();
        }

        public AnimationFootFeatureCurveSet Left { get; }
        public AnimationFootFeatureCurveSet Right { get; }
        public bool IsValid => Left != null && Right != null;
    }

    internal struct AnimationFootFeatureBlendAccumulator
    {
        float m_Weight;
        Vector3 m_Velocity;
        float m_Height;
        float m_PlantConfidence;
        AnimationPredictedFootStepSample m_PredictedStep;
        AnimationPredictedFootStepSample m_IncomingPredictedStep;
        float m_PredictionPairScore;

        public void Add(AnimationFootFeatureSample sample, float weight)
        {
            Add(sample, weight, 1f);
        }

        public void Add(AnimationFootFeatureSample sample, float weight, float visualTimeScale)
        {
            if (!sample.IsValid || !float.IsFinite(weight) || weight <= 0f ||
                !float.IsFinite(visualTimeScale) || visualTimeScale < 0f)
                throw new ArgumentException("Foot Analysis blend contribution is invalid.");
            m_Weight += weight;
            m_Velocity += sample.SoleLocalVelocity * visualTimeScale * weight;
            m_Height += sample.SoleHeight * weight;
            m_PlantConfidence += sample.PlantConfidence * weight;
            AnimationPredictedFootStepSample candidate =
                sample.PredictedStep.ApplyTimeScale(visualTimeScale);
            AnimationPredictedFootStepSample incomingCandidate =
                sample.IncomingPredictedStep.ApplyTimeScale(visualTimeScale);
            AnimationPredictedFootStepSample currentAuthority =
                m_PredictedStep.HasLandingEvent
                    ? m_PredictedStep
                    : m_IncomingPredictedStep;
            AnimationPredictedFootStepSample candidateAuthority =
                candidate.HasLandingEvent
                    ? candidate
                    : incomingCandidate;
            float pairScore = candidateAuthority.HasLandingEvent
                ? weight * candidateAuthority.Confidence
                : 0f;
            AnimationPredictedFootStepSample.Select(
                currentAuthority,
                m_PredictionPairScore,
                candidateAuthority,
                pairScore,
                out bool candidatePairSelected);
            if (candidatePairSelected)
            {
                m_PredictedStep = candidate;
                m_IncomingPredictedStep = incomingCandidate;
                m_PredictionPairScore = pairScore;
            }
        }

        public AnimationFootFeatureSample Resolve()
        {
            if (m_Weight <= 0f)
                throw new InvalidOperationException("Foot Analysis blend has no visible contribution.");
            return new AnimationFootFeatureSample(
                m_Velocity / m_Weight,
                m_Height / m_Weight,
                m_PlantConfidence / m_Weight,
                m_PredictedStep,
                m_IncomingPredictedStep);
        }
    }

    [Serializable]
    public sealed class AnimationFootAnalysisProjectionIdentity
    {
        [SerializeField] CharacterFootPlacementAnalysisMode m_Mode;
        [SerializeField] string m_AnalysisSourceId = string.Empty;
        [SerializeField] int m_AnalysisVersion;
        [SerializeField] string m_AlgorithmVersion = string.Empty;
        [SerializeField] string m_CalibrationId = string.Empty;
        [SerializeField] int m_CalibrationSchemaVersion;
        [SerializeField] string m_CalibrationRevision = string.Empty;
        [SerializeField] string m_GeometryValidationIdentity = string.Empty;
        [SerializeField] string m_GeometryValidationContentHash = string.Empty;
        [SerializeField] string m_ArtifactContentHash = string.Empty;

        public AnimationFootAnalysisProjectionIdentity(
            CharacterFootPlacementAnalysisMode mode,
            string analysisSourceId,
            int analysisVersion,
            string algorithmVersion,
            CharacterFootPlacementRigCalibrationId calibrationId,
            int calibrationSchemaVersion,
            string calibrationRevision,
            string geometryValidationIdentity,
            string geometryValidationContentHash,
            string artifactContentHash)
        {
            m_Mode = mode;
            m_AnalysisSourceId = analysisSourceId ?? string.Empty;
            m_AnalysisVersion = analysisVersion;
            m_AlgorithmVersion = algorithmVersion ?? string.Empty;
            m_CalibrationId = calibrationId.Value;
            m_CalibrationSchemaVersion = calibrationSchemaVersion;
            m_CalibrationRevision = calibrationRevision ?? string.Empty;
            m_GeometryValidationIdentity = geometryValidationIdentity ?? string.Empty;
            m_GeometryValidationContentHash = geometryValidationContentHash ?? string.Empty;
            m_ArtifactContentHash = artifactContentHash ?? string.Empty;
            RequireValid();
        }

        public CharacterFootPlacementAnalysisMode Mode => m_Mode;
        public string AnalysisSourceId => m_AnalysisSourceId;
        public int AnalysisVersion => m_AnalysisVersion;
        public string AlgorithmVersion => m_AlgorithmVersion;
        public CharacterFootPlacementRigCalibrationId CalibrationId => new CharacterFootPlacementRigCalibrationId(m_CalibrationId);
        public int CalibrationSchemaVersion => m_CalibrationSchemaVersion;
        public string CalibrationRevision => m_CalibrationRevision;
        public string GeometryValidationIdentity => m_GeometryValidationIdentity;
        public string GeometryValidationContentHash => m_GeometryValidationContentHash;
        public string ArtifactContentHash => m_ArtifactContentHash;
        public bool IsEnabled => m_Mode == CharacterFootPlacementAnalysisMode.GeneratedPerFootFeatures;
        public bool IsValid => IsEnabled &&
                               !string.IsNullOrWhiteSpace(m_AnalysisSourceId) &&
                               m_AnalysisVersion > 0 &&
                               !string.IsNullOrWhiteSpace(m_AlgorithmVersion) &&
                               !string.IsNullOrWhiteSpace(m_CalibrationId) &&
                               string.Equals(m_CalibrationId, m_CalibrationId.Trim(), StringComparison.Ordinal) &&
                               m_CalibrationSchemaVersion == CharacterFootPlacementRigCalibration.CurrentSchemaVersion &&
                               !string.IsNullOrWhiteSpace(m_CalibrationRevision) &&
                               IsStableHash(m_GeometryValidationIdentity) &&
                               IsStableHash(m_GeometryValidationContentHash) &&
                               IsStableHash(m_ArtifactContentHash);

        public void RequireValid()
        {
            if (!IsValid)
                throw new InvalidOperationException("Foot Analysis Projection identity is invalid.");
            _ = CalibrationId;
        }

        static bool IsStableHash(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length != 64)
                return false;
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (c < '0' || c > '9' && c < 'a' || c > 'f')
                    return false;
            }
            return true;
        }
    }

    public sealed class AnimationFootAnalysisProjectionBuildData
    {
        readonly IReadOnlyDictionary<string, AnimationFootFeaturePair> m_Features;

        public AnimationFootAnalysisProjectionBuildData(
            AnimationFootAnalysisProjectionIdentity identity,
            IReadOnlyDictionary<string, AnimationFootFeaturePair> features)
        {
            Identity = identity ?? throw new ArgumentNullException(nameof(identity));
            Identity.RequireValid();
            m_Features = features ?? throw new ArgumentNullException(nameof(features));
        }

        public AnimationFootAnalysisProjectionIdentity Identity { get; }

        public bool TryGet(
            string timelineAuthoringId,
            string trackAuthoringId,
            string clipAuthoringId,
            out AnimationFootFeaturePair pair)
        {
            return m_Features.TryGetValue(
                       BindingKey(timelineAuthoringId, trackAuthoringId, clipAuthoringId),
                       out pair) && pair.IsValid;
        }

        public bool TryGetBlendSpace(
            CharacterAnimationBlendSpaceId blendSpaceId,
            CharacterAnimationBlendSpaceSampleId sampleId,
            out AnimationFootFeaturePair pair)
        {
            return m_Features.TryGetValue(
                       BlendSpaceBindingKey(blendSpaceId, sampleId),
                       out pair) && pair.IsValid;
        }

        public bool TryGetPoseSource(
            string bindingAssetIdentity,
            out AnimationFootFeaturePair pair)
        {
            return m_Features.TryGetValue(
                       PoseSourceBindingKey(bindingAssetIdentity),
                       out pair) && pair.IsValid;
        }

        public static string BindingKey(
            string timelineAuthoringId,
            string trackAuthoringId,
            string clipAuthoringId)
        {
            if (string.IsNullOrWhiteSpace(timelineAuthoringId) ||
                string.IsNullOrWhiteSpace(trackAuthoringId) ||
                string.IsNullOrWhiteSpace(clipAuthoringId))
                throw new ArgumentException("Foot Analysis stable clip binding identity is invalid.");
            return string.Concat(timelineAuthoringId, "\n", trackAuthoringId, "\n", clipAuthoringId);
        }

        public static string BlendSpaceBindingKey(
            CharacterAnimationBlendSpaceId blendSpaceId,
            CharacterAnimationBlendSpaceSampleId sampleId)
        {
            if (!blendSpaceId.IsValid || !sampleId.IsValid)
                throw new ArgumentException("Foot Analysis Blend Space Sample binding identity is invalid.");
            return string.Concat("blend-space\n", blendSpaceId.Value, "\n", sampleId.Value);
        }

        public static string PoseSourceBindingKey(string bindingAssetIdentity)
        {
            if (string.IsNullOrWhiteSpace(bindingAssetIdentity))
                throw new ArgumentException("Foot Analysis Presentation Pose source identity is invalid.");
            return string.Concat("pose-source-object\n", bindingAssetIdentity.Trim());
        }
    }
}
