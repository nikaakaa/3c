using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    internal interface ICharacterFootDiagnosis
    {
        string DiagnosticId { get; }
        string FileName { get; }
        CharacterFootDiagnosisDocument Build(CharacterFootDiagnosisContext context);
    }

    internal sealed class CharacterFootDiagnosisContext
    {
        const int RepresentativeEventLimit = 8;
        readonly JObject m_Facts;
        readonly JArray m_Events;

        internal CharacterFootDiagnosisContext(JObject facts)
        {
            m_Facts = facts ?? throw new ArgumentNullException(nameof(facts));
            m_Events = facts["events"] as JArray ?? new JArray();
        }

        internal List<JObject> Events(params string[] kinds)
        {
            var accepted = new HashSet<string>(kinds, StringComparer.Ordinal);
            return m_Events
                .OfType<JObject>()
                .Where(value => accepted.Contains(
                    value.Value<string>("kind") ?? string.Empty))
                .OrderBy(value => value.Value<int?>("startFrame") ?? 0)
                .ThenBy(value => value.Value<string>("side"), StringComparer.Ordinal)
                .ToList();
        }

        internal List<JObject> StepTimeCandidateSelections() =>
            (m_Facts["stepTimeCandidateSelections"] as JArray ??
             new JArray())
            .OfType<JObject>()
            .OrderBy(value => value.Value<int?>("frame") ?? 0)
            .ThenBy(
                value => value.Value<string>("side"),
                StringComparer.Ordinal)
            .ToList();

        internal CharacterFootDiagnosisTarget Target(
            string id,
            string question,
            IEnumerable<string> eventKinds,
            IEnumerable<string> rules,
            List<JObject> eligible,
            Func<JObject, List<string>> matchRules,
            Func<CharacterFootDiagnosisEvidence, double> rank,
            params string[] metricNames)
        {
            List<CharacterFootDiagnosisEvidence> matched = Match(
                eligible,
                matchRules);
            var measurements = new SortedDictionary<string, CharacterFootDiagnosisDistribution>(
                StringComparer.Ordinal);
            foreach (string metricName in metricNames)
            {
                List<double> values = eligible
                    .Select(value => MetricToken(value, metricName))
                    .Where(value => value.HasValue)
                    .Select(value => value.Value)
                    .ToList();
                measurements[metricName] =
                    CharacterFootDiagnosisDistribution.Create(values);
            }
            return new CharacterFootDiagnosisTarget
            {
                id = id,
                question = question,
                eventKinds = eventKinds.ToList(),
                rules = rules.ToList(),
                eligibleEventCount = eligible.Count,
                matchedEventCount = matched.Count,
                matchedEventRateAvailable = eligible.Count > 0,
                matchedEventRate = eligible.Count > 0
                    ? (double?)matched.Count / eligible.Count
                    : null,
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

        internal CharacterFootDiagnosisOccurrenceProfile Occurrence(
            string sampleUnit,
            string metricName,
            string thresholdUnit,
            List<JObject> eligible,
            double primaryThreshold,
            params double[] thresholds)
        {
            if (string.IsNullOrWhiteSpace(sampleUnit) ||
                string.IsNullOrWhiteSpace(metricName) ||
                string.IsNullOrWhiteSpace(thresholdUnit))
            {
                throw new ArgumentException(
                    "Foot diagnosis occurrence identity is invalid.");
            }
            if (thresholds == null || thresholds.Length == 0)
                throw new ArgumentException("Foot diagnosis thresholds are unavailable.");
            var configured = new List<double>(thresholds.Length);
            for (int i = 0; i < thresholds.Length; i++)
            {
                double threshold = thresholds[i];
                if (!double.IsFinite(threshold) ||
                    threshold <= 0d ||
                    i > 0 && threshold <= thresholds[i - 1])
                {
                    throw new ArgumentException(
                        "Foot diagnosis thresholds must be finite, positive, and ordered.");
                }
                configured.Add(threshold);
            }
            if (!double.IsFinite(primaryThreshold) ||
                !configured.Contains(primaryThreshold))
            {
                throw new ArgumentException(
                    "Foot diagnosis primary threshold is invalid.");
            }
            var profile = new CharacterFootDiagnosisOccurrenceProfile
            {
                available = eligible.Count > 0,
                sampleUnit = sampleUnit,
                metric = metricName,
                comparison = "GreaterThan",
                thresholdUnit = thresholdUnit,
                eligibleEventCount = eligible.Count,
                configuredThresholds = configured,
                rates = new List<CharacterFootDiagnosisOccurrenceRate>(
                    eligible.Count > 0 ? thresholds.Length : 0)
            };
            if (eligible.Count == 0)
                return profile;
            var values = new List<double>(eligible.Count);
            for (int i = 0; i < eligible.Count; i++)
            {
                double? value = MetricToken(eligible[i], metricName);
                if (!value.HasValue || !double.IsFinite(value.Value))
                {
                    throw new InvalidOperationException(
                        $"Foot diagnosis occurrence metric '{metricName}' is missing or non-finite.");
                }
                values.Add(value.Value);
            }
            for (int i = 0; i < thresholds.Length; i++)
            {
                double threshold = thresholds[i];
                int matched = values.Count(value => value > threshold);
                double rate = (double)matched / eligible.Count;
                if (!double.IsFinite(rate))
                    throw new InvalidOperationException(
                        "Foot diagnosis occurrence rate is non-finite.");
                var occurrenceRate = new CharacterFootDiagnosisOccurrenceRate
                {
                    threshold = threshold,
                    eligibleEventCount = eligible.Count,
                    matchedEventCount = matched,
                    matchedEventRate = rate
                };
                profile.rates.Add(occurrenceRate);
                if (threshold == primaryThreshold)
                    profile.primaryRate = occurrenceRate;
            }
            return profile;
        }

        internal CharacterFootDiagnosisDocument Document(
            string diagnosticId,
            params CharacterFootDiagnosisTarget[] targets)
        {
            JObject sample = m_Facts["sample"] as JObject;
            var list = targets.ToList();
            CharacterFootDiagnosisTarget primaryTarget = list.FirstOrDefault(
                value => value.occurrence != null);
            return new CharacterFootDiagnosisDocument
            {
                schema = "character-foot-diagnosis-file/3",
                diagnosticId = diagnosticId,
                facts = new CharacterFootDiagnosisFactsReference
                {
                    file = "facts.json",
                    schema = m_Facts.Value<string>("schema") ?? string.Empty,
                    sampleIdentity = sample?.Value<string>("identity") ?? string.Empty
                },
                coverage = diagnosticId == "contact-plane-penetration"
                    ? BuildPenetrationCoverage()
                    : null,
                targets = list,
                summary = new CharacterFootDiagnosisSummary
                {
                    primaryResult = BuildPrimaryResult(primaryTarget),
                    targetCount = list.Count,
                    targetWithMatchesCount = list.Count(
                        value => value.matchedEventCount > 0),
                    matchedEventCount = list.Sum(value => value.matchedEventCount)
                }
            };
        }

        static CharacterFootDiagnosisPrimaryResult BuildPrimaryResult(
            CharacterFootDiagnosisTarget target)
        {
            if (target?.occurrence == null)
                return null;
            if (!target.measurements.TryGetValue(
                    target.occurrence.metric,
                    out CharacterFootDiagnosisDistribution distribution))
            {
                throw new InvalidOperationException(
                    "Foot diagnosis primary occurrence distribution is unavailable.");
            }
            return new CharacterFootDiagnosisPrimaryResult
            {
                kind = "OccurrenceRateWithAmplitudeDistribution",
                targetId = target.id,
                occurrence = target.occurrence,
                pathStageAnalysis = target.pathStageAnalysis,
                amplitudeDistribution = distribution
            };
        }

        internal static double Metric(JObject value, string name) =>
            MetricToken(value, name) ?? 0d;

        internal static bool Evidence(JObject value, string name) =>
            value["evidence"]?[name]?.Value<bool?>() ?? false;

        internal static double Metric(
            CharacterFootDiagnosisEvidence value,
            string name) =>
            value.metrics.TryGetValue(name, out double result) ? result : 0d;

        internal static bool Evidence(
            CharacterFootDiagnosisEvidence value,
            string name) =>
            value.evidence.TryGetValue(name, out bool result) && result;

        static double? MetricToken(JObject value, string name) =>
            value["metrics"]?[name]?.Value<double?>();

        static List<CharacterFootDiagnosisEvidence> Match(
            IEnumerable<JObject> events,
            Func<JObject, List<string>> matchRules)
        {
            var result = new List<CharacterFootDiagnosisEvidence>();
            foreach (JObject value in events)
            {
                List<string> rules = matchRules(value);
                if (rules.Count == 0)
                    continue;
                result.Add(new CharacterFootDiagnosisEvidence
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

        CharacterFootDiagnosisCoverage BuildPenetrationCoverage()
        {
            JObject coverage = m_Facts["coverage"] as JObject;
            return new CharacterFootDiagnosisCoverage
            {
                availableFootRowCount =
                    coverage?.Value<int?>("contactPlaneAvailableFootRowCount") ?? 0,
                unavailableFootRowCount =
                    coverage?.Value<int?>("contactPlaneUnavailableFootRowCount") ?? 0,
                unavailableReasons = ReadIntMap(
                    coverage?["contactPlanePenetrationAvailability"] as JObject)
            };
        }

        static SortedDictionary<string, double> ReadDoubleMap(JObject value)
        {
            var result = new SortedDictionary<string, double>(StringComparer.Ordinal);
            if (value == null)
                return result;
            foreach (JProperty property in value.Properties())
            {
                double metric = property.Value.Value<double>();
                if (!double.IsFinite(metric))
                    throw new InvalidOperationException(
                        $"Foot diagnosis evidence metric '{property.Name}' is non-finite.");
                result[property.Name] = metric;
            }
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

        static SortedDictionary<string, int> ReadIntMap(JObject value)
        {
            var result = new SortedDictionary<string, int>(StringComparer.Ordinal);
            if (value == null)
                return result;
            foreach (JProperty property in value.Properties())
                result[property.Name] = property.Value.Value<int>();
            return result;
        }
    }

    [Serializable]
    internal sealed class CharacterFootDiagnosisDocument
    {
        public string schema;
        public string diagnosticId;
        public CharacterFootDiagnosisFactsReference facts;
        public CharacterFootDiagnosisCoverage coverage;
        public List<CharacterFootDiagnosisTarget> targets;
        public CharacterFootDiagnosisSummary summary;
        public CharacterFootStepTimeCandidateSelectionReport
            stepTimeCandidateSelection;
    }

    [Serializable]
    internal sealed class CharacterFootDiagnosisFactsReference
    {
        public string file;
        public string sha256;
        public string schema;
        public string sampleIdentity;
    }

    [Serializable]
    internal sealed class CharacterFootDiagnosisCoverage
    {
        public int availableFootRowCount;
        public int unavailableFootRowCount;
        public SortedDictionary<string, int> unavailableReasons;
    }

    [Serializable]
    internal sealed class CharacterFootDiagnosisSummary
    {
        public CharacterFootDiagnosisPrimaryResult primaryResult;
        public int targetCount;
        public int targetWithMatchesCount;
        public int matchedEventCount;
    }

    [Serializable]
    internal sealed class CharacterFootDiagnosisTarget
    {
        public string id;
        public string question;
        public List<string> eventKinds;
        public List<string> rules;
        public int eligibleEventCount;
        public int matchedEventCount;
        public bool matchedEventRateAvailable;
        public double? matchedEventRate;
        public CharacterFootDiagnosisOccurrenceProfile occurrence;
        public CharacterFootPathStageAnalysisCoverage pathStageAnalysis;
        public SortedDictionary<string, CharacterFootDiagnosisDistribution> measurements;
        public int representativeEventCount;
        public List<CharacterFootDiagnosisEvidence> representativeEvents;
    }

    [Serializable]
    internal sealed class CharacterFootDiagnosisPrimaryResult
    {
        public string kind;
        public string targetId;
        public CharacterFootDiagnosisOccurrenceProfile occurrence;
        public CharacterFootPathStageAnalysisCoverage pathStageAnalysis;
        public CharacterFootDiagnosisDistribution amplitudeDistribution;
    }

    [Serializable]
    internal sealed class CharacterFootDiagnosisOccurrenceProfile
    {
        public bool available;
        public string sampleUnit;
        public string metric;
        public string comparison;
        public string thresholdUnit;
        public int eligibleEventCount;
        public List<double> configuredThresholds;
        public CharacterFootDiagnosisOccurrenceRate primaryRate;
        public List<CharacterFootDiagnosisOccurrenceRate> rates;
    }

    [Serializable]
    internal sealed class CharacterFootDiagnosisOccurrenceRate
    {
        public double threshold;
        public int eligibleEventCount;
        public int matchedEventCount;
        public double matchedEventRate;
    }

    [Serializable]
    internal sealed class CharacterFootDiagnosisEvidence
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
        public CharacterFootPathStageAnalysis pathStageAnalysis;
    }

    [Serializable]
    internal sealed class CharacterFootDiagnosisDistribution
    {
        public bool available;
        public int count;
        public double? median;
        public double? p90;
        public double? p99;
        public double? maximum;

        internal static CharacterFootDiagnosisDistribution Create(
            List<double> values)
        {
            for (int i = 0; i < values.Count; i++)
            {
                if (!double.IsFinite(values[i]))
                    throw new InvalidOperationException(
                        "Foot diagnosis distribution contains a non-finite value.");
            }
            values.Sort();
            return new CharacterFootDiagnosisDistribution
            {
                available = values.Count > 0,
                count = values.Count,
                median = values.Count > 0 ? Percentile(values, 0.5d) : null,
                p90 = values.Count > 0 ? Percentile(values, 0.9d) : null,
                p99 = values.Count > 0 ? Percentile(values, 0.99d) : null,
                maximum = values.Count > 0 ? values[^1] : null
            };
        }

        static double Percentile(List<double> values, double percentile)
        {
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
