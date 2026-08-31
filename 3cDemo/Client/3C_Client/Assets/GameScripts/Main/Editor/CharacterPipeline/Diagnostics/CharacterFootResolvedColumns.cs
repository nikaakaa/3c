using UnityEngine;
using Source = ThirdPersonCharacter.Pipeline.Presentation.CharacterResolvedFootDiagnostics;
using Column = ThirdPersonCharacter.Pipeline.Editor.CharacterFootCsvColumn<ThirdPersonCharacter.Pipeline.Presentation.CharacterResolvedFootDiagnostics, ThirdPersonCharacter.Pipeline.Editor.CharacterFootResolvedSample>;
using Codecs = ThirdPersonCharacter.Pipeline.Editor.CharacterFootCsvCodecs;
using Unit = ThirdPersonCharacter.Pipeline.Editor.CharacterFootCsvUnit;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    internal sealed class CharacterFootResolvedSample
    {
        internal ulong Frame;
        internal ulong Completion;
        internal string RigId;
        internal string RigRevision;
        internal string Side;
        internal string Outcome;
        internal Vector3 FinalSole;
        internal Vector3 EffectiveSole;
        internal Vector3 GoalTargetAnkle;
        internal Quaternion GoalTargetRotation;
        internal Vector3 EffectiveAnkle;
        internal Quaternion EffectiveRotation;
        internal Vector3 EffectiveHeel;
        internal Vector3 EffectiveToe;
        internal Vector3 EffectiveSoleFromContacts;
        internal Vector3 SourceSoleForward;
        internal Quaternion SourceSoleFrameLocalRotation;
        internal Vector3 GoalTargetCorrection;
        internal Vector3 EffectiveSoleCorrection;
        internal float PositionWeight;
        internal float RotationWeight;
        internal CharacterFootSupportTargetSample SupportTarget = new CharacterFootSupportTargetSample();
        internal bool ContactAvailable;
        internal ulong ContactEventIdentity;
        internal Vector3 ContactPoint;
        internal float ContactOwnership;
        internal string SupportEligibility;
        internal float SupportWeight;
        internal float SupportIntentWeight;
        internal float SupportHorizontalError;
        internal ulong SupportEventIdentity;
        internal bool PelvisReachAvailable;
        internal ulong PelvisReachEventIdentity;
        internal Vector3 PelvisReachPoint;
        internal bool LandingReachAvailable;
        internal ulong LandingReachEventIdentity;
        internal Vector3 LandingReachHip;
        internal Vector3 LandingReachTargetAnkle;
        internal float LandingReachLegLength;
        internal float LandingReachMinimumCompressionReserve;
    }

    internal sealed class CharacterFootSupportTargetSample
    {
        internal bool Available;
        internal ulong Frame;
        internal ulong Completion;
        internal string Side;
        internal Vector3 Position;
        internal Vector3 Normal;
        internal int Surface;
        internal ulong WorldRevision;
        internal string Kind;
        internal string PositionSource;
        internal ulong PositionFrame;
        internal ulong PositionCompletion;
        internal ulong PositionEvent;
        internal ulong PositionPath;
        internal string NormalSource;
        internal ulong NormalFrame;
        internal ulong NormalCompletion;
        internal ulong NormalEvent;
    }

    internal static class CharacterFootResolvedColumns
    {
        internal static readonly CharacterFootCsvGroup<Source, CharacterFootResolvedSample> Schema =
            new CharacterFootCsvGroup<Source, CharacterFootResolvedSample>(
                "ResolvedFoot",
                () => new CharacterFootResolvedSample(),
                new Column[]
                {
                Column.Create("ResolvedFrameSequence", Codecs.UInt64, Unit.Frame,
                    (in Source source) => source.FrameSequence, (target, value) => target.Frame = value),
                Column.Create("ResolvedCompletionIdentity", Codecs.UInt64, Unit.Identity,
                    (in Source source) => source.CompletionIdentity, (target, value) => target.Completion = value),
                Column.Create("ResolvedRigId", Codecs.Text, Unit.Identity,
                    (in Source source) => source.RigId, (target, value) => target.RigId = value),
                Column.Create("ResolvedRigRevision", Codecs.Text, Unit.Identity,
                    (in Source source) => source.RigRevision, (target, value) => target.RigRevision = value),
                Column.Create("ResolvedSide", Codecs.Text, Unit.Category,
                    (in Source source) => source.Side.ToString(), (target, value) => target.Side = value),
                Column.Create("ResolvedOutcome", Codecs.Text, Unit.Category,
                    (in Source source) => source.Outcome.ToString(), (target, value) => target.Outcome = value),
                Column.Create("ResolvedFinalSole", Codecs.Vector, Unit.Metres,
                    (in Source source) => source.FinalSole, (target, value) => target.FinalSole = value),
                Column.Create("ResolvedEffectiveSole", Codecs.Vector, Unit.Metres,
                    (in Source source) => source.EffectiveSole, (target, value) => target.EffectiveSole = value),
                Column.Create("ResolvedGoalTargetAnkle", Codecs.Vector, Unit.Metres,
                    (in Source source) => source.GoalTargetAnkle, (target, value) => target.GoalTargetAnkle = value),
                Column.Create("ResolvedGoalTargetRotation", Codecs.Rotation, Unit.Unitless,
                    (in Source source) => source.GoalTargetRotation, (target, value) => target.GoalTargetRotation = value),
                Column.Create("ResolvedEffectiveAnkle", Codecs.Vector, Unit.Metres,
                    (in Source source) => source.EffectiveAnkle, (target, value) => target.EffectiveAnkle = value),
                Column.Create("ResolvedEffectiveRotation", Codecs.Rotation, Unit.Unitless,
                    (in Source source) => source.EffectiveRotation, (target, value) => target.EffectiveRotation = value),
                Column.Create("ResolvedEffectiveHeel", Codecs.Vector, Unit.Metres,
                    (in Source source) => source.EffectiveHeel, (target, value) => target.EffectiveHeel = value),
                Column.Create("ResolvedEffectiveToe", Codecs.Vector, Unit.Metres,
                    (in Source source) => source.EffectiveToe, (target, value) => target.EffectiveToe = value),
                Column.Create("ResolvedEffectiveSoleFromContacts", Codecs.Vector, Unit.Metres,
                    (in Source source) => source.EffectiveSoleFromContacts, (target, value) => target.EffectiveSoleFromContacts = value),
                Column.Create("ResolvedSourceSoleForward", Codecs.Vector, Unit.Direction,
                    (in Source source) => source.SourceSoleForward, (target, value) => target.SourceSoleForward = value),
                Column.Create("ResolvedSourceSoleFrameLocalRotation", Codecs.Rotation, Unit.Unitless,
                    (in Source source) => source.SourceSoleFrameLocalRotation, (target, value) => target.SourceSoleFrameLocalRotation = value),
                Column.Create("ResolvedGoalTargetCorrection", Codecs.Vector, Unit.Metres,
                    (in Source source) => source.GoalTargetCorrection, (target, value) => target.GoalTargetCorrection = value),
                Column.Create("ResolvedEffectiveSoleCorrection", Codecs.Vector, Unit.Metres,
                    (in Source source) => source.EffectiveSoleCorrection, (target, value) => target.EffectiveSoleCorrection = value),
                Column.Create("ResolvedPositionWeight", Codecs.Float32, Unit.Unitless,
                    (in Source source) => source.PositionWeight, (target, value) => target.PositionWeight = value),
                Column.Create("ResolvedRotationWeight", Codecs.Float32, Unit.Unitless,
                    (in Source source) => source.RotationWeight, (target, value) => target.RotationWeight = value),
                Column.Create("ResolvedSupportTargetAvailable", Codecs.Boolean, Unit.None,
                    (in Source source) => source.SupportTarget.Available, (target, value) => target.SupportTarget.Available = value),
                Column.Create("ResolvedSupportTargetFrameSequence", Codecs.UInt64, Unit.Frame,
                    (in Source source) => source.SupportTarget.FrameSequence, (target, value) => target.SupportTarget.Frame = value),
                Column.Create("ResolvedSupportTargetCompletionIdentity", Codecs.UInt64, Unit.Identity,
                    (in Source source) => source.SupportTarget.CompletionIdentity, (target, value) => target.SupportTarget.Completion = value),
                Column.Create("ResolvedSupportTargetSide", Codecs.Text, Unit.Category,
                    (in Source source) => source.SupportTarget.Side.ToString(), (target, value) => target.SupportTarget.Side = value),
                Column.Create("ResolvedSupportTargetPosition", Codecs.Vector, Unit.Metres,
                    (in Source source) => source.SupportTarget.Position, (target, value) => target.SupportTarget.Position = value, "ResolvedSupportTargetAvailable"),
                Column.Create("ResolvedSupportTargetNormal", Codecs.Vector, Unit.Direction,
                    (in Source source) => source.SupportTarget.SupportNormal, (target, value) => target.SupportTarget.Normal = value, "ResolvedSupportTargetAvailable"),
                Column.Create("ResolvedSupportTargetSurfaceIdentity", Codecs.Int32, Unit.Identity,
                    (in Source source) => source.SupportTarget.SurfaceIdentity, (target, value) => target.SupportTarget.Surface = value, "ResolvedSupportTargetAvailable"),
                Column.Create("ResolvedSupportTargetWorldRevision", Codecs.UInt64, Unit.Identity,
                    (in Source source) => source.SupportTarget.WorldRevision, (target, value) => target.SupportTarget.WorldRevision = value, "ResolvedSupportTargetAvailable"),
                Column.Create("ResolvedSupportTargetKind", Codecs.Text, Unit.Category,
                    (in Source source) => source.SupportTarget.Kind.ToString(), (target, value) => target.SupportTarget.Kind = value, "ResolvedSupportTargetAvailable"),
                Column.Create("ResolvedSupportTargetPositionSource", Codecs.Text, Unit.Category,
                    (in Source source) => source.SupportTarget.PositionSource.ToString(), (target, value) => target.SupportTarget.PositionSource = value, "ResolvedSupportTargetAvailable"),
                Column.Create("ResolvedSupportTargetPositionFrameSequence", Codecs.UInt64, Unit.Frame,
                    (in Source source) => source.SupportTarget.PositionFrameSequence, (target, value) => target.SupportTarget.PositionFrame = value, "ResolvedSupportTargetAvailable"),
                Column.Create("ResolvedSupportTargetPositionCompletionIdentity", Codecs.UInt64, Unit.Identity,
                    (in Source source) => source.SupportTarget.PositionCompletionIdentity, (target, value) => target.SupportTarget.PositionCompletion = value, "ResolvedSupportTargetAvailable"),
                Column.Create("ResolvedSupportTargetPositionEventIdentity", Codecs.UInt64, Unit.Identity,
                    (in Source source) => source.SupportTarget.PositionEventIdentity, (target, value) => target.SupportTarget.PositionEvent = value, "ResolvedSupportTargetAvailable"),
                Column.Create("ResolvedSupportTargetPositionPathIdentity", Codecs.UInt64, Unit.Identity,
                    (in Source source) => source.SupportTarget.PositionPathIdentity, (target, value) => target.SupportTarget.PositionPath = value, "ResolvedSupportTargetAvailable"),
                Column.Create("ResolvedSupportTargetNormalSource", Codecs.Text, Unit.Category,
                    (in Source source) => source.SupportTarget.NormalSource.ToString(), (target, value) => target.SupportTarget.NormalSource = value, "ResolvedSupportTargetAvailable"),
                Column.Create("ResolvedSupportTargetNormalFrameSequence", Codecs.UInt64, Unit.Frame,
                    (in Source source) => source.SupportTarget.NormalFrameSequence, (target, value) => target.SupportTarget.NormalFrame = value, "ResolvedSupportTargetAvailable"),
                Column.Create("ResolvedSupportTargetNormalCompletionIdentity", Codecs.UInt64, Unit.Identity,
                    (in Source source) => source.SupportTarget.NormalCompletionIdentity, (target, value) => target.SupportTarget.NormalCompletion = value, "ResolvedSupportTargetAvailable"),
                Column.Create("ResolvedSupportTargetNormalEventIdentity", Codecs.UInt64, Unit.Identity,
                    (in Source source) => source.SupportTarget.NormalEventIdentity, (target, value) => target.SupportTarget.NormalEvent = value, "ResolvedSupportTargetAvailable"),
                Column.Create("ResolvedContactAvailable", Codecs.Boolean, Unit.None,
                    (in Source source) => source.ContactAvailable, (target, value) => target.ContactAvailable = value),
                Column.Create("ResolvedContactEventIdentity", Codecs.UInt64, Unit.Identity,
                    (in Source source) => source.ContactEventIdentity, (target, value) => target.ContactEventIdentity = value),
                Column.Create("ResolvedContactPoint", Codecs.Vector, Unit.Metres,
                    (in Source source) => source.ContactPoint, (target, value) => target.ContactPoint = value, "ResolvedContactAvailable"),
                Column.Create("ResolvedContactOwnership", Codecs.Float32, Unit.Unitless,
                    (in Source source) => source.ContactOwnership, (target, value) => target.ContactOwnership = value),
                Column.Create("ResolvedSupportEligibility", Codecs.Text, Unit.Category,
                    (in Source source) => source.SupportEligibility.ToString(), (target, value) => target.SupportEligibility = value),
                Column.Create("ResolvedSupportWeight", Codecs.Float32, Unit.Unitless,
                    (in Source source) => source.SupportWeight, (target, value) => target.SupportWeight = value),
                Column.Create("ResolvedSupportIntentWeight", Codecs.Float32, Unit.Unitless,
                    (in Source source) => source.SupportIntentWeight, (target, value) => target.SupportIntentWeight = value),
                Column.Create("ResolvedSupportHorizontalError", Codecs.Float32, Unit.Metres,
                    (in Source source) => source.SupportHorizontalError, (target, value) => target.SupportHorizontalError = value),
                Column.Create("ResolvedSupportEventIdentity", Codecs.UInt64, Unit.Identity,
                    (in Source source) => source.SupportEventIdentity, (target, value) => target.SupportEventIdentity = value),
                Column.Create("ResolvedPelvisReachAvailable", Codecs.Boolean, Unit.None,
                    (in Source source) => source.PelvisReachAvailable, (target, value) => target.PelvisReachAvailable = value),
                Column.Create("ResolvedPelvisReachEventIdentity", Codecs.UInt64, Unit.Identity,
                    (in Source source) => source.PelvisReachEventIdentity, (target, value) => target.PelvisReachEventIdentity = value),
                Column.Create("ResolvedPelvisReachPoint", Codecs.Vector, Unit.Metres,
                    (in Source source) => source.PelvisReachPoint, (target, value) => target.PelvisReachPoint = value, "ResolvedPelvisReachAvailable"),
                Column.Create("ResolvedLandingReachAvailable", Codecs.Boolean, Unit.None,
                    (in Source source) => source.LandingReachAvailable, (target, value) => target.LandingReachAvailable = value),
                Column.Create("ResolvedLandingReachEventIdentity", Codecs.UInt64, Unit.Identity,
                    (in Source source) => source.LandingReachEventIdentity, (target, value) => target.LandingReachEventIdentity = value),
                Column.Create("ResolvedLandingReachHip", Codecs.Vector, Unit.Metres,
                    (in Source source) => source.LandingReachHip, (target, value) => target.LandingReachHip = value, "ResolvedLandingReachAvailable"),
                Column.Create("ResolvedLandingReachTargetAnkle", Codecs.Vector, Unit.Metres,
                    (in Source source) => source.LandingReachTargetAnkle, (target, value) => target.LandingReachTargetAnkle = value, "ResolvedLandingReachAvailable"),
                Column.Create("ResolvedLandingReachLegLength", Codecs.Float32, Unit.Metres,
                    (in Source source) => source.LandingReachLegLength, (target, value) => target.LandingReachLegLength = value, "ResolvedLandingReachAvailable"),
                Column.Create("ResolvedLandingReachMinimumCompressionReserve", Codecs.Float32, Unit.Metres,
                    (in Source source) => source.LandingReachMinimumCompressionReserve, (target, value) => target.LandingReachMinimumCompressionReserve = value, "ResolvedLandingReachAvailable"),
                });
    }
}
