using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using KK.GeneratedDiagnosticSampling;
using KK.GeneratedDiagnosticSampling.Host;

namespace ThirdPersonCharacter.Pipeline.Diagnostics.FootAnalysis.Editor
{
    internal sealed class CharacterFootSwingOutputInputs
    {
        internal const string FrameSequence = "frame-sequence";
        internal const string ResetSequence = "reset-sequence";
        internal const string DeltaSeconds = "delta-seconds";
        internal const string BodyTick = "body-tick";
        internal const string MotionState = "motion-state";
        internal const string ConstraintState = "constraint-state";
        internal const string AnchorAvailable = "anchor-available";
        internal const string EventIdentity = "event-identity";
        internal const string FormalSourceIdentity = "formal-source-identity";
        internal const string FormalSourceCycle = "formal-source-cycle";
        internal const string PathIdentity = "path-identity";
        internal const string PathAvailable = "path-available";
        internal const string PathRevisionReason = "path-revision-reason";
        internal const string PathResidualRebuilt = "path-residual-rebuilt";
        internal const string PathRevisionDistance = "path-revision-distance";
        internal const string PathLandingDelta = "path-landing-delta";
        internal const string PathTargetDelta = "path-target-delta";
        internal const string PathTargetCorrection = "path-target-correction";
        internal const string GroundPathState = "ground-path-state";
        internal const string GroundLandingEvent = "ground-landing-event";
        internal const string GroundLandingSurface = "ground-landing-surface";
        internal const string GroundLandingPoint = "ground-landing-point";
        internal const string SourceAnklePosition = "source-ankle-position";
        internal const string SourceAnkleRotation = "source-ankle-rotation";
        internal const string SourceHeel = "source-heel";
        internal const string SourceToe = "source-toe";
        internal const string PhysicalWriteAvailable = "physical-write-available";
        internal const string PhysicalAnklePosition = "physical-ankle-position";
        internal const string PhysicalAnkleRotation = "physical-ankle-rotation";

        internal CharacterFootSwingOutputInputs(
            DiagnosticOperatorExecutionContext context)
        {
            Frame = context.Input(FrameSequence);
            Reset = context.Input(ResetSequence);
            Delta = context.Input(DeltaSeconds);
            Tick = context.Input(BodyTick);
            State = context.Input(MotionState);
            Constraint = context.Input(ConstraintState);
            Anchor = context.Input(AnchorAvailable);
            Event = context.Input(EventIdentity);
            SourceIdentity = context.Input(FormalSourceIdentity);
            SourceCycle = context.Input(FormalSourceCycle);
            Path = context.Input(PathIdentity);
            PathIsAvailable = context.Input(PathAvailable);
            RevisionReason = context.Input(PathRevisionReason);
            ResidualRebuilt = context.Input(PathResidualRebuilt);
            RevisionDistance = context.Input(PathRevisionDistance);
            LandingDelta = context.Input(PathLandingDelta);
            TargetDelta = context.Input(PathTargetDelta);
            TargetCorrection = context.Input(PathTargetCorrection);
            GroundState = context.Input(GroundPathState);
            GroundEvent = context.Input(GroundLandingEvent);
            GroundSurface = context.Input(GroundLandingSurface);
            GroundPoint = context.Input(GroundLandingPoint);
            SourceAnklePoint = context.Input(SourceAnklePosition);
            SourceAnkleOrientation = context.Input(SourceAnkleRotation);
            SourceHeelPoint = context.Input(SourceHeel);
            SourceToePoint = context.Input(SourceToe);
            PhysicalIsAvailable = context.Input(PhysicalWriteAvailable);
            PhysicalAnklePoint = context.Input(PhysicalAnklePosition);
            PhysicalAnkleOrientation = context.Input(PhysicalAnkleRotation);
        }

        internal DiagnosticBoundInput Frame { get; }
        internal DiagnosticBoundInput Reset { get; }
        internal DiagnosticBoundInput Delta { get; }
        internal DiagnosticBoundInput Tick { get; }
        internal DiagnosticBoundInput State { get; }
        internal DiagnosticBoundInput Constraint { get; }
        internal DiagnosticBoundInput Anchor { get; }
        internal DiagnosticBoundInput Event { get; }
        internal DiagnosticBoundInput SourceIdentity { get; }
        internal DiagnosticBoundInput SourceCycle { get; }
        internal DiagnosticBoundInput Path { get; }
        internal DiagnosticBoundInput PathIsAvailable { get; }
        internal DiagnosticBoundInput RevisionReason { get; }
        internal DiagnosticBoundInput ResidualRebuilt { get; }
        internal DiagnosticBoundInput RevisionDistance { get; }
        internal DiagnosticBoundInput LandingDelta { get; }
        internal DiagnosticBoundInput TargetDelta { get; }
        internal DiagnosticBoundInput TargetCorrection { get; }
        internal DiagnosticBoundInput GroundState { get; }
        internal DiagnosticBoundInput GroundEvent { get; }
        internal DiagnosticBoundInput GroundSurface { get; }
        internal DiagnosticBoundInput GroundPoint { get; }
        internal DiagnosticBoundInput SourceAnklePoint { get; }
        internal DiagnosticBoundInput SourceAnkleOrientation { get; }
        internal DiagnosticBoundInput SourceHeelPoint { get; }
        internal DiagnosticBoundInput SourceToePoint { get; }
        internal DiagnosticBoundInput PhysicalIsAvailable { get; }
        internal DiagnosticBoundInput PhysicalAnklePoint { get; }
        internal DiagnosticBoundInput PhysicalAnkleOrientation { get; }

        internal static DiagnosticOperatorInputSlot[] Slots() => new[]
        {
            Slot(FrameSequence, DiagnosticValueKind.UInt64),
            Slot(ResetSequence, DiagnosticValueKind.UInt64),
            Slot(DeltaSeconds, DiagnosticValueKind.Float32),
            Slot(BodyTick, DiagnosticValueKind.UInt64),
            Slot(MotionState, DiagnosticValueKind.UInt32),
            Slot(ConstraintState, DiagnosticValueKind.UInt32),
            Slot(AnchorAvailable, DiagnosticValueKind.Boolean),
            Slot(EventIdentity, DiagnosticValueKind.UInt64),
            Slot(FormalSourceIdentity, DiagnosticValueKind.Identity),
            Slot(FormalSourceCycle, DiagnosticValueKind.Int32),
            Slot(PathIdentity, DiagnosticValueKind.UInt64),
            Slot(PathAvailable, DiagnosticValueKind.Boolean),
            Slot(PathRevisionReason, DiagnosticValueKind.UInt32),
            Slot(PathResidualRebuilt, DiagnosticValueKind.Boolean),
            Slot(PathRevisionDistance, DiagnosticValueKind.Float32),
            Slot(PathLandingDelta, DiagnosticValueKind.Float32),
            Slot(PathTargetDelta, DiagnosticValueKind.Float32),
            Slot(PathTargetCorrection, DiagnosticValueKind.Vector3),
            Slot(GroundPathState, DiagnosticValueKind.UInt32),
            Slot(GroundLandingEvent, DiagnosticValueKind.UInt64),
            Slot(GroundLandingSurface, DiagnosticValueKind.Int32),
            Slot(GroundLandingPoint, DiagnosticValueKind.Vector3),
            Slot(SourceAnklePosition, DiagnosticValueKind.Vector3),
            Slot(SourceAnkleRotation, DiagnosticValueKind.Quaternion),
            Slot(SourceHeel, DiagnosticValueKind.Vector3),
            Slot(SourceToe, DiagnosticValueKind.Vector3),
            Slot(PhysicalWriteAvailable, DiagnosticValueKind.Boolean),
            Slot(PhysicalAnklePosition, DiagnosticValueKind.Vector3),
            Slot(PhysicalAnkleRotation, DiagnosticValueKind.Quaternion)
        };

        internal static DiagnosticOperatorInputSlot Slot(
            string id,
            DiagnosticValueKind kind,
            DiagnosticDatasetCardinality cardinality = DiagnosticDatasetCardinality.Main) =>
            new DiagnosticOperatorInputSlot(id, kind, cardinality, true);
    }

    internal sealed class CharacterFootSwingFrame
    {
        internal string Dimension;
        internal ulong Sequence;
        internal ulong Frame;
        internal ulong Reset;
        internal ulong BodyTick;
        internal double Delta;
        internal uint MotionState;
        internal uint ConstraintState;
        internal bool AnchorAvailable;
        internal ulong EventIdentity;
        internal string SourceIdentity;
        internal int SourceCycle;
        internal ulong PathIdentity;
        internal bool PathAvailable;
        internal uint RevisionReason;
        internal bool ResidualRebuilt;
        internal double RevisionDistance;
        internal double LandingDelta;
        internal double TargetDelta;
        internal DiagnosticVector3 TargetCorrection;
        internal uint GroundState;
        internal ulong GroundEvent;
        internal int GroundSurface;
        internal DiagnosticVector3 GroundPoint;
        internal bool PhysicalAvailable;
        internal DiagnosticVector3 SourceAnkle;
        internal DiagnosticVector3 SourceHeel;
        internal DiagnosticVector3 SourceToe;
        internal DiagnosticVector3 PhysicalAnkle;
        internal DiagnosticVector3 PhysicalHeel;
        internal DiagnosticVector3 PhysicalToe;

        internal DiagnosticVector3 Source(int index) => index == 0
            ? SourceAnkle
            : index == 1
                ? SourceHeel
                : SourceToe;

        internal DiagnosticVector3 Physical(int index) => index == 0
            ? PhysicalAnkle
            : index == 1
                ? PhysicalHeel
                : PhysicalToe;
    }

    internal sealed class CharacterFootSwingOutputPair
    {
        internal CharacterFootSwingFrame Previous;
        internal CharacterFootSwingFrame Current;
        internal bool IsRevision;
        internal double AnkleStep;
        internal double HeelStep;
        internal double ToeStep;
        internal double PrimaryStep;
        internal double PrimarySpeed;
        internal bool AccelerationAvailable;
        internal double PrimaryAcceleration;
        internal bool JerkAvailable;
        internal double PrimaryJerk;
    }

    internal sealed class CharacterFootSwingOutputReadResult
    {
        internal List<CharacterFootSwingOutputPair> Stable { get; } =
            new List<CharacterFootSwingOutputPair>();
        internal List<CharacterFootSwingOutputPair> Revision { get; } =
            new List<CharacterFootSwingOutputPair>();
        internal HashSet<string> MissingEvidence { get; } =
            new HashSet<string>(StringComparer.Ordinal);
    }

