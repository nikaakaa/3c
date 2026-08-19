using System;
using MCPForUnity.Editor.Helpers;
using MCPForUnity.Editor.Tools;
using Newtonsoft.Json.Linq;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using UnityEditor;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    [McpForUnityTool(
        "character.foot_landing_stair_ad",
        Description = "Start, inspect, or stop the formal Gameplay Lab stair AD foot landing sampler. Uses the existing Launcher route driver and writes the normal FootLandingSamples CSV.",
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
            [ToolParameter("Action: start, status, or stop. Defaults to status.", Required = false)]
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
                    case "status":
                        return Success("Foot landing stair AD automation status.");
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
                    phase = GameplayLabFootIkKeyboardRouteDriver.Phase.ToString(),
                    lap = GameplayLabFootIkKeyboardRouteDriver.Lap,
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
