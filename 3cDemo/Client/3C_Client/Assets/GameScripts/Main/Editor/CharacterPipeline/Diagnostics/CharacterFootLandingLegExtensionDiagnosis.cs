using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    internal sealed class CharacterFootLandingLegExtensionDiagnosis : ICharacterFootDiagnosis
    {
        const double ExtensionDelta = 0.02d;
        const double BendDropDegrees = 5d;

        public string DiagnosticId => "landing-leg-extension";
        public string FileName => "landing-leg-extension.json";

        public CharacterFootDiagnosisDocument Build(CharacterFootDiagnosisContext context)
        {
            List<JObject> events = context.Events("Landing");
            CharacterFootDiagnosisTarget target = context.Target(
                DiagnosticId,
                "Landing阶段是否出现腿继续伸直或弯曲方向反转",
                new[] { "Landing" },
                new[]
                {
                    "targetExtensionRatioDelta>0.02",
                    "solvedBendDropDegrees>5",
                    "bendDirectionReversed=true"
                },
                events,
                value =>
                {
                    var rules = new List<string>(3);
                    if (CharacterFootDiagnosisContext.Metric(
                            value,
                            "targetExtensionRatioDelta") > ExtensionDelta)
                    {
                        rules.Add("targetExtensionRatioDelta>0.02");
                    }
                    if (CharacterFootDiagnosisContext.Metric(
                            value,
                            "solvedBendDropDegrees") > BendDropDegrees)
                    {
                        rules.Add("solvedBendDropDegrees>5");
                    }
                    if (CharacterFootDiagnosisContext.Evidence(
                            value,
                            "bendDirectionReversed"))
                    {
                        rules.Add("bendDirectionReversed=true");
                    }
                    return rules;
                },
                value => Math.Max(
                    Math.Max(
                        CharacterFootDiagnosisContext.Metric(
                            value,
                            "targetExtensionRatioDelta") / ExtensionDelta,
                        CharacterFootDiagnosisContext.Metric(
                            value,
                            "solvedBendDropDegrees") / BendDropDegrees),
                    CharacterFootDiagnosisContext.Evidence(
                        value,
                        "bendDirectionReversed")
                        ? 1d
                        : 0d),
                "targetExtensionRatioDelta",
                "originalExtensionRatioPeak",
                "targetExtensionRatioPeak",
                "solvedBendDropDegrees",
                "solvedExtensionRatioPeak",
                "solvedBendDegreesMinimum",
                "originalCompressionReserveMinimumMeters",
                "targetCompressionReserveMinimumMeters",
                "solvedCompressionReserveMinimumMeters",
                "landingReachMinimumCorrectionMeters",
                "landingReachSignedCorrectionAlongUpMeters",
                "landingReachRuntimeGoalClampDistanceMeters",
                "landingReachLegLengthMeters",
                "landingReachUsableLegLengthMeters",
                "landingReachMinimumAlongUpMeters",
                "landingReachMaximumAlongUpMeters");
            target.scorePolicy = "Health";
            CharacterFootDiagnosisDocument document = context.Document(
                DiagnosticId,
                target);
            document.landingReach = CharacterFootLandingReachReport.Create(
                context.LandingReaches(),
                events);
            document.pelvisHeightTargets = context.PelvisHeightTargets();
            return document;
        }
    }

    [Serializable]
    internal sealed class CharacterFootPelvisHeightTargetObservation
    {
        public int frame;
        public string completionIdentity;
        public string strideState;
        public bool available;
        public string inputStage = "PreFootReachWeightedGoalSole";
        public CharacterFootVectorFact componentUp;
        public CharacterFootVectorFact leftAnimatedSole;
        public CharacterFootVectorFact rightAnimatedSole;
        public CharacterFootVectorFact leftTargetSole;
        public CharacterFootVectorFact rightTargetSole;
        public double? animatedMinimumAlongUp;
        public double? targetMinimumAlongUp;
        public double? requestedOffsetAlongUp;
    }

    [Serializable]
    internal sealed class CharacterFootLandingReachReport
    {
        public double candidateCompressionReserveMeters;
        public int factCount;
        public int availableFactCount;
        public SortedDictionary<string, int> availabilityCounts;
        public SortedDictionary<string, int> classificationCounts;
        public CharacterFootDiagnosisDistribution
            minimumCorrectionDistributionMeters;
        public CharacterFootDiagnosisDistribution
            originalExtensionRatioDistribution;
        public CharacterFootDiagnosisDistribution
            targetExtensionRatioDistribution;
        public CharacterFootDiagnosisDistribution
            solvedExtensionRatioDistribution;
        public CharacterFootDiagnosisDistribution
            originalCompressionReserveDistributionMeters;
        public CharacterFootDiagnosisDistribution
            targetCompressionReserveDistributionMeters;
        public CharacterFootDiagnosisDistribution
            solvedCompressionReserveDistributionMeters;
        public CharacterFootDiagnosisDistribution
            runtimeGoalClampDistributionMeters;
        public List<CharacterFootLandingReachRepresentative>
            representativeEvents;

        internal static CharacterFootLandingReachReport Create(
            List<JObject> facts,
            List<JObject> landingEvents)
        {
            List<CharacterFootLandingReachObservation> observations = facts
                .Select(CharacterFootLandingReachObservation.From)
                .ToList();
            var byFrame = observations.ToDictionary(
                value => (value.frame, value.side));
            var representatives =
                new List<CharacterFootLandingReachRepresentative>();
            foreach (JObject landingEvent in landingEvents)
            {
                int frame = landingEvent.Value<int?>("peakFrame") ?? 0;
                string side = landingEvent.Value<string>("side") ??
                              string.Empty;
                if (!byFrame.TryGetValue(
                        (frame, side),
                        out CharacterFootLandingReachObservation observation))
                {
                    continue;
                }
                representatives.Add(
                    CharacterFootLandingReachRepresentative.From(
                        landingEvent,
                        observation));
            }
            return new CharacterFootLandingReachReport
            {
                candidateCompressionReserveMeters = observations.Count > 0
                    ? observations[0].candidateCompressionReserveMeters
                    : 0.02d,
                factCount = observations.Count,
                availableFactCount = observations.Count(
                    value => value.landingReachAvailable),
                availabilityCounts = Counts(
                    observations.Select(value => value.availability)),
                classificationCounts = Counts(
                    observations.Select(value => value.classification)),
                minimumCorrectionDistributionMeters =
                    CharacterFootDiagnosisDistribution.Create(
                        observations
                            .Where(value => value.landingReachAvailable)
                            .Select(value => value.minimumCorrectionMeters)
                            .ToList()),
                originalExtensionRatioDistribution =
                    CharacterFootDiagnosisDistribution.Create(
                        observations
                            .Where(value => value.finalIkLegAvailable)
                            .Select(value => value.originalExtensionRatio)
                            .ToList()),
                targetExtensionRatioDistribution =
                    CharacterFootDiagnosisDistribution.Create(
                        observations
                            .Where(value => value.finalIkLegAvailable)
                            .Select(value => value.targetExtensionRatio)
                            .ToList()),
                solvedExtensionRatioDistribution =
                    CharacterFootDiagnosisDistribution.Create(
                        observations
                            .Where(value => value.finalIkLegAvailable)
                            .Select(value => value.solvedExtensionRatio)
                            .ToList()),
                originalCompressionReserveDistributionMeters =
                    CharacterFootDiagnosisDistribution.Create(
                        observations
                            .Where(value => value.finalIkLegAvailable)
                            .Select(value =>
                                value.originalCompressionReserveMeters)
                            .ToList()),
                targetCompressionReserveDistributionMeters =
                    CharacterFootDiagnosisDistribution.Create(
                        observations
                            .Where(value => value.finalIkLegAvailable)
                            .Select(value =>
                                value.actualTargetCompressionReserveMeters)
                            .ToList()),
                solvedCompressionReserveDistributionMeters =
                    CharacterFootDiagnosisDistribution.Create(
                        observations
                            .Where(value => value.finalIkLegAvailable)
                            .Select(value =>
                                value.solvedCompressionReserveMeters)
                            .ToList()),
                runtimeGoalClampDistributionMeters =
                    CharacterFootDiagnosisDistribution.Create(
                        observations
                            .Where(value => value.runtimeReachEvaluated)
                            .Select(value =>
                                value.runtimeGoalClampDistanceMeters)
                            .ToList()),
                representativeEvents = representatives
                    .OrderBy(value => value.startFrame)
                    .ThenBy(value => value.side, StringComparer.Ordinal)
                    .ToList()
            };
        }

        static SortedDictionary<string, int> Counts(
            IEnumerable<string> values) =>
            new SortedDictionary<string, int>(
                values
                    .GroupBy(value => value ?? string.Empty)
                    .ToDictionary(
                        value => value.Key,
                        value => value.Count(),
                        StringComparer.Ordinal),
                StringComparer.Ordinal);
    }

    [Serializable]
    internal sealed class CharacterFootLandingReachRepresentative
    {
        public int startFrame;
        public int endFrame;
        public int peakFrame;
        public string side;
        public string eventIdentity;
        public string sourceIdentity;
        public int sourceCycle;
        public CharacterFootLandingReachObservation landingReach;

        internal static CharacterFootLandingReachRepresentative From(
            JObject landingEvent,
            CharacterFootLandingReachObservation observation) =>
            new CharacterFootLandingReachRepresentative
            {
                startFrame = landingEvent.Value<int?>("startFrame") ?? 0,
                endFrame = landingEvent.Value<int?>("endFrame") ?? 0,
                peakFrame = landingEvent.Value<int?>("peakFrame") ?? 0,
                side = landingEvent.Value<string>("side") ?? string.Empty,
                eventIdentity = landingEvent.Value<string>("eventIdentity") ??
                                "0",
                sourceIdentity = landingEvent.Value<string>("sourceIdentity") ??
                                 string.Empty,
                sourceCycle = landingEvent.Value<int?>("sourceCycle") ?? 0,
                landingReach = observation
            };
    }

    [Serializable]
    internal sealed class CharacterFootLandingReachObservation
    {
        public int frame;
        public string side;
        public string availability;
        public string classification;
        public double candidateCompressionReserveMeters;
        public bool finalIkLegAvailable;
        public CharacterFootLandingReachVector3 componentUp;
        public CharacterFootLandingReachVector3 originalHip;
        public CharacterFootLandingReachVector3 originalKnee;
        public CharacterFootLandingReachVector3 originalAnkle;
        public CharacterFootLandingReachVector3 targetAnkle;
        public CharacterFootLandingReachVector3 baselineHipBeforePelvisOutput;
        public double appliedPelvisGoalAlongUpMeters;
        public double upperLegLengthMeters;
        public double lowerLegLengthMeters;
        public double legLengthMeters;
        public double landingUsableLegLengthMeters;
        public double hipTargetHorizontalDistanceMeters;
        public double hipTargetVerticalAlongUpMeters;
        public bool landingReachAvailable;
        public double landingReachMinimumAlongUpMeters;
        public double landingReachMaximumAlongUpMeters;
        public double strideSpringOutputMeters;
        public bool currentOutputWithinLandingReach;
        public double minimumCorrectionMeters;
        public double signedCorrectionAlongUpMeters;
        public string correctionDirection;
        public double originalExtensionRatio;
        public double targetExtensionRatio;
        public double solvedExtensionRatio;
        public double originalCompressionReserveMeters;
        public double actualTargetCompressionReserveMeters;
        public double solvedCompressionReserveMeters;
        public bool runtimeReachEvaluated;
        public bool runtimeReachAvailable;
        public bool runtimeGoalClamped;
        public double runtimeGoalClampDistanceMeters;
        public bool resolvedReachRequestAvailable;
        public string resolvedReachEventIdentity;
        public double resolvedReachLegLengthMeters;
        public double resolvedReachMinimumCompressionReserveMeters;
        public bool primarySupportAvailable;
        public string primarySupportSide;
        public string primarySupportLandingEventIdentity;
        public string strideState;
        public string strideSupportSide;
        public bool supportReachAvailable;
        public double supportReachMinimumAlongUpMeters;
        public double supportReachMaximumAlongUpMeters;
        public bool supportIntersectionExists;
        public double intersectionMinimumAlongUpMeters;
        public double intersectionMaximumAlongUpMeters;
        public double supportConflictGapMeters;

        internal static CharacterFootLandingReachObservation From(
            JObject value) =>
            new CharacterFootLandingReachObservation
            {
                frame = value.Value<int>("frame"),
                side = value.Value<string>("side") ?? string.Empty,
                availability = value.Value<string>("availability") ??
                               string.Empty,
                classification = value.Value<string>("classification") ??
                                 string.Empty,
                candidateCompressionReserveMeters =
                    value.Value<double>(
                        "candidateCompressionReserveMeters"),
                finalIkLegAvailable =
                    value.Value<bool>("finalIkLegAvailable"),
                componentUp = CharacterFootLandingReachVector3.From(
                    value["componentUp"] as JObject),
                originalHip = CharacterFootLandingReachVector3.From(
                    value["originalHip"] as JObject),
                originalKnee = CharacterFootLandingReachVector3.From(
                    value["originalKnee"] as JObject),
                originalAnkle = CharacterFootLandingReachVector3.From(
                    value["originalAnkle"] as JObject),
                targetAnkle = CharacterFootLandingReachVector3.From(
                    value["targetAnkle"] as JObject),
                baselineHipBeforePelvisOutput =
                    CharacterFootLandingReachVector3.From(
                        value["baselineHipBeforePelvisOutput"] as JObject),
                appliedPelvisGoalAlongUpMeters =
                    value.Value<double>(
                        "appliedPelvisGoalAlongUpMeters"),
                upperLegLengthMeters =
                    value.Value<double>("upperLegLengthMeters"),
                lowerLegLengthMeters =
                    value.Value<double>("lowerLegLengthMeters"),
                legLengthMeters = value.Value<double>("legLengthMeters"),
                landingUsableLegLengthMeters =
                    value.Value<double>("landingUsableLegLengthMeters"),
                hipTargetHorizontalDistanceMeters =
                    value.Value<double>(
                        "hipTargetHorizontalDistanceMeters"),
                hipTargetVerticalAlongUpMeters =
                    value.Value<double>("hipTargetVerticalAlongUpMeters"),
                landingReachAvailable =
                    value.Value<bool>("landingReachAvailable"),
                landingReachMinimumAlongUpMeters =
                    value.Value<double>(
                        "landingReachMinimumAlongUpMeters"),
                landingReachMaximumAlongUpMeters =
                    value.Value<double>(
                        "landingReachMaximumAlongUpMeters"),
                strideSpringOutputMeters =
                    value.Value<double>("strideSpringOutputMeters"),
                currentOutputWithinLandingReach =
                    value.Value<bool>(
                        "currentOutputWithinLandingReach"),
                minimumCorrectionMeters =
                    value.Value<double>("minimumCorrectionMeters"),
                signedCorrectionAlongUpMeters =
                    value.Value<double>(
                        "signedCorrectionAlongUpMeters"),
                correctionDirection =
                    value.Value<string>("correctionDirection") ??
                    string.Empty,
                originalExtensionRatio =
                    value.Value<double>("originalExtensionRatio"),
                targetExtensionRatio =
                    value.Value<double>("targetExtensionRatio"),
                solvedExtensionRatio =
                    value.Value<double>("solvedExtensionRatio"),
                originalCompressionReserveMeters =
                    value.Value<double>(
                        "originalCompressionReserveMeters"),
                actualTargetCompressionReserveMeters =
                    value.Value<double>(
                        "actualTargetCompressionReserveMeters"),
                solvedCompressionReserveMeters =
                    value.Value<double>(
                        "solvedCompressionReserveMeters"),
                runtimeReachEvaluated =
                    value.Value<bool>("runtimeReachEvaluated"),
                runtimeReachAvailable =
                    value.Value<bool>("runtimeReachAvailable"),
                runtimeGoalClamped =
                    value.Value<bool>("runtimeGoalClamped"),
                runtimeGoalClampDistanceMeters =
                    value.Value<double>(
                        "runtimeGoalClampDistanceMeters"),
                resolvedReachRequestAvailable =
                    value.Value<bool>("resolvedReachRequestAvailable"),
                resolvedReachEventIdentity =
                    value.Value<string>("resolvedReachEventIdentity") ??
                    "0",
                resolvedReachLegLengthMeters =
                    value.Value<double>("resolvedReachLegLengthMeters"),
                resolvedReachMinimumCompressionReserveMeters =
                    value.Value<double>(
                        "resolvedReachMinimumCompressionReserveMeters"),
                primarySupportAvailable =
                    value.Value<bool>("primarySupportAvailable"),
                primarySupportSide =
                    value.Value<string>("primarySupportSide") ??
                    string.Empty,
                primarySupportLandingEventIdentity =
                    value.Value<string>(
                        "primarySupportLandingEventIdentity") ?? "0",
                strideState = value.Value<string>("strideState") ??
                              string.Empty,
                strideSupportSide =
                    value.Value<string>("strideSupportSide") ??
                    string.Empty,
                supportReachAvailable =
                    value.Value<bool>("supportReachAvailable"),
                supportReachMinimumAlongUpMeters =
                    value.Value<double>(
                        "supportReachMinimumAlongUpMeters"),
                supportReachMaximumAlongUpMeters =
                    value.Value<double>(
                        "supportReachMaximumAlongUpMeters"),
                supportIntersectionExists =
                    value.Value<bool>("supportIntersectionExists"),
                intersectionMinimumAlongUpMeters =
                    value.Value<double>(
                        "intersectionMinimumAlongUpMeters"),
                intersectionMaximumAlongUpMeters =
                    value.Value<double>(
                        "intersectionMaximumAlongUpMeters"),
                supportConflictGapMeters =
                    value.Value<double>("supportConflictGapMeters")
            };
    }

    [Serializable]
    internal sealed class CharacterFootLandingReachVector3
    {
        public double x;
        public double y;
        public double z;

        internal static CharacterFootLandingReachVector3 From(JObject value)
        {
            value ??= new JObject();
            return new CharacterFootLandingReachVector3
            {
                x = value.Value<double>("x"),
                y = value.Value<double>("y"),
                z = value.Value<double>("z")
            };
        }
    }
}