    internal static class CharacterFootSwingOutputReader
    {
        internal static CharacterFootSwingOutputReadResult Read(
            DiagnosticOperatorExecutionContext context,
            double timeEpsilon,
            double positionNoiseFloor,
            uint acceptedState,
            uint swingState,
            uint groundAcceptedState,
            uint pathAvailabilityReason,
            uint landingEventReason)
        {
            var inputs = new CharacterFootSwingOutputInputs(context);
            var result = new CharacterFootSwingOutputReadResult();
            var history = new Dictionary<string, List<CharacterFootSwingFrame>>(
                StringComparer.Ordinal);
            DiagnosticDatasetCursor cursor = context.CreateCursor(0);
            while (cursor.MoveNext())
            {
                DiagnosticDatasetRow row = cursor.Current;
                string dimension = row.SampleKey.DimensionId;
                if (!context.IncludesDimension(dimension) ||
                    !context.MatchesFilters(row))
                {
                    history.Remove(dimension);
                    continue;
                }
                if (!TryRead(
                        row,
                        inputs,
                        acceptedState,
                        swingState,
                        result.MissingEvidence,
                        out CharacterFootSwingFrame frame,
                        out bool eligible) ||
                    !eligible)
                {
                    history.Remove(dimension);
                    continue;
                }
                if (!history.TryGetValue(dimension, out List<CharacterFootSwingFrame> values))
                {
                    values = new List<CharacterFootSwingFrame>(4);
                    history.Add(dimension, values);
                }
                if (values.Count != 0)
                {
                    CharacterFootSwingFrame previous = values[values.Count - 1];
                    if (Continuous(previous, frame))
                    {
                        CharacterFootSwingOutputPair pair = BuildPair(
                            values,
                            frame,
                            timeEpsilon);
                        bool pathAvailabilityChanged =
                            previous.PathAvailable != frame.PathAvailable ||
                            (frame.RevisionReason & pathAvailabilityReason) != 0;
                        bool landingEventChanged =
                            previous.GroundEvent != frame.GroundEvent ||
                            (frame.RevisionReason & landingEventReason) != 0;
                        bool surfaceChanged = previous.GroundSurface != 0 &&
                            frame.GroundSurface != 0 &&
                            previous.GroundSurface != frame.GroundSurface;
                        double noiseFloor = Math.Max(
                            positionNoiseFloor,
                            frame.RevisionDistance);
                        bool semanticRevision = pathAvailabilityChanged ||
                            landingEventChanged ||
                            surfaceChanged ||
                            CharacterFootDiagnosticOperatorSupport.Distance(
                                previous.GroundPoint,
                                frame.GroundPoint) > noiseFloor ||
                            frame.LandingDelta > noiseFloor ||
                            frame.TargetDelta > noiseFloor;
                        pair.IsRevision = semanticRevision;
                        if (semanticRevision)
                        {
                            result.Revision.Add(pair);
                        }
                        else if (previous.EventIdentity != 0 &&
                            previous.EventIdentity == frame.EventIdentity &&
                            string.Equals(
                                previous.SourceIdentity,
                                frame.SourceIdentity,
                                StringComparison.Ordinal) &&
                            previous.SourceCycle == frame.SourceCycle &&
                            previous.PathAvailable &&
                            frame.PathAvailable &&
                            previous.GroundState == groundAcceptedState &&
                            frame.GroundState == groundAcceptedState)
                        {
                            result.Stable.Add(pair);
                        }
                    }
                }
                values.Add(frame);
                if (values.Count > 4)
                    values.RemoveAt(0);
            }
            return result;
        }

        static bool TryRead(
            in DiagnosticDatasetRow row,
            CharacterFootSwingOutputInputs inputs,
            uint acceptedState,
            uint swingState,
            ISet<string> missing,
            out CharacterFootSwingFrame frame,
            out bool eligible)
        {
            DiagnosticBoundInput[] discriminators =
            {
                inputs.Frame,
                inputs.Reset,
                inputs.State,
                inputs.Constraint,
                inputs.Anchor
            };
            bool complete = true;
            for (int i = 0; i < discriminators.Length; i++)
            {
                if (row.IsAvailable(discriminators[i].Handle))
                    continue;
                missing.Add(CharacterFootDiagnosticOperatorSupport.Identity(discriminators[i]));
                complete = false;
            }
            if (!complete)
            {
                frame = null;
                eligible = false;
                return false;
            }
            frame = new CharacterFootSwingFrame
            {
                Dimension = row.SampleKey.DimensionId,
                Sequence = row.SampleKey.Sequence,
                Frame = row.GetUInt64(inputs.Frame.Handle),
                Reset = row.GetUInt64(inputs.Reset.Handle),
                MotionState = row.GetUInt32(inputs.State.Handle),
                ConstraintState = row.GetUInt32(inputs.Constraint.Handle),
                AnchorAvailable = row.GetBoolean(inputs.Anchor.Handle)
            };
            eligible = frame.MotionState == acceptedState &&
                frame.ConstraintState == swingState &&
                !frame.AnchorAvailable;
            if (!eligible)
                return true;
            DiagnosticBoundInput[] required =
            {
                inputs.Delta,
                inputs.Tick,
                inputs.Event,
                inputs.SourceIdentity,
                inputs.SourceCycle,
                inputs.Path,
                inputs.PathIsAvailable,
                inputs.RevisionReason,
                inputs.ResidualRebuilt,
                inputs.RevisionDistance,
                inputs.LandingDelta,
                inputs.TargetDelta,
                inputs.TargetCorrection,
                inputs.GroundState,
                inputs.GroundEvent,
                inputs.GroundSurface,
                inputs.GroundPoint,
                inputs.PhysicalIsAvailable
            };
            for (int i = 0; i < required.Length; i++)
            {
                if (row.IsAvailable(required[i].Handle))
                    continue;
                missing.Add(CharacterFootDiagnosticOperatorSupport.Identity(required[i]));
                complete = false;
            }
            if (!complete)
            {
                frame = null;
                eligible = false;
                return false;
            }
            frame.BodyTick = row.GetUInt64(inputs.Tick.Handle);
            frame.Delta = row.GetFloat32(inputs.Delta.Handle);
            frame.EventIdentity = row.GetUInt64(inputs.Event.Handle);
            frame.SourceIdentity = row.GetIdentity(inputs.SourceIdentity.Handle);
            frame.SourceCycle = row.GetInt32(inputs.SourceCycle.Handle);
            frame.PathIdentity = row.GetUInt64(inputs.Path.Handle);
            frame.PathAvailable = row.GetBoolean(inputs.PathIsAvailable.Handle);
            frame.RevisionReason = row.GetUInt32(inputs.RevisionReason.Handle);
            frame.ResidualRebuilt = row.GetBoolean(inputs.ResidualRebuilt.Handle);
            frame.RevisionDistance = row.GetFloat32(inputs.RevisionDistance.Handle);
            frame.LandingDelta = row.GetFloat32(inputs.LandingDelta.Handle);
            frame.TargetDelta = row.GetFloat32(inputs.TargetDelta.Handle);
            frame.TargetCorrection = row.GetVector3(inputs.TargetCorrection.Handle);
            frame.GroundState = row.GetUInt32(inputs.GroundState.Handle);
            frame.GroundEvent = row.GetUInt64(inputs.GroundEvent.Handle);
            frame.GroundSurface = row.GetInt32(inputs.GroundSurface.Handle);
            frame.GroundPoint = row.GetVector3(inputs.GroundPoint.Handle);
            frame.PhysicalAvailable = row.GetBoolean(inputs.PhysicalIsAvailable.Handle);
            if (!frame.PhysicalAvailable)
            {
                missing.Add("physical-write-available=false");
                frame = null;
                eligible = false;
                return false;
            }
            DiagnosticBoundInput[] geometry =
            {
                inputs.SourceAnklePoint,
                inputs.SourceAnkleOrientation,
                inputs.SourceHeelPoint,
                inputs.SourceToePoint,
                inputs.PhysicalAnklePoint,
                inputs.PhysicalAnkleOrientation
            };
            for (int i = 0; i < geometry.Length; i++)
            {
                if (row.IsAvailable(geometry[i].Handle))
                    continue;
                missing.Add(CharacterFootDiagnosticOperatorSupport.Identity(geometry[i]));
                complete = false;
            }
            if (!complete)
            {
                frame = null;
                eligible = false;
                return false;
            }
            DiagnosticVector3 sourceAnkle = row.GetVector3(inputs.SourceAnklePoint.Handle);
            DiagnosticQuaternion sourceRotation =
                row.GetQuaternion(inputs.SourceAnkleOrientation.Handle);
            DiagnosticVector3 sourceHeel = row.GetVector3(inputs.SourceHeelPoint.Handle);
            DiagnosticVector3 sourceToe = row.GetVector3(inputs.SourceToePoint.Handle);
            DiagnosticVector3 physicalAnkle =
                row.GetVector3(inputs.PhysicalAnklePoint.Handle);
            DiagnosticQuaternion physicalRotation =
                row.GetQuaternion(inputs.PhysicalAnkleOrientation.Handle);
            if (!CharacterFootDiagnosticOperatorSupport.TryRebuildPhysicalProbe(
                    sourceAnkle,
                    sourceRotation,
                    sourceHeel,
                    physicalAnkle,
                    physicalRotation,
                    out DiagnosticVector3 physicalHeel) ||
                !CharacterFootDiagnosticOperatorSupport.TryRebuildPhysicalProbe(
                    sourceAnkle,
                    sourceRotation,
                    sourceToe,
                    physicalAnkle,
                    physicalRotation,
                    out DiagnosticVector3 physicalToe))
            {
                missing.Add("invalid-physical-ankle-rigid-transform");
                frame = null;
                eligible = false;
                return false;
            }
            frame.SourceAnkle = sourceAnkle;
            frame.SourceHeel = sourceHeel;
            frame.SourceToe = sourceToe;
            frame.PhysicalAnkle = physicalAnkle;
            frame.PhysicalHeel = physicalHeel;
            frame.PhysicalToe = physicalToe;
            return true;
        }

