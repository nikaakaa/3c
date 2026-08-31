using UnityEngine;
using Source = ThirdPersonCharacter.Pipeline.Presentation.CharacterFootPathContinuityDiagnostics;
using Column = ThirdPersonCharacter.Pipeline.Editor.CharacterFootCsvColumn<ThirdPersonCharacter.Pipeline.Presentation.CharacterFootPathContinuityDiagnostics, ThirdPersonCharacter.Pipeline.Editor.CharacterFootPathContinuitySample>;
using Codecs = ThirdPersonCharacter.Pipeline.Editor.CharacterFootCsvCodecs;
using Unit = ThirdPersonCharacter.Pipeline.Editor.CharacterFootCsvUnit;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    internal sealed class CharacterFootPathContinuitySample
    {
        internal bool PathContinuityEvaluated;
        internal string PathRevisionReason;
        internal bool PathResidualRebuilt;
        internal bool TargetTrackingApplied;
        internal bool PathAvailableBefore;
        internal bool PathAvailableAfter;
        internal ulong PathPreviousLandingEventIdentity;
        internal ulong PathCurrentLandingEventIdentity;
        internal Vector3 PathPreviousTargetCorrection;
        internal Vector3 PathCurrentTargetCorrection;
        internal float PathLandingPointDelta;
        internal float PathTargetDelta;
        internal Vector3 SwingResidualBeforeRevision;
        internal Vector3 SwingResidualBeforeDecay;
        internal Vector3 SwingResidualAfterDecay;
        internal Vector3 ResidualOutputCorrection;
        internal float LandingAcceptanceDistance;
        internal float PathRevisionDistance;
        internal float SwingResidualTolerance;
        internal float ResidualTimeToLandingSeconds;
        internal float ResidualBaseHalfLifeSeconds;
        internal bool ResidualDeadlineHalfLifeAvailable;
        internal float ResidualDeadlineHalfLifeSeconds;
        internal float ResidualAppliedHalfLifeSeconds;
        internal string SwingTargetHeightAdoptionMode;
        internal float SwingRawTargetHeightAlongUp;
        internal float SwingFilteredTargetHeightBefore;
        internal float SwingTargetHeightDelta;
        internal float SwingTargetHeightAppliedDelta;
        internal bool SwingTargetHeightUpdateHeld;
        internal bool SwingTargetHeightForceRefreshed;
        internal bool SwingTargetHeightRateLimited;
        internal bool SwingTargetHeightClamped;
        internal float SwingTargetHeightForceRefreshDistance;
        internal float SwingTargetMaximumVerticalSpeed;
        internal float SwingFilteredTargetHeightAlongUp;
        internal Vector3 ComponentUp;
    }

    internal static class CharacterFootPathContinuityColumns
    {
        internal static readonly CharacterFootCsvGroup<Source, CharacterFootPathContinuitySample> Schema =
            new CharacterFootCsvGroup<Source, CharacterFootPathContinuitySample>(
                "PathContinuity", () => new CharacterFootPathContinuitySample(), new Column[]
                {
                    Column.Create("FootMotionPathContinuityEvaluated", Codecs.Boolean, Unit.None,
                        (in Source source) => source.PathContinuityEvaluated, (target, value) => target.PathContinuityEvaluated = value),
                    Column.Create("FootMotionPathRevisionReason", Codecs.Text, Unit.Category,
                        (in Source source) => source.PathRevisionReason, (target, value) => target.PathRevisionReason = value),
                    Column.Create("FootMotionPathResidualRebuilt", Codecs.Boolean, Unit.None,
                        (in Source source) => source.PathResidualRebuilt, (target, value) => target.PathResidualRebuilt = value),
                    Column.Create("FootMotionTargetTrackingApplied", Codecs.Boolean, Unit.None,
                        (in Source source) => source.TargetTrackingApplied, (target, value) => target.TargetTrackingApplied = value),
                    Column.Create("FootMotionPathAvailableBefore", Codecs.Boolean, Unit.None,
                        (in Source source) => source.PathAvailableBefore, (target, value) => target.PathAvailableBefore = value),
                    Column.Create("FootMotionPathAvailableAfter", Codecs.Boolean, Unit.None,
                        (in Source source) => source.PathAvailableAfter, (target, value) => target.PathAvailableAfter = value),
                    Column.Create("FootMotionPathPreviousLandingEventIdentity", Codecs.UInt64, Unit.Identity,
                        (in Source source) => source.PathPreviousLandingEventIdentity, (target, value) => target.PathPreviousLandingEventIdentity = value),
                    Column.Create("FootMotionPathCurrentLandingEventIdentity", Codecs.UInt64, Unit.Identity,
                        (in Source source) => source.PathCurrentLandingEventIdentity, (target, value) => target.PathCurrentLandingEventIdentity = value),
                    Column.Create("FootMotionPathPreviousTargetCorrection", Codecs.Vector, Unit.Metres,
                        (in Source source) => source.PathPreviousTargetCorrection, (target, value) => target.PathPreviousTargetCorrection = value, "FootMotionPathAvailableBefore"),
                    Column.Create("FootMotionPathCurrentTargetCorrection", Codecs.Vector, Unit.Metres,
                        (in Source source) => source.PathCurrentTargetCorrection, (target, value) => target.PathCurrentTargetCorrection = value, "FootMotionPathAvailableAfter"),
                    Column.Create("FootMotionPathLandingPointDeltaMeters", Codecs.Float32, Unit.Metres,
                        (in Source source) => source.PathLandingPointDelta, (target, value) => target.PathLandingPointDelta = value),
                    Column.Create("FootMotionPathTargetDeltaMeters", Codecs.Float32, Unit.Metres,
                        (in Source source) => source.PathTargetDelta, (target, value) => target.PathTargetDelta = value),
                    Column.Create("FootMotionSwingResidualBeforeRevision", Codecs.Vector, Unit.Metres,
                        (in Source source) => source.SwingResidualBeforeRevision, (target, value) => target.SwingResidualBeforeRevision = value),
                    Column.Create("FootMotionSwingResidualBeforeDecay", Codecs.Vector, Unit.Metres,
                        (in Source source) => source.SwingResidualBeforeDecay, (target, value) => target.SwingResidualBeforeDecay = value),
                    Column.Create("FootMotionSwingResidualAfterDecay", Codecs.Vector, Unit.Metres,
                        (in Source source) => source.SwingResidualAfterDecay, (target, value) => target.SwingResidualAfterDecay = value),
                    Column.Create("FootMotionResidualOutputCorrection", Codecs.Vector, Unit.Metres,
                        (in Source source) => source.ResidualOutputCorrection, (target, value) => target.ResidualOutputCorrection = value),
                    Column.Create("FootMotionLandingAcceptanceDistance", Codecs.Float32, Unit.Metres,
                        (in Source source) => source.LandingAcceptanceDistance, (target, value) => target.LandingAcceptanceDistance = value),
                    Column.Create("FootMotionPathRevisionDistance", Codecs.Float32, Unit.Metres,
                        (in Source source) => source.PathRevisionDistance, (target, value) => target.PathRevisionDistance = value),
                    Column.Create("FootMotionSwingResidualTolerance", Codecs.Float32, Unit.Metres,
                        (in Source source) => source.SwingResidualTolerance, (target, value) => target.SwingResidualTolerance = value),
                    Column.Create("FootMotionResidualTimeToLandingSeconds", Codecs.Float32, Unit.Seconds,
                        (in Source source) => source.ResidualTimeToLandingSeconds, (target, value) => target.ResidualTimeToLandingSeconds = value),
                    Column.Create("FootMotionResidualBaseHalfLifeSeconds", Codecs.Float32, Unit.Seconds,
                        (in Source source) => source.ResidualBaseHalfLifeSeconds, (target, value) => target.ResidualBaseHalfLifeSeconds = value),
                    Column.Create("FootMotionResidualDeadlineHalfLifeAvailable", Codecs.Boolean, Unit.None,
                        (in Source source) => source.ResidualDeadlineHalfLifeAvailable, (target, value) => target.ResidualDeadlineHalfLifeAvailable = value),
                    Column.Create("FootMotionResidualDeadlineHalfLifeSeconds", Codecs.Float32, Unit.Seconds,
                        (in Source source) => source.ResidualDeadlineHalfLifeSeconds, (target, value) => target.ResidualDeadlineHalfLifeSeconds = value, "FootMotionResidualDeadlineHalfLifeAvailable"),
                    Column.Create("FootMotionResidualAppliedHalfLifeSeconds", Codecs.Float32, Unit.Seconds,
                        (in Source source) => source.ResidualAppliedHalfLifeSeconds, (target, value) => target.ResidualAppliedHalfLifeSeconds = value),
                    Column.Create("FootMotionSwingTargetHeightAdoptionMode", Codecs.Text, Unit.Category,
                        (in Source source) => source.SwingTargetHeightAdoptionMode, (target, value) => target.SwingTargetHeightAdoptionMode = value),
                    Column.Create("FootMotionSwingRawTargetHeightAlongUp", Codecs.Float32, Unit.Metres,
                        (in Source source) => source.SwingRawTargetHeightAlongUp, (target, value) => target.SwingRawTargetHeightAlongUp = value),
                    Column.Create("FootMotionSwingFilteredTargetHeightBefore", Codecs.Float32, Unit.Metres,
                        (in Source source) => source.SwingFilteredTargetHeightBefore, (target, value) => target.SwingFilteredTargetHeightBefore = value),
                    Column.Create("FootMotionSwingTargetHeightDelta", Codecs.Float32, Unit.Metres,
                        (in Source source) => source.SwingTargetHeightDelta, (target, value) => target.SwingTargetHeightDelta = value),
                    Column.Create("FootMotionSwingTargetHeightAppliedDelta", Codecs.Float32, Unit.Metres,
                        (in Source source) => source.SwingTargetHeightAppliedDelta, (target, value) => target.SwingTargetHeightAppliedDelta = value),
                    Column.Create("FootMotionSwingTargetHeightUpdateHeld", Codecs.Boolean, Unit.None,
                        (in Source source) => source.SwingTargetHeightUpdateHeld, (target, value) => target.SwingTargetHeightUpdateHeld = value),
                    Column.Create("FootMotionSwingTargetHeightForceRefreshed", Codecs.Boolean, Unit.None,
                        (in Source source) => source.SwingTargetHeightForceRefreshed, (target, value) => target.SwingTargetHeightForceRefreshed = value),
                    Column.Create("FootMotionSwingTargetHeightRateLimited", Codecs.Boolean, Unit.None,
                        (in Source source) => source.SwingTargetHeightRateLimited, (target, value) => target.SwingTargetHeightRateLimited = value),
                    Column.Create("FootMotionSwingTargetHeightClamped", Codecs.Boolean, Unit.None,
                        (in Source source) => source.SwingTargetHeightClamped, (target, value) => target.SwingTargetHeightClamped = value),
                    Column.Create("FootMotionSwingTargetHeightForceRefreshDistance", Codecs.Float32, Unit.Metres,
                        (in Source source) => source.SwingTargetHeightForceRefreshDistance, (target, value) => target.SwingTargetHeightForceRefreshDistance = value),
                    Column.Create("FootMotionSwingTargetMaximumVerticalSpeed", Codecs.Float32, Unit.MetresPerSecond,
                        (in Source source) => source.SwingTargetMaximumVerticalSpeed, (target, value) => target.SwingTargetMaximumVerticalSpeed = value),
                    Column.Create("FootMotionSwingFilteredTargetHeightAlongUp", Codecs.Float32, Unit.Metres,
                        (in Source source) => source.SwingFilteredTargetHeightAlongUp, (target, value) => target.SwingFilteredTargetHeightAlongUp = value),
                    Column.Create("FootMotionTargetHeightComponentUp", Codecs.Vector, Unit.Direction,
                        (in Source source) => source.TargetHeightComponentUp, (target, value) => target.ComponentUp = value),
                });
    }
}
