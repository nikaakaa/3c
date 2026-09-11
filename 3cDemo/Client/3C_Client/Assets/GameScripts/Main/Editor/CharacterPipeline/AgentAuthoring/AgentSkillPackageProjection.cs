using System;
using System.Collections.Generic;
using System.Linq;
using FlowCanvas;
using FlowCanvas.Macros;
using Newtonsoft.Json.Linq;
using ThirdPersonCharacter.Control.Authoring;
using TreeDesigner.Authoring;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    internal static class AgentSkillPackageProjection
    {
        public static IReadOnlyList<AgentPackageSkillNodeKindDescriptor>
            ExportCatalog()
        {
            return BtsmtlSkillGraphAuthoringMetadata
                .GetDomainCapabilities()
                .Where(value => !value.SystemOwned)
                .Select(value => new AgentPackageSkillNodeKindDescriptor
                {
                    kind = value.ExternalKind,
                    graphRoles = value.AllowedDocumentRoles
                        .Select(role => role.Value)
                        .OrderBy(role => role, StringComparer.Ordinal)
                        .ToList(),
                    properties = value.Fields
                        .Where(field => field.AuthoringVisible)
                        .Select(field => field.FieldId.Value)
                        .OrderBy(field => field, StringComparer.Ordinal)
                        .ToList(),
                    flowPorts = ToPackagePorts(value.FixedPorts, true),
                    valuePorts = ToPackagePorts(value.FixedPorts, false),
                    canCreate = true,
                    canConfigure = true
                })
                .GroupBy(value => value.kind, StringComparer.Ordinal)
                .Select(value => value.First())
                .OrderBy(value => value.kind, StringComparer.Ordinal)
                .ToList();
        }

        public static bool ValidateCatalog(AgentCompileReport report)
        {
            bool valid = true;
            IReadOnlyList<AgentPackageSkillNodeKindDescriptor> exported =
                ExportCatalog();
            var kinds = new HashSet<string>(StringComparer.Ordinal);
            foreach (AgentPackageSkillNodeKindDescriptor descriptor in exported)
            {
                if (descriptor == null ||
                    string.IsNullOrWhiteSpace(descriptor.kind) ||
                    !kinds.Add(descriptor.kind))
                {
                    report.Error(
                        "capabilities",
                        "skill_capability_export_invalid",
                        "Skill capability metadata投影包含无效或重复kind。");
                    valid = false;
                }
            }
            var formal = BtsmtlSkillGraphAuthoringMetadata
                .GetDomainCapabilities()
                .Where(value => !value.SystemOwned)
                .ToDictionary(value => value.ExternalKind, StringComparer.Ordinal);
            foreach (GraphAuthoringCapabilityDescriptor capability in formal.Values)
            {
                AgentPackageSkillNodeKindDescriptor package = exported
                    .SingleOrDefault(value => value.kind == capability.ExternalKind);
                if (package == null ||
                    !package.properties.SequenceEqual(
                        capability.Fields
                            .Where(field => field.AuthoringVisible)
                            .Select(field => field.FieldId.Value)
                            .OrderBy(field => field, StringComparer.Ordinal)))
                {
                    report.Error(
                        "capabilities." + capability.ExternalKind,
                        "skill_capability_projection_mismatch",
                        "Skill package capability与正式metadata不一致。");
                    valid = false;
                }
            }
            return valid;
        }

        public static bool ValidateProperties(
            string kind,
            JObject properties,
            AgentCompileReport report,
            string path)
        {
            GraphAuthoringCapabilityDescriptor capability = BtsmtlSkillGraphAuthoringMetadata.Require(kind);
            bool valid = true;
            foreach (JProperty property in properties?.Properties() ?? Enumerable.Empty<JProperty>())
            {
                if (!capability.TryGetField(
                        new GraphAuthoringFieldId(property.Name),
                        out GraphAuthoringFieldDescriptor field) ||
                    !field.AuthoringWritable)
                {
                    report.Error(
                        path + "." + property.Name,
                        "skill_node_property_unknown",
                        "Skill Node property未在正式metadata中声明。");
                    valid = false;
                    continue;
                }
                if (!BtsmtlAuthoringFieldValueValidation.Matches(
                        field,
                        property.Value))
                {
                    report.Error(
                        path + "." + property.Name,
                        "skill_node_property_invalid",
                        "Skill Node property类型或约束不符合正式metadata。");
                    valid = false;
                }
            }
            foreach (GraphAuthoringFieldDescriptor field in capability.Fields)
            {
                if (field.Optional || HasValue(properties?[field.FieldId.Value]))
                    continue;
                report.Error(
                    path + "." + field.FieldId.Value,
                    "skill_node_property_required",
                    $"Skill Node必须声明{field.FieldId.Value}。");
                valid = false;
            }
            return valid;
        }

        public static bool TryGetKind(FlowNode node, out string kind) =>
            BtsmtlSkillGraphAuthoringMetadata.TryGetKind(node, out kind);

        public static bool TryGetKind(Type type, out string kind) =>
            BtsmtlSkillGraphAuthoringMetadata.TryGetKind(type, out kind);

        public static bool TryResolveType(string kind, out Type type) =>
            BtsmtlSkillGraphAuthoringMetadata.TryResolveType(kind, out type);

        public static bool IsAnchor(string kind) =>
            BtsmtlSkillGraphAuthoringMetadata.IsAnchor(kind);

        public static bool IsAllowed(
            AgentPackageSkillFlowNode node,
            BtsmtlSkillFlowGraphRole role) =>
            BtsmtlSkillGraphAuthoringMetadata.IsAllowed(
                node?.capability,
                role);

        public static bool IsAllowed(
            string kind,
            BtsmtlSkillFlowGraphRole role) =>
            BtsmtlSkillGraphAuthoringMetadata.IsAllowed(kind, role);

        public static IReadOnlyList<AgentPackagePortDescriptor> ProjectPorts(
            AgentPackageSkillFlowNode node,
            AgentPackageSkillFlowGraphFile graph,
            IReadOnlyList<AgentPackageSkillMacroFile> macros)
        {
            if (node == null)
                return Array.Empty<AgentPackagePortDescriptor>();
            string kind = node.capability ?? string.Empty;
            GraphAuthoringCapabilityDescriptor capability =
                BtsmtlSkillGraphAuthoringMetadata.Require(kind);
            var dynamicPorts = new List<GraphAuthoringDynamicPortProjection>();
            switch (capability.DynamicPortSource)
            {
                case GraphAuthoringDynamicPortSource.MacroParameters:
                    string macroId = node.properties?.Value<string>("graphId");
                    AgentPackageSkillMacroFile macro = (macros ??
                            new List<AgentPackageSkillMacroFile>())
                        .FirstOrDefault(value => value != null && value.id == macroId);
                    int macroOrder = 0;
                    AddMacroParameterPorts(
                        dynamicPorts,
                        macro?.inputs,
                        true,
                        ref macroOrder);
                    AddMacroParameterPorts(
                        dynamicPorts,
                        macro?.outputs,
                        false,
                        ref macroOrder);
                    break;
                case GraphAuthoringDynamicPortSource.BlackboardValue:
                    return ProjectBlackboardPorts(node);
                case GraphAuthoringDynamicPortSource.OrderedSteps:
                    int stepOrder = 100;
                    foreach (JObject step in (node.properties?["steps"] as JArray)
                                 ?.OfType<JObject>() ?? Enumerable.Empty<JObject>())
                    {
                        dynamicPorts.Add(
                            BtsmtlSkillGraphAuthoringMetadata.ProjectCompositeStepPort(
                                kind,
                                step.Value<string>("id"),
                                step.Value<string>("name"),
                                stepOrder++));
                    }
                    break;
            }
            return ToPackagePorts(BtsmtlSkillGraphAuthoringMetadata.ProjectPorts(
                kind,
                Array.Empty<GraphAuthoringTypedPropertyValue>(),
                dynamicPorts));
        }

        public static IReadOnlyList<AgentPackagePortDescriptor> ProjectAnchorPorts(
            AgentPackageSkillGraphAnchor anchor,
            AgentPackageSkillFlowGraphFile graph,
            IReadOnlyList<AgentPackageSkillMacroFile> macros)
        {
            if (anchor == null)
                return Array.Empty<AgentPackagePortDescriptor>();
            GraphAuthoringCapabilityDescriptor capability =
                BtsmtlSkillGraphAuthoringMetadata.Require(anchor.kind);
            var dynamic = new List<GraphAuthoringDynamicPortProjection>();
            switch (capability.DynamicPortSource)
            {
                case GraphAuthoringDynamicPortSource.OrderedSteps:
                    int stepOrder = 100;
                    foreach (AgentPackageSkillFlowStep step in
                             anchor.steps ?? new List<AgentPackageSkillFlowStep>())
                    {
                        dynamic.Add(
                            BtsmtlSkillGraphAuthoringMetadata.ProjectAnchorStepPort(
                                anchor.kind,
                                step.id,
                                step.name,
                                stepOrder++));
                    }
                    break;
                case GraphAuthoringDynamicPortSource.MacroInputs:
                case GraphAuthoringDynamicPortSource.MacroOutputs:
                    AgentPackageSkillMacroFile macro = (macros ??
                            new List<AgentPackageSkillMacroFile>())
                        .FirstOrDefault(value => value != null && value.id == graph?.id);
                    IEnumerable<AgentPackageSkillMacroParameter> parameters =
                        capability.DynamicPortSource ==
                        GraphAuthoringDynamicPortSource.MacroInputs
                            ? macro?.inputs ?? new List<AgentPackageSkillMacroParameter>()
                            : macro?.outputs ?? new List<AgentPackageSkillMacroParameter>();
                    int parameterOrder = 0;
                    foreach (AgentPackageSkillMacroParameter parameter in parameters)
                    {
                        dynamic.Add(
                            BtsmtlSkillGraphAuthoringMetadata.ProjectMacroInterfacePort(
                                anchor.kind,
                                parameter.id,
                                parameter.name,
                                parameter.valueType,
                                parameterOrder++));
                    }
                    break;
            }
            return ToPackagePorts(BtsmtlSkillGraphAuthoringMetadata.ProjectPorts(
                anchor.kind,
                Array.Empty<GraphAuthoringTypedPropertyValue>(),
                dynamic));
        }

        static void AddMacroParameterPorts(
            ICollection<GraphAuthoringDynamicPortProjection> target,
            IEnumerable<AgentPackageSkillMacroParameter> parameters,
            bool input,
            ref int order)
        {
            foreach (AgentPackageSkillMacroParameter parameter in
                     parameters ?? Enumerable.Empty<AgentPackageSkillMacroParameter>())
            {
                target.Add(
                    BtsmtlSkillGraphAuthoringMetadata.ProjectMacroParameterPort(
                        parameter.id,
                        parameter.name,
                        parameter.valueType,
                        input,
                        order++));
            }
        }

        public static string ValueType(Type type) =>
            BtsmtlSkillGraphAuthoringMetadata.ValueType(type);

        public static bool TryResolveValueType(string value, out Type type) =>
            BtsmtlSkillGraphAuthoringMetadata.TryResolveValueType(value, out type);

        static IReadOnlyList<AgentPackagePortDescriptor> ProjectBlackboardPorts(
            AgentPackageSkillFlowNode node)
        {
            string valueType = node.properties?.Value<string>("valueType") ??
                string.Empty;
            string accessMode = node.properties?.Value<string>("accessMode") ??
                string.Empty;
            return ToPackagePorts(
                BtsmtlSkillGraphAuthoringMetadata.ProjectBlackboardPorts(
                    valueType,
                    accessMode));
        }

        static bool HasValue(JToken token)
        {
            return token != null &&
                   token.Type != JTokenType.Null &&
                   (token.Type != JTokenType.String ||
                    !string.IsNullOrWhiteSpace(token.Value<string>()));
        }

        static List<AgentPackagePortDescriptor> ToPackagePorts(
            IEnumerable<GraphAuthoringPortDescriptor> ports,
            bool flow)
        {
            return (ports ?? Array.Empty<GraphAuthoringPortDescriptor>())
                .Where(port => (port.ValueTypeId == "flow") == flow)
                .OrderBy(port => port.Order)
                .Select(port => new AgentPackagePortDescriptor
                {
                    key = port.PortId.Value,
                    direction = port.Direction.ToString(),
                    valueType = flow ? string.Empty : port.ValueTypeId,
                    capacity = port.Capacity.ToString(),
                    required = port.Required
                })
                .ToList();
        }

        static List<AgentPackagePortDescriptor> ToPackagePorts(
            IEnumerable<GraphAuthoringDynamicPortProjection> ports)
        {
            return (ports ?? Array.Empty<GraphAuthoringDynamicPortProjection>())
                .OrderBy(port => port.Order)
                .Select(port => new AgentPackagePortDescriptor
                {
                    key = port.PortId.Value,
                    direction = port.Direction.ToString(),
                    valueType = port.ValueTypeId == "flow"
                        ? string.Empty
                        : port.ValueTypeId,
                    capacity = port.Capacity.ToString(),
                    required = port.Required
                })
                .ToList();
        }

    }
}