        static CharacterFootSwingOutputPair BuildPair(
            IReadOnlyList<CharacterFootSwingFrame> history,
            CharacterFootSwingFrame current,
            double timeEpsilon)
        {
            CharacterFootSwingFrame previous = history[history.Count - 1];
            var steps = new double[3];
            var velocities = new DiagnosticVector3[3];
            double delta = Math.Max(current.Delta, timeEpsilon);
            for (int i = 0; i < 3; i++)
            {
                DiagnosticVector3 previousOffset = Subtract(
                    previous.Physical(i),
                    previous.Source(i));
                DiagnosticVector3 currentOffset = Subtract(
                    current.Physical(i),
                    current.Source(i));
                DiagnosticVector3 step = Subtract(currentOffset, previousOffset);
                steps[i] = Length(step);
                velocities[i] = Scale(step, 1d / delta);
            }
            var pair = new CharacterFootSwingOutputPair
            {
                Previous = previous,
                Current = current,
                AnkleStep = steps[0],
                HeelStep = steps[1],
                ToeStep = steps[2],
                PrimaryStep = Math.Max(steps[0], Math.Max(steps[1], steps[2])),
                PrimarySpeed = Math.Max(
                    Length(velocities[0]),
                    Math.Max(Length(velocities[1]), Length(velocities[2])))
            };
            if (history.Count >= 2)
            {
                CharacterFootSwingFrame beforePrevious = history[history.Count - 2];
                if (Continuous(beforePrevious, previous) &&
                    beforePrevious.PhysicalAvailable)
                {
                    var accelerations = new DiagnosticVector3[3];
                    double previousDelta = Math.Max(previous.Delta, timeEpsilon);
                    for (int i = 0; i < 3; i++)
                    {
                        DiagnosticVector3 beforeOffset = Subtract(
                            beforePrevious.Physical(i),
                            beforePrevious.Source(i));
                        DiagnosticVector3 previousOffset = Subtract(
                            previous.Physical(i),
                            previous.Source(i));
                        DiagnosticVector3 previousVelocity = Scale(
                            Subtract(previousOffset, beforeOffset),
                            1d / previousDelta);
                        accelerations[i] = Scale(
                            Subtract(velocities[i], previousVelocity),
                            1d / delta);
                    }
                    pair.AccelerationAvailable = true;
                    pair.PrimaryAcceleration = Math.Max(
                        Length(accelerations[0]),
                        Math.Max(Length(accelerations[1]), Length(accelerations[2])));
                    if (history.Count >= 3)
                    {
                        CharacterFootSwingFrame beforeBefore = history[history.Count - 3];
                        if (Continuous(beforeBefore, beforePrevious) &&
                            beforeBefore.PhysicalAvailable)
                        {
                            double beforeDelta = Math.Max(beforePrevious.Delta, timeEpsilon);
                            double primaryJerk = 0d;
                            for (int i = 0; i < 3; i++)
                            {
                                DiagnosticVector3 beforeBeforeOffset = Subtract(
                                    beforeBefore.Physical(i),
                                    beforeBefore.Source(i));
                                DiagnosticVector3 beforeOffset = Subtract(
                                    beforePrevious.Physical(i),
                                    beforePrevious.Source(i));
                                DiagnosticVector3 previousOffset = Subtract(
                                    previous.Physical(i),
                                    previous.Source(i));
                                DiagnosticVector3 beforeVelocity = Scale(
                                    Subtract(beforeOffset, beforeBeforeOffset),
                                    1d / beforeDelta);
                                DiagnosticVector3 previousVelocity = Scale(
                                    Subtract(previousOffset, beforeOffset),
                                    1d / previousDelta);
                                DiagnosticVector3 previousAcceleration = Scale(
                                    Subtract(previousVelocity, beforeVelocity),
                                    1d / previousDelta);
                                primaryJerk = Math.Max(
                                    primaryJerk,
                                    Length(Scale(
                                        Subtract(accelerations[i], previousAcceleration),
                                        1d / delta)));
                            }
                            pair.JerkAvailable = true;
                            pair.PrimaryJerk = primaryJerk;
                        }
                    }
                }
            }
            return pair;
        }

        internal static bool Continuous(
            CharacterFootSwingFrame previous,
            CharacterFootSwingFrame current) =>
            current.Frame == previous.Frame + 1 &&
            current.Reset == previous.Reset;

        internal static DiagnosticVector3 Subtract(
            in DiagnosticVector3 left,
            in DiagnosticVector3 right) =>
            new DiagnosticVector3(
                left.X - right.X,
                left.Y - right.Y,
                left.Z - right.Z);

        internal static DiagnosticVector3 Scale(
            in DiagnosticVector3 value,
            double scale) =>
            new DiagnosticVector3(
                (float)(value.X * scale),
                (float)(value.Y * scale),
                (float)(value.Z * scale));

        internal static double Length(in DiagnosticVector3 value) =>
            Math.Sqrt(value.X * value.X + value.Y * value.Y + value.Z * value.Z);
    }

    internal abstract class CharacterFootSwingOutputJumpOperator :
        IDiagnosticAnalysisOperator
    {
        const string Threshold = "maximum-output-step-meters";
        const string PositionNoiseFloor = "position-noise-floor-meters";
        const string TimeEpsilon = "time-epsilon-seconds";
        const string LowCadenceDelta = "low-cadence-delta-seconds";
        const string SpeedAnomaly = "speed-anomaly-meters-per-second";
        const string AcceptedState = "accepted-motion-state";
        const string SwingState = "swing-constraint-state";
        const string GroundAcceptedState = "accepted-ground-path-state";
        const string PathAvailabilityReason = "path-availability-reason";
        const string LandingEventReason = "landing-event-reason";
        const string RepresentativeLimit = "representative-limit";

        readonly bool m_Revision;

        protected CharacterFootSwingOutputJumpOperator(string id, bool revision)
        {
            m_Revision = revision;
            Descriptor = new DiagnosticOperatorDescriptor(
                id,
                "motion",
                CharacterFootSwingOutputInputs.Slots(),
                Parameters());
        }

        public DiagnosticOperatorDescriptor Descriptor { get; }

        public DiagnosticOperatorResult Execute(
            DiagnosticOperatorExecutionContext context)
        {
            double threshold = CharacterFootDiagnosticOperatorSupport.RequireNumber(
                context,
                Threshold);
            double positionNoise = CharacterFootDiagnosticOperatorSupport.RequireNumber(
                context,
                PositionNoiseFloor);
            double timeEpsilon = CharacterFootDiagnosticOperatorSupport.RequireNumber(
                context,
                TimeEpsilon);
            double lowCadence = CharacterFootDiagnosticOperatorSupport.RequireNumber(
                context,
                LowCadenceDelta);
            double speedAnomaly = CharacterFootDiagnosticOperatorSupport.RequireNumber(
                context,
                SpeedAnomaly);
            uint accepted = CharacterFootDiagnosticOperatorSupport.RequireUInt32(
                context,
                AcceptedState);
            uint swing = CharacterFootDiagnosticOperatorSupport.RequireUInt32(
                context,
                SwingState);
            uint groundAccepted = CharacterFootDiagnosticOperatorSupport.RequireUInt32(
                context,
                GroundAcceptedState);
            uint pathAvailability = CharacterFootDiagnosticOperatorSupport.RequireUInt32(
                context,
                PathAvailabilityReason);
            uint landingEvent = CharacterFootDiagnosticOperatorSupport.RequireUInt32(
                context,
                LandingEventReason);
            int representativeLimit = checked((int)
                CharacterFootDiagnosticOperatorSupport.RequireUInt32(
                    context,
                    RepresentativeLimit));
            CharacterFootSwingOutputReadResult read =
                CharacterFootSwingOutputReader.Read(
                    context,
                    timeEpsilon,
                    positionNoise,
                    accepted,
                    swing,
                    groundAccepted,
                    pathAvailability,
                    landingEvent);
            if (read.MissingEvidence.Count != 0)
                return DiagnosticOperatorResult.MissingEvidence(
                    CharacterFootDiagnosticOperatorSupport.MissingSummary(
                        read.MissingEvidence));
            List<CharacterFootSwingOutputPair> eligible = m_Revision
                ? read.Revision
                : read.Stable;
            if (eligible.Count == 0)
                return DiagnosticOperatorResult.NotApplicable(
                    m_Revision
                        ? "No complete accepted unanchored Swing path-revision frame pair was observed."
                        : "No complete accepted unanchored stable-path Swing frame pair was observed.");
            List<CharacterFootSwingOutputPair> matched = eligible
                .Where(value => value.PrimaryStep > threshold)
                .OrderByDescending(value => value.PrimaryStep)
                .ThenBy(value => value.Current.Sequence)
                .Take(representativeLimit)
                .ToList();
            var occurrences = eligible.Select(value => value.PrimaryStep).ToArray();
            double score = CharacterFootDiagnosticOperatorSupport.SeverityHealth(
                occurrences,
                eligible.Count);
            int matchCount = eligible.Count(value => value.PrimaryStep > threshold);
            string summary =
                $"{matchCount} of {eligible.Count} {(m_Revision ? "path-revision" : "stable-path")} Swing frame pairs exceeded {CharacterFootDiagnosticOperatorSupport.Format(threshold)} m final physical output step.";
            if (matched.Count == 0)
                return DiagnosticOperatorResult.Passed(summary, score: score);
            var inputs = new CharacterFootSwingOutputInputs(context);
            var findings = new List<DiagnosticFinding>(matched.Count);
            for (int i = 0; i < matched.Count; i++)
            {
                CharacterFootSwingOutputPair pair = matched[i];
                bool lowSampling = pair.Current.Delta >= lowCadence ||
                    pair.Current.BodyTick > pair.Previous.BodyTick + 1;
                bool speedExceeded = pair.PrimarySpeed > speedAnomaly;
                string sampling = speedExceeded
                    ? lowSampling
                        ? "LowCadenceSpeedAnomaly"
                        : "RegularCadenceSpeedAnomaly"
                    : lowSampling
                        ? "LowCadenceNormalSpeed"
                        : "RegularCadenceNormalSpeed";
                var evidence = new List<DiagnosticEvidence>
                {
                    Evidence(inputs.PhysicalAnklePoint, pair, "output-step-meters", pair.PrimaryStep),
                    Evidence(inputs.PhysicalAnklePoint, pair, "ankle-output-step-meters", pair.AnkleStep),
                    Evidence(inputs.PhysicalAnklePoint, pair, "heel-output-step-meters", pair.HeelStep),
                    Evidence(inputs.PhysicalAnklePoint, pair, "toe-output-step-meters", pair.ToeStep),
                    Evidence(inputs.Delta, pair, "output-speed-meters-per-second", pair.PrimarySpeed),
                    Evidence(inputs.RevisionReason, pair, "path-revision-reason", pair.Current.RevisionReason.ToString(CultureInfo.InvariantCulture)),
                    Evidence(inputs.LandingDelta, pair, "path-landing-delta-meters", pair.Current.LandingDelta),
                    Evidence(inputs.TargetDelta, pair, "path-target-delta-meters", pair.Current.TargetDelta),
                    Evidence(inputs.Delta, pair, "sampling-classification", sampling)
                };
                if (pair.AccelerationAvailable)
                    evidence.Add(Evidence(
                        inputs.Delta,
                        pair,
                        "output-acceleration-meters-per-second-squared",
                        pair.PrimaryAcceleration));
                if (pair.JerkAvailable)
                    evidence.Add(Evidence(
                        inputs.Delta,
                        pair,
                        "output-jerk-meters-per-second-cubed",
                        pair.PrimaryJerk));
                findings.Add(new DiagnosticFinding(
                    CharacterFootDiagnosticOperatorSupport.FindingId(
                        context.RuleId,
                        pair.Current.Sequence),
                    CharacterFootDiagnosticOperatorSupport.Severity(pair.PrimaryStep),
                    pair.Current.Dimension,
                    pair.Previous.Sequence,
                    pair.Current.Sequence,
                    $"Final physical foot output changed by {CharacterFootDiagnosticOperatorSupport.Format(pair.PrimaryStep)} m on a {(m_Revision ? "path-revision" : "stable-path")} Swing frame pair.",
                    evidence,
                    CharacterFootDiagnosticOperatorSupport.FrameRange(
                        inputs.Frame,
                        pair.Previous.Frame,
                        pair.Current.Frame)));
            }
            return DiagnosticOperatorResult.Failed(summary, null, findings, score);
        }

        static DiagnosticEvidence Evidence(
            in DiagnosticBoundInput input,
            CharacterFootSwingOutputPair pair,
            string id,
            double value) => Evidence(
                input,
                pair,
                id,
                CharacterFootDiagnosticOperatorSupport.Format(value));

        static DiagnosticEvidence Evidence(
            in DiagnosticBoundInput input,
            CharacterFootSwingOutputPair pair,
            string id,
            string value) =>
            CharacterFootDiagnosticOperatorSupport.Evidence(
                id,
                input,
                pair.Current.Dimension,
                pair.Previous.Sequence,
                pair.Current.Sequence,
                value);

        internal static DiagnosticOperatorParameter[] Parameters() => new[]
        {
            Number(Threshold),
            Number(PositionNoiseFloor),
            Number(TimeEpsilon),
            Number(LowCadenceDelta),
            Number(SpeedAnomaly),
            Integer(AcceptedState),
            Integer(SwingState),
            Integer(GroundAcceptedState),
            Integer(PathAvailabilityReason),
            Integer(LandingEventReason),
            Integer(RepresentativeLimit, 1d)
        };

        internal static DiagnosticOperatorParameter Number(string id) =>
            new DiagnosticOperatorParameter(
                id,
                DiagnosticOperatorParameterKind.Number,
                true,
                0d);

        internal static DiagnosticOperatorParameter Integer(
            string id,
            double minimum = 0d) =>
            new DiagnosticOperatorParameter(
                id,
                DiagnosticOperatorParameterKind.Integer,
                true,
                minimum);
    }

