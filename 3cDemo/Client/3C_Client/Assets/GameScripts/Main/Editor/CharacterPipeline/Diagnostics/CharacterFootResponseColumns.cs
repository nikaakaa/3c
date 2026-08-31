using UnityEngine;
using ThirdPersonCharacter.Pipeline.Presentation;
using static ThirdPersonCharacter.Pipeline.Editor.CharacterFootCsvValues;
using Source = ThirdPersonCharacter.Pipeline.Presentation.CharacterFootSwingMotionDiagnostics;
using Column = ThirdPersonCharacter.Pipeline.Editor.CharacterFootCsvColumn<ThirdPersonCharacter.Pipeline.Presentation.CharacterFootSwingMotionDiagnostics, ThirdPersonCharacter.Pipeline.Editor.CharacterFootResponseSample>;
using Codecs = ThirdPersonCharacter.Pipeline.Editor.CharacterFootCsvCodecs;
using Unit = ThirdPersonCharacter.Pipeline.Editor.CharacterFootCsvUnit;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    internal sealed class CharacterFootResponseSample
    {
        internal string PlantTargetHeightAdoptionMode;
        internal float PlantTargetMaximumVerticalSpeed;
        internal float PlantTargetHeightBefore;
        internal float PlantTargetHeightTarget;
        internal float PlantTargetVerticalDelta;
        internal float PlantTargetAppliedVerticalDelta;
        internal float PlantTargetHeightAfter;
        internal ulong PlantTargetHeightEventIdentity;
        internal string PlantTargetHeightUpdateReason;
        internal bool PlantTargetForceRefreshed;
        internal float PlantTargetForceRefreshDistance;
        internal bool PlantTargetVerticalClamped;
        internal Vector3 PlantPreviousSelectedWorldTarget;
        internal Vector3 PlantSelectedWorldTarget;
        internal bool PreviousResponseOutputAvailable;
        internal Vector3 PreviousResponseOutputPoint;
        internal Vector3 DesiredOutputPoint;
        internal Vector3 ResponseOutputPoint;
        internal string PlantResidualCaptureReason;
        internal Vector3 PlantWorldResidualBeforeCapture;
        internal Vector3 PlantWorldResidualCapturedBeforeDecay;
        internal bool PlantWorldResidualDecayApplied;
        internal float PlantWorldResidualBaseHalfLifeSeconds;
        internal bool PlantWorldResidualDeadlineHalfLifeAvailable;
        internal float PlantWorldResidualDeadlineHalfLifeSeconds;
        internal float PlantWorldResidualAppliedHalfLifeSeconds;
        internal Vector3 PlantWorldResidualAfterDecay;
        internal float PlantWorldResidualCompletionTolerance;
        internal bool PlantWorldResidualClearedAtCompletionTolerance;
        internal string CorrectionResponseDomain;
        internal string CorrectionResponsePreviousDomain;
        internal bool CorrectionResponseDomainTransferred;
        internal bool CorrectionResponseEvaluated;
        internal bool CorrectionResponseInitializedBefore;
        internal bool CorrectionResponseInitializedThisFrame;
        internal string CorrectionResponseInitializationReason;
        internal float CorrectionResponseDesired;
        internal Vector3 CorrectionResponseRequestedDirection;
        internal Vector3 CorrectionResponsePreviousDirection;
        internal bool CorrectionResponseDirectionLimited;
        internal float CorrectionResponseMaximumDirectionChangeDegrees;
        internal float CorrectionResponseAppliedDirectionChangeDegrees;
        internal bool CorrectionResponseVisibleOutputTransferred;
        internal float CorrectionResponseBeforeRebase;
        internal float CorrectionResponsePrevious;
        internal float CorrectionResponseCurrent;
        internal Vector3 CorrectionResponseDirection;
        internal string CorrectionResponseDeltaDirection;
        internal float CorrectionResponseSelectedSpeed;
        internal float CorrectionResponseAppliedDelta;
        internal string PlantVerticalContinuityOwners;
        internal Vector3 PlantEffectiveCorrectionBefore;
        internal Vector3 PlantEffectiveCorrectionAfter;
        internal float PlantOutputDistance;
        internal float PlantPenetrationDepth;
    }

    internal static class CharacterFootResponseColumns
    {
        internal static readonly CharacterFootCsvGroup<Source, CharacterFootResponseSample> Schema =
            new CharacterFootCsvGroup<Source, CharacterFootResponseSample>(
                "ResponseContact", () => new CharacterFootResponseSample(), new Column[]
                {
                    Column.Create("FootMotionPlantTargetHeightAdoptionMode", Codecs.Text, Unit.Category,
                        (in Source source) => source.PlantTargetHeightAdoptionMode, (target, value) => target.PlantTargetHeightAdoptionMode = value),
                    Column.Create("FootMotionPlantTargetMaximumVerticalSpeed", Codecs.Float32, Unit.MetresPerSecond,
                        (in Source source) => source.PlantTargetMaximumVerticalSpeed, (target, value) => target.PlantTargetMaximumVerticalSpeed = value),
                    Column.Create("FootMotionPlantTargetHeightBefore", Codecs.Float32, Unit.Metres,
                        (in Source source) => source.PlantTargetHeightBefore, (target, value) => target.PlantTargetHeightBefore = value),
                    Column.Create("FootMotionPlantTargetHeightTarget", Codecs.Float32, Unit.Metres,
                        (in Source source) => source.PlantTargetHeightTarget, (target, value) => target.PlantTargetHeightTarget = value),
                    Column.Create("FootMotionPlantTargetVerticalDelta", Codecs.Float32, Unit.Metres,
                        (in Source source) => source.PlantTargetVerticalDelta, (target, value) => target.PlantTargetVerticalDelta = value),
                    Column.Create("FootMotionPlantTargetAppliedVerticalDelta", Codecs.Float32, Unit.Metres,
                        (in Source source) => source.PlantTargetAppliedVerticalDelta, (target, value) => target.PlantTargetAppliedVerticalDelta = value),
                    Column.Create("FootMotionPlantTargetHeightAfter", Codecs.Float32, Unit.Metres,
                        (in Source source) => source.PlantTargetHeightAfter, (target, value) => target.PlantTargetHeightAfter = value),
                    Column.Create("FootMotionPlantTargetHeightEventIdentity", Codecs.UInt64, Unit.Identity,
                        (in Source source) => source.PlantTargetHeightEventIdentity, (target, value) => target.PlantTargetHeightEventIdentity = value),
                    Column.Create("FootMotionPlantTargetHeightUpdateReason", Codecs.Text, Unit.Category,
                        (in Source source) => source.PlantTargetHeightUpdateReason, (target, value) => target.PlantTargetHeightUpdateReason = value),
                    Column.Create("FootMotionPlantTargetForceRefreshed", Codecs.Boolean, Unit.None,
                        (in Source source) => source.PlantTargetForceRefreshed, (target, value) => target.PlantTargetForceRefreshed = value),
                    Column.Create("FootMotionPlantTargetForceRefreshDistance", Codecs.Float32, Unit.Metres,
                        (in Source source) => source.PlantTargetForceRefreshDistance, (target, value) => target.PlantTargetForceRefreshDistance = value),
                    Column.Create("FootMotionPlantTargetVerticalClamped", Codecs.Boolean, Unit.None,
                        (in Source source) => source.PlantTargetVerticalClamped, (target, value) => target.PlantTargetVerticalClamped = value),
                    Column.Create("FootMotionPlantPreviousSelectedWorldTarget", Codecs.Vector, Unit.Metres,
                        (in Source source) => source.PlantPreviousSelectedWorldTarget, (target, value) => target.PlantPreviousSelectedWorldTarget = value),
                    Column.Create("FootMotionPlantSelectedWorldTarget", Codecs.Vector, Unit.Metres,
                        (in Source source) => source.PlantSelectedWorldTarget, (target, value) => target.PlantSelectedWorldTarget = value),
                    Column.Create("FootMotionPreviousResponseOutputAvailable", Codecs.Boolean, Unit.None,
                        (in Source source) => source.PreviousResponseOutputAvailable, (target, value) => target.PreviousResponseOutputAvailable = value),
                    Column.Create("FootMotionPreviousResponseOutputPoint", Codecs.Vector, Unit.Metres,
                        (in Source source) => source.PreviousResponseOutputPoint, (target, value) => target.PreviousResponseOutputPoint = value, "FootMotionPreviousResponseOutputAvailable"),
                    Column.Create("FootMotionDesiredOutputPoint", Codecs.Vector, Unit.Metres,
                        (in Source source) => source.DesiredOutputPoint, (target, value) => target.DesiredOutputPoint = value),
                    Column.Create("FootMotionResponseOutputPoint", Codecs.Vector, Unit.Metres,
                        (in Source source) => source.ResponseOutputPoint, (target, value) => target.ResponseOutputPoint = value),
                    Column.Create("FootMotionPlantResidualCaptureReason", Codecs.Text, Unit.Category,
                        (in Source source) => source.PlantResidualCaptureReason, (target, value) => target.PlantResidualCaptureReason = value),
                    Column.Create("FootMotionPlantWorldResidualBeforeCapture", Codecs.Vector, Unit.Metres,
                        (in Source source) => source.PlantWorldResidualBeforeCapture, (target, value) => target.PlantWorldResidualBeforeCapture = value),
                    Column.Create("FootMotionPlantWorldResidualCapturedBeforeDecay", Codecs.Vector, Unit.Metres,
                        (in Source source) => source.PlantWorldResidualCapturedBeforeDecay, (target, value) => target.PlantWorldResidualCapturedBeforeDecay = value),
                    Column.Create("FootMotionPlantWorldResidualDecayApplied", Codecs.Boolean, Unit.None,
                        (in Source source) => source.PlantWorldResidualDecayApplied, (target, value) => target.PlantWorldResidualDecayApplied = value),
                    Column.Create("FootMotionPlantWorldResidualBaseHalfLifeSeconds", Codecs.Float32, Unit.Seconds,
                        (in Source source) => source.PlantWorldResidualBaseHalfLifeSeconds, (target, value) => target.PlantWorldResidualBaseHalfLifeSeconds = value),
                    Column.Create("FootMotionPlantWorldResidualDeadlineHalfLifeAvailable", Codecs.Boolean, Unit.None,
                        (in Source source) => source.PlantWorldResidualDeadlineHalfLifeAvailable, (target, value) => target.PlantWorldResidualDeadlineHalfLifeAvailable = value),
                    Column.Create("FootMotionPlantWorldResidualDeadlineHalfLifeSeconds", Codecs.Float32, Unit.Seconds,
                        (in Source source) => source.PlantWorldResidualDeadlineHalfLifeSeconds, (target, value) => target.PlantWorldResidualDeadlineHalfLifeSeconds = value, "FootMotionPlantWorldResidualDeadlineHalfLifeAvailable"),
                    Column.Create("FootMotionPlantWorldResidualAppliedHalfLifeSeconds", Codecs.Float32, Unit.Seconds,
                        (in Source source) => source.PlantWorldResidualAppliedHalfLifeSeconds, (target, value) => target.PlantWorldResidualAppliedHalfLifeSeconds = value),
                    Column.Create("FootMotionPlantWorldResidualAfterDecay", Codecs.Vector, Unit.Metres,
                        (in Source source) => source.PlantWorldResidualAfterDecay, (target, value) => target.PlantWorldResidualAfterDecay = value),
                    Column.Create("FootMotionPlantWorldResidualCompletionTolerance", Codecs.Float32, Unit.Metres,
                        (in Source source) => source.PlantWorldResidualCompletionTolerance, (target, value) => target.PlantWorldResidualCompletionTolerance = value),
                    Column.Create("FootMotionPlantWorldResidualClearedAtCompletionTolerance", Codecs.Boolean, Unit.None,
                        (in Source source) => source.PlantWorldResidualClearedAtCompletionTolerance, (target, value) => target.PlantWorldResidualClearedAtCompletionTolerance = value),
                    Column.Create("FootMotionCorrectionResponseDomain", Codecs.Text, Unit.Category,
                        (in Source source) => source.CorrectionResponseDomain, (target, value) => target.CorrectionResponseDomain = value),
                    Column.Create("FootMotionCorrectionResponsePreviousDomain", Codecs.Text, Unit.Category,
                        (in Source source) => source.CorrectionResponsePreviousDomain, (target, value) => target.CorrectionResponsePreviousDomain = value),
                    Column.Create("FootMotionCorrectionResponseDomainTransferred", Codecs.Boolean, Unit.None,
                        (in Source source) => source.CorrectionResponseDomainTransferred, (target, value) => target.CorrectionResponseDomainTransferred = value),
                    Column.Create("FootMotionCorrectionResponseEvaluated", Codecs.Boolean, Unit.None,
                        (in Source source) => source.CorrectionResponseEvaluated, (target, value) => target.CorrectionResponseEvaluated = value),
                    Column.Create("FootMotionCorrectionResponseInitializedBefore", Codecs.Boolean, Unit.None,
                        (in Source source) => source.CorrectionResponseInitializedBefore, (target, value) => target.CorrectionResponseInitializedBefore = value),
                    Column.Create("FootMotionCorrectionResponseInitializedThisFrame", Codecs.Boolean, Unit.None,
                        (in Source source) => source.CorrectionResponseInitializedThisFrame, (target, value) => target.CorrectionResponseInitializedThisFrame = value),
                    Column.Create("FootMotionCorrectionResponseInitializationReason", Codecs.Text, Unit.Category,
                        (in Source source) => source.CorrectionResponseInitializationReason, (target, value) => target.CorrectionResponseInitializationReason = value),
                    Column.Create("FootMotionCorrectionResponseDesired", Codecs.Float32, Unit.Metres,
                        (in Source source) => source.CorrectionResponseDesired, (target, value) => target.CorrectionResponseDesired = value),
                    Column.Create("FootMotionCorrectionResponseRequestedDirection", Codecs.Vector, Unit.Direction,
                        (in Source source) => source.CorrectionResponseRequestedDirection, (target, value) => target.CorrectionResponseRequestedDirection = value),
                    Column.Create("FootMotionCorrectionResponsePreviousDirection", Codecs.Vector, Unit.Direction,
                        (in Source source) => source.CorrectionResponsePreviousDirection, (target, value) => target.CorrectionResponsePreviousDirection = value),
                    Column.Create("FootMotionCorrectionResponseDirectionLimited", Codecs.Boolean, Unit.None,
                        (in Source source) => source.CorrectionResponseDirectionLimited, (target, value) => target.CorrectionResponseDirectionLimited = value),
                    Column.Create("FootMotionCorrectionResponseMaximumDirectionChangeDegrees", Codecs.Float32, Unit.Degrees,
                        (in Source source) => source.CorrectionResponseMaximumDirectionChangeDegrees, (target, value) => target.CorrectionResponseMaximumDirectionChangeDegrees = value),
                    Column.Create("FootMotionCorrectionResponseAppliedDirectionChangeDegrees", Codecs.Float32, Unit.Degrees,
                        (in Source source) => source.CorrectionResponseAppliedDirectionChangeDegrees, (target, value) => target.CorrectionResponseAppliedDirectionChangeDegrees = value),
                    Column.Create("FootMotionCorrectionResponseVisibleOutputTransferred", Codecs.Boolean, Unit.None,
                        (in Source source) => source.CorrectionResponseVisibleOutputTransferred, (target, value) => target.CorrectionResponseVisibleOutputTransferred = value),
                    Column.Create("FootMotionCorrectionResponseBeforeRebase", Codecs.Float32, Unit.Metres,
                        (in Source source) => source.CorrectionResponseBeforeRebase, (target, value) => target.CorrectionResponseBeforeRebase = value),
                    Column.Create("FootMotionCorrectionResponsePrevious", Codecs.Float32, Unit.Metres,
                        (in Source source) => source.CorrectionResponsePrevious, (target, value) => target.CorrectionResponsePrevious = value),
                    Column.Create("FootMotionCorrectionResponseCurrent", Codecs.Float32, Unit.Metres,
                        (in Source source) => source.CorrectionResponseCurrent, (target, value) => target.CorrectionResponseCurrent = value),
                    Column.Create("FootMotionCorrectionResponseDirection", Codecs.Vector, Unit.Direction,
                        (in Source source) => source.CorrectionResponseDirection, (target, value) => target.CorrectionResponseDirection = value),
                    Column.Create("FootMotionCorrectionResponseDeltaDirection", Codecs.Text, Unit.Category,
                        (in Source source) => source.CorrectionResponseDeltaDirection, (target, value) => target.CorrectionResponseDeltaDirection = value),
                    Column.Create("FootMotionCorrectionResponseSelectedSpeed", Codecs.Float32, Unit.MetresPerSecond,
                        (in Source source) => source.CorrectionResponseSelectedSpeed, (target, value) => target.CorrectionResponseSelectedSpeed = value),
                    Column.Create("FootMotionCorrectionResponseAppliedDelta", Codecs.Float32, Unit.Metres,
                        (in Source source) => source.CorrectionResponseAppliedDelta, (target, value) => target.CorrectionResponseAppliedDelta = value),
                    Column.Create("FootMotionPlantVerticalContinuityOwners", Codecs.Text, Unit.Category,
                        (in Source source) => source.PlantVerticalContinuityOwners, (target, value) => target.PlantVerticalContinuityOwners = value),
                    Column.Create("FootMotionPlantEffectiveCorrectionBefore", Codecs.Vector, Unit.Metres,
                        (in Source source) => source.PlantEffectiveCorrectionBefore, (target, value) => target.PlantEffectiveCorrectionBefore = value),
                    Column.Create("FootMotionPlantEffectiveCorrectionAfter", Codecs.Vector, Unit.Metres,
                        (in Source source) => source.PlantEffectiveCorrectionAfter, (target, value) => target.PlantEffectiveCorrectionAfter = value),
                    Column.Create("FootMotionPlantOutputDistance", Codecs.Float32, Unit.Metres,
                        (in Source source) => source.PlantOutputDistance, (target, value) => target.PlantOutputDistance = value),
                    Column.Create("FootMotionPlantPenetrationDepth", Codecs.Float32, Unit.Metres,
                        (in Source source) => source.PlantPenetrationDepth, (target, value) => target.PlantPenetrationDepth = value),
                });
    }
}
