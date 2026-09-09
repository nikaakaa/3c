#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using FlowCanvas;
using ParadoxNotion;
using UnityEngine;

namespace ThirdPersonCharacter.Control.Authoring
{
    public static class BtsmtlSkillMacroInterface
    {
        public static void Initialize(BtsmtlSkillMacroGraph graph)
        {
            if (graph.inputDefinitions.Count != 0 || graph.outputDefinitions.Count != 0)
                throw new InvalidOperationException("技能Macro接口只能初始化一次。");
            graph.inputDefinitions.Add(new DynamicParameterDefinition("执行", typeof(Flow)));
        }

        public static void Validate(BtsmtlSkillMacroGraph graph)
            => Validate(graph.inputDefinitions, graph.outputDefinitions);

        public static void ValidateCall(MacroNodeWrapper call, BtsmtlSkillMacroGraph graph, string path)
        {
            if (call == null || graph == null)
                throw new InvalidOperationException($"{path}: 技能Macro调用目标缺失。");
            Validate(graph);
            var inputIds = new HashSet<string>(StringComparer.Ordinal);
            var outputIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (DynamicParameterDefinition definition in graph.inputDefinitions)
            {
                Port port = call.GetInputPort(definition.ID);
                RequirePort(port, definition, true, path);
                inputIds.Add(definition.ID);
            }
            foreach (DynamicParameterDefinition definition in graph.outputDefinitions)
            {
                Port port = call.GetOutputPort(definition.ID);
                RequirePort(port, definition, false, path);
                outputIds.Add(definition.ID);
            }
            foreach (Port port in call.GetInputFlowPorts().Cast<Port>().Concat(call.GetInputValuePorts()))
                if (!inputIds.Contains(port.ID))
                    throw new InvalidOperationException($"{path}: 调用节点包含未登记输入端口'{port.ID}'。");
            foreach (Port port in call.GetOutputFlowPorts().Cast<Port>().Concat(call.GetOutputValuePorts()))
                if (!outputIds.Contains(port.ID))
                    throw new InvalidOperationException($"{path}: 调用节点包含未登记输出端口'{port.ID}'。");
        }

        public static void Validate(IEnumerable<DynamicParameterDefinition> inputs, IEnumerable<DynamicParameterDefinition> outputs)
        {
            var identities = new HashSet<string>(StringComparer.Ordinal);
            var names = new HashSet<string>(StringComparer.Ordinal);
            int flowInputs = 0;
            foreach (DynamicParameterDefinition input in inputs)
            {
                RequireParameter(input, identities, names);
                if (input.type == typeof(Flow))
                    flowInputs++;
                else
                    RequireValueType(input.type);
            }
            if (flowInputs != 1)
                throw new InvalidOperationException("技能Macro必须有且仅有一个执行入口。");
            names.Clear();
            foreach (DynamicParameterDefinition output in outputs)
            {
                RequireParameter(output, identities, names);
                RequireValueType(output.type);
            }
        }

        static void RequireParameter(DynamicParameterDefinition parameter, HashSet<string> identities, HashSet<string> names)
        {
            if (parameter == null || !parameter.hasStableIdentity || !identities.Add(parameter.ID) ||
                string.IsNullOrWhiteSpace(parameter.name) || !names.Add(parameter.name))
                throw new InvalidOperationException("技能Macro参数必须有独立稳定身份和名称。");
        }

        public static void RequireValueType(Type type)
        {
            if (type != typeof(bool) && type != typeof(int) && type != typeof(float) && type != typeof(string) &&
                type != typeof(Vector2) && type != typeof(Vector3) && type != typeof(uint) && type != typeof(ulong))
                throw new InvalidOperationException($"技能Macro不支持'{type?.FullName}'值参数；执行完成由技能调用状态返回。");
        }

        static void RequirePort(Port port, DynamicParameterDefinition definition, bool input, string path)
        {
            if (port == null ||
                port.IsFlowPort() != (definition.type == typeof(Flow)) ||
                definition.type != typeof(Flow) && port.type != definition.type)
            {
                string direction = input ? "输入" : "输出";
                throw new InvalidOperationException(
                    $"{path}: Macro{direction}端口'{definition.ID}'类型或方向与接口不一致。");
            }
        }
    }
}
#endif
