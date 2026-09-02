using System;
using System.Collections.Generic;
using KK.GeneratedDiagnosticSampling;
using KK.GeneratedDiagnosticSampling.Host;

namespace ThirdPersonCharacter.Pipeline.Diagnostics.FootAnalysis.Editor
{
    internal sealed class CharacterFootLockedMotionInputs
    {
        internal const string FrameSequence = "frame-sequence";
        internal const string ResetSequence = "reset-sequence";
        internal const string DeltaSeconds = "delta-seconds";
        internal const string ConstraintState = "constraint-state";
        internal const string LockResponse = "lock-response";
        internal const string EventIdentity = "event-identity";
        internal const string SurfaceIdentity = "surface-identity";
        internal const string AnchorAvailable = "anchor-available";
        internal const string AnchorEventIdentity = "anchor-event-identity";
        internal const string AnchorSurfaceIdentity = "anchor-surface-identity";
        internal const string AnchorPoint = "anchor-point";
        internal const string AnchorNormal = "anchor-normal";
        internal const string CorrectedSole = "corrected-sole";
        internal const string SourceAnklePosition = "source-ankle-position";
        internal const string SourceAnkleRotation = "source-ankle-rotation";
        internal const string SourceHeel = "source-heel";
        internal const string SourceToe = "source-toe";
        internal const string PhysicalWriteAvailable = "physical-write-available";
        internal const string PhysicalAnklePosition = "physical-ankle-position";
        internal const string PhysicalAnkleRotation = "physical-ankle-rotation";

