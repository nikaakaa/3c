using System;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    public enum AnimationFootAnalysisArtifactStatus : byte
    {
        Missing = 0,
        Stale = 1,
        Ready = 2,
        Corrupt = 3
    }

    public sealed class AnimationFootAnalysisArtifactIdentity
    {
        public const int CurrentFormatVersion = 6;

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
            float sampleRate,
            float plantEnterVerticalSpeed,
            float plantExitVerticalSpeed,
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
            SampleRate = RequireFinitePositive(sampleRate, nameof(sampleRate));
            PlantEnterVerticalSpeed = RequireFiniteNonNegative(plantEnterVerticalSpeed, nameof(plantEnterVerticalSpeed));
            PlantExitVerticalSpeed = RequireFinitePositive(plantExitVerticalSpeed, nameof(plantExitVerticalSpeed));
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
        public float SampleRate { get; }
        public float PlantEnterVerticalSpeed { get; }
        public float PlantExitVerticalSpeed { get; }
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
            SampleRate.Equals(other.SampleRate) && PlantEnterVerticalSpeed.Equals(other.PlantEnterVerticalSpeed) &&
            PlantExitVerticalSpeed.Equals(other.PlantExitVerticalSpeed) && PlantEnterHeight.Equals(other.PlantEnterHeight) &&
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
                "animation-foot-analysis-artifact/v6", ClipAssetGuid, ClipDependencyHash,
                AnalysisSourceAssetGuid, AnalysisSourceDependencyHash, AnalysisSourceId,
                AnalysisVersion.ToString(System.Globalization.CultureInfo.InvariantCulture),
                RigAssetGuid, RigId, RigRevision, RigContentHash,
                SamplingRigAssetGuid, SamplingRigDependencyHash, CalibrationAssetGuid, CalibrationId,
                CalibrationSchemaVersion.ToString(System.Globalization.CultureInfo.InvariantCulture),
                CalibrationRevision, GeometryValidationIdentity, GeometryValidationContentHash,
                Bits(SampleRate), Bits(PlantEnterVerticalSpeed), Bits(PlantExitVerticalSpeed),
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
            StableHash contentHash)
        {
            Identity = identity ?? throw new ArgumentNullException(nameof(identity));
            if (!features.IsValid)
                throw new ArgumentException("Animation Foot Analysis artifact features are invalid.", nameof(features));
            if (!contentHash.IsValid)
                throw new ArgumentException("Animation Foot Analysis artifact content hash is invalid.", nameof(contentHash));
            Features = features;
            ContentHash = contentHash;
        }

        public AnimationFootAnalysisArtifactIdentity Identity { get; }
        public AnimationFootFeaturePair Features { get; }
        public StableHash ContentHash { get; }
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
