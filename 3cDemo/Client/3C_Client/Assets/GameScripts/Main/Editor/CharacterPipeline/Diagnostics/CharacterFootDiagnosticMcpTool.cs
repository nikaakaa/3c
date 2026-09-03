using System;
using MCPForUnity.Editor.Helpers;
using MCPForUnity.Editor.Tools;
using Newtonsoft.Json.Linq;
using UnityEditor;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    [McpForUnityTool(
        "character.foot_diagnostics",
        Description = "Select, capture, finalize, and analyze the generated Foot diagnostic capability through the same workflow used by the 3C Launcher.",
        StructuredOutput = true,
        AutoRegister = true,
        RequiresPolling = false,
        HasBehaviorAnnotations = true,
        ReadOnlyHint = false,
        DestructiveHint = false,
        IdempotentHint = false,
        OpenWorldHint = false)]
    public static class CharacterFootDiagnosticMcpTool
    {
        public sealed class Parameters
        {
            [ToolParameter("Action: status, compilation_enable, compilation_disable, select_sampler, capture_start, capture_start_controlled, capture_window_open, capture_window_close, capture_stop, analyze_last, analyze_existing, or open_report. Defaults to status.", Required = false)]
            public string action { get; set; }

            [ToolParameter("Sampler for select_sampler: core or full.", Required = false)]
            public string sampler { get; set; }

            [ToolParameter("Exact completed capability.manifest.json path for analyze_existing.", Required = false)]
            public string manifest_path { get; set; }
        }

        public static object HandleCommand(JObject parameters)
        {
            string action = parameters?.Value<string>("action") ?? "status";
            string sampler = parameters?.Value<string>("sampler") ?? string.Empty;
            string manifest = parameters?.Value<string>("manifest_path") ??
                string.Empty;
            try
            {
                switch (action.Trim().ToLowerInvariant())
                {
                    case "status":
                        break;
                    case "compilation_enable":
                        CharacterDiagnosticCompilationMode.EnableFootCapture();
                        break;
                    case "compilation_disable":
                        CharacterDiagnosticCompilationMode.DisableFootCapture();
                        break;
                    case "select_sampler":
                        CharacterFootDiagnosticSampling.SelectSampler(
                            ResolveSampler(sampler));
                        break;
                    case "capture_start":
                        CharacterFootDiagnosticSampling.StartSampling();
                        break;
                    case "capture_start_controlled":
                        CharacterFootDiagnosticSampling
                            .StartControlledSampling();
                        break;
                    case "capture_window_open":
                        CharacterFootDiagnosticSampling
                            .OpenControlledCaptureWindow();
                        break;
                    case "capture_window_close":
                        CharacterFootDiagnosticSampling
                            .CloseControlledCaptureWindow();
                        break;
                    case "capture_stop":
                        CharacterFootDiagnosticSampling.StopAndSaveSampling();
                        break;
                    case "analyze_last":
                        CharacterFootDiagnosticSampling.AnalyzeLastCapture();
                        break;
                    case "analyze_existing":
                        if (string.IsNullOrWhiteSpace(manifest))
                        {
                            throw new ArgumentException(
                                "manifest_path is required.",
                                nameof(manifest));
                        }
                        CharacterFootDiagnosticSampling
                            .AnalyzeExistingCapture(manifest);
                        break;
                    case "open_report":
                        CharacterFootDiagnosticSampling.OpenLastReport();
                        break;
                    default:
                        return new ErrorResponse(
                            "invalid_action",
                            new { action });
                }
                return Success(action);
            }
            catch (Exception exception)
            {
                return new ErrorResponse(
                    "foot_diagnostics_failed",
                    new
                    {
                        action,
                        sampler,
                        manifest_path = manifest,
                        message = exception.Message
                    });
            }
        }

        static string ResolveSampler(string sampler)
        {
            if (string.Equals(sampler, "core", StringComparison.OrdinalIgnoreCase))
                return CharacterFootDiagnosticSampling.CoreSamplerId;
            if (string.Equals(sampler, "full", StringComparison.OrdinalIgnoreCase))
                return CharacterFootDiagnosticSampling.FullSamplerId;
            throw new ArgumentException(
                "sampler must be core or full.",
                nameof(sampler));
        }

        static object Success(string action) => new SuccessResponse(
            "Foot diagnostic workflow state loaded.",
            new
            {
                action,
                playing = EditorApplication.isPlaying,
                capture_compilation =
                    CharacterDiagnosticCompilationMode.IsFootCaptureEnabled,
                sampling_available = CharacterFootDiagnosticSampling.IsAvailable,
                analysis_available =
                    CharacterFootDiagnosticSampling.IsAnalysisAvailable,
                selected_sampler =
                    CharacterFootDiagnosticSampling.SelectedSamplerId,
                capturing = CharacterFootDiagnosticSampling.IsCapturing,
                finalizing = CharacterFootDiagnosticSampling.IsFinalizing,
                controlled_window =
                    CharacterFootDiagnosticSampling.IsControlledCaptureWindow,
                capture_window_open =
                    CharacterFootDiagnosticSampling.IsCaptureWindowOpen,
                captured_frame_count =
                    CharacterFootDiagnosticSampling.CapturedFrameCount,
                last_saved_frame_count =
                    CharacterFootDiagnosticSampling.LastSavedFrameCount,
                capture_failure = CharacterFootDiagnosticSampling.LastFailure,
                sample_path = CharacterFootDiagnosticSampling.LastSavedPath,
                manifest_path = CharacterFootDiagnosticSampling.LastManifestPath,
                analyzing = CharacterFootDiagnosticSampling.IsAnalyzing,
                analysis_failure =
                    CharacterFootDiagnosticSampling.LastAnalysisFailure,
                analysis_directory =
                    CharacterFootDiagnosticSampling.LastAnalysisDirectory,
                report_path = CharacterFootDiagnosticSampling.LastReportPath
            });
    }
}
