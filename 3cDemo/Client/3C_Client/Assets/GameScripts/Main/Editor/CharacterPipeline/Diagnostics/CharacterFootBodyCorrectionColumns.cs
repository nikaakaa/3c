using UnityEngine;
using Source = ThirdPersonCharacter.Pipeline.Presentation.CharacterFootLandingPredictionInputDiagnostics;
using Column = ThirdPersonCharacter.Pipeline.Editor.CharacterFootCsvColumn<ThirdPersonCharacter.Pipeline.Presentation.CharacterFootLandingPredictionInputDiagnostics, ThirdPersonCharacter.Pipeline.Editor.CharacterFootBodyCorrectionSample>;
using Codecs = ThirdPersonCharacter.Pipeline.Editor.CharacterFootCsvCodecs;
using Unit = ThirdPersonCharacter.Pipeline.Editor.CharacterFootCsvUnit;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    internal sealed class CharacterFootBodyCorrectionSample
    {
        internal Vector3 VisiblePosition;
        internal Quaternion VisibleRotation;
        internal Vector3 VisibleVelocity;
        internal float VisibleYawVelocityDegreesPerSecond;
        internal Vector3 TargetPosition;
        internal Quaternion TargetRotation;
        internal Vector3 TargetVelocity;
        internal float TargetYawVelocityDegreesPerSecond;
        internal float PositionError;
        internal float RotationError;
        internal Vector3 CorrectionPositionError;
        internal Vector3 CorrectionPositionVelocity;
        internal float CorrectionYawVelocityDegreesPerSecond;
        internal bool CorrectionActive;
        internal bool CorrectionClamped;
        internal bool CorrectionSettled;
        internal ulong ResetSequence;
    }

    internal static class CharacterFootBodyCorrectionColumns
    {
        internal static readonly CharacterFootCsvGroup<Source, CharacterFootBodyCorrectionSample> Schema =
            new CharacterFootCsvGroup<Source, CharacterFootBodyCorrectionSample>(
                "BodyCorrection", () => new CharacterFootBodyCorrectionSample(), new Column[]
                {
                    Column.Create("VisibleBodyPosition", Codecs.Vector, Unit.Metres,
                        (in Source source) => source.VisibleBodyPosition, (target, value) => target.VisiblePosition = value),
                    Column.Create("VisibleBodyRotation", Codecs.Rotation, Unit.Unitless,
                        (in Source source) => source.VisibleBodyRotation, (target, value) => target.VisibleRotation = value),
                    Column.Create("VisibleBodyVelocity", Codecs.Vector, Unit.MetresPerSecond,
                        (in Source source) => source.VisibleBodyVelocity, (target, value) => target.VisibleVelocity = value),
                    Column.Create("VisibleBodyYawVelocityDegreesPerSecond", Codecs.Float32, Unit.DegreesPerSecond,
                        (in Source source) => source.VisibleBodyYawVelocityDegreesPerSecond, (target, value) => target.VisibleYawVelocityDegreesPerSecond = value),
                    Column.Create("TargetBodyPosition", Codecs.Vector, Unit.Metres,
                        (in Source source) => source.TargetBodyPosition, (target, value) => target.TargetPosition = value),
                    Column.Create("TargetBodyRotation", Codecs.Rotation, Unit.Unitless,
                        (in Source source) => source.TargetBodyRotation, (target, value) => target.TargetRotation = value),
                    Column.Create("TargetBodyVelocity", Codecs.Vector, Unit.MetresPerSecond,
                        (in Source source) => source.TargetBodyVelocity, (target, value) => target.TargetVelocity = value),
                    Column.Create("TargetBodyYawVelocityDegreesPerSecond", Codecs.Float32, Unit.DegreesPerSecond,
                        (in Source source) => source.TargetBodyYawVelocityDegreesPerSecond, (target, value) => target.TargetYawVelocityDegreesPerSecond = value),
                    Column.Create("BodyPositionError", Codecs.Float32, Unit.Metres,
                        (in Source source) => source.BodyPositionError, (target, value) => target.PositionError = value),
                    Column.Create("BodyRotationError", Codecs.Float32, Unit.Degrees,
                        (in Source source) => source.BodyRotationError, (target, value) => target.RotationError = value),
                    Column.Create("CorrectionPositionError", Codecs.Vector, Unit.Metres,
                        (in Source source) => source.CorrectionPositionError, (target, value) => target.CorrectionPositionError = value),
                    Column.Create("CorrectionPositionVelocity", Codecs.Vector, Unit.MetresPerSecond,
                        (in Source source) => source.CorrectionPositionVelocity, (target, value) => target.CorrectionPositionVelocity = value),
                    Column.Create("CorrectionYawVelocityDegreesPerSecond", Codecs.Float32, Unit.DegreesPerSecond,
                        (in Source source) => source.CorrectionYawVelocityDegreesPerSecond, (target, value) => target.CorrectionYawVelocityDegreesPerSecond = value),
                    Column.Create("CorrectionActive", Codecs.Boolean, Unit.None,
                        (in Source source) => source.CorrectionActive, (target, value) => target.CorrectionActive = value),
                    Column.Create("CorrectionClamped", Codecs.Boolean, Unit.None,
                        (in Source source) => source.CorrectionClamped, (target, value) => target.CorrectionClamped = value),
                    Column.Create("CorrectionSettled", Codecs.Boolean, Unit.None,
                        (in Source source) => source.CorrectionSettled, (target, value) => target.CorrectionSettled = value),
                    Column.Create("BodyResetSequence", Codecs.UInt64, Unit.Identity,
                        (in Source source) => source.BodyResetSequence, (target, value) => target.ResetSequence = value),
                });
    }
}
