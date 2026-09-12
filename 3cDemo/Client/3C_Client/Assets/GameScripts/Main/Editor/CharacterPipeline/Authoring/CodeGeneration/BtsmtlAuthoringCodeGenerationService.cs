using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
                return context.CreateResult(string.Empty);

            return context.CreateResult(BtsmtlAuthoringCodeSourceBuilder.Build(context));
        }

        static BtsmtlAuthoringCodeExportResult Failure(
            BtsmtlAuthoringCodeExportRequest request,
            BtsmtlAuthoringCodeDiagnostic diagnostic) =>
            new(
                false,
                request.OutputCodePath,
                request.DefinitionAssetPath,
                request.RecipeType,
                request.EntryTypeName,
                string.Empty,
                Array.Empty<BtsmtlAuthoringCodeExternalDependency>(),
                new[] { diagnostic });
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
            if (!string.Equals(request.RecipeType, entry.RecipeType, StringComparison.Ordinal) ||
                !string.Equals(request.EntryTypeName, entry.EntryTypeName, StringComparison.Ordinal) ||
                !string.Equals(request.SourceCodePath, entry.SourceCodePath, StringComparison.Ordinal))
                return BtsmtlAuthoringGenerationResult.Failure(
                    request.OutputAssetPath,
                    new BtsmtlAuthoringCodeDiagnostic(
                        BtsmtlAuthoringCodeDiagnosticSeverity.Error,
                        "generation_entry_mismatch",
                        request.EntryTypeName,
                        "请求的recipe或入口类型与已编译正式入口不一致。"));
            if (!string.Equals(request.SourceCodePath, context.SourceCodePath, StringComparison.Ordinal) ||
                !string.Equals(request.RecipeType, context.RecipeType, StringComparison.Ordinal) ||
                !string.Equals(request.DefinitionAssetPath, context.DefinitionAssetPath, StringComparison.Ordinal) ||
                !string.Equals(request.OutputAssetPath, context.OutputAssetPath, StringComparison.Ordinal))
                return BtsmtlAuthoringGenerationResult.Failure(
                    request.OutputAssetPath,
                    new BtsmtlAuthoringCodeDiagnostic(
                        BtsmtlAuthoringCodeDiagnosticSeverity.Error,
                        "generation_context_mismatch",
                        request.EntryTypeName,
                        "生成请求与正式生成上下文的精确路径或recipe不一致。"));

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
    }

    internal static class BtsmtlAuthoringCodeSourceBuilder
    {
        public static string Build(BtsmtlAuthoringCodeExportContext context)
        {
            var writer = new SourceWriter();
            writer.WriteLine("using System;");
            writer.WriteLine("using ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration;");
            foreach (string namespaceName in context.Usings.OrderBy(value => value, StringComparer.Ordinal))
                writer.WriteLine($"using {namespaceName};");
            writer.WriteLine();
            writer.WriteLine($"namespace {context.Request.NamespaceName}");
            writer.OpenBlock();
            writer.WriteLine(
                $"public sealed class {context.Request.EntryTypeName} : IBtsmtlAuthoringGenerationEntry");
            writer.OpenBlock();
            writer.WriteLine(
                $"public string RecipeType => {BtsmtlAuthoringCodeSyntax.StringLiteral(context.Request.RecipeType)};");
            writer.WriteLine(
                $"public string EntryTypeName => typeof({context.Request.EntryTypeName}).FullName;");
            writer.WriteLine(
                $"public string SourceCodePath => {BtsmtlAuthoringCodeSyntax.StringLiteral(context.Request.OutputCodePath)};");
            writer.WriteLine(
                "public BtsmtlAuthoringGenerationResult Execute(BtsmtlAuthoringGenerationContext context)");
            writer.OpenBlock();
            bool wrotePhase = false;
            foreach (BtsmtlAuthoringCodeEmissionPhase phase in Enum.GetValues(typeof(BtsmtlAuthoringCodeEmissionPhase)))
            {
                IReadOnlyList<string> statements = context.Statements(phase);
                if (statements.Count == 0)
                    continue;
                if (wrotePhase)
                    writer.WriteLine();
                foreach (string statement in statements)
                    writer.WriteLine(statement);
                wrotePhase = true;
            }
            writer.WriteLine($"return context.Complete({context.RootVariableName});");
            writer.CloseBlock();
            writer.CloseBlock();
            writer.CloseBlock();
            return writer.ToString();
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
