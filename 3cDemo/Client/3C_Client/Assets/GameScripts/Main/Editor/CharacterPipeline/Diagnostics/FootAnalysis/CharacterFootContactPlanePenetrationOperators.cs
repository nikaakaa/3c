using System;
using System.Collections.Generic;
using KK.GeneratedDiagnosticSampling;
using KK.GeneratedDiagnosticSampling.Host;

namespace ThirdPersonCharacter.Pipeline.Diagnostics.FootAnalysis.Editor
{
    internal enum CharacterFootPenetrationResponsibility
    {
        Clear,
        Introduced,
        Amplified,
        PartiallyResolved,
        Resolved,
        BaselineResidual
    }

    internal readonly struct CharacterFootPenetrationLine
    {
        internal CharacterFootPenetrationLine(
            double heelDepth,
            double toeDepth,
            double meanDepth,
            double lengthCoefficient)
        {
            HeelDepth = heelDepth;
            ToeDepth = toeDepth;
            MeanDepth = meanDepth;
            LengthCoefficient = lengthCoefficient;
        }

        internal double HeelDepth { get; }
        internal double ToeDepth { get; }
        internal double MaximumDepth => Math.Max(HeelDepth, ToeDepth);
        internal double MeanDepth { get; }
        internal double LengthCoefficient { get; }
    }

    internal sealed class CharacterFootContactPlaneInputs
    {
        internal const string FrameSequence = "frame-sequence";
        internal const string ResetSequence = "reset-sequence";
        internal const string DeltaSeconds = "delta-seconds";
        internal const string ConstraintState = "constraint-state";
        internal const string EventIdentity = "event-identity";
        internal const string SurfaceIdentity = "surface-identity";
        internal const string ContactPlaneAvailable = "contact-plane-available";
        internal const string AnchorAvailable = "anchor-available";
        internal const string AnchorEventIdentity = "anchor-event-identity";
        internal const string AnchorSurfaceIdentity = "anchor-surface-identity";
        internal const string AnchorPoint = "anchor-point";
        internal const string AnchorNormal = "anchor-normal";
        internal const string SourceAnklePosition = "source-ankle-position";
        internal const string SourceAnkleRotation = "source-ankle-rotation";
        internal const string SourceHeel = "source-heel";
        internal const string SourceToe = "source-toe";
        internal const string PhysicalWriteAvailable = "physical-write-available";
        internal const string PhysicalAnklePosition = "physical-ankle-position";
        internal const string PhysicalAnkleRotation = "physical-ankle-rotation";

        internal CharacterFootContactPlaneInputs(
            DiagnosticOperatorExecutionContext context)
        {
            Frame = context.Input(FrameSequence);
            Reset = context.Input(ResetSequence);
            Delta = context.Input(DeltaSeconds);
            State = context.Input(ConstraintState);
            Event = context.Input(EventIdentity);
            Surface = context.Input(SurfaceIdentity);
            PlaneAvailable = context.Input(ContactPlaneAvailable);
            AnchorIsAvailable = context.Input(AnchorAvailable);
            AnchorEvent = context.Input(AnchorEventIdentity);
            AnchorSurface = context.Input(AnchorSurfaceIdentity);
            Anchor = context.Input(AnchorPoint);
            Normal = context.Input(AnchorNormal);
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
        internal DiagnosticBoundInput State { get; }
        internal DiagnosticBoundInput Event { get; }
        internal DiagnosticBoundInput Surface { get; }
        internal DiagnosticBoundInput PlaneAvailable { get; }
        internal DiagnosticBoundInput AnchorIsAvailable { get; }
        internal DiagnosticBoundInput AnchorEvent { get; }
        internal DiagnosticBoundInput AnchorSurface { get; }
        internal DiagnosticBoundInput Anchor { get; }
        internal DiagnosticBoundInput Normal { get; }
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
            Slot(ConstraintState, DiagnosticValueKind.UInt32),
            Slot(EventIdentity, DiagnosticValueKind.UInt64),
            Slot(SurfaceIdentity, DiagnosticValueKind.Int32),
            Slot(ContactPlaneAvailable, DiagnosticValueKind.Boolean),
            Slot(AnchorAvailable, DiagnosticValueKind.Boolean),
            Slot(AnchorEventIdentity, DiagnosticValueKind.UInt64),
            Slot(AnchorSurfaceIdentity, DiagnosticValueKind.Int32),
            Slot(AnchorPoint, DiagnosticValueKind.Vector3),
            Slot(AnchorNormal, DiagnosticValueKind.Vector3),
            Slot(SourceAnklePosition, DiagnosticValueKind.Vector3),
            Slot(SourceAnkleRotation, DiagnosticValueKind.Quaternion),
            Slot(SourceHeel, DiagnosticValueKind.Vector3),
            Slot(SourceToe, DiagnosticValueKind.Vector3),
            Slot(PhysicalWriteAvailable, DiagnosticValueKind.Boolean),
            Slot(PhysicalAnklePosition, DiagnosticValueKind.Vector3),
            Slot(PhysicalAnkleRotation, DiagnosticValueKind.Quaternion)
        };

