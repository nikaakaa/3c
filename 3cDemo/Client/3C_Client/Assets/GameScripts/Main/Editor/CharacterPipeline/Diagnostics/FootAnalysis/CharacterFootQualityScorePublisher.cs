using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using KK.GeneratedDiagnosticSampling.Host;

namespace ThirdPersonCharacter.Pipeline.Diagnostics.FootAnalysis.Editor
{
    internal static class CharacterFootQualityScorePublisher
    {
        internal const string FileName = "quality-score.json";
        const string ScoreId = "foot-total-score";
        const string ScoreSchema = "character-foot-quality-score/4";
        const string ScoringVersion = "foot-quality-seven-dimensions/3";
        const int MinimumEligible = 10;
        const int FullEvidenceEligible = 50;
        const string Notice =
            "总分仅为浅层参考，不代表通过，不替代逐项证据与用户观感。";

        static readonly string[] s_Limitations =
        {
            "权值和严重度权重为当前业务取舍，不宣称客观最优；不同评分版本不能直接解释成行为改善。",
            "总分不代替具体帧、幅度、持续时间、最差项与Evidence；没有全局Pass/Fail。",
            "位移按表现帧统计，比较必须使用相同输入与Presentation Schedule；速度和加速度仍在分项报告。",
            "接触未贴合只证明与Verified Anchor平面的间隙，不证明有限Surface脚下有地；满位置权重接触过程只计一次，FullAnchor/Sliding/Landing分项展示，Release及部分权重仅作证据。",
            "接触分项没有独立Health，不重复扣分；须查看各域脚帧/过程次数、持续时间与重新离面，少量FullAnchor样本不能证明保持质量，短暂大间隙不因不足100ms而隐藏。",
            "腿部按Runtime实际Landing状态段计分；普通Swing膝盖连续性仍须查看独立Solver与Physical证据。",
            "Health至少需要10个eligible；10到49个样本仍列为弱Evidence，不用少量零命中冒充充分证明。",
            "米制严重度按1/2/5/10/20/30厘米互斥分档加权，不再由单个最差样本把整个维度硬封顶。",
            "缺失维度不补0或100，不重分配权重；分数区间只表示未知项的数学上下界。"
        };

        static readonly DimensionDefinition[] s_Dimensions =
        {
            new DimensionDefinition("penetration", "下陷穿透", "final-contact-plane-penetration", 0.20d),
            new DimensionDefinition("contact-fit", "接触未贴合", "contact-support-gap", 0.20d),
            new DimensionDefinition("stable-swing", "普通Swing平顺度", "stable-swing-output-jump", 0.15d),
            new DimensionDefinition("path-revision", "Path变化连续性", "path-revision-output-jump", 0.15d),
            new DimensionDefinition("contact-transition", "接触状态交接", "contact-state-output-jump", 0.15d),
            new DimensionDefinition("leg-pose", "腿部姿态可达性", "landing-leg-extension", 0.10d),
            new DimensionDefinition("locked-horizontal", "锁脚水平稳定性", "locked-horizontal-drift", 0.05d)
        };

        static readonly double[] s_SeverityThresholds =
        {
            0.01d,
            0.02d,
            0.05d,
            0.10d,
            0.20d,
            0.30d
        };

        internal static string Publish(
            DiagnosticAnalysisResult result,
            DiagnosticAnalysisPlan plan,
            string outputDirectory)
        {
            if (result == null)
                throw new ArgumentNullException(nameof(result));
            if (plan == null)
                throw new ArgumentNullException(nameof(plan));
            if (!plan.Scores.Any(value => value.Id == ScoreId))
                return string.Empty;
            outputDirectory = Path.GetFullPath(outputDirectory);
            string path = Path.Combine(outputDirectory, FileName);
            string stagingPath = path + ".staging";
            if (File.Exists(path) || File.Exists(stagingPath))
                throw new IOException("Foot quality score output already exists.");
            string content = BuildJson(result, plan);
            try
            {
                using (var stream = new FileStream(
                    stagingPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.Read))
                using (var writer = new StreamWriter(
                    stream,
                    new UTF8Encoding(false)))
                {
                    writer.Write(content);
                    writer.Flush();
                    stream.Flush(true);
                }
                File.Move(stagingPath, path);
            }
            catch
            {
                if (File.Exists(stagingPath))
                    File.Delete(stagingPath);
                throw;
            }
            return path;
        }

