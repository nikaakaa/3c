using UnityEngine;
using Source = ThirdPersonCharacter.Pipeline.Presentation.CharacterFootLandingPredictionInputDiagnostics;
using Column = ThirdPersonCharacter.Pipeline.Editor.CharacterFootCsvColumn<ThirdPersonCharacter.Pipeline.Presentation.CharacterFootLandingPredictionInputDiagnostics, ThirdPersonCharacter.Pipeline.Editor.CharacterFootTimingSample>;
using Codecs = ThirdPersonCharacter.Pipeline.Editor.CharacterFootCsvCodecs;
using Unit = ThirdPersonCharacter.Pipeline.Editor.CharacterFootCsvUnit;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    internal sealed class CharacterFootTimingSample
    {
        internal float DeltaSeconds;
        internal ulong PreviousBodyTick;
        internal ulong CurrentBodyTick;
        internal float BodySampleAlpha;
        internal float BodySampleAgeSeconds;
        internal bool MotionTimelineAvailable;
        internal ulong TimelineGeneration;
        internal ulong TimelineAuthorityTick;
        internal int TimelineTickRate;
        internal Vector2 TimelineCurrentVelocity;
        internal Vector2 TimelineContinuationVelocity;
        internal bool TimelineHasContinuation;
        internal float TimelineBodyYawVelocityDegreesPerSecond;
        internal float TimelineMaximumBodyYawVelocityDegreesPerSecond;
        internal float CurrentSegmentRemainingSeconds;
    }

    internal static class CharacterFootTimingColumns
    {
        internal static readonly CharacterFootCsvGroup<Source, CharacterFootTimingSample> Schema =
            new CharacterFootCsvGroup<Source, CharacterFootTimingSample>(
                "Timing", () => new CharacterFootTimingSample(), new Column[]
                {
                    Column.Create("PresentationDeltaSeconds", Codecs.Float32, Unit.Seconds,
                        (in Source source) => source.PresentationDeltaSeconds, (target, value) => target.DeltaSeconds = value),
                    Column.Create("PreviousBodyTick", Codecs.UInt64, Unit.Frame,
                        (in Source source) => source.PreviousBodyTick, (target, value) => target.PreviousBodyTick = value),
                    Column.Create("CurrentBodyTick", Codecs.UInt64, Unit.Frame,
                        (in Source source) => source.CurrentBodyTick, (target, value) => target.CurrentBodyTick = value),
                    Column.Create("BodySampleAlpha", Codecs.Float32, Unit.Unitless,
                        (in Source source) => source.BodySampleAlpha, (target, value) => target.BodySampleAlpha = value),
                    Column.Create("BodySampleAgeSeconds", Codecs.Float32, Unit.Seconds,
                        (in Source source) => source.BodySampleAgeSeconds, (target, value) => target.BodySampleAgeSeconds = value),
                    Column.Create("MotionTimelineAvailable", Codecs.Boolean, Unit.None,
                        (in Source source) => source.MotionTimelineAvailable, (target, value) => target.MotionTimelineAvailable = value),
                    Column.Create("TimelineGeneration", Codecs.UInt64, Unit.Identity,
                        (in Source source) => source.TimelineGeneration, (target, value) => target.TimelineGeneration = value),
                    Column.Create("TimelineAuthorityTick", Codecs.UInt64, Unit.Frame,
                        (in Source source) => source.TimelineAuthorityTick, (target, value) => target.TimelineAuthorityTick = value),
                    Column.Create("TimelineTickRate", Codecs.Int32, Unit.Hertz,
                        (in Source source) => source.TimelineTickRate, (target, value) => target.TimelineTickRate = value),
                    Column.Create("TimelineCurrentVelocityX", Codecs.Float32, Unit.MetresPerSecond,
                        (in Source source) => source.TimelineCurrentVelocityX, (target, value) => target.TimelineCurrentVelocity.x = value),
                    Column.Create("TimelineCurrentVelocityZ", Codecs.Float32, Unit.MetresPerSecond,
                        (in Source source) => source.TimelineCurrentVelocityZ, (target, value) => target.TimelineCurrentVelocity.y = value),
                    Column.Create("TimelineContinuationVelocityX", Codecs.Float32, Unit.MetresPerSecond,
                        (in Source source) => source.TimelineContinuationVelocityX, (target, value) => target.TimelineContinuationVelocity.x = value),
                    Column.Create("TimelineContinuationVelocityZ", Codecs.Float32, Unit.MetresPerSecond,
                        (in Source source) => source.TimelineContinuationVelocityZ, (target, value) => target.TimelineContinuationVelocity.y = value),
                    Column.Create("TimelineHasContinuation", Codecs.Boolean, Unit.None,
                        (in Source source) => source.TimelineHasContinuation, (target, value) => target.TimelineHasContinuation = value),
                    Column.Create("TimelineBodyYawVelocityDegreesPerSecond", Codecs.Float32, Unit.DegreesPerSecond,
                        (in Source source) => source.TimelineBodyYawVelocityDegreesPerSecond, (target, value) => target.TimelineBodyYawVelocityDegreesPerSecond = value),
                    Column.Create("TimelineMaximumBodyYawVelocityDegreesPerSecond", Codecs.Float32, Unit.DegreesPerSecond,
                        (in Source source) => source.TimelineMaximumBodyYawVelocityDegreesPerSecond, (target, value) => target.TimelineMaximumBodyYawVelocityDegreesPerSecond = value),
                    Column.Create("CurrentSegmentRemainingSeconds", Codecs.Float32, Unit.Seconds,
                        (in Source source) => source.CurrentSegmentRemainingSeconds, (target, value) => target.CurrentSegmentRemainingSeconds = value),
                });
    }
}