        static DiagnosticOperatorInputSlot Slot(
            string id,
            DiagnosticValueKind kind) =>
            new DiagnosticOperatorInputSlot(
                id,
                kind,
                DiagnosticDatasetCardinality.Main,
                true);
    }

    internal sealed class CharacterFootContactPlaneSegment
    {
        readonly double m_GeometryEpsilon;

        internal CharacterFootContactPlaneSegment(
            string dimensionId,
            ulong sequence,
            ulong frame,
            ulong reset,
            ulong eventIdentity,
            int surfaceIdentity,
            uint constraintState,
            double geometryEpsilon)
        {
            DimensionId = dimensionId;
            SequenceStart = sequence;
            SequenceEnd = sequence;
            FrameStart = frame;
            FrameEnd = frame;
            ResetSequence = reset;
            EventIdentity = eventIdentity;
            SurfaceIdentity = surfaceIdentity;
            ConstraintState = constraintState;
            m_GeometryEpsilon = geometryEpsilon;
        }

        internal string DimensionId { get; }
        internal ulong SequenceStart { get; }
        internal ulong SequenceEnd { get; private set; }
        internal ulong FrameStart { get; }
        internal ulong FrameEnd { get; private set; }
        internal ulong ResetSequence { get; }
        internal ulong EventIdentity { get; }
        internal int SurfaceIdentity { get; }
        internal uint ConstraintState { get; }
        internal int FrameCount { get; private set; }
        internal int PenetratingFrameCount { get; private set; }
        internal double DurationSeconds { get; private set; }
        internal double FinalDepthTimeIntegral { get; private set; }
        internal double FinalHeelDepthMaximum { get; private set; }
        internal double FinalToeDepthMaximum { get; private set; }
        internal double FinalDepthMaximum { get; private set; }
        internal double FinalMeanDepthMaximum { get; private set; }
        internal double FinalLengthCoefficientMaximum { get; private set; }
        internal double SourceDepthMaximum { get; private set; }
        internal double IntroducedDepthMaximum { get; private set; }
        internal double ResolvedDepthMaximum { get; private set; }
        internal int IntroducedFrameCount { get; private set; }
        internal int AmplifiedFrameCount { get; private set; }
        internal int PartiallyResolvedFrameCount { get; private set; }
        internal int ResolvedFrameCount { get; private set; }
        internal int BaselineResidualFrameCount { get; private set; }
        internal ulong PeakSequence { get; private set; }
        internal DiagnosticVector3 PeakSourceHeel { get; private set; }
        internal DiagnosticVector3 PeakSourceToe { get; private set; }
        internal DiagnosticVector3 PeakPhysicalHeel { get; private set; }
        internal DiagnosticVector3 PeakPhysicalToe { get; private set; }
        internal DiagnosticVector3 PeakAnchor { get; private set; }
        internal DiagnosticVector3 PeakNormal { get; private set; }

        internal bool Continues(
            ulong frame,
            ulong reset,
            ulong eventIdentity,
            int surfaceIdentity,
            uint constraintState) =>
            frame == FrameEnd + 1 &&
            reset == ResetSequence &&
            eventIdentity == EventIdentity &&
            surfaceIdentity == SurfaceIdentity &&
            constraintState == ConstraintState;

