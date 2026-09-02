using System;
using System.Collections.Generic;
using System.Linq;
using KK.GeneratedDiagnosticSampling;
using KK.GeneratedDiagnosticSampling.Host;

namespace ThirdPersonCharacter.Pipeline.Diagnostics.FootAnalysis.Editor
{
    internal static class CharacterFootLandingPolicySupport
    {
        internal static DiagnosticOperatorInputSlot Slot(
            string id,
            DiagnosticValueKind kind) => new DiagnosticOperatorInputSlot(
                id,
                kind,
                DiagnosticDatasetCardinality.Main,
                true);

        internal static DiagnosticOperatorParameter Number(string id) =>
            new DiagnosticOperatorParameter(
                id,
                DiagnosticOperatorParameterKind.Number,
                true,
                0d);

        internal static DiagnosticOperatorParameter Integer(string id) =>
            new DiagnosticOperatorParameter(
                id,
                DiagnosticOperatorParameterKind.Integer,
                true);

        internal static Dictionary<string, List<DiagnosticDatasetRow>> Rows(
            DiagnosticOperatorExecutionContext context,
            IReadOnlyList<DiagnosticBoundInput> required,
            ISet<string> missing)
        {
            var result = new Dictionary<string, List<DiagnosticDatasetRow>>(
                StringComparer.Ordinal);
            DiagnosticDatasetCursor cursor = required[0].Dataset.Main.CreateCursor();
            while (cursor.MoveNext())
            {
                DiagnosticDatasetRow row = cursor.Current;
                string dimension = row.SampleKey.DimensionId;
                if (!context.IncludesDimension(dimension) ||
                    !context.MatchesFilters(row))
                {
                    continue;
                }
                bool available = true;
                for (int i = 0; i < required.Count; i++)
                {
                    if (row.IsAvailable(required[i].Handle))
                        continue;
                    missing.Add(CharacterFootDiagnosticOperatorSupport.Identity(required[i]));
                    available = false;
                }
                if (!available)
                    continue;
                if (!result.TryGetValue(dimension, out List<DiagnosticDatasetRow> rows))
                {
                    rows = new List<DiagnosticDatasetRow>();
                    result.Add(dimension, rows);
                }
                rows.Add(row);
            }
            foreach (List<DiagnosticDatasetRow> rows in result.Values)
                rows.Sort((left, right) => left.SampleKey.Sequence.CompareTo(right.SampleKey.Sequence));
            return result;
        }

        internal static bool Continuous(
            in DiagnosticDatasetRow previous,
            in DiagnosticDatasetRow current,
            in DiagnosticBoundInput frame,
            in DiagnosticBoundInput reset) =>
            current.GetUInt64(frame.Handle) == previous.GetUInt64(frame.Handle) + 1 &&
            current.GetUInt64(reset.Handle) == previous.GetUInt64(reset.Handle);

        internal static bool Require(
            in DiagnosticDatasetRow row,
            ISet<string> missing,
            params DiagnosticBoundInput[] inputs)
        {
            bool complete = true;
            for (int i = 0; i < inputs.Length; i++)
            {
                if (row.IsAvailable(inputs[i].Handle))
                    continue;
                missing.Add(CharacterFootDiagnosticOperatorSupport.Identity(inputs[i]));
                complete = false;
            }
            return complete;
        }

        internal static double Distance(
            in DiagnosticVector3 left,
            in DiagnosticVector3 right) =>
            CharacterFootDiagnosticOperatorSupport.Distance(left, right);

        internal static double Range(IReadOnlyList<DiagnosticVector3> values)
        {
            double result = 0d;
            for (int i = 0; i < values.Count; i++)
                for (int j = i + 1; j < values.Count; j++)
                    result = Math.Max(result, Distance(values[i], values[j]));
            return result;
        }

        internal static int ReversalCount(IReadOnlyList<DiagnosticVector3> values)
        {
            int result = 0;
            double previousX = 0d;
            double previousY = 0d;
            double previousZ = 0d;
            bool hasPrevious = false;
            for (int i = 1; i < values.Count; i++)
            {
                double x = values[i].X - values[i - 1].X;
                double y = values[i].Y - values[i - 1].Y;
                double z = values[i].Z - values[i - 1].Z;
                double magnitude = Math.Sqrt(x * x + y * y + z * z);
                if (magnitude <= 0.000001d)
                    continue;
                if (hasPrevious && previousX * x + previousY * y + previousZ * z < 0d)
                    result++;
                previousX = x;
                previousY = y;
                previousZ = z;
                hasPrevious = true;
            }
            return result;
        }

        internal static DiagnosticEvidence Evidence(
            string id,
            in DiagnosticBoundInput input,
            string dimension,
            ulong start,
            ulong end,
            double value) => CharacterFootDiagnosticOperatorSupport.Evidence(
                id,
                input,
                dimension,
                start,
                end,
                CharacterFootDiagnosticOperatorSupport.Format(value));

        internal static DiagnosticEvidence Evidence(
            string id,
            in DiagnosticBoundInput input,
            string dimension,
            ulong start,
            ulong end,
            bool value) => CharacterFootDiagnosticOperatorSupport.Evidence(
                id,
                input,
                dimension,
                start,
                end,
                value ? "true" : "false");

        internal static DiagnosticFinding Finding(
            DiagnosticOperatorExecutionContext context,
            string target,
            DiagnosticSeverity severity,
            string dimension,
            in DiagnosticDatasetRow start,
            in DiagnosticDatasetRow end,
            in DiagnosticBoundInput frame,
            string message,
            IEnumerable<DiagnosticEvidence> evidence) => new DiagnosticFinding(
                context.RuleId + "-" + target + "-sequence-" +
                    end.SampleKey.Sequence,
                severity,
                dimension,
                start.SampleKey.Sequence,
                end.SampleKey.Sequence,
                message,
                evidence,
                CharacterFootDiagnosticOperatorSupport.FrameRange(
                    frame,
                    start.GetUInt64(frame.Handle),
                    end.GetUInt64(frame.Handle)));

        internal static DiagnosticOperatorResult Result(
            int eligible,
            IReadOnlyList<DiagnosticFinding> findings,
            string label,
            IEnumerable<DiagnosticEvidence> evidence = null)
        {
            if (eligible == 0)
                return DiagnosticOperatorResult.NotApplicable(
                    $"No {label} window was observed.");
            return new DiagnosticOperatorResult(
                findings.Count == 0
                    ? DiagnosticRuleState.Passed
                    : DiagnosticRuleState.Failed,
                $"{findings.Count} of {eligible} {label} windows failed.",
                null,
                evidence,
                findings);
        }
    }

    internal sealed class CharacterFootReleaseFlybackOperator :
        IDiagnosticAnalysisOperator
    {
        const string Frame = "frame-sequence";
        const string Reset = "reset-sequence";
        const string State = "constraint-state";
        const string Event = "landing-event-identity";
        const string Correction = "final-effective-correction";
        const string ReleasingState = "releasing-state";
        const string Threshold = "correction-excursion-threshold-meters";

        public DiagnosticOperatorDescriptor Descriptor { get; } =
            new DiagnosticOperatorDescriptor(
                "foot/release-flyback",
                "landing",
                new[]
                {
                    CharacterFootLandingPolicySupport.Slot(Frame, DiagnosticValueKind.UInt64),
                    CharacterFootLandingPolicySupport.Slot(Reset, DiagnosticValueKind.UInt64),
                    CharacterFootLandingPolicySupport.Slot(State, DiagnosticValueKind.UInt32),
                    CharacterFootLandingPolicySupport.Slot(Event, DiagnosticValueKind.UInt64),
                    CharacterFootLandingPolicySupport.Slot(Correction, DiagnosticValueKind.Vector3)
                },
                new[]
                {
                    CharacterFootLandingPolicySupport.Integer(ReleasingState),
                    CharacterFootLandingPolicySupport.Number(Threshold)
                });

        public DiagnosticOperatorResult Execute(DiagnosticOperatorExecutionContext context)
        {
            var frame = context.Input(Frame);
            var reset = context.Input(Reset);
            var state = context.Input(State);
            var eventId = context.Input(Event);
            var correction = context.Input(Correction);
            var required = new[] { frame, reset, state, eventId, correction };
            var missing = new HashSet<string>(StringComparer.Ordinal);
            Dictionary<string, List<DiagnosticDatasetRow>> rowsByDimension =
                CharacterFootLandingPolicySupport.Rows(context, required, missing);
            if (missing.Count != 0)
                return DiagnosticOperatorResult.MissingEvidence(
                    CharacterFootDiagnosticOperatorSupport.MissingSummary(missing));
            uint releasing = CharacterFootDiagnosticOperatorSupport.RequireUInt32(
                context,
                ReleasingState);
            double threshold = CharacterFootDiagnosticOperatorSupport.RequireNumber(
                context,
                Threshold);
            var findings = new List<DiagnosticFinding>();
            int eligible = 0;
            foreach (KeyValuePair<string, List<DiagnosticDatasetRow>> pair in rowsByDimension)
            {
                List<DiagnosticDatasetRow> rows = pair.Value;
                int index = 0;
                while (index < rows.Count)
                {
                    if (rows[index].GetUInt32(state.Handle) != releasing)
                    {
                        index++;
                        continue;
                    }
                    int start = index;
                    ulong identity = rows[index].GetUInt64(eventId.Handle);
                    var values = new List<DiagnosticVector3>();
                    while (index < rows.Count &&
                        rows[index].GetUInt32(state.Handle) == releasing &&
                        rows[index].GetUInt64(eventId.Handle) == identity &&
                        (index == start || CharacterFootLandingPolicySupport.Continuous(
                            rows[index - 1], rows[index], frame, reset)))
                    {
                        values.Add(rows[index].GetVector3(correction.Handle));
                        index++;
                    }
                    int end = index - 1;
                    eligible++;
                    double excursion = CharacterFootLandingPolicySupport.Range(values);
                    int reversals = CharacterFootLandingPolicySupport.ReversalCount(values);
                    if (excursion <= threshold || reversals == 0)
                        continue;
                    findings.Add(CharacterFootLandingPolicySupport.Finding(
                        context,
                        "release-flyback",
                        DiagnosticSeverity.Warning,
                        pair.Key,
                        rows[start],
                        rows[end],
                        frame,
                        $"Release correction excursion reached {CharacterFootDiagnosticOperatorSupport.Format(excursion)} m with {reversals} velocity reversals.",
                        new[]
                        {
                            CharacterFootLandingPolicySupport.Evidence(
                                "correction-excursion-meters", correction, pair.Key,
                                rows[start].SampleKey.Sequence, rows[end].SampleKey.Sequence, excursion),
                            CharacterFootLandingPolicySupport.Evidence(
                                "velocity-direction-reversal-count", correction, pair.Key,
                                rows[start].SampleKey.Sequence, rows[end].SampleKey.Sequence, reversals)
                        }));
                }
            }
            return CharacterFootLandingPolicySupport.Result(
                eligible,
                findings,
                "Release");
        }
    }

