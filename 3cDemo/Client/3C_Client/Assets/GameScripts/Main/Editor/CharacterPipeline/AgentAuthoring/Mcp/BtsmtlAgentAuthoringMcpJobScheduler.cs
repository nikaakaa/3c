using System;
using System.Collections.Generic;
using MCPForUnity.Editor.Helpers;
using Newtonsoft.Json.Linq;
using UnityEditor;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring.Mcp
{
    static class BtsmtlAgentAuthoringMcpJobScheduler
    {
        sealed class Job
        {
            public string Id;
            public AgentAuthoringAction Action;
            public string Domain;
            public string RootAssetPath;
            public JObject Parameters;
            public bool RequiresHash;
            public bool RequiresConfirmation;
            public bool Started;
            public bool Completed;
            public object FinalResponse;
        }

        static readonly Dictionary<string, Job> Jobs =
            new Dictionary<string, Job>(StringComparer.Ordinal);
        static readonly object JobsLock = new object();

        public static object Handle(
            JObject parameters,
            AgentAuthoringAction action,
            bool requiresHash,
            bool requiresConfirmation)
        {
            if (parameters == null)
                return new ErrorResponse("request_missing");
            if (!BtsmtlAgentAuthoringMcpBridge.TryNormalizeTransportParameters(
                parameters,
                requiresHash,
                requiresConfirmation,
                out JObject canonicalParameters,
                out ErrorResponse parameterError))
                return parameterError;
            parameters = canonicalParameters;
            JToken actionToken = parameters["action"];
            if (actionToken != null && actionToken.Type != JTokenType.String)
                return new ErrorResponse(
                    "poll_action_invalid",
                    new { action = actionToken.ToString() });
            string pollAction =
                actionToken?.Value<string>()?.Trim() ?? string.Empty;
            if (string.Equals(
                    pollAction,
                    "status",
                    StringComparison.OrdinalIgnoreCase))
                return Status(parameters, action);
            if (!string.IsNullOrEmpty(pollAction) &&
                !string.Equals(
                    pollAction,
                    "start",
                    StringComparison.OrdinalIgnoreCase))
            {
                return new ErrorResponse(
                    "poll_action_unsupported",
                    new { action = pollAction, supported = new[] { "start", "status" } });
            }
            if (parameters["job_id"] != null)
            {
                return new ErrorResponse(
                    "job_id_not_allowed_for_start",
                    new { action = action.ToString() });
            }
            if (!TryGetString(
                    parameters,
                    "domain",
                    out string domain) ||
                !AgentAuthoringSchema.IsDomain(domain))
            {
                return new ErrorResponse(
                    "domain_required",
                    new { domain });
            }
            if (!TryGetString(
                    parameters,
                    "root_asset_path",
                    out string rootAssetPath))
            {
                return new ErrorResponse(
                    "root_asset_path_required",
                    new { domain });
            }

            var businessParameters =
                (JObject)parameters.DeepClone();
            businessParameters.Remove("action");
            businessParameters.Remove("job_id");
            Job job;
            lock (JobsLock)
            {
                Job active = FindActive(rootAssetPath);
                if (active != null)
                {
                    if (active.Action == action &&
                        string.Equals(
                            active.Domain,
                            domain,
                            StringComparison.Ordinal) &&
                        JToken.DeepEquals(
                            active.Parameters,
                            businessParameters))
                        return Pending(active, "BTSMTL Agent Document job is already running.");
                    return new ErrorResponse(
                        "authoring_job_busy",
                        new
                        {
                            job_id = active.Id,
                            action = active.Action.ToString(),
                            domain = active.Domain,
                            root_asset_path = active.RootAssetPath,
                            started = active.Started,
                            completed = active.Completed
                        });
                }

                string jobId = Guid.NewGuid().ToString("N");
                job = new Job
                {
                    Id = jobId,
                    Action = action,
                    Domain = domain,
                    RootAssetPath = rootAssetPath,
                    Parameters = businessParameters,
                    RequiresHash = requiresHash,
                    RequiresConfirmation = requiresConfirmation
                };
                Jobs.Add(jobId, job);
            }
            void RunOnce()
            {
                EditorApplication.update -= RunOnce;
                Execute(job.Id);
            }
            EditorApplication.update += RunOnce;
            return Pending(job, "BTSMTL Agent Document job scheduled.");
        }

        static object Status(
            JObject parameters,
            AgentAuthoringAction toolAction)
        {
            if (!TryGetString(
                    parameters,
                    "job_id",
                    out string jobId))
            {
                return new ErrorResponse(
                    "job_id_required",
                    new { action = "status" });
            }
            lock (JobsLock)
            {
                if (!Jobs.TryGetValue(jobId, out Job job))
                {
                    return new ErrorResponse(
                        "job_lost",
                        new
                        {
                            job_id = jobId,
                            action = toolAction.ToString(),
                            remediation =
                                "The Unity domain reloaded or the job identity is no longer available. Start a new explicit job; the lost job is never replayed automatically."
                        });
                }
                if (job.Action != toolAction)
                {
                    return new ErrorResponse(
                        "job_action_mismatch",
                        new
                        {
                            job_id = jobId,
                            expected_action = toolAction.ToString(),
                            actual_action = job.Action.ToString()
                        });
                }
                if (!job.Completed)
                    return Pending(job, "BTSMTL Agent Document job is running.");
                return job.FinalResponse ?? new ErrorResponse(
                    "job_response_missing",
                    new
                    {
                        job_id = job.Id,
                        action = job.Action.ToString()
                    });
            }
        }

        static void Execute(string jobId)
        {
            Job job;
            lock (JobsLock)
            {
                if (!Jobs.TryGetValue(jobId, out job) ||
                    job.Started ||
                    job.Completed)
                    return;
                job.Started = true;
            }
            object finalResponse = null;
            try
            {
                finalResponse =
                    BtsmtlAgentAuthoringMcpBridge.Execute(
                        job.Parameters,
                        job.Action,
                        job.RequiresHash,
                        job.RequiresConfirmation);
            }
            catch (Exception exception)
            {
                finalResponse = new ErrorResponse(
                    "authoring_job_exception",
                    new
                    {
                        job_id = job.Id,
                        action = job.Action.ToString(),
                        domain = job.Domain,
                        root_asset_path = job.RootAssetPath,
                        error = exception.Message
                    });
            }
            finally
            {
                lock (JobsLock)
                {
                    job.FinalResponse = finalResponse;
                    job.Completed = true;
                }
            }
        }

        static Job FindActive(string rootAssetPath)
        {
            foreach (Job job in Jobs.Values)
            {
                if (!job.Completed &&
                    string.Equals(
                        job.RootAssetPath,
                        rootAssetPath,
                        StringComparison.Ordinal))
                    return job;
            }
            return null;
        }

        static PendingResponse Pending(Job job, string message)
        {
            return new PendingResponse(
                message,
                1.0,
                new
                {
                    job_id = job.Id,
                    action = job.Action.ToString(),
                    domain = job.Domain,
                    root_asset_path = job.RootAssetPath,
                    started = job.Started,
                    completed = job.Completed
                });
        }

        static bool TryGetString(
            JObject parameters,
            string key,
            out string value)
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
