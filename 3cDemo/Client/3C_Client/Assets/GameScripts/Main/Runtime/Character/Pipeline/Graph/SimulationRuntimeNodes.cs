using System;
using TreeDesigner;

namespace ThirdPersonCharacter.Pipeline.Graph
{
    [Serializable]
    public abstract class SimulationOperationNode : ActionNode
    {
        protected sealed override void DoAction()
        {
        }
    }

    [Serializable]
    public abstract class SimulationValueNode : ValueNode
    {
        protected sealed override void OutputValue()
        {
        }
    }
}
