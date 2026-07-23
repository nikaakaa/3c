using System;
using System.Collections.Generic;
using MCPForUnity.Editor.Helpers;
using MCPForUnity.Editor.Tools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ThirdPersonCharacter.Pipeline.Graph;
using UnityEditor;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring.Mcp
{
    [McpForUnityTool(
        "manage_character_animation_presentation",
        Description = "Inspect, atomically migrate, configure producer source bindings or a locomotion Blend Space, and explicitly rebuild Float32 and Fixed targets for one Character Animation Presentation boundary.",
        RequiresPolling = true,
        PollAction = "status",
        MaxPollSeconds = 1800)]
    public static class ManageCharacterAnimationPresentationMcpTool
    {
        sealed class BuildJob
        {
            public string id;
            public string state;
            public object result;
            public Exception error;
        }

        static readonly Dictionary<string, BuildJob> s_BuildJobs = new Dictionary<string, BuildJob>(StringComparer.Ordinal);
        static string s_LatestBuildJobId = string.Empty;

        public sealed class Parameters
        {
            [ToolParameter("Action: inspect, apply_migration, rebuild_targets, inspect_blend_space, apply_blend_space, build_blend_space, inspect_producer_bindings, apply_producer_bindings, build_producer_bindings, or status.")]
            public string action { get; set; }

            [ToolParameter("Exact Assets/... path to the CharacterPipelineDefinition asset.", Required = false)]
            public string root_asset_path { get; set; }

            [ToolParameter("Character Animation Presentation migration request JSON.", Required = false)]
            public string migration_json { get; set; }

            [ToolParameter("Build job identity returned by build_blend_space.", Required = false)]
            public string job_id { get; set; }
        }

        public static object HandleCommand(JObject @params)
        {
            if (@params == null)
                return new ErrorResponse("request_missing");
            var parameters = new ToolParams(@params);
            string action = parameters.Get("action");
            if (string.IsNullOrWhiteSpace(action))
                return new ErrorResponse("action_required");
            if (action == "status")
                return GetBuildStatus(parameters.Get("job_id"));
            string rootPath = parameters.Get("root_asset_path");
            string migrationJson = parameters.Get("migration_json");
            if (string.IsNullOrWhiteSpace(rootPath))
                return new ErrorResponse("root_asset_path_required");
            if (string.IsNullOrWhiteSpace(migrationJson))
                return new ErrorResponse("migration_json_required");
            CharacterPipelineDefinition definition = AssetDatabase.LoadAssetAtPath<CharacterPipelineDefinition>(rootPath);
            if (!definition)
                return new ErrorResponse("root_asset_not_found", new { rootPath });
            try
            {
                if (action == "inspect_producer_bindings" || action == "apply_producer_bindings" || action == "build_producer_bindings")
                {
                    CharacterAnimationProducerBindingRequest bindingRequest =
                        JsonConvert.DeserializeObject<CharacterAnimationProducerBindingRequest>(migrationJson);
                    return action switch
                    {
                        "inspect_producer_bindings" => new SuccessResponse(
                            "Character Animation producer bindings inspected.",
                            CharacterAnimationProducerBindingAuthoringService.Inspect(definition, bindingRequest)),
                        "apply_producer_bindings" => new SuccessResponse(
                            "Character Animation producer bindings applied.",
                            CharacterAnimationProducerBindingAuthoringService.Apply(definition, bindingRequest)),
                        _ => ScheduleBuild(definition, bindingRequest)
                    };
                }
                if (action == "inspect_blend_space" || action == "apply_blend_space" || action == "build_blend_space")
                {
                    CharacterAnimationBlendSpaceMigrationRequest blendSpaceRequest =
                        JsonConvert.DeserializeObject<CharacterAnimationBlendSpaceMigrationRequest>(migrationJson);
                    return action switch
                    {
                        "inspect_blend_space" => new SuccessResponse(
                            "Character Animation Blend Space migration inspected.",
                            CharacterAnimationBlendSpaceMigrationAuthoringService.Inspect(definition, blendSpaceRequest)),
                        "apply_blend_space" => new SuccessResponse(
                            "Character Animation Blend Space migration applied.",
                            CharacterAnimationBlendSpaceMigrationAuthoringService.Apply(definition, blendSpaceRequest)),
                        _ => ScheduleBuild(definition, blendSpaceRequest)
                    };
                }
                CharacterAnimationPresentationMigrationRequest request =
                    JsonConvert.DeserializeObject<CharacterAnimationPresentationMigrationRequest>(migrationJson);
                return action switch
                {
                    "inspect" => new SuccessResponse(
                        "Character Animation Presentation migration inspected.",
                        CharacterAnimationPresentationMigrationAuthoringService.Inspect(definition, request)),
                    "apply_migration" => new SuccessResponse(
                        "Character Animation Presentation migration applied.",
                        CharacterAnimationPresentationMigrationAuthoringService.Apply(definition, request)),
                    "rebuild_targets" => new SuccessResponse(
                        "Character Animation Presentation targets rebuilt.",
                        CharacterAnimationPresentationMigrationAuthoringService.RebuildTargets(definition, request)),
                    _ => new ErrorResponse("unsupported_action", new { action })
                };
            }
            catch (JsonException exception)
            {
                return new ErrorResponse("migration_json_invalid", new { exception.Message });
            }
            catch (System.Exception exception)
            {
                return new ErrorResponse("animation_presentation_migration_failed", new { exception.Message, exception.StackTrace });
            }
        }

        static object ScheduleBuild(
            CharacterPipelineDefinition definition,
            CharacterAnimationBlendSpaceMigrationRequest request)
        {
            if (!string.IsNullOrEmpty(s_LatestBuildJobId) &&
                s_BuildJobs.TryGetValue(s_LatestBuildJobId, out BuildJob active) &&
                (active.state == "pending" || active.state == "running"))
                return new ErrorResponse("animation_presentation_build_already_running", new { job_id = active.id });
            var job = new BuildJob
            {
                id = Guid.NewGuid().ToString("N"),
                state = "pending"
            };
            s_BuildJobs[job.id] = job;
            s_LatestBuildJobId = job.id;
            bool started = false;
            void StartBuild()
            {
                if (started)
                    return;
                started = true;
                EditorApplication.update -= StartBuild;
                RunBuild(job, definition, request);
            }
            EditorApplication.update += StartBuild;
            return new PendingResponse(
                "Character Animation Blend Space target build pending.",
                3.0,
                new { job_id = job.id, state = job.state });
        }

        static void RunBuild(
            BuildJob job,
            CharacterPipelineDefinition definition,
            CharacterAnimationBlendSpaceMigrationRequest request)
        {
            job.state = "running";
            try
            {
                job.result = CharacterAnimationBlendSpaceMigrationAuthoringService.Build(definition, request);
                job.state = "succeeded";
            }
            catch (Exception exception)
            {
                job.error = exception;
                job.state = "failed";
            }
        }

        static object ScheduleBuild(
            CharacterPipelineDefinition definition,
            CharacterAnimationProducerBindingRequest request)
        {
            if (!string.IsNullOrEmpty(s_LatestBuildJobId) &&
                s_BuildJobs.TryGetValue(s_LatestBuildJobId, out BuildJob active) &&
                (active.state == "pending" || active.state == "running"))
                return new ErrorResponse("animation_presentation_build_already_running", new { job_id = active.id });
            var job = new BuildJob
            {
                id = Guid.NewGuid().ToString("N"),
                state = "pending"
            };
            s_BuildJobs[job.id] = job;
            s_LatestBuildJobId = job.id;
            bool started = false;
            void StartBuild()
            {
                if (started)
                    return;
                started = true;
                EditorApplication.update -= StartBuild;
                RunBuild(job, definition, request);
            }
            EditorApplication.update += StartBuild;
            return new PendingResponse(
                "Character Animation producer binding target build pending.",
                3.0,
                new { job_id = job.id, state = job.state });
        }

        static void RunBuild(
            BuildJob job,
            CharacterPipelineDefinition definition,
            CharacterAnimationProducerBindingRequest request)
        {
            job.state = "running";
            try
            {
                job.result = CharacterAnimationProducerBindingAuthoringService.Build(definition, request);
                job.state = "succeeded";
            }
            catch (Exception exception)
            {
                job.error = exception;
                job.state = "failed";
            }
        }

        static object GetBuildStatus(string jobId)
        {
            string resolvedId = string.IsNullOrWhiteSpace(jobId) ? s_LatestBuildJobId : jobId;
            if (string.IsNullOrWhiteSpace(resolvedId) || !s_BuildJobs.TryGetValue(resolvedId, out BuildJob job))
                return new ErrorResponse("animation_presentation_build_job_not_found", new { job_id = resolvedId });
            if (job.state == "pending" || job.state == "running")
            {
                return new PendingResponse(
                    $"Character Animation Presentation target build {job.state}.",
                    3.0,
                    new { job_id = job.id, state = job.state });
            }
            if (job.error != null)
            {
                return new ErrorResponse(
                    "animation_presentation_migration_failed",
                    new { job_id = job.id, state = job.state, job.error.Message, job.error.StackTrace });
            }
            return new SuccessResponse(
                "Character Animation Presentation targets built.",
                new { job_id = job.id, state = job.state, result = job.result });
        }
    }
}
