using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    internal sealed class CharacterFootSwingPathJitterDiagnosis : ICharacterFootDiagnosis
    {
        const double CorrectionStepMeters = 0.02d;
        static readonly double[] s_OccurrenceThresholds =
        {
            0.01d,
            0.02d,
            0.05d,
            0.10d
        };
        static readonly double[] s_SpeedOccurrenceThresholds =
        {
            1d,
            2d,
            5d,
            10d
        };

        public string DiagnosticId => "swing-path-jitter";
        public string FileName => "swing-path-jitter.json";

        public CharacterFootDiagnosisDocument Build(CharacterFootDiagnosisContext context)
        {
            List<JObject> allPathChanges =
                context.Events("PathChange");
            List<JObject> events =
                ResolveEligibleEvents(allPathChanges);
            List<JObject> phaseEvents =
                ResolvePhaseAdvanceEvents(allPathChanges);
            List<JObject> stablePhaseEvents =
                context.Events("StablePathSwingPhaseJump");
            List<JObject> sameGroundPathEnvelopeSteps =
                context.Events("SameGroundPathSwingEnvelopeStep");
            CharacterFootDiagnosisTarget target = context.Target(
                "path-change-correction-jump",
                "无Anchor Swing的Ground Path变化附近是否出现修正跳变",
                new[] { "PathChange" },
                new[] { "correctionStepMaximumMeters>0.02" },
                events,
                value => CharacterFootDiagnosisContext.Metric(
                             value,
                             "correctionStepMaximumMeters") > CorrectionStepMeters
                    ? new List<string> { "correctionStepMaximumMeters>0.02" }
                    : new List<string>(),
                value => CharacterFootDiagnosisContext.Metric(
                             value,
                             "correctionStepMaximumMeters") /
                         CorrectionStepMeters,
                "nextLandingEndpointDeltaMeters",
                "nextLandingEndpointDeltaMinimumMeters",
                "nextLandingEndpointDeltaMaximumMeters",
                "semanticPathChangeCount",
                "correctionStepMaximumMeters",
                "correctionExcursionMeters",
                "correctionJerkMetersPerSecondCubed",
                "ActualReconstructionError",
                "PhaseAdvanceDelta",
                "PathRevisionDelta",
                "ObservedSwingTargetDelta",
                "PathRevisionContribution",
                "PhaseContribution");
            target.occurrence = context.Occurrence(
                "UniqueUnanchoredSwingPathJump",
                "correctionStepMaximumMeters",
                "Meters",
                events,
                CorrectionStepMeters,
                s_OccurrenceThresholds);
            target.useAsPrimaryOccurrence = true;
            CharacterFootPathStageDiagnosisProjection.Apply(
                target,
                events);
            CharacterFootDiagnosisTarget phaseTarget = context.Target(
                "swing-phase-advance",
                "Ground Path变化窗口中的Swing Target跳变是否已由Landing Phase推进解释",
                new[] { "PathChange" },
                new[] { "swingPhaseAdvance" },
                phaseEvents,
                value => new List<string>
                {
                    "swingPhaseAdvance"
                },
                value => CharacterFootDiagnosisContext.Metric(
                    value,
                    "PhaseAdvanceDelta"),
                "ActualReconstructionError",
                "PhaseAdvanceDelta",
                "PathRevisionDelta",
                "ObservedSwingTargetDelta",
                "PathRevisionContribution",
                "PhaseContribution");
            CharacterFootPathStageDiagnosisProjection.Apply(
                phaseTarget,
                phaseEvents);
            CharacterFootDiagnosisTarget stablePhaseTarget = context.Target(
                "stable-path-swing-phase-jump",
                "同Landing Event与Formal Source的完整上楼Swing段是否持续逐帧修订并产生跳变",
                new[] { "StablePathSwingPhaseJump" },
                new[] { "ObservedSwingTargetDelta>0.02" },
                stablePhaseEvents,
                value => CharacterFootDiagnosisContext.Metric(
                             value,
                             "ObservedSwingTargetDeltaMaximum") >
                         CorrectionStepMeters
                    ? new List<string>
                    {
                        "sequenceContainsObservedSwingTargetDelta>0.02"
                    }
                    : new List<string>(),
                value => Math.Max(
                    CharacterFootDiagnosisContext.Metric(
                        value,
                        "ObservedSwingTargetDeltaMaximum"),
                    Math.Max(
                        CharacterFootDiagnosisContext.Metric(
                            value,
                            "FinalPhysicalAnkleDeltaMaximum"),
                        CharacterFootDiagnosisContext.Metric(
                            value,
                            "FinalPhysicalSoleDeltaMaximum"))),
                "framePairCount",
                "pathResidualRebuildCount",
                "pathResidualRebuildRate",
                "maximumConsecutiveRebuildFrames",
                "endpointTreadChangedCount",
                "endpointYStableTargetRevisedCount",
                "PredictedStepHeightMedian",
                "PredictedStepHeightP90",
                "PredictedStepHeightMaximum",
                "ObservedSwingTargetDeltaMaximum",
                "DesiredCorrectionDeltaMaximum",
                "FinalCorrectionDeltaMaximum",
                "CurrentAnimatedSoleDeltaMaximum",
                "FinalPhysicalAnkleDeltaMaximum",
                "FinalPhysicalSoleDeltaMaximum",
                "SafetyFloorClampMaximum");
            ApplyStablePathFrameStatistics(
                stablePhaseTarget,
                stablePhaseEvents);
            CharacterFootDiagnosisTarget sameGroundPathTarget =
                context.Target(
                    "same-ground-path-swing-envelope-step-jump",
                    "同一GroundPath内普通Swing跨Envelope台阶段时高度约束是否驱动Correction与物理脚同向跳变",
                    new[] { "SameGroundPathSwingEnvelopeStep" },
                    new[] { "causalChainMatched=true" },
                    sameGroundPathEnvelopeSteps,
                    value => CharacterFootDiagnosisContext.Evidence(
                                 value,
                                 "causalChainMatched")
                        ? new List<string>
                        {
                            "causalChainMatched=true"
                        }
                        : new List<string>(),
                    value => Math.Max(
                        CharacterFootDiagnosisContext.Metric(
                            value,
                            "EnvelopeSampleAlongUpStep"),
                        Math.Max(
                            CharacterFootDiagnosisContext.Metric(
                                value,
                                "FinalCorrectionAlongUpStep"),
                            Math.Max(
                                CharacterFootDiagnosisContext.Metric(
                                    value,
                                    "PhysicalAnkleAlongUpStep"),
                                CharacterFootDiagnosisContext.Metric(
                                    value,
                                    "PhysicalSoleAlongUpStep")))),
                    "EnvelopeSampleAlongUpDelta",
                    "EnvelopeSampleAlongUpStep",
                    "AnimatedSoleAlongUpDelta",
                    "AnimatedSoleAlongUpStep",
                    "FormalFootHeightDelta",
                    "FormalTargetSoleHeightDelta",
                    "EnvelopeConstraintDelta",
                    "FormalHeightConstraintDelta",
                    "DesiredCorrectionAlongUpDelta",
                    "DesiredCorrectionAlongUpStep",
                    "FinalCorrectionAlongUpDelta",
                    "FinalCorrectionAlongUpStep",
                    "PhysicalAnkleAlongUpDelta",
                    "PhysicalAnkleAlongUpStep",
                    "PhysicalSoleAlongUpDelta",
                    "PhysicalSoleAlongUpStep",
                    "SafetyFloorClamp",
                    "SafetyFloorClampDelta",
                    "SafetyFloorOutputAlongUpDelta",
                    "ProgressDelta",
                    "PresentationDeltaSeconds",
                    "BodyTickSpan");
            sameGroundPathTarget.categoricalMeasurements =
                new SortedDictionary<
                    string,
                    List<CharacterFootDiagnosisCategoryCount>>(
                    StringComparer.Ordinal)
                {
                    ["Classification"] = sameGroundPathEnvelopeSteps
                        .GroupBy(
                            value => value[
                                    "sameGroundPathSwingEnvelopeStep"]?
                                ["classification"]?.Value<string>() ??
                                "Unspecified",
                            StringComparer.Ordinal)
                        .OrderBy(
                            value => value.Key,
                            StringComparer.Ordinal)
                        .Select(value =>
                            new CharacterFootDiagnosisCategoryCount
                            {
                                value = value.Key,
                                count = value.Count()
                            })
                        .ToList()
                };
            return context.Document(
                DiagnosticId,
                target,
                phaseTarget,
                stablePhaseTarget,
                sameGroundPathTarget);
        }

        static void ApplyStablePathFrameStatistics(
            CharacterFootDiagnosisTarget target,
            List<JObject> segmentEvents)
        {
            List<JObject> frames = segmentEvents
                .SelectMany(value =>
                    (value["stablePathSwingPhaseJump"]?["sequence"] as
                     JArray ?? new JArray()).OfType<JObject>())
                .OrderBy(value => value.Value<int?>("frame") ?? 0)
                .ThenBy(
                    value => value.Value<string>("side"),
                    StringComparer.Ordinal)
                .ToList();
            int matched = frames.Count(value =>
                value.Value<double?>(
                    "observedSwingTargetDeltaMeters") >
                CorrectionStepMeters);
            target.eligibleEventCount = frames.Count;
            target.matchedEventCount = matched;
            target.matchedEventRateAvailable = frames.Count > 0;
            target.matchedEventRate = frames.Count > 0
                ? (double?)matched / frames.Count
                : null;
            target.measurements = new SortedDictionary<
                string,
                CharacterFootDiagnosisDistribution>(
                StringComparer.Ordinal)
            {
                ["PresentationDeltaSeconds"] = Distribution(
                    frames,
                    "presentationDeltaSeconds"),
                ["BodyTickSpan"] = Distribution(
                    frames,
                    "bodyTickSpan"),
                ["NextLandingYDelta"] = Distribution(
                    frames,
                    "nextLandingYDeltaMeters"),
                ["NextLandingYStep"] = Distribution(
                    frames,
                    "nextLandingYStepMeters"),
                ["NextLandingAlongUpDelta"] = Distribution(
                    frames,
                    "nextLandingAlongUpDeltaMeters"),
                ["PredictedStepHeight"] = Distribution(
                    frames,
                    "predictedStepHeightMeters"),
                ["LandingPointDelta"] = Distribution(
                    frames,
                    "landingPointDeltaMeters"),
                ["TargetDelta"] = Distribution(
                    frames,
                    "targetDeltaMeters"),
                ["PathRevisionDelta"] = Distribution(
                    frames,
                    "pathRevisionDeltaMeters"),
                ["PhaseAdvanceDelta"] = Distribution(
                    frames,
                    "phaseAdvanceDeltaMeters"),
                ["ObservedSwingTargetDelta"] = Distribution(
                    frames,
                    "observedSwingTargetDeltaMeters"),
                ["ObservedSwingTargetSpeedMetersPerSecond"] =
                    Distribution(
                        frames,
                        "observedSwingTargetSpeedMetersPerSecond"),
                ["ObservedSwingTargetAccelerationMetersPerSecondSquared"] =
                    Distribution(
                        frames,
                        "observedSwingTargetAccelerationMetersPerSecondSquared",
                        "observedSwingTargetAccelerationAvailable"),
                ["ObservedSwingTargetJerkMetersPerSecondCubed"] =
                    Distribution(
                        frames,
                        "observedSwingTargetJerkMetersPerSecondCubed",
                        "observedSwingTargetJerkAvailable"),
                ["ActualReconstructionError"] = Distribution(
                    frames,
                    "actualReconstructionErrorMeters"),
                ["PathRevisionContribution"] = Distribution(
                    frames,
                    "pathRevisionContribution"),
                ["PhaseContribution"] = Distribution(
                    frames,
                    "phaseContribution"),
                ["ProgressDelta"] = Distribution(
                    frames,
                    "progressDelta"),
                ["EnvelopeSampleDelta"] = Distribution(
                    frames,
                    "envelopeSampleDeltaMeters"),
                ["ResidualBeforeRevision"] = Distribution(
                    frames,
                    "residualBeforeRevisionMeters"),
                ["ResidualAfterDecay"] = Distribution(
                    frames,
                    "residualAfterDecayMeters"),
                ["DesiredCorrectionDelta"] = Distribution(
                    frames,
                    "desiredCorrectionDeltaMeters"),
                ["DesiredCorrectionSpeedMetersPerSecond"] =
                    Distribution(
                        frames,
                        "desiredCorrectionSpeedMetersPerSecond"),
                ["FinalCorrectionDelta"] = Distribution(
                    frames,
                    "finalCorrectionDeltaMeters"),
                ["FinalCorrectionSpeedMetersPerSecond"] =
                    Distribution(
                        frames,
                        "finalCorrectionSpeedMetersPerSecond"),
                ["FinalCorrectionAccelerationMetersPerSecondSquared"] =
                    Distribution(
                        frames,
                        "finalCorrectionAccelerationMetersPerSecondSquared",
                        "finalCorrectionAccelerationAvailable"),
                ["FinalCorrectionJerkMetersPerSecondCubed"] =
                    Distribution(
                        frames,
                        "finalCorrectionJerkMetersPerSecondCubed",
                        "finalCorrectionJerkAvailable"),
                ["CurrentAnimatedSoleDelta"] = Distribution(
                    frames,
                    "currentAnimatedSoleDeltaMeters"),
                ["CurrentAnimatedSoleYDelta"] = Distribution(
                    frames,
                    "currentAnimatedSoleYDeltaMeters"),
                ["CurrentAnimatedSoleYStep"] = Distribution(
                    frames,
                    "currentAnimatedSoleYStepMeters"),
                ["CurrentAnimatedSoleSpeedMetersPerSecond"] =
                    Distribution(
                        frames,
                        "currentAnimatedSoleSpeedMetersPerSecond"),
                ["CurrentAnimatedSoleYSpeedMetersPerSecond"] =
                    Distribution(
                        frames,
                        "currentAnimatedSoleYSpeedMetersPerSecond"),
                ["FinalPhysicalAnkleDelta"] = Distribution(
                    frames,
                    "finalPhysicalAnkleDeltaMeters",
                    "finalPhysicalAnkleAvailable"),
                ["FinalPhysicalAnkleYDelta"] = Distribution(
                    frames,
                    "finalPhysicalAnkleYDeltaMeters",
                    "finalPhysicalAnkleAvailable"),
                ["FinalPhysicalAnkleYStep"] = Distribution(
                    frames,
                    "finalPhysicalAnkleYStepMeters",
                    "finalPhysicalAnkleAvailable"),
                ["FinalPhysicalAnkleSpeedMetersPerSecond"] =
                    Distribution(
                        frames,
                        "finalPhysicalAnkleSpeedMetersPerSecond",
                        "finalPhysicalAnkleAvailable"),
                ["FinalPhysicalAnkleYSpeedMetersPerSecond"] =
                    Distribution(
                        frames,
                        "finalPhysicalAnkleYSpeedMetersPerSecond",
                        "finalPhysicalAnkleAvailable"),
                ["FinalPhysicalAnkleYAccelerationMetersPerSecondSquared"] =
                    Distribution(
                        frames,
                        "finalPhysicalAnkleYAccelerationMetersPerSecondSquared",
                        "finalPhysicalAnkleYAccelerationAvailable"),
                ["FinalPhysicalAnkleYJerkMetersPerSecondCubed"] =
                    Distribution(
                        frames,
                        "finalPhysicalAnkleYJerkMetersPerSecondCubed",
                        "finalPhysicalAnkleYJerkAvailable"),
                ["FinalPhysicalAnkleAccelerationMetersPerSecondSquared"] =
                    Distribution(
                        frames,
                        "finalPhysicalAnkleAccelerationMetersPerSecondSquared",
                        "finalPhysicalAnkleAccelerationAvailable"),
                ["FinalPhysicalAnkleJerkMetersPerSecondCubed"] =
                    Distribution(
                        frames,
                        "finalPhysicalAnkleJerkMetersPerSecondCubed",
                        "finalPhysicalAnkleJerkAvailable"),
                ["FinalPhysicalSoleDelta"] = Distribution(
                    frames,
                    "finalPhysicalSoleDeltaMeters",
                    "finalPhysicalSoleAvailable"),
                ["FinalPhysicalSoleYDelta"] = Distribution(
                    frames,
                    "finalPhysicalSoleYDeltaMeters",
                    "finalPhysicalSoleAvailable"),
                ["FinalPhysicalSoleYStep"] = Distribution(
                    frames,
                    "finalPhysicalSoleYStepMeters",
                    "finalPhysicalSoleAvailable"),
                ["FinalPhysicalSoleSpeedMetersPerSecond"] =
                    Distribution(
                        frames,
                        "finalPhysicalSoleSpeedMetersPerSecond",
                        "finalPhysicalSoleAvailable"),
                ["FinalPhysicalSoleYSpeedMetersPerSecond"] =
                    Distribution(
                        frames,
                        "finalPhysicalSoleYSpeedMetersPerSecond",
                        "finalPhysicalSoleAvailable"),
                ["SafetyFloorClamp"] = Distribution(
                    frames,
                    "safetyFloorClampMeters")
            };
            target.categoricalMeasurements = new SortedDictionary<
                string,
                List<CharacterFootDiagnosisCategoryCount>>(
                StringComparer.Ordinal)
            {
                ["NextLandingYDeltaMeters"] = CategoryCounts(
                    frames,
                    value =>
                    {
                        double delta = value.Value<double>(
                            "nextLandingYDeltaMeters");
                        if (Math.Abs(delta) < 0.005d)
                            delta = 0d;
                        return Math.Round(delta, 2).ToString(
                            "0.00",
                            System.Globalization.CultureInfo
                                .InvariantCulture);
                    }),
                ["EndpointRevisionClass"] = CategoryCounts(
                    frames,
                    value => value.Value<string>(
                                 "endpointRevisionClass") ??
                             string.Empty),
                ["PathRevisionReason"] = CategoryCounts(
                    frames,
                    value => value.Value<string>(
                                 "pathRevisionReason") ??
                             string.Empty),
                ["LargeStepExplanation"] = CategoryCounts(
                    frames,
                    value => value.Value<string>(
                                 "largeStepExplanation") ??
                             string.Empty)
            };
            target.occurrence = BuildOccurrence(
                frames,
                "observedSwingTargetDeltaMeters",
                "ObservedSwingTargetDelta",
                "Meters",
                CorrectionStepMeters,
                s_OccurrenceThresholds);
            target.supplementalOccurrences =
                new List<CharacterFootDiagnosisOccurrenceProfile>
                {
                    BuildOccurrence(
                        frames,
                        "observedSwingTargetSpeedMetersPerSecond",
                        "ObservedSwingTargetSpeedMetersPerSecond",
                        "MetersPerSecond",
                        5d,
                        s_SpeedOccurrenceThresholds),
                    BuildOccurrence(
                        frames,
                        "finalPhysicalAnkleYSpeedMetersPerSecond",
                        "FinalPhysicalAnkleYSpeedMetersPerSecond",
                        "MetersPerSecond",
                        5d,
                        s_SpeedOccurrenceThresholds,
                        "finalPhysicalAnkleAvailable")
                };
        }

        static CharacterFootDiagnosisDistribution Distribution(
            List<JObject> frames,
            string property,
            string availabilityProperty = null)
        {
            IEnumerable<JObject> eligible = frames;
            if (!string.IsNullOrEmpty(availabilityProperty))
            {
                eligible = eligible.Where(value =>
                    value.Value<bool?>(availabilityProperty) == true);
            }
            return CharacterFootDiagnosisDistribution.Create(
                eligible.Select(value =>
                        value.Value<double?>(property) ??
                        throw new InvalidOperationException(
                            $"Stable Path Swing frame metric '{property}' is unavailable."))
                    .ToList());
        }

        static List<CharacterFootDiagnosisCategoryCount> CategoryCounts(
            List<JObject> frames,
            Func<JObject, string> selector) =>
            frames
                .GroupBy(selector, StringComparer.Ordinal)
                .OrderBy(value => value.Key, StringComparer.Ordinal)
                .Select(value => new CharacterFootDiagnosisCategoryCount
                {
                    value = value.Key,
                    count = value.Count()
                })
                .ToList();

        static CharacterFootDiagnosisOccurrenceProfile BuildOccurrence(
            List<JObject> frames,
            string property,
            string metric,
            string thresholdUnit,
            double primaryThreshold,
            double[] thresholds,
            string availabilityProperty = null)
        {
            List<JObject> eligible = string.IsNullOrEmpty(
                    availabilityProperty)
                ? frames
                : frames.Where(value =>
                        value.Value<bool?>(availabilityProperty) == true)
                    .ToList();
            var profile = new CharacterFootDiagnosisOccurrenceProfile
            {
                available = eligible.Count > 0,
                sampleUnit =
                    "ContinuousSameEventSourceUpStairSwingFramePair",
                metric = metric,
                comparison = "GreaterThan",
                thresholdUnit = thresholdUnit,
                eligibleEventCount = eligible.Count,
                configuredThresholds = thresholds.ToList(),
                rates = new List<CharacterFootDiagnosisOccurrenceRate>()
            };
            if (eligible.Count == 0)
                return profile;
            List<double> values = eligible.Select(value =>
                    value.Value<double?>(property) ??
                    throw new InvalidOperationException(
                        $"Stable Path Swing occurrence metric '{property}' is unavailable."))
                .ToList();
            foreach (double threshold in thresholds)
            {
                int count = values.Count(value => value > threshold);
                var rate = new CharacterFootDiagnosisOccurrenceRate
                {
                    threshold = threshold,
                    eligibleEventCount = values.Count,
                    matchedEventCount = count,
                    matchedEventRate = (double)count / values.Count
                };
                profile.rates.Add(rate);
                if (threshold == primaryThreshold)
                    profile.primaryRate = rate;
            }
            return profile;
        }

        static List<JObject> ResolveEligibleEvents(List<JObject> events) =>
            events
                .Where(value =>
                    CharacterFootDiagnosisContext.Evidence(
                        value,
                        "unanchoredSwingEligible") &&
                    !CharacterFootDiagnosisContext.Evidence(
                        value,
                        "anchorAvailable") &&
                    !CharacterFootDiagnosisContext.Evidence(
                        value,
                        "swingPhaseAdvance"))
                .GroupBy(
                    value => (
                        side: value.Value<string>("side") ?? string.Empty,
                        peakFrame: value.Value<int?>("peakFrame") ?? 0))
                .Select(MergePeakGroup)
                .OrderBy(value => value.Value<int?>("peakFrame") ?? 0)
                .ThenBy(
                    value => value.Value<string>("side"),
                    StringComparer.Ordinal)
                .ToList();

        static List<JObject> ResolvePhaseAdvanceEvents(
            List<JObject> events) =>
            events
                .Where(value =>
                    CharacterFootDiagnosisContext.Evidence(
                        value,
                        "unanchoredSwingEligible") &&
                    !CharacterFootDiagnosisContext.Evidence(
                        value,
                        "anchorAvailable") &&
                    CharacterFootDiagnosisContext.Evidence(
                        value,
                        "swingPhaseAdvance"))
                .GroupBy(
                    value => (
                        side: value.Value<string>("side") ??
                              string.Empty,
                        peakFrame:
                            value.Value<int?>("peakFrame") ?? 0))
                .Select(MergePeakGroup)
                .OrderBy(
                    value =>
                        value.Value<int?>("peakFrame") ?? 0)
                .ThenBy(
                    value => value.Value<string>("side"),
                    StringComparer.Ordinal)
                .ToList();

        static JObject MergePeakGroup(
            IGrouping<(string side, int peakFrame), JObject> group)
        {
            List<JObject> values = group.ToList();
            JObject selected = values
                .OrderByDescending(value =>
                    value["pathStageAnalysis"]?["available"]
                        ?.Value<bool?>() ?? false)
                .ThenBy(value => Math.Abs(
                    group.Key.peakFrame -
                    (value.Value<int?>("endFrame") ?? 0)))
                .ThenBy(value => value.Value<int?>("startFrame") ?? 0)
                .First();
            var merged = (JObject)selected.DeepClone();
            merged["startFrame"] = values.Min(
                value => value.Value<int?>("startFrame") ?? 0);
            merged["endFrame"] = values.Max(
                value => value.Value<int?>("endFrame") ?? 0);
            JObject metrics = merged["metrics"] as JObject ?? new JObject();
            metrics["semanticPathChangeCount"] = values.Count;
            metrics["nextLandingEndpointDeltaMinimumMeters"] = values.Min(
                value => CharacterFootDiagnosisContext.Metric(
                    value,
                    "nextLandingEndpointDeltaMeters"));
            metrics["nextLandingEndpointDeltaMaximumMeters"] = values.Max(
                value => CharacterFootDiagnosisContext.Metric(
                    value,
                    "nextLandingEndpointDeltaMeters"));
            merged["metrics"] = metrics;
            return merged;
        }
    }

    [Serializable]
    internal sealed class CharacterFootStablePathSwingPhaseJumpAnalysis
    {
        public int startFrame;
        public int endFrame;
        public string side;
        public string eventIdentity;
        public string sourceIdentity;
        public int sourceCycleStart;
        public int sourceCycleEnd;
        public string contributionContinuityIdentityStart;
        public string contributionContinuityIdentityEnd;
        public int framePairCount;
        public int pathResidualRebuildCount;
        public double pathResidualRebuildRate;
        public int maximumConsecutiveRebuildFrames;
        public int endpointTreadChangedCount;
        public int endpointYStableTargetRevisedCount;
        public double predictedStepHeightMedianMeters;
        public double predictedStepHeightP90Meters;
        public double predictedStepHeightMaximumMeters;
        public double presentationDeltaMedianSeconds;
        public double presentationDeltaP90Seconds;
        public double finalPhysicalAnkleYDeltaP90Meters;
        public double finalPhysicalAnkleYSpeedP90MetersPerSecond;
        public int lowPresentationSamplingLargeStepCount;
        public int speedAnomalyCount;
        public SortedDictionary<string, int> revisionReasonCounts;
        public List<CharacterFootStablePathSwingFrameRevision> sequence;
    }

    [Serializable]
    internal sealed class CharacterFootStablePathSwingFrameRevision
    {
        public int previousFrame;
        public int frame;
        public double presentationDeltaSeconds;
        public ulong bodyTickSpan;
        public string previousEventIdentity;
        public string eventIdentity;
        public string previousSourceIdentity;
        public string sourceIdentity;
        public int previousSourceCycle;
        public int sourceCycle;
        public string previousContributionContinuityIdentity;
        public string contributionContinuityIdentity;
        public string previousRawPathInputIdentity;
        public string rawPathInputIdentity;
        public double predictedStepHeightMeters;
        public double nextLandingYDeltaMeters;
        public double nextLandingYStepMeters;
        public double nextLandingAlongUpDeltaMeters;
        public bool endpointTreadChanged;
        public bool endpointYStable;
        public bool endpointYStableButTargetRevised;
        public string endpointRevisionClass;
        public double landingPointDeltaMeters;
        public double targetDeltaMeters;
        public bool frozenPathCounterfactualAvailable;
        public double pathRevisionDeltaMeters;
        public double phaseAdvanceDeltaMeters;
        public double observedSwingTargetDeltaMeters;
        public double observedSwingTargetSpeedMetersPerSecond;
        public bool observedSwingTargetAccelerationAvailable;
        public double observedSwingTargetAccelerationMetersPerSecondSquared;
        public bool observedSwingTargetJerkAvailable;
        public double observedSwingTargetJerkMetersPerSecondCubed;
        public double actualReconstructionErrorMeters;
        public double pathRevisionContribution;
        public double phaseContribution;
        public double progressDelta;
        public double envelopeSampleDeltaMeters;
        public CharacterFootVectorFact residualBeforeRevision;
        public double residualBeforeRevisionMeters;
        public CharacterFootVectorFact residualAfterDecay;
        public double residualAfterDecayMeters;
        public double desiredCorrectionDeltaMeters;
        public double desiredCorrectionSpeedMetersPerSecond;
        public double finalCorrectionDeltaMeters;
        public double finalCorrectionSpeedMetersPerSecond;
        public bool finalCorrectionAccelerationAvailable;
        public double finalCorrectionAccelerationMetersPerSecondSquared;
        public bool finalCorrectionJerkAvailable;
        public double finalCorrectionJerkMetersPerSecondCubed;
        public double currentAnimatedSoleDeltaMeters;
        public double currentAnimatedSoleYDeltaMeters;
        public double currentAnimatedSoleYStepMeters;
        public double currentAnimatedSoleAlongUpDeltaMeters;
        public double currentAnimatedSoleSpeedMetersPerSecond;
        public double currentAnimatedSoleYSpeedMetersPerSecond;
        public bool finalPhysicalAnkleAvailable;
        public double finalPhysicalAnkleDeltaMeters;
        public double finalPhysicalAnkleYDeltaMeters;
        public double finalPhysicalAnkleYStepMeters;
        public double finalPhysicalAnkleAlongUpDeltaMeters;
        public double finalPhysicalAnkleSpeedMetersPerSecond;
        public double finalPhysicalAnkleYSpeedMetersPerSecond;
        public bool finalPhysicalAnkleYAccelerationAvailable;
        public double finalPhysicalAnkleYAccelerationMetersPerSecondSquared;
        public bool finalPhysicalAnkleYJerkAvailable;
        public double finalPhysicalAnkleYJerkMetersPerSecondCubed;
        public bool finalPhysicalAnkleAccelerationAvailable;
        public double finalPhysicalAnkleAccelerationMetersPerSecondSquared;
        public bool finalPhysicalAnkleJerkAvailable;
        public double finalPhysicalAnkleJerkMetersPerSecondCubed;
        public bool finalPhysicalSoleAvailable;
        public double finalPhysicalSoleDeltaMeters;
        public double finalPhysicalSoleYDeltaMeters;
        public double finalPhysicalSoleYStepMeters;
        public double finalPhysicalSoleAlongUpDeltaMeters;
        public double finalPhysicalSoleSpeedMetersPerSecond;
        public double finalPhysicalSoleYSpeedMetersPerSecond;
        public double safetyFloorClampMeters;
        public bool pathResidualRebuilt;
        public string pathRevisionReason;
        public bool lowPresentationSampling;
        public bool largePerFrameDisplacement;
        public bool speedAnomaly;
        public bool lowPresentationSamplingLargeStep;
        public string largeStepExplanation;
        [Newtonsoft.Json.JsonIgnore]
        internal UnityEngine.Vector3 observedSwingTargetDeltaVector;
        [Newtonsoft.Json.JsonIgnore]
        internal UnityEngine.Vector3 desiredCorrectionDeltaVector;
        [Newtonsoft.Json.JsonIgnore]
        internal UnityEngine.Vector3 finalCorrectionDeltaVector;
        [Newtonsoft.Json.JsonIgnore]
        internal UnityEngine.Vector3 currentAnimatedSoleDeltaVector;
        [Newtonsoft.Json.JsonIgnore]
        internal UnityEngine.Vector3 finalPhysicalAnkleDeltaVector;
        [Newtonsoft.Json.JsonIgnore]
        internal UnityEngine.Vector3 finalPhysicalSoleDeltaVector;
    }

    [Serializable]
    internal sealed class CharacterFootSameGroundPathSwingEnvelopeStepAnalysis
    {
        public int previousFrame;
        public int frame;
        public string side;
        public string landingEventIdentity;
        public string formalSourceIdentity;
        public int formalSourceCycle;
        public string previousContributionContinuityIdentity;
        public string contributionContinuityIdentity;
        public string groundPathInputIdentity;
        public string footMotionGroundPathInputIdentity;
        public double presentationDeltaSeconds;
        public ulong bodyTickSpan;
        public double previousProgress;
        public double progress;
        public double progressDelta;
        public CharacterFootVectorFact componentUp;
        public CharacterFootVectorFact previousEnvelopeSample;
        public CharacterFootVectorFact envelopeSample;
        public double previousEnvelopeSampleAlongUpMeters;
        public double envelopeSampleAlongUpMeters;
        public double envelopeSampleAlongUpDeltaMeters;
        public CharacterFootVectorFact previousAnimatedSole;
        public CharacterFootVectorFact animatedSole;
        public double previousAnimatedSoleAlongUpMeters;
        public double animatedSoleAlongUpMeters;
        public double animatedSoleAlongUpDeltaMeters;
        public bool formalFootHeightAvailable;
        public double previousFormalFootHeightMeters;
        public double formalFootHeightMeters;
        public double previousFormalTargetSoleHeightMeters;
        public double formalTargetSoleHeightMeters;
        public double previousEnvelopeConstraintMeters;
        public double envelopeConstraintMeters;
        public double envelopeConstraintDeltaMeters;
        public double previousFormalHeightConstraintMeters;
        public double formalHeightConstraintMeters;
        public double formalHeightConstraintDeltaMeters;
        public double desiredCorrectionAlongUpDeltaMeters;
        public double finalCorrectionAlongUpDeltaMeters;
        public bool physicalAnkleAvailable;
        public double physicalAnkleAlongUpDeltaMeters;
        public bool physicalSoleAvailable;
        public double physicalSoleAlongUpDeltaMeters;
        public double previousSafetyFloorClampMeters;
        public double safetyFloorClampMeters;
        public double safetyFloorClampDeltaMeters;
        public double safetyFloorOutputAlongUpDeltaMeters;
        public bool pathResidualRebuilt;
        public string pathRevisionReason;
        public bool causalChainMatched;
        public string classification;
    }
}
