using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using MCPForUnity.Editor.Helpers;
using MCPForUnity.Editor.Tools;
using Newtonsoft.Json.Linq;
using ThirdPersonCharacter.Pipeline.Editor;
using ThirdPersonPerformance;
using ThirdPersonPerformance.Instrumentation;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Editor.CharacterSimulation
{
    [McpForUnityTool(
        "performance.prepare",
        Description = "Configure the exact local Windows performance Toolchain or publish an immutable Fixed Input plus local camera Performance Scenario through the formal shared workflow.",
        StructuredOutput = true,
        AutoRegister = true,
        RequiresPolling = false,
        HasBehaviorAnnotations = true,
        ReadOnlyHint = false,
        DestructiveHint = false,
        IdempotentHint = true,
        OpenWorldHint = false)]
    public static class ThirdPersonPerformancePrepareMcpTool
    {
        public sealed class Parameters
        {
            [ToolParameter("Action: status, configure_toolchain, or publish_scenario. Defaults to status.", Required = false)]
            public string action { get; set; }

            [ToolParameter("Exact Fixed Input Trace JSON path. publish_scenario defaults to the workflow's last trace.", Required = false)]
            public string trace_path { get; set; }

            [ToolParameter("Exact Fixed runtime id. Defaults to character.fixed-local.", Required = false)]
            public string runtime_id { get; set; }

        }

        public static object HandleCommand(JObject parameters)
        {
            string action = parameters?["action"]?.Value<string>()?.Trim().ToLowerInvariant() ?? "status";
            try
            {
                switch (action)
                {
                    case "status":
                        return PerformanceMcpBridge.Success("Performance workflow status.");
                    case "configure_toolchain":
                        ThirdPersonPerformanceCaptureWorkflow.ConfigureToolchain();
                        return PerformanceMcpBridge.Success("Performance Toolchain configured.");
                    case "publish_scenario":
                    {
                        string tracePath = parameters?["trace_path"]?.Value<string>() ??
                                           CharacterFixedInputTraceWorkflow.LastTracePath;
                        string runtimeId = parameters?["runtime_id"]?.Value<string>() ??
                                           ThirdPersonPerformanceCaptureWorkflow.FixedRuntimeId;
                        ThirdPersonPerformanceCaptureWorkflow.PublishScenario(runtimeId, tracePath);
                        return PerformanceMcpBridge.Success("Performance Scenario published.");
                    }
                    default:
                        return new ErrorResponse("invalid_action", new { action });
                }
            }
            catch (Exception exception)
            {
                return new ErrorResponse("performance_prepare_failed", new { action, message = exception.Message });
            }
        }
    }

    [McpForUnityTool(
        "performance.build_player",
        Description = "Build and atomically publish the exact Windows x64 IL2CPP Development Performance Player with an explicit Disabled, MarkerOnly or Span instrumentation mode. Use start, then poll status with the returned job_id.",
        StructuredOutput = true,
        AutoRegister = true,
        RequiresPolling = true,
        BackgroundPollingStatus = true,
        PollAction = "status",
        MaxPollSeconds = 3600,
        HasBehaviorAnnotations = true,
        ReadOnlyHint = false,
        DestructiveHint = false,
        IdempotentHint = false,
        OpenWorldHint = false)]
    public static class ThirdPersonPerformanceBuildPlayerMcpTool
    {
        public sealed class Parameters
        {
            [ToolParameter("Action: start or status. Defaults to start.", Required = false)]
            public string action { get; set; }

            [ToolParameter("Stable job identity returned by start. status may omit it only when one latest matching job exists.", Required = false)]
            public string job_id { get; set; }

            [ToolParameter("Exact Fixed runtime id. Defaults to character.fixed-local.", Required = false)]
            public string runtime_id { get; set; }

            [ToolParameter("Required instrumentation mode: Disabled, MarkerOnly or Span.", Required = true)]
            public string instrumentation_mode { get; set; }
        }

        public static object HandleCommand(JObject parameters)
        {
            string action = parameters?["action"]?.Value<string>()?.Trim().ToLowerInvariant() ?? "start";
            if (action == "status")
                return PerformanceMcpJobScheduler.Status(parameters?["job_id"]?.Value<string>(), "build_player");
            if (action != "start")
                return new ErrorResponse("invalid_action", new { action });
            string runtimeId = parameters?["runtime_id"]?.Value<string>() ??
                               ThirdPersonPerformanceCaptureWorkflow.FixedRuntimeId;
            string instrumentationModeText = parameters?["instrumentation_mode"]?.Value<string>()?.Trim();
            if (!Enum.TryParse(
                    instrumentationModeText,
                    true,
                    out PerformanceInstrumentationMode instrumentationMode) ||
                !Enum.IsDefined(typeof(PerformanceInstrumentationMode), instrumentationMode))
            {
                return new ErrorResponse(
                    "invalid_instrumentation_mode",
                    new { instrumentation_mode = instrumentationModeText });
            }
            return PerformanceMcpJobScheduler.Start(
                "build_player",
                false,
                jobId =>
                {
                    ThirdPersonPerformanceCaptureWorkflow.BuildPlayer(
                        runtimeId,
                        jobId,
                        instrumentationMode,
                        (phase, message, elapsed) => PerformanceMcpJobScheduler.ReportBuildProgress(jobId, phase, message, elapsed));
                    return PerformanceMcpBridge.Success("Performance Player published.");
                });
        }
    }

    [McpForUnityTool(
        "performance.smoke",
        Description = "Run the non-elevated Performance Player HELLO, READY, STOP and clean-exit gate. This never starts WPR or records performance data.",
        StructuredOutput = true,
        AutoRegister = true,
        RequiresPolling = true,
        BackgroundPollingStatus = true,
        PollAction = "status",
        MaxPollSeconds = 300,
        HasBehaviorAnnotations = true,
        ReadOnlyHint = false,
        DestructiveHint = false,
        IdempotentHint = false,
        OpenWorldHint = false)]
    public static class ThirdPersonPerformanceSmokeMcpTool
    {
        public sealed class Parameters
        {
            [ToolParameter("Action: start, status, or cancel. Defaults to start.", Required = false)]
            public string action { get; set; }

            [ToolParameter("Stable job identity returned by start.", Required = false)]
            public string job_id { get; set; }
        }

        public static object HandleCommand(JObject parameters) =>
            PerformanceGateMcpCommand.Handle(
                parameters,
                PerformanceOperationKinds.Smoke,
                ThirdPersonPerformanceCaptureWorkflow.StartSmoke);
    }

    [McpForUnityTool(
        "performance.replay",
        Description = "Run the non-elevated exact Scenario warmup and full Fixed Input plus camera replay gate. Requires a matching Completed Smoke gate and never starts WPR or Profiler recording.",
        StructuredOutput = true,
        AutoRegister = true,
        RequiresPolling = true,
        BackgroundPollingStatus = true,
        PollAction = "status",
        MaxPollSeconds = 600,
        HasBehaviorAnnotations = true,
        ReadOnlyHint = false,
        DestructiveHint = false,
        IdempotentHint = false,
        OpenWorldHint = false)]
    public static class ThirdPersonPerformanceReplayMcpTool
    {
        public sealed class Parameters
        {
            [ToolParameter("Action: start, status, or cancel. Defaults to start.", Required = false)]
            public string action { get; set; }

            [ToolParameter("Stable job identity returned by start.", Required = false)]
            public string job_id { get; set; }
        }

        public static object HandleCommand(JObject parameters) =>
            PerformanceGateMcpCommand.Handle(
                parameters,
                PerformanceOperationKinds.Replay,
                ThirdPersonPerformanceCaptureWorkflow.StartReplay);
    }

    static class PerformanceGateMcpCommand
    {
        public static object Handle(JObject parameters, string operation, Action start)
        {
            string action = parameters?["action"]?.Value<string>()?.Trim().ToLowerInvariant() ?? "start";
            try
            {
                switch (action)
                {
                    case "start":
                        return PerformanceMcpJobScheduler.Start(
                            operation,
                            true,
                            _ =>
                            {
                                start();
                                return null;
                            });
                    case "status":
                        return PerformanceMcpJobScheduler.Status(parameters?["job_id"]?.Value<string>(), operation);
                    case "cancel":
                        ThirdPersonPerformanceCaptureWorkflow.CancelOwnedRun();
                        return PerformanceMcpBridge.Success($"Owned Performance {operation} cancellation requested.");
                    default:
                        return new ErrorResponse("invalid_action", new { action });
                }
            }
            catch (Exception exception)
            {
                return new ErrorResponse($"performance_{operation}_command_failed", new { action, message = exception.Message });
            }
        }
    }

    [McpForUnityTool(
        "performance.capture",
        Description = "Start, poll or cancel one owned Performance Capture, or set the explicit Completed Baseline. Capture uses the existing Player, Controller, WPR/WPA and atomic artifact workflow.",
        StructuredOutput = true,
        AutoRegister = true,
        RequiresPolling = true,
        BackgroundPollingStatus = true,
        PollAction = "status",
        MaxPollSeconds = 3600,
        HasBehaviorAnnotations = true,
        ReadOnlyHint = false,
        DestructiveHint = false,
        IdempotentHint = false,
        OpenWorldHint = false)]
    public static class ThirdPersonPerformanceCaptureMcpTool
    {
        public sealed class Parameters
        {
            [ToolParameter("Action: start, status, cancel, select_baseline, or clear_baseline. Defaults to start.", Required = false)]
            public string action { get; set; }

            [ToolParameter("Stable job identity returned by start. status may omit it only when one latest matching job exists.", Required = false)]
            public string job_id { get; set; }

            [ToolParameter("Exact Completed Capture manifest path. Required for select_baseline.", Required = false)]
            public string baseline_manifest_path { get; set; }
        }

        public static object HandleCommand(JObject parameters)
        {
            string action = parameters?["action"]?.Value<string>()?.Trim().ToLowerInvariant() ?? "start";
            try
            {
                switch (action)
                {
                    case "start":
                        return PerformanceMcpJobScheduler.Start(
                            "capture",
                            true,
                            _ =>
                            {
                                ThirdPersonPerformanceCaptureWorkflow.StartCapture();
                                return null;
                            });
                    case "status":
                        return PerformanceMcpJobScheduler.Status(
                            parameters?["job_id"]?.Value<string>(),
                            "capture");
                    case "cancel":
                        ThirdPersonPerformanceCaptureWorkflow.CancelOwnedRun();
                        return PerformanceMcpBridge.Success("Owned Performance run cancellation requested.");
                    case "select_baseline":
                    {
                        string path = parameters?["baseline_manifest_path"]?.Value<string>() ?? string.Empty;
                        ThirdPersonPerformanceCaptureWorkflow.SelectBaseline(path);
                        return PerformanceMcpBridge.Success("Performance Baseline selected.");
                    }
                    case "clear_baseline":
                        ThirdPersonPerformanceCaptureWorkflow.ClearBaseline();
                        return PerformanceMcpBridge.Success("Performance Baseline cleared.");
                    default:
                        return new ErrorResponse("invalid_action", new { action });
                }
            }
            catch (Exception exception)
            {
                return new ErrorResponse("performance_capture_command_failed", new { action, message = exception.Message });
            }
        }
    }

    [McpForUnityTool(
        "performance.analyze",
        Description = "Compare explicitly selected repeated Completed Captures offline using the existing Controller. No Player, WPR or Unity build is started; the Controller executable is built before analysis.",
        StructuredOutput = true,
        AutoRegister = true,
        RequiresPolling = true,
        BackgroundPollingStatus = true,
        PollAction = "status",
        MaxPollSeconds = 900,
        HasBehaviorAnnotations = true,
        ReadOnlyHint = false,
        DestructiveHint = false,
        IdempotentHint = false,
        OpenWorldHint = false)]
    public static class ThirdPersonPerformanceAnalyzeMcpTool
    {
        public sealed class Parameters
        {
            [ToolParameter("Action: start or status. Defaults to start.", Required = false)]
            public string action { get; set; }

            [ToolParameter("Exact analysis request JSON containing baseline_manifest_paths and candidate_manifest_paths. Required for start.", Required = false)]
            public string request_path { get; set; }

            [ToolParameter("Job identity returned by start.", Required = false)]
            public string job_id { get; set; }
        }

        public static object HandleCommand(JObject parameters)
        {
            string action = parameters?["action"]?.Value<string>()?.Trim().ToLowerInvariant() ?? "start";
            try
            {
                if (action == "status")
                    return PerformanceMcpJobScheduler.Status(parameters?["job_id"]?.Value<string>(), "analyze");
                if (action != "start")
                    return new ErrorResponse("invalid_action", new { action });
                string path = parameters?["request_path"]?.Value<string>();
                if (string.IsNullOrWhiteSpace(path))
                    return new ErrorResponse("analysis_request_required");
                return PerformanceMcpJobScheduler.Start("analyze", false, _ =>
                {
                    string report = ThirdPersonPerformanceCaptureWorkflow.AnalyzeCaptures(path);
                    return new SuccessResponse("Performance repeat analysis published; inspect report status.", PerformanceMcpBridge.ReadAnalysis(report, 200));
                });
            }
            catch (Exception exception)
            {
                return new ErrorResponse("performance_analysis_failed", new { message = exception.Message });
            }
        }
    }

    [McpForUnityTool(
        "performance.report",
        Description = "Read exact Performance Capture status, manifests, summaries and comparisons, or list published Capture manifests without opening Windows UI.",
        StructuredOutput = true,
        AutoRegister = true,
        RequiresPolling = false,
        HasBehaviorAnnotations = true,
        ReadOnlyHint = true,
        DestructiveHint = false,
        IdempotentHint = true,
        OpenWorldHint = false)]
    public static class ThirdPersonPerformanceReportMcpTool
    {
        public sealed class Parameters
        {
            [ToolParameter("Action: status, gates, gate, list, manifest, summary, comparison, or analysis. Defaults to status.", Required = false)]
            public string action { get; set; }

            [ToolParameter("Exact Capture manifest path. manifest, summary and comparison default to the last explicit Capture.", Required = false)]
            public string manifest_path { get; set; }

            [ToolParameter("Exact analysis.json path for action=analysis; defaults to last explicit repeat analysis.", Required = false)]
            public string analysis_path { get; set; }

            [ToolParameter("Maximum hotspot, thread and wait rows returned by summary. Defaults to 30 and is capped at 200.", Required = false)]
            public int limit { get; set; }
        }

        public static object HandleCommand(JObject parameters)
        {
            string action = parameters?["action"]?.Value<string>()?.Trim().ToLowerInvariant() ?? "status";
            string manifestPath = parameters?["manifest_path"]?.Value<string>();
            if (string.IsNullOrWhiteSpace(manifestPath))
            {
                manifestPath = action == "gate"
                    ? ThirdPersonPerformanceCaptureWorkflow.LastReplayManifestPath
                    : ThirdPersonPerformanceCaptureWorkflow.LastCaptureManifestPath;
            }
            int limit = Mathf.Clamp(parameters?["limit"]?.Value<int>() ?? 30, 1, 200);
            try
            {
                switch (action)
                {
                    case "status":
                        return PerformanceMcpBridge.Success("Performance Capture status.");
                    case "gates":
                        return new SuccessResponse("Performance Gates listed.", PerformanceMcpBridge.ListGates());
                    case "gate":
                        return new SuccessResponse("Performance Gate manifest loaded.", PerformanceMcpBridge.ReadGate(manifestPath));
                    case "list":
                        return new SuccessResponse("Performance Captures listed.", PerformanceMcpBridge.ListCaptures());
                    case "manifest":
                        return new SuccessResponse("Performance Capture manifest loaded.", PerformanceMcpBridge.ReadManifest(manifestPath));
                    case "summary":
                        return new SuccessResponse("Performance Capture summary loaded.", PerformanceMcpBridge.ReadSummary(manifestPath, limit));
                    case "comparison":
                        return new SuccessResponse("Performance Capture comparison loaded.", PerformanceMcpBridge.ReadComparison(manifestPath, limit));
                    case "analysis":
                        return new SuccessResponse("Performance repeat analysis loaded.", PerformanceMcpBridge.ReadAnalysis(
                            parameters?["analysis_path"]?.Value<string>() ?? ThirdPersonPerformanceCaptureWorkflow.LastAnalysisPath, limit));
                    default:
                        return new ErrorResponse("invalid_action", new { action });
                }
            }
            catch (Exception exception)
            {
                return new ErrorResponse("performance_report_failed", new { action, manifest_path = manifestPath, message = exception.Message });
            }
        }
    }

    static class PerformanceMcpJobScheduler
    {
        [Serializable]
        sealed class JobDocument
        {
            public string schema = "third-person-performance-mcp-job/2";
            public string job_id = string.Empty;
            public string operation = string.Empty;
            public string state = string.Empty;
            public string message = string.Empty;
            public string phase = string.Empty;
            public long elapsed_ms;
            public string updated_utc = string.Empty;
            public string workspace_path = string.Empty;
            public string player_manifest_before = string.Empty;
            public string player_manifest_after = string.Empty;
            public string result_manifest_path = string.Empty;
            public string analysis_path = string.Empty;
        }

        sealed class Job
        {
            public JobDocument Document;
            public Func<string, object> Execute;
            public bool MonitorRun;
            public object FinalResponse;
        }

        static readonly Dictionary<string, Job> Jobs = new Dictionary<string, Job>(StringComparer.Ordinal);
        static readonly object Gate = new object();
        static string JobRoot => Path.Combine(ThirdPersonPerformanceCaptureWorkflow.PerformanceRoot, "McpJobs");

        public static object Start(string operation, bool monitorRun, Func<string, object> execute)
        {
            if (execute == null)
                return new ErrorResponse("performance_job_missing_operation");
            Job job;
            lock (Gate)
            {
                Job active = Jobs.Values.FirstOrDefault(value =>
                    value.Document.operation == operation && IsPending(value.Document.state));
                if (active != null)
                    return Pending(active.Document, "The same Performance job is already active.");
                JobDocument persisted = ReadLatest(operation);
                if (persisted != null)
                {
                    Reconcile(persisted, false);
                    if (IsPending(persisted.state))
                        return Pending(persisted, "The same persisted Performance job is still active.");
                }
                string jobId = Guid.NewGuid().ToString("N");
                var document = new JobDocument
                {
                    job_id = jobId,
                    operation = operation,
                    state = "scheduled",
                    message = "Performance job scheduled.",
                    workspace_path = operation == "build_player"
                        ? Path.Combine(ThirdPersonPerformanceCaptureWorkflow.PerformanceBuildWorkspaceRoot, jobId)
                        : string.Empty,
                    player_manifest_before = ThirdPersonPerformanceCaptureWorkflow.PlayerManifestPath
                };
                job = new Job
                {
                    Document = document,
                    Execute = execute,
                    MonitorRun = monitorRun
                };
                Jobs.Add(jobId, job);
                Write(document);
            }
            void RunOnce()
            {
                EditorApplication.update -= RunOnce;
                Execute(job.Document.job_id);
            }
            EditorApplication.update += RunOnce;
            return Pending(job.Document, "Performance job scheduled.");
        }

        internal static void ReportBuildProgress(string jobId, string phase, string message, long elapsedMilliseconds)
        {
            lock (Gate)
            {
                JobDocument document = Jobs[jobId].Document;
                document.phase = phase;
                document.elapsed_ms = elapsedMilliseconds;
                SetState(document, "running", message);
            }
        }

        public static object Status(string jobId, string operation)
        {
            lock (Gate)
            {
                if (string.IsNullOrWhiteSpace(jobId))
                {
                    Job latest = Jobs.Values.LastOrDefault(value => value.Document.operation == operation);
                    jobId = latest?.Document.job_id ?? ReadLatest(operation)?.job_id;
                    if (string.IsNullOrWhiteSpace(jobId))
                        return new ErrorResponse("job_id_required", new { operation });
                }
                JobDocument document = Jobs.TryGetValue(jobId, out Job job)
                    ? job.Document
                    : Read(jobId);
                if (document == null)
                    return new ErrorResponse("performance_job_lost", new { job_id = jobId, operation });
                if (!string.Equals(document.operation, operation, StringComparison.Ordinal))
                    return new ErrorResponse("performance_job_operation_mismatch", new { job_id = jobId, expected = operation, actual = document.operation });
                Reconcile(document, job != null);
                if (IsPending(document.state))
                    return Pending(document, document.message);
                if (job?.FinalResponse != null)
                    return job.FinalResponse;
                if (document.state == "completed")
                    return new SuccessResponse("Performance job completed.", JobData(document));
                return new ErrorResponse("performance_job_failed", JobData(document));
            }
        }

        static void Execute(string jobId)
        {
            Job job;
            lock (Gate)
            {
                if (!Jobs.TryGetValue(jobId, out job) || job.Document.state != "scheduled")
                    return;
                SetState(job.Document, "running", "Performance job is running.");
            }
            object response = null;
            bool monitoring = false;
            try
            {
                response = job.Execute(job.Document.job_id);
                if (job.Document.operation == "analyze")
                    job.Document.analysis_path = ThirdPersonPerformanceCaptureWorkflow.LastAnalysisPath;
                if (job.MonitorRun)
                {
                    job.Document.result_manifest_path = RunManifestPath(job.Document.operation);
                    SetState(job.Document, "monitoring", $"Performance {job.Document.operation} is running.");
                    monitoring = true;
                    return;
                }
                job.Document.player_manifest_after = ThirdPersonPerformanceCaptureWorkflow.PlayerManifestPath;
                SetState(job.Document, "completed", "Performance job completed.");
            }
            catch (Exception exception)
            {
                response = new ErrorResponse(
                    "performance_job_failed",
                    new { job_id = job.Document.job_id, operation = job.Document.operation, message = exception.Message });
                SetState(job.Document, "faulted", exception.Message);
            }
            finally
            {
                if (!monitoring)
                {
                    lock (Gate)
                        job.FinalResponse = response;
                }
            }
        }

        static void Reconcile(JobDocument document, bool hasRuntimeJob)
        {
            if (document.state == "scheduled" && !hasRuntimeJob)
            {
                SetState(document, "faulted", "Unity reloaded before the scheduled Performance job started.");
                return;
            }
            if ((document.operation == "build_player" || document.operation == "analyze") && IsPending(document.state))
            {
                if (!hasRuntimeJob)
                    SetState(document, "faulted", "Unity reloaded before the Performance job completed; inspect any published artifacts before retrying.");
                return;
            }
            if (document.operation == "build_player" || document.state != "monitoring")
                return;
            if (!string.IsNullOrWhiteSpace(document.result_manifest_path) && File.Exists(document.result_manifest_path))
            {
                CompleteRun(document);
                return;
            }
            if (IsRunActive(document))
                return;
            SetState(document, "faulted", $"Performance {document.operation} ended without publishing a manifest.");
        }

        static void CompleteRun(JobDocument document)
        {
            if (document.operation == PerformanceOperationKinds.Capture)
            {
                PerformanceCaptureManifestDocument manifest =
                    PerformanceMcpBridge.ReadManifestDocument(document.result_manifest_path);
                SetState(
                    document,
                    string.Equals(manifest.status, PerformanceCaptureStatus.Completed.ToString(), StringComparison.Ordinal)
                        ? "completed"
                        : "faulted",
                    manifest.message);
            }
            else
            {
                PerformanceGateManifestDocument manifest =
                    PerformanceMcpBridge.ReadGateDocument(document.result_manifest_path);
                SetState(
                    document,
                    string.Equals(manifest.status, PerformanceCaptureStatus.Completed.ToString(), StringComparison.Ordinal)
                        ? "completed"
                        : "faulted",
                    manifest.message);
            }
        }

        static bool IsRunActive(JobDocument document)
        {
            if (string.IsNullOrWhiteSpace(document.result_manifest_path))
                return false;
            string resultRoot = Path.GetDirectoryName(Path.GetFullPath(document.result_manifest_path));
            string runId = Path.GetFileName(resultRoot);
            string runRoot = Path.GetDirectoryName(resultRoot);
            if (string.IsNullOrWhiteSpace(runId) || string.IsNullOrWhiteSpace(runRoot))
                return false;
            string staging = Path.Combine(runRoot, ".staging", runId);
            if (!Directory.Exists(staging))
                return false;
            string statusPath = Path.Combine(staging, "status.json");
            if (!File.Exists(statusPath))
                return true;
            PerformanceRunStatusDocument status = JsonUtility.FromJson<PerformanceRunStatusDocument>(
                File.ReadAllText(statusPath, Encoding.UTF8));
            if (status == null ||
                !string.Equals(status.schema, PerformanceCaptureSchemas.Status, StringComparison.Ordinal) ||
                !string.Equals(status.operation, document.operation, StringComparison.Ordinal) ||
                !string.Equals(status.run_id, runId, StringComparison.Ordinal))
            {
                return false;
            }
            if (IsTerminalStatus(status.status))
                return status.controller_process_id > 0 && IsProcessRunning(status.controller_process_id);
            if (DateTime.TryParse(
                    status.updated_utc,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out DateTime updatedUtc) &&
                DateTime.UtcNow - updatedUtc.ToUniversalTime() <= TimeSpan.FromMinutes(1))
            {
                return true;
            }
            if (status.controller_process_id <= 0)
                return false;
            return IsProcessRunning(status.controller_process_id);
        }

        static bool IsTerminalStatus(string status) =>
            string.Equals(status, PerformanceCaptureStatus.Completed.ToString(), StringComparison.Ordinal) ||
            string.Equals(status, PerformanceCaptureStatus.Faulted.ToString(), StringComparison.Ordinal) ||
            string.Equals(status, PerformanceCaptureStatus.Cancelled.ToString(), StringComparison.Ordinal);

        static bool IsProcessRunning(int processId)
        {
            try
            {
                using Process process = Process.GetProcessById(processId);
                return !process.HasExited;
            }
            catch
            {
                return false;
            }
        }

        static bool IsPending(string state) =>
            state == "scheduled" || state == "running" || state == "monitoring";

        static void SetState(JobDocument document, string state, string message)
        {
            document.state = state;
            document.message = message ?? string.Empty;
            document.updated_utc = DateTime.UtcNow.ToString("O");
            Write(document);
        }

        static PendingResponse Pending(JobDocument document, string message) =>
            new PendingResponse(
                message,
                2d,
                JobData(document));

        static object JobData(JobDocument document) => new
        {
            job_id = document.job_id,
            operation = document.operation,
            state = document.state,
            message = document.message,
            phase = document.phase,
            elapsed_ms = document.elapsed_ms,
            updated_utc = document.updated_utc,
            workspace_path = document.workspace_path,
            player_manifest_path = document.player_manifest_after,
            result_manifest_path = document.result_manifest_path,
            analysis_path = document.analysis_path
        };

        static string RunManifestPath(string operation)
        {
            if (operation == PerformanceOperationKinds.Smoke)
                return ThirdPersonPerformanceCaptureWorkflow.LastSmokeManifestPath;
            if (operation == PerformanceOperationKinds.Replay)
                return ThirdPersonPerformanceCaptureWorkflow.LastReplayManifestPath;
            if (operation == PerformanceOperationKinds.Capture)
                return ThirdPersonPerformanceCaptureWorkflow.LastCaptureManifestPath;
            throw new InvalidOperationException($"Performance operation '{operation}' has no run manifest.");
        }

        static JobDocument ReadLatest(string operation)
        {
            if (!Directory.Exists(JobRoot))
                return null;
            foreach (string path in Directory.GetFiles(JobRoot, "*.json", SearchOption.TopDirectoryOnly)
                         .OrderByDescending(File.GetLastWriteTimeUtc))
            {
                JobDocument document = ReadPath(path);
                if (document != null && document.operation == operation)
                    return document;
            }
            return null;
        }

        static JobDocument Read(string jobId)
        {
            if (!Guid.TryParseExact(jobId, "N", out _))
                return null;
            return ReadPath(Path.Combine(JobRoot, jobId + ".json"));
        }

        static JobDocument ReadPath(string path)
        {
            if (!File.Exists(path))
                return null;
            JobDocument document = JsonUtility.FromJson<JobDocument>(File.ReadAllText(path, Encoding.UTF8));
            return document != null && document.schema == "third-person-performance-mcp-job/2"
                ? document
                : null;
        }

        static void Write(JobDocument document)
        {
            Directory.CreateDirectory(JobRoot);
            string path = Path.Combine(JobRoot, document.job_id + ".json");
            string temporary = path + ".tmp";
            File.WriteAllText(temporary, JsonUtility.ToJson(document, true), new UTF8Encoding(false));
            if (File.Exists(path))
                File.Replace(temporary, path, null);
            else
                File.Move(temporary, path);
        }
    }

    static class PerformanceMcpBridge
    {
        public static object Success(string message)
        {
            ThirdPersonPerformanceCaptureWorkflow.TryValidateToolchain(out string toolchainStatus);
            return new SuccessResponse(
                message,
                new
                {
                    editor_playing = EditorApplication.isPlayingOrWillChangePlaymode,
                    toolchain_status = toolchainStatus,
                    toolchain_path = ThirdPersonPerformanceCaptureWorkflow.ToolchainPath,
                    trace_path = CharacterFixedInputTraceWorkflow.LastTracePath,
                    scenario_path = ThirdPersonPerformanceCaptureWorkflow.ScenarioPath,
                    capture_profile_path = ThirdPersonPerformanceCaptureWorkflow.CaptureProfilePath,
                    budget_path = ThirdPersonPerformanceCaptureWorkflow.BudgetPath,
                    player_manifest_path = ThirdPersonPerformanceCaptureWorkflow.PlayerManifestPath,
                    baseline_manifest_path = ThirdPersonPerformanceCaptureWorkflow.BaselineManifestPath,
                    run_running = ThirdPersonPerformanceCaptureWorkflow.IsRunRunning,
                    capture_status = ThirdPersonPerformanceCaptureWorkflow.Status,
                    last_smoke_manifest_path = ThirdPersonPerformanceCaptureWorkflow.LastSmokeManifestPath,
                    smoke_gate_ready = ThirdPersonPerformanceCaptureWorkflow.SmokeGateReady,
                    last_replay_manifest_path = ThirdPersonPerformanceCaptureWorkflow.LastReplayManifestPath,
                    replay_gate_ready = ThirdPersonPerformanceCaptureWorkflow.ReplayGateReady,
                    last_capture_manifest_path = ThirdPersonPerformanceCaptureWorkflow.LastCaptureManifestPath
                });
        }

        public static object[] ListCaptures()
        {
            string root = Path.Combine(ThirdPersonPerformanceCaptureWorkflow.PerformanceRoot, "Captures");
            if (!Directory.Exists(root))
                return Array.Empty<object>();
            return Directory.GetFiles(root, "manifest.json", SearchOption.AllDirectories)
                .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}.staging{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(path => path, StringComparer.Ordinal)
                .Select(path => ManifestData(path, ReadManifestDocument(path)))
                .Cast<object>()
                .ToArray();
        }

        public static object[] ListGates()
        {
            string root = Path.Combine(ThirdPersonPerformanceCaptureWorkflow.PerformanceRoot, "Gates");
            if (!Directory.Exists(root))
                return Array.Empty<object>();
            return Directory.GetFiles(root, "manifest.json", SearchOption.AllDirectories)
                .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}.staging{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(path => path, StringComparer.Ordinal)
                .Select(path => GateData(path, ReadGateDocument(path)))
                .Cast<object>()
                .ToArray();
        }

        public static object ReadGate(string path)
        {
            PerformanceGateManifestDocument manifest = ReadGateDocument(path);
            return new { identity = GateData(path, manifest), manifest };
        }

        public static object ReadManifest(string path)
        {
            PerformanceCaptureManifestDocument manifest = ReadManifestDocument(path);
            return new
            {
                identity = ManifestData(path, manifest),
                manifest
            };
        }

        public static object ReadSummary(string manifestPath, int limit)
        {
            string path = ThirdPersonPerformanceCaptureWorkflow.RequireCaptureFile(manifestPath, "summary");
            PerformanceSummaryDocument summary = ReadJson<PerformanceSummaryDocument>(path);
            if (!string.Equals(summary.schema, PerformanceCaptureSchemas.Summary, StringComparison.Ordinal))
                throw new InvalidDataException("Performance summary schema is unsupported; rebuild and capture with the current workflow.");
            return new
            {
                manifest_path = Path.GetFullPath(manifestPath),
                summary_path = path,
                summary.capture_id,
                summary.status,
                summary.presentation_fps,
                summary.target_fps,
                summary.fps_budget_exceeded,
                summary.logic_ticks_per_second,
                summary.dropped_logic_ticks,
                summary.instrumentation_mode,
                summary.timing_basis,
                summary.budget_evaluated,
                summary.capture_seconds,
                summary.total_exclusive_samples,
                summary.unresolved_exclusive_samples,
                summary.unavailable_budget_metrics,
                summary.budget_passed,
                summary.budget_exceeded_count,
                logic_tick_render_frame = summary.logic_tick_render_frame_distribution,
                metrics = summary.metrics,
                instrumentation_points = summary.instrumentation_points.Take(limit).ToArray(),
                hotspots = summary.hotspots.Take(limit).ToArray(),
                thread_hotspots = summary.thread_hotspots.Take(limit).ToArray(),
                wait_evidence = summary.wait_evidence.Take(limit).ToArray()
            };
        }

        public static object ReadComparison(string manifestPath, int limit)
        {
            string path = ThirdPersonPerformanceCaptureWorkflow.RequireCaptureFile(manifestPath, "comparison");
            PerformanceComparisonDocument comparison = ReadJson<PerformanceComparisonDocument>(path);
            if (comparison.schema != PerformanceCaptureSchemas.Comparison)
                throw new InvalidDataException("Performance comparison schema is invalid; regenerate the Capture with the current Controller.");
            return new
            {
                manifest_path = Path.GetFullPath(manifestPath),
                comparison_path = path,
                comparison.schema,
                comparison.baseline_manifest_path,
                comparison.baseline_manifest_hash,
                comparison.baseline_capture_id,
                comparison.candidate_capture_id,
                comparison.status,
                comparison.message,
                comparison.baseline_budget_passed,
                comparison.candidate_budget_passed,
                comparison.baseline_budget_evaluated,
                comparison.candidate_budget_evaluated,
                comparison.budget_delta_available,
                comparison.warnings,
                comparison.budget_exceeded_delta,
                comparison.metrics,
                instrumentation_points = comparison.instrumentation_points.Take(limit).ToArray(),
                hotspots = comparison.hotspots.Take(limit).ToArray()
            };
        }

        public static object ReadAnalysis(string path, int limit)
        {
            PerformanceAnalysisDocument report = ReadJson<PerformanceAnalysisDocument>(path);
            if (report.schema != PerformanceCaptureSchemas.Analysis)
                throw new InvalidDataException("Performance repeat analysis schema is invalid.");
            return new
            {
                analysis_path = Path.GetFullPath(path),
                report.schema,
                report.comparison_kind,
                report.status,
                report.message,
                report.created_utc,
                report.request_hash,
                report.sources,
                report.warnings,
                total_metric_count = report.metrics.Length,
                metrics = report.metrics.Take(limit).ToArray()
            };
        }

        public static PerformanceCaptureManifestDocument ReadManifestDocument(string path)
        {
            PerformanceCaptureManifestDocument manifest = ReadJson<PerformanceCaptureManifestDocument>(path);
            if (!string.Equals(manifest.schema, PerformanceCaptureSchemas.Manifest, StringComparison.Ordinal))
                throw new InvalidDataException("Performance Capture manifest schema is invalid.");
            return manifest;
        }

        public static PerformanceGateManifestDocument ReadGateDocument(string path)
        {
            PerformanceGateManifestDocument manifest = ReadJson<PerformanceGateManifestDocument>(path);
            if (!string.Equals(manifest.schema, PerformanceCaptureSchemas.Gate, StringComparison.Ordinal))
                throw new InvalidDataException("Performance Gate manifest schema is invalid.");
            return manifest;
        }

        public static object ManifestData(string path, PerformanceCaptureManifestDocument manifest) =>
            new
            {
                manifest_path = Path.GetFullPath(path),
                manifest.capture_id,
                manifest.status,
                manifest.stage,
                manifest.message,
                manifest.created_utc,
                manifest.completed_utc,
                manifest.scenario_id,
                manifest.runtime_id,
                manifest.build_id,
                manifest.build_inputs_hash,
                manifest.hardware_identity,
                manifest.metric_catalog_revision,
                manifest.instrumentation_identity,
                manifest.instrumentation_mode,
                manifest.instrumentation_span_layout_revision,
                manifest.instrumentation_manifest_path,
                manifest.instrumentation_span_path,
                manifest.instrumentation_span_hash
            };

        static object GateData(string path, PerformanceGateManifestDocument manifest) =>
            new
            {
                manifest_path = Path.GetFullPath(path),
                manifest.operation,
                manifest.run_id,
                manifest.status,
                manifest.stage,
                manifest.message,
                manifest.completed_utc,
                manifest.scenario_id,
                manifest.build_id,
                manifest.instrumentation_identity,
                manifest.instrumentation_mode,
                manifest.instrumentation_span_layout_revision,
                manifest.start_body_hash,
                manifest.input_sequence_hash,
                manifest.body_trajectory_hash
            };

        static T ReadJson<T>(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                throw new FileNotFoundException("Performance document is missing.", path);
            T value = JsonUtility.FromJson<T>(File.ReadAllText(path, Encoding.UTF8));
            return value == null ? throw new InvalidDataException($"Performance document '{path}' is invalid.") : value;
        }
    }
}
