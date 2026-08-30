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

        internal List<JObject> LandingReaches() =>
            (m_Facts["landingReaches"] as JArray ?? new JArray())
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

        internal List<CharacterFootDiagnosisEvidence> Representatives(
            List<JObject> eligible,
            Func<JObject, List<string>> matchRules,
            Func<CharacterFootDiagnosisEvidence, double> rank,
            int limit)
        {
            if (limit <= 0)
                throw new ArgumentOutOfRangeException(nameof(limit));
            return Match(eligible, matchRules)
                .OrderByDescending(rank)
                .ThenBy(value => value.startFrame)
                .Take(limit)
                .OrderBy(value => value.startFrame)
                .ThenBy(value => value.side, StringComparer.Ordinal)
                .ToList();
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
            var document = new CharacterFootDiagnosisDocument
            {
                schema = "character-foot-diagnosis-file/21",
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
                    matchedEventCount = list.Sum(value => value.matchedEventCount),
                    targetResults = list.Select(value =>
                            new CharacterFootDiagnosisTargetResult
                            {
                                id = value.id,
                                eligibleEventCount =
                                    value.eligibleEventCount,
                                matchedEventCount =
                                    value.matchedEventCount,
                                matchedEventRateAvailable =
                                    value.matchedEventRateAvailable,
                                matchedEventRate =
                                    value.matchedEventRate,
                                score = value.score
                            })
                        .ToList()
                }
            };
            CharacterFootDiagnosisScoring.Apply(document);
            return document;
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
                    evidence = ReadBoolMap(value["evidence"] as JObject),
                    swingToLandingFloorHandoff =
                        value["swingToLandingFloorHandoff"]?
                            .ToObject<
                                CharacterFootSwingToLandingFloorHandoffAnalysis>(),
                    lateApproachLandingRevision =
                        value["lateApproachLandingRevision"]?
                            .ToObject<
                                CharacterFootLateApproachLandingRevisionAnalysis>(),
                    landingObservation = value["landingObservation"]?
                        .ToObject<CharacterFootLandingObservationAnalysis>(),
                    visibleOutputJump =
                        value["visibleOutputJump"]?
                            .ToObject<CharacterFootVisibleOutputJumpAnalysis>(),
                    contactSupportGap = value["contactSupportGap"]?
                        .ToObject<CharacterFootContactSupportGapSequence>()
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
        public CharacterFootLandingReachReport landingReach;
        public CharacterFootContactSupportGapCoverage contactSupportGapCoverage;
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
        public List<CharacterFootDiagnosisTargetResult> targetResults;
    }

    [Serializable]
    internal sealed class CharacterFootDiagnosisTargetResult
    {
        public string id;
        public int eligibleEventCount;
        public int matchedEventCount;
        public bool matchedEventRateAvailable;
        public double? matchedEventRate;
        public CharacterFootDiagnosisScore score;
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
        public string scorePolicy = "Informational";
        public CharacterFootDiagnosisScore score;
        public CharacterFootDiagnosisOccurrenceProfile occurrence;
        public List<CharacterFootDiagnosisOccurrenceProfile>
            supplementalOccurrences;
        public CharacterFootPathStageAnalysisCoverage pathStageAnalysis;
        public SortedDictionary<string, CharacterFootDiagnosisDistribution> measurements;
        public SortedDictionary<string, List<CharacterFootDiagnosisCategoryCount>>
            categoricalMeasurements;
        public int representativeEventCount;
        public List<CharacterFootDiagnosisEvidence> representativeEvents;
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
    internal sealed class CharacterFootDiagnosisScore
    {
        public string policy;
        public bool healthAvailable;
        public double? healthScore;
        public string healthRating;
        public bool evidenceAvailable;
        public double? evidenceScore;
        public string evidenceRating;
        public string unavailableReason;
        public int evidenceFullSampleEventCount;
        public double? frequencyBurden;
        public double? frequencyHealthScore;
        public string worstSeverityBand;
        public double? tailScoreCeiling;
        public List<CharacterFootDiagnosisScoreBand> severityBands;
    }

    [Serializable]
    internal sealed class CharacterFootDiagnosisScoreBand
    {
        public string id;
        public double? lowerExclusive;
        public double? upperInclusive;
        public int eventCount;
        public double eventRate;
        public double penaltyWeight;
    }

    [Serializable]
    internal sealed class CharacterFootQualityScorecard
    {
        public string schema = "character-foot-quality-score/1";
        public string scoringVersion = "foot-quality-seven-dimensions/1";
        public string purpose = "ProvisionalReference";
        public bool isShallowReference = true;
        public string notice = "总分仅为浅层参考，不代表通过，不替代逐项证据与用户观感。";
        public CharacterFootDiagnosisFactsReference facts;
        public bool totalScoreAvailable;
        public double? totalScore;
        public string unavailableReason;
        public double availableWeight;
        public double knownWeightedContribution;
        public double minimumPossibleScore;
        public double maximumPossibleScore;
        public double? weightedEvidenceScore;
        public string worstDimensionId;
        public List<string> missingDimensions;
        public List<string> incompleteEvidenceDimensions;
        public List<string> incompleteAttributionTargets;
        public List<string> limitations;
        public List<CharacterFootQualityDimension> dimensions;
        public List<CharacterFootQualityEvidenceReference> evidenceTargets;
        public CharacterFootContactSupportGapCoverage contactSupportCoverage;
    }

    [Serializable]
    internal sealed class CharacterFootQualityDimension
    {
        public string id;
        public string name;
        public double weight;
        public string targetFile;
        public string targetId;
        public int eligibleEventCount;
        public int matchedEventCount;
        public double? matchedEventRate;
        public double? weightedContribution;
        public CharacterFootDiagnosisScore score;
    }

    [Serializable]
    internal sealed class CharacterFootQualityEvidenceReference
    {
        public string file;
        public string targetId;
        public int eligibleEventCount;
        public int matchedEventCount;
        public double? evidenceScore;
    }

    internal static class CharacterFootDiagnosisScoring
    {
        sealed class DimensionDefinition
        {
            internal DimensionDefinition(string id, string name, double weight,
                string file, string target)
            {
                Id = id;
                Name = name;
                Weight = weight;
                File = file;
                Target = target;
            }

            internal readonly string Id;
            internal readonly string Name;
            internal readonly double Weight;
            internal readonly string File;
            internal readonly string Target;
        }

        static readonly DimensionDefinition[] s_Dimensions =
        {
            new DimensionDefinition("penetration", "下陷／穿透", 0.20d,
                "contact-plane-penetration.json", "final-contact-plane-penetration"),
            new DimensionDefinition("contact-fit", "接触未贴合", 0.20d,
                "landing-state-consistency.json", "contact-support-gap"),
            new DimensionDefinition("stable-swing", "普通Swing平顺度", 0.15d,
                "swing-path-jitter.json", "stable-swing-output-jump"),
            new DimensionDefinition("path-revision", "Path变化连续性", 0.15d,
                "swing-path-jitter.json", "path-revision-output-jump"),
            new DimensionDefinition("contact-transition", "接触状态交接", 0.15d,
                "landing-state-consistency.json", "contact-state-output-jump"),
            new DimensionDefinition("leg-pose", "腿部姿态／可达性", 0.10d,
                "landing-leg-extension.json", "landing-leg-extension"),
            new DimensionDefinition("locked-horizontal", "锁脚水平稳定性", 0.05d,
                "locked-sole-motion.json", "locked-horizontal-drift")
        };

        const int FullEvidenceEventCount = 50;
        static readonly double[] s_FiveBandPenalties =
        {
            0d,
            0.1d,
            0.35d,
            0.7d,
            1d
        };
        static readonly double[] s_FiveBandCeilings =
        {
            100d,
            95d,
            89d,
            74d,
            49d
        };

        internal static CharacterFootQualityScorecard BuildQualityScorecard(
            IReadOnlyDictionary<string, CharacterFootDiagnosisDocument> documents,
            CharacterFootDiagnosisFactsReference facts)
        {
            var result = new CharacterFootQualityScorecard
            {
                facts = facts,
                dimensions = new List<CharacterFootQualityDimension>(),
                missingDimensions = new List<string>(),
                incompleteEvidenceDimensions = new List<string>(),
                incompleteAttributionTargets = new List<string>(),
                evidenceTargets = new List<CharacterFootQualityEvidenceReference>(),
                limitations = new List<string>
                {
                    "权值为首版业务取舍，不宣称客观最优；不同评分版本不能直接解释成行为改善。",
                    "总分不代替具体帧、幅度、持续时间、最差项与Evidence；没有全局Pass/Fail。",
                    "位移按表现帧统计，比较必须使用相同输入与Presentation Schedule；速度和加速度仍在分项报告。",
                    "接触未贴合只证明与Verified Anchor平面的间隙，不证明有限Surface脚下有地；正常Swing与Releasing不纳入。",
                    "腿部目前只覆盖正式Landing诊断域；Sliding缺少正式水平上限时不按FullAnchor漂移计分。",
                    "缺失维度不补0或100，不重分配权重；分数区间只表示未知项的数学上下界。"
                },
                contactSupportCoverage = documents["landing-state-consistency.json"]
                    .contactSupportGapCoverage
            };
            if (Math.Abs(s_Dimensions.Sum(value => value.Weight) - 1d) > 1e-12d)
                throw new InvalidOperationException("Foot quality weights do not sum to one.");
            var expectedTargets = new HashSet<string>(
                s_Dimensions.Select(value => value.Target), StringComparer.Ordinal);
            var qualityTargets = documents.Values.SelectMany(value => value.targets)
                .Where(value => value.scorePolicy == "Health").ToList();
            if (qualityTargets.Count != s_Dimensions.Length ||
                qualityTargets.Any(value => !expectedTargets.Contains(value.id)))
                throw new InvalidOperationException("Foot quality target ownership is invalid.");
            double weightedEvidence = 0d;
            bool completeEvidence = true;
            foreach (DimensionDefinition definition in s_Dimensions)
            {
                if (!documents.TryGetValue(definition.File, out CharacterFootDiagnosisDocument document))
                    throw new InvalidOperationException("Foot quality source document is unavailable.");
                CharacterFootDiagnosisTarget target = document.targets.Single(
                    value => value.id == definition.Target);
                CharacterFootDiagnosisScore score = target.score;
                if (score == null || score.policy != "Health")
                    throw new InvalidOperationException("Foot quality score is not finalized.");
                var dimension = new CharacterFootQualityDimension
                {
                    id = definition.Id,
                    name = definition.Name,
                    weight = definition.Weight,
                    targetFile = definition.File,
                    targetId = definition.Target,
                    eligibleEventCount = target.eligibleEventCount,
                    matchedEventCount = target.matchedEventCount,
                    matchedEventRate = target.matchedEventRate,
                    score = score
                };
                if (score.healthAvailable)
                {
                    RequireScore(score.healthScore);
                    dimension.weightedContribution = score.healthScore.Value * definition.Weight;
                    result.knownWeightedContribution += dimension.weightedContribution.Value;
                    result.availableWeight += definition.Weight;
                }
                else
                {
                    result.missingDimensions.Add(definition.Id);
                }
                if (score.evidenceAvailable)
                {
                    RequireScore(score.evidenceScore);
                    weightedEvidence += score.evidenceScore.Value * definition.Weight;
                }
                else
                {
                    completeEvidence = false;
                }
                if (!score.evidenceAvailable || score.evidenceScore.Value < 100d)
                    result.incompleteEvidenceDimensions.Add(definition.Id);
                result.dimensions.Add(dimension);
            }
            result.totalScoreAvailable = result.missingDimensions.Count == 0;
            result.totalScore = result.totalScoreAvailable
                ? Round(result.knownWeightedContribution) : null;
            result.unavailableReason = result.totalScoreAvailable ? null : "MissingQualityDimensions";
            result.minimumPossibleScore = Round(result.knownWeightedContribution);
            result.maximumPossibleScore = Round(result.knownWeightedContribution +
                100d * (1d - result.availableWeight));
            result.knownWeightedContribution = Round(result.knownWeightedContribution);
            result.availableWeight = Math.Round(result.availableWeight, 8);
            result.weightedEvidenceScore = completeEvidence ? Round(weightedEvidence) : null;
            result.worstDimensionId = result.dimensions
                .Where(value => value.score.healthAvailable)
                .OrderBy(value => value.score.healthScore.Value)
                .ThenBy(value => value.id, StringComparer.Ordinal)
                .FirstOrDefault()?.id;
            foreach (KeyValuePair<string, CharacterFootDiagnosisDocument> document in documents)
                foreach (CharacterFootDiagnosisTarget target in document.Value.targets)
                {
                    if (target.pathStageAnalysis != null &&
                        target.pathStageAnalysis.availableEventCount <
                            target.pathStageAnalysis.eligibleEventCount)
                        result.incompleteAttributionTargets.Add(target.id);
                    if (target.scorePolicy != "Informational")
                        continue;
                    result.evidenceTargets.Add(new CharacterFootQualityEvidenceReference
                    {
                        file = document.Key,
                        targetId = target.id,
                        eligibleEventCount = target.eligibleEventCount,
                        matchedEventCount = target.matchedEventCount,
                        evidenceScore = target.score.evidenceScore
                    });
                }
            return result;
        }

        static void RequireScore(double? score)
        {
            if (!score.HasValue || !double.IsFinite(score.Value) || score < 0d || score > 100d)
                throw new InvalidOperationException("Foot quality score value is invalid.");
        }

        internal static void Apply(CharacterFootDiagnosisDocument document)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));
            document.targets ??= new List<CharacterFootDiagnosisTarget>();
            for (int i = 0; i < document.targets.Count; i++)
                document.targets[i].score = Score(document.targets[i]);
            if (document.summary == null)
                throw new InvalidOperationException(
                    "Foot diagnosis summary is unavailable.");
            document.summary.targetResults ??=
                new List<CharacterFootDiagnosisTargetResult>();
            for (int i = 0; i < document.summary.targetResults.Count; i++)
            {
                CharacterFootDiagnosisTargetResult result =
                    document.summary.targetResults[i];
                CharacterFootDiagnosisTarget target = document.targets.Find(
                    value => string.Equals(
                        value.id,
                        result.id,
                        StringComparison.Ordinal));
                result.score = target?.score;
            }
        }

        static CharacterFootDiagnosisScore Score(
            CharacterFootDiagnosisTarget target)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));
            string policy = target.scorePolicy;
            if (policy != "Health" && policy != "Informational")
                throw new InvalidOperationException("Foot diagnosis score policy is invalid.");
            CharacterFootDiagnosisScore score = Evidence(
                policy,
                target.eligibleEventCount,
                target.pathStageAnalysis);
            if (target.eligibleEventCount <= 0)
            {
                score.unavailableReason = "NoEligibleEvents";
                return score;
            }
            if (string.Equals(
                    policy,
                    "Informational",
                    StringComparison.Ordinal))
            {
                score.unavailableReason = "InformationalTarget";
                return score;
            }
            if (target.pathStageAnalysis != null &&
                target.pathStageAnalysis.availableEventCount <
                target.pathStageAnalysis.eligibleEventCount)
            {
                score.unavailableReason = "RequiredStageFactsIncomplete";
                return score;
            }
            score.healthAvailable = true;
            if (target.occurrence?.available == true)
            {
                ScoreOccurrence(target, score);
            }
            else
            {
                double rate = target.matchedEventRate ?? 0d;
                RequireRate(rate);
                score.frequencyBurden = rate;
                score.frequencyHealthScore = Round(
                    100d * (1d - rate));
                score.healthScore = score.frequencyHealthScore;
                score.worstSeverityBand = target.matchedEventCount > 0
                    ? "MatchedViolation"
                    : "NoMatchedViolation";
            }
            score.healthRating = HealthRating(score.healthScore.Value);
            return score;
        }

        static void ScoreOccurrence(
            CharacterFootDiagnosisTarget target,
            CharacterFootDiagnosisScore score)
        {
            CharacterFootDiagnosisOccurrenceProfile occurrence =
                target.occurrence;
            int thresholdCount = occurrence.configuredThresholds?.Count ?? 0;
            if (thresholdCount == 0 ||
                occurrence.rates == null ||
                occurrence.rates.Count != thresholdCount ||
                occurrence.eligibleEventCount != target.eligibleEventCount)
            {
                throw new InvalidOperationException(
                    $"Foot diagnosis target '{target.id}' occurrence score input is invalid.");
            }
            var bands = new List<CharacterFootDiagnosisScoreBand>(
                thresholdCount + 1);
            int previousMatched = occurrence.eligibleEventCount;
            double burden = 0d;
            int worstBand = 0;
            for (int i = 0; i <= thresholdCount; i++)
            {
                int matched = i < thresholdCount
                    ? occurrence.rates[i].matchedEventCount
                    : 0;
                if (matched < 0 || matched > previousMatched)
                {
                    throw new InvalidOperationException(
                        $"Foot diagnosis target '{target.id}' occurrence counts are invalid.");
                }
                int count = previousMatched - matched;
                double rate = (double)count / occurrence.eligibleEventCount;
                RequireRate(rate);
                double penalty = Penalty(i, thresholdCount + 1);
                burden += rate * penalty;
                if (count > 0)
                    worstBand = i;
                bands.Add(new CharacterFootDiagnosisScoreBand
                {
                    id = BandId(occurrence.configuredThresholds, i),
                    lowerExclusive = i == 0
                        ? null
                        : occurrence.configuredThresholds[i - 1],
                    upperInclusive = i < thresholdCount
                        ? occurrence.configuredThresholds[i]
                        : null,
                    eventCount = count,
                    eventRate = rate,
                    penaltyWeight = penalty
                });
                previousMatched = matched;
            }
            double ceiling = Ceiling(worstBand, thresholdCount + 1);
            double health = Math.Min(100d * (1d - burden), ceiling);
            score.frequencyBurden = Round(burden);
            score.frequencyHealthScore = Round(100d * (1d - burden));
            score.worstSeverityBand = bands[worstBand].id;
            score.tailScoreCeiling = ceiling;
            score.severityBands = bands;
            score.healthScore = Round(health);
        }

        static CharacterFootDiagnosisScore Evidence(
            string policy,
            int eligibleEventCount,
            CharacterFootPathStageAnalysisCoverage stage)
        {
            var score = new CharacterFootDiagnosisScore
            {
                policy = policy,
                evidenceFullSampleEventCount = FullEvidenceEventCount,
                healthRating = "Unavailable",
                evidenceRating = "Unavailable",
                severityBands = new List<CharacterFootDiagnosisScoreBand>()
            };
            if (eligibleEventCount <= 0)
                return score;
            double sampleCoverage = Math.Min(
                1d,
                (double)eligibleEventCount / FullEvidenceEventCount);
            double stageCoverage = stage == null
                ? 1d
                : stage.eligibleEventCount > 0
                    ? (double)stage.availableEventCount /
                      stage.eligibleEventCount
                    : 0d;
            RequireRate(stageCoverage);
            double evidence = 100d * sampleCoverage * stageCoverage;
            score.evidenceAvailable = true;
            score.evidenceScore = Round(evidence);
            score.evidenceRating = EvidenceRating(evidence);
            return score;
        }


        static double Penalty(int index, int bandCount)
        {
            if (bandCount == s_FiveBandPenalties.Length)
                return s_FiveBandPenalties[index];
            return bandCount <= 1
                ? 0d
                : (double)index / (bandCount - 1);
        }

        static double Ceiling(int index, int bandCount)
        {
            if (bandCount == s_FiveBandCeilings.Length)
                return s_FiveBandCeilings[index];
            if (index <= 0)
                return 100d;
            double normalized = (double)index / (bandCount - 1);
            return normalized >= 1d
                ? 49d
                : normalized >= 0.75d
                    ? 74d
                    : normalized >= 0.5d
                        ? 89d
                        : 95d;
        }

        static string BandId(List<double> thresholds, int index)
        {
            if (index == 0)
                return "AtOrBelowFirstThreshold";
            if (index == thresholds.Count)
                return "AboveLastThreshold";
            return $"ThresholdBand{index}";
        }

        static void RequireRate(double value)
        {
            if (!double.IsFinite(value) || value < 0d || value > 1d)
                throw new InvalidOperationException(
                    "Foot diagnosis score rate is invalid.");
        }

        static double Round(double value) =>
            Math.Round(value, 1, MidpointRounding.AwayFromZero);

        static string HealthRating(double value) => value >= 90d
            ? "Stable"
            : value >= 75d
                ? "Attention"
                : value >= 50d
                    ? "Degraded"
                    : "Severe";

        static string EvidenceRating(double value) => value >= 90d
            ? "Strong"
            : value >= 60d
                ? "Moderate"
                : "Limited";
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
        public CharacterFootSwingToLandingFloorHandoffAnalysis
            swingToLandingFloorHandoff;
        public CharacterFootLateApproachLandingRevisionAnalysis
            lateApproachLandingRevision;
        public CharacterFootLandingObservationAnalysis landingObservation;
        public CharacterFootVisibleOutputJumpAnalysis visibleOutputJump;
        public CharacterFootContactSupportGapSequence contactSupportGap;
    }

    [Serializable]
    internal sealed class CharacterFootDiagnosisCategoryCount
    {
        public string value;
        public int count;
    }

    [Serializable]
    internal sealed class CharacterFootVectorFact
    {
        public double x;
        public double y;
        public double z;

        internal static CharacterFootVectorFact From(
            UnityEngine.Vector3 value) =>
            new CharacterFootVectorFact
            {
                x = value.x,
                y = value.y,
                z = value.z
            };
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
