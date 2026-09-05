using System;
using System.Collections.Generic;
using System.Linq;
using MCPForUnity.Editor.Helpers;
using Newtonsoft.Json.Linq;
using UnityEditor;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    enum CharacterSimulationBuildKind
    {
        Float32,
        Fixed
    }

    static class CharacterSimulationBuildMcpJobScheduler
    {
        sealed class Job
        {
            public string Id;
            public CharacterSimulationBuildKind Kind;
            public string DefinitionAssetPath;
            public string WrapperAssetPath;
            public JObject Parameters;
            public bool Started;
            public bool Completed;
            public object FinalResponse;
        }

        static readonly Dictionary<string, Job> Jobs =
            new Dictionary<string, Job>(StringComparer.Ordinal);
        static readonly object JobsLock = new object();

        public static object Handle(
            JObject parameters,
            CharacterSimulationBuildKind kind)
        {
            if (parameters == null)
                return new ErrorResponse("request_missing");
            if (!TryNormalize(
                    parameters,
                    kind,
                    out JObject canonical,
                    out ErrorResponse parameterError))
                return parameterError;

            JToken actionToken = canonical["action"];
            if (actionToken != null && actionToken.Type != JTokenType.String)
            {
                return new ErrorResponse(
                    "poll_action_invalid",
                    new { action = actionToken.ToString() });
            }

            string action = actionToken?.Value<string>()?.Trim() ?? string.Empty;
            if (string.Equals(action, "status", StringComparison.OrdinalIgnoreCase))
                return Status(canonical, kind);
            if (!string.IsNullOrEmpty(action) &&
                !string.Equals(action, "start", StringComparison.OrdinalIgnoreCase))
            {
                return new ErrorResponse(
                    "poll_action_unsupported",
                    new { action, supported = new[] { "start", "status" } });
            }
            if (canonical["job_id"] != null)
            {
                return new ErrorResponse(
                    "job_id_not_allowed_for_start",
                    new { build_kind = kind.ToString() });
            }
            if (!TryGetString(canonical, "definition_asset_path", out string definitionPath))
                return new ErrorResponse("definition_asset_path_required");

            string wrapperPath = string.Empty;
            if (kind == CharacterSimulationBuildKind.Fixed &&
                !TryGetString(canonical, "wrapper_asset_path", out wrapperPath))
            {
                return new ErrorResponse(
                    "wrapper_asset_path_required",
                    new { definitionAssetPath = definitionPath });
            }

            var businessParameters = (JObject)canonical.DeepClone();
            businessParameters.Remove("action");
            businessParameters.Remove("job_id");
            Job job;
            lock (JobsLock)
            {
                Job active = Jobs.Values.FirstOrDefault(candidate => !candidate.Completed);
                if (active != null)
                {
                    if (active.Kind == kind &&
                        JToken.DeepEquals(active.Parameters, businessParameters))
                        return Pending(active, "Character Build job is already running.");
                    return new ErrorResponse(
                        "character_build_job_busy",
                        JobState(active));
                }

                job = new Job
                {
                    Id = Guid.NewGuid().ToString("N"),
                    Kind = kind,
                    DefinitionAssetPath = definitionPath,
                    WrapperAssetPath = wrapperPath,
                    Parameters = businessParameters
                };
                Jobs.Add(job.Id, job);
            }

            void RunOnce()
            {
                EditorApplication.update -= RunOnce;
                Execute(job.Id);
            }

            EditorApplication.update += RunOnce;
            return Pending(job, "Character Build job scheduled.");
        }

        static object Status(
            JObject parameters,
            CharacterSimulationBuildKind toolKind)
        {
            if (!TryGetString(parameters, "job_id", out string jobId))
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
                            build_kind = toolKind.ToString(),
                            remediation = "The Unity domain reloaded or the job identity is no longer available. Start a new explicit build; the lost build is never replayed automatically."
                        });
                }
                if (job.Kind != toolKind)
                {
                    return new ErrorResponse(
                        "job_build_kind_mismatch",
                        new
                        {
                            job_id = jobId,
                            expected_build_kind = toolKind.ToString(),
                            actual_build_kind = job.Kind.ToString()
                        });
                }
                if (!job.Completed)
                    return Pending(job, "Character Build job is running.");
                return job.FinalResponse ?? new ErrorResponse(
                    "job_response_missing",
                    JobState(job));
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

            object response;
            try
            {
                response = job.Kind == CharacterSimulationBuildKind.Float32
                    ? CharacterSimulationBuildMcpBridge.BuildFloat32(job.Parameters)
                    : CharacterSimulationBuildMcpBridge.BuildFixed(job.Parameters);
            }
            catch (Exception exception)
            {
                response = new ErrorResponse(
                    "character_build_job_exception",
                    new
                    {
                        job_id = job.Id,
                        build_kind = job.Kind.ToString(),
                        definition_asset_path = job.DefinitionAssetPath,
                        wrapper_asset_path = job.WrapperAssetPath,
                        error = exception.Message
                    });
            }

            lock (JobsLock)
            {
                job.FinalResponse = response;
                job.Completed = true;
            }
        }

        static bool TryNormalize(
            JObject parameters,
            CharacterSimulationBuildKind kind,
            out JObject canonical,
            out ErrorResponse error)
        {
            canonical = new JObject();
            error = null;
            var allowed = new HashSet<string>(StringComparer.Ordinal)
            {
                "action",
                "job_id",
                "definition_asset_path"
            };
            if (kind == CharacterSimulationBuildKind.Fixed)
                allowed.Add("wrapper_asset_path");

            foreach (JProperty property in parameters.Properties())
            {
                if (!allowed.Contains(property.Name))
                {
                    error = new ErrorResponse(
                        "unknown_parameter",
                        new { parameter = property.Name });
                    return false;
                }
                if (canonical.Property(property.Name) != null)
                {
                    error = new ErrorResponse(
                        "duplicate_parameter",
                        new { parameter = property.Name });
                    return false;
                }
                canonical.Add(property.Name, property.Value.DeepClone());
            }
            return true;
        }

        static PendingResponse Pending(Job job, string message)
        {
            return new PendingResponse(
                message,
                1.0,
                JobState(job));
        }

        static object JobState(Job job)
        {
            return new
            {
                job_id = job.Id,
                build_kind = job.Kind.ToString(),
                definition_asset_path = job.DefinitionAssetPath,
                wrapper_asset_path = job.WrapperAssetPath,
                started = job.Started,
                completed = job.Completed
            };
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
