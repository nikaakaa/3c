#if UNITY_EDITOR
using FlowCanvas;
using FlowCanvas.Nodes;
using NodeCanvas.Framework;

namespace ThirdPersonCharacter.Control.Authoring
{
    public sealed class BtsmtlSkillNativeNodeWrapper<T> : SimplexNodeWrapper<T>
        where T : SimplexNode
    {
        protected override void OnNodeInspectorGUI() =>
            BtsmtlSkillNodeInspector.DrawValueInputs((FlowGraph)graph, this);
    }
}
#endif