        internal CharacterFootLockedMotionInputs(
            DiagnosticOperatorExecutionContext context)
        {
            Frame = context.Input(FrameSequence);
            Reset = context.Input(ResetSequence);
            Delta = context.Input(DeltaSeconds);
            State = context.Input(ConstraintState);
            Response = context.Input(LockResponse);
            Event = context.Input(EventIdentity);
            Surface = context.Input(SurfaceIdentity);
            AnchorIsAvailable = context.Input(AnchorAvailable);
            AnchorEvent = context.Input(AnchorEventIdentity);
            AnchorSurface = context.Input(AnchorSurfaceIdentity);
            Anchor = context.Input(AnchorPoint);
            Normal = context.Input(AnchorNormal);
            CorrectedSolePoint = context.Input(CorrectedSole);
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
        internal DiagnosticBoundInput Response { get; }
        internal DiagnosticBoundInput Event { get; }
        internal DiagnosticBoundInput Surface { get; }
        internal DiagnosticBoundInput AnchorIsAvailable { get; }
        internal DiagnosticBoundInput AnchorEvent { get; }
        internal DiagnosticBoundInput AnchorSurface { get; }
        internal DiagnosticBoundInput Anchor { get; }
        internal DiagnosticBoundInput Normal { get; }
        internal DiagnosticBoundInput CorrectedSolePoint { get; }
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
            Slot(LockResponse, DiagnosticValueKind.UInt32),
            Slot(EventIdentity, DiagnosticValueKind.UInt64),
            Slot(SurfaceIdentity, DiagnosticValueKind.Int32),
            Slot(AnchorAvailable, DiagnosticValueKind.Boolean),
            Slot(AnchorEventIdentity, DiagnosticValueKind.UInt64),
            Slot(AnchorSurfaceIdentity, DiagnosticValueKind.Int32),
            Slot(AnchorPoint, DiagnosticValueKind.Vector3),
            Slot(AnchorNormal, DiagnosticValueKind.Vector3),
            Slot(CorrectedSole, DiagnosticValueKind.Vector3),
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

    internal sealed class CharacterFootLockedMotionSegment
    {
        readonly DiagnosticVector3 m_EntryAnchor;

        internal CharacterFootLockedMotionSegment(
            string dimensionId,
            uint response,
            ulong sequence,
            ulong frame,
            ulong reset,
            ulong eventIdentity,
            int surfaceIdentity,
            in DiagnosticVector3 anchor,
            in DiagnosticVector3 normal)
        {
            DimensionId = dimensionId;
            Response = response;
            SequenceStart = sequence;
            SequenceEnd = sequence;
            FrameStart = frame;
            FrameEnd = frame;
            ResetSequence = reset;
            EventIdentity = eventIdentity;
            SurfaceIdentity = surfaceIdentity;
            m_EntryAnchor = anchor;
            EntryNormal = normal;
            MinimumSoleAlongNormal = double.MaxValue;
        }

        internal string DimensionId { get; }
        internal uint Response { get; }
        internal ulong SequenceStart { get; }
        internal ulong SequenceEnd { get; private set; }
        internal ulong FrameStart { get; }
        internal ulong FrameEnd { get; private set; }
        internal ulong ResetSequence { get; }
        internal ulong EventIdentity { get; }
        internal int SurfaceIdentity { get; }
        internal DiagnosticVector3 EntryNormal { get; }
        internal int FrameCount { get; private set; }
        internal double DurationSeconds { get; private set; }
        internal double AnchorDisplacementMaximum { get; private set; }
        internal double PhysicalHorizontalDistanceMaximum { get; private set; }
        internal double MinimumSoleAlongNormal { get; private set; }
        internal double DownwardExcursion => Math.Max(0d, -MinimumSoleAlongNormal);
        internal ulong HorizontalPeakSequence { get; private set; }
        internal ulong VerticalPeakSequence { get; private set; }
        internal DiagnosticVector3 HorizontalPeakSole { get; private set; }
        internal DiagnosticVector3 HorizontalPeakAnchor { get; private set; }
        internal DiagnosticVector3 VerticalPeakCorrectedSole { get; private set; }
        internal DiagnosticVector3 VerticalPeakAnchor { get; private set; }

        internal bool Continues(
            ulong frame,
            ulong reset,
            ulong eventIdentity,
            int surfaceIdentity,
            uint response,
            in DiagnosticVector3 anchor,
            in DiagnosticVector3 normal,
            double anchorTolerance) =>
            frame == FrameEnd + 1 &&
            reset == ResetSequence &&
            eventIdentity == EventIdentity &&
            surfaceIdentity == SurfaceIdentity &&
            response == Response &&
            CharacterFootDiagnosticOperatorSupport.Distance(
                m_EntryAnchor,
                anchor) <= anchorTolerance &&
            CharacterFootDiagnosticOperatorSupport.Distance(
                EntryNormal,
                normal) <= anchorTolerance;

        internal void Add(
            in DiagnosticDatasetRow row,
            ulong frame,
            double deltaSeconds,
            in DiagnosticVector3 anchor,
            in DiagnosticVector3 correctedSole,
            in DiagnosticVector3 physicalHeel,
            in DiagnosticVector3 physicalToe,
            double normalX,
            double normalY,
            double normalZ)
        {
            DiagnosticVector3 physicalSole =
                CharacterFootDiagnosticOperatorSupport.Midpoint(
                    physicalHeel,
                    physicalToe);
            double horizontal =
                CharacterFootDiagnosticOperatorSupport.HorizontalDistance(
                    physicalSole,
                    anchor,
                    normalX,
                    normalY,
                    normalZ);
            double along = CharacterFootDiagnosticOperatorSupport.Clearance(
                correctedSole,
                anchor,
                normalX,
                normalY,
                normalZ);
            double anchorDisplacement =
                CharacterFootDiagnosticOperatorSupport.Distance(
                    m_EntryAnchor,
                    anchor);
            FrameCount++;
            SequenceEnd = row.SampleKey.Sequence;
            FrameEnd = frame;
            DurationSeconds += Math.Max(deltaSeconds, 0.000001d);
            AnchorDisplacementMaximum = Math.Max(
                AnchorDisplacementMaximum,
                anchorDisplacement);
            if (horizontal > PhysicalHorizontalDistanceMaximum)
            {
                PhysicalHorizontalDistanceMaximum = horizontal;
                HorizontalPeakSequence = row.SampleKey.Sequence;
                HorizontalPeakSole = physicalSole;
                HorizontalPeakAnchor = anchor;
            }
            if (along < MinimumSoleAlongNormal)
            {
                MinimumSoleAlongNormal = along;
                VerticalPeakSequence = row.SampleKey.Sequence;
                VerticalPeakCorrectedSole = correctedSole;
                VerticalPeakAnchor = anchor;
            }
        }
    }

    internal sealed class CharacterFootLockedMotionReadResult
    {
        internal List<CharacterFootLockedMotionSegment> Segments { get; } =
            new List<CharacterFootLockedMotionSegment>();
        internal HashSet<string> MissingEvidence { get; } =
            new HashSet<string>(StringComparer.Ordinal);
    }

    internal static class CharacterFootLockedMotionReader
    {
        internal static CharacterFootLockedMotionReadResult Read(
            DiagnosticOperatorExecutionContext context,
            double anchorTolerance,
            uint lockedState,
            uint fullAnchorResponse,
            uint slidingResponse)
        {
            var inputs = new CharacterFootLockedMotionInputs(context);
            var result = new CharacterFootLockedMotionReadResult();
            var active = new Dictionary<string, CharacterFootLockedMotionSegment>(
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
                    !row.IsAvailable(inputs.State.Handle))
                {
                    AddMissing(result.MissingEvidence, inputs.Frame, row);
                    AddMissing(result.MissingEvidence, inputs.Reset, row);
                    AddMissing(result.MissingEvidence, inputs.State, row);
                    Flush(active, result.Segments, dimension);
                    continue;
                }
                uint state = row.GetUInt32(inputs.State.Handle);
                if (state != lockedState)
                {
                    Flush(active, result.Segments, dimension);
                    continue;
                }
                DiagnosticBoundInput[] required =
                {
                    inputs.Delta,
                    inputs.Response,
                    inputs.Event,
                    inputs.Surface,
                    inputs.AnchorIsAvailable,
                    inputs.AnchorEvent,
                    inputs.AnchorSurface,
                    inputs.Anchor,
                    inputs.Normal,
                    inputs.CorrectedSolePoint,
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
                uint response = row.GetUInt32(inputs.Response.Handle);
                if (response != fullAnchorResponse && response != slidingResponse)
                {
                    result.MissingEvidence.Add("invalid-locked-response");
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
                DiagnosticVector3 anchor = row.GetVector3(inputs.Anchor.Handle);
                if (!active.TryGetValue(dimension, out CharacterFootLockedMotionSegment segment) ||
                    !segment.Continues(
                        frame,
                        reset,
                        eventIdentity,
                        surfaceIdentity,
                        response,
                        anchor,
                        normal,
                        anchorTolerance))
                {
                    Flush(active, result.Segments, dimension);
                    segment = new CharacterFootLockedMotionSegment(
                        dimension,
                        response,
                        row.SampleKey.Sequence,
                        frame,
                        reset,
                        eventIdentity,
                        surfaceIdentity,
                        anchor,
                        normal);
                    active.Add(dimension, segment);
                }
                DiagnosticVector3 correctedSole =
                    row.GetVector3(inputs.CorrectedSolePoint.Handle);
                DiagnosticVector3 sourceAnkle =
                    row.GetVector3(inputs.SourceAnklePoint.Handle);
                DiagnosticQuaternion sourceAnkleRotation =
                    row.GetQuaternion(inputs.SourceAnkleOrientation.Handle);
                DiagnosticVector3 sourceHeel =
                    row.GetVector3(inputs.SourceHeelPoint.Handle);
                DiagnosticVector3 sourceToe =
                    row.GetVector3(inputs.SourceToePoint.Handle);
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
                    anchor,
                    correctedSole,
                    physicalHeel,
                    physicalToe,
                    normalX,
                    normalY,
                    normalZ);
            }
            foreach (CharacterFootLockedMotionSegment segment in active.Values)
                result.Segments.Add(segment);
            result.Segments.Sort(CompareSegments);
            return result;
        }

        static int CompareSegments(
            CharacterFootLockedMotionSegment left,
            CharacterFootLockedMotionSegment right)
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
            IDictionary<string, CharacterFootLockedMotionSegment> active,
            ICollection<CharacterFootLockedMotionSegment> completed,
            string dimension)
        {
            if (!active.TryGetValue(dimension, out CharacterFootLockedMotionSegment segment))
                return;
            completed.Add(segment);
            active.Remove(dimension);
        }
    }

