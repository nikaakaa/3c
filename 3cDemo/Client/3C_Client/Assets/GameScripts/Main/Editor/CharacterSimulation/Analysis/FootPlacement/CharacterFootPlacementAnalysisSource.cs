using System;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Presentation;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    public readonly struct CharacterFootPlacementAnalysisSourceId : IEquatable<CharacterFootPlacementAnalysisSourceId>
    {
        public CharacterFootPlacementAnalysisSourceId(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || !string.Equals(value, value.Trim(), StringComparison.Ordinal))
                throw new ArgumentException("Foot Analysis source identity is invalid.", nameof(value));
            Value = value;
        }

        public string Value { get; }
        public bool Equals(CharacterFootPlacementAnalysisSourceId other) =>
            string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is CharacterFootPlacementAnalysisSourceId other && Equals(other);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value ?? string.Empty);
        public override string ToString() => Value ?? string.Empty;
    }

    [Serializable]
    public sealed class CharacterFootPlacementAnalysisThresholds
    {
        [SerializeField, Min(0f)] float m_PlantEnterVerticalSpeed = 0.12f;
        [SerializeField, Min(0f)] float m_PlantExitVerticalSpeed = 0.28f;
        [SerializeField, Min(0f)] float m_PlantEnterHeight = 0.025f;
        [SerializeField, Min(0f)] float m_PlantExitHeight = 0.08f;
        [SerializeField, Min(0.001f)] float m_MinimumLandingSegmentSeconds = 0.05f;
        [SerializeField, Min(0.001f)] float m_MaximumLandingSearchSeconds = 1.5f;

        public float PlantEnterVerticalSpeed => m_PlantEnterVerticalSpeed;
        public float PlantExitVerticalSpeed => m_PlantExitVerticalSpeed;
        public float PlantEnterHeight => m_PlantEnterHeight;
        public float PlantExitHeight => m_PlantExitHeight;
        public float MinimumLandingSegmentSeconds => m_MinimumLandingSegmentSeconds;
        public float MaximumLandingSearchSeconds => m_MaximumLandingSearchSeconds;

        public void RequireValid()
        {
            RequireFiniteNonNegative(m_PlantEnterVerticalSpeed, nameof(m_PlantEnterVerticalSpeed));
            RequireFinitePositive(m_PlantExitVerticalSpeed, nameof(m_PlantExitVerticalSpeed));
            RequireFiniteNonNegative(m_PlantEnterHeight, nameof(m_PlantEnterHeight));
            RequireFinitePositive(m_PlantExitHeight, nameof(m_PlantExitHeight));
            RequireFinitePositive(m_MinimumLandingSegmentSeconds, nameof(m_MinimumLandingSegmentSeconds));
            RequireFinitePositive(m_MaximumLandingSearchSeconds, nameof(m_MaximumLandingSearchSeconds));
            if (m_PlantExitVerticalSpeed <= m_PlantEnterVerticalSpeed || m_PlantExitHeight <= m_PlantEnterHeight)
                throw new InvalidOperationException("Foot Analysis plant exit thresholds must exceed enter thresholds.");
            if (m_MaximumLandingSearchSeconds < m_MinimumLandingSegmentSeconds)
                throw new InvalidOperationException("Foot Analysis landing search must cover the minimum segment duration.");
        }

        static void RequireFiniteNonNegative(float value, string field)
        {
            if (!float.IsFinite(value) || value < 0f)
                throw new InvalidOperationException($"Foot Analysis '{field}' is invalid.");
        }

        static void RequireFinitePositive(float value, string field)
        {
            if (!float.IsFinite(value) || value <= 0f)
                throw new InvalidOperationException($"Foot Analysis '{field}' is invalid.");
        }
    }

    [Serializable]
    public sealed class CharacterFootPlacementCurveReductionSettings
    {
        [SerializeField, Min(0.000001f)] float m_VelocityTolerance = 0.002f;
        [SerializeField, Min(0.000001f)] float m_HeightTolerance = 0.001f;
        [SerializeField, Min(0.000001f)] float m_ConfidenceTolerance = 0.005f;
        [SerializeField, Min(0.000001f)] float m_LandingDelayTolerance = 0.002f;
        [SerializeField, Min(0.000001f)] float m_LandingOffsetTolerance = 0.001f;

        public float VelocityTolerance => m_VelocityTolerance;
        public float HeightTolerance => m_HeightTolerance;
        public float ConfidenceTolerance => m_ConfidenceTolerance;
        public float LandingDelayTolerance => m_LandingDelayTolerance;
        public float LandingOffsetTolerance => m_LandingOffsetTolerance;

        public void RequireValid()
        {
            Require(m_VelocityTolerance, nameof(m_VelocityTolerance));
            Require(m_HeightTolerance, nameof(m_HeightTolerance));
            Require(m_ConfidenceTolerance, nameof(m_ConfidenceTolerance));
            Require(m_LandingDelayTolerance, nameof(m_LandingDelayTolerance));
            Require(m_LandingOffsetTolerance, nameof(m_LandingOffsetTolerance));
        }

        static void Require(float value, string field)
        {
            if (!float.IsFinite(value) || value <= 0f)
                throw new InvalidOperationException($"Foot Analysis reduction '{field}' is invalid.");
        }
    }

    [CreateAssetMenu(
        fileName = "CharacterFootPlacementAnalysisSource",
        menuName = "3C/Editor/Foot Placement Analysis Source")]
    public sealed class CharacterFootPlacementAnalysisSource : ScriptableObject
    {
        public const string AlgorithmVersion = "animation-foot-analysis/v7";

        [SerializeField] string m_AnalysisSourceId = string.Empty;
        [SerializeField, Min(1)] int m_AnalysisVersion = 1;
        [SerializeField] string m_SamplingRigAssetGuid = string.Empty;
        [SerializeField] CharacterAnimationRigDefinition m_RigDefinition;
        [SerializeField] CharacterFootPlacementRigCalibration m_RigCalibration;
        [SerializeField] AnimationClip m_CalibrationPreviewClip;
        [SerializeField, Range(0f, 1f)] float m_CalibrationPreviewNormalizedTime;
        [SerializeField, Min(1f)] float m_SampleRate = 60f;
        [SerializeField] CharacterFootPlacementAnalysisThresholds m_Thresholds = new CharacterFootPlacementAnalysisThresholds();
        [SerializeField] CharacterFootPlacementCurveReductionSettings m_Reduction = new CharacterFootPlacementCurveReductionSettings();

        public CharacterFootPlacementAnalysisSourceId AnalysisSourceId =>
            new CharacterFootPlacementAnalysisSourceId(m_AnalysisSourceId);
        public int AnalysisVersion => m_AnalysisVersion;
        public string SamplingRigAssetGuid => m_SamplingRigAssetGuid ?? string.Empty;
        public CharacterAnimationRigDefinition RigDefinition => m_RigDefinition;
        public CharacterFootPlacementRigCalibration RigCalibration => m_RigCalibration;
        public AnimationClip CalibrationPreviewClip => m_CalibrationPreviewClip;
        public float CalibrationPreviewNormalizedTime => m_CalibrationPreviewNormalizedTime;
        public float CalibrationPreviewTimeSeconds =>
            m_CalibrationPreviewClip ? m_CalibrationPreviewClip.length * m_CalibrationPreviewNormalizedTime : 0f;
        public float SampleRate => m_SampleRate;
        public CharacterFootPlacementAnalysisThresholds Thresholds => m_Thresholds;
        public CharacterFootPlacementCurveReductionSettings Reduction => m_Reduction;

        public void Configure(
            CharacterFootPlacementAnalysisSourceId sourceId,
            int version,
            string samplingRigAssetGuid,
            CharacterAnimationRigDefinition rigDefinition,
            CharacterFootPlacementRigCalibration calibration,
            AnimationClip calibrationPreviewClip,
            float calibrationPreviewNormalizedTime)
        {
            m_AnalysisSourceId = sourceId.Value;
            m_AnalysisVersion = version;
            m_SamplingRigAssetGuid = samplingRigAssetGuid ?? string.Empty;
            m_RigDefinition = rigDefinition;
            m_RigCalibration = calibration;
            m_CalibrationPreviewClip = calibrationPreviewClip;
            m_CalibrationPreviewNormalizedTime = calibrationPreviewNormalizedTime;
            RequireCalibrationAuthoringInput();
            m_Thresholds.RequireValid();
            m_Reduction.RequireValid();
        }

        public void RequireValid()
        {
            RequireCalibrationAuthoringInput();
            m_RigCalibration.RequireRig(m_RigDefinition);
            if (!float.IsFinite(m_SampleRate) || m_SampleRate < 1f || m_SampleRate > 240f)
                throw new InvalidOperationException("Foot Analysis sample rate must be within [1, 240] Hz.");
            if (m_Thresholds == null || m_Reduction == null)
                throw new InvalidOperationException("Foot Analysis settings are incomplete.");
            m_Thresholds.RequireValid();
            m_Reduction.RequireValid();
        }

        public void RequireCalibrationAuthoringInput()
        {
            _ = AnalysisSourceId;
            if (m_AnalysisVersion <= 0)
                throw new InvalidOperationException("Foot Analysis version must be positive.");
            if (!IsAssetGuid(m_SamplingRigAssetGuid))
                throw new InvalidOperationException("Foot Analysis Sampling Rig Asset GUID is invalid.");
            if (!m_RigDefinition)
                throw new InvalidOperationException("Foot Analysis requires a Rig Definition v4.");
            m_RigDefinition.RequireValid();
            if (!m_RigCalibration)
                throw new InvalidOperationException("Foot Analysis requires a Rig Calibration.");
            m_RigCalibration.RequireRigForAuthoring(m_RigDefinition);
            if (!m_CalibrationPreviewClip || !EditorUtility.IsPersistent(m_CalibrationPreviewClip))
                throw new InvalidOperationException("Foot Analysis requires a persisted Calibration Preview Clip.");
            if (!float.IsFinite(m_CalibrationPreviewClip.length) || m_CalibrationPreviewClip.length <= 0f)
                throw new InvalidOperationException("Foot Analysis Calibration Preview Clip must have a positive duration.");
            if (!float.IsFinite(m_CalibrationPreviewNormalizedTime) ||
                m_CalibrationPreviewNormalizedTime < 0f ||
                m_CalibrationPreviewNormalizedTime > 1f)
                throw new InvalidOperationException("Foot Analysis Calibration Preview normalized time must be within [0, 1].");
        }

        public static bool IsAssetGuid(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length != 32)
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
}
