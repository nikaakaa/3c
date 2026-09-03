using System;
using System.Collections.Generic;
using System.Globalization;
using KK.GeneratedDiagnosticSampling;
using KK.GeneratedDiagnosticSampling.Host;

namespace ThirdPersonCharacter.Pipeline.Diagnostics.FootAnalysis.Editor
{
    internal sealed class CharacterFootLandingPathContinuityOperator :
        IDiagnosticAnalysisOperator
    {
        const string PositionNoise = "position-noise-meters";
        const string ClearanceTolerance = "clearance-tolerance-meters";
        const string TimeEpsilon = "time-epsilon-seconds";
        const string AvailabilityReason = "availability-revision-reason";
        const string EventReason = "event-revision-reason";
        const string LandingPointReason = "landing-point-revision-reason";

        static readonly string[] s_Targets =
        {
            "identity-only-residual-rebuild",
            "path-revision-contract-mismatch",
            "releasing-to-swing-envelope-violation",
            "residual-deadline-miss",
            "residual-growth-without-revision"
        };

        public DiagnosticOperatorDescriptor Descriptor { get; } =
            new DiagnosticOperatorDescriptor(
                "foot/landing-path-continuity",
                "motion",
                PathInputs.Slots(),
                new[]
                {
                    Number(PositionNoise),
                    Number(ClearanceTolerance),
                    Number(TimeEpsilon),
                    Integer(AvailabilityReason),
                    Integer(EventReason),
                    Integer(LandingPointReason)
                });

        public DiagnosticOperatorResult Execute(
            DiagnosticOperatorExecutionContext context)
        {
            var inputs = new PathInputs(context);
            double positionNoise = CharacterFootDiagnosticOperatorSupport.RequireNumber(
                context,
                PositionNoise);
            double clearanceTolerance =
                CharacterFootDiagnosticOperatorSupport.RequireNumber(
                    context,
                    ClearanceTolerance);
            double timeEpsilon = CharacterFootDiagnosticOperatorSupport.RequireNumber(
                context,
                TimeEpsilon);
            uint availabilityReason =
                CharacterFootDiagnosticOperatorSupport.RequireUInt32(
                    context,
                    AvailabilityReason);
            uint eventReason = CharacterFootDiagnosticOperatorSupport.RequireUInt32(
                context,
                EventReason);
            uint landingPointReason =
                CharacterFootDiagnosticOperatorSupport.RequireUInt32(
                    context,
                    LandingPointReason);
            var counters = new PathCounters(s_Targets);
            var findings = new List<DiagnosticFinding>();
            var missing = new HashSet<string>(StringComparer.Ordinal);
            var previousByDimension = new Dictionary<string, DiagnosticDatasetRow>(
                StringComparer.Ordinal);
            DiagnosticDatasetCursor cursor = context.CreateCursor(0);
            while (cursor.MoveNext())
            {
                DiagnosticDatasetRow current = cursor.Current;
                string dimension = current.SampleKey.DimensionId;
                if (!context.IncludesDimension(dimension) ||
                    !context.MatchesFilters(current))
                {
                    previousByDimension.Remove(dimension);
                    continue;
                }
                if (!RequireCurrent(current, inputs, missing))
                {
                    previousByDimension.Remove(dimension);
                    continue;
                }
                bool hasPrevious = previousByDimension.TryGetValue(
                    dimension,
                    out DiagnosticDatasetRow previous);
                bool continuous = hasPrevious &&
                    previous.IsAvailable(inputs.Frame.Handle) &&
                    previous.IsAvailable(inputs.Reset.Handle) &&
                    current.GetUInt64(inputs.Frame.Handle) ==
                    previous.GetUInt64(inputs.Frame.Handle) + 1 &&
                    current.GetUInt64(inputs.Reset.Handle) ==
                    previous.GetUInt64(inputs.Reset.Handle);
                bool evaluated = current.GetBoolean(inputs.Evaluated.Handle);
                bool rebuilt = current.GetBoolean(inputs.ResidualRebuilt.Handle);
                bool availabilityChanged =
                    current.GetBoolean(inputs.AvailableBefore.Handle) !=
                    current.GetBoolean(inputs.AvailableAfter.Handle);
                bool comparable =
                    current.GetBoolean(inputs.AvailableBefore.Handle) &&
                    current.GetBoolean(inputs.AvailableAfter.Handle);
                bool eventChanged = comparable &&
                    current.GetUInt64(inputs.PreviousEvent.Handle) !=
                    current.GetUInt64(inputs.CurrentEvent.Handle);
                bool landingPointChanged = comparable &&
                    current.GetFloat32(inputs.LandingPointDelta.Handle) >
                    current.GetFloat32(inputs.RevisionDistance.Handle);
                bool revisionExpected =
                    availabilityChanged || eventChanged || landingPointChanged;
                uint reason = current.GetUInt32(inputs.RevisionReason.Handle);
                bool reasonAvailability = (reason & availabilityReason) != 0;
                bool reasonEvent = (reason & eventReason) != 0;
                bool reasonLandingPoint = (reason & landingPointReason) != 0;
                bool reasonMatches =
                    reasonAvailability == availabilityChanged &&
                    reasonEvent == eventChanged &&
                    reasonLandingPoint == landingPointChanged;
                bool inputIdentityChanged = continuous &&
                    previous.IsAvailable(inputs.InputIdentity.Handle) &&
                    previous.GetUInt64(inputs.InputIdentity.Handle) !=
                    current.GetUInt64(inputs.InputIdentity.Handle);
                bool identityOnly =
                    inputIdentityChanged && evaluated && !revisionExpected;
                if (identityOnly)
                {
                    counters.Eligible(s_Targets[0], dimension, current);
                    if (rebuilt)
                    {
                        counters.Matched(s_Targets[0], dimension);
                        findings.Add(Finding(
                            context.RuleId,
                            s_Targets[0],
                            inputs.Frame,
                            inputs.ResidualRebuilt,
                            current,
                            previous,
                            "Ground Path input identity changed without a geometric revision, but Swing Residual was rebuilt.",
                            "true"));
                    }
                }
                bool revisionRelevant =
                    revisionExpected || rebuilt || !reasonMatches;
                if (revisionRelevant)
                {
                    counters.Eligible(s_Targets[1], dimension, current);
                    if (revisionExpected != rebuilt || !reasonMatches)
                    {
                        counters.Matched(s_Targets[1], dimension);
                        findings.Add(Finding(
                            context.RuleId,
                            s_Targets[1],
                            inputs.Frame,
                            inputs.RevisionReason,
                            current,
                            continuous ? previous : current,
                            "Path revision expectation, residual rebuild, and revision reason are inconsistent.",
                            $"expected={revisionExpected};rebuilt={rebuilt};reason={reason.ToString(CultureInfo.InvariantCulture)}"));
                    }
                }
                if (current.GetBoolean(inputs.ReleasingToSwing.Handle))
                {
                    counters.Eligible(s_Targets[2], dimension, current);
                    double clearance =
                        current.GetFloat32(inputs.ClearanceAfter.Handle);
                    if (clearance < -clearanceTolerance)
                    {
                        counters.Matched(s_Targets[2], dimension);
                        findings.Add(Finding(
                            context.RuleId,
                            s_Targets[2],
                            inputs.Frame,
                            inputs.ClearanceAfter,
                            current,
                            continuous ? previous : current,
                            $"Releasing completed into Swing with safety-floor clearance {CharacterFootDiagnosticOperatorSupport.Format(clearance)} m.",
                            CharacterFootDiagnosticOperatorSupport.Format(clearance)));
                    }
                }
                double delta = Math.Max(
                    current.GetFloat32(inputs.Delta.Handle),
                    0.000001d);
                double timeToLanding =
                    current.GetFloat32(inputs.TimeToLanding.Handle);
                bool deadlineReached = evaluated &&
                    timeToLanding > 0d &&
                    timeToLanding <= delta + timeEpsilon;
                double residualBefore = Magnitude(
                    current.GetVector3(inputs.ResidualBeforeDecay.Handle));
                double residualAfter = Magnitude(
                    current.GetVector3(inputs.ResidualAfterDecay.Handle));
                if (deadlineReached)
                {
                    counters.Eligible(s_Targets[3], dimension, current);
                    double tolerance =
                        current.GetFloat32(inputs.ResidualTolerance.Handle);
                    if (residualAfter > tolerance + clearanceTolerance)
                    {
                        counters.Matched(s_Targets[3], dimension);
                        findings.Add(Finding(
                            context.RuleId,
                            s_Targets[3],
                            inputs.Frame,
                            inputs.ResidualAfterDecay,
                            current,
                            continuous ? previous : current,
                            $"Swing Residual remained {CharacterFootDiagnosticOperatorSupport.Format(residualAfter)} m at the landing deadline; tolerance was {CharacterFootDiagnosticOperatorSupport.Format(tolerance)} m.",
                            CharacterFootDiagnosticOperatorSupport.Format(residualAfter)));
                    }
                }
                if (evaluated && !rebuilt)
                {
                    counters.Eligible(s_Targets[4], dimension, current);
                    if (residualAfter > residualBefore + positionNoise)
                    {
                        counters.Matched(s_Targets[4], dimension);
                        findings.Add(Finding(
                            context.RuleId,
                            s_Targets[4],
                            inputs.Frame,
                            inputs.ResidualAfterDecay,
                            current,
                            continuous ? previous : current,
                            $"Swing Residual grew from {CharacterFootDiagnosticOperatorSupport.Format(residualBefore)} m to {CharacterFootDiagnosticOperatorSupport.Format(residualAfter)} m without a path revision.",
                            CharacterFootDiagnosticOperatorSupport.Format(
                                residualAfter - residualBefore)));
                    }
                }
                previousByDimension[dimension] = current;
            }
            if (missing.Count != 0)
                return DiagnosticOperatorResult.MissingEvidence(
                    CharacterFootDiagnosticOperatorSupport.MissingSummary(missing));
            if (counters.TotalEligible == 0)
                return DiagnosticOperatorResult.NotApplicable(
                    "No Landing Path Continuity rule window was eligible.");
            IReadOnlyList<DiagnosticEvidence> coverage = counters.Evidence(inputs.Frame);
            string summary =
                $"{counters.TotalMatched} of {counters.TotalEligible} Landing Path Continuity rule windows failed.";
            return new DiagnosticOperatorResult(
                findings.Count == 0
                    ? DiagnosticRuleState.Passed
                    : DiagnosticRuleState.Failed,
                summary,
                null,
                coverage,
                findings);
        }

        static bool RequireCurrent(
            in DiagnosticDatasetRow row,
            PathInputs inputs,
            ISet<string> missing)
        {
            DiagnosticBoundInput[] required = inputs.Required;
            bool complete = true;
            for (int i = 0; i < required.Length; i++)
            {
                if (row.IsAvailable(required[i].Handle))
                    continue;
                missing.Add(CharacterFootDiagnosticOperatorSupport.Identity(required[i]));
                complete = false;
            }
            return complete;
        }

        static DiagnosticFinding Finding(
            string ruleId,
            string target,
            in DiagnosticBoundInput frameInput,
            in DiagnosticBoundInput evidenceInput,
            in DiagnosticDatasetRow current,
            in DiagnosticDatasetRow previous,
            string message,
            string value)
        {
            ulong start = previous.SampleKey.Sequence;
            ulong end = current.SampleKey.Sequence;
            ulong frameStart = previous.GetUInt64(frameInput.Handle);
            ulong frameEnd = current.GetUInt64(frameInput.Handle);
            return new DiagnosticFinding(
                CharacterFootDiagnosticOperatorSupport.FindingId(
                    ruleId + "-" + target,
                    current.SampleKey.Sequence),
                DiagnosticSeverity.Warning,
                current.SampleKey.DimensionId,
                start,
                end,
                message,
                new[]
                {
                    CharacterFootDiagnosticOperatorSupport.Evidence(
                        target,
                        evidenceInput,
                        current.SampleKey.DimensionId,
                        start,
                        end,
                        value)
                },
                CharacterFootDiagnosticOperatorSupport.FrameRange(
                    frameInput,
                    frameStart,
                    frameEnd));
        }

        static double Magnitude(in DiagnosticVector3 value) => Math.Sqrt(
            value.X * value.X + value.Y * value.Y + value.Z * value.Z);

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

        sealed class PathInputs
        {
            internal PathInputs(DiagnosticOperatorExecutionContext context)
            {
                Frame = context.Input("frame-sequence");
                Reset = context.Input("reset-sequence");
                Delta = context.Input("delta-seconds");
                InputIdentity = context.Input("ground-path-input-identity");
                Evaluated = context.Input("path-continuity-evaluated");
                ResidualRebuilt = context.Input("path-residual-rebuilt");
                AvailableBefore = context.Input("path-available-before");
                AvailableAfter = context.Input("path-available-after");
                PreviousEvent = context.Input("path-previous-event-identity");
                CurrentEvent = context.Input("path-current-event-identity");
                LandingPointDelta = context.Input("landing-point-delta-meters");
                RevisionDistance = context.Input("path-revision-distance-meters");
                RevisionReason = context.Input("path-revision-reason");
                ResidualBeforeDecay = context.Input("residual-before-decay");
                ResidualAfterDecay = context.Input("residual-after-decay");
                ResidualTolerance = context.Input("residual-tolerance-meters");
                TimeToLanding = context.Input("time-to-landing-seconds");
                ReleasingToSwing = context.Input("releasing-completed-to-swing");
                ClearanceAfter = context.Input("safety-floor-clearance-after-meters");
                Required = new[]
                {
                    Frame, Reset, Delta, InputIdentity, Evaluated, ResidualRebuilt,
                    AvailableBefore, AvailableAfter, PreviousEvent, CurrentEvent,
                    LandingPointDelta, RevisionDistance, RevisionReason,
                    ResidualBeforeDecay, ResidualAfterDecay, ResidualTolerance,
                    TimeToLanding, ReleasingToSwing, ClearanceAfter
                };
            }

            internal DiagnosticBoundInput Frame { get; }
            internal DiagnosticBoundInput Reset { get; }
            internal DiagnosticBoundInput Delta { get; }
            internal DiagnosticBoundInput InputIdentity { get; }
            internal DiagnosticBoundInput Evaluated { get; }
            internal DiagnosticBoundInput ResidualRebuilt { get; }
            internal DiagnosticBoundInput AvailableBefore { get; }
            internal DiagnosticBoundInput AvailableAfter { get; }
            internal DiagnosticBoundInput PreviousEvent { get; }
            internal DiagnosticBoundInput CurrentEvent { get; }
            internal DiagnosticBoundInput LandingPointDelta { get; }
            internal DiagnosticBoundInput RevisionDistance { get; }
            internal DiagnosticBoundInput RevisionReason { get; }
            internal DiagnosticBoundInput ResidualBeforeDecay { get; }
            internal DiagnosticBoundInput ResidualAfterDecay { get; }
            internal DiagnosticBoundInput ResidualTolerance { get; }
            internal DiagnosticBoundInput TimeToLanding { get; }
            internal DiagnosticBoundInput ReleasingToSwing { get; }
            internal DiagnosticBoundInput ClearanceAfter { get; }
            internal DiagnosticBoundInput[] Required { get; }

            internal static DiagnosticOperatorInputSlot[] Slots() => new[]
            {
                Slot("frame-sequence", DiagnosticValueKind.UInt64),
                Slot("reset-sequence", DiagnosticValueKind.UInt64),
                Slot("delta-seconds", DiagnosticValueKind.Float32),
                Slot("ground-path-input-identity", DiagnosticValueKind.UInt64),
                Slot("path-continuity-evaluated", DiagnosticValueKind.Boolean),
                Slot("path-residual-rebuilt", DiagnosticValueKind.Boolean),
                Slot("path-available-before", DiagnosticValueKind.Boolean),
                Slot("path-available-after", DiagnosticValueKind.Boolean),
                Slot("path-previous-event-identity", DiagnosticValueKind.UInt64),
                Slot("path-current-event-identity", DiagnosticValueKind.UInt64),
                Slot("landing-point-delta-meters", DiagnosticValueKind.Float32),
                Slot("path-revision-distance-meters", DiagnosticValueKind.Float32),
                Slot("path-revision-reason", DiagnosticValueKind.UInt32),
                Slot("residual-before-decay", DiagnosticValueKind.Vector3),
                Slot("residual-after-decay", DiagnosticValueKind.Vector3),
                Slot("residual-tolerance-meters", DiagnosticValueKind.Float32),
                Slot("time-to-landing-seconds", DiagnosticValueKind.Float32),
                Slot("releasing-completed-to-swing", DiagnosticValueKind.Boolean),
                Slot("safety-floor-clearance-after-meters", DiagnosticValueKind.Float32)
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

        sealed class PathCounter
        {
            internal PathCounter(
                string target,
                string dimension,
                in DiagnosticDatasetRow row)
            {
                Target = target;
                Dimension = dimension;
                SequenceStart = row.SampleKey.Sequence;
                SequenceEnd = row.SampleKey.Sequence;
            }

            internal string Target { get; }
            internal string Dimension { get; }
            internal ulong SequenceStart { get; }
            internal ulong SequenceEnd { get; set; }
            internal int Eligible { get; set; }
            internal int Matched { get; set; }
        }

        sealed class PathCounters
        {
            readonly HashSet<string> m_Targets;
            readonly Dictionary<string, PathCounter> m_Counters =
                new Dictionary<string, PathCounter>(StringComparer.Ordinal);

            internal PathCounters(IEnumerable<string> targets)
            {
                m_Targets = new HashSet<string>(targets, StringComparer.Ordinal);
            }

            internal int TotalEligible { get; private set; }
            internal int TotalMatched { get; private set; }

            internal void Eligible(
                string target,
                string dimension,
                in DiagnosticDatasetRow row)
            {
                if (!m_Targets.Contains(target))
                    throw new InvalidOperationException("Path target is invalid.");
                string key = target + "|" + dimension;
                if (!m_Counters.TryGetValue(key, out PathCounter counter))
                {
                    counter = new PathCounter(target, dimension, row);
                    m_Counters.Add(key, counter);
                }
                counter.Eligible++;
                counter.SequenceEnd = row.SampleKey.Sequence;
                TotalEligible++;
            }

            internal void Matched(string target, string dimension)
            {
                PathCounter counter = m_Counters[target + "|" + dimension];
                counter.Matched++;
                TotalMatched++;
            }

            internal IReadOnlyList<DiagnosticEvidence> Evidence(
                in DiagnosticBoundInput frame)
            {
                var result = new List<DiagnosticEvidence>(m_Counters.Count);
                foreach (PathCounter counter in m_Counters.Values)
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

    internal sealed class CharacterFootLandingPathXzDistanceOperator :
        IDiagnosticAnalysisOperator
    {
        const string Frame = "frame-sequence";
        const string Reset = "reset-sequence";
        const string Evaluated = "path-continuity-evaluated";
        const string PathAvailableBefore = "path-available-before";
        const string PathAvailableAfter = "path-available-after";
        const string PreviousEvent = "path-previous-event-identity";
        const string CurrentEvent = "path-current-event-identity";
        const string InputIdentity = "ground-path-input-identity";
        const string LandingPoint = "ground-landing-point";
        const string LandingPointDelta = "landing-point-delta-meters";
        const string RevisionDistance = "path-revision-distance-meters";
        const string RevisionReason = "path-revision-reason";
        const string LandingPointReason = "landing-point-revision-reason";
        const string XzThreshold = "zzz-xz-rewrite-distance-meters";
        const string RepresentativeLimit = "representative-limit";

        public DiagnosticOperatorDescriptor Descriptor { get; } =
            new DiagnosticOperatorDescriptor(
                "foot/landing-path-xz-distance",
                "motion",
                Inputs.Slots(),
                new[]
                {
                    Number(XzThreshold),
                    Integer(LandingPointReason),
                    Integer(RepresentativeLimit)
                });

        public DiagnosticOperatorResult Execute(
            DiagnosticOperatorExecutionContext context)
        {
            var inputs = new Inputs(context);
            double xzThreshold = CharacterFootDiagnosticOperatorSupport.RequireNumber(
                context,
                XzThreshold);
            uint landingPointReason = CharacterFootDiagnosticOperatorSupport.RequireUInt32(
                context,
                LandingPointReason);
            int representativeLimit = checked((int)
                CharacterFootDiagnosticOperatorSupport.RequireUInt32(
                    context,
                    RepresentativeLimit));
            var missing = new HashSet<string>(StringComparer.Ordinal);
            var previousByDimension = new Dictionary<string, DiagnosticDatasetRow>(
                StringComparer.Ordinal);
            var observations = new List<XzObservation>();
            DiagnosticDatasetCursor cursor = context.CreateCursor(0);
            while (cursor.MoveNext())
            {
                DiagnosticDatasetRow current = cursor.Current;
                string dimension = current.SampleKey.DimensionId;
                if (!context.IncludesDimension(dimension) ||
                    !context.MatchesFilters(current) ||
                    !Require(current, inputs.Unconditional, missing))
                {
                    previousByDimension.Remove(dimension);
                    continue;
                }
                bool hasPrevious = previousByDimension.TryGetValue(
                    dimension,
                    out DiagnosticDatasetRow previous);
                bool continuous = hasPrevious &&
                    previous.IsAvailable(inputs.Frame.Handle) &&
                    previous.IsAvailable(inputs.Reset.Handle) &&
                    current.GetUInt64(inputs.Frame.Handle) ==
                    previous.GetUInt64(inputs.Frame.Handle) + 1 &&
                    current.GetUInt64(inputs.Reset.Handle) ==
                    previous.GetUInt64(inputs.Reset.Handle);
                bool pathAvailable = current.GetBoolean(inputs.PathAvailableBefore.Handle) &&
                    current.GetBoolean(inputs.PathAvailableAfter.Handle) &&
                    hasPrevious &&
                    previous.GetBoolean(inputs.PathAvailableAfter.Handle);
                ulong currentEvent = current.GetUInt64(inputs.CurrentEvent.Handle);
                bool sameEvent = continuous &&
                    pathAvailable &&
                    current.GetBoolean(inputs.Evaluated.Handle) &&
                    previous.GetBoolean(inputs.Evaluated.Handle) &&
                    currentEvent != 0 &&
                    previous.GetUInt64(inputs.CurrentEvent.Handle) == currentEvent &&
                    current.GetUInt64(inputs.PreviousEvent.Handle) == currentEvent;
                bool landingChanged = sameEvent &&
                    (current.GetUInt32(inputs.RevisionReason.Handle) & landingPointReason) != 0;
                if (landingChanged)
                {
                    if (!previous.IsAvailable(inputs.LandingPoint.Handle) ||
                        !current.IsAvailable(inputs.LandingPoint.Handle))
                    {
                        missing.Add("character-foot-ik/main/foot/ground-path-next-landing");
                        previousByDimension.Remove(dimension);
                        continue;
                    }
                    DiagnosticVector3 before = previous.GetVector3(
                        inputs.LandingPoint.Handle);
                    DiagnosticVector3 after = current.GetVector3(
                        inputs.LandingPoint.Handle);
                    double dx = after.X - before.X;
                    double dz = after.Z - before.Z;
                    double dy = after.Y - before.Y;
                    double xzDistance = Math.Sqrt(dx * dx + dz * dz);
                    double pointDistance = Math.Sqrt(
                        dx * dx + dy * dy + dz * dz);
                    bool rewrite = xzDistance > xzThreshold;
                    observations.Add(new XzObservation(
                        current,
                        previous,
                        currentEvent,
                        xzDistance,
                        pointDistance,
                        Math.Abs(dy),
                        rewrite,
                        current.GetFloat32(inputs.LandingPointDelta.Handle),
                        current.GetFloat32(inputs.RevisionDistance.Handle),
                        current.GetUInt64(inputs.InputIdentity.Handle),
                        previous.GetUInt64(inputs.InputIdentity.Handle)));
                }
                previousByDimension[dimension] = current;
            }
            if (missing.Count != 0)
                return DiagnosticOperatorResult.MissingEvidence(
                    CharacterFootDiagnosticOperatorSupport.MissingSummary(missing));
            if (observations.Count == 0)
                return DiagnosticOperatorResult.NotApplicable(
                    "No continuous same-event GroundPath LandingPointChanged pair was eligible.");
            observations.Sort((left, right) =>
                right.XzDistance.CompareTo(left.XzDistance));
            var evidence = new List<DiagnosticEvidence>();
            int limit = representativeLimit <= 0
                ? observations.Count
                : Math.Min(representativeLimit, observations.Count);
            for (int i = 0; i < limit; i++)
                evidence.AddRange(BuildEvidence(
                    inputs,
                    observations[i],
                    xzThreshold));
            XzObservation maxCarry = observations.Find(value => !value.Rewrite);
            if (maxCarry != null && !ContainsEvidence(evidence, maxCarry))
                evidence.AddRange(BuildEvidence(
                    inputs,
                    maxCarry,
                    xzThreshold));
            int rewriteCount = observations.FindAll(value => value.Rewrite).Count;
            int carryCount = observations.Count - rewriteCount;
            double[] distances = new double[observations.Count];
            for (int i = 0; i < observations.Count; i++)
                distances[i] = observations[i].XzDistance;
            Array.Sort(distances);
            string summary =
                $"Observed {observations.Count.ToString(CultureInfo.InvariantCulture)} continuous same-event GroundPath LandingPointChanged pairs; " +
                $"carryWithinZzz={carryCount.ToString(CultureInfo.InvariantCulture)}; " +
                $"rewriteBeyondZzz={rewriteCount.ToString(CultureInfo.InvariantCulture)}; " +
                $"zzzThreshold={CharacterFootDiagnosticOperatorSupport.Format(xzThreshold)}m; " +
                $"xzMin={CharacterFootDiagnosticOperatorSupport.Format(distances[0])}m; " +
                $"xzMedian={CharacterFootDiagnosticOperatorSupport.Format(Percentile(distances, 0.5d))}m; " +
                $"xzP90={CharacterFootDiagnosticOperatorSupport.Format(Percentile(distances, 0.9d))}m; " +
                $"xzMax={CharacterFootDiagnosticOperatorSupport.Format(distances[distances.Length - 1])}m; " +
                "PathRevisionDistance is reported separately from the ZZZ XZ threshold; " +
                "ZZZ stair-edge reference seq1532->1533 has target/height-history +0.15m, arr230_y +0.021575m, f54/f56/f57/f58=0.";
            return new DiagnosticOperatorResult(
                DiagnosticRuleState.Passed,
                summary,
                null,
                evidence,
                null);
        }

        static bool ContainsEvidence(
            IReadOnlyList<DiagnosticEvidence> evidence,
            XzObservation observation)
        {
            string dimension = observation.Current.SampleKey.DimensionId;
            for (int i = 0; i < evidence.Count; i++)
            {
                if (evidence[i].SequenceStart == observation.Previous.SampleKey.Sequence &&
                    evidence[i].SequenceEnd == observation.Current.SampleKey.Sequence &&
                    string.Equals(evidence[i].DimensionId, dimension, StringComparison.Ordinal))
                {
                    return true;
                }
            }
            return false;
        }

        static IReadOnlyList<DiagnosticEvidence> BuildEvidence(
            Inputs inputs,
            XzObservation observation,
            double xzThreshold)
        {
            string classification = observation.Rewrite
                ? "RewriteBeyondZzzThreshold"
                : "CarryWithinZzzThreshold";
            var evidence = new List<DiagnosticEvidence>
            {
                CharacterFootDiagnosticOperatorSupport.Evidence(
                    EvidenceId("xz-distance", observation),
                    inputs.LandingPoint,
                    observation.Current.SampleKey.DimensionId,
                    observation.Previous.SampleKey.Sequence,
                    observation.Current.SampleKey.Sequence,
                    CharacterFootDiagnosticOperatorSupport.Format(observation.XzDistance)),
                CharacterFootDiagnosticOperatorSupport.Evidence(
                    EvidenceId("point-distance", observation),
                    inputs.LandingPoint,
                    observation.Current.SampleKey.DimensionId,
                    observation.Previous.SampleKey.Sequence,
                    observation.Current.SampleKey.Sequence,
                    CharacterFootDiagnosticOperatorSupport.Format(observation.PointDistance)),
                CharacterFootDiagnosticOperatorSupport.Evidence(
                    EvidenceId("vertical-delta", observation),
                    inputs.LandingPoint,
                    observation.Current.SampleKey.DimensionId,
                    observation.Previous.SampleKey.Sequence,
                    observation.Current.SampleKey.Sequence,
                    CharacterFootDiagnosticOperatorSupport.Format(observation.VerticalDistance)),
                CharacterFootDiagnosticOperatorSupport.Evidence(
                    EvidenceId("revision-distance", observation),
                    inputs.RevisionDistance,
                    observation.Current.SampleKey.DimensionId,
                    observation.Previous.SampleKey.Sequence,
                    observation.Current.SampleKey.Sequence,
                    CharacterFootDiagnosticOperatorSupport.Format(observation.RevisionDistance)),
                CharacterFootDiagnosticOperatorSupport.Evidence(
                    EvidenceId("recorded-point-distance", observation),
                    inputs.LandingPointDelta,
                    observation.Current.SampleKey.DimensionId,
                    observation.Previous.SampleKey.Sequence,
                    observation.Current.SampleKey.Sequence,
                    CharacterFootDiagnosticOperatorSupport.Format(observation.RecordedPointDistance)),
                ParameterEvidence(
                    EvidenceId("zzz-threshold", observation),
                    XzThreshold,
                    inputs.LandingPoint,
                    observation.Current.SampleKey.DimensionId,
                    observation.Previous.SampleKey.Sequence,
                    observation.Current.SampleKey.Sequence,
                    CharacterFootDiagnosticOperatorSupport.Format(xzThreshold)),
                ParameterEvidence(
                    EvidenceId("classification", observation),
                    "xz-classification",
                    inputs.LandingPoint,
                    observation.Current.SampleKey.DimensionId,
                    observation.Previous.SampleKey.Sequence,
                    observation.Current.SampleKey.Sequence,
                    classification),
                CharacterFootDiagnosticOperatorSupport.Evidence(
                    EvidenceId("event", observation),
                    inputs.CurrentEvent,
                    observation.Current.SampleKey.DimensionId,
                    observation.Previous.SampleKey.Sequence,
                    observation.Current.SampleKey.Sequence,
                    observation.EventIdentity.ToString(CultureInfo.InvariantCulture)),
                CharacterFootDiagnosticOperatorSupport.Evidence(
                    EvidenceId("path-transition", observation),
                    inputs.InputIdentity,
                    observation.Current.SampleKey.DimensionId,
                    observation.Previous.SampleKey.Sequence,
                    observation.Current.SampleKey.Sequence,
                    observation.PreviousInputIdentity.ToString(CultureInfo.InvariantCulture) +
                    "->" + observation.CurrentInputIdentity.ToString(CultureInfo.InvariantCulture))
            };
            return evidence;
        }

        static string EvidenceId(string suffix, XzObservation observation) =>
            "xz-" + suffix + "-" +
            observation.Current.SampleKey.Sequence.ToString(CultureInfo.InvariantCulture);

        static DiagnosticEvidence ParameterEvidence(
            string id,
            string parameter,
            in DiagnosticBoundInput datasetInput,
            string dimension,
            ulong sequenceStart,
            ulong sequenceEnd,
            string value) =>
            new DiagnosticEvidence(
                id,
                datasetInput.DatasetId,
                parameter,
                dimension,
                sequenceStart,
                sequenceEnd,
                value);

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

        static double Percentile(double[] sorted, double percentile)
        {
            if (sorted.Length == 1)
                return sorted[0];
            double position = (sorted.Length - 1) * percentile;
            int lower = (int)Math.Floor(position);
            int upper = (int)Math.Ceiling(position);
            if (lower == upper)
                return sorted[lower];
            double fraction = position - lower;
            return sorted[lower] + (sorted[upper] - sorted[lower]) * fraction;
        }

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
                Evaluated = context.Input("path-continuity-evaluated");
                PathAvailableBefore = context.Input("path-available-before");
                PathAvailableAfter = context.Input("path-available-after");
                PreviousEvent = context.Input("path-previous-event-identity");
                CurrentEvent = context.Input("path-current-event-identity");
                InputIdentity = context.Input("ground-path-input-identity");
                LandingPoint = context.Input("ground-landing-point");
                LandingPointDelta = context.Input("landing-point-delta-meters");
                RevisionDistance = context.Input("path-revision-distance-meters");
                RevisionReason = context.Input("path-revision-reason");
                Unconditional = new[]
                {
                    Frame, Reset, Evaluated, PathAvailableBefore, PathAvailableAfter,
                    PreviousEvent, CurrentEvent, InputIdentity, LandingPointDelta,
                    RevisionDistance, RevisionReason
                };
            }

            internal DiagnosticBoundInput Frame { get; }
            internal DiagnosticBoundInput Reset { get; }
            internal DiagnosticBoundInput Evaluated { get; }
            internal DiagnosticBoundInput PathAvailableBefore { get; }
            internal DiagnosticBoundInput PathAvailableAfter { get; }
            internal DiagnosticBoundInput PreviousEvent { get; }
            internal DiagnosticBoundInput CurrentEvent { get; }
            internal DiagnosticBoundInput InputIdentity { get; }
            internal DiagnosticBoundInput LandingPoint { get; }
            internal DiagnosticBoundInput LandingPointDelta { get; }
            internal DiagnosticBoundInput RevisionDistance { get; }
            internal DiagnosticBoundInput RevisionReason { get; }
            internal IReadOnlyList<DiagnosticBoundInput> Unconditional { get; }

            internal static DiagnosticOperatorInputSlot[] Slots() => new[]
            {
                Slot("frame-sequence", DiagnosticValueKind.UInt64),
                Slot("reset-sequence", DiagnosticValueKind.UInt64),
                Slot("path-continuity-evaluated", DiagnosticValueKind.Boolean),
                Slot("path-available-before", DiagnosticValueKind.Boolean),
                Slot("path-available-after", DiagnosticValueKind.Boolean),
                Slot("path-previous-event-identity", DiagnosticValueKind.UInt64),
                Slot("path-current-event-identity", DiagnosticValueKind.UInt64),
                Slot("ground-path-input-identity", DiagnosticValueKind.UInt64),
                Slot("ground-landing-point", DiagnosticValueKind.Vector3),
                Slot("landing-point-delta-meters", DiagnosticValueKind.Float32),
                Slot("path-revision-distance-meters", DiagnosticValueKind.Float32),
                Slot("path-revision-reason", DiagnosticValueKind.UInt32)
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

        sealed class XzObservation
        {
            internal XzObservation(
                in DiagnosticDatasetRow current,
                in DiagnosticDatasetRow previous,
                ulong eventIdentity,
                double xzDistance,
                double pointDistance,
                double verticalDistance,
                bool rewrite,
                double recordedPointDistance,
                double revisionDistance,
                ulong currentInputIdentity,
                ulong previousInputIdentity)
            {
                Current = current;
                Previous = previous;
                EventIdentity = eventIdentity;
                XzDistance = xzDistance;
                PointDistance = pointDistance;
                VerticalDistance = verticalDistance;
                Rewrite = rewrite;
                RecordedPointDistance = recordedPointDistance;
                RevisionDistance = revisionDistance;
                CurrentInputIdentity = currentInputIdentity;
                PreviousInputIdentity = previousInputIdentity;
            }

            internal DiagnosticDatasetRow Current { get; }
            internal DiagnosticDatasetRow Previous { get; }
            internal ulong EventIdentity { get; }
            internal double XzDistance { get; }
            internal double PointDistance { get; }
            internal double VerticalDistance { get; }
            internal bool Rewrite { get; }
            internal double RecordedPointDistance { get; }
            internal double RevisionDistance { get; }
            internal ulong CurrentInputIdentity { get; }
            internal ulong PreviousInputIdentity { get; }
        }
    }

    internal sealed class CharacterFootLateApproachLandingRevisionOperator :
        IDiagnosticAnalysisOperator
    {
        public DiagnosticOperatorDescriptor Descriptor { get; } =
            new DiagnosticOperatorDescriptor(
                "foot/late-approach-landing-revision",
                "motion",
                Inputs.Slots(),
                Array.Empty<DiagnosticOperatorParameter>());

        public DiagnosticOperatorResult Execute(
            DiagnosticOperatorExecutionContext context)
        {
            var inputs = new Inputs(context);
            var missing = new HashSet<string>(StringComparer.Ordinal);
            var findings = new List<DiagnosticFinding>();
            var previousByDimension = new Dictionary<string, DiagnosticDatasetRow>(
                StringComparer.Ordinal);
            int eligible = 0;
            DiagnosticDatasetCursor cursor = context.CreateCursor(0);
            while (cursor.MoveNext())
            {
                DiagnosticDatasetRow current = cursor.Current;
                string dimension = current.SampleKey.DimensionId;
                if (!context.IncludesDimension(dimension) ||
                    !context.MatchesFilters(current) ||
                    !Require(current, inputs.Required, missing))
                {
                    previousByDimension.Remove(dimension);
                    continue;
                }
                bool hasPrevious = previousByDimension.TryGetValue(
                    dimension,
                    out DiagnosticDatasetRow previous);
                bool continuous = hasPrevious &&
                    Require(previous, inputs.Required, missing) &&
                    current.GetUInt64(inputs.Frame.Handle) ==
                    previous.GetUInt64(inputs.Frame.Handle) + 1 &&
                    current.GetUInt64(inputs.Reset.Handle) ==
                    previous.GetUInt64(inputs.Reset.Handle);
                ulong selectedEvent = current.GetUInt64(inputs.SelectedEvent.Handle);
                bool selectedApproach =
                    current.GetBoolean(inputs.SelectedApproach.Handle);
                bool consumedAvailable =
                    current.GetBoolean(inputs.ConsumedAvailable.Handle);
                bool previousEligible = continuous &&
                    previous.GetUInt64(inputs.SelectedEvent.Handle) != 0 &&
                    previous.GetUInt64(inputs.SelectedEvent.Handle) == selectedEvent &&
                    previous.GetBoolean(inputs.SelectedApproach.Handle) &&
                    selectedApproach &&
                    previous.GetBoolean(inputs.ConsumedAvailable.Handle) &&
                    consumedAvailable &&
                    previous.GetUInt64(inputs.ConsumedEvent.Handle) == selectedEvent &&
                    current.GetUInt64(inputs.ConsumedEvent.Handle) == selectedEvent;
                if (previousEligible)
                {
                    eligible++;
                    int previousSurface =
                        previous.GetInt32(inputs.ConsumedSurface.Handle);
                    int currentSurface = current.GetInt32(inputs.ConsumedSurface.Handle);
                    double pointDelta = CharacterFootDiagnosticOperatorSupport.Distance(
                        previous.GetVector3(inputs.ConsumedPoint.Handle),
                        current.GetVector3(inputs.ConsumedPoint.Handle));
                    double acceptance =
                        current.GetFloat32(inputs.AcceptanceDistance.Handle);
                    if (previousSurface != currentSurface || pointDelta > acceptance)
                    {
                        var evidence = new List<DiagnosticEvidence>
                        {
                            CharacterFootDiagnosticOperatorSupport.Evidence(
                                "landing-point-delta-meters",
                                inputs.ConsumedPoint,
                                dimension,
                                previous.SampleKey.Sequence,
                                current.SampleKey.Sequence,
                                CharacterFootDiagnosticOperatorSupport.Format(pointDelta)),
                            CharacterFootDiagnosticOperatorSupport.Evidence(
                                "landing-acceptance-distance-meters",
                                inputs.AcceptanceDistance,
                                dimension,
                                previous.SampleKey.Sequence,
                                current.SampleKey.Sequence,
                                CharacterFootDiagnosticOperatorSupport.Format(acceptance)),
                            CharacterFootDiagnosticOperatorSupport.Evidence(
                                "surface-transition",
                                inputs.ConsumedSurface,
                                dimension,
                                previous.SampleKey.Sequence,
                                current.SampleKey.Sequence,
                                previousSurface.ToString(CultureInfo.InvariantCulture) + "->" +
                                currentSurface.ToString(CultureInfo.InvariantCulture))
                        };
                        findings.Add(new DiagnosticFinding(
                            CharacterFootDiagnosticOperatorSupport.FindingId(
                                context.RuleId + "-late-approach-landing-revision",
                                current.SampleKey.Sequence),
                            DiagnosticSeverity.Warning,
                            dimension,
                            previous.SampleKey.Sequence,
                            current.SampleKey.Sequence,
                            "Consumed NextSwing Landing changed surface or exceeded Landing Acceptance Distance during the same Approach Contact event.",
                            evidence,
                            CharacterFootDiagnosticOperatorSupport.FrameRange(
                                inputs.Frame,
                                previous.GetUInt64(inputs.Frame.Handle),
                                current.GetUInt64(inputs.Frame.Handle))));
                    }
                }
                previousByDimension[dimension] = current;
            }
            if (missing.Count != 0)
                return DiagnosticOperatorResult.MissingEvidence(
                    CharacterFootDiagnosticOperatorSupport.MissingSummary(missing));
            if (eligible == 0)
                return DiagnosticOperatorResult.NotApplicable(
                    "No continuous same-event Approach Contact frame pair was eligible.");
            string summary =
                $"{findings.Count} of {eligible} late Approach Landing frame pairs failed.";
            return new DiagnosticOperatorResult(
                findings.Count == 0
                    ? DiagnosticRuleState.Passed
                    : DiagnosticRuleState.Failed,
                summary,
                null,
                null,
                findings);
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

        sealed class Inputs
        {
            internal Inputs(DiagnosticOperatorExecutionContext context)
            {
                Frame = context.Input("frame-sequence");
                Reset = context.Input("reset-sequence");
                SelectedEvent = context.Input("selected-landing-event-identity");
                SelectedApproach = context.Input("selected-in-approach-contact");
                ConsumedAvailable = context.Input("consumed-landing-available");
                ConsumedEvent = context.Input("consumed-landing-event-identity");
                ConsumedSurface = context.Input("consumed-landing-surface-identity");
                ConsumedPoint = context.Input("consumed-landing-point");
                AcceptanceDistance = context.Input("landing-acceptance-distance-meters");
                Required = new[]
                {
                    Frame, Reset, SelectedEvent, SelectedApproach, ConsumedAvailable,
                    ConsumedEvent, ConsumedSurface, ConsumedPoint, AcceptanceDistance
                };
            }

            internal DiagnosticBoundInput Frame { get; }
            internal DiagnosticBoundInput Reset { get; }
            internal DiagnosticBoundInput SelectedEvent { get; }
            internal DiagnosticBoundInput SelectedApproach { get; }
            internal DiagnosticBoundInput ConsumedAvailable { get; }
            internal DiagnosticBoundInput ConsumedEvent { get; }
            internal DiagnosticBoundInput ConsumedSurface { get; }
            internal DiagnosticBoundInput ConsumedPoint { get; }
            internal DiagnosticBoundInput AcceptanceDistance { get; }
            internal IReadOnlyList<DiagnosticBoundInput> Required { get; }

            internal static DiagnosticOperatorInputSlot[] Slots() => new[]
            {
                Slot("frame-sequence", DiagnosticValueKind.UInt64),
                Slot("reset-sequence", DiagnosticValueKind.UInt64),
                Slot("selected-landing-event-identity", DiagnosticValueKind.UInt64),
                Slot("selected-in-approach-contact", DiagnosticValueKind.Boolean),
                Slot("consumed-landing-available", DiagnosticValueKind.Boolean),
                Slot("consumed-landing-event-identity", DiagnosticValueKind.UInt64),
                Slot("consumed-landing-surface-identity", DiagnosticValueKind.Int32),
                Slot("consumed-landing-point", DiagnosticValueKind.Vector3),
                Slot("landing-acceptance-distance-meters", DiagnosticValueKind.Float32)
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
    }
}
