#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using FlowCanvas.Macros;
using UnityEditor;

namespace ThirdPersonCharacter.Control.Authoring
{
    public static class BtsmtlSkillNodeCatalog
    {
        public static IReadOnlyList<Type> All { get; } = Create();

        static readonly HashSet<Type> s_Types = new(All);
        public static bool Contains(Type type) => type != null && s_Types.Contains(type);

        static IReadOnlyList<Type> Create()
        {
            var result = TypeCache.GetTypesDerivedFrom<BtsmtlSkillFlowNode>()
                .Where(type => type != null && !type.IsAbstract && !type.ContainsGenericParameters)
                .ToList();
            result.Add(typeof(MacroNodeWrapper));
            result.Add(typeof(BtsmtlSkillMacroInputNode));
            result.Add(typeof(BtsmtlSkillMacroOutputNode));
            result.AddRange(BtsmtlSkillNativeNodeCatalog.All.Select(value => value.NodeType));
            return result.Distinct().OrderBy(type => type.FullName, StringComparer.Ordinal).ToArray();
        }
    }
}
#endif
