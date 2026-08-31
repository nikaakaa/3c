using UnityEngine;
using ThirdPersonCharacter.Pipeline.Presentation;
using static ThirdPersonCharacter.Pipeline.Editor.CharacterFootCsvValues;
using Source = ThirdPersonCharacter.Pipeline.Editor.CharacterFootPelvisCsvSource;
using Column = ThirdPersonCharacter.Pipeline.Editor.CharacterFootCsvColumn<ThirdPersonCharacter.Pipeline.Editor.CharacterFootPelvisCsvSource, ThirdPersonCharacter.Pipeline.Editor.CharacterFootPelvisSample>;
using Codecs = ThirdPersonCharacter.Pipeline.Editor.CharacterFootCsvCodecs;
using Unit = ThirdPersonCharacter.Pipeline.Editor.CharacterFootCsvUnit;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    internal readonly struct CharacterFootPelvisCsvSource
    {
        internal CharacterFootPelvisCsvSource(
            in CharacterFootStrideHipsDiagnostics stride, Vector3 finalGoal,
            Vector3 physicalComponent, Vector3 physicalWorld, bool residualAvailable, float residual)
        {
            Stride = stride;
            FinalGoal = finalGoal;
            PhysicalComponent = physicalComponent;
            PhysicalWorld = physicalWorld;
            ResidualAvailable = residualAvailable;
            Residual = residual;
        }

        internal CharacterFootStrideHipsDiagnostics Stride { get; }
        internal Vector3 FinalGoal { get; }
        internal Vector3 PhysicalComponent { get; }
        internal Vector3 PhysicalWorld { get; }
        internal bool ResidualAvailable { get; }
        internal float Residual { get; }
    }

    internal sealed class CharacterFootPelvisSample
    {
        internal CharacterFootPelvisObservationSample Observation = new CharacterFootPelvisObservationSample();
        internal CharacterFootPelvisPostureSample Posture = new CharacterFootPelvisPostureSample();
        internal CharacterFootPelvisReachSample Reach = new CharacterFootPelvisReachSample();
        internal CharacterFootPelvisResponseSample Response = new CharacterFootPelvisResponseSample();
        internal string State;
        internal string SupportSide;
        internal string SwingSide;
        internal float Progress;
        internal CharacterFootStrideSlope Slope;
        internal CharacterFootStrideRejectReason RejectReason;
        internal Vector3 Start;
        internal Vector3 End;
        internal Vector3 SampledGround;
        internal Vector3 Delta;
        internal Vector3 FinalGoal;
        internal Vector3 PhysicalComponent;
        internal CharacterFootPelvisHeightTargetSample HeightTarget = new CharacterFootPelvisHeightTargetSample();
        internal bool SameAs(CharacterFootPelvisSample other) =>
            Observation.SameAs(other.Observation) && Posture.SameAs(other.Posture) &&
            Reach.SameAs(other.Reach) && Response.SameAs(other.Response);
    }

    internal sealed class CharacterFootPelvisObservationSample
    {
        internal bool PoseInputAvailable;
        internal Vector3 PoseRootWorldPosition;
        internal Vector3 AnimatedWorldPosition;
        internal Vector3 AnimatedComponentPosition;
        internal Vector3 PhysicalWorldPosition;
        internal bool GoalResidualAvailable;
        internal float GoalResidual;
        internal bool SameAs(CharacterFootPelvisObservationSample other) =>
            PoseInputAvailable == other.PoseInputAvailable &&
            PoseRootWorldPosition.Equals(other.PoseRootWorldPosition) &&
            AnimatedWorldPosition.Equals(other.AnimatedWorldPosition) &&
            AnimatedComponentPosition.Equals(other.AnimatedComponentPosition) &&
            PhysicalWorldPosition.Equals(other.PhysicalWorldPosition) &&
            GoalResidualAvailable == other.GoalResidualAvailable && GoalResidual.Equals(other.GoalResidual);
    }

    internal sealed class CharacterFootPelvisPostureSample
    {
        internal bool Evaluated;
        internal bool Available;
        internal Vector3 Hip;
        internal Vector3 AnimatedAnkle;
        internal Vector3 TargetAnkle;
        internal float LegLength;
        internal float CompressionReserve;
        internal float UsableLegLength;
        internal float MinimumAlongUp;
        internal float MaximumAlongUp;
        internal float OffsetAlongUp;
        internal bool TargetAdjusted;
        internal bool SameAs(CharacterFootPelvisPostureSample other) =>
            Evaluated == other.Evaluated &&
            Available == other.Available &&
            Hip.Equals(other.Hip) &&
            AnimatedAnkle.Equals(other.AnimatedAnkle) &&
            TargetAnkle.Equals(other.TargetAnkle) &&
            LegLength == other.LegLength &&
            CompressionReserve == other.CompressionReserve &&
            UsableLegLength == other.UsableLegLength &&
            MinimumAlongUp == other.MinimumAlongUp &&
            MaximumAlongUp == other.MaximumAlongUp &&
            OffsetAlongUp == other.OffsetAlongUp &&
            TargetAdjusted == other.TargetAdjusted;
    }

    internal sealed class CharacterFootPelvisLegSample
    {
        internal CharacterFootPelvisLegReachRole Role;
        internal CharacterFootPelvisLegReachStatus Status;
        internal ulong EventIdentity;
        internal Vector3 Hip;
        internal Vector3 TargetAnkle;
        internal float LegLength;
        internal float MinimumCompressionReserve;
        internal float UsableLegLength;
        internal float MinimumAlongUp;
        internal float MaximumAlongUp;
        internal bool Requested;
        internal bool Available;
        internal bool FootTarget => (Role & CharacterFootPelvisLegReachRole.FootTarget) != 0;
        internal bool PrimarySupport => (Role & CharacterFootPelvisLegReachRole.PrimarySupport) != 0;
        internal bool SameAs(CharacterFootPelvisLegSample other) =>
            Role == other.Role &&
            Status == other.Status &&
            EventIdentity == other.EventIdentity &&
            Hip.Equals(other.Hip) &&
            TargetAnkle.Equals(other.TargetAnkle) &&
            LegLength == other.LegLength &&
            MinimumCompressionReserve == other.MinimumCompressionReserve &&
            UsableLegLength == other.UsableLegLength &&
            MinimumAlongUp == other.MinimumAlongUp &&
            MaximumAlongUp == other.MaximumAlongUp &&
            Requested == other.Requested &&
            Available == other.Available;
    }

    internal sealed class CharacterFootPelvisReachSample
    {
        internal Vector3 ComponentUp;
        internal CharacterFootPelvisReachStatus Status;
        internal bool IntersectionEvaluated;
        internal float IntersectionMinimumAlongUp;
        internal float IntersectionMaximumAlongUp;
        internal CharacterFootPelvisLegSample Left = new CharacterFootPelvisLegSample();
        internal CharacterFootPelvisLegSample Right = new CharacterFootPelvisLegSample();
        internal bool SameAs(CharacterFootPelvisReachSample other) =>
            ComponentUp.Equals(other.ComponentUp) &&
            Status == other.Status &&
            IntersectionEvaluated == other.IntersectionEvaluated &&
            IntersectionMinimumAlongUp == other.IntersectionMinimumAlongUp &&
            IntersectionMaximumAlongUp == other.IntersectionMaximumAlongUp &&
            Left.SameAs(other.Left) &&
            Right.SameAs(other.Right);
    }

    internal sealed class CharacterFootPelvisResponseSample
    {
        internal bool Evaluated;
        internal bool Completed;
        internal float IntegratedOutput;
        internal bool HadPreviousState;
        internal bool SupportChanged;
        internal bool VelocityReset;
        internal float PreviousTarget;
        internal float PreviousOutput;
        internal float PreviousVelocity;
        internal float Input;
        internal float InputVelocity;
        internal float Frequency;
        internal float Target;
        internal float Output;
        internal float Velocity;
        internal float PositionWeight;
        internal CharacterFootStrideSlope PreviousSlope;
        internal CharacterFootPelvisSpringHandoffReason Handoff;
        internal bool SameAs(CharacterFootPelvisResponseSample other) =>
            Evaluated == other.Evaluated &&
            Completed == other.Completed &&
            IntegratedOutput == other.IntegratedOutput &&
            HadPreviousState == other.HadPreviousState &&
            SupportChanged == other.SupportChanged &&
            VelocityReset == other.VelocityReset &&
            PreviousTarget == other.PreviousTarget &&
            PreviousOutput == other.PreviousOutput &&
            PreviousVelocity == other.PreviousVelocity &&
            Input == other.Input &&
            InputVelocity == other.InputVelocity &&
            Frequency == other.Frequency &&
            Target == other.Target &&
            Output == other.Output &&
            Velocity == other.Velocity &&
            PositionWeight == other.PositionWeight &&
            PreviousSlope == other.PreviousSlope &&
            Handoff == other.Handoff;
    }

    internal sealed class CharacterFootPelvisHeightTargetSample
    {
        internal bool Available;
        internal Vector3 ComponentUp;
        internal Vector3 LeftAnimatedSole;
        internal Vector3 RightAnimatedSole;
        internal Vector3 LeftTargetSole;
        internal Vector3 RightTargetSole;
        internal float AnimatedMinimumAlongUp;
        internal float TargetMinimumAlongUp;
        internal float RequestedOffsetAlongUp;



        internal bool SameAs(CharacterFootPelvisHeightTargetSample other) =>
            Available == other.Available && ComponentUp.Equals(other.ComponentUp) &&
            LeftAnimatedSole.Equals(other.LeftAnimatedSole) && RightAnimatedSole.Equals(other.RightAnimatedSole) &&
            LeftTargetSole.Equals(other.LeftTargetSole) && RightTargetSole.Equals(other.RightTargetSole) &&
            AnimatedMinimumAlongUp == other.AnimatedMinimumAlongUp &&
            TargetMinimumAlongUp == other.TargetMinimumAlongUp && RequestedOffsetAlongUp == other.RequestedOffsetAlongUp;


    }

    internal static class CharacterFootPelvisColumns
    {
        internal static readonly CharacterFootCsvGroup<Source, CharacterFootPelvisSample> Schema =
            new CharacterFootCsvGroup<Source, CharacterFootPelvisSample>(
                "Pelvis", () => new CharacterFootPelvisSample(), new Column[]
                {
                    Column.Create("StrideState", Codecs.Text, Unit.Category,
                        (in Source source) => source.Stride.Core.State.ToString(), (target, value) => target.State = value),
                    Column.Create("StrideRejectReason", Codecs.Text, Unit.Category,
                        (in Source source) => source.Stride.Core.RejectReason.ToString(), (target, value) => target.RejectReason = ParseEnumValue<CharacterFootStrideRejectReason>(value, "StrideRejectReason")),
                    Column.Create("StrideSupportSide", Codecs.Text, Unit.Category,
                        (in Source source) => source.Stride.Core.SupportSide.ToString(), (target, value) => target.SupportSide = value),
                    Column.Create("StrideSwingSide", Codecs.Text, Unit.Category,
                        (in Source source) => source.Stride.Core.SwingSide.ToString(), (target, value) => target.SwingSide = value),
                    Column.Create("StrideProgress", Codecs.Float32, Unit.Unitless,
                        (in Source source) => source.Stride.Core.Progress, (target, value) => target.Progress = value),
                    Column.Create("StrideSlope", Codecs.Text, Unit.Category,
                        (in Source source) => source.Stride.Core.Slope.ToString(), (target, value) => target.Slope = ParseEnumValue<CharacterFootStrideSlope>(value, "StrideSlope")),
                    Column.Create("StrideStart", Codecs.Vector, Unit.Metres,
                        (in Source source) => source.Stride.Core.StrideStart, (target, value) => target.Start = value),
                    Column.Create("StrideEnd", Codecs.Vector, Unit.Metres,
                        (in Source source) => source.Stride.Core.StrideEnd, (target, value) => target.End = value),
                    Column.Create("StrideSampledGround", Codecs.Vector, Unit.Metres,
                        (in Source source) => source.Stride.Core.SampledGround, (target, value) => target.SampledGround = value),
                    Column.Create("PelvisPoseInputAvailable", Codecs.Boolean, Unit.None,
                        (in Source source) => source.Stride.Observation.PoseInputAvailable, (target, value) => target.Observation.PoseInputAvailable = value),
                    Column.Create("StridePoseRootPosition", Codecs.Vector, Unit.Metres,
                        (in Source source) => source.Stride.Observation.PoseRootPosition, (target, value) => target.Observation.PoseRootWorldPosition = value),
                    Column.Create("StrideAnimatedPelvis", Codecs.Vector, Unit.Metres,
                        (in Source source) => source.Stride.Observation.AnimatedPelvis, (target, value) => target.Observation.AnimatedWorldPosition = value),
                    Column.Create("StrideAnimatedPelvisComponentPosition", Codecs.Vector, Unit.Metres,
                        (in Source source) => source.Stride.Observation.AnimatedPelvisComponentPosition, (target, value) => target.Observation.AnimatedComponentPosition = value),
                    Column.Create("PelvisHeightTargetAvailable", Codecs.Boolean, Unit.None,
                        (in Source source) => source.Stride.HeightTarget.Available, (target, value) => target.HeightTarget.Available = value),
                    Column.Create("PelvisHeightTargetComponentUp", Codecs.Vector, Unit.Direction,
                        (in Source source) => source.Stride.HeightTarget.ComponentUp, (target, value) => target.HeightTarget.ComponentUp = value),
                    Column.Create("PelvisHeightTargetLeftAnimatedSole", Codecs.Vector, Unit.Metres,
                        (in Source source) => source.Stride.HeightTarget.LeftAnimatedSole, (target, value) => target.HeightTarget.LeftAnimatedSole = value),
                    Column.Create("PelvisHeightTargetRightAnimatedSole", Codecs.Vector, Unit.Metres,
                        (in Source source) => source.Stride.HeightTarget.RightAnimatedSole, (target, value) => target.HeightTarget.RightAnimatedSole = value),
                    Column.Create("PelvisHeightTargetLeftTargetSole", Codecs.Vector, Unit.Metres,
                        (in Source source) => source.Stride.HeightTarget.LeftTargetSole, (target, value) => target.HeightTarget.LeftTargetSole = value),
                    Column.Create("PelvisHeightTargetRightTargetSole", Codecs.Vector, Unit.Metres,
                        (in Source source) => source.Stride.HeightTarget.RightTargetSole, (target, value) => target.HeightTarget.RightTargetSole = value),
                    Column.Create("PelvisHeightTargetAnimatedMinimumAlongUp", Codecs.Float32, Unit.Metres,
                        (in Source source) => source.Stride.HeightTarget.AnimatedMinimumAlongUp, (target, value) => target.HeightTarget.AnimatedMinimumAlongUp = value),
                    Column.Create("PelvisHeightTargetMinimumAlongUp", Codecs.Float32, Unit.Metres,
                        (in Source source) => source.Stride.HeightTarget.MinimumAlongUp, (target, value) => target.HeightTarget.TargetMinimumAlongUp = value),
                    Column.Create("PelvisRequestedOffsetAlongUp", Codecs.Float32, Unit.Metres,
                        (in Source source) => source.Stride.HeightTarget.RequestedOffsetAlongUp, (target, value) => target.HeightTarget.RequestedOffsetAlongUp = value),
                    Column.Create("StrideHadPreviousState", Codecs.Boolean, Unit.None,
                        (in Source source) => source.Stride.Response.HadPreviousState, (target, value) => target.Response.HadPreviousState = value),
                    Column.Create("StrideSupportChanged", Codecs.Boolean, Unit.None,
                        (in Source source) => source.Stride.Response.SupportChanged, (target, value) => target.Response.SupportChanged = value),
                    Column.Create("StridePreviousSlope", Codecs.Text, Unit.Category,
                        (in Source source) => source.Stride.Response.PreviousSlope.ToString(), (target, value) => target.Response.PreviousSlope = ParseEnumValue<CharacterFootStrideSlope>(value, "StridePreviousSlope")),
                    Column.Create("StrideSpringHandoffReason", Codecs.Text, Unit.Category,
                        (in Source source) => source.Stride.Response.HandoffReason.ToString().Replace(", ", "|"), (target, value) => target.Response.Handoff = ParseFlagsValue<CharacterFootPelvisSpringHandoffReason>(value, "StrideSpringHandoffReason", '|')),
                    Column.Create("StrideSpringVelocityReset", Codecs.Boolean, Unit.None,
                        (in Source source) => source.Stride.Response.VelocityReset, (target, value) => target.Response.VelocityReset = value),
                    Column.Create("StridePreviousSpringTarget", Codecs.Float32, Unit.Metres,
                        (in Source source) => source.Stride.Response.PreviousTarget, (target, value) => target.Response.PreviousTarget = value),
                    Column.Create("StridePreviousSpringOutput", Codecs.Float32, Unit.Metres,
                        (in Source source) => source.Stride.Response.PreviousOutput, (target, value) => target.Response.PreviousOutput = value),
                    Column.Create("StridePreviousSpringVelocity", Codecs.Float32, Unit.MetresPerSecond,
                        (in Source source) => source.Stride.Response.PreviousVelocity, (target, value) => target.Response.PreviousVelocity = value),
                    Column.Create("StrideSpringInput", Codecs.Float32, Unit.Metres,
                        (in Source source) => source.Stride.Response.Input, (target, value) => target.Response.Input = value),
                    Column.Create("StrideSpringInputVelocity", Codecs.Float32, Unit.MetresPerSecond,
                        (in Source source) => source.Stride.Response.InputVelocity, (target, value) => target.Response.InputVelocity = value),
                    Column.Create("StrideSpringFrequency", Codecs.Float32, Unit.Hertz,
                        (in Source source) => source.Stride.Response.Frequency, (target, value) => target.Response.Frequency = value),
                    Column.Create("PelvisPosturePreferenceEvaluated", Codecs.Boolean, Unit.None,
                        (in Source source) => source.Stride.Posture.Evaluated, (target, value) => target.Posture.Evaluated = value),
                    Column.Create("PelvisPosturePreferenceAvailable", Codecs.Boolean, Unit.None,
                        (in Source source) => source.Stride.Posture.Available, (target, value) => target.Posture.Available = value),
                    Column.Create("PelvisPosturePreferenceHip", Codecs.Vector, Unit.Metres,
                        (in Source source) => source.Stride.Posture.Hip, (target, value) => target.Posture.Hip = value),
                    Column.Create("PelvisPosturePreferenceAnimatedAnkle", Codecs.Vector, Unit.Metres,
                        (in Source source) => source.Stride.Posture.AnimatedAnkle, (target, value) => target.Posture.AnimatedAnkle = value),
                    Column.Create("PelvisPosturePreferenceTargetAnkle", Codecs.Vector, Unit.Metres,
                        (in Source source) => source.Stride.Posture.TargetAnkle, (target, value) => target.Posture.TargetAnkle = value),
                    Column.Create("PelvisPosturePreferenceLegLength", Codecs.Float32, Unit.Metres,
                        (in Source source) => source.Stride.Posture.LegLength, (target, value) => target.Posture.LegLength = value),
                    Column.Create("PelvisPosturePreferenceCompressionReserve", Codecs.Float32, Unit.Metres,
                        (in Source source) => source.Stride.Posture.CompressionReserve, (target, value) => target.Posture.CompressionReserve = value),
                    Column.Create("PelvisPosturePreferenceUsableLegLength", Codecs.Float32, Unit.Metres,
                        (in Source source) => source.Stride.Posture.UsableLegLength, (target, value) => target.Posture.UsableLegLength = value),
                    Column.Create("PelvisPosturePreferenceMinimumAlongUp", Codecs.Float32, Unit.Metres,
                        (in Source source) => source.Stride.Posture.MinimumAlongUp, (target, value) => target.Posture.MinimumAlongUp = value),
                    Column.Create("PelvisPosturePreferenceMaximumAlongUp", Codecs.Float32, Unit.Metres,
                        (in Source source) => source.Stride.Posture.MaximumAlongUp, (target, value) => target.Posture.MaximumAlongUp = value),
                    Column.Create("PelvisPosturePreferenceOffsetAlongUp", Codecs.Float32, Unit.Metres,
                        (in Source source) => source.Stride.Posture.OffsetAlongUp, (target, value) => target.Posture.OffsetAlongUp = value),
                    Column.Create("PelvisPosturePreferenceTargetAdjusted", Codecs.Boolean, Unit.None,
                        (in Source source) => source.Stride.Posture.TargetAdjusted, (target, value) => target.Posture.TargetAdjusted = value),
                    Column.Create("PelvisReachComponentUp", Codecs.Vector, Unit.Direction,
                        (in Source source) => source.Stride.Reach.ComponentUp, (target, value) => target.Reach.ComponentUp = value),
                    Column.Create("PelvisReachStatus", Codecs.Text, Unit.Category,
                        (in Source source) => source.Stride.Reach.Status.ToString(), (target, value) => target.Reach.Status = ParseEnumValue<CharacterFootPelvisReachStatus>(value, "PelvisReachStatus")),
                    Column.Create("PelvisReachIntersectionEvaluated", Codecs.Boolean, Unit.None,
                        (in Source source) => source.Stride.Reach.IntersectionEvaluated, (target, value) => target.Reach.IntersectionEvaluated = value),
                    Column.Create("PelvisReachIntersectionMinimumAlongUp", Codecs.Float32, Unit.Metres,
                        (in Source source) => source.Stride.Reach.IntersectionMinimumAlongUp, (target, value) => target.Reach.IntersectionMinimumAlongUp = value),
                    Column.Create("PelvisReachIntersectionMaximumAlongUp", Codecs.Float32, Unit.Metres,
                        (in Source source) => source.Stride.Reach.IntersectionMaximumAlongUp, (target, value) => target.Reach.IntersectionMaximumAlongUp = value),
                    Column.Create("PelvisReachLeftRole", Codecs.Text, Unit.Category,
                        (in Source source) => source.Stride.Reach.Left.Role.ToString(), (target, value) => target.Reach.Left.Role = ParseFlagsValue<CharacterFootPelvisLegReachRole>(value, "PelvisReachLeftRole")),
                    Column.Create("PelvisReachLeftStatus", Codecs.Text, Unit.Category,
                        (in Source source) => source.Stride.Reach.Left.Status.ToString(), (target, value) => target.Reach.Left.Status = ParseEnumValue<CharacterFootPelvisLegReachStatus>(value, "PelvisReachLeftStatus")),
                    Column.Create("PelvisReachLeftEventIdentity", Codecs.UInt64, Unit.Identity,
                        (in Source source) => source.Stride.Reach.Left.EventIdentity, (target, value) => target.Reach.Left.EventIdentity = value),
                    Column.Create("PelvisReachLeftHip", Codecs.Vector, Unit.Metres,
                        (in Source source) => source.Stride.Reach.Left.Hip, (target, value) => target.Reach.Left.Hip = value),
                    Column.Create("PelvisReachLeftTargetAnkle", Codecs.Vector, Unit.Metres,
                        (in Source source) => source.Stride.Reach.Left.TargetAnkle, (target, value) => target.Reach.Left.TargetAnkle = value),
                    Column.Create("PelvisReachLeftLegLength", Codecs.Float32, Unit.Metres,
                        (in Source source) => source.Stride.Reach.Left.LegLength, (target, value) => target.Reach.Left.LegLength = value),
                    Column.Create("PelvisReachLeftMinimumCompressionReserve", Codecs.Float32, Unit.Metres,
                        (in Source source) => source.Stride.Reach.Left.MinimumCompressionReserve, (target, value) => target.Reach.Left.MinimumCompressionReserve = value),
                    Column.Create("PelvisReachLeftUsableLegLength", Codecs.Float32, Unit.Metres,
                        (in Source source) => source.Stride.Reach.Left.UsableLegLength, (target, value) => target.Reach.Left.UsableLegLength = value),
                    Column.Create("PelvisReachLeftMinimumAlongUp", Codecs.Float32, Unit.Metres,
                        (in Source source) => source.Stride.Reach.Left.MinimumAlongUp, (target, value) => target.Reach.Left.MinimumAlongUp = value),
                    Column.Create("PelvisReachLeftMaximumAlongUp", Codecs.Float32, Unit.Metres,
                        (in Source source) => source.Stride.Reach.Left.MaximumAlongUp, (target, value) => target.Reach.Left.MaximumAlongUp = value),
                    Column.Create("PelvisReachLeftRequested", Codecs.Boolean, Unit.None,
                        (in Source source) => source.Stride.Reach.Left.Requested, (target, value) => target.Reach.Left.Requested = value),
                    Column.Create("PelvisReachLeftAvailable", Codecs.Boolean, Unit.None,
                        (in Source source) => source.Stride.Reach.Left.Available, (target, value) => target.Reach.Left.Available = value),
                    Column.Create("PelvisReachRightRole", Codecs.Text, Unit.Category,
                        (in Source source) => source.Stride.Reach.Right.Role.ToString(), (target, value) => target.Reach.Right.Role = ParseFlagsValue<CharacterFootPelvisLegReachRole>(value, "PelvisReachRightRole")),
                    Column.Create("PelvisReachRightStatus", Codecs.Text, Unit.Category,
                        (in Source source) => source.Stride.Reach.Right.Status.ToString(), (target, value) => target.Reach.Right.Status = ParseEnumValue<CharacterFootPelvisLegReachStatus>(value, "PelvisReachRightStatus")),
                    Column.Create("PelvisReachRightEventIdentity", Codecs.UInt64, Unit.Identity,
                        (in Source source) => source.Stride.Reach.Right.EventIdentity, (target, value) => target.Reach.Right.EventIdentity = value),
                    Column.Create("PelvisReachRightHip", Codecs.Vector, Unit.Metres,
                        (in Source source) => source.Stride.Reach.Right.Hip, (target, value) => target.Reach.Right.Hip = value),
                    Column.Create("PelvisReachRightTargetAnkle", Codecs.Vector, Unit.Metres,
                        (in Source source) => source.Stride.Reach.Right.TargetAnkle, (target, value) => target.Reach.Right.TargetAnkle = value),
                    Column.Create("PelvisReachRightLegLength", Codecs.Float32, Unit.Metres,
                        (in Source source) => source.Stride.Reach.Right.LegLength, (target, value) => target.Reach.Right.LegLength = value),
                    Column.Create("PelvisReachRightMinimumCompressionReserve", Codecs.Float32, Unit.Metres,
                        (in Source source) => source.Stride.Reach.Right.MinimumCompressionReserve, (target, value) => target.Reach.Right.MinimumCompressionReserve = value),
                    Column.Create("PelvisReachRightUsableLegLength", Codecs.Float32, Unit.Metres,
                        (in Source source) => source.Stride.Reach.Right.UsableLegLength, (target, value) => target.Reach.Right.UsableLegLength = value),
                    Column.Create("PelvisReachRightMinimumAlongUp", Codecs.Float32, Unit.Metres,
                        (in Source source) => source.Stride.Reach.Right.MinimumAlongUp, (target, value) => target.Reach.Right.MinimumAlongUp = value),
                    Column.Create("PelvisReachRightMaximumAlongUp", Codecs.Float32, Unit.Metres,
                        (in Source source) => source.Stride.Reach.Right.MaximumAlongUp, (target, value) => target.Reach.Right.MaximumAlongUp = value),
                    Column.Create("PelvisReachRightRequested", Codecs.Boolean, Unit.None,
                        (in Source source) => source.Stride.Reach.Right.Requested, (target, value) => target.Reach.Right.Requested = value),
                    Column.Create("PelvisReachRightAvailable", Codecs.Boolean, Unit.None,
                        (in Source source) => source.Stride.Reach.Right.Available, (target, value) => target.Reach.Right.Available = value),
                    Column.Create("PelvisResponseEvaluated", Codecs.Boolean, Unit.None,
                        (in Source source) => source.Stride.Response.Evaluated, (target, value) => target.Response.Evaluated = value),
                    Column.Create("PelvisSpringCompleted", Codecs.Boolean, Unit.None,
                        (in Source source) => source.Stride.Response.Completed, (target, value) => target.Response.Completed = value),
                    Column.Create("PelvisSpringIntegratedOutput", Codecs.Float32, Unit.Metres,
                        (in Source source) => source.Stride.Response.IntegratedOutput, (target, value) => target.Response.IntegratedOutput = value),
                    Column.Create("StrideSpringTarget", Codecs.Float32, Unit.Metres,
                        (in Source source) => source.Stride.Response.Target, (target, value) => target.Response.Target = value),
                    Column.Create("StrideSpringOutput", Codecs.Float32, Unit.Metres,
                        (in Source source) => source.Stride.Response.Output, (target, value) => target.Response.Output = value),
                    Column.Create("StrideSpringVelocity", Codecs.Float32, Unit.MetresPerSecond,
                        (in Source source) => source.Stride.Response.Velocity, (target, value) => target.Response.Velocity = value),
                    Column.Create("StridePelvisDelta", Codecs.Vector, Unit.Metres,
                        (in Source source) => source.Stride.Core.PelvisDelta, (target, value) => target.Delta = value),
                    Column.Create("StridePositionWeight", Codecs.Float32, Unit.Unitless,
                        (in Source source) => source.Stride.Response.PositionWeight, (target, value) => target.Response.PositionWeight = value),
                    Column.Create("FinalPelvisGoal", Codecs.Vector, Unit.Metres,
                        (in Source source) => source.FinalGoal, (target, value) => target.FinalGoal = value),
                    Column.Create("FinalPhysicalPelvisComponentPosition", Codecs.Vector, Unit.Metres,
                        (in Source source) => source.PhysicalComponent, (target, value) => target.PhysicalComponent = value),
                    Column.Create("FinalPhysicalPelvisWorldPosition", Codecs.Vector, Unit.Metres,
                        (in Source source) => source.PhysicalWorld, (target, value) => target.Observation.PhysicalWorldPosition = value),
                    Column.Create("FinalPhysicalPelvisGoalResidualAvailable", Codecs.Boolean, Unit.None,
                        (in Source source) => source.ResidualAvailable, (target, value) => target.Observation.GoalResidualAvailable = value),
                    Column.Create("FinalPhysicalPelvisGoalResidual", Codecs.Float32, Unit.Metres,
                        (in Source source) => source.Residual, (target, value) => target.Observation.GoalResidual = value),
                });
    }
}
