using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BTSMTL.EventGraphs;
using BTSMTL.Timeline;
using MCPForUnity.Editor.Helpers;
using MCPForUnity.Editor.Tools;
using Newtonsoft.Json.Linq;
using ThirdPersonCharacter.Pipeline;
using ThirdPersonCharacter.Pipeline.Editor.Authoring.EventGraph;
using ThirdPersonCharacter.Control.Authoring;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    [McpForUnityTool(
        "btsmtl.export_code",
        Description = "从一个精确的正式 Skill Graph、Timeline 或 EventGraph 资产完整导出可重建的 C# authoring 文件；不修改输入资产，不读取旧源码，不触发生成或Build。",
        StructuredOutput = true,
        AutoRegister = true,
        RequiresPolling = false,
        HasBehaviorAnnotations = true,
        ReadOnlyHint = false,
        DestructiveHint = true,
        IdempotentHint = true,
        OpenWorldHint = false)]
    public static class BtsmtlAuthoringCodeExportMcpTool
    {
        public sealed class Parameters
        {
            [ToolParameter("精确 Assets/... Skill Graph、Timeline 或 EventGraph 资产路径。", Required = true)]
            public string asset_path { get; set; }

            [ToolParameter("当 asset_path 指向含多个子资产的文件时，填写精确 local file id。", Required = false)]
            public long asset_local_file_id { get; set; }

            [ToolParameter("精确 CharacterPipelineDefinition 资产路径。", Required = true)]
            public string definition_asset_path { get; set; }

            [ToolParameter("项目 Editor 代码目录内的精确 .cs 输出路径。", Required = true)]
            public string output_code_path { get; set; }

            [ToolParameter("正式生成入口 recipe identity。", Required = true)]
            public string recipe_type { get; set; }

            [ToolParameter("生成 C# 类型名，不含命名空间。", Required = true)]
            public string entry_type_name { get; set; }

            [ToolParameter("生成 C# 命名空间。", Required = true)]
            public string namespace_name { get; set; }
        }

        public static object HandleCommand(JObject @params) =>
            BtsmtlAuthoringCodeMcpBridge.Export(@params);
    }

    [McpForUnityTool(
        "btsmtl.generate_assets",
        Description = "执行一个精确且已编译的 C# authoring 入口，使用正式 Graph/Timeline API 创建或替换指定资产并保存；不接受任意源码正文，不运行旧同名入口，不触发Build。",
        StructuredOutput = true,
        AutoRegister = true,
        RequiresPolling = false,
        HasBehaviorAnnotations = true,
        ReadOnlyHint = false,
        DestructiveHint = true,
        IdempotentHint = false,
        OpenWorldHint = false)]
    public static class BtsmtlAuthoringCodeGenerateMcpTool
    {
        public sealed class Parameters
        {
            [ToolParameter("精确已编译 C# authoring 文件路径。", Required = true)]
            public string source_code_path { get; set; }

            [ToolParameter("必须与已编译入口的 RecipeType 完全一致。", Required = true)]
            public string recipe_type { get; set; }

            [ToolParameter("必须填写已编译入口返回的完整 EntryTypeName。", Required = true)]
            public string entry_type_name { get; set; }

            [ToolParameter("精确 CharacterPipelineDefinition 资产路径。", Required = true)]
            public string definition_asset_path { get; set; }

            [ToolParameter("精确生成输出资产路径或根输出路径。", Required = true)]
            public string output_asset_path { get; set; }
        }

        public static object HandleCommand(JObject @params) =>
            BtsmtlAuthoringCodeMcpBridge.Generate(@params);
    }

    static class BtsmtlAuthoringCodeMcpBridge
    {
        static readonly IReadOnlyList<IBtsmtlAuthoringCodeDomainAdapter> s_Adapters =
            new IBtsmtlAuthoringCodeDomainAdapter[]
            {
                new BtsmtlSkillAuthoringCodeAdapter(),
                EventGraphAuthoringCodeAdapter.Instance
            };

        public static object Export(JObject parameters)
        {
            const string operation = "export_code";
            try
            {
                EnsureEditorReady(operation);
                RejectUnknown(parameters, new[]
                {
                    "asset_path",
                    "asset_local_file_id",
                    "definition_asset_path",
                    "output_code_path",
                    "recipe_type",
                    "entry_type_name",
                    "namespace_name"
                });
                string assetPath = RequireAssetPath(parameters, "asset_path");
                string definitionPath = RequireDefinitionPath(parameters);
                string outputRelativePath = RequireEditorCodePath(parameters, "output_code_path", true);
                string recipeType = RequireString(parameters, "recipe_type");
                string entryTypeName = RequireString(parameters, "entry_type_name");
                string namespaceName = RequireString(parameters, "namespace_name");
                long localFileId = RequireLocalFileId(parameters);
                UnityEngine.Object root = ResolveRoot(assetPath, localFileId);
                var request = new BtsmtlAuthoringCodeExportRequest(
                    root,
                    definitionPath,
                    ProjectAbsolutePath(outputRelativePath),
                    recipeType,
                    namespaceName,
                    entryTypeName);
                BtsmtlAuthoringCodeExportResult result =
                    BtsmtlAuthoringCodeExportService.Export(request, s_Adapters);
                if (!result.Success)
                    return Failure(operation, result.Diagnostics);
                BtsmtlAuthoringCodeFileWriter.Write(result);
                return new
                {
                    success = true,
                    message = "正式 authoring 资产已显式导出为 C#。",
                    data = new
                    {
                        operation,
                        asset_path = assetPath,
                        definition_asset_path = definitionPath,
                        output_code_path = outputRelativePath,
                        recipe_type = result.RecipeType,
                        entry_type_name = $"{namespaceName}.{result.EntryTypeName}",
                        external_dependencies = result.ExternalDependencies.Select(value => new
                        {
                            asset_path = value.AssetPath,
                            type_name = value.TypeName,
                            local_file_id = value.LocalFileId
                        }).ToArray(),
                        diagnostics = Diagnostics(result.Diagnostics)
                    }
                };
            }
            catch (Exception error)
            {
                return new ErrorResponse("authoring_code_export_failed", new { operation, message = error.Message });
            }
        }

        public static object Generate(JObject parameters)
        {
            const string operation = "generate_assets";
            try
            {
                EnsureEditorReady(operation);
                RejectUnknown(parameters, new[]
                {
                    "source_code_path",
                    "recipe_type",
                    "entry_type_name",
                    "definition_asset_path",
                    "output_asset_path"
                });
                string sourceRelativePath = RequireEditorCodePath(parameters, "source_code_path", false);
                string sourceAbsolutePath = ProjectAbsolutePath(sourceRelativePath);
                if (!File.Exists(sourceAbsolutePath))
                    return new ErrorResponse("source_code_missing", new { operation, source_code_path = sourceRelativePath });
                string recipeType = RequireString(parameters, "recipe_type");
                string entryTypeName = RequireString(parameters, "entry_type_name");
                string definitionPath = RequireDefinitionPath(parameters);
                string outputPath = RequireAssetPath(parameters, "output_asset_path");
                Type entryType = ResolveEntryType(entryTypeName);
                if (entryType == null || !typeof(IBtsmtlAuthoringGenerationEntry).IsAssignableFrom(entryType) || entryType.IsAbstract)
                    return new ErrorResponse("generation_entry_invalid", new { operation, entry_type_name = entryTypeName });
                if (Activator.CreateInstance(entryType) is not IBtsmtlAuthoringGenerationEntry entry)
                    return new ErrorResponse("generation_entry_create_failed", new { operation, entry_type_name = entryTypeName });
                var request = new BtsmtlAuthoringGenerationRequest(
                    sourceAbsolutePath,
                    recipeType,
                    entryTypeName,
                    definitionPath,
                    outputPath);
                var context = new BtsmtlUnityAuthoringGenerationContext(request);
                BtsmtlAuthoringGenerationResult result =
                    BtsmtlAuthoringGenerationService.Execute(request, entry, context);
                if (!result.Success)
                    return Failure(operation, result.Diagnostics);
                return new
                {
                    success = true,
                    message = result.Saved
                        ? "C# authoring 入口已执行并保存资产。"
                        : "C# authoring 入口已执行。",
                    data = new
                    {
                        operation,
                        source_code_path = sourceRelativePath,
                        recipe_type = recipeType,
                        entry_type_name = entryTypeName,
                        definition_asset_path = definitionPath,
                        output_asset_path = result.OutputAssetPath,
                        saved = result.Saved,
                        created_asset_paths = result.CreatedAssetPaths.ToArray(),
                        replaced_asset_paths = result.ReplacedAssetPaths.ToArray(),
                        deleted_asset_paths = result.DeletedAssetPaths.ToArray(),
                        diagnostics = Diagnostics(result.Diagnostics)
                    }
                };
            }
            catch (Exception error)
            {
                return new ErrorResponse("authoring_code_generation_failed", new { operation, message = error.Message });
            }
        }

        static void EnsureEditorReady(string operation)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException($"{operation}在Play或切换Play期间不可用。");
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || AssetDatabase.IsAssetImportWorkerProcess())
                throw new InvalidOperationException($"{operation}要求Unity完成编译和资源导入。");
        }

        static UnityEngine.Object ResolveRoot(string assetPath, long localFileId)
        {
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            UnityEngine.Object[] supported = assets
                .Where(value => value is BtsmtlSkillFlowGraph || value is TimelineAsset || value is HostEventGraph)
                .ToArray();
            if (localFileId != 0L)
            {
                UnityEngine.Object match = supported.SingleOrDefault(value =>
                    AssetDatabase.TryGetGUIDAndLocalFileIdentifier(value, out _, out long currentId) &&
                    currentId == localFileId);
                return match ?? throw new InvalidOperationException(
                    $"Asset '{assetPath}' has no supported authoring root with local id {localFileId}.");
            }
            UnityEngine.Object main = AssetDatabase.LoadMainAssetAtPath(assetPath);
            if (main is BtsmtlSkillFlowGraph || main is TimelineAsset || main is HostEventGraph)
                return main;
            if (supported.Length == 1)
                return supported[0];
            throw new InvalidOperationException(
                $"Asset '{assetPath}' has multiple supported authoring roots; asset_local_file_id is required.");
        }

        static Type ResolveEntryType(string entryTypeName)
        {
            Type result = Type.GetType(entryTypeName, false);
            if (result != null)
                return result;
            return AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType(entryTypeName, false))
                .SingleOrDefault(type => type != null);
        }

        static void RejectUnknown(JObject parameters, IEnumerable<string> allowed)
        {
            if (parameters == null)
                throw new ArgumentException("Request parameters are missing.");
            var allowedSet = new HashSet<string>(allowed, StringComparer.Ordinal);
            string unknown = parameters.Properties()
                .Select(property => property.Name)
                .FirstOrDefault(name => !allowedSet.Contains(name));
            if (!string.IsNullOrEmpty(unknown))
                throw new ArgumentException($"Unknown parameter '{unknown}'.");
        }

        static string RequireDefinitionPath(JObject parameters)
        {
            string path = RequireAssetPath(parameters, "definition_asset_path");
            CharacterPipelineDefinition definition = AssetDatabase.LoadAssetAtPath<CharacterPipelineDefinition>(path);
            if (!definition || AssetDatabase.LoadMainAssetAtPath(path) != definition)
                throw new InvalidOperationException($"Character Pipeline Definition '{path}' is unavailable.");
            return path;
        }

        static string RequireAssetPath(JObject parameters, string key)
        {
            string value = RequireString(parameters, key).Replace('\\', '/');
            if (!value.StartsWith("Assets/", StringComparison.Ordinal) || value.Contains(".."))
                throw new ArgumentException($"Parameter '{key}' must be an exact Assets/... path.");
            return value;
        }

        static string RequireEditorCodePath(JObject parameters, string key, bool output)
        {
            string value = RequireString(parameters, key).Replace('\\', '/');
            if (Path.IsPathRooted(value))
            {
                string root = ProjectRoot().Replace('\\', '/').TrimEnd('/');
                if (!value.StartsWith(root + "/", StringComparison.OrdinalIgnoreCase))
                    throw new ArgumentException($"Parameter '{key}' must stay inside the current project.");
                value = value.Substring(root.Length + 1);
            }
            if (!value.StartsWith("Assets/GameScripts/Main/Editor/", StringComparison.Ordinal) ||
                value.Contains("..") ||
                !value.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException($"Parameter '{key}' must be an exact Editor .cs path.");
            if (output && string.Equals(value, "Assets/GameScripts/Main/Editor/CharacterPipeline/Authoring/CodeGeneration/BtsmtlAuthoringCodeMcpTools.cs", StringComparison.Ordinal))
                throw new ArgumentException("Generated output cannot replace the authoring MCP source.");
            return value;
        }

        static long RequireLocalFileId(JObject parameters)
        {
            JToken value = parameters["asset_local_file_id"];
            if (value == null)
                return 0L;
            if (value.Type != JTokenType.Integer)
                throw new ArgumentException("asset_local_file_id must be an integer.");
            return value.Value<long>();
        }

        static string RequireString(JObject parameters, string key)
        {
            string value = parameters?[key]?.Value<string>()?.Trim();
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException($"Parameter '{key}' is required.");
            return value;
        }

        static string ProjectRoot() => Directory.GetParent(Application.dataPath).FullName;

        static string ProjectAbsolutePath(string relativePath) =>
            Path.Combine(ProjectRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar));

        static object Failure(string operation, IReadOnlyList<BtsmtlAuthoringCodeDiagnostic> diagnostics) =>
            new ErrorResponse(
                diagnostics.FirstOrDefault().Code ?? "authoring_code_failed",
                new
                {
                    operation,
                    diagnostics = Diagnostics(diagnostics)
                });

        static object[] Diagnostics(IEnumerable<BtsmtlAuthoringCodeDiagnostic> diagnostics) =>
            (diagnostics ?? Enumerable.Empty<BtsmtlAuthoringCodeDiagnostic>())
            .Select(value => new
            {
                severity = value.Severity.ToString(),
                code = value.Code,
                subject = value.Subject,
                message = value.Message,
                suggestion = value.Suggestion
            })
            .Cast<object>()
            .ToArray();
    }
}
