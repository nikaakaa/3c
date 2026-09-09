#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using FlowCanvas;
using FlowCanvas.Macros;
using FlowCanvas.Nodes;
using NodeCanvas.Framework;

namespace ThirdPersonCharacter.Control.Authoring
{
    public sealed class BtsmtlSkillNodeCapability
    {
        internal BtsmtlSkillNodeCapability(Type nodeType, string kind, IReadOnlyList<string> properties)
        {
            NodeType = nodeType;
            Kind = kind;
            Properties = properties;
        }

        public Type NodeType { get; }
        public string Kind { get; }
        public IReadOnlyList<string> Properties { get; }
        public bool IsAnchor => BtsmtlSkillCapabilityCatalog.IsAnchor(Kind);
    }

    public readonly struct BtsmtlSkillPortShape
    {
        internal BtsmtlSkillPortShape(string id, Type valueType, bool isFlow, bool isInput, bool multiple, bool required)
        {
            Id = id;
            ValueType = valueType;
            IsFlow = isFlow;
            IsInput = isInput;
            Multiple = multiple;
            Required = required;
        }

        public string Id { get; }
        public Type ValueType { get; }
        public bool IsFlow { get; }
        public bool IsInput { get; }
        public bool Multiple { get; }
        public bool Required { get; }
    }

    public static class BtsmtlSkillCapabilityCatalog
    {
        static readonly IReadOnlyList<BtsmtlSkillNodeCapability> s_All = Create();
        static readonly IReadOnlyDictionary<Type, BtsmtlSkillNodeCapability> s_ByType =
            s_All.ToDictionary(value => value.NodeType);
        static readonly IReadOnlyDictionary<string, BtsmtlSkillNodeCapability> s_ByKind =
            s_All.GroupBy(value => value.Kind, StringComparer.Ordinal)
                .ToDictionary(value => value.Key, value => value.First(), StringComparer.Ordinal);

        public static IReadOnlyList<BtsmtlSkillNodeCapability> All => s_All;

        public static bool TryGet(Type type, out BtsmtlSkillNodeCapability capability)
        {
            capability = null;
            return type != null && s_ByType.TryGetValue(type, out capability);
        }

        public static bool TryGet(FlowNode node, out BtsmtlSkillNodeCapability capability) =>
            TryGet(node?.GetType(), out capability);

        public static bool TryResolveType(string kind, out Type type)
        {
            if (s_ByKind.TryGetValue(kind ?? string.Empty, out BtsmtlSkillNodeCapability capability))
            {
                type = capability.NodeType;
                return true;
            }
            type = null;
            return false;
        }

        public static bool TryGetKind(Type type, out string kind)
        {
            if (TryGet(type, out BtsmtlSkillNodeCapability capability))
            {
                kind = capability.Kind;
                return true;
            }
            kind = null;
            return false;
        }

        public static bool TryGetKind(FlowNode node, out string kind) =>
            TryGetKind(node?.GetType(), out kind);

        public static IReadOnlyList<string> Properties(string kind) =>
            s_ByKind.TryGetValue(kind ?? string.Empty, out BtsmtlSkillNodeCapability capability)
                ? capability.Properties
                : Array.Empty<string>();

        public static bool IsAnchor(string kind) =>
            kind == "@root" || kind == "@enter" || kind == "@any" ||
            kind == "@exit" || kind == "@onEnter" || kind == "@onExit" ||
            kind == "@timelineEnable" || kind == "@timelineDisable" ||
            kind == "@timelineDestroy" || kind == "@result" ||
            kind == "@input" || kind == "@output";

        public static IReadOnlyList<BtsmtlSkillPortShape> ProjectPorts(Type type)
        {
            if (!TryGet(type, out _))
                throw new InvalidOperationException($"技能节点类型未登记：{type?.FullName}");
            var node = (FlowNode)Activator.CreateInstance(type);
            node.GatherPorts();
            return ProjectPorts(node);
        }

        public static IReadOnlyList<BtsmtlSkillPortShape> ProjectPorts(FlowNode node)
        {
            if (!TryGet(node, out _))
                throw new InvalidOperationException("技能节点类型未登记，不能投影端口。");
            return ProjectPorts(node, true)
                .Concat(ProjectPorts(node, false))
                .OrderBy(value => value.Id, StringComparer.Ordinal)
                .ThenBy(value => value.IsInput ? 0 : 1)
                .ToArray();
        }

