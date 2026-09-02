using System;
using System.Collections.Generic;
using System.Globalization;
using KK.GeneratedDiagnosticSampling;
using KK.GeneratedDiagnosticSampling.Host;

namespace ThirdPersonCharacter.Pipeline.Diagnostics.FootAnalysis.Editor
{
    internal sealed class CharacterFootContactSupportGapOperator :
        IDiagnosticAnalysisOperator
    {
        const string LandingState = "landing-state";
        const string LockedState = "locked-state";
        const string ReleasingState = "releasing-state";
        const string FullAnchorResponse = "full-anchor-response";
        const string SlidingResponse = "sliding-response";
        const string GapThreshold = "gap-threshold-meters";
        const string PersistentDuration = "persistent-duration-seconds";
        const string TransientLargeGap = "transient-large-gap-meters";
        const string TouchTolerance = "touch-tolerance-meters";
        const string PositionNoise = "position-noise-meters";
        const string TimeEpsilon = "time-epsilon-seconds";

        static readonly string[] s_Targets =
        {
            "contact-support-gap",
            "full-anchor-contact-gap",
            "sliding-contact-gap",
            "landing-contact-convergence",
            "contact-release-separation",
            "contact-transient-large-gap"
        };

        public DiagnosticOperatorDescriptor Descriptor { get; } =
            new DiagnosticOperatorDescriptor(
                "foot/contact-support-gap",
                "contact",
                GapInputs.Slots(),
                new[]
                {
                    Integer(LandingState),
                    Integer(LockedState),
                    Integer(ReleasingState),
                    Integer(FullAnchorResponse),
                    Integer(SlidingResponse),
                    Number(GapThreshold),
                    Number(PersistentDuration),
                    Number(TransientLargeGap),
                    Number(TouchTolerance),
                    Number(PositionNoise),
                    Number(TimeEpsilon)
                });

        public DiagnosticOperatorResult Execute(
            DiagnosticOperatorExecutionContext context)
        {
            var inputs = new GapInputs(context);
            var policy = new GapPolicy(
                CharacterFootDiagnosticOperatorSupport.RequireUInt32(
                    context,
                    LandingState),
                CharacterFootDiagnosticOperatorSupport.RequireUInt32(
                    context,
                    LockedState),
                CharacterFootDiagnosticOperatorSupport.RequireUInt32(
                    context,
                    ReleasingState),
                CharacterFootDiagnosticOperatorSupport.RequireUInt32(
                    context,
                    FullAnchorResponse),
                CharacterFootDiagnosticOperatorSupport.RequireUInt32(
                    context,
                    SlidingResponse),
                CharacterFootDiagnosticOperatorSupport.RequireNumber(
                    context,
                    GapThreshold),
                CharacterFootDiagnosticOperatorSupport.RequireNumber(
                    context,
                    PersistentDuration),
                CharacterFootDiagnosticOperatorSupport.RequireNumber(
                    context,
                    TransientLargeGap),
                CharacterFootDiagnosticOperatorSupport.RequireNumber(
                    context,
                    TouchTolerance),
                CharacterFootDiagnosticOperatorSupport.RequireNumber(
                    context,
                    PositionNoise),
                CharacterFootDiagnosticOperatorSupport.RequireNumber(
                    context,
                    TimeEpsilon));
            var samplesByDimension = new Dictionary<string, List<GapSample>>(
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
                if (!TryBuildSample(row, inputs, policy, missing, out GapSample sample))
                    continue;
                if (!samplesByDimension.TryGetValue(
                        dimension,
                        out List<GapSample> samples))
                {
                    samples = new List<GapSample>();
                    samplesByDimension.Add(dimension, samples);
                }
                samples.Add(sample);
            }
            if (missing.Count != 0)
                return DiagnosticOperatorResult.MissingEvidence(
                    CharacterFootDiagnosticOperatorSupport.MissingSummary(missing));
            var segments = new List<GapSegment>();
            foreach (List<GapSample> samples in samplesByDimension.Values)
                BuildSegments(samples, policy, segments);
            segments.Sort((left, right) =>
                left.Start.Row.SampleKey.Sequence.CompareTo(
                    right.Start.Row.SampleKey.Sequence));
            var counters = new ContactCounters(s_Targets);
            var findings = new List<DiagnosticFinding>();
            AnalyzeDomainSegments(
                context,
                inputs,
                policy,
                segments,
                counters,
                findings);
            AnalyzeEpisodes(
                context,
                inputs,
                policy,
                samplesByDimension,
                segments,
                counters,
                findings);
            if (counters.TotalEligible == 0)
                return DiagnosticOperatorResult.NotApplicable(
                    "No complete Contact support policy segment was eligible.");
            findings.Sort((left, right) =>
            {
                int sequence = left.SequenceStart.CompareTo(right.SequenceStart);
                return sequence != 0
                    ? sequence
                    : string.CompareOrdinal(left.Id, right.Id);
            });
            string summary =
                $"{counters.TotalMatched} of {counters.TotalEligible} Contact support rule windows failed.";
            return new DiagnosticOperatorResult(
                findings.Count == 0
                    ? DiagnosticRuleState.Passed
                    : DiagnosticRuleState.Failed,
                summary,
                null,
                counters.Evidence(inputs.Frame),
                findings);
        }

        static bool TryBuildSample(
            in DiagnosticDatasetRow row,
            GapInputs inputs,
            GapPolicy policy,
            ISet<string> missing,
            out GapSample sample)
        {
            sample = null;
            if (!Require(row, inputs.Fundamental, missing))
                return false;
            uint state = row.GetUInt32(inputs.State.Handle);
            uint response = row.GetUInt32(inputs.Response.Handle);
            bool requested = row.GetBoolean(inputs.Requested.Handle);
            bool anchorAvailable = row.GetBoolean(inputs.AnchorAvailable.Handle);
            string domain = policy.Domain(state, response);
            bool observed = requested || state == policy.ReleasingState && anchorAvailable;
            if (!observed)
                return false;
            bool applicable = requested &&
                (state == policy.LandingState || state == policy.LockedState) &&
                row.GetBoolean(inputs.Grounded.Handle) &&
                row.GetBoolean(inputs.Authoritative.Handle) &&
                row.GetFloat32(inputs.FormalWeight.Handle) > 0f;
            bool fullWeight =
                row.GetFloat32(inputs.PositionWeight.Handle) >=
                1d - policy.TimeEpsilon;
            bool qualityEligible = applicable && fullWeight;
            bool referenceRequired = qualityEligible || state == policy.ReleasingState;
            if (!referenceRequired)
                return false;
            if (!anchorAvailable ||
                !row.GetBoolean(inputs.PhysicalAvailable.Handle) ||
                string.Equals(domain, "Unclassified", StringComparison.Ordinal))
            {
                missing.Add(!anchorAvailable
                    ? "contact-anchor-unavailable"
                    : !row.GetBoolean(inputs.PhysicalAvailable.Handle)
                        ? "physical-write-available=false"
                        : "contact-holding-state-unavailable");
                return false;
            }
            if (!Require(row, inputs.Reference, missing))
                return false;
            ulong requestEvent = row.GetUInt64(inputs.RequestEvent.Handle);
            ulong anchorEvent = row.GetUInt64(inputs.AnchorEvent.Handle);
            if (state != policy.ReleasingState && anchorEvent != requestEvent)
            {
                missing.Add("same-event-anchor-unavailable");
                return false;
            }
            DiagnosticVector3 anchorNormal =
                row.GetVector3(inputs.AnchorNormal.Handle);
            if (!CharacterFootDiagnosticOperatorSupport.TryNormalize(
                    anchorNormal,
                    out double normalX,
                    out double normalY,
                    out double normalZ))
            {
                missing.Add("invalid-contact-anchor-normal");
                return false;
            }
            DiagnosticVector3 sourceAnkle =
                row.GetVector3(inputs.SourceAnklePosition.Handle);
            DiagnosticQuaternion sourceRotation =
                row.GetQuaternion(inputs.SourceAnkleRotation.Handle);
            DiagnosticVector3 physicalAnkle =
                row.GetVector3(inputs.PhysicalAnklePosition.Handle);
            DiagnosticQuaternion physicalRotation =
                row.GetQuaternion(inputs.PhysicalAnkleRotation.Handle);
            if (!CharacterFootDiagnosticOperatorSupport.TryRebuildPhysicalProbe(
                    sourceAnkle,
                    sourceRotation,
                    row.GetVector3(inputs.SourceHeel.Handle),
                    physicalAnkle,
                    physicalRotation,
                    out DiagnosticVector3 physicalHeel) ||
                !CharacterFootDiagnosticOperatorSupport.TryRebuildPhysicalProbe(
                    sourceAnkle,
                    sourceRotation,
                    row.GetVector3(inputs.SourceToe.Handle),
                    physicalAnkle,
                    physicalRotation,
                    out DiagnosticVector3 physicalToe))
            {
                missing.Add("invalid-physical-ankle-rigid-transform");
                return false;
            }
            DiagnosticVector3 anchorPoint =
                row.GetVector3(inputs.AnchorPoint.Handle);
            double heel = CharacterFootDiagnosticOperatorSupport.Clearance(
                physicalHeel,
                anchorPoint,
                normalX,
                normalY,
                normalZ);
            double toe = CharacterFootDiagnosticOperatorSupport.Clearance(
                physicalToe,
                anchorPoint,
                normalX,
                normalY,
                normalZ);
            sample = new GapSample(
                row,
                row.GetUInt64(inputs.Frame.Handle),
                row.GetUInt64(inputs.Reset.Handle),
                domain,
                qualityEligible,
                fullWeight,
                Math.Max(row.GetFloat32(inputs.Delta.Handle), 0.000001d),
                Math.Max(0d, Math.Min(heel, toe)),
                heel,
                toe,
                anchorEvent,
                row.GetUInt64(inputs.AnchorAcquiredFrame.Handle),
                row.GetUInt64(inputs.AnchorAcquiredCompletion.Handle),
                row.GetUInt64(inputs.AnchorWorldRevision.Handle),
                row.GetInt32(inputs.AnchorSurface.Handle),
                anchorPoint,
                anchorNormal);
            return true;
        }

        static bool Require(
            in DiagnosticDatasetRow row,
            IReadOnlyList<DiagnosticBoundInput> inputs,
            ISet<string> missing)
        {
            bool complete = true;
            for (int i = 0; i < inputs.Count; i++)
            {
                if (row.IsAvailable(inputs[i].Handle))
                    continue;
                missing.Add(CharacterFootDiagnosticOperatorSupport.Identity(inputs[i]));
                complete = false;
            }
            return complete;
        }

        static void BuildSegments(
            IReadOnlyList<GapSample> samples,
            GapPolicy policy,
            ICollection<GapSegment> output)
        {
            int index = 0;
            while (index < samples.Count)
            {
                int start = index;
                while (index + 1 < samples.Count &&
                    SamePolicy(samples[index], samples[index + 1]))
                {
                    index++;
                }
                output.Add(new GapSegment(samples, start, index, policy));
                index++;
            }
        }

        static bool SameReference(GapSample previous, GapSample current) =>
            current.Frame == previous.Frame + 1 &&
            current.Reset == previous.Reset &&
            current.AnchorEvent == previous.AnchorEvent &&
            current.AnchorAcquiredFrame == previous.AnchorAcquiredFrame &&
            current.AnchorAcquiredCompletion == previous.AnchorAcquiredCompletion &&
            current.AnchorWorldRevision == previous.AnchorWorldRevision &&
            current.AnchorSurface == previous.AnchorSurface &&
            Same(previous.AnchorPoint, current.AnchorPoint) &&
            Same(previous.AnchorNormal, current.AnchorNormal);

        static bool SamePolicy(GapSample previous, GapSample current) =>
            SameReference(previous, current) &&
            string.Equals(previous.Domain, current.Domain, StringComparison.Ordinal) &&
            previous.QualityEligible == current.QualityEligible &&
            previous.FullWeight == current.FullWeight;

        static bool Same(
            in DiagnosticVector3 left,
            in DiagnosticVector3 right) =>
            left.X.Equals(right.X) &&
            left.Y.Equals(right.Y) &&
            left.Z.Equals(right.Z);

        static void AnalyzeDomainSegments(
            DiagnosticOperatorExecutionContext context,
            GapInputs inputs,
            GapPolicy policy,
            IReadOnlyList<GapSegment> segments,
            ContactCounters counters,
            ICollection<DiagnosticFinding> findings)
        {
            for (int i = 0; i < segments.Count; i++)
            {
                GapSegment segment = segments[i];
                string target = TargetForDomain(segment.Domain);
                if (target != null &&
                    (segment.QualityEligible ||
                     string.Equals(segment.Domain, "Release", StringComparison.Ordinal)))
                {
                    counters.Eligible(target, segment);
                    double metric = string.Equals(
                        segment.Domain,
                        "Landing",
                        StringComparison.Ordinal)
                        ? segment.PersistentMaximum
                        : segment.Maximum;
                    if (metric > policy.GapThreshold)
                    {
                        counters.Matched(target, segment.Dimension);
                        findings.Add(GapFinding(
                            context.RuleId,
                            target,
                            inputs,
                            segment,
                            metric,
                            string.Equals(segment.Domain, "Release", StringComparison.Ordinal)
                                ? DiagnosticSeverity.Information
                                : CharacterFootDiagnosticOperatorSupport.Severity(metric)));
                    }
                }
                if (segment.QualityEligible &&
                    segment.LongestGapDuration + policy.TimeEpsilon <
                    policy.PersistentDuration)
                {
                    counters.Eligible(s_Targets[5], segment);
                    if (segment.Maximum > policy.TransientLargeGap)
                    {
                        counters.Matched(s_Targets[5], segment.Dimension);
                        findings.Add(GapFinding(
                            context.RuleId,
                            s_Targets[5],
                            inputs,
                            segment,
                            segment.Maximum,
                            DiagnosticSeverity.Information));
                    }
                }
            }
        }

        static void AnalyzeEpisodes(
            DiagnosticOperatorExecutionContext context,
            GapInputs inputs,
            GapPolicy policy,
            IReadOnlyDictionary<string, List<GapSample>> samplesByDimension,
            IReadOnlyList<GapSegment> segments,
            ContactCounters counters,
            ICollection<DiagnosticFinding> findings)
        {
            foreach (List<GapSample> samples in samplesByDimension.Values)
            {
                int index = 0;
                while (index < samples.Count)
                {
                    if (!samples[index].QualityEligible)
                    {
                        index++;
                        continue;
                    }
                    int start = index;
                    while (index + 1 < samples.Count &&
                        samples[index + 1].QualityEligible &&
                        SameReference(samples[index], samples[index + 1]))
                    {
                        index++;
                    }
                    int end = index;
                    GapSample first = samples[start];
                    GapSample last = samples[end];
                    counters.Eligible(
                        s_Targets[0],
                        first.Dimension,
                        first.Row.SampleKey.Sequence,
                        last.Row.SampleKey.Sequence);
                    double scoredMaximum = 0d;
                    for (int segmentIndex = 0; segmentIndex < segments.Count; segmentIndex++)
                    {
                        GapSegment segment = segments[segmentIndex];
                        if (!string.Equals(
                                segment.Dimension,
                                first.Dimension,
                                StringComparison.Ordinal) ||
                            segment.Start.Row.SampleKey.Sequence <
                            first.Row.SampleKey.Sequence ||
                            segment.End.Row.SampleKey.Sequence >
                            last.Row.SampleKey.Sequence)
                        {
                            continue;
                        }
                        scoredMaximum = Math.Max(
                            scoredMaximum,
                            segment.ScoredMaximum);
                    }
                    if (scoredMaximum > policy.GapThreshold)
                    {
                        counters.Matched(s_Targets[0], first.Dimension);
                        findings.Add(new DiagnosticFinding(
                            CharacterFootDiagnosticOperatorSupport.FindingId(
                                context.RuleId + "-" + s_Targets[0],
                                last.Row.SampleKey.Sequence),
                            CharacterFootDiagnosticOperatorSupport.Severity(
                                scoredMaximum),
                            first.Dimension,
                            first.Row.SampleKey.Sequence,
                            last.Row.SampleKey.Sequence,
                            $"A fully weighted Contact episode reached {CharacterFootDiagnosticOperatorSupport.Format(scoredMaximum)} m scored gap.",
                            new[]
                            {
                                CharacterFootDiagnosticOperatorSupport.Evidence(
                                    "scored-gap-maximum-meters",
                                    inputs.PhysicalAnklePosition,
                                    first.Dimension,
                                    first.Row.SampleKey.Sequence,
                                    last.Row.SampleKey.Sequence,
                                    CharacterFootDiagnosticOperatorSupport.Format(
                                        scoredMaximum))
                            },
                            CharacterFootDiagnosticOperatorSupport.FrameRange(
                                inputs.Frame,
                                first.Frame,
                                last.Frame)));
                    }
                    index++;
                }
            }
        }

        static string TargetForDomain(string domain)
        {
            switch (domain)
            {
                case "FullAnchor": return s_Targets[1];
                case "Sliding": return s_Targets[2];
                case "Landing": return s_Targets[3];
                case "Release": return s_Targets[4];
                default: return null;
            }
        }

        static DiagnosticFinding GapFinding(
            string ruleId,
            string target,
            GapInputs inputs,
            GapSegment segment,
            double metric,
            DiagnosticSeverity severity) =>
            new DiagnosticFinding(
                CharacterFootDiagnosticOperatorSupport.FindingId(
                    ruleId + "-" + target,
                    segment.Peak.Row.SampleKey.Sequence),
                severity,
                segment.Dimension,
                segment.Start.Row.SampleKey.Sequence,
                segment.End.Row.SampleKey.Sequence,
                $"{segment.Domain} Contact segment reached {CharacterFootDiagnosticOperatorSupport.Format(metric)} m gap.",
                new[]
                {
                    CharacterFootDiagnosticOperatorSupport.Evidence(
                        "maximum-whole-foot-gap-meters",
                        inputs.PhysicalAnklePosition,
                        segment.Dimension,
                        segment.Start.Row.SampleKey.Sequence,
                        segment.End.Row.SampleKey.Sequence,
                        CharacterFootDiagnosticOperatorSupport.Format(
                            segment.Maximum)),
                    CharacterFootDiagnosticOperatorSupport.Evidence(
                        "longest-gap-duration-seconds",
                        inputs.Delta,
                        segment.Dimension,
                        segment.Start.Row.SampleKey.Sequence,
                        segment.End.Row.SampleKey.Sequence,
                        CharacterFootDiagnosticOperatorSupport.Format(
                            segment.LongestGapDuration))
                },
                CharacterFootDiagnosticOperatorSupport.FrameRange(
                    inputs.Frame,
                    segment.Start.Frame,
                    segment.End.Frame));

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

        internal sealed class GapPolicy
        {
            internal GapPolicy(
                uint landingState,
                uint lockedState,
                uint releasingState,
                uint fullAnchorResponse,
                uint slidingResponse,
                double gapThreshold,
                double persistentDuration,
                double transientLargeGap,
                double touchTolerance,
                double positionNoise,
                double timeEpsilon)
            {
                LandingState = landingState;
                LockedState = lockedState;
                ReleasingState = releasingState;
                FullAnchorResponse = fullAnchorResponse;
                SlidingResponse = slidingResponse;
                GapThreshold = gapThreshold;
                PersistentDuration = persistentDuration;
                TransientLargeGap = transientLargeGap;
                TouchTolerance = touchTolerance;
                PositionNoise = positionNoise;
                TimeEpsilon = timeEpsilon;
            }

            internal uint LandingState { get; }
            internal uint LockedState { get; }
            internal uint ReleasingState { get; }
            internal uint FullAnchorResponse { get; }
            internal uint SlidingResponse { get; }
            internal double GapThreshold { get; }
            internal double PersistentDuration { get; }
            internal double TransientLargeGap { get; }
            internal double TouchTolerance { get; }
            internal double PositionNoise { get; }
            internal double TimeEpsilon { get; }

            internal string Domain(uint state, uint response)
            {
                if (state == LandingState)
                    return "Landing";
                if (state == LockedState && response == FullAnchorResponse)
                    return "FullAnchor";
                if (state == LockedState && response == SlidingResponse)
                    return "Sliding";
                if (state == ReleasingState)
                    return "Release";
                return "Unclassified";
            }
        }

        internal sealed class GapSample
        {
            internal GapSample(
                in DiagnosticDatasetRow row,
                ulong frame,
                ulong reset,
                string domain,
                bool qualityEligible,
                bool fullWeight,
                double deltaSeconds,
                double gap,
                double heelClearance,
                double toeClearance,
                ulong anchorEvent,
                ulong anchorAcquiredFrame,
                ulong anchorAcquiredCompletion,
                ulong anchorWorldRevision,
                int anchorSurface,
                in DiagnosticVector3 anchorPoint,
                in DiagnosticVector3 anchorNormal)
            {
                Row = row;
                Frame = frame;
                Reset = reset;
                Domain = domain;
                QualityEligible = qualityEligible;
                FullWeight = fullWeight;
                DeltaSeconds = deltaSeconds;
                Gap = gap;
                HeelClearance = heelClearance;
                ToeClearance = toeClearance;
                AnchorEvent = anchorEvent;
                AnchorAcquiredFrame = anchorAcquiredFrame;
                AnchorAcquiredCompletion = anchorAcquiredCompletion;
                AnchorWorldRevision = anchorWorldRevision;
                AnchorSurface = anchorSurface;
                AnchorPoint = anchorPoint;
                AnchorNormal = anchorNormal;
            }

            internal DiagnosticDatasetRow Row { get; }
            internal string Dimension => Row.SampleKey.DimensionId;
            internal ulong Frame { get; }
            internal ulong Reset { get; }
            internal string Domain { get; }
            internal bool QualityEligible { get; }
            internal bool FullWeight { get; }
            internal double DeltaSeconds { get; }
            internal double Gap { get; }
            internal double HeelClearance { get; }
            internal double ToeClearance { get; }
            internal ulong AnchorEvent { get; }
            internal ulong AnchorAcquiredFrame { get; }
            internal ulong AnchorAcquiredCompletion { get; }
            internal ulong AnchorWorldRevision { get; }
            internal int AnchorSurface { get; }
            internal DiagnosticVector3 AnchorPoint { get; }
            internal DiagnosticVector3 AnchorNormal { get; }
        }

        internal sealed class GapSegment
        {
            internal GapSegment(
                IReadOnlyList<GapSample> samples,
                int start,
                int end,
                GapPolicy policy)
            {
                Start = samples[start];
                End = samples[end];
                Domain = Start.Domain;
                QualityEligible = Start.QualityEligible;
                Peak = Start;
                double runDuration = 0d;
                double runMaximum = 0d;
                bool previousAbove = false;
                for (int i = start; i <= end; i++)
                {
                    GapSample sample = samples[i];
                    if (sample.Gap > Peak.Gap)
                        Peak = sample;
                    Maximum = Math.Max(Maximum, sample.Gap);
                    bool above = sample.Gap > policy.GapThreshold;
                    if (above)
                    {
                        if (previousAbove)
                            runDuration += sample.DeltaSeconds;
                        runMaximum = Math.Max(runMaximum, sample.Gap);
                    }
                    else if (previousAbove)
                    {
                        FinishRun(policy, ref runDuration, ref runMaximum);
                    }
                    previousAbove = above;
                }
                if (previousAbove)
                    FinishRun(policy, ref runDuration, ref runMaximum);
                bool full = QualityEligible &&
                    string.Equals(Domain, "FullAnchor", StringComparison.Ordinal) &&
                    Maximum > policy.GapThreshold;
                bool sliding = QualityEligible &&
                    string.Equals(Domain, "Sliding", StringComparison.Ordinal) &&
                    Maximum > policy.GapThreshold;
                bool landing = QualityEligible &&
                    string.Equals(Domain, "Landing", StringComparison.Ordinal) &&
                    PersistentMaximum > policy.GapThreshold;
                ScoredMaximum = full || sliding
                    ? Maximum
                    : landing
                        ? PersistentMaximum
                        : 0d;
            }

            internal GapSample Start { get; }
            internal GapSample End { get; }
            internal GapSample Peak { get; private set; }
            internal string Dimension => Start.Dimension;
            internal string Domain { get; }
            internal bool QualityEligible { get; }
            internal double Maximum { get; private set; }
            internal double LongestGapDuration { get; private set; }
            internal double PersistentMaximum { get; private set; }
            internal double ScoredMaximum { get; }

            void FinishRun(
                GapPolicy policy,
                ref double duration,
                ref double maximum)
            {
                LongestGapDuration = Math.Max(LongestGapDuration, duration);
                if (duration + policy.TimeEpsilon >= policy.PersistentDuration)
                    PersistentMaximum = Math.Max(PersistentMaximum, maximum);
                duration = 0d;
                maximum = 0d;
            }
        }

        sealed class GapInputs
        {
            internal GapInputs(DiagnosticOperatorExecutionContext context)
            {
                Frame = context.Input("frame-sequence");
                Reset = context.Input("reset-sequence");
                Delta = context.Input("delta-seconds");
                State = context.Input("constraint-state");
                Response = context.Input("lock-response");
                Requested = context.Input("current-lock-requested");
                RequestEvent = context.Input("current-lock-request-event-identity");
                Grounded = context.Input("grounded");
                Authoritative = context.Input("current-step-authoritative");
                FormalWeight = context.Input("formal-foot-placement-weight");
                PositionWeight = context.Input("goal-position-weight");
                AnchorAvailable = context.Input("anchor-available");
                AnchorEvent = context.Input("anchor-event-identity");
                AnchorAcquiredFrame = context.Input("anchor-acquired-frame-sequence");
                AnchorAcquiredCompletion = context.Input("anchor-acquired-completion-identity");
                AnchorWorldRevision = context.Input("anchor-world-revision");
                AnchorSurface = context.Input("anchor-surface-identity");
                AnchorPoint = context.Input("anchor-point");
                AnchorNormal = context.Input("anchor-normal");
                SourceAnklePosition = context.Input("source-ankle-position");
                SourceAnkleRotation = context.Input("source-ankle-rotation");
                SourceHeel = context.Input("source-heel");
                SourceToe = context.Input("source-toe");
                PhysicalAvailable = context.Input("physical-write-available");
                PhysicalAnklePosition = context.Input("physical-ankle-position");
                PhysicalAnkleRotation = context.Input("physical-ankle-rotation");
                Fundamental = new[]
                {
                    Frame, Reset, Delta, State, Response, Requested, Grounded,
                    Authoritative, FormalWeight, PositionWeight, AnchorAvailable,
                    PhysicalAvailable
                };
                Reference = new[]
                {
                    RequestEvent, AnchorEvent, AnchorAcquiredFrame,
                    AnchorAcquiredCompletion, AnchorWorldRevision, AnchorSurface,
                    AnchorPoint, AnchorNormal, SourceAnklePosition,
                    SourceAnkleRotation, SourceHeel, SourceToe,
                    PhysicalAnklePosition, PhysicalAnkleRotation
                };
            }

            internal DiagnosticBoundInput Frame { get; }
            internal DiagnosticBoundInput Reset { get; }
            internal DiagnosticBoundInput Delta { get; }
            internal DiagnosticBoundInput State { get; }
            internal DiagnosticBoundInput Response { get; }
            internal DiagnosticBoundInput Requested { get; }
            internal DiagnosticBoundInput RequestEvent { get; }
            internal DiagnosticBoundInput Grounded { get; }
            internal DiagnosticBoundInput Authoritative { get; }
            internal DiagnosticBoundInput FormalWeight { get; }
            internal DiagnosticBoundInput PositionWeight { get; }
            internal DiagnosticBoundInput AnchorAvailable { get; }
            internal DiagnosticBoundInput AnchorEvent { get; }
            internal DiagnosticBoundInput AnchorAcquiredFrame { get; }
            internal DiagnosticBoundInput AnchorAcquiredCompletion { get; }
            internal DiagnosticBoundInput AnchorWorldRevision { get; }
            internal DiagnosticBoundInput AnchorSurface { get; }
            internal DiagnosticBoundInput AnchorPoint { get; }
            internal DiagnosticBoundInput AnchorNormal { get; }
            internal DiagnosticBoundInput SourceAnklePosition { get; }
            internal DiagnosticBoundInput SourceAnkleRotation { get; }
            internal DiagnosticBoundInput SourceHeel { get; }
            internal DiagnosticBoundInput SourceToe { get; }
            internal DiagnosticBoundInput PhysicalAvailable { get; }
            internal DiagnosticBoundInput PhysicalAnklePosition { get; }
            internal DiagnosticBoundInput PhysicalAnkleRotation { get; }
            internal IReadOnlyList<DiagnosticBoundInput> Fundamental { get; }
            internal IReadOnlyList<DiagnosticBoundInput> Reference { get; }

            internal static DiagnosticOperatorInputSlot[] Slots() => new[]
            {
                Slot("frame-sequence", DiagnosticValueKind.UInt64),
                Slot("reset-sequence", DiagnosticValueKind.UInt64),
                Slot("delta-seconds", DiagnosticValueKind.Float32),
                Slot("constraint-state", DiagnosticValueKind.UInt32),
                Slot("lock-response", DiagnosticValueKind.UInt32),
                Slot("current-lock-requested", DiagnosticValueKind.Boolean),
                Slot("current-lock-request-event-identity", DiagnosticValueKind.UInt64),
                Slot("grounded", DiagnosticValueKind.Boolean),
                Slot("current-step-authoritative", DiagnosticValueKind.Boolean),
                Slot("formal-foot-placement-weight", DiagnosticValueKind.Float32),
                Slot("goal-position-weight", DiagnosticValueKind.Float32),
                Slot("anchor-available", DiagnosticValueKind.Boolean),
                Slot("anchor-event-identity", DiagnosticValueKind.UInt64),
                Slot("anchor-acquired-frame-sequence", DiagnosticValueKind.UInt64),
                Slot("anchor-acquired-completion-identity", DiagnosticValueKind.UInt64),
                Slot("anchor-world-revision", DiagnosticValueKind.UInt64),
                Slot("anchor-surface-identity", DiagnosticValueKind.Int32),
                Slot("anchor-point", DiagnosticValueKind.Vector3),
                Slot("anchor-normal", DiagnosticValueKind.Vector3),
                Slot("source-ankle-position", DiagnosticValueKind.Vector3),
                Slot("source-ankle-rotation", DiagnosticValueKind.Quaternion),
                Slot("source-heel", DiagnosticValueKind.Vector3),
                Slot("source-toe", DiagnosticValueKind.Vector3),
                Slot("physical-write-available", DiagnosticValueKind.Boolean),
                Slot("physical-ankle-position", DiagnosticValueKind.Vector3),
                Slot("physical-ankle-rotation", DiagnosticValueKind.Quaternion)
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

    internal sealed class CharacterFootContactStateOutputJumpOperator :
        IDiagnosticAnalysisOperator
    {
        const string LandingState = "landing-state";
        const string LockedState = "locked-state";
        const string ReleasingState = "releasing-state";
        const string Threshold = "output-jump-meters";
        const string GeometryEpsilon = "runtime-geometry-epsilon-meters";

        public DiagnosticOperatorDescriptor Descriptor { get; } =
            new DiagnosticOperatorDescriptor(
                "foot/contact-state-output-jump",
                "motion",
                JumpInputs.Slots(),
                new[]
                {
                    Integer(LandingState),
                    Integer(LockedState),
                    Integer(ReleasingState),
                    Number(Threshold),
                    Number(GeometryEpsilon)
                });

        public DiagnosticOperatorResult Execute(
            DiagnosticOperatorExecutionContext context)
        {
            var inputs = new JumpInputs(context);
            uint landingState = CharacterFootDiagnosticOperatorSupport.RequireUInt32(
                context,
                LandingState);
            uint lockedState = CharacterFootDiagnosticOperatorSupport.RequireUInt32(
                context,
                LockedState);
            uint releasingState = CharacterFootDiagnosticOperatorSupport.RequireUInt32(
                context,
                ReleasingState);
            double threshold = CharacterFootDiagnosticOperatorSupport.RequireNumber(
                context,
                Threshold);
            double epsilon = CharacterFootDiagnosticOperatorSupport.RequireNumber(
                context,
                GeometryEpsilon);
            var missing = new HashSet<string>(StringComparer.Ordinal);
            var previousByDimension = new Dictionary<string, DiagnosticDatasetRow>(
                StringComparer.Ordinal);
            var findings = new List<DiagnosticFinding>();
            int eligible = 0;
            DiagnosticDatasetCursor cursor = context.CreateCursor(0);
            while (cursor.MoveNext())
            {
                DiagnosticDatasetRow current = cursor.Current;
                string dimension = current.SampleKey.DimensionId;
                if (!context.IncludesDimension(dimension) ||
                    !context.MatchesFilters(current) ||
                    !Require(current, inputs.Fundamental, missing))
                {
                    previousByDimension.Remove(dimension);
                    continue;
                }
                bool hasPrevious = previousByDimension.TryGetValue(
                    dimension,
                    out DiagnosticDatasetRow previous);
                bool continuous = hasPrevious &&
                    Require(previous, inputs.Fundamental, missing) &&
                    current.GetUInt64(inputs.Frame.Handle) ==
                    previous.GetUInt64(inputs.Frame.Handle) + 1 &&
                    current.GetUInt64(inputs.Reset.Handle) ==
                    previous.GetUInt64(inputs.Reset.Handle);
                if (continuous &&
                    (ContactState(previous.GetUInt32(inputs.State.Handle), landingState, lockedState, releasingState) ||
                     ContactState(current.GetUInt32(inputs.State.Handle), landingState, lockedState, releasingState)))
                {
                    if (!Require(previous, inputs.Probes, missing) ||
                        !Require(current, inputs.Probes, missing) ||
                        !previous.GetBoolean(inputs.PhysicalAvailable.Handle) ||
                        !current.GetBoolean(inputs.PhysicalAvailable.Handle))
                    {
                        missing.Add("physical-write-available=false");
                    }
                    else if (TryProbes(previous, inputs, out ProbeSet previousProbes) &&
                             TryProbes(current, inputs, out ProbeSet currentProbes))
                    {
                        eligible++;
                        double ankle = AdditionalStep(
                            previousProbes.SourceAnkle,
                            currentProbes.SourceAnkle,
                            previousProbes.PhysicalAnkle,
                            currentProbes.PhysicalAnkle,
                            epsilon);
                        double heel = AdditionalStep(
                            previousProbes.SourceHeel,
                            currentProbes.SourceHeel,
                            previousProbes.PhysicalHeel,
                            currentProbes.PhysicalHeel,
                            epsilon);
                        double toe = AdditionalStep(
                            previousProbes.SourceToe,
                            currentProbes.SourceToe,
                            previousProbes.PhysicalToe,
                            currentProbes.PhysicalToe,
                            epsilon);
                        double maximum = Math.Max(ankle, Math.Max(heel, toe));
                        if (maximum > threshold)
                        {
                            findings.Add(new DiagnosticFinding(
                                CharacterFootDiagnosticOperatorSupport.FindingId(
                                    context.RuleId + "-contact-state-output-jump",
                                    current.SampleKey.Sequence),
                                CharacterFootDiagnosticOperatorSupport.Severity(maximum),
                                dimension,
                                previous.SampleKey.Sequence,
                                current.SampleKey.Sequence,
                                $"Contact-state physical output added {CharacterFootDiagnosticOperatorSupport.Format(maximum)} m beyond source motion.",
                                new[]
                                {
                                    CharacterFootDiagnosticOperatorSupport.Evidence(
                                        "contact-state-additional-output-step-meters",
                                        inputs.PhysicalAnklePosition,
                                        dimension,
                                        previous.SampleKey.Sequence,
                                        current.SampleKey.Sequence,
                                        CharacterFootDiagnosticOperatorSupport.Format(maximum)),
                                    CharacterFootDiagnosticOperatorSupport.Evidence(
                                        "ankle-additional-step-meters",
                                        inputs.PhysicalAnklePosition,
                                        dimension,
                                        previous.SampleKey.Sequence,
                                        current.SampleKey.Sequence,
                                        CharacterFootDiagnosticOperatorSupport.Format(ankle)),
                                    CharacterFootDiagnosticOperatorSupport.Evidence(
                                        "heel-additional-step-meters",
                                        inputs.PhysicalAnkleRotation,
                                        dimension,
                                        previous.SampleKey.Sequence,
                                        current.SampleKey.Sequence,
                                        CharacterFootDiagnosticOperatorSupport.Format(heel)),
                                    CharacterFootDiagnosticOperatorSupport.Evidence(
                                        "toe-additional-step-meters",
                                        inputs.PhysicalAnkleRotation,
                                        dimension,
                                        previous.SampleKey.Sequence,
                                        current.SampleKey.Sequence,
                                        CharacterFootDiagnosticOperatorSupport.Format(toe))
                                },
                                CharacterFootDiagnosticOperatorSupport.FrameRange(
                                    inputs.Frame,
                                    previous.GetUInt64(inputs.Frame.Handle),
                                    current.GetUInt64(inputs.Frame.Handle))));
                        }
                    }
                    else
                    {
                        missing.Add("invalid-physical-ankle-rigid-transform");
                    }
                }
                previousByDimension[dimension] = current;
            }
            if (missing.Count != 0)
                return DiagnosticOperatorResult.MissingEvidence(
                    CharacterFootDiagnosticOperatorSupport.MissingSummary(missing));
            if (eligible == 0)
                return DiagnosticOperatorResult.NotApplicable(
                    "No continuous Contact-state physical output frame pair was eligible.");
            string summary =
                $"{findings.Count} of {eligible} Contact-state physical output frame pairs exceeded {CharacterFootDiagnosticOperatorSupport.Format(threshold)} m.";
            return new DiagnosticOperatorResult(
                findings.Count == 0
                    ? DiagnosticRuleState.Passed
                    : DiagnosticRuleState.Failed,
                summary,
                null,
                null,
                findings);
        }

        static bool ContactState(
            uint value,
            uint landing,
            uint locked,
            uint releasing) =>
            value == landing || value == locked || value == releasing;

        static bool Require(
            in DiagnosticDatasetRow row,
            IReadOnlyList<DiagnosticBoundInput> inputs,
            ISet<string> missing)
        {
            bool complete = true;
            for (int i = 0; i < inputs.Count; i++)
            {
                if (row.IsAvailable(inputs[i].Handle))
                    continue;
                missing.Add(CharacterFootDiagnosticOperatorSupport.Identity(inputs[i]));
                complete = false;
            }
            return complete;
        }

        static bool TryProbes(
            in DiagnosticDatasetRow row,
            JumpInputs inputs,
            out ProbeSet probes)
        {
            DiagnosticVector3 sourceAnkle =
                row.GetVector3(inputs.SourceAnklePosition.Handle);
            DiagnosticQuaternion sourceRotation =
                row.GetQuaternion(inputs.SourceAnkleRotation.Handle);
            DiagnosticVector3 sourceHeel =
                row.GetVector3(inputs.SourceHeel.Handle);
            DiagnosticVector3 sourceToe =
                row.GetVector3(inputs.SourceToe.Handle);
            DiagnosticVector3 physicalAnkle =
                row.GetVector3(inputs.PhysicalAnklePosition.Handle);
            DiagnosticQuaternion physicalRotation =
                row.GetQuaternion(inputs.PhysicalAnkleRotation.Handle);
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
                probes = default;
                return false;
            }
            probes = new ProbeSet(
                sourceAnkle,
                sourceHeel,
                sourceToe,
                physicalAnkle,
                physicalHeel,
                physicalToe);
            return true;
        }

        static double AdditionalStep(
            in DiagnosticVector3 previousSource,
            in DiagnosticVector3 source,
            in DiagnosticVector3 previousPhysical,
            in DiagnosticVector3 physical,
            double epsilon)
        {
            double physicalX = physical.X - previousPhysical.X;
            double physicalY = physical.Y - previousPhysical.Y;
            double physicalZ = physical.Z - previousPhysical.Z;
            double sourceX = source.X - previousSource.X;
            double sourceY = source.Y - previousSource.Y;
            double sourceZ = source.Z - previousSource.Z;
            double sourceLengthSquared =
                sourceX * sourceX + sourceY * sourceY + sourceZ * sourceZ;
            double blend = sourceLengthSquared > epsilon * epsilon
                ? Math.Max(0d, Math.Min(
                    1d,
                    (physicalX * sourceX +
                     physicalY * sourceY +
                     physicalZ * sourceZ) /
                    sourceLengthSquared))
                : 0d;
            double x = physicalX - sourceX * blend;
            double y = physicalY - sourceY * blend;
            double z = physicalZ - sourceZ * blend;
            return Math.Sqrt(x * x + y * y + z * z);
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

        readonly struct ProbeSet
        {
            internal ProbeSet(
                in DiagnosticVector3 sourceAnkle,
                in DiagnosticVector3 sourceHeel,
                in DiagnosticVector3 sourceToe,
                in DiagnosticVector3 physicalAnkle,
                in DiagnosticVector3 physicalHeel,
                in DiagnosticVector3 physicalToe)
            {
                SourceAnkle = sourceAnkle;
                SourceHeel = sourceHeel;
                SourceToe = sourceToe;
                PhysicalAnkle = physicalAnkle;
                PhysicalHeel = physicalHeel;
                PhysicalToe = physicalToe;
            }

            internal DiagnosticVector3 SourceAnkle { get; }
            internal DiagnosticVector3 SourceHeel { get; }
            internal DiagnosticVector3 SourceToe { get; }
            internal DiagnosticVector3 PhysicalAnkle { get; }
            internal DiagnosticVector3 PhysicalHeel { get; }
            internal DiagnosticVector3 PhysicalToe { get; }
        }

        sealed class JumpInputs
        {
            internal JumpInputs(DiagnosticOperatorExecutionContext context)
            {
                Frame = context.Input("frame-sequence");
                Reset = context.Input("reset-sequence");
                State = context.Input("constraint-state");
                SourceAnklePosition = context.Input("source-ankle-position");
                SourceAnkleRotation = context.Input("source-ankle-rotation");
                SourceHeel = context.Input("source-heel");
                SourceToe = context.Input("source-toe");
                PhysicalAvailable = context.Input("physical-write-available");
                PhysicalAnklePosition = context.Input("physical-ankle-position");
                PhysicalAnkleRotation = context.Input("physical-ankle-rotation");
                Fundamental = new[] { Frame, Reset, State, PhysicalAvailable };
                Probes = new[]
                {
                    SourceAnklePosition, SourceAnkleRotation, SourceHeel,
                    SourceToe, PhysicalAnklePosition, PhysicalAnkleRotation
                };
            }

            internal DiagnosticBoundInput Frame { get; }
            internal DiagnosticBoundInput Reset { get; }
            internal DiagnosticBoundInput State { get; }
            internal DiagnosticBoundInput SourceAnklePosition { get; }
            internal DiagnosticBoundInput SourceAnkleRotation { get; }
            internal DiagnosticBoundInput SourceHeel { get; }
            internal DiagnosticBoundInput SourceToe { get; }
            internal DiagnosticBoundInput PhysicalAvailable { get; }
            internal DiagnosticBoundInput PhysicalAnklePosition { get; }
            internal DiagnosticBoundInput PhysicalAnkleRotation { get; }
            internal IReadOnlyList<DiagnosticBoundInput> Fundamental { get; }
            internal IReadOnlyList<DiagnosticBoundInput> Probes { get; }

            internal static DiagnosticOperatorInputSlot[] Slots() => new[]
            {
                Slot("frame-sequence", DiagnosticValueKind.UInt64),
                Slot("reset-sequence", DiagnosticValueKind.UInt64),
                Slot("constraint-state", DiagnosticValueKind.UInt32),
                Slot("source-ankle-position", DiagnosticValueKind.Vector3),
                Slot("source-ankle-rotation", DiagnosticValueKind.Quaternion),
                Slot("source-heel", DiagnosticValueKind.Vector3),
                Slot("source-toe", DiagnosticValueKind.Vector3),
                Slot("physical-write-available", DiagnosticValueKind.Boolean),
                Slot("physical-ankle-position", DiagnosticValueKind.Vector3),
                Slot("physical-ankle-rotation", DiagnosticValueKind.Quaternion)
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

    internal sealed class ContactCounters
    {
        readonly HashSet<string> m_Targets;
        readonly Dictionary<string, Counter> m_Counters =
            new Dictionary<string, Counter>(StringComparer.Ordinal);

        internal ContactCounters(IEnumerable<string> targets)
        {
            m_Targets = new HashSet<string>(targets, StringComparer.Ordinal);
        }

        internal int TotalEligible { get; private set; }
        internal int TotalMatched { get; private set; }

        internal void Eligible(string target, CharacterFootContactSupportGapOperator.GapSegment segment) =>
            Eligible(
                target,
                segment.Dimension,
                segment.Start.Row.SampleKey.Sequence,
                segment.End.Row.SampleKey.Sequence);

        internal void Eligible(
            string target,
            string dimension,
            ulong sequenceStart,
            ulong sequenceEnd)
        {
            if (!m_Targets.Contains(target))
                throw new InvalidOperationException("Contact target is invalid.");
            string key = target + "|" + dimension;
            if (!m_Counters.TryGetValue(key, out Counter counter))
            {
                counter = new Counter(
                    target,
                    dimension,
                    sequenceStart,
                    sequenceEnd);
                m_Counters.Add(key, counter);
            }
            counter.Eligible++;
            counter.SequenceEnd = sequenceEnd;
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

        sealed class Counter
        {
            internal Counter(
                string target,
                string dimension,
                ulong sequenceStart,
                ulong sequenceEnd)
            {
                Target = target;
                Dimension = dimension;
                SequenceStart = sequenceStart;
                SequenceEnd = sequenceEnd;
            }

            internal string Target { get; }
            internal string Dimension { get; }
            internal ulong SequenceStart { get; }
            internal ulong SequenceEnd { get; set; }
            internal int Eligible { get; set; }
            internal int Matched { get; set; }
        }
    }
}