    internal sealed class CharacterFootStableSwingOutputJumpOperator :
        CharacterFootSwingOutputJumpOperator
    {
        internal CharacterFootStableSwingOutputJumpOperator()
            : base("foot/stable-swing-output-jump", false)
        {
        }
    }

    internal sealed class CharacterFootPathRevisionOutputJumpOperator :
        CharacterFootSwingOutputJumpOperator
    {
        internal CharacterFootPathRevisionOutputJumpOperator()
            : base("foot/path-revision-output-jump", true)
        {
        }
    }

    internal sealed class CharacterFootPathRevisionAmplificationEvidenceOperator :
        IDiagnosticAnalysisOperator
    {
        static readonly string[] s_StageNames =
        {
            "state-target",
            "interpolation-output",
            "captured-residual",
            "decayed-residual",
            "residual-output",
            "pre-safety-floor-output",
            "safety-floor-output",
            "final-effective-correction",
            "encoded-goal-correction",
            "final-physical-output"
        };

        static readonly string[] s_StageSlots =
        {
            "state-target-correction",
            "interpolation-output-correction",
            "swing-residual-before-decay",
            "swing-residual-after-decay",
            "residual-output-correction",
            "correction-before-safety-floor",
            "safety-floor-output-correction",
            "final-effective-correction",
            "goal-target-correction"
        };

        public DiagnosticOperatorDescriptor Descriptor { get; } =
            new DiagnosticOperatorDescriptor(
                "foot/path-revision-amplification-evidence",
                "motion",
                Slots(),
                EvidenceParameters());

        public DiagnosticOperatorResult Execute(
            DiagnosticOperatorExecutionContext context)
        {
            double threshold = CharacterFootDiagnosticOperatorSupport.RequireNumber(
                context,
                "maximum-output-step-meters");
            double noise = CharacterFootDiagnosticOperatorSupport.RequireNumber(
                context,
                "position-noise-floor-meters");
            double timeEpsilon = CharacterFootDiagnosticOperatorSupport.RequireNumber(
                context,
                "time-epsilon-seconds");
            uint accepted = CharacterFootDiagnosticOperatorSupport.RequireUInt32(
                context,
                "accepted-motion-state");
            uint swing = CharacterFootDiagnosticOperatorSupport.RequireUInt32(
                context,
                "swing-constraint-state");
            uint groundAccepted = CharacterFootDiagnosticOperatorSupport.RequireUInt32(
                context,
                "accepted-ground-path-state");
            uint pathAvailability = CharacterFootDiagnosticOperatorSupport.RequireUInt32(
                context,
                "path-availability-reason");
            uint landingEvent = CharacterFootDiagnosticOperatorSupport.RequireUInt32(
                context,
                "landing-event-reason");
            int limit = checked((int)
                CharacterFootDiagnosticOperatorSupport.RequireUInt32(
                    context,
                    "representative-limit"));
            CharacterFootSwingOutputReadResult read =
                CharacterFootSwingOutputReader.Read(
                    context,
                    timeEpsilon,
                    noise,
                    accepted,
                    swing,
                    groundAccepted,
                    pathAvailability,
                    landingEvent);
            if (read.MissingEvidence.Count != 0)
                return DiagnosticOperatorResult.MissingEvidence(
                    CharacterFootDiagnosticOperatorSupport.MissingSummary(
                        read.MissingEvidence));
            List<CharacterFootSwingOutputPair> allMatched = read.Revision
                .Where(value => value.PrimaryStep > threshold)
                .OrderByDescending(value => value.PrimaryStep)
                .ThenBy(value => value.Current.Sequence)
                .ToList();
            List<CharacterFootSwingOutputPair> matched = allMatched
                .Take(limit)
                .ToList();
            if (read.Revision.Count == 0)
                return DiagnosticOperatorResult.NotApplicable(
                    "No complete accepted unanchored Swing path-revision frame pair was observed.");
            if (matched.Count == 0)
                return new DiagnosticOperatorResult(
                    DiagnosticRuleState.Passed,
                    $"0 of {read.Revision.Count} path-revision frame pairs exceeded {CharacterFootDiagnosticOperatorSupport.Format(threshold)} m.",
                    null,
                    null,
                    null);
            var handles = s_StageSlots.Select(context.Input).ToArray();
            var snapshots = ReadStages(
                context,
                handles,
                matched,
                read.MissingEvidence);
            if (read.MissingEvidence.Count != 0)
                return DiagnosticOperatorResult.MissingEvidence(
                    CharacterFootDiagnosticOperatorSupport.MissingSummary(
                        read.MissingEvidence));
            var frameInput = context.Input(CharacterFootSwingOutputInputs.FrameSequence);
            var findings = new List<DiagnosticFinding>(matched.Count);
            for (int i = 0; i < matched.Count; i++)
            {
                CharacterFootSwingOutputPair pair = matched[i];
                StageSnapshot previous = snapshots[(pair.Current.Dimension, pair.Previous.Sequence)];
                StageSnapshot current = snapshots[(pair.Current.Dimension, pair.Current.Sequence)];
                var steps = new double[s_StageNames.Length];
                for (int stage = 0; stage < steps.Length - 1; stage++)
                {
                    steps[stage] = CharacterFootDiagnosticOperatorSupport.Distance(
                        previous.Values[stage],
                        current.Values[stage]);
                }
                steps[steps.Length - 1] = pair.PrimaryStep;
                int firstAmplification = -1;
                for (int stage = 1; stage < steps.Length; stage++)
                {
                    if (steps[stage] > steps[stage - 1] + noise)
                    {
                        firstAmplification = stage;
                        break;
                    }
                }
                var evidence = new List<DiagnosticEvidence>
                {
                    CharacterFootDiagnosticOperatorSupport.Evidence(
                        "first-amplification-stage",
                        handles[0],
                        pair.Current.Dimension,
                        pair.Previous.Sequence,
                        pair.Current.Sequence,
                        firstAmplification < 0
                            ? "None"
                            : s_StageNames[firstAmplification]),
                    CharacterFootDiagnosticOperatorSupport.Evidence(
                        "final-physical-output-step-meters",
                        context.Input(
                            CharacterFootSwingOutputInputs.PhysicalAnklePosition),
                        pair.Current.Dimension,
                        pair.Previous.Sequence,
                        pair.Current.Sequence,
                        CharacterFootDiagnosticOperatorSupport.Format(pair.PrimaryStep))
                };
                for (int stage = 0; stage < steps.Length; stage++)
                {
                    DiagnosticBoundInput evidenceInput = stage < handles.Length
                        ? handles[stage]
                        : context.Input(
                            CharacterFootSwingOutputInputs.PhysicalAnklePosition);
                    evidence.Add(CharacterFootDiagnosticOperatorSupport.Evidence(
                        s_StageNames[stage] + "-step-meters",
                        evidenceInput,
                        pair.Current.Dimension,
                        pair.Previous.Sequence,
                        pair.Current.Sequence,
                        CharacterFootDiagnosticOperatorSupport.Format(steps[stage])));
                }
                findings.Add(new DiagnosticFinding(
                    CharacterFootDiagnosticOperatorSupport.FindingId(
                        context.RuleId,
                        pair.Current.Sequence),
                    DiagnosticSeverity.Information,
                    pair.Current.Dimension,
                    pair.Previous.Sequence,
                    pair.Current.Sequence,
                    firstAmplification < 0
                        ? "Path revision output jump has no stage amplification above the noise floor."
                        : $"Path revision output jump first amplified at {s_StageNames[firstAmplification]}.",
                    evidence,
                    CharacterFootDiagnosticOperatorSupport.FrameRange(
                        frameInput,
                        pair.Previous.Frame,
                        pair.Current.Frame)));
            }
            return new DiagnosticOperatorResult(
                DiagnosticRuleState.Failed,
                $"{allMatched.Count} of {read.Revision.Count} path-revision frame pairs exceeded {CharacterFootDiagnosticOperatorSupport.Format(threshold)} m; {matched.Count} representative pairs include ordered stage amplification evidence.",
                null,
                null,
                findings);
        }

        static DiagnosticOperatorInputSlot[] Slots()
        {
            var result = new List<DiagnosticOperatorInputSlot>(
                CharacterFootSwingOutputInputs.Slots());
            for (int i = 0; i < s_StageSlots.Length; i++)
            {
                result.Add(CharacterFootSwingOutputInputs.Slot(
                    s_StageSlots[i],
                    DiagnosticValueKind.Vector3));
            }
            return result.ToArray();
        }

        static DiagnosticOperatorParameter[] EvidenceParameters() => new[]
        {
            CharacterFootSwingOutputJumpOperator.Number("maximum-output-step-meters"),
            CharacterFootSwingOutputJumpOperator.Number("position-noise-floor-meters"),
            CharacterFootSwingOutputJumpOperator.Number("time-epsilon-seconds"),
            CharacterFootSwingOutputJumpOperator.Integer("accepted-motion-state"),
            CharacterFootSwingOutputJumpOperator.Integer("swing-constraint-state"),
            CharacterFootSwingOutputJumpOperator.Integer("accepted-ground-path-state"),
            CharacterFootSwingOutputJumpOperator.Integer("path-availability-reason"),
            CharacterFootSwingOutputJumpOperator.Integer("landing-event-reason"),
            CharacterFootSwingOutputJumpOperator.Integer("representative-limit", 1d)
        };

        static Dictionary<(string Dimension, ulong Sequence), StageSnapshot> ReadStages(
            DiagnosticOperatorExecutionContext context,
            IReadOnlyList<DiagnosticBoundInput> handles,
            IReadOnlyList<CharacterFootSwingOutputPair> pairs,
            ISet<string> missing)
        {
            var required = new HashSet<(string Dimension, ulong Sequence)>();
            for (int i = 0; i < pairs.Count; i++)
            {
                required.Add((pairs[i].Current.Dimension, pairs[i].Previous.Sequence));
                required.Add((pairs[i].Current.Dimension, pairs[i].Current.Sequence));
            }
            var result = new Dictionary<(string Dimension, ulong Sequence), StageSnapshot>();
            DiagnosticDatasetCursor cursor = context.CreateCursor(0);
            while (cursor.MoveNext())
            {
                DiagnosticDatasetRow row = cursor.Current;
                var key = (row.SampleKey.DimensionId, row.SampleKey.Sequence);
                if (!required.Contains(key))
                    continue;
                var values = new DiagnosticVector3[handles.Count];
                bool complete = true;
                for (int i = 0; i < handles.Count; i++)
                {
                    if (!row.IsAvailable(handles[i].Handle))
                    {
                        missing.Add(CharacterFootDiagnosticOperatorSupport.Identity(handles[i]));
                        complete = false;
                    }
                    else
                    {
                        values[i] = row.GetVector3(handles[i].Handle);
                    }
                }
                if (complete)
                    result.Add(key, new StageSnapshot(values));
            }
            foreach (var key in required)
            {
                if (!result.ContainsKey(key))
                    missing.Add($"stage-snapshot:{key.Dimension}:{key.Sequence.ToString(CultureInfo.InvariantCulture)}");
            }
            return result;
        }

        sealed class StageSnapshot
        {
            internal StageSnapshot(DiagnosticVector3[] values)
            {
                Values = values;
            }

            internal DiagnosticVector3[] Values { get; }
        }
    }

