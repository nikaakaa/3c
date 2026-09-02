using System;
using System.Collections.Generic;
using System.Globalization;
using KK.GeneratedDiagnosticSampling;
using KK.GeneratedDiagnosticSampling.Host;

namespace ThirdPersonCharacter.Pipeline.Diagnostics.FootAnalysis.Editor
{
    internal sealed class CharacterFootLandingStateConsistencyOperator :
        IDiagnosticAnalysisOperator
    {
        const string LandingState = "landing-state";
        const string LockedState = "locked-state";
        const string ReleasingState = "releasing-state";
        const string FormalUnlockedMode = "formal-unlocked-mode";
        const string ExitJump = "exit-jump-meters";
        const string GeometryEpsilon = "runtime-geometry-epsilon-meters";

        static readonly string[] s_Targets =
        {
            "missed-landing-entry",
            "early-landing-entry",
            "landing-without-contact-plane",
            "landing-not-closing",
            "landing-wrong-exit",
            "landing-exit-jump",
            "landing-persists-after-formal-unlock"
        };

        public DiagnosticOperatorDescriptor Descriptor { get; } =
            new DiagnosticOperatorDescriptor(
                "foot/landing-state-consistency",
                "landing",
                Inputs.Slots(),
                new[]
                {
                    Integer(LandingState),
                    Integer(LockedState),
                    Integer(ReleasingState),
                    Integer(FormalUnlockedMode),
                    Number(ExitJump),
                    Number(GeometryEpsilon)
                });

        public DiagnosticOperatorResult Execute(
            DiagnosticOperatorExecutionContext context)
        {
            var inputs = new Inputs(context);
            uint landingState = CharacterFootDiagnosticOperatorSupport.RequireUInt32(
                context,
                LandingState);
            uint lockedState = CharacterFootDiagnosticOperatorSupport.RequireUInt32(
                context,
                LockedState);
            uint releasingState = CharacterFootDiagnosticOperatorSupport.RequireUInt32(
                context,
                ReleasingState);
            uint formalUnlockedMode =
                CharacterFootDiagnosticOperatorSupport.RequireUInt32(
                    context,
                    FormalUnlockedMode);
            double exitJump = CharacterFootDiagnosticOperatorSupport.RequireNumber(
                context,
                ExitJump);
            double geometryEpsilon =
                CharacterFootDiagnosticOperatorSupport.RequireNumber(
                    context,
                    GeometryEpsilon);
            var rowsByDimension = new Dictionary<string, List<DiagnosticDatasetRow>>(
                StringComparer.Ordinal);
            var missing = new HashSet<string>(StringComparer.Ordinal);
            DiagnosticDatasetCursor cursor = context.CreateCursor(0);
            while (cursor.MoveNext())
            {
                DiagnosticDatasetRow row = cursor.Current;
                string dimension = row.SampleKey.DimensionId;
                if (!context.IncludesDimension(dimension) ||
                    !context.MatchesFilters(row))
                {
                    continue;
                }
                if (!Require(row, inputs.Fundamental, missing))
                    continue;
                if (!rowsByDimension.TryGetValue(
                        dimension,
                        out List<DiagnosticDatasetRow> rows))
                {
                    rows = new List<DiagnosticDatasetRow>();
                    rowsByDimension.Add(dimension, rows);
                }
                rows.Add(row);
            }
            if (missing.Count != 0)
                return DiagnosticOperatorResult.MissingEvidence(
                    CharacterFootDiagnosticOperatorSupport.MissingSummary(missing));
            var counters = new Counters(s_Targets);
            var findings = new List<DiagnosticFinding>();
            foreach (KeyValuePair<string, List<DiagnosticDatasetRow>> pair in rowsByDimension)
            {
                AnalyzeDimension(
                    context,
                    inputs,
                    pair.Key,
                    pair.Value,
                    landingState,
                    lockedState,
                    releasingState,
                    formalUnlockedMode,
                    exitJump,
                    geometryEpsilon,
                    counters,
                    findings,
                    missing);
            }
            if (missing.Count != 0)
                return DiagnosticOperatorResult.MissingEvidence(
                    CharacterFootDiagnosticOperatorSupport.MissingSummary(missing));
            if (counters.TotalEligible == 0)
                return DiagnosticOperatorResult.NotApplicable(
                    "No Formal Landing boundary or Runtime Landing span was observed.");
            findings.Sort((left, right) =>
            {
                int sequence = left.SequenceStart.CompareTo(right.SequenceStart);
                return sequence != 0
                    ? sequence
                    : string.CompareOrdinal(left.Id, right.Id);
            });
            string summary =
                $"{counters.TotalMatched} of {counters.TotalEligible} Landing State rule windows failed.";
            return new DiagnosticOperatorResult(
                findings.Count == 0
                    ? DiagnosticRuleState.Passed
                    : DiagnosticRuleState.Failed,
                summary,
                null,
                counters.Evidence(inputs.Frame),
                findings);
        }

        static void AnalyzeDimension(
            DiagnosticOperatorExecutionContext context,
            Inputs inputs,
            string dimension,
            IReadOnlyList<DiagnosticDatasetRow> rows,
            uint landingState,
            uint lockedState,
            uint releasingState,
            uint formalUnlockedMode,
            double exitJump,
            double geometryEpsilon,
            Counters counters,
            ICollection<DiagnosticFinding> findings,
            ISet<string> missing)
        {
            for (int i = 1; i < rows.Count; i++)
            {
                DiagnosticDatasetRow previous = rows[i - 1];
                DiagnosticDatasetRow current = rows[i];
                if (!Continuous(previous, current, inputs) ||
                    !FormalLandingBoundary(
                        previous,
                        current,
                        inputs,
                        formalUnlockedMode))
                {
                    continue;
                }
                counters.Eligible(s_Targets[0], dimension, previous, current);
                uint state = current.GetUInt32(inputs.State.Handle);
                if (state == landingState || state == lockedState)
                    continue;
                counters.Matched(s_Targets[0], dimension);
                findings.Add(Finding(
                    context.RuleId,
                    s_Targets[0],
                    DiagnosticSeverity.Warning,
                    inputs.Frame,
                    inputs.State,
                    previous,
                    current,
                    "Formal Landing boundary did not enter Runtime Landing or Locked.",
                    state.ToString(CultureInfo.InvariantCulture)));
            }
            int index = 0;
            while (index < rows.Count)
            {
                if (rows[index].GetUInt32(inputs.State.Handle) != landingState)
                {
                    index++;
                    continue;
                }
                int start = index;
                ulong eventIdentity = rows[index].GetUInt64(inputs.Event.Handle);
                while (index + 1 < rows.Count &&
                    Continuous(rows[index], rows[index + 1], inputs) &&
                    rows[index + 1].GetUInt32(inputs.State.Handle) == landingState &&
                    rows[index + 1].GetUInt64(inputs.Event.Handle) == eventIdentity)
                {
                    index++;
                }
                int end = index;
                DiagnosticDatasetRow first = rows[start];
                DiagnosticDatasetRow last = rows[end];
                bool hasEntry = start > 0 &&
                    Continuous(rows[start - 1], first, inputs);
                bool hasExit = end + 1 < rows.Count &&
                    Continuous(last, rows[end + 1], inputs);
                DiagnosticDatasetRow previous = hasEntry ? rows[start - 1] : first;
                DiagnosticDatasetRow next = hasExit ? rows[end + 1] : last;
                for (int target = 1; target < s_Targets.Length; target++)
                    counters.Eligible(s_Targets[target], dimension, first, last);
                bool followedBoundary = hasEntry &&
                    FormalLandingBoundary(
                        previous,
                        first,
                        inputs,
                        formalUnlockedMode);
                if (!followedBoundary)
                {
                    counters.Matched(s_Targets[1], dimension);
                    findings.Add(Finding(
                        context.RuleId,
                        s_Targets[1],
                        DiagnosticSeverity.Warning,
                        inputs.Frame,
                        inputs.State,
                        first,
                        first,
                        "Runtime Landing began without a Formal Landing boundary.",
                        "false"));
                }
                bool contactPlaneThroughout = true;
                bool formalUnlockedWithin = false;
                bool anchorComplete = true;
                for (int rowIndex = start; rowIndex <= end; rowIndex++)
                {
                    DiagnosticDatasetRow row = rows[rowIndex];
                    bool planeAvailable =
                        row.GetBoolean(inputs.ContactPlaneAvailable.Handle);
                    bool anchorAvailable =
                        row.GetBoolean(inputs.AnchorAvailable.Handle);
                    bool anchorEventAvailable =
                        row.IsAvailable(inputs.AnchorEvent.Handle);
                    if (planeAvailable &&
                        anchorAvailable &&
                        !anchorEventAvailable)
                    {
                        missing.Add(
                            CharacterFootDiagnosticOperatorSupport.Identity(
                                inputs.AnchorEvent));
                    }
                    contactPlaneThroughout &=
                        planeAvailable &&
                        anchorAvailable &&
                        anchorEventAvailable &&
                        row.GetUInt64(inputs.AnchorEvent.Handle) ==
                        row.GetUInt64(inputs.Event.Handle);
                    formalUnlockedWithin |=
                        row.GetUInt32(inputs.FormalLockMode.Handle) ==
                        formalUnlockedMode;
                    if (!row.IsAvailable(inputs.AnchorPoint.Handle))
                    {
                        missing.Add(
                            CharacterFootDiagnosticOperatorSupport.Identity(
                                inputs.AnchorPoint));
                        anchorComplete = false;
                    }
                }
                if (!contactPlaneThroughout)
                {
                    counters.Matched(s_Targets[2], dimension);
                    findings.Add(Finding(
                        context.RuleId,
                        s_Targets[2],
                        DiagnosticSeverity.Warning,
                        inputs.Frame,
                        inputs.ContactPlaneAvailable,
                        first,
                        last,
                        "Runtime Landing span did not retain a same-event Contact Plane.",
                        "false"));
                }
                if (anchorComplete)
                {
                    double entryDistance =
                        CharacterFootDiagnosticOperatorSupport.Distance(
                            first.GetVector3(inputs.CorrectedSole.Handle),
                            first.GetVector3(inputs.AnchorPoint.Handle));
                    double exitDistance =
                        CharacterFootDiagnosticOperatorSupport.Distance(
                            last.GetVector3(inputs.CorrectedSole.Handle),
                            last.GetVector3(inputs.AnchorPoint.Handle));
                    double closure = entryDistance - exitDistance;
                    if (end - start + 1 > 1 && closure <= 0d)
                    {
                        counters.Matched(s_Targets[3], dimension);
                        findings.Add(Finding(
                            context.RuleId,
                            s_Targets[3],
                            DiagnosticSeverity.Warning,
                            inputs.Frame,
                            inputs.CorrectedSole,
                            first,
                            last,
                            $"Landing did not close toward the Anchor; closure was {CharacterFootDiagnosticOperatorSupport.Format(closure)} m.",
                            CharacterFootDiagnosticOperatorSupport.Format(closure)));
                    }
                }
                uint exitState = next.GetUInt32(inputs.State.Handle);
                if (hasExit && exitState != lockedState && exitState != releasingState)
                {
                    counters.Matched(s_Targets[4], dimension);
                    findings.Add(Finding(
                        context.RuleId,
                        s_Targets[4],
                        DiagnosticSeverity.Warning,
                        inputs.Frame,
                        inputs.State,
                        last,
                        next,
                        "Continuous Runtime Landing exit did not enter Locked or Releasing.",
                        exitState.ToString(CultureInfo.InvariantCulture)));
                }
                if (hasExit)
                {
                    double additional = AdditionalOutputStep(
                        last.GetVector3(inputs.CorrectedSole.Handle),
                        next.GetVector3(inputs.CorrectedSole.Handle),
                        last.GetVector3(inputs.OriginalSole.Handle),
                        next.GetVector3(inputs.OriginalSole.Handle),
                        geometryEpsilon);
                    if (additional > exitJump)
                    {
                        counters.Matched(s_Targets[5], dimension);
                        findings.Add(Finding(
                            context.RuleId,
                            s_Targets[5],
                            CharacterFootDiagnosticOperatorSupport.Severity(additional),
                            inputs.Frame,
                            inputs.CorrectedSole,
                            last,
                            next,
                            $"Landing exit added {CharacterFootDiagnosticOperatorSupport.Format(additional)} m beyond normal animated Sole movement.",
                            CharacterFootDiagnosticOperatorSupport.Format(additional)));
                    }
                }
                if (formalUnlockedWithin)
                {
                    counters.Matched(s_Targets[6], dimension);
                    findings.Add(Finding(
                        context.RuleId,
                        s_Targets[6],
                        DiagnosticSeverity.Warning,
                        inputs.Frame,
                        inputs.FormalLockMode,
                        first,
                        last,
                        "Runtime Landing persisted after the Formal input became Unlocked.",
                        "true"));
                }
                index++;
            }
        }

        static bool Require(
            in DiagnosticDatasetRow row,
            IReadOnlyList<DiagnosticBoundInput> required,
            ISet<string> missing)
        {
            bool complete = true;
            for (int i = 0; i < required.Count; i++)
            {
                if (row.IsAvailable(required[i].Handle))
                    continue;
                missing.Add(CharacterFootDiagnosticOperatorSupport.Identity(required[i]));
                complete = false;
            }
            return complete;
        }

        static bool Continuous(
            in DiagnosticDatasetRow previous,
            in DiagnosticDatasetRow current,
            Inputs inputs) =>
            current.GetUInt64(inputs.Frame.Handle) ==
            previous.GetUInt64(inputs.Frame.Handle) + 1 &&
            current.GetUInt64(inputs.Reset.Handle) ==
            previous.GetUInt64(inputs.Reset.Handle);

        static bool FormalLandingBoundary(
            in DiagnosticDatasetRow previous,
            in DiagnosticDatasetRow current,
            Inputs inputs,
            uint formalUnlockedMode) =>
            previous.GetUInt32(inputs.FormalLockMode.Handle) == formalUnlockedMode &&
            current.GetUInt32(inputs.FormalLockMode.Handle) != formalUnlockedMode;

        static double AdditionalOutputStep(
            in DiagnosticVector3 previousCorrected,
            in DiagnosticVector3 currentCorrected,
            in DiagnosticVector3 previousAnimated,
            in DiagnosticVector3 currentAnimated,
            double epsilon)
        {
            double correctedX = currentCorrected.X - previousCorrected.X;
            double correctedY = currentCorrected.Y - previousCorrected.Y;
            double correctedZ = currentCorrected.Z - previousCorrected.Z;
            double animatedX = currentAnimated.X - previousAnimated.X;
            double animatedY = currentAnimated.Y - previousAnimated.Y;
            double animatedZ = currentAnimated.Z - previousAnimated.Z;
            double animatedLengthSquared =
                animatedX * animatedX +
                animatedY * animatedY +
                animatedZ * animatedZ;
            double blend = animatedLengthSquared > epsilon * epsilon
                ? Math.Max(0d, Math.Min(
                    1d,
                    (correctedX * animatedX +
                     correctedY * animatedY +
                     correctedZ * animatedZ) /
                    animatedLengthSquared))
                : 0d;
            double additionalX = correctedX - animatedX * blend;
            double additionalY = correctedY - animatedY * blend;
            double additionalZ = correctedZ - animatedZ * blend;
            return Math.Sqrt(
                additionalX * additionalX +
                additionalY * additionalY +
                additionalZ * additionalZ);
        }

        static DiagnosticFinding Finding(
            string ruleId,
            string target,
            DiagnosticSeverity severity,
            in DiagnosticBoundInput frameInput,
            in DiagnosticBoundInput evidenceInput,
            in DiagnosticDatasetRow start,
            in DiagnosticDatasetRow end,
            string message,
            string value) =>
            new DiagnosticFinding(
                CharacterFootDiagnosticOperatorSupport.FindingId(
                    ruleId + "-" + target,
                    end.SampleKey.Sequence),
                severity,
                end.SampleKey.DimensionId,
                start.SampleKey.Sequence,
                end.SampleKey.Sequence,
                message,
                new[]
                {
                    CharacterFootDiagnosticOperatorSupport.Evidence(
                        target,
                        evidenceInput,
                        end.SampleKey.DimensionId,
                        start.SampleKey.Sequence,
                        end.SampleKey.Sequence,
                        value)
                },
                CharacterFootDiagnosticOperatorSupport.FrameRange(
                    frameInput,
                    start.GetUInt64(frameInput.Handle),
                    end.GetUInt64(frameInput.Handle)));

        static DiagnosticOperatorParameter Number(string id) =>
            new DiagnosticOperatorParameter(
                id,
                DiagnosticOperatorParameterKind.Number,
                true,
                0d);

        static DiagnosticOperatorParameter Integer(string id) =>
            new DiagnosticOperatorParameter(
                id,
                DiagnosticOperatorParameterKind.Integer,
                true,
                0d);

        sealed class Inputs
        {
            internal Inputs(DiagnosticOperatorExecutionContext context)
            {
                Frame = context.Input("frame-sequence");
                Reset = context.Input("reset-sequence");
                State = context.Input("constraint-state");
                Event = context.Input("landing-event-identity");
                FormalLockMode = context.Input("formal-lock-mode");
                ContactPlaneAvailable = context.Input("contact-plane-available");
                AnchorAvailable = context.Input("anchor-available");
                AnchorEvent = context.Input("anchor-event-identity");
                AnchorPoint = context.Input("anchor-point");
                CorrectedSole = context.Input("corrected-sole");
                OriginalSole = context.Input("original-sole");
                Fundamental = new[]
                {
                    Frame, Reset, State, Event, FormalLockMode,
                    ContactPlaneAvailable, AnchorAvailable,
                    CorrectedSole, OriginalSole
                };
            }

            internal DiagnosticBoundInput Frame { get; }
            internal DiagnosticBoundInput Reset { get; }
            internal DiagnosticBoundInput State { get; }
            internal DiagnosticBoundInput Event { get; }
            internal DiagnosticBoundInput FormalLockMode { get; }
            internal DiagnosticBoundInput ContactPlaneAvailable { get; }
            internal DiagnosticBoundInput AnchorAvailable { get; }
            internal DiagnosticBoundInput AnchorEvent { get; }
            internal DiagnosticBoundInput AnchorPoint { get; }
            internal DiagnosticBoundInput CorrectedSole { get; }
            internal DiagnosticBoundInput OriginalSole { get; }
            internal IReadOnlyList<DiagnosticBoundInput> Fundamental { get; }

            internal static DiagnosticOperatorInputSlot[] Slots() => new[]
            {
                Slot("frame-sequence", DiagnosticValueKind.UInt64),
                Slot("reset-sequence", DiagnosticValueKind.UInt64),
                Slot("constraint-state", DiagnosticValueKind.UInt32),
                Slot("landing-event-identity", DiagnosticValueKind.UInt64),
                Slot("formal-lock-mode", DiagnosticValueKind.UInt32),
                Slot("contact-plane-available", DiagnosticValueKind.Boolean),
                Slot("anchor-available", DiagnosticValueKind.Boolean),
                Slot("anchor-event-identity", DiagnosticValueKind.UInt64),
                Slot("anchor-point", DiagnosticValueKind.Vector3),
                Slot("corrected-sole", DiagnosticValueKind.Vector3),
                Slot("original-sole", DiagnosticValueKind.Vector3)
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

        sealed class Counter
        {
            internal Counter(
                string target,
                string dimension,
                in DiagnosticDatasetRow start,
                in DiagnosticDatasetRow end)
            {
                Target = target;
                Dimension = dimension;
                SequenceStart = start.SampleKey.Sequence;
                SequenceEnd = end.SampleKey.Sequence;
            }

            internal string Target { get; }
            internal string Dimension { get; }
            internal ulong SequenceStart { get; }
            internal ulong SequenceEnd { get; set; }
            internal int Eligible { get; set; }
            internal int Matched { get; set; }
        }

        sealed class Counters
        {
            readonly HashSet<string> m_Targets;
            readonly Dictionary<string, Counter> m_Counters =
                new Dictionary<string, Counter>(StringComparer.Ordinal);

            internal Counters(IEnumerable<string> targets)
            {
                m_Targets = new HashSet<string>(targets, StringComparer.Ordinal);
            }

            internal int TotalEligible { get; private set; }
            internal int TotalMatched { get; private set; }

            internal void Eligible(
                string target,
                string dimension,
                in DiagnosticDatasetRow start,
                in DiagnosticDatasetRow end)
            {
                if (!m_Targets.Contains(target))
                    throw new InvalidOperationException("Landing State target is invalid.");
                string key = target + "|" + dimension;
                if (!m_Counters.TryGetValue(key, out Counter counter))
                {
                    counter = new Counter(target, dimension, start, end);
                    m_Counters.Add(key, counter);
                }
                counter.Eligible++;
                counter.SequenceEnd = end.SampleKey.Sequence;
                TotalEligible++;
            }

            internal void Matched(string target, string dimension)
            {
                Counter counter = m_Counters[target + "|" + dimension];
                counter.Matched++;
                TotalMatched++;
            }

            internal IReadOnlyList<DiagnosticEvidence> Evidence(
                in DiagnosticBoundInput frame)
            {
                var result = new List<DiagnosticEvidence>(m_Counters.Count);
                foreach (Counter counter in m_Counters.Values)
                {
                    result.Add(CharacterFootDiagnosticOperatorSupport.Evidence(
                        counter.Target + "-coverage",
                        frame,
                        counter.Dimension,
                        counter.SequenceStart,
                        counter.SequenceEnd,
                        $"eligible={counter.Eligible.ToString(CultureInfo.InvariantCulture)};matched={counter.Matched.ToString(CultureInfo.InvariantCulture)}"));
                }
                result.Sort((left, right) =>
                {
                    int id = string.CompareOrdinal(left.Id, right.Id);
                    return id != 0
                        ? id
                        : string.CompareOrdinal(left.DimensionId, right.DimensionId);
                });
                return result;
            }
        }
    }
}
