using System;
using MCPForUnity.Editor.Helpers;
using MCPForUnity.Editor.Tools;
using Newtonsoft.Json.Linq;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    [McpForUnityTool(
        "character.foot_landing_stair_ad",
        Description = "Start, inspect, or stop the formal Gameplay Lab stair foot landing sampler. Each run publishes samples.csv, facts.json, and one JSON per diagnosis under diagnoses/ without a global pass/fail result.",
        StructuredOutput = true,
        AutoRegister = true,
        RequiresPolling = false,
        HasBehaviorAnnotations = true,
        ReadOnlyHint = false,
        DestructiveHint = false,
        IdempotentHint = false,
        OpenWorldHint = false)]
    public static class CharacterFootLandingStairAdMcpTool
    {
        public sealed class Parameters
        {
            [ToolParameter("Action: start, start_straight, status, stop, or analyze_latest. Defaults to status.", Required = false)]
            public string action { get; set; }
        }

        public static object HandleCommand(JObject @params)
        {
            string action = @params?["action"]?.Value<string>() ?? "status";
            try
            {
                switch (action.Trim().ToLowerInvariant())
                {
                    case "start":
                        GameplayLabFootIkKeyboardRouteDriver.Start();
                        return Success("Foot landing stair AD automation started.");
                    case "start_straight":
                        GameplayLabFootIkKeyboardRouteDriver.StartStraight();
                        return Success("Foot landing stair straight automation started.");
                    case "status":
                        return Success("Foot landing stair automation status.");
                    case "stop":
                        GameplayLabFootIkKeyboardRouteDriver.ClearPending();
                        GameplayLabFootIkKeyboardRouteDriver.Stop();
                        return Success("Foot landing stair AD automation stopped.");
                    case "analyze_latest":
                        CharacterFootLandingPredictionSampler.AnalyzeLastSavedSamples();
                        return Success("Latest sealed Foot Landing samples were analyzed.");
                    default:
                        return new ErrorResponse("invalid_action", new { action });
                }
            }
            catch (Exception exception)
            {
                return new ErrorResponse("foot_landing_stair_ad_failed", new { action, message = exception.Message });
            }
        }

        static object Success(string message)
        {
            bool hasPlayerPosition =
                GameplayLabFootIkKeyboardRouteDriver.TryGetPlayerPosition(out Vector3 playerPosition);
            return new
            {
                success = true,
                message,
                data = new
                {
                    playing = EditorApplication.isPlaying,
                    paused = EditorApplication.isPaused,
                    active = GameplayLabFootIkKeyboardRouteDriver.IsActive,
                    pending = GameplayLabFootIkKeyboardRouteDriver.IsPending,
                    mode = GameplayLabFootIkKeyboardRouteDriver.Mode.ToString(),
                    phase = GameplayLabFootIkKeyboardRouteDriver.PhaseName,
                    lap = GameplayLabFootIkKeyboardRouteDriver.Lap,
                    sample_seconds = GameplayLabFootIkKeyboardRouteDriver.SampleSecondsValue,
                    player_position_available = hasPlayerPosition,
                    player_position = hasPlayerPosition
                        ? new { x = playerPosition.x, y = playerPosition.y, z = playerPosition.z }
                        : null,
                    sampling = CharacterFootLandingPredictionSampler.IsCapturing,
                    runtime_target_count = AnimationPresentationRuntimeTargetRegistry.Targets.Count,
                    captured_frame_count = CharacterFootLandingPredictionSampler.CapturedFrameCount,
                    pending_frame_count = CharacterFootLandingPredictionSampler.PendingFrameCount,
                    dropped_pending_frame_count = CharacterFootLandingPredictionSampler.DroppedPendingFrameCount,
                    last_saved_frame_count = CharacterFootLandingPredictionSampler.LastSavedFrameCount,
                    sample_directory = CharacterFootLandingPredictionSampler.LastSavedDirectory,
                    samples_path = CharacterFootLandingPredictionSampler.LastSavedPath,
                    facts_path = CharacterFootLandingPredictionSampler.LastSavedFactsPath,
                    diagnoses_directory = CharacterFootLandingPredictionSampler.LastSavedDiagnosisDirectory,
                    fact_event_count = CharacterFootLandingPredictionSampler.LastFactEventCount,
                    diagnosis_target_count = CharacterFootLandingPredictionSampler.LastDiagnosisTargetCount,
                    diagnosis_match_count = CharacterFootLandingPredictionSampler.LastDiagnosisMatchCount,
                    diagnostic_summary = CharacterFootLandingPredictionSampler.LastDiagnosticSummary,
                    automation_status = GameplayLabFootIkKeyboardRouteDriver.LastDiagnosticSummary
                }
            };
        }
    }
}
