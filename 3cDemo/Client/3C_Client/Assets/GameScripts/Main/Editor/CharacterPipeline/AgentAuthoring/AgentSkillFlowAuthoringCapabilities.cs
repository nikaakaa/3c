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
        static readonly Type[] s_NodeTypes =
        {
            typeof(BtsmtlSkillSequenceFlowNode),
            typeof(BtsmtlSkillSelectorFlowNode),
            typeof(BtsmtlSkillLoopFlowNode),
            typeof(BtsmtlSkillParallelFlowNode),
            typeof(BtsmtlSkillSucceedFlowNode),
            typeof(BtsmtlSkillStateMachineFlowNode),
            typeof(BtsmtlSkillStateFlowNode),
            typeof(BtsmtlSkillStateRootCompletedFlowNode),
            typeof(BtsmtlSkillStateExitCauseFlowNode),
            typeof(BtsmtlSkillTimelineFlowNode),
            typeof(BtsmtlSkillBooleanInputFlowNode),
            typeof(BtsmtlSkillScalarInputFlowNode),
            typeof(BtsmtlSkillVector2InputFlowNode),
            typeof(BtsmtlSkillInputMagnitudeFlowNode),
            typeof(BtsmtlSkillActionRequestFlowNode),
            typeof(BtsmtlSkillActionContextActiveFlowNode),
            typeof(BtsmtlSkillActionWindowActiveFlowNode),
            typeof(BtsmtlSkillCanActivateActionFlowNode),
            typeof(BtsmtlSkillSubmitActionLifecycleFlowNode),
            typeof(BtsmtlSkillMoveFacingAngleFlowNode),
            typeof(BtsmtlSkillBlackboardBooleanFlowNode),
            typeof(BtsmtlSkillBlackboardScalarFlowNode),
            typeof(BtsmtlSkillBlackboardGetFlowNode),
            typeof(BtsmtlSkillBlackboardSetFlowNode),
            typeof(BtsmtlSkillLocomotionFlowNode)
        };

        static readonly IReadOnlyDictionary<string, Type> s_Types = CreateTypes();

        public static IReadOnlyList<AgentPackageSkillNodeKindDescriptor> ExportCatalog()
        {
            var result = new List<AgentPackageSkillNodeKindDescriptor>();
            foreach (Type type in s_NodeTypes
                         .Concat(new[] { typeof(MacroNodeWrapper) })
                         .Concat(BtsmtlSkillNativeNodeCatalog.All.Select(value => value.NodeType)))
            {
                if (!TryGetKind(type, out string kind) || IsAnchor(kind))
                    continue;
                FlowNode prototype = (FlowNode)Activator.CreateInstance(type);
                prototype.GatherPorts();
                result.Add(new AgentPackageSkillNodeKindDescriptor
                {
                    kind = kind,
                    graphRoles = AllowedRoles(type).Select(value => value.ToString()).ToList(),
                    properties = Properties(kind),
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

        public static bool TryGetKind(FlowNode node, out string kind)
        {
            kind = null;
            if (node is BtsmtlSkillTimelineEnableFlowNode)
            {
                kind = "@timelineEnable";
                return true;
            }
            if (node is BtsmtlSkillTimelineDisableFlowNode)
            {
                kind = "@timelineDisable";
                return true;
            }
            if (node is BtsmtlSkillTimelineDestroyFlowNode)
            {
                kind = "@timelineDestroy";
                return true;
            }
            if (node is BtsmtlSkillFlowNode skill)
            {
                kind = skill.CapabilityId;
                return !string.IsNullOrWhiteSpace(kind);
            }
            if (node is MacroNodeWrapper)
            {
                kind = "macro-call";
                return true;
            }
            if (node is MacroInputNode)
            {
                kind = "@input";
                return true;
            }
            if (node is MacroOutputNode)
            {
                kind = "@output";
                return true;
            }
            if (BtsmtlSkillNativeNodeCatalog.TryGet(node.GetType(), out BtsmtlSkillNativeNodeContract contract))
            {
                kind = contract.Kind;
                return true;
            }
            return false;
        }

        public static bool TryGetKind(Type type, out string kind)
        {
            kind = null;
            if (type == typeof(BtsmtlSkillTimelineEnableFlowNode))
            {
                kind = "@timelineEnable";
                return true;
            }
            if (type == typeof(BtsmtlSkillTimelineDisableFlowNode))
            {
                kind = "@timelineDisable";
                return true;
            }
            if (type == typeof(BtsmtlSkillTimelineDestroyFlowNode))
            {
                kind = "@timelineDestroy";
                return true;
            }
            if (type == typeof(MacroNodeWrapper))
            {
                kind = "macro-call";
                return true;
            }
            if (type == typeof(MacroInputNode))
            {
                kind = "@input";
                return true;
            }
            if (type == typeof(MacroOutputNode))
            {
                kind = "@output";
                return true;
            }
            if (BtsmtlSkillNativeNodeCatalog.TryGet(type, out BtsmtlSkillNativeNodeContract contract))
            {
                kind = contract.Kind;
                return true;
            }
            if (!typeof(BtsmtlSkillFlowNode).IsAssignableFrom(type))
                return false;
            FlowNode node = (FlowNode)Activator.CreateInstance(type);
            kind = ((BtsmtlSkillFlowNode)node).CapabilityId;
            return !string.IsNullOrWhiteSpace(kind);
        }

        public static bool TryResolveType(string kind, out Type type)
        {
            return s_Types.TryGetValue(kind ?? string.Empty, out type);
        }

        public static bool IsAnchor(string kind)
        {
            return kind == "@root" || kind == "@enter" || kind == "@any" ||
                   kind == "@exit" || kind == "@onEnter" || kind == "@onExit" ||
                   kind == "@timelineEnable" || kind == "@timelineDisable" ||
                   kind == "@timelineDestroy" || kind == "@result" ||
                   kind == "@input" || kind == "@output";
        }

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
            if (kind == "loop")
            {
                result.Add(FlowPort("Input", "Input"));
                result.Add(FlowPort("Output", "Output"));
                return result;
            }
            if (kind == "locomotion-input-motion")
            {
                result.Add(FlowPort("Input", "Input"));
                result.Add(ValuePort("m_MoveInput", "Input", "vector2"));
                return result;
            }
            if (kind == "submit-action-lifecycle")
            {
                result.Add(FlowPort("Input", "Input"));
                result.Add(ValuePort("m_Submitted", "Output", "bool"));
                return result;
            }
            if (kind == "character-move-facing-angle")
            {
                result.Add(ValuePort("m_MoveInput", "Input", "vector2"));
                result.Add(ValuePort("m_Output", "Output", "float"));
                return result;
            }
            if (kind == "pipeline-blackboard-bool")
            {
                result.Add(ValuePort("m_Output", "Output", "bool"));
                return result;
            }
            if (kind == "pipeline-blackboard-float")
            {
                result.Add(ValuePort("m_Output", "Output", "float"));
                return result;
            }
            if (kind == "character-input-bool" || kind == "character-action-request" ||
                kind == "action-context-active" || kind == "action-window-active" ||
                kind == "can-activate-action" || kind == "state-root-completed" ||
                kind == "state-exit-cause")
            {
                result.Add(ValuePort("m_Output", "Output", "bool"));
                return result;
            }
            if (kind == "character-input-float" || kind == "character-input-vector2-magnitude")
            {
                result.Add(ValuePort("m_Output", "Output", "float"));
                return result;
            }
            if (kind == "character-input-vector2")
            {
                result.Add(ValuePort("m_Output", "Output", "vector2"));
                return result;
            }
            if (kind == "state-machine" || kind == "timeline")
            {
                result.Add(FlowPort("Input", "Input"));
                return result;
            }
            AgentPackageSkillNodeKindDescriptor descriptor = ExportCatalog()
                .FirstOrDefault(value => value.kind == kind);
            if (descriptor != null)
                result.AddRange(descriptor.flowPorts.Concat(descriptor.valuePorts));
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

        static IReadOnlyDictionary<string, Type> CreateTypes()
        {
            var result = new Dictionary<string, Type>(StringComparer.Ordinal)
            {
                ["sequence"] = typeof(BtsmtlSkillSequenceFlowNode),
                ["selector"] = typeof(BtsmtlSkillSelectorFlowNode),
                ["loop"] = typeof(BtsmtlSkillLoopFlowNode),
                ["parallel"] = typeof(BtsmtlSkillParallelFlowNode),
                ["succeed"] = typeof(BtsmtlSkillSucceedFlowNode),
                ["state-machine"] = typeof(BtsmtlSkillStateMachineFlowNode),
                ["state"] = typeof(BtsmtlSkillStateFlowNode),
                ["state-root-completed"] = typeof(BtsmtlSkillStateRootCompletedFlowNode),
                ["state-exit-cause"] = typeof(BtsmtlSkillStateExitCauseFlowNode),
                ["timeline"] = typeof(BtsmtlSkillTimelineFlowNode),
                ["character-input-bool"] = typeof(BtsmtlSkillBooleanInputFlowNode),
                ["character-input-float"] = typeof(BtsmtlSkillScalarInputFlowNode),
                ["character-input-vector2"] = typeof(BtsmtlSkillVector2InputFlowNode),
                ["character-input-vector2-magnitude"] = typeof(BtsmtlSkillInputMagnitudeFlowNode),
                ["character-action-request"] = typeof(BtsmtlSkillActionRequestFlowNode),
                ["action-context-active"] = typeof(BtsmtlSkillActionContextActiveFlowNode),
                ["action-window-active"] = typeof(BtsmtlSkillActionWindowActiveFlowNode),
                ["can-activate-action"] = typeof(BtsmtlSkillCanActivateActionFlowNode),
                ["submit-action-lifecycle"] = typeof(BtsmtlSkillSubmitActionLifecycleFlowNode),
                ["character-move-facing-angle"] = typeof(BtsmtlSkillMoveFacingAngleFlowNode),
                ["pipeline-blackboard-bool"] = typeof(BtsmtlSkillBlackboardBooleanFlowNode),
                ["pipeline-blackboard-float"] = typeof(BtsmtlSkillBlackboardScalarFlowNode),
                ["exposed-property"] = typeof(BtsmtlSkillBlackboardGetFlowNode),
                ["locomotion-input-motion"] = typeof(BtsmtlSkillLocomotionFlowNode),
                ["macro-call"] = typeof(MacroNodeWrapper)
            };
            foreach (BtsmtlSkillNativeNodeContract contract in BtsmtlSkillNativeNodeCatalog.All)
                result[contract.Kind] = contract.NodeType;
            return result;
        }

        static IReadOnlyList<BtsmtlSkillFlowGraphRole> AllowedRoles(Type type)
        {
            if (type == typeof(MacroInputNode) || type == typeof(MacroOutputNode))
                return new[] { BtsmtlSkillFlowGraphRole.Subgraph };
            if (type == typeof(BtsmtlSkillStateEnterFlowNode) ||
                type == typeof(BtsmtlSkillStateAnyFlowNode) ||
                type == typeof(BtsmtlSkillStateExitFlowNode) ||
                type == typeof(BtsmtlSkillStateFlowNode))
                return new[] { BtsmtlSkillFlowGraphRole.StateMachine };
            if (typeof(BtsmtlSkillTimelineHookFlowNode).IsAssignableFrom(type))
                return new[] { BtsmtlSkillFlowGraphRole.TimelineBody };
            if (typeof(BtsmtlSkillStateLifecycleFlowNode).IsAssignableFrom(type))
                return new[] { BtsmtlSkillFlowGraphRole.StateBody };
            if (type == typeof(BtsmtlSkillRootFlowNode))
                return new[]
                {
                    BtsmtlSkillFlowGraphRole.Skill,
                    BtsmtlSkillFlowGraphRole.StateBody,
                    BtsmtlSkillFlowGraphRole.TimelineBody
                };
            if (type == typeof(BtsmtlSkillConditionResultFlowNode))
                return new[] { BtsmtlSkillFlowGraphRole.ConditionRule };
            if (BtsmtlSkillNativeNodeCatalog.TryGet(type, out _))
                return Enum.GetValues(typeof(BtsmtlSkillFlowGraphRole))
                    .Cast<BtsmtlSkillFlowGraphRole>()
                    .Where(value => value != BtsmtlSkillFlowGraphRole.StateMachine)
                    .ToArray();
            if (typeof(IBtsmtlSkillPureValueNode).IsAssignableFrom(type))
                return Enum.GetValues(typeof(BtsmtlSkillFlowGraphRole))
                    .Cast<BtsmtlSkillFlowGraphRole>()
                    .Where(value => value != BtsmtlSkillFlowGraphRole.StateMachine)
                    .ToArray();
            return Enum.GetValues(typeof(BtsmtlSkillFlowGraphRole))
                .Cast<BtsmtlSkillFlowGraphRole>()
                .Where(value => value != BtsmtlSkillFlowGraphRole.StateMachine &&
                                value != BtsmtlSkillFlowGraphRole.ConditionRule)
                .ToArray();
        }

        static List<string> Properties(string kind)
        {
            return kind switch
            {
                "loop" => new List<string> { "stopType" },
                "parallel" => new List<string> { "mode" },
                "state-exit-cause" => new List<string> { "cause" },
                "character-input-bool" or "character-input-float" or
                    "character-input-vector2" or "character-input-vector2-magnitude" or
                    "character-action-request" => new List<string> { "inputId" },
                "action-context-active" => new List<string> { "actionContext" },
                "action-window-active" => new List<string> { "windowType" },
                "can-activate-action" => new List<string> { "actionProfile", "targetSnapshot" },
                "submit-action-lifecycle" => new List<string> { "actionContext", "transitionType", "reason" },
                "pipeline-blackboard-bool" or "pipeline-blackboard-float" => new List<string> { "declarationId", "ownerId", "valueType" },
                "exposed-property" => new List<string> { "declarationId", "ownerId", "valueType", "accessMode", "factContext" },
                "state-machine" => new List<string> { "graphId" },
                "state" => new List<string> { "bodyGraphId", "steps" },
                "timeline" => new List<string> { "timelineId", "timelineOwnership", "actionContext", "playbackMode" },
                "locomotion-input-motion" => new List<string>
                {
                    "moveSpeed", "displacementMode", "turnSpeedDegrees", "cameraRelative",
                    "executionMode", "durationSeconds", "actionMotionCurve"
                },
                "macro-call" => new List<string> { "graphId" },
                _ => new List<string>()
            };
        }

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
                capacity = direction == "Input" ? "Single" : "Multiple"
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
            IEnumerable<Port> ports = flow
                ? node.GetInputFlowPorts().Cast<Port>()
                    .Concat(node.GetOutputFlowPorts().Cast<Port>())
                : node.GetInputValuePorts().Cast<Port>()
                    .Concat(node.GetOutputValuePorts().Cast<Port>());
            return ports.OrderBy(value => value.ID, StringComparer.Ordinal)
                .Select(value => new AgentPackagePortDescriptor
                {
                    key = value.ID,
                    direction = value.IsInputPort() ? "Input" : "Output",
                    valueType = flow ? string.Empty : ValueType(value.type),
                    capacity = value.IsInputPort() ? "Single" : "Multiple",
                    required = value is ValueInput input && input.isRequired
                })
                .ToList();
        }
    }
}
