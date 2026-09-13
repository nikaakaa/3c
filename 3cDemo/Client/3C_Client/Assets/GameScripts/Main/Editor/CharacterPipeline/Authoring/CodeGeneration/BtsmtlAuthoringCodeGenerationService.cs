using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public static class BtsmtlAuthoringCodeExportService
    {
        public static BtsmtlAuthoringCodeExportResult Export(
            BtsmtlAuthoringCodeExportRequest request,
            IReadOnlyList<IBtsmtlAuthoringCodeDomainAdapter> adapters)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            if (adapters == null)
                throw new ArgumentNullException(nameof(adapters));

            var matchingAdapters = new List<IBtsmtlAuthoringCodeDomainAdapter>();
            foreach (IBtsmtlAuthoringCodeDomainAdapter adapter in adapters)
            {
                if (adapter == null)
                    continue;
                bool handlesRoot;
                try
                {
                    handlesRoot = adapter.CanHandle(request.Root);
                }
                catch (Exception error)
                {
                    return Failure(
                        request,
                        new BtsmtlAuthoringCodeDiagnostic(
                            BtsmtlAuthoringCodeDiagnosticSeverity.Error,
                            "domain_support_check_failed",
                            adapter.DomainId,
                            error.Message));
                }
                if (handlesRoot)
                    matchingAdapters.Add(adapter);
            }

            if (matchingAdapters.Count == 0)
                return Failure(
                    request,
                    new BtsmtlAuthoringCodeDiagnostic(
                        BtsmtlAuthoringCodeDiagnosticSeverity.Error,
                        "unsupported_authoring_root",
                        request.Root.GetType().FullName,
                        "没有正式领域适配支持该导出根对象。",
                        "为该正式领域提供薄输出适配；不能省略对象后报告成功。"));
            if (matchingAdapters.Count > 1)
                return Failure(
                    request,
                    new BtsmtlAuthoringCodeDiagnostic(
                        BtsmtlAuthoringCodeDiagnosticSeverity.Error,
                        "multiple_authoring_adapters",
                        request.Root.GetType().FullName,
                        "多个正式领域适配同时认领了同一个导出根对象。"));

            var context = new BtsmtlAuthoringCodeExportContext(request);
            IBtsmtlAuthoringCodeDomainAdapter selectedAdapter = matchingAdapters[0];
            try
            {
                selectedAdapter.Emit(context, request.Root);
            }
            catch (Exception error)
            {
                context.ReportError("domain_export_failed", selectedAdapter.DomainId, error.Message);
            }

            if (string.IsNullOrEmpty(context.RootVariableName))
                context.ReportError(
                    "authoring_root_not_registered",
                    request.Root.GetType().FullName,
                    "正式领域适配没有注册导出根对象变量。",
                    "先注册根对象，再输出根绑定代码。");
            if (context.HasErrors)
                return context.CreateResult(Array.Empty<BtsmtlAuthoringCodeSourceFile>());

            return context.CreateResult(BtsmtlAuthoringCodeSourceBuilder.Build(context));
        }

        static BtsmtlAuthoringCodeExportResult Failure(
            BtsmtlAuthoringCodeExportRequest request,
            BtsmtlAuthoringCodeDiagnostic diagnostic)
        {
            BtsmtlAuthoringCodeDiagnostic located = string.IsNullOrEmpty(diagnostic.FilePath)
                ? new BtsmtlAuthoringCodeDiagnostic(
                    diagnostic.Severity,
                    diagnostic.Code,
                    diagnostic.Subject,
                    diagnostic.Message,
                    diagnostic.Suggestion,
                    request.OutputCodePath)
                : diagnostic;
            return new BtsmtlAuthoringCodeExportResult(
                false,
                request.OutputCodePath,
                request.DefinitionAssetPath,
                request.RecipeType,
                request.EntryTypeName,
                Array.Empty<BtsmtlAuthoringCodeSourceFile>(),
                Array.Empty<BtsmtlAuthoringCodeExternalDependency>(),
                new[] { located });
        }
    }

    public static class BtsmtlAuthoringGenerationService
    {
        public static BtsmtlAuthoringGenerationResult Execute(
            BtsmtlAuthoringGenerationRequest request,
            IBtsmtlAuthoringGenerationEntry entry,
            BtsmtlAuthoringGenerationContext context)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            if (entry == null)
                return BtsmtlAuthoringGenerationResult.Failure(
                    request.OutputAssetPath,
                    new BtsmtlAuthoringCodeDiagnostic(
                        BtsmtlAuthoringCodeDiagnosticSeverity.Error,
                        "generation_entry_missing",
                        request.EntryTypeName,
                        "已编译的正式生成入口不能为空。"));
            if (context == null)
                return BtsmtlAuthoringGenerationResult.Failure(
                    request.OutputAssetPath,
                    new BtsmtlAuthoringCodeDiagnostic(
                        BtsmtlAuthoringCodeDiagnosticSeverity.Error,
                        "generation_context_missing",
                        request.EntryTypeName,
                        "正式生成上下文不能为空。"));
            if (!string.Equals(request.EntryTypeName, entry.GetType().FullName, StringComparison.Ordinal))
                return BtsmtlAuthoringGenerationResult.Failure(
                    request.OutputAssetPath,
                    new BtsmtlAuthoringCodeDiagnostic(
                        BtsmtlAuthoringCodeDiagnosticSeverity.Error,
                        "generation_entry_mismatch",
                        request.EntryTypeName,
                        "请求的入口类型与已编译正式入口不一致。"));
            if (!SourceMatchesEntry(request.SourceCodePath, entry.GetType()))
                return BtsmtlAuthoringGenerationResult.Failure(
                    request.OutputAssetPath,
                    new BtsmtlAuthoringCodeDiagnostic(
                        BtsmtlAuthoringCodeDiagnosticSeverity.Error,
                        "generation_source_entry_mismatch",
                        request.EntryTypeName,
                        "请求源码路径没有关联到当前已编译入口类型。"));
            if (!string.Equals(request.DefinitionAssetPath, context.DefinitionAssetPath, StringComparison.Ordinal) ||
                !string.Equals(request.OutputAssetPath, context.OutputAssetPath, StringComparison.Ordinal))
                return BtsmtlAuthoringGenerationResult.Failure(
                    request.OutputAssetPath,
                    new BtsmtlAuthoringCodeDiagnostic(
                        BtsmtlAuthoringCodeDiagnosticSeverity.Error,
                        "generation_context_mismatch",
                        request.EntryTypeName,
                        "生成请求与正式生成上下文的精确资产路径不一致。"));

            try
            {
                return entry.Execute(context) ?? BtsmtlAuthoringGenerationResult.Failure(
                    request.OutputAssetPath,
                    new BtsmtlAuthoringCodeDiagnostic(
                        BtsmtlAuthoringCodeDiagnosticSeverity.Error,
                        "generation_entry_empty_result",
                        request.EntryTypeName,
                        "正式生成入口没有返回结果。"));
            }
            catch (Exception error)
            {
                return BtsmtlAuthoringGenerationResult.Failure(
                    request.OutputAssetPath,
                    new BtsmtlAuthoringCodeDiagnostic(
                        BtsmtlAuthoringCodeDiagnosticSeverity.Error,
                        "generation_entry_failed",
                        request.EntryTypeName,
                        error.Message));
            }
        }

        static bool SourceMatchesEntry(string sourceCodePath, Type entryType)
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string absolutePath = Path.GetFullPath(sourceCodePath ?? string.Empty);
            if (!string.Equals(
                    Path.GetFileNameWithoutExtension(absolutePath),
                    entryType.Name,
                    StringComparison.Ordinal))
                return false;
            string normalizedRoot = projectRoot.Replace('\\', '/').TrimEnd('/');
            string normalizedSource = absolutePath.Replace('\\', '/');
            if (!normalizedSource.StartsWith(normalizedRoot + "/", StringComparison.OrdinalIgnoreCase))
                return false;
            string assetPath = normalizedSource.Substring(normalizedRoot.Length + 1);
            MonoScript source = AssetDatabase.LoadAssetAtPath<MonoScript>(assetPath);
            return source && source.GetClass() == entryType;
        }
    }

    internal static class BtsmtlAuthoringCodeSourceBuilder
    {
        public static IReadOnlyList<BtsmtlAuthoringCodeSourceFile> Build(
            BtsmtlAuthoringCodeExportContext context)
        {
            string entryPath = Path.GetFullPath(context.OutputCodePath);
            string outputDirectory = Path.GetDirectoryName(entryPath);
            if (string.IsNullOrEmpty(outputDirectory))
                throw new InvalidOperationException("C#导出路径缺少专属生成目录。");

            var sharedAssetVariables = new HashSet<string>(
                context.ExternalAssets
                    .Where(value => context.IsVariableUsed(value.VariableName))
                    .Where(value => context.ExternalAssetUsageSections(value.VariableName).Count > 1)
                    .Select(value => value.VariableName),
                StringComparer.Ordinal);
            var localAssetGroups = context.ExternalAssets
                .Where(value => context.IsVariableUsed(value.VariableName))
                .Where(value => !sharedAssetVariables.Contains(value.VariableName))
                .GroupBy(value => context.ExternalAssetUsageSections(value.VariableName).FirstOrDefault() ?? "Root")
                .ToDictionary(value => value.Key, value => value.ToArray(), StringComparer.Ordinal);
            var files = new List<BtsmtlAuthoringCodeSourceFile>
            {
                CreateFile(
                    entryPath,
                    "Root",
                    true,
                    BuildEntry(
                        context,
                        sharedAssetVariables,
                        localAssetGroups.TryGetValue("Root", out BtsmtlAuthoringCodeExternalAssetReference[] rootAssets)
                            ? rootAssets
                            : Array.Empty<BtsmtlAuthoringCodeExternalAssetReference>(),
                        localAssetGroups.Keys.Where(value => value != "Root").ToArray()))
            };

            if (sharedAssetVariables.Count != 0)
            {
                string resourcesPath = Path.Combine(outputDirectory, "SharedResources.cs");
                files.Add(CreateFile(
                    resourcesPath,
                    "SharedResources",
                    false,
                    BuildSharedResources(context, sharedAssetVariables)));
            }

            foreach (string sectionName in context.Sections)
            {
                if (string.Equals(sectionName, "Root", StringComparison.Ordinal))
                    continue;
                if (!Enum.GetValues(typeof(BtsmtlAuthoringCodeEmissionPhase))
                        .Cast<BtsmtlAuthoringCodeEmissionPhase>()
                        .Any(phase => context.Statements(phase).Any(value =>
                            string.Equals(value.SectionName, sectionName, StringComparison.Ordinal))))
                    continue;
                string filePath = Path.Combine(
                    outputDirectory,
                    sectionName.Replace('/', Path.DirectorySeparatorChar) + ".cs");
                files.Add(CreateFile(
                    filePath,
                    sectionName,
                    false,
                    BuildSection(
                        context,
                        sectionName,
                        localAssetGroups.TryGetValue(sectionName, out BtsmtlAuthoringCodeExternalAssetReference[] localAssets)
                            ? localAssets
                            : Array.Empty<BtsmtlAuthoringCodeExternalAssetReference>())));
            }
            return files;
        }

        static BtsmtlAuthoringCodeSourceFile CreateFile(
            string filePath,
            string sectionName,
            bool isEntryPoint,
            string sourceCode)
        {
            string fullPath = Path.GetFullPath(filePath);
            string projectRoot = Path.GetFullPath(Directory.GetParent(Application.dataPath).FullName)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string normalizedRoot = projectRoot.Replace('\\', '/');
            string normalizedPath = fullPath.Replace('\\', '/');
            string relativePath = normalizedPath.StartsWith(normalizedRoot + "/", StringComparison.OrdinalIgnoreCase)
                ? normalizedPath.Substring(normalizedRoot.Length + 1)
                : normalizedPath;
            return new BtsmtlAuthoringCodeSourceFile(
                fullPath,
                relativePath,
                sectionName,
                isEntryPoint,
                sourceCode);
        }

        static string BuildEntry(
            BtsmtlAuthoringCodeExportContext context,
            ISet<string> sharedAssetVariables,
            IReadOnlyList<BtsmtlAuthoringCodeExternalAssetReference> rootLocalAssets,
            IReadOnlyCollection<string> localResourceSections)
        {
            var writer = new SourceWriter();
            WriteHeader(writer, context);
            writer.WriteLine(
                $"public sealed partial class {context.Request.EntryTypeName} : IBtsmtlAuthoringGenerationEntry");
            writer.OpenBlock();
            writer.WriteLine(
                "public BtsmtlAuthoringGenerationResult Execute(BtsmtlAuthoringGenerationContext context)");
            writer.OpenBlock();
            writer.WriteLine("var generation = new GenerationState();");
            if (rootLocalAssets.Count != 0)
                writer.WriteLine($"{ResourceLoaderName("Root")}(generation, context);");
            if (sharedAssetVariables.Count != 0)
                writer.WriteLine("LoadSharedResources(generation, context);");
            foreach (string sectionName in context.Sections)
                if (localResourceSections.Contains(sectionName))
                    writer.WriteLine($"{ResourceLoaderName(sectionName)}(generation, context);");
            foreach (BtsmtlAuthoringCodeEmissionPhase phase in Enum.GetValues(typeof(BtsmtlAuthoringCodeEmissionPhase)))
            {
                foreach (StatementBlock block in Blocks(context, phase))
                    writer.WriteLine($"{MethodName(phase, block.SectionName, block.Index)}(generation, context);");
            }
            writer.WriteLine($"return context.Complete(generation.{context.RootVariableName});");
            writer.CloseBlock();
            if (rootLocalAssets.Count != 0)
            {
                writer.WriteLine();
                WriteResourceLoader(writer, ResourceLoaderName("Root"), rootLocalAssets);
            }
            writer.WriteLine();
            WriteStatementBlocks(writer, context, "Root");
            writer.WriteLine();
            WriteState(writer, context);
            writer.CloseBlock();
            writer.CloseBlock();
            return writer.ToString();
        }

        static string BuildSharedResources(
            BtsmtlAuthoringCodeExportContext context,
            ISet<string> sharedAssetVariables)
        {
            var writer = new SourceWriter();
            WriteHeader(writer, context);
            writer.WriteLine($"public sealed partial class {context.Request.EntryTypeName}");
            writer.OpenBlock();
            WriteResourceLoader(
                writer,
                "LoadSharedResources",
                context.ExternalAssets
                    .Where(value => context.IsVariableUsed(value.VariableName))
                    .Where(value => sharedAssetVariables.Contains(value.VariableName))
                    .ToArray());
            writer.CloseBlock();
            writer.CloseBlock();
            return writer.ToString();
        }

        static string BuildSection(
            BtsmtlAuthoringCodeExportContext context,
            string sectionName,
            IReadOnlyList<BtsmtlAuthoringCodeExternalAssetReference> localAssets)
        {
            var writer = new SourceWriter();
            WriteHeader(writer, context);
            writer.WriteLine($"public sealed partial class {context.Request.EntryTypeName}");
            writer.OpenBlock();
            if (localAssets.Count != 0)
            {
                WriteResourceLoader(writer, ResourceLoaderName(sectionName), localAssets);
                writer.WriteLine();
            }
            WriteStatementBlocks(writer, context, sectionName);
            writer.CloseBlock();
            writer.CloseBlock();
            return writer.ToString();
        }

        static void WriteStatementBlocks(
            SourceWriter writer,
            BtsmtlAuthoringCodeExportContext context,
            string sectionName)
        {
            bool wroteMethod = false;
            foreach (BtsmtlAuthoringCodeEmissionPhase phase in Enum.GetValues(typeof(BtsmtlAuthoringCodeEmissionPhase)))
                foreach (StatementBlock block in Blocks(context, phase)
                             .Where(value => string.Equals(value.SectionName, sectionName, StringComparison.Ordinal)))
                {
                    if (wroteMethod)
                        writer.WriteLine();
                    writer.WriteLine(
                        $"static void {MethodName(block.Phase, sectionName, block.Index)}(GenerationState generation, BtsmtlAuthoringGenerationContext context)");
                    writer.OpenBlock();
                    foreach (BtsmtlAuthoringCodeStatement statement in block.Statements)
                        writer.WriteLine(RewriteStatement(context, statement.Text));
                    writer.CloseBlock();
                    wroteMethod = true;
                }
        }

        static string MethodName(
            BtsmtlAuthoringCodeEmissionPhase phase,
            string sectionName,
            int blockIndex) =>
            $"Build{phase}{BtsmtlAuthoringCodeSyntax.Identifier(sectionName.Replace('/', '_'))}{blockIndex}";

        static string ResourceLoaderName(string sectionName) =>
            $"Load{BtsmtlAuthoringCodeSyntax.Identifier(sectionName.Replace('/', '_'))}Resources";

        static IReadOnlyList<StatementBlock> Blocks(
            BtsmtlAuthoringCodeExportContext context,
            BtsmtlAuthoringCodeEmissionPhase phase)
        {
            var result = new List<StatementBlock>();
            StatementBlock current = null;
            foreach (BtsmtlAuthoringCodeStatement statement in context.Statements(phase))
            {
                if (current == null ||
                    !string.Equals(current.SectionName, statement.SectionName, StringComparison.Ordinal))
                {
                    current = new StatementBlock(phase, statement.SectionName, result.Count);
                    result.Add(current);
                }
                current.Statements.Add(statement);
            }
            return result;
        }

        static void WriteHeader(SourceWriter writer, BtsmtlAuthoringCodeExportContext context)
        {
            writer.WriteLine("using System;");
            writer.WriteLine("using ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration;");
            writer.WriteLine("using TimelineAnimationClip = BTSMTL.Timeline.AnimationClip;");
            writer.WriteLine("using UnityObject = UnityEngine.Object;");
            writer.WriteLine("using UnityAnimationClip = UnityEngine.AnimationClip;");
            foreach (string namespaceName in context.Usings.OrderBy(value => value, StringComparer.Ordinal))
                writer.WriteLine($"using {namespaceName};");
            writer.WriteLine();
            writer.WriteLine($"namespace {context.Request.NamespaceName}");
            writer.OpenBlock();
        }

        static void WriteResourceLoader(
            SourceWriter writer,
            string methodName,
            IReadOnlyList<BtsmtlAuthoringCodeExternalAssetReference> assets)
        {
            writer.WriteLine(
                $"static void {methodName}(GenerationState generation, BtsmtlAuthoringGenerationContext context)");
            writer.OpenBlock();
            foreach (BtsmtlAuthoringCodeExternalAssetReference asset in assets)
                writer.WriteLine(
                    $"generation.{asset.VariableName} = context.ResolveExternalAsset<{asset.TypeName}>({BtsmtlAuthoringCodeSyntax.StringLiteral(asset.AssetPath)}, {asset.LocalFileId}L);");
            writer.CloseBlock();
        }

        static void WriteState(
            SourceWriter writer,
            BtsmtlAuthoringCodeExportContext context)
        {
            writer.WriteLine("sealed class GenerationState");
            writer.OpenBlock();
            foreach (string variableName in context.ObjectVariableOrder.Where(context.IsVariableUsed))
                writer.WriteLine($"internal {context.VariableTypeNames[variableName]} {variableName};");
            foreach (BtsmtlAuthoringCodeExternalAssetReference asset in context.ExternalAssets
                         .Where(value => context.IsVariableUsed(value.VariableName)))
                writer.WriteLine($"internal {asset.TypeName} {asset.VariableName};");
            writer.CloseBlock();
        }

        static string RewriteStatement(
            BtsmtlAuthoringCodeExportContext context,
            string statement)
        {
            string result = statement;
            string declaredVariable = context.VariableTypeNames.Keys
                .Where(value => result.StartsWith($"var {value} =", StringComparison.Ordinal))
                .OrderByDescending(value => value.Length)
                .FirstOrDefault();
            if (!string.IsNullOrEmpty(declaredVariable) &&
                context.VariableSections.ContainsKey(declaredVariable))
            {
                string prefix = $"var {declaredVariable} =";
                result = $"generation.{declaredVariable} =" + result.Substring(prefix.Length);
            }

            foreach (string variableName in context.VariableTypeNames.Keys
                         .OrderByDescending(value => value.Length))
            {
                if (string.Equals(variableName, declaredVariable, StringComparison.Ordinal))
                    continue;
                result = ReplaceIdentifier(result, variableName, $"generation.{variableName}");
            }
            return result;
        }

        static string ReplaceIdentifier(string text, string identifier, string replacement)
        {
            var result = new StringBuilder(text.Length);
            bool inString = false;
            bool escaped = false;
            int index = 0;
            while (index < text.Length)
            {
                char character = text[index];
                if (character == '\"' && !escaped)
                    inString = !inString;
                if (!inString && IsIdentifierStart(text, index, identifier))
                {
                    result.Append(replacement);
                    index += identifier.Length;
                    escaped = false;
                    continue;
                }
                result.Append(character);
                escaped = inString && character == '\\' && !escaped;
                if (character != '\\')
                    escaped = false;
                index++;
            }
            return result.ToString();
        }

        static bool IsIdentifierStart(string text, int index, string identifier)
        {
            if (index > 0 && IsIdentifierCharacter(text[index - 1]))
                return false;
            if (index + identifier.Length > text.Length ||
                !string.Equals(text.Substring(index, identifier.Length), identifier, StringComparison.Ordinal))
                return false;
            int end = index + identifier.Length;
            return end == text.Length || !IsIdentifierCharacter(text[end]);
        }

        static bool IsIdentifierCharacter(char value) =>
            value == '_' || value >= '0' && value <= '9' ||
            value >= 'A' && value <= 'Z' || value >= 'a' && value <= 'z';

        sealed class StatementBlock
        {
            public StatementBlock(
                BtsmtlAuthoringCodeEmissionPhase phase,
                string sectionName,
                int index)
            {
                Phase = phase;
                SectionName = sectionName;
                Index = index;
            }

            public BtsmtlAuthoringCodeEmissionPhase Phase { get; }
            public string SectionName { get; }
            public int Index { get; }
            public List<BtsmtlAuthoringCodeStatement> Statements { get; } = new();
        }

        sealed class SourceWriter
        {
            readonly StringBuilder m_Source = new();
            int m_Indent;

            public void WriteLine(string value = null)
            {
                if (!string.IsNullOrEmpty(value))
                    m_Source.Append(' ', m_Indent * 4).Append(value);
                m_Source.AppendLine();
            }

            public void OpenBlock()
            {
                WriteLine("{");
                m_Indent++;
            }

            public void CloseBlock()
            {
                m_Indent--;
                WriteLine("}");
            }

            public override string ToString() => m_Source.ToString();
        }
    }
}
