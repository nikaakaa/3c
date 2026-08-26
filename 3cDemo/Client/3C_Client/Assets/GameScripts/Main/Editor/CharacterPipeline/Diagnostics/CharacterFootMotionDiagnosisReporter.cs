using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    internal readonly struct CharacterFootMotionDiagnosisReport
    {
        internal CharacterFootMotionDiagnosisReport(
            string path,
            int targetCount,
            int matchCount)
        {
            Path = path ?? string.Empty;
            TargetCount = targetCount;
            MatchCount = matchCount;
        }

        internal string Path { get; }
        internal int TargetCount { get; }
        internal int MatchCount { get; }
    }

    internal static class CharacterFootMotionDiagnosisReporter
    {
        const string Schema = "character-foot-motion-diagnosis/3";
        const string ReporterId = "character-foot-motion-diagnosis-reporter";
        const int ReporterVersion = 3;
        const int RepresentativeEventLimit = 8;
        const double LandingExtensionDeltaThreshold = 0.02d;
        const double LandingBendDropThresholdDegrees = 5d;
        const double LockedSinkThresholdMeters = 0.005d;
        const double LockedDriftThresholdMeters = 0.01d;
        const double PathCorrectionStepThresholdMeters = 0.02d;
        const double LockAcquireCorrectionStepThresholdMeters = 0.01d;
        const double ReleaseExcursionThresholdMeters = 0.01d;

        internal static CharacterFootMotionDiagnosisReport Create(
            string factsPath)
        {
            if (string.IsNullOrWhiteSpace(factsPath) || !File.Exists(factsPath))
            {
                throw new FileNotFoundException(
                    "Foot Motion facts file is unavailable.",
                    factsPath);
            }
            string fullFactsPath = Path.GetFullPath(factsPath);
            if (!string.Equals(
                    Path.GetFileName(fullFactsPath),
                    "facts.json",
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Foot Motion diagnosis input must be facts.json.");
            }
            JObject facts = JObject.Parse(File.ReadAllText(
                fullFactsPath,
                Encoding.UTF8));
            JArray events = facts["events"] as JArray ?? new JArray();
            var targets = new List<DiagnosisTarget>
            {
                BuildLandingExtension(events),
                BuildLockedMotion(events),
                BuildPathChangeJitter(events),
                BuildLockTransition(events),
                BuildSourceContactPlanePenetration(events),
                BuildIntroducedContactPlanePenetration(events),
                BuildAmplifiedContactPlanePenetration(events),
                BuildUnresolvedToeContactPlanePenetration(events),
                BuildFinalHeelContactPlanePenetration(events)
            };
            var document = new DiagnosisDocument
            {
                schema = Schema,
                facts = new FactsReference
                {
                    file = Path.GetFileName(fullFactsPath),
                    sha256 = ComputeSha256(fullFactsPath),
                    schema = facts.Value<string>("schema") ?? string.Empty,
                    sampleIdentity = facts["sample"]?.Value<string>("identity") ?? string.Empty
                },
                reporter = new ReporterFact
                {
                    id = ReporterId,
                    version = ReporterVersion,
                    representativeEventLimit = RepresentativeEventLimit
                },
                penetrationCoverage = BuildPenetrationCoverage(facts),
                targets = targets,
                summary = new DiagnosisSummary
                {
                    targetCount = targets.Count,
                    targetWithMatchesCount = targets.Count(value => value.matchedEventCount > 0),
                    matchedEventCount = targets.Sum(value => value.matchedEventCount)
                }
            };
            string reportPath = Path.Combine(
                Path.GetDirectoryName(fullFactsPath) ?? string.Empty,
                "diagnosis.json");
            Publish(reportPath, document);
            return new CharacterFootMotionDiagnosisReport(
                reportPath,
                document.summary.targetCount,
                document.summary.matchedEventCount);
        }

        static DiagnosisTarget BuildLandingExtension(JArray events)
        {
            List<JObject> eligible = Events(events, "Landing");
            List<DiagnosisEvidence> matched = Match(
                eligible,
                value =>
                {
                    var rules = new List<string>(3);
                    if (Metric(value, "targetExtensionRatioDelta") >
                        LandingExtensionDeltaThreshold)
                    {
                        rules.Add("targetExtensionRatioDelta>0.02");
                    }
                    if (Metric(value, "solvedBendDropDegrees") >
                        LandingBendDropThresholdDegrees)
                    {
                        rules.Add("solvedBendDropDegrees>5");
                    }
                    if (Evidence(value, "bendDirectionReversed"))
                        rules.Add("bendDirectionReversed=true");
                    return rules;
                });
            return Target(
                "landing-leg-extension",
                "Landing阶段是否出现腿继续伸直或弯曲方向反转",
                new[] { "Landing" },
                new[]
                {
                    "targetExtensionRatioDelta>0.02",
                    "solvedBendDropDegrees>5",
                    "bendDirectionReversed=true"
                },
                eligible,
                matched,
                value => Math.Max(
                    Math.Max(
                        Metric(value, "targetExtensionRatioDelta") /
                        LandingExtensionDeltaThreshold,
                        Metric(value, "solvedBendDropDegrees") /
                        LandingBendDropThresholdDegrees),
                    Evidence(value, "bendDirectionReversed") ? 1d : 0d),
                "targetExtensionRatioDelta",
                "solvedBendDropDegrees",
                "solvedExtensionRatioPeak",
                "solvedBendDegreesMinimum");
        }

        static DiagnosisTarget BuildLockedMotion(JArray events)
        {
            List<JObject> eligible = Events(events, "Locked");
            List<DiagnosisEvidence> matched = Match(
                eligible,
                value =>
                {
                    var rules = new List<string>(2);
                    if (Metric(value, "soleDownwardExcursionMeters") >
                        LockedSinkThresholdMeters)
                    {
                        rules.Add("soleDownwardExcursionMeters>0.005");
                    }
                    if (Evidence(value, "anchorStable") &&
                        Metric(value, "correctedSoleAnchorDistanceChangeMeters") >
                        LockedDriftThresholdMeters)
                    {
                        rules.Add("anchorStable=true&&correctedSoleAnchorDistanceChangeMeters>0.01");
                    }
                    return rules;
                });
            return Target(
                "locked-sole-sink-or-drift",
                "Locked阶段脚底是否相对稳定Anchor下陷或漂移",
                new[] { "Locked" },
                new[]
                {
                    "soleDownwardExcursionMeters>0.005",
                    "anchorStable=true&&correctedSoleAnchorDistanceChangeMeters>0.01"
                },
                eligible,
                matched,
                value => Math.Max(
                    Metric(value, "soleDownwardExcursionMeters") /
                    LockedSinkThresholdMeters,
                    Metric(value, "correctedSoleAnchorDistanceChangeMeters") /
                    LockedDriftThresholdMeters),
                "soleDownwardExcursionMeters",
                "correctedSoleAnchorDistanceChangeMeters",
                "visibleSoleStepMaximumMeters",
                "anchorDisplacementMeters");
        }

        static DiagnosisTarget BuildPathChangeJitter(JArray events)
        {
            List<JObject> eligible = Events(events, "PathChange");
            List<DiagnosisEvidence> matched = Match(
                eligible,
                value =>
                {
                    var rules = new List<string>(1);
                    if (Metric(value, "correctionStepMaximumMeters") >
                        PathCorrectionStepThresholdMeters)
                    {
                        rules.Add("correctionStepMaximumMeters>0.02");
                    }
                    return rules;
                });
            return Target(
                "path-change-correction-jump",
                "Ground Path变化附近是否出现脚部修正跳变",
                new[] { "PathChange" },
                new[] { "correctionStepMaximumMeters>0.02" },
                eligible,
                matched,
                value => Metric(value, "correctionStepMaximumMeters") /
                         PathCorrectionStepThresholdMeters,
                "nextLandingEndpointDeltaMeters",
                "correctionStepMaximumMeters",
                "correctionExcursionMeters",
                "correctionJerkMetersPerSecondCubed");
        }

        static DiagnosisTarget BuildLockTransition(JArray events)
        {
            List<JObject> eligible = Events(events, "Landing", "Release");
            List<DiagnosisEvidence> matched = Match(
                eligible,
                value =>
                {
                    var rules = new List<string>(2);
                    string kind = value.Value<string>("kind") ?? string.Empty;
                    if (kind == "Landing" &&
                        Metric(value, "correctionStepMaximumMeters") >
                        LockAcquireCorrectionStepThresholdMeters)
                    {
                        rules.Add("Landing.correctionStepMaximumMeters>0.01");
                    }
                    if (kind == "Release" &&
                        Metric(value, "velocityDirectionReversalCount") > 0d &&
                        Metric(value, "correctionExcursionMeters") >
                        ReleaseExcursionThresholdMeters)
                    {
                        rules.Add("Release.velocityDirectionReversalCount>0&&correctionExcursionMeters>0.01");
                    }
                    return rules;
                });
            return Target(
                "lock-transition-flyback",
                "进入或退出Lock时是否出现突跳后反向回拉",
                new[] { "Landing", "Release" },
                new[]
                {
                    "Landing.correctionStepMaximumMeters>0.01",
                    "Release.velocityDirectionReversalCount>0&&correctionExcursionMeters>0.01"
                },
                eligible,
                matched,
                value => value.eventKind == "Landing"
                    ? Metric(value, "correctionStepMaximumMeters") /
                      LockAcquireCorrectionStepThresholdMeters
                    : Math.Max(
                        Metric(value, "correctionExcursionMeters") /
                        ReleaseExcursionThresholdMeters,
                        Metric(value, "velocityDirectionReversalCount")),
                "correctionStepMaximumMeters",
                "correctionExcursionMeters",
                "velocityDirectionReversalCount");
        }

        static DiagnosisTarget BuildSourceContactPlanePenetration(JArray events)
        {
            List<JObject> eligible = Events(events, "ContactPlanePenetration");
            List<DiagnosisEvidence> matched = Match(
                eligible,
                value => Metric(value, "sourceDepthMaximumMeters") >
                         CharacterFootContactPlanePenetration.GeometryEpsilonMeters
                    ? new List<string> { "sourceDepthMaximumMeters>0.00001" }
                    : new List<string>());
            return Target(
                "source-contact-plane-penetration",
                "Foot Placement处理前的Heel-Toe接触线是否已进入正式接触平面",
                new[] { "ContactPlanePenetration" },
                new[] { "sourceDepthMaximumMeters>0.00001" },
                eligible,
                matched,
                value => Metric(value, "sourceDepthMaximumMeters"),
                "sourceHeelDepthMaximumMeters",
                "sourceToeDepthMaximumMeters",
                "sourceDepthMaximumMeters",
                "sourceLengthCoefficientMaximum");
        }

        static DiagnosisTarget BuildIntroducedContactPlanePenetration(JArray events)
        {
            List<JObject> eligible = Events(events, "ContactPlanePenetration");
            List<DiagnosisEvidence> matched = Match(
                eligible,
                value => Metric(value, "introducedDepthMaximumMeters") >
                         CharacterFootContactPlanePenetration.GeometryEpsilonMeters
                    ? new List<string> { "introducedDepthMaximumMeters>0.00001" }
                    : new List<string>());
            return Target(
                "foot-placement-introduced-contact-plane-penetration",
                "当前Foot Placement与最终IK是否新增接触平面侵入",
                new[] { "ContactPlanePenetration" },
                new[] { "introducedDepthMaximumMeters>0.00001" },
                eligible,
                matched,
                value => Metric(value, "introducedDepthMaximumMeters"),
                "introducedDepthMaximumMeters",
                "sourceDepthMaximumMeters",
                "finalDepthMaximumMeters",
                "introducedFrameCount");
        }

        static DiagnosisTarget BuildAmplifiedContactPlanePenetration(JArray events)
        {
            List<JObject> eligible = Events(events, "ContactPlanePenetration");
            List<DiagnosisEvidence> matched = Match(
                eligible,
                value => Metric(value, "amplifiedFrameCount") > 0d
                    ? new List<string> { "amplifiedFrameCount>0" }
                    : new List<string>());
            return Target(
                "foot-placement-amplified-contact-plane-penetration",
                "当前Foot Placement与最终IK是否加重动画源已有侵入",
                new[] { "ContactPlanePenetration" },
                new[] { "amplifiedFrameCount>0" },
                eligible,
                matched,
                value => Metric(value, "introducedDepthMaximumMeters"),
                "amplifiedFrameCount",
                "introducedDepthMaximumMeters",
                "sourceDepthMaximumMeters",
                "finalDepthMaximumMeters");
        }

        static DiagnosisTarget BuildUnresolvedToeContactPlanePenetration(JArray events)
        {
            List<JObject> eligible = Events(events, "ContactPlanePenetration");
            List<DiagnosisEvidence> matched = Match(
                eligible,
                value => Metric(value, "finalToeDepthMaximumMeters") >
                         CharacterFootContactPlanePenetration.GeometryEpsilonMeters
                    ? new List<string> { "finalToeDepthMaximumMeters>0.00001" }
                    : new List<string>());
            return Target(
                "unresolved-toe-contact-plane-penetration",
                "最终Toe接触探针是否仍进入接触平面，仅记录视觉残留不自动归责",
                new[] { "ContactPlanePenetration" },
                new[] { "finalToeDepthMaximumMeters>0.00001" },
                eligible,
                matched,
                value => Metric(value, "finalToeDepthMaximumMeters"),
                "sourceToeDepthMaximumMeters",
                "finalToeDepthMaximumMeters",
                "introducedDepthMaximumMeters",
                "baselineResidualFrameCount");
        }

        static DiagnosisTarget BuildFinalHeelContactPlanePenetration(JArray events)
        {
            List<JObject> eligible = Events(events, "ContactPlanePenetration");
            List<DiagnosisEvidence> matched = Match(
                eligible,
                value => Metric(value, "finalHeelDepthMaximumMeters") >
                         CharacterFootContactPlanePenetration.GeometryEpsilonMeters
                    ? new List<string> { "finalHeelDepthMaximumMeters>0.00001" }
                    : new List<string>());
            return Target(
                "final-heel-contact-plane-penetration",
                "最终Heel接触探针是否进入正式接触平面",
                new[] { "ContactPlanePenetration" },
                new[] { "finalHeelDepthMaximumMeters>0.00001" },
                eligible,
                matched,
                value => Metric(value, "finalHeelDepthMaximumMeters"),
                "sourceHeelDepthMaximumMeters",
                "finalHeelDepthMaximumMeters",
                "finalLengthCoefficientMaximum",
                "finalDepthTimeIntegralMeterSeconds");
        }

        static DiagnosisTarget Target(
            string id,
            string question,
            IEnumerable<string> eventKinds,
            IEnumerable<string> rules,
            List<JObject> eligible,
            List<DiagnosisEvidence> matched,
            Func<DiagnosisEvidence, double> rank,
            params string[] metricNames)
        {
            var measurements = new SortedDictionary<string, DistributionFact>(
                StringComparer.Ordinal);
            foreach (string metricName in metricNames)
            {
                List<double> values = eligible
                    .Select(value => MetricToken(value, metricName))
                    .Where(value => value.HasValue)
                    .Select(value => value.Value)
                    .ToList();
                measurements[metricName] = DistributionFact.Create(values);
            }
            return new DiagnosisTarget
            {
                id = id,
                question = question,
                eventKinds = eventKinds.ToList(),
                rules = rules.ToList(),
                eligibleEventCount = eligible.Count,
                matchedEventCount = matched.Count,
                matchedEventRate = eligible.Count > 0
                    ? (double)matched.Count / eligible.Count
                    : 0d,
                measurements = measurements,
                representativeEventCount = Math.Min(
                    RepresentativeEventLimit,
                    matched.Count),
                representativeEvents = matched
                    .OrderByDescending(rank)
                    .ThenBy(value => value.startFrame)
                    .Take(RepresentativeEventLimit)
                    .OrderBy(value => value.startFrame)
                    .ThenBy(value => value.side, StringComparer.Ordinal)
                    .ToList()
            };
        }

        static List<DiagnosisEvidence> Match(
            IEnumerable<JObject> events,
            Func<JObject, List<string>> matchRules)
        {
            var result = new List<DiagnosisEvidence>();
            foreach (JObject value in events)
            {
                List<string> rules = matchRules(value);
                if (rules.Count == 0)
                    continue;
                result.Add(new DiagnosisEvidence
                {
                    eventKind = value.Value<string>("kind") ?? string.Empty,
                    side = value.Value<string>("side") ?? string.Empty,
                    startFrame = value.Value<int?>("startFrame") ?? 0,
                    endFrame = value.Value<int?>("endFrame") ?? 0,
                    peakFrame = value.Value<int?>("peakFrame") ?? 0,
                    eventIdentity = value.Value<string>("eventIdentity") ?? string.Empty,
                    sourceIdentity = value.Value<string>("sourceIdentity") ?? string.Empty,
                    sourceCycle = value.Value<int?>("sourceCycle") ?? 0,
                    matchedRules = rules,
                    metrics = ReadDoubleMap(value["metrics"] as JObject),
                    evidence = ReadBoolMap(value["evidence"] as JObject)
                });
            }
            return result;
        }

        static List<JObject> Events(JArray events, params string[] kinds)
        {
            var accepted = new HashSet<string>(kinds, StringComparer.Ordinal);
            return events
                .OfType<JObject>()
                .Where(value => accepted.Contains(
                    value.Value<string>("kind") ?? string.Empty))
                .OrderBy(value => value.Value<int?>("startFrame") ?? 0)
                .ThenBy(value => value.Value<string>("side"), StringComparer.Ordinal)
                .ToList();
        }

        static double Metric(JObject value, string name) =>
            MetricToken(value, name) ?? 0d;

        static double? MetricToken(JObject value, string name) =>
            value["metrics"]?[name]?.Value<double?>();

        static bool Evidence(JObject value, string name) =>
            value["evidence"]?[name]?.Value<bool?>() ?? false;

        static double Metric(DiagnosisEvidence value, string name) =>
            value.metrics.TryGetValue(name, out double result) ? result : 0d;

        static bool Evidence(DiagnosisEvidence value, string name) =>
            value.evidence.TryGetValue(name, out bool result) && result;

        static SortedDictionary<string, double> ReadDoubleMap(JObject value)
        {
            var result = new SortedDictionary<string, double>(StringComparer.Ordinal);
            if (value == null)
                return result;
            foreach (JProperty property in value.Properties())
                result[property.Name] = property.Value.Value<double>();
            return result;
        }

        static SortedDictionary<string, bool> ReadBoolMap(JObject value)
        {
            var result = new SortedDictionary<string, bool>(StringComparer.Ordinal);
            if (value == null)
                return result;
            foreach (JProperty property in value.Properties())
                result[property.Name] = property.Value.Value<bool>();
            return result;
        }

        static PenetrationCoverageFact BuildPenetrationCoverage(JObject facts)
        {
            JObject coverage = facts["coverage"] as JObject;
            return new PenetrationCoverageFact
            {
                availableFootRowCount =
                    coverage?.Value<int?>("contactPlaneAvailableFootRowCount") ?? 0,
                unavailableFootRowCount =
                    coverage?.Value<int?>("contactPlaneUnavailableFootRowCount") ?? 0,
                availabilityReasons = ReadIntMap(
                    coverage?["contactPlanePenetrationAvailability"] as JObject)
            };
        }

        static SortedDictionary<string, int> ReadIntMap(JObject value)
        {
            var result = new SortedDictionary<string, int>(StringComparer.Ordinal);
            if (value == null)
                return result;
            foreach (JProperty property in value.Properties())
                result[property.Name] = property.Value.Value<int>();
            return result;
        }

        static void Publish(string path, DiagnosisDocument document)
        {
            string partPath = path + ".part";
            if (File.Exists(partPath))
                File.Delete(partPath);
            try
            {
                using (var stream = new FileStream(
                           partPath,
                           FileMode.CreateNew,
                           FileAccess.Write,
                           FileShare.Read,
                           65536,
                           FileOptions.SequentialScan))
                using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
                using (var json = new JsonTextWriter(writer)
                       {
                           Formatting = Formatting.Indented,
                           Culture = CultureInfo.InvariantCulture
                       })
                {
                    JsonSerializer serializer = JsonSerializer.Create(
                        new JsonSerializerSettings
                        {
                            Culture = CultureInfo.InvariantCulture,
                            NullValueHandling = NullValueHandling.Ignore
                        });
                    serializer.Serialize(json, document);
                    json.Flush();
                    writer.Flush();
                    stream.Flush(true);
                }
                if (File.Exists(path))
                    File.Replace(partPath, path, null);
                else
                    File.Move(partPath, path);
            }
            catch
            {
                if (File.Exists(partPath))
                    File.Delete(partPath);
                throw;
            }
        }

        static string ComputeSha256(string path)
        {
            using SHA256 sha = SHA256.Create();
            using FileStream stream = File.OpenRead(path);
            byte[] hash = sha.ComputeHash(stream);
            var builder = new StringBuilder(hash.Length * 2);
            for (int i = 0; i < hash.Length; i++)
                builder.Append(hash[i].ToString("x2", CultureInfo.InvariantCulture));
            return builder.ToString();
        }

        [Serializable]
        sealed class DiagnosisDocument
        {
            public string schema;
            public FactsReference facts;
            public ReporterFact reporter;
            public PenetrationCoverageFact penetrationCoverage;
            public List<DiagnosisTarget> targets;
            public DiagnosisSummary summary;
        }

        [Serializable]
        sealed class FactsReference
        {
            public string file;
            public string sha256;
            public string schema;
            public string sampleIdentity;
        }

        [Serializable]
        sealed class ReporterFact
        {
            public string id;
            public int version;
            public int representativeEventLimit;
        }

        [Serializable]
        sealed class PenetrationCoverageFact
        {
            public int availableFootRowCount;
            public int unavailableFootRowCount;
            public SortedDictionary<string, int> availabilityReasons;
        }

        [Serializable]
        sealed class DiagnosisSummary
        {
            public int targetCount;
            public int targetWithMatchesCount;
            public int matchedEventCount;
        }

        [Serializable]
        sealed class DiagnosisTarget
        {
            public string id;
            public string question;
            public List<string> eventKinds;
            public List<string> rules;
            public int eligibleEventCount;
            public int matchedEventCount;
            public double matchedEventRate;
            public SortedDictionary<string, DistributionFact> measurements;
            public int representativeEventCount;
            public List<DiagnosisEvidence> representativeEvents;
        }

        [Serializable]
        sealed class DiagnosisEvidence
        {
            public string eventKind;
            public string side;
            public int startFrame;
            public int endFrame;
            public int peakFrame;
            public string eventIdentity;
            public string sourceIdentity;
            public int sourceCycle;
            public List<string> matchedRules;
            public SortedDictionary<string, double> metrics;
            public SortedDictionary<string, bool> evidence;
        }

        [Serializable]
        sealed class DistributionFact
        {
            public int count;
            public double median;
            public double p90;
            public double p99;
            public double maximum;

            internal static DistributionFact Create(List<double> values)
            {
                values.Sort();
                return new DistributionFact
                {
                    count = values.Count,
                    median = Percentile(values, 0.5d),
                    p90 = Percentile(values, 0.9d),
                    p99 = Percentile(values, 0.99d),
                    maximum = values.Count > 0 ? values[^1] : 0d
                };
            }

            static double Percentile(List<double> values, double percentile)
            {
                if (values.Count == 0)
                    return 0d;
                double position = (values.Count - 1) * percentile;
                int lower = (int)Math.Floor(position);
                int upper = (int)Math.Ceiling(position);
                if (lower == upper)
                    return values[lower];
                double t = position - lower;
                return values[lower] + (values[upper] - values[lower]) * t;
            }
        }
    }
}
