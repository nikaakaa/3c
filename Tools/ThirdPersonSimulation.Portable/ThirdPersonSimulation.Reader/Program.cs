using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using ThirdPersonSimulation;

namespace ThirdPersonSimulation.Reader
{
    internal static class Program
    {
        static readonly HashSet<string> s_Sections = new HashSet<string>(StringComparer.Ordinal)
        {
            "summary",
            "operations",
            "value-inputs",
            "control-flow",
            "state-slots",
            "scopes",
            "equipment",
            "producers",
            "source-map",
            "all"
        };

        static int Main(string[] args)
        {
            if (!TryParse(args, out ReaderRequest request, out string error))
            {
                Console.Error.WriteLine(error);
                Console.Error.WriteLine("Usage: ThirdPersonSimulation.Reader semantic-ir <artifact-path> [--section <summary|operations|value-inputs|control-flow|state-slots|scopes|equipment|producers|source-map|all>] [--format <text|json>]");
                return 2;
            }

            try
            {
                byte[] bytes = File.ReadAllBytes(request.Path);
                ReadSemanticIr(bytes, request);
                return 0;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine($"{request.Command}: invalid artifact: {exception.Message}");
                return 1;
            }
        }

        static bool TryParse(string[] args, out ReaderRequest request, out string error)
        {
            request = default;
            error = string.Empty;
            if (args == null || args.Length < 2)
            {
                error = "An explicit artifact subcommand and path are required.";
                return false;
            }
            string command = args[0];
            if (!string.Equals(command, "semantic-ir", StringComparison.Ordinal))
            {
                error = $"Unknown subcommand '{command}'.";
                return false;
            }

            string section = "summary";
            string format = "text";
            for (int i = 2; i < args.Length; i += 2)
            {
                if (i + 1 >= args.Length)
                {
                    error = $"Option '{args[i]}' requires a value.";
                    return false;
                }
                if (string.Equals(args[i], "--section", StringComparison.Ordinal))
                    section = args[i + 1];
                else if (string.Equals(args[i], "--format", StringComparison.Ordinal))
                    format = args[i + 1];
                else
                {
                    error = $"Unknown option '{args[i]}'.";
                    return false;
                }
            }
            if (!s_Sections.Contains(section))
            {
                error = $"Unknown section '{section}'.";
                return false;
            }
            if (!string.Equals(format, "text", StringComparison.Ordinal) &&
                !string.Equals(format, "json", StringComparison.Ordinal))
            {
                error = $"Unknown format '{format}'.";
                return false;
            }
            request = new ReaderRequest(command, args[1], section, format);
            return true;
        }

        static void ReadSemanticIr(byte[] bytes, ReaderRequest request)
        {
            CharacterGameplaySemanticIrArtifactHeader header = CharacterGameplaySemanticIrCodec.ReadArtifactHeader(bytes);
            ValidatedSemanticIrArtifact artifact = CharacterGameplaySemanticIrCodec.ReadValidatedArtifact(
                bytes,
                new SemanticIrLoadExpectation(
                    header.ProgramId,
                    header.CompilerVersion,
                    header.OperationSetVersion,
                    header.TickRate,
                    header.SourceRevision,
                    header.SemanticHash,
                    header.Root));
            if (request.IsJson)
                WriteSemanticJson(artifact.SemanticIr, request.Section);
            else
                WriteSemanticText(artifact.SemanticIr, request.Section);
        }

        static void WriteSemanticText(CharacterGameplaySemanticIr ir, string section)
        {
            Console.WriteLine("Artifact: semantic-ir");
            WriteManifestText(ir.Manifest.ProgramId, ir.Manifest.CompilerVersion, ir.Manifest.OperationSetVersion, ir.Manifest.TickRate, ir.Manifest.SourceRevision, ir.SemanticHash);
            Console.WriteLine($"GameplayCapabilities: {string.Join(",", ir.Manifest.Capabilities.GameplayCapabilities)}");
            Console.WriteLine($"WorldCapabilities: {ir.Manifest.Capabilities.RequiredWorldCapabilities}");
            WriteSemanticCountsText(ir);
            WriteSemanticSectionText(ir, section);
        }

