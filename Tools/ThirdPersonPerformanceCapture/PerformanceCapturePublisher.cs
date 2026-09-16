using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using ThirdPersonPerformance;
using ThirdPersonPerformance.Instrumentation;

namespace ThirdPersonPerformanceCapture.Controller;

internal static class PerformanceCapturePublisher
{
    static readonly JsonSerializerOptions JsonOptions = new()
    {
        IncludeFields = true,
        WriteIndented = true,
        PropertyNameCaseInsensitive = false
    };

    sealed class MetricAccumulator
    {
        public readonly List<double> Values = new();
        public long Invocations;
    }

    sealed class MetricSampleSet
    {
        public readonly Dictionary<string, MetricAccumulator> Metrics = new(StringComparer.Ordinal);
        public readonly Dictionary<ulong, double> LogicFrameTotals = new();
    }

    sealed class WaitAccumulator
    {
        public string Process = string.Empty;
        public string Thread = string.Empty;
        public double Ready;
        public double Wait;
        public long Count;
    }

    sealed class InstrumentationPoint
    {
        public ulong PointId;
        public string PointIdText = string.Empty;
        public string MetricId = string.Empty;
        public string Assembly = string.Empty;
        public string DeclaringType = string.Empty;
        public string Method = string.Empty;
        public string SourceFile = string.Empty;
        public int SourceLine;
    }

    sealed class InstrumentationActorAccumulator
    {
        public readonly string ActorId;
        public readonly List<double> Values = new();
        public long Invocations;
        public long Exceptions;

        public InstrumentationActorAccumulator(string actorId)
        {
            ActorId = actorId;
        }
    }

    sealed class InstrumentationPointAccumulator
    {
        public readonly InstrumentationPoint Point;
        public readonly List<double> Values = new();
        public readonly Dictionary<string, InstrumentationActorAccumulator> Actors = new(StringComparer.Ordinal);
        public readonly List<PerformanceInstrumentationSpanSampleDocument> Slowest = new();
        public long Exceptions;

        public InstrumentationPointAccumulator(InstrumentationPoint point)
        {
            Point = point;
        }
    }

    public static void PublishGateCompleted(
        PerformanceRunRequestDocument request,
        PerformanceScenarioDocument scenario,
        PerformanceCaptureProfileDocument profile,
        PerformancePlayerManifestDocument playerManifest,
        string playerManifestHash,
        int playerProcessId,
        int playerExitCode,
        int controllerProcessId,
        string controllerLog)
    {
        WriteText(Path.Combine(request.staging_root, "controller.log"), controllerLog);
        var process = new PerformanceProcessDocument
        {
            player_process_id = playerProcessId,
            player_exit_code = playerExitCode,
            controller_process_id = controllerProcessId,
            controller_exit_code = 0
        };
        WriteJson(Path.Combine(request.staging_root, "process.json"), process);
        string runtimePath = Path.Combine(request.staging_root, "runtime-result.json");
        RequireNonEmptyFile(runtimePath, "runtime result");
        RequireNonEmptyFile(Path.Combine(request.staging_root, "player.log"), "player log");
        PerformanceRuntimeResultDocument runtime = ReadJson<PerformanceRuntimeResultDocument>(runtimePath);
        if (!string.Equals(runtime.schema, PerformanceCaptureSchemas.RuntimeResult, StringComparison.Ordinal) ||
            !string.Equals(runtime.operation, request.operation, StringComparison.Ordinal) ||
            !string.Equals(runtime.run_id, request.run_id, StringComparison.Ordinal) ||
            !string.Equals(runtime.status, PerformanceCaptureStatus.Completed.ToString(), StringComparison.Ordinal))
        {
            throw new InvalidDataException("Performance Gate runtime result identity is invalid.");
        }
        ValidateInstrumentation(request, profile, playerManifest, runtime, false);
        if (string.Equals(request.operation, PerformanceOperationKinds.Smoke, StringComparison.Ordinal))
        {
            if (runtime.logic_ticks != 0)
                throw new InvalidDataException("Performance Smoke Gate advanced replay LogicTicks.");
        }
        else if (string.Equals(request.operation, PerformanceOperationKinds.Replay, StringComparison.Ordinal))
        {
            if (runtime.logic_ticks != scenario.warmup_logic_ticks + scenario.capture_logic_ticks ||
                string.IsNullOrWhiteSpace(runtime.start_body_hash) ||
                string.IsNullOrWhiteSpace(runtime.input_sequence_hash) ||
                string.IsNullOrWhiteSpace(runtime.body_trajectory_hash))
            {
                throw new InvalidDataException("Performance Replay Gate evidence is incomplete.");
            }
        }
        else
        {
            throw new InvalidDataException($"Performance Gate operation '{request.operation}' is unsupported.");
        }
        PerformanceGateManifestDocument manifest = CreateGateManifest(
            request,
            scenario,
            profile,
            playerManifest,
            playerManifestHash,
            process,
            runtime,
            PerformanceCaptureStatus.Completed,
            "completed",
            $"Performance {request.operation} Gate completed.");
        manifest.files = PerformanceFileUtility.BuildClosure(request.staging_root, "manifest.json");
        WriteJson(Path.Combine(request.staging_root, "manifest.json"), manifest);
        PublishDirectory(request.staging_root, request.result_root);
    }

    public static void PublishGateTerminal(
        PerformanceRunRequestDocument request,
        PerformanceScenarioDocument scenario,
        PerformanceCaptureProfileDocument profile,
        PerformancePlayerManifestDocument playerManifest,
        string playerManifestHash,
        PerformanceCaptureStatus status,
        string stage,
        string message,
        int playerProcessId,
        int playerExitCode,
        int controllerProcessId,
        string controllerLog)
    {
        WriteText(Path.Combine(request.staging_root, "controller.log"), controllerLog);
        var process = new PerformanceProcessDocument
        {
            player_process_id = playerProcessId,
            player_exit_code = playerExitCode,
            controller_process_id = controllerProcessId,
            controller_exit_code = status == PerformanceCaptureStatus.Cancelled ? 3 : 2
        };
        WriteJson(Path.Combine(request.staging_root, "process.json"), process);
        PerformanceRuntimeResultDocument runtime = TryReadRuntimeResult(request.staging_root);
        PerformanceGateManifestDocument manifest = CreateGateManifest(
            request,
            scenario,
            profile,
            playerManifest,
            playerManifestHash,
            process,
            runtime,
            status,
            stage,
            message);
        manifest.files = PerformanceFileUtility.BuildClosure(request.staging_root, "manifest.json");
        WriteJson(Path.Combine(request.staging_root, "manifest.json"), manifest);
        PublishDirectory(request.staging_root, request.result_root);
    }

