using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    public sealed class AnimationFootContactSchedule
    {
        public const string LeftMarkerId = "LeftFootContact";
        public const string RightMarkerId = "RightFootContact";

        readonly float[] m_LeftLandingPhases;
        readonly float[] m_RightLandingPhases;

        AnimationFootContactSchedule(bool inferLandingEvents, IEnumerable<float> left, IEnumerable<float> right)
        {
            InferLandingEvents = inferLandingEvents;
            m_LeftLandingPhases = Normalize(left, nameof(left));
            m_RightLandingPhases = Normalize(right, nameof(right));
            var parts = new List<string>
            {
                "animation-foot-contact-schedule/v1",
                inferLandingEvents ? "proposal" : "authored"
            };
            Append(parts, LeftMarkerId, m_LeftLandingPhases);
            Append(parts, RightMarkerId, m_RightLandingPhases);
            IdentityHash = StableHash.Compute(parts.ToArray());
        }

        public static AnimationFootContactSchedule Inferred { get; } =
            new AnimationFootContactSchedule(true, Array.Empty<float>(), Array.Empty<float>());

        public static AnimationFootContactSchedule None { get; } =
            new AnimationFootContactSchedule(false, Array.Empty<float>(), Array.Empty<float>());

        public static AnimationFootContactSchedule Authored(
            IEnumerable<float> leftLandingPhases,
            IEnumerable<float> rightLandingPhases) =>
            new AnimationFootContactSchedule(false, leftLandingPhases, rightLandingPhases);

        public bool InferLandingEvents { get; }
        public IReadOnlyList<float> LeftLandingPhases => m_LeftLandingPhases;
        public IReadOnlyList<float> RightLandingPhases => m_RightLandingPhases;
        public StableHash IdentityHash { get; }

        static float[] Normalize(IEnumerable<float> values, string field)
        {
            float[] result = values?.OrderBy(value => value).ToArray() ?? Array.Empty<float>();
            for (int i = 0; i < result.Length; i++)
            {
                if (!float.IsFinite(result[i]) || result[i] < 0f || result[i] > 1f ||
                    i > 0 && Math.Abs(result[i] - result[i - 1]) <= 0.000001f)
                    throw new ArgumentException("Foot contact schedule contains an invalid or duplicate phase.", field);
            }
            return result;
        }

        static void Append(List<string> parts, string markerId, IReadOnlyList<float> phases)
        {
            parts.Add(markerId);
            parts.Add(phases.Count.ToString(CultureInfo.InvariantCulture));
            for (int i = 0; i < phases.Count; i++)
                parts.Add(BitConverter.SingleToInt32Bits(phases[i]).ToString("x8", CultureInfo.InvariantCulture));
        }
    }

    public enum AnimationFootAnalysisArtifactStatus : byte
    {
        Missing = 0,
        Stale = 1,
        Ready = 2,
        Corrupt = 3
    }

    public sealed class AnimationFootAnalysisArtifactIdentity
    {
        public const int CurrentFormatVersion = 29;

        public AnimationFootAnalysisArtifactIdentity(
            string clipAssetGuid,
            string clipDependencyHash,
            string analysisSourceAssetGuid,
            string analysisSourceDependencyHash,
            string analysisSourceId,
            int analysisVersion,
            string rigAssetGuid,
            string rigId,
            string rigRevision,
            string rigContentHash,
            string samplingRigAssetGuid,
            string samplingRigDependencyHash,
            string calibrationAssetGuid,
            string calibrationId,
            int calibrationSchemaVersion,
            string calibrationRevision,
            string geometryValidationIdentity,
            string geometryValidationContentHash,
            string contactScheduleHash,
            float sampleRate,
            float plantEnterContactSpeed,
            float plantExitContactSpeed,
            float plantEnterHeight,
            float plantExitHeight,
            float minimumLandingSegmentSeconds,
            float maximumLandingSearchSeconds,
            float velocityTolerance,
            float heightTolerance,
            float confidenceTolerance,
            float landingDelayTolerance,
            float landingOffsetTolerance,
            string algorithmVersion)
        {
            ClipAssetGuid = RequireGuid(clipAssetGuid, nameof(clipAssetGuid));
            ClipDependencyHash = RequireHash(clipDependencyHash, nameof(clipDependencyHash));
            AnalysisSourceAssetGuid = RequireGuid(analysisSourceAssetGuid, nameof(analysisSourceAssetGuid));
            AnalysisSourceDependencyHash = RequireHash(analysisSourceDependencyHash, nameof(analysisSourceDependencyHash));
            AnalysisSourceId = RequireText(analysisSourceId, nameof(analysisSourceId));
            if (analysisVersion <= 0)
                throw new ArgumentOutOfRangeException(nameof(analysisVersion));
            AnalysisVersion = analysisVersion;
            RigAssetGuid = RequireGuid(rigAssetGuid, nameof(rigAssetGuid));
            RigId = RequireText(rigId, nameof(rigId));
            RigRevision = RequireText(rigRevision, nameof(rigRevision));
            RigContentHash = RequireHash(rigContentHash, nameof(rigContentHash));
            SamplingRigAssetGuid = RequireGuid(samplingRigAssetGuid, nameof(samplingRigAssetGuid));
            SamplingRigDependencyHash = RequireHash(samplingRigDependencyHash, nameof(samplingRigDependencyHash));
            CalibrationAssetGuid = RequireGuid(calibrationAssetGuid, nameof(calibrationAssetGuid));
            CalibrationId = RequireText(calibrationId, nameof(calibrationId));
            if (calibrationSchemaVersion != CharacterFootPlacementRigCalibration.CurrentSchemaVersion)
                throw new ArgumentOutOfRangeException(nameof(calibrationSchemaVersion));
            CalibrationSchemaVersion = calibrationSchemaVersion;
            CalibrationRevision = RequireText(calibrationRevision, nameof(calibrationRevision));
            GeometryValidationIdentity = RequireHash(geometryValidationIdentity, nameof(geometryValidationIdentity));
            GeometryValidationContentHash = RequireHash(geometryValidationContentHash, nameof(geometryValidationContentHash));
            ContactScheduleHash = RequireHash(contactScheduleHash, nameof(contactScheduleHash));
            SampleRate = RequireFinitePositive(sampleRate, nameof(sampleRate));
            PlantEnterContactSpeed = RequireFiniteNonNegative(plantEnterContactSpeed, nameof(plantEnterContactSpeed));
            PlantExitContactSpeed = RequireFinitePositive(plantExitContactSpeed, nameof(plantExitContactSpeed));
            PlantEnterHeight = RequireFiniteNonNegative(plantEnterHeight, nameof(plantEnterHeight));
            PlantExitHeight = RequireFinitePositive(plantExitHeight, nameof(plantExitHeight));
            MinimumLandingSegmentSeconds = RequireFinitePositive(minimumLandingSegmentSeconds, nameof(minimumLandingSegmentSeconds));
            MaximumLandingSearchSeconds = RequireFinitePositive(maximumLandingSearchSeconds, nameof(maximumLandingSearchSeconds));
            VelocityTolerance = RequireFinitePositive(velocityTolerance, nameof(velocityTolerance));
            HeightTolerance = RequireFinitePositive(heightTolerance, nameof(heightTolerance));
            ConfidenceTolerance = RequireFinitePositive(confidenceTolerance, nameof(confidenceTolerance));
            LandingDelayTolerance = RequireFinitePositive(landingDelayTolerance, nameof(landingDelayTolerance));
            LandingOffsetTolerance = RequireFinitePositive(landingOffsetTolerance, nameof(landingOffsetTolerance));
            AlgorithmVersion = RequireText(algorithmVersion, nameof(algorithmVersion));
            IdentityHash = StableHash.Compute(ToIdentityParts());
        }

        public int FormatVersion => CurrentFormatVersion;
        public string ClipAssetGuid { get; }
        public string ClipDependencyHash { get; }
        public string AnalysisSourceAssetGuid { get; }
        public string AnalysisSourceDependencyHash { get; }
        public string AnalysisSourceId { get; }
        public int AnalysisVersion { get; }
        public string RigAssetGuid { get; }
        public string RigId { get; }
        public string RigRevision { get; }
        public string RigContentHash { get; }
        public string SamplingRigAssetGuid { get; }
        public string SamplingRigDependencyHash { get; }
        public string CalibrationAssetGuid { get; }
        public string CalibrationId { get; }
        public int CalibrationSchemaVersion { get; }
        public string CalibrationRevision { get; }
        public string GeometryValidationIdentity { get; }
        public string GeometryValidationContentHash { get; }
        public string ContactScheduleHash { get; }
        public float SampleRate { get; }
        public float PlantEnterContactSpeed { get; }
        public float PlantExitContactSpeed { get; }
        public float PlantEnterHeight { get; }
        public float PlantExitHeight { get; }
        public float MinimumLandingSegmentSeconds { get; }
        public float MaximumLandingSearchSeconds { get; }
        public float VelocityTolerance { get; }
        public float HeightTolerance { get; }
        public float ConfidenceTolerance { get; }
        public float LandingDelayTolerance { get; }
        public float LandingOffsetTolerance { get; }
        public string AlgorithmVersion { get; }
        public StableHash IdentityHash { get; }

        public bool EqualsExact(AnimationFootAnalysisArtifactIdentity other) =>
            other != null && IdentityHash.Equals(other.IdentityHash) &&
            string.Equals(ClipAssetGuid, other.ClipAssetGuid, StringComparison.Ordinal) &&
            string.Equals(ClipDependencyHash, other.ClipDependencyHash, StringComparison.Ordinal) &&
            string.Equals(AnalysisSourceAssetGuid, other.AnalysisSourceAssetGuid, StringComparison.Ordinal) &&
            string.Equals(AnalysisSourceDependencyHash, other.AnalysisSourceDependencyHash, StringComparison.Ordinal) &&
            string.Equals(AnalysisSourceId, other.AnalysisSourceId, StringComparison.Ordinal) &&
            AnalysisVersion == other.AnalysisVersion &&
            string.Equals(RigAssetGuid, other.RigAssetGuid, StringComparison.Ordinal) &&
            string.Equals(RigId, other.RigId, StringComparison.Ordinal) &&
            string.Equals(RigRevision, other.RigRevision, StringComparison.Ordinal) &&
            string.Equals(RigContentHash, other.RigContentHash, StringComparison.Ordinal) &&
            string.Equals(SamplingRigAssetGuid, other.SamplingRigAssetGuid, StringComparison.Ordinal) &&
            string.Equals(SamplingRigDependencyHash, other.SamplingRigDependencyHash, StringComparison.Ordinal) &&
            string.Equals(CalibrationAssetGuid, other.CalibrationAssetGuid, StringComparison.Ordinal) &&
            string.Equals(CalibrationId, other.CalibrationId, StringComparison.Ordinal) &&
            CalibrationSchemaVersion == other.CalibrationSchemaVersion &&
            string.Equals(CalibrationRevision, other.CalibrationRevision, StringComparison.Ordinal) &&
            string.Equals(GeometryValidationIdentity, other.GeometryValidationIdentity, StringComparison.Ordinal) &&
            string.Equals(GeometryValidationContentHash, other.GeometryValidationContentHash, StringComparison.Ordinal) &&
            string.Equals(ContactScheduleHash, other.ContactScheduleHash, StringComparison.Ordinal) &&
            SampleRate.Equals(other.SampleRate) && PlantEnterContactSpeed.Equals(other.PlantEnterContactSpeed) &&
            PlantExitContactSpeed.Equals(other.PlantExitContactSpeed) && PlantEnterHeight.Equals(other.PlantEnterHeight) &&
            PlantExitHeight.Equals(other.PlantExitHeight) &&
            MinimumLandingSegmentSeconds.Equals(other.MinimumLandingSegmentSeconds) &&
            MaximumLandingSearchSeconds.Equals(other.MaximumLandingSearchSeconds) &&
            VelocityTolerance.Equals(other.VelocityTolerance) && HeightTolerance.Equals(other.HeightTolerance) &&
            ConfidenceTolerance.Equals(other.ConfidenceTolerance) &&
            LandingDelayTolerance.Equals(other.LandingDelayTolerance) &&
            LandingOffsetTolerance.Equals(other.LandingOffsetTolerance) &&
            string.Equals(AlgorithmVersion, other.AlgorithmVersion, StringComparison.Ordinal);

        string[] ToIdentityParts()
        {
            return new[]
            {
                "animation-foot-analysis-artifact/v18", ClipAssetGuid, ClipDependencyHash,
                AnalysisSourceAssetGuid, AnalysisSourceDependencyHash, AnalysisSourceId,
                AnalysisVersion.ToString(System.Globalization.CultureInfo.InvariantCulture),
                RigAssetGuid, RigId, RigRevision, RigContentHash,
                SamplingRigAssetGuid, SamplingRigDependencyHash, CalibrationAssetGuid, CalibrationId,
                CalibrationSchemaVersion.ToString(System.Globalization.CultureInfo.InvariantCulture),
                CalibrationRevision, GeometryValidationIdentity, GeometryValidationContentHash, ContactScheduleHash,
                Bits(SampleRate), Bits(PlantEnterContactSpeed), Bits(PlantExitContactSpeed),
                Bits(PlantEnterHeight), Bits(PlantExitHeight), Bits(MinimumLandingSegmentSeconds),
                Bits(MaximumLandingSearchSeconds), Bits(VelocityTolerance), Bits(HeightTolerance),
                Bits(ConfidenceTolerance), Bits(LandingDelayTolerance), Bits(LandingOffsetTolerance),
                AlgorithmVersion
            };
        }

        static string Bits(float value) => BitConverter.SingleToInt32Bits(value).ToString("x8");

        static string RequireGuid(string value, string field)
        {
            if (!CharacterFootPlacementAnalysisSource.IsAssetGuid(value))
                throw new ArgumentException($"Animation Foot Analysis identity '{field}' is not an asset GUID.", field);
            return value;
        }

        static string RequireHash(string value, string field)
        {
            if (string.IsNullOrEmpty(value) || value.Length != 32 && value.Length != 64)
                throw new ArgumentException($"Animation Foot Analysis identity '{field}' is not a dependency hash.", field);
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (c < '0' || c > '9' && c < 'a' || c > 'f')
                    throw new ArgumentException($"Animation Foot Analysis identity '{field}' is not a dependency hash.", field);
            }
            return value;
        }

        static string RequireText(string value, string field)
        {
            if (string.IsNullOrWhiteSpace(value) || !string.Equals(value, value.Trim(), StringComparison.Ordinal))
                throw new ArgumentException($"Animation Foot Analysis identity '{field}' is invalid.", field);
            return value;
        }

        static float RequireFinitePositive(float value, string field)
        {
            if (!float.IsFinite(value) || value <= 0f)
                throw new ArgumentOutOfRangeException(field);
            return value;
        }

        static float RequireFiniteNonNegative(float value, string field)
        {
            if (!float.IsFinite(value) || value < 0f)
                throw new ArgumentOutOfRangeException(field);
            return value;
        }
    }

    public sealed class AnimationFootAnalysisArtifact
    {
        public AnimationFootAnalysisArtifact(
            AnimationFootAnalysisArtifactIdentity identity,
            AnimationFootFeaturePair features,
            AnimationFootSynchronizationDescriptor synchronization,
            StableHash contentHash)
        {
            Identity = identity ?? throw new ArgumentNullException(nameof(identity));
            if (!features.IsValid)
                throw new ArgumentException("Animation Foot Analysis artifact features are invalid.", nameof(features));
            if (!contentHash.IsValid)
                throw new ArgumentException("Animation Foot Analysis artifact content hash is invalid.", nameof(contentHash));
            Synchronization = synchronization ??
                throw new ArgumentNullException(nameof(synchronization));
            Synchronization.RequireValid();
            Features = features;
            ContentHash = contentHash;
        }

        public AnimationFootAnalysisArtifactIdentity Identity { get; }
        public AnimationFootFeaturePair Features { get; }
        public AnimationFootSynchronizationDescriptor Synchronization { get; }
        public StableHash ContentHash { get; }
    }

    public readonly struct AnimationFootSynchronizationSample
    {
        public AnimationFootSynchronizationSample(
            float normalizedTime,
            Vector2 rootLocalSolePlanarPosition,
            float calibratedSoleHeight,
            Vector3 soleLocalVelocity,
            float plantConfidence)
        {
            NormalizedTime = normalizedTime;
            RootLocalSolePlanarPosition = rootLocalSolePlanarPosition;
            CalibratedSoleHeight = calibratedSoleHeight;
            SoleLocalVelocity = soleLocalVelocity;
            PlantConfidence = plantConfidence;
            RequireValid();
        }

        public float NormalizedTime { get; }
        public Vector2 RootLocalSolePlanarPosition { get; }
        public float CalibratedSoleHeight { get; }
        public Vector3 SoleLocalVelocity { get; }
        public float PlantConfidence { get; }

        public void RequireValid()
        {
            if (!float.IsFinite(NormalizedTime) || NormalizedTime < 0f || NormalizedTime > 1f ||
                !float.IsFinite(RootLocalSolePlanarPosition.x) ||
                !float.IsFinite(RootLocalSolePlanarPosition.y) ||
                !float.IsFinite(CalibratedSoleHeight) ||
                !float.IsFinite(SoleLocalVelocity.x) ||
                !float.IsFinite(SoleLocalVelocity.y) ||
                !float.IsFinite(SoleLocalVelocity.z) ||
                !float.IsFinite(PlantConfidence) || PlantConfidence < 0f || PlantConfidence > 1f)
                throw new InvalidOperationException("Foot synchronization sample is invalid.");
        }
    }

    public sealed class AnimationFootSynchronizationFootDescriptor
    {
        readonly AnimationFootSynchronizationSample[] m_Samples;

        public AnimationFootSynchronizationFootDescriptor(
            AnimationFootSynchronizationSample[] samples)
        {
            m_Samples = samples == null
                ? throw new ArgumentNullException(nameof(samples))
                : (AnimationFootSynchronizationSample[])samples.Clone();
            RequireValid();
        }

        public IReadOnlyList<AnimationFootSynchronizationSample> Samples => m_Samples;

        public void RequireValid()
        {
            if (m_Samples == null || m_Samples.Length < 3)
                throw new InvalidOperationException("Foot synchronization descriptor requires at least three samples.");
            for (int i = 0; i < m_Samples.Length; i++)
            {
                m_Samples[i].RequireValid();
                if (i > 0 && m_Samples[i].NormalizedTime <= m_Samples[i - 1].NormalizedTime)
                    throw new InvalidOperationException("Foot synchronization sample time is not strictly increasing.");
            }
            if (m_Samples[0].NormalizedTime != 0f ||
                m_Samples[m_Samples.Length - 1].NormalizedTime != 1f)
                throw new InvalidOperationException("Foot synchronization descriptor must cover normalized time [0, 1].");
        }
    }

    public sealed class AnimationFootSynchronizationDescriptor
    {
        public AnimationFootSynchronizationDescriptor(
            float sampleRate,
            float durationSeconds,
            AnimationFootSynchronizationFootDescriptor left,
            AnimationFootSynchronizationFootDescriptor right)
        {
            SampleRate = sampleRate;
            DurationSeconds = durationSeconds;
            Left = left ?? throw new ArgumentNullException(nameof(left));
            Right = right ?? throw new ArgumentNullException(nameof(right));
            RequireValid();
        }

        public float SampleRate { get; }
        public float DurationSeconds { get; }
        public AnimationFootSynchronizationFootDescriptor Left { get; }
        public AnimationFootSynchronizationFootDescriptor Right { get; }

        public void RequireValid()
        {
            if (!float.IsFinite(SampleRate) || SampleRate <= 0f ||
                !float.IsFinite(DurationSeconds) || DurationSeconds <= 0f)
                throw new InvalidOperationException("Foot synchronization descriptor timing is invalid.");
            Left?.RequireValid();
            Right?.RequireValid();
            if (Left == null || Right == null || Left.Samples.Count != Right.Samples.Count)
                throw new InvalidOperationException("Foot synchronization descriptor sample counts do not match.");
        }
    }

    public readonly struct AnimationFootAnalysisBuildResult
    {
        public AnimationFootAnalysisBuildResult(
            AnimationFootFeaturePair features,
            AnimationFootSynchronizationDescriptor synchronization)
        {
            if (!features.IsValid)
                throw new ArgumentException("Foot Analysis features are invalid.", nameof(features));
            Features = features;
            Synchronization = synchronization ?? throw new ArgumentNullException(nameof(synchronization));
            Synchronization.RequireValid();
        }

        public AnimationFootFeaturePair Features { get; }
        public AnimationFootSynchronizationDescriptor Synchronization { get; }
    }

    public readonly struct AnimationFootAnalysisArtifactInspection
    {
        public AnimationFootAnalysisArtifactInspection(
            AnimationFootAnalysisArtifactStatus status,
            string path,
            AnimationFootAnalysisArtifact artifact,
            string error)
        {
            Status = status;
            Path = path ?? string.Empty;
            Artifact = artifact;
            Error = error ?? string.Empty;
        }

        public AnimationFootAnalysisArtifactStatus Status { get; }
        public string Path { get; }
        public AnimationFootAnalysisArtifact Artifact { get; }
        public string Error { get; }
    }
}