        static void WriteManifestText(ProgramId programId, string compiler, OperationSetVersion operationSet, int tickRate, ProgramRevision sourceRevision, SemanticHash semanticHash)
        {
            Console.WriteLine($"ProgramId: {programId.Value}");
            Console.WriteLine($"CompilerVersion: {compiler}");
            Console.WriteLine($"OperationSetVersion: {operationSet}");
            Console.WriteLine($"TickRate: {tickRate.ToString(CultureInfo.InvariantCulture)}");
            Console.WriteLine($"SourceRevision: {sourceRevision.Value}");
            Console.WriteLine($"SemanticHash: {semanticHash}");
        }

        static void WriteSemanticCountsText(CharacterGameplaySemanticIr ir)
        {
            Console.WriteLine($"Operations: {ir.Operations.Count}");
            Console.WriteLine($"Literals: {ir.Literals.Count}");
            Console.WriteLine($"ValueInputs: {CountValueInputs(ir.ControlFlow, ir.ConstantInputBindings.Count)}");
            Console.WriteLine($"ControlFlow: {ir.ControlFlow.Count}");
            Console.WriteLine($"References: {ir.References.Count}");
            Console.WriteLine($"StateSlots: {ir.StateDeclarations.Count}");
            Console.WriteLine($"Scopes: {ir.Scopes.Count}");
            Console.WriteLine($"WorldRequests: {ir.WorldRequests.Count}");
            Console.WriteLine($"OutputChannels: {ir.OutputChannels.Count}");
            Console.WriteLine($"CatalogEntries: {ir.CatalogEntries.Count}");
            Console.WriteLine($"Producers: {ir.Producers.Count}");
            Console.WriteLine($"SourceMap: {ir.SourceMap.Count}");
        }

        static void WriteSemanticSectionText(CharacterGameplaySemanticIr ir, string section)
        {
            if (section == "summary")
                return;
            if (section == "operations" || section == "all")
            {
                Console.WriteLine("[operations]");
                for (int i = 0; i < ir.Operations.Count; i++)
                {
                    SemanticOperation value = ir.Operations[i];
                    Console.WriteLine($"{value.Handle.Value}\t{value.Code}\t{Escape(value.TemplateIdentity)}\toperands={Join(value.Operands)}\tliterals={Join(value.LiteralReferences)}\tstate={Join(value.StateSlots)}\ti0={value.Integer0}\ti1={value.Integer1}\tu0={value.Unsigned0}\tn0={Number(value.Number0)}\tn0-source={Escape(value.Number0SourceIdentity)}\ttext={Escape(value.Text0)}\tflags={value.Flags}");
                }
            }
            if (section == "value-inputs" || section == "all")
            {
                Console.WriteLine("[value-inputs]");
                for (int i = 0; i < ir.ControlFlow.Count; i++)
                {
                    ProgramControlFlowEdge value = ir.ControlFlow[i];
                    if (value.Kind != ProgramControlFlowKind.Value)
                        continue;
                    SemanticValueKind kind = ir.ResolveLinkedValueKind(value);
                    Console.WriteLine($"{value.Target.Value}\tport={Escape(value.TargetPort)}\tkind={kind}\toperation={value.Source.Value}\toutput={Escape(value.SourcePort)}\tedge={Escape(value.Identity)}");
                }
                for (int i = 0; i < ir.ConstantInputBindings.Count; i++)
                {
                    SemanticConstantInputBinding value = ir.ConstantInputBindings[i];
                    SemanticLiteral constant = ir.Literals[value.ConstantIndex];
                    Console.WriteLine($"{value.TargetOperation.Value}\tport={Escape(value.TargetPort)}\tkind={value.ResolvedValueKind}\tconstant={value.ConstantIndex}\tsource={Escape(constant.Identity)}");
                }
            }
            WriteEquipmentText(ir.CatalogEntries, section);
            WriteCommonSectionsText(ir.ControlFlow, ir.StateDeclarations, ir.Scopes, ir.Producers, ir.SourceMap, section);
        }

        static void WriteEquipmentText(IReadOnlyList<ProgramCatalogEntry> catalog, string section)
        {
            if (section != "equipment" && section != "all")
                return;
            Console.WriteLine("[equipment]");
            for (int i = 0; i < catalog.Count; i++)
            {
                ProgramCatalogEntry entry = catalog[i];
                if (!IsEquipmentCatalogEntry(entry.Kind))
                    continue;
                string fields = string.Join(",", entry.Fields.Select(value => value.Kind == ProgramCatalogFieldKind.Constant
                    ? $"{value.Name}=constant:{value.ConstantIndex.ToString(CultureInfo.InvariantCulture)}"
                    : $"{value.Name}=identity:{Escape(value.Identity)}"));
                Console.WriteLine($"{entry.Index}\t{entry.Kind}\t{Escape(entry.Identity)}\trevision={entry.Revision}\tfields={fields}");
            }
        }