    public static void PublishCompleted(
        PerformanceRunRequestDocument request,
        PerformanceScenarioDocument scenario,
        PerformanceCaptureProfileDocument profile,
        PerformanceBudgetDocument budget,
        PerformancePlayerManifestDocument playerManifest,
        PerformanceToolchainDocument toolchain,
        string requestHash,
        string playerManifestHash,
        string toolchainHash,
        int playerProcessId,
        int playerExitCode,
        int controllerProcessId,
        string wprInstance,
        string controllerLog)
    {
        WriteText(Path.Combine(request.staging_root, "controller.log"), controllerLog);
        var process = new PerformanceProcessDocument
        {
            player_process_id = playerProcessId,
            player_exit_code = playerExitCode,
            controller_process_id = controllerProcessId,
            controller_exit_code = 0,
            wpr_instance = wprInstance
        };
        WriteJson(Path.Combine(request.staging_root, "process.json"), process);
        RequireCompletedArtifacts(request.staging_root);
        PerformanceRuntimeResultDocument runtime = ReadJson<PerformanceRuntimeResultDocument>(
            Path.Combine(request.staging_root, "runtime-result.json"));
        if (!string.Equals(runtime.schema, PerformanceCaptureSchemas.RuntimeResult, StringComparison.Ordinal) ||
            !string.Equals(runtime.operation, PerformanceOperationKinds.Capture, StringComparison.Ordinal) ||
            !string.Equals(runtime.run_id, request.run_id, StringComparison.Ordinal) ||
            !string.Equals(runtime.status, PerformanceCaptureStatus.Completed.ToString(), StringComparison.Ordinal) ||
            runtime.logic_ticks != scenario.capture_logic_ticks || runtime.capture_seconds <= 0d ||
            string.IsNullOrWhiteSpace(runtime.operating_system) || string.IsNullOrWhiteSpace(runtime.processor) ||
            string.IsNullOrWhiteSpace(runtime.graphics_device) || string.IsNullOrWhiteSpace(runtime.graphics_api))
        {
            throw new InvalidDataException("Performance runtime result is not a completed Scenario.");
        }
        ValidateInstrumentation(request, profile, playerManifest, runtime, true);
        PerformanceMetricCatalogDocument catalog = ReadJson<PerformanceMetricCatalogDocument>(
            Path.Combine(request.staging_root, "metric-catalog.json"));
        if (!string.Equals(catalog.schema, "third-person-performance-metric-catalog/1", StringComparison.Ordinal) ||
            !string.Equals(catalog.revision, runtime.metric_catalog_revision, StringComparison.Ordinal) ||
            catalog.metrics == null || catalog.metrics.Length == 0)
        {
            throw new InvalidDataException("Performance metric catalog identity is invalid.");
        }
        PerformanceSummaryDocument summary = BuildSummary(request, scenario, budget, playerManifest, runtime, catalog, profile);
        WriteJson(Path.Combine(request.staging_root, "summary.json"), summary);
        PerformanceComparisonDocument comparison = BuildComparison(request, scenario, summary, runtime);
        WriteJson(Path.Combine(request.staging_root, "comparison.json"), comparison);
        RequireNonEmptyFile(Path.Combine(request.staging_root, "summary.json"), "summary");
        RequireNonEmptyFile(Path.Combine(request.staging_root, "comparison.json"), "comparison");
        var manifest = CreateManifest(
            request,
            scenario,
            playerManifest,
            toolchain,
            requestHash,
            playerManifestHash,
            toolchainHash,
            process,
            runtime,
            PerformanceCaptureStatus.Completed,
            "completed",
            "Performance Capture completed.");
        manifest.files = PerformanceFileUtility.BuildClosure(
            request.staging_root,
            "manifest.json");
        WriteJson(Path.Combine(request.staging_root, "manifest.json"), manifest);
        PublishDirectory(request.staging_root, request.result_root);
    }

    static void RequireCompletedArtifacts(string root)
    {
        string[] names =
        {
            "runtime-result.json",
            "metric-samples.csv",
            "metric-catalog.json",
            "instrumentation-spans.bin",
            "unity-profiler.raw",
            "windows-cpu.etl",
            "cpu-hotspots.csv",
            "thread-stacks.csv",
            "context-switches.csv",
            "wpa-exporter.json",
            "xperf-marks.csv",
            "xperf-stack.xhtml",
            "player.log",
            "controller.log",
            "process.json",
            "request.json",
            "status.json"
        };
        for (int i = 0; i < names.Length; i++)
            RequireNonEmptyFile(Path.Combine(root, names[i]), names[i]);
    }

    static void RequireNonEmptyFile(string path, string role)
    {
        if (!File.Exists(path) || new FileInfo(path).Length == 0L)
            throw new InvalidDataException($"Completed Performance Capture is missing '{role}'.");
    }

