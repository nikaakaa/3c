using System;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    public enum AgentAuthoringAction
    {
        ExportSnapshot,
        DryRunPatch,
        ApplyPatch,
        Validate
    }

    [Serializable]
    public sealed class AgentAuthoringRequest
    {
        public AgentAuthoringAction action;
        public string definitionAssetPath;
        public string patchJson;
    }

    [Serializable]
    public sealed class AgentAuthoringResponse
    {
        public string action;
        public string definitionAssetPath;
        public bool success;
        public bool applied;
        public bool saved;
        public string errorCode;
        public string errorMessage;
        public AgentGraphSnapshot snapshot;
        public AgentCompileReport report;
    }

    public static class AgentAuthoringActionUtility
    {
        public static bool TryParse(string value, out AgentAuthoringAction action)
        {
            switch (value)
            {
                case "export_snapshot":
                    action = AgentAuthoringAction.ExportSnapshot;
                    return true;
                case "dry_run_patch":
                    action = AgentAuthoringAction.DryRunPatch;
                    return true;
                case "apply_patch":
                    action = AgentAuthoringAction.ApplyPatch;
                    return true;
                case "validate":
                    action = AgentAuthoringAction.Validate;
                    return true;
                default:
                    action = default;
                    return false;
            }
        }

        public static string ToProtocolValue(AgentAuthoringAction action)
        {
            switch (action)
            {
                case AgentAuthoringAction.ExportSnapshot:
                    return "export_snapshot";
                case AgentAuthoringAction.DryRunPatch:
                    return "dry_run_patch";
                case AgentAuthoringAction.ApplyPatch:
                    return "apply_patch";
                case AgentAuthoringAction.Validate:
                    return "validate";
                default:
                    return string.Empty;
            }
        }

        public static bool RequiresPatch(AgentAuthoringAction action)
        {
            return action == AgentAuthoringAction.DryRunPatch || action == AgentAuthoringAction.ApplyPatch;
        }
    }
}