    internal sealed class CharacterFootSwingToLandingFloorHandoffOperator :
        IDiagnosticAnalysisOperator
    {
        const string Frame = "frame-sequence";
        const string Reset = "reset-sequence";
        const string State = "constraint-state";
        const string AnimatedSole = "animated-sole";
        const string FinalSole = "final-sole";
        const string FloorClamp = "safety-floor-clamp-meters";
        const string Residual = "swing-residual-after-decay";
        const string FormalHeight = "formal-foot-height";
        const string Progress = "swing-progress";
        const string TimeToLanding = "time-to-landing-seconds";
        const string SwingState = "swing-state";
        const string LandingState = "landing-state";
        const string Threshold = "additional-output-step-threshold-meters";

        public DiagnosticOperatorDescriptor Descriptor { get; } =
            new DiagnosticOperatorDescriptor(
                "foot/swing-to-landing-floor-handoff",
                "landing",
                Slots(),
                new[]
                {
                    CharacterFootLandingPolicySupport.Integer(SwingState),
                    CharacterFootLandingPolicySupport.Integer(LandingState),
                    CharacterFootLandingPolicySupport.Number(Threshold)
                });

        public DiagnosticOperatorResult Execute(DiagnosticOperatorExecutionContext context)
        {
            var inputs = Slots(context);
            var missing = new HashSet<string>(StringComparer.Ordinal);
            Dictionary<string, List<DiagnosticDatasetRow>> rowsByDimension =
                CharacterFootLandingPolicySupport.Rows(
                    context,
                    new[] { inputs[Frame], inputs[Reset], inputs[State] },
                    missing);
            if (missing.Count != 0)
                return DiagnosticOperatorResult.MissingEvidence(
                    CharacterFootDiagnosticOperatorSupport.MissingSummary(missing));
            uint swing = CharacterFootDiagnosticOperatorSupport.RequireUInt32(context, SwingState);
            uint landing = CharacterFootDiagnosticOperatorSupport.RequireUInt32(context, LandingState);
            double threshold = CharacterFootDiagnosticOperatorSupport.RequireNumber(context, Threshold);
            var findings = new List<DiagnosticFinding>();
            int eligible = 0;
            foreach (KeyValuePair<string, List<DiagnosticDatasetRow>> pair in rowsByDimension)
            {
                for (int i = 1; i < pair.Value.Count; i++)
                {
                    DiagnosticDatasetRow previous = pair.Value[i - 1];
                    DiagnosticDatasetRow current = pair.Value[i];
                    if (!CharacterFootLandingPolicySupport.Continuous(
                            previous, current, inputs[Frame], inputs[Reset]) ||
                        previous.GetUInt32(inputs[State].Handle) != swing ||
                        current.GetUInt32(inputs[State].Handle) != landing)
                    {
                        continue;
                    }
                    if (!CharacterFootLandingPolicySupport.Require(
                            previous,
                            missing,
                            inputs[AnimatedSole],
                            inputs[FinalSole],
                            inputs[FloorClamp],
                            inputs[Residual],
                            inputs[FormalHeight],
                            inputs[Progress],
                            inputs[TimeToLanding]) ||
                        !CharacterFootLandingPolicySupport.Require(
                            current,
                            missing,
                            inputs[AnimatedSole],
                            inputs[FinalSole]))
                    {
                        continue;
                    }
                    eligible++;
                    double animatedStep = CharacterFootLandingPolicySupport.Distance(
                        previous.GetVector3(inputs[AnimatedSole].Handle),
                        current.GetVector3(inputs[AnimatedSole].Handle));
                    double finalStep = CharacterFootLandingPolicySupport.Distance(
                        previous.GetVector3(inputs[FinalSole].Handle),
                        current.GetVector3(inputs[FinalSole].Handle));
                    double additional = Math.Max(0d, finalStep - animatedStep);
                    if (additional <= threshold)
                        continue;
                    findings.Add(CharacterFootLandingPolicySupport.Finding(
                        context,
                        "swing-to-landing-floor-handoff",
                        CharacterFootDiagnosticOperatorSupport.Severity(additional),
                        pair.Key,
                        previous,
                        current,
                        inputs[Frame],
                        $"Swing-to-Landing added {CharacterFootDiagnosticOperatorSupport.Format(additional)} m beyond animated sole motion.",
                        new[]
                        {
                            CharacterFootLandingPolicySupport.Evidence("additional-output-step-meters", inputs[FinalSole], pair.Key, previous.SampleKey.Sequence, current.SampleKey.Sequence, additional),
                            CharacterFootLandingPolicySupport.Evidence("safety-floor-clamp-meters", inputs[FloorClamp], pair.Key, previous.SampleKey.Sequence, current.SampleKey.Sequence, previous.GetFloat32(inputs[FloorClamp].Handle)),
                            CharacterFootLandingPolicySupport.Evidence("formal-foot-height", inputs[FormalHeight], pair.Key, previous.SampleKey.Sequence, current.SampleKey.Sequence, previous.GetFloat32(inputs[FormalHeight].Handle)),
                            CharacterFootLandingPolicySupport.Evidence("swing-progress", inputs[Progress], pair.Key, previous.SampleKey.Sequence, current.SampleKey.Sequence, previous.GetFloat32(inputs[Progress].Handle)),
                            CharacterFootLandingPolicySupport.Evidence("time-to-landing-seconds", inputs[TimeToLanding], pair.Key, previous.SampleKey.Sequence, current.SampleKey.Sequence, previous.GetFloat32(inputs[TimeToLanding].Handle)),
                            CharacterFootLandingPolicySupport.Evidence("residual-meters", inputs[Residual], pair.Key, previous.SampleKey.Sequence, current.SampleKey.Sequence, Magnitude(previous.GetVector3(inputs[Residual].Handle)))
                        }));
                }
            }
            if (missing.Count != 0)
                return DiagnosticOperatorResult.MissingEvidence(
                    CharacterFootDiagnosticOperatorSupport.MissingSummary(missing));
            return CharacterFootLandingPolicySupport.Result(eligible, findings, "Swing-to-Landing handoff");
        }

        static DiagnosticOperatorInputSlot[] Slots() => new[]
        {
            CharacterFootLandingPolicySupport.Slot(Frame, DiagnosticValueKind.UInt64),
            CharacterFootLandingPolicySupport.Slot(Reset, DiagnosticValueKind.UInt64),
            CharacterFootLandingPolicySupport.Slot(State, DiagnosticValueKind.UInt32),
            CharacterFootLandingPolicySupport.Slot(AnimatedSole, DiagnosticValueKind.Vector3),
            CharacterFootLandingPolicySupport.Slot(FinalSole, DiagnosticValueKind.Vector3),
            CharacterFootLandingPolicySupport.Slot(FloorClamp, DiagnosticValueKind.Float32),
            CharacterFootLandingPolicySupport.Slot(Residual, DiagnosticValueKind.Vector3),
            CharacterFootLandingPolicySupport.Slot(FormalHeight, DiagnosticValueKind.Float32),
            CharacterFootLandingPolicySupport.Slot(Progress, DiagnosticValueKind.Float32),
            CharacterFootLandingPolicySupport.Slot(TimeToLanding, DiagnosticValueKind.Float32)
        };

        static Dictionary<string, DiagnosticBoundInput> Slots(
            DiagnosticOperatorExecutionContext context) => Slots().ToDictionary(
                value => value.Id,
                value => context.Input(value.Id),
                StringComparer.Ordinal);

        static double Magnitude(in DiagnosticVector3 value) => Math.Sqrt(
            value.X * value.X + value.Y * value.Y + value.Z * value.Z);
    }

