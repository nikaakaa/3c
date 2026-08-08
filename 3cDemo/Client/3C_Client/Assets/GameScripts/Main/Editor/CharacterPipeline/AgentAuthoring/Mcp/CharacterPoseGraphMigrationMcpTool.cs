using System;
using MCPForUnity.Editor.Helpers;
using MCPForUnity.Editor.Tools;
using Newtonsoft.Json.Linq;
using ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring.Mcp
{
    [McpForUnityTool(
        "character.migrate_legacy_pose_state_graphs",
        Description = "Run the one-time typed Presentation Pose Graph migration for one exact CharacterPipelineDefinition. It replaces deleted legacy Foot Placement managed payloads with the current FootGrounding typed payload, saves the formal Pose Graph asset, does not scan, select, build or modify runtime/compiler products.",
        StructuredOutput = true,
        HasBehaviorAnnotations = true,
        ReadOnlyHint = false,
        DestructiveHint = true,
        IdempotentHint = true,
        OpenWorldHint = false)]
    public static class CharacterPoseGraphMigrationMcpTool
    {
        public sealed class Parameters
        {
            [ToolParameter("Exact Assets/... path to one CharacterPipelineDefinition asset.")]
            public string definition_asset_path { get; set; }
        }

        public static object HandleCommand(JObject @params)
        {
            if (@params == null)
                return new ErrorResponse("request_missing");
            foreach (JProperty property in @params.Properties())
            {
                if (!string.Equals(property.Name, "definition_asset_path", StringComparison.Ordinal))
                    return new ErrorResponse("unknown_parameter", new { parameter = property.Name });
            }
            JToken token = @params["definition_asset_path"];
            if (token?.Type != JTokenType.String || string.IsNullOrWhiteSpace(token.Value<string>()))
                return new ErrorResponse("definition_asset_path_required");
            CharacterPresentationPoseGraphMigrationResponse response =
                CharacterPresentationPoseGraphMigrationService.Migrate(token.Value<string>());
            if (!response.success)
                return new ErrorResponse(response.errorCode ?? "pose_graph_migration_failed", response);
            return new SuccessResponse(
                "Legacy Presentation Pose Graph migration completed.",
                response);
        }
    }
}