        internal void Add(
            in DiagnosticDatasetRow row,
            ulong frame,
            double deltaSeconds,
            in DiagnosticVector3 sourceHeel,
            in DiagnosticVector3 sourceToe,
            in DiagnosticVector3 physicalHeel,
            in DiagnosticVector3 physicalToe,
            in DiagnosticVector3 anchor,
            in DiagnosticVector3 normal,
            double normalX,
            double normalY,
            double normalZ)
        {
            CharacterFootPenetrationLine source = ResolveLine(
                CharacterFootDiagnosticOperatorSupport.Clearance(
                    sourceHeel, anchor, normalX, normalY, normalZ),
                CharacterFootDiagnosticOperatorSupport.Clearance(
                    sourceToe, anchor, normalX, normalY, normalZ));
            CharacterFootPenetrationLine final = ResolveLine(
                CharacterFootDiagnosticOperatorSupport.Clearance(
                    physicalHeel, anchor, normalX, normalY, normalZ),
                CharacterFootDiagnosticOperatorSupport.Clearance(
                    physicalToe, anchor, normalX, normalY, normalZ));
            double introducedHeel = Math.Max(0d, final.HeelDepth - source.HeelDepth);
            double introducedToe = Math.Max(0d, final.ToeDepth - source.ToeDepth);
            double resolvedHeel = Math.Max(0d, source.HeelDepth - final.HeelDepth);
            double resolvedToe = Math.Max(0d, source.ToeDepth - final.ToeDepth);
            CharacterFootPenetrationResponsibility heel = Responsibility(
                source.HeelDepth,
                final.HeelDepth,
                m_GeometryEpsilon);
            CharacterFootPenetrationResponsibility toe = Responsibility(
                source.ToeDepth,
                final.ToeDepth,
                m_GeometryEpsilon);
            FrameCount++;
            SequenceEnd = row.SampleKey.Sequence;
            FrameEnd = frame;
            DurationSeconds += Math.Max(deltaSeconds, 0.000001d);
            FinalDepthTimeIntegral += final.MeanDepth * Math.Max(deltaSeconds, 0.000001d);
            FinalHeelDepthMaximum = Math.Max(FinalHeelDepthMaximum, final.HeelDepth);
            FinalToeDepthMaximum = Math.Max(FinalToeDepthMaximum, final.ToeDepth);
            FinalMeanDepthMaximum = Math.Max(FinalMeanDepthMaximum, final.MeanDepth);
            FinalLengthCoefficientMaximum = Math.Max(
                FinalLengthCoefficientMaximum,
                final.LengthCoefficient);
            SourceDepthMaximum = Math.Max(SourceDepthMaximum, source.MaximumDepth);
            IntroducedDepthMaximum = Math.Max(
                IntroducedDepthMaximum,
                Math.Max(introducedHeel, introducedToe));
            ResolvedDepthMaximum = Math.Max(
                ResolvedDepthMaximum,
                Math.Max(resolvedHeel, resolvedToe));
            if (final.MaximumDepth > m_GeometryEpsilon)
                PenetratingFrameCount++;
            if (introducedHeel > m_GeometryEpsilon || introducedToe > m_GeometryEpsilon)
                IntroducedFrameCount++;
            if (heel == CharacterFootPenetrationResponsibility.Amplified ||
                toe == CharacterFootPenetrationResponsibility.Amplified)
                AmplifiedFrameCount++;
            if (heel == CharacterFootPenetrationResponsibility.PartiallyResolved ||
                toe == CharacterFootPenetrationResponsibility.PartiallyResolved)
                PartiallyResolvedFrameCount++;
            if (heel == CharacterFootPenetrationResponsibility.Resolved ||
                toe == CharacterFootPenetrationResponsibility.Resolved)
                ResolvedFrameCount++;
            if (heel == CharacterFootPenetrationResponsibility.BaselineResidual ||
                toe == CharacterFootPenetrationResponsibility.BaselineResidual)
                BaselineResidualFrameCount++;
            if (FrameCount > 1 && final.MaximumDepth <= FinalDepthMaximum)
                return;
            FinalDepthMaximum = final.MaximumDepth;
            PeakSequence = row.SampleKey.Sequence;
            PeakSourceHeel = sourceHeel;
            PeakSourceToe = sourceToe;
            PeakPhysicalHeel = physicalHeel;
            PeakPhysicalToe = physicalToe;
            PeakAnchor = anchor;
            PeakNormal = normal;
        }

        static CharacterFootPenetrationLine ResolveLine(
            double heelClearance,
            double toeClearance)
        {
            double heelDepth = Math.Max(0d, -heelClearance);
            double toeDepth = Math.Max(0d, -toeClearance);
            double coefficient;
            double meanDepth;
            if (heelClearance >= 0d && toeClearance >= 0d)
            {
                coefficient = 0d;
                meanDepth = 0d;
            }
            else if (heelClearance < 0d && toeClearance < 0d)
            {
                coefficient = 1d;
                meanDepth = (heelDepth + toeDepth) * 0.5d;
            }
            else if (heelClearance < 0d)
            {
                coefficient = -heelClearance / (toeClearance - heelClearance);
                meanDepth = 0.5d * coefficient * heelDepth;
            }
            else
            {
                coefficient = -toeClearance / (heelClearance - toeClearance);
                meanDepth = 0.5d * coefficient * toeDepth;
            }
            return new CharacterFootPenetrationLine(
                heelDepth,
                toeDepth,
                meanDepth,
                Math.Max(0d, Math.Min(1d, coefficient)));
        }

