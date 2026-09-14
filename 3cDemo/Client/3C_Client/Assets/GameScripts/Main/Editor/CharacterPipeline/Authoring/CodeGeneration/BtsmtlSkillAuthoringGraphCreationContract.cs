using System;
using FlowCanvas;
using ThirdPersonCharacter.Control.Authoring;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public static class BtsmtlSkillAuthoringGraphCreationContract
    {
        public static T EnsureOwnedGraph<T>(
            FlowGraph owner,
            string identity,
            Type graphType,
            BtsmtlSkillFlowGraphRole role,
            string name)
        {
            if (graphType == typeof(BtsmtlSkillMacroGraph))
                return (T)(object)BtsmtlSkillAuthoringCode.EnsureMacro(owner, identity, name);
            if (graphType == typeof(BtsmtlSkillFlowGraph))
                return (T)(object)BtsmtlSkillAuthoringCode.EnsureChildGraph(owner, identity, role, name);
            throw new InvalidOperationException($"技能子图类型没有正式创建合同：{graphType?.FullName}");
        }
    }
}
