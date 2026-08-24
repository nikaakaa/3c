using System;
using System.Linq;
using MCPForUnity.Editor.Helpers;
using MCPForUnity.Editor.Tools;
using Newtonsoft.Json.Linq;
using ThirdPersonCharacter.Pipeline.Simulation.Editor;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    [McpForUnityTool(
        "character.foot_motion_bake",
        Description = "Analyze or apply one exact Foot Motion Target AnimationClip through the formal Bake Session. Resolves Motion Reference from the exact Analysis Source, returns a 22-channel diff and requires the exact plan hash plus explicit replace_existing to overwrite different or partial author Curves.",
        StructuredOutput = true,
        AutoRegister = true,
        RequiresPolling = false,
        HasBehaviorAnnotations = true,
        ReadOnlyHint = false,
        DestructiveHint = true,
        IdempotentHint = false,
        OpenWorldHint = false)]
    public static class CharacterFootMotionBakeMcpTool
    {
        public sealed class Parameters
        {
            [ToolParameter("Action: analyze or apply.", Required = true)]
            public string action { get; set; }

            [ToolParameter("Exact Assets/... path to one CharacterFootPlacementAnalysisSource asset.", Required = true)]
            public string analysis_source_asset_path { get; set; }

            [ToolParameter("Exact Assets/... path to one native Target AnimationClip.", Required = true)]
            public string target_clip_asset_path { get; set; }

            [ToolParameter("Exact plan hash returned by analyze. Required for apply.", Required = false)]
            public string expected_plan_hash { get; set; }

            [ToolParameter("Explicit true confirmation required to replace Different or Partial existing Curves.", Required = false)]
            public bool replace_existing { get; set; }
        }

        public static object HandleCommand(JObject @params)
        {
            string action = @params?["action"]?.Value<string>() ?? string.Empty;
            string sourcePath = @params?["analysis_source_asset_path"]?.Value<string>() ?? string.Empty;
            string targetPath = @params?["target_clip_asset_path"]?.Value<string>() ?? string.Empty;
            string expectedPlanHash = @params?["expected_plan_hash"]?.Value<string>() ?? string.Empty;
            bool replaceExisting = @params?["replace_existing"]?.Value<bool>() ?? false;
            try
            {
                CharacterFootPlacementAnalysisSource source =
                    LoadExact<CharacterFootPlacementAnalysisSource>(sourcePath, ".asset", "Analysis Source");
                AnimationClip target = LoadExact<AnimationClip>(targetPath, ".anim", "Target AnimationClip");
                switch (action.Trim().ToLowerInvariant())
                {
                    case "analyze":
                    {
                        CharacterFootMotionBakePlan plan = CharacterFootMotionBakeService.Analyze(source, target);
                        return Success(plan, false);
                    }
                    case "apply":
                    {
                        if (string.IsNullOrWhiteSpace(expectedPlanHash))
                            return new ErrorResponse("expected_plan_hash_required", new { target_clip_asset_path = targetPath });
                        CharacterFootMotionBakePlan plan =
                            CharacterFootMotionBakeService.BuildPlanFromReadyArtifact(source, target);
                        CharacterFootMotionBakeApplyResult result = CharacterFootMotionBakeService.Apply(
                            plan,
                            expectedPlanHash,
                            replaceExisting);
                        return Success(result.Plan, result.Applied);
                    }
                    default:
                        return new ErrorResponse("invalid_action", new { action });
                }
            }
            catch (Exception exception)
            {
                return new ErrorResponse(
                    "foot_motion_bake_failed",
                    new
                    {
                        action,
                        analysis_source_asset_path = sourcePath,
                        target_clip_asset_path = targetPath,
                        message = exception.Message
                    });
            }
        }

        static object Success(CharacterFootMotionBakePlan plan, bool applied) =>
            new
            {
                success = true,
                message = applied
                    ? "Foot Motion Curves were applied and verified."
                    : "Foot Motion Bake plan is ready.",
                data = new
                {
                    source_asset_path = plan.SourceAssetPath,
                    target_asset_path = plan.TargetAssetPath,
                    motion_reference_asset_path = plan.MotionReferenceAssetPath,
                    state = plan.State.ToString(),
                    plan_hash = plan.PlanHash,
                    registered_curve_hash = plan.RegisteredCurveHash,
                    artifact_identity_hash = plan.ArtifactIdentityHash.Value,
                    artifact_content_hash = plan.ArtifactContentHash.Value,
                    algorithm_version = CharacterFootPlacementAnalysisSource.AlgorithmVersion,
                    requires_replace = plan.RequiresReplace,
                    no_change = plan.IsNoChange,
                    applied,
                    metrics = Metrics(plan),
                    changed_channels = plan.ChangedChannels.Select(value => new
                    {
                        channel_id = value.ChannelId,
                        property_name = value.PropertyName,
                        kind = value.Kind.ToString()
                    }).ToArray()
                }
            };

        static object Metrics(CharacterFootMotionBakePlan plan)
        {
            AnimationFootAnalysisArtifactInspection inspection =
                AnimationFootAnalysisArtifactBuilder.Inspect(plan.TargetClip, plan.Source);
            if (inspection.Status != AnimationFootAnalysisArtifactStatus.Ready || inspection.Artifact == null)
                return null;
            AnimationFootMotionDataDescriptor data = inspection.Artifact.MotionData;
            int sampleCount = data.Raw.RootSamples.Count;
            int activeCount = plan.TargetClip.isLooping ? sampleCount - 1 : sampleCount;
            int bothContactFalse = 0;
            int zeroSupport = 0;
            float maximumSupportSum = 0f;
            float maximumSupportWhenBothContactFalse = 0f;
            for (int i = 0; i < activeCount; i++)
            {
                AnimationFootMotionDerivedSample left = data.Left.Samples[i];
                AnimationFootMotionDerivedSample right = data.Right.Samples[i];
                float supportSum = left.Constraint.Support + right.Constraint.Support;
                maximumSupportSum = Mathf.Max(maximumSupportSum, supportSum);
                if (supportSum <= 0.0001f)
                    zeroSupport++;
                if (left.Filter.Contact < 0.5f && right.Filter.Contact < 0.5f)
                {
                    bothContactFalse++;
                    maximumSupportWhenBothContactFalse = Mathf.Max(
                        maximumSupportWhenBothContactFalse,
                        supportSum);
                }
            }
            return new
            {
                sample_count = sampleCount,
                active_sample_count = activeCount,
                both_contact_false_count = bothContactFalse,
                zero_support_count = zeroSupport,
                maximum_support_sum = maximumSupportSum,
                maximum_support_when_both_contact_false = maximumSupportWhenBothContactFalse,
                left = FootMetrics(data.Left, activeCount),
                right = FootMetrics(data.Right, activeCount)
            };
        }

        static object FootMetrics(AnimationFootMotionFootPage foot, int activeCount)
        {
            int landing = 0;
            int liftOff = 0;
            int contact = 0;
            int locked = 0;
            float maximumStepTime = 0f;
            float maximumStepDistance = 0f;
            float maximumFootHeight = 0f;
            float minimumToeHeight = float.PositiveInfinity;
            float maximumToeHeight = float.NegativeInfinity;
            float maximumToeSpeed = 0f;
            float minimumPositionError = float.PositiveInfinity;
            float maximumPositionError = 0f;
            float minimumRotationError = float.PositiveInfinity;
            float maximumRotationError = 0f;
            float maximumLockWeight = 0f;
            float maximumSupportCandidate = 0f;
            float maximumSupport = 0f;
            for (int i = 0; i < foot.Events.Count; i++)
            {
                if (foot.Events[i].Kind == AnimationFootMotionEventKind.Landing)
                    landing++;
                else
                    liftOff++;
            }
            for (int i = 0; i < activeCount; i++)
            {
                AnimationFootMotionDerivedSample sample = foot.Samples[i];
                if (sample.Filter.Contact >= 0.5f)
                    contact++;
                if (sample.Constraint.LockMode == AnimationFootLockMode.Locked)
                    locked++;
                maximumStepTime = Mathf.Max(maximumStepTime, sample.Step.TimeSeconds);
                maximumStepDistance = Mathf.Max(maximumStepDistance, sample.Step.Distance);
                maximumFootHeight = Mathf.Max(maximumFootHeight, sample.Step.HeightAbovePath);
                minimumToeHeight = Mathf.Min(minimumToeHeight, sample.Filter.ToeHeight);
                maximumToeHeight = Mathf.Max(maximumToeHeight, sample.Filter.ToeHeight);
                maximumToeSpeed = Mathf.Max(maximumToeSpeed, sample.Filter.ToeSpeed);
                minimumPositionError = Mathf.Min(minimumPositionError, sample.Filter.PositionError);
                maximumPositionError = Mathf.Max(maximumPositionError, sample.Filter.PositionError);
                minimumRotationError = Mathf.Min(minimumRotationError, sample.Filter.RotationError);
                maximumRotationError = Mathf.Max(maximumRotationError, sample.Filter.RotationError);
                maximumLockWeight = Mathf.Max(maximumLockWeight, sample.Constraint.LockWeight);
                maximumSupportCandidate = Mathf.Max(
                    maximumSupportCandidate,
                    sample.Constraint.SupportCandidate);
                maximumSupport = Mathf.Max(maximumSupport, sample.Constraint.Support);
            }
            return new
            {
                landing_count = landing,
                lift_off_count = liftOff,
                contact_sample_count = contact,
                locked_sample_count = locked,
                maximum_step_time = maximumStepTime,
                maximum_step_distance = maximumStepDistance,
                maximum_foot_height = maximumFootHeight,
                minimum_toe_height = minimumToeHeight,
                maximum_toe_height = maximumToeHeight,
                maximum_toe_speed = maximumToeSpeed,
                minimum_position_error = minimumPositionError,
                maximum_position_error = maximumPositionError,
                minimum_rotation_error = minimumRotationError,
                maximum_rotation_error = maximumRotationError,
                maximum_lock_weight = maximumLockWeight,
                maximum_support_candidate = maximumSupportCandidate,
                maximum_support = maximumSupport
            };
        }

        static T LoadExact<T>(string path, string extension, string role)
            where T : UnityEngine.Object
        {
            if (string.IsNullOrWhiteSpace(path) ||
                !path.StartsWith("Assets/", StringComparison.Ordinal) ||
                !path.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Foot Motion Bake {role} path is invalid: '{path}'.");
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (!asset || AssetDatabase.LoadMainAssetAtPath(path) != asset)
                throw new InvalidOperationException($"Foot Motion Bake {role} does not resolve to one exact main asset: '{path}'.");
            return asset;
        }
    }
}
