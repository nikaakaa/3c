#if UNITY_EDITOR
using System;
using System.Collections.Generic;
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
    }
}
#endif
