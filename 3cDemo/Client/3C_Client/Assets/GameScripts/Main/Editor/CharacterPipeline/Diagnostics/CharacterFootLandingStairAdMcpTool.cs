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
        Description = "Start, inspect, or stop the formal Gameplay Lab stair foot landing sampler. Supports A/D stress and straight up/down routes through the existing Launcher route driver and writes the normal FootLandingSamples CSV.",
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
            [ToolParameter("Action: start, start_straight, status, or stop. Defaults to status.", Required = false)]
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
                    saved_path = CharacterFootLandingPredictionSampler.LastSavedPath,
                    report = GameplayLabFootIkKeyboardRouteDriver.LastReport
                }
            };
        }
    }
}
