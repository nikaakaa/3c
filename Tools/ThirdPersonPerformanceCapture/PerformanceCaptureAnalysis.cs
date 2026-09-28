using System.Globalization;
using System.Text;
using System.Text.Json;
using ThirdPersonPerformance;

namespace ThirdPersonPerformanceCapture.Controller;

internal static class PerformanceCaptureAnalysis
{
    static readonly JsonSerializerOptions JsonOptions = new() { IncludeFields = true, WriteIndented = true };

    sealed record Capture(string Path, string Hash, PerformanceCaptureManifestDocument Manifest, PerformanceSummaryDocument Summary);
    sealed record Measurement(string Metric, string Point, string Unit, string Scope, string Statistic, double Value, bool Available = true);

    static T Read<T>(string path) where T : class
    {
        using JsonDocument json = JsonDocument.Parse(File.ReadAllText(path, Encoding.UTF8));
        Require(json.RootElement.ValueKind == JsonValueKind.Object && json.RootElement.TryGetProperty("schema", out var schema) &&
            schema.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(schema.GetString()), $"Document schema is missing: {path}");
        return json.RootElement.Deserialize<T>(JsonOptions) ?? throw new InvalidDataException($"Empty document: {path}");
    }

    static Capture ReadCapture(string manifestPath)
    {
        manifestPath = Path.GetFullPath(manifestPath);
        string hash = PerformanceFileUtility.Sha256(manifestPath);
        PerformanceCaptureManifestDocument manifest = Read<PerformanceCaptureManifestDocument>(manifestPath);
        Require(manifest.schema == PerformanceCaptureSchemas.Manifest && manifest.status == "Completed", "Capture must use the current schema and be Completed.");
        Require(!string.IsNullOrWhiteSpace(manifest.capture_id) && !string.IsNullOrWhiteSpace(manifest.build_id) &&
            !string.IsNullOrWhiteSpace(manifest.player_manifest_hash), "Capture identity is missing.");
        string root = Path.GetDirectoryName(manifestPath)!;
        string prefix = root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var roles = new Dictionary<string, string>(StringComparer.Ordinal);
        Require(manifest.files != null && manifest.files.Length > 0, "Capture has no artifact closure.");
        foreach (PerformanceFileDocument file in manifest.files)
        {
            Require(file != null && !string.IsNullOrWhiteSpace(file.path) && !Path.IsPathRooted(file.path), "Invalid artifact path.");
            string path = Path.GetFullPath(Path.Combine(root, file.path));
            Require(path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && paths.Add(path), $"Duplicate or escaped artifact: {file.path}");
            Require(File.Exists(path) && new FileInfo(path).Length == file.size && PerformanceFileUtility.Sha256(path) == file.sha256, $"Artifact does not match its manifest: {file.path}");
            if (file.role != "run-file")
                Require(roles.TryAdd(file.role, path), $"Duplicate artifact role: {file.role}");
        }
        string[] requiredRoles = { "summary", "comparison", "runtime-result", "metric-samples", "metric-catalog", "unity-profiler", "windows-cpu", "cpu-hotspots", "thread-stacks", "context-switches", "wpa-exporter-config", "xperf-marks", "xperf-stack-report", "player-log", "controller-log", "process", "request", "status" };
        foreach (string role in requiredRoles)
            Require(roles.TryGetValue(role, out string path) && new FileInfo(path).Length > 0, $"Missing artifact role: {role}");
        Require(PerformanceFileUtility.Sha256(roles["request"]) == manifest.request_hash, "Capture request hash does not match its manifest.");
        Require(manifest.instrumentation_mode is "Disabled" or "MarkerOnly" or "Span", "Unknown instrumentation mode.");
        Require(roles.ContainsKey("instrumentation-spans") == (manifest.instrumentation_mode == "Span"), "Span artifact does not match instrumentation mode.");
        PerformanceSummaryDocument summary = Read<PerformanceSummaryDocument>(roles["summary"]);
        ValidateSummary(manifest, summary);
        PerformanceRuntimeResultDocument runtime = Read<PerformanceRuntimeResultDocument>(roles["runtime-result"]);
        Require(runtime.schema == PerformanceCaptureSchemas.RuntimeResult && runtime.status == "Completed" &&
            runtime.operation == PerformanceOperationKinds.Capture && runtime.run_id == manifest.capture_id &&
            runtime.capture_seconds == summary.capture_seconds && runtime.instrumentation_identity == manifest.instrumentation_identity &&
            runtime.instrumentation_mode == manifest.instrumentation_mode, "Runtime evidence does not match the Capture summary.");
        Require(hash == PerformanceFileUtility.Sha256(manifestPath), "Capture manifest changed during analysis.");
        return new Capture(manifestPath, hash, manifest, summary);
    }