        static CharacterFootPenetrationResponsibility Responsibility(
            double sourceDepth,
            double finalDepth,
            double epsilon)
        {
            if (sourceDepth <= epsilon && finalDepth <= epsilon)
                return CharacterFootPenetrationResponsibility.Clear;
            if (sourceDepth <= epsilon)
                return CharacterFootPenetrationResponsibility.Introduced;
            if (finalDepth <= epsilon)
                return CharacterFootPenetrationResponsibility.Resolved;
            double change = finalDepth - sourceDepth;
            if (Math.Abs(change) <= epsilon)
                return CharacterFootPenetrationResponsibility.BaselineResidual;
            return change > 0d
                ? CharacterFootPenetrationResponsibility.Amplified
                : CharacterFootPenetrationResponsibility.PartiallyResolved;
        }
    }

    internal sealed class CharacterFootContactPlaneReadResult
    {
        internal List<CharacterFootContactPlaneSegment> Segments { get; } =
            new List<CharacterFootContactPlaneSegment>();
        internal HashSet<string> MissingEvidence { get; } =
            new HashSet<string>(StringComparer.Ordinal);
    }

    internal static class CharacterFootContactPlaneReader
    {
        internal static CharacterFootContactPlaneReadResult Read(
            DiagnosticOperatorExecutionContext context,
            double geometryEpsilon,
            uint landingState,
            uint lockedState)
        {
            var inputs = new CharacterFootContactPlaneInputs(context);
            var result = new CharacterFootContactPlaneReadResult();
            var active = new Dictionary<string, CharacterFootContactPlaneSegment>(
                StringComparer.Ordinal);
            DiagnosticDatasetCursor cursor = context.CreateCursor(0);
            while (cursor.MoveNext())
            {
                DiagnosticDatasetRow row = cursor.Current;
                string dimension = row.SampleKey.DimensionId;
                if (!context.IncludesDimension(dimension) ||
                    !context.MatchesFilters(row))
                {
                    Flush(active, result.Segments, dimension);
                    continue;
                }
                if (!row.IsAvailable(inputs.Frame.Handle) ||
                    !row.IsAvailable(inputs.Reset.Handle) ||
                    !row.IsAvailable(inputs.State.Handle) ||
                    !row.IsAvailable(inputs.PlaneAvailable.Handle))
                {
                    AddMissing(result.MissingEvidence, inputs.Frame, row);
                    AddMissing(result.MissingEvidence, inputs.Reset, row);
                    AddMissing(result.MissingEvidence, inputs.State, row);
                    AddMissing(result.MissingEvidence, inputs.PlaneAvailable, row);
                    Flush(active, result.Segments, dimension);
                    continue;
                }
                uint state = row.GetUInt32(inputs.State.Handle);
                if (!row.GetBoolean(inputs.PlaneAvailable.Handle) ||
                    state != landingState && state != lockedState)
                {
                    Flush(active, result.Segments, dimension);
                    continue;
                }
                DiagnosticBoundInput[] required =
                {
                    inputs.Delta,
                    inputs.Event,
                    inputs.Surface,
                    inputs.AnchorIsAvailable,
                    inputs.AnchorEvent,
                    inputs.AnchorSurface,
                    inputs.Anchor,
                    inputs.Normal,
                    inputs.SourceAnklePoint,
                    inputs.SourceAnkleOrientation,
                    inputs.SourceHeelPoint,
                    inputs.SourceToePoint,
                    inputs.PhysicalIsAvailable,
                    inputs.PhysicalAnklePoint,
                    inputs.PhysicalAnkleOrientation
                };
                bool complete = true;
                for (int i = 0; i < required.Length; i++)
                {
                    if (row.IsAvailable(required[i].Handle))
                        continue;
                    result.MissingEvidence.Add(
                        CharacterFootDiagnosticOperatorSupport.Identity(required[i]));
                    complete = false;
                }
                if (!complete ||
                    !row.GetBoolean(inputs.AnchorIsAvailable.Handle) ||
                    !row.GetBoolean(inputs.PhysicalIsAvailable.Handle))
                {
                    if (complete)
                    {
                        if (!row.GetBoolean(inputs.AnchorIsAvailable.Handle))
                            result.MissingEvidence.Add("anchor-available=false");
                        if (!row.GetBoolean(inputs.PhysicalIsAvailable.Handle))
                            result.MissingEvidence.Add("physical-write-available=false");
                    }
                    Flush(active, result.Segments, dimension);
                    continue;
                }
                ulong eventIdentity = row.GetUInt64(inputs.Event.Handle);
                int surfaceIdentity = row.GetInt32(inputs.Surface.Handle);
                if (row.GetUInt64(inputs.AnchorEvent.Handle) != eventIdentity ||
                    row.GetInt32(inputs.AnchorSurface.Handle) != surfaceIdentity)
                {
                    result.MissingEvidence.Add("contact-anchor-lineage-mismatch");
                    Flush(active, result.Segments, dimension);
                    continue;
                }
                DiagnosticVector3 normal = row.GetVector3(inputs.Normal.Handle);
                if (!CharacterFootDiagnosticOperatorSupport.TryNormalize(
                        normal,
                        out double normalX,
                        out double normalY,
                        out double normalZ))
                {
                    result.MissingEvidence.Add("invalid-contact-anchor-normal");
                    Flush(active, result.Segments, dimension);
                    continue;
                }
                ulong frame = row.GetUInt64(inputs.Frame.Handle);
                ulong reset = row.GetUInt64(inputs.Reset.Handle);
                if (!active.TryGetValue(dimension, out CharacterFootContactPlaneSegment segment) ||
                    !segment.Continues(
                        frame,
                        reset,
                        eventIdentity,
                        surfaceIdentity,
                        state))
                {
                    Flush(active, result.Segments, dimension);
                    segment = new CharacterFootContactPlaneSegment(
                        dimension,
                        row.SampleKey.Sequence,
                        frame,
                        reset,
                        eventIdentity,
                        surfaceIdentity,
                        state,
                        geometryEpsilon);
                    active.Add(dimension, segment);
                }
                DiagnosticVector3 anchor = row.GetVector3(inputs.Anchor.Handle);
                DiagnosticVector3 sourceAnkle =
                    row.GetVector3(inputs.SourceAnklePoint.Handle);
                DiagnosticQuaternion sourceAnkleRotation =
                    row.GetQuaternion(inputs.SourceAnkleOrientation.Handle);
                DiagnosticVector3 sourceHeel = row.GetVector3(inputs.SourceHeelPoint.Handle);
                DiagnosticVector3 sourceToe = row.GetVector3(inputs.SourceToePoint.Handle);
                DiagnosticVector3 physicalAnkle =
                    row.GetVector3(inputs.PhysicalAnklePoint.Handle);
                DiagnosticQuaternion physicalAnkleRotation =
                    row.GetQuaternion(inputs.PhysicalAnkleOrientation.Handle);
                if (!CharacterFootDiagnosticOperatorSupport.TryRebuildPhysicalProbe(
                        sourceAnkle,
                        sourceAnkleRotation,
                        sourceHeel,
                        physicalAnkle,
                        physicalAnkleRotation,
                        out DiagnosticVector3 physicalHeel) ||
                    !CharacterFootDiagnosticOperatorSupport.TryRebuildPhysicalProbe(
                        sourceAnkle,
                        sourceAnkleRotation,
                        sourceToe,
                        physicalAnkle,
                        physicalAnkleRotation,
                        out DiagnosticVector3 physicalToe))
                {
                    result.MissingEvidence.Add("invalid-physical-ankle-rigid-transform");
                    Flush(active, result.Segments, dimension);
                    continue;
                }
                segment.Add(
                    row,
                    frame,
                    row.GetFloat32(inputs.Delta.Handle),
                    sourceHeel,
                    sourceToe,
                    physicalHeel,
                    physicalToe,
                    anchor,
                    normal,
                    normalX,
                    normalY,
                    normalZ);
            }
            foreach (CharacterFootContactPlaneSegment segment in active.Values)
                result.Segments.Add(segment);
            result.Segments.Sort(CompareSegments);
            return result;
        }

