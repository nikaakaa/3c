using UnityEngine;
using Source = ThirdPersonCharacter.Pipeline.Presentation.CharacterFootSwingMotionDiagnostics;
using Column = ThirdPersonCharacter.Pipeline.Editor.CharacterFootCsvColumn<ThirdPersonCharacter.Pipeline.Presentation.CharacterFootSwingMotionDiagnostics, ThirdPersonCharacter.Pipeline.Editor.CharacterFootLifecycleSample>;
using Codecs = ThirdPersonCharacter.Pipeline.Editor.CharacterFootCsvCodecs;
using Unit = ThirdPersonCharacter.Pipeline.Editor.CharacterFootCsvUnit;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    internal sealed class CharacterFootLifecycleSample
    {
        internal string PreTransitionReason;
        internal string PreTransitionSource;
        internal string PreTransitionTarget;
        internal string PreTransitionAnchorCommand;
        internal string PostTransitionReason;
        internal string PostTransitionSource;
        internal string PostTransitionTarget;
        internal string PostTransitionAnchorCommand;
        internal bool LifecycleTransitionEvaluated;
        internal bool PreviousLockRequestAvailable;
        internal bool PreviousLockRequested;
        internal ulong PreviousLockRequestEventIdentity;
        internal string PreviousLockRequestMode;
        internal float PreviousLockRequestWeight;
        internal float PreviousContactEdgeSeconds;
        internal ulong PreviousLatestContactEventIdentity;
        internal ulong PreviousLatestReleasedContactEventIdentity;
        internal ulong PreviousCompletedLockWeightEventIdentity;
        internal bool PreviousContactAnchorAvailable;
        internal ulong PreviousContactAnchorEventIdentity;
        internal ulong PreviousContactAnchorAcquiredFrameSequence;
        internal ulong PreviousContactAnchorAcquiredCompletionIdentity;
        internal ulong PreviousContactAnchorWorldRevision;
        internal int PreviousContactAnchorSurfaceIdentity;
        internal Vector3 PreviousContactAnchorPoint;
        internal Vector3 PreviousContactAnchorNormal;
        internal bool CurrentLockRequested;
        internal ulong CurrentLockRequestEventIdentity;
        internal string CurrentLockRequestMode;
        internal float CurrentLockRequestWeight;
        internal string CurrentLockRequestAvailability;
        internal string ContactEdge;
        internal float CurrentContactEdgeSeconds;
        internal ulong CurrentLatestContactEventIdentity;
        internal ulong CurrentLatestReleasedContactEventIdentity;
        internal ulong CurrentCompletedLockWeightEventIdentity;
        internal bool CurrentContactAnchorAvailable;
        internal ulong CurrentContactAnchorEventIdentity;
        internal ulong CurrentContactAnchorAcquiredFrameSequence;
        internal ulong CurrentContactAnchorAcquiredCompletionIdentity;
        internal ulong CurrentContactAnchorWorldRevision;
        internal int CurrentContactAnchorSurfaceIdentity;
        internal Vector3 CurrentContactAnchorPoint;
        internal Vector3 CurrentContactAnchorNormal;
        internal bool SameEventContactReentryRefreshed;
        internal bool SameEventContactReentryUnavailable;
        internal bool RetainedVerifiedAnchor;
        internal bool ReentryInterpolationHistoryRetained;
        internal float FormalFootPlacementWeight;
        internal bool HardOwnershipLoss;
        internal string HardOwnershipLossReason;
        internal bool PreTransitionSuppressOutput;
        internal bool PreTransitionResetInterpolation;
        internal bool PostTransitionEvaluated;
        internal bool PostTransitionSuppressOutput;
        internal bool PostTransitionResetInterpolation;
    }

    internal static class CharacterFootLifecycleColumns
    {
        internal static readonly CharacterFootCsvGroup<Source, CharacterFootLifecycleSample> Schema =
            new CharacterFootCsvGroup<Source, CharacterFootLifecycleSample>(
                "Lifecycle", () => new CharacterFootLifecycleSample(), new Column[]
                {
                    Column.Create("FootMotionPreTransitionReason", Codecs.Text, Unit.Category,
                        (in Source source) => source.PreTransitionReason, (target, value) => target.PreTransitionReason = value),
                    Column.Create("FootMotionPreTransitionSource", Codecs.Text, Unit.Category,
                        (in Source source) => source.PreTransitionSource.ToString(), (target, value) => target.PreTransitionSource = value),
                    Column.Create("FootMotionPreTransitionTarget", Codecs.Text, Unit.Category,
                        (in Source source) => source.PreTransitionTarget.ToString(), (target, value) => target.PreTransitionTarget = value),
                    Column.Create("FootMotionPreTransitionAnchorCommand", Codecs.Text, Unit.Category,
                        (in Source source) => source.PreTransitionAnchorCommand, (target, value) => target.PreTransitionAnchorCommand = value),
                    Column.Create("FootMotionPostTransitionReason", Codecs.Text, Unit.Category,
                        (in Source source) => source.PostTransitionReason, (target, value) => target.PostTransitionReason = value),
                    Column.Create("FootMotionPostTransitionSource", Codecs.Text, Unit.Category,
                        (in Source source) => source.PostTransitionSource.ToString(), (target, value) => target.PostTransitionSource = value),
                    Column.Create("FootMotionPostTransitionTarget", Codecs.Text, Unit.Category,
                        (in Source source) => source.PostTransitionTarget.ToString(), (target, value) => target.PostTransitionTarget = value),
                    Column.Create("FootMotionPostTransitionAnchorCommand", Codecs.Text, Unit.Category,
                        (in Source source) => source.PostTransitionAnchorCommand, (target, value) => target.PostTransitionAnchorCommand = value),
                    Column.Create("FootMotionLifecycleTransitionEvaluated", Codecs.Boolean, Unit.None,
                        (in Source source) => source.LifecycleTransitionEvaluated, (target, value) => target.LifecycleTransitionEvaluated = value),
                    Column.Create("FootMotionPreviousLockRequestAvailable", Codecs.Boolean, Unit.None,
                        (in Source source) => source.PreviousLockRequestAvailable, (target, value) => target.PreviousLockRequestAvailable = value),
                    Column.Create("FootMotionPreviousLockRequested", Codecs.Boolean, Unit.None,
                        (in Source source) => source.PreviousLockRequested, (target, value) => target.PreviousLockRequested = value, "FootMotionPreviousLockRequestAvailable"),
                    Column.Create("FootMotionPreviousLockRequestEventIdentity", Codecs.UInt64, Unit.Identity,
                        (in Source source) => source.PreviousLockRequestEventIdentity, (target, value) => target.PreviousLockRequestEventIdentity = value, "FootMotionPreviousLockRequestAvailable"),
                    Column.Create("FootMotionPreviousLockRequestMode", Codecs.Text, Unit.Category,
                        (in Source source) => source.PreviousLockRequestMode, (target, value) => target.PreviousLockRequestMode = value, "FootMotionPreviousLockRequestAvailable"),
                    Column.Create("FootMotionPreviousLockRequestWeight", Codecs.Float32, Unit.Unitless,
                        (in Source source) => source.PreviousLockRequestWeight, (target, value) => target.PreviousLockRequestWeight = value, "FootMotionPreviousLockRequestAvailable"),
                    Column.Create("FootMotionPreviousContactEdgeSeconds", Codecs.Float32, Unit.Seconds,
                        (in Source source) => source.PreviousContactEdgeSeconds, (target, value) => target.PreviousContactEdgeSeconds = value),
                    Column.Create("FootMotionPreviousLatestContactEventIdentity", Codecs.UInt64, Unit.Identity,
                        (in Source source) => source.PreviousLatestContactEventIdentity, (target, value) => target.PreviousLatestContactEventIdentity = value),
                    Column.Create("FootMotionPreviousLatestReleasedContactEventIdentity", Codecs.UInt64, Unit.Identity,
                        (in Source source) => source.PreviousLatestReleasedContactEventIdentity, (target, value) => target.PreviousLatestReleasedContactEventIdentity = value),
                    Column.Create("FootMotionPreviousCompletedLockWeightEventIdentity", Codecs.UInt64, Unit.Identity,
                        (in Source source) => source.PreviousCompletedLockWeightEventIdentity, (target, value) => target.PreviousCompletedLockWeightEventIdentity = value),
                    Column.Create("FootMotionPreviousContactAnchorAvailable", Codecs.Boolean, Unit.None,
                        (in Source source) => source.PreviousContactAnchorAvailable, (target, value) => target.PreviousContactAnchorAvailable = value),
                    Column.Create("FootMotionPreviousContactAnchorEventIdentity", Codecs.UInt64, Unit.Identity,
                        (in Source source) => source.PreviousContactAnchorEventIdentity, (target, value) => target.PreviousContactAnchorEventIdentity = value, "FootMotionPreviousContactAnchorAvailable"),
                    Column.Create("FootMotionPreviousContactAnchorAcquiredFrameSequence", Codecs.UInt64, Unit.Frame,
                        (in Source source) => source.PreviousContactAnchorAcquiredFrameSequence, (target, value) => target.PreviousContactAnchorAcquiredFrameSequence = value, "FootMotionPreviousContactAnchorAvailable"),
                    Column.Create("FootMotionPreviousContactAnchorAcquiredCompletionIdentity", Codecs.UInt64, Unit.Identity,
                        (in Source source) => source.PreviousContactAnchorAcquiredCompletionIdentity, (target, value) => target.PreviousContactAnchorAcquiredCompletionIdentity = value, "FootMotionPreviousContactAnchorAvailable"),
                    Column.Create("FootMotionPreviousContactAnchorWorldRevision", Codecs.UInt64, Unit.Identity,
                        (in Source source) => source.PreviousContactAnchorWorldRevision, (target, value) => target.PreviousContactAnchorWorldRevision = value, "FootMotionPreviousContactAnchorAvailable"),
                    Column.Create("FootMotionPreviousContactAnchorSurfaceIdentity", Codecs.Int32, Unit.Identity,
                        (in Source source) => source.PreviousContactAnchorSurfaceIdentity, (target, value) => target.PreviousContactAnchorSurfaceIdentity = value, "FootMotionPreviousContactAnchorAvailable"),
                    Column.Create("FootMotionPreviousContactAnchorPoint", Codecs.Vector, Unit.Metres,
                        (in Source source) => source.PreviousContactAnchorPoint, (target, value) => target.PreviousContactAnchorPoint = value, "FootMotionPreviousContactAnchorAvailable"),
                    Column.Create("FootMotionPreviousContactAnchorNormal", Codecs.Vector, Unit.Direction,
                        (in Source source) => source.PreviousContactAnchorNormal, (target, value) => target.PreviousContactAnchorNormal = value, "FootMotionPreviousContactAnchorAvailable"),
                    Column.Create("FootMotionCurrentLockRequested", Codecs.Boolean, Unit.None,
                        (in Source source) => source.CurrentLockRequested, (target, value) => target.CurrentLockRequested = value),
                    Column.Create("FootMotionCurrentLockRequestEventIdentity", Codecs.UInt64, Unit.Identity,
                        (in Source source) => source.CurrentLockRequestEventIdentity, (target, value) => target.CurrentLockRequestEventIdentity = value),
                    Column.Create("FootMotionCurrentLockRequestMode", Codecs.Text, Unit.Category,
                        (in Source source) => source.CurrentLockRequestMode, (target, value) => target.CurrentLockRequestMode = value),
                    Column.Create("FootMotionCurrentLockRequestWeight", Codecs.Float32, Unit.Unitless,
                        (in Source source) => source.CurrentLockRequestWeight, (target, value) => target.CurrentLockRequestWeight = value),
                    Column.Create("FootMotionCurrentLockRequestAvailability", Codecs.Text, Unit.Category,
                        (in Source source) => source.CurrentLockRequestAvailability, (target, value) => target.CurrentLockRequestAvailability = value),
                    Column.Create("FootMotionContactEdge", Codecs.Text, Unit.Category,
                        (in Source source) => source.ContactEdge, (target, value) => target.ContactEdge = value),
                    Column.Create("FootMotionCurrentContactEdgeSeconds", Codecs.Float32, Unit.Seconds,
                        (in Source source) => source.CurrentContactEdgeSeconds, (target, value) => target.CurrentContactEdgeSeconds = value),
                    Column.Create("FootMotionCurrentLatestContactEventIdentity", Codecs.UInt64, Unit.Identity,
                        (in Source source) => source.CurrentLatestContactEventIdentity, (target, value) => target.CurrentLatestContactEventIdentity = value),
                    Column.Create("FootMotionCurrentLatestReleasedContactEventIdentity", Codecs.UInt64, Unit.Identity,
                        (in Source source) => source.CurrentLatestReleasedContactEventIdentity, (target, value) => target.CurrentLatestReleasedContactEventIdentity = value),
                    Column.Create("FootMotionCurrentCompletedLockWeightEventIdentity", Codecs.UInt64, Unit.Identity,
                        (in Source source) => source.CurrentCompletedLockWeightEventIdentity, (target, value) => target.CurrentCompletedLockWeightEventIdentity = value),
                    Column.Create("FootMotionCurrentContactAnchorAvailable", Codecs.Boolean, Unit.None,
                        (in Source source) => source.CurrentContactAnchorAvailable, (target, value) => target.CurrentContactAnchorAvailable = value),
                    Column.Create("FootMotionCurrentContactAnchorEventIdentity", Codecs.UInt64, Unit.Identity,
                        (in Source source) => source.CurrentContactAnchorEventIdentity, (target, value) => target.CurrentContactAnchorEventIdentity = value, "FootMotionCurrentContactAnchorAvailable"),
                    Column.Create("FootMotionCurrentContactAnchorAcquiredFrameSequence", Codecs.UInt64, Unit.Frame,
                        (in Source source) => source.CurrentContactAnchorAcquiredFrameSequence, (target, value) => target.CurrentContactAnchorAcquiredFrameSequence = value, "FootMotionCurrentContactAnchorAvailable"),
                    Column.Create("FootMotionCurrentContactAnchorAcquiredCompletionIdentity", Codecs.UInt64, Unit.Identity,
                        (in Source source) => source.CurrentContactAnchorAcquiredCompletionIdentity, (target, value) => target.CurrentContactAnchorAcquiredCompletionIdentity = value, "FootMotionCurrentContactAnchorAvailable"),
                    Column.Create("FootMotionCurrentContactAnchorWorldRevision", Codecs.UInt64, Unit.Identity,
                        (in Source source) => source.CurrentContactAnchorWorldRevision, (target, value) => target.CurrentContactAnchorWorldRevision = value, "FootMotionCurrentContactAnchorAvailable"),
                    Column.Create("FootMotionCurrentContactAnchorSurfaceIdentity", Codecs.Int32, Unit.Identity,
                        (in Source source) => source.CurrentContactAnchorSurfaceIdentity, (target, value) => target.CurrentContactAnchorSurfaceIdentity = value, "FootMotionCurrentContactAnchorAvailable"),
                    Column.Create("FootMotionCurrentContactAnchorPoint", Codecs.Vector, Unit.Metres,
                        (in Source source) => source.CurrentContactAnchorPoint, (target, value) => target.CurrentContactAnchorPoint = value, "FootMotionCurrentContactAnchorAvailable"),
                    Column.Create("FootMotionCurrentContactAnchorNormal", Codecs.Vector, Unit.Direction,
                        (in Source source) => source.CurrentContactAnchorNormal, (target, value) => target.CurrentContactAnchorNormal = value, "FootMotionCurrentContactAnchorAvailable"),
                    Column.Create("FootMotionSameEventContactReentryRefreshed", Codecs.Boolean, Unit.None,
                        (in Source source) => source.SameEventContactReentryRefreshed, (target, value) => target.SameEventContactReentryRefreshed = value),
                    Column.Create("FootMotionSameEventContactReentryUnavailable", Codecs.Boolean, Unit.None,
                        (in Source source) => source.SameEventContactReentryUnavailable, (target, value) => target.SameEventContactReentryUnavailable = value),
                    Column.Create("FootMotionRetainedVerifiedAnchor", Codecs.Boolean, Unit.None,
                        (in Source source) => source.RetainedVerifiedAnchor, (target, value) => target.RetainedVerifiedAnchor = value),
                    Column.Create("FootMotionReentryInterpolationHistoryRetained", Codecs.Boolean, Unit.None,
                        (in Source source) => source.ReentryInterpolationHistoryRetained, (target, value) => target.ReentryInterpolationHistoryRetained = value),
                    Column.Create("FootMotionFormalFootPlacementWeight", Codecs.Float32, Unit.Unitless,
                        (in Source source) => source.FormalFootPlacementWeight, (target, value) => target.FormalFootPlacementWeight = value),
                    Column.Create("FootMotionHardOwnershipLoss", Codecs.Boolean, Unit.None,
                        (in Source source) => source.HardOwnershipLoss, (target, value) => target.HardOwnershipLoss = value),
                    Column.Create("FootMotionHardOwnershipLossReason", Codecs.Text, Unit.Category,
                        (in Source source) => source.HardOwnershipLossReason, (target, value) => target.HardOwnershipLossReason = value),
                    Column.Create("FootMotionPreTransitionSuppressOutput", Codecs.Boolean, Unit.None,
                        (in Source source) => source.PreTransitionSuppressOutput, (target, value) => target.PreTransitionSuppressOutput = value),
                    Column.Create("FootMotionPreTransitionResetInterpolation", Codecs.Boolean, Unit.None,
                        (in Source source) => source.PreTransitionResetInterpolation, (target, value) => target.PreTransitionResetInterpolation = value),
                    Column.Create("FootMotionPostTransitionEvaluated", Codecs.Boolean, Unit.None,
                        (in Source source) => source.PostTransitionEvaluated, (target, value) => target.PostTransitionEvaluated = value),
                    Column.Create("FootMotionPostTransitionSuppressOutput", Codecs.Boolean, Unit.None,
                        (in Source source) => source.PostTransitionSuppressOutput, (target, value) => target.PostTransitionSuppressOutput = value),
                    Column.Create("FootMotionPostTransitionResetInterpolation", Codecs.Boolean, Unit.None,
                        (in Source source) => source.PostTransitionResetInterpolation, (target, value) => target.PostTransitionResetInterpolation = value),
                });
    }
}
