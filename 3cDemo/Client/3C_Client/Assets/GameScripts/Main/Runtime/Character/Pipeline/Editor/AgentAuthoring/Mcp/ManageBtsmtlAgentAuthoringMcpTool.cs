using MCPForUnity.Editor.Helpers;
using MCPForUnity.Editor.Tools;
using Newtonsoft.Json.Linq;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring.Mcp
{
    [McpForUnityTool(
        "manage_btsmtl_agent_authoring",
        Description = "Export a CharacterPipelineDefinition Agent snapshot, dry-run or transactionally apply Agent Patch IR, or validate the authored BTSMTL graph.")]
    public static class ManageBtsmtlAgentAuthoringMcpTool
    {
        public sealed class Parameters
        {
            [ToolParameter("Action: export_snapshot, dry_run_patch, apply_patch, or validate.")]
            public string action { get; set; }

            [ToolParameter("Exact Assets/... path to a CharacterPipelineDefinition asset.")]
            public string definition_asset_path { get; set; }

            [ToolParameter("Agent Patch IR JSON. Required for dry_run_patch and apply_patch.", Required = false)]
            public string patch_json { get; set; }
        }

        public static object HandleCommand(JObject @params)
        {
            if (@params == null)
                return new ErrorResponse("request_missing");

            var parameters = new ToolParams(@params);
            if (!TryGetRequiredString(parameters, "action", out string actionValue))
                return new ErrorResponse("action_required");

            if (!AgentAuthoringActionUtility.TryParse(actionValue, out AgentAuthoringAction action))
                return new ErrorResponse("unsupported_action", new { action = actionValue });

            if (!TryGetRequiredString(parameters, "definition_asset_path", out string definitionAssetPath))
                return new ErrorResponse("definition_asset_path_required", new { action = actionValue });

            string patchJson = null;
            if (AgentAuthoringActionUtility.RequiresPatch(action) && !TryGetRequiredString(parameters, "patch_json", out patchJson))
                return new ErrorResponse("patch_json_required", new { action = actionValue, definitionAssetPath });

            AgentAuthoringResponse response = new AgentPatchAuthoringService().Execute(new AgentAuthoringRequest
            {
                action = action,
                definitionAssetPath = definitionAssetPath,
                patchJson = patchJson
            });

            if (!response.success)
                return new ErrorResponse(string.IsNullOrEmpty(response.errorCode) ? "authoring_failed" : response.errorCode, response);

            return new SuccessResponse($"BTSMTL Agent authoring action completed: {response.action}", response);
        }

        static bool TryGetRequiredString(ToolParams parameters, string key, out string value)
        {
            value = parameters.Get(key);
            return !string.IsNullOrWhiteSpace(value);
        }
    }
}