    internal sealed class CharacterFootSwingEnvelopeInputs
    {
        internal const string GroundLastLanding = "ground-last-landing";
        internal const string ComponentUp = "component-up";
        internal const string CorridorRadius = "corridor-radius";
        internal const string EnvelopeVertex = "envelope-vertex";

        internal CharacterFootSwingEnvelopeInputs(
            DiagnosticOperatorExecutionContext context)
        {
            LastLanding = context.Input(GroundLastLanding);
            Up = context.Input(ComponentUp);
            Radius = context.Input(CorridorRadius);
            Vertex = context.Input(EnvelopeVertex);
        }

        internal DiagnosticBoundInput LastLanding { get; }
        internal DiagnosticBoundInput Up { get; }
        internal DiagnosticBoundInput Radius { get; }
        internal DiagnosticBoundInput Vertex { get; }

        internal static DiagnosticOperatorInputSlot[] Slots()
        {
            var result = new List<DiagnosticOperatorInputSlot>(
                CharacterFootSwingOutputInputs.Slots())
            {
                CharacterFootSwingOutputInputs.Slot(
                    GroundLastLanding,
                    DiagnosticValueKind.Vector3),
                CharacterFootSwingOutputInputs.Slot(
                    ComponentUp,
                    DiagnosticValueKind.Vector3),
                CharacterFootSwingOutputInputs.Slot(
                    CorridorRadius,
                    DiagnosticValueKind.Float32),
                CharacterFootSwingOutputInputs.Slot(
                    EnvelopeVertex,
                    DiagnosticValueKind.Vector3,
                    DiagnosticDatasetCardinality.Table)
            };
            return result.ToArray();
        }
    }

