using UnityEngine;
using Source = ThirdPersonCharacter.Pipeline.Presentation.CharacterFootLandingPredictionInputDiagnostics;
using Column = ThirdPersonCharacter.Pipeline.Editor.CharacterFootCsvColumn<ThirdPersonCharacter.Pipeline.Presentation.CharacterFootLandingPredictionInputDiagnostics, ThirdPersonCharacter.Pipeline.Editor.CharacterFootPredictionMotionSample>;
using Codecs = ThirdPersonCharacter.Pipeline.Editor.CharacterFootCsvCodecs;
using Unit = ThirdPersonCharacter.Pipeline.Editor.CharacterFootCsvUnit;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    internal sealed class CharacterFootPredictionMotionSample
    {
        internal bool Available;
        internal string RejectReason;
        internal string ResetReason;
        internal string SourceIdentity;
        internal Vector2 RawCurrentVelocity;
        internal Vector2 RawContinuationVelocity;
        internal Vector2 PreviousStableCurrentVelocity;
        internal Vector2 PreviousStableContinuationVelocity;
        internal Vector2 StableCurrentVelocity;
        internal Vector2 StableContinuationVelocity;
        internal Vector2 CurrentVelocityDelta;
        internal Vector2 ContinuationVelocityDelta;
        internal float VelocityResponseAlpha;
        internal float VelocityDeltaThreshold;
        internal float VelocitySmoothSpeed;
        internal float MaximumSpeed;
        internal bool CurrentResponseApplied;
        internal bool ContinuationResponseApplied;
        internal bool CurrentMaximumSpeedClamped;
        internal bool ContinuationMaximumSpeedClamped;
        internal ulong Revision;
    }

    internal static class CharacterFootPredictionMotionColumns
    {
        internal static readonly CharacterFootCsvGroup<Source, CharacterFootPredictionMotionSample> Schema =
            new CharacterFootCsvGroup<Source, CharacterFootPredictionMotionSample>(
                "PredictionMotion", () => new CharacterFootPredictionMotionSample(), new Column[]
                {
                    Column.Create("PredictionMotionAvailable", Codecs.Boolean, Unit.None,
                        (in Source source) => source.PredictionMotionAvailable, (target, value) => target.Available = value),
                    Column.Create("PredictionMotionRejectReason", Codecs.Text, Unit.Category,
                        (in Source source) => source.PredictionMotionRejectReason, (target, value) => target.RejectReason = value),
                    Column.Create("PredictionMotionResetReason", Codecs.Text, Unit.Category,
                        (in Source source) => source.PredictionMotionResetReason, (target, value) => target.ResetReason = value),
                    Column.Create("PredictionMotionSourceIdentity", Codecs.Text, Unit.Identity,
                        (in Source source) => source.PredictionMotionSourceIdentity, (target, value) => target.SourceIdentity = value),
                    Column.Create("PredictionRawCurrentVelocityX", Codecs.Float32, Unit.MetresPerSecond,
                        (in Source source) => source.PredictionRawCurrentVelocityX, (target, value) => target.RawCurrentVelocity.x = value),
                    Column.Create("PredictionRawCurrentVelocityZ", Codecs.Float32, Unit.MetresPerSecond,
                        (in Source source) => source.PredictionRawCurrentVelocityZ, (target, value) => target.RawCurrentVelocity.y = value),
                    Column.Create("PredictionRawContinuationVelocityX", Codecs.Float32, Unit.MetresPerSecond,
                        (in Source source) => source.PredictionRawContinuationVelocityX, (target, value) => target.RawContinuationVelocity.x = value),
                    Column.Create("PredictionRawContinuationVelocityZ", Codecs.Float32, Unit.MetresPerSecond,
                        (in Source source) => source.PredictionRawContinuationVelocityZ, (target, value) => target.RawContinuationVelocity.y = value),
                    Column.Create("PredictionPreviousStableCurrentVelocityX", Codecs.Float32, Unit.MetresPerSecond,
                        (in Source source) => source.PredictionPreviousStableCurrentVelocityX, (target, value) => target.PreviousStableCurrentVelocity.x = value),
                    Column.Create("PredictionPreviousStableCurrentVelocityZ", Codecs.Float32, Unit.MetresPerSecond,
                        (in Source source) => source.PredictionPreviousStableCurrentVelocityZ, (target, value) => target.PreviousStableCurrentVelocity.y = value),
                    Column.Create("PredictionPreviousStableContinuationVelocityX", Codecs.Float32, Unit.MetresPerSecond,
                        (in Source source) => source.PredictionPreviousStableContinuationVelocityX, (target, value) => target.PreviousStableContinuationVelocity.x = value),
                    Column.Create("PredictionPreviousStableContinuationVelocityZ", Codecs.Float32, Unit.MetresPerSecond,
                        (in Source source) => source.PredictionPreviousStableContinuationVelocityZ, (target, value) => target.PreviousStableContinuationVelocity.y = value),
                    Column.Create("PredictionStableCurrentVelocityX", Codecs.Float32, Unit.MetresPerSecond,
                        (in Source source) => source.PredictionStableCurrentVelocityX, (target, value) => target.StableCurrentVelocity.x = value),
                    Column.Create("PredictionStableCurrentVelocityZ", Codecs.Float32, Unit.MetresPerSecond,
                        (in Source source) => source.PredictionStableCurrentVelocityZ, (target, value) => target.StableCurrentVelocity.y = value),
                    Column.Create("PredictionStableContinuationVelocityX", Codecs.Float32, Unit.MetresPerSecond,
                        (in Source source) => source.PredictionStableContinuationVelocityX, (target, value) => target.StableContinuationVelocity.x = value),
                    Column.Create("PredictionStableContinuationVelocityZ", Codecs.Float32, Unit.MetresPerSecond,
                        (in Source source) => source.PredictionStableContinuationVelocityZ, (target, value) => target.StableContinuationVelocity.y = value),
                    Column.Create("PredictionCurrentVelocityDeltaX", Codecs.Float32, Unit.MetresPerSecond,
                        (in Source source) => source.PredictionCurrentVelocityDeltaX, (target, value) => target.CurrentVelocityDelta.x = value),
                    Column.Create("PredictionCurrentVelocityDeltaZ", Codecs.Float32, Unit.MetresPerSecond,
                        (in Source source) => source.PredictionCurrentVelocityDeltaZ, (target, value) => target.CurrentVelocityDelta.y = value),
                    Column.Create("PredictionContinuationVelocityDeltaX", Codecs.Float32, Unit.MetresPerSecond,
                        (in Source source) => source.PredictionContinuationVelocityDeltaX, (target, value) => target.ContinuationVelocityDelta.x = value),
                    Column.Create("PredictionContinuationVelocityDeltaZ", Codecs.Float32, Unit.MetresPerSecond,
                        (in Source source) => source.PredictionContinuationVelocityDeltaZ, (target, value) => target.ContinuationVelocityDelta.y = value),
                    Column.Create("PredictionVelocityResponseAlpha", Codecs.Float32, Unit.Unitless,
                        (in Source source) => source.PredictionVelocityResponseAlpha, (target, value) => target.VelocityResponseAlpha = value),
                    Column.Create("PredictionVelocityDeltaThreshold", Codecs.Float32, Unit.MetresPerSecond,
                        (in Source source) => source.PredictionVelocityDeltaThreshold, (target, value) => target.VelocityDeltaThreshold = value),
                    Column.Create("PredictionVelocitySmoothSpeed", Codecs.Float32, Unit.PerSecond,
                        (in Source source) => source.PredictionVelocitySmoothSpeed, (target, value) => target.VelocitySmoothSpeed = value),
                    Column.Create("PredictionMaximumSpeed", Codecs.Float32, Unit.MetresPerSecond,
                        (in Source source) => source.PredictionMaximumSpeed, (target, value) => target.MaximumSpeed = value),
                    Column.Create("PredictionCurrentResponseApplied", Codecs.Boolean, Unit.None,
                        (in Source source) => source.PredictionCurrentResponseApplied, (target, value) => target.CurrentResponseApplied = value),
                    Column.Create("PredictionContinuationResponseApplied", Codecs.Boolean, Unit.None,
                        (in Source source) => source.PredictionContinuationResponseApplied, (target, value) => target.ContinuationResponseApplied = value),
                    Column.Create("PredictionCurrentMaximumSpeedClamped", Codecs.Boolean, Unit.None,
                        (in Source source) => source.PredictionCurrentMaximumSpeedClamped, (target, value) => target.CurrentMaximumSpeedClamped = value),
                    Column.Create("PredictionContinuationMaximumSpeedClamped", Codecs.Boolean, Unit.None,
                        (in Source source) => source.PredictionContinuationMaximumSpeedClamped, (target, value) => target.ContinuationMaximumSpeedClamped = value),
                    Column.Create("PredictionMotionRevision", Codecs.UInt64, Unit.Identity,
                        (in Source source) => source.PredictionMotionRevision, (target, value) => target.Revision = value),
                });
    }
}