    internal sealed class CharacterFootLockedHorizontalDriftOperator :
        IDiagnosticAnalysisOperator
    {
        const string Threshold = "maximum-horizontal-distance-meters";
        const string AnchorTolerance = "anchor-stability-tolerance-meters";
        const string LockedState = "locked-state";
        const string FullAnchorResponse = "full-anchor-response";
        const string SlidingResponse = "sliding-response";

        public DiagnosticOperatorDescriptor Descriptor { get; } =
            new DiagnosticOperatorDescriptor(
                "foot/locked-horizontal-drift",
                "motion",
                CharacterFootLockedMotionInputs.Slots(),
                Parameters());

        public DiagnosticOperatorResult Execute(
            DiagnosticOperatorExecutionContext context)
        {
            double threshold = CharacterFootDiagnosticOperatorSupport.RequireNumber(
                context,
                Threshold);
            double anchorTolerance =
                CharacterFootDiagnosticOperatorSupport.RequireNumber(
                    context,
                    AnchorTolerance);
            uint lockedState = CharacterFootDiagnosticOperatorSupport.RequireUInt32(
                context,
                LockedState);
            uint fullAnchorResponse =
                CharacterFootDiagnosticOperatorSupport.RequireUInt32(
                    context,
                    FullAnchorResponse);
            uint slidingResponse =
                CharacterFootDiagnosticOperatorSupport.RequireUInt32(
                    context,
                    SlidingResponse);
            CharacterFootLockedMotionReadResult read =
                CharacterFootLockedMotionReader.Read(
                    context,
                    anchorTolerance,
                    lockedState,
                    fullAnchorResponse,
                    slidingResponse);
            if (read.MissingEvidence.Count != 0)
                return DiagnosticOperatorResult.MissingEvidence(
                    CharacterFootDiagnosticOperatorSupport.MissingSummary(
                        read.MissingEvidence));
            var eligible = new List<CharacterFootLockedMotionSegment>();
            for (int i = 0; i < read.Segments.Count; i++)
            {
                CharacterFootLockedMotionSegment segment = read.Segments[i];
                if (segment.Response == fullAnchorResponse &&
                    segment.AnchorDisplacementMaximum <= anchorTolerance)
                {
                    eligible.Add(segment);
                }
            }
            if (eligible.Count == 0)
                return DiagnosticOperatorResult.NotApplicable(
                    "No complete stable FullAnchor segment was observed.");
            var inputs = new CharacterFootLockedMotionInputs(context);
            var occurrences = new List<double>(eligible.Count);
            var findings = new List<DiagnosticFinding>();
            for (int i = 0; i < eligible.Count; i++)
            {
                CharacterFootLockedMotionSegment segment = eligible[i];
                occurrences.Add(segment.PhysicalHorizontalDistanceMaximum);
                if (segment.PhysicalHorizontalDistanceMaximum <= threshold)
                    continue;
                var evidence = new List<DiagnosticEvidence>
                {
                    CharacterFootDiagnosticOperatorSupport.Evidence(
                        "maximum-horizontal-distance-meters",
                        inputs.PhysicalAnklePoint,
                        segment.DimensionId,
                        segment.SequenceStart,
                        segment.SequenceEnd,
                        CharacterFootDiagnosticOperatorSupport.Format(
                            segment.PhysicalHorizontalDistanceMaximum)),
                    CharacterFootDiagnosticOperatorSupport.Evidence(
                        "anchor-displacement-meters",
                        inputs.Anchor,
                        segment.DimensionId,
                        segment.SequenceStart,
                        segment.SequenceEnd,
                        CharacterFootDiagnosticOperatorSupport.Format(
                            segment.AnchorDisplacementMaximum)),
                    CharacterFootDiagnosticOperatorSupport.Evidence(
                        "severity-band",
                        inputs.PhysicalAnklePoint,
                        segment.DimensionId,
                        segment.SequenceStart,
                        segment.SequenceEnd,
                        CharacterFootDiagnosticOperatorSupport.SeverityBand(
                            segment.PhysicalHorizontalDistanceMaximum))
                };
                findings.Add(new DiagnosticFinding(
                    CharacterFootDiagnosticOperatorSupport.FindingId(
                        context.RuleId,
                        segment.HorizontalPeakSequence),
                    CharacterFootDiagnosticOperatorSupport.Severity(
                        segment.PhysicalHorizontalDistanceMaximum),
                    segment.DimensionId,
                    segment.SequenceStart,
                    segment.SequenceEnd,
                    $"Final physical sole reached {CharacterFootDiagnosticOperatorSupport.Format(segment.PhysicalHorizontalDistanceMaximum)} m horizontal distance from the stable anchor.",
                    evidence,
                    CharacterFootDiagnosticOperatorSupport.FrameRange(
                        inputs.Frame,
                        segment.FrameStart,
                        segment.FrameEnd)));
            }
            double score = CharacterFootDiagnosticOperatorSupport.SeverityHealth(
                occurrences,
                eligible.Count);
            string summary =
                $"{findings.Count} of {eligible.Count} stable FullAnchor segments exceeded {CharacterFootDiagnosticOperatorSupport.Format(threshold)} m.";
            return findings.Count == 0
                ? DiagnosticOperatorResult.Passed(summary, score: score)
                : DiagnosticOperatorResult.Failed(summary, null, findings, score);
        }

        static DiagnosticOperatorParameter[] Parameters() => new[]
        {
            new DiagnosticOperatorParameter(
                Threshold,
                DiagnosticOperatorParameterKind.Number,
                true,
                0d),
            new DiagnosticOperatorParameter(
                AnchorTolerance,
                DiagnosticOperatorParameterKind.Number,
                true,
                0d),
            new DiagnosticOperatorParameter(
                LockedState,
                DiagnosticOperatorParameterKind.Integer,
                true),
            new DiagnosticOperatorParameter(
                FullAnchorResponse,
                DiagnosticOperatorParameterKind.Integer,
                true),
            new DiagnosticOperatorParameter(
                SlidingResponse,
                DiagnosticOperatorParameterKind.Integer,
                true)
        };
    }

