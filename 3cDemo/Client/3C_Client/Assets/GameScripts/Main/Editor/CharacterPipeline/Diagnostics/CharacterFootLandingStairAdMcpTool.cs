using System;
using MCPForUnity.Editor.Helpers;
using MCPForUnity.Editor.Tools;
using Newtonsoft.Json.Linq;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    [McpForUnityTool(
        "character.foot_landing_stair_ad",
        Description = "Start, inspect, or stop the formal Gameplay Lab stair AD foot landing sampler. Uses the existing Launcher route driver and writes the normal FootLandingSamples CSV.",
        StructuredOutput = true,
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
                active = GameplayLabFootIkKeyboardRouteDriver.IsActive,
                pending = GameplayLabFootIkKeyboardRouteDriver.IsPending,
                phase = GameplayLabFootIkKeyboardRouteDriver.Phase.ToString(),
                lap = GameplayLabFootIkKeyboardRouteDriver.Lap,
                sampling = CharacterFootLandingPredictionSampler.IsCapturing,
                saved_path = CharacterFootLandingPredictionSampler.LastSavedPath,
                report = GameplayLabFootIkKeyboardRouteDriver.LastReport
                }
            };
        }
    }
}