    internal sealed class CharacterFootPlantInterpolationOutputJumpOperator :
        IDiagnosticAnalysisOperator
    {
        const string Frame = "frame-sequence";
        const string Reset = "reset-sequence";
        const string Evaluated = "plant-interpolation-evaluated";
        const string FinalCorrection = "final-effective-correction";
        const string SelectedTarget = "plant-selected-target";
        const string Desired = "desired-output-point";
        const string Response = "response-output-point";
        const string Residual = "plant-residual-after-decay";
        const string Delta = "delta-seconds";
        const string Threshold = "output-step-threshold-meters";

        public DiagnosticOperatorDescriptor Descriptor { get; } =
            new DiagnosticOperatorDescriptor(
                "foot/plant-interpolation-output-jump",
                "landing",
                Slots(),
                new[] { CharacterFootLandingPolicySupport.Number(Threshold) });

        public DiagnosticOperatorResult Execute(DiagnosticOperatorExecutionContext context)
        {
            Dictionary<string, DiagnosticBoundInput> inputs = Bind(context, Slots());
            var missing = new HashSet<string>(StringComparer.Ordinal);
            Dictionary<string, List<DiagnosticDatasetRow>> rows =
                CharacterFootLandingPolicySupport.Rows(
                    context,
                    new[] { inputs[Frame], inputs[Reset], inputs[Evaluated] },
                    missing);
            if (missing.Count != 0)
                return DiagnosticOperatorResult.MissingEvidence(
                    CharacterFootDiagnosticOperatorSupport.MissingSummary(missing));
            double threshold = CharacterFootDiagnosticOperatorSupport.RequireNumber(context, Threshold);
            var findings = new List<DiagnosticFinding>();
            int eligible = 0;
            foreach (KeyValuePair<string, List<DiagnosticDatasetRow>> pair in rows)
            {
                for (int i = 1; i < pair.Value.Count; i++)
                {
                    DiagnosticDatasetRow previous = pair.Value[i - 1];
                    DiagnosticDatasetRow current = pair.Value[i];
                    if (!CharacterFootLandingPolicySupport.Continuous(previous, current, inputs[Frame], inputs[Reset]) ||
                        !previous.GetBoolean(inputs[Evaluated].Handle) ||
                        !current.GetBoolean(inputs[Evaluated].Handle))
                    {
                        continue;
                    }
                    if (!CharacterFootLandingPolicySupport.Require(
                            previous,
                            missing,
                            inputs[FinalCorrection],
                            inputs[SelectedTarget]) ||
                        !CharacterFootLandingPolicySupport.Require(
                            current,
                            missing,
                            inputs[FinalCorrection],
                            inputs[SelectedTarget],
                            inputs[Desired],
                            inputs[Response],
                            inputs[Residual],
                            inputs[Delta]))
                    {
                        continue;
                    }
                    eligible++;
                    double step = CharacterFootLandingPolicySupport.Distance(
                        previous.GetVector3(inputs[FinalCorrection].Handle),
                        current.GetVector3(inputs[FinalCorrection].Handle));
                    if (step <= threshold)
                        continue;
                    double delta = Math.Max(current.GetFloat32(inputs[Delta].Handle), 0.000001d);
                    findings.Add(CharacterFootLandingPolicySupport.Finding(
                        context,
                        "plant-interpolation-output-jump",
                        CharacterFootDiagnosticOperatorSupport.Severity(step),
                        pair.Key,
                        previous,
                        current,
                        inputs[Frame],
                        $"Plant interpolation output offset stepped {CharacterFootDiagnosticOperatorSupport.Format(step)} m.",
                        new[]
                        {
                            CharacterFootLandingPolicySupport.Evidence("output-step-meters", inputs[FinalCorrection], pair.Key, previous.SampleKey.Sequence, current.SampleKey.Sequence, step),
                            CharacterFootLandingPolicySupport.Evidence("output-speed-meters-per-second", inputs[Delta], pair.Key, previous.SampleKey.Sequence, current.SampleKey.Sequence, step / delta),
                            CharacterFootLandingPolicySupport.Evidence("selected-target-step-meters", inputs[SelectedTarget], pair.Key, previous.SampleKey.Sequence, current.SampleKey.Sequence, CharacterFootLandingPolicySupport.Distance(previous.GetVector3(inputs[SelectedTarget].Handle), current.GetVector3(inputs[SelectedTarget].Handle))),
                            CharacterFootLandingPolicySupport.Evidence("desired-to-response-meters", inputs[Response], pair.Key, previous.SampleKey.Sequence, current.SampleKey.Sequence, CharacterFootLandingPolicySupport.Distance(current.GetVector3(inputs[Desired].Handle), current.GetVector3(inputs[Response].Handle))),
                            CharacterFootLandingPolicySupport.Evidence("residual-after-decay-meters", inputs[Residual], pair.Key, previous.SampleKey.Sequence, current.SampleKey.Sequence, Magnitude(current.GetVector3(inputs[Residual].Handle)))
                        }));
                }
            }
            if (missing.Count != 0)
                return DiagnosticOperatorResult.MissingEvidence(
                    CharacterFootDiagnosticOperatorSupport.MissingSummary(missing));
            return CharacterFootLandingPolicySupport.Result(eligible, findings, "Plant interpolation");
        }

        static DiagnosticOperatorInputSlot[] Slots() => new[]
        {
            CharacterFootLandingPolicySupport.Slot(Frame, DiagnosticValueKind.UInt64),
            CharacterFootLandingPolicySupport.Slot(Reset, DiagnosticValueKind.UInt64),
            CharacterFootLandingPolicySupport.Slot(Evaluated, DiagnosticValueKind.Boolean),
            CharacterFootLandingPolicySupport.Slot(FinalCorrection, DiagnosticValueKind.Vector3),
            CharacterFootLandingPolicySupport.Slot(SelectedTarget, DiagnosticValueKind.Vector3),
            CharacterFootLandingPolicySupport.Slot(Desired, DiagnosticValueKind.Vector3),
            CharacterFootLandingPolicySupport.Slot(Response, DiagnosticValueKind.Vector3),
            CharacterFootLandingPolicySupport.Slot(Residual, DiagnosticValueKind.Vector3),
            CharacterFootLandingPolicySupport.Slot(Delta, DiagnosticValueKind.Float32)
        };

        static Dictionary<string, DiagnosticBoundInput> Bind(
            DiagnosticOperatorExecutionContext context,
            IEnumerable<DiagnosticOperatorInputSlot> slots) => slots.ToDictionary(
                value => value.Id,
                value => context.Input(value.Id),
                StringComparer.Ordinal);

        static double Magnitude(in DiagnosticVector3 value) => Math.Sqrt(
            value.X * value.X + value.Y * value.Y + value.Z * value.Z);
    }

    internal sealed class CharacterFootContactAcquisitionContinuityOperator :
        IDiagnosticAnalysisOperator
    {
        const string Frame = "frame-sequence";
        const string Reset = "reset-sequence";
        const string State = "constraint-state";
        const string AnchorAcquiredFrame = "anchor-acquired-frame-sequence";
        const string Anchor = "anchor-point";
        const string OriginalSole = "original-sole";
        const string FinalSole = "final-sole";
        const string Response = "response-output-point";
        const string Residual = "plant-residual-after-decay";
        const string IdleState = "idle-state";
        const string Threshold = "visible-output-step-threshold-meters";

        public DiagnosticOperatorDescriptor Descriptor { get; } =
            new DiagnosticOperatorDescriptor(
                "foot/contact-acquisition-continuity",
                "landing",
                Slots(),
                new[]
                {
                    CharacterFootLandingPolicySupport.Integer(IdleState),
                    CharacterFootLandingPolicySupport.Number(Threshold)
                });

        public DiagnosticOperatorResult Execute(DiagnosticOperatorExecutionContext context)
        {
            Dictionary<string, DiagnosticBoundInput> inputs = Bind(context, Slots());
            var missing = new HashSet<string>(StringComparer.Ordinal);
            Dictionary<string, List<DiagnosticDatasetRow>> rows =
                CharacterFootLandingPolicySupport.Rows(
                    context,
                    new[] { inputs[Frame], inputs[Reset], inputs[State] },
                    missing);
            if (missing.Count != 0)
                return DiagnosticOperatorResult.MissingEvidence(
                    CharacterFootDiagnosticOperatorSupport.MissingSummary(missing));
            uint idle = CharacterFootDiagnosticOperatorSupport.RequireUInt32(context, IdleState);
            double threshold = CharacterFootDiagnosticOperatorSupport.RequireNumber(context, Threshold);
            var findings = new List<DiagnosticFinding>();
            int eligible = 0;
            foreach (KeyValuePair<string, List<DiagnosticDatasetRow>> pair in rows)
            {
                for (int i = 1; i < pair.Value.Count; i++)
                {
                    DiagnosticDatasetRow previous = pair.Value[i - 1];
                    DiagnosticDatasetRow current = pair.Value[i];
                    if (!CharacterFootLandingPolicySupport.Continuous(previous, current, inputs[Frame], inputs[Reset]) ||
                        current.GetUInt32(inputs[State].Handle) == idle ||
                        !current.IsAvailable(inputs[AnchorAcquiredFrame].Handle) ||
                        current.GetUInt64(inputs[AnchorAcquiredFrame].Handle) !=
                            current.GetUInt64(inputs[Frame].Handle))
                    {
                        continue;
                    }
                    if (!CharacterFootLandingPolicySupport.Require(
                            previous,
                            missing,
                            inputs[FinalSole]) ||
                        !CharacterFootLandingPolicySupport.Require(
                            current,
                            missing,
                            inputs[Anchor],
                            inputs[OriginalSole],
                            inputs[FinalSole],
                            inputs[Response],
                            inputs[Residual]))
                    {
                        continue;
                    }
                    eligible++;
                    double visibleStep = CharacterFootLandingPolicySupport.Distance(
                        previous.GetVector3(inputs[FinalSole].Handle),
                        current.GetVector3(inputs[FinalSole].Handle));
                    if (visibleStep <= threshold)
                        continue;
                    findings.Add(CharacterFootLandingPolicySupport.Finding(
                        context,
                        "contact-acquisition-continuity",
                        CharacterFootDiagnosticOperatorSupport.Severity(visibleStep),
                        pair.Key,
                        previous,
                        current,
                        inputs[Frame],
                        $"Contact acquisition changed the visible final sole by {CharacterFootDiagnosticOperatorSupport.Format(visibleStep)} m.",
                        new[]
                        {
                            CharacterFootLandingPolicySupport.Evidence("visible-output-step-meters", inputs[FinalSole], pair.Key, previous.SampleKey.Sequence, current.SampleKey.Sequence, visibleStep),
                            CharacterFootLandingPolicySupport.Evidence("original-sole-to-anchor-meters", inputs[OriginalSole], pair.Key, previous.SampleKey.Sequence, current.SampleKey.Sequence, CharacterFootLandingPolicySupport.Distance(current.GetVector3(inputs[OriginalSole].Handle), current.GetVector3(inputs[Anchor].Handle))),
                            CharacterFootLandingPolicySupport.Evidence("response-to-anchor-meters", inputs[Response], pair.Key, previous.SampleKey.Sequence, current.SampleKey.Sequence, CharacterFootLandingPolicySupport.Distance(current.GetVector3(inputs[Response].Handle), current.GetVector3(inputs[Anchor].Handle))),
                            CharacterFootLandingPolicySupport.Evidence("residual-after-decay-meters", inputs[Residual], pair.Key, previous.SampleKey.Sequence, current.SampleKey.Sequence, Magnitude(current.GetVector3(inputs[Residual].Handle)))
                        }));
                }
            }
            if (missing.Count != 0)
                return DiagnosticOperatorResult.MissingEvidence(
                    CharacterFootDiagnosticOperatorSupport.MissingSummary(missing));
            return CharacterFootLandingPolicySupport.Result(eligible, findings, "Contact acquisition");
        }

        static DiagnosticOperatorInputSlot[] Slots() => new[]
        {
            CharacterFootLandingPolicySupport.Slot(Frame, DiagnosticValueKind.UInt64),
            CharacterFootLandingPolicySupport.Slot(Reset, DiagnosticValueKind.UInt64),
            CharacterFootLandingPolicySupport.Slot(State, DiagnosticValueKind.UInt32),
            CharacterFootLandingPolicySupport.Slot(AnchorAcquiredFrame, DiagnosticValueKind.UInt64),
            CharacterFootLandingPolicySupport.Slot(Anchor, DiagnosticValueKind.Vector3),
            CharacterFootLandingPolicySupport.Slot(OriginalSole, DiagnosticValueKind.Vector3),
            CharacterFootLandingPolicySupport.Slot(FinalSole, DiagnosticValueKind.Vector3),
            CharacterFootLandingPolicySupport.Slot(Response, DiagnosticValueKind.Vector3),
            CharacterFootLandingPolicySupport.Slot(Residual, DiagnosticValueKind.Vector3)
        };

        static Dictionary<string, DiagnosticBoundInput> Bind(
            DiagnosticOperatorExecutionContext context,
            IEnumerable<DiagnosticOperatorInputSlot> slots) => slots.ToDictionary(
                value => value.Id,
                value => context.Input(value.Id),
                StringComparer.Ordinal);

        static double Magnitude(in DiagnosticVector3 value) => Math.Sqrt(
            value.X * value.X + value.Y * value.Y + value.Z * value.Z);
    }