    internal sealed class CharacterFootLockedVerticalAnchorEvidenceOperator :
        IDiagnosticAnalysisOperator
    {
        const string Threshold = "maximum-downward-excursion-meters";
        const string AnchorTolerance = "anchor-stability-tolerance-meters";
        const string LockedState = "locked-state";
        const string FullAnchorResponse = "full-anchor-response";
        const string SlidingResponse = "sliding-response";

        public DiagnosticOperatorDescriptor Descriptor { get; } =
            new DiagnosticOperatorDescriptor(
                "foot/locked-vertical-anchor-evidence",
                "motion",
                CharacterFootLockedMotionInputs.Slots(),
                Parameters());

        public DiagnosticOperatorResult Execute(
            DiagnosticOperatorExecutionContext context)
        {
            double threshold = CharacterFootDiagnosticOperatorSupport.RequireNumber(
                context,
                Threshold);
            double anchorTolerance =
                CharacterFootDiagnosticOperatorSupport.RequireNumber(
                    context,
                    AnchorTolerance);
            uint lockedState = CharacterFootDiagnosticOperatorSupport.RequireUInt32(
                context,
                LockedState);
            uint fullAnchorResponse =
                CharacterFootDiagnosticOperatorSupport.RequireUInt32(
                    context,
                    FullAnchorResponse);
            uint slidingResponse =
                CharacterFootDiagnosticOperatorSupport.RequireUInt32(
                    context,
                    SlidingResponse);
            CharacterFootLockedMotionReadResult read =
                CharacterFootLockedMotionReader.Read(
                    context,
                    anchorTolerance,
                    lockedState,
                    fullAnchorResponse,
                    slidingResponse);
            if (read.MissingEvidence.Count != 0)
                return DiagnosticOperatorResult.MissingEvidence(
                    CharacterFootDiagnosticOperatorSupport.MissingSummary(
                        read.MissingEvidence));
            if (read.Segments.Count == 0)
                return DiagnosticOperatorResult.NotApplicable(
                    "No complete FullAnchor or Sliding segment was observed.");
            var inputs = new CharacterFootLockedMotionInputs(context);
            var findings = new List<DiagnosticFinding>();
            for (int i = 0; i < read.Segments.Count; i++)
            {
                CharacterFootLockedMotionSegment segment = read.Segments[i];
                if (segment.DownwardExcursion <= threshold)
                    continue;
                var evidence = new List<DiagnosticEvidence>
                {
                    CharacterFootDiagnosticOperatorSupport.Evidence(
                        "downward-excursion-meters",
                        inputs.CorrectedSolePoint,
                        segment.DimensionId,
                        segment.SequenceStart,
                        segment.SequenceEnd,
                        CharacterFootDiagnosticOperatorSupport.Format(
                            segment.DownwardExcursion)),
                    CharacterFootDiagnosticOperatorSupport.Evidence(
                        "lock-response",
                        inputs.Response,
                        segment.DimensionId,
                        segment.SequenceStart,
                        segment.SequenceEnd,
                        ResponseName(
                            segment.Response,
                            fullAnchorResponse,
                            slidingResponse)),
                    CharacterFootDiagnosticOperatorSupport.Evidence(
                        "physical-horizontal-distance-maximum-meters",
                        inputs.PhysicalAnklePoint,
                        segment.DimensionId,
                        segment.SequenceStart,
                        segment.SequenceEnd,
                        CharacterFootDiagnosticOperatorSupport.Format(
                            segment.PhysicalHorizontalDistanceMaximum))
                };
                findings.Add(new DiagnosticFinding(
                    CharacterFootDiagnosticOperatorSupport.FindingId(
                        context.RuleId,
                        segment.VerticalPeakSequence),
                    DiagnosticSeverity.Information,
                    segment.DimensionId,
                    segment.SequenceStart,
                    segment.SequenceEnd,
                    $"Corrected sole moved {CharacterFootDiagnosticOperatorSupport.Format(segment.DownwardExcursion)} m below the anchor plane during {ResponseName(segment.Response, fullAnchorResponse, slidingResponse)}.",
                    evidence,
                    CharacterFootDiagnosticOperatorSupport.FrameRange(
                        inputs.Frame,
                        segment.FrameStart,
                        segment.FrameEnd)));
            }
            string summary =
                $"{findings.Count} of {read.Segments.Count} locked segments exceeded {CharacterFootDiagnosticOperatorSupport.Format(threshold)} m downward excursion.";
            return new DiagnosticOperatorResult(
                findings.Count == 0
                    ? DiagnosticRuleState.Passed
                    : DiagnosticRuleState.Failed,
                summary,
                null,
                null,
                findings);
        }

        static DiagnosticOperatorParameter[] Parameters() => new[]
        {
            new DiagnosticOperatorParameter(
                Threshold,
                DiagnosticOperatorParameterKind.Number,
                true,
                0d),
            new DiagnosticOperatorParameter(
                AnchorTolerance,
                DiagnosticOperatorParameterKind.Number,
                true,
                0d),
            new DiagnosticOperatorParameter(
                LockedState,
                DiagnosticOperatorParameterKind.Integer,
                true),
            new DiagnosticOperatorParameter(
                FullAnchorResponse,
                DiagnosticOperatorParameterKind.Integer,
                true),
            new DiagnosticOperatorParameter(
                SlidingResponse,
                DiagnosticOperatorParameterKind.Integer,
                true)
        };

        static string ResponseName(
            uint response,
            uint fullAnchorResponse,
            uint slidingResponse)
        {
            if (response == fullAnchorResponse)
                return "FullAnchor";
            if (response == slidingResponse)
                return "Sliding";
            throw new InvalidOperationException("Locked response is invalid.");
        }
    }
}
