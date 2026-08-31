using UnityEngine;
using Capture = ThirdPersonCharacter.Pipeline.Editor.CharacterFootLandingPredictionSampler.FootIkCapture;
using Source = ThirdPersonCharacter.Pipeline.Editor.CharacterFootSolverCsvSource;
using Column = ThirdPersonCharacter.Pipeline.Editor.CharacterFootCsvColumn<ThirdPersonCharacter.Pipeline.Editor.CharacterFootSolverCsvSource, ThirdPersonCharacter.Pipeline.Editor.CharacterFootSolverSample>;
using Codecs = ThirdPersonCharacter.Pipeline.Editor.CharacterFootCsvCodecs;
using Unit = ThirdPersonCharacter.Pipeline.Editor.CharacterFootCsvUnit;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    internal readonly struct CharacterFootSolverCsvSource
    {
        internal CharacterFootSolverCsvSource(
            in Capture capture, Vector3 worldPosition, Quaternion worldRotation,
            Vector3 heel, Vector3 toe, float goalResidual)
        {
            Capture = capture;
            WorldPosition = worldPosition;
            WorldRotation = worldRotation;
            Heel = heel;
            Toe = toe;
            GoalResidual = goalResidual;
        }

        internal Capture Capture { get; }
        internal Vector3 WorldPosition { get; }
        internal Quaternion WorldRotation { get; }
        internal Vector3 Heel { get; }
        internal Vector3 Toe { get; }
        internal float GoalResidual { get; }
    }

    internal sealed class CharacterFootSolverSample
    {
        internal bool IkSolverAvailable;
        internal bool IkSucceeded;
        internal ulong IkFrameSequence;
        internal ulong IkInputCompletionIdentity;
        internal ulong IkOutputCompletionIdentity;
        internal string IkBackendIdentity;
        internal string IkRigId;
        internal string IkRigRevision;
        internal string IkProfileId;
        internal string IkProfileRevision;
        internal string IkFailure;
        internal int IkAppliedGoalCount;
        internal bool IkEffectorAvailable;
        internal string IkEffectorSlot;
        internal Vector3 IkTargetPosition;
        internal Vector3 IkSolvedPosition;
        internal float IkPositionResidual;
        internal float IkRotationResidualDegrees;
        internal bool IkLegAvailable;
        internal string IkLegSlot;
        internal float IkLegBendWeight;
        internal float IkLegStabilizationWeight;
        internal bool IkLegRetainedPreviousBendDirection;
        internal Vector3 IkLegOriginalHip;
        internal Vector3 IkLegOriginalKnee;
        internal Vector3 IkLegOriginalAnkle;
        internal Vector3 IkLegTargetAnkle;
        internal Vector3 IkLegSolvedHip;
        internal Vector3 IkLegSolvedKnee;
        internal Vector3 IkLegSolvedAnkle;
        internal float IkLegOriginalBendDegrees;
        internal float IkLegSolvedBendDegrees;
        internal float IkLegOriginalExtensionRatio;
        internal float IkLegTargetExtensionRatio;
        internal float IkLegSolvedExtensionRatio;
        internal float IkLegOriginalCompressionReserve;
        internal float IkLegTargetCompressionReserve;
        internal float IkLegSolvedCompressionReserve;
        internal Vector3 IkLegEffectiveBendDirection;
        internal float IkLegAnimatedBendDirectionPreviousDot;
        internal float IkLegEffectiveBendDirectionPreviousDot;
        internal bool IkPelvisAvailable;
        internal Vector3 IkPelvisTargetPosition;
        internal Vector3 IkPelvisSolvedPosition;
        internal float IkPelvisPositionResidual;
        internal float IkPelvisRotationResidualDegrees;
        internal bool PhysicalWriteAvailable;
        internal ulong PhysicalWriteCompletionIdentity;
        internal Vector3 PhysicalAnkleComponentPosition;
        internal Quaternion PhysicalAnkleComponentRotation;
        internal Vector3 PhysicalAnkleWorldPosition;
        internal Quaternion PhysicalAnkleWorldRotation;
        internal Vector3 PhysicalHeelWorld;
        internal Vector3 PhysicalToeWorld;
        internal float PhysicalAnkleGoalResidual;
    }

    internal static class CharacterFootSolverColumns
    {
        internal static readonly CharacterFootCsvGroup<Source, CharacterFootSolverSample> Schema =
            new CharacterFootCsvGroup<Source, CharacterFootSolverSample>(
                "SolverPhysical", () => new CharacterFootSolverSample(), new Column[]
                {
                    Column.Create("FinalIkSolverAvailable", Codecs.Boolean, Unit.None,
                        (in Source source) => source.Capture.SolverAvailable, (target, value) => target.IkSolverAvailable = value),
                    Column.Create("FinalIkSucceeded", Codecs.Boolean, Unit.None,
                        (in Source source) => source.Capture.Solver.Succeeded, (target, value) => target.IkSucceeded = value),
                    Column.Create("FinalIkFrameSequence", Codecs.UInt64, Unit.Frame,
                        (in Source source) => source.Capture.Solver.FrameSequence, (target, value) => target.IkFrameSequence = value),
                    Column.Create("FinalIkInputCompletionIdentity", Codecs.UInt64, Unit.Identity,
                        (in Source source) => source.Capture.Solver.InputCompletionIdentity, (target, value) => target.IkInputCompletionIdentity = value),
                    Column.Create("FinalIkOutputCompletionIdentity", Codecs.UInt64, Unit.Identity,
                        (in Source source) => source.Capture.Solver.OutputCompletionIdentity, (target, value) => target.IkOutputCompletionIdentity = value),
                    Column.Create("FinalIkBackendIdentity", Codecs.Text, Unit.Identity,
                        (in Source source) => source.Capture.Solver.BackendIdentity, (target, value) => target.IkBackendIdentity = value),
                    Column.Create("FinalIkRigId", Codecs.Text, Unit.Identity,
                        (in Source source) => source.Capture.Solver.RigId, (target, value) => target.IkRigId = value),
                    Column.Create("FinalIkRigRevision", Codecs.Text, Unit.Identity,
                        (in Source source) => source.Capture.Solver.RigRevision, (target, value) => target.IkRigRevision = value),
                    Column.Create("FinalIkProfileId", Codecs.Text, Unit.Identity,
                        (in Source source) => source.Capture.Solver.ProfileId, (target, value) => target.IkProfileId = value),
                    Column.Create("FinalIkProfileRevision", Codecs.Text, Unit.Identity,
                        (in Source source) => source.Capture.Solver.ProfileRevision, (target, value) => target.IkProfileRevision = value),
                    Column.Create("FinalIkFailure", Codecs.Text, Unit.Category,
                        (in Source source) => source.Capture.Solver.Failure.ToString(), (target, value) => target.IkFailure = value),
                    Column.Create("FinalIkAppliedGoalCount", Codecs.Int32, Unit.Count,
                        (in Source source) => source.Capture.Solver.AppliedGoalCount, (target, value) => target.IkAppliedGoalCount = value),
                    Column.Create("FinalIkEffectorAvailable", Codecs.Boolean, Unit.None,
                        (in Source source) => source.Capture.EffectorAvailable, (target, value) => target.IkEffectorAvailable = value),
                    Column.Create("FinalIkEffectorSlot", Codecs.Text, Unit.Category,
                        (in Source source) => source.Capture.Effector.Slot.ToString(), (target, value) => target.IkEffectorSlot = value),
                    Column.Create("FinalIkTargetPosition", Codecs.Vector, Unit.Metres,
                        (in Source source) => source.Capture.Effector.TargetComponentPosition, (target, value) => target.IkTargetPosition = value),
                    Column.Create("FinalIkSolvedPosition", Codecs.Vector, Unit.Metres,
                        (in Source source) => source.Capture.Effector.SolvedComponentPosition, (target, value) => target.IkSolvedPosition = value),
                    Column.Create("FinalIkPositionResidual", Codecs.Float32, Unit.Metres,
                        (in Source source) => source.Capture.Effector.PositionResidual, (target, value) => target.IkPositionResidual = value),
                    Column.Create("FinalIkRotationResidualDegrees", Codecs.Float32, Unit.Degrees,
                        (in Source source) => source.Capture.Effector.RotationResidualDegrees, (target, value) => target.IkRotationResidualDegrees = value),
                    Column.Create("FinalIkLegAvailable", Codecs.Boolean, Unit.None,
                        (in Source source) => source.Capture.Limb.LegPose.IsAvailable, (target, value) => target.IkLegAvailable = value),
                    Column.Create("FinalIkLegSlot", Codecs.Text, Unit.Category,
                        (in Source source) => source.Capture.Limb.Limb.ToString(), (target, value) => target.IkLegSlot = value),
                    Column.Create("FinalIkLegBendWeight", Codecs.Float32, Unit.Unitless,
                        (in Source source) => source.Capture.Limb.BendWeight, (target, value) => target.IkLegBendWeight = value),
                    Column.Create("FinalIkLegStabilizationWeight", Codecs.Float32, Unit.Unitless,
                        (in Source source) => source.Capture.Limb.LegPose.StabilizationWeight, (target, value) => target.IkLegStabilizationWeight = value),
                    Column.Create("FinalIkLegRetainedPreviousBendDirection", Codecs.Boolean, Unit.None,
                        (in Source source) => source.Capture.Limb.LegPose.RetainedPreviousBendDirection, (target, value) => target.IkLegRetainedPreviousBendDirection = value),
                    Column.Create("FinalIkLegOriginalHip", Codecs.Vector, Unit.Metres,
                        (in Source source) => source.Capture.Limb.LegPose.OriginalHip, (target, value) => target.IkLegOriginalHip = value),
                    Column.Create("FinalIkLegOriginalKnee", Codecs.Vector, Unit.Metres,
                        (in Source source) => source.Capture.Limb.LegPose.OriginalKnee, (target, value) => target.IkLegOriginalKnee = value),
                    Column.Create("FinalIkLegOriginalAnkle", Codecs.Vector, Unit.Metres,
                        (in Source source) => source.Capture.Limb.LegPose.OriginalAnkle, (target, value) => target.IkLegOriginalAnkle = value),
                    Column.Create("FinalIkLegTargetAnkle", Codecs.Vector, Unit.Metres,
                        (in Source source) => source.Capture.Limb.LegPose.TargetAnkle, (target, value) => target.IkLegTargetAnkle = value),
                    Column.Create("FinalIkLegSolvedHip", Codecs.Vector, Unit.Metres,
                        (in Source source) => source.Capture.Limb.LegPose.SolvedHip, (target, value) => target.IkLegSolvedHip = value),
                    Column.Create("FinalIkLegSolvedKnee", Codecs.Vector, Unit.Metres,
                        (in Source source) => source.Capture.Limb.LegPose.SolvedKnee, (target, value) => target.IkLegSolvedKnee = value),
                    Column.Create("FinalIkLegSolvedAnkle", Codecs.Vector, Unit.Metres,
                        (in Source source) => source.Capture.Limb.LegPose.SolvedAnkle, (target, value) => target.IkLegSolvedAnkle = value),
                    Column.Create("FinalIkLegOriginalBendDegrees", Codecs.Float32, Unit.Degrees,
                        (in Source source) => source.Capture.Limb.LegPose.OriginalBendDegrees, (target, value) => target.IkLegOriginalBendDegrees = value),
                    Column.Create("FinalIkLegSolvedBendDegrees", Codecs.Float32, Unit.Degrees,
                        (in Source source) => source.Capture.Limb.LegPose.SolvedBendDegrees, (target, value) => target.IkLegSolvedBendDegrees = value),
                    Column.Create("FinalIkLegOriginalExtensionRatio", Codecs.Float32, Unit.Unitless,
                        (in Source source) => source.Capture.Limb.LegPose.OriginalExtensionRatio, (target, value) => target.IkLegOriginalExtensionRatio = value),
                    Column.Create("FinalIkLegTargetExtensionRatio", Codecs.Float32, Unit.Unitless,
                        (in Source source) => source.Capture.Limb.LegPose.TargetExtensionRatio, (target, value) => target.IkLegTargetExtensionRatio = value),
                    Column.Create("FinalIkLegSolvedExtensionRatio", Codecs.Float32, Unit.Unitless,
                        (in Source source) => source.Capture.Limb.LegPose.SolvedExtensionRatio, (target, value) => target.IkLegSolvedExtensionRatio = value),
                    Column.Create("FinalIkLegOriginalCompressionReserve", Codecs.Float32, Unit.Metres,
                        (in Source source) => source.Capture.Limb.LegPose.OriginalCompressionReserve, (target, value) => target.IkLegOriginalCompressionReserve = value),
                    Column.Create("FinalIkLegTargetCompressionReserve", Codecs.Float32, Unit.Metres,
                        (in Source source) => source.Capture.Limb.LegPose.TargetCompressionReserve, (target, value) => target.IkLegTargetCompressionReserve = value),
                    Column.Create("FinalIkLegSolvedCompressionReserve", Codecs.Float32, Unit.Metres,
                        (in Source source) => source.Capture.Limb.LegPose.SolvedCompressionReserve, (target, value) => target.IkLegSolvedCompressionReserve = value),
                    Column.Create("FinalIkLegEffectiveBendDirection", Codecs.Vector, Unit.Direction,
                        (in Source source) => source.Capture.Limb.LegPose.EffectiveBendDirection, (target, value) => target.IkLegEffectiveBendDirection = value),
                    Column.Create("FinalIkLegAnimatedBendDirectionPreviousDot", Codecs.Float32, Unit.Unitless,
                        (in Source source) => source.Capture.Limb.LegPose.AnimatedBendDirectionPreviousDot, (target, value) => target.IkLegAnimatedBendDirectionPreviousDot = value),
                    Column.Create("FinalIkLegEffectiveBendDirectionPreviousDot", Codecs.Float32, Unit.Unitless,
                        (in Source source) => source.Capture.Limb.LegPose.EffectiveBendDirectionPreviousDot, (target, value) => target.IkLegEffectiveBendDirectionPreviousDot = value),
                    Column.Create("FinalIkPelvisAvailable", Codecs.Boolean, Unit.None,
                        (in Source source) => source.Capture.PelvisAvailable, (target, value) => target.IkPelvisAvailable = value),
                    Column.Create("FinalIkPelvisTargetPosition", Codecs.Vector, Unit.Metres,
                        (in Source source) => source.Capture.Pelvis.TargetComponentPosition, (target, value) => target.IkPelvisTargetPosition = value),
                    Column.Create("FinalIkPelvisSolvedPosition", Codecs.Vector, Unit.Metres,
                        (in Source source) => source.Capture.Pelvis.SolvedComponentPosition, (target, value) => target.IkPelvisSolvedPosition = value),
                    Column.Create("FinalIkPelvisPositionResidual", Codecs.Float32, Unit.Metres,
                        (in Source source) => source.Capture.Pelvis.PositionResidual, (target, value) => target.IkPelvisPositionResidual = value),
                    Column.Create("FinalIkPelvisRotationResidualDegrees", Codecs.Float32, Unit.Degrees,
                        (in Source source) => source.Capture.Pelvis.RotationResidualDegrees, (target, value) => target.IkPelvisRotationResidualDegrees = value),
                    Column.Create("FinalPhysicalWriteAvailable", Codecs.Boolean, Unit.None,
                        (in Source source) => source.Capture.PhysicalWriteAvailable, (target, value) => target.PhysicalWriteAvailable = value),
                    Column.Create("FinalPhysicalWriteCompletionIdentity", Codecs.UInt64, Unit.Identity,
                        (in Source source) => source.Capture.PhysicalWriteCompletionIdentity, (target, value) => target.PhysicalWriteCompletionIdentity = value),
                    Column.Create("FinalPhysicalAnkleComponentPosition", Codecs.Vector, Unit.Metres,
                        (in Source source) => source.Capture.PhysicalAnkleComponentPosition, (target, value) => target.PhysicalAnkleComponentPosition = value),
                    Column.Create("FinalPhysicalAnkleComponentRotation", Codecs.Rotation, Unit.Unitless,
                        (in Source source) => source.Capture.PhysicalAnkleComponentRotation, (target, value) => target.PhysicalAnkleComponentRotation = value),
                    Column.Create("FinalPhysicalAnkleWorldPosition", Codecs.Vector, Unit.Metres,
                        (in Source source) => source.WorldPosition, (target, value) => target.PhysicalAnkleWorldPosition = value),
                    Column.Create("FinalPhysicalAnkleWorldRotation", Codecs.Rotation, Unit.Unitless,
                        (in Source source) => source.WorldRotation, (target, value) => target.PhysicalAnkleWorldRotation = value),
                    Column.Create("FinalPhysicalHeelWorld", Codecs.Vector, Unit.Metres,
                        (in Source source) => source.Heel, (target, value) => target.PhysicalHeelWorld = value),
                    Column.Create("FinalPhysicalToeWorld", Codecs.Vector, Unit.Metres,
                        (in Source source) => source.Toe, (target, value) => target.PhysicalToeWorld = value),
                    Column.Create("FinalPhysicalAnkleGoalResidual", Codecs.Float32, Unit.Metres,
                        (in Source source) => source.GoalResidual, (target, value) => target.PhysicalAnkleGoalResidual = value),
                });
    }
}