    internal sealed class CharacterFootLockWeightCompletionByContactEventOperator :
        IDiagnosticAnalysisOperator
    {
        const string Frame = "frame-sequence";
        const string Reset = "reset-sequence";
        const string Event = "lock-request-event-identity";
        const string Requested = "lock-requested";
        const string Weight = "lock-weight";
        const string State = "constraint-state";
        const string OutputDistance = "plant-output-distance";
        const string Penetration = "plant-penetration-depth";
        const string ReachEvaluated = "landing-reach-evaluated";
        const string ReachAvailable = "landing-reach-available";
        const string ReachLegLength = "landing-reach-leg-length";
        const string ReachCompressionReserve = "landing-reach-compression-reserve";
        const string LockedState = "locked-state";
        const string FullWeight = "full-weight-threshold";
        const string CompletionTolerance = "landing-lock-completion-tolerance-meters";
        const string PenetrationTolerance = "ground-penetration-tolerance-meters";

        public DiagnosticOperatorDescriptor Descriptor { get; } =
            new DiagnosticOperatorDescriptor(
                "foot/lock-weight-completion-by-contact-event",
                "landing",
                Slots(),
                new[]
                {
                    CharacterFootLandingPolicySupport.Integer(LockedState),
                    CharacterFootLandingPolicySupport.Number(FullWeight),
                    CharacterFootLandingPolicySupport.Number(CompletionTolerance),
                    CharacterFootLandingPolicySupport.Number(PenetrationTolerance)
                });

        public DiagnosticOperatorResult Execute(DiagnosticOperatorExecutionContext context)
        {
            Dictionary<string, DiagnosticBoundInput> inputs = Bind(context, Slots());
            var missing = new HashSet<string>(StringComparer.Ordinal);
            Dictionary<string, List<DiagnosticDatasetRow>> rows =
                CharacterFootLandingPolicySupport.Rows(
                    context,
                    new[]
                    {
                        inputs[Frame],
                        inputs[Reset],
                        inputs[Event],
                        inputs[Requested],
                        inputs[Weight],
                        inputs[State]
                    },
                    missing);
            if (missing.Count != 0)
                return DiagnosticOperatorResult.MissingEvidence(
                    CharacterFootDiagnosticOperatorSupport.MissingSummary(missing));
            uint locked = CharacterFootDiagnosticOperatorSupport.RequireUInt32(context, LockedState);
            double fullWeight = CharacterFootDiagnosticOperatorSupport.RequireNumber(context, FullWeight);
            double completion = CharacterFootDiagnosticOperatorSupport.RequireNumber(context, CompletionTolerance);
            double penetration = CharacterFootDiagnosticOperatorSupport.RequireNumber(context, PenetrationTolerance);
            var findings = new List<DiagnosticFinding>();
            int eligible = 0;
            foreach (KeyValuePair<string, List<DiagnosticDatasetRow>> pair in rows)
            {
                int index = 0;
                while (index < pair.Value.Count)
                {
                    DiagnosticDatasetRow first = pair.Value[index];
                    if (!first.GetBoolean(inputs[Requested].Handle) ||
                        first.GetUInt64(inputs[Event].Handle) == 0)
                    {
                        index++;
                        continue;
                    }
                    ulong eventIdentity = first.GetUInt64(inputs[Event].Handle);
                    int start = index;
                    while (index + 1 < pair.Value.Count &&
                        CharacterFootLandingPolicySupport.Continuous(
                            pair.Value[index], pair.Value[index + 1], inputs[Frame], inputs[Reset]) &&
                        pair.Value[index + 1].GetUInt64(inputs[Event].Handle) == eventIdentity)
                    {
                        index++;
                    }
                    int end = index;
                    DiagnosticDatasetRow[] window = pair.Value.Skip(start).Take(end - start + 1).ToArray();
                    eligible++;
                    double maximumWeight = window.Max(value => value.GetFloat32(inputs[Weight].Handle));
                    bool reachedFull = maximumWeight >= fullWeight;
                    bool enteredLocked = window.Any(value => value.GetUInt32(inputs[State].Handle) == locked);
                    bool geometryMissing = false;
                    bool reachEvaluated = false;
                    if (reachedFull)
                    {
                        for (int rowIndex = 0; rowIndex < window.Length; rowIndex++)
                        {
                            geometryMissing |= !CharacterFootLandingPolicySupport.Require(
                                window[rowIndex],
                                missing,
                                inputs[OutputDistance],
                                inputs[Penetration],
                                inputs[ReachEvaluated],
                                inputs[ReachAvailable]);
                            if (!window[rowIndex].IsAvailable(inputs[ReachEvaluated].Handle) ||
                                !window[rowIndex].GetBoolean(inputs[ReachEvaluated].Handle))
                            {
                                continue;
                            }
                            reachEvaluated = true;
                            geometryMissing |= !CharacterFootLandingPolicySupport.Require(
                                window[rowIndex],
                                missing,
                                inputs[ReachLegLength],
                                inputs[ReachCompressionReserve]);
                        }
                        if (!reachEvaluated)
                        {
                            missing.Add("landing-reach-not-evaluated");
                            geometryMissing = true;
                        }
                    }
                    if (geometryMissing)
                    {
                        index++;
                        continue;
                    }
                    bool geometryClosed = window.Any(value =>
                        !reachedFull ||
                        value.GetFloat32(inputs[OutputDistance].Handle) <= completion &&
                        value.GetFloat32(inputs[Penetration].Handle) <= penetration &&
                        value.GetBoolean(inputs[ReachAvailable].Handle));
                    bool failure = reachedFull && !(enteredLocked && geometryClosed) ||
                        !reachedFull && enteredLocked;
                    if (failure)
                    {
                        findings.Add(CharacterFootLandingPolicySupport.Finding(
                            context,
                            "lock-weight-completion",
                            DiagnosticSeverity.Warning,
                            pair.Key,
                            first,
                            pair.Value[end],
                            inputs[Frame],
                            $"Contact event fullWeight={reachedFull}, enteredLocked={enteredLocked}, geometryClosed={geometryClosed}.",
                            new[]
                            {
                                CharacterFootLandingPolicySupport.Evidence("maximum-lock-weight", inputs[Weight], pair.Key, first.SampleKey.Sequence, pair.Value[end].SampleKey.Sequence, maximumWeight),
                                CharacterFootLandingPolicySupport.Evidence("entered-locked", inputs[State], pair.Key, first.SampleKey.Sequence, pair.Value[end].SampleKey.Sequence, enteredLocked),
                                CharacterFootLandingPolicySupport.Evidence("geometry-closed", inputs[OutputDistance], pair.Key, first.SampleKey.Sequence, pair.Value[end].SampleKey.Sequence, geometryClosed)
                            }));
                    }
                    index++;
                }
            }
            if (missing.Count != 0)
                return DiagnosticOperatorResult.MissingEvidence(
                    CharacterFootDiagnosticOperatorSupport.MissingSummary(missing));
            return CharacterFootLandingPolicySupport.Result(eligible, findings, "Contact lock-weight event");
        }

        static DiagnosticOperatorInputSlot[] Slots() => new[]
        {
            CharacterFootLandingPolicySupport.Slot(Frame, DiagnosticValueKind.UInt64),
            CharacterFootLandingPolicySupport.Slot(Reset, DiagnosticValueKind.UInt64),
            CharacterFootLandingPolicySupport.Slot(Event, DiagnosticValueKind.UInt64),
            CharacterFootLandingPolicySupport.Slot(Requested, DiagnosticValueKind.Boolean),
            CharacterFootLandingPolicySupport.Slot(Weight, DiagnosticValueKind.Float32),
            CharacterFootLandingPolicySupport.Slot(State, DiagnosticValueKind.UInt32),
            CharacterFootLandingPolicySupport.Slot(OutputDistance, DiagnosticValueKind.Float32),
            CharacterFootLandingPolicySupport.Slot(Penetration, DiagnosticValueKind.Float32),
            CharacterFootLandingPolicySupport.Slot(ReachEvaluated, DiagnosticValueKind.Boolean),
            CharacterFootLandingPolicySupport.Slot(ReachAvailable, DiagnosticValueKind.Boolean),
            CharacterFootLandingPolicySupport.Slot(ReachLegLength, DiagnosticValueKind.Float32),
            CharacterFootLandingPolicySupport.Slot(ReachCompressionReserve, DiagnosticValueKind.Float32)
        };

        static Dictionary<string, DiagnosticBoundInput> Bind(
            DiagnosticOperatorExecutionContext context,
            IEnumerable<DiagnosticOperatorInputSlot> slots) => slots.ToDictionary(
                value => value.Id,
                value => context.Input(value.Id),
                StringComparer.Ordinal);
    }

