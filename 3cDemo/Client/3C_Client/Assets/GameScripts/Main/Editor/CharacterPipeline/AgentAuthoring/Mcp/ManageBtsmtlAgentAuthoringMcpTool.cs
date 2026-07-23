using MCPForUnity.Editor.Helpers;
using MCPForUnity.Editor.Tools;
using Newtonsoft.Json.Linq;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring.Mcp
{
    [McpForUnityTool(
        "manage_btsmtl_agent_authoring",
        Description = "Bootstrap an AIController or export, dry-run, apply, and validate the v17 BTSMTL Agent contract for an explicit CharacterController or AIController root domain.")]
    public static class ManageBtsmtlAgentAuthoringMcpTool
    {
        public sealed class Parameters
        {
            [ToolParameter("Action: bootstrap_ai_controller, export_snapshot, dry_run_patch, apply_patch, or validate.")]
            public string action { get; set; }

            [ToolParameter("Root domain: CharacterController or AIController.")]
            public string domain { get; set; }

            [ToolParameter("Exact Assets/... path to the domain root Definition asset.")]
            public string root_asset_path { get; set; }

            [ToolParameter("Agent Patch IR JSON, or AI bootstrap request JSON. Required for bootstrap_ai_controller, dry_run_patch and apply_patch.", Required = false)]
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

            if (!TryGetRequiredString(parameters, "domain", out string domain) || !AgentAuthoringSchema.IsDomain(domain))
                return new ErrorResponse("domain_required", new { action = actionValue, domain });

            if (!TryGetRequiredString(parameters, "root_asset_path", out string rootAssetPath))
                return new ErrorResponse("root_asset_path_required", new { action = actionValue, domain });

            string patchJson = null;
            if (AgentAuthoringActionUtility.RequiresPatch(action) && !TryGetRequiredString(parameters, "patch_json", out patchJson))
                return new ErrorResponse("patch_json_required", new { action = actionValue, domain, rootAssetPath });

            AgentAuthoringResponse response = new AgentPatchAuthoringService().Execute(new AgentAuthoringRequest
            {
                action = action,
                domain = domain,
                rootAssetPath = rootAssetPath,
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
