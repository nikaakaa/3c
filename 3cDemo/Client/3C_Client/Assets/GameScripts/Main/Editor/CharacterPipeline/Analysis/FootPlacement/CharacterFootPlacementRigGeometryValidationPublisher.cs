using System;
using System.Globalization;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonCharacter.Pipeline.Simulation.Editor;
using ThirdPersonSimulation;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    static class CharacterFootPlacementRigGeometryValidationPublisher
    {
        internal static CharacterFootPlacementRigGeometryValidationIdentity RequireCurrent(
            CharacterFootPlacementAnalysisSource source)
        {
            if (!source || !source.RigDefinition || !source.RigCalibration || !source.CalibrationPreviewClip)
                throw new InvalidOperationException("Foot Placement geometry validation source is incomplete.");
            CharacterFootPlacementRigGeometryValidationIdentity identity =
                source.RigCalibration.GeometryValidation ??
                throw new InvalidOperationException("Foot Placement Calibration geometry validation identity is missing.");
            identity.RequireMatches(source.RigDefinition, source.RigCalibration);
            string samplingRigPath = AssetDatabase.GUIDToAssetPath(source.SamplingRigAssetGuid);
            string previewClipPath = AssetDatabase.GetAssetPath(source.CalibrationPreviewClip);
            string previewClipGuid = AssetDatabase.AssetPathToGUID(previewClipPath);
            if (string.IsNullOrEmpty(samplingRigPath) ||
                !AssetDatabase.LoadAssetAtPath<GameObject>(samplingRigPath) ||
                !CharacterFootPlacementAnalysisSource.IsAssetGuid(previewClipGuid))
                throw new InvalidOperationException("Foot Placement geometry validation authoring inputs no longer resolve.");
            if (!string.Equals(identity.SamplingRigAssetGuid, source.SamplingRigAssetGuid, StringComparison.Ordinal) ||
                !string.Equals(identity.SamplingRigDependencyHash, ComputeDependencyHash(samplingRigPath).Value, StringComparison.Ordinal) ||
                !string.Equals(identity.PreviewClipAssetGuid, previewClipGuid, StringComparison.Ordinal) ||
                !string.Equals(identity.PreviewClipDependencyHash, ComputePreviewPoseHash(source.CalibrationPreviewClip).Value, StringComparison.Ordinal) ||
                BitConverter.SingleToInt32Bits(identity.PreviewNormalizedTime) != BitConverter.SingleToInt32Bits(source.CalibrationPreviewNormalizedTime))
                throw new InvalidOperationException("Foot Placement geometry validation identity is stale for the current Sampling Rig or Calibration Preview Pose.");
            return identity;
        }

        internal static CharacterFootPlacementRigGeometryValidationIdentity Publish(
            CharacterFootPlacementAnalysisSource source,
            CharacterFootPlacementRigGeometryReport report)
        {
            if (!source)
                throw new ArgumentNullException(nameof(source));
            if (report == null || !report.IsValid)
                throw new InvalidOperationException("Foot Placement geometry validation cannot publish an invalid report.");
            if (!source.RigDefinition || !source.RigCalibration || !source.CalibrationPreviewClip)
                throw new InvalidOperationException("Foot Placement geometry validation source is incomplete.");
            source.RigDefinition.RequireValid();
            string samplingRigPath = AssetDatabase.GUIDToAssetPath(source.SamplingRigAssetGuid);
            if (string.IsNullOrEmpty(samplingRigPath) || !AssetDatabase.LoadAssetAtPath<GameObject>(samplingRigPath))
                throw new InvalidOperationException("Foot Placement Sampling Rig does not resolve to a Prefab asset.");
            string previewClipPath = AssetDatabase.GetAssetPath(source.CalibrationPreviewClip);
            string previewClipGuid = AssetDatabase.AssetPathToGUID(previewClipPath);
            if (!CharacterFootPlacementAnalysisSource.IsAssetGuid(previewClipGuid))
                throw new InvalidOperationException("Foot Placement Calibration Preview Clip is not a persisted asset.");
            var identity = new CharacterFootPlacementRigGeometryValidationIdentity(
                ComputeGeometryContentHash(report).Value,
                source.RigDefinition.RigId,
                source.RigDefinition.Revision,
                source.RigCalibration.CalibrationId,
                source.RigCalibration.ContentRevision,
                source.SamplingRigAssetGuid,
                ComputeDependencyHash(samplingRigPath).Value,
                previewClipGuid,
                ComputePreviewPoseHash(source.CalibrationPreviewClip).Value,
                source.CalibrationPreviewNormalizedTime);
            source.RigCalibration.PublishGeometryValidation(identity);
            return identity;
        }

        static StableHash ComputeGeometryContentHash(CharacterFootPlacementRigGeometryReport report)
        {
            return StableHash.Compute(
                "character-foot-placement-rig-geometry/v2",
                Format(report.ReferenceGroundHeight),
                Format(report.Left),
                Format(report.Right));
        }

        static StableHash ComputeDependencyHash(string assetPath)
        {
            return StableHash.Compute(
                "unity-asset-dependency/v1",
                AssetDatabase.GetAssetDependencyHash(assetPath).ToString());
        }

        static StableHash ComputePreviewPoseHash(AnimationClip clip) =>
            StableHash.Compute(
                "character-foot-placement-preview-pose/v1",
                CharacterAnimationClipRegisteredCurveCatalog.ComputeAnalysisInputHash(clip));

        static string Format(CharacterFootPlacementFootRigGeometry value)
        {
            return string.Join("|",
                Format(value.HeelContact),
                Format(value.ToeContact),
                Format(value.SoleForward),
                Format(value.SoleUp),
                Format(value.SoleRotation),
                Format(value.LegLength),
                Format(value.SoleLength),
                Format(value.ContactGroundError),
                Format(value.SoleForwardErrorDegrees),
                Format(value.SoleUpErrorDegrees),
                Format(value.FlatGroundCorrectionDegrees));
        }

        static string Format(Vector3 value) => string.Join("|", Format(value.x), Format(value.y), Format(value.z));
        static string Format(Quaternion value) => string.Join("|", Format(value.x), Format(value.y), Format(value.z), Format(value.w));
        static string Format(float value) => value.ToString("R", CultureInfo.InvariantCulture);
    }
}