    internal sealed class CharacterFootApproachProgressOwnershipOperator :
        IDiagnosticAnalysisOperator
    {
        const string Frame = "frame-sequence";
        const string Reset = "reset-sequence";
        const string InApproach = "in-approach";
        const string Event = "approach-event-identity";
        const string Source = "source-identity";
        const string SourceCycle = "source-cycle";
        const string Progress = "approach-progress";
        const string PlantEvaluated = "plant-interpolation-evaluated";
        const string PlantEvent = "plant-event-identity";
        const string ResidualCaptureReason = "residual-capture-reason";
        const string ResidualEvent = "residual-event-identity";
        const string FinalCorrection = "final-effective-correction";
        const string FormalWeight = "formal-foot-placement-weight";
        const string PositionWeight = "goal-position-weight";
        const string Epsilon = "comparison-epsilon";
        const string NoResidualCaptureReason = "no-residual-capture-reason";

        public DiagnosticOperatorDescriptor Descriptor { get; } =
            new DiagnosticOperatorDescriptor(
                "foot/approach-progress-ownership",
                "landing",
                Slots(),
                new[]
                {
                    CharacterFootLandingPolicySupport.Number(Epsilon),
                    CharacterFootLandingPolicySupport.Integer(NoResidualCaptureReason)
                });

        public DiagnosticOperatorResult Execute(DiagnosticOperatorExecutionContext context)
        {
            Dictionary<string, DiagnosticBoundInput> inputs = Bind(context, Slots());
            var missing = new HashSet<string>(StringComparer.Ordinal);
            Dictionary<string, List<DiagnosticDatasetRow>> rows =
                CharacterFootLandingPolicySupport.Rows(
                    context,
                    new[]
                    {
                        inputs[Frame],
                        inputs[Reset],
                        inputs[InApproach],
                        inputs[Event],
                        inputs[Source],
                        inputs[SourceCycle],
                        inputs[Progress]
                    },
                    missing);
            if (missing.Count != 0)
                return DiagnosticOperatorResult.MissingEvidence(
                    CharacterFootDiagnosticOperatorSupport.MissingSummary(missing));
            double epsilon = CharacterFootDiagnosticOperatorSupport.RequireNumber(context, Epsilon);
            uint noResidualCaptureReason =
                CharacterFootDiagnosticOperatorSupport.RequireUInt32(
                    context,
                    NoResidualCaptureReason);
            var findings = new List<DiagnosticFinding>();
            int eligible = 0;
            foreach (KeyValuePair<string, List<DiagnosticDatasetRow>> pair in rows)
            {
                DiagnosticDatasetRow? previous = null;
                for (int i = 0; i < pair.Value.Count; i++)
                {
                    DiagnosticDatasetRow current = pair.Value[i];
                    if (!current.GetBoolean(inputs[InApproach].Handle) ||
                        current.GetUInt64(inputs[Event].Handle) == 0)
                    {
                        previous = current;
                        continue;
                    }
                    if (!CharacterFootLandingPolicySupport.Require(
                            current,
                            missing,
                            inputs[PlantEvaluated],
                            inputs[PlantEvent],
                            inputs[ResidualCaptureReason],
                            inputs[ResidualEvent],
                            inputs[FinalCorrection],
                            inputs[FormalWeight],
                            inputs[PositionWeight]))
                    {
                        previous = current;
                        continue;
                    }
                    eligible++;
                    bool sameLineage = previous.HasValue &&
                        CharacterFootLandingPolicySupport.Continuous(
                            previous.Value, current, inputs[Frame], inputs[Reset]) &&
                        previous.Value.GetBoolean(inputs[InApproach].Handle) &&
                        previous.Value.GetUInt64(inputs[Event].Handle) ==
                            current.GetUInt64(inputs[Event].Handle) &&
                        previous.Value.GetIdentity(inputs[Source].Handle) ==
                            current.GetIdentity(inputs[Source].Handle) &&
                        previous.Value.GetInt32(inputs[SourceCycle].Handle) ==
                            current.GetInt32(inputs[SourceCycle].Handle);
                    double progressDelta = sameLineage
                        ? current.GetFloat32(inputs[Progress].Handle) -
                            previous.Value.GetFloat32(inputs[Progress].Handle)
                        : 0d;
                    bool monotonic = !sameLineage || progressDelta >= -epsilon;
                    bool plantOwned = current.GetBoolean(inputs[PlantEvaluated].Handle) &&
                        current.GetUInt64(inputs[PlantEvent].Handle) ==
                            current.GetUInt64(inputs[Event].Handle);
                    bool residualOwned =
                        current.GetUInt32(inputs[ResidualCaptureReason].Handle) !=
                            noResidualCaptureReason &&
                        current.GetUInt64(inputs[ResidualEvent].Handle) ==
                            current.GetUInt64(inputs[Event].Handle);
                    bool visibleOwned = current.GetFloat32(inputs[PositionWeight].Handle) > epsilon;
                    if (!monotonic || plantOwned || residualOwned || visibleOwned)
                    {
                        DiagnosticDatasetRow start = sameLineage ? previous.Value : current;
                        findings.Add(CharacterFootLandingPolicySupport.Finding(
                            context,
                            "approach-progress-ownership",
                            DiagnosticSeverity.Warning,
                            pair.Key,
                            start,
                            current,
                            inputs[Frame],
                            $"Approach monotonic={monotonic}, plantOwned={plantOwned}, residualOwned={residualOwned}, visibleOwned={visibleOwned}.",
                            new[]
                            {
                                CharacterFootLandingPolicySupport.Evidence("progress-delta", inputs[Progress], pair.Key, start.SampleKey.Sequence, current.SampleKey.Sequence, progressDelta),
                                CharacterFootLandingPolicySupport.Evidence("plant-owned", inputs[PlantEvaluated], pair.Key, start.SampleKey.Sequence, current.SampleKey.Sequence, plantOwned),
                                CharacterFootLandingPolicySupport.Evidence("residual-owned", inputs[ResidualCaptureReason], pair.Key, start.SampleKey.Sequence, current.SampleKey.Sequence, residualOwned),
                                CharacterFootLandingPolicySupport.Evidence("visible-position-owned", inputs[PositionWeight], pair.Key, start.SampleKey.Sequence, current.SampleKey.Sequence, visibleOwned),
                                CharacterFootLandingPolicySupport.Evidence("formal-weight", inputs[FormalWeight], pair.Key, start.SampleKey.Sequence, current.SampleKey.Sequence, current.GetFloat32(inputs[FormalWeight].Handle)),
                                CharacterFootLandingPolicySupport.Evidence("final-correction-meters", inputs[FinalCorrection], pair.Key, start.SampleKey.Sequence, current.SampleKey.Sequence, Magnitude(current.GetVector3(inputs[FinalCorrection].Handle)))
                            }));
                    }
                    previous = current;
                }
            }
            if (missing.Count != 0)
                return DiagnosticOperatorResult.MissingEvidence(
                    CharacterFootDiagnosticOperatorSupport.MissingSummary(missing));
            return CharacterFootLandingPolicySupport.Result(eligible, findings, "Approach ownership");
        }

        static DiagnosticOperatorInputSlot[] Slots() => new[]
        {
            CharacterFootLandingPolicySupport.Slot(Frame, DiagnosticValueKind.UInt64),
            CharacterFootLandingPolicySupport.Slot(Reset, DiagnosticValueKind.UInt64),
            CharacterFootLandingPolicySupport.Slot(InApproach, DiagnosticValueKind.Boolean),
            CharacterFootLandingPolicySupport.Slot(Event, DiagnosticValueKind.UInt64),
            CharacterFootLandingPolicySupport.Slot(Source, DiagnosticValueKind.Identity),
            CharacterFootLandingPolicySupport.Slot(SourceCycle, DiagnosticValueKind.Int32),
            CharacterFootLandingPolicySupport.Slot(Progress, DiagnosticValueKind.Float32),
            CharacterFootLandingPolicySupport.Slot(PlantEvaluated, DiagnosticValueKind.Boolean),
            CharacterFootLandingPolicySupport.Slot(PlantEvent, DiagnosticValueKind.UInt64),
            CharacterFootLandingPolicySupport.Slot(ResidualCaptureReason, DiagnosticValueKind.UInt32),
            CharacterFootLandingPolicySupport.Slot(ResidualEvent, DiagnosticValueKind.UInt64),
            CharacterFootLandingPolicySupport.Slot(FinalCorrection, DiagnosticValueKind.Vector3),
            CharacterFootLandingPolicySupport.Slot(FormalWeight, DiagnosticValueKind.Float32),
            CharacterFootLandingPolicySupport.Slot(PositionWeight, DiagnosticValueKind.Float32)
        };

        static Dictionary<string, DiagnosticBoundInput> Bind(
            DiagnosticOperatorExecutionContext context,
            IEnumerable<DiagnosticOperatorInputSlot> slots) => slots.ToDictionary(
                value => value.Id,
                value => context.Input(value.Id),
                StringComparer.Ordinal);

        static double Magnitude(in DiagnosticVector3 value) => Math.Sqrt(
            value.X * value.X + value.Y * value.Y + value.Z * value.Z);
    }

    internal sealed class CharacterFootActionHardOwnershipOperator :
        IDiagnosticAnalysisOperator
    {
        const string Frame = "frame-sequence";
        const string ActionIdentity = "action-instance-identity";
        const string ActionWeight = "action-foot-weight";
        const string Grounded = "grounded";
        const string Authoritative = "current-step-authoritative";
        const string HardLoss = "hard-ownership-loss";
        const string Suppress = "pre-transition-suppress-output";
        const string ResetInterpolation = "pre-transition-reset-interpolation";
        const string Epsilon = "comparison-epsilon";

        public DiagnosticOperatorDescriptor Descriptor { get; } =
            new DiagnosticOperatorDescriptor(
                "foot/action-hard-ownership",
                "landing",
                Slots(),
                new[] { CharacterFootLandingPolicySupport.Number(Epsilon) });

        public DiagnosticOperatorResult Execute(DiagnosticOperatorExecutionContext context)
        {
            Dictionary<string, DiagnosticBoundInput> inputs = Bind(context, Slots());
            var missing = new HashSet<string>(StringComparer.Ordinal);
            Dictionary<string, List<DiagnosticDatasetRow>> rows =
                CharacterFootLandingPolicySupport.Rows(
                    context,
                    inputs.Values.ToArray(),
                    missing);
            if (missing.Count != 0)
                return DiagnosticOperatorResult.MissingEvidence(
                    CharacterFootDiagnosticOperatorSupport.MissingSummary(missing));
            double epsilon = CharacterFootDiagnosticOperatorSupport.RequireNumber(context, Epsilon);
            var findings = new List<DiagnosticFinding>();
            int eligible = 0;
            foreach (KeyValuePair<string, List<DiagnosticDatasetRow>> pair in rows)
            {
                for (int i = 0; i < pair.Value.Count; i++)
                {
                    DiagnosticDatasetRow row = pair.Value[i];
                    bool occupied = row.GetUInt64(inputs[ActionIdentity].Handle) != 0 ||
                        row.GetFloat32(inputs[ActionWeight].Handle) > epsilon;
                    if (!occupied)
                        continue;
                    eligible++;
                    bool authoritative = row.GetBoolean(inputs[Grounded].Handle) &&
                        row.GetBoolean(inputs[Authoritative].Handle);
                    bool invalid = authoritative &&
                        (row.GetBoolean(inputs[HardLoss].Handle) ||
                         row.GetBoolean(inputs[Suppress].Handle) ||
                         row.GetBoolean(inputs[ResetInterpolation].Handle));
                    if (!invalid)
                        continue;
                    findings.Add(CharacterFootLandingPolicySupport.Finding(
                        context,
                        "action-hard-ownership",
                        DiagnosticSeverity.Warning,
                        pair.Key,
                        row,
                        row,
                        inputs[Frame],
                        "Action occupancy changed Hard Ownership while Grounded and Current Step remained authoritative.",
                        new[]
                        {
                            CharacterFootLandingPolicySupport.Evidence("action-foot-weight", inputs[ActionWeight], pair.Key, row.SampleKey.Sequence, row.SampleKey.Sequence, row.GetFloat32(inputs[ActionWeight].Handle)),
                            CharacterFootLandingPolicySupport.Evidence("hard-ownership-loss", inputs[HardLoss], pair.Key, row.SampleKey.Sequence, row.SampleKey.Sequence, row.GetBoolean(inputs[HardLoss].Handle)),
                            CharacterFootLandingPolicySupport.Evidence("suppress-output", inputs[Suppress], pair.Key, row.SampleKey.Sequence, row.SampleKey.Sequence, row.GetBoolean(inputs[Suppress].Handle))
                        }));
                }
            }
            return CharacterFootLandingPolicySupport.Result(eligible, findings, "Action ownership");
        }

        static DiagnosticOperatorInputSlot[] Slots() => new[]
        {
            CharacterFootLandingPolicySupport.Slot(Frame, DiagnosticValueKind.UInt64),
            CharacterFootLandingPolicySupport.Slot(ActionIdentity, DiagnosticValueKind.UInt64),
            CharacterFootLandingPolicySupport.Slot(ActionWeight, DiagnosticValueKind.Float32),
            CharacterFootLandingPolicySupport.Slot(Grounded, DiagnosticValueKind.Boolean),
            CharacterFootLandingPolicySupport.Slot(Authoritative, DiagnosticValueKind.Boolean),
            CharacterFootLandingPolicySupport.Slot(HardLoss, DiagnosticValueKind.Boolean),
            CharacterFootLandingPolicySupport.Slot(Suppress, DiagnosticValueKind.Boolean),
            CharacterFootLandingPolicySupport.Slot(ResetInterpolation, DiagnosticValueKind.Boolean)
        };

        static Dictionary<string, DiagnosticBoundInput> Bind(
            DiagnosticOperatorExecutionContext context,
            IEnumerable<DiagnosticOperatorInputSlot> slots) => slots.ToDictionary(
                value => value.Id,
                value => context.Input(value.Id),
                StringComparer.Ordinal);
    }