    static void ValidateSummary(PerformanceCaptureManifestDocument manifest, PerformanceSummaryDocument summary)
    {
        Require(summary.schema == PerformanceCaptureSchemas.Summary && manifest.statistics_schema == summary.schema &&
            summary.status == "Completed" && summary.capture_id == manifest.capture_id && summary.instrumentation_mode == manifest.instrumentation_mode,
            "Summary identity does not match its Capture.");
        Require(double.IsFinite(summary.capture_seconds) && summary.capture_seconds > 0, "Invalid Capture duration.");
        Require(summary.metrics != null && summary.instrumentation_points != null && summary.hotspots != null, "Summary collections are missing.");
        Require(summary.metrics.All(value => value != null && !string.IsNullOrWhiteSpace(value.metric_id) &&
            !string.IsNullOrWhiteSpace(value.unit) && !string.IsNullOrWhiteSpace(value.sample_scope) && Valid(value.distribution) && value.distribution.sample_count > 0), "Invalid metric distribution.");
        Require(summary.metrics.Select(value => value.metric_id).Distinct(StringComparer.Ordinal).Count() == summary.metrics.Length, "Duplicate metric identity.");
        Require(summary.instrumentation_points.All(value => value != null && !string.IsNullOrWhiteSpace(value.point_id) &&
            !string.IsNullOrWhiteSpace(value.metric_id) && Valid(value.distribution)), "Invalid instrumentation distribution.");
        Require(summary.instrumentation_points.Select(value => value.point_id).Distinct(StringComparer.Ordinal).Count() == summary.instrumentation_points.Length, "Duplicate instrumentation point.");
        Require(summary.hotspots.All(value => value != null && FiniteNonnegative(value.inclusive_samples) && FiniteNonnegative(value.exclusive_samples)), "Invalid hotspot sample count.");
        Require(FiniteNonnegative(summary.presentation_fps) && FiniteNonnegative(summary.logic_ticks_per_second) && summary.dropped_logic_ticks >= 0 &&
            FiniteNonnegative(summary.unresolved_exclusive_samples) && FiniteNonnegative(summary.total_exclusive_samples) &&
            summary.unresolved_exclusive_samples <= summary.total_exclusive_samples, "Invalid summary counters.");
    }

