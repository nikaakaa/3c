using System;
using System.Linq;
using MCPForUnity.Editor.Helpers;
using MCPForUnity.Editor.Tools;
using Newtonsoft.Json.Linq;
using ThirdPersonCharacter.Pipeline;
using UnityEditor;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    [McpForUnityTool(
        "character.configure_animation_resources",
        Description = "Analyze or apply the formal Animation Resource configuration for one exact Character Pipeline Definition. Analyze is read-only; apply requires the exact plan hash and updates the Profile, Pose Graph, formal Clip curves and matching Rig Binding Prefabs in one transaction.",
        StructuredOutput = true,
        AutoRegister = true,
        RequiresPolling = true,
        BackgroundPollingStatus = true,
        PollAction = "status",
        MaxPollSeconds = 600,
        HasBehaviorAnnotations = true,
        ReadOnlyHint = false,
        DestructiveHint = true,
        IdempotentHint = false,
        OpenWorldHint = false)]
    public static class CharacterAnimationResourceConfigurationMcpTool
    {
        public sealed class Parameters
        {
            [ToolParameter("Polling action: start or status.", Required = true)]
            public string action { get; set; }

            [ToolParameter("Operation for start: analyze or apply.", Required = false)]
            public string operation { get; set; }

            [ToolParameter("Stable job identity returned by start; required for status.", Required = false)]
            public string job_id { get; set; }

            [ToolParameter("Exact Assets/... path to one CharacterPipelineDefinition asset.", Required = true)]
            public string definition_asset_path { get; set; }

            [ToolParameter("Exact formal Renderer Binding Id.", Required = true)]
            public string renderer_binding_id { get; set; }

            [ToolParameter("Exact Animation curve Transform path below the formal Animator.", Required = true)]
            public string animation_curve_path { get; set; }

            [ToolParameter("Exact plan hash returned by analyze. Required for apply.", Required = false)]
            public string expected_plan_hash { get; set; }
        }

        public static object HandleCommand(JObject @params)
            => CharacterAnimationResourceConfigurationMcpJobScheduler.Handle(@params);

        internal static CharacterPipelineDefinition LoadDefinition(string path)
        {
            string normalized = (path ?? string.Empty).Trim().Replace('\\', '/');
            if (!normalized.StartsWith("Assets/", StringComparison.Ordinal) ||
                !normalized.EndsWith(".asset", StringComparison.OrdinalIgnoreCase) ||
                normalized.Contains(".."))
            {
                throw new InvalidOperationException(
                    $"Character Pipeline Definition path '{normalized}' is invalid.");
            }
            CharacterPipelineDefinition definition =
                AssetDatabase.LoadAssetAtPath<CharacterPipelineDefinition>(normalized);
            if (!definition || AssetDatabase.LoadMainAssetAtPath(normalized) != definition)
                throw new InvalidOperationException(
                    $"Character Pipeline Definition '{normalized}' is unavailable.");
            return definition;
        }

        internal static object CreateSuccess(
            CharacterAnimationPropertyImportPlan plan,
            bool applied,
            string jobId)
        {
            return new
            {
                success = true,
                message = applied
                    ? "Formal Animation Resource configuration was applied and saved."
                    : "Formal Animation Resource configuration plan is ready.",
                data = new
                {
                    job_id = jobId,
                    applied,
                    definition_asset_path = plan.DefinitionAssetPath,
                    profile_asset_path = plan.ProfileAssetPath,
                    pose_graph_asset_path = plan.PoseGraphAssetPath,
                    renderer_binding_id = plan.RendererBindingId,
                    animation_curve_path = plan.AnimationCurvePath,
                    plan_hash = plan.PlanHash,
                    mesh_identity = new
                    {
                        guid = plan.MeshGuid,
                        local_file_id = plan.MeshLocalFileId,
                        content_hash = plan.MeshContentHash
                    },
                    source_closure = plan.SourceClosure.Select(value => new
                    {
                        asset_path = value.AssetPath,
                        asset_guid = value.AssetGuid,
                        local_file_id = value.LocalFileId,
                        dependency_hash = value.DependencyHash,
                        source_category = value.SourceCategory,
                        current_backend = value.CurrentBackendName,
                        target_backend = value.TargetBackend,
                        blendshape_curve_hash = value.BlendShapeCurveHash,
                        linearization_count = value.LinearizationCount,
                        root_evidence_count = value.RootEvidenceCount,
                        root_evidence_bindings = value.RootEvidenceBindings.ToArray(),
                        root_evidence_hash = value.RootEvidenceHash
                    }).ToArray(),
                    curve_set = plan.CurveSet.Select(value => new
                    {
                        source_blendshape_name = value.SourceBlendShapeName,
                        target_blendshape_name = value.TargetBlendShapeName,
                        source_property_name = value.SourcePropertyName,
                        target_property_name = value.TargetPropertyName,
                        blendshape_index = value.BlendShapeIndex,
                        parameter_id = value.ParameterId,
                        requires_rename = value.RequiresRename
                    }).ToArray(),
                    pose_graphs = plan.PoseGraphs.Select(value => new
                    {
                        graph_id = value.GraphId,
                        is_root = value.IsRoot,
                        current_parameter_count = value.CurrentParameterCount,
                        target_parameter_count = value.TargetParameterCount,
                        current_policy_count = value.CurrentPolicyCount,
                        target_policy_count = value.TargetPolicyCount
                    }).ToArray(),
                    root_parameter_policies = plan.RootPolicies.Select(value => new
                    {
                        parameter_id = value.ParameterId.Value,
                        policy = value.Policy.ToString()
                    }).ToArray(),
                    compression = new
                    {
                        revision = plan.Compression.Revision,
                        sample_rate = plan.Compression.SampleRate,
                        transform_precision = plan.Compression.TransformPrecision,
                        scale_precision = plan.Compression.ScalePrecision,
                        rotation_precision_degrees = plan.Compression.RotationPrecisionDegrees,
                        scalar_precision = plan.Compression.ScalarPrecision,
                        shell_distance = plan.Compression.ShellDistance,
                        optimize_loops = plan.Compression.OptimizeLoops,
                        enable_database = plan.Compression.EnableDatabase,
                        enable_medium_tier = plan.Compression.EnableMediumTier,
                        enable_low_tier = plan.Compression.EnableLowTier,
                        enable_per_track_rounding = plan.Compression.EnablePerTrackRounding,
                        medium_importance_tier_proportion = plan.Compression.MediumImportanceTierProportion,
                        low_importance_tier_proportion = plan.Compression.LowImportanceTierProportion,
                        max_database_chunk_size = plan.Compression.MaxDatabaseChunkSize,
                        compiler_options = plan.Compression.CompilerOptions
                    },
                    prefab_targets = plan.PrefabTargets.Select(value => new
                    {
                        asset_path = value.AssetPath,
                        rig_id = value.RigId,
                        rig_revision = value.RigRevision,
                        binding_id = value.BindingId,
                        renderer_path = value.RendererPath,
                        current_binding_id = value.CurrentBindingId,
                        current_renderer_path = value.CurrentRendererPath,
                        current_mesh_guid = value.CurrentMeshGuid,
                        current_mesh_local_file_id = value.CurrentMeshLocalFileId,
                        current_mesh_content_hash = value.CurrentMeshContentHash,
                        mesh_guid = value.MeshGuid,
                        mesh_local_file_id = value.MeshLocalFileId,
                        mesh_content_hash = value.MeshContentHash
                    }).ToArray(),
                    diffs = plan.Diffs.Select(value => new
                    {
                        kind = value.Kind.ToString(),
                        target = value.Target,
                        before = value.Before,
                        after = value.After,
                        detail = value.Detail
                    }).ToArray()
                }
            };
        }
    }
}