        static IEnumerable<BtsmtlSkillPortShape> ProjectPorts(FlowNode node, bool flow)
        {
            IEnumerable<Port> ports = flow
                ? node.GetInputFlowPorts().Cast<Port>().Concat(node.GetOutputFlowPorts().Cast<Port>())
                : node.GetInputValuePorts().Cast<Port>().Concat(node.GetOutputValuePorts().Cast<Port>());
            foreach (Port port in ports)
            {
                bool multiple = port is FlowInput || port is ValueOutput;
                bool required = port is ValueInput input && input.isRequired;
                yield return new BtsmtlSkillPortShape(
                    port.ID,
                    flow ? typeof(Flow) : port.type,
                    flow,
                    port.IsInputPort(),
                    multiple,
                    required);
            }
        }

        static IReadOnlyList<BtsmtlSkillNodeCapability> Create()
        {
            var result = new List<BtsmtlSkillNodeCapability>();
            foreach (Type type in BtsmtlSkillNodeCatalog.All)
            {
                string kind;
                if (BtsmtlSkillNativeNodeCatalog.TryGet(type, out BtsmtlSkillNativeNodeContract native))
                    kind = native.Kind;
                else if (type == typeof(MacroNodeWrapper))
                    kind = "macro-call";
                else if (typeof(MacroInputNode).IsAssignableFrom(type))
                    kind = "@input";
                else if (typeof(MacroOutputNode).IsAssignableFrom(type))
                    kind = "@output";
                else if (typeof(BtsmtlSkillFlowNode).IsAssignableFrom(type))
                    kind = ((BtsmtlSkillFlowNode)Activator.CreateInstance(type)).CapabilityId;
                else
                    continue;
                result.Add(new BtsmtlSkillNodeCapability(type, kind, PropertyNames(kind)));
            }
            return result.AsReadOnly();
        }

        static IReadOnlyList<string> PropertyNames(string kind) => kind switch
        {
            "sequence" or "selector" => new[] { "steps" },
            "loop" => new[] { "stopType" },
            "parallel" => new[] { "mode", "steps" },
            "state-exit-cause" => new[] { "cause" },
            "character-input-bool" or "character-input-float" or
                "character-input-vector2" or "character-input-vector2-magnitude" or
                "character-action-request" => new[] { "inputId", "providerOwnerId" },
            "action-context-active" => new[] { "actionContext" },
            "action-window-active" => new[] { "windowType" },
            "can-activate-action" => new[] { "actionProfile", "targetSnapshot" },
            "submit-action-lifecycle" => new[] { "actionContext", "transitionType", "reason" },
            "gameplay-tag-has" => new[] { "tagId", "providerOwnerId" },
            "gameplay-tag-query" => new[] { "query", "providerOwnerId" },
            "gameplay-attribute-read" => new[] { "attributeId", "providerOwnerId" },
            "gameplay-effect-apply" => new[] { "effect", "actionContext", "predicted", "providerOwnerId" },
            "gameplay-effect-remove" => new[] { "selector", "handle", "effect", "query", "providerOwnerId" },
            "character-move-facing-angle" => new[] { "providerOwnerId" },
            "character-state-vector3" or "character-state-scalar" or
                "character-state-yaw" or "character-state-bool" => new[] { "fieldId", "providerOwnerId" },
            "pipeline-blackboard-bool" or "pipeline-blackboard-float" => new[] { "declarationId", "ownerId", "valueType" },
            "exposed-property" => new[] { "declarationId", "ownerId", "valueType", "accessMode", "factContext" },
            "state-machine" => new[] { "graphId" },
            "state" => new[] { "bodyGraphId", "steps" },
            "timeline" => new[] { "timelineId", "timelineOwnership", "actionContext", "playbackMode" },
            "locomotion-input-motion" => new[]
            {
                "moveSpeed", "displacementMode", "turnSpeedDegrees", "cameraRelative",
                "executionMode", "durationSeconds", "actionMotionCurve"
            },
            "macro-call" => new[] { "graphId" },
            _ => Array.Empty<string>()
        };
    }
}
#endif