        static string BuildJson(
            DiagnosticAnalysisResult result,
            DiagnosticAnalysisPlan plan)
        {
            DiagnosticPlanScore scoreDefinition = plan.Scores
                .FirstOrDefault(value => value.Id == ScoreId);
            if (scoreDefinition.Members.Count != s_Dimensions.Length)
                throw new InvalidDataException("Foot quality score members are invalid.");
            var snapshots = new List<DimensionSnapshot>(s_Dimensions.Length);
            for (int i = 0; i < s_Dimensions.Length; i++)
            {
                DimensionDefinition definition = s_Dimensions[i];
                DiagnosticPlanScoreMember member = scoreDefinition?.Members
                    .FirstOrDefault(value => value.RuleId == definition.RuleId);
                DiagnosticRuleResult rule = result.Rules
                    .FirstOrDefault(value => value.RuleId == definition.RuleId);
                snapshots.Add(BuildDimension(definition, member, rule));
            }

            double totalWeight = snapshots
                .Where(value => value.Configured)
                .Sum(value => value.Weight);
            double availableWeight = snapshots
                .Where(value => value.Configured && value.Health.HasValue)
                .Sum(value => value.Weight);
            double knownContribution = snapshots
                .Where(value => value.Configured && value.Health.HasValue)
                .Sum(value => value.Health.Value * value.Weight);
            bool complete = snapshots.Count == s_Dimensions.Length &&
                snapshots.All(value => value.Configured && value.Health.HasValue) &&
                totalWeight > 0d;
            double? totalScore = complete
                ? knownContribution / totalWeight
                : (double?)null;
            double? minimumScore = totalWeight > 0d
                ? knownContribution / totalWeight
                : null;
            double? maximumScore = totalWeight > 0d
                ? (knownContribution +
                   (totalWeight - availableWeight) * 100d) / totalWeight
                : null;
            double weightedEvidence = snapshots
                .Where(value => value.Configured && value.Evidence.HasValue)
                .Sum(value => value.Evidence.Value * value.Weight);
            bool completeEvidence = snapshots.All(value =>
                value.Configured && value.Evidence.HasValue);
            var missingDimensions = snapshots
                .Where(value => !value.Health.HasValue)
                .Select(value => value.Definition.Id)
                .ToList();
            var incompleteEvidenceDimensions = snapshots
                .Where(value => !value.Evidence.HasValue ||
                    value.Evidence.Value < 100d ||
                    value.Definition.Id == "contact-fit" &&
                    value.Coverage.Eligible.GetValueOrDefault() < FullEvidenceEligible)
                .Select(value => value.Definition.Id)
                .ToList();
            DimensionSnapshot worst = snapshots
                .Where(value => value.Health.HasValue)
                .OrderBy(value => value.Health.Value)
                .ThenBy(value => value.Definition.Id, StringComparer.Ordinal)
                .FirstOrDefault();

            var builder = new StringBuilder();
            builder.Append("{\n");
            Property(builder, "schema", ScoreSchema, true, 1);
            Property(builder, "scoringVersion", ScoringVersion, true, 1);
            Property(builder, "purpose", "ProvisionalReference", true, 1);
            BooleanProperty(builder, "isShallowReference", true, true, 1);
            Property(builder, "notice", Notice, true, 1);
            Property(builder, "planId", result.PlanId, true, 1);
            Property(builder, "planHash", result.PlanHash, true, 1);
            Property(builder, "analyzerHash", result.AnalyzerHash, true, 1);
            BooleanProperty(builder, "totalScoreAvailable", complete, true, 1);
            NullableNumberProperty(builder, "totalScore", totalScore, true, 1);
            NullableStringProperty(
                builder,
                "unavailableReason",
                complete ? null : "MissingQualityDimensions",
                true,
                1);
            NumberProperty(builder, "availableWeight", availableWeight, true, 1);
            NumberProperty(builder, "knownWeightedContribution", knownContribution, true, 1);
            NullableNumberProperty(builder, "minimumPossibleScore", minimumScore, true, 1);
            NullableNumberProperty(builder, "maximumPossibleScore", maximumScore, true, 1);
            NullableNumberProperty(
                builder,
                "weightedEvidenceScore",
                completeEvidence && totalWeight > 0d
                    ? weightedEvidence / totalWeight
                    : (double?)null,
                true,
                1);
            NullableStringProperty(
                builder,
                "worstDimensionId",
                worst.Health.HasValue ? worst.Definition.Id : null,
                true,
                1);
            StringArrayProperty(builder, "missingDimensions", missingDimensions, true, 1);
            StringArrayProperty(
                builder,
                "incompleteEvidenceDimensions",
                incompleteEvidenceDimensions,
                true,
                1);
            StringArrayProperty(
                builder,
                "incompleteAttributionTargets",
                Array.Empty<string>(),
                true,
                1);
            StringArrayProperty(builder, "limitations", s_Limitations, true, 1);
            Indent(builder, 1).Append("\"dimensions\": [\n");
            for (int i = 0; i < snapshots.Count; i++)
            {
                WriteDimension(builder, snapshots[i], 2);
                builder.Append(i + 1 == snapshots.Count ? "\n" : ",\n");
            }
            Indent(builder, 1).Append("],\n");
            Indent(builder, 1).Append("\"evidenceTargets\": [\n");
            List<DiagnosticRuleResult> evidenceRules = result.Rules
                .Where(value => !s_Dimensions.Any(
                    definition => definition.RuleId == value.RuleId))
                .ToList();
            for (int i = 0; i < evidenceRules.Count; i++)
            {
                WriteEvidenceTarget(builder, evidenceRules[i], 2);
                builder.Append(i + 1 == evidenceRules.Count ? "\n" : ",\n");
            }
            Indent(builder, 1).Append("],\n");
            Indent(builder, 1).Append("\"targets\": [\n");
            for (int i = 0; i < result.Rules.Count; i++)
            {
                WriteTarget(builder, result.Rules[i], 2);
                builder.Append(i + 1 == result.Rules.Count ? "\n" : ",\n");
            }
            Indent(builder, 1).Append("]\n");
            builder.Append("}\n");
            return builder.ToString();
        }

