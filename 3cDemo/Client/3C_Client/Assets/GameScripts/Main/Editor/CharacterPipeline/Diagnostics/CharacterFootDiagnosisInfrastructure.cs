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

        internal CharacterFootDiagnosisDocument Document(
            string diagnosticId,
            params CharacterFootDiagnosisTarget[] targets)
        {
            JObject sample = m_Facts["sample"] as JObject;
            var list = targets.ToList();
            return new CharacterFootDiagnosisDocument
            {
                schema = "character-foot-diagnosis-file/1",
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
                    targetCount = list.Count,
                    targetWithMatchesCount = list.Count(
                        value => value.matchedEventCount > 0),
                    matchedEventCount = list.Sum(value => value.matchedEventCount)
                }
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
        public double matchedEventRate;
        public SortedDictionary<string, CharacterFootDiagnosisDistribution> measurements;
        public int representativeEventCount;
        public List<CharacterFootDiagnosisEvidence> representativeEvents;
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
    }

    [Serializable]
    internal sealed class CharacterFootDiagnosisDistribution
    {
        public int count;
        public double median;
        public double p90;
        public double p99;
        public double maximum;

        internal static CharacterFootDiagnosisDistribution Create(
            List<double> values)
        {
            values.Sort();
            return new CharacterFootDiagnosisDistribution
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