        static void WriteCommonSectionsText(
            IReadOnlyList<ProgramControlFlowEdge> controlFlow,
            IReadOnlyList<ProgramStateSlot> stateSlots,
            IReadOnlyList<ProgramScopeLayout> scopes,
            IReadOnlyList<ProgramProducer> producers,
            IReadOnlyList<ProgramSourceMapEntry> sourceMap,
            string section)
        {
            if (section == "control-flow" || section == "all")
            {
                Console.WriteLine("[control-flow]");
                for (int i = 0; i < controlFlow.Count; i++)
                {
                    ProgramControlFlowEdge value = controlFlow[i];
                    Console.WriteLine($"{Escape(value.Identity)}\t{value.Kind}\t{value.Source.Value}->{value.Target.Value}\tports={Escape(value.SourcePort)}->{Escape(value.TargetPort)}\torder={value.Order}\tpriority={value.Priority}\tabort={value.AbortPolicy}\tcondition={(value.HasCondition ? value.Condition.Value.ToString(CultureInfo.InvariantCulture) : "none")}");
                }
            }
            if (section == "state-slots" || section == "all")
            {
                Console.WriteLine("[state-slots]");
                for (int i = 0; i < stateSlots.Count; i++)
                {
                    ProgramStateSlot value = stateSlots[i];
                    Console.WriteLine($"{value.Index}\t{Escape(value.Identity)}\t{value.ValueKind}\t{value.OwnerKind}\t{value.Semantic}\towner={Escape(value.OwnerIdentity)}\tdefault={value.DefaultConstantIndex}");
                }
            }
            if (section == "scopes" || section == "all")
            {
                Console.WriteLine("[scopes]");
                for (int i = 0; i < scopes.Count; i++)
                {
                    ProgramScopeLayout value = scopes[i];
                    Console.WriteLine($"{value.CompiledOwnerIndex}\t{Escape(value.Identity)}\t{value.Kind}\towner={Escape(value.OwnerIdentity)}\toperation={(value.OwnerOperation.IsValid ? value.OwnerOperation.Value.ToString(CultureInfo.InvariantCulture) : "none")}\tstate={Join(value.StateSlots)}");
                }
            }
            if (section == "producers" || section == "all")
            {
                Console.WriteLine("[producers]");
                for (int i = 0; i < producers.Count; i++)
                {
                    ProgramProducer value = producers[i];
                    Console.WriteLine($"{value.Index}\t{Escape(value.Identity)}\tanimationChannel={Escape(value.AnimationChannelId.Value)}\tsource={Escape(value.SourceIdentity)}\tchannel={value.ChannelKind}");
                }
            }
            if (section == "source-map" || section == "all")
            {
                Console.WriteLine("[source-map]");
                for (int i = 0; i < sourceMap.Count; i++)
                {
                    ProgramSourceMapEntry value = sourceMap[i];
                    Console.WriteLine($"{value.TargetKind}:{value.TargetIndex}\t{Escape(value.SourceType)}\tgraph={Escape(value.GraphId)}\tnode={Escape(value.NodeId)}\tedge={Escape(value.EdgeId)}\tdeclaration={Escape(value.DeclarationId)}\ttimeline={Escape(value.TimelineId)}\ttrack={Escape(value.TrackId)}\tclip={Escape(value.ClipId)}\tpath={Escape(value.DisplayPath)}");
                }
            }
        }