        static DimensionSnapshot BuildDimension(
            DimensionDefinition definition,
            DiagnosticPlanScoreMember member,
            DiagnosticRuleResult rule)
        {
            bool configured = member != null;
            if (configured && Math.Abs(member.Weight - definition.Weight) > 1e-12d)
                throw new InvalidDataException(
                    $"Foot quality weight for '{definition.RuleId}' is invalid.");
            double weight = configured ? member.Weight : 0d;
            CoverageSummary coverage = rule == null
                ? default
                : ReadDimensionCoverage(rule, definition.RuleId);
            bool healthAvailable = rule != null &&
                configured &&
                rule.Score.HasValue &&
                coverage.Eligible.HasValue &&
                coverage.Eligible.Value >= MinimumEligible &&
                rule.State != DiagnosticRuleState.MissingEvidence &&
                rule.State != DiagnosticRuleState.NotApplicable;
            double? health = healthAvailable
                ? rule.Score.Value * 100d
                : null;
            double? evidence = coverage.Eligible.HasValue &&
                coverage.Eligible.Value > 0 &&
                rule != null &&
                rule.State != DiagnosticRuleState.MissingEvidence &&
                rule.State != DiagnosticRuleState.NotApplicable
                ? Math.Min(100d,
                    (double)coverage.Eligible.Value / FullEvidenceEligible * 100d)
                : null;
            return new DimensionSnapshot(
                definition,
                weight,
                configured,
                rule,
                coverage,
                health,
                evidence);
        }

        static CoverageSummary ReadDimensionCoverage(
            DiagnosticRuleResult rule,
            string ruleId)
        {
            if (ruleId == "contact-support-gap")
            {
                List<Coverage> coverage = ReadCoverage(rule)
                    .Where(value => value.Id == "contact-support-gap-coverage")
                    .ToList();
                if (coverage.Count != 0)
                    return CoverageSummary.From(coverage);
            }
            if (TryReadSummary(rule.Summary, out int matched, out int eligible))
                return new CoverageSummary(eligible, matched);
            return default;
        }