    internal sealed class CharacterFootSwingActualEnvelopeCounterfactualOperator :
        IDiagnosticAnalysisOperator
    {
        const string HorizontalEpsilon = "horizontal-epsilon-meters";
        const string HeightEpsilon = "height-epsilon-meters";

        public DiagnosticOperatorDescriptor Descriptor { get; } =
            new DiagnosticOperatorDescriptor(
                "foot/swing-actual-foot-envelope-counterfactual",
                "motion",
                CharacterFootSwingEnvelopeInputs.Slots(),
                Parameters());

        public DiagnosticOperatorResult Execute(
            DiagnosticOperatorExecutionContext context)
        {
            double threshold = CharacterFootDiagnosticOperatorSupport.RequireNumber(
                context,
                "maximum-output-step-meters");
            double noise = CharacterFootDiagnosticOperatorSupport.RequireNumber(
                context,
                "position-noise-floor-meters");
            double timeEpsilon = CharacterFootDiagnosticOperatorSupport.RequireNumber(
                context,
                "time-epsilon-seconds");
            double horizontalEpsilon = CharacterFootDiagnosticOperatorSupport.RequireNumber(
                context,
                HorizontalEpsilon);
            double heightEpsilon = CharacterFootDiagnosticOperatorSupport.RequireNumber(
                context,
                HeightEpsilon);
            uint accepted = CharacterFootDiagnosticOperatorSupport.RequireUInt32(
                context,
                "accepted-motion-state");
            uint swing = CharacterFootDiagnosticOperatorSupport.RequireUInt32(
                context,
                "swing-constraint-state");
            uint groundAccepted = CharacterFootDiagnosticOperatorSupport.RequireUInt32(
                context,
                "accepted-ground-path-state");
            uint pathAvailability = CharacterFootDiagnosticOperatorSupport.RequireUInt32(
                context,
                "path-availability-reason");
            uint landingEvent = CharacterFootDiagnosticOperatorSupport.RequireUInt32(
                context,
                "landing-event-reason");
            int limit = checked((int)
                CharacterFootDiagnosticOperatorSupport.RequireUInt32(
                    context,
                    "representative-limit"));
            CharacterFootSwingOutputReadResult read =
                CharacterFootSwingOutputReader.Read(
                    context,
                    timeEpsilon,
                    noise,
                    accepted,
                    swing,
                    groundAccepted,
                    pathAvailability,
                    landingEvent);
            if (read.MissingEvidence.Count != 0)
                return DiagnosticOperatorResult.MissingEvidence(
                    CharacterFootDiagnosticOperatorSupport.MissingSummary(
                        read.MissingEvidence));
            List<CharacterFootSwingOutputPair> eligible = read.Stable
                .Where(value =>
                    value.Previous.PathIdentity == value.Current.PathIdentity &&
                    !value.Current.ResidualRebuilt)
                .ToList();
            if (eligible.Count == 0)
                return DiagnosticOperatorResult.NotApplicable(
                    "No complete accepted unanchored same-path Swing frame pair was observed.");
            var inputs = new CharacterFootSwingEnvelopeInputs(context);
            Dictionary<(string Dimension, ulong Sequence), EnvelopeFrame> frames =
                ReadEnvelopeFrames(context, inputs, eligible, read.MissingEvidence);
            Dictionary<(string Dimension, ulong Sequence), List<DiagnosticVector3>> vertices =
                ReadVertices(context, inputs, eligible);
            var observations = new List<EnvelopeObservation>(eligible.Count);
            for (int i = 0; i < eligible.Count; i++)
            {
                CharacterFootSwingOutputPair pair = eligible[i];
                var key = (pair.Current.Dimension, pair.Current.Sequence);
                if (!frames.TryGetValue(key, out EnvelopeFrame frame) ||
                    !vertices.TryGetValue(key, out List<DiagnosticVector3> values) ||
                    values.Count < 2)
                {
                    read.MissingEvidence.Add(
                        $"ground-envelope:{pair.Current.Dimension}:{pair.Current.Sequence.ToString(CultureInfo.InvariantCulture)}");
                    continue;
                }
                if (!TryObserve(
                        pair,
                        frame,
                        values,
                        horizontalEpsilon,
                        heightEpsilon,
                        out EnvelopeObservation observation))
                {
                    read.MissingEvidence.Add(
                        $"invalid-ground-envelope-geometry:{pair.Current.Dimension}:{pair.Current.Sequence.ToString(CultureInfo.InvariantCulture)}");
                    continue;
                }
                observations.Add(observation);
            }
            if (read.MissingEvidence.Count != 0)
                return DiagnosticOperatorResult.MissingEvidence(
                    CharacterFootDiagnosticOperatorSupport.MissingSummary(
                        read.MissingEvidence));
            List<EnvelopeObservation> matched = observations
                .Where(value => value.UniqueInCorridor && value.Advance > threshold)
                .OrderByDescending(value => value.Advance)
                .ThenBy(value => value.Pair.Current.Sequence)
                .Take(limit)
                .ToList();
            int uniqueCount = observations.Count(value => value.UniqueInCorridor);
            int matchCount = observations.Count(value =>
                value.UniqueInCorridor && value.Advance > threshold);
            string summary =
                $"{matchCount} of {uniqueCount} unique in-corridor actual-foot envelope observations exceeded {CharacterFootDiagnosticOperatorSupport.Format(threshold)} m advance above the Builder target; {observations.Count} eligible pairs were measured.";
            if (matched.Count == 0)
                return new DiagnosticOperatorResult(
                    DiagnosticRuleState.Passed,
                    summary,
                    null,
                    null,
                    null);
            var findings = new List<DiagnosticFinding>(matched.Count);
            for (int i = 0; i < matched.Count; i++)
            {
                EnvelopeObservation value = matched[i];
                CharacterFootSwingOutputPair pair = value.Pair;
                var evidence = new[]
                {
                    CharacterFootDiagnosticOperatorSupport.Evidence(
                        "advance-above-builder-target-meters",
                        inputs.Vertex,
                        pair.Current.Dimension,
                        pair.Previous.Sequence,
                        pair.Current.Sequence,
                        CharacterFootDiagnosticOperatorSupport.Format(value.Advance)),
                    CharacterFootDiagnosticOperatorSupport.Evidence(
                        "minimum-envelope-correction-meters",
                        inputs.Vertex,
                        pair.Current.Dimension,
                        pair.Previous.Sequence,
                        pair.Current.Sequence,
                        CharacterFootDiagnosticOperatorSupport.Format(value.MinimumCorrection)),
                    CharacterFootDiagnosticOperatorSupport.Evidence(
                        "builder-target-along-up-meters",
                        inputs.Up,
                        pair.Current.Dimension,
                        pair.Previous.Sequence,
                        pair.Current.Sequence,
                        CharacterFootDiagnosticOperatorSupport.Format(value.BuilderTarget)),
                    CharacterFootDiagnosticOperatorSupport.Evidence(
                        "cross-track-distance-meters",
                        inputs.Radius,
                        pair.Current.Dimension,
                        pair.Previous.Sequence,
                        pair.Current.Sequence,
                        CharacterFootDiagnosticOperatorSupport.Format(value.CrossTrack)),
                    CharacterFootDiagnosticOperatorSupport.Evidence(
                        "candidate-count",
                        inputs.Vertex,
                        pair.Current.Dimension,
                        pair.Previous.Sequence,
                        pair.Current.Sequence,
                        value.CandidateCount.ToString(CultureInfo.InvariantCulture)),
                    CharacterFootDiagnosticOperatorSupport.Evidence(
                        "height-span-meters",
                        inputs.Vertex,
                        pair.Current.Dimension,
                        pair.Previous.Sequence,
                        pair.Current.Sequence,
                        CharacterFootDiagnosticOperatorSupport.Format(value.HeightSpan))
                };
                findings.Add(new DiagnosticFinding(
                    CharacterFootDiagnosticOperatorSupport.FindingId(
                        context.RuleId,
                        pair.Current.Sequence),
                    DiagnosticSeverity.Information,
                    pair.Current.Dimension,
                    pair.Previous.Sequence,
                    pair.Current.Sequence,
                    $"Actual physical foot envelope requires {CharacterFootDiagnosticOperatorSupport.Format(value.Advance)} m earlier lift above the Builder target.",
                    evidence,
                    CharacterFootDiagnosticOperatorSupport.FrameRange(
                        context.Input(CharacterFootSwingOutputInputs.FrameSequence),
                        pair.Previous.Frame,
                        pair.Current.Frame)));
            }
            return new DiagnosticOperatorResult(
                DiagnosticRuleState.Failed,
                summary,
                null,
                null,
                findings);
        }

        static DiagnosticOperatorParameter[] Parameters() => new[]
        {
            CharacterFootSwingOutputJumpOperator.Number("maximum-output-step-meters"),
            CharacterFootSwingOutputJumpOperator.Number("position-noise-floor-meters"),
            CharacterFootSwingOutputJumpOperator.Number("time-epsilon-seconds"),
            CharacterFootSwingOutputJumpOperator.Integer("accepted-motion-state"),
            CharacterFootSwingOutputJumpOperator.Integer("swing-constraint-state"),
            CharacterFootSwingOutputJumpOperator.Integer("accepted-ground-path-state"),
            CharacterFootSwingOutputJumpOperator.Integer("path-availability-reason"),
            CharacterFootSwingOutputJumpOperator.Integer("landing-event-reason"),
            CharacterFootSwingOutputJumpOperator.Integer("representative-limit", 1d),
            CharacterFootSwingOutputJumpOperator.Number(HorizontalEpsilon),
            CharacterFootSwingOutputJumpOperator.Number(HeightEpsilon)
        };

        static Dictionary<(string Dimension, ulong Sequence), EnvelopeFrame> ReadEnvelopeFrames(
            DiagnosticOperatorExecutionContext context,
            CharacterFootSwingEnvelopeInputs inputs,
            IReadOnlyList<CharacterFootSwingOutputPair> pairs,
            ISet<string> missing)
        {
            var required = new HashSet<(string Dimension, ulong Sequence)>(
                pairs.Select(value =>
                    (value.Current.Dimension, value.Current.Sequence)));
            var result = new Dictionary<(string Dimension, ulong Sequence), EnvelopeFrame>();
            DiagnosticDatasetCursor cursor = context.CreateCursor(0);
            while (cursor.MoveNext())
            {
                DiagnosticDatasetRow row = cursor.Current;
                var key = (row.SampleKey.DimensionId, row.SampleKey.Sequence);
                if (!required.Contains(key))
                    continue;
                DiagnosticBoundInput[] values = { inputs.LastLanding, inputs.Up, inputs.Radius };
                bool complete = true;
                for (int i = 0; i < values.Length; i++)
                {
                    if (row.IsAvailable(values[i].Handle))
                        continue;
                    missing.Add(CharacterFootDiagnosticOperatorSupport.Identity(values[i]));
                    complete = false;
                }
                if (complete)
                {
                    result.Add(key, new EnvelopeFrame(
                        row.GetVector3(inputs.LastLanding.Handle),
                        row.GetVector3(inputs.Up.Handle),
                        row.GetFloat32(inputs.Radius.Handle)));
                }
            }
            return result;
        }

        static Dictionary<(string Dimension, ulong Sequence), List<DiagnosticVector3>> ReadVertices(
            DiagnosticOperatorExecutionContext context,
            CharacterFootSwingEnvelopeInputs inputs,
            IReadOnlyList<CharacterFootSwingOutputPair> pairs)
        {
            var required = new HashSet<(string Dimension, ulong Sequence)>(
                pairs.Select(value =>
                    (value.Current.Dimension, value.Current.Sequence)));
            var result = new Dictionary<(string Dimension, ulong Sequence), List<DiagnosticVector3>>();
            int inputIndex = context.InputCount - 1;
            DiagnosticDatasetCursor cursor = context.CreateCursor(inputIndex);
            while (cursor.MoveNext())
            {
                DiagnosticDatasetRow row = cursor.Current;
                var key = (row.SampleKey.DimensionId, row.SampleKey.Sequence);
                if (!required.Contains(key) || !row.IsAvailable(inputs.Vertex.Handle))
                    continue;
                if (!result.TryGetValue(key, out List<DiagnosticVector3> values))
                {
                    values = new List<DiagnosticVector3>();
                    result.Add(key, values);
                }
                values.Add(row.GetVector3(inputs.Vertex.Handle));
            }
            return result;
        }

        static bool TryObserve(
            CharacterFootSwingOutputPair pair,
            EnvelopeFrame frame,
            IReadOnlyList<DiagnosticVector3> vertices,
            double horizontalEpsilon,
            double heightEpsilon,
            out EnvelopeObservation result)
        {
            result = null;
            if (!CharacterFootDiagnosticOperatorSupport.TryNormalize(
                    frame.Up,
                    out double upX,
                    out double upY,
                    out double upZ) ||
                !double.IsFinite(frame.Radius) ||
                frame.Radius <= 0d)
            {
                return false;
            }
            DiagnosticVector3 path = Project(
                CharacterFootSwingOutputReader.Subtract(
                    pair.Current.GroundPoint,
                    frame.LastLanding),
                upX,
                upY,
                upZ);
            double pathLength = CharacterFootSwingOutputReader.Length(path);
            if (pathLength <= 0.0001d)
                return false;
            DiagnosticVector3 direction = CharacterFootSwingOutputReader.Scale(
                path,
                1d / pathLength);
            DiagnosticVector3 physicalSole = CharacterFootDiagnosticOperatorSupport.Midpoint(
                pair.Current.PhysicalHeel,
                pair.Current.PhysicalToe);
            DiagnosticVector3 sourceSole = CharacterFootDiagnosticOperatorSupport.Midpoint(
                pair.Current.SourceHeel,
                pair.Current.SourceToe);
            DiagnosticVector3 actualOffset = Project(
                CharacterFootSwingOutputReader.Subtract(physicalSole, frame.LastLanding),
                upX,
                upY,
                upZ);
            double actualDistance = Dot(actualOffset, direction);
            double parameter = Math.Max(0d, Math.Min(1d, actualDistance / pathLength));
            DiagnosticVector3 closest = CharacterFootSwingOutputReader.Scale(path, parameter);
            double crossTrack = CharacterFootSwingOutputReader.Length(
                CharacterFootSwingOutputReader.Subtract(actualOffset, closest));
            bool inCorridor = actualDistance >= -horizontalEpsilon &&
                actualDistance <= pathLength + horizontalEpsilon &&
                crossTrack <= frame.Radius + horizontalEpsilon;
            var heights = new List<double>(vertices.Count * 2);
            bool verticalEdge = false;
            for (int i = 1; i < vertices.Count; i++)
            {
                DiagnosticVector3 previousOffset = CharacterFootSwingOutputReader.Subtract(
                    vertices[i - 1],
                    frame.LastLanding);
                DiagnosticVector3 currentOffset = CharacterFootSwingOutputReader.Subtract(
                    vertices[i],
                    frame.LastLanding);
                double previousDistance = Dot(previousOffset, direction);
                double currentDistance = Dot(currentOffset, direction);
                if (actualDistance < Math.Min(previousDistance, currentDistance) - horizontalEpsilon ||
                    actualDistance > Math.Max(previousDistance, currentDistance) + horizontalEpsilon)
                {
                    continue;
                }
                double previousHeight = Dot(vertices[i - 1], upX, upY, upZ);
                double currentHeight = Dot(vertices[i], upX, upY, upZ);
                double distanceDelta = currentDistance - previousDistance;
                if (Math.Abs(distanceDelta) <= horizontalEpsilon)
                {
                    if (Math.Abs(actualDistance - previousDistance) > horizontalEpsilon)
                        continue;
                    AddUnique(heights, previousHeight, heightEpsilon);
                    AddUnique(heights, currentHeight, heightEpsilon);
                    verticalEdge |= Math.Abs(currentHeight - previousHeight) > heightEpsilon;
                }
                else
                {
                    double interpolation = Math.Max(
                        0d,
                        Math.Min(1d, (actualDistance - previousDistance) / distanceDelta));
                    AddUnique(
                        heights,
                        previousHeight + (currentHeight - previousHeight) * interpolation,
                        heightEpsilon);
                }
            }
            if (heights.Count == 0)
            {
                result = new EnvelopeObservation(pair, false, crossTrack, 0, 0d, 0d, 0d, 0d);
                return true;
            }
            double minimum = heights.Min();
            double maximum = heights.Max();
            double heightSpan = maximum - minimum;
            bool unique = !verticalEdge && !(heights.Count > 1 && heightSpan > heightEpsilon);
            double sourceHeight = Dot(sourceSole, upX, upY, upZ);
            double minimumCorrection = minimum - sourceHeight;
            double builderTarget = Dot(
                pair.Current.TargetCorrection,
                upX,
                upY,
                upZ);
            double advance = Math.Max(0d, minimumCorrection - builderTarget);
            result = new EnvelopeObservation(
                pair,
                inCorridor && unique,
                crossTrack,
                heights.Count,
                heightSpan,
                minimumCorrection,
                builderTarget,
                advance);
            return true;
        }

        static DiagnosticVector3 Project(
            in DiagnosticVector3 value,
            double x,
            double y,
            double z)
        {
            double along = Dot(value, x, y, z);
            return new DiagnosticVector3(
                (float)(value.X - along * x),
                (float)(value.Y - along * y),
                (float)(value.Z - along * z));
        }

        static double Dot(
            in DiagnosticVector3 left,
            in DiagnosticVector3 right) =>
            left.X * right.X + left.Y * right.Y + left.Z * right.Z;

        static double Dot(
            in DiagnosticVector3 value,
            double x,
            double y,
            double z) =>
            value.X * x + value.Y * y + value.Z * z;

        static void AddUnique(ICollection<double> values, double value, double epsilon)
        {
            foreach (double existing in values)
            {
                if (Math.Abs(existing - value) <= epsilon)
                    return;
            }
            values.Add(value);
        }

        sealed class EnvelopeFrame
        {
            internal EnvelopeFrame(
                in DiagnosticVector3 lastLanding,
                in DiagnosticVector3 up,
                double radius)
            {
                LastLanding = lastLanding;
                Up = up;
                Radius = radius;
            }

            internal DiagnosticVector3 LastLanding { get; }
            internal DiagnosticVector3 Up { get; }
            internal double Radius { get; }
        }

        sealed class EnvelopeObservation
        {
            internal EnvelopeObservation(
                CharacterFootSwingOutputPair pair,
                bool uniqueInCorridor,
                double crossTrack,
                int candidateCount,
                double heightSpan,
                double minimumCorrection,
                double builderTarget,
                double advance)
            {
                Pair = pair;
                UniqueInCorridor = uniqueInCorridor;
                CrossTrack = crossTrack;
                CandidateCount = candidateCount;
                HeightSpan = heightSpan;
                MinimumCorrection = minimumCorrection;
                BuilderTarget = builderTarget;
                Advance = advance;
            }

            internal CharacterFootSwingOutputPair Pair { get; }
            internal bool UniqueInCorridor { get; }
            internal double CrossTrack { get; }
            internal int CandidateCount { get; }
            internal double HeightSpan { get; }
            internal double MinimumCorrection { get; }
            internal double BuilderTarget { get; }
            internal double Advance { get; }
        }
    }

