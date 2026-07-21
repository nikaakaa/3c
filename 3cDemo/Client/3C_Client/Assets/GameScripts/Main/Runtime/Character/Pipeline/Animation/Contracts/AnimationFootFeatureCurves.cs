using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Presentation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public enum CharacterFootPlacementAnalysisMode : byte
    {
        Disabled = 0,
        GeneratedPerFootFeatures = 1
    }

    [Serializable]
    public sealed class AnimationFootFeatureCurveSet
    {
        [SerializeField] AnimationCurve m_SoleLocalVelocityX;
        [SerializeField] AnimationCurve m_SoleLocalVelocityY;
        [SerializeField] AnimationCurve m_SoleLocalVelocityZ;
        [SerializeField] AnimationCurve m_SoleHeight;
        [SerializeField] AnimationCurve m_PlantConfidence;
        [SerializeField] AnimationCurve m_NextLandingConfidence;
        [SerializeField] AnimationCurve m_NextLandingDelaySeconds;
        [SerializeField] AnimationCurve m_NextLandingLocalOffsetX;
        [SerializeField] AnimationCurve m_NextLandingLocalOffsetZ;

        public AnimationFootFeatureCurveSet(
            AnimationCurve soleLocalVelocityX,
            AnimationCurve soleLocalVelocityY,
            AnimationCurve soleLocalVelocityZ,
            AnimationCurve soleHeight,
            AnimationCurve plantConfidence,
            AnimationCurve nextLandingConfidence,
            AnimationCurve nextLandingDelaySeconds,
            AnimationCurve nextLandingLocalOffsetX,
            AnimationCurve nextLandingLocalOffsetZ)
        {
            m_SoleLocalVelocityX = Copy(soleLocalVelocityX);
            m_SoleLocalVelocityY = Copy(soleLocalVelocityY);
            m_SoleLocalVelocityZ = Copy(soleLocalVelocityZ);
            m_SoleHeight = Copy(soleHeight);
            m_PlantConfidence = Copy(plantConfidence);
            m_NextLandingConfidence = Copy(nextLandingConfidence);
            m_NextLandingDelaySeconds = Copy(nextLandingDelaySeconds);
            m_NextLandingLocalOffsetX = Copy(nextLandingLocalOffsetX);
            m_NextLandingLocalOffsetZ = Copy(nextLandingLocalOffsetZ);
            RequireValid();
        }

        public AnimationCurve SoleLocalVelocityX => m_SoleLocalVelocityX;
        public AnimationCurve SoleLocalVelocityY => m_SoleLocalVelocityY;
        public AnimationCurve SoleLocalVelocityZ => m_SoleLocalVelocityZ;
        public AnimationCurve SoleHeight => m_SoleHeight;
        public AnimationCurve PlantConfidence => m_PlantConfidence;
        public AnimationCurve NextLandingConfidence => m_NextLandingConfidence;
        public AnimationCurve NextLandingDelaySeconds => m_NextLandingDelaySeconds;
        public AnimationCurve NextLandingLocalOffsetX => m_NextLandingLocalOffsetX;
        public AnimationCurve NextLandingLocalOffsetZ => m_NextLandingLocalOffsetZ;

        public AnimationFootFeatureSample Sample(float normalizedTime)
        {
            RequireValid();
            float time = Mathf.Clamp01(normalizedTime);
            return new AnimationFootFeatureSample(
                new Vector3(
                    m_SoleLocalVelocityX.Evaluate(time),
                    m_SoleLocalVelocityY.Evaluate(time),
                    m_SoleLocalVelocityZ.Evaluate(time)),
                m_SoleHeight.Evaluate(time),
                m_PlantConfidence.Evaluate(time),
                m_NextLandingConfidence.Evaluate(time),
                m_NextLandingDelaySeconds.Evaluate(time),
                new Vector2(
                    m_NextLandingLocalOffsetX.Evaluate(time),
                    m_NextLandingLocalOffsetZ.Evaluate(time)));
        }

        public void RequireValid()
        {
            RequireCurve(m_SoleLocalVelocityX, nameof(m_SoleLocalVelocityX), false, false);
            RequireCurve(m_SoleLocalVelocityY, nameof(m_SoleLocalVelocityY), false, false);
            RequireCurve(m_SoleLocalVelocityZ, nameof(m_SoleLocalVelocityZ), false, false);
            RequireCurve(m_SoleHeight, nameof(m_SoleHeight), false, false);
            RequireCurve(m_PlantConfidence, nameof(m_PlantConfidence), true, false);
            RequireCurve(m_NextLandingConfidence, nameof(m_NextLandingConfidence), true, false);
            RequireCurve(m_NextLandingDelaySeconds, nameof(m_NextLandingDelaySeconds), false, true);
            RequireCurve(m_NextLandingLocalOffsetX, nameof(m_NextLandingLocalOffsetX), false, false);
            RequireCurve(m_NextLandingLocalOffsetZ, nameof(m_NextLandingLocalOffsetZ), false, false);
        }

        static void RequireCurve(AnimationCurve curve, string field, bool normalized, bool nonNegative)
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

        static AnimationCurve Copy(AnimationCurve source)
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

    public readonly struct AnimationFootFeatureSample
    {
        public AnimationFootFeatureSample(
            Vector3 soleLocalVelocity,
            float soleHeight,
            float plantConfidence,
            float nextLandingConfidence,
            float nextLandingDelaySeconds,
            Vector2 nextLandingLocalOffset)
        {
            SoleLocalVelocity = RequireFinite(soleLocalVelocity, nameof(soleLocalVelocity));
            SoleHeight = RequireFinite(soleHeight, nameof(soleHeight));
            PlantConfidence = RequireNormalized(plantConfidence, nameof(plantConfidence));
            NextLandingConfidence = RequireNormalized(nextLandingConfidence, nameof(nextLandingConfidence));
            NextLandingDelaySeconds = RequireNonNegative(nextLandingDelaySeconds, nameof(nextLandingDelaySeconds));
            NextLandingLocalOffset = RequireFinite(nextLandingLocalOffset, nameof(nextLandingLocalOffset));
            m_IsSpecified = true;
        }

        readonly bool m_IsSpecified;
        public Vector3 SoleLocalVelocity { get; }
        public float SoleHeight { get; }
        public float PlantConfidence { get; }
        public float NextLandingConfidence { get; }
        public float NextLandingDelaySeconds { get; }
        public Vector2 NextLandingLocalOffset { get; }
        public bool IsValid => m_IsSpecified;

        static float RequireNormalized(float value, string field)
        {
            if (!float.IsFinite(value) || value < -0.00001f || value > 1.00001f)
                throw new ArgumentOutOfRangeException(field);
            return Mathf.Clamp01(value);
        }

        static float RequireNonNegative(float value, string field)
        {
            if (!float.IsFinite(value) || value < 0f)
                throw new ArgumentOutOfRangeException(field);
            return value;
        }

        static float RequireFinite(float value, string field)
        {
            if (!float.IsFinite(value))
                throw new ArgumentOutOfRangeException(field);
            return value;
        }

        static Vector2 RequireFinite(Vector2 value, string field)
        {
            if (!float.IsFinite(value.x) || !float.IsFinite(value.y))
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
        float m_LandingConfidence;
        float m_LandingWeight;
        float m_LandingDelay;
        Vector2 m_LandingOffset;

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
            float effectiveLandingConfidence = visualTimeScale > 0.000001f
                ? sample.NextLandingConfidence
                : 0f;
            m_LandingConfidence += effectiveLandingConfidence * weight;
            float landingWeight = weight * effectiveLandingConfidence;
            m_LandingWeight += landingWeight;
            if (landingWeight > 0f)
                m_LandingDelay += sample.NextLandingDelaySeconds / visualTimeScale * landingWeight;
            m_LandingOffset += sample.NextLandingLocalOffset * landingWeight;
        }

        public AnimationFootFeatureSample Resolve()
        {
            if (m_Weight <= 0f)
                throw new InvalidOperationException("Foot Analysis blend has no visible contribution.");
            float landingDelay = m_LandingWeight > 0f ? m_LandingDelay / m_LandingWeight : 0f;
            Vector2 landingOffset = m_LandingWeight > 0f ? m_LandingOffset / m_LandingWeight : Vector2.zero;
            return new AnimationFootFeatureSample(
                m_Velocity / m_Weight,
                m_Height / m_Weight,
                m_PlantConfidence / m_Weight,
                m_LandingConfidence / m_Weight,
                landingDelay,
                landingOffset);
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
        [SerializeField] string m_CalibrationRevision = string.Empty;
        [SerializeField] string m_ArtifactContentHash = string.Empty;

        public AnimationFootAnalysisProjectionIdentity(
            CharacterFootPlacementAnalysisMode mode,
            string analysisSourceId,
            int analysisVersion,
            string algorithmVersion,
            CharacterFootPlacementRigCalibrationId calibrationId,
            string calibrationRevision,
            string artifactContentHash)
        {
            m_Mode = mode;
            m_AnalysisSourceId = analysisSourceId ?? string.Empty;
            m_AnalysisVersion = analysisVersion;
            m_AlgorithmVersion = algorithmVersion ?? string.Empty;
            m_CalibrationId = calibrationId.Value;
            m_CalibrationRevision = calibrationRevision ?? string.Empty;
            m_ArtifactContentHash = artifactContentHash ?? string.Empty;
            RequireValid();
        }

        public CharacterFootPlacementAnalysisMode Mode => m_Mode;
        public string AnalysisSourceId => m_AnalysisSourceId;
        public int AnalysisVersion => m_AnalysisVersion;
        public string AlgorithmVersion => m_AlgorithmVersion;
        public CharacterFootPlacementRigCalibrationId CalibrationId => new CharacterFootPlacementRigCalibrationId(m_CalibrationId);
        public string CalibrationRevision => m_CalibrationRevision;
        public string ArtifactContentHash => m_ArtifactContentHash;
        public bool IsEnabled => m_Mode == CharacterFootPlacementAnalysisMode.GeneratedPerFootFeatures;

        public void RequireValid()
        {
            if (!IsEnabled || string.IsNullOrWhiteSpace(m_AnalysisSourceId) || m_AnalysisVersion <= 0 ||
                string.IsNullOrWhiteSpace(m_AlgorithmVersion) || string.IsNullOrWhiteSpace(m_CalibrationRevision) ||
                !IsStableHash(m_ArtifactContentHash))
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
    }
}