        static List<Coverage> ReadCoverage(DiagnosticRuleResult rule)
        {
            var result = new List<Coverage>();
            foreach (DiagnosticEvidence evidence in rule.Evidence)
            {
                if (!evidence.Id.EndsWith("-coverage", StringComparison.Ordinal) ||
                    !TryReadCoverageValue(evidence.Value, out int eligible, out int matched))
                {
                    continue;
                }
                result.Add(new Coverage(
                    evidence.Id,
                    evidence.DimensionId,
                    evidence.SequenceStart,
                    evidence.SequenceEnd,
                    eligible,
                    matched));
            }
            if (result.Count != 0)
                return result;
            if (!TryReadSummary(rule.Summary, out int summaryMatched, out int summaryEligible))
                return result;
            ulong start = rule.Findings.Count == 0
                ? 0
                : rule.Findings.Min(value => value.SequenceStart);
            ulong end = rule.Findings.Count == 0
                ? 0
                : rule.Findings.Max(value => value.SequenceEnd);
            string dimension = rule.Findings.Count == 0
                ? "aggregate"
                : rule.Findings[0].DimensionId;
            result.Add(new Coverage(
                rule.RuleId + "-coverage",
                dimension,
                start,
                end,
                summaryEligible,
                summaryMatched));
            return result;
        }