        static int CompareSegments(
            CharacterFootContactPlaneSegment left,
            CharacterFootContactPlaneSegment right)
        {
            int sequence = left.SequenceStart.CompareTo(right.SequenceStart);
            return sequence != 0
                ? sequence
                : string.CompareOrdinal(left.DimensionId, right.DimensionId);
        }

        static void AddMissing(
            ISet<string> missing,
            in DiagnosticBoundInput input,
            in DiagnosticDatasetRow row)
        {
            if (!row.IsAvailable(input.Handle))
                missing.Add(CharacterFootDiagnosticOperatorSupport.Identity(input));
        }

        static void Flush(
            IDictionary<string, CharacterFootContactPlaneSegment> active,
            ICollection<CharacterFootContactPlaneSegment> completed,
            string dimension)
        {
            if (!active.TryGetValue(dimension, out CharacterFootContactPlaneSegment segment))
                return;
            completed.Add(segment);
            active.Remove(dimension);
        }
    }

    internal sealed class CharacterFootFinalContactPlanePenetrationOperator :
        IDiagnosticAnalysisOperator
    {
        const string Threshold = "maximum-depth-meters";
        const string GeometryEpsilon = "geometry-epsilon-meters";
        const string LandingState = "landing-state";
        const string LockedState = "locked-state";

        public DiagnosticOperatorDescriptor Descriptor { get; } =
            new DiagnosticOperatorDescriptor(
                "foot/final-contact-plane-penetration",
                "contact",
                CharacterFootContactPlaneInputs.Slots(),
                new[]
                {
                    new DiagnosticOperatorParameter(
                        Threshold,
                        DiagnosticOperatorParameterKind.Number,
                        true,
                        0d),
                    new DiagnosticOperatorParameter(
                        GeometryEpsilon,
                        DiagnosticOperatorParameterKind.Number,
                        true,
                        0d),
                    new DiagnosticOperatorParameter(
                        LandingState,
                        DiagnosticOperatorParameterKind.Integer,
                        true),
                    new DiagnosticOperatorParameter(
                        LockedState,
                        DiagnosticOperatorParameterKind.Integer,
                        true)
                });

        public DiagnosticOperatorResult Execute(
            DiagnosticOperatorExecutionContext context)
        {
            double threshold = CharacterFootDiagnosticOperatorSupport.RequireNumber(
                context,
                Threshold);
            double epsilon = CharacterFootDiagnosticOperatorSupport.RequireNumber(
                context,
                GeometryEpsilon);
            uint landingState = CharacterFootDiagnosticOperatorSupport.RequireUInt32(
                context,
                LandingState);
            uint lockedState = CharacterFootDiagnosticOperatorSupport.RequireUInt32(
                context,
                LockedState);
            CharacterFootContactPlaneReadResult read =
                CharacterFootContactPlaneReader.Read(
                    context,
                    epsilon,
                    landingState,
                    lockedState);
            if (read.MissingEvidence.Count != 0)
                return DiagnosticOperatorResult.MissingEvidence(
                    CharacterFootDiagnosticOperatorSupport.MissingSummary(
                        read.MissingEvidence));
            if (read.Segments.Count == 0)
                return DiagnosticOperatorResult.NotApplicable(
                    "No complete Landing or Locked contact-plane segment was observed.");
            var inputs = new CharacterFootContactPlaneInputs(context);
            var occurrences = new List<double>(read.Segments.Count);
            var findings = new List<DiagnosticFinding>();
            for (int i = 0; i < read.Segments.Count; i++)
            {
                CharacterFootContactPlaneSegment segment = read.Segments[i];
                occurrences.Add(segment.FinalDepthMaximum);
                if (segment.FinalDepthMaximum <= threshold)
                    continue;
                findings.Add(BuildFinding(context.RuleId, inputs, segment));
            }
            double score = CharacterFootDiagnosticOperatorSupport.SeverityHealth(
                occurrences,
                read.Segments.Count);
            string summary =
                $"{findings.Count} of {read.Segments.Count} contact-plane segments exceeded {CharacterFootDiagnosticOperatorSupport.Format(threshold)} m.";
            return findings.Count == 0
                ? DiagnosticOperatorResult.Passed(summary, score: score)
                : DiagnosticOperatorResult.Failed(summary, null, findings, score);
        }

        static DiagnosticFinding BuildFinding(
            string ruleId,
            CharacterFootContactPlaneInputs inputs,
            CharacterFootContactPlaneSegment segment)
        {
            var evidence = new List<DiagnosticEvidence>
            {
                CharacterFootDiagnosticOperatorSupport.Evidence(
                    "maximum-depth-meters",
                    inputs.PhysicalAnklePoint,
                    segment.DimensionId,
                    segment.SequenceStart,
                    segment.SequenceEnd,
                    CharacterFootDiagnosticOperatorSupport.Format(segment.FinalDepthMaximum)),
                CharacterFootDiagnosticOperatorSupport.Evidence(
                    "heel-depth-meters",
                    inputs.PhysicalAnklePoint,
                    segment.DimensionId,
                    segment.SequenceStart,
                    segment.SequenceEnd,
                    CharacterFootDiagnosticOperatorSupport.Format(segment.FinalHeelDepthMaximum)),
                CharacterFootDiagnosticOperatorSupport.Evidence(
                    "toe-depth-meters",
                    inputs.PhysicalAnklePoint,
                    segment.DimensionId,
                    segment.SequenceStart,
                    segment.SequenceEnd,
                    CharacterFootDiagnosticOperatorSupport.Format(segment.FinalToeDepthMaximum)),
                CharacterFootDiagnosticOperatorSupport.Evidence(
                    "source-maximum-depth-meters",
                    inputs.SourceHeelPoint,
                    segment.DimensionId,
                    segment.SequenceStart,
                    segment.SequenceEnd,
                    CharacterFootDiagnosticOperatorSupport.Format(segment.SourceDepthMaximum)),
                CharacterFootDiagnosticOperatorSupport.Evidence(
                    "introduced-maximum-depth-meters",
                    inputs.PhysicalAnklePoint,
                    segment.DimensionId,
                    segment.SequenceStart,
                    segment.SequenceEnd,
                    CharacterFootDiagnosticOperatorSupport.Format(segment.IntroducedDepthMaximum)),
                CharacterFootDiagnosticOperatorSupport.Evidence(
                    "depth-time-integral-meter-seconds",
                    inputs.Delta,
                    segment.DimensionId,
                    segment.SequenceStart,
                    segment.SequenceEnd,
                    CharacterFootDiagnosticOperatorSupport.Format(segment.FinalDepthTimeIntegral)),
                CharacterFootDiagnosticOperatorSupport.Evidence(
                    "penetrating-frame-count",
                    inputs.Frame,
                    segment.DimensionId,
                    segment.SequenceStart,
                    segment.SequenceEnd,
                    segment.PenetratingFrameCount.ToString()),
                CharacterFootDiagnosticOperatorSupport.Evidence(
                    "severity-band",
                    inputs.PhysicalAnklePoint,
                    segment.DimensionId,
                    segment.SequenceStart,
                    segment.SequenceEnd,
                    CharacterFootDiagnosticOperatorSupport.SeverityBand(
                        segment.FinalDepthMaximum))
            };
            return new DiagnosticFinding(
                CharacterFootDiagnosticOperatorSupport.FindingId(
                    ruleId,
                    segment.PeakSequence),
                CharacterFootDiagnosticOperatorSupport.Severity(
                    segment.FinalDepthMaximum),
                segment.DimensionId,
                segment.SequenceStart,
                segment.SequenceEnd,
                $"Final physical heel/toe penetrated the contact plane by {CharacterFootDiagnosticOperatorSupport.Format(segment.FinalDepthMaximum)} m.",
                evidence,
                CharacterFootDiagnosticOperatorSupport.FrameRange(
                    inputs.Frame,
                    segment.FrameStart,
                    segment.FrameEnd));
        }
    }

