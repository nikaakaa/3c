using System;
using MCPForUnity.Editor.Helpers;
using MCPForUnity.Editor.Tools;
using Newtonsoft.Json.Linq;
using ThirdPersonCharacter.Pipeline.Diagnostics.PresentationReplication;
using UnityEditor;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    [McpForUnityTool(
        "character.presentation_replication_diagnostics",
        Description = "Select, capture, finalize, and analyze the generated Character Presentation Replication diagnostic capability.",
        StructuredOutput = true,
        AutoRegister = true,
        RequiresPolling = false,
        HasBehaviorAnnotations = true,
        ReadOnlyHint = false,
        DestructiveHint = false,
        IdempotentHint = false,
        OpenWorldHint = false)]
    public static class CharacterPresentationReplicationDiagnosticMcpTool
    {
        public sealed class Parameters
        {
            [ToolParameter("Action: status, select_sampler, capture_start, capture_start_controlled, capture_window_open, capture_window_close, capture_stop, analyze_last, analyze_existing, or open_report. Defaults to status.", Required = false)]
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
            string manifest = parameters?.Value<string>("manifest_path") ?? string.Empty;
            try
            {
                switch (action.Trim().ToLowerInvariant())
                {
                    case "status":
                        break;
                    case "select_sampler":
                        CharacterPresentationReplicationDiagnosticSampling
                            .SelectSampler(ResolveSampler(sampler));
                        break;
                    case "capture_start":
                        CharacterPresentationReplicationDiagnosticSampling
                            .StartSampling();
                        break;
                    case "capture_start_controlled":
                        CharacterPresentationReplicationDiagnosticSampling
                            .StartControlledSampling();
                        break;
                    case "capture_window_open":
                        CharacterPresentationReplicationDiagnosticSampling
                            .OpenControlledCaptureWindow();
                        break;
                    case "capture_window_close":
                        CharacterPresentationReplicationDiagnosticSampling
                            .CloseControlledCaptureWindow();
                        break;
                    case "capture_stop":
                        CharacterPresentationReplicationDiagnosticSampling
                            .StopAndSaveSampling();
                        break;
                    case "analyze_last":
                        CharacterPresentationReplicationDiagnosticSampling
                            .AnalyzeLastCapture();
                        break;
                    case "analyze_existing":
                        if (string.IsNullOrWhiteSpace(manifest))
                        {
                            throw new ArgumentException(
                                "manifest_path is required.",
                                nameof(manifest));
                        }
                        CharacterPresentationReplicationDiagnosticSampling
                            .AnalyzeExistingCapture(manifest);
                        break;
                    case "open_report":
                        CharacterPresentationReplicationDiagnosticSampling
                            .OpenLastReport();
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
                    "presentation_replication_diagnostics_failed",
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
                return CharacterPresentationReplicationDiagnosticSampling.CoreSamplerId;
            if (string.Equals(sampler, "full", StringComparison.OrdinalIgnoreCase))
                return CharacterPresentationReplicationDiagnosticSampling.FullSamplerId;
            throw new ArgumentException(
                "sampler must be core or full.",
                nameof(sampler));
        }

        static object Success(string action) => new SuccessResponse(
            "Character Presentation Replication diagnostic workflow state loaded.",
            new
            {
                action,
                reference_profile =
                    CharacterPresentationReplicationDiagnosticIdentity.ReferenceProfileId,
                playing = EditorApplication.isPlaying,
                sampling_available =
                    CharacterPresentationReplicationDiagnosticSampling.IsAvailable,
                analysis_available =
                    CharacterPresentationReplicationDiagnosticSampling.IsAnalysisAvailable,
                selected_sampler =
                    CharacterPresentationReplicationDiagnosticSampling.SelectedSamplerId,
                capturing =
                    CharacterPresentationReplicationDiagnosticSampling.IsCapturing,
                finalizing =
                    CharacterPresentationReplicationDiagnosticSampling.IsFinalizing,
                controlled_window =
                    CharacterPresentationReplicationDiagnosticSampling.IsControlledCaptureWindow,
                capture_window_open =
                    CharacterPresentationReplicationDiagnosticSampling.IsCaptureWindowOpen,
                captured_frame_count =
                    CharacterPresentationReplicationDiagnosticSampling.CapturedFrameCount,
                last_saved_frame_count =
                    CharacterPresentationReplicationDiagnosticSampling.LastSavedFrameCount,
                capture_failure =
                    CharacterPresentationReplicationDiagnosticSampling.LastFailure,
                sample_path =
                    CharacterPresentationReplicationDiagnosticSampling.LastSavedPath,
                manifest_path =
                    CharacterPresentationReplicationDiagnosticSampling.LastManifestPath,
                analyzing =
                    CharacterPresentationReplicationDiagnosticSampling.IsAnalyzing,
                analysis_failure =
                    CharacterPresentationReplicationDiagnosticSampling.LastAnalysisFailure,
                analysis_directory =
                    CharacterPresentationReplicationDiagnosticSampling.LastAnalysisDirectory,
                report_path =
                    CharacterPresentationReplicationDiagnosticSampling.LastReportPath
            });
    }
}