        static bool TryReadCoverageValue(
            string value,
            out int eligible,
            out int matched)
        {
            eligible = 0;
            matched = 0;
            string[] parts = (value ?? string.Empty).Split(';');
            bool hasEligible = false;
            bool hasMatched = false;
            for (int i = 0; i < parts.Length; i++)
            {
                string[] pair = parts[i].Split('=');
                if (pair.Length != 2)
                    continue;
                if (pair[0] == "eligible" &&
                    int.TryParse(pair[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out eligible))
                {
                    hasEligible = true;
                }
                else if (pair[0] == "matched" &&
                         int.TryParse(pair[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out matched))
                {
                    hasMatched = true;
                }
            }
            return hasEligible && hasMatched && eligible >= 0 && matched >= 0 && matched <= eligible;
        }

        static bool TryReadSummary(
            string summary,
            out int matched,
            out int eligible)
        {
            matched = 0;
            eligible = 0;
            string text = summary ?? string.Empty;
            int marker = text.IndexOf(
                " of ",
                StringComparison.Ordinal);
            if (marker <= 0)
                return false;
            int matchedStart = marker - 1;
            while (matchedStart >= 0 && char.IsDigit(text[matchedStart]))
                matchedStart--;
            matchedStart++;
            int eligibleStart = marker + 4;
            int eligibleEnd = eligibleStart;
            while (eligibleEnd < text.Length && char.IsDigit(text[eligibleEnd]))
                eligibleEnd++;
            if (matchedStart >= marker || eligibleStart == eligibleEnd ||
                !int.TryParse(
                    text.Substring(matchedStart, marker - matchedStart),
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out matched) ||
                !int.TryParse(
                    text.Substring(eligibleStart, eligibleEnd - eligibleStart),
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out eligible))
            {
                matched = 0;
                eligible = 0;
                return false;
            }
            return matched >= 0 && eligible >= 0 && matched <= eligible;
        }

        static void WriteDimension(
            StringBuilder builder,
            DimensionSnapshot value,
            int indent)
        {
            Indent(builder, indent).Append("{\n");
            Property(builder, "id", value.Definition.Id, true, indent + 1);
            Property(builder, "name", value.Definition.Name, true, indent + 1);
            Property(builder, "targetFile", "diagnosis.json", true, indent + 1);
            Property(builder, "targetId", value.Definition.RuleId, true, indent + 1);
            NumberProperty(builder, "weight", value.Weight, true, indent + 1);
            NumberProperty(
                builder,
                "eligibleEventCount",
                value.Coverage.Eligible.GetValueOrDefault(),
                true,
                indent + 1);
            NullableNumberProperty(
                builder,
                "matchedEventCount",
                value.Coverage.Matched,
                true,
                indent + 1);
            NullableNumberProperty(
                builder,
                "matchedEventRate",
                value.Coverage.Rate,
                true,
                indent + 1);
            NullableNumberProperty(
                builder,
                "weightedContribution",
                value.Health.HasValue ? value.Health.Value * value.Weight : (double?)null,
                true,
                indent + 1);
            WriteScore(builder, value, indent + 1);
            Indent(builder, indent).Append("}");
        }

        static void WriteScore(
            StringBuilder builder,
            DimensionSnapshot value,
            int indent)
        {
            Indent(builder, indent).Append("\"score\": {\n");
            Property(
                builder,
                "policy",
                value.Configured ? "Health" : "Unconfigured",
                true,
                indent + 1);
            BooleanProperty(builder, "healthAvailable", value.Health.HasValue, true, indent + 1);
            NullableNumberProperty(builder, "healthScore", value.Health, true, indent + 1);
            Property(
                builder,
                "healthRating",
                value.Health.HasValue ? HealthRating(value.Health.Value) : "Unavailable",
                true,
                indent + 1);
            BooleanProperty(builder, "evidenceAvailable", value.Evidence.HasValue, true, indent + 1);
            NullableNumberProperty(builder, "evidenceScore", value.Evidence, true, indent + 1);
            Property(
                builder,
                "evidenceRating",
                value.Evidence.HasValue
                    ? EvidenceRating(value.Evidence.Value)
                    : "Unavailable",
                true,
                indent + 1);
            NullableStringProperty(
                builder,
                "unavailableReason",
                value.Health.HasValue ? null : UnavailableReason(value.Rule, value.Coverage),
                true,
                indent + 1);
            NumberProperty(builder, "evidenceFullSampleEventCount", FullEvidenceEligible, true, indent + 1);
            NumberProperty(builder, "minimumHealthEligibleEventCount", MinimumEligible, true, indent + 1);
            BooleanProperty(
                builder,
                "healthCoverageSatisfied",
                value.Coverage.Eligible.GetValueOrDefault() >= MinimumEligible,
                true,
                indent + 1);
            NullableNumberProperty(
                builder,
                "severityWeightedBurden",
                value.Health.HasValue ? 1d - value.Health.Value / 100d : (double?)null,
                true,
                indent + 1);
            NullableNumberProperty(
                builder,
                "severityWeightedHealthScore",
                value.Health,
                true,
                indent + 1);
            NullableStringProperty(builder, "worstSeverityBand", null, true, indent + 1);
            Indent(builder, indent + 1).Append("\"severityBands\": []\n");
            Indent(builder, indent).Append("}");
        }

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

        static void WriteEvidenceTarget(
            StringBuilder builder,
            DiagnosticRuleResult rule,
            int indent)
        {
            CoverageSummary coverage = ReadDimensionCoverage(rule, rule.RuleId);
            Indent(builder, indent).Append("{\n");
            Property(builder, "file", "diagnosis.json", true, indent + 1);
            Property(builder, "targetId", rule.RuleId, true, indent + 1);
            NumberProperty(builder, "eligibleEventCount", coverage.Eligible.GetValueOrDefault(), true, indent + 1);
            NumberProperty(builder, "matchedEventCount", coverage.Matched.GetValueOrDefault(), true, indent + 1);
            NullableNumberProperty(
                builder,
                "evidenceScore",
                coverage.Eligible.HasValue
                    ? Math.Min(100d, (double)coverage.Eligible.Value / FullEvidenceEligible * 100d)
                    : null,
                false,
                indent + 1);
            Indent(builder, indent).Append("}");
        }

        static void WriteTarget(
            StringBuilder builder,
            DiagnosticRuleResult rule,
            int indent)
        {
            List<Coverage> coverage = ReadCoverage(rule);
            Indent(builder, indent).Append("{\n");
            Property(builder, "id", rule.RuleId, true, indent + 1);
            Property(builder, "operator", rule.OperatorId, true, indent + 1);
            Property(builder, "category", rule.Category, true, indent + 1);
            Property(builder, "state", rule.State.ToString(), true, indent + 1);
            Property(builder, "summary", rule.Summary, true, indent + 1);
            NullableNumberProperty(
                builder,
                "health",
                rule.Score.HasValue ? rule.Score.Value * 100d : (double?)null,
                true,
                indent + 1);
            NumberProperty(builder, "findingCount", rule.Findings.Count, true, indent + 1);
            NumberProperty(builder, "evidenceCount", rule.Evidence.Count, true, indent + 1);
            Indent(builder, indent + 1).Append("\"coverage\": [\n");
            for (int i = 0; i < coverage.Count; i++)
            {
                Coverage value = coverage[i];
                Indent(builder, indent + 2).Append("{\n");
                Property(builder, "id", value.Id, true, indent + 3);
                Property(builder, "dimension", value.Dimension, true, indent + 3);
                NumberProperty(builder, "sequenceStart", value.SequenceStart, true, indent + 3);
                NumberProperty(builder, "sequenceEnd", value.SequenceEnd, true, indent + 3);
                NumberProperty(builder, "eligible", value.Eligible, true, indent + 3);
                NumberProperty(builder, "matched", value.Matched, false, indent + 3);
                Indent(builder, indent + 2).Append("}");
                builder.Append(i + 1 == coverage.Count ? "\n" : ",\n");
            }
            Indent(builder, indent + 1).Append("]\n");
            Indent(builder, indent).Append("}");
        }

        static string UnavailableReason(
            DiagnosticRuleResult rule,
            CoverageSummary coverage)
        {
            if (rule == null)
                return "rule-not-in-plan";
            if (rule.State == DiagnosticRuleState.MissingEvidence)
                return "missing-evidence";
            if (rule.State == DiagnosticRuleState.NotApplicable)
                return "not-applicable";
            if (!coverage.Eligible.HasValue)
                return "coverage-unavailable";
            if (coverage.Eligible.Value < MinimumEligible)
                return "insufficient-eligible-samples";
            if (!rule.Score.HasValue)
                return "health-unavailable";
            return string.Empty;
        }

        static void Property(
            StringBuilder builder,
            string name,
            string value,
            bool comma,
            int indent)
        {
            Indent(builder, indent).Append('"').Append(Escape(name)).Append("\": \"")
                .Append(Escape(value)).Append('"').Append(comma ? ",\n" : "\n");
        }

        static void NumberProperty(
            StringBuilder builder,
            string name,
            double value,
            bool comma,
            int indent)
        {
            Indent(builder, indent).Append('"').Append(Escape(name)).Append("\": ")
                .Append(value.ToString("R", CultureInfo.InvariantCulture))
                .Append(comma ? ",\n" : "\n");
        }

        static void NullableStringProperty(
            StringBuilder builder,
            string name,
            string value,
            bool comma,
            int indent)
        {
            Indent(builder, indent).Append('"').Append(Escape(name)).Append("\": ");
            if (value == null)
                builder.Append("null");
            else
                builder.Append('"').Append(Escape(value)).Append('"');
            builder.Append(comma ? ",\n" : "\n");
        }

        static void StringArrayProperty(
            StringBuilder builder,
            string name,
            IEnumerable<string> values,
            bool comma,
            int indent)
        {
            Indent(builder, indent).Append('"').Append(Escape(name)).Append("\": [");
            bool first = true;
            foreach (string value in values)
            {
                if (!first)
                    builder.Append(", ");
                builder.Append('"').Append(Escape(value)).Append('"');
                first = false;
            }
            builder.Append(']').Append(comma ? ",\n" : "\n");
        }

        static void NumberProperty(
            StringBuilder builder,
            string name,
            int value,
            bool comma,
            int indent) =>
            NumberProperty(builder, name, (double)value, comma, indent);

        static void NumberProperty(
            StringBuilder builder,
            string name,
            ulong value,
            bool comma,
            int indent)
        {
            Indent(builder, indent).Append('"').Append(Escape(name)).Append("\": ")
                .Append(value.ToString(CultureInfo.InvariantCulture))
                .Append(comma ? ",\n" : "\n");
        }

        static void NullableNumberProperty(
            StringBuilder builder,
            string name,
            double? value,
            bool comma,
            int indent)
        {
            Indent(builder, indent).Append('"').Append(Escape(name)).Append("\": ");
            if (value.HasValue)
                builder.Append(value.Value.ToString("R", CultureInfo.InvariantCulture));
            else
                builder.Append("null");
            builder.Append(comma ? ",\n" : "\n");
        }

        static void BooleanProperty(
            StringBuilder builder,
            string name,
            bool value,
            bool comma,
            int indent)
        {
            Indent(builder, indent).Append('"').Append(Escape(name)).Append("\": ")
                .Append(value ? "true" : "false")
                .Append(comma ? ",\n" : "\n");
        }

        static void ArrayProperty(
            StringBuilder builder,
            string name,
            IReadOnlyList<double> values,
            bool comma,
            int indent)
        {
            Indent(builder, indent).Append('"').Append(Escape(name)).Append("\": [");
            for (int i = 0; i < values.Count; i++)
            {
                if (i != 0)
                    builder.Append(", ");
                builder.Append(values[i].ToString("R", CultureInfo.InvariantCulture));
            }
            builder.Append(']').Append(comma ? ",\n" : "\n");
        }

        static StringBuilder Indent(StringBuilder builder, int count) =>
            builder.Append(' ', count * 2);

        static string Escape(string value)
        {
            var builder = new StringBuilder();
            foreach (char character in value ?? string.Empty)
            {
                switch (character)
                {
                    case '"': builder.Append("\\\""); break;
                    case '\\': builder.Append("\\\\"); break;
                    case '\b': builder.Append("\\b"); break;
                    case '\f': builder.Append("\\f"); break;
                    case '\n': builder.Append("\\n"); break;
                    case '\r': builder.Append("\\r"); break;
                    case '\t': builder.Append("\\t"); break;
                    default: builder.Append(character); break;
                }
            }
            return builder.ToString();
        }

        readonly struct DimensionDefinition
        {
            internal DimensionDefinition(
                string id,
                string name,
                string ruleId,
                double weight)
            {
                Id = id;
                Name = name;
                RuleId = ruleId;
                Weight = weight;
            }

            internal string Id { get; }
            internal string Name { get; }
            internal string RuleId { get; }
            internal double Weight { get; }
        }

        readonly struct DimensionSnapshot
        {
            internal DimensionSnapshot(
                DimensionDefinition definition,
                double weight,
                bool configured,
                DiagnosticRuleResult rule,
                CoverageSummary coverage,
                double? health,
                double? evidence)
            {
                Definition = definition;
                Weight = weight;
                Configured = configured;
                Rule = rule;
                Coverage = coverage;
                Health = health;
                Evidence = evidence;
            }

            internal DimensionDefinition Definition { get; }
            internal double Weight { get; }
            internal bool Configured { get; }
            internal DiagnosticRuleResult Rule { get; }
            internal CoverageSummary Coverage { get; }
            internal double? Health { get; }
            internal double? Evidence { get; }
        }

        readonly struct CoverageSummary
        {
            internal CoverageSummary(int eligible, int matched)
            {
                Eligible = eligible;
                Matched = matched;
            }

            internal int? Eligible { get; }
            internal int? Matched { get; }
            internal double? Rate => Eligible.HasValue && Eligible.Value > 0
                ? (double)Matched.Value / Eligible.Value
                : null;

            internal static CoverageSummary From(IReadOnlyList<Coverage> values)
            {
                int eligible = values.Sum(value => value.Eligible);
                int matched = values.Sum(value => value.Matched);
                return new CoverageSummary(eligible, matched);
            }
        }

        readonly struct Coverage
        {
            internal Coverage(
                string id,
                string dimension,
                ulong sequenceStart,
                ulong sequenceEnd,
                int eligible,
                int matched)
            {
                Id = id;
                Dimension = dimension;
                SequenceStart = sequenceStart;
                SequenceEnd = sequenceEnd;
                Eligible = eligible;
                Matched = matched;
            }

            internal string Id { get; }
            internal string Dimension { get; }
            internal ulong SequenceStart { get; }
            internal ulong SequenceEnd { get; }
            internal int Eligible { get; }
            internal int Matched { get; }
        }
    }
}
