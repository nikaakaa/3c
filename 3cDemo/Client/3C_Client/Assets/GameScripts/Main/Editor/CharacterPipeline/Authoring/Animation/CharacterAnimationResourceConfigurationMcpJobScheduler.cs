using System;
using System.Collections.Generic;
using System.Linq;
using MCPForUnity.Editor.Helpers;
using Newtonsoft.Json.Linq;
using ThirdPersonCharacter.Pipeline;
using UnityEditor;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    static class CharacterAnimationResourceConfigurationMcpJobScheduler
    {
        sealed class Job
        {
            public string Id;
            public string Operation;
            public string DefinitionAssetPath;
            public string RendererBindingId;
            public string AnimationCurvePath;
            public string ExpectedPlanHash;
            public JObject Parameters;
            public bool Started;
            public bool Completed;
            public object FinalResponse;
        }

        static readonly Dictionary<string, Job> Jobs =
            new Dictionary<string, Job>(StringComparer.Ordinal);
        static readonly object JobsLock = new object();

        internal static object Handle(JObject parameters)
        {
            if (parameters == null)
                return new ErrorResponse("request_missing");
            if (!TryNormalize(parameters, out JObject canonical, out ErrorResponse error))
                return error;
            string action = StringValue(canonical, "action");
            if (string.Equals(action, "status", StringComparison.OrdinalIgnoreCase))
            {
                if (canonical.Properties().Any(value =>
                        value.Name != "action" && value.Name != "job_id"))
                {
                    return new ErrorResponse(
                        "status_parameters_not_allowed",
                        new { allowed = new[] { "action", "job_id" } });
                }
                return Status(StringValue(canonical, "job_id"));
            }
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
                    new { action = "start" });
            }
            string operation = StringValue(canonical, "operation");
            if (!string.Equals(operation, "analyze", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(operation, "apply", StringComparison.OrdinalIgnoreCase))
            {
                return new ErrorResponse(
                    "operation_required",
                    new { operation, supported = new[] { "analyze", "apply" } });
            }
            if (!TryGetString(canonical, "definition_asset_path", out string definitionPath) ||
                !TryGetString(canonical, "renderer_binding_id", out string rendererBindingId) ||
                !TryGetString(canonical, "animation_curve_path", out string animationCurvePath))
            {
                return new ErrorResponse(
                    "animation_resource_configuration_parameters_required",
                    new
                    {
                        definition_asset_path_required = true,
                        renderer_binding_id_required = true,
                        animation_curve_path_required = true
                    });
            }
            string expectedPlanHash = StringValue(canonical, "expected_plan_hash");
            if (string.Equals(operation, "apply", StringComparison.OrdinalIgnoreCase) &&
                string.IsNullOrWhiteSpace(expectedPlanHash))
            {
                return new ErrorResponse(
                    "expected_plan_hash_required",
                    new { definition_asset_path = definitionPath });
            }
            var businessParameters = (JObject)canonical.DeepClone();
            businessParameters.Remove("action");
            businessParameters.Remove("job_id");
            Job job;
            lock (JobsLock)
            {
                Job active = Jobs.Values.FirstOrDefault(value => !value.Completed);
                if (active != null)
                {
                    if (string.Equals(active.Operation, operation, StringComparison.OrdinalIgnoreCase) &&
                        JToken.DeepEquals(active.Parameters, businessParameters))
                        return Pending(active, "Animation resource configuration job is already running.");
                    return new ErrorResponse(
                        "animation_resource_configuration_job_busy",
                        JobState(active));
                }
                job = new Job
                {
                    Id = Guid.NewGuid().ToString("N"),
                    Operation = operation.Trim().ToLowerInvariant(),
                    DefinitionAssetPath = definitionPath,
                    RendererBindingId = rendererBindingId,
                    AnimationCurvePath = animationCurvePath,
                    ExpectedPlanHash = expectedPlanHash,
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
            return Pending(job, "Animation resource configuration job scheduled.");
        }

        static object Status(string jobId)
        {
            if (string.IsNullOrWhiteSpace(jobId))
                return new ErrorResponse("job_id_required", new { action = "status" });
            lock (JobsLock)
            {
                if (!Jobs.TryGetValue(jobId.Trim(), out Job job))
                {
                    return new ErrorResponse(
                        "job_lost",
                        new
                        {
                            job_id = jobId,
                            remediation = "The Unity domain reloaded or the job identity is no longer available. Start a new explicit job; a lost job is never replayed automatically."
                        });
                }
                if (!job.Completed)
                    return Pending(job, "Animation resource configuration job is running.");
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
                if (!Jobs.TryGetValue(jobId, out job) || job.Started || job.Completed)
                    return;
                job.Started = true;
            }
            object response;
            try
            {
                if (job.Operation == "apply" &&
                    (EditorApplication.isCompiling ||
                     EditorApplication.isUpdating ||
                     EditorApplication.isPlayingOrWillChangePlaymode))
                {
                    response = new ErrorResponse(
                        "editor_busy",
                        new
                        {
                            job_id = job.Id,
                            is_compiling = EditorApplication.isCompiling,
                            is_updating = EditorApplication.isUpdating,
                            is_playing_or_will_change_playmode = EditorApplication.isPlayingOrWillChangePlaymode
                        });
                }
                else
                {
                    CharacterPipelineDefinition definition =
                        CharacterAnimationResourceConfigurationMcpTool.LoadDefinition(
                            job.DefinitionAssetPath);
                    CharacterAnimationPropertyImportPlan plan =
                        job.Operation == "analyze"
                            ? CharacterAnimationPropertyImporter.Analyze(
                                definition,
                                job.RendererBindingId,
                                job.AnimationCurvePath)
                            : CharacterAnimationPropertyImporter.Apply(
                                definition,
                                job.RendererBindingId,
                                job.AnimationCurvePath,
                                job.ExpectedPlanHash);
                    response = CharacterAnimationResourceConfigurationMcpTool.CreateSuccess(
                        plan,
                        job.Operation == "apply",
                        job.Id);
                }
            }
            catch (Exception exception)
            {
                response = new ErrorResponse(
                    "animation_resource_configuration_job_failed",
                    new
                    {
                        job_id = job.Id,
                        operation = job.Operation,
                        definition_asset_path = job.DefinitionAssetPath,
                        renderer_binding_id = job.RendererBindingId,
                        animation_curve_path = job.AnimationCurvePath,
                        message = exception.Message
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
            out JObject canonical,
            out ErrorResponse error)
        {
            canonical = new JObject();
            error = null;
            var allowed = new HashSet<string>(StringComparer.Ordinal)
            {
                "action",
                "operation",
                "job_id",
                "definition_asset_path",
                "renderer_binding_id",
                "animation_curve_path",
                "expected_plan_hash"
            };
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
            foreach (string key in new[] { "action", "operation", "job_id", "definition_asset_path", "renderer_binding_id", "animation_curve_path", "expected_plan_hash" })
            {
                JToken token = canonical[key];
                if (token != null && token.Type != JTokenType.String)
                {
                    error = new ErrorResponse(
                        "parameter_type_invalid",
                        new { parameter = key, expected = "string" });
                    return false;
                }
            }
            return true;
        }

        static PendingResponse Pending(Job job, string message) =>
            new PendingResponse(message, 1.0, JobState(job));

        static object JobState(Job job) => new
        {
            job_id = job.Id,
            operation = job.Operation,
            definition_asset_path = job.DefinitionAssetPath,
            renderer_binding_id = job.RendererBindingId,
            animation_curve_path = job.AnimationCurvePath,
            started = job.Started,
            completed = job.Completed
        };

        static string StringValue(JObject parameters, string key) =>
            parameters[key]?.Value<string>()?.Trim() ?? string.Empty;

        static bool TryGetString(JObject parameters, string key, out string value)
        {
            value = StringValue(parameters, key);
            return !string.IsNullOrWhiteSpace(value);
        }
    }
}
