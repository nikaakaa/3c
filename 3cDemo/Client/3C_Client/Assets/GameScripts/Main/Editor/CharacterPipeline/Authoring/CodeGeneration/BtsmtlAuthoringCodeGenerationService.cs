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
                            error.ToString()));
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
                        error.ToString()));
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

            OutputPlan plan = CreatePlan(context);
            Dictionary<string, HashSet<string>> namespaceSymbols =
                BuildNamespaceSymbols(new[]
                {
                    "System",
                    "ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration"
                }.Concat(context.Usings));
            var files = new List<BtsmtlAuthoringCodeSourceFile>
            {
                CreateFile(entryPath, "Root", true, TrimHeader(BuildRootFile(context, plan), context.Request.NamespaceName, namespaceSymbols))
            };
            if (plan.Root.HasStatements)
                files.Add(CreateFile(
                    Path.Combine(outputDirectory, "Root.cs"),
                    "Root",
                    false,
                    TrimHeader(BuildRootSectionFile(context, plan), context.Request.NamespaceName, namespaceSymbols)));
            foreach (SectionPlan section in plan.Sections)
            {
                if (string.Equals(section.Name, "Root", StringComparison.Ordinal) || !section.HasStatements)
                    continue;
                string filePath = Path.Combine(
                    outputDirectory,
                    section.Name.Replace('/', Path.DirectorySeparatorChar) + ".cs");
                files.Add(CreateFile(
                    filePath,
                    section.Name,
                    false,
                    TrimHeader(BuildSectionFile(context, plan, section), context.Request.NamespaceName, namespaceSymbols)));
            }
            return files;
        }

        static OutputPlan CreatePlan(BtsmtlAuthoringCodeExportContext context)
        {
            var plan = new OutputPlan();
            foreach (BtsmtlAuthoringCodeEmissionPhase phase in Enum.GetValues(typeof(BtsmtlAuthoringCodeEmissionPhase)))
                foreach (BtsmtlAuthoringCodeStatement statement in context.Statements(phase))
                    plan.Statements.Add(
                        new StatementPlan(
                            phase,
                            statement.SectionName,
                            statement.Text,
                            statement.CanRunWithSectionDependencies));

            foreach (BtsmtlAuthoringCodeExternalAssetReference asset in context.ExternalAssets)
            {
                if (!context.IsVariableUsed(asset.VariableName))
                    continue;
                List<string> usageSections = plan.Statements
                    .Where(value => ContainsIdentifier(value.Text, asset.VariableName))
                    .Select(value => value.SectionName)
                    .Distinct(StringComparer.Ordinal)
                    .ToList();
                if (usageSections.Count != 1 ||
                    plan.Statements.Any(value =>
                        value.Deferred && ContainsIdentifier(value.Text, asset.VariableName)))
                    plan.SharedAssets.Add(asset.VariableName);
                plan.VariableOwners[asset.VariableName] =
                    plan.SharedAssets.Contains(asset.VariableName)
                        ? "Root"
                        : usageSections.FirstOrDefault() ?? "Root";
            }

            foreach (KeyValuePair<string, string> value in context.VariableSections)
                plan.VariableOwners[value.Key] = value.Value;

            var sectionDependencies = new Dictionary<string, SortedSet<string>>(StringComparer.Ordinal);
            foreach (StatementPlan statement in plan.Statements)
            {
                if (statement.Phase == BtsmtlAuthoringCodeEmissionPhase.RootBinding)
                {
                    statement.Deferred = true;
                    continue;
                }
                List<string> dependencies = ReferencedOwners(plan, statement)
                    .Where(owner =>
                        !string.Equals(owner, statement.SectionName, StringComparison.Ordinal) &&
                        !string.Equals(owner, "Root", StringComparison.Ordinal))
                    .Distinct(StringComparer.Ordinal)
                    .ToList();
                if (dependencies.Count == 0)
                    continue;
                if (statement.Phase != BtsmtlAuthoringCodeEmissionPhase.Create ||
                    !statement.CanRunWithSectionDependencies ||
                    !AssignsLocalVariable(plan, statement))
                {
                    statement.Deferred = true;
                    continue;
                }
                if (!sectionDependencies.TryGetValue(statement.SectionName, out SortedSet<string> owners))
                {
                    owners = new SortedSet<string>(StringComparer.Ordinal);
                    sectionDependencies.Add(statement.SectionName, owners);
                }
                foreach (string owner in dependencies)
                    owners.Add(owner);
            }

            foreach (BtsmtlAuthoringCodeExternalAssetReference asset in context.ExternalAssets)
            {
                if (!plan.Statements.Any(statement => statement.Deferred && ContainsIdentifier(statement.Text, asset.VariableName)))
                    continue;
                plan.SharedAssets.Add(asset.VariableName);
                plan.VariableOwners[asset.VariableName] = "Root";
            }

            foreach (string variableName in context.VariableTypeNames.Keys)
            {
                if (!context.IsVariableUsed(variableName) ||
                    !plan.VariableOwners.TryGetValue(variableName, out string owner))
                    continue;
                if (string.Equals(variableName, context.RootVariableName, StringComparison.Ordinal))
                {
                    plan.Promoted.Add(variableName);
                    continue;
                }
                if (plan.Statements.Any(statement =>
                        ContainsIdentifier(statement.Text, variableName) &&
                        (statement.Deferred ||
                         !string.Equals(statement.SectionName, owner, StringComparison.Ordinal))))
                    plan.Promoted.Add(variableName);
            }

            List<string> sectionNames = OrderSections(context.Sections, sectionDependencies);
            foreach (string sectionName in sectionNames)
            {
                if (string.Equals(sectionName, "Root", StringComparison.Ordinal))
                    continue;
                if (plan.Statements.Any(value =>
                        string.Equals(value.SectionName, sectionName, StringComparison.Ordinal)))
                    plan.Sections.Add(new SectionPlan(sectionName));
            }
            plan.Sections.Insert(0, new SectionPlan("Root"));
            foreach (SectionPlan section in plan.Sections)
            {
                if (sectionDependencies.TryGetValue(section.Name, out SortedSet<string> dependencies))
                    section.Dependencies.AddRange(dependencies);
                foreach (string variableName in VariableOrder(context))
                    if (plan.Promoted.Contains(variableName) &&
                        plan.VariableOwners.TryGetValue(variableName, out string owner) &&
                        string.Equals(owner, section.Name, StringComparison.Ordinal))
                        section.PromotedVariables.Add(variableName);
                section.HasStatements = plan.Statements.Any(value =>
                    string.Equals(value.SectionName, section.Name, StringComparison.Ordinal));
                section.UsesRoot = !string.Equals(section.Name, "Root", StringComparison.Ordinal) &&
                    plan.Statements.Any(value =>
                        !value.Deferred &&
                        string.Equals(value.SectionName, section.Name, StringComparison.Ordinal) &&
                        context.VariableTypeNames.Keys.Any(variableName =>
                            ContainsIdentifier(value.Text, variableName) &&
                            plan.VariableOwners.TryGetValue(variableName, out string owner) &&
                            string.Equals(owner, "Root", StringComparison.Ordinal)));
                foreach (BtsmtlAuthoringCodeExternalAssetReference asset in context.ExternalAssets)
                    if (!plan.SharedAssets.Contains(asset.VariableName) &&
                        plan.VariableOwners.TryGetValue(asset.VariableName, out string assetOwner) &&
                        string.Equals(assetOwner, section.Name, StringComparison.Ordinal))
                        section.LocalAssets.Add(asset);
            }
            plan.Root = plan.Sections[0];
            plan.FinalStatements.AddRange(plan.Statements.Where(value => value.Deferred));
            foreach (StatementPlan statement in plan.FinalStatements)
                foreach (string variableName in context.VariableTypeNames.Keys)
                    if (ContainsIdentifier(statement.Text, variableName) &&
                        plan.VariableOwners.TryGetValue(variableName, out string owner))
                        plan.FinalOwners.Add(owner);
            foreach (SectionPlan section in plan.Sections)
                plan.SectionByName.Add(section.Name, section);
            return plan;
        }

        static IEnumerable<string> ReferencedOwners(OutputPlan plan, StatementPlan statement)
        {
            foreach (KeyValuePair<string, string> variable in plan.VariableOwners)
                if (ContainsIdentifier(statement.Text, variable.Key))
                    yield return variable.Value;
        }

        static bool AssignsLocalVariable(OutputPlan plan, StatementPlan statement)
        {
            string variableName = plan.VariableOwners.Keys
                .Where(value =>
                    statement.Text.StartsWith($"var {value} =", StringComparison.Ordinal) ||
                    statement.Text.StartsWith($"{value} =", StringComparison.Ordinal))
                .OrderByDescending(value => value.Length)
                .FirstOrDefault();
            return variableName != null &&
                plan.VariableOwners.TryGetValue(variableName, out string owner) &&
                string.Equals(owner, statement.SectionName, StringComparison.Ordinal);
        }

        static List<string> OrderSections(
            IEnumerable<string> sectionNames,
            IReadOnlyDictionary<string, SortedSet<string>> dependencies)
        {
            var names = sectionNames
                .Where(value => !string.Equals(value, "Root", StringComparison.Ordinal))
                .ToList();
            var available = new HashSet<string>(names, StringComparer.Ordinal);
            var states = new Dictionary<string, int>(StringComparer.Ordinal);
            var result = new List<string>();

            foreach (string name in names)
                Visit(name);

            return result;

            void Visit(string name)
            {
                if (!available.Contains(name))
                    return;
                if (!states.TryGetValue(name, out int state))
                    state = 0;
                if (state == 2)
                    return;
                if (state == 1)
                    throw new InvalidOperationException($"C# authoring section dependency cycle at '{name}'.");
                states[name] = 1;
                if (dependencies.TryGetValue(name, out SortedSet<string> owners))
                    foreach (string owner in owners)
                    {
                        if (!available.Contains(owner))
                            throw new InvalidOperationException(
                                $"C# authoring section '{name}' depends on missing section '{owner}'.");
                        Visit(owner);
                    }
                states[name] = 2;
                result.Add(name);
            }
        }

        static IEnumerable<string> VariableOrder(BtsmtlAuthoringCodeExportContext context)
        {
            foreach (string variableName in context.ObjectVariableOrder)
                yield return variableName;
            foreach (string variableName in context.LocalVariableOrder)
                yield return variableName;
            foreach (BtsmtlAuthoringCodeExternalAssetReference asset in context.ExternalAssets)
                yield return asset.VariableName;
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

        static string BuildRootFile(
            BtsmtlAuthoringCodeExportContext context,
            OutputPlan plan)
        {
            var writer = new SourceWriter();
            WriteHeader(writer, context);
            writer.WriteLine(
                $"public sealed partial class {context.Request.EntryTypeName} : IBtsmtlAuthoringGenerationEntry");
            writer.OpenBlock();
            writer.WriteLine(
                "public BtsmtlAuthoringGenerationResult Execute(BtsmtlAuthoringGenerationContext context)");
            writer.OpenBlock();
            writer.WriteLine("var rootParts = BuildRoot(context);");
            foreach (SectionPlan section in plan.Sections)
            {
                if (string.Equals(section.Name, "Root", StringComparison.Ordinal) || !section.HasStatements)
                    continue;
                var arguments = new List<string>();
                foreach (string dependency in section.Dependencies)
                {
                    SectionPlan dependencySection = plan.SectionByName[dependency];
                    if (dependencySection.HasResult)
                        arguments.Add(dependencySection.ResultParameterName);
                }
                if (section.UsesRoot)
                    arguments.Add("rootParts");
                arguments.Add("context");
                writer.WriteLine(
                    section.HasResult
                        ? $"var {section.ResultParameterName} = Build{SectionIdentifier(section.Name)}({string.Join(", ", arguments)});"
                        : $"Build{SectionIdentifier(section.Name)}({string.Join(", ", arguments)});");
            }
            if (plan.FinalStatements.Count != 0)
                writer.WriteLine($"FinalizeAuthoring({string.Join(", ", FinalArguments(plan))});");
            writer.WriteLine($"return context.Complete(rootParts.{context.RootVariableName});");
            writer.CloseBlock();
            writer.CloseBlock();
            writer.CloseBlock();
            return writer.ToString();
        }

        static string BuildRootSectionFile(
            BtsmtlAuthoringCodeExportContext context,
            OutputPlan plan)
        {
            var writer = new SourceWriter();
            WriteHeader(writer, context);
            writer.WriteLine($"public sealed partial class {context.Request.EntryTypeName}");
            writer.OpenBlock();
            writer.WriteLine();
            writer.WriteLine($"static {plan.Root.ResultTypeName} BuildRoot(BtsmtlAuthoringGenerationContext context)");
            writer.OpenBlock();
            writer.WriteLine($"var parts = new {plan.Root.ResultTypeName}();");
            WriteSharedAssets(writer, context, plan);
            WriteLocalAssets(writer, plan.Root);
            WriteStatements(writer, context, plan, plan.Root, false);
            writer.WriteLine("return parts;");
            writer.CloseBlock();
            if (plan.FinalStatements.Count != 0)
            {
                writer.WriteLine();
                WriteFinalizer(writer, context, plan);
            }
            writer.WriteLine();
            WriteResultType(writer, context, plan.Root);
            writer.CloseBlock();
            writer.CloseBlock();
            return writer.ToString();
        }

        static string BuildSectionFile(
            BtsmtlAuthoringCodeExportContext context,
            OutputPlan plan,
            SectionPlan section)
        {
            var writer = new SourceWriter();
            WriteHeader(writer, context);
            writer.WriteLine($"public sealed partial class {context.Request.EntryTypeName}");
            writer.OpenBlock();
            string resultType = section.HasResult ? section.ResultTypeName : "void";
            var parameters = new List<string>();
            foreach (string dependency in section.Dependencies)
            {
                SectionPlan dependencySection = plan.SectionByName[dependency];
                if (dependencySection.HasResult)
                    parameters.Add($"{dependencySection.ResultTypeName} {dependencySection.ResultParameterName}");
            }
            if (section.UsesRoot)
                parameters.Add($"{plan.Root.ResultTypeName} rootParts");
            parameters.Add("BtsmtlAuthoringGenerationContext context");
            writer.WriteLine(
                $"static {resultType} Build{SectionIdentifier(section.Name)}({string.Join(", ", parameters)})");
            writer.OpenBlock();
            if (section.HasResult)
                writer.WriteLine($"var parts = new {section.ResultTypeName}();");
            WriteLocalAssets(writer, section);
            WriteStatements(writer, context, plan, section, false);
            if (section.HasResult)
                writer.WriteLine("return parts;");
            writer.CloseBlock();
            if (section.HasResult)
            {
                writer.WriteLine();
                WriteResultType(writer, context, section);
            }
            writer.CloseBlock();
            writer.CloseBlock();
            return writer.ToString();
        }

        static void WriteFinalizer(
            SourceWriter writer,
            BtsmtlAuthoringCodeExportContext context,
            OutputPlan plan)
        {
            var parameters = new List<string>();
            foreach (SectionPlan section in plan.Sections)
                if (plan.FinalOwners.Contains(section.Name) && section.HasResult)
                    parameters.Add($"{section.ResultTypeName} {section.ResultParameterName}");
            parameters.Add("BtsmtlAuthoringGenerationContext context");
            writer.WriteLine($"static void FinalizeAuthoring({string.Join(", ", parameters)})");
            writer.OpenBlock();
            foreach (StatementPlan statement in plan.FinalStatements)
                writer.WriteLine(
                    RewriteStatement(
                        context,
                        plan,
                        statement.Text,
                        statement.SectionName,
                        null,
                        true));
            writer.CloseBlock();
        }

        static IEnumerable<string> FinalArguments(OutputPlan plan)
        {
            foreach (SectionPlan section in plan.Sections)
                if (plan.FinalOwners.Contains(section.Name) && section.HasResult)
                    yield return section.ResultParameterName;
            yield return "context";
        }

        static void WriteSharedAssets(
            SourceWriter writer,
            BtsmtlAuthoringCodeExportContext context,
            OutputPlan plan)
        {
            foreach (BtsmtlAuthoringCodeExternalAssetReference asset in context.ExternalAssets)
                if (plan.SharedAssets.Contains(asset.VariableName))
                    writer.WriteLine(
                        $"parts.{asset.VariableName} = context.ResolveExternalAsset<{asset.TypeName}>({BtsmtlAuthoringCodeSyntax.StringLiteral(asset.AssetPath)}, {asset.LocalFileId}L);");
        }

        static void WriteLocalAssets(
            SourceWriter writer,
            SectionPlan section)
        {
            foreach (BtsmtlAuthoringCodeExternalAssetReference asset in section.LocalAssets)
                writer.WriteLine(
                    $"var {asset.VariableName} = context.ResolveExternalAsset<{asset.TypeName}>({BtsmtlAuthoringCodeSyntax.StringLiteral(asset.AssetPath)}, {asset.LocalFileId}L);");
        }

        static void WriteStatements(
            SourceWriter writer,
            BtsmtlAuthoringCodeExportContext context,
            OutputPlan plan,
            SectionPlan section,
            bool final)
        {
            foreach (StatementPlan statement in plan.Statements)
            {
                if (statement.Deferred != final ||
                    !string.Equals(statement.SectionName, section.Name, StringComparison.Ordinal))
                    continue;
                writer.WriteLine(
                    RewriteStatement(
                        context,
                        plan,
                        statement.Text,
                        section.Name,
                        "parts",
                        final));
            }
        }

        static void WriteResultType(
            SourceWriter writer,
            BtsmtlAuthoringCodeExportContext context,
            SectionPlan section)
        {
            writer.WriteLine($"sealed class {section.ResultTypeName}");
            writer.OpenBlock();
            foreach (string variableName in section.PromotedVariables)
                writer.WriteLine($"internal {context.VariableTypeNames[variableName]} {variableName};");
            writer.CloseBlock();
        }

        static string SectionIdentifier(string sectionName) =>
            BtsmtlAuthoringCodeSyntax.Identifier(sectionName.Replace('/', '_'));

        static string RewriteStatement(
            BtsmtlAuthoringCodeExportContext context,
            OutputPlan plan,
            string statement,
            string sectionName,
            string localResultName,
            bool final)
        {
            string result = statement;
            string declaredVariable = context.VariableTypeNames.Keys
                .Where(value => result.StartsWith($"var {value} =", StringComparison.Ordinal))
                .OrderByDescending(value => value.Length)
                .FirstOrDefault();
            if (!string.IsNullOrEmpty(declaredVariable))
            {
                string expression = VariableReference(
                    plan,
                    declaredVariable,
                    sectionName,
                    localResultName,
                    final);
                if (!string.Equals(expression, declaredVariable, StringComparison.Ordinal))
                {
                    string prefix = $"var {declaredVariable} =";
                    result = expression + " =" + result.Substring(prefix.Length);
                }
            }
            foreach (string variableName in context.VariableTypeNames.Keys
                         .OrderByDescending(value => value.Length))
            {
                if (string.Equals(variableName, declaredVariable, StringComparison.Ordinal))
                    continue;
                string expression = VariableReference(
                    plan,
                    variableName,
                    sectionName,
                    localResultName,
                    final);
                if (!string.Equals(expression, variableName, StringComparison.Ordinal))
                    result = ReplaceIdentifier(result, variableName, expression);
            }
            return result;
        }

        static string VariableReference(
            OutputPlan plan,
            string variableName,
            string sectionName,
            string localResultName,
            bool final)
        {
            if (!plan.VariableOwners.TryGetValue(variableName, out string owner))
                return variableName;
            if (!final && string.Equals(owner, sectionName, StringComparison.Ordinal))
                return plan.Promoted.Contains(variableName)
                    ? $"{localResultName}.{variableName}"
                    : variableName;
            if (plan.SectionByName.TryGetValue(owner, out SectionPlan resultSection))
                return $"{resultSection.ResultParameterName}.{variableName}";
            return variableName;
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
                if (character == '"' && !escaped)
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

        static bool ContainsIdentifier(string text, string identifier)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(identifier))
                return false;
            for (int index = 0; index < text.Length; index++)
                if (IsIdentifierStart(text, index, identifier))
                    return true;
            return false;
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

        static string TrimHeader(
            string sourceCode,
            string generatedNamespaceName,
            IReadOnlyDictionary<string, HashSet<string>> namespaceSymbols)
        {
            string[] lines = sourceCode.Replace("\r\n", "\n").Split('\n');
            int namespaceIndex = Array.FindIndex(
                lines,
                value => value.StartsWith("namespace ", StringComparison.Ordinal));
            if (namespaceIndex < 0)
                return sourceCode;
            string body = string.Join("\n", lines.Skip(namespaceIndex));
            HashSet<string> identifiers = Identifiers(body);
            var result = new List<string>();
            for (int i = 0; i < namespaceIndex; i++)
            {
                string line = lines[i];
                if (!line.StartsWith("using ", StringComparison.Ordinal))
                {
                    result.Add(line);
                    continue;
                }
                int aliasIndex = line.IndexOf(" = ", StringComparison.Ordinal);
                if (aliasIndex >= 0)
                {
                    string alias = line.Substring("using ".Length, aliasIndex - "using ".Length);
                    if (identifiers.Contains(alias))
                        result.Add(line);
                    continue;
                }
                string namespaceName = line.Substring("using ".Length).TrimEnd(';');
                if (string.Equals(namespaceName, generatedNamespaceName, StringComparison.Ordinal))
                    continue;
                if (HasNamespaceSymbol(namespaceName, identifiers, namespaceSymbols))
                    result.Add(line);
            }
            result.AddRange(lines.Skip(namespaceIndex));
            return string.Join(Environment.NewLine, result);
        }

        static HashSet<string> Identifiers(string source)
        {
            var result = new HashSet<string>(StringComparer.Ordinal);
            int index = 0;
            while (index < source.Length)
            {
                if (source[index] == '_' ||
                    source[index] >= 'A' && source[index] <= 'Z' ||
                    source[index] >= 'a' && source[index] <= 'z')
                {
                    int start = index++;
                    while (index < source.Length && IsIdentifierCharacter(source[index]))
                        index++;
                    result.Add(source.Substring(start, index - start));
                    continue;
                }
                index++;
            }
            return result;
        }

        static bool HasNamespaceSymbol(
            string namespaceName,
            ISet<string> identifiers,
            IReadOnlyDictionary<string, HashSet<string>> namespaceSymbols)
        {
            return namespaceSymbols.TryGetValue(namespaceName, out HashSet<string> symbols) &&
                symbols.Overlaps(identifiers);
        }

        static Dictionary<string, HashSet<string>> BuildNamespaceSymbols(
            IEnumerable<string> namespaceNames)
        {
            var result = namespaceNames.ToDictionary(
                value => value,
                value => new HashSet<string>(StringComparer.Ordinal),
                StringComparer.Ordinal);
            foreach (System.Reflection.Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                System.Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (System.Reflection.ReflectionTypeLoadException error)
                {
                    types = error.Types;
                }
                foreach (System.Type type in types)
                {
                    if (type == null ||
                        !result.TryGetValue(type.Namespace ?? string.Empty, out HashSet<string> symbols))
                        continue;
                    string name = type.Name;
                    int tick = name.IndexOf('`');
                    if (tick >= 0)
                        name = name.Substring(0, tick);
                    symbols.Add(name);
                }
            }
            return result;
        }

        sealed class OutputPlan
        {
            public List<StatementPlan> Statements { get; } = new();
            public List<StatementPlan> FinalStatements { get; } = new();
            public List<SectionPlan> Sections { get; } = new();
            public Dictionary<string, SectionPlan> SectionByName { get; } = new(StringComparer.Ordinal);
            public Dictionary<string, string> VariableOwners { get; } = new(StringComparer.Ordinal);
            public HashSet<string> Promoted { get; } = new(StringComparer.Ordinal);
            public HashSet<string> SharedAssets { get; } = new(StringComparer.Ordinal);
            public HashSet<string> FinalOwners { get; } = new(StringComparer.Ordinal);
            public SectionPlan Root { get; set; }
        }

        sealed class SectionPlan
        {
            public SectionPlan(string name)
            {
                Name = name;
                ResultTypeName = string.Equals(name, "Root", StringComparison.Ordinal)
                    ? "RootParts"
                    : SectionIdentifier(name) + "Parts";
                ResultParameterName = string.Equals(name, "Root", StringComparison.Ordinal)
                    ? "rootParts"
                    : LowerFirst(SectionIdentifier(name));
            }

            public string Name { get; }
            public string ResultTypeName { get; }
            public string ResultParameterName { get; }
            public bool HasStatements { get; set; }
            public bool UsesRoot { get; set; }
            public bool HasResult => PromotedVariables.Count != 0;
            public List<string> Dependencies { get; } = new();
            public List<string> PromotedVariables { get; } = new();
            public List<BtsmtlAuthoringCodeExternalAssetReference> LocalAssets { get; } = new();

            static string LowerFirst(string value) =>
                string.IsNullOrEmpty(value)
                    ? "parts"
                    : char.ToLowerInvariant(value[0]) + value.Substring(1);
        }

        sealed class StatementPlan
        {
            public StatementPlan(
                BtsmtlAuthoringCodeEmissionPhase phase,
                string sectionName,
                string text,
                bool canRunWithSectionDependencies)
            {
                Phase = phase;
                SectionName = sectionName;
                Text = text;
                CanRunWithSectionDependencies = canRunWithSectionDependencies;
            }

            public BtsmtlAuthoringCodeEmissionPhase Phase { get; }
            public string SectionName { get; }
            public string Text { get; }
            public bool CanRunWithSectionDependencies { get; }
            public bool Deferred { get; set; }
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