    internal sealed class CharacterFootContactTransitionContextOperator :
        IDiagnosticAnalysisOperator
    {
        const string Frame = "frame-sequence";
        const string Reset = "reset-sequence";
        const string ContactEdge = "contact-edge";
        const string PreviousLatestEvent = "previous-latest-contact-event-identity";
        const string CurrentLatestEvent = "current-latest-contact-event-identity";
        const string PreviousReleasedEvent = "previous-latest-released-event-identity";
        const string CurrentReleasedEvent = "current-latest-released-event-identity";
        const string PreviousCompletedWeightEvent = "previous-completed-lock-weight-event-identity";
        const string CurrentCompletedWeightEvent = "current-completed-lock-weight-event-identity";
        const string ReentryRefreshed = "same-event-reentry-refreshed";
        const string ReentryUnavailable = "same-event-reentry-unavailable";
        const string PreviousRequested = "previous-lock-requested";
        const string CurrentRequested = "current-lock-requested";
        const string PreviousEvent = "previous-lock-event-identity";
        const string CurrentEvent = "current-lock-event-identity";
        const string PreviousMode = "previous-lock-mode";
        const string CurrentMode = "current-lock-mode";
        const string PreviousWeight = "previous-lock-weight";
        const string CurrentWeight = "current-lock-weight";
        const string PreviousEdgeSeconds = "previous-contact-edge-seconds";
        const string CurrentEdgeSeconds = "current-contact-edge-seconds";
        const string PreviousAnchorAvailable = "previous-anchor-available";
        const string CurrentAnchorAvailable = "current-anchor-available";
        const string PreviousAnchorEvent = "previous-anchor-event-identity";
        const string CurrentAnchorEvent = "current-anchor-event-identity";
        const string PreviousAnchorPoint = "previous-anchor-point";
        const string CurrentAnchorPoint = "current-anchor-point";
        const string PreviousAnchorNormal = "previous-anchor-normal";
        const string CurrentAnchorNormal = "current-anchor-normal";
        const string Epsilon = "comparison-epsilon";
        const string NoContactEdge = "no-contact-edge";

        public DiagnosticOperatorDescriptor Descriptor { get; } =
            new DiagnosticOperatorDescriptor(
                "foot/contact-transition-context",
                "landing",
                Slots(),
                new[]
                {
                    CharacterFootLandingPolicySupport.Number(Epsilon),
                    CharacterFootLandingPolicySupport.Integer(NoContactEdge)
                });

        public DiagnosticOperatorResult Execute(DiagnosticOperatorExecutionContext context)
        {
            Dictionary<string, DiagnosticBoundInput> inputs = Bind(context, Slots());
            var missing = new HashSet<string>(StringComparer.Ordinal);
            Dictionary<string, List<DiagnosticDatasetRow>> rows =
                CharacterFootLandingPolicySupport.Rows(
                    context,
                    new[]
                    {
                        inputs[Frame],
                        inputs[Reset],
                        inputs[ContactEdge],
                        inputs[PreviousLatestEvent],
                        inputs[CurrentLatestEvent],
                        inputs[PreviousReleasedEvent],
                        inputs[CurrentReleasedEvent],
                        inputs[PreviousCompletedWeightEvent],
                        inputs[CurrentCompletedWeightEvent],
                        inputs[PreviousAnchorAvailable],
                        inputs[CurrentAnchorAvailable],
                        inputs[ReentryRefreshed],
                        inputs[ReentryUnavailable]
                    },
                    missing);
            if (missing.Count != 0)
                return DiagnosticOperatorResult.MissingEvidence(
                    CharacterFootDiagnosticOperatorSupport.MissingSummary(missing));
            double epsilon = CharacterFootDiagnosticOperatorSupport.RequireNumber(context, Epsilon);
            uint noContactEdge = CharacterFootDiagnosticOperatorSupport.RequireUInt32(
                context,
                NoContactEdge);
            var findings = new List<DiagnosticFinding>();
            int eligible = 0;
            foreach (KeyValuePair<string, List<DiagnosticDatasetRow>> pair in rows)
            {
                for (int i = 1; i < pair.Value.Count; i++)
                {
                    DiagnosticDatasetRow previous = pair.Value[i - 1];
                    DiagnosticDatasetRow current = pair.Value[i];
                    if (!CharacterFootLandingPolicySupport.Continuous(
                            previous, current, inputs[Frame], inputs[Reset]))
                    {
                        continue;
                    }
                    bool contactRelevant =
                        current.GetUInt32(inputs[ContactEdge].Handle) != noContactEdge ||
                        current.GetBoolean(inputs[PreviousAnchorAvailable].Handle) ||
                        current.GetBoolean(inputs[CurrentAnchorAvailable].Handle) ||
                        current.GetUInt64(inputs[PreviousLatestEvent].Handle) != 0 ||
                        current.GetUInt64(inputs[CurrentLatestEvent].Handle) != 0 ||
                        current.GetUInt64(inputs[PreviousReleasedEvent].Handle) != 0 ||
                        current.GetUInt64(inputs[CurrentReleasedEvent].Handle) != 0 ||
                        current.GetBoolean(inputs[ReentryRefreshed].Handle) ||
                        current.GetBoolean(inputs[ReentryUnavailable].Handle);
                    if (!contactRelevant)
                        continue;
                    if (!CharacterFootLandingPolicySupport.Require(
                            current,
                            missing,
                            inputs[PreviousRequested],
                            inputs[PreviousEvent],
                            inputs[PreviousMode],
                            inputs[PreviousWeight],
                            inputs[PreviousEdgeSeconds],
                            inputs[PreviousAnchorAvailable]) ||
                        !CharacterFootLandingPolicySupport.Require(
                            previous,
                            missing,
                            inputs[CurrentRequested],
                            inputs[CurrentEvent],
                            inputs[CurrentMode],
                            inputs[CurrentWeight],
                            inputs[CurrentEdgeSeconds],
                            inputs[CurrentAnchorAvailable]))
                    {
                        continue;
                    }
                    eligible++;
                    bool previousAnchorAvailable = current.GetBoolean(
                        inputs[PreviousAnchorAvailable].Handle);
                    bool priorAnchorAvailable = previous.GetBoolean(
                        inputs[CurrentAnchorAvailable].Handle);
                    bool anchorComplete = !previousAnchorAvailable && !priorAnchorAvailable ||
                        CharacterFootLandingPolicySupport.Require(
                            current,
                            missing,
                            inputs[PreviousAnchorEvent],
                            inputs[PreviousAnchorPoint],
                            inputs[PreviousAnchorNormal]) &&
                        CharacterFootLandingPolicySupport.Require(
                            previous,
                            missing,
                            inputs[CurrentAnchorEvent],
                            inputs[CurrentAnchorPoint],
                            inputs[CurrentAnchorNormal]);
                    if (!anchorComplete)
                        continue;
                    bool anchorConsistent = previousAnchorAvailable == priorAnchorAvailable &&
                        (!previousAnchorAvailable ||
                            current.GetUInt64(inputs[PreviousAnchorEvent].Handle) ==
                                previous.GetUInt64(inputs[CurrentAnchorEvent].Handle) &&
                            CharacterFootLandingPolicySupport.Distance(
                                current.GetVector3(inputs[PreviousAnchorPoint].Handle),
                                previous.GetVector3(inputs[CurrentAnchorPoint].Handle)) <= epsilon &&
                            CharacterFootLandingPolicySupport.Distance(
                                current.GetVector3(inputs[PreviousAnchorNormal].Handle),
                                previous.GetVector3(inputs[CurrentAnchorNormal].Handle)) <= epsilon);
                    bool consistent =
                        current.GetBoolean(inputs[PreviousRequested].Handle) ==
                            previous.GetBoolean(inputs[CurrentRequested].Handle) &&
                        current.GetUInt64(inputs[PreviousEvent].Handle) ==
                            previous.GetUInt64(inputs[CurrentEvent].Handle) &&
                        current.GetUInt32(inputs[PreviousMode].Handle) ==
                            previous.GetUInt32(inputs[CurrentMode].Handle) &&
                        Math.Abs(current.GetFloat32(inputs[PreviousWeight].Handle) -
                            previous.GetFloat32(inputs[CurrentWeight].Handle)) <= epsilon &&
                        Math.Abs(current.GetFloat32(inputs[PreviousEdgeSeconds].Handle) -
                            previous.GetFloat32(inputs[CurrentEdgeSeconds].Handle)) <= epsilon &&
                        current.GetUInt64(inputs[PreviousLatestEvent].Handle) ==
                            previous.GetUInt64(inputs[CurrentLatestEvent].Handle) &&
                        current.GetUInt64(inputs[PreviousReleasedEvent].Handle) ==
                            previous.GetUInt64(inputs[CurrentReleasedEvent].Handle) &&
                        current.GetUInt64(inputs[PreviousCompletedWeightEvent].Handle) ==
                            previous.GetUInt64(inputs[CurrentCompletedWeightEvent].Handle) &&
                        anchorConsistent;
                    if (consistent)
                        continue;
                    findings.Add(CharacterFootLandingPolicySupport.Finding(
                        context,
                        "contact-transition-context",
                        DiagnosticSeverity.Error,
                        pair.Key,
                        previous,
                        current,
                        inputs[Frame],
                        "Committed previous/current Contact Transition context did not continue from the preceding frame.",
                        new[]
                        {
                            CharacterFootLandingPolicySupport.Evidence("context-consistent", inputs[PreviousEvent], pair.Key, previous.SampleKey.Sequence, current.SampleKey.Sequence, false),
                            CharacterFootLandingPolicySupport.Evidence("previous-lock-weight", inputs[PreviousWeight], pair.Key, previous.SampleKey.Sequence, current.SampleKey.Sequence, current.GetFloat32(inputs[PreviousWeight].Handle)),
                            CharacterFootLandingPolicySupport.Evidence("prior-current-lock-weight", inputs[CurrentWeight], pair.Key, previous.SampleKey.Sequence, current.SampleKey.Sequence, previous.GetFloat32(inputs[CurrentWeight].Handle))
                        }));
                }
            }
            if (missing.Count != 0)
                return DiagnosticOperatorResult.MissingEvidence(
                    CharacterFootDiagnosticOperatorSupport.MissingSummary(missing));
            return CharacterFootLandingPolicySupport.Result(eligible, findings, "Contact transition");
        }

        static DiagnosticOperatorInputSlot[] Slots() => new[]
        {
            CharacterFootLandingPolicySupport.Slot(Frame, DiagnosticValueKind.UInt64),
            CharacterFootLandingPolicySupport.Slot(Reset, DiagnosticValueKind.UInt64),
            CharacterFootLandingPolicySupport.Slot(ContactEdge, DiagnosticValueKind.UInt32),
            CharacterFootLandingPolicySupport.Slot(PreviousLatestEvent, DiagnosticValueKind.UInt64),
            CharacterFootLandingPolicySupport.Slot(CurrentLatestEvent, DiagnosticValueKind.UInt64),
            CharacterFootLandingPolicySupport.Slot(PreviousReleasedEvent, DiagnosticValueKind.UInt64),
            CharacterFootLandingPolicySupport.Slot(CurrentReleasedEvent, DiagnosticValueKind.UInt64),
            CharacterFootLandingPolicySupport.Slot(PreviousCompletedWeightEvent, DiagnosticValueKind.UInt64),
            CharacterFootLandingPolicySupport.Slot(CurrentCompletedWeightEvent, DiagnosticValueKind.UInt64),
            CharacterFootLandingPolicySupport.Slot(ReentryRefreshed, DiagnosticValueKind.Boolean),
            CharacterFootLandingPolicySupport.Slot(ReentryUnavailable, DiagnosticValueKind.Boolean),
            CharacterFootLandingPolicySupport.Slot(PreviousRequested, DiagnosticValueKind.Boolean),
            CharacterFootLandingPolicySupport.Slot(CurrentRequested, DiagnosticValueKind.Boolean),
            CharacterFootLandingPolicySupport.Slot(PreviousEvent, DiagnosticValueKind.UInt64),
            CharacterFootLandingPolicySupport.Slot(CurrentEvent, DiagnosticValueKind.UInt64),
            CharacterFootLandingPolicySupport.Slot(PreviousMode, DiagnosticValueKind.UInt32),
            CharacterFootLandingPolicySupport.Slot(CurrentMode, DiagnosticValueKind.UInt32),
            CharacterFootLandingPolicySupport.Slot(PreviousWeight, DiagnosticValueKind.Float32),
            CharacterFootLandingPolicySupport.Slot(CurrentWeight, DiagnosticValueKind.Float32),
            CharacterFootLandingPolicySupport.Slot(PreviousEdgeSeconds, DiagnosticValueKind.Float32),
            CharacterFootLandingPolicySupport.Slot(CurrentEdgeSeconds, DiagnosticValueKind.Float32),
            CharacterFootLandingPolicySupport.Slot(PreviousAnchorAvailable, DiagnosticValueKind.Boolean),
            CharacterFootLandingPolicySupport.Slot(CurrentAnchorAvailable, DiagnosticValueKind.Boolean),
            CharacterFootLandingPolicySupport.Slot(PreviousAnchorEvent, DiagnosticValueKind.UInt64),
            CharacterFootLandingPolicySupport.Slot(CurrentAnchorEvent, DiagnosticValueKind.UInt64),
            CharacterFootLandingPolicySupport.Slot(PreviousAnchorPoint, DiagnosticValueKind.Vector3),
            CharacterFootLandingPolicySupport.Slot(CurrentAnchorPoint, DiagnosticValueKind.Vector3),
            CharacterFootLandingPolicySupport.Slot(PreviousAnchorNormal, DiagnosticValueKind.Vector3),
            CharacterFootLandingPolicySupport.Slot(CurrentAnchorNormal, DiagnosticValueKind.Vector3)
        };

        static Dictionary<string, DiagnosticBoundInput> Bind(
            DiagnosticOperatorExecutionContext context,
            IEnumerable<DiagnosticOperatorInputSlot> slots) => slots.ToDictionary(
                value => value.Id,
                value => context.Input(value.Id),
                StringComparer.Ordinal);
    }

