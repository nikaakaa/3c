using System;
using System.Collections.Generic;
using System.Linq;
using FlowCanvas;
using FlowCanvas.Macros;
using FlowCanvas.Nodes;
using NodeCanvas.Framework;
using Newtonsoft.Json.Linq;
using ThirdPersonCharacter.Control.Authoring;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.Pipeline.Motion;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    internal static class AgentSkillFlowAuthoringCapabilities
    {

        public static IReadOnlyList<AgentPackageSkillNodeKindDescriptor> ExportCatalog()
        {
            var result = new List<AgentPackageSkillNodeKindDescriptor>();
            foreach (BtsmtlSkillNodeCapability capability in BtsmtlSkillCapabilityCatalog.All)
            {
                Type type = capability.NodeType;
                string kind = capability.Kind;
                if (type == typeof(BtsmtlSkillBlackboardSetFlowNode) || IsAnchor(kind))
                    continue;
                FlowNode prototype = (FlowNode)Activator.CreateInstance(type);
                if (prototype is BtsmtlSkillCompositeFlowNode composite)
                    composite.SetSteps(Array.Empty<BtsmtlSkillStepPort>());
                prototype.GatherPorts();
                result.Add(new AgentPackageSkillNodeKindDescriptor
                {
                    kind = kind,
                    graphRoles = AllowedRoles(type).Select(value => value.ToString()).ToList(),
                    properties = Properties(kind).ToList(),
                    flowPorts = Ports(prototype, true),
                    valuePorts = Ports(prototype, false),
                    canCreate = true,
                    canConfigure = true
                });
            }
            return result
                .GroupBy(value => value.kind, StringComparer.Ordinal)
                .Select(value => value.First())
                .OrderBy(value => value.kind, StringComparer.Ordinal)
                .ToList();
        }

        public static bool ValidateCatalog(AgentCompileReport report)
        {
            bool valid = true;
            var descriptors = ExportCatalog()
                .GroupBy(value => value.kind, StringComparer.Ordinal)
                .ToDictionary(value => value.Key, value => value.First(), StringComparer.Ordinal);
            foreach (BtsmtlSkillNodeCapability capability in BtsmtlSkillCapabilityCatalog.All)
            {
                if (capability.NodeType == typeof(BtsmtlSkillBlackboardSetFlowNode) || IsAnchor(capability.Kind))
                    continue;
                if (!descriptors.TryGetValue(capability.Kind, out AgentPackageSkillNodeKindDescriptor descriptor))
                {
                    report.Error("capabilities." + capability.Kind, "skill_capability_export_missing", "技能Capability没有进入Document目录。");
                    valid = false;
                    continue;
                }
                if (!typeof(BtsmtlSkillCompositeFlowNode).IsAssignableFrom(capability.NodeType) &&
                    capability.NodeType != typeof(MacroNodeWrapper) &&
                    capability.NodeType != typeof(BtsmtlSkillBlackboardAccessFlowNode))
                {
                    FlowNode prototype = (FlowNode)Activator.CreateInstance(capability.NodeType);
                    prototype.GatherPorts();
                    foreach (BtsmtlSkillPortShape shape in BtsmtlSkillCapabilityCatalog.ProjectPorts(prototype))
                    {
                        AgentPackagePortDescriptor port = (shape.IsFlow ? descriptor.flowPorts : descriptor.valuePorts)
                            .FirstOrDefault(value =>
                                value.key == shape.Id &&
                                value.direction == (shape.IsInput ? "Input" : "Output"));
                        if (port == null ||
                            port.valueType != (shape.IsFlow ? string.Empty : ValueType(shape.ValueType)) ||
                            port.capacity != (shape.Multiple ? "Multiple" : "Single") ||
                            port.required != shape.Required)
                        {
                            report.Error("capabilities." + capability.Kind + ".ports." + shape.Id,
                                "skill_capability_port_shape_mismatch",
                                "技能Capability固定Port Shape与Document目录不一致。");
                            valid = false;
                        }
                    }
                }
            }
            return valid;
        }

        public static bool TryGetKind(FlowNode node, out string kind)
        {
            kind = null;
            return BtsmtlSkillCapabilityCatalog.TryGetKind(node, out kind);
        }

        public static bool TryGetKind(Type type, out string kind)
        {
            kind = null;
            return BtsmtlSkillCapabilityCatalog.TryGetKind(type, out kind);
        }

        public static bool TryResolveType(string kind, out Type type)
        {
            return BtsmtlSkillCapabilityCatalog.TryResolveType(kind, out type);
        }

        public static bool IsAnchor(string kind) => BtsmtlSkillCapabilityCatalog.IsAnchor(kind);

        public static bool IsAllowed(string kind, BtsmtlSkillFlowGraphRole role)
        {
            if (!TryResolveType(kind, out Type type))
                return false;
            return AllowedRoles(type).Contains(role);
        }

        public static bool IsAllowed(AgentPackageSkillFlowNode node, BtsmtlSkillFlowGraphRole role)
        {
            if (node?.capability == "exposed-property" &&
                node.properties?.Value<string>("accessMode") == "set")
                return AllowedRoles(typeof(BtsmtlSkillBlackboardSetFlowNode)).Contains(role);
            return IsAllowed(node?.capability, role);
        }

        public static IReadOnlyList<AgentPackagePortDescriptor> ProjectPorts(
            AgentPackageSkillFlowNode node,
            AgentPackageSkillFlowGraphFile graph,
            IReadOnlyList<AgentPackageSkillMacroFile> macros)
        {
            var result = new List<AgentPackagePortDescriptor>();
            string kind = node?.capability ?? string.Empty;
            if (kind == "macro-call")
            {
                string macroId = node.properties?.Value<string>("graphId");
                AgentPackageSkillMacroFile macro = (macros ?? new List<AgentPackageSkillMacroFile>())
                    .FirstOrDefault(value => value != null && value.id == macroId);
                foreach (AgentPackageSkillMacroParameter parameter in macro?.inputs ?? new List<AgentPackageSkillMacroParameter>())
                    result.Add(ParameterPort(parameter, true));
                foreach (AgentPackageSkillMacroParameter parameter in macro?.outputs ?? new List<AgentPackageSkillMacroParameter>())
                    result.Add(ParameterPort(parameter, false));
                return result;
            }
            if (kind == "exposed-property")
            {
                bool write = node.properties?.Value<string>("accessMode") == "set";
                if (write)
                {
                    result.Add(FlowPort("Input", "Input"));
                    result.Add(ValuePort("m_Value", "Input", ValueTypeFromNode(node)));
                }
                else
                    result.Add(ValuePort("m_Output", "Output", ValueTypeFromNode(node)));
                return result;
            }
            if (kind == "sequence" || kind == "selector" || kind == "parallel" || kind == "state")
            {
                string input = kind == "state" ? "StateIn" : "Input";
                result.Add(FlowPort(input, "Input"));
                foreach (JObject step in (node.properties?["steps"] as JArray)?.OfType<JObject>() ?? Enumerable.Empty<JObject>())
                    result.Add(FlowPort(step.Value<string>("id"), "Output"));
                return result;
            }
            if (TryResolveType(kind, out Type type))
            {
                FlowNode prototype = (FlowNode)Activator.CreateInstance(type);
                prototype.GatherPorts();
                result.AddRange(Ports(prototype, true));
                result.AddRange(Ports(prototype, false));
            }
            return result;
        }

        public static string ValueType(Type type)
        {
            if (type == typeof(Flow))
                return "flow";
            if (type == typeof(bool))
                return "bool";
            if (type == typeof(int))
                return "int";
            if (type == typeof(float))
                return "float";
            if (type == typeof(string))
                return "string";
            if (type == typeof(uint))
                return "uint";
            if (type == typeof(ulong))
                return "ulong";
            if (type == typeof(UnityEngine.Vector2))
                return "vector2";
            if (type == typeof(UnityEngine.Vector3))
                return "vector3";
            if (type == typeof(ActionTargetSnapshot))
                return "action-target-snapshot";
            return string.Empty;
        }

        public static bool TryResolveValueType(string value, out Type type)
        {
            type = value?.Trim().ToLowerInvariant() switch
            {
                "flow" => typeof(Flow),
                "bool" => typeof(bool),
                "int" => typeof(int),
                "float" => typeof(float),
                "string" => typeof(string),
                "uint" => typeof(uint),
                "ulong" => typeof(ulong),
                "vector2" => typeof(UnityEngine.Vector2),
                "vector3" => typeof(UnityEngine.Vector3),
                "action-target-snapshot" => typeof(ActionTargetSnapshot),
                _ => null
            };
            return type != null;
        }

        static IReadOnlyList<BtsmtlSkillFlowGraphRole> AllowedRoles(Type type)
        {
            return Enum.GetValues(typeof(BtsmtlSkillFlowGraphRole))
                .Cast<BtsmtlSkillFlowGraphRole>()
                .Where(value => BtsmtlSkillFlowGraphRules.Allows(type, value, value == BtsmtlSkillFlowGraphRole.Subgraph))
                .ToArray();
        }

        static IReadOnlyList<string> Properties(string kind) => BtsmtlSkillCapabilityCatalog.Properties(kind);

        static AgentPackagePortDescriptor ParameterPort(AgentPackageSkillMacroParameter parameter, bool input)
        {
            return parameter?.valueType == "flow"
                ? FlowPort(parameter.id, input ? "Input" : "Output")
                : ValuePort(parameter?.id, input ? "Input" : "Output", parameter?.valueType);
        }

        static AgentPackagePortDescriptor FlowPort(string key, string direction)
        {
            return new AgentPackagePortDescriptor
            {
                key = key,
                direction = direction,
                valueType = string.Empty,
                capacity = direction == "Input" ? "Multiple" : "Single"
            };
        }

        static AgentPackagePortDescriptor ValuePort(string key, string direction, string valueType)
        {
            return new AgentPackagePortDescriptor
            {
                key = key,
                direction = direction,
                valueType = valueType,
                capacity = direction == "Input" ? "Single" : "Multiple"
            };
        }

        static string ValueTypeFromNode(AgentPackageSkillFlowNode node) =>
            node.properties?.Value<string>("valueType") ?? string.Empty;

        static List<AgentPackagePortDescriptor> Ports(FlowNode node, bool flow)
        {
            return BtsmtlSkillCapabilityCatalog.ProjectPorts(node)
                .Where(value => value.IsFlow == flow)
                .Select(value => new AgentPackagePortDescriptor
                {
                    key = value.Id,
                    direction = value.IsInput ? "Input" : "Output",
                    valueType = value.IsFlow ? string.Empty : ValueType(value.ValueType),
                    capacity = value.Multiple ? "Multiple" : "Single",
                    required = value.Required
                })
                .ToList();
        }
    }
}
