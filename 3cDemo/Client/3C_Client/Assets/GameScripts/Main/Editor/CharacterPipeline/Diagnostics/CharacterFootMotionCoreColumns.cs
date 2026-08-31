using UnityEngine;
using Source = ThirdPersonCharacter.Pipeline.Editor.CharacterFootMotionCoreCsvSource;
using Column = ThirdPersonCharacter.Pipeline.Editor.CharacterFootCsvColumn<ThirdPersonCharacter.Pipeline.Editor.CharacterFootMotionCoreCsvSource, ThirdPersonCharacter.Pipeline.Editor.CharacterFootMotionCoreSample>;
using Codecs = ThirdPersonCharacter.Pipeline.Editor.CharacterFootCsvCodecs;
using Unit = ThirdPersonCharacter.Pipeline.Editor.CharacterFootCsvUnit;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    internal sealed class CharacterFootMotionCoreSample
    {
        internal string State;
        internal string RejectReason;
        internal ulong LandingEventIdentity;
        internal ulong GroundPathInputIdentity;
        internal float Distance;
        internal float SwingProgress;
        internal Vector3 OriginalSole;
        internal Vector3 OriginalAnkle;
        internal Quaternion SourceAnkleRotation;
        internal Vector3 SourceHeel;
        internal Vector3 SourceToe;
        internal Vector3 SwingBaselineSample;
        internal float SwingBaselineSampleAlongUp;
        internal Vector3 SwingEnvelopeSample;
        internal float SwingEnvelopeSampleAlongUp;
        internal float SwingFormalFootHeight;
        internal float SwingRawFormalTargetHeight;
        internal float SwingEnvelopeMinimumCorrection;
        internal float SwingBuilderSelectedCorrection;
        internal bool BuilderSwingTargetAvailable;
        internal Vector3 BuilderSwingTargetCorrection;
        internal string SwingPathHorizontalAxisState;
        internal float ActualFootHorizontalDistance;
        internal float BaselineHorizontalDistance;
        internal float EnvelopeHorizontalDistance;
        internal float ActualMinusEnvelopeHorizontalDistance;
        internal string ActualFootAxisRegion;
        internal float ActualFootClosestPathParameter;
        internal float ActualFootDistanceAlongAxis;
        internal float ActualFootCrossTrackDistance;
        internal float ActualFootGroundPathCorridorRadius;
        internal bool ActualFootWithinGroundPathCorridor;
        internal string ActualEnvelopeIntersectionState;
        internal int ActualEnvelopeCandidateCount;
        internal float ActualEnvelopeMinimumHeightAlongUp;
        internal float ActualEnvelopeMaximumHeightAlongUp;
        internal float ActualEnvelopeHeightSpan;
        internal bool ActualEnvelopeHasVerticalEdge;
        internal bool ActualEnvelopeHasMultipleHeights;
        internal bool ActualEnvelopeAmbiguous;
        internal string ActualEnvelopeCounterfactualState;
        internal bool ActualProgressEnvelopeCorrectionAvailable;
        internal float ActualProgressEnvelopeMinimumCorrection;
        internal float ActualProgressEnvelopeAdvanceAboveBuilderTarget;
        internal float LandingPredictionError;
        internal Vector3 CorrectedSole;
        internal Vector3 CorrectedAnkle;
        internal float MotionPositionWeight;
        internal float MotionRotationWeight;
        internal string ConstraintState;
        internal string LockResponse;
        internal float SupportHorizontalError;
        internal float ContactOwnership;
        internal float SupportWeight;
        internal bool LandingReachEvaluated;
        internal bool LandingReachAvailable;
        internal Vector3 Anchor;
        internal bool ContactPlaneAvailable;
        internal int ContactSurfaceIdentity;
        internal Vector3 ContactNormal;
        internal string PenetrationAvailability;
        internal Vector3 SwingDesiredCorrection;
    }

    internal static class CharacterFootMotionCoreColumns
    {
        internal static readonly CharacterFootCsvGroup<Source, CharacterFootMotionCoreSample> Schema =
            new CharacterFootCsvGroup<Source, CharacterFootMotionCoreSample>(
                "MotionCore", () => new CharacterFootMotionCoreSample(), new Column[]
                {
                    Column.Create("FootMotionState", Codecs.Text, Unit.Category,
                        (in Source source) => source.Core.State.ToString(), (target, value) => target.State = value),
                    Column.Create("FootMotionRejectReason", Codecs.Text, Unit.Category,
                        (in Source source) => source.Core.RejectReason.ToString(), (target, value) => target.RejectReason = value),
                    Column.Create("FootMotionLandingEventIdentity", Codecs.UInt64, Unit.Identity,
                        (in Source source) => source.Core.LandingEventIdentity, (target, value) => target.LandingEventIdentity = value),
                    Column.Create("FootMotionGroundPathInputIdentity", Codecs.UInt64, Unit.Identity,
                        (in Source source) => source.Core.GroundPathInputIdentity, (target, value) => target.GroundPathInputIdentity = value),
                    Column.Create("FootMotionDistance", Codecs.Float32, Unit.Metres,
                        (in Source source) => source.Core.Distance, (target, value) => target.Distance = value),
                    Column.Create("FootMotionProgress", Codecs.Float32, Unit.Unitless,
                        (in Source source) => source.Core.Progress, (target, value) => target.SwingProgress = value),
                    Column.Create("FootMotionOriginalSole", Codecs.Vector, Unit.Metres,
                        (in Source source) => source.Core.OriginalSole, (target, value) => target.OriginalSole = value),
                    Column.Create("FootMotionOriginalAnkle", Codecs.Vector, Unit.Metres,
                        (in Source source) => source.Core.OriginalAnkle, (target, value) => target.OriginalAnkle = value),
                    Column.Create("FootMotionSourceAnkleRotation", Codecs.Rotation, Unit.Unitless,
                        (in Source source) => source.Foot.SourceAnkleRotation, (target, value) => target.SourceAnkleRotation = value),
                    Column.Create("FootMotionSourceHeel", Codecs.Vector, Unit.Metres,
                        (in Source source) => source.Foot.SourceHeelPosition, (target, value) => target.SourceHeel = value),
                    Column.Create("FootMotionSourceToe", Codecs.Vector, Unit.Metres,
                        (in Source source) => source.Foot.SourceToePosition, (target, value) => target.SourceToe = value),
                    Column.Create("FootMotionBaselineSample", Codecs.Vector, Unit.Metres,
                        (in Source source) => source.Core.BaselineSample, (target, value) => target.SwingBaselineSample = value),
                    Column.Create("FootMotionBaselineSampleAlongUp", Codecs.Float32, Unit.Metres,
                        (in Source source) => source.BaselineSampleAlongUp, (target, value) => target.SwingBaselineSampleAlongUp = value),
                    Column.Create("FootMotionEnvelopeSample", Codecs.Vector, Unit.Metres,
                        (in Source source) => source.Core.EnvelopeSample, (target, value) => target.SwingEnvelopeSample = value),
                    Column.Create("FootMotionEnvelopeSampleAlongUp", Codecs.Float32, Unit.Metres,
                        (in Source source) => source.EnvelopeSampleAlongUp, (target, value) => target.SwingEnvelopeSampleAlongUp = value),
                    Column.Create("FootMotionFormalFootHeight", Codecs.Float32, Unit.Metres,
                        (in Source source) => source.FormalFootHeight, (target, value) => target.SwingFormalFootHeight = value),
                    Column.Create("FootMotionRawFormalTargetHeight", Codecs.Float32, Unit.Metres,
                        (in Source source) => source.RawFormalTargetHeight, (target, value) => target.SwingRawFormalTargetHeight = value),
                    Column.Create("FootMotionEnvelopeMinimumCorrection", Codecs.Float32, Unit.Metres,
                        (in Source source) => source.EnvelopeMinimumCorrection, (target, value) => target.SwingEnvelopeMinimumCorrection = value),
                    Column.Create("FootMotionBuilderSelectedCorrection", Codecs.Float32, Unit.Metres,
                        (in Source source) => source.BuilderSelectedCorrection, (target, value) => target.SwingBuilderSelectedCorrection = value),
                    Column.Create("FootMotionBuilderSwingTargetAvailable", Codecs.Boolean, Unit.None,
                        (in Source source) => source.BuilderSwingTargetAvailable, (target, value) => target.BuilderSwingTargetAvailable = value),
                    Column.Create("FootMotionBuilderSwingTargetCorrection", Codecs.Vector, Unit.Metres,
                        (in Source source) => source.BuilderSwingTargetCorrection, (target, value) => target.BuilderSwingTargetCorrection = value, "FootMotionBuilderSwingTargetAvailable"),
                    Column.Create("FootMotionSwingPathHorizontalAxisState", Codecs.Text, Unit.Category,
                        (in Source source) => source.HorizontalAxisState.ToString(), (target, value) => target.SwingPathHorizontalAxisState = value),
                    Column.Create("FootMotionActualFootHorizontalDistanceMeters", Codecs.Float32, Unit.Metres,
                        (in Source source) => source.ActualEnvelope.ActualFootHorizontalDistance, (target, value) => target.ActualFootHorizontalDistance = value),
                    Column.Create("FootMotionBaselineHorizontalDistanceMeters", Codecs.Float32, Unit.Metres,
                        (in Source source) => source.ActualEnvelope.BaselineHorizontalDistance, (target, value) => target.BaselineHorizontalDistance = value),
                    Column.Create("FootMotionEnvelopeHorizontalDistanceMeters", Codecs.Float32, Unit.Metres,
                        (in Source source) => source.ActualEnvelope.EnvelopeHorizontalDistance, (target, value) => target.EnvelopeHorizontalDistance = value),
                    Column.Create("FootMotionActualMinusEnvelopeHorizontalDistanceMeters", Codecs.Float32, Unit.Metres,
                        (in Source source) => source.ActualEnvelope.ActualFootHorizontalDistance - source.ActualEnvelope.EnvelopeHorizontalDistance, (target, value) => target.ActualMinusEnvelopeHorizontalDistance = value),
                    Column.Create("FootMotionActualFootAxisRegion", Codecs.Text, Unit.Category,
                        (in Source source) => source.ActualEnvelope.AxisRegion.ToString(), (target, value) => target.ActualFootAxisRegion = value),
                    Column.Create("FootMotionActualFootClosestPathParameter", Codecs.Float32, Unit.Unitless,
                        (in Source source) => source.ActualEnvelope.ClosestPathParameter, (target, value) => target.ActualFootClosestPathParameter = value),
                    Column.Create("FootMotionActualFootDistanceAlongAxisMeters", Codecs.Float32, Unit.Metres,
                        (in Source source) => source.ActualEnvelope.DistanceAlongAxis, (target, value) => target.ActualFootDistanceAlongAxis = value),
                    Column.Create("FootMotionActualFootCrossTrackDistanceMeters", Codecs.Float32, Unit.Metres,
                        (in Source source) => source.ActualEnvelope.CrossTrackDistance, (target, value) => target.ActualFootCrossTrackDistance = value),
                    Column.Create("FootMotionActualFootGroundPathCorridorRadiusMeters", Codecs.Float32, Unit.Metres,
                        (in Source source) => source.ActualEnvelope.CorridorRadius, (target, value) => target.ActualFootGroundPathCorridorRadius = value),
                    Column.Create("FootMotionActualFootWithinGroundPathCorridor", Codecs.Boolean, Unit.None,
                        (in Source source) => source.ActualEnvelope.WithinGroundPathCorridor, (target, value) => target.ActualFootWithinGroundPathCorridor = value),
                    Column.Create("FootMotionActualEnvelopeIntersectionState", Codecs.Text, Unit.Category,
                        (in Source source) => source.ActualEnvelope.State.ToString(), (target, value) => target.ActualEnvelopeIntersectionState = value),
                    Column.Create("FootMotionActualEnvelopeCandidateCount", Codecs.Int32, Unit.Count,
                        (in Source source) => source.ActualEnvelope.CandidateCount, (target, value) => target.ActualEnvelopeCandidateCount = value),
                    Column.Create("FootMotionActualEnvelopeMinimumHeightAlongUp", Codecs.Float32, Unit.Metres,
                        (in Source source) => source.ActualEnvelope.MinimumHeightAlongUp, (target, value) => target.ActualEnvelopeMinimumHeightAlongUp = value),
                    Column.Create("FootMotionActualEnvelopeMaximumHeightAlongUp", Codecs.Float32, Unit.Metres,
                        (in Source source) => source.ActualEnvelope.MaximumHeightAlongUp, (target, value) => target.ActualEnvelopeMaximumHeightAlongUp = value),
                    Column.Create("FootMotionActualEnvelopeHeightSpan", Codecs.Float32, Unit.Metres,
                        (in Source source) => source.ActualEnvelope.HeightSpan, (target, value) => target.ActualEnvelopeHeightSpan = value),
                    Column.Create("FootMotionActualEnvelopeHasVerticalEdge", Codecs.Boolean, Unit.None,
                        (in Source source) => source.ActualEnvelope.HasVerticalEdge, (target, value) => target.ActualEnvelopeHasVerticalEdge = value),
                    Column.Create("FootMotionActualEnvelopeHasMultipleHeights", Codecs.Boolean, Unit.None,
                        (in Source source) => source.ActualEnvelope.HasMultipleHeights, (target, value) => target.ActualEnvelopeHasMultipleHeights = value),
                    Column.Create("FootMotionActualEnvelopeAmbiguous", Codecs.Boolean, Unit.None,
                        (in Source source) => source.ActualEnvelope.Ambiguous, (target, value) => target.ActualEnvelopeAmbiguous = value),
                    Column.Create("FootMotionActualEnvelopeCounterfactualState", Codecs.Text, Unit.Category,
                        (in Source source) => source.ActualEnvelope.CounterfactualState.ToString(), (target, value) => target.ActualEnvelopeCounterfactualState = value),
                    Column.Create("FootMotionActualProgressEnvelopeCorrectionAvailable", Codecs.Boolean, Unit.None,
                        (in Source source) => source.ActualEnvelopeCorrectionAvailable, (target, value) => target.ActualProgressEnvelopeCorrectionAvailable = value),
                    Column.Create("FootMotionActualProgressEnvelopeMinimumCorrection", Codecs.Float32, Unit.Unitless,
                        (in Source source) => source.ActualEnvelopeMinimumCorrection, (target, value) => target.ActualProgressEnvelopeMinimumCorrection = value, "FootMotionActualProgressEnvelopeCorrectionAvailable"),
                    Column.Create("FootMotionActualProgressEnvelopeAdvanceAboveBuilderTarget", Codecs.Float32, Unit.Unitless,
                        (in Source source) => source.ActualEnvelopeAdvanceAboveBuilderTarget, (target, value) => target.ActualProgressEnvelopeAdvanceAboveBuilderTarget = value, "FootMotionActualProgressEnvelopeCorrectionAvailable"),
                    Column.Create("FootMotionLandingPredictionError", Codecs.Float32, Unit.Metres,
                        (in Source source) => source.Core.LandingPredictionError, (target, value) => target.LandingPredictionError = value),
                    Column.Create("FootMotionCorrectedSole", Codecs.Vector, Unit.Metres,
                        (in Source source) => source.Core.CorrectedSole, (target, value) => target.CorrectedSole = value),
                    Column.Create("FootMotionCorrectedAnkle", Codecs.Vector, Unit.Metres,
                        (in Source source) => source.Core.CorrectedAnkle, (target, value) => target.CorrectedAnkle = value),
                    Column.Create("FootMotionPositionWeight", Codecs.Float32, Unit.Unitless,
                        (in Source source) => source.Core.PositionWeight, (target, value) => target.MotionPositionWeight = value),
                    Column.Create("FootMotionRotationWeight", Codecs.Float32, Unit.Unitless,
                        (in Source source) => source.Core.RotationWeight, (target, value) => target.MotionRotationWeight = value),
                    Column.Create("FootMotionConstraintState", Codecs.Text, Unit.Category,
                        (in Source source) => source.Core.ConstraintState.ToString(), (target, value) => target.ConstraintState = value),
                    Column.Create("FootMotionLockResponse", Codecs.Text, Unit.Category,
                        (in Source source) => source.Core.LockResponse.ToString(), (target, value) => target.LockResponse = value),
                    Column.Create("FootMotionSupportHorizontalError", Codecs.Float32, Unit.Metres,
                        (in Source source) => source.Core.SupportHorizontalError, (target, value) => target.SupportHorizontalError = value),
                    Column.Create("FootMotionContactOwnership", Codecs.Float32, Unit.Unitless,
                        (in Source source) => source.Core.ContactOwnership, (target, value) => target.ContactOwnership = value),
                    Column.Create("FootMotionSupportWeight", Codecs.Float32, Unit.Unitless,
                        (in Source source) => source.Core.SupportWeight, (target, value) => target.SupportWeight = value),
                    Column.Create("FootMotionLandingReachEvaluated", Codecs.Boolean, Unit.None,
                        (in Source source) => source.Core.LandingReachEvaluated, (target, value) => target.LandingReachEvaluated = value),
                    Column.Create("FootMotionLandingReachAvailable", Codecs.Boolean, Unit.None,
                        (in Source source) => source.Core.LandingReachAvailable, (target, value) => target.LandingReachAvailable = value),
                    Column.Create("FootMotionSupportContactAnchor", Codecs.Vector, Unit.Metres,
                        (in Source source) => source.Core.SupportContactAnchor, (target, value) => target.Anchor = value),
                    Column.Create("FootMotionContactPlaneAvailable", Codecs.Boolean, Unit.None,
                        (in Source source) => source.Core.ContactPlaneAvailable, (target, value) => target.ContactPlaneAvailable = value),
                    Column.Create("FootMotionContactSurfaceIdentity", Codecs.Int32, Unit.Identity,
                        (in Source source) => source.Core.ContactSurfaceIdentity, (target, value) => target.ContactSurfaceIdentity = value, "FootMotionContactPlaneAvailable"),
                    Column.Create("FootMotionContactPlaneNormal", Codecs.Vector, Unit.Direction,
                        (in Source source) => source.Core.ContactPlaneNormal, (target, value) => target.ContactNormal = value, "FootMotionContactPlaneAvailable"),
                    Column.Create("FootContactPlanePenetrationAvailability", Codecs.Text, Unit.Category,
                        (in Source source) => source.PenetrationAvailability.ToString(), (target, value) => target.PenetrationAvailability = value),
                    Column.Create("FootMotionDesiredCorrection", Codecs.Vector, Unit.Metres,
                        (in Source source) => source.Core.DesiredCorrection, (target, value) => target.SwingDesiredCorrection = value),
                });
    }
}