    internal sealed class CharacterFootFormalGoalWeightPolicyOperator :
        IDiagnosticAnalysisOperator
    {
        const string Frame = "frame-sequence";
        const string Outcome = "resolved-outcome";
        const string Suppress = "suppress-output";
        const string FormalWeight = "formal-foot-placement-weight";
        const string LockWeight = "lock-weight";
        const string MotionPositionWeight = "motion-position-weight";
        const string MotionRotationWeight = "motion-rotation-weight";
        const string ResolvedPositionWeight = "resolved-position-weight";
        const string ResolvedRotationWeight = "resolved-rotation-weight";
        const string GoalPositionWeight = "goal-position-weight";
        const string GoalRotationWeight = "goal-rotation-weight";
        const string ReadyOutcome = "ready-outcome";
        const string Epsilon = "comparison-epsilon";

        public DiagnosticOperatorDescriptor Descriptor { get; } =
            new DiagnosticOperatorDescriptor(
                "foot/formal-goal-weight-policy",
                "landing",
                Slots(),
                new[]
                {
                    CharacterFootLandingPolicySupport.Integer(ReadyOutcome),
                    CharacterFootLandingPolicySupport.Number(Epsilon)
                });

        public DiagnosticOperatorResult Execute(DiagnosticOperatorExecutionContext context)
        {
            Dictionary<string, DiagnosticBoundInput> inputs = Bind(context, Slots());
            var missing = new HashSet<string>(StringComparer.Ordinal);
            Dictionary<string, List<DiagnosticDatasetRow>> rows =
                CharacterFootLandingPolicySupport.Rows(
                    context,
                    inputs.Values.ToArray(),
                    missing);
            if (missing.Count != 0)
                return DiagnosticOperatorResult.MissingEvidence(
                    CharacterFootDiagnosticOperatorSupport.MissingSummary(missing));
            uint readyOutcome = CharacterFootDiagnosticOperatorSupport.RequireUInt32(
                context,
                ReadyOutcome);
            double epsilon = CharacterFootDiagnosticOperatorSupport.RequireNumber(context, Epsilon);
            var findings = new List<DiagnosticFinding>();
            int eligible = 0;
            foreach (KeyValuePair<string, List<DiagnosticDatasetRow>> pair in rows)
            {
                for (int i = 0; i < pair.Value.Count; i++)
                {
                    DiagnosticDatasetRow row = pair.Value[i];
                    eligible++;
                    bool ready = row.GetUInt32(inputs[Outcome].Handle) == readyOutcome &&
                        !row.GetBoolean(inputs[Suppress].Handle);
                    double formal = row.GetFloat32(inputs[FormalWeight].Handle);
                    double expectedPosition = ready ? formal : 0d;
                    double resolvedPosition = row.GetFloat32(
                        inputs[ResolvedPositionWeight].Handle);
                    double goalPosition = row.GetFloat32(inputs[GoalPositionWeight].Handle);
                    double resolvedRotation = row.GetFloat32(
                        inputs[ResolvedRotationWeight].Handle);
                    double goalRotation = row.GetFloat32(inputs[GoalRotationWeight].Handle);
                    bool consistent =
                        Math.Abs(resolvedPosition - expectedPosition) <= epsilon &&
                        Math.Abs(goalPosition - expectedPosition) <= epsilon &&
                        Math.Abs(goalRotation - resolvedRotation) <= epsilon &&
                        (!ready || Math.Abs(
                            row.GetFloat32(inputs[MotionPositionWeight].Handle) -
                            resolvedPosition) <= epsilon);
                    if (consistent)
                        continue;
                    findings.Add(CharacterFootLandingPolicySupport.Finding(
                        context,
                        "formal-goal-weight-policy",
                        DiagnosticSeverity.Error,
                        pair.Key,
                        row,
                        row,
                        inputs[Frame],
                        $"Formal weight policy expected position weight {CharacterFootDiagnosticOperatorSupport.Format(expectedPosition)}, resolved={CharacterFootDiagnosticOperatorSupport.Format(resolvedPosition)}, goal={CharacterFootDiagnosticOperatorSupport.Format(goalPosition)}.",
                        new[]
                        {
                            CharacterFootLandingPolicySupport.Evidence("formal-weight", inputs[FormalWeight], pair.Key, row.SampleKey.Sequence, row.SampleKey.Sequence, formal),
                            CharacterFootLandingPolicySupport.Evidence("lock-weight", inputs[LockWeight], pair.Key, row.SampleKey.Sequence, row.SampleKey.Sequence, row.GetFloat32(inputs[LockWeight].Handle)),
                            CharacterFootLandingPolicySupport.Evidence("resolved-position-weight", inputs[ResolvedPositionWeight], pair.Key, row.SampleKey.Sequence, row.SampleKey.Sequence, resolvedPosition),
                            CharacterFootLandingPolicySupport.Evidence("goal-position-weight", inputs[GoalPositionWeight], pair.Key, row.SampleKey.Sequence, row.SampleKey.Sequence, goalPosition),
                            CharacterFootLandingPolicySupport.Evidence("resolved-rotation-weight", inputs[ResolvedRotationWeight], pair.Key, row.SampleKey.Sequence, row.SampleKey.Sequence, resolvedRotation),
                            CharacterFootLandingPolicySupport.Evidence("goal-rotation-weight", inputs[GoalRotationWeight], pair.Key, row.SampleKey.Sequence, row.SampleKey.Sequence, goalRotation),
                            CharacterFootLandingPolicySupport.Evidence("motion-rotation-weight", inputs[MotionRotationWeight], pair.Key, row.SampleKey.Sequence, row.SampleKey.Sequence, row.GetFloat32(inputs[MotionRotationWeight].Handle))
                        }));
                }
            }
            return CharacterFootLandingPolicySupport.Result(eligible, findings, "Formal Goal weight policy");
        }

        static DiagnosticOperatorInputSlot[] Slots() => new[]
        {
            CharacterFootLandingPolicySupport.Slot(Frame, DiagnosticValueKind.UInt64),
            CharacterFootLandingPolicySupport.Slot(Outcome, DiagnosticValueKind.UInt32),
            CharacterFootLandingPolicySupport.Slot(Suppress, DiagnosticValueKind.Boolean),
            CharacterFootLandingPolicySupport.Slot(FormalWeight, DiagnosticValueKind.Float32),
            CharacterFootLandingPolicySupport.Slot(LockWeight, DiagnosticValueKind.Float32),
            CharacterFootLandingPolicySupport.Slot(MotionPositionWeight, DiagnosticValueKind.Float32),
            CharacterFootLandingPolicySupport.Slot(MotionRotationWeight, DiagnosticValueKind.Float32),
            CharacterFootLandingPolicySupport.Slot(ResolvedPositionWeight, DiagnosticValueKind.Float32),
            CharacterFootLandingPolicySupport.Slot(ResolvedRotationWeight, DiagnosticValueKind.Float32),
            CharacterFootLandingPolicySupport.Slot(GoalPositionWeight, DiagnosticValueKind.Float32),
            CharacterFootLandingPolicySupport.Slot(GoalRotationWeight, DiagnosticValueKind.Float32)
        };

        static Dictionary<string, DiagnosticBoundInput> Bind(
            DiagnosticOperatorExecutionContext context,
            IEnumerable<DiagnosticOperatorInputSlot> slots) => slots.ToDictionary(
                value => value.Id,
                value => context.Input(value.Id),
                StringComparer.Ordinal);
    }

