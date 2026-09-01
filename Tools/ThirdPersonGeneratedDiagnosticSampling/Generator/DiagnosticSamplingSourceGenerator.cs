using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace ThirdPerson.GeneratedDiagnosticSampling.Generator
{
    [Generator]
    public sealed class DiagnosticSamplingSourceGenerator : ISourceGenerator
    {
        const string GeneratorIdentityValue =
            "thirdperson.generated-diagnostic-sampling.generator/1";
        const string CapabilityAttribute =
            "ThirdPerson.GeneratedDiagnosticSampling.DiagnosticCapabilityAttribute";
        const string FieldAttribute =
            "ThirdPerson.GeneratedDiagnosticSampling.DiagnosticFieldAttribute";
        const string SamplerAttribute =
            "ThirdPerson.GeneratedDiagnosticSampling.DiagnosticSamplerAttribute";
        const string ProgramAttribute =
            "ThirdPerson.GeneratedDiagnosticSampling.DiagnosticCaptureProgramAttribute";
        const string TableAttribute =
            "ThirdPerson.GeneratedDiagnosticSampling.DiagnosticTableAttribute";
        const string TableCountAttribute =
            "ThirdPerson.GeneratedDiagnosticSampling.DiagnosticTableCountAttribute";

        static readonly DiagnosticDescriptor s_InvalidProgram = new DiagnosticDescriptor(
            "DGS001",
            "Invalid diagnostic capture program",
            "{0}",
            "GeneratedDiagnosticSampling",
            DiagnosticSeverity.Error,
            true);

        static readonly DiagnosticDescriptor s_InvalidSampler = new DiagnosticDescriptor(
            "DGS002",
            "Invalid diagnostic sampler",
            "{0}",
            "GeneratedDiagnosticSampling",
            DiagnosticSeverity.Error,
            true);

        static readonly DiagnosticDescriptor s_InvalidField = new DiagnosticDescriptor(
            "DGS003",
            "Invalid diagnostic field",
            "{0}",
            "GeneratedDiagnosticSampling",
            DiagnosticSeverity.Error,
            true);

        static readonly DiagnosticDescriptor s_DuplicateIdentity = new DiagnosticDescriptor(
            "DGS004",
            "Duplicate diagnostic identity",
            "{0}",
            "GeneratedDiagnosticSampling",
            DiagnosticSeverity.Error,
            true);

        static readonly DiagnosticDescriptor s_InvalidTable = new DiagnosticDescriptor(
            "DGS005",
            "Invalid diagnostic table",
            "{0}",
            "GeneratedDiagnosticSampling",
            DiagnosticSeverity.Error,
            true);

        public void Initialize(GeneratorInitializationContext context)
        {
        }

        public void Execute(GeneratorExecutionContext context)
        {
            INamedTypeSymbol capabilityAttribute = context.Compilation.GetTypeByMetadataName(
                CapabilityAttribute);
            INamedTypeSymbol fieldAttribute = context.Compilation.GetTypeByMetadataName(FieldAttribute);
            INamedTypeSymbol samplerAttribute = context.Compilation.GetTypeByMetadataName(
                SamplerAttribute);
            INamedTypeSymbol programAttribute = context.Compilation.GetTypeByMetadataName(
                ProgramAttribute);
            INamedTypeSymbol tableAttribute = context.Compilation.GetTypeByMetadataName(TableAttribute);
            INamedTypeSymbol tableCountAttribute = context.Compilation.GetTypeByMetadataName(
                TableCountAttribute);
            if (capabilityAttribute == null ||
                fieldAttribute == null ||
                samplerAttribute == null ||
                programAttribute == null ||
                tableAttribute == null ||
                tableCountAttribute == null)
            {
                return;
            }

            INamedTypeSymbol[] types = EnumerateTypes(context.Compilation.Assembly.GlobalNamespace)
                .ToArray();
            Dictionary<string, CapabilityModel> capabilities = DiscoverCapabilities(
                context,
                types,
                capabilityAttribute);
            Dictionary<INamedTypeSymbol, SamplerModel> samplers = DiscoverSamplers(
                context,
                types,
                samplerAttribute);
            IReadOnlyList<FieldModel> fields = DiscoverFields(context, types, fieldAttribute);
            IReadOnlyDictionary<string, TableModel> tables = DiscoverTables(
                context,
                types,
                tableAttribute,
                tableCountAttribute);

            foreach (INamedTypeSymbol type in types)
            {
                AttributeData attribute = FindAttribute(type, programAttribute);
                if (attribute == null)
                    continue;
                EmitProgram(context, type, attribute, capabilities, samplers, fields, tables);
            }
        }

        static Dictionary<string, CapabilityModel> DiscoverCapabilities(
            GeneratorExecutionContext context,
            IEnumerable<INamedTypeSymbol> types,
            INamedTypeSymbol attributeType)
        {
            var result = new Dictionary<string, CapabilityModel>(StringComparer.Ordinal);
            foreach (INamedTypeSymbol type in types)
            {
                AttributeData attribute = FindAttribute(type, attributeType);
                if (attribute == null || attribute.ConstructorArguments.Length < 3)
                    continue;
                string id = attribute.ConstructorArguments[0].Value as string;
                int revision = (int)attribute.ConstructorArguments[1].Value;
                INamedTypeSymbol viewType = attribute.ConstructorArguments[2].Value as INamedTypeSymbol;
                if (string.IsNullOrWhiteSpace(id) || revision <= 0 || viewType == null)
                {
                    Report(context, s_InvalidProgram, type, $"Capability '{type.Name}' is invalid.");
                    continue;
                }
                if (result.ContainsKey(id))
                {
                    Report(context, s_DuplicateIdentity, type, $"Capability '{id}' is duplicated.");
                    continue;
                }
                result.Add(id, new CapabilityModel(id, revision, viewType));
            }
            return result;
        }

        static Dictionary<INamedTypeSymbol, SamplerModel> DiscoverSamplers(
            GeneratorExecutionContext context,
            IEnumerable<INamedTypeSymbol> types,
            INamedTypeSymbol attributeType)
        {
            var result = new Dictionary<INamedTypeSymbol, SamplerModel>(
                SymbolEqualityComparer.Default);
            foreach (INamedTypeSymbol type in types)
            {
                AttributeData attribute = FindAttribute(type, attributeType);
                if (attribute == null || attribute.ConstructorArguments.Length < 6)
                    continue;
                var model = new SamplerModel(
                    attribute.ConstructorArguments[0].Value as string,
                    attribute.ConstructorArguments[1].Value as string,
                    (int)attribute.ConstructorArguments[2].Value,
                    attribute.ConstructorArguments[3].Value as string,
                    attribute.ConstructorArguments[4].Value as string,
                    Strings(attribute.ConstructorArguments[5]),
                    NamedStrings(attribute, "Fields"),
                    NamedStrings(attribute, "Tables"));
                if (!model.IsValid)
                {
                    Report(context, s_InvalidSampler, type, $"Sampler '{type.Name}' is invalid.");
                    continue;
                }
                result.Add(type, model);
            }
            return result;
        }

        static IReadOnlyList<FieldModel> DiscoverFields(
            GeneratorExecutionContext context,
            IEnumerable<INamedTypeSymbol> types,
            INamedTypeSymbol attributeType)
        {
            var fields = new List<FieldModel>();
            var identities = new HashSet<string>(StringComparer.Ordinal);
            foreach (INamedTypeSymbol type in types)
            {
                foreach (IMethodSymbol method in type.GetMembers().OfType<IMethodSymbol>())
                {
                    AttributeData attribute = FindAttribute(method, attributeType);
                    if (attribute == null || attribute.ConstructorArguments.Length < 7)
                        continue;
                    var model = new FieldModel(
                        attribute.ConstructorArguments[0].Value as string,
                        attribute.ConstructorArguments[1].Value as string,
                        (int)attribute.ConstructorArguments[2].Value,
                        (int)attribute.ConstructorArguments[3].Value,
                        attribute.ConstructorArguments[4].Value as string,
                        attribute.ConstructorArguments[5].Value as string,
                        Strings(attribute.ConstructorArguments[6]),
                        NamedString(attribute, "AvailabilityFieldId"),
                        NamedInt64(attribute, "AvailabilityValue", 1),
                        NamedBoolean(attribute, "Derived"),
                        NamedStrings(attribute, "Dependencies"),
                        method);
                    if (!model.IsValid)
                    {
                        Report(context, s_InvalidField, method, $"Field '{method.Name}' is invalid.");
                        continue;
                    }
                    string identity = model.CapabilityId + "|" + model.Id;
                    if (!identities.Add(identity))
                    {
                        Report(context, s_DuplicateIdentity, method, $"Field '{identity}' is duplicated.");
                        continue;
                    }
                    fields.Add(model);
                }
            }
            return fields;
        }

        static IReadOnlyDictionary<string, TableModel> DiscoverTables(
            GeneratorExecutionContext context,
            IEnumerable<INamedTypeSymbol> types,
            INamedTypeSymbol tableAttribute,
            INamedTypeSymbol countAttribute)
        {
            var result = new Dictionary<string, TableModel>(StringComparer.Ordinal);
            foreach (INamedTypeSymbol type in types)
            {
                AttributeData attribute = FindAttribute(type, tableAttribute);
                if (attribute == null || attribute.ConstructorArguments.Length < 4)
                    continue;
                string capabilityId = attribute.ConstructorArguments[0].Value as string;
                string id = attribute.ConstructorArguments[1].Value as string;
                int revision = (int)attribute.ConstructorArguments[2].Value;
                int capacity = (int)attribute.ConstructorArguments[3].Value;
                IMethodSymbol countMethod = type.GetMembers()
                    .OfType<IMethodSymbol>()
                    .SingleOrDefault(method =>
                    {
                        AttributeData count = FindAttribute(method, countAttribute);
                        return count != null &&
                            count.ConstructorArguments.Length >= 2 &&
                            string.Equals(
                                count.ConstructorArguments[0].Value as string,
                                capabilityId,
                                StringComparison.Ordinal) &&
                            string.Equals(
                                count.ConstructorArguments[1].Value as string,
                                id,
                                StringComparison.Ordinal);
                    });
                var model = new TableModel(
                    capabilityId,
                    id,
                    revision,
                    capacity,
                    countMethod);
                string identity = capabilityId + "|" + id;
                if (!model.IsValid)
                {
                    Report(context, s_InvalidTable, type, $"Table '{identity}' is invalid.");
                    continue;
                }
                if (result.ContainsKey(identity))
                {
                    Report(context, s_DuplicateIdentity, type, $"Table '{identity}' is duplicated.");
                    continue;
                }
                result.Add(identity, model);
            }
            return result;
        }

        static void EmitProgram(
            GeneratorExecutionContext context,
            INamedTypeSymbol programType,
            AttributeData attribute,
            IReadOnlyDictionary<string, CapabilityModel> capabilities,
            IReadOnlyDictionary<INamedTypeSymbol, SamplerModel> samplers,
            IReadOnlyList<FieldModel> fields,
            IReadOnlyDictionary<string, TableModel> tables)
        {
            if (attribute.ConstructorArguments.Length < 3)
            {
                Report(context, s_InvalidProgram, programType, $"Program '{programType.Name}' is invalid.");
                return;
            }
            string programId = attribute.ConstructorArguments[0].Value as string;
            string capabilityId = attribute.ConstructorArguments[1].Value as string;
            if (string.IsNullOrWhiteSpace(programId) ||
                string.IsNullOrWhiteSpace(capabilityId) ||
                !capabilities.TryGetValue(capabilityId, out CapabilityModel capability))
            {
                Report(
                    context,
                    s_InvalidProgram,
                    programType,
                    $"Program '{programType.Name}' references unknown capability '{capabilityId}'.");
                return;
            }
            if (!IsStaticPartial(programType))
            {
                Report(
                    context,
                    s_InvalidProgram,
                    programType,
                    $"Program '{programType.Name}' must be a static partial class.");
                return;
            }

            INamedTypeSymbol[] samplerTypes = Types(attribute.ConstructorArguments[2]);
            var selectedSamplers = new List<SamplerModel>();
            foreach (INamedTypeSymbol samplerType in samplerTypes)
            {
                if (samplerType == null || !samplers.TryGetValue(samplerType, out SamplerModel sampler))
                {
                    Report(
                        context,
                        s_InvalidProgram,
                        programType,
                        $"Program '{programId}' references a type without DiagnosticSamplerAttribute.");
                    return;
                }
                if (!string.Equals(sampler.CapabilityId, capabilityId, StringComparison.Ordinal))
                {
                    Report(
                        context,
                        s_InvalidProgram,
                        programType,
                        $"Program '{programId}' contains sampler '{sampler.Id}' from another capability.");
                    return;
                }
                selectedSamplers.Add(sampler);
            }
            if (selectedSamplers.Count == 0)
            {
                Report(context, s_InvalidProgram, programType, $"Program '{programId}' has no samplers.");
                return;
            }

            string[] selectedTableIds = selectedSamplers
                .SelectMany(value => value.Tables)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            var selectedTables = new List<TableModel>();
            foreach (string tableId in selectedTableIds)
            {
                if (!tables.TryGetValue(capabilityId + "|" + tableId, out TableModel table) ||
                    !ValidateTableCount(table, capability.ViewType))
                {
                    Report(
                        context,
                        s_InvalidTable,
                        programType,
                        $"Program '{programId}' references invalid table '{tableId}'.");
                    return;
                }
                selectedTables.Add(table);
            }

            FieldModel[] capabilityMainFields = fields
                .Where(value => string.Equals(
                    value.CapabilityId,
                    capabilityId,
                    StringComparison.Ordinal))
                .Where(value => string.Equals(value.TableId, "main", StringComparison.Ordinal))
                .ToArray();
            var samplerFields = new Dictionary<SamplerModel, IReadOnlyList<FieldModel>>();
            foreach (SamplerModel sampler in selectedSamplers)
            {
                foreach (string group in sampler.Groups)
                {
                    if (capabilityMainFields.All(value => !value.Groups.Contains(
                            group,
                            StringComparer.Ordinal)))
                    {
                        Report(
                            context,
                            s_InvalidSampler,
                            programType,
                            $"Sampler '{sampler.Id}' references unknown group '{group}'.");
                        return;
                    }
                }
                var groups = new HashSet<string>(sampler.Groups, StringComparer.Ordinal);
                var explicitFields = new HashSet<string>(sampler.Fields, StringComparer.Ordinal);
                List<FieldModel> values = capabilityMainFields
                    .Where(value =>
                        explicitFields.Contains(value.Id) ||
                        value.Groups.Any(groups.Contains))
                    .OrderBy(value => value.Id, StringComparer.Ordinal)
                    .ToList();
                foreach (string fieldId in explicitFields)
                {
                    if (values.All(value => !string.Equals(
                            value.Id,
                            fieldId,
                            StringComparison.Ordinal)))
                    {
                        Report(
                            context,
                            s_InvalidSampler,
                            programType,
                            $"Sampler '{sampler.Id}' references unknown field '{fieldId}'.");
                        return;
                    }
                }
                if (!CloseDependencies(context, programType, programId, capabilityMainFields, values))
                    return;
                if (values.Count == 0 && sampler.Tables.Length == 0)
                {
                    Report(
                        context,
                        s_InvalidSampler,
                        programType,
                        $"Sampler '{sampler.Id}' selects no fields or tables.");
                    return;
                }
                samplerFields.Add(sampler, values);
            }
            List<FieldModel> selectedFields = samplerFields.Values
                .SelectMany(value => value)
                .Distinct()
                .OrderBy(value => value.Id, StringComparer.Ordinal)
                .ToList();
            foreach (FieldModel field in selectedFields)
            {
                if (!ValidateExtractor(field, capability.ViewType))
                {
                    Report(
                        context,
                        s_InvalidField,
                        field.Method,
                        $"Extractor '{field.Method.Name}' does not match capability view or value kind.");
                    return;
                }
            }
            var tableFields = new Dictionary<TableModel, IReadOnlyList<FieldModel>>();
            foreach (TableModel table in selectedTables)
            {
                FieldModel[] values = fields
                    .Where(value => string.Equals(
                        value.CapabilityId,
                        capabilityId,
                        StringComparison.Ordinal))
                    .Where(value => string.Equals(value.TableId, table.Id, StringComparison.Ordinal))
                    .OrderBy(value => value.Id, StringComparer.Ordinal)
                    .ToArray();
                if (values.Length == 0 || values.Any(value => !ValidateTableExtractor(
                        value,
                        capability.ViewType)) ||
                    !ValidateClosedDependencies(
                        context,
                        programType,
                        programId,
                        values))
                {
                    Report(
                        context,
                        s_InvalidTable,
                        programType,
                        $"Table '{table.Id}' has no valid row fields.");
                    return;
                }
                tableFields.Add(table, values);
            }

            string samplerSetIdentity = Hash(selectedSamplers
                .OrderBy(value => value.Id, StringComparer.Ordinal)
                .Select(value => value.Canonical));
            string assemblyIdentity = programType.ContainingAssembly.Identity.ToString();
            string generatorIdentity = GeneratorIdentityValue + "/" +
                typeof(DiagnosticSamplingSourceGenerator).Assembly.ManifestModule.ModuleVersionId
                    .ToString("N");
            string schemaIdentity = Hash(new[]
                {
                    generatorIdentity,
                    assemblyIdentity,
                    capability.Id,
                    capability.Revision.ToString(),
                    programId,
                    samplerSetIdentity
                }
                .Concat(selectedFields.Select(value => value.Canonical))
                .Concat(selectedTables.Select(value => value.Canonical))
                .Concat(tableFields.SelectMany(value => value.Value.Select(field => field.Canonical))));
            string programHash = Hash(new[]
                {
                    "generated-diagnostic-program/1",
                    generatorIdentity,
                    assemblyIdentity,
                    schemaIdentity
                }
                .Concat(selectedFields.Select(value => MethodSource(value.Method)))
                .Concat(selectedTables.Select(value => MethodSource(value.CountMethod)))
                .Concat(tableFields.SelectMany(value => value.Value.Select(
                    field => MethodSource(field.Method)))));
            string layoutIdentity = Hash(new[] { "diagnostic-packet-layout/1", schemaIdentity });
            string source = GenerateSource(
                programType,
                capability,
                programId,
                assemblyIdentity,
                generatorIdentity,
                samplerSetIdentity,
                schemaIdentity,
                programHash,
                layoutIdentity,
                selectedSamplers,
                samplerFields,
                selectedFields,
                selectedTables,
                tableFields);
            context.AddSource(
                programType.Name + ".DiagnosticCapture.g.cs",
                SourceText.From(source, Encoding.UTF8));
        }

        static bool CloseDependencies(
            GeneratorExecutionContext context,
            INamedTypeSymbol programType,
            string programId,
            IReadOnlyList<FieldModel> allFields,
            List<FieldModel> selectedFields)
        {
            bool changed;
            do
            {
                changed = false;
                foreach (FieldModel field in selectedFields.ToArray())
                {
                    IEnumerable<string> dependencyIds = field.Dependencies;
                    if (!string.IsNullOrWhiteSpace(field.AvailabilityFieldId))
                        dependencyIds = dependencyIds.Concat(new[] { field.AvailabilityFieldId });
                    foreach (string dependencyId in dependencyIds.Distinct(StringComparer.Ordinal))
                    {
                        if (selectedFields.Any(value => string.Equals(
                                value.Id,
                                dependencyId,
                                StringComparison.Ordinal)))
                        {
                            continue;
                        }
                        FieldModel dependency = allFields.SingleOrDefault(value =>
                            string.Equals(value.CapabilityId, field.CapabilityId, StringComparison.Ordinal) &&
                            string.Equals(value.TableId, field.TableId, StringComparison.Ordinal) &&
                            string.Equals(value.Id, dependencyId, StringComparison.Ordinal));
                        if (dependency == null)
                        {
                            Report(
                                context,
                                s_InvalidProgram,
                                programType,
                                $"Program '{programId}' has unknown dependency field '{dependencyId}'.");
                            return false;
                        }
                        selectedFields.Add(dependency);
                        changed = true;
                    }
                }
            }
            while (changed);
            selectedFields.Sort((left, right) => StringComparer.Ordinal.Compare(left.Id, right.Id));
            return ValidateClosedDependencies(
                context,
                programType,
                programId,
                selectedFields);
        }

        static bool ValidateClosedDependencies(
            GeneratorExecutionContext context,
            INamedTypeSymbol programType,
            string programId,
            IReadOnlyList<FieldModel> fields)
        {
            foreach (FieldModel field in fields)
            {
                foreach (string dependencyId in field.Dependencies)
                {
                    if (fields.All(value => !string.Equals(
                            value.Id,
                            dependencyId,
                            StringComparison.Ordinal)))
                    {
                        Report(
                            context,
                            s_InvalidProgram,
                            programType,
                            $"Program '{programId}' has unknown dependency field '{dependencyId}'.");
                        return false;
                    }
                }
                if (string.IsNullOrWhiteSpace(field.AvailabilityFieldId))
                    continue;
                FieldModel availability = fields.SingleOrDefault(value => string.Equals(
                    value.Id,
                    field.AvailabilityFieldId,
                    StringComparison.Ordinal));
                if (availability == null ||
                    availability.ValueKind < 1 ||
                    availability.ValueKind > 5 ||
                    !IsAvailabilityValueValid(
                        availability.ValueKind,
                        field.AvailabilityValue))
                {
                    Report(
                        context,
                        s_InvalidProgram,
                        programType,
                        $"Program '{programId}' has invalid availability field '{field.AvailabilityFieldId}'.");
                    return false;
                }
            }
            if (HasDependencyCycle(fields))
            {
                Report(
                    context,
                    s_InvalidProgram,
                    programType,
                    $"Program '{programId}' has a derived field dependency cycle.");
                return false;
            }
            return true;
        }

        static bool IsAvailabilityValueValid(int valueKind, long value)
        {
            switch (valueKind)
            {
                case 1: return value == 0 || value == 1;
                case 2: return value >= int.MinValue && value <= int.MaxValue;
                case 3: return value >= 0 && value <= uint.MaxValue;
                case 4: return true;
                case 5: return value >= 0;
                default: return false;
            }
        }

        static bool HasDependencyCycle(IReadOnlyList<FieldModel> fields)
        {
            Dictionary<string, FieldModel> map = fields.ToDictionary(value => value.Id, StringComparer.Ordinal);
            var visiting = new HashSet<string>(StringComparer.Ordinal);
            var visited = new HashSet<string>(StringComparer.Ordinal);
            foreach (FieldModel field in fields)
            {
                if (Visit(field, map, visiting, visited))
                    return true;
            }
            return false;
        }

        static bool Visit(
            FieldModel field,
            IReadOnlyDictionary<string, FieldModel> fields,
            ISet<string> visiting,
            ISet<string> visited)
        {
            if (visited.Contains(field.Id))
                return false;
            if (!visiting.Add(field.Id))
                return true;
            foreach (string dependencyId in field.Dependencies)
            {
                if (fields.TryGetValue(dependencyId, out FieldModel dependency) &&
                    Visit(dependency, fields, visiting, visited))
                {
                    return true;
                }
            }
            visiting.Remove(field.Id);
            visited.Add(field.Id);
            return false;
        }

        static string GenerateSource(
            INamedTypeSymbol programType,
            CapabilityModel capability,
            string programId,
            string assemblyIdentity,
            string generatorIdentity,
            string samplerSetIdentity,
            string schemaIdentity,
            string programHash,
            string layoutIdentity,
            IReadOnlyList<SamplerModel> samplers,
            IReadOnlyDictionary<SamplerModel, IReadOnlyList<FieldModel>> samplerFields,
            IReadOnlyList<FieldModel> fields,
            IReadOnlyList<TableModel> tables,
            IReadOnlyDictionary<TableModel, IReadOnlyList<FieldModel>> tableFields)
        {
            var counts = new int[13];
            var handles = new Dictionary<FieldModel, int>();
            foreach (FieldModel field in fields)
                handles.Add(field, counts[field.ValueKind]++);
            var fieldPositions = new Dictionary<FieldModel, int>();
            for (int i = 0; i < fields.Count; i++)
                fieldPositions.Add(fields[i], i);
            var tableHandles = new Dictionary<TableModel, IReadOnlyDictionary<FieldModel, int>>();
            var tablePositions = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int tableIndex = 0; tableIndex < tables.Count; tableIndex++)
            {
                TableModel table = tables[tableIndex];
                tablePositions.Add(table.Id, tableIndex);
                var rowCounts = new int[13];
                var rowHandles = new Dictionary<FieldModel, int>();
                foreach (FieldModel field in tableFields[table])
                    rowHandles.Add(field, rowCounts[field.ValueKind]++);
                tableHandles.Add(table, rowHandles);
            }

            string namespaceName = programType.ContainingNamespace.IsGlobalNamespace
                ? string.Empty
                : programType.ContainingNamespace.ToDisplayString();
            var builder = new StringBuilder();
            builder.AppendLine("// <auto-generated />");
            if (namespaceName.Length > 0)
            {
                builder.Append("namespace ").Append(namespaceName).AppendLine();
                builder.AppendLine("{");
            }
            builder.Append("static partial class ").Append(programType.Name).AppendLine();
            builder.AppendLine("{");
            builder.Append("    public const string DiagnosticCapabilityId = \"")
                .Append(Escape(capability.Id)).AppendLine("\";");
            builder.Append("    public const int DiagnosticCapabilityRevision = ")
                .Append(capability.Revision).AppendLine(";");
            builder.Append("    public const string DiagnosticProgramId = \"")
                .Append(Escape(programId)).AppendLine("\";");
            builder.Append("    public const string DiagnosticAssemblyIdentity = \"")
                .Append(Escape(assemblyIdentity)).AppendLine("\";");
            builder.Append("    public const string DiagnosticGeneratorIdentity = \"")
                .Append(generatorIdentity).AppendLine("\";");
            builder.Append("    public const string DiagnosticSamplerSetIdentity = \"")
                .Append(samplerSetIdentity).AppendLine("\";");
            builder.Append("    public const string DiagnosticSchemaIdentity = \"")
                .Append(schemaIdentity).AppendLine("\";");
            builder.Append("    public const string DiagnosticGeneratedProgramHash = \"")
                .Append(programHash).AppendLine("\";");
            builder.Append("    public const string DiagnosticPacketLayoutIdentity = \"")
                .Append(layoutIdentity).AppendLine("\";");
            builder.AppendLine();
            builder.AppendLine("    public static global::ThirdPerson.GeneratedDiagnosticSampling.DiagnosticPacketLayout CreateDiagnosticPacketLayout() =>");
            builder.AppendLine("        new global::ThirdPerson.GeneratedDiagnosticSampling.DiagnosticPacketLayout(");
            builder.Append("            DiagnosticPacketLayoutIdentity, ")
                .Append(counts[1]).Append(", ")
                .Append(counts[2]).Append(", ")
                .Append(counts[3]).Append(", ")
                .Append(counts[4]).Append(", ")
                .Append(counts[5]).Append(", ")
                .Append(counts[6]).Append(", ")
                .Append(counts[7]).Append(", ")
                .Append(counts[8]).Append(", ")
                .Append(counts[9]).Append(", ")
                .Append(counts[10]).Append(", ")
                .Append(counts[11]).Append(", ")
                .Append(counts[12]);
            if (tables.Count == 0)
                builder.AppendLine(");");
            else
            {
                builder.AppendLine(", new global::ThirdPerson.GeneratedDiagnosticSampling.DiagnosticTableLayout[]");
                builder.AppendLine("            {");
                foreach (TableModel table in tables)
                {
                    int[] rowCounts = Counts(tableFields[table]);
                    string rowIdentity = Hash(new[] { layoutIdentity, table.Canonical }
                        .Concat(tableFields[table].Select(value => value.Canonical)));
                    builder.Append("                new global::ThirdPerson.GeneratedDiagnosticSampling.DiagnosticTableLayout(\"")
                        .Append(Escape(table.Id)).Append("\", ").Append(table.Capacity)
                        .Append(", new global::ThirdPerson.GeneratedDiagnosticSampling.DiagnosticPacketLayout(\"")
                        .Append(rowIdentity).Append("\", ")
                        .Append(string.Join(", ", rowCounts.Skip(1))).AppendLine(")),");
                }
                builder.AppendLine("            });");
            }
            builder.AppendLine();
            builder.AppendLine("    public static global::ThirdPerson.GeneratedDiagnosticSampling.DiagnosticSchemaLayout CreateDiagnosticSchemaLayout()");
            builder.AppendLine("    {");
            builder.AppendLine("        var fields = new global::ThirdPerson.GeneratedDiagnosticSampling.DiagnosticFieldHandle[]");
            builder.AppendLine("        {");
            foreach (FieldModel field in fields)
            {
                builder.Append("            ")
                    .Append(FieldHandleExpression(field, handles, fields))
                    .AppendLine(",");
            }
            builder.AppendLine("        };");
            builder.AppendLine("        var tables = new global::ThirdPerson.GeneratedDiagnosticSampling.DiagnosticTableSchema[]");
            builder.AppendLine("        {");
            for (int tableIndex = 0; tableIndex < tables.Count; tableIndex++)
            {
                TableModel table = tables[tableIndex];
                builder.Append("            new global::ThirdPerson.GeneratedDiagnosticSampling.DiagnosticTableSchema(\"")
                    .Append(Escape(table.Id)).Append("\", ")
                    .Append(table.Revision).Append(", ")
                    .Append(tableIndex).Append(", ")
                    .Append(table.Capacity)
                    .AppendLine(", new global::ThirdPerson.GeneratedDiagnosticSampling.DiagnosticFieldHandle[]");
                builder.AppendLine("            {");
                foreach (FieldModel field in tableFields[table])
                {
                    builder.Append("                ")
                        .Append(FieldHandleExpression(
                            field,
                            tableHandles[table],
                            tableFields[table]))
                        .AppendLine(",");
                }
                builder.AppendLine("            }),");
            }
            builder.AppendLine("        };");
            builder.AppendLine("        var samplers = new global::ThirdPerson.GeneratedDiagnosticSampling.DiagnosticSamplerLayout[]");
            builder.AppendLine("        {");
            foreach (SamplerModel sampler in samplers.OrderBy(value => value.Id, StringComparer.Ordinal))
            {
                builder.Append("            new global::ThirdPerson.GeneratedDiagnosticSampling.DiagnosticSamplerLayout(\"")
                    .Append(Escape(sampler.Id)).Append("\", ")
                    .Append(sampler.Revision).Append(", \"")
                    .Append(Escape(sampler.HostAdapterId)).Append("\", \"")
                    .Append(Escape(sampler.OutputSchema)).AppendLine("\",");
                AppendReferences(
                    builder,
                    "fields",
                    samplerFields[sampler].Select(value => fieldPositions[value]),
                    16);
                builder.AppendLine(",");
                AppendReferences(
                    builder,
                    "tables",
                    sampler.Tables.Select(value => tablePositions[value]),
                    16);
                builder.AppendLine("),");
            }
            builder.AppendLine("        };");
            builder.AppendLine("        return new global::ThirdPerson.GeneratedDiagnosticSampling.DiagnosticSchemaLayout(");
            builder.AppendLine("            DiagnosticCapabilityId,");
            builder.AppendLine("            DiagnosticCapabilityRevision,");
            builder.AppendLine("            DiagnosticProgramId,");
            builder.AppendLine("            DiagnosticSamplerSetIdentity,");
            builder.AppendLine("            DiagnosticSchemaIdentity,");
            builder.AppendLine("            CreateDiagnosticPacketLayout(),");
            builder.AppendLine("            fields,");
            builder.AppendLine("            tables,");
            builder.AppendLine("            samplers);");
            builder.AppendLine("    }");
            builder.AppendLine();
            builder.AppendLine("    public static global::ThirdPerson.GeneratedDiagnosticSampling.DiagnosticCapabilityBuildDescriptor CreateDiagnosticCapabilityBuildDescriptor(");
            builder.AppendLine("        string lineageTypeIdentity,");
            builder.AppendLine("        int packetCapacity,");
            builder.AppendLine("        string writerTransportIdentity) =>");
            builder.AppendLine("        new global::ThirdPerson.GeneratedDiagnosticSampling.DiagnosticCapabilityBuildDescriptor(");
            builder.AppendLine("            DiagnosticCapabilityId,");
            builder.AppendLine("            DiagnosticCapabilityRevision,");
            builder.AppendLine("            global::ThirdPerson.GeneratedDiagnosticSampling.DiagnosticCapabilityMode.Capture,");
            builder.AppendLine("            DiagnosticProgramId,");
            builder.AppendLine("            DiagnosticSamplerSetIdentity,");
            builder.AppendLine("            DiagnosticSchemaIdentity,");
            builder.AppendLine("            DiagnosticGeneratedProgramHash,");
            builder.AppendLine("            DiagnosticGeneratorIdentity,");
            builder.AppendLine("            DiagnosticPacketLayoutIdentity,");
            builder.AppendLine("            lineageTypeIdentity,");
            builder.AppendLine("            packetCapacity,");
            builder.AppendLine("            writerTransportIdentity);");
            builder.AppendLine();
            builder.Append("    public static void Capture(in ")
                .Append(capability.ViewType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
                .AppendLine(" source, ref global::ThirdPerson.GeneratedDiagnosticSampling.DiagnosticCapturePacket packet)");
            builder.AppendLine("    {");
            builder.AppendLine("        if (packet == null) throw new global::System.ArgumentNullException(nameof(packet));");
            builder.AppendLine("        if (!global::System.String.Equals(packet.Layout.Identity, DiagnosticPacketLayoutIdentity, global::System.StringComparison.Ordinal)) throw new global::System.InvalidOperationException(\"Diagnostic packet layout does not match generated program.\");");
            foreach (FieldModel field in fields)
            {
                builder.Append("        packet.").Append(Setter(field.ValueKind)).Append('(')
                    .Append(handles[field]).Append(", ")
                    .Append(field.Method.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
                    .Append('.').Append(field.Method.Name).AppendLine("(in source));");
            }
            for (int tableIndex = 0; tableIndex < tables.Count; tableIndex++)
            {
                TableModel table = tables[tableIndex];
                builder.Append("        int tableCount").Append(tableIndex).Append(" = ")
                    .Append(table.CountMethod.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
                    .Append('.').Append(table.CountMethod.Name).AppendLine("(in source);");
                builder.Append("        packet.Tables[").Append(tableIndex).Append("]")
                    .Append(".Begin(tableCount").Append(tableIndex).AppendLine(", packet.SampleKey);");
                builder.Append("        for (int row = 0; row < tableCount").Append(tableIndex)
                    .AppendLine("; row++)");
                builder.AppendLine("        {");
                builder.Append("            global::ThirdPerson.GeneratedDiagnosticSampling.DiagnosticCapturePacket tableRow = packet.Tables[")
                    .Append(tableIndex).AppendLine("].Row(row);");
                foreach (FieldModel field in tableFields[table])
                {
                    builder.Append("            tableRow.").Append(Setter(field.ValueKind)).Append('(')
                        .Append(tableHandles[table][field]).Append(", ")
                        .Append(field.Method.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
                        .Append('.').Append(field.Method.Name).AppendLine("(in source, row));");
                }
                builder.AppendLine("        }");
            }
            builder.AppendLine("    }");
            builder.AppendLine("}");
            if (namespaceName.Length > 0)
                builder.AppendLine("}");
            return builder.ToString();
        }

        static string FieldHandleExpression(
            FieldModel field,
            IReadOnlyDictionary<FieldModel, int> handles,
            IReadOnlyList<FieldModel> fields)
        {
            string availability =
                "default(global::ThirdPerson.GeneratedDiagnosticSampling.DiagnosticFieldAvailability)";
            if (!string.IsNullOrWhiteSpace(field.AvailabilityFieldId))
            {
                FieldModel source = fields.Single(value => string.Equals(
                    value.Id,
                    field.AvailabilityFieldId,
                    StringComparison.Ordinal));
                availability =
                    "new global::ThirdPerson.GeneratedDiagnosticSampling.DiagnosticFieldAvailability(" +
                    "(global::ThirdPerson.GeneratedDiagnosticSampling.DiagnosticValueKind)" +
                    source.ValueKind + ", " + handles[source] + ", " +
                    field.AvailabilityValue + "L)";
            }
            return
                "new global::ThirdPerson.GeneratedDiagnosticSampling.DiagnosticFieldHandle(\"" +
                Escape(field.Id) + "\", " + field.Revision + ", " +
                "(global::ThirdPerson.GeneratedDiagnosticSampling.DiagnosticValueKind)" +
                field.ValueKind + ", \"" + Escape(field.Unit) + "\", " +
                handles[field] + ", " + availability + ", " +
                (field.Derived ? "true" : "false") + ")";
        }

        static void AppendReferences(
            StringBuilder builder,
            string variable,
            IEnumerable<int> sourceIndices,
            int indent)
        {
            int[] indices = sourceIndices.OrderBy(value => value).ToArray();
            string padding = new string(' ', indent);
            string type = string.Equals(variable, "fields", StringComparison.Ordinal)
                ? "global::ThirdPerson.GeneratedDiagnosticSampling.DiagnosticFieldHandle"
                : "global::ThirdPerson.GeneratedDiagnosticSampling.DiagnosticTableSchema";
            if (indices.Length == 0)
            {
                builder.Append(padding).Append("global::System.Array.Empty<")
                    .Append(type).Append(">()");
                return;
            }
            builder.Append(padding).Append("new ").Append(type).AppendLine("[]");
            builder.Append(padding).AppendLine("{");
            foreach (int index in indices)
                builder.Append(padding).Append("    ").Append(variable).Append('[')
                    .Append(index).AppendLine("],");
            builder.Append(padding).Append('}');
        }

        static int[] Counts(IEnumerable<FieldModel> fields)
        {
            var counts = new int[13];
            foreach (FieldModel field in fields)
                counts[field.ValueKind]++;
            return counts;
        }

        static string Setter(int valueKind)
        {
            switch (valueKind)
            {
                case 1: return "SetBoolean";
                case 2: return "SetInt32";
                case 3: return "SetUInt32";
                case 4: return "SetInt64";
                case 5: return "SetUInt64";
                case 6: return "SetFloat32";
                case 7: return "SetFloat64";
                case 8: return "SetIdentity";
                case 9: return "SetVector2";
                case 10: return "SetVector3";
                case 11: return "SetVector4";
                case 12: return "SetQuaternion";
                default: throw new ArgumentOutOfRangeException(nameof(valueKind));
            }
        }

        static bool ValidateExtractor(FieldModel field, INamedTypeSymbol viewType)
        {
            IMethodSymbol method = field.Method;
            if (!method.IsStatic || method.Parameters.Length != 1)
                return false;
            IParameterSymbol parameter = method.Parameters[0];
            if (parameter.RefKind != RefKind.In ||
                !SymbolEqualityComparer.Default.Equals(parameter.Type, viewType))
            {
                return false;
            }
            string expected = ExpectedType(field.ValueKind);
            return string.Equals(
                method.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                expected,
                StringComparison.Ordinal);
        }

        static bool ValidateTableCount(TableModel table, INamedTypeSymbol viewType) =>
            table.CountMethod.IsStatic &&
            table.CountMethod.Parameters.Length == 1 &&
            table.CountMethod.Parameters[0].RefKind == RefKind.In &&
            SymbolEqualityComparer.Default.Equals(
                table.CountMethod.Parameters[0].Type,
                viewType) &&
            table.CountMethod.ReturnType.SpecialType == SpecialType.System_Int32;

        static bool ValidateTableExtractor(FieldModel field, INamedTypeSymbol viewType)
        {
            IMethodSymbol method = field.Method;
            return method.IsStatic &&
                method.Parameters.Length == 2 &&
                method.Parameters[0].RefKind == RefKind.In &&
                SymbolEqualityComparer.Default.Equals(method.Parameters[0].Type, viewType) &&
                method.Parameters[1].RefKind == RefKind.None &&
                method.Parameters[1].Type.SpecialType == SpecialType.System_Int32 &&
                string.Equals(
                    method.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    ExpectedType(field.ValueKind),
                    StringComparison.Ordinal);
        }

        static string ExpectedType(int valueKind)
        {
            switch (valueKind)
            {
                case 1: return "bool";
                case 2: return "int";
                case 3: return "uint";
                case 4: return "long";
                case 5: return "ulong";
                case 6: return "float";
                case 7: return "double";
                case 8: return "string";
                case 9: return "global::ThirdPerson.GeneratedDiagnosticSampling.DiagnosticVector2";
                case 10: return "global::ThirdPerson.GeneratedDiagnosticSampling.DiagnosticVector3";
                case 11: return "global::ThirdPerson.GeneratedDiagnosticSampling.DiagnosticVector4";
                case 12: return "global::ThirdPerson.GeneratedDiagnosticSampling.DiagnosticQuaternion";
                default: return string.Empty;
            }
        }

        static bool IsStaticPartial(INamedTypeSymbol type)
        {
            if (!type.IsStatic)
                return false;
            return type.DeclaringSyntaxReferences
                .Select(value => value.GetSyntax())
                .OfType<ClassDeclarationSyntax>()
                .Any(value => value.Modifiers.Any(SyntaxKind.PartialKeyword));
        }

        static AttributeData FindAttribute(ISymbol symbol, INamedTypeSymbol attributeType) =>
            symbol.GetAttributes().FirstOrDefault(value => SymbolEqualityComparer.Default.Equals(
                value.AttributeClass,
                attributeType));

        static IEnumerable<INamedTypeSymbol> EnumerateTypes(INamespaceSymbol namespaceSymbol)
        {
            foreach (INamedTypeSymbol type in namespaceSymbol.GetTypeMembers())
            {
                yield return type;
                foreach (INamedTypeSymbol nested in EnumerateTypes(type))
                    yield return nested;
            }
            foreach (INamespaceSymbol child in namespaceSymbol.GetNamespaceMembers())
            {
                foreach (INamedTypeSymbol type in EnumerateTypes(child))
                    yield return type;
            }
        }

        static IEnumerable<INamedTypeSymbol> EnumerateTypes(INamedTypeSymbol type)
        {
            foreach (INamedTypeSymbol nested in type.GetTypeMembers())
            {
                yield return nested;
                foreach (INamedTypeSymbol descendant in EnumerateTypes(nested))
                    yield return descendant;
            }
        }

        static string[] Strings(TypedConstant constant) => constant.Kind == TypedConstantKind.Array
            ? constant.Values.Select(value => value.Value as string).Where(value => value != null).ToArray()
            : Array.Empty<string>();

        static INamedTypeSymbol[] Types(TypedConstant constant) =>
            constant.Kind == TypedConstantKind.Array
                ? constant.Values.Select(value => value.Value as INamedTypeSymbol).ToArray()
                : Array.Empty<INamedTypeSymbol>();

        static string[] NamedStrings(AttributeData attribute, string name)
        {
            KeyValuePair<string, TypedConstant> value = attribute.NamedArguments.FirstOrDefault(
                pair => string.Equals(pair.Key, name, StringComparison.Ordinal));
            return value.Key == null ? Array.Empty<string>() : Strings(value.Value);
        }

        static string NamedString(AttributeData attribute, string name)
        {
            KeyValuePair<string, TypedConstant> value = attribute.NamedArguments.FirstOrDefault(
                pair => string.Equals(pair.Key, name, StringComparison.Ordinal));
            return value.Key == null ? string.Empty : value.Value.Value as string ?? string.Empty;
        }

        static long NamedInt64(AttributeData attribute, string name, long fallback)
        {
            KeyValuePair<string, TypedConstant> value = attribute.NamedArguments.FirstOrDefault(
                pair => string.Equals(pair.Key, name, StringComparison.Ordinal));
            return value.Key == null ? fallback : Convert.ToInt64(value.Value.Value);
        }

        static bool NamedBoolean(AttributeData attribute, string name)
        {
            KeyValuePair<string, TypedConstant> value = attribute.NamedArguments.FirstOrDefault(
                pair => string.Equals(pair.Key, name, StringComparison.Ordinal));
            return value.Key != null && (bool)value.Value.Value;
        }

        static void Report(
            GeneratorExecutionContext context,
            DiagnosticDescriptor descriptor,
            ISymbol symbol,
            string message) =>
            context.ReportDiagnostic(Diagnostic.Create(descriptor, Location(symbol), message));

        static Location Location(ISymbol symbol) =>
            symbol.Locations.FirstOrDefault(value => value.IsInSource) ??
            Microsoft.CodeAnalysis.Location.None;

        static string MethodSource(IMethodSymbol method)
        {
            string source = string.Join(
                "\n",
                method.DeclaringSyntaxReferences
                    .Select(value => value.GetSyntax().NormalizeWhitespace().ToFullString()));
            return source.Length == 0 ? method.ToDisplayString() : source;
        }

        static string Hash(IEnumerable<string> values)
        {
            string text = string.Join("\n", values);
            using (SHA256 sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(text));
                var builder = new StringBuilder(hash.Length * 2);
                foreach (byte value in hash)
                    builder.Append(value.ToString("x2"));
                return builder.ToString();
            }
        }

        static string Escape(string value) => value.Replace("\\", "\\\\").Replace("\"", "\\\"");

        sealed class CapabilityModel
        {
            public CapabilityModel(string id, int revision, INamedTypeSymbol viewType)
            {
                Id = id;
                Revision = revision;
                ViewType = viewType;
            }

            public string Id { get; }
            public int Revision { get; }
            public INamedTypeSymbol ViewType { get; }
        }

        sealed class SamplerModel
        {
            public SamplerModel(
                string capabilityId,
                string id,
                int revision,
                string hostAdapterId,
                string outputSchema,
                string[] groups,
                string[] fields,
                string[] tables)
            {
                CapabilityId = capabilityId;
                Id = id;
                Revision = revision;
                HostAdapterId = hostAdapterId;
                OutputSchema = outputSchema;
                Groups = groups;
                Fields = fields;
                Tables = tables;
            }

            public string CapabilityId { get; }
            public string Id { get; }
            public int Revision { get; }
            public string HostAdapterId { get; }
            public string OutputSchema { get; }
            public string[] Groups { get; }
            public string[] Fields { get; }
            public string[] Tables { get; }
            public bool IsValid =>
                !string.IsNullOrWhiteSpace(CapabilityId) &&
                !string.IsNullOrWhiteSpace(Id) &&
                Revision > 0 &&
                !string.IsNullOrWhiteSpace(HostAdapterId) &&
                !string.IsNullOrWhiteSpace(OutputSchema) &&
                (Groups.Length > 0 || Fields.Length > 0 || Tables.Length > 0);
            public string Canonical => string.Join("|", new[]
            {
                CapabilityId,
                Id,
                Revision.ToString(),
                HostAdapterId,
                OutputSchema,
                string.Join(",", Groups.OrderBy(value => value, StringComparer.Ordinal)),
                string.Join(",", Fields.OrderBy(value => value, StringComparer.Ordinal)),
                string.Join(",", Tables.OrderBy(value => value, StringComparer.Ordinal))
            });
        }

        sealed class FieldModel
        {
            public FieldModel(
                string capabilityId,
                string id,
                int revision,
                int valueKind,
                string unit,
                string tableId,
                string[] groups,
                string availabilityFieldId,
                long availabilityValue,
                bool derived,
                string[] dependencies,
                IMethodSymbol method)
            {
                CapabilityId = capabilityId;
                Id = id;
                Revision = revision;
                ValueKind = valueKind;
                Unit = unit;
                TableId = tableId;
                Groups = groups;
                AvailabilityFieldId = availabilityFieldId;
                AvailabilityValue = availabilityValue;
                Derived = derived;
                Dependencies = dependencies;
                Method = method;
            }

            public string CapabilityId { get; }
            public string Id { get; }
            public int Revision { get; }
            public int ValueKind { get; }
            public string Unit { get; }
            public string TableId { get; }
            public string[] Groups { get; }
            public string AvailabilityFieldId { get; }
            public long AvailabilityValue { get; }
            public bool Derived { get; }
            public string[] Dependencies { get; }
            public IMethodSymbol Method { get; }
            public bool IsValid =>
                !string.IsNullOrWhiteSpace(CapabilityId) &&
                !string.IsNullOrWhiteSpace(Id) &&
                Revision > 0 &&
                ValueKind >= 1 && ValueKind <= 12 &&
                !string.IsNullOrWhiteSpace(Unit) &&
                !string.IsNullOrWhiteSpace(TableId) &&
                Groups.Length > 0;
            public string Canonical => string.Join("|", new[]
            {
                CapabilityId,
                Id,
                Revision.ToString(),
                ValueKind.ToString(),
                Unit,
                TableId,
                string.Join(",", Groups.OrderBy(value => value, StringComparer.Ordinal)),
                AvailabilityFieldId,
                AvailabilityValue.ToString(),
                Derived.ToString(),
                string.Join(",", Dependencies.OrderBy(value => value, StringComparer.Ordinal)),
                Method.ToDisplayString()
            });
        }

        sealed class TableModel
        {
            public TableModel(
                string capabilityId,
                string id,
                int revision,
                int capacity,
                IMethodSymbol countMethod)
            {
                CapabilityId = capabilityId;
                Id = id;
                Revision = revision;
                Capacity = capacity;
                CountMethod = countMethod;
            }

            public string CapabilityId { get; }
            public string Id { get; }
            public int Revision { get; }
            public int Capacity { get; }
            public IMethodSymbol CountMethod { get; }
            public bool IsValid =>
                !string.IsNullOrWhiteSpace(CapabilityId) &&
                !string.IsNullOrWhiteSpace(Id) &&
                Revision > 0 &&
                Capacity > 0 &&
                CountMethod != null;
            public string Canonical => string.Join("|", new[]
            {
                CapabilityId,
                Id,
                Revision.ToString(),
                Capacity.ToString(),
                CountMethod?.ToDisplayString() ?? string.Empty
            });
        }
    }
}
