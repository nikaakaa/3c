using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Timeline;
using MCPForUnity.Editor.Helpers;
using MCPForUnity.Editor.Tools;
using Newtonsoft.Json.Linq;
using ThirdPersonCharacter.Pipeline.Simulation.Fixed;
using ThirdPersonSimulation;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    [McpForUnityTool("character.build_float32_products", Description = "Build and publish the Float32 Program wrapper and Presentation Projection for one exact CharacterPipelineDefinition path.", StructuredOutput = true, RequiresPolling = true, BackgroundPollingStatus = true, PollAction = "status", MaxPollSeconds = 600, HasBehaviorAnnotations = true, ReadOnlyHint = false, DestructiveHint = true, IdempotentHint = false, OpenWorldHint = false)]
    public static class BuildCharacterFloat32ProductsMcpTool
    {
        public sealed class Parameters
        {
            [ToolParameter("Omit or use start to create a job; use status to poll one job.", Required = false)]
            public string action { get; set; }

            [ToolParameter("Stable job identity returned by the initial call; required for status.", Required = false)]
            public string job_id { get; set; }

            [ToolParameter("Exact Assets/... path to one CharacterPipelineDefinition asset required for start.", Required = false)]
            public string definition_asset_path { get; set; }
        }

        public static object HandleCommand(JObject @params)
        {
            return CharacterSimulationBuildMcpJobScheduler.Handle(
                @params,
                CharacterSimulationBuildKind.Float32);
        }
    }

    [McpForUnityTool("character.build_fixed_products", Description = "Build and publish the Fixed Program wrapper and Presentation Projection for one exact CharacterPipelineDefinition and one exact wrapper destination.", StructuredOutput = true, RequiresPolling = true, BackgroundPollingStatus = true, PollAction = "status", MaxPollSeconds = 600, HasBehaviorAnnotations = true, ReadOnlyHint = false, DestructiveHint = true, IdempotentHint = false, OpenWorldHint = false)]
    public static class BuildCharacterFixedProductsMcpTool
    {
        public sealed class Parameters
        {
            [ToolParameter("Omit or use start to create a job; use status to poll one job.", Required = false)]
            public string action { get; set; }

            [ToolParameter("Stable job identity returned by the initial call; required for status.", Required = false)]
            public string job_id { get; set; }

            [ToolParameter("Exact Assets/... path to one CharacterPipelineDefinition asset required for start.", Required = false)]
            public string definition_asset_path { get; set; }

            [ToolParameter("Exact Assets/... .asset destination for the Fixed Program wrapper required for start.", Required = false)]
            public string wrapper_asset_path { get; set; }
        }

        public static object HandleCommand(JObject @params)
        {
            return CharacterSimulationBuildMcpJobScheduler.Handle(
                @params,
                CharacterSimulationBuildKind.Fixed);
        }
    }

    [McpForUnityTool("timeline.build_float32_program", Description = "Build and publish the Float32 Program wrapper for one exact TimelineAsset path through the shared Semantic IR pipeline.", StructuredOutput = true, RequiresPolling = true, BackgroundPollingStatus = true, PollAction = "status", MaxPollSeconds = 600, HasBehaviorAnnotations = true, ReadOnlyHint = false, DestructiveHint = true, IdempotentHint = false, OpenWorldHint = false)]
    public static class BuildTimelineFloat32ProgramMcpTool
    {
        public sealed class Parameters
        {
            [ToolParameter("Omit or use start to create a job; use status to poll one job.", Required = false)]
            public string action { get; set; }

            [ToolParameter("Stable job identity returned by the initial call; required for status.", Required = false)]
            public string job_id { get; set; }

            [ToolParameter("Exact Assets/... path to one TimelineAsset required for start.", Required = false)]
            public string timeline_asset_path { get; set; }
        }

        public static object HandleCommand(JObject @params)
        {
            return CharacterSimulationBuildMcpJobScheduler.Handle(
                @params,
                CharacterSimulationBuildKind.TimelineFloat32);
        }
    }

    [McpForUnityTool("timeline.build_fixed_program", Description = "Build and publish the Fixed Program wrapper for one exact TimelineAsset and one exact wrapper destination through the shared Semantic IR pipeline.", StructuredOutput = true, RequiresPolling = true, BackgroundPollingStatus = true, PollAction = "status", MaxPollSeconds = 600, HasBehaviorAnnotations = true, ReadOnlyHint = false, DestructiveHint = true, IdempotentHint = false, OpenWorldHint = false)]
    public static class BuildTimelineFixedProgramMcpTool
    {
        public sealed class Parameters
        {
            [ToolParameter("Omit or use start to create a job; use status to poll one job.", Required = false)]
            public string action { get; set; }

            [ToolParameter("Stable job identity returned by the initial call; required for status.", Required = false)]
            public string job_id { get; set; }

            [ToolParameter("Exact Assets/... path to one TimelineAsset required for start.", Required = false)]
            public string timeline_asset_path { get; set; }

            [ToolParameter("Exact Assets/... .asset destination for the Fixed Timeline Program wrapper required for start.", Required = false)]
            public string wrapper_asset_path { get; set; }
        }

        public static object HandleCommand(JObject @params)
        {
            return CharacterSimulationBuildMcpJobScheduler.Handle(
                @params,
                CharacterSimulationBuildKind.TimelineFixed);
        }
    }

    static class CharacterSimulationBuildMcpBridge
    {
        static bool s_Building;

        public static object BuildFloat32(JObject parameters)
        {
            object validation = ValidateRequest(
                parameters,
                new HashSet<string>(StringComparer.Ordinal) { "definition_asset_path" },
                false,
                out CharacterPipelineDefinition definition,
                out string definitionPath,
                out _);
            if (validation != null)
                return validation;
            if (!TryEnter(out object busy))
                return busy;
            try
            {
                ICharacterSimulationTargetBuildAdapter target =
                    CharacterSimulationTargetCatalog.Float32(definition);
                CharacterSimulationBuildResult result = CharacterSimulationBuildOrchestrator.Build(
                    new CharacterSimulationBuildRequest(
                        definition,
                        CharacterSimulationBuildPublicationMode.Publish,
                        new[] { target }));
                if (!result.IsValid)
                    return BuildFailure(definitionPath, target.UnityWrapperDestination, result);

                CharacterSimulationProgramAsset wrapper =
                    AssetDatabase.LoadAssetAtPath<CharacterSimulationProgramAsset>(
                        target.UnityWrapperDestination);
                if (!wrapper)
                {
                    return new ErrorResponse(
                        "float32_wrapper_missing_after_build",
                        new { definitionAssetPath = definitionPath, wrapperAssetPath = target.UnityWrapperDestination });
                }
                return new SuccessResponse(
                    "Exact Float32 Program and Presentation Projection were published.",
                    CreateResponse(
                        definition,
                        definitionPath,
                        target.UnityWrapperDestination,
                        wrapper.NumericProfileId,
                        wrapper.TargetAbiVersion,
                        wrapper.ProgramId,
                        wrapper.SourceRevision,
                        wrapper.SemanticHash,
                        wrapper.ProgramHash,
                        wrapper.LayoutHash,
                        wrapper.CanonicalBytesHash,
                        wrapper.CanonicalByteLength,
                        result));
            }
            catch (Exception exception)
            {
                return new ErrorResponse(
                    "character_build_exception",
                    new { definitionAssetPath = definitionPath, message = exception.Message });
            }
            finally
            {
                s_Building = false;
            }
        }

        public static object BuildFixed(JObject parameters)
        {
            object validation = ValidateRequest(
                parameters,
                new HashSet<string>(StringComparer.Ordinal)
                {
                    "definition_asset_path",
                    "wrapper_asset_path"
                },
                true,
                out CharacterPipelineDefinition definition,
                out string definitionPath,
                out string wrapperPath);
            if (validation != null)
                return validation;
            if (!TryEnter(out object busy))
                return busy;
            try
            {
                ICharacterSimulationTargetBuildAdapter target =
                    new FixedCharacterSimulationTargetBuildAdapter(wrapperPath);
                CharacterSimulationBuildResult result = CharacterSimulationBuildOrchestrator.Build(
                    new CharacterSimulationBuildRequest(
                        definition,
                        CharacterSimulationBuildPublicationMode.Publish,
                        new[] { target }));
                if (!result.IsValid)
                    return BuildFailure(definitionPath, wrapperPath, result);

                FixedCharacterSimulationProgramAsset wrapper =
                    AssetDatabase.LoadAssetAtPath<FixedCharacterSimulationProgramAsset>(wrapperPath);
                if (!wrapper)
                {
                    return new ErrorResponse(
                        "fixed_wrapper_missing_after_build",
                        new { definitionAssetPath = definitionPath, wrapperAssetPath = wrapperPath });
                }
                return new SuccessResponse(
                    "Exact Fixed Program and Presentation Projection were published.",
                    CreateResponse(
                        definition,
                        definitionPath,
                        wrapperPath,
                        ThirdPersonSimulation.Fixed.FixedSimulationNumericProfile.Value.Id.Value,
                        ThirdPersonSimulation.Fixed.FixedSimulationNumericProfile.Value.AbiVersion.Value,
                        wrapper.ProgramId,
                        wrapper.SourceRevision,
                        wrapper.SemanticHash,
                        wrapper.ProgramHash,
                        wrapper.LayoutHash,
                        wrapper.CanonicalBytesHash,
                        wrapper.CanonicalByteLength,
                        result));
            }
            catch (Exception exception)
            {
                return new ErrorResponse(
                    "character_build_exception",
                    new
                    {
                        definitionAssetPath = definitionPath,
                        wrapperAssetPath = wrapperPath,
                        message = exception.Message
                    });
            }
            finally
            {
                s_Building = false;
            }
        }

        public static object BuildTimelineFloat32(JObject parameters)
        {
            object validation = ValidateTimelineRequest(
                parameters,
                new HashSet<string>(StringComparer.Ordinal) { "timeline_asset_path" },
                false,
                out TimelineAsset timeline,
                out string timelinePath,
                out _);
            if (validation != null)
                return validation;
            if (!TryEnter(out object busy))
                return busy;
            try
            {
                ICharacterSimulationTargetBuildAdapter target =
                    CharacterSimulationTargetCatalog.Float32(timeline);
                TimelineSimulationBuildResult result = CharacterSimulationBuildOrchestrator.Build(
                    new TimelineSimulationBuildRequest(
                        timeline,
                        CharacterSimulationBuildPublicationMode.Publish,
                        new[] { target }));
                if (!result.IsValid)
                    return TimelineBuildFailure(timelinePath, target.UnityWrapperDestination, result);
                TimelineSimulationProgramAsset wrapper =
                    AssetDatabase.LoadAssetAtPath<TimelineSimulationProgramAsset>(target.UnityWrapperDestination);
                if (!wrapper)
                {
                    return new ErrorResponse(
                        "timeline_float32_wrapper_missing_after_build",
                        new { timelineAssetPath = timelinePath, wrapperAssetPath = target.UnityWrapperDestination });
                }
                return new SuccessResponse(
                    "Exact Float32 Timeline Program was published.",
                    CreateTimelineResponse(
                        timelinePath,
                        target.UnityWrapperDestination,
                        wrapper.NumericProfileId,
                        wrapper.TargetAbiVersion,
                        wrapper.ProgramId,
                        wrapper.SourceRevision,
                        wrapper.SemanticHash,
                        wrapper.ProgramHash,
                        wrapper.LayoutHash,
                        wrapper.CanonicalBytesHash,
                        wrapper.CanonicalByteLength,
                        result));
            }
            catch (Exception exception)
            {
                return new ErrorResponse(
                    "timeline_build_exception",
                    new { timelineAssetPath = timelinePath, message = exception.Message });
            }
            finally
            {
                s_Building = false;
            }
        }

        public static object BuildTimelineFixed(JObject parameters)
        {
            object validation = ValidateTimelineRequest(
                parameters,
                new HashSet<string>(StringComparer.Ordinal)
                {
                    "timeline_asset_path",
                    "wrapper_asset_path"
                },
                true,
                out TimelineAsset timeline,
                out string timelinePath,
                out string wrapperPath);
            if (validation != null)
                return validation;
            if (!TryEnter(out object busy))
                return busy;
            try
            {
                ICharacterSimulationTargetBuildAdapter target =
                    new FixedCharacterSimulationTargetBuildAdapter(
                        wrapperPath,
                        SimulationProgramRootKind.Timeline);
                TimelineSimulationBuildResult result = CharacterSimulationBuildOrchestrator.Build(
                    new TimelineSimulationBuildRequest(
                        timeline,
                        CharacterSimulationBuildPublicationMode.Publish,
                        new[] { target }));
                if (!result.IsValid)
                    return TimelineBuildFailure(timelinePath, wrapperPath, result);
                FixedTimelineSimulationProgramAsset wrapper =
                    AssetDatabase.LoadAssetAtPath<FixedTimelineSimulationProgramAsset>(wrapperPath);
                if (!wrapper)
                {
                    return new ErrorResponse(
                        "timeline_fixed_wrapper_missing_after_build",
                        new { timelineAssetPath = timelinePath, wrapperAssetPath = wrapperPath });
                }
                return new SuccessResponse(
                    "Exact Fixed Timeline Program was published.",
                    CreateTimelineResponse(
                        timelinePath,
                        wrapperPath,
                        ThirdPersonSimulation.Fixed.FixedSimulationNumericProfile.Value.Id.Value,
                        ThirdPersonSimulation.Fixed.FixedSimulationNumericProfile.Value.AbiVersion.Value,
                        wrapper.ProgramId,
                        wrapper.SourceRevision,
                        wrapper.SemanticHash,
                        wrapper.ProgramHash,
                        wrapper.LayoutHash,
                        wrapper.CanonicalBytesHash,
                        wrapper.CanonicalByteLength,
                        result));
            }
            catch (Exception exception)
            {
                return new ErrorResponse(
                    "timeline_build_exception",
                    new
                    {
                        timelineAssetPath = timelinePath,
                        wrapperAssetPath = wrapperPath,
                        message = exception.Message
                    });
            }
            finally
            {
                s_Building = false;
            }
        }

        static object ValidateRequest(
            JObject parameters,
            HashSet<string> allowed,
            bool requiresWrapper,
            out CharacterPipelineDefinition definition,
            out string definitionPath,
            out string wrapperPath)
        {
            definition = null;
            definitionPath = string.Empty;
            wrapperPath = string.Empty;
            if (parameters == null)
                return new ErrorResponse("request_missing");
            string unknown = parameters.Properties()
                .Select(property => property.Name)
                .FirstOrDefault(name => !allowed.Contains(name));
            if (!string.IsNullOrEmpty(unknown))
                return new ErrorResponse("unknown_parameter", new { parameter = unknown });
            if (!TryGetExactAssetPath(parameters, "definition_asset_path", out definitionPath))
                return new ErrorResponse("definition_asset_path_required");
            definition = AssetDatabase.LoadAssetAtPath<CharacterPipelineDefinition>(definitionPath);
            if (!definition ||
                !string.Equals(AssetDatabase.GetAssetPath(definition), definitionPath, StringComparison.Ordinal))
            {
                return new ErrorResponse(
                    "character_pipeline_definition_not_found",
                    new { definitionAssetPath = definitionPath });
            }
            if (!requiresWrapper)
                return null;
            if (!TryGetExactAssetPath(parameters, "wrapper_asset_path", out wrapperPath))
                return new ErrorResponse("wrapper_asset_path_required", new { definitionAssetPath = definitionPath });
            return null;
        }

        static object ValidateTimelineRequest(
            JObject parameters,
            HashSet<string> allowed,
            bool requiresWrapper,
            out TimelineAsset timeline,
            out string timelinePath,
            out string wrapperPath)
        {
            timeline = null;
            timelinePath = string.Empty;
            wrapperPath = string.Empty;
            if (parameters == null)
                return new ErrorResponse("request_missing");
            string unknown = parameters.Properties()
                .Select(property => property.Name)
                .FirstOrDefault(name => !allowed.Contains(name));
            if (!string.IsNullOrEmpty(unknown))
                return new ErrorResponse("unknown_parameter", new { parameter = unknown });
            if (!TryGetExactAssetPath(parameters, "timeline_asset_path", out timelinePath))
                return new ErrorResponse("timeline_asset_path_required");
            timeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>(timelinePath);
            if (!timeline || !string.Equals(AssetDatabase.GetAssetPath(timeline), timelinePath, StringComparison.Ordinal))
            {
                return new ErrorResponse(
                    "timeline_asset_not_found",
                    new { timelineAssetPath = timelinePath });
            }
            if (!requiresWrapper)
                return null;
            if (!TryGetExactAssetPath(parameters, "wrapper_asset_path", out wrapperPath))
                return new ErrorResponse("wrapper_asset_path_required", new { timelineAssetPath = timelinePath });
            return null;
        }

        static bool TryGetExactAssetPath(
            JObject parameters,
            string key,
            out string path)
        {
            path = string.Empty;
            JToken token = parameters[key];
            if (token?.Type != JTokenType.String)
                return false;
            path = token.Value<string>();
            return !string.IsNullOrWhiteSpace(path) &&
                   path.StartsWith("Assets/", StringComparison.Ordinal) &&
                   path.EndsWith(".asset", StringComparison.OrdinalIgnoreCase) &&
                   path.IndexOf('\\') < 0 &&
                   path.IndexOf("/../", StringComparison.Ordinal) < 0 &&
                   path.IndexOf("/./", StringComparison.Ordinal) < 0 &&
                   path.IndexOf("//", StringComparison.Ordinal) < 0;
        }

        static bool TryEnter(out object error)
        {
            error = null;
            if (s_Building)
            {
                error = new ErrorResponse("character_build_already_running");
                return false;
            }
            if (EditorApplication.isCompiling ||
                EditorApplication.isUpdating ||
                EditorApplication.isPlayingOrWillChangePlaymode ||
                Application.isPlaying)
            {
                error = new ErrorResponse(
                    "unity_editor_busy",
                    new
                    {
                        isCompiling = EditorApplication.isCompiling,
                        isUpdating = EditorApplication.isUpdating,
                        isPlayingOrWillChangePlaymode = EditorApplication.isPlayingOrWillChangePlaymode
                    });
                return false;
            }
            s_Building = true;
            return true;
        }

        static object BuildFailure(
            string definitionPath,
            string wrapperPath,
            CharacterSimulationBuildResult result)
        {
            return new ErrorResponse(
                "character_build_failed",
                new
                {
                    definitionAssetPath = definitionPath,
                    wrapperAssetPath = wrapperPath,
                    diagnostics = Messages(result)
                });
        }

        static object TimelineBuildFailure(
            string timelinePath,
            string wrapperPath,
            TimelineSimulationBuildResult result)
        {
            return new ErrorResponse(
                "timeline_build_failed",
                new
                {
                    timelineAssetPath = timelinePath,
                    wrapperAssetPath = wrapperPath,
                    diagnostics = Messages(result)
                });
        }

        static object CreateResponse(
            CharacterPipelineDefinition definition,
            string definitionPath,
            string wrapperPath,
            string numericProfileId,
            int targetAbiVersion,
            string programId,
            string sourceRevision,
            string semanticHash,
            string programHash,
            string layoutHash,
            string canonicalBytesHash,
            int canonicalByteLength,
            CharacterSimulationBuildResult result)
        {
            CharacterPresentationProjectionAsset projection =
                definition.PresentationProjection;
            return new
            {
                definitionAssetPath = definitionPath,
                wrapperAssetPath = wrapperPath,
                projectionAssetPath = projection ? AssetDatabase.GetAssetPath(projection) : string.Empty,
                numericProfileId,
                targetAbiVersion,
                programId,
                sourceRevision,
                semanticHash,
                programHash,
                layoutHash,
                canonicalBytesHash,
                canonicalByteLength,
                projection = projection
                    ? new
                    {
                        projection.ProgramId,
                        projection.SourceRevision,
                        projection.SemanticHash,
                        projection.ContractHash,
                        projection.ProjectionRevision
                    }
                    : null,
                diagnostics = Messages(result)
            };
        }

        static object CreateTimelineResponse(
            string timelinePath,
            string wrapperPath,
            string numericProfileId,
            int targetAbiVersion,
            string programId,
            string sourceRevision,
            string semanticHash,
            string programHash,
            string layoutHash,
            string canonicalBytesHash,
            int canonicalByteLength,
            TimelineSimulationBuildResult result)
        {
            SimulationProgramRootDescriptor root = result.Artifact.Header.Root;
            return new
            {
                timelineAssetPath = timelinePath,
                wrapperAssetPath = wrapperPath,
                numericProfileId,
                targetAbiVersion,
                programId,
                sourceRevision,
                semanticHash,
                programHash,
                layoutHash,
                canonicalBytesHash,
                canonicalByteLength,
                rootKind = root.Kind.ToString(),
                rootIdentity = root.RootIdentity,
                entryIdentity = root.EntryIdentity,
                contentIdentity = root.ContentIdentity,
                diagnostics = Messages(result)
            };
        }

        static object[] Messages(CharacterSimulationBuildResult result)
        {
            return Messages(result?.Report);
        }

        static object[] Messages(TimelineSimulationBuildResult result)
        {
            return Messages(result?.Report);
        }

        static object[] Messages(CharacterSimulationCompileReport report)
        {
            if (report == null)
                return Array.Empty<object>();
            return report.Messages
                .Select(message => (object)new
                {
                    stage = message.Stage.ToString(),
                    severity = message.Severity.ToString(),
                    message.Code,
                    message.SourceIdentity,
                    message.Message
                })
                .ToArray();
        }
    }
}