    internal sealed class CharacterFootContactReentryOutputGeometryOperator :
        IDiagnosticAnalysisOperator
    {
        const string Frame = "frame-sequence";
        const string Refreshed = "same-event-reentry-refreshed";
        const string PreviousResponseAvailable = "previous-response-available";
        const string PlantEvaluated = "plant-interpolation-evaluated";
        const string ResponseEvaluated = "correction-response-evaluated";
        const string Outcome = "resolved-outcome";
        const string SelectedTarget = "plant-selected-target";
        const string ResidualBefore = "residual-before-decay";
        const string ResidualAfter = "residual-after-decay";
        const string PreviousResponse = "previous-response-output";
        const string Desired = "desired-output";
        const string Response = "response-output";
        const string FinalSole = "final-sole";
        const string ReadyOutcome = "ready-outcome";

        public DiagnosticOperatorDescriptor Descriptor { get; } =
            new DiagnosticOperatorDescriptor(
                "foot/contact-reentry-output-geometry",
                "landing",
                Slots(),
                new[] { CharacterFootLandingPolicySupport.Integer(ReadyOutcome) });

        public DiagnosticOperatorResult Execute(DiagnosticOperatorExecutionContext context)
        {
            Dictionary<string, DiagnosticBoundInput> inputs = Bind(context, Slots());
            var missing = new HashSet<string>(StringComparer.Ordinal);
            Dictionary<string, List<DiagnosticDatasetRow>> rows =
                CharacterFootLandingPolicySupport.Rows(
                    context,
                    new[]
                    {
                        inputs[Frame],
                        inputs[Refreshed],
                        inputs[PreviousResponseAvailable],
                        inputs[PlantEvaluated],
                        inputs[ResponseEvaluated],
                        inputs[Outcome]
                    },
                    missing);
            if (missing.Count != 0)
                return DiagnosticOperatorResult.MissingEvidence(
                    CharacterFootDiagnosticOperatorSupport.MissingSummary(missing));
            uint ready = CharacterFootDiagnosticOperatorSupport.RequireUInt32(context, ReadyOutcome);
            var evidence = new List<DiagnosticEvidence>();
            int eligible = 0;
            foreach (KeyValuePair<string, List<DiagnosticDatasetRow>> pair in rows)
            {
                for (int i = 0; i < pair.Value.Count; i++)
                {
                    DiagnosticDatasetRow row = pair.Value[i];
                    if (!row.GetBoolean(inputs[Refreshed].Handle) ||
                        !row.GetBoolean(inputs[PreviousResponseAvailable].Handle) ||
                        !row.GetBoolean(inputs[PlantEvaluated].Handle) ||
                        !row.GetBoolean(inputs[ResponseEvaluated].Handle) ||
                        row.GetUInt32(inputs[Outcome].Handle) != ready)
                    {
                        continue;
                    }
                    if (!CharacterFootLandingPolicySupport.Require(
                            row,
                            missing,
                            inputs[SelectedTarget],
                            inputs[ResidualBefore],
                            inputs[ResidualAfter],
                            inputs[PreviousResponse],
                            inputs[Desired],
                            inputs[Response],
                            inputs[FinalSole]))
                    {
                        continue;
                    }
                    eligible++;
                    DiagnosticVector3 selected = row.GetVector3(inputs[SelectedTarget].Handle);
                    DiagnosticVector3 before = row.GetVector3(inputs[ResidualBefore].Handle);
                    var captured = new DiagnosticVector3(
                        selected.X + before.X,
                        selected.Y + before.Y,
                        selected.Z + before.Z);
                    DiagnosticVector3 after = row.GetVector3(inputs[ResidualAfter].Handle);
                    ulong sequence = row.SampleKey.Sequence;
                    evidence.Add(CharacterFootLandingPolicySupport.Evidence(
                        "captured-target-to-previous-response-meters",
                        inputs[PreviousResponse],
                        pair.Key,
                        sequence,
                        sequence,
                        CharacterFootLandingPolicySupport.Distance(
                            captured,
                            row.GetVector3(inputs[PreviousResponse].Handle))));
                    evidence.Add(CharacterFootLandingPolicySupport.Evidence(
                        "residual-decay-step-meters",
                        inputs[ResidualAfter],
                        pair.Key,
                        sequence,
                        sequence,
                        CharacterFootLandingPolicySupport.Distance(before, after)));
                    evidence.Add(CharacterFootLandingPolicySupport.Evidence(
                        "captured-target-to-desired-meters",
                        inputs[Desired],
                        pair.Key,
                        sequence,
                        sequence,
                        CharacterFootLandingPolicySupport.Distance(
                            captured,
                            row.GetVector3(inputs[Desired].Handle))));
                    evidence.Add(CharacterFootLandingPolicySupport.Evidence(
                        "desired-to-response-meters",
                        inputs[Response],
                        pair.Key,
                        sequence,
                        sequence,
                        CharacterFootLandingPolicySupport.Distance(
                            row.GetVector3(inputs[Desired].Handle),
                            row.GetVector3(inputs[Response].Handle))));
                    evidence.Add(CharacterFootLandingPolicySupport.Evidence(
                        "response-to-final-sole-meters",
                        inputs[FinalSole],
                        pair.Key,
                        sequence,
                        sequence,
                        CharacterFootLandingPolicySupport.Distance(
                            row.GetVector3(inputs[Response].Handle),
                            row.GetVector3(inputs[FinalSole].Handle))));
                }
            }
            if (missing.Count != 0)
                return DiagnosticOperatorResult.MissingEvidence(
                    CharacterFootDiagnosticOperatorSupport.MissingSummary(missing));
            if (eligible == 0)
                return DiagnosticOperatorResult.NotApplicable(
                    "No complete same-event Contact reentry geometry observation was available.");
            return new DiagnosticOperatorResult(
                DiagnosticRuleState.Passed,
                $"Recorded {eligible} same-event Contact reentry geometry observations.",
                null,
                evidence,
                null);
        }

        static DiagnosticOperatorInputSlot[] Slots() => new[]
        {
            CharacterFootLandingPolicySupport.Slot(Frame, DiagnosticValueKind.UInt64),
            CharacterFootLandingPolicySupport.Slot(Refreshed, DiagnosticValueKind.Boolean),
            CharacterFootLandingPolicySupport.Slot(PreviousResponseAvailable, DiagnosticValueKind.Boolean),
            CharacterFootLandingPolicySupport.Slot(PlantEvaluated, DiagnosticValueKind.Boolean),
            CharacterFootLandingPolicySupport.Slot(ResponseEvaluated, DiagnosticValueKind.Boolean),
            CharacterFootLandingPolicySupport.Slot(Outcome, DiagnosticValueKind.UInt32),
            CharacterFootLandingPolicySupport.Slot(SelectedTarget, DiagnosticValueKind.Vector3),
            CharacterFootLandingPolicySupport.Slot(ResidualBefore, DiagnosticValueKind.Vector3),
            CharacterFootLandingPolicySupport.Slot(ResidualAfter, DiagnosticValueKind.Vector3),
            CharacterFootLandingPolicySupport.Slot(PreviousResponse, DiagnosticValueKind.Vector3),
            CharacterFootLandingPolicySupport.Slot(Desired, DiagnosticValueKind.Vector3),
            CharacterFootLandingPolicySupport.Slot(Response, DiagnosticValueKind.Vector3),
            CharacterFootLandingPolicySupport.Slot(FinalSole, DiagnosticValueKind.Vector3)
        };

        static Dictionary<string, DiagnosticBoundInput> Bind(
            DiagnosticOperatorExecutionContext context,
            IEnumerable<DiagnosticOperatorInputSlot> slots) => slots.ToDictionary(
                value => value.Id,
                value => context.Input(value.Id),
                StringComparer.Ordinal);
    }
}