        static void WriteSemanticJson(CharacterGameplaySemanticIr ir, string section)
        {
            using Utf8JsonWriter writer = CreateJsonWriter();
            writer.WriteStartObject();
            writer.WriteString("artifact", "semantic-ir");
            WriteManifestJson(writer, ir.Manifest.ProgramId, ir.Manifest.CompilerVersion, ir.Manifest.OperationSetVersion, ir.Manifest.TickRate, ir.Manifest.SourceRevision, ir.SemanticHash, string.Empty);
            WriteCapabilitiesJson(writer, ir.Manifest.Capabilities);
            writer.WritePropertyName("counts");
            writer.WriteStartObject();
            writer.WriteNumber("operations", ir.Operations.Count);
            writer.WriteNumber("literals", ir.Literals.Count);
            writer.WriteNumber("valueInputs", CountValueInputs(ir.ControlFlow, ir.ConstantInputBindings.Count));
            writer.WriteNumber("controlFlow", ir.ControlFlow.Count);
            writer.WriteNumber("references", ir.References.Count);
            writer.WriteNumber("stateSlots", ir.StateDeclarations.Count);
            writer.WriteNumber("scopes", ir.Scopes.Count);
            writer.WriteNumber("worldRequests", ir.WorldRequests.Count);
            writer.WriteNumber("outputChannels", ir.OutputChannels.Count);
            writer.WriteNumber("catalogEntries", ir.CatalogEntries.Count);
            writer.WriteNumber("producers", ir.Producers.Count);
            writer.WriteNumber("sourceMap", ir.SourceMap.Count);
            writer.WriteEndObject();
            WriteSemanticSectionJson(writer, ir, section);
            writer.WriteEndObject();
            writer.Flush();
        }

        static Utf8JsonWriter CreateJsonWriter()
        {
            return new Utf8JsonWriter(Console.OpenStandardOutput(), new JsonWriterOptions { Indented = true });
        }

        static void WriteManifestJson(Utf8JsonWriter writer, ProgramId programId, string compiler, OperationSetVersion operationSet, int tickRate, ProgramRevision sourceRevision, SemanticHash semanticHash, string numericProfile)
        {
            writer.WritePropertyName("manifest");
            writer.WriteStartObject();
            writer.WriteString("programId", programId.Value);
            writer.WriteString("compilerVersion", compiler);
            writer.WriteString("operationSetVersion", operationSet.ToString());
            writer.WriteNumber("tickRate", tickRate);
            writer.WriteString("sourceRevision", sourceRevision.Value);
            writer.WriteString("semanticHash", semanticHash.ToString());
            if (!string.IsNullOrEmpty(numericProfile))
                writer.WriteString("numericProfile", numericProfile);
            writer.WriteEndObject();
        }

        static void WriteCapabilitiesJson(Utf8JsonWriter writer, ProgramCapabilityManifest capabilities)
        {
            writer.WritePropertyName("gameplayCapabilities");
            writer.WriteStartArray();
            for (int i = 0; i < capabilities.GameplayCapabilities.Count; i++)
                writer.WriteStringValue(capabilities.GameplayCapabilities[i]);
            writer.WriteEndArray();
            writer.WriteString("worldCapabilities", capabilities.RequiredWorldCapabilities.ToString());
        }

