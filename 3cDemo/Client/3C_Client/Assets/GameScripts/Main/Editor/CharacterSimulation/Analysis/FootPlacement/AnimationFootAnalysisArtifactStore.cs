using System;
using System.IO;
using System.Linq;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Editor;
using ThirdPersonCharacter.Pipeline.Presentation;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    public static class AnimationFootAnalysisArtifactIdentityBuilder
    {
        public static AnimationFootAnalysisArtifactIdentity Build(
            UnityEngine.AnimationClip clip,
            CharacterFootPlacementAnalysisSource source,
            AnimationFootContactSchedule contactSchedule)
        {
            if (!clip)
                throw new ArgumentNullException(nameof(clip));
            if (!source)
                throw new ArgumentNullException(nameof(source));
            if (contactSchedule == null)
                throw new ArgumentNullException(nameof(contactSchedule));
            source.RequireValid();
            string clipPath = RequireAssetPath(clip, "AnimationClip");
            string sourcePath = RequireAssetPath(source, "Analysis Source");
            string rigDefinitionPath = RequireAssetPath(source.RigDefinition, "Rig Definition");
            string rigPath = AssetDatabase.GUIDToAssetPath(source.SamplingRigAssetGuid);
            if (string.IsNullOrEmpty(rigPath) || !AssetDatabase.LoadAssetAtPath<GameObject>(rigPath))
                throw new InvalidOperationException($"Sampling Rig GUID '{source.SamplingRigAssetGuid}' does not resolve to a Prefab asset.");
            string calibrationPath = RequireAssetPath(source.RigCalibration, "Rig Calibration");
            CharacterFootPlacementRigGeometryValidationIdentity geometryValidation =
                source.RigCalibration.GeometryValidation ??
                throw new InvalidOperationException("Foot Placement Calibration geometry validation identity is missing.");
            geometryValidation.RequireMatches(source.RigDefinition, source.RigCalibration);
            CharacterFootPlacementAnalysisThresholds thresholds = source.Thresholds;
            CharacterFootPlacementCurveReductionSettings reduction = source.Reduction;
            CharacterAnimationClipContentIdentity clipIdentity =
                CharacterAnimationClipRegisteredCurveCatalog.ResolveIdentity(clip);
            return new AnimationFootAnalysisArtifactIdentity(
                clipIdentity.AssetGuid,
                clipIdentity.AnalysisInputHash,
                AssetDatabase.AssetPathToGUID(sourcePath),
                AssetDatabase.GetAssetDependencyHash(sourcePath).ToString(),
                source.AnalysisSourceId.Value,
                source.AnalysisVersion,
                AssetDatabase.AssetPathToGUID(rigDefinitionPath),
                source.RigDefinition.RigId,
                source.RigDefinition.Revision,
                AssetDatabase.GetAssetDependencyHash(rigDefinitionPath).ToString(),
                source.SamplingRigAssetGuid,
                AssetDatabase.GetAssetDependencyHash(rigPath).ToString(),
                AssetDatabase.AssetPathToGUID(calibrationPath),
                source.RigCalibration.CalibrationId.Value,
                source.RigCalibration.SchemaVersion,
                source.RigCalibration.ContentRevision,
                geometryValidation.IdentityHash,
                geometryValidation.GeometryContentHash,
                contactSchedule.IdentityHash.Value,
                source.SampleRate,
                thresholds.PlantEnterContactSpeed,
                thresholds.PlantExitContactSpeed,
                thresholds.PlantEnterHeight,
                thresholds.PlantExitHeight,
                thresholds.MinimumLandingSegmentSeconds,
                thresholds.MaximumLandingSearchSeconds,
                reduction.VelocityTolerance,
                reduction.HeightTolerance,
                reduction.ConfidenceTolerance,
                reduction.LandingDelayTolerance,
                reduction.LandingOffsetTolerance,
                CharacterFootPlacementAnalysisSource.AlgorithmVersion);
        }

        static string RequireAssetPath(UnityEngine.Object value, string label)
        {
            string path = AssetDatabase.GetAssetPath(value);
            if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(path)))
                throw new InvalidOperationException($"{label} must be a persisted asset.");
            return path;
        }
    }

    public static class AnimationFootAnalysisArtifactStore
    {
        public static string RootPath => Path.GetFullPath(Path.Combine("Library", "CharacterFootAnalysis"));

        public static string GetPath(AnimationFootAnalysisArtifactIdentity identity)
        {
            if (identity == null)
                throw new ArgumentNullException(nameof(identity));
            return Path.Combine(
                RootPath,
                identity.AnalysisSourceAssetGuid,
                identity.ClipAssetGuid,
                identity.IdentityHash.Value + ".foot-analysis");
        }

        public static AnimationFootAnalysisArtifactInspection Inspect(AnimationFootAnalysisArtifactIdentity expected)
        {
            if (expected == null)
                throw new ArgumentNullException(nameof(expected));
            string path = GetPath(expected);
            if (!File.Exists(path))
            {
                string directory = Path.GetDirectoryName(path);
                bool hasPriorIdentity = Directory.Exists(directory) &&
                    Directory.EnumerateFiles(directory, "*.foot-analysis", SearchOption.TopDirectoryOnly).Any();
                return new AnimationFootAnalysisArtifactInspection(
                    hasPriorIdentity ? AnimationFootAnalysisArtifactStatus.Stale : AnimationFootAnalysisArtifactStatus.Missing,
                    path,
                    null,
                    hasPriorIdentity ? "A prior artifact exists, but its exact input identity is stale." : string.Empty);
            }
            try
            {
                AnimationFootAnalysisArtifact artifact = AnimationFootAnalysisArtifactCodec.Read(File.ReadAllBytes(path));
                if (!artifact.Identity.EqualsExact(expected))
                {
                    return new AnimationFootAnalysisArtifactInspection(
                        AnimationFootAnalysisArtifactStatus.Corrupt,
                        path,
                        null,
                        "Artifact identity does not match the expected identity encoded by its path.");
                }
                return new AnimationFootAnalysisArtifactInspection(
                    AnimationFootAnalysisArtifactStatus.Ready,
                    path,
                    artifact,
                    string.Empty);
            }
            catch (Exception exception)
            {
                return new AnimationFootAnalysisArtifactInspection(
                    AnimationFootAnalysisArtifactStatus.Corrupt,
                    path,
                    null,
                    exception.Message);
            }
        }

        public static AnimationFootAnalysisArtifact Write(
            AnimationFootAnalysisArtifactIdentity identity,
            AnimationFootFeaturePair features,
            AnimationFootPhaseValidationDescriptor phaseValidation)
        {
            string path = GetPath(identity);
            string directory = Path.GetDirectoryName(path) ?? throw new InvalidOperationException("Artifact path has no directory.");
            Directory.CreateDirectory(directory);
            byte[] bytes = AnimationFootAnalysisArtifactCodec.Write(
                identity,
                features,
                phaseValidation,
                out _);
            string temporaryPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllBytes(temporaryPath, bytes);
                AnimationFootAnalysisArtifact verified = AnimationFootAnalysisArtifactCodec.Read(File.ReadAllBytes(temporaryPath));
                if (!verified.Identity.EqualsExact(identity))
                    throw new InvalidDataException("Written Animation Foot Analysis artifact identity does not round-trip.");
                if (File.Exists(path))
                    File.Replace(temporaryPath, path, null);
                else
                    File.Move(temporaryPath, path);
                AnimationFootAnalysisArtifact published = AnimationFootAnalysisArtifactCodec.Read(File.ReadAllBytes(path));
                if (!published.Identity.EqualsExact(identity) || !published.ContentHash.Equals(verified.ContentHash))
                    throw new InvalidDataException("Published Animation Foot Analysis artifact does not match staged bytes.");
                return published;
            }
            finally
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);
            }
        }
    }
}
