using System;
using ThirdPersonCharacter.Pipeline.Animation;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    internal static class CharacterFootPlacementPublicationValidation
    {
        internal static void Require(
            CharacterPresentationProjection projection,
            CharacterFootPlacementRigCalibration calibration)
        {
            if (projection == null)
                throw new ArgumentNullException(nameof(projection));
            if (!calibration)
                throw new ArgumentNullException(nameof(calibration));
            calibration.RequireValid();
            CharacterFootPlacementRigGeometryValidationIdentity geometry =
                calibration.GeometryValidation ??
                throw new InvalidOperationException("Foot Placement geometry validation identity is missing.");
            geometry.RequireValid();
            if (!string.Equals(geometry.RigId, projection.Rig.RigId, StringComparison.Ordinal) ||
                !string.Equals(geometry.RigRevision, projection.Rig.RigRevision, StringComparison.Ordinal) ||
                geometry.CalibrationId != calibration.CalibrationId ||
                !string.Equals(geometry.CalibrationRevision, calibration.ContentRevision, StringComparison.Ordinal))
                throw new InvalidOperationException("Foot Placement geometry validation does not match the published Rig and Calibration revision.");
            AnimationFootAnalysisProjectionIdentity footAnalysis = projection.FootAnalysis;
            if (footAnalysis == null || !footAnalysis.IsEnabled ||
                footAnalysis.CalibrationId != calibration.CalibrationId ||
                footAnalysis.CalibrationSchemaVersion != calibration.SchemaVersion ||
                !string.Equals(footAnalysis.CalibrationRevision, calibration.ContentRevision, StringComparison.Ordinal) ||
                !string.Equals(footAnalysis.GeometryValidationIdentity, geometry.IdentityHash, StringComparison.Ordinal) ||
                !string.Equals(footAnalysis.GeometryValidationContentHash, geometry.GeometryContentHash, StringComparison.Ordinal))
                throw new InvalidOperationException("Foot Placement Projection, Foot Analysis artifact and Calibration geometry identities do not match exactly.");
        }
    }
}
