using System;
using System.Collections.Generic;
using System.Linq;
using MCPForUnity.Editor.Helpers;
using MCPForUnity.Editor.Tools;
using Newtonsoft.Json.Linq;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring.Mcp
{
    [McpForUnityTool("btsmtl.checkout_document", Description = "Create or refresh the canonical Document v3 package, including Blackboard declarations with optional inputBinding.inputValueId and factProjection payloads, readonly Linked Pose Interface context, and editable Presentation Profile, Linked Implementation, Entry Graph, Group and selector fragments for CharacterController. Returns the exact package path, sync state, source revision and document hashes without mutating Unity authoring or building Character products.", StructuredOutput = true, RequiresPolling = true, BackgroundPollingStatus = true, PollAction = "status", MaxPollSeconds = 600, HasBehaviorAnnotations = true, ReadOnlyHint = false, DestructiveHint = false, IdempotentHint = true, OpenWorldHint = false)]
    public static class CheckoutBtsmtlDocumentMcpTool
    {
        public sealed class Parameters
        {
            [ToolParameter("Omit or use start to create a job; use status to poll one job.", Required = false)]
            public string action { get; set; }

            [ToolParameter("Stable job identity returned by the initial call; required for status.", Required = false)]
            public string job_id { get; set; }

            [ToolParameter("Root domain required for start: CharacterController or AIController.", Required = false)]
            public string domain { get; set; }

            [ToolParameter("Exact Assets/... root Definition path required for start.", Required = false)]
            public string root_asset_path { get; set; }
        }

        public static object HandleCommand(JObject @params)
        {
            return BtsmtlAgentAuthoringMcpJobScheduler.Handle(
                @params,
                AgentAuthoringAction.CheckoutDocument,
                false,
                false);
        }
    }

    [McpForUnityTool("btsmtl.rebase_document", Description = "Accept current Unity authoring and readonly context as the new Document v3 baseline while preserving editable target files, including complete canonical local Pose Graph, Timeline and Linked Implementation Entry Graph closures admitted by the service-owned manifest lifecycle. Returns the rebased hashes and sync state without mutating Unity authoring or building Character products.", StructuredOutput = true, RequiresPolling = true, BackgroundPollingStatus = true, PollAction = "status", MaxPollSeconds = 600, HasBehaviorAnnotations = true, ReadOnlyHint = false, DestructiveHint = true, IdempotentHint = true, OpenWorldHint = false)]
    public static class RebaseBtsmtlDocumentMcpTool
    {
        public sealed class Parameters
        {
            [ToolParameter("Omit or use start to create a job; use status to poll one job.", Required = false)]
            public string action { get; set; }

            [ToolParameter("Stable job identity returned by the initial call; required for status.", Required = false)]
            public string job_id { get; set; }

            [ToolParameter("Root domain required for start: CharacterController or AIController.", Required = false)]
            public string domain { get; set; }

            [ToolParameter("Exact Assets/... root Definition path required for start.", Required = false)]
            public string root_asset_path { get; set; }

            [ToolParameter("Explicit true confirmation required for start.", Required = false)]
            public bool confirm_rebase { get; set; }
        }

        public static object HandleCommand(JObject @params)
        {
            return BtsmtlAgentAuthoringMcpJobScheduler.Handle(
                @params,
                AgentAuthoringAction.RebaseDocument,
                false,
                true);
        }
    }

    [McpForUnityTool("btsmtl.dry_run_document", Description = "Strictly reconcile the complete Document v3 editable target, including Blackboard declaration, inputBinding.inputValueId and factProjection paths, into the shared typed Mutation plan. Reject readonly Interface edits, runtime handles, legacy authority/syncPolicy fields and old flat Blackboard payloads. Returns the exact effective document hash, plan hash, planned diff and identity-preserving diagnostics without mutating Unity authoring or building Character products.", StructuredOutput = true, RequiresPolling = true, BackgroundPollingStatus = true, PollAction = "status", MaxPollSeconds = 600, HasBehaviorAnnotations = true, ReadOnlyHint = true, DestructiveHint = false, IdempotentHint = true, OpenWorldHint = false)]
    public static class DryRunBtsmtlDocumentMcpTool
    {
        public sealed class Parameters
        {
            [ToolParameter("Omit or use start to create a job; use status to poll one job.", Required = false)]
            public string action { get; set; }

            [ToolParameter("Stable job identity returned by the initial call; required for status.", Required = false)]
            public string job_id { get; set; }

            [ToolParameter("Root domain required for start: CharacterController or AIController.", Required = false)]
            public string domain { get; set; }

            [ToolParameter("Exact Assets/... root Definition path required for start.", Required = false)]
            public string root_asset_path { get; set; }
        }

        public static object HandleCommand(JObject @params)
        {
            return BtsmtlAgentAuthoringMcpJobScheduler.Handle(
                @params,
                AgentAuthoringAction.DryRunDocument,
                false,
                false);
        }
    }

    [McpForUnityTool("btsmtl.apply_document", Description = "Apply the complete Document v3 target only when expected_document_hash exactly matches the latest dry-run effective hash. Service-admitted local Pose Graph, Timeline, Linked Implementation, Entry Graph and selector creation join the same asset-level transaction; any failure rolls back. Success saves authoring, reverse-exports formal GUID/local file identities and the canonical manifest without building Character products or switching an active runtime Implementation.", StructuredOutput = true, RequiresPolling = true, BackgroundPollingStatus = true, PollAction = "status", MaxPollSeconds = 600, HasBehaviorAnnotations = true, ReadOnlyHint = false, DestructiveHint = true, IdempotentHint = false, OpenWorldHint = false)]
    public static class ApplyBtsmtlDocumentMcpTool
    {
        public sealed class Parameters
        {
            [ToolParameter("Omit or use start to create a job; use status to poll one job.", Required = false)]
            public string action { get; set; }

            [ToolParameter("Stable job identity returned by the initial call; required for status.", Required = false)]
            public string job_id { get; set; }

            [ToolParameter("Root domain required for start: CharacterController or AIController.", Required = false)]
            public string domain { get; set; }

            [ToolParameter("Exact Assets/... root Definition path required for start.", Required = false)]
            public string root_asset_path { get; set; }

            [ToolParameter("Exact document hash returned by the latest successful dry-run for this same Document v3 package.", Required = false)]
            public string expected_document_hash { get; set; }
        }

        public static object HandleCommand(JObject @params)
        {
            return BtsmtlAgentAuthoringMcpJobScheduler.Handle(
                @params,
                AgentAuthoringAction.ApplyDocument,
                true,
                false);
        }
    }

    [McpForUnityTool("btsmtl.validate", Description = "Validate the current Unity authoring root through the formal domain validators, including Linked Interface/Implementation Entry coverage, Graph capability context, Group selector uniqueness and Equipment candidate closure, without editing, checking out a package or building Character products. Returns identity-preserving structured diagnostics.", StructuredOutput = true, RequiresPolling = true, BackgroundPollingStatus = true, PollAction = "status", MaxPollSeconds = 600, HasBehaviorAnnotations = true, ReadOnlyHint = true, DestructiveHint = false, IdempotentHint = true, OpenWorldHint = false)]
    public static class ValidateBtsmtlAgentMcpTool
    {
        public sealed class Parameters
        {
            [ToolParameter("Omit or use start to create a job; use status to poll one job.", Required = false)]
            public string action { get; set; }

            [ToolParameter("Stable job identity returned by the initial call; required for status.", Required = false)]
            public string job_id { get; set; }

            [ToolParameter("Root domain required for start: CharacterController or AIController.", Required = false)]
            public string domain { get; set; }

            [ToolParameter("Exact Assets/... root Definition path required for start.", Required = false)]
            public string root_asset_path { get; set; }
        }

        public static object HandleCommand(JObject @params)
        {
            return BtsmtlAgentAuthoringMcpJobScheduler.Handle(
                @params,
                AgentAuthoringAction.Validate,
                false,
                false);
        }
    }

    static class BtsmtlAgentAuthoringMcpBridge
    {
        public static bool TryNormalizeTransportParameters(
            JObject parameters,
            bool requiresHash,
            bool requiresConfirmation,
            out JObject normalized,
            out ErrorResponse error)
        {
            normalized = new JObject();
            error = null;
            foreach (JProperty property in parameters.Properties())
            {
                if (!TryGetCanonicalParameterName(
                        property.Name,
                        out string canonicalName) ||
                    string.Equals(
                        canonicalName,
                        "expected_document_hash",
                        StringComparison.Ordinal) &&
                    !requiresHash ||
                    string.Equals(
                        canonicalName,
                        "confirm_rebase",
                        StringComparison.Ordinal) &&
                    !requiresConfirmation)
                {
                    error = new ErrorResponse(
                        "unknown_parameter",
                        new { parameter = property.Name });
                    return false;
                }
                if (normalized.Property(canonicalName) != null)
                {
                    error = new ErrorResponse(
                        "duplicate_parameter",
                        new
                        {
                            parameter = canonicalName,
                            transport_parameter = property.Name
                        });
                    return false;
                }
                normalized.Add(
                    canonicalName,
                    property.Value.DeepClone());
            }
            return true;
        }

        public static object Execute(
            JObject parameters,
            AgentAuthoringAction action,
            bool requiresHash,
            bool requiresConfirmation)
        {
            if (parameters == null)
                return new ErrorResponse("request_missing");
            var allowed = new HashSet<string>(StringComparer.Ordinal) { "domain", "root_asset_path" };
            if (requiresHash)
                allowed.Add("expected_document_hash");
            if (requiresConfirmation)
                allowed.Add("confirm_rebase");
            string unknown = parameters.Properties()
                .Select(property => property.Name)
                .FirstOrDefault(name => !allowed.Contains(name));
            if (!string.IsNullOrEmpty(unknown))
                return new ErrorResponse("unknown_parameter", new { parameter = unknown });
            if (!TryGetString(parameters, "domain", out string domain) || !AgentAuthoringSchema.IsDomain(domain))
                return new ErrorResponse("domain_required", new { domain });
            if (!TryGetString(parameters, "root_asset_path", out string rootAssetPath))
                return new ErrorResponse("root_asset_path_required", new { domain });

            string expectedDocumentHash = null;
            if (requiresHash && !TryGetString(parameters, "expected_document_hash", out expectedDocumentHash))
                return new ErrorResponse("expected_document_hash_required", new { domain, rootAssetPath });
            bool confirmRebase = false;
            if (requiresConfirmation)
            {
                JToken confirmation = parameters["confirm_rebase"];
                if (confirmation?.Type != JTokenType.Boolean || !confirmation.Value<bool>())
                    return new ErrorResponse("rebase_confirmation_required", new { domain, rootAssetPath });
                confirmRebase = true;
            }

            AgentAuthoringResponse response = new AgentAuthoringDocumentApplicationService().Execute(new AgentAuthoringRequest
            {
                action = action,
                domain = domain,
                rootAssetPath = rootAssetPath,
                expectedDocumentHash = expectedDocumentHash,
                confirmRebase = confirmRebase
            });
            if (!response.success)
                return new ErrorResponse(string.IsNullOrEmpty(response.errorCode) ? "authoring_failed" : response.errorCode, response);
            return new SuccessResponse($"BTSMTL Agent Document action completed: {response.action}", response);
        }

        static bool TryGetCanonicalParameterName(
            string name,
            out string canonicalName)
        {
            switch (name)
            {
                case "action":
                case "domain":
                    canonicalName = name;
                    return true;
                case "job_id":
                case "jobId":
                    canonicalName = "job_id";
                    return true;
                case "root_asset_path":
                case "rootAssetPath":
                    canonicalName = "root_asset_path";
                    return true;
                case "expected_document_hash":
                case "expectedDocumentHash":
                    canonicalName = "expected_document_hash";
                    return true;
                case "confirm_rebase":
                case "confirmRebase":
                    canonicalName = "confirm_rebase";
                    return true;
                default:
                    canonicalName = string.Empty;
                    return false;
            }
        }

        static bool TryGetString(JObject parameters, string key, out string value)
        {
            value = null;
            JToken token = parameters[key];
            if (token?.Type != JTokenType.String)
                return false;
            value = token.Value<string>();
            return !string.IsNullOrWhiteSpace(value);
        }
    }
}
