using UnityEngine;
using Source = ThirdPersonCharacter.Pipeline.Presentation.CharacterFootSwingMotionDiagnostics;
using Column = ThirdPersonCharacter.Pipeline.Editor.CharacterFootCsvColumn<ThirdPersonCharacter.Pipeline.Presentation.CharacterFootSwingMotionDiagnostics, ThirdPersonCharacter.Pipeline.Editor.CharacterFootOutputStagesSample>;
using Codecs = ThirdPersonCharacter.Pipeline.Editor.CharacterFootCsvCodecs;
using Unit = ThirdPersonCharacter.Pipeline.Editor.CharacterFootCsvUnit;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    internal sealed class CharacterFootOutputStagesSample
    {
        internal Vector3 StateTargetCorrection;
        internal string InterpolationPolicy;
        internal Vector3 InterpolationOutputCorrection;
        internal bool InterpolationCompleted;
        internal string ConstraintStateBefore;
        internal string LockResponseBefore;
        internal bool OutputStagesAvailable;
        internal bool ReleasingCompletedToSwing;
        internal bool SafetyFloorAvailable;
        internal string SafetyFloorOwner;
        internal int SafetyFloorOwnerSurfaceIdentity;
        internal ulong SafetyFloorOwnerPathIdentity;
        internal Vector3 CorrectionBeforeSafetyFloor;
        internal Vector3 SafetyFloorMinimumCorrection;
        internal Vector3 SafetyFloorOutputCorrection;
        internal Vector3 FinalEffectiveCorrection;
        internal bool SafetyFloorClamped;
        internal float SafetyFloorClampMeters;
        internal float SafetyFloorClearanceBeforeMeters;
        internal float SafetyFloorClearanceAfterMeters;
        internal bool PlantInterpolationEvaluated;
        internal ulong PlantTargetEventIdentity;
        internal bool PlantTargetVerified;
        internal string PlantTargetKind;
        internal string PlantLockResponse;
        internal bool PlantLockWeightCompleted;
        internal Vector3 PlantDesiredPoint;
        internal Vector3 PlantFilteredPoint;
    }

    internal static class CharacterFootOutputStagesColumns
    {
        internal static readonly CharacterFootCsvGroup<Source, CharacterFootOutputStagesSample> Schema =
            new CharacterFootCsvGroup<Source, CharacterFootOutputStagesSample>(
                "OutputStages", () => new CharacterFootOutputStagesSample(), new Column[]
                {
                    Column.Create("FootMotionStateTargetCorrection", Codecs.Vector, Unit.Metres,
                        (in Source source) => source.StateTargetCorrection, (target, value) => target.StateTargetCorrection = value),
                    Column.Create("FootMotionInterpolationPolicy", Codecs.Text, Unit.Category,
                        (in Source source) => source.InterpolationPolicy, (target, value) => target.InterpolationPolicy = value),
                    Column.Create("FootMotionInterpolationOutputCorrection", Codecs.Vector, Unit.Metres,
                        (in Source source) => source.InterpolationOutputCorrection, (target, value) => target.InterpolationOutputCorrection = value),
                    Column.Create("FootMotionInterpolationCompleted", Codecs.Boolean, Unit.None,
                        (in Source source) => source.InterpolationCompleted, (target, value) => target.InterpolationCompleted = value),
                    Column.Create("FootMotionConstraintStateBefore", Codecs.Text, Unit.Category,
                        (in Source source) => source.ConstraintStateBefore.ToString(), (target, value) => target.ConstraintStateBefore = value),
                    Column.Create("FootMotionLockResponseBefore", Codecs.Text, Unit.Category,
                        (in Source source) => source.LockResponseBefore.ToString(), (target, value) => target.LockResponseBefore = value),
                    Column.Create("FootMotionOutputStagesAvailable", Codecs.Boolean, Unit.None,
                        (in Source source) => source.OutputStagesAvailable, (target, value) => target.OutputStagesAvailable = value),
                    Column.Create("FootMotionReleasingCompletedToSwing", Codecs.Boolean, Unit.None,
                        (in Source source) => source.ReleasingCompletedToSwing, (target, value) => target.ReleasingCompletedToSwing = value),
                    Column.Create("FootMotionSafetyFloorAvailable", Codecs.Boolean, Unit.None,
                        (in Source source) => source.SafetyFloorAvailable, (target, value) => target.SafetyFloorAvailable = value),
                    Column.Create("FootMotionSafetyFloorOwner", Codecs.Text, Unit.Category,
                        (in Source source) => source.SafetyFloorOwner.ToString(), (target, value) => target.SafetyFloorOwner = value),
                    Column.Create("FootMotionSafetyFloorOwnerSurfaceIdentity", Codecs.Int32, Unit.Identity,
                        (in Source source) => source.SafetyFloorOwnerSurfaceIdentity, (target, value) => target.SafetyFloorOwnerSurfaceIdentity = value),
                    Column.Create("FootMotionSafetyFloorOwnerPathIdentity", Codecs.UInt64, Unit.Identity,
                        (in Source source) => source.SafetyFloorOwnerPathIdentity, (target, value) => target.SafetyFloorOwnerPathIdentity = value),
                    Column.Create("FootMotionCorrectionBeforeSafetyFloor", Codecs.Vector, Unit.Metres,
                        (in Source source) => source.CorrectionBeforeSafetyFloor, (target, value) => target.CorrectionBeforeSafetyFloor = value),
                    Column.Create("FootMotionSafetyFloorMinimumCorrection", Codecs.Vector, Unit.Metres,
                        (in Source source) => source.SafetyFloorMinimumCorrection, (target, value) => target.SafetyFloorMinimumCorrection = value, "FootMotionSafetyFloorAvailable"),
                    Column.Create("FootMotionSafetyFloorOutputCorrection", Codecs.Vector, Unit.Metres,
                        (in Source source) => source.SafetyFloorOutputCorrection, (target, value) => target.SafetyFloorOutputCorrection = value, "FootMotionSafetyFloorAvailable"),
                    Column.Create("FootMotionFinalEffectiveCorrection", Codecs.Vector, Unit.Metres,
                        (in Source source) => source.FinalEffectiveCorrection, (target, value) => target.FinalEffectiveCorrection = value),
                    Column.Create("FootMotionSafetyFloorClamped", Codecs.Boolean, Unit.None,
                        (in Source source) => source.SafetyFloorClamped, (target, value) => target.SafetyFloorClamped = value),
                    Column.Create("FootMotionSafetyFloorClampMeters", Codecs.Float32, Unit.Metres,
                        (in Source source) => source.SafetyFloorClampMeters, (target, value) => target.SafetyFloorClampMeters = value),
                    Column.Create("FootMotionSafetyFloorClearanceBeforeMeters", Codecs.Float32, Unit.Metres,
                        (in Source source) => source.SafetyFloorClearanceBeforeMeters, (target, value) => target.SafetyFloorClearanceBeforeMeters = value),
                    Column.Create("FootMotionSafetyFloorClearanceAfterMeters", Codecs.Float32, Unit.Metres,
                        (in Source source) => source.SafetyFloorClearanceAfterMeters, (target, value) => target.SafetyFloorClearanceAfterMeters = value),
                    Column.Create("FootMotionPlantInterpolationEvaluated", Codecs.Boolean, Unit.None,
                        (in Source source) => source.PlantInterpolationEvaluated, (target, value) => target.PlantInterpolationEvaluated = value),
                    Column.Create("FootMotionPlantTargetEventIdentity", Codecs.UInt64, Unit.Identity,
                        (in Source source) => source.PlantTargetEventIdentity, (target, value) => target.PlantTargetEventIdentity = value),
                    Column.Create("FootMotionPlantTargetVerified", Codecs.Boolean, Unit.None,
                        (in Source source) => source.PlantTargetVerified, (target, value) => target.PlantTargetVerified = value),
                    Column.Create("FootMotionPlantTargetKind", Codecs.Text, Unit.Category,
                        (in Source source) => source.PlantTargetKind, (target, value) => target.PlantTargetKind = value),
                    Column.Create("FootMotionPlantLockResponse", Codecs.Text, Unit.Category,
                        (in Source source) => source.PlantLockResponse.ToString(), (target, value) => target.PlantLockResponse = value),
                    Column.Create("FootMotionPlantLockWeightCompleted", Codecs.Boolean, Unit.None,
                        (in Source source) => source.PlantLockWeightCompleted, (target, value) => target.PlantLockWeightCompleted = value),
                    Column.Create("FootMotionPlantDesiredPoint", Codecs.Vector, Unit.Metres,
                        (in Source source) => source.PlantDesiredPoint, (target, value) => target.PlantDesiredPoint = value),
                    Column.Create("FootMotionPlantFilteredPoint", Codecs.Vector, Unit.Metres,
                        (in Source source) => source.PlantFilteredPoint, (target, value) => target.PlantFilteredPoint = value),
                });
    }
}