    static bool FiniteNonnegative(double value) => double.IsFinite(value) && value >= 0;
    static bool Valid(PerformanceDistributionDocument value) => value != null && value.sample_count >= 0 && value.invocation_count >= 0 &&
        FiniteNonnegative(value.mean) && FiniteNonnegative(value.p50) && FiniteNonnegative(value.p95) && FiniteNonnegative(value.p99) &&
        FiniteNonnegative(value.max) && value.p50 <= value.p95 && value.p95 <= value.p99 && value.p99 <= value.max &&
        (value.sample_count > 0 || value.invocation_count == 0 && value.mean == 0 && value.max == 0);

    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidDataException(message);
    }

    static string[] Conflicts(PerformanceCaptureManifestDocument baseline, PerformanceCaptureManifestDocument candidate)
    {
        var conflicts = new List<string>();
        void Match(string name, object before, object after)
        {
            if ((before is string text && string.IsNullOrWhiteSpace(text)) || !Equals(before, after))
                conflicts.Add($"{name}: baseline='{before}', candidate='{after}'");
        }
        Match("schema", baseline.schema, candidate.schema);
        Match("scenario_hash", baseline.scenario_hash, candidate.scenario_hash);
        Match("runtime_id", baseline.runtime_id, candidate.runtime_id);
        Match("roster_identity", baseline.roster_identity, candidate.roster_identity);
        Match("toolchain_identity", baseline.toolchain_identity, candidate.toolchain_identity);
        Match("hardware_identity", baseline.hardware_identity, candidate.hardware_identity);
        Match("metric_catalog_revision", baseline.metric_catalog_revision, candidate.metric_catalog_revision);
        Match("instrumentation_identity", baseline.instrumentation_identity, candidate.instrumentation_identity);
        Match("instrumentation_mode", baseline.instrumentation_mode, candidate.instrumentation_mode);
        Match("instrumentation_span_layout_revision", baseline.instrumentation_span_layout_revision, candidate.instrumentation_span_layout_revision);
        Match("build_mode", baseline.build_mode, candidate.build_mode);
        Match("wpr_profile_hash", baseline.wpr_profile_hash, candidate.wpr_profile_hash);
        Match("statistics_schema", baseline.statistics_schema, candidate.statistics_schema);
        Match("budget_hash", baseline.budget_hash, candidate.budget_hash);
        Match("capture_profile_hash", baseline.capture_profile_hash, candidate.capture_profile_hash);
        Match("width", baseline.width, candidate.width);
        Match("height", baseline.height, candidate.height);
        Match("quality_level", baseline.quality_level, candidate.quality_level);
        Match("v_sync_count", baseline.v_sync_count, candidate.v_sync_count);
        Match("target_frame_rate", baseline.target_frame_rate, candidate.target_frame_rate);
        if (baseline.build_mode != "Development" || candidate.build_mode != "Development")
            conflicts.Add("Development Player Captures are required.");
        return conflicts.ToArray();
    }

    static string DeltaStatus(double before, double after) => before == 0 ? after == 0 ? "BothZero" : "BaselineZero" : "Comparable";
    static double Percent(double before, double after) => before == 0 ? 0 : (after - before) / before * 100;
    static string HotspotKey(PerformanceFunctionHotspotDocument value) => value.thread + "\n" + value.module + "\n" + value.function;

    public static PerformanceComparisonDocument Compare(string baselinePath, PerformanceCaptureManifestDocument candidateManifest, PerformanceSummaryDocument candidate)
    {
        ValidateSummary(candidateManifest, candidate);
        var result = new PerformanceComparisonDocument { candidate_capture_id = candidateManifest.capture_id };
        if (string.IsNullOrWhiteSpace(baselinePath))
        {
            result.status = "NotRequested";
            result.message = "No Baseline Capture was selected.";
            return result;
        }
        try
        {
            Capture capture = ReadCapture(baselinePath);
            result.baseline_manifest_path = capture.Path;
            result.baseline_manifest_hash = capture.Hash;
            PerformanceSummaryDocument baseline = capture.Summary;
            result.baseline_capture_id = baseline.capture_id;
            Require(baseline.capture_id != candidate.capture_id, "Cannot compare a Capture with itself.");
            string[] conflicts = Conflicts(capture.Manifest, candidateManifest);
            Require(conflicts.Length == 0, string.Join("; ", conflicts));
            var beforeMetrics = baseline.metrics.ToDictionary(value => value.metric_id, StringComparer.Ordinal);
            var afterMetrics = candidate.metrics.ToDictionary(value => value.metric_id, StringComparer.Ordinal);
            result.metrics = beforeMetrics.Keys.Union(afterMetrics.Keys, StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).Select(id =>
            {
                beforeMetrics.TryGetValue(id, out var before);
                afterMetrics.TryGetValue(id, out var after);
                var row = new PerformanceComparisonMetricDocument { metric_id = id, unit = (after ?? before).unit, sample_scope = (after ?? before).sample_scope,
                    baseline_sample_count = before?.distribution.sample_count ?? 0, candidate_sample_count = after?.distribution.sample_count ?? 0 };
                row.status = before == null ? "MissingBaseline" : after == null ? "MissingCandidate" : before.unit != after.unit || before.sample_scope != after.sample_scope ? "DefinitionMismatch" : DeltaStatus(before.distribution.p95, after.distribution.p95);
                if (before != null) row.baseline_p95 = before.distribution.p95;
                if (after != null) row.candidate_p95 = after.distribution.p95;
                if (row.status is "Comparable" or "BothZero" or "BaselineZero")
                {
                    row.absolute_delta = row.candidate_p95 - row.baseline_p95;
                    row.percent_delta_available = row.baseline_p95 != 0;
                    row.percent_delta = Percent(row.baseline_p95, row.candidate_p95);
                }
                return row;
            }).ToArray();
            var beforePoints = baseline.instrumentation_points.ToDictionary(value => value.point_id, StringComparer.Ordinal);
            var afterPoints = candidate.instrumentation_points.ToDictionary(value => value.point_id, StringComparer.Ordinal);
            result.instrumentation_points = beforePoints.Keys.Union(afterPoints.Keys, StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).Select(id =>
            {
                beforePoints.TryGetValue(id, out var before);
                afterPoints.TryGetValue(id, out var after);
                var row = new PerformanceInstrumentationPointComparisonDocument { point_id = id, metric_id = (after ?? before).metric_id,
                    baseline_sample_count = before?.distribution.sample_count ?? 0, candidate_sample_count = after?.distribution.sample_count ?? 0,
                    baseline_invocation_count = before?.distribution.invocation_count ?? 0, candidate_invocation_count = after?.distribution.invocation_count ?? 0,
                    baseline_p95 = before?.distribution.p95 ?? 0, candidate_p95 = after?.distribution.p95 ?? 0 };
                row.status = before == null ? "MissingBaseline" : after == null ? "MissingCandidate" : before.metric_id != after.metric_id ? "DefinitionMismatch" : DeltaStatus(row.baseline_p95, row.candidate_p95);
                if (before != null && after != null && before.metric_id == after.metric_id)
                {
                    if (row.baseline_sample_count == 0 && row.candidate_sample_count == 0) row.status = "NoSamples";
                    else if (row.baseline_sample_count == 0) row.status = "NoBaselineSamples";
                    else if (row.candidate_sample_count == 0) row.status = "NoCandidateSamples";
                }
                if (row.status is "Comparable" or "BothZero" or "BaselineZero")
                {
                    row.absolute_delta = row.candidate_p95 - row.baseline_p95;
                    row.percent_delta_available = row.baseline_p95 != 0;
                    row.percent_delta = Percent(row.baseline_p95, row.candidate_p95);
                }
                return row;
            }).ToArray();
            var beforeHotspots = baseline.hotspots.GroupBy(HotspotKey).ToDictionary(group => group.Key, group => group.Sum(value => value.inclusive_samples), StringComparer.Ordinal);
            var afterHotspots = candidate.hotspots.GroupBy(HotspotKey).ToDictionary(group => group.Key, group => group.Sum(value => value.inclusive_samples), StringComparer.Ordinal);
            result.hotspots = beforeHotspots.Keys.Union(afterHotspots.Keys, StringComparer.Ordinal).Select(key =>
            {
                beforeHotspots.TryGetValue(key, out double before);
                afterHotspots.TryGetValue(key, out double after);
                string[] identity = key.Split('\n');
                double beforeRate = before / baseline.capture_seconds;
                double afterRate = after / candidate.capture_seconds;
                return new PerformanceHotspotComparisonDocument { thread = identity[0], module = identity[1], function = identity[2],
                    baseline_inclusive_samples = before, candidate_inclusive_samples = after,
                    baseline_samples_per_second = beforeRate, candidate_samples_per_second = afterRate,
                    absolute_delta = afterRate - beforeRate, percent_delta_available = beforeRate != 0, percent_delta = Percent(beforeRate, afterRate), status = DeltaStatus(beforeRate, afterRate) };
            }).OrderByDescending(value => Math.Abs(value.absolute_delta)).ThenBy(value => value.function, StringComparer.Ordinal).Take(200).ToArray();
            result.baseline_budget_evaluated = baseline.budget_evaluated;
            result.candidate_budget_evaluated = candidate.budget_evaluated;
            result.baseline_budget_passed = baseline.budget_passed;
            result.candidate_budget_passed = candidate.budget_passed;
            result.budget_delta_available = baseline.budget_evaluated && candidate.budget_evaluated;
            result.budget_exceeded_delta = result.budget_delta_available ? candidate.budget_exceeded_count - baseline.budget_exceeded_count : 0;
            result.warnings = Warnings(new[] { baseline, candidate }).Append("Single-run differences do not establish a repeatable improvement. Hotspot deltas normalize samples by Player capture_seconds, not CPU milliseconds. The ETW marker window also includes Controller/Player handshake boundaries; an absent sampled hotspot is not proof of no execution.").ToArray();
            bool unavailable = result.metrics.Any(value => value.status.StartsWith("Missing", StringComparison.Ordinal) || value.status == "DefinitionMismatch") ||
                result.instrumentation_points.Any(value => value.status.StartsWith("Missing", StringComparison.Ordinal) || value.status is "DefinitionMismatch" or "NoBaselineSamples" or "NoCandidateSamples");
            result.status = unavailable ? "Incomplete" : "Comparable";
            result.message = unavailable ? "Capture identities match; some measurements cannot be compared. Inspect row statuses." : "Capture identities match. Inspect sample counts and warnings before interpreting differences.";
        }
        catch (Exception exception) when (exception is IOException or InvalidDataException or JsonException or ArgumentException or UnauthorizedAccessException)
        {
            result.status = "Rejected";
            result.message = exception.Message;
        }
        return result;
    }

    static IEnumerable<string> Warnings(IEnumerable<PerformanceSummaryDocument> summaries)
    {
        foreach (PerformanceSummaryDocument summary in summaries)
        {
            if (!summary.budget_evaluated) yield return $"{summary.capture_id}: budget evaluation is incomplete.";
            if (summary.dropped_logic_ticks > 0) yield return $"{summary.capture_id}: dropped LogicTicks={summary.dropped_logic_ticks}.";
            if (summary.total_exclusive_samples == 0) yield return $"{summary.capture_id}: no exclusive CPU samples.";
            if (summary.unresolved_exclusive_samples > 0) yield return $"{summary.capture_id}: unresolved exclusive samples={summary.unresolved_exclusive_samples.ToString("R", CultureInfo.InvariantCulture)}/{summary.total_exclusive_samples.ToString("R", CultureInfo.InvariantCulture)}.";
            int unobserved = summary.instrumentation_points.Count(value => value.distribution.sample_count == 0);
            if (unobserved > 0) yield return $"{summary.capture_id}: {unobserved} instrumentation points were not invoked; no latency can be inferred for them.";
            long exceptions = summary.instrumentation_points.Sum(value => value.exception_count);
            if (exceptions > 0) yield return $"{summary.capture_id}: {exceptions} instrumentation spans ended with exceptions.";
            foreach (var metric in summary.metrics.Where(value => value.distribution.sample_count < 100))
                yield return $"{summary.capture_id}: {metric.metric_id} has only {metric.distribution.sample_count} samples; tail percentiles are coarse.";
        }
    }

    static IEnumerable<Measurement> Measurements(PerformanceSummaryDocument summary)
    {
        foreach (var metric in summary.metrics)
        {
            yield return new Measurement(metric.metric_id, "", metric.unit, metric.sample_scope, "p95", metric.distribution.p95);
            yield return new Measurement(metric.metric_id, "", metric.unit, metric.sample_scope, "p99", metric.distribution.p99);
        }
        foreach (var point in summary.instrumentation_points)
            yield return new Measurement(point.metric_id, point.point_id, "Milliseconds", "Invocation", "p95", point.distribution.p95, point.distribution.sample_count > 0);
        yield return new Measurement("capture.presentation-fps", "", "FramesPerSecond", "Capture", "value", summary.presentation_fps);
        yield return new Measurement("capture.dropped-logic-ticks", "", "Count", "Capture", "value", summary.dropped_logic_ticks);
    }

    static string Key(Measurement measurement) => measurement.Point + "\n" + measurement.Metric + "\n" + measurement.Statistic;
    static double Median(double[] sorted) => sorted.Length % 2 == 0 ? sorted[sorted.Length / 2 - 1] / 2 + sorted[sorted.Length / 2] / 2 : sorted[sorted.Length / 2];
    static PerformanceRunDistributionDocument Distribution(double[] values)
    {
        if (values.Length == 0) return new PerformanceRunDistributionDocument();
        double[] sorted = values.OrderBy(value => value).ToArray();
        double median = Median(sorted);
        return new PerformanceRunDistributionDocument { values = values, median = median, min = sorted[0], max = sorted[^1],
            median_absolute_deviation = Median(values.Select(value => Math.Abs(value - median)).OrderBy(value => value).ToArray()) };
    }

    public static int Run(string requestPath, string outputRoot)
    {
        Require(!string.IsNullOrWhiteSpace(outputRoot), "Offline analysis requires --analysis-output=<new directory>.");
        outputRoot = Path.GetFullPath(outputRoot);
        Require(!Directory.Exists(outputRoot) && !File.Exists(outputRoot), "Analysis output already exists; evidence cannot be overwritten.");
        byte[] requestBytes = File.ReadAllBytes(requestPath);
        var report = new PerformanceAnalysisDocument { created_utc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture), request_hash = PerformanceFileUtility.Sha256(requestBytes) };
        try
        {
            using JsonDocument requestJson = JsonDocument.Parse(Encoding.UTF8.GetString(requestBytes).TrimStart('\ufeff'));
            Require(requestJson.RootElement.ValueKind == JsonValueKind.Object && requestJson.RootElement.TryGetProperty("schema", out var requestSchema) &&
                requestSchema.ValueKind == JsonValueKind.String && requestSchema.GetString() == PerformanceCaptureSchemas.AnalysisRequest, "Invalid analysis request schema.");
            var request = requestJson.RootElement.Deserialize<PerformanceAnalysisRequestDocument>(JsonOptions);
            Require(request != null && request.schema == PerformanceCaptureSchemas.AnalysisRequest, "Invalid analysis request schema.");
            Require(request.baseline_manifest_paths != null && request.candidate_manifest_paths != null &&
                request.baseline_manifest_paths.Length is > 0 and <= 100 && request.candidate_manifest_paths.Length is > 0 and <= 100, "Each group requires 1–100 explicitly selected Capture manifests.");
            string[] paths = request.baseline_manifest_paths.Concat(request.candidate_manifest_paths).ToArray();
            Require(paths.All(path => !string.IsNullOrWhiteSpace(path) && Path.IsPathFullyQualified(path)), "Capture manifest paths must be absolute.");
            Require(paths.Select(Path.GetFullPath).Distinct(StringComparer.OrdinalIgnoreCase).Count() == paths.Length, "A Capture cannot appear more than once or in both groups.");
            Capture[] captures = paths.Select(ReadCapture).ToArray();
            int baselineCount = request.baseline_manifest_paths.Length;
            Require(captures.Select(value => value.Manifest.capture_id).Distinct(StringComparer.Ordinal).Count() == captures.Length, "Copied Captures cannot count as independent runs.");
            report.sources = captures.Select((value, index) => new PerformanceAnalysisSourceDocument { group = index < baselineCount ? "Baseline" : "Candidate", manifest_path = value.Path,
                manifest_hash = value.Hash, capture_id = value.Manifest.capture_id, build_id = value.Manifest.build_id,
                capture_seconds = value.Summary.capture_seconds, dropped_logic_ticks = value.Summary.dropped_logic_ticks,
                budget_evaluated = value.Summary.budget_evaluated, budget_passed = value.Summary.budget_passed }).ToArray();
            for (int i = 0; i < captures.Length; i++)
            {
                string[] conflicts = Conflicts(captures[0].Manifest, captures[i].Manifest);
                Require(conflicts.Length == 0, $"{captures[i].Manifest.capture_id}: {string.Join("; ", conflicts)}");
                var groupFirst = captures[i < baselineCount ? 0 : baselineCount].Manifest;
                Require(groupFirst.build_id == captures[i].Manifest.build_id && groupFirst.player_manifest_hash == captures[i].Manifest.player_manifest_hash,
                    "Each repeat group must use one exact Player build. Build changes are allowed only between groups.");
            }
            var tables = captures.Select(value => Measurements(value.Summary).ToDictionary(Key, StringComparer.Ordinal)).ToArray();
            string[] keys = tables.SelectMany(value => value.Keys).Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray();
            report.metrics = keys.Select(key =>
            {
                Measurement definition = tables.First(value => value.ContainsKey(key))[key];
                int[] unavailable = Enumerable.Range(0, captures.Length).Where(i => !tables[i].TryGetValue(key, out var value) || !value.Available || value.Unit != definition.Unit || value.Scope != definition.Scope).ToArray();
                double[] before = Enumerable.Range(0, baselineCount).Where(i => !unavailable.Contains(i)).Select(i => tables[i][key].Value).ToArray();
                double[] after = Enumerable.Range(baselineCount, captures.Length - baselineCount).Where(i => !unavailable.Contains(i)).Select(i => tables[i][key].Value).ToArray();
                var row = new PerformanceAnalysisMetricDocument { metric_id = definition.Metric, point_id = definition.Point, unit = definition.Unit, statistic = definition.Statistic,
                    sample_scope = definition.Scope,
                    baseline = Distribution(before), candidate = Distribution(after), unavailable_capture_ids = unavailable.Select(i => captures[i].Manifest.capture_id).ToArray() };
                if (unavailable.Length > 0)
                    row.status = tables.All(table => table.TryGetValue(key, out var value) && !value.Available && value.Unit == definition.Unit && value.Scope == definition.Scope) ? "NotObserved" : "Incomplete";
                else
                {
                    row.absolute_delta = row.candidate.median - row.baseline.median;
                    row.percent_delta_available = row.baseline.median != 0;
                    row.percent_delta = Percent(row.baseline.median, row.candidate.median);
                    row.status = before.Length < 3 || after.Length < 3 ? "InsufficientRepeats" :
                        row.baseline.max == row.baseline.min && row.baseline.min == row.candidate.min && row.candidate.min == row.candidate.max ? "IdenticalObservedValues" :
                        row.candidate.max < row.baseline.min ? "CandidateRangeLower" : row.candidate.min > row.baseline.max ? "CandidateRangeHigher" : "ObservedRangesOverlap";
                }
                return row;
            }).ToArray();
            report.warnings = Warnings(captures.Select(value => value.Summary)).Append("Run-level descriptive statistics only: frames from different runs are not pooled. Range separation is not a confidence interval or a significance test; 3 runs is a minimum reporting threshold, not proof of sufficient power. No automatic outlier removal or performance pass/fail.").ToArray();
            report.status = report.metrics.Any(value => value.status == "Incomplete") ? "Incomplete" :
                baselineCount < 3 || captures.Length - baselineCount < 3 ? "InsufficientRepeats" : "Comparable";
            report.message = "Values follow source order within each group. Deltas compare group medians; lower FPS is worse, while lower time or allocation is better. Instrumentation modes must match; this report does not calibrate instrumentation overhead.";
        }
        catch (Exception exception) when (exception is IOException or InvalidDataException or JsonException or ArgumentException or UnauthorizedAccessException)
        {
            report.status = "Rejected";
            report.message = exception.Message;
        }
        string staging = outputRoot + "." + Guid.NewGuid().ToString("N") + ".staging";
        Directory.CreateDirectory(staging);
        File.WriteAllBytes(Path.Combine(staging, "request.json"), requestBytes);
        PerformanceFileUtility.WriteJson(Path.Combine(staging, "analysis.json"), report, JsonOptions);
        File.WriteAllText(Path.Combine(staging, "analysis.md"), Markdown(report), new UTF8Encoding(false));
        Directory.Move(staging, outputRoot);
        Console.WriteLine($"{report.status}: {Path.Combine(outputRoot, "analysis.json")}");
        return report.status == "Rejected" ? 2 : 0;
    }

    static string Markdown(PerformanceAnalysisDocument report)
    {
        static string Cell(string text) => (text ?? "").Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ");
        static string Number(double value) => value.ToString("G6", CultureInfo.InvariantCulture);
        var text = new StringBuilder("# 性能重复采集比较\n\n");
        text.AppendLine($"状态：**{report.status}**\n\n{report.message}\n");
        text.AppendLine("每次采集权重相同；范围为实际观测的最小值与最大值，并非置信区间。变化百分比不可用时显示 `—`。原始值、采集身份和完整状态见同目录 analysis.json。\n");
        text.AppendLine("| 组别 | Capture | Build | 预算 |\n|---|---|---|---|");
        foreach (var source in report.sources)
            text.AppendLine($"| {source.group} | {Cell(source.capture_id)} | {Cell(source.build_id)} | {(source.budget_evaluated ? source.budget_passed ? "通过" : "超预算" : "未完整评估")} |");
        text.AppendLine("\n| 指标 / 采样点 | 统计 / 单位 | 基线中位数 [范围] | 候选中位数 [范围] | 中位数差 | 变化 | 状态 |\n|---|---|---|---|---|---|---|");
        foreach (var row in report.metrics)
        {
            string before = row.baseline.values.Length == 0 ? "—" : $"{Number(row.baseline.median)} [{Number(row.baseline.min)}, {Number(row.baseline.max)}]";
            string after = row.candidate.values.Length == 0 ? "—" : $"{Number(row.candidate.median)} [{Number(row.candidate.min)}, {Number(row.candidate.max)}]";
            text.AppendLine($"| {Cell(row.metric_id)} {Cell(row.point_id)} | {row.statistic} / {Cell(row.unit)} | {before} | {after} | {(row.status is "Incomplete" or "NotObserved" ? "—" : Number(row.absolute_delta))} | {(row.percent_delta_available ? Number(row.percent_delta) + "%" : "—")} | {row.status} |");
        }
        text.AppendLine("\n## 证据限制\n");
        foreach (string warning in report.warnings) text.AppendLine($"- {warning}");
        return text.ToString();
    }
}