    static void ValidateInstrumentation(
        PerformanceRunRequestDocument request,
        PerformanceCaptureProfileDocument profile,
        PerformancePlayerManifestDocument playerManifest,
        PerformanceRuntimeResultDocument runtime,
        bool requireSpanFile)
    {
        if (string.IsNullOrWhiteSpace(request.instrumentation_identity) ||
            !string.Equals(request.instrumentation_identity, playerManifest.instrumentation_identity, StringComparison.Ordinal) ||
            !string.Equals(request.instrumentation_mode, playerManifest.instrumentation_mode, StringComparison.Ordinal) ||
            !string.Equals(runtime.instrumentation_identity, playerManifest.instrumentation_identity, StringComparison.Ordinal) ||
            !Enum.TryParse(playerManifest.instrumentation_mode, true, out PerformanceInstrumentationMode mode) ||
            !Enum.IsDefined(typeof(PerformanceInstrumentationMode), mode) ||
            mode == PerformanceInstrumentationMode.Disabled ||
            !string.Equals(runtime.instrumentation_mode, mode.ToString(), StringComparison.Ordinal) ||
            playerManifest.instrumentation_span_layout_revision != PerformanceInstrumentationIdentity.SpanLayoutRevision ||
            runtime.instrumentation_span_layout_revision != PerformanceInstrumentationIdentity.SpanLayoutRevision ||
            mode == PerformanceInstrumentationMode.Span && profile.instrumentation_span_capacity <= 0)
        {
            throw new InvalidDataException("Performance instrumentation identity is invalid.");
        }
        if (requireSpanFile)
        {
            if (mode != PerformanceInstrumentationMode.Span ||
                !string.Equals(runtime.instrumentation_span_path, "instrumentation-spans.bin", StringComparison.Ordinal) ||
                string.IsNullOrWhiteSpace(runtime.instrumentation_span_hash))
            {
                throw new InvalidDataException("Performance Capture instrumentation Span artifact is incomplete.");
            }
            string root = Path.GetFullPath(request.staging_root);
            string prefix = root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            string spanPath = Path.GetFullPath(Path.Combine(root, runtime.instrumentation_span_path));
            if (!spanPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Performance instrumentation Span artifact escapes the Capture staging root.");
            RequireNonEmptyFile(spanPath, "instrumentation spans");
            if (!string.Equals(PerformanceFileUtility.Sha256(spanPath), runtime.instrumentation_span_hash, StringComparison.Ordinal))
                throw new InvalidDataException("Performance instrumentation Span artifact hash is invalid.");
        }
        else if (!string.IsNullOrEmpty(runtime.instrumentation_span_path) ||
                 !string.IsNullOrEmpty(runtime.instrumentation_span_hash))
        {
            throw new InvalidDataException("Performance Smoke or Replay wrote an instrumentation Span artifact.");
        }
    }

    public static void PublishFaulted(
        PerformanceRunRequestDocument request,
        PerformanceScenarioDocument scenario,
        PerformancePlayerManifestDocument playerManifest,
        PerformanceToolchainDocument toolchain,
        string requestHash,
        string playerManifestHash,
        string toolchainHash,
        string stage,
        string message,
        int playerProcessId,
        int playerExitCode,
        int controllerProcessId,
        string wprInstance,
        string controllerLog)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.staging_root) || !Directory.Exists(request.staging_root))
            throw new InvalidOperationException("Performance fault has no staging directory.");
        WriteText(Path.Combine(request.staging_root, "controller.log"), controllerLog);
        var process = new PerformanceProcessDocument
        {
            player_process_id = playerProcessId,
            player_exit_code = playerExitCode,
            controller_process_id = controllerProcessId,
            controller_exit_code = 2,
            wpr_instance = wprInstance
        };
        WriteJson(Path.Combine(request.staging_root, "process.json"), process);
        PerformanceRuntimeResultDocument runtime = TryReadRuntimeResult(request.staging_root);
        var manifest = CreateManifest(
            request,
            scenario,
            playerManifest,
            toolchain,
            requestHash,
            playerManifestHash,
            toolchainHash,
            process,
            runtime,
            PerformanceCaptureStatus.Faulted,
            stage,
            message);
        manifest.files = PerformanceFileUtility.BuildClosure(
            request.staging_root,
            "manifest.json");
        WriteJson(Path.Combine(request.staging_root, "manifest.json"), manifest);
        PublishDirectory(request.staging_root, request.result_root);
    }

    public static void PublishCancelled(
        PerformanceRunRequestDocument request,
        PerformanceScenarioDocument scenario,
        PerformancePlayerManifestDocument playerManifest,
        PerformanceToolchainDocument toolchain,
        string requestHash,
        string playerManifestHash,
        string toolchainHash,
        int playerProcessId,
        int playerExitCode,
        int controllerProcessId,
        string wprInstance,
        string controllerLog)
    {
        WriteText(Path.Combine(request.staging_root, "controller.log"), controllerLog);
        var process = new PerformanceProcessDocument
        {
            player_process_id = playerProcessId,
            player_exit_code = playerExitCode,
            controller_process_id = controllerProcessId,
            controller_exit_code = 3,
            wpr_instance = wprInstance
        };
        WriteJson(Path.Combine(request.staging_root, "process.json"), process);
        PerformanceRuntimeResultDocument runtime = TryReadRuntimeResult(request.staging_root);
        var manifest = CreateManifest(
            request,
            scenario,
            playerManifest,
            toolchain,
            requestHash,
            playerManifestHash,
            toolchainHash,
            process,
            runtime,
            PerformanceCaptureStatus.Cancelled,
            "cancelled",
            "Performance Capture was cancelled by its owner.");
        manifest.files = PerformanceFileUtility.BuildClosure(request.staging_root, "manifest.json");
        WriteJson(Path.Combine(request.staging_root, "manifest.json"), manifest);
        PublishDirectory(request.staging_root, request.result_root);
    }

    static PerformanceSummaryDocument BuildSummary(
        PerformanceRunRequestDocument request,
        PerformanceScenarioDocument scenario,
        PerformanceBudgetDocument budget,
        PerformancePlayerManifestDocument playerManifest,
        PerformanceRuntimeResultDocument runtime,
        PerformanceMetricCatalogDocument catalog,
        PerformanceCaptureProfileDocument profile)
    {
        MetricSampleSet samples = ReadMetricSamples(
            Path.Combine(request.staging_root, "metric-samples.csv"));
        Dictionary<string, MetricAccumulator> accumulators = samples.Metrics;
        var summaries = new List<PerformanceMetricSummaryDocument>();
        double presentationFps = runtime.presentation_frames / runtime.capture_seconds;
        bool fpsExceeded = budget.target_fps > 0d && presentationFps < budget.target_fps;
        bool budgetPassed = runtime.dropped_logic_ticks <= budget.maximum_dropped_logic_ticks && !fpsExceeded;
        for (int i = 0; i < catalog.metrics.Length; i++)
        {
            PerformanceMetricDefinitionDocument definition = catalog.metrics[i];
            if (!accumulators.TryGetValue(definition.metric_id, out MetricAccumulator accumulator) ||
                accumulator.Values.Count == 0)
            {
                continue;
            }
            bool nanoseconds = string.Equals(definition.unit, "Nanoseconds", StringComparison.Ordinal);
            double scale = nanoseconds ? 1d / 1000000d : 1d;
            accumulator.Values.Sort();
            var distribution = new PerformanceDistributionDocument
            {
                sample_count = accumulator.Values.Count,
                invocation_count = accumulator.Invocations,
                mean = accumulator.Values.Average() * scale,
                p50 = Percentile(accumulator.Values, 0.50d) * scale,
                p95 = Percentile(accumulator.Values, 0.95d) * scale,
                p99 = Percentile(accumulator.Values, 0.99d) * scale,
                max = accumulator.Values[^1] * scale
            };
            PerformanceMetricBudgetDocument metricBudget = budget.metrics?.FirstOrDefault(
                value => string.Equals(value.metric_id, definition.metric_id, StringComparison.Ordinal));
            bool exceeded = metricBudget != null &&
                            (metricBudget.p95_limit > 0d && distribution.p95 > metricBudget.p95_limit ||
                             metricBudget.p99_limit > 0d && distribution.p99 > metricBudget.p99_limit);
            budgetPassed &= !exceeded;
            summaries.Add(new PerformanceMetricSummaryDocument
            {
                metric_id = definition.metric_id,
                profiler_name = definition.profiler_name,
                parent_id = definition.parent_id,
                sample_scope = definition.sample_scope,
                unit = nanoseconds ? "Milliseconds" : definition.unit,
                distribution = distribution,
                mean_per_invocation = accumulator.Invocations == 0
                    ? 0d
                    : accumulator.Values.Sum() * scale / accumulator.Invocations,
                budget_exceeded = exceeded
            });
        }
        PerformanceMetricBudgetDocument[] requiredBudgets = budget.metrics ?? Array.Empty<PerformanceMetricBudgetDocument>();
        for (int i = 0; i < requiredBudgets.Length; i++)
        {
            PerformanceMetricBudgetDocument required = requiredBudgets[i];
            PerformanceMetricSummaryDocument metric = summaries.FirstOrDefault(
                value => string.Equals(value.metric_id, required.metric_id, StringComparison.Ordinal));
            if (metric == null || !string.Equals(metric.unit, required.unit, StringComparison.Ordinal))
                throw new InvalidDataException($"Performance budget metric '{required.metric_id}' is missing or has another unit.");
        }
        var byId = summaries.ToDictionary(value => value.metric_id, StringComparer.Ordinal);
        for (int i = 0; i < summaries.Count; i++)
        {
            PerformanceMetricSummaryDocument metric = summaries[i];
            if (!string.IsNullOrEmpty(metric.parent_id) &&
                byId.TryGetValue(metric.parent_id, out PerformanceMetricSummaryDocument parent) &&
                parent.distribution.p95 > 0d && string.Equals(parent.unit, metric.unit, StringComparison.Ordinal))
            {
                metric.parent_p95_ratio = metric.distribution.p95 / parent.distribution.p95;
            }
        }
        PerformanceFunctionHotspotDocument[] hotspots = ReadHotspots(
            Path.Combine(request.staging_root, "cpu-hotspots.csv"));
        PerformanceThreadHotspotDocument[] threadHotspots = hotspots
            .GroupBy(value => new { value.process, value.thread })
            .Select(group => new PerformanceThreadHotspotDocument
            {
                process = group.Key.process,
                thread = group.Key.thread,
                inclusive_samples = group.Sum(value => value.inclusive_samples),
                exclusive_samples = group.Sum(value => value.exclusive_samples)
            })
            .OrderByDescending(value => value.inclusive_samples)
            .ThenBy(value => value.thread, StringComparer.Ordinal)
            .ToArray();
        PerformanceWaitEvidenceDocument[] waitEvidence = ReadWaitEvidence(
            Path.Combine(request.staging_root, "context-switches.csv"));
        return new PerformanceSummaryDocument
        {
            capture_id = request.run_id,
            status = PerformanceCaptureStatus.Completed.ToString(),
            presentation_fps = presentationFps,
            target_fps = budget.target_fps,
            fps_budget_exceeded = fpsExceeded,
            logic_ticks_per_second = runtime.logic_ticks / runtime.capture_seconds,
            dropped_logic_ticks = runtime.dropped_logic_ticks,
            budget_passed = budgetPassed,
            budget_exceeded_count = summaries.Count(value => value.budget_exceeded) +
                                    (runtime.dropped_logic_ticks > budget.maximum_dropped_logic_ticks ? 1 : 0) +
                                    (fpsExceeded ? 1 : 0),
            logic_tick_render_frame_distribution = BuildDistribution(
                samples.LogicFrameTotals.Values.ToList(),
                accumulators.TryGetValue("session.logic-tick", out MetricAccumulator logic) ? logic.Invocations : 0L,
                1d / 1000000d),
            metrics = summaries
                .OrderByDescending(value => value.distribution.p95)
                .ThenBy(value => value.metric_id, StringComparer.Ordinal)
                .ToArray(),
            instrumentation_points = ReadInstrumentationPointSummaries(
                request,
                playerManifest,
                runtime,
                profile,
                catalog),
            hotspots = hotspots,
            thread_hotspots = threadHotspots,
            wait_evidence = waitEvidence
        };
    }

    static PerformanceInstrumentationPointSummaryDocument[] ReadInstrumentationPointSummaries(
        PerformanceRunRequestDocument request,
        PerformancePlayerManifestDocument playerManifest,
        PerformanceRuntimeResultDocument runtime,
        PerformanceCaptureProfileDocument profile,
        PerformanceMetricCatalogDocument catalog)
    {
        string playerRoot = Path.GetDirectoryName(Path.GetFullPath(request.player_manifest_path))!;
        string manifestPath = Path.GetFullPath(Path.Combine(playerRoot, playerManifest.instrumentation_manifest_path));
        Dictionary<ulong, InstrumentationPoint> points = ReadInstrumentationManifest(
            manifestPath,
            playerManifest.instrumentation_identity,
            playerManifest.instrumentation_mode,
            runtime.metric_catalog_revision,
            catalog);
        var accumulators = points.Values.ToDictionary(
            value => value.PointId,
            value => new InstrumentationPointAccumulator(value));
        string spanPath = Path.GetFullPath(Path.Combine(request.staging_root, runtime.instrumentation_span_path));
        using FileStream stream = File.OpenRead(spanPath);
        using var reader = new BinaryReader(stream, Encoding.UTF8, true);
        if (!string.Equals(reader.ReadString(), PerformanceInstrumentationIdentity.SpanFileSchema, StringComparison.Ordinal) ||
            reader.ReadInt32() != PerformanceInstrumentationIdentity.SpanLayoutRevision)
        {
            throw new InvalidDataException("Performance instrumentation Span file schema is invalid.");
        }
        int count = reader.ReadInt32();
        if (count < 0 || count > profile.instrumentation_span_capacity)
            throw new InvalidDataException("Performance instrumentation Span file count exceeds its fixed capacity.");
        for (int i = 0; i < count; i++)
        {
            ulong sequence = reader.ReadUInt64();
            ulong pointId = reader.ReadUInt64();
            ulong metricId = reader.ReadUInt64();
            ulong renderFrame = reader.ReadUInt64();
            ulong logicTick = reader.ReadUInt64();
            ulong actorId = reader.ReadUInt64();
            _ = reader.ReadUInt64();
            _ = reader.ReadUInt64();
            _ = reader.ReadInt64();
            long durationTicks = reader.ReadInt64();
            _ = reader.ReadInt32();
            byte contextFlags = reader.ReadByte();
            byte endStateValue = reader.ReadByte();
            if (sequence != (ulong)i || durationTicks < 0 ||
                (endStateValue != (byte)PerformanceProbeEndState.Completed &&
                 endStateValue != (byte)PerformanceProbeEndState.Exception))
            {
                throw new InvalidDataException($"Performance instrumentation Span record {i} is invalid.");
            }
            if (!accumulators.TryGetValue(pointId, out InstrumentationPointAccumulator accumulator))
                throw new InvalidDataException($"Performance instrumentation Span PointId '{pointId:x16}' is not in the Player manifest.");
            if (metricId != PerformanceInstrumentationIdentity.Hash64(accumulator.Point.MetricId))
                throw new InvalidDataException($"Performance instrumentation Span MetricId for PointId '{pointId:x16}' is invalid.");
            PerformanceProbeEndState endState = (PerformanceProbeEndState)endStateValue;
            double durationMilliseconds = durationTicks * 1000d / Stopwatch.Frequency;
            accumulator.Values.Add(durationMilliseconds);
            if (endState == PerformanceProbeEndState.Exception)
                accumulator.Exceptions++;
            string actorKey = (contextFlags & (byte)PerformanceInstrumentationContextFlags.Actor) != 0
                ? actorId.ToString("x16", CultureInfo.InvariantCulture)
                : "global";
            if (!accumulator.Actors.TryGetValue(actorKey, out InstrumentationActorAccumulator actor))
            {
                actor = new InstrumentationActorAccumulator(actorKey);
                accumulator.Actors.Add(actorKey, actor);
            }
            actor.Values.Add(durationMilliseconds);
            actor.Invocations++;
            if (endState == PerformanceProbeEndState.Exception)
                actor.Exceptions++;
            AddSlowest(
                accumulator.Slowest,
                new PerformanceInstrumentationSpanSampleDocument
                {
                    sequence = sequence,
                    actor_id = actorKey,
                    render_frame = renderFrame,
                    logic_tick = logicTick,
                    duration_milliseconds = durationMilliseconds,
                    end_state = endState.ToString()
                });
        }
        if (reader.BaseStream.Position != reader.BaseStream.Length)
            throw new InvalidDataException("Performance instrumentation Span file contains trailing data.");
        return accumulators.Values
            .Select(value => new PerformanceInstrumentationPointSummaryDocument
            {
                point_id = value.Point.PointIdText,
                metric_id = value.Point.MetricId,
                assembly = value.Point.Assembly,
                declaring_type = value.Point.DeclaringType,
                method = value.Point.Method,
                source_file = value.Point.SourceFile,
                source_line = value.Point.SourceLine,
                distribution = BuildDistribution(value.Values, value.Values.Count, 1d),
                exception_count = value.Exceptions,
                actors = value.Actors.Values
                    .OrderBy(actor => actor.ActorId, StringComparer.Ordinal)
                    .Select(actor => new PerformanceInstrumentationActorSummaryDocument
                    {
                        actor_id = actor.ActorId,
                        invocation_count = actor.Invocations,
                        exception_count = actor.Exceptions,
                        distribution = BuildDistribution(actor.Values, actor.Invocations, 1d)
                    })
                    .ToArray(),
                slowest_samples = value.Slowest
                    .OrderByDescending(sample => sample.duration_milliseconds)
                    .ThenBy(sample => sample.sequence)
                    .ToArray()
            })
            .OrderByDescending(value => value.distribution.p95)
            .ThenBy(value => value.metric_id, StringComparer.Ordinal)
            .ThenBy(value => value.point_id, StringComparer.Ordinal)
            .ToArray();
    }

    static Dictionary<ulong, InstrumentationPoint> ReadInstrumentationManifest(
        string path,
        string expectedIdentity,
        string expectedMode,
        string expectedCatalogRevision,
        PerformanceMetricCatalogDocument catalog)
    {
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path, Encoding.UTF8));
        JsonElement root = document.RootElement;
        if (!string.Equals(JsonString(root, "schema"), PerformanceInstrumentationIdentity.ManifestSchema, StringComparison.Ordinal) ||
            !string.Equals(JsonString(root, "identity"), expectedIdentity, StringComparison.Ordinal) ||
            !string.Equals(JsonString(root, "mode"), expectedMode, StringComparison.Ordinal) ||
            !string.Equals(JsonString(root, "catalog_revision"), expectedCatalogRevision, StringComparison.Ordinal) ||
            !string.Equals(JsonString(root, "weaver_version"), PerformanceInstrumentationIdentity.WeaverVersion, StringComparison.Ordinal) ||
            !root.TryGetProperty("span_layout_revision", out JsonElement layout) ||
            layout.GetInt32() != PerformanceInstrumentationIdentity.SpanLayoutRevision ||
            !root.TryGetProperty("assemblies", out JsonElement assemblies) ||
            assemblies.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidDataException("Performance instrumentation manifest identity is invalid.");
        }
        var metricIds = new HashSet<string>(
            catalog.metrics.Select(value => value.metric_id),
            StringComparer.Ordinal);
        var points = new Dictionary<ulong, InstrumentationPoint>();
        foreach (JsonElement assembly in assemblies.EnumerateArray())
        {
            string assemblyName = JsonString(assembly, "assembly");
            if (string.IsNullOrWhiteSpace(assemblyName) ||
                !assembly.TryGetProperty("points", out JsonElement assemblyPoints) ||
                assemblyPoints.ValueKind != JsonValueKind.Array)
            {
                throw new InvalidDataException("Performance instrumentation assembly manifest is invalid.");
            }
            foreach (JsonElement point in assemblyPoints.EnumerateArray())
            {
                string pointIdText = JsonString(point, "point_id");
                string metricId = JsonString(point, "metric_id");
                string metricHashText = JsonString(point, "metric_hash");
                string declaringType = JsonString(point, "declaring_type");
                string method = JsonString(point, "method");
                int sourceLine = point.TryGetProperty("source_line", out JsonElement sourceLineValue) &&
                                 sourceLineValue.ValueKind == JsonValueKind.Number
                    ? sourceLineValue.GetInt32()
                    : 0;
                if (!ulong.TryParse(pointIdText, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ulong pointId) ||
                    !ulong.TryParse(metricHashText, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ulong metricHash) ||
                    string.IsNullOrWhiteSpace(metricId) || !metricIds.Contains(metricId) ||
                    string.IsNullOrWhiteSpace(declaringType) || string.IsNullOrWhiteSpace(method) ||
                    metricHash != PerformanceInstrumentationIdentity.Hash64(metricId) ||
                    pointId != PerformanceInstrumentationIdentity.Hash64(
                        assemblyName + "|" + declaringType + "|" + method + "|" + metricId))
                {
                    throw new InvalidDataException("Performance instrumentation point manifest contains an invalid point.");
                }
                if (!points.TryAdd(pointId, new InstrumentationPoint
                {
                    PointId = pointId,
                    PointIdText = pointIdText,
                    MetricId = metricId,
                    Assembly = assemblyName,
                    DeclaringType = declaringType,
                    Method = method,
                    SourceFile = JsonString(point, "source_file"),
                    SourceLine = sourceLine
                }))
                {
                    throw new InvalidDataException($"Performance instrumentation point '{pointIdText}' is duplicated.");
                }
            }
        }
        return points;
    }

    static string JsonString(JsonElement value, string name) =>
        value.TryGetProperty(name, out JsonElement property) && property.ValueKind == JsonValueKind.String
            ? property.GetString() ?? string.Empty
            : string.Empty;

    static void AddSlowest(
        List<PerformanceInstrumentationSpanSampleDocument> values,
        PerformanceInstrumentationSpanSampleDocument sample)
    {
        const int limit = 32;
        if (values.Count < limit)
        {
            values.Add(sample);
            return;
        }
        int slowestIndex = 0;
        for (int i = 1; i < values.Count; i++)
        {
            if (values[i].duration_milliseconds < values[slowestIndex].duration_milliseconds)
                slowestIndex = i;
        }
        if (sample.duration_milliseconds > values[slowestIndex].duration_milliseconds)
            values[slowestIndex] = sample;
    }

    static MetricSampleSet ReadMetricSamples(string path)
    {
        string[] lines = File.ReadAllLines(path, Encoding.UTF8);
        if (lines.Length < 2 || !string.Equals(lines[0], "metric_id,sample_scope,sample_index,identity,render_frame,value,count", StringComparison.Ordinal))
            throw new InvalidDataException("Performance metric samples header is invalid.");
        var result = new MetricSampleSet();
        for (int i = 1; i < lines.Length; i++)
        {
            string[] fields = Csv(lines[i]);
            if (fields.Length != 7 ||
                !ulong.TryParse(fields[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out ulong renderFrame) ||
                !double.TryParse(fields[5], NumberStyles.Float, CultureInfo.InvariantCulture, out double value) ||
                !long.TryParse(fields[6], NumberStyles.Integer, CultureInfo.InvariantCulture, out long count) || count <= 0)
            {
                throw new InvalidDataException($"Performance metric sample line {i + 1} is invalid.");
            }
            if (!result.Metrics.TryGetValue(fields[0], out MetricAccumulator accumulator))
            {
                accumulator = new MetricAccumulator();
                result.Metrics.Add(fields[0], accumulator);
            }
            accumulator.Values.Add(value);
            accumulator.Invocations = checked(accumulator.Invocations + count);
            if (string.Equals(fields[0], "session.logic-tick", StringComparison.Ordinal))
            {
                result.LogicFrameTotals.TryGetValue(renderFrame, out double total);
                result.LogicFrameTotals[renderFrame] = total + value;
            }
        }
        return result;
    }

    static PerformanceDistributionDocument BuildDistribution(List<double> values, long invocations, double scale)
    {
        values.Sort();
        return new PerformanceDistributionDocument
        {
            sample_count = values.Count,
            invocation_count = invocations,
            mean = values.Count == 0 ? 0d : values.Average() * scale,
            p50 = Percentile(values, 0.50d) * scale,
            p95 = Percentile(values, 0.95d) * scale,
            p99 = Percentile(values, 0.99d) * scale,
            max = values.Count == 0 ? 0d : values[^1] * scale
        };
    }

    static PerformanceFunctionHotspotDocument[] ReadHotspots(string path)
    {
        string[] lines = File.ReadAllLines(path, Encoding.UTF8);
        if (lines.Length < 2)
            throw new InvalidDataException("Xperf CPU hotspot export is empty.");
        string[] header = Csv(lines[0]);
        int processColumn = FindColumn(header, "Process");
        int threadColumn = FindColumn(header, "Thread");
        int moduleColumn = FindColumn(header, "Module");
        int functionColumn = FindColumn(header, "Function");
        int inclusiveColumn = FindColumn(header, "Inclusive");
        int exclusiveColumn = FindColumn(header, "Exclusive");
        var values = new List<PerformanceFunctionHotspotDocument>();
        for (int i = 1; i < lines.Length; i++)
        {
            string[] fields = Csv(lines[i]);
            if (fields.Length != header.Length ||
                !double.TryParse(fields[inclusiveColumn], NumberStyles.Float, CultureInfo.InvariantCulture, out double inclusive) ||
                !double.TryParse(fields[exclusiveColumn], NumberStyles.Float, CultureInfo.InvariantCulture, out double exclusive))
            {
                throw new InvalidDataException($"Xperf CPU hotspot line {i + 1} is invalid.");
            }
            string function = fields[functionColumn];
            if (string.IsNullOrWhiteSpace(fields[processColumn]) || string.IsNullOrWhiteSpace(fields[threadColumn]) ||
                string.IsNullOrWhiteSpace(fields[moduleColumn]) || string.IsNullOrWhiteSpace(function) ||
                function.Contains("<Symbols disabled>", StringComparison.Ordinal) ||
                function.Contains("Unknown", StringComparison.OrdinalIgnoreCase) ||
                function.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ||
                function.Contains("!0x", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException("Xperf CPU hotspot export contains unresolved symbols.");
            }
            values.Add(new PerformanceFunctionHotspotDocument
            {
                process = fields[processColumn],
                thread = fields[threadColumn],
                module = fields[moduleColumn],
                function = function,
                inclusive_samples = inclusive,
                exclusive_samples = exclusive
            });
        }
        return values
            .OrderByDescending(value => value.inclusive_samples)
            .ThenBy(value => value.function, StringComparer.Ordinal)
            .Take(200)
            .ToArray();
    }

    static PerformanceWaitEvidenceDocument[] ReadWaitEvidence(string path)
    {
        string[] lines = File.ReadAllLines(path, Encoding.UTF8);
        if (lines.Length < 2)
            throw new InvalidDataException("WPA context-switch export is empty.");
        string[] header = Csv(lines[0].TrimStart('\ufeff'));
        int processColumn = FindExactColumn(header, "New Process");
        int threadColumn = FindExactColumn(header, "New Thread Id");
        int readyColumn = FindExactColumn(header, "Ready");
        int waitColumn = FindExactColumn(header, "Waits");
        int countColumn = FindExactColumn(header, "Count");
        var values = new Dictionary<string, WaitAccumulator>(StringComparer.Ordinal);
        for (int i = 1; i < lines.Length; i++)
        {
            string[] fields = Csv(lines[i]);
            if (fields.Length != header.Length)
                throw new InvalidDataException($"WPA context-switch line {i + 1} is invalid.");
            string key = fields[processColumn] + "\n" + fields[threadColumn];
            if (!values.TryGetValue(key, out WaitAccumulator accumulator))
            {
                accumulator = new WaitAccumulator
                {
                    Process = fields[processColumn],
                    Thread = fields[threadColumn]
                };
                values.Add(key, accumulator);
            }
            accumulator.Ready += WpaNumber(fields[readyColumn]);
            accumulator.Wait += WpaNumber(fields[waitColumn]);
            accumulator.Count = checked(accumulator.Count + (long)WpaNumber(fields[countColumn]));
        }
        return values.Values
            .Select(value => new PerformanceWaitEvidenceDocument
            {
                process = value.Process,
                thread = value.Thread,
                ready_time = value.Ready,
                wait_time = value.Wait,
                context_switches = value.Count
            })
            .OrderByDescending(value => value.wait_time)
            .ThenByDescending(value => value.ready_time)
            .ThenBy(value => value.thread, StringComparer.Ordinal)
            .Take(100)
            .ToArray();
    }

    static PerformanceComparisonDocument BuildComparison(
        PerformanceRunRequestDocument request,
        PerformanceScenarioDocument scenario,
        PerformanceSummaryDocument candidate,
        PerformanceRuntimeResultDocument runtime)
    {
        if (string.IsNullOrWhiteSpace(request.baseline_manifest_path))
        {
            return new PerformanceComparisonDocument
            {
                candidate_capture_id = request.run_id,
                status = "NotRequested",
                message = "No Baseline Capture was selected."
            };
        }
        PerformanceCaptureManifestDocument baselineManifest = ReadJson<PerformanceCaptureManifestDocument>(request.baseline_manifest_path);
        string baselineClosureError = BaselineClosureError(request.baseline_manifest_path, baselineManifest);
        if (!string.IsNullOrEmpty(baselineClosureError))
        {
            return new PerformanceComparisonDocument
            {
                baseline_capture_id = baselineManifest.capture_id,
                candidate_capture_id = request.run_id,
                status = "Rejected",
                message = baselineClosureError
            };
        }
        PerformanceToolchainDocument toolchain = ReadJson<PerformanceToolchainDocument>(request.toolchain_path);
        string wprProfileHash = PerformanceFileUtility.Sha256(toolchain.wpr_profile_path);
        string hardware = HardwareIdentity(runtime);
        var conflicts = new List<string>();
        AddConflict(conflicts, "manifest_schema", PerformanceCaptureSchemas.Manifest, baselineManifest.schema);
        AddConflict(conflicts, "status", PerformanceCaptureStatus.Completed.ToString(), baselineManifest.status);
        AddConflict(conflicts, "scenario_hash", scenario.content_hash, baselineManifest.scenario_hash);
        AddConflict(conflicts, "runtime_id", scenario.runtime_id, baselineManifest.runtime_id);
        AddConflict(conflicts, "roster_identity", scenario.roster_identity, baselineManifest.roster_identity);
        AddConflict(conflicts, "hardware_identity", hardware, baselineManifest.hardware_identity);
        AddConflict(conflicts, "metric_catalog_revision", runtime.metric_catalog_revision, baselineManifest.metric_catalog_revision);
        AddConflict(conflicts, "instrumentation_identity", runtime.instrumentation_identity, baselineManifest.instrumentation_identity);
        AddConflict(conflicts, "instrumentation_mode", runtime.instrumentation_mode, baselineManifest.instrumentation_mode);
        AddConflict(conflicts, "instrumentation_span_layout_revision", runtime.instrumentation_span_layout_revision, baselineManifest.instrumentation_span_layout_revision);
        AddConflict(conflicts, "build_mode", "Development", baselineManifest.build_mode);
        AddConflict(conflicts, "wpr_profile", toolchain.wpr_profile_path, baselineManifest.wpr_profile, true);
        AddConflict(conflicts, "wpr_profile_hash", wprProfileHash, baselineManifest.wpr_profile_hash);
        AddConflict(conflicts, "statistics_schema", PerformanceCaptureSchemas.Summary, baselineManifest.statistics_schema);
        AddConflict(conflicts, "budget_hash", request.budget_hash, baselineManifest.budget_hash);
        AddConflict(conflicts, "capture_profile_hash", request.profile_hash, baselineManifest.capture_profile_hash);
        AddConflict(conflicts, "width", scenario.width, baselineManifest.width);
        AddConflict(conflicts, "height", scenario.height, baselineManifest.height);
        AddConflict(conflicts, "quality_level", scenario.quality_level, baselineManifest.quality_level);
        AddConflict(conflicts, "v_sync_count", scenario.v_sync_count, baselineManifest.v_sync_count);
        AddConflict(conflicts, "target_frame_rate", scenario.target_frame_rate, baselineManifest.target_frame_rate);
        if (conflicts.Count != 0)
        {
            return new PerformanceComparisonDocument
            {
                baseline_capture_id = baselineManifest.capture_id,
                candidate_capture_id = request.run_id,
                status = "Rejected",
                message = string.Join("; ", conflicts)
            };
        }
        string baselineRoot = Path.GetDirectoryName(Path.GetFullPath(request.baseline_manifest_path))!;
        PerformanceSummaryDocument baseline = ReadJson<PerformanceSummaryDocument>(Path.Combine(baselineRoot, "summary.json"));
        if (!string.Equals(baseline.schema, PerformanceCaptureSchemas.Summary, StringComparison.Ordinal))
        {
            return new PerformanceComparisonDocument
            {
                baseline_capture_id = baselineManifest.capture_id,
                candidate_capture_id = request.run_id,
                status = "Rejected",
                message = $"summary_schema: expected='{PerformanceCaptureSchemas.Summary}', baseline='{baseline.schema}'"
            };
        }
        var pointComparisons = new List<PerformanceInstrumentationPointComparisonDocument>();
        PerformanceInstrumentationPointSummaryDocument[] baselinePoints =
            baseline.instrumentation_points ?? Array.Empty<PerformanceInstrumentationPointSummaryDocument>();
        Dictionary<string, PerformanceInstrumentationPointSummaryDocument> baselinePointsById =
            baselinePoints.ToDictionary(value => value.point_id, StringComparer.Ordinal);
        PerformanceInstrumentationPointSummaryDocument[] candidatePoints =
            candidate.instrumentation_points ?? Array.Empty<PerformanceInstrumentationPointSummaryDocument>();
        for (int i = 0; i < candidatePoints.Length; i++)
        {
            PerformanceInstrumentationPointSummaryDocument current = candidatePoints[i];
            if (!baselinePointsById.TryGetValue(current.point_id, out PerformanceInstrumentationPointSummaryDocument before) ||
                !string.Equals(before.metric_id, current.metric_id, StringComparison.Ordinal))
            {
                continue;
            }
            double delta = current.distribution.p95 - before.distribution.p95;
            pointComparisons.Add(new PerformanceInstrumentationPointComparisonDocument
            {
                point_id = current.point_id,
                metric_id = current.metric_id,
                baseline_p95 = before.distribution.p95,
                candidate_p95 = current.distribution.p95,
                absolute_delta = delta,
                percent_delta = before.distribution.p95 == 0d ? 0d : delta / before.distribution.p95 * 100d,
                baseline_invocation_count = before.distribution.invocation_count,
                candidate_invocation_count = current.distribution.invocation_count
            });
        }
        var baselineMetrics = baseline.metrics.ToDictionary(value => value.metric_id, StringComparer.Ordinal);
        var comparisons = new List<PerformanceComparisonMetricDocument>();
        for (int i = 0; i < candidate.metrics.Length; i++)
        {
            PerformanceMetricSummaryDocument current = candidate.metrics[i];
            if (!baselineMetrics.TryGetValue(current.metric_id, out PerformanceMetricSummaryDocument before) ||
                !string.Equals(before.unit, current.unit, StringComparison.Ordinal))
            {
                continue;
            }
            double delta = current.distribution.p95 - before.distribution.p95;
            comparisons.Add(new PerformanceComparisonMetricDocument
            {
                metric_id = current.metric_id,
                baseline_p95 = before.distribution.p95,
                candidate_p95 = current.distribution.p95,
                absolute_delta = delta,
                percent_delta = before.distribution.p95 == 0d ? 0d : delta / before.distribution.p95 * 100d
            });
        }
        Dictionary<string, double> baselineHotspots = baseline.hotspots
            .GroupBy(HotspotKey)
            .ToDictionary(group => group.Key, group => group.Sum(value => value.inclusive_samples), StringComparer.Ordinal);
        Dictionary<string, double> candidateHotspots = candidate.hotspots
            .GroupBy(HotspotKey)
            .ToDictionary(group => group.Key, group => group.Sum(value => value.inclusive_samples), StringComparer.Ordinal);
        var hotspotKeys = new HashSet<string>(baselineHotspots.Keys, StringComparer.Ordinal);
        hotspotKeys.UnionWith(candidateHotspots.Keys);
        PerformanceHotspotComparisonDocument[] hotspotComparisons = hotspotKeys
            .Select(key =>
            {
                baselineHotspots.TryGetValue(key, out double before);
                candidateHotspots.TryGetValue(key, out double after);
                string[] identity = key.Split('\n');
                double delta = after - before;
                return new PerformanceHotspotComparisonDocument
                {
                    thread = identity[0],
                    module = identity[1],
                    function = identity[2],
                    baseline_inclusive_samples = before,
                    candidate_inclusive_samples = after,
                    absolute_delta = delta,
                    percent_delta = before == 0d ? 0d : delta / before * 100d
                };
            })
            .OrderByDescending(value => Math.Abs(value.absolute_delta))
            .ThenBy(value => value.function, StringComparer.Ordinal)
            .Take(200)
            .ToArray();
        return new PerformanceComparisonDocument
        {
            baseline_capture_id = baseline.capture_id,
            candidate_capture_id = candidate.capture_id,
            status = "Comparable",
            message = "Baseline and Candidate identities match.",
            baseline_budget_passed = baseline.budget_passed,
            candidate_budget_passed = candidate.budget_passed,
            budget_exceeded_delta = candidate.budget_exceeded_count - baseline.budget_exceeded_count,
            metrics = comparisons.OrderByDescending(value => Math.Abs(value.percent_delta)).ToArray(),
            instrumentation_points = pointComparisons
                .OrderByDescending(value => Math.Abs(value.percent_delta))
                .ThenBy(value => value.point_id, StringComparer.Ordinal)
                .ToArray(),
            hotspots = hotspotComparisons
        };
    }

    static string HotspotKey(PerformanceFunctionHotspotDocument value) =>
        value.thread + "\n" + value.module + "\n" + value.function;

    static string BaselineClosureError(string manifestPath, PerformanceCaptureManifestDocument manifest)
    {
        if (manifest.files == null || manifest.files.Length == 0)
            return "baseline_closure: manifest has no files";
        string root = Path.GetDirectoryName(Path.GetFullPath(manifestPath))!;
        string prefix = root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        int summaryCount = 0;
        int instrumentationSpanCount = 0;
        for (int i = 0; i < manifest.files.Length; i++)
        {
            PerformanceFileDocument file = manifest.files[i];
            if (file == null)
                return "baseline_closure: manifest contains a null file";
            string path = Path.GetFullPath(Path.Combine(root, file.path));
            if (!path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) || !File.Exists(path) ||
                new FileInfo(path).Length != file.size ||
                !string.Equals(PerformanceFileUtility.Sha256(path), file.sha256, StringComparison.Ordinal))
            {
                return $"baseline_closure: file '{file.path}' is missing or has another hash";
            }
            if (string.Equals(file.role, "summary", StringComparison.Ordinal))
                summaryCount++;
            if (string.Equals(file.role, "instrumentation-spans", StringComparison.Ordinal))
                instrumentationSpanCount++;
        }
        if (summaryCount != 1)
            return "baseline_closure: manifest requires exactly one summary";
        return instrumentationSpanCount == 1
            ? string.Empty
            : "baseline_closure: manifest requires exactly one instrumentation span file";
    }

    static void AddConflict(List<string> conflicts, string field, object expected, object actual, bool ignoreCase = false)
    {
        string expectedText = Convert.ToString(expected, CultureInfo.InvariantCulture) ?? string.Empty;
        string actualText = Convert.ToString(actual, CultureInfo.InvariantCulture) ?? string.Empty;
        StringComparison comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        if (!string.Equals(expectedText, actualText, comparison))
            conflicts.Add($"{field}: expected='{expectedText}', baseline='{actualText}'");
    }

    static PerformanceCaptureManifestDocument CreateManifest(
        PerformanceRunRequestDocument request,
        PerformanceScenarioDocument scenario,
        PerformancePlayerManifestDocument playerManifest,
        PerformanceToolchainDocument toolchain,
        string requestHash,
        string playerManifestHash,
        string toolchainHash,
        PerformanceProcessDocument process,
        PerformanceRuntimeResultDocument runtime,
        PerformanceCaptureStatus status,
        string stage,
        string message) =>
        new()
        {
            capture_id = request.run_id,
            status = status.ToString(),
            stage = stage,
            message = message,
            created_utc = runtime?.started_utc ?? string.Empty,
            completed_utc = runtime?.completed_utc ?? DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
            scenario_id = scenario?.scenario_id ?? string.Empty,
            scenario_hash = scenario?.content_hash ?? string.Empty,
            runtime_id = scenario?.runtime_id ?? string.Empty,
            roster_identity = scenario?.roster_identity ?? string.Empty,
            build_id = playerManifest?.build_id ?? string.Empty,
            build_mode = playerManifest?.build_mode ?? string.Empty,
            request_hash = requestHash ?? string.Empty,
            player_manifest_hash = playerManifestHash ?? string.Empty,
            toolchain_identity = toolchainHash ?? string.Empty,
            budget_hash = request?.budget_hash ?? string.Empty,
            capture_profile_hash = request?.profile_hash ?? string.Empty,
            content_identity = playerManifest?.content_identity ?? string.Empty,
            pipeline_identity = playerManifest?.pipeline_identity ?? string.Empty,
            pose_graph_revision = playerManifest?.pose_graph_revision ?? string.Empty,
            solver_identity = playerManifest?.solver_identity ?? string.Empty,
            instrumentation_identity = playerManifest?.instrumentation_identity ?? string.Empty,
            instrumentation_mode = playerManifest?.instrumentation_mode ?? string.Empty,
            instrumentation_span_layout_revision = playerManifest?.instrumentation_span_layout_revision ?? 0,
            instrumentation_manifest_path = playerManifest?.instrumentation_manifest_path ?? string.Empty,
            instrumentation_span_path = runtime?.instrumentation_span_path ?? string.Empty,
            instrumentation_span_hash = runtime?.instrumentation_span_hash ?? string.Empty,
            hardware_identity = HardwareIdentity(runtime),
            operating_system = runtime?.operating_system ?? string.Empty,
            processor = runtime?.processor ?? string.Empty,
            graphics_device = runtime?.graphics_device ?? string.Empty,
            graphics_api = runtime?.graphics_api ?? string.Empty,
            metric_catalog_revision = runtime?.metric_catalog_revision ?? string.Empty,
            wpr_profile = toolchain?.wpr_profile_path ?? string.Empty,
            wpr_profile_hash = toolchain?.wpr_profile_hash ?? string.Empty,
            statistics_schema = PerformanceCaptureSchemas.Summary,
            width = scenario?.width ?? 0,
            height = scenario?.height ?? 0,
            quality_level = scenario?.quality_level ?? 0,
            v_sync_count = scenario?.v_sync_count ?? 0,
            target_frame_rate = scenario?.target_frame_rate ?? 0,
            process = process
        };

    static PerformanceGateManifestDocument CreateGateManifest(
        PerformanceRunRequestDocument request,
        PerformanceScenarioDocument scenario,
        PerformanceCaptureProfileDocument profile,
        PerformancePlayerManifestDocument playerManifest,
        string playerManifestHash,
        PerformanceProcessDocument process,
        PerformanceRuntimeResultDocument runtime,
        PerformanceCaptureStatus status,
        string stage,
        string message) =>
        new()
        {
            operation = request.operation,
            run_id = request.run_id,
            status = status.ToString(),
            stage = stage,
            message = message,
            created_utc = runtime?.started_utc ?? string.Empty,
            completed_utc = runtime?.completed_utc ?? DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
            scenario_id = scenario?.scenario_id ?? string.Empty,
            scenario_hash = scenario?.content_hash ?? string.Empty,
            build_id = playerManifest?.build_id ?? string.Empty,
            instrumentation_identity = playerManifest?.instrumentation_identity ?? string.Empty,
            instrumentation_mode = playerManifest?.instrumentation_mode ?? string.Empty,
            instrumentation_span_layout_revision = playerManifest?.instrumentation_span_layout_revision ?? 0,
            player_manifest_hash = playerManifestHash ?? string.Empty,
            profile_hash = request?.profile_hash ?? string.Empty,
            predecessor_gate_hash = request?.smoke_manifest_hash ?? string.Empty,
            input_trace_hash = scenario?.input_trace_hash ?? string.Empty,
            camera_trace_hash = scenario?.camera_trace_hash ?? string.Empty,
            warmup_logic_ticks = scenario?.warmup_logic_ticks ?? 0,
            capture_logic_ticks = scenario?.capture_logic_ticks ?? 0,
            start_body_hash = runtime?.start_body_hash ?? string.Empty,
            input_sequence_hash = runtime?.input_sequence_hash ?? string.Empty,
            body_trajectory_hash = runtime?.body_trajectory_hash ?? string.Empty,
            process = process
        };

    static PerformanceRuntimeResultDocument TryReadRuntimeResult(string root)
    {
        string path = Path.Combine(root, "runtime-result.json");
        return File.Exists(path) ? ReadJson<PerformanceRuntimeResultDocument>(path) : new PerformanceRuntimeResultDocument();
    }

    static void PublishDirectory(string stagingRoot, string finalRoot)
    {
        if (Directory.Exists(finalRoot))
            throw new IOException($"Performance Capture destination already exists: {finalRoot}");
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(finalRoot))!);
        Directory.Move(stagingRoot, finalRoot);
    }

    static double Percentile(IReadOnlyList<double> values, double percentile)
    {
        if (values.Count == 0)
            return 0d;
        int index = Math.Max(0, Math.Min(values.Count - 1, (int)Math.Ceiling(values.Count * percentile) - 1));
        return values[index];
    }

    static int FindColumn(IReadOnlyList<string> header, string token)
    {
        for (int i = 0; i < header.Count; i++)
        {
            if (header[i].Contains(token, StringComparison.OrdinalIgnoreCase))
                return i;
        }
        throw new InvalidDataException($"WPA CPU hotspot export has no '{token}' column.");
    }

    static int FindExactColumn(IReadOnlyList<string> header, string name)
    {
        for (int i = 0; i < header.Count; i++)
        {
            if (string.Equals(header[i], name, StringComparison.OrdinalIgnoreCase) ||
                header[i].StartsWith(name + " (", StringComparison.OrdinalIgnoreCase))
                return i;
        }
        throw new InvalidDataException($"WPA context-switch export has no '{name}' column.");
    }

    static double WpaNumber(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return 0d;
        if (double.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out double parsed))
            return parsed;
        string token = value.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;
        if (double.TryParse(token, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out parsed))
            return parsed;
        throw new InvalidDataException($"WPA context-switch numeric value '{value}' is invalid.");
    }

    static string[] Csv(string line)
    {
        var values = new List<string>();
        var current = new StringBuilder();
        bool quoted = false;
        for (int i = 0; i < line.Length; i++)
        {
            char value = line[i];
            if (value == '"')
            {
                if (quoted && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    quoted = !quoted;
                }
            }
            else if (value == ',' && !quoted)
            {
                values.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(value);
            }
        }
        if (quoted)
            throw new InvalidDataException("CSV line has an unterminated quote.");
        values.Add(current.ToString());
        return values.ToArray();
    }

    static string HardwareIdentity(PerformanceRuntimeResultDocument runtime) => runtime == null
        ? string.Empty
        : $"{Environment.MachineName}|{runtime.operating_system}|{runtime.processor}|{runtime.graphics_device}|{runtime.graphics_api}";

    static T ReadJson<T>(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException("Performance document is missing.", path);
        T value = JsonSerializer.Deserialize<T>(File.ReadAllText(path, Encoding.UTF8), JsonOptions);
        return value == null ? throw new InvalidDataException($"Performance document '{path}' is invalid.") : value;
    }

    static void WriteJson<T>(string path, T value) =>
        File.WriteAllText(path, JsonSerializer.Serialize(value, JsonOptions), new UTF8Encoding(false));

    static void WriteText(string path, string value) =>
        File.WriteAllText(path, value ?? string.Empty, new UTF8Encoding(false));
}
