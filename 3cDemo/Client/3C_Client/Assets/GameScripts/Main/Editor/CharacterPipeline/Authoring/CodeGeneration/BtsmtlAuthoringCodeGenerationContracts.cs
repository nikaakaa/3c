using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public enum BtsmtlAuthoringCodeEmissionPhase : byte
    {
        Create,
        Configure,
        Bind,
        Connect,
        RootBinding
    }

    public enum BtsmtlAuthoringCodeDiagnosticSeverity : byte
    {
        Error,
        Warning
    }

    public readonly struct BtsmtlAuthoringCodeDiagnostic
    {
        public BtsmtlAuthoringCodeDiagnostic(
            BtsmtlAuthoringCodeDiagnosticSeverity severity,
            string code,
            string subject,
            string message,
            string suggestion = null)
        {
            Severity = severity;
            Code = code ?? string.Empty;
            Subject = subject ?? string.Empty;
            Message = message ?? string.Empty;
            Suggestion = suggestion ?? string.Empty;
        }

        public BtsmtlAuthoringCodeDiagnosticSeverity Severity { get; }
        public string Code { get; }
        public string Subject { get; }
        public string Message { get; }
        public string Suggestion { get; }

        public override string ToString() =>
            string.IsNullOrEmpty(Subject)
                ? $"{Code}: {Message}"
                : $"{Code} ({Subject}): {Message}";
    }

    public readonly struct BtsmtlAuthoringCodeExternalDependency
    {
        public BtsmtlAuthoringCodeExternalDependency(string assetPath, string typeName)
        {
            AssetPath = assetPath ?? string.Empty;
            TypeName = typeName ?? string.Empty;
        }

        public string AssetPath { get; }
        public string TypeName { get; }
    }

    public interface IBtsmtlAuthoringCodeDomainAdapter
    {
        string DomainId { get; }
        bool CanHandle(object root);
        void Emit(BtsmtlAuthoringCodeExportContext context, object root);
    }

    public sealed class BtsmtlAuthoringCodeExportRequest
    {
        public BtsmtlAuthoringCodeExportRequest(
            object root,
            string definitionAssetPath,
            string outputCodePath,
            string recipeType,
            string namespaceName,
            string entryTypeName)
        {
            Root = root ?? throw new ArgumentNullException(nameof(root));
            DefinitionAssetPath = RequirePath(definitionAssetPath, nameof(definitionAssetPath));
            OutputCodePath = RequirePath(outputCodePath, nameof(outputCodePath));
            RecipeType = RequireValue(recipeType, nameof(recipeType));
            NamespaceName = RequireQualifiedIdentifier(namespaceName, nameof(namespaceName));
            EntryTypeName = RequireIdentifier(entryTypeName, nameof(entryTypeName));
        }

        public object Root { get; }
        public string DefinitionAssetPath { get; }
        public string OutputCodePath { get; }
        public string RecipeType { get; }
        public string NamespaceName { get; }
        public string EntryTypeName { get; }

        static string RequirePath(string value, string name) => RequireValue(value, name);

        static string RequireValue(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Value is required.", name);
            return value;
        }

        static string RequireIdentifier(string value, string name)
        {
            value = RequireValue(value, name);
            if (!BtsmtlAuthoringCodeSyntax.IsIdentifier(value))
                throw new ArgumentException("Value must be a C# identifier.", name);
            return value;
        }

        static string RequireQualifiedIdentifier(string value, string name)
        {
            value = RequireValue(value, name);
            if (!BtsmtlAuthoringCodeSyntax.IsQualifiedIdentifier(value))
                throw new ArgumentException("Value must be a qualified C# identifier.", name);
            return value;
        }
    }

    public sealed class BtsmtlAuthoringCodeExportContext
    {
        readonly Dictionary<object, string> m_ObjectVariables = new(ReferenceComparer.Instance);
        readonly Dictionary<object, string> m_ObjectIdentities = new(ReferenceComparer.Instance);
        readonly Dictionary<string, object> m_IdentityOwners = new(StringComparer.Ordinal);
        readonly HashSet<string> m_VariableNames = new(StringComparer.Ordinal);
        readonly HashSet<string> m_Usings = new(StringComparer.Ordinal);
        readonly Dictionary<BtsmtlAuthoringCodeEmissionPhase, List<string>> m_Statements =
            new();
        readonly List<BtsmtlAuthoringCodeDiagnostic> m_Diagnostics = new();
        readonly List<BtsmtlAuthoringCodeExternalDependency> m_ExternalDependencies = new();
        readonly HashSet<string> m_ExternalDependencyKeys = new(StringComparer.Ordinal);

        internal BtsmtlAuthoringCodeExportContext(BtsmtlAuthoringCodeExportRequest request)
        {
            Request = request;
            foreach (BtsmtlAuthoringCodeEmissionPhase phase in Enum.GetValues(typeof(BtsmtlAuthoringCodeEmissionPhase)))
                m_Statements.Add(phase, new List<string>());
        }

        public BtsmtlAuthoringCodeExportRequest Request { get; }
        public object Root => Request.Root;
        public string DefinitionAssetPath => Request.DefinitionAssetPath;
        public string OutputCodePath => Request.OutputCodePath;
        public bool HasErrors => m_Diagnostics.Any(value => value.Severity == BtsmtlAuthoringCodeDiagnosticSeverity.Error);
        public string RootVariableName { get; private set; }

        public string RegisterObject(
            object source,
            string authoringIdentity,
            string variableHint,
            bool isRoot = false)
        {
            if (source == null)
            {
                ReportError("null_authoring_object", string.Empty, "正式对象不能为空。");
                return string.Empty;
            }
            if (string.IsNullOrWhiteSpace(authoringIdentity))
            {
                ReportError("authoring_identity_missing", source.GetType().FullName, "正式对象缺少稳定作者身份。");
                return string.Empty;
            }
            if (m_ObjectVariables.TryGetValue(source, out string existingVariable))
            {
                if (!string.Equals(m_ObjectIdentities[source], authoringIdentity, StringComparison.Ordinal))
                    ReportError("authoring_identity_changed", authoringIdentity, "同一正式对象在一次导出中出现了不同的稳定作者身份。");
                if (m_IdentityOwners.TryGetValue(authoringIdentity, out object existingOwner) &&
                    !ReferenceEquals(existingOwner, source))
                    ReportError("authoring_identity_duplicate", authoringIdentity, "同一稳定作者身份对应了多个正式对象。");
                if (isRoot)
                    RootVariableName = existingVariable;
                return existingVariable;
            }
            if (m_IdentityOwners.TryGetValue(authoringIdentity, out object identityOwner) &&
                !ReferenceEquals(identityOwner, source))
            {
                ReportError("authoring_identity_duplicate", authoringIdentity, "同一稳定作者身份对应了多个正式对象。");
                return string.Empty;
            }

            string variableName = AllocateVariableName(variableHint, authoringIdentity);
            m_ObjectVariables.Add(source, variableName);
            m_ObjectIdentities.Add(source, authoringIdentity);
            m_IdentityOwners.Add(authoringIdentity, source);
            if (isRoot)
                RootVariableName = variableName;
            return variableName;
        }

        public bool TryGetVariable(object source, out string variableName) =>
            source != null && m_ObjectVariables.TryGetValue(source, out variableName);

        public string RequireVariable(object source, string subject)
        {
            if (TryGetVariable(source, out string variableName))
                return variableName;
            ReportError(
                "authoring_object_not_registered",
                subject,
                "正式对象尚未注册为代码对象变量。",
                "先收集对象身份，再输出引用或连接。");
            return string.Empty;
        }

        public void AddUsing(string namespaceName)
        {
            if (string.IsNullOrWhiteSpace(namespaceName) ||
                !BtsmtlAuthoringCodeSyntax.IsQualifiedIdentifier(namespaceName))
            {
                ReportError("invalid_using", namespaceName, "输出代码的using名称不是合法的限定标识符。");
                return;
            }
            m_Usings.Add(namespaceName);
        }

        public void AddExternalDependency(string assetPath, string typeName)
        {
            if (string.IsNullOrWhiteSpace(assetPath) || string.IsNullOrWhiteSpace(typeName))
            {
                ReportError("external_dependency_missing", assetPath, "外部资源依赖必须包含路径和正式类型。");
                return;
            }
            string key = $"{typeName}\n{assetPath}";
            if (!m_ExternalDependencyKeys.Add(key))
                return;
            m_ExternalDependencies.Add(new BtsmtlAuthoringCodeExternalDependency(assetPath, typeName));
        }

        public void AddStatement(BtsmtlAuthoringCodeEmissionPhase phase, string statement)
        {
            if (string.IsNullOrWhiteSpace(statement))
            {
                ReportError("empty_authoring_statement", phase.ToString(), "代码输出语句不能为空。");
                return;
            }
            if (statement.IndexOfAny(new[] { '\r', '\n' }) >= 0)
            {
                ReportError("multiline_authoring_statement", phase.ToString(), "单条代码输出语句不能包含换行。");
                return;
            }
            m_Statements[phase].Add(statement);
        }

        public void ReportError(string code, string subject, string message, string suggestion = null) =>
            Report(BtsmtlAuthoringCodeDiagnosticSeverity.Error, code, subject, message, suggestion);

        public void ReportWarning(string code, string subject, string message, string suggestion = null) =>
            Report(BtsmtlAuthoringCodeDiagnosticSeverity.Warning, code, subject, message, suggestion);

        public void Report(
            BtsmtlAuthoringCodeDiagnosticSeverity severity,
            string code,
            string subject,
            string message,
            string suggestion = null) =>
            m_Diagnostics.Add(new BtsmtlAuthoringCodeDiagnostic(severity, code, subject, message, suggestion));

        internal IReadOnlyCollection<string> Usings => m_Usings;

        internal IReadOnlyList<string> Statements(BtsmtlAuthoringCodeEmissionPhase phase) => m_Statements[phase];

        internal BtsmtlAuthoringCodeExportResult CreateResult(string sourceCode) =>
            new(
                !HasErrors,
                Request.OutputCodePath,
                Request.DefinitionAssetPath,
                Request.RecipeType,
                Request.EntryTypeName,
                sourceCode ?? string.Empty,
                new ReadOnlyCollection<BtsmtlAuthoringCodeExternalDependency>(m_ExternalDependencies.ToArray()),
                new ReadOnlyCollection<BtsmtlAuthoringCodeDiagnostic>(m_Diagnostics.ToArray()));

        string AllocateVariableName(string variableHint, string authoringIdentity)
        {
            string hint = BtsmtlAuthoringCodeSyntax.Identifier(
                string.IsNullOrWhiteSpace(variableHint) ? "value" : variableHint);
            string identityToken = BtsmtlAuthoringCodeSyntax.IdentityToken(authoringIdentity);
            string baseName = $"{hint}_{identityToken}";
            string candidate = baseName;
            int suffix = 2;
            while (!m_VariableNames.Add(candidate))
                candidate = $"{baseName}_{suffix++}";
            return candidate;
        }

        sealed class ReferenceComparer : IEqualityComparer<object>
        {
            public static readonly ReferenceComparer Instance = new();

            public new bool Equals(object left, object right) => ReferenceEquals(left, right);
            public int GetHashCode(object value) => RuntimeHelpers.GetHashCode(value);
        }
    }

    public sealed class BtsmtlAuthoringCodeExportResult
    {
        internal BtsmtlAuthoringCodeExportResult(
            bool success,
            string outputCodePath,
            string definitionAssetPath,
            string recipeType,
            string entryTypeName,
            string sourceCode,
            IReadOnlyList<BtsmtlAuthoringCodeExternalDependency> externalDependencies,
            IReadOnlyList<BtsmtlAuthoringCodeDiagnostic> diagnostics)
        {
            Success = success;
            OutputCodePath = outputCodePath;
            DefinitionAssetPath = definitionAssetPath;
            RecipeType = recipeType;
            EntryTypeName = entryTypeName;
            SourceCode = sourceCode;
            ExternalDependencies = externalDependencies;
            Diagnostics = diagnostics;
        }

        public bool Success { get; }
        public string OutputCodePath { get; }
        public string DefinitionAssetPath { get; }
        public string RecipeType { get; }
        public string EntryTypeName { get; }
        public string SourceCode { get; }
        public IReadOnlyList<BtsmtlAuthoringCodeExternalDependency> ExternalDependencies { get; }
        public IReadOnlyList<BtsmtlAuthoringCodeDiagnostic> Diagnostics { get; }
    }

    public sealed class BtsmtlAuthoringGenerationRequest
    {
        public BtsmtlAuthoringGenerationRequest(
            string sourceCodePath,
            string recipeType,
            string entryTypeName,
            string definitionAssetPath,
            string outputAssetPath)
        {
            SourceCodePath = Require(sourceCodePath, nameof(sourceCodePath));
            RecipeType = Require(recipeType, nameof(recipeType));
            EntryTypeName = Require(entryTypeName, nameof(entryTypeName));
            DefinitionAssetPath = Require(definitionAssetPath, nameof(definitionAssetPath));
            OutputAssetPath = Require(outputAssetPath, nameof(outputAssetPath));
        }

        public string SourceCodePath { get; }
        public string RecipeType { get; }
        public string EntryTypeName { get; }
        public string DefinitionAssetPath { get; }
        public string OutputAssetPath { get; }

        static string Require(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Value is required.", name);
            return value;
        }
    }

    public abstract class BtsmtlAuthoringGenerationContext
    {
        public abstract string SourceCodePath { get; }
        public abstract string RecipeType { get; }
        public abstract string DefinitionAssetPath { get; }
        public abstract string OutputAssetPath { get; }
        public abstract BtsmtlAuthoringGenerationResult Complete(object rootOutput);
        public abstract BtsmtlAuthoringGenerationResult Fail(BtsmtlAuthoringCodeDiagnostic diagnostic);
    }

    public interface IBtsmtlAuthoringGenerationEntry
    {
        string RecipeType { get; }
        string EntryTypeName { get; }
        BtsmtlAuthoringGenerationResult Execute(BtsmtlAuthoringGenerationContext context);
    }

    public sealed class BtsmtlAuthoringGenerationResult
    {
        public BtsmtlAuthoringGenerationResult(
            bool success,
            object rootOutput,
            string outputAssetPath,
            IEnumerable<string> createdAssetPaths,
            IEnumerable<string> replacedAssetPaths,
            IEnumerable<string> deletedAssetPaths,
            IEnumerable<BtsmtlAuthoringCodeDiagnostic> diagnostics)
        {
            Success = success;
            RootOutput = rootOutput;
            OutputAssetPath = outputAssetPath ?? string.Empty;
            CreatedAssetPaths = new ReadOnlyCollection<string>((createdAssetPaths ?? Enumerable.Empty<string>()).ToArray());
            ReplacedAssetPaths = new ReadOnlyCollection<string>((replacedAssetPaths ?? Enumerable.Empty<string>()).ToArray());
            DeletedAssetPaths = new ReadOnlyCollection<string>((deletedAssetPaths ?? Enumerable.Empty<string>()).ToArray());
            Diagnostics = new ReadOnlyCollection<BtsmtlAuthoringCodeDiagnostic>(
                (diagnostics ?? Enumerable.Empty<BtsmtlAuthoringCodeDiagnostic>()).ToArray());
        }

        public bool Success { get; }
        public object RootOutput { get; }
        public string OutputAssetPath { get; }
        public IReadOnlyList<string> CreatedAssetPaths { get; }
        public IReadOnlyList<string> ReplacedAssetPaths { get; }
        public IReadOnlyList<string> DeletedAssetPaths { get; }
        public IReadOnlyList<BtsmtlAuthoringCodeDiagnostic> Diagnostics { get; }

        public static BtsmtlAuthoringGenerationResult Failure(
            string outputAssetPath,
            BtsmtlAuthoringCodeDiagnostic diagnostic) =>
            new(
                false,
                null,
                outputAssetPath,
                null,
                null,
                null,
                new[] { diagnostic });
    }

    public static class BtsmtlAuthoringCodeSyntax
    {
        static readonly HashSet<string> s_Keywords = new(StringComparer.Ordinal)
        {
            "abstract",
            "as",
            "base",
            "bool",
            "break",
            "byte",
            "case",
            "catch",
            "char",
            "checked",
            "class",
            "const",
            "continue",
            "decimal",
            "default",
            "delegate",
            "do",
            "double",
            "else",
            "enum",
            "event",
            "explicit",
            "extern",
            "false",
            "finally",
            "fixed",
            "float",
            "for",
            "foreach",
            "goto",
            "if",
            "implicit",
            "in",
            "int",
            "interface",
            "internal",
            "is",
            "lock",
            "long",
            "namespace",
            "new",
            "null",
            "object",
            "operator",
            "out",
            "override",
            "params",
            "private",
            "protected",
            "public",
            "readonly",
            "ref",
            "return",
            "sbyte",
            "sealed",
            "short",
            "sizeof",
            "stackalloc",
            "static",
            "string",
            "struct",
            "switch",
            "this",
            "throw",
            "true",
            "try",
            "typeof",
            "uint",
            "ulong",
            "unchecked",
            "unsafe",
            "ushort",
            "using",
            "virtual",
            "void",
            "volatile",
            "while",
            "add",
            "alias",
            "and",
            "args",
            "async",
            "await",
            "by",
            "descending",
            "dynamic",
            "equals",
            "file",
            "from",
            "get",
            "global",
            "group",
            "init",
            "into",
            "join",
            "let",
            "managed",
            "nameof",
            "nint",
            "not",
            "notnull",
            "on",
            "or",
            "orderby",
            "partial",
            "record",
            "remove",
            "required",
            "scoped",
            "select",
            "set",
            "unmanaged",
            "value",
            "var",
            "when",
            "where",
            "with",
            "yield"
        };

        public static bool IsIdentifier(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;
            if (s_Keywords.Contains(value))
                return false;
            if (!(value[0] == '_' || IsAsciiLetter(value[0])))
                return false;
            for (int i = 1; i < value.Length; i++)
            {
                char character = value[i];
                if (!(character == '_' || IsAsciiLetter(character) || IsAsciiDigit(character)))
                    return false;
            }
            return true;
        }

        public static bool IsQualifiedIdentifier(string value) =>
            !string.IsNullOrWhiteSpace(value) &&
            value.Split('.').All(IsIdentifier);

        public static string Identifier(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "_";
            var builder = new StringBuilder(value.Length + 1);
            for (int i = 0; i < value.Length; i++)
            {
                char character = value[i];
                builder.Append(character == '_' || IsAsciiLetter(character) || (i > 0 && IsAsciiDigit(character))
                    ? character
                    : '_');
            }
            if (!(builder[0] == '_' || IsAsciiLetter(builder[0])))
                builder.Insert(0, '_');
            if (s_Keywords.Contains(builder.ToString()))
                builder.Insert(0, '_');
            return builder.ToString();
        }

        public static string StringLiteral(string value)
        {
            if (value == null)
                return "null";
            var builder = new StringBuilder(value.Length + 2);
            builder.Append('"');
            foreach (char character in value)
            {
                switch (character)
                {
                    case '\\': builder.Append("\\\\"); break;
                    case '"': builder.Append("\\\""); break;
                    case '\0': builder.Append("\\0"); break;
                    case '\a': builder.Append("\\a"); break;
                    case '\b': builder.Append("\\b"); break;
                    case '\f': builder.Append("\\f"); break;
                    case '\n': builder.Append("\\n"); break;
                    case '\r': builder.Append("\\r"); break;
                    case '\t': builder.Append("\\t"); break;
                    case '\v': builder.Append("\\v"); break;
                    default:
                        if (char.IsControl(character))
                            builder.Append($"\\u{(int)character:x4}");
                        else
                            builder.Append(character);
                        break;
                }
            }
            builder.Append('"');
            return builder.ToString();
        }

        public static string FloatLiteral(float value)
        {
            EnsureFinite(value);
            return value.ToString("R", CultureInfo.InvariantCulture) + "f";
        }

        public static string DoubleLiteral(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                throw new ArgumentException("C# authoring values must be finite.", nameof(value));
            return value.ToString("R", CultureInfo.InvariantCulture) + "d";
        }

        public static string EnumLiteral(string qualifiedTypeName, string memberName)
        {
            if (!IsQualifiedIdentifier(qualifiedTypeName) || !IsIdentifier(memberName))
                throw new ArgumentException("Enum type and member must be C# identifiers.");
            return $"{qualifiedTypeName}.{memberName}";
        }

        internal static string IdentityToken(string identity)
        {
            string normalized = Identifier(identity);
            if (normalized.Length <= 32)
                return normalized;
            using (SHA256 algorithm = SHA256.Create())
            {
                byte[] hash = algorithm.ComputeHash(Encoding.UTF8.GetBytes(identity));
                return BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant().Substring(0, 12);
            }
        }

        static void EnsureFinite(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
                throw new ArgumentException("C# authoring values must be finite.", nameof(value));
        }

        static bool IsAsciiLetter(char value) =>
            value >= 'A' && value <= 'Z' || value >= 'a' && value <= 'z';

        static bool IsAsciiDigit(char value) => value >= '0' && value <= '9';
    }
}