    internal sealed class CharacterFootContactPlanePenetrationContributionOperator :
        IDiagnosticAnalysisOperator
    {
        const string GeometryEpsilon = "geometry-epsilon-meters";
        const string LandingState = "landing-state";
        const string LockedState = "locked-state";

        public DiagnosticOperatorDescriptor Descriptor { get; } =
            new DiagnosticOperatorDescriptor(
                "foot/contact-plane-penetration-contribution",
                "contact",
                CharacterFootContactPlaneInputs.Slots(),
                new[]
                {
                    new DiagnosticOperatorParameter(
                        GeometryEpsilon,
                        DiagnosticOperatorParameterKind.Number,
                        true,
                        0d),
                    new DiagnosticOperatorParameter(
                        LandingState,
                        DiagnosticOperatorParameterKind.Integer,
                        true),
                    new DiagnosticOperatorParameter(
                        LockedState,
                        DiagnosticOperatorParameterKind.Integer,
                        true)
                });

        public DiagnosticOperatorResult Execute(
            DiagnosticOperatorExecutionContext context)
        {
            double epsilon = CharacterFootDiagnosticOperatorSupport.RequireNumber(
                context,
                GeometryEpsilon);
            uint landingState = CharacterFootDiagnosticOperatorSupport.RequireUInt32(
                context,
                LandingState);
            uint lockedState = CharacterFootDiagnosticOperatorSupport.RequireUInt32(
                context,
                LockedState);
            CharacterFootContactPlaneReadResult read =
                CharacterFootContactPlaneReader.Read(
                    context,
                    epsilon,
                    landingState,
                    lockedState);
            if (read.MissingEvidence.Count != 0)
                return DiagnosticOperatorResult.MissingEvidence(
                    CharacterFootDiagnosticOperatorSupport.MissingSummary(
                        read.MissingEvidence));
            if (read.Segments.Count == 0)
                return DiagnosticOperatorResult.NotApplicable(
                    "No complete Landing or Locked contact-plane segment was observed.");
            var inputs = new CharacterFootContactPlaneInputs(context);
            var findings = new List<DiagnosticFinding>();
            for (int i = 0; i < read.Segments.Count; i++)
            {
                CharacterFootContactPlaneSegment segment = read.Segments[i];
                if (segment.SourceDepthMaximum <= epsilon &&
                    segment.IntroducedDepthMaximum <= epsilon &&
                    segment.AmplifiedFrameCount == 0 &&
                    segment.PartiallyResolvedFrameCount == 0 &&
                    segment.ResolvedFrameCount == 0 &&
                    segment.BaselineResidualFrameCount == 0)
                {
                    continue;
                }
                var evidence = new List<DiagnosticEvidence>
                {
                    CharacterFootDiagnosticOperatorSupport.Evidence(
                        "source-depth-maximum-meters",
                        inputs.SourceHeelPoint,
                        segment.DimensionId,
                        segment.SequenceStart,
                        segment.SequenceEnd,
                        CharacterFootDiagnosticOperatorSupport.Format(segment.SourceDepthMaximum)),
                    CharacterFootDiagnosticOperatorSupport.Evidence(
                        "introduced-depth-maximum-meters",
                        inputs.PhysicalAnklePoint,
                        segment.DimensionId,
                        segment.SequenceStart,
                        segment.SequenceEnd,
                        CharacterFootDiagnosticOperatorSupport.Format(segment.IntroducedDepthMaximum)),
                    CharacterFootDiagnosticOperatorSupport.Evidence(
                        "resolved-depth-maximum-meters",
                        inputs.PhysicalAnklePoint,
                        segment.DimensionId,
                        segment.SequenceStart,
                        segment.SequenceEnd,
                        CharacterFootDiagnosticOperatorSupport.Format(segment.ResolvedDepthMaximum)),
                    CharacterFootDiagnosticOperatorSupport.Evidence(
                        "amplified-frame-count",
                        inputs.Frame,
                        segment.DimensionId,
                        segment.SequenceStart,
                        segment.SequenceEnd,
                        segment.AmplifiedFrameCount.ToString()),
                    CharacterFootDiagnosticOperatorSupport.Evidence(
                        "partially-resolved-frame-count",
                        inputs.Frame,
                        segment.DimensionId,
                        segment.SequenceStart,
                        segment.SequenceEnd,
                        segment.PartiallyResolvedFrameCount.ToString()),
                    CharacterFootDiagnosticOperatorSupport.Evidence(
                        "resolved-frame-count",
                        inputs.Frame,
                        segment.DimensionId,
                        segment.SequenceStart,
                        segment.SequenceEnd,
                        segment.ResolvedFrameCount.ToString()),
                    CharacterFootDiagnosticOperatorSupport.Evidence(
                        "baseline-residual-frame-count",
                        inputs.Frame,
                        segment.DimensionId,
                        segment.SequenceStart,
                        segment.SequenceEnd,
                        segment.BaselineResidualFrameCount.ToString())
                };
                findings.Add(new DiagnosticFinding(
                    CharacterFootDiagnosticOperatorSupport.FindingId(
                        context.RuleId,
                        segment.SequenceStart),
                    DiagnosticSeverity.Information,
                    segment.DimensionId,
                    segment.SequenceStart,
                    segment.SequenceEnd,
                    "Contact-plane penetration responsibility evidence was observed.",
                    evidence,
                    CharacterFootDiagnosticOperatorSupport.FrameRange(
                        inputs.Frame,
                        segment.FrameStart,
                        segment.FrameEnd)));
            }
            string summary =
                $"{findings.Count} of {read.Segments.Count} contact-plane segments contain penetration contribution evidence.";
            return new DiagnosticOperatorResult(
                findings.Count == 0
                    ? DiagnosticRuleState.Passed
                    : DiagnosticRuleState.Failed,
                summary,
                null,
                null,
                findings);
        }
    }
}