        static void WriteSemanticSectionJson(Utf8JsonWriter writer, CharacterGameplaySemanticIr ir, string section)
        {
            writer.WriteString("section", section);
            if (section == "summary")
                return;
            if (section == "operations" || section == "all")
            {
                writer.WritePropertyName("operations");
                writer.WriteStartArray();
                for (int i = 0; i < ir.Operations.Count; i++)
                {
                    SemanticOperation value = ir.Operations[i];
                    writer.WriteStartObject();
                    writer.WriteNumber("handle", value.Handle.Value);
                    writer.WriteString("code", value.Code.ToString());
                    writer.WriteString("templateIdentity", value.TemplateIdentity);
                    WriteIntArray(writer, "operands", value.Operands);
                    WriteIntArray(writer, "literals", value.LiteralReferences);
                    WriteIntArray(writer, "stateSlots", value.StateSlots);
                    writer.WriteNumber("integer0", value.Integer0);
                    writer.WriteNumber("integer1", value.Integer1);
                    writer.WriteNumber("unsigned0", value.Unsigned0);
                    writer.WriteNumber("number0", value.Number0);
                    writer.WriteString("number0SourceIdentity", value.Number0SourceIdentity);
                    writer.WriteString("text0", value.Text0);
                    writer.WriteNumber("flags", value.Flags);
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();
            }
            if (section == "value-inputs" || section == "all")
            {
                writer.WritePropertyName("valueInputs");
                writer.WriteStartArray();
                for (int i = 0; i < ir.ControlFlow.Count; i++)
                {
                    ProgramControlFlowEdge value = ir.ControlFlow[i];
                    if (value.Kind != ProgramControlFlowKind.Value)
                        continue;
                    writer.WriteStartObject();
                    writer.WriteNumber("targetOperation", value.Target.Value);
                    writer.WriteString("targetPort", value.TargetPort);
                    writer.WriteString("resolvedValueKind", ir.ResolveLinkedValueKind(value).ToString());
                    writer.WriteNumber("sourceOperation", value.Source.Value);
                    writer.WriteString("sourcePort", value.SourcePort);
                    writer.WriteString("edgeIdentity", value.Identity);
                    writer.WriteEndObject();
                }
                for (int i = 0; i < ir.ConstantInputBindings.Count; i++)
                {
                    SemanticConstantInputBinding value = ir.ConstantInputBindings[i];
                    SemanticLiteral constant = ir.Literals[value.ConstantIndex];
                    writer.WriteStartObject();
                    writer.WriteNumber("targetOperation", value.TargetOperation.Value);
                    writer.WriteString("targetPort", value.TargetPort);
                    writer.WriteString("resolvedValueKind", value.ResolvedValueKind.ToString());
                    writer.WriteNumber("constantIndex", value.ConstantIndex);
                    writer.WriteString("constantSourceIdentity", constant.Identity);
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();
            }
            WriteEquipmentJson(writer, ir.CatalogEntries, section);
            WriteCommonSectionsJson(writer, ir.ControlFlow, ir.StateDeclarations, ir.Scopes, ir.Producers, ir.SourceMap, section);
        }

        static void WriteEquipmentJson(Utf8JsonWriter writer, IReadOnlyList<ProgramCatalogEntry> catalog, string section)
        {
            if (section != "equipment" && section != "all")
                return;
            writer.WritePropertyName("equipment");
            writer.WriteStartArray();
            for (int i = 0; i < catalog.Count; i++)
            {
                ProgramCatalogEntry entry = catalog[i];
                if (!IsEquipmentCatalogEntry(entry.Kind))
                    continue;
                writer.WriteStartObject();
                writer.WriteNumber("index", entry.Index);
                writer.WriteString("kind", entry.Kind.ToString());
                writer.WriteString("identity", entry.Identity);
                writer.WriteString("revision", entry.Revision.ToString());
                writer.WritePropertyName("fields");
                writer.WriteStartArray();
                for (int fieldIndex = 0; fieldIndex < entry.Fields.Count; fieldIndex++)
                {
                    ProgramCatalogField field = entry.Fields[fieldIndex];
                    writer.WriteStartObject();
                    writer.WriteString("name", field.Name);
                    writer.WriteString("kind", field.Kind.ToString());
                    if (field.Kind == ProgramCatalogFieldKind.Constant)
                        writer.WriteNumber("constantIndex", field.ConstantIndex);
                    else
                        writer.WriteString("identity", field.Identity);
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
        }

        static bool IsEquipmentCatalogEntry(ProgramCatalogEntryKind kind) =>
            kind >= ProgramCatalogEntryKind.CompositionRoot && kind <= ProgramCatalogEntryKind.EquipmentVisualBinding;

        static void WriteCommonSectionsJson(
            Utf8JsonWriter writer,
            IReadOnlyList<ProgramControlFlowEdge> controlFlow,
            IReadOnlyList<ProgramStateSlot> stateSlots,
            IReadOnlyList<ProgramScopeLayout> scopes,
            IReadOnlyList<ProgramProducer> producers,
            IReadOnlyList<ProgramSourceMapEntry> sourceMap,
            string section)
        {
            if (section == "control-flow" || section == "all")
            {
                writer.WritePropertyName("controlFlow");
                writer.WriteStartArray();
                for (int i = 0; i < controlFlow.Count; i++)
                {
                    ProgramControlFlowEdge value = controlFlow[i];
                    writer.WriteStartObject();
                    writer.WriteString("identity", value.Identity);
                    writer.WriteString("kind", value.Kind.ToString());
                    writer.WriteNumber("source", value.Source.Value);
                    writer.WriteNumber("target", value.Target.Value);
                    writer.WriteString("sourcePort", value.SourcePort);
                    writer.WriteString("targetPort", value.TargetPort);
                    writer.WriteNumber("order", value.Order);
                    writer.WriteNumber("priority", value.Priority);
                    writer.WriteString("abortPolicy", value.AbortPolicy.ToString());
                    if (value.HasCondition)
                        writer.WriteNumber("condition", value.Condition.Value);
                    else
                        writer.WriteNull("condition");
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();
            }
            if (section == "state-slots" || section == "all")
            {
                writer.WritePropertyName("stateSlots");
                writer.WriteStartArray();
                for (int i = 0; i < stateSlots.Count; i++)
                {
                    ProgramStateSlot value = stateSlots[i];
                    writer.WriteStartObject();
                    writer.WriteNumber("index", value.Index);
                    writer.WriteString("identity", value.Identity);
                    writer.WriteString("valueKind", value.ValueKind.ToString());
                    writer.WriteString("ownerKind", value.OwnerKind.ToString());
                    writer.WriteString("semantic", value.Semantic.ToString());
                    writer.WriteString("ownerIdentity", value.OwnerIdentity);
                    writer.WriteNumber("defaultConstantIndex", value.DefaultConstantIndex);
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();
            }
            if (section == "scopes" || section == "all")
            {
                writer.WritePropertyName("scopes");
                writer.WriteStartArray();
                for (int i = 0; i < scopes.Count; i++)
                {
                    ProgramScopeLayout value = scopes[i];
                    writer.WriteStartObject();
                    writer.WriteNumber("compiledOwnerIndex", value.CompiledOwnerIndex);
                    writer.WriteString("identity", value.Identity);
                    writer.WriteString("kind", value.Kind.ToString());
                    writer.WriteString("ownerIdentity", value.OwnerIdentity);
                    if (value.OwnerOperation.IsValid)
                        writer.WriteNumber("ownerOperation", value.OwnerOperation.Value);
                    else
                        writer.WriteNull("ownerOperation");
                    WriteIntArray(writer, "stateSlots", value.StateSlots);
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();
            }
            if (section == "producers" || section == "all")
            {
                writer.WritePropertyName("producers");
                writer.WriteStartArray();
                for (int i = 0; i < producers.Count; i++)
                {
                    ProgramProducer value = producers[i];
                    writer.WriteStartObject();
                    writer.WriteNumber("index", value.Index);
                    writer.WriteString("identity", value.Identity);
                    writer.WriteString("animationChannelId", value.AnimationChannelId.Value);
                    writer.WriteString("sourceIdentity", value.SourceIdentity);
                    writer.WriteString("channelKind", value.ChannelKind.ToString());
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();
            }
            if (section == "source-map" || section == "all")
            {
                writer.WritePropertyName("sourceMap");
                writer.WriteStartArray();
                for (int i = 0; i < sourceMap.Count; i++)
                {
                    ProgramSourceMapEntry value = sourceMap[i];
                    writer.WriteStartObject();
                    writer.WriteString("targetKind", value.TargetKind.ToString());
                    writer.WriteNumber("targetIndex", value.TargetIndex);
                    writer.WriteString("sourceType", value.SourceType);
                    writer.WriteString("graphId", value.GraphId);
                    writer.WriteString("nodeId", value.NodeId);
                    writer.WriteString("edgeId", value.EdgeId);
                    writer.WriteString("declarationId", value.DeclarationId);
                    writer.WriteString("timelineId", value.TimelineId);
                    writer.WriteString("trackId", value.TrackId);
                    writer.WriteString("clipId", value.ClipId);
                    writer.WriteString("displayPath", value.DisplayPath);
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();
            }
        }

        static void WriteIntArray(Utf8JsonWriter writer, string name, IReadOnlyList<int> values)
        {
            writer.WritePropertyName(name);
            writer.WriteStartArray();
            for (int i = 0; i < values.Count; i++)
                writer.WriteNumberValue(values[i]);
            writer.WriteEndArray();
        }

        static int CountValueInputs(IReadOnlyList<ProgramControlFlowEdge> controlFlow, int constantInputCount)
        {
            int count = constantInputCount;
            for (int i = 0; i < controlFlow.Count; i++)
            {
                if (controlFlow[i].Kind == ProgramControlFlowKind.Value)
                    count++;
            }
            return count;
        }

        static string Join(IReadOnlyList<int> values) => string.Join(",", values);
        static string Number(double value) => value.ToString("R", CultureInfo.InvariantCulture);
        static string Escape(string value) => (value ?? string.Empty).Replace("\\", "\\\\").Replace("\t", "\\t").Replace("\r", "\\r").Replace("\n", "\\n");

        readonly struct ReaderRequest
        {
            public ReaderRequest(string command, string path, string section, string format)
            {
                Command = command;
                Path = path;
                Section = section;
                Format = format;
            }

            public string Command { get; }
            public string Path { get; }
            public string Section { get; }
            public string Format { get; }
            public bool IsJson => string.Equals(Format, "json", StringComparison.Ordinal);
        }
    }
}

