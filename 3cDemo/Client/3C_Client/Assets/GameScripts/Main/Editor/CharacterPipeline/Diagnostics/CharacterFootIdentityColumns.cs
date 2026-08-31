using System;
using System.Globalization;
using ThirdPersonCharacter.Pipeline.Presentation;
using UnityEngine;
using Source = ThirdPersonCharacter.Pipeline.Editor.CharacterFootIdentityCsvSource;
using Column = ThirdPersonCharacter.Pipeline.Editor.CharacterFootCsvColumn<ThirdPersonCharacter.Pipeline.Editor.CharacterFootIdentityCsvSource, ThirdPersonCharacter.Pipeline.Editor.CharacterFootIdentitySample>;
using Codecs = ThirdPersonCharacter.Pipeline.Editor.CharacterFootCsvCodecs;
using Unit = ThirdPersonCharacter.Pipeline.Editor.CharacterFootCsvUnit;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    internal readonly struct CharacterFootIdentityCsvSource
    {
        internal CharacterFootIdentityCsvSource(
            string sampleIdentity, string sampleStartedUtc,
            string programIdentity, string projectionRevision,
            string poseGraphId, string poseGraphRevision, string posePlanHash,
            in CharacterFootLandingPredictionDiagnostics frame,
            string targetRuntimeInstanceId, int targetHostInstanceId,
            in CharacterFootLandingPredictionFootDiagnostics foot,
            in CharacterFootStepCandidateSelectionDiagnostics selection)
        {
            SampleIdentity = sampleIdentity;
            SampleStartedUtc = sampleStartedUtc;
            ProgramIdentity = programIdentity;
            ProjectionRevision = projectionRevision;
            PoseGraphId = poseGraphId;
            PoseGraphRevision = poseGraphRevision;
            PosePlanHash = posePlanHash;
            Frame = frame;
            TargetRuntimeInstanceId = targetRuntimeInstanceId;
            TargetHostInstanceId = targetHostInstanceId;
            Foot = foot;
            Selection = selection;
        }
        internal string SampleIdentity { get; }
        internal string SampleStartedUtc { get; }
        internal string ProgramIdentity { get; }
        internal string ProjectionRevision { get; }
        internal string PoseGraphId { get; }
        internal string PoseGraphRevision { get; }
        internal string PosePlanHash { get; }
        internal CharacterFootLandingPredictionDiagnostics Frame { get; }
        internal string TargetRuntimeInstanceId { get; }
        internal int TargetHostInstanceId { get; }
        internal CharacterFootLandingPredictionFootDiagnostics Foot { get; }
        internal CharacterFootStepCandidateSelectionDiagnostics Selection { get; }
    }

    internal sealed class CharacterFootIdentitySample
    {
        internal string SampleIdentity;
        internal string SampleStartedUtc;
        internal string ProgramIdentity;
        internal string ProjectionRevision;
        internal string PoseGraphId;
        internal string PoseGraphRevision;
        internal string PosePlanHash;
        internal int FrameSequence;
        internal ulong CompletionIdentity;
        internal string TargetRuntimeInstanceId;
        internal int TargetHostInstanceId;
        internal int RootInstanceId;
        internal string ProfileId;
        internal string ProfileRevision;
        internal string Side;
        internal string State;
        internal string RejectReason;
        internal string StepSource;
        internal ulong LandingEventIdentity;
        internal ulong TrajectoryGeneration;
        internal float LandingConfidence;
        internal float TimeToLandingSeconds;
        internal string NextLandingTrackingState;
        internal ulong NextLandingTrackingEventIdentity;
        internal bool VerifiedLastLandingAvailable;
        internal ulong VerifiedLastLandingEventIdentity;
        internal string PlantTargetState;
        internal bool PlantTargetAvailable;
        internal ulong PlantTargetEventIdentity;
        internal int PlantTargetSurfaceIdentity;
        internal Vector3 PlantTargetPoint;
        internal Vector3 PlantTargetNormal;
        internal ulong PlantTargetTrajectoryGeneration;
        internal string PlantTargetFutureBodySource;
        internal bool PlantTargetUpdated;
        internal bool PlantVerificationAttempted;
        internal bool PlantVerificationUnavailable;
        internal bool ApproachPlantTargetPrepared;
        internal float StepSelectionMaximumPredictionTimeSeconds;
        internal ulong StepSelectionLastLandingEventIdentity;
        internal string SelectedStepSource;
        internal ulong SelectedLandingEventIdentity;
    }

    internal static class CharacterFootIdentityColumns
    {
        internal static readonly CharacterFootCsvGroup<CharacterFootIdentityCsvSource, CharacterFootIdentitySample> Schema =
            new CharacterFootCsvGroup<CharacterFootIdentityCsvSource, CharacterFootIdentitySample>(
                "Identity", () => new CharacterFootIdentitySample(),
                new CharacterFootCsvColumn<CharacterFootIdentityCsvSource, CharacterFootIdentitySample>[]
                {
                    Column.Create("SampleIdentity", Codecs.Text, Unit.Identity,
                        (in Source source) => source.SampleIdentity, (target, value) => target.SampleIdentity = value),
                    Column.Create("SampleStartedUtc", Codecs.Text, Unit.Category,
                        (in Source source) => source.SampleStartedUtc, (target, value) => target.SampleStartedUtc = value),
                    Column.Create("ProgramIdentity", Codecs.Text, Unit.Identity,
                        (in Source source) => source.ProgramIdentity, (target, value) => target.ProgramIdentity = value),
                    Column.Create("ProjectionRevision", Codecs.Text, Unit.Identity,
                        (in Source source) => source.ProjectionRevision, (target, value) => target.ProjectionRevision = value),
                    Column.Create("PoseGraphId", Codecs.Text, Unit.Identity,
                        (in Source source) => source.PoseGraphId, (target, value) => target.PoseGraphId = value),
                    Column.Create("PoseGraphRevision", Codecs.Text, Unit.Identity,
                        (in Source source) => source.PoseGraphRevision, (target, value) => target.PoseGraphRevision = value),
                    Column.Create("PosePlanHash", Codecs.Text, Unit.Identity,
                        (in Source source) => source.PosePlanHash, (target, value) => target.PosePlanHash = value),
                    Column.Create("FrameSequence", Codecs.Int32, Unit.Frame,
                        (in Source source) => checked((int)source.Frame.FrameSequence), (target, value) => target.FrameSequence = value),
                    Column.Create("CompletionIdentity", Codecs.UInt64, Unit.Identity,
                        (in Source source) => source.Frame.CompletionIdentity, (target, value) => target.CompletionIdentity = value),
                    Column.Create("TargetRuntimeInstanceId", Codecs.Text, Unit.Identity,
                        (in Source source) => source.TargetRuntimeInstanceId, (target, value) => target.TargetRuntimeInstanceId = value),
                    Column.Create("TargetHostInstanceId", Codecs.Int32, Unit.Identity,
                        (in Source source) => source.TargetHostInstanceId, (target, value) => target.TargetHostInstanceId = value),
                    Column.Create("RootInstanceId", Codecs.Int32, Unit.Identity,
                        (in Source source) => source.Frame.RootInstanceId, (target, value) => target.RootInstanceId = value),
                    Column.Create("FootProfileId", Codecs.Text, Unit.Identity,
                        (in Source source) => source.Frame.ProfileId, (target, value) => target.ProfileId = value),
                    Column.Create("FootProfileRevision", Codecs.Text, Unit.Identity,
                        (in Source source) => source.Frame.ProfileRevision, (target, value) => target.ProfileRevision = value),
                    Column.Create("Side", Codecs.Text, Unit.Category,
                        (in Source source) => source.Foot.Side.ToString(), (target, value) => target.Side = value),
                    Column.Create("State", Codecs.Text, Unit.Category,
                        (in Source source) => source.Foot.State.ToString(), (target, value) => target.State = value),
                    Column.Create("RejectReason", Codecs.Text, Unit.Category,
                        (in Source source) => source.Foot.RejectReason.ToString(), (target, value) => target.RejectReason = value),
                    Column.Create("StepSource", Codecs.Text, Unit.Category,
                        (in Source source) => source.Foot.StepSource.ToString(), (target, value) => target.StepSource = value),
                    Column.Create("LandingEventIdentity", Codecs.UInt64, Unit.Identity,
                        (in Source source) => source.Foot.LandingEventIdentity, (target, value) => target.LandingEventIdentity = value),
                    Column.Create("TrajectoryGeneration", Codecs.UInt64, Unit.Identity,
                        (in Source source) => source.Foot.TrajectoryGeneration, (target, value) => target.TrajectoryGeneration = value),
                    Column.Create("LandingConfidence", Codecs.Float32, Unit.Unitless,
                        (in Source source) => source.Foot.LandingConfidence, (target, value) => target.LandingConfidence = value),
                    Column.Create("TimeToLandingSeconds", Codecs.Float32, Unit.Seconds,
                        (in Source source) => source.Foot.TimeToLandingSeconds, (target, value) => target.TimeToLandingSeconds = value),
                    Column.Create("NextLandingTrackingState", Codecs.Text, Unit.Category,
                        (in Source source) => source.Foot.NextLandingTrackingState, (target, value) => target.NextLandingTrackingState = value),
                    Column.Create("NextLandingTrackingEventIdentity", Codecs.UInt64, Unit.Identity,
                        (in Source source) => source.Foot.NextLandingTrackingEventIdentity, (target, value) => target.NextLandingTrackingEventIdentity = value),
                    Column.Create("VerifiedLastLandingAvailable", Codecs.Boolean, Unit.None,
                        (in Source source) => source.Foot.VerifiedLastLandingAvailable, (target, value) => target.VerifiedLastLandingAvailable = value),
                    Column.Create("VerifiedLastLandingEventIdentity", Codecs.UInt64, Unit.Identity,
                        (in Source source) => source.Foot.VerifiedLastLandingEventIdentity, (target, value) => target.VerifiedLastLandingEventIdentity = value),
                    Column.Create("PlantTargetState", Codecs.Text, Unit.Category,
                        (in Source source) => source.Foot.PlantTargetState, (target, value) => target.PlantTargetState = value),
                    Column.Create("PlantTargetAvailable", Codecs.Boolean, Unit.None,
                        (in Source source) => source.Foot.PlantTargetAvailable, (target, value) => target.PlantTargetAvailable = value),
                    Column.Create("PlantTargetEventIdentity", Codecs.UInt64, Unit.Identity,
                        (in Source source) => source.Foot.PlantTargetEventIdentity, (target, value) => target.PlantTargetEventIdentity = value, "PlantTargetAvailable"),
                    Column.Create("PlantTargetSurfaceIdentity", Codecs.Int32, Unit.Count,
                        (in Source source) => source.Foot.PlantTargetSurfaceIdentity, (target, value) => target.PlantTargetSurfaceIdentity = value, "PlantTargetAvailable"),
                    Column.Create("PlantTargetPoint", Codecs.Vector, Unit.Metres,
                        (in Source source) => source.Foot.PlantTargetPoint, (target, value) => target.PlantTargetPoint = value, "PlantTargetAvailable"),
                    Column.Create("PlantTargetNormal", Codecs.Vector, Unit.Direction,
                        (in Source source) => source.Foot.PlantTargetNormal, (target, value) => target.PlantTargetNormal = value, "PlantTargetAvailable"),
                    Column.Create("PlantTargetTrajectoryGeneration", Codecs.UInt64, Unit.Identity,
                        (in Source source) => source.Foot.PlantTargetTrajectoryGeneration, (target, value) => target.PlantTargetTrajectoryGeneration = value, "PlantTargetAvailable"),
                    Column.Create("PlantTargetFutureBodyTranslationSourceIdentity", Codecs.Text, Unit.Identity,
                        (in Source source) => source.Foot.PlantTargetFutureBodyTranslationSourceIdentity, (target, value) => target.PlantTargetFutureBodySource = value, "PlantTargetAvailable"),
                    Column.Create("PlantTargetUpdated", Codecs.Boolean, Unit.None,
                        (in Source source) => source.Foot.PlantTargetUpdated, (target, value) => target.PlantTargetUpdated = value),
                    Column.Create("PlantVerificationAttempted", Codecs.Boolean, Unit.None,
                        (in Source source) => source.Foot.PlantVerificationAttempted, (target, value) => target.PlantVerificationAttempted = value),
                    Column.Create("PlantVerificationUnavailable", Codecs.Boolean, Unit.None,
                        (in Source source) => source.Foot.PlantVerificationUnavailable, (target, value) => target.PlantVerificationUnavailable = value),
                    Column.Create("ApproachPlantTargetPrepared", Codecs.Boolean, Unit.None,
                        (in Source source) => source.Foot.ApproachPlantTargetPrepared, (target, value) => target.ApproachPlantTargetPrepared = value),
                    Column.Create("StepSelectionMaximumPredictionTimeSeconds", Codecs.Float32, Unit.Seconds,
                        (in Source source) => source.Selection.MaximumPredictionTimeSeconds, (target, value) => target.StepSelectionMaximumPredictionTimeSeconds = value),
                    Column.Create("StepSelectionLastLandingEventIdentity", Codecs.UInt64, Unit.Identity,
                        (in Source source) => source.Selection.LastLandingEventIdentity, (target, value) => target.StepSelectionLastLandingEventIdentity = value),
                    Column.Create("SelectedStepSource", Codecs.Text, Unit.Category,
                        (in Source source) => source.Selection.SelectedSource.ToString(), (target, value) => target.SelectedStepSource = value),
                    Column.Create("SelectedLandingEventIdentity", Codecs.UInt64, Unit.Identity,
                        (in Source source) => source.Selection.SelectedLandingEventIdentity, (target, value) => target.SelectedLandingEventIdentity = value),
                });
    }

    internal sealed class CharacterFootRootLandingSample
    {
        internal UnityEngine.Vector3 RootLocalLanding;
    }

    internal static class CharacterFootRootLandingColumns
    {
        internal static readonly CharacterFootCsvGroup<UnityEngine.Vector3, CharacterFootRootLandingSample> Schema =
            new CharacterFootCsvGroup<UnityEngine.Vector3, CharacterFootRootLandingSample>(
                "RootLanding", () => new CharacterFootRootLandingSample(),
                new[]
                {
                    CharacterFootCsvColumn<UnityEngine.Vector3, CharacterFootRootLandingSample>.Create(
                        "RootLocalLanding", CharacterFootCsvCodecs.Vector, CharacterFootCsvUnit.Metres,
                        (in UnityEngine.Vector3 source) => source, (target, value) => target.RootLocalLanding = value)
                });
    }

    internal sealed class CharacterFootActionSample
    {
        internal bool Grounded;
        internal float HorizontalSpeed;
        internal ulong LeftInstanceIdentity;
        internal float LeftFootWeight;
        internal ulong RightInstanceIdentity;
        internal float RightFootWeight;

        internal ulong InstanceIdentity(string side) =>
            side == "Left" ? LeftInstanceIdentity : RightInstanceIdentity;
        internal float FootWeight(string side) =>
            side == "Left" ? LeftFootWeight : RightFootWeight;
    }

    internal static class CharacterFootActionColumns
    {
        internal static readonly CharacterFootCsvGroup<CharacterFootLandingPredictionInputDiagnostics, CharacterFootActionSample> Schema =
            new CharacterFootCsvGroup<CharacterFootLandingPredictionInputDiagnostics, CharacterFootActionSample>(
                "Action", () => new CharacterFootActionSample(),
                new[]
                {
                    CharacterFootCsvColumn<CharacterFootLandingPredictionInputDiagnostics, CharacterFootActionSample>.Create(
                        "Grounded", CharacterFootCsvCodecs.Boolean, CharacterFootCsvUnit.None,
                        (in CharacterFootLandingPredictionInputDiagnostics source) => source.Grounded, (target, value) => target.Grounded = value),
                    CharacterFootCsvColumn<CharacterFootLandingPredictionInputDiagnostics, CharacterFootActionSample>.Create(
                        "HorizontalSpeed", CharacterFootCsvCodecs.Float32, CharacterFootCsvUnit.MetresPerSecond,
                        (in CharacterFootLandingPredictionInputDiagnostics source) => source.HorizontalSpeed, (target, value) => target.HorizontalSpeed = value),
                    CharacterFootCsvColumn<CharacterFootLandingPredictionInputDiagnostics, CharacterFootActionSample>.Create(
                        "LeftActionInstanceIdentity", CharacterFootCsvCodecs.UInt64, CharacterFootCsvUnit.Identity,
                        (in CharacterFootLandingPredictionInputDiagnostics source) => source.LeftActionInstanceIdentity, (target, value) => target.LeftInstanceIdentity = value),
                    CharacterFootCsvColumn<CharacterFootLandingPredictionInputDiagnostics, CharacterFootActionSample>.Create(
                        "LeftActionFootWeight", CharacterFootCsvCodecs.Float32, CharacterFootCsvUnit.Unitless,
                        (in CharacterFootLandingPredictionInputDiagnostics source) => source.LeftActionFootWeight, (target, value) => target.LeftFootWeight = value),
                    CharacterFootCsvColumn<CharacterFootLandingPredictionInputDiagnostics, CharacterFootActionSample>.Create(
                        "RightActionInstanceIdentity", CharacterFootCsvCodecs.UInt64, CharacterFootCsvUnit.Identity,
                        (in CharacterFootLandingPredictionInputDiagnostics source) => source.RightActionInstanceIdentity, (target, value) => target.RightInstanceIdentity = value),
                    CharacterFootCsvColumn<CharacterFootLandingPredictionInputDiagnostics, CharacterFootActionSample>.Create(
                        "RightActionFootWeight", CharacterFootCsvCodecs.Float32, CharacterFootCsvUnit.Unitless,
                        (in CharacterFootLandingPredictionInputDiagnostics source) => source.RightActionFootWeight, (target, value) => target.RightFootWeight = value)
                });
    }
}
