using System;
namespace TreeDesigner
{
    [Serializable]
    [NodeName("Succeed")]
    [NodePath("Base/Action/Succeed")]
    [NodeAuthoringCapability(NodeAuthoringCapability.SharedFlow)]
    public sealed class SucceedNode : ActionNode
    {
        protected override void DoAction()
        {
        }
    }
}