    internal sealed class CharacterFootSwingCorrectionCadenceOperator :
        IDiagnosticAnalysisOperator
    {
        static readonly string[] s_Slots =
        {
            "frame-sequence",
            "reset-sequence",
            "motion-state",
            "constraint-state",
            "event-identity",
            "formal-source-identity",
            "formal-source-cycle",
            "path-identity",
            "path-revision-reason",
            "path-residual-rebuilt",
            "output-stages-available",
            "correction-response-evaluated",
            "final-effective-correction",
            "formal-foot-height",
            "correction-response-desired",
            "correction-response-previous",
            "correction-response-current",
            "correction-response-applied-delta",
            "correction-response-selected-speed",
            "response-output-point",
            "envelope-sample",
            "original-sole",
            "correction-response-direction"
        };

        static readonly DiagnosticValueKind[] s_Kinds =
        {
            DiagnosticValueKind.UInt64,
            DiagnosticValueKind.UInt64,
            DiagnosticValueKind.UInt32,
            DiagnosticValueKind.UInt32,
            DiagnosticValueKind.UInt64,
            DiagnosticValueKind.Identity,
            DiagnosticValueKind.Int32,
            DiagnosticValueKind.UInt64,
            DiagnosticValueKind.UInt32,
            DiagnosticValueKind.Boolean,
            DiagnosticValueKind.Boolean,
            DiagnosticValueKind.Boolean,
            DiagnosticValueKind.Vector3,
            DiagnosticValueKind.Float32,
            DiagnosticValueKind.Float32,
            DiagnosticValueKind.Float32,
            DiagnosticValueKind.Float32,
            DiagnosticValueKind.Float32,
            DiagnosticValueKind.Float32,
            DiagnosticValueKind.Vector3,
            DiagnosticValueKind.Vector3,
            DiagnosticValueKind.Vector3,
            DiagnosticValueKind.Vector3
        };

        public DiagnosticOperatorDescriptor Descriptor { get; } =
            new DiagnosticOperatorDescriptor(
                "foot/stable-swing-correction-response-cadence",
                "motion",
                Slots(),
                new[]
                {
                    CharacterFootSwingOutputJumpOperator.Number("hold-maximum-meters"),
                    CharacterFootSwingOutputJumpOperator.Number("advance-minimum-meters"),
                    CharacterFootSwingOutputJumpOperator.Integer("accepted-motion-state"),
                    CharacterFootSwingOutputJumpOperator.Integer("swing-constraint-state"),
                    CharacterFootSwingOutputJumpOperator.Integer("no-path-revision-reason"),
                    CharacterFootSwingOutputJumpOperator.Integer("representative-limit", 1d)
                });

        public DiagnosticOperatorResult Execute(
            DiagnosticOperatorExecutionContext context)
        {
            double hold = CharacterFootDiagnosticOperatorSupport.RequireNumber(
                context,
                "hold-maximum-meters");
            double advance = CharacterFootDiagnosticOperatorSupport.RequireNumber(
                context,
                "advance-minimum-meters");
            uint accepted = CharacterFootDiagnosticOperatorSupport.RequireUInt32(
                context,
                "accepted-motion-state");
            uint swing = CharacterFootDiagnosticOperatorSupport.RequireUInt32(
                context,
                "swing-constraint-state");
            uint noRevision = CharacterFootDiagnosticOperatorSupport.RequireUInt32(
                context,
                "no-path-revision-reason");
            int limit = checked((int)
                CharacterFootDiagnosticOperatorSupport.RequireUInt32(
                    context,
                    "representative-limit"));
            DiagnosticBoundInput[] inputs = s_Slots.Select(context.Input).ToArray();
            var missing = new HashSet<string>(StringComparer.Ordinal);
            var history = new Dictionary<string, List<CadenceFrame>>(StringComparer.Ordinal);
            var observations = new List<CadenceObservation>();
            DiagnosticDatasetCursor cursor = context.CreateCursor(0);
            while (cursor.MoveNext())
            {
                DiagnosticDatasetRow row = cursor.Current;
                string dimension = row.SampleKey.DimensionId;
                if (!context.IncludesDimension(dimension) || !context.MatchesFilters(row))
                {
                    history.Remove(dimension);
                    continue;
                }
                bool complete = true;
                for (int i = 0; i < 4; i++)
                {
                    if (row.IsAvailable(inputs[i].Handle))
                        continue;
                    missing.Add(CharacterFootDiagnosticOperatorSupport.Identity(inputs[i]));
                    complete = false;
                }
                if (!complete)
                {
                    history.Remove(dimension);
                    continue;
                }
                uint motionState = row.GetUInt32(inputs[2].Handle);
                uint constraintState = row.GetUInt32(inputs[3].Handle);
                if (motionState != accepted || constraintState != swing)
                {
                    history.Remove(dimension);
                    continue;
                }
                for (int i = 4; i < inputs.Length; i++)
                {
                    if (row.IsAvailable(inputs[i].Handle))
                        continue;
                    missing.Add(CharacterFootDiagnosticOperatorSupport.Identity(inputs[i]));
                    complete = false;
                }
                if (!complete)
                {
                    history.Remove(dimension);
                    continue;
                }
                CadenceFrame frame = CadenceFrame.Read(row, inputs);
                if (!history.TryGetValue(dimension, out List<CadenceFrame> values))
                {
                    values = new List<CadenceFrame>(2);
                    history.Add(dimension, values);
                }
                if (values.Count == 2 && Eligible(
                        values[0],
                        values[1],
                        frame,
                        accepted,
                        swing,
                        noRevision))
                {
                    double previousStep = CharacterFootDiagnosticOperatorSupport.Distance(
                        values[0].FinalCorrection,
                        values[1].FinalCorrection);
                    double currentStep = CharacterFootDiagnosticOperatorSupport.Distance(
                        values[1].FinalCorrection,
                        frame.FinalCorrection);
                    bool holdToAdvance = previousStep < hold && currentStep > advance;
                    bool advanceToHold = previousStep > advance && currentStep < hold;
                    observations.Add(new CadenceObservation(
                        values[0],
                        values[1],
                        frame,
                        previousStep,
                        currentStep,
                        holdToAdvance,
                        advanceToHold));
                }
                values.Add(frame);
                if (values.Count > 2)
                    values.RemoveAt(0);
            }
            if (missing.Count != 0)
                return DiagnosticOperatorResult.MissingEvidence(
                    CharacterFootDiagnosticOperatorSupport.MissingSummary(missing));
            if (observations.Count == 0)
                return DiagnosticOperatorResult.NotApplicable(
                    "No complete accepted same-source, same-event and same-path Swing frame triple was observed.");
            List<CadenceObservation> matched = observations
                .Where(value => value.HoldToAdvance || value.AdvanceToHold)
                .OrderByDescending(value => Math.Max(value.PreviousStep, value.CurrentStep))
                .ThenBy(value => value.Current.Sequence)
                .Take(limit)
                .ToList();
            int matchCount = observations.Count(value =>
                value.HoldToAdvance || value.AdvanceToHold);
            string summary =
                $"{matchCount} of {observations.Count} stable Swing frame triples switched between below {CharacterFootDiagnosticOperatorSupport.Format(hold)} m hold and above {CharacterFootDiagnosticOperatorSupport.Format(advance)} m advance.";
            if (matched.Count == 0)
                return new DiagnosticOperatorResult(
                    DiagnosticRuleState.Passed,
                    summary,
                    null,
                    null,
                    null);
            var findings = new List<DiagnosticFinding>(matched.Count);
            for (int i = 0; i < matched.Count; i++)
            {
                CadenceObservation value = matched[i];
                string transition = value.HoldToAdvance
                    ? "HoldToAdvance"
                    : "AdvanceToHold";
                string firstStage = value.FirstLargeStage(advance);
                var evidence = new[]
                {
                    Evidence(inputs[12], value, "previous-final-effective-correction-step-meters", value.PreviousStep),
                    Evidence(inputs[12], value, "current-final-effective-correction-step-meters", value.CurrentStep),
                    Evidence(inputs[14], value, "previous-desired-response-delta", value.Previous.Desired - value.First.Desired),
                    Evidence(inputs[14], value, "current-desired-response-delta", value.Current.Desired - value.Previous.Desired),
                    Evidence(inputs[15], value, "previous-correction-response-previous", value.Previous.ResponsePrevious),
                    Evidence(inputs[16], value, "previous-correction-response-current", value.Previous.ResponseCurrent),
                    Evidence(inputs[15], value, "current-correction-response-previous", value.Current.ResponsePrevious),
                    Evidence(inputs[16], value, "current-correction-response-current", value.Current.ResponseCurrent),
                    Evidence(inputs[17], value, "previous-applied-response-delta", value.Previous.AppliedDelta),
                    Evidence(inputs[17], value, "current-applied-response-delta", value.Current.AppliedDelta),
                    Evidence(inputs[18], value, "previous-selected-response-speed", value.Previous.SelectedSpeed),
                    Evidence(inputs[18], value, "current-selected-response-speed", value.Current.SelectedSpeed),
                    Evidence(inputs[19], value, "previous-response-output-step-meters", CharacterFootDiagnosticOperatorSupport.Distance(value.First.ResponseOutput, value.Previous.ResponseOutput)),
                    Evidence(inputs[19], value, "current-response-output-step-meters", CharacterFootDiagnosticOperatorSupport.Distance(value.Previous.ResponseOutput, value.Current.ResponseOutput)),
                    Evidence(inputs[13], value, "previous-formal-foot-height-delta", value.Previous.FormalHeight - value.First.FormalHeight),
                    Evidence(inputs[13], value, "current-formal-foot-height-delta", value.Current.FormalHeight - value.Previous.FormalHeight),
                    Evidence(inputs[20], value, "previous-envelope-sample-step-meters", CharacterFootDiagnosticOperatorSupport.Distance(value.First.Envelope, value.Previous.Envelope)),
                    Evidence(inputs[20], value, "current-envelope-sample-step-meters", CharacterFootDiagnosticOperatorSupport.Distance(value.Previous.Envelope, value.Current.Envelope)),
                    Evidence(inputs[21], value, "previous-original-sole-step-meters", CharacterFootDiagnosticOperatorSupport.Distance(value.First.OriginalSole, value.Previous.OriginalSole)),
                    Evidence(inputs[21], value, "current-original-sole-step-meters", CharacterFootDiagnosticOperatorSupport.Distance(value.Previous.OriginalSole, value.Current.OriginalSole)),
                    Evidence(inputs[22], value, "previous-envelope-direction-contribution", Dot(CharacterFootSwingOutputReader.Subtract(value.Previous.Envelope, value.First.Envelope), value.Previous.Direction)),
                    Evidence(inputs[22], value, "current-envelope-direction-contribution", Dot(CharacterFootSwingOutputReader.Subtract(value.Current.Envelope, value.Previous.Envelope), value.Current.Direction)),
                    Evidence(inputs[22], value, "previous-original-sole-direction-contribution", -Dot(CharacterFootSwingOutputReader.Subtract(value.Previous.OriginalSole, value.First.OriginalSole), value.Previous.Direction)),
                    Evidence(inputs[22], value, "current-original-sole-direction-contribution", -Dot(CharacterFootSwingOutputReader.Subtract(value.Current.OriginalSole, value.Previous.OriginalSole), value.Current.Direction)),
                    Evidence(inputs[12], value, "cadence-transition", transition),
                    Evidence(inputs[12], value, "first-large-step-stage", firstStage)
                };
                findings.Add(new DiagnosticFinding(
                    CharacterFootDiagnosticOperatorSupport.FindingId(
                        context.RuleId,
                        value.Current.Sequence),
                    DiagnosticSeverity.Information,
                    value.Current.Dimension,
                    value.First.Sequence,
                    value.Current.Sequence,
                    $"Stable Swing correction response changed cadence from {transition} at {firstStage}.",
                    evidence,
                    CharacterFootDiagnosticOperatorSupport.FrameRange(
                        inputs[0],
                        value.First.Frame,
                        value.Current.Frame)));
            }
            return new DiagnosticOperatorResult(
                DiagnosticRuleState.Failed,
                summary,
                null,
                null,
                findings);
        }

        static DiagnosticOperatorInputSlot[] Slots()
        {
            var result = new DiagnosticOperatorInputSlot[s_Slots.Length];
            for (int i = 0; i < result.Length; i++)
                result[i] = CharacterFootSwingOutputInputs.Slot(s_Slots[i], s_Kinds[i]);
            return result;
        }

        static bool Eligible(
            CadenceFrame first,
            CadenceFrame previous,
            CadenceFrame current,
            uint accepted,
            uint swing,
            uint noRevision) =>
            first.Frame + 1 == previous.Frame &&
            previous.Frame + 1 == current.Frame &&
            first.Reset == previous.Reset &&
            previous.Reset == current.Reset &&
            first.MotionState == accepted &&
            previous.MotionState == accepted &&
            current.MotionState == accepted &&
            first.ConstraintState == swing &&
            previous.ConstraintState == swing &&
            current.ConstraintState == swing &&
            first.EventIdentity != 0 &&
            first.EventIdentity == previous.EventIdentity &&
            previous.EventIdentity == current.EventIdentity &&
            first.PathIdentity != 0 &&
            first.PathIdentity == previous.PathIdentity &&
            previous.PathIdentity == current.PathIdentity &&
            string.Equals(first.SourceIdentity, current.SourceIdentity, StringComparison.Ordinal) &&
            string.Equals(previous.SourceIdentity, current.SourceIdentity, StringComparison.Ordinal) &&
            first.SourceCycle == current.SourceCycle &&
            previous.SourceCycle == current.SourceCycle &&
            current.RevisionReason == noRevision &&
            !current.ResidualRebuilt &&
            first.OutputAvailable &&
            previous.OutputAvailable &&
            current.OutputAvailable &&
            first.ResponseEvaluated &&
            previous.ResponseEvaluated &&
            current.ResponseEvaluated;

        static DiagnosticEvidence Evidence(
            in DiagnosticBoundInput input,
            CadenceObservation value,
            string id,
            double number) => Evidence(
                input,
                value,
                id,
                CharacterFootDiagnosticOperatorSupport.Format(number));

        static DiagnosticEvidence Evidence(
            in DiagnosticBoundInput input,
            CadenceObservation value,
            string id,
            string text) =>
            CharacterFootDiagnosticOperatorSupport.Evidence(
                id,
                input,
                value.Current.Dimension,
                value.First.Sequence,
                value.Current.Sequence,
                text);

        static double Dot(
            in DiagnosticVector3 left,
            in DiagnosticVector3 right) =>
            left.X * right.X + left.Y * right.Y + left.Z * right.Z;

        sealed class CadenceFrame
        {
            internal string Dimension;
            internal ulong Sequence;
            internal ulong Frame;
            internal ulong Reset;
            internal uint MotionState;
            internal uint ConstraintState;
            internal ulong EventIdentity;
            internal string SourceIdentity;
            internal int SourceCycle;
            internal ulong PathIdentity;
            internal uint RevisionReason;
            internal bool ResidualRebuilt;
            internal bool OutputAvailable;
            internal bool ResponseEvaluated;
            internal DiagnosticVector3 FinalCorrection;
            internal double FormalHeight;
            internal double Desired;
            internal double ResponsePrevious;
            internal double ResponseCurrent;
            internal double AppliedDelta;
            internal double SelectedSpeed;
            internal DiagnosticVector3 ResponseOutput;
            internal DiagnosticVector3 Envelope;
            internal DiagnosticVector3 OriginalSole;
            internal DiagnosticVector3 Direction;

            internal static CadenceFrame Read(
                in DiagnosticDatasetRow row,
                IReadOnlyList<DiagnosticBoundInput> inputs) =>
                new CadenceFrame
                {
                    Dimension = row.SampleKey.DimensionId,
                    Sequence = row.SampleKey.Sequence,
                    Frame = row.GetUInt64(inputs[0].Handle),
                    Reset = row.GetUInt64(inputs[1].Handle),
                    MotionState = row.GetUInt32(inputs[2].Handle),
                    ConstraintState = row.GetUInt32(inputs[3].Handle),
                    EventIdentity = row.GetUInt64(inputs[4].Handle),
                    SourceIdentity = row.GetIdentity(inputs[5].Handle),
                    SourceCycle = row.GetInt32(inputs[6].Handle),
                    PathIdentity = row.GetUInt64(inputs[7].Handle),
                    RevisionReason = row.GetUInt32(inputs[8].Handle),
                    ResidualRebuilt = row.GetBoolean(inputs[9].Handle),
                    OutputAvailable = row.GetBoolean(inputs[10].Handle),
                    ResponseEvaluated = row.GetBoolean(inputs[11].Handle),
                    FinalCorrection = row.GetVector3(inputs[12].Handle),
                    FormalHeight = row.GetFloat32(inputs[13].Handle),
                    Desired = row.GetFloat32(inputs[14].Handle),
                    ResponsePrevious = row.GetFloat32(inputs[15].Handle),
                    ResponseCurrent = row.GetFloat32(inputs[16].Handle),
                    AppliedDelta = row.GetFloat32(inputs[17].Handle),
                    SelectedSpeed = row.GetFloat32(inputs[18].Handle),
                    ResponseOutput = row.GetVector3(inputs[19].Handle),
                    Envelope = row.GetVector3(inputs[20].Handle),
                    OriginalSole = row.GetVector3(inputs[21].Handle),
                    Direction = row.GetVector3(inputs[22].Handle)
                };
        }

        sealed class CadenceObservation
        {
            internal CadenceObservation(
                CadenceFrame first,
                CadenceFrame previous,
                CadenceFrame current,
                double previousStep,
                double currentStep,
                bool holdToAdvance,
                bool advanceToHold)
            {
                First = first;
                Previous = previous;
                Current = current;
                PreviousStep = previousStep;
                CurrentStep = currentStep;
                HoldToAdvance = holdToAdvance;
                AdvanceToHold = advanceToHold;
            }

            internal CadenceFrame First { get; }
            internal CadenceFrame Previous { get; }
            internal CadenceFrame Current { get; }
            internal double PreviousStep { get; }
            internal double CurrentStep { get; }
            internal bool HoldToAdvance { get; }
            internal bool AdvanceToHold { get; }

            internal string FirstLargeStage(double advance)
            {
                bool current = HoldToAdvance || !AdvanceToHold;
                CadenceFrame before = current ? Previous : First;
                CadenceFrame after = current ? Current : Previous;
                if (Math.Abs(after.FormalHeight - before.FormalHeight) > advance)
                    return "FormalFootHeight";
                if (Math.Abs(after.Desired - before.Desired) > advance)
                    return "DesiredResponse";
                if (Math.Abs(after.AppliedDelta) > advance)
                    return "CorrectionResponseScalar";
                return "FinalEffectiveCorrection";
            }
        }
    }
}
